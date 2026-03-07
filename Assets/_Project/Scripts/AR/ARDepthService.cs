// ??????????????????????????????????????????????
//  ARDepthService.cs  ·  _Project.Scripts.AR
//  Runtime toggle for ARCore Depth API occlusion via AROcclusionManager.
// ??????????????????????????????????????????????

using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace _Project.Scripts.AR
{
    /// <summary>
    /// Wraps the <see cref="AROcclusionManager"/> to enable or disable
    /// environment-depth occlusion at runtime.<br/>
    /// When occlusion is active, real-world objects (hands, furniture) mask
    /// virtual blocks correctly. When disabled, blocks always render on top
    /// of the real world.<br/>
    /// Attach to <c>XR Origin (Mobile AR)</c>. The <see cref="AROcclusionManager"/>
    /// lives on <c>Main Camera</c> and must be wired manually in the Inspector.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/AR/AR Depth Service")]
    public class ARDepthService : MonoBehaviour
    {
        #region Inspector ?????????????????????????????????????

        [Header("Dependencies")]
        [Tooltip("AROcclusionManager on the Main Camera. Must be assigned manually in the Inspector.")]
        [SerializeField] private AROcclusionManager _occlusionManager;

        [Header("Initial State")]
        [Tooltip("When true, depth occlusion starts enabled when the app launches.")]
        [SerializeField] private bool _enabledOnStart = false;

        #endregion

        #region Events ????????????????????????????????????????

        /// <summary>
        /// Raised whenever the depth-occlusion state changes.
        /// The <see langword="bool"/> parameter is <c>true</c> when occlusion
        /// is now enabled.
        /// </summary>
        public event Action<bool> OnDepthToggled;

        #endregion

        #region Public API ????????????????????????????????????

        /// <summary><c>true</c> when depth occlusion is currently active.</summary>
        public bool IsDepthEnabled { get; private set; }

        /// <summary>
        /// Toggles depth occlusion between enabled and disabled.
        /// Called by the <c>Btn_Depth</c> button in the options menu.
        /// </summary>
        public void ToggleDepth()
        {
            SetDepth(!IsDepthEnabled);
        }

        /// <summary>
        /// Explicitly sets the depth-occlusion state.
        /// </summary>
        /// <param name="enable"><c>true</c> to enable occlusion, <c>false</c> to disable.</param>
        public void SetDepth(bool enable)
        {
            if (_occlusionManager == null)
            {
                Debug.LogWarning("[ARDepthService] _occlusionManager is not assigned — operation ignored.", this);
                return;
            }

            IsDepthEnabled = enable;

            if (enable)
            {
                // Enable environment depth estimation for real-world occlusion.
                // Fastest mode prioritises mobile GPU performance over accuracy.
                _occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Fastest;

                // Enable human-body (hand) depth if the device supports it.
                _occlusionManager.requestedHumanDepthMode = HumanSegmentationDepthMode.Fastest;

                // Activating the component lets ARCore start processing depth frames.
                _occlusionManager.enabled = true;
            }
            else
            {
                // Disable depth modes to free GPU resources immediately.
                _occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Disabled;
                _occlusionManager.requestedHumanDepthMode       = HumanSegmentationDepthMode.Disabled;

                // Disabling the component stops all depth processing entirely.
                _occlusionManager.enabled = false;
            }

            OnDepthToggled?.Invoke(IsDepthEnabled);
            Debug.Log($"[ARDepthService] Depth occlusion: {(IsDepthEnabled ? "ENABLED" : "DISABLED")}.");
        }

        #endregion

        #region Unity Lifecycle ????????????????????????????????

        private void Awake()
        {
            // ARDepthService lives on XR Origin; AROcclusionManager lives on Main Camera.
            // They are on different GameObjects, so GetComponent would always return null.
            // The reference must be wired manually in the Inspector.
            Debug.Log("[ARDepthService] Awake — ready.");
        }

        private void Start()
        {
            ValidateReferences();

            // Apply the initial state. We call SetDepth directly rather than
            // ToggleDepth so the starting value is deterministic regardless of
            // IsDepthEnabled's default.
            if (_occlusionManager != null)
                SetDepth(_enabledOnStart);

            Debug.Log($"[ARDepthService] Initialized — depth occlusion starts {(_enabledOnStart ? "enabled" : "disabled")}.");
        }

        #endregion

        #region Validation ????????????????????????????????????

        private void ValidateReferences()
        {
            if (_occlusionManager == null)
                Debug.LogWarning(
                    "[ARDepthService] _occlusionManager is not assigned. " +
                    "Add AROcclusionManager to the Main Camera and assign it here.", this);
        }

        #endregion
    }
}
