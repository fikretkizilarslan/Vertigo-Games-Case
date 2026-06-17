using System.Collections.Generic;
using UnityEngine;

namespace BattlePass.UI
{
    /// <summary>
    /// ScriptableObject storing the static/configured tier road data for a Battle Pass season.
    /// Enables swapping season layouts without modifying scenes or manager prefabs.
    /// Contains both the generation rules and the generated tier list.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBattlePassSeasonData", menuName = "Battle Pass/Season Data")]
    public class BattlePassSeasonDataSO : ScriptableObject
    {
        [Header("Auto Generator Settings")]
        [Min(1)] public int generateLevelCount = 50;
        public int instantRewardCount = 7;

        [Header("Instant Reward Showcase (Optional)")]
        [Tooltip("Featured instant-reward cards shown before level 1 (the highlighted SOLARIS / CLEOPATRA row). " +
                 "Add an entry here — e.g. an Attachment reward — to control exactly which cards appear, in order.")]
        public List<InstantRewardEntry> instantRewardShowcase = new List<InstantRewardEntry>();

        [Header("Generated Tiers")]
        [Tooltip("The list of tiers generated/defined for this Battle Pass season.")]
        public List<BattlePassTierData> tiers = new List<BattlePassTierData>();
    }
}
