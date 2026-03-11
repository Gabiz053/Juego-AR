// ------------------------------------------------------------
//  BlockDestroy.cs  -  _Project.Scripts.Voxel
//  Proximity knockback and tool-triggered destruction with
//  physics tumble, shrink and VFX feedback.
// ------------------------------------------------------------

using System.Collections;
using UnityEngine;
using _Project.Scripts.Core;
using _Project.Scripts.Interaction;
using Random = UnityEngine.Random;

namespace _Project.Scripts.Voxel
{
    /// <summary>
    /// Handles block destruction via proximity knock or tool break.<br/>
    /// Auto-locates scene singletons in <c>Awake</c> so prefab instances
    /// need no post-instantiate injection.<br/>
    /// Proximity knock only triggers when the player holds the pickaxe
    /// (<see cref="ToolType.Tool_Destroy"/>).  Both proximity and tool-
    /// triggered destruction are undoable via <see cref="DestroyBlockAction"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Voxel/Block Destroy")]
    public class BlockDestroy : MonoBehaviour
    {
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

        private Transform        _camera;
        private Transform        _worldContainer;
        private VoxelBlock       _voxelBlock;
        private GameAudioService _audioService;
        private HapticService    _hapticService;
        private ToolManager      _toolManager;
        private UndoRedoService  _undoRedoService;
        private HarmonyService   _harmonyService;
        private bool             _knocked;
        private bool             _ready;

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            _camera          = Camera.main != null ? Camera.main.transform : null;
            _voxelBlock      = GetComponent<VoxelBlock>();
            _toolManager     = FindAnyObjectByType<ToolManager>();
            _audioService    = FindAnyObjectByType<GameAudioService>();
            _hapticService   = FindAnyObjectByType<HapticService>();
            _undoRedoService = FindAnyObjectByType<UndoRedoService>();
            _harmonyService  = FindAnyObjectByType<HarmonyService>();
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
        /// </summary>
        private void Update()
        {
            if (!_ready || _knocked || _camera == null) return;
            if (_toolManager == null || _toolManager.CurrentTool != ToolType.Tool_Destroy) return;

            float sqrDist = (transform.position - _camera.position).sqrMagnitude;
            if (sqrDist > _knockRadius * _knockRadius) return;

            // Mark knocked immediately — prevents double-trigger from
            // BreakFromTool or a second Update call in the same frame.
            _knocked = true;

            RecordUndoForProximity();
            StartCoroutine(KnockRoutine(null));
        }

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
        /// (see <see cref="BlockDestroyer"/>).
        /// </summary>
        public void BreakFromTool(Vector3 hitDirection)
        {
            if (_knocked) return;
            _knocked = true;
            StartCoroutine(KnockRoutine(hitDirection));
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Records a <see cref="DestroyBlockAction"/> and notifies
        /// <see cref="HarmonyService"/> for the proximity knock path.
        /// Called after <c>_knocked</c> is already <c>true</c>.
        /// </summary>
        private void RecordUndoForProximity()
        {
            if (_voxelBlock != null && _undoRedoService != null && _worldContainer != null)
            {
                GameObject prefab = _toolManager.GetBlockPrefab(_voxelBlock.Type);
                if (prefab != null)
                {
                    Vector3 localPos = _worldContainer.InverseTransformPoint(transform.position);

                    _undoRedoService.Record(new DestroyBlockAction(
                        prefab, _worldContainer, localPos, Quaternion.identity));

                    Debug.Log($"[BlockDestroy] Proximity destroy {_voxelBlock.Type} at local {localPos}.");
                }
            }

            if (_voxelBlock != null)
                _harmonyService?.NotifyBlockDestroyed(_voxelBlock.Type);
        }

        /// <summary>
        /// Destruction sequence: audio → VFX → unparent → physics
        /// impulse → tumble delay → shrink to zero → Destroy.
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
            rb.mass                   = 1f;
            rb.useGravity             = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            Collider ownCollider = GetComponent<Collider>();
            if (ownCollider != null) ownCollider.enabled = true;

            // -- Kick direction --
            Vector3 kickDir;
            if (overrideDirection.HasValue)
            {
                kickDir = (overrideDirection.Value.normalized + Vector3.up * 0.5f).normalized;
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

            // -- Tumble delay --
            yield return new WaitForSeconds(_destroyDelay);

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
                Debug.LogError("[BlockDestroy] Camera.main not found!", this);
            if (_worldContainer == null)
                Debug.LogError("[BlockDestroy] No parent found — block must be child of WorldContainer!", this);
            if (_toolManager == null)
                Debug.LogWarning("[BlockDestroy] ToolManager not found!", this);
            if (_audioService == null)
                Debug.LogWarning("[BlockDestroy] GameAudioService not found!", this);
            if (_undoRedoService == null)
                Debug.LogWarning("[BlockDestroy] UndoRedoService not found!", this);
            if (_harmonyService == null)
                Debug.LogWarning("[BlockDestroy] HarmonyService not found!", this);
        }

        #endregion
    }
}
