using System;
using UnityEngine;
using VonderGames.AiTesting.Core;

namespace VonderGames.AiTesting.Gameplay.Session
{
    public sealed class GameplayClock : IDisposable
    {
        public float DrawingTimeScale { get; }
        public float CombatScale => !_session.IsCombatActive ? 0 : Mathf.Min(_session.State == RunState.Drawing ? DrawingTimeScale : 1, _perfectRemaining > 0 ? _combat.PerfectScale : 1);
        public float PlayerScale => !_session.IsCombatActive ? 0 : _session.State == RunState.Drawing ? DrawingTimeScale : 1;
        public float PlayerDelta => Time.unscaledDeltaTime * PlayerScale;
        public float PlayerPhysicsRatio => CombatScale > 0 ? PlayerScale / CombatScale : 0;
        private readonly RunSession _session;
        private readonly CombatDefinition _combat;
        private readonly float _baseFixedDelta;
        private float _perfectRemaining;

        public GameplayClock(RunSession session, CombatDefinition combat, float drawingTimeScale)
        {
            _session = session;
            _combat = combat;
            DrawingTimeScale = drawingTimeScale;
            _baseFixedDelta = Time.fixedDeltaTime;
            _session.OnStateChanged += OnStateChangedHandler;
        }

        public void Tick(float realDelta)
        {
            if (_session.IsCombatActive)
                _perfectRemaining = Mathf.Max(0, _perfectRemaining - realDelta);
            ApplyTimeScale();
        }

        public void BeginPerfectDodge()
        {
            _perfectRemaining = _combat.PerfectDuration;
            ApplyTimeScale();
        }

        public void Reset()
        {
            _perfectRemaining = 0;
            ApplyTimeScale();
        }

        public void Dispose()
        {
            _session.OnStateChanged -= OnStateChangedHandler;
            Time.timeScale = 1;
            Time.fixedDeltaTime = _baseFixedDelta;
        }

        private void OnStateChangedHandler(RunState state) => ApplyTimeScale();
        private void ApplyTimeScale()
        {
            float scale = CombatScale;
            Time.timeScale = scale;
            // Keep physics sampling responsive in real time, including relative dash/shot sweeps.
            if (scale > 0 && !Time.inFixedTimeStep) Time.fixedDeltaTime = _baseFixedDelta * scale;
        }

    }
}
