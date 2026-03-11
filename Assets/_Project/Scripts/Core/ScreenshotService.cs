// ------------------------------------------------------------
//  ScreenshotService.cs  -  _Project.Scripts.Core
//  Reusable screenshot capture with automatic canvas hiding.
// ------------------------------------------------------------

using System;
using System.Collections;
using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Captures a full-screen screenshot, temporarily disabling the
    /// assigned <see cref="Canvas"/> so the UI does not appear in the
    /// image.  The file is saved to <c>Application.persistentDataPath</c>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Core/Screenshot Service")]
    public class ScreenshotService : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Canvas")]
        [Tooltip("Canvas to disable during capture.")]
        [SerializeField] private Canvas _canvasToHide;

        [Header("File")]
        [Tooltip("Prefix added to every screenshot filename.")]
        [SerializeField] private string _filePrefix = "ARmonia";

        #endregion

        #region Events --------------------------------------------

        /// <summary>Raised after a screenshot has been saved (full filename).</summary>
        public event Action<string> OnScreenshotCaptured;

        #endregion

        #region State ---------------------------------------------

        private bool _isCapturing;
        private readonly WaitForEndOfFrame _waitEndOfFrame = new WaitForEndOfFrame();

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Start()
        {
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

        private IEnumerator CaptureRoutine()
        {
            _isCapturing = true;

            if (_canvasToHide != null) _canvasToHide.enabled = false;

            yield return _waitEndOfFrame;

            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName  = $"{_filePrefix}_{timeStamp}.png";
            ScreenCapture.CaptureScreenshot(fileName);

            if (_canvasToHide != null) _canvasToHide.enabled = true;

            _isCapturing = false;
            OnScreenshotCaptured?.Invoke(fileName);
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_canvasToHide == null)
                Debug.LogError("[ScreenshotService] _canvasToHide is not assigned!", this);
        }

        #endregion
    }
}
