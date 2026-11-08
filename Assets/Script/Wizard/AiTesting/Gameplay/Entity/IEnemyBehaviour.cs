using UnityEngine;
namespace VonderGames.AiTesting.Gameplay.Entity
{
    public interface IEnemyBehaviour
    {
        EnemyState State { get; }
        void Reset();
        Vector2 Tick(Vector2 offset, float delta);
    }
}
