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
        private Quaternion _restRotation;
        private bool _isInitialized;

        private void Awake() => InitializeVisual();

        public void InitializeVisual()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            _restRotation = _renderer.transform.localRotation;
            _restColor = _renderer.color;
            _restScale = _renderer.transform.localScale;
        }

        private void OnDisable()
        {
            ResetVisual();
        }

        public void ResetVisual()
        {
            InitializeVisual();
            _renderer.DOKill();
            _renderer.transform.DOKill();
            _renderer.color = _restColor;
            _renderer.transform.localRotation = _restRotation;
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
            _renderer.DOColor(_restColor, 0.12f).SetUpdate(true);
        }

        public void Pop()
        {
            _renderer.transform.DOKill();
            _renderer.transform.localScale = _restScale * 1.2f;
            _renderer.transform.DOScale(_restScale, 0.18f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        public void Stretch(Vector2 direction)
        {
            _renderer.transform.DOKill();
            Vector3 scale = Mathf.Abs(direction.x) >= Mathf.Abs(direction.y) ? new Vector3(1.4f, 0.75f, 1) : new Vector3(0.75f, 1.4f, 1);
            _renderer.transform.localScale = Vector3.Scale(_restScale, scale);
            _renderer.transform.DOScale(_restScale, 0.15f).SetEase(Ease.OutCubic).SetUpdate(true);
        }
    }
}
