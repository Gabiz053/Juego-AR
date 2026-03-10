// ──────────────────────────────────────────────
//  GameAudioService.cs  ·  _Project.Scripts.Core
//  Centralised one-shot audio playback with pitch variation.
// ──────────────────────────────────────────────

using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Lightweight audio helper that plays one-shot sound effects with
    /// optional random pitch variation for organic feedback.<br/>
    /// Accepts either a single <see cref="AudioClip"/> or an
    /// <see cref="AudioClip"/> array — when an array is supplied, a clip
    /// is chosen at random while avoiding immediate back-to-back repetition.<br/>
    /// Any system that needs to play SFX (block placement, destruction,
    /// UI feedback, etc.) can reference this service instead of owning
    /// its own <see cref="AudioSource"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Core/Game Audio Service")]
    public class GameAudioService : MonoBehaviour
    {
        #region Inspector ─────────────────────────────────────

        [Header("Dependencies")]
        [Tooltip("Dedicated AudioSource for SFX playback. Assign manually in the Inspector.")]
        [SerializeField] private AudioSource _audioSource;

        [Header("Pitch Variation")]
        [Tooltip("Maximum random offset applied to pitch (±). 0 = no variation.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _pitchVariation = 0.15f;

        #endregion

        #region Cached Components ─────────────────────────────

        // Tracks the last clip index played from an array to avoid
        // immediate back-to-back repetition when the pool has > 1 entry.
        private int _lastArrayIndex = -1;

        #endregion

        #region Unity Lifecycle ────────────────────────────────

        private void Awake()
        {
            if (_audioSource != null)
            {
                // Sensible defaults for a UI/SFX audio source.
                _audioSource.playOnAwake  = false;
                _audioSource.spatialBlend = 0f;
            }

            Debug.Log("[GameAudioService] Awake — AudioSource configured.");
        }

        private void Start()
        {
            ValidateReferences();
            Debug.Log("[GameAudioService] Initialized.");
        }

        #endregion

        #region Public API ────────────────────────────────────

        /// <summary>
        /// Plays a single clip with random pitch variation.
        /// No-op if <paramref name="clip"/> is <c>null</c>.
        /// </summary>
        public void PlayOneShot(AudioClip clip)
        {
            if (clip == null) return;
            PlayInternal(clip);
        }

        /// <summary>
        /// Plays a single clip with random pitch variation and an explicit volume scale.
        /// No-op if <paramref name="clip"/> is <c>null</c>.
        /// </summary>
        public void PlayOneShot(AudioClip clip, float volumeScale)
        {
            if (clip == null) return;
            PlayInternal(clip, volumeScale);
        }

        /// <summary>
        /// Picks a random clip from <paramref name="clips"/> — avoiding
        /// immediate repetition when the pool has more than one entry —
        /// then plays it with random pitch variation.<br/>
        /// No-op if the array is <c>null</c> or empty.
        /// </summary>
        public void PlayOneShot(AudioClip[] clips)
        {
            AudioClip clip = PickRandom(clips);
            if (clip == null) return;
            PlayInternal(clip);
        }

        /// <summary>
        /// Picks a random clip from <paramref name="clips"/> and plays it
        /// with random pitch variation and an explicit volume scale.<br/>
        /// No-op if the array is <c>null</c> or empty.
        /// </summary>
        public void PlayOneShot(AudioClip[] clips, float volumeScale)
        {
            AudioClip clip = PickRandom(clips);
            if (clip == null) return;
            PlayInternal(clip, volumeScale);
        }

        /// <summary>Current pitch variation range (±).</summary>
        public float PitchVariation => _pitchVariation;

        #endregion

        #region Internals ─────────────────────────────────────

        /// <summary>
        /// Picks a random clip from the pool, advancing past the last-played
        /// index when the pool has more than one entry to avoid repetition.
        /// Returns <c>null</c> when the array is null, empty, or contains
        /// only null entries.
        /// </summary>
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

            AudioClip clip = clips[index];
            if (clip == null) return null;

            _lastArrayIndex = index;
            return clip;
        }

        /// <summary>Applies pitch variation and fires PlayOneShot at full volume.</summary>
        private void PlayInternal(AudioClip clip)
        {
            if (_audioSource == null)
            {
                Debug.LogError("[GameAudioService] AudioSource is missing!", this);
                return;
            }

            float prevPitch = _audioSource.pitch;
            _audioSource.pitch = Random.Range(1f - _pitchVariation, 1f + _pitchVariation);
            _audioSource.PlayOneShot(clip);
            _audioSource.pitch = prevPitch;

            Debug.Log($"[GameAudioService] Playing: {clip.name} (pitch: {_audioSource.pitch:F2}).");
        }

        /// <summary>Applies pitch variation and fires PlayOneShot with a volume scale.</summary>
        private void PlayInternal(AudioClip clip, float volumeScale)
        {
            if (_audioSource == null)
            {
                Debug.LogError("[GameAudioService] AudioSource is missing!", this);
                return;
            }

            float prevPitch = _audioSource.pitch;
            _audioSource.pitch = Random.Range(1f - _pitchVariation, 1f + _pitchVariation);
            _audioSource.PlayOneShot(clip, volumeScale);
            _audioSource.pitch = prevPitch;

            Debug.Log($"[GameAudioService] Playing: {clip.name} (pitch: {_audioSource.pitch:F2}, vol: {volumeScale:F2}).");
        }

        #endregion

        #region Validation ────────────────────────────────────

        private void ValidateReferences()
        {
            if (_audioSource == null)
                Debug.LogError("[GameAudioService] AudioSource component not found!", this);
        }

        #endregion
    }
}
