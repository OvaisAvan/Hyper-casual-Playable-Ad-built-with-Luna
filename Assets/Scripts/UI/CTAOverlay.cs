using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using TapBlitz.Ad;

namespace TapBlitz.UI
{
    /// <summary>
    /// The in-game CTA overlay shown when gameplay ends.
    /// Displays final score, best combo, star rating, and an install button.
    /// After the user taps Install, delegates to LunaCtaHandler.
    /// </summary>
    public class CTAOverlay : MonoBehaviour
    {
        public static CTAOverlay Instance { get; private set; }

        [Header("Panel")]
        [SerializeField] private GameObject  panel;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Score Summary")]
        [SerializeField] private TMP_Text finalScoreText;
        [SerializeField] private TMP_Text bestComboText;
        [SerializeField] private TMP_Text taglineText;

        [Header("Stars")]
        [SerializeField] private Image[] starImages;          // 3 star images
        [SerializeField] private Color   starActiveColor   = new Color(1f, 0.85f, 0.1f);
        [SerializeField] private Color   starInactiveColor = new Color(0.3f, 0.3f, 0.3f);

        [Header("CTA Button")]
        [SerializeField] private Button   installButton;
        [SerializeField] private TMP_Text installButtonLabel;

        [Header("Copy")]
        [SerializeField] private string ctaButtonText = "INSTALL FREE";
        [SerializeField] private string[] taglines    = {
            "Can you beat this score?",
            "Think you can do better?",
            "Play the full game — it's free!"
        };

        [Header("Star Thresholds (score)")]
        [SerializeField] private int oneStar   = 50;
        [SerializeField] private int twoStar   = 100;
        [SerializeField] private int threeStar = 180;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            panel?.SetActive(false);
            if (installButton)
                installButton.onClick.AddListener(OnInstallTapped);
            if (installButtonLabel)
                installButtonLabel.text = ctaButtonText;
        }

        // ── Show ──────────────────────────────────────────────────────────────

        public void Show(int finalScore, int bestCombo)
        {
            panel?.SetActive(true);
            if (canvasGroup) canvasGroup.alpha = 0f;

            // Populate fields
            if (finalScoreText) finalScoreText.text = $"{finalScore:N0}";
            if (bestComboText)  bestComboText.text  = $"Best Combo: ×{bestCombo}";
            if (taglineText)    taglineText.text     = taglines[Random.Range(0, taglines.Length)];

            SetStars(finalScore);
            LunaAnalytics.Instance?.TrackCTAShown(finalScore, bestCombo);

            StartCoroutine(FadeIn());
            StartCoroutine(AnimateStars());
        }

        // ── Stars ─────────────────────────────────────────────────────────────

        private void SetStars(int score)
        {
            int starCount = score >= threeStar ? 3 : score >= twoStar ? 2 : score >= oneStar ? 1 : 0;
            for (int i = 0; i < starImages.Length; i++)
                if (starImages[i])
                    starImages[i].color = starInactiveColor;

            // Animate them in AnimateStars
            if (canvasGroup) canvasGroup.alpha = 0f;
        }

        private IEnumerator AnimateStars()
        {
            yield return new WaitForSeconds(0.5f);
            int score     = Core.ScoreManager.Instance?.CurrentScore ?? 0;
            int starCount = score >= threeStar ? 3 : score >= twoStar ? 2 : score >= oneStar ? 1 : 0;

            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] == null) continue;
                bool active = i < starCount;
                yield return new WaitForSeconds(0.2f);
                starImages[i].color = active ? starActiveColor : starInactiveColor;
                if (active)
                {
                    Managers.AudioManager.Instance?.PlayStarPop();
                    yield return StartCoroutine(StarPop(starImages[i].transform));
                }
            }
        }

        private IEnumerator StarPop(Transform t)
        {
            float dur = 0.25f, elapsed = 0f;
            while (elapsed < dur)
            {
                float p = elapsed / dur;
                t.localScale = Vector3.Lerp(Vector3.one * 1.4f, Vector3.one, p);
                elapsed += Time.deltaTime;
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        // ── CTA Button ────────────────────────────────────────────────────────

        private void OnInstallTapped()
        {
            Managers.AudioManager.Instance?.PlayUIClick();
            LunaCtaHandler.Instance?.OnInstallButtonTapped();
        }

        // ── Animations ────────────────────────────────────────────────────────

        private IEnumerator FadeIn()
        {
            float dur = 0.4f, elapsed = 0f;
            while (elapsed < dur)
            {
                if (canvasGroup) canvasGroup.alpha = elapsed / dur;
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (canvasGroup) canvasGroup.alpha = 1f;
        }
    }
}
