using System.Collections;
using UnityEngine;

namespace _Project.Scripts.UI
{
    public class GameOptionsMenu : MonoBehaviour
    {
        [Header("Interfaz (UI)")]
        [Tooltip("El panel que contiene los botones de opciones (el que se oculta).")]
        [SerializeField] private GameObject _optionsPanel;
        [Tooltip("El Canvas principal, para ocultarlo al hacer la foto.")]
        [SerializeField] private Canvas _mainCanvas;
        [Tooltip("El Panel del popup de confirmación de borrado.")]
        [SerializeField] private GameObject _confirmPopup; // <-- NUEVO

        [Header("Mundo AR")]
        [SerializeField] private Transform _worldContainer;
        [SerializeField] private Light _directionalLight;

        private void Start()
        {
            if (_optionsPanel != null) _optionsPanel.SetActive(false);

            // Nos aseguramos de que el popup empiece apagado por seguridad
            if (_confirmPopup != null) _confirmPopup.SetActive(false);
        }

        public void ToggleMenu()
        {
            if (_optionsPanel != null)
            {
                _optionsPanel.SetActive(!_optionsPanel.activeSelf);
            }
        }

        public void ToggleLighting()
        {
            if (_directionalLight != null)
            {
                _directionalLight.enabled = !_directionalLight.enabled;
                Debug.Log($"[Menu] Iluminación {(_directionalLight.enabled ? "Activada" : "Desactivada")}");
            }
        }

        // --- NUEVA LÓGICA DE BORRADO ---

        /// <summary>
        /// El botón de la papelera llama a esto. Solo abre el popup y esconde el menú superior.
        /// </summary>
        public void RequestClearAll()
        {
            if (_confirmPopup != null)
            {
                _confirmPopup.SetActive(true);
                ToggleMenu(); // Cerramos el menú desplegable para que no moleste
            }
        }

        /// <summary>
        /// El botón de "SÍ, BORRAR" llama a esto. Es el que destruye de verdad.
        /// </summary>
        public void ConfirmClearAll()
        {
            if (_worldContainer == null) return;

            for (int i = _worldContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_worldContainer.GetChild(i).gameObject);
            }

            // Ocultamos el popup al terminar
            if (_confirmPopup != null) _confirmPopup.SetActive(false);
            Debug.Log("[Menu] Mundo reiniciado. Todos los bloques eliminados.");
        }

        /// <summary>
        /// El botón de "CANCELAR" llama a esto.
        /// </summary>
        public void CancelClearAll()
        {
            if (_confirmPopup != null) _confirmPopup.SetActive(false);
        }

        // -------------------------------

        public void TakePhoto()
        {
            StartCoroutine(CaptureScreenshotRoutine());
        }

        private IEnumerator CaptureScreenshotRoutine()
        {
            if (_mainCanvas != null) _mainCanvas.enabled = false;
            yield return new WaitForEndOfFrame();

            string timeStamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"JardinZenAR_{timeStamp}.png";
            ScreenCapture.CaptureScreenshot(fileName);
            Debug.Log($"[Menu] Foto guardada: {fileName}");

            if (_mainCanvas != null) _mainCanvas.enabled = true;
            ToggleMenu();
        }

        public void ExitGame()
        {
            Debug.Log("[Menu] Saliendo del juego...");
            Application.Quit();
        }
    }
}