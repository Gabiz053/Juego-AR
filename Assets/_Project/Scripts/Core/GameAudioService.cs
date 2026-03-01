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
    /// Any system that needs to play SFX (block placement, destruction,
    /// UI feedback, etc.) can reference this service instead of owning
    /// its own <see cref="AudioSource"/>.<br/>
    /// Attach to a persistent GameObject (e.g. <c>XR Origin</c>) that
    /// already has an <see cref="AudioSource"/> component.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Core/Game Audio Service")]
    public class GameAudioService : MonoBehaviour
    {
        #region Inspector ─────────────────────────────────────

        [Header("Pitch Variation")]
        [Tooltip("Maximum random offset applied to pitch (±). 0 = no variation.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _pitchVariation = 0.15f;

        #endregion

        #region Cached Components ─────────────────────────────

        private AudioSource _audioSource;

        #endregion

        #region Unity Lifecycle ────────────────────────────────

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();

            // Sensible defaults for a UI/SFX audio source.
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;

            Debug.Log("[GameAudioService] Awake — AudioSource cached and configured.");
        }

        private void Start()
        {
            ValidateReferences();
            Debug.Log("[GameAudioService] Initialized.");
        }

        #endregion

        #region Public API ────────────────────────────────────

        /// <summary>
        /// Plays a one-shot audio clip with slight random pitch variation.
        /// Safe to call rapidly — clips overlap naturally via <c>PlayOneShot</c>.
        /// </summary>
        /// <param name="clip">The <see cref="AudioClip"/> to play. Ignored if <c>null</c>.</param>
        public void PlayOneShot(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.LogWarning("[GameAudioService] PlayOneShot called with null clip — ignoring.");
                return;
            }

            if (_audioSource == null)
            {
                Debug.LogError("[GameAudioService] AudioSource is missing!", this);
                return;
            }

            _audioSource.pitch = Random.Range(1f - _pitchVariation, 1f + _pitchVariation);
            _audioSource.PlayOneShot(clip);

            Debug.Log($"[GameAudioService] Playing: {clip.name} (pitch: {_audioSource.pitch:F2}).");
        }

        /// <summary>
        /// Plays a one-shot clip with an explicit volume override.
        /// </summary>
        /// <param name="clip">The <see cref="AudioClip"/> to play.</param>
        /// <param name="volumeScale">Volume multiplier (0–1).</param>
        public void PlayOneShot(AudioClip clip, float volumeScale)
        {
            if (clip == null)
            {
                Debug.LogWarning("[GameAudioService] PlayOneShot called with null clip — ignoring.");
                return;
            }

            if (_audioSource == null)
            {
                Debug.LogError("[GameAudioService] AudioSource is missing!", this);
                return;
            }

            _audioSource.pitch = Random.Range(1f - _pitchVariation, 1f + _pitchVariation);
            _audioSource.PlayOneShot(clip, volumeScale);

            Debug.Log($"[GameAudioService] Playing: {clip.name} (pitch: {_audioSource.pitch:F2}, vol: {volumeScale:F2}).");
        }

        /// <summary>Current pitch variation range (±).</summary>
        public float PitchVariation => _pitchVariation;

        #endregion

        #region Validation ────────────────────────────────────

        /// <summary>
        /// Logs errors for any missing components at startup.
        /// </summary>
        private void ValidateReferences()
        {
            if (_audioSource == null)
                Debug.LogError("[GameAudioService] AudioSource component not found!", this);
        }

        #endregion
    }
}
