// ──────────────────────────────────────────────
//  ScreenshotService.cs  ·  _Project.Scripts.Core
//  Reusable screenshot capture with automatic canvas hiding.
// ──────────────────────────────────────────────

using System;
using System.Collections;
using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Captures a full-screen screenshot, temporarily disabling the
    /// assigned <see cref="Canvas"/> so the UI does not appear in the image.
    /// The file is saved to <c>Application.persistentDataPath</c> (mobile)
    /// or the project root (Editor) with a timestamped name.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Core/Screenshot Service")]
    public class ScreenshotService : MonoBehaviour
    {
        #region Inspector ─────────────────────────────────────

        [Tooltip("Canvas to disable during capture so the UI is not visible in the photo.")]
        [SerializeField] private Canvas _canvasToHide;

        [Tooltip("Prefix added to every screenshot filename (e.g. 'ARmonia' → ARmonia_20260301_143022.png).")]
        [SerializeField] private string _filePrefix = "ARmonia";

        #endregion

        #region Events ────────────────────────────────────────

        /// <summary>
        /// Raised immediately after a screenshot has been saved.
        /// The <see langword="string"/> parameter is the full filename.
        /// </summary>
        public event Action<string> OnScreenshotCaptured;

        #endregion

        #region Private State ─────────────────────────────────

        /// <summary>Prevents multiple captures from overlapping.</summary>
        private bool _isCapturing;

        /// <summary>Cached yield instruction — avoids GC allocation per capture.</summary>
        private readonly WaitForEndOfFrame _waitEndOfFrame = new WaitForEndOfFrame();

        #endregion

        #region Unity Lifecycle ────────────────────────────────

        private void Start()
        {
            ValidateReferences();
            Debug.Log("[ScreenshotService] Initialized.");
        }

        #endregion

        #region Public API ────────────────────────────────────

        /// <summary>
        /// Begins the capture coroutine. Safe to call from a UI button.
        /// Multiple rapid calls are debounced — only the first is honoured.
        /// </summary>
        public void Capture()
        {
            if (_isCapturing)
            {
                Debug.LogWarning("[ScreenshotService] Capture already in progress — ignoring duplicate call.");
                return;
            }

            StartCoroutine(CaptureRoutine());
        }

        #endregion

        #region Internals ─────────────────────────────────────

        /// <summary>
        /// Hides the canvas, waits for end-of-frame, captures the
        /// screenshot, then re-enables the canvas.
        /// </summary>
        private IEnumerator CaptureRoutine()
        {
            _isCapturing = true;
            Debug.Log("[ScreenshotService] Capture started — hiding canvas.");

            // 1. Hide the UI so it doesn't appear in the photo.
            if (_canvasToHide != null) _canvasToHide.enabled = false;

            // 2. Wait for the rendering pipeline to finish the frame.
            yield return _waitEndOfFrame;

            // 3. Build a timestamped filename and capture.
            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName  = $"{_filePrefix}_{timeStamp}.png";
            ScreenCapture.CaptureScreenshot(fileName);

            Debug.Log($"[ScreenshotService] Screenshot saved: {fileName}");

            // 4. Re-enable the UI.
            if (_canvasToHide != null) _canvasToHide.enabled = true;

            _isCapturing = false;

            // 5. Notify listeners.
            OnScreenshotCaptured?.Invoke(fileName);
        }

        #endregion

        #region Validation ────────────────────────────────────

        /// <summary>
        /// Logs errors for any missing Inspector references at startup.
        /// </summary>
        private void ValidateReferences()
        {
            if (_canvasToHide == null)
                Debug.LogError("[ScreenshotService] _canvasToHide is not assigned!", this);

            if (string.IsNullOrWhiteSpace(_filePrefix))
                Debug.LogWarning("[ScreenshotService] _filePrefix is empty — filenames will start with '_'.", this);
        }

        #endregion
    }
}
