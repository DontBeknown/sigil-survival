using System.IO;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using VonderGames.AiTesting.Core;
using VonderGames.AiTesting.Gameplay.Entity;
using VonderGames.AiTesting.Gameplay.Instance;
using VonderGames.AiTesting.Gameplay.Manager;
using VonderGames.AiTesting.UI;

namespace VonderGames.AiTesting.Editor
{
    public static class WizardSceneBuilder
    {
        private static Sprite _square;
        private static TMP_FontAsset _font;
        private static readonly Color _ink = new Color(0.045f, 0.065f, 0.11f);
        private static readonly Color _panel = new Color(0.08f, 0.11f, 0.18f);
        private static readonly Color _cyan = new Color(0.25f, 0.8f, 1);

        [MenuItem("Wizard/Create playable scene")]
        public static void Create()
        {
            if (EditorApplication.isPlaying)
                throw new System.InvalidOperationException("Stop Play Mode before scene authoring.");

            if (File.Exists("Assets/Scene/Initialize.unity"))
                throw new System.InvalidOperationException("Initialize already exists. Preserve it and edit incrementally.");

            foreach (string folder in new[] { "Assets/Scene", "Assets/Prefab", "Assets/ScriptableObject" })
                Directory.CreateDirectory(folder);

            AssetDatabase.Refresh();
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (_font == null)
            {
                TMPro.TMP_PackageResourceImporter.ImportResources(true, false, false);
                throw new System.InvalidOperationException("TMP Essentials import has not finished. Retry once the import completes.");
            }

            _square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), Vector2.one * 0.5f, Texture2D.whiteTexture.width);
            _square.name = "PrimitiveSquare";
            AssetDatabase.CreateAsset(_square, "Assets/ScriptableObject/PrimitiveSquare.asset");
            WizardVerification.RepairPrimitiveTexture();
            ConfigureLayers();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var systems = new GameObject("Systems").transform;
            var level = new GameObject("Level").transform;
            var environment = Child("Environment", level);
            var entities = Child("Entities", level);
            var runtime = Child("RunObjects", entities);
            var cameras = new GameObject("Cameras").transform;
            var uiRoot = new GameObject("UI").transform;
            var game = Child("GameplayInstance", systems).gameObject.AddComponent<GameplayInstance>();
            var input = Child("InputManager", systems).gameObject.AddComponent<InputManager>();
            var eventSystem = Child("EventSystem", systems).gameObject.AddComponent<EventSystem>();
            var module = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            Set(input, "_uiModule", module);

            CreateArena(environment);
            var playerObject = CreateActor("Player", _cyan, 8);
            var player = playerObject.AddComponent<PlayerController>();
            var playerFeedback = playerObject.GetComponent<PrimitiveFeedback>();
            var marker = Visual("AimMarker", playerObject.transform, Color.white);
            marker.transform.localPosition = Vector3.right * 0.85f;
            marker.transform.localScale = new Vector3(0.2f, 0.12f, 1);
            Set(player, "_feedback", playerFeedback);
            Set(player, "_aimMarker", marker.transform);
            SavePrefab(playerObject, "Player");
            playerObject.transform.SetParent(entities);

            var enemyObject = CreateActor("Enemy", Color.red, 9);
            var enemy = enemyObject.AddComponent<EnemyAgent>();
            Set(enemy, "_feedback", enemyObject.GetComponent<PrimitiveFeedback>());
            var telegraph = Visual("AttackTelegraph", enemyObject.transform, new Color(1, 0.3f, 0.2f, 0.25f));
            telegraph.sortingOrder = 1;
            telegraph.enabled = false;
            Set(enemy, "_telegraph", telegraph);
            var enemyPrefab = SavePrefab(enemyObject, "Enemy").GetComponent<EnemyAgent>();
            Object.DestroyImmediate(enemyObject);

            var projectileObject = new GameObject("SpellProjectile");
            var projectile = projectileObject.AddComponent<SpellProjectile>();
            var projectileVisual = Visual("Visual", projectileObject.transform, _cyan);
            projectileVisual.sortingOrder = 5;
            Set(projectile, "_renderer", projectileVisual);
            var projectilePrefab = SavePrefab(projectileObject, "SpellProjectile").GetComponent<SpellProjectile>();
            Object.DestroyImmediate(projectileObject);

            var pickupObject = new GameObject("ExperiencePickup");
            pickupObject.transform.localScale = Vector3.one * 0.25f;
            var pickupVisual = Visual("Visual", pickupObject.transform, new Color(0.4f, 1, 0.6f));
            var pickupFeedback = pickupObject.AddComponent<PrimitiveFeedback>();
            Set(pickupFeedback, "_renderer", pickupVisual);
            var pickup = pickupObject.AddComponent<ExperiencePickup>();
            Set(pickup, "_feedback", pickupFeedback);
            var pickupPrefab = SavePrefab(pickupObject, "ExperiencePickup").GetComponent<ExperiencePickup>();
            Object.DestroyImmediate(pickupObject);

            var camera = Child("MainCamera", cameras).gameObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 10;
            camera.backgroundColor = _ink;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.gameObject.AddComponent<CinemachineBrain>();
            var follow = Child("ArenaCamera", cameras).gameObject.AddComponent<CinemachineCamera>();
            follow.Follow = player.transform;
            follow.Lens.OrthographicSize = 10;
            follow.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            follow.transform.position = new Vector3(0, 0, -10);
            var composer = follow.gameObject.AddComponent<CinemachinePositionComposer>();
            composer.CameraDistance = 10;
            composer.Damping = new Vector3(0.25f, 0.25f, 0);
            composer.Composition.DeadZone.Enabled = true;
            composer.Composition.DeadZone.Size = new Vector2(0.08f, 0.08f);
            var bounds = Child("CameraBounds", environment).gameObject.AddComponent<PolygonCollider2D>();
            bounds.points = new[] { new Vector2(-30, -20), new Vector2(-30, 20), new Vector2(30, 20), new Vector2(30, -20) };
            bounds.isTrigger = true;
            bounds.gameObject.layer = 2;
            var confiner = follow.gameObject.AddComponent<CinemachineConfiner2D>();
            confiner.BoundingShape2D = bounds;
            confiner.Damping = 0;
            var listener = follow.gameObject.AddComponent<CinemachineImpulseListener>();
            listener.ChannelMask = 1;
            listener.Gain = 1;
            listener.Use2DDistance = true;
            var impulse = player.gameObject.AddComponent<CinemachineImpulseSource>();
            impulse.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            impulse.ImpulseDefinition.ImpulseDuration = 0.18f;

            var ui = CreateUI(uiRoot);
            Set(game, "_input", input);
            Set(game, "_player", player);
            Set(game, "_camera", camera);
            Set(game, "_followCamera", follow);
            Set(game, "_impulse", impulse);
            Set(game, "_ui", ui);
            Set(game, "_runtimeRoot", runtime);
            Set(game, "_square", _square);
            Set(game, "_enemyPrefab", enemyPrefab);
            Set(game, "_projectilePrefab", projectilePrefab);
            Set(game, "_pickupPrefab", pickupPrefab);
            SetArray(game, "_spells", CreateSpells());
            SetArray(game, "_enemies", new[] { CreateEnemy("Chaser", 40, 2.5f, 10, 2, 0.9f, new Color(1, 0.35f, 0.4f)),
                CreateEnemy("Runner", 20, 4.2f, 8, 3, 0.65f, new Color(1, 0.7f, 0.25f)),
                CreateEnemy("Brute", 160, 1.5f, 20, 8, 1.5f, new Color(0.75f, 0.4f, 1)) });
            Set(game, "_bossDefinition", CreateEnemy("Warden", 2400, 2, 25, 0, 2.4f, new Color(1, 0.2f, 0.65f), true));
            SetArray(game, "_upgrades", CreateUpgrades());
            var difficulty = ScriptableObject.CreateInstance<DifficultyDefinition>();
            var serializedDifficulty = new SerializedObject(difficulty);
            var phases = serializedDifficulty.FindProperty("_phases");
            phases.arraySize = 6;
            float[] times = { 0, 60, 120, 240, 300, 480 };
            float[] rates = { 1, 1.5f, 2.5f, 3.5f, 4.5f, 6 };
            for (int i = 0; i < 6; i++)
            {
                var phase = phases.GetArrayElementAtIndex(i);
                phase.FindPropertyRelative("StartTime").floatValue = times[i];
                phase.FindPropertyRelative("SpawnsPerSecond").floatValue = rates[i];
                phase.FindPropertyRelative("EnemyTypes").intValue = i < 2 ? 1 : i < 4 ? 2 : 3;
            }
            serializedDifficulty.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(difficulty, "Assets/ScriptableObject/DefaultDifficulty.asset");
            Set(game, "_difficulty", difficulty);
            EditorSceneManager.SaveScene(scene, "Assets/Scene/Initialize.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scene/Initialize.unity", true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Wizard playable scene, configuration assets, and prefabs created.");
        }

        private static void ConfigureLayers()
        {
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = settings.FindProperty("layers");
            layers.GetArrayElementAtIndex(8).stringValue = "Player";
            layers.GetArrayElementAtIndex(9).stringValue = "Enemy";
            layers.GetArrayElementAtIndex(10).stringValue = "Arena";
            settings.ApplyModifiedPropertiesWithoutUndo();
            Physics2D.IgnoreLayerCollision(8, 9, true);
            Physics2D.IgnoreLayerCollision(9, 9, true);
        }

        private static void CreateArena(Transform environment)
        {
            var floor = Visual("Floor", environment, new Color(0.075f, 0.095f, 0.14f));
            floor.transform.localScale = new Vector3(60, 40, 1);
            floor.sortingOrder = -10;
            for (int x = -30; x <= 30; x += 5)
            {
                var line = Visual("GridVertical", environment, new Color(0.1f, 0.13f, 0.19f));
                line.transform.position = new Vector3(x, 0, 0);
                line.transform.localScale = new Vector3(0.025f, 40, 1);
                line.sortingOrder = -9;
            }
            for (int y = -20; y <= 20; y += 5)
            {
                var line = Visual("GridHorizontal", environment, new Color(0.1f, 0.13f, 0.19f));
                line.transform.position = new Vector3(0, y, 0);
                line.transform.localScale = new Vector3(60, 0.025f, 1);
                line.sortingOrder = -9;
            }
            Wall(environment, new Vector2(-30, 0), new Vector2(2, 40));
            Wall(environment, new Vector2(30, 0), new Vector2(2, 40));
            Wall(environment, new Vector2(0, -20), new Vector2(60, 2));
            Wall(environment, new Vector2(0, 20), new Vector2(60, 2));
        }

        private static void Wall(Transform parent, Vector2 position, Vector2 size)
        {
            var wall = Visual("BoundaryWall", parent, new Color(0.2f, 0.3f, 0.4f));
            wall.gameObject.layer = 10;
            wall.transform.position = position;
            wall.transform.localScale = size;
            wall.gameObject.AddComponent<BoxCollider2D>();
        }

        private static GameObject CreateActor(string name, Color color, int layer)
        {
            var actor = new GameObject(name);
            actor.layer = layer;
            var body = actor.AddComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            actor.AddComponent<BoxCollider2D>().size = Vector2.one * 0.9f;
            var renderer = Visual("Visual", actor.transform, color);
            var feedback = actor.AddComponent<PrimitiveFeedback>();
            Set(feedback, "_renderer", renderer);
            return actor;
        }

        private static SpellDefinition[] CreateSpells()
        {
            var result = new SpellDefinition[3];
            string[] names = { "Arcane Bolt", "Fireball", "Frost Nova" };
            float[] damage = { 20, 50, 15 };
            float[] cooldown = { 0.35f, 3, 6 };
            float[] radius = { 0, 2.5f, 4 };
            Color[] colors = { _cyan, new Color(1, 0.5f, 0.2f), new Color(0.45f, 0.8f, 1) };
            for (int i = 0; i < 3; i++)
            {
                var spell = ScriptableObject.CreateInstance<SpellDefinition>();
                var data = new SerializedObject(spell);
                data.FindProperty("_kind").enumValueIndex = i;
                data.FindProperty("_displayName").stringValue = names[i];
                data.FindProperty("_damage").floatValue = damage[i];
                data.FindProperty("_cooldown").floatValue = cooldown[i];
                data.FindProperty("_radius").floatValue = radius[i];
                data.FindProperty("_speed").floatValue = i == 1 ? 15 : 24;
                data.FindProperty("_color").colorValue = colors[i];
                data.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(spell, $"Assets/ScriptableObject/{(SpellKind)i}.asset");
                result[i] = spell;
            }
            return result;
        }

        private static EnemyDefinition CreateEnemy(string name, float health, float speed, float damage, int experience, float size, Color color, bool isBoss = false)
        {
            var enemy = ScriptableObject.CreateInstance<EnemyDefinition>();
            var data = new SerializedObject(enemy);
            data.FindProperty("_health").floatValue = health;
            data.FindProperty("_speed").floatValue = speed;
            data.FindProperty("_damage").floatValue = damage;
            data.FindProperty("_experience").intValue = experience;
            data.FindProperty("_size").floatValue = size;
            data.FindProperty("_color").colorValue = color;
            data.FindProperty("_isBoss").boolValue = isBoss;
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(enemy, $"Assets/ScriptableObject/{name}.asset");
            return enemy;
        }

        private static UpgradeDefinition[] CreateUpgrades()
        {
            string[] names = { "Unlock Fireball", "Unlock Frost Nova", "Arcane Power", "Quick Casting", "Swift Steps", "Vitality", "Healing" };
            string[] descriptions = { "RMB: 50 area damage.\n2.5-unit radius.\n3-second cooldown.", "Space: 15 radial damage.\n4-unit radius, knockback,\n50% slow for 2 seconds.\n6-second cooldown.",
                "+15% base spell damage.", "Multiply all spell cooldowns by 0.9 (10% shorter).", "+10% base movement speed.", "+20 maximum health.\nRestore 20 health immediately.", "Restore 30 health,\nup to your maximum." };
            var result = new UpgradeDefinition[7];
            for (int i = 0; i < 7; i++)
            {
                var upgrade = ScriptableObject.CreateInstance<UpgradeDefinition>();
                var data = new SerializedObject(upgrade);
                data.FindProperty("_kind").enumValueIndex = i;
                data.FindProperty("_displayName").stringValue = names[i];
                data.FindProperty("_description").stringValue = descriptions[i];
                data.FindProperty("_maximumRank").intValue = i < 2 ? 1 : 5;
                data.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(upgrade, $"Assets/ScriptableObject/Upgrade{(UpgradeKind)i}.asset");
                result[i] = upgrade;
            }
            return result;
        }

        private static UICanvasGameplayInstance CreateUI(Transform parent)
        {
            var canvasObject = new GameObject("CanvasGameplay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var ui = canvasObject.AddComponent<UICanvasGameplayInstance>();
            var safe = Rect("SafeArea", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Set(ui, "_safeArea", safe);
            var hud = Rect("HUD", safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var health = Bar("Health", hud, new Vector2(0, 1), new Vector2(32, -64), new Vector2(300, 28), _cyan);
            Set(ui, "_healthFill", health);
            Set(ui, "_healthLabel", Label("HealthLabel", hud, new Vector2(0, 1), new Vector2(32, -30), new Vector2(350, 34), "HEALTH", 24, TextAlignmentOptions.Left));
            Set(ui, "_timerLabel", Label("Timer", hud, new Vector2(0.5f, 1), new Vector2(0, -32), new Vector2(350, 48), "00:00 / 10:00", 28));
            Set(ui, "_killsLabel", Label("Kills", hud, Vector2.one, new Vector2(-32, -32), new Vector2(260, 48), "0 DEFEATED", 24, TextAlignmentOptions.Right));
            var boss = Rect("BossHealth", hud, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -114), new Vector2(600, 24));
            var bossBackground = boss.gameObject.AddComponent<UnityEngine.UI.Image>();
            bossBackground.color = _panel;
            bossBackground.raycastTarget = false;
            Set(ui, "_bossRoot", boss.gameObject);
            Set(ui, "_bossFill", Fill("Fill", boss, new Color(1, 0.2f, 0.65f)));
            Set(ui, "_bossLabel", Label("Name", boss, new Vector2(0.5f, 1), new Vector2(0, 8), new Vector2(600, 30), "THE WARDEN", 22));
            boss.gameObject.SetActive(false);
            var experience = Rect("Experience", hud, Vector2.zero, Vector2.right, Vector2.zero, Vector2.zero);
            experience.offsetMin = new Vector2(32, 160);
            experience.offsetMax = new Vector2(-32, 180);
            var xpBackground = experience.gameObject.AddComponent<UnityEngine.UI.Image>();
            xpBackground.color = _panel;
            xpBackground.raycastTarget = false;
            Set(ui, "_experienceFill", Fill("Fill", experience, new Color(0.4f, 1, 0.6f)));
            Set(ui, "_experienceLabel", Label("Level", hud, new Vector2(0.5f, 0), new Vector2(0, 195), new Vector2(600, 36), "LEVEL 1", 24));
            var slots = Rect("SpellSlots", hud, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 32), new Vector2(272, 80));
            var row = slots.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 16;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = true;
            var spellLabels = new TMP_Text[3];
            var spellFills = new UnityEngine.UI.Image[3];
            for (int i = 0; i < 3; i++)
            {
                var slot = Rect("Spell" + i, slots, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(80, 80));
                var background = slot.gameObject.AddComponent<UnityEngine.UI.Image>();
                background.color = _panel;
                background.raycastTarget = false;
                spellFills[i] = Fill("Cooldown", slot, new Color(0.2f, 0.35f, 0.5f, 0.7f));
                spellLabels[i] = StretchLabel("Label", slot, "LOCKED", 18);
                spellLabels[i].textWrappingMode = TextWrappingModes.NoWrap;
            }
            SetArray(ui, "_spellLabels", spellLabels);
            SetArray(ui, "_spellFills", spellFills);
            Label("ControlsHint", hud, new Vector2(0, 0), new Vector2(32, 32), new Vector2(460, 64), "WASD  Move   •   Mouse  Aim\nLMB  Cast   •   Esc  Pause", 22, TextAlignmentOptions.Left);
            var overlay = Rect("Panels", safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var shade = overlay.gameObject.AddComponent<UnityEngine.UI.Image>();
            shade.color = new Color(0.015f, 0.025f, 0.05f, 0.86f);
            Set(ui, "_overlay", overlay.gameObject);
            var menu = Rect("Menu", overlay, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(640, 600));
            menu.gameObject.AddComponent<UnityEngine.UI.Image>().color = _panel;
            Set(ui, "_menu", menu);
            Set(ui, "_heading", Label("Heading", menu, new Vector2(0.5f, 1), new Vector2(0, -32), new Vector2(600, 60), "SQUARE WIZARD", 38));
            Set(ui, "_body", Label("Body", menu, new Vector2(0.5f, 1), new Vector2(0, -112), new Vector2(580, 190), "", 24));
            var buttons = Rect("Buttons", menu, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 28), new Vector2(400, 264));
            var column = buttons.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 10;
            column.childControlHeight = column.childControlWidth = true;
            column.childForceExpandHeight = false;
            column.childAlignment = TextAnchor.LowerCenter;
            var menuButtons = new UnityEngine.UI.Button[4];
            var menuLabels = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                menuButtons[i] = Button("Action" + i, buttons);
                menuButtons[i].gameObject.AddComponent<LayoutElement>().preferredHeight = 56;
                menuLabels[i] = StretchLabel("Label", menuButtons[i].transform, "Begin run", 26);
            }
            SetArray(ui, "_menuButtons", menuButtons);
            SetArray(ui, "_menuButtonLabels", menuLabels);
            var upgrade = Rect("UpgradeSelection", overlay, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(1120, 520));
            upgrade.gameObject.AddComponent<UnityEngine.UI.Image>().color = _panel;
            Set(ui, "_upgradePanel", upgrade);
            Set(ui, "_upgradeHeading", Label("Heading", upgrade, new Vector2(0.5f, 1), new Vector2(0, -32), new Vector2(1060, 60), "CHOOSE AN UPGRADE", 32));
            var cardRow = Rect("Cards", upgrade, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            cardRow.offsetMin = new Vector2(28, 32);
            cardRow.offsetMax = new Vector2(-28, -120);
            var cardLayout = cardRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            cardLayout.spacing = 20;
            cardLayout.childControlWidth = cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = cardLayout.childForceExpandHeight = true;
            var cards = new UnityEngine.UI.Button[3];
            var cardLabels = new TMP_Text[3];
            var cardTitles = new TMP_Text[3];
            var cardRanks = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                cards[i] = Button("Upgrade" + i, cardRow);
                cardTitles[i] = Label("Title", cards[i].transform, new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(300, 90), "Upgrade", 28);
                cardLabels[i] = Label("Description", cards[i].transform, new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(300, 170), "", 24, TextAlignmentOptions.Top);
                cardRanks[i] = Label("Rank", cards[i].transform, new Vector2(0.5f, 0), new Vector2(0, 28), new Vector2(300, 36), "", 24);
            }
            SetArray(ui, "_cards", cards);
            SetArray(ui, "_cardLabels", cardLabels);
            SetArray(ui, "_cardTitleLabels", cardTitles);
            SetArray(ui, "_cardRankLabels", cardRanks);
            Set(ui, "_notice", Label("SaveNotice", safe, new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(1000, 28), "", 20));
            SavePrefab(canvasObject, "CanvasGameplay");
            return ui;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = min == max ? min : Vector2.one * 0.5f;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static TMP_Text Label(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, string text, float fontSize, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var rect = Rect(name, parent, anchor, anchor, position, size);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = _font;
            label.text = text;
            label.fontSize = fontSize;
            label.color = new Color(0.9f, 0.94f, 1);
            label.alignment = alignment;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        private static TMP_Text StretchLabel(string name, Transform parent, string text, float fontSize)
        {
            var label = Label(name, parent, Vector2.one * 0.5f, Vector2.zero, Vector2.zero, text, fontSize);
            var rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(6, 4);
            rect.offsetMax = new Vector2(-6, -4);
            return label;
        }

        private static UnityEngine.UI.Button Button(string name, Transform parent)
        {
            var rect = Rect(name, parent, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(400, 56));
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.13f, 0.23f, 0.34f);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.55f, 0.9f, 1);
            colors.selectedColor = new Color(0.55f, 0.9f, 1);
            button.colors = colors;
            return button;
        }

        private static UnityEngine.UI.Image Bar(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var rect = Rect(name, parent, anchor, anchor, position, size);
            var background = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            background.color = _panel;
            background.raycastTarget = false;
            return Fill("Fill", rect, color);
        }

        private static UnityEngine.UI.Image Fill(string name, Transform parent, Color color)
        {
            var rect = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = _square;
            image.type = UnityEngine.UI.Image.Type.Filled;
            image.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Transform Child(string name, Transform parent)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static SpriteRenderer Visual(string name, Transform parent, Color color)
        {
            var renderer = Child(name, parent).gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = _square;
            renderer.color = color;
            renderer.sortingOrder = 2;
            return renderer;
        }

        private static GameObject SavePrefab(GameObject instance, string name)
        {
            return PrefabUtility.SaveAsPrefabAsset(instance, $"Assets/Prefab/{name}.prefab");
        }

        private static void Set(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray<T>(Object target, string property, T[] values) where T : Object
        {
            var serialized = new SerializedObject(target);
            var array = serialized.FindProperty(property);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
