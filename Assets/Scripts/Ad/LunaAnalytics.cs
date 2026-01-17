using UnityEngine;
using System.Collections.Generic;

namespace TapBlitz.Ad
{
    /// <summary>
    /// Wraps Luna's analytics system.
    ///
    /// Luna records all events in its dashboard for A/B testing and
    /// creative optimisation. Standard events:
    ///
    ///   tap_hit      — player tapped a valid target
    ///   tap_miss     — player missed
    ///   combo_tier   — player reached a new combo tier
    ///   cta_shown    — CTA overlay displayed
    ///   cta_tapped   — player tapped install
    ///   game_start   — first interaction
    ///   game_end     — session ended (with score/combo payload)
    ///
    /// When LUNA_ENABLED is defined, calls go to Luna.Unity.Analytics.
    /// Otherwise they log to console (editor) or fire via LunaBridgeJS (WebGL).
    /// </summary>
    public class LunaAnalytics : MonoBehaviour
    {
        public static LunaAnalytics Instance { get; private set; }

        private bool gameStartFired;
        private int  tapCount;
        private int  hitCount;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Public track methods ──────────────────────────────────────────────

        public void TrackTap(bool isHit, Vector3 worldPos)
        {
            tapCount++;
            if (isHit) hitCount++;

            if (!gameStartFired)
            {
                gameStartFired = true;
                Track("game_start", new Dictionary<string, object>
                {
                    { "first_tap_type", isHit ? "hit" : "miss" }
                });
            }

            Track("tap", new Dictionary<string, object>
            {
                { "is_hit",   isHit },
                { "tap_index", tapCount },
                { "accuracy", tapCount > 0 ? (float)hitCount / tapCount : 0f }
            });
        }

        public void TrackComboTier(int tier, string label, float multiplier)
        {
            Track("combo_tier", new Dictionary<string, object>
            {
                { "tier",       tier },
                { "label",      label },
                { "multiplier", multiplier }
            });
        }

        public void TrackCTAShown(int score, int bestCombo)
        {
            Track("cta_shown", new Dictionary<string, object>
            {
                { "score",      score },
                { "best_combo", bestCombo },
                { "tap_count",  tapCount },
                { "accuracy",   tapCount > 0 ? (float)hitCount / tapCount : 0f }
            });
        }

        public void TrackCTATap()
        {
            Track("cta_tapped", new Dictionary<string, object>
            {
                { "score",     Core.ScoreManager.Instance?.CurrentScore ?? 0 },
                { "tap_count", tapCount }
            });
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void Track(string eventName, Dictionary<string, object> payload = null)
        {
#if LUNA_ENABLED
            // Luna SDK: Luna.Unity.Analytics.TrackEvent(eventName, payload);
            // Uncomment when Luna package is imported:
            // Luna.Unity.Analytics.TrackEvent(eventName, payload);
#elif UNITY_WEBGL && !UNITY_EDITOR
            string json = payload != null ? DictToJson(payload) : "{}";
            LunaBridgeJS.Track(eventName, json);
#else
            string payloadStr = payload != null ? DictToJson(payload) : "{}";
            Debug.Log($"[LunaAnalytics] {eventName}: {payloadStr}");
#endif
        }

        private string DictToJson(Dictionary<string, object> dict)
        {
            var parts = new System.Text.StringBuilder("{");
            bool first = true;
            foreach (var kv in dict)
            {
                if (!first) parts.Append(",");
                string val = kv.Value is string s ? $"\"{s}\"" : kv.Value.ToString().ToLower();
                parts.Append($"\"{kv.Key}\":{val}");
                first = false;
            }
            parts.Append("}");
            return parts.ToString();
        }
    }
}
