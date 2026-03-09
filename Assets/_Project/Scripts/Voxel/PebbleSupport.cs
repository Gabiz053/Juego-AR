// ??????????????????????????????????????????????
//  PebbleSupport.cs  ·  _Project.Scripts.Voxel
//  Monitors whether a pebble still has a voxel block beneath it.
//  If the supporting block is destroyed the pebble breaks itself.
// ??????????????????????????????????????????????

using UnityEngine;

namespace _Project.Scripts.Voxel
{
    /// <summary>
    /// Attached alongside <see cref="ProceduralPebble"/> and <see cref="BlockDestroy"/>.<br/>
    /// After the spawn animation completes (<see cref="Arm"/> is called by
    /// <see cref="Interaction.PlowTool"/>), begins polling downward to check whether
    /// a voxel block still supports this pebble.<br/>
    /// <br/>
    /// Rules:<br/>
    /// • <b>AR plane</b> — never breaks on its own (ground is permanent).<br/>
    /// • <b>Voxel block</b> — breaks if no voxel collider is found directly below.<br/>
    /// • Poll only starts after <see cref="Arm"/> — the spawn animation is immune.<br/>
    /// • Uses a provided <see cref="LayerMask"/> so other pebbles are never counted
    ///   as valid support.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Voxel/Pebble Support")]
    public class PebbleSupport : MonoBehaviour
    {
        #region Inspector ?????????????????????????????????????

        [Tooltip("How often (seconds) the support check runs once armed.")]
        [SerializeField] private float _checkInterval = 0.35f;

        [Tooltip("How far (metres) to cast downward when looking for support.")]
        [SerializeField] private float _checkDistance = 0.20f;

        #endregion

        #region State ?????????????????????????????????????????

        private bool      _armed;       // true once spawn animation has finished
        private bool      _onARPlane;   // if true, never auto-breaks
        private LayerMask _voxelMask;   // only voxel blocks count as support
        private Vector3   _supportDir = Vector3.down;  // direction used when raycasting for support

        #endregion

        #region Public API ????????????????????????????????????

        /// <summary>
        /// Called by <see cref="Interaction.PlowTool"/> right after instantiation,
        /// before the spawn animation starts.
        /// </summary>
        /// <param name="onARPlane">True if placed on the AR ground plane — disables all auto-break.</param>
        /// <param name="voxelMask">Layer mask that includes only voxel blocks.</param>
        public void Configure(bool onARPlane, LayerMask voxelMask)
        {
            _onARPlane   = onARPlane;
            _voxelMask   = voxelMask;
            // Default support direction: straight down (floor placement).
            _supportDir  = Vector3.down;
        }

        /// <summary>
        /// Overload that also receives the surface normal so wall-mounted pebbles
        /// cast their support ray into the wall instead of toward the floor.
        /// </summary>
        public void Configure(bool onARPlane, LayerMask voxelMask, Vector3 surfaceNormal)
        {
            _onARPlane  = onARPlane;
            _voxelMask  = voxelMask;
            // Invert the surface normal ? direction into the supporting surface.
            _supportDir = -surfaceNormal.normalized;
        }

        /// <summary>
        /// Called once the spawn animation has settled. Starts the support poll.
        /// </summary>
        public void Arm()
        {
            if (_armed) return;
            _armed = true;

            // Ground pebbles never need checking.
            if (_onARPlane) return;

            // No voxel mask means we cannot reliably detect support — skip.
            if (_voxelMask == 0) return;

            InvokeRepeating(nameof(Poll), _checkInterval, _checkInterval);
        }

        #endregion

        #region Poll ??????????????????????????????????????????

        private void Poll()
        {
            // Offset origin away from the surface so the ray never starts inside
            // the supporting collider (Physics.Raycast ignores the origin collider).
            const float originLift = 0.05f;
            Vector3 origin = transform.position - _supportDir * originLift;

            if (Physics.Raycast(origin, _supportDir, _checkDistance + originLift, _voxelMask))
                return;   // support found — all good

            BlockDestroy bd = GetComponent<BlockDestroy>();
            if (bd != null) bd.BreakFromTool(-_supportDir);

            CancelInvoke(nameof(Poll));
            enabled = false;
        }

        #endregion
    }
}
