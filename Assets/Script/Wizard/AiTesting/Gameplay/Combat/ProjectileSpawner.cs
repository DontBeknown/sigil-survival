using System;
using System.Collections.Generic;
using UnityEngine;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Entity;
using VonderGames.AiTesting.Gameplay.Pooling;
using VonderGames.AiTesting.Gameplay.Session;

namespace VonderGames.AiTesting.Gameplay.Combat
{
    public sealed class ProjectileSpawner : IDisposable
    {
        private readonly SceneEntityPool<SpellProjectile> _pool;
        private readonly List<SpellProjectile> _hostileProjectiles = new List<SpellProjectile>();
        public IReadOnlyList<SpellProjectile> HostileProjectiles => _hostileProjectiles;
        public int ActiveCount => _pool.ActiveCount;
        private readonly PlayerController _player;
        private readonly RunSession _session;
        private readonly RunProgress _progress;
        private readonly CombatDefinition _combat;
        private readonly SpellDefinition[] _spells;
        private readonly CombatFeedback _feedback;
        private readonly SaveService _saves;
        private readonly CombatDamage _damage;
        private readonly Action _perfectDodge;
        private readonly Func<bool> _hasProtection;
        private readonly Action<SpellProjectile> _forgetAction;
        private readonly Action<SpellProjectile> _returnAction;

        public ProjectileSpawner(SpellProjectile prefab, Transform runtimeRoot, PlayerController player,
            RunSession session, RunProgress progress, CombatDefinition combat, SpellDefinition[] spells,
            CombatFeedback feedback, SaveService saves, CombatDamage damage, Action perfectDodge, Func<bool> hasProtection)
        {
            _player = player;
            _session = session;
            _progress = progress;
            _combat = combat;
            _spells = spells;
            _feedback = feedback;
            _saves = saves;
            _damage = damage;
            _perfectDodge = perfectDodge;
            _hasProtection = hasProtection;
            _forgetAction = ForgetProjectile;
            _returnAction = ReturnProjectile;
            _pool = new SceneEntityPool<SpellProjectile>(prefab, runtimeRoot, projectile => projectile.ResetForPool(), 256);
        }

        public SpellProjectile SpawnHostileProjectile(Vector2 position, Vector2 direction, EnemyAgent attacker = null)
        {
            SpellProjectile projectile = Spawn(position, direction, _spells[0], 15, true, attacker);
            _hostileProjectiles.Add(projectile);
            return projectile;
        }

        public void LaunchProjectile(SpellKind kind, Vector2 direction)
        {
            SpellDefinition spell = _spells[(int)kind];
            Spawn((Vector2)_player.transform.position + direction * 0.7f, direction, spell, _progress.GetDamage(spell), false, null);
            _player.ShowShot(direction);
        }

        public void RecordAttacker(EnemyAgent enemy)
        {
            foreach (SpellProjectile projectile in _hostileProjectiles)
                if (projectile != null) projectile.RecordAttacker(enemy);
        }

        public void ForgetProjectile(SpellProjectile shot) => _hostileProjectiles.Remove(shot);
        public void Clear() { _pool.ClearActive(); _hostileProjectiles.Clear(); }
        public void Dispose() { Clear(); _pool.Dispose(); }

        private SpellProjectile Spawn(Vector2 position, Vector2 direction, SpellDefinition spell, float damage, bool hostile, EnemyAgent attacker)
        {
            SpellProjectile projectile = _pool.Rent(position);
            projectile.Initialize(_session, _player, _progress, _combat, _feedback, _saves, _damage,
                _perfectDodge, _hasProtection, _forgetAction, _returnAction, direction, spell, damage, hostile, attacker);
            projectile.gameObject.SetActive(true);
            return projectile;
        }

        private void ReturnProjectile(SpellProjectile projectile)
        {
            ForgetProjectile(projectile);
            _pool.Return(projectile);
        }

    }
}
