using System;
using UnityEngine;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Combat;
using VonderGames.AiTesting.Gameplay.Manager;
using VonderGames.AiTesting.Gameplay.Session;

namespace VonderGames.AiTesting.Gameplay.Entity
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class PlayerController : MonoBehaviour, IDamageable
    {
        [SerializeField] private PrimitiveFeedback _feedback;
        [SerializeField] private Transform _aimMarker;
        public event Action<float, float> OnHealthChanged;
        public event Action OnDied;
        public event Action OnDamaged;
        public event Action OnDashUnavailable;
        public PlayerHealth HealthState { get; private set; }
        public Vector2 LastMovementDirection => _motor.LastMovementDirection;
        public Vector2 AimDirection { get; private set; } = Vector2.right;
        public float Health => HealthState.Current;
        public bool IsDead => HealthState.IsDead;
        public bool IsDashing => _dash.IsDashing;
        public bool DashedThisStep => _dash.DashedThisStep;
        public float DashStepAge => _dash.DashStepAge;
        public float DashStepDuration => _dash.DashStepDuration;
        public float DashCooldown => _dash.Cooldown;
        public Vector2 Position => _body.position;
        public Bounds HitBounds => new Bounds(_body.position + Vector2.Scale(_collider.offset, transform.lossyScale), Vector2.Scale(_collider.size, transform.lossyScale));
        private Rigidbody2D _body;
        private BoxCollider2D _collider;
        private RunSession _session;
        private GameplayClock _clock;
        private InputManager _input;
        private CameraFeedback _camera;
        private Func<bool> _hasProtection;
        private PlayerMotor _motor;
        private PlayerDash _dash;
        private PlayerPresentation _presentation;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _collider = GetComponent<BoxCollider2D>();
        }
        private void Update()
        {
            if (_session == null || !_session.IsCombatActive) return;
            HealthState.Tick(Time.deltaTime);
            _dash.Tick(_clock.PlayerDelta);
            _presentation.Tick();
            if (_input.CanSelectSpell && _input.Dash.WasPressedThisFrame()) TryDash();
        }
        private void FixedUpdate()
        {
            if (_session == null) return;
            _dash.BeginPhysicsStep();
            if (!_session.IsCombatActive) { _motor.Stop(); return; }
            if (IsDashing) _dash.Step(Time.fixedDeltaTime * _clock.PlayerPhysicsRatio);
            else _motor.Step(_clock.PlayerPhysicsRatio);
        }
        private void OnDestroy()
        {
            if (HealthState != null) HealthState.OnChanged -= OnHealthChangedHandler;
        }
        public void Initialize(RunSession session, GameplayClock clock, RunProgress progress,
            CombatDefinition combat, InputManager input, CombatFeedback feedback, SaveService saves,
            CameraFeedback camera, Func<bool> hasProtection)
        {
            _session = session; _clock = clock; _input = input; _camera = camera; _hasProtection = hasProtection;
            HealthState = new PlayerHealth(progress);
            HealthState.OnChanged += OnHealthChangedHandler;
            _motor = new PlayerMotor(_body, progress, input);
            _dash = new PlayerDash(this, _body, _collider, combat, feedback, _feedback, _motor);
            _presentation = new PlayerPresentation(this, _feedback, _aimMarker, feedback, saves, hasProtection);
            ClearTemporary();
        }
        public bool TryDash()
        {
            if (!_session.IsCombatActive || IsDashing) return false;
            if (DashCooldown > 0) { OnDashUnavailable?.Invoke(); return false; }
            return _dash.TryDash();
        }
        public void RefreshDash() => _dash.RefreshDash();
        public bool TryRewardPerfectDodge() => _dash.TryRewardPerfectDodge();
        public void SetDanger(float strength) => _presentation.SetDanger(strength);
        public void ClearTemporary() { _dash.Reset(); _presentation.Clear(); }
        public void ResetPlayer()
        {
            ClearTemporary();
            _motor.Reset();
            AimDirection = Vector2.right;
            _presentation.Reset();
            HealthState.Reset();
        }
        public void ShowShot(Vector2 direction) { AimDirection = direction; _presentation.ShowShot(direction); }
        public void Heal(float amount) => HealthState.Heal(amount);
        public void ApplyDamage(float amount, Vector2 direction, float knockback = 0)
        {
            if (!_session.IsCombatActive || IsDashing || _hasProtection() || !HealthState.TryDamage(amount)) return;
            _presentation.ShowDamage(amount);
            _camera.Shake(0.15f);
            if (IsDead) OnDied?.Invoke();
            else OnDamaged?.Invoke();
        }
        private void OnHealthChangedHandler(float current, float maximum) => OnHealthChanged?.Invoke(current, maximum);

    }
}
