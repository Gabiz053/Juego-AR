// ------------------------------------------------------------
//  SandGravity.cs  -  _Project.Scripts.Voxel
//  Minecraft-style gravity for sand blocks.
// ------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Project.Scripts.Infrastructure;

namespace _Project.Scripts.Voxel
{
    /// <summary>
    /// Gives sand blocks gravity behaviour.  After <see cref="BlockSpawn"/>
    /// completes its fly-in animation, arms the block for gravity.<br/>
    /// Reacts to <see cref="BlockDestroyedEvent"/> via the
    /// <see cref="EventBus"/> for immediate response when a supporting block
    /// is mined.  A slow safety-net poll (1 s) catches edge cases like
    /// cascading falls or AR plane removal.<br/>
    /// A shared static <see cref="HashSet{T}"/> of reserved cells prevents
    /// two blocks from targeting the same landing position.<br/>
    /// Designed to be placed <b>only</b> on the <c>Voxel_Sand</c> prefab.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Voxel/Sand Gravity")]
    public class SandGravity : MonoBehaviour
    {
        #region Constants -----------------------------------------

        /// <summary>Fraction of grid cell used as the support check distance.</summary>
        private const float SUPPORT_CHECK_RATIO = 0.5f;

        /// <summary>Fraction of grid cell added above hit point for landing offset.</summary>
        private const float LANDING_OFFSET_RATIO = 0.5f;

        /// <summary>Seconds between safety-net polls (cascades, AR plane removal).</summary>
        private const float SAFETY_POLL_INTERVAL = 1f;

        #endregion

        #region Inspector -----------------------------------------

        [Header("Timing")]
        [Tooltip("Seconds to wait after spawn before the first support check.")]
        [SerializeField] [Range(0f, 1f)] private float _initialDelay = 0.15f;

        [Header("Fall")]
        [Tooltip("Fall speed in local units per second.")]
        [SerializeField] [Range(1f, 30f)] private float _gravitySpeed = 15f;

        [Tooltip("Maximum raycast distance when searching for a landing spot.")]
        [SerializeField] [Range(5f, 100f)] private float _maxFallDistance = 50f;

        [Header("Physics")]
        [Tooltip("Layer mask for detecting support below (should include Voxel + Default for AR planes).")]
        [SerializeField] private LayerMask _layerMask = ~0;

        [Tooltip("Small margin added to the support raycast to avoid false negatives at edges.")]
        [SerializeField] [Range(0f, 0.2f)] private float _supportMargin = 0.05f;

        #endregion

        #region State ---------------------------------------------

        /// <summary>
        /// Landing cells claimed by blocks that are currently mid-fall.
        /// Prevents two blocks from targeting the same grid cell.
        /// </summary>
        private static readonly HashSet<Vector3Int> RESERVED_CELLS = new HashSet<Vector3Int>();

        private VoxelBlock        _voxelBlock;
        private BlockDestroy      _blockDestroy;
        private IGridManager      _gridManager;
        private IGameAudioService _audioService;
        private Collider          _collider;
        private Transform         _worldContainer;
        private bool              _isArmed;
        private bool              _isFalling;
        private Vector3Int        _reservedCell;
        private bool              _hasReservation;
        private WaitForSeconds    _waitInitialDelay;

        #endregion

        #region Unity Lifecycle -----------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            RESERVED_CELLS.Clear();
        }

        private void Awake()
        {
            _voxelBlock   = GetComponent<VoxelBlock>();
            _blockDestroy = GetComponent<BlockDestroy>();
            _collider     = GetComponent<Collider>();

            ServiceLocator.TryGet<IGridManager>(out _gridManager);
            ServiceLocator.TryGet<IGameAudioService>(out _audioService);

            _waitInitialDelay = new WaitForSeconds(_initialDelay);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<BlockDestroyedEvent>(HandleBlockDestroyed);
        }

        private void Start()
        {
            // Guard: only sand blocks should have gravity.
            if (_voxelBlock == null || _voxelBlock.Type != BlockType.Sand)
            {
                Debug.LogWarning("[SandGravity] Not a Sand block -- disabling.", this);
                enabled = false;
                return;
            }

            _worldContainer = transform.parent;

            ValidateReferences();
            StartCoroutine(InitRoutine());
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(SafetyPoll));
            EventBus.Unsubscribe<BlockDestroyedEvent>(HandleBlockDestroyed);
            ReleaseReservation();
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Waits for <see cref="BlockSpawn"/> to finish, then arms the
        /// block and starts the slow safety-net poll.
        /// </summary>
        private IEnumerator InitRoutine()
        {
            // Wait until BlockSpawn finishes (BlockDestroy.IsReady becomes true).
            if (_blockDestroy != null)
            {
                while (!_blockDestroy.IsReady)
                    yield return null;
            }

            yield return _waitInitialDelay;

            // If the block was destroyed during the wait, abort.
            if (this == null || gameObject == null) yield break;

            _isArmed = true;
            Debug.Log("[SandGravity] Armed -- events + safety poll.");

            // Immediate first check (e.g. placed in mid-air).
            CheckAndFall();

            // Slow safety net for cascading and edge cases.
            InvokeRepeating(nameof(SafetyPoll), SAFETY_POLL_INTERVAL, SAFETY_POLL_INTERVAL);
        }

        /// <summary>
        /// Reacts to any block being destroyed.  The main trigger for
        /// detecting support loss.
        /// </summary>
        private void HandleBlockDestroyed(BlockDestroyedEvent evt)
        {
            if (!_isArmed || _isFalling) return;
            CheckAndFall();
        }

        /// <summary>
        /// Slow safety-net poll that catches support loss not triggered by
        /// <see cref="BlockDestroyedEvent"/> (e.g. cascading falls,
        /// AR plane removal).
        /// </summary>
        private void SafetyPoll()
        {
            if (!_isArmed || _isFalling) return;
            CheckAndFall();
        }

        /// <summary>
        /// Core gravity check: if no support below, reserves the landing
        /// cell and starts an animated fall.
        /// </summary>
        private void CheckAndFall()
        {
            if (HasSupportBelow()) return;

            CancelInvoke(nameof(SafetyPoll));

            if (!TryFindLandingPosition(out Vector3 landingLocal))
            {
                Debug.Log("[SandGravity] No landing position found -- staying in place.");
                InvokeRepeating(nameof(SafetyPoll), SAFETY_POLL_INTERVAL, SAFETY_POLL_INTERVAL);
                return;
            }

            // Skip if landing at current position (already grounded).
            Vector3Int targetCell  = PositionToCell(landingLocal);
            Vector3Int currentCell = PositionToCell(transform.localPosition);
            if (targetCell == currentCell)
            {
                InvokeRepeating(nameof(SafetyPoll), SAFETY_POLL_INTERVAL, SAFETY_POLL_INTERVAL);
                return;
            }

            // Reserve the landing cell so other falling blocks stack above.
            _reservedCell   = targetCell;
            _hasReservation = true;
            RESERVED_CELLS.Add(_reservedCell);

            Debug.Log($"[SandGravity] No support -- falling from {transform.localPosition} to {landingLocal}.");
            StartCoroutine(FallRoutine(landingLocal));
        }

        /// <summary>
        /// Checks whether there is a collider directly below this block
        /// within half a grid cell distance (immediate neighbour or AR plane).
        /// </summary>
        private bool HasSupportBelow()
        {
            float worldScale = _worldContainer != null ? _worldContainer.localScale.x : 1f;
            float gridWorld  = _gridManager.GridSize * worldScale;
            float checkDist  = gridWorld * SUPPORT_CHECK_RATIO + _supportMargin;

            return Physics.Raycast(
                transform.position,
                Vector3.down,
                checkDist,
                _layerMask);
        }

        /// <summary>
        /// Raycasts far downward to find a landing surface, then computes
        /// the snapped grid cell one unit above the hit point.  Skips cells
        /// already reserved by other falling sand blocks.
        /// </summary>
        private bool TryFindLandingPosition(out Vector3 landingLocal)
        {
            landingLocal = Vector3.zero;

            float worldScale = _worldContainer != null ? _worldContainer.localScale.x : 1f;
            float maxDist    = _maxFallDistance * worldScale;

            if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, maxDist, _layerMask))
                return false;

            // Convert hit point to local space and snap to grid.
            Vector3 hitLocal = _worldContainer.InverseTransformPoint(hit.point);

            // Place the block one half-grid above the hit surface so it sits on top.
            hitLocal.y += _gridManager.GridSize * LANDING_OFFSET_RATIO;

            landingLocal = _gridManager.GetSnappedPosition(hitLocal);

            // Stack above any cells reserved by other falling sand blocks.
            Vector3Int cell = PositionToCell(landingLocal);
            int safety = 50;
            while (RESERVED_CELLS.Contains(cell) && safety-- > 0)
            {
                landingLocal.y += _gridManager.GridSize;
                cell = PositionToCell(landingLocal);
            }

            return true;
        }

        /// <summary>
        /// Smoothly moves the block from its current position to the landing
        /// position.  Disables collider and <see cref="BlockDestroy"/> during
        /// the fall (same safety pattern as <see cref="BlockSpawn"/>).<br/>
        /// After landing, re-enables interactions, syncs physics, releases
        /// the cell reservation, and restarts the safety poll.
        /// </summary>
        private IEnumerator FallRoutine(Vector3 targetLocal)
        {
            _isFalling = true;

            // Disable interactions during fall.
            if (_collider != null) _collider.enabled = false;
            if (_blockDestroy != null) _blockDestroy.enabled = false;

            Vector3 startLocal = transform.localPosition;
            float distance     = Vector3.Distance(startLocal, targetLocal);
            float duration     = distance / _gravitySpeed;
            float elapsed      = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Accelerate: quadratic ease-in for natural gravity feel.
                float eased = t * t;
                transform.localPosition = Vector3.LerpUnclamped(startLocal, targetLocal, eased);
                yield return null;
            }

            // Snap to exact grid position.
            transform.localPosition = targetLocal;

            // Re-enable interactions.
            if (_collider != null) _collider.enabled = true;
            if (_blockDestroy != null)
            {
                _blockDestroy.enabled = true;
                _blockDestroy.SetReady();
            }

            // Sync physics so raycasts from other blocks see this collider.
            Physics.SyncTransforms();

            // Release the reservation now that the collider is in place.
            ReleaseReservation();

            PlayLandingSound();
            Debug.Log($"[SandGravity] Landed at {targetLocal}.");

            _isFalling = false;
            _isArmed   = true;

            // Restart slow safety-net poll.
            InvokeRepeating(nameof(SafetyPoll), SAFETY_POLL_INTERVAL, SAFETY_POLL_INTERVAL);
        }

        /// <summary>
        /// Converts a local-space position to a deterministic cell index.
        /// Uses <see cref="Mathf.FloorToInt"/> to avoid banker's rounding
        /// ambiguity at cell boundaries (0.5, 1.5, etc.).
        /// </summary>
        private Vector3Int PositionToCell(Vector3 localPos)
        {
            float g = _gridManager.GridSize;
            return new Vector3Int(
                Mathf.FloorToInt(localPos.x / g),
                Mathf.FloorToInt(localPos.y / g),
                Mathf.FloorToInt(localPos.z / g));
        }

        /// <summary>
        /// Removes this block's cell reservation from the shared set.
        /// </summary>
        private void ReleaseReservation()
        {
            if (!_hasReservation) return;
            RESERVED_CELLS.Remove(_reservedCell);
            _hasReservation = false;
        }

        /// <summary>
        /// Plays a random place sound through <see cref="GameAudioService"/>
        /// using the clips defined on the <see cref="VoxelBlock"/>.
        /// </summary>
        private void PlayLandingSound()
        {
            if (_audioService == null || _voxelBlock == null) return;

            AudioClip[] clips = _voxelBlock.PlaceSounds;
            if (clips != null && clips.Length > 0)
                _audioService.PlayOneShot(clips);
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_gridManager == null)
                Debug.LogWarning("[SandGravity] _gridManager is not assigned.", this);
            if (_blockDestroy == null)
                Debug.LogWarning("[SandGravity] _blockDestroy is not assigned.", this);
            if (_collider == null)
                Debug.LogWarning("[SandGravity] _collider is not assigned.", this);
            if (_worldContainer == null)
                Debug.LogWarning("[SandGravity] _worldContainer is not assigned.", this);
        }

        #endregion
    }
}
