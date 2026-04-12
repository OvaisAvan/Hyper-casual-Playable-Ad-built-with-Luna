#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace TapBlitz.Editor
{
    /// <summary>
    /// Custom Editor Window: Window → TapBlitz → Luna Ad Builder
    ///
    /// One-click workflow for building and packaging Luna playable ads:
    ///   1. Validates the project (LunaProjectValidator)
    ///   2. Builds WebGL
    ///   3. Runs build_luna.py to package for Luna submission
    ///   4. Opens the output folder
    ///
    /// Luna submission format:
    ///   - A single .zip containing index.html + all assets, OR
    ///   - A single inlined HTML file (depending on Luna version)
    ///
    /// Luna documentation: https://developer.unity.com/products/luna
    /// </summary>
    public class LunaAdBuilder : EditorWindow
    {
        private string buildOutputPath  = "Builds/WebGL";
        private string lunaOutputPath   = "Builds/Luna";
        private string pythonPath       = "python3";
        private bool   autoValidate     = true;
        private bool   autoOpenFolder   = true;
        private bool   runPackager      = true;
        private string gameTitle        = "TapBlitz";
        private string storeUrlIos      = "https://apps.apple.com/app/idYOURID";
        private string storeUrlAndroid  = "https://play.google.com/store/apps/details?id=com.yourcompany.tapblitz";

        private Vector2 scroll;

        [MenuItem("Window/TapBlitz/Luna Ad Builder")]
        public static void ShowWindow() =>
            GetWindow<LunaAdBuilder>("Luna Ad Builder");

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            GUILayout.Label("TapBlitz — Luna Playable Ad Builder", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Builds a Luna-compatible WebGL playable ad.\n" +
                "Submit the output folder or HTML file to Unity Luna / ironSource Luna dashboard.",
                MessageType.Info);

            // ── Paths ──────────────────────────────────────────────────────────
            EditorGUILayout.Space(8);
            GUILayout.Label("Build Paths", EditorStyles.boldLabel);
            buildOutputPath = EditorGUILayout.TextField("WebGL Output Dir",  buildOutputPath);
            lunaOutputPath  = EditorGUILayout.TextField("Luna Package Dir",  lunaOutputPath);
            pythonPath      = EditorGUILayout.TextField("Python Executable", pythonPath);

            // ── Ad Config ──────────────────────────────────────────────────────
            EditorGUILayout.Space(8);
            GUILayout.Label("Ad Configuration", EditorStyles.boldLabel);
            gameTitle       = EditorGUILayout.TextField("Game Title",        gameTitle);
            storeUrlIos     = EditorGUILayout.TextField("iOS Store URL",     storeUrlIos);
            storeUrlAndroid = EditorGUILayout.TextField("Android Store URL", storeUrlAndroid);

            // ── Options ────────────────────────────────────────────────────────
            EditorGUILayout.Space(8);
            GUILayout.Label("Options", EditorStyles.boldLabel);
            autoValidate   = EditorGUILayout.Toggle("Validate Before Build",  autoValidate);
            runPackager    = EditorGUILayout.Toggle("Run Luna Packager Script", runPackager);
            autoOpenFolder = EditorGUILayout.Toggle("Open Output When Done",   autoOpenFolder);

            // ── Buttons ────────────────────────────────────────────────────────
            EditorGUILayout.Space(12);
            GUI.backgroundColor = new Color(0.5f, 0.85f, 1f);
            if (GUILayout.Button("▶  Build Luna Playable Ad", GUILayout.Height(44)))
                Build();

            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate Project"))  ValidateProject();
            if (GUILayout.Button("Run Packager Only")) RunPackager();
            if (GUILayout.Button("Open Output"))       OpenFolder(lunaOutputPath);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Luna Docs: https://developer.unity.com/products/luna\n" +
                "Max file size: 5 MB (aim for ≤ 3 MB for best results).",
                MessageType.None);

            EditorGUILayout.EndScrollView();
        }

        // ── Actions ───────────────────────────────────────────────────────────

        private void Build()
        {
            if (autoValidate && !ValidateProject()) return;

            string[] scenes = GetEnabledScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError("[LunaBuilder] No scenes in Build Settings!");
                return;
            }

            Directory.CreateDirectory(buildOutputPath);

            var opts = new BuildPlayerOptions
            {
                scenes           = scenes,
                locationPathName = buildOutputPath,
                target           = BuildTarget.WebGL,
                options          = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(opts);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.LogError("[LunaBuilder] WebGL build FAILED.");
                return;
            }

            Debug.Log("[LunaBuilder] WebGL build complete ✅");

            if (runPackager) RunPackager();
            if (autoOpenFolder) OpenFolder(lunaOutputPath);
        }

        private bool ValidateProject()
        {
            bool valid = LunaProjectValidator.Validate();
            Debug.Log(valid ? "[LunaBuilder] Validation PASSED ✅" : "[LunaBuilder] Validation FAILED ❌");
            return valid;
        }

        private void RunPackager()
        {
            string scriptPath = Path.GetFullPath("BuildConfig/build_luna.py");
            if (!File.Exists(scriptPath))
            {
                Debug.LogError($"[LunaBuilder] Packager script not found: {scriptPath}");
                return;
            }

            string args = $"\"{scriptPath}\" \"{Path.GetFullPath(buildOutputPath)}\" \"{Path.GetFullPath(lunaOutputPath)}\" --title \"{gameTitle}\" --ios \"{storeUrlIos}\" --android \"{storeUrlAndroid}\"";

            var psi = new ProcessStartInfo(pythonPath, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };

            using var proc = Process.Start(psi);
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            if (!string.IsNullOrEmpty(stdout)) Debug.Log("[Packager] " + stdout);
            if (!string.IsNullOrEmpty(stderr)) Debug.LogWarning("[Packager] " + stderr);
            Debug.Log(proc.ExitCode == 0 ? "[LunaBuilder] Package complete ✅" : "[LunaBuilder] Package failed ❌");
        }

        private string[] GetEnabledScenes()
        {
            var list = new System.Collections.Generic.List<string>();
            foreach (var s in EditorBuildSettings.scenes)
                if (s.enabled) list.Add(s.path);
            return list.ToArray();
        }

        private void OpenFolder(string path)
        {
            Directory.CreateDirectory(path);
            EditorUtility.RevealInFinder(Path.GetFullPath(path));
        }
    }
}
#endif
