using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runtime batch analysis tool. Open via Tools/Paxie/Batch Diagnostic.
/// Shows exactly which renderers have MaterialPropertyBlocks set (breaking GPU Instancing)
/// and which materials are not shared across multiple renderers.
/// </summary>
public sealed class BatchDiagnosticWindow : EditorWindow
{
    private Vector2 _scroll;
    private string  _report;
    private bool    _autoRefresh;

    [MenuItem("Performance Tools/Diagnostics/Batch Diagnostic")]
    private static void Open() => GetWindow<BatchDiagnosticWindow>("Batch Diagnostic");

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Run this in Play Mode to diagnose why GPU Instancing / batching isn't reducing batch counts.",
            MessageType.Info);

        _autoRefresh = EditorGUILayout.Toggle("Auto Refresh (every repaint)", _autoRefresh);

        if (GUILayout.Button("Scan Now") || (_autoRefresh && Application.isPlaying))
            _report = BuildReport();

        if (!string.IsNullOrEmpty(_report))
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.TextArea(_report, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        if (_autoRefresh) Repaint();
    }

    private static string BuildReport()
    {
        if (!Application.isPlaying)
            return "Enter Play Mode first.";

        var allRenderers = new List<Renderer>(512);
        for (int si = 0; si < SceneManager.sceneCount; si++)
        {
            var sc = SceneManager.GetSceneAt(si);
            if (!sc.isLoaded) continue;
            foreach (var root in sc.GetRootGameObjects())
                allRenderers.AddRange(root.GetComponentsInChildren<Renderer>(true));
        }

        int totalRenderers   = allRenderers.Count;
        int withMPB          = 0;
        int withoutInstancing = 0;
        int activeRenderers  = 0;

        // Material â†’ renderer list (for instancing potential analysis)
        var matToRenderers = new Dictionary<Material, List<string>>(128);

        foreach (var r in allRenderers)
        {
            if (r == null || !r.gameObject.activeInHierarchy || !r.enabled) continue;
            activeRenderers++;

            if (r.HasPropertyBlock())
                withMPB++;

            foreach (var mat in r.sharedMaterials)
            {
                if (mat == null) continue;
                if (!mat.enableInstancing)
                    withoutInstancing++;
                if (!matToRenderers.TryGetValue(mat, out var lst))
                    matToRenderers[mat] = lst = new List<string>(4);
                lst.Add(r.gameObject.name);
            }
        }

        // Find materials used by only 1 renderer (can't batch anyway)
        int singletonMats   = 0;
        int instancableMats = 0;
        int estimatedSaved  = 0;
        var topWaste = new List<(int saves, string matName, int count)>();

        foreach (var kv in matToRenderers)
        {
            var mat    = kv.Key;
            string matName = mat != null ? mat.name : "unknown";
            int count = kv.Value.Count;
            if (count <= 1) { singletonMats++; continue; }

            if (mat != null && mat.enableInstancing)
            {
                instancableMats++;
                estimatedSaved += count - 1;
                if (count > 3)
                    topWaste.Add((count - 1, matName, count));
            }
        }

        topWaste.Sort((a, b) => b.saves.CompareTo(a.saves));

        var sb = new StringBuilder(1024);
        sb.AppendLine("â•â•â•â•â•â•â•â•â•â• Batch Diagnostic â•â•â•â•â•â•â•â•â•â•");
        sb.AppendLine($"Total renderers      : {totalRenderers}");
        sb.AppendLine($"Active renderers      : {activeRenderers}");
        sb.AppendLine($"With PropertyBlock    : {withMPB}  â† BREAKS GPU Instancing & SRP Batcher");
        sb.AppendLine($"enableInstancing=OFF  : {withoutInstancing}  â† GPU Instancing inactive for these");
        sb.AppendLine($"Unique shared mats    : {matToRenderers.Count}");
        sb.AppendLine($"Singleton mats (Ã—1)   : {singletonMats}  (can't batch; unique textures/settings)");
        sb.AppendLine($"Instancable mats (Ã—2+): {instancableMats}");
        sb.AppendLine($"Estimated saves if GPU instancing fixed: ~{estimatedSaved} draw calls");
        sb.AppendLine();

        if (withMPB > 0)
        {
            sb.AppendLine("âš  TOP MaterialPropertyBlock HOLDERS (first 20):");
            int shown = 0;
            foreach (var r in allRenderers)
            {
                if (r == null || !r.gameObject.activeInHierarchy) continue;
                if (!r.HasPropertyBlock()) continue;
                sb.AppendLine($"  {r.gameObject.name}  [{r.GetType().Name}]");
                if (++shown >= 20) { sb.AppendLine("  ... (truncated)"); break; }
            }
            sb.AppendLine();
        }

        if (topWaste.Count > 0)
        {
            sb.AppendLine("âœ“ TOP INSTANCABLE MATERIALS (most potential savings):");
            int shown = 0;
            foreach (var (saves, matName, count) in topWaste)
            {
                sb.AppendLine($"  -{saves} draws  {matName}  ({count} renderers)");
                if (++shown >= 15) break;
            }
        }

        return sb.ToString();
    }
}

