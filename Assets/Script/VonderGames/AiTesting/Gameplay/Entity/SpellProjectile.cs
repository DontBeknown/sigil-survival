using UnityEngine;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Instance;

namespace VonderGames.AiTesting.Gameplay.Entity
{
    public sealed class SpellProjectile : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;

        private GameplayInstance _game;
        private Vector2 _velocity;
        private SpellDefinition _spell;
        private float _damage;
        private float _life;
        private bool _isHostile;
        private bool _hasHit;
        private ContactFilter2D _filter;
        private readonly RaycastHit2D[] _hits = new RaycastHit2D[8];

        private void Update()
        {
            if (_game.State != RunState.Playing || _hasHit)
                return;

            float delta = Time.deltaTime;
            Vector2 position = transform.position;
            Vector2 step = _velocity * delta;
            int count = Physics2D.CircleCast(position, 0.15f, step.normalized, _filter, _hits, step.magnitude);
            int nearest = -1;
            float distance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (_hits[i].distance < distance)
                {
                    nearest = i;
                    distance = _hits[i].distance;
                }
            }
            if (nearest >= 0)
            {
                transform.position = _hits[nearest].centroid;
                if (_isHostile)
                {
                    if (_hits[nearest].collider.TryGetComponent<PlayerController>(out var player))
                        player.ApplyDamage(_damage, _velocity.normalized);
                }
                else if (_spell.Kind == SpellKind.Fireball)
                    _game.DamageArea(transform.position, _spell.Radius, _damage, false, _spell.Color);
                else if (_hits[nearest].collider.TryGetComponent<EnemyAgent>(out var enemy))
                    enemy.ApplyDamage(_damage, _velocity.normalized, 1.5f);

                Despawn();
                return;
            }
            transform.position = position + step;
            _life -= delta;
            if (_life <= 0)
            {
                if (!_isHostile && _spell.Kind == SpellKind.Fireball)
                    _game.DamageArea(transform.position, _spell.Radius, _damage, false, _spell.Color);

                Despawn();
            }
        }

        public void Initialize(GameplayInstance game, Vector2 direction, SpellDefinition spell, float damage, bool isHostile = false)
        {
            _game = game;
            _spell = spell;
            _damage = damage;
            _isHostile = isHostile;
            _velocity = direction.normalized * (isHostile ? 6 : spell.Speed);
            _life = isHostile ? 6 : 1.5f;
            _filter = new ContactFilter2D { useLayerMask = true, layerMask = (1 << 10) | (1 << (isHostile ? 8 : 9)), useTriggers = false };
            _renderer.color = isHostile ? new Color(1, 0.25f, 0.5f) : spell.Color;
            transform.localScale = Vector3.one * (isHostile || spell.Kind == SpellKind.Fireball ? 0.4f : 0.23f);
        }

        private void Despawn()
        {
            _hasHit = true;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
