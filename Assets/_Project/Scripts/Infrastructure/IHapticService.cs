// ------------------------------------------------------------
//  IHapticService.cs  -  _Project.Scripts.Infrastructure
//  Contract for haptic feedback presets.
// ------------------------------------------------------------

using System;

namespace _Project.Scripts.Infrastructure
{
    /// <summary>
    /// Centralised haptic feedback with preset vibration intensities.
    /// Consumers never reference the native vibration plugin directly.
    /// </summary>
    public interface IHapticService
    {
        /// <summary><c>true</c> when haptic feedback is active.</summary>
        bool IsEnabled { get; }

        /// <summary>Toggles haptics ON / OFF.</summary>
        void ToggleHaptics();

        /// <summary>Sets the haptic state explicitly.</summary>
        void SetEnabled(bool enabled);

        /// <summary>Very soft pop vibration (~50 ms). UI taps, block placement.</summary>
        void VibrateLight();

        /// <summary>Medium peek vibration (~100 ms). Block destruction.</summary>
        void VibrateMedium();

        /// <summary>Strong triple-tap nope pattern. Perfect harmony.</summary>
        void VibrateHeavy();

        /// <summary>Raised when haptics are toggled ON / OFF.</summary>
        event Action<bool> OnHapticsToggled;
    }
}
