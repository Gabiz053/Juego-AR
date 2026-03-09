// ??????????????????????????????????????????????
//  BlockSpawn.cs  ·  _Project.Scripts.Voxel
//  Fly-in + scale-up animation played once when a block is placed.
//  Works entirely in LOCAL space of the WorldContainer so the animation
//  is immune to parent scale / movement changes.
//  Attach directly to each block prefab in the Inspector.
// ??????????????????????????????????????????????

using System;
using System.Collections;
using UnityEngine;

namespace _Project.Scripts.Voxel
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Voxel/Block Spawn")]
    public class BlockSpawn : MonoBehaviour
    {
        #region Inspector ?????????????????????????????????????

        [Header("Timing")]
        [Tooltip("Total duration of the fly-in + scale-up animation (seconds).")]
        [SerializeField] private float _duration = 0.18f;

        [Header("Scale")]
        [Tooltip("Overshoot scale factor at the peak before settling to 1.")]
        [SerializeField] private float _peakScale = 1.15f;

        [Header("Origin Offset")]
        [Tooltip("Offset in camera-local space for the animation start point.\nKeep Z small so it doesn't obscure the view.")]
        [SerializeField] private Vector3 _cameraLocalOffset = new Vector3(0f, -0.12f, 0.15f);

        #endregion

        #region Public API ????????????????????????????????????

        /// <summary>
        /// Kicks off the spawn animation.<br/>
        /// <paramref name="onComplete"/> is invoked when the block has fully settled —
        /// used by <see cref="Interaction.ARBlockPlacer"/> to release the reserved pending cell.
        /// </summary>
        public void Play(Transform cameraTransform, Action onComplete = null)
        {
            StartCoroutine(SpawnCoroutine(cameraTransform, onComplete));
        }

        #endregion

        #region Animation coroutine ???????????????????????????

        private IEnumerator SpawnCoroutine(Transform cameraTransform, Action onComplete)
        {
            // Suppress proximity-knockback during the entire flight.
            BlockDestroy blockDestroy = GetComponent<BlockDestroy>();
            if (blockDestroy != null) blockDestroy.enabled = false;

            // Disable collider during flight.
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // Capture the final LOCAL position once — immune to parent movement.
            Vector3 finalLocal = transform.localPosition;

            // Compute start in world space, then convert to local space immediately.
            Vector3 startWorld = cameraTransform != null
                ? cameraTransform.TransformPoint(_cameraLocalOffset)
                : transform.position;
            Vector3 startLocal = transform.parent != null
                ? transform.parent.InverseTransformPoint(startWorld)
                : startWorld;

            // ?? Phase 1 (80 %): fly in + scale 0 ? peakScale ??????????????????
            float phase1  = _duration * 0.80f;
            float elapsed = 0f;

            while (elapsed < phase1)
            {
                elapsed += Time.deltaTime;
                float t     = Mathf.Clamp01(elapsed / phase1);
                float eased = 1f - Mathf.Pow(1f - t, 3f);   // ease-out cubic

                transform.localPosition = Vector3.LerpUnclamped(startLocal, finalLocal, eased);
                transform.localScale    = Vector3.one * Mathf.LerpUnclamped(0f, _peakScale, eased);
                yield return null;
            }

            // Snap to exact destination before phase 2.
            transform.localPosition = finalLocal;
            transform.localScale    = Vector3.one * _peakScale;

            // ?? Phase 2 (20 %): settle peakScale ? 1 ??????????????????????????
            float phase2 = _duration * 0.20f;
            elapsed = 0f;

            while (elapsed < phase2)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / phase2);
                transform.localScale = Vector3.one * Mathf.Lerp(_peakScale, 1f, t * t);
                yield return null;
            }

            transform.localPosition = finalLocal;
            transform.localScale    = Vector3.one;

            // Re-enable collider once settled.
            if (col != null) col.enabled = true;

            // Block is now in its final position — allow proximity detection.
            if (blockDestroy != null)
            {
                blockDestroy.enabled = true;
                blockDestroy.SetReady();
            }

            // Notify ARBlockPlacer to release the reserved cell.
            onComplete?.Invoke();

            enabled = false;
        }

        #endregion
    }
}
