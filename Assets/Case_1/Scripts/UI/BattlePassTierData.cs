using UnityEngine;

namespace VertigoCase.UI
{
    public enum RewardRarity
    {
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythic
    }

    /// <summary>
    /// Holds the runtime state of a reward slot.
    /// References static data from RewardItemSO and tracks amount and claim status.
    /// </summary>
    [System.Serializable]
    public class RewardSlot
    {
        [Tooltip("The reward data assigned to this slot (ScriptableObject)")]
        public RewardItemSO rewardData;

        [Tooltip("The amount of the reward (e.g., 1000 Gold, 2 Diamond)")]
        public int amount = 1;

        [HideInInspector] public bool isClaimed = false;
    }

    /// <summary>
    /// Defines a single tier level on the Battle Pass road.
    /// Contains a Free and a Premium reward slot.
    /// </summary>
    [System.Serializable]
    public class BattlePassTierData
    {
        public int level;
        
        [Tooltip("If true, this is an 'Unlock Now' instant reward tier (hides level indicator and free reward).")]
        public bool isInstantReward;

        [Tooltip("If true, hides the amount and shows 'UNLOCK NOW' text (generally for the first 2 instant rewards).")]
        public bool isUnlockNowText;
        
        [Tooltip("Enables the yellow glowing outline background (e.g., ui_event_pass_collectable).")]
        public bool isHighlighted;

        public RewardSlot freeReward;
        public RewardSlot premiumReward;
        public int xpRequiredToUnlock = 100;
    }
}
