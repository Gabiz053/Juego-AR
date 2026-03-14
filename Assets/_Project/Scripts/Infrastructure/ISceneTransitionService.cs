// ------------------------------------------------------------
//  ISceneTransitionService.cs  -  _Project.Scripts.Infrastructure
//  Contract for fade-to-black scene transitions.
// ------------------------------------------------------------

namespace _Project.Scripts.Infrastructure
{
    /// <summary>
    /// Performs a smooth fade-to-black transition between scenes.
    /// The implementation persists across scene loads via
    /// <c>DontDestroyOnLoad</c>.
    /// </summary>
    public interface ISceneTransitionService
    {
        /// <summary>Returns true while a transition is in progress.</summary>
        bool IsTransitioning { get; }

        /// <summary>Starts a fade-to-black transition to the given scene.</summary>
        void TransitionTo(string sceneName);
    }
}
