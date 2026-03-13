// ------------------------------------------------------------
//  DwellSelector.cs  -  _Project.Scripts.Title
//  Implements dwell-time selection: when the hand cursor stays
//  over a button for a configurable duration, it triggers the
//  corresponding TitleSceneManager.SelectMode() call.
//  Highlights hovered buttons with a scale-up effect.
//  Plays hover and click sound effects via its own AudioSource.
// ------------------------------------------------------------

using System;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Title
{
    /// <summary>
    /// Listens to <see cref="HandTrackingService"/> fingertip positions and
    /// checks whether the cursor overlaps any of the mode-selection buttons.<br/>
    /// Selection can happen two ways:<br/>
    /// 1. <b>Pinch click</b> — a thumb-index pinch while hovering a button triggers
    ///    instant selection (preferred method).<br/>
    /// 2. <b>Dwell time</b> — staying over a button long enough also triggers
    ///    selection as a fallback.<br/>
    /// Drives the radial progress fill on <see cref="HandCursorUI"/> and
    /// highlights the hovered button with a smooth scale-up and tint effect.<br/>
    /// Plays hover and click SFX through its own <see cref="AudioSource"/>.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
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

        /// <summary>Maximum random pitch offset applied to hover/click SFX.</summary>
        private const float PITCH_VARIATION = 0.05f;

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

        [Header("Audio")]
        [Tooltip("Clips played at random when the cursor enters a button (hover).")]
        [SerializeField] private AudioClip[] _hoverSounds;

        [Tooltip("Clips played at random when a button is selected (pinch or dwell).")]
        [SerializeField] private AudioClip[] _selectSounds;

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
        private AudioSource _audioSource;
        private int _lastHoverSoundIndex = -1;
        private int _lastSelectSoundIndex = -1;

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
                _handTracking.OnPinchDetected           += HandlePinch;
            }
        }

        private void OnDisable()
        {
            if (_handTracking != null)
            {
                _handTracking.OnFingertipScreenPosition -= HandleFingertipUpdate;
                _handTracking.OnHandLost                -= HandleHandLost;
                _handTracking.OnPinchDetected           -= HandlePinch;
            }
        }

        private void Start()
        {
            ValidateReferences();
            InitializeAudioSource();
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
        /// Configures the required <see cref="AudioSource"/> for 2D one-shot playback.
        /// </summary>
        private void InitializeAudioSource()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake  = false;
            _audioSource.spatialBlend = 0f;
        }

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

                // Play hover sound when entering a new button
                if (_hoveredIndex >= 0)
                {
                    PlaySfx(_hoverSounds, ref _lastHoverSoundIndex);
                    Debug.Log($"[DwellSelector] Hover entered button {_hoveredIndex} -- playing hover SFX.");
                }
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
            PlaySfx(_selectSounds, ref _lastSelectSoundIndex);
            Debug.Log($"[DwellSelector] Selection triggered on button {modeIndex}.");
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

        /// <summary>
        /// Handles pinch gesture from <see cref="HandTrackingService"/>.
        /// If the cursor is currently hovering over a button, triggers
        /// instant selection without waiting for dwell to complete.
        /// </summary>
        private void HandlePinch()
        {
            if (_selectionMade) return;
            if (_hoveredIndex < 0) return;

            Debug.Log($"[DwellSelector] Pinch click on button {_hoveredIndex} -- selecting mode.");
            TriggerSelection(_hoveredIndex);
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

        /// <summary>
        /// Picks a random clip from <paramref name="clips"/> (avoiding the
        /// last-played index), applies pitch variation, and plays it as a one-shot.
        /// </summary>
        private void PlaySfx(AudioClip[] clips, ref int lastIndex)
        {
            if (clips == null || clips.Length == 0 || _audioSource == null) return;

            int index;
            if (clips.Length == 1)
                index = 0;
            else
            {
                do { index = UnityEngine.Random.Range(0, clips.Length); }
                while (index == lastIndex);
            }

            AudioClip clip = clips[index];
            if (clip == null) return;

            lastIndex = index;

            float prevPitch    = _audioSource.pitch;
            _audioSource.pitch = UnityEngine.Random.Range(1f - PITCH_VARIATION, 1f + PITCH_VARIATION);
            _audioSource.PlayOneShot(clip);
            _audioSource.pitch = prevPitch;
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
