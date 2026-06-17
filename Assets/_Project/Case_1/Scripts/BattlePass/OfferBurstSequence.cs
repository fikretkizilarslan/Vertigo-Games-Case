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
        [SerializeField] private RectTransform explosionGlowRect;
        [SerializeField] private CanvasGroup explosionGlowGroup;
        [SerializeField] private ParticleSystem implodeVfx;
        [SerializeField] private ParticleSystem explodeGlowVfx;

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

        [Header("Floating Bobbing Animation")]
        [SerializeField] private bool enableBobbing = true;
        [SerializeField] private float bobbingSpeed = 3f;
        [SerializeField] private float bobbingAmplitude = 18f;

        private Coroutine playRoutine;
        private Vector3 lockBaseScale = Vector3.one;
        private Quaternion lockBaseRotation = Quaternion.identity;
        private Vector3 ticketBaseScale = Vector3.one;
        private Vector2 ticketBaseAnchoredPosition = Vector2.zero;
        private Vector3 explosionGlowBaseScale = Vector3.one;
        private Color solidBaseColor = Color.white;
        private Color patternBaseColor = Color.white;
        private bool isInitialized;
        private bool claimRequested;
        private bool mainPanelWasHiddenByOverlay;

        public bool IsPlaying => playRoutine != null;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            if (!isInitialized)
            {
                Initialize();
            }
            else if (playRoutine == null)
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
                Debug.LogWarning("[OfferBurstSequence] Scene references are missing. Assign Grp_OfferBurst references in the Inspector.");
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

            // Text content and formatting overrides are skipped here so that they can be fully customized manually in the Inspector/Editor.

            lockBaseScale = lockRect.localScale.sqrMagnitude > 0.0001f ? lockRect.localScale : Vector3.one;
            lockBaseRotation = lockRect.localRotation;
            ticketBaseScale = ticketRoot != null && ticketRoot.localScale.sqrMagnitude > 0.0001f
                ? ticketRoot.localScale
                : Vector3.one;

            if (ticketRoot != null)
            {
                ticketBaseAnchoredPosition = ticketRoot.anchoredPosition;
            }

            if (explosionGlowRect != null)
            {
                explosionGlowBaseScale = explosionGlowRect.localScale.sqrMagnitude > 0.0001f
                    ? explosionGlowRect.localScale
                    : Vector3.one;
            }

            if (claimButton != null)
            {
                claimButton.onClick.RemoveListener(OnClaimClicked);
                claimButton.onClick.AddListener(OnClaimClicked);
            }

            if (implodeVfx == null)
            {
                Transform implodeTrans = transform.Find("Ps_Lock_Implode");
                if (implodeTrans != null) implodeVfx = implodeTrans.GetComponent<ParticleSystem>();
            }

            if (explodeGlowVfx == null)
            {
                Transform explodeTrans = transform.Find("Ps_Lock_Explode_Glow");
                if (explodeTrans != null) explodeGlowVfx = explodeTrans.GetComponent<ParticleSystem>();
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
            // Empty body so that it does not overwrite the font, size, auto-sizing, wrapping, or layout set in the Inspector.
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
            if (rewardRevealGroup != null)
            {
                DestroySortingCanvas(rewardRevealGroup.gameObject);
            }

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

            // Render particles (Ps_Ticket) in front of the background dim overlay (overlaySortingOrder + 1)
            TicketVfxLayeringUtility.ApplySortingOnly(ticketRoot, overlaySortingOrder + 1);

            // Render the main ticket image (Img_Ticket) on top of the particles by giving it a nested Canvas at overlaySortingOrder + 2
            if (ticketImage != null)
            {
                if (!ticketImage.TryGetComponent(out Canvas ticketCanvas))
                {
                    ticketCanvas = ticketImage.gameObject.AddComponent<Canvas>();
                }
                ticketCanvas.overrideSorting = true;
                ticketCanvas.sortingOrder = overlaySortingOrder + 2;
                ticketCanvas.sortingLayerName = "UI";
            }
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

            if (patternOverlay != null)
            {
                patternOverlay.raycastTarget = blocksInput;
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

            if (implodeVfx != null)
            {
                StopImplodeVfx();
            }

            if (explodeGlowVfx != null)
            {
                explodeGlowVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (explosionGlowRect != null)
            {
                explosionGlowRect.gameObject.SetActive(false);
                explosionGlowRect.localScale = explosionGlowBaseScale;
                if (explosionGlowGroup != null)
                {
                    explosionGlowGroup.alpha = 0f;
                }
            }

            if (ticketVfxRoot != null)
            {
                ticketVfxRoot.SetActive(false);
            }

            if (claimButton != null)
            {
                claimButton.interactable = false;
            }

            if (ticketRoot != null)
            {
                ticketRoot.anchoredPosition = ticketBaseAnchoredPosition;
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

        private void PlayImplodeVfx()
        {
            if (implodeVfx == null)
                return;

            implodeVfx.Play(true);

            ParticleSystem[] childSystems = implodeVfx.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < childSystems.Length; i++)
            {
                if (childSystems[i] != implodeVfx)
                    childSystems[i].Play(true);
            }
        }

        private void StopImplodeVfx()
        {
            if (implodeVfx == null)
                return;

            implodeVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem[] childSystems = implodeVfx.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < childSystems.Length; i++)
            {
                if (childSystems[i] != implodeVfx)
                    childSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
            PlayImplodeVfx();

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

            // Anticipation squeeze before the lock bursts open.
            elapsed = 0f;
            float squeezeDuration = 0.22f;
            while (elapsed < squeezeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / squeezeDuration);
                
                // Squash and stretch anticipation:
                // Phase 1 (0 to 0.35): squash flat (X wide, Y short)
                // Phase 2 (0.35 to 1.0): pull inward (shrink) and stretch (X very thin, Y stretched, overall scale small)
                Vector3 currentTargetScale;
                if (t < 0.35f)
                {
                    float nt = t / 0.35f;
                    float ease = nt * nt * (3f - 2f * nt);
                    currentTargetScale = Vector3.Lerp(Vector3.one, new Vector3(1.35f, 0.65f, 1f), ease);
                }
                else
                {
                    float nt = (t - 0.35f) / 0.65f;
                    float ease = nt * nt; // Accelerate pull inward
                    Vector3 startScale = new Vector3(1.35f, 0.65f, 1f);
                    Vector3 endScale = new Vector3(0.15f, 1.85f, 1f); // Extremely squeezed X, stretched Y, very thin
                    currentTargetScale = Vector3.Lerp(startScale, endScale, ease);
                }

                lockRect.localScale = Vector3.Scale(lockBaseScale, currentTargetScale);
                yield return null;
            }

            if (lockBurstVfxSlot != null)
            {
                lockBurstVfxSlot.SetActive(true);
            }

            if (explodeGlowVfx != null)
            {
                explodeGlowVfx.Play(true);
            }

            if (explosionGlowRect != null)
            {
                explosionGlowRect.gameObject.SetActive(true);
                if (explosionGlowGroup != null)
                {
                    explosionGlowGroup.alpha = 1f;
                }
            }

            elapsed = 0f;
            while (elapsed < explodeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / explodeDuration);

                // Lock explosion: scale up rapidly and fade out
                float easedScale = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic
                lockRect.localScale = Vector3.Scale(lockBaseScale, Vector3.Lerp(new Vector3(0.15f, 1.85f, 1f), lockBaseScale * lockBurstPeakScale, easedScale));
                lockGroup.alpha = 1f - t * t; // Fade out quadratic

                // Rotate during explosion
                lockRect.localRotation = lockBaseRotation * Quaternion.Euler(0f, 0f, t * lockBurstRotation);

                // Billboard glow stretching
                if (explosionGlowRect != null)
                {
                    // Stretch X (horizontal) from 0.2 to 12.0, Y (vertical) from 4.5 to 0.05
                    // This creates a beautiful bright flash streak
                    float stretchX = Mathf.Lerp(0.2f, 12f, t);
                    float stretchY = Mathf.Lerp(4.5f, 0.05f, t);
                    explosionGlowRect.localScale = Vector3.Scale(explosionGlowBaseScale, new Vector3(stretchX, stretchY, 1f));

                    if (explosionGlowGroup != null)
                    {
                        explosionGlowGroup.alpha = Mathf.Clamp01(1f - t);
                    }
                }

                yield return null;
            }

            if (explosionGlowRect != null)
            {
                explosionGlowRect.gameObject.SetActive(false);
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

            float bobTime = 0f;
            float delayElapsed = 0f;
            bool claimButtonEnabled = false;

            claimRequested = false;
            while (!claimRequested)
            {
                float dt = Time.unscaledDeltaTime;
                bobTime += dt;

                if (!claimButtonEnabled)
                {
                    delayElapsed += dt;
                    if (delayElapsed >= claimButtonDelay)
                    {
                        if (claimButton != null)
                        {
                            claimButton.interactable = true;
                        }
                        claimButtonEnabled = true;
                    }
                }

                if (enableBobbing && ticketRoot != null)
                {
                    float offset = Mathf.Sin(bobTime * bobbingSpeed) * bobbingAmplitude;
                    ticketRoot.anchoredPosition = ticketBaseAnchoredPosition + new Vector2(0f, offset);
                }

                yield return null;
            }

            ResetToIdle();
            playRoutine = null;
            onComplete?.Invoke();
        }

#if UNITY_EDITOR
        [ContextMenu("Setup Particle Effects")]
        public void SetupParticleEffects()
        {
            if (lockBurstVfxSlot == null)
            {
                Transform slot = FindChildByPath("Grp_VFX_Slot");
                if (slot != null) lockBurstVfxSlot = slot.gameObject;
            }

            if (lockBurstVfxSlot == null) return;

            // Clean up any old ones that were placed under Grp_VFX_Slot
            if (lockBurstVfxSlot != null)
            {
                Transform oldImplode = lockBurstVfxSlot.transform.Find("Ps_Lock_Implode");
                if (oldImplode != null) DestroyImmediate(oldImplode.gameObject);

                Transform oldExplode = lockBurstVfxSlot.transform.Find("Ps_Lock_Explode_Glow");
                if (oldExplode != null) DestroyImmediate(oldExplode.gameObject);
            }

            // 1. Ps_Lock_Implode
            Transform implodeTrans = transform.Find("Ps_Lock_Implode");
            if (implodeTrans != null && !(implodeTrans is RectTransform))
            {
                DestroyImmediate(implodeTrans.gameObject);
                implodeTrans = null;
            }
            bool isNewImplode = false;
            if (implodeTrans == null)
            {
                GameObject go = new GameObject("Ps_Lock_Implode", typeof(RectTransform));
                go.layer = 5;
                go.transform.SetParent(transform, false);
                implodeTrans = go.transform;
                isNewImplode = true;
            }
            implodeTrans.gameObject.layer = 5;
            implodeVfx = implodeTrans.GetComponent<ParticleSystem>();
            if (implodeVfx == null)
            {
                implodeVfx = implodeTrans.gameObject.AddComponent<ParticleSystem>();
                isNewImplode = true;
            }

            Canvas implodeCanvas = implodeTrans.GetComponent<Canvas>();
            if (implodeCanvas == null) implodeCanvas = implodeTrans.gameObject.AddComponent<Canvas>();
            implodeCanvas.overrideSorting = true;
            implodeCanvas.sortingLayerName = "UI";
            implodeCanvas.sortingOrder = overlaySortingOrder + 1;

            if (isNewImplode)
            {
                ConfigureImplodeVfx();
            }

            // 2. Ps_Lock_Explode_Glow
            Transform explodeTrans = transform.Find("Ps_Lock_Explode_Glow");
            if (explodeTrans != null && !(explodeTrans is RectTransform))
            {
                DestroyImmediate(explodeTrans.gameObject);
                explodeTrans = null;
            }
            bool isNewExplode = false;
            if (explodeTrans == null)
            {
                GameObject go = new GameObject("Ps_Lock_Explode_Glow", typeof(RectTransform));
                go.layer = 5;
                go.transform.SetParent(transform, false);
                explodeTrans = go.transform;
                isNewExplode = true;
            }
            explodeTrans.gameObject.layer = 5;
            explodeGlowVfx = explodeTrans.GetComponent<ParticleSystem>();
            if (explodeGlowVfx == null)
            {
                explodeGlowVfx = explodeTrans.gameObject.AddComponent<ParticleSystem>();
                isNewExplode = true;
            }

            Canvas explodeCanvas = explodeTrans.GetComponent<Canvas>();
            if (explodeCanvas == null) explodeCanvas = explodeTrans.gameObject.AddComponent<Canvas>();
            explodeCanvas.overrideSorting = true;
            explodeCanvas.sortingLayerName = "UI";
            explodeCanvas.sortingOrder = overlaySortingOrder + 1;

            if (isNewExplode)
            {
                ConfigureExplodeGlowVfx();
            }

            UnityEditor.EditorUtility.SetDirty(this);
            if (lockBurstVfxSlot != null) UnityEditor.EditorUtility.SetDirty(lockBurstVfxSlot);
            if (implodeVfx != null) UnityEditor.EditorUtility.SetDirty(implodeVfx.gameObject);
            if (explodeGlowVfx != null) UnityEditor.EditorUtility.SetDirty(explodeGlowVfx.gameObject);
        }

        private void ConfigureImplodeVfx()
        {
            if (implodeVfx == null) return;

            var main = implodeVfx.main;
            main.loop = false;
            main.duration = 0.25f;
            main.startLifetime = 0.25f;
            main.startSpeed = -8.0f; // pull inward
            main.startSize = 1.5f;
            main.maxParticles = 100;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.playOnAwake = false;

            var emission = implodeVfx.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

            var shape = implodeVfx.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1.8f;
            shape.radiusThickness = 0.05f;

            var size = implodeVfx.sizeOverLifetime;
            size.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 1f);
            sizeCurve.AddKey(1f, 0.2f);
            size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var renderer = implodeVfx.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.lengthScale = 3.5f;
                renderer.velocityScale = 0.4f;
                renderer.sortingLayerName = "UI";
                renderer.sortingOrder = overlaySortingOrder + 1;

                Material mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Case_1/Materials/VFX/Mt_Uı_VFX.mat");
                if (mat != null) renderer.material = mat;
            }
        }

        private void ConfigureExplodeGlowVfx()
        {
            if (explodeGlowVfx == null) return;

            var main = explodeGlowVfx.main;
            main.loop = false;
            main.duration = 0.4f;
            main.startLifetime = 0.4f;
            main.startSpeed = 0f;
            main.startSize = 1.0f;
            main.maxParticles = 5;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.playOnAwake = false;

            var emission = explodeGlowVfx.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });

            var shape = explodeGlowVfx.shape;
            shape.enabled = false;

            var size = explodeGlowVfx.sizeOverLifetime;
            size.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.2f);
            sizeCurve.AddKey(0.15f, 6.0f); // Expands very fast
            sizeCurve.AddKey(1f, 8.0f); // Continues growing slightly
            size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var color = explodeGlowVfx.colorOverLifetime;
            color.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) }
            );
            color.color = grad;

            var renderer = explodeGlowVfx.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.sortingLayerName = "UI";
                renderer.sortingOrder = overlaySortingOrder + 1;

                Material mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Case_1/Materials/VFX/Mt_Uı_VFX.mat");
                if (mat != null) renderer.material = mat;
            }
        }
#endif
    }
}
