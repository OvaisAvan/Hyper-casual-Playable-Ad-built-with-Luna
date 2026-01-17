using UnityEngine;
using System.Collections;
using TapBlitz.Ad;
using TapBlitz.UI;
using TapBlitz.Managers;

namespace TapBlitz.Core
{
    public enum GamePhase { Idle, Starting, Playing, Ending }

    /// <summary>
    /// Owns the game loop for the playable ad session.
    /// Coordinates: init → tutorial → gameplay → CTA trigger.
    /// Talks to LunaAdController for ad lifecycle events.
    /// </summary>
    public class GameLoop : MonoBehaviour
    {
        public static GameLoop Instance { get; private set; }

        [Header("Timing")]
        [SerializeField] private float startDelay    = 0.6f;   // board settle time
        [SerializeField] private float playDuration  = 15f;    // seconds of gameplay
        [SerializeField] private float endDelay      = 0.5f;   // pause before CTA

        [Header("CTA Trigger Conditions")]
        [SerializeField] private int   scoreCtaTrigger = 150;  // show CTA early if reached
        [SerializeField] private int   comboCtaTrigger = 15;   // show CTA on big combo

        public GamePhase CurrentPhase { get; private set; } = GamePhase.Idle;
        public float     TimeRemaining { get; private set; }

        private bool ctaTriggered;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(startDelay);
            StartGame();
        }

        // ── Game Phases ───────────────────────────────────────────────────────

        public void StartGame()
        {
            CurrentPhase  = GamePhase.Starting;
            TimeRemaining = playDuration;
            ctaTriggered  = false;

            ScoreManager.Instance?.ResetScore();
            TapController.Instance?.SetInputEnabled(false);

            AdHUD.Instance?.ShowCountdown(3, OnCountdownComplete);
        }

        private void OnCountdownComplete()
        {
            CurrentPhase = GamePhase.Playing;
            TapController.Instance?.SetInputEnabled(true);
            TargetSpawner.Instance?.StartSpawning();
            TutorialFinger.Instance?.Show();
            LunaAdController.Instance?.NotifyGameStarted();
            StartCoroutine(GameTimer());
        }

        private IEnumerator GameTimer()
        {
            while (TimeRemaining > 0f && CurrentPhase == GamePhase.Playing)
            {
                TimeRemaining -= Time.deltaTime;
                AdHUD.Instance?.UpdateTimer(TimeRemaining, playDuration);

                // Early CTA triggers
                if (!ctaTriggered)
                {
                    int score = ScoreManager.Instance?.CurrentScore ?? 0;
                    int combo = ComboSystem.Instance?.CurrentCombo  ?? 0;
                    if (score >= scoreCtaTrigger || combo >= comboCtaTrigger)
                        TriggerCTA();
                }

                yield return null;
            }

            if (!ctaTriggered) TriggerCTA();
        }

        public void TriggerCTA()
        {
            if (ctaTriggered) return;
            ctaTriggered = true;
            CurrentPhase = GamePhase.Ending;

            TapController.Instance?.SetInputEnabled(false);
            TargetSpawner.Instance?.StopSpawning();

            StartCoroutine(EndSequence());
        }

        private IEnumerator EndSequence()
        {
            yield return new WaitForSeconds(endDelay);

            int   score    = ScoreManager.Instance?.CurrentScore ?? 0;
            int   bestCombo = ComboSystem.Instance?.BestCombo   ?? 0;
            float accuracy  = TapController.Instance?.Accuracy  ?? 0f;

            LunaAdController.Instance?.NotifyGameEnded(score, bestCombo, accuracy);
            CTAOverlay.Instance?.Show(score, bestCombo);
            AudioManager.Instance?.PlayCTAJingle();
        }
    }
}
