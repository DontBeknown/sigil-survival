using System;
using UnityEngine;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Combat;
using VonderGames.AiTesting.Gameplay.Entity;
using VonderGames.AiTesting.Gameplay.Pooling;
using VonderGames.AiTesting.Gameplay.Session;
using VonderGames.AiTesting.UI;

namespace VonderGames.AiTesting.Gameplay.Spawning
{
    public sealed class EnemySpawner : IDisposable
    {
        public event Action<EnemyAgent> OnEnemyDied;
        public bool HasBossSpawned { get; private set; }
        public float BossTime => _difficulty.BossTime;
        private readonly RunSession _session;
        private readonly RunProgress _progress;
        private readonly PlayerController _player;
        private readonly EnemyRegistry _registry;
        private readonly RangedAttackScheduler _scheduler;
        private readonly ProjectileSpawner _projectiles;
        private readonly SceneEntityPool<EnemyAgent> _enemyPool;
        private readonly CombatDefinition _combat;
        private readonly DifficultyDefinition _difficulty;
        private readonly EnemyDefinition[] _enemies;
        private readonly EnemyDefinition _rangedDefinition;
        private readonly EnemyDefinition _bossDefinition;
        private readonly CombatFeedback _feedback;
        private readonly SaveService _saves;
        private readonly UICanvasGameplayInstance _ui;
        private readonly CameraFeedback _camera;
        private readonly System.Random _random;
        private readonly Action _beforeBoss;
        private readonly Func<Vector2, Vector2, EnemyAgent, SpellProjectile> _spawnProjectile;
        private readonly Action<float> _shake;
        private readonly Action<EnemyAgent> _enemyDied;
        private readonly Action<EnemyAgent> _bossHealthChanged;
        private float _spawnBudget;

        public EnemySpawner(RunSession session, RunProgress progress, PlayerController player,
            EnemyRegistry registry, RangedAttackScheduler scheduler, ProjectileSpawner projectiles,
            EnemyAgent enemyPrefab, Transform runtimeRoot,
            CombatDefinition combat, DifficultyDefinition difficulty, EnemyDefinition[] enemies,
            EnemyDefinition rangedDefinition, EnemyDefinition bossDefinition, CombatFeedback feedback,
            SaveService saves, UICanvasGameplayInstance ui,
            CameraFeedback camera, System.Random random, Action beforeBoss)
        {
            _session = session;
            _progress = progress;
            _player = player;
            _registry = registry;
            _scheduler = scheduler;
            _projectiles = projectiles;
            _combat = combat;
            _difficulty = difficulty;
            _enemies = enemies;
            _rangedDefinition = rangedDefinition;
            _bossDefinition = bossDefinition;
            _feedback = feedback;
            _saves = saves;
            _ui = ui;
            _camera = camera;
            _random = random;
            _beforeBoss = beforeBoss;
            _spawnProjectile = _projectiles.SpawnHostileProjectile;
            _shake = _camera.Shake;
            _enemyDied = EnemyDied;
            _bossHealthChanged = _session.NotifyBossHealth;
            _enemyPool = new SceneEntityPool<EnemyAgent>(enemyPrefab, runtimeRoot, ResetEnemy, difficulty.EnemyCap);
        }

        public void Reset()
        {
            Clear();
            HasBossSpawned = false;
            _spawnBudget = 0;
        }

        public void Tick(float delta)
        {
            if (!HasBossSpawned && _session.Elapsed >= _difficulty.BossTime) SpawnBoss();
            if (HasBossSpawned) return;
            var phase = _difficulty.GetPhase(_session.Elapsed);
            float rate = _session.Elapsed >= _difficulty.BossTime - 10 ? 0.5f : phase.SpawnsPerSecond;
            _spawnBudget = Mathf.Min(_spawnBudget + delta * rate, 4);
            while (_spawnBudget >= 1)
            {
                _spawnBudget--;
                if (_registry.Count >= _difficulty.EnemyCap) continue;
                int available = Mathf.Min(phase.EnemyTypes, _enemies.Length);
                bool includeRanged = _session.Elapsed >= _combat.RangedStart && _registry.RangedCount < _combat.RangedEnemyCap;
                int roll = _random.Next(available + (includeRanged ? 1 : 0));
                SpawnEnemy(roll == available ? _rangedDefinition : _enemies[roll], GetSpawnPosition());
            }
        }

        public EnemyAgent SpawnEnemy(EnemyDefinition definition, Vector2 position)
        {
            if (definition.IsRanged && _registry.RangedCount >= _combat.RangedEnemyCap) return null;
            EnemyAgent enemy = _enemyPool.Rent(position);
            enemy.Initialize(_session, _player, _progress, _combat, _feedback, _saves,
                _spawnProjectile, _shake, _enemyDied, _bossHealthChanged, definition);
            _registry.Add(enemy);
            enemy.gameObject.SetActive(true);
            _scheduler.Add(enemy);
            return enemy;
        }

        public void EnemyDied(EnemyAgent enemy)
        {
            OnEnemyDied?.Invoke(enemy);
        }

        public void Despawn(EnemyAgent enemy) => _enemyPool.Return(enemy);

        public void SkipToBoss()
        {
            if (!Application.isPlaying || !_session.IsCombatActive || HasBossSpawned) return;
            _session.AdvanceTo(_difficulty.BossTime);
            SpawnBoss();
        }

        public void Clear()
        {
            _scheduler.Clear();
            _enemyPool.ClearActive();
        }

        public void Dispose()
        {
            Clear();
            _enemyPool.Dispose();
        }

        private void ResetEnemy(EnemyAgent enemy)
        {
            _projectiles.RecordAttacker(enemy);
            _scheduler.Remove(enemy);
            _registry.Remove(enemy);
            enemy.ResetForPool();
        }

        private void SpawnBoss()
        {
            _scheduler.Clear();
            _beforeBoss();
            HasBossSpawned = true;
            _enemyPool.ClearActive();
            EnemyAgent boss = SpawnEnemy(_bossDefinition, GetSpawnPosition());
            _session.NotifyBossHealth(boss);
            _ui.ShowCue("THE WARDEN HAS ARRIVED");
            _session.NotifyHudChanged();
        }
        private Vector2 GetSpawnPosition()
        {
            Vector2 position = _player.transform.position;
            float angle = (float)_random.NextDouble() * Mathf.PI * 2;
            position += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 18;
            position.x = Mathf.Clamp(position.x, -28, 28);
            position.y = Mathf.Clamp(position.y, -18, 18);
            if (Vector2.Distance(position, _player.transform.position) < 8)
                position = -(Vector2)_player.transform.position * 0.5f;

            return position;
        }

    }
}
