// ------------------------------------------------------------
//  BlockSpawn.cs  -  _Project.Scripts.Voxel
//  Fly-in + scale-up animation played once when a block is
//  placed.  Works in local space of the WorldContainer.
// ------------------------------------------------------------

using System;
using System.Collections;
using UnityEngine;

namespace _Project.Scripts.Voxel
{
    /// <summary>
    /// One-shot spawn animation: fly from camera to grid position with
    /// an overshoot settle.  Attach to each block prefab.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Voxel/Block Spawn")]
    public class BlockSpawn : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Timing")]
        [Tooltip("Total animation duration (seconds).")]
        [SerializeField] private float _duration = 0.18f;

        [Header("Scale")]
        [Tooltip("Overshoot scale factor at the peak before settling to 1.")]
        [SerializeField] private float _peakScale = 1.15f;

        [Header("Origin Offset")]
        [Tooltip("Offset in camera-local space for the animation start point.")]
        [SerializeField] private Vector3 _cameraLocalOffset = new Vector3(0f, -0.12f, 0.15f);

        #endregion

        #region Public API ----------------------------------------

        /// <summary>
        /// Kicks off the spawn animation.  <paramref name="onComplete"/> is
        /// invoked when the block has settled (releases the pending cell).
        /// </summary>
        public void Play(Transform cameraTransform, Action onComplete = null)
        {
            StartCoroutine(SpawnCoroutine(cameraTransform, onComplete));
        }

        #endregion

        #region Internals -----------------------------------------

        private IEnumerator SpawnCoroutine(Transform cameraTransform, Action onComplete)
        {
            BlockDestroy blockDestroy = GetComponent<BlockDestroy>();
            if (blockDestroy != null) blockDestroy.enabled = false;

            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Vector3 finalLocal = transform.localPosition;

            Vector3 startWorld = cameraTransform != null
                ? cameraTransform.TransformPoint(_cameraLocalOffset)
                : transform.position;
            Vector3 startLocal = transform.parent != null
                ? transform.parent.InverseTransformPoint(startWorld)
                : startWorld;

            // Phase 1 (80%): fly in + scale 0 -> peakScale
            float phase1  = _duration * 0.80f;
            float elapsed = 0f;

            while (elapsed < phase1)
            {
                elapsed += Time.deltaTime;
                float t     = Mathf.Clamp01(elapsed / phase1);
                float eased = 1f - Mathf.Pow(1f - t, 3f);

                transform.localPosition = Vector3.LerpUnclamped(startLocal, finalLocal, eased);
                transform.localScale    = Vector3.one * Mathf.LerpUnclamped(0f, _peakScale, eased);
                yield return null;
            }

            transform.localPosition = finalLocal;
            transform.localScale    = Vector3.one * _peakScale;

            // Phase 2 (20%): settle peakScale -> 1
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

            if (col != null) col.enabled = true;

            if (blockDestroy != null)
            {
                blockDestroy.enabled = true;
                blockDestroy.SetReady();
            }

            onComplete?.Invoke();
            enabled = false;
        }

        #endregion
    }
}
