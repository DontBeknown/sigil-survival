using UnityEngine;
using VonderGames.AiTesting.Core;

namespace VonderGames.AiTesting.Gameplay.Entity
{
    public sealed class RangedEnemyBehaviour : IEnemyBehaviour
    {
        private readonly EnemyAgent _enemy;
        private float _stateTimer;
        public EnemyState State { get; private set; }
        public bool IsAimReady => State == EnemyState.RangedAim && _stateTimer <= 0;

        public RangedEnemyBehaviour(EnemyAgent enemy) => _enemy = enemy;
        public void Reset() { State = EnemyState.Chase; _stateTimer = 0; }
        public void BeginAim() { State = EnemyState.RangedAim; _stateTimer = _enemy.Definition.AimDuration; }
        public void CancelAim() => State = EnemyState.Chase;
        public Vector2 Tick(Vector2 offset, float delta)
        {
            _stateTimer -= delta;
            EnemyDefinition definition = _enemy.Definition;
            if (State == EnemyState.RangedAim)
            {
                float size = Mathf.Lerp(0.25f, 1.2f, Mathf.Clamp01(_stateTimer / definition.AimDuration));
                _enemy.Telegraph.transform.localScale = Vector3.one * size;
                _enemy.AimRing.transform.localScale = Vector3.one * size;
                return Vector2.zero;
            }
            float distance = offset.magnitude;
            return offset.normalized * definition.Speed * (distance < definition.PreferredMinimum ? -1 : distance > definition.PreferredMaximum ? 1 : 0);
        }

    }
}
