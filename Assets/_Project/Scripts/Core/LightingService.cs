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

        [Header("Focus Light Settings")]
        [Tooltip("Range of the camera spot light in Focus mode (metres).")]
        [SerializeField] [Range(5f, 100f)] private float _focusLightRange = 15f;

        [Tooltip("Intensity of the camera spot light in Focus mode.")]
        [SerializeField] [Range(0.1f, 5f)] private float _focusLightIntensity = 0.25f;

        [Tooltip("Outer cone angle of the spot light in Focus mode (degrees).")]
        [SerializeField] [Range(20f, 120f)] private float _focusLightAngle = 70f;

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
            {
                _cameraSpotLight.enabled = focusMode;

                if (focusMode)
                {
                    _cameraSpotLight.range     = _focusLightRange;
                    _cameraSpotLight.intensity  = _focusLightIntensity;
                    _cameraSpotLight.spotAngle  = _focusLightAngle;
                }
            }

            if (_directionalLight != null && _disableGlobalOnFocus)
                _directionalLight.enabled = !focusMode;

            OnLightingToggled?.Invoke(focusMode);
            Debug.Log($"[LightingService] Mode: {(focusMode ? "Focus" : "Global")}.");
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_directionalLight == null)
                Debug.LogWarning("[LightingService] _directionalLight is not assigned.", this);
            if (_cameraSpotLight == null)
                Debug.LogWarning("[LightingService] _cameraSpotLight is not assigned.", this);
        }

        #endregion
    }
}
