// ------------------------------------------------------------
//  ARDepthService.cs  -  _Project.Scripts.AR
//  Runtime toggle for ARCore Depth API occlusion via
//  AROcclusionManager.  Uses Best quality for maximum fidelity.
// ------------------------------------------------------------

using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace _Project.Scripts.AR
{
    /// <summary>
    /// Wraps <see cref="AROcclusionManager"/> to enable or disable
    /// environment-depth occlusion at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/AR/AR Depth Service")]
    public class ARDepthService : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Dependencies")]
        [Tooltip("AROcclusionManager on the Main Camera.")]
        [SerializeField] private AROcclusionManager _occlusionManager;

        [Header("Initial State")]
        [Tooltip("When true, depth occlusion starts enabled on launch.")]
        [SerializeField] private bool _enabledOnStart;

        #endregion

        #region Events --------------------------------------------

        /// <summary>Raised whenever the depth-occlusion state changes.</summary>
        public event Action<bool> OnDepthToggled;

        #endregion

        #region Public API ----------------------------------------

        /// <summary><c>true</c> when depth occlusion is currently active.</summary>
        public bool IsDepthEnabled { get; private set; }

        /// <summary>Toggles depth occlusion ON / OFF.</summary>
        public void ToggleDepth() => SetDepth(!IsDepthEnabled);

        /// <summary>Explicitly sets the depth-occlusion state.</summary>
        public void SetDepth(bool enable)
        {
            if (_occlusionManager == null) return;

            IsDepthEnabled = enable;

            if (enable)
            {
                _occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Best;
                _occlusionManager.requestedHumanDepthMode       = HumanSegmentationDepthMode.Best;
                _occlusionManager.enabled = true;
            }
            else
            {
                _occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Disabled;
                _occlusionManager.requestedHumanDepthMode       = HumanSegmentationDepthMode.Disabled;
                _occlusionManager.enabled = false;
            }

            OnDepthToggled?.Invoke(IsDepthEnabled);
            Debug.Log($"[ARDepthService] Depth occlusion {(enable ? "ON (Best)" : "OFF")}.");
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Start()
        {
            ValidateReferences();

            if (_occlusionManager != null)
                SetDepth(_enabledOnStart);
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_occlusionManager == null)
                Debug.LogWarning("[ARDepthService] _occlusionManager is not assigned!", this);
        }

        #endregion
    }
}
