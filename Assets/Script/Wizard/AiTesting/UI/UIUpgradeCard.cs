using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VonderGames.AiTesting.Core;

namespace VonderGames.AiTesting.UI
{
    public sealed class UIUpgradeCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private UnityEngine.UI.Button _button;
        [SerializeField] private UnityEngine.UI.Image _background;
        [SerializeField] private UnityEngine.UI.Image _header;
        [SerializeField] private TMP_Text _category;
        [SerializeField] private TMP_Text _icon;
        [SerializeField] private TMP_Text _key;
        [SerializeField] private TMP_Text _rarity;
        [SerializeField] private UnityEngine.UI.Image[] _edges;
        [SerializeField] private UnityEngine.UI.Image[] _innerEdges;
        [SerializeField] private UnityEngine.UI.Image[] _impactEdges;
        [SerializeField] private RectTransform _impact;
        [SerializeField] private UnityEngine.UI.Image[] _sparks;
        [SerializeField] private UnityEngine.UI.Image[] _rays;
        [SerializeField] private UnityEngine.UI.Image[] _corners;

        private Color _color;
        private float _age = 2;
        private int _tier;
        private bool _isRevealed;
        private bool _isReducedFlash;
        private bool _isSkipped;
        private bool _isHovered;
        private bool _isSelected;

        private void Update()
        {
            if (!_isRevealed) return;
            _age += Time.unscaledDeltaTime;
            bool focused = _button.interactable && (_isHovered || _isSelected);
            _background.color = Color.Lerp(new Color(0.035f, 0.055f, 0.085f), _color, focused ? 0.13f : 0.045f);
            float duration = _isSkipped ? 0.16f : 0.35f + _tier * 0.12f;
            float progress = Mathf.Clamp01(_age / duration);
            float alpha = (1 - progress) * (_isReducedFlash ? 0.18f : 0.7f);
            _impact.localScale = Vector3.one * (1 + progress * (0.035f + _tier * 0.025f));
            foreach (var edge in _impactEdges) edge.color = WithAlpha(_color, alpha);
            foreach (var edge in _edges) edge.color = WithAlpha(_color, focused ? 1 : 0.65f);
            for (int i = 0; i < _sparks.Length; i++)
            {
                bool active = !_isSkipped && i < _tier * 4 && progress < 1;
                _sparks[i].gameObject.SetActive(active);
                if (!active) continue;
                float angle = (i + 0.5f) * Mathf.PI * 2 / Mathf.Max(1, _tier * 4);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var card = (RectTransform)transform;
                Vector2 perimeter = GetPerimeter(direction, card.rect.size) * 1.04f;
                _sparks[i].rectTransform.anchoredPosition = perimeter + direction * progress * (20 + _tier * 18);
                _sparks[i].rectTransform.localScale = Vector3.one * (1 - progress * 0.6f);
                _sparks[i].rectTransform.localRotation = Quaternion.Euler(0, 0, 45 + progress * 150);
                _sparks[i].color = WithAlpha(_color, (1 - progress) * (_isReducedFlash ? 0.4f : 0.9f));
            }
            for (int i = 0; i < _rays.Length; i++)
            {
                bool active = !_isSkipped && _tier >= 2 && i < (_tier == 3 ? 16 : 8) && progress < 1;
                _rays[i].gameObject.SetActive(active);
                if (!active) continue;
                float angle = i * Mathf.PI * 2 / (_tier == 3 ? 16 : 8);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                _rays[i].rectTransform.anchoredPosition = GetPerimeter(direction, ((RectTransform)transform).rect.size) * (1.03f + progress * 0.32f);
                _rays[i].rectTransform.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
                _rays[i].rectTransform.sizeDelta = new Vector2((12 + _tier * 7) * (1 - progress), 2);
                _rays[i].color = WithAlpha(_color, (1 - progress) * (_isReducedFlash ? 0.15f : 0.6f));
            }
        }

        private void OnDisable() => ResetPresentation();

        public void Roll(int index)
        {
            ResetPresentation();
            _isRevealed = false;
            _color = new Color(0.3f, 0.45f, 0.6f);
            _background.color = new Color(0.035f, 0.055f, 0.085f);
            _header.color = new Color(0.07f, 0.12f, 0.18f);
            _category.text = "UNSEALING";
            _icon.text = "?";
            _icon.color = new Color(0.4f, 0.55f, 0.65f);
            _key.text = (index + 1).ToString();
            _rarity.text = "REVEALING";
            foreach (var edge in _edges) edge.color = WithAlpha(_color, 0.5f);
        }

        public void Reveal(UpgradeOffer offer, bool skipped, bool reducedFlash)
        {
            _isRevealed = true;
            _isSkipped = skipped;
            _isReducedFlash = reducedFlash;
            _tier = (int)offer.Rarity;
            _age = 0;
            _color = GetRarityColor(offer.Rarity);
            _header.color = Color.Lerp(new Color(0.045f, 0.075f, 0.11f), _color, 0.18f);
            _category.text = offer.IsSkill ? (offer.ResultingRank == 1 ? "NEW ELEMENT" : "ELEMENT UPGRADE") : "PASSIVE BLESSING";
            _icon.text = GetIcon(offer.Kind);
            _icon.color = _color;
            _rarity.text = offer.Rarity.ToString().ToUpperInvariant();
            _rarity.color = _color;
            foreach (var edge in _innerEdges) { edge.gameObject.SetActive(_tier >= 2); edge.color = WithAlpha(_color, 0.45f); }
            foreach (var corner in _corners) { corner.gameObject.SetActive(_tier == 3); corner.color = _color; }
            transform.DOKill();
            transform.localScale = Vector3.one * (skipped ? 0.98f : 0.95f - _tier * 0.025f);
            transform.DOScale(1, skipped ? 0.15f : 0.25f + _tier * 0.07f).SetEase(_tier == 0 ? Ease.OutCubic : Ease.OutBack, 1 + _tier * 0.6f).SetUpdate(true);
        }

        public void ResetPresentation()
        {
            transform.DOKill();
            transform.localScale = Vector3.one;
            _isRevealed = _isHovered = _isSelected = false;
            foreach (var edge in _impactEdges) edge.color = Color.clear;
            foreach (var spark in _sparks) spark.gameObject.SetActive(false);
            foreach (var ray in _rays) ray.gameObject.SetActive(false);
            foreach (var edge in _innerEdges) edge.gameObject.SetActive(false);
            foreach (var corner in _corners) corner.gameObject.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData) => _isHovered = true;
        public void OnPointerExit(PointerEventData eventData) => _isHovered = false;
        public void OnSelect(BaseEventData eventData) => _isSelected = true;
        public void OnDeselect(BaseEventData eventData) => _isSelected = false;

        public static Color GetRarityColor(UpgradeRarity rarity)
        {
            switch (rarity)
            {
                case UpgradeRarity.Rare: return new Color(0.25f, 0.72f, 1);
                case UpgradeRarity.Epic: return new Color(0.73f, 0.45f, 1);
                case UpgradeRarity.Legendary: return new Color(1, 0.77f, 0.28f);
                default: return new Color(0.52f, 0.64f, 0.72f);
            }
        }

        private static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

        private static Vector2 GetPerimeter(Vector2 direction, Vector2 size)
        {
            float fraction = Mathf.Max(Mathf.Abs(direction.x) / Mathf.Max(1, size.x * 0.5f), Mathf.Abs(direction.y) / Mathf.Max(1, size.y * 0.5f));
            return direction / fraction;
        }

        private static string GetIcon(UpgradeKind kind)
        {
            switch (kind)
            {
                case UpgradeKind.Fire: return "F";
                case UpgradeKind.Water: return "W";
                case UpgradeKind.Thunder: return "T";
                case UpgradeKind.Damage: return "DMG";
                case UpgradeKind.Area: return "AOE";
                case UpgradeKind.Experience: return "XP";
                case UpgradeKind.Luck: return "LCK";
                case UpgradeKind.Cooldown: return "CD";
                case UpgradeKind.Movement: return "SPD";
                case UpgradeKind.Health: return "HP";
                default: return "+";
            }
        }
    }
}
