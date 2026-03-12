// ------------------------------------------------------------
//  CreeperFaceFilter.cs  -  _Project.Scripts.Title
//  Overlays a Creeper head on the AR-tracked face.
// ------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace _Project.Scripts.Title
{
    /// <summary>
    /// Subscribes to <see cref="ARFaceManager"/> events and attaches a
    /// Creeper visual to every detected <see cref="ARFace"/>.<br/><br/>
    /// <b>Mode A — 3D Prefab (recommended):</b> Instantiates
    /// <see cref="_creeperHeadPrefab"/> as a child of the face transform.
    /// The prefab follows head position and rotation automatically.
    /// Use this mode with your own Creeper head model.<br/><br/>
    /// <b>Mode B — Flat Texture:</b> Applies <see cref="_creeperMaterial"/>
    /// directly onto the AR face mesh geometry for a flat overlay.
    /// Use this mode with the procedural <see cref="CreeperFaceTexture"/>.<br/><br/>
    /// If both are assigned, Mode A (prefab) takes priority.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Title/Creeper Face Filter")]
    public class CreeperFaceFilter : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Mode A — 3D Prefab (recommended)")]
        [Tooltip("Creeper head prefab instantiated as child of the tracked face.")]
        [SerializeField] private GameObject _creeperHeadPrefab;

        [Tooltip("Local position offset applied to the prefab relative to the face centre.")]
        [SerializeField] private Vector3 _prefabOffset = Vector3.zero;

        [Tooltip("Local euler rotation applied to the prefab relative to the face.")]
        [SerializeField] private Vector3 _prefabRotation = Vector3.zero;

        [Tooltip("Local scale applied to the prefab.")]
        [SerializeField] private Vector3 _prefabScale = Vector3.one;

        [Header("Mode B — Flat Texture Overlay")]
        [Tooltip("Material applied to the face mesh (unlit + transparent). " +
                 "Auto-assigned by CreeperFaceTexture if present.")]
        [SerializeField] private Material _creeperMaterial;

        [Header("Dependencies")]
        [Tooltip("ARFaceManager — auto-detected on this or parent GameObject if empty.")]
        [SerializeField] private ARFaceManager _faceManager;

        #endregion

        #region State ---------------------------------------------

        private readonly Dictionary<TrackableId, GameObject> _spawnedHeads = new();

        #endregion

        #region Public API ----------------------------------------

        /// <summary>
        /// Assigns the overlay material at runtime.<br/>
        /// Used by <see cref="CreeperFaceTexture"/> auto-wire (Mode B).
        /// </summary>
        public void SetMaterial(Material material)
        {
            _creeperMaterial = material;
            Debug.Log("[CreeperFaceFilter] Material set at runtime.");
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            if (_faceManager == null)
                _faceManager = GetComponentInParent<ARFaceManager>();
        }

        private void OnEnable()
        {
            if (_faceManager != null)
                _faceManager.trackablesChanged.AddListener(OnFacesChanged);
        }

        private void OnDisable()
        {
            if (_faceManager != null)
                _faceManager.trackablesChanged.RemoveListener(OnFacesChanged);
        }

        private void Start()
        {
            ValidateReferences();
        }

        private void OnDestroy()
        {
            foreach (var kvp in _spawnedHeads)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value);
            }
            _spawnedHeads.Clear();
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Callback for <see cref="ARFaceManager.trackablesChanged"/>.
        /// Routes added and removed faces to the appropriate handler.
        /// </summary>
        private void OnFacesChanged(ARTrackablesChangedEventArgs<ARFace> args)
        {
            foreach (ARFace face in args.added)
                AttachCreeper(face);

            // removed is ReadOnlyList<KeyValuePair<TrackableId, ARFace>>
            foreach (KeyValuePair<TrackableId, ARFace> kvp in args.removed)
                DetachCreeper(kvp.Key);
        }

        /// <summary>
        /// Attaches the Creeper to a newly detected face.
        /// Prefab mode (A) takes priority over material mode (B).
        /// </summary>
        private void AttachCreeper(ARFace face)
        {
            if (_creeperHeadPrefab != null)
            {
                AttachPrefab(face);
                return;
            }

            if (_creeperMaterial != null)
            {
                ApplyMaterial(face);
                return;
            }

            Debug.LogWarning("[CreeperFaceFilter] No prefab or material assigned — nothing to show.");
        }

        /// <summary>
        /// <b>Mode A:</b> Instantiates the Creeper head prefab as a child
        /// of the <see cref="ARFace"/> transform so it tracks automatically.
        /// </summary>
        private void AttachPrefab(ARFace face)
        {
            if (_spawnedHeads.ContainsKey(face.trackableId)) return;

            GameObject head = Instantiate(_creeperHeadPrefab, face.transform);
            head.transform.localPosition = _prefabOffset;
            head.transform.localRotation = Quaternion.Euler(_prefabRotation);
            head.transform.localScale    = _prefabScale;

            _spawnedHeads[face.trackableId] = head;
            Debug.Log($"[CreeperFaceFilter] Prefab attached to face {face.trackableId}.");
        }

        /// <summary>
        /// <b>Mode B:</b> Applies the Creeper material to the face mesh.
        /// Ensures <see cref="ARFaceMeshVisualizer"/>, <see cref="MeshFilter"/>
        /// and <see cref="MeshRenderer"/> are present on the face.
        /// </summary>
        private void ApplyMaterial(ARFace face)
        {
            if (face.GetComponent<ARFaceMeshVisualizer>() == null)
                face.gameObject.AddComponent<ARFaceMeshVisualizer>();

            if (face.GetComponent<MeshFilter>() == null)
                face.gameObject.AddComponent<MeshFilter>();

            MeshRenderer renderer = face.GetComponent<MeshRenderer>();
            if (renderer == null)
                renderer = face.gameObject.AddComponent<MeshRenderer>();

            renderer.material = _creeperMaterial;
            Debug.Log($"[CreeperFaceFilter] Material applied to face {face.trackableId}.");
        }

        /// <summary>
        /// Destroys the spawned prefab when its face is lost.
        /// </summary>
        private void DetachCreeper(TrackableId id)
        {
            if (_spawnedHeads.TryGetValue(id, out GameObject head))
            {
                if (head != null) Destroy(head);
                _spawnedHeads.Remove(id);
                Debug.Log($"[CreeperFaceFilter] Prefab detached — face {id} lost.");
            }
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_faceManager == null)
                Debug.LogError("[CreeperFaceFilter] ARFaceManager not found!", this);
            if (_creeperHeadPrefab == null && _creeperMaterial == null)
                Debug.LogError("[CreeperFaceFilter] Assign either a prefab (Mode A) " +
                               "or a material (Mode B)!", this);
        }

        #endregion
    }
}
