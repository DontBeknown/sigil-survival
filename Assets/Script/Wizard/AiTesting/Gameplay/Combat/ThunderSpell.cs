using System.Collections.Generic;
using UnityEngine;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Entity;
using VonderGames.AiTesting.Gameplay.Spawning;
using VonderGames.AiTesting.UI;

namespace VonderGames.AiTesting.Gameplay.Combat
{
    public sealed class ThunderSpell : ISpell
    {
        private readonly PlayerController _player;
        private readonly EnemyRegistry _registry;
        private readonly RunProgress _progress;
        private readonly CombatDefinition _combat;
        private readonly CombatDamage _damage;
        private readonly CombatFeedback _feedback;
        private readonly UICanvasGameplayInstance _ui;
        private readonly SpellDefinition _spell;
        private readonly HashSet<EnemyAgent> _visited = new HashSet<EnemyAgent>();

        public ThunderSpell(PlayerController player, EnemyRegistry registry, RunProgress progress, CombatDefinition combat, CombatDamage damage, CombatFeedback feedback, UICanvasGameplayInstance ui, SpellDefinition spell)
        {
            _player = player;
            _registry = registry;
            _progress = progress;
            _combat = combat;
            _damage = damage;
            _feedback = feedback;
            _ui = ui;
            _spell = spell;
        }

        public void Cast(float damage, bool isCombo)
        {
            Vector2 position = _player.transform.position;
                _visited.Clear();
                EnemyAgent target = _registry.FindNearest(position, _combat.ThunderRange);
                int count = _progress.ThunderTargets + (isCombo ? 2 : 0);
                for (int i = 0; i < count && target != null; i++)
                {
                    _visited.Add(target);
                    Vector2 next = target.transform.position;
                    _feedback.Lightning(position, next, _spell.Color, isCombo ? 0.18f : 0.1f);
                    _damage.DealPlayerDamage(target, damage, next - position, 2);
                    position = next;
                    target = _registry.FindNearest(position, _progress.GetThunderLinkRange(_combat), _visited);
                }
                if (_visited.Count == 0)
                {
                    _feedback.Burst(position, _spell.Color, 8, 0.7f);
                    _ui.ShowCue("NO TARGET");
                }
        }

    }
}
