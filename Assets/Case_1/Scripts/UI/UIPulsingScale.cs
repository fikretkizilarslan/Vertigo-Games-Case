using System.Collections;
using UnityEngine;

namespace VertigoCase.UI
{
    /// <summary>
    /// Attach this script to any UI element (like Red Dots / Notification Badges)
    /// to make them pulse in periodic cycles (e.g., pulse 2 times, wait 2 seconds, repeat).
    /// </summary>
    public class UIPulsingScale : MonoBehaviour
    {
        [Header("Pulse Settings")]
        [SerializeField] private float pulseAmount = 0.15f;        // Max scale multiplier (e.g., 0.15 = +/- 15%)
        [SerializeField] private int pulsesPerCycle = 2;           // Number of pulses in a row
        [SerializeField] private float singlePulseDuration = 0.4f;  // Duration of one complete pulse (up and down)
        [SerializeField] private float delayBetweenCycles = 2.0f;   // Waiting time (rest) between cycles in seconds

        private Vector3 originalScale;
        private bool hasCachedScale = false;
        private Coroutine pulseCoroutine;

        private void Awake()
        {
            CacheOriginalScale();
        }

        private void OnEnable()
        {
            CacheOriginalScale();
            transform.localScale = originalScale;
            StartPulse();
        }

        private void OnDisable()
        {
            StopPulse();
            if (hasCachedScale)
            {
                transform.localScale = originalScale;
            }
        }

        private void StartPulse()
        {
            StopPulse();
            pulseCoroutine = StartCoroutine(PulseLoopRoutine());
        }

        private void StopPulse()
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }
        }

        private IEnumerator PulseLoopRoutine()
        {
            while (true)
            {
                // 1. Pulse Phase: Scale up and down consecutively
                for (int i = 0; i < pulsesPerCycle; i++)
                {
                    float elapsed = 0f;
                    while (elapsed < singlePulseDuration)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        float t = elapsed / singlePulseDuration;
                        
                        // Map t [0, 1] to sine wave [0, PI] range.
                        // Sin(0) = 0, Sin(PI/2) = 1, Sin(PI) = 0 for smooth bounce back.
                        float angle = t * Mathf.PI;
                        float scaleMultiplier = 1f + Mathf.Sin(angle) * pulseAmount;
                        
                        transform.localScale = originalScale * scaleMultiplier;
                        yield return null;
                    }
                    
                    // Reset to exact original scale after each single pulse
                    transform.localScale = originalScale;
                }

                // 2. Rest Phase: Wait for the specified delay between cycles
                yield return new WaitForSecondsRealtime(delayBetweenCycles);
            }
        }

        private void CacheOriginalScale()
        {
            if (!hasCachedScale)
            {
                originalScale = transform.localScale;
                hasCachedScale = true;
            }
        }
    }
}
