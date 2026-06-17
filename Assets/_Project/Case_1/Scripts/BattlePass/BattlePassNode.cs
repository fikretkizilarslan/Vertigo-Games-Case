using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattlePass.UI
{
    public class BattlePassNode : MonoBehaviour
    {
        [Header("Global Elements")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private RectTransform levelNodeAnchor;
        [SerializeField] private Image levelNodeImage;
        [SerializeField] private Image specialLevelIcon; // Ticket icon for Level 0
        [Header("Free Reward UI")]
        [SerializeField] private Image freeCardBg;
        [SerializeField] private Image freeIcon;
        [SerializeField] private RectTransform freeItemMask;
        [SerializeField] private TextMeshProUGUI freeTitleText;
        [SerializeField] private TextMeshProUGUI freeAmountText;
        [SerializeField] private Image freeCurrencyIcon;
        [SerializeField] private GameObject freeLockOverlay;
        [SerializeField] private GameObject freeTickOverlay;
        [Tooltip("Img_Glow behind the card. Tinted per rarity via Image.color; shared material comes from BattlePassManager.")]
        [SerializeField] private Image freeGlow;
        [SerializeField] private Button freeButton;
        [SerializeField] private GameObject freeRedDot;

        [Header("Premium Reward UI")]
        [SerializeField] private Image premiumCardBg;
        [SerializeField] private Image premiumIcon;
        [SerializeField] private RectTransform premiumItemMask;
        [SerializeField] private TextMeshProUGUI premiumTitleText;
        [SerializeField] private TextMeshProUGUI premiumAmountText;
        [SerializeField] private Image premiumCurrencyIcon;
        [SerializeField] private GameObject premiumLockOverlay;
        [SerializeField] private GameObject premiumTickOverlay;
        [Tooltip("Img_Glow behind the card. Tinted per rarity via Image.color; shared material comes from BattlePassManager.")]
        [SerializeField] private Image premiumGlow;
        [SerializeField] private Button premiumButton;
        [SerializeField] private GameObject premiumRedDot;

        private BattlePassTierData tierData;
        private BattlePassManager manager;
        private bool isInitialized = false;
        private bool effectStateInitialized;
        private bool lastShowFreeGlow;
        private bool lastShowPremiumGlow;
        private bool lastShowFreePulse;
        private bool lastShowPremiumPulse;
        private Material assignedFreeCardMaterial;
        private Material assignedPremiumCardMaterial;
        private Material assignedFreeGlowMaterial;
        private Material assignedPremiumGlowMaterial;
        private bool freeClaimShineActive;
        private bool premiumClaimShineActive;
        private Coroutine freeClaimShineRoutine;
        private Coroutine premiumClaimShineRoutine;

        public RectTransform LevelNodeAnchor => levelNodeAnchor;
        public BattlePassTierData TierData => tierData;

        public void Initialize(BattlePassTierData data, BattlePassManager bpManager)
        {
            tierData = data;
            manager = bpManager;

            // Configure level text or special ticket icon for Level 0
            if (data.level == 0)
            {
                if (levelText != null) levelText.gameObject.SetActive(false);
                if (specialLevelIcon != null) specialLevelIcon.gameObject.SetActive(true);
            }
            else
            {
                if (levelText != null)
                {
                    levelText.gameObject.SetActive(true);
                    levelText.text = data.level.ToString();
                }
                if (specialLevelIcon != null) specialLevelIcon.gameObject.SetActive(false);
            }

            // Free track card setup
            SetupRewardUI(data.freeReward, freeCardBg, freeIcon, freeTitleText,
                          freeAmountText, freeCurrencyIcon, freeButton, false, data.isHighlighted);

            // Premium track card setup
            SetupRewardUI(data.premiumReward, premiumCardBg, premiumIcon, premiumTitleText,
                          premiumAmountText, premiumCurrencyIcon, premiumButton, true, data.isHighlighted);

            // Layout configurations for instant rewards
            if (data.isInstantReward)
            {
                // Disable the free card container
                if (freeCardBg != null && freeCardBg.transform.parent != null)
                    freeCardBg.transform.parent.gameObject.SetActive(false);
            }

            // Prefabs bake Sh_Shine on highlighted cards; default UI material batches better on mobile.
            ApplyCardMaterial(freeCardBg, null, ref assignedFreeCardMaterial);
            ApplyCardMaterial(premiumCardBg, null, ref assignedPremiumCardMaterial);
            effectStateInitialized = false;

            // Auto-assign red dots when they were not wired in the Inspector (prefab uses Img_Reddot).
            if (freeRedDot == null && freeCardBg != null)
            {
                freeRedDot = ResolveRedDot(freeCardBg.transform);
            }
            if (premiumRedDot == null && premiumCardBg != null)
            {
                premiumRedDot = ResolveRedDot(premiumCardBg.transform);
            }

            if (freeGlow == null && freeCardBg != null)
            {
                freeGlow = ResolveGlow(freeCardBg.transform);
            }
            if (premiumGlow == null && premiumCardBg != null)
            {
                premiumGlow = ResolveGlow(premiumCardBg.transform);
            }

            // UILocalUVModifier provides atlas-independent local UVs for Sh_Shine
            if (freeCardBg != null && freeCardBg.GetComponent<UILocalUVModifier>() == null)
            {
                freeCardBg.gameObject.AddComponent<UILocalUVModifier>();
            }
            if (premiumCardBg != null && premiumCardBg.GetComponent<UILocalUVModifier>() == null)
            {
                premiumCardBg.gameObject.AddComponent<UILocalUVModifier>();
            }

            UpdateNodeVisualState();
            isInitialized = true;
        }

        /// <summary>
        /// Configures the card's visual elements according to the RewardSlot data.
        /// Reads name, icon, and rarity configurations from the ScriptableObject.
        /// </summary>
        private void SetupRewardUI(RewardSlot slot, Image cardBg, Image icon,
            TextMeshProUGUI titleText, TextMeshProUGUI amountText,
            Image currencyIcon, Button button, bool isPremium, bool isHighlighted)
        {
            // If there's no reward data, disable the card object
            if (slot == null || slot.rewardData == null)
            {
                if (cardBg != null && cardBg.transform.parent != null)
                    cardBg.transform.parent.gameObject.SetActive(false);
                return;
            }

            // Enable the card container
            if (cardBg != null && cardBg.transform.parent != null)
                cardBg.transform.parent.gameObject.SetActive(true);

            RewardItemSO data = slot.rewardData;

            // Main central sprite icon
            if (icon != null)
                icon.sprite = data.Icon;

            // Title text (e.g. GOLD, SOLARIS)
            if (titleText != null)
            {
                titleText.text = data.DisplayName.ToUpper();
            }

            // Amount text or unlock conditions
            if (amountText != null)
            {
                if (manager.RewardTypesToShowAmountText != null && manager.RewardTypesToShowAmountText.Contains(data.Type))
                {
                    if (data.Type == RewardType.Attachment)
                    {
                        amountText.text = "ATTACHMENT";
                    }
                    else
                    {
                        amountText.text = slot.amount.ToString("N0");
                    }
                }
                else
                {
                    amountText.text = "";
                }
            }

            // Mini currency type indicator icon
            if (currencyIcon != null)
            {
                bool isTextShowing = amountText != null && !string.IsNullOrEmpty(amountText.text);
                if (data.CurrencyIcon != null && isTextShowing && data.Type != RewardType.Attachment)
                {
                    currencyIcon.sprite = data.CurrencyIcon;
                    currencyIcon.gameObject.SetActive(true);
                }
                else
                {
                    currencyIcon.gameObject.SetActive(false);
                }
            }

            // Assign frame background (uses highlighted gold outline if active)
            if (cardBg != null)
            {
                if (isHighlighted && manager.HighlightedCardBgSprite != null)
                    cardBg.sprite = manager.HighlightedCardBgSprite;
                else
                    cardBg.sprite = manager.GetCardSpriteByRarity(data.Rarity);
            }

            // Claim only when the player taps the visible card — not the parent container,
            // whose full rect can overlap the road / skip badge in the middle.
            BindRewardButton(button, cardBg, isPremium);
        }

        /// <summary>
        /// Routes reward claims to <paramref name="cardBg"/> only. The parent track button
        /// (Free / premium container) is disabled so road-level clicks never auto-claim.
        /// </summary>
        private void BindRewardButton(Button parentButton, Image cardBg, bool isPremium)
        {
            if (cardBg == null)
            {
                if (parentButton != null)
                {
                    parentButton.onClick.RemoveAllListeners();
                    parentButton.interactable = false;
                }
                return;
            }

            Button cardButton = cardBg.GetComponent<Button>();
            if (cardButton == null)
                cardButton = cardBg.gameObject.AddComponent<Button>();

            cardButton.targetGraphic = cardBg;
            cardButton.transition = Selectable.Transition.None;
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(() => OnRewardClicked(isPremium));

            if (parentButton != null && parentButton != cardButton)
            {
                parentButton.onClick.RemoveAllListeners();
                parentButton.interactable = false;

                Image parentHit = parentButton.GetComponent<Image>();
                if (parentHit != null)
                    parentHit.raycastTarget = false;
            }

            cardBg.raycastTarget = true;
        }

        public void UpdateNodeVisualState()
        {
            if (tierData == null || manager == null) return;

            NodeClaimState state = ComputeClaimState();
            UpdateNodeCoreState(state);
            UpdateNodeEffectState(state);
        }

        public void UpdateNodeCoreState()
        {
            if (tierData == null || manager == null) return;
            UpdateNodeCoreState(ComputeClaimState());
        }

        public void UpdateNodeEffectState()
        {
            if (tierData == null || manager == null) return;

            NodeClaimState state = ComputeClaimState();
            bool showEffects = manager.IsNearViewport(GetViewportAnchor());
            bool showFreeGlow = showEffects && state.freeClaimablePulse;
            bool showPremiumGlow = showEffects && state.premiumClaimable;

            if (effectStateInitialized
                && !showEffects
                && !showFreeGlow
                && !showPremiumGlow
                && !lastShowFreeGlow
                && !lastShowPremiumGlow
                && !lastShowFreePulse
                && !lastShowPremiumPulse)
            {
                return;
            }

            UpdateNodeEffectState(state, showEffects, showFreeGlow, showPremiumGlow);
        }

        private struct NodeClaimState
        {
            public bool isLevelUnlocked;
            public bool hasPremiumActive;
            public bool freeClaimed;
            public bool premiumClaimed;
            public bool freeClaimablePulse;
            public bool premiumClaimable;
            public bool premiumLocked;
            public bool showFreeRedDot;
            public bool showPremiumRedDot;
        }

        private NodeClaimState ComputeClaimState()
        {
            bool isLevelUnlocked = tierData.isInstantReward ? manager.IsPremiumActive : manager.CurrentLevel >= tierData.level;
            bool hasPremiumActive = manager.IsPremiumActive;
            bool freeClaimed = tierData.freeReward != null && tierData.freeReward.isClaimed;
            bool premiumClaimed = tierData.premiumReward != null && tierData.premiumReward.isClaimed;
            bool freeClaimablePulse = !tierData.isInstantReward && isLevelUnlocked && !freeClaimed;
            bool premiumClaimable = isLevelUnlocked && hasPremiumActive && tierData.premiumReward != null && !premiumClaimed;

            bool showFreeRedDot = isLevelUnlocked &&
                                  tierData.freeReward != null &&
                                  tierData.freeReward.rewardData != null &&
                                  !freeClaimed;

            bool showPremiumRedDot = false;
            if (tierData.premiumReward != null && tierData.premiumReward.rewardData != null)
            {
                showPremiumRedDot = tierData.isInstantReward
                    ? !premiumClaimed
                    : isLevelUnlocked && !premiumClaimed;
            }

            return new NodeClaimState
            {
                isLevelUnlocked = isLevelUnlocked,
                hasPremiumActive = hasPremiumActive,
                freeClaimed = freeClaimed,
                premiumClaimed = premiumClaimed,
                freeClaimablePulse = freeClaimablePulse,
                premiumClaimable = premiumClaimable,
                premiumLocked = !isLevelUnlocked || !hasPremiumActive,
                showFreeRedDot = showFreeRedDot,
                showPremiumRedDot = showPremiumRedDot
            };
        }

        private RectTransform GetViewportAnchor()
        {
            if (levelNodeAnchor != null && levelNodeAnchor.gameObject.activeInHierarchy)
            {
                return levelNodeAnchor;
            }

            if (premiumCardBg != null)
            {
                return premiumCardBg.rectTransform;
            }

            if (freeCardBg != null)
            {
                return freeCardBg.rectTransform;
            }

            return transform as RectTransform;
        }

        private void UpdateNodeCoreState(NodeClaimState state)
        {
            if (tierData.isInstantReward)
            {
                if (levelNodeImage != null) levelNodeImage.gameObject.SetActive(false);
                if (levelText != null) levelText.gameObject.SetActive(false);
                if (specialLevelIcon != null) specialLevelIcon.gameObject.SetActive(false);

                if (tierData.isUnlockNowText)
                {
                    if (state.hasPremiumActive)
                    {
                        if (premiumAmountText != null)
                        {
                            if (tierData.premiumReward.rewardData.Type == RewardType.Attachment)
                                premiumAmountText.text = "ATTACHMENT";
                            else if (manager.RewardTypesToShowAmountText.Contains(tierData.premiumReward.rewardData.Type))
                                premiumAmountText.text = tierData.premiumReward.amount.ToString("N0");
                            else
                                premiumAmountText.text = "";
                        }

                        if (premiumCurrencyIcon != null)
                        {
                            bool isTextShowing = premiumAmountText != null && !string.IsNullOrEmpty(premiumAmountText.text);
                            if (tierData.premiumReward.rewardData.CurrencyIcon != null && isTextShowing && tierData.premiumReward.rewardData.Type != RewardType.Attachment)
                            {
                                premiumCurrencyIcon.sprite = tierData.premiumReward.rewardData.CurrencyIcon;
                                premiumCurrencyIcon.gameObject.SetActive(true);
                            }
                            else
                            {
                                premiumCurrencyIcon.gameObject.SetActive(false);
                            }
                        }
                    }
                    else
                    {
                        if (premiumAmountText != null) premiumAmountText.text = "UNLOCK NOW";
                        if (premiumCurrencyIcon != null) premiumCurrencyIcon.gameObject.SetActive(false);
                    }
                }
            }
            else if (levelNodeImage != null)
            {
                levelNodeImage.gameObject.SetActive(true);
                levelNodeImage.sprite = state.isLevelUnlocked
                    ? manager.LevelCompletedSprite
                    : manager.LevelLockedSprite;
            }

            RefreshAttachmentLabel(tierData.freeReward, freeAmountText, state.isLevelUnlocked);
            RefreshAttachmentLabel(tierData.premiumReward, premiumAmountText, state.isLevelUnlocked);

            if (!tierData.isInstantReward)
            {
                if (freeLockOverlay != null) freeLockOverlay.SetActive(!state.isLevelUnlocked);
                if (freeTickOverlay != null)
                {
                    bool wasActive = freeTickOverlay.activeSelf;
                    if (!wasActive && state.freeClaimed && isInitialized)
                    {
                        freeTickOverlay.SetActive(true);
                        StartCoroutine(PopAnimationRoutine(freeTickOverlay.transform));
                    }
                    else
                    {
                        freeTickOverlay.SetActive(state.freeClaimed);
                        if (!state.freeClaimed) freeTickOverlay.transform.localScale = Vector3.one;
                    }
                }

                ApplyCardBackgroundSprite(freeCardBg, tierData.freeReward, state.freeClaimed);
            }

            if (premiumLockOverlay != null) premiumLockOverlay.SetActive(state.premiumLocked);
            if (premiumTickOverlay != null)
            {
                bool wasActive = premiumTickOverlay.activeSelf;
                if (!wasActive && state.premiumClaimed && isInitialized)
                {
                    premiumTickOverlay.SetActive(true);
                    StartCoroutine(PopAnimationRoutine(premiumTickOverlay.transform));
                }
                else
                {
                    premiumTickOverlay.SetActive(state.premiumClaimed);
                    if (!state.premiumClaimed) premiumTickOverlay.transform.localScale = Vector3.one;
                }
            }

            ApplyCardBackgroundSprite(premiumCardBg, tierData.premiumReward, state.premiumClaimed);
            SetRedDotActive(freeRedDot, state.showFreeRedDot);
            SetRedDotActive(premiumRedDot, state.showPremiumRedDot);
        }

        private void UpdateNodeEffectState(NodeClaimState state)
        {
            bool showEffects = manager.IsNearViewport(GetViewportAnchor());
            UpdateNodeEffectState(
                state,
                showEffects,
                showEffects && state.freeClaimablePulse,
                showEffects && state.premiumClaimable);
        }

        private void UpdateNodeEffectState(
            NodeClaimState state,
            bool showEffects,
            bool showFreeGlow,
            bool showPremiumGlow)
        {
            bool showFreePulse = showFreeGlow;
            bool showPremiumPulse = showPremiumGlow;

            // Default UI material on cards; claim shine is a short-lived overlay (see PlayClaimShine).
            if (!tierData.isInstantReward && !freeClaimShineActive)
            {
                ApplyCardMaterial(freeCardBg, null, ref assignedFreeCardMaterial);
            }

            if (!premiumClaimShineActive)
            {
                ApplyCardMaterial(premiumCardBg, null, ref assignedPremiumCardMaterial);
            }

            if (!effectStateInitialized || lastShowFreePulse != showFreePulse)
            {
                lastShowFreePulse = showFreePulse;
                SetClaimPulseActive(freeCardBg, showFreePulse);
            }

            if (!effectStateInitialized || lastShowPremiumPulse != showPremiumPulse)
            {
                lastShowPremiumPulse = showPremiumPulse;
                SetClaimPulseActive(premiumCardBg, showPremiumPulse);
            }

            if (!effectStateInitialized || lastShowFreeGlow != showFreeGlow)
            {
                lastShowFreeGlow = showFreeGlow;
                ApplyCardGlow(freeGlow, tierData.freeReward, tierData.isHighlighted, showFreeGlow, ref assignedFreeGlowMaterial);
            }

            if (!effectStateInitialized || lastShowPremiumGlow != showPremiumGlow)
            {
                lastShowPremiumGlow = showPremiumGlow;
                ApplyCardGlow(premiumGlow, tierData.premiumReward, tierData.isHighlighted, showPremiumGlow, ref assignedPremiumGlowMaterial);
            }

            effectStateInitialized = true;
        }

        /// <summary>
        /// Assigns a UI material without reading <see cref="Graphic.material"/> (getter creates instances).
        /// </summary>
        private static void ApplyCardMaterial(Image image, Material material, ref Material assigned)
        {
            if (image == null || assigned == material)
            {
                return;
            }

            assigned = material;
            image.material = material;
        }

        /// <summary>
        /// Drives a card's back glow. Every card shares ONE glow material
        /// (<see cref="BattlePassManager.CardGlowMaterial"/>); only the per-card tint changes,
        /// applied through the Image vertex color so no per-card material copies are created.
        /// Hidden once the reward is claimed or unavailable.
        /// </summary>
        private void ApplyCardGlow(Image glow, RewardSlot slot, bool isHighlighted, bool show, ref Material assignedMaterial)
        {
            if (glow == null) return;

            glow.raycastTarget = false;

            if (show && slot != null && slot.rewardData != null && manager != null)
            {
                Material glowMat = manager.CardGlowMaterial;
                if (glowMat != null)
                {
                    ApplyCardMaterial(glow, glowMat, ref assignedMaterial);
                }

                Color tint = isHighlighted
                    ? manager.HighlightedCardColor
                    : manager.GetCardColorByRarity(slot.rewardData.Rarity);
                tint.a = 1f;
                glow.color = tint;

                // Keep the halo behind the card draw order.
                glow.rectTransform.SetSiblingIndex(0);
            }

            glow.gameObject.SetActive(show);
        }

        /// <summary>
        /// Pulses a claimable reward card with the same <see cref="UITweenAnimator"/> settings as the red dot.
        /// Searches the track root (<c>Free</c>/<c>premium</c>), <c>Grp_Card</c>, and <c>Img_Card</c> so
        /// prefab layouts with the animator on any of those objects still work.
        /// </summary>
        private void SetClaimPulseActive(Image cardBg, bool isClaimable)
        {
            if (cardBg == null) return;

            Transform pulseRoot = GetCardPulseRoot(cardBg.transform);
            UITweenAnimator animator = ResolvePulseAnimator(pulseRoot, cardBg.transform);

            foreach (UITweenAnimator extra in pulseRoot.GetComponentsInChildren<UITweenAnimator>(true))
            {
                if (animator != null && extra == animator)
                {
                    continue;
                }

                extra.Stop();
            }

            if (animator == null)
            {
                ResetCardVisualScale(cardBg.transform);
                return;
            }

            if (isClaimable)
            {
                animator.enabled = true;
                animator.Play();
            }
            else
            {
                animator.Stop();
                animator.enabled = false;
                ResetCardVisualScale(cardBg.transform);
            }
        }

        private static void ResetCardVisualScale(Transform cardTransform)
        {
            if (cardTransform == null)
            {
                return;
            }

            cardTransform.localScale = Vector3.one;

            Transform track = cardTransform.parent;
            if (track != null)
            {
                track.localScale = Vector3.one;
            }
        }

        private static Transform GetCardPulseRoot(Transform cardTransform)
        {
            if (cardTransform == null) return null;

            Transform track = cardTransform.parent;
            if (track != null && (track.name == "Free" || track.name == "premium" || track.name == "Grp_Free" || track.name == "Grp_Premium"))
                return track;

            if (track != null && track.name == "Grp_Card")
                return track;

            return cardTransform;
        }

        private static UITweenAnimator ResolvePulseAnimator(Transform pulseRoot, Transform cardTransform)
        {
            if (pulseRoot == null) return null;

            UITweenAnimator animator = pulseRoot.GetComponent<UITweenAnimator>();
            if (animator != null) return animator;

            if (cardTransform != null)
            {
                animator = cardTransform.GetComponent<UITweenAnimator>();
                if (animator != null) return animator;
            }

            animator = pulseRoot.GetComponentInChildren<UITweenAnimator>(true);
            if (animator != null) return animator;

            return pulseRoot.gameObject.AddComponent<UITweenAnimator>();
        }

        /// <summary>
        /// Shows or hides a red-dot badge. When hiding, any running <see cref="UITweenAnimator"/>
        /// is stopped via <c>OnDisable</c> so the claimed card stays clean.
        /// </summary>
        private void SetRedDotActive(GameObject redDot, bool isVisible)
        {
            if (redDot == null) return;
            redDot.SetActive(isVisible);
        }

        /// <summary>
        /// Finds the back-glow image on the reward track (<c>Free</c>/<c>premium</c> &gt; <c>Img_Glow</c>).
        /// </summary>
        private Image ResolveGlow(Transform cardTransform)
        {
            if (cardTransform == null) return null;

            Transform track = cardTransform.parent;
            if (track == null) return null;

            Transform found = track.Find("Img_Glow");
            if (found == null)
                found = FindChildRecursive(track, "Img_Glow");

            return found != null ? found.GetComponent<Image>() : null;
        }

        /// <summary>
        /// Finds the notification badge under a reward card. Prefabs name it <c>Img_Reddot</c>;
        /// older layouts may still use <c>RedDot</c>.
        /// </summary>
        private GameObject ResolveRedDot(Transform cardRoot)
        {
            if (cardRoot == null) return null;

            Transform found = cardRoot.Find("Img_Reddot");
            if (found == null) found = cardRoot.Find("RedDot");
            if (found == null) found = FindChildRecursive(cardRoot, "Img_Reddot");
            if (found == null) found = FindChildRecursive(cardRoot, "RedDot");
            if (found == null) found = FindChildRecursive(cardRoot, "RedDot (1)");

            return found != null ? found.gameObject : null;
        }

        /// <summary>
        /// Refreshes an Attachment card's label so it reads "ATTACHMENT" while its tier is still
        /// locked and switches to "UNLOCK NOW" once the player reaches the tier level (i.e. the
        /// reward becomes claimable). Only standard (non-instant) attachment rewards are affected;
        /// every other reward type and the instant-reward teaser cards are left untouched.
        /// </summary>
        /// <param name="slot">Reward slot to inspect (no-op when empty or not an attachment).</param>
        /// <param name="amountText">Label to repaint.</param>
        /// <param name="isLevelUnlocked">True once the tier level has been reached.</param>
        private void RefreshAttachmentLabel(RewardSlot slot, TextMeshProUGUI amountText, bool isLevelUnlocked)
        {
            if (tierData.isInstantReward) return;
            if (amountText == null || slot == null || slot.rewardData == null) return;
            if (slot.rewardData.Type != RewardType.Attachment) return;

            amountText.text = isLevelUnlocked ? "UNLOCK NOW" : "ATTACHMENT";
        }

        /// <summary>
        /// Picks the card frame sprite. Rarity drives the default look; only tiers flagged
        /// <see cref="BattlePassTierData.isHighlighted"/> use the collectable yellow frame.
        /// Level unlock / skip never swaps backgrounds.
        /// </summary>
        private void ApplyCardBackgroundSprite(Image cardBg, RewardSlot slot, bool claimed)
        {
            if (cardBg == null || slot == null || slot.rewardData == null) return;

            if (claimed && manager.ClaimedCardBgSprite != null)
            {
                cardBg.sprite = manager.ClaimedCardBgSprite;
                return;
            }

            if (tierData.isHighlighted && manager.HighlightedCardBgSprite != null)
                cardBg.sprite = manager.HighlightedCardBgSprite;
            else
                cardBg.sprite = manager.GetCardSpriteByRarity(slot.rewardData.Rarity);
        }

        private void OnRewardClicked(bool isPremium)
        {
            manager.OnRewardClicked(this, isPremium);
        }

        /// <summary>
        /// Plays a short <c>Sh_Shine</c> sweep on the reward card. Called automatically when the
        /// player claims; you can also invoke this from other scripts or animation events.
        /// Duration and on/off are configured on <see cref="BattlePassManager"/> (Claim Shine).
        /// </summary>
        public void PlayClaimShine(bool isPremium)
        {
            if (manager == null || !manager.EnableClaimShine)
            {
                return;
            }

            if (isPremium)
            {
                if (premiumClaimShineRoutine != null)
                {
                    StopCoroutine(premiumClaimShineRoutine);
                }

                premiumClaimShineRoutine = StartCoroutine(ClaimShineRoutine(premiumCardBg, tierData.premiumReward, isPremiumTrack: true));
            }
            else
            {
                if (freeClaimShineRoutine != null)
                {
                    StopCoroutine(freeClaimShineRoutine);
                }

                freeClaimShineRoutine = StartCoroutine(ClaimShineRoutine(freeCardBg, tierData.freeReward, isPremiumTrack: false));
            }
        }

        private Material ResolveClaimShineMaterial(RewardSlot slot)
        {
            if (slot == null || slot.rewardData == null || manager == null)
            {
                return null;
            }

            if (manager.ClaimShineMaterial != null)
            {
                return manager.ClaimShineMaterial;
            }

            return tierData.isHighlighted
                ? manager.CardSweepMaterial
                : manager.RarityCardSweepMaterial;
        }

        private IEnumerator ClaimShineRoutine(Image cardBg, RewardSlot slot, bool isPremiumTrack)
        {
            Material shineMat = ResolveClaimShineMaterial(slot);
            if (cardBg == null || shineMat == null)
            {
                yield break;
            }

            if (isPremiumTrack)
            {
                premiumClaimShineActive = true;
                ApplyCardMaterial(cardBg, shineMat, ref assignedPremiumCardMaterial);
            }
            else
            {
                freeClaimShineActive = true;
                ApplyCardMaterial(cardBg, shineMat, ref assignedFreeCardMaterial);
            }

            yield return new WaitForSecondsRealtime(manager.ClaimShineDuration);

            if (isPremiumTrack)
            {
                premiumClaimShineActive = false;
                ApplyCardMaterial(cardBg, null, ref assignedPremiumCardMaterial);
                premiumClaimShineRoutine = null;
            }
            else
            {
                freeClaimShineActive = false;
                ApplyCardMaterial(cardBg, null, ref assignedFreeCardMaterial);
                freeClaimShineRoutine = null;
            }

            UpdateNodeEffectState();
        }

        private IEnumerator PopAnimationRoutine(Transform target)
        {
            target.localScale = Vector3.zero;
            float duration = 0.25f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                
                // Springy Back Out ease scaling curve
                float scale = 1f;
                if (t < 1f)
                {
                    float ts = t - 1f;
                    scale = ts * ts * ((2f + 1f) * ts + 2f) + 1f;
                }
                
                target.localScale = new Vector3(scale, scale, scale);
                yield return null;
            }

            target.localScale = Vector3.one;
        }

        public Vector3 GetCardWorldPosition(bool isPremium)
        {
            if (isPremium)
            {
                return premiumCardBg != null ? premiumCardBg.transform.position : transform.position;
            }
            else
            {
                return freeCardBg != null ? freeCardBg.transform.position : transform.position;
            }
        }

        private Transform FindChildRecursive(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName) return child;
                Transform found = FindChildRecursive(child, childName);
                if (found != null) return found;
            }
            return null;
        }
    }
}
