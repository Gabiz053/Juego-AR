// ------------------------------------------------------------
//  VFXBlockDestroy.cs  -  _Project.Scripts.Voxel
//  Particle dust burst spawned when a block is destroyed.
// ------------------------------------------------------------

using UnityEngine;

namespace _Project.Scripts.Voxel
{
    /// <summary>
    /// One-shot destruction VFX.  Configures particle modules in code
    /// and self-destructs after <see cref="_lifetime"/> seconds.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Voxel/VFX Block Destroy")]
    public class VFXBlockDestroy : MonoBehaviour
    {
        #region Constants -----------------------------------------

        /// <summary>Built-in Unity cube mesh resource path used for debris particles.</summary>
        private const string BUILTIN_CUBE_MESH = "Cube.fbx";

        #endregion

        #region Inspector -----------------------------------------

        [Header("Particles")]
        [Tooltip("Particle System child -- dust and debris burst.")]
        [SerializeField] private ParticleSystem _particles;

        [Tooltip("Seconds before self-destruction.")]
        [SerializeField] private float _lifetime = 1.0f;

        [Tooltip("Colour of the dust -- match the block destroyed.")]
        [SerializeField] private Color _colour = new Color(0.72f, 0.65f, 0.45f, 1f);

        #endregion

        #region State ---------------------------------------------

        private ParticleSystemRenderer _renderer;

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            if (_particles != null)
                _renderer = _particles.GetComponent<ParticleSystemRenderer>();
        }

        private void Start()
        {
            ValidateReferences();

            if (_particles != null)
                _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            Configure();

            if (_particles != null)
                _particles.Play();

            Destroy(gameObject, _lifetime);
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Configures particle system modules (emission burst, shape,
        /// rotation-over-lifetime, size/colour fading) for a destruction puff.
        /// Uses cube mesh via <c>Resources.GetBuiltinResource</c>.
        /// </summary>
        private void Configure()
        {
            if (_particles == null) return;

            // Main
            var m             = _particles.main;
            m.loop            = false;
            m.playOnAwake     = false;
            m.duration        = 0.1f;
            m.startLifetime   = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            m.startSpeed      = new ParticleSystem.MinMaxCurve(0.5f, 1.8f);
            m.startSize       = new ParticleSystem.MinMaxCurve(0.008f, 0.020f);
            m.startRotation   = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            m.startColor      = new ParticleSystem.MinMaxGradient(
                _colour,
                new Color(_colour.r * 0.6f, _colour.g * 0.6f, _colour.b * 0.55f, 1f));
            m.gravityModifier = new ParticleSystem.MinMaxCurve(2.0f);
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.maxParticles    = 20;

            // Emission
            var e = _particles.emission;
            e.rateOverTime = 0;
            e.SetBursts(new[] { new ParticleSystem.Burst(0f, 10, 16) });

            // Shape
            var s       = _particles.shape;
            s.enabled   = true;
            s.shapeType = ParticleSystemShapeType.Sphere;
            s.radius    = 0.04f;

            // Rotation over lifetime
            var rot     = _particles.rotationOverLifetime;
            rot.enabled = true;
            rot.z       = new ParticleSystem.MinMaxCurve(-120f * Mathf.Deg2Rad, 120f * Mathf.Deg2Rad);

            // Size over lifetime
            var sol     = _particles.sizeOverLifetime;
            sol.enabled = true;
            sol.size    = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(
                    new Keyframe(0f,   1f,  0f, 0f),
                    new Keyframe(0.6f, 0.7f),
                    new Keyframe(1f,   0f, -2f, 0f)));

            // Colour over lifetime
            var col     = _particles.colorOverLifetime;
            col.enabled = true;
            var grad    = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f),
                        new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(1f, 0.5f),
                        new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);

            // Renderer
            if (_renderer != null)
            {
                _renderer.renderMode = ParticleSystemRenderMode.Mesh;
                _renderer.mesh       = Resources.GetBuiltinResource<Mesh>(BUILTIN_CUBE_MESH);
            }
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_particles == null)
                Debug.LogWarning("[VFXBlockDestroy] _particles is not assigned.", this);
        }

        #endregion
    }
}
