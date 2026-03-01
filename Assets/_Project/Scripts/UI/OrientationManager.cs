using System.Collections;
using UnityEngine;
using _Project.Scripts.Interaction;

namespace _Project.Scripts.UI
{
    public class OrientationManager : MonoBehaviour
    {
        [Header("Elementos a Ocultar en Horizontal")]
        [SerializeField] private GameObject _hotbarBlocks;
        [SerializeField] private GameObject _actionTools;
        [Tooltip("El cuadrado amarillo que resalta la herramienta.")]
        [SerializeField] private GameObject _selectorVisual; // <-- NUEVO

        [Header("Dependencias")]
        [SerializeField] private ToolManager _toolManager;

        private bool _isLandscape = false;
        private ToolType _previousTool = ToolType.Build_Tierra; // Recordará tu herramienta

        private void Start()
        {
            CheckOrientation();
        }

        private void Update()
        {
            CheckOrientation();
        }

        private void CheckOrientation()
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

            if (_hotbarBlocks != null) _hotbarBlocks.SetActive(showBuildUI);
            if (_actionTools != null) _actionTools.SetActive(showBuildUI);

            if (landscape)
            {
                // 1. Guardamos la herramienta que tenías antes de girar
                if (_toolManager != null) _previousTool = _toolManager.CurrentTool;

                // 2. Apagamos el cuadrado amarillo
                if (_selectorVisual != null) _selectorVisual.SetActive(false);

                // 3. Forzamos la mano vacía para no construir por accidente
                if (_toolManager != null) _toolManager.SelectToolByIndex(5);
            }
            else
            {
                // 1. Volvemos a encender el cuadrado
                if (_selectorVisual != null) _selectorVisual.SetActive(true);

                // 2. Esperamos a que los botones se coloquen bien
                StartCoroutine(RebuildUIRoutine());
            }
        }

        private IEnumerator RebuildUIRoutine()
        {
            // Magia: Le damos a Unity 1 fotograma de tiempo para ordenar la UI vertical
            yield return new WaitForEndOfFrame();

            // Le pedimos al cerebro que vuelva a seleccionar tu herramienta anterior, 
            // lo que obligará al UIManager a mover el cuadrado a la posición correcta.
            if (_toolManager != null)
            {
                _toolManager.SelectToolByIndex((int)_previousTool);
            }
        }
    }
}