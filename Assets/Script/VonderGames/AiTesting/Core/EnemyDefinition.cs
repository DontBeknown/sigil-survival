using UnityEngine;

namespace VonderGames.AiTesting.Core
{
    [CreateAssetMenu(menuName = "Wizard/Enemy")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private float _health = 40;
        public float Health => _health;
        [SerializeField] private float _speed = 2.5f;
        public float Speed => _speed;
        [SerializeField] private float _damage = 10;
        public float Damage => _damage;
        [SerializeField] private float _attackCooldown = 1;
        public float AttackCooldown => _attackCooldown;
        [SerializeField] private int _experience = 2;
        public int Experience => _experience;
        [SerializeField] private float _size = 0.9f;
        public float Size => _size;
        [SerializeField] private Color _color = Color.red;
        public Color Color => _color;
        [SerializeField] private bool _isBoss;
        public bool IsBoss => _isBoss;
    }
}
