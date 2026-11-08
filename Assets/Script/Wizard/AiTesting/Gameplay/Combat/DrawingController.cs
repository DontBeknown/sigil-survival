using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Entity;
using VonderGames.AiTesting.Gameplay.Manager;
using VonderGames.AiTesting.Gameplay.Progression;
using VonderGames.AiTesting.Gameplay.Session;
using VonderGames.AiTesting.UI;

namespace VonderGames.AiTesting.Gameplay.Combat
{
    public sealed class DrawingController
    {
        private readonly RunSession _session;
        private readonly ProgressionController _progression;
        private readonly InputManager _input;
        private readonly CooldownTracker _cooldowns;
        private readonly DeflectController _deflect;
        private readonly SpellCaster _spells;
        private readonly UIGestureDrawing _drawing;
        private readonly PlayerController _player;
        private readonly CombatFeedback _feedback;
        private readonly UICanvasGameplayInstance _ui;
        private SpellKind _drawingSpell;

        public DrawingController(RunSession session, ProgressionController progression, InputManager input,
            CooldownTracker cooldowns, DeflectController deflect, SpellCaster spells, UIGestureDrawing drawing,
            PlayerController player, CombatFeedback feedback, UICanvasGameplayInstance ui)
        {
            _session = session; _progression = progression; _input = input; _cooldowns = cooldowns;
            _deflect = deflect; _spells = spells; _drawing = drawing; _player = player; _feedback = feedback; _ui = ui;
        }

        public void InterruptDrawing()
        {
            if (_deflect.IsDrawing) FinishDrawing(false, "INTERRUPTED");
        }
        public bool BeginDrawing(SpellKind kind)
        {
            if (_session.State != RunState.Playing || !_input.CanSelectSpell || kind == SpellKind.Bolt || kind == SpellKind.Deflect ||
                !_progression.Progress.IsUnlocked(kind) || _cooldowns.GetCooldown(kind) > 0 || _cooldowns.DrawingLockout > 0)
                return false;

            if (_progression.Progress.PendingChoices > 0)
            {
                _progression.ShowUpgradeChoices();
                return false;
            }
            _drawingSpell = kind;
            _session.SetState(RunState.Drawing);
            _drawing.Open(kind);
            return true;
        }
        public void FinishDrawing(bool isSuccessful, string reason)
        {
            if (_session.State != RunState.Drawing)
                return;

            if (_deflect.IsDrawing && !isSuccessful && reason != "CANCELLED" && reason != "Boss encounter")
            {
                _drawing.RetryDeflect();
                return;
            }
            _drawing.Close();
            if (_deflect.IsDrawing)
                _deflect.Resolve(isSuccessful, reason);
            else if (isSuccessful)
                _spells.CastElement(_drawingSpell);
            else
            {
                _cooldowns.LockDrawing();
                _feedback.TraceFailure(_player.transform.position);
                _ui.ShowDrawingFailure(reason, false);
            }
            if (!_session.IsCombatActive)
                return;

            if (_progression.Progress.PendingChoices > 0)
                _progression.ShowUpgradeChoices();
            else
                _session.SetState(RunState.Playing);
        }
        public void TogglePause()
        {
            if (_session.State == RunState.Drawing)
                FinishDrawing(false, "CANCELLED");
            else if (_session.State == RunState.Playing)
                _session.SetState(RunState.Paused);
            else if (_session.State == RunState.Paused)
                _session.SetState(RunState.Playing);
        }

    }
}
