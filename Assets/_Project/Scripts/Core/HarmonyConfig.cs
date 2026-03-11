// ------------------------------------------------------------
//  HarmonyConfig.cs  -  _Project.Scripts.Core
//  ScriptableObject with all harmony scoring weights and
//  thresholds.  Tweak from the Inspector without touching code.
// ------------------------------------------------------------

using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Holds every tunable constant used by <see cref="HarmonyService"/>.<br/>
    /// Create one via <c>Assets > Create > ARmonia > Core > Harmony Config</c>.
    /// </summary>
    [CreateAssetMenu(
        fileName = "HarmonyConfig",
        menuName = "ARmonia/Core/Harmony Config",
        order    = 1)]
    public class HarmonyConfig : ScriptableObject
    {
        #region Pillar Weights ------------------------------------

        [Header("Pillar Weights (must sum to 1)")]
        [Tooltip("Weight of the block-variety score.")]
        [Range(0f, 1f)] public float varietyWeight = 0.45f;

        [Tooltip("Weight of the decoration (pebble) score.")]
        [Range(0f, 1f)] public float decorationWeight = 0.35f;

        [Tooltip("Weight of the block-quantity score (rewards building more).")]
        [Range(0f, 1f)] public float quantityWeight = 0.20f;

        #endregion

        #region Variety -------------------------------------------

        [Header("Variety")]
        [Tooltip("Distinct block types needed to reach full variety score.\n"
               + "There are 6 types available -- set to 6 to require them all.")]
        [Min(1)] public int fullVarietyTypeCount = 6;

        #endregion

        #region Quantity ------------------------------------------

        [Header("Quantity")]
        [Tooltip("Blocks needed for full quantity score.")]
        [Min(1)] public int targetBlockCount = 50;

        #endregion

        #region Decoration ----------------------------------------

        [Header("Decoration")]
        [Tooltip("Pebbles needed for full decoration score.")]
        [Min(1)] public int targetPebbleCount = 25;

        #endregion

        #region Minimums Gate -------------------------------------

        [Header("Minimums Gate")]
        [Tooltip("Minimum number of Sand blocks required.\n"
               + "Until met, the final score is scaled down proportionally.")]
        [Min(0)] public int minSandBlocks = 10;

        [Tooltip("Minimum number of Grass blocks required.\n"
               + "Until met, the final score is scaled down proportionally.")]
        [Min(0)] public int minGrassBlocks = 10;

        [Tooltip("How much the gate suppresses the score when minimums are not met.\n"
               + "1.0 = score is completely blocked until minimums are met.\n"
               + "0.5 = score can reach up to 50% without meeting minimums.")]
        [Range(0f, 1f)] public float gateStrength = 0.85f;

        #endregion
    }
}
