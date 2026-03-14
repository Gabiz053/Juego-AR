// ------------------------------------------------------------
//  WorldModeBootstrapper.cs  -  _Project.Scripts.AR
//  Reads WorldModeContext at startup and configures the voxel
//  world (scale, grid, anchor strategy) accordingly.
// ------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using _Project.Scripts.Core;
using _Project.Scripts.Infrastructure;

namespace _Project.Scripts.AR
{
    /// <summary>
    /// Entry point for the game scene.  Reads
    /// <see cref="WorldModeContext.Selected"/>, finds the matching
    /// <see cref="WorldModeSO"/> and applies its configuration
    /// (scale, anchor manager, etc.).<br/>
    /// AR manager configuration is deferred to <see cref="Start"/> via a
    /// coroutine so the <see cref="ARSession"/> has time to resume after a
    /// scene transition (e.g. from <c>Title_Screen</c> with front-camera
    /// face tracking).
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
        [Tooltip("Mode to use when launching Main_AR directly (bypassing the title screen).\n"
               + "Set to the mode you want to test.\n"
               + "Has NO effect when coming from the title screen -- WorldModeContext.Selected is used instead.")]
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

        #region Events --------------------------------------------

        /// <summary>
        /// Raised once when Bonsai mode detects and anchors to a tracked
        /// image for the first time.  <see cref="BonsaiSessionController"/>
        /// subscribes to open the garden selector popup.
        /// </summary>
        public event Action OnBonsaiImageDetected;

        #endregion

        #region State ---------------------------------------------

        private WorldModeSO    _activeConfig;
        private Camera         _mainCamera;
        private bool           _anchored;
        private ARTrackedImage _trackedImage;

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            _mainCamera = Camera.main;

            // WorldModeContext.Selected is None when Main_AR is opened directly
            // (bypassing the title screen, e.g. in Editor or dev testing).
            // In that case the dev override takes priority.
            bool fromTitleScreen = WorldModeContext.Selected != WorldMode.None;
            WorldMode targetMode = fromTitleScreen ? WorldModeContext.Selected : _devOverrideMode;

            _activeConfig = FindConfig(targetMode);

            if (_activeConfig == null)
            {
                Debug.LogError($"[WorldModeBootstrapper] No WorldModeSO found for mode '{targetMode}'. Check _modeConfigs.", this);
                return;
            }

            // Write back so other systems can always read WorldModeContext.Selected.
            WorldModeContext.Selected = _activeConfig.Mode;

            ApplyWorldScale();

            string source = fromTitleScreen ? "title screen" : "dev override";
            Debug.Log($"[WorldModeBootstrapper] Mode: {_activeConfig.DisplayName} (source: {source}), scale: {_activeConfig.WorldContainerScale}, anchor: {_activeConfig.AnchorType}.");
        }

        private void Start()
        {
            ValidateReferences();

            if (_activeConfig != null)
                StartCoroutine(ConfigureARManagersDeferred());
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

        /// <summary>
        /// Waits for the <see cref="ARSession"/> to reach a tracking-ready state
        /// before configuring managers.  This avoids a race condition when
        /// transitioning from the title scene (front camera / face-tracking)
        /// where the native session would apply its config before the image
        /// library is set on the subsystem.
        /// </summary>
        private IEnumerator ConfigureARManagersDeferred()
        {
            // Give the ARSession one frame to resume / start its subsystem.
            yield return null;

            // Wait until the session is actually tracking.
            float timeout = 6f;
            float elapsed = 0f;
            while (ARSession.state < ARSessionState.SessionTracking && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (ARSession.state < ARSessionState.SessionTracking)
                Debug.LogWarning($"[WorldModeBootstrapper] ARSession did not reach SessionTracking within {timeout}s (state: {ARSession.state}) -- configuring managers anyway.");
            else
                Debug.Log($"[WorldModeBootstrapper] ARSession ready (state: {ARSession.state}, waited {elapsed:F2}s) -- configuring AR managers.");

            ConfigureARManagers();
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
                Debug.Log("[WorldModeBootstrapper] ARPlaneManager enabled -- waiting for plane detection.");
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
                {
                    _arTrackedImageManager.referenceLibrary = _activeConfig.ImageLibrary;
                    Debug.Log($"[WorldModeBootstrapper] ImageLibrary assigned -- {_activeConfig.ImageLibrary.count} reference image(s), physical width: {_activeConfig.ImagePhysicalWidth}m.");
                }
                else
                {
                    Debug.LogError("[WorldModeBootstrapper] Bonsai mode but ImageLibrary is null! Assign it in WorldModeConfig_Bonsai.", this);
                }

                _arTrackedImageManager.enabled = true;
                _arTrackedImageManager.trackablesChanged.AddListener(OnTrackablesChangedImage);
                Debug.Log("[WorldModeBootstrapper] ARTrackedImageManager enabled -- waiting for image detection.");
            }
            else
            {
                Debug.LogError("[WorldModeBootstrapper] _arTrackedImageManager is not assigned! Required for Bonsai mode.", this);
            }

            if (_arPlaneManager != null)
                _arPlaneManager.enabled = false;
        }

        /// <summary>
        /// Listens for added, updated and removed tracked images.
        /// First detection anchors the WorldContainer and fires
        /// <see cref="OnBonsaiImageDetected"/>.  Subsequent updates
        /// keep the WorldContainer glued to the live image pose
        /// (continuous tracking so the garden follows the card).
        /// </summary>
        private void OnTrackablesChangedImage(ARTrackablesChangedEventArgs<ARTrackedImage> args)
        {
            // Guard: during scene unload references may already be destroyed.
            if (_worldContainer == null) return;

            // -- First detection: anchor to the image --
            if (!_anchored)
            {
                foreach (ARTrackedImage img in args.added)
                {
                    Debug.Log($"[WorldModeBootstrapper] Image ADDED -- name: '{img.referenceImage.name}', state: {img.trackingState}, pos: {img.transform.position}, size: {img.size}.");
                    if (img.trackingState == TrackingState.Tracking || img.trackingState == TrackingState.Limited)
                    {
                        AnchorToImage(img);
                        return;
                    }
                }

                foreach (ARTrackedImage img in args.updated)
                {
                    if (img.trackingState == TrackingState.Tracking)
                    {
                        AnchorToImage(img);
                        return;
                    }
                }

                return;
            }

            // -- Continuous tracking: follow the image pose --
            if (_trackedImage == null || _worldContainer == null) return;

            foreach (ARTrackedImage img in args.updated)
            {
                if (img.trackableId != _trackedImage.trackableId) continue;

                if (img.trackingState == TrackingState.Tracking)
                {
                    _worldContainer.SetPositionAndRotation(
                        img.transform.position,
                        img.transform.rotation);
                }
            }

            foreach (KeyValuePair<TrackableId, ARTrackedImage> kvp in args.removed)
            {
                if (_trackedImage != null && kvp.Key == _trackedImage.trackableId)
                    Debug.LogWarning("[WorldModeBootstrapper] Tracked image lost -- garden may drift.");
            }
        }

        /// <summary>
        /// Positions the WorldContainer at the tracked image pose,
        /// activates the grid, stores the image reference for continuous
        /// tracking, and fires <see cref="OnBonsaiImageDetected"/>.
        /// Unlike plane mode, no <see cref="ARAnchor"/> is created --
        /// the pose is updated every frame from the live image.
        /// </summary>
        private void AnchorToImage(ARTrackedImage img)
        {
            if (_anchored || _worldContainer == null) return;

            _worldContainer.SetPositionAndRotation(
                img.transform.position,
                img.transform.rotation);

            _gridManager?.ActivateGrid(_mainCamera.transform);

            _trackedImage = img;
            _anchored     = true;

            OnBonsaiImageDetected?.Invoke();
            Debug.Log($"[WorldModeBootstrapper] World anchored to image '{img.referenceImage.name}' -- continuous tracking active, pose: {img.transform.position}.");
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
                Debug.LogWarning("[WorldModeBootstrapper] _modeConfigs is not assigned.", this);
            if (_worldContainer == null)
                Debug.LogWarning("[WorldModeBootstrapper] _worldContainer is not assigned.", this);
            if (_arWorldManager == null)
                Debug.LogWarning("[WorldModeBootstrapper] _arWorldManager is not assigned.", this);
            if (_gridManager == null)
                Debug.LogWarning("[WorldModeBootstrapper] _gridManager is not assigned.", this);
            if (_mainCamera == null)
                Debug.LogWarning("[WorldModeBootstrapper] _mainCamera is not assigned.", this);
        }

        #endregion
    }
}
