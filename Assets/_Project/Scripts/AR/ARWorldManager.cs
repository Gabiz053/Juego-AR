// ──────────────────────────────────────────────
//  ARWorldManager.cs  ·  _Project.Scripts.AR
//  Manages the spatial anchor that pins the voxel world to a
//  real-world surface, preventing AR drift.
// ──────────────────────────────────────────────

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using _Project.Scripts.Core;

namespace _Project.Scripts.AR
{
    /// <summary>
    /// Establishes and manages the AR spatial anchor that pins the
    /// <c>WorldContainer</c> to a real-world surface.<br/>
    /// On the first valid AR hit the container is positioned (ground
    /// level) and oriented (facing the player), then parented to an
    /// <see cref="ARAnchor"/> so ARCore/ARKit can correct for drift.<br/>
    /// Also acts as a facade for <see cref="GridManager.ActivateGrid"/>
    /// — the grid is activated automatically when the world is anchored.<br/>
    /// Attach to the <c>XR Origin (Mobile AR)</c> GameObject.
    /// </summary>
    [RequireComponent(typeof(ARAnchorManager))]
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/AR/AR World Manager")]
    public class ARWorldManager : MonoBehaviour
    {
        #region Constants ─────────────────────────────────────

        /// <summary>
        /// Minimum squared magnitude of the flattened camera-forward
        /// vector required to compute a valid orientation.
        /// Prevents <c>LookRotation</c> with a near-zero vector when
        /// the player looks straight down.
        /// </summary>
        private const float MIN_FORWARD_SQR_MAG = 0.001f;

        #endregion

        #region Inspector ─────────────────────────────────────

        [Header("Dependencies")]
        [Tooltip("Transform that parents all placed blocks (WorldContainer). Positioned and re-parented when the anchor is created.")]
        [SerializeField] private Transform _worldContainer;

        [Tooltip("GridManager — the construction grid is activated when the world is anchored.")]
        [SerializeField] private GridManager _gridManager;

        #endregion

        #region Cached Components ─────────────────────────────

        /// <summary>AR Foundation anchor manager (guaranteed by <c>[RequireComponent]</c>).</summary>
        private ARAnchorManager _anchorManager;

        /// <summary>Runtime anchor instance. <c>null</c> when not yet anchored.</summary>
        private ARAnchor _worldAnchor;

        #endregion

        #region Public API ────────────────────────────────────

        /// <summary>
        /// <c>true</c> once the world has been anchored to a surface.
        /// Returns <c>false</c> after <see cref="ResetAnchor"/> or
        /// before the first placement.
        /// </summary>
        public bool IsWorldAnchored => _worldAnchor != null;

        /// <summary>
        /// Anchors the world container to the given AR hit pose.<br/>
        /// 1) Positions the container at the hit point (ground level).<br/>
        /// 2) Orients it on the XZ plane facing the player camera.<br/>
        /// 3) Creates an <see cref="ARAnchor"/> and parents the container.<br/>
        /// 4) Activates the construction grid.<br/>
        /// No-op if the world is already anchored.
        /// </summary>
        /// <param name="hitPose">Pose from the AR raycast hit on a detected plane.</param>
        /// <param name="playerCamera">Player camera — used for orientation and grid tracking.</param>
        public void AnchorWorld(Pose hitPose, Transform playerCamera)
        {
            if (IsWorldAnchored)
            {
                Debug.Log("[ARWorldManager] AnchorWorld called but world is already anchored — ignoring.");
                return;
            }

            // 1. Position the world container at ground level (hit point).
            _worldContainer.position = hitPose.position;
            Debug.Log($"[ARWorldManager] WorldContainer positioned at {hitPose.position}.");

            // 2. Orient on the XZ plane so "forward" faces the player.
            OrientTowardsCamera(playerCamera);

            // 3. Create the AR spatial anchor.
            CreateAnchor(hitPose);

            // 4. Parent the world container to the anchor for drift correction.
            _worldContainer.SetParent(_worldAnchor.transform);
            Debug.Log("[ARWorldManager] WorldContainer parented to AR anchor.");

            // 5. Activate the construction grid halo.
            if (_gridManager != null)
            {
                _gridManager.ActivateGrid(playerCamera);
                Debug.Log("[ARWorldManager] Grid activated.");
            }
            else
            {
                Debug.LogWarning("[ARWorldManager] _gridManager is null — grid will not activate.", this);
            }

            Debug.Log("[ARWorldManager] World anchored successfully.");
        }

        /// <summary>
        /// Destroys the current AR anchor and un-parents the world
        /// container, returning the system to its pre-anchor state.
        /// Called by <see cref="UI.WorldResetService"/> during a full
        /// world reset.
        /// </summary>
        public void ResetAnchor()
        {
            if (_worldAnchor != null)
            {
                Destroy(_worldAnchor.gameObject);
                _worldAnchor = null;
                Debug.Log("[ARWorldManager] AR anchor destroyed.");
            }

            // Un-parent the world container so it is no longer
            // attached to the (now-destroyed) anchor.
            if (_worldContainer != null)
            {
                _worldContainer.SetParent(null);
                Debug.Log("[ARWorldManager] WorldContainer un-parented (root).");
            }

            Debug.Log("[ARWorldManager] Anchor reset complete — ready for re-anchor.");
        }

        #endregion

        #region Unity Lifecycle ────────────────────────────────

        private void Awake()
        {
            _anchorManager = GetComponent<ARAnchorManager>();
            Debug.Log("[ARWorldManager] Awake — ARAnchorManager cached.");
        }

        private void Start()
        {
            ValidateReferences();
            Debug.Log("[ARWorldManager] Initialized.");
        }

        #endregion

        #region Internals ─────────────────────────────────────

        /// <summary>
        /// Rotates the world container on the XZ plane so its forward
        /// direction faces the player camera.  If the camera is looking
        /// nearly straight down, the rotation is skipped to avoid a
        /// degenerate <c>LookRotation</c>.
        /// </summary>
        private void OrientTowardsCamera(Transform playerCamera)
        {
            Vector3 flatForward = playerCamera.forward;
            flatForward.y = 0f;

            if (flatForward.sqrMagnitude < MIN_FORWARD_SQR_MAG)
            {
                Debug.LogWarning("[ARWorldManager] Camera forward is nearly vertical — skipping orientation.", this);
                return;
            }

            _worldContainer.rotation = Quaternion.LookRotation(flatForward.normalized);
            Debug.Log("[ARWorldManager] WorldContainer oriented towards player camera.");
        }

        /// <summary>
        /// Creates an <see cref="ARAnchor"/> at the given pose.
        /// Tries the <see cref="ARAnchorManager.anchorPrefab"/> first;
        /// if none is assigned, creates a bare <c>GameObject</c> with
        /// an <c>ARAnchor</c> component as a safe fallback.
        /// </summary>
        private void CreateAnchor(Pose pose)
        {
            // Try instantiating the manager's anchor prefab.
            if (_anchorManager.anchorPrefab != null)
            {
                GameObject anchorInstance = Instantiate(
                    _anchorManager.anchorPrefab,
                    pose.position,
                    pose.rotation);

                _worldAnchor = anchorInstance.GetComponent<ARAnchor>();
                Debug.Log("[ARWorldManager] AR anchor created from prefab.");
            }

            // Fallback — create a manual anchor if the prefab was null
            // or did not contain an ARAnchor component.
            if (_worldAnchor == null)
            {
                GameObject anchorObj = new GameObject("World_ARAnchor");
                anchorObj.transform.SetPositionAndRotation(pose.position, pose.rotation);
                _worldAnchor = anchorObj.AddComponent<ARAnchor>();
                Debug.Log("[ARWorldManager] AR anchor created manually (no prefab).");
            }
        }

        #endregion

        #region Validation ────────────────────────────────────

        /// <summary>
        /// Logs errors for any missing Inspector references at startup.
        /// </summary>
        private void ValidateReferences()
        {
            if (_anchorManager == null)
                Debug.LogError("[ARWorldManager] ARAnchorManager not found!", this);
            if (_worldContainer == null)
                Debug.LogError("[ARWorldManager] _worldContainer is not assigned!", this);
            if (_gridManager == null)
                Debug.LogWarning("[ARWorldManager] _gridManager is not assigned — grid will not activate on anchor.", this);
        }

        #endregion
    }
}