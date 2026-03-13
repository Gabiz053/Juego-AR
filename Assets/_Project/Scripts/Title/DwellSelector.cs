// ------------------------------------------------------------
//  DwellSelector.cs  -  _Project.Scripts.Title
//  Implements dwell-time selection: when the hand cursor stays
//  over a button for a configurable duration, it triggers the
//  corresponding TitleSceneManager.SelectMode() call.
//  Highlights hovered buttons with a scale-up effect.
// ------------------------------------------------------------

using System;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Title
{
    /// <summary>
    /// Listens to <see cref="HandTrackingService"/> fingertip positions and
    /// checks whether the cursor overlaps any of the mode-selection buttons.<br/>
    /// Accumulates a dwell timer while hovering; when the threshold is reached,
    /// calls <see cref="TitleSceneManager.SelectMode"/>.<br/>
    /// Drives the radial progress fill on <see cref="HandCursorUI"/> and
    /// highlights the hovered button with a smooth scale-up and tint effect.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Title/Dwell Selector")]
    public class DwellSelector : MonoBehaviour
    {
        #region Constants -----------------------------------------

        /// <summary>Default dwell time in seconds.</summary>
        private const float DEFAULT_DWELL_DURATION = 1f;

        /// <summary>Scale factor applied to hovered buttons.</summary>
        private const float HOVER_SCALE = 1.12f;

        /// <summary>Speed at which button scale lerps to target (units/s).</summary>
        private const float HOVER_SCALE_SPEED = 8f;

        /// <summary>Tint colour applied to hovered button images (white highlight).</summary>
        private static readonly Color HOVER_TINT = Color.white;

        #endregion

        #region Inspector -----------------------------------------

        [Header("Dependencies")]
        [Tooltip("HandTrackingService that provides fingertip screen positions.")]
        [SerializeField] private HandTrackingService _handTracking;

        [Tooltip("HandCursorUI to update the dwell progress ring.")]
        [SerializeField] private HandCursorUI _cursorUI;

        [Tooltip("TitleSceneManager whose SelectMode(int) will be called on dwell completion.")]
        [SerializeField] private TitleSceneManager _titleSceneManager;

        [Header("Button Targets")]
        [Tooltip("RectTransforms of the mode buttons in order: [0]=Bonsai, [1]=Normal, [2]=Real.")]
        [SerializeField] private RectTransform[] _buttonRects = new RectTransform[3];

        [Header("Dwell Settings")]
        [Tooltip("Seconds the cursor must remain over a button to trigger selection.")]
        [SerializeField] [Range(0.5f, 3f)] private float _dwellDuration = DEFAULT_DWELL_DURATION;

        #endregion

        #region Events --------------------------------------------

        /// <summary>Fired each frame during dwell with (buttonIndex, progress 0-1).</summary>
        public event Action<int, float> OnDwellProgress;

        /// <summary>Fired once when dwell completes with the selected button index.</summary>
        public event Action<int> OnDwellCompleted;

        #endregion

        #region State -----------------------------------------

        private float _dwellTimer;
        private int _hoveredIndex = -1;
        private bool _selectionMade;
        private float _lastUpdateTime;
        private Image[] _buttonImages;
        private Color[] _buttonOriginalColors;
        private float[] _buttonCurrentScales;

        #endregion

        #region Public API ----------------------------------------

        /// <summary>Current dwell progress (0-1) for the hovered button.</summary>
        public float DwellProgress => _hoveredIndex >= 0
            ? Mathf.Clamp01(_dwellTimer / _dwellDuration)
            : 0f;

        /// <summary>Index of the currently hovered button (-1 if none).</summary>
        public int HoveredButtonIndex => _hoveredIndex;

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void OnEnable()
        {
            if (_handTracking != null)
            {
                _handTracking.OnFingertipScreenPosition += HandleFingertipUpdate;
                _handTracking.OnHandLost                += HandleHandLost;
            }
        }

        private void OnDisable()
        {
            if (_handTracking != null)
            {
                _handTracking.OnFingertipScreenPosition -= HandleFingertipUpdate;
                _handTracking.OnHandLost                -= HandleHandLost;
            }
        }

        private void Start()
        {
            ValidateReferences();
            CacheButtonReferences();
            ResetDwell();
        }

        private void Update()
        {
            UpdateButtonHighlights();
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Caches Image components and original colours from button RectTransforms
        /// so hover effects can be applied and reverted without allocations.
        /// </summary>
        private void CacheButtonReferences()
        {
            if (_buttonRects == null) return;

            _buttonImages = new Image[_buttonRects.Length];
            _buttonOriginalColors = new Color[_buttonRects.Length];
            _buttonCurrentScales = new float[_buttonRects.Length];

            for (int i = 0; i < _buttonRects.Length; i++)
            {
                _buttonCurrentScales[i] = 1f;

                if (_buttonRects[i] == null) continue;

                _buttonImages[i] = _buttonRects[i].GetComponent<Image>();
                if (_buttonImages[i] != null)
                    _buttonOriginalColors[i] = _buttonImages[i].color;
            }
        }

        /// <summary>
        /// Smoothly scales hovered/unhovered buttons towards their target scale
        /// and applies a tint colour to the hovered one.
        /// </summary>
        private void UpdateButtonHighlights()
        {
            if (_buttonRects == null || _buttonCurrentScales == null) return;

            for (int i = 0; i < _buttonRects.Length; i++)
            {
                if (_buttonRects[i] == null) continue;

                float target = (i == _hoveredIndex) ? HOVER_SCALE : 1f;
                _buttonCurrentScales[i] = Mathf.MoveTowards(
                    _buttonCurrentScales[i], target, HOVER_SCALE_SPEED * Time.deltaTime);

                float s = _buttonCurrentScales[i];
                _buttonRects[i].localScale = new Vector3(s, s, 1f);

                if (_buttonImages[i] != null)
                {
                    _buttonImages[i].color = (i == _hoveredIndex)
                        ? HOVER_TINT
                        : _buttonOriginalColors[i];
                }
            }
        }

        /// <summary>
        /// Called each time a valid fingertip screen position is received.
        /// Checks overlap with button RectTransforms and accumulates dwell time.
        /// </summary>
        private void HandleFingertipUpdate(Vector2 screenPos)
        {
            if (_selectionMade) return;

            int hitIndex = GetHoveredButtonIndex(screenPos);

            // Target changed — reset timer and notify cursor
            if (hitIndex != _hoveredIndex)
            {
                _dwellTimer     = 0f;
                _hoveredIndex   = hitIndex;
                _lastUpdateTime = Time.time;
                _cursorUI?.SetDwellProgress(0f);
                _cursorUI?.SetHovering(_hoveredIndex >= 0);
            }

            if (_hoveredIndex >= 0)
            {
                float now = Time.time;
                _dwellTimer += now - _lastUpdateTime;
                _lastUpdateTime = now;
                float progress = Mathf.Clamp01(_dwellTimer / _dwellDuration);

                _cursorUI?.SetDwellProgress(progress);
                OnDwellProgress?.Invoke(_hoveredIndex, progress);

                if (_dwellTimer >= _dwellDuration)
                {
                    TriggerSelection(_hoveredIndex);
                }
            }
            else
            {
                _lastUpdateTime = Time.time;
                _cursorUI?.SetDwellProgress(0f);
            }
        }

        /// <summary>
        /// Returns the index of the button whose RectTransform contains the
        /// given screen point, or -1 if none.
        /// </summary>
        private int GetHoveredButtonIndex(Vector2 screenPos)
        {
            for (int i = 0; i < _buttonRects.Length; i++)
            {
                if (_buttonRects[i] == null) continue;

                if (RectTransformUtility.RectangleContainsScreenPoint(
                        _buttonRects[i], screenPos, null))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>Calls <see cref="TitleSceneManager.SelectMode"/> for the given index.</summary>
        private void TriggerSelection(int modeIndex)
        {
            _selectionMade = true;
            Debug.Log($"[DwellSelector] Dwell complete on button {modeIndex} -- selecting mode.");
            OnDwellCompleted?.Invoke(modeIndex);

            if (_titleSceneManager != null)
                _titleSceneManager.SelectMode(modeIndex);
        }

        /// <summary>Resets dwell state when the tracked hand is lost.</summary>
        private void HandleHandLost()
        {
            if (_hoveredIndex >= 0)
                Debug.Log("[DwellSelector] Hand lost -- dwell reset.");
            ResetDwell();
        }

        /// <summary>Zeroes out dwell timer, hovered index, progress ring, and cursor hover state.</summary>
        private void ResetDwell()
        {
            _dwellTimer     = 0f;
            _hoveredIndex   = -1;
            _selectionMade  = false;
            _lastUpdateTime = Time.time;
            _cursorUI?.SetDwellProgress(0f);
            _cursorUI?.SetHovering(false);
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_handTracking == null)
                Debug.LogError("[DwellSelector] _handTracking is not assigned!", this);
            if (_cursorUI == null)
                Debug.LogError("[DwellSelector] _cursorUI is not assigned!", this);
            if (_titleSceneManager == null)
                Debug.LogError("[DwellSelector] _titleSceneManager is not assigned!", this);

            if (_buttonRects == null || _buttonRects.Length == 0)
            {
                Debug.LogError("[DwellSelector] _buttonRects array is empty!", this);
                return;
            }

            for (int i = 0; i < _buttonRects.Length; i++)
            {
                if (_buttonRects[i] == null)
                    Debug.LogWarning($"[DwellSelector] _buttonRects[{i}] is not assigned.", this);
            }
        }

        #endregion
    }
}
