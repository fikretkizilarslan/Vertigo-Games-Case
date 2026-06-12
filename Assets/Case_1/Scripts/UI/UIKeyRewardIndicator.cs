using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace VertigoCase.UI
{
    /// <summary>
    /// A floating indicator dedicated to pointing to the next upcoming key unclaimed reward
    /// (e.g. premium chest, unique characters, fragments, etc.) on the right edge of the viewport.
    /// </summary>
    public class UIKeyRewardIndicator : MonoBehaviour, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform viewportRect;
        [SerializeField] private UILevelIndicator levelIndicator; // To check if Level Indicator is clamped to the left
        [SerializeField] private Image arrowImage; // Indicator arrow/pointer pointing left or right
        [SerializeField] private Image rewardIconImage; // Image showing the target reward icon

        [Header("Settings")]
        [SerializeField] private float padding = 50f; // Padding from left/right edges of viewport
        [SerializeField] private float snapDuration = 0.5f; // Duration of snap animation
        [SerializeField] private float visibilityThreshold = -50f; // Threshold to determine if reward is off-screen

        [Header("Arrow Customization")]
        [SerializeField] private Vector3 rightRotation = new Vector3(0, 0, 0); // Rotation for pointing right

        [Header("Content Alignment (Automated)")]
        [SerializeField] private RectTransform circleRect; // Container for the reward icon
        [SerializeField] private float contentOffset = 13f; // Offset compensation for sibling/child rotation alignment

        private RectTransform indicatorRect;
        private RectTransform targetRewardNode;
        private Coroutine snapCoroutine;
        private CanvasGroup canvasGroup;
        private bool isCurrentlyVisible = false;

        private void Start()
        {
            indicatorRect = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Fallback assignments to make setup foolproof
            if (levelIndicator == null)
            {
                levelIndicator = FindFirstObjectByType<UILevelIndicator>();
            }
            if (scrollRect == null)
            {
                scrollRect = FindFirstObjectByType<ScrollRect>();
            }
            if (viewportRect == null && scrollRect != null)
            {
                viewportRect = scrollRect.viewport;
            }

            // Resolve child components if not manually assigned
            if (arrowImage == null)
            {
                Transform arrowTrans = transform.Find("Img_Ind_Reward_Arrow");
                if (arrowTrans != null)
                {
                    arrowImage = arrowTrans.GetComponent<Image>();
                }
            }
            if (rewardIconImage == null)
            {
                Transform iconTrans = transform.Find("Img_Ind_Reward_Arrow/RewardIcon");
                if (iconTrans == null) iconTrans = transform.Find("Img_Ind_Reward_Arrow/ChestIcon");
                if (iconTrans == null) iconTrans = transform.Find("Img_Ind_Reward_Arrow/LevelCircle");

                if (iconTrans == null) iconTrans = transform.Find("Img_Ind_Reward_Circle");
                if (iconTrans == null) iconTrans = transform.Find("RewardIcon");
                if (iconTrans == null) iconTrans = transform.Find("ChestIcon");
                if (iconTrans == null) iconTrans = transform.Find("LevelCircle");

                if (iconTrans != null)
                {
                    rewardIconImage = iconTrans.GetComponent<Image>();
                }
            }
            if (circleRect == null && rewardIconImage != null)
            {
                circleRect = rewardIconImage.rectTransform;
            }

            // Start off hidden
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        public void SetTarget(RectTransform targetNode, Sprite rewardIcon)
        {
            targetRewardNode = targetNode;
            if (rewardIconImage != null)
            {
                rewardIconImage.sprite = rewardIcon;
                rewardIconImage.gameObject.SetActive(rewardIcon != null);
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
            if (viewportRect == null || indicatorRect == null) return;

            // Do not show if target node is null
            if (targetRewardNode == null)
            {
                SetVisibility(false);
                return;
            }

            RectTransform parentRect = indicatorRect.parent as RectTransform;
            if (parentRect == null) return;

            // Convert target node world position to parent local coordinates
            Vector3 targetWorldPos = targetRewardNode.position;
            Vector3 parentLocalPos = parentRect.InverseTransformPoint(targetWorldPos);

            // Compute local left and right edges based on pivot
            float leftLocalX = -viewportRect.pivot.x * viewportRect.rect.width;
            float rightLocalX = (1f - viewportRect.pivot.x) * viewportRect.rect.width;

            Vector3 viewportLeftWorld = viewportRect.TransformPoint(new Vector3(leftLocalX, 0, 0));
            Vector3 viewportRightWorld = viewportRect.TransformPoint(new Vector3(rightLocalX, 0, 0));

            float minXInParent = parentRect.InverseTransformPoint(viewportLeftWorld).x + padding;
            float maxXInParent = parentRect.InverseTransformPoint(viewportRightWorld).x - padding;

            // Clamp X coordinate (clamped to edge when off-screen, free when entering screen)
            float clampedX = Mathf.Clamp(parentLocalPos.x, minXInParent, maxXInParent);

            // Position and retain original height
            indicatorRect.anchoredPosition = new Vector2(clampedX, indicatorRect.anchoredPosition.y);

            // Calculate off-screen status relative to viewport
            Vector3 viewportLocalPos = viewportRect.InverseTransformPoint(targetWorldPos);
            float rightThreshold = rightLocalX - visibilityThreshold;

            // 1. As long as Level Indicator is not clamped to the right (OnScreen or OffScreenLeft), the right edge is clear for teaser
            bool isRightEdgeFree = levelIndicator == null || levelIndicator.CurrentState != UILevelIndicator.IndicatorState.OffScreenRight;

            // 2. Teaser appears only when the target key reward is off-screen to the right
            bool isRewardOffScreenRight = viewportLocalPos.x > rightThreshold;

            bool shouldShow = isRightEdgeFree && isRewardOffScreenRight;

            SetVisibility(shouldShow);

            if (shouldShow && arrowImage != null)
            {
                // Assign pointing-right rotation and anchor/pivot properties
                arrowImage.gameObject.SetActive(true);
                arrowImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                arrowImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                arrowImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                arrowImage.rectTransform.anchoredPosition = Vector2.zero;
                arrowImage.rectTransform.localRotation = Quaternion.Euler(rightRotation);

                // Check child hierarchy for rotation compensation
                bool isCircleChildOfArrow = circleRect != null && circleRect.IsChildOf(arrowImage.transform);
                bool isIconChildOfArrow = rewardIconImage != null && rewardIconImage.rectTransform.IsChildOf(arrowImage.transform);

                // Circle/Container orientation adjustment
                if (circleRect != null)
                {
                    if (isCircleChildOfArrow)
                    {
                        circleRect.localRotation = arrowImage.rectTransform.localRotation;
                    }
                    else
                    {
                        circleRect.localRotation = Quaternion.identity;
                        circleRect.anchoredPosition = new Vector2(-contentOffset, circleRect.anchoredPosition.y);
                    }
                }

                // Reward Icon orientation adjustment
                if (rewardIconImage != null)
                {
                    if (isIconChildOfArrow)
                    {
                        rewardIconImage.rectTransform.localRotation = arrowImage.rectTransform.localRotation;
                    }
                    else
                    {
                        rewardIconImage.rectTransform.localRotation = Quaternion.identity;
                        rewardIconImage.rectTransform.anchoredPosition = new Vector2(-contentOffset, rewardIconImage.rectTransform.anchoredPosition.y);
                    }
                }
            }
        }

        private void SetVisibility(bool visible)
        {
            if (isCurrentlyVisible == visible) return;
            isCurrentlyVisible = visible;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.blocksRaycasts = visible;
                canvasGroup.interactable = visible;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (targetRewardNode == null || scrollRect == null || viewportRect == null) return;

            RectTransform contentRect = scrollRect.content;
            float contentWidth = contentRect.rect.width;
            float viewportWidth = viewportRect.rect.width;

            if (contentWidth <= viewportWidth) return;

            // Anchor/pivot-independent scrolling coordinate math
            float targetLocalX = contentRect.InverseTransformPoint(targetRewardNode.position).x;
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
