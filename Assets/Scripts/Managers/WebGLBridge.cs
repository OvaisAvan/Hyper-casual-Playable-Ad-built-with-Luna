using UnityEngine;

namespace TapBlitz.Managers
{
    /// <summary>
    /// WebGL compatibility layer:
    ///   - Applies performance settings for WebGL ad builds
    ///   - Listens for postMessage from the parent frame (store URL injection)
    ///   - Exposes the Unity instance to JS via window.unityInstance
    ///
    /// Luna communicates via SendMessage — no extra listener needed.
    /// This handles non-Luna environments (generic HTML5 ad networks).
    /// </summary>
    public class WebGLBridge : MonoBehaviour
    {
        [Header("Performance")]
        [SerializeField] private int  targetFPS      = 60;
        [SerializeField] private bool disableVSync   = true;

        [Header("Screen")]
        [SerializeField] private bool preventSleep   = true;

        private void Awake()
        {
            Application.targetFrameRate  = targetFPS;
            if (disableVSync) QualitySettings.vSyncCount = 0;
            if (preventSleep) Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Texture memory — keep low for ad size limits
            QualitySettings.globalTextureMipmapLimit = 0;

            Debug.Log($"[WebGLBridge] Init. FPS={targetFPS} VSync={!disableVSync}");
        }

        // ── Called from JS via SendMessage ────────────────────────────────────

        /// <summary>
        /// Parent frame can inject store URL:
        /// unityInstance.SendMessage('WebGLBridge','OnStoreUrlReceived','https://...')
        /// </summary>
        public void OnStoreUrlReceived(string url)
        {
            Ad.LunaCtaHandler.Instance?.SetStoreUrl(url);
            Debug.Log($"[WebGLBridge] Store URL received: {url}");
        }

        /// <summary>
        /// Parent frame can mute audio:
        /// unityInstance.SendMessage('WebGLBridge','OnMuteAudio','true')
        /// </summary>
        public void OnMuteAudio(string muteStr)
        {
            bool mute = muteStr.ToLower() == "true" || muteStr == "1";
            AudioListener.volume = mute ? 0f : 1f;
        }

        /// <summary>
        /// Luna calls this to pause when the tab is hidden.
        /// unityInstance.SendMessage('WebGLBridge','OnPauseGame','true')
        /// </summary>
        public void OnPauseGame(string pauseStr)
        {
            bool pause     = pauseStr.ToLower() == "true" || pauseStr == "1";
            Time.timeScale = pause ? 0f : 1f;
        }
    }
}
