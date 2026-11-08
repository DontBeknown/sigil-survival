using UnityEngine;

namespace VonderGames.AiTesting.Core
{
    [CreateAssetMenu(menuName = "Wizard/Combat")]
    public sealed class CombatDefinition : ScriptableObject
    {
        [SerializeField] private float _dashDistance = 3;
        public float DashDistance => _dashDistance;
        [SerializeField] private float _dashDuration = 0.18f;
        public float DashDuration => _dashDuration;
        [SerializeField] private float _dashCooldown = 0.8f;
        public float DashCooldown => _dashCooldown;
        [SerializeField] private float _perfectWindow = 0.12f;
        public float PerfectWindow => _perfectWindow;
        [SerializeField] private float _perfectScale = 0.2f;
        public float PerfectScale => _perfectScale;
        [SerializeField] private float _perfectDuration = 0.6f;
        public float PerfectDuration => _perfectDuration;
        [SerializeField, Range(0, 1)] private float _perfectHealFraction = 0.1f;
        public float PerfectHealFraction => _perfectHealFraction;
        [SerializeField, Min(0)] private float _bossHealthPerLevel = 0.15f;
        public float BossHealthPerLevel => _bossHealthPerLevel;
        [SerializeField] private float _deflectWindow = 0.12f;
        public float DeflectWindow => _deflectWindow;
        [SerializeField] private float _deflectCooldown = 4;
        public float DeflectCooldown => _deflectCooldown;
        [SerializeField] private float _deflectDamage = 40;
        public float DeflectDamage => _deflectDamage;
        [SerializeField] private float _deflectRadius = 3;
        public float DeflectRadius => _deflectRadius;
        [SerializeField] private float _reflectedDamage = 60;
        public float ReflectedDamage => _reflectedDamage;
        [SerializeField] private float _rangedStart = 60;
        public float RangedStart => _rangedStart;
        [SerializeField, Min(0)] private int _rangedEnemyCap = 5;
        public int RangedEnemyCap => _rangedEnemyCap;
        [SerializeField] private float _launchInterval = 0.65f;
        public float LaunchInterval => _launchInterval;
        [SerializeField] private float _hostileSpeed = 26;
        public float HostileSpeed => _hostileSpeed;
        [SerializeField, Min(0.01f)] private float _hostileRadius = 0.3f;
        public float HostileRadius => _hostileRadius;
        [SerializeField] private float _hostileLifetime = 3;
        public float HostileLifetime => _hostileLifetime;
        [SerializeField] private float _fireWidth = 0.42f;
        public float FireWidth => _fireWidth;
        [SerializeField, Min(0)] private float _fireExplosionRadius = 1.8f;
        public float FireExplosionRadius => _fireExplosionRadius;
        [SerializeField] private float _thunderRange = 9;
        public float ThunderRange => _thunderRange;
        [SerializeField] private float _thunderLinkRange = 4.5f;
        public float ThunderLinkRange => _thunderLinkRange;
        [SerializeField, Min(0)] private float _thunderLinkRangePerRank = 0.75f;
        public float ThunderLinkRangePerRank => _thunderLinkRangePerRank;
        [SerializeField] private int _experienceMultiplier = 3;
        public int ExperienceMultiplier => _experienceMultiplier;
        [SerializeField] private float _revealDuration = 1;
        public float RevealDuration => _revealDuration;
    }
}
