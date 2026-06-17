using System.Collections;
using UnityEngine;
using TMPro;

namespace BattlePass.UI
{
    /// <summary>
    /// Partial class — Season countdown timer and top XP panel update logic.
    /// Extracted from BattlePassManager.cs to reduce file size while keeping
    /// a single MonoBehaviour component on the scene GameObject.
    ///
    /// All fields (topXpSlider, topXpText, topTargetLevelText, topTimeLeftText,
    /// seasonEndDateTime, currentLevel, currentXp, xpPerLevel) live in the
    /// main BattlePassManager.cs and are accessible here through the partial class.
    /// </summary>
    public partial class BattlePassManager
    {
        // ──────────────────────────────────────────────────────────────────────
        // Top XP Panel
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Refreshes the XP slider, current/target XP label and the next-level number
        /// displayed in the header panel above the Battle Pass road.
        /// </summary>
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

        // ──────────────────────────────────────────────────────────────────────
        // Season Countdown
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Ticks every second and keeps the header time-left label up to date.
        /// Shows "Xd XXh" while more than a day remains; switches to "HH:mm:ss"
        /// on the final day; prints "SEASON ENDED" once the target date passes.
        /// </summary>
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
