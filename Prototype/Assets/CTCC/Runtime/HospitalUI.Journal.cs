using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CTCC
{
    internal readonly struct NotebookEntry
    {
        public readonly ActionChoice Choice;
        public readonly string DragId;
        public NotebookEntry(string title, Action select, string dragId = null)
        {
            Choice = new ActionChoice(title, select);
            DragId = dragId;
        }
    }

    public sealed partial class HospitalUI
    {
        private static readonly Color BookCover = new(0.25f, 0.17f, 0.115f);
        private static readonly Color Paper = new(0.88f, 0.83f, 0.70f);
        private static readonly Color PaperLight = new(0.94f, 0.89f, 0.77f);
        private static readonly Color BookInk = new(0.16f, 0.19f, 0.17f);
        private static readonly Color BookRule = new(0.41f, 0.35f, 0.25f, 0.40f);
        private static readonly string[] Bookmarks = { "Hiện trường", "Tài liệu", "Suy luận", "Dòng thời gian", "Quyết định" };
        private RectTransform notebookLeft, notebookRight, heldSlip;
        private ScrollRect notebookIndexScroll;
        private InputField notebookEntry;
        private bool notebookOpen, notebookAlternate;
        private RectTransform[] documentSlots;
        private Action<string, int> dropDocument;
        private Coroutine pageTurn;

        internal void ShowNotebook(int bookmark, Action<int> select, Action close, bool alternate, bool animate)
        {
            ResetNotebookInput();
            for (int i = panel.childCount - 1; i >= 0; i--)
            {
                var child = panel.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
            notebookOpen = true;
            notebookAlternate = alternate;
            dismiss = close;
            onCloseExtra = null;
            focus.Clear();
            selected = -1;

            panel.anchorMin = new Vector2(0.077f, 0.081f);
            panel.anchorMax = new Vector2(0.923f, 0.884f);
            panelBg.color = BookCover;
            modalBackdrop.color = new Color(0.018f, 0.020f, 0.016f, 0.71f);
            modalBackdrop.raycastTarget = true;
            Graphic(Rect("Paper edges", panel, Vector2.zero, Vector2.one, new Vector2(12, 10), new Vector2(-12, -13)),
                new Color(0.66f, 0.58f, 0.44f), false);
            Graphic(Rect("Page block", panel, Vector2.zero, Vector2.one, new Vector2(14, 13), new Vector2(-14, -14)), Paper, false);
            notebookLeft = Rect("Left page", panel, Vector2.zero, new Vector2(0.35f, 1), new Vector2(19, 19), new Vector2(-4, -18));
            notebookRight = Rect("Right page", panel, new Vector2(0.35f, 0), Vector2.one, new Vector2(4, 19), new Vector2(-19, -18));
            Graphic(notebookLeft, Paper, false);
            Graphic(notebookRight, PaperLight, false);
            Graphic(Rect("Binding", panel, new Vector2(0.35f, 0), new Vector2(0.35f, 1), new Vector2(-5, 16), new Vector2(5, -16)),
                new Color(0.34f, 0.27f, 0.18f, 0.34f), false);
            Graphic(Rect("Binding edge", panel, new Vector2(0.35f, 0), new Vector2(0.35f, 1), new Vector2(5, 16), new Vector2(12, -16)),
                new Color(0.35f, 0.28f, 0.20f, 0.12f), false);

            float width = 0.145f;
            for (int i = 0; i < Bookmarks.Length; i++)
            {
                int index = i;
                float x = 0.026f + i * (width + 0.0075f);
                var tab = Rect("Bookmark", panel, new Vector2(x, 1), new Vector2(x + width, 1.056f));
                NotebookButton(tab, new ActionChoice(Bookmarks[i], () => select(index)), i == bookmark ? PaperLight :
                    new Color(0.69f, 0.68f, 0.55f), centered: true);
            }
            NotebookButton(Rect("Close notebook", panel, new Vector2(0.84f, 1), new Vector2(0.988f, 1.056f)),
                new ActionChoice("Khép sổ  ·  Esc", close), Paper, centered: true);
            JournalText(notebookLeft, "Sổ trực  /  An Sinh", 24, .902f, .987f, true);
            PageRule(notebookLeft, .898f);

            modal.gameObject.SetActive(true);
            modalOpenedFrame = Time.frameCount;
            hud.gameObject.SetActive(false);
            OpenChanged?.Invoke();
            if (animate) pageTurn = StartCoroutine(TurnNotebookPage());
        }

        internal void NotebookIndex(IReadOnlyList<NotebookEntry> entries, string empty)
        {
            var viewport = Rect("Notebook index", notebookLeft, new Vector2(0, 0.055f), new Vector2(1, 0.875f),
                new Vector2(22, 0), new Vector2(-23, 0));
            var list = MakeNotebookScroll(viewport, out notebookIndexScroll);
            if (entries.Count == 0)
            {
                var text = FillText("Empty index", list, 22, BookInk, TextAnchor.UpperLeft);
                text.text = empty;
                text.supportRichText = false;
                return;
            }
            foreach (var item in entries)
            {
                var row = Rect("Notebook entry", list, Vector2.zero, Vector2.one);
                var size = row.gameObject.AddComponent<LayoutElement>();
                size.minHeight = scale > 1.1f ? 77f : 59f;
                NotebookButton(row, item.Choice, PaperLight);
                if (item.DragId != null && dropDocument != null)
                {
                    var drag = row.gameObject.AddComponent<NotebookDrag>();
                    drag.Begin = point =>
                    {
                        if (ModalInputBlocked) return;
                        heldSlip = Fixed("Paper in hand", canvas, Vector2.zero, Vector2.zero, new Vector2(330, 66));
                        Graphic(heldSlip, PaperLight, false);
                        FillText("Dragged title", heldSlip, 19, BookInk, TextAnchor.MiddleCenter, true).text = item.Choice.Label;
                        DragNotebookSlip(point);
                    };
                    drag.Move = DragNotebookSlip;
                    drag.End = point =>
                    {
                        if (heldSlip == null) return;
                        Destroy(heldSlip.gameObject);
                        heldSlip = null;
                        if (ModalInputBlocked || documentSlots == null) return;
                        for (int i = 0; i < documentSlots.Length; i++)
                            if (RectTransformUtility.RectangleContainsScreenPoint(documentSlots[i], point))
                            {
                                dropDocument?.Invoke(item.DragId, i);
                                break;
                            }
                    };
                }
            }
        }

        internal void NotebookPage(string title, string body, params ActionChoice[] actions)
        {
            JournalText(notebookRight, title, 31, .895f, .987f, true);
            PageRule(notebookRight, .888f);
            float foot = Mathf.Min(0.39f, 0.07f + actions.Length * 0.064f);
            var scroll = Rect("Ruled page", notebookRight, new Vector2(0f, foot), new Vector2(1, .86f),
                new Vector2(32, 2), new Vector2(-32, -4));
            var article = FillText("Notebook writing", MakeNotebookScroll(scroll, out readingScroll), 23, BookInk, TextAnchor.UpperLeft);
            article.supportRichText = false;
            article.text = body;
            article.lineSpacing = 1.22f;
            NotebookActions(actions, foot);
        }

        internal void NotebookEditor(string title, string sources, string value, Action<string> changed, params ActionChoice[] actions)
        {
            JournalText(notebookRight, title, 30, .895f, .987f, true);
            PageRule(notebookRight, .888f);
            JournalText(notebookRight, sources, 21, .723f, .865f);
            var box = Rect("Notebook input", notebookRight, new Vector2(0, .395f), new Vector2(1, .703f),
                new Vector2(31, 0), new Vector2(-31, 0));
            var backing = Graphic(box, new Color(0.83f, 0.80f, 0.66f), true);
            var field = box.gameObject.AddComponent<InputField>();
            var text = MakeText(Rect("Writing", box, Vector2.zero, Vector2.one, new Vector2(14, 14), new Vector2(-14, -14)),
                23, BookInk, TextAnchor.UpperLeft, false);
            text.supportRichText = false;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            var placeholder = FillText("Writing prompt", box, 21, new Color(0.31f, 0.30f, 0.23f), TextAnchor.MiddleCenter);
            placeholder.text = "Ghi lại điều mình đang nghĩ…";
            field.textComponent = text;
            field.placeholder = placeholder;
            field.targetGraphic = backing;
            field.characterLimit = JournalNote.MaxLength;
            field.lineType = InputField.LineType.MultiLineNewline;
            field.navigation = new Navigation { mode = Navigation.Mode.None };
            field.SetTextWithoutNotify(value ?? "");
            field.onValueChanged.AddListener(changed.Invoke);
            notebookEntry = field;
            NotebookActions(actions, 0.355f);
        }

        internal void FocusNotebookWriting()
        {
            notebookEntry?.Select();
            notebookEntry?.ActivateInputField();
        }

        internal void NotebookCompare(string leftLabel, Texture left, ActionChoice[] leftActions,
            string rightLabel, Texture right, ActionChoice[] rightActions, Action<string, int> drop, ActionChoice back)
        {
            JournalText(notebookRight, "Đối chiếu", 31, .895f, .987f, true);
            PageRule(notebookRight, .888f);
            documentSlots = new RectTransform[2];
            dropDocument = drop;
            AddDocumentSlot(0, leftLabel, left, leftActions);
            AddDocumentSlot(1, rightLabel, right, rightActions);
            NotebookActions(new[] { back }, .114f);
        }

        private void AddDocumentSlot(int side, string label, Texture image, ActionChoice[] actions)
        {
            float min = side * 0.5f;
            var slot = Rect("Document clip", notebookRight, new Vector2(min, .124f), new Vector2(min + .5f, .856f),
                new Vector2(20, 0), new Vector2(-20, 0));
            Graphic(slot, new Color(0.83f, 0.78f, 0.65f), false);
            documentSlots[side] = slot;
            JournalText(slot, label, 19, .85f, .98f, true);
            if (image != null)
            {
                var imageArea = Rect("Placed document", slot, new Vector2(.035f, .345f), new Vector2(.965f, .852f));
                var imageRect = Rect("Paper scan", imageArea, Vector2.zero, Vector2.one);
                var raw = imageRect.gameObject.AddComponent<RawImage>();
                raw.texture = image;
                raw.raycastTarget = false;
                var aspect = imageRect.gameObject.AddComponent<AspectRatioFitter>();
                aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                aspect.aspectRatio = (float)image.width / image.height;
            }
            else
            {
                JournalText(slot, "Kéo tài liệu vào đây", 20, .44f, .62f);
            }
            for (int i = 0; i < actions.Length; i++)
            {
                float y = 0.025f + (actions.Length - i - 1) * 0.105f;
                NotebookButton(Rect("Paper action", slot, new Vector2(.035f, y), new Vector2(.965f, y + .096f)),
                    actions[i], PaperLight, centered: true);
            }
        }

        private void NotebookActions(ActionChoice[] actions, float height)
        {
            if (actions.Length == 0) return;
            int columns = actions.Length > 4 ? 2 : 1;
            int rows = (actions.Length + columns - 1) / columns;
            float rowHeight = (height - .039f) / rows;
            for (int i = 0; i < actions.Length; i++)
            {
                float x = i % columns / (float)columns;
                float y = .038f + (rows - 1 - i / columns) * rowHeight;
                var rect = Rect("Notebook action", notebookRight, new Vector2(x, y), new Vector2(x + 1f / columns, y + rowHeight),
                    new Vector2(28, 3), new Vector2(-28, -3));
                NotebookButton(rect, actions[i], Paper);
            }
        }

        private RectTransform MakeNotebookScroll(RectTransform viewport, out ScrollRect scroll)
        {
            Graphic(viewport, Color.clear, true);
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            var content = Rect("Notebook lines", viewport, new Vector2(0, 1), Vector2.one);
            content.pivot = new Vector2(.5f, 1);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5;
            layout.childControlHeight = layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35;
            return content;
        }

        private void JournalText(RectTransform page, string copy, int size, float bottom, float top, bool bold = false)
        {
            var area = Rect("Notebook text", page, new Vector2(0, bottom), new Vector2(1, top),
                new Vector2(28, 0), new Vector2(-25, 0));
            var label = FillText("Writing", area, size, BookInk, TextAnchor.MiddleLeft, bold);
            label.text = copy;
            label.supportRichText = false;
        }

        private void PageRule(RectTransform page, float y) =>
            Graphic(Rect("Ruled margin", page, new Vector2(0, y), new Vector2(1, y), new Vector2(23, 0), new Vector2(-23, 1)), BookRule, false);

        private void NotebookButton(RectTransform row, ActionChoice action, Color color, bool centered = false)
        {
            var normal = Graphic(row, color, true);
            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = normal;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.colors = new ColorBlock
            {
                normalColor = Color.white, highlightedColor = new Color(0.86f, 0.89f, 0.76f),
                pressedColor = new Color(0.70f, 0.77f, 0.60f), selectedColor = new Color(0.75f, 0.81f, 0.62f),
                disabledColor = new Color(0.77f, 0.73f, 0.64f), colorMultiplier = 1f, fadeDuration = 0.09f
            };
            button.interactable = action.Enabled;
            if (action.Enabled)
            {
                button.onClick.AddListener(() => { if (!ModalInputBlocked) action.Press?.Invoke(); });
                focus.Add(button);
            }
            var area = Rect("Pencil margin", row, Vector2.zero, Vector2.one, new Vector2(12, 2), new Vector2(-12, -2));
            var text = FillText("Notebook label", area, 20, BookInk, centered ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft, true);
            text.text = action.Label;
            text.supportRichText = false;
        }

        internal void FinishNotebook(bool writing = false)
        {
            if (writing) FocusNotebookWriting();
            else MoveFocus(1);
        }

        private bool HandleNotebookInput(Keyboard keys)
        {
            if (!notebookOpen) return false;
            if (notebookEntry != null && notebookEntry.isFocused)
            {
                if (keys.tabKey.wasPressedThisFrame || keys.escapeKey.wasPressedThisFrame)
                {
                    notebookEntry.DeactivateInputField();
                    MoveFocus(1);
                }
                return true;
            }
            if (notebookAlternate && keys.jKey.wasPressedThisFrame)
            {
                dismiss?.Invoke();
                return true;
            }
            if (notebookIndexScroll != null && (keys.leftArrowKey.wasPressedThisFrame || keys.rightArrowKey.wasPressedThisFrame))
            {
                int index = focus.FindIndex(b => b.transform.IsChildOf(notebookIndexScroll.transform));
                if (index >= 0) MoveFocus(index - selected);
                return true;
            }
            return false;
        }

        private void DragNotebookSlip(Vector2 point)
        {
            if (heldSlip != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, point, null, out Vector2 local))
                heldSlip.anchoredPosition = local - canvas.rect.min + new Vector2(8, 18);
        }

        private void RevealNotebookEntry(Transform entry)
        {
            if (notebookIndexScroll == null || !entry.IsChildOf(notebookIndexScroll.content)) return;
            Canvas.ForceUpdateCanvases();
            var viewport = notebookIndexScroll.viewport;
            var row = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, entry);
            float excess = row.max.y > viewport.rect.yMax ? row.max.y - viewport.rect.yMax :
                row.min.y < viewport.rect.yMin ? row.min.y - viewport.rect.yMin : 0f;
            float scrollable = notebookIndexScroll.content.rect.height - viewport.rect.height;
            if (scrollable > 0.1f && Mathf.Abs(excess) > 0.1f)
                notebookIndexScroll.verticalNormalizedPosition = Mathf.Clamp01(notebookIndexScroll.verticalNormalizedPosition + excess / scrollable);
        }

        private IEnumerator TurnNotebookPage()
        {
            var group = notebookRight.gameObject.AddComponent<CanvasGroup>();
            for (float t = 0f; t < .17f; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.SmoothStep(.62f, 1f, t / .17f);
                yield return null;
            }
            group.alpha = 1f;
            pageTurn = null;
        }

        private void ResetNotebookInput()
        {
            if (pageTurn != null) StopCoroutine(pageTurn);
            pageTurn = null;
            if (heldSlip != null) Destroy(heldSlip.gameObject);
            heldSlip = null;
            notebookOpen = false;
            notebookIndexScroll = null;
            readingScroll = null;
            notebookEntry = null;
            documentSlots = null;
            dropDocument = null;
        }
    }
}
