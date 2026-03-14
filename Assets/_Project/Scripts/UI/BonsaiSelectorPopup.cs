// ------------------------------------------------------------
//  BonsaiSelectorPopup.cs  -  _Project.Scripts.UI
//  Modal popup for selecting a saved garden in Bonsai mode.
// ------------------------------------------------------------

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using _Project.Scripts.Core;
using _Project.Scripts.Infrastructure;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Full-screen modal displayed upon image detection in Bonsai mode.
    /// Lists all saved gardens with a load button and a delete button
    /// per entry.  If none exist, shows an empty-state message with a
    /// "back to menu" button.  The <c>GameObject</c> starts
    /// <b>disabled</b> in the scene and is activated by
    /// <see cref="Show"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("ARmonia/UI/Bonsai Selector Popup")]
    public class BonsaiSelectorPopup : MonoBehaviour
    {
        #region Constants -----------------------------------------

        /// <summary>Preferred height in pixels for each garden list row.</summary>
        private const float ROW_HEIGHT = 120f;

        /// <summary>Fixed width in pixels for the delete button.</summary>
        private const float DELETE_BUTTON_WIDTH = 120f;

        /// <summary>Font size for garden name labels.</summary>
        private const float LABEL_FONT_SIZE = 42f;

        /// <summary>Font size for the delete button text.</summary>
        private const float DELETE_FONT_SIZE = 36f;

        /// <summary>Spacing between load and delete buttons inside a row.</summary>
        private const float ROW_SPACING = 10f;

        #endregion

        #region Inspector -----------------------------------------

        [Header("Animation")]
        [Tooltip("Seconds for the popup to fade in.")]
        [SerializeField] private float _fadeInDuration = 0.25f;

        [Tooltip("Seconds for the popup to fade out.")]
        [SerializeField] private float _fadeOutDuration = 0.20f;

        [Header("List Mode")]
        [Tooltip("Parent transform for dynamically spawned garden rows.")]
        [SerializeField] private Transform _listContent;

        [Tooltip("Button prefab instantiated for the garden name (load action).")]
        [SerializeField] private GameObject _listItemPrefab;

        [Header("Empty Mode")]
        [Tooltip("Root GameObject shown when no saved gardens exist.")]
        [SerializeField] private GameObject _emptyStateRoot;

        [Tooltip("Button that returns to the title screen (empty state).")]
        [SerializeField] private Button _btnBackToMenu;

        [Header("Common")]
        [Tooltip("Button to close the popup without action.")]
        [SerializeField] private Button _btnClose;

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
        /// Queries saved gardens, builds the appropriate UI state
        /// (list or empty), and fades in.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            EnsureInitialized();

            ClearListItems();

            string[] gardens = _saveLoad?.GetSavedGardensList();
            bool hasGardens  = gardens != null && gardens.Length > 0;

            if (_listContent != null)
                _listContent.gameObject.SetActive(hasGardens);
            if (_emptyStateRoot != null)
                _emptyStateRoot.SetActive(!hasGardens);

            if (hasGardens)
                PopulateList(gardens);

            _canvasGroup.alpha          = 0f;
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;

            if (_activeRoutine != null)
                StopCoroutine(_activeRoutine);

            _activeRoutine = StartCoroutine(FadeIn());
            Debug.Log($"[BonsaiSelectorPopup] Popup shown -- {(hasGardens ? gardens.Length + " garden(s)" : "empty state")}.");
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
            if (_btnBackToMenu != null)
                _btnBackToMenu.onClick.RemoveListener(OnBackToMenuPressed);
            if (_btnClose != null)
                _btnClose.onClick.RemoveListener(OnClosePressed);
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Lazy-initialises CanvasGroup and button listeners.
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

            if (_btnBackToMenu != null)
                _btnBackToMenu.onClick.AddListener(OnBackToMenuPressed);
            if (_btnClose != null)
                _btnClose.onClick.AddListener(OnClosePressed);

            Debug.Log("[BonsaiSelectorPopup] Initialized.");
        }

        /// <summary>
        /// Spawns a fully-programmatic row per saved garden: a wide load
        /// button covering the row background and a small delete button
        /// anchored to the right.  No dependency on the list-item prefab
        /// layout -- all sizing is done via anchors.
        /// </summary>
        private void PopulateList(string[] gardenNames)
        {
            if (_listContent == null) return;

            // -- Ensure the _listContent stretches to fill the viewport --
            RectTransform contentRect = _listContent.GetComponent<RectTransform>();
            if (contentRect != null)
            {
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot     = new Vector2(0.5f, 1f);
                contentRect.offsetMin = new Vector2(0f, contentRect.offsetMin.y);
                contentRect.offsetMax = new Vector2(0f, contentRect.offsetMax.y);
            }

            // Guarantee a VerticalLayoutGroup with correct settings.
            VerticalLayoutGroup vlg = _listContent.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
                vlg = _listContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth      = true;
            vlg.childForceExpandWidth  = true;
            vlg.childControlHeight     = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing                = ROW_SPACING;

            // Guarantee a ContentSizeFitter: unconstrained width, grow height.
            ContentSizeFitter csf = _listContent.GetComponent<ContentSizeFitter>();
            if (csf == null)
                csf = _listContent.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            foreach (string gardenName in gardenNames)
            {
                // -- Row root: VLG forces its width; no layout group needed inside --
                GameObject row = new GameObject("Row_" + gardenName, typeof(RectTransform));
                row.transform.SetParent(_listContent, false);

                LayoutElement rowLayout = row.AddComponent<LayoutElement>();
                rowLayout.preferredHeight = ROW_HEIGHT;

                // -- Load button: stretches full row except the delete zone --
                GameObject loadGo = new GameObject("Btn_Load", typeof(RectTransform));
                loadGo.transform.SetParent(row.transform, false);

                Image loadImage = loadGo.AddComponent<Image>();
                loadImage.color = new Color(0.22f, 0.22f, 0.22f, 1f);

                Button loadBtn = loadGo.AddComponent<Button>();
                ColorBlock loadCb   = loadBtn.colors;
                loadCb.highlightedColor = Color.white;
                loadCb.pressedColor     = new Color(0.35f, 0.35f, 0.35f, 1f);
                loadBtn.colors          = loadCb;
                loadBtn.targetGraphic   = loadImage;

                // Anchor: left edge to right edge minus delete width.
                RectTransform loadRect = loadGo.GetComponent<RectTransform>();
                loadRect.anchorMin = Vector2.zero;
                loadRect.anchorMax = new Vector2(1f, 1f);
                loadRect.offsetMin = Vector2.zero;
                loadRect.offsetMax = new Vector2(-(DELETE_BUTTON_WIDTH + ROW_SPACING), 0f);

                // Label inside load button.
                GameObject loadTextGo = new GameObject("Txt_Name", typeof(RectTransform));
                loadTextGo.transform.SetParent(loadGo.transform, false);

                TextMeshProUGUI loadLabel = loadTextGo.AddComponent<TextMeshProUGUI>();
                loadLabel.text      = gardenName;
                loadLabel.fontSize  = LABEL_FONT_SIZE;
                loadLabel.alignment = TextAlignmentOptions.MidlineLeft;
                loadLabel.color     = Color.white;
                loadLabel.margin    = new Vector4(20f, 0f, 10f, 0f);

                RectTransform loadTextRect = loadTextGo.GetComponent<RectTransform>();
                loadTextRect.anchorMin = Vector2.zero;
                loadTextRect.anchorMax = Vector2.one;
                loadTextRect.offsetMin = Vector2.zero;
                loadTextRect.offsetMax = Vector2.zero;

                string capturedLoad = gardenName;
                loadBtn.onClick.AddListener(() => OnGardenSelected(capturedLoad));

                // -- Delete button: fixed width, anchored to the right --
                GameObject delGo = new GameObject("Btn_Delete", typeof(RectTransform));
                delGo.transform.SetParent(row.transform, false);

                Image delImage = delGo.AddComponent<Image>();
                delImage.color = new Color(0.7f, 0.2f, 0.2f, 1f);

                Button delBtn = delGo.AddComponent<Button>();
                ColorBlock delCb    = delBtn.colors;
                delCb.highlightedColor = Color.white;
                delCb.pressedColor     = new Color(0.5f, 0.1f, 0.1f, 1f);
                delBtn.colors          = delCb;
                delBtn.targetGraphic   = delImage;

                // Anchor: right edge, fixed width.
                RectTransform delRect = delGo.GetComponent<RectTransform>();
                delRect.anchorMin = new Vector2(1f, 0f);
                delRect.anchorMax = new Vector2(1f, 1f);
                delRect.pivot     = new Vector2(1f, 0.5f);
                delRect.sizeDelta = new Vector2(DELETE_BUTTON_WIDTH, 0f);

                // "X" label inside delete button.
                GameObject delTextGo = new GameObject("Txt_Delete", typeof(RectTransform));
                delTextGo.transform.SetParent(delGo.transform, false);

                TextMeshProUGUI delText = delTextGo.AddComponent<TextMeshProUGUI>();
                delText.text      = "X";
                delText.fontSize  = DELETE_FONT_SIZE;
                delText.alignment = TextAlignmentOptions.Center;
                delText.color     = Color.white;

                RectTransform delTextRect = delTextGo.GetComponent<RectTransform>();
                delTextRect.anchorMin = Vector2.zero;
                delTextRect.anchorMax = Vector2.one;
                delTextRect.offsetMin = Vector2.zero;
                delTextRect.offsetMax = Vector2.zero;

                string capturedDel = gardenName;
                delBtn.onClick.AddListener(() => OnGardenDeleted(capturedDel));
            }
        }

        /// <summary>
        /// Loads the selected garden and closes the popup.
        /// </summary>
        private void OnGardenSelected(string fileName)
        {
            _uiAudio?.PlayConfirm();

            GardenSaveData data = _saveLoad?.LoadGarden(fileName);
            if (data != null)
                _saveLoad?.ApplyGarden(data);

            Debug.Log($"[BonsaiSelectorPopup] Garden '{fileName}' selected and loaded.");
            Close();
        }

        /// <summary>
        /// Deletes a saved garden and refreshes the list.
        /// </summary>
        private void OnGardenDeleted(string fileName)
        {
            _uiAudio?.PlayCancel();
            _saveLoad?.DeleteGarden(fileName);
            Debug.Log($"[BonsaiSelectorPopup] Garden '{fileName}' deleted.");

            // Refresh the list in-place.
            ClearListItems();
            string[] gardens = _saveLoad?.GetSavedGardensList();
            bool hasGardens  = gardens != null && gardens.Length > 0;

            if (_listContent != null)
                _listContent.gameObject.SetActive(hasGardens);
            if (_emptyStateRoot != null)
                _emptyStateRoot.SetActive(!hasGardens);

            if (hasGardens)
                PopulateList(gardens);
        }

        /// <summary>
        /// Returns to the title screen via <see cref="SceneTransitionService"/>.
        /// </summary>
        private void OnBackToMenuPressed()
        {
            _uiAudio?.PlayClick();
            Debug.Log("[BonsaiSelectorPopup] Back to menu pressed.");

            SceneTransitionService.EnsureAvailable();

            if (ServiceLocator.TryGet<ISceneTransitionService>(out var transition))
                transition.TransitionTo("Title_Screen");
        }

        private void OnClosePressed()
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

        /// <summary>
        /// Destroys all dynamic rows from the list content.
        /// </summary>
        private void ClearListItems()
        {
            if (_listContent == null) return;

            for (int i = _listContent.childCount - 1; i >= 0; i--)
                Destroy(_listContent.GetChild(i).gameObject);
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
            if (_listContent == null)
                Debug.LogWarning("[BonsaiSelectorPopup] _listContent is not assigned.", this);
            if (_listItemPrefab == null)
                Debug.LogWarning("[BonsaiSelectorPopup] _listItemPrefab is not assigned.", this);
            if (_emptyStateRoot == null)
                Debug.LogWarning("[BonsaiSelectorPopup] _emptyStateRoot is not assigned.", this);
            if (_btnBackToMenu == null)
                Debug.LogWarning("[BonsaiSelectorPopup] _btnBackToMenu is not assigned.", this);
            if (_saveLoad == null)
                Debug.LogWarning("[BonsaiSelectorPopup] ISaveLoadService is not registered.", this);
        }

        #endregion
    }
}
