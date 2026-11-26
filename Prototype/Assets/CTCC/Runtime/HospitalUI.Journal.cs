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
        private static readonly Color BookCover = new(.19f, .145f, .115f);
        private static readonly Color Paper = new(.89f, .85f, .745f);
        private static readonly Color PaperLight = new(.96f, .925f, .83f);
        private static readonly Color BookInk = new(.15f, .20f, .19f);
        private static readonly Color NoteInk = new(.21f, .34f, .35f);
        private static readonly Color BookRule = new(.42f, .35f, .25f, .4f);
        private static readonly Color MarkInk = new(.58f, .22f, .18f);
        private static readonly string[] Bookmarks = { "Hiện trường", "Tài liệu", "Suy luận", "Dòng thời gian", "Quyết định" };

        private readonly List<NotebookControl> notebookControls = new();
        private readonly List<NotebookControl> notebookEntries = new();
        private readonly List<Text> notebookLabels = new();
        private RectTransform notebookRoot, notebookLeft, notebookRight, binding, heldSlip, illustration;
        private CanvasGroup pageTint;
        private ScrollRect notebookIndexScroll, notebookReader;
        private InputField notebookEntry;
        private Text indexEmpty, readerText, slipTitle;
        private RawImage illustrationImage;
        private AspectRatioFitter illustrationFit;
        private Coroutine pageTurn;
        private bool notebookOpen, notebookAlternate, writingOnPage, readerOnPage;
        private int usedControls, usedEntries, usedLabels;
        private Action<string, Vector2> dropSlip;
        private Button notebookDefaultFocus, previousNotebookFocus;
        private string previousNotebookLabel;
        private int notebookBookmark;
        private bool notebookWide, notebookClipboard;
        private Action<string> changeWriting;
        private float notebookFontScale;

        private sealed class NotebookControl
        {
            public RectTransform Rect;
            public Text Label;
            public Image Backing;
            public Button Button;
            public ActionChoice Action;
            public string DragId;
            public NotebookDrag SignatureDrag;
        }

        internal void ShowNotebook(int bookmark, Action<int> select, Action close, bool alternate, bool animate,
            bool wideComparison = false, bool clipboard = false, bool returnToClipboard = false, Action putDown = null)
        {
            bool samePage = notebookOpen && notebookBookmark == bookmark && notebookWide == wideComparison &&
                notebookClipboard == clipboard;
            previousNotebookFocus = samePage && EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject?.GetComponent<Button>() : null;
            previousNotebookLabel = previousNotebookFocus?.GetComponentInChildren<Text>()?.text;
            notebookBookmark = bookmark;
            notebookWide = wideComparison;
            notebookClipboard = clipboard;
            if (!notebookOpen) ClearModalPanel();
            if (notebookRoot == null) BuildNotebook();
            StopPageTurn();
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
            onCloseExtra = null;
            focus.Clear();
            selected = -1;

            panel.anchorMin = new Vector2(.044f, .064f);
            panel.anchorMax = new Vector2(.956f, .894f);
            panelBg.color = BookCover;
            modalBackdrop.color = new Color(.018f, .020f, .016f, .68f);
            modalBackdrop.raycastTarget = true;
            float split = wideComparison ? .165f : clipboard ? .285f : .303f;
            Place(notebookLeft, 0, 0, split, 1, new Vector2(16, 17), new Vector2(-4, -17));
            Place(notebookRight, split, 0, 1, 1, new Vector2(4, 17), new Vector2(-16, -17));
            Place(binding, split, 0, split, 1, new Vector2(-4, 14), new Vector2(4, -15));

            if (!clipboard)
            {
                for (int i = 0; i < Bookmarks.Length; i++)
                {
                    int index = i;
                    float x = .021f + i * .155f;
                    var tab = NotebookButton(notebookRoot, x, 1, x + .148f, 1.063f,
                        new ActionChoice(Bookmarks[i], () => select(index)), i == bookmark ? PaperLight : new Color(.72f, .70f, .58f), true, size: 17);
                    if (i == bookmark) notebookDefaultFocus = tab.Button;
                }
            }
            NotebookButton(notebookRoot, .829f, 1, .987f, 1.063f,
                new ActionChoice(clipboard ? "Đặt phiếu xuống" : returnToClipboard ? "Về phiếu F101" : "Khép sổ  ·  Esc",
                    putDown ?? close), Paper, true, size: 17);

            notebookRoot.gameObject.SetActive(true);
            bool wasModal = IsModal;
            modal.gameObject.SetActive(true);
            modalOpenedFrame = Time.frameCount;
            hud.gameObject.SetActive(false);
            if (!wasModal) OpenChanged?.Invoke();
            if (animate) pageTurn = StartCoroutine(TurnNotebookPage());
        }

        private void BuildNotebook()
        {
            notebookRoot = Rect("Sổ trực", panel, Vector2.zero, Vector2.one);
            Graphic(Rect("Page edges", notebookRoot, Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -12)),
                new Color(.67f, .60f, .47f), false);
            notebookLeft = Rect("Left page", notebookRoot, Vector2.zero, Vector2.one);
            notebookRight = Rect("Right page", notebookRoot, Vector2.zero, Vector2.one);
            Graphic(notebookLeft, Paper, false);
            Graphic(notebookRight, PaperLight, false);
            pageTint = notebookRight.gameObject.AddComponent<CanvasGroup>();
            binding = Rect("Binding", notebookRoot, Vector2.zero, Vector2.one);
            Graphic(binding, new Color(.34f, .27f, .18f, .47f), false);
            PageRule(notebookLeft, .893f);
            PageRule(notebookRight, .892f);

            notebookIndexScroll = MakeNotebookScroll(notebookLeft, "Notebook index");
            indexEmpty = FillText("Empty index", notebookIndexScroll.content, 20, BookInk, TextAnchor.UpperLeft);
            notebookReader = MakeNotebookScroll(notebookRight, "Ruled page");
            readerText = FillText("Notebook writing", notebookReader.content, 23, BookInk, TextAnchor.UpperLeft);
            readerText.supportRichText = false;
            readerText.lineSpacing = 1.18f;
            BuildNotebookEntry();

            illustration = Rect("Document pasted into the journal", notebookRight, new Vector2(.58f, .12f), new Vector2(.97f, .856f));
            var imageRect = Rect("Pasted scan", illustration, Vector2.zero, Vector2.one);
            illustrationImage = imageRect.gameObject.AddComponent<RawImage>();
            illustrationImage.color = Color.white;
            illustrationImage.raycastTarget = false;
            illustrationFit = imageRect.gameObject.AddComponent<AspectRatioFitter>();
            illustrationFit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

            heldSlip = Fixed("Paper in hand", canvas, Vector2.zero, Vector2.zero, new Vector2(330, 64));
            Graphic(heldSlip, PaperLight, false);
            slipTitle = FillText("Dragged title", heldSlip, 19, BookInk, TextAnchor.MiddleCenter, true);
            heldSlip.gameObject.SetActive(false);
        }

        internal void NotebookIndex(IReadOnlyList<NotebookEntry> entries, string empty, string heading = "Sổ trực  /  An Sinh",
            float bottom = .065f)
        {
            NotebookLabel(notebookLeft, heading, 22, 0, .905f, 1, .997f, bold: true);
            Place((RectTransform)notebookIndexScroll.transform, 0, bottom, 1, .877f, new Vector2(17, 0), new Vector2(-18, 0));
            indexEmpty.gameObject.SetActive(entries.Count == 0);
            indexEmpty.fontSize = Scaled(20);
            indexEmpty.text = empty;
            float y = 0;
            float height = Mathf.Max(64, 59 * notebookFontScale);
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var row = TakeNotebookControl(notebookEntries, ref usedEntries, notebookIndexScroll.content, isEntry: true);
                Place(row.Rect, 0, 1, 1, 1, new Vector2(0, -y - height), new Vector2(0, -y));
                BindNotebookControl(row, entry.Choice, PaperLight, false, entry.DragId, 18);
                y += height + 7;
            }
            notebookIndexScroll.content.sizeDelta = new Vector2(0, Mathf.Max(y, 95));
            float maxOffset = Mathf.Max(0, notebookIndexScroll.content.rect.height - notebookIndexScroll.viewport.rect.height);
            notebookIndexScroll.content.anchoredPosition = new Vector2(0,
                Mathf.Clamp(notebookIndexScroll.content.anchoredPosition.y, 0, maxOffset));
        }

        internal void NotebookPage(string title, string body, params ActionChoice[] actions) =>
            NotebookPage(title, body, null, actions);

        internal void NotebookPage(string title, string body, Texture scan, params ActionChoice[] actions)
        {
            NotebookLabel(notebookRight, title, 31, 0, .901f, 1, .992f, bold: true);
            float foot = NotebookActions(actions);
            readerOnPage = true;
            readingScroll = notebookReader;
            Place((RectTransform)notebookReader.transform, 0, foot, scan == null ? 1 : .575f, .862f,
                new Vector2(28, 0), new Vector2(-26, -4));
            notebookReader.gameObject.SetActive(true);
            readerText.fontSize = Scaled(23);
            readerText.text = body;
            float height = readerText.cachedTextGeneratorForLayout.GetPreferredHeight(body,
                readerText.GetGenerationSettings(new Vector2(readerText.rectTransform.rect.width, 0))) / readerText.pixelsPerUnit + 14;
            notebookReader.content.sizeDelta = new Vector2(0, height);
            NotebookIllustration(notebookRight, scan, .58f, .12f, .97f, .856f);
        }

        private void NotebookIllustration(RectTransform page, Texture scan, float x, float y, float x1, float y1)
        {
            if (scan == null) return;
            if (illustration.parent != page) illustration.SetParent(page, false);
            Place(illustration, x, y, x1, y1);
            illustrationImage.texture = scan;
            illustrationFit.aspectRatio = (float)scan.width / scan.height;
            illustration.gameObject.SetActive(true);
        }

        private void BuildNotebookEntry()
        {
            var box = Rect("Notebook input", notebookRight, new Vector2(0, .387f), new Vector2(1, .71f),
                new Vector2(29, 0), new Vector2(-29, 0));
            var backing = Graphic(box, new Color(.90f, .876f, .77f), true);
            notebookEntry = box.gameObject.AddComponent<InputField>();
            var text = MakeText(Rect("Writing", box, Vector2.zero, Vector2.one, new Vector2(15, 14), new Vector2(-15, -14)),
                23, NoteInk, TextAnchor.UpperLeft, false);
            text.supportRichText = false;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            var placeholder = FillText("Writing prompt", box, 21, NoteInk, TextAnchor.MiddleCenter);
            placeholder.text = "Ghi lại điều mình đang nghĩ…";
            notebookEntry.textComponent = text;
            notebookEntry.placeholder = placeholder;
            notebookEntry.targetGraphic = backing;
            notebookEntry.characterLimit = JournalNote.MaxLength;
            notebookEntry.lineType = InputField.LineType.MultiLineNewline;
            notebookEntry.navigation = new Navigation { mode = Navigation.Mode.None };
            notebookEntry.onValueChanged.AddListener(value => changeWriting?.Invoke(value));
        }

        internal void NotebookEditor(string title, string value, Action<string> changed, ActionChoice chooseFirst,
            ActionChoice chooseSecond, ActionChoice openFirst, ActionChoice openSecond, ActionChoice unlink,
            ActionChoice status, IReadOnlyList<ActionChoice> footer)
        {
            NotebookLabel(notebookRight, title, 31, 0, .901f, .62f, .992f, bold: true);
            if (status.Enabled)
                NotebookButton(notebookRight, .64f, .915f, .976f, .976f, status, Paper, true, 17);

            NotebookButton(notebookRight, .023f, .751f, .465f, .847f, chooseFirst, Paper, size: 18);
            NotebookButton(notebookRight, .535f, .751f, .976f, .847f, chooseSecond, Paper, size: 18);
            if (unlink.Enabled)
                NotebookLabel(notebookRight, "↔", 25, .446f, .761f, .555f, .839f, bold: true,
                    color: NoteInk, align: TextAnchor.MiddleCenter);
            if (openFirst.Enabled) NotebookButton(notebookRight, .024f, .686f, .258f, .743f, openFirst, PaperLight, size: 17);
            if (openSecond.Enabled) NotebookButton(notebookRight, .536f, .686f, .772f, .743f, openSecond, PaperLight, size: 17);
            if (unlink.Enabled) NotebookButton(notebookRight, .774f, .686f, .966f, .743f, unlink, PaperLight, size: 17).Label.color = MarkInk;

            NotebookLabel(notebookRight, "Ghi chú của Minh", 19, 0, .59f, .72f, .671f, bold: true, color: NoteInk);
            NotebookButton(notebookRight, .794f, .602f, .976f, .66f,
                new ActionChoice("Viết tiếp", FocusNotebookWriting), PaperLight, true, 17);
            float foot = NotebookActions(footer);
            Place((RectTransform)notebookEntry.transform, 0, foot, 1, .589f, new Vector2(29, 0), new Vector2(-29, 0));
            notebookEntry.textComponent.fontSize = Scaled(23);
            ((Text)notebookEntry.placeholder).fontSize = Scaled(21);
            notebookEntry.SetTextWithoutNotify(value ?? "");
            changeWriting = changed;
            writingOnPage = true;
            notebookEntry.gameObject.SetActive(true);
        }

        internal void FocusNotebookWriting()
        {
            if (!writingOnPage) return;
            notebookEntry.Select();
            notebookEntry.ActivateInputField();
        }

        private float NotebookActions(IReadOnlyList<ActionChoice> actions)
        {
            const int columns = 3;
            const float rowHeight = .079f;
            float foot = .038f;
            int rows = (actions.Count + columns - 1) / columns;
            int visibleColumns = Mathf.Min(columns, actions.Count);
            for (int i = 0; i < actions.Count; i++)
            {
                float x = i % columns / (float)visibleColumns;
                float y = foot + (rows - 1 - i / columns) * rowHeight;
                NotebookButton(notebookRight, x, y, x + 1f / visibleColumns, y + rowHeight, actions[i], Paper, true,
                    insetMin: new Vector2(15, 3), insetMax: new Vector2(-15, -3));
            }
            return foot + rows * rowHeight + .02f;
        }

        private static ScrollRect MakeNotebookScroll(RectTransform page, string name)
        {
            var viewport = Rect(name, page, Vector2.zero, Vector2.one);
            Graphic(viewport, Color.clear, true);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            var content = Rect("Notebook lines", viewport, new Vector2(0, 1), Vector2.one);
            content.pivot = new Vector2(.5f, 1);
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.inertia = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 36;
            return scroll;
        }

        private Text NotebookLabel(RectTransform parent, string copy, int size, float x, float y, float x1, float y1,
            bool bold = false, Color? color = null, TextAnchor align = TextAnchor.MiddleLeft)
        {
            if (usedLabels == notebookLabels.Count)
                notebookLabels.Add(FillText("Notebook text", parent, size, BookInk, align));
            Text label = notebookLabels[usedLabels++];
            if (label.transform.parent != parent) label.transform.SetParent(parent, false);
            label.transform.SetAsLastSibling();
            Place(label.rectTransform, x, y, x1, y1, new Vector2(24, 1), new Vector2(-22, -1));
            label.font = bold ? heading : standard;
            label.fontSize = Scaled(size);
            label.color = color ?? BookInk;
            label.alignment = align;
            label.supportRichText = false;
            label.text = copy;
            label.gameObject.SetActive(true);
            return label;
        }

        private static void PageRule(RectTransform page, float y) =>
            Graphic(Rect("Ruled margin", page, new Vector2(0, y), new Vector2(1, y), new Vector2(20, 0), new Vector2(-20, 1)), BookRule, false);

        private NotebookControl NotebookButton(RectTransform parent, float x, float y, float x1, float y1, ActionChoice action,
            Color color, bool centered = false, int size = 19, Vector2 insetMin = default, Vector2 insetMax = default)
        {
            var control = TakeNotebookControl(notebookControls, ref usedControls, parent);
            Place(control.Rect, x, y, x1, y1, insetMin, insetMax);
            BindNotebookControl(control, action, color, centered, null, size);
            return control;
        }

        private NotebookControl TakeNotebookControl(List<NotebookControl> controls, ref int used, RectTransform parent, bool isEntry = false)
        {
            if (used == controls.Count)
            {
                var row = Rect(isEntry ? "Notebook entry" : "Notebook action", parent, Vector2.zero, Vector2.one);
                var control = new NotebookControl { Rect = row, Backing = Graphic(row, Paper, true), Button = row.gameObject.AddComponent<Button>() };
                var inset = Rect("Pencil margin", row, Vector2.zero, Vector2.one, new Vector2(10, 1), new Vector2(-10, -1));
                control.Label = FillText("Notebook label", inset, 19, BookInk, TextAnchor.MiddleLeft, true);
                control.Label.supportRichText = false;
                var button = control.Button;
                button.targetGraphic = control.Backing;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                button.colors = new ColorBlock
                {
                    normalColor = Color.white, highlightedColor = new Color(.84f, .91f, .78f),
                    pressedColor = new Color(.68f, .79f, .62f), selectedColor = new Color(.68f, .82f, .62f),
                    disabledColor = new Color(.87f, .83f, .77f), colorMultiplier = 1, fadeDuration = .07f
                };
                button.onClick.AddListener(() =>
                {
                    if (!ModalInputBlocked && control.Action.Enabled) control.Action.Press?.Invoke();
                });
                if (isEntry)
                {
                    var drag = row.gameObject.AddComponent<NotebookDrag>();
                    drag.Begin = point =>
                    {
                        if (ModalInputBlocked || control.DragId == null || dropSlip == null) return;
                        heldSlip.gameObject.SetActive(true);
                        heldSlip.SetAsLastSibling();
                        slipTitle.fontSize = Scaled(19);
                        slipTitle.text = control.Action.Label;
                        DragNotebookSlip(point);
                    };
                    drag.Move = DragNotebookSlip;
                    drag.End = point =>
                    {
                        if (!heldSlip.gameObject.activeSelf) return;
                        heldSlip.gameObject.SetActive(false);
                        if (!ModalInputBlocked) dropSlip?.Invoke(control.DragId, point);
                    };
                }
                controls.Add(control);
            }
            var next = controls[used++];
            if (next.Rect.parent != parent) next.Rect.SetParent(parent, false);
            next.Rect.SetAsLastSibling();
            return next;
        }

        private void BindNotebookControl(NotebookControl control, ActionChoice action, Color color, bool centered,
            string dragId, int size)
        {
            control.Action = action;
            control.DragId = dragId;
            ClearSignatureDrag(control);
            control.Backing.color = color;
            control.Button.interactable = action.Enabled;
            control.Label.fontSize = Scaled(size);
            control.Label.alignment = centered ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            control.Label.color = BookInk;
            control.Label.text = action.Label;
            control.Rect.gameObject.SetActive(true);
            if (action.Enabled) focus.Add(control.Button);
        }

        private static void Place(RectTransform target, float x, float y, float x1, float y1,
            Vector2 min = default, Vector2 max = default)
        {
            target.anchorMin = new Vector2(x, y);
            target.anchorMax = new Vector2(x1, y1);
            target.offsetMin = min;
            target.offsetMax = max;
        }

        internal void FinishNotebook()
        {
            RetireNotebookControls(notebookControls, usedControls);
            RetireNotebookControls(notebookEntries, usedEntries);
            for (int i = usedLabels; i < notebookLabels.Count; i++) notebookLabels[i].gameObject.SetActive(false);
            notebookEntry.gameObject.SetActive(writingOnPage);
            notebookReader.gameObject.SetActive(readerOnPage);
            if (previousNotebookFocus != null && focus.Contains(previousNotebookFocus) &&
                VisibleAction(previousNotebookLabel) == VisibleAction(previousNotebookFocus.GetComponentInChildren<Text>()?.text))
                FocusButton(previousNotebookFocus);
            else if (notebookDefaultFocus != null) FocusButton(notebookDefaultFocus);
            else MoveFocus(1);
        }

        private static string VisibleAction(string label)
        {
            if (string.IsNullOrEmpty(label)) return label;
            return label[0] is '☑' or '□' or '↳' or '✓' ? label.Substring(1).TrimStart() : label;
        }

        private static void RetireNotebookControls(List<NotebookControl> controls, int used)
        {
            for (int i = used; i < controls.Count; i++)
            {
                controls[i].Action = default;
                controls[i].DragId = null;
                ClearSignatureDrag(controls[i]);
                controls[i].Rect.gameObject.SetActive(false);
            }
        }

        private bool HandleNotebookInput(Keyboard keys)
        {
            if (!notebookOpen) return false;
            if (writingOnPage && notebookEntry.isFocused)
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
            return HandlePaperInput(keys);
        }

        private static void ClearSignatureDrag(NotebookControl control)
        {
            if (control.SignatureDrag == null) return;
            control.SignatureDrag.Begin = control.SignatureDrag.Move = control.SignatureDrag.End = null;
        }

        private void DragNotebookSlip(Vector2 point)
        {
            if (heldSlip.gameObject.activeSelf &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, point, null, out var local))
                heldSlip.anchoredPosition = local - canvas.rect.min + new Vector2(10, 22);
        }

        private void RevealNotebookEntry(Transform entry)
        {
            if (!notebookOpen || entry.parent != notebookIndexScroll.content) return;
            var row = (RectTransform)entry;
            float viewTop = notebookIndexScroll.content.anchoredPosition.y;
            float viewHeight = notebookIndexScroll.viewport.rect.height;
            float top = -row.offsetMax.y;
            float bottom = -row.offsetMin.y;
            if (top < viewTop) viewTop = top;
            if (bottom > viewTop + viewHeight) viewTop = bottom - viewHeight;
            notebookIndexScroll.content.anchoredPosition = new Vector2(0, Mathf.Max(0, viewTop));
        }

        private IEnumerator TurnNotebookPage()
        {
            for (float t = 0; t < .18f; t += Time.unscaledDeltaTime)
            {
                pageTint.alpha = Mathf.SmoothStep(.58f, 1f, t / .18f);
                yield return null;
            }
            pageTint.alpha = 1;
            pageTurn = null;
        }

        private void StopPageTurn()
        {
            if (pageTurn != null) StopCoroutine(pageTurn);
            pageTurn = null;
            if (pageTint != null) pageTint.alpha = 1;
        }

        private void ResetNotebookInput()
        {
            notebookOpen = false;
            readingScroll = null;
            if (notebookRoot == null) return;
            if (notebookEntry.isFocused) notebookEntry.DeactivateInputField();
            notebookRoot.gameObject.SetActive(false);
            heldSlip.gameObject.SetActive(false);
            HidePaperViews();
            HideFormInk();
            StopPageTurn();
            RetireNotebookControls(notebookControls, 0);
            RetireNotebookControls(notebookEntries, 0);
            dropSlip = null;
            changeWriting = null;
        }
    }
}
