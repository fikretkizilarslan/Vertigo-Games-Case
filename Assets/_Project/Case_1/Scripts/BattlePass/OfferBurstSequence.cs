using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePass.UI
{
    /// <summary>
    /// One-shot premium-offer hero moment wired in the scene under the battle-pass canvas.
    /// Uses a full-opacity tiled backdrop under Img_Dim plus a fully opaque lock icon so the
    /// parent CanvasGroup does not wash out the lock.
    /// </summary>
    [DisallowMultipleComponent]
    public class OfferBurstSequence : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform lockRect;
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private CanvasGroup lockGroup;
        [SerializeField] private Image solidFill;
        [SerializeField] private Graphic patternOverlay;
        [SerializeField] private GameObject vfxSlot;
        [SerializeField] private Image lockImage;

        [Header("Timing")]
        [SerializeField] [Range(0f, 1f)] private float backdropMaxAlpha = 1f;
        [SerializeField] private float popInDuration = 0.22f;
        [SerializeField] private float shakeDuration = 0.32f;
        [SerializeField] private float explodeDuration = 0.28f;
        [SerializeField] private float fadeOutDuration = 0.2f;

        private Coroutine playRoutine;
        private Vector3 lockBaseScale = Vector3.one;
        private Quaternion lockBaseRotation = Quaternion.identity;
        private Color solidBaseColor = Color.white;
        private Color patternBaseColor = Color.white;
        private bool isInitialized;

        public bool IsPlaying => playRoutine != null;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            if (isInitialized)
            {
                ResetToIdle();
            }
        }

        public void Play(Action onComplete = null)
        {
            if (!Initialize())
            {
                onComplete?.Invoke();
                return;
            }

            if (playRoutine != null)
            {
                StopCoroutine(playRoutine);
            }

            playRoutine = StartCoroutine(PlayRoutine(onComplete));
        }

        private bool Initialize()
        {
            Transform dimTransform = transform.Find("Img_Dim");
            if (solidFill == null && dimTransform != null)
            {
                solidFill = dimTransform.GetComponent<Image>();
            }

            if (patternOverlay == null && dimTransform != null)
            {
                Transform tileTransform = dimTransform.Find("Img_Background_Tile");
                if (tileTransform != null)
                {
                    patternOverlay = tileTransform.GetComponent<Graphic>();
                }
            }

            if (lockRect == null)
            {
                Transform lockTransform = transform.Find("Img_Lock");
                if (lockTransform != null)
                {
                    lockRect = lockTransform as RectTransform;
                }
            }

            if (overlayGroup == null)
            {
                overlayGroup = GetComponent<CanvasGroup>();
            }

            if (lockGroup == null && lockRect != null)
            {
                lockGroup = lockRect.GetComponent<CanvasGroup>();
            }

            if (lockImage == null && lockRect != null)
            {
                lockImage = lockRect.GetComponent<Image>();
            }

            if (vfxSlot == null)
            {
                Transform slot = transform.Find("Grp_VFX_Slot");
                if (slot != null)
                {
                    vfxSlot = slot.gameObject;
                }
            }

            if (lockRect == null || overlayGroup == null || lockGroup == null || solidFill == null)
            {
                Debug.LogWarning("[OfferBurstSequence] Scene references are missing. Assign Grp_OfferBurst hierarchy in BattlePass Scene.");
                return false;
            }

            lockGroup.ignoreParentGroups = true;

            solidBaseColor = solidFill.color;
            solidBaseColor.a = 1f;

            if (patternOverlay != null)
            {
                patternBaseColor = patternOverlay.color;
                patternBaseColor.a = 1f;
            }

            if (lockImage != null)
            {
                Color lockColor = lockImage.color;
                lockColor.a = 1f;
                lockImage.color = lockColor;
            }

            lockBaseScale = lockRect.localScale.sqrMagnitude > 0.0001f ? lockRect.localScale : Vector3.one;
            lockBaseRotation = lockRect.localRotation;
            isInitialized = true;
            ResetToIdle();
            return true;
        }

        private void ResetToIdle()
        {
            if (overlayGroup != null)
            {
                overlayGroup.alpha = 0f;
                overlayGroup.blocksRaycasts = false;
                overlayGroup.interactable = false;
            }

            SetBackdropAlpha(0f);

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

        private void SetBackdropAlpha(float alpha)
        {
            float a = Mathf.Clamp01(alpha) * backdropMaxAlpha;

            if (solidFill != null)
            {
                Color c = solidBaseColor;
                c.a = a;
                solidFill.color = c;
            }

            if (patternOverlay != null)
            {
                Color c = patternBaseColor;
                c.a = a;
                patternOverlay.color = c;
            }
        }

        private IEnumerator PlayRoutine(Action onComplete)
        {
            ResetToIdle();

            overlayGroup.alpha = 1f;
            overlayGroup.blocksRaycasts = true;
            overlayGroup.interactable = true;

            float elapsed = 0f;
            while (elapsed < popInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / popInDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                float overshoot = Mathf.Lerp(0f, 1.15f, eased);

                SetBackdropAlpha(eased);
                lockGroup.alpha = 1f;
                lockRect.localScale = lockBaseScale * overshoot;
                yield return null;
            }

            SetBackdropAlpha(backdropMaxAlpha);
            lockGroup.alpha = 1f;
            lockRect.localScale = lockBaseScale;

            elapsed = 0f;
            while (elapsed < shakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / shakeDuration;
                float shake = Mathf.Sin(t * Mathf.PI * 10f) * (1f - t) * 12f;
                lockRect.localRotation = lockBaseRotation * Quaternion.Euler(0f, 0f, shake);
                yield return null;
            }

            lockRect.localRotation = lockBaseRotation;

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

                lockRect.localScale = lockBaseScale * burst;
                lockGroup.alpha = 1f - t;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                SetBackdropAlpha(backdropMaxAlpha * (1f - t));
                yield return null;
            }

            ResetToIdle();
            playRoutine = null;
            onComplete?.Invoke();
        }
    }
}
