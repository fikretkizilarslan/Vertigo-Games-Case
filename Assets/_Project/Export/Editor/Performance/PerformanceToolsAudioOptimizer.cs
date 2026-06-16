using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Audio clip compression auditor and auto-fixer for mobile.
///
/// Mobile audio rules applied by this tool:
///   â€¢ BGM  (> 5 s)  â†’ Streaming  + Vorbis           â€” never loaded into RAM, read from disk
///   â€¢ SFX  (2â€“5 s)  â†’ CompressedInMemory + Vorbis    â€” balanced: small RAM footprint
///   â€¢ SFX  (< 2 s)  â†’ DecompressOnLoad + ADPCM       â€” fastest playback, fine for short clips
///   â€¢ Stereo SFX    â†’ force mono (spatial difference
///                      is inaudible on mobile; halves file size)
///   â€¢ Sample rate   â†’ 22 050 Hz for SFX (indistinguishable from 44 100 Hz, 50 % smaller)
///
/// Menu: PerformanceTools â†’ Performance â†’ Audio Optimizer
/// </summary>
public sealed class PerformanceToolsAudioOptimizer : EditorWindow
{
    private enum AudioIssue
    {
        ShouldStream, ShouldDecompressOnLoad, ShouldCompressInMemory,
        StereoSfx, HighSampleRate, Pcm
    }

    private sealed class ClipInfo
    {
        public string           Path;
        public string           Name;
        public float            LengthSec;
        public List<AudioIssue> Issues = new List<AudioIssue>(3);
    }

    private Vector2        _scroll;
    private List<ClipInfo> _issues  = new List<ClipInfo>();
    private bool           _scanned;

    [MenuItem("Performance Tools/Performance/Audio Optimizer")]
    private static void Open() => GetWindow<PerformanceToolsAudioOptimizer>("Audio Optimizer");

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Audio clips can consume a large portion of mobile RAM.\n" +
            "BGM: Streaming Vorbis  â€¢  Short SFX: ADPCM  â€¢  Mid SFX: Vorbis\n" +
            "Forcing stereo SFX to mono reduces file size by ~50 %.",
            MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan")) Scan();
        GUI.enabled = _scanned && _issues.Count > 0;
        if (GUILayout.Button($"Auto-Fix All ({_issues.Count})")) AutoFixAll();
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        if (!_scanned) return;

        if (_issues.Count == 0)
        {
            EditorGUILayout.HelpBox("âœ“ All audio clips are optimally configured for mobile!", MessageType.None);
            return;
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var ci in _issues)
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ci.Path);
            EditorGUILayout.ObjectField(clip, typeof(AudioClip), false, GUILayout.Width(180));
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField($"{ci.LengthSec:F1} s", EditorStyles.miniBoldLabel);
            foreach (var issue in ci.Issues)
                EditorGUILayout.LabelField(IssueLabel(issue), EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            if (GUILayout.Button("Fix", GUILayout.Width(40)))
            {
                FixClip(ci.Path, ci.LengthSec);
                Scan();
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    private void Scan()
    {
        _issues.Clear();
        _scanned = true;

        var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Project" });

        foreach (var guid in guids)
        {
            var path     = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) continue;

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) continue;

            float len      = clip.length;
            var   defaults = importer.defaultSampleSettings;

            var ci = new ClipInfo
            {
                Path      = path,
                Name      = Path.GetFileNameWithoutExtension(path),
                LengthSec = len,
            };

            bool isBgm = len >= 5f;
            bool isMid = len >= 2f && len < 5f;
            bool isSfx = len < 2f;

            if (isBgm && defaults.loadType != AudioClipLoadType.Streaming)
                ci.Issues.Add(AudioIssue.ShouldStream);

            if (isSfx && defaults.loadType != AudioClipLoadType.DecompressOnLoad)
                ci.Issues.Add(AudioIssue.ShouldDecompressOnLoad);

            if (isMid && defaults.loadType != AudioClipLoadType.CompressedInMemory)
                ci.Issues.Add(AudioIssue.ShouldCompressInMemory);

            if (!isBgm && !importer.forceToMono)
                ci.Issues.Add(AudioIssue.StereoSfx);

            if (defaults.compressionFormat == AudioCompressionFormat.PCM)
                ci.Issues.Add(AudioIssue.Pcm);

            if (defaults.sampleRateSetting == AudioSampleRateSetting.OverrideSampleRate &&
                defaults.sampleRateOverride > 22050)
                ci.Issues.Add(AudioIssue.HighSampleRate);

            if (ci.Issues.Count > 0)
                _issues.Add(ci);
        }

        Debug.Log($"[AudioOptimizer] Scan complete â€” {_issues.Count} clip(s) need attention.");
        Repaint();
    }

    private void AutoFixAll()
    {
        foreach (var ci in _issues)
            FixClip(ci.Path, ci.LengthSec);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Scan();
    }

    private static void FixClip(string path, float lengthSec)
    {
        var importer = AssetImporter.GetAtPath(path) as AudioImporter;
        if (importer == null) return;

        var  settings = importer.defaultSampleSettings;
        bool isBgm    = lengthSec >= 5f;
        bool isMid    = lengthSec >= 2f && lengthSec < 5f;

        if (isBgm)
        {
            settings.loadType          = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality           = 0.5f;
        }
        else if (isMid)
        {
            settings.loadType          = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality           = 0.5f;
        }
        else
        {
            settings.loadType          = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.ADPCM;
        }

        if (!isBgm)
            importer.forceToMono = true;

        settings.sampleRateSetting  = AudioSampleRateSetting.OverrideSampleRate;
        settings.sampleRateOverride = isBgm ? 44100u : 22050u;

        importer.defaultSampleSettings = settings;
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    private static string IssueLabel(AudioIssue t) => t switch
    {
        AudioIssue.ShouldStream           => "âš  BGM is not Streaming (loaded fully into RAM)",
        AudioIssue.ShouldDecompressOnLoad => "âš  Short SFX should use DecompressOnLoad + ADPCM",
        AudioIssue.ShouldCompressInMemory => "âš  Mid SFX should use CompressedInMemory + Vorbis",
        AudioIssue.StereoSfx              => "âš  SFX is stereo â€” forceToMono saves ~50 % size",
        AudioIssue.Pcm                    => "âš  PCM (uncompressed) format â€” switch to Vorbis/ADPCM",
        AudioIssue.HighSampleRate         => "âš  Sample rate > 22 050 Hz (unnecessary for SFX)",
        _                                 => t.ToString()
    };
}

