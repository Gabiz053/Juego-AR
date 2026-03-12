// ------------------------------------------------------------
//  TitleSceneManager.cs  -  _Project.Scripts.Title
//  Bootstrap controller for the Title / Face-Track scene.
// ------------------------------------------------------------

using UnityEngine;
using UnityEngine.SceneManagement;
using _Project.Scripts.Core;

namespace _Project.Scripts.Title
{
    /// <summary>
    /// Entry-point controller for the <c>Title_Screen</c> scene.<br/>
    /// Presents three mode buttons (Bonsai, Normal, Real).  Each button
    /// calls <see cref="SelectMode"/> which writes the chosen
    /// <see cref="WorldMode"/> into <see cref="WorldModeContext"/> and
    /// loads the game scene.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Title/Title Scene Manager")]
    public class TitleSceneManager : MonoBehaviour
    {
        #region Constants -----------------------------------------

        /// <summary>Scene name loaded when the player starts the game.</summary>
        private const string GAME_SCENE = "Main_AR";

        #endregion

        #region Inspector -----------------------------------------

        [Header("UI References")]
        [Tooltip("Canvas root for the title screen UI.")]
        [SerializeField] private Canvas _titleCanvas;

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Start()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Debug.Log("[TitleSceneManager] Title scene started.");
            ValidateReferences();
        }

        #endregion

        #region Public API ----------------------------------------

        /// <summary>
        /// Selects a <see cref="WorldMode"/> by its integer value and loads
        /// the game scene.<br/>
        /// Designed for <c>Button.OnClick</c> wiring in the Inspector:<br/>
        /// • Btn_Bonsai ? <c>SelectMode(0)</c><br/>
        /// • Btn_Normal ? <c>SelectMode(1)</c><br/>
        /// • Btn_Real   ? <c>SelectMode(2)</c>
        /// </summary>
        public void SelectMode(int modeIndex)
        {
            WorldMode mode = (WorldMode)modeIndex;
            WorldModeContext.Selected = mode;
            Debug.Log($"[TitleSceneManager] Mode selected: {mode} -- loading {GAME_SCENE}.");
            SceneManager.LoadScene(GAME_SCENE);
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_titleCanvas == null)
                Debug.LogWarning("[TitleSceneManager] _titleCanvas is not assigned.", this);
        }

        #endregion
    }
}
