using DG.Tweening;
using UnityEngine;

namespace VonderGames.AiTesting.Gameplay.Entity
{
    public sealed class PrimitiveFeedback : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        public SpriteRenderer Renderer => _renderer;

        private Color _restColor;
        private Vector3 _restScale;

        private void Awake()
        {
            _restColor = _renderer.color;
            _restScale = _renderer.transform.localScale;
        }

        private void OnDisable()
        {
            _renderer.DOKill();
            _renderer.transform.DOKill();
            _renderer.color = _restColor;
            _renderer.transform.localScale = _restScale;
        }

        public void SetColor(Color color)
        {
            _restColor = color;
            _renderer.color = color;
        }

        public void Flash(bool reduceFlash)
        {
            _renderer.DOKill();
            _renderer.color = reduceFlash ? Color.Lerp(_restColor, Color.white, 0.2f) : Color.white;
            _renderer.DOColor(_restColor, 0.12f);
        }

        public void Pop()
        {
            _renderer.transform.DOKill();
            _renderer.transform.localScale = _restScale * 1.2f;
            _renderer.transform.DOScale(_restScale, 0.18f).SetEase(Ease.OutBack);
        }
    }
}
