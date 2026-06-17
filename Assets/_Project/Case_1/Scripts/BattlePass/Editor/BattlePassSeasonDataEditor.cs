using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BattlePass.UI
{
    /// <summary>
    /// Custom editor for <see cref="BattlePassSeasonDataSO"/> that handles automatic tier generation
    /// directly inside the ScriptableObject asset itself, keeping the runtime manager clean.
    /// </summary>
    [CustomEditor(typeof(BattlePassSeasonDataSO))]
    public class BattlePassSeasonDataEditor : Editor
    {
        private BattlePassSeasonDataSO _seasonData;
        private int _generateLevelCount;

        public override void OnInspectorGUI()
        {
            InspectorBanner.Draw("Battle Pass Season Data", new Color(0.14f, 0.34f, 0.8f));

            DrawDefaultInspector();

            _seasonData = (BattlePassSeasonDataSO)target;

            EditorGUILayout.Space(8);
            if (GUILayout.Button("Auto Generate Tiers", GUILayout.Height(28)))
            {
                AutoGenerateTiers();
            }
        }

        private void AutoGenerateTiers()
        {
            _generateLevelCount = _seasonData.generateLevelCount;
            int instantRewardCount = _seasonData.instantRewardCount;
            List<BattlePassTierData> tierList = _seasonData.tiers;

            // Find all RewardItemSO assets and filter out those excluded from Battle Pass
            List<RewardItemSO> allRewards = new List<RewardItemSO>();
            string[] guids = AssetDatabase.FindAssets("t:RewardItemSO");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RewardItemSO item = AssetDatabase.LoadAssetAtPath<RewardItemSO>(path);
                if (item != null && !item.excludeFromBattlePass)
                    allRewards.Add(item);
            }

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

            Undo.RecordObject(_seasonData, "Auto Generate Tiers");
            tierList.Clear();

            // --- Instant reward showcase (highlighted cards before level 1) ---
            List<InstantRewardEntry> showcase = _seasonData.instantRewardShowcase;
            List<InstantRewardEntry> validShowcase = (showcase != null)
                ? showcase.FindAll(e => e != null && e.reward != null)
                : null;

            if (validShowcase != null && validShowcase.Count > 0)
            {
                int showcaseCount = validShowcase.Count;
                for (int i = 0; i < showcaseCount; i++)
                {
                    InstantRewardEntry entry = validShowcase[i];

                    BattlePassTierData instantTier = new BattlePassTierData();
                    instantTier.level = -(showcaseCount - i);
                    instantTier.isInstantReward = true;
                    instantTier.isHighlighted = true;
                    instantTier.isUnlockNowText = entry.showUnlockNowText;
                    instantTier.premiumReward = new RewardSlot { rewardData = entry.reward, amount = Mathf.Max(1, entry.amount) };
                    tierList.Add(instantTier);

                    if (entry.reward.IsUnique)
                    {
                        placedUniqueRewards.Add(entry.reward);
                    }
                }
            }
            else
            {
                // Built-in default instant reward sequence (starts from negative levels)
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
                    instantTier.level = -(finalInstantCount - i);
                    instantTier.isInstantReward = true;
                    instantTier.isHighlighted = true;
                    instantTier.isUnlockNowText = (i < 2);
                    instantTier.premiumReward = new RewardSlot { rewardData = item, amount = def.amount };
                    tierList.Add(instantTier);

                    if (item.IsUnique)
                    {
                        placedUniqueRewards.Add(item);
                    }
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
            for (int i = 0; i <= _generateLevelCount; i++)
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
                    Debug.LogWarning($"[BattlePass] Free track: Could not distribute all fragments of {item.DisplayName}. Remaining: {remaining}");
                }
            }

            // Premium Track Post-Pass
            foreach (var kvp in new Dictionary<RewardItemSO, int>(remainingToDistributePremium))
            {
                RewardItemSO item = kvp.Key;
                int remaining = kvp.Value;
                if (remaining <= 0) continue;

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
                    Debug.LogWarning($"[BattlePass] Premium track: Could not distribute all fragments of {item.DisplayName}. Remaining: {remaining}");
                }
            }

            // Guarantee unique premium items are placed
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

            // Guarantee unique free items are placed
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

            // Post-process Gold amounts
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

            EditorUtility.SetDirty(_seasonData);
            AssetDatabase.SaveAssets();
            Debug.Log($"Automatically generated {_generateLevelCount} levels in '{_seasonData.name}'!");
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
            bool isMilestoneLevel = (currentLevel > 0 && (currentLevel % 8 == 0 || currentLevel == _generateLevelCount));
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

            if (filtered.Count == 0)
            {
                filtered = pool.FindAll(r => 
                    (recentList.Count == 0 || r != recentList[recentList.Count - 1]) &&
                    (excludeItem == null || r != excludeItem) &&
                    (!r.IsUnique || !placedUniques.Contains(r)) &&
                    (!remainingToDistribute.ContainsKey(r) || remainingToDistribute[r] > 0)
                );
            }

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

            int spacing = 2;
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

            if (isPremium && picked != null && ((picked.DisplayName != null && picked.DisplayName.ToLower().Contains("gold")) || picked.name.ToLower().Contains("gold")))
            {
                spacing = 8;
            }
            
            nextAllowedLevels[picked] = currentLevel + spacing;
            if (picked.ShowInKeyRewardIndicator && otherNextAllowedLevels != null)
            {
                otherNextAllowedLevels[picked] = currentLevel + spacing;
            }

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
            if (recentList.Count > 1) recentList.RemoveAt(0);

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
    }
}
