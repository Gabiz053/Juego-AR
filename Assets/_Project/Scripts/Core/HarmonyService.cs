// ------------------------------------------------------------
//  HarmonyService.cs  -  _Project.Scripts.Core
//  Passive garden evaluator -- recalculates harmony on events
//  from placement / destroy systems, never polls Update.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using _Project.Scripts.Voxel;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Evaluates the harmony of the AR garden and emits
    /// <see cref="OnHarmonyChanged"/> whenever the score changes.<br/>
    /// Three pillars: <b>Variety</b>, <b>Decoration</b>, <b>Quantity</b>.<br/>
    /// Driven entirely by events -- no <c>Update</c> polling.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Core/Harmony Service")]
    public class HarmonyService : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Config")]
        [Tooltip("Scoring weights and thresholds.")]
        [SerializeField] private HarmonyConfig _config;

        [Header("World")]
        [Tooltip("WorldContainer -- children with VoxelBlock are counted.")]
        [SerializeField] private Transform _worldContainer;

        #endregion

        #region Events --------------------------------------------

        /// <summary>Fired every time the harmony score changes (0-1).</summary>
        public event Action<float> OnHarmonyChanged;

        /// <summary>Fired once when the score first reaches 1.0.</summary>
        public event Action OnPerfectHarmony;

        /// <summary>Fired when the world is fully reset.</summary>
        public event Action OnWorldReset;

        #endregion

        #region State ---------------------------------------------

        private readonly Dictionary<BlockType, int> _blockCounts = new Dictionary<BlockType, int>();
        private int   _totalBlocks;
        private int   _totalPebbles;
        private float _lastScore    = -1f;
        private bool  _perfectFired;

        /// <summary>Current harmony score [0, 1].</summary>
        public float CurrentScore => _lastScore < 0f ? 0f : _lastScore;

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Start()
        {
            ValidateReferences();
            RebuildCounters();
            Recalculate();
            Debug.Log($"[HarmonyService] Initialized -- score: {_lastScore:F2}, blocks: {_totalBlocks}, pebbles: {_totalPebbles}.");
        }

        #endregion

        #region Public API ----------------------------------------

        /// <summary>Call after a voxel block is placed.</summary>
        public void NotifyBlockPlaced(BlockType type)
        {
            _blockCounts.TryGetValue(type, out int current);
            _blockCounts[type] = current + 1;
            _totalBlocks++;
            Recalculate();
        }

        /// <summary>Call after a voxel block is destroyed.</summary>
        public void NotifyBlockDestroyed(BlockType type)
        {
            if (_blockCounts.TryGetValue(type, out int current) && current > 0)
            {
                _blockCounts[type] = current - 1;
                if (_blockCounts[type] == 0)
                    _blockCounts.Remove(type);
            }
            _totalBlocks = Mathf.Max(0, _totalBlocks - 1);
            Recalculate();
        }

        /// <summary>Call after a pebble is placed.</summary>
        public void NotifyPebblePlaced()
        {
            _totalPebbles++;
            Recalculate();
        }

        /// <summary>Call after a pebble is destroyed.</summary>
        public void NotifyPebbleDestroyed()
        {
            _totalPebbles = Mathf.Max(0, _totalPebbles - 1);
            Recalculate();
        }

        /// <summary>Full world reset -- clears counters and score.</summary>
        public void NotifyWorldReset()
        {
            _blockCounts.Clear();
            _totalBlocks  = 0;
            _totalPebbles = 0;
            _lastScore    = -1f;
            _perfectFired = false;
            OnWorldReset?.Invoke();
            Recalculate();
            Debug.Log("[HarmonyService] World reset -- all counters cleared.");
        }

        /// <summary>Full rescan after undo / redo operations.</summary>
        public void NotifyUndoRedo()
        {
            RebuildCounters();
            Recalculate();
            Debug.Log($"[HarmonyService] Undo/Redo rescan -- blocks: {_totalBlocks}, pebbles: {_totalPebbles}, score: {_lastScore:F2}.");
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Combines the three pillar scores, applies the minimums gate,
        /// fires <see cref="OnHarmonyChanged"/> and checks for perfect harmony.
        /// </summary>
        private void Recalculate()
        {
            if (_config == null) return;

            float variety    = ScoreVariety();
            float decoration = ScoreDecoration();
            float quantity   = ScoreQuantity();

            float raw = variety    * _config.varietyWeight
                      + decoration * _config.decorationWeight
                      + quantity   * _config.quantityWeight;

            float gate  = ScoreMinimumGate();
            float score = Mathf.Clamp01(raw > 0.999f && gate >= 1f ? 1f : raw * gate);

            if (Mathf.Abs(score - _lastScore) < 0.005f) return;

            _lastScore = score;
            OnHarmonyChanged?.Invoke(score);
            Debug.Log($"[HarmonyService] Score: {score:F2} (var={variety:F2} dec={decoration:F2} qty={quantity:F2} gate={gate:F2}).");

            if (score >= 1f && !_perfectFired)
            {
                _perfectFired = true;
                OnPerfectHarmony?.Invoke();
                Debug.Log("[HarmonyService] *** PERFECT HARMONY REACHED ***");
            }
        }

        /// <summary>Ratio of distinct block types used to the target count.</summary>
        private float ScoreVariety()
        {
            int distinct = _blockCounts.Count;
            return distinct == 0 ? 0f : Mathf.Clamp01((float)distinct / _config.fullVarietyTypeCount);
        }

        /// <summary>Ratio of pebbles placed to <see cref="HarmonyConfig.targetPebbleCount"/>.</summary>
        private float ScoreDecoration()
        {
            if (_totalBlocks == 0) return 0f;
            return Mathf.Clamp01((float)_totalPebbles / _config.targetPebbleCount);
        }

        /// <summary>Ratio of total blocks to <see cref="HarmonyConfig.targetBlockCount"/>.</summary>
        private float ScoreQuantity()
        {
            return Mathf.Clamp01((float)_totalBlocks / _config.targetBlockCount);
        }

        /// <summary>
        /// Returns a 0-1 multiplier that penalises the score when the
        /// mandatory minimums (Sand/Grass) are not yet met.
        /// </summary>
        private float ScoreMinimumGate()
        {
            float penalty = 0f;
            float half    = _config.gateStrength * 0.5f;

            _blockCounts.TryGetValue(BlockType.Sand, out int sandCount);
            if (sandCount < _config.minSandBlocks)
                penalty += half * (1f - (float)sandCount / _config.minSandBlocks);

            _blockCounts.TryGetValue(BlockType.Grass, out int grassCount);
            if (grassCount < _config.minGrassBlocks)
                penalty += half * (1f - (float)grassCount / _config.minGrassBlocks);

            float gate = 1f - penalty;
            return gate > 0.999f ? 1f : gate;
        }

        /// <summary>Scans WorldContainer children to rebuild all counters.</summary>
        private void RebuildCounters()
        {
            _blockCounts.Clear();
            _totalBlocks  = 0;
            _totalPebbles = 0;

            if (_worldContainer == null) return;

            foreach (Transform child in _worldContainer)
            {
                VoxelBlock vb = child.GetComponent<VoxelBlock>();
                if (vb != null)
                {
                    _blockCounts.TryGetValue(vb.Type, out int c);
                    _blockCounts[vb.Type] = c + 1;
                    _totalBlocks++;
                    continue;
                }

                if (child.GetComponent<ProceduralPebble>() != null)
                    _totalPebbles++;
            }
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_config == null)
                Debug.LogError("[HarmonyService] _config is not assigned!", this);
            if (_worldContainer == null)
                Debug.LogError("[HarmonyService] _worldContainer is not assigned!", this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_config == null) return;
            float sum = _config.varietyWeight + _config.decorationWeight + _config.quantityWeight;
            if (!Mathf.Approximately(sum, 1f))
                Debug.LogWarning($"[HarmonyService] Pillar weights sum to {sum:F2} -- should be 1.0.", this);
        }
#endif

        #endregion
    }
}
