using UnityEngine;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Entity;
using VonderGames.AiTesting.Gameplay.Spawning;

namespace VonderGames.AiTesting.Gameplay.Combat
{
    public sealed class FireSpell : ISpell
    {
        private readonly PlayerController _player;
        private readonly EnemyRegistry _registry;
        private readonly ProjectileSpawner _projectiles;
        private readonly CombatFeedback _feedback;
        private readonly SpellDefinition _spell;

        public FireSpell(PlayerController player, EnemyRegistry registry, ProjectileSpawner projectiles,
            CombatFeedback feedback, SpellDefinition spell)
        {
            _player = player;
            _registry = registry;
            _projectiles = projectiles;
            _feedback = feedback;
            _spell = spell;
        }

        public void Cast(float damage, bool isCombo)
        {
            Vector2 position = _player.transform.position;
            EnemyAgent target = _registry.FindNearest(position, float.MaxValue);
            Vector2 direction = target != null ? ((Vector2)target.transform.position - position).normalized : _player.LastMovementDirection;
            _projectiles.LaunchProjectile(SpellKind.Fire, direction);
            _feedback.Burst(position + direction * 0.65f, _spell.Color, 10, 0.7f);
        }

    }
}
