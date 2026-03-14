// ------------------------------------------------------------
//  MusicService.cs  -  _Project.Scripts.Core
//  Background music player: shuffles tracks, loops indefinitely,
//  and exposes a runtime volume control.
// ------------------------------------------------------------

using System;
using System.Collections;
using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Manages background music playback for the entire session.<br/>
    /// Shuffles the assigned track list and plays them in a continuous
    /// loop, fading between tracks.  Exposes <see cref="SetVolume"/>
    /// so the options menu slider can control volume at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Core/Music Service")]
    public class MusicService : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Tracks")]
        [Tooltip("All background music tracks. Shuffled and looped automatically.")]
        [SerializeField] private AudioClip[] _tracks;

        [Header("Playback")]
        [Tooltip("Starting volume for background music (0-1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _initialVolume = 0.4f;

        [Tooltip("Crossfade duration in seconds between tracks.")]
        [Range(0f, 5f)]
        [SerializeField] private float _crossfadeDuration = 2f;

        [Tooltip("When true, music starts playing on scene load.")]
        [SerializeField] private bool _playOnStart = true;

        [Header("Dependencies")]
        [Tooltip("Dedicated AudioSource for music (separate from GameAudioService).")]
        [SerializeField] private AudioSource _audioSource;

        #endregion

        #region Events --------------------------------------------

        /// <summary>Raised whenever the volume changes (0-1).</summary>
        public event Action<float> OnVolumeChanged;

        #endregion

        #region State ---------------------------------------------

        private int[] _shuffledOrder;
        private int   _currentTrackIndex;

        #endregion

        #region Public API ----------------------------------------

        /// <summary>Current music volume (0-1).</summary>
        public float Volume { get; private set; }

        /// <summary>
        /// Sets the music volume (0-1).<br/>
        /// Called directly by the UI Slider's <c>OnValueChanged</c> event.
        /// Setting to 0 silences the music without stopping playback.
        /// </summary>
        /// <param name="volume">Target volume, clamped to [0, 1].</param>
        public void SetVolume(float volume)
        {
            Volume = Mathf.Clamp01(volume);
            if (_audioSource != null)
                _audioSource.volume = Volume;

            OnVolumeChanged?.Invoke(Volume);
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            if (_audioSource != null)
            {
                _audioSource.playOnAwake  = false;
                _audioSource.loop         = false;
                _audioSource.spatialBlend = 0f;
            }
        }

        private void Start()
        {
            ValidateReferences();

            if (_tracks == null || _tracks.Length == 0) return;

            SetVolume(_playOnStart ? _initialVolume : 0f);
            BuildShuffledOrder();

            if (_playOnStart)
                StartCoroutine(PlaybackLoop());
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Infinite loop that plays tracks in shuffled order with
        /// crossfade transitions between them.
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

                _audioSource.clip = track;
                _audioSource.Play();
                Debug.Log($"[MusicService] Now playing: {track.name} ({_currentTrackIndex + 1}/{_tracks.Length}).");
                
                float waitTime = track.length - _crossfadeDuration;
                if (waitTime > 0f)
                    yield return new WaitForSeconds(waitTime);

                float targetVolume = Volume;
                yield return StartCoroutine(FadeVolume(_audioSource.volume, 0f, _crossfadeDuration));
                AdvanceTrack();
                yield return StartCoroutine(FadeVolume(0f, targetVolume, _crossfadeDuration));
            }
        }

        /// <summary>Smoothly interpolates <see cref="AudioSource.volume"/> over <paramref name="duration"/> seconds.</summary>
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

        /// <summary>Moves to the next shuffled track, re-shuffling when the list wraps.</summary>
        private void AdvanceTrack()
        {
            _currentTrackIndex++;
            if (_currentTrackIndex >= _shuffledOrder.Length)
            {
                BuildShuffledOrder();
                _currentTrackIndex = 0;
            }
        }

        /// <summary>Fisher-Yates shuffle -- O(n), unbiased.</summary>
        private void BuildShuffledOrder()
        {
            _shuffledOrder = new int[_tracks.Length];
            for (int i = 0; i < _tracks.Length; i++)
                _shuffledOrder[i] = i;

            for (int i = _tracks.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (_shuffledOrder[i], _shuffledOrder[j]) = (_shuffledOrder[j], _shuffledOrder[i]);
            }
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_audioSource == null)
                Debug.LogWarning("[MusicService] _audioSource is not assigned.", this);
            if (_tracks == null || _tracks.Length == 0)
                Debug.LogWarning("[MusicService] _tracks is not assigned.", this);
        }

        #endregion
    }
}
