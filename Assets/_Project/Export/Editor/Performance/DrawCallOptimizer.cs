using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class DrawCallOptimizer
{
    [MenuItem("Performance Tools/Performance/Optimize Draw Calls NOW")]
    public static void OptimizeScene()
    {
        bool changed = false;

        // 1. GPU Instancing
        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Project" });
        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && !mat.enableInstancing)
            {
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
                changed = true;
            }
        }

        // 2. Static Batching for Environment + Record Prefab Overrides
        Scene activeScene = EditorSceneManager.GetActiveScene();
        GameObject envFolder = GameObject.Find("--- ENVIRONMENT ---");
        if (envFolder != null)
        {
            foreach (Transform child in envFolder.GetComponentsInChildren<Transform>(true))
            {
                if (child.gameObject == envFolder) continue;
                
                var flags = GameObjectUtility.GetStaticEditorFlags(child.gameObject);
                if ((flags & StaticEditorFlags.BatchingStatic) == 0)
                {
                    flags |= StaticEditorFlags.BatchingStatic;
                    GameObjectUtility.SetStaticEditorFlags(child.gameObject, flags);
                    
                    if (PrefabUtility.IsPartOfPrefabInstance(child.gameObject))
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
                    }
                    changed = true;
                }
            }
        }

        // ENABLE SRP Batcher for Ultimate Mobile Performance
        var mobileRP = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        if (mobileRP != null && !mobileRP.useSRPBatcher)
        {
            mobileRP.useSRPBatcher = true;
            EditorUtility.SetDirty(mobileRP);
            changed = true;
        }
        var pcRP = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        if (pcRP != null && !pcRP.useSRPBatcher)
        {
            pcRP.useSRPBatcher = true;
            EditorUtility.SetDirty(pcRP);
            changed = true;
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            AssetDatabase.SaveAssets();
            Debug.Log("AAA Optimization Re-Applied (SRP Batcher ON, Instancing ON, Static Batching ON)!");
        }
        else
        {
            Debug.Log("No changes needed. Already fully optimized.");
        }
    }
}

