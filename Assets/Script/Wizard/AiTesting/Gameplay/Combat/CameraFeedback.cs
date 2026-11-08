using System;
using Unity.Cinemachine;
using UnityEngine;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Manager;

namespace VonderGames.AiTesting.Gameplay.Combat
{
    public sealed class CameraFeedback : IDisposable
    {
        private readonly Camera _camera;
        private readonly InputManager _input;
        private readonly CinemachineCamera _followCamera;
        private readonly CinemachineImpulseSource _impulse;
        private readonly SaveService _saves;
        private readonly bool _previousImpulseTimeMode;
        private float _nextShakeTime;
        public Vector2 MouseWorldPosition => _camera.ScreenToWorldPoint(_input.Actions.UI.Point.ReadValue<Vector2>());

        public CameraFeedback(Camera camera, InputManager input, CinemachineCamera followCamera,
            CinemachineImpulseSource impulse, SaveService saves)
        {
            _camera = camera;
            _input = input;
            _followCamera = followCamera;
            _impulse = impulse;
            _saves = saves;
            _previousImpulseTimeMode = CinemachineImpulseManager.Instance.IgnoreTimeScale;
            CinemachineImpulseManager.Instance.IgnoreTimeScale = true;
        }
        public void Tick() => _followCamera.Lens.OrthographicSize = Mathf.Min(10, 29.5f / Mathf.Max(0.01f, _camera.aspect));
        public Vector2 WorldToScreen(Vector2 position) => _camera.WorldToScreenPoint(position);
        public void Reset() { _nextShakeTime = 0; _followCamera.PreviousStateIsValid = false; Clear(); }
        public void Clear() => CinemachineImpulseManager.Instance.Clear();
        public void Dispose()
        {
            Clear();
            CinemachineImpulseManager.Instance.IgnoreTimeScale = _previousImpulseTimeMode;
        }
        public void Shake(float strength)
        {
            if (_saves.Data.ReduceShake || Time.unscaledTime < _nextShakeTime)
                return;

            _nextShakeTime = Time.unscaledTime + 0.1f;
            strength = Mathf.Min(strength, 0.28f);
            _impulse.GenerateImpulseWithVelocity(new Vector3(strength, strength * 0.6f, 0));
        }

    }
}
