using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePass.UI
{
    /// <summary>
    /// Keeps <c>Grp_Gem</c> hidden until the player taps the gem teaser button or claims a Lucky Gem reward.
    /// Reveals the wallet with a short DOTween pop, then the currency fly animator can play collect VFX.
    /// </summary>
    [DisallowMultipleComponent]
    public class GemWalletController : MonoBehaviour
    {
        [Header("Gem Wallet UI")]
        [Tooltip("Scene: Grp_Gem — hidden until revealed.")]
        [SerializeField] private GameObject grpGem;
        [Tooltip("Always-visible teaser the player taps to open the gem wallet (e.g. Btn_Gem_Reveal). Auto-resolved when empty.")]
        [SerializeField] private Button gemRevealButton;

        [Header("Reveal Animation")]
        [Min(0.05f)] [SerializeField] private float revealDuration = 0.32f;
        [SerializeField] private float revealFromScale = 0.82f;

        [Header("Hide Animation")]
        [Tooltip("Pause after collect finishes before the wallet dismisses.")]
        [Min(0f)] [SerializeField] private float hideDelayAfterCollect = 0.12f;
        [Min(0.05f)] [SerializeField] private float hideDuration = 0.3f;
        [Tooltip("Brief scale-up before shrinking away (1.08 = +8%).")]
        [SerializeField] private float hidePunchScale = 1.08f;
        [SerializeField] private bool useUnscaledTime = true;

        private CanvasGroup grpGemCanvasGroup;
        private RectTransform grpGemRect;
        private Tween walletTween;
        private bool isRevealed;

        public bool IsRevealed => isRevealed;

        private void Awake()
        {
            ResolveReferences();
            HideImmediate();
            BindRevealButton();
        }

        /// <summary>Hides the gem wallet instantly (used on startup).</summary>
        public void HideImmediate()
        {
            ResolveReferences();
            KillWalletTween();

            if (grpGem == null)
            {
                return;
            }

            isRevealed = false;
            grpGem.SetActive(false);

            if (grpGemCanvasGroup != null)
            {
                grpGemCanvasGroup.alpha = 0f;
            }

            if (grpGemRect != null)
            {
                grpGemRect.localScale = Vector3.one * revealFromScale;
            }
        }

        /// <summary>
        /// Reveals <c>Grp_Gem</c> with a fade + scale pop. Safe to call when already visible.
        /// </summary>
        public void RevealWallet(Action onComplete = null)
        {
            ResolveReferences();

            if (grpGem == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (isRevealed)
            {
                onComplete?.Invoke();
                return;
            }

            KillWalletTween();
            grpGem.SetActive(true);
            isRevealed = true;

            EnsureCanvasGroup();

            grpGemCanvasGroup.alpha = 0f;
            if (grpGemRect != null)
            {
                grpGemRect.localScale = Vector3.one * revealFromScale;
            }

            Sequence sequence = DOTween.Sequence().SetUpdate(useUnscaledTime);
            sequence.Append(grpGemCanvasGroup.DOFade(1f, revealDuration).SetEase(Ease.OutQuad));

            if (grpGemRect != null)
            {
                sequence.Join(grpGemRect.DOScale(1f, revealDuration).SetEase(Ease.OutBack));
            }

            sequence.OnComplete(() =>
            {
                walletTween = null;
                onComplete?.Invoke();
            });

            walletTween = sequence;
        }

        /// <summary>
        /// Dismisses the gem wallet after collect: slight punch up, shrink + fade out, then hides.
        /// </summary>
        public void HideWalletAnimated(Action onComplete = null)
        {
            ResolveReferences();

            if (!isRevealed || grpGem == null)
            {
                onComplete?.Invoke();
                return;
            }

            KillWalletTween();
            EnsureCanvasGroup();

            Sequence sequence = DOTween.Sequence().SetUpdate(useUnscaledTime);

            if (hideDelayAfterCollect > 0f)
            {
                sequence.AppendInterval(hideDelayAfterCollect);
            }

            float punchDuration = hideDuration * 0.35f;
            float shrinkDuration = hideDuration * 0.65f;
            Vector3 punchScale = Vector3.one * hidePunchScale;

            if (grpGemRect != null)
            {
                grpGemRect.localScale = Vector3.one;
                sequence.Append(grpGemRect.DOScale(punchScale, punchDuration).SetEase(Ease.OutQuad));

                Sequence shrinkSequence = DOTween.Sequence();
                shrinkSequence.Join(grpGemRect.DOScale(Vector3.one * revealFromScale, shrinkDuration).SetEase(Ease.InBack));
                shrinkSequence.Join(grpGemCanvasGroup.DOFade(0f, shrinkDuration).SetEase(Ease.InQuad));
                sequence.Append(shrinkSequence);
            }
            else
            {
                sequence.Append(grpGemCanvasGroup.DOFade(0f, hideDuration).SetEase(Ease.InQuad));
            }

            sequence.OnComplete(() =>
            {
                walletTween = null;
                HideImmediate();
                onComplete?.Invoke();
            });

            walletTween = sequence;
        }

        private void EnsureCanvasGroup()
        {
            if (grpGem == null)
            {
                return;
            }

            if (grpGemRect == null)
            {
                grpGemRect = grpGem.transform as RectTransform;
            }

            if (grpGemCanvasGroup == null)
            {
                grpGemCanvasGroup = grpGem.GetComponent<CanvasGroup>();
                if (grpGemCanvasGroup == null)
                {
                    grpGemCanvasGroup = grpGem.AddComponent<CanvasGroup>();
                }
            }
        }

        private void ResolveReferences()
        {
            if (grpGem == null)
            {
                GameObject found = GameObject.Find("Grp_Gem");
                if (found != null)
                {
                    grpGem = found;
                }
            }

            if (grpGem != null)
            {
                grpGemRect = grpGem.transform as RectTransform;
                grpGemCanvasGroup = grpGem.GetComponent<CanvasGroup>();
            }

            if (gemRevealButton == null)
            {
                GameObject teaser = GameObject.Find("Btn_Gem_Reveal");
                if (teaser != null)
                {
                    gemRevealButton = teaser.GetComponent<Button>();
                }
            }
        }

        private void BindRevealButton()
        {
            if (gemRevealButton == null)
            {
                return;
            }

            gemRevealButton.onClick.RemoveListener(HandleRevealClicked);
            gemRevealButton.onClick.AddListener(HandleRevealClicked);
        }

        private void HandleRevealClicked()
        {
            RevealWallet();
        }

        private void KillWalletTween()
        {
            if (walletTween != null && walletTween.IsActive())
            {
                walletTween.Kill();
            }

            walletTween = null;
        }

        private void OnDestroy()
        {
            KillWalletTween();

            if (gemRevealButton != null)
            {
                gemRevealButton.onClick.RemoveListener(HandleRevealClicked);
            }
        }
    }
}
