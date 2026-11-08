using System;
using UnityEngine;
using VonderGames.AiTesting.Core;

namespace VonderGames.AiTesting.Gameplay.Entity
{
    public sealed class PlayerPresentation
    {
        private readonly PlayerController _player;
        private readonly PrimitiveFeedback _visual;
        private readonly Transform _aimMarker;
        private readonly CombatFeedback _feedback;
        private readonly SaveService _saves;
        private readonly Func<bool> _hasProtection;
        private LineRenderer _outline;
        private LineRenderer _deflectShield;
        private LineRenderer _danger;
        private float _dangerStrength;
        private float _dangerTarget;

        public PlayerPresentation(PlayerController player, PrimitiveFeedback visual, Transform aimMarker,
            CombatFeedback feedback, SaveService saves, Func<bool> hasProtection)
        {
            _player = player; _visual = visual; _aimMarker = aimMarker;
            _feedback = feedback; _saves = saves; _hasProtection = hasProtection;
            _outline = _feedback.CreateIndicator("DashOutline", _player.transform, Color.cyan, 20);
            _outline.positionCount = 5;
            for (int i = 0; i < 5; i++)
            {
                float angle = Mathf.PI / 4 + i * Mathf.PI / 2;
                _outline.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.85f);
            }
            _danger = _feedback.CreateIndicator("DangerHalo", _player.transform, new Color(1, 0.6f, 0.4f), 21);
            _deflectShield = _feedback.CreateIndicator("DeflectProtection", _player.transform, new Color(0.5f, 0.95f, 1, 0.8f), 22);
            _deflectShield.positionCount = 5;
            for (int i = 0; i < 5; i++)
            {
                float angle = Mathf.PI / 2 + i * Mathf.PI / 2;
                _deflectShield.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.15f);
            }
            _danger.positionCount = 33;
            for (int i = 0; i < 33; i++)
            {
                float angle = Mathf.PI * (0.08f + i / 32f * 0.84f);
                _danger.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.65f, 0.5f + Mathf.Sin(angle) * 0.65f));
            }
        }
        public void Tick()
        {
            _visual.Renderer.enabled = _player.IsDashing || _hasProtection() || _player.HealthState.Invulnerability <= 0 || Mathf.FloorToInt(_player.HealthState.Invulnerability * 12) % 2 == 0;
            _aimMarker.localPosition = _player.AimDirection * 0.85f;
            _outline.enabled = _player.IsDashing;
            _deflectShield.enabled = _hasProtection();
            _dangerStrength = Mathf.MoveTowards(_dangerStrength, _dangerTarget, Time.unscaledDeltaTime * 6);
            _danger.enabled = _dangerStrength > 0.01f;
            _danger.transform.localScale = Vector3.one * (0.8f + _dangerStrength * 0.5f);
            float pulse = _saves.Data.ReduceFlash ? 0 : Mathf.Sin(Time.unscaledTime * 9) * 0.08f * _dangerStrength;
            Color color = new Color(1, 0.65f, 0.45f, 0.2f + _dangerStrength * 0.5f + pulse);
            _danger.startColor = _danger.endColor = color;
        }
        public void SetDanger(float strength) => _dangerTarget = strength;
        public void Clear()
        {
            _dangerTarget = _dangerStrength = 0;
            _outline.enabled = _danger.enabled = _deflectShield.enabled = false;
            _visual.ResetVisual();
        }
        public void ShowDamage(float amount)
        {
            _feedback.Number(_player.transform.position, amount, true);
            _feedback.Burst(_player.transform.position, new Color(1, 0.3f, 0.4f), 8, 0.6f);
            _visual.Flash(_saves.Data.ReduceFlash);
        }
        public void ShowShot(Vector2 direction)
        {
            if (!_player.IsDashing) _visual.Pop();
            _feedback.Burst((Vector2)_player.transform.position + direction * 0.7f, Color.cyan, 2, 0.2f);
        }
        public void Reset() { Clear(); _visual.Renderer.enabled = true; }

    }
}
