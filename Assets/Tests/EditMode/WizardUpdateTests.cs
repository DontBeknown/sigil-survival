using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using VonderGames.AiTesting.Core;

namespace VonderGames.AiTesting.Tests
{
    public sealed class WizardUpdateTests
    {
        [Test]
        public void RarityAwardsPersistAndLearningIgnoresRarity()
        {
            var water = ScriptableObject.CreateInstance<SpellDefinition>();
            try
            {
                Set(water, "_kind", SpellKind.Water);
                Set(water, "_damage", 10f);
                Set(water, "_damageIncrement", 10f);
                Set(water, "_secondaryIncrement", 0.3f);
                var spells = new SpellDefinition[] { null, null, water };
                var progress = new RunProgress();
                progress.AddExperience(1000);
                var learn = progress.CreateOffer(UpgradeKind.Water, UpgradeRarity.Legendary, spells);
                Assert.That(learn.Bonus, Is.Zero);
                progress.TryApply(learn);
                Assert.That(progress.GetDamage(water), Is.EqualTo(10));
                progress.TryApply(progress.CreateOffer(UpgradeKind.Water, UpgradeRarity.Legendary, spells));
                progress.TryApply(progress.CreateOffer(UpgradeKind.Water, UpgradeRarity.Common, spells));
                Assert.That(progress.GetDamage(water), Is.EqualTo(50));
                Assert.That(progress.StunDuration, Is.EqualTo(2.7f).Within(0.001f));
                Assert.That(progress.GetSpellRank(SpellKind.Water), Is.EqualTo(3));
                progress.Reset();
                Assert.That(progress.GetDamage(water), Is.EqualTo(10));
            }
            finally { UnityEngine.Object.DestroyImmediate(water); }
        }

        [Test]
        public void FractionalExperienceAndCappedBonusesRemainAccurate()
        {
            var progress = new RunProgress();
            progress.AddExperience(10000);
            progress.TryApply(progress.CreateOffer(UpgradeKind.Experience, UpgradeRarity.Rare, null));
            float before = progress.Experience;
            progress.AddExperience(1);
            Assert.That(progress.Experience - before, Is.EqualTo(1.225f).Within(0.001f));
            for (int i = 0; i < 3; i++) progress.TryApply(progress.CreateOffer(UpgradeKind.Cooldown, UpgradeRarity.Legendary, null));
            var last = progress.CreateOffer(UpgradeKind.Cooldown, UpgradeRarity.Legendary, null);
            Assert.That(last.Bonus, Is.EqualTo(15));
            progress.TryApply(last);
            Assert.That(progress.CooldownMultiplier, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(progress.CreateOffer(UpgradeKind.Cooldown, UpgradeRarity.Common, null).Bonus, Is.Zero);
            for (int i = 0; i < 10; i++) progress.TryApply(progress.CreateOffer(UpgradeKind.Luck, UpgradeRarity.Legendary, null));
            Assert.That(progress.CriticalChance, Is.EqualTo(0.6f));
            Assert.That(progress.BonusDropChance, Is.EqualTo(0.5f));
        }

        [Test]
        public void EveryOfferSetHasOneElementAndTwoDistinctStats()
        {
            var definitions = new UpgradeDefinition[3];
            try
            {
                var kinds = new[] { UpgradeKind.Fire, UpgradeKind.Water, UpgradeKind.Thunder };
                for (int i = 0; i < kinds.Length; i++)
                {
                    definitions[i] = ScriptableObject.CreateInstance<UpgradeDefinition>();
                    Set(definitions[i], "_kind", kinds[i]);
                }
                var progress = new RunProgress();
                for (int seed = 0; seed < 100; seed++)
                {
                    var choices = progress.GetChoices(definitions, new System.Random(seed));
                    Assert.That(choices.Count, Is.EqualTo(3));
                    Assert.That(choices[0].IsSkill, Is.True);
                    Assert.That(choices[1].IsSkill || choices[2].IsSkill, Is.False);
                    Assert.That(choices[1].Kind, Is.Not.EqualTo(choices[2].Kind));
                    Assert.That(choices.Exists(offer => offer.Kind == UpgradeKind.Healing), Is.False);
                }
            }
            finally { foreach (var definition in definitions) UnityEngine.Object.DestroyImmediate(definition); }
        }

        [Test]
        public void LuckReweightsRarityWithoutChangingCommonWeight()
        {
            Assert.That(UpgradeOffer.RollRarity(new FixedRandom(0.64), 0), Is.EqualTo(UpgradeRarity.Common));
            Assert.That(UpgradeOffer.RollRarity(new FixedRandom(0.64), 100), Is.EqualTo(UpgradeRarity.Rare));
            Assert.That(UpgradeOffer.RollRarity(new FixedRandom(0.999), 100), Is.EqualTo(UpgradeRarity.Legendary));
        }

        [Test]
        public void RelativeSweepCatchesFastMovingPlayerAndRejectsNearMiss()
        {
            var player = new Bounds(Vector3.zero, Vector3.one);
            Assert.That(ProjectileThreat.Sweep(new Vector2(-4, 0), new Vector2(8, 0), player, 0.15f, out float fraction), Is.True);
            Assert.That(fraction, Is.InRange(0.4f, 0.5f));
            Assert.That(ProjectileThreat.Sweep(new Vector2(-4, 1), new Vector2(8, 0), player, 0.15f, out _), Is.False);
            Assert.That(ProjectileThreat.Sweep(new Vector2(2, 0), new Vector2(-4, 0), player, 0.15f, out _), Is.True);
        }

        [Test]
        public void DiamondLoopRequiresClockwiseReturnToStart()
        {
            var trace = new GestureTrace();
            trace.Reset(SpellKind.Deflect);
            trace.Begin(trace.Points[0]);
            trace.Move(trace.Points[1]);
            Assert.That(trace.HasFailed, Is.False);
            trace.Move(trace.Points[2]);
            trace.Move(trace.Points[3]);
            Assert.That(trace.IsComplete, Is.False);
            trace.Move(trace.Points[4]);
            Assert.That(trace.IsComplete, Is.True);
            trace.Reset(SpellKind.Deflect);
            trace.Begin(trace.Points[0]);
            trace.Move(trace.Points[3]);
            Assert.That(trace.HasFailed, Is.True);
        }

        private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private sealed class FixedRandom : System.Random
        {
            private readonly double _value;
            public FixedRandom(double value) => _value = value;
            public override double NextDouble() => _value;
        }
    }
}
