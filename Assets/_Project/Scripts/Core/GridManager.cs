// ──────────────────────────────────────────────
//  GridManager.cs  ·  _Project.Scripts.Core
//  Voxel grid configuration, snapping math, and grid-visual facade.
// ──────────────────────────────────────────────

using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Central authority for the voxel grid.<br/>
    /// • Owns the <see cref="GridSize"/> configuration.<br/>
    /// • Provides <see cref="GetSnappedPosition"/> — the canonical
    ///   floor-and-offset snap used by every placement system.<br/>
    /// • Acts as a <b>facade</b> for <see cref="GridVisualizer"/>,
    ///   exposing <see cref="ActivateGrid"/> / <see cref="DeactivateGrid"/>
    ///   so callers (ARWorldManager, WorldResetService) only need a
    ///   single reference.<br/>
    /// Attach to the <c>WorldContainer</c> GameObject.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Core/Grid Manager")]
    public class GridManager : MonoBehaviour
    {
        #region Inspector ─────────────────────────────────────

        [Header("Grid Configuration")]
        [Tooltip("World-space size of one grid cell (metres). All snapping derives from this value.")]
        [SerializeField] private float _gridSize = 1f;

        [Header("Visualisation")]
        [Tooltip("GridVisualizer that renders the radial grid halo. Lives on the same GameObject.")]
        [SerializeField] private GridVisualizer _gridVisualizer;

        #endregion

        #region Public API ────────────────────────────────────

        /// <summary>Size of one grid cell in world units.</summary>
        public float GridSize => _gridSize;

        /// <summary>
        /// Snaps a raw world-space position to the centre of the
        /// enclosing grid cell.<br/>
        /// Uses <c>Floor + half-cell offset</c> so the block centre sits
        /// at <c>0.5 × gridSize</c> — the bottom face rests exactly on
        /// the grid plane (y = 0 in local space).
        /// </summary>
        /// <param name="rawPosition">Unsnapped position in <b>local</b> WorldContainer space.</param>
        /// <returns>Snapped position aligned to the grid.</returns>
        public Vector3 GetSnappedPosition(Vector3 rawPosition)
        {
            float half = _gridSize / 2f;

            float snappedX = (Mathf.Floor(rawPosition.x / _gridSize) * _gridSize) + half;
            float snappedY = (Mathf.Floor(rawPosition.y / _gridSize) * _gridSize) + half;
            float snappedZ = (Mathf.Floor(rawPosition.z / _gridSize) * _gridSize) + half;

            return new Vector3(snappedX, snappedY, snappedZ);
        }

        /// <summary>
        /// Activates the visual grid halo centred on the player camera.
        /// Delegates rendering to <see cref="GridVisualizer"/>.
        /// </summary>
        /// <param name="cameraTransform">Player camera used for centre tracking.</param>
        public void ActivateGrid(Transform cameraTransform)
        {
            if (_gridVisualizer == null)
            {
                Debug.LogWarning("[GridManager] _gridVisualizer is null — cannot show grid.", this);
                return;
            }

            _gridVisualizer.Activate(cameraTransform, this);
            Debug.Log("[GridManager] Grid activated.");
        }

        /// <summary>
        /// Deactivates and destroys the visual grid halo.
        /// Delegates to <see cref="GridVisualizer"/>.
        /// </summary>
        public void DeactivateGrid()
        {
            if (_gridVisualizer == null)
            {
                Debug.LogWarning("[GridManager] _gridVisualizer is null — cannot hide grid.", this);
                return;
            }

            _gridVisualizer.Deactivate();
            Debug.Log("[GridManager] Grid deactivated.");
        }

        /// <summary>Whether the grid visual is currently displayed.</summary>
        public bool IsGridActive => _gridVisualizer != null && _gridVisualizer.IsActive;

        #endregion

        #region Unity Lifecycle ────────────────────────────────

        private void Start()
        {
            ValidateReferences();
            Debug.Log($"[GridManager] Initialized — gridSize: {_gridSize}.");
        }

        #endregion

        #region Validation ────────────────────────────────────

        /// <summary>
        /// Logs errors for any missing Inspector references at startup.
        /// </summary>
        private void ValidateReferences()
        {
            if (_gridSize <= 0f)
                Debug.LogError($"[GridManager] _gridSize must be > 0, but is {_gridSize}!", this);
            if (_gridVisualizer == null)
                Debug.LogWarning("[GridManager] _gridVisualizer is not assigned — grid visual will not work.", this);
        }

        #endregion
    }
}