// ------------------------------------------------------------
//  IToolManager.cs  -  _Project.Scripts.Infrastructure
//  Contract for tool / block selection and prefab lookup.
// ------------------------------------------------------------

using System;
using UnityEngine;

namespace _Project.Scripts.Infrastructure
{
    /// <summary>
    /// Tracks the player's currently selected tool and provides
    /// the corresponding block prefab via <see cref="GetBlockPrefab"/>.
    /// </summary>
    public interface IToolManager
    {
        /// <summary>The tool currently selected by the player.</summary>
        ToolType CurrentTool { get; }

        /// <summary><c>true</c> when the current tool is a block-building tool.</summary>
        bool IsBuildTool { get; }

        /// <summary>Selects a tool by its integer index.</summary>
        void SelectToolByIndex(int index);

        /// <summary>Returns the prefab for the current build tool, or null.</summary>
        GameObject GetCurrentBlockPrefab();

        /// <summary>Returns the prefab for a specific <see cref="BlockType"/>.</summary>
        GameObject GetBlockPrefab(BlockType blockType);

        /// <summary>Raised whenever the selected tool changes.</summary>
        event Action<ToolType> OnToolChanged;
    }
}
