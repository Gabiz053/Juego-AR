// ------------------------------------------------------------
//  WorldModeContext.cs  -  _Project.Scripts.Infrastructure
//  Static cross-scene channel that carries the player's mode
//  choice from the title screen into the game scene.
// ------------------------------------------------------------

namespace _Project.Scripts.Infrastructure
{
    /// <summary>
    /// Lightweight static context that stores the <see cref="WorldMode"/>
    /// selected by the player before the game scene loads.<br/>
    /// <br/>
    /// <b>Title screen</b> writes:
    /// <code>WorldModeContext.Selected = WorldMode.Bonsai;</code>
    /// <b>Game scene</b> reads via <see cref="AR.WorldModeBootstrapper"/>.<br/>
    /// <br/>
    /// No MonoBehaviour, no DontDestroyOnLoad, no scene dependency --
    /// just a static field that survives domain reloads in play mode.
    /// </summary>
    public static class WorldModeContext
    {
        /// <summary>
        /// The mode the player chose on the title screen.<br/>
        /// Defaults to <see cref="WorldMode.None"/> on cold start so
        /// <see cref="AR.WorldModeBootstrapper"/> can detect that no title
        /// screen selection was made and apply its <c>_devOverrideMode</c>.
        /// </summary>
        public static WorldMode Selected { get; set; } = WorldMode.None;
    }
}
