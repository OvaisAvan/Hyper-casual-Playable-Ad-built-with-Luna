using UnityEngine;
using TapBlitz.UI;

namespace TapBlitz.Core
{
    /// <summary>
    /// Tracks current score, applies combo multipliers, and
    /// notifies the HUD. Score is shown on the CTA panel at the end.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [Header("Score Settings")]
        [SerializeField] private int   basePointsPerHit = 10;
        [SerializeField] private float comboMultiplierMax = 5f;

        public int   CurrentScore     { get; private set; }
        public int   HighScore        { get; private set; }
        public float ComboMultiplier  { get; private set; } = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            HighScore = PlayerPrefs.GetInt("TapBlitz_HighScore", 0);
        }

        // ── API ───────────────────────────────────────────────────────────────

        public void AddPoints(int rawPoints)
        {
            int earned = Mathf.RoundToInt(rawPoints * ComboMultiplier);
            CurrentScore = Mathf.Max(0, CurrentScore + earned);

            if (CurrentScore > HighScore)
            {
                HighScore = CurrentScore;
                PlayerPrefs.SetInt("TapBlitz_HighScore", HighScore);
            }

            AdHUD.Instance?.UpdateScore(CurrentScore);
            AdHUD.Instance?.FlashScoreDelta(earned);
        }

        public void RegisterHit(Vector3 worldPos)
        {
            // Multiplier is set by ComboSystem
            ComboMultiplier = ComboSystem.Instance?.CurrentMultiplier ?? 1f;
            ComboMultiplier = Mathf.Min(ComboMultiplier, comboMultiplierMax);
        }

        public void ResetScore()
        {
            CurrentScore    = 0;
            ComboMultiplier = 1f;
            AdHUD.Instance?.UpdateScore(0);
        }
    }
}
