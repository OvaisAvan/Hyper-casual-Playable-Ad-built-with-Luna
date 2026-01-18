using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace TapBlitz.UI
{
    /// <summary>
    /// Manages the in-game HUD elements:
    ///   - Score display with animated delta flash
    ///   - Countdown timer bar
    ///   - Combo label and multiplier badge
    ///   - Pre-game countdown (3-2-1-GO!)
    /// </summary>
    public class AdHUD : MonoBehaviour
    {
        public static AdHUD Instance { get; private set; }

        [Header("Score")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text scoreDeltaText;   // shows "+30" flash

        [Header("Timer")]
        [SerializeField] private Image    timerFill;        // horizontal fill bar
        [SerializeField] private TMP_Text timerLabel;
        [SerializeField] private Color    timerNormal  = new Color(0.4f, 0.8f, 1f);
        [SerializeField] private Color    timerUrgent  = new Color(1f, 0.3f, 0.3f);

        [Header("Combo")]
        [SerializeField] private GameObject comboPanel;
        [SerializeField] private TMP_Text   comboCountText;
        [SerializeField] private TMP_Text   comboMultText;
        [SerializeField] private TMP_Text   comboLabelText;
        [SerializeField] private TMP_Text   comboBreakText;

        [Header("Countdown")]
        [SerializeField] private GameObject countdownPanel;
        [SerializeField] private TMP_Text   countdownText;

        private Coroutine deltaRoutine;
        private Coroutine comboLabelRoutine;
        private Coroutine countdownRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (scoreDeltaText) scoreDeltaText.alpha = 0f;
            if (comboLabelText) comboLabelText.alpha = 0f;
            if (comboBreakText) comboBreakText.alpha = 0f;
            comboPanel?.SetActive(false);
            countdownPanel?.SetActive(false);
        }

        // ── Score ─────────────────────────────────────────────────────────────

        public void UpdateScore(int score)
        {
            if (scoreText) scoreText.text = score.ToString("N0");
        }

        public void FlashScoreDelta(int delta)
        {
            if (scoreDeltaText == null) return;
            if (deltaRoutine != null) StopCoroutine(deltaRoutine);
            deltaRoutine = StartCoroutine(FlashDelta(delta));
        }

        private IEnumerator FlashDelta(int delta)
        {
            scoreDeltaText.text  = delta >= 0 ? $"+{delta}" : delta.ToString();
            scoreDeltaText.color = delta >= 0 ? Color.yellow : Color.red;

            float dur = 0.7f, elapsed = 0f;
            Vector3 startPos = scoreDeltaText.rectTransform.anchoredPosition3D;

            while (elapsed < dur)
            {
                float t = elapsed / dur;
                scoreDeltaText.alpha = Mathf.Lerp(1f, 0f, t * t);
                scoreDeltaText.rectTransform.anchoredPosition =
                    startPos + new Vector3(0f, 30f * t, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            scoreDeltaText.alpha = 0f;
            scoreDeltaText.rectTransform.anchoredPosition = startPos;
        }

        // ── Timer ─────────────────────────────────────────────────────────────

        public void UpdateTimer(float remaining, float total)
        {
            float fill = Mathf.Clamp01(remaining / total);
            if (timerFill) timerFill.fillAmount = fill;

            int secs = Mathf.CeilToInt(remaining);
            if (timerLabel) timerLabel.text = secs.ToString();

            bool urgent = remaining <= 5f;
            Color c = urgent
                ? Color.Lerp(timerNormal, timerUrgent, Mathf.PingPong(Time.time * 4f, 1f))
                : timerNormal;
            if (timerFill) timerFill.color   = c;
            if (timerLabel) timerLabel.color = c;
        }

        // ── Combo ─────────────────────────────────────────────────────────────

        public void UpdateCombo(int combo, float multiplier)
        {
            bool active = combo >= 2;
            comboPanel?.SetActive(active);
            if (comboCountText) comboCountText.text = $"×{combo}";
            if (comboMultText)  comboMultText.text  = multiplier > 1f ? $"×{multiplier:F1}" : "";
        }

        public void ShowComboLabel(string label, float multiplier)
        {
            if (comboLabelText == null) return;
            if (comboLabelRoutine != null) StopCoroutine(comboLabelRoutine);
            comboLabelRoutine = StartCoroutine(FlashComboLabel(label));
        }

        public void ShowComboBreak()
        {
            if (comboBreakText == null) return;
            StartCoroutine(FlashText(comboBreakText, "COMBO BREAK!", Color.red, 0.8f));
        }

        private IEnumerator FlashComboLabel(string label)
        {
            comboLabelText.text = label;
            yield return StartCoroutine(FlashText(comboLabelText, label, Color.yellow, 1.2f));
        }

        private IEnumerator FlashText(TMP_Text text, string content, Color color, float duration)
        {
            text.text  = content;
            text.color = color;
            text.alpha = 1f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                text.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            text.alpha = 0f;
        }

        // ── Countdown ─────────────────────────────────────────────────────────

        public void ShowCountdown(int from, System.Action onComplete)
        {
            if (countdownRoutine != null) StopCoroutine(countdownRoutine);
            countdownRoutine = StartCoroutine(CountdownRoutine(from, onComplete));
        }

        private IEnumerator CountdownRoutine(int from, System.Action onComplete)
        {
            countdownPanel?.SetActive(true);
            for (int i = from; i >= 1; i--)
            {
                if (countdownText) countdownText.text = i.ToString();
                Managers.AudioManager.Instance?.PlayCountdownTick();
                yield return StartCoroutine(CountdownBeat());
            }
            if (countdownText) countdownText.text = "GO!";
            Managers.AudioManager.Instance?.PlayCountdownGo();
            yield return new WaitForSeconds(0.5f);
            countdownPanel?.SetActive(false);
            onComplete?.Invoke();
        }

        private IEnumerator CountdownBeat()
        {
            float dur = 0.8f, elapsed = 0f;
            if (countdownText)
            {
                while (elapsed < dur)
                {
                    float t = elapsed / dur;
                    countdownText.transform.localScale = Vector3.Lerp(Vector3.one * 1.3f, Vector3.one, t);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
            else yield return new WaitForSeconds(dur);
        }
    }
}
