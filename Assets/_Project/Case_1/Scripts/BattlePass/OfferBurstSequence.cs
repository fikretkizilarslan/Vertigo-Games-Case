using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VertigoCase.UI
{
    /// <summary>
    /// One-shot premium-offer hero moment: dim overlay, lock icon pop, shake, explode.
    /// VFX can be parented under <see cref="vfxSlot"/>; it is toggled at the burst frame.
    /// </summary>
    [DisallowMultipleComponent]
    public class OfferBurstSequence : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform lockRect;
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private CanvasGroup lockGroup;
        [SerializeField] private GameObject vfxSlot;
        [SerializeField] private Image lockImage;
        [SerializeField] private Sprite lockSprite;

        [Header("Timing")]
        [SerializeField] private float dimAlpha = 0.45f;
        [SerializeField] private float popInDuration = 0.22f;
        [SerializeField] private float shakeDuration = 0.32f;
        [SerializeField] private float explodeDuration = 0.28f;
        [SerializeField] private float fadeOutDuration = 0.2f;

        private RectTransform sequenceRect;
        private Coroutine playRoutine;
        private Vector3 lockBaseScale = Vector3.one;
        private Quaternion lockBaseRotation = Quaternion.identity;

        public bool IsPlaying => playRoutine != null;

        public void Configure(Sprite sprite)
        {
            if (sprite == null) return;
            lockSprite = sprite;
            EnsureHierarchy();
            if (lockImage != null)
            {
                lockImage.sprite = lockSprite;
            }
        }

        private void Awake()
        {
            EnsureHierarchy();
            CaptureBase();
            ResetToIdle();
        }

        public void Play(Action onComplete = null)
        {
            EnsureHierarchy();
            CaptureBase();

            if (playRoutine != null)
            {
                StopCoroutine(playRoutine);
            }

            playRoutine = StartCoroutine(PlayRoutine(onComplete));
        }

        private void EnsureHierarchy()
        {
            if (sequenceRect == null)
            {
                sequenceRect = transform as RectTransform;
            }

            if (sequenceRect == null)
            {
                sequenceRect = gameObject.AddComponent<RectTransform>();
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }

            if (canvas != null && transform.parent != canvas.transform)
            {
                transform.SetParent(canvas.transform, false);
            }

            StretchToParent(sequenceRect);

            if (overlayGroup == null)
            {
                overlayGroup = GetComponent<CanvasGroup>();
                if (overlayGroup == null)
                {
                    overlayGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (lockRect == null)
            {
                Transform existing = transform.Find("Img_Lock");
                if (existing != null)
                {
                    lockRect = existing as RectTransform;
                }
            }

            if (lockRect == null)
            {
                GameObject lockGo = new GameObject("Img_Lock", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
                lockGo.transform.SetParent(transform, false);
                lockRect = lockGo.GetComponent<RectTransform>();
                lockRect.anchorMin = new Vector2(0.5f, 0.5f);
                lockRect.anchorMax = new Vector2(0.5f, 0.5f);
                lockRect.pivot = new Vector2(0.5f, 0.5f);
                lockRect.sizeDelta = new Vector2(180f, 180f);
                lockRect.anchoredPosition = Vector2.zero;

                lockGroup = lockGo.GetComponent<CanvasGroup>();
                lockImage = lockGo.GetComponent<Image>();
                lockImage.raycastTarget = false;
                lockImage.preserveAspect = true;
            }

            if (lockGroup == null && lockRect != null)
            {
                lockGroup = lockRect.GetComponent<CanvasGroup>();
                if (lockGroup == null)
                {
                    lockGroup = lockRect.gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (lockImage == null && lockRect != null)
            {
                lockImage = lockRect.GetComponent<Image>();
            }

            if (lockImage != null && lockSprite != null)
            {
                lockImage.sprite = lockSprite;
            }

            if (vfxSlot == null)
            {
                Transform slot = transform.Find("Grp_VFX_Slot");
                vfxSlot = slot != null ? slot.gameObject : null;
            }

            if (vfxSlot == null)
            {
                GameObject slotGo = new GameObject("Grp_VFX_Slot", typeof(RectTransform));
                slotGo.transform.SetParent(transform, false);
                RectTransform slotRect = slotGo.GetComponent<RectTransform>();
                slotRect.anchorMin = new Vector2(0.5f, 0.5f);
                slotRect.anchorMax = new Vector2(0.5f, 0.5f);
                slotRect.pivot = new Vector2(0.5f, 0.5f);
                slotRect.sizeDelta = new Vector2(256f, 256f);
                slotRect.anchoredPosition = Vector2.zero;
                vfxSlot = slotGo;
            }

            transform.SetAsLastSibling();
        }

        private void CaptureBase()
        {
            if (lockRect != null)
            {
                lockBaseScale = lockRect.localScale;
                lockBaseRotation = lockRect.localRotation;
            }
        }

        private void ResetToIdle()
        {
            if (overlayGroup != null)
            {
                overlayGroup.alpha = 0f;
                overlayGroup.blocksRaycasts = false;
                overlayGroup.interactable = false;
            }

            if (lockGroup != null)
            {
                lockGroup.alpha = 0f;
            }

            if (lockRect != null)
            {
                lockRect.localScale = Vector3.zero;
                lockRect.localRotation = lockBaseRotation;
            }

            if (vfxSlot != null)
            {
                vfxSlot.SetActive(false);
            }
        }

        private IEnumerator PlayRoutine(Action onComplete)
        {
            ResetToIdle();

            if (overlayGroup != null)
            {
                overlayGroup.blocksRaycasts = true;
                overlayGroup.interactable = true;
            }

            float elapsed = 0f;
            while (elapsed < popInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / popInDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                float overshoot = Mathf.Lerp(0f, 1.15f, eased);

                if (overlayGroup != null)
                {
                    overlayGroup.alpha = dimAlpha * eased;
                }

                if (lockGroup != null)
                {
                    lockGroup.alpha = eased;
                }

                if (lockRect != null)
                {
                    lockRect.localScale = lockBaseScale * overshoot;
                }

                yield return null;
            }

            if (lockRect != null)
            {
                lockRect.localScale = lockBaseScale;
            }

            elapsed = 0f;
            while (elapsed < shakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / shakeDuration;
                float shake = Mathf.Sin(t * Mathf.PI * 10f) * (1f - t) * 12f;

                if (lockRect != null)
                {
                    lockRect.localRotation = lockBaseRotation * Quaternion.Euler(0f, 0f, shake);
                }

                yield return null;
            }

            if (lockRect != null)
            {
                lockRect.localRotation = lockBaseRotation;
            }

            if (vfxSlot != null)
            {
                vfxSlot.SetActive(true);
            }

            elapsed = 0f;
            while (elapsed < explodeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / explodeDuration);
                float burst = 1f + t * 0.85f;

                if (lockRect != null)
                {
                    lockRect.localScale = lockBaseScale * burst;
                }

                if (lockGroup != null)
                {
                    lockGroup.alpha = 1f - t;
                }

                yield return null;
            }

            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);

                if (overlayGroup != null)
                {
                    overlayGroup.alpha = dimAlpha * (1f - t);
                }

                yield return null;
            }

            ResetToIdle();
            playRoutine = null;
            onComplete?.Invoke();
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }
    }
}
