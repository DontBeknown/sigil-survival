using System.Collections.Generic;
using UnityEngine;
using VonderGames.AiTesting.Gameplay.Entity;

namespace VonderGames.AiTesting.Gameplay.Spawning
{
    public sealed class EnemyRegistry
    {
        private readonly List<EnemyAgent> _activeEnemies = new List<EnemyAgent>();
        public IReadOnlyList<EnemyAgent> ActiveEnemies => _activeEnemies;
        public int Count => _activeEnemies.Count;
        public int RangedCount
        {
            get
            {
                int count = 0;
                foreach (EnemyAgent enemy in _activeEnemies)
                    if (!enemy.IsDead && enemy.isActiveAndEnabled && enemy.Definition.IsRanged) count++;
                return count;
            }
        }

        public void Add(EnemyAgent enemy) => _activeEnemies.Add(enemy);
        public void Remove(EnemyAgent enemy) => _activeEnemies.Remove(enemy);
        public EnemyAgent FindNearest(Vector2 origin, float range, HashSet<EnemyAgent> excluded = null)
        {
            EnemyAgent nearest = null;
            float bestDistance = range * range;
            foreach (var enemy in _activeEnemies)
            {
                if (enemy.IsDead || (excluded != null && excluded.Contains(enemy)))
                    continue;

                float distance = ((Vector2)enemy.transform.position - origin).sqrMagnitude;
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    nearest = enemy;
                }
            }
            return nearest;
        }

    }
}
