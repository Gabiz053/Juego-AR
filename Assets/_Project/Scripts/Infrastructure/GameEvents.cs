// ------------------------------------------------------------
//  GameEvents.cs  -  _Project.Scripts.Infrastructure
//  Struct definitions for every event published through EventBus.
//  Using readonly structs avoids heap allocation per publish call.
// ------------------------------------------------------------

using UnityEngine;

namespace _Project.Scripts.Infrastructure
{
    // -- Block Events -----------------------------------------------

    /// <summary>
    /// Fired after a voxel block is successfully placed on the grid.
    /// </summary>
    public readonly struct BlockPlacedEvent
    {
        public readonly Vector3Int Cell;
        public readonly BlockType  Type;

        public BlockPlacedEvent(Vector3Int cell, BlockType type)
        {
            Cell = cell;
            Type = type;
        }
    }

    /// <summary>
    /// Fired after a voxel block is destroyed and removed from the grid.
    /// </summary>
    public readonly struct BlockDestroyedEvent
    {
        public readonly Vector3Int Cell;
        public readonly BlockType  Type;

        public BlockDestroyedEvent(Vector3Int cell, BlockType type)
        {
            Cell = cell;
            Type = type;
        }
    }

    // -- Pebble Events ----------------------------------------------

    /// <summary>
    /// Fired after a decorative pebble is placed.
    /// </summary>
    public readonly struct PebblePlacedEvent { }

    /// <summary>
    /// Fired after a decorative pebble is destroyed.
    /// </summary>
    public readonly struct PebbleDestroyedEvent { }

    // -- Tool Events ------------------------------------------------

    /// <summary>
    /// Fired when the active tool changes (build, destroy, brush, plow).
    /// </summary>
    public readonly struct ToolChangedEvent
    {
        public readonly ToolType PreviousTool;
        public readonly ToolType CurrentTool;

        public ToolChangedEvent(ToolType previousTool, ToolType currentTool)
        {
            PreviousTool = previousTool;
            CurrentTool  = currentTool;
        }
    }

    // -- Harmony Events ---------------------------------------------

    /// <summary>
    /// Fired when the harmony score is recalculated.
    /// </summary>
    public readonly struct HarmonyChangedEvent
    {
        public readonly float NormalizedScore;
        public readonly bool  IsPerfect;

        public HarmonyChangedEvent(float normalizedScore, bool isPerfect)
        {
            NormalizedScore = normalizedScore;
            IsPerfect       = isPerfect;
        }
    }

    // -- World Events -----------------------------------------------

    /// <summary>
    /// Fired when the world grid is fully cleared (reset).
    /// </summary>
    public readonly struct WorldResetEvent { }

    // -- Undo / Redo Events -----------------------------------------

    /// <summary>
    /// Fired after an undo operation completes.
    /// </summary>
    public readonly struct UndoPerformedEvent { }

    /// <summary>
    /// Fired after a redo operation completes.
    /// </summary>
    public readonly struct RedoPerformedEvent { }
}
