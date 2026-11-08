using System;
using System.Collections.Generic;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Entity;
using VonderGames.AiTesting.Gameplay.Manager;
using VonderGames.AiTesting.Gameplay.Session;
using VonderGames.AiTesting.UI;

namespace VonderGames.AiTesting.Gameplay.Progression
{
    public sealed class ProgressionController : IDisposable
    {
        public RunProgress Progress { get; } = new RunProgress();
        public IReadOnlyList<UpgradeOffer> Choices => _choices;
        private List<UpgradeOffer> _choices = new List<UpgradeOffer>();
        private readonly RunSession _session;
        private readonly PlayerController _player;
        private readonly InputManager _input;
        private readonly UICanvasGameplayInstance _ui;
        private readonly UpgradeDefinition[] _upgrades;
        private readonly SpellDefinition[] _spells;
        private readonly CombatDefinition _combat;
        private readonly System.Random _random;

        public ProgressionController(RunSession session, PlayerController player, InputManager input,
            UICanvasGameplayInstance ui, UpgradeDefinition[] upgrades, SpellDefinition[] spells,
            CombatDefinition combat, System.Random random)
        {
            _session = session;
            _player = player;
            _input = input;
            _ui = ui;
            _upgrades = upgrades;
            _spells = spells;
            _combat = combat;
            _random = random;
            Progress.OnChanged += OnProgressChangedHandler;
        }

        public void Reset() { _choices.Clear(); Progress.Reset(); }
        public void Dispose() => Progress.OnChanged -= OnProgressChangedHandler;
        private void OnProgressChangedHandler() => _session.NotifyHudChanged();
        public void AddExperience(int amount)
        {
            if (!_session.IsCombatActive)
                return;

            Progress.AddExperience(amount);
            if (Progress.PendingChoices > 0 && _session.State != RunState.Drawing)
                ShowUpgradeChoices();
        }
        public void ChooseUpgrade(int index)
        {
            if (_session.State != RunState.UpgradeSelection || index < 0 || index >= _choices.Count || !_input.CanSelectSpell)
                return;

            if (!_ui.AreChoicesRevealed) return;
            UpgradeOffer choice = _choices[index];
            float previousMaximum = Progress.MaximumHealth;
            if (!Progress.TryApply(choice))
                return;

            if (Progress.MaximumHealth > previousMaximum)
                _player.Heal(Progress.MaximumHealth - previousMaximum);
            _ui.ShowSelection(choice);
            _input.SuppressUntilRelease();
            if (Progress.PendingChoices > 0)
                ShowUpgradeChoices();
            else
                _session.SetState(RunState.Playing);

            _ui.ShowCue($"{choice.DisplayName} improved");
        }
        public void ShowUpgradeChoices()
        {
            _choices = Progress.GetChoices(_upgrades, _random, _spells, _combat);
            _session.SetState(RunState.UpgradeSelection);
        }

    }
}
