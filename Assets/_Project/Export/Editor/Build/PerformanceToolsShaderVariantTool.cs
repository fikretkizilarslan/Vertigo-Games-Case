using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Shader variant analyzer and ShaderVariantCollection generator.
///
/// Problem: URP compiles every keyword combination used by every material as a separate
/// shader "variant". Unused variants:
///   â€¢ Inflate build size
///   â€¢ Extend build time
///   â€¢ May cause a compile spike on first load
///
/// This tool:
///   1. Scans all materials under Assets/_Project
///   2. Shows how many unique keyword combinations each shader uses
///   3. Flags shaders with a high variant count
///   4. Generates a ShaderVariantCollection for Unity's shader prewarming system
///
/// Menu: PerformanceTools â†’ Build â†’ Shader Variant Analyzer
/// </summary>
public sealed class PerformanceToolsShaderVariantTool : EditorWindow
{
    private sealed class ShaderStats
    {
        public Shader         Shader;
        public int            VariantCount;
        public List<string>   ActiveKeywordSets = new List<string>();
        public List<Material> Materials         = new List<Material>();
    }

    private Vector2           _scroll;
    private List<ShaderStats> _stats   = new List<ShaderStats>();
    private bool              _scanned;

    [MenuItem("Performance Tools/Build/Shader Variant Analyzer")]
    private static void Open() => GetWindow<PerformanceToolsShaderVariantTool>("Shader Variants");

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Each unique keyword combination = 1 shader variant.\n" +
            "1 000+ variants is normal for URP projects, but unused ones inflate the build.\n" +
            "This tool lists all active combinations and can generate a ShaderVariantCollection for prewarming.",
            MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan Materials")) Scan();
        GUI.enabled = _scanned && _stats.Count > 0;
        if (GUILayout.Button("Create ShaderVariantCollection"))
            CreateVariantCollection();
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        if (!_scanned) return;

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        int totalVariants = _stats.Sum(s => s.VariantCount);
        EditorGUILayout.LabelField($"Total unique shader-keyword combinations: {totalVariants}", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        foreach (var s in _stats.OrderByDescending(x => x.VariantCount))
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.ObjectField(s.Shader, typeof(Shader), false);
            EditorGUILayout.LabelField(
                $"Variants: {s.VariantCount}  â€¢  Materials: {s.Materials.Count}",
                EditorStyles.miniLabel);

            if (s.VariantCount > 10)
                EditorGUILayout.LabelField(
                    "âš  High variant count â€” consider keyword stripping via MaterialKeywordStripper",
                    EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(1);
        }

        EditorGUILayout.EndScrollView();
    }

    private void Scan()
    {
        _stats.Clear();
        _scanned = true;

        var matGuids  = AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Project" });
        var shaderMap = new Dictionary<Shader, ShaderStats>();

        foreach (var guid in matGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat  = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == null) continue;

            if (!shaderMap.TryGetValue(mat.shader, out var ss))
            {
                ss = new ShaderStats { Shader = mat.shader };
                shaderMap[mat.shader] = ss;
                _stats.Add(ss);
            }

            ss.Materials.Add(mat);

            var kwds = mat.shaderKeywords;
            System.Array.Sort(kwds);
            string combo = kwds.Length == 0 ? "<no keywords>" : string.Join("|", kwds);
            if (!ss.ActiveKeywordSets.Contains(combo))
            {
                ss.ActiveKeywordSets.Add(combo);
                ss.VariantCount++;
            }
        }

        Debug.Log($"[ShaderVariantTool] Scanned {matGuids.Length} materials across {_stats.Count} shaders.");
        Repaint();
    }

    private void CreateVariantCollection()
    {
        var collection = new ShaderVariantCollection();

        foreach (var ss in _stats)
        {
            foreach (var combo in ss.ActiveKeywordSets)
            {
                string[] kwds = combo == "<no keywords>"
                    ? System.Array.Empty<string>()
                    : combo.Split('|');

                try
                {
                    var entry = new ShaderVariantCollection.ShaderVariant(
                        ss.Shader, PassType.ScriptableRenderPipeline, kwds);
                    collection.Add(entry);
                }
                catch { /* keyword combo not valid for this shader â€” skip */ }
            }
        }

        string savePath = "Assets/_Project/Settings/PerformanceTools_ShaderVariants.shadervariants";
        Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
        AssetDatabase.CreateAsset(collection, savePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = collection;
        Debug.Log($"[ShaderVariantTool] Created '{savePath}' with {collection.shaderCount} shader(s).");
        EditorUtility.DisplayDialog("Done",
            $"ShaderVariantCollection saved to:\n{savePath}\n\n" +
            "Add it to Edit â†’ Project Settings â†’ Graphics â†’ Preloaded Shaders\n" +
            "to prewarm shaders before the first frame.",
            "OK");
    }
}

