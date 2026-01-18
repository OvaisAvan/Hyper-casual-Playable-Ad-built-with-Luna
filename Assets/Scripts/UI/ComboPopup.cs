using UnityEngine;
using TMPro;
using System.Collections;

namespace TapBlitz.UI
{
    /// <summary>
    /// A world-space floating popup ("NICE! ×1.5") that appears when
    /// a combo tier is reached. Pooled by EffectsManager.
    /// Attach to a prefab with TMP_Text + CanvasGroup.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ComboPopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text  mainLabel;
        [SerializeField] private TMP_Text  multLabel;

        [Header("Animation")]
        [SerializeField] private float floatDistance = 80f;
        [SerializeField] private float duration      = 1.1f;

        [Header("Colours per tier")]
        [SerializeField] private Color tier1Color = Color.yellow;
        [SerializeField] private Color tier2Color = new Color(0.4f, 1f, 0.5f);
        [SerializeField] private Color tier3Color = new Color(0.4f, 0.8f, 1f);
        [SerializeField] private Color tier4Color = new Color(1f, 0.5f, 1f);

        private CanvasGroup cg;

        private void Awake() => cg = GetComponent<CanvasGroup>();

        public void Play(string label, float multiplier, int tier)
        {
            if (mainLabel) mainLabel.text = label;
            if (multLabel) multLabel.text = multiplier > 1f ? $"×{multiplier:F1}" : "";

            Color c = tier switch
            {
                1 => tier1Color,
                2 => tier2Color,
                3 => tier3Color,
                _ => tier4Color
            };
            if (mainLabel) mainLabel.color = c;
            if (multLabel) multLabel.color = c;

            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            RectTransform rt    = GetComponent<RectTransform>();
            Vector2       start = rt ? rt.anchoredPosition : Vector2.zero;
            float         elapsed = 0f;

            // Pop scale
            transform.localScale = Vector3.one * 1.3f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                if (rt) rt.anchoredPosition = start + Vector2.up * (floatDistance * t);
                if (cg) cg.alpha            = 1f - (t * t);
                transform.localScale        = Vector3.Lerp(Vector3.one * 1.3f, Vector3.one * 0.8f, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            Managers.EffectsManager.Instance?.ReturnPopup(gameObject);
        }
    }
}
