// ------------------------------------------------------------
//  ARPlaneGridAligner.cs  -  _Project.Scripts.AR
//  Feeds the WorldContainer world-to-local matrix to every active
//  AR plane material so the grid shader aligns with the voxel grid.
// ------------------------------------------------------------

using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace _Project.Scripts.AR
{
    /// <summary>
    /// Pushes the <c>WorldContainer</c> world-to-local matrix into each
    /// active AR plane's <see cref="MeshRenderer"/> via a
    /// <see cref="MaterialPropertyBlock"/> every frame.  Also exposes
    /// runtime toggles for grid-line visibility and plane-mesh visibility.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/AR/AR Plane Grid Aligner")]
    public class ARPlaneGridAligner : MonoBehaviour
    {
        #region Constants -----------------------------------------

        private static readonly int GRID_MATRIX_ID  = Shader.PropertyToID("_GridMatrix");
        private static readonly int GRID_ENABLED_ID = Shader.PropertyToID("_GridEnabled");

        #endregion

        #region Inspector -----------------------------------------

        [Header("Dependencies")]
        [Tooltip("AR Plane Manager -- provides the list of active detected planes.")]
        [SerializeField] private ARPlaneManager _planeManager;

        [Tooltip("WorldContainer -- its world-to-local matrix aligns the grid shader.")]
        [SerializeField] private Transform _worldContainer;

        #endregion

        #region State ---------------------------------------------

        private MaterialPropertyBlock _mpb;

        #endregion

        #region Public API ----------------------------------------

        /// <summary>Whether the plane mesh renderers are currently visible.</summary>
        public bool IsVisualEnabled { get; private set; } = true;

        /// <summary>Whether the grid lines are drawn on top of the sand.</summary>
        public bool IsGridEnabled { get; private set; } = true;

        /// <summary>Shows or hides the plane mesh visuals.</summary>
        public void SetVisual(bool visible)
        {
            IsVisualEnabled = visible;
            if (_planeManager == null) return;

            foreach (ARPlane plane in _planeManager.trackables)
            {
                MeshRenderer mr = plane.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = visible;
            }
            Debug.Log($"[ARPlaneGridAligner] Plane visual {(visible ? "ON" : "OFF")}.");
        }

        /// <summary>Shows or hides only the grid lines drawn on the sand.</summary>
        public void SetGrid(bool visible)
        {
            IsGridEnabled = visible;
            Debug.Log($"[ARPlaneGridAligner] Grid lines {(visible ? "ON" : "OFF")}.");
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
        }

        private void Start()
        {
            ValidateReferences();
        }

        /// <summary>
        /// Injects <c>_GridMatrix</c> and <c>_GridEnabled</c> into every
        /// tracked plane's material each frame via <see cref="MaterialPropertyBlock"/>.
        /// Runs in LateUpdate so WorldContainer transforms are final.
        /// </summary>
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
                _mpb.SetFloat(GRID_ENABLED_ID, IsGridEnabled ? 1f : 0f);
                mr.SetPropertyBlock(_mpb);

                mr.enabled = IsVisualEnabled;
            }
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_planeManager == null)
                Debug.LogWarning("[ARPlaneGridAligner] _planeManager is not assigned.", this);
            if (_worldContainer == null)
                Debug.LogWarning("[ARPlaneGridAligner] _worldContainer is not assigned.", this);
        }

        #endregion
    }
}
