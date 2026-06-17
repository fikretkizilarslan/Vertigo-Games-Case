using System.Collections.Generic;
using UnityEngine;

namespace BattlePass.UI
{
    /// <summary>
    /// Simple per-prefab object pool for short-lived VFX GameObjects.
    /// Replaces Instantiate+Destroy on every claim event so GC pressure
    /// stays flat during gameplay on mobile devices.
    ///
    /// Usage:
    ///   GameObject instance = pool.Get(prefab, worldPos);   // retrieve
    ///   // VfxAutoReturn handles release automatically via ParticleSystem duration
    ///
    /// The pool falls back to Instantiate when its queue is empty, so it is
    /// always safe to use even before warm-up.
    /// </summary>
    [DisallowMultipleComponent]
    public class VfxPool : MonoBehaviour
    {
        [Tooltip("Initial queue capacity per prefab. Resize is automatic when the queue is exhausted.")]
        [Min(1)] [SerializeField] private int defaultCapacity = 4;

        private readonly Dictionary<GameObject, Queue<GameObject>> _pools =
            new Dictionary<GameObject, Queue<GameObject>>();

        // ──────────────────────────────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Retrieves a pooled instance of <paramref name="prefab"/>, places it at
        /// <paramref name="worldPosition"/> and activates it. Falls back to
        /// Instantiate when the pool queue is empty.
        /// A <see cref="VfxAutoReturn"/> component is attached (or refreshed) so the
        /// instance returns itself to the pool once its ParticleSystems finish.
        /// </summary>
        public GameObject Get(GameObject prefab, Vector3 worldPosition)
        {
            if (prefab == null) return null;

            GameObject instance = DequeueOrInstantiate(prefab, worldPosition);

            // Attach or refresh the auto-return component so lifetime is always tracked.
            VfxAutoReturn autoReturn = instance.GetComponent<VfxAutoReturn>();
            if (autoReturn == null)
            {
                autoReturn = instance.AddComponent<VfxAutoReturn>();
            }
            autoReturn.Init(prefab, this);

            return instance;
        }

        /// <summary>
        /// Returns <paramref name="instance"/> to the pool for <paramref name="prefab"/>.
        /// Called automatically by <see cref="VfxAutoReturn"/>; safe to call manually.
        /// </summary>
        public void Release(GameObject instance, GameObject prefab)
        {
            if (instance == null || prefab == null) return;

            instance.SetActive(false);

            if (!_pools.TryGetValue(prefab, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>(defaultCapacity);
                _pools[prefab] = queue;
            }

            queue.Enqueue(instance);
        }

        // ──────────────────────────────────────────────────────────────────────
        // Internal helpers
        // ──────────────────────────────────────────────────────────────────────

        private GameObject DequeueOrInstantiate(GameObject prefab, Vector3 worldPosition)
        {
            if (_pools.TryGetValue(prefab, out Queue<GameObject> queue) && queue.Count > 0)
            {
                GameObject pooled = queue.Dequeue();

                // Guard against pooled instances destroyed from outside (scene reload, etc.)
                if (pooled != null)
                {
                    pooled.transform.position = worldPosition;
                    pooled.SetActive(true);
                    return pooled;
                }
            }

            // Pool empty or instance was destroyed — create a fresh one.
            GameObject fresh = Instantiate(prefab, worldPosition, Quaternion.identity, transform);
            return fresh;
        }

        // ──────────────────────────────────────────────────────────────────────
        // Lifecycle
        // ──────────────────────────────────────────────────────────────────────

        private void OnDestroy()
        {
            foreach (var queue in _pools.Values)
            {
                while (queue.Count > 0)
                {
                    GameObject obj = queue.Dequeue();
                    if (obj != null) Destroy(obj);
                }
            }
            _pools.Clear();
        }
    }
}
