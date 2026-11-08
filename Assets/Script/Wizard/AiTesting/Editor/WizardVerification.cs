using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace VonderGames.AiTesting.Editor
{
    public static class WizardVerification
    {
        public static void RepairPrimitiveTexture()
        {
            const string texturePath = "Assets/ScriptableObject/PrimitiveTexture.asset";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                texture.name = "PrimitiveWhiteTexture";
                var pixels = new Color[16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                texture.SetPixels(pixels);
                texture.Apply();
                AssetDatabase.CreateAsset(texture, texturePath);
            }
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ScriptableObject/PrimitiveSquare.asset");
            var replacement = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4);
            replacement.name = "PrimitiveSquare";
            EditorUtility.CopySerialized(replacement, existing);
            UnityEngine.Object.DestroyImmediate(replacement);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
        }

        public static void PolishUpgradeCards()
        {
            var ui = UnityEngine.Object.FindFirstObjectByType<VonderGames.AiTesting.UI.UICanvasGameplayInstance>();
            AlignCards(ui);
            EditorUtility.SetDirty(ui);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            var prefab = PrefabUtility.LoadPrefabContents("Assets/Prefab/UI/CanvasScreenSpace.prefab");
            AlignCards(prefab.GetComponent<VonderGames.AiTesting.UI.UICanvasGameplayInstance>());
            PrefabUtility.SaveAsPrefabAsset(prefab, "Assets/Prefab/UI/CanvasScreenSpace.prefab");
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        public static string SetResolution(int width, int height)
        {
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singleton.GetProperty("instance").GetValue(null);
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { 1 });
            var sizeType = assembly.GetType("UnityEditor.GameViewSize");
            var fixedType = assembly.GetType("UnityEditor.GameViewSizeType");
            var size = Activator.CreateInstance(sizeType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                new[] { Enum.ToObject(fixedType, 1), (object)width, height, "Wizard QA " + width + "x" + height }, null);
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
            int index = (int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group, null) +
                (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null) - 1;
            var gameView = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
            gameView.GetType().GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(gameView, index);
            gameView.Repaint();
            return width + "x" + height;
        }

        public static string InspectLayout()
        {
            Canvas.ForceUpdateCanvases();
            var issues = new List<string>();
            int textCount = 0;
            int buttonCount = 0;
            foreach (var text in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            {
                textCount++;
                text.ForceMeshUpdate();
                if (text.isTextOverflowing)
                    issues.Add(text.name + ": text overflow");

                CheckRect(text.rectTransform, issues);
            }
            foreach (var button in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
            {
                buttonCount++;
                CheckRect((RectTransform)button.transform, issues);
            }
            return JsonUtility.ToJson(new LayoutReport { Width = Screen.width, Height = Screen.height, TextCount = textCount, ButtonCount = buttonCount, Issues = issues.ToArray() });
        }

        private static void CheckRect(RectTransform rect, List<string> issues)
        {
            if (rect.rect.width <= 0 || rect.rect.height <= 0)
                issues.Add(rect.name + ": zero size");

            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                if (corner.x < -1 || corner.y < -1 || corner.x > Screen.width + 1 || corner.y > Screen.height + 1)
                {
                    issues.Add(rect.name + ": outside screen");
                    break;
                }
            }
        }

        private static void AlignCards(VonderGames.AiTesting.UI.UICanvasGameplayInstance ui)
        {
            var serialized = new SerializedObject(ui);
            var cards = serialized.FindProperty("_cards");
            var descriptions = serialized.FindProperty("_cardLabels");
            var titles = serialized.FindProperty("_cardTitleLabels");
            var ranks = serialized.FindProperty("_cardRankLabels");
            titles.arraySize = ranks.arraySize = cards.arraySize;
            for (int i = 0; i < cards.arraySize; i++)
            {
                var card = (UnityEngine.UI.Button)cards.GetArrayElementAtIndex(i).objectReferenceValue;
                var description = (TMP_Text)descriptions.GetArrayElementAtIndex(i).objectReferenceValue;
                description.name = "Description";
                description.margin = Vector4.zero;
                description.alignment = TextAlignmentOptions.Top;
                var rect = description.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(20, 84);
                rect.offsetMax = new Vector2(-20, -130);
                var title = CardText(card.transform, "Title", description.font, 28);
                title.rectTransform.anchorMin = new Vector2(0, 1);
                title.rectTransform.anchorMax = Vector2.one;
                title.rectTransform.offsetMin = new Vector2(20, -110);
                title.rectTransform.offsetMax = new Vector2(-20, -20);
                var rank = CardText(card.transform, "Rank", description.font, 24);
                rank.rectTransform.anchorMin = Vector2.zero;
                rank.rectTransform.anchorMax = Vector2.right;
                rank.rectTransform.offsetMin = new Vector2(20, 28);
                rank.rectTransform.offsetMax = new Vector2(-20, 64);
                titles.GetArrayElementAtIndex(i).objectReferenceValue = title;
                ranks.GetArrayElementAtIndex(i).objectReferenceValue = rank;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TMP_Text CardText(Transform parent, string name, TMP_FontAsset font, float size)
        {
            var existing = parent.Find(name);
            var text = existing != null ? existing.GetComponent<TMP_Text>() : new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            text.transform.SetParent(parent, false);
            text.font = font;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        [Serializable]
        private sealed class LayoutReport
        {
            public int Width;
            public int Height;
            public int TextCount;
            public int ButtonCount;
            public string[] Issues;
        }
    }
}
