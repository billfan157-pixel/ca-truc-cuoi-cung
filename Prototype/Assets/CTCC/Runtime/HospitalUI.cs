using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CTCC
{
    public readonly struct ActionChoice
    {
        public readonly string Label;
        public readonly Action Press;
        public readonly bool Enabled;
        public ActionChoice(string label, Action press, bool enabled = true)
        {
            Label = label;
            Press = press;
            Enabled = enabled;
        }
    }

    public sealed class HospitalUI : MonoBehaviour
    {
        private static readonly Color Ink = new(0.91f, 0.91f, 0.84f);
        private static readonly Color Secondary = new(0.70f, 0.77f, 0.72f);
        private static readonly Color Accent = new(0.80f, 0.85f, 0.64f);
        private static readonly Color Frame = new(0.42f, 0.52f, 0.44f, 0.75f);
        private static readonly Color ScreenDark = new(0.052f, 0.078f, 0.072f, 0.94f);
        private static readonly Color InspectDark = new(0.045f, 0.068f, 0.064f, 0.88f);

        private readonly List<Button> focus = new();
        private Font standard, heading;
        private RectTransform canvas, hud, modal, panel, reticleRect;
        private Image fade, cueBacking, promptBacking, modalBackdrop, panelBg, reticleImg, toastBacking;
        private Text place, task, prompt, cue, help, toastTitle, toastBody;
        private Action dismiss;
        private Action onCloseExtra;
        private ScrollRect readingScroll;
        private float cueUntil, toastUntil;
        private float scale = 1f;
        private bool captions = true;
        private int selected;
        private int modalOpenedFrame = -1;

        public bool IsModal => modal != null && modal.gameObject.activeSelf;
        public bool ModalInputBlocked { get; set; }
        public event Action OpenChanged;

        public void Initialize()
        {
            standard = Resources.Load<Font>("CTCC/BeVietnamPro-Regular");
            heading = Resources.Load<Font>("CTCC/BeVietnamPro-SemiBold");
            if (standard == null || heading == null)
                throw new InvalidOperationException("Vietnamese UI typefaces are missing.");

            var root = new GameObject("CTCC · Interface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            var c = root.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 12;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.65f;
            canvas = root.GetComponent<RectTransform>();

            var events = new GameObject("Interface navigation", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
            var module = events.GetComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
            module.move = null;
            module.submit = null;
            module.cancel = null;

            hud = Rect("Play controls", canvas, Vector2.zero, Vector2.one);
            // Subtle dark vignette header bar top-left for razor-sharp contrast over bright B or dark R
            var objBg = Fixed("ObjectiveBacking", hud, new Vector2(0, 1), new Vector2(40, -76), new Vector2(560, 102));
            Graphic(objBg, new Color(0.035f, 0.052f, 0.048f, 0.52f), false);
            Graphic(Rect("ObjAccentBar", objBg, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(3, 0)), Accent, false);

            place = FloatText("Current reality", hud, new Vector2(0, 1), new Vector2(56, -48), new Vector2(530, 34), 19,
                Accent, TextAnchor.UpperLeft, true);
            task = FloatText("Objective", hud, new Vector2(0, 1), new Vector2(56, -92), new Vector2(535, 62), 22,
                Ink, TextAnchor.MiddleLeft, true);
            help = FloatText("Controls", hud, new Vector2(0, 0), new Vector2(44, 24), new Vector2(1100, 34), 17,
                new Color(0.72f, 0.78f, 0.73f, 0.72f), TextAnchor.MiddleLeft);

            // Morphing atmospheric crosshair dot
            reticleRect = Fixed("Aim", hud, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5, 5));
            reticleRect.pivot = new Vector2(0.5f, 0.5f);
            reticleImg = Graphic(reticleRect, new Color(0.95f, 0.92f, 0.78f, 0.62f), false);

            // Compact diegetic prompt pill
            var tag = Fixed("Interaction", hud, new Vector2(0.5f, 0.5f), new Vector2(0, -68), new Vector2(560, 44));
            tag.pivot = new Vector2(0.5f, 0.5f);
            promptBacking = Graphic(tag, new Color(0.042f, 0.065f, 0.058f, 0.84f), false);
            Graphic(Rect("PromptBorder", tag, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 2)), Accent, false);
            prompt = FillText("Prompt", tag, 20, Ink, TextAnchor.MiddleCenter, true);

            // Directional audio caption / subtitle strip
            var subtitle = Fixed("Caption", hud, new Vector2(0.5f, 0.11f), Vector2.zero, new Vector2(980, 56));
            subtitle.pivot = new Vector2(0.5f, 0.5f);
            cueBacking = Graphic(subtitle, new Color(0.038f, 0.058f, 0.054f, 0.88f), false);
            cue = FillText("Subtitle", subtitle, 21, Ink, TextAnchor.MiddleCenter);
            cueBacking.gameObject.SetActive(false);

            // Non-blocking diegetic environmental observation toast (bottom-right, replaces modal popups for world clues!)
            var toastBox = Fixed("ObservationToast", hud, new Vector2(1, 0), new Vector2(-580, 118), new Vector2(536, 134));
            toastBacking = Graphic(toastBox, new Color(0.042f, 0.066f, 0.060f, 0.90f), false);
            Graphic(Rect("ToastRule", toastBox, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(4, 0)), Accent, false);
            toastTitle = MakeText(Rect("ToastTitle", toastBox, new Vector2(0, 0.64f), new Vector2(1, 1), new Vector2(20, 0), new Vector2(-16, -8)),
                20, Accent, TextAnchor.MiddleLeft, true);
            toastBody = MakeText(Rect("ToastBody", toastBox, new Vector2(0, 0), new Vector2(1, 0.66f), new Vector2(20, 10), new Vector2(-16, -2)),
                19, Ink, TextAnchor.UpperLeft, false);
            toastBacking.gameObject.SetActive(false);

            // Modal / Side Inspect Container
            modal = Rect("Reading", canvas, Vector2.zero, Vector2.one);
            modalBackdrop = Graphic(modal, new Color(0.012f, 0.020f, 0.017f, 0.72f), true);
            panel = Rect("Reading page", modal, new Vector2(0.195f, 0.092f), new Vector2(0.805f, 0.908f));
            panelBg = Graphic(panel, ScreenDark, true);
            modal.gameObject.SetActive(false);

            fade = Graphic(Rect("Reality transition", canvas, Vector2.zero, Vector2.one), Color.clear, true);
            fade.gameObject.SetActive(false);
        }

        public void Apply(VisitSettings options)
        {
            scale = Mathf.Clamp(options.textScale, 0.85f, 1.35f);
            captions = options.captions;
            place.fontSize = Scaled(19);
            task.fontSize = Scaled(22);
            help.fontSize = Scaled(17);
            prompt.fontSize = Scaled(20);
            cue.fontSize = Scaled(21);
            toastTitle.fontSize = Scaled(20);
            toastBody.fontSize = Scaled(19);
            help.text = options.alternateBindings
                ? "Mũi tên  Đi    K  Tương tác    L  Đèn pin    J  Sổ trực    Esc  Menu"
                : "WASD  Đi    E  Tương tác    F  Đèn pin    Tab  Sổ trực    Esc  Menu";
        }

        public void Hud(string reality, string objective)
        {
            place.text = reality;
            task.text = objective;
        }

        public void Prompt(string text)
        {
            bool has = !string.IsNullOrEmpty(text);
            promptBacking.gameObject.SetActive(has);
            prompt.text = text ?? "";
            if (reticleRect != null)
            {
                reticleRect.sizeDelta = has ? new Vector2(9, 9) : new Vector2(5, 5);
                reticleRect.localRotation = has ? Quaternion.Euler(0, 0, 45f) : Quaternion.identity;
                reticleImg.color = has ? new Color(0.95f, 0.94f, 0.72f, 0.96f) : new Color(0.95f, 0.92f, 0.78f, 0.56f);
            }
        }

        public void Caption(string text, float seconds = 4f, bool essential = false)
        {
            if (!captions && !essential) return;
            cue.text = text;
            cueUntil = Time.unscaledTime + seconds;
            cueBacking.gameObject.SetActive(true);
        }

        public void Toast(string title, string body, float seconds = 6f)
        {
            toastTitle.text = title;
            toastBody.text = body;
            toastUntil = Time.unscaledTime + seconds;
            toastBacking.gameObject.SetActive(true);
        }

        public void Show(string section, string title, string body, ActionChoice[] buttons, Action close, Texture splitImage = null)
        {
            ShowLayout(section, title, body, buttons, close, splitImage, sideInspectMode: false, onClose: null);
        }

        public void ShowInspectSidebar(string section, string title, string body, ActionChoice[] buttons, Action close, Action onClose = null)
        {
            ShowLayout(section, title, body, buttons, close, splitImage: null, sideInspectMode: true, onClose: onClose);
        }

        private void ShowLayout(string section, string title, string body, ActionChoice[] buttons, Action close,
            Texture splitImage, bool sideInspectMode, Action onClose)
        {
            for (int i = panel.childCount - 1; i >= 0; i--)
            {
                panel.GetChild(i).gameObject.SetActive(false);
                Destroy(panel.GetChild(i).gameObject);
            }
            dismiss = close;
            onCloseExtra = onClose;
            focus.Clear();
            selected = -1;

            if (sideInspectMode)
            {
                // Leave left 62% completely clear for the live lit 3D Physical Inspector!
                modalBackdrop.color = new Color(0.01f, 0.018f, 0.016f, 0.16f);
                modalBackdrop.raycastTarget = false;
                panel.anchorMin = new Vector2(0.625f, 0.085f);
                panel.anchorMax = new Vector2(0.965f, 0.915f);
                panelBg.color = InspectDark;
            }
            else
            {
                modalBackdrop.color = new Color(0.012f, 0.020f, 0.017f, 0.72f);
                modalBackdrop.raycastTarget = true;
                panel.anchorMin = new Vector2(0.195f, 0.092f);
                panel.anchorMax = new Vector2(0.805f, 0.908f);
                panelBg.color = ScreenDark;
            }

            // Subtle frame edge border
            Graphic(Rect("BorderTop", panel, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -2), Vector2.zero), Frame, false);
            Graphic(Rect("BorderLeft", panel, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(2, 0)), Frame, false);

            int padX = sideInspectMode ? 32 : 49;
            int titleSize = sideInspectMode ? 34 : 46;
            var h = Rect("Document header", panel, new Vector2(0, 0.785f), new Vector2(1, 1), new Vector2(padX, 2), new Vector2(-padX, -28));
            FloatText("Section", h, new Vector2(0, 1), new Vector2(0, -20), new Vector2(720, 34), sideInspectMode ? 18 : 21,
                Accent, TextAnchor.MiddleLeft, true).text = section;
            var name = Rect("Title", h, new Vector2(0, 0.12f), new Vector2(1, 0.68f));
            FillText("Title text", name, titleSize, Ink, TextAnchor.MiddleLeft, true).text = title;
            Graphic(Rect("Header rule", panel, new Vector2(0, 0.782f), new Vector2(1, 0.783f), new Vector2(padX, 0), new Vector2(-padX, 0)), Frame, false);

            float shelf = Mathf.Min(0.47f, 0.055f + buttons.Length * (sideInspectMode ? 0.076f : 0.068f));
            float split = splitImage != null ? 0.63f : 1f;
            var bodyArea = Rect("Scrollable content", panel, new Vector2(0, shelf), new Vector2(split, 0.768f),
                new Vector2(padX, 11), new Vector2(-padX + 4, -12));
            var scroll = bodyArea.gameObject.AddComponent<ScrollRect>();
            readingScroll = scroll;
            Graphic(bodyArea, Color.clear, true);
            bodyArea.gameObject.AddComponent<RectMask2D>();
            var bodyRect = Rect("Page text", bodyArea, new Vector2(0, 1), new Vector2(1, 1));
            bodyRect.pivot = new Vector2(0.5f, 1);
            bodyRect.sizeDelta = new Vector2(0, 1);
            var layout = bodyRect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            var fit = bodyRect.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var article = FillText("Readable copy", bodyRect, sideInspectMode ? 21 : 23, Ink, TextAnchor.UpperLeft);
            article.text = sideInspectMode
                ? body + "\n\n<color=#a6b6a8>Kéo chuột hoặc WASD để xoay vật · Lăn chuột / Q-Z để thu phóng</color>"
                : body;
            article.supportRichText = true;
            article.lineSpacing = 1.18f;
            scroll.viewport = bodyArea;
            scroll.content = bodyRect;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 30;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            if (splitImage != null)
            {
                var media = Rect("Evidence photograph", panel, new Vector2(0.64f, shelf + 0.018f), new Vector2(1, 0.765f),
                    new Vector2(4, 0), new Vector2(-47, -10));
                var frame = Rect("Image within column", media, Vector2.zero, Vector2.one);
                var image = frame.gameObject.AddComponent<RawImage>();
                image.texture = splitImage;
                image.raycastTarget = false;
                var keep = frame.gameObject.AddComponent<AspectRatioFitter>();
                keep.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                keep.aspectRatio = (float)splitImage.width / splitImage.height;
            }

            var footer = Rect("Actions", panel, new Vector2(0, 0), new Vector2(1, shelf), new Vector2(padX, 22), new Vector2(-padX, -6));
            for (int i = 0; i < buttons.Length; i++)
            {
                float h0 = 1f - (i + 1f) / buttons.Length;
                float h1 = 1f - i / (float)buttons.Length;
                var row = Rect("Action " + i, footer, new Vector2(0, h0), new Vector2(1, h1), new Vector2(0, 3), new Vector2(0, -3));
                var spec = buttons[i];
                var btnBg = Graphic(row, new Color(0.145f, 0.205f, 0.175f), true);
                var button = row.gameObject.AddComponent<Button>();
                button.targetGraphic = btnBg;
                var colors = button.colors;
                colors.normalColor = new Color(0.76f, 0.83f, 0.73f);
                colors.selectedColor = new Color(0.94f, 0.99f, 0.78f);
                colors.highlightedColor = new Color(0.97f, 0.99f, 0.88f);
                colors.pressedColor = new Color(0.68f, 0.86f, 0.70f);
                colors.disabledColor = new Color(0.32f, 0.41f, 0.39f);
                button.colors = colors;
                var nav = button.navigation;
                nav.mode = Navigation.Mode.None;
                button.navigation = nav;
                button.interactable = spec.Enabled;
                if (spec.Enabled)
                {
                    button.onClick.AddListener(() => { if (!ModalInputBlocked) spec.Press?.Invoke(); });
                    focus.Add(button);
                }
                var label = Rect("Inset", row, Vector2.zero, Vector2.one, new Vector2(18, 1), new Vector2(-14, -1));
                FillText("Label", label, sideInspectMode ? 20 : 22, spec.Enabled ? Ink : Secondary, TextAnchor.MiddleLeft, true).text = spec.Label;
            }

            modal.gameObject.SetActive(true);
            modalOpenedFrame = Time.frameCount;
            hud.gameObject.SetActive(false);
            OpenChanged?.Invoke();
            MoveFocus(1);
        }

        public void Close()
        {
            if (!IsModal) return;
            var extra = onCloseExtra;
            onCloseExtra = null;
            extra?.Invoke();
            modal.gameObject.SetActive(false);
            hud.gameObject.SetActive(true);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
            dismiss = null;
            OpenChanged?.Invoke();
        }

        public void Fade(float opacity, Color? tint = null)
        {
            fade.gameObject.SetActive(opacity > 0.001f);
            Color baseCol = tint ?? new Color(0.025f, 0.04f, 0.037f, 1f);
            fade.color = new Color(baseCol.r, baseCol.g, baseCol.b, opacity);
        }

        private void Update()
        {
            if (cueBacking != null && Time.unscaledTime > cueUntil) cueBacking.gameObject.SetActive(false);
            if (toastBacking != null && Time.unscaledTime > toastUntil) toastBacking.gameObject.SetActive(false);
            if (modal == null || !IsModal || ModalInputBlocked || modalOpenedFrame == Time.frameCount) return;
            HandleNavigation(Keyboard.current);
        }

        internal void HandleNavigation(Keyboard k)
        {
            if (k == null) return;
            if (k.escapeKey.wasPressedThisFrame)
            {
                dismiss?.Invoke();
                return;
            }
            bool reverse = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed || k.upArrowKey.wasPressedThisFrame;
            if (k.tabKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame)
                MoveFocus(reverse ? -1 : 1);
            if (readingScroll != null && (k.pageDownKey.wasPressedThisFrame || k.pageUpKey.wasPressedThisFrame))
                readingScroll.verticalNormalizedPosition = Mathf.Clamp01(readingScroll.verticalNormalizedPosition +
                    (k.pageUpKey.wasPressedThisFrame ? 0.28f : -0.28f));
            if ((k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame) && selected >= 0 && selected < focus.Count)
                focus[selected].onClick.Invoke();
        }

        private void MoveFocus(int offset)
        {
            if (focus.Count == 0) return;
            selected = (selected + offset + focus.Count) % focus.Count;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(focus[selected].gameObject);
        }

        private int Scaled(int size) => Mathf.RoundToInt(size * scale);

        private Text FillText(string label, RectTransform parent, int size, Color color, TextAnchor aligning, bool bold = false)
            => MakeText(Rect(label, parent, Vector2.zero, Vector2.one), size, color, aligning, bold);

        private Text FloatText(string label, RectTransform parent, Vector2 anchor, Vector2 offset, Vector2 size,
            int fontSize, Color color, TextAnchor aligning, bool bold = false)
            => MakeText(Fixed(label, parent, anchor, offset, size), fontSize, color, aligning, bold);

        private Text MakeText(RectTransform rect, int size, Color color, TextAnchor aligning, bool bold)
        {
            var text = rect.gameObject.AddComponent<Text>();
            text.font = bold ? heading : standard;
            text.fontSize = Scaled(size);
            text.color = color;
            text.alignment = aligning;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform Fixed(string name, RectTransform parent, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            var rect = Rect(name, parent, anchor, anchor);
            rect.pivot = new Vector2(0, 0.5f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
            return rect;
        }

        private static RectTransform Rect(string name, RectTransform parent, Vector2 low, Vector2 high,
            Vector2 insetMin = default, Vector2 insetMax = default)
        {
            var child = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            child.SetParent(parent, false);
            child.anchorMin = low;
            child.anchorMax = high;
            child.offsetMin = insetMin;
            child.offsetMax = insetMax;
            return child;
        }

        private static Image Graphic(RectTransform rect, Color color, bool hit)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = hit;
            return image;
        }
    }
}
