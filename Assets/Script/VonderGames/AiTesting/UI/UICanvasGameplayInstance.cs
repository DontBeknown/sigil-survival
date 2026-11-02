using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Manager;
using VonderGames.AiTesting.Gameplay.Instance;

namespace VonderGames.AiTesting.UI
{
    public sealed class UICanvasGameplayInstance : MonoBehaviour
    {
        [SerializeField] private RectTransform _safeArea;
        [SerializeField] private UnityEngine.UI.Image _healthFill;
        [SerializeField] private TMP_Text _healthLabel;
        [SerializeField] private TMP_Text _timerLabel;
        [SerializeField] private TMP_Text _killsLabel;
        [SerializeField] private UnityEngine.UI.Image _experienceFill;
        [SerializeField] private TMP_Text _experienceLabel;
        [SerializeField] private TMP_Text[] _spellLabels;
        [SerializeField] private UnityEngine.UI.Image[] _spellFills;
        [SerializeField] private GameObject _bossRoot;
        [SerializeField] private UnityEngine.UI.Image _bossFill;
        [SerializeField] private TMP_Text _bossLabel;
        [SerializeField] private GameObject _overlay;
        [SerializeField] private RectTransform _menu;
        [SerializeField] private TMP_Text _heading;
        [SerializeField] private TMP_Text _body;
        [SerializeField] private UnityEngine.UI.Button[] _menuButtons;
        [SerializeField] private TMP_Text[] _menuButtonLabels;
        [SerializeField] private RectTransform _upgradePanel;
        [SerializeField] private TMP_Text _upgradeHeading;
        [SerializeField] private UnityEngine.UI.Button[] _cards;
        [SerializeField] private TMP_Text[] _cardLabels;
        [SerializeField] private TMP_Text[] _cardTitleLabels;
        [SerializeField] private TMP_Text[] _cardRankLabels;
        [SerializeField] private TMP_Text _notice;

        private GameplayInstance _game;
        private InputManager _input;
        private bool _isSubpanel;
        private readonly Action[] _menuActions = new Action[4];
        private Rect _lastSafeArea;
        private Vector2Int _lastScreen;

        public void Initialize(GameplayInstance game, InputManager input)
        {
            _game = game;
            _input = input;
            _game.OnHudChanged += OnHudChangedHandler;
            _game.OnStateChanged += OnStateChangedHandler;
            _game.OnBossHealthChanged += OnBossHealthChangedHandler;
            _game.Player.OnHealthChanged += OnHealthChangedHandler;
            for (int i = 0; i < _menuButtons.Length; i++)
            {
                int index = i;
                _menuButtons[i].onClick.AddListener(() => _menuActions[index]?.Invoke());
            }
            for (int i = 0; i < _cards.Length; i++)
            {
                int index = i;
                _cards[i].onClick.AddListener(() => _game.ChooseUpgrade(index));
            }
            ApplySafeArea();
        }

        private void Update()
        {
            if (_lastSafeArea != Screen.safeArea || _lastScreen != new Vector2Int(Screen.width, Screen.height))
                ApplySafeArea();

            if (!_overlay.activeSelf)
                return;

            if (_input.Actions.UI.Next.WasPressedThisFrame())
            {
                var current = EventSystem.current.currentSelectedGameObject;
                var selectable = current != null ? current.GetComponent<Selectable>() : null;
                var next = selectable != null ? selectable.FindSelectableOnDown() ?? selectable.FindSelectableOnRight() : null;
                if (next != null)
                    EventSystem.current.SetSelectedGameObject(next.gameObject);
            }
            if (_game.State == RunState.UpgradeSelection)
            {
                if (_input.Actions.UI.Choice1.WasPressedThisFrame()) _game.ChooseUpgrade(0);
                else if (_input.Actions.UI.Choice2.WasPressedThisFrame()) _game.ChooseUpgrade(1);
                else if (_input.Actions.UI.Choice3.WasPressedThisFrame()) _game.ChooseUpgrade(2);
            }
        }

        private void OnDestroy()
        {
            if (_game == null)
                return;

            _game.OnHudChanged -= OnHudChangedHandler;
            _game.OnStateChanged -= OnStateChangedHandler;
            _game.OnBossHealthChanged -= OnBossHealthChangedHandler;
            _game.Player.OnHealthChanged -= OnHealthChangedHandler;
            _menu.DOKill();
            _upgradePanel.DOKill();
        }

        public bool CloseSubpanel()
        {
            if (!_isSubpanel)
                return false;

            OnStateChangedHandler(_game.State);
            return true;
        }

        private void OnHealthChangedHandler(float current, float maximum)
        {
            _healthFill.fillAmount = current / maximum;
            _healthLabel.text = $"HEALTH  {Mathf.CeilToInt(current)} / {maximum:0}";
        }

        private void OnBossHealthChangedHandler(float current, float maximum)
        {
            _bossRoot.SetActive(maximum > 0);
            _bossFill.fillAmount = maximum > 0 ? current / maximum : 0;
            _bossLabel.text = $"THE WARDEN  {Mathf.CeilToInt(current)} / {maximum:0}";
        }

        private void OnHudChangedHandler()
        {
            _timerLabel.text = FormatTime(_game.Elapsed) + (_game.HasBossSpawned ? "  •  BOSS" : " / 10:00");
            _killsLabel.text = $"{_game.Kills}  DEFEATED";
            _experienceFill.fillAmount = (float)_game.Progress.Experience / _game.Progress.RequiredExperience;
            _experienceLabel.text = $"LEVEL {_game.Progress.Level}   •   {_game.Progress.Experience} / {_game.Progress.RequiredExperience} XP";
            string[] keys = { "LMB", "RMB", "SPACE" };
            string[] names = { "BOLT", "FIRE", "NOVA" };
            for (int i = 0; i < 3; i++)
            {
                bool unlocked = _game.Progress.IsUnlocked((SpellKind)i);
                float remaining = _game.GetCooldown((SpellKind)i);
                _spellLabels[i].text = unlocked ? $"{keys[i]}\n{names[i]}\n{(remaining > 0.05f ? remaining.ToString("0.0") : "READY")}" : $"{keys[i]}\n{names[i]}\nLOCKED";
                _spellLabels[i].color = unlocked ? Color.white : new Color(0.55f, 0.6f, 0.7f);
                _spellFills[i].fillAmount = unlocked ? remaining / (_game.Spells[i].Cooldown * _game.Progress.CooldownMultiplier) : 1;
            }
        }

        private void OnStateChangedHandler(RunState state)
        {
            _isSubpanel = false;
            bool isPlaying = state == RunState.Playing;
            bool isUpgrade = state == RunState.UpgradeSelection;
            _overlay.SetActive(!isPlaying);
            _menu.gameObject.SetActive(!isPlaying && !isUpgrade);
            _upgradePanel.gameObject.SetActive(isUpgrade);
            _notice.text = _game.Saves.LastError ?? "";
            if (isPlaying)
            {
                EventSystem.current.SetSelectedGameObject(null);
                return;
            }
            if (isUpgrade)
            {
                _upgradeHeading.text = $"LEVEL {_game.Progress.Level}  •  CHOOSE AN UPGRADE";
                for (int i = 0; i < _cards.Length; i++)
                {
                    bool isVisible = i < _game.Choices.Count;
                    _cards[i].gameObject.SetActive(isVisible);
                    if (!isVisible)
                        continue;

                    var choice = _game.Choices[i];
                    _cardTitleLabels[i].text = $"<b>{i + 1}  {choice.DisplayName}</b>";
                    _cardLabels[i].text = choice.Description;
                    _cardRankLabels[i].text = choice.Kind == UpgradeKind.Healing ? "Repeatable" : $"Rank {_game.Progress.GetRank(choice.Kind) + 1} / {choice.MaximumRank}";
                }
                SetNavigation(_cards, _game.Choices.Count, true);
                PopPanel(_upgradePanel);
                Focus(_cards[0]);
                return;
            }
            if (state == RunState.Title)
            {
                _heading.text = "SQUARE WIZARD";
                _body.text = "Move. Cast. Survive.\nSurvive ten minutes, then defeat the Warden.\n\n" +
                    $"BEST  {FormatTime(_game.Saves.Data.BestSurvivalTime)}  •  {_game.Saves.Data.BestKillCount} defeats";
                SetButtons(("Begin run", _game.StartRun), ("Controls", ShowControls), ("Settings", ShowSettings));
            }
            else if (state == RunState.Paused)
            {
                _heading.text = "PAUSED";
                _body.text = "Take a breath.\nThe arena will wait.";
                SetButtons(("Resume", _game.TogglePause), ("Settings", ShowSettings), ("Restart run", _game.StartRun), ("Return to title", _game.ReturnToTitle));
            }
            else
            {
                _heading.text = state == RunState.Victory ? "WARDEN DEFEATED" : "RUN ENDED";
                _body.text = $"Time  {FormatTime(_game.Elapsed)}\nDefeats  {_game.Kills}   •   Level  {_game.Progress.Level}\n\n" +
                    $"BEST  {FormatTime(_game.Saves.Data.BestSurvivalTime)}  •  {_game.Saves.Data.BestKillCount} defeats";
                SetButtons(("Try again", _game.StartRun), ("Return to title", _game.ReturnToTitle));
            }
            PopPanel(_menu);
        }

        private void ShowControls()
        {
            _isSubpanel = true;
            _heading.text = "CONTROLS";
            _body.text = "WASD / Arrows — Move\nMouse — Aim    LMB — Hold to cast bolt\nRMB — Fireball    Space — Frost Nova\nEscape — Pause    1 / 2 / 3 — Upgrade\n\nCollect green XP. Unlock spells on level-up.";
            SetButtons(("Back", () => OnStateChangedHandler(_game.State)));
        }

        private void ShowSettings()
        {
            _isSubpanel = true;
            _heading.text = "SETTINGS";
            _body.text = "Accessibility\nSettings are saved automatically.";
            SetButtons(($"Reduced shake: {(_game.Saves.Data.ReduceShake ? "ON" : "OFF")}", ToggleShake),
                ($"Reduced flashes: {(_game.Saves.Data.ReduceFlash ? "ON" : "OFF")}", ToggleFlash),
                ("Back", () => OnStateChangedHandler(_game.State)));
        }

        private void ToggleShake()
        {
            _game.Saves.Data.ReduceShake = !_game.Saves.Data.ReduceShake;
            _game.Saves.Save();
            _notice.text = _game.Saves.LastError ?? "";
            ShowSettings();
        }

        private void ToggleFlash()
        {
            _game.Saves.Data.ReduceFlash = !_game.Saves.Data.ReduceFlash;
            _game.Saves.Save();
            _notice.text = _game.Saves.LastError ?? "";
            ShowSettings();
        }

        private void SetButtons(params (string Label, Action Action)[] buttons)
        {
            for (int i = 0; i < _menuButtons.Length; i++)
            {
                _menuButtons[i].gameObject.SetActive(i < buttons.Length);
                _menuActions[i] = i < buttons.Length ? buttons[i].Action : null;
                if (i < buttons.Length)
                    _menuButtonLabels[i].text = buttons[i].Label;
            }
            SetNavigation(_menuButtons, buttons.Length, false);
            Focus(_menuButtons[0]);
        }

        private static void SetNavigation(UnityEngine.UI.Button[] buttons, int count, bool isHorizontal)
        {
            for (int i = 0; i < count; i++)
            {
                var previous = buttons[(i + count - 1) % count];
                var next = buttons[(i + 1) % count];
                var navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = previous, selectOnDown = next };
                if (isHorizontal)
                {
                    navigation.selectOnLeft = previous;
                    navigation.selectOnRight = next;
                }
                buttons[i].navigation = navigation;
            }
        }

        private void ApplySafeArea()
        {
            _lastSafeArea = Screen.safeArea;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);
            _safeArea.anchorMin = _lastSafeArea.min / new Vector2(Screen.width, Screen.height);
            _safeArea.anchorMax = _lastSafeArea.max / new Vector2(Screen.width, Screen.height);
            float width = Screen.width / GetComponent<Canvas>().scaleFactor;
            _menu.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(640, width - 64));
            _upgradePanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(1120, width - 64));
        }

        private static void Focus(UnityEngine.UI.Button button)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }

        private static void PopPanel(RectTransform panel)
        {
            panel.DOKill();
            panel.localScale = Vector3.one * 0.96f;
            panel.DOScale(1, 0.16f).SetUpdate(true).SetEase(Ease.OutCubic);
        }

        private static string FormatTime(float seconds) => $"{(int)seconds / 60:00}:{(int)seconds % 60:00}";
    }
}
