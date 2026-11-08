using UnityEngine;
using VonderGames.AiTesting.Gameplay.Entity;

namespace VonderGames.AiTesting.Gameplay.Combat
{
    public sealed class ProjectileThreatMonitor
    {
        private readonly ProjectileSpawner _projectiles;
        private readonly PlayerController _player;

        public ProjectileThreatMonitor(ProjectileSpawner projectiles, PlayerController player)
        {
            _projectiles = projectiles;
            _player = player;
        }
        public void Tick()
        {
            float imminent = float.MaxValue;
            foreach (var shot in _projectiles.HostileProjectiles)
            {
                if (shot != null && shot.TryPredictImpact(0.8f, out float impact)) imminent = Mathf.Min(imminent, impact);
            }
            _player.SetDanger(imminent == float.MaxValue ? 0 : 1 - imminent / 0.8f);
        }

    }
}
