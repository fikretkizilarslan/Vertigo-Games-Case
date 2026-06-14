using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VertigoCase.UI
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
        [SerializeField] private GameObject freeGlowOutline;
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
        [SerializeField] private GameObject premiumGlowOutline;
        [SerializeField] private Button premiumButton;
        [SerializeField] private GameObject premiumRedDot;

        private BattlePassTierData tierData;
        private BattlePassManager manager;
        private bool isInitialized = false;

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

            bool showPremiumBg = data.isInstantReward || (data.level <= bpManager.CurrentLevel);

            // Free track card setup
            SetupRewardUI(data.freeReward, freeCardBg, freeIcon, freeTitleText,
                          freeAmountText, freeCurrencyIcon, freeButton, false, showPremiumBg);

            // Premium track card setup
            SetupRewardUI(data.premiumReward, premiumCardBg, premiumIcon, premiumTitleText,
                          premiumAmountText, premiumCurrencyIcon, premiumButton, true, showPremiumBg);

            // Layout configurations for instant rewards
            if (data.isInstantReward)
            {
                // Disable the free card container
                if (freeCardBg != null && freeCardBg.transform.parent != null)
                    freeCardBg.transform.parent.gameObject.SetActive(false);
            }

            // Auto-assign red dots when they were not wired in the Inspector (prefab uses Img_Reddot).
            if (freeRedDot == null && freeCardBg != null)
            {
                freeRedDot = ResolveRedDot(freeCardBg.transform);
            }
            if (premiumRedDot == null && premiumCardBg != null)
            {
                premiumRedDot = ResolveRedDot(premiumCardBg.transform);
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

            // Bind click listener event
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnRewardClicked(isPremium));
            }
        }

        public void UpdateNodeVisualState()
        {
            if (tierData == null) return;

            // Hide level badge indicators for instant rewards
            if (tierData.isInstantReward)
            {
                if (levelNodeImage != null) levelNodeImage.gameObject.SetActive(false);
                if (levelText != null) levelText.gameObject.SetActive(false);
                if (specialLevelIcon != null) specialLevelIcon.gameObject.SetActive(false);

                // Update text to UNLOCK NOW if premium is locked
                if (tierData.isUnlockNowText)
                {
                    if (manager.IsPremiumActive)
                    {
                        if (premiumAmountText != null)
                        {
                            if (tierData.premiumReward.rewardData.Type == RewardType.Attachment)
                                premiumAmountText.text = "ATTACHMENT";
                            else if (manager.RewardTypesToShowAmountText.Contains(tierData.premiumReward.rewardData.Type))
                                premiumAmountText.text = premiumAmountText.text = tierData.premiumReward.amount.ToString("N0");
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
            else
            {
                if (levelNodeImage != null) levelNodeImage.gameObject.SetActive(true);
            }

            bool isLevelUnlocked = tierData.isInstantReward ? manager.IsPremiumActive : manager.CurrentLevel >= tierData.level;
            bool hasPremiumActive = manager.IsPremiumActive;

            // Update level complete/locked badge graphics
            if (levelNodeImage != null && !tierData.isInstantReward)
                levelNodeImage.sprite = isLevelUnlocked
                    ? manager.LevelCompletedSprite
                    : manager.LevelLockedSprite;

            bool freeClaimed = tierData.freeReward != null && tierData.freeReward.isClaimed;
            bool premiumClaimed = tierData.premiumReward != null && tierData.premiumReward.isClaimed;

            // Attachment cards read "ATTACHMENT" while their tier is still locked and flip to
            // "UNLOCK NOW" the moment the player reaches the tier level (the reward is claimable).
            RefreshAttachmentLabel(tierData.freeReward, freeAmountText, isLevelUnlocked);
            RefreshAttachmentLabel(tierData.premiumReward, premiumAmountText, isLevelUnlocked);

            // --- Free Reward UI State ---
            if (!tierData.isInstantReward)
            {
                bool freeClaimable = isLevelUnlocked && !freeClaimed;

                if (freeLockOverlay != null) freeLockOverlay.SetActive(!isLevelUnlocked);
                if (freeTickOverlay != null)
                {
                    bool wasActive = freeTickOverlay.activeSelf;
                    if (!wasActive && freeClaimed && isInitialized)
                    {
                        freeTickOverlay.SetActive(true);
                        StartCoroutine(PopAnimationRoutine(freeTickOverlay.transform));
                    }
                    else
                    {
                        freeTickOverlay.SetActive(freeClaimed);
                        if (!freeClaimed) freeTickOverlay.transform.localScale = Vector3.one;
                    }
                }
                if (freeGlowOutline != null) freeGlowOutline.SetActive(freeClaimable);

                // Set claimed background style
                if (freeClaimed)
                {
                    if (freeCardBg != null && manager.ClaimedCardBgSprite != null)
                        freeCardBg.sprite = manager.ClaimedCardBgSprite;
                }
                else
                {
                    // Restore original background rarity sprite
                    if (freeCardBg != null && tierData.freeReward != null && tierData.freeReward.rewardData != null)
                    {
                        bool showPremiumBg = tierData.isInstantReward || (manager.CurrentLevel >= tierData.level);
                        if (showPremiumBg && manager.HighlightedCardBgSprite != null)
                            freeCardBg.sprite = manager.HighlightedCardBgSprite;
                        else
                            freeCardBg.sprite = manager.GetCardSpriteByRarity(tierData.freeReward.rewardData.Rarity);
                    }
                }

                if (freeCardBg != null)
                {
                    freeCardBg.material = ResolveCardSweepMaterial(freeClaimed, freeClaimable);
                }
            }

            // --- Premium Reward UI State ---
            bool premiumClaimable = isLevelUnlocked && hasPremiumActive && !premiumClaimed;
            bool premiumLocked = !isLevelUnlocked || !hasPremiumActive;

            if (premiumLockOverlay != null) premiumLockOverlay.SetActive(premiumLocked);
            if (premiumTickOverlay != null)
            {
                bool wasActive = premiumTickOverlay.activeSelf;
                if (!wasActive && premiumClaimed && isInitialized)
                {
                    premiumTickOverlay.SetActive(true);
                    StartCoroutine(PopAnimationRoutine(premiumTickOverlay.transform));
                }
                else
                {
                    premiumTickOverlay.SetActive(premiumClaimed);
                    if (!premiumClaimed) premiumTickOverlay.transform.localScale = Vector3.one;
                }
            }
            if (premiumGlowOutline != null) premiumGlowOutline.SetActive(premiumClaimable);

            // Set claimed background style
            if (premiumClaimed)
            {
                if (premiumCardBg != null && manager.ClaimedCardBgSprite != null)
                    premiumCardBg.sprite = manager.ClaimedCardBgSprite;
            }
            else
            {
                // Restore original background rarity sprite
                if (premiumCardBg != null && tierData.premiumReward != null && tierData.premiumReward.rewardData != null)
                {
                    bool showPremiumBg = tierData.isInstantReward || (manager.CurrentLevel >= tierData.level);
                    if (showPremiumBg && manager.HighlightedCardBgSprite != null)
                        premiumCardBg.sprite = manager.HighlightedCardBgSprite;
                    else
                        premiumCardBg.sprite = manager.GetCardSpriteByRarity(tierData.premiumReward.rewardData.Rarity);
                }
            }

            if (premiumCardBg != null)
            {
                // Collectable/highlighted cards keep their original sweep as an enticing teaser:
                // instant showcase cards (CLEOPATRA, SOLARIS, GOLD, ...) shine even while Premium is
                // inactive, since their "unlock" state is gated on the Premium purchase rather than a
                // reached level. Standard cards only shine once their tier level is reached. In both
                // cases a claimed card stops shining.
                bool showPremiumSweep = tierData.isInstantReward
                    ? !premiumClaimed
                    : isLevelUnlocked && !premiumClaimed;
                premiumCardBg.material = ResolveCardSweepMaterial(premiumClaimed, showPremiumSweep);
            }

            // --- Red Dot Notification Badges ---
            bool showFreeRedDot = isLevelUnlocked && 
                                  tierData.freeReward != null && 
                                  tierData.freeReward.rewardData != null && 
                                  !freeClaimed;

            bool showPremiumRedDot = false;
            if (tierData.premiumReward != null && tierData.premiumReward.rewardData != null)
            {
                if (tierData.isInstantReward)
                {
                    // Instant showcase cards keep the badge until the player actually claims them.
                    showPremiumRedDot = !premiumClaimed;
                }
                else
                {
                    // Standard premium rewards show when unlocked and unclaimed
                    showPremiumRedDot = isLevelUnlocked && !premiumClaimed;
                }
            }

            SetRedDotActive(freeRedDot, showFreeRedDot);
            SetRedDotActive(premiumRedDot, showPremiumRedDot);
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
        /// Picks the shine/sweep material for a card background. Collectable/highlighted cards keep
        /// their dedicated sweep (only while it should be shown), while the other (rarity) cards use
        /// their own sweep material so they can shine with a different look. Claimed cards get none.
        /// </summary>
        private Material ResolveCardSweepMaterial(bool isClaimed, bool showHighlightedSweep)
        {
            if (isClaimed) return null;

            bool showsHighlightedBg = (tierData.isInstantReward || (manager.CurrentLevel >= tierData.level))
                                      && manager.HighlightedCardBgSprite != null;

            if (showsHighlightedBg)
                return showHighlightedSweep ? manager.CardSweepMaterial : null;

            return manager.RarityCardSweepMaterial;
        }

        private void OnRewardClicked(bool isPremium)
        {
            manager.OnRewardClicked(this, isPremium);
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
