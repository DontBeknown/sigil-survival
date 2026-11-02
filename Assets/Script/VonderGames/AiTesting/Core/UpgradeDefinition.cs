using UnityEngine;

namespace VonderGames.AiTesting.Core
{
    public enum UpgradeKind { Fireball, Nova, Damage, Cooldown, Movement, Health, Healing }

    [CreateAssetMenu(menuName = "Wizard/Upgrade")]
    public sealed class UpgradeDefinition : ScriptableObject
    {
        [SerializeField] private UpgradeKind _kind;
        public UpgradeKind Kind => _kind;
        [SerializeField] private string _displayName;
        public string DisplayName => _displayName;
        [SerializeField, TextArea] private string _description;
        public string Description => _description;
        [SerializeField] private int _maximumRank = 5;
        public int MaximumRank => _maximumRank;
    }
}
