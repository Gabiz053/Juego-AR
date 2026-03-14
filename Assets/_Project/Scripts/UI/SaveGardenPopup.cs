// ------------------------------------------------------------
//  SaveGardenPopup.cs  -  _Project.Scripts.UI
//  Modal popup for naming and saving the current garden.
// ------------------------------------------------------------

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using _Project.Scripts.Infrastructure;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Full-screen modal that prompts the user for a garden name
    /// and delegates to <see cref="ISaveLoadService.SaveCurrentGarden"/>.
    /// The <c>GameObject</c> starts <b>disabled</b> in the scene and is
    /// activated by <see cref="Show"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("ARmonia/UI/Save Garden Popup")]
    public class SaveGardenPopup : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Animation")]
        [Tooltip("Seconds for the popup to fade in.")]
        [SerializeField] private float _fadeInDuration = 0.25f;

        [Tooltip("Seconds for the popup to fade out.")]
        [SerializeField] private float _fadeOutDuration = 0.20f;

        [Header("References")]
        [Tooltip("Input field where the user types the garden name.")]
        [SerializeField] private TMP_InputField _inputField;

        [Tooltip("Button to confirm saving.")]
        [SerializeField] private Button _saveButton;

        [Tooltip("Button to cancel and close the popup.")]
        [SerializeField] private Button _cancelButton;

        #endregion

        #region State ---------------------------------------------

        private CanvasGroup      _canvasGroup;
        private Coroutine        _activeRoutine;
        private bool             _initialized;
        private IUIAudioService  _uiAudio;
        private ISaveLoadService _saveLoad;

        #endregion

        #region Public API ----------------------------------------

        /// <summary>
        /// Activates the popup, clears the input field, and fades in.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            EnsureInitialized();

            if (_inputField != null)
            {
                _inputField.text = string.Empty;
                _inputField.ActivateInputField();
            }

            _canvasGroup.alpha          = 0f;
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;

            if (_activeRoutine != null)
                StopCoroutine(_activeRoutine);

            _activeRoutine = StartCoroutine(FadeIn());
            Debug.Log("[SaveGardenPopup] Popup shown.");
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Start()
        {
            ServiceLocator.TryGet<IUIAudioService>(out _uiAudio);
            ServiceLocator.TryGet<ISaveLoadService>(out _saveLoad);
            ValidateReferences();
        }

        private void OnDestroy()
        {
            if (_saveButton != null)
                _saveButton.onClick.RemoveListener(OnSavePressed);
            if (_cancelButton != null)
                _cancelButton.onClick.RemoveListener(OnCancelPressed);
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Lazy-initialises the CanvasGroup and button listeners.
        /// Called on first <see cref="Show"/> because the GameObject
        /// starts disabled and <c>Awake</c> would not run.
        /// </summary>
        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            ServiceLocator.TryGet<IUIAudioService>(out _uiAudio);
            ServiceLocator.TryGet<ISaveLoadService>(out _saveLoad);

            if (_saveButton != null)
                _saveButton.onClick.AddListener(OnSavePressed);
            if (_cancelButton != null)
                _cancelButton.onClick.AddListener(OnCancelPressed);

            Debug.Log("[SaveGardenPopup] Initialized.");
        }

        /// <summary>Validates the input and triggers the save operation.</summary>
        private void OnSavePressed()
        {
            string gardenName = _inputField != null ? _inputField.text.Trim() : string.Empty;

            if (string.IsNullOrEmpty(gardenName))
            {
                Debug.LogWarning("[SaveGardenPopup] Garden name is empty -- ignoring save.");
                return;
            }

            _saveLoad?.SaveCurrentGarden(gardenName);
            _uiAudio?.PlayConfirm();

            Debug.Log($"[SaveGardenPopup] Save requested for '{gardenName}'.");
            Close();
        }

        /// <summary>Cancels and closes the popup.</summary>
        private void OnCancelPressed()
        {
            _uiAudio?.PlayCancel();
            Close();
        }

        /// <summary>Fades out and deactivates the popup.</summary>
        private void Close()
        {
            if (_activeRoutine != null)
                StopCoroutine(_activeRoutine);

            _activeRoutine = StartCoroutine(FadeOut());
        }

        private IEnumerator FadeIn()
        {
            _canvasGroup.blocksRaycasts = true;

            float elapsed = 0f;
            while (elapsed < _fadeInDuration)
            {
                elapsed            += Time.deltaTime;
                _canvasGroup.alpha  = Mathf.SmoothStep(0f, 1f, elapsed / _fadeInDuration);
                yield return null;
            }

            _canvasGroup.alpha        = 1f;
            _canvasGroup.interactable = true;
            _activeRoutine            = null;
        }

        private IEnumerator FadeOut()
        {
            _canvasGroup.interactable = false;

            float elapsed = 0f;
            while (elapsed < _fadeOutDuration)
            {
                elapsed            += Time.deltaTime;
                _canvasGroup.alpha  = Mathf.SmoothStep(1f, 0f, elapsed / _fadeOutDuration);
                yield return null;
            }

            _canvasGroup.alpha          = 0f;
            _canvasGroup.blocksRaycasts = false;
            _activeRoutine              = null;
            gameObject.SetActive(false);
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_inputField == null)
                Debug.LogWarning("[SaveGardenPopup] _inputField is not assigned.", this);
            if (_saveButton == null)
                Debug.LogWarning("[SaveGardenPopup] _saveButton is not assigned.", this);
            if (_cancelButton == null)
                Debug.LogWarning("[SaveGardenPopup] _cancelButton is not assigned.", this);
            if (_saveLoad == null)
                Debug.LogWarning("[SaveGardenPopup] ISaveLoadService is not registered.", this);
        }

        #endregion
    }
}
