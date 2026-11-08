using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VonderGames.AiTesting.UI;

namespace VonderGames.AiTesting.Editor
{
    public static class WizardUIRedesign
    {
        private static TMP_FontAsset _font;
        private static Sprite _pixel;
        private static readonly Color _ink = new Color(0.025f, 0.04f, 0.065f, 0.98f);
        private static readonly Color _muted = new Color(0.48f, 0.62f, 0.7f);
        private static readonly Color _teal = new Color(0.3f, 0.85f, 0.8f);

        public static void Apply(UICanvasGameplayInstance ui, TMP_FontAsset font)
        {
            _font = font;
            _pixel = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ScriptableObject/PrimitiveSquare.asset");
            var data = new SerializedObject(ui);
            var upgrade = (RectTransform)data.FindProperty("_upgradePanel").objectReferenceValue;
            upgrade.sizeDelta = new Vector2(1140, 760);
            upgrade.GetComponent<UnityEngine.UI.Image>().color = _ink;
            Frame("PanelFrame", upgrade, new Color(0.22f, 0.34f, 0.4f), 1, 0);
            Text("Eyebrow", upgrade, new Vector2(0.5f, 1), new Vector2(0, -29), new Vector2(1000, 24), "A R C A N E   A S C E N S I O N", 15, _teal);
            var heading = (TMP_Text)data.FindProperty("_upgradeHeading").objectReferenceValue;
            Place(heading.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -76), new Vector2(-48, 64), true);
            heading.fontSize = 34;
            heading.enableAutoSizing = true;
            heading.fontSizeMin = 22;
            heading.fontSizeMax = 34;
            heading.fontStyle = FontStyles.Bold;
            Text("Subtitle", upgrade, new Vector2(0.5f, 1), new Vector2(0, -119), new Vector2(-60, 26), "Three possibilities. One stronger wizard.", 18, _muted, true);
            Text("SelectionHint", upgrade, new Vector2(0.5f, 0), new Vector2(0, 28), new Vector2(-64, 32), "[1]  [2]  [3]  CHOOSE     /     CLICK TO SELECT     /     CLICK OR ENTER TO SKIP REVEAL", 14, _muted, true);
            var row = (RectTransform)upgrade.Find("Cards");
            row.offsetMin = new Vector2(34, 80);
            row.offsetMax = new Vector2(-34, -158);
            row.GetComponent<HorizontalLayoutGroup>().spacing = 24;
            var cards = data.FindProperty("_cards");
            var views = new UIUpgradeCard[cards.arraySize];
            for (int i = 0; i < cards.arraySize; i++)
            {
                var button = (UnityEngine.UI.Button)cards.GetArrayElementAtIndex(i).objectReferenceValue;
                views[i] = PatchCard(button, data, i);
            }
            SetArray(ui, "_cardViews", views);
            var confetti = (RectTransform)data.FindProperty("_confettiRoot").objectReferenceValue;
            confetti.SetAsLastSibling();
            var safe = (RectTransform)data.FindProperty("_safeArea").objectReferenceValue;
            var hud = safe.Find("HUD");
            var modal = (GameObject)data.FindProperty("_overlay").objectReferenceValue;
            modal.GetComponent<UnityEngine.UI.Image>().color = new Color(0.008f, 0.018f, 0.035f, 0.9f);
            foreach (string name in new[] { "_healthFill", "_experienceFill", "_bossFill", "_dashFill" })
            {
                var fill = (UnityEngine.UI.Image)data.FindProperty(name).objectReferenceValue;
                fill.sprite = _pixel;
                fill.type = UnityEngine.UI.Image.Type.Filled;
                fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
                fill.color = name == "_healthFill" ? new Color(0.93f, 0.37f, 0.4f) : name == "_bossFill" ? new Color(0.85f, 0.3f, 0.45f) : _teal;
            }
            var fills = data.FindProperty("_spellFills");
            var labels = data.FindProperty("_spellLabels");
            var slots = data.FindProperty("_spellButtons");
            for (int i = 0; i < slots.arraySize; i++)
            {
                var button = (UnityEngine.UI.Button)slots.GetArrayElementAtIndex(i).objectReferenceValue;
                button.GetComponent<UnityEngine.UI.Image>().color = new Color(0.035f, 0.065f, 0.095f, 0.97f);
                Color element = i == 0 ? new Color(1, 0.48f, 0.27f) : i == 1 ? new Color(0.3f, 0.75f, 1) : i == 2 ? new Color(0.75f, 0.6f, 1) : _teal;
                var top = Image("ElementAccent", button.transform, element);
                Place(top.rectTransform, new Vector2(0.5f, 1), Vector2.zero, new Vector2(-8, 3), true);
                top.rectTransform.pivot = new Vector2(0.5f, 1);
                Frame("SlotFrame", button.transform, new Color(0.18f, 0.3f, 0.37f), 1, 0);
                var label = (TMP_Text)labels.GetArrayElementAtIndex(i).objectReferenceValue;
                label.fontStyle = FontStyles.Bold;
                label.fontSizeMin = 12;
                label.fontSizeMax = 17;
                var fill = (UnityEngine.UI.Image)fills.GetArrayElementAtIndex(i).objectReferenceValue;
                fill.sprite = _pixel;
                fill.color = new Color(element.r, element.g, element.b, 0.18f);
                fill.type = UnityEngine.UI.Image.Type.Filled;
                fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            }
            var summary = (TMP_Text)data.FindProperty("_statSummary").objectReferenceValue;
            summary.color = _muted;
            summary.fontSize = 14;
            summary.enableAutoSizing = true;
            summary.fontSizeMin = 11;
            summary.fontSizeMax = 14;
            foreach (string name in new[] { "_timerLabel", "_killsLabel", "_experienceLabel", "_healthLabel" })
            {
                var text = (TMP_Text)data.FindProperty(name).objectReferenceValue;
                text.fontStyle = FontStyles.Bold;
                text.fontSize = name == "_timerLabel" ? 26 : 18;
                text.color = new Color(0.8f, 0.9f, 0.92f);
            }
            var menu = (RectTransform)data.FindProperty("_menu").objectReferenceValue;
            menu.GetComponent<UnityEngine.UI.Image>().color = _ink;
            Frame("MenuFrame", menu, new Color(0.22f, 0.34f, 0.4f), 1, 0);
            var menuButtons = data.FindProperty("_menuButtons");
            for (int i = 0; i < menuButtons.arraySize; i++)
            {
                var button = (UnityEngine.UI.Button)menuButtons.GetArrayElementAtIndex(i).objectReferenceValue;
                button.GetComponent<UnityEngine.UI.Image>().color = new Color(0.06f, 0.13f, 0.17f);
                Frame("ButtonFrame", button.transform, new Color(0.2f, 0.4f, 0.45f), 1, 0);
            }
            var drawing = safe.Find("DrawingOverlay/DrawingPanel");
            drawing.GetComponent<UnityEngine.UI.Image>().color = _ink;
            Frame("DrawingFrame", drawing, new Color(0.25f, 0.52f, 0.6f), 1, 0);
            EditorUtility.SetDirty(ui);
        }

        private static UIUpgradeCard PatchCard(UnityEngine.UI.Button button, SerializedObject ui, int index)
        {
            var root = (RectTransform)button.transform;
            button.transition = Selectable.Transition.None;
            var oldOutline = button.GetComponent<UnityEngine.UI.Outline>();
            oldOutline.enabled = false;
            var oldInner = root.Find("InnerRarityBorder");
            oldInner.GetComponent<UnityEngine.UI.Outline>().enabled = false;
            oldInner.gameObject.SetActive(false);
            root.Find("RarityAccent").gameObject.SetActive(false);
            var background = button.GetComponent<UnityEngine.UI.Image>();
            background.color = new Color(0.035f, 0.055f, 0.085f);
            var view = button.GetComponent<UIUpgradeCard>() ?? button.gameObject.AddComponent<UIUpgradeCard>();
            Set(view, "_button", button);
            Set(view, "_background", background);
            var header = Image("Header", root, new Color(0.06f, 0.11f, 0.16f));
            header.rectTransform.anchorMin = new Vector2(0, 0.74f);
            header.rectTransform.anchorMax = Vector2.one;
            header.rectTransform.offsetMin = new Vector2(2, 0);
            header.rectTransform.offsetMax = new Vector2(-2, -2);
            header.transform.SetAsFirstSibling();
            Set(view, "_header", header);
            SetArray(view, "_edges", Frame("Border", root, _muted, 1.5f, 0));
            SetArray(view, "_innerEdges", Frame("InnerFrame", root, _muted, 1, 6));
            var impactEdges = Frame("RevealImpact", root, Color.clear, 2, -3);
            SetArray(view, "_impactEdges", impactEdges);
            Set(view, "_impact", (RectTransform)impactEdges[0].transform.parent);
            Set(view, "_category", Text("Category", root, new Vector2(0.5f, 0.94f), Vector2.zero, new Vector2(-68, 25), "UNSEALING", 12, _muted, true));
            Set(view, "_icon", Text("Sigil", root, new Vector2(0.5f, 0.81f), Vector2.zero, new Vector2(110, 70), "?", 44, _teal));
            Set(view, "_key", Text("KeyBadge", root, new Vector2(1, 1), new Vector2(-23, -23), new Vector2(28, 28), (index + 1).ToString(), 13, _muted));
            Set(view, "_rarity", Text("Rarity", root, new Vector2(0.5f, 0.11f), Vector2.zero, new Vector2(-36, 28), "REVEALING", 15, _muted, true));
            var title = (TMP_Text)ui.FindProperty("_cardTitleLabels").GetArrayElementAtIndex(index).objectReferenceValue;
            Place(title.rectTransform, new Vector2(0.5f, 0.65f), Vector2.zero, new Vector2(-40, 64), true);
            title.fontSizeMin = 20;
            title.fontSizeMax = 28;
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.91f, 0.95f, 0.95f);
            var description = (TMP_Text)ui.FindProperty("_cardLabels").GetArrayElementAtIndex(index).objectReferenceValue;
            description.rectTransform.anchorMin = new Vector2(0.075f, 0.2f);
            description.rectTransform.anchorMax = new Vector2(0.925f, 0.55f);
            description.rectTransform.pivot = Vector2.one * 0.5f;
            description.rectTransform.offsetMin = description.rectTransform.offsetMax = Vector2.zero;
            description.fontSizeMin = 14;
            description.fontSizeMax = 22;
            description.alignment = TextAlignmentOptions.Center;
            description.color = new Color(0.7f, 0.79f, 0.84f);
            var rank = (TMP_Text)ui.FindProperty("_cardRankLabels").GetArrayElementAtIndex(index).objectReferenceValue;
            Place(rank.rectTransform, new Vector2(0.5f, 0.055f), Vector2.zero, new Vector2(-32, 22), true);
            rank.fontSizeMin = 11;
            rank.fontSizeMax = 13;
            rank.color = _muted;
            var divider = Image("Divider", root, new Color(0.19f, 0.28f, 0.34f));
            Place(divider.rectTransform, new Vector2(0.5f, 0.565f), Vector2.zero, new Vector2(-44, 1), true);
            var sparks = new UnityEngine.UI.Image[12];
            for (int i = 0; i < sparks.Length; i++)
            {
                sparks[i] = Image("RaritySpark" + i, root, Color.clear);
                Place(sparks[i].rectTransform, Vector2.one * 0.5f, Vector2.zero, Vector2.one * 7);
            }
            SetArray(view, "_sparks", sparks);
            var rays = new UnityEngine.UI.Image[16];
            for (int i = 0; i < rays.Length; i++)
            {
                rays[i] = Image("RarityRay" + i, root, Color.clear);
                Place(rays[i].rectTransform, Vector2.one * 0.5f, Vector2.zero, new Vector2(20, 2));
            }
            SetArray(view, "_rays", rays);
            var corners = new UnityEngine.UI.Image[4];
            for (int i = 0; i < corners.Length; i++)
            {
                corners[i] = Image("GoldCorner" + i, root, _teal);
                Place(corners[i].rectTransform, new Vector2(i % 2, i / 2), new Vector2(i % 2 == 0 ? 7 : -7, i < 2 ? 7 : -7), Vector2.one * 9);
                corners[i].rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                corners[i].gameObject.SetActive(false);
            }
            SetArray(view, "_corners", corners);
            return view;
        }

        private static UnityEngine.UI.Image[] Frame(string name, Transform parent, Color color, float thickness, float inset)
        {
            var root = Rect(name, parent);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.one * inset;
            root.offsetMax = Vector2.one * -inset;
            var edges = new UnityEngine.UI.Image[4];
            for (int i = 0; i < 4; i++)
            {
                edges[i] = Image("Edge" + i, root, color);
                var rect = edges[i].rectTransform;
                bool horizontal = i < 2;
                rect.anchorMin = horizontal ? new Vector2(0, i) : new Vector2(i - 2, 0);
                rect.anchorMax = horizontal ? new Vector2(1, i) : new Vector2(i - 2, 1);
                rect.pivot = horizontal ? new Vector2(0.5f, i) : new Vector2(i - 2, 0.5f);
                rect.sizeDelta = horizontal ? new Vector2(0, thickness) : new Vector2(thickness, 0);
                rect.anchoredPosition = Vector2.zero;
            }
            return edges;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var existing = parent.Find(name);
            if (existing != null) return (RectTransform)existing;
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static UnityEngine.UI.Image Image(string name, Transform parent, Color color)
        {
            var rect = Rect(name, parent);
            var image = rect.GetComponent<UnityEngine.UI.Image>() ?? rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = _pixel;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text Text(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, string value, int fontSize, Color color, bool stretch = false)
        {
            var rect = Rect(name, parent);
            Place(rect, anchor, position, size, stretch);
            var text = rect.GetComponent<TextMeshProUGUI>() ?? rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = _font;
            text.fontSize = fontSize;
            text.enableAutoSizing = stretch;
            text.fontSizeMin = Mathf.Min(fontSize, 11);
            text.fontSizeMax = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, bool stretch = false)
        {
            rect.anchorMin = stretch ? new Vector2(0, anchor.y) : anchor;
            rect.anchorMax = stretch ? new Vector2(1, anchor.y) : anchor;
            rect.pivot = Vector2.one * 0.5f;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void Set(Object target, string name, Object value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(name).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string name, Object[] values)
        {
            var data = new SerializedObject(target);
            var property = data.FindProperty(name);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
