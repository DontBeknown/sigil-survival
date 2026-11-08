using UnityEngine;

namespace VonderGames.AiTesting.UI
{
    public sealed class SafeAreaLayout
    {
        private readonly RectTransform _safeArea;
        private readonly RectTransform _menu;
        private readonly RectTransform _upgradePanel;
        private readonly RectTransform _spellSlots;
        private readonly RectTransform _dashRoot;
        private readonly Canvas _canvas;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreen;

        public SafeAreaLayout(RectTransform safeArea, RectTransform menu, RectTransform upgradePanel, RectTransform spellSlots, RectTransform dashRoot, Canvas canvas)
        {
            _safeArea = safeArea;
            _menu = menu;
            _upgradePanel = upgradePanel;
            _spellSlots = spellSlots;
            _dashRoot = dashRoot;
            _canvas = canvas;

        }
        public void Apply()
        {
            _lastSafeArea = Screen.safeArea;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);
            _safeArea.anchorMin = _lastSafeArea.min / new Vector2(Screen.width, Screen.height);
            _safeArea.anchorMax = _lastSafeArea.max / new Vector2(Screen.width, Screen.height);
            float width = _lastSafeArea.width / _canvas.scaleFactor;
            _menu.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(640, width - 64));
            _upgradePanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(1140, width - 64));
            _upgradePanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Min(760, _lastSafeArea.height / _canvas.scaleFactor - 64));
            float slotsWidth = Mathf.Min(800, width - 260);
            _spellSlots.sizeDelta = new Vector2(slotsWidth, 104);
            _spellSlots.anchoredPosition = new Vector2(-110, 32);
            _dashRoot.anchoredPosition = new Vector2(slotsWidth * 0.5f, 84);
        }
        public void Tick()
        {
            if (_lastSafeArea != Screen.safeArea || _lastScreen != new Vector2Int(Screen.width, Screen.height)) Apply();
        }

    }
}
