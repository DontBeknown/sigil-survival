using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Entity;
using VonderGames.AiTesting.Gameplay.Session;
using VonderGames.AiTesting.Gameplay.Combat;
using VonderGames.AiTesting.Gameplay.Spawning;
using VonderGames.AiTesting.Gameplay.Pooling;
using VonderGames.AiTesting.Gameplay.Progression;
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
        [SerializeField] private CombatFeedback _feedback;
        public CombatFeedback Feedback => _feedback;
        [SerializeField] private UIGestureDrawing _drawing;
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
        [SerializeField] private CombatDefinition _combat;
        public CombatDefinition Combat => _combat;
        [SerializeField] private EnemyDefinition _rangedDefinition;
        [SerializeField] private DifficultyDefinition _difficulty;
        public float BossTime => _difficulty.BossTime;
        [SerializeField, Range(0.01f, 1)] private float _drawingTimeScale = 0.15f;
        public float DrawingTimeScale => _drawingTimeScale;


        public RunSession Session { get; private set; }
        public GameplayClock Clock { get; private set; }
        public EnemyRegistry Enemies { get; private set; }
        public EnemySpawner Spawner { get; private set; }
        public CooldownTracker Cooldowns { get; private set; }
        public ProgressionController Progression { get; private set; }
        public SpellCaster SpellCaster { get; private set; }
        public DeflectController Deflect { get; private set; }
        public DrawingController Drawing { get; private set; }
        public PlayerCombatController PlayerCombat { get; private set; }
        public CameraFeedback CameraFeedback { get; private set; }
        public CombatDamage Damage { get; private set; }
        public ProjectileSpawner Projectiles { get; private set; }
        public ExperienceSpawner Experience { get; private set; }
        private EnemyDeathController _enemyDeaths;
        private RangedAttackScheduler _scheduler;
        private ProjectileThreatMonitor _threats;
        private SaveService _saves;
        private bool _isInitialized;

        public RunProgress Progress => Progression.Progress;
        public float ComboRemaining => SpellCaster.ComboRemaining;
        public IReadOnlyList<EnemyAgent> ActiveEnemies => Enemies.ActiveEnemies;
        public SaveService Saves => _saves;
        public RunState State => Session.State;
        public bool IsCombatActive => Session.IsCombatActive;
        public float Elapsed => Session.Elapsed;
        public int Kills => Session.Kills;
        public bool HasBossSpawned => Spawner.HasBossSpawned;
        public float CombatScale => Clock.CombatScale;
        public float PlayerScale => Clock.PlayerScale;
        public float PlayerDelta => Clock.PlayerDelta;
        public float PlayerPhysicsRatio => Clock.PlayerPhysicsRatio;
        public Vector2 MouseWorldPosition => CameraFeedback.MouseWorldPosition;
        public bool IsDeflectDrawing => Deflect.IsDrawing;
        public bool HasDeflectProtection => Deflect.HasProtection;
        public int ActiveRangedEnemyCount => Enemies.RangedCount;
        public IReadOnlyList<UpgradeOffer> Choices => Progression.Choices;

        // Compatibility surface for existing Editor tools; runtime collaborators receive focused owners.
        public event Action OnHudChanged { add => Session.OnHudChanged += value; remove => Session.OnHudChanged -= value; }
        public event Action<RunState> OnStateChanged { add => Session.OnStateChanged += value; remove => Session.OnStateChanged -= value; }
        public event Action<float, float> OnBossHealthChanged { add => Session.OnBossHealthChanged += value; remove => Session.OnBossHealthChanged -= value; }

        private void Start()
        {
            string legacyPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.persistentDataPath), "AI_TESTING", "wizard-survival.json");
            string savePath = System.IO.File.Exists(legacyPath) || System.IO.File.Exists(legacyPath + ".bak")
                ? legacyPath : System.IO.Path.Combine(Application.persistentDataPath, "wizard-survival.json");
            _saves ??= new SaveService(savePath);
            _saves.Load();
            _input.Initialize();
            Session = new RunSession(_saves, _input);
            Clock = new GameplayClock(Session, _combat, _drawingTimeScale);
            Enemies = new EnemyRegistry();
            _scheduler = new RangedAttackScheduler(_combat);
            CameraFeedback = new CameraFeedback(_camera, _input, _followCamera, _impulse, _saves);
            var random = new System.Random();
            Progression = new ProgressionController(Session, _player, _input, _ui, _upgrades, _spells, _combat, random);
            Cooldowns = new CooldownTracker(_spells, _combat, Progress);
            Damage = new CombatDamage(Session, Progress, _combat, _spells, Enemies, _feedback, random);
            PlayerCombat = new PlayerCombatController(Session, Progress, _player, _combat, Clock, Cooldowns, _feedback, _ui, CameraFeedback);
            Projectiles = new ProjectileSpawner(_projectilePrefab, _runtimeRoot, _player, Session, Progress,
                _combat, _spells, _feedback, _saves, Damage, PlayerCombat.PerfectDodge, () => Deflect.HasProtection);
            Deflect = new DeflectController(Session, Progress, _player, _input, _combat, Cooldowns,
                Projectiles, Enemies, Damage, PlayerCombat, _feedback, _ui, _drawing, CameraFeedback);
            SpellCaster = new SpellCaster(Session, Progress, _player, _spells, Cooldowns, Projectiles,
                Enemies, Damage, _combat, _feedback, _ui, CameraFeedback);
            Drawing = new DrawingController(Session, Progression, _input, Cooldowns, Deflect,
                SpellCaster, _drawing, _player, _feedback, _ui);
            Spawner = new EnemySpawner(Session, Progress, _player, Enemies, _scheduler, Projectiles,
                _enemyPrefab, _runtimeRoot, _combat, _difficulty, _enemies, _rangedDefinition,
                _bossDefinition, _feedback, _saves, _ui, CameraFeedback, random, OnBeforeBossHandler);
            Experience = new ExperienceSpawner(_pickupPrefab, _runtimeRoot, Session, _player, Progression, _feedback, _ui);
            _enemyDeaths = new EnemyDeathController(Spawner, Experience, Session, Progress, _combat, _feedback, random);
            _threats = new ProjectileThreatMonitor(Projectiles, _player);
            _feedback.Initialize(_saves);
            _player.Initialize(Session, Clock, Progress, _combat, _input, _feedback, _saves, CameraFeedback, () => Deflect.HasProtection);
            _drawing.Initialize(Session, Drawing, Clock, _spells, _input);
            _ui.Initialize(Session, Progression, Cooldowns, SpellCaster, Spawner, Drawing, Deflect,
                _player, _combat, _saves, CameraFeedback, _input, StartRun, ReturnToTitle);
            Session.OnEnding += OnRunEndingHandler;
            _player.OnDied += OnPlayerDiedHandler;
            _player.OnDamaged += Drawing.InterruptDrawing;
            _player.OnDashUnavailable += _ui.NudgeDash;
            _input.Actions.System.Pause.performed += OnPauseRequestedHandler;
            _isInitialized = true;
            _player.ResetPlayer();
            Session.SetState(RunState.Title);
            Session.NotifyHudChanged();
        }

        private void Update()
        {
            CameraFeedback.Tick();
            Clock.Tick(Time.unscaledDeltaTime);
            if (!IsCombatActive) return;
            AdvanceTime(Time.unscaledDeltaTime * Clock.CombatScale);
            if (!IsCombatActive) return;
            _scheduler.Tick(Time.unscaledDeltaTime * Clock.CombatScale);
            _threats.Tick();
            SpellCaster.TickAutoAttack();
        }

        private void OnDestroy()
        {
            if (!_isInitialized) return;
            _input.Actions.System.Pause.performed -= OnPauseRequestedHandler;
            _player.OnDied -= OnPlayerDiedHandler;
            _player.OnDamaged -= Drawing.InterruptDrawing;
            _player.OnDashUnavailable -= _ui.NudgeDash;
            Session.OnEnding -= OnRunEndingHandler;
            Deflect.Clear();
            _enemyDeaths.Dispose();
            Spawner.Dispose();
            Experience.Dispose();
            Projectiles.Dispose();
            Progression.Dispose();
            Clock.Dispose();
            CameraFeedback.Dispose();
        }

        public void StartRun()
        {
            ClearRunObjects();
            Session.Reset();
            Spawner.Reset();
            Clock.Reset();
            Cooldowns.Reset();
            SpellCaster.Reset();
            CameraFeedback.Reset();
            Progression.Reset();
            _player.ResetPlayer();
            Session.ClearBossHealth();
            Session.SetState(RunState.Playing);
            Session.NotifyHudChanged();
        }

        public void ReturnToTitle()
        {
            ClearRunObjects();
            Session.SetState(RunState.Title);
        }

        public void AdvanceTime(float delta)
        {
            if (!IsCombatActive) return;
            Session.Advance(delta);
            Cooldowns.Advance(delta);
            SpellCaster.Advance(delta);
            Spawner.Tick(delta);
        }

        public void TogglePause() => Drawing.TogglePause();
        public bool BeginDrawing(SpellKind kind) => Drawing.BeginDrawing(kind);
        public void FinishDrawing(bool successful, string reason) => Drawing.FinishDrawing(successful, reason);
        public float GetCooldown(SpellKind kind) => Cooldowns.GetCooldown(kind);
        public float GetCooldownDuration(SpellKind kind) => Cooldowns.GetCooldownDuration(kind);
        public void RefreshCooldowns() => PlayerCombat.RefreshCooldowns();
        public void PerfectDodge() => PlayerCombat.PerfectDodge();
        public bool AttemptDeflect() => Deflect.AttemptDeflect();
        public void InterruptDrawing() => Drawing.InterruptDrawing();
        public EnemyAgent FindNearest(Vector2 origin, float range, HashSet<EnemyAgent> excluded = null) => Enemies.FindNearest(origin, range, excluded);
        public EnemyAgent SpawnEnemy(EnemyDefinition definition, Vector2 position) => Spawner.SpawnEnemy(definition, position);
        public SpellProjectile SpawnHostileProjectile(Vector2 position, Vector2 direction, EnemyAgent attacker = null) => Projectiles.SpawnHostileProjectile(position, direction, attacker);
        public void EnemyDied(EnemyAgent enemy) => Spawner.EnemyDied(enemy);
        public void AddExperience(int amount) => Progression.AddExperience(amount);
        public void ChooseUpgrade(int index) => Progression.ChooseUpgrade(index);
        public void NotifyBossHealth(EnemyAgent boss) => Session.NotifyBossHealth(boss);
        public void EndRun(bool victory) => Session.EndRun(victory);
        public void Shake(float strength) => CameraFeedback.Shake(strength);
        public Vector2 WorldToScreen(Vector2 position) => CameraFeedback.WorldToScreen(position);
        public void DealPlayerDamage(EnemyAgent enemy, float damage, Vector2 direction, float knockback = 0) => Damage.DealPlayerDamage(enemy, damage, direction, knockback);
        public void ExplodeFire(Vector2 position, float damage, EnemyHitHistory visited) => Damage.ExplodeFire(position, damage, visited);
        public void ForgetProjectile(SpellProjectile shot) => Projectiles.ForgetProjectile(shot);
        public void NotifyDashUnavailable() => _ui.NudgeDash();
        public void NotifyExperiencePickup(Vector2 position, int amount) => _ui.ShowExperiencePickup(position, amount * Progress.ExperienceMultiplier);
        [ContextMenu("Debug/Skip to Boss (Play Mode)")]
        public void SkipToBoss()
        {
            if (!Application.isPlaying || !IsCombatActive || Spawner.HasBossSpawned) return;
            if (Session.State == RunState.Drawing) Drawing.FinishDrawing(false, "Boss encounter");
            Spawner.SkipToBoss();
        }

        private void OnPauseRequestedHandler(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!_ui.CloseSubpanel()) Drawing.TogglePause();
        }

        private void OnBeforeBossHandler()
        {
            if (Deflect.IsDrawing) Drawing.FinishDrawing(false, "Boss encounter");
        }

        private void OnPlayerDiedHandler() => Session.EndRun(false);

        private void OnRunEndingHandler()
        {
            _drawing.Close();
            Deflect.Clear();
            Clock.Reset();
            _scheduler.Clear();
            Projectiles.Clear();
            _player.ClearTemporary();
            _feedback.Clear();
            CameraFeedback.Clear();
        }

        private void ClearRunObjects()
        {
            OnRunEndingHandler();
            Spawner.Clear();
            Experience.Clear();
            _ui.ResetRunPresentation();
            _ui.ShowCue("");
        }

    }
}
