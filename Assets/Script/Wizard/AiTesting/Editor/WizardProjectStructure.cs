using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VonderGames.AiTesting.Editor
{
    public static class WizardProjectStructure
    {
        [MenuItem("Wizard/Organize project and scene structure")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Stop Play Mode before organizing the project.");

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scene/Initialize.unity" || scene.isDirty)
                throw new InvalidOperationException("Open and save Initialize before organizing the project.");

            MoveAsset("Assets/Scenes/SampleScene.unity", "Assets/Scene/SampleScene.unity");
            if (AssetDatabase.IsValidFolder("Assets/Scenes") &&
                System.IO.Directory.GetFileSystemEntries("Assets/Scenes").Length == 0)
                AssetDatabase.DeleteAsset("Assets/Scenes");

            const string uiPath = "Assets/Prefab/UI/CanvasScreenSpace.prefab";
            MoveAsset("Assets/Prefab/UI/CanvasGameplay.prefab", uiPath);
            GameObject prefab = PrefabUtility.LoadPrefabContents(uiPath);
            try
            {
                prefab.name = "CanvasScreenSpace";
                PrefabUtility.SaveAsPrefabAsset(prefab, uiPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }

            Transform level = scene.GetRootGameObjects().Single(root => root.name == "Level").transform;
            GroupChildren(level.Find("Environment"), "Grid", "GridVertical", "GridHorizontal");
            GroupChildren(level.Find("Collision"), "Wall", "BoundaryWall");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void MoveAsset(string source, string destination)
        {
            if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(source)))
                return;

            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(destination)))
                throw new InvalidOperationException($"Cannot move {source}: {destination} already exists.");

            string error = AssetDatabase.MoveAsset(source, destination);
            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException(error);
        }

        private static void GroupChildren(Transform parent, string name, params string[] childNames)
        {
            Transform group = parent.Find(name);
            if (group == null)
            {
                var gameObject = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(gameObject, "Create hierarchy group");
                group = gameObject.transform;
                group.SetParent(parent, false);
            }

            Transform[] children = parent.Cast<Transform>().Where(child => childNames.Contains(child.name)).ToArray();
            foreach (Transform child in children)
                Undo.SetTransformParent(child, group, "Organize scene hierarchy");
        }
    }
}
