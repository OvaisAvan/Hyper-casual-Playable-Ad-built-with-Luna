using UnityEngine;
using System.Collections.Generic;
using TapBlitz.UI;

namespace TapBlitz.Managers
{
    /// <summary>
    /// Manages all visual effects for TapBlitz:
    ///   - Hit burst particles (pooled)
    ///   - Ripple sprites on tap (pooled)
    ///   - Combo popup labels (pooled)
    /// Uses a simple keyed pool to avoid GC spikes in WebGL.
    /// </summary>
    public class EffectsManager : MonoBehaviour
    {
        public static EffectsManager Instance { get; private set; }

        [Header("Hit Burst")]
        [SerializeField] private ParticleSystem hitBurstPrefab;
        [SerializeField] private int            burstPoolSize  = 12;

        [Header("Combo Popup")]
        [SerializeField] private ComboPopup comboPopupPrefab;
        [SerializeField] private Transform  popupParent;
        [SerializeField] private int        popupPoolSize = 6;

        [Header("Screen Shake")]
        [SerializeField] private CameraShake cameraShake;

        private readonly Queue<ParticleSystem> burstPool  = new();
        private readonly Queue<ComboPopup>     popupPool  = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            PrewarmBursts();
            PrewarmPopups();
        }

        // ── Hit burst ─────────────────────────────────────────────────────────

        public void SpawnHitBurst(Vector3 worldPos)
        {
            ParticleSystem ps = GetBurst();
            ps.transform.position = worldPos;
            ps.gameObject.SetActive(true);
            ps.Play();
            StartCoroutine(ReturnBurstAfter(ps, ps.main.duration + ps.main.startLifetime.constantMax));
        }

        private ParticleSystem GetBurst() =>
            burstPool.Count > 0
                ? burstPool.Dequeue()
                : Instantiate(hitBurstPrefab, transform);

        private System.Collections.IEnumerator ReturnBurstAfter(ParticleSystem ps, float delay)
        {
            yield return new WaitForSeconds(delay);
            ps.Stop();
            ps.gameObject.SetActive(false);
            burstPool.Enqueue(ps);
        }

        // ── Combo popup ───────────────────────────────────────────────────────

        public void SpawnComboPopup(string label, float multiplier, int tier, Vector2 canvasPos)
        {
            ComboPopup popup = GetPopup();
            if (popup.GetComponent<RectTransform>() is RectTransform rt)
                rt.anchoredPosition = canvasPos;
            popup.gameObject.SetActive(true);
            popup.Play(label, multiplier, tier);
        }

        public void ReturnPopup(GameObject popupGo)
        {
            ComboPopup cp = popupGo.GetComponent<ComboPopup>();
            if (cp != null)
            {
                popupGo.SetActive(false);
                popupPool.Enqueue(cp);
            }
            else Destroy(popupGo);
        }

        private ComboPopup GetPopup() =>
            popupPool.Count > 0
                ? popupPool.Dequeue()
                : Instantiate(comboPopupPrefab, popupParent);

        // ── Prewarming ────────────────────────────────────────────────────────

        private void PrewarmBursts()
        {
            if (hitBurstPrefab == null) return;
            for (int i = 0; i < burstPoolSize; i++)
            {
                var ps = Instantiate(hitBurstPrefab, transform);
                ps.gameObject.SetActive(false);
                burstPool.Enqueue(ps);
            }
        }

        private void PrewarmPopups()
        {
            if (comboPopupPrefab == null || popupParent == null) return;
            for (int i = 0; i < popupPoolSize; i++)
            {
                var popup = Instantiate(comboPopupPrefab, popupParent);
                popup.gameObject.SetActive(false);
                popupPool.Enqueue(popup);
            }
        }
    }
}
