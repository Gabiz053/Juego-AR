// ??????????????????????????????????????????????
//  BlockDestroy.cs  ·  _Project.Scripts.Voxel
//  Detects when the AR camera enters the block's personal space,
//  then ejects it from the WorldContainer, enables gravity and
//  destroys it after a short tumble — mimicking a physical collision.
//  Also triggered directly by the Destroy tool via BreakFromTool().
//  Attach directly to each block prefab in the Inspector.
// ??????????????????????????????????????????????

using System.Collections;
using UnityEngine;
using _Project.Scripts.Core;

namespace _Project.Scripts.Voxel
{
    /// <summary>
    /// Handles block destruction with physics feedback.<br/>
    /// Attach to every block prefab.<br/>
    /// Caches <c>Camera.main</c> and its own <see cref="VoxelBlock"/> in <c>Awake</c>.<br/>
    /// <see cref="Interaction.ARBlockPlacer"/> calls <see cref="InjectSharedRefs"/> once after
    /// instantiation to forward the break VFX prefab and audio service.<br/>
    /// When the camera enters within <see cref="_knockRadius"/> the block:<br/>
    /// 1. Unparents from <c>WorldContainer</c>.<br/>
    /// 2. Gains a <see cref="Rigidbody"/> with gravity.<br/>
    /// 3. Receives a random upward-forward kick.<br/>
    /// 4. Plays the break sound and spawns break VFX.<br/>
    /// 5. Destroys itself after <see cref="_destroyDelay"/> seconds.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Voxel/Block Destroy")]
    public class BlockDestroy : MonoBehaviour
    {
        #region Inspector ?????????????????????????????????????

        [Header("Detection")]
        [Tooltip("Distance (metres, world space) at which the camera is considered to have collided with the block.")]
        [SerializeField] private float _knockRadius = 0.18f;

        [Header("Physics")]
        [Tooltip("Magnitude of the random impulse applied when the block is knocked.")]
        [SerializeField] private float _knockForce = 2.5f;

        [Tooltip("Seconds the block tumbles before the shrink begins.")]
        [SerializeField] private float _destroyDelay = 0.12f;

        [Tooltip("Seconds the block takes to shrink to zero after tumbling.")]
        [SerializeField] private float _shrinkDuration = 0.18f;

        // Break VFX and audio come from ARBlockPlacer via InjectSharedRefs —
        // they are NOT serialized here to avoid duplicating references across
        // every block prefab.

        #endregion

        #region Cached references ?????????????????????????????

        private Transform        _camera;
        private VoxelBlock       _voxelBlock;
        private GameObject       _breakVfxPrefab;
        private GameAudioService _audioService;
        private bool             _knocked;
        private bool             _ready;   // set by BlockSpawn when the animation finishes

        #endregion

        #region Unity Lifecycle ????????????????????????????????

        private void Awake()
        {
            _camera     = Camera.main != null ? Camera.main.transform : null;
            _voxelBlock = GetComponent<VoxelBlock>();

            if (_camera == null)
                Debug.LogWarning("[BlockDestroy] Camera.main not found — knockback disabled.", this);
        }

        private void Update()
        {
            if (!_ready || _knocked || _camera == null) return;

            float sqrDist = (transform.position - _camera.position).sqrMagnitude;
            if (sqrDist <= _knockRadius * _knockRadius)
                StartCoroutine(KnockRoutine());
        }

        #endregion

        #region Public API ????????????????????????????????????

        /// <summary>
        /// Called once by <see cref="Interaction.ARBlockPlacer"/> right after
        /// instantiation to forward the shared break VFX prefab and audio service.
        /// </summary>
        public void InjectSharedRefs(GameObject breakVfxPrefab, GameAudioService audioService)
        {
            _breakVfxPrefab = breakVfxPrefab;
            _audioService   = audioService;
        }

        /// <summary>
        /// Called by <see cref="BlockSpawn"/> when the spawn animation has fully
        /// settled. Until this is called the proximity check is suppressed so the
        /// block cannot be knocked back while it is still flying in from the camera.
        /// </summary>
        public void SetReady()
        {
            _ready = true;
        }

        /// <summary>
        /// Triggers the physics-based break immediately (used by the Destroy tool).<br/>
        /// Kicks the block away from <paramref name="hitDirection"/>, plays audio + VFX,
        /// and destroys the GameObject after <see cref="_destroyDelay"/> seconds.
        /// </summary>
        public void BreakFromTool(Vector3 hitDirection)
        {
            if (_knocked) return;
            StartCoroutine(KnockRoutine(hitDirection));
        }

        #endregion

        #region Knockback coroutine ???????????????????????????

        private IEnumerator KnockRoutine() => KnockRoutine(null);

        private IEnumerator KnockRoutine(Vector3? overrideDirection)
        {
            _knocked = true;

            // Audio + VFX
            if (_audioService != null && _voxelBlock != null)
                _audioService.PlayOneShot(_voxelBlock.BreakSounds);

            if (_breakVfxPrefab != null)
                Instantiate(_breakVfxPrefab, transform.position, Quaternion.identity);

            // Unparent from WorldContainer
            transform.SetParent(null, worldPositionStays: true);

            // Add Rigidbody with gravity
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.mass      = 1f;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Kick direction: supplied override or away from camera
            Vector3 baseDir = overrideDirection.HasValue
                ? overrideDirection.Value.normalized
                : (_camera != null
                    ? (transform.position - _camera.position).normalized
                    : Random.insideUnitSphere.normalized);

            Vector3 kickDir = (baseDir + Vector3.up * 0.6f).normalized;
            rb.AddForce(kickDir * _knockForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * _knockForce * 3f, ForceMode.Impulse);

            enabled = false;

            // Let the block tumble freely.
            yield return new WaitForSeconds(_destroyDelay);

            // Freeze physics exactly where it landed — no position or rotation change.
            if (rb != null)
            {
                rb.linearVelocity  = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic     = true;
            }

            // Shrink uniformly from whatever scale/rotation the block settled at.
            Vector3 startScale = transform.localScale;
            float   elapsed    = 0f;

            while (elapsed < _shrinkDuration)
            {
                elapsed += Time.deltaTime;
                float t     = Mathf.Clamp01(elapsed / _shrinkDuration);
                float eased = t * t;   // ease-in — accelerates into nothing
                transform.localScale = Vector3.LerpUnclamped(startScale, Vector3.zero, eased);
                yield return null;
            }

            Destroy(gameObject);
        }

        #endregion
    }
}
