using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CTCC
{
    public sealed partial class HospitalUI
    {
        private Image heldBadgeBacking;
        private Text heldBadgeText;
        private Image bagEchoCard;
        private Text bagEchoLabel;
        private Coroutine bagEchoRoutine;
        private Image cinematicSkipBacking;
        private Text cinematicSkipText;
        private GameObject cinematicBarTop, cinematicBarBottom;

        // 4 Quick-Slots compact HUD bar (Proposal V1.1 §3.1 / BP-02)
        private GameObject quickSlotBar;
        private readonly Image[] quickSlotBg = new Image[InventoryService.MaxQuickSlots];
        private readonly Image[] quickSlotBorder = new Image[InventoryService.MaxQuickSlots];
        private readonly Text[] quickSlotText = new Text[InventoryService.MaxQuickSlots];

        private void EnsureWalkthroughHudElements()
        {
            if (hud == null) return;
            if (heldBadgeBacking == null)
            {
                var box = Fixed("HeldItemBadge", hud, new Vector2(1, 0), new Vector2(-496, 90), new Vector2(456, 46));
                heldBadgeBacking = Graphic(box, new Color(0.040f, 0.064f, 0.056f, 0.86f), false);
                Graphic(Rect("HeldAccent", box, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(3, 0)), Accent, false);
                heldBadgeText = FillText("HeldLabel", Rect("HeldInset", box, Vector2.zero, Vector2.one, new Vector2(14, 0), new Vector2(-10, 0)),
                    18, Ink, TextAnchor.MiddleLeft, true);
                heldBadgeBacking.gameObject.SetActive(false);
            }
            if (quickSlotBar == null)
            {
                var bar = Fixed("QuickSlotBar", hud, new Vector2(1, 0), new Vector2(-856, 28), new Vector2(816, 48));
                quickSlotBar = bar.gameObject;
                const float slotW = 198f;
                const float gap = 8f;
                for (int i = 0; i < InventoryService.MaxQuickSlots; i++)
                {
                    var slot = new GameObject($"QuickSlot_{i + 1}", typeof(RectTransform)).GetComponent<RectTransform>();
                    slot.SetParent(bar, false);
                    slot.anchorMin = slot.anchorMax = new Vector2(0f, 0.5f);
                    slot.pivot = new Vector2(0f, 0.5f);
                    slot.anchoredPosition = new Vector2(i * (slotW + gap), 0f);
                    slot.sizeDelta = new Vector2(slotW, 46f);
                    quickSlotBg[i] = Graphic(slot, new Color(0.038f, 0.058f, 0.052f, 0.82f), false);
                    quickSlotBorder[i] = Graphic(Rect("Rule", slot, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 2)), Frame, false);
                    quickSlotText[i] = FillText("Label", Rect("Inset", slot, Vector2.zero, Vector2.one, new Vector2(8, 0), new Vector2(-8, 0)),
                        16, Secondary, TextAnchor.MiddleLeft, true);
                    slot.gameObject.SetActive(false);
                }
                quickSlotBar.SetActive(false);
            }
            if (cinematicSkipBacking == null && canvas != null)
            {
                var top = Rect("CinematicBarTop", canvas, new Vector2(0, 0.935f), Vector2.one);
                Graphic(top, new Color(0.01f, 0.014f, 0.012f, 0.86f), false);
                cinematicBarTop = top.gameObject;
                cinematicBarTop.SetActive(false);

                var bot = Rect("CinematicBarBottom", canvas, Vector2.zero, new Vector2(1, 0.065f));
                Graphic(bot, new Color(0.01f, 0.014f, 0.012f, 0.86f), false);
                cinematicBarBottom = bot.gameObject;
                cinematicBarBottom.SetActive(false);

                var skip = Fixed("CinematicSkip", canvas, new Vector2(1, 1), new Vector2(-376, -34), new Vector2(340, 44));
                cinematicSkipBacking = Graphic(skip, new Color(0.042f, 0.065f, 0.058f, 0.82f), false);
                cinematicSkipText = FillText("SkipLabel", skip, 17, Accent, TextAnchor.MiddleCenter, true);
                cinematicSkipText.text = "[Space / Esc]  Bỏ qua cảnh";
                cinematicSkipBacking.gameObject.SetActive(false);
            }
        }

        public void SetHeldItemBadge(string itemLabel)
        {
            EnsureWalkthroughHudElements();
            if (heldBadgeBacking == null) return;
            bool has = !string.IsNullOrEmpty(itemLabel);
            heldBadgeBacking.gameObject.SetActive(has);
            if (has)
            {
                heldBadgeText.fontSize = Scaled(18);
                heldBadgeText.text = $"Trên tay: {itemLabel}  ·  [H] Cất";
            }
        }

        public void SetQuickSlots(IReadOnlyList<(string label, bool equipped)> slots)
        {
            EnsureWalkthroughHudElements();
            if (quickSlotBar == null) return;
            int count = slots != null ? Mathf.Min(slots.Count, InventoryService.MaxQuickSlots) : 0;
            quickSlotBar.SetActive(count > 0);
            for (int i = 0; i < InventoryService.MaxQuickSlots; i++)
            {
                bool active = i < count;
                quickSlotBg[i].gameObject.SetActive(active);
                if (!active) continue;
                var (label, equipped) = slots[i];
                quickSlotBg[i].color = equipped
                    ? new Color(0.12f, 0.21f, 0.16f, 0.94f)
                    : new Color(0.038f, 0.058f, 0.052f, 0.82f);
                quickSlotBorder[i].color = equipped ? Accent : Frame;
                quickSlotText[i].color = equipped ? Ink : Secondary;
                quickSlotText[i].fontSize = Scaled(15);
                quickSlotText[i].text = $"[{i + 1}] {label}";
            }
        }

        public void SetCinematicOverlay(bool active, bool reducedMotion)
        {
            EnsureWalkthroughHudElements();
            if (cinematicBarTop != null) cinematicBarTop.SetActive(active && !reducedMotion);
            if (cinematicBarBottom != null) cinematicBarBottom.SetActive(active && !reducedMotion);
            if (cinematicSkipBacking != null)
            {
                cinematicSkipBacking.gameObject.SetActive(active);
                if (active) cinematicSkipText.fontSize = Scaled(17);
            }
            if (hud != null && !IsModal)
                hud.gameObject.SetActive(!active);
        }

        /// <summary>
        /// Renders the 3-compartment Investigation Backpack docked alongside the live 3D canvas backpack stage:
        /// [0] Dụng cụ & đồ vật, [1] Giấy tờ & vật chứng (with optional conditional P-BAG08_ECHO shimmer), [2] Pocket Note.
        /// Reuses the pooled uGUI notebook container for zero GC churn and full keyboard/130% accessibility.
        /// </summary>
        internal void ShowBackpack(
            int compartment,
            Action<int> selectCompartment,
            Action close,
            bool alternate,
            bool animate,
            IReadOnlyList<NotebookEntry> items,
            string emptyCopy,
            string detailTitle,
            string detailBody,
            Texture detailImage,
            ActionChoice[] detailActions,
            bool showBag08Echo = false,
            Action onEchoDissolve = null,
            bool show3DStageOnLeft = true,
            Action onCloseBag = null)
        {
            bool samePage = notebookOpen && notebookBookmark == (100 + compartment) && !notebookWide && !notebookClipboard;
            previousNotebookFocus = samePage && UnityEngine.EventSystems.EventSystem.current != null
                ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject?.GetComponent<Button>() : null;
            previousNotebookLabel = previousNotebookFocus?.GetComponentInChildren<Text>()?.text;
            notebookBookmark = 100 + compartment;
            notebookWide = false;
            notebookClipboard = false;
            if (!notebookOpen) ClearModalPanel();
            if (notebookRoot == null) BuildNotebook();
            StopPageTurn();
            StopBagEcho();
            heldSlip.gameObject.SetActive(false);
            if (notebookEntry.isFocused) notebookEntry.DeactivateInputField();
            notebookOpen = true;
            notebookAlternate = alternate;
            notebookDefaultFocus = null;
            dropSlip = null;
            changeWriting = null;
            writingOnPage = readerOnPage = false;
            usedControls = usedEntries = usedLabels = 0;
            notebookFontScale = scale;
            readingScroll = null;
            notebookIndexScroll.StopMovement();
            if (!samePage) notebookIndexScroll.content.anchoredPosition = Vector2.zero;
            notebookReader.StopMovement();
            notebookReader.content.anchoredPosition = Vector2.zero;
            illustration.gameObject.SetActive(false);
            HidePaperViews();
            HideFormInk();
            dismiss = close;
            onCloseExtra = () =>
            {
                StopBagEcho();
                onCloseBag?.Invoke();
            };
            focus.Clear();
            selected = -1;

            if (show3DStageOnLeft)
            {
                // Leave the left 40% completely open to showcase the live lit 3D weathered canvas backpack!
                panel.anchorMin = new Vector2(.405f, .068f);
                panel.anchorMax = new Vector2(.968f, .888f);
                modalBackdrop.color = new Color(.012f, .018f, .015f, .24f);
                modalBackdrop.raycastTarget = false;
            }
            else
            {
                panel.anchorMin = new Vector2(.055f, .072f);
                panel.anchorMax = new Vector2(.945f, .888f);
                modalBackdrop.color = new Color(.014f, .019f, .016f, .70f);
                modalBackdrop.raycastTarget = true;
            }
            panelBg.color = new Color(.14f, .165f, .145f);

            const float split = .39f;
            Place(notebookLeft, 0, 0, split, 1, new Vector2(14, 15), new Vector2(-4, -15));
            Place(notebookRight, split, 0, 1, 1, new Vector2(4, 15), new Vector2(-14, -15));
            Place(binding, split, 0, split, 1, new Vector2(-3, 14), new Vector2(3, -15));

            string[] tabs = { "Dụng cụ & đồ vật", "Giấy tờ & vật chứng", "Pocket Note" };
            int tabFont = show3DStageOnLeft ? 16 : 18;
            for (int i = 0; i < tabs.Length; i++)
            {
                int idx = i;
                float x = .010f + i * .254f;
                var tab = NotebookButton(notebookRoot, x, 1, x + .246f, 1.070f,
                    new ActionChoice(tabs[i], () => selectCompartment(idx)),
                    i == compartment ? PaperLight : new Color(.68f, .70f, .60f), true, size: tabFont);
                if (i == compartment) notebookDefaultFocus = tab.Button;
            }
            NotebookButton(notebookRoot, .776f, 1, .990f, 1.070f,
                new ActionChoice("Khép ba lô · Esc", close), Paper, true, size: tabFont);

            string headingText = compartment switch
            {
                0 => "BA LÔ / DỤNG CỤ",
                1 => "BA LÔ / GIẤY TỜ",
                _ => "BA LÔ / POCKET NOTE"
            };
            NotebookIndex(items, emptyCopy, headingText);
            NotebookPage(detailTitle, detailBody, detailImage, detailActions ?? Array.Empty<ActionChoice>());

            if (showBag08Echo && compartment == 1)
                ShowBag08EchoVisual(onEchoDissolve);

            notebookRoot.gameObject.SetActive(true);
            bool wasModal = IsModal;
            modal.gameObject.SetActive(true);
            modalOpenedFrame = Time.frameCount;
            hud.gameObject.SetActive(false);
            if (!wasModal) OpenChanged?.Invoke();
            if (animate) pageTurn = StartCoroutine(TurnNotebookPage());
            FinishNotebook();
        }

        private void ShowBag08EchoVisual(Action onDissolve)
        {
            if (bagEchoCard == null)
            {
                var r = Rect("P_BAG08_ECHO_Ghost", notebookLeft, new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.28f));
                bagEchoCard = Graphic(r, new Color(0.84f, 0.87f, 0.83f, 0.34f), false);
                Graphic(Rect("EchoCornerRule", r, new Vector2(0.04f, 0.14f), new Vector2(0.96f, 0.16f)),
                    new Color(0.24f, 0.34f, 0.36f, 0.35f), false);
                bagEchoLabel = MakeText(Rect("EchoText", r, Vector2.zero, Vector2.one, new Vector2(12, 6), new Vector2(-12, -6)),
                    15, new Color(0.16f, 0.24f, 0.25f, 0.82f), TextAnchor.MiddleLeft, false);
                bagEchoLabel.text = "AS-05B · CHƯA BÀN GIAO\n[Ô chữ ký người nhận trống · LOC03 / B]\n(Mép thẻ thoáng hiện trên lớp vải túi rồi mờ dần…)";
            }
            bagEchoCard.transform.SetAsLastSibling();
            bagEchoCard.gameObject.SetActive(true);
            bagEchoLabel.fontSize = Scaled(15);
            bagEchoRoutine = StartCoroutine(FadeBagEcho(onDissolve));
        }

        private System.Collections.IEnumerator FadeBagEcho(Action onDissolve)
        {
            float hold = 3.2f;
            for (float t = 0; t < hold; t += Time.unscaledDeltaTime)
            {
                if (bagEchoCard == null || !bagEchoCard.gameObject.activeSelf) yield break;
                float pulse = 0.28f + 0.08f * Mathf.Sin(t * 2.4f);
                bagEchoCard.color = new Color(0.84f, 0.87f, 0.83f, pulse);
                yield return null;
            }
            for (float t = 0; t < 1.8f; t += Time.unscaledDeltaTime)
            {
                if (bagEchoCard == null || !bagEchoCard.gameObject.activeSelf) yield break;
                float a = Mathf.Lerp(0.32f, 0f, t / 1.8f);
                bagEchoCard.color = new Color(0.84f, 0.87f, 0.83f, a);
                bagEchoLabel.color = new Color(0.16f, 0.24f, 0.25f, a * 2.4f);
                yield return null;
            }
            if (bagEchoCard != null) bagEchoCard.gameObject.SetActive(false);
            bagEchoRoutine = null;
            onDissolve?.Invoke();
        }

        private void StopBagEcho()
        {
            if (bagEchoRoutine != null) { StopCoroutine(bagEchoRoutine); bagEchoRoutine = null; }
            if (bagEchoCard != null) bagEchoCard.gameObject.SetActive(false);
        }
    }
}
