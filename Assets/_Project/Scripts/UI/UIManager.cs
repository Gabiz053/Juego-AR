using UnityEngine;
using _Project.Scripts.Interaction;

namespace _Project.Scripts.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Dependencias")]
        [SerializeField] private ToolManager _toolManager;

        [Header("Elementos Visuales")]
        [SerializeField] private RectTransform _selectorRect;
        [SerializeField] private RectTransform[] _slotRects;

        private void OnEnable()
        {
            if (_toolManager != null) _toolManager.OnToolChanged += UpdateSelector;
        }

        private void OnDisable()
        {
            if (_toolManager != null) _toolManager.OnToolChanged -= UpdateSelector;
        }

        private void Start()
        {
            // Pequeño retraso de 1 frame para dejar que los Layout Groups ordenen los botones primero
            Invoke(nameof(ForceInitialSelection), 0.1f);
        }

        private void ForceInitialSelection()
        {
            if (_toolManager != null) UpdateSelector(_toolManager.CurrentTool);
        }

        private void UpdateSelector(ToolType newTool)
        {
            int toolIndex = (int)newTool;

            if (toolIndex >= 0 && toolIndex < _slotRects.Length)
            {
                RectTransform targetBtn = _slotRects[toolIndex];

                // 1. Movemos el selector a la coordenada exacta de pantalla del botón
                _selectorRect.position = targetBtn.position;

                // 2. Le copiamos el ancho y alto para que encaje perfecto
                _selectorRect.sizeDelta = new Vector2(targetBtn.rect.width, targetBtn.rect.height);

                Debug.Log($"[UIManager] Selector movido visualmente al botón {newTool} (Índice: {toolIndex})");
            }
        }

        // Esta es la función que llaman los botones
        public void OnSlotClicked(int index)
        {
            Debug.Log($"[UIManager] ¡Clic detectado en el botón número {index}!");
            _toolManager.SelectToolByIndex(index);
        }
    }
}