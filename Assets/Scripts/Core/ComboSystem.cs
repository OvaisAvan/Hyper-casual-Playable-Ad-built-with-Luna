using UnityEngine;
using System.Collections;
using TapBlitz.Managers;
using TapBlitz.UI;

namespace TapBlitz.Core
{
    /// <summary>
    /// Tracks consecutive hits to build a combo multiplier.
    /// A miss resets the combo. Fires visual/audio feedback at each tier.
    ///
    /// Combo tiers:
    ///   1–4  hits → ×1.0
    ///   5–9  hits → ×1.5  "NICE!"
    ///   10–14 hits → ×2.0  "GREAT!"
    ///   15–19 hits → ×3.0  "AMAZING!"
    ///   20+  hits → ×5.0  "UNSTOPPABLE!"
    /// </summary>
    public class ComboSystem : MonoBehaviour
    {
        public static ComboSystem Instance { get; private set; }

        [Header("Combo Tiers")]
        [SerializeField] private int[]   tierThresholds   = { 5, 10, 15, 20 };
        [SerializeField] private float[] tierMultipliers  = { 1.5f, 2f, 3f, 5f };
        [SerializeField] private string[] tierLabels      = { "NICE!", "GREAT!", "AMAZING!", "UNSTOPPABLE!" };

        [Header("Combo Decay")]
        [SerializeField] private bool  comboDecays      = false;   // set true for timed combo window
        [SerializeField] private float comboWindowSec   = 3f;

        public int   CurrentCombo      { get; private set; }
        public float CurrentMultiplier { get; private set; } = 1f;
        public int   BestCombo         { get; private set; }

        private int    currentTierIndex = -1;
        private Coroutine decayRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void RegisterHit()
        {
            if (comboDecays)
            {
                if (decayRoutine != null) StopCoroutine(decayRoutine);
                decayRoutine = StartCoroutine(ComboDecay());
            }

            CurrentCombo++;
            if (CurrentCombo > BestCombo) BestCombo = CurrentCombo;

            UpdateTier();
            AdHUD.Instance?.UpdateCombo(CurrentCombo, CurrentMultiplier);
        }

        public void RegisterMiss()
        {
            if (CurrentCombo == 0) return;

            if (decayRoutine != null) StopCoroutine(decayRoutine);
            int broken = CurrentCombo;
            CurrentCombo      = 0;
            CurrentMultiplier = 1f;
            currentTierIndex  = -1;

            AdHUD.Instance?.UpdateCombo(0, 1f);
            AdHUD.Instance?.ShowComboBreak();

            if (broken >= tierThresholds[0])
                AudioManager.Instance?.PlayComboBreak();
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void UpdateTier()
        {
            int newTier = -1;
            for (int i = tierThresholds.Length - 1; i >= 0; i--)
            {
                if (CurrentCombo >= tierThresholds[i]) { newTier = i; break; }
            }

            CurrentMultiplier = newTier >= 0 ? tierMultipliers[newTier] : 1f;

            if (newTier > currentTierIndex)
            {
                currentTierIndex = newTier;
                string label = tierLabels[newTier];
                AdHUD.Instance?.ShowComboLabel(label, CurrentMultiplier);
                AudioManager.Instance?.PlayComboTierUp();
                CameraShake.Instance?.Shake(0.12f, newTier * 0.05f + 0.05f);
            }
        }

        private IEnumerator ComboDecay()
        {
            yield return new WaitForSeconds(comboWindowSec);
            RegisterMiss();
        }
    }
}
