using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePass.UI
{
    /// <summary>
    /// Builds the editable reward-reveal hierarchy under Grp_OfferBurst so timing, sprites,
    /// layout and VFX can be tuned directly in the scene.
    /// </summary>
    public static class OfferBurstSceneSetup
    {
        private const string RootName = "Grp_OfferBurst";
        private const string RewardGroupName = "Grp_RewardReveal";
        private const string TicketGroupName = "Grp_Ticket";
        private const string TicketVfxName = "Grp_Ticket_VFX";
        private const string TicketGlowName = "Img_Ticket_Glow";
        private const string TicketImageName = "Img_Ticket";
        private const string PremiumTextName = "Txt_PremiumStatus";
        private const string ClaimButtonName = "Btn_Claim";
        private const string ClaimLabelName = "Txt_Claim";

        private const string TicketGlowSpritePath = "Assets/_Project/Case_1/Sprite/Used/Icons/ui_icon_battlepass_glow.png";
        private const string TicketSpritePath = "Assets/_Project/Case_1/Sprite/Used/Icons/ui_icon_battlepass_shadow.png";
        private const string ClaimButtonSpritePath = "Assets/_Project/Case_1/Sprite/Used/Buttons/ui_button_blue.png";
        private const string TicketVfxPrefabPath = "Assets/_Project/Case_1/Prefabs/UI/VFX/Ps_Ticket.prefab";
        private const string FontAssetPath = "Assets/Share/Font/Gotham Bold SDF.asset";

        [MenuItem("BattlePass/Setup Offer Burst Reward UI")]
        public static void SetupRewardUi()
        {
            OfferBurstSequence sequence = Object.FindFirstObjectByType<OfferBurstSequence>(FindObjectsInactive.Include);
            if (sequence == null)
            {
                EditorUtility.DisplayDialog("Offer Burst Setup", $"Could not find {RootName} with OfferBurstSequence in the active scene.", "OK");
                return;
            }

            Transform root = sequence.transform;
            Transform rewardGroup = EnsureRewardGroup(root);
            Transform ticketGroup = EnsureTicketGroup(rewardGroup);
            EnsureTicketVfx(ticketGroup);
            Image glow = EnsureImage(ticketGroup, TicketGlowName, LoadSprite(TicketGlowSpritePath), new Vector2(320f, 320f), Vector2.zero, 0);
            Image ticket = EnsureImage(ticketGroup, TicketImageName, LoadSprite(TicketSpritePath), new Vector2(220f, 220f), Vector2.zero, 1);
            TMP_Text premiumText = EnsurePremiumText(rewardGroup);
            Button claimButton = EnsureClaimButton(rewardGroup);

            WireSequence(sequence, rewardGroup, ticketGroup, glow, ticket, premiumText, claimButton);

            EditorUtility.SetDirty(sequence);
            EditorUtility.SetDirty(root.gameObject);
            Debug.Log("[OfferBurstSceneSetup] Grp_OfferBurst reward UI is ready. Expand Grp_RewardReveal in the Hierarchy to tweak layout, sprites and VFX.");
        }

        private static Transform EnsureRewardGroup(Transform root)
        {
            Transform existing = root.Find(RewardGroupName);
            if (existing != null)
            {
                return existing;
            }

            GameObject go = new GameObject(RewardGroupName, typeof(RectTransform), typeof(CanvasGroup));
            Undo.RegisterCreatedObjectUndo(go, "Create Offer Burst Reward Group");
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(root, false);
            Stretch(rect);
            rect.SetAsLastSibling();

            CanvasGroup group = go.GetComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            group.ignoreParentGroups = true;
            return rect;
        }

        private static Transform EnsureTicketGroup(Transform rewardGroup)
        {
            Transform existing = rewardGroup.Find(TicketGroupName);
            if (existing != null)
            {
                return existing;
            }

            GameObject go = new GameObject(TicketGroupName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create Offer Burst Ticket Group");
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(rewardGroup, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 40f);
            rect.sizeDelta = new Vector2(360f, 360f);
            rect.localRotation = Quaternion.Euler(0f, 0f, -3f);
            rect.SetAsFirstSibling();
            return rect;
        }

        private static void EnsureTicketVfx(Transform ticketGroup)
        {
            Transform existing = ticketGroup.Find(TicketVfxName);
            if (existing != null)
            {
                return;
            }

            GameObject slot = new GameObject(TicketVfxName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(slot, "Create Offer Burst Ticket VFX Slot");
            RectTransform rect = slot.GetComponent<RectTransform>();
            rect.SetParent(ticketGroup, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(320f, 320f);
            rect.SetAsFirstSibling();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TicketVfxPrefabPath);
            if (prefab != null)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab, slot.transform) as GameObject;
                if (instance != null)
                {
                    Undo.RegisterCreatedObjectUndo(instance, "Instantiate Ticket VFX");
                    RectTransform vfxRect = instance.GetComponent<RectTransform>();
                    if (vfxRect != null)
                    {
                        vfxRect.anchorMin = new Vector2(0.5f, 0.5f);
                        vfxRect.anchorMax = new Vector2(0.5f, 0.5f);
                        vfxRect.pivot = new Vector2(0.5f, 0.5f);
                        vfxRect.anchoredPosition = Vector2.zero;
                        vfxRect.sizeDelta = new Vector2(320f, 320f);
                        vfxRect.localScale = Vector3.one;
                    }

                    instance.SetActive(false);
                }
            }
            else
            {
                Debug.LogWarning($"[OfferBurstSceneSetup] Missing prefab at {TicketVfxPrefabPath}. Drag Ps_Ticket under {TicketVfxName} manually.");
            }
        }

        private static Image EnsureImage(Transform parent, string objectName, Sprite sprite, Vector2 size, Vector2 anchoredPosition, int siblingIndex)
        {
            Transform existing = parent.Find(objectName);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(go, $"Create {objectName}");
                go.transform.SetParent(parent, false);
            }

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            rect.SetSiblingIndex(siblingIndex);

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;
            return image;
        }

        private static TMP_Text EnsurePremiumText(Transform rewardGroup)
        {
            Transform existing = rewardGroup.Find(PremiumTextName);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(PremiumTextName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(go, "Create Premium Status Text");
                go.transform.SetParent(rewardGroup, false);
            }

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -150f);
            rect.sizeDelta = new Vector2(640f, 72f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            TMP_Text text = go.GetComponent<TextMeshProUGUI>();
            text.font = LoadFont();
            text.fontSize = 34f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 0.95f, 0.45f, 1f);
            text.text = "Premium Active";
            text.enableAutoSizing = true;
            text.fontSizeMin = 24f;
            text.fontSizeMax = 40f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.alignment = TextAlignmentOptions.Center;
            return text;
        }

        private static Button EnsureClaimButton(Transform rewardGroup)
        {
            Transform existing = rewardGroup.Find(ClaimButtonName);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(ClaimButtonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                Undo.RegisterCreatedObjectUndo(go, "Create Claim Button");
                go.transform.SetParent(rewardGroup, false);
            }

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 120f);
            rect.sizeDelta = new Vector2(320f, 110f);

            Image image = go.GetComponent<Image>();
            image.sprite = LoadSprite(ClaimButtonSpritePath);
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.color = Color.white;

            Button button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;

            Transform labelTransform = go.transform.Find(ClaimLabelName);
            GameObject labelGo;
            if (labelTransform != null)
            {
                labelGo = labelTransform.gameObject;
            }
            else
            {
                labelGo = new GameObject(ClaimLabelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(labelGo, "Create Claim Label");
                labelGo.transform.SetParent(go.transform, false);
            }

            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            Stretch(labelRect);

            TMP_Text label = labelGo.GetComponent<TextMeshProUGUI>();
            label.font = LoadFont();
            label.fontSize = 40f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.text = "CLAIM";
            label.raycastTarget = false;

            return button;
        }

        private static void WireSequence(
            OfferBurstSequence sequence,
            Transform rewardGroup,
            Transform ticketGroup,
            Image glow,
            Image ticket,
            TMP_Text premiumText,
            Button claimButton)
        {
            SerializedObject so = new SerializedObject(sequence);
            so.FindProperty("rewardRevealGroup").objectReferenceValue = rewardGroup.GetComponent<CanvasGroup>();
            so.FindProperty("ticketRoot").objectReferenceValue = ticketGroup as RectTransform;
            so.FindProperty("ticketGlowImage").objectReferenceValue = glow;
            so.FindProperty("ticketImage").objectReferenceValue = ticket;
            so.FindProperty("ticketVfxRoot").objectReferenceValue = ticketGroup.Find(TicketVfxName)?.gameObject;
            so.FindProperty("premiumStatusText").objectReferenceValue = premiumText;
            so.FindProperty("claimButton").objectReferenceValue = claimButton;
            so.FindProperty("claimButtonLabel").objectReferenceValue = claimButton.GetComponentInChildren<TMP_Text>(true);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static TMP_FontAsset LoadFont()
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        }
    }
}
