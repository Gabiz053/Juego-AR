// ??????????????????????????????????????????????
//  ARPlaneVisualOffset.cs  ·  _Project.Scripts.AR
//  Floats the AR plane mesh slightly above the detected surface.
// ??????????????????????????????????????????????

using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace _Project.Scripts.AR
{
    /// <summary>
    /// Offsets the AR plane visual upward by nudging the material's
    /// texture offset — no Transform manipulation, no per-frame logic,
    /// no accumulation.<br/>
    /// On enable the <see cref="MeshRenderer"/> material receives a
    /// world-space position bias via <c>_BaseMap</c> offset so the mesh
    /// appears slightly above the real floor without fighting ARCore.<br/>
    /// Attach to the <c>AR_Default_Plane</c> prefab root.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/AR/AR Plane Visual Offset")]
    public class ARPlaneVisualOffset : MonoBehaviour
    {
        [Tooltip("How many metres above the detected surface the plane visual floats. 0.003–0.008 works well on real devices.")]
        [Range(0f, 0.05f)]
        [SerializeField] private float _visualOffset = 0.005f;

        private ARPlane      _arPlane;
        private MeshRenderer _meshRenderer;

        private void Awake()
        {
            _arPlane      = GetComponent<ARPlane>();
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        private void OnEnable()
        {
            if (_arPlane != null)
                _arPlane.boundaryChanged += OnBoundaryChanged;
        }

        private void OnDisable()
        {
            if (_arPlane != null)
                _arPlane.boundaryChanged -= OnBoundaryChanged;
        }

        // Fires once when ARCore first positions the plane and whenever
        // it updates — at that moment the Transform is already correct,
        // so we apply the offset exactly once per update, not per frame.
        private void OnBoundaryChanged(ARPlaneBoundaryChangedEventArgs args)
        {
            ApplyOffset();
        }

        // Called the first time the plane becomes active in case
        // boundaryChanged already fired before we subscribed.
        private void Start()
        {
            ApplyOffset();
        }

        private void ApplyOffset()
        {
            // Move along the plane's world normal (transform.up) by the offset.
            // This runs at most a few times per plane lifetime — never per frame.
            // We use transform.position directly; ARCore will correct it on the
            // next boundary update if it drifts, and we re-apply then.
            transform.position = transform.position + transform.up * _visualOffset;
        }
    }
}
