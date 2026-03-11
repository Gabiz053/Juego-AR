// ------------------------------------------------------------
//  GridManager.cs  -  _Project.Scripts.Core
//  Voxel grid configuration, snapping math, and grid-visual facade.
// ------------------------------------------------------------

using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Central authority for the voxel grid: owns the grid size,
    /// provides canonical snapping, and acts as a facade for
    /// <see cref="GridVisualizer"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Core/Grid Manager")]
    public class GridManager : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Grid Configuration")]
        [Tooltip("World-space size of one grid cell (metres).")]
        [SerializeField] private float _gridSize = 1f;

        [Header("Visualisation")]
        [Tooltip("GridVisualizer that renders the radial grid halo.")]
        [SerializeField] private GridVisualizer _gridVisualizer;

        #endregion

        #region Public API ----------------------------------------

        /// <summary>Size of one grid cell in world units.</summary>
        public float GridSize => _gridSize;

        /// <summary>Whether the grid visual is currently displayed.</summary>
        public bool IsGridActive => _gridVisualizer != null && _gridVisualizer.IsActive;

        /// <summary>
        /// Snaps a raw local position to the centre of the enclosing grid cell.
        /// </summary>
        public Vector3 GetSnappedPosition(Vector3 rawPosition)
        {
            float half = _gridSize / 2f;
            float x = Mathf.Floor(rawPosition.x / _gridSize) * _gridSize + half;
            float y = Mathf.Floor(rawPosition.y / _gridSize) * _gridSize + half;
            float z = Mathf.Floor(rawPosition.z / _gridSize) * _gridSize + half;
            return new Vector3(x, y, z);
        }

        /// <summary>Activates the visual grid halo.</summary>
        public void ActivateGrid(Transform cameraTransform)
        {
            if (_gridVisualizer != null)
                _gridVisualizer.Activate(cameraTransform, this);
        }

        /// <summary>Deactivates the visual grid halo.</summary>
        public void DeactivateGrid()
        {
            if (_gridVisualizer != null)
                _gridVisualizer.Deactivate();
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Start()
        {
            ValidateReferences();
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_gridSize <= 0f)
                Debug.LogError($"[GridManager] _gridSize must be > 0, but is {_gridSize}!", this);
            if (_gridVisualizer == null)
                Debug.LogWarning("[GridManager] _gridVisualizer is not assigned!", this);
        }

        #endregion
    }
}