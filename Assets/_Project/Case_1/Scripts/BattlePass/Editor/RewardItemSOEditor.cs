using UnityEditor;
using UnityEngine;

namespace VertigoCase.UI
{
    [CustomEditor(typeof(RewardItemSO))]
    [CanEditMultipleObjects]
    public class RewardItemSOEditor : Editor
    {
        private SerializedProperty displayName;
        private SerializedProperty icon;
        private SerializedProperty currencyIcon;

        private SerializedProperty rewardType;
        private SerializedProperty rarity;
        private SerializedProperty isUnique;
        private SerializedProperty showInKeyRewardIndicator;

        private SerializedProperty excludeFromBattlePass;
        private SerializedProperty canAppearInFreeTrack;
        private SerializedProperty canAppearInPremiumTrack;



        private SerializedProperty distributeFixedTotal;
        private SerializedProperty fixedTotalAmount;

        private void OnEnable()
        {
            displayName = serializedObject.FindProperty("displayName");
            icon = serializedObject.FindProperty("icon");
            currencyIcon = serializedObject.FindProperty("currencyIcon");

            rewardType = serializedObject.FindProperty("rewardType");
            rarity = serializedObject.FindProperty("rarity");
            isUnique = serializedObject.FindProperty("isUnique");
            showInKeyRewardIndicator = serializedObject.FindProperty("showInKeyRewardIndicator");

            excludeFromBattlePass = serializedObject.FindProperty("excludeFromBattlePass");
            canAppearInFreeTrack = serializedObject.FindProperty("canAppearInFreeTrack");
            canAppearInPremiumTrack = serializedObject.FindProperty("canAppearInPremiumTrack");



            distributeFixedTotal = serializedObject.FindProperty("distributeFixedTotal");
            fixedTotalAmount = serializedObject.FindProperty("fixedTotalAmount");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Visual Information Section
            DrawHeader("Visual Information");
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(displayName);
            EditorGUILayout.PropertyField(icon);
            EditorGUILayout.PropertyField(currencyIcon);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Classification Section
            DrawHeader("Classification");
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(rewardType);
            EditorGUILayout.PropertyField(rarity);
            EditorGUILayout.PropertyField(isUnique);
            EditorGUILayout.PropertyField(showInKeyRewardIndicator);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Battle Pass Pool Rules Section
            DrawHeader("Battle Pass Pool Rules");
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(excludeFromBattlePass);
            if (!excludeFromBattlePass.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(canAppearInFreeTrack);
                EditorGUILayout.PropertyField(canAppearInPremiumTrack);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);



            // Distribution Constraints Section
            DrawHeader("Distribution Constraints");
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(distributeFixedTotal);
            if (distributeFixedTotal.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(fixedTotalAmount);
                if (fixedTotalAmount.intValue < 1) fixedTotalAmount.intValue = 1;
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader(string label)
        {
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
            style.fontSize = 11;
            style.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.3f, 0.8f, 1f) : new Color(0.1f, 0.4f, 0.7f);
            EditorGUILayout.LabelField(label, style);
        }
    }
}
