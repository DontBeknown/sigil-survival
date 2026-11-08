using UnityEngine;

namespace VonderGames.AiTesting.Core
{
    public enum SpellKind { Bolt = 0, Fire = 1, Water = 2, Thunder = 3, Deflect = 4 }

    [CreateAssetMenu(menuName = "Wizard/Spell")]
    public sealed class SpellDefinition : ScriptableObject
    {
        [SerializeField] private SpellKind _kind;
        public SpellKind Kind => _kind;
        [SerializeField] private string _displayName;
        public string DisplayName => _displayName;
        [SerializeField] private float _damage = 20;
        public float Damage => _damage;
        [SerializeField] private float _cooldown = 0.35f;
        public float Cooldown => _cooldown;
        [SerializeField] private float _speed = 24;
        public float Speed => _speed;
        [SerializeField] private float _radius;
        public float Radius => _radius;
        [SerializeField] private Color _color = Color.cyan;
        public Color Color => _color;
        [SerializeField] private float _damageIncrement;
        public float DamageIncrement => _damageIncrement;
        [SerializeField] private float _secondaryIncrement;
        public float SecondaryIncrement => _secondaryIncrement;
    }
}
