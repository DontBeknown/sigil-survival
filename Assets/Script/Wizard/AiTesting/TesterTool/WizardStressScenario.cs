#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Profiling;
using UnityEngine;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Entity;
using VonderGames.AiTesting.Gameplay.Instance;

namespace VonderGames.AiTesting.TesterTool
{
    public sealed class WizardStressScenario : MonoBehaviour
    {
        private GameplayInstance _game;
        private EnemyDefinition _definition;
        private readonly List<(SpellProjectile Projectile, int Version)> _projectiles = new List<(SpellProjectile, int)>();
        private readonly List<double> _mainTimes = new List<double>();
        private readonly List<double> _frameTimes = new List<double>();
        private ProfilerRecorder _mainRecorder;
        private ProfilerRecorder _gcRecorder;
        private long _maximumGc;
        private double _gcTotal;
        private float _start;
        private int _minimumEnemies = 150;
        private int _minimumProjectiles = 200;
        private int _previousTargetFrameRate;
        private int _previousVSync;

        public void Begin(GameplayInstance game, EnemyDefinition definition)
        {
            _game = game;
            game.StartRun();
            _definition = Instantiate(definition);
            typeof(EnemyDefinition).GetField("_damage", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_definition, 0f);
            typeof(EnemyDefinition).GetField("_speed", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_definition, 0f);
            game.Player.HealthState.GrantInvulnerability(100000f);
            for (int i = 0; i < 150; i++)
            {
                float angle = i * Mathf.PI * 2 / 150;
                game.SpawnEnemy(_definition, new Vector2(Mathf.Cos(angle) * 20, Mathf.Sin(angle) * 14));
            }
            for (int i = 0; i < 200; i++)
                _projectiles.Add(CreateProjectile(i));

            _previousTargetFrameRate = Application.targetFrameRate;
            _previousVSync = QualitySettings.vSyncCount;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            _mainRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
            _gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
            _start = Time.realtimeSinceStartup;
        }

        private void LateUpdate()
        {
            if (_game == null)
                return;

            for (int i = 0; i < _projectiles.Count; i++)
            {
                if (_projectiles[i].Projectile == null || !_projectiles[i].Projectile.gameObject.activeSelf ||
                    _projectiles[i].Version != _projectiles[i].Projectile.LeaseVersion)
                    _projectiles[i] = CreateProjectile(i);
            }
            float elapsed = Time.realtimeSinceStartup - _start;
            if (elapsed < 3)
                return;

            _minimumEnemies = Mathf.Min(_minimumEnemies, _game.ActiveEnemies.Count);
            _minimumProjectiles = Mathf.Min(_minimumProjectiles, _game.Projectiles.ActiveCount);
            _frameTimes.Add(Time.unscaledDeltaTime * 1000.0);
            if (_mainRecorder.Valid) _mainTimes.Add(_mainRecorder.LastValue / 1000000.0);
            if (_gcRecorder.Valid)
            {
                _gcTotal += _gcRecorder.LastValue;
                _maximumGc = Math.Max(_maximumGc, _gcRecorder.LastValue);
            }
            if (elapsed >= 15)
            {
                string report = JsonUtility.ToJson(new StressReport
                {
                    Samples = _frameTimes.Count,
                    MinimumEnemies = _minimumEnemies,
                    MinimumProjectiles = _minimumProjectiles,
                    AverageFrameMs = Average(_frameTimes),
                    P95FrameMs = Percentile(_frameTimes),
                    AverageMainThreadMs = Average(_mainTimes),
                    P95MainThreadMs = Percentile(_mainTimes),
                    AverageGcBytes = _gcTotal / Math.Max(1, _frameTimes.Count),
                    MaximumGcBytes = _maximumGc,
                    Machine = SystemInfo.processorType + " / " + SystemInfo.graphicsDeviceName,
                    Resolution = Screen.width + "x" + Screen.height,
                    Notes = "Editor-only measurement; 3s warmup, 12s sample, uncapped. Main Thread includes Editor overhead and synchronization. GPU timing not measured."
                }, true);
                Directory.CreateDirectory("Temp/WizardQA");
                File.WriteAllText("Temp/WizardQA/stress.json", report);
                Debug.Log("Wizard stress complete: " + report);
                _game.ReturnToTitle();
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            _mainRecorder.Dispose();
            _gcRecorder.Dispose();
            QualitySettings.vSyncCount = _previousVSync;
            Application.targetFrameRate = _previousTargetFrameRate;
            if (_definition != null) Destroy(_definition);
        }

        private (SpellProjectile Projectile, int Version) CreateProjectile(int index)
        {
            float angle = index * Mathf.PI * 2 / 200;
            var position = new Vector2(Mathf.Cos(angle) * 12, Mathf.Sin(angle) * 10);
            SpellProjectile projectile = _game.SpawnHostileProjectile(position, new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle)));
            return (projectile, projectile.LeaseVersion);
        }

        private static double Average(List<double> values)
        {
            double sum = 0;
            foreach (double value in values) sum += value;
            return sum / Math.Max(1, values.Count);
        }

        private static double Percentile(List<double> values)
        {
            if (values.Count == 0) return 0;
            var sorted = new List<double>(values);
            sorted.Sort();
            return sorted[Math.Min(sorted.Count - 1, (int)(sorted.Count * 0.95))];
        }

        [Serializable]
        private sealed class StressReport
        {
            public int Samples;
            public int MinimumEnemies;
            public int MinimumProjectiles;
            public double AverageFrameMs;
            public double P95FrameMs;
            public double AverageMainThreadMs;
            public double P95MainThreadMs;
            public double AverageGcBytes;
            public long MaximumGcBytes;
            public string Machine;
            public string Resolution;
            public string Notes;
        }
    }
}
#endif
