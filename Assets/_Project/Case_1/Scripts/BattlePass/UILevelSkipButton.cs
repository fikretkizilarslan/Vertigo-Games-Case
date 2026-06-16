using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VertigoCase.UI
{
    /// <summary>
    /// Interactive "skip" badge that rides along the progress line (scene object
    /// <c>Btn_XP_Skip</c>), sitting in the gap right after the current level node.
    /// It shows the diamond cost required to skip the level and raises a callback
    /// when tapped so the owning <see cref="BattlePassManager"/> can charge the
    /// player's diamonds and advance one level.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UILevelSkipButton : MonoBehaviour
    {
        [Header("Cost Display")]
        [Tooltip("Diamond icon rendered on the skip badge.")]
        [SerializeField] private Image iconImage;

        [Tooltip("Diamond cost label rendered on the skip badge (e.g. \"20\").")]
        [SerializeField] private TextMeshProUGUI costText;

        [Header("Interaction")]
        [Tooltip("Button used to detect skip taps. Auto-resolved from this GameObject when left empty.")]
        [SerializeField] private Button skipButton;

        [Header("Click VFX")]
        [Tooltip("Particle burst parented under this badge (scene: Ps_Xp_Button). Auto-resolved by name when left empty.")]
        [SerializeField] private GameObject clickVfxRoot;

        private Action _onSkipRequested;
        private Coroutine clickVfxRoutine;

        /// <summary>
        /// Connects the tap callback invoked when the player taps the skip badge.
        /// Safe to call more than once; the previous handler is replaced.
        /// </summary>
        /// <param name="onSkipRequested">Callback raised on tap (typically the manager's skip handler).</param>
        public void Bind(Action onSkipRequested)
        {
            _onSkipRequested = onSkipRequested;

            if (skipButton == null)
            {
                skipButton = GetComponent<Button>();
            }

            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(HandleSkipClicked);
                skipButton.onClick.AddListener(HandleSkipClicked);
            }
        }

        /// <summary>
        /// Refreshes the diamond icon and the cost label displayed on the badge.
        /// </summary>
        /// <param name="icon">Currency icon sprite (hidden when null).</param>
        /// <param name="costLabel">Formatted cost text to display.</param>
        public void SetCost(Sprite icon, string costLabel)
        {
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(icon != null);
            }

            if (costText != null)
            {
                costText.text = costLabel;
            }
        }

        private void Awake()
        {
            ResolveClickVfxRoot();
        }

        /// <summary>
        /// Plays the embedded click VFX centered on this badge, rendered behind the icon and cost label.
        /// </summary>
        public void PlayClickVfx()
        {
            ResolveClickVfxRoot();
            if (clickVfxRoot == null)
            {
                return;
            }

            if (clickVfxRoutine != null)
            {
                StopCoroutine(clickVfxRoutine);
            }

            clickVfxRoutine = StartCoroutine(PlayClickVfxRoutine());
        }

        private void ResolveClickVfxRoot()
        {
            if (clickVfxRoot != null)
            {
                return;
            }

            Transform vfxTransform = transform.Find("Ps_Xp_Button");
            if (vfxTransform != null)
            {
                clickVfxRoot = vfxTransform.gameObject;
            }
        }

        private IEnumerator PlayClickVfxRoutine()
        {
            Transform vfxTransform = clickVfxRoot.transform;
            vfxTransform.SetAsFirstSibling();
            vfxTransform.localPosition = Vector3.zero;

            RectTransform vfxRect = vfxTransform as RectTransform;
            if (vfxRect != null)
            {
                vfxRect.anchorMin = new Vector2(0.5f, 0.5f);
                vfxRect.anchorMax = new Vector2(0.5f, 0.5f);
                vfxRect.pivot = new Vector2(0.5f, 0.5f);
                vfxRect.anchoredPosition = Vector2.zero;
            }

            clickVfxRoot.SetActive(true);

            ParticleSystem[] particleSystems = clickVfxRoot.GetComponentsInChildren<ParticleSystem>(true);
            float duration = 0.5f;

            foreach (ParticleSystem ps in particleSystems)
            {
                if (ps == null)
                {
                    continue;
                }

                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(true);

                ParticleSystem.MainModule main = ps.main;
                float psDuration = main.duration;
                if (!main.loop)
                {
                    psDuration += Mathf.Max(main.startLifetime.constant, main.startLifetime.constantMax);
                }

                duration = Mathf.Max(duration, psDuration);
            }

            yield return new WaitForSecondsRealtime(duration);

            clickVfxRoot.SetActive(false);
            clickVfxRoutine = null;
        }

        private void HandleSkipClicked()
        {
            _onSkipRequested?.Invoke();
        }

        private void OnDestroy()
        {
            if (clickVfxRoutine != null)
            {
                StopCoroutine(clickVfxRoutine);
                clickVfxRoutine = null;
            }

            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(HandleSkipClicked);
            }
        }
    }
}
