using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VertigoCase.UI
{
    /// <summary>
    /// Represents the level skip button that moves along with the progress line fill,
    /// displaying the skip cost and handling level purchases.
    /// </summary>
    public class UILevelSkipButton : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI costText;

        public void UpdateSkipCost(Sprite icon, string cost)
        {
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(icon != null);
            }
            
            if (costText != null)
            {
                costText.text = cost;
            }
        }
    }
}
