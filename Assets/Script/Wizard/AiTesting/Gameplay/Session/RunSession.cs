using System;
using UnityEngine;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Entity;
using VonderGames.AiTesting.Gameplay.Manager;

namespace VonderGames.AiTesting.Gameplay.Session
{
    public sealed class RunSession
    {
        public event Action<RunState> OnStateChanged;
        public event Action OnHudChanged;
        public event Action<float, float> OnBossHealthChanged;
        public event Action OnEnding;
        public RunState State { get; private set; }
        public bool IsCombatActive => State == RunState.Playing || State == RunState.Drawing;
        public float Elapsed { get; private set; }
        public int Kills { get; private set; }
        private readonly SaveService _saves;
        private readonly InputManager _input;
        private float _hudTimer;

        public RunSession(SaveService saves, InputManager input)
        {
            _saves = saves;
            _input = input;
        }

        public void Reset()
        {
            Elapsed = 0;
            Kills = 0;
            _hudTimer = 0;
        }

        public void Advance(float delta)
        {
            Elapsed += delta;
            _hudTimer += delta;
            if (_hudTimer >= 0.05f)
            {
                _hudTimer = 0;
                NotifyHudChanged();
            }
        }

        public void AdvanceTo(float elapsed) => Elapsed = Mathf.Max(Elapsed, elapsed);
        public void RecordKill() { Kills++; NotifyHudChanged(); }
        public void NotifyHudChanged() => OnHudChanged?.Invoke();
        public void NotifyBossHealth(EnemyAgent boss) => OnBossHealthChanged?.Invoke(boss.Health, boss.MaximumHealth);
        public void ClearBossHealth() => OnBossHealthChanged?.Invoke(0, 0);

        public void SetState(RunState state)
        {
            bool wasCombatActive = IsCombatActive;
            State = state;
            if (wasCombatActive != IsCombatActive)
                _input.SetGameplayActive(IsCombatActive);

            _input.SuppressUntilRelease();
            OnStateChanged?.Invoke(state);
        }

        public void EndRun(bool isVictory)
        {
            if (!IsCombatActive)
                return;

            OnEnding?.Invoke();
            var data = _saves.Data;
            data.BestSurvivalTime = Mathf.Max(data.BestSurvivalTime, Elapsed);
            data.BestKillCount = Mathf.Max(data.BestKillCount, Kills);
            if (isVictory) data.BossVictories++;
            _saves.Save();
            SetState(isVictory ? RunState.Victory : RunState.Defeat);
        }

    }
}
