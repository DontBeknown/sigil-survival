using System;
using UnityEngine;
using VonderGames.AiTesting.Core;

namespace VonderGames.AiTesting.Gameplay.Entity
{
    public sealed class PlayerHealth
    {
        public event Action<float, float> OnChanged;
        public float Current { get; private set; }
        public float Invulnerability { get; private set; }
        public bool IsDead => Current <= 0;
        private readonly RunProgress _progress;

        public PlayerHealth(RunProgress progress) => _progress = progress;
        public void Tick(float delta) => Invulnerability = Mathf.Max(0, Invulnerability - delta);
        public void GrantInvulnerability(float duration) => Invulnerability = Mathf.Max(Invulnerability, duration);
        public void Reset()
        {
            Current = _progress.MaximumHealth;
            Invulnerability = 0;
            OnChanged?.Invoke(Current, _progress.MaximumHealth);
        }
        public void Heal(float amount)
        {
            Current = Mathf.Min(_progress.MaximumHealth, Current + amount);
            OnChanged?.Invoke(Current, _progress.MaximumHealth);
        }
        public bool TryDamage(float amount)
        {
            if (IsDead || Invulnerability > 0) return false;
            Current = Mathf.Max(0, Current - amount);
            Invulnerability = 1;
            OnChanged?.Invoke(Current, _progress.MaximumHealth);
            return true;
        }

    }
}
