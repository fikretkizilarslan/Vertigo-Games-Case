using UnityEngine;
using UnityEngine.UI;

namespace VertigoCase.UI
{
    /// <summary>
    /// Scrolls the UV Rect of a RawImage component every frame
    /// to create a moving/scrolling background pattern effect.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class ScrollingRawImage : MonoBehaviour
    {
        [Header("Scroll Settings")]
        [SerializeField] private Vector2 scrollSpeed = new Vector2(-0.03f, 0.015f);

        private RawImage rawImage;

        private void Awake()
        {
            rawImage = GetComponent<RawImage>();
        }

        private void Update()
        {
            // Offset the UV Rect x and y coordinates relative to time
            Rect uvRect = rawImage.uvRect;
            uvRect.x += scrollSpeed.x * Time.deltaTime;
            uvRect.y += scrollSpeed.y * Time.deltaTime;
            rawImage.uvRect = uvRect;
        }
    }
}
