#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace TapBlitz.Editor
{
    /// <summary>
    /// Validates the Unity project configuration before building for Luna.
    /// Checks: WebGL platform, compression, exception handling, scene setup.
    /// Run automatically before build, or via Window → TapBlitz → Luna Ad Builder.
    /// </summary>
    public static class LunaProjectValidator
    {
        [MenuItem("Window/TapBlitz/Validate Luna Project")]
        public static bool Validate()
        {
            bool allPassed = true;
            int  passed    = 0;
            int  failed    = 0;

            Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Debug.Log("[LunaValidator] Running pre-build checks…");

            // ── Platform ──────────────────────────────────────────────────────
            Check("Active platform is WebGL",
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL,
                "Switch to WebGL: File → Build Settings → WebGL → Switch Platform",
                ref passed, ref failed, ref allPassed);

            // ── Compression ───────────────────────────────────────────────────
            Check("WebGL compression is Gzip or Disabled",
                PlayerSettings.WebGL.compressionFormat == WebGLCompressionFormat.Gzip ||
                PlayerSettings.WebGL.compressionFormat == WebGLCompressionFormat.Disabled,
                "Set Player Settings → WebGL → Publishing → Compression Format → Gzip",
                ref passed, ref failed, ref allPassed);

            // ── Exception handling ────────────────────────────────────────────
            Check("Exception support is None or Explicitly Thrown",
                PlayerSettings.WebGL.exceptionSupport == WebGLExceptionSupport.None ||
                PlayerSettings.WebGL.exceptionSupport == WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly,
                "Set Player Settings → WebGL → Publishing → Exception Support → None",
                ref passed, ref failed, ref allPassed);

            // ── Strip engine code ─────────────────────────────────────────────
            Check("Strip Engine Code is enabled",
                PlayerSettings.stripEngineCode,
                "Enable Player Settings → Other Settings → Strip Engine Code",
                ref passed, ref failed, ref allPassed);

            // ── Scenes ────────────────────────────────────────────────────────
            Check("At least one scene in Build Settings",
                EditorBuildSettings.scenes.Length > 0,
                "Add TapBlitz scene: File → Build Settings → Add Open Scenes",
                ref passed, ref failed, ref allPassed);

            // ── WebGL template ────────────────────────────────────────────────
            bool lunaTemplate = PlayerSettings.WebGL.template.Contains("LunaPreview") ||
                                PlayerSettings.WebGL.template.Contains("Luna");
            Check("Luna WebGL template selected",
                lunaTemplate,
                "Set Player Settings → WebGL → Resolution → WebGL Template → LunaPreview",
                ref passed, ref failed, ref allPassed);

            // ── TextMeshPro ───────────────────────────────────────────────────
            bool tmpExists = System.Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro") != null;
            Check("TextMeshPro is imported",
                tmpExists,
                "Window → TextMeshPro → Import TMP Essential Resources",
                ref passed, ref failed, ref allPassed);

            Debug.Log($"[LunaValidator] Results: {passed} passed, {failed} failed.");
            Debug.Log(allPassed
                ? "[LunaValidator] ✅ All checks passed — ready to build!"
                : "[LunaValidator] ❌ Fix the issues above before building.");
            Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            return allPassed;
        }

        private static void Check(string label, bool condition, string fix,
                                   ref int passed, ref int failed, ref bool allPassed)
        {
            if (condition)
            {
                Debug.Log($"  ✅ {label}");
                passed++;
            }
            else
            {
                Debug.LogWarning($"  ❌ {label}\n     Fix: {fix}");
                failed++;
                allPassed = false;
            }
        }
    }
}
#endif
