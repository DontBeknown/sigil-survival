using System;
using UnityEngine;
using VonderGames.AiTesting.Gameplay.Progression;
using VonderGames.AiTesting.Gameplay.Session;
using VonderGames.AiTesting.UI;

namespace VonderGames.AiTesting.Gameplay.Entity
{
    public sealed class ExperiencePickup : MonoBehaviour
    {
        [SerializeField] private PrimitiveFeedback _feedback;

        private RunSession _session;
        private PlayerController _player;
        private ProgressionController _progression;
        private CombatFeedback _combatFeedback;
        private UICanvasGameplayInstance _ui;
        private Action<ExperiencePickup> _return;
        private bool _isCollected;

        private int _amount;
        private float _attractionSpeed;
        private bool _isBonus;

        private void Update()
        {
            if (_isCollected || !_session.IsCombatActive)
                return;

            Vector2 offset = _player.transform.position - transform.position;
            if (offset.sqrMagnitude < 9)
            {
                _attractionSpeed = Mathf.MoveTowards(_attractionSpeed, 16, Time.deltaTime * 40);
                transform.position = Vector2.MoveTowards(transform.position, _player.transform.position, Time.deltaTime * _attractionSpeed);
            }

            if (offset.sqrMagnitude < 0.65f)
            {
                _isCollected = true;
                _combatFeedback.Burst(transform.position, new Color(0.3f, 1, 0.6f), 3, 0.2f);
                _ui.ShowExperiencePickup(transform.position, _amount * _progression.Progress.ExperienceMultiplier);
                _progression.AddExperience(_amount);
                _return(this);
            }
        }

        public void Initialize(RunSession session, PlayerController player, ProgressionController progression,
            CombatFeedback combatFeedback, UICanvasGameplayInstance ui, Action<ExperiencePickup> returnToPool,
            int amount, bool isBonus = false)
        {
            _session = session; _player = player; _progression = progression; _combatFeedback = combatFeedback;
            _ui = ui; _return = returnToPool;
            _isCollected = false;
            _attractionSpeed = 0;
            _feedback.InitializeVisual();
            _amount = amount;
            _isBonus = isBonus;
            if (_isBonus) _combatFeedback.Star(transform.position, new Color(1, 0.9f, 0.4f), 0.4f);
            _feedback.Pop();
        }
        public void ResetForPool()
        {
            _isCollected = true;
            _amount = 0;
            _attractionSpeed = 0;
            _isBonus = false;
            _feedback.ResetVisual();
        }
    }
}
