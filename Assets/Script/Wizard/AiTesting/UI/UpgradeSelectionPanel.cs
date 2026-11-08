using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Combat;
using VonderGames.AiTesting.Gameplay.Manager;
using VonderGames.AiTesting.Gameplay.Progression;
using VonderGames.AiTesting.Gameplay.Session;

namespace VonderGames.AiTesting.UI
{
    public sealed class UpgradeSelectionPanel : IDisposable
    {
        private readonly RectTransform _upgradePanel;
        private readonly TMP_Text _upgradeHeading;
        private readonly UnityEngine.UI.Button[] _cards;
        private readonly TMP_Text[] _cardLabels;
        private readonly TMP_Text[] _cardTitleLabels;
        private readonly TMP_Text[] _cardRankLabels;
        private readonly TMP_Text[] _cardAccents;
        private readonly UnityEngine.UI.Outline[] _innerBorders;
        private readonly UIUpgradeCard[] _cardViews;
        private readonly UnityEngine.UI.Image[] _confetti;
        private readonly RunSession _session;
        private readonly ProgressionController _progression;
        private readonly CooldownTracker _cooldowns;
        private readonly CombatDefinition _combat;
        private readonly SaveService _saves;
        private readonly CameraFeedback _camera;
        private readonly InputManager _input;
        private readonly Action<UpgradeOffer> _pulseSelection;
        public bool AreChoicesRevealed => _areChoicesRevealed;
        private readonly Vector2[] _confettiVelocities = new Vector2[32];
        private readonly Vector2[] _cardLabelPositions = new Vector2[3];
        private float _revealElapsed;
        private bool _areChoicesRevealed;
        private GameObject _selectionEcho;
        private readonly UnityAction[] _cardActions;

        public UpgradeSelectionPanel(RectTransform upgradePanel, TMP_Text upgradeHeading,
            UnityEngine.UI.Button[] cards, TMP_Text[] cardLabels, TMP_Text[] cardTitleLabels,
            TMP_Text[] cardRankLabels, TMP_Text[] cardAccents, UnityEngine.UI.Outline[] innerBorders,
            UIUpgradeCard[] cardViews, UnityEngine.UI.Image[] confetti, RunSession session,
            ProgressionController progression, CooldownTracker cooldowns, CombatDefinition combat,
            SaveService saves, CameraFeedback camera, InputManager input, Action<UpgradeOffer> pulseSelection)
        {
            _upgradePanel = upgradePanel;
            _upgradeHeading = upgradeHeading;
            _cards = cards;
            _cardLabels = cardLabels;
            _cardTitleLabels = cardTitleLabels;
            _cardRankLabels = cardRankLabels;
            _cardAccents = cardAccents;
            _innerBorders = innerBorders;
            _cardViews = cardViews;
            _confetti = confetti;
            _session = session;
            _progression = progression;
            _cooldowns = cooldowns;
            _combat = combat;
            _saves = saves;
            _camera = camera;
            _input = input;
            _pulseSelection = pulseSelection;
            _cardActions = new UnityAction[_cards.Length];
            for (int i = 0; i < _cards.Length; i++)
            {
                int index = i;
                _cardActions[i] = () => _progression.ChooseUpgrade(index);
                _cards[i].onClick.AddListener(_cardActions[i]);
                _cardLabelPositions[i] = _cardLabels[i].rectTransform.anchoredPosition;
            }
        }
        public void Dispose()
        {
            Clear();
            for (int i = 0; i < _cards.Length; i++) _cards[i].onClick.RemoveListener(_cardActions[i]);
            _upgradePanel.DOKill();
        }
        public void ShowSelection(UpgradeOffer offer)
        {
            int index = 0;
            for (int i = 0; i < _progression.Choices.Count; i++) if (_progression.Choices[i] == offer) index = i;
            if (_selectionEcho != null)
            {
                _selectionEcho.transform.DOKill();
                _selectionEcho.GetComponent<CanvasGroup>().DOKill();
                UnityEngine.Object.Destroy(_selectionEcho);
            }
            _selectionEcho = UnityEngine.Object.Instantiate(_cards[index].gameObject, _upgradePanel.parent);
            var rect = (RectTransform)_selectionEcho.transform;
            rect.position = _cards[index].transform.position;
            rect.localScale = _cards[index].transform.localScale;
            _selectionEcho.GetComponent<UnityEngine.UI.Button>().interactable = false;
            var group = _selectionEcho.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            rect.DOScale(rect.localScale * 1.08f, 0.15f).SetUpdate(true);
            group.DOFade(0, 0.15f).SetUpdate(true).OnComplete(() =>
            {
                if (group != null) UnityEngine.Object.Destroy(group.gameObject);
            });
            _pulseSelection(offer);
        }

        public void BeginReveal()
        {
            _areChoicesRevealed = false;
            _revealElapsed = 0;
            _upgradeHeading.text = $"Choose your next power  /  LEVEL {_progression.Progress.Level}";
            for (int i = 0; i < _cards.Length; i++)
            {
                _cards[i].interactable = false;
                _cards[i].transform.DOKill();
                _cards[i].transform.localScale = Vector3.one;
                _cardViews[i].Roll(i);
                _cards[i].GetComponent<UnityEngine.UI.Outline>().DOKill();
                _cards[i].GetComponent<UnityEngine.UI.Outline>().effectColor = new Color(0.4f, 0.45f, 0.5f);
                _cardAccents[i].text = "";
                _innerBorders[i].gameObject.SetActive(false);
                _cardTitleLabels[i].text = "A new possibility";
                _cardLabels[i].text = "◇   ✦   △";
                _cardRankLabels[i].text = $"{_progression.Progress.PendingChoices} choice(s) waiting";
            }
            EventSystem.current.SetSelectedGameObject(null);
            UIAnimation.PopPanel(_upgradePanel);
        }

        private void AnimateReels()
        {
            float phase = 18 * Mathf.Sqrt(_revealElapsed / _combat.RevealDuration);
            int symbol = Mathf.FloorToInt(phase);
            string[] symbols = { "◇   △   ✦", "✦   ◇   △", "△   ✦   ◇" };
            for (int i = 0; i < _cards.Length; i++)
            {
                _cardLabels[i].text = symbols[(symbol + i) % symbols.Length];
                _cardLabels[i].rectTransform.anchoredPosition = _cardLabelPositions[i] + Vector2.up * ((phase % 1 - 0.5f) * 24);
            }
        }

        private void RevealChoices(bool skipped)
        {
            _areChoicesRevealed = true;
            UpgradeRarity highest = UpgradeRarity.Common;
            for (int i = 0; i < _cards.Length; i++)
            {
                UpgradeOffer offer = _progression.Choices[i];
                _cardLabels[i].rectTransform.anchoredPosition = _cardLabelPositions[i];
                if ((int)offer.Rarity > (int)highest) highest = offer.Rarity;
                Color color = RarityColor(offer.Rarity);
                _cards[i].interactable = true;
                _cardTitleLabels[i].text = offer.DisplayName;
                _cardLabels[i].text = offer.Description + (offer.IsSkill ? $"\n{_cooldowns.GetCooldownDuration(offer.Kind == UpgradeKind.Fire ? SpellKind.Fire : offer.Kind == UpgradeKind.Water ? SpellKind.Water : SpellKind.Thunder):0.##}s cooldown" : "");
                _cardRankLabels[i].text = offer.IsSkill ? $"RANK {offer.ResultingRank}  /  {(offer.ResultingRank == 1 ? "NEW SPELL" : "UPGRADED SPELL")}" : "PERMANENT RUN BONUS";
                _cardRankLabels[i].color = new Color(0.48f, 0.62f, 0.7f);
                _cardViews[i].Reveal(offer, skipped, _saves.Data.ReduceFlash);
            }
            if (!skipped && highest >= UpgradeRarity.Epic) EmitConfetti(highest);
            if (!skipped && highest == UpgradeRarity.Legendary) _camera.Shake(0.12f);
            UIAnimation.SetNavigation(_cards, _cards.Length, true);
            if (!skipped) UIAnimation.Focus(_cards[0]);
        }

        private void EmitConfetti(UpgradeRarity highest)
        {
            int count = highest == UpgradeRarity.Legendary ? 32 : 12;
            for (int i = 0; i < _confetti.Length; i++)
            {
                _confetti[i].gameObject.SetActive(i < count);
                if (i >= count) continue;
                _confetti[i].color = RarityColor(highest);
                if (_saves.Data.ReduceFlash)
                {
                    Color soft = _confetti[i].color;
                    soft.a = 0.45f;
                    _confetti[i].color = soft;
                }
                float side = i % 2 == 0 ? -1 : 1;
                _confetti[i].rectTransform.anchoredPosition = new Vector2(side * (_upgradePanel.rect.width * 0.5f + 12), (i % 8 - 3.5f) * 38);
                _confettiVelocities[i] = new Vector2(side * (30 + i % 3 * 20), 110 + i % 4 * 45);
            }
        }

        private static Color RarityColor(UpgradeRarity rarity)
        {
            switch (rarity)
            {
                case UpgradeRarity.Rare: return new Color(0.3f, 0.65f, 1);
                case UpgradeRarity.Epic: return new Color(0.75f, 0.4f, 1);
                case UpgradeRarity.Legendary: return new Color(1, 0.8f, 0.25f);
                default: return new Color(0.75f, 0.8f, 0.85f);
            }
        }
        public bool Tick()
        {
            float delta = Time.unscaledDeltaTime;
            for (int i = 0; i < _confetti.Length; i++)
            {
                if (!_confetti[i].gameObject.activeSelf) continue;
                _confettiVelocities[i] += Vector2.down * delta * 150;
                _confetti[i].rectTransform.anchoredPosition += _confettiVelocities[i] * delta;
                Color color = _confetti[i].color;
                color.a = Mathf.Max(0, color.a - delta);
                _confetti[i].color = color;
                if (color.a <= 0) _confetti[i].gameObject.SetActive(false);
            }

            if (_session.State != RunState.UpgradeSelection || _areChoicesRevealed) return false;
            _revealElapsed += delta;
            bool skip = _input.CanSelectSpell && (_input.Actions.UI.Click.WasPressedThisFrame() || _input.Actions.UI.Submit.WasPressedThisFrame() ||
                _input.Actions.UI.Choice1.WasPressedThisFrame() || _input.Actions.UI.Choice2.WasPressedThisFrame() || _input.Actions.UI.Choice3.WasPressedThisFrame());
            if (skip || _revealElapsed >= _combat.RevealDuration) RevealChoices(skip);
            else AnimateReels();
            if (skip) _input.SuppressUntilRelease();
            return skip;
        }
        public void Clear()
        {
            _areChoicesRevealed = false;
            _revealElapsed = 0;
            if (_selectionEcho != null)
            {
                _selectionEcho.transform.DOKill();
                _selectionEcho.GetComponent<CanvasGroup>().DOKill();
                UnityEngine.Object.Destroy(_selectionEcho);
            }
            foreach (var card in _cards)
            {
                card.transform.DOKill();
                card.GetComponent<UnityEngine.UI.Outline>().DOKill();
                card.transform.localScale = Vector3.one;
            }
            foreach (var piece in _confetti) piece.gameObject.SetActive(false);
            foreach (var view in _cardViews) view.ResetPresentation();
            foreach (var accent in _cardAccents) accent.rectTransform.DOKill();
        }

        public void LeaveSelection() => _areChoicesRevealed = false;

    }
}
