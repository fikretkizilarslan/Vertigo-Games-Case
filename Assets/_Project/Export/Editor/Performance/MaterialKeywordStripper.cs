using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Strips shader keywords that are enabled on a material but have no effect
/// (e.g. _NORMALMAP enabled but no normal map texture assigned).
///
/// Each unique keyword combination = a unique compiled shader variant = a separate
/// SetPass call.  Removing unused keywords collapses materials into fewer variants
/// and directly reduces SetPass without changing draw call counts or visual output.
///
/// Safe to run any time. The URP material inspector re-validates keywords when you
/// open/save a material, so run this tool again if you change materials later.
///
/// Menu: Paxie â†’ Performance â†’ Strip Unused Material Keywords
/// </summary>
public static class MaterialKeywordStripper
{
    [MenuItem("Performance Tools/Performance/Strip Unused Material Keywords")]
    public static void StripAll()
    {
        var guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Project" });
        int stripped = 0, skipped = 0, total = 0;
        var log = new StringBuilder(512);

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat  = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { skipped++; continue; }
            total++;

            var removedKw = new List<string>(4);
            StripURP(mat, removedKw);

            if (removedKw.Count > 0)
            {
                EditorUtility.SetDirty(mat);
                stripped++;
                log.Append('\n').Append(mat.name).Append(" â†’ removed: ")
                   .Append(string.Join(", ", removedKw));
            }
        }

        AssetDatabase.SaveAssets();

        Debug.Log($"[KeywordStripper] Processed {total} materials, " +
                  $"stripped keywords from {stripped}.{log}");

        EditorUtility.DisplayDialog("Strip Unused Keywords",
            $"Done.\n\nProcessed: {total} materials\n" +
            $"Cleaned:   {stripped} materials\n\n" +
            "Fewer shader variants = fewer SetPass calls.\n" +
            "Re-open Play Mode to see the improvement.",
            "OK");
    }

    // â”€â”€ URP/Lit & URP/SimpleLit keyword rules â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static void StripURP(Material mat, List<string> removed)
    {
        // _NORMALMAP â€” only needed when a normal map texture is assigned.
        if (mat.IsKeywordEnabled("_NORMALMAP"))
        {
            bool hasBump = mat.HasProperty("_BumpMap") && mat.GetTexture("_BumpMap") != null;
            if (!hasBump) { mat.DisableKeyword("_NORMALMAP"); removed.Add("_NORMALMAP"); }
        }

        // _EMISSION â€” only needed when emission colour is non-black.
        if (mat.IsKeywordEnabled("_EMISSION"))
        {
            bool hasEmit = false;
            if (mat.HasProperty("_EmissionColor"))
            {
                var ec = mat.GetColor("_EmissionColor");
                hasEmit = ec.r > 0.001f || ec.g > 0.001f || ec.b > 0.001f;
            }
            if (!hasEmit) { mat.DisableKeyword("_EMISSION"); removed.Add("_EMISSION"); }
        }

        // _METALLICSPECGLOSSMAP â€” only needed with a metallic/specular map texture.
        if (mat.IsKeywordEnabled("_METALLICSPECGLOSSMAP"))
        {
            bool hasTex = (mat.HasProperty("_MetallicGlossMap") && mat.GetTexture("_MetallicGlossMap") != null)
                       || (mat.HasProperty("_SpecGlossMap")     && mat.GetTexture("_SpecGlossMap")     != null);
            if (!hasTex) { mat.DisableKeyword("_METALLICSPECGLOSSMAP"); removed.Add("_METALLICSPECGLOSSMAP"); }
        }

        // _SPECULARHIGHLIGHTS_OFF / _ENVIRONMENTREFLECTIONS_OFF
        // If the material has these set via float properties, sync the keyword.
        SyncOffKeyword(mat, "_SPECULARHIGHLIGHTS_OFF",      "_SpecularHighlights",      removed, invertLogic: true);
        SyncOffKeyword(mat, "_ENVIRONMENTREFLECTIONS_OFF",  "_EnvironmentReflections",  removed, invertLogic: true);

        // _OCCLUSIONMAP â€” only needed with an occlusion texture.
        if (mat.IsKeywordEnabled("_OCCLUSIONMAP"))
        {
            bool hasTex = mat.HasProperty("_OcclusionMap") && mat.GetTexture("_OcclusionMap") != null;
            if (!hasTex) { mat.DisableKeyword("_OCCLUSIONMAP"); removed.Add("_OCCLUSIONMAP"); }
        }

        // _DETAIL_MULX2 / _DETAIL_SCALED â€” detail textures.
        foreach (var kw in new[] { "_DETAIL_MULX2", "_DETAIL_SCALED" })
        {
            if (!mat.IsKeywordEnabled(kw)) continue;
            bool hasTex = mat.HasProperty("_DetailAlbedoMap") && mat.GetTexture("_DetailAlbedoMap") != null;
            if (!hasTex) { mat.DisableKeyword(kw); removed.Add(kw); }
        }

        // _PARALLAXMAP
        if (mat.IsKeywordEnabled("_PARALLAXMAP"))
        {
            bool hasTex = mat.HasProperty("_ParallaxMap") && mat.GetTexture("_ParallaxMap") != null;
            if (!hasTex) { mat.DisableKeyword("_PARALLAXMAP"); removed.Add("_PARALLAXMAP"); }
        }
    }

    /// <summary>
    /// Disables <paramref name="keyword"/> when the shader float property
    /// indicates the feature is off (0 â†’ feature off, 1 â†’ feature on).
    /// For "_OFF" keywords the logic is inverted: keyword is set when the feature = 0.
    /// </summary>
    private static void SyncOffKeyword(Material mat, string keyword, string property,
        List<string> removed, bool invertLogic)
    {
        if (!mat.IsKeywordEnabled(keyword)) return;
        if (!mat.HasProperty(property)) { mat.DisableKeyword(keyword); removed.Add(keyword); return; }
        float val     = mat.GetFloat(property);
        bool featureOn = !invertLogic ? val > 0.5f : val < 0.5f;
        // "_OFF" keyword should only be set when feature is OFF.
        if (featureOn) { mat.DisableKeyword(keyword); removed.Add(keyword); }
    }
}

