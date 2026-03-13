// ------------------------------------------------------------
//  CreeperFaceFilter.cs  -  _Project.Scripts.Title
//  Overlays a Creeper head prefab on the AR-tracked face.
// ------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace _Project.Scripts.Title
{
    /// <summary>
    /// Subscribes to <see cref="ARFaceManager"/> events and attaches a
    /// Creeper head prefab to every detected <see cref="ARFace"/>.<br/>
    /// The prefab is instantiated as a child of the face transform so it
    /// follows head position and rotation automatically.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Title/Creeper Face Filter")]
    public class CreeperFaceFilter : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Creeper Head Prefab")]
        [Tooltip("Creeper head prefab instantiated as child of the tracked face.")]
        [SerializeField] private GameObject _creeperHeadPrefab;

        [Tooltip("Local position offset applied to the prefab relative to the face centre.")]
        [SerializeField] private Vector3 _prefabOffset = Vector3.zero;

        [Tooltip("Local euler rotation applied to the prefab relative to the face.")]
        [SerializeField] private Vector3 _prefabRotation = Vector3.zero;

        [Tooltip("Local scale applied to the prefab.")]
        [SerializeField] private Vector3 _prefabScale = Vector3.one;

        [Header("Dependencies")]
        [Tooltip("ARFaceManager � auto-detected on this or parent GameObject if empty.")]
        [SerializeField] private ARFaceManager _faceManager;

        #endregion

        #region State ---------------------------------------------

        private readonly Dictionary<TrackableId, GameObject> _spawnedHeads = new();

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
                AttachPrefab(face);

            foreach (KeyValuePair<TrackableId, ARFace> kvp in args.removed)
                DetachPrefab(kvp.Key);
        }

        /// <summary>
        /// Instantiates the Creeper head prefab as a child of the
        /// <see cref="ARFace"/> transform so it tracks automatically.
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
        /// Destroys the spawned prefab when its face is lost.
        /// </summary>
        private void DetachPrefab(TrackableId id)
        {
            if (_spawnedHeads.TryGetValue(id, out GameObject head))
            {
                if (head != null) Destroy(head);
                _spawnedHeads.Remove(id);
                Debug.Log($"[CreeperFaceFilter] Prefab detached -- face {id} lost.");
            }
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_faceManager == null)
                Debug.LogError("[CreeperFaceFilter] ARFaceManager not found!", this);
            if (_creeperHeadPrefab == null)
                Debug.LogError("[CreeperFaceFilter] _creeperHeadPrefab is not assigned!", this);
        }

        #endregion
    }
}
