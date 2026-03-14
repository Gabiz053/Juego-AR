// ------------------------------------------------------------
//  HandCursorUI.cs  -  _Project.Scripts.Title
//  Positions a UI cursor following the fingertip position.
// ------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Title
{
    /// <summary>
    /// Moves a cursor dot on the <c>TitleCanvas</c> to follow the index
    /// fingertip reported by <see cref="HandTrackingService"/>.<br/>
    /// Fades in when a hand is detected and fades out when lost.
    /// When dwelling on a button the dot hides and the radial progress
    /// ring takes over as the only visible cursor element.<br/>
    /// Exposes <see cref="SetDwellProgress"/> for <see cref="DwellSelector"/>
    /// to drive the radial fill overlay.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Title/Hand Cursor UI")]
    public class HandCursorUI : MonoBehaviour
    {
        #region Constants -----------------------------------------

        /// <summary>Speed of the alpha fade in/out (units per second).</summary>
        private const float FADE_SPEED = 5f;

        /// <summary>Progress threshold below which the dwell ring is hidden.</summary>
        private const float DWELL_RING_SHOW_THRESHOLD = 0.01f;

        /// <summary>Scale applied to the cursor dot when hovering over a button.</summary>
        private const float HOVER_DOT_SCALE = 1.35f;

        /// <summary>Speed at which the dot scale lerps to target (units/s).</summary>
        private const float DOT_SCALE_SPEED = 1.5f;

        #endregion

        #region Inspector -----------------------------------------

        [Header("Dependencies")]
        [Tooltip("HandTrackingService that provides fingertip positions.")]
        [SerializeField] private HandTrackingService _handTracking;

        [Tooltip("RectTransform of the root canvas (TitleCanvas) for coordinate conversion.")]
        [SerializeField] private RectTransform _canvasRect;

        [Header("Cursor Visuals")]
        [Tooltip("Root RectTransform of the cursor group (HandCursor object).")]
        [SerializeField] private RectTransform _cursorRoot;

        [Tooltip("CanvasGroup on the cursor root for fade in/out.")]
        [SerializeField] private CanvasGroup _cursorCanvasGroup;

        [Tooltip("Image of the cursor dot (Img_CursorDot).")]
        [SerializeField] private Image _cursorDotImage;

        [Tooltip("Image with Fill Method Radial360 showing dwell progress.")]
        [SerializeField] private Image _dwellProgressImage;

        #endregion

        #region State ---------------------------------------------

        private float _targetAlpha;
        private RectTransform _cursorDotRect;
        private float _currentDotScale = 1f;
        private float _targetDotScale = 1f;

        #endregion

        #region Public API ----------------------------------------

        /// <summary>
        /// Sets the dwell progress ring fill amount (0 = empty, 1 = full).
        /// When dwelling, the dot hides and only the ring is visible.
        /// When not dwelling, the dot shows and the ring hides.
        /// Called by <see cref="DwellSelector"/> each frame.
        /// </summary>
        public void SetDwellProgress(float normalized01)
        {
            float clamped = Mathf.Clamp01(normalized01);
            bool isDwelling = clamped > DWELL_RING_SHOW_THRESHOLD;

            if (_dwellProgressImage != null)
            {
                _dwellProgressImage.fillAmount = clamped;
                _dwellProgressImage.enabled = isDwelling;
            }

            if (_cursorDotImage != null)
                _cursorDotImage.enabled = !isDwelling;
        }

        /// <summary>
        /// Tells the cursor whether it is over a button so the dot
        /// can scale up slightly as visual feedback.
        /// Called by <see cref="DwellSelector"/>.
        /// </summary>
        public void SetHovering(bool isHovering)
        {
            _targetDotScale = isHovering ? HOVER_DOT_SCALE : 1f;
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void OnEnable()
        {
            if (_handTracking != null)
            {
                _handTracking.OnFingertipScreenPosition += HandleFingertipUpdate;
                _handTracking.OnHandDetected            += HandleHandDetected;
                _handTracking.OnHandLost                += HandleHandLost;
            }
        }

        private void OnDisable()
        {
            if (_handTracking != null)
            {
                _handTracking.OnFingertipScreenPosition -= HandleFingertipUpdate;
                _handTracking.OnHandDetected            -= HandleHandDetected;
                _handTracking.OnHandLost                -= HandleHandLost;
            }
        }

        private void Start()
        {
            ValidateReferences();

            if (_cursorDotImage != null)
                _cursorDotRect = _cursorDotImage.rectTransform;

            _targetAlpha = 0f;
            if (_cursorCanvasGroup != null)
            {
                _cursorCanvasGroup.alpha          = 0f;
                _cursorCanvasGroup.blocksRaycasts = false;
                _cursorCanvasGroup.interactable   = false;
            }

            SetDwellProgress(0f);
        }

        private void Update()
        {
            if (_cursorCanvasGroup == null) return;

            _cursorCanvasGroup.alpha = Mathf.MoveTowards(
                _cursorCanvasGroup.alpha, _targetAlpha, FADE_SPEED * Time.deltaTime);

            if (_cursorDotRect != null)
            {
                _currentDotScale = Mathf.MoveTowards(
                    _currentDotScale, _targetDotScale, DOT_SCALE_SPEED * Time.deltaTime);
                _cursorDotRect.localScale = new Vector3(_currentDotScale, _currentDotScale, 1f);
            }
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Converts the screen-pixel position to canvas local coordinates
        /// and moves the cursor rect.
        /// </summary>
        private void HandleFingertipUpdate(Vector2 screenPos)
        {
            if (_canvasRect == null || _cursorRoot == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPos, null, out Vector2 localPoint);

            _cursorRoot.anchoredPosition = localPoint;
        }

        /// <summary>Fades the cursor in when a hand is detected.</summary>
        private void HandleHandDetected()
        {
            _targetAlpha = 1f;
            Debug.Log("[HandCursorUI] Cursor visible -- hand detected.");
        }

        /// <summary>Fades the cursor out and resets state when the hand is lost.</summary>
        private void HandleHandLost()
        {
            _targetAlpha = 0f;
            SetDwellProgress(0f);
            SetHovering(false);
            Debug.Log("[HandCursorUI] Cursor hidden -- hand lost.");
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_handTracking == null)
                Debug.LogWarning("[HandCursorUI] _handTracking is not assigned.", this);
            if (_canvasRect == null)
                Debug.LogWarning("[HandCursorUI] _canvasRect is not assigned.", this);
            if (_cursorRoot == null)
                Debug.LogWarning("[HandCursorUI] _cursorRoot is not assigned.", this);
            if (_cursorCanvasGroup == null)
                Debug.LogWarning("[HandCursorUI] _cursorCanvasGroup is not assigned.", this);
            if (_cursorDotImage == null)
                Debug.LogWarning("[HandCursorUI] _cursorDotImage is not assigned.", this);
            if (_dwellProgressImage == null)
                Debug.LogWarning("[HandCursorUI] _dwellProgressImage is not assigned.", this);
        }

        #endregion
    }
}
