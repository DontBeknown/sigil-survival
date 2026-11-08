using System.Collections.Generic;
using UnityEngine;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Entity;

namespace VonderGames.AiTesting.Gameplay.Spawning
{
    public sealed class RangedAttackScheduler
    {
        private readonly CombatDefinition _combat;
        private readonly Queue<EnemyAgent> _firingQueue = new Queue<EnemyAgent>();
        private EnemyAgent _firingEnemy;
        private float _launchTimer;

        public RangedAttackScheduler(CombatDefinition combat) => _combat = combat;
        public void Add(EnemyAgent enemy)
        {
            if (enemy.Definition.IsRanged) _firingQueue.Enqueue(enemy);
        }

        public void Remove(EnemyAgent enemy)
        {
            if (_firingEnemy == enemy)
            {
                enemy.CancelAim();
                _firingEnemy = null;
            }
            int count = _firingQueue.Count;
            for (int i = 0; i < count; i++)
            {
                EnemyAgent queued = _firingQueue.Dequeue();
                if (queued != enemy) _firingQueue.Enqueue(queued);
            }
        }

        public void Clear()
        {
            if (_firingEnemy != null) _firingEnemy.CancelAim();
            _firingEnemy = null;
            _firingQueue.Clear();
            _launchTimer = 0;
        }
        public void Tick(float delta)
        {
            _launchTimer = Mathf.Max(0, _launchTimer - delta);
            if (_firingEnemy != null)
            {
                if (!_firingEnemy.CanFireRanged) { _firingEnemy.CancelAim(); _firingEnemy = null; }
                else if (_firingEnemy.IsAimReady && _launchTimer <= 0)
                {
                    _firingEnemy.LaunchRanged();
                    _firingEnemy = null;
                    _launchTimer = _combat.LaunchInterval;
                }
                return;
            }
            if (_launchTimer > 0) return;
            int count = _firingQueue.Count;
            for (int i = 0; i < count; i++)
            {
                EnemyAgent enemy = _firingQueue.Dequeue();
                if (enemy == null || enemy.IsDead) continue;
                _firingQueue.Enqueue(enemy);
                if (!enemy.CanFireRanged) continue;
                _firingEnemy = enemy;
                enemy.BeginAim();
                break;
            }
        }

    }
}
