// ??????????????????????????????????????????????
//  PlowTool.cs  ·  _Project.Scripts.Interaction
//  Decoration tool — tap to place procedural pebble details on any
//  surface (AR plane or top face of existing blocks).
// ??????????????????????????????????????????????

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using _Project.Scripts.Voxel;
using _Project.Scripts.Core;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Plow (decoration) tool — tap any surface to scatter procedurally
    /// generated pebble details.<br/>
    /// <br/>
    /// <b>Architecture:</b><br/>
    /// • Completely independent script; attach to <c>XR Origin (Mobile AR)</c>.<br/>
    /// • Uses its own AR + physics raycast — does not touch <see cref="ARBlockPlacer"/>.<br/>
    /// • Active only when <see cref="ToolManager.CurrentTool"/> == <see cref="ToolType.Tool_Plow"/>.<br/>
    /// • Pebble prefabs carry <see cref="BlockSpawn"/> and <see cref="BlockDestroy"/>
    ///   so they animate in/out and can be mined exactly like full blocks.<br/>
    /// • Each placement applies random Y rotation + scale variance so no two
    ///   pebbles look identical.<br/>
    /// • Surface normal alignment: the pebble's up axis is rotated to match
    ///   the hit normal so it sits flush on sloped block faces.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Interaction/Plow Tool")]
    public class PlowTool : MonoBehaviour
    {
        #region Inspector ?????????????????????????????????????

        [Header("Dependencies")]
        [Tooltip("ToolManager — only active when Tool_Plow is selected.")]
        [SerializeField] private ToolManager _toolManager;

        [Tooltip("AR Raycast Manager — for placing on detected ground planes.")]
        [SerializeField] private ARRaycastManager _arRaycastManager;

        [Tooltip("WorldContainer — all decoration details are parented here.")]
        [SerializeField] private Transform _worldContainer;

        [Header("Services")]
        [Tooltip("Audio service — plays the pebble placement sound.")]
        [SerializeField] private GameAudioService _audioService;

        [Tooltip("Break VFX prefab injected into each pebble's BlockDestroy.")]
        [SerializeField] private GameObject _breakVfxPrefab;

        [Header("Harmony")]
        [Tooltip("HarmonyService — notified on every pebble place.")]
        [SerializeField] private HarmonyService _harmonyService;

        [Header("Pebble Prefabs")]
        [Tooltip("Pool of pebble prefabs to pick from at random. " +
                 "Each should have ProceduralPebble + BlockSpawn + BlockDestroy.")]
        [SerializeField] private GameObject[] _pebblePrefabs;

        [Header("Pebble Audio")]
        [Tooltip("Clips played at random when a pebble is placed. Leave empty to use the audio service directly.")]
        [SerializeField] private AudioClip[] _placeSounds;

        [Tooltip("Clips played at random when a pebble is destroyed (mined).")]
        [SerializeField] private AudioClip[] _breakSounds;

        [Header("Placement")]
        [Tooltip("Layer mask that includes voxel blocks so the physics raycast hits them.")]
        [SerializeField] private LayerMask _voxelLayerMask;

        [Tooltip("Layer mask for pebble objects — used so support checks ignore other pebbles.")]
        [SerializeField] private LayerMask _pebbleLayerMask;

        [Tooltip("Maximum distance (metres) at which a pebble can be placed.")]
        [SerializeField] private float _maxPlaceDistance = 7f;

        [Tooltip("Minimum distance (metres) from the camera — prevents placing right in front.")]
        [SerializeField] private float _minPlaceDistance = 0.3f;

        [Header("Randomisation")]
        [Tooltip("Scale is randomised between this and _scaleMax relative to the prefab scale.")]
        [SerializeField] private float _scaleMin = 0.80f;

        [Tooltip("Scale is randomised between _scaleMin and this relative to the prefab scale.")]
        [SerializeField] private float _scaleMax = 1.20f;

        [Tooltip("Random offset radius (metres) applied in the XZ plane so pebbles don't " +
                 "stack perfectly on top of each other on the same tap point.")]
        [SerializeField] private float _scatterRadius = 0.03f;

        [Header("Brush / Continuous")]
        [Tooltip("Minimum seconds between pebble placements while dragging with the brush active.")]
        [SerializeField] private float _brushCooldown = 0.06f;

        #endregion

        #region Runtime ???????????????????????????????????????

        private Camera _mainCamera;
        private readonly List<ARRaycastHit> _arHits = new List<ARRaycastHit>();
        private float _lastBrushTime = -999f;

        #endregion

        #region Unity Lifecycle ????????????????????????????????

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void OnEnable()  => EnhancedTouchSupport.Enable();
        private void OnDisable() => EnhancedTouchSupport.Disable();

        private void Start() => ValidateReferences();

        private void Update()
        {
            if (_toolManager == null || _toolManager.CurrentTool != ToolType.Tool_Plow) return;
            if (Touch.activeTouches.Count == 0) return;

            Touch touch = Touch.activeTouches[0];

            // Tap ? place single pebble.
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                if (EventSystem.current != null &&
                    EventSystem.current.IsPointerOverGameObject(touch.touchId))
                    return;

                TryPlacePebble(touch.screenPosition);
            }
        }

        #endregion

        #region Public API ????????????????????????????????????

        /// <summary>
        /// Called by <see cref="BrushTool"/> on every throttled drag frame so
        /// pebbles are scattered continuously without grid alignment.
        /// Returns <c>true</c> if a pebble was placed (respects <see cref="_brushCooldown"/>).
        /// </summary>
        public bool PlacePebbleAtScreen(Vector2 screenPos)
        {
            if (Time.time - _lastBrushTime < _brushCooldown) return false;
            bool placed = TryPlacePebble(screenPos);
            if (placed) _lastBrushTime = Time.time;
            return placed;
        }

        #endregion

        #region Placement ?????????????????????????????????????

        private bool TryPlacePebble(Vector2 screenPos)
        {
            if (_pebblePrefabs == null || _pebblePrefabs.Length == 0)
            {
                Debug.LogWarning("[PlowTool] No pebble prefabs assigned.", this);
                return false;
            }

            Ray ray = _mainCamera.ScreenPointToRay(screenPos);

            Vector3 hitPoint   = Vector3.zero;
            Vector3 hitNormal  = Vector3.up;
            bool    didHit     = false;
            bool    onARPlane  = false;

            // 1. Physics raycast — hits top faces of existing blocks.
            if (Physics.Raycast(ray, out RaycastHit physHit, _maxPlaceDistance, _voxelLayerMask))
            {
                hitPoint  = physHit.point;
                hitNormal = physHit.normal;
                didHit    = true;
                onARPlane = false;
            }
            // 2. AR plane raycast — ground plane.
            else if (_arRaycastManager != null &&
                     _arRaycastManager.Raycast(screenPos, _arHits, TrackableType.PlaneWithinPolygon))
            {
                Pose pose = _arHits[0].pose;
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

            // Distance guard.
            float dist = Vector3.Distance(hitPoint, _mainCamera.transform.position);
            if (dist < _minPlaceDistance) return false;

            PlaceAt(hitPoint, hitNormal, onARPlane);
            return true;
        }

        private void PlaceAt(Vector3 worldPoint, Vector3 surfaceNormal, bool onARPlane)
        {
            int idx = Random.Range(0, _pebblePrefabs.Length);
            GameObject prefab = _pebblePrefabs[idx];
            if (prefab == null) return;

            // Scatter projected onto the surface plane — works on walls and floors.
            Vector2 scatter2D = Random.insideUnitCircle * _scatterRadius;
            Vector3 tangent   = Vector3.Cross(surfaceNormal, Vector3.up);
            if (tangent.sqrMagnitude < 0.01f)
                tangent = Vector3.Cross(surfaceNormal, Vector3.right);
            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(surfaceNormal, tangent);
            Vector3 spawnPos  = worldPoint + tangent * scatter2D.x + bitangent * scatter2D.y;

            GameObject pebble = Object.Instantiate(prefab, _worldContainer);
            pebble.transform.position = spawnPos;

            // Align to surface normal and randomise axial rotation.
            Quaternion normalRot = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
            Quaternion yRot      = Quaternion.AngleAxis(Random.Range(0f, 360f), surfaceNormal);
            pebble.transform.rotation = yRot * normalRot;

            float scale = Random.Range(_scaleMin, _scaleMax);
            pebble.transform.localScale = prefab.transform.localScale * scale;

            // Configure support monitor — pass surface normal so wall pebbles cast correctly.
            PebbleSupport support = pebble.GetComponent<PebbleSupport>();
            support?.Configure(onARPlane, _voxelLayerMask, surfaceNormal);

            // Inject break VFX, audio service and break clips into BlockDestroy.
            BlockDestroy blockDestroy = pebble.GetComponent<BlockDestroy>();
            blockDestroy?.InjectSharedRefs(_breakVfxPrefab, _audioService, _breakSounds);

            // Run spawn animation, then arm support + BlockDestroy together.
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

            if (_audioService != null && _placeSounds != null && _placeSounds.Length > 0)
                _audioService.PlayOneShot(_placeSounds);

            _harmonyService?.NotifyPebblePlaced();

            Debug.Log($"[PlowTool] Pebble '{prefab.name}' placed at {spawnPos} (ARPlane={onARPlane}).");
        }

        #endregion

        #region Validation ????????????????????????????????????

        private void ValidateReferences()
        {
            if (_toolManager == null)
                Debug.LogError("[PlowTool] _toolManager is not assigned!", this);
            if (_arRaycastManager == null)
                Debug.LogError("[PlowTool] _arRaycastManager is not assigned!", this);
            if (_worldContainer == null)
                Debug.LogError("[PlowTool] _worldContainer is not assigned!", this);
            if (_mainCamera == null)
                Debug.LogError("[PlowTool] Camera.main not found!", this);
            if (_pebblePrefabs == null || _pebblePrefabs.Length == 0)
                Debug.LogWarning("[PlowTool] _pebblePrefabs is empty — tool will do nothing.", this);
            if (_audioService == null)
                Debug.LogWarning("[PlowTool] _audioService not assigned — pebbles will be silent.", this);
        }

        #endregion
    }
}
