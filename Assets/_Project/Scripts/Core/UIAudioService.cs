// ------------------------------------------------------------
//  UIAudioService.cs  -  _Project.Scripts.Core
//  Centralised audio service for all UI interaction sounds.
// ------------------------------------------------------------

using UnityEngine;
using _Project.Scripts.Infrastructure;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Plays UI feedback sounds through a single shared
    /// <see cref="AudioSource"/> with random clip selection and pitch
    /// variation.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Core/UI Audio Service")]
    public class UIAudioService : MonoBehaviour, IUIAudioService
    {
        #region Inspector -----------------------------------------

        [Header("UI Sounds")]
        [Tooltip("Clips played at random on generic button tap.")]
        [SerializeField] private AudioClip[] _clickSounds;

        [Tooltip("Clips played at random on toggle switch.")]
        [SerializeField] private AudioClip[] _toggleSounds;

        [Tooltip("Clips played at random when options dropdown opens / closes.")]
        [SerializeField] private AudioClip[] _menuOpenSounds;

        [Tooltip("Clips played at random on destructive action confirmation.")]
        [SerializeField] private AudioClip[] _confirmSounds;

        [Tooltip("Clips played at random on cancel / dismiss.")]
        [SerializeField] private AudioClip[] _cancelSounds;

        [Tooltip("Clips played at random on hotbar slot selection.")]
        [SerializeField] private AudioClip[] _slotSelectSounds;

        [Tooltip("Clips played at random when a screenshot is taken.")]
        [SerializeField] private AudioClip[] _photoSounds;

        [Header("Harmony Phase Sounds")]
        [Tooltip("Played when harmony reaches 25%.")]
        [SerializeField] private AudioClip _harmonyPhase1Sound;

        [Tooltip("Played when harmony reaches 50%.")]
        [SerializeField] private AudioClip _harmonyPhase2Sound;

        [Tooltip("Played when harmony reaches 75%.")]
        [SerializeField] private AudioClip _harmonyPhase3Sound;

        [Tooltip("Played when harmony reaches 100%.")]
        [SerializeField] private AudioClip _harmonyPhase4Sound;

        [Header("Pitch Variation")]
        [Tooltip("Maximum random pitch offset per play. 0 = no variation.")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _pitchVariation = 0.05f;

        #endregion

        #region State ---------------------------------------------

        private AudioSource    _audioSource;
        private IHapticService _hapticService;

        private int _lastClickIndex      = -1;
        private int _lastToggleIndex     = -1;
        private int _lastMenuOpenIndex   = -1;
        private int _lastConfirmIndex    = -1;
        private int _lastCancelIndex     = -1;
        private int _lastSlotSelectIndex = -1;
        private int _lastPhotoIndex      = -1;

        #endregion

        #region Public API ----------------------------------------

        /// <summary>Plays a random button-click sound with a soft haptic tick.</summary>
        public void PlayClick()
        {
            Play(_clickSounds, ref _lastClickIndex);
            _hapticService?.VibrateLight();
        }

        /// <summary>Plays a random toggle sound with a soft haptic tick.</summary>
        public void PlayToggle()
        {
            Play(_toggleSounds, ref _lastToggleIndex);
            _hapticService?.VibrateLight();
        }

        /// <summary>Plays a random menu open/close sound with a soft haptic tick.</summary>
        public void PlayMenuOpen()
        {
            Play(_menuOpenSounds, ref _lastMenuOpenIndex);
            _hapticService?.VibrateLight();
        }

        /// <summary>Plays a random confirmation sound with a soft haptic tick.</summary>
        public void PlayConfirm()
        {
            Play(_confirmSounds, ref _lastConfirmIndex);
            _hapticService?.VibrateLight();
        }

        /// <summary>Plays a random cancel sound with a soft haptic tick.</summary>
        public void PlayCancel()
        {
            Play(_cancelSounds, ref _lastCancelIndex);
            _hapticService?.VibrateLight();
        }

        /// <summary>Plays a random hotbar slot-selection sound with a soft haptic tick.</summary>
        public void PlaySlotSelect()
        {
            Play(_slotSelectSounds, ref _lastSlotSelectIndex);
            _hapticService?.VibrateLight();
        }

        /// <summary>Plays a random screenshot sound (no haptic -- handled by ScreenshotService).</summary>
        public void PlayPhoto()      => Play(_photoSounds,      ref _lastPhotoIndex);

        /// <summary>Plays the harmony phase clip (1-4 = 25/50/75/100%).</summary>
        public void PlayHarmonyPhase(int phase)
        {
            AudioClip clip = phase switch
            {
                1 => _harmonyPhase1Sound,
                2 => _harmonyPhase2Sound,
                3 => _harmonyPhase3Sound,
                4 => _harmonyPhase4Sound,
                _ => null
            };

            if (clip == null) return;

            float prevPitch    = _audioSource.pitch;
            _audioSource.pitch = Random.Range(1f - _pitchVariation, 1f + _pitchVariation);
            _audioSource.PlayOneShot(clip);
            _audioSource.pitch = prevPitch;
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake  = false;
            _audioSource.spatialBlend = 0f;

            ServiceLocator.TryGet<IHapticService>(out _hapticService);
            ServiceLocator.Register<IUIAudioService>(this);
        }

        private void Start()
        {
            ValidateReferences();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IUIAudioService>();
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Picks a random clip from <paramref name="clips"/> (avoiding the
        /// last-played index), applies pitch variation, and plays it.
        /// </summary>
        private void Play(AudioClip[] clips, ref int lastIndex)
        {
            if (clips == null || clips.Length == 0 || _audioSource == null) return;

            int index;
            if (clips.Length == 1)
                index = 0;
            else
            {
                do { index = Random.Range(0, clips.Length); }
                while (index == lastIndex);
            }

            AudioClip clip = clips[index];
            if (clip == null) return;

            lastIndex = index;

            float prevPitch    = _audioSource.pitch;
            _audioSource.pitch = Random.Range(1f - _pitchVariation, 1f + _pitchVariation);
            _audioSource.PlayOneShot(clip);
            _audioSource.pitch = prevPitch;
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_audioSource == null)
                Debug.LogWarning("[UIAudioService] _audioSource is not assigned.", this);
        }

        #endregion
    }
}
