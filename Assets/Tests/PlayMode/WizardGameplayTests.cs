using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Entity;
using VonderGames.AiTesting.Gameplay.Instance;
using VonderGames.AiTesting.Gameplay.Manager;

namespace VonderGames.AiTesting.Tests
{
    public sealed class WizardGameplayTests
    {
        private GameplayInstance _game;
        private string _saveDirectory;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("Initialize");
            // Scene loading completes before the first Start callback.
            yield return null;
            _game = UnityEngine.Object.FindFirstObjectByType<GameplayInstance>();
            _saveDirectory = Path.Combine(Path.GetTempPath(), "wizard-playtest-" + Guid.NewGuid());
            typeof(GameplayInstance).GetField("_saves", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_game, new SaveService(Path.Combine(_saveDirectory, "save.json")));
            _game.StartRun();
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
        public IEnumerator PauseFreezesTimersAndDisablesCombatInput()
        {
            Assert.That(_game.TryCast(SpellKind.Bolt), Is.True);
            Assert.That(_game.TryCast(SpellKind.Bolt), Is.False);
            Assert.That(_game.TryCast(SpellKind.Fireball), Is.False);
            _game.TogglePause();
            float elapsed = _game.Elapsed;
            float cooldown = _game.GetCooldown(SpellKind.Bolt);
            Vector3 position = _game.Player.transform.position;
            yield return new WaitForSecondsRealtime(0.15f);
            _game.AdvanceTime(1);
            Assert.That(_game.Elapsed, Is.EqualTo(elapsed));
            Assert.That(_game.GetCooldown(SpellKind.Bolt), Is.EqualTo(cooldown));
            Assert.That(_game.Player.transform.position, Is.EqualTo(position));
            Assert.That(_game.TryCast(SpellKind.Bolt), Is.False);
            Assert.That(UnityEngine.Object.FindFirstObjectByType<InputManager>().Actions.Player.enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator QueuedUpgradesResumeOnlyAfterLastSelection()
        {
            _game.AddExperience(40);
            Assert.That(_game.State, Is.EqualTo(RunState.UpgradeSelection));
            Assert.That(Time.timeScale, Is.Zero);
            _game.ChooseUpgrade(0);
            Assert.That(_game.State, Is.EqualTo(RunState.UpgradeSelection));
            _game.ChooseUpgrade(0);
            Assert.That(_game.State, Is.EqualTo(RunState.Playing));
            Assert.That(_game.Progress.Experience, Is.EqualTo(15));
            yield return null;
        }

        [UnityTest]
        public IEnumerator BossSpawnsOnceAndDeathAwardsVictory()
        {
            _game.AdvanceTime(600);
            Assert.That(_game.HasBossSpawned, Is.True);
            Assert.That(_game.State, Is.EqualTo(RunState.Playing));
            Assert.That(_game.ActiveEnemies.Count, Is.EqualTo(1));
            var boss = _game.ActiveEnemies[0];
            Assert.That(boss.Definition.IsBoss, Is.True);
            _game.AdvanceTime(10);
            Assert.That(_game.ActiveEnemies.Count, Is.EqualTo(1));
            boss.ApplyDamage(100000, Vector2.right);
            Assert.That(_game.State, Is.EqualTo(RunState.Victory));
            Assert.That(_game.Saves.Data.BossVictories, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DamageInvulnerabilityAndRetryResetAllRunState()
        {
            _game.Player.ApplyDamage(10, Vector2.zero);
            _game.Player.ApplyDamage(10, Vector2.zero);
            Assert.That(_game.Player.Health, Is.EqualTo(90));
            yield return new WaitForSeconds(0.55f);
            _game.Player.ApplyDamage(1000, Vector2.zero);
            Assert.That(_game.State, Is.EqualTo(RunState.Defeat));
            for (int i = 0; i < 3; i++) _game.StartRun();
            yield return null;
            Assert.That(_game.Player.Health, Is.EqualTo(100));
            Assert.That(_game.Progress.Level, Is.EqualTo(1));
            Assert.That(_game.GetCooldown(SpellKind.Bolt), Is.Zero);
            Assert.That(_game.ActiveEnemies.Count, Is.Zero);
            Assert.That(_game.HasBossSpawned, Is.False);
            Assert.That(UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SweptBoltDamagesTargetOnce()
        {
            var definition = (EnemyDefinition[])typeof(GameplayInstance).GetField("_enemies", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_game);
            var enemy = _game.SpawnEnemy(definition[0], new Vector2(2, 0));
            typeof(PlayerController).GetProperty("AimDirection").GetSetMethod(true).Invoke(_game.Player, new object[] { Vector2.right });
            Physics2D.SyncTransforms();
            _game.TryCast(SpellKind.Bolt);
            yield return new WaitForSeconds(0.15f);
            Assert.That(enemy.Health, Is.EqualTo(20));
            yield return new WaitForSeconds(0.1f);
            Assert.That(enemy.Health, Is.EqualTo(20));
        }

        [UnityTest]
        public IEnumerator HostileProjectileHittingWallCannotDamagePlayer()
        {
            _game.SpawnHostileProjectile(new Vector2(28.5f, 0), Vector2.right);
            yield return new WaitForSeconds(0.25f);
            Assert.That(_game.Player.Health, Is.EqualTo(100));
        }

        [UnityTest]
        public IEnumerator UnlocksEnableAreaSpellsAndPreserveAuthoredDamage()
        {
            var upgrades = (UpgradeDefinition[])typeof(GameplayInstance).GetField("_upgrades", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_game);
            var definitions = (EnemyDefinition[])typeof(GameplayInstance).GetField("_enemies", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_game);
            _game.Progress.AddExperience(100);
            Assert.That(_game.Progress.TryApply(upgrades[0]), Is.True);
            Assert.That(_game.Progress.TryApply(upgrades[1]), Is.True);
            var first = _game.SpawnEnemy(definitions[2], new Vector2(2, 0));
            var second = _game.SpawnEnemy(definitions[2], new Vector2(2, 1));
            Assert.That(_game.TryCast(SpellKind.Nova), Is.True);
            Assert.That(first.Health, Is.EqualTo(145));
            Assert.That(second.Health, Is.EqualTo(145));
            Assert.That((float)typeof(EnemyAgent).GetField("_slowTimer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(first), Is.EqualTo(2));
            typeof(PlayerController).GetProperty("AimDirection").GetSetMethod(true).Invoke(_game.Player, new object[] { Vector2.right });
            Physics2D.SyncTransforms();
            Assert.That(_game.TryCast(SpellKind.Fireball), Is.True);
            yield return new WaitForSeconds(0.4f);
            Assert.That(first.Health, Is.EqualTo(95));
            Assert.That(second.Health, Is.EqualTo(95));
            Assert.That(_game.Spells[1].Damage, Is.EqualTo(50));
        }

        [UnityTest]
        public IEnumerator HeldMenuClickCannotCastAfterResume()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                var input = UnityEngine.Object.FindFirstObjectByType<InputManager>();
                _game.TogglePause();
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
                yield return null;
                _game.TogglePause();
                yield return null;
                yield return null;
                Assert.That(input.CanCast, Is.False);
                Assert.That(_game.GetCooldown(SpellKind.Bolt), Is.Zero);
                InputSystem.QueueStateEvent(mouse, new MouseState());
                yield return null;
            }
            finally { InputSystem.RemoveDevice(mouse); }
        }

        [UnityTest]
        public IEnumerator NewInputSystemMovesDiagonallyAtNormalizedSpeed()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D));
                yield return null;
                yield return new WaitForFixedUpdate();
                var velocity = _game.Player.GetComponent<Rigidbody2D>().linearVelocity;
                Assert.That(velocity.magnitude, Is.EqualTo(6).Within(0.05f));
                Assert.That(velocity.x, Is.GreaterThan(4));
                Assert.That(velocity.y, Is.GreaterThan(4));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }

        [UnityTest]
        public IEnumerator BossTelegraphsBeforeChargeAndStaysInsideArena()
        {
            _game.AdvanceTime(600);
            var boss = _game.ActiveEnemies[0];
            float deadline = Time.realtimeSinceStartup + 3;
            while (boss.State != EnemyState.TelegraphCharge && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That(boss.State, Is.EqualTo(EnemyState.TelegraphCharge));
            Vector2 position = boss.transform.position;
            yield return new WaitForSeconds(0.3f);
            Assert.That(Vector2.Distance(position, boss.transform.position), Is.LessThan(0.1f));
            deadline = Time.realtimeSinceStartup + 2;
            while (boss.State != EnemyState.Charge && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That(boss.State, Is.EqualTo(EnemyState.Charge));
            yield return new WaitForSeconds(0.7f);
            Assert.That(Mathf.Abs(boss.transform.position.x), Is.LessThan(29));
            Assert.That(Mathf.Abs(boss.transform.position.y), Is.LessThan(19));
        }
    }
}
