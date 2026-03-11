// ------------------------------------------------------------
//  WorldModeSO.cs  -  _Project.Scripts.Core
//  ScriptableObject that stores the configuration for one
//  WorldMode.  Create three instances: Bonsai, Normal, Real.
// ------------------------------------------------------------

using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Data-only asset that describes how a particular <see cref="WorldMode"/>
    /// should configure the voxel world at runtime.<br/>
    /// Create one instance per mode via
    /// <b>Assets > Create > ARmonia > Core > World Mode Config</b>
    /// and fill in the Inspector fields.
    /// </summary>
    [CreateAssetMenu(
        fileName = "WorldModeConfig_New",
        menuName = "ARmonia/Core/World Mode Config")]
    public class WorldModeSO : ScriptableObject
    {
        #region Identity ------------------------------------------

        [Header("Identity")]
        [Tooltip("Which mode this asset represents.")]
        public WorldMode Mode;

        [Tooltip("Display name shown in UI (e.g. 'Bonsai', 'Normal', 'Real').")]
        public string DisplayName;

        #endregion

        #region World Scale ---------------------------------------

        [Header("World Scale")]
        [Tooltip("localScale applied to WorldContainer.\n"
               + "Bonsai  = 0.02  (2 cm per block)\n"
               + "Normal  = 0.10  (10 cm per block)\n"
               + "Real    = 1.00  (1 m per block, true Minecraft scale)")]
        public float WorldContainerScale = 0.1f;

        #endregion

        #region Anchor --------------------------------------------

        [Header("Anchor Type")]
        [Tooltip("Bonsai uses a tracked image as the anchor surface.\n"
               + "Normal and Real use AR ground planes.")]
        public AnchorType AnchorType = AnchorType.ARPlane;

        #endregion

        #region Bonsai Image Tracking -----------------------------

        [Header("Bonsai - Image Tracking")]
        [Tooltip("Reference image library used only in Bonsai mode.\n"
               + "Leave null for Normal and Real modes.")]
        public XRReferenceImageLibrary ImageLibrary;

        [Tooltip("Physical width of the target image in metres (default 0.20 = 20 cm).")]
        public float ImagePhysicalWidth = 0.20f;

        #endregion

        #region Block Constraints ---------------------------------

        [Header("Block Constraints")]
        [Tooltip("Maximum number of blocks allowed in this mode.\n"
               + "0 = unlimited.")]
        public int MaxBlocks;

        #endregion
    }

    /// <summary>How the world origin is attached to the real world.</summary>
    public enum AnchorType
    {
        /// <summary>Anchored to an AR ground plane (Normal / Real modes).</summary>
        ARPlane = 0,

        /// <summary>Anchored to a tracked image via ARTrackedImageManager (Bonsai mode).</summary>
        TrackedImage = 1
    }
}
