using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Gestor de la cuadrícula (Grid) para el sistema Voxel.
    /// Redondea posiciones y genera una malla visual súper optimizada para verla in-game en AR.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [Header("Configuración de la Cuadrícula")]
        [Tooltip("Tamaño de cada voxel en metros. Por defecto 1 para escala 1:1.")]
        [SerializeField] private float _gridSize = 1f;

        [Header("Visualización In-Game (AR)")]
        [Tooltip("Activa para generar la malla visual de la cuadrícula al iniciar el juego.")]
        [SerializeField] private bool _showInGame = true;

        [Tooltip("Cuántos bloques de distancia dibujará la cuadrícula desde el centro.")]
        [SerializeField] private int _gridExtent = 5;

        [Tooltip("Material para las líneas. Debe ser un material URP Unlit para que no le afecten las sombras.")]
        [SerializeField] private Material _lineMaterial;

        // Propiedad pública de solo lectura
        public float GridSize => _gridSize;

        private void Start()
        {
            // Si la opción está activa, construimos la cuadrícula visual 3D al arrancar
            if (_showInGame)
            {
                GenerateProceduralGridMesh();
            }
        }

        #region Matemáticas del Voxel

        public Vector3 GetSnappedPosition(Vector3 rawPosition)
        {
            float snappedX = Mathf.Round(rawPosition.x / _gridSize) * _gridSize;
            float snappedY = Mathf.Round(rawPosition.y / _gridSize) * _gridSize;
            float snappedZ = Mathf.Round(rawPosition.z / _gridSize) * _gridSize;

            return new Vector3(snappedX, snappedY, snappedZ);
        }

        #endregion

        #region Generación de Malla Visual (In-Game)

        /// <summary>
        /// Crea un GameObject hijo y le inyecta una malla generada por código 
        /// usando solo vértices y líneas. Coste de rendimiento casi nulo.
        /// </summary>
        private void GenerateProceduralGridMesh()
        {
            // 1. Creamos el objeto que contendrá la malla y lo hacemos hijo del WorldContainer
            GameObject gridVisualObj = new GameObject("InGame_GridVisual");
            gridVisualObj.transform.SetParent(transform);
            gridVisualObj.transform.localPosition = Vector3.zero;
            gridVisualObj.transform.localRotation = Quaternion.identity;
            gridVisualObj.transform.localScale = Vector3.one;

            // 2. Añadimos los componentes necesarios para que Unity lo dibuje
            MeshFilter meshFilter = gridVisualObj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = gridVisualObj.AddComponent<MeshRenderer>();

            // Si olvidaste poner un material en el Inspector, creamos uno básico de emergencia
            if (_lineMaterial == null)
            {
                meshRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                {
                    color = new Color(1f, 1f, 1f, 0.3f) // Blanco semitransparente
                };
            }
            else
            {
                meshRenderer.material = _lineMaterial;
            }

            // 3. Empezamos a calcular los puntos de las líneas
            Mesh gridMesh = new Mesh { name = "ProceduralGrid" };
            List<Vector3> vertices = new List<Vector3>();
            List<int> indices = new List<int>();

            int currentIndex = 0;

            // Calculamos líneas paralelas al eje Z
            for (float x = -_gridExtent; x <= _gridExtent; x += _gridSize)
            {
                vertices.Add(new Vector3(x, 0, -_gridExtent)); // Punto inicial
                vertices.Add(new Vector3(x, 0, _gridExtent));  // Punto final
                indices.Add(currentIndex++);
                indices.Add(currentIndex++);
            }

            // Calculamos líneas paralelas al eje X
            for (float z = -_gridExtent; z <= _gridExtent; z += _gridSize)
            {
                vertices.Add(new Vector3(-_gridExtent, 0, z)); // Punto inicial
                vertices.Add(new Vector3(_gridExtent, 0, z));  // Punto final
                indices.Add(currentIndex++);
                indices.Add(currentIndex++);
            }

            // 4. Inyectamos los datos en la malla usando topología de LÍNEAS, no de triángulos
            gridMesh.SetVertices(vertices);
            gridMesh.SetIndices(indices, MeshTopology.Lines, 0);

            meshFilter.mesh = gridMesh;
        }

        #endregion
    }
}