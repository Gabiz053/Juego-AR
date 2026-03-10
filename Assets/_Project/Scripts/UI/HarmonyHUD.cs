// ??????????????????????????????????????????????
//  HarmonyHUD.cs  ·  _Project.Scripts.UI
//  Pure UI controller for the Harmony meter widget.
//  Drives visuals only — no harmony scoring logic here.
// ??????????????????????????????????????????????

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using _Project.Scripts.Core;

namespace _Project.Scripts.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/UI/Harmony HUD")]
    public class HarmonyHUD : MonoBehaviour
    {
        #region Inspector ?????????????????????????????????????

        [Header("Bar Fill Image")]
        [Tooltip("The Image used as the fill. Must be a direct child of the bar background.\n" +
                 "Anchor Min=(0,0) Anchor Max=(0,1) Pivot=(0, 0.5) — script drives anchorMax.x.")]
        [SerializeField] private Image _fillImage;

        [Tooltip("Seconds the bar takes to animate to a new value.")]
        [SerializeField] private float _animDuration = 0.6f;

        [Header("Label")]
        [SerializeField] private TMP_Text _statusLabel;

        [Header("Colour Gradient")]
        [SerializeField] private Color _colourLow  = new Color(0.90f, 0.35f, 0.10f);
        [SerializeField] private Color _colourMid  = new Color(0.95f, 0.80f, 0.10f);
        [SerializeField] private Color _colourHigh = new Color(0.20f, 0.78f, 0.35f);

        [Header("Status Phrases")]
        [SerializeField] private string _phraseEmpty   = "Empieza tu jardín";
        [SerializeField] private string _phraseLow     = "Añade más variedad";
        [SerializeField] private string _phraseMid     = "Jardín equilibrado";
        [SerializeField] private string _phraseHigh    = "Gran armonía ?";
        [SerializeField] private string _phrasePerfect = "¡Armonía perfecta! ??";

        [Header("Service")]
        [SerializeField] private HarmonyService _harmonyService;

        [Header("State Change Animation")]
        [Tooltip("RectTransform to pop/shake. Leave empty to use this GameObject's own.")]
        [SerializeField] private RectTransform _hudRoot;
        [SerializeField] private float _popScale     = 1.20f;
        [SerializeField] private float _popDuration  = 0.45f;
        [SerializeField] private float _shakeStrength = 7f;
        [SerializeField] private float _shakeDuration = 0.28f;

        [Header("Rounded Bar")]
        [Tooltip("Pixel radius for the rounded corners on the fill image.\n" +
                 "Requires the fill Image to use a 9-sliced sprite with rounded corners,\n" +
                 "OR set to 0 to skip (use a plain sprite instead).")]
        [SerializeField] private float _cornerRadius = 8f;

        #endregion

        #region State ?????????????????????????????????????????

        private float         _displayedValue;
        private float         _targetValue;
        private Coroutine     _animCoroutine;
        private Coroutine     _popCoroutine;
        private RectTransform _fillRect;
        private Vector3       _hudOriginalScale;
        private Vector2       _hudOriginalAnchoredPos;
        private int           _lastPhaseIndex = -1;
        private UIAudioService _uiAudio;
        // When true the HUD is frozen — no animations or sounds until world reset.
        private bool          _frozen;

        #endregion

        #region Unity Lifecycle ????????????????????????????????

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

            // Auto-locate UIAudioService from the root Canvas.
            Canvas root = GetComponentInParent<Canvas>();
            if (root != null)
                _uiAudio = root.GetComponentInChildren<UIAudioService>();

            // Apply rounded corners to fill and background if a radius is set.
            if (_cornerRadius > 0f)
                ApplyRoundedCorners();
        }

        private void OnEnable()
        {
            if (_harmonyService != null)
            {
                _harmonyService.OnHarmonyChanged  += SetHarmony;
                _harmonyService.OnPerfectHarmony  += OnPerfectReached;
                _harmonyService.OnWorldReset       += OnWorldReset;
            }
        }

        private void OnDisable()
        {
            if (_harmonyService != null)
            {
                _harmonyService.OnHarmonyChanged  -= SetHarmony;
                _harmonyService.OnPerfectHarmony  -= OnPerfectReached;
                _harmonyService.OnWorldReset       -= OnWorldReset;
            }
        }

        private void Start()
        {
            StartCoroutine(InitAfterLayout());
        }

        private IEnumerator InitAfterLayout()
        {
            yield return null;
            float initial       = _harmonyService != null ? _harmonyService.CurrentScore : 0f;
            _lastPhaseIndex     = GetPhaseIndex(initial);
            ApplyImmediate(initial);
        }

        #endregion

        #region Public API ????????????????????????????????????

        public void SetHarmony(float value01)
        {
            // Frozen after winning — ignore all updates until world is reset.
            if (_frozen) return;

            _targetValue = Mathf.Clamp01(value01);

            int  newPhase     = GetPhaseIndex(_targetValue);
            bool phaseChanged = newPhase != _lastPhaseIndex && _lastPhaseIndex != -1;

            if (phaseChanged)
            {
                _uiAudio?.PlayHarmonyPhase(newPhase);

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

        // Called once when perfect harmony is reached — freezes HUD reactions.
        private void OnPerfectReached()
        {
            _frozen = true;

            // Stop any running animations and leave the bar at 100 %.
            if (_animCoroutine != null) { StopCoroutine(_animCoroutine); _animCoroutine = null; }
            if (_popCoroutine  != null) { StopCoroutine(_popCoroutine);  _popCoroutine  = null; }

            // Snap to full so the bar stays visually complete.
            _hudRoot.localScale       = _hudOriginalScale;
            _hudRoot.anchoredPosition = _hudOriginalAnchoredPos;
            ApplyImmediate(1f);
        }

        // Called when the world is reset — unfreezes the HUD for a new game.
        private void OnWorldReset()
        {
            _frozen         = false;
            _lastPhaseIndex = -1;
            ApplyImmediate(0f);
        }

        #endregion

        #region Animation ?????????????????????????????????????

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

            // ?? 1. Overshoot scale up ??????????????????????
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                float t  = Mathf.SmoothStep(0f, 1f, elapsed / half);
                // Slight overshoot via AnimationCurve-style bounce.
                float scale = Mathf.LerpUnclamped(1f, _popScale + 0.05f, t);
                _hudRoot.localScale = _hudOriginalScale * scale;
                yield return null;
            }

            // ?? 2. Spring back past original + settle ??????
            elapsed = 0f;
            float springDur = _popDuration * 0.7f;

            while (elapsed < springDur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t  = elapsed / springDur;
                // Damped spring: oscillates and decays to 1.
                float spring = 1f + (_popScale + 0.05f - 1f)
                               * Mathf.Exp(-t * 8f)
                               * Mathf.Cos(t * 18f);
                _hudRoot.localScale = _hudOriginalScale * spring;

                // Perlin shake decays alongside spring.
                if (elapsed < _shakeDuration)
                {
                    float decay = 1f - elapsed / _shakeDuration;
                    float ox    = (Mathf.PerlinNoise(elapsed * 55f, 0f)   - 0.5f) * 2f * _shakeStrength * decay;
                    float oy    = (Mathf.PerlinNoise(0f,   elapsed * 55f) - 0.5f) * 2f * _shakeStrength * decay;
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

        // Phase 3 (75-99 %): rapid side-to-side buzz — "¡Ya casi lo tienes!"
        private IEnumerator AnticipationShake()
        {
            if (_hudRoot == null) yield break;

            // ?? 1. Fast scale pop ??????????????????????????
            float elapsed = 0f;
            float popUp   = 0.10f;
            while (elapsed < popUp)
            {
                elapsed += Time.unscaledDeltaTime;
                float t  = Mathf.SmoothStep(0f, 1f, elapsed / popUp);
                _hudRoot.localScale = Vector3.LerpUnclamped(
                    _hudOriginalScale, _hudOriginalScale * (_popScale + 0.10f), t);
                yield return null;
            }

            // ?? 2. Rapid side-to-side pulses ???????????????
            int   pulses    = 7;
            float pulseDur  = 0.055f;
            float amplitude = _shakeStrength * 2.6f;

            for (int i = 0; i < pulses; i++)
            {
                float decay = 1f - (float)i / pulses;
                float dir   = (i % 2 == 0) ? 1f : -1f;
                elapsed     = 0f;

                while (elapsed < pulseDur)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t  = elapsed / pulseDur;
                    float ox = Mathf.Sin(t * Mathf.PI) * amplitude * decay * dir;
                    // Scale pulses slightly with each side tap.
                    float sc = 1f + Mathf.Sin(t * Mathf.PI) * 0.05f * decay;
                    _hudRoot.anchoredPosition = _hudOriginalAnchoredPos + new Vector2(ox, 0f);
                    _hudRoot.localScale       = _hudOriginalScale * sc;
                    yield return null;
                }
            }

            // ?? 3. Tiny upward bounce to finish ???????????
            elapsed = 0f;
            float bounceDur = 0.14f;
            while (elapsed < bounceDur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t  = elapsed / bounceDur;
                float oy = Mathf.Sin(t * Mathf.PI) * _shakeStrength * 1.4f;
                _hudRoot.anchoredPosition = _hudOriginalAnchoredPos + new Vector2(0f, oy);
                yield return null;
            }

            _hudRoot.localScale       = _hudOriginalScale;
            _hudRoot.anchoredPosition = _hudOriginalAnchoredPos;
            _popCoroutine = null;
        }

        #endregion

        #region Helpers ???????????????????????????????????????

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

        private Color SampleGradient(float t)
        {
            if (t <= 0.5f) return Color.Lerp(_colourLow, _colourMid, t * 2f);
            return Color.Lerp(_colourMid, _colourHigh, (t - 0.5f) * 2f);
        }

        private string GetPhrase(float t)
        {
            if (t >= 1.00f) return _phrasePerfect;
            if (t >= 0.75f) return _phraseHigh;
            if (t >= 0.50f) return _phraseMid;
            if (t >= 0.25f) return _phraseLow;
            return _phraseEmpty;
        }

        private int GetPhaseIndex(float t)
        {
            if (t >= 1.00f) return 4;
            if (t >= 0.75f) return 3;
            if (t >= 0.50f) return 2;
            if (t >= 0.25f) return 1;
            return 0;
        }

        /// <summary>
        /// Generates a rounded-rectangle sprite at runtime and assigns it to
        /// the fill image and its parent background image.
        /// No external sprite asset required.
        /// </summary>
        private void ApplyRoundedCorners()
        {
            if (_fillImage == null) return;

            int w = 128, h = 32;
            int r = Mathf.Clamp(Mathf.RoundToInt(_cornerRadius), 1, h / 2);

            Sprite rounded = CreateRoundedSprite(w, h, r);

            // Apply to fill.
            _fillImage.sprite    = rounded;
            _fillImage.type      = Image.Type.Sliced;
            _fillImage.pixelsPerUnitMultiplier = 1f;

            // Apply same sprite to the background Image if it exists.
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
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode   = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    pixels[y * w + x] = IsInsideRoundedRect(x, y, w, h, r)
                        ? Color.white
                        : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            // 9-slice border = corner radius so the sprite stretches correctly.
            return Sprite.Create(
                tex,
                new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 100f,
                extrude: 0,
                meshType: SpriteMeshType.FullRect,
                border: new Vector4(r, r, r, r));
        }

        private static bool IsInsideRoundedRect(int x, int y, int w, int h, int r)
        {
            // Inside the central cross (no corner check needed).
            if (x >= r && x < w - r) return true;
            if (y >= r && y < h - r) return true;

            // Corner circles.
            int cx = (x < r) ? r       : w - r - 1;
            int cy = (y < r) ? r       : h - r - 1;
            int dx = x - cx, dy = y - cy;
            return dx * dx + dy * dy <= r * r;
        }

        #endregion

        #region Validation ????????????????????????????????????

        private void OnValidate()
        {
            if (_fillImage == null)
                Debug.LogWarning("[HarmonyHUD] _fillImage is not assigned.", this);
        }

        #endregion
    }
}
