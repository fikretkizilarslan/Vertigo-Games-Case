using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Audits project textures for mobile compression settings and auto-fixes issues.
///
/// Detected problems:
///   â€¢ No ASTC format override for Android or iOS
///   â€¢ Max size > 1024 on a UI sprite (512â€“1024 is sufficient for mobile)
///   â€¢ Mipmaps disabled on a 3D texture (causes aliasing at distance)
///   â€¢ Mipmaps enabled on a UI sprite (wastes GPU memory â€” UI is always same-size)
///   â€¢ Read/Write enabled when not required (doubles CPU-side memory footprint)
///
/// Menu: PerformanceTools â†’ Performance â†’ Texture Audit (ASTC)
/// Auto-Fix applies ASTC 6Ã—6 + optimal settings for Android and iOS.
/// </summary>
public sealed class PerformanceToolsTextureAuditor : EditorWindow
{
    private enum IssueType { NoAstcAndroid, NoAstcIos, OversizeMax, MipmapOffOn3D, MipmapOnUi, ReadWriteOn }

    private sealed class TextureIssue
    {
        public string          Path;
        public string          Name;
        public List<IssueType> Issues = new List<IssueType>(2);
    }

    private Vector2            _scroll;
    private List<TextureIssue> _issues      = new List<TextureIssue>();
    private bool               _scanned;
    private bool               _onlyProject = true;

    [MenuItem("Performance Tools/Performance/Texture Audit (ASTC)")]
    private static void Open() => GetWindow<PerformanceToolsTextureAuditor>("Texture Audit");

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "ASTC is the required GPU format for Android and iOS.\n" +
            "ASTC 6Ã—6 offers the best size/quality balance for mobile games.\n" +
            "Textures with Read/Write enabled hold a second CPU-side copy (doubles RAM usage).",
            MessageType.Info);

        _onlyProject = EditorGUILayout.Toggle("Search _Project folder only", _onlyProject);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan")) Scan();
        GUI.enabled = _scanned && _issues.Count > 0;
        if (GUILayout.Button($"Auto-Fix All ({_issues.Count} issues)")) AutoFixAll();
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        if (!_scanned) return;

        if (_issues.Count == 0)
        {
            EditorGUILayout.HelpBox("âœ“ All textures are optimally configured for mobile!", MessageType.None);
            return;
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var issue in _issues)
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            var tex = AssetDatabase.LoadAssetAtPath<Texture>(issue.Path);
            EditorGUILayout.ObjectField(tex, typeof(Texture), false, GUILayout.Width(180));
            EditorGUILayout.BeginVertical();
            foreach (var t in issue.Issues)
                EditorGUILayout.LabelField(IssueLabel(t), EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            if (GUILayout.Button("Fix", GUILayout.Width(40)))
                FixTexture(issue.Path);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    private void Scan()
    {
        _issues.Clear();
        _scanned = true;

        string searchRoot = _onlyProject ? "Assets/_Project" : "Assets";
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { searchRoot });

        foreach (var guid in guids)
        {
            var path     = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            var issue = new TextureIssue { Path = path, Name = Path.GetFileNameWithoutExtension(path) };

            bool isUiSprite = importer.textureType == TextureImporterType.Sprite
                              && importer.spriteImportMode != SpriteImportMode.None;
            bool is3D = importer.textureType == TextureImporterType.Default
                        || importer.textureType == TextureImporterType.NormalMap;

            // ASTC â€” Android
            var androidSettings = importer.GetPlatformTextureSettings("Android");
            if (!androidSettings.overridden || !IsAstc(androidSettings.format))
                issue.Issues.Add(IssueType.NoAstcAndroid);

            // ASTC â€” iOS
            var iosSettings = importer.GetPlatformTextureSettings("iPhone");
            if (!iosSettings.overridden || !IsAstc(iosSettings.format))
                issue.Issues.Add(IssueType.NoAstcIos);

            if (importer.maxTextureSize > 1024 && isUiSprite)
                issue.Issues.Add(IssueType.OversizeMax);

            if (is3D && !importer.mipmapEnabled)
                issue.Issues.Add(IssueType.MipmapOffOn3D);

            if (isUiSprite && importer.mipmapEnabled)
                issue.Issues.Add(IssueType.MipmapOnUi);

            if (importer.isReadable)
                issue.Issues.Add(IssueType.ReadWriteOn);

            if (issue.Issues.Count > 0)
                _issues.Add(issue);
        }

        Debug.Log($"[TextureAudit] Scan complete â€” {_issues.Count} texture(s) with issues.");
        Repaint();
    }

    private void AutoFixAll()
    {
        foreach (var issue in _issues)
            FixTexture(issue.Path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        _issues.Clear();
        Scan();
    }

    private static void FixTexture(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        bool isUiSprite = importer.textureType == TextureImporterType.Sprite;

        var android = importer.GetPlatformTextureSettings("Android");
        android.overridden         = true;
        android.maxTextureSize     = isUiSprite ? 1024 : Mathf.Min(importer.maxTextureSize, 2048);
        android.format             = TextureImporterFormat.ASTC_6x6;
        android.compressionQuality = 50;
        importer.SetPlatformTextureSettings(android);

        var ios = importer.GetPlatformTextureSettings("iPhone");
        ios.overridden         = true;
        ios.maxTextureSize     = android.maxTextureSize;
        ios.format             = TextureImporterFormat.ASTC_6x6;
        ios.compressionQuality = 50;
        importer.SetPlatformTextureSettings(ios);

        if (isUiSprite)
            importer.mipmapEnabled = false;

        if (importer.isReadable)
            importer.isReadable = false;

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    private static bool IsAstc(TextureImporterFormat f) =>
        f == TextureImporterFormat.ASTC_4x4  || f == TextureImporterFormat.ASTC_5x5  ||
        f == TextureImporterFormat.ASTC_6x6  || f == TextureImporterFormat.ASTC_8x8  ||
        f == TextureImporterFormat.ASTC_10x10|| f == TextureImporterFormat.ASTC_12x12;

    private static string IssueLabel(IssueType t) => t switch
    {
        IssueType.NoAstcAndroid  => "âš  Android: no ASTC format override",
        IssueType.NoAstcIos      => "âš  iOS: no ASTC format override",
        IssueType.OversizeMax    => "âš  UI sprite max size > 1024",
        IssueType.MipmapOffOn3D  => "âš  3D texture has mipmaps disabled",
        IssueType.MipmapOnUi     => "âš  UI sprite has mipmaps enabled (wastes GPU memory)",
        IssueType.ReadWriteOn    => "âš  Read/Write enabled (doubles CPU RAM usage)",
        _                        => t.ToString()
    };
}

