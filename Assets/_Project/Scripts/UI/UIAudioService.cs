// ??????????????????????????????????????????????
//  UIAudioService.cs  ·  _Project.Scripts.UI
//  Centralised audio service for all UI interaction sounds.
// ??????????????????????????????????????????????

using UnityEngine;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Plays UI feedback sounds (button clicks, toggles, confirmations, etc.)
    /// through a single shared <see cref="AudioSource"/>.<br/>
    /// Each action holds an array of clips — one is chosen at random on every
    /// call, with pitch variation applied on top, so sounds never repeat
    /// identically back-to-back.<br/>
    /// Centralising UI audio here lets you swap every sound from one place
    /// without touching individual buttons.<br/>
    /// Attach to <c>MainCanvas</c>.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/UI/UI Audio Service")]
    public class UIAudioService : MonoBehaviour
    {
        #region Inspector ?????????????????????????????????????

        [Header("UI Sounds")]
        [Tooltip("One or more clips played at random when any generic button is tapped.")]
        [SerializeField] private AudioClip[] _clickSounds;

        [Tooltip("One or more clips played at random when a toggle is switched on or off (depth, lighting, etc.).")]
        [SerializeField] private AudioClip[] _toggleSounds;

        [Tooltip("One or more clips played at random when the options dropdown is opened or closed.")]
        [SerializeField] private AudioClip[] _menuOpenSounds;

        [Tooltip("One or more clips played at random when a destructive action is confirmed (clear world, etc.).")]
        [SerializeField] private AudioClip[] _confirmSounds;

        [Tooltip("One or more clips played at random when a popup or action is cancelled.")]
        [SerializeField] private AudioClip[] _cancelSounds;

        [Tooltip("One or more clips played at random when a hotbar slot is selected.")]
        [SerializeField] private AudioClip[] _slotSelectSounds;

        [Tooltip("One or more clips played at random when a screenshot is taken.")]
        [SerializeField] private AudioClip[] _photoSounds;

        [Header("Pitch Variation")]
        [Tooltip("Maximum random pitch offset applied per play (±). 0 = no variation.")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _pitchVariation = 0.05f;

        #endregion

        #region Cached Components ?????????????????????????????

        private AudioSource _audioSource;

        // Per-pool trackers of the last index played — prevents the same clip
        // from being chosen twice in a row when a pool has more than one entry.
        private int _lastClickIndex      = -1;
        private int _lastToggleIndex     = -1;
        private int _lastMenuOpenIndex   = -1;
        private int _lastConfirmIndex    = -1;
        private int _lastCancelIndex     = -1;
        private int _lastSlotSelectIndex = -1;
        private int _lastPhotoIndex      = -1;

        #endregion

        #region Unity Lifecycle ????????????????????????????????

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();

            // UI audio is non-spatial — it always plays at full volume
            // regardless of camera position.
            _audioSource.playOnAwake  = false;
            _audioSource.spatialBlend = 0f;

            Debug.Log("[UIAudioService] Awake — AudioSource cached and configured.");
        }

        private void Start()
        {
            ValidateReferences();
            Debug.Log("[UIAudioService] Initialized.");
        }

        #endregion

        #region Public API ????????????????????????????????????

        /// <summary>Plays a random button-click sound.</summary>
        public void PlayClick()      => Play(_clickSounds,      ref _lastClickIndex);

        /// <summary>Plays a random toggle on/off sound.</summary>
        public void PlayToggle()     => Play(_toggleSounds,     ref _lastToggleIndex);

        /// <summary>Plays a random menu open/close sound.</summary>
        public void PlayMenuOpen()   => Play(_menuOpenSounds,   ref _lastMenuOpenIndex);

        /// <summary>Plays a random destructive-action confirmation sound.</summary>
        public void PlayConfirm()    => Play(_confirmSounds,    ref _lastConfirmIndex);

        /// <summary>Plays a random cancel/dismiss sound.</summary>
        public void PlayCancel()     => Play(_cancelSounds,     ref _lastCancelIndex);

        /// <summary>Plays a random hotbar slot-selection sound.</summary>
        public void PlaySlotSelect() => Play(_slotSelectSounds, ref _lastSlotSelectIndex);

        /// <summary>Plays a random screenshot capture sound.</summary>
        public void PlayPhoto()      => Play(_photoSounds,      ref _lastPhotoIndex);

        #endregion

        #region Internals ?????????????????????????????????????

        /// <summary>
        /// Picks a random clip from <paramref name="clips"/>, avoiding
        /// immediate repetition when the pool has more than one entry,
        /// then plays it with a slight random pitch variation.<br/>
        /// No-op when the array is null or empty.
        /// </summary>
        /// <param name="clips">Pool of clips to choose from.</param>
        /// <param name="lastIndex">
        /// Per-pool tracker of the last index played. Updated on every call
        /// so the same clip is never chosen twice in a row (pool size &gt; 1).
        /// </param>
        private void Play(AudioClip[] clips, ref int lastIndex)
        {
            if (clips == null || clips.Length == 0) return;

            if (_audioSource == null)
            {
                Debug.LogError("[UIAudioService] AudioSource is null — cannot play clip.", this);
                return;
            }

            int index;
            if (clips.Length == 1)
            {
                index = 0;
            }
            else
            {
                // Pick a random index that is different from the last one played.
                do { index = Random.Range(0, clips.Length); }
                while (index == lastIndex);
            }

            AudioClip clip = clips[index];
            if (clip == null) return; // Silently skip null entries inside the array.

            lastIndex = index;

            // Store and restore pitch so a concurrent call cannot leave it altered.
            float prevPitch = _audioSource.pitch;
            _audioSource.pitch = Random.Range(1f - _pitchVariation, 1f + _pitchVariation);
            _audioSource.PlayOneShot(clip);
            _audioSource.pitch = prevPitch;
        }

        #endregion

        #region Validation ????????????????????????????????????

        private void ValidateReferences()
        {
            if (_audioSource == null)
                Debug.LogError("[UIAudioService] AudioSource not found!", this);

            // Soft warnings — the UI works fine with empty arrays.
            if (_clickSounds      == null || _clickSounds.Length      == 0) Debug.LogWarning("[UIAudioService] _clickSounds is empty.",      this);
            if (_toggleSounds     == null || _toggleSounds.Length     == 0) Debug.LogWarning("[UIAudioService] _toggleSounds is empty.",     this);
            if (_menuOpenSounds   == null || _menuOpenSounds.Length   == 0) Debug.LogWarning("[UIAudioService] _menuOpenSounds is empty.",   this);
            if (_confirmSounds    == null || _confirmSounds.Length    == 0) Debug.LogWarning("[UIAudioService] _confirmSounds is empty.",    this);
            if (_cancelSounds     == null || _cancelSounds.Length     == 0) Debug.LogWarning("[UIAudioService] _cancelSounds is empty.",     this);
            if (_slotSelectSounds == null || _slotSelectSounds.Length == 0) Debug.LogWarning("[UIAudioService] _slotSelectSounds is empty.", this);
            if (_photoSounds      == null || _photoSounds.Length      == 0) Debug.LogWarning("[UIAudioService] _photoSounds is empty.",      this);
        }

        #endregion
    }
}
