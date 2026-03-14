// ------------------------------------------------------------
//  TitleLogoAnimator.cs  -  _Project.Scripts.Title
//  Gentle floating animation for the title logo text.
// ------------------------------------------------------------

using UnityEngine;

namespace _Project.Scripts.Title
{
    /// <summary>
    /// Applies a smooth sine-wave bobbing motion to the attached
    /// <see cref="RectTransform"/>, giving the title logo a subtle
    /// floating feel on the title screen.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Title/Title Logo Animator")]
    public class TitleLogoAnimator : MonoBehaviour
    {
        #region Constants -----------------------------------------

        /// <summary>Default vertical amplitude in canvas units.</summary>
        private const float DEFAULT_AMPLITUDE = 12f;

        /// <summary>Default oscillation frequency in Hz.</summary>
        private const float DEFAULT_FREQUENCY = 0.6f;

        #endregion

        #region Inspector -----------------------------------------

        [Header("Bobbing Settings")]
        [Tooltip("Vertical amplitude of the bobbing motion in canvas units.")]
        [SerializeField] private float _amplitude = DEFAULT_AMPLITUDE;

        [Tooltip("Oscillation frequency in cycles per second.")]
        [SerializeField] private float _frequency = DEFAULT_FREQUENCY;

        #endregion

        #region State ---------------------------------------------

        private RectTransform _rectTransform;
        private Vector2 _originPosition;

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Start()
        {
            ValidateReferences();

            if (_rectTransform != null)
                _originPosition = _rectTransform.anchoredPosition;
        }

        private void Update()
        {
            if (_rectTransform == null) return;

            float offset = _amplitude * Mathf.Sin(Time.time * _frequency * Mathf.PI * 2f);
            _rectTransform.anchoredPosition = new Vector2(_originPosition.x, _originPosition.y + offset);
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_rectTransform == null)
                Debug.LogWarning("[TitleLogoAnimator] _rectTransform is not assigned.", this);
        }

        #endregion
    }
}
