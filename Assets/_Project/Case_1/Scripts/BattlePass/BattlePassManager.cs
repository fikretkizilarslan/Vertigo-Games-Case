using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.UI;
using TMPro;

namespace VertigoCase.UI
{
    public class BattlePassManager : MonoBehaviour
    {
        [Header("Player Data (Runtime Demo)")]
        [Min(0)] [SerializeField] private int currentLevel = 1;
        [Min(0)] [SerializeField] private int currentXp = 0;
        [Min(1)] [SerializeField] private int xpPerLevel = 100;
        [SerializeField] private bool isPremiumActive = false;

        [Header("Tier Setup — Populate from Inspector")]
        [SerializeField] private List<BattlePassTierData> tierList = new List<BattlePassTierData>();

        [Header("Card Background Sprites")]
        [SerializeField] private Sprite cardUncommon;
        [SerializeField] private Sprite cardRare;
        [SerializeField] private Sprite cardEpic;
        [SerializeField] private Sprite cardLegendary;
        [SerializeField] private Sprite cardMythic;
        [SerializeField] private Sprite highlightedCardBgSprite; // ui_event_pass_collectable
        [SerializeField] private Sprite claimedCardBgSprite; // ui_item_level_panel_collected
        [SerializeField] private Material cardSweepMaterial; // Shine sweep for collectable/highlighted cards
        [SerializeField] private Material rarityCardSweepMaterial; // Shine sweep for the other (rarity) cards

        [Header("Card Rarity Glow")]
        [Tooltip("Single shared material (Case1/Sh_UIGlowPulse) used by every card's back glow. Per-card tint comes from the Image vertex color, so no per-card material copies are needed.")]
        [SerializeField] private Material cardGlowMaterial;
        [Tooltip("Tint applied to the back glow per rarity. Drives the shared glow material via the Image's vertex color.")]
        [SerializeField] private Color colorUncommon = new Color(0.30f, 0.80f, 0.25f, 1f);
        [SerializeField] private Color colorRare = new Color(0.23f, 0.63f, 1f, 1f);
        [SerializeField] private Color colorEpic = new Color(0.65f, 0.30f, 1f, 1f);
        [SerializeField] private Color colorLegendary = new Color(1f, 0.54f, 0.12f, 1f);
        [SerializeField] private Color colorMythic = new Color(0.94f, 0.19f, 0.19f, 1f);
        [Tooltip("Tint for the highlighted/collectable (event) cards.")]
        [SerializeField] private Color colorHighlighted = new Color(1f, 0.82f, 0.25f, 1f);

        public Sprite HighlightedCardBgSprite => highlightedCardBgSprite;
        public Sprite ClaimedCardBgSprite => claimedCardBgSprite;
        public Material CardSweepMaterial => cardSweepMaterial;
        public Material RarityCardSweepMaterial => rarityCardSweepMaterial;
        public Material CardGlowMaterial => cardGlowMaterial;
        public bool EnableClaimShine => enableClaimShine;
        public float ClaimShineDuration => claimShineDuration;
        public Material ClaimShineMaterial => claimShineMaterial;
        public Color HighlightedCardColor => colorHighlighted;

        /// <summary>Maps a reward rarity to the tint used by the shared card glow material.</summary>
        public Color GetCardColorByRarity(RewardRarity rarity)
        {
            switch (rarity)
            {
                case RewardRarity.Uncommon: return colorUncommon;
                case RewardRarity.Rare: return colorRare;
                case RewardRarity.Epic: return colorEpic;
                case RewardRarity.Legendary: return colorLegendary;
                case RewardRarity.Mythic: return colorMythic;
                default: return colorUncommon;
            }
        }

        [Header("Level Node Sprites")]
        [SerializeField] private Sprite levelCompletedSprite;
        [SerializeField] private Sprite levelLockedSprite;

        public Sprite LevelCompletedSprite => levelCompletedSprite;
        public Sprite LevelLockedSprite => levelLockedSprite;

        [Header("UI References - Road")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Transform contentContainer;
        [SerializeField] private Slider roadSlider;
        [SerializeField] private UILevelIndicator levelIndicator;
        [SerializeField] private UIKeyRewardIndicator keyRewardIndicator;
        [SerializeField] private UILevelSkipButton levelSkipButton;
        [SerializeField] private RectTransform lineGradient;

        [Header("Top Header XP Panel")]
        [SerializeField] private Slider topXpSlider;
        [SerializeField] private TextMeshProUGUI topXpText;
        [SerializeField] private TextMeshProUGUI topTargetLevelText;
        [SerializeField] private TextMeshProUGUI topTimeLeftText;
        [SerializeField] private string seasonEndDateTime = "2026-06-22T18:00:00";

        [Header("Level Skip Settings")]
        [SerializeField] private int gemCostPerLevel = 20;
        [SerializeField] private Sprite gemIconSprite;
        [SerializeField] private float levelSkipButtonHorizontalOffset = 0f;

        [Header("Diamond Wallet")]
        [Tooltip("Header diamond counter label (scene: Txt_Count_Diamond). Auto-resolved by name when left empty.")]
        [SerializeField] private TextMeshProUGUI diamondCountText;
        [Tooltip("Player's current diamond balance. Diamonds are spent when skipping a level with the Btn_XP_Skip badge.")]
        [Min(0)] [SerializeField] private int diamondBalance = 100;

        [Header("Gold Wallet")]
        [Tooltip("Header gold counter label (scene: Txt_Count_Gold). Auto-resolved by name when left empty.")]
        [SerializeField] private TextMeshProUGUI goldCountText;
        [Tooltip("Player's current gold balance, topped up when a gold reward card is claimed.")]
        [Min(0)] [SerializeField] private int goldBalance = 0;

        [Header("Currency Fly Animation")]
        [Tooltip("DOTween fly animation for gold / diamond wallet changes. Auto-added on this GameObject when empty.")]
        [SerializeField] private CurrencyWalletFlyAnimator walletFlyAnimator;

        [Header("Startup")]
        [Tooltip("Road scroll-view canvas group, faded in once the road finishes building to hide the first-frame layout pop. Auto-resolved from the ScrollRect when empty.")]
        [SerializeField] private CanvasGroup roadCanvasGroup;
        [Tooltip("Fade-in duration (seconds) used to reveal the road on startup.")]
        [Min(0f)] [SerializeField] private float startupRevealDuration = 0.2f;

        [Header("Prefabs")]
        [SerializeField] private GameObject nodePrefab;

        [Header("VFX & Particles - Claim")]
        [Tooltip("Claim VFX spawned when a FREE reward card is claimed.")]
        [SerializeField] private GameObject freeClaimVfxPrefab;
        [Tooltip("Claim VFX spawned when a PREMIUM reward card is claimed.")]
        [SerializeField] private GameObject premiumClaimVfxPrefab;
        [Tooltip("Brief Sh_Shine sweep on the card when a reward is claimed.")]
        [SerializeField] private bool enableClaimShine = true;
        [Tooltip("Sh_Shine material played on the card when claimed. Leave empty to fall back to Card Sweep / Rarity Card Sweep by tier.")]
        [SerializeField] private Material claimShineMaterial;
        [Tooltip("How long the shine material stays on the card after claim (seconds, unscaled).")]
        [Min(0.05f)] [SerializeField] private float claimShineDuration = 0.75f;

        [Header("Premium Offer")]
        [Tooltip("Offer button (scene: Btn_Offer_Get). Tapping it activates the premium track. Auto-resolved by name when left empty.")]
        [SerializeField] private Button offerButton;
        [Tooltip("Scene: Grp_OfferBurst — lock pop / burst played the first time premium is unlocked.")]
        [SerializeField] private OfferBurstSequence offerBurstSequence;

        [Header("UI Customization")]
        [SerializeField] private List<RewardType> rewardTypesToShowAmountText = new List<RewardType>
        {
            RewardType.Currency,
            RewardType.Character,
            RewardType.Consumable,
            RewardType.Attachment
        };

        public List<RewardType> RewardTypesToShowAmountText => rewardTypesToShowAmountText;

        private List<BattlePassNode> instantiatedNodes = new List<BattlePassNode>();
        private RectTransform sliderRectTransform;
        private Coroutine scrollSnapRoutine;
        private readonly Dictionary<Transform, Coroutine> activePunchRoutines = new Dictionary<Transform, Coroutine>();
        private readonly Dictionary<Transform, Vector3> punchBaseScales = new Dictionary<Transform, Vector3>();

        private float initialLineLocalY;
        private float initialLineLocalZ;
        private readonly Vector3[] skipButtonCorners = new Vector3[4];
        private readonly Vector3[] levelNodeCorners = new Vector3[4];
        private Vector3[] m_Corners;
        private WaitForSeconds m_WaitAnimateDelay;

        // LateUpdate performance caching
        private float m_LastScrollPos = -1f;
        private int m_LastScreenWidth = -1;
        private int m_LastScreenHeight = -1;
        private int m_LastLevel = -1;
        private int m_LastXp = -1;

        public int CurrentLevel => currentLevel;
        public bool IsPremiumActive => isPremiumActive;

        [Header("Auto Generator Settings")]
        [SerializeField] private int generateLevelCount = 50;
        [SerializeField] private int instantRewardCount = 7;

        [Header("Instant Reward Showcase (Optional)")]
        [Tooltip("Featured instant-reward cards shown before level 1 (the highlighted SOLARIS / CLEOPATRA row). " +
                 "Add an entry here — e.g. an Attachment reward — to control exactly which cards appear, in order. " +
                 "When this list is empty, the generator falls back to its built-in default sequence.")]
        [SerializeField] private List<InstantRewardEntry> instantRewardShowcase = new List<InstantRewardEntry>();

#if UNITY_EDITOR
        // Editor-only accessors consumed by BattlePassManagerEditor (the tier generator tool).
        // The heavy generation logic lives in Scripts/UI/Editor/BattlePassTierGenerator.cs so this
        // runtime class stays lean and free of editor-only code.
        public List<BattlePassTierData> EditorTierList => tierList;
        public int EditorGenerateLevelCount => generateLevelCount;
        public int EditorInstantRewardCount => instantRewardCount;
        public List<InstantRewardEntry> EditorInstantRewardShowcase => instantRewardShowcase;

        private void OnValidate()
        {
            currentLevel = Mathf.Max(0, currentLevel);
            currentXp = Mathf.Max(0, currentXp);
            xpPerLevel = Mathf.Max(1, xpPerLevel);

            if (Application.isPlaying)
            {
                // Defer the UI update to the next editor update frame to avoid SendMessage warnings during OnValidate
                UnityEditor.EditorApplication.delayCall += UpdateAllUIDeferred;
            }
        }

        private void UpdateAllUIDeferred()
        {
            UnityEditor.EditorApplication.delayCall -= UpdateAllUIDeferred;
            if (Application.isPlaying && this != null)
            {
                // Reset claimed status of all tiers in play mode when Inspector is updated
                if (tierList != null)
                {
                    foreach (var tier in tierList)
                    {
                        if (tier.freeReward != null) tier.freeReward.isClaimed = false;
                        if (tier.premiumReward != null) tier.premiumReward.isClaimed = false;
                    }
                }

                UpdateAllUI();
            }
        }
#endif

        private void Start()
        {
            if (levelIndicator == null)
            {
                levelIndicator = FindFirstObjectByType<UILevelIndicator>();
            }
            if (keyRewardIndicator == null)
            {
                keyRewardIndicator = FindFirstObjectByType<UIKeyRewardIndicator>();
            }

            if (roadSlider != null)
            {
                sliderRectTransform = roadSlider.GetComponent<RectTransform>();
            }

            if (lineGradient != null)
            {
                initialLineLocalY = lineGradient.localPosition.y;
                initialLineLocalZ = lineGradient.localPosition.z;
            }

            m_Corners = new Vector3[4];
            m_WaitAnimateDelay = new WaitForSeconds(0.5f);

            ResolveDiamondWallet();
            ResolveGoldWallet();
            EnsureWalletFlyAnimator();
            walletFlyAnimator?.SyncDisplay(goldBalance, diamondBalance);

            if (levelSkipButton != null)
            {
                levelSkipButton.Bind(TrySkipCurrentLevel);
            }

            ResolveOfferButton();
            ResolveOfferBurst();

            // Keep the road hidden while it is being built so the first-frame layout pop is not visible.
            PrepareRoadReveal();

            // tierList must be populated. Warn if empty.
            if (tierList == null || tierList.Count == 0)
            {
                Debug.LogWarning("[BattlePassManager] tierList is empty! Must be populated in the Inspector.");
                if (roadCanvasGroup != null) roadCanvasGroup.alpha = 1f;
                return;
            }

            SpawnRoadNodes();

            // Test Setup: Mark rewards below level 2 as claimed (levels 2, 3, and 4 will be claimable)
            foreach (var tier in tierList)
            {
                if (tier.level < 2)
                {
                    if (tier.freeReward != null) tier.freeReward.isClaimed = true;
                    if (isPremiumActive && tier.premiumReward != null) tier.premiumReward.isClaimed = true;
                }
            }
            
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
            }

            StartCoroutine(SeasonCountdownRoutine());

            // Settle the layout immediately so the road is correctly positioned on the very first
            // rendered frame, then reveal it. This removes the visible "build then jump" pop on startup.
            Canvas.ForceUpdateCanvases();
            if (contentContainer is RectTransform contentRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            }
            Canvas.ForceUpdateCanvases();

            UpdateAllUI();

            StartCoroutine(StartupRevealRoutine());
            StartCoroutine(DeferredCardPulseRefreshRoutine());
            scrollSnapRoutine = StartCoroutine(AnimateToCurrentLevelRoutine());
        }

        /// <summary>
        /// Re-applies card pulse / glow after the first layout pass so DOTween picks up the
        /// final RectTransform scale (avoids a one-frame miss right after road spawn).
        /// </summary>
        private IEnumerator DeferredCardPulseRefreshRoutine()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            UpdateAllUI();
        }

        /// <summary>
        /// Hides the road scroll view (alpha 0) before it is populated, resolving the
        /// <see cref="roadCanvasGroup"/> from the ScrollRect when it has not been assigned.
        /// </summary>
        private void PrepareRoadReveal()
        {
            if (roadCanvasGroup == null && scrollRect != null)
            {
                roadCanvasGroup = scrollRect.GetComponent<CanvasGroup>();
                if (roadCanvasGroup == null)
                {
                    roadCanvasGroup = scrollRect.gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (roadCanvasGroup != null)
            {
                roadCanvasGroup.alpha = 0f;
            }
        }

        /// <summary>
        /// Fades the road scroll view back in once its layout has been built, so the player
        /// only ever sees the finished, correctly positioned road.
        /// </summary>
        private IEnumerator StartupRevealRoutine()
        {
            if (roadCanvasGroup == null) yield break;

            // One extra frame guarantees the layout rebuild has flushed before we start revealing.
            yield return null;

            float elapsed = 0f;
            while (elapsed < startupRevealDuration && startupRevealDuration > 0f)
            {
                elapsed += Time.unscaledDeltaTime;
                roadCanvasGroup.alpha = Mathf.Clamp01(elapsed / startupRevealDuration);
                yield return null;
            }

            roadCanvasGroup.alpha = 1f;
        }

        private void SpawnRoadNodes()
        {
            if (contentContainer == null) return;

            // Clear existing child nodes except slider and gradient
            foreach (Transform child in contentContainer)
            {
                bool isRoadSlider = (roadSlider != null && child.gameObject == roadSlider.gameObject);
                bool isLineGradient = (lineGradient != null && child.gameObject == lineGradient.gameObject);
                
                if (!isRoadSlider && !isLineGradient)
                {
                    Destroy(child.gameObject);
                }
            }
            instantiatedNodes.Clear();

            // Instantiate prefab node for each tier
            foreach (var tier in tierList)
            {
                if (nodePrefab == null) continue;

                GameObject obj = Instantiate(nodePrefab, contentContainer);
                BattlePassNode node = obj.GetComponent<BattlePassNode>();
                if (node != null)
                {
                    node.Initialize(tier, this);
                    instantiatedNodes.Add(node);
                }
            }

            UpdateFloatingIndicatorTarget();
        }

        public void UpdateAllUI()
        {
            foreach (var node in instantiatedNodes)
            {
                node.UpdateNodeVisualState();
            }

            UpdateProgressLine();
            UpdateFloatingIndicatorTarget();
            UpdateTopXpPanel();
        }

        /// <summary>
        /// Refreshes glow / pulse only for nodes currently near the scroll viewport.
        /// Keeps batch count low when premium unlocks many claimable cards off-screen.
        /// </summary>
        public void RefreshVisibleNodeEffects()
        {
            foreach (var node in instantiatedNodes)
            {
                node.UpdateNodeEffectState();
            }
        }

        /// <summary>
        /// Updates lock overlays, badges and red dots without touching custom card materials.
        /// </summary>
        public void RefreshNodeCoreStates()
        {
            foreach (var node in instantiatedNodes)
            {
                node.UpdateNodeCoreState();
            }
        }

        public bool IsNearViewport(RectTransform anchor, float margin = 420f)
        {
            if (anchor == null || scrollRect == null || scrollRect.viewport == null)
            {
                return true;
            }

            RectTransform viewport = scrollRect.viewport;
            Vector3 local = viewport.InverseTransformPoint(anchor.position);
            float left = -viewport.pivot.x * viewport.rect.width - margin;
            float right = (1f - viewport.pivot.x) * viewport.rect.width + margin;
            return local.x >= left && local.x <= right;
        }

        private void OnScrollValueChanged(Vector2 _)
        {
            UpdateFloatingIndicatorTarget();
            RefreshVisibleNodeEffects();
        }

        private int GetNodeIndexForLevel(int level)
        {
            int idx = instantiatedNodes.FindIndex(n => n.TierData.level == level);
            if (idx == -1) return level <= 0 ? 0 : instantiatedNodes.Count - 1;
            return idx;
        }

        private void UpdateProgressLine()
        {
            if (roadSlider == null || instantiatedNodes == null || instantiatedNodes.Count == 0) return;

            int currentLevelIndex = GetNodeIndexForLevel(currentLevel);
            int nextLevelIndex = Mathf.Min(currentLevelIndex + 1, instantiatedNodes.Count - 1);

            BattlePassNode currentNode = instantiatedNodes[currentLevelIndex];
            BattlePassNode nextNode = instantiatedNodes[nextLevelIndex];

            if (currentNode == null || nextNode == null || currentNode.LevelNodeAnchor == null || nextNode.LevelNodeAnchor == null) return;

            Vector3 currentPos = currentNode.LevelNodeAnchor.position;
            Vector3 nextPos = nextNode.LevelNodeAnchor.position;

            float progressFactor = (float)currentXp / xpPerLevel;
            Vector3 targetWorldPos = Vector3.Lerp(currentPos, nextPos, progressFactor);

            if (sliderRectTransform == null)
            {
                sliderRectTransform = roadSlider.GetComponent<RectTransform>();
            }

            if (sliderRectTransform == null) return;

            Vector3 targetLocalPos = sliderRectTransform.InverseTransformPoint(targetWorldPos);

            float sliderWidth = sliderRectTransform.rect.width;
            float localLeftX = -sliderRectTransform.pivot.x * sliderWidth;

            float percentage = 0f;
            if (sliderWidth > 0f)
            {
                percentage = (targetLocalPos.x - localLeftX) / sliderWidth;
            }
            percentage = Mathf.Clamp01(percentage);

            roadSlider.minValue = 0f;
            roadSlider.maxValue = 1f;
            roadSlider.value = percentage;
        }

        private void LateUpdate()
        {
            if (instantiatedNodes == null || instantiatedNodes.Count == 0) return;

            float currentScroll = scrollRect != null ? scrollRect.horizontalNormalizedPosition : 0f;
            int screenWidth = Screen.width;
            int screenHeight = Screen.height;

            // Run alignment logic only when coordinates, size, or level progress actually change,
            // preventing constant per-frame Canvas and ScrollRect hierarchy dirtying.
            if (currentScroll != m_LastScrollPos ||
                screenWidth != m_LastScreenWidth ||
                screenHeight != m_LastScreenHeight ||
                currentLevel != m_LastLevel ||
                currentXp != m_LastXp)
            {
                m_LastScrollPos = currentScroll;
                m_LastScreenWidth = screenWidth;
                m_LastScreenHeight = screenHeight;
                m_LastLevel = currentLevel;
                m_LastXp = currentXp;

                UpdateProgressLine();
                UpdateFloatingIndicatorTarget();
            }
        }

        public Vector3 GetProgressFillEndWorldPosition()
        {
            if (roadSlider == null || instantiatedNodes.Count == 0 || roadSlider.fillRect == null || m_Corners == null) return Vector3.zero;

            roadSlider.fillRect.GetWorldCorners(m_Corners);

            // Midpoint of right-top and right-bottom corners is the end of fill
            return (m_Corners[2] + m_Corners[3]) / 2f;
        }

        private void UpdateFloatingIndicatorTarget()
        {
            if (instantiatedNodes.Count == 0) return;

            int currentLevelIndex = GetNodeIndexForLevel(currentLevel);
            BattlePassNode currentNode = instantiatedNodes[currentLevelIndex];

            // 1. Viewport Level Indicator (clamped target tracking)
            if (levelIndicator != null)
            {
                int targetLevelIndex = currentLevelIndex;
                int displayLevel = currentLevel;

                // Point to next level if available
                if (currentLevelIndex + 1 < instantiatedNodes.Count)
                {
                    targetLevelIndex = currentLevelIndex + 1;
                    displayLevel = currentLevel + 1;
                }

                BattlePassNode targetNode = instantiatedNodes[targetLevelIndex];
                levelIndicator.SetTarget(targetNode.LevelNodeAnchor, displayLevel);
            }

            // 2. Viewport Key Reward Indicator
            if (keyRewardIndicator != null)
            {
                BattlePassNode nextUnclaimedKeyRewardNode = null;
                RectTransform viewportRect = scrollRect != null ? scrollRect.viewport : null;

                if (viewportRect != null)
                {
                    float rightLocalX = (1f - viewportRect.pivot.x) * viewportRect.rect.width;

                    // Find first unclaimed unique/chest reward off-screen to the right
                    foreach (var node in instantiatedNodes)
                    {
                        var tier = node.TierData;
                        if (tier.level >= currentLevel &&
                            tier.premiumReward != null && 
                            tier.premiumReward.rewardData != null && 
                            !tier.premiumReward.isClaimed)
                        {
                            bool isUniqueReward = tier.premiumReward.rewardData.IsUnique;
                            bool forceShowIndicator = tier.premiumReward.rewardData.ShowInKeyRewardIndicator;

                            if (isUniqueReward || forceShowIndicator)
                            {
                                Vector3 targetWorldPos = node.LevelNodeAnchor.position;
                                Vector3 viewportLocalPos = viewportRect.InverseTransformPoint(targetWorldPos);

                                if (viewportLocalPos.x > rightLocalX)
                                {
                                    nextUnclaimedKeyRewardNode = node;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (nextUnclaimedKeyRewardNode != null)
                {
                    keyRewardIndicator.SetTarget(nextUnclaimedKeyRewardNode.LevelNodeAnchor, nextUnclaimedKeyRewardNode.TierData.premiumReward.rewardData.Icon);
                }
                else
                {
                    keyRewardIndicator.SetTarget(null, null);
                }
            }

            // 3. Progress Line Level Skip Button
            if (levelSkipButton != null)
            {
                if (currentLevel >= generateLevelCount)
                {
                    levelSkipButton.gameObject.SetActive(false);
                    if (lineGradient != null) lineGradient.gameObject.SetActive(false);
                }
                else
                {
                    levelSkipButton.gameObject.SetActive(true);
                    if (lineGradient != null) lineGradient.gameObject.SetActive(true);

                    // The skip badge sits in the gap right after the current (yellow) level
                    // node, with its left tip touching that node, pointing toward the next one.
                    Vector3 skipPos = GetLevelSkipButtonWorldPosition(currentNode);
                    levelSkipButton.transform.position = skipPos;

                    if (lineGradient != null)
                    {
                        Vector3 localPosInContent = contentContainer.InverseTransformPoint(skipPos);
                        lineGradient.localPosition = new Vector3(localPosInContent.x, initialLineLocalY, initialLineLocalZ);
                    }

                    levelSkipButton.SetCost(gemIconSprite, gemCostPerLevel.ToString());
                }
            }
        }

        private Vector3 GetLevelSkipButtonWorldPosition(BattlePassNode currentNode)
        {
            RectTransform skipRect = levelSkipButton != null ? levelSkipButton.GetComponent<RectTransform>() : null;
            RectTransform parentRect = skipRect != null ? skipRect.parent as RectTransform : null;
            RectTransform nodeAnchor = currentNode != null ? currentNode.LevelNodeAnchor : null;

            if (skipRect == null || parentRect == null || nodeAnchor == null)
                return skipRect != null ? skipRect.position : Vector3.zero;

            // Right edge of the current (yellow) level node in the badge's parent space.
            nodeAnchor.GetWorldCorners(levelNodeCorners);
            float nodeRightX = GetRectMaxXInParentSpace(levelNodeCorners, parentRect);

            // Half width of the skip badge so we can align its LEFT tip to the node edge.
            skipRect.GetWorldCorners(skipButtonCorners);
            float skipHalfWidth = GetRectWidthInParentSpace(skipButtonCorners, parentRect) * 0.5f;

            // Keep the badge's current vertical position (on the road), override X only so the
            // badge tip touches the current node and the body points toward the next node.
            Vector3 targetLocalPos = parentRect.InverseTransformPoint(skipRect.position);
            targetLocalPos.x = nodeRightX + skipHalfWidth + levelSkipButtonHorizontalOffset;

            return parentRect.TransformPoint(targetLocalPos);
        }

        private static float GetRectWidthInParentSpace(Vector3[] worldCorners, RectTransform parentRect)
        {
            float minX = float.MaxValue;
            float maxX = float.MinValue;

            for (int i = 0; i < worldCorners.Length; i++)
            {
                float localX = parentRect.InverseTransformPoint(worldCorners[i]).x;
                minX = Mathf.Min(minX, localX);
                maxX = Mathf.Max(maxX, localX);
            }

            return maxX - minX;
        }

        private static float GetRectMaxXInParentSpace(Vector3[] worldCorners, RectTransform parentRect)
        {
            float maxX = float.MinValue;

            for (int i = 0; i < worldCorners.Length; i++)
            {
                float localX = parentRect.InverseTransformPoint(worldCorners[i]).x;
                maxX = Mathf.Max(maxX, localX);
            }

            return maxX;
        }

        public Sprite GetCardSpriteByRarity(RewardRarity rarity)
        {
            switch (rarity)
            {
                case RewardRarity.Uncommon: return cardUncommon;
                case RewardRarity.Rare: return cardRare;
                case RewardRarity.Epic: return cardEpic;
                case RewardRarity.Legendary: return cardLegendary;
                case RewardRarity.Mythic: return cardMythic;
                default: return cardUncommon;
            }
        }

        public void OnRewardClicked(BattlePassNode node, bool isPremium)
        {
            int nodeLevel = node.TierData.level;
            
            if (nodeLevel > currentLevel) return;

            RewardSlot slot = isPremium ? node.TierData.premiumReward : node.TierData.freeReward;
            
            if (slot == null || slot.isClaimed) return;
            if (isPremium && !isPremiumActive) return;

            // Claim the reward
            slot.isClaimed = true;

            // Credit currency rewards into their wallet (e.g. a claimed "💎 15" diamond card tops
            // up the header diamond counter by its amount).
            CreditClaimedReward(slot);

            // Spawn claim VFX if configured
            SpawnClaimVFX(node, isPremium);

            UpdateAllUI();
            node.PlayClaimShine(isPremium);
        }

        /// <summary>
        /// Plays the short claim shine on a node. Safe to call from other scripts / animation events.
        /// </summary>
        public void PlayClaimShine(BattlePassNode node, bool isPremium)
        {
            node?.PlayClaimShine(isPremium);
        }

        /// <summary>
        /// Adds a freshly claimed currency reward into the matching wallet. Diamond rewards (e.g. the
        /// "💎 15" card) top up the header diamond counter (<c>Txt_Count_Diamond</c>) by the reward
        /// amount. Non-currency / non-diamond rewards are ignored.
        /// </summary>
        /// <param name="slot">The reward slot that was just claimed.</param>
        private void CreditClaimedReward(RewardSlot slot)
        {
            if (slot == null || slot.rewardData == null) return;
            if (slot.rewardData.Type != RewardType.Currency) return;

            int amount = Mathf.Max(0, slot.amount);

            if (IsDiamondReward(slot.rewardData))
            {
                int fromBalance = diamondBalance;
                diamondBalance += amount;
                AnimateWalletChange(WalletCurrencyType.Diamond, fromBalance, diamondBalance);
            }
            else if (IsGoldReward(slot.rewardData))
            {
                int fromBalance = goldBalance;
                goldBalance += amount;
                AnimateWalletChange(WalletCurrencyType.Gold, fromBalance, goldBalance);
            }
        }

        private void EnsureWalletFlyAnimator()
        {
            if (walletFlyAnimator == null)
            {
                walletFlyAnimator = GetComponent<CurrencyWalletFlyAnimator>();
            }

            if (walletFlyAnimator == null)
            {
                walletFlyAnimator = gameObject.AddComponent<CurrencyWalletFlyAnimator>();
            }
        }

        private void AnimateWalletChange(WalletCurrencyType currency, int fromValue, int toValue)
        {
            EnsureWalletFlyAnimator();

            if (walletFlyAnimator != null)
            {
                walletFlyAnimator.PlayChange(currency, fromValue, toValue);
                return;
            }

            if (currency == WalletCurrencyType.Gold)
            {
                RefreshGoldCounter();
            }
            else
            {
                RefreshDiamondCounter();
            }
        }

        /// <summary>
        /// Identifies whether a reward is the diamond/hard currency by matching its display or asset
        /// name, so claimed diamond cards can be routed into the header diamond wallet.
        /// </summary>
        /// <param name="reward">Reward item to test.</param>
        /// <returns>True when the reward represents diamonds.</returns>
        private bool IsDiamondReward(RewardItemSO reward)
        {
            return RewardNameContains(reward, "diamond");
        }

        /// <summary>
        /// Identifies whether a reward is the gold/soft currency by matching its display or asset
        /// name, so claimed gold cards can be routed into the header gold wallet.
        /// </summary>
        /// <param name="reward">Reward item to test.</param>
        /// <returns>True when the reward represents gold.</returns>
        private bool IsGoldReward(RewardItemSO reward)
        {
            return RewardNameContains(reward, "gold");
        }

        /// <summary>
        /// Case-insensitive check of a reward's display name and asset name against a keyword,
        /// shared by the currency-wallet routing helpers.
        /// </summary>
        private bool RewardNameContains(RewardItemSO reward, string keyword)
        {
            string displayName = reward.DisplayName != null ? reward.DisplayName.ToLowerInvariant() : string.Empty;
            string assetName = reward.name != null ? reward.name.ToLowerInvariant() : string.Empty;
            return displayName.Contains(keyword) || assetName.Contains(keyword);
        }

        /// <summary>
        /// Resolves the header diamond counter (scene: <c>Txt_Count_Diamond</c>) when it has not
        /// been wired in the Inspector, then paints the current balance.
        /// </summary>
        private void ResolveDiamondWallet()
        {
            if (diamondCountText == null)
            {
                GameObject diamondCounter = GameObject.Find("Txt_Count_Diamond");
                if (diamondCounter != null)
                {
                    diamondCountText = diamondCounter.GetComponent<TextMeshProUGUI>();
                }
            }

            RefreshDiamondCounter();
        }

        /// <summary>
        /// Writes the current <see cref="diamondBalance"/> into the header diamond counter label.
        /// </summary>
        private void RefreshDiamondCounter()
        {
            if (diamondCountText != null)
            {
                diamondCountText.text = diamondBalance.ToString();
            }
        }

        /// <summary>
        /// Resolves the header gold counter (scene: <c>Txt_Count_Gold</c>) when it has not been wired
        /// in the Inspector, then paints the current balance.
        /// </summary>
        private void ResolveGoldWallet()
        {
            if (goldCountText == null)
            {
                GameObject goldCounter = GameObject.Find("Txt_Count_Gold");
                if (goldCounter != null)
                {
                    goldCountText = goldCounter.GetComponent<TextMeshProUGUI>();
                }
            }

            RefreshGoldCounter();
        }

        /// <summary>
        /// Writes the current <see cref="goldBalance"/> into the header gold counter label.
        /// </summary>
        private void RefreshGoldCounter()
        {
            if (goldCountText != null)
            {
                goldCountText.text = goldBalance.ToString("N0");
            }
        }

        /// <summary>
        /// Resolves the premium offer button (scene: <c>Btn_Offer_Get</c>) when it has not been
        /// wired in the Inspector and connects its tap to <see cref="ActivatePremium"/>.
        /// </summary>
        private void ResolveOfferButton()
        {
            if (offerButton == null)
            {
                GameObject offerGo = GameObject.Find("Btn_Offer_Get");
                if (offerGo != null)
                {
                    offerButton = offerGo.GetComponent<Button>();
                }
            }

            if (offerButton != null)
            {
                offerButton.onClick.RemoveListener(ActivatePremium);
                offerButton.onClick.AddListener(ActivatePremium);
            }
        }

        private void ResolveOfferBurst()
        {
            if (offerBurstSequence == null)
            {
                offerBurstSequence = FindFirstObjectByType<OfferBurstSequence>(FindObjectsInactive.Include);
            }

            if (offerBurstSequence == null)
            {
                Debug.LogWarning("[BattlePassManager] OfferBurstSequence not found in scene. Assign Grp_OfferBurst or premium unlock will skip the lock burst.");
            }
        }

        /// <summary>
        /// Unlocks the premium track on the first <c>Btn_Offer_Get</c> tap, then refreshes every
        /// node so premium rewards become claimable.
        /// </summary>
        public void ActivatePremium()
        {
            if (isPremiumActive)
            {
                if (offerButton != null)
                {
                    PlayClickFeedback(offerButton.transform);
                }

                return;
            }

            if (offerBurstSequence != null && !offerBurstSequence.IsPlaying)
            {
                if (offerButton != null)
                {
                    offerButton.interactable = false;
                }

                offerBurstSequence.Play(CompletePremiumActivation);
                return;
            }

            CompletePremiumActivation();
        }

        private void CompletePremiumActivation()
        {
            if (offerButton != null)
            {
                offerButton.interactable = true;
            }

            isPremiumActive = true;
            StartCoroutine(CompletePremiumActivationDeferred());
        }

        private IEnumerator CompletePremiumActivationDeferred()
        {
            yield return null;

            // Cheap pass first: remove lock overlays immediately without enabling off-screen VFX.
            RefreshNodeCoreStates();
            UpdateProgressLine();
            UpdateTopXpPanel();
            UpdateFloatingIndicatorTarget();

            yield return null;

            // Only viewport cards get glow / pulse (avoids +40 batch spike).
            RefreshVisibleNodeEffects();
        }

        /// <summary>
        /// Tap handler for the <c>Btn_XP_Skip</c> badge: spends <see cref="gemCostPerLevel"/>
        /// diamonds, advances one level and re-centres the road on the new level. Does nothing
        /// when the player cannot afford the skip or is already at the final level.
        /// </summary>
        public void TrySkipCurrentLevel()
        {
            if (currentLevel >= generateLevelCount) return;
            if (diamondBalance < gemCostPerLevel) return;

            int fromBalance = diamondBalance;
            diamondBalance -= gemCostPerLevel;
            AnimateWalletChange(WalletCurrencyType.Diamond, fromBalance, diamondBalance);

            if (levelSkipButton != null)
            {
                PlayClickFeedback(levelSkipButton.transform);
            }

            currentLevel = Mathf.Min(currentLevel + 1, generateLevelCount);
            currentXp = 0;

            UpdateAllUI();

            if (scrollRect != null && instantiatedNodes.Count > 0)
            {
                if (scrollSnapRoutine != null) StopCoroutine(scrollSnapRoutine);
                scrollSnapRoutine = StartCoroutine(ScrollToLevelRoutine(currentLevel, 0f, 0.4f));
            }
        }

        /// <summary>
        /// Plays the shared button-tap feedback: an instant scale punch on the tapped button.
        /// </summary>
        private void PlayClickFeedback(Transform buttonTransform)
        {
            if (buttonTransform == null)
            {
                return;
            }

            if (!punchBaseScales.TryGetValue(buttonTransform, out Vector3 baseScale))
            {
                baseScale = buttonTransform.localScale;
                punchBaseScales[buttonTransform] = baseScale;
            }

            if (activePunchRoutines.TryGetValue(buttonTransform, out Coroutine running) && running != null)
            {
                StopCoroutine(running);
                buttonTransform.localScale = baseScale;
            }

            activePunchRoutines[buttonTransform] = StartCoroutine(
                PunchScaleRoutine(buttonTransform, baseScale, 0.18f, 0.22f));
        }

        /// <summary>
        /// Briefly scales <paramref name="target"/> up and back to its original size for tactile
        /// click feedback. Uses unscaled time so it animates even while the game is paused.
        /// </summary>
        private IEnumerator PunchScaleRoutine(Transform target, Vector3 baseScale, float strength, float duration)
        {
            if (target == null) yield break;

            Vector3 peakScale = baseScale * (1f + strength);
            float half = Mathf.Max(0.01f, duration * 0.5f);

            float t = 0f;
            while (t < half)
            {
                if (target == null) yield break;
                t += Time.unscaledDeltaTime;
                target.localScale = Vector3.Lerp(baseScale, peakScale, t / half);
                yield return null;
            }

            t = 0f;
            while (t < half)
            {
                if (target == null) yield break;
                t += Time.unscaledDeltaTime;
                target.localScale = Vector3.Lerp(peakScale, baseScale, t / half);
                yield return null;
            }

            if (target != null)
            {
                target.localScale = baseScale;
            }

            activePunchRoutines[target] = null;
        }

        private void SpawnClaimVFX(BattlePassNode node, bool isPremium)
        {
            GameObject prefabToSpawn = isPremium ? premiumClaimVfxPrefab : freeClaimVfxPrefab;
            SpawnVfxAt(prefabToSpawn, node.GetCardWorldPosition(isPremium));
        }

        /// <summary>
        /// Instantiates a VFX prefab at the given world position under this manager and auto-destroys
        /// it once its longest particle system has finished. Shared by claim VFX and click VFX.
        /// </summary>
        /// <param name="prefab">VFX prefab to spawn (no-op when null).</param>
        /// <param name="worldPosition">World position to place the spawned VFX at.</param>
        private void SpawnVfxAt(GameObject prefab, Vector3 worldPosition)
        {
            if (prefab == null) return;

            // Spawn the VFX instance under this manager's canvas hierarchy
            GameObject vfxInstance = Instantiate(prefab, transform);
            if (vfxInstance == null) return;

            vfxInstance.transform.position = worldPosition;

            // Auto-destroy after the longest active particle system finishes playing
            ParticleSystem[] allPS = vfxInstance.GetComponentsInChildren<ParticleSystem>();
            float duration = 3.0f; // Default fallback if no particle systems found

            if (allPS != null && allPS.Length > 0)
            {
                float maxDuration = 0f;
                foreach (var ps in allPS)
                {
                    if (ps == null) continue;

                    var main = ps.main;
                    float psDuration = main.duration;

                    // If it's not looping, account for the start lifetime offset
                    if (!main.loop)
                    {
                        float lifetime = Mathf.Max(main.startLifetime.constant, main.startLifetime.constantMax);
                        psDuration += lifetime;
                    }

                    if (psDuration > maxDuration)
                    {
                        maxDuration = psDuration;
                    }
                }

                if (maxDuration > 0f)
                {
                    duration = maxDuration;
                }
            }

            Destroy(vfxInstance, duration);
        }

        private IEnumerator AnimateToCurrentLevelRoutine()
        {
            if (scrollRect == null || instantiatedNodes.Count == 0) yield break;

            scrollRect.horizontalNormalizedPosition = 0f;
            yield return null;

            int currentLevelIndex = GetNodeIndexForLevel(currentLevel);
            BattlePassNode currentNode = instantiatedNodes[currentLevelIndex];

            RectTransform contentRect = scrollRect.content;
            RectTransform viewportRect = scrollRect.viewport;
            float contentWidth = contentRect.rect.width;
            float viewportWidth = viewportRect.rect.width;

            if (contentWidth <= viewportWidth) yield break;

            float targetLocalX = contentRect.InverseTransformPoint(currentNode.LevelNodeAnchor.position).x;
            float distanceFromLeft = targetLocalX + (contentRect.pivot.x * contentWidth);
            float desiredScrollPos = distanceFromLeft - (viewportWidth / 2f);
            float targetNormalized = desiredScrollPos / (contentWidth - viewportWidth);
            targetNormalized = Mathf.Clamp01(targetNormalized);

            yield return m_WaitAnimateDelay;

            float duration = 1.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Ease Out Cubic scroll interpolation.
                float easeT = 1f - Mathf.Pow(1f - t, 3f);

                scrollRect.horizontalNormalizedPosition = Mathf.Lerp(0f, targetNormalized, easeT);
                yield return null;
            }

            scrollRect.horizontalNormalizedPosition = targetNormalized;
        }

        /// <summary>
        /// Smoothly scrolls the road so the requested level sits in the centre of the viewport.
        /// Shared by the startup intro animation and the level-skip re-centre.
        /// </summary>
        /// <param name="level">Target level to centre on.</param>
        /// <param name="startDelay">Delay (seconds) before the scroll begins.</param>
        /// <param name="duration">Scroll animation duration (seconds).</param>
        private IEnumerator ScrollToLevelRoutine(int level, float startDelay, float duration)
        {
            if (scrollRect == null || instantiatedNodes.Count == 0) yield break;

            // Wait 1 frame for the layout to rebuild before measuring node positions.
            yield return null;

            int levelIndex = GetNodeIndexForLevel(level);
            BattlePassNode targetNode = instantiatedNodes[levelIndex];
            if (targetNode == null || targetNode.LevelNodeAnchor == null) yield break;

            RectTransform contentRect = scrollRect.content;
            RectTransform viewportRect = scrollRect.viewport;
            float contentWidth = contentRect.rect.width;
            float viewportWidth = viewportRect.rect.width;

            if (contentWidth <= viewportWidth) yield break;

            float targetLocalX = contentRect.InverseTransformPoint(targetNode.LevelNodeAnchor.position).x;
            float distanceFromLeft = targetLocalX + (contentRect.pivot.x * contentWidth);
            float desiredScrollPos = distanceFromLeft - (viewportWidth / 2f);
            float targetNormalized = Mathf.Clamp01(desiredScrollPos / (contentWidth - viewportWidth));

            float startNormalized = scrollRect.horizontalNormalizedPosition;

            if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

            float elapsed = 0f;
            while (elapsed < duration && duration > 0f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Ease Out Cubic scroll interpolation.
                float easeT = 1f - Mathf.Pow(1f - t, 3f);

                scrollRect.horizontalNormalizedPosition = Mathf.Lerp(startNormalized, targetNormalized, easeT);
                yield return null;
            }

            scrollRect.horizontalNormalizedPosition = targetNormalized;
        }

        private void UpdateTopXpPanel()
        {
            if (topXpSlider != null)
            {
                topXpSlider.minValue = 0;
                topXpSlider.maxValue = xpPerLevel;
                topXpSlider.value = currentXp;
            }

            if (topXpText != null)
            {
                topXpText.text = $"{currentXp}/{xpPerLevel}";
            }

            if (topTargetLevelText != null)
            {
                topTargetLevelText.text = (currentLevel + 1).ToString();
            }
        }

        private IEnumerator SeasonCountdownRoutine()
        {
            if (string.IsNullOrEmpty(seasonEndDateTime)) yield break;

            System.DateTime targetDate;
            if (!System.DateTime.TryParse(seasonEndDateTime, out targetDate))
            {
                Debug.LogWarning($"[BattlePassManager] Invalid DateTime format: {seasonEndDateTime}");
                yield break;
            }

            while (true)
            {
                System.TimeSpan diff = targetDate - System.DateTime.Now;
                if (diff.TotalSeconds <= 0)
                {
                    if (topTimeLeftText != null) topTimeLeftText.text = "SEASON ENDED";
                    yield break;
                }

                if (topTimeLeftText != null)
                {
                    if (diff.TotalDays >= 1)
                    {
                        topTimeLeftText.text = $"{Mathf.FloorToInt((float)diff.TotalDays)}d {diff.Hours:D2}h";
                    }
                    else
                    {
                        topTimeLeftText.text = $"{diff.Hours:D2}h {diff.Minutes:D2}m {diff.Seconds:D2}s";
                    }
                }

                yield return new WaitForSeconds(1.0f);
            }
        }
    }
}
