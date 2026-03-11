// ------------------------------------------------------------
//  LightingService.cs  -  _Project.Scripts.Core
//  Manages the two exclusive lighting modes (Global / Focus).
// ------------------------------------------------------------

using System;
using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Controls the scene lighting by swapping between two exclusive modes:<br/>
    /// <b>Global</b> - Directional Light ON, Camera Spot Light OFF.<br/>
    /// <b>Focus</b>  - Directional Light OFF, Camera Spot Light ON.<br/>
    /// Fires <see cref="OnLightingToggled"/> so UI buttons can reflect the
    /// current state.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Core/Lighting Service")]
    public class LightingService : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Lights")]
        [Tooltip("Scene directional light (active in Global mode).")]
        [SerializeField] private Light _directionalLight;

        [Tooltip("Spot/Point light child of the AR Camera (active in Focus mode).")]
        [SerializeField] private Light _cameraSpotLight;

        [Header("Behaviour")]
        [Tooltip("When ON, Focus mode also disables the directional light.")]
        [SerializeField] private bool _disableGlobalOnFocus = true;

        #endregion

        #region Events --------------------------------------------

        /// <summary>
        /// Raised whenever the lighting mode changes.<br/>
        /// <c>true</c> = Focus mode active, <c>false</c> = Global mode.
        /// </summary>
        public event Action<bool> OnLightingToggled;

        #endregion

        #region Public API ----------------------------------------

        /// <summary><c>true</c> when the camera spot light (Focus) is active.</summary>
        public bool IsFocusMode { get; private set; }

        /// <summary>Swaps between Global and Focus lighting modes.</summary>
        public void ToggleLighting()
        {
            ApplyMode(!IsFocusMode);
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Start()
        {
            ValidateReferences();
            ApplyMode(false);
        }

        #endregion

        #region Internals -----------------------------------------

        private void ApplyMode(bool focusMode)
        {
            IsFocusMode = focusMode;

            if (_cameraSpotLight != null)
                _cameraSpotLight.enabled = focusMode;

            if (_directionalLight != null && _disableGlobalOnFocus)
                _directionalLight.enabled = !focusMode;

            OnLightingToggled?.Invoke(focusMode);
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_directionalLight == null)
                Debug.LogError("[LightingService] _directionalLight is not assigned!", this);
            if (_cameraSpotLight == null)
                Debug.LogWarning("[LightingService] _cameraSpotLight is not assigned!", this);
        }

        #endregion
    }
}
