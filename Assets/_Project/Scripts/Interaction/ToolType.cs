// ──────────────────────────────────────────────
//  ToolType.cs  ·  _Project.Scripts.Interaction
//  Enumerates every tool/block slot in the player inventory.
// ──────────────────────────────────────────────

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// All available tools in the player's inventory toolbar.<br/>
    /// <c>Build_*</c> values (0–4) map 1:1 to <see cref="Voxel.BlockType"/>
    /// integer values, enabling direct cast conversion.<br/>
    /// <b>WARNING:</b> Do NOT change existing integer values — they are
    /// baked into UI button <c>OnClick</c> events in the scene.
    /// </summary>
    public enum ToolType
    {
        // ── Building Tools (match BlockType int values) ─────
        Build_Sand   = 0,
        Build_Glass  = 1,
        Build_Stone  = 2,
        Build_Wood   = 3,
        Build_Torch  = 4,
        Build_Grass  = 5,

        // ── Utility Tools ───────────────────────────────────
        Tool_None    = 6,
        Tool_Destroy = 7,
        Tool_Brush   = 8,
        Tool_Plow    = 9
    }
}
