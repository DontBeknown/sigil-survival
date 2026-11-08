using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VonderGames.AiTesting.UI
{
    public static class UIAnimation
    {

        public static void Pulse(RectTransform rect, float scale)
        {
            rect.DOKill();
            rect.localScale = Vector3.one * scale;
            rect.DOScale(1, 0.15f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        public static void SetNavigation(UnityEngine.UI.Button[] buttons, int count, bool isHorizontal)
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

        public static void Focus(UnityEngine.UI.Button button)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }

        public static void PopPanel(RectTransform panel)
        {
            panel.DOKill();
            panel.localScale = Vector3.one * 0.96f;
            panel.DOScale(1, 0.16f).SetUpdate(true).SetEase(Ease.OutCubic);
        }

        public static string FormatTime(float seconds) => $"{(int)seconds / 60:00}:{(int)seconds % 60:00}";

    }
}
