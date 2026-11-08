using UnityEngine;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Manager;

namespace VonderGames.AiTesting.Gameplay.Entity
{
    public sealed class PlayerMotor
    {
        public Vector2 LastMovementDirection { get; set; } = Vector2.right;
        private readonly Rigidbody2D _body;
        private readonly RunProgress _progress;
        private readonly InputManager _input;

        public PlayerMotor(Rigidbody2D body, RunProgress progress, InputManager input)
        {
            _body = body; _progress = progress; _input = input;
        }
        public Vector2 ReadMovement() => Vector2.ClampMagnitude(_input.Actions.Player.Movement.ReadValue<Vector2>(), 1);
        public void Step(float physicsRatio)
        {
            Vector2 movement = ReadMovement();
            if (movement.sqrMagnitude > 0.01f) LastMovementDirection = movement.normalized;
            _body.linearVelocity = movement * (6 * _progress.MovementMultiplier * physicsRatio);
        }
        public void Stop() => _body.linearVelocity = Vector2.zero;
        public void Reset() { Stop(); _body.position = Vector2.zero; LastMovementDirection = Vector2.right; }

    }
}
