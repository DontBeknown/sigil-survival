using System;
using System.Collections.Generic;

namespace VonderGames.AiTesting.Core
{
    public enum RunState { Title, Playing, UpgradeSelection, Paused, Victory, Defeat }

    public sealed class RunProgress
    {
        public event Action OnChanged;

        private readonly int[] _ranks = new int[7];
        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int PendingChoices { get; private set; }
        public int RequiredExperience => 10 + 5 * (Level - 1);
        public float DamageMultiplier => 1 + 0.15f * GetRank(UpgradeKind.Damage);
        public float CooldownMultiplier => (float)Math.Pow(0.9, GetRank(UpgradeKind.Cooldown));
        public float MovementMultiplier => 1 + 0.1f * GetRank(UpgradeKind.Movement);
        public float MaximumHealth => 100 + 20 * GetRank(UpgradeKind.Health);

        public int GetRank(UpgradeKind kind) => _ranks[(int)kind];

        public bool IsUnlocked(SpellKind kind)
        {
            return kind == SpellKind.Bolt || GetRank(kind == SpellKind.Fireball ? UpgradeKind.Fireball : UpgradeKind.Nova) > 0;
        }

        public void Reset()
        {
            Array.Clear(_ranks, 0, _ranks.Length);
            Level = 1;
            Experience = 0;
            PendingChoices = 0;
            OnChanged?.Invoke();
        }

        public void AddExperience(int amount)
        {
            Experience += Math.Max(0, amount);
            while (Experience >= RequiredExperience)
            {
                Experience -= RequiredExperience;
                Level++;
                PendingChoices++;
            }
            OnChanged?.Invoke();
        }

        public bool IsEligible(UpgradeDefinition upgrade)
        {
            if (upgrade.Kind == UpgradeKind.Healing)
                return true;

            return Level >= 2 && GetRank(upgrade.Kind) < upgrade.MaximumRank;
        }

        public bool TryApply(UpgradeDefinition upgrade)
        {
            if (PendingChoices == 0 || !IsEligible(upgrade))
                return false;

            if (upgrade.Kind != UpgradeKind.Healing)
                _ranks[(int)upgrade.Kind]++;

            PendingChoices--;
            OnChanged?.Invoke();
            return true;
        }

        public List<UpgradeDefinition> GetChoices(UpgradeDefinition[] definitions, Random random)
        {
            var eligible = new List<UpgradeDefinition>();
            UpgradeDefinition healing = null;
            foreach (var definition in definitions)
            {
                if (definition.Kind == UpgradeKind.Healing)
                    healing = definition;
                else if (IsEligible(definition))
                    eligible.Add(definition);
            }
            for (int i = eligible.Count - 1; i > 0; i--)
            {
                int other = random.Next(i + 1);
                (eligible[i], eligible[other]) = (eligible[other], eligible[i]);
            }
            if (eligible.Count > 3)
                eligible.RemoveRange(3, eligible.Count - 3);

            // At exhausted ranks only one meaningful healing option remains. Never duplicate cards.
            if (eligible.Count < 3 && healing != null)
                eligible.Add(healing);

            return eligible;
        }
    }
}
