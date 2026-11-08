using TMPro;
using UnityEngine;
using VonderGames.AiTesting.Core;

namespace VonderGames.AiTesting.Gameplay.Entity
{
    public sealed class CombatFeedback : MonoBehaviour
    {
        [SerializeField] private Transform _effectRoot;
        [SerializeField] private Transform _damageRoot;
        [SerializeField] private SpriteRenderer _particlePrefab;
        [SerializeField] private TextMeshProUGUI _numberPrefab;
        [SerializeField] private Material _lineMaterial;
        public Material LineMaterial => _lineMaterial;

        private SaveService _saves;
        private readonly Effect[] _particles = new Effect[96];
        private readonly Effect[] _lines = new Effect[24];
        private readonly Effect[] _numbers = new Effect[47];
        private readonly Effect[] _announcements = new Effect[1];

        private sealed class Effect
        {
            public GameObject Object;
            public SpriteRenderer Sprite;
            public LineRenderer Line;
            public TMP_Text Text;
            public float Life;
            public float Duration;
            public Color Color;
            public Vector3 Scale;
            public Vector3 Velocity;
            public bool IsRing;
        }

        private void Update()
        {
            if (_saves == null)
                return;

            Animate(_particles);
            Animate(_lines);
            Animate(_numbers);
            Animate(_announcements);
        }

        public void Initialize(SaveService saves) => _saves = saves;

        public void Clear()
        {
            foreach (var pool in new[] { _particles, _lines, _numbers, _announcements })
            {
                foreach (var effect in pool)
                {
                    if (effect == null)
                        continue;

                    effect.Life = 0;
                    effect.Object.SetActive(false);
                }
            }
        }

        public void Number(Vector2 position, float amount, bool isPlayer = false, bool critical = false)
        {
            Effect effect = Acquire(_numbers, 2);
            if (effect == null)
                return;

            effect.Text.text = isPlayer ? $"-{Mathf.CeilToInt(amount)}" : critical ? $"{Mathf.CeilToInt(amount)} CRIT" : Mathf.CeilToInt(amount).ToString();
            effect.Velocity = new Vector3(Random.Range(-0.35f, 0.35f), 1.2f, 0);
            Begin(effect, position + Vector2.up * 0.65f, isPlayer ? new Color(1, 0.3f, 0.35f) : new Color(1, 0.94f, 0.7f), 0.65f, Vector3.one * (critical ? 1.35f : 1));
        }

        public LineRenderer CreateIndicator(string name, Transform parent, Color color, int order)
        {
            var line = new GameObject(name, typeof(LineRenderer)).GetComponent<LineRenderer>();
            line.transform.SetParent(parent, false);
            line.sharedMaterial = _lineMaterial;
            line.useWorldSpace = false;
            line.widthMultiplier = 0.045f;
            line.startColor = line.endColor = color;
            line.sortingOrder = order;
            line.numCapVertices = 2;
            return line;
        }

        public void Announce(Vector2 position, string message, Color color)
        {
            Effect announcement = _announcements[0] ?? Acquire(_announcements, 2);
            announcement.Text.text = message;
            announcement.Velocity = Vector3.up * 0.2f;
            Begin(announcement, position + Vector2.up * 1.2f, color, 0.6f, Vector3.one);
        }

        public void TraceFailure(Vector2 position)
        {
            Color color = new Color(0.95f, 0.4f, 0.32f);
            for (int fragment = 0; fragment < 3; fragment++)
            {
                Effect effect = Acquire(_lines, 1);
                if (effect == null) break;
                float start = fragment * Mathf.PI * 2 / 3;
                effect.Line.positionCount = 3;
                for (int point = 0; point < 3; point++)
                {
                    float angle = start + point * 0.3f;
                    effect.Line.SetPosition(point, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)));
                }
                effect.Line.widthMultiplier = 0.07f;
                effect.IsRing = false;
                effect.Velocity = new Vector3(Mathf.Cos(start + 0.3f), Mathf.Sin(start + 0.3f)) * 0.8f;
                Begin(effect, position, color, 0.35f, Vector3.one * 0.65f);
            }
            Burst(position, color, 6, 0.4f);
        }

        public void Afterimage(SpriteRenderer source, float duration = 0.2f, bool isDash = false)
        {
            Effect effect = Acquire(_particles, 0);
            if (effect == null) return;
            effect.Sprite.sprite = source.sprite;
            effect.Velocity = Vector3.zero;
            Color color = isDash ? new Color(0.3f, 0.9f, 1, 0.3f) : new Color(source.color.r, source.color.g, source.color.b, 0.3f);
            Begin(effect, source.transform.position, color, duration, source.transform.lossyScale);
            effect.Object.transform.rotation = source.transform.rotation;
        }

        public void Shield(Vector2 position, float radius, Color color, float duration, bool broken = false)
        {
            Effect effect = Acquire(_lines, 1);
            if (effect == null) return;
            effect.Line.positionCount = broken ? 4 : 5;
            for (int i = 0; i < effect.Line.positionCount; i++)
            {
                float angle = Mathf.PI / 2 - i * Mathf.PI / 2;
                effect.Line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)));
            }
            effect.Line.widthMultiplier = 0.1f;
            effect.IsRing = true;
            effect.Velocity = Vector3.zero;
            Begin(effect, position, color, duration, Vector3.one * radius);
        }

        public void Star(Vector2 position, Color color, float radius)
        {
            Effect effect = Acquire(_lines, 1);
            if (effect == null) return;
            effect.Line.positionCount = 9;
            for (int i = 0; i < 9; i++)
            {
                float angle = i * Mathf.PI / 4;
                effect.Line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * (i % 2 == 0 ? 1 : 0.3f));
            }
            effect.Line.widthMultiplier = 0.045f;
            effect.IsRing = false;
            effect.Velocity = Vector3.zero;
            Begin(effect, position, color, 0.5f, Vector3.one * radius);
        }

        public void Burst(Vector2 position, Color color, int count, float radius)
        {
            for (int i = 0; i < count; i++)
            {
                Effect effect = Acquire(_particles, 0);
                if (effect == null)
                    return;

                effect.Sprite.sprite = _particlePrefab.sprite;
                float angle = i * Mathf.PI * 2 / count + Random.Range(-0.2f, 0.2f);
                effect.Velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius * 4;
                Begin(effect, position, color, Random.Range(0.18f, 0.32f), Vector3.one * Random.Range(0.08f, 0.18f));
            }
        }

        public void Ring(Vector2 position, float radius, Color color, float duration)
        {
            Effect effect = Acquire(_lines, 1);
            if (effect == null)
                return;

            effect.Line.positionCount = 49;
            for (int i = 0; i < 49; i++)
            {
                float angle = i * Mathf.PI * 2 / 48;
                effect.Line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0));
            }
            effect.Line.widthMultiplier = 0.12f;
            effect.IsRing = true;
            effect.Velocity = Vector3.zero;
            Begin(effect, position, color, duration, Vector3.one * radius);
        }

        public void Lightning(Vector2 start, Vector2 end, Color color, float width)
        {
            Effect effect = Acquire(_lines, 1);
            if (effect == null)
                return;

            Vector2 offset = end - start;
            Vector2 normal = new Vector2(-offset.y, offset.x).normalized;
            effect.Line.positionCount = 9;
            for (int i = 0; i < 9; i++)
            {
                Vector2 point = offset * (i / 8f);
                if (i > 0 && i < 8)
                    point += normal * Random.Range(-0.25f, 0.25f);

                effect.Line.SetPosition(i, point);
            }
            effect.Line.widthMultiplier = width;
            effect.IsRing = false;
            effect.Velocity = Vector3.zero;
            Begin(effect, start, color, 0.24f, Vector3.one);
        }

        private Effect Acquire(Effect[] pool, int kind)
        {
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i] != null && pool[i].Life > 0)
                    continue;

                if (pool[i] == null)
                {
                    var effect = new Effect();
                    if (kind == 0)
                    {
                        effect.Sprite = Instantiate(_particlePrefab, _effectRoot);
                        effect.Object = effect.Sprite.gameObject;
                    }
                    else if (kind == 1)
                    {
                        effect.Object = new GameObject("SpellLine");
                        effect.Object.transform.SetParent(_effectRoot, false);
                        effect.Line = effect.Object.AddComponent<LineRenderer>();
                        effect.Line.sharedMaterial = _lineMaterial;
                        effect.Line.useWorldSpace = false;
                        effect.Line.sortingOrder = 8;
                        effect.Line.numCapVertices = 3;
                    }
                    else
                    {
                        effect.Text = Instantiate(_numberPrefab, _damageRoot);
                        effect.Object = effect.Text.gameObject;
                    }
                    pool[i] = effect;
                }
                return pool[i];
            }
            return null;
        }

        private void Begin(Effect effect, Vector2 position, Color color, float duration, Vector3 scale)
        {
            effect.Object.transform.position = position;
            effect.Object.transform.localRotation = Quaternion.identity;
            effect.Scale = scale;
            effect.Color = color;
            if (_saves.Data.ReduceFlash && effect.Text == null)
                effect.Color.a = 0.55f;

            effect.Life = duration;
            effect.Duration = duration;
            effect.Object.SetActive(true);
            ApplyAppearance(effect, 0);
        }

        private void Animate(Effect[] pool)
        {
            foreach (var effect in pool)
            {
                if (effect == null || effect.Life <= 0)
                    continue;

                effect.Life = Mathf.Max(0, effect.Life - Time.unscaledDeltaTime);
                if (effect.Life <= 0)
                {
                    effect.Object.SetActive(false);
                    continue;
                }
                effect.Object.transform.position += effect.Velocity * Time.unscaledDeltaTime;
                ApplyAppearance(effect, 1 - effect.Life / effect.Duration);
            }
        }

        private static void ApplyAppearance(Effect effect, float progress)
        {
            Color color = effect.Color;
            color.a *= 1 - progress * progress;
            if (effect.Sprite != null)
            {
                effect.Sprite.color = color;
                effect.Object.transform.localScale = effect.Scale * (1 - progress * 0.8f);
            }
            else if (effect.Text != null)
            {
                effect.Text.color = color;
                effect.Object.transform.localScale = effect.Scale * (1 + 0.35f * Mathf.Exp(-progress * 12));
            }
            else
            {
                effect.Line.startColor = color;
                effect.Line.endColor = color;
                effect.Object.transform.localScale = effect.IsRing ? effect.Scale * (0.15f + 0.85f * (1 - Mathf.Pow(1 - progress, 3))) : effect.Scale;
            }
        }
    }
}
