// ------------------------------------------------------------
//  ScreenshotToastPanel.cs  -  _Project.Scripts.UI
//  Confirmation toast shown after a screenshot is saved.
// ------------------------------------------------------------

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using _Project.Scripts.Infrastructure;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Displays a confirmation panel after a screenshot is captured.
    /// Shows a thumbnail of the captured image and a dismiss button.
    /// The <c>GameObject</c> starts <b>disabled</b> in the scene and is
    /// activated by <see cref="Show"/>.  Pressing <c>Btn_Accept</c>
    /// fades it out and deactivates it again.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("ARmonia/UI/Screenshot Toast Panel")]
    public class ScreenshotToastPanel : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Animation")]
        [Tooltip("Seconds for the toast to fade in.")]
        [SerializeField] private float _fadeInDuration = 0.3f;

        [Tooltip("Seconds for the toast to fade out after Accept.")]
        [SerializeField] private float _fadeOutDuration = 0.2f;

        [Header("References")]
        [Tooltip("RawImage that previews the captured screenshot.")]
        [SerializeField] private RawImage _previewImage;

        [Tooltip("Dismiss button inside the toast card.")]
        [SerializeField] private Button _acceptButton;

        #endregion

        #region State ---------------------------------------------

        private CanvasGroup     _canvasGroup;
        private Coroutine       _activeRoutine;
        private Texture2D       _currentTexture;
        private bool            _initialized;
        private IUIAudioService _uiAudio;

        #endregion

        #region Public API ----------------------------------------

        /// <summary>
        /// Activates the panel GameObject, assigns the screenshot
        /// thumbnail, and fades the toast in.  The panel takes ownership
        /// of <paramref name="screenshot"/> and destroys it on dismiss.
        /// </summary>
        public void Show(Texture2D screenshot)
        {
            // Activate first so the CanvasGroup can be resolved.
            gameObject.SetActive(true);
            EnsureInitialized();

            ReleaseTexture();
            _currentTexture = screenshot;

            if (_previewImage != null)
                _previewImage.texture = screenshot;

            // Reset CanvasGroup to invisible before starting fade.
            _canvasGroup.alpha          = 0f;
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;

            if (_activeRoutine != null)
                StopCoroutine(_activeRoutine);

            _activeRoutine = StartCoroutine(FadeIn());
            Debug.Log("[ScreenshotToastPanel] Toast shown.");
        }

        /// <summary>
        /// Public dismiss method.  Can be wired to <c>Btn_Accept.OnClick</c>
        /// in the Inspector as an alternative to the automatic listener.
        /// </summary>
        public void Dismiss()
        {
            OnAcceptPressed();
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Start()
        {
            ServiceLocator.TryGet<IUIAudioService>(out _uiAudio);
            ValidateReferences();
        }

        private void OnDestroy()
        {
            if (_acceptButton != null)
                _acceptButton.onClick.RemoveListener(OnAcceptPressed);

            ReleaseTexture();
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Lazy-initialises the CanvasGroup and button listener.
        /// Called on first <see cref="Show"/> instead of <c>Awake</c>
        /// because the GameObject starts disabled in the scene and
        /// <c>Awake</c> would not run until activation.
        /// </summary>
        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (_acceptButton != null)
                _acceptButton.onClick.AddListener(OnAcceptPressed);

            Debug.Log("[ScreenshotToastPanel] Initialized.");
        }

        /// <summary>Called by Btn_Accept.onClick (wired in <see cref="EnsureInitialized"/>).</summary>
        private void OnAcceptPressed()
        {
            _uiAudio?.PlayClick();

            if (_activeRoutine != null)
                StopCoroutine(_activeRoutine);

            _activeRoutine = StartCoroutine(FadeOut());
            Debug.Log("[ScreenshotToastPanel] Toast dismissed.");
        }

        /// <summary>
        /// Coroutine that fades the panel in from alpha 0 to 1 using
        /// <see cref="Mathf.SmoothStep"/> for a polished ease.
        /// </summary>
        private IEnumerator FadeIn()
        {
            _canvasGroup.blocksRaycasts = true;

            float elapsed = 0f;
            while (elapsed < _fadeInDuration)
            {
                elapsed           += Time.deltaTime;
                _canvasGroup.alpha  = Mathf.SmoothStep(0f, 1f, elapsed / _fadeInDuration);
                yield return null;
            }

            _canvasGroup.alpha        = 1f;
            _canvasGroup.interactable = true;
            _activeRoutine            = null;
        }

        /// <summary>
        /// Coroutine that fades the panel out from alpha 1 to 0 using
        /// <see cref="Mathf.SmoothStep"/>, releases the texture, and
        /// deactivates the <c>GameObject</c>.
        /// </summary>
        private IEnumerator FadeOut()
        {
            _canvasGroup.interactable = false;

            float elapsed = 0f;
            while (elapsed < _fadeOutDuration)
            {
                elapsed           += Time.deltaTime;
                _canvasGroup.alpha  = Mathf.SmoothStep(1f, 0f, elapsed / _fadeOutDuration);
                yield return null;
            }

            _canvasGroup.alpha          = 0f;
            _canvasGroup.blocksRaycasts = false;
            _activeRoutine              = null;

            ReleaseTexture();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Clears the preview image and destroys the owned texture to
        /// avoid GPU memory leaks between captures.
        /// </summary>
        private void ReleaseTexture()
        {
            if (_currentTexture == null) return;

            if (_previewImage != null)
                _previewImage.texture = null;

            Destroy(_currentTexture);
            _currentTexture = null;
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_previewImage == null)
                Debug.LogWarning("[ScreenshotToastPanel] _previewImage is not assigned.", this);
            if (_acceptButton == null)
                Debug.LogWarning("[ScreenshotToastPanel] _acceptButton is not assigned.", this);
            if (_uiAudio == null)
                Debug.LogWarning("[ScreenshotToastPanel] _uiAudio is not assigned.", this);
        }

        #endregion
    }
}
