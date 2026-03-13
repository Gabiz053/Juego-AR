// ------------------------------------------------------------
//  HandTrackingService.cs  -  _Project.Scripts.Title
//  Captures front-camera frames, runs MediaPipe hand landmark
//  detection, and broadcasts the index-fingertip screen position.
// ------------------------------------------------------------

using System;
using System.Collections;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Mediapipe;
using Mediapipe.Unity;
using Mediapipe.Unity.Experimental;
using Mediapipe.Unity.Sample;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Components.Containers;

namespace _Project.Scripts.Title
{
    /// <summary>
    /// Initialises a MediaPipe <see cref="HandLandmarker"/> with GPU delegate
    /// (falls back to CPU), acquires CPU images from the front camera via
    /// <see cref="ARCameraManager.frameReceived"/>, and fires events with
    /// the index-fingertip position in screen-pixel coordinates.<br/>
    /// Designed to run alongside <see cref="CreeperFaceFilter"/> on the same
    /// <c>XR Origin (Front Camera)</c> GameObject.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Title/Hand Tracking Service")]
    public class HandTrackingService : MonoBehaviour
    {
        #region Constants -----------------------------------------

        /// <summary>MediaPipe landmark index for the tip of the index finger.</summary>
        private const int LANDMARK_INDEX_TIP = 8;

        /// <summary>Exponential smoothing factor (lower = smoother, higher = responsive).</summary>
        private const float SMOOTHING_FACTOR = 0.7f;

        /// <summary>Model file bundled by the MediaPipe Unity Plugin.</summary>
        private const string MODEL_PATH = "hand_landmarker.bytes";

        /// <summary>Number of consecutive empty results before OnHandLost fires.</summary>
        private const int MAX_CONSECUTIVE_MISSES = 5;

        #endregion

        #region Inspector -----------------------------------------

        [Header("Dependencies")]
        [Tooltip("ARCameraManager on the Main Camera — provides camera frames.")]
        [SerializeField] private ARCameraManager _cameraManager;

        [Header("MediaPipe Settings")]
        [Tooltip("Minimum confidence for initial hand detection (0-1).")]
        [SerializeField] [Range(0.1f, 1f)] private float _minDetectionConfidence = 0.5f;

        [Tooltip("Minimum confidence for hand presence between frames (0-1).")]
        [SerializeField] [Range(0.1f, 1f)] private float _minPresenceConfidence = 0.5f;

        [Tooltip("Minimum confidence for landmark tracking (0-1).")]
        [SerializeField] [Range(0.1f, 1f)] private float _minTrackingConfidence = 0.5f;

        #endregion

        #region Events --------------------------------------------

        /// <summary>Fired every time a valid fingertip position is computed (screen pixels).</summary>
        public event Action<Vector2> OnFingertipScreenPosition;

        /// <summary>Fired once when a hand first becomes visible.</summary>
        public event Action OnHandDetected;

        /// <summary>Fired once when the hand is lost.</summary>
        public event Action OnHandLost;

        #endregion

        #region State ---------------------------------------------

        private HandLandmarker _handLandmarker;
        private HandLandmarkerResult _result;
        private TextureFramePool _textureFramePool;
        private Texture2D _cameraTexture;

        private bool _isInitialized;
        private bool _isTracking;
        private Vector2 _smoothedScreenPos;
        private int _consecutiveMisses;
        private bool _isGlogInitialized;
        private bool _gpuInitialized;
        private int _imageRotationDegrees;
        private bool _loggedFirstFrame;

        #endregion

        #region Public API ----------------------------------------

        /// <summary>True while a hand is actively tracked.</summary>
        public bool IsTracking => _isTracking;

        /// <summary>Last smoothed fingertip position in screen pixels.</summary>
        public Vector2 CurrentScreenPosition => _smoothedScreenPos;

        #endregion

        #region Unity Lifecycle -----------------------------------

        private IEnumerator Start()
        {
            ValidateReferences();
            yield return InitializeMediaPipe();

            if (_isInitialized && _cameraManager != null)
            {
                _cameraManager.frameReceived += OnCameraFrameReceived;
                Debug.Log("[HandTrackingService] Subscribed to ARCameraManager.frameReceived.");
            }
        }

        private void OnDisable()
        {
            if (_cameraManager != null)
                _cameraManager.frameReceived -= OnCameraFrameReceived;

            _handLandmarker?.Close();
            _handLandmarker = null;

            _textureFramePool?.Dispose();
            _textureFramePool = null;

            if (_cameraTexture != null)
                Destroy(_cameraTexture);

            if (_gpuInitialized)
            {
                GpuManager.Shutdown();
                _gpuInitialized = false;
            }

            if (_isGlogInitialized)
            {
                Glog.Shutdown();
                _isGlogInitialized = false;
            }

            Debug.Log("[HandTrackingService] Disabled and cleaned up.");
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Initialises Glog, the asset loader, the GPU manager, prepares the
        /// hand landmark model, and creates the <see cref="HandLandmarker"/>
        /// with GPU delegate (falls back to CPU if GPU is unavailable).
        /// </summary>
        private IEnumerator InitializeMediaPipe()
        {
            Debug.Log("[HandTrackingService] Starting MediaPipe initialization...");

            // --- MediaPipe runtime init ---
            try
            {
                Protobuf.SetLogHandler(Protobuf.DefaultLogHandler);
                Glog.Initialize("MediaPipeUnityPlugin");
                _isGlogInitialized = true;
                Debug.Log("[HandTrackingService] Glog initialized.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HandTrackingService] Glog init failed -- {ex.Message}");
                yield break;
            }

            // --- Asset loader ---
            try
            {
#if UNITY_EDITOR
                AssetLoader.Provide(new LocalResourceManager());
                Debug.Log("[HandTrackingService] AssetLoader: LocalResourceManager (Editor).");
#else
                AssetLoader.Provide(new StreamingAssetsResourceManager());
                Debug.Log("[HandTrackingService] AssetLoader: StreamingAssetsResourceManager (Device).");
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HandTrackingService] AssetLoader init failed -- {ex.Message}");
                yield break;
            }

            // --- GPU manager (optional, falls back to CPU) ---
            Debug.Log("[HandTrackingService] Initializing GpuManager...");
            yield return GpuManager.Initialize();

            _gpuInitialized = GpuManager.IsInitialized;
            if (_gpuInitialized)
                Debug.Log("[HandTrackingService] GpuManager initialized -- GPU delegate available.");
            else
                Debug.LogWarning("[HandTrackingService] GpuManager failed -- falling back to CPU delegate.");

            // --- Prepare model asset ---
            Debug.Log($"[HandTrackingService] Preparing model asset: {MODEL_PATH}...");
            yield return AssetLoader.PrepareAssetAsync(MODEL_PATH);
            Debug.Log("[HandTrackingService] Model asset prepared.");

            // --- Create the HandLandmarker (IMAGE mode, GPU preferred) ---
            try
            {
                var delegateType = _gpuInitialized
                    ? Mediapipe.Tasks.Core.BaseOptions.Delegate.GPU
                    : Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU;

                var baseOptions = new Mediapipe.Tasks.Core.BaseOptions(
                    delegateType,
                    modelAssetPath: MODEL_PATH);

                var options = new HandLandmarkerOptions(
                    baseOptions,
                    runningMode: Mediapipe.Tasks.Vision.Core.RunningMode.IMAGE,
                    numHands: 1,
                    minHandDetectionConfidence: _minDetectionConfidence,
                    minHandPresenceConfidence: _minPresenceConfidence,
                    minTrackingConfidence: _minTrackingConfidence);

                _handLandmarker = HandLandmarker.CreateFromOptions(options, GpuManager.GpuResources);
                _result = HandLandmarkerResult.Alloc(1);

                _isInitialized = true;
                Debug.Log($"[HandTrackingService] Initialized -- {delegateType} delegate, IMAGE mode.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HandTrackingService] HandLandmarker creation failed -- {ex.Message}");
            }
        }

        /// <summary>
        /// Called every AR camera frame. Acquires a CPU image, converts it to
        /// RGBA32, builds a MediaPipe Image, runs hand landmark detection,
        /// and processes the results.
        /// </summary>
        private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
        {
            if (!_isInitialized || _handLandmarker == null) return;

            if (!_cameraManager.TryAcquireLatestCpuImage(out XRCpuImage cpuImage))
                return;

            try
            {
                ProcessFrame(cpuImage);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HandTrackingService] ProcessFrame error -- {ex.Message}");
            }
            finally
            {
                cpuImage.Dispose();
            }
        }

        /// <summary>
        /// Converts the <see cref="XRCpuImage"/> to RGBA32, copies it into a
        /// <see cref="Texture2D"/>, feeds through a <see cref="TextureFramePool"/>
        /// to build a MediaPipe Image, and calls TryDetect.
        /// </summary>
        private void ProcessFrame(XRCpuImage cpuImage)
        {
            // --- Log first frame info for debugging ---
            if (!_loggedFirstFrame)
            {
                _loggedFirstFrame = true;
                _imageRotationDegrees = ComputeImageRotation(cpuImage.width, cpuImage.height);
                Debug.Log($"[HandTrackingService] First frame -- image {cpuImage.width}x{cpuImage.height}" +
                          $", screen {UnityEngine.Screen.width}x{UnityEngine.Screen.height}" +
                          $", rotation {_imageRotationDegrees} deg, orientation {UnityEngine.Screen.orientation}.");
            }

            // --- Convert XRCpuImage to RGBA32 ---
            var conversionParams = new XRCpuImage.ConversionParams
            {
                inputRect        = new RectInt(0, 0, cpuImage.width, cpuImage.height),
                outputDimensions = new Vector2Int(cpuImage.width, cpuImage.height),
                outputFormat     = TextureFormat.RGBA32,
                transformation   = XRCpuImage.Transformation.None
            };

            int size = cpuImage.GetConvertedDataSize(conversionParams);
            var buffer = new NativeArray<byte>(size, Allocator.Temp);
            cpuImage.Convert(conversionParams, buffer);

            // --- Load into a reusable Texture2D ---
            EnsureCameraTexture(cpuImage.width, cpuImage.height);
            _cameraTexture.LoadRawTextureData(buffer);
            _cameraTexture.Apply();
            buffer.Dispose();

            // --- Build a MediaPipe Image via TextureFrame ---
            if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
                return;

            // Front camera: flip horizontally for selfie mirror correction
            textureFrame.ReadTextureOnCPU(_cameraTexture, flipHorizontally: true, flipVertically: false);
            var image = textureFrame.BuildCPUImage();
            textureFrame.Release();

            // --- Run inference with rotation matching device orientation ---
            var imageProcessingOptions = new Mediapipe.Tasks.Vision.Core.ImageProcessingOptions(
                rotationDegrees: _imageRotationDegrees);

            if (_handLandmarker.TryDetect(image, imageProcessingOptions, ref _result))
                ProcessLandmarks();
            else
                HandleNoHand();
        }

        /// <summary>
        /// Determines the rotation degrees needed for MediaPipe based on the
        /// camera sensor image dimensions vs the current screen orientation.<br/>
        /// On Android in portrait mode the front camera sensor is typically
        /// landscape, requiring a 270° rotation for MediaPipe to see hands upright.
        /// </summary>
        private int ComputeImageRotation(int imageWidth, int imageHeight)
        {
            bool imageIsLandscape = imageWidth > imageHeight;
            bool screenIsPortrait = UnityEngine.Screen.orientation == ScreenOrientation.Portrait
                                 || UnityEngine.Screen.orientation == ScreenOrientation.PortraitUpsideDown;

            if (imageIsLandscape && screenIsPortrait)
                return 270;

            if (!imageIsLandscape && !screenIsPortrait)
                return 90;

            return 0;
        }

        /// <summary>
        /// Creates/resizes the Texture2D and TextureFramePool to match the
        /// camera resolution.
        /// </summary>
        private void EnsureCameraTexture(int width, int height)
        {
            if (_cameraTexture != null && _cameraTexture.width == width && _cameraTexture.height == height)
                return;

            if (_cameraTexture != null)
                Destroy(_cameraTexture);

            _cameraTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            _textureFramePool?.Dispose();
            _textureFramePool = new TextureFramePool(width, height, TextureFormat.RGBA32, 2);

            Debug.Log($"[HandTrackingService] Camera texture created -- {width}x{height}.");
        }

        /// <summary>
        /// Extracts the index fingertip landmark (index 8), converts to screen
        /// coordinates, applies smoothing, and fires events.
        /// </summary>
        private void ProcessLandmarks()
        {
            if (_result.handLandmarks == null || _result.handLandmarks.Count == 0)
            {
                HandleNoHand();
                return;
            }

            NormalizedLandmarks hand = _result.handLandmarks[0];
            if (hand.landmarks == null || hand.landmarks.Count <= LANDMARK_INDEX_TIP)
            {
                HandleNoHand();
                return;
            }

            _consecutiveMisses = 0;
            Mediapipe.Tasks.Components.Containers.NormalizedLandmark tip = hand.landmarks[LANDMARK_INDEX_TIP];
            Vector2 rawScreen = ConvertNormalizedToScreen(tip.x, tip.y);

            _smoothedScreenPos = _isTracking
                ? Vector2.Lerp(_smoothedScreenPos, rawScreen, SMOOTHING_FACTOR)
                : rawScreen;

            if (!_isTracking)
            {
                _isTracking = true;
                Debug.Log($"[HandTrackingService] Hand detected -- fingertip at ({tip.x:F2}, {tip.y:F2}).");
                OnHandDetected?.Invoke();
            }

            OnFingertipScreenPosition?.Invoke(_smoothedScreenPos);
        }

        /// <summary>
        /// Handles frames where no hand is detected. After a few consecutive
        /// misses, fires <see cref="OnHandLost"/>.
        /// </summary>
        private void HandleNoHand()
        {
            if (!_isTracking) return;

            _consecutiveMisses++;
            if (_consecutiveMisses < MAX_CONSECUTIVE_MISSES) return;

            _isTracking = false;
            Debug.Log("[HandTrackingService] Hand lost.");
            OnHandLost?.Invoke();
        }

        /// <summary>
        /// Converts MediaPipe normalised coordinates (0-1) to Unity screen
        /// pixels. Landmarks are returned in the original (pre-rotation) image
        /// space, so we must apply the inverse rotation to map them to the
        /// portrait screen coordinate system.<br/>
        /// Unity screen origin is bottom-left; MediaPipe image origin is top-left.
        /// </summary>
        private Vector2 ConvertNormalizedToScreen(float normX, float normY)
        {
            float screenX, screenY;

            switch (_imageRotationDegrees)
            {
                case 90:
                case 270:
                    // Landscape sensor → portrait screen (front camera with flipHorizontally)
                    screenX = (1f - normY) * UnityEngine.Screen.width;
                    screenY = (1f - normX) * UnityEngine.Screen.height;
                    break;
                case 180:
                    screenX = (1f - normX) * UnityEngine.Screen.width;
                    screenY = normY * UnityEngine.Screen.height;
                    break;
                default: // 0
                    screenX = normX * UnityEngine.Screen.width;
                    screenY = (1f - normY) * UnityEngine.Screen.height;
                    break;
            }

            return new Vector2(screenX, screenY);
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_cameraManager == null)
                Debug.LogError("[HandTrackingService] _cameraManager is not assigned!", this);
        }

        #endregion
    }
}
