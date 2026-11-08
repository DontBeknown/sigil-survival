using System;
using UnityEngine;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Session;

namespace VonderGames.AiTesting.Gameplay.Entity
{
    public enum EnemyState { Chase, Attack, TelegraphCharge, Charge, TelegraphBurst, Recovery, Dead, RangedAim }

    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyAgent : MonoBehaviour, IDamageable
    {
        [SerializeField] private PrimitiveFeedback _feedback;
        [SerializeField] private SpriteRenderer _telegraph;
        [SerializeField] private SpriteRenderer _stunMarker;

        private Rigidbody2D _body;
        private RunSession _session;
        private PlayerController _player;
        private RunProgress _progress;
        private CombatDefinition _combat;
        private CombatFeedback _combatFeedback;
        private SaveService _saves;
        private Func<Vector2, Vector2, EnemyAgent, SpellProjectile> _spawnProjectile;
        private Action<float> _shake;
        private Action<EnemyAgent> _onDied;
        private Action<EnemyAgent> _onBossHealthChanged;
        private IEnemyBehaviour _behaviour;
        private MeleeEnemyBehaviour _melee;
        private RangedEnemyBehaviour _ranged;
        private BossAttackBehaviour _boss;
        private bool _isDead;
        public int LeaseVersion { get; private set; }
        internal SpriteRenderer Telegraph => _telegraph;
        internal LineRenderer AimRing => _aimRing;
        public Vector2 Position => _body.position;

        private EnemyDefinition _definition;
        public EnemyDefinition Definition => _definition;
        private float _health;
        public float Health => _health;
        private float _maximumHealth;
        public float MaximumHealth => _maximumHealth;
        public bool IsDead => _isDead;
        public EnemyState State => IsDead ? EnemyState.Dead : _behaviour.State;
        private float _attackTimer;
        private float _slowTimer;
        public bool IsStunned => _slowTimer > 0;
        private Vector2 _knockback;
        private readonly Collider2D[] _neighbors = new Collider2D[12];
        private ContactFilter2D _separationFilter;
        private LineRenderer _aimRing;
        public bool CanFireRanged => _definition != null && _definition.IsRanged && !IsDead && !IsStunned && _attackTimer <= 0 &&
            Vector2.Distance(transform.position, _player.transform.position) >= _definition.PreferredMinimum &&
            Vector2.Distance(transform.position, _player.transform.position) <= _definition.PreferredMaximum;
        public bool IsAimReady => _ranged.IsAimReady;

        private void Awake() => CacheComponents();

        private void CacheComponents()
        {
            _body = GetComponent<Rigidbody2D>();
            _separationFilter = new ContactFilter2D { useLayerMask = true, layerMask = 1 << 9 };
        }

        private void FixedUpdate()
        {
            if (!_session.IsCombatActive || IsDead)
                return;

            float delta = Time.fixedDeltaTime;
            if (_slowTimer > 0)
            {
                _slowTimer = Mathf.Max(0, _slowTimer - delta);
                _stunMarker.enabled = _slowTimer > 0;
                _body.linearVelocity = Vector2.zero;
                return;
            }
            _attackTimer -= delta;
            _slowTimer = Mathf.Max(0, _slowTimer - delta);
            _knockback = Vector2.MoveTowards(_knockback, Vector2.zero, 18 * delta);
            Vector2 offset = (Vector2)_player.transform.position - _body.position;
            float distance = offset.magnitude;
            Vector2 movement = _behaviour.Tick(offset, delta);

            if (distance < (_definition.Size + 1) * 0.5f + 0.25f && _attackTimer <= 0)
            {
                _player.ApplyDamage(_definition.Damage, offset.normalized);
                _attackTimer = _definition.AttackCooldown;
            }

            if (!_definition.IsBoss)
            {
                int count = Physics2D.OverlapCircle(_body.position, _definition.Size + 0.5f, _separationFilter, _neighbors);
                Vector2 separation = Vector2.zero;
                for (int i = 0; i < count; i++)
                {
                    if (_neighbors[i].attachedRigidbody == _body)
                        continue;

                    Vector2 away = _body.position - (Vector2)_neighbors[i].transform.position;
                    separation += away / Mathf.Max(away.sqrMagnitude, 0.1f);
                }
                movement += Vector2.ClampMagnitude(separation, 1.5f);
            }
            _body.linearVelocity = movement * (_slowTimer > 0 ? 0.5f : 1) + _knockback;
            _body.position = new Vector2(Mathf.Clamp(_body.position.x, -29 + _definition.Size / 2, 29 - _definition.Size / 2),
                Mathf.Clamp(_body.position.y, -19 + _definition.Size / 2, 19 - _definition.Size / 2));
        }

        public void Initialize(RunSession session, PlayerController player, RunProgress progress,
            CombatDefinition combat, CombatFeedback combatFeedback, SaveService saves,
            Func<Vector2, Vector2, EnemyAgent, SpellProjectile> spawnProjectile, Action<float> shake,
            Action<EnemyAgent> onDied, Action<EnemyAgent> onBossHealthChanged, EnemyDefinition definition)
        {
            CacheComponents();
            _feedback.InitializeVisual();
            _feedback.ResetVisual();
            _session = session; _player = player; _progress = progress; _combat = combat;
            _combatFeedback = combatFeedback; _saves = saves; _spawnProjectile = spawnProjectile;
            _shake = shake; _onDied = onDied; _onBossHealthChanged = onBossHealthChanged;
            LeaseVersion++;
            _isDead = false;
            _attackTimer = _slowTimer = 0;
            _knockback = Vector2.zero;
            _body.linearVelocity = Vector2.zero;
            _body.position = transform.position;
            if (_melee == null)
            {
                _melee = new MeleeEnemyBehaviour(this);
                _ranged = new RangedEnemyBehaviour(this);
                _boss = new BossAttackBehaviour(this);
            }

            _definition = definition;
            _maximumHealth = definition.Health * (definition.IsBoss ? 1 + (_progress.Level - 1) * _combat.BossHealthPerLevel : 1);
            _health = _maximumHealth;
            _behaviour = definition.IsBoss ? (IEnemyBehaviour)_boss : definition.IsRanged ? (IEnemyBehaviour)_ranged : _melee;
            _behaviour.Reset();
            transform.localScale = Vector3.one * definition.Size;
            _feedback.SetColor(definition.Color);
            if (definition.IsRanged)
            {
                _feedback.Renderer.transform.localRotation = Quaternion.Euler(0, 0, 45);
                if (_aimRing == null) _aimRing = _combatFeedback.CreateIndicator("FiringFocus", transform, new Color(1, 0.5f, 0.7f), 18);
                _aimRing.positionCount = 33;
                for (int i = 0; i < 33; i++)
                {
                    float angle = i * Mathf.PI * 2 / 32;
                    _aimRing.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)));
                }
                _aimRing.enabled = false;
            }
            _telegraph.enabled = false;
            _stunMarker.enabled = false;
            _telegraph.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            _telegraph.transform.localScale = Vector3.one;
        }

        public void Stun(float duration)
        {
            _slowTimer = duration;
            _stunMarker.enabled = true;
            _body.linearVelocity = Vector2.zero;
        }

        public void ApplyDamage(float amount, Vector2 direction, float knockback = 0)
        {
            ReceivePlayerHit(amount, direction, knockback, false);
        }

        public void BeginAim()
        {
            _ranged.BeginAim();
            _telegraph.enabled = true;
            _telegraph.color = _saves.Data.ReduceFlash ? new Color(0.8f, 0.5f, 0.6f, 0.4f) : new Color(1, 0.4f, 0.6f, 0.65f);
            _telegraph.transform.localPosition = Vector3.zero;
            _aimRing.enabled = true;
        }

        public void CancelAim()
        {
            if (IsDead) return;
            _ranged.CancelAim();
            _telegraph.enabled = false;
            if (_aimRing != null) _aimRing.enabled = false;
        }

        public void LaunchRanged()
        {
            Vector2 direction = (_player.transform.position - transform.position).normalized;
            _spawnProjectile(transform.position, direction, this);
            _combatFeedback.Burst((Vector2)transform.position + direction * 0.6f, _definition.Color, 5, 0.35f);
            _attackTimer = _definition.AttackCooldown;
            CancelAim();
        }

        public void ReceivePlayerHit(float amount, Vector2 direction, float knockback, bool critical)
        {
            if (IsDead || !_session.IsCombatActive)
                return;

            _combatFeedback.Number(transform.position, amount, false, critical);
            _combatFeedback.Burst(transform.position, _definition.Color, critical ? 6 : 3, critical ? 0.5f : 0.3f);
            _feedback.Pop();
            _health = Mathf.Max(0, _health - amount);
            _knockback += direction.normalized * knockback * (_definition.IsBoss ? 0.2f : 1);
            _feedback.Flash(_saves.Data.ReduceFlash);
            if (_definition.IsBoss)
                _onBossHealthChanged(this);

            if (_health <= 0)
            {
                _isDead = true;
                _body.linearVelocity = Vector2.zero;
                _onDied(this);

            }
        }

        internal void Shake(float strength) => _shake(strength);
        internal void SpawnHostileProjectile(Vector2 position, Vector2 direction) => _spawnProjectile(position, direction, this);

        public void ResetForPool()
        {
            _isDead = true;
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0;
            _knockback = Vector2.zero;
            _attackTimer = _slowTimer = 0;
            _telegraph.enabled = _stunMarker.enabled = false;
            if (_aimRing != null) _aimRing.enabled = false;
            _feedback.ResetVisual();
            Array.Clear(_neighbors, 0, _neighbors.Length);
        }
    }
}
