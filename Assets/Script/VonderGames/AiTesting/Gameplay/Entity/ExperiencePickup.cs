using UnityEngine;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Instance;

namespace VonderGames.AiTesting.Gameplay.Entity
{
    public sealed class ExperiencePickup : MonoBehaviour
    {
        [SerializeField] private PrimitiveFeedback _feedback;

        private GameplayInstance _game;
        private int _amount;

        private void Update()
        {
            if (_game.State != RunState.Playing)
                return;

            Vector2 offset = _game.Player.transform.position - transform.position;
            if (offset.sqrMagnitude < 9)
                transform.position = Vector2.MoveTowards(transform.position, _game.Player.transform.position, Time.deltaTime * 12);

            if (offset.sqrMagnitude < 0.65f)
            {
                gameObject.SetActive(false);
                _game.AddExperience(_amount);
                Destroy(gameObject);
            }
        }

        public void Initialize(GameplayInstance game, int amount)
        {
            _game = game;
            _amount = amount;
            _feedback.Pop();
        }
    }
}
