using UnityEditor;
using UnityEngine;

/// <summary>
/// Tek tÄ±kla projedeki tÃ¼m Material asset'lerinde 'Enable GPU Instancing' bayraÄŸÄ±nÄ±
/// aÃ§ar ve deÄŸiÅŸikliÄŸi diske yazar. Cube material'Ä± iÃ§in kritik:
/// AttackReactiveCube MaterialPropertyBlock kullanÄ±yor â†’ SRP Batcher devre dÄ±ÅŸÄ± kalÄ±yor â†’
/// batching tek yoldan geliyor (GPU Instancing). Bu flag aÃ§Ä±k deÄŸilse her kÃ¼p ayrÄ± draw call.
/// </summary>
public static class ForceEnableGpuInstancingTool
{
    [MenuItem("Performance Tools/Performance/Enable GPU Instancing On All Materials")]
    public static void EnableOnAll()
    {
        var guids = AssetDatabase.FindAssets("t:Material");
        int changed = 0;
        int total = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path)) continue;
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;
                total++;
                if (mat.enableInstancing) continue;
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
                changed++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"[ForceEnableGpuInstancing] Updated {changed}/{total} materials. " +
                  "Save the project (Ctrl+S) to commit.");
    }

    [MenuItem("Performance Tools/Performance/Enable GPU Instancing On Selected")]
    public static void EnableOnSelected()
    {
        var sel = Selection.objects;
        if (sel == null || sel.Length == 0)
        {
            Debug.LogWarning("[ForceEnableGpuInstancing] Select Material asset(s) in Project window first.");
            return;
        }

        int changed = 0;
        int seen = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < sel.Length; i++)
            {
                if (sel[i] is Material mat)
                {
                    seen++;
                    if (mat.enableInstancing) continue;
                    mat.enableInstancing = true;
                    EditorUtility.SetDirty(mat);
                    changed++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"[ForceEnableGpuInstancing] Updated {changed}/{seen} selected materials.");
    }
}

