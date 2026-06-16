using UnityEngine;
using TMPro;

namespace VertigoCase.UI
{
    /// <summary>
    /// Floating indicator that tracks the current level node. Points left or right when the node is
    /// off-screen and stays hidden while the node is visible inside the viewport.
    /// </summary>
    public class UILevelIndicator : UIViewportIndicator
    {
        [Header("Level Indicator")]
        [SerializeField] private TextMeshProUGUI indicatorText; // Text showing the level number inside the bubble
        [SerializeField] private Vector3 leftRotation = new Vector3(0, 180, 0); // Rotation for pointing left

        public enum IndicatorState
        {
            None,
            OnScreen,
            OffScreenLeft,
            OffScreenRight
        }

        private RectTransform targetLevelNode;
        private int currentLevelNumber;
        private IndicatorState currentState = IndicatorState.None;

        public IndicatorState CurrentState => currentState;

        protected override RectTransform TargetNode => targetLevelNode;

        public void SetTarget(RectTransform targetNode, int level)
        {
            if (targetLevelNode == targetNode && currentLevelNumber == level) return;

            targetLevelNode = targetNode;
            currentLevelNumber = level;
            if (indicatorText != null)
            {
                indicatorText.text = level.ToString();
            }
        }

        protected override void UpdateIndicatorPosition()
        {
            if (targetLevelNode == null || viewportRect == null || indicatorRect == null) return;

            RectTransform parentRect = indicatorRect.parent as RectTransform;
            if (parentRect == null) return;

            // Convert target node world position to parent local coordinates
            Vector3 targetWorldPos = targetLevelNode.position;
            Vector3 parentLocalPos = parentRect.InverseTransformPoint(targetWorldPos);

            // Compute local left and right edges based on pivot
            float leftLocalX = -viewportRect.pivot.x * viewportRect.rect.width;
            float rightLocalX = (1f - viewportRect.pivot.x) * viewportRect.rect.width;

            Vector3 viewportLeftWorld = viewportRect.TransformPoint(new Vector3(leftLocalX, 0, 0));
            Vector3 viewportRightWorld = viewportRect.TransformPoint(new Vector3(rightLocalX, 0, 0));

            float minXInParent = parentRect.InverseTransformPoint(viewportLeftWorld).x + padding;
            float maxXInParent = parentRect.InverseTransformPoint(viewportRightWorld).x - padding;

            // Clamp X coordinate to keep indicator on screen
            float clampedX = Mathf.Clamp(parentLocalPos.x, minXInParent, maxXInParent);

            // Position and retain original height
            indicatorRect.anchoredPosition = new Vector2(clampedX, indicatorRect.anchoredPosition.y);

            // Calculate off-screen status relative to viewport
            Vector3 viewportLocalPos = viewportRect.InverseTransformPoint(targetWorldPos);
            float leftThreshold = leftLocalX + visibilityThreshold;
            float rightThreshold = rightLocalX - visibilityThreshold;

            // State Caching to prevent redundant hierarchy updates
            IndicatorState newState;
            if (viewportLocalPos.x < leftThreshold)
            {
                newState = IndicatorState.OffScreenLeft;
            }
            else if (viewportLocalPos.x > rightThreshold)
            {
                newState = IndicatorState.OffScreenRight;
            }
            else
            {
                newState = IndicatorState.OnScreen;
            }

            if (newState != currentState)
            {
                currentState = newState;

                // Manage visibility and click blocking via CanvasGroup
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = (currentState == IndicatorState.OnScreen) ? 0f : 1f;
                    canvasGroup.blocksRaycasts = (currentState != IndicatorState.OnScreen);
                    canvasGroup.interactable = (currentState != IndicatorState.OnScreen);
                }

                if (arrowImage != null)
                {
                    switch (currentState)
                    {
                        case IndicatorState.OnScreen:
                            arrowImage.gameObject.SetActive(false);
                            break;
                        case IndicatorState.OffScreenLeft:
                            arrowImage.gameObject.SetActive(true);
                            arrowImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                            arrowImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                            arrowImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                            arrowImage.rectTransform.anchoredPosition = Vector2.zero;
                            arrowImage.rectTransform.localRotation = Quaternion.Euler(leftRotation);
                            break;
                        case IndicatorState.OffScreenRight:
                            arrowImage.gameObject.SetActive(true);
                            arrowImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                            arrowImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                            arrowImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                            arrowImage.rectTransform.anchoredPosition = Vector2.zero;
                            arrowImage.rectTransform.localRotation = Quaternion.Euler(rightRotation);
                            break;
                    }

                    // Detect whether children are nested under Arrow or siblings
                    bool isCircleChildOfArrow = circleRect != null && circleRect.IsChildOf(arrowImage.transform);
                    bool isTextChildOfArrow = indicatorText != null && indicatorText.rectTransform.IsChildOf(arrowImage.transform);

                    // 1. Circle/Container Alignment
                    if (circleRect != null)
                    {
                        if (isCircleChildOfArrow)
                        {
                            circleRect.localRotation = arrowImage.rectTransform.localRotation;
                        }
                        else
                        {
                            circleRect.localRotation = Quaternion.identity;
                            float currentOffset = 0f;
                            if (currentState == IndicatorState.OffScreenLeft) currentOffset = contentOffset;
                            else if (currentState == IndicatorState.OffScreenRight) currentOffset = -contentOffset;

                            circleRect.anchoredPosition = new Vector2(currentOffset, circleRect.anchoredPosition.y);
                        }
                    }

                    // 2. Text Alignment
                    if (indicatorText != null)
                    {
                        if (isTextChildOfArrow)
                        {
                            indicatorText.rectTransform.localRotation = arrowImage.rectTransform.localRotation;
                        }
                        else
                        {
                            indicatorText.rectTransform.localRotation = Quaternion.identity;
                            float currentOffset = 0f;
                            if (currentState == IndicatorState.OffScreenLeft) currentOffset = contentOffset;
                            else if (currentState == IndicatorState.OffScreenRight) currentOffset = -contentOffset;

                            indicatorText.rectTransform.anchoredPosition = new Vector2(currentOffset, indicatorText.rectTransform.anchoredPosition.y);
                        }
                    }
                }
            }
        }
    }
}
