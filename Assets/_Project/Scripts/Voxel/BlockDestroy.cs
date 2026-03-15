// ------------------------------------------------------------
//  BlockDestroy.cs  -  _Project.Scripts.Voxel
//  Proximity knockback and tool-triggered destruction.
// ------------------------------------------------------------

using System.Collections;
using UnityEngine;
using _Project.Scripts.Core;
using _Project.Scripts.Infrastructure;
using Random = UnityEngine.Random;

namespace _Project.Scripts.Voxel
{
    /// <summary>
    /// Handles block destruction via proximity knock or tool break.<br/>
    /// Resolves services via <see cref="ServiceLocator"/> in <c>Awake</c>
    /// so prefab instances need no post-instantiate injection.<br/>
    /// Proximity knock only triggers when the player holds the pickaxe
    /// (<see cref="Interaction.ToolType.Tool_Destroy"/>).  Both proximity
    /// and tool-triggered destruction are undoable via
    /// <see cref="DestroyBlockAction"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Voxel/Block Destroy")]
    public class BlockDestroy : MonoBehaviour
    {
        #region Constants -----------------------------------------

        /// <summary>Upward bias added to the camera-away knock direction.</summary>
        private const float KNOCK_UP_BIAS = 0.6f;

        /// <summary>Upward bias added to an overridden knock direction.</summary>
        private const float OVERRIDE_UP_BIAS = 0.5f;

        /// <summary>Horizontal random spread applied to the knock direction.</summary>
        private const float KNOCK_SPREAD = 0.3f;

        /// <summary>Torque multiplier relative to knock force.</summary>
        private const float TORQUE_MULTIPLIER = 3f;

        /// <summary>Mass assigned to the rigidbody during destruction.</summary>
        private const float KNOCK_MASS = 1f;

        #endregion

        #region Inspector -----------------------------------------

        [Header("Detection")]
        [Tooltip("Distance (metres) at which the camera auto-knocks the block.")]
        [SerializeField] private float _knockRadius = 0.18f;

        [Header("Physics")]
        [Tooltip("Impulse magnitude applied when the block is knocked.")]
        [SerializeField] private float _knockForce = 2.5f;

        [Tooltip("Seconds the block tumbles before shrink begins.")]
        [SerializeField] private float _destroyDelay = 0.12f;

        [Tooltip("Seconds the block takes to shrink to zero.")]
        [SerializeField] private float _shrinkDuration = 0.18f;

        [Header("VFX")]
        [Tooltip("VFX prefab spawned when the block breaks.\nSet on the block/pebble prefab asset.")]
        [SerializeField] private GameObject _breakVfxPrefab;

        [Header("Audio Override")]
        [Tooltip("Break sounds used when no VoxelBlock component is present (pebbles).\nLeave empty on voxel block prefabs — they read from VoxelBlock.BreakSounds.")]
        [SerializeField] private AudioClip[] _breakSounds;

        #endregion

        #region State ---------------------------------------------

        private Transform         _camera;
        private Transform         _worldContainer;
        private VoxelBlock        _voxelBlock;
        private IGameAudioService _audioService;
        private IHapticService    _hapticService;
        private IToolManager      _toolManager;
        private IUndoRedoService  _undoRedoService;
        private bool              _knocked;
        private bool              _ready;
        private WaitForSeconds    _waitDestroyDelay;

        #endregion

        #region Public API ----------------------------------------

        /// <summary>True once <see cref="SetReady"/> has been called.</summary>
        public bool IsReady => _ready;

        /// <summary>True once the block has started its destruction sequence.</summary>
        public bool IsKnocked => _knocked;

        /// <summary>
        /// Marks the block ready for proximity detection.
        /// Called by <see cref="BlockSpawn"/> after the fly-in completes.
        /// </summary>
        public void SetReady() => _ready = true;

        /// <summary>
        /// Triggers physics break from the Destroy tool or PebbleSupport.
        /// Undo recording is the caller's responsibility
        /// (see <see cref="Interaction.BlockDestroyer"/>).
        /// </summary>
        public void BreakFromTool(Vector3 hitDirection)
        {
            if (_knocked) return;
            _knocked = true;
            StartCoroutine(KnockRoutine(hitDirection));
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            _camera     = Camera.main != null ? Camera.main.transform : null;
            _voxelBlock = GetComponent<VoxelBlock>();

            ServiceLocator.TryGet<IToolManager>(out _toolManager);
            ServiceLocator.TryGet<IGameAudioService>(out _audioService);
            ServiceLocator.TryGet<IHapticService>(out _hapticService);
            ServiceLocator.TryGet<IUndoRedoService>(out _undoRedoService);

            _waitDestroyDelay = new WaitForSeconds(_destroyDelay);
        }

        private void Start()
        {
            // Cache the parent at spawn time — KnockRoutine unparents later.
            _worldContainer = transform.parent;
            ValidateReferences();
        }

        /// <summary>
        /// Proximity detection: if camera enters knock radius while
        /// the pickaxe is active, records undo and self-destructs.
        /// In Bonsai mode the radius is scaled down so the phone must
        /// be right against the block.
        /// </summary>
        private void Update()
        {
            if (!_ready || _knocked || _camera == null) return;
            if (_toolManager == null || _toolManager.CurrentTool != ToolType.Tool_Destroy) return;

            float radius = WorldModeContext.Selected == WorldMode.Bonsai
                ? _knockRadius * 0.3f
                : _knockRadius;

            float sqrDist = (transform.position - _camera.position).sqrMagnitude;
            if (sqrDist > radius * radius) return;

            // Mark knocked immediately — prevents double-trigger from
            // BreakFromTool or a second Update call in the same frame.
            _knocked = true;

            RecordUndoForProximity();
            StartCoroutine(KnockRoutine(null));
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Records a <see cref="DestroyBlockAction"/> and publishes
        /// <see cref="BlockDestroyedEvent"/> for the proximity knock path.
        /// Called after <c>_knocked</c> is already <c>true</c>.
        /// </summary>
        private void RecordUndoForProximity()
        {
            if (_voxelBlock != null && _undoRedoService != null && _worldContainer != null)
            {
                GameObject prefab = _toolManager?.GetBlockPrefab(_voxelBlock.Type);
                if (prefab != null)
                {
                    Vector3 localPos = _worldContainer.InverseTransformPoint(transform.position);

                    _undoRedoService.Record(new DestroyBlockAction(
                        prefab, _worldContainer, localPos, Quaternion.identity));

                    Debug.Log($"[BlockDestroy] Proximity destroy {_voxelBlock.Type} at local {localPos}.");
                }
            }

            if (_voxelBlock != null)
            {
                Vector3 localPos = _worldContainer != null
                    ? _worldContainer.InverseTransformPoint(transform.position)
                    : transform.localPosition;
                EventBus.Publish(new BlockDestroyedEvent(
                    Vector3Int.RoundToInt(localPos), _voxelBlock.Type));
            }
        }

        /// <summary>
        /// Destruction sequence: audio -> VFX -> unparent -> physics
        /// impulse -> tumble delay -> shrink to zero -> Destroy.
        /// </summary>
        private IEnumerator KnockRoutine(Vector3? overrideDirection)
        {
            // -- Haptic --
            _hapticService?.VibrateMedium();

            // -- Audio --
            if (_audioService != null)
            {
                AudioClip[] clips = _voxelBlock != null ? _voxelBlock.BreakSounds : _breakSounds;
                if (clips != null && clips.Length > 0)
                    _audioService.PlayOneShot(clips);
            }

            // -- VFX --
            if (_breakVfxPrefab != null)
                Instantiate(_breakVfxPrefab, transform.position, Quaternion.identity);

            // -- Unparent so physics works in world space --
            transform.SetParent(null, worldPositionStays: true);

            // -- Rigidbody --
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

            rb.isKinematic            = false;
            rb.mass                   = KNOCK_MASS;
            rb.useGravity             = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            Collider ownCollider = GetComponent<Collider>();
            if (ownCollider != null) ownCollider.enabled = true;

            // -- Kick direction --
            Vector3 kickDir;
            if (overrideDirection.HasValue)
            {
                kickDir = (overrideDirection.Value.normalized + Vector3.up * OVERRIDE_UP_BIAS).normalized;
            }
            else
            {
                Vector3 awayFromCam = _camera != null
                    ? (transform.position - _camera.position).normalized
                    : Random.insideUnitSphere.normalized;
                kickDir = (awayFromCam + Vector3.up * KNOCK_UP_BIAS).normalized;
            }

            Vector3 spread = new Vector3(
                Random.Range(-KNOCK_SPREAD, KNOCK_SPREAD), 0f, Random.Range(-KNOCK_SPREAD, KNOCK_SPREAD));
            kickDir = (kickDir + spread).normalized;

            rb.AddForce(kickDir * _knockForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * _knockForce * TORQUE_MULTIPLIER, ForceMode.Impulse);

            enabled = false;

            // -- Tumble delay --
            yield return _waitDestroyDelay;

            if (rb != null)
            {
                rb.linearVelocity  = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic     = true;
            }

            // -- Shrink to zero --
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

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_camera == null)
                Debug.LogWarning("[BlockDestroy] _camera is not assigned.", this);
            if (_worldContainer == null)
                Debug.LogWarning("[BlockDestroy] _worldContainer is not assigned.", this);
            if (_toolManager == null)
                Debug.LogWarning("[BlockDestroy] _toolManager is not assigned.", this);
            if (_audioService == null)
                Debug.LogWarning("[BlockDestroy] _audioService is not assigned.", this);
            if (_undoRedoService == null)
                Debug.LogWarning("[BlockDestroy] _undoRedoService is not assigned.", this);
        }

        #endregion
    }
}
