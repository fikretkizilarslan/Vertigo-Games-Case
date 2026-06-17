using UnityEngine;

namespace BattlePass.UI
{
    /// <summary>
    /// ScriptableObject tracking the player profile status, level, XP and currency wallet balances.
    /// Simpler version with a single set of fields and a helper to reset to starter defaults.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPlayerProfile", menuName = "Battle Pass/Player Profile")]
    public class PlayerProfileSO : ScriptableObject
    {
        public int currentLevel = 1;
        public int currentXp = 0;
        public int xpPerLevel = 100;
        public bool isPremiumActive = false;
        public int goldBalance = 0;
        public int diamondBalance = 100;
        public int gemBalance = 0;

        /// <summary>
        /// Resets the profile stats back to starter default values.
        /// </summary>
        public void ResetToDefaults()
        {
            currentLevel = 1;
            currentXp = 0;
            xpPerLevel = 100;
            isPremiumActive = false;
            goldBalance = 0;
            diamondBalance = 100;
            gemBalance = 0;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-mark the asset as dirty so Unity saves changes to disk and doesn't discard them on Play Mode entry.
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
