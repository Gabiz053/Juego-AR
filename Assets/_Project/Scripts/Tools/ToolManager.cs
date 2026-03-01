using System;
using UnityEngine;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Lista de todas las herramientas. 
    /// ¡ATENCIÓN A LOS NÚMEROS! Hemos añadido Tool_None en el índice 5.
    /// </summary>
    public enum ToolType
    {
        Build_Tierra = 0,
        Build_Arena = 1,
        Build_Piedra = 2,
        Build_Madera = 3,
        Build_Antorcha = 4,
        Tool_None = 5,    // <-- NUEVO: Mano vacía (para no hacer nada)
        Tool_Destroy = 6, // <-- Ahora es el 6
        Tool_Plow = 7,    // <-- Ahora es el 7
        Tool_Brush = 8    // <-- Ahora es el 8
    }

    public class ToolManager : MonoBehaviour
    {
        [Header("Prefabs de Construcción")]
        [SerializeField] private GameObject _prefabTierra;
        [SerializeField] private GameObject _prefabArena;
        [SerializeField] private GameObject _prefabPiedra;
        [SerializeField] private GameObject _prefabMadera;
        [SerializeField] private GameObject _prefabAntorcha;

        // Herramienta seleccionada actualmente. (Puedes empezar con la mano vacía si prefieres)
        public ToolType CurrentTool { get; private set; } = ToolType.Build_Tierra;

        public event Action<ToolType> OnToolChanged;

        public void SelectToolByIndex(int index)
        {
            CurrentTool = (ToolType)index;
            Debug.Log($"[ToolManager] Herramienta seleccionada: {CurrentTool}");

            OnToolChanged?.Invoke(CurrentTool);
        }

        public GameObject GetCurrentBlockPrefab()
        {
            return CurrentTool switch
            {
                ToolType.Build_Tierra => _prefabTierra,
                ToolType.Build_Arena => _prefabArena,
                ToolType.Build_Piedra => _prefabPiedra,
                ToolType.Build_Madera => _prefabMadera,
                ToolType.Build_Antorcha => _prefabAntorcha,
                _ => null // Para Destruir, Mano Vacía, Arado o Pincel, devuelve null (no construye)
            };
        }

        public bool IsBuildingTool()
        {
            return GetCurrentBlockPrefab() != null;
        }
    }
}