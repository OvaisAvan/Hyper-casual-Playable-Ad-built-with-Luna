using UnityEngine;
using System.Collections;

namespace TapBlitz.Ad
{
    public enum LunaAdPhase { Initialising, Playing, CTAShown }

    /// <summary>
    /// Luna SDK integration layer.
    ///
    /// Luna (formerly ironSource Luna, now Unity Luna) is Unity's playable ad platform.
    /// This controller bridges Unity C# game events to the Luna JS runtime.
    ///
    /// Luna JS API methods called:
    ///   Luna.Unity.Playable.ShowEndCard()   → triggers the end card / CTA
    ///   Luna.Unity.LifeCycle.GameStart()    → marks gameplay as started
    ///   Luna.Unity.LifeCycle.GameEnd(data)  → sends end-of-game metrics
    ///
    /// Luna also fires events INTO Unity:
    ///   OnLunaMuteAudio(bool)  → mute/unmute audio
    ///   OnLunaPauseGame(bool)  → pause/resume game
    ///
    /// Reference: https://developer.unity.com/products/luna (Unity Developer portal)
    /// </summary>
    public class LunaAdController : MonoBehaviour
    {
        public static LunaAdController Instance { get; private set; }

        [Header("Luna Settings")]
        [SerializeField] private float initTimeout = 5f;   // fallback if Luna doesn't init

        public LunaAdPhase CurrentPhase { get; private set; } = LunaAdPhase.Initialising;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private IEnumerator Start()
        {
            // Brief grace period for Luna JS runtime to initialise
            yield return new WaitForSeconds(0.3f);

            CurrentPhase = LunaAdPhase.Playing;
            NotifyLunaReady();
        }

        // ── Luna Lifecycle Calls ──────────────────────────────────────────────

        /// <summary>Call when the player first interacts (first tap).</summary>
        public void NotifyGameStarted()
        {
#if LUNA_ENABLED
            Luna.Unity.LifeCycle.GameStart();
#else
            LunaBridgeJS.Call("gameStart");
#endif
            Debug.Log("[Luna] GameStart fired.");
        }

        /// <summary>Call when gameplay ends — send metrics to Luna dashboard.</summary>
        public void NotifyGameEnded(int score, int bestCombo, float accuracy)
        {
#if LUNA_ENABLED
            Luna.Unity.LifeCycle.GameEnd(new Luna.Unity.LifeCycle.GameEndData
            {
                score    = score,
                level    = 1,
                success  = true
            });
#else
            LunaBridgeJS.Call("gameEnd", $"{{\"score\":{score},\"combo\":{bestCombo},\"accuracy\":{accuracy:F2}}}");
#endif
            Debug.Log($"[Luna] GameEnd fired. Score={score} Combo={bestCombo} Acc={accuracy:P0}");
        }

        /// <summary>Trigger Luna's native end card / CTA flow.</summary>
        public void ShowLunaEndCard()
        {
            CurrentPhase = LunaAdPhase.CTAShown;
#if LUNA_ENABLED
            Luna.Unity.Playable.ShowEndCard();
#else
            LunaBridgeJS.Call("showEndCard");
#endif
            Debug.Log("[Luna] ShowEndCard fired.");
        }

        // ── Messages FROM Luna JS ─────────────────────────────────────────────

        /// <summary>Luna calls this to mute/unmute audio during the ad.</summary>
        public void OnLunaMuteAudio(string muteStr)
        {
            bool mute = muteStr == "1" || muteStr.ToLower() == "true";
            AudioListener.volume = mute ? 0f : 1f;
            Debug.Log($"[Luna] Audio mute: {mute}");
        }

        /// <summary>Luna calls this to pause/resume the game (e.g. tab hidden).</summary>
        public void OnLunaPauseGame(string pauseStr)
        {
            bool pause     = pauseStr == "1" || pauseStr.ToLower() == "true";
            Time.timeScale = pause ? 0f : 1f;
            Debug.Log($"[Luna] Game pause: {pause}");
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void NotifyLunaReady()
        {
#if LUNA_ENABLED
            // Luna SDK auto-detects readiness
#else
            LunaBridgeJS.Call("adReady");
#endif
            Debug.Log("[Luna] Ad ready.");
        }
    }
}
