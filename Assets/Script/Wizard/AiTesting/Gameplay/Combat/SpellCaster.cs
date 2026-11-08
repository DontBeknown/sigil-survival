using UnityEngine;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Entity;
using VonderGames.AiTesting.Gameplay.Session;
using VonderGames.AiTesting.Gameplay.Spawning;
using VonderGames.AiTesting.UI;

namespace VonderGames.AiTesting.Gameplay.Combat
{
    public sealed class SpellCaster
    {
        public float ComboRemaining => _combo.Remaining;
        private readonly SpellCombo _combo = new SpellCombo();
        private readonly ISpell[] _effects = new ISpell[5];
        private readonly RunSession _session;
        private readonly RunProgress _progress;
        private readonly PlayerController _player;
        private readonly SpellDefinition[] _spells;
        private readonly CooldownTracker _cooldowns;
        private readonly ProjectileSpawner _projectiles;
        private readonly EnemyRegistry _registry;
        private readonly CombatFeedback _feedback;
        private readonly UICanvasGameplayInstance _ui;
        private readonly CameraFeedback _camera;

        public SpellCaster(RunSession session, RunProgress progress, PlayerController player,
            SpellDefinition[] spells, CooldownTracker cooldowns, ProjectileSpawner projectiles,
            EnemyRegistry registry, CombatDamage damage, CombatDefinition combat,
            CombatFeedback feedback, UICanvasGameplayInstance ui, CameraFeedback camera)
        {
            _session = session; _progress = progress; _player = player; _spells = spells;
            _cooldowns = cooldowns; _projectiles = projectiles; _registry = registry;
            _feedback = feedback; _ui = ui; _camera = camera;
            _effects[(int)SpellKind.Fire] = new FireSpell(player, registry, projectiles, feedback, spells[(int)SpellKind.Fire]);
            _effects[(int)SpellKind.Water] = new WaterSpell(player, registry, progress, damage, feedback, spells[(int)SpellKind.Water]);
            _effects[(int)SpellKind.Thunder] = new ThunderSpell(player, registry, progress, combat, damage, feedback, ui, spells[(int)SpellKind.Thunder]);
        }
        public void Reset() => _combo.Reset();
        public void Advance(float delta) => _combo.Advance(delta);
        public void TickAutoAttack()
        {
            if (_cooldowns.GetCooldown(SpellKind.Bolt) > 0) return;
            _cooldowns.StartBoltOpportunity();
            EnemyAgent target = _registry.FindNearest(_player.transform.position, 8);
            if (target != null)
                _projectiles.LaunchProjectile(SpellKind.Bolt, ((Vector2)target.transform.position - (Vector2)_player.transform.position).normalized);
        }
        public void CastElement(SpellKind kind)
        {
            var spell = _spells[(int)kind];
            _cooldowns.Start(kind);
            bool isCombo = _combo.Cast(kind);
            float damage = _progress.GetDamage(spell) * (isCombo ? 1.5f : 1);
            _effects[(int)kind].Cast(damage, isCombo);
            if (!_session.IsCombatActive) return;
            if (isCombo)
            {
                _ui.ShowCue("SHATTER STORM!");
                _feedback.Ring(_player.transform.position, 2, spell.Color, 0.35f);
                _feedback.Burst(_player.transform.position, new Color(0.7f, 0.85f, 1), 20, 2);
            }
            _camera.Shake(isCombo ? 0.28f : 0.16f);
            _session.NotifyHudChanged();
        }

    }
}
