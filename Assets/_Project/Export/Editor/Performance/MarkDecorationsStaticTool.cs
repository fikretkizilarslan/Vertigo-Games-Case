using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Auto-marks scene-authored decorative MeshRenderers as <see cref="StaticEditorFlags.BatchingStatic"/>
/// so Unity's build-time static batching consolidates them into shared meshes â€” fewer draw calls
/// without touching gameplay code.
///
/// Skipped (treated as dynamic):
///   - Cubes (AttackReactiveCube / CubeGridPlacer)
///   - Ducks (DuckJump)
///   - Anything with an <see cref="Animator"/> on the GameObject or any ancestor
///   - Rigidbodies (any kind)
///   - Particle systems and ParticleSystemRenderer
///   - SkinnedMeshRenderers
///   - LODGroups (Unity batches their children explicitly)
///
/// Run from menu after opening the scene whose decorations you want batched. Re-run it after
/// adding new decoration GameObjects.
/// </summary>
public static class MarkDecorationsStaticTool
{
    private const string MenuPath = "Performance Tools/Performance/Mark Scene Decorations As Batching-Static";
    private const string MenuPathClear = "Performance Tools/Performance/Clear Batching-Static From Active Scene";

    [MenuItem(MenuPath)]
    public static void MarkDecorations()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("Mark Decorations Static",
                "No valid active scene. Open the scene first.", "OK");
            return;
        }

        var renderers = CollectMeshRenderers(scene);
        int markedCount = 0;
        int alreadyMarked = 0;
        int skippedDynamic = 0;
        var rootsAffected = new HashSet<string>();

        foreach (var rend in renderers)
        {
            if (rend == null) continue;
            if (IsDynamic(rend))
            {
                skippedDynamic++;
                continue;
            }

            var go = rend.gameObject;
            var flags = GameObjectUtility.GetStaticEditorFlags(go);
            if ((flags & StaticEditorFlags.BatchingStatic) != 0)
            {
                alreadyMarked++;
                continue;
            }

            Undo.RecordObject(go, "Mark Decoration Batching-Static");
            GameObjectUtility.SetStaticEditorFlags(go, flags | StaticEditorFlags.BatchingStatic);
            markedCount++;
            rootsAffected.Add(go.transform.root.name);
            EditorUtility.SetDirty(go);
        }

        if (markedCount > 0)
            EditorSceneManager.MarkSceneDirty(scene);

        var sb = new System.Text.StringBuilder(256);
        sb.Append("[MarkDecorationsStatic] ");
        sb.Append("marked=").Append(markedCount);
        sb.Append(" alreadyMarked=").Append(alreadyMarked);
        sb.Append(" skippedDynamic=").Append(skippedDynamic);
        sb.Append(" rootsAffected=").Append(rootsAffected.Count);
        if (rootsAffected.Count > 0)
        {
            sb.Append(" (");
            int n = 0;
            foreach (var r in rootsAffected)
            {
                if (n > 0) sb.Append(", ");
                if (n >= 8) { sb.Append("..."); break; }
                sb.Append(r);
                n++;
            }
            sb.Append(')');
        }
        Debug.Log(sb.ToString());

        EditorUtility.DisplayDialog("Mark Decorations Static",
            $"Marked {markedCount} renderer(s) as BatchingStatic.\n" +
            $"Already marked: {alreadyMarked}. Skipped (dynamic): {skippedDynamic}.\n\n" +
            "Save the scene (Ctrl+S) and rebuild Play mode to see batch-count drop.",
            "OK");
    }

    [MenuItem(MenuPathClear)]
    public static void ClearBatchingStaticFromActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid()) return;

        var renderers = CollectMeshRenderers(scene);
        int cleared = 0;
        foreach (var rend in renderers)
        {
            if (rend == null) continue;
            var go = rend.gameObject;
            var flags = GameObjectUtility.GetStaticEditorFlags(go);
            if ((flags & StaticEditorFlags.BatchingStatic) == 0) continue;
            Undo.RecordObject(go, "Clear BatchingStatic");
            GameObjectUtility.SetStaticEditorFlags(go, flags & ~StaticEditorFlags.BatchingStatic);
            EditorUtility.SetDirty(go);
            cleared++;
        }

        if (cleared > 0)
            EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log($"[MarkDecorationsStatic] Cleared BatchingStatic from {cleared} renderer(s) in '{scene.name}'.");
    }

    private static List<MeshRenderer> CollectMeshRenderers(Scene scene)
    {
        var list = new List<MeshRenderer>(256);
        var roots = scene.GetRootGameObjects();
        var buf = new List<MeshRenderer>(64);
        for (int i = 0; i < roots.Length; i++)
        {
            var root = roots[i];
            if (root == null) continue;
            buf.Clear();
            root.GetComponentsInChildren(true, buf);
            list.AddRange(buf);
        }
        return list;
    }

    private static bool IsDynamic(MeshRenderer rend)
    {
        if (rend == null) return true;

        // ParticleSystemRenderer derives from Renderer, not MeshRenderer â€” already filtered.
        // SkinnedMeshRenderer derives from Renderer â€” also filtered.
        if (rend.gameObject.GetComponent<ParticleSystem>() != null) return true;

        // Any animator anywhere up the chain disqualifies (Animator drives transform).
        if (rend.GetComponentInParent<Animator>(true) != null) return true;

        // LODGroup parents handle their own batching path.
        if (rend.GetComponentInParent<LODGroup>(true) != null) return true;

        // Rigidbody = potentially physics-driven. Skip.
        if (rend.GetComponentInParent<Rigidbody>(true) != null) return true;

        // Project-specific dynamic markers: ducks, cubes (destroyable / movable).
        if (HasComponentInParent(rend.transform, "DuckJump")) return true;
        if (HasComponentInParent(rend.transform, "AttackReactiveCube")) return true;
        if (HasComponentInParent(rend.transform, "CubeGridPlacer")) return true;

        // Anything inside a Canvas is UI; not relevant for static mesh batching.
        if (rend.GetComponentInParent<Canvas>(true) != null) return true;

        return false;
    }

    /// <summary>
    /// Looks up the chain for a component by simple type name. Avoids hard references to
    /// project assemblies in case they get moved.
    /// </summary>
    private static bool HasComponentInParent(Transform t, string typeName)
    {
        while (t != null)
        {
            var comps = t.GetComponents<MonoBehaviour>();
            for (int i = 0; i < comps.Length; i++)
            {
                var c = comps[i];
                if (c == null) continue;
                if (c.GetType().Name == typeName) return true;
            }
            t = t.parent;
        }
        return false;
    }
}

