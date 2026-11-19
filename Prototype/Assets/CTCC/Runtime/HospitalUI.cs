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
        public ActionChoice(string label,Action press,bool enabled=true)
        {
            Label=label;
            Press=press;
            Enabled=enabled;
        }
    }

    public sealed class HospitalUI : MonoBehaviour
    {
        private static readonly Color Ink = new(.90f,.91f,.83f);
        private static readonly Color Secondary = new(.74f,.80f,.74f);
        private static readonly Color Accent = new(.78f,.84f,.65f);
        private static readonly Color Frame = new(.43f,.52f,.43f);
        private static readonly Color ScreenDark = new(.065f,.10f,.093f,.97f);
        private readonly List<Button> focus = new();
        private Font standard, heading;
        private RectTransform canvas, hud, modal, panel;
        private Image fade, cueBacking, promptBacking;
        private Text place, task, prompt, cue, help;
        private Action dismiss;
        private ScrollRect readingScroll;
        private float cueUntil;
        private float scale = 1;
        private bool captions=true;
        private int selected;
        public bool IsModal => modal.gameObject.activeSelf;
        public event Action OpenChanged;

        public void Initialize()
        {
            standard=Resources.Load<Font>("CTCC/BeVietnamPro-Regular");
            heading=Resources.Load<Font>("CTCC/BeVietnamPro-SemiBold");
            if (standard==null || heading==null)
                throw new InvalidOperationException("Vietnamese UI typefaces are missing.");
            var root=new GameObject("CTCC · Interface",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            root.transform.SetParent(transform,false);
            var c=root.GetComponent<Canvas>();
            c.renderMode=RenderMode.ScreenSpaceOverlay;
            c.sortingOrder=12;
            var scaler=root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);
            scaler.matchWidthOrHeight=.65f;
            canvas=root.GetComponent<RectTransform>();

            var events=new GameObject("Interface navigation",typeof(EventSystem),typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform,false);
            var module=events.GetComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
            module.move=null;
            module.submit=null;
            module.cancel=null;

            hud=Rect("Play controls",canvas,Vector2.zero,Vector2.one);
            place=FloatText("Current reality",hud,new Vector2(0,1),new Vector2(54,-53),new Vector2(530,39),22,
                Accent,TextAnchor.UpperLeft,true);
            task=FloatText("Objective",hud,new Vector2(0,1),new Vector2(54,-107),new Vector2(610,78),24,
                Ink,TextAnchor.MiddleLeft,true);
            help=FloatText("Controls",hud,new Vector2(0,0),new Vector2(54,27),new Vector2(960,39),19,
                Secondary,TextAnchor.MiddleLeft);
            var dot=Fixed("Aim",hud,new Vector2(.5f,.5f),Vector2.zero,new Vector2(5,5));
            dot.pivot=new Vector2(.5f,.5f);
            Graphic(dot,new Color(.96f,.93f,.77f,.77f),false);
            var tag=Fixed("Interaction",hud,new Vector2(.5f,.5f),new Vector2(0,-78),new Vector2(700,49));
            tag.pivot=new Vector2(.5f,.5f);
            promptBacking=Graphic(tag,new Color(.055f,.082f,.069f,.86f),false);
            prompt=FillText("Prompt",tag,21,Ink,TextAnchor.MiddleCenter,true);
            var subtitle=Fixed("Caption",hud,new Vector2(.5f,.11f),Vector2.zero,new Vector2(975,59));
            subtitle.pivot=new Vector2(.5f,.5f);
            cueBacking=Graphic(subtitle,new Color(.05f,.075f,.068f,.91f),false);
            cue=FillText("Subtitle",subtitle,22,Ink,TextAnchor.MiddleCenter);
            cueBacking.gameObject.SetActive(false);

            modal=Rect("Reading",canvas,Vector2.zero,Vector2.one);
            Graphic(modal,new Color(.014f,.023f,.019f,.74f),true);
            panel=Rect("Reading page",modal,new Vector2(.195f,.092f),new Vector2(.805f,.908f));
            Graphic(panel,ScreenDark,true);
            modal.gameObject.SetActive(false);

            fade=Graphic(Rect("Reality transition",canvas,Vector2.zero,Vector2.one),Color.clear,true);
            fade.gameObject.SetActive(false);
        }

        public void Apply(VisitSettings options)
        {
            scale=Mathf.Clamp(options.textScale,.85f,1.35f);
            captions=options.captions;
            place.fontSize=Scaled(22);
            task.fontSize=Scaled(24);
            help.fontSize=Scaled(19);
            prompt.fontSize=Scaled(21);
            cue.fontSize=Scaled(22);
            help.text=options.alternateBindings ? "Mũi tên   Đi     K   Tương tác     J   Sổ trực     Esc   Menu" :
                "W A S D   Đi     E   Tương tác     Tab   Sổ trực     Esc   Menu";
        }

        public void Hud(string reality,string objective)
        {
            place.text=reality;
            task.text=objective;
        }

        public void Prompt(string text)
        {
            promptBacking.gameObject.SetActive(!string.IsNullOrEmpty(text));
            prompt.text=text ?? "";
        }

        public void Caption(string text,float seconds=4,bool essential=false)
        {
            if (!captions && !essential) return;
            cue.text=text;
            cueUntil=Time.unscaledTime+seconds;
            cueBacking.gameObject.SetActive(true);
        }

        public void Show(string section,string title,string body,ActionChoice[] buttons,Action close,Texture splitImage=null)
        {
            for(int i=panel.childCount-1;i>=0;i--)
            {
                panel.GetChild(i).gameObject.SetActive(false);
                Destroy(panel.GetChild(i).gameObject);
            }
            dismiss=close;
            focus.Clear();
            selected=-1;
            var h=Rect("Document header",panel,new Vector2(0,.775f),new Vector2(1,1),new Vector2(49,2),new Vector2(-49,-32));
            FloatText("Section",h,new Vector2(0,1),new Vector2(0,-24),new Vector2(800,39),22,Accent,TextAnchor.MiddleLeft,true).text=section;
            var name=Rect("Title",h,new Vector2(0,.15f),new Vector2(1,.67f));
            FillText("Title text",name,49,Ink,TextAnchor.MiddleLeft,true).text=title;
            Graphic(Rect("Header rule",panel,new Vector2(0,.772f),new Vector2(1,.773f),new Vector2(49,0),new Vector2(-49,0)),Frame,false);

            float shelf=Mathf.Min(.47f,.045f+buttons.Length*.068f);
            var split=splitImage!=null ? .63f : 1f;
            var bodyArea=Rect("Scrollable content",panel,new Vector2(0,shelf),new Vector2(split,.758f),
                new Vector2(49,11),new Vector2(-46,-13));
            var scroll=bodyArea.gameObject.AddComponent<ScrollRect>();
            readingScroll=scroll;
            Graphic(bodyArea,Color.clear,true);
            bodyArea.gameObject.AddComponent<RectMask2D>();
            var bodyRect=Rect("Page text",bodyArea,new Vector2(0,1),new Vector2(1,1));
            bodyRect.pivot=new Vector2(.5f,1);
            bodyRect.sizeDelta=new Vector2(0,1);
            var layout=bodyRect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandHeight=false;
            layout.childForceExpandWidth=true;
            layout.childControlWidth=true;
            layout.childControlHeight=true;
            var fit=bodyRect.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var article=FillText("Readable copy",bodyRect,24,Ink,TextAnchor.UpperLeft);
            article.text=body;
            article.lineSpacing=1.16f;
            scroll.viewport=bodyArea;
            scroll.content=bodyRect;
            scroll.horizontal=false;
            scroll.scrollSensitivity=30;
            scroll.movementType=ScrollRect.MovementType.Clamped;
            if (splitImage!=null)
            {
                var media=Rect("Evidence photograph",panel,new Vector2(.64f,shelf+.018f),new Vector2(1,.755f),
                    new Vector2(4,0),new Vector2(-47,-10));
                var frame=Rect("Image within column",media,Vector2.zero,Vector2.one);
                var image=frame.gameObject.AddComponent<RawImage>();
                image.texture=splitImage;
                image.raycastTarget=false;
                var keep=frame.gameObject.AddComponent<AspectRatioFitter>();
                keep.aspectMode=AspectRatioFitter.AspectMode.FitInParent;
                keep.aspectRatio=(float)splitImage.width/splitImage.height;
            }

            var footer=Rect("Actions",panel,new Vector2(0,0),new Vector2(1,shelf),new Vector2(49,24),new Vector2(-49,-6));
            for(int i=0;i<buttons.Length;i++)
            {
                float h0=1-(i+1f)/buttons.Length;
                float h1=1-i/(float)buttons.Length;
                var row=Rect("Action "+i,footer,new Vector2(0,h0),new Vector2(1,h1),new Vector2(0,3),new Vector2(0,-3));
                var spec=buttons[i];
                var frame=Graphic(row,new Color(.16f,.224f,.188f),true);
                var button=row.gameObject.AddComponent<Button>();
                button.targetGraphic=frame;
                var colors=button.colors;
                colors.normalColor=new Color(.73f,.81f,.70f);
                colors.selectedColor=new Color(.93f,.99f,.77f);
                colors.highlightedColor=new Color(.97f,.99f,.88f);
                colors.pressedColor=new Color(.68f,.86f,.70f);
                colors.disabledColor=new Color(.32f,.41f,.39f);
                button.colors=colors;
                var nav=button.navigation;
                nav.mode=Navigation.Mode.None;
                button.navigation=nav;
                button.interactable=spec.Enabled;
                if (spec.Enabled)
                {
                    button.onClick.AddListener(()=>spec.Press?.Invoke());
                    focus.Add(button);
                }
                var label=Rect("Inset",row,Vector2.zero,Vector2.one,new Vector2(19,1),new Vector2(-16,-1));
                FillText("Label",label,22,spec.Enabled ? Ink : Secondary,TextAnchor.MiddleLeft,true).text=spec.Label;
            }
            modal.gameObject.SetActive(true);
            hud.gameObject.SetActive(false);
            OpenChanged?.Invoke();
            MoveFocus(1);
        }

        public void Close()
        {
            if (!IsModal) return;
            modal.gameObject.SetActive(false);
            hud.gameObject.SetActive(true);
            EventSystem.current.SetSelectedGameObject(null);
            dismiss=null;
            OpenChanged?.Invoke();
        }

        public void Fade(float opacity)
        {
            fade.gameObject.SetActive(opacity>.001f);
            fade.color=new Color(.025f,.04f,.037f,opacity);
        }

        private void Update()
        {
            if(cueBacking!=null && Time.unscaledTime>cueUntil) cueBacking.gameObject.SetActive(false);
            if(modal==null || !IsModal) return;
            HandleNavigation(Keyboard.current);
        }

        internal void HandleNavigation(Keyboard k)
        {
            if(k==null) return;
            if(k.escapeKey.wasPressedThisFrame)
            {
                dismiss?.Invoke();
                return;
            }
            bool reverse=k.leftShiftKey.isPressed || k.rightShiftKey.isPressed || k.upArrowKey.wasPressedThisFrame;
            if(k.tabKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame)
                MoveFocus(reverse?-1:1);
            if(readingScroll!=null && (k.pageDownKey.wasPressedThisFrame || k.pageUpKey.wasPressedThisFrame))
                readingScroll.verticalNormalizedPosition=Mathf.Clamp01(readingScroll.verticalNormalizedPosition+
                    (k.pageUpKey.wasPressedThisFrame?.28f:-.28f));
            if((k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame) && selected>=0 && selected<focus.Count)
                focus[selected].onClick.Invoke();
        }

        private void MoveFocus(int offset)
        {
            if(focus.Count==0) return;
            selected=(selected+offset+focus.Count)%focus.Count;
            EventSystem.current.SetSelectedGameObject(focus[selected].gameObject);
        }

        private int Scaled(int size) => Mathf.RoundToInt(size*scale);

        private Text FillText(string label,RectTransform parent,int size,Color color,TextAnchor aligning,bool bold=false)
        {
            return MakeText(Rect(label,parent,Vector2.zero,Vector2.one),size,color,aligning,bold);
        }

        private Text FloatText(string label,RectTransform parent,Vector2 anchor,Vector2 offset,Vector2 size,
            int fontSize,Color color,TextAnchor aligning,bool bold=false)
        {
            return MakeText(Fixed(label,parent,anchor,offset,size),fontSize,color,aligning,bold);
        }

        private Text MakeText(RectTransform rect,int size,Color color,TextAnchor aligning,bool bold)
        {
            var text=rect.gameObject.AddComponent<Text>();
            text.font=bold?heading:standard;
            text.fontSize=Scaled(size);
            text.color=color;
            text.alignment=aligning;
            text.horizontalOverflow=HorizontalWrapMode.Wrap;
            text.verticalOverflow=VerticalWrapMode.Overflow;
            text.raycastTarget=false;
            return text;
        }

        private static RectTransform Fixed(string name,RectTransform parent,Vector2 anchor,Vector2 offset,Vector2 size)
        {
            var rect=Rect(name,parent,anchor,anchor);
            rect.pivot=new Vector2(0,.5f);
            rect.anchoredPosition=offset;
            rect.sizeDelta=size;
            return rect;
        }

        private static RectTransform Rect(string name,RectTransform parent,Vector2 low,Vector2 high,
            Vector2 insetMin=default,Vector2 insetMax=default)
        {
            var child=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            child.SetParent(parent,false);
            child.anchorMin=low;
            child.anchorMax=high;
            child.offsetMin=insetMin;
            child.offsetMax=insetMax;
            return child;
        }

        private static Image Graphic(RectTransform rect,Color color,bool hit)
        {
            var image=rect.gameObject.AddComponent<Image>();
            image.color=color;
            image.raycastTarget=hit;
            return image;
        }
    }
}
