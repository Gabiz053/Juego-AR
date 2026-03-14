// ------------------------------------------------------------
//  EventBus.cs  -  _Project.Scripts.Infrastructure
//  Type-safe publish / subscribe hub using pure C# Actions.
//  Decouples systems that need to communicate without references.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Infrastructure
{
    /// <summary>
    /// Centralized, type-safe event bus built on <see cref="System.Action{T}"/>.<br/>
    /// Each event is a plain struct or class — the <b>type itself</b> is the channel key.
    /// This eliminates magic strings and gives full IntelliSense + compile-time safety.<br/><br/>
    /// <b>Define an event:</b>
    /// <code>
    /// public readonly struct BlockPlacedEvent
    /// {
    ///     public readonly Vector3Int Cell;
    ///     public readonly BlockType  Type;
    ///     public BlockPlacedEvent(Vector3Int cell, BlockType type)
    ///     { Cell = cell; Type = type; }
    /// }
    /// </code>
    /// <b>Subscribe:</b>
    /// <code>
    /// EventBus.Subscribe&lt;BlockPlacedEvent&gt;(OnBlockPlaced);
    /// </code>
    /// <b>Publish:</b>
    /// <code>
    /// EventBus.Publish(new BlockPlacedEvent(cell, BlockType.Sand));
    /// </code>
    /// <b>Unsubscribe (OnDisable / OnDestroy):</b>
    /// <code>
    /// EventBus.Unsubscribe&lt;BlockPlacedEvent&gt;(OnBlockPlaced);
    /// </code>
    /// </summary>
    public static class EventBus
    {
        #region State -------------------------------------------------

        /// <summary>
        /// Each event type maps to a single <see cref="Delegate"/> that is
        /// actually an <c>Action&lt;T&gt;</c>. Using <see cref="Delegate"/>
        /// as the value type allows a single dictionary for all event types.
        /// </summary>
        private static readonly Dictionary<Type, Delegate> _subscriptions =
            new Dictionary<Type, Delegate>();

        #endregion

        #region Public API --------------------------------------------

        /// <summary>
        /// Adds a listener for event type <typeparamref name="T"/>.
        /// Call in <c>OnEnable</c> and pair with <see cref="Unsubscribe{T}"/>
        /// in <c>OnDisable</c> to prevent leaks.
        /// </summary>
        /// <typeparam name="T">Event struct / class used as channel key.</typeparam>
        /// <param name="handler">Callback invoked when the event is published.</param>
        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            Type key = typeof(T);

            if (handler == null)
            {
                Debug.LogError($"[EventBus] Null handler passed to Subscribe<{key.Name}>.");
                return;
            }

            if (_subscriptions.TryGetValue(key, out Delegate existing))
            {
                _subscriptions[key] = Delegate.Combine(existing, handler);
            }
            else
            {
                _subscriptions[key] = handler;
            }
        }

        /// <summary>
        /// Removes a previously registered listener for event type <typeparamref name="T"/>.
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            Type key = typeof(T);

            if (!_subscriptions.TryGetValue(key, out Delegate existing)) return;

            Delegate updated = Delegate.Remove(existing, handler);

            if (updated == null)
            {
                _subscriptions.Remove(key);
            }
            else
            {
                _subscriptions[key] = updated;
            }
        }

        /// <summary>
        /// Fires an event to every subscriber of type <typeparamref name="T"/>.
        /// Invocation is synchronous — all handlers run before this method returns.<br/>
        /// Each handler is invoked in isolation: if one throws, the remaining
        /// subscribers still execute (exception is logged, not propagated).
        /// </summary>
        /// <typeparam name="T">Event struct / class.</typeparam>
        /// <param name="eventData">Payload delivered to every subscriber.</param>
        /// <remarks>
        /// <see cref="Delegate.GetInvocationList"/> allocates a small array per call.
        /// This is acceptable because game events fire at most once per user gesture
        /// (not per frame).  The safety guarantee outweighs the minor allocation.
        /// </remarks>
        public static void Publish<T>(T eventData) where T : struct
        {
            Type key = typeof(T);

            if (!_subscriptions.TryGetValue(key, out Delegate existing))
            {
#if UNITY_EDITOR
                Debug.Log($"[EventBus] Publish<{key.Name}> -- no subscribers.");
#endif
                return;
            }

            if (existing is Action<T> action)
            {
                foreach (Delegate d in action.GetInvocationList())
                {
                    try
                    {
                        ((Action<T>)d).Invoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(
                            $"[EventBus] Exception in {key.Name} handler " +
                            $"({d.Target?.GetType().Name ?? "static"}.{d.Method.Name}) -- {ex}");
                    }
                }
            }
            else
            {
                Debug.LogError($"[EventBus] Type mismatch for {key.Name} -- expected Action<{key.Name}>.");
            }
        }

        /// <summary>
        /// Removes all subscriptions for every event type.
        /// Call during scene-transition cleanup or test teardown.
        /// </summary>
        public static void Reset()
        {
            _subscriptions.Clear();
            Debug.Log("[EventBus] All subscriptions cleared.");
        }

        /// <summary>
        /// Returns <c>true</c> if at least one handler is subscribed
        /// for event type <typeparamref name="T"/>.
        /// </summary>
        public static bool HasSubscribers<T>() where T : struct
        {
            return _subscriptions.ContainsKey(typeof(T));
        }

        #endregion
    }
}
