using System;
using UnityEngine;

namespace VonderGames.AiTesting.Core
{
    [Serializable]
    public struct DifficultyPhase
    {
        public float StartTime;
        public float SpawnsPerSecond;
        public int EnemyTypes;
    }

    [CreateAssetMenu(menuName = "Wizard/Difficulty")]
    public sealed class DifficultyDefinition : ScriptableObject
    {
        [SerializeField] private float _bossTime = 600;
        public float BossTime => _bossTime;
        [SerializeField] private int _enemyCap = 150;
        public int EnemyCap => _enemyCap;
        [SerializeField] private DifficultyPhase[] _phases;

        public DifficultyPhase GetPhase(float time)
        {
            var phase = _phases[0];
            foreach (var candidate in _phases)
            {
                if (candidate.StartTime > time)
                    break;

                phase = candidate;
            }
            return phase;
        }
    }
}
