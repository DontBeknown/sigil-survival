using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Combat;
using VonderGames.AiTesting.Gameplay.Manager;
using VonderGames.AiTesting.Gameplay.Session;

namespace VonderGames.AiTesting.UI
{
    public sealed class UIGestureDrawing : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private RectTransform _guide;
        [SerializeField] private TMP_Text _heading;
        [SerializeField] private TMP_Text _instruction;
        [SerializeField] private TMP_Text _timer;
        [SerializeField] private UnityEngine.UI.Image[] _markers;
        [SerializeField] private TMP_Text[] _markerLabels;
        [SerializeField] private RectTransform[] _segments;
        [SerializeField] private RectTransform[] _arrows;
        [SerializeField] private RectTransform[] _trailSegments;

        private RunSession _session;
        private DrawingController _drawing;
        private GameplayClock _clock;
        private SpellDefinition[] _spells;
        private InputManager _input;
        private readonly GestureTrace _trace = new GestureTrace();
        private float _deadline;
        private bool _isAwaitingRelease;
        private bool _wasHeld;
        private int _trailCursor;
        private Vector2 _lastTrailPoint;
        private Color _color;
        private int _previousReached;
        private bool _isDeflect;
        private SpellKind _kind;
        private Canvas _canvas;

        private void Update()
        {
            if (_session == null || _session.State != RunState.Drawing)
                return;

            float remaining = _deadline - Time.unscaledTime;
            var panel = (RectTransform)_guide.parent;
            float scale = Mathf.Min(1, (panel.rect.width - 24) / 400, (panel.rect.height - 200) / 320);
            _guide.localScale = Vector3.one * Mathf.Max(0.15f, scale);
            _timer.color = !_isDeflect && remaining < 1 ? new Color(1, 0.7f, 0.4f) : Color.white;
            _timer.text = _isDeflect ? "NO TIME LIMIT / PROTECTED" : $"{Mathf.Max(0, remaining):0.0}s  •  COMBAT {_clock.DrawingTimeScale * 100:0}%";
            if (!_isDeflect && remaining <= 0)
            {
                _drawing.FinishDrawing(false, "TIME EXPIRED");
                return;
            }
            bool isHeld = _input.Actions.UI.Click.IsPressed();
            if (_isAwaitingRelease)
            {
                _isAwaitingRelease = isHeld;
                return;
            }
            Camera camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_guide, _input.Actions.UI.Point.ReadValue<Vector2>(), camera, out var point))
                return;
            if (isHeld && !_trace.HasStarted)
            {
                if (!_trace.Begin(point))
                {
                    if (!_wasHeld) ShowStartHint("Hold and move onto 1 to start — outside clicks are ignored.");
                    _wasHeld = true;
                    return;
                }
                _lastTrailPoint = point;
            }
            if (isHeld && _trace.HasStarted)
            {
                _trace.Move(point);
                if (_trace.HasFailed)
                {
                    if (_isDeflect)
                    {
                        ResetStroke("Follow the checkpoints in order — retry at 1.");
                        _isAwaitingRelease = true;
                        return;
                    }
                    _drawing.FinishDrawing(false, "TRACE FAILED");
                    return;
                }
                if (Vector2.Distance(_lastTrailPoint, point) >= 3)
                {
                    RectTransform segment = _trailSegments[_trailCursor++ % _trailSegments.Length];
                    segment.gameObject.SetActive(true);
                    PlaceSegment(segment, _lastTrailPoint, point, 5);
                    segment.GetComponent<UnityEngine.UI.Image>().color = _color;
                    _lastTrailPoint = point;
                }
                RefreshMarkers();
            }
            if (!isHeld && _wasHeld)
            {
                if (!_trace.IsComplete)
                {
                    ResetStroke();
                    return;
                }
                _drawing.FinishDrawing(_trace.IsComplete, _trace.IsComplete ? "" : "TRACE FAILED");
                return;
            }
            _wasHeld = isHeld;
        }

        public void Initialize(RunSession session, DrawingController drawing, GameplayClock clock, SpellDefinition[] spells, InputManager input)
        {
            _session = session; _drawing = drawing; _clock = clock; _spells = spells;
            _input = input;
            _canvas = _guide.GetComponentInParent<Canvas>(true);
            Close();
        }

        public void Open(SpellKind kind)
        {
            _isDeflect = kind == SpellKind.Deflect;
            _kind = kind;
            _trace.Reset(kind);
            _deadline = _isDeflect ? float.PositiveInfinity : Time.unscaledTime + 3;
            _isAwaitingRelease = _input.Actions.UI.Click.IsPressed();
            _wasHeld = false;
            _trailCursor = 0;
            _previousReached = 0;
            _color = _spells[(int)kind].Color;
            _heading.text = _spells[(int)kind].DisplayName.ToUpperInvariant();
            if (kind == SpellKind.Deflect) _heading.text = "DEFLECT / PROTECTED";
            _root.SetActive(true);
            for (int i = 0; i < _segments.Length; i++)
            {
                bool isVisible = i < _trace.Points.Length - 1;
                _segments[i].gameObject.SetActive(isVisible);
                _arrows[i].gameObject.SetActive(isVisible);
                if (!isVisible)
                    continue;

                Vector2 start = _trace.Points[i];
                Vector2 end = _trace.Points[i + 1];
                PlaceSegment(_segments[i], start, end, 4);
                _arrows[i].anchoredPosition = Vector2.Lerp(start, end, 0.55f);
                _arrows[i].localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg);
            }
            foreach (var segment in _trailSegments)
                segment.gameObject.SetActive(false);

            RefreshMarkers();
        }

        public void Close()
        {
            foreach (var marker in _markers)
            {
                marker.rectTransform.DOKill();
                marker.rectTransform.localScale = Vector3.one;
            }
            _root.SetActive(false);
        }

        public void RetryDeflect()
        {
            ResetStroke("Follow the checkpoints in order — retry at 1. No time limit.");
            _isAwaitingRelease = true;
        }

        private void ResetStroke(string hint = null)
        {
            bool hadStroke = _trace.HasStarted;
            _trace.Reset(_kind);
            _wasHeld = false;
            _trailCursor = 0;
            _previousReached = 0;
            foreach (var segment in _trailSegments) segment.gameObject.SetActive(false);
            RefreshMarkers();
            string retryHint = _isDeflect ? "Incomplete stroke — retry at 1. No time limit." : "Incomplete stroke — retry at 1 before time runs out.";
            ShowStartHint(hint ?? (hadStroke ? retryHint : "Start at 1 — outside clicks are ignored."));
        }

        private void ShowStartHint(string message)
        {
            _instruction.text = message;
            RectTransform start = _markers[0].rectTransform;
            start.DOKill();
            start.localScale = Vector3.one;
            start.DOPunchScale(Vector3.one * 0.15f, 0.2f, 3).SetUpdate(true);
        }

        private void RefreshMarkers()
        {
            bool checkpointChanged = _trace.Reached != _previousReached;
            for (int i = 0; i < _markers.Length; i++)
            {
                bool isVisible = i < _trace.Points.Length;
                _markers[i].gameObject.SetActive(isVisible);
                if (!isVisible)
                    continue;

                _markers[i].rectTransform.anchoredPosition = _trace.Points[i];
                _markers[i].color = i < _trace.Reached ? _color : i == _trace.Reached ? Color.white : new Color(0.25f, 0.3f, 0.4f);
                _markers[i].rectTransform.localScale = Vector3.one * (i == _trace.Reached ? 1.12f : checkpointChanged && i == _trace.Reached - 1 ? 1.1f : 1);
                _markerLabels[i].text = i < _trace.Reached ? $"{i + 1} ✓" : (i == 0 ? "1\nSTART" : (i + 1).ToString());
                if (i == _trace.Points.Length - 1 && _trace.Points[i] == _trace.Points[0])
                {
                    _markers[i].gameObject.SetActive(_trace.Reached >= _trace.Points.Length - 1);
                    _markerLabels[0].text = "1 / 5";
                }
            }
            _previousReached = _trace.Reached;
            _instruction.text = _trace.IsComplete ? "Release to cast!" : _isDeflect
                ? "Trace from 1, then release. Unlimited retries. You are protected. Esc cancels."
                : "Hold at 1, trace in order, then release. Retry within 3s. WASD moves. Esc cancels.";
        }

        private static void PlaceSegment(RectTransform segment, Vector2 start, Vector2 end, float width)
        {
            segment.anchoredPosition = (start + end) * 0.5f;
            segment.sizeDelta = new Vector2(Vector2.Distance(start, end), width);
            segment.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg);
        }
    }
}
