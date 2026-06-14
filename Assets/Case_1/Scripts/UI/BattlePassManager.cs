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

        public Sprite HighlightedCardBgSprite => highlightedCardBgSprite;
        public Sprite ClaimedCardBgSprite => claimedCardBgSprite;
        public Material CardSweepMaterial => cardSweepMaterial;
        public Material RarityCardSweepMaterial => rarityCardSweepMaterial;

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

        [Header("Prefabs")]
        [SerializeField] private GameObject nodePrefab;

        [Header("VFX & Particles")]
        [SerializeField] private GameObject freeClaimVfxPrefab;
        [SerializeField] private GameObject premiumClaimVfxPrefab;

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

        private float initialLineLocalY;
        private float initialLineLocalZ;
        private readonly Vector3[] skipButtonCorners = new Vector3[4];
        private readonly Vector3[] levelNodeCorners = new Vector3[4];

        public int CurrentLevel => currentLevel;
        public bool IsPremiumActive => isPremiumActive;

        [Header("Auto Generator Settings")]
        [SerializeField] private int generateLevelCount = 50;
        [SerializeField] private int instantRewardCount = 7;

#if UNITY_EDITOR

        [ContextMenu("Auto Generate Tiers")]
        private void AutoGenerateTiers()
        {
            // Find all RewardItemSO assets and filter out those excluded from Battle Pass
            List<RewardItemSO> allRewards = new List<RewardItemSO>();
#if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets("t:RewardItemSO");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RewardItemSO item = AssetDatabase.LoadAssetAtPath<RewardItemSO>(path);
                if (item != null && !item.excludeFromBattlePass)
                    allRewards.Add(item);
            }
#endif
            // Separate free and premium pools
            List<RewardItemSO> freePool = allRewards.FindAll(r => r.canAppearInFreeTrack);
            List<RewardItemSO> premiumPool = allRewards.FindAll(r => r.canAppearInPremiumTrack);
            
            // Set to track unique rewards and prevent duplication
            HashSet<RewardItemSO> placedUniqueRewards = new HashSet<RewardItemSO>();

            // Categorize free rewards
            List<RewardItemSO> freeCurrencies = freePool.FindAll(r => r.Type == RewardType.Currency);
            List<RewardItemSO> freeOthers = freePool.FindAll(r => r.Type != RewardType.Currency);
            
            // Categorize premium rewards
            List<RewardItemSO> premCurrencies = premiumPool.FindAll(r => r.Type == RewardType.Currency);
            List<RewardItemSO> premOthers = premiumPool.FindAll(r => r.Type != RewardType.Currency);

            if (allRewards.Count == 0)
            {
                Debug.LogWarning("No RewardItemSO found in project!");
                return;
            }

            tierList.Clear();

            // Fixed instant reward sequence (starts from negative levels)
            var instantRewardDefs = new[]
            {
                new { name = "cleopatra", amount = 1, unlockNow = true },
                new { name = "solaris",   amount = 1, unlockNow = true },
                new { name = "cleopatra", amount = 2, unlockNow = false },
                new { name = "solaris",   amount = 2, unlockNow = false },
                new { name = "gold",      amount = 1000, unlockNow = false },
                new { name = "diamond",   amount = 5, unlockNow = false },
                new { name = "lucky",     amount = 10, unlockNow = false },
            };

            int finalInstantCount = Mathf.Clamp(instantRewardCount, 0, 100);
            for (int i = 0; i < finalInstantCount; i++)
            {
                var def = instantRewardDefs[i % instantRewardDefs.Length];
                RewardItemSO item = GetReward(def.name, allRewards);
                if (item == null) continue;

                BattlePassTierData instantTier = new BattlePassTierData();
                instantTier.level = -(finalInstantCount - i); // e.g. -7, -6, -5
                instantTier.isInstantReward = true;
                instantTier.isHighlighted = true;
                instantTier.isUnlockNowText = (i < 2);
                instantTier.premiumReward = new RewardSlot { rewardData = item, amount = def.amount };
                tierList.Add(instantTier);

                // Add unique starter items to the unique set to prevent repetition
                if (item.IsUnique)
                {
                    placedUniqueRewards.Add(item);
                }
            }

            // Anti-repetition check: track last 2 choices to enforce minimum spacing
            List<RewardItemSO> recentFree = new List<RewardItemSO>();
            List<RewardItemSO> recentPrem = new List<RewardItemSO>();

            RewardItemSO goldReward = GetReward("gold", allRewards);
            RewardItemSO diamondReward = GetReward("diamond", allRewards);

            // Spacing constraint dictionaries tracking allowed levels
            Dictionary<RewardItemSO, int> nextAllowedLevelsFree = new Dictionary<RewardItemSO, int>();
            Dictionary<RewardItemSO, int> nextAllowedLevelsPremium = new Dictionary<RewardItemSO, int>();

            // Tracking remaining fragments/amounts for distribution
            Dictionary<RewardItemSO, int> remainingToDistributeFree = new Dictionary<RewardItemSO, int>();
            Dictionary<RewardItemSO, int> remainingToDistributePremium = new Dictionary<RewardItemSO, int>();

            foreach (var item in freePool)
            {
                if (item.DistributeFixedTotal)
                {
                    remainingToDistributeFree[item] = item.FixedTotalAmount;
                }
            }

            foreach (var item in premiumPool)
            {
                if (item.DistributeFixedTotal)
                {
                    remainingToDistributePremium[item] = item.FixedTotalAmount;
                }
            }

            // Standard levels (Level 0 is ticket icon, Level 1+ are normal levels)
            for (int i = 0; i <= generateLevelCount; i++)
            {
                BattlePassTierData tier = new BattlePassTierData();
                tier.level = i;
                tier.isHighlighted = false;

                // Free Reward selection according to constraints
                tier.freeReward = new RewardSlot();

                if (freePool.Count > 0)
                {
                    RewardItemSO freeItem = PickWithoutRepeat(freePool, recentFree, placedUniqueRewards, nextAllowedLevelsFree, remainingToDistributeFree, i, null, recentPrem, nextAllowedLevelsPremium, false);
                    tier.freeReward.rewardData = freeItem;

                    // Determine slot amount based on reward type
                    if (freeItem.DistributeFixedTotal && remainingToDistributeFree.ContainsKey(freeItem))
                    {
                        int remaining = remainingToDistributeFree[freeItem];
                        int maxPerSlot = (freeItem.FixedTotalAmount <= 5) ? 1 : Random.Range(1, 4);
                        int amount = Mathf.Min(maxPerSlot, remaining);
                        tier.freeReward.amount = amount;
                        remainingToDistributeFree[freeItem] -= amount;
                    }
                    else if (freeItem.Type == RewardType.Currency)
                    {
                        if (freeItem.DisplayName.ToLower().Contains("gold"))
                            tier.freeReward.amount = 1000 + (i * 150);
                        else
                            tier.freeReward.amount = 5 + i;
                    }
                    else if (freeItem.Type == RewardType.Consumable)
                    {
                        tier.freeReward.amount = Random.Range(2, 6);
                    }
                    else if (freeItem.Type == RewardType.Character)
                    {
                        tier.freeReward.amount = Random.Range(1, 4);
                    }
                    else
                    {
                        tier.freeReward.amount = 1;
                    }
                }

                // --- Premium Reward selection according to constraints ---
                tier.premiumReward = new RewardSlot();
                
                // Specific premium rewards for levels 0 and 1
                if (i == 0 && goldReward != null) 
                { 
                    tier.premiumReward.rewardData = goldReward; 
                    tier.premiumReward.amount = 5000; 
                    recentPrem.Add(goldReward); 
                    if (goldReward.IsUnique) placedUniqueRewards.Add(goldReward); 
                    nextAllowedLevelsPremium[goldReward] = 8; 
                }
                else if (i == 1 && diamondReward != null) 
                { 
                    tier.premiumReward.rewardData = diamondReward; 
                    tier.premiumReward.amount = 12; 
                    recentPrem.Add(diamondReward); 
                    if (diamondReward.IsUnique) placedUniqueRewards.Add(diamondReward); 
                    nextAllowedLevelsPremium[diamondReward] = 3; 
                }
                else if (premiumPool.Count > 0)
                {
                    RewardItemSO premItem = PickWithoutRepeat(premiumPool, recentPrem, placedUniqueRewards, nextAllowedLevelsPremium, remainingToDistributePremium, i, tier.freeReward != null ? tier.freeReward.rewardData : null, recentFree, nextAllowedLevelsFree, true);
                    tier.premiumReward.rewardData = premItem;

                    if (premItem.DistributeFixedTotal && remainingToDistributePremium.ContainsKey(premItem))
                    {
                        int remaining = remainingToDistributePremium[premItem];
                        int maxPerSlot = (premItem.FixedTotalAmount <= 5) ? 1 : Random.Range(1, 4);
                        int amount = Mathf.Min(maxPerSlot, remaining);
                        tier.premiumReward.amount = amount;
                        remainingToDistributePremium[premItem] -= amount;
                    }
                    else if (premItem.Type == RewardType.Currency)
                    {
                        if (premItem.DisplayName.ToLower().Contains("gold"))
                            tier.premiumReward.amount = 1500 + (i * 300);
                        else
                            tier.premiumReward.amount = 10 + (i * 2);
                    }
                    else if (premItem.Type == RewardType.Consumable)
                    {
                        tier.premiumReward.amount = Random.Range(2, 8);
                    }
                    else if (premItem.Type == RewardType.Character)
                    {
                        tier.premiumReward.amount = Random.Range(1, 4);
                    }
                    else
                    {
                        tier.premiumReward.amount = 1;
                    }
                }

                tierList.Add(tier);
            }

            // --- POST-PASS: Distribute remaining fragments ---
            
            // Free Track Post-Pass
            foreach (var kvp in new Dictionary<RewardItemSO, int>(remainingToDistributeFree))
            {
                RewardItemSO item = kvp.Key;
                int remaining = kvp.Value;
                if (remaining <= 0) continue;

                // 1. Try adding to existing slots of this item
                List<BattlePassTierData> existingTiers = tierList.FindAll(t => !t.isInstantReward && t.freeReward != null && t.freeReward.rewardData == item);
                foreach (var tier in existingTiers)
                {
                    if (remaining <= 0) break;
                    int maxCap = (item.ShowInKeyRewardIndicator && !item.DistributeFixedTotal) ? 9999 : 3;
                    int addable = Mathf.Max(0, maxCap - tier.freeReward.amount);
                    if (addable > 0)
                    {
                        int toAdd = Mathf.Min(addable, remaining);
                        tier.freeReward.amount += toAdd;
                        remaining -= toAdd;
                    }
                }

                // 2. Replace non-unique, non-distribution items with this item
                if (remaining > 0 && (!item.ShowInKeyRewardIndicator || item.DistributeFixedTotal))
                {
                    List<BattlePassTierData> candidateTiers = tierList.FindAll(t => 
                        !t.isInstantReward && 
                        t.level > 0 &&
                        t.freeReward != null && 
                        t.freeReward.rewardData != null && 
                        !t.freeReward.rewardData.IsUnique && 
                        !t.freeReward.rewardData.DistributeFixedTotal
                    );

                    // Shuffle candidate tiers
                    for (int idx = 0; idx < candidateTiers.Count; idx++)
                    {
                        int tempIdx = Random.Range(idx, candidateTiers.Count);
                        var temp = candidateTiers[idx];
                        candidateTiers[idx] = candidateTiers[tempIdx];
                        candidateTiers[tempIdx] = temp;
                    }

                    foreach (var tier in candidateTiers)
                    {
                        if (remaining <= 0) break;

                        // Check for adjacent duplicate rewards on same track, or same-level duplicate on other track
                        int L = tier.level;
                        bool hasDuplicate = tierList.Exists(t => 
                            ((t.level == L - 1 || t.level == L + 1) && t.freeReward != null && t.freeReward.rewardData == item) ||
                            (t.level == L && t.premiumReward != null && t.premiumReward.rewardData == item)
                        );
                        if (hasDuplicate) continue;

                        tier.freeReward.rewardData = item;
                        int amount = Mathf.Min(Random.Range(1, 4), remaining);
                        tier.freeReward.amount = amount;
                        remaining -= amount;
                    }
                }

                if (remaining > 0)
                {
                    Debug.LogWarning($"[BattlePassManager] Free track: Could not distribute all fragments of {item.DisplayName}. Remaining: {remaining}");
                }
            }

            // Premium Track Post-Pass
            foreach (var kvp in new Dictionary<RewardItemSO, int>(remainingToDistributePremium))
            {
                RewardItemSO item = kvp.Key;
                int remaining = kvp.Value;
                if (remaining <= 0) continue;

                // 1. Try adding to existing slots of this item
                List<BattlePassTierData> existingTiers = tierList.FindAll(t => !t.isInstantReward && t.premiumReward != null && t.premiumReward.rewardData == item);
                foreach (var tier in existingTiers)
                {
                    if (remaining <= 0) break;
                    int maxCap = (item.ShowInKeyRewardIndicator && !item.DistributeFixedTotal) ? 9999 : 3;
                    int addable = Mathf.Max(0, maxCap - tier.premiumReward.amount);
                    if (addable > 0)
                    {
                        int toAdd = Mathf.Min(addable, remaining);
                        tier.premiumReward.amount += toAdd;
                        remaining -= toAdd;
                    }
                }

                // 2. Replace non-unique, non-distribution items with this item
                if (remaining > 0 && (!item.ShowInKeyRewardIndicator || item.DistributeFixedTotal))
                {
                    List<BattlePassTierData> candidateTiers = tierList.FindAll(t => 
                        !t.isInstantReward && 
                        t.level > 1 &&
                        t.premiumReward != null && 
                        t.premiumReward.rewardData != null && 
                        !t.premiumReward.rewardData.IsUnique && 
                        !t.premiumReward.rewardData.DistributeFixedTotal
                    );

                    // Shuffle candidate tiers
                    for (int idx = 0; idx < candidateTiers.Count; idx++)
                    {
                        int tempIdx = Random.Range(idx, candidateTiers.Count);
                        var temp = candidateTiers[idx];
                        candidateTiers[idx] = candidateTiers[tempIdx];
                        candidateTiers[tempIdx] = temp;
                    }

                    foreach (var tier in candidateTiers)
                    {
                        if (remaining <= 0) break;

                        // Check for adjacent duplicate rewards on same track, or same-level duplicate on other track
                        int L = tier.level;
                        bool hasDuplicate = tierList.Exists(t => 
                            ((t.level == L - 1 || t.level == L + 1) && t.premiumReward != null && t.premiumReward.rewardData == item) ||
                            (t.level == L && t.freeReward != null && t.freeReward.rewardData == item)
                        );
                        if (hasDuplicate) continue;

                        tier.premiumReward.rewardData = item;
                        int amount = Mathf.Min(Random.Range(1, 4), remaining);
                        tier.premiumReward.amount = amount;
                        remaining -= amount;
                    }
                }

                if (remaining > 0)
                {
                    Debug.LogWarning($"[BattlePassManager] Premium track: Could not distribute all fragments of {item.DisplayName}. Remaining: {remaining}");
                }
            }

            // Guarantee all unique premium items (like Anubis) are placed at least once
            List<RewardItemSO> uniquePremiumPool = premOthers.FindAll(r => r.IsUnique);
            foreach (var uniqueItem in uniquePremiumPool)
            {
                if (!placedUniqueRewards.Contains(uniqueItem))
                {
                    List<BattlePassTierData> candidates = tierList.FindAll(t => 
                        !t.isInstantReward && 
                        t.level > 1 && 
                        t.premiumReward != null && 
                        t.premiumReward.rewardData != null && 
                        !t.premiumReward.rewardData.IsUnique && 
                        !t.premiumReward.rewardData.DistributeFixedTotal
                    );
                    if (candidates.Count > 0)
                    {
                        BattlePassTierData targetTier = candidates[Random.Range(0, candidates.Count)];
                        targetTier.premiumReward.rewardData = uniqueItem;
                        targetTier.premiumReward.amount = 1;
                        placedUniqueRewards.Add(uniqueItem);
                    }
                }
            }

            // Guarantee all unique free items are placed at least once
            List<RewardItemSO> uniqueFreePool = freeOthers.FindAll(r => r.IsUnique);
            foreach (var uniqueItem in uniqueFreePool)
            {
                if (!placedUniqueRewards.Contains(uniqueItem))
                {
                    List<BattlePassTierData> candidates = tierList.FindAll(t => 
                        !t.isInstantReward && 
                        t.level > 0 && 
                        t.freeReward != null && 
                        t.freeReward.rewardData != null && 
                        !t.freeReward.rewardData.IsUnique && 
                        !t.freeReward.rewardData.DistributeFixedTotal
                    );
                    if (candidates.Count > 0)
                    {
                        BattlePassTierData targetTier = candidates[Random.Range(0, candidates.Count)];
                        targetTier.freeReward.rewardData = uniqueItem;
                        targetTier.freeReward.amount = 1;
                        placedUniqueRewards.Add(uniqueItem);
                    }
                }
            }

            // Post-process Gold amounts to follow the exact progression rules
            int finalFreeGoldCount = 0;
            int finalPremiumGoldCount = 0;

            foreach (var t in tierList)
            {
                if (!t.isInstantReward)
                {
                    if (t.freeReward != null && t.freeReward.rewardData != null && 
                        ((t.freeReward.rewardData.DisplayName != null && t.freeReward.rewardData.DisplayName.ToLower().Contains("gold")) || t.freeReward.rewardData.name.ToLower().Contains("gold")))
                    {
                        finalFreeGoldCount++;
                        if (finalFreeGoldCount <= 3)
                        {
                            t.freeReward.amount = 1000;
                        }
                        else
                        {
                            t.freeReward.amount = 1000 + 200 + (finalFreeGoldCount - 4) * 250;
                        }
                    }

                    if (t.premiumReward != null && t.premiumReward.rewardData != null && 
                        ((t.premiumReward.rewardData.DisplayName != null && t.premiumReward.rewardData.DisplayName.ToLower().Contains("gold")) || t.premiumReward.rewardData.name.ToLower().Contains("gold")))
                    {
                        finalPremiumGoldCount++;
                        t.premiumReward.amount = 5000 + (finalPremiumGoldCount - 1) * 1000;
                    }
                }
            }

            EditorUtility.SetDirty(this);
            Debug.Log($"Automatically generated {generateLevelCount} levels!");
        }

        private RewardItemSO GetReward(string namePart, List<RewardItemSO> allRewards)
        {
            string lowerName = namePart.ToLower();
            foreach (var r in allRewards) 
                if (r != null && (r.name.ToLower().Contains(lowerName) || r.DisplayName.ToLower().Contains(lowerName))) return r;
            return null;
        }

        private RewardItemSO PickWithoutRepeat(
            List<RewardItemSO> pool, 
            List<RewardItemSO> recentList, 
            HashSet<RewardItemSO> placedUniques, 
            Dictionary<RewardItemSO, int> nextAllowedLevels,
            Dictionary<RewardItemSO, int> remainingToDistribute,
            int currentLevel,
            RewardItemSO excludeItem = null,
            List<RewardItemSO> otherRecentList = null,
            Dictionary<RewardItemSO, int> otherNextAllowedLevels = null,
            bool isPremium = false)
        {
            bool isMilestoneLevel = (currentLevel > 0 && (currentLevel % 8 == 0 || currentLevel == generateLevelCount));
            
            // Check if the last item placed on this track was Uncommon to prevent consecutive uncommons
            bool lastWasUncommon = (recentList.Count > 0 && recentList[recentList.Count - 1].Rarity == RewardRarity.Uncommon);

            List<RewardItemSO> filtered = pool.FindAll(r => 
                !recentList.Contains(r) && 
                (excludeItem == null || r != excludeItem) &&
                (!r.IsUnique || !placedUniques.Contains(r)) &&
                (isMilestoneLevel ? r.ShowInKeyRewardIndicator : !r.ShowInKeyRewardIndicator) &&
                (!lastWasUncommon || r.Rarity != RewardRarity.Uncommon) &&
                (!nextAllowedLevels.ContainsKey(r) || currentLevel >= nextAllowedLevels[r]) &&
                (!remainingToDistribute.ContainsKey(r) || remainingToDistribute[r] > 0)
            );

            // Fallback 1: Relax full recent history but keep milestone logic, spacing, consecutive uncommons check, and immediate previous exclusions
            if (filtered.Count == 0) 
            {
                filtered = pool.FindAll(r => 
                    (recentList.Count == 0 || r != recentList[recentList.Count - 1]) &&
                    (excludeItem == null || r != excludeItem) &&
                    (!r.IsUnique || !placedUniques.Contains(r)) &&
                    (isMilestoneLevel ? r.ShowInKeyRewardIndicator : !r.ShowInKeyRewardIndicator) &&
                    (!lastWasUncommon || r.Rarity != RewardRarity.Uncommon) &&
                    (!nextAllowedLevels.ContainsKey(r) || currentLevel >= nextAllowedLevels[r]) &&
                    (!remainingToDistribute.ContainsKey(r) || remainingToDistribute[r] > 0)
                );
            }

            // Fallback 2: Relax spacing cooldowns but STILL keep consecutive uncommons check, milestone logic and immediate previous exclusions
            if (filtered.Count == 0)
            {
                filtered = pool.FindAll(r => 
                    (recentList.Count == 0 || r != recentList[recentList.Count - 1]) &&
                    (excludeItem == null || r != excludeItem) &&
                    (!r.IsUnique || !placedUniques.Contains(r)) &&
                    (isMilestoneLevel ? r.ShowInKeyRewardIndicator : !r.ShowInKeyRewardIndicator) &&
                    (!lastWasUncommon || r.Rarity != RewardRarity.Uncommon) &&
                    (!remainingToDistribute.ContainsKey(r) || remainingToDistribute[r] > 0)
                );
            }

            // Fallback 3: Relax spacing cooldowns AND consecutive uncommons check but keep milestone logic and immediate previous exclusions
            if (filtered.Count == 0)
            {
                filtered = pool.FindAll(r => 
                    (recentList.Count == 0 || r != recentList[recentList.Count - 1]) &&
                    (excludeItem == null || r != excludeItem) &&
                    (!r.IsUnique || !placedUniques.Contains(r)) &&
                    (isMilestoneLevel ? r.ShowInKeyRewardIndicator : !r.ShowInKeyRewardIndicator) &&
                    (!remainingToDistribute.ContainsKey(r) || remainingToDistribute[r] > 0)
                );
            }

            // Fallback 4: Relax milestone logic but STILL keep immediate previous and same-level exclusions
            if (filtered.Count == 0)
            {
                filtered = pool.FindAll(r => 
                    (recentList.Count == 0 || r != recentList[recentList.Count - 1]) &&
                    (excludeItem == null || r != excludeItem) &&
                    (!r.IsUnique || !placedUniques.Contains(r)) &&
                    (!remainingToDistribute.ContainsKey(r) || remainingToDistribute[r] > 0)
                );
            }

            // Fallback 5: Absolute backup (only if mathematically locked, relax immediate constraints but keep uniqueness)
            if (filtered.Count == 0)
            {
                filtered = pool.FindAll(r => 
                    (!r.IsUnique || !placedUniques.Contains(r)) &&
                    (!remainingToDistribute.ContainsKey(r) || remainingToDistribute[r] > 0)
                );
                if (filtered.Count == 0)
                {
                    filtered = pool.FindAll(r => !remainingToDistribute.ContainsKey(r) || remainingToDistribute[r] > 0);
                    if (filtered.Count == 0) filtered = pool;
                }
            }

            // Weighted random selection based on rarity weights to control card densities
            int totalWeight = 0;
            List<int> weights = new List<int>();
            foreach (var r in filtered)
            {
                int w = GetRarityWeight(r.Rarity);
                weights.Add(w);
                totalWeight += w;
            }

            RewardItemSO picked = filtered[0];
            if (totalWeight > 0)
            {
                int randomValue = Random.Range(0, totalWeight);
                int currentSum = 0;
                for (int idx = 0; idx < filtered.Count; idx++)
                {
                    currentSum += weights[idx];
                    if (randomValue < currentSum)
                    {
                        picked = filtered[idx];
                        break;
                    }
                }
            }

            if (picked.IsUnique)
            {
                placedUniques.Add(picked);
            }

            // 1. Rarity-based spacing (prevents identical items from repeating too close)
            // Gentler cooldowns to allow natural alternation (rare = 2, epic = 3, etc.)
            int spacing = 2; // Default for Uncommon
            switch (picked.Rarity)
            {
                case RewardRarity.Rare:
                    spacing = 2;
                    break;
                case RewardRarity.Epic:
                    spacing = 3;
                    break;
                case RewardRarity.Legendary:
                    spacing = 4;
                    break;
                case RewardRarity.Mythic:
                    spacing = 5;
                    break;
            }

            // Override for Premium Gold to make it rarer
            if (isPremium && picked != null && ((picked.DisplayName != null && picked.DisplayName.ToLower().Contains("gold")) || picked.name.ToLower().Contains("gold")))
            {
                spacing = 8;
            }
            
            nextAllowedLevels[picked] = currentLevel + spacing;
            // Only share cooldowns cross-track for Key Rewards
            if (picked.ShowInKeyRewardIndicator && otherNextAllowedLevels != null)
            {
                otherNextAllowedLevels[picked] = currentLevel + spacing;
            }

            // 2. Spacing for Key Rewards (ShowInKeyRewardIndicator = true) - minimum interval of 8 levels between any key rewards
            if (picked.ShowInKeyRewardIndicator)
            {
                foreach (var r in pool)
                {
                    if (r.ShowInKeyRewardIndicator)
                    {
                        nextAllowedLevels[r] = currentLevel + 8;
                        if (otherNextAllowedLevels != null)
                        {
                            otherNextAllowedLevels[r] = currentLevel + 8;
                        }
                    }
                }
            }

            recentList.Add(picked);
            if (recentList.Count > 1) recentList.RemoveAt(0); // Size 1 for standard items to prevent starvation

            // Only share history cross-track for Key Rewards
            if (picked.ShowInKeyRewardIndicator && otherRecentList != null)
            {
                otherRecentList.Add(picked);
                if (otherRecentList.Count > 1) otherRecentList.RemoveAt(0);
            }

            return picked;
        }

        private int GetRarityWeight(RewardRarity rarity)
        {
            switch (rarity)
            {
                case RewardRarity.Uncommon:
                    return 10;
                case RewardRarity.Rare:
                    return 30;
                case RewardRarity.Epic:
                    return 30;
                case RewardRarity.Legendary:
                    return 15;
                case RewardRarity.Mythic:
                    return 10;
                default:
                    return 10;
            }
        }
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

            // tierList must be populated. Warn if empty.
            if (tierList == null || tierList.Count == 0)
            {
                Debug.LogWarning("[BattlePassManager] tierList is empty! Must be populated in the Inspector.");
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
                scrollRect.onValueChanged.AddListener((vec) => UpdateFloatingIndicatorTarget());
            }

            StartCoroutine(SeasonCountdownRoutine());

            // Force canvas update to let layouts resolve, then update UI
            Canvas.ForceUpdateCanvases();
            AutoAttachLocalUVModifiers();
            UpdateAllUI();
            StartCoroutine(AnimateToCurrentLevelRoutine());
        }

        private void AutoAttachLocalUVModifiers()
        {
            Graphic[] graphics = FindObjectsByType<Graphic>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Graphic graphic in graphics)
            {
                if (graphic != null && graphic.material != null && graphic.material.shader != null)
                {
                    if (graphic.material.shader.name == "Case1/Sh_Shine")
                    {
                        if (graphic.GetComponent<UILocalUVModifier>() == null)
                        {
                            graphic.gameObject.AddComponent<UILocalUVModifier>();
                        }
                    }
                }
            }
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
            // Refresh all UI elements
            foreach (var node in instantiatedNodes)
            {
                node.UpdateNodeVisualState();
            }

            UpdateProgressLine();
            UpdateFloatingIndicatorTarget();
            UpdateTopXpPanel();
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
            // Refreshes alignments on LateUpdate to support dynamic screen resizing
            if (instantiatedNodes != null && instantiatedNodes.Count > 0)
            {
                UpdateProgressLine();
                UpdateFloatingIndicatorTarget();
            }
        }

        public Vector3 GetProgressFillEndWorldPosition()
        {
            if (roadSlider == null || instantiatedNodes.Count == 0 || roadSlider.fillRect == null) return Vector3.zero;

            // Retrieve the right edge world position of the slider fill area
            Vector3[] corners = new Vector3[4];
            roadSlider.fillRect.GetWorldCorners(corners);
            
            // Midpoint of right-top and right-bottom corners is the end of fill
            return (corners[2] + corners[3]) / 2f;
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

                    levelSkipButton.UpdateSkipCost(gemIconSprite, gemCostPerLevel.ToString());
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

            // Spawn claim VFX if configured
            SpawnClaimVFX(node, isPremium);

            UpdateAllUI();
        }

        private void SpawnClaimVFX(BattlePassNode node, bool isPremium)
        {
            GameObject prefabToSpawn = isPremium ? premiumClaimVfxPrefab : freeClaimVfxPrefab;
            if (prefabToSpawn == null) return;

            // Spawn the VFX instance under this manager's canvas hierarchy
            GameObject vfxInstance = Instantiate(prefabToSpawn, transform);
            
            if (vfxInstance != null)
            {
                // Position VFX at the clicked card's world position
                vfxInstance.transform.position = node.GetCardWorldPosition(isPremium);
                
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
        }

        private IEnumerator AnimateToCurrentLevelRoutine()
        {
            if (scrollRect == null || instantiatedNodes.Count == 0) yield break;

            // Reset horizontal scroll position to zero
            scrollRect.horizontalNormalizedPosition = 0f;
            
            // Wait 1 frame for hierarchy layout to rebuild
            yield return null;

            int currentLevelIndex = GetNodeIndexForLevel(currentLevel);
            BattlePassNode currentNode = instantiatedNodes[currentLevelIndex];

            RectTransform contentRect = scrollRect.content;
            RectTransform viewportRect = scrollRect.viewport;
            float contentWidth = contentRect.rect.width;
            float viewportWidth = viewportRect.rect.width;

            if (contentWidth <= viewportWidth) yield break;

            // Compute local target scroll position
            float targetLocalX = contentRect.InverseTransformPoint(currentNode.LevelNodeAnchor.position).x;
            float distanceFromLeft = targetLocalX + (contentRect.pivot.x * contentWidth);
            float desiredScrollPos = distanceFromLeft - (viewportWidth / 2f);
            float targetNormalized = desiredScrollPos / (contentWidth - viewportWidth);
            targetNormalized = Mathf.Clamp01(targetNormalized);

            // Delay scroll scroll animation slightly for visual effect
            yield return new WaitForSeconds(0.5f);

            float duration = 1.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Ease Out Cubic scroll interpolation
                float easeT = 1f - Mathf.Pow(1f - t, 3f);
                
                scrollRect.horizontalNormalizedPosition = Mathf.Lerp(0f, targetNormalized, easeT);
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
