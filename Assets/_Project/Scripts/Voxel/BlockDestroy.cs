// ------------------------------------------------------------
//  BlockDestroy.cs  -  _Project.Scripts.Voxel
//  Proximity knockback and tool-triggered destruction with
//  physics tumble, shrink and VFX feedback.
// ------------------------------------------------------------

using System.Collections;
using UnityEngine;
using _Project.Scripts.Core;

namespace _Project.Scripts.Voxel
{
    /// <summary>
    /// Handles block destruction with physics feedback.<br/>
    /// Caches <c>Camera.main</c> in <c>Awake</c>.
    /// <see cref="Interaction.ARBlockPlacer"/> calls <see cref="InjectSharedRefs"/>
    /// once after instantiation to provide VFX and audio refs.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Voxel/Block Destroy")]
    public class BlockDestroy : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Detection")]
        [Tooltip("Distance (metres) at which the camera knocks the block.")]
        [SerializeField] private float _knockRadius = 0.18f;

        [Header("Physics")]
        [Tooltip("Impulse magnitude applied when the block is knocked.")]
        [SerializeField] private float _knockForce = 2.5f;

        [Tooltip("Seconds the block tumbles before shrink begins.")]
        [SerializeField] private float _destroyDelay = 0.12f;

        [Tooltip("Seconds the block takes to shrink to zero.")]
        [SerializeField] private float _shrinkDuration = 0.18f;

        #endregion

        #region State ---------------------------------------------

        private Transform        _camera;
        private VoxelBlock       _voxelBlock;
        private GameObject       _breakVfxPrefab;
        private GameAudioService _audioService;
        private AudioClip[]      _breakSounds;
        private bool             _knocked;
        private bool             _ready;

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            _camera     = Camera.main != null ? Camera.main.transform : null;
            _voxelBlock = GetComponent<VoxelBlock>();
        }

        private void Update()
        {
            if (!_ready || _knocked || _camera == null) return;

            float sqrDist = (transform.position - _camera.position).sqrMagnitude;
            if (sqrDist <= _knockRadius * _knockRadius)
                StartCoroutine(KnockRoutine(null));
        }

        #endregion

        #region Public API ----------------------------------------

        /// <summary>True once <see cref="SetReady"/> has been called.</summary>
        public bool IsReady => _ready;

        /// <summary>Marks the block ready for proximity detection.</summary>
        public void SetReady() => _ready = true;

        /// <summary>Injects shared VFX and audio refs (blocks).</summary>
        public void InjectSharedRefs(GameObject breakVfxPrefab, GameAudioService audioService)
        {
            _breakVfxPrefab = breakVfxPrefab;
            _audioService   = audioService;
        }

        /// <summary>Injects shared refs with extra break sounds (pebbles).</summary>
        public void InjectSharedRefs(GameObject breakVfxPrefab, GameAudioService audioService,
                                     AudioClip[] breakSounds)
        {
            _breakVfxPrefab = breakVfxPrefab;
            _audioService   = audioService;
            _breakSounds    = breakSounds;
        }

        /// <summary>
        /// Triggers physics break immediately (Destroy tool).
        /// </summary>
        public void BreakFromTool(Vector3 hitDirection)
        {
            if (_knocked) return;
            StartCoroutine(KnockRoutine(hitDirection));
        }

        #endregion

        #region Internals -----------------------------------------

        private IEnumerator KnockRoutine(Vector3? overrideDirection)
        {
            _knocked = true;

            // Audio
            if (_audioService != null)
            {
                AudioClip[] clips = _voxelBlock != null ? _voxelBlock.BreakSounds : _breakSounds;
                if (clips != null && clips.Length > 0)
                    _audioService.PlayOneShot(clips);
            }

            // VFX
            if (_breakVfxPrefab != null)
                Instantiate(_breakVfxPrefab, transform.position, Quaternion.identity);

            // Unparent and add physics
            transform.SetParent(null, worldPositionStays: true);

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

            rb.isKinematic            = false;
            rb.mass                   = 1f;
            rb.useGravity             = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            Collider ownCollider = GetComponent<Collider>();
            if (ownCollider != null) ownCollider.enabled = true;

            // Kick direction
            Vector3 kickDir;
            if (overrideDirection.HasValue)
            {
                Vector3 outward = overrideDirection.Value.normalized;
                kickDir = (outward + Vector3.up * 0.5f).normalized;
            }
            else
            {
                Vector3 awayFromCam = _camera != null
                    ? (transform.position - _camera.position).normalized
                    : Random.insideUnitSphere.normalized;
                kickDir = (awayFromCam + Vector3.up * 0.6f).normalized;
            }

            Vector3 spread = new Vector3(
                Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
            kickDir = (kickDir + spread).normalized;

            rb.AddForce(kickDir * _knockForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * _knockForce * 3f, ForceMode.Impulse);

            enabled = false;

            yield return new WaitForSeconds(_destroyDelay);

            if (rb != null)
            {
                rb.linearVelocity  = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic     = true;
            }

            // Shrink to zero
            Vector3 startScale = transform.localScale;
            float   elapsed    = 0f;

            while (elapsed < _shrinkDuration)
            {
                elapsed += Time.deltaTime;
                float t     = Mathf.Clamp01(elapsed / _shrinkDuration);
                float eased = t * t;
                transform.localScale = Vector3.LerpUnclamped(startScale, Vector3.zero, eased);
                yield return null;
            }

            Destroy(gameObject);
        }

        #endregion
    }
}
