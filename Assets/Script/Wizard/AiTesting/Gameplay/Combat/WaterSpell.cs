using UnityEngine;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Entity;
using VonderGames.AiTesting.Gameplay.Spawning;

namespace VonderGames.AiTesting.Gameplay.Combat
{
    public sealed class WaterSpell : ISpell
    {
        private readonly PlayerController _player;
        private readonly EnemyRegistry _registry;
        private readonly RunProgress _progress;
        private readonly CombatDamage _damage;
        private readonly CombatFeedback _feedback;
        private readonly SpellDefinition _spell;

        public WaterSpell(PlayerController player, EnemyRegistry registry, RunProgress progress, CombatDamage damage, CombatFeedback feedback, SpellDefinition spell)
        {
            _player = player;
            _registry = registry;
            _progress = progress;
            _damage = damage;
            _feedback = feedback;
            _spell = spell;
        }

        public void Cast(float damage, bool isCombo)
        {
            Vector2 position = _player.transform.position;
                float radius = _spell.Radius * _progress.AreaMultiplier;
                for (int i = _registry.ActiveEnemies.Count - 1; i >= 0; i--)
                {
                    var enemy = _registry.ActiveEnemies[i];
                    Vector2 direction = (Vector2)enemy.transform.position - position;
                    if (direction.sqrMagnitude > radius * radius)
                        continue;

                    enemy.Stun(_progress.StunDuration);
                    _damage.DealPlayerDamage(enemy, damage, direction, 5);
                }
                _feedback.Ring(position, radius, _spell.Color, 0.45f);
                _feedback.Burst(position, _spell.Color, 14, radius);
        }

    }
}
