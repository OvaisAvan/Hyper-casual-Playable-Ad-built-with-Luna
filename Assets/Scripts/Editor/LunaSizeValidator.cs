#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Collections.Generic;

namespace TapBlitz.Editor
{
    /// <summary>
    /// Post-build processor that validates the WebGL output size
    /// against Luna's submission requirements.
    ///
    /// Luna size limits (as of 2024):
    ///   Recommended : ≤ 2 MB
    ///   Warn        : 2–5 MB
    ///   Hard limit  : 5 MB (submission rejected above this)
    /// </summary>
    public class LunaSizeValidator : IPostprocessBuildWithReport
    {
        public int callbackOrder => 10;

        private const long RecommendedBytes = 2 * 1024 * 1024;   // 2 MB
        private const long WarnBytes        = 3 * 1024 * 1024;   // 3 MB
        private const long ErrorBytes       = 5 * 1024 * 1024;   // 5 MB

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL) return;

            string path     = report.summary.outputPath;
            long   total    = GetDirectorySize(path);
            float  totalMB  = total / (1024f * 1024f);

            Debug.Log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Debug.Log($"[LunaSizeValidator] Build size: {totalMB:F2} MB");

            if (total > ErrorBytes)
            {
                Debug.LogError($"[LunaSizeValidator] ⛔ EXCEEDS 5 MB — Luna will REJECT this build!");
                Debug.LogError("[LunaSizeValidator] Reduce texture sizes, enable ASTC compression, strip unused assets.");
            }
            else if (total > WarnBytes)
            {
                Debug.LogWarning($"[LunaSizeValidator] ⚠️ Over 3 MB — verify Luna's current limit before submitting.");
            }
            else if (total > RecommendedBytes)
            {
                Debug.LogWarning($"[LunaSizeValidator] Over 2 MB recommended. Consider optimising textures.");
            }
            else
            {
                Debug.Log($"[LunaSizeValidator] ✅ Within Luna recommended limits ({totalMB:F2} MB)");
            }

            Debug.Log("[LunaSizeValidator] Largest files:");
            foreach (var f in GetLargestFiles(path, 8))
                Debug.Log($"  {f.Name,-40} {f.Length / 1024f,8:F1} KB");

            Debug.Log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        }

        private long GetDirectorySize(string dirPath)
        {
            if (!Directory.Exists(dirPath)) return 0;
            long size = 0;
            foreach (string f in Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories))
                size += new FileInfo(f).Length;
            return size;
        }

        private List<FileInfo> GetLargestFiles(string dirPath, int count)
        {
            var files = new List<FileInfo>();
            if (!Directory.Exists(dirPath)) return files;
            foreach (string f in Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories))
                files.Add(new FileInfo(f));
            files.Sort((a, b) => b.Length.CompareTo(a.Length));
            return files.GetRange(0, Mathf.Min(count, files.Count));
        }
    }
}
#endif
