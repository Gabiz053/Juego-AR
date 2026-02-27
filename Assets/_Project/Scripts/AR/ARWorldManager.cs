using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Gestiona la orientación global del mundo y el ancla espacial para evitar el AR Drift.
    /// </summary>
    [RequireComponent(typeof(ARAnchorManager))]
    public class ARWorldManager : MonoBehaviour
    {
        [Header("Dependencias")]
        [SerializeField] private Transform _worldContainer;
        [SerializeField] private GridManager _gridManager;

        private ARAnchorManager _anchorManager;
        private ARAnchor _worldAnchor;

        public bool IsWorldAnchored => _worldAnchor != null;

        private void Awake()
        {
            _anchorManager = GetComponent<ARAnchorManager>();
        }

        /// <summary>
        /// Fija el mundo al primer bloque colocado, estableciendo el Nivel 0 y la orientación.
        /// </summary>
        public void AnchorWorld(Pose hitPose, Transform playerCamera)
        {
            if (IsWorldAnchored) return;

            // 1. Altura cero (suelo) en el punto de impacto
            _worldContainer.position = hitPose.position;

            // 2. Orientación plana (XZ) relativa al jugador
            Vector3 cameraForwardFlat = playerCamera.forward;
            cameraForwardFlat.y = 0;
            if (cameraForwardFlat.sqrMagnitude > 0.001f)
            {
                _worldContainer.rotation = Quaternion.LookRotation(cameraForwardFlat.normalized);
            }

            // 3. CORRECCIÓN: Comprobamos si hay prefab antes de instanciar para evitar el ArgumentException
            if (_anchorManager.anchorPrefab != null)
            {
                _worldAnchor = Instantiate(_anchorManager.anchorPrefab, hitPose.position, hitPose.rotation).GetComponent<ARAnchor>();
            }

            // Si no había prefab (o falló la instanciación), creamos uno manual de forma segura
            if (_worldAnchor == null)
            {
                GameObject anchorObj = new GameObject("World_ARAnchor");
                anchorObj.transform.SetPositionAndRotation(hitPose.position, hitPose.rotation);
                _worldAnchor = anchorObj.AddComponent<ARAnchor>();
            }

            // 4. Emparentamos el mundo al ancla
            _worldContainer.SetParent(_worldAnchor.transform);

            // 5. Activamos el halo de la cuadrícula
            _gridManager.ActivarGrid(playerCamera);

            Debug.Log("Mundo Anclado: Orientación establecida y Cuadrícula activada.");
        }
    }
}