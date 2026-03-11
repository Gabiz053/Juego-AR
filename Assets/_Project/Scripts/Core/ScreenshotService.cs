// ----
// ------------------------------------------------------------
//  ScreenshotService.cs  -  _Project.Scripts.Core
//  Captures a screenshot, saves it to the device gallery via
//  NativeGallery, and provides visual + audio feedback.
// ------------------------------------------------------------

using System;
using System.Collections;
using System.IO;
using UnityEngine;
using _Project.Scripts.UI;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Captures a full-screen screenshot, temporarily disabling the
    /// assigned <see cref="Canvas"/> so the UI does not appear in the
    /// image.  The file is saved to the device gallery via
    /// <c>NativeGallery</c> (Android / iOS) or to
    /// <c>Application.persistentDataPath</c> in the Editor.
    /// Provides a white-flash overlay, a shutter sound, and a
    /// confirmation toast as feedback.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Core/Screenshot Service")]
    public class ScreenshotService : MonoBehaviour
    {
        #region Constants -----------------------------------------

        private const string GALLERY_ALBUM = "ARmonia";

        #endregion

        #region Inspector -----------------------------------------

        [Header("Canvas")]
        [Tooltip("Canvas to disable during capture so UI is not visible.")]
        [SerializeField] private Canvas _canvasToHide;

        [Header("Flash Overlay")]
        [Tooltip("GameObject of the full-screen white panel used as flash feedback.")]
        [SerializeField] private GameObject _flashOverlayObject;

        [Tooltip("Duration in seconds of the flash fade-out.")]
        [SerializeField] private float _flashDuration = 0.25f;

        [Header("Toast")]
        [Tooltip("ScreenshotToastPanel shown after the capture to confirm the save.")]
        [SerializeField] private ScreenshotToastPanel _toastPanel;

        [Header("Audio")]
        [Tooltip("UIAudioService used to play the shutter sound.")]
        [SerializeField] private UIAudioService _uiAudio;

        [Header("File")]
        [Tooltip("Prefix added to every screenshot filename.")]
        [SerializeField] private string _filePrefix = "ARmonia";

        #endregion

        #region Events --------------------------------------------

        /// <summary>Raised after a screenshot has been saved (full path).</summary>
        public event Action<string> OnScreenshotCaptured;

        #endregion

        #region Cached Components / State -------------------------

        private bool          _isCapturing;
        private int           _captureCount;
        private Coroutine     _activeFlash;
        private CanvasGroup   _flashCanvasGroup;
        private HapticService _hapticService;

        private readonly WaitForEndOfFrame _waitEndOfFrame = new WaitForEndOfFrame();

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Start()
        {
            _hapticService = FindAnyObjectByType<HapticService>();
            InitFlashOverlay();
            RequestGalleryPermission();
            ValidateReferences();
        }

        #endregion

        #region Public API ----------------------------------------

        /// <summary>
        /// Begins the capture coroutine.  Multiple rapid calls are debounced.
        /// </summary>
        public void Capture()
        {
            if (_isCapturing) return;
            StartCoroutine(CaptureRoutine());
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Main capture pipeline: hide UI ? read pixels ? restore UI ?
        /// play feedback ? save to gallery ? show toast ? fire event.
        /// </summary>
        private IEnumerator CaptureRoutine()
        {
            _isCapturing = true;

            // Hide UI so the screenshot only shows the AR scene.
            if (_canvasToHide != null) _canvasToHide.enabled = false;

            yield return _waitEndOfFrame;

            // Read screen pixels into a temporary texture.
            Texture2D screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            screenshot.Apply();

            // Restore UI immediately after pixel read.
            if (_canvasToHide != null) _canvasToHide.enabled = true;

            // Feedback: shutter sound + white flash + haptic.
            _uiAudio?.PlayPhoto();
            PlayFlash();
            _hapticService?.VibrateLight();

            // Build a unique filename (timestamp + session counter).
            _captureCount++;
            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName  = $"{_filePrefix}_{timeStamp}_{_captureCount}.png";

            // Save to device gallery (or persistent path in Editor).
            string savedPath = SaveToGallery(screenshot, fileName);

            Debug.Log($"[ScreenshotService] Screenshot saved -- {savedPath}.");

            // Hand the texture to the toast (it will Destroy it on dismiss).
            // Do NOT Destroy here — the toast needs it for the preview image.
            if (_toastPanel != null)
                _toastPanel.Show(screenshot);
            else
                Destroy(screenshot);

            _isCapturing = false;
            OnScreenshotCaptured?.Invoke(savedPath);
        }

        /// <summary>
        /// Saves the texture to the device gallery via NativeGallery.
        /// Falls back to <c>Application.persistentDataPath</c> in the Editor.
        /// </summary>
        private string SaveToGallery(Texture2D texture, string fileName)
        {
#if UNITY_EDITOR
            string path = Path.Combine(Application.persistentDataPath, fileName);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Debug.Log($"[ScreenshotService] Editor fallback -- saved to {path}.");
            return path;
#else
            NativeGallery.SaveImageToGallery(texture, GALLERY_ALBUM, fileName,
                (success, resultPath) =>
                {
                    if (success)
                        Debug.Log($"[ScreenshotService] Gallery callback -- saved to {resultPath}.");
                    else
                        Debug.LogWarning("[ScreenshotService] Gallery callback -- save failed.");
                });

            // NativeGallery handles the actual gallery path; return a best-effort path for the event.
            return Path.Combine(Application.persistentDataPath, fileName);
#endif
        }

        /// <summary>
        /// Activates the flash overlay GameObject, starts the fade-out
        /// coroutine, and deactivates it once finished.
        /// </summary>
        private void PlayFlash()
        {
            if (_flashOverlayObject == null || _flashCanvasGroup == null) return;

            if (_activeFlash != null)
                StopCoroutine(_activeFlash);

            _activeFlash = StartCoroutine(FlashRoutine());
        }

        /// <summary>
        /// Coroutine that enables the flash overlay, fades alpha from 1 to 0
        /// over <see cref="_flashDuration"/> seconds, then disables the GameObject.
        /// </summary>
        private IEnumerator FlashRoutine()
        {
            _flashOverlayObject.SetActive(true);
            _flashCanvasGroup.alpha = 1f;

            float elapsed = 0f;
            while (elapsed < _flashDuration)
            {
                elapsed += Time.deltaTime;
                _flashCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / _flashDuration);
                yield return null;
            }

            _flashCanvasGroup.alpha = 0f;
            _flashOverlayObject.SetActive(false);
            _activeFlash = null;
        }

        /// <summary>
        /// Caches the <see cref="CanvasGroup"/> from the flash overlay
        /// GameObject and ensures it starts hidden and disabled.
        /// </summary>
        private void InitFlashOverlay()
        {
            if (_flashOverlayObject == null) return;

            _flashCanvasGroup = _flashOverlayObject.GetComponent<CanvasGroup>();
            if (_flashCanvasGroup == null)
                _flashCanvasGroup = _flashOverlayObject.AddComponent<CanvasGroup>();

            _flashCanvasGroup.alpha          = 0f;
            _flashCanvasGroup.interactable   = false;
            _flashCanvasGroup.blocksRaycasts = false;

            // Disable the GameObject so it is invisible and costs nothing at runtime.
            _flashOverlayObject.SetActive(false);
        }

        /// <summary>
        /// Requests gallery write permission at startup so the first
        /// capture does not stall on a system dialog (Android 13+ / iOS).
        /// </summary>
        private static void RequestGalleryPermission()
        {
#if !UNITY_EDITOR
            bool hasPermission = NativeGallery.CheckPermission(
                NativeGallery.PermissionType.Write, NativeGallery.MediaType.Image);

            if (!hasPermission)
            {
                NativeGallery.RequestPermissionAsync(
                    (permission) => Debug.Log($"[ScreenshotService] Gallery permission result -- {permission}."),
                    NativeGallery.PermissionType.Write, NativeGallery.MediaType.Image);

                Debug.Log("[ScreenshotService] Requested gallery write permission.");
            }
#endif
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_canvasToHide == null)
                Debug.LogError("[ScreenshotService] _canvasToHide is not assigned!", this);
            if (_flashOverlayObject == null)
                Debug.LogWarning("[ScreenshotService] _flashOverlayObject is not assigned -- no flash feedback.", this);
            if (_toastPanel == null)
                Debug.LogWarning("[ScreenshotService] _toastPanel is not assigned -- no confirmation toast.", this);
            if (_uiAudio == null)
                Debug.LogWarning("[ScreenshotService] _uiAudio is not assigned -- no shutter sound.", this);
        }

        #endregion
    }
}
