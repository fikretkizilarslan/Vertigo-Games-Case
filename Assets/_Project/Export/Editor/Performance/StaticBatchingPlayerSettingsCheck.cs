using UnityEditor;
using UnityEngine;

/// <summary>
/// Convenience shortcut that opens Project Settings â†’ Player so you can verify
/// "Static Batching" is enabled for the active build target.
///
/// We avoid programmatic toggling here because the underlying PlayerSettings batching
/// API moved between Unity versions (BuildTarget vs NamedBuildTarget vs Graphics Settings)
/// and a misfire here would silently misconfigure the project. Visual verification is safer.
/// </summary>
public static class StaticBatchingPlayerSettingsCheck
{
    private const string MenuPathOpen = "Performance Tools/Performance/Open Player Settings (verify Static Batching)";

    [MenuItem(MenuPathOpen)]
    public static void OpenPlayerSettings()
    {
        SettingsService.OpenProjectSettings("Project/Player");
        Debug.Log(
            "[StaticBatchingCheck] Project Settings â†’ Player opened. " +
            "Expand 'Other Settings' for the active build target and confirm 'Static Batching' is checked. " +
            "It is enabled by default; this is a one-time visual check.");
    }
}

