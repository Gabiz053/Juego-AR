// ??????????????????????????????????????????????
//  ButtonPressAnimation.cs  ·  _Project.Scripts.UI
//  Adds a squeeze animation to any Button.
//  Add this component once per button — no OnClick wiring needed.
// ??????????????????????????????????????????????

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Attaches to any <see cref="Button"/> and automatically plays:<br/>
    /// • A squeeze (scale-down ? scale-up) animation on pointer down.<br/>
    /// <br/>
    /// <b>No OnClick wiring required.</b> Add this component to every button
    /// prefab or GameObject — it self-registers via
    /// <see cref="IPointerDownHandler"/> and <see cref="IPointerUpHandler"/>.<br/>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    [AddComponentMenu("ARmonia/UI/Button Press Animation")]
    public class ButtonPressAnimation : MonoBehaviour,
                                        IPointerDownHandler,
                                        IPointerUpHandler
    {
        #region Inspector ?????????????????????????????????????

        [Header("Squeeze")]
        [Tooltip("Scale the button shrinks to on press (0.88 = 12 % smaller).")]
        [SerializeField] private float _pressedScale  = 0.88f;

        [Tooltip("Seconds for the full press + release animation.")]
        [SerializeField] private float _duration      = 0.10f;

        #endregion

        #region State ?????????????????????????????????????????

        private Vector3       _originalScale;
        private Coroutine     _anim;
        private Button        _button;
        private RectTransform _rect;

        #endregion

        #region Unity Lifecycle ????????????????????????????????

        private void Awake()
        {
            _button        = GetComponent<Button>();
            _originalScale = transform.localScale;
            _rect          = GetComponent<RectTransform>();

            // Ensure pivot is centred so scale animates from the middle of the button.
            if (_rect != null)
                _rect.pivot = new Vector2(0.5f, 0.5f);
        }

        #endregion

        #region Pointer Handlers ??????????????????????????????

        public void OnPointerDown(PointerEventData _)
        {
            if (_button != null && !_button.interactable) return;

            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(Squeeze());
        }

        public void OnPointerUp(PointerEventData _)
        {
            // Snap back to original scale in case the coroutine was interrupted.
            if (_anim == null)
                transform.localScale = _originalScale;
        }

        #endregion

        #region Animation ?????????????????????????????????????

        private IEnumerator Squeeze()
        {
            float half    = _duration * 0.5f;
            float elapsed = 0f;
            Vector3 target = _originalScale * _pressedScale;

            // Press down.
            while (elapsed < half)
            {
                elapsed            += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(_originalScale, target,
                                           Mathf.SmoothStep(0f, 1f, elapsed / half));
                yield return null;
            }

            elapsed = 0f;

            // Spring back.
            while (elapsed < half)
            {
                elapsed            += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(target, _originalScale,
                                           Mathf.SmoothStep(0f, 1f, elapsed / half));
                yield return null;
            }

            transform.localScale = _originalScale;
            _anim = null;
        }

        #endregion
    }
}
