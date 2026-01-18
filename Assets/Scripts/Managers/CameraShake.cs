using UnityEngine;
using System.Collections;

namespace TapBlitz.Managers
{
    /// <summary>
    /// Lightweight camera shake using Perlin noise.
    /// Called on high combo tiers and bomb taps.
    /// Attach to the Main Camera.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [SerializeField] private float defaultDuration  = 0.2f;
        [SerializeField] private float defaultMagnitude = 0.08f;

        private Vector3     originalPos;
        private Coroutine   shakeRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance    = this;
            originalPos = transform.localPosition;
        }

        public void Shake(float duration = -1f, float magnitude = -1f)
        {
            if (duration  < 0) duration  = defaultDuration;
            if (magnitude < 0) magnitude = defaultMagnitude;

            if (shakeRoutine != null) StopCoroutine(shakeRoutine);
            shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t         = 1f - (elapsed / duration);   // fade out
                float offsetX   = (Mathf.PerlinNoise(Time.time * 30f, 0f) - 0.5f) * 2f * magnitude * t;
                float offsetY   = (Mathf.PerlinNoise(0f, Time.time * 30f) - 0.5f) * 2f * magnitude * t;
                transform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localPosition = originalPos;
        }
    }
}
