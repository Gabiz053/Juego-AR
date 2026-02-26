using UnityEngine;

namespace _Project.Scripts.Environment
{
    /// <summary>
    /// Contiene la información y propiedades únicas de cada tipo de bloque.
    /// Va adjunto a cada Prefab (Arena, Piedra, Madera, etc.).
    /// </summary>
    public class VoxelBlock : MonoBehaviour
    {
        [Header("Efectos de Sonido")]
        [Tooltip("El clip de audio que sonará al colocar este bloque en el mundo.")]
        [SerializeField] private AudioClip _placeSound;

        [Tooltip("El clip de audio que sonará cuando el jugador destruya este bloque.")]
        [SerializeField] private AudioClip _breakSound;

        // Propiedades públicas (Getters) para que otros scripts puedan leer los sonidos
        // sin poder modificarlos por accidente.
        public AudioClip PlaceSound => _placeSound;
        public AudioClip BreakSound => _breakSound;
    }
}