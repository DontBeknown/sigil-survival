using UnityEngine;

namespace VonderGames.AiTesting.Core
{
    public enum UpgradeKind { Fire = 0, Water = 1, Damage = 2, Cooldown = 3, Movement = 4, Health = 5, Healing = 6, Thunder = 7, Area = 8, Experience = 9, Luck = 10 }

    [CreateAssetMenu(menuName = "Wizard/Upgrade")]
    public sealed class UpgradeDefinition : ScriptableObject
    {
        [SerializeField] private UpgradeKind _kind;
        public UpgradeKind Kind => _kind;
        [SerializeField] private string _displayName;
        public string DisplayName => _displayName;
        [SerializeField, TextArea] private string _description;
        public string Description => _description;
    }
}
