// ------------------------------------------------------------
//  PlowTool.cs  -  _Project.Scripts.Interaction
//  Decoration tool for placing procedural pebble details.
// ------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using _Project.Scripts.Infrastructure;
using _Project.Scripts.Voxel;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Plow (decoration) tool � tap any surface to scatter procedurally
    /// generated pebble details.  Active only when
    /// <see cref="ToolManager.CurrentTool"/> == <see cref="ToolType.Tool_Plow"/>.
    /// Placement feedback (audio + VFX) is handled by each pebble prefab's
    /// <see cref="BlockSpawn"/> component.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Interaction/Plow Tool")]
    public class PlowTool : MonoBehaviour
    {
        #region Constants -----------------------------------------

        /// <summary>Minimum sqrMagnitude for a tangent vector to be considered valid.</summary>
        private const float TANGENT_SQR_THRESHOLD = 0.01f;

        /// <summary>Full circle rotation in degrees for random pebble orientation.</summary>
        private const float FULL_ROTATION_DEG = 360f;

        #endregion

        #region Inspector -----------------------------------------

        [Header("Dependencies")]
        [Tooltip("AR Raycast Manager -- for placing on detected ground planes.")]
        [SerializeField] private ARRaycastManager _arRaycastManager;

        [Tooltip("WorldContainer -- all decorations are parented here.")]
        [SerializeField] private Transform _worldContainer;

        [Header("Pebble Prefabs")]
        [Tooltip("Pool of pebble prefabs to pick from at random.")]
        [SerializeField] private GameObject[] _pebblePrefabs;

        [Header("Placement")]
        [Tooltip("Layer mask for voxel blocks (physics raycast).")]
        [SerializeField] private LayerMask _voxelLayerMask;

        [Tooltip("Layer mask for pebble objects (support checks ignore these).")]
        [SerializeField] private LayerMask _pebbleLayerMask;

        [Tooltip("Maximum placement distance (metres).")]
        [SerializeField] private float _maxPlaceDistance = 7f;

        [Tooltip("Minimum distance from camera.")]
        [SerializeField] private float _minPlaceDistance = 0.3f;

        [Header("Randomisation")]
        [Tooltip("Min scale multiplier relative to the prefab scale.")]
        [SerializeField] private float _scaleMin = 0.80f;

        [Tooltip("Max scale multiplier relative to the prefab scale.")]
        [SerializeField] private float _scaleMax = 1.20f;

        [Tooltip("Random XZ offset radius (metres) for scatter.")]
        [SerializeField] private float _scatterRadius = 0.03f;

        [Header("Brush / Continuous")]
        [Tooltip("Minimum seconds between placements while brush-dragging.")]
        [SerializeField] private float _brushCooldown = 0.06f;

        #endregion

        #region State ---------------------------------------------

        private Camera _mainCamera;
        private IToolManager _toolManager;
        private readonly List<ARRaycastHit> _arHits = new List<ARRaycastHit>();
        private float _lastBrushTime = float.NegativeInfinity;

        #endregion

        #region Public API ----------------------------------------

        /// <summary>
        /// Called by <see cref="BrushTool"/> on each throttled drag frame.
        /// Returns <c>true</c> if a pebble was placed.
        /// </summary>
        public bool PlacePebbleAtScreen(Vector2 screenPos)
        {
            if (Time.time - _lastBrushTime < _brushCooldown) return false;
            bool placed = TryPlacePebble(screenPos);
            if (placed) _lastBrushTime = Time.time;
            return placed;
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            _mainCamera = Camera.main;
            ServiceLocator.TryGet<IToolManager>(out _toolManager);
        }
        private void OnEnable()  => EnhancedTouchSupport.Enable();
        private void OnDisable() => EnhancedTouchSupport.Disable();
        private void Start()     => ValidateReferences();

        private void Update()
        {
            if (_toolManager == null || _toolManager.CurrentTool != ToolType.Tool_Plow) return;
            if (Touch.activeTouches.Count == 0) return;

            Touch touch = Touch.activeTouches[0];
            if (touch.phase != UnityEngine.InputSystem.TouchPhase.Began) return;

            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject(touch.touchId))
                return;

            TryPlacePebble(touch.screenPosition);
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Casts a physics ray (voxel surface) then falls back to an AR
        /// plane ray.  Returns <c>true</c> when a pebble was placed.
        /// </summary>
        private bool TryPlacePebble(Vector2 screenPos)
        {
            if (_pebblePrefabs == null || _pebblePrefabs.Length == 0) return false;
            if (_mainCamera == null) return false;

            Ray ray = _mainCamera.ScreenPointToRay(screenPos);

            Vector3 hitPoint  = Vector3.zero;
            Vector3 hitNormal = Vector3.up;
            bool    didHit    = false;
            bool    onARPlane = false;

            if (Physics.Raycast(ray, out RaycastHit physHit, _maxPlaceDistance, _voxelLayerMask))
            {
                hitPoint  = physHit.point;
                hitNormal = physHit.normal;
                didHit    = true;
            }
            else if (_arRaycastManager != null &&
                     _arRaycastManager.Raycast(screenPos, _arHits, TrackableType.PlaneWithinPolygon))
            {
                Pose pose     = _arHits[0].pose;
                float sqrDist = (pose.position - _mainCamera.transform.position).sqrMagnitude;
                if (sqrDist <= _maxPlaceDistance * _maxPlaceDistance)
                {
                    hitPoint  = pose.position;
                    hitNormal = Vector3.up;
                    didHit    = true;
                    onARPlane = true;
                }
            }

            if (!didHit) return false;

            float sqrDistToCamera = (hitPoint - _mainCamera.transform.position).sqrMagnitude;
            if (sqrDistToCamera < _minPlaceDistance * _minPlaceDistance) return false;

            PlaceAt(hitPoint, hitNormal, onARPlane);
            return true;
        }

        /// <summary>
        /// Instantiates a random pebble prefab at <paramref name="worldPoint"/>,
        /// applies scatter, rotation to surface normal, random scale,
        /// and starts the spawn animation.  Audio and VFX are handled by
        /// the pebble prefab's <see cref="BlockSpawn"/> component.
        /// </summary>
        private void PlaceAt(Vector3 worldPoint, Vector3 surfaceNormal, bool onARPlane)
        {
            GameObject prefab = _pebblePrefabs[Random.Range(0, _pebblePrefabs.Length)];
            if (prefab == null) return;

            // Scatter projected onto the surface plane
            Vector2 scatter2D = Random.insideUnitCircle * _scatterRadius;
            Vector3 tangent   = Vector3.Cross(surfaceNormal, Vector3.up);
            if (tangent.sqrMagnitude < TANGENT_SQR_THRESHOLD)
                tangent = Vector3.Cross(surfaceNormal, Vector3.right);
            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(surfaceNormal, tangent);
            Vector3 spawnPos  = worldPoint + tangent * scatter2D.x + bitangent * scatter2D.y;

            GameObject pebble = Instantiate(prefab, _worldContainer);
            pebble.transform.position = spawnPos;

            Quaternion normalRot = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
            Quaternion yRot      = Quaternion.AngleAxis(Random.Range(0f, FULL_ROTATION_DEG), surfaceNormal);
            pebble.transform.rotation = yRot * normalRot;

            float scale = Random.Range(_scaleMin, _scaleMax);
            pebble.transform.localScale = prefab.transform.localScale * scale;

            PebbleSupport support = pebble.GetComponent<PebbleSupport>();
            support?.Configure(onARPlane, _voxelLayerMask, surfaceNormal);

            BlockDestroy blockDestroy = pebble.GetComponent<BlockDestroy>();

            BlockSpawn blockSpawn = pebble.GetComponent<BlockSpawn>();
            if (blockSpawn != null)
            {
                blockSpawn.Play(_mainCamera.transform, () =>
                {
                    blockDestroy?.SetReady();
                    support?.Arm();
                });
            }
            else
            {
                blockDestroy?.SetReady();
                support?.Arm();
            }

            EventBus.Publish(new PebblePlacedEvent());
            Debug.Log($"[PlowTool] Placed pebble at {worldPoint}.");
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_toolManager == null)
                Debug.LogWarning("[PlowTool] _toolManager is not assigned.", this);
            if (_arRaycastManager == null)
                Debug.LogWarning("[PlowTool] _arRaycastManager is not assigned.", this);
            if (_worldContainer == null)
                Debug.LogWarning("[PlowTool] _worldContainer is not assigned.", this);
            if (_mainCamera == null)
                Debug.LogWarning("[PlowTool] _mainCamera is not assigned.", this);
            if (_pebblePrefabs == null || _pebblePrefabs.Length == 0)
                Debug.LogWarning("[PlowTool] _pebblePrefabs is not assigned.", this);
        }

        #endregion
    }
}
