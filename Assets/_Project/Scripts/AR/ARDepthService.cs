// ------------------------------------------------------------
//  ARDepthService.cs  -  _Project.Scripts.AR
//  Toggles depth-based occlusion via AROcclusionManager.
// ------------------------------------------------------------

using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace _Project.Scripts.AR
{
    /// <summary>
    /// Minimal depth-occlusion toggle.  Enables / disables the
    /// <see cref="AROcclusionManager"/> and nothing else.<br/>
    /// The Inspector checkbox <see cref="_depthOnStart"/> controls
    /// whether occlusion starts enabled or disabled.  The manager's
    /// Inspector settings (<c>requestedEnvironmentDepthMode</c> etc.)
    /// control the actual depth behaviour.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/AR/AR Depth Service")]
    public class ARDepthService : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Dependencies")]
        [Tooltip("AROcclusionManager on the Main Camera.")]
        [SerializeField] private AROcclusionManager _occlusionManager;

        [Header("Behaviour")]
        [Tooltip("When ON, depth occlusion starts enabled at scene load.")]
        [SerializeField] private bool _depthOnStart = true;

        #endregion

        #region Events --------------------------------------------

        /// <summary>Raised whenever the depth-occlusion state changes.</summary>
        public event Action<bool> OnDepthToggled;

        #endregion

        #region Public API ----------------------------------------

        /// <summary><c>true</c> when depth occlusion is currently active.</summary>
        public bool IsDepthEnabled { get; private set; }

        /// <summary>Toggles depth occlusion ON / OFF.</summary>
        public void ToggleDepth()
        {
            Debug.Log($"[ARDepthService] ToggleDepth called. Current: {IsDepthEnabled}.");
            SetDepth(!IsDepthEnabled);
        }

        /// <summary>Explicitly sets the depth-occlusion state.</summary>
        public void SetDepth(bool enable)
        {
            if (_occlusionManager == null)
            {
                Debug.LogWarning("[ARDepthService] Cannot toggle -- _occlusionManager is null.");
                return;
            }

            IsDepthEnabled = enable;
            _occlusionManager.enabled = enable;

            Debug.Log($"[ARDepthService] AROcclusionManager.enabled = {enable}.");
            OnDepthToggled?.Invoke(IsDepthEnabled);
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Start()
        {
            ValidateReferences();

            if (_occlusionManager != null)
            {
                SetDepth(_depthOnStart);
                Debug.Log($"[ARDepthService] Initialized -- depth is {(IsDepthEnabled ? "ON" : "OFF")}.");
            }
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_occlusionManager == null)
                Debug.LogWarning("[ARDepthService] _occlusionManager is not assigned.", this);
        }

        #endregion
    }
}
