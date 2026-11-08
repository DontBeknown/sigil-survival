using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Combat;
using VonderGames.AiTesting.Gameplay.Entity;
using VonderGames.AiTesting.Gameplay.Manager;
using VonderGames.AiTesting.Gameplay.Progression;
using VonderGames.AiTesting.Gameplay.Session;
using VonderGames.AiTesting.Gameplay.Spawning;

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

        [SerializeField] private TMP_Text _comboLabel;
        [SerializeField] private TMP_Text _cue;
        [SerializeField] private UnityEngine.UI.Button[] _spellButtons;
        [SerializeField] private TMP_Text[] _cardAccents;
        [SerializeField] private UnityEngine.UI.Outline[] _innerBorders;
        [SerializeField] private UIUpgradeCard[] _cardViews;

        [SerializeField] private TMP_Text _dashLabel;
        [SerializeField] private UnityEngine.UI.Image _dashFill;
        [SerializeField] private TMP_Text _resetCue;
        [SerializeField] private TMP_Text _statSummary;
        [SerializeField] private UnityEngine.UI.Image[] _confetti;
        [SerializeField] private RectTransform _confettiRoot;
        [SerializeField] private RectTransform _experienceStreak;
        [SerializeField] private RectTransform _spellSlots;
        [SerializeField] private RectTransform _dashRoot;

        public bool AreChoicesRevealed => _upgradeSelection.AreChoicesRevealed;
        private GameplayHud _hud;
        private RunMenuPanel _runMenu;
        private UpgradeSelectionPanel _upgradeSelection;
        private SafeAreaLayout _layout;
        private RunSession _session;
        private ProgressionController _progression;
        private DrawingController _drawing;
        private DeflectController _deflect;
        private InputManager _input;
        private PlayerController _player;
        private UnityAction[] _spellActions;
        private bool _isInitialized;

        public void Initialize(RunSession session, ProgressionController progression, CooldownTracker cooldowns,
            SpellCaster spells, EnemySpawner spawner, DrawingController drawing, DeflectController deflect,
            PlayerController player, CombatDefinition combat, SaveService saves, CameraFeedback camera,
            InputManager input, Action startRun, Action returnToTitle)
        {
            _session = session; _progression = progression; _drawing = drawing; _deflect = deflect;
            _input = input; _player = player;
            _hud = new GameplayHud(_healthFill, _healthLabel, _timerLabel, _killsLabel,
                _experienceFill, _experienceLabel, _spellLabels, _spellFills, _bossRoot, _bossFill,
                _bossLabel, _comboLabel, _cue, _spellButtons, _dashLabel, _dashFill, _resetCue,
                _statSummary, _experienceStreak, session, progression, spawner, spells, cooldowns,
                player, combat, saves, camera);
            _upgradeSelection = new UpgradeSelectionPanel(_upgradePanel, _upgradeHeading, _cards,
                _cardLabels, _cardTitleLabels, _cardRankLabels, _cardAccents, _innerBorders, _cardViews,
                _confetti, session, progression, cooldowns, combat, saves, camera, input, _hud.ShowSelectionPulse);
            _runMenu = new RunMenuPanel(_menu, _heading, _body, _menuButtons, _menuButtonLabels,
                _notice, session, progression, saves, drawing, startRun, returnToTitle);
            _layout = new SafeAreaLayout(_safeArea, _menu, _upgradePanel, _spellSlots, _dashRoot, GetComponent<Canvas>());
            session.OnHudChanged += _hud.OnHudChangedHandler;
            session.OnBossHealthChanged += _hud.OnBossHealthChangedHandler;
            session.OnStateChanged += OnStateChangedHandler;
            player.OnHealthChanged += _hud.OnHealthChangedHandler;
            _spellActions = new UnityAction[_spellButtons.Length];
            for (int i = 0; i < _spellButtons.Length; i++)
            {
                SpellKind kind = (SpellKind)(i + 1);
                _spellActions[i] = () =>
                {
                    if (kind == SpellKind.Deflect) deflect.AttemptDeflect();
                    else drawing.BeginDrawing(kind);
                };
                _spellButtons[i].onClick.AddListener(_spellActions[i]);
            }
            _layout.Apply();
            _isInitialized = true;
        }
        private void Update()
        {
            if (!_isInitialized) return;
            _layout.Tick();
            _hud.Tick();
            if (_session.State == RunState.Playing && _input.CanSelectSpell &&
                (_input.Choice4.WasPressedThisFrame() || (_input.Deflect.WasPressedThisFrame() && _input.CanCast)))
                _deflect.AttemptDeflect();
            if (_upgradeSelection.Tick()) return;
            if (_session.State == RunState.Playing && _input.CanSelectSpell)
            {
                if (_input.Actions.UI.Choice1.WasPressedThisFrame()) _drawing.BeginDrawing(SpellKind.Fire);
                else if (_input.Actions.UI.Choice2.WasPressedThisFrame()) _drawing.BeginDrawing(SpellKind.Water);
                else if (_input.Actions.UI.Choice3.WasPressedThisFrame()) _drawing.BeginDrawing(SpellKind.Thunder);
            }
            if (!_overlay.activeSelf) return;
            if (_input.Actions.UI.Next.WasPressedThisFrame())
            {
                var current = EventSystem.current.currentSelectedGameObject;
                var selectable = current != null ? current.GetComponent<Selectable>() : null;
                var next = selectable != null ? selectable.FindSelectableOnDown() ?? selectable.FindSelectableOnRight() : null;
                if (next != null) EventSystem.current.SetSelectedGameObject(next.gameObject);
            }
            if (_session.State == RunState.UpgradeSelection && AreChoicesRevealed)
            {
                if (_input.Actions.UI.Choice1.WasPressedThisFrame()) _progression.ChooseUpgrade(0);
                else if (_input.Actions.UI.Choice2.WasPressedThisFrame()) _progression.ChooseUpgrade(1);
                else if (_input.Actions.UI.Choice3.WasPressedThisFrame()) _progression.ChooseUpgrade(2);
            }
        }

        private void OnDestroy()
        {
            if (!_isInitialized) return;
            _session.OnHudChanged -= _hud.OnHudChangedHandler;
            _session.OnBossHealthChanged -= _hud.OnBossHealthChangedHandler;
            _session.OnStateChanged -= OnStateChangedHandler;
            _player.OnHealthChanged -= _hud.OnHealthChangedHandler;
            for (int i = 0; i < _spellButtons.Length; i++) _spellButtons[i].onClick.RemoveListener(_spellActions[i]);
            _hud.Clear();
            _runMenu.Dispose();
            _upgradeSelection.Dispose();
        }

        public bool CloseSubpanel() => _runMenu.CloseSubpanel();
        public void ShowCue(string message, float duration = 1.5f) => _hud.ShowCue(message, duration);
        public void ShowDrawingFailure(string reason, bool isDeflect) => _hud.ShowDrawingFailure(reason, isDeflect);
        public void ShowReset() => _hud.ShowReset();
        public void NudgeDash() => _hud.NudgeDash();
        public void ShowSelection(UpgradeOffer offer) => _upgradeSelection.ShowSelection(offer);
        public void ShowExperiencePickup(Vector2 position, float amount) => _hud.ShowExperiencePickup(position, amount);
        public void ResetRunPresentation() { _hud.ResetRunPresentation(); _upgradeSelection.Clear(); }

        private void OnStateChangedHandler(RunState state)
        {
            if (state != RunState.UpgradeSelection) _upgradeSelection.LeaveSelection();
            if (state == RunState.Title || state == RunState.Victory || state == RunState.Defeat)
            {
                _hud.Clear();
                _upgradeSelection.Clear();
            }
            bool playing = state == RunState.Playing || state == RunState.Drawing;
            bool upgrading = state == RunState.UpgradeSelection;
            _overlay.SetActive(!playing);
            _menu.gameObject.SetActive(!playing && !upgrading);
            _upgradePanel.gameObject.SetActive(upgrading);
            _runMenu.Render(state);
            if (playing) EventSystem.current.SetSelectedGameObject(null);
            else if (upgrading) _upgradeSelection.BeginReveal();
        }

    }
}
