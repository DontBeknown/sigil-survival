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
            progress.AddExperience(24);
            Assert.That(progress.Level, Is.EqualTo(3));
            Assert.That(progress.Experience, Is.EqualTo(10));
            Assert.That(progress.PendingChoices, Is.EqualTo(2));
            Assert.That(progress.RequiredExperience, Is.EqualTo(15));
        }

        [Test]
        public void ElementRanksContinueBeyondFormerCapAndReset()
        {
            var upgrade = CreateUpgrade(UpgradeKind.Fire);
            try
            {
                var progress = new RunProgress();
                Assert.That(progress.IsEligible(upgrade), Is.False);
                progress.AddExperience(100000);
                for (int i = 0; i < 50; i++) Assert.That(progress.TryApply(upgrade), Is.True);
                Assert.That(progress.GetSpellRank(SpellKind.Fire), Is.EqualTo(50));
                Assert.That(progress.IsEligible(upgrade), Is.True);
                progress.Reset();
                Assert.That(progress.IsUnlocked(SpellKind.Fire), Is.False);
                Assert.That(progress.PendingChoices, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(upgrade); }
        }

        [Test]
        public void EveryChoiceRemainsAvailableAtHighRanks()
        {
            var fire = CreateUpgrade(UpgradeKind.Fire);
            var water = CreateUpgrade(UpgradeKind.Water);
            var thunder = CreateUpgrade(UpgradeKind.Thunder);
            try
            {
                var progress = new RunProgress();
                progress.AddExperience(100000);
                for (int i = 0; i < 20; i++)
                {
                    progress.TryApply(fire);
                    progress.TryApply(water);
                    progress.TryApply(thunder);
                }
                var choices = progress.GetChoices(new[] { fire, water, thunder }, new System.Random(1));
                Assert.That(choices.Count, Is.EqualTo(3));
                Assert.That(choices[0].IsSkill, Is.True);
                Assert.That(choices[1].IsSkill || choices[2].IsSkill, Is.False);
                Assert.That(choices[1].Kind, Is.Not.EqualTo(choices[2].Kind));
                Assert.That(progress.StunDuration, Is.EqualTo(3));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fire);
                UnityEngine.Object.DestroyImmediate(water);
                UnityEngine.Object.DestroyImmediate(thunder);
            }
        }

        [TestCase(SpellKind.Fire)]
        [TestCase(SpellKind.Water)]
        [TestCase(SpellKind.Thunder)]
        [TestCase(SpellKind.Deflect)]
        public void FastSegmentsReachOrderedCheckpoints(SpellKind kind)
        {
            var trace = new GestureTrace();
            trace.Reset(kind);
            Assert.That(trace.Begin(trace.Points[0]), Is.True);
            for (int i = 1; i < trace.Points.Length; i++) trace.Move(trace.Points[i]);
            Assert.That(trace.IsComplete, Is.True);
            Assert.That(trace.HasFailed, Is.False);
        }

        [Test]
        public void SkippingCheckpointRejectsTrace()
        {
            var trace = new GestureTrace();
            trace.Reset(SpellKind.Fire);
            trace.Begin(trace.Points[0]);
            trace.Move(trace.Points[2]);
            Assert.That(trace.HasFailed, Is.True);
            Assert.That(trace.IsComplete, Is.False);
        }

        [Test]
        public void ComboHonorsOrderingRefreshAndExpiry()
        {
            var combo = new SpellCombo();
            Assert.That(combo.Cast(SpellKind.Thunder), Is.False);
            combo.Cast(SpellKind.Water);
            combo.Advance(2);
            combo.Cast(SpellKind.Bolt);
            Assert.That(combo.Remaining, Is.EqualTo(1));
            Assert.That(combo.Cast(SpellKind.Thunder), Is.True);
            Assert.That(combo.Cast(SpellKind.Thunder), Is.False);
            combo.Cast(SpellKind.Water);
            combo.Cast(SpellKind.Fire);
            Assert.That(combo.Cast(SpellKind.Thunder), Is.False);
            combo.Cast(SpellKind.Water);
            combo.Advance(4);
            Assert.That(combo.Cast(SpellKind.Thunder), Is.False);
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

        private static UpgradeDefinition CreateUpgrade(UpgradeKind kind)
        {
            var upgrade = ScriptableObject.CreateInstance<UpgradeDefinition>();
            typeof(UpgradeDefinition).GetField("_kind", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(upgrade, kind);
            return upgrade;
        }
    }
}
