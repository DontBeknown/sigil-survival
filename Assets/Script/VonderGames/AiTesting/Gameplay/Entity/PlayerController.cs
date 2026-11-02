using System;
using UnityEngine;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Instance;
using VonderGames.AiTesting.Gameplay.Manager;

namespace VonderGames.AiTesting.Gameplay.Entity
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour, IDamageable
    {
        [SerializeField] private PrimitiveFeedback _feedback;
        [SerializeField] private Transform _aimMarker;

        public event Action<float, float> OnHealthChanged;

        private Rigidbody2D _body;
        private GameplayInstance _game;
        private InputManager _input;
        private Camera _camera;
        private float _health = 100;
        public float Health => _health;
        public bool IsDead => _health <= 0;
        private float _invulnerability;
        public Vector2 AimDirection { get; private set; } = Vector2.right;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (_game.State != RunState.Playing)
                return;

            _invulnerability = Mathf.Max(0, _invulnerability - Time.deltaTime);
            Vector2 cursor = _camera.ScreenToWorldPoint(_input.Actions.Player.Aim.ReadValue<Vector2>());
            Vector2 direction = cursor - _body.position;
            if (direction.sqrMagnitude > 0.01f)
                AimDirection = direction.normalized;

            _aimMarker.localPosition = AimDirection * 0.85f;
        }

        private void FixedUpdate()
        {
            if (_game.State != RunState.Playing)
            {
                _body.linearVelocity = Vector2.zero;
                return;
            }
            Vector2 movement = Vector2.ClampMagnitude(_input.Actions.Player.Movement.ReadValue<Vector2>(), 1);
            _body.linearVelocity = movement * (6 * _game.Progress.MovementMultiplier);
        }

        public void Initialize(GameplayInstance game, InputManager input, Camera camera)
        {
            _game = game;
            _input = input;
            _camera = camera;
        }

        public void ResetPlayer()
        {
            _body.position = Vector2.zero;
            _body.linearVelocity = Vector2.zero;
            _health = 100;
            _invulnerability = 0;
            AimDirection = Vector2.right;
            OnHealthChanged?.Invoke(_health, _game.Progress.MaximumHealth);
        }

        public void Heal(float amount)
        {
            _health = Mathf.Min(_game.Progress.MaximumHealth, _health + amount);
            OnHealthChanged?.Invoke(_health, _game.Progress.MaximumHealth);
        }

        public void ApplyDamage(float amount, Vector2 direction, float knockback = 0)
        {
            if (IsDead || _invulnerability > 0 || _game.State != RunState.Playing)
                return;

            _health = Mathf.Max(0, _health - amount);
            _invulnerability = 0.5f;
            _feedback.Flash(_game.Saves.Data.ReduceFlash);
            _game.Shake(0.15f);
            OnHealthChanged?.Invoke(_health, _game.Progress.MaximumHealth);
            if (IsDead)
                _game.EndRun(false);
        }
    }
}
