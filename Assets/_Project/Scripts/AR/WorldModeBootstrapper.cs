// ??????????????????????????????????????????????
//  WorldModeBootstrapper.cs  ·  _Project.Scripts.AR
//  Reads WorldModeContext at startup and configures the voxel world
//  (scale, grid, anchor strategy) accordingly.
//  Attach to the XR Origin (Mobile AR) GameObject.
// ??????????????????????????????????????????????

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using _Project.Scripts.Core;

namespace _Project.Scripts.AR
{
    /// <summary>
    /// Entry point for the game scene.<br/>
    /// Reads <see cref="WorldModeContext.Selected"/> (set by the title
    /// screen, or overridden here via <see cref="_devOverrideMode"/> in
    /// the Inspector while the title screen does not yet exist), finds the
    /// matching <see cref="WorldModeSO"/> and applies its configuration:<br/>
    /// <list type="bullet">
    /// <item>Sets <c>WorldContainer.localScale</c>.</item>
    /// <item>Enables the correct anchor manager
    ///       (<see cref="ARPlaneManager"/> or <see cref="ARTrackedImageManager"/>)
    ///       and disables the other.</item>
    /// <item>Registers the appropriate first-placement handler on
    ///       <see cref="ARPlaneManager"/> or <see cref="ARTrackedImageManager"/>.</item>
    /// </list>
    /// Every other system (<see cref="ARWorldManager"/>, <see cref="Interaction.ARBlockPlacer"/>,
    /// <see cref="Core.GridManager"/>) is untouched — they only see the
    /// already-configured <c>WorldContainer</c> and respond to
    /// <see cref="ARWorldManager.AnchorWorld"/> exactly as before.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/AR/World Mode Bootstrapper")]
    public class WorldModeBootstrapper : MonoBehaviour
    {
        #region Inspector ?????????????????????????????????????

        [Header("Mode Configs")]
        [Tooltip("Three WorldModeSO assets — one per mode. Order does not matter; " +
                 "they are matched by WorldModeSO.Mode.")]
        [SerializeField] private WorldModeSO[] _modeConfigs;

        [Header("Dev Override")]
        [Tooltip("Used while the title screen does not exist.\n" +
                 "The bootstrapper writes this value into WorldModeContext " +
                 "at Awake so the rest of the session uses it.\n" +
                 "When the title screen is implemented it will write " +
                 "WorldModeContext.Selected before loading this scene " +
                 "and this field becomes irrelevant.")]
        [SerializeField] private WorldMode _devOverrideMode = WorldMode.Normal;

        [Header("World References")]
        [Tooltip("The WorldContainer whose localScale is set by this bootstrapper.")]
        [SerializeField] private Transform _worldContainer;

        [Tooltip("ARWorldManager that handles anchor creation once a surface is found.")]
        [SerializeField] private ARWorldManager _arWorldManager;

        [Tooltip("GridManager — its GridSize is NOT changed here; " +
                 "visual scale is handled entirely by WorldContainer.localScale.")]
        [SerializeField] private GridManager _gridManager;

        [Header("AR Managers")]
        [Tooltip("AR Plane Manager — active in Normal and Real modes.")]
        [SerializeField] private ARPlaneManager _arPlaneManager;

        [Tooltip("AR Tracked Image Manager — active in Bonsai mode only.")]
        [SerializeField] private ARTrackedImageManager _arTrackedImageManager;

        #endregion

        #region State ?????????????????????????????????????????

        private WorldModeSO _activeConfig;
        private Camera      _mainCamera;
        private bool        _anchored;

        #endregion

        #region Unity Lifecycle ????????????????????????????????

        private void Awake()
        {
            // Override WorldModeContext with the Inspector value so the
            // game is playable without a title screen.
            WorldModeContext.Selected = _devOverrideMode;

            _mainCamera = Camera.main;

            _activeConfig = FindConfig(WorldModeContext.Selected);
            if (_activeConfig == null)
            {
                Debug.LogError($"[WorldModeBootstrapper] No WorldModeSO found for mode " +
                               $"'{WorldModeContext.Selected}'. Add it to _modeConfigs.", this);
                return;
            }

            ApplyWorldScale();
            ConfigureARManagers();

            Debug.Log($"[WorldModeBootstrapper] Mode '{_activeConfig.DisplayName}' applied " +
                      $"(scale={_activeConfig.WorldContainerScale}, " +
                      $"anchor={_activeConfig.AnchorType}).");
        }

        private void OnDestroy()
        {
            UnsubscribeFromManagers();
        }

        #endregion

        #region Configuration ?????????????????????????????????

        /// <summary>
        /// Applies <see cref="WorldModeSO.WorldContainerScale"/> to the
        /// WorldContainer as a uniform scale on all three axes.
        /// </summary>
        private void ApplyWorldScale()
        {
            if (_worldContainer == null) return;
            float s = _activeConfig.WorldContainerScale;
            _worldContainer.localScale = new Vector3(s, s, s);
            Debug.Log($"[WorldModeBootstrapper] WorldContainer scale set to {s}.");
        }

        /// <summary>
        /// Enables exactly one AR manager based on <see cref="WorldModeSO.AnchorType"/>
        /// and subscribes to its first-detection event.
        /// </summary>
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

        // ?? Plane mode (Normal / Real) ??????????????????????

        private void EnablePlaneMode()
        {
            if (_arPlaneManager != null)
            {
                _arPlaneManager.enabled = true;
                _arPlaneManager.trackablesChanged.AddListener(OnTrackablesChangedPlane);
            }

            if (_arTrackedImageManager != null)
                _arTrackedImageManager.enabled = false;

            Debug.Log("[WorldModeBootstrapper] AR Plane mode enabled.");
        }

        private void OnTrackablesChangedPlane(ARTrackablesChangedEventArgs<ARPlane> args)
        {
            if (args.added.Count > 0)
            {
                Debug.Log($"[WorldModeBootstrapper] First AR plane detected " +
                          $"({args.added[0].trackableId}).");
                _arPlaneManager.trackablesChanged.RemoveListener(OnTrackablesChangedPlane);
            }
        }

        // ?? Tracked image mode (Bonsai) ?????????????????????

        private void EnableTrackedImageMode()
        {
            if (_arTrackedImageManager != null)
            {
                if (_activeConfig.ImageLibrary != null)
                    _arTrackedImageManager.referenceLibrary = _activeConfig.ImageLibrary;
                else
                    Debug.LogWarning("[WorldModeBootstrapper] Bonsai mode selected but " +
                                     "ImageLibrary is null — tracking will not work.", this);

                _arTrackedImageManager.enabled = true;
                _arTrackedImageManager.trackablesChanged.AddListener(OnTrackablesChangedImage);
            }
            else
            {
                Debug.LogError("[WorldModeBootstrapper] _arTrackedImageManager is not assigned " +
                               "but Bonsai mode requires it.", this);
            }

            if (_arPlaneManager != null)
                _arPlaneManager.enabled = false;

            Debug.Log("[WorldModeBootstrapper] AR Tracked Image mode enabled (Bonsai).");
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
                if (img.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
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

            Debug.Log($"[WorldModeBootstrapper] World anchored to tracked image " +
                      $"'{img.referenceImage.name}' at {img.transform.position}.");
        }

        #endregion

        #region Helpers ???????????????????????????????????????

        /// <summary>
        /// Finds the <see cref="WorldModeSO"/> in <see cref="_modeConfigs"/>
        /// whose <see cref="WorldModeSO.Mode"/> matches <paramref name="mode"/>.
        /// Returns <c>null</c> if not found.
        /// </summary>
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

        #region Validation ????????????????????????????????????

        private void OnValidate()
        {
            if (_modeConfigs == null || _modeConfigs.Length == 0)
                Debug.LogWarning("[WorldModeBootstrapper] _modeConfigs is empty — " +
                                 "no mode configuration will be applied.", this);
            if (_worldContainer == null)
                Debug.LogWarning("[WorldModeBootstrapper] _worldContainer is not assigned.", this);
            if (_arWorldManager == null)
                Debug.LogWarning("[WorldModeBootstrapper] _arWorldManager is not assigned.", this);
        }

        #endregion
    }
}
