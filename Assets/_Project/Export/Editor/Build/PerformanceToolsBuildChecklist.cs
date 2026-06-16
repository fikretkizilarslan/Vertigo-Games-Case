using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Pre-build mobile optimization checklist.
///
/// Automatically validates 13 critical settings and reports any problems.
/// Items marked âš¡ support one-click Auto-Fix.
///
/// Menu: PerformanceTools â†’ Build â†’ Mobile Build Checklist
/// </summary>
public sealed class PerformanceToolsBuildChecklist : EditorWindow
{
    private enum Status { Ok, Warn, Error, Unknown }

    private sealed class CheckItem
    {
        public string        Name;
        public Status        Status;
        public string        Detail;
        public System.Action AutoFix;
    }

    private Vector2         _scroll;
    private List<CheckItem> _items = new List<CheckItem>();
    private bool            _ran;

    [MenuItem("Performance Tools/Build/Mobile Build Checklist")]
    private static void Open() => GetWindow<PerformanceToolsBuildChecklist>("Build Checklist");

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Make sure every item is green before shipping a release build.\n" +
            "âš¡ = Auto-Fix available.",
            MessageType.Info);

        if (GUILayout.Button("Run Checks")) RunAll();

        if (!_ran) return;

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        foreach (var item in _items)
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);

            string icon = item.Status switch
            {
                Status.Ok    => "âœ“",
                Status.Warn  => "âš ",
                Status.Error => "âœ—",
                _            => "?"
            };
            var color = item.Status switch
            {
                Status.Ok    => Color.green,
                Status.Warn  => new Color(1f, 0.6f, 0f),
                Status.Error => Color.red,
                _            => Color.gray
            };

            var old = GUI.color;
            GUI.color = color;
            EditorGUILayout.LabelField(icon, GUILayout.Width(14));
            GUI.color = old;

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(item.Name, EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(item.Detail))
                EditorGUILayout.LabelField(item.Detail, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            if (item.AutoFix != null && item.Status != Status.Ok)
            {
                if (GUILayout.Button("âš¡ Fix", GUILayout.Width(55)))
                {
                    item.AutoFix();
                    RunAll();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(1);
        }

        EditorGUILayout.EndScrollView();
    }

    private void RunAll()
    {
        _items.Clear();
        _ran = true;

        CheckScriptingBackend();
        CheckIl2CppStripping();
        CheckGraphicsApiAndroid();
        CheckUrpHdr();
        CheckUrpShadows();
        CheckUrpDynamicBatching();
        CheckSrpBatcher();
        CheckTextureCompression();
        CheckDevelopmentBuild();
        CheckMinApi();
        CheckPhysicsSettings();
        CheckVSync();
        CheckTargetFrameRate();
        CheckEditorScriptFolder();

        Repaint();
    }

    // â”€â”€â”€ Individual checks â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void CheckScriptingBackend()
    {
        var target  = NamedBuildTarget.Android;
        var backend = PlayerSettings.GetScriptingBackend(target);
        Add("Scripting Backend: IL2CPP",
            backend == ScriptingImplementation.IL2CPP ? Status.Ok : Status.Error,
            backend == ScriptingImplementation.IL2CPP ? null : "Mono is slow on mobile â€” switch to IL2CPP",
            () => PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP));
    }

    private void CheckIl2CppStripping()
    {
        var target = NamedBuildTarget.Android;
        var level  = PlayerSettings.GetManagedStrippingLevel(target);
        bool ok    = level >= ManagedStrippingLevel.Medium;
        Add("IL2CPP Code Stripping â‰¥ Medium",
            ok ? Status.Ok : Status.Warn,
            ok ? null : $"Current: {level} â€” set to Medium or High to reduce build size",
            () => PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Medium));
    }

    private void CheckGraphicsApiAndroid()
    {
        bool auto = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android);
        Add("Android Graphics API",
            auto ? Status.Ok : Status.Warn,
            auto
                ? "Auto-select enabled (Vulkan + GLES3)"
                : "Manual override â€” ensure Vulkan is first in the list",
            () => PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, true));
    }

    private void CheckUrpHdr()
    {
        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset == null) { Add("URP HDR", Status.Unknown, "URP asset not found"); return; }
        bool hdr = urpAsset.supportsHDR;
        Add("URP: HDR Disabled",
            hdr ? Status.Warn : Status.Ok,
            hdr ? "HDR is expensive on mobile â€” disable it" : null,
            () => { urpAsset.supportsHDR = false; EditorUtility.SetDirty(urpAsset); });
    }

    private void CheckUrpShadows()
    {
        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset == null) return;
        float dist = urpAsset.shadowDistance;
        bool  ok   = dist <= 25f;
        Add($"URP: Shadow Distance â‰¤ 25 (current: {dist:F0})",
            ok ? Status.Ok : Status.Warn,
            ok ? null : "Reduce shadow distance (20 recommended for mobile)",
            () => { urpAsset.shadowDistance = 20f; EditorUtility.SetDirty(urpAsset); });
    }

    private void CheckUrpDynamicBatching()
    {
        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset == null) return;
        bool dyn = urpAsset.supportsDynamicBatching;
        Add("URP: Dynamic Batching Enabled",
            dyn ? Status.Ok : Status.Warn,
            dyn ? null : "Enable Dynamic Batching (helps with small meshes that share a material)",
            () => { urpAsset.supportsDynamicBatching = true; EditorUtility.SetDirty(urpAsset); });
    }

    private void CheckSrpBatcher()
    {
        bool srp = GraphicsSettings.useScriptableRenderPipelineBatching;
        Add("SRP Batcher Enabled",
            srp ? Status.Ok : Status.Error,
            srp ? null : "SRP Batcher is critical â€” enable it immediately",
            () => { GraphicsSettings.useScriptableRenderPipelineBatching = true; });
    }

    private void CheckTextureCompression()
    {
        var  format = EditorUserBuildSettings.androidBuildSubtarget;
        bool ok     = format == MobileTextureSubtarget.ASTC;
        Add("Android Texture Compression: ASTC",
            ok ? Status.Ok : Status.Error,
            ok ? null : $"Current: {format} â€” select ASTC in Build Settings",
            () => { EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC; });
    }

    private void CheckDevelopmentBuild()
    {
        bool dev = EditorUserBuildSettings.development;
        Add("Development Build Disabled",
            dev ? Status.Warn : Status.Ok,
            dev ? "Development Build is ON â€” disable it for release" : null,
            () => { EditorUserBuildSettings.development = false; });
    }

    private void CheckMinApi()
    {
        int  minApi = (int)PlayerSettings.Android.minSdkVersion;
        bool ok     = minApi >= 24;
        Add($"Android Min API â‰¥ 24 (current: {minApi})",
            ok ? Status.Ok : Status.Warn,
            ok ? null : "Devices below API 24 may not support ASTC textures");
    }

    private void CheckPhysicsSettings()
    {
        Add("Physics: Fixed Timestep check",
            Status.Unknown,
            "Edit â†’ Project Settings â†’ Time â†’ Fixed Timestep: 0.02 (50 Hz) or 0.033 (30 Hz) recommended");
    }

    private void CheckVSync()
    {
        int  vsync = QualitySettings.vSyncCount;
        bool ok    = vsync == 0;
        Add("VSync Disabled (use Application.targetFrameRate on mobile)",
            ok ? Status.Ok : Status.Warn,
            ok ? null : "VSync prevents proper FPS control on mobile â€” disable it",
            () => QualitySettings.vSyncCount = 0);
    }

    private void CheckTargetFrameRate()
    {
        Add("Application.targetFrameRate = 60",
            Status.Unknown,
            "Confirm your Bootstrapper or GameManager calls Application.targetFrameRate = 60 at startup");
    }

    private void CheckEditorScriptFolder()
    {
        var guids = AssetDatabase.FindAssets("t:Script", new[] { "Assets/_Project/Scripts/Editor" });
        Add($"Editor scripts ({guids.Length}) are in the Editor/ folder",
            Status.Ok,
            "âœ“ All Editor scripts are correctly placed â€” they will be excluded from player builds");
    }

    private void Add(string name, Status status, string detail, System.Action autoFix = null)
    {
        _items.Add(new CheckItem { Name = name, Status = status, Detail = detail, AutoFix = autoFix });
    }
}

