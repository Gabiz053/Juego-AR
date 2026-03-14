// ------------------------------------------------------------
//  ServiceLocator.cs  -  _Project.Scripts.Infrastructure
//  Central registry for resolving service interfaces at runtime.
//  Replaces scattered singletons and FindObjectOfType calls.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Infrastructure
{
    /// <summary>
    /// Generic Service Locator that registers and resolves services by interface.
    /// Services register themselves during <c>Awake</c> and unregister in <c>OnDestroy</c>.
    /// Consumers call <see cref="Get{T}"/> to obtain a reference without coupling
    /// to the concrete implementation.<br/><br/>
    /// <b>Threading:</b> This class is designed for Unity's single-threaded
    /// main-thread model.  Do not call from background threads or async
    /// continuations that may run off the main thread.<br/><br/>
    /// <b>Usage — provider side:</b>
    /// <code>
    /// ServiceLocator.Register&lt;IAudioService&gt;(this);
    /// </code>
    /// <b>Usage — consumer side:</b>
    /// <code>
    /// var audio = ServiceLocator.Get&lt;IAudioService&gt;();
    /// </code>
    /// </summary>
    public static class ServiceLocator
    {
        #region State -------------------------------------------------

        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        #endregion

        #region Public API --------------------------------------------

        /// <summary>
        /// Registers a service instance under the specified interface type.
        /// Logs a warning if a service of the same type is already registered
        /// and replaces it — last-write-wins semantics.
        /// </summary>
        /// <typeparam name="T">Interface or base type used as the lookup key.</typeparam>
        /// <param name="service">Concrete instance that implements <typeparamref name="T"/>.</param>
        public static void Register<T>(T service) where T : class
        {
            Type key = typeof(T);

            if (service == null)
            {
                Debug.LogError($"[ServiceLocator] Attempted to register null for {key.Name}.");
                return;
            }

            if (_services.ContainsKey(key))
            {
                Debug.LogWarning($"[ServiceLocator] Overwriting existing registration for {key.Name}.");
            }

            _services[key] = service;
            Debug.Log($"[ServiceLocator] Registered -- {key.Name}.");
        }

        /// <summary>
        /// Resolves a previously registered service by interface type.
        /// Returns <c>null</c> and logs an error if no service is found.
        /// </summary>
        /// <typeparam name="T">Interface or base type to resolve.</typeparam>
        /// <returns>The registered instance, or <c>null</c> if not found.</returns>
        public static T Get<T>() where T : class
        {
            Type key = typeof(T);

            if (_services.TryGetValue(key, out object service))
            {
                return service as T;
            }

            Debug.LogError($"[ServiceLocator] Service not found -- {key.Name}. Did you forget to Register?");
            return null;
        }

        /// <summary>
        /// Attempts to resolve a service without logging an error on failure.
        /// Useful when a service is optional (e.g. haptics on devices without a motor).
        /// </summary>
        /// <typeparam name="T">Interface or base type to resolve.</typeparam>
        /// <param name="service">The resolved instance, or <c>null</c>.</param>
        /// <returns><c>true</c> if the service was found.</returns>
        public static bool TryGet<T>(out T service) where T : class
        {
            Type key = typeof(T);

            if (_services.TryGetValue(key, out object raw))
            {
                service = raw as T;
                return service != null;
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Removes a service registration. Call this from <c>OnDestroy</c>
        /// to prevent stale references after scene unloads.
        /// </summary>
        /// <typeparam name="T">Interface or base type to unregister.</typeparam>
        public static void Unregister<T>() where T : class
        {
            Type key = typeof(T);

            if (_services.Remove(key))
            {
                Debug.Log($"[ServiceLocator] Unregistered -- {key.Name}.");
            }
        }

        /// <summary>
        /// Clears every registration. Intended for scene-transition cleanup
        /// or test teardown — <b>not for normal gameplay use</b>.
        /// </summary>
        public static void Reset()
        {
            _services.Clear();
            Debug.Log("[ServiceLocator] All services cleared.");
        }

        /// <summary>
        /// Returns <c>true</c> if a service of type <typeparamref name="T"/>
        /// is currently registered.
        /// </summary>
        public static bool IsRegistered<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        #endregion
    }
}
