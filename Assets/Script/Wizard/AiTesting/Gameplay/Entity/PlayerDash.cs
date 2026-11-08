using UnityEngine;
using VonderGames.AiTesting.Core;

namespace VonderGames.AiTesting.Gameplay.Entity
{
    public sealed class PlayerDash
    {
        private readonly PlayerController _player;
        private readonly Rigidbody2D _body;
        private readonly BoxCollider2D _collider;
        private readonly LayerMask _restExclusions;
        private readonly CombatDefinition _combat;
        private readonly CombatFeedback _feedback;
        private readonly PrimitiveFeedback _visual;
        private readonly PlayerMotor _motor;
        private float _dashAge;
        private float _dashRemaining;
        private float _dashCooldown;
        private bool _hasPerfectDodgeReward;
        private float _afterimageDistance;
        private Vector2 _dashDirection;
        public bool IsDashing => _dashRemaining > 0;
        public bool DashedThisStep { get; private set; }
        public float DashStepAge { get; private set; }
        public float DashStepDuration { get; private set; }
        public float Cooldown => _dashCooldown;

        public PlayerDash(PlayerController player, Rigidbody2D body, BoxCollider2D collider,
            CombatDefinition combat, CombatFeedback feedback, PrimitiveFeedback visual, PlayerMotor motor)
        {
            _player = player; _body = body; _collider = collider; _restExclusions = collider.excludeLayers;
            _combat = combat; _feedback = feedback; _visual = visual; _motor = motor;
        }
        public void Tick(float delta) => _dashCooldown = Mathf.Max(0, _dashCooldown - delta);
        public void BeginPhysicsStep() => DashedThisStep = false;
        public void Step(float delta)
        {
            DashedThisStep = true;
            DashStepAge = _dashAge;
            DashStepDuration = Mathf.Min(delta, _dashRemaining);
            float distance = _combat.DashDistance / _combat.DashDuration * DashStepDuration;
            RaycastHit2D wall = Physics2D.BoxCast(_player.HitBounds.center, _player.HitBounds.size, 0, _dashDirection, distance, 1 << 10);
            float travelled = wall.collider != null ? Mathf.Max(0, wall.distance - 0.01f) : distance;
            _body.linearVelocity = Vector2.zero;
            _body.position += _dashDirection * travelled;
            _dashAge += DashStepDuration;
            _dashRemaining = Mathf.Max(0, _dashRemaining - DashStepDuration);
            _afterimageDistance += travelled;
            if (_afterimageDistance >= _combat.DashDistance / 4)
            {
                _afterimageDistance = 0;
                _feedback.Afterimage(_visual.Renderer, 0.2f, true);
            }
            if (wall.collider != null) _dashRemaining = 0;
            if (!IsDashing) _collider.excludeLayers = _restExclusions;
        }
        public bool TryDash()
        {
            if (IsDashing || _dashCooldown > 0) return false;
            Vector2 movement = _motor.ReadMovement();
            _dashDirection = movement.sqrMagnitude > 0.01f ? movement.normalized : _motor.LastMovementDirection;
            _motor.LastMovementDirection = _dashDirection;
            _dashAge = 0;
            _hasPerfectDodgeReward = false;
            _dashRemaining = _combat.DashDuration;
            _dashCooldown = _combat.DashCooldown;
            _afterimageDistance = 0;
            _collider.excludeLayers = _restExclusions | (1 << 9);
            _feedback.Ring(_player.transform.position, 0.8f, Color.cyan, 0.15f);
            _visual.Stretch(_dashDirection);
            return true;
        }
        public void Reset()
        {
            _dashRemaining = _dashCooldown = _dashAge = _afterimageDistance = 0;
            _hasPerfectDodgeReward = false;
            DashedThisStep = false;
            DashStepAge = DashStepDuration = 0;
            _collider.excludeLayers = _restExclusions;
        }
        public void RefreshDash() => _dashCooldown = 0;
        public bool TryRewardPerfectDodge()
        {
            if (_hasPerfectDodgeReward || (!IsDashing && !DashedThisStep)) return false;
            _hasPerfectDodgeReward = true;
            return true;
        }

    }
}
