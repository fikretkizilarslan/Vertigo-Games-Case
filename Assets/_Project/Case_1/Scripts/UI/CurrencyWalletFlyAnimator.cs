using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VertigoCase.UI
{
    public enum WalletCurrencyType
    {
        Gold,
        Diamond
    }

    /// <summary>
    /// DOTween-driven gold / diamond collect and spend animation.
    /// Spawns up to <see cref="maxFlyIcons"/> icons at screen center (gain) or at the wallet icon (spend),
    /// flies them to the header icon, punches the icon on each hit, and steps the counter one unit at a time.
    /// </summary>
    [DisallowMultipleComponent]
    public class CurrencyWalletFlyAnimator : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private RectTransform goldIconTarget;
        [SerializeField] private RectTransform diamondIconTarget;
        [SerializeField] private TextMeshProUGUI goldCountText;
        [SerializeField] private TextMeshProUGUI diamondCountText;

        [Header("Fly Sprites")]
        [Tooltip("Sprite used for flying gold icons. Falls back to the header gold icon sprite when empty.")]
        [SerializeField] private Sprite goldFlySprite;
        [Tooltip("Sprite used for flying diamond icons. Falls back to the header diamond icon sprite when empty.")]
        [SerializeField] private Sprite diamondFlySprite;

        [Header("Fly Layer")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private RectTransform flyLayer;

        [Header("Tuning")]
        [SerializeField] private int maxFlyIcons = 10;
        [SerializeField] private float flyDuration = 0.55f;
        [SerializeField] private float spawnSpreadRadius = 90f;
        [SerializeField] private float spawnStagger = 0.045f;
        [SerializeField] private Vector2 flyIconSize = new Vector2(72f, 72f);
        [Tooltip("How much the header icon grows on each hit (0.1 = +10%).")]
        [SerializeField] private float iconPunchStrength = 0.1f;
        [SerializeField] private float iconPunchDuration = 0.14f;
        [SerializeField] private float arriveShrinkDuration = 0.1f;
        [SerializeField] private Ease flyEase = Ease.InOutCubic;
        [SerializeField] private bool useUnscaledTime = true;

        private readonly List<Image> flyIconPool = new List<Image>();
        private int activeFlyTweens;
        private int displayedGold;
        private int displayedDiamond;
        private Vector3 goldIconBaseScale = Vector3.one;
        private Vector3 diamondIconBaseScale = Vector3.one;
        private Tween goldPunchTween;
        private Tween diamondPunchTween;
        private Tween counterStepTween;
        private bool referencesResolved;

        private void Awake()
        {
            ResolveReferences();
        }

        /// <summary>Syncs the visible counters without playing an animation.</summary>
        public void SyncDisplay(int gold, int diamond)
        {
            ResolveReferences();
            RepairOversizedIcons();
            displayedGold = gold;
            displayedDiamond = diamond;
            WriteGoldText(displayedGold);
            WriteDiamondText(displayedDiamond);
        }

        /// <summary>
        /// Animates a wallet change from <paramref name="fromValue"/> to <paramref name="toValue"/>.
        /// </summary>
        public void PlayChange(WalletCurrencyType currency, int fromValue, int toValue)
        {
            if (fromValue == toValue)
            {
                return;
            }

            ResolveReferences();
            RepairOversizedIcons();
            StopActiveFlies();

            bool isGain = toValue > fromValue;

            // Spending only steps the counter down — no reverse fly from header icon to center.
            if (!isGain)
            {
                PlayCounterStepOnly(currency, fromValue, toValue);
                return;
            }

            int delta = Mathf.Abs(toValue - fromValue);
            int flyCount = Mathf.Min(maxFlyIcons, Mathf.Max(1, delta));
            int[] increments = BuildIncrements(delta, flyCount);
            int accumulatedDelta = 0;

            SetCounter(currency, fromValue);

            RectTransform iconTarget = GetIconTarget(currency);
            RectTransform layer = EnsureFlyLayer();
            if (iconTarget == null || layer == null)
            {
                SetCounter(currency, toValue);
                return;
            }

            Vector2 centerLocal = GetScreenCenterLocal(layer);
            Vector2 targetLocal = WorldPointToLayerLocal(iconTarget.position, layer);

            activeFlyTweens = flyCount;
            int completedFlies = 0;

            for (int i = 0; i < flyCount; i++)
            {
                int increment = increments[i];
                float delay = i * spawnStagger;
                Image flyIcon = RentFlyIcon(currency);
                RectTransform flyRect = flyIcon.rectTransform;

                Vector2 startPos = isGain
                    ? centerLocal + RandomInsideCircle(spawnSpreadRadius)
                    : targetLocal + RandomInsideCircle(spawnSpreadRadius * 0.35f);
                Vector2 endPos = isGain ? targetLocal : centerLocal;

                flyRect.anchoredPosition = startPos;
                flyRect.localScale = Vector3.zero;
                flyIcon.color = Color.white;
                flyIcon.gameObject.SetActive(true);

                Sequence flySequence = DOTween.Sequence().SetUpdate(useUnscaledTime);
                if (delay > 0f)
                {
                    flySequence.AppendInterval(delay);
                }

                flySequence.Append(flyRect.DOScale(1f, 0.12f).SetEase(Ease.OutBack));
                flySequence.Append(
                    flyRect.DOAnchorPos(endPos, flyDuration + Random.Range(-0.04f, 0.04f))
                        .SetEase(flyEase));

                flySequence.OnComplete(() =>
                {
                    PunchIcon(currency);
                    accumulatedDelta += increment;
                    SetCounter(currency, fromValue + accumulatedDelta);

                    flyRect.DOScale(0f, arriveShrinkDuration)
                        .SetEase(Ease.InBack)
                        .SetUpdate(useUnscaledTime)
                        .OnComplete(() => ReturnFlyIcon(flyIcon));

                    completedFlies++;
                    if (completedFlies >= flyCount)
                    {
                        SetCounter(currency, toValue);
                        ResetIconScale(currency);
                        activeFlyTweens = 0;
                    }
                });
            }
        }

        /// <summary>
        /// Steps the wallet label down one integer at a time without spawning fly icons.
        /// </summary>
        private void PlayCounterStepOnly(WalletCurrencyType currency, int fromValue, int toValue)
        {
            int delta = fromValue - toValue;
            int stepCount = Mathf.Min(maxFlyIcons, Mathf.Max(1, delta));
            float duration = Mathf.Max(0.12f, stepCount * spawnStagger);

            SetCounter(currency, fromValue);

            if (counterStepTween != null && counterStepTween.IsActive())
            {
                counterStepTween.Kill();
            }

            int displayValue = fromValue;
            counterStepTween = DOTween.To(
                    () => (float)displayValue,
                    value =>
                    {
                        displayValue = Mathf.RoundToInt(value);
                        SetCounter(currency, displayValue);
                    },
                    toValue,
                    duration)
                .SetEase(Ease.Linear)
                .SetUpdate(useUnscaledTime)
                .OnComplete(() =>
                {
                    SetCounter(currency, toValue);
                    counterStepTween = null;
                });
        }

        private void ResolveReferences()
        {
            if (referencesResolved)
            {
                return;
            }

            if (goldCountText == null)
            {
                goldCountText = GameObject.Find("Txt_Count_Gold")?.GetComponent<TextMeshProUGUI>();
            }

            if (diamondCountText == null)
            {
                diamondCountText = GameObject.Find("Txt_Count_Diamond")?.GetComponent<TextMeshProUGUI>();
            }

            if (goldIconTarget == null)
            {
                goldIconTarget = FindIconUnderGroup("Grp_Gold", "Img_Gold");
            }

            if (diamondIconTarget == null)
            {
                diamondIconTarget = FindIconUnderGroup("Grp_Diamond", "Img_Diamond");
            }

            if (goldFlySprite == null && goldIconTarget != null)
            {
                goldFlySprite = goldIconTarget.GetComponent<Image>()?.sprite;
            }

            if (diamondFlySprite == null && diamondIconTarget != null)
            {
                diamondFlySprite = diamondIconTarget.GetComponent<Image>()?.sprite;
            }

            if (rootCanvas == null)
            {
                rootCanvas = GetComponentInParent<Canvas>();
                if (rootCanvas == null && goldIconTarget != null)
                {
                    rootCanvas = goldIconTarget.GetComponentInParent<Canvas>();
                }
            }

            if (goldIconTarget != null)
            {
                CaptureBaseScale(goldIconTarget, ref goldIconBaseScale, !referencesResolved);
            }

            if (diamondIconTarget != null)
            {
                CaptureBaseScale(diamondIconTarget, ref diamondIconBaseScale, !referencesResolved);
            }

            referencesResolved = true;
        }

        private static void CaptureBaseScale(RectTransform icon, ref Vector3 baseScale, bool allowCapture)
        {
            Vector3 current = icon.localScale;
            if (allowCapture || current.x <= 1.05f)
            {
                baseScale = current.x <= 1.05f ? current : Vector3.one;
            }

            icon.localScale = baseScale;
        }

        private void RepairOversizedIcons()
        {
            if (goldIconTarget != null && goldIconTarget.localScale.x > goldIconBaseScale.x * 1.02f)
            {
                goldIconTarget.localScale = goldIconBaseScale;
            }

            if (diamondIconTarget != null && diamondIconTarget.localScale.x > diamondIconBaseScale.x * 1.02f)
            {
                diamondIconTarget.localScale = diamondIconBaseScale;
            }
        }

        private static RectTransform FindIconUnderGroup(string groupName, string iconName)
        {
            Transform group = GameObject.Find(groupName)?.transform;
            if (group == null)
            {
                return GameObject.Find(iconName)?.GetComponent<RectTransform>();
            }

            Transform icon = group.Find(iconName);
            if (icon == null)
            {
                Image[] images = group.GetComponentsInChildren<Image>(true);
                foreach (Image image in images)
                {
                    if (image != null && image.name == iconName)
                    {
                        return image.rectTransform;
                    }
                }
            }

            return icon != null ? icon.GetComponent<RectTransform>() : null;
        }

        private RectTransform EnsureFlyLayer()
        {
            if (flyLayer != null)
            {
                return flyLayer;
            }

            if (rootCanvas == null)
            {
                return null;
            }

            GameObject layerObject = new GameObject("CurrencyFlyLayer", typeof(RectTransform), typeof(CanvasGroup));
            layerObject.transform.SetParent(rootCanvas.transform, false);

            RectTransform layerRect = layerObject.GetComponent<RectTransform>();
            layerRect.anchorMin = Vector2.zero;
            layerRect.anchorMax = Vector2.one;
            layerRect.offsetMin = Vector2.zero;
            layerRect.offsetMax = Vector2.zero;
            layerRect.SetAsLastSibling();

            CanvasGroup canvasGroup = layerObject.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            flyLayer = layerRect;
            return flyLayer;
        }

        private RectTransform GetIconTarget(WalletCurrencyType currency)
        {
            return currency == WalletCurrencyType.Gold ? goldIconTarget : diamondIconTarget;
        }

        private Sprite GetFlySprite(WalletCurrencyType currency)
        {
            return currency == WalletCurrencyType.Gold ? goldFlySprite : diamondFlySprite;
        }

        private void SetCounter(WalletCurrencyType currency, int value)
        {
            if (currency == WalletCurrencyType.Gold)
            {
                displayedGold = value;
                WriteGoldText(displayedGold);
            }
            else
            {
                displayedDiamond = value;
                WriteDiamondText(displayedDiamond);
            }
        }

        private void WriteGoldText(int value)
        {
            if (goldCountText != null)
            {
                goldCountText.text = value.ToString("N0");
            }
        }

        private void WriteDiamondText(int value)
        {
            if (diamondCountText != null)
            {
                diamondCountText.text = value.ToString();
            }
        }

        private void PunchIcon(WalletCurrencyType currency)
        {
            RectTransform icon = GetIconTarget(currency);
            if (icon == null)
            {
                return;
            }

            Vector3 baseScale = currency == WalletCurrencyType.Gold ? goldIconBaseScale : diamondIconBaseScale;
            Tween runningTween = currency == WalletCurrencyType.Gold ? goldPunchTween : diamondPunchTween;

            if (runningTween != null && runningTween.IsActive())
            {
                runningTween.Kill();
            }

            icon.localScale = baseScale;
            Tween pulseTween = CreateIconPulseTween(icon, baseScale);

            if (currency == WalletCurrencyType.Gold)
            {
                goldPunchTween = pulseTween;
            }
            else
            {
                diamondPunchTween = pulseTween;
            }
        }

        private Tween CreateIconPulseTween(RectTransform icon, Vector3 baseScale)
        {
            float growDuration = iconPunchDuration * 0.45f;
            float shrinkDuration = iconPunchDuration * 0.55f;
            Vector3 peakScale = baseScale * (1f + iconPunchStrength);

            return DOTween.Sequence()
                .SetUpdate(useUnscaledTime)
                .Append(icon.DOScale(peakScale, growDuration).SetEase(Ease.OutQuad))
                .Append(icon.DOScale(baseScale, shrinkDuration).SetEase(Ease.InOutSine))
                .OnKill(() =>
                {
                    if (icon != null)
                    {
                        icon.localScale = baseScale;
                    }
                })
                .OnComplete(() =>
                {
                    if (icon != null)
                    {
                        icon.localScale = baseScale;
                    }
                });
        }

        private void ResetIconScale(WalletCurrencyType currency)
        {
            if (currency == WalletCurrencyType.Gold)
            {
                if (goldPunchTween != null && goldPunchTween.IsActive())
                {
                    goldPunchTween.Kill();
                }

                goldPunchTween = null;

                if (goldIconTarget != null)
                {
                    goldIconTarget.localScale = goldIconBaseScale;
                }
            }
            else
            {
                if (diamondPunchTween != null && diamondPunchTween.IsActive())
                {
                    diamondPunchTween.Kill();
                }

                diamondPunchTween = null;

                if (diamondIconTarget != null)
                {
                    diamondIconTarget.localScale = diamondIconBaseScale;
                }
            }
        }

        private Image RentFlyIcon(WalletCurrencyType currency)
        {
            Sprite sprite = GetFlySprite(currency);
            foreach (Image pooled in flyIconPool)
            {
                if (pooled != null && !pooled.gameObject.activeSelf)
                {
                    pooled.sprite = sprite;
                    return pooled;
                }
            }

            RectTransform layer = EnsureFlyLayer();
            GameObject iconObject = new GameObject("FlyCurrencyIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(layer, false);

            RectTransform rect = iconObject.GetComponent<RectTransform>();
            rect.sizeDelta = flyIconSize;

            Image image = iconObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.sprite = sprite;
            image.preserveAspect = true;

            flyIconPool.Add(image);
            return image;
        }

        private void ReturnFlyIcon(Image flyIcon)
        {
            if (flyIcon == null)
            {
                return;
            }

            DOTween.Kill(flyIcon.rectTransform);
            flyIcon.gameObject.SetActive(false);
        }

        private void StopActiveFlies()
        {
            if (counterStepTween != null && counterStepTween.IsActive())
            {
                counterStepTween.Kill();
            }

            counterStepTween = null;

            ResetIconScale(WalletCurrencyType.Gold);
            ResetIconScale(WalletCurrencyType.Diamond);

            foreach (Image pooled in flyIconPool)
            {
                if (pooled == null)
                {
                    continue;
                }

                DOTween.Kill(pooled.rectTransform);
                DOTween.Kill(pooled);
                pooled.gameObject.SetActive(false);
            }

            activeFlyTweens = 0;
        }

        private Vector2 GetScreenCenterLocal(RectTransform layer)
        {
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Camera eventCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screenCenter, eventCamera, out Vector2 localPoint);
            return localPoint;
        }

        private Vector2 WorldPointToLayerLocal(Vector3 worldPoint, RectTransform layer)
        {
            Camera eventCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, worldPoint);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screenPoint, eventCamera, out Vector2 localPoint);
            return localPoint;
        }

        private static Vector2 RandomInsideCircle(float radius)
        {
            return Random.insideUnitCircle * radius;
        }

        private static int[] BuildIncrements(int total, int count)
        {
            int[] increments = new int[count];
            int baseAmount = total / count;
            int remainder = total % count;

            for (int i = 0; i < count; i++)
            {
                increments[i] = baseAmount + (i < remainder ? 1 : 0);
            }

            return increments;
        }

        private void OnDestroy()
        {
            StopActiveFlies();
        }
    }
}
