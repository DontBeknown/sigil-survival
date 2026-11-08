using UnityEditor;
using UnityEngine;
using VonderGames.AiTesting.Gameplay.Instance;

namespace VonderGames.AiTesting.Editor
{
    [CustomEditor(typeof(GameplayInstance))]
    public sealed class GameplayInstanceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var game = (GameplayInstance)target;
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!Application.isPlaying || !game.IsCombatActive || game.HasBossSpawned))
            {
                if (GUILayout.Button("Skip to Boss"))
                    game.SkipToBoss();
            }
            EditorGUILayout.HelpBox("Play Mode only. Advances to the Warden without changing health or upgrades.", MessageType.Info);
        }
    }
}
