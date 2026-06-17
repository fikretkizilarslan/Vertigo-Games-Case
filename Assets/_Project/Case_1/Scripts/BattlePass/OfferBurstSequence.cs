using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePass.UI
{
    /// <summary>
    /// Premium-offer hero moment under the battle-pass canvas.
    /// Hides the road scroll while active so reward cards never bleed through the overlay.
    /// Grp_OfferBurst can stay disabled in the scene while editing; GET enables it at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public class OfferBurstSequence : MonoBehaviour
    {
        [Header("Backdrop")]
        [SerializeField] private Image solidFill;
        [SerializeField] private Graphic patternOverlay;
        [SerializeField] [Range(0f, 1f)] private float backdropMaxAlpha = 1f;

        [Header("Lock")]
        [SerializeField] private RectTransform lockRect;
        [SerializeField] private CanvasGroup lockGroup;
        [SerializeField] private Image lockImage;
        [SerializeField] private GameObject lockBurstVfxSlot;

        [Header("Reward Reveal")]
        [SerializeField] private CanvasGroup rewardRevealGroup;
        [SerializeField] private RectTransform ticketRoot;
        [SerializeField] private Image ticketGlowImage;
        [SerializeField] private Image ticketImage;
        [SerializeField] private GameObject ticketVfxRoot;
        [SerializeField] private TMP_Text premiumStatusText;
        [SerializeField] private Button claimButton;
        [SerializeField] private TMP_Text claimButtonLabel;
        [SerializeField] private string premiumStatusMessage = "Premium Active";
        [SerializeField] private string claimLabel = "CLAIM";

        [Header("Overlay")]
        [SerializeField] private CanvasGroup overlayGroup;
        [Tooltip("Panel_Main — hidden during the overlay so road reward cards never bleed through.")]
        [SerializeField] private GameObject battlePassMainPanel;
        [SerializeField] private int overlaySortingOrder = 200;

        [Header("Timing — Backdrop & Lock")]
        [SerializeField] private float popInDuration = 0.22f;
        [SerializeField] private float shakeDuration = 0.32f;
        [SerializeField] private float explodeDuration = 0.34f;
        [SerializeField] private float lockBurstPeakScale = 1.55f;
        [SerializeField] private float lockBurstRotation = 28f;

        [Header("Timing — Reward & Dismiss")]
        [SerializeField] private float rewardRevealDuration = 0.35f;
        [SerializeField] private float claimButtonDelay = 0.12f;
        [SerializeField] private float fadeOutDuration = 0.22f;

        private Coroutine playRoutine;
        private Vector3 lockBaseScale = Vector3.one;
        private Quaternion lockBaseRotation = Quaternion.identity;
        private Vector3 ticketBaseScale = Vector3.one;
        private Color solidBaseColor = Color.white;
        private Color patternBaseColor = Color.white;
        private bool isInitialized;
        private bool claimRequested;
        private bool mainPanelWasHiddenByOverlay;

        public bool IsPlaying => playRoutine != null;

        private void Awake()
        {
            if (gameObject.activeInHierarchy)
            {
                Initialize();
            }
        }

        private void OnEnable()
        {
            if (isInitialized && playRoutine == null)
            {
                ResetToIdle();
            }
        }

        public void Play(Action onComplete = null)
        {
            EnsurePlaybackReady();

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

        private void EnsurePlaybackReady()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (!child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(true);
                }
            }

            if (rewardRevealGroup != null && !rewardRevealGroup.gameObject.activeSelf)
            {
                rewardRevealGroup.gameObject.SetActive(true);
            }
        }

        private bool Initialize()
        {
            Transform dimTransform = FindChildByPath("Img_Dim");
            if (solidFill == null && dimTransform != null)
            {
                solidFill = dimTransform.GetComponent<Image>();
            }

            if (patternOverlay == null && dimTransform != null)
            {
                Transform tileTransform = FindDirectChild(dimTransform, "Img_Background_Tile");
                if (tileTransform != null)
                {
                    patternOverlay = tileTransform.GetComponent<Graphic>();
                }
            }

            if (lockRect == null)
            {
                Transform lockTransform = FindChildByPath("Img_Lock");
                if (lockTransform != null)
                {
                    lockRect = lockTransform as RectTransform;
                }
            }

            if (lockBurstVfxSlot == null)
            {
                Transform slot = FindChildByPath("Grp_VFX_Slot");
                if (slot != null)
                {
                    lockBurstVfxSlot = slot.gameObject;
                }
            }

            if (rewardRevealGroup == null)
            {
                Transform rewardTransform = FindChildByPath("Grp_RewardReveal");
                if (rewardTransform != null)
                {
                    rewardRevealGroup = rewardTransform.GetComponent<CanvasGroup>();
                }
            }

            if (ticketRoot == null)
            {
                Transform ticketTransform = FindChildByPath("Grp_RewardReveal/Grp_Ticket");
                if (ticketTransform != null)
                {
                    ticketRoot = ticketTransform as RectTransform;
                }
            }

            if (ticketGlowImage == null)
            {
                Transform glowTransform = FindChildByPath("Grp_RewardReveal/Grp_Ticket/Img_Ticket_Glow");
                if (glowTransform != null)
                {
                    ticketGlowImage = glowTransform.GetComponent<Image>();
                }
            }

            if (ticketImage == null)
            {
                Transform ticketImageTransform = FindChildByPath("Grp_RewardReveal/Grp_Ticket/Img_Ticket");
                if (ticketImageTransform != null)
                {
                    ticketImage = ticketImageTransform.GetComponent<Image>();
                }
            }

            if (ticketVfxRoot == null)
            {
                Transform vfxTransform = FindChildByPath("Grp_RewardReveal/Grp_Ticket/Grp_Ticket_VFX");
                if (vfxTransform != null)
                {
                    ticketVfxRoot = vfxTransform.gameObject;
                }
            }

            if (premiumStatusText == null)
            {
                Transform textTransform = FindChildByPath("Grp_RewardReveal/Txt_PremiumStatus");
                if (textTransform != null)
                {
                    premiumStatusText = textTransform.GetComponent<TMP_Text>();
                }
            }

            if (claimButton == null)
            {
                Transform buttonTransform = FindChildByPath("Grp_RewardReveal/Btn_Claim");
                if (buttonTransform != null)
                {
                    claimButton = buttonTransform.GetComponent<Button>();
                }
            }

            if (claimButtonLabel == null && claimButton != null)
            {
                claimButtonLabel = claimButton.GetComponentInChildren<TMP_Text>(true);
            }

            if (battlePassMainPanel == null)
            {
                Transform panelTransform = transform.parent != null
                    ? FindDirectChildInParent("Panel_Main")
                    : null;
                if (panelTransform != null)
                {
                    battlePassMainPanel = panelTransform.gameObject;
                }
            }

            if (overlayGroup == null)
            {
                overlayGroup = GetComponent<CanvasGroup>();
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

            if (lockRect == null || overlayGroup == null || lockGroup == null || solidFill == null)
            {
                Debug.LogWarning("[OfferBurstSequence] Scene references are missing. Run BattlePass/Setup Offer Burst Reward UI or assign Grp_OfferBurst in the Inspector.");
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

            if (premiumStatusText != null && !string.IsNullOrWhiteSpace(premiumStatusMessage))
            {
                premiumStatusText.text = premiumStatusMessage;
                ConfigurePremiumStatusText(premiumStatusText);
            }

            if (claimButtonLabel != null && !string.IsNullOrWhiteSpace(claimLabel))
            {
                claimButtonLabel.text = claimLabel;
            }

            lockBaseScale = lockRect.localScale.sqrMagnitude > 0.0001f ? lockRect.localScale : Vector3.one;
            lockBaseRotation = lockRect.localRotation;
            ticketBaseScale = ticketRoot != null && ticketRoot.localScale.sqrMagnitude > 0.0001f
                ? ticketRoot.localScale
                : Vector3.one;

            if (claimButton != null)
            {
                claimButton.onClick.RemoveListener(OnClaimClicked);
                claimButton.onClick.AddListener(OnClaimClicked);
            }

            RemoveNestedSortingCanvases();
            ConfigureTicketLayering();
            EnsureOverlayCanvasOnTop();

            isInitialized = true;
            ResetToIdle();
            return true;
        }

        private Transform FindDirectChildInParent(string childName)
        {
            Transform parent = transform.parent;
            if (parent == null)
            {
                return null;
            }

            return FindDirectChild(parent, childName);
        }

        private Transform FindChildByPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            string[] parts = path.Split('/');
            Transform current = transform;
            foreach (string part in parts)
            {
                if (current == null)
                {
                    return null;
                }

                current = FindDirectChild(current, part);
            }

            return current;
        }

        private static Transform FindDirectChild(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private void ConfigurePremiumStatusText(TMP_Text text)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = 24f;
            text.fontSizeMax = 40f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            if (text.rectTransform != null)
            {
                RectTransform rect = text.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, -150f);
                rect.sizeDelta = new Vector2(640f, 72f);
            }
        }

        private void EnsureOverlayCanvasOnTop()
        {
            if (!TryGetComponent(out Canvas overlayCanvas))
            {
                overlayCanvas = gameObject.AddComponent<Canvas>();
            }

            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = overlaySortingOrder;

            if (!TryGetComponent(out GraphicRaycaster _))
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private void RemoveNestedSortingCanvases()
        {
            if (ticketRoot != null)
            {
                DestroySortingCanvas(ticketRoot.gameObject);
            }

            if (premiumStatusText != null)
            {
                DestroySortingCanvas(premiumStatusText.gameObject);
            }

            if (claimButton != null)
            {
                DestroySortingCanvas(claimButton.gameObject);
            }
        }

        private static void DestroySortingCanvas(GameObject target)
        {
            if (target.TryGetComponent(out Canvas canvas))
            {
                if (Application.isPlaying)
                {
                    Destroy(canvas);
                }
                else
                {
                    DestroyImmediate(canvas);
                }
            }
        }

        private void ConfigureTicketLayering()
        {
            if (ticketRoot == null)
            {
                return;
            }

            TicketVfxLayeringUtility.ApplySortingOnly(ticketRoot, 4);
            TicketVfxLayeringUtility.RemoveTicketForegroundCanvas(ticketRoot);
        }

        private void HideBattlePassMainPanel()
        {
            if (battlePassMainPanel == null || !battlePassMainPanel.activeSelf)
            {
                return;
            }

            battlePassMainPanel.SetActive(false);
            mainPanelWasHiddenByOverlay = true;
        }

        private void ShowBattlePassMainPanel()
        {
            if (!mainPanelWasHiddenByOverlay || battlePassMainPanel == null)
            {
                return;
            }

            battlePassMainPanel.SetActive(true);
            mainPanelWasHiddenByOverlay = false;
        }

        private void SetBackdropBlocksInput(bool blocksInput)
        {
            if (solidFill != null)
            {
                solidFill.raycastTarget = blocksInput;
            }
        }

        private void OnClaimClicked()
        {
            claimRequested = true;
        }

        private void ResetToIdle(bool restoreMainPanel = true)
        {
            claimRequested = false;
            if (restoreMainPanel)
            {
                ShowBattlePassMainPanel();
            }

            SetBackdropBlocksInput(false);

            if (overlayGroup != null)
            {
                overlayGroup.alpha = 0f;
                overlayGroup.blocksRaycasts = false;
                overlayGroup.interactable = false;
            }

            SetBackdropAlpha(0f);
            SetRewardRevealVisible(false, 0f);

            if (lockGroup != null)
            {
                lockGroup.alpha = 0f;
            }

            if (lockRect != null)
            {
                lockRect.gameObject.SetActive(true);
                lockRect.localScale = Vector3.zero;
                lockRect.localRotation = lockBaseRotation;
            }

            if (lockBurstVfxSlot != null)
            {
                lockBurstVfxSlot.SetActive(false);
            }

            if (ticketVfxRoot != null)
            {
                ticketVfxRoot.SetActive(false);
            }

            if (claimButton != null)
            {
                claimButton.interactable = false;
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

        private void SetRewardRevealVisible(bool visible, float normalizedAlpha)
        {
            if (rewardRevealGroup == null) return;

            float a = visible ? Mathf.Clamp01(normalizedAlpha) : 0f;
            rewardRevealGroup.alpha = a;
            rewardRevealGroup.interactable = visible && a > 0.9f;
            rewardRevealGroup.blocksRaycasts = visible && a > 0.01f;

            if (ticketRoot != null)
            {
                float scale = visible ? Mathf.Lerp(0.85f, 1f, a) : 0.85f;
                ticketRoot.localScale = ticketBaseScale * scale;
            }
        }

        private IEnumerator PlayRoutine(Action onComplete)
        {
            ResetToIdle(restoreMainPanel: false);
            HideBattlePassMainPanel();

            overlayGroup.alpha = 1f;
            overlayGroup.blocksRaycasts = true;
            overlayGroup.interactable = true;
            SetBackdropBlocksInput(true);

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
                float shake = Mathf.Sin(t * Mathf.PI * 12f) * (1f - t) * 14f;
                lockRect.localRotation = lockBaseRotation * Quaternion.Euler(0f, 0f, shake);
                yield return null;
            }

            lockRect.localRotation = lockBaseRotation;

            if (lockBurstVfxSlot != null)
            {
                lockBurstVfxSlot.SetActive(true);
            }

            elapsed = 0f;
            const float burstPeakPortion = 0.32f;
            while (elapsed < explodeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / explodeDuration);

                if (t < burstPeakPortion)
                {
                    float peakT = t / burstPeakPortion;
                    float easedPeak = 1f - Mathf.Pow(1f - peakT, 2f);
                    lockRect.localScale = lockBaseScale * Mathf.Lerp(1f, lockBurstPeakScale, easedPeak);
                    lockGroup.alpha = 1f;
                }
                else
                {
                    float fadeT = (t - burstPeakPortion) / (1f - burstPeakPortion);
                    float easedFade = fadeT * fadeT;
                    lockRect.localScale = lockBaseScale * Mathf.Lerp(lockBurstPeakScale, 0f, easedFade);
                    lockGroup.alpha = 1f - easedFade;
                    lockRect.localRotation = lockBaseRotation * Quaternion.Euler(0f, 0f, easedFade * lockBurstRotation);
                }

                yield return null;
            }

            lockRect.localScale = Vector3.zero;
            lockGroup.alpha = 0f;
            lockRect.gameObject.SetActive(false);

            if (ticketVfxRoot != null)
            {
                ConfigureTicketLayering();
                ticketVfxRoot.SetActive(true);
                ConfigureTicketLayering();
            }

            elapsed = 0f;
            while (elapsed < rewardRevealDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / rewardRevealDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                SetBackdropAlpha(backdropMaxAlpha);
                SetRewardRevealVisible(true, eased);
                yield return null;
            }

            SetBackdropAlpha(backdropMaxAlpha);
            SetRewardRevealVisible(true, 1f);
            SetBackdropBlocksInput(false);

            if (claimButtonDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(claimButtonDelay);
            }

            if (claimButton != null)
            {
                claimButton.interactable = true;
            }

            claimRequested = false;
            while (!claimRequested)
            {
                yield return null;
            }

            if (claimButton != null)
            {
                claimButton.interactable = false;
            }

            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                SetBackdropAlpha(backdropMaxAlpha * (1f - t));
                SetRewardRevealVisible(true, 1f - t);
                yield return null;
            }

            ResetToIdle();
            playRoutine = null;
            onComplete?.Invoke();
        }
    }
}
