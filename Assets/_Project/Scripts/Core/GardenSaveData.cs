// ------------------------------------------------------------
//  GardenSaveData.cs  -  _Project.Scripts.Core
//  Serializable data structures for garden save/load system.
// ------------------------------------------------------------

using System;
using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Serializable snapshot of a single voxel block.
    /// Stores the block type and its local position relative to
    /// <c>WorldContainer</c>.  Rotation is always identity for voxels.
    /// </summary>
    [Serializable]
    public struct VoxelSaveData
    {
        /// <summary>Integer value of <see cref="Infrastructure.BlockType"/>.</summary>
        public int blockType;

        /// <summary>Local X position relative to WorldContainer.</summary>
        public float posX;

        /// <summary>Local Y position relative to WorldContainer.</summary>
        public float posY;

        /// <summary>Local Z position relative to WorldContainer.</summary>
        public float posZ;

        /// <summary>Creates a snapshot from raw values.</summary>
        public VoxelSaveData(int blockType, Vector3 localPosition)
        {
            this.blockType = blockType;
            posX = localPosition.x;
            posY = localPosition.y;
            posZ = localPosition.z;
        }

        /// <summary>Reconstructs the local position as a <see cref="Vector3"/>.</summary>
        public Vector3 LocalPosition => new Vector3(posX, posY, posZ);
    }

    /// <summary>
    /// Serializable snapshot of a single procedural pebble.
    /// Stores prefab index, local transform relative to
    /// <c>WorldContainer</c>.  The pebble mesh shape is regenerated
    /// randomly on load (seed is not preserved).
    /// </summary>
    [Serializable]
    public struct PebbleSaveData
    {
        /// <summary>Index into the pebble prefabs array.</summary>
        public int prefabIndex;

        // -- Position --
        public float posX;
        public float posY;
        public float posZ;

        // -- Rotation (quaternion) --
        public float rotX;
        public float rotY;
        public float rotZ;
        public float rotW;

        // -- Scale --
        public float scaleX;
        public float scaleY;
        public float scaleZ;

        /// <summary>Creates a snapshot from raw values.</summary>
        public PebbleSaveData(
            int        prefabIndex,
            Vector3    localPosition,
            Quaternion localRotation,
            Vector3    localScale)
        {
            this.prefabIndex = prefabIndex;
            posX   = localPosition.x;
            posY   = localPosition.y;
            posZ   = localPosition.z;
            rotX   = localRotation.x;
            rotY   = localRotation.y;
            rotZ   = localRotation.z;
            rotW   = localRotation.w;
            scaleX = localScale.x;
            scaleY = localScale.y;
            scaleZ = localScale.z;
        }

        /// <summary>Reconstructs the local position.</summary>
        public Vector3 LocalPosition => new Vector3(posX, posY, posZ);

        /// <summary>Reconstructs the local rotation.</summary>
        public Quaternion LocalRotation => new Quaternion(rotX, rotY, rotZ, rotW);

        /// <summary>Reconstructs the local scale.</summary>
        public Vector3 LocalScale => new Vector3(scaleX, scaleY, scaleZ);
    }

    /// <summary>
    /// Top-level container for a complete garden snapshot.
    /// Serialized to JSON via <see cref="JsonUtility"/>.
    /// </summary>
    [Serializable]
    public class GardenSaveData
    {
        /// <summary>User-supplied name for this garden.</summary>
        public string gardenName;

        /// <summary>ISO-8601 timestamp of when the garden was saved.</summary>
        public string createdAt;

        /// <summary>All voxel blocks in the garden.</summary>
        public VoxelSaveData[] voxels;

        /// <summary>All procedural pebbles in the garden.</summary>
        public PebbleSaveData[] pebbles;
    }
}
