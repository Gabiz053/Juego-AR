// ------------------------------------------------------------
//  WorldModeBootstrapper.cs  -  _Project.Scripts.AR
//  Reads WorldModeContext at startup and configures the voxel
//  world (scale, grid, anchor strategy) accordingly.
// ------------------------------------------------------------

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using _Project.Scripts.Core;

namespace _Project.Scripts.AR
{
    /// <summary>
    /// Entry point for the game scene.  Reads
    /// <see cref="WorldModeContext.Selected"/>, finds the matching
    /// <see cref="WorldModeSO"/> and applies its configuration
    /// (scale, anchor manager, etc.).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/AR/World Mode Bootstrapper")]
    public class WorldModeBootstrapper : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Mode Configs")]
        [Tooltip("One WorldModeSO per mode. Matched by WorldModeSO.Mode.")]
        [SerializeField] private WorldModeSO[] _modeConfigs;

        [Header("Dev Override")]
        [Tooltip("Inspector override while the title screen is not yet implemented.")]
        [SerializeField] private WorldMode _devOverrideMode = WorldMode.Normal;

        [Header("World References")]
        [Tooltip("WorldContainer whose localScale is set by this bootstrapper.")]
        [SerializeField] private Transform _worldContainer;

        [Tooltip("ARWorldManager that handles anchor creation.")]
        [SerializeField] private ARWorldManager _arWorldManager;

        [Tooltip("GridManager -- visual scale is handled by WorldContainer.localScale.")]
        [SerializeField] private GridManager _gridManager;

        [Header("AR Managers")]
        [Tooltip("AR Plane Manager -- active in Normal and Real modes.")]
        [SerializeField] private ARPlaneManager _arPlaneManager;

        [Tooltip("AR Tracked Image Manager -- active in Bonsai mode only.")]
        [SerializeField] private ARTrackedImageManager _arTrackedImageManager;

        #endregion

        #region State ---------------------------------------------

        private WorldModeSO _activeConfig;
        private Camera      _mainCamera;
        private bool        _anchored;

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            WorldModeContext.Selected = _devOverrideMode;
            _mainCamera = Camera.main;

            _activeConfig = FindConfig(WorldModeContext.Selected);
            if (_activeConfig == null)
            {
                Debug.LogError($"[WorldModeBootstrapper] No WorldModeSO for mode '{WorldModeContext.Selected}'.", this);
                return;
            }

            ApplyWorldScale();
            ConfigureARManagers();
            Debug.Log($"[WorldModeBootstrapper] Mode: {_activeConfig.DisplayName}, scale: {_activeConfig.WorldContainerScale}, anchor: {_activeConfig.AnchorType}.");
        }

        private void Start()
        {
            ValidateReferences();
        }

        private void OnDestroy()
        {
            UnsubscribeFromManagers();
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>Applies <see cref="WorldModeSO.WorldContainerScale"/> to <c>_worldContainer</c>.</summary>
        private void ApplyWorldScale()
        {
            if (_worldContainer == null) return;
            float s = _activeConfig.WorldContainerScale;
            _worldContainer.localScale = new Vector3(s, s, s);
        }

        /// <summary>Routes to plane or tracked-image mode based on <see cref="AnchorType"/>.</summary>
        private void ConfigureARManagers()
        {
            switch (_activeConfig.AnchorType)
            {
                case AnchorType.TrackedImage:
                    EnableTrackedImageMode();
                    break;
                case AnchorType.ARPlane:
                default:
                    EnablePlaneMode();
                    break;
            }
        }

        // -- Plane mode (Normal / Real) --------------------------

        /// <summary>Enables <see cref="ARPlaneManager"/> and subscribes to new-plane events.</summary>
        private void EnablePlaneMode()
        {
            if (_arPlaneManager != null)
            {
                _arPlaneManager.enabled = true;
                _arPlaneManager.trackablesChanged.AddListener(OnTrackablesChangedPlane);
            }

            if (_arTrackedImageManager != null)
                _arTrackedImageManager.enabled = false;
        }

        /// <summary>Auto-unsubscribes once the first plane is detected.</summary>
        private void OnTrackablesChangedPlane(ARTrackablesChangedEventArgs<ARPlane> args)
        {
            if (args.added.Count > 0)
                _arPlaneManager.trackablesChanged.RemoveListener(OnTrackablesChangedPlane);
        }

        // -- Tracked image mode (Bonsai) -------------------------

        /// <summary>Enables <see cref="ARTrackedImageManager"/> and subscribes to image events.</summary>
        private void EnableTrackedImageMode()
        {
            if (_arTrackedImageManager != null)
            {
                if (_activeConfig.ImageLibrary != null)
                    _arTrackedImageManager.referenceLibrary = _activeConfig.ImageLibrary;
                else
                    Debug.LogWarning("[WorldModeBootstrapper] Bonsai mode but ImageLibrary is null!", this);

                _arTrackedImageManager.enabled = true;
                _arTrackedImageManager.trackablesChanged.AddListener(OnTrackablesChangedImage);
            }
            else
            {
                Debug.LogError("[WorldModeBootstrapper] _arTrackedImageManager required for Bonsai!", this);
            }

            if (_arPlaneManager != null)
                _arPlaneManager.enabled = false;
        }

        /// <summary>Listens for added or updated tracked images and anchors on the first valid one.</summary>
        private void OnTrackablesChangedImage(ARTrackablesChangedEventArgs<ARTrackedImage> args)
        {
            if (_anchored) return;

            foreach (ARTrackedImage img in args.added)
            {
                AnchorToImage(img);
                return;
            }

            foreach (ARTrackedImage img in args.updated)
            {
                if (img.trackingState == TrackingState.Tracking)
                {
                    AnchorToImage(img);
                    return;
                }
            }
        }

        /// <summary>Anchors the world to the tracked image pose and unsubscribes from further events.</summary>
        private void AnchorToImage(ARTrackedImage img)
        {
            if (_anchored || _arWorldManager == null) return;

            Pose imagePose = new Pose(img.transform.position, img.transform.rotation);
            _arWorldManager.AnchorWorld(imagePose, _mainCamera.transform);
            _anchored = true;

            _arTrackedImageManager.trackablesChanged.RemoveListener(OnTrackablesChangedImage);
        }

        /// <summary>Searches <c>_modeConfigs</c> for the <see cref="WorldModeSO"/> matching <paramref name="mode"/>.</summary>
        private WorldModeSO FindConfig(WorldMode mode)
        {
            if (_modeConfigs == null) return null;
            foreach (WorldModeSO cfg in _modeConfigs)
            {
                if (cfg != null && cfg.Mode == mode)
                    return cfg;
            }
            return null;
        }

        /// <summary>Removes all event listeners from AR managers to prevent leaks.</summary>
        private void UnsubscribeFromManagers()
        {
            if (_arPlaneManager != null)
                _arPlaneManager.trackablesChanged.RemoveListener(OnTrackablesChangedPlane);
            if (_arTrackedImageManager != null)
                _arTrackedImageManager.trackablesChanged.RemoveListener(OnTrackablesChangedImage);
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_modeConfigs == null || _modeConfigs.Length == 0)
                Debug.LogError("[WorldModeBootstrapper] _modeConfigs is empty!", this);
            if (_worldContainer == null)
                Debug.LogError("[WorldModeBootstrapper] _worldContainer is not assigned!", this);
            if (_arWorldManager == null)
                Debug.LogError("[WorldModeBootstrapper] _arWorldManager is not assigned!", this);
            if (_gridManager == null)
                Debug.LogWarning("[WorldModeBootstrapper] _gridManager is not assigned!", this);
            if (_mainCamera == null)
                Debug.LogError("[WorldModeBootstrapper] Camera.main not found!", this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_modeConfigs == null || _modeConfigs.Length == 0)
                Debug.LogWarning("[WorldModeBootstrapper] _modeConfigs is empty!", this);
        }
#endif

        #endregion
    }
}
