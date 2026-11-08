using UnityEngine;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Entity;
using VonderGames.AiTesting.Gameplay.Session;
using VonderGames.AiTesting.UI;

namespace VonderGames.AiTesting.Gameplay.Combat
{
    public sealed class PlayerCombatController
    {
        private readonly RunSession _session;
        private readonly RunProgress _progress;
        private readonly PlayerController _player;
        private readonly CombatDefinition _combat;
        private readonly GameplayClock _clock;
        private readonly CooldownTracker _cooldowns;
        private readonly CombatFeedback _feedback;
        private readonly UICanvasGameplayInstance _ui;
        private readonly CameraFeedback _camera;

        public PlayerCombatController(RunSession session, RunProgress progress, PlayerController player,
            CombatDefinition combat, GameplayClock clock, CooldownTracker cooldowns,
            CombatFeedback feedback, UICanvasGameplayInstance ui, CameraFeedback camera)
        {
            _session = session; _progress = progress; _player = player; _combat = combat;
            _clock = clock; _cooldowns = cooldowns; _feedback = feedback; _ui = ui; _camera = camera;
        }

        public void RefreshCooldowns()
        {
            _cooldowns.Refresh();
            _player.RefreshDash();
            _ui.ShowReset();
            _session.NotifyHudChanged();
        }
        public void PerfectDodge()
        {
            if (!_session.IsCombatActive) return;
            float previousHealth = _player.Health;
            _player.Heal(_progress.MaximumHealth * _combat.PerfectHealFraction);
            float healed = _player.Health - previousHealth;
            _clock.BeginPerfectDodge();
            RefreshCooldowns();
            _feedback.Announce(_player.transform.position, healed > 0 ? $"PERFECT DODGE +{healed:0.#} HP" : "PERFECT DODGE", Color.cyan);
            _feedback.Ring(_player.transform.position, 1.2f, Color.cyan, 0.3f);
            _camera.Shake(0.08f);
        }

    }
}
