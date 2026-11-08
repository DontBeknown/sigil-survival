using System;

namespace VonderGames.AiTesting.Core
{
    public enum UpgradeRarity { Common, Rare, Epic, Legendary }

    public sealed class UpgradeOffer
    {
        public UpgradeKind Kind { get; }
        public UpgradeRarity Rarity { get; }
        public bool IsSkill => Kind == UpgradeKind.Fire || Kind == UpgradeKind.Water || Kind == UpgradeKind.Thunder;
        public string DisplayName { get; }
        public int ResultingRank { get; }
        public float Bonus { get; }
        public float SecondaryBonus { get; }
        public float Before { get; }
        public float After { get; }
        public string Description { get; }
        public float Strength => GetStrength(Rarity);

        public UpgradeOffer(UpgradeKind kind, UpgradeRarity rarity, string displayName, int resultingRank,
            float bonus, float secondaryBonus, float before, float after, string description)
        {
            Kind = kind;
            Rarity = rarity;
            DisplayName = displayName;
            ResultingRank = resultingRank;
            Bonus = bonus;
            SecondaryBonus = secondaryBonus;
            Before = before;
            After = after;
            Description = description;
        }

        public static float GetStrength(UpgradeRarity rarity)
        {
            switch (rarity)
            {
                case UpgradeRarity.Rare: return 1.5f;
                case UpgradeRarity.Epic: return 2;
                case UpgradeRarity.Legendary: return 3;
                default: return 1;
            }
        }

        public static UpgradeRarity RollRarity(Random random, float luck)
        {
            float factor = 1 + luck / 100;
            double roll = random.NextDouble() * (65 + 35 * factor);
            if (roll < 65) return UpgradeRarity.Common;
            if (roll < 65 + 25 * factor) return UpgradeRarity.Rare;
            if (roll < 65 + 33 * factor) return UpgradeRarity.Epic;
            return UpgradeRarity.Legendary;
        }
    }
}
