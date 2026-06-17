using UnityEngine;

namespace BattlePass.UI
{
    // Reward category - determines card behavior and layout
    public enum RewardType
    {
        Currency,      // Gold, Diamond, Lucky Gem (displays amount)
        Character,     // Cleopatra, Solaris (displays UNLOCK NOW)
        Consumable,    // Dynamite, etc. (displays amount)
        Chest,         // Common/Epic/Legendary Chest
        BoosterPack,   // Rare/Uncommon Booster Pack
        Attachment     // Spine, Weapon Attachment
    }

    /// <summary>
    /// ScriptableObject defining a single reward item type.
    /// Acts as static data template for pass cards.
    /// </summary>
    [CreateAssetMenu(fileName = "NewReward", menuName = "Reward Data/Reward Item Data")]
    public class RewardItemSO : ScriptableObject
    {
        [Header("Visual Information")]
        [Tooltip("The display name on the card (e.g., GOLD, CLEOPATRA)")]
        [SerializeField] private string displayName;

        [Tooltip("The large central icon sprite of the card")]
        [SerializeField] private Sprite icon;

        [Tooltip("Small icon shown next to amounts (e.g., gold coin icon)")]
        [SerializeField] private Sprite currencyIcon;

        [Header("Classification")]
        [Tooltip("The reward type determining the subtitle behavior (amount vs UNLOCK NOW)")]
        [SerializeField] private RewardType rewardType;

        [Tooltip("The rarity determining the frame border background color")]
        [SerializeField] private RewardRarity rarity;

        [Tooltip("If true, only 1 slot can contain this item throughout the entire Battle Pass road (for unique/hero items).")]
        [SerializeField] private bool isUnique = false;

        [Tooltip("If true, this item will be tracked by the Key Reward Indicator even if not unique.")]
        [SerializeField] private bool showInKeyRewardIndicator = false;
        
        [Header("Battle Pass Pool Rules")]
        [Tooltip("Completely excludes this item from the automatic tier generator.")]
        public bool excludeFromBattlePass = false;

        [Tooltip("This item can be placed randomly in the FREE track.")]
        public bool canAppearInFreeTrack = true;

        [Tooltip("This item can be placed randomly in the PREMIUM track.")]
        public bool canAppearInPremiumTrack = true;

        [Header("Distribution Constraints")]
        [Tooltip("Enforces a fixed total amount distribution (e.g., 10 Cleopatra cards distributed across the pass).")]
        [SerializeField] private bool distributeFixedTotal = false;

        [Tooltip("The exact total amount to distribute across the entire road.")]
        [SerializeField] private int fixedTotalAmount = 10;

        // Public read-only accessors
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public Sprite CurrencyIcon => currencyIcon;
        public RewardRarity Rarity => rarity;
        public RewardType Type => rewardType;
        public bool IsUnique => isUnique;
        public bool ShowInKeyRewardIndicator => showInKeyRewardIndicator;
        public bool DistributeFixedTotal => distributeFixedTotal;
        public int FixedTotalAmount => fixedTotalAmount;

        /// <summary>
        /// Returns the formatted subtitle string based on the reward type and amount.
        /// </summary>
        public string GetAmountText(int amount)
        {
            switch (rewardType)
            {
                case RewardType.Character:
                case RewardType.Attachment:
                    return "UNLOCK NOW";

                case RewardType.Currency:
                case RewardType.Consumable:
                case RewardType.Chest:
                case RewardType.BoosterPack:
                    return amount > 1 ? amount.ToString("N0") : "";

                default:
                    return amount.ToString("N0");
            }
        }
    }
}
