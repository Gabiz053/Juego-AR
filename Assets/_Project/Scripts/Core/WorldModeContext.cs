// ------------------------------------------------------------
//  WorldModeContext.cs  -  _Project.Scripts.Core
//  Static cross-scene channel that carries the player's mode
//  choice from the title screen into the game scene.
// ------------------------------------------------------------

namespace _Project.Scripts.Core
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
    /// When the title screen is implemented it sets this before
    /// <c>SceneManager.LoadScene</c>; until then the value is
    /// overridden by the Inspector field in <see cref="AR.WorldModeBootstrapper"/>.
    /// </summary>
    public static class WorldModeContext
    {
        /// <summary>
        /// The mode the player chose on the title screen.<br/>
        /// Defaults to <see cref="WorldMode.Normal"/> so the game is
        /// always playable without a title screen.
        /// </summary>
        public static WorldMode Selected { get; set; } = WorldMode.Normal;
    }
}
