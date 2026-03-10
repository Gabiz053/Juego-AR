// ??????????????????????????????????????????????
//  HarmonyParticles.cs  ·  _Project.Scripts.UI
//  3D world-space particle celebration effect for perfect harmony.
//  No sprites required — generates everything procedurally.
// ??????????????????????????????????????????????

using System.Collections;
using UnityEngine;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Drives a world-space <see cref="ParticleSystem"/> burst when
    /// <see cref="Play"/> is called.<br/>
    /// <br/>
    /// Requires a <see cref="ParticleSystem"/> on the same GameObject.<br/>
    /// All particle appearance is configured through the ParticleSystem
    /// component in the Inspector — no sprite asset needed.<br/>
    /// The system is positioned in front of the AR camera each time
    /// it plays so it always appears in view.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ParticleSystem))]
    [AddComponentMenu("ARmonia/UI/Harmony Particles")]
    public class HarmonyParticles : MonoBehaviour
    {
        #region Inspector ?????????????????????????????????????

        [Header("Burst — Celebration")]
        [Tooltip("How many particles to emit per celebration burst.")]
        [SerializeField] private int _burstCount = 120;

        [Tooltip("Metres in front of the AR camera where the effect spawns.")]
        [SerializeField] private float _distanceFromCamera = 1.2f;

        [Tooltip("How many seconds between repeated bursts within one celebration.")]
        [SerializeField] private float _repeatInterval = 1.1f;

        [Tooltip("How many times to repeat the burst during celebration.")]
        [SerializeField] private int _repeatCount = 3;

        [Header("Ambient — After Perfect")]
        [Tooltip("Particles emitted continuously once perfect harmony is reached.")]
        [SerializeField] private int _ambientEmitPerSecond = 5;

        [Tooltip("Metres radius around the camera where ambient particles spawn.")]
        [SerializeField] private float _ambientRadius = 0.7f;

        #endregion

        #region State ?????????????????????????????????????????

        private ParticleSystem _ps;
        private Camera         _mainCamera;
        private bool           _ambientRunning;
        private Coroutine      _ambientCoroutine;

        #endregion

        #region Unity Lifecycle ????????????????????????????????

        private void Awake()
        {
            _ps         = GetComponent<ParticleSystem>();
            _mainCamera = Camera.main;

            ConfigureParticleSystem();
        }

        #endregion

        #region Public API ????????????????????????????????????

        /// <summary>Trigger the full celebration burst sequence.</summary>
        public void Play()
        {
            StartCoroutine(BurstSequence());
            StartAmbient();
        }

        /// <summary>Starts the continuous subtle ambient particle loop.</summary>
        public void StartAmbient()
        {
            if (_ambientRunning) return;
            _ambientRunning   = true;
            _ambientCoroutine = StartCoroutine(AmbientLoop());
        }

        /// <summary>Stops the ambient loop (e.g. on world reset).</summary>
        public void StopAmbient()
        {
            _ambientRunning = false;
            if (_ambientCoroutine != null)
            {
                StopCoroutine(_ambientCoroutine);
                _ambientCoroutine = null;
            }
        }

        #endregion

        #region Internals ?????????????????????????????????????

        private IEnumerator BurstSequence()
        {
            for (int i = 0; i < _repeatCount; i++)
            {
                PositionInFrontOfCamera();
                _ps.Emit(_burstCount);

                if (i < _repeatCount - 1)
                    yield return new WaitForSeconds(_repeatInterval);
            }
        }

        private IEnumerator AmbientLoop()
        {
            float interval = _ambientEmitPerSecond > 0 ? 1f / _ambientEmitPerSecond : 0.5f;

            while (_ambientRunning)
            {
                if (_mainCamera != null)
                {
                    // Scatter randomly around the camera at a set radius.
                    Vector3 offset = Random.insideUnitSphere * _ambientRadius;
                    offset.y = Mathf.Abs(offset.y);   // always above camera plane

                    var emitParams = new ParticleSystem.EmitParams();
                    emitParams.position = _mainCamera.transform.position
                                        + _mainCamera.transform.forward * _distanceFromCamera
                                        + offset;
                    emitParams.applyShapeToPosition = false;

                    // Emit with reduced size for the subtle ambient effect.
                    emitParams.startSize = Random.Range(0.004f, 0.010f);
                    _ps.Emit(emitParams, 1);
                }

                yield return new WaitForSeconds(interval);
            }
        }

        private void PositionInFrontOfCamera()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            Transform cam = _mainCamera.transform;
            transform.position = cam.position + cam.forward * _distanceFromCamera;
            transform.rotation = cam.rotation;
        }

        /// <summary>
        /// Configures the ParticleSystem entirely by code so no manual
        /// Inspector setup is needed — it works out of the box.
        /// </summary>
        private void ConfigureParticleSystem()
        {
            if (_ps == null) return;

            // ?? Main module ????????????????????????????????
            var main                  = _ps.main;
            main.loop                 = false;
            main.playOnAwake          = false;
            main.maxParticles         = 500;
            main.startLifetime        = new ParticleSystem.MinMaxCurve(2.8f, 4.5f);
            main.startSpeed           = new ParticleSystem.MinMaxCurve(0.10f, 0.45f);
            main.startSize            = new ParticleSystem.MinMaxCurve(0.012f, 0.032f);
            main.gravityModifier      = new ParticleSystem.MinMaxCurve(-0.035f, -0.008f);
            main.simulationSpace      = ParticleSystemSimulationSpace.World;

            // Zen colour gradient: gold ? jade ? white ? lavender
            var gradient              = new ParticleSystem.MinMaxGradient();
            gradient.mode             = ParticleSystemGradientMode.RandomColor;
            Gradient g                = new Gradient();
            g.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(1.00f, 0.88f, 0.30f), 0.00f),   // gold
                    new GradientColorKey(new Color(0.40f, 0.90f, 0.55f), 0.33f),   // jade
                    new GradientColorKey(new Color(0.78f, 0.78f, 1.00f), 0.66f),   // lavender
                    new GradientColorKey(new Color(1.00f, 1.00f, 1.00f), 1.00f),   // white
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f),
                });
            gradient.gradient         = g;
            main.startColor           = gradient;

            // ?? Emission — manual bursts only ??????????????
            var emission              = _ps.emission;
            emission.enabled          = false;   // we call Emit() manually

            // ?? Shape — hemisphere pointing up ?????????????
            var shape                 = _ps.shape;
            shape.enabled             = true;
            shape.shapeType           = ParticleSystemShapeType.Hemisphere;
            shape.radius              = 0.08f;
            shape.rotation            = new Vector3(-90f, 0f, 0f);   // open upward

            // ?? Velocity over lifetime — gentle swirl ??????
            // All orbital axes MUST share the same MinMaxCurve mode.
            // Use Constant (single value) for all three so Unity doesn't complain.
            var vol   = _ps.velocityOverLifetime;
            vol.enabled = true;
            vol.space   = ParticleSystemSimulationSpace.Local;
            // Randomise direction per-particle via x/z linear velocity instead of orbital.
            vol.x = new ParticleSystem.MinMaxCurve(0f);
            vol.y = new ParticleSystem.MinMaxCurve(0.15f);   // gentle upward drift
            vol.z = new ParticleSystem.MinMaxCurve(0f);
            // Orbital — all same mode: Constant zero (swirl disabled to avoid mode mismatch).
            vol.orbitalX = new ParticleSystem.MinMaxCurve(0f);
            vol.orbitalY = new ParticleSystem.MinMaxCurve(0f);
            vol.orbitalZ = new ParticleSystem.MinMaxCurve(0f);

            // ?? Size over lifetime — shrink at end ?????????
            var sol                   = _ps.sizeOverLifetime;
            sol.enabled               = true;
            AnimationCurve sizeCurve  = new AnimationCurve();
            sizeCurve.AddKey(0f,  1f);
            sizeCurve.AddKey(0.7f, 1f);
            sizeCurve.AddKey(1f,  0f);
            sol.size                  = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // ?? Colour over lifetime — fade out at end ?????
            var col                   = _ps.colorOverLifetime;
            col.enabled               = true;
            Gradient fadeOut          = new Gradient();
            fadeOut.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            col.color                 = new ParticleSystem.MinMaxGradient(fadeOut);

            // ?? Renderer — use default sphere mesh, URP unlit ?
            var renderer              = _ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode       = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder     = 10;

            // Assign URP unlit particle material if available, otherwise create one.
            Material mat = Resources.Load<Material>("Particles/Default");
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                             ?? Shader.Find("Particles/Standard Unlit")
                             ?? Shader.Find("Sprites/Default");
                if (shader != null)
                    mat = new Material(shader);
            }
            if (mat != null)
                renderer.material = mat;
        }

        #endregion
    }
}
