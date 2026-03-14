// ------------------------------------------------------------
//  IHarmonyService.cs  -  _Project.Scripts.Infrastructure
//  Contract for the garden harmony evaluator (read-only + events).
// ------------------------------------------------------------

using System;

namespace _Project.Scripts.Infrastructure
{
    /// <summary>
    /// Read-only view of the harmony scoring system.
    /// Mutation happens internally via <see cref="EventBus"/> subscriptions --
    /// consumers only read the score and subscribe to change events.
    /// </summary>
    public interface IHarmonyService
    {
        /// <summary>Current harmony score [0, 1].</summary>
        float CurrentScore { get; }

        /// <summary>Fired every time the harmony score changes (0-1).</summary>
        event Action<float> OnHarmonyChanged;

        /// <summary>Fired once when the score first reaches 1.0.</summary>
        event Action OnPerfectHarmony;

        /// <summary>Fired when the world is fully reset.</summary>
        event Action OnWorldReset;
    }
}
