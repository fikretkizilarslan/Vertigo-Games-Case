#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using BattlePass.UI;

/// <summary>
/// One-shot Editor utility that auto-wires missing Inspector references on
/// BattlePassManager and GemWalletController. Run once from the menu then delete.
/// </summary>
public static class AutoWireReferences
{
    [MenuItem("Tools/Battle Pass/Auto-Wire Missing References")]
    public static void Wire()
    {
        int wired = 0;

        // ── BattlePassManager ────────────────────────────────────────────────
        BattlePassManager manager = Object.FindFirstObjectByType<BattlePassManager>(FindObjectsInactive.Include);
        if (manager == null) { Debug.LogError("[AutoWire] BattlePassManager not found in scene."); return; }

        SerializedObject soManager = new SerializedObject(manager);

        // goldCountText
        if (soManager.FindProperty("goldCountText").objectReferenceValue == null)
        {
            GameObject goldGO = GameObject.Find("Txt_Count_Gold");
            if (goldGO != null)
            {
                soManager.FindProperty("goldCountText").objectReferenceValue = goldGO.GetComponent<TextMeshProUGUI>();
                Debug.Log("[AutoWire] Wired goldCountText → " + goldGO.name);
                wired++;
            }
            else Debug.LogWarning("[AutoWire] Txt_Count_Gold not found in scene.");
        }

        // gemCountText
        if (soManager.FindProperty("gemCountText").objectReferenceValue == null)
        {
            GameObject gemGO = GameObject.Find("Txt_Count_Gem");
            if (gemGO != null)
            {
                soManager.FindProperty("gemCountText").objectReferenceValue = gemGO.GetComponent<TextMeshProUGUI>();
                Debug.Log("[AutoWire] Wired gemCountText → " + gemGO.name);
                wired++;
            }
            else Debug.LogWarning("[AutoWire] Txt_Count_Gem not found in scene.");
        }

        soManager.ApplyModifiedProperties();

        // ── GemWalletController ──────────────────────────────────────────────
        GemWalletController gwc = Object.FindFirstObjectByType<GemWalletController>(FindObjectsInactive.Include);
        if (gwc == null) { Debug.LogWarning("[AutoWire] GemWalletController not found in scene — skipping gem wallet wiring."); }
        else
        {
            SerializedObject soGwc = new SerializedObject(gwc);

            // grpGem
            if (soGwc.FindProperty("grpGem").objectReferenceValue == null)
            {
                GameObject grpGemGO = GameObject.Find("Grp_Gem");
                // Also try inactive search
                if (grpGemGO == null)
                {
                    var all = Resources.FindObjectsOfTypeAll<GameObject>();
                    foreach (var go in all) { if (go.name == "Grp_Gem") { grpGemGO = go; break; } }
                }
                if (grpGemGO != null)
                {
                    soGwc.FindProperty("grpGem").objectReferenceValue = grpGemGO;
                    Debug.Log("[AutoWire] Wired grpGem → " + grpGemGO.name);
                    wired++;
                }
                else Debug.LogWarning("[AutoWire] Grp_Gem not found.");
            }

            // gemRevealButton
            if (soGwc.FindProperty("gemRevealButton").objectReferenceValue == null)
            {
                GameObject revealGO = GameObject.Find("Btn_Gem_Reveal");
                if (revealGO == null)
                {
                    var all = Resources.FindObjectsOfTypeAll<GameObject>();
                    foreach (var go in all) { if (go.name == "Btn_Gem_Reveal") { revealGO = go; break; } }
                }
                if (revealGO != null)
                {
                    soGwc.FindProperty("gemRevealButton").objectReferenceValue = revealGO.GetComponent<Button>();
                    Debug.Log("[AutoWire] Wired gemRevealButton → " + revealGO.name);
                    wired++;
                }
                else Debug.LogWarning("[AutoWire] Btn_Gem_Reveal not found — it may not exist in this scene.");
            }

            soGwc.ApplyModifiedProperties();
        }

        // Save the scene
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"[AutoWire] Done. {wired} reference(s) wired. Save the scene (Ctrl+S) to persist.");
    }
}
#endif
