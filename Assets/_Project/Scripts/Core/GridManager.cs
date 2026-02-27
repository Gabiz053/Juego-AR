using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Core
{
    public class GridManager : MonoBehaviour
    {
        [Header("Configuración Matemática")]
        [SerializeField] private float _gridSize = 1f;

        [Header("Estética de la Cuadrícula (Halo)")]
        [Tooltip("Radio en unidades de bloque alrededor del jugador donde se dibujará la cuadrícula.")]
        [SerializeField] private float _gridRadius = 4f;
        [SerializeField] private Material _lineMaterial;
        [SerializeField] private Color _gridColor = new Color(1f, 1f, 1f, 0.4f);

        public float GridSize => _gridSize;

        private bool _isGridActive = false;
        private Transform _playerCamera;

        // Componentes de la malla
        private GameObject _gridVisualObj;
        private MeshFilter _meshFilter;
        private Mesh _gridMesh;
        private Vector3 _lastSnappedCenter = new Vector3(9999f, 9999f, 9999f);

        // Caché de Optimización Zero-GC
        private List<Vector3> _vertices = new List<Vector3>();
        private List<Color32> _colors = new List<Color32>();
        private List<int> _indices = new List<int>();
        private float _sqrRadius;

        private void Awake()
        {
            _sqrRadius = _gridRadius * _gridRadius;
        }

        public void ActivarGrid(Transform cameraTransform)
        {
            _playerCamera = cameraTransform;
            _isGridActive = true;

            CrearObjetoMalla();
        }

        #region Matemáticas (Snap Voxel)

        public Vector3 GetSnappedPosition(Vector3 rawPosition)
        {
            // ¡EL FIX! Usamos Floor y sumamos la mitad del bloque. 
            // Ahora el centro del cubo estará en 0.5, por lo que su cara inferior estará exactamente en 0 (el suelo real).
            float snappedX = (Mathf.Floor(rawPosition.x / _gridSize) * _gridSize) + (_gridSize / 2f);
            float snappedY = (Mathf.Floor(rawPosition.y / _gridSize) * _gridSize) + (_gridSize / 2f);
            float snappedZ = (Mathf.Floor(rawPosition.z / _gridSize) * _gridSize) + (_gridSize / 2f);

            return new Vector3(snappedX, snappedY, snappedZ);
        }

        #endregion

        #region Renderizado Dinámico

        private void Update()
        {
            if (!_isGridActive || _playerCamera == null) return;

            Vector3 localCamPos = transform.InverseTransformPoint(_playerCamera.position);
            localCamPos.y = 0;

            Vector3 snappedCamPos = GetSnappedPosition(localCamPos);

            if (snappedCamPos != _lastSnappedCenter)
            {
                _lastSnappedCenter = snappedCamPos;
                ActualizarMallaVisual();
            }
        }

        private void CrearObjetoMalla()
        {
            _gridVisualObj = new GameObject("Dynamic_GridVisual");

            // 'false' impide que Unity rompa la escala del halo al jugar en "Modo Maqueta"
            _gridVisualObj.transform.SetParent(transform, false);
            _gridVisualObj.transform.localPosition = Vector3.zero;
            _gridVisualObj.transform.localRotation = Quaternion.identity;
            _gridVisualObj.transform.localScale = Vector3.one;

            _meshFilter = _gridVisualObj.AddComponent<MeshFilter>();
            MeshRenderer renderer = _gridVisualObj.AddComponent<MeshRenderer>();

            if (_lineMaterial != null) renderer.material = _lineMaterial;

            _gridMesh = new Mesh { name = "RadialGridMesh" };
            _meshFilter.mesh = _gridMesh;
        }

        private void ActualizarMallaVisual()
        {
            _vertices.Clear();
            _colors.Clear();
            _indices.Clear();

            int currentIndex = 0;
            int steps = Mathf.CeilToInt(_gridRadius / _gridSize);

            // El centro exacto de la celda donde está el jugador
            Vector3 cellCenter = _lastSnappedCenter;

            // Para que las líneas envuelvan los bloques y no los atraviesen por el medio,
            // forzamos el origen de las líneas a los números enteros (esquinas de la celda)
            float originX = Mathf.Floor(cellCenter.x / _gridSize) * _gridSize;
            float originZ = Mathf.Floor(cellCenter.z / _gridSize) * _gridSize;

            // Aplanamos la "Y" para calcular la distancia circular en el suelo
            Vector3 fadeCenter = new Vector3(cellCenter.x, 0, cellCenter.z);

            // Tramos verticales
            for (int x = -steps; x <= steps; x++)
            {
                float xPos = originX + (x * _gridSize);
                for (int z = -steps; z < steps; z++)
                {
                    Vector3 startP = new Vector3(xPos, 0, originZ + (z * _gridSize));
                    Vector3 endP = new Vector3(xPos, 0, originZ + ((z + 1) * _gridSize));
                    AñadirSegmento(startP, endP, fadeCenter, ref currentIndex);
                }
            }

            // Tramos horizontales
            for (int z = -steps; z <= steps; z++)
            {
                float zPos = originZ + (z * _gridSize);
                for (int x = -steps; x < steps; x++)
                {
                    Vector3 startP = new Vector3(originX + (x * _gridSize), 0, zPos);
                    Vector3 endP = new Vector3(originX + ((x + 1) * _gridSize), 0, zPos);
                    AñadirSegmento(startP, endP, fadeCenter, ref currentIndex);
                }
            }

            _gridMesh.Clear();
            _gridMesh.SetVertices(_vertices);
            _gridMesh.SetColors(_colors);
            _gridMesh.SetIndices(_indices, MeshTopology.Lines, 0);

            // Obligamos a la cámara a no hacer invisible la malla
            _gridMesh.RecalculateBounds();
        }

        private void AñadirSegmento(Vector3 start, Vector3 end, Vector3 center, ref int index)
        {
            Color32 colorStart = CalcularColorDifuminado(start, center);
            Color32 colorEnd = CalcularColorDifuminado(end, center);

            if (colorStart.a == 0 && colorEnd.a == 0) return;

            _vertices.Add(start); _colors.Add(colorStart);
            _vertices.Add(end); _colors.Add(colorEnd);
            _indices.Add(index++); _indices.Add(index++);
        }

        private Color32 CalcularColorDifuminado(Vector3 point, Vector3 center)
        {
            float sqrDistance = (point - center).sqrMagnitude;
            float alphaFactor = Mathf.Clamp01(1f - (sqrDistance / _sqrRadius));

            Color finalColor = _gridColor;
            finalColor.a = _gridColor.a * alphaFactor;

            return finalColor;
        }

        #endregion
    }
}