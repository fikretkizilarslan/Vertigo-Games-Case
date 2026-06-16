using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Editor window: lists every Graphic in the scene that has raycastTarget = true
/// but no interactable component (Button, Toggle, Slider, Dropdown, InputField, EventTrigger).
///
/// Why it matters: GraphicRaycaster hit-tests every raycastTarget element each frame.
/// Large lists measurably increase CPU cost on mobile. Disabling unnecessary targets is free.
///
/// Menu: PerformanceTools â†’ Diagnostics â†’ RaycastTarget Auditor
/// </summary>
public sealed class RaycastTargetAuditor : EditorWindow
{
    private struct Entry
    {
        public Graphic graphic;
        public bool    hasInteractable;
        public string  path;
    }

    private readonly List<Entry> _suspicious = new List<Entry>(64);
    private Vector2 _scroll;
    private bool    _scanned;

    [MenuItem("Performance Tools/Diagnostics/RaycastTarget Auditor")]
    public static void Open() => GetWindow<RaycastTargetAuditor>("RaycastTarget Auditor");

    private void OnGUI()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("RaycastTarget Auditor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Lists every Graphic (Image, Text, etc.) that has raycastTarget = true " +
            "but no interactable component (Button, Toggle, Slider, Dropdown, InputField, EventTrigger). " +
            "Disabling these reduces GraphicRaycaster CPU cost every frame.",
            MessageType.Info);

        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Scan", GUILayout.Height(28)))
                Scan();

            GUI.enabled = _scanned && _suspicious.Count > 0;
            if (GUILayout.Button($"Disable All ({_suspicious.Count})", GUILayout.Height(28)))
                DisableAll();
            GUI.enabled = true;
        }

        if (!_scanned)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Not yet scanned. Press 'Scan'.", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField($"Suspicious items: {_suspicious.Count}", EditorStyles.boldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        for (int i = _suspicious.Count - 1; i >= 0; i--)
        {
            var e = _suspicious[i];
            if (e.graphic == null)
            {
                _suspicious.RemoveAt(i);
                continue;
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.ObjectField(e.graphic, typeof(Graphic), true, GUILayout.Width(200));
                EditorGUILayout.LabelField(e.path, EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Disable", GUILayout.Width(60)))
                {
                    Undo.RecordObject(e.graphic, "RaycastTarget OFF");
                    e.graphic.raycastTarget = false;
                    EditorUtility.SetDirty(e.graphic);
                    _suspicious.RemoveAt(i);
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void Scan()
    {
        _suspicious.Clear();
        _scanned = true;

        var graphics = new List<Graphic>(256);
        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            var scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded) continue;
            foreach (var root in scene.GetRootGameObjects())
                graphics.AddRange(root.GetComponentsInChildren<Graphic>(true));
        }

        foreach (var g in graphics)
        {
            if (g == null || !g.raycastTarget) continue;
            if (HasInteractableComponent(g.gameObject)) continue;
            if (IsRequiredSliderRaycastGraphic(g)) continue;

            _suspicious.Add(new Entry
            {
                graphic        = g,
                hasInteractable = false,
                path           = BuildPath(g.transform),
            });
        }

        _suspicious.Sort((a, b) => string.Compare(a.path, b.path, System.StringComparison.Ordinal));
    }

    private void DisableAll()
    {
        foreach (var e in _suspicious)
        {
            if (e.graphic == null) continue;
            Undo.RecordObject(e.graphic, "RaycastTarget OFF");
            e.graphic.raycastTarget = false;
            EditorUtility.SetDirty(e.graphic);
        }
        _suspicious.Clear();
        Debug.Log("[RaycastTargetAuditor] All suspicious raycastTarget values set to false.");
    }

    private static bool HasInteractableComponent(GameObject go)
    {
        if (go.GetComponent<Button>()     != null) return true;
        if (go.GetComponent<Toggle>()     != null) return true;
        if (go.GetComponent<Slider>()     != null) return true;
        if (go.GetComponent<Dropdown>()   != null) return true;
        if (go.GetComponent<InputField>() != null) return true;
        if (go.GetComponent<UnityEngine.EventSystems.EventTrigger>() != null) return true;
        if (go.GetComponent<TMPro.TMP_Dropdown>()  != null) return true;
        if (go.GetComponent<TMPro.TMP_InputField>() != null) return true;
        return false;
    }

    /// <summary>Slider handle + track live on child Graphics; disabling them breaks dragging.</summary>
    private static bool IsRequiredSliderRaycastGraphic(Graphic g)
    {
        var slider = g.GetComponentInParent<Slider>();
        if (slider == null)
            return false;

        if (slider.targetGraphic == g)
            return true;

        var t = g.transform;
        if (t.parent == slider.transform && t.name == "Background")
            return true;

        return false;
    }

    private static string BuildPath(Transform t)
    {
        var sb = new System.Text.StringBuilder(64);
        BuildPathRecursive(t, sb);
        return sb.ToString();
    }

    private static void BuildPathRecursive(Transform t, System.Text.StringBuilder sb)
    {
        if (t.parent != null)
        {
            BuildPathRecursive(t.parent, sb);
            sb.Append('/');
        }
        sb.Append(t.name);
    }
}

