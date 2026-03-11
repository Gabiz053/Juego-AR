// ------------------------------------------------------------
//  OrientationManager.cs  -  _Project.Scripts.UI
//  Hides build UI in landscape and restores it in portrait.
// ------------------------------------------------------------

using System.Collections;
using UnityEngine;
using _Project.Scripts.Interaction;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Monitors device orientation and hides the block hotbar, tool
    /// panel and selector when the device is in landscape.  On return
    /// to portrait the previous tool selection is restored.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/UI/Orientation Manager")]
    public class OrientationManager : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("UI Elements to Hide in Landscape")]
        [Tooltip("The block hotbar panel (HUD_Hotbar).")]
        [SerializeField] private GameObject _hotbarPanel;

        [Tooltip("The tool panel (HUD_ToolPanel).")]
        [SerializeField] private GameObject _toolPanel;

        [Tooltip("The yellow selector highlight (HUD_Selector).")]
        [SerializeField] private GameObject _selectorHighlight;

        [Header("Dependencies")]
        [Tooltip("ToolManager -- forces Tool_None in landscape.")]
        [SerializeField] private ToolManager _toolManager;

        #endregion

        #region State ---------------------------------------------

        private bool     _isLandscape;
        private ToolType _previousTool = ToolType.Build_Sand;

        private readonly WaitForEndOfFrame _waitEndOfFrame = new WaitForEndOfFrame();

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Start()
        {
            ValidateReferences();
            EvaluateOrientation();
        }

        private void Update()
        {
            EvaluateOrientation();
        }

        #endregion

        #region Internals -----------------------------------------

        private void EvaluateOrientation()
        {
            bool currentIsLandscape = Screen.width > Screen.height;

            if (currentIsLandscape != _isLandscape)
            {
                _isLandscape = currentIsLandscape;
                OnOrientationChanged(_isLandscape);
            }
        }

        private void OnOrientationChanged(bool landscape)
        {
            bool showBuildUI = !landscape;

            SetActive(_hotbarPanel, showBuildUI);
            SetActive(_toolPanel, showBuildUI);

            if (landscape)
            {
                if (_toolManager != null)
                    _previousTool = _toolManager.CurrentTool;

                SetActive(_selectorHighlight, false);

                if (_toolManager != null)
                    _toolManager.SelectToolByIndex((int)ToolType.Tool_None);
            }
            else
            {
                SetActive(_selectorHighlight, true);
                StartCoroutine(RestoreToolAfterLayout());
            }
        }

        private IEnumerator RestoreToolAfterLayout()
        {
            yield return _waitEndOfFrame;

            if (_toolManager != null)
                _toolManager.SelectToolByIndex((int)_previousTool);
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        private void ValidateReferences()
        {
            if (_hotbarPanel == null)
                Debug.LogError("[OrientationManager] _hotbarPanel is not assigned!", this);
            if (_toolPanel == null)
                Debug.LogError("[OrientationManager] _toolPanel is not assigned!", this);
            if (_selectorHighlight == null)
                Debug.LogError("[OrientationManager] _selectorHighlight is not assigned!", this);
            if (_toolManager == null)
                Debug.LogError("[OrientationManager] _toolManager is not assigned!", this);
        }

        #endregion
    }
}