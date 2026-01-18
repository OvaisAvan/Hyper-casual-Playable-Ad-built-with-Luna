using UnityEngine;
using TMPro;
using System.Collections;

namespace TapBlitz.UI
{
    /// <summary>
    /// Standalone countdown component.
    /// AdHUD drives the countdown via ShowCountdown();
    /// this component handles only the visual animation of each beat.
    /// Attach to the countdown panel alongside a TMP_Text.
    /// </summary>
    public class CountdownTimer : MonoBehaviour
    {
        [SerializeField] private TMP_Text  numberText;
        [SerializeField] private float     beatDuration  = 0.75f;
        [SerializeField] private float     scaleFrom     = 1.5f;
        [SerializeField] private Color     numberColor   = Color.white;
        [SerializeField] private Color     goColor       = new Color(0.4f, 1f, 0.5f);

        public void AnimateBeat(string text, bool isGo = false)
        {
            if (numberText == null) return;
            numberText.text  = text;
            numberText.color = isGo ? goColor : numberColor;
            StopAllCoroutines();
            StartCoroutine(BeatAnimation());
        }

        private IEnumerator BeatAnimation()
        {
            float elapsed = 0f;
            while (elapsed < beatDuration)
            {
                float t = elapsed / beatDuration;
                transform.localScale = Vector3.Lerp(
                    Vector3.one * scaleFrom,
                    Vector3.one,
                    Mathf.SmoothStep(0f, 1f, t));
                if (numberText) numberText.alpha = Mathf.Lerp(1f, 0.2f, t * t * t);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }
}
