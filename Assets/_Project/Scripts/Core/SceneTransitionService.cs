// ------------------------------------------------------------
//  SceneTransitionService.cs  -  _Project.Scripts.Core
//  Provides a smooth fade-to-black transition between scenes.
//  Registers in ServiceLocator instead of using a static singleton.
// ------------------------------------------------------------

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using _Project.Scripts.Infrastructure;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Service that fades the screen to black, loads the target scene
    /// asynchronously, then fades back in. Creates its own Canvas and Image
    /// programmatically so no prefab wiring is required.<br/>
    /// Accessed via <c>ServiceLocator.Get&lt;ISceneTransitionService&gt;()</c>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Core/Scene Transition Service")]
    public class SceneTransitionService : MonoBehaviour, ISceneTransitionService
    {
        #region Constants -----------------------------------------

        /// <summary>Duration of each fade (in + out) in seconds.</summary>
        private const float FADE_DURATION = 0.4f;

        /// <summary>Canvas sort order -- must be above everything else.</summary>
        private const int CANVAS_SORT_ORDER = 999;

        #endregion

        #region State ---------------------------------------------

        private CanvasGroup _overlayGroup;
        private bool _isTransitioning;

        #endregion

        #region Public API ----------------------------------------

        /// <summary>Returns true while a transition is in progress.</summary>
        public bool IsTransitioning => _isTransitioning;

        /// <summary>
        /// Starts a fade-to-black transition to the given scene.
        /// Ignores duplicate calls while a transition is already in progress.
        /// </summary>
        public void TransitionTo(string sceneName)
        {
            if (_isTransitioning) return;

            Debug.Log($"[SceneTransitionService] Transition started -- target: {sceneName}.");
            StartCoroutine(TransitionCoroutine(sceneName));
        }

        /// <summary>
        /// Ensures an <see cref="ISceneTransitionService"/> is registered in the
        /// <see cref="ServiceLocator"/>.  If none exists, creates a new GameObject
        /// with this component (which self-registers in <c>Awake</c>).<br/>
        /// Call this from any script that needs the service before attempting
        /// <c>ServiceLocator.TryGet</c>.
        /// </summary>
        public static void EnsureAvailable()
        {
            if (ServiceLocator.IsRegistered<ISceneTransitionService>()) return;

            var go = new GameObject("[SceneTransitionService]");
            go.AddComponent<SceneTransitionService>();
            Debug.Log("[SceneTransitionService] Auto-created -- no instance found in scene.");
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            if (ServiceLocator.IsRegistered<ISceneTransitionService>())
            {
                Destroy(gameObject);
                return;
            }

            ServiceLocator.Register<ISceneTransitionService>(this);
            DontDestroyOnLoad(gameObject);
            BuildOverlay();
        }

        private void OnDestroy()
        {
            if (ServiceLocator.TryGet<ISceneTransitionService>(out var current) && current == (ISceneTransitionService)this)
                ServiceLocator.Unregister<ISceneTransitionService>();
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Builds a full-screen black overlay Canvas + Image + CanvasGroup
        /// programmatically. Starts fully transparent.
        /// </summary>
        private void BuildOverlay()
        {
            // Canvas
            var canvasGo = new GameObject("TransitionOverlay");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CANVAS_SORT_ORDER;

            canvasGo.AddComponent<CanvasScaler>();

            // Full-screen black image
            var imageGo = new GameObject("Img_Fade");
            imageGo.transform.SetParent(canvasGo.transform, false);

            var image = imageGo.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = true;

            var rt = image.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // CanvasGroup for fade
            _overlayGroup = canvasGo.AddComponent<CanvasGroup>();
            _overlayGroup.alpha = 0f;
            _overlayGroup.blocksRaycasts = false;
            _overlayGroup.interactable = false;
        }

        /// <summary>
        /// Coroutine: fade in overlay -> load scene async -> fade out overlay.
        /// </summary>
        private IEnumerator TransitionCoroutine(string sceneName)
        {
            _isTransitioning = true;
            _overlayGroup.blocksRaycasts = true;

            // Fade to black
            yield return FadeOverlay(0f, 1f);

            // Load scene asynchronously
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            if (asyncLoad != null)
            {
                while (!asyncLoad.isDone)
                    yield return null;
            }

            // Fade from black
            yield return FadeOverlay(1f, 0f);

            _overlayGroup.blocksRaycasts = false;
            _isTransitioning = false;
            Debug.Log($"[SceneTransitionService] Transition complete -- scene: {sceneName}.");
        }

        /// <summary>
        /// Smoothly interpolates the overlay alpha from <paramref name="from"/>
        /// to <paramref name="to"/> over <see cref="FADE_DURATION"/> seconds.
        /// Uses <c>Time.unscaledDeltaTime</c> so it works even when time is paused.
        /// </summary>
        private IEnumerator FadeOverlay(float from, float to)
        {
            float elapsed = 0f;
            _overlayGroup.alpha = from;

            while (elapsed < FADE_DURATION)
            {
                elapsed += Time.unscaledDeltaTime;
                _overlayGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / FADE_DURATION));
                yield return null;
            }

            _overlayGroup.alpha = to;
        }

        #endregion
    }
}
