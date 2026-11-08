using UnityEngine;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Entity;
using VonderGames.AiTesting.Gameplay.Manager;
using VonderGames.AiTesting.Gameplay.Session;
using VonderGames.AiTesting.Gameplay.Spawning;
using VonderGames.AiTesting.UI;

namespace VonderGames.AiTesting.Gameplay.Combat
{
    public sealed class DeflectController
    {
        public bool IsDrawing => _session.State == RunState.Drawing && _caughtProjectile != null;
        public bool HasProtection => IsDrawing && _caughtProjectile.IsSuspended;
        private readonly RunSession _session;
        private readonly RunProgress _progress;
        private readonly PlayerController _player;
        private readonly InputManager _input;
        private readonly CombatDefinition _combat;
        private readonly CooldownTracker _cooldowns;
        private readonly ProjectileSpawner _projectiles;
        private readonly EnemyRegistry _registry;
        private readonly CombatDamage _damage;
        private readonly PlayerCombatController _playerCombat;
        private readonly CombatFeedback _feedback;
        private readonly UICanvasGameplayInstance _ui;
        private readonly UIGestureDrawing _drawing;
        private readonly CameraFeedback _camera;
        private SpellProjectile _caughtProjectile;

        public DeflectController(RunSession session, RunProgress progress, PlayerController player,
            InputManager input, CombatDefinition combat, CooldownTracker cooldowns,
            ProjectileSpawner projectiles, EnemyRegistry registry, CombatDamage damage,
            PlayerCombatController playerCombat, CombatFeedback feedback, UICanvasGameplayInstance ui,
            UIGestureDrawing drawing, CameraFeedback camera)
        {
            _session = session; _progress = progress; _player = player; _input = input;
            _combat = combat; _cooldowns = cooldowns; _projectiles = projectiles; _registry = registry;
            _damage = damage; _playerCombat = playerCombat; _feedback = feedback; _ui = ui;
            _drawing = drawing; _camera = camera;
        }

        public void Clear()
        {
            SpellProjectile caught = _caughtProjectile;
            _caughtProjectile = null;
            if (caught != null) caught.Remove();
        }
        public bool AttemptDeflect()
        {
            if (_session.State != RunState.Playing || !_input.CanSelectSpell || _cooldowns.GetCooldown(SpellKind.Deflect) > 0 || _progress.PendingChoices > 0)
                return false;

            _cooldowns.Start(SpellKind.Deflect);
            SpellProjectile nearest = null;
            float distance = float.MaxValue;
            foreach (var shot in _projectiles.HostileProjectiles)
            {
                if (shot == null || !shot.TryPredictImpact(_combat.DeflectWindow, out _)) continue;
                float candidate = ((Vector2)shot.transform.position - (Vector2)_player.transform.position).sqrMagnitude;
                if (candidate < distance) { distance = candidate; nearest = shot; }
            }
            if (nearest == null)
            {
                _feedback.Shield(_player.transform.position, 0.8f, new Color(0.5f, 0.6f, 0.7f), 0.2f, true);
                _feedback.Burst(_player.transform.position, Color.gray, 4, 0.3f);
                _ui.ShowCue("MISSED TIMING", 0.5f);
            }
            else
            {
                _caughtProjectile = nearest;
                nearest.Suspend();
                
                _session.SetState(RunState.Drawing);
                _drawing.Open(SpellKind.Deflect);
                _ui.ShowCue("CAUGHT — IMMUNE WHILE DRAWING");
            }
            _session.NotifyHudChanged();
            return nearest != null;
        }
        public void Resolve(bool success, string reason)
        {
            SpellProjectile shot = _caughtProjectile;
            _caughtProjectile = null;
            if (shot == null) return;

            shot.Release(success);
            if (success)
            {
                float radius = _combat.DeflectRadius * _progress.AreaMultiplier;
                for (int i = _registry.ActiveEnemies.Count - 1; i >= 0; i--)
                {
                    EnemyAgent enemy = _registry.ActiveEnemies[i];
                    Vector2 direction = enemy.transform.position - _player.transform.position;
                    if (direction.sqrMagnitude <= radius * radius)
                        _damage.DealPlayerDamage(enemy, _combat.DeflectDamage * _progress.DamageMultiplier, direction, 4);
                }
                if (!_session.IsCombatActive) return;
                _feedback.Shield(_player.transform.position, radius, Color.cyan, 0.35f);
                _feedback.Ring(_player.transform.position, radius, Color.cyan, 0.35f);
                _feedback.Burst(_player.transform.position, Color.cyan, 12, radius);
                _playerCombat.RefreshCooldowns();
                _ui.ShowCue("DEFLECT!");
                _camera.Shake(0.2f);
            }
            else
            {
                _feedback.Shield(shot.transform.position, 0.6f, new Color(0.95f, 0.4f, 0.32f), 0.25f, true);
                _feedback.TraceFailure(shot.transform.position);
                _ui.ShowDrawingFailure("CANCELLED", true);
            }
        }

    }
}
