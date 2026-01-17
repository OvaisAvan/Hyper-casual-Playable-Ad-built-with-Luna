using UnityEngine;
using System.Collections;
using TapBlitz.Managers;

namespace TapBlitz.Core
{
    public enum TargetType { Normal, Bonus, Bomb }

    /// <summary>
    /// Controls a single tap target:
    ///  - Normal  → tap for points
    ///  - Bonus   → tap for 3× points + combo boost
    ///  - Bomb    → tapping subtracts score and breaks combo
    ///
    /// Targets shrink over their lifetime; missing them costs combo.
    /// </summary>
    public class TargetController : MonoBehaviour
    {
        [Header("Type")]
        [SerializeField] private TargetType targetType = TargetType.Normal;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite         normalSprite;
        [SerializeField] private Sprite         bonusSprite;
        [SerializeField] private Sprite         bombSprite;
        [SerializeField] private Color          normalColor = new Color(0.4f, 0.8f, 1f);
        [SerializeField] private Color          bonusColor  = new Color(1f, 0.85f, 0.1f);
        [SerializeField] private Color          bombColor   = new Color(1f, 0.25f, 0.25f);

        [Header("Lifetime")]
        [SerializeField] private float baseLifetime   = 2.5f;
        [SerializeField] private float minLifetime    = 1.0f;
        [SerializeField] private AnimationCurve shrinkCurve = AnimationCurve.EaseInOut(0,1,1,0);

        [Header("Score Values")]
        [SerializeField] private int normalPoints = 10;
        [SerializeField] private int bonusPoints  = 30;
        [SerializeField] private int bombPenalty  = 15;

        public TargetType TargetType => targetType;
        public bool IsAlive { get; private set; }

        private float lifetime;
        private float elapsed;
        private Coroutine lifetimeRoutine;

        // ── Initialise ────────────────────────────────────────────────────────

        public void Initialise(float difficulty)
        {
            IsAlive  = true;
            elapsed  = 0f;
            lifetime = Mathf.Lerp(baseLifetime, minLifetime, difficulty);

            ApplyVisuals();
            transform.localScale = Vector3.one;

            if (lifetimeRoutine != null) StopCoroutine(lifetimeRoutine);
            lifetimeRoutine = StartCoroutine(LifetimeRoutine());
        }

        private void ApplyVisuals()
        {
            if (spriteRenderer == null) return;
            switch (targetType)
            {
                case TargetType.Bonus:
                    spriteRenderer.sprite = bonusSprite ?? normalSprite;
                    spriteRenderer.color  = bonusColor;
                    break;
                case TargetType.Bomb:
                    spriteRenderer.sprite = bombSprite ?? normalSprite;
                    spriteRenderer.color  = bombColor;
                    break;
                default:
                    spriteRenderer.sprite = normalSprite;
                    spriteRenderer.color  = normalColor;
                    break;
            }
        }

        // ── Tap response ──────────────────────────────────────────────────────

        public void OnTapped()
        {
            if (!IsAlive) return;
            IsAlive = false;

            if (lifetimeRoutine != null) StopCoroutine(lifetimeRoutine);

            int points = targetType switch
            {
                TargetType.Bonus => bonusPoints,
                TargetType.Bomb  => -bombPenalty,
                _                => normalPoints
            };

            ScoreManager.Instance?.AddPoints(points);
            StartCoroutine(PopAndReturn());
        }

        // ── Lifetime ──────────────────────────────────────────────────────────

        private IEnumerator LifetimeRoutine()
        {
            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lifetime;
                transform.localScale = Vector3.one * shrinkCurve.Evaluate(t);

                // Pulse urgency in final 30%
                if (t > 0.7f && spriteRenderer != null)
                {
                    float pulse = Mathf.PingPong(Time.time * 6f, 1f);
                    spriteRenderer.color = Color.Lerp(spriteRenderer.color, Color.white, pulse * 0.3f);
                }

                yield return null;
            }

            // Expired without being tapped
            if (IsAlive)
            {
                IsAlive = false;
                ComboSystem.Instance?.RegisterMiss();
                AudioManager.Instance?.PlayTargetExpire();
                ReturnToPool();
            }
        }

        // ── Animations ────────────────────────────────────────────────────────

        private IEnumerator PopAndReturn()
        {
            // Quick scale pop
            float dur = 0.18f, elapsed = 0f;
            Vector3 start = transform.localScale;
            while (elapsed < dur)
            {
                float t = elapsed / dur;
                transform.localScale = Vector3.Lerp(start, Vector3.zero, t * t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            TargetSpawner.Instance?.UnregisterTarget(this);
            if (PoolManager.Instance != null)
                PoolManager.Instance.Return(gameObject);
            else
                Destroy(gameObject);
        }
    }
}
