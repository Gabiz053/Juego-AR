// ------------------------------------------------------------
//  WorldMode.cs  -  _Project.Scripts.Infrastructure
//  Enumerates the three garden scale modes.
// ------------------------------------------------------------

namespace _Project.Scripts.Infrastructure
{
    /// <summary>
    /// The three available garden scale modes, plus a sentinel for
    /// "not yet selected" used by <see cref="WorldModeContext"/>.<br/>
    /// <list type="bullet">
    /// <item><b>None</b>   - sentinel: no mode selected yet (default on cold start). <see cref="AR.WorldModeBootstrapper"/> applies its <c>_devOverrideMode</c> when it sees this value.</item>
    /// <item><b>Bonsai</b> - miniature garden anchored to a tracked image (20x20 cm).</item>
    /// <item><b>Normal</b> - tabletop scale anchored to an AR ground plane (scale 0.1).</item>
    /// <item><b>Real</b>   - full Minecraft scale anchored to an AR ground plane (scale 1.0).</item>
    /// </list>
    /// </summary>
    public enum WorldMode
    {
        None   = -1,
        Bonsai =  0,
        Normal =  1,
        Real   =  2
    }
}
