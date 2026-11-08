using System.Collections.Generic;
using VonderGames.AiTesting.Gameplay.Entity;

namespace VonderGames.AiTesting.Gameplay.Combat
{
    public sealed class EnemyHitHistory
    {
        private readonly Dictionary<EnemyAgent, int> _versions = new Dictionary<EnemyAgent, int>();

        public bool Add(EnemyAgent enemy)
        {
            if (_versions.TryGetValue(enemy, out int version) && version == enemy.LeaseVersion) return false;
            _versions[enemy] = enemy.LeaseVersion;
            return true;
        }
        public void Clear() => _versions.Clear();

    }
}
