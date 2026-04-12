using UnityEngine;
using UnityEngine.EventSystems;
using TapBlitz.Managers;
using TapBlitz.Ad;
using TapBlitz.UI;

namespace TapBlitz.Core
{
    /// <summary>
    /// Handles all tap / click input and routes it to the correct target.
    /// Supports mouse (editor/WebGL desktop) and touch (mobile).
    /// Integrates with Luna analytics to track tap events.
    /// </summary>
    public class TapController : MonoBehaviour
    {
        public static TapController Instance { get; private set; }

        [Header("Input Settings")]
        [SerializeField] private LayerMask tappableLayer;
        [SerializeField] private float     missFlashDuration = 0.15f;

        [Header("Effects")]
        [SerializeField] private GameObject tapRipplePrefab;
        [SerializeField] private Transform  fxParent;

        private Camera mainCam;
        private bool   inputEnabled = true;

        // Stats tracked for Luna analytics
        private int totalTaps;
        private int successfulTaps;
        private int missedTaps;

        public int TotalTaps      => totalTaps;
        public int SuccessfulTaps => successfulTaps;
        public int MissedTaps     => missedTaps;
        public float Accuracy     => totalTaps == 0 ? 0f : (float)successfulTaps / totalTaps;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            mainCam  = Camera.main;
        }

        private void Update()
        {
            if (!inputEnabled) return;
            if (LunaAdController.Instance != null &&
                LunaAdController.Instance.CurrentPhase != LunaAdPhase.Playing) return;

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
            if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
                ProcessTapAt(Input.mousePosition);
#else
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began && !IsPointerOverUI())
                    ProcessTapAt(t.position);
            }
#endif
        }

        // ── Core tap logic ────────────────────────────────────────────────────

        private void ProcessTapAt(Vector2 screenPos)
        {
            totalTaps++;
            TutorialFinger.Instance?.Hide();
            SpawnRipple(screenPos);

            Vector3 worldPos = mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
            worldPos.z = 0f;

            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero, 0f, tappableLayer);

            if (hit.collider != null)
            {
                TargetController target = hit.collider.GetComponent<TargetController>();
                if (target != null && target.IsAlive)
                {
                    successfulTaps++;
                    target.OnTapped();
                    ScoreManager.Instance?.RegisterHit(worldPos);
                    ComboSystem.Instance?.RegisterHit();
                    AudioManager.Instance?.PlayTapHit();
                    EffectsManager.Instance?.SpawnHitBurst(worldPos);
                    LunaAnalytics.Instance?.TrackTap(true, worldPos);
                    return;
                }
            }

            // Miss
            missedTaps++;
            ComboSystem.Instance?.RegisterMiss();
            AudioManager.Instance?.PlayTapMiss();
            LunaAnalytics.Instance?.TrackTap(false, worldPos);
        }

        private void SpawnRipple(Vector2 screenPos)
        {
            if (tapRipplePrefab == null || fxParent == null) return;
            Vector3 worldPos = mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
            worldPos.z = 0f;
            GameObject ripple = PoolManager.Instance != null
                ? PoolManager.Instance.Get(tapRipplePrefab, worldPos, Quaternion.identity)
                : Instantiate(tapRipplePrefab, worldPos, Quaternion.identity, fxParent);
            if (ripple) ripple.transform.SetParent(fxParent);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void SetInputEnabled(bool enabled) => inputEnabled = enabled;

        private bool IsPointerOverUI() =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
