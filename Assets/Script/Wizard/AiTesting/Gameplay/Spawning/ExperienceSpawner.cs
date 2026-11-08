using System;
using UnityEngine;
using VonderGames.AiTesting.Gameplay.Entity;
using VonderGames.AiTesting.Gameplay.Pooling;
using VonderGames.AiTesting.Gameplay.Progression;
using VonderGames.AiTesting.Gameplay.Session;
using VonderGames.AiTesting.UI;

namespace VonderGames.AiTesting.Gameplay.Spawning
{
    public sealed class ExperienceSpawner : IDisposable
    {
        private readonly SceneEntityPool<ExperiencePickup> _pool;
        private readonly RunSession _session;
        private readonly PlayerController _player;
        private readonly ProgressionController _progression;
        private readonly CombatFeedback _feedback;
        private readonly UICanvasGameplayInstance _ui;
        private readonly Action<ExperiencePickup> _return;

        public ExperienceSpawner(ExperiencePickup prefab, Transform runtimeRoot, RunSession session,
            PlayerController player, ProgressionController progression, CombatFeedback feedback, UICanvasGameplayInstance ui)
        {
            _session = session;
            _player = player;
            _progression = progression;
            _feedback = feedback;
            _ui = ui;
            _pool = new SceneEntityPool<ExperiencePickup>(prefab, runtimeRoot, pickup => pickup.ResetForPool(), 256);
            _return = _pool.Return;
        }

        public void Spawn(Vector2 position, int amount, bool isBonus)
        {
            ExperiencePickup pickup = _pool.Rent(position);
            pickup.Initialize(_session, _player, _progression, _feedback, _ui, _return, amount, isBonus);
            pickup.gameObject.SetActive(true);
        }

        public void Clear() => _pool.ClearActive();
        public void Dispose() => _pool.Dispose();
    }
}
