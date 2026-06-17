using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePass.UI
{
    /// <summary>
    /// Partial class — Premium offer flow: resolve the offer button/burst references,
    /// play the cinematic unlock sequence and complete premium activation.
    /// Extracted from BattlePassManager.cs to reduce file size while keeping
    /// a single MonoBehaviour component on the scene GameObject.
    ///
    /// All fields (offerButton, offerBurstSequence, isPremiumActive, roadCanvasGroup, …)
    /// live in the main BattlePassManager.cs and are accessible here through the
    /// partial class mechanism.
    /// </summary>
    public partial class BattlePassManager
    {
        // ──────────────────────────────────────────────────────────────────────
        // Reference resolution
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves the premium offer button (scene: <c>Btn_Offer_Get</c>) when it has
        /// not been wired in the Inspector and connects its tap to <see cref="ActivatePremium"/>.
        /// </summary>
        /// <remarks>
        /// Prefer wiring the reference directly in the Inspector to avoid the runtime
        /// scene-graph search. The <see cref="GameObject.Find"/> fallback is kept for
        /// backwards compatibility but will log a warning to make it easy to spot.
        /// </remarks>
        private void ResolveOfferButton()
        {
            if (offerButton == null)
            {
                Debug.LogWarning(
                    "[BattlePassManager] Btn_Offer_Get not wired in Inspector — falling back to " +
                    "GameObject.Find. Assign the offerButton field directly to avoid this search.");
                GameObject offerGo = GameObject.Find("Btn_Offer_Get");
                if (offerGo != null)
                {
                    offerButton = offerGo.GetComponent<Button>();
                }
            }

            if (offerButton != null)
            {
                offerButton.onClick.RemoveListener(ActivatePremium);
                offerButton.onClick.AddListener(ActivatePremium);
            }
        }

        /// <summary>
        /// Resolves the <see cref="OfferBurstSequence"/> component when it was not
        /// wired in the Inspector. Warns if the component cannot be found.
        /// </summary>
        private void ResolveOfferBurst()
        {
            if (offerBurstSequence == null)
            {
                offerBurstSequence = FindFirstObjectByType<OfferBurstSequence>(FindObjectsInactive.Include);
            }

            if (offerBurstSequence == null)
            {
                Debug.LogWarning(
                    "[BattlePassManager] OfferBurstSequence not found in scene. " +
                    "Assign Grp_OfferBurst or premium unlock will skip the lock burst.");
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Activation flow
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Unlocks the premium track on the first <c>Btn_Offer_Get</c> tap, then refreshes every
        /// node so premium rewards become claimable. Subsequent taps give a click-feedback pulse.
        /// </summary>
        public void ActivatePremium()
        {
            if (isPremiumActive)
            {
                if (offerButton != null)
                {
                    PlayClickFeedback(offerButton.transform);
                }

                return;
            }

            if (offerBurstSequence != null && !offerBurstSequence.IsPlaying)
            {
                if (offerButton != null)
                {
                    offerButton.interactable = false;
                }

                offerBurstSequence.Play(CompletePremiumActivation);
                return;
            }

            CompletePremiumActivation();
        }

        private void CompletePremiumActivation()
        {
            if (offerButton != null)
            {
                offerButton.interactable = true;
            }

            isPremiumActive = true;
            StartCoroutine(CompletePremiumActivationDeferred());
        }

        /// <summary>
        /// Two-frame deferred premium refresh that keeps the single-frame batch count flat.
        /// Frame 1: removes lock overlays and updates progress indicators (cheap).
        /// Frame 2: enables glow/pulse only on viewport-visible cards (avoids the +40 batch spike
        ///          that would occur if every card enabled its material simultaneously).
        /// </summary>
        private IEnumerator CompletePremiumActivationDeferred()
        {
            yield return null;

            // Cheap pass first: remove lock overlays immediately without enabling off-screen VFX.
            RefreshNodeCoreStates();
            UpdateProgressLine();
            UpdateTopXpPanel();
            UpdateFloatingIndicatorTarget();

            yield return null;

            // Only viewport cards get glow / pulse (avoids +40 batch spike).
            RefreshVisibleNodeEffects();
        }
    }
}
