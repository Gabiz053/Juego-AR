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
        private AudioClip[]      _breakSounds;   // injected by PlowTool for pebbles without VoxelBlock
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
        /// Overload used by <see cref="Interaction.PlowTool"/> for pebbles that have no
        /// <see cref="VoxelBlock"/> component. The extra <paramref name="breakSounds"/>
        /// array is played on destruction instead of <c>VoxelBlock.BreakSounds</c>.
        /// </summary>
        public void InjectSharedRefs(GameObject breakVfxPrefab, GameAudioService audioService,
                                     AudioClip[] breakSounds)
        {
            _breakVfxPrefab = breakVfxPrefab;
            _audioService   = audioService;
            _breakSounds    = breakSounds;
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

        /// <summary>True once <see cref="SetReady"/> has been called by <see cref="BlockSpawn"/>.</summary>
        public bool IsReady => _ready;

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

            // Audio — prefer VoxelBlock clips, fall back to injected clips (pebbles).
            if (_audioService != null)
            {
                AudioClip[] clips = (_voxelBlock != null) ? _voxelBlock.BreakSounds : _breakSounds;
                if (clips != null && clips.Length > 0)
                    _audioService.PlayOneShot(clips);
            }

            if (_breakVfxPrefab != null)
                Instantiate(_breakVfxPrefab, transform.position, Quaternion.identity);

            transform.SetParent(null, worldPositionStays: true);

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

            rb.isKinematic            = false;
            rb.mass                   = 1f;
            rb.useGravity             = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Re-enable the collider NOW (before the impulse) so the block
            // can collide with the AR ground plane and other objects.
            Collider ownCollider = GetComponent<Collider>();
            if (ownCollider != null) ownCollider.enabled = true;

            // Kick direction: always computed in world space from camera to block
            // so it is not affected by WorldContainer scale or rotation.
            // If an explicit override is supplied (e.g. from BreakFromTool) we
            // convert it from the hit-normal convention (away from surface) to
            // a world-space kick that adds an upward component.
            Vector3 kickDir;
            if (overrideDirection.HasValue)
            {
                // overrideDirection is the hit normal in world space (away from block).
                // Use it directly as the outward direction and add upward bias.
                Vector3 outward = overrideDirection.Value.normalized;
                kickDir = (outward + Vector3.up * 0.5f).normalized;
            }
            else
            {
                // Proximity knock: push away from the camera with upward bias.
                Vector3 awayFromCam = _camera != null
                    ? (transform.position - _camera.position).normalized
                    : Random.insideUnitSphere.normalized;
                kickDir = (awayFromCam + Vector3.up * 0.6f).normalized;
            }

            // Add a small random lateral spread so blocks don't all fly the same way.
            Vector3 spread = new Vector3(
                Random.Range(-0.3f, 0.3f),
                0f,
                Random.Range(-0.3f, 0.3f));
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
