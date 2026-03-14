// ------------------------------------------------------------
//  BlockSpawn.cs  -  _Project.Scripts.Voxel
//  Fly-in animation + placement feedback (audio + VFX).
// ------------------------------------------------------------

using System;
using System.Collections;
using UnityEngine;
using _Project.Scripts.Infrastructure;

namespace _Project.Scripts.Voxel
{
    /// <summary>
    /// One-shot spawn animation: fly from camera to grid position with
    /// an overshoot settle.  Also plays placement audio and VFX so all
    /// feedback lives on the prefab � callers don't need to know about it.<br/>
    /// Reads place sounds from <see cref="VoxelBlock.PlaceSounds"/> when
    /// present; falls back to <see cref="_placeSounds"/> for pebbles.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Voxel/Block Spawn")]
    public class BlockSpawn : MonoBehaviour
    {
        #region Constants -----------------------------------------

        /// <summary>Fraction of total duration used for the fly-in phase.</summary>
        private const float FLY_IN_PHASE_RATIO = 0.80f;

        /// <summary>Fraction of total duration used for the settle phase.</summary>
        private const float SETTLE_PHASE_RATIO = 0.20f;

        #endregion

        #region Inspector -----------------------------------------

        [Header("Timing")]
        [Tooltip("Total animation duration (seconds).")]
        [SerializeField] private float _duration = 0.18f;

        [Header("Scale")]
        [Tooltip("Overshoot scale factor at the peak before settling to 1.")]
        [SerializeField] private float _peakScale = 1.15f;

        [Header("Origin Offset")]
        [Tooltip("Offset in camera-local space for the animation start point.")]
        [SerializeField] private Vector3 _cameraLocalOffset = new Vector3(0f, -0.12f, 0.15f);

        [Header("Place Feedback")]
        [Tooltip("VFX prefab spawned at placement position.\nLeave empty if this prefab has no place VFX.")]
        [SerializeField] private GameObject _placeVfxPrefab;

        [Header("Audio Override")]
        [Tooltip("Place sounds used when no VoxelBlock component is present (pebbles).\nLeave empty on voxel block prefabs � they read from VoxelBlock.PlaceSounds.")]
        [SerializeField] private AudioClip[] _placeSounds;

        #endregion

        #region State ---------------------------------------------

        private VoxelBlock        _voxelBlock;
        private BlockDestroy      _blockDestroy;
        private Collider          _collider;
        private IGameAudioService _audioService;
        private IHapticService    _hapticService;

        #endregion

        #region Public API ----------------------------------------

        /// <summary>
        /// Kicks off the spawn animation.  <paramref name="onComplete"/> is
        /// invoked when the block has settled (releases the pending cell).
        /// Place audio and VFX are triggered at the end of the animation.
        /// </summary>
        public void Play(Transform cameraTransform, Action onComplete = null)
        {
            StartCoroutine(SpawnCoroutine(cameraTransform, onComplete));
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            _voxelBlock   = GetComponent<VoxelBlock>();
            _blockDestroy = GetComponent<BlockDestroy>();
            _collider     = GetComponent<Collider>();
            ServiceLocator.TryGet<IGameAudioService>(out _audioService);
            ServiceLocator.TryGet<IHapticService>(out _hapticService);
        }

        private void Start()
        {
            ValidateReferences();
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Two-phase animation: fly-in with scale overshoot, then settle.
        /// Plays place audio and VFX once the block reaches its final position.
        /// </summary>
        private IEnumerator SpawnCoroutine(Transform cameraTransform, Action onComplete)
        {
            if (_blockDestroy != null) _blockDestroy.enabled = false;
            if (_collider != null) _collider.enabled = false;

            // Haptic fires immediately so the player feels the tap response
            // at the moment they touch, not after the fly-in completes.
            _hapticService?.VibrateLight();

            Vector3 finalLocal = transform.localPosition;

            Vector3 startWorld = cameraTransform != null
                ? cameraTransform.TransformPoint(_cameraLocalOffset)
                : transform.position;
            Vector3 startLocal = transform.parent != null
                ? transform.parent.InverseTransformPoint(startWorld)
                : startWorld;

            // Phase 1 (80%): fly in + scale 0 -> peakScale
            float phase1  = _duration * FLY_IN_PHASE_RATIO;
            float elapsed = 0f;

            while (elapsed < phase1)
            {
                elapsed += Time.deltaTime;
                float t     = Mathf.Clamp01(elapsed / phase1);
                float eased = 1f - Mathf.Pow(1f - t, 3f);

                transform.localPosition = Vector3.LerpUnclamped(startLocal, finalLocal, eased);
                transform.localScale    = Vector3.one * Mathf.LerpUnclamped(0f, _peakScale, eased);
                yield return null;
            }

            transform.localPosition = finalLocal;
            transform.localScale    = Vector3.one * _peakScale;

            // Phase 2 (20%): settle peakScale -> 1
            float phase2 = _duration * SETTLE_PHASE_RATIO;
            elapsed = 0f;

            while (elapsed < phase2)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / phase2);
                transform.localScale = Vector3.one * Mathf.Lerp(_peakScale, 1f, t * t);
                yield return null;
            }

            transform.localPosition = finalLocal;
            transform.localScale    = Vector3.one;

            if (_collider != null) _collider.enabled = true;

            if (_blockDestroy != null)
            {
                _blockDestroy.enabled = true;
                _blockDestroy.SetReady();
            }

            PlayPlaceFeedback();

            onComplete?.Invoke();
            enabled = false;
        }

        /// <summary>
        /// Fires place VFX and plays a random place sound.
        /// Reads from <see cref="VoxelBlock.PlaceSounds"/> when
        /// present; falls back to <see cref="_placeSounds"/> for pebbles.
        /// Haptic feedback is handled at the start of the spawn animation.
        /// </summary>
        private void PlayPlaceFeedback()
        {
            if (_placeVfxPrefab != null)
            {
                Vector3 worldPos = transform.parent != null
                    ? transform.parent.TransformPoint(transform.localPosition)
                    : transform.position;
                Instantiate(_placeVfxPrefab, worldPos, Quaternion.identity);
            }

            if (_audioService != null)
            {
                AudioClip[] clips = _voxelBlock != null ? _voxelBlock.PlaceSounds : _placeSounds;
                if (clips != null && clips.Length > 0)
                    _audioService.PlayOneShot(clips);
            }
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_audioService == null)
                Debug.LogWarning("[BlockSpawn] _audioService is not assigned.", this);
            if (_collider == null)
                Debug.LogWarning("[BlockSpawn] _collider is not assigned.", this);
        }

        #endregion
    }
}
