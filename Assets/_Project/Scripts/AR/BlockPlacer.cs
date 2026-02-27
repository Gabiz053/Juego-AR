using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using _Project.Scripts.Environment; // Para poder leer VoxelBlock
using _Project.Scripts.Core;

namespace _Project.Scripts.Interaction
{
    [RequireComponent(typeof(ARRaycastManager), typeof(LineRenderer), typeof(AudioSource))]
    public class ARBlockPlacer : MonoBehaviour
    {
        #region Inspector

        [Header("Dependencias de Arquitectura")]
        [SerializeField] private ToolManager _toolManager;
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private ARWorldManager _worldManager;
        [SerializeField] private Transform _worldContainer;

        [Header("Configuración Voxel")]
        [SerializeField] private LayerMask _voxelLayerMask;
        [SerializeField] private float _maxBuildDistance = 7f;
        [SerializeField] private float _overlapTolerance = 0.05f;

        [Header("Game Feel")]
        [SerializeField] private ParticleSystem _buildParticlePrefab;
        [Range(0f, 0.3f)][SerializeField] private float _pitchVariation = 0.15f;

        [Header("Debug")]
        [SerializeField] private bool _showDebugRay = true;
        [SerializeField] private Color _rayColor = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private float _rayWidth = 0.005f;

        #endregion

        #region Caché Privada

        private ARRaycastManager _raycastManager;
        private LineRenderer _lineRenderer;
        private AudioSource _audioSource;
        private Camera _mainCamera;
        private List<ARRaycastHit> _arHits = new List<ARRaycastHit>();

        #endregion

        private void Awake()
        {
            _raycastManager = GetComponent<ARRaycastManager>();
            _lineRenderer = GetComponent<LineRenderer>();
            _audioSource = GetComponent<AudioSource>();
            _mainCamera = Camera.main;

            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;

            ConfigurarLineaVisual();
        }

        private void OnEnable() => EnhancedTouchSupport.Enable();
        private void OnDisable() => EnhancedTouchSupport.Disable();

        private void Update()
        {
            if (Touch.activeTouches.Count > 0)
            {
                Touch touch = Touch.activeTouches[0];

                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    // Solo lanzamos láser y construimos si la herramienta actual es de construir
                    if (_toolManager.IsBuildingTool())
                    {
                        if (_showDebugRay) StartCoroutine(DibujarRayo(touch.screenPosition));
                        IntentarColocarBloque(touch.screenPosition);
                    }
                }
            }
        }

        private void IntentarColocarBloque(Vector2 screenPosition)
        {
            Ray ray = _mainCamera.ScreenPointToRay(screenPosition);

            // FASE 1: Prioridad a bloques existentes (Física 3D)
            if (Physics.Raycast(ray, out RaycastHit physHit, _maxBuildDistance, _voxelLayerMask))
            {
                Vector3 localNormal = _worldContainer.InverseTransformDirection(physHit.normal);
                Vector3 localHitPos = _worldContainer.InverseTransformPoint(physHit.transform.position);
                Vector3 rawLocalPos = localHitPos + (localNormal * _gridManager.GridSize);

                ProcesarYColocar(rawLocalPos);
                return;
            }

            // FASE 2: Si falla, buscar el suelo AR
            if (_raycastManager.Raycast(screenPosition, _arHits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = _arHits[0].pose;

                float sqrDist = (hitPose.position - _mainCamera.transform.position).sqrMagnitude;
                if (sqrDist > (_maxBuildDistance * _maxBuildDistance)) return;

                if (!_worldManager.IsWorldAnchored)
                {
                    _worldManager.AnchorWorld(hitPose, _mainCamera.transform);
                }

                // Pasamos el punto de impacto al espacio de nuestro mundo anclado
                Vector3 rawLocalPos = _worldContainer.InverseTransformPoint(hitPose.position);

                // ¡LA SOLUCIÓN ELEGANTE! 
                // En lugar de forzar y = 0, redondeamos la altura al Nivel (piso) más cercano.
                // Esto absorbe los pequeños temblores del suelo de ARCore, pero permite 
                // construir en mesas o escalones reales sin aplastarlos al suelo.
                float currentGridSize = _gridManager.GridSize;
                rawLocalPos.y = Mathf.Round(rawLocalPos.y / currentGridSize) * currentGridSize;

                ProcesarYColocar(rawLocalPos);
            }
        }

        private void ProcesarYColocar(Vector3 rawLocalPosition)
        {
            Vector3 snappedLocalPos = _gridManager.GetSnappedPosition(rawLocalPosition);
            Vector3 worldPos = _worldContainer.TransformPoint(snappedLocalPos);
            float worldScale = _worldContainer.localScale.x;

            if (IsCameraInsideVoxel(worldPos, worldScale)) return;
            if (!IsSpaceEmpty(worldPos, worldScale)) return;



            // Le pedimos al ToolManager el prefab que toca poner
            GameObject prefabToPlace = _toolManager.GetCurrentBlockPrefab();
            if (prefabToPlace == null) return; // Seguridad extra

            GameObject newBlock = Instantiate(prefabToPlace, _worldContainer);
            newBlock.transform.SetLocalPositionAndRotation(snappedLocalPos, Quaternion.identity);

            if (_buildParticlePrefab != null) Instantiate(_buildParticlePrefab, worldPos, Quaternion.identity);

            VoxelBlock blockData = newBlock.GetComponent<VoxelBlock>();
            if (blockData != null && blockData.PlaceSound != null) ReproducirAudio(blockData.PlaceSound);
        }

        private bool IsCameraInsideVoxel(Vector3 worldPos, float worldScale)
        {
            float scaledSize = _gridManager.GridSize * worldScale;
            return new Bounds(worldPos, Vector3.one * scaledSize).Contains(_mainCamera.transform.position);
        }

        private bool IsSpaceEmpty(Vector3 worldPos, float worldScale)
        {
            float halfSize = ((_gridManager.GridSize * worldScale) / 2f) - _overlapTolerance;
            return !Physics.CheckBox(worldPos, Vector3.one * halfSize, Quaternion.identity, _voxelLayerMask);
        }

        private void ReproducirAudio(AudioClip clip)
        {
            if (_audioSource == null) return;
            _audioSource.pitch = Random.Range(1f - _pitchVariation, 1f + _pitchVariation);
            _audioSource.PlayOneShot(clip);
        }

        private IEnumerator DibujarRayo(Vector2 screenPos)
        {
            _lineRenderer.enabled = true;
            Vector3 startPos = _mainCamera.transform.position + (_mainCamera.transform.up * -0.1f);
            Ray ray = _mainCamera.ScreenPointToRay(screenPos);
            Vector3 endPos = ray.origin + (ray.direction * (_maxBuildDistance / 2f));

            _lineRenderer.SetPosition(0, startPos);
            _lineRenderer.SetPosition(1, endPos);
            yield return new WaitForSeconds(0.1f);
            _lineRenderer.enabled = false;
        }

        private void ConfigurarLineaVisual()
        {
            _lineRenderer.startWidth = _rayWidth;
            _lineRenderer.endWidth = _rayWidth;

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            _lineRenderer.material = unlit != null ? new Material(unlit) : new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.material.color = _rayColor;
            _lineRenderer.startColor = _rayColor;
            _lineRenderer.endColor = _rayColor;
            _lineRenderer.enabled = false;
        }
    }
}