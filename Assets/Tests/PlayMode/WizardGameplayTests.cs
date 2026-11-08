using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using VonderGames.AiTesting.Gameplay.Manager;
using UnityEngine.TestTools;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Entity;
using VonderGames.AiTesting.Gameplay.Instance;

namespace VonderGames.AiTesting.Tests
{
    public sealed class WizardGameplayTests
    {
        private GameplayInstance _game;
        private string _saveDirectory;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _saveDirectory = Path.Combine(Path.GetTempPath(), "wizard-playtest-" + Guid.NewGuid());
            SceneManager.sceneLoaded += OnSceneLoadedHandler;
            try
            {
                yield return SceneManager.LoadSceneAsync("Initialize");
                yield return null;
            }
            finally
            {
                SceneManager.sceneLoaded -= OnSceneLoadedHandler;
            }
            _game.StartRun();
            yield return null;
            yield return null;
        }

        private void OnSceneLoadedHandler(Scene scene, LoadSceneMode mode)
        {
            _game = UnityEngine.Object.FindFirstObjectByType<GameplayInstance>();
            // Inject before Start constructs services so every owner shares the isolated save.
            typeof(GameplayInstance).GetField("_saves", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_game, new SaveService(Path.Combine(_saveDirectory, "save.json")));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _game.ReturnToTitle();
            yield return null;
            Time.timeScale = 1;
            if (Directory.Exists(_saveDirectory)) Directory.Delete(_saveDirectory, true);
        }

        [UnityTest]
        public IEnumerator QueuedUpgradesPreserveOverflow()
        {
            _game.AddExperience(25);
            Assert.That(_game.State, Is.EqualTo(RunState.UpgradeSelection));
            Assert.That(Time.timeScale, Is.Zero);
            for (int i = 0; i < 3; i++)
            {
                yield return null;
                yield return null;
                yield return new WaitForSecondsRealtime(1.1f);
                _game.ChooseUpgrade(i);
            }
            Assert.That(_game.State, Is.EqualTo(RunState.Playing));
            Assert.That(_game.Progress.Experience, Is.EqualTo(1));
            Assert.That(_game.Progress.PendingChoices, Is.Zero);
        }

        [UnityTest]
        public IEnumerator DrawingSlowsCombatAndCancellationCostsNoCooldown()
        {
            yield return UnlockElements();
            Assert.That(_game.BeginDrawing(SpellKind.Fire), Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0.15f).Within(0.001f));
            float elapsed = _game.Elapsed;
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(_game.Elapsed, Is.GreaterThan(elapsed));
            Assert.That(_game.Elapsed - elapsed, Is.LessThan(0.1f));
            _game.FinishDrawing(false, "Cancelled");
            Assert.That(_game.GetCooldown(SpellKind.Fire), Is.Zero);
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(_game.BeginDrawing(SpellKind.Fire), Is.False);
        }

        [UnityTest]
        public IEnumerator DrawingTimeoutUsesRealTime()
        {
            yield return UnlockElements();
            _game.BeginDrawing(SpellKind.Fire);
            yield return new WaitForSecondsRealtime(3.2f);
            Assert.That(_game.State, Is.EqualTo(RunState.Playing));
            Assert.That(_game.GetCooldown(SpellKind.Fire), Is.Zero);
        }

        [UnityTest]
        public IEnumerator WaterStunsAndThunderConsumesCombo()
        {
            yield return UnlockElements();
            var definitions = (EnemyDefinition[])typeof(GameplayInstance).GetField("_enemies", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_game);
            var enemy = _game.SpawnEnemy(definitions[2], new Vector2(2, 0));
            _game.BeginDrawing(SpellKind.Water);
            _game.FinishDrawing(true, "");
            Assert.That(enemy.IsStunned, Is.True);
            Assert.That(_game.ComboRemaining, Is.EqualTo(3));
            yield return null;
            yield return null;
            Assert.That(_game.BeginDrawing(SpellKind.Thunder), Is.True);
            _game.FinishDrawing(true, "");
            Assert.That(_game.ComboRemaining, Is.Zero);
            Assert.That(_game.GetCooldown(SpellKind.Thunder), Is.GreaterThan(0));
            Assert.That(enemy.Health, Is.InRange(definitions[2].Health - 140, definitions[2].Health - 70));
        }

        [UnityTest]
        public IEnumerator BossArrivesAtFiveMinutesAndSkipCannotDuplicate()
        {
            _game.AdvanceTime(299 - _game.Elapsed);
            Assert.That(_game.HasBossSpawned, Is.False);
            _game.AdvanceTime(1);
            Assert.That(_game.HasBossSpawned, Is.True);
            var boss = _game.ActiveEnemies[0];
            _game.SkipToBoss();
            Assert.That(_game.ActiveEnemies.Count, Is.EqualTo(1));
            Assert.That(_game.ActiveEnemies[0], Is.SameAs(boss));
            Assert.That(_game.State, Is.EqualTo(RunState.Playing));
            boss.ApplyDamage(100000, Vector2.right);
            Assert.That(_game.State, Is.EqualTo(RunState.Victory));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DeathDuringDrawingAndRestartRestoreTimeAndState()
        {
            yield return UnlockElements();
            _game.BeginDrawing(SpellKind.Fire);
            _game.Player.ApplyDamage(1000, Vector2.zero);
            Assert.That(_game.State, Is.EqualTo(RunState.Defeat));
            Assert.That(Time.timeScale, Is.Zero);
            _game.StartRun();
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(_game.Progress.Level, Is.EqualTo(1));
            Assert.That(_game.ComboRemaining, Is.Zero);
            Assert.That(_game.HasBossSpawned, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PauseFreezesCombatAndPlayerInput()
        {
            _game.TogglePause();
            float elapsed = _game.Elapsed;
            Vector3 position = _game.Player.transform.position;
            yield return new WaitForSecondsRealtime(0.15f);
            _game.AdvanceTime(1);
            Assert.That(_game.Elapsed, Is.EqualTo(elapsed));
            Assert.That(_game.Player.transform.position, Is.EqualTo(position));
            Assert.That(UnityEngine.Object.FindFirstObjectByType<InputManager>().Actions.Player.enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator HostileWallHitDoesNotDamagePlayer()
        {
            _game.SpawnHostileProjectile(new Vector2(28.5f, 0), Vector2.right);
            yield return new WaitForSeconds(0.25f);
            Assert.That(_game.Player.Health, Is.EqualTo(100));
        }

        [UnityTest]
        public IEnumerator NewInputSystemKeepsDiagonalMovementNormalized()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D));
                yield return null;
                yield return new WaitForFixedUpdate();
                Assert.That(_game.Player.GetComponent<Rigidbody2D>().linearVelocity.magnitude, Is.EqualTo(6).Within(0.05f));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }

        [UnityTest]
        public IEnumerator FirePiercesDistinctEnemiesWithoutRepeatedDamage()
        {
            yield return UnlockElements();
            var definitions = (EnemyDefinition[])typeof(GameplayInstance).GetField("_enemies", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_game);
            var first = _game.SpawnEnemy(definitions[2], new Vector2(2, 0));
            var second = _game.SpawnEnemy(definitions[2], new Vector2(4, 0));
            first.Stun(3);
            second.Stun(3);
            Physics2D.SyncTransforms();
            _game.BeginDrawing(SpellKind.Fire);
            _game.FinishDrawing(true, "");
            yield return new WaitForSeconds(0.3f);
            // A neutral bolt may also hit the first target; the second receives just the lance.
            Assert.That(second.Health, Is.EqualTo(definitions[2].Health - 80).Or.EqualTo(Mathf.Max(0, definitions[2].Health - 160)));
            float remaining = second.Health;
            yield return new WaitForSeconds(0.1f);
            Assert.That(second.Health, Is.EqualTo(remaining));
        }

        [UnityTest]
        public IEnumerator StunnedEnemyCannotDealContactDamage()
        {
            yield return UnlockElements();
            var definitions = (EnemyDefinition[])typeof(GameplayInstance).GetField("_enemies", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_game);
            var enemy = _game.SpawnEnemy(definitions[2], new Vector2(0.5f, 0));
            enemy.Stun(2);
            yield return new WaitForSeconds(0.3f);
            Assert.That(_game.Player.Health, Is.EqualTo(100));
            Assert.That(enemy.IsStunned, Is.True);
        }

        [UnityTest]
        public IEnumerator DeflectWhiffSpendsCooldownWithoutOpeningDrawing()
        {
            Assert.That(_game.AttemptDeflect(), Is.False);
            Assert.That(_game.State, Is.EqualTo(RunState.Playing));
            Assert.That(_game.GetCooldown(SpellKind.Deflect), Is.GreaterThan(3.9f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DeflectSuspendsOneShotAndSuccessRefreshesCooldowns()
        {
            var first = _game.SpawnHostileProjectile(new Vector2(1.2f, 0), Vector2.left);
            var second = _game.SpawnHostileProjectile(new Vector2(1.8f, 0), Vector2.left);
            Assert.That(_game.AttemptDeflect(), Is.True);
            Assert.That(first.IsSuspended, Is.True);
            Assert.That(second.IsSuspended, Is.False);
            Vector3 position = first.transform.position;
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(first.transform.position, Is.EqualTo(position));
            _game.FinishDrawing(true, "");
            Assert.That(first.IsHostile, Is.False);
            Assert.That(first.IsSuspended, Is.False);
            Assert.That(_game.GetCooldown(SpellKind.Deflect), Is.Zero);
            Assert.That(_game.Player.DashCooldown, Is.Zero);
            Assert.That(_game.HasDeflectProtection, Is.False);
            _game.Player.ApplyDamage(1, Vector2.left);
            Assert.That(_game.Player.Health, Is.EqualTo(99));
        }

        [UnityTest]
        public IEnumerator DeflectProtectsDuringDrawingAndCancellationEndsProtection()
        {
            var shot = _game.SpawnHostileProjectile(new Vector2(1.2f, 0), Vector2.left);
            Assert.That(_game.AttemptDeflect(), Is.True);
            _game.FinishDrawing(false, "CANCELLED");
            Assert.That(shot.IsHostile, Is.True);
            Assert.That(shot.IsSuspended, Is.False);
            Assert.That(_game.GetCooldown(SpellKind.Deflect), Is.GreaterThan(0));
            shot.Remove();
            _game.RefreshCooldowns();
            yield return null;
            yield return null;
            var interrupted = _game.SpawnHostileProjectile(new Vector2(1.2f, 0), Vector2.left);
            Assert.That(_game.AttemptDeflect(), Is.True);
            Assert.That(_game.HasDeflectProtection, Is.True);
            _game.Player.ApplyDamage(1, Vector2.left);
            Assert.That(_game.Player.Health, Is.EqualTo(100));
            Assert.That(_game.State, Is.EqualTo(RunState.Drawing));
            Assert.That(interrupted.IsSuspended, Is.True);
            _game.FinishDrawing(false, "TRACE FAILED");
            Assert.That(_game.HasDeflectProtection, Is.True);
            Assert.That(_game.State, Is.EqualTo(RunState.Drawing));
            Assert.That(interrupted.IsSuspended, Is.True);
            _game.FinishDrawing(false, "CANCELLED");
            Assert.That(_game.HasDeflectProtection, Is.False);
            Assert.That(_game.State, Is.EqualTo(RunState.Playing));
            Assert.That(interrupted.IsSuspended, Is.False);
            Assert.That(interrupted.IsHostile, Is.True);
            _game.Player.ApplyDamage(1, Vector2.left);
            Assert.That(_game.Player.Health, Is.EqualTo(99));
        }

        [UnityTest]
        public IEnumerator SlowdownsChooseStrongerScaleAndPauseFreezesCombat()
        {
            yield return UnlockElements();
            _game.PerfectDodge();
            Assert.That(Time.timeScale, Is.EqualTo(0.2f));
            Assert.That(_game.PlayerScale, Is.EqualTo(1));
            Assert.That(_game.BeginDrawing(SpellKind.Fire), Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0.15f));
            Assert.That(_game.PlayerScale, Is.EqualTo(0.15f));
            _game.FinishDrawing(false, "CANCELLED");
            _game.TogglePause();
            float elapsed = _game.Elapsed;
            yield return new WaitForSecondsRealtime(0.7f);
            Assert.That(_game.Elapsed, Is.EqualTo(elapsed));
            _game.TogglePause();
            Assert.That(Time.timeScale, Is.EqualTo(0.2f));
        }

        [UnityTest]
        public IEnumerator RestartRemovesCaughtShotAndRestoresClocks()
        {
            var shot = _game.SpawnHostileProjectile(new Vector2(1.2f, 0), Vector2.left);
            _game.AttemptDeflect();
            _game.PerfectDodge();
            _game.StartRun();
            yield return null;
            Assert.That(shot == null || !shot.gameObject.activeSelf, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(_game.Player.DashCooldown, Is.Zero);
            Assert.That(_game.GetCooldown(SpellKind.Deflect), Is.Zero);
            Assert.That(_game.IsDeflectDrawing, Is.False);
        }

        [UnityTest]
        public IEnumerator DashLocksMovementDirectionAndIgnoresDamage()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                yield return null;
                yield return null;
                Vector2 start = _game.Player.Position;
                Assert.That(_game.Player.TryDash(), Is.True);
                _game.Player.ApplyDamage(1000, Vector2.left);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                yield return new WaitForSecondsRealtime(0.14f);
                Assert.That(_game.Player.Position.x - start.x, Is.GreaterThan(2));
                Assert.That(Mathf.Abs(_game.Player.Position.y - start.y), Is.LessThan(0.05f));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return new WaitForSecondsRealtime(0.1f);
                Assert.That(_game.Player.Position.x - start.x, Is.EqualTo(3).Within(0.05f));
                Assert.That(_game.Player.Health, Is.EqualTo(100));
                Assert.That(_game.Player.TryDash(), Is.False);
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }

        [UnityTest]
        public IEnumerator EarlyDashCrossingRewardsOnceAndPreservesProjectile()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                InputSystem.QueueStateEvent(mouse, new MouseState { position = _game.WorldToScreen(Vector2.right * 4) });
                yield return null;
                yield return null;
                var shot = _game.SpawnHostileProjectile(new Vector2(2.2f, 0), Vector2.left);
                _game.Player.TryDash();
                yield return new WaitForSecondsRealtime(0.14f);
                Assert.That(shot != null && shot.gameObject.activeSelf, Is.True);
                Assert.That(shot.HasRewarded, Is.True);
                Assert.That(Time.timeScale, Is.EqualTo(0.2f));
                Assert.That(_game.Player.Health, Is.EqualTo(100));
                var remaining = typeof(VonderGames.AiTesting.Gameplay.Session.GameplayClock).GetField("_perfectRemaining", BindingFlags.Instance | BindingFlags.NonPublic);
                float previous = (float)remaining.GetValue(_game.Clock);
                yield return new WaitForSecondsRealtime(0.1f);
                Assert.That((float)remaining.GetValue(_game.Clock), Is.LessThan(previous));
            }
            finally { InputSystem.RemoveDevice(mouse); }
        }

        [UnityTest]
        public IEnumerator LateDashCrossingGrantsImmunityWithoutSlowMotion()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                InputSystem.QueueStateEvent(mouse, new MouseState { position = _game.WorldToScreen(Vector2.right * 4) });
                yield return null;
                yield return null;
                var shot = _game.SpawnHostileProjectile(new Vector2(6.5f, 0), Vector2.left);
                _game.Player.TryDash();
                yield return new WaitForSecondsRealtime(0.22f);
                Assert.That(shot.HasRewarded, Is.False);
                Assert.That(_game.Player.Health, Is.EqualTo(100));
                Assert.That(Time.timeScale, Is.EqualTo(1));
            }
            finally { InputSystem.RemoveDevice(mouse); }
        }

        [UnityTest]
        public IEnumerator RevealSkipKeepsOffersAndDoesNotSelectWithSameKey()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                _game.AddExperience(5);
                var offer = _game.Choices[0];
                yield return null;
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit1));
                yield return null;
                yield return null;
                Assert.That(_game.Progress.PendingChoices, Is.EqualTo(1));
                Assert.That(_game.Choices[0], Is.SameAs(offer));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                yield return null;
                _game.ChooseUpgrade(0);
                Assert.That(_game.Progress.PendingChoices, Is.Zero);
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }

        [UnityTest]
        public IEnumerator RangedTurnsAreFairAndSpaced()
        {
            var definition = (EnemyDefinition)typeof(GameplayInstance).GetField("_rangedDefinition", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_game);
            var enemies = new[]
            {
                _game.SpawnEnemy(definition, Vector2.right * 10),
                _game.SpawnEnemy(definition, Vector2.up * 10),
                _game.SpawnEnemy(definition, Vector2.left * 10)
            };
            var times = new float[3];
            var timer = typeof(EnemyAgent).GetField("_attackTimer", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int sample = 0; sample < 70 && times[2] == 0; sample++)
            {
                yield return new WaitForSeconds(0.05f);
                for (int i = 0; i < enemies.Length; i++)
                    if (times[i] == 0 && (float)timer.GetValue(enemies[i]) > 0) times[i] = _game.Elapsed;
            }
            Assert.That(times[0], Is.GreaterThan(0));
            Assert.That(times[1] - times[0], Is.GreaterThanOrEqualTo(0.6f));
            Assert.That(times[2] - times[1], Is.GreaterThanOrEqualTo(0.6f));
        }

        [UnityTest]
        public IEnumerator RangedEnemyCapRejectsSixthAndAllowsReplacementAfterDeath()
        {
            var definition = (EnemyDefinition)typeof(GameplayInstance).GetField("_rangedDefinition", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_game);
            EnemyAgent first = null;
            for (int i = 0; i < 5; i++)
            {
                var enemy = _game.SpawnEnemy(definition, new Vector2(10, i * 2));
                Assert.That(enemy, Is.Not.Null);
                if (i == 0) first = enemy;
            }
            Assert.That(_game.ActiveRangedEnemyCount, Is.EqualTo(5));
            Assert.That(_game.SpawnEnemy(definition, Vector2.left * 10), Is.Null);
            first.ApplyDamage(100000, Vector2.right);
            Assert.That(_game.SpawnEnemy(definition, Vector2.left * 10), Is.Not.Null);
            Assert.That(_game.ActiveRangedEnemyCount, Is.EqualTo(5));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DeflectHasNoDrawingDeadlineAndRetainsProtectionAcrossRetries()
        {
            var shot = _game.SpawnHostileProjectile(new Vector2(1.2f, 0), Vector2.left);
            Assert.That(_game.AttemptDeflect(), Is.True);
            yield return new WaitForSecondsRealtime(3.3f);
            Assert.That(_game.IsDeflectDrawing, Is.True);
            Assert.That(_game.HasDeflectProtection, Is.True);
            Assert.That(shot.IsSuspended, Is.True);
            _game.FinishDrawing(false, "TIME EXPIRED");
            Assert.That(_game.HasDeflectProtection, Is.True);
            _game.FinishDrawing(true, "");
            Assert.That(_game.HasDeflectProtection, Is.False);
            Assert.That(shot.IsHostile, Is.False);
        }

        private IEnumerator UnlockElements()
        {
            _game.Progress.AddExperience(29);
            var definitions = (UpgradeDefinition[])typeof(GameplayInstance).GetField("_upgrades", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_game);
            foreach (var definition in definitions) _game.Progress.TryApply(definition);
            yield return null;
            yield return null;
        }
    }
}
