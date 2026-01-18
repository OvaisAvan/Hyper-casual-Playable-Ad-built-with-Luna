using UnityEngine;
using System.Collections;

namespace TapBlitz.UI
{
    /// <summary>
    /// Shows an animated tap-finger hint at a random active target's position
    /// for the first ~3 seconds to teach the player what to do.
    /// Auto-hides after first successful tap.
    /// </summary>
    public class TutorialFinger : MonoBehaviour
    {
        public static TutorialFinger Instance { get; private set; }

        [Header("References")]
        [SerializeField] private RectTransform fingerTransform;
        [SerializeField] private CanvasGroup   canvasGroup;

        [Header("Settings")]
        [SerializeField] private float showDuration = 3.0f;
        [SerializeField] private float tapAnimDuration = 0.35f;
        [SerializeField] private float scaleTapped = 0.75f;

        [Header("Hint Text")]
        [SerializeField] private TMPro.TMP_Text hintLabel;
        [SerializeField] private string hintText = "Tap the targets!";

        private bool visible;
        private Coroutine autoHideRoutine;
        private Coroutine tapLoopRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            SetAlpha(0f);
            if (hintLabel) hintLabel.text = hintText;
        }

        public void Show()
        {
            if (visible) return;
            visible = true;
            SetAlpha(1f);
            tapLoopRoutine   = StartCoroutine(TapLoop());
            autoHideRoutine  = StartCoroutine(AutoHide());
        }

        public void Hide()
        {
            if (!visible) return;
            visible = false;
            if (tapLoopRoutine  != null) StopCoroutine(tapLoopRoutine);
            if (autoHideRoutine != null) StopCoroutine(autoHideRoutine);
            StartCoroutine(FadeOut(0.25f));
        }

        // ── Tap animation ─────────────────────────────────────────────────────

        private IEnumerator TapLoop()
        {
            while (true)
            {
                // Move to a target if possible
                MoveToTarget();

                // Scale down (tap)
                yield return StartCoroutine(ScaleTo(Vector3.one * scaleTapped, tapAnimDuration * 0.4f));
                yield return new WaitForSeconds(0.1f);
                // Scale back up
                yield return StartCoroutine(ScaleTo(Vector3.one, tapAnimDuration * 0.6f));
                yield return new WaitForSeconds(0.6f);
            }
        }

        private void MoveToTarget()
        {
            // Try to point at the first active target
            Core.TargetController[] targets = FindObjectsOfType<Core.TargetController>();
            if (targets.Length == 0) return;

            Core.TargetController pick = targets[Random.Range(0, targets.Length)];
            if (pick == null) return;

            // Convert world → screen → canvas local
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector2 screenPos = cam.WorldToScreenPoint(pick.transform.position);
            Canvas  canvas    = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(),
                screenPos, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam,
                out Vector2 localPoint);

            if (fingerTransform) fingerTransform.anchoredPosition = localPoint;
        }

        private IEnumerator ScaleTo(Vector3 target, float duration)
        {
            Vector3 start   = fingerTransform ? fingerTransform.localScale : Vector3.one;
            float   elapsed = 0f;
            while (elapsed < duration)
            {
                if (fingerTransform)
                    fingerTransform.localScale = Vector3.Lerp(start, target, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (fingerTransform) fingerTransform.localScale = target;
        }

        private IEnumerator AutoHide()
        {
            yield return new WaitForSeconds(showDuration);
            Hide();
        }

        private IEnumerator FadeOut(float duration)
        {
            float start = canvasGroup ? canvasGroup.alpha : 1f, elapsed = 0f;
            while (elapsed < duration)
            {
                SetAlpha(Mathf.Lerp(start, 0f, elapsed / duration));
                elapsed += Time.deltaTime;
                yield return null;
            }
            SetAlpha(0f);
        }

        private void SetAlpha(float a) { if (canvasGroup) canvasGroup.alpha = a; }
    }
}
