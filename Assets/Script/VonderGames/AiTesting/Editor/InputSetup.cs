using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;

namespace VonderGames.AiTesting.Editor
{
    public static class InputSetup
    {
        public static void Create()
        {
            Directory.CreateDirectory("Assets/InputSystem");
            Directory.CreateDirectory("Assets/Script/VonderGames/AiTesting/Core/InputSystem");
            const string path = "Assets/InputSystem/InputActions.inputactions";
            AssetDatabase.Refresh();
            if (AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions") != null)
                AssetDatabase.MoveAsset("Assets/InputSystem_Actions.inputactions", path);

            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "InputActions";
            var player = asset.AddActionMap("Player");
            var movement = player.AddAction("Movement", InputActionType.Value, expectedControlLayout: "Vector2");
            movement.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            movement.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            player.AddAction("Aim", InputActionType.Value, "<Mouse>/position", expectedControlLayout: "Vector2");
            player.AddAction("Bolt", InputActionType.Button, "<Mouse>/leftButton");
            player.AddAction("Fireball", InputActionType.Button, "<Mouse>/rightButton");
            player.AddAction("Nova", InputActionType.Button, "<Keyboard>/space");
            var system = asset.AddActionMap("System");
            system.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            var ui = asset.AddActionMap("UI");
            var navigate = ui.AddAction("Navigate", InputActionType.Value, expectedControlLayout: "Vector2");
            navigate.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            ui.AddAction("Submit", InputActionType.Button, "<Keyboard>/enter");
            ui.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
            ui.AddAction("Point", InputActionType.PassThrough, "<Mouse>/position", expectedControlLayout: "Vector2");
            ui.AddAction("Click", InputActionType.PassThrough, "<Mouse>/leftButton");
            ui.AddAction("ScrollWheel", InputActionType.PassThrough, "<Mouse>/scroll", expectedControlLayout: "Vector2");
            ui.AddAction("Next", InputActionType.Button, "<Keyboard>/tab");
            for (int i = 1; i <= 3; i++)
                ui.AddAction("Choice" + i, InputActionType.Button, "<Keyboard>/digit" + i);

            File.WriteAllText(path, asset.ToJson());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var imported = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            InputActionCodeGenerator.GenerateWrapperCode("Assets/Script/VonderGames/AiTesting/Core/InputSystem/InputActions.cs", imported,
                new InputActionCodeGenerator.Options { className = "InputActions", namespaceName = "VonderGames.AiTesting.Core.InputSystem", sourceAssetPath = path });
            var importer = new SerializedObject(AssetImporter.GetAtPath(path));
            importer.FindProperty("m_GenerateWrapperCode").boolValue = true;
            importer.FindProperty("m_WrapperClassName").stringValue = "InputActions";
            importer.FindProperty("m_WrapperCodeNamespace").stringValue = "VonderGames.AiTesting.Core.InputSystem";
            importer.FindProperty("m_WrapperCodePath").stringValue = "Assets/Script/VonderGames/AiTesting/Core/InputSystem/InputActions.cs";
            importer.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.WriteImportSettingsIfDirty(path);
            UnityEngine.Object.DestroyImmediate(asset);
            AssetDatabase.Refresh();
            Debug.Log("Wizard input authored and wrapper generated.");
        }
    }
}
