// ??????????????????????????????????????????????
//  HarmonyService.cs  ·  _Project.Scripts.Core
//  Passive garden evaluator — listens to placement/destroy events,
//  recalculates harmony, and broadcasts the result.
// ??????????????????????????????????????????????

using System;
using System.Collections.Generic;
using UnityEngine;
using _Project.Scripts.Voxel;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Evaluates the harmony of the AR garden and emits
    /// <see cref="OnHarmonyChanged"/> whenever the score changes.<br/>
    /// <br/>
    /// <b>Three pillars:</b><br/>
    /// • <b>Variety</b>   — how many distinct block types are present.<br/>
    /// • <b>Decoration</b>— how many pebbles have been placed.<br/>
    /// • <b>Quantity</b>  — rewards building toward a target block count.<br/>
    /// <br/>
    /// The service never polls <c>Update</c>. It recalculates only when the
    /// garden changes, driven entirely by events from <see cref="Interaction.ARBlockPlacer"/>,
    /// <see cref="Interaction.PlowTool"/>, and <see cref="UI.WorldResetService"/>.<br/>
    /// <br/>
    /// Wire <see cref="OnHarmonyChanged"/> ? <see cref="UI.HarmonyHUD.SetHarmony"/>
    /// in the Inspector or from code.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Core/Harmony Service")]
    public class HarmonyService : MonoBehaviour
    {
        #region Inspector ?????????????????????????????????????

        [Tooltip("Scoring weights and thresholds. Create via Assets ? Create ? ARmonia ? Core ? Harmony Config.")]
        [SerializeField] private HarmonyConfig _config;

        [Tooltip("WorldContainer transform — children with VoxelBlock components are counted on each recalculation.")]
        [SerializeField] private Transform _worldContainer;

        #endregion

        #region Events ????????????????????????????????????????

        /// <summary>
        /// Fired every time the harmony score changes (value in [0, 1]).<br/>
        /// Wire this to <see cref="UI.HarmonyHUD.SetHarmony"/>.
        /// </summary>
        public event Action<float> OnHarmonyChanged;

        /// <summary>
        /// Fired exactly once when the score first reaches 1.0, then
        /// suppressed for <see cref="HarmonyConfig.perfectCooldown"/> seconds.
        /// </summary>
        public event Action OnPerfectHarmony;

        /// <summary>Fired when the world is fully reset.</summary>
        public event Action OnWorldReset;

        #endregion

        #region State ?????????????????????????????????????????

        // Mutable counters — updated by event callbacks, not by scanning children.
        private readonly Dictionary<BlockType, int> _blockCounts = new Dictionary<BlockType, int>();
        private int   _totalBlocks;
        private int   _totalPebbles;
        private float _lastScore       = -1f;
        private bool  _perfectFired    = false;   // fires only once per session

        /// <summary>Current harmony score [0, 1]. Read-only outside this class.</summary>
        public float CurrentScore => _lastScore < 0f ? 0f : _lastScore;

        #endregion

        #region Unity Lifecycle ????????????????????????????????

        private void Start()
        {
            if (_config == null)
                Debug.LogError("[HarmonyService] _config is not assigned!", this);
            if (_worldContainer == null)
                Debug.LogError("[HarmonyService] _worldContainer is not assigned!", this);

            // Bootstrap counters from any blocks already present in the scene
            // (e.g. placed before this service initialised).
            RebuildCounters();
            Recalculate();
        }

        #endregion

        #region Public API — called by ARBlockPlacer / PlowTool ??

        /// <summary>Call after a voxel block is successfully placed.</summary>
        public void NotifyBlockPlaced(BlockType type)
        {
            _blockCounts.TryGetValue(type, out int current);
            _blockCounts[type] = current + 1;
            _totalBlocks++;
            Recalculate();
        }

        /// <summary>Call after a voxel block is successfully destroyed.</summary>
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

        /// <summary>Call after a pebble is successfully placed.</summary>
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

        /// <summary>
        /// Full world reset — clears all counters and resets the score to zero.
        /// </summary>
        public void NotifyWorldReset()
        {
            _blockCounts.Clear();
            _totalBlocks  = 0;
            _totalPebbles = 0;
            _lastScore    = -1f;
            _perfectFired = false;
            OnWorldReset?.Invoke();
            Recalculate();
        }

        /// <summary>
        /// Full rescan of WorldContainer children — use after undo/redo operations
        /// where multiple blocks may appear or disappear at once.
        /// </summary>
        public void NotifyUndoRedo()
        {
            RebuildCounters();
            Recalculate();
        }

        #endregion

        #region Scoring ???????????????????????????????????????

        private void Recalculate()
        {
            if (_config == null) return;

            float variety    = ScoreVariety();
            float decoration = ScoreDecoration();
            float quantity   = ScoreQuantity();

            float raw  = variety    * _config.varietyWeight
                       + decoration * _config.decorationWeight
                       + quantity   * _config.quantityWeight;

            float gate  = ScoreMinimumGate();
            // Snap raw to 1 when all pillars are fully satisfied (float precision).
            float score = Mathf.Clamp01(raw > 0.999f && gate >= 1f ? 1f : raw * gate);

            // Threshold: treat anything within 0.005 as unchanged to avoid jitter.
            if (Mathf.Abs(score - _lastScore) < 0.005f) return;

            float previous = _lastScore;
            _lastScore     = score;
            OnHarmonyChanged?.Invoke(score);

            if (score >= 1f && !_perfectFired)
            {
                _perfectFired = true;
                OnPerfectHarmony?.Invoke();
                Debug.Log("[HarmonyService] Perfect harmony achieved!");
            }

            Debug.Log($"[HarmonyService] Score={score:P0}  " +
                      $"(V={variety:F2} D={decoration:F2} Q={quantity:F2} Gate={gate:F2})");
        }

        // ?? Pillar 1: Variety ??????????????????????????????
        // Score = distinct types present / fullVarietyTypeCount
        // 0 types ? 0,  4 types ? 1.0
        private float ScoreVariety()
        {
            int distinct = _blockCounts.Count;
            if (distinct == 0) return 0f;
            return Mathf.Clamp01((float)distinct / _config.fullVarietyTypeCount);
        }

        // ?? Pillar 2: Decoration ???????????????????????????
        // Pebbles are independent from blocks — they have their own target.
        // We only gate on _totalBlocks > 0 so an empty garden doesn't get
        // free pebble points before any construction has started.
        private float ScoreDecoration()
        {
            if (_totalBlocks == 0) return 0f;
            return Mathf.Clamp01((float)_totalPebbles / _config.targetPebbleCount);
        }

        // ?? Pillar 3: Quantity ?????????????????????????????
        // Pebbles do NOT count — only VoxelBlock instances count here.
        private float ScoreQuantity()
        {
            return Mathf.Clamp01((float)_totalBlocks / _config.targetBlockCount);
        }

        // ?? Minimums gate ??????????????????????????????????
        // Returns a multiplier in [1-gateStrength .. 1].
        // Each unmet minimum contributes half the gate penalty.
        // Both met ? 1.0 (no penalty, score can reach 1.0).
        // Neither met ? (1 - gateStrength).
        private float ScoreMinimumGate()
        {
            float penalty = 0f;
            float half    = _config.gateStrength * 0.5f;

            // Sand gate — partial credit as blocks are added.
            _blockCounts.TryGetValue(BlockType.Sand, out int sandCount);
            if (sandCount < _config.minSandBlocks)
                penalty += half * (1f - (float)sandCount / _config.minSandBlocks);

            // Grass gate.
            _blockCounts.TryGetValue(BlockType.Grass, out int grassCount);
            if (grassCount < _config.minGrassBlocks)
                penalty += half * (1f - (float)grassCount / _config.minGrassBlocks);

            // Clamp to avoid floating point making gate slightly below 1
            // when minimums are exactly met.
            float gate = 1f - penalty;
            return gate > 0.999f ? 1f : gate;
        }

        #endregion

        #region Helpers ???????????????????????????????????????

        /// <summary>
        /// Scans WorldContainer children to rebuild all counters from scratch.
        /// O(n) over children — called only at Start and after undo/redo.
        /// </summary>
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

                // Pebbles don't have VoxelBlock — identify by ProceduralPebble tag or component.
                if (child.GetComponent<ProceduralPebble>() != null)
                    _totalPebbles++;
            }
        }

        #endregion

        #region Validation ????????????????????????????????????

        private void OnValidate()
        {
            if (_config != null)
            {
                float sum = _config.varietyWeight + _config.decorationWeight + _config.quantityWeight;
                if (!Mathf.Approximately(sum, 1f))
                    Debug.LogWarning($"[HarmonyService] Pillar weights sum to {sum:F2} — should be 1.0.", this);
            }
        }

        #endregion
    }
}
