// ------------------------------------------------------------
//  BonsaiSessionController.cs  -  _Project.Scripts.AR
//  Orchestrates the Bonsai mode flow: waits for image detection,
//  then opens the garden selector popup.
// ------------------------------------------------------------

using UnityEngine;
using _Project.Scripts.Infrastructure;
using _Project.Scripts.UI;

namespace _Project.Scripts.AR
{
    /// <summary>
    /// Listens for <see cref="WorldModeBootstrapper.OnBonsaiImageDetected"/>
    /// and shows the <see cref="BonsaiSelectorPopup"/> so the user can
    /// pick a saved garden to load onto the tracked card.<br/>
    /// Self-disables when <see cref="WorldModeContext.Selected"/> is not
    /// <see cref="WorldMode.Bonsai"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/AR/Bonsai Session Controller")]
    public class BonsaiSessionController : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Dependencies")]
        [Tooltip("Bootstrapper that fires OnBonsaiImageDetected.")]
        [SerializeField] private WorldModeBootstrapper _worldModeBootstrapper;

        [Tooltip("Popup shown when the tracked image is detected.")]
        [SerializeField] private BonsaiSelectorPopup _bonsaiSelectorPopup;

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            if (WorldModeContext.Selected != WorldMode.Bonsai)
            {
                enabled = false;
                return;
            }
        }

        private void OnEnable()
        {
            if (_worldModeBootstrapper != null)
                _worldModeBootstrapper.OnBonsaiImageDetected += HandleImageDetected;
        }

        private void OnDisable()
        {
            if (_worldModeBootstrapper != null)
                _worldModeBootstrapper.OnBonsaiImageDetected -= HandleImageDetected;
        }

        private void Start()
        {
            ValidateReferences();
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Called once when the first tracked image is detected.
        /// Opens the garden selector popup for the user to choose
        /// a previously saved garden.
        /// </summary>
        private void HandleImageDetected()
        {
            if (_bonsaiSelectorPopup == null)
            {
                Debug.LogWarning("[BonsaiSessionController] _bonsaiSelectorPopup is null -- cannot show selector.");
                return;
            }

            _bonsaiSelectorPopup.Show();
            Debug.Log("[BonsaiSessionController] Image detected -- garden selector popup opened.");
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_worldModeBootstrapper == null)
                Debug.LogWarning("[BonsaiSessionController] _worldModeBootstrapper is not assigned.", this);
            if (_bonsaiSelectorPopup == null)
                Debug.LogWarning("[BonsaiSessionController] _bonsaiSelectorPopup is not assigned.", this);
        }

        #endregion
    }
}
