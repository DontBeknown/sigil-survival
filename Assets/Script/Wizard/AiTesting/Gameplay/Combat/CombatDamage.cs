using UnityEngine;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Entity;
using VonderGames.AiTesting.Gameplay.Session;
using VonderGames.AiTesting.Gameplay.Spawning;

namespace VonderGames.AiTesting.Gameplay.Combat
{
    public sealed class CombatDamage
    {
        private readonly RunSession _session;
        private readonly RunProgress _progress;
        private readonly CombatDefinition _combat;
        private readonly SpellDefinition[] _spells;
        private readonly EnemyRegistry _registry;
        private readonly CombatFeedback _feedback;
        private readonly System.Random _random;

        public CombatDamage(RunSession session, RunProgress progress, CombatDefinition combat,
            SpellDefinition[] spells, EnemyRegistry registry, CombatFeedback feedback, System.Random random)
        {
            _session = session;
            _progress = progress;
            _combat = combat;
            _spells = spells;
            _registry = registry;
            _feedback = feedback;
            _random = random;
        }
        public void DealPlayerDamage(EnemyAgent enemy, float damage, Vector2 direction, float knockback = 0)
        {
            bool critical = _random.NextDouble() < _progress.CriticalChance;
            enemy.ReceivePlayerHit(damage * (critical ? 2 : 1), direction, knockback, critical);
        }
        public void ExplodeFire(Vector2 position, float damage, EnemyHitHistory visited)
        {
            if (!_session.IsCombatActive) return;
            float radius = _combat.FireExplosionRadius * _progress.AreaMultiplier;
            Color color = _spells[(int)SpellKind.Fire].Color;
            _feedback.Ring(position, radius, color, 0.3f);
            _feedback.Ring(position, radius * 0.55f, Color.Lerp(color, Color.white, 0.35f), 0.18f);
            _feedback.Burst(position, color, 14, radius * 0.65f);
            for (int i = _registry.ActiveEnemies.Count - 1; i >= 0 && _session.IsCombatActive; i--)
            {
                EnemyAgent enemy = _registry.ActiveEnemies[i];
                Vector2 direction = (Vector2)enemy.transform.position - position;
                if (enemy.IsDead || direction.sqrMagnitude > radius * radius || !visited.Add(enemy)) continue;
                // Piercing and overlapping explosions share one damage allowance per enemy per fireball.
                DealPlayerDamage(enemy, damage, direction, 4);
            }
        }

    }
}
