// ------------------------------------------------------------
//  HarmonyHUD.cs  -  _Project.Scripts.UI
//  Pure UI controller for the Harmony meter widget.
// ------------------------------------------------------------

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using _Project.Scripts.Infrastructure;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Animated harmony bar with colour gradient, status phrases and
    /// pop/shake feedback on phase transitions.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/UI/Harmony HUD")]
    public class HarmonyHUD : MonoBehaviour
    {
        #region Constants -----------------------------------------

        private const float POP_OVERSHOOT        = 0.05f;
        private const float SPRING_DECAY_RATE    = 8f;
        private const float SPRING_FREQUENCY     = 18f;
        private const float SHAKE_PERLIN_SPEED   = 55f;
        private const float ANTIC_POP_UP_DUR     = 0.10f;
        private const float ANTIC_POP_EXTRA      = 0.10f;
        private const int   ANTIC_PULSE_COUNT    = 7;
        private const float ANTIC_PULSE_DURATION = 0.055f;
        private const float ANTIC_AMP_MULTIPLIER = 2.6f;
        private const float ANTIC_PULSE_SCALE    = 0.05f;
        private const float ANTIC_BOUNCE_DUR     = 0.14f;
        private const float ANTIC_BOUNCE_MULT    = 1.4f;
        private const float SPRING_DUR_RATIO     = 0.7f;

        /// <summary>Phase boundary: low harmony threshold (25%).</summary>
        private const float PHASE_LOW     = 0.25f;

        /// <summary>Phase boundary: mid harmony threshold (50%).</summary>
        private const float PHASE_MID     = 0.50f;

        /// <summary>Phase boundary: high harmony threshold (75%).</summary>
        private const float PHASE_HIGH    = 0.75f;

        /// <summary>Phase boundary: perfect harmony threshold (100%).</summary>
        private const float PHASE_PERFECT = 1.00f;

        /// <summary>Width of the procedural rounded-corner texture (px).</summary>
        private const int ROUNDED_TEX_WIDTH  = 128;

        /// <summary>Height of the procedural rounded-corner texture (px).</summary>
        private const int ROUNDED_TEX_HEIGHT = 32;

        /// <summary>Default pixels-per-unit for procedural sprites.</summary>
        private const float DEFAULT_PIXELS_PER_UNIT = 100f;

        #endregion

        #region Inspector -----------------------------------------

        [Header("Bar Fill Image")]
        [Tooltip("Image used as the fill (anchorMax.x is driven by script).")]
        [SerializeField] private Image _fillImage;

        [Tooltip("Seconds the bar takes to animate to a new value.")]
        [SerializeField] private float _animDuration = 0.6f;

        [Header("Label")]
        [Tooltip("TMP label that shows the current harmony status phrase.")]
        [SerializeField] private TMP_Text _statusLabel;

        [Header("Colour Gradient")]
        [Tooltip("Bar colour at low harmony (0-50%).")]
        [SerializeField] private Color _colourLow  = new Color(0.90f, 0.35f, 0.10f);
        [Tooltip("Bar colour at mid harmony (50%).")]
        [SerializeField] private Color _colourMid  = new Color(0.95f, 0.80f, 0.10f);
        [Tooltip("Bar colour at high harmony (50-100%).")]
        [SerializeField] private Color _colourHigh = new Color(0.20f, 0.78f, 0.35f);

        [Header("Status Phrases")]
        [Tooltip("Phrase shown when harmony is 0%.")]
        [SerializeField] private string _phraseEmpty   = "Empieza tu jardin";
        [Tooltip("Phrase shown when harmony is 25-49%.")]
        [SerializeField] private string _phraseLow     = "Anade mas variedad";
        [Tooltip("Phrase shown when harmony is 50-74%.")]
        [SerializeField] private string _phraseMid     = "Jardin equilibrado";
        [Tooltip("Phrase shown when harmony is 75-99%.")]
        [SerializeField] private string _phraseHigh    = "Gran armonia";
        [Tooltip("Phrase shown when harmony reaches 100%.")]
        [SerializeField] private string _phrasePerfect = "Armonia perfecta!";

        [Header("State Change Animation")]
        [Tooltip("RectTransform to pop/shake (defaults to this GO).")]
        [SerializeField] private RectTransform _hudRoot;
        [Tooltip("Peak scale multiplier during the pop animation.")]
        [SerializeField] private float _popScale      = 1.20f;
        [Tooltip("Seconds for the full pop + spring-back cycle.")]
        [SerializeField] private float _popDuration   = 0.45f;
        [Tooltip("Pixel intensity of the Perlin shake offset.")]
        [SerializeField] private float _shakeStrength = 7f;
        [Tooltip("Seconds the shake lasts before decaying to zero.")]
        [SerializeField] private float _shakeDuration = 0.28f;

        [Header("Rounded Bar")]
        [Tooltip("Pixel radius for rounded corners on the fill image (0 = skip).")]
        [SerializeField] private float _cornerRadius = 8f;

        #endregion

        #region State ---------------------------------------------

        private float          _displayedValue;
        private float          _targetValue;
        private Coroutine      _animCoroutine;
        private Coroutine      _popCoroutine;
        private RectTransform  _fillRect;
        private Vector3        _hudOriginalScale;
        private Vector2        _hudOriginalAnchoredPos;
        private int            _lastPhaseIndex = -1;
        private IHarmonyService _harmonyService;
        private IUIAudioService _uiAudio;
        private IHapticService  _hapticService;
        private bool           _frozen;

        #endregion

        #region Public API ----------------------------------------

        public void SetHarmony(float value01)
        {
            if (_frozen) return;

            _targetValue = Mathf.Clamp01(value01);

            int  newPhase     = GetPhaseIndex(_targetValue);
            bool phaseChanged = newPhase != _lastPhaseIndex && _lastPhaseIndex != -1;

            if (phaseChanged)
            {
                _uiAudio?.PlayHarmonyPhase(newPhase);
                PlayPhaseHaptic(newPhase);

                if (_popCoroutine != null) StopCoroutine(_popCoroutine);
                _popCoroutine = newPhase == 3
                    ? StartCoroutine(AnticipationShake())
                    : StartCoroutine(PopAndShake());
            }
            _lastPhaseIndex = newPhase;

            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _animCoroutine = StartCoroutine(AnimateBar());
        }

        public void SetHarmonyImmediate(float value01)
        {
            if (_frozen) return;
            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _displayedValue = Mathf.Clamp01(value01);
            _targetValue    = _displayedValue;
            ApplyImmediate(_displayedValue);
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            if (_fillImage != null)
                _fillRect = _fillImage.GetComponent<RectTransform>();

            if (_hudRoot == null)
                _hudRoot = GetComponent<RectTransform>();

            if (_hudRoot != null)
            {
                _hudOriginalScale       = _hudRoot.localScale;
                _hudOriginalAnchoredPos = _hudRoot.anchoredPosition;
            }

            ServiceLocator.TryGet<IHarmonyService>(out _harmonyService);
            ServiceLocator.TryGet<IUIAudioService>(out _uiAudio);
            ServiceLocator.TryGet<IHapticService>(out _hapticService);

            if (_cornerRadius > 0f)
                ApplyRoundedCorners();
        }

        private void OnEnable()
        {
            if (_harmonyService != null)
            {
                _harmonyService.OnHarmonyChanged += SetHarmony;
                _harmonyService.OnPerfectHarmony += OnPerfectReached;
                _harmonyService.OnWorldReset     += OnWorldReset;
            }
        }

        private void OnDisable()
        {
            if (_harmonyService != null)
            {
                _harmonyService.OnHarmonyChanged -= SetHarmony;
                _harmonyService.OnPerfectHarmony -= OnPerfectReached;
                _harmonyService.OnWorldReset     -= OnWorldReset;
            }
        }

        private void Start()
        {
            ValidateReferences();
            StartCoroutine(InitAfterLayout());
        }

        private IEnumerator InitAfterLayout()
        {
            yield return null;
            float initial   = _harmonyService != null ? _harmonyService.CurrentScore : 0f;
            _lastPhaseIndex = GetPhaseIndex(initial);
            ApplyImmediate(initial);
        }

        #endregion

        #region Internals -----------------------------------------

        private void OnPerfectReached()
        {
            _frozen = true;
            if (_animCoroutine != null) { StopCoroutine(_animCoroutine); _animCoroutine = null; }
            if (_popCoroutine  != null) { StopCoroutine(_popCoroutine);  _popCoroutine  = null; }

            _hudRoot.localScale       = _hudOriginalScale;
            _hudRoot.anchoredPosition = _hudOriginalAnchoredPos;
            ApplyImmediate(1f);
        }

        private void OnWorldReset()
        {
            _frozen         = false;
            _lastPhaseIndex = -1;
            ApplyImmediate(0f);
        }

        /// <summary>
        /// Escalating haptic pulse that grows stronger with each
        /// harmony phase: 0 = light, 1 = light, 2 = medium, 3 = heavy.
        /// </summary>
        private void PlayPhaseHaptic(int phase)
        {
            if (_hapticService == null) return;

            switch (phase)
            {
                case 0: case 1: _hapticService.VibrateLight();  break;
                case 2:         _hapticService.VibrateMedium(); break;
                case 3:         _hapticService.VibrateHeavy();  break;
            }
        }

        private IEnumerator AnimateBar()
        {
            float start   = _displayedValue;
            float elapsed = 0f;

            while (elapsed < _animDuration)
            {
                elapsed        += Time.deltaTime;
                float t         = Mathf.SmoothStep(0f, 1f, elapsed / _animDuration);
                _displayedValue = Mathf.Lerp(start, _targetValue, t);
                ApplyImmediate(_displayedValue);
                yield return null;
            }

            _displayedValue = _targetValue;
            ApplyImmediate(_displayedValue);
            _animCoroutine = null;
        }

        private IEnumerator PopAndShake()
        {
            if (_hudRoot == null) yield break;

            float elapsed = 0f;
            float half    = _popDuration * 0.5f;

            // 1. Overshoot scale up
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                float t  = Mathf.SmoothStep(0f, 1f, elapsed / half);
                float scale = Mathf.LerpUnclamped(1f, _popScale + POP_OVERSHOOT, t);
                _hudRoot.localScale = _hudOriginalScale * scale;
                yield return null;
            }

            // 2. Spring back + settle
            elapsed = 0f;
            float springDur = _popDuration * SPRING_DUR_RATIO;

            while (elapsed < springDur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t  = elapsed / springDur;
                float spring = 1f + (_popScale + POP_OVERSHOOT - 1f)
                               * Mathf.Exp(-t * SPRING_DECAY_RATE)
                               * Mathf.Cos(t * SPRING_FREQUENCY);
                _hudRoot.localScale = _hudOriginalScale * spring;

                if (elapsed < _shakeDuration)
                {
                    float decay = 1f - elapsed / _shakeDuration;
                    float ox    = (Mathf.PerlinNoise(elapsed * SHAKE_PERLIN_SPEED, 0f)   - 0.5f) * 2f * _shakeStrength * decay;
                    float oy    = (Mathf.PerlinNoise(0f,   elapsed * SHAKE_PERLIN_SPEED) - 0.5f) * 2f * _shakeStrength * decay;
                    _hudRoot.anchoredPosition = _hudOriginalAnchoredPos + new Vector2(ox, oy);
                }
                else
                {
                    _hudRoot.anchoredPosition = _hudOriginalAnchoredPos;
                }
                yield return null;
            }

            _hudRoot.localScale       = _hudOriginalScale;
            _hudRoot.anchoredPosition = _hudOriginalAnchoredPos;
            _popCoroutine = null;
        }

        private IEnumerator AnticipationShake()
        {
            if (_hudRoot == null) yield break;

            // 1. Fast scale pop
            float elapsed = 0f;
            while (elapsed < ANTIC_POP_UP_DUR)
            {
                elapsed += Time.unscaledDeltaTime;
                float t  = Mathf.SmoothStep(0f, 1f, elapsed / ANTIC_POP_UP_DUR);
                _hudRoot.localScale = Vector3.LerpUnclamped(
                    _hudOriginalScale, _hudOriginalScale * (_popScale + ANTIC_POP_EXTRA), t);
                yield return null;
            }

            // 2. Rapid side-to-side pulses
            float amplitude = _shakeStrength * ANTIC_AMP_MULTIPLIER;

            for (int i = 0; i < ANTIC_PULSE_COUNT; i++)
            {
                float decay = 1f - (float)i / ANTIC_PULSE_COUNT;
                float dir   = (i % 2 == 0) ? 1f : -1f;
                elapsed     = 0f;

                while (elapsed < ANTIC_PULSE_DURATION)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t  = elapsed / ANTIC_PULSE_DURATION;
                    float ox = Mathf.Sin(t * Mathf.PI) * amplitude * decay * dir;
                    float sc = 1f + Mathf.Sin(t * Mathf.PI) * ANTIC_PULSE_SCALE * decay;
                    _hudRoot.anchoredPosition = _hudOriginalAnchoredPos + new Vector2(ox, 0f);
                    _hudRoot.localScale       = _hudOriginalScale * sc;
                    yield return null;
                }
            }

            // 3. Tiny upward bounce
            elapsed = 0f;
            while (elapsed < ANTIC_BOUNCE_DUR)
            {
                elapsed += Time.unscaledDeltaTime;
                float t  = elapsed / ANTIC_BOUNCE_DUR;
                float oy = Mathf.Sin(t * Mathf.PI) * _shakeStrength * ANTIC_BOUNCE_MULT;
                _hudRoot.anchoredPosition = _hudOriginalAnchoredPos + new Vector2(0f, oy);
                yield return null;
            }

            _hudRoot.localScale       = _hudOriginalScale;
            _hudRoot.anchoredPosition = _hudOriginalAnchoredPos;
            _popCoroutine = null;
        }

        /// <summary>Sets fill anchors, colour and phrase text without animation.</summary>
        private void ApplyImmediate(float v)
        {
            if (_fillRect != null)
            {
                _fillRect.anchorMin = new Vector2(0f, 0f);
                _fillRect.anchorMax = new Vector2(Mathf.Clamp01(v), 1f);
                _fillRect.offsetMin = Vector2.zero;
                _fillRect.offsetMax = Vector2.zero;
            }

            if (_fillImage != null)
                _fillImage.color = SampleGradient(v);

            if (_statusLabel != null)
                _statusLabel.text = GetPhrase(v);
        }

        /// <summary>Lerps between low ? mid ? high colours based on score <paramref name="t"/>.</summary>
        private Color SampleGradient(float t)
        {
            if (t <= 0.5f) return Color.Lerp(_colourLow, _colourMid, t * 2f);
            return Color.Lerp(_colourMid, _colourHigh, (t - 0.5f) * 2f);
        }

        /// <summary>Returns the display phrase string for the given score bracket.</summary>
        private string GetPhrase(float t)
        {
            if (t >= PHASE_PERFECT) return _phrasePerfect;
            if (t >= PHASE_HIGH)    return _phraseHigh;
            if (t >= PHASE_MID)     return _phraseMid;
            if (t >= PHASE_LOW)     return _phraseLow;
            return _phraseEmpty;
        }

        /// <summary>Maps score to a 0-4 phase index for audio triggers.</summary>
        private int GetPhaseIndex(float t)
        {
            if (t >= PHASE_PERFECT) return 4;
            if (t >= PHASE_HIGH)    return 3;
            if (t >= PHASE_MID)     return 2;
            if (t >= PHASE_LOW)     return 1;
            return 0;
        }

        /// <summary>
        /// Generates a rounded-rectangle sprite procedurally and applies
        /// it to the fill image and its background (if present).
        /// </summary>
        private void ApplyRoundedCorners()
        {
            if (_fillImage == null) return;

            int w = ROUNDED_TEX_WIDTH, h = ROUNDED_TEX_HEIGHT;
            int r = Mathf.Clamp(Mathf.RoundToInt(_cornerRadius), 1, h / 2);

            Sprite rounded = CreateRoundedSprite(w, h, r);

            _fillImage.sprite = rounded;
            _fillImage.type   = Image.Type.Sliced;
            _fillImage.pixelsPerUnitMultiplier = 1f;

            Image bg = _fillImage.transform.parent?.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = rounded;
                bg.type   = Image.Type.Sliced;
                bg.pixelsPerUnitMultiplier = 1f;
            }
        }

        private static Sprite CreateRoundedSprite(int w, int h, int r)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode   = TextureWrapMode.Clamp
            };

            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = IsInsideRoundedRect(x, y, w, h, r) ? Color.white : Color.clear;

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(
                tex,
                new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: DEFAULT_PIXELS_PER_UNIT,
                extrude: 0,
                meshType: SpriteMeshType.FullRect,
                border: new Vector4(r, r, r, r));
        }

        private static bool IsInsideRoundedRect(int x, int y, int w, int h, int r)
        {
            if (x >= r && x < w - r) return true;
            if (y >= r && y < h - r) return true;

            int cx = (x < r) ? r : w - r - 1;
            int cy = (y < r) ? r : h - r - 1;
            int dx = x - cx, dy = y - cy;
            return dx * dx + dy * dy <= r * r;
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_harmonyService == null)
                Debug.LogWarning("[HarmonyHUD] _harmonyService is not assigned.", this);
            if (_fillImage == null)
                Debug.LogWarning("[HarmonyHUD] _fillImage is not assigned.", this);
        }

        #endregion
    }
}
