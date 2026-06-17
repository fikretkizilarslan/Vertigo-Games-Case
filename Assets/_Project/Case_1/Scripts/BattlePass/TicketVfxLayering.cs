using UnityEngine;
using UnityEngine.UI;

namespace BattlePass.UI
{
    /// <summary>
    /// Keeps authored Ps_Ticket hierarchy intact and only normalizes particle sorting.
    /// Does not disable or reorder any VFX objects.
    /// </summary>
    [DisallowMultipleComponent]
    public class TicketVfxLayering : MonoBehaviour
    {
        [SerializeField] private int particleSortingOrder = 4;

        private void Awake()
        {
            Apply();
        }

        public void Apply()
        {
            TicketVfxLayeringUtility.ApplySortingOnly(transform, particleSortingOrder);
        }
    }

    internal static class TicketVfxLayeringUtility
    {
        public static void ApplySortingOnly(Transform ticketRoot, int particleSortingOrder = 4)
        {
            if (ticketRoot == null)
            {
                return;
            }

            ParticleSystem[] particleSystems = ticketRoot.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                if (!particleSystem.gameObject.activeSelf)
                {
                    particleSystem.gameObject.SetActive(true);
                }

                if (!particleSystem.isPlaying)
                {
                    particleSystem.Play(true);
                }
            }

            ParticleSystemRenderer[] renderers = ticketRoot.GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach (ParticleSystemRenderer renderer in renderers)
            {
                renderer.enabled = true;
                renderer.sortingOrder = particleSortingOrder;
            }
        }

        public static void RemoveTicketForegroundCanvas(Transform ticketRoot)
        {
            if (ticketRoot == null)
            {
                return;
            }

            for (int i = 0; i < ticketRoot.childCount; i++)
            {
                Transform child = ticketRoot.GetChild(i);
                if (!child.name.Contains("Ticket") || !child.TryGetComponent(out Image _))
                {
                    continue;
                }

                if (child.TryGetComponent(out Canvas canvas))
                {
                    if (Application.isPlaying)
                    {
                        Object.Destroy(canvas);
                    }
                    else
                    {
                        Object.DestroyImmediate(canvas);
                    }
                }
            }
        }
    }
}
