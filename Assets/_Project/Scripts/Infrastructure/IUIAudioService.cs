// ------------------------------------------------------------
//  IUIAudioService.cs  -  _Project.Scripts.Infrastructure
//  Contract for UI interaction sound feedback.
// ------------------------------------------------------------

namespace _Project.Scripts.Infrastructure
{
    /// <summary>
    /// Plays UI feedback sounds (clicks, toggles, menu, confirm, cancel,
    /// slot selection, photo shutter, harmony phase chimes).<br/>
    /// Centralises all UI audio so consumers never reference an
    /// <c>AudioSource</c> directly.
    /// </summary>
    public interface IUIAudioService
    {
        /// <summary>Plays a random button-click sound with a soft haptic tick.</summary>
        void PlayClick();

        /// <summary>Plays a random toggle sound with a soft haptic tick.</summary>
        void PlayToggle();

        /// <summary>Plays a random menu open/close sound with a soft haptic tick.</summary>
        void PlayMenuOpen();

        /// <summary>Plays a random confirmation sound with a soft haptic tick.</summary>
        void PlayConfirm();

        /// <summary>Plays a random cancel sound with a soft haptic tick.</summary>
        void PlayCancel();

        /// <summary>Plays a random hotbar slot-selection sound with a soft haptic tick.</summary>
        void PlaySlotSelect();

        /// <summary>Plays a random screenshot sound (no haptic).</summary>
        void PlayPhoto();

        /// <summary>Plays the harmony phase clip (1-4 = 25/50/75/100%).</summary>
        void PlayHarmonyPhase(int phase);
    }
}
