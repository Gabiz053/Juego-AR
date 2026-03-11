// ------------------------------------------------------------
//  GameAudioService.cs  -  _Project.Scripts.Core
//  Centralised one-shot audio playback with pitch variation.
// ------------------------------------------------------------

using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Plays one-shot sound effects with optional random pitch variation
    /// for organic feedback.  Accepts a single <see cref="AudioClip"/> or
    /// an array (one chosen at random, avoiding immediate repetition).<br/>
    /// Any system that needs SFX can reference this service instead of
    /// owning its own <see cref="AudioSource"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Core/Game Audio Service")]
    public class GameAudioService : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Dependencies")]
        [Tooltip("Dedicated AudioSource for SFX playback.")]
        [SerializeField] private AudioSource _audioSource;

        [Header("Pitch Variation")]
        [Tooltip("Maximum random offset applied to pitch. 0 = no variation.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _pitchVariation = 0.15f;

        #endregion

        #region State ---------------------------------------------

        private int _lastArrayIndex = -1;

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            if (_audioSource != null)
            {
                _audioSource.playOnAwake  = false;
                _audioSource.spatialBlend = 0f;
            }
        }

        private void Start()
        {
            ValidateReferences();
        }

        #endregion

        #region Public API ----------------------------------------

        /// <summary>Plays a clip with random pitch variation at full volume.</summary>
        public void PlayOneShot(AudioClip clip)
        {
            if (clip != null) PlayInternal(clip, 1f);
        }

        /// <summary>Plays a clip with random pitch variation at the given volume.</summary>
        public void PlayOneShot(AudioClip clip, float volumeScale)
        {
            if (clip != null) PlayInternal(clip, volumeScale);
        }

        /// <summary>Picks a random clip from the array and plays it.</summary>
        public void PlayOneShot(AudioClip[] clips)
        {
            AudioClip clip = PickRandom(clips);
            if (clip != null) PlayInternal(clip, 1f);
        }

        /// <summary>Picks a random clip from the array and plays it at the given volume.</summary>
        public void PlayOneShot(AudioClip[] clips, float volumeScale)
        {
            AudioClip clip = PickRandom(clips);
            if (clip != null) PlayInternal(clip, volumeScale);
        }

        #endregion

        #region Internals -----------------------------------------

        private AudioClip PickRandom(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;

            int index;
            if (clips.Length == 1)
            {
                index = 0;
            }
            else
            {
                do { index = Random.Range(0, clips.Length); }
                while (index == _lastArrayIndex);
            }

            _lastArrayIndex = index;
            return clips[index];
        }

        private void PlayInternal(AudioClip clip, float volumeScale)
        {
            if (_audioSource == null) return;

            float prevPitch    = _audioSource.pitch;
            _audioSource.pitch = Random.Range(1f - _pitchVariation, 1f + _pitchVariation);
            _audioSource.PlayOneShot(clip, volumeScale);
            _audioSource.pitch = prevPitch;
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_audioSource == null)
                Debug.LogError("[GameAudioService] _audioSource is not assigned!", this);
        }

        #endregion
    }
}
