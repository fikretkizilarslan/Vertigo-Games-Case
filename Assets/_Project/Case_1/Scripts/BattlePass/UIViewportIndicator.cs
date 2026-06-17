using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BattlePass.UI
{
    /// <summary>
    /// Shared behaviour for the floating viewport indicators that point to an off-screen target on
    /// the Battle Pass road. Owns the transparent hit area, click/tap-to-scroll, the eased snap
    /// animation and pointer-press cancellation. Concrete indicators decide which node they track
    /// and how they clamp/orient themselves every frame.
    /// </summary>
    public abstract class UIViewportIndicator : MonoBehaviour, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] protected ScrollRect scrollRect;
        [SerializeField] protected RectTransform viewportRect;
        [SerializeField] protected Image arrowImage; // Indicator arrow/pointer

        [Header("Settings")]
        [SerializeField] protected float padding = 50f; // Padding from left/right edges of viewport
        [SerializeField] protected float snapDuration = 0.5f; // Duration of snap animation
        [SerializeField] protected float visibilityThreshold = -50f; // Threshold that decides off-screen state

        [Header("Arrow Customization")]
        [SerializeField] protected Vector3 rightRotation = new Vector3(0, 0, 0); // Rotation for pointing right

        [Header("Content Alignment (Automated)")]
        [SerializeField] protected RectTransform circleRect; // Container for the circle/icon
        [SerializeField] protected float contentOffset = 13f; // Offset compensation for rotation alignment

        protected RectTransform indicatorRect;
        protected CanvasGroup canvasGroup;
        private Coroutine snapCoroutine;

        /// <summary>The road node this indicator currently tracks and scrolls to.</summary>
        protected abstract RectTransform TargetNode { get; }

        protected virtual void Start()
        {
            indicatorRect = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            EnsureClickableArea();
            OnIndicatorStart();
        }

        /// <summary>Hook for subclasses to run extra setup right after the base Start finishes.</summary>
        protected virtual void OnIndicatorStart() { }

        protected virtual void Update()
        {
            // Cancel snap animation if the user drags/touches the screen during the snap.
            if (snapCoroutine != null && IsPointerPressed())
            {
                StopCoroutine(snapCoroutine);
                snapCoroutine = null;
            }
        }

        protected static bool IsPointerPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Pointer.current != null)
            {
                return UnityEngine.InputSystem.Pointer.current.press.isPressed;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButton(0);
#endif
            return false;
        }

        /// <summary>
        /// Guarantees the whole indicator root is a single clickable surface: adds a transparent
        /// raycast image, wires the optional root Button to the scroll action and disables child
        /// buttons so taps never get swallowed by inner graphics.
        /// </summary>
        protected void EnsureClickableArea()
        {
            Image hitArea = GetComponent<Image>();
            if (hitArea == null)
            {
                hitArea = gameObject.AddComponent<Image>();
                hitArea.color = new Color(0f, 0f, 0f, 0f);
            }

            hitArea.raycastTarget = true;

            Button rootButton = GetComponent<Button>();
            if (rootButton != null)
            {
                rootButton.targetGraphic = hitArea;
                rootButton.onClick.RemoveListener(ScrollToTarget);
                rootButton.onClick.AddListener(ScrollToTarget);
            }

            Button[] childButtons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < childButtons.Length; i++)
            {
                if (childButtons[i].gameObject != gameObject)
                {
                    childButtons[i].interactable = false;
                }
            }
        }

        protected virtual void LateUpdate()
        {
            UpdateIndicatorPosition();
        }

        /// <summary>Per-frame clamp/orientation logic, implemented by each concrete indicator.</summary>
        protected abstract void UpdateIndicatorPosition();

        public void OnPointerClick(PointerEventData eventData)
        {
            ScrollToTarget();
        }

        protected void ScrollToTarget()
        {
            RectTransform target = TargetNode;
            if (target == null || scrollRect == null || viewportRect == null) return;

            RectTransform contentRect = scrollRect.content;
            float contentWidth = contentRect.rect.width;
            float viewportWidth = viewportRect.rect.width;

            if (contentWidth <= viewportWidth) return;

            // Anchor/pivot-independent scrolling coordinate math.
            float targetLocalX = contentRect.InverseTransformPoint(target.position).x;
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

        private IEnumerator SnapToPositionRoutine(float targetX)
        {
            float startX = scrollRect.horizontalNormalizedPosition;
            float elapsed = 0f;

            while (elapsed < snapDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / snapDuration;

                // Ease Out Cubic snap animation.
                float easeT = 1f - Mathf.Pow(1f - t, 3f);

                scrollRect.horizontalNormalizedPosition = Mathf.Lerp(startX, targetX, easeT);
                yield return null;
            }

            scrollRect.horizontalNormalizedPosition = targetX;
            snapCoroutine = null;
        }
    }
}
