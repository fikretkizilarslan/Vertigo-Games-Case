using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.UI;
using TMPro;

namespace BattlePass.UI
{
    public partial class BattlePassManager : MonoBehaviour
    {
        [Header("Player Data (Runtime Demo)")]
        [UnityEngine.Serialization.FormerlySerializedAs("currentLevel")]
        [Min(0)] [SerializeField] private int fallbackLevel = 1;
        [UnityEngine.Serialization.FormerlySerializedAs("currentXp")]
        [Min(0)] [SerializeField] private int fallbackXp = 0;
        [UnityEngine.Serialization.FormerlySerializedAs("xpPerLevel")]
        [Min(1)] [SerializeField] private int fallbackXpPerLevel = 100;
        [UnityEngine.Serialization.FormerlySerializedAs("isPremiumActive")]
        [SerializeField] private bool fallbackPremiumActive = false;

        [Header("ScriptableObject Architecture (Optional Overrides)")]
        [SerializeField] private BattlePassSeasonDataSO seasonDataSO;
        [SerializeField] private PlayerProfileSO playerProfileSO;

        [Header("Tier Setup — Populate from Inspector")]
        [HideInInspector] [SerializeField] private List<BattlePassTierData> tierList = new List<BattlePassTierData>();

        // ──────────────────────────────────────────────────────────────────────
        // ScriptableObject Integration Properties
        // ──────────────────────────────────────────────────────────────────────
        public int currentLevel
        {
            get => playerProfileSO != null ? playerProfileSO.currentLevel : fallbackLevel;
            set
            {
                if (playerProfileSO != null) playerProfileSO.currentLevel = value;
                else fallbackLevel = value;
            }
        }

        private int currentXp
        {
            get => playerProfileSO != null ? playerProfileSO.currentXp : fallbackXp;
            set
            {
                if (playerProfileSO != null) playerProfileSO.currentXp = value;
                else fallbackXp = value;
            }
        }

        private int xpPerLevel
        {
            get => playerProfileSO != null ? playerProfileSO.xpPerLevel : fallbackXpPerLevel;
            set
            {
                if (playerProfileSO != null) playerProfileSO.xpPerLevel = value;
                else fallbackXpPerLevel = value;
            }
        }

        private bool isPremiumActive
        {
            get => playerProfileSO != null ? playerProfileSO.isPremiumActive : fallbackPremiumActive;
            set
            {
                if (playerProfileSO != null) playerProfileSO.isPremiumActive = value;
                else fallbackPremiumActive = value;
            }
        }

        private int goldBalance
        {
            get => playerProfileSO != null ? playerProfileSO.goldBalance : fallbackGoldBalance;
            set
            {
                if (playerProfileSO != null) playerProfileSO.goldBalance = value;
                else fallbackGoldBalance = value;
            }
        }

        private int diamondBalance
        {
            get => playerProfileSO != null ? playerProfileSO.diamondBalance : fallbackDiamondBalance;
            set
            {
                if (playerProfileSO != null) playerProfileSO.diamondBalance = value;
                else fallbackDiamondBalance = value;
            }
        }

        private int gemBalance
        {
            get => playerProfileSO != null ? playerProfileSO.gemBalance : fallbackGemBalance;
            set
            {
                if (playerProfileSO != null) playerProfileSO.gemBalance = value;
                else fallbackGemBalance = value;
            }
        }

        [Header("Visual Config")]
        [Tooltip("Shared card frame sprites, shine/glow materials, per-rarity glow tints and level node sprites. Create via Assets > Create > Battle Pass > Visual Config.")]
        [SerializeField] private BattlePassVisualConfig visualConfig;

        public Sprite HighlightedCardBgSprite => visualConfig != null ? visualConfig.HighlightedCardBgSprite : null;
        public Sprite ClaimedCardBgSprite => visualConfig != null ? visualConfig.ClaimedCardBgSprite : null;
        public Material CardSweepMaterial => visualConfig != null ? visualConfig.CardSweepMaterial : null;
        public Material RarityCardSweepMaterial => visualConfig != null ? visualConfig.RarityCardSweepMaterial : null;
        public Material CardGlowMaterial => visualConfig != null ? visualConfig.CardGlowMaterial : null;
        public bool EnableClaimShine => enableClaimShine;
        public float ClaimShineDuration => claimShineDuration;
        public Material ClaimShineMaterial => claimShineMaterial;
        public Color HighlightedCardColor => visualConfig != null ? visualConfig.HighlightedCardColor : Color.white;
        public Sprite LevelCompletedSprite => visualConfig != null ? visualConfig.LevelCompletedSprite : null;
        public Sprite LevelLockedSprite => visualConfig != null ? visualConfig.LevelLockedSprite : null;

        /// <summary>Maps a reward rarity to the tint used by the shared card glow material.</summary>
        public Color GetCardColorByRarity(RewardRarity rarity)
        {
            return visualConfig != null ? visualConfig.GetCardColorByRarity(rarity) : Color.white;
        }

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
        [UnityEngine.Serialization.FormerlySerializedAs("diamondBalance")]
        [Min(0)] [SerializeField] private int fallbackDiamondBalance = 100;

        [Header("Gold Wallet")]
        [Tooltip("Header gold counter label (scene: Txt_Count_Gold). Auto-resolved by name when left empty.")]
        [SerializeField] private TextMeshProUGUI goldCountText;
        [Tooltip("Player's current gold balance, topped up when a gold reward card is claimed.")]
        [UnityEngine.Serialization.FormerlySerializedAs("goldBalance")]
        [Min(0)] [SerializeField] private int fallbackGoldBalance = 0;

        [Header("Lucky Gem Wallet")]
        [Tooltip("Header gem counter (scene: Txt_Count_Gem). Wallet UI: Grp_Gem — hidden until revealed.")]
        [SerializeField] private TextMeshProUGUI gemCountText;
        [UnityEngine.Serialization.FormerlySerializedAs("gemBalance")]
        [Min(0)] [SerializeField] private int fallbackGemBalance = 0;
        [SerializeField] private GemWalletController gemWalletController;

        [Header("Currency Fly Animation")]
        [Tooltip("DOTween fly animation for gold / diamond wallet changes. Auto-added on this GameObject when empty.")]
        [SerializeField] private CurrencyWalletFlyAnimator walletFlyAnimator;

        [Header("Startup")]
        [Tooltip("Road scroll-view canvas group, faded in once the road finishes building to hide the first-frame layout pop. Auto-resolved from the ScrollRect when empty.")]
        [SerializeField] private CanvasGroup roadCanvasGroup;
        [Tooltip("Fade-in duration (seconds) used to reveal the road on startup.")]
        [Min(0f)] [SerializeField] private float startupRevealDuration = 0f;

        [Header("Prefabs")]
        [SerializeField] private GameObject nodePrefab;
        [Tooltip("VFX object pool. Auto-added when left empty. Replaces Instantiate+Destroy for claim VFX, keeping GC allocations flat on mobile.")]
        [SerializeField] private VfxPool vfxPool;

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
        [Tooltip("Text showing premium active/inactive status on the banner. Auto-resolved by name when left empty.")]
        [SerializeField] private TextMeshProUGUI premiumStatusBannerText;

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

        // LateUpdate performance caching
        private float m_LastScrollPos = -1f;
        private int m_LastScreenWidth = -1;
        private int m_LastScreenHeight = -1;
        private int m_LastLevel = -1;
        private int m_LastXp = -1;

        public int CurrentLevel => currentLevel;
        public bool IsPremiumActive => isPremiumActive;

        [Header("Auto Generator Settings (Fallback)")]
        [UnityEngine.Serialization.FormerlySerializedAs("generateLevelCount")]
        [SerializeField] private int fallbackGenerateLevelCount = 50;
        [UnityEngine.Serialization.FormerlySerializedAs("instantRewardCount")]
        [SerializeField] private int fallbackInstantRewardCount = 7;

        [Header("Instant Reward Showcase (Fallback)")]
        [Tooltip("Featured instant-reward cards shown before level 1. Fallback when Season Data SO is not assigned.")]
        [UnityEngine.Serialization.FormerlySerializedAs("instantRewardShowcase")]
        [SerializeField] private List<InstantRewardEntry> fallbackInstantRewardShowcase = new List<InstantRewardEntry>();

        private int generateLevelCount => seasonDataSO != null ? seasonDataSO.generateLevelCount : fallbackGenerateLevelCount;
        private int instantRewardCount => seasonDataSO != null ? seasonDataSO.instantRewardCount : fallbackInstantRewardCount;
        private List<InstantRewardEntry> instantRewardShowcase => seasonDataSO != null ? seasonDataSO.instantRewardShowcase : fallbackInstantRewardShowcase;

#if UNITY_EDITOR
        public List<BattlePassTierData> EditorTierList => tierList;

        private void OnValidate()
        {
            fallbackLevel = Mathf.Max(0, fallbackLevel);
            fallbackXp = Mathf.Max(0, fallbackXp);
            fallbackXpPerLevel = Mathf.Max(1, fallbackXpPerLevel);

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
            // Always start with premium closed (inactive) on game launch
            isPremiumActive = false;

            // Let the ScriptableObject values edited in the Inspector be the runtime source of truth.

            if (seasonDataSO != null && seasonDataSO.tiers != null)
            {
                tierList = new List<BattlePassTierData>();
                foreach (var tier in seasonDataSO.tiers)
                {
                    if (tier != null)
                    {
                        tierList.Add(tier.Clone());
                    }
                }
            }

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

            ResolveDiamondWallet();
            ResolveGoldWallet();
            ResolveGemWallet();
            EnsureWalletFlyAnimator();
            EnsureGemWalletController();
            EnsureVfxPool();
            walletFlyAnimator?.SyncDisplay(goldBalance, diamondBalance, gemBalance);
            gemWalletController?.HideImmediate();

            if (levelSkipButton != null)
            {
                levelSkipButton.Bind(TrySkipCurrentLevel);
            }

            ResolveOfferButton();
            ResolveOfferBurst();
            ResolvePremiumStatusBanner();
            UpdatePremiumStatusBanner();

            // Keep the road hidden while it is being built so the first-frame layout pop is not visible.
            PrepareRoadReveal();

            // tierList must be populated. Warn if empty.
            if (tierList == null || tierList.Count == 0)
            {
                Debug.LogWarning("[BattlePassManager] tierList is empty! Must be populated in the Inspector.");
                if (roadCanvasGroup != null) roadCanvasGroup.alpha = 1f;
                return;
            }

            // Test Setup: Mark rewards below level 2 as claimed (levels 2, 3, and 4 will be claimable).
            // Done before spawning so each node already renders its correct claimed state in
            // Initialize, letting us skip a second full-node UI pass on the heavy build frame.
            foreach (var tier in tierList)
            {
                if (tier.level < 2)
                {
                    if (tier.freeReward != null) tier.freeReward.isClaimed = true;
                    if (isPremiumActive && tier.premiumReward != null) tier.premiumReward.isClaimed = true;
                }
            }

            StartCoroutine(SeasonCountdownRoutine());

            // Build the road across a few frames so pressing Play never blocks on instantiating
            // every card in a single frame. The rest of the scene (header, background) shows
            // instantly while the road is assembled behind the hidden CanvasGroup, then snaps in
            // already centred on the current level — no multi-second freeze, no intro auto-scroll.
            StartCoroutine(StartupBuildRoutine());
        }

        /// <summary>
        /// Spreads the road build over several frames and reveals it already centred on the current
        /// level. Keeps the heavy instantiate cost off a single frame (no startup freeze) and skips
        /// the old intro auto-scroll so the scene is interactive immediately.
        /// </summary>
        private IEnumerator StartupBuildRoutine()
        {
            // Paint the static scene (header, offer, background) on the very first frame before
            // spending any time on the road, so Play feels instant even while the road is built
            // behind the hidden CanvasGroup.
            yield return null;

            // Turn off layout driving on the road content for the duration of the build. With the
            // HorizontalLayoutGroup + ContentSizeFitter active, every single node added forces a
            // re-solve of the whole (growing) content each frame — an O(n^2) stall that is what made
            // startup hang for seconds. We disable them, instantiate every node cheaply, then
            // re-enable and rebuild the final layout exactly once.
            LayoutGroup contentLayout = null;
            ContentSizeFitter contentFitter = null;
            if (contentContainer != null)
            {
                contentLayout = contentContainer.GetComponent<LayoutGroup>();
                contentFitter = contentContainer.GetComponent<ContentSizeFitter>();
            }
            bool layoutWasEnabled = contentLayout != null && contentLayout.enabled;
            bool fitterWasEnabled = contentFitter != null && contentFitter.enabled;
            if (contentLayout != null) contentLayout.enabled = false;
            if (contentFitter != null) contentFitter.enabled = false;

            yield return StartCoroutine(SpawnRoadNodesRoutine());

            // Restore layout driving and solve the final road layout in a single pass.
            if (contentLayout != null) contentLayout.enabled = layoutWasEnabled;
            if (contentFitter != null) contentFitter.enabled = fitterWasEnabled;

            Canvas.ForceUpdateCanvases();
            if (contentContainer is RectTransform contentRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            }
            Canvas.ForceUpdateCanvases();

            // Land directly on the current level instead of animating in from level 0.
            JumpScrollToLevel(currentLevel);

            if (scrollRect != null)
            {
                scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
            }

            UpdateProgressLine();
            UpdateFloatingIndicatorTarget();
            UpdateTopXpPanel();
            RefreshVisibleNodeEffects();

            // Reveal the finished, correctly positioned road.
            if (startupRevealDuration <= 0f)
            {
                if (roadCanvasGroup != null) roadCanvasGroup.alpha = 1f;
            }
            else
            {
                yield return StartCoroutine(StartupRevealRoutine());
            }
        }

        /// <summary>
        /// Instantly centres the scroll view on the requested level (no animation). Shared startup
        /// helper so the road opens directly on the current level.
        /// </summary>
        private void JumpScrollToLevel(int level)
        {
            if (scrollRect == null || instantiatedNodes.Count == 0) return;

            int levelIndex = GetNodeIndexForLevel(level);
            BattlePassNode targetNode = instantiatedNodes[levelIndex];
            if (targetNode == null || targetNode.LevelNodeAnchor == null) return;

            RectTransform contentRect = scrollRect.content;
            RectTransform viewportRect = scrollRect.viewport;
            if (contentRect == null || viewportRect == null) return;

            float contentWidth = contentRect.rect.width;
            float viewportWidth = viewportRect.rect.width;
            if (contentWidth <= viewportWidth)
            {
                scrollRect.horizontalNormalizedPosition = 0f;
                return;
            }

            float targetLocalX = contentRect.InverseTransformPoint(targetNode.LevelNodeAnchor.position).x;
            float distanceFromLeft = targetLocalX + (contentRect.pivot.x * contentWidth);
            float desiredScrollPos = distanceFromLeft - (viewportWidth / 2f);
            scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(desiredScrollPos / (contentWidth - viewportWidth));
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

        /// <summary>
        /// Instantiates one node per tier, spread over several frames so the build never blocks the
        /// main thread in a single frame. The road stays hidden (alpha 0) until the build finishes.
        /// </summary>
        private IEnumerator SpawnRoadNodesRoutine()
        {
            if (contentContainer == null) yield break;

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

            // How many nodes to instantiate per frame. Layout driving is disabled by the caller during
            // the build, so each instantiate is cheap and we can spawn a large batch per frame while
            // still spreading the cost enough to avoid a single visible hitch.
            const int spawnBatchSize = 12;
            int spawnedInBatch = 0;

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

                spawnedInBatch++;
                if (spawnedInBatch >= spawnBatchSize)
                {
                    spawnedInBatch = 0;
                    yield return null;
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
            return visualConfig != null ? visualConfig.GetCardSpriteByRarity(rarity) : null;
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

            node.PlayClaimShine(isPremium);
            UpdateAllUI();
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
            else if (IsLuckyGemReward(slot.rewardData))
            {
                int fromBalance = gemBalance;
                gemBalance += amount;
                CreditLuckyGemReward(fromBalance, gemBalance);
            }
        }

        private void CreditLuckyGemReward(int fromBalance, int toBalance)
        {
            EnsureGemWalletController();
            if (gemWalletController != null)
            {
                gemWalletController.RevealWallet();
                AnimateWalletChange(WalletCurrencyType.Gem, fromBalance, toBalance, () =>
                {
                    gemWalletController.HideWalletAnimated();
                });
                return;
            }

            AnimateWalletChange(WalletCurrencyType.Gem, fromBalance, toBalance);
        }

        private void EnsureGemWalletController()
        {
            if (gemWalletController == null)
            {
                gemWalletController = GetComponent<GemWalletController>();
            }

            if (gemWalletController == null)
            {
                gemWalletController = gameObject.AddComponent<GemWalletController>();
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

        private void AnimateWalletChange(WalletCurrencyType currency, int fromValue, int toValue, Action onComplete = null)
        {
            EnsureWalletFlyAnimator();

            if (walletFlyAnimator != null)
            {
                walletFlyAnimator.PlayChange(currency, fromValue, toValue, onComplete);
                return;
            }

            if (currency == WalletCurrencyType.Gold)
            {
                RefreshGoldCounter();
            }
            else if (currency == WalletCurrencyType.Diamond)
            {
                RefreshDiamondCounter();
            }
            else
            {
                RefreshGemCounter();
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// Identifies whether a reward is the diamond/hard currency.
        /// Checks <see cref="CurrencySubtype"/> first for an explicit mapping;
        /// falls back to display-name / asset-name matching for legacy assets
        /// that do not have the field set.
        /// </summary>
        private bool IsDiamondReward(RewardItemSO reward)
        {
            if (reward.CurrencySubtype == CurrencySubtype.Diamond) return true;
            if (reward.CurrencySubtype != CurrencySubtype.None) return false;
            return RewardNameContains(reward, "diamond");
        }

        /// <summary>
        /// Identifies whether a reward is the gold/soft currency.
        /// Checks <see cref="CurrencySubtype"/> first for an explicit mapping;
        /// falls back to display-name / asset-name matching for legacy assets
        /// that do not have the field set.
        /// </summary>
        private bool IsGoldReward(RewardItemSO reward)
        {
            if (reward.CurrencySubtype == CurrencySubtype.Gold) return true;
            if (reward.CurrencySubtype != CurrencySubtype.None) return false;
            return RewardNameContains(reward, "gold");
        }

        /// <summary>
        /// Identifies Lucky Gem currency rewards (not gold, not diamond).
        /// Checks <see cref="CurrencySubtype"/> first for an explicit mapping;
        /// falls back to display-name / asset-name matching for legacy assets
        /// that do not have the field set.
        /// </summary>
        private bool IsLuckyGemReward(RewardItemSO reward)
        {
            if (reward == null) return false;

            // Explicit enum check — no string parsing needed.
            if (reward.CurrencySubtype == CurrencySubtype.LuckyGem) return true;
            if (reward.CurrencySubtype == CurrencySubtype.Gold || reward.CurrencySubtype == CurrencySubtype.Diamond) return false;

            // Legacy fallback: exclude known non-gem currencies first, then name-match.
            if (IsDiamondReward(reward) || IsGoldReward(reward)) return false;

            string displayName = reward.DisplayName != null ? reward.DisplayName.ToLowerInvariant() : string.Empty;
            string assetName = reward.name != null ? reward.name.ToLowerInvariant() : string.Empty;
            return displayName.Contains("lucky") || displayName.Contains("gem")
                || assetName.Contains("lucky") || assetName.Contains("gem");
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
                Debug.LogWarning(
                    "[BattlePassManager] Txt_Count_Diamond not wired in Inspector — " +
                    "falling back to GameObject.Find. Assign diamondCountText directly to avoid this search.");
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
                Debug.LogWarning(
                    "[BattlePassManager] Txt_Count_Gold not wired in Inspector — " +
                    "falling back to GameObject.Find. Assign goldCountText directly to avoid this search.");
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

        private void ResolveGemWallet()
        {
            if (gemCountText == null)
            {
                Debug.LogWarning(
                    "[BattlePassManager] Txt_Count_Gem not wired in Inspector — " +
                    "falling back to GameObject.Find. Assign gemCountText directly to avoid this search.");
                GameObject gemCounter = GameObject.Find("Txt_Count_Gem");
                if (gemCounter != null)
                {
                    gemCountText = gemCounter.GetComponent<TextMeshProUGUI>();
                }
            }

            RefreshGemCounter();
        }

        private void RefreshGemCounter()
        {
            if (gemCountText != null)
            {
                gemCountText.text = gemBalance.ToString();
            }
        }

        // ── Premium offer flow ─────────────────────────────────────────────────
        // See BattlePassManager.Premium.cs (partial class)
        // ResolveOfferButton · ResolveOfferBurst · ActivatePremium
        // CompletePremiumActivation · CompletePremiumActivationDeferred

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
        /// Spawns a VFX prefab via the <see cref="VfxPool"/>, falling back to
        /// Instantiate+Destroy when the pool is unavailable.
        /// Shared by claim VFX (<see cref="SpawnClaimVFX"/>) and any future click VFX.
        /// </summary>
        /// <param name="prefab">VFX prefab to spawn (no-op when null).</param>
        /// <param name="worldPosition">World position to place the VFX at.</param>
        private void SpawnVfxAt(GameObject prefab, Vector3 worldPosition)
        {
            if (prefab == null) return;

            // Prefer the pool to avoid per-claim GC allocations on mobile.
            if (vfxPool != null)
            {
                vfxPool.Get(prefab, worldPosition);
                return;
            }

            // Fallback: legacy Instantiate + Destroy when no pool is assigned.
            GameObject vfxInstance = Instantiate(prefab, transform);
            if (vfxInstance == null) return;

            vfxInstance.transform.position = worldPosition;

            ParticleSystem[] allPS = vfxInstance.GetComponentsInChildren<ParticleSystem>();
            float duration = 3.0f;

            if (allPS != null && allPS.Length > 0)
            {
                float maxDuration = 0f;
                foreach (var ps in allPS)
                {
                    if (ps == null) continue;
                    var main = ps.main;
                    float psDuration = main.duration;
                    if (!main.loop)
                    {
                        float lifetime = Mathf.Max(main.startLifetime.constant, main.startLifetime.constantMax);
                        psDuration += lifetime;
                    }
                    if (psDuration > maxDuration) maxDuration = psDuration;
                }
                if (maxDuration > 0f) duration = maxDuration;
            }

            Destroy(vfxInstance, duration);
        }

        /// <summary>
        /// Ensures a <see cref="VfxPool"/> is attached to this GameObject.
        /// Auto-adds the component when it has not been wired in the Inspector,
        /// so the first claim VFX never falls back to the legacy Instantiate path.
        /// </summary>
        private void EnsureVfxPool()
        {
            if (vfxPool != null) return;
            vfxPool = GetComponent<VfxPool>();
            if (vfxPool == null)
            {
                vfxPool = gameObject.AddComponent<VfxPool>();
            }
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

        private void ResolvePremiumStatusBanner()
        {
            if (premiumStatusBannerText == null)
            {
                GameObject statusGo = GameObject.Find("Txt_Banner_Premium_Inactive");
                if (statusGo != null)
                {
                    premiumStatusBannerText = statusGo.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        public void UpdatePremiumStatusBanner()
        {
            if (premiumStatusBannerText != null)
            {
                premiumStatusBannerText.text = isPremiumActive ? "ACTIVE" : "INACTIVE";
            }
        }

        // ── Season countdown & XP panel ────────────────────────────────────────
        // See BattlePassManager.Season.cs (partial class)
        // UpdateTopXpPanel · SeasonCountdownRoutine
    }
}
