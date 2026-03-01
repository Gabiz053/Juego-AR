using System.Collections;
using UnityEngine;
using _Project.Scripts.Core;

namespace _Project.Scripts.UI
{
    public class GameOptionsMenu : MonoBehaviour
    {
        [Header("Interfaz (UI)")]
        [SerializeField] private GameObject _optionsPanel;
        [Tooltip("El botón gigante invisible que cierra el menú al tocar fuera.")]
        [SerializeField] private GameObject _blockerPanel; // <-- NUEVO
        [SerializeField] private Canvas _mainCanvas;
        [SerializeField] private GameObject _confirmPopup;

        [Header("Mundo AR")]
        [SerializeField] private Transform _worldContainer;
        [SerializeField] private Light _directionalLight;

        [Header("Managers a Resetear")]
        [SerializeField] private ARWorldManager _arWorldManager;
        [SerializeField] private GridManager _gridManager;

        private void Start()
        {
            if (_optionsPanel != null) _optionsPanel.SetActive(false);
            if (_confirmPopup != null) _confirmPopup.SetActive(false);

            // Apagamos el bloqueador al empezar
            if (_blockerPanel != null) _blockerPanel.SetActive(false);
        }

        public void ToggleMenu()
        {
            if (_optionsPanel != null)
            {
                // Invertimos el estado (si está abierto lo cierra, y viceversa)
                bool isOpen = !_optionsPanel.activeSelf;

                _optionsPanel.SetActive(isOpen);

                // Hacemos que el bloqueador invisible aparezca solo si el menú está abierto
                if (_blockerPanel != null) _blockerPanel.SetActive(isOpen);
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

        // --- LÓGICA DE BORRADO ---

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
            // 1. Destruimos todos los bloques físicos
            if (_worldContainer != null)
            {
                for (int i = _worldContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(_worldContainer.GetChild(i).gameObject);
                }
            }

            // 2. Destruimos el Ancla AR para poder empezar de cero en otro sitio
            if (_arWorldManager != null)
            {
                _arWorldManager.ResetAnchor();
            }

            // 3. Apagamos el halo verde de la cuadrícula
            if (_gridManager != null)
            {
                _gridManager.DesactivarGrid();
            }

            // Ocultamos el popup al terminar
            if (_confirmPopup != null) _confirmPopup.SetActive(false);

            Debug.Log("[Menu] Mundo reiniciado TOTALMENTE (Bloques + Ancla).");
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