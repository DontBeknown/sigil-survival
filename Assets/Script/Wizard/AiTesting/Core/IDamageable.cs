using UnityEngine;

namespace VonderGames.AiTesting.Core
{
    public interface IDamageable
    {
        bool IsDead { get; }
        void ApplyDamage(float amount, Vector2 direction, float knockback = 0);
    }
}
