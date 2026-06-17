using System.Collections;
using UnityEngine;

namespace BattlePass.UI
{
    /// <summary>
    /// Attached automatically by <see cref="VfxPool"/> to every pooled VFX instance.
    /// Measures the total play duration across all child ParticleSystems and returns
    /// the GameObject to the pool once they have all finished playing.
    ///
    /// This component replaces the old Destroy(instance, duration) pattern, keeping
    /// each VFX instance alive for reuse instead of being garbage-collected.
    /// </summary>
    [DisallowMultipleComponent]
    public class VfxAutoReturn : MonoBehaviour
    {
        private const float FallbackDuration = 3f;

        private GameObject _sourcePrefab;
        private VfxPool _pool;
        private Coroutine _returnRoutine;

        // ──────────────────────────────────────────────────────────────────────
        // Pool API
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Called by <see cref="VfxPool"/> each time the instance is retrieved from the pool.
        /// Restarts the return countdown so duration is always based on the current activation.
        /// </summary>
        public void Init(GameObject sourcePrefab, VfxPool pool)
        {
            _sourcePrefab = sourcePrefab;
            _pool = pool;

            if (_returnRoutine != null)
            {
                StopCoroutine(_returnRoutine);
            }

            _returnRoutine = StartCoroutine(ReturnAfterDuration());
        }

        // ──────────────────────────────────────────────────────────────────────
        // Internal
        // ──────────────────────────────────────────────────────────────────────

        private IEnumerator ReturnAfterDuration()
        {
            float duration = CalculateDuration();
            yield return new WaitForSeconds(duration);

            _returnRoutine = null;
            _pool?.Release(gameObject, _sourcePrefab);
        }

        /// <summary>
        /// Scans all child ParticleSystems and returns the longest play time.
        /// Looping systems are excluded — they would never auto-return.
        /// Falls back to <see cref="FallbackDuration"/> when no systems are found.
        /// </summary>
        private float CalculateDuration()
        {
            ParticleSystem[] allPS = GetComponentsInChildren<ParticleSystem>(true);
            if (allPS == null || allPS.Length == 0) return FallbackDuration;

            float maxDuration = 0f;
            foreach (var ps in allPS)
            {
                if (ps == null) continue;

                var main = ps.main;
                if (main.loop) continue; // Looping systems have infinite duration; skip them.

                float psDuration = main.duration;
                float lifetime = Mathf.Max(main.startLifetime.constant, main.startLifetime.constantMax);
                psDuration += lifetime;

                if (psDuration > maxDuration) maxDuration = psDuration;
            }

            return maxDuration > 0f ? maxDuration : FallbackDuration;
        }
    }
}
