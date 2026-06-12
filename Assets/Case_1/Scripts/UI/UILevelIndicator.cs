using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace VertigoCase.UI
{
    public class UILevelIndicator : MonoBehaviour, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform viewportRect;
        [SerializeField] private Image arrowImage; // Indicator arrow/pointer pointing left or right
        [SerializeField] private TextMeshProUGUI indicatorText; // Text showing level number inside the bubble

        [Header("Settings")]
        [SerializeField] private float padding = 50f; // Padding from left/right edges of viewport
        [SerializeField] private float snapDuration = 0.5f; // Duration of snap animation
        [SerializeField] private float visibilityThreshold = -50f; // Threshold to determine when the indicator clamps and shows

        [Header("Arrow Customization")]
        [SerializeField] private Vector3 leftRotation = new Vector3(0, 180, 0); // Rotation for pointing left
        [SerializeField] private Vector3 rightRotation = new Vector3(0, 0, 0); // Rotation for pointing right
        [Header("Content Alignment (Automated)")]
        [SerializeField] private RectTransform circleRect; // Container for the level circle
        [SerializeField] private float contentOffset = 13f; // Offset compensation for sibling/child rotation alignment

        public enum IndicatorState
        {
            None,
            OnScreen,
            OffScreenLeft,
            OffScreenRight
        }

        private RectTransform indicatorRect;
        private RectTransform targetLevelNode;
        private int currentLevelNumber;
        private IndicatorState currentState = IndicatorState.None;

        public IndicatorState CurrentState => currentState;
        private Coroutine snapCoroutine;
        private CanvasGroup canvasGroup;

        private void Start()
        {
            indicatorRect = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void SetTarget(RectTransform targetNode, int level)
        {
            targetLevelNode = targetNode;
            currentLevelNumber = level;
            if (indicatorText != null)
            {
                indicatorText.text = level.ToString();
            }
        }

        private void Update()
        {
            // Cancel snap animation if user drags/touches screen during snap
            bool isPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Pointer.current != null)
            {
                isPressed = UnityEngine.InputSystem.Pointer.current.press.isPressed;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            isPressed = Input.GetMouseButton(0);
#endif
            if (snapCoroutine != null && isPressed)
            {
                StopCoroutine(snapCoroutine);
                snapCoroutine = null;
            }
        }

        private void LateUpdate()
        {
            UpdateIndicatorPosition();
        }

        private void UpdateIndicatorPosition()
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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (targetLevelNode == null || scrollRect == null || viewportRect == null) return;

            RectTransform contentRect = scrollRect.content;
            float contentWidth = contentRect.rect.width;
            float viewportWidth = viewportRect.rect.width;

            if (contentWidth <= viewportWidth) return;

            // Compute scroll target coordinates safely
            float targetLocalX = contentRect.InverseTransformPoint(targetLevelNode.position).x;
            float distanceFromLeft = targetLocalX + (contentRect.pivot.x * contentWidth);
            float desiredScrollPos = distanceFromLeft - (viewportWidth / 2f);
            
            float targetNormalized = desiredScrollPos / (contentWidth - viewportWidth);
            float clampedTargetNormalized = Mathf.Clamp01(targetNormalized);

            if (snapCoroutine != null)
            {
                StopCoroutine(snapCoroutine);
            }
            snapCoroutine = StartCoroutine(SnapToPositionRoutine(clampedTargetNormalized));
        }

        private System.Collections.IEnumerator SnapToPositionRoutine(float targetX)
        {
            float startX = scrollRect.horizontalNormalizedPosition;
            float elapsed = 0f;

            while (elapsed < snapDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / snapDuration;
                
                // Ease Out Cubic snap animation
                float easeT = 1f - Mathf.Pow(1f - t, 3f);
                
                scrollRect.horizontalNormalizedPosition = Mathf.Lerp(startX, targetX, easeT);
                yield return null;
            }

            scrollRect.horizontalNormalizedPosition = targetX;
            snapCoroutine = null;
        }
    }
}
