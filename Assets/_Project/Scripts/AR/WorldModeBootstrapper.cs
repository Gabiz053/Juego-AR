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
        }

        private void OnDestroy()
        {
            UnsubscribeFromManagers();
        }

        #endregion

        #region Internals -----------------------------------------

        private void ApplyWorldScale()
        {
            if (_worldContainer == null) return;
            float s = _activeConfig.WorldContainerScale;
            _worldContainer.localScale = new Vector3(s, s, s);
        }

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

        private void OnTrackablesChangedPlane(ARTrackablesChangedEventArgs<ARPlane> args)
        {
            if (args.added.Count > 0)
                _arPlaneManager.trackablesChanged.RemoveListener(OnTrackablesChangedPlane);
        }

        // -- Tracked image mode (Bonsai) -------------------------

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

        private void AnchorToImage(ARTrackedImage img)
        {
            if (_anchored || _arWorldManager == null) return;

            Pose imagePose = new Pose(img.transform.position, img.transform.rotation);
            _arWorldManager.AnchorWorld(imagePose, _mainCamera.transform);
            _anchored = true;

            _arTrackedImageManager.trackablesChanged.RemoveListener(OnTrackablesChangedImage);
        }

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

        private void UnsubscribeFromManagers()
        {
            if (_arPlaneManager != null)
                _arPlaneManager.trackablesChanged.RemoveListener(OnTrackablesChangedPlane);
            if (_arTrackedImageManager != null)
                _arTrackedImageManager.trackablesChanged.RemoveListener(OnTrackablesChangedImage);
        }

        #endregion

        #region Validation ----------------------------------------

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
