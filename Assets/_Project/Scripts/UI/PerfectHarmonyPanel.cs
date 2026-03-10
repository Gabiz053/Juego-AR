// ??????????????????????????????????????????????
//  PerfectHarmonyPanel.cs  ·  _Project.Scripts.UI
//  Controls the "Perfect Harmony" celebration overlay.
// ??????????????????????????????????????????????

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using _Project.Scripts.Core;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Shows a zen celebration overlay when the player reaches 100 % harmony.<br/>
    /// Subscribes to <see cref="HarmonyService.OnPerfectHarmony"/>.<br/>
    /// <br/>
    /// <b>Auto-wires itself:</b><br/>
    /// • <see cref="CanvasGroup"/> — found on the same GameObject.<br/>
    /// • <see cref="HarmonyParticles"/> — found anywhere in children.<br/>
    /// • <see cref="UIAudioService"/> — found on the root Canvas.<br/>
    /// Only <see cref="_harmonyService"/>, <see cref="_continueButton"/> and
    /// <see cref="_titleLabel"/> need to be assigned in the Inspector.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("ARmonia/UI/Perfect Harmony Panel")]
    public class PerfectHarmonyPanel : MonoBehaviour
    {
        #region Inspector ?????????????????????????????????????

        [Header("Required")]
        [Tooltip("HarmonyService — subscribes to OnPerfectHarmony.")]
        [SerializeField] private HarmonyService _harmonyService;

        [Tooltip("'Continuar' button — shown after the intro animation.")]
        [SerializeField] private Button _continueButton;

        [Header("Optional Labels")]
        [Tooltip("Main title label. Leave empty if not used.")]
        [SerializeField] private TMP_Text _titleLabel;

        [Header("Animation")]
        [Tooltip("Seconds for the panel to fade in.")]
        [SerializeField] private float _fadeInDuration  = 0.6f;

        [Tooltip("Seconds for the panel to fade out after Continue is pressed.")]
        [SerializeField] private float _fadeOutDuration = 0.35f;

        #endregion

        #region Auto-located refs ?????????????????????????????

        // All auto-found in Awake — no manual wiring needed.
        private CanvasGroup      _canvasGroup;
        private HarmonyParticles _particles;
        private UIAudioService   _uiAudio;

        #endregion

        #region Unity Lifecycle ????????????????????????????????

        private void Awake()
        {
            // ?? Auto-locate CanvasGroup on this GameObject ??
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // ?? Auto-locate HarmonyParticles in children ????
            _particles = GetComponentInChildren<HarmonyParticles>(includeInactive: true);

            // ?? Auto-locate UIAudioService on root Canvas ???
            Canvas root = GetComponentInParent<Canvas>();
            if (root != null)
                _uiAudio = root.GetComponentInChildren<UIAudioService>();

            // Start fully hidden.
            _canvasGroup.alpha          = 0f;
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;

            // Button starts hidden and inactive until ShowSequence runs.
            if (_continueButton != null)
                _continueButton.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (_harmonyService != null)
            {
                _harmonyService.OnPerfectHarmony += HandlePerfectHarmony;
                _harmonyService.OnWorldReset      += HandleWorldReset;
            }
        }

        private void OnDisable()
        {
            if (_harmonyService != null)
            {
                _harmonyService.OnPerfectHarmony -= HandlePerfectHarmony;
                _harmonyService.OnWorldReset      -= HandleWorldReset;
            }
        }

        #endregion

        #region Public API ????????????????????????????????????

        /// <summary>Called by <c>Btn_Continue.onClick</c>.</summary>
        public void OnContinuePressed()
        {
            StartCoroutine(FadeOut());
        }

        #endregion

        #region Internals ?????????????????????????????????????

        private void HandlePerfectHarmony() => StartCoroutine(ShowSequence());

        private void HandleWorldReset()
        {
            _particles?.StopAmbient();
            // Also hide the panel immediately if it was still showing.
            StopAllCoroutines();
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha          = 0f;
                _canvasGroup.interactable   = false;
                _canvasGroup.blocksRaycasts = false;
            }
            if (_continueButton != null)
                _continueButton.gameObject.SetActive(false);
        }

        private IEnumerator ShowSequence()
        {
            // Show the button hidden (alpha 0) so it fades in with the panel.
            if (_continueButton != null)
            {
                _continueButton.gameObject.SetActive(true);
                CanvasGroup btnCg = _continueButton.GetComponent<CanvasGroup>();
                if (btnCg == null) btnCg = _continueButton.gameObject.AddComponent<CanvasGroup>();
                btnCg.alpha        = 0f;
                btnCg.interactable = false;
            }

            _canvasGroup.blocksRaycasts = true;

            // Fade in panel + button together.
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
        }

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

        private IEnumerator Fade(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed            += Time.deltaTime;
                _canvasGroup.alpha  = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }
            _canvasGroup.alpha = to;
        }

        #endregion

        #region Validation ????????????????????????????????????

        private void OnValidate()
        {
            if (_harmonyService == null)
                Debug.LogWarning("[PerfectHarmonyPanel] Assign _harmonyService in the Inspector.", this);
            if (_continueButton == null)
                Debug.LogWarning("[PerfectHarmonyPanel] Assign _continueButton in the Inspector.", this);
        }

        #endregion
    }
}
