using System;
using System.Collections.Generic;

namespace VonderGames.AiTesting.Core
{
    public enum RunState { Title, Playing, UpgradeSelection, Paused, Victory, Defeat, Drawing }

    public sealed class RunProgress
    {
        public event Action OnChanged;

        private readonly int[] _ranks = new int[11];
        private readonly float[] _bonuses = new float[11];
        private readonly float[] _spellDamage = new float[5];
        private readonly float[] _spellSecondary = new float[5];
        private static readonly UpgradeKind[] _stats = { UpgradeKind.Damage, UpgradeKind.Area, UpgradeKind.Experience,
            UpgradeKind.Luck, UpgradeKind.Cooldown, UpgradeKind.Movement, UpgradeKind.Health };

        public int Level { get; private set; } = 1;
        public float Experience { get; private set; }
        public int PendingChoices { get; private set; }
        public int RequiredExperience => 5 + 3 * (Level - 1) + (Level - 1) * (Level - 1);
        public float DamageMultiplier => 1 + GetBonus(UpgradeKind.Damage) / 100;
        public float AreaMultiplier => 1 + GetBonus(UpgradeKind.Area) / 100;
        public float ExperienceMultiplier => 1 + GetBonus(UpgradeKind.Experience) / 100;
        public float CooldownReduction => Math.Min(60, GetBonus(UpgradeKind.Cooldown));
        public float CooldownMultiplier => 1 - CooldownReduction / 100;
        public float MovementMultiplier => 1 + GetBonus(UpgradeKind.Movement) / 100;
        public float MaximumHealth => 100 + GetBonus(UpgradeKind.Health);
        public float Luck => GetBonus(UpgradeKind.Luck);
        public float CriticalChance => Math.Min(0.6f, 0.05f + Luck * 0.005f);
        public float BonusDropChance => Math.Min(0.5f, Luck * 0.01f);
        public float StunDuration => Math.Min(3, 1.5f + _spellSecondary[(int)SpellKind.Water]);
        public int ThunderTargets => 4 + Math.Max(0, GetSpellRank(SpellKind.Thunder) - 1);

        public float GetThunderLinkRange(CombatDefinition combat) =>
            combat.ThunderLinkRange + Math.Max(0, GetSpellRank(SpellKind.Thunder) - 1) * combat.ThunderLinkRangePerRank;

        public int GetRank(UpgradeKind kind) => _ranks[(int)kind];
        public float GetBonus(UpgradeKind kind) => _bonuses[(int)kind];
        public bool IsUnlocked(SpellKind kind) => kind == SpellKind.Bolt || kind == SpellKind.Deflect || GetSpellRank(kind) > 0;
        public int GetSpellRank(SpellKind kind) => kind == SpellKind.Bolt ? 0 : kind == SpellKind.Deflect ? 1 : GetRank(GetUpgradeKind(kind));

        public static UpgradeKind GetUpgradeKind(SpellKind kind)
        {
            switch (kind)
            {
                case SpellKind.Fire: return UpgradeKind.Fire;
                case SpellKind.Water: return UpgradeKind.Water;
                case SpellKind.Thunder: return UpgradeKind.Thunder;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        public float GetDamage(SpellDefinition spell) => (spell.Damage + _spellDamage[(int)spell.Kind]) * DamageMultiplier;

        public void Reset()
        {
            Array.Clear(_ranks, 0, _ranks.Length);
            Array.Clear(_bonuses, 0, _bonuses.Length);
            Array.Clear(_spellDamage, 0, _spellDamage.Length);
            Array.Clear(_spellSecondary, 0, _spellSecondary.Length);
            Level = 1;
            Experience = 0;
            PendingChoices = 0;
            OnChanged?.Invoke();
        }

        public void AddExperience(float amount)
        {
            Experience += Math.Max(0, amount) * ExperienceMultiplier;
            while (Experience >= RequiredExperience)
            {
                Experience -= RequiredExperience;
                Level++;
                PendingChoices++;
            }
            OnChanged?.Invoke();
        }

        public bool IsEligible(UpgradeDefinition upgrade) => upgrade.Kind != UpgradeKind.Healing && Level >= 2;

        // Retained for existing developer tools; live selections use the immutable offer.
        public bool TryApply(UpgradeDefinition upgrade)
        {
            if (!IsEligible(upgrade)) return false;
            return TryApply(CreateOffer(upgrade.Kind, UpgradeRarity.Common, null));
        }

        public bool TryApply(UpgradeOffer offer)
        {
            if (PendingChoices == 0) return false;
            _ranks[(int)offer.Kind]++;
            if (offer.IsSkill)
            {
                int spell = (int)GetSpellKind(offer.Kind);
                _spellDamage[spell] += offer.Bonus;
                _spellSecondary[spell] += offer.SecondaryBonus;
            }
            else
                _bonuses[(int)offer.Kind] += offer.Bonus;
            PendingChoices--;
            OnChanged?.Invoke();
            return true;
        }

        public List<UpgradeOffer> GetChoices(UpgradeDefinition[] definitions, Random random, SpellDefinition[] spells = null, CombatDefinition combat = null)
        {
            var elements = new List<UpgradeDefinition>();
            foreach (var definition in definitions)
            {
                if (definition.Kind == UpgradeKind.Fire || definition.Kind == UpgradeKind.Water || definition.Kind == UpgradeKind.Thunder)
                    elements.Add(definition);
            }
            UpgradeKind element = elements[random.Next(elements.Count)].Kind;
            int first = random.Next(_stats.Length);
            int second = random.Next(_stats.Length - 1);
            if (second >= first) second++;
            return new List<UpgradeOffer>
            {
                CreateOffer(element, UpgradeOffer.RollRarity(random, Luck), spells, combat),
                CreateOffer(_stats[first], UpgradeOffer.RollRarity(random, Luck), spells, combat),
                CreateOffer(_stats[second], UpgradeOffer.RollRarity(random, Luck), spells, combat)
            };
        }

        public UpgradeOffer CreateOffer(UpgradeKind kind, UpgradeRarity rarity, SpellDefinition[] spells, CombatDefinition combat = null)
        {
            int rank = GetRank(kind);
            float strength = UpgradeOffer.GetStrength(rarity);
            bool skill = kind == UpgradeKind.Fire || kind == UpgradeKind.Water || kind == UpgradeKind.Thunder;
            if (skill)
            {
                SpellKind element = GetSpellKind(kind);
                SpellDefinition spell = spells == null ? null : spells[(int)element];
                float increment = spell != null ? spell.DamageIncrement : element == SpellKind.Fire ? 30 : element == SpellKind.Water ? 10 : 16;
                float bonus = rank == 0 ? 0 : increment * strength;
                float secondary = rank == 0 ? 0 : (spell != null ? spell.SecondaryIncrement : element == SpellKind.Water ? 0.3f : 0) * strength;
                float before = spell != null ? GetDamage(spell) : 0;
                float after = before + bonus * DamageMultiplier;
                float linkRange = combat != null ? GetThunderLinkRange(combat) : 4.5f + Math.Max(0, rank - 1) * 0.75f;
                float linkIncrement = combat != null ? combat.ThunderLinkRangePerRank : 0.75f;
                float nextLinkRange = linkRange + (rank > 0 ? linkIncrement : 0);
                string detail = element == SpellKind.Fire ? $"Pierces 8 units; {(combat != null ? combat.FireWidth : 0.42f) * 2 * AreaMultiplier:0.##}-unit width.\nImpact explosion: {(combat != null ? combat.FireExplosionRadius : 1.8f) * AreaMultiplier:0.##}-unit radius." : element == SpellKind.Water
                    ? $"{(spell != null ? spell.Radius : 4.5f) * AreaMultiplier:0.##}-unit pulse. Stun {StunDuration:0.00}s -> {Math.Min(3, StunDuration + secondary):0.00}s."
                    : $"Targets {ThunderTargets} -> {4 + rank} (+2 in combo).\nLink range {linkRange:0.##} -> {nextLinkRange:0.##}; cast range {(combat != null ? combat.ThunderRange : 9):0.##}.";
                return new UpgradeOffer(kind, rarity, element.ToString(), rank + 1, bonus, secondary, before, after,
                    rank == 0 ? $"Learn standard rank 1: {after:0.##} damage.\nRarity does not improve the initial spell.\n{detail}"
                    : $"Damage {before:0.##} -> {after:0.##} (+{after - before:0.##})\n{detail}");
            }
            float baseBonus = kind == UpgradeKind.Damage || kind == UpgradeKind.Experience ? 15 : kind == UpgradeKind.Area ? 10 : kind == UpgradeKind.Health ? 20 : 5;
            float award = baseBonus * strength;
            float previous = GetBonus(kind);
            if (kind == UpgradeKind.Cooldown) award = Math.Min(award, 60 - CooldownReduction);
            float next = previous + award;
            string name = kind == UpgradeKind.Experience ? "XP Gain" : kind == UpgradeKind.Cooldown ? "Cooldown Reduction"
                : kind == UpgradeKind.Movement ? "Movement Speed" : kind == UpgradeKind.Health ? "Maximum Health" : kind.ToString();
            string unit = kind == UpgradeKind.Health ? " health" : kind == UpgradeKind.Luck ? " points" : "%";
            float displayBefore = kind == UpgradeKind.Health ? MaximumHealth : previous;
            float displayAfter = displayBefore + award;
            string description = $"+{award:0.##}{unit}\n{displayBefore:0.##} -> {displayAfter:0.##}{unit}";
            if (kind == UpgradeKind.Luck)
            {
                float factor = 1 + next / 100;
                float total = 65 + 35 * factor;
                description += $"\nCrit {CriticalChance * 100:0.#}% -> {Math.Min(60, 5 + next * 0.5f):0.#}%"
                    + $"\nBonus XP {BonusDropChance * 100:0.#}% -> {Math.Min(50, next):0.#}%"
                    + $"\nC/R/E/L: {65 / total * 100:0.#}/{25 * factor / total * 100:0.#}/{8 * factor / total * 100:0.#}/{2 * factor / total * 100:0.#}%";
            }
            return new UpgradeOffer(kind, rarity, name, rank + 1, award, 0, displayBefore, displayAfter, description);
        }

        private static SpellKind GetSpellKind(UpgradeKind kind) => kind == UpgradeKind.Fire ? SpellKind.Fire : kind == UpgradeKind.Water ? SpellKind.Water : SpellKind.Thunder;
    }
}
