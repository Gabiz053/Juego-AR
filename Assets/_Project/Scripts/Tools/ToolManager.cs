using System;
using UnityEngine;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Lista de todas las herramientas y bloques disponibles en el juego.
    /// El orden numérico es vital para la UI (0 al 7).
    /// </summary>
    public enum ToolType
    {
        Build_Tierra = 0,
        Build_Arena = 1,
        Build_Piedra = 2,
        Build_Madera = 3,
        Build_Antorcha = 4,
        Tool_Destroy = 5,
        Tool_Plow = 6,    // Arado
        Tool_Brush = 7    // Pincel 3D
    }

    /// <summary>
    /// Gestiona qué herramienta tiene el jugador seleccionada actualmente
    /// y provee los Prefabs correspondientes a otros scripts.
    /// </summary>
    public class ToolManager : MonoBehaviour
    {
        [Header("Prefabs de Construcción (Los 5 Bloques)")]
        [SerializeField] private GameObject _prefabTierra;
        [SerializeField] private GameObject _prefabArena;
        [SerializeField] private GameObject _prefabPiedra;
        [SerializeField] private GameObject _prefabMadera;
        [SerializeField] private GameObject _prefabAntorcha;

        // Herramienta seleccionada actualmente. Empezamos con Tierra por defecto.
        public ToolType CurrentTool { get; private set; } = ToolType.Build_Tierra;

        // Evento (Action) al que la UI se suscribirá para mover el selector visual
        public event Action<ToolType> OnToolChanged;

        /// <summary>
        /// Cambia la herramienta activa. 
        /// Será llamado por los botones de la UI (Hotbar).
        /// </summary>
        /// <param name="index">El índice numérico de la herramienta (0 al 7)</param>
        public void SelectToolByIndex(int index)
        {
            CurrentTool = (ToolType)index;
            Debug.Log($"[ToolManager] Herramienta seleccionada: {CurrentTool}");

            OnToolChanged?.Invoke(CurrentTool);
        }

        /// <summary>
        /// Devuelve el prefab correspondiente si tenemos un bloque seleccionado.
        /// </summary>
        public GameObject GetCurrentBlockPrefab()
        {
            return CurrentTool switch
            {
                ToolType.Build_Tierra => _prefabTierra,
                ToolType.Build_Arena => _prefabArena,
                ToolType.Build_Piedra => _prefabPiedra,
                ToolType.Build_Madera => _prefabMadera,
                ToolType.Build_Antorcha => _prefabAntorcha,
                _ => null // Para Destruir, Arado o Pincel
            };
        }

        /// <summary>
        /// Comprueba si la herramienta actual es de colocar bloques.
        /// </summary>
        public bool IsBuildingTool()
        {
            return GetCurrentBlockPrefab() != null;
        }
    }
}