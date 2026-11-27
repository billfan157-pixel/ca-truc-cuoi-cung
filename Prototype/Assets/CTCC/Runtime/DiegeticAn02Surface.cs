using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CTCC
{
    public readonly struct An02SurfaceState
    {
        public readonly FormStatus DraftDecision;
        public readonly FormStatus CommittedStatus;
        public readonly int CommittedRevision;
        public readonly bool ZeroMarked;
        public readonly bool HasObsA;
        public readonly bool HasObsB;
        public readonly bool CardMarked;
        public readonly bool SpatialLinked;
        public readonly bool Ex01HasClock;
        public readonly bool Ex01HasCardTime;
        public readonly bool Ex01HasShift;
        public readonly bool Ex01Solved;
        public readonly int[] Ex01Digits;
        public readonly int Ex01Cursor;
        public readonly bool ShowBranchCSlip;
        public readonly int SignStep; // 0 = Draft, 1 = Preview (ready to sign N. H. Minh), 2 = Confirm (ready to stamp)
        public readonly string Feedback;
        public readonly List<(string id, string label, bool selected)> SelectableSources;
        public readonly List<(string id, string label, bool selected)> Claims;

        public An02SurfaceState(
            FormStatus draftDecision,
            FormStatus committedStatus,
            int committedRevision,
            bool zeroMarked,
            bool hasObsA,
            bool hasObsB,
            bool cardMarked,
            bool spatialLinked,
            bool ex01HasClock,
            bool ex01HasCardTime,
            bool ex01HasShift,
            bool ex01Solved,
            int[] ex01Digits,
            int ex01Cursor,
            bool showBranchCSlip,
            int signStep,
            string feedback,
            List<(string id, string label, bool selected)> selectableSources,
            List<(string id, string label, bool selected)> claims)
        {
            DraftDecision = draftDecision;
            CommittedStatus = committedStatus;
            CommittedRevision = committedRevision;
            ZeroMarked = zeroMarked;
            HasObsA = hasObsA;
            HasObsB = hasObsB;
            CardMarked = cardMarked;
            SpatialLinked = spatialLinked;
            Ex01HasClock = ex01HasClock;
            Ex01HasCardTime = ex01HasCardTime;
            Ex01HasShift = ex01HasShift;
            Ex01Solved = ex01Solved;
            Ex01Digits = ex01Digits;
            Ex01Cursor = ex01Cursor;
            ShowBranchCSlip = showBranchCSlip;
            SignStep = signStep;
            Feedback = feedback;
            SelectableSources = selectableSources;
            Claims = claims;
        }
    }

    /// <summary>
    /// Phase 2.5 Diegetic Direct-Surface 3D F101 & AN02 Controller (Requirement G):
    /// - Mounts live, high-resolution on-demand RenderTexture surfaces directly onto Room 101's physical wall clipboard
    ///   (F101 sheet: 1024x1360) and a pinned 3D margin/deduction slip beside it (768x1000) on Layer 31 (0.0 ms idle GPU cost).
    /// - ZERO 2D sidebar in default mode: player directly clicks the 3D F101 paper to circle "0", check neutral A/B/C boxes,
    ///   sign "N. H. Minh" in cursive ink, press the red rubber stamp box, lift the corner for 2-sided 3D inspect,
    ///   enter free manual HH:MM EX01 digits on the pinned margin slip, and attach Branch C sources & claim.
    /// - Acquires InspectionSharpnessGuard (1.0x renderScale + far-only Gaussian DoF) so every handwritten line stays razor-sharp.
    /// </summary>
    public sealed class DiegeticAn02Surface : MonoBehaviour
    {
        private const int OffscreenLayer = 31;
        private const float F101Width = 0.222f;
        private const float F101Height = 0.310f;
        private const float SlipWidth = 0.214f;
        private const float SlipHeight = 0.282f;

        private struct ClickZone
        {
            public string Id;
            public string Label;
            public Rect Uv; // normalized bottom-left (0,0) to top-right (1,1)
            public bool OnSlip;
            public bool Marked;
        }

        private Camera eye;
        private EnvironmentalAudio audioBus;
        private Font uiFont, uiBold;

        // World 3D interactive quads mounted flush over the physical Room 101 wall clipboard
        private Transform worldRoot;
        private MeshRenderer f101QuadRenderer;
        private MeshCollider f101QuadCollider;
        private Material f101SurfaceMat;
        private MeshRenderer slipQuadRenderer;
        private MeshCollider slipQuadCollider;
        private Material slipSurfaceMat;
        private Material boardBackMat;
        private Material clipBarMat;
        private Light localDeskLight;

        // Offscreen RenderTexture #1: Live F101 Sheet (1024 x 1360)
        private RenderTexture f101Rt;
        private GameObject f101OffscreenRoot;
        private Camera f101Cam;
        private RawImage f101BaseImage;
        private Image f101ZeroCircle;
        private Text f101StatusStamp;
        private Image f101BoxA, f101BoxB, f101BoxC;
        private Text f101MarkA, f101MarkB, f101MarkC;
        private Image f101SignBg, f101StampBg, f101CancelSignBg;
        private Text f101SignText, f101StampText, f101CancelSignText, f101FooterNote;

        // Offscreen RenderTexture #2: Pinned 3D Margin / Branch C Slip (768 x 1000)
        private RenderTexture slipRt;
        private GameObject slipOffscreenRoot;
        private Camera slipCam;
        private Text slipHeaderTitle;
        private Image slipTabNotesBg, slipTabBranchCBg;
        private Text slipTabNotesText, slipTabBranchCText;
        private GameObject slipNotesGroup, slipBranchCGroup;
        // Notes & EX01 widgets
        private Text slipFieldSummary;
        private Image slipCardMarkBox;
        private Text slipCardMarkText;
        private Text slipEx01Status;
        private readonly Image[] slipDigitBgs = new Image[4];
        private readonly Text[] slipDigitTexts = new Text[4];
        private Image slipEx01ResetBg, slipEx01SubmitBg;
        private Text slipEx01ResetText, slipEx01SubmitText;
        private Text slipNotesFeedback;
        // Branch C widgets (pooled rows)
        private Text slipBranchCChecklist;
        private readonly List<(GameObject row, Image box, Text mark, Text label)> slipSourceRows = new();
        private readonly List<(GameObject row, Image dot, Text label)> slipClaimRows = new();
        private Text slipBranchCFeedback;

        private readonly List<ClickZone> activeZones = new(24);
        private bool active;
        private bool sharpnessAcquired;
        private int openedFrame = -1;
        private string hoveredZoneId;
        private Action<string> onZoneClicked;

        public bool IsActive => active;
        public string HoveredZoneId => hoveredZoneId;
        public bool InputBlocked { get; set; }

        public void Initialize(Camera camera, EnvironmentalAudio audio)
        {
            eye = camera;
            audioBus = audio;
            uiFont = Resources.Load<Font>("CTCC/BeVietnamPro-Regular");
            uiBold = Resources.Load<Font>("CTCC/BeVietnamPro-SemiBold");

            if (eye != null) eye.cullingMask &= ~(1 << OffscreenLayer);

            BuildWorldSurfaceQuads();
            BuildOffscreenF101Canvas();
            BuildOffscreenSlipCanvas();

            worldRoot.gameObject.SetActive(false);
            if (localDeskLight != null) localDeskLight.enabled = false;
        }

        public void Open(Vector3 clipboardCenter, Texture baseF101Texture, Volume activeGrade, bool reducedMotion,
            Action<string> onClickZone)
        {
            onZoneClicked = onClickZone;
            openedFrame = Time.frameCount;
            hoveredZoneId = null;

            // Align cleanly on Room 101 West wall (+X normal into Room 101), clear of the open doorway jamb on -Z!
            worldRoot.position = clipboardCenter + new Vector3(0.014f, 0f, 0.16f);
            worldRoot.rotation = Quaternion.LookRotation(Vector3.left, Vector3.up); // Quad front (-Z local) faces +X!

            if (f101BaseImage != null && baseF101Texture != null)
                f101BaseImage.texture = baseF101Texture;

            if (!active)
            {
                active = true;
                InspectionSharpnessGuard.Acquire(activeGrade, allowBackgroundSoften: !reducedMotion);
                sharpnessAcquired = true;
            }

            worldRoot.gameObject.SetActive(true);
            if (localDeskLight != null) localDeskLight.enabled = true;
        }

        public void Close()
        {
            if (sharpnessAcquired)
            {
                InspectionSharpnessGuard.Release();
                sharpnessAcquired = false;
            }
            active = false;
            hoveredZoneId = null;
            onZoneClicked = null;
            activeZones.Clear();
            if (worldRoot != null) worldRoot.gameObject.SetActive(false);
            if (localDeskLight != null) localDeskLight.enabled = false;
        }

        public void Redraw(in An02SurfaceState state)
        {
            if (!active) return;
            activeZones.Clear();
            RedrawF101Sheet(state);
            RedrawSideSlip(state);
            RenderOffscreenNow();
        }

        /// <summary>
        /// Direct deterministic click hook for QA / InputQA verification (routes through exact same zone click pipeline).
        /// </summary>
        internal bool TriggerZoneById(string zoneId)
        {
            if (!active || InputBlocked || string.IsNullOrEmpty(zoneId)) return false;
            for (int i = 0; i < activeZones.Count; i++)
            {
                if (activeZones[i].Id == zoneId)
                {
                    InvokeZone(zoneId);
                    return true;
                }
            }
            return false;
        }

        internal int ActiveZoneCount => activeZones.Count;

        public void ProjectHotspots(List<ProjectedHotspot3D> output)
        {
            output.Clear();
            if (!active || eye == null) return;
            for (int i = 0; i < activeZones.Count; i++)
            {
                var z = activeZones[i];
                Vector3 worldPt = ZoneWorldPoint(z);
                Vector3 sp = eye.WorldToScreenPoint(worldPt);
                if (sp.z <= 0.05f) continue;
                output.Add(new ProjectedHotspot3D(z.Id, z.Label, sp, hoveredZoneId == z.Id, z.Marked));
            }
        }

        private Vector3 ZoneWorldPoint(in ClickZone z)
        {
            Transform quadTf = z.OnSlip ? slipQuadRenderer.transform : f101QuadRenderer.transform;
            float u = z.Uv.center.x - 0.5f;
            float v = z.Uv.center.y - 0.5f;
            return quadTf.TransformPoint(new Vector3(u, v, -0.002f));
        }

        private void Update()
        {
            if (!active || InputBlocked || eye == null) return;
            if (Time.frameCount == openedFrame) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 pointer = mouse.position.ReadValue();
            bool overUiChip = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            hoveredZoneId = overUiChip ? null : RaycastZone(pointer);

            if (!overUiChip && mouse.leftButton.wasPressedThisFrame && !string.IsNullOrEmpty(hoveredZoneId))
            {
                InvokeZone(hoveredZoneId);
            }
        }

        private void InvokeZone(string zoneId)
        {
            if (audioBus != null && worldRoot != null)
            {
                string sfx = zoneId is ("SIGN_STAMP") ? "stamp"
                    : zoneId is ("MARK_ZERO" or "MARK_CARD" or "SIGN_NAME" or "EX01_SUBMIT") ? "signaturePen"
                    : "paperFlip";
                audioBus.From(sfx, worldRoot.position, 0.55f);
            }
            onZoneClicked?.Invoke(zoneId);
        }

        private string RaycastZone(Vector2 screenPos)
        {
            Ray ray = eye.ScreenPointToRay(screenPos);
            if (f101QuadCollider != null && f101QuadCollider.Raycast(ray, out var hitF, 3.0f))
            {
                Vector2 uv = hitF.textureCoord;
                for (int i = activeZones.Count - 1; i >= 0; i--)
                    if (!activeZones[i].OnSlip && activeZones[i].Uv.Contains(uv))
                        return activeZones[i].Id;
            }
            if (slipQuadCollider != null && slipQuadCollider.Raycast(ray, out var hitS, 3.0f))
            {
                Vector2 uv = hitS.textureCoord;
                for (int i = activeZones.Count - 1; i >= 0; i--)
                    if (activeZones[i].OnSlip && activeZones[i].Uv.Contains(uv))
                        return activeZones[i].Id;
            }
            return null;
        }

        private void BuildWorldSurfaceQuads()
        {
            worldRoot = new GameObject("CTCC_DiegeticAn02Surface").transform;
            worldRoot.SetParent(transform, false);

            var lit = Shader.Find("Universal Render Pipeline/Lit")
                      ?? Shader.Find("Universal Render Pipeline/Unlit")
                      ?? Shader.Find("Sprites/Default");

            // 1. Live F101 Sheet Quad (0.222 x 0.310m) — 16-bit depth required by URP 17 RenderGraph
            f101Rt = new RenderTexture(1024, 1360, 16, RenderTextureFormat.ARGB32)
            {
                name = "CTCC_An02F101LiveRT",
                filterMode = FilterMode.Trilinear,
                anisoLevel = 16,
                useMipMap = true,
                autoGenerateMips = true,
                mipMapBias = -0.5f
            };
            f101Rt.Create();

            f101SurfaceMat = new Material(lit) { name = "CTCC_An02F101LiveMat" };
            f101SurfaceMat.SetTexture("_BaseMap", f101Rt);
            f101SurfaceMat.SetColor("_BaseColor", new Color(0.95f, 0.94f, 0.90f, 1f));
            f101SurfaceMat.SetFloat("_Metallic", 0f);
            f101SurfaceMat.SetFloat("_Smoothness", 0.06f);
            f101SurfaceMat.SetTexture("_EmissionMap", f101Rt);
            f101SurfaceMat.SetColor("_EmissionColor", new Color(0.28f, 0.27f, 0.25f, 1f));
            f101SurfaceMat.EnableKeyword("_EMISSION");

            var f101Go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            f101Go.name = "An02_F101_InteractiveSheet";
            f101Go.transform.SetParent(worldRoot, false);
            f101Go.transform.localPosition = Vector3.zero;
            f101Go.transform.localRotation = Quaternion.identity;
            f101Go.transform.localScale = new Vector3(F101Width, F101Height, 1f);
            f101QuadRenderer = f101Go.GetComponent<MeshRenderer>();
            f101QuadRenderer.sharedMaterial = f101SurfaceMat;
            f101QuadRenderer.shadowCastingMode = ShadowCastingMode.Off;
            f101QuadCollider = f101Go.GetComponent<MeshCollider>();

            // Thin physical dark-wood clipboard backing + steel spring clip behind F101 sheet
            var boardGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boardGo.name = "An02_WoodenClipboardBack";
            var bc = boardGo.GetComponent<Collider>();
            if (bc != null) Destroy(bc);
            boardGo.transform.SetParent(worldRoot, false);
            boardGo.transform.localPosition = new Vector3(0f, 0.005f, 0.004f);
            boardGo.transform.localScale = new Vector3(F101Width + 0.026f, F101Height + 0.036f, 0.006f);
            boardBackMat = new Material(lit) { name = "An02BoardWoodMat" };
            boardBackMat.SetColor("_BaseColor", new Color(0.24f, 0.16f, 0.10f, 1f));
            boardBackMat.SetFloat("_Smoothness", 0.14f);
            var boardMr = boardGo.GetComponent<MeshRenderer>();
            boardMr.sharedMaterial = boardBackMat;
            boardMr.shadowCastingMode = ShadowCastingMode.Off;

            var clipBarGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            clipBarGo.name = "An02_MetalSpringClip";
            var cc = clipBarGo.GetComponent<Collider>();
            if (cc != null) Destroy(cc);
            clipBarGo.transform.SetParent(worldRoot, false);
            clipBarGo.transform.localPosition = new Vector3(0f, F101Height * 0.5f + 0.006f, -0.003f);
            clipBarGo.transform.localScale = new Vector3(0.088f, 0.024f, 0.008f);
            clipBarMat = new Material(lit) { name = "An02MetalClipMat" };
            clipBarMat.SetColor("_BaseColor", new Color(0.56f, 0.54f, 0.50f, 1f));
            clipBarMat.SetFloat("_Metallic", 0.72f);
            clipBarMat.SetFloat("_Smoothness", 0.42f);
            var clipMr = clipBarGo.GetComponent<MeshRenderer>();
            clipMr.sharedMaterial = clipBarMat;
            clipMr.shadowCastingMode = ShadowCastingMode.Off;

            // 2. Pinned 3D Margin / Branch C Deduction Slip (0.214 x 0.282m) sitting right beside F101 on the wall (+Z = right)
            slipRt = new RenderTexture(768, 1000, 16, RenderTextureFormat.ARGB32)
            {
                name = "CTCC_An02SideSlipLiveRT",
                filterMode = FilterMode.Trilinear,
                anisoLevel = 16,
                useMipMap = true,
                autoGenerateMips = true,
                mipMapBias = -0.5f
            };
            slipRt.Create();

            slipSurfaceMat = new Material(lit) { name = "CTCC_An02SideSlipLiveMat" };
            slipSurfaceMat.SetTexture("_BaseMap", slipRt);
            slipSurfaceMat.SetColor("_BaseColor", new Color(0.96f, 0.95f, 0.91f, 1f));
            slipSurfaceMat.SetFloat("_Metallic", 0f);
            slipSurfaceMat.SetFloat("_Smoothness", 0.06f);
            slipSurfaceMat.SetTexture("_EmissionMap", slipRt);
            slipSurfaceMat.SetColor("_EmissionColor", new Color(0.28f, 0.27f, 0.24f, 1f));
            slipSurfaceMat.EnableKeyword("_EMISSION");

            var slipGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            slipGo.name = "An02_MarginSlip_InteractiveSheet";
            slipGo.transform.SetParent(worldRoot, false);
            // In worldRoot (facing -X, so local +X is +Z along wall = viewer's RIGHT, local -Z is +X into Room 101)
            slipGo.transform.localPosition = new Vector3(0.242f, -0.004f, -0.014f);
            slipGo.transform.localRotation = Quaternion.Euler(0f, -4f, -1.2f);
            slipGo.transform.localScale = new Vector3(SlipWidth, SlipHeight, 1f);
            slipQuadRenderer = slipGo.GetComponent<MeshRenderer>();
            slipQuadRenderer.sharedMaterial = slipSurfaceMat;
            slipQuadRenderer.shadowCastingMode = ShadowCastingMode.Off;
            slipQuadCollider = slipGo.GetComponent<MeshCollider>();

            // Subtle warm ward reading fill light right above the clipboard
            var lGo = new GameObject("An02ClipLight");
            lGo.transform.SetParent(worldRoot, false);
            lGo.transform.localPosition = new Vector3(0.11f, 0.22f, -0.34f);
            localDeskLight = lGo.AddComponent<Light>();
            localDeskLight.type = LightType.Point;
            localDeskLight.range = 1.6f;
            localDeskLight.intensity = 0.62f;
            localDeskLight.color = new Color(0.99f, 0.95f, 0.86f);
            localDeskLight.shadows = LightShadows.None;
        }

        private void BuildOffscreenF101Canvas()
        {
            f101OffscreenRoot = new GameObject("CTCC_An02F101_Offscreen");
            f101OffscreenRoot.transform.SetParent(transform, false);
            f101OffscreenRoot.transform.position = new Vector3(0f, -640f, 0f);

            var camGo = new GameObject("F101OffscreenCam");
            camGo.transform.SetParent(f101OffscreenRoot.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0f, -10f);
            f101Cam = camGo.AddComponent<Camera>();
            f101Cam.orthographic = true;
            f101Cam.orthographicSize = 1f;
            f101Cam.nearClipPlane = 0.1f;
            f101Cam.farClipPlane = 20f;
            f101Cam.clearFlags = CameraClearFlags.SolidColor;
            f101Cam.backgroundColor = new Color(0.91f, 0.89f, 0.83f, 1f);
            f101Cam.cullingMask = 1 << OffscreenLayer;
            f101Cam.targetTexture = f101Rt;
            f101Cam.enabled = false;

            var cGo = new GameObject("F101Canvas", typeof(Canvas), typeof(CanvasScaler));
            cGo.transform.SetParent(f101OffscreenRoot.transform, false);
            var canvas = cGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = f101Cam;
            canvas.planeDistance = 5f;
            var scaler = cGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024f, 1360f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = cGo.GetComponent<RectTransform>();

            // Base scanned F101 document texture
            var baseGo = new GameObject("BaseF101", typeof(RectTransform), typeof(RawImage));
            baseGo.transform.SetParent(root, false);
            var baseRt = baseGo.GetComponent<RectTransform>();
            baseRt.anchorMin = Vector2.zero; baseRt.anchorMax = Vector2.one;
            baseRt.offsetMin = Vector2.zero; baseRt.offsetMax = Vector2.zero;
            f101BaseImage = baseGo.GetComponent<RawImage>();
            f101BaseImage.color = Color.white;

            // Top-right corner lift cue ("Soi 2 mặt")
            var liftBg = MakePanel(root, "LiftCorner", new Vector2(0.76f, 0.935f), new Vector2(0.97f, 0.985f), new Color(0.16f, 0.22f, 0.24f, 0.84f));
            MakeText(liftBg.rectTransform, "Soi 2 mặt [F] ↗", 22, new Color(0.96f, 0.94f, 0.86f, 1f), TextAnchor.MiddleCenter, bold: true);

            // Blue ballpoint highlight frame over the "0" summary row (matches PaperField(Zero, 110, 684, 844, 132) -> y=[0.40..0.50])
            f101ZeroCircle = MakePanel(root, "ZeroInkMark", new Vector2(0.095f, 0.398f), new Vector2(0.935f, 0.502f), new Color(0.14f, 0.34f, 0.78f, 0.22f));
            var zeroOutline = f101ZeroCircle.gameObject.AddComponent<Outline>();
            zeroOutline.effectColor = new Color(0.10f, 0.28f, 0.72f, 0.95f);
            zeroOutline.effectDistance = new Vector2(5f, -5f);
            MakeText(f101ZeroCircle.rectTransform, "○  ĐÃ KHOANH TRÒN BÚT BI", 21, new Color(0.08f, 0.24f, 0.66f, 0.98f), TextAnchor.UpperRight, bold: true);
            f101ZeroCircle.gameObject.SetActive(false);

            // Lower diegetic ink-stamped decision & signature sheet section (cleanly covers bottom blank box y=[0.02..0.38])
            var lowerBg = MakePanel(root, "DecisionBox", new Vector2(0.055f, 0.022f), new Vector2(0.945f, 0.382f), new Color(0.93f, 0.91f, 0.85f, 0.96f));
            var lowerOut = lowerBg.gameObject.AddComponent<Outline>();
            lowerOut.effectColor = new Color(0.24f, 0.22f, 0.19f, 0.82f);
            lowerOut.effectDistance = new Vector2(2f, -2f);

            f101StatusStamp = MakeText(RectOf(lowerBg.rectTransform, "RevLine", new Vector2(0.03f, 0.885f), new Vector2(0.97f, 0.975f)),
                "KẾT LUẬN BÀN GIAO (BẤM TRỰC TIẾP Ô ĐỂ CHỌN — KHÔNG MẶC ĐỊNH)", 21, new Color(0.18f, 0.16f, 0.14f, 1f), TextAnchor.MiddleLeft, bold: true);

            // Equal-weight, neutral A / B / C rows (zero moral color coding!)
            BuildF101OptionRow(lowerBg.rectTransform, "OptA", new Vector2(0.03f, 0.68f), new Vector2(0.97f, 0.855f),
                "A.  Xác nhận theo bản kê: 0 bệnh nhân chưa bàn giao", out f101BoxA, out f101MarkA);
            BuildF101OptionRow(lowerBg.rectTransform, "OptB", new Vector2(0.03f, 0.49f), new Vector2(0.97f, 0.665f),
                "B.  Chưa đủ thông tin xác nhận · Giữ phiếu trạng thái chờ", out f101BoxB, out f101MarkB);
            BuildF101OptionRow(lowerBg.rectTransform, "OptC", new Vector2(0.03f, 0.30f), new Vector2(0.97f, 0.475f),
                "C.  Ghi nhận số 0 chưa đủ căn cứ xác nhận (kẹp nguồn bên phải)", out f101BoxC, out f101MarkC);

            // 2-step Signature line + Red Stamp Box directly on the paper
            f101SignBg = MakePanel(lowerBg.rectTransform, "SignLineBtn", new Vector2(0.03f, 0.085f), new Vector2(0.56f, 0.265f), new Color(0.86f, 0.84f, 0.78f, 1f));
            var sOut = f101SignBg.gameObject.AddComponent<Outline>();
            sOut.effectColor = new Color(0.16f, 0.26f, 0.56f, 0.85f);
            sOut.effectDistance = new Vector2(2f, -2f);
            f101SignText = MakeText(f101SignBg.rectTransform, "Đọc lại & ký tên", 26, new Color(0.10f, 0.20f, 0.52f, 1f), TextAnchor.MiddleCenter, bold: true);

            f101StampBg = MakePanel(lowerBg.rectTransform, "StampBtn", new Vector2(0.585f, 0.085f), new Vector2(0.97f, 0.265f), new Color(0.84f, 0.80f, 0.74f, 0.95f));
            var stOut = f101StampBg.gameObject.AddComponent<Outline>();
            stOut.effectColor = new Color(0.68f, 0.18f, 0.16f, 0.90f);
            stOut.effectDistance = new Vector2(3f, -3f);
            f101StampText = MakeText(f101StampBg.rectTransform, "ĐÓNG DẤU CA TRỰC", 24, new Color(0.66f, 0.14f, 0.12f, 1f), TextAnchor.MiddleCenter, bold: true);

            f101CancelSignBg = MakePanel(lowerBg.rectTransform, "CancelSignBtn", new Vector2(0.76f, 0.885f), new Vector2(0.97f, 0.975f), new Color(0.32f, 0.28f, 0.24f, 0.88f));
            f101CancelSignText = MakeText(f101CancelSignBg.rectTransform, "Sửa lại", 21, new Color(0.96f, 0.94f, 0.88f, 1f), TextAnchor.MiddleCenter, bold: true);
            f101CancelSignBg.gameObject.SetActive(false);

            f101FooterNote = MakeText(RectOf(lowerBg.rectTransform, "Footer", new Vector2(0.03f, 0.01f), new Vector2(0.97f, 0.075f)),
                "Có thể kiểm tra lại và điều chỉnh phiếu sau khi ký.", 18, new Color(0.28f, 0.26f, 0.22f, 0.92f), TextAnchor.MiddleLeft, bold: false);

            SetLayerRecursive(f101OffscreenRoot, OffscreenLayer);
        }

        private void BuildF101OptionRow(RectTransform parent, string name, Vector2 min, Vector2 max, string label,
            out Image boxBg, out Text markText)
        {
            var row = MakePanel(parent, name, min, max, new Color(0.89f, 0.87f, 0.81f, 0.94f));
            var ro = row.gameObject.AddComponent<Outline>();
            ro.effectColor = new Color(0.32f, 0.30f, 0.26f, 0.55f);
            ro.effectDistance = new Vector2(1.5f, -1.5f);

            boxBg = MakePanel(row.rectTransform, "Box", new Vector2(0.018f, 0.16f), new Vector2(0.088f, 0.84f), new Color(0.97f, 0.96f, 0.92f, 1f));
            var bo = boxBg.gameObject.AddComponent<Outline>();
            bo.effectColor = new Color(0.18f, 0.18f, 0.16f, 0.95f);
            bo.effectDistance = new Vector2(2.5f, -2.5f);
            markText = MakeText(boxBg.rectTransform, "", 38, new Color(0.08f, 0.24f, 0.66f, 1f), TextAnchor.MiddleCenter, bold: true);

            MakeText(RectOf(row.rectTransform, "Label", new Vector2(0.105f, 0.05f), new Vector2(0.985f, 0.95f)),
                label, 23, new Color(0.12f, 0.12f, 0.11f, 1f), TextAnchor.MiddleLeft, bold: true);
        }

        private void BuildOffscreenSlipCanvas()
        {
            slipOffscreenRoot = new GameObject("CTCC_An02Slip_Offscreen");
            slipOffscreenRoot.transform.SetParent(transform, false);
            slipOffscreenRoot.transform.position = new Vector3(0f, -720f, 0f);

            var camGo = new GameObject("SlipOffscreenCam");
            camGo.transform.SetParent(slipOffscreenRoot.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0f, -10f);
            slipCam = camGo.AddComponent<Camera>();
            slipCam.orthographic = true;
            slipCam.orthographicSize = 1f;
            slipCam.nearClipPlane = 0.1f;
            slipCam.farClipPlane = 20f;
            slipCam.clearFlags = CameraClearFlags.SolidColor;
            slipCam.backgroundColor = new Color(0.93f, 0.89f, 0.76f, 1f);
            slipCam.cullingMask = 1 << OffscreenLayer;
            slipCam.targetTexture = slipRt;
            slipCam.enabled = false;

            var cGo = new GameObject("SlipCanvas", typeof(Canvas), typeof(CanvasScaler));
            cGo.transform.SetParent(slipOffscreenRoot.transform, false);
            var canvas = cGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = slipCam;
            canvas.planeDistance = 5f;
            var scaler = cGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(768f, 1000f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = cGo.GetComponent<RectTransform>();
            var paper = MakePanel(root, "Paper", Vector2.zero, Vector2.one, new Color(0.93f, 0.89f, 0.77f, 1f));
            var border = paper.gameObject.AddComponent<Outline>();
            border.effectColor = new Color(0.32f, 0.28f, 0.20f, 0.85f);
            border.effectDistance = new Vector2(4f, -4f);

            // Top paper clip stripe + title
            MakePanel(root, "HeaderRule", new Vector2(0.04f, 0.868f), new Vector2(0.96f, 0.873f), new Color(0.28f, 0.24f, 0.18f, 0.70f));
            slipHeaderTitle = MakeText(RectOf(root, "Title", new Vector2(0.05f, 0.93f), new Vector2(0.95f, 0.985f)),
                "GIẤY GHI CHÚ KẸP MÉP BẢNG", 26, new Color(0.16f, 0.14f, 0.12f, 1f), TextAnchor.MiddleCenter, bold: true);

            // Two physical paper tabs at top of slip: "Ghi chú giờ" | "Căn cứ nhánh C"
            slipTabNotesBg = MakePanel(root, "TabNotes", new Vector2(0.05f, 0.88f), new Vector2(0.49f, 0.925f), new Color(0.18f, 0.24f, 0.26f, 0.90f));
            slipTabNotesText = MakeText(slipTabNotesBg.rectTransform, "Ghi chú & Mốc giờ", 20, new Color(0.95f, 0.93f, 0.85f, 1f), TextAnchor.MiddleCenter, bold: true);
            slipTabBranchCBg = MakePanel(root, "TabC", new Vector2(0.51f, 0.88f), new Vector2(0.95f, 0.925f), new Color(0.84f, 0.79f, 0.66f, 1f));
            slipTabBranchCText = MakeText(slipTabBranchCBg.rectTransform, "Nguồn nhánh C", 20, new Color(0.20f, 0.18f, 0.14f, 1f), TextAnchor.MiddleCenter, bold: true);

            // --- GROUP 1: Field Notes + EX01 Free HH:MM Pencil Input ---
            slipNotesGroup = new GameObject("NotesGroup", typeof(RectTransform));
            slipNotesGroup.transform.SetParent(root, false);
            var nRt = slipNotesGroup.GetComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0.05f, 0.04f); nRt.anchorMax = new Vector2(0.95f, 0.86f);
            nRt.offsetMin = Vector2.zero; nRt.offsetMax = Vector2.zero;

            slipFieldSummary = MakeText(RectOf(nRt, "FieldSum", new Vector2(0f, 0.63f), new Vector2(1f, 0.99f)),
                "", 22, new Color(0.15f, 0.14f, 0.12f, 1f), TextAnchor.UpperLeft, bold: false);

            slipCardMarkBox = MakePanel(nRt, "CardMarkBtn", new Vector2(0f, 0.54f), new Vector2(1f, 0.62f), new Color(0.86f, 0.83f, 0.72f, 1f));
            var cmo = slipCardMarkBox.gameObject.AddComponent<Outline>();
            cmo.effectColor = new Color(0.16f, 0.30f, 0.62f, 0.85f);
            cmo.effectDistance = new Vector2(2f, -2f);
            slipCardMarkText = MakeText(slipCardMarkBox.rectTransform, "Đánh dấu ô ký nhận trống trên thẻ AS-05B", 20, new Color(0.10f, 0.22f, 0.52f, 1f), TextAnchor.MiddleCenter, bold: true);

            // Horizontal divider above EX01 pencil margin block
            MakePanel(nRt, "ExRule", new Vector2(0f, 0.515f), new Vector2(1f, 0.521f), new Color(0.32f, 0.28f, 0.20f, 0.65f));

            slipEx01Status = MakeText(RectOf(nRt, "ExStatus", new Vector2(0f, 0.355f), new Vector2(1f, 0.505f)),
                "", 20, new Color(0.18f, 0.16f, 0.13f, 1f), TextAnchor.UpperLeft, bold: false);

            // 4 manual graphite digit cells: [H1] [H2] : [M1] [M2]
            float[] xStarts = { 0.05f, 0.26f, 0.54f, 0.75f };
            for (int i = 0; i < 4; i++)
            {
                slipDigitBgs[i] = MakePanel(nRt, "Digit_" + i, new Vector2(xStarts[i], 0.21f), new Vector2(xStarts[i] + 0.18f, 0.34f), new Color(0.98f, 0.96f, 0.90f, 1f));
                var dOut = slipDigitBgs[i].gameObject.AddComponent<Outline>();
                dOut.effectColor = new Color(0.20f, 0.20f, 0.18f, 0.90f);
                dOut.effectDistance = new Vector2(3f, -3f);
                slipDigitTexts[i] = MakeText(slipDigitBgs[i].rectTransform, "-", 56, new Color(0.14f, 0.15f, 0.16f, 1f), TextAnchor.MiddleCenter, bold: true);
            }
            MakeText(RectOf(nRt, "Colon", new Vector2(0.45f, 0.21f), new Vector2(0.53f, 0.34f)),
                ":", 54, new Color(0.18f, 0.16f, 0.14f, 1f), TextAnchor.MiddleCenter, bold: true);

            slipEx01ResetBg = MakePanel(nRt, "ExReset", new Vector2(0.02f, 0.115f), new Vector2(0.36f, 0.19f), new Color(0.84f, 0.80f, 0.70f, 1f));
            var rOut = slipEx01ResetBg.gameObject.AddComponent<Outline>();
            rOut.effectColor = new Color(0.32f, 0.28f, 0.22f, 0.8f);
            rOut.effectDistance = new Vector2(2f, -2f);
            slipEx01ResetText = MakeText(slipEx01ResetBg.rectTransform, "Xóa (--:--)", 21, new Color(0.22f, 0.18f, 0.14f, 1f), TextAnchor.MiddleCenter, bold: true);

            slipEx01SubmitBg = MakePanel(nRt, "ExSubmit", new Vector2(0.39f, 0.115f), new Vector2(0.98f, 0.19f), new Color(0.18f, 0.26f, 0.28f, 0.94f));
            slipEx01SubmitText = MakeText(slipEx01SubmitBg.rectTransform, "Ghi mốc giờ bút chì", 22, new Color(0.96f, 0.94f, 0.86f, 1f), TextAnchor.MiddleCenter, bold: true);

            slipNotesFeedback = MakeText(RectOf(nRt, "NotesFb", new Vector2(0f, 0f), new Vector2(1f, 0.105f)),
                "", 19, new Color(0.64f, 0.14f, 0.12f, 1f), TextAnchor.UpperLeft, bold: true);

            // --- GROUP 2: Branch C Deduction Slip (Sources + Claim) ---
            slipBranchCGroup = new GameObject("BranchCGroup", typeof(RectTransform));
            slipBranchCGroup.transform.SetParent(root, false);
            var cRt = slipBranchCGroup.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.05f, 0.04f); cRt.anchorMax = new Vector2(0.95f, 0.86f);
            cRt.offsetMin = Vector2.zero; cRt.offsetMax = Vector2.zero;

            slipBranchCChecklist = MakeText(RectOf(cRt, "ChecklistC", new Vector2(0f, 0.855f), new Vector2(1f, 0.995f)),
                "", 18, new Color(0.15f, 0.14f, 0.12f, 1f), TextAnchor.UpperLeft, bold: false);

            // Pool 7 selectable evidence source rows
            for (int i = 0; i < 7; i++)
            {
                float yTop = 0.840f - i * 0.056f;
                var row = MakePanel(cRt, "Src_" + i, new Vector2(0f, yTop - 0.050f), new Vector2(1f, yTop), new Color(0.88f, 0.84f, 0.72f, 0.95f));
                var box = MakePanel(row.rectTransform, "Bx", new Vector2(0.015f, 0.14f), new Vector2(0.080f, 0.86f), new Color(0.98f, 0.97f, 0.92f, 1f));
                var bo = box.gameObject.AddComponent<Outline>();
                bo.effectColor = new Color(0.18f, 0.18f, 0.16f, 0.90f);
                bo.effectDistance = new Vector2(2f, -2f);
                var mk = MakeText(box.rectTransform, "", 24, new Color(0.08f, 0.24f, 0.66f, 1f), TextAnchor.MiddleCenter, bold: true);
                var lbl = MakeText(RectOf(row.rectTransform, "Lb", new Vector2(0.098f, 0.02f), new Vector2(0.985f, 0.98f)),
                    "", 18, new Color(0.12f, 0.12f, 0.11f, 1f), TextAnchor.MiddleLeft, bold: true);
                row.gameObject.SetActive(false);
                slipSourceRows.Add((row.gameObject, box, mk, lbl));
            }

            // Pool all 5 F101 claim radio rows
            MakeText(RectOf(cRt, "ClaimHdr", new Vector2(0f, 0.415f), new Vector2(1f, 0.448f)),
                "KẾT LUẬN ĐÍNH KÈM:", 18, new Color(0.20f, 0.18f, 0.14f, 1f), TextAnchor.MiddleLeft, bold: true);
            for (int i = 0; i < 5; i++)
            {
                float yTop = 0.410f - i * 0.062f;
                var row = MakePanel(cRt, "Clm_" + i, new Vector2(0f, yTop - 0.056f), new Vector2(1f, yTop), new Color(0.88f, 0.84f, 0.72f, 0.95f));
                var dot = MakePanel(row.rectTransform, "Dt", new Vector2(0.015f, 0.22f), new Vector2(0.068f, 0.78f), new Color(0.98f, 0.97f, 0.92f, 1f));
                var dOut = dot.gameObject.AddComponent<Outline>();
                dOut.effectColor = new Color(0.18f, 0.18f, 0.16f, 0.90f);
                dOut.effectDistance = new Vector2(2f, -2f);
                var lbl = MakeText(RectOf(row.rectTransform, "Lb", new Vector2(0.086f, 0.02f), new Vector2(0.988f, 0.98f)),
                    "", 16, new Color(0.12f, 0.12f, 0.11f, 1f), TextAnchor.MiddleLeft, bold: false);
                row.gameObject.SetActive(false);
                slipClaimRows.Add((row.gameObject, dot, lbl));
            }

            slipBranchCFeedback = MakeText(RectOf(cRt, "FbC", new Vector2(0f, 0f), new Vector2(1f, 0.095f)),
                "", 17, new Color(0.64f, 0.14f, 0.12f, 1f), TextAnchor.UpperLeft, bold: true);

            SetLayerRecursive(slipOffscreenRoot, OffscreenLayer);
        }

        private void RedrawF101Sheet(in An02SurfaceState s)
        {
            f101ZeroCircle.gameObject.SetActive(s.ZeroMarked);
            AddZone("MARK_ZERO", s.ZeroMarked ? "Bỏ khoanh số 0" : "Khoanh tròn dòng số 0",
                new Rect(0.09f, 0.395f, 0.85f, 0.11f), onSlip: false, marked: s.ZeroMarked);
            AddZone("LIFT_F101", "Nhấc tờ F101 soi 2 mặt",
                new Rect(0.75f, 0.93f, 0.23f, 0.06f), onSlip: false, marked: false);

            f101StatusStamp.text = s.CommittedRevision == 0
                ? "PHIẾU CHƯA KÝ  ·  CHỌN PHƯƠNG ÁN TRỰC TIẾP TRÊN GIẤY"
                : $"ĐÃ KÝ LẦN {s.CommittedRevision}  ·  CÓ THỂ CHỌN LẠI VÀ KÝ SỬA";

            f101MarkA.text = s.DraftDecision == FormStatus.Affirmed ? "✓" : "";
            f101MarkB.text = s.DraftDecision == FormStatus.Unverified ? "✓" : "";
            f101MarkC.text = s.DraftDecision == FormStatus.Refuted ? "✓" : "";

            // Normalized coordinates inside DecisionBox (x=[0.055..0.945], y=[0.022..0.382])
            AddZone("OPT_A", "Chọn A · Xác nhận 0", SubUv(0.055f, 0.022f, 0.89f, 0.36f, 0.03f, 0.68f, 0.94f, 0.175f), false, s.DraftDecision == FormStatus.Affirmed);
            AddZone("OPT_B", "Chọn B · Giữ phiếu chờ", SubUv(0.055f, 0.022f, 0.89f, 0.36f, 0.03f, 0.49f, 0.94f, 0.175f), false, s.DraftDecision == FormStatus.Unverified);
            AddZone("OPT_C", "Chọn C · Số 0 chưa đủ căn cứ", SubUv(0.055f, 0.022f, 0.89f, 0.36f, 0.03f, 0.30f, 0.94f, 0.175f), false, s.DraftDecision == FormStatus.Refuted);

            bool hasDraft = s.DraftDecision != FormStatus.Pending;
            f101CancelSignBg.gameObject.SetActive(s.SignStep > 0);
            if (s.SignStep > 0)
            {
                AddZone("SIGN_CANCEL", "Quay lại chỉnh sửa", SubUv(0.055f, 0.022f, 0.89f, 0.36f, 0.76f, 0.885f, 0.21f, 0.09f), false, false);
            }

            if (s.SignStep == 0)
            {
                f101SignBg.color = hasDraft ? new Color(0.84f, 0.86f, 0.92f, 1f) : new Color(0.88f, 0.87f, 0.83f, 0.75f);
                f101SignText.text = hasDraft ? "Đọc lại & chuẩn bị ký ▸" : "Chọn A, B hoặc C trước";
                f101SignText.color = hasDraft ? new Color(0.08f, 0.22f, 0.56f, 1f) : new Color(0.45f, 0.43f, 0.38f, 0.9f);
                f101StampBg.color = new Color(0.88f, 0.86f, 0.82f, 0.55f);
                f101StampText.text = "Chưa ký tên";
                f101StampText.color = new Color(0.48f, 0.44f, 0.40f, 0.75f);
                f101FooterNote.text = "Bấm trực tiếp ô A / B / C trên phiếu rồi ký tên xác nhận.";
                if (hasDraft)
                    AddZone("SIGN_PREVIEW", "Đọc lại trước khi ký", SubUv(0.055f, 0.022f, 0.89f, 0.36f, 0.03f, 0.085f, 0.53f, 0.18f), false, false);
            }
            else if (s.SignStep == 1)
            {
                f101SignBg.color = new Color(0.78f, 0.85f, 0.96f, 1f);
                f101SignText.text = "Bấm để ký:  N. H. Minh";
                f101SignText.color = new Color(0.06f, 0.18f, 0.56f, 1f);
                f101StampBg.color = new Color(0.88f, 0.85f, 0.80f, 0.65f);
                f101StampText.text = "Chờ chữ ký…";
                f101StampText.color = new Color(0.46f, 0.36f, 0.34f, 0.82f);
                f101FooterNote.text = "Bước 1/2: Ký nét bút bi N. H. Minh lên phiếu (có thể sửa lại sau).";
                AddZone("SIGN_NAME", "Ký tên: N. H. Minh", SubUv(0.055f, 0.022f, 0.89f, 0.36f, 0.03f, 0.085f, 0.53f, 0.18f), false, false);
            }
            else
            {
                f101SignBg.color = new Color(0.92f, 0.91f, 0.86f, 1f);
                f101SignText.text = "N. H. Minh   ✓";
                f101SignText.color = new Color(0.06f, 0.22f, 0.66f, 1f);
                f101StampBg.color = new Color(0.94f, 0.82f, 0.80f, 1f);
                f101StampText.text = "ĐÓNG DẤU CA TRỰC";
                f101StampText.color = new Color(0.68f, 0.10f, 0.10f, 1f);
                f101FooterNote.text = "Bước 2/2: Bấm ô đỏ để đóng dấu xác nhận bàn giao ca trực.";
                AddZone("SIGN_STAMP", "Đóng dấu ca trực", SubUv(0.055f, 0.022f, 0.89f, 0.36f, 0.585f, 0.085f, 0.385f, 0.18f), false, true);
            }
        }

        private void RedrawSideSlip(in An02SurfaceState s)
        {
            slipTabNotesBg.color = !s.ShowBranchCSlip ? new Color(0.18f, 0.24f, 0.26f, 0.94f) : new Color(0.84f, 0.79f, 0.66f, 1f);
            slipTabNotesText.color = !s.ShowBranchCSlip ? new Color(0.96f, 0.94f, 0.86f, 1f) : new Color(0.20f, 0.18f, 0.14f, 1f);
            slipTabBranchCBg.color = s.ShowBranchCSlip ? new Color(0.18f, 0.24f, 0.26f, 0.94f) : new Color(0.84f, 0.79f, 0.66f, 1f);
            slipTabBranchCText.color = s.ShowBranchCSlip ? new Color(0.96f, 0.94f, 0.86f, 1f) : new Color(0.20f, 0.18f, 0.14f, 1f);

            AddZone("SLIP_TAB_NOTES", "Ghi chú & Mốc giờ", new Rect(0.05f, 0.88f, 0.44f, 0.045f), onSlip: true, marked: !s.ShowBranchCSlip);
            AddZone("SLIP_TAB_C", "Nguồn nhánh C", new Rect(0.51f, 0.88f, 0.44f, 0.045f), onSlip: true, marked: s.ShowBranchCSlip);

            slipNotesGroup.SetActive(!s.ShowBranchCSlip);
            slipBranchCGroup.SetActive(s.ShowBranchCSlip);

            if (!s.ShowBranchCSlip)
            {
                slipHeaderTitle.text = "GHI CHÚ MÉP & MỐC GIỜ";
                var summary = "GHI NHẬN TẠI PHÒNG 101:\n";
                if (!s.HasObsA && !s.HasObsB)
                    summary += "• Chưa ghi nhận dấu vết sau rèm giường 4.\n(Phương án A và B không yêu cầu quan sát thêm.)";
                else
                {
                    if (s.HasObsA) summary += "• Sau rèm giường 4: chăn gấp vội & đôi dép nhỏ.\n";
                    if (s.HasObsB) summary += "• Thẻ AS-05B: ô ký nhận còn trống" + (s.CardMarked ? " [✓ Đã đánh dấu].\n" : ".\n");
                    summary += s.SpatialLinked
                        ? "• Đối chứng tại hiện trường: ✓ Đã xác thực tại hốc giường 4."
                        : "• Đối chứng tại hiện trường: Chưa đối chứng trực tiếp sau rèm.";
                }
                slipFieldSummary.text = summary;

                slipCardMarkBox.gameObject.SetActive(s.HasObsB);
                if (s.HasObsB)
                {
                    slipCardMarkText.text = s.CardMarked ? "✓ Đã đánh dấu ô ký nhận trên AS-05B" : "Đánh dấu ô ký nhận trên thẻ AS-05B";
                    AddZone("MARK_CARD", s.CardMarked ? "Bỏ đánh dấu thẻ AS-05B" : "Đánh dấu ô ký nhận AS-05B",
                        SubUv(0.05f, 0.04f, 0.90f, 0.82f, 0f, 0.54f, 1f, 0.08f), onSlip: true, marked: s.CardMarked);
                }

                if (s.Ex01Solved)
                {
                    slipEx01Status.text = "MỐC GIỜ NGOẠI LỆ (TÙY CHỌN)\n✓ Đã ghi nhận mốc trùng 03:17 giữa đồng hồ quầy và vết hằn mặt sau thẻ AS-05B.";
                    for (int i = 0; i < 4; i++)
                    {
                        slipDigitBgs[i].color = new Color(0.86f, 0.91f, 0.84f, 1f);
                        slipDigitTexts[i].text = "0317"[i].ToString();
                    }
                    slipEx01ResetBg.gameObject.SetActive(false);
                    slipEx01SubmitBg.gameObject.SetActive(false);
                }
                else
                {
                    slipEx01Status.text =
                        $"MỐC GIỜ NGOẠI LỆ (BẤM Ô HOẶC GÕ 0–9)\n" +
                        $"• Đồng hồ quầy: {(s.Ex01HasClock ? "✓" : "Chưa kiểm")}  ·  Mặt sau AS-05B: {(s.Ex01HasCardTime ? "✓" : "Chưa soi")}  ·  Bối cảnh ca: {(s.Ex01HasShift ? "✓" : "Chưa kiểm")}";
                    float[] xStarts = { 0.05f, 0.26f, 0.54f, 0.75f };
                    for (int i = 0; i < 4; i++)
                    {
                        int v = s.Ex01Digits != null && i < s.Ex01Digits.Length ? s.Ex01Digits[i] : -1;
                        slipDigitTexts[i].text = v >= 0 ? v.ToString() : "-";
                        slipDigitBgs[i].color = s.Ex01Cursor == i
                            ? new Color(0.99f, 0.96f, 0.80f, 1f)
                            : new Color(0.97f, 0.95f, 0.90f, 1f);
                        AddZone("EX01_D" + i, $"Chỉnh chữ số #{i + 1}",
                            SubUv(0.05f, 0.04f, 0.90f, 0.82f, xStarts[i], 0.21f, 0.18f, 0.13f), onSlip: true, marked: v >= 0);
                    }
                    slipEx01ResetBg.gameObject.SetActive(true);
                    slipEx01SubmitBg.gameObject.SetActive(true);
                    AddZone("EX01_RESET", "Xóa mốc giờ", SubUv(0.05f, 0.04f, 0.90f, 0.82f, 0.02f, 0.115f, 0.34f, 0.075f), onSlip: true, marked: false);
                    AddZone("EX01_SUBMIT", "Ghi nhận mốc giờ", SubUv(0.05f, 0.04f, 0.90f, 0.82f, 0.39f, 0.115f, 0.59f, 0.075f), onSlip: true, marked: false);
                }
                slipNotesFeedback.text = string.IsNullOrEmpty(s.Feedback) ? "" : "⚠ " + s.Feedback;
            }
            else
            {
                slipHeaderTitle.text = "CĂN CỨ KẸP NHÁNH C";
                slipBranchCChecklist.text =
                    $"• Khoanh số 0 trên F101: {(s.ZeroMarked ? "✓ Đã khoanh" : "Chưa (bấm số 0 bên trái)")}\n" +
                    $"• Ô ký nhận AS-05B: {(s.CardMarked ? "✓ Đã đánh dấu" : "Chưa đánh dấu")}\n" +
                    $"• Đối chứng tại hốc giường 4: {(s.SpatialLinked ? "✓ Đã xác thực tại hiện trường" : "Chưa (cầm AS-05B ra sau rèm)")}";

                int srcCount = s.SelectableSources != null ? Mathf.Min(s.SelectableSources.Count, slipSourceRows.Count) : 0;
                for (int i = 0; i < slipSourceRows.Count; i++)
                {
                    var row = slipSourceRows[i];
                    if (i < srcCount)
                    {
                        var item = s.SelectableSources[i];
                        row.row.SetActive(true);
                        row.mark.text = item.selected ? "✓" : "";
                        row.label.text = item.label;
                        float yTop = 0.840f - i * 0.056f;
                        AddZone("SRC_" + item.id, (item.selected ? "Gỡ nguồn: " : "Kẹp nguồn: ") + item.label,
                            SubUv(0.05f, 0.04f, 0.90f, 0.82f, 0f, yTop - 0.050f, 1f, 0.050f), onSlip: true, marked: item.selected);
                    }
                    else
                    {
                        row.row.SetActive(false);
                    }
                }

                int clmCount = s.Claims != null ? Mathf.Min(s.Claims.Count, slipClaimRows.Count) : 0;
                for (int i = 0; i < slipClaimRows.Count; i++)
                {
                    var row = slipClaimRows[i];
                    if (i < clmCount)
                    {
                        var item = s.Claims[i];
                        row.row.SetActive(true);
                        row.dot.color = item.selected ? new Color(0.10f, 0.26f, 0.66f, 1f) : new Color(0.98f, 0.97f, 0.92f, 1f);
                        row.label.text = item.label;
                        float yTop = 0.410f - i * 0.062f;
                        AddZone("CLM_" + item.id, item.label,
                            SubUv(0.05f, 0.04f, 0.90f, 0.82f, 0f, yTop - 0.056f, 1f, 0.056f), onSlip: true, marked: item.selected);
                    }
                    else
                    {
                        row.row.SetActive(false);
                    }
                }
                slipBranchCFeedback.text = string.IsNullOrEmpty(s.Feedback) ? "" : "⚠ " + s.Feedback;
            }
        }

        private void RenderOffscreenNow()
        {
            Canvas.ForceUpdateCanvases();
            if (f101Cam != null) f101Cam.Render();
            if (slipCam != null) slipCam.Render();
        }

        private void AddZone(string id, string label, Rect uv, bool onSlip, bool marked) =>
            activeZones.Add(new ClickZone { Id = id, Label = label, Uv = uv, OnSlip = onSlip, Marked = marked });

        private static Rect SubUv(float px, float py, float pw, float ph, float lx, float ly, float lw, float lh) =>
            new(px + lx * pw, py + ly * ph, lw * pw, lh * ph);

        private Image MakePanel(RectTransform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var rt = RectOf(parent, name, min, max);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static RectTransform RectOf(RectTransform parent, string name, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        private Text MakeText(RectTransform parent, string content, int size, Color color, TextAnchor align, bool bold)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8, 2); rt.offsetMax = new Vector2(-8, -2);
            var t = go.GetComponent<Text>();
            t.font = bold && uiBold != null ? uiBold : uiFont;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.text = content;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.lineSpacing = 1.12f;
            t.raycastTarget = false;
            return t;
        }

        private static void SetLayerRecursive(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLayerRecursive(child.gameObject, layer);
        }

        private void OnDestroy()
        {
            Close();
            if (f101Rt != null) { f101Rt.Release(); Destroy(f101Rt); }
            if (slipRt != null) { slipRt.Release(); Destroy(slipRt); }
            if (f101SurfaceMat != null) Destroy(f101SurfaceMat);
            if (slipSurfaceMat != null) Destroy(slipSurfaceMat);
            if (boardBackMat != null) Destroy(boardBackMat);
            if (clipBarMat != null) Destroy(clipBarMat);
            if (worldRoot != null) Destroy(worldRoot.gameObject);
            if (f101OffscreenRoot != null) Destroy(f101OffscreenRoot);
            if (slipOffscreenRoot != null) Destroy(slipOffscreenRoot);
        }
    }
}
