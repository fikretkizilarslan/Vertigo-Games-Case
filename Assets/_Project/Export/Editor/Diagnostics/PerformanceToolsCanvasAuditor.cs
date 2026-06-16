using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Canvas and UI system auditor.
/// Displays which Canvases cause issues, identifies unnecessary GraphicRaycasters,
/// and reports nested LayoutGroups â€” all in a single editor window.
///
/// Menu: PerformanceTools â†’ Diagnostics â†’ Canvas Auditor
///
/// Canvas optimization rules:
///   â€¢ Each Canvas is its own draw-batch pool â†’ fewer Canvases = fewer rebuilds
///   â€¢ GraphicRaycaster should only exist on Canvases that require pointer input
///   â€¢ Static UI (backgrounds, decorations) in a separate Canvas: never rebuilds again
///   â€¢ Dynamic UI (score, timer) in a separate Canvas: only that Canvas rebuilds
///   â€¢ LayoutGroup recalculates every frame; replace with anchors where possible
/// </summary>
public sealed class PerformanceToolsCanvasAuditor : EditorWindow
{
    private Vector2 _scroll;

    private sealed class CanvasInfo
    {
        public Canvas Canvas;
        public bool   HasRaycaster;
        public bool   HasInteractables;
        public int    ChildGraphics;
        public int    LayoutGroups;
        public int    NonStaticImages; // raycastTarget=true but no Button/Toggle
        public string RenderMode;
        public string Path;
    }

    private List<CanvasInfo> _results = new List<CanvasInfo>();
    private bool _scanned;

    [MenuItem("Performance Tools/Diagnostics/Canvas Auditor")]
    private static void Open() => GetWindow<PerformanceToolsCanvasAuditor>("Canvas Auditor");

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Canvas rebuild is the largest per-frame CPU cost of UGUI.\n" +
            "Splitting static and dynamic UI into separate Canvases eliminates unnecessary rebuilds.",
            MessageType.Info);

        if (GUILayout.Button("Scan Active Scene")) Scan();

        if (!_scanned) return;

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        foreach (var ci in _results)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.ObjectField(ci.Canvas, typeof(Canvas), true);

            var issues = new List<string>(4);
            if (ci.HasRaycaster && !ci.HasInteractables)
                issues.Add("âš  GraphicRaycaster present but no interactable elements â€” can be removed");
            if (ci.LayoutGroups > 0)
                issues.Add($"âš  {ci.LayoutGroups} LayoutGroup(s) â€” consider replacing with anchors");
            if (ci.NonStaticImages > 5)
                issues.Add($"âš  {ci.NonStaticImages} Graphics have raycastTarget=true but are not interactable");

            EditorGUILayout.LabelField(
                $"[{ci.RenderMode}]  Graphics: {ci.ChildGraphics}  " +
                $"Raycaster: {(ci.HasRaycaster ? "âœ“" : "âœ—")}  " +
                $"LayoutGroups: {ci.LayoutGroups}  " +
                $"Excess raycastTargets: {ci.NonStaticImages}",
                EditorStyles.miniLabel);

            foreach (var issue in issues)
                EditorGUILayout.LabelField(issue, EditorStyles.miniLabel);

            if (issues.Count == 0)
                EditorGUILayout.LabelField("âœ“ No issues found", EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        if (GUILayout.Button("Remove Unnecessary GraphicRaycasters"))
            RemoveUnnecessaryRaycasters();
    }

    private void Scan()
    {
        _results.Clear();
        _scanned = true;

        var canvases = new List<Canvas>(32);
        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            var scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded) continue;
            foreach (var root in scene.GetRootGameObjects())
                canvases.AddRange(root.GetComponentsInChildren<Canvas>(true));
        }

        foreach (var canvas in canvases)
        {
            if (canvas == null) continue;

            var ci = new CanvasInfo
            {
                Canvas       = canvas,
                HasRaycaster = canvas.GetComponent<GraphicRaycaster>() != null,
                RenderMode   = canvas.renderMode.ToString(),
                Path         = BuildPath(canvas.transform),
            };

            var graphics      = canvas.GetComponentsInChildren<Graphic>(true);
            var interactables = canvas.GetComponentsInChildren<Selectable>(true);
            var layouts       = canvas.GetComponentsInChildren<LayoutGroup>(true);

            ci.ChildGraphics    = graphics.Length;
            ci.HasInteractables = interactables.Length > 0;
            ci.LayoutGroups     = layouts.Length;

            int raycastNonInteractable = 0;
            foreach (var g in graphics)
            {
                if (!g.raycastTarget) continue;
                if (g.GetComponent<Selectable>() == null) raycastNonInteractable++;
            }
            ci.NonStaticImages = raycastNonInteractable;

            _results.Add(ci);
        }

        Repaint();
    }

    private void RemoveUnnecessaryRaycasters()
    {
        int removed = 0;
        foreach (var ci in _results)
        {
            if (ci.Canvas == null) continue;
            if (!ci.HasRaycaster) continue;
            if (ci.HasInteractables) continue;

            var raycaster = ci.Canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null) continue;

            Undo.DestroyObjectImmediate(raycaster);
            ci.HasRaycaster = false;
            removed++;
        }

        Debug.Log($"[CanvasAuditor] Removed {removed} unnecessary GraphicRaycaster(s).");
        if (removed > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            Repaint();
        }
    }

    private static string BuildPath(Transform t)
    {
        var parts = new List<string>(4);
        while (t != null) { parts.Insert(0, t.name); t = t.parent; }
        return string.Join("/", parts);
    }
}

