using UnityEngine;
using System.Runtime.InteropServices;

namespace TapBlitz.Ad
{
    /// <summary>
    /// C# static wrapper for the LunaBridge.jslib JavaScript plugin.
    /// Provides typed methods so the rest of the codebase never
    /// deals with raw DllImport strings.
    ///
    /// In editor / non-WebGL builds all calls are no-ops that log to console.
    /// </summary>
    public static class LunaBridgeJS
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void _LunaCall(string method);
        [DllImport("__Internal")] private static extern void _LunaCallData(string method, string data);
        [DllImport("__Internal")] private static extern void _LunaTrack(string eventName, string json);
        [DllImport("__Internal")] private static extern void _LunaOpenStore(string url);
        [DllImport("__Internal")] private static extern void _LunaShowEndCard();
#endif

        public static void Call(string method)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            _LunaCall(method);
#else
            Debug.Log($"[LunaBridge] Call → {method}");
#endif
        }

        public static void Call(string method, string data)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            _LunaCallData(method, data);
#else
            Debug.Log($"[LunaBridge] Call → {method}({data})");
#endif
        }

        public static void Track(string eventName, string json)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            _LunaTrack(eventName, json);
#else
            Debug.Log($"[LunaBridge] Track → {eventName}: {json}");
#endif
        }

        public static void OpenStore(string url)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            _LunaOpenStore(url);
#else
            Application.OpenURL(url);
            Debug.Log($"[LunaBridge] OpenStore → {url}");
#endif
        }

        public static void ShowEndCard()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            _LunaShowEndCard();
#else
            Debug.Log("[LunaBridge] ShowEndCard");
#endif
        }
    }
}
