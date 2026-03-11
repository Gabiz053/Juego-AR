// ------------------------------------------------------------
// ------------------------------------------------------------
//  PerfectHarmonyPanel.cs  -  _Project.Scripts.UI
//  Controls the "Perfect Harmony" celebration overlay.
// ------------------------------------------------------------

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using _Project.Scripts.Core;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Shows a zen celebration overlay when the player reaches 100%
    /// harmony.  Auto-wires <see cref="CanvasGroup"/>, 
    /// <see cref="HarmonyParticles"/> and <see cref="UIAudioService"/>.<br/>
    /// <b>Important:</b> the <c>GameObject</c> must stay <b>active</b> in
    /// the scene � visibility is controlled via <see cref="CanvasGroup"/>
    /// alpha, not <c>SetActive</c>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("ARmonia/UI/Perfect Harmony Panel")]
    public class PerfectHarmonyPanel : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Required")]
        [Tooltip("HarmonyService -- subscribes to OnPerfectHarmony.")]
        [SerializeField] private HarmonyService _harmonyService;

        [Tooltip("Continue button -- shown after the intro animation.")]
        [SerializeField] private Button _continueButton;

        [Header("Optional Labels")]
        [Tooltip("Main title label.")]
        [SerializeField] private TMP_Text _titleLabel;

        [Header("Animation")]
        [Tooltip("Seconds for the panel to fade in.")]
        [SerializeField] private float _fadeInDuration = 0.6f;

        [Tooltip("Seconds for the panel to fade out after Continue.")]
        [SerializeField] private float _fadeOutDuration = 0.35f;

        #endregion

        #region State ---------------------------------------------

        private CanvasGroup      _canvasGroup;
        private HarmonyParticles _particles;
        private UIAudioService   _uiAudio;
        private HapticService    _hapticService;

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _particles = GetComponentInChildren<HarmonyParticles>(includeInactive: true);

            Canvas root = GetComponentInParent<Canvas>();
            if (root != null)
                _uiAudio = root.GetComponentInChildren<UIAudioService>();

            _hapticService = FindAnyObjectByType<HapticService>();

            // Start invisible � DO NOT use SetActive(false) or events
            // from HarmonyService will never reach this panel.
            _canvasGroup.alpha          = 0f;
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;

            if (_continueButton != null)
                _continueButton.gameObject.SetActive(false);

            Debug.Log("[PerfectHarmonyPanel] Awake complete -- invisible, waiting for perfect harmony.");
        }

        private void OnEnable()
        {
            if (_harmonyService != null)
            {
                _harmonyService.OnPerfectHarmony += HandlePerfectHarmony;
                _harmonyService.OnWorldReset     += HandleWorldReset;
            }
        }

        private void OnDisable()
        {
            if (_harmonyService != null)
            {
                _harmonyService.OnPerfectHarmony -= HandlePerfectHarmony;
                _harmonyService.OnWorldReset     -= HandleWorldReset;
            }
        }

        private void Start()
        {
            // Safety: if the GameObject was disabled in the editor by mistake,
            // warn the developer loudly so it's obvious why the panel fails.
            if (!gameObject.activeInHierarchy)
                Debug.LogError("[PerfectHarmonyPanel] GameObject is DISABLED -- panel will never show! " +
                               "Enable it and use CanvasGroup alpha=0 for invisibility.", this);

            ValidateReferences();
        }

        #endregion

        #region Public API ----------------------------------------

        /// <summary>Called by Btn_Continue.onClick.</summary>
        public void OnContinuePressed()
        {
            StartCoroutine(FadeOut());
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>Triggered by <see cref="HarmonyService.OnPerfectHarmony"/>.</summary>
        private void HandlePerfectHarmony()
        {
            Debug.Log("[PerfectHarmonyPanel] Perfect harmony reached -- showing panel.");
            StartCoroutine(ShowSequence());
        }

        /// <summary>Resets the panel to invisible on world reset.</summary>
        private void HandleWorldReset()
        {
            _particles?.StopAmbient();
            StopAllCoroutines();

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha          = 0f;
                _canvasGroup.interactable   = false;
                _canvasGroup.blocksRaycasts = false;
            }

            if (_continueButton != null)
                _continueButton.gameObject.SetActive(false);

            Debug.Log("[PerfectHarmonyPanel] Reset -- hidden.");
        }

        /// <summary>
        /// Fade-in sequence: shows button, fades in with SmoothStep,
        /// then plays particles, audio and haptic celebration.
        /// </summary>
        private IEnumerator ShowSequence()
        {
            if (_continueButton != null)
            {
                _continueButton.gameObject.SetActive(true);
                CanvasGroup btnCg = _continueButton.GetComponent<CanvasGroup>();
                if (btnCg == null) btnCg = _continueButton.gameObject.AddComponent<CanvasGroup>();
                btnCg.alpha        = 0f;
                btnCg.interactable = false;
            }

            _canvasGroup.blocksRaycasts = true;

            float elapsed = 0f;
            CanvasGroup buttonGroup = _continueButton != null
                ? _continueButton.GetComponent<CanvasGroup>()
                : null;

            while (elapsed < _fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t  = Mathf.SmoothStep(0f, 1f, elapsed / _fadeInDuration);
                _canvasGroup.alpha = t;
                if (buttonGroup != null) buttonGroup.alpha = t;
                yield return null;
            }

            _canvasGroup.alpha        = 1f;
            _canvasGroup.interactable = true;
            if (buttonGroup != null)
            {
                buttonGroup.alpha        = 1f;
                buttonGroup.interactable = true;
            }

            _particles?.Play();
            _uiAudio?.PlayConfirm();
            _hapticService?.VibrateHeavy();
        }

        /// <summary>
        /// Fade-out sequence triggered by Btn_Continue.  Hides the panel
        /// via <see cref="CanvasGroup"/> alpha.
        /// </summary>
        private IEnumerator FadeOut()
        {
            _uiAudio?.PlayClick();
            _canvasGroup.interactable = false;

            CanvasGroup buttonGroup = _continueButton != null
                ? _continueButton.GetComponent<CanvasGroup>()
                : null;
            if (buttonGroup != null) buttonGroup.interactable = false;

            float elapsed = 0f;
            while (elapsed < _fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t  = 1f - Mathf.SmoothStep(0f, 1f, elapsed / _fadeOutDuration);
                _canvasGroup.alpha = t;
                if (buttonGroup != null) buttonGroup.alpha = t;
                yield return null;
            }

            _canvasGroup.alpha          = 0f;
            _canvasGroup.blocksRaycasts = false;

            if (_continueButton != null)
                _continueButton.gameObject.SetActive(false);
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_harmonyService == null)
                Debug.LogError("[PerfectHarmonyPanel] _harmonyService is not assigned!", this);
            if (_continueButton == null)
                Debug.LogWarning("[PerfectHarmonyPanel] _continueButton is not assigned!", this);
            if (_canvasGroup == null)
                Debug.LogError("[PerfectHarmonyPanel] CanvasGroup not found!", this);
        }

        #endregion
    }
}
