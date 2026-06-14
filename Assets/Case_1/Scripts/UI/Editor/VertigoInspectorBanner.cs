using UnityEditor;
using UnityEngine;

namespace VertigoCase.UI
{
    /// <summary>
    /// Shared editor helper that paints a coloured title banner at the very top of an inspector,
    /// giving the project's critical scripts a consistent, custom-branded header strip.
    /// </summary>
    public static class VertigoInspectorBanner
    {
        /// <summary>
        /// Draws a full-width coloured banner with a bold, upper-cased title.
        /// </summary>
        /// <param name="title">Banner caption (shown upper-cased).</param>
        /// <param name="background">Banner fill colour.</param>
        public static void Draw(string title, Color background)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 30f);

            EditorGUI.DrawRect(rect, background);

            // Darker base strip for a little depth.
            Rect underline = new Rect(rect.x, rect.yMax - 3f, rect.width, 3f);
            EditorGUI.DrawRect(underline, background * 0.55f);

            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 13,
                padding = new RectOffset(12, 12, 0, 0)
            };
            style.normal.textColor = Color.white;

            GUI.Label(rect, title.ToUpperInvariant(), style);
            EditorGUILayout.Space(4f);
        }
    }

    /// <summary>
    /// Adds the branded banner to the <see cref="BattlePassNode"/> inspector.
    /// </summary>
    [CustomEditor(typeof(BattlePassNode))]
    public class BattlePassNodeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            VertigoInspectorBanner.Draw("Battle Pass Node", new Color(0.85f, 0.42f, 0.12f));
            DrawDefaultInspector();
        }
    }

    /// <summary>
    /// Adds the branded banner to the <see cref="UILevelSkipButton"/> inspector.
    /// </summary>
    [CustomEditor(typeof(UILevelSkipButton))]
    public class UILevelSkipButtonEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            VertigoInspectorBanner.Draw("Level Skip Button", new Color(0.16f, 0.55f, 0.27f));
            DrawDefaultInspector();
        }
    }
}
