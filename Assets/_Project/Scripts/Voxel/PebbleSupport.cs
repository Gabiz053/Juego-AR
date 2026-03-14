// ------------------------------------------------------------
//  PebbleSupport.cs  -  _Project.Scripts.Voxel
//  Monitors pebble support and auto-breaks when unsupported.
// ------------------------------------------------------------

using UnityEngine;

namespace _Project.Scripts.Voxel
{
    /// <summary>
    /// Polls downward after the spawn animation to verify a voxel block
    /// still supports this pebble.  Pebbles on the AR ground plane never
    /// auto-break.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Voxel/Pebble Support")]
    public class PebbleSupport : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Support Check")]
        [Tooltip("How often (seconds) the support check runs once armed.")]
        [SerializeField] private float _checkInterval = 0.35f;

        [Tooltip("How far (metres) to cast downward when looking for support.")]
        [SerializeField] private float _checkDistance = 0.20f;

        #endregion

        #region State ---------------------------------------------

        private bool         _armed;
        private bool         _onARPlane;
        private LayerMask    _voxelMask;
        private Vector3      _supportDir = Vector3.down;
        private BlockDestroy _blockDestroy;

        #endregion

        #region Public API ----------------------------------------

        /// <summary>Configures the pebble for floor placement.</summary>
        public void Configure(bool onARPlane, LayerMask voxelMask)
        {
            _onARPlane  = onARPlane;
            _voxelMask  = voxelMask;
            _supportDir = Vector3.down;
        }

        /// <summary>Configures the pebble with a custom surface normal (wall mount).</summary>
        public void Configure(bool onARPlane, LayerMask voxelMask, Vector3 surfaceNormal)
        {
            _onARPlane  = onARPlane;
            _voxelMask  = voxelMask;
            _supportDir = -surfaceNormal.normalized;
        }

        /// <summary>
        /// Called once the spawn animation has settled.  Starts the support poll.
        /// </summary>
        public void Arm()
        {
            if (_armed) return;
            _armed = true;

            if (_onARPlane || _voxelMask == 0) return;

            InvokeRepeating(nameof(Poll), _checkInterval, _checkInterval);
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            _blockDestroy = GetComponent<BlockDestroy>();
        }

        private void Start()
        {
            ValidateReferences();
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Casts a ray in <c>_supportDir</c> to check for supporting
        /// geometry.  If nothing is hit, triggers <see cref="BlockDestroy.BreakFromTool"/>
        /// and disables further polling.
        /// </summary>
        private void Poll()
        {
            const float ORIGIN_LIFT = 0.05f;
            Vector3 origin = transform.position - _supportDir * ORIGIN_LIFT;

            if (Physics.Raycast(origin, _supportDir, _checkDistance + ORIGIN_LIFT, _voxelMask))
                return;

            if (_blockDestroy != null) _blockDestroy.BreakFromTool(-_supportDir);

            CancelInvoke(nameof(Poll));
            enabled = false;
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_blockDestroy == null)
                Debug.LogWarning("[PebbleSupport] _blockDestroy is not assigned.", this);
        }

        #endregion
    }
}
