// ------------------------------------------------------------
//  IGridManager.cs  -  _Project.Scripts.Infrastructure
//  Contract for voxel grid configuration and snapping.
// ------------------------------------------------------------

using UnityEngine;

namespace _Project.Scripts.Infrastructure
{
    /// <summary>
    /// Central authority for the voxel grid: owns the grid size,
    /// provides canonical snapping, and controls the grid visual.
    /// </summary>
    public interface IGridManager
    {
        /// <summary>Size of one grid cell in world units.</summary>
        float GridSize { get; }

        /// <summary>Whether the grid visual is currently displayed.</summary>
        bool IsGridActive { get; }

        /// <summary>Snaps a raw local position to the centre of the enclosing grid cell.</summary>
        Vector3 GetSnappedPosition(Vector3 rawPosition);

        /// <summary>Activates the visual grid halo.</summary>
        void ActivateGrid(Transform cameraTransform);

        /// <summary>Deactivates the visual grid halo.</summary>
        void DeactivateGrid();
    }
}
