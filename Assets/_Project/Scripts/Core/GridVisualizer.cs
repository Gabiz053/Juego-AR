// ──────────────────────────────────────────────
//  GridVisualizer.cs  ·  _Project.Scripts.Core
//  Renders a radial, camera-following grid halo on the XZ plane
//  using a dynamic line mesh.
// ──────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Procedurally generates a flat grid mesh centred on the player
    /// camera with a smooth radial alpha fade.<br/>
    /// The grid follows the camera each frame, only regenerating when
    /// the snapped centre actually changes (zero-GC optimised).<br/>
    /// Extracted from <see cref="GridManager"/> so visual rendering
    /// lives in its own component and can be toggled independently.<br/>
    /// Attach to the same GameObject as <c>GridManager</c>
    /// (<c>WorldContainer</c>).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Core/Grid Visualizer")]
    public class GridVisualizer : MonoBehaviour
    {
        #region Inspector ─────────────────────────────────────

        [Header("Grid Appearance")]
        [Tooltip("Radius (in grid units) around the camera where grid lines are drawn.")]
        [SerializeField] private float _gridRadius = 4f;

        [Tooltip("Material applied to the grid mesh. Should support vertex colours.")]
        [SerializeField] private Material _lineMaterial;

        [Tooltip("Base colour (and alpha) used for grid lines. Alpha fades radially.")]
        [SerializeField] private Color _gridColor = new Color(1f, 1f, 1f, 0.4f);

        #endregion

        #region Runtime State ─────────────────────────────────

        /// <summary>Whether the grid visual is currently active.</summary>
        private bool _isActive;

        /// <summary>Reference to the player camera used for centre tracking.</summary>
        private Transform _playerCamera;

        /// <summary>Grid cell size — injected by <see cref="GridManager"/>.</summary>
        private float _gridSize;

        /// <summary>Snapping helper — injected by <see cref="GridManager"/>.</summary>
        private GridManager _gridManager;

        // Visual objects
        private GameObject _gridVisualObject;
        private MeshFilter _meshFilter;
        private Mesh _gridMesh;

        /// <summary>Last snapped centre — used to skip redundant rebuilds.</summary>
        private Vector3 _lastSnappedCenter = new Vector3(9999f, 9999f, 9999f);

        // Zero-GC reusable buffers
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color32> _colors   = new List<Color32>();
        private readonly List<int>     _indices  = new List<int>();

        /// <summary>Squared radius — cached to avoid per-vertex sqrt.</summary>
        private float _sqrRadius;

        #endregion

        #region Public API ────────────────────────────────────

        /// <summary>
        /// Initialises and shows the grid, centred on the camera.
        /// Called by <see cref="GridManager.ActivateGrid"/>.
        /// </summary>
        /// <param name="cameraTransform">Player camera transform used for tracking.</param>
        /// <param name="gridManager">Parent GridManager (provides GridSize and snapping).</param>
        public void Activate(Transform cameraTransform, GridManager gridManager)
        {
            _playerCamera = cameraTransform;
            _gridManager  = gridManager;
            _gridSize     = gridManager.GridSize;
            _sqrRadius    = _gridRadius * _gridRadius;
            _isActive     = true;

            CreateMeshObject();

            Debug.Log($"[GridVisualizer] Activated — radius {_gridRadius}, gridSize {_gridSize}.");
        }

        /// <summary>
        /// Hides and destroys the grid visual.
        /// Called by <see cref="GridManager.DeactivateGrid"/>.
        /// </summary>
        public void Deactivate()
        {
            _isActive = false;

            if (_gridVisualObject != null)
            {
                Destroy(_gridVisualObject);
                _gridVisualObject = null;
            }

            // Reset tracking so next activation rebuilds immediately.
            _lastSnappedCenter = new Vector3(9999f, 9999f, 9999f);

            Debug.Log("[GridVisualizer] Deactivated — mesh destroyed.");
        }

        /// <summary>Whether the grid visual is currently displayed.</summary>
        public bool IsActive => _isActive;

        #endregion

        #region Unity Lifecycle ────────────────────────────────

        private void Update()
        {
            if (!_isActive || _playerCamera == null || _gridManager == null) return;

            // Project camera position onto the local XZ plane.
            Vector3 localCamPos = transform.InverseTransformPoint(_playerCamera.position);
            localCamPos.y = 0f;

            Vector3 snappedCenter = _gridManager.GetSnappedPosition(localCamPos);

            // Only rebuild when the snapped centre changes cell.
            if (snappedCenter != _lastSnappedCenter)
            {
                _lastSnappedCenter = snappedCenter;
                RebuildMesh();
            }
        }

        #endregion

        #region Mesh Construction ──────────────────────────────

        /// <summary>
        /// Creates the runtime <c>GameObject</c> that holds the grid
        /// <see cref="MeshFilter"/> and <see cref="MeshRenderer"/>.
        /// </summary>
        private void CreateMeshObject()
        {
            _gridVisualObject = new GameObject("Dynamic_GridVisual");

            // worldPositionStays = false keeps scale consistent
            // when WorldContainer is in "model scale" mode.
            _gridVisualObject.transform.SetParent(transform, false);
            _gridVisualObject.transform.localPosition = Vector3.zero;
            _gridVisualObject.transform.localRotation = Quaternion.identity;
            _gridVisualObject.transform.localScale    = Vector3.one;

            _meshFilter = _gridVisualObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = _gridVisualObject.AddComponent<MeshRenderer>();

            if (_lineMaterial != null)
            {
                meshRenderer.material = _lineMaterial;
            }
            else
            {
                Debug.LogWarning("[GridVisualizer] _lineMaterial is null — grid will use default material.", this);
            }

            _gridMesh = new Mesh { name = "RadialGridMesh" };
            _meshFilter.mesh = _gridMesh;

            Debug.Log("[GridVisualizer] Grid mesh object created.");
        }

        /// <summary>
        /// Clears and rebuilds the entire grid mesh based on the
        /// current <see cref="_lastSnappedCenter"/>.
        /// </summary>
        private void RebuildMesh()
        {
            _vertices.Clear();
            _colors.Clear();
            _indices.Clear();

            int currentIndex = 0;
            int steps = Mathf.CeilToInt(_gridRadius / _gridSize);

            // Snap origin to cell corners so lines wrap blocks instead
            // of cutting through their centres.
            float originX = Mathf.Floor(_lastSnappedCenter.x / _gridSize) * _gridSize;
            float originZ = Mathf.Floor(_lastSnappedCenter.z / _gridSize) * _gridSize;

            // Flat fade centre (Y = 0) for radial distance calculation.
            Vector3 fadeCenter = new Vector3(_lastSnappedCenter.x, 0f, _lastSnappedCenter.z);

            // ── Vertical lines (along Z) ──
            for (int x = -steps; x <= steps; x++)
            {
                float xPos = originX + (x * _gridSize);
                for (int z = -steps; z < steps; z++)
                {
                    Vector3 start = new Vector3(xPos, 0f, originZ + (z * _gridSize));
                    Vector3 end   = new Vector3(xPos, 0f, originZ + ((z + 1) * _gridSize));
                    AddSegment(start, end, fadeCenter, ref currentIndex);
                }
            }

            // ── Horizontal lines (along X) ──
            for (int z = -steps; z <= steps; z++)
            {
                float zPos = originZ + (z * _gridSize);
                for (int x = -steps; x < steps; x++)
                {
                    Vector3 start = new Vector3(originX + (x * _gridSize), 0f, zPos);
                    Vector3 end   = new Vector3(originX + ((x + 1) * _gridSize), 0f, zPos);
                    AddSegment(start, end, fadeCenter, ref currentIndex);
                }
            }

            _gridMesh.Clear();
            _gridMesh.SetVertices(_vertices);
            _gridMesh.SetColors(_colors);
            _gridMesh.SetIndices(_indices, MeshTopology.Lines, 0);
            _gridMesh.RecalculateBounds();
        }

        /// <summary>
        /// Adds a single line segment between <paramref name="start"/> and
        /// <paramref name="end"/> with radial alpha fade, skipping fully
        /// transparent segments for early-out optimisation.
        /// </summary>
        private void AddSegment(Vector3 start, Vector3 end, Vector3 center, ref int index)
        {
            Color32 colorStart = CalculateFadedColor(start, center);
            Color32 colorEnd   = CalculateFadedColor(end, center);

            // Skip fully invisible segments.
            if (colorStart.a == 0 && colorEnd.a == 0) return;

            _vertices.Add(start);
            _colors.Add(colorStart);
            _vertices.Add(end);
            _colors.Add(colorEnd);

            _indices.Add(index++);
            _indices.Add(index++);
        }

        /// <summary>
        /// Returns the grid colour with alpha modulated by distance from
        /// <paramref name="center"/> (smooth radial falloff).
        /// </summary>
        private Color32 CalculateFadedColor(Vector3 point, Vector3 center)
        {
            float sqrDistance = (point - center).sqrMagnitude;
            float alphaFactor = Mathf.Clamp01(1f - (sqrDistance / _sqrRadius));

            Color faded = _gridColor;
            faded.a = _gridColor.a * alphaFactor;
            return faded;
        }

        #endregion
    }
}
