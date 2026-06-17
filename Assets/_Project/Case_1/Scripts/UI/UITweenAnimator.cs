using DG.Tweening;
using UnityEngine;

namespace BattlePass.UI
{
    /// <summary>
    /// Generic DOTween-driven looping animation for UI elements.
    /// Pick an <see cref="AnimationType"/> in the Inspector and tweak the parameters.
    /// Designed for things like Red Dots / notification badges, icons, buttons, etc.
    /// </summary>
    [DisallowMultipleComponent]
    public class UITweenAnimator : MonoBehaviour
    {
        public enum AnimationType
        {
            PulseScale,     // Smoothly grows then shrinks back (shrink/grow)
            Bounce,         // Quick elastic punch on the scale
            ShakeScale,     // Random scale jitter
            ShakeRotation,  // Random rotation jitter
            Spin,           // Continuous Z rotation
            SwingRotation,  // Rotates back and forth like a pendulum
            FadePulse       // Fades alpha down and back up (needs CanvasGroup)
        }

        [Header("Animation Type")]
        [Tooltip("Which animation this element should play.")]
        [SerializeField] private AnimationType animationType = AnimationType.PulseScale;

        [Header("General Timing")]
        [Tooltip("Duration (seconds) of one full motion (e.g. one grow+shrink, one swing).")]
        [SerializeField] private float duration = 0.4f;
        [Tooltip("How many times the motion repeats before resting (cyclic types only).")]
        [SerializeField] private int repeatCount = 2;
        [Tooltip("Rest time (seconds) between cycles. Set 0 for continuous looping.")]
        [SerializeField] private float delayBetweenCycles = 2f;
        [SerializeField] private Ease ease = Ease.InOutSine;
        [Tooltip("Use unscaled time so it runs even when Time.timeScale is 0.")]
        [SerializeField] private bool useUnscaledTime = true;
        [Tooltip("Start the animation automatically when the object is enabled.")]
        [SerializeField] private bool playOnEnable = true;

        [Header("Scale  (PulseScale / Bounce / ShakeScale)")]
        [Tooltip("Scale strength. 0.15 = +/-15% of the original scale.")]
        [SerializeField] private float scaleAmount = 0.15f;

        [Header("Rotation  (Spin / SwingRotation / ShakeRotation)")]
        [Tooltip("Angle in degrees used by Swing and Shake rotation.")]
        [SerializeField] private float rotationAngle = 15f;
        [Tooltip("Spin speed in degrees per second (Spin type). Negative = counter-clockwise.")]
        [SerializeField] private float spinSpeed = 180f;

        [Header("Fade  (FadePulse)")]
        [Tooltip("Lowest alpha reached during a fade pulse (0..1).")]
        [SerializeField] private float minAlpha = 0.3f;

        private CanvasGroup canvasGroup;
        private Vector3 baseScale;
        private Vector3 restScale;
        private bool hasRestScale;
        private Vector3 baseEuler;
        private Tween tween;

        private void Awake()
        {
            CaptureRestScale();
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            Stop();
        }

        /// <summary>Starts (or restarts) the currently selected animation.</summary>
        public void Play()
        {
            Stop();
            EnsureRestScale();
            transform.localScale = restScale;
            baseScale = restScale;

            switch (animationType)
            {
                case AnimationType.PulseScale:    tween = BuildPulseScale();    break;
                case AnimationType.Bounce:        tween = BuildBounce();        break;
                case AnimationType.ShakeScale:    tween = BuildShakeScale();    break;
                case AnimationType.ShakeRotation: tween = BuildShakeRotation(); break;
                case AnimationType.Spin:          tween = BuildSpin();          break;
                case AnimationType.SwingRotation: tween = BuildSwingRotation(); break;
                case AnimationType.FadePulse:     tween = BuildFadePulse();     break;
            }

            if (tween != null)
            {
                tween.SetUpdate(useUnscaledTime);
                tween.SetLink(gameObject);
            }
        }

        /// <summary>Stops the animation and restores the original transform / alpha.</summary>
        public void Stop()
        {
            if (tween != null)
            {
                tween.Kill();
                tween = null;
            }
            ResetState();
        }

        private Tween BuildPulseScale()
        {
            float leg = Mathf.Max(0.01f, duration) * 0.5f;
            Vector3 peak = baseScale * (1f + scaleAmount);
            int pulses = Mathf.Max(1, repeatCount);

            Sequence seq = DOTween.Sequence();
            for (int i = 0; i < pulses; i++)
            {
                seq.Append(transform.DOScale(peak, leg).SetEase(ease));
                seq.Append(transform.DOScale(baseScale, leg).SetEase(ease));
            }
            AppendRest(seq);
            return seq.SetLoops(-1, LoopType.Restart);
        }

        private Tween BuildBounce()
        {
            int pulses = Mathf.Max(1, repeatCount);
            Sequence seq = DOTween.Sequence();
            for (int i = 0; i < pulses; i++)
            {
                seq.Append(transform.DOPunchScale(Vector3.one * scaleAmount, Mathf.Max(0.01f, duration), 6, 0.8f));
                seq.Append(transform.DOScale(baseScale, 0.01f));
            }
            AppendRest(seq);
            return seq.SetLoops(-1, LoopType.Restart);
        }

        private Tween BuildShakeScale()
        {
            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOShakeScale(Mathf.Max(0.01f, duration), scaleAmount, 10, 90f, true));
            seq.Append(transform.DOScale(baseScale, 0.01f));
            AppendRest(seq);
            return seq.SetLoops(-1, LoopType.Restart);
        }

        private Tween BuildShakeRotation()
        {
            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOShakeRotation(Mathf.Max(0.01f, duration), new Vector3(0f, 0f, rotationAngle), 10, 90f));
            seq.Append(transform.DOLocalRotate(baseEuler, 0.01f));
            AppendRest(seq);
            return seq.SetLoops(-1, LoopType.Restart);
        }

        private Tween BuildSpin()
        {
            float speed = Mathf.Approximately(spinSpeed, 0f) ? 180f : spinSpeed;
            float fullTurn = 360f / Mathf.Abs(speed);
            float sign = speed >= 0f ? -1f : 1f; // negative Z = clockwise
            return transform.DOLocalRotate(new Vector3(0f, 0f, 360f * sign), fullTurn, RotateMode.FastBeyond360)
                            .SetEase(Ease.Linear)
                            .SetLoops(-1, LoopType.Restart);
        }

        private Tween BuildSwingRotation()
        {
            transform.localEulerAngles = baseEuler + new Vector3(0f, 0f, -rotationAngle);
            return transform.DOLocalRotate(baseEuler + new Vector3(0f, 0f, rotationAngle), Mathf.Max(0.01f, duration))
                            .SetEase(ease)
                            .SetLoops(-1, LoopType.Yoyo);
        }

        private Tween BuildFadePulse()
        {
            EnsureCanvasGroup();
            canvasGroup.alpha = 1f;
            return canvasGroup.DOFade(Mathf.Clamp01(minAlpha), Mathf.Max(0.01f, duration))
                              .SetEase(ease)
                              .SetLoops(-1, LoopType.Yoyo);
        }

        private void AppendRest(Sequence seq)
        {
            if (delayBetweenCycles > 0f)
            {
                seq.AppendInterval(delayBetweenCycles);
            }
        }

        private void CaptureRestScale()
        {
            restScale = transform.localScale;
            hasRestScale = true;
            baseScale = restScale;
            baseEuler = transform.localEulerAngles;
        }

        private void EnsureRestScale()
        {
            if (!hasRestScale)
            {
                CaptureRestScale();
            }
        }

        private void ResetState()
        {
            EnsureRestScale();
            transform.localScale = restScale;
            baseScale = restScale;
            transform.localEulerAngles = baseEuler;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        private void EnsureCanvasGroup()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            duration = Mathf.Max(0.01f, duration);
            repeatCount = Mathf.Max(1, repeatCount);
            delayBetweenCycles = Mathf.Max(0f, delayBetweenCycles);
            minAlpha = Mathf.Clamp01(minAlpha);
        }
#endif
    }
}
