using _Project.Scripts.Core;
using _Project.Scripts.Environment;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

// IMPORTANTE: Descomenta estas líneas si usaste namespaces en tus otros scripts
// using _Project.Scripts.Core; 
// using _Project.Scripts.Environment;

namespace _Project.Scripts.Interaction
{
    [RequireComponent(typeof(ARRaycastManager))]
    [RequireComponent(typeof(LineRenderer))]
    [RequireComponent(typeof(AudioSource))]
    public class ARBlockPlacer : MonoBehaviour
    {
        #region Variables Serializadas (Inspector)

        [Header("Dependencias de Arquitectura")]
        [Tooltip("El bloque activo que vamos a instanciar (Ej: Prefab 'Voxel_Arena').")]
        [SerializeField] private GameObject _objectToPlace;
        [Tooltip("Cerebro matemático para redondear posiciones (Snap).")]
        [SerializeField] private GridManager _gridManager;
        [Tooltip("Contenedor global. Vital para escalar el mundo en el futuro sin romper matemáticas.")]
        [SerializeField] private Transform _worldContainer;

        [Header("Configuración de Físicas y Construcción")]
        [Tooltip("Capa de Unity asignada EXCLUSIVAMENTE a los bloques (Ej: 'Voxel').")]
        [SerializeField] private LayerMask _voxelLayerMask;
        [Tooltip("Distancia máxima en metros a la que el jugador puede construir.")]
        [SerializeField] private float _maxBuildDistance = 7f;
        [Tooltip("Tolerancia para el CheckBox anti-solapamiento. Evita falsos positivos por Z-Fighting.")]
        [SerializeField] private float _overlapTolerance = 0.05f;

        [Header("Game Feel (Juice: VFX & SFX)")]
        [Tooltip("Prefab del sistema de partículas de polvo/magia al colocar un bloque.")]
        [SerializeField] private ParticleSystem _buildParticlePrefab;
        [Tooltip("Variación aleatoria del tono (Pitch) para evitar fatiga auditiva (efecto robot).")]
        [Range(0f, 0.3f)][SerializeField] private float _pitchVariation = 0.15f;

        [Header("Depuración Visual (Láser)")]
        [Tooltip("Dibuja un láser visual desde la cámara hasta el punto de impacto.")]
        [SerializeField] private bool _showDebugRay = true;
        [SerializeField] private Color _rayColor = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private float _rayWidth = 0.005f;
        [SerializeField] private float _rayDuration = 0.1f;

        #endregion

        #region Variables Privadas (Caché)

        private ARRaycastManager _raycastManager;
        private LineRenderer _lineRenderer;
        private AudioSource _audioSource;
        private Camera _mainCamera;

        // Optimizamos memoria: Reutilizamos la lista para no saturar el Garbage Collector al tocar
        private List<ARRaycastHit> _arHits = new List<ARRaycastHit>();

        #endregion

        #region Ciclo de Vida (Unity)

        private void Awake()
        {
            // Cacheo inicial estricto para no usar GetComponent en tiempo de ejecución
            _raycastManager = GetComponent<ARRaycastManager>();
            _lineRenderer = GetComponent<LineRenderer>();
            _audioSource = GetComponent<AudioSource>();
            _mainCamera = Camera.main;

            // Configuración segura del emisor de sonido
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f; // Sonido 2D (interfaz), siempre se escucha bien

            ConfigurarLineaVisual();
        }

        private void OnEnable() => EnhancedTouchSupport.Enable();
        private void OnDisable() => EnhancedTouchSupport.Disable();

        private void Update()
        {
            if (Touch.activeTouches.Count > 0)
            {
                Touch touch = Touch.activeTouches[0];

                // Solo actuamos en el frame exacto en que el dedo impacta la pantalla
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    if (_showDebugRay)
                    {
                        StartCoroutine(DibujarRayo(touch.screenPosition));
                    }
                    IntentarColocarBloque(touch.screenPosition);
                }
            }
        }

        #endregion

        #region Lógica de Interacción (Raycasting)

        /// <summary>
        /// Dispara un rayo desde la cámara. Prioriza apilar bloques usando físicas 3D. 
        /// Si falla, busca un plano de ARCore para colocar el bloque base.
        /// </summary>
        private void IntentarColocarBloque(Vector2 screenPosition)
        {
            Ray ray = _mainCamera.ScreenPointToRay(screenPosition);

            // FASE 1: Choque contra un bloque existente
            if (Physics.Raycast(ray, out RaycastHit physHit, _maxBuildDistance, _voxelLayerMask))
            {
                // Extraemos la normal de la cara tocada (en espacio local del mundo)
                Vector3 localNormal = _worldContainer.InverseTransformDirection(physHit.normal);
                Vector3 localHitPos = _worldContainer.InverseTransformPoint(physHit.transform.position);

                // La posición teórica es el bloque actual + 1 casilla hacia la normal
                Vector3 rawLocalPos = localHitPos + (localNormal * _gridManager.GridSize);

                ProcesarYColocar(rawLocalPos);
                return; // Cortamos la ejecución para no detectar el suelo AR debajo del bloque
            }

            // FASE 2: Choque contra el suelo AR
            if (_raycastManager.Raycast(screenPosition, _arHits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = _arHits[0].pose;

                // Validación: No construir si el suelo AR detectado está lejísimos
                if (Vector3.Distance(_mainCamera.transform.position, hitPose.position) > _maxBuildDistance) return;

                Vector3 rawLocalPos = _worldContainer.InverseTransformPoint(hitPose.position);
                ProcesarYColocar(rawLocalPos);
            }
        }

        #endregion

        #region Construcción Segura

        /// <summary>
        /// Valida el espacio físico y, si es seguro, instancia el bloque con sus efectos.
        /// </summary>
        private void ProcesarYColocar(Vector3 rawLocalPosition)
        {
            // 1. Matemáticas del Snap
            Vector3 snappedLocalPos = _gridManager.GetSnappedPosition(rawLocalPosition);

            // Traducimos a coordenadas del mundo real para poder usar colliders y comprobar espacio
            Vector3 worldPos = _worldContainer.TransformPoint(snappedLocalPos);
            float currentWorldScale = _worldContainer.localScale.x;

            // 2. Validaciones de Seguridad (Anti-Bugs)
            if (IsCameraInsideVoxel(worldPos, currentWorldScale))
            {
                Debug.LogWarning("Construcción bloqueada: El jugador está dentro del bloque.");
                return;
            }
            if (!IsSpaceEmpty(worldPos, currentWorldScale))
            {
                Debug.LogWarning("Construcción bloqueada: Espacio ya ocupado o solapado.");
                return;
            }

            // 3. Instanciación
            GameObject newBlock = Instantiate(_objectToPlace, _worldContainer);
            newBlock.transform.localPosition = snappedLocalPos;
            newBlock.transform.localRotation = Quaternion.identity;

            // 4. Game Feel: Partículas
            if (_buildParticlePrefab != null)
            {
                Instantiate(_buildParticlePrefab, worldPos, Quaternion.identity);
            }

            // 5. Game Feel: Audio Dinámico
            // Leemos el script VoxelBlock (que debe estar en tu Prefab) para saber qué sonido hacer
            VoxelBlock blockData = newBlock.GetComponent<VoxelBlock>();
            if (blockData != null && blockData.PlaceSound != null)
            {
                ReproducirSonidoConstruccion(blockData.PlaceSound);
            }
            else
            {
                Debug.LogWarning($"El prefab {_objectToPlace.name} no tiene el script VoxelBlock o no tiene sonido asignado.");
            }
        }

        private bool IsCameraInsideVoxel(Vector3 worldPos, float worldScale)
        {
            float scaledSize = _gridManager.GridSize * worldScale;
            Bounds voxelBounds = new Bounds(worldPos, Vector3.one * scaledSize);
            return voxelBounds.Contains(_mainCamera.transform.position);
        }

        private bool IsSpaceEmpty(Vector3 worldPos, float worldScale)
        {
            // Reducimos mínimamente el área de comprobación para no tocar las caras de los vecinos
            float halfSize = ((_gridManager.GridSize * worldScale) / 2f) - _overlapTolerance;
            return !Physics.CheckBox(worldPos, Vector3.one * halfSize, Quaternion.identity, _voxelLayerMask);
        }

        private void ReproducirSonidoConstruccion(AudioClip clipToPlay)
        {
            if (_audioSource == null) return;
            // Variación de tono para inmersión
            _audioSource.pitch = Random.Range(1f - _pitchVariation, 1f + _pitchVariation);
            _audioSource.PlayOneShot(clipToPlay);
        }

        #endregion

        #region Depuración Visual

        private IEnumerator DibujarRayo(Vector2 screenPos)
        {
            _lineRenderer.enabled = true;

            // Bajamos un poco el origen del rayo para que no salga literalmente del ojo/centro de la cámara
            Vector3 startPos = _mainCamera.transform.position + (_mainCamera.transform.up * -0.1f);
            Ray ray = _mainCamera.ScreenPointToRay(screenPos);
            Vector3 endPos = ray.origin + (ray.direction * (_maxBuildDistance / 2f));

            _lineRenderer.SetPosition(0, startPos);
            _lineRenderer.SetPosition(1, endPos);

            yield return new WaitForSeconds(_rayDuration);
            _lineRenderer.enabled = false;
        }

        private void ConfigurarLineaVisual()
        {
            _lineRenderer.startWidth = _rayWidth;
            _lineRenderer.endWidth = _rayWidth;

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader != null)
            {
                _lineRenderer.material = new Material(unlitShader);
                _lineRenderer.material.color = _rayColor;
            }
            else
            {
                _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            }

            _lineRenderer.startColor = _rayColor;
            _lineRenderer.endColor = _rayColor;
            _lineRenderer.enabled = false;
        }

        #endregion
    }
}