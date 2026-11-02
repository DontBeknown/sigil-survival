using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using VonderGames.AiTesting.Core;

namespace VonderGames.AiTesting.Tests
{
    public sealed class WizardCoreTests
    {
        [Test]
        public void PrimitiveSpriteHasPersistentTextureAfterImport()
        {
            var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ScriptableObject/PrimitiveSquare.asset");
            Assert.That(sprite.texture, Is.Not.Null);
            Assert.That(UnityEditor.AssetDatabase.GetAssetPath(sprite.texture), Is.EqualTo("Assets/ScriptableObject/PrimitiveTexture.asset"));
            Assert.That(sprite.bounds.size.x, Is.EqualTo(1).Within(0.001f));
        }

        [Test]
        public void ExperiencePreservesOverflowAndQueuesEachLevel()
        {
            var progress = new RunProgress();
            progress.AddExperience(40);
            Assert.That(progress.Level, Is.EqualTo(3));
            Assert.That(progress.Experience, Is.EqualTo(15));
            Assert.That(progress.PendingChoices, Is.EqualTo(2));
            Assert.That(progress.RequiredExperience, Is.EqualTo(20));
        }

        [Test]
        public void UnlockCannotBeOfferedTwiceAndResetRestoresDefaults()
        {
            var upgrade = CreateUpgrade(UpgradeKind.Fireball, 1);
            try
            {
                var progress = new RunProgress();
                Assert.That(progress.IsEligible(upgrade), Is.False);
                progress.AddExperience(40);
                Assert.That(progress.TryApply(upgrade), Is.True);
                Assert.That(progress.IsUnlocked(SpellKind.Fireball), Is.True);
                Assert.That(progress.IsEligible(upgrade), Is.False);
                Assert.That(progress.TryApply(upgrade), Is.False);
                progress.Reset();
                Assert.That(progress.IsUnlocked(SpellKind.Fireball), Is.False);
                Assert.That(progress.Level, Is.EqualTo(1));
                Assert.That(progress.PendingChoices, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(upgrade); }
        }

        [Test]
        public void CooldownRanksMultiplyAndNeverMutateDefinition()
        {
            var upgrade = CreateUpgrade(UpgradeKind.Cooldown, 5);
            try
            {
                var progress = new RunProgress();
                progress.AddExperience(1000);
                for (int i = 0; i < 5; i++) Assert.That(progress.TryApply(upgrade), Is.True);
                Assert.That(progress.CooldownMultiplier, Is.EqualTo(0.59049f).Within(0.00001f));
                Assert.That(progress.TryApply(upgrade), Is.False);
                Assert.That(upgrade.MaximumRank, Is.EqualTo(5));
            }
            finally { UnityEngine.Object.DestroyImmediate(upgrade); }
        }

        [Test]
        public void ChoicesAreDistinctAndHealingRemainsWhenRanksExhausted()
        {
            var damage = CreateUpgrade(UpgradeKind.Damage, 1);
            var speed = CreateUpgrade(UpgradeKind.Movement, 1);
            var healing = CreateUpgrade(UpgradeKind.Healing, 5);
            try
            {
                var progress = new RunProgress();
                progress.AddExperience(1000);
                var choices = progress.GetChoices(new[] { damage, speed, healing }, new System.Random(1));
                Assert.That(choices.Count, Is.EqualTo(3));
                CollectionAssert.AllItemsAreUnique(choices);
                progress.TryApply(damage);
                progress.TryApply(speed);
                choices = progress.GetChoices(new[] { damage, speed, healing }, new System.Random(1));
                Assert.That(choices, Is.EqualTo(new[] { healing }));
                Assert.That(progress.TryApply(healing), Is.True);
                Assert.That(progress.IsEligible(healing), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(damage);
                UnityEngine.Object.DestroyImmediate(speed);
                UnityEngine.Object.DestroyImmediate(healing);
            }
        }

        [Test]
        public void SaveSurvivesNewServiceAndRecoversCorruptPrimary()
        {
            string directory = Path.Combine(Path.GetTempPath(), "wizard-test-" + Guid.NewGuid());
            string path = Path.Combine(directory, "save.json");
            try
            {
                var first = new SaveService(path);
                first.Data.BestKillCount = 12;
                first.Data.ReduceShake = true;
                Assert.That(first.Save(), Is.True);
                first.Data.BestKillCount = 24;
                Assert.That(first.Save(), Is.True);
                var second = new SaveService(path);
                second.Load();
                Assert.That(second.Data.BestKillCount, Is.EqualTo(24));
                Assert.That(second.Data.ReduceShake, Is.True);
                File.WriteAllText(path, "broken json");
                var recovered = new SaveService(path);
                recovered.Load();
                Assert.That(recovered.Data.BestKillCount, Is.EqualTo(12));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test]
        public void SaveFailureIsReportedAndDoesNotThrow()
        {
            string path = Path.Combine(Path.GetTempPath(), "wizard-file-" + Guid.NewGuid());
            File.WriteAllText(path, "A file cannot be a directory");
            try
            {
                var saves = new SaveService(Path.Combine(path, "save.json"));
                Assert.That(saves.Save(), Is.False);
                Assert.That(saves.LastError, Is.Not.Empty);
            }
            finally { File.Delete(path); }
        }

        private static UpgradeDefinition CreateUpgrade(UpgradeKind kind, int maximumRank)
        {
            var upgrade = ScriptableObject.CreateInstance<UpgradeDefinition>();
            typeof(UpgradeDefinition).GetField("_kind", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(upgrade, kind);
            typeof(UpgradeDefinition).GetField("_maximumRank", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(upgrade, maximumRank);
            return upgrade;
        }
    }
}
