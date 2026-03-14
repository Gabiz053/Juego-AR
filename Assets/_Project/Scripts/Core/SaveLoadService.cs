// ------------------------------------------------------------
//  SaveLoadService.cs  -  _Project.Scripts.Core
//  Persists garden state to JSON and restores it into the world.
// ------------------------------------------------------------

using System;
using System.IO;
using UnityEngine;
using _Project.Scripts.Infrastructure;
using _Project.Scripts.Voxel;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Saves and loads garden state as JSON files in
    /// <c>Application.persistentDataPath/Gardens/</c>.<br/>
    /// On load, clears existing blocks/pebbles, instantiates
    /// from saved data, arms each instance for immediate use
    /// (skipping spawn animation), and triggers a harmony rescan.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Core/Save Load Service")]
    public class SaveLoadService : MonoBehaviour, ISaveLoadService
    {
        #region Constants -----------------------------------------

        /// <summary>Subdirectory inside persistentDataPath for garden files.</summary>
        private const string GARDENS_FOLDER = "Gardens";

        /// <summary>File extension for saved gardens.</summary>
        private const string FILE_EXTENSION = ".json";

        /// <summary>Suffix appended by Unity when instantiating prefabs.</summary>
        private const string CLONE_SUFFIX = "(Clone)";

        #endregion

        #region Inspector -----------------------------------------

        [Header("World")]
        [Tooltip("Transform that parents all placed blocks and pebbles.")]
        [SerializeField] private Transform _worldContainer;

        [Header("Databases")]
        [Tooltip("Block database for looking up prefabs by BlockType.")]
        [SerializeField] private BlockDatabaseSO _blockDatabase;

        [Tooltip("Pebble prefabs pool (same order as PlowTool._pebblePrefabs).")]
        [SerializeField] private GameObject[] _pebblePrefabs;

        #endregion

        #region State ---------------------------------------------

        private string _gardensPath;

        #endregion

        #region Public API ----------------------------------------

        /// <inheritdoc/>
        public void SaveCurrentGarden(string gardenName)
        {
            if (_worldContainer == null)
            {
                Debug.LogWarning("[SaveLoadService] _worldContainer is null -- cannot save.");
                return;
            }

            GardenSaveData data = SnapshotWorld(gardenName);
            string json     = JsonUtility.ToJson(data, true);
            string fileName = SanitizeFileName(gardenName);
            string filePath = Path.Combine(_gardensPath, fileName + FILE_EXTENSION);

            EnsureDirectoryExists();
            File.WriteAllText(filePath, json);

            Debug.Log($"[SaveLoadService] Garden '{gardenName}' saved -- {data.voxels.Length} voxels, {data.pebbles.Length} pebbles -> {filePath}.");
        }

        /// <inheritdoc/>
        public string[] GetSavedGardensList()
        {
            EnsureDirectoryExists();

            string[] files = Directory.GetFiles(_gardensPath, "*" + FILE_EXTENSION);
            string[] names = new string[files.Length];

            for (int i = 0; i < files.Length; i++)
                names[i] = Path.GetFileNameWithoutExtension(files[i]);

            Debug.Log($"[SaveLoadService] Found {names.Length} saved garden(s).");
            return names;
        }

        /// <inheritdoc/>
        public GardenSaveData LoadGarden(string fileName)
        {
            string filePath = Path.Combine(_gardensPath, fileName + FILE_EXTENSION);

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[SaveLoadService] File not found: {filePath}.");
                return null;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                GardenSaveData data = JsonUtility.FromJson<GardenSaveData>(json);
                Debug.Log($"[SaveLoadService] Loaded '{data.gardenName}' -- {data.voxels.Length} voxels, {data.pebbles.Length} pebbles.");
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveLoadService] Failed to load garden '{fileName}': {ex.Message}.");
                return null;
            }
        }

        /// <inheritdoc/>
        public void ApplyGarden(GardenSaveData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[SaveLoadService] Cannot apply null garden data.");
                return;
            }

            ClearWorldContents();
            ClearUndoHistory();
            InstantiateVoxels(data.voxels);
            InstantiatePebbles(data.pebbles);
            TriggerHarmonyRescan();

            Debug.Log($"[SaveLoadService] Garden '{data.gardenName}' applied -- {data.voxels.Length} voxels, {data.pebbles.Length} pebbles.");
        }

        /// <inheritdoc/>
        public void DeleteGarden(string fileName)
        {
            string filePath = Path.Combine(_gardensPath, fileName + FILE_EXTENSION);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log($"[SaveLoadService] Deleted garden file: {filePath}.");
            }
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            _gardensPath = Path.Combine(Application.persistentDataPath, GARDENS_FOLDER);
            ServiceLocator.Register<ISaveLoadService>(this);
        }

        private void Start()
        {
            ValidateReferences();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<ISaveLoadService>();
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Iterates WorldContainer children and builds a
        /// <see cref="GardenSaveData"/> snapshot.
        /// </summary>
        private GardenSaveData SnapshotWorld(string gardenName)
        {
            var voxelList  = new System.Collections.Generic.List<VoxelSaveData>();
            var pebbleList = new System.Collections.Generic.List<PebbleSaveData>();

            foreach (Transform child in _worldContainer)
            {
                VoxelBlock vb = child.GetComponent<VoxelBlock>();
                if (vb != null)
                {
                    voxelList.Add(new VoxelSaveData(
                        (int)vb.Type,
                        child.localPosition));
                    continue;
                }

                ProceduralPebble pp = child.GetComponent<ProceduralPebble>();
                if (pp != null)
                {
                    int prefabIndex = MatchPebblePrefabIndex(child.gameObject);
                    pebbleList.Add(new PebbleSaveData(
                        prefabIndex,
                        child.localPosition,
                        child.localRotation,
                        child.localScale));
                }
            }

            return new GardenSaveData
            {
                gardenName = gardenName,
                createdAt  = DateTime.Now.ToString("o"),
                voxels     = voxelList.ToArray(),
                pebbles    = pebbleList.ToArray()
            };
        }

        /// <summary>
        /// Matches an instantiated pebble to its source prefab index
        /// by comparing the GameObject name (strip "(Clone)" suffix).
        /// Falls back to index 0 when no match is found.
        /// </summary>
        private int MatchPebblePrefabIndex(GameObject pebbleInstance)
        {
            if (_pebblePrefabs == null || _pebblePrefabs.Length == 0) return 0;

            string instanceName = pebbleInstance.name.Replace(CLONE_SUFFIX, "").Trim();

            for (int i = 0; i < _pebblePrefabs.Length; i++)
            {
                if (_pebblePrefabs[i] != null && _pebblePrefabs[i].name == instanceName)
                    return i;
            }

            return 0;
        }

        /// <summary>
        /// Destroys all VoxelBlock and ProceduralPebble children of
        /// WorldContainer without resetting the AR anchor or grid.
        /// </summary>
        private void ClearWorldContents()
        {
            if (_worldContainer == null) return;

            for (int i = _worldContainer.childCount - 1; i >= 0; i--)
            {
                GameObject child = _worldContainer.GetChild(i).gameObject;

                bool isBlock  = child.GetComponent<VoxelBlock>()       != null;
                bool isPebble = child.GetComponent<ProceduralPebble>() != null;

                if (isBlock || isPebble)
                    Destroy(child);
            }
        }

        /// <summary>Clears the undo/redo stacks via ServiceLocator.</summary>
        private void ClearUndoHistory()
        {
            if (ServiceLocator.TryGet<IUndoRedoService>(out var undoRedo))
                undoRedo.Clear();
        }

        /// <summary>
        /// Instantiates voxel blocks from save data, arms them for
        /// immediate use (no fly-in animation).
        /// </summary>
        private void InstantiateVoxels(VoxelSaveData[] voxels)
        {
            if (voxels == null || _blockDatabase == null) return;

            foreach (VoxelSaveData vd in voxels)
            {
                BlockType type = (BlockType)vd.blockType;
                GameObject prefab = _blockDatabase.GetPrefab(type);

                if (prefab == null)
                {
                    Debug.LogWarning($"[SaveLoadService] No prefab for BlockType {type} -- skipping.");
                    continue;
                }

                GameObject instance = Instantiate(prefab, _worldContainer);
                instance.transform.SetLocalPositionAndRotation(vd.LocalPosition, Quaternion.identity);
                PlaceBlockAction.ArmForImmediate(instance);
            }
        }

        /// <summary>
        /// Instantiates pebbles from save data, applies saved transform,
        /// and arms them for immediate use (no spawn animation).
        /// </summary>
        private void InstantiatePebbles(PebbleSaveData[] pebbles)
        {
            if (pebbles == null || _pebblePrefabs == null || _pebblePrefabs.Length == 0)
                return;

            foreach (PebbleSaveData pd in pebbles)
            {
                int index = Mathf.Clamp(pd.prefabIndex, 0, _pebblePrefabs.Length - 1);
                GameObject prefab = _pebblePrefabs[index];

                if (prefab == null)
                {
                    Debug.LogWarning($"[SaveLoadService] Pebble prefab at index {index} is null -- skipping.");
                    continue;
                }

                GameObject instance = Instantiate(prefab, _worldContainer);
                instance.transform.localPosition = pd.LocalPosition;
                instance.transform.localRotation = pd.LocalRotation;
                instance.transform.localScale    = pd.LocalScale;
                PlaceBlockAction.ArmForImmediate(instance);
            }
        }

        /// <summary>
        /// Publishes an <see cref="UndoPerformedEvent"/> so that
        /// <see cref="HarmonyService"/> does a full rescan via
        /// <c>RebuildCounters</c> + <c>Recalculate</c>.
        /// </summary>
        private void TriggerHarmonyRescan()
        {
            EventBus.Publish(new UndoPerformedEvent());
        }

        /// <summary>Creates the gardens directory if it does not exist.</summary>
        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_gardensPath))
                Directory.CreateDirectory(_gardensPath);
        }

        /// <summary>
        /// Strips invalid file-name characters and replaces spaces
        /// with underscores for a safe file name.
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "garden_" + DateTime.Now.Ticks;

            char[] invalid = Path.GetInvalidFileNameChars();
            string sanitized = name.Trim();

            foreach (char c in invalid)
                sanitized = sanitized.Replace(c, '_');

            sanitized = sanitized.Replace(' ', '_');
            return sanitized;
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_worldContainer == null)
                Debug.LogWarning("[SaveLoadService] _worldContainer is not assigned.", this);
            if (_blockDatabase == null)
                Debug.LogWarning("[SaveLoadService] _blockDatabase is not assigned.", this);
            if (_pebblePrefabs == null || _pebblePrefabs.Length == 0)
                Debug.LogWarning("[SaveLoadService] _pebblePrefabs is not assigned.", this);
        }

        #endregion
    }
}
