// ------------------------------------------------------------
//  GridVisualizer.cs  -  _Project.Scripts.Core
//  Renders a radial, camera-following grid halo on the XZ plane
//  using a dynamic line mesh.
// ------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Procedurally generates a flat grid mesh centred on the player
    /// camera with a smooth radial alpha fade.  Only regenerates when
    /// the snapped centre changes cell.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Core/Grid Visualizer")]
    public class GridVisualizer : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Grid Appearance")]
        [Tooltip("Radius (grid units) around the camera where lines are drawn.")]
        [SerializeField] private float _gridRadius = 4f;

        [Tooltip("Material applied to the grid mesh (must support vertex colours).")]
        [SerializeField] private Material _lineMaterial;

        [Tooltip("Base colour for grid lines. Alpha fades radially.")]
        [SerializeField] private Color _gridColor = new Color(1f, 1f, 1f, 0.4f);

        #endregion

        #region State ---------------------------------------------

        private bool        _isActive;
        private Transform   _playerCamera;
        private float       _gridSize;
        private GridManager _gridManager;

        private GameObject _gridVisualObject;
        private MeshFilter _meshFilter;
        private Mesh       _gridMesh;

        private Vector3 _lastSnappedCenter = new Vector3(9999f, 9999f, 9999f);
        private float   _sqrRadius;

        // Zero-GC reusable buffers
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color32> _colors   = new List<Color32>();
        private readonly List<int>     _indices  = new List<int>();

        #endregion

        #region Public API ----------------------------------------

        /// <summary>Whether the grid visual is currently displayed.</summary>
        public bool IsActive => _isActive;

        /// <summary>Initialises and shows the grid.</summary>
        public void Activate(Transform cameraTransform, GridManager gridManager)
        {
            _playerCamera = cameraTransform;
            _gridManager  = gridManager;
            _gridSize     = gridManager.GridSize;
            _sqrRadius    = _gridRadius * _gridRadius;
            _isActive     = true;

            CreateMeshObject();
        }

        /// <summary>Hides and destroys the grid visual.</summary>
        public void Deactivate()
        {
            _isActive = false;

            if (_gridVisualObject != null)
            {
                Destroy(_gridVisualObject);
                _gridVisualObject = null;
            }

            _lastSnappedCenter = new Vector3(9999f, 9999f, 9999f);
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Update()
        {
            if (!_isActive || _playerCamera == null || _gridManager == null) return;

            Vector3 localCamPos = transform.InverseTransformPoint(_playerCamera.position);
            localCamPos.y = 0f;

            Vector3 snappedCenter = _gridManager.GetSnappedPosition(localCamPos);

            if (snappedCenter != _lastSnappedCenter)
            {
                _lastSnappedCenter = snappedCenter;
                RebuildMesh();
            }
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>Creates the child <c>Dynamic_GridVisual</c> GameObject with MeshFilter and MeshRenderer.</summary>
        private void CreateMeshObject()
        {
            _gridVisualObject = new GameObject("Dynamic_GridVisual");
            _gridVisualObject.transform.SetParent(transform, false);
            _gridVisualObject.transform.localPosition = Vector3.zero;
            _gridVisualObject.transform.localRotation = Quaternion.identity;
            _gridVisualObject.transform.localScale    = Vector3.one;

            _meshFilter = _gridVisualObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = _gridVisualObject.AddComponent<MeshRenderer>();

            if (_lineMaterial != null)
                meshRenderer.material = _lineMaterial;

            _gridMesh = new Mesh { name = "RadialGridMesh" };
            _meshFilter.mesh = _gridMesh;
        }

        /// <summary>
        /// Clears and regenerates the line mesh from scratch.
        /// Builds vertical (Z-axis) and horizontal (X-axis) line segments
        /// centred on <c>_lastSnappedCenter</c>, using reusable buffers
        /// to avoid GC allocations.
        /// </summary>
        private void RebuildMesh()
        {
            _vertices.Clear();
            _colors.Clear();
            _indices.Clear();

            int currentIndex = 0;
            int steps = Mathf.CeilToInt(_gridRadius / _gridSize);

            float originX = Mathf.Floor(_lastSnappedCenter.x / _gridSize) * _gridSize;
            float originZ = Mathf.Floor(_lastSnappedCenter.z / _gridSize) * _gridSize;

            Vector3 fadeCenter = new Vector3(_lastSnappedCenter.x, 0f, _lastSnappedCenter.z);

            // Vertical lines (along Z)
            for (int x = -steps; x <= steps; x++)
            {
                float xPos = originX + x * _gridSize;
                for (int z = -steps; z < steps; z++)
                {
                    Vector3 start = new Vector3(xPos, 0f, originZ + z * _gridSize);
                    Vector3 end   = new Vector3(xPos, 0f, originZ + (z + 1) * _gridSize);
                    AddSegment(start, end, fadeCenter, ref currentIndex);
                }
            }

            // Horizontal lines (along X)
            for (int z = -steps; z <= steps; z++)
            {
                float zPos = originZ + z * _gridSize;
                for (int x = -steps; x < steps; x++)
                {
                    Vector3 start = new Vector3(originX + x * _gridSize, 0f, zPos);
                    Vector3 end   = new Vector3(originX + (x + 1) * _gridSize, 0f, zPos);
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
        /// Adds a two-vertex line segment to the mesh buffers.
        /// Skips fully transparent segments (both endpoints faded to zero).
        /// </summary>
        private void AddSegment(Vector3 start, Vector3 end, Vector3 center, ref int index)
        {
            Color32 colorStart = FadedColor(start, center);
            Color32 colorEnd   = FadedColor(end, center);

            if (colorStart.a == 0 && colorEnd.a == 0) return;

            _vertices.Add(start);
            _colors.Add(colorStart);
            _vertices.Add(end);
            _colors.Add(colorEnd);

            _indices.Add(index++);
            _indices.Add(index++);
        }

        /// <summary>
        /// Returns the grid colour with alpha modulated by radial distance
        /// from <paramref name="center"/>.  Points beyond <c>_gridRadius</c>
        /// return fully transparent.
        /// </summary>
        private Color32 FadedColor(Vector3 point, Vector3 center)
        {
            float sqrDist    = (point - center).sqrMagnitude;
            float alphaFactor = Mathf.Clamp01(1f - sqrDist / _sqrRadius);

            Color faded = _gridColor;
            faded.a     = _gridColor.a * alphaFactor;
            return faded;
        }

        #endregion
    }
}
