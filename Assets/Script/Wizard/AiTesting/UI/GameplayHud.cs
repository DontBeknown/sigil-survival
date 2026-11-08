using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Combat;
using VonderGames.AiTesting.Gameplay.Entity;
using VonderGames.AiTesting.Gameplay.Progression;
using VonderGames.AiTesting.Gameplay.Session;
using VonderGames.AiTesting.Gameplay.Spawning;

namespace VonderGames.AiTesting.UI
{
    public sealed class GameplayHud
    {
        private readonly UnityEngine.UI.Image _healthFill;
        private readonly TMP_Text _healthLabel;
        private readonly TMP_Text _timerLabel;
        private readonly TMP_Text _killsLabel;
        private readonly UnityEngine.UI.Image _experienceFill;
        private readonly TMP_Text _experienceLabel;
        private readonly TMP_Text[] _spellLabels;
        private readonly UnityEngine.UI.Image[] _spellFills;
        private readonly GameObject _bossRoot;
        private readonly UnityEngine.UI.Image _bossFill;
        private readonly TMP_Text _bossLabel;
        private readonly TMP_Text _comboLabel;
        private readonly TMP_Text _cue;
        private readonly UnityEngine.UI.Button[] _spellButtons;
        private readonly TMP_Text _dashLabel;
        private readonly UnityEngine.UI.Image _dashFill;
        private readonly TMP_Text _resetCue;
        private readonly TMP_Text _statSummary;
        private readonly RectTransform _experienceStreak;
        private readonly RunSession _session;
        private readonly ProgressionController _progression;
        private readonly EnemySpawner _spawner;
        private readonly SpellCaster _spells;
        private readonly CooldownTracker _cooldowns;
        private readonly PlayerController _player;
        private readonly CombatDefinition _combat;
        private readonly SaveService _saves;
        private readonly CameraFeedback _camera;
        private float _cueRemaining;
        private Vector2 _cueRestPosition;
        private readonly float[] _previousCooldowns = new float[4];
        private float _previousDashCooldown;
        private float _resetRemaining;
        private float _pickupTotal;
        private float _pickupRemaining;
        private Vector2 _streakStart;

        public GameplayHud(UnityEngine.UI.Image healthFill, TMP_Text healthLabel, TMP_Text timerLabel,
            TMP_Text killsLabel, UnityEngine.UI.Image experienceFill, TMP_Text experienceLabel,
            TMP_Text[] spellLabels, UnityEngine.UI.Image[] spellFills, GameObject bossRoot,
            UnityEngine.UI.Image bossFill, TMP_Text bossLabel, TMP_Text comboLabel, TMP_Text cue,
            UnityEngine.UI.Button[] spellButtons, TMP_Text dashLabel, UnityEngine.UI.Image dashFill,
            TMP_Text resetCue, TMP_Text statSummary, RectTransform experienceStreak, RunSession session,
            ProgressionController progression, EnemySpawner spawner, SpellCaster spells,
            CooldownTracker cooldowns, PlayerController player, CombatDefinition combat,
            SaveService saves, CameraFeedback camera)
        {
            _healthFill = healthFill;
            _healthLabel = healthLabel;
            _timerLabel = timerLabel;
            _killsLabel = killsLabel;
            _experienceFill = experienceFill;
            _experienceLabel = experienceLabel;
            _spellLabels = spellLabels;
            _spellFills = spellFills;
            _bossRoot = bossRoot;
            _bossFill = bossFill;
            _bossLabel = bossLabel;
            _comboLabel = comboLabel;
            _cue = cue;
            _spellButtons = spellButtons;
            _dashLabel = dashLabel;
            _dashFill = dashFill;
            _resetCue = resetCue;
            _statSummary = statSummary;
            _experienceStreak = experienceStreak;
            _session = session;
            _progression = progression;
            _spawner = spawner;
            _spells = spells;
            _cooldowns = cooldowns;
            _player = player;
            _combat = combat;
            _saves = saves;
            _camera = camera;
            _cueRestPosition = _cue.rectTransform.anchoredPosition;
        }
        public void ShowCue(string message, float duration = 1.5f)
        {
            _cue.rectTransform.DOKill();
            _cue.rectTransform.anchoredPosition = _cueRestPosition;
            _cue.color = Color.white;
            _cue.text = message;
            _cueRemaining = string.IsNullOrEmpty(message) ? 0 : duration;
        }

        public void ShowDrawingFailure(string reason, bool isDeflect)
        {
            string failure = reason == "CANCELLED" || reason == "TIME EXPIRED" || reason == "INTERRUPTED" ? reason : "TRACE FAILED";
            ShowCue(isDeflect && failure == "CANCELLED" ? "DEFLECT CANCELLED" : $"{(isDeflect ? "DEFLECT FAILED" : "CAST FAILED")} / {failure}", 1.5f);
            _cue.color = _saves.Data.ReduceFlash ? new Color(0.85f, 0.65f, 0.55f) : new Color(1, 0.55f, 0.4f);
            _cue.rectTransform.DOPunchAnchorPos(Vector2.right * 7, 0.2f, 3).SetUpdate(true);
        }

        public void OnHealthChangedHandler(float current, float maximum)
        {
            _healthFill.fillAmount = current / maximum;
            _healthLabel.text = $"HEALTH  {Mathf.CeilToInt(current)} / {maximum:0}";
        }

        public void OnBossHealthChangedHandler(float current, float maximum)
        {
            _bossRoot.SetActive(maximum > 0);
            _bossFill.fillAmount = maximum > 0 ? current / maximum : 0;
            _bossLabel.text = $"THE WARDEN  {Mathf.CeilToInt(current)} / {maximum:0}";
        }

        public void OnHudChangedHandler()
        {
            _statSummary.text = $"DMG +{(_progression.Progress.DamageMultiplier - 1) * 100:0.#}%  AREA +{(_progression.Progress.AreaMultiplier - 1) * 100:0.#}%  XP +{(_progression.Progress.ExperienceMultiplier - 1) * 100:0.#}%\n"
                + $"LUCK {_progression.Progress.Luck:0.#}  CRIT {_progression.Progress.CriticalChance * 100:0.#}%  CDR {_progression.Progress.CooldownReduction:0.#}%  SPEED +{(_progression.Progress.MovementMultiplier - 1) * 100:0.#}%";

            _timerLabel.text = UIAnimation.FormatTime(_session.Elapsed) + (_spawner.HasBossSpawned ? "  •  BOSS" : " / " + UIAnimation.FormatTime(_spawner.BossTime));
            _killsLabel.text = $"{_session.Kills}  DEFEATED";
            
            _experienceLabel.text = $"LEVEL {_progression.Progress.Level}   •   {_progression.Progress.Experience} / {_progression.Progress.RequiredExperience} XP";
            string[] keys = { "1", "2", "3", "RMB / 4" };
            string[] names = { "FLAME LANCE", "FROST PULSE", "CHAIN SPARK", "DEFLECT" };
            bool hasComboSpells = _progression.Progress.IsUnlocked(SpellKind.Water) && _progression.Progress.IsUnlocked(SpellKind.Thunder);
            _comboLabel.text = hasComboSpells ? (_spells.ComboRemaining > 0 ? $"FOLLOW WITH THUNDER  {_spells.ComboRemaining:0.0}s" : "WATER → THUNDER = SHATTER STORM") : "";
            for (int i = 0; i < 4; i++)
            {
                bool unlocked = _progression.Progress.IsUnlocked((SpellKind)(i + 1));
                float remaining = _cooldowns.GetCooldown((SpellKind)(i + 1));
                _spellLabels[i].text = unlocked ? $"{keys[i]}\n{names[i]}\n{(remaining > 0.05f ? remaining.ToString("0.0") : "READY")}" : $"{keys[i]}\n{names[i]}\nLOCKED";
                _spellLabels[i].color = unlocked ? Color.white : new Color(0.55f, 0.6f, 0.7f);
                _spellButtons[i].interactable = unlocked && remaining <= 0 && _session.State == RunState.Playing;
                if (_previousCooldowns[i] > 0 && remaining <= 0)
                {
                    _spellFills[i].rectTransform.DOKill();
                    _spellFills[i].rectTransform.localScale = Vector3.one * 1.15f;
                    _spellFills[i].rectTransform.DOScale(1, 0.2f).SetUpdate(true);
                }
                _previousCooldowns[i] = remaining;
                _spellFills[i].fillAmount = unlocked ? remaining / _cooldowns.GetCooldownDuration((SpellKind)(i + 1)) : 1;
            }
        }

        public void ShowReset()
        {
            Array.Clear(_previousCooldowns, 0, _previousCooldowns.Length);
            _previousDashCooldown = 0;
            _resetCue.text = "COOLDOWNS RESET";
            _resetRemaining = 0.6f;
            foreach (var fill in _spellFills) UIAnimation.Pulse(fill.rectTransform, 1.12f);
            UIAnimation.Pulse(_dashFill.rectTransform, 1.12f);
        }

        public void NudgeDash()
        {
            _dashLabel.text = $"SPACE  {_player.DashCooldown:0.0}s";
            _dashLabel.rectTransform.DOKill();
            _dashLabel.rectTransform.DOPunchAnchorPos(Vector2.right * 4, 0.15f, 3).SetUpdate(true);
        }

        public void ShowExperiencePickup(Vector2 position, float amount)
        {
            _pickupTotal += amount;
            _pickupRemaining = 0.5f;
            Camera camera = _experienceStreak.GetComponentInParent<Canvas>().worldCamera;
            Vector2 screen = _camera.WorldToScreen(position);
            RectTransform parent = (RectTransform)_experienceStreak.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, camera, out _streakStart);
            _experienceStreak.gameObject.SetActive(true);
        }
        public void Tick()
        {
            float delta = Time.unscaledDeltaTime;
            _cueRemaining = Mathf.Max(0, _cueRemaining - delta);
            _cue.alpha = Mathf.Clamp01(_cueRemaining * 2);
            _resetRemaining = Mathf.Max(0, _resetRemaining - delta);
            _resetCue.alpha = Mathf.Clamp01(_resetRemaining * 4);
            float dash = _player.DashCooldown;
            _dashLabel.text = dash > 0 ? $"SPACE  {dash:0.0}s" : "SPACE  DASH READY";
            _dashFill.fillAmount = dash / _combat.DashCooldown;
            if (_previousDashCooldown > 0 && dash <= 0) UIAnimation.Pulse(_dashFill.rectTransform, 1.12f);
            _previousDashCooldown = dash;
            float target = _progression.Progress.Experience / _progression.Progress.RequiredExperience;
            _experienceFill.fillAmount = Mathf.MoveTowards(_experienceFill.fillAmount, target, delta * 4);
            _experienceLabel.text = $"LEVEL {_progression.Progress.Level}   •   {_progression.Progress.Experience:0.#} / {_progression.Progress.RequiredExperience} XP";
            if (_pickupRemaining > 0)
            {
                _pickupRemaining = Mathf.Max(0, _pickupRemaining - delta);
                _experienceLabel.text += $"  +{_pickupTotal:0.#}";
                Vector2 destination = _experienceStreak.parent.InverseTransformPoint(_experienceFill.transform.position);
                _experienceStreak.anchoredPosition = Vector2.Lerp(_streakStart, destination, 1 - _pickupRemaining / 0.5f);
                if (_pickupRemaining == 0) { _pickupTotal = 0; _experienceStreak.gameObject.SetActive(false); }
            }
        }
        public void ResetRunPresentation()
        {
            Clear();
            Array.Clear(_previousCooldowns, 0, _previousCooldowns.Length);
            _previousDashCooldown = 0;
            _dashLabel.rectTransform.anchoredPosition = Vector2.zero;
        }
        public void ShowSelectionPulse(UpgradeOffer offer)
        {
            UIAnimation.Pulse(offer.IsSkill ? _spellFills[offer.Kind == UpgradeKind.Fire ? 0 : offer.Kind == UpgradeKind.Water ? 1 : 2].rectTransform : _statSummary.rectTransform, 1.1f);
        }
        public void Clear()
        {
            _cue.rectTransform.DOKill();
            _cue.rectTransform.anchoredPosition = _cueRestPosition;
            _resetRemaining = _pickupRemaining = _pickupTotal = 0;
            _resetCue.alpha = 0;
            _experienceStreak.gameObject.SetActive(false);
            foreach (var fill in _spellFills) { fill.rectTransform.DOKill(); fill.rectTransform.localScale = Vector3.one; }
            _dashFill.rectTransform.DOKill();
            _dashFill.rectTransform.localScale = Vector3.one;
            _dashLabel.rectTransform.DOKill();
            _statSummary.rectTransform.DOKill();
            _statSummary.rectTransform.localScale = Vector3.one;
        }

    }
}
