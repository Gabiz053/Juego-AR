// ??????????????????????????????????????????????
//  MusicService.cs  ·  _Project.Scripts.Core
//  Background music player: shuffles tracks, loops indefinitely,
//  and exposes a runtime volume control.
// ??????????????????????????????????????????????

using System;
using System.Collections;
using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Manages background music playback for the entire session.<br/>
    /// Shuffles the assigned track list and plays them one after another
    /// in a continuous loop, fading between tracks.<br/>
    /// Exposes <see cref="SetVolume"/> so the options menu slider can
    /// control music volume at runtime (0 = silent, 1 = full).<br/>
    /// Attach to <c>XR Origin (Mobile AR)</c>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Core/Music Service")]
    public class MusicService : MonoBehaviour
    {
        #region Inspector ?????????????????????????????????????

        [Header("Tracks")]
        [Tooltip("All background music tracks. They will be shuffled and looped automatically.")]
        [SerializeField] private AudioClip[] _tracks;

        [Header("Playback")]
        [Tooltip("Starting volume for background music (0–1). The UI slider overrides this at runtime.")]
        [Range(0f, 1f)]
        [SerializeField] private float _initialVolume = 0.4f;

        [Tooltip("Fade duration in seconds when crossfading between tracks.")]
        [Range(0f, 5f)]
        [SerializeField] private float _crossfadeDuration = 2f;

        [Header("Initial State")]
        [Tooltip("When true, music starts playing as soon as the scene loads.")]
        [SerializeField] private bool _playOnStart = true;

        [Header("Dependencies")]
        [Tooltip("Dedicated AudioSource for music. Must be a different AudioSource than the one used by GameAudioService. Assign manually in the Inspector.")]
        [SerializeField] private AudioSource _audioSource;

        #endregion

        #region Events ????????????????????????????????????????

        /// <summary>
        /// Raised whenever the volume changes.
        /// The <see langword="float"/> parameter is the new volume (0–1).
        /// Useful for syncing the UI slider to an externally driven value.
        /// </summary>
        public event Action<float> OnVolumeChanged;

        #endregion

        #region Public API ????????????????????????????????????

        /// <summary>Current music volume (0–1). 0 means silent but still playing internally.</summary>
        public float Volume { get; private set; }

        /// <summary>
        /// Sets the music volume to <paramref name="volume"/> (0–1).<br/>
        /// Called directly by the UI Slider's <c>OnValueChanged</c> event.
        /// Setting to 0 silences the music without stopping playback.
        /// </summary>
        /// <param name="volume">Target volume, clamped to [0, 1].</param>
        public void SetVolume(float volume)
        {
            Volume = Mathf.Clamp01(volume);
            _audioSource.volume = Volume;

            OnVolumeChanged?.Invoke(Volume);
            Debug.Log($"[MusicService] Volume set to {Volume:F2}.");
        }

        #endregion

        #region Cached Components ?????????????????????????????

        // Shuffled copy of the track indices — rebuilt each full cycle.
        private int[] _shuffledOrder;
        private int   _currentTrackIndex = 0;

        #endregion

        #region Unity Lifecycle ????????????????????????????????

        private void Awake()
        {
            // _audioSource must be assigned in the Inspector to a dedicated AudioSource
            // that is separate from the one GameAudioService uses. Both components live on
            // XR Origin, so GetComponent would return whichever comes first — unreliable.
            if (_audioSource == null)
                Debug.LogError("[MusicService] _audioSource is not assigned in the Inspector. " +
                               "Add a second AudioSource to XR Origin and assign it here.", this);

            if (_audioSource != null)
            {
                _audioSource.playOnAwake  = false;
                _audioSource.loop         = false;
                _audioSource.spatialBlend = 0f;
            }

            Debug.Log("[MusicService] Awake — AudioSource configured.");
        }

        private void Start()
        {
            ValidateReferences();

            if (_tracks == null || _tracks.Length == 0)
            {
                Debug.LogWarning("[MusicService] No tracks assigned — music will not play.");
                return;
            }

            // Apply the initial volume before starting playback.
            SetVolume(_playOnStart ? _initialVolume : 0f);

            BuildShuffledOrder();

            if (_playOnStart)
                StartCoroutine(PlaybackLoop());

            Debug.Log($"[MusicService] Initialized — {_tracks.Length} tracks loaded, playOnStart: {_playOnStart}.");
        }

        #endregion

        #region Playback ??????????????????????????????????????

        /// <summary>
        /// Continuous coroutine that plays tracks one after another,
        /// reshuffling the order at the start of each new cycle.
        /// </summary>
        private IEnumerator PlaybackLoop()
        {
            while (true)
            {
                AudioClip track = _tracks[_shuffledOrder[_currentTrackIndex]];

                if (track == null)
                {
                    AdvanceTrack();
                    continue;
                }

                Debug.Log($"[MusicService] Now playing: {track.name}.");
                _audioSource.clip = track;
                _audioSource.Play();

                // Wait for the track to finish, minus the crossfade window.
                float waitTime = track.length - _crossfadeDuration;
                if (waitTime > 0f)
                    yield return new WaitForSeconds(waitTime);

                // Fade out, then advance, then fade in — but only up to the
                // current user volume so we respect the slider position.
                float targetVolume = Volume;
                yield return StartCoroutine(FadeVolume(_audioSource.volume, 0f, _crossfadeDuration));
                AdvanceTrack();
                yield return StartCoroutine(FadeVolume(0f, targetVolume, _crossfadeDuration));
            }
        }

        /// <summary>
        /// Smoothly transitions the AudioSource volume between two values.
        /// </summary>
        private IEnumerator FadeVolume(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                _audioSource.volume = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _audioSource.volume = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            _audioSource.volume = to;
        }

        /// <summary>
        /// Moves to the next track index, rebuilding the shuffle order
        /// when the current cycle is exhausted.
        /// </summary>
        private void AdvanceTrack()
        {
            _currentTrackIndex++;
            if (_currentTrackIndex >= _shuffledOrder.Length)
            {
                BuildShuffledOrder();
                _currentTrackIndex = 0;
                Debug.Log("[MusicService] Full cycle complete — reshuffling tracks.");
            }
        }

        /// <summary>
        /// Fills <see cref="_shuffledOrder"/> with indices 0..n-1 in a
        /// random order using a Fisher-Yates shuffle.
        /// </summary>
        private void BuildShuffledOrder()
        {
            _shuffledOrder = new int[_tracks.Length];
            for (int i = 0; i < _tracks.Length; i++)
                _shuffledOrder[i] = i;

            // Fisher-Yates shuffle — O(n), unbiased.
            for (int i = _tracks.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (_shuffledOrder[i], _shuffledOrder[j]) = (_shuffledOrder[j], _shuffledOrder[i]);
            }
        }

        #endregion

        #region Validation ????????????????????????????????????

        private void ValidateReferences()
        {
            if (_audioSource == null)
                Debug.LogError("[MusicService] _audioSource is not assigned — assign a dedicated AudioSource in the Inspector!", this);
            if (_tracks == null || _tracks.Length == 0)
                Debug.LogWarning("[MusicService] _tracks array is empty — no music will play.", this);
        }

        #endregion
    }
}
