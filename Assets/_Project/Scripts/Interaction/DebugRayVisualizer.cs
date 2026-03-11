// ------------------------------------------------------------
//  DebugRayVisualizer.cs  -  _Project.Scripts.Interaction
//  Draws a brief debug ray from the camera on each touch tap.
// ------------------------------------------------------------

using System.Collections;
using UnityEngine;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Draws a short-lived line from the camera toward the touch point
    /// using a <see cref="LineRenderer"/>.  Development-only visual aid.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Interaction/Debug Ray Visualizer")]
    public class DebugRayVisualizer : MonoBehaviour
    {
        #region Constants -----------------------------------------

        private const float CAMERA_OFFSET    = -0.1f;
        private const float RAY_DURATION     = 0.1f;
        private const float RAY_LENGTH_FACTOR = 0.5f;

        private static readonly WaitForSeconds RAY_WAIT = new WaitForSeconds(RAY_DURATION);

        #endregion

        #region Inspector -----------------------------------------

        [Header("Dependencies")]
        [Tooltip("LineRenderer used to draw the debug ray.")]
        [SerializeField] private LineRenderer _lineRenderer;

        [Header("Settings")]
        [Tooltip("Enable or disable the debug ray at runtime.")]
        [SerializeField] private bool _enabled = true;

        [Tooltip("Maximum logical ray distance.")]
        [SerializeField] private float _maxRayDistance = 7f;

        [Header("Appearance")]
        [Tooltip("Color of the debug ray.")]
        [SerializeField] private Color _rayColor = new Color(1f, 1f, 1f, 0.5f);

        [Tooltip("Width of the debug ray (world units).")]
        [SerializeField] private float _rayWidth = 0.005f;

        #endregion

        #region State ---------------------------------------------

        private Camera _mainCamera;

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            _mainCamera = Camera.main;
            SetupLineRenderer();
        }

        private void Start()
        {
            ValidateReferences();
        }

        #endregion

        #region Public API ----------------------------------------

        /// <summary>Whether the visualizer is active.</summary>
        public bool IsEnabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        /// <summary>Draws a short ray from the camera toward the screen position.</summary>
        public void ShowRay(Vector2 screenPosition)
        {
            if (!_enabled || _mainCamera == null) return;
            StartCoroutine(DrawRayRoutine(screenPosition));
        }

        /// <summary>Updates the max distance used to calculate drawn ray length.</summary>
        public void SetMaxDistance(float distance) => _maxRayDistance = distance;

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Shows the line for <see cref="RAY_DURATION"/> seconds then hides it.
        /// </summary>
        private IEnumerator DrawRayRoutine(Vector2 screenPosition)
        {
            _lineRenderer.enabled = true;

            Vector3 startPos = _mainCamera.transform.position
                             + _mainCamera.transform.up * CAMERA_OFFSET;

            Ray ray        = _mainCamera.ScreenPointToRay(screenPosition);
            Vector3 endPos = ray.origin + ray.direction * (_maxRayDistance * RAY_LENGTH_FACTOR);

            _lineRenderer.SetPosition(0, startPos);
            _lineRenderer.SetPosition(1, endPos);

            yield return RAY_WAIT;

            _lineRenderer.enabled = false;
        }

        /// <summary>
        /// Configures the <see cref="LineRenderer"/> width, colour and
        /// material on first creation.  Uses URP/Unlit with fallback.
        /// </summary>
        private void SetupLineRenderer()
        {
            if (_lineRenderer == null) return;

            _lineRenderer.startWidth = _rayWidth;
            _lineRenderer.endWidth   = _rayWidth;

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

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_lineRenderer == null)
                Debug.LogError("[DebugRayVisualizer] _lineRenderer is not assigned!", this);
            if (_mainCamera == null)
                Debug.LogError("[DebugRayVisualizer] Camera.main not found!", this);
        }

        #endregion
    }
}
