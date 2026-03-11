// ------------------------------------------------------------
//  VFXBlockPlace.cs  -  _Project.Scripts.Voxel
//  Particle burst + scale pop played when a block is placed.
// ------------------------------------------------------------

using System;
using System.Collections;
using UnityEngine;

namespace _Project.Scripts.Voxel
{
    /// <summary>
    /// One-shot VFX spawned at the placement position.
    /// Configures particle modules in code and self-destructs.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Voxel/VFX Block Place")]
    public class VFXBlockPlace : MonoBehaviour
    {
        #region Constants -----------------------------------------

        private const float POP_DURATION  = 0.06f;
        private const float EASE_DURATION = 0.14f;
        private const float PEAK_SCALE    = 1.18f;

        #endregion

        #region Inspector -----------------------------------------

        [Header("Particles")]
        [Tooltip("Particle System child -- tiny pixel debris burst.")]
        [SerializeField] private ParticleSystem _particles;

        [Tooltip("Seconds before self-destruction.")]
        [SerializeField] private float _lifetime = 0.8f;

        [Tooltip("Colour of the debris -- set to match the block placed.")]
        [SerializeField] private Color _colour = new Color(0.86f, 0.79f, 0.57f, 1f);

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Start()
        {
            if (_particles != null)
                _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            Configure();
            StartCoroutine(Play());
        }

        #endregion

        #region Internals -----------------------------------------

        private void Configure()
        {
            if (_particles == null) return;

            var m             = _particles.main;
            m.loop            = false;
            m.playOnAwake     = false;
            m.duration        = 0.1f;
            m.startLifetime   = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
            m.startSpeed      = new ParticleSystem.MinMaxCurve(0.8f, 2.0f);
            m.startSize       = new ParticleSystem.MinMaxCurve(0.015f, 0.030f);
            m.startColor      = new ParticleSystem.MinMaxGradient(
                _colour,
                new Color(_colour.r * 0.75f, _colour.g * 0.75f, _colour.b * 0.65f, 1f));
            m.gravityModifier = new ParticleSystem.MinMaxCurve(2.5f);
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.maxParticles    = 20;

            var e = _particles.emission;
            e.rateOverTime = 0;
            e.SetBursts(new[] { new ParticleSystem.Burst(0f, 10, 16) });

            var s       = _particles.shape;
            s.enabled   = true;
            s.shapeType = ParticleSystemShapeType.Sphere;
            s.radius    = 0.04f;

            var sol     = _particles.sizeOverLifetime;
            sol.enabled = true;
            sol.size    = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(
                    new Keyframe(0f, 1f,  0f, -2f),
                    new Keyframe(1f, 0f, -2f,  0f)));

            var col     = _particles.colorOverLifetime;
            col.enabled = true;
            var grad    = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f),
                        new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(1f, 0.6f),
                        new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);
        }

        private IEnumerator Play()
        {
            if (_particles != null) _particles.Play();

            yield return StartCoroutine(ScaleTo(Vector3.one * PEAK_SCALE, POP_DURATION,  t => 1f - (1f-t)*(1f-t)));
            yield return StartCoroutine(ScaleTo(Vector3.one,              EASE_DURATION, t => t * t));

            Destroy(gameObject, _lifetime);
        }

        private IEnumerator ScaleTo(Vector3 target, float dur, Func<float, float> ease)
        {
            Vector3 start = transform.localScale;
            float   t     = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.LerpUnclamped(start, target, ease(Mathf.Clamp01(t / dur)));
                yield return null;
            }
            transform.localScale = target;
        }

        #endregion

        #region Validation ----------------------------------------

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_particles == null)
                Debug.LogWarning("[VFXBlockPlace] _particles not assigned.", this);
        }
#endif

        #endregion
    }
}
