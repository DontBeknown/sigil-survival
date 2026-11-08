using System;
using UnityEngine;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Combat;
using VonderGames.AiTesting.Gameplay.Session;

namespace VonderGames.AiTesting.Gameplay.Entity
{
    public sealed class SpellProjectile : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;

        private RunSession _session;
        private PlayerController _player;
        private RunProgress _progress;
        private CombatDefinition _combat;
        private CombatFeedback _feedback;
        private SaveService _saves;
        private CombatDamage _damageService;
        private Action _perfectDodge;
        private Func<bool> _hasProtection;
        private Action<SpellProjectile> _forget;
        private Action<SpellProjectile> _return;
        private SpriteRenderer _core;

        private Vector2 _velocity;
        private SpellDefinition _spell;
        private EnemyAgent _attacker;
        private Vector2 _attackerPosition;
        private Vector2 _previousPlayer;
        private float _damage;
        private float _life;
        private float _distance;
        private float _radius;
        private float _reflectedTrailDistance;
        private bool _isHostile;
        private bool _isReflected;
        private bool _isSuspended;
        private bool _hasHit;
        private bool _hasRewarded;
        private bool _isCrossingPlayer;
        private ContactFilter2D _filter;
        private TrailRenderer _trail;
        private LineRenderer _cage;
        private LineRenderer _tether;
        private readonly RaycastHit2D[] _hits = new RaycastHit2D[160];
        private readonly EnemyHitHistory _visited = new EnemyHitHistory();
        public bool IsSuspended => _isSuspended;
        public bool HasRewarded => _hasRewarded;
        public bool IsHostile => _isHostile && !_hasHit;
        public int LeaseVersion { get; private set; }

        private void FixedUpdate()
        {
            if (!_session.IsCombatActive || _hasHit) return;
            if (_attacker != null) _attackerPosition = _attacker.transform.position;
            Vector2 player = _player.Position;
            if (_isSuspended)
            {
                _previousPlayer = player;
                return;
            }
            float delta = Mathf.Min(Time.fixedDeltaTime, _life);
            Vector2 position = transform.position;
            Vector2 step = _velocity * delta;
            bool lance = !_isHostile && !_isReflected && _spell.Kind == SpellKind.Fire;
            bool piercing = lance || _isReflected;
            if (lance) step = Vector2.ClampMagnitude(step, Mathf.Max(0, 8 - _distance));
            int count = Physics2D.CircleCast(position, _radius, step.normalized, _filter, _hits, step.magnitude);
            float wallDistance = step.magnitude;
            bool wallHit = false;
            for (int i = 0; i < count; i++)
            {
                if (_hits[i].collider.gameObject.layer != 10) continue;
                if (_hits[i].distance <= wallDistance) { wallDistance = _hits[i].distance; wallHit = true; }
            }
            if (_isHostile)
            {
                Bounds bounds = _player.HitBounds;
                Vector2 playerStep = player - _previousPlayer;
                bounds.center -= (Vector3)playerStep;
                bool crossing = ProjectileThreat.Sweep(position, step - playerStep, bounds, _radius, out float fraction);
                if (crossing && step.magnitude * fraction <= wallDistance && (!_isCrossingPlayer || _player.DashedThisStep))
                {
                    if (_player.IsDashing || _player.DashedThisStep)
                    {
                        if (!_hasRewarded && _player.DashStepAge + _player.DashStepDuration * fraction <= _combat.PerfectWindow)
                        {
                            _hasRewarded = true;
                            if (_player.TryRewardPerfectDodge())
                                _perfectDodge();
                        }
                        else if (!_isCrossingPlayer)
                            _feedback.Ring(player, 0.6f, new Color(0.45f, 0.8f, 0.9f, 0.5f), 0.15f);
                    }
                    else
                    {
                        if (_hasProtection())
                            _feedback.Shield(player, 0.9f, new Color(0.5f, 0.95f, 1), 0.16f);
                        _player.ApplyDamage(_damage, _velocity.normalized);
                        Remove();
                        return;
                    }
                }
                Bounds expanded = _player.HitBounds;
                expanded.Expand(_radius * 2);
                _isCrossingPlayer = expanded.Contains(position + step);
            }
            else
            {
                float closest = wallDistance;
                EnemyAgent first = null;
                for (int i = 0; i < count; i++)
                {
                    RaycastHit2D hit = _hits[i];
                    if (hit.distance > wallDistance || !hit.collider.TryGetComponent<EnemyAgent>(out var enemy) || enemy.IsDead) continue;
                    if (piercing)
                    {
                        if (!_visited.Add(enemy)) continue;
                        _damageService.DealPlayerDamage(enemy, _damage, _velocity.normalized, 4);
                        if (_hasHit || !_session.IsCombatActive) return;
                        if (lance)
                        {
                            _damageService.ExplodeFire(position + step.normalized * hit.distance, _damage, _visited);
                            if (_hasHit || !_session.IsCombatActive) return;
                        }
                        else
                            _feedback.Burst(enemy.transform.position, _renderer.color, 5, 0.5f);
                    }
                    else if (hit.distance < closest) { first = enemy; closest = hit.distance; }
                }
                if (first != null)
                {
                    _damageService.DealPlayerDamage(first, _damage, _velocity.normalized, 1.5f);
                    Remove();
                    return;
                }
            }
            transform.position = position + step.normalized * wallDistance;
            if (_isReflected)
            {
                _reflectedTrailDistance += wallDistance;
                if (_reflectedTrailDistance >= 0.6f)
                {
                    _reflectedTrailDistance = 0;
                    _feedback.Shield(transform.position, _radius, Color.cyan, 0.16f);
                }
            }
            _previousPlayer = player;
            _distance += wallDistance;
            _life -= delta;
            if (wallHit)
            {
                if (lance)
                    _damageService.ExplodeFire(transform.position, _damage, _visited);
                else
                    _feedback.Burst(transform.position, _renderer.color, 3, 0.2f);
                Remove();
            }
            else if (_life <= 0 || (lance && _distance >= 8))
            {
                if (lance)
                    _damageService.ExplodeFire(transform.position, _damage, _visited);
                else
                    _feedback.Afterimage(_renderer, 0.1f);
                Remove();
            }
        }

        private void LateUpdate()
        {
            if (!_isSuspended || _hasHit) return;
            _tether.SetPosition(0, Vector3.zero);
            _tether.SetPosition(1, transform.InverseTransformPoint(_player.transform.position));
        }

        public void Initialize(RunSession session, PlayerController player, RunProgress progress,
            CombatDefinition combat, CombatFeedback feedback, SaveService saves, CombatDamage damageService,
            Action perfectDodge, Func<bool> hasProtection, Action<SpellProjectile> forget,
            Action<SpellProjectile> returnToPool, Vector2 direction, SpellDefinition spell, float damage,
            bool isHostile = false, EnemyAgent attacker = null)
        {
            _session = session; _player = player; _progress = progress; _combat = combat;
            LeaseVersion++;
            _feedback = feedback; _saves = saves; _damageService = damageService;
            _perfectDodge = perfectDodge; _hasProtection = hasProtection; _forget = forget; _return = returnToPool;
            _hasHit = _hasRewarded = _isReflected = _isSuspended = _isCrossingPlayer = false;
            _distance = _reflectedTrailDistance = 0;
            _visited.Clear();
            if (_cage != null) _cage.enabled = _tether.enabled = false;
            _renderer.transform.localRotation = Quaternion.identity;

            _spell = spell;
            _damage = damage;
            _isHostile = isHostile;
            _attacker = attacker;
            _attackerPosition = attacker != null ? (Vector2)attacker.transform.position : (Vector2)transform.position - direction * 3;
            _previousPlayer = _player.Position;
            _velocity = direction.normalized * (isHostile ? _combat.HostileSpeed : spell.Speed);
            _life = isHostile ? _combat.HostileLifetime : 1.5f;
            _filter = new ContactFilter2D { useLayerMask = true, layerMask = (1 << 10) | (isHostile ? 0 : 1 << 9), useTriggers = false };
            bool lance = !isHostile && spell.Kind == SpellKind.Fire;
            _radius = isHostile ? _combat.HostileRadius : (lance ? _combat.FireWidth : 0.15f) * _progress.AreaMultiplier;
            Color color = isHostile ? new Color(1, 0.3f, 0.5f) : spell.Color;
            _renderer.color = color;
            _renderer.sortingOrder = isHostile ? 18 : 6;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            _renderer.transform.localScale = new Vector3(lance ? 1.6f : isHostile ? _radius * 3 : 0.45f, _radius * 2, 1);
            if (_trail == null) _trail = gameObject.AddComponent<TrailRenderer>();
            _trail.Clear();
            _trail.emitting = true;
            _trail.sharedMaterial = _feedback.LineMaterial;
            _trail.time = lance ? 0.16f : 0.09f;
            _trail.startWidth = _radius * 2;
            _trail.endWidth = 0;
            _trail.startColor = color;
            _trail.endColor = new Color(color.r, color.g, color.b, 0);
            _trail.sortingOrder = isHostile ? 17 : 6;
            _trail.minVertexDistance = 0.08f;
            if (_core == null)
            {
                var core = new GameObject("ProjectileCore", typeof(SpriteRenderer));
                core.transform.SetParent(_renderer.transform, false);
                core.transform.localScale = new Vector3(0.85f, 0.45f, 1);
                _core = core.GetComponent<SpriteRenderer>();
                _core.sprite = _renderer.sprite;
            }
            _core.sortingOrder = isHostile ? 19 : 7;
            _core.color = _saves.Data.ReduceFlash ? Color.Lerp(color, Color.white, 0.3f) : new Color(1, 0.95f, 0.8f);
            if (!isHostile) return;
            if (_cage != null) return;
            _cage = _feedback.CreateIndicator("CaughtCage", transform, Color.cyan, 22);
            _cage.positionCount = 5;
            for (int i = 0; i < 5; i++)
            {
                float angle = i * Mathf.PI / 2;
                _cage.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.65f);
            }
            _tether = _feedback.CreateIndicator("CaughtTether", transform, new Color(0.4f, 0.9f, 1, 0.6f), 21);
            _tether.positionCount = 2;
            _cage.enabled = _tether.enabled = false;
        }

        public bool TryPredictImpact(float horizon, out float impact)
        {
            impact = 0;
            if (!_isHostile || _isSuspended || _hasHit) return false;
            float duration = Mathf.Min(horizon, _life);
            if (!ProjectileThreat.Sweep(transform.position, _velocity * duration, _player.HitBounds, _radius, out float fraction)) return false;
            impact = duration * fraction;
            RaycastHit2D wall = Physics2D.CircleCast(transform.position, _radius, _velocity.normalized, _velocity.magnitude * impact, 1 << 10);
            return wall.collider == null;
        }

        public void RecordAttacker(EnemyAgent enemy)
        {
            if (_attacker == enemy)
            {
                _attackerPosition = enemy.transform.position;
                _attacker = null;
            }
        }

        public void Suspend()
        {
            _isSuspended = true;
            _trail.emitting = false;
            _cage.enabled = _tether.enabled = true;
        }

        public void Release(bool reflected)
        {
            Vector2 target = reflected ? (_attacker != null ? (Vector2)_attacker.transform.position : _attackerPosition) : (Vector2)_player.transform.position;
            Vector2 direction = target - (Vector2)transform.position;
            if (direction.sqrMagnitude < 0.0001f) direction = reflected ? -_velocity : _velocity;
            _velocity = direction.normalized * _combat.HostileSpeed;
            _isSuspended = false;
            _isReflected = reflected;
            _isHostile = !reflected;
            _previousPlayer = _player.Position;
            _isCrossingPlayer = false;
            _cage.enabled = _tether.enabled = false;
            _trail.Clear();
            _trail.emitting = true;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            if (reflected)
            {
                _damage = _combat.ReflectedDamage * _progress.DamageMultiplier;
                _radius = _combat.HostileRadius * _progress.AreaMultiplier;
                _filter.layerMask = (1 << 10) | (1 << 9);
                _renderer.color = Color.cyan;
                _renderer.transform.localRotation = Quaternion.Euler(0, 0, 45);
                _renderer.transform.localScale = Vector3.one * _radius * 2;
                _trail.startColor = Color.cyan;
                _trail.startWidth = _radius * 2;
                _forget(this);
            }
            _feedback.Shield(transform.position, 0.35f, reflected ? Color.cyan : new Color(1, 0.3f, 0.5f), 0.2f);
        }

        public void Remove()
        {
            if (_hasHit) return;
            _hasHit = true;
            _forget(this);
            _return(this);
        }
        public void ResetForPool()
        {
            _hasHit = true;
            _isSuspended = _isReflected = _isHostile = _hasRewarded = _isCrossingPlayer = false;
            _attacker = null;
            _velocity = Vector2.zero;
            _life = _distance = _damage = _reflectedTrailDistance = 0;
            _visited.Clear();
            if (_trail != null) { _trail.emitting = false; _trail.Clear(); }
            if (_cage != null) _cage.enabled = _tether.enabled = false;
            Array.Clear(_hits, 0, _hits.Length);
            _forget(this);
        }
    }
}
