using UnityEngine;

namespace VonderGames.AiTesting.Gameplay.Entity
{
    public sealed class BossAttackBehaviour : IEnemyBehaviour
    {
        private readonly EnemyAgent _enemy;
        private EnemyState _state;
        private float _stateTimer;
        private Vector2 _chargeDirection;
        private bool _isNextCharge;
        public EnemyState State => _state;

        public BossAttackBehaviour(EnemyAgent enemy) => _enemy = enemy;
        public void Reset()
        {
            _state = EnemyState.Chase;
            _stateTimer = 1.5f;
            _chargeDirection = Vector2.zero;
            _isNextCharge = true;
        }
        public Vector2 Tick(Vector2 offset, float delta)
        {
            _stateTimer -= delta;
            switch (_state)
            {
                case EnemyState.Chase:
                case EnemyState.Recovery:
                    if (_stateTimer <= 0)
                    {
                        _state = _isNextCharge ? EnemyState.TelegraphCharge : EnemyState.TelegraphBurst;
                        _chargeDirection = offset.normalized;
                        _stateTimer = 1;
                        _enemy.Telegraph.enabled = true;
                        _enemy.Telegraph.color = new Color(1, 0.45f, 0.3f, 0.3f);
                        _enemy.Telegraph.transform.localScale = _isNextCharge ? new Vector3(0.55f, 5, 1) : Vector3.one * 3;
                        _enemy.Telegraph.transform.localPosition = _isNextCharge ? (Vector3)(_chargeDirection * 2.5f) : Vector3.zero;
                        _enemy.Telegraph.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(-_chargeDirection.x, _chargeDirection.y) * Mathf.Rad2Deg);
                    }
                    return _state == EnemyState.Chase ? offset.normalized * _enemy.Definition.Speed : Vector2.zero;
                case EnemyState.TelegraphCharge:
                    if (_stateTimer <= 0)
                    {
                        _enemy.Telegraph.enabled = false;
                        _state = EnemyState.Charge;
                        _stateTimer = 0.65f;
                        _enemy.Shake(0.18f);
                    }
                    return Vector2.zero;
                case EnemyState.Charge:
                    if (_stateTimer <= 0)
                        BeginRecovery();

                    return _chargeDirection * 15;
                case EnemyState.TelegraphBurst:
                    if (_stateTimer <= 0)
                    {
                        _enemy.Telegraph.enabled = false;
                        for (int i = 0; i < 16; i++)
                        {
                            float angle = i * Mathf.PI * 2 / 16;
                            _enemy.SpawnHostileProjectile(_enemy.Position, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
                        }
                        _enemy.Shake(0.12f);
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
            _stateTimer = _enemy.Health <= _enemy.MaximumHealth / 2 ? 0.65f : 1.4f;
        }

    }
}
