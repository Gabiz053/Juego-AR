// ------------------------------------------------------------
//  SandGravity.cs  -  _Project.Scripts.Voxel
//  Minecraft-style gravity for sand blocks.
// ------------------------------------------------------------

using System.Collections;
using UnityEngine;
using _Project.Scripts.Infrastructure;

namespace _Project.Scripts.Voxel
{
    /// <summary>
    /// Gives sand blocks gravity behaviour.  After <see cref="BlockSpawn"/>
    /// completes its fly-in animation, starts polling downward to verify
    /// support.<br/>
    /// If no block or AR plane is found directly below, the block falls
    /// smoothly to the nearest supported grid cell.  Keeps polling after
    /// landing so it reacts when a supporting block is mined.<br/>
    /// All timing, speed and distance values are configurable from the Inspector.<br/>
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

        #endregion

        #region Inspector -----------------------------------------

        [Header("Timing")]
        [Tooltip("Seconds to wait after spawn before the first support check.")]
        [SerializeField] [Range(0f, 1f)] private float _initialDelay = 0.15f;

        [Tooltip("Seconds between each support poll (similar to PebbleSupport).")]
        [SerializeField] [Range(0.05f, 1f)] private float _pollInterval = 0.15f;

        [Header("Fall")]
        [Tooltip("Fall speed in local units per second.")]
        [SerializeField] [Range(1f, 30f)] private float _fallSpeed = 7f;

        [Tooltip("Maximum raycast distance when searching for a landing spot.")]
        [SerializeField] [Range(5f, 100f)] private float _maxFallDistance = 50f;

        [Header("Physics")]
        [Tooltip("Layer mask for detecting support below (should include Voxel + Default for AR planes).")]
        [SerializeField] private LayerMask _layerMask = ~0;

        [Tooltip("Small margin added to the support raycast to avoid false negatives at edges.")]
        [SerializeField] [Range(0f, 0.2f)] private float _supportMargin = 0.05f;

        #endregion

        #region State ---------------------------------------------

        private VoxelBlock        _voxelBlock;
        private BlockDestroy      _blockDestroy;
        private IGridManager      _gridManager;
        private IGameAudioService _audioService;
        private Collider          _collider;
        private Transform        _worldContainer;
        private bool             _isFalling;
        private WaitForSeconds   _waitInitialDelay;

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            _voxelBlock   = GetComponent<VoxelBlock>();
            _blockDestroy = GetComponent<BlockDestroy>();
            _collider     = GetComponent<Collider>();

            ServiceLocator.TryGet<IGridManager>(out _gridManager);
            ServiceLocator.TryGet<IGameAudioService>(out _audioService);

            _waitInitialDelay = new WaitForSeconds(_initialDelay);
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
            CancelInvoke(nameof(PollSupport));
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Waits for <see cref="BlockSpawn"/> to finish, then starts
        /// periodic support polling via <see cref="InvokeRepeating"/>.
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

            Debug.Log("[SandGravity] Armed -- starting support poll.");
            InvokeRepeating(nameof(PollSupport), 0f, _pollInterval);
        }

        /// <summary>
        /// Periodic support check.  If no support is found below,
        /// cancels polling and starts a fall coroutine.
        /// </summary>
        private void PollSupport()
        {
            if (_isFalling) return;

            if (HasSupportBelow()) return;

            // No support -- stop polling and fall.
            CancelInvoke(nameof(PollSupport));

            if (!TryFindLandingPosition(out Vector3 landingLocal))
            {
                Debug.Log("[SandGravity] No landing position found -- staying in place.");
                return;
            }

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
        /// the snapped grid cell one unit above the hit point.
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
            return true;
        }

        /// <summary>
        /// Smoothly moves the block from its current position to the landing
        /// position.  Disables collider and <see cref="BlockDestroy"/> during
        /// the fall (same safety pattern as <see cref="BlockSpawn"/>).<br/>
        /// After landing, restarts the support poll so future neighbour
        /// destruction is detected.
        /// </summary>
        private IEnumerator FallRoutine(Vector3 targetLocal)
        {
            _isFalling = true;

            // Disable interactions during fall.
            if (_collider != null) _collider.enabled = false;
            if (_blockDestroy != null) _blockDestroy.enabled = false;

            Vector3 startLocal = transform.localPosition;
            float sqrDistance  = (targetLocal - startLocal).sqrMagnitude;
            float distance     = Mathf.Sqrt(sqrDistance);
            float duration     = distance / _fallSpeed;
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

            // Landing SFX.
            PlayLandingSound();

            Debug.Log($"[SandGravity] Landed at {targetLocal}.");

            _isFalling = false;

            // Restart polling to detect future support loss.
            InvokeRepeating(nameof(PollSupport), _pollInterval, _pollInterval);
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
