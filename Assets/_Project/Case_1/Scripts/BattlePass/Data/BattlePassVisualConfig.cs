using UnityEngine;

namespace BattlePass.UI
{
    /// <summary>
    /// Shared visual theme for the Battle Pass: card frame sprites, shine/glow materials,
    /// per-rarity glow tints and level node sprites. Extracted from <see cref="BattlePassManager"/>
    /// so the manager Inspector stays focused on scene wiring and runtime data.
    /// </summary>
    [CreateAssetMenu(fileName = "BattlePassVisualConfig", menuName = "Battle Pass/Visual Config")]
    public class BattlePassVisualConfig : ScriptableObject
    {
        [Header("Card Background Sprites")]
        [SerializeField] private Sprite cardUncommon;
        [SerializeField] private Sprite cardRare;
        [SerializeField] private Sprite cardEpic;
        [SerializeField] private Sprite cardLegendary;
        [SerializeField] private Sprite cardMythic;
        [SerializeField] private Sprite highlightedCardBgSprite; // ui_event_pass_collectable
        [SerializeField] private Sprite claimedCardBgSprite; // ui_item_level_panel_collected

        [Header("Card Shine Materials")]
        [Tooltip("Shine sweep for collectable/highlighted cards.")]
        [SerializeField] private Material cardSweepMaterial;
        [Tooltip("Shine sweep for the other (rarity) cards.")]
        [SerializeField] private Material rarityCardSweepMaterial;

        [Header("Card Rarity Glow")]
        [Tooltip("Single shared material (Case1/Sh_UIGlowPulse) used by every card's back glow. Per-card tint comes from the Image vertex color.")]
        [SerializeField] private Material cardGlowMaterial;
        [SerializeField] private Color colorUncommon = new Color(0.30f, 0.80f, 0.25f, 1f);
        [SerializeField] private Color colorRare = new Color(0.23f, 0.63f, 1f, 1f);
        [SerializeField] private Color colorEpic = new Color(0.65f, 0.30f, 1f, 1f);
        [SerializeField] private Color colorLegendary = new Color(1f, 0.54f, 0.12f, 1f);
        [SerializeField] private Color colorMythic = new Color(0.94f, 0.19f, 0.19f, 1f);
        [Tooltip("Tint for the highlighted/collectable (event) cards.")]
        [SerializeField] private Color colorHighlighted = new Color(1f, 0.82f, 0.25f, 1f);

        [Header("Level Node Sprites")]
        [SerializeField] private Sprite levelCompletedSprite;
        [SerializeField] private Sprite levelLockedSprite;

        public Sprite HighlightedCardBgSprite => highlightedCardBgSprite;
        public Sprite ClaimedCardBgSprite => claimedCardBgSprite;
        public Material CardSweepMaterial => cardSweepMaterial;
        public Material RarityCardSweepMaterial => rarityCardSweepMaterial;
        public Material CardGlowMaterial => cardGlowMaterial;
        public Color HighlightedCardColor => colorHighlighted;
        public Sprite LevelCompletedSprite => levelCompletedSprite;
        public Sprite LevelLockedSprite => levelLockedSprite;

        /// <summary>Card frame sprite for a reward rarity.</summary>
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

        /// <summary>Back-glow tint for a reward rarity.</summary>
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
    }
}
