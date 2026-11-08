using System;
using System.IO;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Entity;
using VonderGames.AiTesting.Gameplay.Instance;
using VonderGames.AiTesting.UI;

namespace VonderGames.AiTesting.Editor
{
    public static class WizardElementalSetup
    {
        private const string _SCENE_PATH = "Assets/Scene/Initialize.unity";
        private const string _DATA_ROOT = "Assets/ScriptableObject/";
        private static TMP_FontAsset _font;

        [MenuItem("Wizard/Apply elemental combat and Vonder structure")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Stop Play Mode before scene authoring.");

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != _SCENE_PATH)
            {
                if (scene.isDirty)
                    throw new InvalidOperationException("Save the active scene before applying the Wizard scene migration.");

                scene = EditorSceneManager.OpenScene(_SCENE_PATH);
            }
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            Folder("Assets/Prefab/Entity");
            Folder("Assets/Prefab/UI");
            Folder("Assets/Prefab/Effect");
            foreach (string name in new[] { "Player", "Enemy", "SpellProjectile", "ExperiencePickup" })
                Move($"Assets/Prefab/{name}.prefab", $"Assets/Prefab/Entity/{name}.prefab");

            Move("Assets/Prefab/CanvasGameplay.prefab", "Assets/Prefab/UI/CanvasScreenSpace.prefab");
            Move(_DATA_ROOT + "Fireball.asset", _DATA_ROOT + "Fire.asset");
            Move(_DATA_ROOT + "Nova.asset", _DATA_ROOT + "Water.asset");
            Move(_DATA_ROOT + "UpgradeFireball.asset", _DATA_ROOT + "UpgradeFire.asset");
            Move(_DATA_ROOT + "UpgradeNova.asset", _DATA_ROOT + "UpgradeWater.asset");

            var spells = new[]
            {
                Spell("Bolt", SpellKind.Bolt, "Wand Bolt", 10, 0.6f, 24, 0, new Color(0.3f, 0.85f, 1)),
                Spell("Fire", SpellKind.Fire, "Flame Lance", 80, 6, 24, 0, new Color(1, 0.42f, 0.12f)),
                Spell("Water", SpellKind.Water, "Frost Pulse", 10, 8, 0, 4.5f, new Color(0.35f, 0.8f, 1)),
                Spell("Thunder", SpellKind.Thunder, "Chain Spark", 40, 7, 0, 0, new Color(0.85f, 0.65f, 1)),
                Spell("Deflect", SpellKind.Deflect, "Deflect", 40, 4, 18, 3, Color.cyan)
            };
            var upgrades = new[]
            {
                Upgrade("UpgradeFire", UpgradeKind.Fire, "Flame Lance", "+30 damage per common upgrade; pierces 8 units with impact explosions."),
                Upgrade("UpgradeWater", UpgradeKind.Water, "Frost Pulse", "+10 damage and +0.3s stun per upgrade. Stun duration stops at 3s; damage continues."),
                Upgrade("UpgradeThunder", UpgradeKind.Thunder, "Chain Spark", "+16 damage, +1 chain target and +0.75 link range per upgrade.")
            };
            Set(spells[1], "_damageIncrement", 30f);
            Set(spells[2], "_damageIncrement", 10f);
            Set(spells[2], "_secondaryIncrement", 0.3f);
            Set(spells[3], "_damageIncrement", 16f);
            var combat = Asset<CombatDefinition>("DefaultCombat");
            Set(combat, "_hostileSpeed", 26f);
            Set(combat, "_hostileRadius", 0.3f);
            Set(combat, "_rangedEnemyCap", 5);
            Set(combat, "_deflectWindow", 0.12f);
            Set(combat, "_perfectWindow", 0.12f);
            var ranged = Asset<EnemyDefinition>("Ranged");
            Set(ranged, "_isRanged", true);
            Set(ranged, "_health", 35f);
            Set(ranged, "_speed", 2.5f);
            Set(ranged, "_damage", 10f);
            Set(ranged, "_attackCooldown", 2f);
            Set(ranged, "_experience", 2);
            Set(ranged, "_color", new Color(0.95f, 0.35f, 0.65f));
            Set(ranged, "_preferredMinimum", 9f);
            Set(ranged, "_preferredMaximum", 12f);
            Set(ranged, "_aimDuration", 0.5f);
            var difficulty = AssetDatabase.LoadAssetAtPath<DifficultyDefinition>(_DATA_ROOT + "DefaultDifficulty.asset");
            Set(difficulty, "_bossTime", 300f);
            var difficultyData = new SerializedObject(difficulty);
            var phases = difficultyData.FindProperty("_phases");
            float[] times = { 0, 30, 60, 120, 150, 240 };
            for (int i = 0; i < phases.arraySize; i++)
                phases.GetArrayElementAtIndex(i).FindPropertyRelative("StartTime").floatValue = times[i];

            difficultyData.ApplyModifiedPropertiesWithoutUndo();
            var chaser = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(_DATA_ROOT + "Chaser.asset");
            Set(chaser, "_health", 20f);
            PatchEnemyPrefab();
            PatchInput();

            GameObject manager = Root(scene, "Manager", "Systems");
            GameObject gameObject = FindComponent<GameplayInstance>(scene).gameObject;
            gameObject.transform.SetParent(null, true);
            gameObject.name = "GameplayInstance";
            var game = gameObject.GetComponent<GameplayInstance>();
            GameObject cameraRig = Root(scene, "CameraRig", "Cameras");
            var follow = cameraRig.GetComponentInChildren<CinemachineCamera>(true);
            cameraRig.GetComponentInChildren<CinemachineBrain>(true).IgnoreTimeScale = true;
            var impulse = (CinemachineImpulseSource)new SerializedObject(game).FindProperty("_impulse").objectReferenceValue;
            impulse.ImpulseDefinition.ImpulseDuration = 0.15f;
            EditorUtility.SetDirty(impulse);
            if (!follow.TryGetComponent<ArenaCameraBounds>(out _))
                follow.gameObject.AddComponent<ArenaCameraBounds>();

            GameObject level = Root(scene, "Level", "Level");
            Transform environment = level.transform.Find("Environment");
            Transform collision = Child(level.transform, "Collision");
            for (int i = environment.childCount - 1; i >= 0; i--)
            {
                Transform child = environment.GetChild(i);
                if (child.name == "BoundaryWall" || child.name == "CameraBounds")
                    child.SetParent(collision, true);
            }
            Transform entity = level.transform.Find("Entity") ?? level.transform.Find("Entities");
            entity.name = "Entity";
            Transform effectRoot = Child(level.transform, "Effect");
            GameObject uiRoot = Root(scene, "UICanvasGameplayInstance", "UI");
            var ui = uiRoot.GetComponentInChildren<UICanvasGameplayInstance>(true);
            ui.gameObject.name = "CanvasScreenSpace";
            UIGestureDrawing drawing = PatchUI(ui);
            Transform worldCanvas = ChildRect(uiRoot.transform, "CanvasWorldSpace", new Vector2(2000, 1200));
            var canvas = GetOrAdd<Canvas>(worldCanvas.gameObject);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cameraRig.GetComponentInChildren<Camera>();
            canvas.sortingOrder = 30;
            worldCanvas.localScale = Vector3.one * 0.03f;
            Transform damageRoot = ChildRect(worldCanvas, "DamageTextContainer", new Vector2(2000, 1200));
            var feedback = GetOrAdd<CombatFeedback>(game.gameObject);
            CreateFeedbackPrefabs(out var particle, out var number, out var material);
            Set(feedback, "_effectRoot", effectRoot);
            Set(feedback, "_damageRoot", damageRoot);
            Set(feedback, "_particlePrefab", particle);
            Set(feedback, "_numberPrefab", number);
            Set(feedback, "_lineMaterial", material);
            Set(game, "_combat", combat);
            Set(game, "_rangedDefinition", ranged);
            Set(game, "_feedback", feedback);
            Set(game, "_drawing", drawing);
            SetArray(game, "_spells", spells);
            SetArray(game, "_upgrades", upgrades);
            Set(game, "_drawingTimeScale", 0.15f);
            manager.transform.SetSiblingIndex(0);
            game.transform.SetSiblingIndex(1);
            cameraRig.transform.SetSiblingIndex(2);
            level.transform.SetSiblingIndex(3);
            uiRoot.transform.SetSiblingIndex(4);

            // Keep the reusable UI prefab in step with the authored scene without replacing either hierarchy.
            const string uiPath = "Assets/Prefab/UI/CanvasScreenSpace.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(uiPath);
            try
            {
                PatchUI(prefab.GetComponent<UICanvasGameplayInstance>());
                PrefabUtility.SaveAsPrefabAsset(prefab, uiPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            WizardProjectStructure.Apply();
            Debug.Log("Elemental combat assets and Vonder hierarchy authored. Gameplay verification has not been run.");
        }

        private static UIGestureDrawing PatchUI(UICanvasGameplayInstance ui)
        {
            var serialized = new SerializedObject(ui);
            var safe = (RectTransform)serialized.FindProperty("_safeArea").objectReferenceValue;
            ((GameObject)serialized.FindProperty("_overlay").objectReferenceValue).name = "ModalContainer";
            var slots = safe.Find("HUD/SpellSlots").GetComponent<RectTransform>();
            Set(ui, "_spellSlots", slots);
            slots.sizeDelta = new Vector2(800, 104);
            if (slots.Find("Deflect") == null)
            {
                var deflect = UnityEngine.Object.Instantiate(slots.GetChild(0).gameObject, slots);
                deflect.name = "Deflect";
            }
            var slotLabels = new TMP_Text[4];
            var slotFills = new UnityEngine.UI.Image[4];
            var buttons = new UnityEngine.UI.Button[4];
            string[] names = { "Fire", "Water", "Thunder", "Deflect" };
            for (int i = 0; i < 4; i++)
            {
                var slot = slots.GetChild(i);
                slot.name = names[i];
                var background = slot.GetComponent<UnityEngine.UI.Image>();
                background.raycastTarget = true;
                buttons[i] = GetOrAdd<UnityEngine.UI.Button>(slot.gameObject);
                buttons[i].targetGraphic = background;
                var label = slot.GetComponentInChildren<TMP_Text>(true);
                slotLabels[i] = label;
                slotFills[i] = slot.Find("Cooldown").GetComponent<UnityEngine.UI.Image>();
                label.enableAutoSizing = true;
                label.fontSizeMin = 14;
                label.fontSizeMax = 20;
            }
            SetArray(ui, "_spellButtons", buttons);
            SetArray(ui, "_spellLabels", slotLabels);
            SetArray(ui, "_spellFills", slotFills);
            var hud = safe.Find("HUD");
            var hint = hud.Find("ControlsHint").GetComponent<TMP_Text>();
            hint.text = "WASD  Move  •  Wand fires automatically\nSpace  Dash  •  RMB / 4  Deflect  •  Esc  Pause";
            hint.fontSize = 18;
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = hint.rectTransform.pivot = new Vector2(0, 1);
            hint.rectTransform.anchoredPosition = new Vector2(32, -110);
            hint.rectTransform.sizeDelta = new Vector2(420, 54);
            Set(ui, "_comboLabel", Label("Combo", hud, new Vector2(0, 245), new Vector2(1000, 42), "", 24));
            Set(ui, "_cue", Label("CombatCue", hud, new Vector2(0, 345), new Vector2(1100, 60), "", 32));
            ((TMP_Text)serialized.FindProperty("_timerLabel").objectReferenceValue).text = "00:00 / 05:00";
            var dash = ChildRect(hud, "Dash", new Vector2(190, 54));
            dash.anchorMin = dash.anchorMax = new Vector2(0.5f, 0);
            dash.anchoredPosition = new Vector2(400, 84);
            slots.anchoredPosition = new Vector2(-110, 32);
            Set(ui, "_dashRoot", dash);
            Image("Background", dash, new Vector2(190, 54), new Color(0.07f, 0.13f, 0.2f));
            var dashFill = Image("Cooldown", dash, new Vector2(190, 54), new Color(0.2f, 0.5f, 0.7f, 0.65f));
            dashFill.type = UnityEngine.UI.Image.Type.Filled;
            dashFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            Set(ui, "_dashFill", dashFill);
            Set(ui, "_dashLabel", Label("Label", dash, Vector2.zero, new Vector2(190, 54), "SPACE  DASH READY", 18));
            Set(ui, "_resetCue", Label("CooldownReset", hud, new Vector2(0, 275), new Vector2(800, 30), "", 20));
            var summary = Label("StatSummary", hud, Vector2.zero, new Vector2(600, 60), "", 16);
            summary.rectTransform.anchorMin = summary.rectTransform.anchorMax = new Vector2(0.5f, 1);
            summary.rectTransform.anchoredPosition = new Vector2(0, -90);
            Set(ui, "_statSummary", summary);
            var streak = Image("ExperienceStreak", hud, new Vector2(18, 5), new Color(0.4f, 1, 0.7f)).rectTransform;
            streak.gameObject.SetActive(false);
            Set(ui, "_experienceStreak", streak);
            var upgrade = (RectTransform)serialized.FindProperty("_upgradePanel").objectReferenceValue;
            var confettiRoot = ChildRect(upgrade, "Confetti", Vector2.zero);
            Stretch(confettiRoot);
            confettiRoot.SetAsFirstSibling();
            Set(ui, "_confettiRoot", confettiRoot);
            var confetti = new UnityEngine.UI.Image[32];
            for (int i = 0; i < confetti.Length; i++)
            {
                confetti[i] = Image("Piece" + i, confettiRoot, new Vector2(6, 10), Color.white);
                confetti[i].gameObject.SetActive(false);
            }
            SetArray(ui, "_confetti", confetti);
            var cards = serialized.FindProperty("_cards");
            var accents = new TMP_Text[cards.arraySize];
            var innerBorders = new UnityEngine.UI.Outline[cards.arraySize];
            for (int i = 0; i < cards.arraySize; i++)
            {
                var card = ((UnityEngine.UI.Button)cards.GetArrayElementAtIndex(i).objectReferenceValue).gameObject;
                GetOrAdd<UnityEngine.UI.Outline>(card);
                var inner = Image("InnerRarityBorder", card.transform, Vector2.zero, Color.clear).rectTransform;
                Stretch(inner);
                inner.offsetMin = Vector2.one * 7;
                inner.offsetMax = Vector2.one * -7;
                innerBorders[i] = GetOrAdd<UnityEngine.UI.Outline>(inner.gameObject);
                innerBorders[i].useGraphicAlpha = false;
                innerBorders[i].effectDistance = Vector2.one;
                inner.gameObject.SetActive(false);
                accents[i] = Label("RarityAccent", card.transform, new Vector2(0, 74), new Vector2(240, 36), "", 24);
                accents[i].rectTransform.anchorMin = accents[i].rectTransform.anchorMax = new Vector2(0.5f, 0);
                foreach (var text in card.GetComponentsInChildren<TMP_Text>(true))
                {
                    text.enableAutoSizing = true;
                    text.fontSizeMin = 14;
                    text.fontSizeMax = text.name == "Title" ? 28 : text.name == "Description" ? 22 : 24;
                    var rect = text.rectTransform;
                    rect.anchorMin = new Vector2(0, rect.anchorMin.y);
                    rect.anchorMax = new Vector2(1, rect.anchorMax.y);
                    rect.sizeDelta = new Vector2(-32, rect.sizeDelta.y);
                    rect.anchoredPosition = new Vector2(0, rect.anchoredPosition.y);
                }
            }
            SetArray(ui, "_cardAccents", accents);
            SetArray(ui, "_innerBorders", innerBorders);
            var drawing = GetOrAdd<UIGestureDrawing>(ui.gameObject);
            var shade = ChildRect(safe, "DrawingOverlay", Vector2.zero);
            Stretch(shade);
            var shadeImage = GetOrAdd<UnityEngine.UI.Image>(shade.gameObject);
            shadeImage.color = Color.clear;
            shadeImage.raycastTarget = false;
            var panel = ChildRect(shade, "DrawingPanel", new Vector2(640, 530));
            panel.anchorMin = new Vector2(0.75f, 0.28f);
            panel.anchorMax = new Vector2(1, 0.88f);
            panel.offsetMin = new Vector2(4, 0);
            panel.offsetMax = new Vector2(-8, 0);
            var panelImage = GetOrAdd<UnityEngine.UI.Image>(panel.gameObject);
            panelImage.color = new Color(0.06f, 0.09f, 0.16f, 0.98f);
            Set(drawing, "_root", shade.gameObject);
            Set(drawing, "_heading", Label("Heading", panel, new Vector2(0, 210), new Vector2(580, 52), "TRACE YOUR SPELL", 30));
            Set(drawing, "_timer", Label("Timer", panel, new Vector2(0, 160), new Vector2(580, 42), "3.0s", 24));
            Set(drawing, "_instruction", Label("Instruction", panel, new Vector2(0, -207), new Vector2(580, 82), "", 21));
            var guide = ChildRect(panel, "Guide", new Vector2(400, 320));
            guide.anchoredPosition = new Vector2(0, -15);
            Set(drawing, "_guide", guide);
            var segments = new RectTransform[4];
            var arrows = new RectTransform[4];
            var trails = new RectTransform[64];
            for (int i = 0; i < 64; i++)
            {
                trails[i] = Image("Trail" + i, guide, new Vector2(1, 5), Color.white).rectTransform;
                trails[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < 4; i++)
            {
                segments[i] = Image("GuideSegment" + i, guide, new Vector2(1, 4), new Color(0.4f, 0.5f, 0.6f, 0.6f)).rectTransform;
                arrows[i] = Label("Direction" + i, guide, Vector2.zero, new Vector2(36, 36), ">", 28).rectTransform;
            }
            var markers = new UnityEngine.UI.Image[5];
            var labels = new TMP_Text[5];
            for (int i = 0; i < 5; i++)
            {
                markers[i] = Image("Checkpoint" + i, guide, new Vector2(64, 64), Color.white);
                labels[i] = Label("Number", markers[i].transform, Vector2.zero, new Vector2(64, 64), (i + 1).ToString(), 19);
                labels[i].color = new Color(0.03f, 0.06f, 0.1f);
            }
            SetArray(drawing, "_segments", segments);
            SetArray(drawing, "_arrows", arrows);
            SetArray(drawing, "_trailSegments", trails);
            SetArray(drawing, "_markers", markers);
            SetArray(drawing, "_markerLabels", labels);
            foreach (var text in new[] { "Heading", "Timer", "Instruction" })
            {
                var label = panel.Find(text).GetComponent<TMP_Text>();
                label.enableAutoSizing = true;
                label.fontSizeMin = 14;
                label.fontSizeMax = label.fontSize;
                var rect = label.rectTransform;
                rect.anchorMin = new Vector2(0, text == "Instruction" ? 0 : 1);
                rect.anchorMax = new Vector2(1, text == "Instruction" ? 0 : 1);
                rect.sizeDelta = new Vector2(-16, text == "Instruction" ? 90 : 44);
                rect.anchoredPosition = new Vector2(0, text == "Heading" ? -30 : text == "Timer" ? -72 : 48);
            }
            shade.gameObject.SetActive(false);
            WizardUIRedesign.Apply(ui, _font);
            return drawing;
        }

        private static void CreateFeedbackPrefabs(out SpriteRenderer particle, out TextMeshProUGUI number, out Material material)
        {
            const string particlePath = "Assets/Prefab/Effect/ImpactParticle.prefab";
            var particleAsset = AssetDatabase.LoadAssetAtPath<GameObject>(particlePath);
            if (particleAsset == null)
            {
                var instance = new GameObject("ImpactParticle", typeof(SpriteRenderer));
                var renderer = instance.GetComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(_DATA_ROOT + "PrimitiveSquare.asset");
                renderer.sortingOrder = 8;
                particleAsset = PrefabUtility.SaveAsPrefabAsset(instance, particlePath);
                UnityEngine.Object.DestroyImmediate(instance);
            }
            particle = particleAsset.GetComponent<SpriteRenderer>();
            const string numberPath = "Assets/Prefab/Effect/DamageNumber.prefab";
            var numberAsset = AssetDatabase.LoadAssetAtPath<GameObject>(numberPath);
            if (numberAsset == null)
            {
                var instance = new GameObject("DamageNumber", typeof(RectTransform));
                var text = instance.AddComponent<TextMeshProUGUI>();
                text.font = _font;
                text.fontSize = 24;
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Center;
                text.raycastTarget = false;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.rectTransform.sizeDelta = new Vector2(160, 60);
                text.text = "10";
                numberAsset = PrefabUtility.SaveAsPrefabAsset(instance, numberPath);
                UnityEngine.Object.DestroyImmediate(instance);
            }
            number = numberAsset.GetComponent<TextMeshProUGUI>();
            const string materialPath = _DATA_ROOT + "SpellLine.mat";
            material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Sprites/Default")) { name = "SpellLine" };
                AssetDatabase.CreateAsset(material, materialPath);
            }
        }

        private static void PatchEnemyPrefab()
        {
            const string path = "Assets/Prefab/Entity/Enemy.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var marker = Child(prefab.transform, "StunMarker");
                marker.localPosition = Vector3.up * 0.9f;
                marker.localScale = Vector3.one * 0.25f;
                marker.localRotation = Quaternion.Euler(0, 0, 45);
                var renderer = GetOrAdd<SpriteRenderer>(marker.gameObject);
                renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(_DATA_ROOT + "PrimitiveSquare.asset");
                renderer.color = new Color(0.35f, 0.85f, 1);
                renderer.sortingOrder = 6;
                renderer.enabled = false;
                Set(prefab.GetComponent<EnemyAgent>(), "_stunMarker", renderer);
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        private static void PatchInput()
        {
            const string path = "Assets/InputSystem/InputActions.inputactions";
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            var copy = InputActionAsset.FromJson(asset.ToJson());
            var player = copy.FindActionMap("Player");
            foreach (string name in new[] { "Aim", "Bolt", "Fireball", "Nova" })
                player.FindAction(name, false)?.RemoveAction();

            if (player.FindAction("Dash", false) == null) player.AddAction("Dash", InputActionType.Button, "<Keyboard>/space");
            if (player.FindAction("Deflect", false) == null) player.AddAction("Deflect", InputActionType.Button, "<Mouse>/rightButton");
            var ui = copy.FindActionMap("UI");
            if (ui.FindAction("Choice4", false) == null)
            {
                var choice = ui.AddAction("Choice4", InputActionType.Button, "<Keyboard>/4");
                choice.AddBinding("<Keyboard>/numpad4");
            }
            File.WriteAllText(path, copy.ToJson());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            InputActionCodeGenerator.GenerateWrapperCode("Assets/Script/Wizard/AiTesting/Core/InputSystem/InputActions.cs",
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(path), new InputActionCodeGenerator.Options
                {
                    className = "InputActions",
                    namespaceName = "VonderGames.AiTesting.Core.InputSystem",
                    sourceAssetPath = path
                });
            UnityEngine.Object.DestroyImmediate(copy);
        }

        private static SpellDefinition Spell(string file, SpellKind kind, string name, float damage, float cooldown, float speed, float radius, Color color)
        {
            var asset = Asset<SpellDefinition>(file);
            Set(asset, "_kind", (int)kind);
            Set(asset, "_displayName", name);
            Set(asset, "_damage", damage);
            Set(asset, "_cooldown", cooldown);
            Set(asset, "_speed", speed);
            Set(asset, "_radius", radius);
            Set(asset, "_color", color);
            return asset;
        }

        private static UpgradeDefinition Upgrade(string file, UpgradeKind kind, string name, string description)
        {
            var asset = Asset<UpgradeDefinition>(file);
            Set(asset, "_kind", (int)kind);
            Set(asset, "_displayName", name);
            Set(asset, "_description", description);
            return asset;
        }

        private static T Asset<T>(string file) where T : ScriptableObject
        {
            string path = _DATA_ROOT + file + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        private static void Set(UnityEngine.Object target, string name, object value)
        {
            var data = new SerializedObject(target);
            var property = data.FindProperty(name);
            if (value is bool boolean) property.boolValue = boolean;
            else if (value is UnityEngine.Object reference) property.objectReferenceValue = reference;
            else if (value is float number) property.floatValue = number;
            else if (value is int integer) property.intValue = integer;
            else if (value is string text) property.stringValue = text;
            else if (value is Color color) property.colorValue = color;
            else throw new ArgumentException(name);
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetArray(UnityEngine.Object target, string name, UnityEngine.Object[] values)
        {
            var data = new SerializedObject(target);
            var property = data.FindProperty(name);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static GameObject Root(Scene scene, string name, string previousName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name || root.name == previousName)
                {
                    root.name = name;
                    return root;
                }
            }
            return new GameObject(name);
        }

        private static T FindComponent<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }
            throw new InvalidOperationException(typeof(T).Name + " is missing from the authored scene.");
        }

        private static Transform Child(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
                return existing;

            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static RectTransform ChildRect(Transform parent, string name, Vector2 size)
        {
            var existing = parent.Find(name);
            if (existing != null)
                return existing.GetComponent<RectTransform>();

            var child = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            child.SetParent(parent, false);
            child.anchorMin = child.anchorMax = child.pivot = Vector2.one * 0.5f;
            child.sizeDelta = size;
            return child;
        }

        private static TMP_Text Label(string name, Transform parent, Vector2 position, Vector2 size, string text, float fontSize)
        {
            var rect = ChildRect(parent, name, size);
            rect.anchoredPosition = position;
            var label = GetOrAdd<TextMeshProUGUI>(rect.gameObject);
            label.font = _font;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        private static UnityEngine.UI.Image Image(string name, Transform parent, Vector2 size, Color color)
        {
            var rect = ChildRect(parent, name, size);
            var image = GetOrAdd<UnityEngine.UI.Image>(rect.gameObject);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            if (component == null)
                component = target.AddComponent<T>();

            return component;
        }

        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            Folder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void Move(string source, string destination)
        {
            if (AssetDatabase.LoadMainAssetAtPath(source) == null)
                return;

            string error = AssetDatabase.MoveAsset(source, destination);
            if (!string.IsNullOrEmpty(error))
                throw new IOException(error);
        }
    }
}
