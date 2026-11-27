using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CTCC
{
    public readonly struct InventoryTileData
    {
        public readonly string Id;
        public readonly string Label;
        public readonly string StatusBadge;
        public readonly Texture Thumbnail;
        public readonly bool Selected;
        public readonly Action Select;

        public InventoryTileData(string id, string label, string statusBadge, Texture thumbnail, bool selected, Action select)
        {
            Id = id;
            Label = label;
            StatusBadge = statusBadge;
            Thumbnail = thumbnail;
            Selected = selected;
            Select = select;
        }
    }

    /// <summary>
    /// Phase 2.5 Commercial Horror Minimalist Inventory UI (RE7/RE8 inspired, An Sinh 2012 palette):
    /// - Eliminates the Notebook layout, page-turn animation, long description walls, and embedded Pocket Note tab.
    /// - Left 55%: completely unobstructed interactive 360-degree 3D item preview turntable (BackpackStage3D).
    /// - Right 42%: dark minimalist visual item grid (2 filter tabs: Đồ vật | Giấy tờ) + compact selected item header & 3-4 action buttons.
    /// - Decouples Sổ tay nhỏ (Pocket Note) into its own dedicated minimal card presentation outside Inventory.
    /// - Zero capacity limit, zero weight, zero inventory Tetris.
    /// </summary>
    public sealed partial class HospitalUI
    {
        private static readonly Color InvPanelDark = new(0.032f, 0.044f, 0.040f, 0.90f);
        private static readonly Color InvTileIdle = new(0.068f, 0.088f, 0.080f, 0.92f);
        private static readonly Color InvTileSelected = new(0.145f, 0.205f, 0.172f, 0.98f);
        private static readonly Color InvTileBorder = new(0.40f, 0.48f, 0.42f, 0.65f);

        private bool inventory2Open;

        public bool IsInventory2Open => inventory2Open && IsModal;

        internal void ShowInventory2(
            int filterIndex, // 0 = Đồ vật, 1 = Giấy tờ
            Action<int> selectFilter,
            IReadOnlyList<InventoryTileData> tiles,
            string emptyCopy,
            string selectedTitle,
            string selectedBriefLine,
            ActionChoice[] actions,
            Action openPocketNote,
            Action openFullJournalIfAllowed,
            Action close,
            Action onCloseBag,
            bool show3DStageOnLeft = true)
        {
            ClearStandaloneOverlayState();
            ClearModalPanel();
            inventory2Open = true;
            dismiss = close;
            onCloseExtra = () =>
            {
                inventory2Open = false;
                onCloseBag?.Invoke();
            };
            focus.Clear();
            selected = -1;

            if (show3DStageOnLeft)
            {
                // Left 55% open for live interactive 360-degree 3D item preview!
                modalBackdrop.color = new Color(0.010f, 0.016f, 0.014f, 0.30f);
                modalBackdrop.raycastTarget = false;
                panel.anchorMin = new Vector2(0.555f, 0.055f);
                panel.anchorMax = new Vector2(0.972f, 0.945f);
            }
            else
            {
                modalBackdrop.color = new Color(0.010f, 0.016f, 0.014f, 0.72f);
                modalBackdrop.raycastTarget = true;
                panel.anchorMin = new Vector2(0.24f, 0.06f);
                panel.anchorMax = new Vector2(0.76f, 0.94f);
            }
            panelBg.color = InvPanelDark;

            Graphic(Rect("InvTopRule", panel, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -2), Vector2.zero), Accent, false);
            Graphic(Rect("InvLeftRule", panel, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(2, 0)), InvTileBorder, false);

            // Top bar: minimalist filter pills + decoupled Sổ tay shortcut + close
            var topBar = Rect("InvTopBar", panel, new Vector2(0, 0.905f), new Vector2(1, 0.992f), new Vector2(20, 4), new Vector2(-20, -6));
            BuildInvHeaderButton(topBar, 0.00f, 0.28f, "Đồ vật", filterIndex == 0, () => selectFilter(0));
            BuildInvHeaderButton(topBar, 0.295f, 0.575f, "Giấy tờ", filterIndex == 1, () => selectFilter(1));
            if (openPocketNote != null)
                BuildInvHeaderButton(topBar, 0.605f, 0.845f, "Sổ tay [N]", false, openPocketNote);
            BuildInvHeaderButton(topBar, 0.860f, 1.00f, "Đóng", false, close);

            Graphic(Rect("InvHeaderDivider", panel, new Vector2(0, 0.900f), new Vector2(1, 0.902f), new Vector2(20, 0), new Vector2(-20, 0)), InvTileBorder, false);

            // Visual Item Tile Grid (middle region: y 0.34 .. 0.885)
            var gridViewport = Rect("InvGridViewport", panel, new Vector2(0, 0.335f), new Vector2(1, 0.888f), new Vector2(20, 6), new Vector2(-20, -6));
            var scroll = gridViewport.gameObject.AddComponent<ScrollRect>();
            readingScroll = scroll;
            Graphic(gridViewport, Color.clear, true);
            gridViewport.gameObject.AddComponent<RectMask2D>();

            var content = Rect("InvGridContent", gridViewport, new Vector2(0, 1), new Vector2(1, 1));
            content.pivot = new Vector2(0.5f, 1f);

            if (tiles == null || tiles.Count == 0)
            {
                content.sizeDelta = new Vector2(0, 180);
                FillText("EmptyGridCopy", content, 19, Secondary, TextAnchor.MiddleCenter, false).text = emptyCopy;
            }
            else
            {
                var grid = content.gameObject.AddComponent<GridLayoutGroup>();
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 3;
                grid.cellSize = new Vector2(236f, 152f);
                grid.spacing = new Vector2(12f, 12f);
                grid.padding = new RectOffset(4, 4, 6, 8);
                var fit = content.gameObject.AddComponent<ContentSizeFitter>();
                fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                for (int i = 0; i < tiles.Count; i++)
                    BuildInventoryTile(content, tiles[i]);
            }
            scroll.viewport = gridViewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 32f;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            // Selected Item Compact Footer (y 0.02 .. 0.325) — crisp title, 1 brief line, 3-4 clean action buttons!
            Graphic(Rect("InvFooterDivider", panel, new Vector2(0, 0.328f), new Vector2(1, 0.330f), new Vector2(20, 0), new Vector2(-20, 0)), InvTileBorder, false);

            var selHeader = Rect("InvSelectedHeader", panel, new Vector2(0, 0.215f), new Vector2(1, 0.322f), new Vector2(22, 0), new Vector2(-22, -4));
            FillText("SelTitle", Rect("T", selHeader, new Vector2(0, 0.45f), new Vector2(1, 1f)), 24, Ink, TextAnchor.MiddleLeft, true).text =
                selectedTitle ?? "";
            FillText("SelSub", Rect("S", selHeader, new Vector2(0, 0f), new Vector2(1, 0.48f)), 16, Secondary, TextAnchor.UpperLeft, false).text =
                selectedBriefLine ?? "";

            // Compact Action Row (2 rows max if > 3 actions, cleanly sized)
            var actionList = new List<ActionChoice>();
            if (actions != null) actionList.AddRange(actions);
            if (openFullJournalIfAllowed != null)
                actionList.Add(new ActionChoice("Mở sổ trực đầy đủ", openFullJournalIfAllowed));

            var actArea = Rect("InvActionArea", panel, new Vector2(0, 0.060f), new Vector2(1, 0.205f), new Vector2(22, 2), new Vector2(-22, -2));
            if (actionList.Count > 0)
            {
                int cols = Mathf.Min(actionList.Count, 2);
                int rows = Mathf.CeilToInt(actionList.Count / (float)cols);
                float rowSpan = rows == 1 ? 0.56f : 1f / rows;
                for (int i = 0; i < actionList.Count; i++)
                {
                    int r = i / cols;
                    int c = i % cols;
                    float x0 = c / (float)cols;
                    float x1 = (c + 1f) / cols;
                    float y1 = 1f - r * rowSpan;
                    float y0 = y1 - rowSpan;
                    var cell = Rect("InvAct_" + i, actArea, new Vector2(x0, y0), new Vector2(x1, y1), new Vector2(4, 4), new Vector2(-4, -4));
                    BuildCompactActionButton(cell, actionList[i], primary: i == 0);
                }
            }

            // Ultra-minimal bottom whisper inside Inventory for 3D preview controls
            FillText("InvPreviewWhisper", Rect("Whisper", panel, new Vector2(0, 0.008f), new Vector2(1, 0.048f), new Vector2(22, 0), new Vector2(-22, 0)),
                14, new Color(0.68f, 0.74f, 0.70f, 0.80f), TextAnchor.MiddleLeft, false).text =
                show3DStageOnLeft
                    ? "Kéo bên trái xoay 360° · Cuộn thu phóng · Esc đóng"
                    : "Tab / Mũi tên chọn · Enter thao tác · Esc đóng";

            bool wasModal = IsModal;
            modal.gameObject.SetActive(true);
            modalOpenedFrame = Time.frameCount;
            hud.gameObject.SetActive(false);
            if (!wasModal) OpenChanged?.Invoke();
            MoveFocus(1);
        }

        private void BuildInvHeaderButton(RectTransform parent, float x0, float x1, string label, bool active, Action onPress)
        {
            var r = Rect("Tab_" + label, parent, new Vector2(x0, 0), new Vector2(x1, 1), new Vector2(2, 2), new Vector2(-2, -2));
            var bg = Graphic(r, active ? new Color(0.18f, 0.25f, 0.20f, 0.96f) : new Color(0.065f, 0.088f, 0.080f, 0.86f), true);
            if (active)
                Graphic(Rect("ActiveBar", r, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 3)), Accent, false);
            var btn = r.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            var cols = btn.colors;
            cols.normalColor = Color.white;
            cols.highlightedColor = new Color(1.08f, 1.10f, 1.02f);
            cols.selectedColor = new Color(1.06f, 1.10f, 0.96f);
            cols.pressedColor = new Color(0.85f, 0.92f, 0.85f);
            btn.colors = cols;
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;
            btn.onClick.AddListener(() => { if (!ModalInputBlocked) onPress?.Invoke(); });
            focus.Add(btn);
            FillText("Label", r, 17, active ? Accent : Ink, TextAnchor.MiddleCenter, true).text = label;
        }

        private void BuildInventoryTile(RectTransform parent, InventoryTileData tile)
        {
            var rt = new GameObject("Tile_" + tile.Id, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            var bg = Graphic(rt, tile.Selected ? InvTileSelected : InvTileIdle, true);

            // Crisp hairline selection frame
            Color edgeCol = tile.Selected ? Accent : new Color(0.30f, 0.38f, 0.34f, 0.55f);
            float t = tile.Selected ? 2f : 1f;
            Graphic(Rect("BT", rt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -t), Vector2.zero), edgeCol, false);
            Graphic(Rect("BB", rt, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, t)), edgeCol, false);
            Graphic(Rect("BL", rt, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(t, 0)), edgeCol, false);
            Graphic(Rect("BR", rt, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-t, 0), Vector2.zero), edgeCol, false);

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            var cols = btn.colors;
            cols.normalColor = Color.white;
            cols.highlightedColor = new Color(1.12f, 1.15f, 1.06f);
            cols.selectedColor = new Color(1.10f, 1.15f, 1.00f);
            cols.pressedColor = new Color(0.85f, 0.94f, 0.85f);
            btn.colors = cols;
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;
            btn.onClick.AddListener(() => { if (!ModalInputBlocked) tile.Select?.Invoke(); });
            focus.Add(btn);

            // Visual thumbnail area (top 68%)
            var thumbArea = Rect("Thumb", rt, new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.92f));
            if (tile.Thumbnail != null)
            {
                var rawGo = Rect("Img", thumbArea, Vector2.zero, Vector2.one);
                var raw = rawGo.gameObject.AddComponent<RawImage>();
                raw.texture = tile.Thumbnail;
                raw.raycastTarget = false;
                var arf = rawGo.gameObject.AddComponent<AspectRatioFitter>();
                arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                arf.aspectRatio = (float)tile.Thumbnail.width / Mathf.Max(1, tile.Thumbnail.height);
            }
            else
            {
                // Minimalist diegetic silhouette pill for 3D tools without a 2D scan texture (rendered cleanly in BeVietnamPro!)
                string emblem = tile.Id switch
                {
                    BackpackCatalog.Flashlight => "ĐÈN PIN",
                    BackpackCatalog.GateKey => "CHÌA ĐỒNG",
                    BackpackCatalog.WingCKey => "CHÌA CŨ",
                    BackpackCatalog.MinhPhone => "DI ĐỘNG",
                    BackpackCatalog.Ch04Tape => "CASSETTE",
                    BackpackCatalog.Ch02Wristband => "VÒNG C01",
                    _ => "VẬT DỤNG"
                };
                var pill = Rect("EmblemPill", thumbArea, new Vector2(0.14f, 0.26f), new Vector2(0.86f, 0.74f));
                Graphic(pill, new Color(0.11f, 0.14f, 0.13f, 0.85f), false);
                FillText("Glyph", pill, 15, tile.Selected ? Accent : new Color(0.80f, 0.84f, 0.76f, 0.90f), TextAnchor.MiddleCenter, true).text = emblem;
            }

            // Crisp 1-line diegetic item name at bottom
            FillText("Name", Rect("N", rt, new Vector2(0, 0), new Vector2(1, 0.29f), new Vector2(8, 2), new Vector2(-8, -2)),
                15, tile.Selected ? Ink : new Color(0.84f, 0.86f, 0.82f), TextAnchor.MiddleCenter, true).text = tile.Label;

            // Optional tiny top-right status chip (Đang cầm / Đang đặt / #1)
            if (!string.IsNullOrEmpty(tile.StatusBadge))
            {
                var badgeRt = Rect("Badge", rt, new Vector2(0.46f, 0.80f), new Vector2(0.97f, 0.97f));
                Graphic(badgeRt, new Color(0.025f, 0.036f, 0.032f, 0.86f), false);
                FillText("BText", badgeRt, 12, Accent, TextAnchor.MiddleCenter, true).text = tile.StatusBadge;
            }
        }

        private void BuildCompactActionButton(RectTransform cell, ActionChoice spec, bool primary)
        {
            var bg = Graphic(cell, primary
                ? new Color(0.18f, 0.255f, 0.205f, 0.96f)
                : new Color(0.105f, 0.145f, 0.130f, 0.92f), true);
            if (primary)
                Graphic(Rect("PrimaryRule", cell, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(3, 0)), Accent, false);
            var btn = cell.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            var cols = btn.colors;
            cols.normalColor = Color.white;
            cols.highlightedColor = new Color(1.12f, 1.15f, 1.05f);
            cols.selectedColor = new Color(1.10f, 1.15f, 0.98f);
            cols.pressedColor = new Color(0.84f, 0.92f, 0.84f);
            cols.disabledColor = new Color(0.45f, 0.48f, 0.46f);
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
            FillText("Label", Rect("In", cell, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0)),
                17, spec.Enabled ? Ink : Secondary, TextAnchor.MiddleCenter, true).text = spec.Label;
        }
    }
}
