using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Combat;
using VonderGames.AiTesting.Gameplay.Progression;
using VonderGames.AiTesting.Gameplay.Session;

namespace VonderGames.AiTesting.UI
{
    public sealed class RunMenuPanel : IDisposable
    {
        private readonly RectTransform _menu;
        private readonly TMP_Text _heading;
        private readonly TMP_Text _body;
        private readonly UnityEngine.UI.Button[] _menuButtons;
        private readonly TMP_Text[] _menuButtonLabels;
        private readonly TMP_Text _notice;
        private readonly RunSession _session;
        private readonly ProgressionController _progression;
        private readonly SaveService _saves;
        private readonly DrawingController _drawing;
        private readonly Action _startRun;
        private readonly Action _returnToTitle;
        private bool _isSubpanel;
        private readonly Action[] _menuActions = new Action[4];
        private readonly UnityAction[] _clickActions;

        public RunMenuPanel(RectTransform menu, TMP_Text heading, TMP_Text body, UnityEngine.UI.Button[] menuButtons, TMP_Text[] menuButtonLabels, TMP_Text notice, RunSession session, ProgressionController progression, SaveService saves, DrawingController drawing, Action startRun, Action returnToTitle)
        {
            _menu = menu;
            _heading = heading;
            _body = body;
            _menuButtons = menuButtons;
            _menuButtonLabels = menuButtonLabels;
            _notice = notice;
            _session = session;
            _progression = progression;
            _saves = saves;
            _drawing = drawing;
            _startRun = startRun;
            _returnToTitle = returnToTitle;
            _clickActions = new UnityAction[_menuButtons.Length];
            for (int i = 0; i < _menuButtons.Length; i++)
            {
                int index = i;
                _clickActions[i] = () => _menuActions[index]?.Invoke();
                _menuButtons[i].onClick.AddListener(_clickActions[i]);
            }
        }
        public void Dispose()
        {
            for (int i = 0; i < _menuButtons.Length; i++) _menuButtons[i].onClick.RemoveListener(_clickActions[i]);
            _menu.DOKill();
        }
        public void Render(RunState state)
        {
            _isSubpanel = false;
            _notice.text = _saves.LastError ?? "";
            if (state == RunState.Playing || state == RunState.Drawing || state == RunState.UpgradeSelection) return;
            if (state == RunState.Title)
            {
                _heading.text = "SIGIL SURVIVAL";
                _body.text = "Move. Cast. Survive.\nSurvive five combat minutes, then defeat the Warden.\n\n" +
                    $"BEST  {UIAnimation.FormatTime(_saves.Data.BestSurvivalTime)}  •  {_saves.Data.BestKillCount} defeats";
                SetButtons(("Begin run", _startRun), ("Controls", ShowControls), ("Settings", ShowSettings));
            }
            else if (state == RunState.Paused)
            {
                _heading.text = "PAUSED";
                _body.text = "Take a breath.\nThe arena will wait.";
                SetButtons(("Resume", _drawing.TogglePause), ("Settings", ShowSettings), ("Restart run", _startRun), ("Return to title", _returnToTitle));
            }
            else
            {
                _heading.text = state == RunState.Victory ? "WARDEN DEFEATED" : "RUN ENDED";
                _body.text = $"Time  {UIAnimation.FormatTime(_session.Elapsed)}\nDefeats  {_session.Kills}   •   Level  {_progression.Progress.Level}\n\n" +
                    $"BEST  {UIAnimation.FormatTime(_saves.Data.BestSurvivalTime)}  •  {_saves.Data.BestKillCount} defeats";
                SetButtons(("Try again", _startRun), ("Return to title", _returnToTitle));
            }
            UIAnimation.PopPanel(_menu);
        }
        public bool CloseSubpanel()
        {
            if (!_isSubpanel)
                return false;

            Render(_session.State);
            return true;
        }

        private void ShowControls()
        {
            _isSubpanel = true;
            _heading.text = "CONTROLS";
            _body.text = "WASD / Arrows — Move\nSpace — Dash in movement direction\nRMB / 4 — Deflect incoming shots\nWand aims and fires automatically\n1 / 2 / 3 — Trace Fire / Water / Thunder\nEscape — Pause    1 / 2 / 3 — Upgrade\n\nHold and trace numbered points, then release.\nDrawing slows combat; WASD still moves.\nWater → Thunder triggers Shatter Storm.";
            SetButtons(("Back", () => Render(_session.State)));
        }

        private void ShowSettings()
        {
            _isSubpanel = true;
            _heading.text = "SETTINGS";
            _body.text = "Accessibility\nSettings are saved automatically.";
            SetButtons(($"Reduced shake: {(_saves.Data.ReduceShake ? "ON" : "OFF")}", ToggleShake),
                ($"Reduced flashes: {(_saves.Data.ReduceFlash ? "ON" : "OFF")}", ToggleFlash),
                ("Back", () => Render(_session.State)));
        }

        private void ToggleShake()
        {
            _saves.Data.ReduceShake = !_saves.Data.ReduceShake;
            _saves.Save();
            _notice.text = _saves.LastError ?? "";
            ShowSettings();
        }

        private void ToggleFlash()
        {
            _saves.Data.ReduceFlash = !_saves.Data.ReduceFlash;
            _saves.Save();
            _notice.text = _saves.LastError ?? "";
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
            UIAnimation.SetNavigation(_menuButtons, buttons.Length, false);
            UIAnimation.Focus(_menuButtons[0]);
        }

    }
}
