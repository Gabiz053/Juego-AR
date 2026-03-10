// ??????????????????????????????????????????????
//  LightingService.cs  ·  _Project.Scripts.Core
//  Manages the two exclusive lighting modes (Global / Focus).
// ??????????????????????????????????????????????

using System;
using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Controls the scene lighting by swapping between two exclusive modes:<br/>
    /// • <b>Global</b> — Directional Light ON, Camera Spot Light OFF.<br/>
    /// • <b>Focus</b>  — Directional Light OFF, Camera Spot Light ON
    ///   (Linterna / flashlight).<br/>
    /// Fires <see cref="OnLightingToggled"/> so UI buttons can reflect the
    /// current state.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Core/Lighting Service")]
    public class LightingService : MonoBehaviour
    {
        #region Inspector ?????????????????????????????????????

        [Header("Lights")]
        [Tooltip("Scene directional light — active in Global mode, off in Focus mode.")]
        [SerializeField] private Light _directionalLight;

        [Tooltip("Spot/Point light child of the AR Camera — off in Global mode, active in Focus mode.")]
        [SerializeField] private Light _cameraSpotLight;

        [Header("Behaviour")]
        [Tooltip("When ON, turning on the camera spot light also disables the directional light.\n" +
                 "When OFF, both lights can be on simultaneously.")]
        [SerializeField] private bool _disableGlobalOnFocus = true;

        #endregion

        #region Events ????????????????????????????????????????

        /// <summary>
        /// Raised whenever the lighting mode changes.<br/>
        /// <c>true</c> = Focus mode active (linterna ON).<br/>
        /// <c>false</c> = Global mode active (linterna OFF).
        /// </summary>
        public event Action<bool> OnLightingToggled;

        #endregion

        #region Public API ????????????????????????????????????

        /// <summary><c>true</c> when the camera spot light (Focus / Linterna) is active.</summary>
        public bool IsFocusMode { get; private set; }

        /// <summary>
        /// Swaps between Global and Focus lighting modes.
        /// Called by <see cref="UI.GameOptionsMenu.ToggleLighting"/>.
        /// </summary>
        public void ToggleLighting()
        {
            ApplyMode(!IsFocusMode);
        }

        #endregion

        #region Unity Lifecycle ????????????????????????????????

        private void Start()
        {
            ValidateReferences();

            // Initial state: Global mode — directional ON, camera spot OFF.
            ApplyMode(false);
            Debug.Log("[LightingService] Initialized — Global mode.");
        }

        #endregion

        #region Internals ?????????????????????????????????????

        private void ApplyMode(bool focusMode)
        {
            IsFocusMode = focusMode;

            if (_cameraSpotLight != null)
                _cameraSpotLight.enabled = focusMode;
            else if (focusMode)
                Debug.LogWarning("[LightingService] _cameraSpotLight is not assigned — focus mode has no light.", this);

            if (_directionalLight != null && _disableGlobalOnFocus)
                _directionalLight.enabled = !focusMode;

            OnLightingToggled?.Invoke(focusMode);

            Debug.Log($"[LightingService] Mode ? {(focusMode ? "FOCUS (linterna)" : "GLOBAL (directional)")}.");
        }

        #endregion

        #region Validation ????????????????????????????????????

        private void ValidateReferences()
        {
            if (_directionalLight == null)
                Debug.LogError("[LightingService] _directionalLight is not assigned!", this);
            if (_cameraSpotLight == null)
                Debug.LogWarning("[LightingService] _cameraSpotLight is not assigned — focus mode will not work.", this);
        }

        #endregion
    }
}
