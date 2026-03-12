// ------------------------------------------------------------
//  TitleSceneManager.cs  -  _Project.Scripts.Title
//  Bootstrap controller for the Title / Face-Track scene.
// ------------------------------------------------------------

using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Scripts.Title
{
    /// <summary>
    /// Entry-point controller for the <c>Title_FaceTrack</c> scene.<br/>
    /// Handles scene transitions to the main AR game.  Future
    /// responsibilities: dwell-time buttons, world-mode selection,
    /// hand-tracking cursor.
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
        /// Loads the main AR game scene.  Called by the Play button
        /// (or future dwell-time trigger).
        /// </summary>
        public void StartGame()
        {
            Debug.Log("[TitleSceneManager] Loading game scene...");
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
