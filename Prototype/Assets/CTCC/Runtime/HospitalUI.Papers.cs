using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CTCC
{
    internal sealed class PaperPlacement
    {
        public readonly string Id;
        public bool Back;
        public bool LargePrint;
        public float Zoom = 1;
        public Vector2 Pan;
        public float PrintOffset;

        public PaperPlacement(string id) => Id = id;
    }

    public sealed partial class HospitalUI
    {
        private sealed class PaperView
        {
            public RectTransform Root, Viewport, Sheet, Empty;
            public Image FocusEdge;
            public RawImage Scan;
            public ScrollRect PrintScroll;
            public Text Print;
            public PaperPlacement Placement;
            public DocumentHotspot[] Hotspots;
            public Action<Vector2> Pin;
            public readonly List<RectTransform> Marks = new();
            public readonly List<RectTransform> Pins = new();
        }

        private readonly PaperView[] paperViews = new PaperView[2];
        private int activePaper;
        private bool paperComparison;

        internal void NotebookCompare(Action<string, int> drop, ActionChoice swap, ActionChoice back)
        {
            paperComparison = true;
            NotebookLabel(notebookRight, "Đối chiếu", 31, 0, .906f, .46f, .995f, bold: true);
            NotebookButton(notebookRight, .49f, .916f, .735f, .984f, swap, Paper, true,
                insetMin: new Vector2(5, 0), insetMax: new Vector2(-5, 0));
            NotebookButton(notebookRight, .74f, .916f, .985f, .984f, back, Paper, true,
                insetMin: new Vector2(5, 0), insetMax: new Vector2(-5, 0));
            NotebookLabel(notebookRight, "1 / 2 Chọn tờ   ·   WASD Dịch giấy   ·   Q / Z Thu phóng   ·   Bấm phải đánh dấu",
                17, 0, .011f, 1, .061f, color: NoteInk);
            dropSlip = (id, point) =>
            {
                for (int side = 0; side < paperViews.Length; side++)
                {
                    var view = paperViews[side];
                    if (view != null && RectTransformUtility.RectangleContainsScreenPoint(view.Root, point))
                    {
                        drop(id, side);
                        return;
                    }
                }
            };
        }

        internal void NotebookPaper(int side, PaperPlacement placement, string label, Texture texture, string transcript,
            DocumentHotspot[] hotspots, IReadOnlyList<JournalPaperMark> pins, Action flip, Action inspect, Action print,
            Action remove, Action<Vector2> pin, Action attach)
        {
            var view = paperViews[side] ??= BuildPaperView(side);
            view.Placement = placement;
            view.Hotspots = hotspots;
            view.Pin = pin;
            view.Root.gameObject.SetActive(true);
            view.FocusEdge.enabled = side == activePaper;
            NotebookLabel(view.Root, label, 20, 0, .916f, 1, .997f, bold: true);
            bool occupied = placement != null;
            bool accessible = occupied && (placement.LargePrint || texture == null);
            view.Viewport.gameObject.SetActive(occupied && !accessible);
            view.PrintScroll.gameObject.SetActive(accessible);
            view.Empty.gameObject.SetActive(!occupied);
            if (!occupied)
            {
                NotebookLabel(view.Empty, "Kéo giấy vào đây", 21, 0, .36f, 1, .57f, color: NoteInk, align: TextAnchor.MiddleCenter);
                return;
            }

            Tool(0, "Lật giấy", flip);
            Tool(1, accessible ? "Mặt giấy" : "Chữ lớn", print);
            Tool(2, "Soi 3D", inspect);
            Tool(3, "Rút tờ", remove);
            if (attach != null)
                NotebookButton(view.Root, .516f, .096f, .964f, .151f, new ActionChoice("Kẹp nguồn vào F101", attach), PaperLight, true, 16);

            if (accessible)
            {
                view.Print.fontSize = Scaled(23);
                view.Print.text = transcript;
                float height = view.Print.cachedTextGeneratorForLayout.GetPreferredHeight(transcript,
                    view.Print.GetGenerationSettings(new Vector2(view.Print.rectTransform.rect.width, 0))) / view.Print.pixelsPerUnit;
                view.PrintScroll.content.sizeDelta = new Vector2(0, height + 16);
                view.PrintScroll.StopMovement();
                float maxOffset = Mathf.Max(0, height + 16 - view.PrintScroll.viewport.rect.height);
                view.PrintScroll.content.anchoredPosition = new Vector2(0, Mathf.Clamp(placement.PrintOffset, 0, maxOffset));
                if (side == activePaper) readingScroll = view.PrintScroll;
            }
            else
            {
                view.Scan.texture = texture;
                SizePaper(view);
                if (pin != null)
                    NotebookButton(view.Root, .034f, .096f, .482f, .151f,
                        new ActionChoice("Ghim vùng đang soi", () => PinAtCenter(view)), PaperLight, true, 16);
                NotebookButton(view.Root, .035f, .021f, .128f, .087f, new ActionChoice("−", () => ZoomPaper(view, -1)), Paper, true, 23);
                NotebookButton(view.Root, .137f, .021f, .23f, .087f, new ActionChoice("+", () => ZoomPaper(view, 1)), Paper, true, 23);
                NotebookButton(view.Root, .244f, .021f, .405f, .087f,
                    new ActionChoice("Đặt lại", () => ResetPaper(view)), Paper, true, 17);
            }

            DrawPaperMarks(view, pins);
            for (int i = 0; i < hotspots.Length; ++i)
            {
                var field = hotspots[i];
                float start = .421f + i * .27f;
                NotebookButton(view.Root, start, .021f, start + .264f, .087f,
                    new ActionChoice((field.Marked ? "✓ " : "") + field.Label, field.Press), Paper, true, 16);
            }

            void Tool(int column, string text, Action press)
            {
                float x = .033f + column * .235f;
                NotebookButton(view.Root, x, .826f, x + .225f, .908f,
                    new ActionChoice(text, () => { SelectPaper(side); press?.Invoke(); }, press != null), PaperLight, true, 17);
            }
        }

        private PaperView BuildPaperView(int side)
        {
            float x = side * .5f;
            var root = Rect("Document clip", notebookRight, new Vector2(x, .066f), new Vector2(x + .5f, .878f),
                new Vector2(17, 0), new Vector2(-17, 0));
            Graphic(root, new Color(.83f, .79f, .68f), false);
            var edge = Graphic(Rect("Active paper", root, new Vector2(0, .91f), Vector2.one, Vector2.zero, new Vector2(4, 0)),
                new Color(NoteInk.r, NoteInk.g, NoteInk.b, .30f), false);
            var viewport = Rect("Paper viewport", root, new Vector2(.033f, .159f), new Vector2(.967f, .818f));
            Graphic(viewport, new Color(.20f, .215f, .187f), true);
            viewport.gameObject.AddComponent<RectMask2D>();
            var sheet = Rect("Paper scan", viewport, Vector2.one * .5f, Vector2.one * .5f);
            var raw = sheet.gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;
            var empty = Rect("Empty clip", root, Vector2.zero, Vector2.one);
            var printScroll = MakeNotebookScroll(root, "Paper transcript");
            Place((RectTransform)printScroll.transform, .032f, .159f, .968f, .817f, new Vector2(12, 0), new Vector2(-12, 0));
            var print = FillText("Large print on paper", printScroll.content, 23, BookInk, TextAnchor.UpperLeft);
            print.supportRichText = false;
            print.lineSpacing = 1.18f;
            var view = new PaperView { Root = root, Viewport = viewport, Sheet = sheet, Scan = raw, Empty = empty,
                PrintScroll = printScroll, Print = print, FocusEdge = edge };
            var pointer = viewport.gameObject.AddComponent<PaperPointer>();
            pointer.Resize = () => SizePaper(view);
            pointer.Pan = delta =>
            {
                if (ModalInputBlocked || view.Placement == null) return;
                SelectPaper(side);
                PanPaper(view, delta * (canvas.rect.width / Screen.width));
            };
            pointer.Zoom = (amount, point) =>
            {
                if (ModalInputBlocked || view.Placement == null || Mathf.Abs(amount) < .01f) return;
                SelectPaper(side);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(view.Viewport, point, null, out var local);
                ZoomPaper(view, Mathf.Sign(amount), local);
            };
            pointer.Press = (point, right) =>
            {
                if (ModalInputBlocked || view.Placement == null) return;
                SelectPaper(side);
                if (right && RectTransformUtility.ScreenPointToLocalPointInRectangle(view.Sheet, point, null, out var local))
                {
                    if (!view.Sheet.rect.Contains(local)) return;
                    Vector2 uv = PaperUv(view.Sheet.rect, local);
                    foreach (var field in view.Hotspots)
                    {
                        if (!field.Uv.Contains(uv)) continue;
                        field.Press?.Invoke();
                        return;
                    }
                    view.Pin?.Invoke(uv);
                }
            };
            return view;
        }

        private void SelectPaper(int side)
        {
            activePaper = side;
            for (int i = 0; i < paperViews.Length; i++)
                if (paperViews[i] != null) paperViews[i].FocusEdge.enabled = i == side;
            var print = paperViews[side]?.PrintScroll;
            readingScroll = print != null && print.gameObject.activeSelf ? print : null;
        }

        private static Vector2 FitPaper(PaperView view)
        {
            Vector2 viewport = view.Viewport.rect.size;
            float aspect = (float)view.Scan.texture.width / view.Scan.texture.height;
            float height = Mathf.Max(0, Mathf.Min(viewport.y - 14, (viewport.x - 14) / aspect));
            return new Vector2(height * aspect, height);
        }

        private static void SizePaper(PaperView view)
        {
            if (view.Placement == null || view.Scan.texture == null) return;
            view.Sheet.sizeDelta = FitPaper(view) * view.Placement.Zoom;
            SetPaperPosition(view, Vector2.Scale(view.Placement.Pan, view.Viewport.rect.size));
        }

        private static void SetPaperPosition(PaperView view, Vector2 position)
        {
            Vector2 size = view.Viewport.rect.size;
            Vector2 limit = Vector2.Max(Vector2.zero, (view.Sheet.sizeDelta - size) * .5f);
            var clamped = new Vector2(Mathf.Clamp(position.x, -limit.x, limit.x), Mathf.Clamp(position.y, -limit.y, limit.y));
            view.Sheet.anchoredPosition = clamped;
            if (size.x > 0 && size.y > 0) view.Placement.Pan = new Vector2(clamped.x / size.x, clamped.y / size.y);
        }

        private static void ZoomPaper(PaperView view, float direction, Vector2 anchor = default)
        {
            float last = view.Placement.Zoom;
            float zoom = Mathf.Clamp(last * Mathf.Pow(1.22f, direction), 1, 4);
            if (Mathf.Approximately(zoom, last)) return;
            Vector2 move = anchor + (view.Sheet.anchoredPosition - anchor) * (zoom / last);
            view.Placement.Zoom = zoom;
            view.Sheet.sizeDelta = FitPaper(view) * zoom;
            SetPaperPosition(view, move);
        }

        private static void PanPaper(PaperView view, Vector2 delta) => SetPaperPosition(view, view.Sheet.anchoredPosition + delta);

        private static void ResetPaper(PaperView view)
        {
            view.Placement.Zoom = 1;
            view.Placement.Pan = Vector2.zero;
            SizePaper(view);
        }

        private static Vector2 PaperUv(Rect rect, Vector2 point) =>
            new((point.x - rect.xMin) / rect.width, (point.y - rect.yMin) / rect.height);

        private static void PinAtCenter(PaperView view)
        {
            if (view.Sheet.rect.width <= 0) return;
            view.Pin?.Invoke(PaperUv(view.Sheet.rect, -view.Sheet.anchoredPosition));
        }

        private static void DrawPaperMarks(PaperView view, IReadOnlyList<JournalPaperMark> pins)
        {
            int used = 0;
            foreach (var field in view.Hotspots)
            {
                if (!field.Marked) continue;
                if (used == view.Marks.Count)
                {
                    var line = Rect("Pencil mark", view.Sheet, Vector2.zero, Vector2.one);
                    Graphic(line, MarkInk, false);
                    view.Marks.Add(line);
                }
                var mark = view.Marks[used++];
                Place(mark, field.Uv.xMin + .018f, field.Uv.yMin, field.Uv.xMax - .018f, field.Uv.yMin,
                    new Vector2(0, 3), new Vector2(0, 5));
                mark.gameObject.SetActive(true);
            }
            for (int i = used; i < view.Marks.Count; i++) view.Marks[i].gameObject.SetActive(false);
            for (int i = 0; i < pins.Count; i++)
            {
                if (i == view.Pins.Count)
                {
                    var dot = Rect("Noted region", view.Sheet, Vector2.zero, Vector2.zero);
                    Graphic(dot, MarkInk, false);
                    view.Pins.Add(dot);
                }
                var pin = pins[i];
                Place(view.Pins[i], pin.x, pin.y, pin.x, pin.y, new Vector2(-5, -5), new Vector2(5, 5));
                view.Pins[i].localRotation = Quaternion.Euler(0, 0, 45);
                view.Pins[i].gameObject.SetActive(true);
            }
            for (int i = pins.Count; i < view.Pins.Count; i++) view.Pins[i].gameObject.SetActive(false);
        }

        private bool HandlePaperInput(Keyboard keys)
        {
            if (!paperComparison) return false;
            if (keys.digit1Key.wasPressedThisFrame || keys.digit2Key.wasPressedThisFrame)
            {
                SelectPaper(keys.digit1Key.wasPressedThisFrame ? 0 : 1);
                return true;
            }
            var view = paperViews[activePaper];
            if (view?.Placement == null || !view.Viewport.gameObject.activeSelf) return false;
            if (keys.mKey.wasPressedThisFrame && view.Pin != null)
            {
                PinAtCenter(view);
                return true;
            }
            var delta = new Vector2((keys.dKey.isPressed ? -1 : 0) + (keys.aKey.isPressed ? 1 : 0),
                (keys.wKey.isPressed ? -1 : 0) + (keys.sKey.isPressed ? 1 : 0));
            if (delta != Vector2.zero) PanPaper(view, delta * (Time.unscaledDeltaTime * 440));
            if (keys.qKey.isPressed || keys.zKey.isPressed) ZoomPaper(view, (keys.qKey.isPressed ? 1 : -1) * Time.unscaledDeltaTime * 8);
            return false;
        }

        private void HidePaperViews()
        {
            paperComparison = false;
            foreach (var view in paperViews)
            {
                if (view == null || !view.Root.gameObject.activeSelf) continue;
                if (view.Placement != null && view.PrintScroll.gameObject.activeSelf)
                    view.Placement.PrintOffset = view.PrintScroll.content.anchoredPosition.y;
                view.Root.gameObject.SetActive(false);
            }
        }
    }
}
