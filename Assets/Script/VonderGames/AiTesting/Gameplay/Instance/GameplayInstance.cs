using System;
using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Entity;
using VonderGames.AiTesting.Gameplay.Manager;
using VonderGames.AiTesting.UI;

namespace VonderGames.AiTesting.Gameplay.Instance
{
    public sealed class GameplayInstance : MonoBehaviour
    {
        [SerializeField] private InputManager _input;
        [SerializeField] private PlayerController _player;
        public PlayerController Player => _player;
        [SerializeField] private Camera _camera;
        [SerializeField] private CinemachineCamera _followCamera;
        [SerializeField] private CinemachineImpulseSource _impulse;
        [SerializeField] private UICanvasGameplayInstance _ui;
        [SerializeField] private Transform _runtimeRoot;
        [SerializeField] private EnemyAgent _enemyPrefab;
        [SerializeField] private SpellProjectile _projectilePrefab;
        [SerializeField] private ExperiencePickup _pickupPrefab;
        [SerializeField] private Sprite _square;
        [SerializeField] private SpellDefinition[] _spells;
        public SpellDefinition[] Spells => _spells;
        [SerializeField] private EnemyDefinition[] _enemies;
        [SerializeField] private EnemyDefinition _bossDefinition;
        [SerializeField] private UpgradeDefinition[] _upgrades;
        [SerializeField] private DifficultyDefinition _difficulty;

        public event Action OnHudChanged;
        public event Action<RunState> OnStateChanged;
        public event Action<float, float> OnBossHealthChanged;

        private readonly RunProgress _progress = new RunProgress();
        public RunProgress Progress => _progress;
        private readonly List<EnemyAgent> _activeEnemies = new List<EnemyAgent>();
        public IReadOnlyList<EnemyAgent> ActiveEnemies => _activeEnemies;
        private readonly float[] _cooldowns = new float[3];
        private readonly System.Random _random = new System.Random();
        private SaveService _saves;
        public SaveService Saves => _saves;
        private RunState _state;
        public RunState State => _state;
        private float _elapsed;
        public float Elapsed => _elapsed;
        private int _kills;
        public int Kills => _kills;
        private float _spawnBudget;
        private float _hudTimer;
        private bool _hasBossSpawned;
        public bool HasBossSpawned => _hasBossSpawned;
        private EnemyAgent _boss;
        private List<UpgradeDefinition> _choices;
        public IReadOnlyList<UpgradeDefinition> Choices => _choices;

        private void Start()
        {
            _saves = new SaveService(Path.Combine(Application.persistentDataPath, "wizard-survival.json"));
            _saves.Load();
            _input.Initialize();
            _player.Initialize(this, _input, _camera);
            _ui.Initialize(this, _input);
            _progress.OnChanged += OnProgressChangedHandler;
            _input.Actions.System.Pause.performed += OnPauseRequestedHandler;
            _player.ResetPlayer();
            SetState(RunState.Title);
            OnHudChanged?.Invoke();
        }

        private void Update()
        {
            // Reduce zoom on unusually wide windows so the view still fits inside the arena.
            float halfHeight = Mathf.Min(10, 29.5f / Mathf.Max(1, _camera.aspect));
            _followCamera.Lens.OrthographicSize = halfHeight;
            if (_state != RunState.Playing)
                return;

            AdvanceTime(Time.deltaTime);
            if (_input.CanCast)
            {
                if (_input.Actions.Player.Bolt.IsPressed()) TryCast(SpellKind.Bolt);
                if (_input.Actions.Player.Fireball.IsPressed()) TryCast(SpellKind.Fireball);
                if (_input.Actions.Player.Nova.IsPressed()) TryCast(SpellKind.Nova);
            }
        }

        private void OnDestroy()
        {
            _progress.OnChanged -= OnProgressChangedHandler;
            if (_input.Actions != null)
                _input.Actions.System.Pause.performed -= OnPauseRequestedHandler;

            Time.timeScale = 1;
        }

        public void StartRun()
        {
            ClearRunObjects();
            _elapsed = 0;
            _kills = 0;
            _spawnBudget = 0;
            _hasBossSpawned = false;
            _boss = null;
            Array.Clear(_cooldowns, 0, _cooldowns.Length);
            _progress.Reset();
            _player.ResetPlayer();
            _followCamera.PreviousStateIsValid = false;
            OnBossHealthChanged?.Invoke(0, 0);
            SetState(RunState.Playing);
            OnHudChanged?.Invoke();
        }

        public void ReturnToTitle()
        {
            ClearRunObjects();
            SetState(RunState.Title);
        }

        public void TogglePause()
        {
            if (_state == RunState.Playing)
                SetState(RunState.Paused);
            else if (_state == RunState.Paused)
                SetState(RunState.Playing);
        }

        public void AdvanceTime(float delta)
        {
            if (_state != RunState.Playing)
                return;

            _elapsed += delta;
            for (int i = 0; i < _cooldowns.Length; i++)
                _cooldowns[i] = Mathf.Max(0, _cooldowns[i] - delta);

            if (!_hasBossSpawned && _elapsed >= _difficulty.BossTime)
                SpawnBoss();

            if (!_hasBossSpawned)
            {
                var phase = _difficulty.GetPhase(_elapsed);
                float rate = _elapsed >= _difficulty.BossTime - 10 ? 0.5f : phase.SpawnsPerSecond;
                _spawnBudget = Mathf.Min(_spawnBudget + delta * rate, 4);
                while (_spawnBudget >= 1)
                {
                    _spawnBudget--;
                    if (_activeEnemies.Count < _difficulty.EnemyCap)
                        SpawnEnemy(_enemies[_random.Next(Mathf.Min(phase.EnemyTypes, _enemies.Length))], GetSpawnPosition());
                }
            }
            _hudTimer += delta;
            if (_hudTimer >= 0.05f)
            {
                _hudTimer = 0;
                OnHudChanged?.Invoke();
            }
        }

        public float GetCooldown(SpellKind kind) => _cooldowns[(int)kind];

        public bool TryCast(SpellKind kind)
        {
            if (_state != RunState.Playing || !_progress.IsUnlocked(kind) || _cooldowns[(int)kind] > 0)
                return false;

            var spell = _spells[(int)kind];
            _cooldowns[(int)kind] = spell.Cooldown * _progress.CooldownMultiplier;
            if (kind == SpellKind.Nova)
                DamageArea(_player.transform.position, spell.Radius, spell.Damage * _progress.DamageMultiplier, true, spell.Color);
            else
            {
                var projectile = Instantiate(_projectilePrefab, _runtimeRoot);
                projectile.transform.position = (Vector2)_player.transform.position + _player.AimDirection * 0.7f;
                projectile.Initialize(this, _player.AimDirection, spell, spell.Damage * _progress.DamageMultiplier);
            }
            OnHudChanged?.Invoke();
            return true;
        }

        public EnemyAgent SpawnEnemy(EnemyDefinition definition, Vector2 position)
        {
            var enemy = Instantiate(_enemyPrefab, position, Quaternion.identity, _runtimeRoot);
            enemy.Initialize(this, definition);
            _activeEnemies.Add(enemy);
            return enemy;
        }

        public SpellProjectile SpawnHostileProjectile(Vector2 position, Vector2 direction)
        {
            var projectile = Instantiate(_projectilePrefab, position, Quaternion.identity, _runtimeRoot);
            projectile.Initialize(this, direction, _spells[0], 15, true);
            return projectile;
        }

        public void DamageArea(Vector2 position, float radius, float damage, bool isFrost, Color color)
        {
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = _activeEnemies[i];
                Vector2 direction = (Vector2)enemy.transform.position - position;
                if (direction.sqrMagnitude > radius * radius)
                    continue;

                if (isFrost)
                    enemy.Slow();

                enemy.ApplyDamage(damage, direction, isFrost ? 8 : 4);
            }
            var effect = new GameObject(isFrost ? "FrostNovaImpact" : "FireballImpact", typeof(SpriteRenderer));
            effect.transform.SetParent(_runtimeRoot);
            effect.transform.position = position;
            var renderer = effect.GetComponent<SpriteRenderer>();
            renderer.sprite = _square;
            renderer.sortingOrder = 4;
            renderer.color = new Color(color.r, color.g, color.b, _saves.Data.ReduceFlash ? 0.1f : 0.25f);
            effect.transform.localScale = Vector3.one * radius * 2;
            renderer.DOFade(0, 0.25f).SetLink(effect).OnComplete(() => Destroy(effect));
        }

        public void EnemyDied(EnemyAgent enemy)
        {
            _activeEnemies.Remove(enemy);
            _kills++;
            if (enemy.Definition.IsBoss)
                EndRun(true);
            else
            {
                var pickup = Instantiate(_pickupPrefab, enemy.transform.position, Quaternion.identity, _runtimeRoot);
                pickup.Initialize(this, enemy.Definition.Experience);
            }
            OnHudChanged?.Invoke();
        }

        public void AddExperience(int amount)
        {
            if (_state != RunState.Playing)
                return;

            _progress.AddExperience(amount);
            if (_progress.PendingChoices > 0)
                ShowUpgradeChoices();
        }

        public void ChooseUpgrade(int index)
        {
            if (_state != RunState.UpgradeSelection || index < 0 || index >= _choices.Count)
                return;

            var upgrade = _choices[index];
            if (!_progress.TryApply(upgrade))
                return;

            if (upgrade.Kind == UpgradeKind.Health) _player.Heal(20);
            if (upgrade.Kind == UpgradeKind.Healing) _player.Heal(30);
            if (_progress.PendingChoices > 0)
                ShowUpgradeChoices();
            else
                SetState(RunState.Playing);
        }

        public void NotifyBossHealth(EnemyAgent boss)
        {
            OnBossHealthChanged?.Invoke(boss.Health, boss.Definition.Health);
        }

        public void EndRun(bool isVictory)
        {
            if (_state != RunState.Playing)
                return;

            var data = _saves.Data;
            data.BestSurvivalTime = Mathf.Max(data.BestSurvivalTime, _elapsed);
            data.BestKillCount = Mathf.Max(data.BestKillCount, _kills);
            if (isVictory) data.BossVictories++;
            _saves.Save();
            SetState(isVictory ? RunState.Victory : RunState.Defeat);
        }

        public void Shake(float strength)
        {
            if (!_saves.Data.ReduceShake)
                _impulse.GenerateImpulseWithVelocity(new Vector3(strength, strength * 0.6f, 0));
        }

        private void OnPauseRequestedHandler(InputAction.CallbackContext context)
        {
            if (_ui.CloseSubpanel())
                return;

            TogglePause();
        }

        private void OnProgressChangedHandler()
        {
            OnHudChanged?.Invoke();
        }

        private void ShowUpgradeChoices()
        {
            _choices = _progress.GetChoices(_upgrades, _random);
            SetState(RunState.UpgradeSelection);
        }

        private void SetState(RunState state)
        {
            _state = state;
            Time.timeScale = state == RunState.Playing ? 1 : 0;
            _input.SetGameplayActive(state == RunState.Playing);
            OnStateChanged?.Invoke(state);
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

        private void SpawnBoss()
        {
            _hasBossSpawned = true;
            foreach (var enemy in _activeEnemies)
            {
                enemy.gameObject.SetActive(false);
                Destroy(enemy.gameObject);
            }
            _activeEnemies.Clear();
            _boss = SpawnEnemy(_bossDefinition, GetSpawnPosition());
            NotifyBossHealth(_boss);
            OnHudChanged?.Invoke();
        }

        private void ClearRunObjects()
        {
            foreach (Transform child in _runtimeRoot)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            _activeEnemies.Clear();
        }
    }
}
