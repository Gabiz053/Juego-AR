// ------------------------------------------------------------
//  ARDepthService.cs  -  _Project.Scripts.AR
//  Toggles depth-based occlusion via AROcclusionManager and
//  works around ARCore subsystem state after scene transitions.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARCore;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace _Project.Scripts.AR
{
    /// <summary>
    /// Manages depth-based occlusion via <see cref="AROcclusionManager"/>.
    /// Works around an ARCore subsystem quirk after scene transitions
    /// where the depth mode remains disabled unless explicitly re-requested
    /// on the subsystem and the ARCore session is marked dirty.
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
                // Bypass the AROcclusionManager guard and set depth on the
                // subsystem directly, then tell ARCore to reread its features.
                // Needed after a scene transition (Title_Screen → Main_AR) where
                // the XROcclusionSubsystem singleton retains Disabled.
                if (_occlusionManager.subsystem != null)
                {
                    _occlusionManager.subsystem.requestedEnvironmentDepthMode = EnvironmentDepthMode.Best;

                    var sessionList = new List<XRSessionSubsystem>();
                    SubsystemManager.GetSubsystems(sessionList);
                    foreach (var s in sessionList)
                    {
                        if (s is ARCoreSessionSubsystem arCore)
                        {
                            arCore.SetConfigurationDirty();
                            break;
                        }
                    }
                }

                _occlusionManager.enabled = true;
            }
            else
            {
                _occlusionManager.enabled = false;
            }

            Debug.Log($"[ARDepthService] Depth occlusion {(enable ? "ON" : "OFF")}.");
            OnDepthToggled?.Invoke(IsDepthEnabled);
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
                Debug.LogWarning("[ARDepthService] _occlusionManager is not assigned.", this);
        }

        #endregion
    }
}
