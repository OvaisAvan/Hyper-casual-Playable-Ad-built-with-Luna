using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TapBlitz.Managers;

namespace TapBlitz.Core
{
    /// <summary>
    /// Spawns tap targets in configurable waves.
    /// Wave data is loaded from wave_config.json via WaveConfig ScriptableObject.
    /// Difficulty ramps up over the 15-second ad duration.
    /// </summary>
    public class TargetSpawner : MonoBehaviour
    {
        public static TargetSpawner Instance { get; private set; }

        [Header("Target Prefabs")]
        [SerializeField] private TargetController[] targetPrefabs;   // normal, bonus, bomb
        [SerializeField] private Transform           targetParent;

        [Header("Spawn Area")]
        [SerializeField] private Vector2 spawnAreaMin = new Vector2(-3.5f, -4f);
        [SerializeField] private Vector2 spawnAreaMax = new Vector2( 3.5f,  4f);
        [SerializeField] private float   edgePadding  = 0.6f;

        [Header("Wave Timing")]
        [SerializeField] private float initialSpawnDelay = 0.8f;
        [SerializeField] private float baseSpawnInterval = 1.2f;
        [SerializeField] private float minSpawnInterval  = 0.35f;
        [SerializeField] private float difficultyRampTime = 12f;   // seconds over which interval shrinks

        [Header("Simultaneous Targets")]
        [SerializeField] private int initialMaxTargets = 2;
        [SerializeField] private int maxTargetsCap     = 6;

        private readonly List<TargetController> activeTargets = new();
        private Coroutine spawnRoutine;
        private float     elapsedTime;
        private bool      spawning;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void StartSpawning()
        {
            spawning    = true;
            elapsedTime = 0f;
            spawnRoutine = StartCoroutine(SpawnLoop());
        }

        public void StopSpawning()
        {
            spawning = false;
            if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        }

        public void RegisterTarget(TargetController t)   { if (!activeTargets.Contains(t)) activeTargets.Add(t); }
        public void UnregisterTarget(TargetController t) => activeTargets.Remove(t);

        public int ActiveCount => activeTargets.Count;

        // ── Spawn Loop ────────────────────────────────────────────────────────

        private IEnumerator SpawnLoop()
        {
            yield return new WaitForSeconds(initialSpawnDelay);

            while (spawning)
            {
                elapsedTime += Time.deltaTime;

                float t        = Mathf.Clamp01(elapsedTime / difficultyRampTime);
                float interval = Mathf.Lerp(baseSpawnInterval, minSpawnInterval, t);
                int   maxSim   = Mathf.RoundToInt(Mathf.Lerp(initialMaxTargets, maxTargetsCap, t));

                if (activeTargets.Count < maxSim)
                    SpawnTarget(t);

                yield return new WaitForSeconds(interval);
            }
        }

        private void SpawnTarget(float difficulty)
        {
            // Weighted random: higher difficulty → more bonus/bomb types
            int prefabIdx = ChoosePrefab(difficulty);
            TargetController prefab = targetPrefabs[prefabIdx];

            Vector3 pos = GetSafeSpawnPosition();
            TargetController instance = PoolManager.Instance != null
                ? PoolManager.Instance.Get(prefab.gameObject, pos, Quaternion.identity)?.GetComponent<TargetController>()
                : Instantiate(prefab, pos, Quaternion.identity, targetParent);

            if (instance == null) return;
            instance.transform.SetParent(targetParent);
            instance.Initialise(difficulty);
            RegisterTarget(instance);
        }

        private int ChoosePrefab(float difficulty)
        {
            if (targetPrefabs.Length == 1) return 0;

            // At difficulty=0 → 90% normal, 10% bonus
            // At difficulty=1 → 50% normal, 30% bonus, 20% bomb
            float roll = Random.value;
            if (targetPrefabs.Length >= 3 && roll > Mathf.Lerp(1f, 0.5f, difficulty)) return 0;  // normal
            if (targetPrefabs.Length >= 2 && roll > Mathf.Lerp(0.9f, 0.2f, difficulty)) return 1; // bonus
            return targetPrefabs.Length >= 3 ? 2 : 0;                                             // bomb or normal
        }

        private Vector3 GetSafeSpawnPosition()
        {
            const int maxAttempts = 20;
            float minDist = 1.2f;

            for (int i = 0; i < maxAttempts; i++)
            {
                Vector3 candidate = new Vector3(
                    Random.Range(spawnAreaMin.x + edgePadding, spawnAreaMax.x - edgePadding),
                    Random.Range(spawnAreaMin.y + edgePadding, spawnAreaMax.y - edgePadding),
                    0f);

                bool tooClose = false;
                foreach (var existing in activeTargets)
                {
                    if (existing == null) continue;
                    if (Vector3.Distance(candidate, existing.transform.position) < minDist)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (!tooClose) return candidate;
            }

            // Fallback — random position
            return new Vector3(
                Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                Random.Range(spawnAreaMin.y, spawnAreaMax.y), 0f);
        }
    }
}
