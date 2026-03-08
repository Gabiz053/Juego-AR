// ??????????????????????????????????????????????
//  ARPlaneGridAligner.cs  ·  _Project.Scripts.AR
//  Feeds the WorldContainer world-to-local matrix to every active
//  AR plane material so the grid shader aligns with the voxel grid.
// ??????????????????????????????????????????????

using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace _Project.Scripts.AR
{
    /// <summary>
    /// Reads the <see cref="ARPlaneManager"/> active planes each frame and
    /// pushes the <c>WorldContainer</c> world-to-local matrix into each
    /// plane's <see cref="MeshRenderer"/> via a <see cref="MaterialPropertyBlock"/>.<br/>
    /// The <c>ARPlane</c> shader samples this matrix to draw its grid
    /// aligned with the voxel grid orientation rather than world axes.<br/>
    /// Before the world is anchored, the identity matrix is used so the
    /// grid still renders (aligned to world axes as a fallback).<br/>
    /// Attach to <c>XR Origin (Mobile AR)</c>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/AR/AR Plane Grid Aligner")]
    public class ARPlaneGridAligner : MonoBehaviour
    {
        #region Inspector ?????????????????????????????????????

        [Header("Dependencies")]
        [Tooltip("AR Plane Manager — provides the list of active detected planes.")]
        [SerializeField] private ARPlaneManager _planeManager;

        [Tooltip("WorldContainer Transform — its world-to-local matrix aligns the grid shader.")]
        [SerializeField] private Transform _worldContainer;

        #endregion

        #region Internals ?????????????????????????????????????

        private static readonly int GRID_MATRIX_ID = Shader.PropertyToID("_GridMatrix");

        private MaterialPropertyBlock _mpb;

        #endregion

        #region Unity Lifecycle ????????????????????????????????

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
        }

        private void Start()
        {
            ValidateReferences();
        }

        private void LateUpdate()
        {
            if (_planeManager == null) return;

            Matrix4x4 gridMatrix = _worldContainer != null
                ? _worldContainer.worldToLocalMatrix
                : Matrix4x4.identity;

            foreach (ARPlane plane in _planeManager.trackables)
            {
                MeshRenderer mr = plane.GetComponent<MeshRenderer>();
                if (mr == null) continue;

                mr.GetPropertyBlock(_mpb);
                _mpb.SetMatrix(GRID_MATRIX_ID, gridMatrix);
                mr.SetPropertyBlock(_mpb);
            }
        }

        #endregion

        #region Validation ????????????????????????????????????

        private void ValidateReferences()
        {
            if (_planeManager == null)
                Debug.LogError("[ARPlaneGridAligner] _planeManager is not assigned!", this);
            if (_worldContainer == null)
                Debug.LogWarning("[ARPlaneGridAligner] _worldContainer is not assigned — grid will use world axes.", this);
        }

        #endregion
    }
}
