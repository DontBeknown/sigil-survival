using System;
using UnityEngine;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Entity;
using VonderGames.AiTesting.Gameplay.Session;
using VonderGames.AiTesting.Gameplay.Spawning;

namespace VonderGames.AiTesting.Gameplay.Combat
{
    public sealed class EnemyDeathController : IDisposable
    {
        private readonly EnemySpawner _enemies;
        private readonly ExperienceSpawner _experience;
        private readonly RunSession _session;
        private readonly RunProgress _progress;
        private readonly CombatDefinition _combat;
        private readonly CombatFeedback _feedback;
        private readonly System.Random _random;

        public EnemyDeathController(EnemySpawner enemies, ExperienceSpawner experience, RunSession session,
            RunProgress progress, CombatDefinition combat, CombatFeedback feedback, System.Random random)
        {
            _enemies = enemies;
            _experience = experience;
            _session = session;
            _progress = progress;
            _combat = combat;
            _feedback = feedback;
            _random = random;
            _enemies.OnEnemyDied += OnEnemyDiedHandler;
        }

        public void Dispose() => _enemies.OnEnemyDied -= OnEnemyDiedHandler;

        private void OnEnemyDiedHandler(EnemyAgent enemy)
        {
            Vector2 position = enemy.transform.position;
            bool isBoss = enemy.Definition.IsBoss;
            int amount = enemy.Definition.Experience * _combat.ExperienceMultiplier;
            _feedback.Burst(position, enemy.Definition.Color, 8, 0.8f);
            _enemies.Despawn(enemy);
            _session.RecordKill();
            if (isBoss)
            {
                _session.EndRun(true);
                return;
            }

            _experience.Spawn(position, amount, false);
            if (_random.NextDouble() < _progress.BonusDropChance)
                _experience.Spawn(position + Vector2.right * 0.3f, amount, true);
        }
    }
}
