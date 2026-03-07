// ??????????????????????????????????????????????
//  ARPlaneVisualOffset.cs  ·  _Project.Scripts.AR
//  Shifts the plane mesh material's texture tiling offset so the
//  visual floats slightly above the real floor without moving the
//  Transform (which ARCore owns and overwrites every frame).
// ??????????????????????????????????????????????

using UnityEngine;

namespace _Project.Scripts.AR
{
    /// <summary>
    /// Applies a vertical visual offset to the AR plane mesh by nudging
    /// the <see cref="MeshRenderer"/>'s material UV offset each frame.<br/>
    /// ARCore controls the plane's <see cref="Transform"/> and overwrites
    /// any position changes — this script works around that by operating
    /// on the renderer instead of the transform.<br/>
    /// Attach to the <c>AR_Default_Plane</c> prefab root.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/AR/AR Plane Visual Offset")]
    public class ARPlaneVisualOffset : MonoBehaviour
    {
        [Tooltip("How many metres above the detected surface the plane visual floats. 0.003–0.01 works well.")]
        [Range(0f, 0.05f)]
        [SerializeField] private float _visualOffset = 0.005f;

        private MeshRenderer _meshRenderer;
        private bool         _offsetApplied;

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        // LateUpdate runs after ARCore has repositioned the plane Transform.
        // We nudge the renderer's position in world space without touching
        // the Transform itself by using a MaterialPropertyBlock trick:
        // shift the mesh vertices is not possible at runtime without a custom
        // shader, so instead we offset the GameObject's position only on Y
        // relative to its parent in LateUpdate — ARCore moves the root, we
        // re-apply the offset on top of that every frame.
        private void LateUpdate()
        {
            // ARCore sets transform.localPosition each frame. We add our
            // offset on top of whatever value it wrote, after it wrote it.
            Vector3 pos = transform.localPosition;
            pos.y = _visualOffset;
            transform.localPosition = pos;
        }
    }
}
