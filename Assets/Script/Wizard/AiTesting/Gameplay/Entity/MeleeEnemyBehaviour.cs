using UnityEngine;
using VonderGames.AiTesting.Core;

namespace VonderGames.AiTesting.Gameplay.Entity
{
    public sealed class MeleeEnemyBehaviour : IEnemyBehaviour
    {
        private readonly EnemyAgent _enemy;
        public EnemyState State { get; private set; }

        public MeleeEnemyBehaviour(EnemyAgent enemy) => _enemy = enemy;
        public void Reset() => State = EnemyState.Chase;
        public Vector2 Tick(Vector2 offset, float delta)
        {
            EnemyDefinition definition = _enemy.Definition;
            State = offset.magnitude < (definition.Size + 1) * 0.5f + 0.2f ? EnemyState.Attack : EnemyState.Chase;
            return State == EnemyState.Chase ? offset.normalized * definition.Speed : Vector2.zero;
        }

    }
}
