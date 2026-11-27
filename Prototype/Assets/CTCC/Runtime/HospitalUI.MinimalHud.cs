using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CTCC
{
    public enum WorldMarkerKind
    {
        None = 0,
        Interact = 1,
        Drag = 2,
        Clue = 3,
        FarApproach = 4
    }

    public readonly struct ProjectedHotspot3D
    {
        public readonly string Id;
        public readonly string Label;
        public readonly Vector2 ScreenPos;
        public readonly bool Hovered;
        public readonly bool Marked;
        public readonly bool Interactive;

        public ProjectedHotspot3D(string id, string label, Vector2 screenPos, bool hovered, bool marked, bool interactive = true)
        {
            Id = id;
            Label = label;
            ScreenPos = screenPos;
            Hovered = hovered;
            Marked = marked;
            Interactive = interactive;
        }
    }

    /// <summary>
    /// Phase 2.5 Minimal Contextual HUD & Standalone 0-Sidebar 3D Inspect Overlay:
    /// - Replaces the giant near-center 760x54 prompt box & permanent controls/objective banners with a crisp world-projected
    ///   diegetic dot/ring/question-mark marker (100% LOS-occluded via SceneTarget.VisibleFrom, never spoily from afar).
    /// - Provides a clean full-screen 0-sidebar overlay for Physical Inspector 2.0, Diegetic Phone 3D, Envelope 3D & AN02 Clipboard,
    ///   with reusable pooled 3D-projected hotspot markers and ultra-minimal opt-in accessibility micro-chips.
    /// </summary>
    public sealed partial class HospitalUI
    {
        private Sprite circleDotSprite;
        private Sprite ringSprite;
        private CanvasGroup objectiveGroup;
        private float objectivePulseUntil;
        private string lastObjectiveKey;

        // World-projected diegetic marker
        private RectTransform worldMarkerRoot;
        private Image worldMarkerRing;
        private Image worldMarkerDot;
        private Text worldMarkerGlyph;

        // Minimal contextual bottom whisper (only when genuinely needed: locked/wrong key/drag/secondary Q/Large Print)
        private Image whisperBacking;
        private Text whisperText;

        // Standalone 0-sidebar 3D overlay container (for Physical Inspector 2.0, Phone 3D, Envelope 3D, AN02 3D)
        private RectTransform standaloneRoot;
        private Text standaloneHintText;
        private Image standaloneHoverPill;
        private Text standaloneHoverText;
        private RectTransform standaloneChipBar;
        private readonly List<GameObject> standaloneChips = new();
        private readonly List<(RectTransform root, Image ring, Image dot)> hotspotPool = new();
        private bool standaloneOverlayActive;
        private Action onStandaloneLargePrint;
        private Action onStandaloneSecondary;

        public bool IsStandaloneOverlay => standaloneOverlayActive && IsModal;
        internal bool WorldMarkerVisible => worldMarkerRoot != null && worldMarkerRoot.gameObject.activeSelf;
        internal WorldMarkerKind ActiveWorldMarkerKind { get; private set; }
        internal bool LegacyCenterPromptVisible => promptBacking != null && promptBacking.gameObject.activeSelf;

        private void InitMinimalHudElements()
        {
            circleDotSprite = BuildCircleSprite(48, filled: true);
            ringSprite = BuildCircleSprite(64, filled: false);

            // Soften center idle aim into a crisp anti-aliased micro-dot
            if (reticleImg != null)
            {
                reticleImg.sprite = circleDotSprite;
                reticleRect.sizeDelta = new Vector2(5f, 5f);
                reticleImg.color = new Color(0.95f, 0.92f, 0.78f, 0.34f);
            }

            // Hide legacy giant 760x54 central prompt box & permanent controls legend by default!
            if (promptBacking != null) promptBacking.gameObject.SetActive(false);
            if (help != null) help.gameObject.SetActive(false);

            // Convert top-left objective into a brief subtle fade-out pulse (never permanently cluttering exploration!)
            if (place != null && place.transform.parent != null)
            {
                var objBg = hud.Find("ObjectiveBacking");
                if (objBg != null)
                {
                    objectiveGroup = objBg.gameObject.AddComponent<CanvasGroup>();
                    objectiveGroup.alpha = 0f;
                    // Reparent place & task inside ObjectiveBacking so they fade cleanly together
                    place.transform.SetParent(objBg, true);
                    task.transform.SetParent(objBg, true);
                }
            }

            // World-projected diegetic marker (dot / hollow ring / drag chevron / '?' clue glyph)
            worldMarkerRoot = new GameObject("WorldDiegeticMarker", typeof(RectTransform)).GetComponent<RectTransform>();
            worldMarkerRoot.SetParent(hud, false);
            worldMarkerRoot.anchorMin = worldMarkerRoot.anchorMax = Vector2.zero;
            worldMarkerRoot.pivot = new Vector2(0.5f, 0.5f);
            worldMarkerRoot.sizeDelta = new Vector2(28f, 28f);

            var ringGo = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            ringGo.transform.SetParent(worldMarkerRoot, false);
            var ringRt = ringGo.GetComponent<RectTransform>();
            ringRt.anchorMin = Vector2.zero; ringRt.anchorMax = Vector2.one;
            ringRt.offsetMin = Vector2.zero; ringRt.offsetMax = Vector2.zero;
            worldMarkerRing = ringGo.GetComponent<Image>();
            worldMarkerRing.sprite = ringSprite;
            worldMarkerRing.raycastTarget = false;
            worldMarkerRing.color = new Color(0.94f, 0.92f, 0.80f, 0.88f);

            var dotGo = new GameObject("Dot", typeof(RectTransform), typeof(Image));
            dotGo.transform.SetParent(worldMarkerRoot, false);
            var dotRt = dotGo.GetComponent<RectTransform>();
            dotRt.anchorMin = dotRt.anchorMax = new Vector2(0.5f, 0.5f);
            dotRt.sizeDelta = new Vector2(6f, 6f);
            worldMarkerDot = dotGo.GetComponent<Image>();
            worldMarkerDot.sprite = circleDotSprite;
            worldMarkerDot.raycastTarget = false;
            worldMarkerDot.color = new Color(0.96f, 0.94f, 0.84f, 0.95f);

            worldMarkerGlyph = MakeText(Rect("Glyph", worldMarkerRoot, Vector2.zero, Vector2.one),
                15, new Color(0.96f, 0.94f, 0.82f, 0.95f), TextAnchor.MiddleCenter, bold: true);
            worldMarkerGlyph.gameObject.SetActive(false);
            worldMarkerRoot.gameObject.SetActive(false);

            // Minimal bottom contextual whisper (ONLY shown when genuinely needed: locked/wrong key/drag/Q-action/LargePrint)
            var wRect = Fixed("MinimalWhisper", hud, new Vector2(0.5f, 0.046f), Vector2.zero, new Vector2(680f, 38f));
            wRect.pivot = new Vector2(0.5f, 0.5f);
            whisperBacking = Graphic(wRect, new Color(0.032f, 0.048f, 0.044f, 0.64f), false);
            whisperText = FillText("WhisperText", Rect("WhisperInset", wRect, Vector2.zero, Vector2.one, new Vector2(14, 2), new Vector2(-14, -2)),
                17, new Color(0.92f, 0.92f, 0.86f, 0.95f), TextAnchor.MiddleCenter, false);
            whisperBacking.gameObject.SetActive(false);

            // Standalone 0-sidebar 3D inspection overlay (inside modal, leaves 100% viewport unobstructed!)
            standaloneRoot = Rect("Standalone3DOverlay", modal, Vector2.zero, Vector2.one);
            standaloneHintText = MakeText(Rect("BottomWhisper", standaloneRoot, new Vector2(0.10f, 0.014f), new Vector2(0.90f, 0.056f)),
                15, new Color(0.86f, 0.88f, 0.82f, 0.78f), TextAnchor.MiddleCenter, false);
            var hShadow = standaloneHintText.gameObject.AddComponent<Shadow>();
            hShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            hShadow.effectDistance = new Vector2(1f, -1f);

            var hoverBox = Fixed("HotspotHoverPill", standaloneRoot, Vector2.zero, Vector2.zero, new Vector2(260f, 32f));
            hoverBox.pivot = new Vector2(0.5f, 0f);
            standaloneHoverPill = Graphic(hoverBox, new Color(0.038f, 0.054f, 0.050f, 0.84f), false);
            standaloneHoverText = FillText("HoverLabel", Rect("Inset", hoverBox, Vector2.zero, Vector2.one, new Vector2(10, 2), new Vector2(-10, -2)),
                15, Ink, TextAnchor.MiddleCenter, true);
            standaloneHoverPill.gameObject.SetActive(false);

            standaloneChipBar = Rect("MicroChipBar", standaloneRoot, new Vector2(0.56f, 0.925f), new Vector2(0.975f, 0.978f));
            var chipLayout = standaloneChipBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            chipLayout.childAlignment = TextAnchor.MiddleRight;
            chipLayout.spacing = 8f;
            chipLayout.childControlWidth = false;
            chipLayout.childControlHeight = true;
            chipLayout.childForceExpandWidth = false;
            chipLayout.childForceExpandHeight = false;
            standaloneRoot.gameObject.SetActive(false);
        }

        private void TickMinimalHud()
        {
            if (objectiveGroup != null)
            {
                float targetAlpha = Time.unscaledTime < objectivePulseUntil ? 0.88f : 0f;
                objectiveGroup.alpha = Mathf.MoveTowards(objectiveGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 1.4f);
            }
            if (standaloneOverlayActive && IsModal && !ModalInputBlocked && modalOpenedFrame != Time.frameCount)
            {
                var keys = UnityEngine.InputSystem.Keyboard.current;
                if (keys != null)
                {
                    if (keys.lKey.wasPressedThisFrame && onStandaloneLargePrint != null)
                        onStandaloneLargePrint.Invoke();
                    else if (keys.gKey.wasPressedThisFrame && onStandaloneSecondary != null)
                        onStandaloneSecondary.Invoke();
                }
            }
        }

        internal void PulseObjectiveMinimal(string reality, string objective)
        {
            if (place != null) place.text = reality ?? "";
            if (task != null) task.text = objective ?? "";
            string key = (reality ?? "") + "|" + (objective ?? "");
            if (key != lastObjectiveKey && !string.IsNullOrEmpty(objective))
            {
                lastObjectiveKey = key;
                objectivePulseUntil = Time.unscaledTime + 4.5f;
            }
        }

        /// <summary>
        /// Phase 2.5 Minimal Contextual HUD: projects a subtle diegetic marker directly onto the LOS-verified focused target.
        /// Never renders through walls, never reveals clue titles from afar, and hides the old 760x54 center prompt box!
        /// </summary>
        public void UpdateWorldMarker(Camera cam, SceneTarget target, SceneTarget beyondReach,
            WorldMarkerKind kind, string minimalWhisper = null)
        {
            if (promptBacking != null && promptBacking.gameObject.activeSelf)
                promptBacking.gameObject.SetActive(false);

            ActiveWorldMarkerKind = kind;
            bool showReticleDot = target == null;
            if (reticleRect != null)
            {
                reticleRect.sizeDelta = new Vector2(4.5f, 4.5f);
                reticleRect.localRotation = Quaternion.identity;
                reticleImg.color = new Color(0.95f, 0.92f, 0.78f, showReticleDot ? 0.34f : 0.14f);
            }

            if (worldMarkerRoot != null)
            {
                SceneTarget anchor = target ?? beyondReach;
                if (anchor != null && cam != null && kind != WorldMarkerKind.None)
                {
                    Vector3 worldPt = anchor.handhold != null ? anchor.Contact : anchor.transform.position;
                    Vector3 sp = cam.WorldToScreenPoint(worldPt);
                    if (sp.z > 0.05f && sp.x >= 24f && sp.x <= Screen.width - 24f && sp.y >= 24f && sp.y <= Screen.height - 24f)
                    {
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(hud, sp, null, out Vector2 localPt);
                        worldMarkerRoot.anchoredPosition = localPt + new Vector2(hud.rect.width * 0.5f, hud.rect.height * 0.5f);
                        bool far = kind == WorldMarkerKind.FarApproach;
                        float sz = far ? 10f : (kind == WorldMarkerKind.Clue ? 26f : 22f);
                        worldMarkerRoot.sizeDelta = new Vector2(sz, sz);
                        worldMarkerRing.enabled = !far;
                        worldMarkerRing.color = kind switch
                        {
                            WorldMarkerKind.Drag => new Color(0.96f, 0.90f, 0.68f, 0.90f),
                            WorldMarkerKind.Clue => new Color(0.95f, 0.93f, 0.76f, 0.92f),
                            _ => new Color(0.94f, 0.93f, 0.84f, 0.86f)
                        };
                        worldMarkerDot.enabled = far || kind == WorldMarkerKind.Interact;
                        worldMarkerDot.color = far
                            ? new Color(0.90f, 0.88f, 0.78f, 0.42f)
                            : new Color(0.96f, 0.95f, 0.86f, 0.95f);

                        if (kind == WorldMarkerKind.Clue)
                        {
                            worldMarkerGlyph.text = "?";
                            worldMarkerGlyph.fontSize = Scaled(14);
                            worldMarkerGlyph.gameObject.SetActive(true);
                        }
                        else if (kind == WorldMarkerKind.Drag)
                        {
                            worldMarkerGlyph.text = "↔";
                            worldMarkerGlyph.fontSize = Scaled(13);
                            worldMarkerGlyph.gameObject.SetActive(true);
                        }
                        else
                        {
                            worldMarkerGlyph.gameObject.SetActive(false);
                        }
                        worldMarkerRoot.gameObject.SetActive(true);
                    }
                    else
                    {
                        worldMarkerRoot.gameObject.SetActive(false);
                    }
                }
                else
                {
                    worldMarkerRoot.gameObject.SetActive(false);
                }
            }

            // Minimal bottom contextual whisper ONLY when genuinely needed (drag / secondary Q / Large-Print accessibility)
            if (whisperBacking != null)
            {
                bool showWhisper = !string.IsNullOrEmpty(minimalWhisper);
                whisperBacking.gameObject.SetActive(showWhisper);
                if (showWhisper)
                {
                    whisperText.fontSize = Scaled(17);
                    whisperText.text = minimalWhisper;
                }
            }
        }

        /// <summary>
        /// Opens a 100% standalone 0-sidebar 3D inspection/device overlay (Physical Inspector 2.0, Diegetic Phone 3D,
        /// Physical Envelope 3D, Diegetic AN02 Clipboard). Hides the 2D modal panel completely!
        /// </summary>
        public void ShowStandaloneOverlay(
            string bottomWhisper,
            Action close,
            Action onClose = null,
            ActionChoice[] microChips = null,
            Action onLargePrintKey = null,
            Action onSecondaryKey = null,
            float backdropAlpha = 0.14f)
        {
            ClearModalPanel();
            standaloneOverlayActive = true;
            dismiss = close;
            onCloseExtra = onClose;
            onStandaloneLargePrint = onLargePrintKey;
            onStandaloneSecondary = onSecondaryKey;
            focus.Clear();
            selected = -1;

            modalBackdrop.color = new Color(0.010f, 0.015f, 0.014f, backdropAlpha);
            modalBackdrop.raycastTarget = false;
            panel.gameObject.SetActive(false);

            standaloneHintText.fontSize = Scaled(15);
            standaloneHintText.text = bottomWhisper ?? "";
            standaloneHoverPill.gameObject.SetActive(false);
            Hide3DHotspotMarkers();
            RebuildStandaloneChips(microChips);

            standaloneRoot.gameObject.SetActive(true);
            bool wasModal = IsModal;
            modal.gameObject.SetActive(true);
            modalOpenedFrame = Time.frameCount;
            hud.gameObject.SetActive(false);
            if (!wasModal) OpenChanged?.Invoke();
        }

        public void SetStandaloneBottomWhisper(string text)
        {
            if (standaloneHintText != null)
                standaloneHintText.text = text ?? "";
        }

        public void Update3DHotspotMarkers(IReadOnlyList<ProjectedHotspot3D> hotspots)
        {
            if (standaloneRoot == null || !standaloneRoot.gameObject.activeSelf)
                return;
            int count = hotspots != null ? hotspots.Count : 0;
            while (hotspotPool.Count < count)
            {
                var rt = new GameObject("Hotspot3D_" + hotspotPool.Count, typeof(RectTransform)).GetComponent<RectTransform>();
                rt.SetParent(standaloneRoot, false);
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(24f, 24f);
                var rGo = new GameObject("Ring", typeof(RectTransform), typeof(Image));
                rGo.transform.SetParent(rt, false);
                var rRt = rGo.GetComponent<RectTransform>();
                rRt.anchorMin = Vector2.zero; rRt.anchorMax = Vector2.one;
                rRt.offsetMin = Vector2.zero; rRt.offsetMax = Vector2.zero;
                var ring = rGo.GetComponent<Image>();
                ring.sprite = ringSprite;
                ring.raycastTarget = false;
                var dGo = new GameObject("Dot", typeof(RectTransform), typeof(Image));
                dGo.transform.SetParent(rt, false);
                var dRt = dGo.GetComponent<RectTransform>();
                dRt.anchorMin = dRt.anchorMax = new Vector2(0.5f, 0.5f);
                dRt.sizeDelta = new Vector2(7f, 7f);
                var dot = dGo.GetComponent<Image>();
                dot.sprite = circleDotSprite;
                dot.raycastTarget = false;
                rt.gameObject.SetActive(false);
                hotspotPool.Add((rt, ring, dot));
            }

            ProjectedHotspot3D? hovered = null;
            for (int i = 0; i < hotspotPool.Count; i++)
            {
                var slot = hotspotPool[i];
                if (i >= count)
                {
                    slot.root.gameObject.SetActive(false);
                    continue;
                }
                var hs = hotspots[i];
                RectTransformUtility.ScreenPointToLocalPointInRectangle(standaloneRoot, hs.ScreenPos, null, out Vector2 lp);
                slot.root.anchoredPosition = lp + new Vector2(standaloneRoot.rect.width * 0.5f, standaloneRoot.rect.height * 0.5f);
                float size = hs.Hovered ? 28f : 19f;
                slot.root.sizeDelta = new Vector2(size, size);
                slot.ring.color = hs.Marked
                    ? new Color(0.86f, 0.34f, 0.28f, hs.Hovered ? 0.96f : 0.78f)
                    : hs.Hovered
                        ? new Color(0.98f, 0.94f, 0.72f, 0.96f)
                        : new Color(0.92f, 0.90f, 0.78f, 0.62f);
                slot.dot.color = hs.Marked
                    ? new Color(0.94f, 0.42f, 0.36f, 0.95f)
                    : new Color(0.98f, 0.96f, 0.84f, hs.Hovered ? 0.96f : 0.70f);
                slot.root.gameObject.SetActive(true);
                if (hs.Hovered && !string.IsNullOrEmpty(hs.Label)) hovered = hs;
            }

            if (hovered.HasValue)
            {
                var h = hovered.Value;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(standaloneRoot, h.ScreenPos, null, out Vector2 lp);
                var pillRt = standaloneHoverPill.rectTransform;
                float px = Mathf.Clamp(lp.x + standaloneRoot.rect.width * 0.5f, 150f, standaloneRoot.rect.width - 150f);
                float py = Mathf.Clamp(lp.y + standaloneRoot.rect.height * 0.5f + 22f, 70f, standaloneRoot.rect.height - 64f);
                pillRt.anchoredPosition = new Vector2(px, py);
                standaloneHoverText.fontSize = Scaled(15);
                standaloneHoverText.text = h.Label;
                float prefW = Mathf.Clamp(h.Label.Length * 9.5f + 34f, 120f, 390f);
                pillRt.sizeDelta = new Vector2(prefW, 32f);
                standaloneHoverPill.gameObject.SetActive(true);
            }
            else
            {
                standaloneHoverPill.gameObject.SetActive(false);
            }
        }

        private void Hide3DHotspotMarkers()
        {
            for (int i = 0; i < hotspotPool.Count; i++)
                hotspotPool[i].root.gameObject.SetActive(false);
            if (standaloneHoverPill != null) standaloneHoverPill.gameObject.SetActive(false);
        }

        private void RebuildStandaloneChips(ActionChoice[] microChips)
        {
            for (int i = 0; i < standaloneChips.Count; i++)
                if (standaloneChips[i] != null) Destroy(standaloneChips[i]);
            standaloneChips.Clear();
            if (microChips == null || microChips.Length == 0) return;
            for (int i = 0; i < microChips.Length; i++)
            {
                var spec = microChips[i];
                var go = new GameObject("MicroChip_" + i, typeof(RectTransform));
                go.transform.SetParent(standaloneChipBar, false);
                var rt = go.GetComponent<RectTransform>();
                float width = Mathf.Clamp(spec.Label.Length * 9.5f + 28f, 82f, 260f);
                rt.sizeDelta = new Vector2(width, 34f);
                var bg = Graphic(rt, new Color(0.042f, 0.062f, 0.056f, 0.78f), true);
                var btn = go.AddComponent<Button>();
                btn.targetGraphic = bg;
                var cols = btn.colors;
                cols.normalColor = new Color(0.84f, 0.88f, 0.82f);
                cols.highlightedColor = new Color(0.98f, 0.99f, 0.86f);
                cols.pressedColor = new Color(0.72f, 0.84f, 0.72f);
                btn.colors = cols;
                var nav = btn.navigation;
                nav.mode = Navigation.Mode.None;
                btn.navigation = nav;
                btn.interactable = spec.Enabled;
                if (spec.Enabled)
                {
                    btn.onClick.AddListener(() => { if (!ModalInputBlocked) spec.Press?.Invoke(); });
                    focus.Add(btn);
                }
                FillText("ChipLabel", Rect("Inset", rt, Vector2.zero, Vector2.one, new Vector2(8, 0), new Vector2(-8, 0)),
                    15, spec.Enabled ? Ink : Secondary, TextAnchor.MiddleCenter, true).text = spec.Label;
                standaloneChips.Add(go);
            }
        }

        private void ClearStandaloneOverlayState()
        {
            standaloneOverlayActive = false;
            onStandaloneLargePrint = null;
            onStandaloneSecondary = null;
            Hide3DHotspotMarkers();
            if (standaloneRoot != null) standaloneRoot.gameObject.SetActive(false);
            if (panel != null) panel.gameObject.SetActive(true);
        }

        private static Sprite BuildCircleSprite(int size, bool filled)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = filled ? "MinimalHudDot" : "MinimalHudRing",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            float half = (size - 1) * 0.5f;
            float outer = half - 1.5f;
            float inner = outer - Mathf.Max(2.8f, size * 0.085f);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - half;
                    float dy = y - half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha;
                    if (filled)
                    {
                        alpha = Mathf.Clamp01((outer - r) * 0.9f + 0.5f);
                    }
                    else
                    {
                        float aOut = Mathf.Clamp01((outer - r) * 0.9f + 0.5f);
                        float aIn = Mathf.Clamp01((r - inner) * 0.9f + 0.5f);
                        alpha = aOut * aIn;
                    }
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
