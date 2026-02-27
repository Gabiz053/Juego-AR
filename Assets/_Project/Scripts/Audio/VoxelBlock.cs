using UnityEngine;

namespace _Project.Scripts.Environment
{
    /// <summary>
    /// Contiene las propiedades únicas de cada tipo de bloque.
    /// Va adjunto a los Prefabs (Arena, Piedra, Madera...).
    /// </summary>
    public class VoxelBlock : MonoBehaviour
    {
        [Header("Efectos de Sonido")]
        [Tooltip("Audio al colocar este bloque.")]
        [SerializeField] private AudioClip _placeSound;

        [Tooltip("Audio al destruir este bloque.")]
        [SerializeField] private AudioClip _breakSound;

        public AudioClip PlaceSound => _placeSound;
        public AudioClip BreakSound => _breakSound;
    }
}