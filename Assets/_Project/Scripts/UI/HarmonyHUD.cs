// ??????????????????????????????????????????????
//  HarmonyHUD.cs  ·  _Project.Scripts.UI
//  Pure UI controller for the Harmony meter widget.
//  Drives visuals only — no harmony scoring logic here.
// ??????????????????????????????????????????????

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Controls the Harmony meter HUD widget (top-left corner).<br/>
    /// Exposes <see cref="SetHarmony"/> — call it from the harmony scoring
    /// service whenever the score changes.<br/>
    /// Visuals:<br/>
    /// • Horizontal fill bar whose colour lerps orange ? yellow ? green.<br/>
    /// • Short status label that changes at defined thresholds.<br/>
    /// • Smooth animated transition on every update (no jarring jumps).<br/>
    /// No game logic lives here — this script only drives the UI.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/UI/Harmony HUD")]
    public class HarmonyHUD : MonoBehaviour
    {
        #region Inspector ?????????????????????????????????????

        [Header("Bar")]
        [Tooltip("The Image used as the fill of the harmony bar.\n" +
                 "Works with ANY sprite and any Image Type — width is driven by RectTransform, not fillAmount.")]
        [SerializeField] private Image _fillImage;

        [Tooltip("The RectTransform that acts as the bar container (parent of _fillImage). " +
                 "Its width defines 100 %. Leave empty to use _fillImage's parent automatically.")]
        [SerializeField] private RectTransform _barContainer;

        [Tooltip("Seconds the bar takes to animate to a new value.")]
        [SerializeField] private float _animDuration = 0.6f;

        [Header("Label")]
        [Tooltip("TMP label that shows the harmony status phrase.")]
        [SerializeField] private TMP_Text _statusLabel;

        [Header("Colour Gradient")]
        [Tooltip("Colour of the fill bar at 0 % harmony.")]
        [SerializeField] private Color _colourLow    = new Color(0.90f, 0.35f, 0.10f); // orange

        [Tooltip("Colour of the fill bar at 50 % harmony.")]
        [SerializeField] private Color _colourMid    = new Color(0.95f, 0.80f, 0.10f); // yellow

        [Tooltip("Colour of the fill bar at 100 % harmony.")]
        [SerializeField] private Color _colourHigh   = new Color(0.20f, 0.78f, 0.35f); // jade green

        [Header("Status Phrases")]
        [Tooltip("Shown when harmony is 0 – 24 %.")]
        [SerializeField] private string _phraseEmpty    = "Empieza tu jardín";

        [Tooltip("Shown when harmony is 25 – 49 %.")]
        [SerializeField] private string _phraseLow      = "Añade más variedad";

        [Tooltip("Shown when harmony is 50 – 74 %.")]
        [SerializeField] private string _phraseMid      = "Jardín equilibrado";

        [Tooltip("Shown when harmony is 75 – 99 %.")]
        [SerializeField] private string _phraseHigh     = "Gran armonía ?";

        [Tooltip("Shown when harmony reaches 100 %.")]
        [SerializeField] private string _phrasePerfect  = "¡Armonía perfecta! ??";

        #endregion

        #region State ?????????????????????????????????????????

        private float          _displayedValue;
        private float          _targetValue;
        private Coroutine      _animCoroutine;
        private RectTransform  _fillRect;
        private float          _containerWidth;   // cached full width = 100 %

        #endregion

        #region Unity Lifecycle ????????????????????????????????

        private void Awake()
        {
            if (_fillImage != null)
                _fillRect = _fillImage.GetComponent<RectTransform>();

            // Resolve bar container: use the explicit reference or fall back to
            // the fill image's direct parent.
            if (_barContainer == null && _fillRect != null)
                _barContainer = _fillRect.parent as RectTransform;

            // Cache the container width AFTER layout has run.
            // We defer to Start so LayoutGroups have already executed.
        }

        private void Start()
        {
            // Read the full width of the container once layout is settled.
            _containerWidth = _barContainer != null ? _barContainer.rect.width : 0f;

            // Start empty.
            ApplyImmediate(0f);
        }

        #endregion

        #region Public API ????????????????????????????????????

        /// <summary>
        /// Updates the harmony display with a smooth animated transition.<br/>
        /// </summary>
        /// <param name="value01">Normalised harmony score in [0, 1].</param>
        public void SetHarmony(float value01)
        {
            _targetValue = Mathf.Clamp01(value01);

            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _animCoroutine = StartCoroutine(AnimateBar());
        }

        /// <summary>
        /// Snaps the bar to a value instantly with no animation.
        /// Useful for resets.
        /// </summary>
        public void SetHarmonyImmediate(float value01)
        {
            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _displayedValue = Mathf.Clamp01(value01);
            _targetValue    = _displayedValue;
            ApplyImmediate(_displayedValue);
        }

        #endregion

        #region Animation ?????????????????????????????????????

        private IEnumerator AnimateBar()
        {
            float start   = _displayedValue;
            float elapsed = 0f;

            while (elapsed < _animDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _animDuration);
                _displayedValue = Mathf.Lerp(start, _targetValue, t);
                ApplyImmediate(_displayedValue);
                yield return null;
            }

            _displayedValue = _targetValue;
            ApplyImmediate(_displayedValue);
        }

        /// <summary>
        /// Applies <paramref name="v"/> to the fill image and status label immediately.
        /// Width-based fill — works with any sprite type (Simple, Sliced, Filled, etc.).
        /// </summary>
        private void ApplyImmediate(float v)
        {
            if (_fillRect != null && _containerWidth > 0f)
            {
                // Anchor the fill to the left edge and scale its width.
                // offsetMax.x drives the right edge relative to the container.
                _fillRect.anchorMin = new Vector2(0f, 0f);
                _fillRect.anchorMax = new Vector2(0f, 1f);
                _fillRect.offsetMin = Vector2.zero;
                _fillRect.offsetMax = new Vector2(_containerWidth * v, 0f);
            }

            if (_fillImage != null)
                _fillImage.color = SampleGradient(v);

            if (_statusLabel != null)
                _statusLabel.text = GetPhrase(v);
        }

        #endregion

        #region Helpers ???????????????????????????????????????

        /// <summary>
        /// Three-stop colour gradient: low ? mid ? high.
        /// </summary>
        private Color SampleGradient(float t)
        {
            if (t <= 0.5f)
                return Color.Lerp(_colourLow, _colourMid, t * 2f);
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

        #endregion

        #region Validation ????????????????????????????????????

        private void OnValidate()
        {
            // This warning fires in the Editor while the field is still being
            // wired up — it is safe to ignore until the scene is fully configured.
            if (_fillImage == null)
                Debug.LogWarning("[HarmonyHUD] _fillImage is not assigned. " +
                                 "Drag the Fill Image component (not the sprite) into this field.", this);
        }

        #endregion
    }
}
