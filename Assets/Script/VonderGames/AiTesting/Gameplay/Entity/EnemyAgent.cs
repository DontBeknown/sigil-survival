using UnityEngine;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Instance;

namespace VonderGames.AiTesting.Gameplay.Entity
{
    public enum EnemyState { Chase, Attack, TelegraphCharge, Charge, TelegraphBurst, Recovery, Dead }

    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyAgent : MonoBehaviour, IDamageable
    {
        [SerializeField] private PrimitiveFeedback _feedback;
        [SerializeField] private SpriteRenderer _telegraph;

        private Rigidbody2D _body;
        private GameplayInstance _game;
        private EnemyDefinition _definition;
        public EnemyDefinition Definition => _definition;
        private float _health;
        public float Health => _health;
        public bool IsDead => _state == EnemyState.Dead;
        private EnemyState _state;
        public EnemyState State => _state;
        private float _attackTimer;
        private float _stateTimer;
        private float _slowTimer;
        private Vector2 _knockback;
        private Vector2 _chargeDirection;
        private bool _isNextCharge = true;
        private readonly Collider2D[] _neighbors = new Collider2D[12];
        private ContactFilter2D _separationFilter;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _separationFilter = new ContactFilter2D { useLayerMask = true, layerMask = 1 << 9 };
        }

        private void FixedUpdate()
        {
            if (_game.State != RunState.Playing || IsDead)
                return;

            float delta = Time.fixedDeltaTime;
            _attackTimer -= delta;
            _stateTimer -= delta;
            _slowTimer = Mathf.Max(0, _slowTimer - delta);
            _knockback = Vector2.MoveTowards(_knockback, Vector2.zero, 18 * delta);
            Vector2 offset = (Vector2)_game.Player.transform.position - _body.position;
            float distance = offset.magnitude;
            Vector2 movement = Vector2.zero;
            if (_definition.IsBoss)
                movement = TickBoss(offset, delta);
            else
            {
                _state = distance < (_definition.Size + 1) * 0.5f + 0.2f ? EnemyState.Attack : EnemyState.Chase;
                if (_state == EnemyState.Chase)
                    movement = offset.normalized * _definition.Speed;
            }

            if (distance < (_definition.Size + 1) * 0.5f + 0.25f && _attackTimer <= 0)
            {
                _game.Player.ApplyDamage(_definition.Damage, offset.normalized);
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

        public void Initialize(GameplayInstance game, EnemyDefinition definition)
        {
            _game = game;
            _definition = definition;
            _health = definition.Health;
            _state = EnemyState.Chase;
            _stateTimer = definition.IsBoss ? 1.5f : 0;
            transform.localScale = Vector3.one * definition.Size;
            _feedback.SetColor(definition.Color);
            _telegraph.enabled = false;
        }

        public void Slow()
        {
            _slowTimer = 2;
        }

        public void ApplyDamage(float amount, Vector2 direction, float knockback = 0)
        {
            if (IsDead || _game.State != RunState.Playing)
                return;

            _health = Mathf.Max(0, _health - amount);
            _knockback += direction.normalized * knockback * (_definition.IsBoss ? 0.2f : 1);
            _feedback.Flash(_game.Saves.Data.ReduceFlash);
            if (_definition.IsBoss)
                _game.NotifyBossHealth(this);

            if (_health <= 0)
            {
                _state = EnemyState.Dead;
                _body.linearVelocity = Vector2.zero;
                _game.EnemyDied(this);
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
        }

        private Vector2 TickBoss(Vector2 offset, float delta)
        {
            switch (_state)
            {
                case EnemyState.Chase:
                case EnemyState.Recovery:
                    if (_stateTimer <= 0)
                    {
                        _state = _isNextCharge ? EnemyState.TelegraphCharge : EnemyState.TelegraphBurst;
                        _chargeDirection = offset.normalized;
                        _stateTimer = 1;
                        _telegraph.enabled = true;
                        _telegraph.color = new Color(1, 0.45f, 0.3f, 0.3f);
                        _telegraph.transform.localScale = _isNextCharge ? new Vector3(0.55f, 5, 1) : Vector3.one * 3;
                        _telegraph.transform.localPosition = _isNextCharge ? (Vector3)(_chargeDirection * 2.5f) : Vector3.zero;
                        _telegraph.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(-_chargeDirection.x, _chargeDirection.y) * Mathf.Rad2Deg);
                    }
                    return _state == EnemyState.Chase ? offset.normalized * _definition.Speed : Vector2.zero;
                case EnemyState.TelegraphCharge:
                    if (_stateTimer <= 0)
                    {
                        _telegraph.enabled = false;
                        _state = EnemyState.Charge;
                        _stateTimer = 0.65f;
                        _game.Shake(0.18f);
                    }
                    return Vector2.zero;
                case EnemyState.Charge:
                    if (_stateTimer <= 0)
                        BeginRecovery();

                    return _chargeDirection * 15;
                case EnemyState.TelegraphBurst:
                    if (_stateTimer <= 0)
                    {
                        _telegraph.enabled = false;
                        for (int i = 0; i < 16; i++)
                        {
                            float angle = i * Mathf.PI * 2 / 16;
                            _game.SpawnHostileProjectile(_body.position, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
                        }
                        _game.Shake(0.12f);
                        BeginRecovery();
                    }
                    return Vector2.zero;
                default:
                    return Vector2.zero;
            }
        }

        private void BeginRecovery()
        {
            _isNextCharge = !_isNextCharge;
            _state = EnemyState.Recovery;
            _stateTimer = _health <= _definition.Health / 2 ? 0.65f : 1.4f;
        }
    }
}
