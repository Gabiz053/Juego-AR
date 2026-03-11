// ------------------------------------------------------------
//  ARWorldManager.cs  -  _Project.Scripts.AR
//  Manages the spatial anchor that pins the voxel world to a
//  real-world surface, preventing AR drift.
// ------------------------------------------------------------

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using _Project.Scripts.Core;

namespace _Project.Scripts.AR
{
    /// <summary>
    /// Establishes and manages the AR spatial anchor that pins
    /// <c>WorldContainer</c> to a real-world surface.  Also activates
    /// the construction grid when the world is anchored.
    /// </summary>
    [RequireComponent(typeof(ARAnchorManager))]
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/AR/AR World Manager")]
    public class ARWorldManager : MonoBehaviour
    {
        #region Constants -----------------------------------------

        private const float MIN_FORWARD_SQR_MAG = 0.001f;

        #endregion

        #region Inspector -----------------------------------------

        [Header("Dependencies")]
        [Tooltip("WorldContainer transform. Positioned and re-parented on anchor.")]
        [SerializeField] private Transform _worldContainer;

        [Tooltip("GridManager -- grid is activated when the world is anchored.")]
        [SerializeField] private GridManager _gridManager;

        #endregion

        #region State ---------------------------------------------

        private ARAnchorManager _anchorManager;
        private ARAnchor        _worldAnchor;

        #endregion

        #region Public API ----------------------------------------

        /// <summary><c>true</c> once the world has been anchored to a surface.</summary>
        public bool IsWorldAnchored => _worldAnchor != null;

        /// <summary>
        /// Anchors the world container to the given AR hit pose.
        /// No-op if already anchored.
        /// </summary>
        public void AnchorWorld(Pose hitPose, Transform playerCamera)
        {
            if (IsWorldAnchored) return;

            _worldContainer.position = hitPose.position;
            OrientTowardsCamera(playerCamera);
            CreateAnchor(hitPose);
            _worldContainer.SetParent(_worldAnchor.transform);

            _gridManager?.ActivateGrid(playerCamera);
            Debug.Log($"[ARWorldManager] World anchored at {hitPose.position}.");
        }

        /// <summary>
        /// Destroys the anchor and un-parents the world container.
        /// </summary>
        public void ResetAnchor()
        {
            if (_worldAnchor != null)
            {
                Destroy(_worldAnchor.gameObject);
                _worldAnchor = null;
            }

            if (_worldContainer != null)
                _worldContainer.SetParent(null);

            Debug.Log("[ARWorldManager] Anchor reset.");
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            _anchorManager = GetComponent<ARAnchorManager>();
        }

        private void Start()
        {
            ValidateReferences();
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Flattens the camera forward to the XZ plane and applies it as
        /// the WorldContainer rotation so blocks face the player.
        /// </summary>
        private void OrientTowardsCamera(Transform playerCamera)
        {
            Vector3 flatForward = playerCamera.forward;
            flatForward.y = 0f;

            if (flatForward.sqrMagnitude < MIN_FORWARD_SQR_MAG) return;

            _worldContainer.rotation = Quaternion.LookRotation(flatForward.normalized);
        }

        /// <summary>
        /// Creates an <see cref="ARAnchor"/> at the given pose.  Tries
        /// the prefab from <see cref="ARAnchorManager"/> first; falls back
        /// to a manually created GameObject with an ARAnchor component.
        /// </summary>
        private void CreateAnchor(Pose pose)
        {
            if (_anchorManager.anchorPrefab != null)
            {
                GameObject anchorInstance = Instantiate(
                    _anchorManager.anchorPrefab,
                    pose.position,
                    pose.rotation);
                _worldAnchor = anchorInstance.GetComponent<ARAnchor>();
            }

            if (_worldAnchor == null)
            {
                var anchorObj = new GameObject("World_ARAnchor");
                anchorObj.transform.SetPositionAndRotation(pose.position, pose.rotation);
                _worldAnchor = anchorObj.AddComponent<ARAnchor>();
            }
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_anchorManager == null)
                Debug.LogError("[ARWorldManager] ARAnchorManager not found!", this);
            if (_worldContainer == null)
                Debug.LogError("[ARWorldManager] _worldContainer is not assigned!", this);
            if (_gridManager == null)
                Debug.LogWarning("[ARWorldManager] _gridManager is not assigned!", this);
        }

        #endregion
    }
}