using UnityEditor;
using UnityEngine;

namespace BattlePass.UI
{
    /// <summary>
    /// Custom editor for PlayerProfileSO to add a Reset to Defaults button directly in the Inspector.
    /// </summary>
    [CustomEditor(typeof(PlayerProfileSO))]
    public class PlayerProfileSOEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            InspectorBanner.Draw("Player Profile SO", new Color(0.12f, 0.6f, 0.3f));

            DrawDefaultInspector();

            PlayerProfileSO profile = (PlayerProfileSO)target;

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Reset to Defaults", GUILayout.Height(30)))
            {
                Undo.RecordObject(profile, "Reset Player Profile");
                profile.ResetToDefaults();
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
                Debug.Log("[PlayerProfileSO] Reset player profile to defaults.");
            }
        }
    }
}
