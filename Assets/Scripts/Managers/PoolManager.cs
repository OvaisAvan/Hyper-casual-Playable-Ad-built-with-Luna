using UnityEngine;
using System.Collections.Generic;

namespace TapBlitz.Managers
{
    /// <summary>
    /// Generic GameObject pool.
    /// Pools are created per-prefab on first request.
    /// Keeps WebGL GC pressure low during rapid target spawning.
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }

        [System.Serializable]
        public struct PoolDefinition
        {
            public GameObject prefab;
            public int        initialSize;
        }

        [SerializeField] private PoolDefinition[] prewarmedPools;

        private readonly Dictionary<int, Queue<GameObject>> pools = new();
        private readonly Dictionary<int, GameObject>        prefabMap = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            foreach (var def in prewarmedPools)
                if (def.prefab != null)
                    Prewarm(def.prefab, def.initialSize);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            int id = prefab.GetInstanceID();
            EnsurePool(prefab);

            Queue<GameObject> pool = pools[id];
            GameObject go;

            if (pool.Count > 0)
            {
                go = pool.Dequeue();
                go.transform.SetPositionAndRotation(position, rotation);
                go.SetActive(true);
            }
            else
            {
                go = Instantiate(prefab, position, rotation, transform);
                go.name = prefab.name + "_pooled";
            }

            return go;
        }

        public void Return(GameObject go)
        {
            if (go == null) return;

            // Find which pool this belongs to by name prefix
            foreach (var kv in prefabMap)
            {
                if (go.name.StartsWith(kv.Value.name))
                {
                    go.SetActive(false);
                    go.transform.SetParent(transform);
                    pools[kv.Key].Enqueue(go);
                    return;
                }
            }

            // Not pooled — just destroy
            Destroy(go);
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void Prewarm(GameObject prefab, int count)
        {
            EnsurePool(prefab);
            int id = prefab.GetInstanceID();
            for (int i = 0; i < count; i++)
            {
                GameObject go = Instantiate(prefab, transform);
                go.name = prefab.name + "_pooled";
                go.SetActive(false);
                pools[id].Enqueue(go);
            }
        }

        private void EnsurePool(GameObject prefab)
        {
            int id = prefab.GetInstanceID();
            if (!pools.ContainsKey(id))
            {
                pools[id]     = new Queue<GameObject>();
                prefabMap[id] = prefab;
            }
        }
    }
}
