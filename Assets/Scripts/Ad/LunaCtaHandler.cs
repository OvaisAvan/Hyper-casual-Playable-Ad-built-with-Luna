using UnityEngine;

namespace TapBlitz.Ad
{
    /// <summary>
    /// Handles the CTA (Call To Action) install button for Luna playable ads.
    ///
    /// Luna provides its own end card via Luna.Unity.Playable.ShowEndCard(),
    /// but you can also trigger a custom in-game CTA overlay BEFORE showing
    /// the Luna end card. This controller manages both paths.
    ///
    /// Store URL is injected by Luna at runtime via SendMessage —
    /// you do NOT need to hardcode it.
    /// </summary>
    public class LunaCtaHandler : MonoBehaviour
    {
        public static LunaCtaHandler Instance { get; private set; }

        [Header("Fallback URL (used outside Luna environment)")]
        [SerializeField] private string fallbackStoreUrl = "https://play.google.com/store/apps/details?id=com.yourcompany.tapblitz";

        [Header("CTA Flow")]
        [Tooltip("If true, show Luna's native end card. If false, use the in-game CTA overlay only.")]
        [SerializeField] private bool useLunaNativeEndCard = true;

        private string runtimeStoreUrl;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            runtimeStoreUrl = fallbackStoreUrl;
        }

        // ── Called by Luna JS at runtime ──────────────────────────────────────

        /// <summary>
        /// Luna injects the store URL via SendMessage before the ad plays.
        /// Usage (auto, Luna runtime): unityInstance.SendMessage('LunaCtaHandler','SetStoreUrl','https://...')
        /// </summary>
        public void SetStoreUrl(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                runtimeStoreUrl = url;
                Debug.Log($"[LunaCTA] Store URL set: {url}");
            }
        }

        // ── Called by in-game Install button ──────────────────────────────────

        public void OnInstallButtonTapped()
        {
            LunaAnalytics.Instance?.TrackCTATap();

            if (useLunaNativeEndCard)
            {
                // Let Luna handle the install redirect via its own end card
                LunaAdController.Instance?.ShowLunaEndCard();
            }
            else
            {
                OpenStoreDirectly();
            }

            Debug.Log("[LunaCTA] Install tapped.");
        }

        public void OpenStoreDirectly()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            LunaBridgeJS.OpenStore(runtimeStoreUrl);
#else
            Application.OpenURL(runtimeStoreUrl);
            Debug.Log($"[LunaCTA] Opening store: {runtimeStoreUrl}");
#endif
        }
    }
}
