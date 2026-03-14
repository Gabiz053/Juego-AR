// ------------------------------------------------------------
//  HapticService.cs  -  _Project.Scripts.Core
//  Thin wrapper around the Vibration plugin for haptic feedback.
// ------------------------------------------------------------

using System;
using UnityEngine;
using _Project.Scripts.Infrastructure;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Centralised haptic feedback service.  Wraps the Vibration plugin
    /// (Benoit Freslon) behind simple preset methods so callers never
    /// reference the plugin directly.<br/>
    /// Starts <b>disabled</b> by default � the user enables it from the
    /// options menu via <see cref="ToggleHaptics"/>.  Fires
    /// <see cref="OnHapticsToggled"/> so the UI button can reflect state.
    /// <br/><br/>
    /// <b>Available presets:</b><br/>
    /// � <see cref="VibrateLight"/> � tiny pop  (? 50 ms, UI taps, block place, photo)<br/>
    /// � <see cref="VibrateMedium"/> � medium peek (? 100 ms, block destroy)<br/>
    /// � <see cref="VibrateHeavy"/> � triple-tap nope pattern (perfect harmony, high phase)
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Core/Haptic Service")]
    public class HapticService : MonoBehaviour, IHapticService
    {
        #region Events --------------------------------------------

        /// <summary>
        /// Raised when haptics are toggled ON / OFF.<br/>
        /// <c>true</c> = haptics enabled.
        /// </summary>
        public event Action<bool> OnHapticsToggled;

        #endregion

        #region State ---------------------------------------------

        private bool _initialized;

        #endregion

        #region Public API ----------------------------------------

        /// <summary><c>true</c> when haptic feedback is active.</summary>
        public bool IsEnabled { get; private set; }

        /// <summary>Toggles haptics ON / OFF.</summary>
        public void ToggleHaptics()
        {
            SetEnabled(!IsEnabled);
        }

        /// <summary>Sets the haptic state explicitly.</summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            OnHapticsToggled?.Invoke(IsEnabled);
            Debug.Log($"[HapticService] Haptics {(IsEnabled ? "enabled" : "disabled")}.");
        }

        /// <summary>
        /// Very soft pop vibration (? 50 ms).<br/>
        /// Use for: UI button taps, block placement, screenshot capture.
        /// </summary>
        public void VibrateLight()
        {
            if (!IsEnabled) return;
            EnsureInitialized();
#if UNITY_ANDROID || UNITY_IOS
            Vibration.VibratePop();
#endif
        }

        /// <summary>
        /// Medium peek vibration (? 100 ms).<br/>
        /// Use for: block destruction, tool interactions.
        /// </summary>
        public void VibrateMedium()
        {
            if (!IsEnabled) return;
            EnsureInitialized();
#if UNITY_ANDROID || UNITY_IOS
            Vibration.VibratePeek();
#endif
        }

        /// <summary>
        /// Strong triple-tap nope pattern.<br/>
        /// Use for: perfect harmony celebration, high harmony phases.
        /// </summary>
        public void VibrateHeavy()
        {
            if (!IsEnabled) return;
            EnsureInitialized();
#if UNITY_ANDROID || UNITY_IOS
            _ = Vibration.VibrateNope();
#endif
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            ServiceLocator.Register<IHapticService>(this);
        }

        private void Start()
        {
            EnsureInitialized();
            Debug.Log("[HapticService] Ready (default OFF).");
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IHapticService>();
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Initialises the native Vibration plugin once.  Safe to call
        /// multiple times � the flag prevents double-init.
        /// </summary>
        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
#if UNITY_ANDROID || UNITY_IOS
            Vibration.Init();
            Debug.Log("[HapticService] Vibration plugin initialized.");
#endif
        }

        #endregion
    }
}
