// ──────────────────────────────────────────────
//  DebugRayVisualizer.cs  ·  _Project.Scripts.Interaction
//  Draws a brief debug ray from the camera on each touch tap.
// ──────────────────────────────────────────────

using System.Collections;
using UnityEngine;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Draws a short-lived line from the camera toward the touch point
    /// using a <see cref="LineRenderer"/>. Useful during development to
    /// visualise where AR raycasts originate.<br/>
    /// Extracted from <see cref="ARBlockPlacer"/> so debug visualisation
    /// logic does not pollute production code.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Interaction/Debug Ray Visualizer")]
    public class DebugRayVisualizer : MonoBehaviour
    {
        #region Constants ─────────────────────────────────────

        /// <summary>Small downward offset so the ray starts just below the camera.</summary>
        private const float CAMERA_OFFSET = -0.1f;

        /// <summary>Duration (seconds) the ray stays visible after a tap.</summary>
        private const float RAY_DURATION = 0.1f;

        /// <summary>Fraction of <see cref="_maxRayDistance"/> used as drawn length.</summary>
        private const float RAY_LENGTH_FACTOR = 0.5f;

        /// <summary>Cached yield instruction — avoids GC allocation per tap.</summary>
        private static readonly WaitForSeconds RayWait = new WaitForSeconds(RAY_DURATION);

        #endregion

        #region Inspector ─────────────────────────────────────

        [Header("Dependencies")]
        [Tooltip("LineRenderer used to draw the debug ray. Assign manually in the Inspector.")]
        [SerializeField] private LineRenderer _lineRenderer;

        [Header("Settings")]
        [Tooltip("Enable or disable the debug ray at runtime.")]
        [SerializeField] private bool _enabled = true;

        [Tooltip("Maximum logical ray distance — the drawn line is half this length.")]
        [SerializeField] private float _maxRayDistance = 7f;

        [Header("Appearance")]
        [Tooltip("Color of the debug ray line.")]
        [SerializeField] private Color _rayColor = new Color(1f, 1f, 1f, 0.5f);

        [Tooltip("Width of the debug ray line (world units).")]
        [SerializeField] private float _rayWidth = 0.005f;

        #endregion

        #region Cached Components ─────────────────────────────

        private Camera _mainCamera;

        #endregion

        #region Unity Lifecycle ────────────────────────────────

        private void Awake()
        {
            _mainCamera = Camera.main;

            SetupLineRenderer();

            Debug.Log("[DebugRayVisualizer] Awake — line renderer configured.");
        }

        private void Start()
        {
            ValidateReferences();
            Debug.Log($"[DebugRayVisualizer] Initialized — enabled: {_enabled}.");
        }

        #endregion

        #region Public API ────────────────────────────────────

        /// <summary>
        /// Whether the visualizer is currently active.
        /// Can be toggled at runtime from the Inspector or via script.
        /// </summary>
        public bool IsEnabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                Debug.Log($"[DebugRayVisualizer] Enabled set to {_enabled}.");
            }
        }

        /// <summary>
        /// Call this from the interaction system when a touch occurs.
        /// If enabled, draws a short ray from the camera toward the
        /// screen position. If disabled, the call is a no-op.
        /// </summary>
        /// <param name="screenPosition">Touch position in screen coordinates.</param>
        public void ShowRay(Vector2 screenPosition)
        {
            if (!_enabled || _mainCamera == null) return;

            StartCoroutine(DrawRayRoutine(screenPosition));
        }

        /// <summary>
        /// Updates the maximum distance used to calculate drawn ray length.
        /// Keeps the debug ray in sync with the build distance.
        /// </summary>
        /// <param name="distance">New max ray distance (metres).</param>
        public void SetMaxDistance(float distance)
        {
            _maxRayDistance = distance;
        }

        #endregion

        #region Internals ─────────────────────────────────────

        /// <summary>
        /// Coroutine that enables the line, positions it, waits briefly,
        /// then hides it again.
        /// </summary>
        private IEnumerator DrawRayRoutine(Vector2 screenPosition)
        {
            _lineRenderer.enabled = true;

            Vector3 startPos = _mainCamera.transform.position
                             + (_mainCamera.transform.up * CAMERA_OFFSET);

            Ray ray    = _mainCamera.ScreenPointToRay(screenPosition);
            Vector3 endPos = ray.origin + (ray.direction * (_maxRayDistance * RAY_LENGTH_FACTOR));

            _lineRenderer.SetPosition(0, startPos);
            _lineRenderer.SetPosition(1, endPos);

            yield return RayWait;

            _lineRenderer.enabled = false;
        }

        /// <summary>
        /// Initialises the <see cref="LineRenderer"/> — width, colour, material.
        /// </summary>
        private void SetupLineRenderer()
        {
            _lineRenderer.startWidth = _rayWidth;
            _lineRenderer.endWidth   = _rayWidth;

            // Use the URP Unlit shader for a clean line, with a fallback.
            Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
            _lineRenderer.material = urpUnlit != null
                ? new Material(urpUnlit)
                : new Material(Shader.Find("Sprites/Default"));

            _lineRenderer.material.color = _rayColor;
            _lineRenderer.startColor     = _rayColor;
            _lineRenderer.endColor       = _rayColor;
            _lineRenderer.enabled        = false;
        }

        #endregion

        #region Validation ────────────────────────────────────

        /// <summary>
        /// Logs errors for any missing components at startup.
        /// </summary>
        private void ValidateReferences()
        {
            if (_lineRenderer == null)
                Debug.LogError("[DebugRayVisualizer] LineRenderer not found!", this);
            if (_mainCamera == null)
                Debug.LogError("[DebugRayVisualizer] Main Camera not found!", this);
        }

        #endregion
    }
}
