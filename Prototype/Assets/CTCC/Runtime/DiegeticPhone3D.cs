using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CTCC
{
    public enum PhoneScreenMode
    {
        Inbox = 0,
        ReadPostponeSms = 1,
        ReadAppointmentSms = 2,
        CallingBay = 3
    }

    public enum PhoneButtonKind
    {
        None = 0,
        Up = 1,
        Down = 2,
        Ok = 3,
        SoftLeft = 4,
        Call = 5,
        End = 6,
        Back = 7
    }

    /// <summary>
    /// Phase 2.5 True Diegetic 2012 Vietnamese Feature Phone (Requirement F):
    /// - Personal device owned by Minh from game start, completely independent of the E10a envelope.
    /// - Spawns 8-part CTCC_PROP_MinhPhone with physically clickable 3D keypad buttons (inward travel + tactile click SFX)
    ///   and a REAL dynamic on-demand RenderTexture (512x640) monochrome-teal backlit LCD screen (0.0 ms idle GPU overhead).
    /// - ZERO sidebar in default mode; passive Inbox view NEVER auto-marks any SMS read; opening each SMS on the physical LCD
    ///   commits its exact epistemic flag (PostponementSms / SmsAppointmentViewed); pressing physical green Call dials Bảy!
    /// </summary>
    public sealed class DiegeticPhone3D : MonoBehaviour
    {
        private const int OffscreenLayer = 31;

        private Camera eye;
        private EnvironmentalAudio audioBus;
        private Transform stageRoot;
        private Transform holder;
        private Light keyLight;
        private Light rimLight;

        private GameObject phoneInst;
        private Material screenMat;
        private RenderTexture lcdRt;
        private GameObject lcdStageRoot;
        private Camera lcdCam;
        private Font uiFont, uiBold;

        // Live LCD uGUI widgets (rendered on demand into lcdRt)
        private Text lcdClock, lcdHeader, lcdSoftL, lcdSoftMid, lcdSoftR;
        private GameObject lcdInboxGroup, lcdReaderGroup, lcdCallGroup;
        private Image lcdRow0Bg, lcdRow1Bg;
        private Text lcdRow0Title, lcdRow0Sub, lcdRow1Title, lcdRow1Sub;
        private Text lcdReaderSender, lcdReaderTime, lcdReaderBody;
        private Text lcdCallStatus, lcdCallName, lcdCallSub;

        // Clickable physical 3D keypad buttons
        private readonly Dictionary<Collider, (PhoneButtonKind kind, Transform tf, Vector3 restPos, string label)> keyButtons = new();
        private readonly Dictionary<PhoneButtonKind, float> pressTimers = new();

        private bool active;
        private bool reducedMotion;
        private bool sharpnessAcquired;
        private int openedFrame = -1;
        private float rotX = 4f, rotY = -6f, zoom = 0.255f, zoomTarget = 0.255f;
        private bool lmbDown, lmbDragging;
        private Vector2 lmbStart;
        private PhoneButtonKind hoveredKey;

        private PhoneScreenMode screenMode = PhoneScreenMode.Inbox;
        private int inboxCursor;
        private bool readPostponeFlag;
        private bool readApptFlag;
        private bool calledBayFlag;
        private float callAnimTimer;
        private bool callFinishedNoAnswer;
        private string smsPostponeText = "";
        private string smsApptText = "";
        private Action<PhoneScreenMode> onScreenEntered;
        private Action onDialBay;
        private Action onCloseRequested;

        public bool IsOpen => active;
        public PhoneScreenMode CurrentScreen => screenMode;
        public int InboxCursor => inboxCursor;
        public PhoneButtonKind HoveredKey => hoveredKey;
        public bool InputBlocked { get; set; }

        public void Initialize(Camera camera, EnvironmentalAudio audio)
        {
            eye = camera;
            audioBus = audio;
            uiFont = Resources.Load<Font>("CTCC/BeVietnamPro-Regular");
            uiBold = Resources.Load<Font>("CTCC/BeVietnamPro-SemiBold");

            // Exclude offscreen LCD layer from player's main FPS camera so it never appears in the world!
            if (eye != null) eye.cullingMask &= ~(1 << OffscreenLayer);

            stageRoot = new GameObject("CTCC_DiegeticPhone3D").transform;
            stageRoot.SetParent(eye.transform, false);
            stageRoot.localPosition = new Vector3(0f, -0.008f, 0.255f);

            holder = new GameObject("PhoneHolder").transform;
            holder.SetParent(stageRoot, false);

            var kGo = new GameObject("PhoneKeyLight");
            kGo.transform.SetParent(eye.transform, false);
            kGo.transform.localPosition = new Vector3(0.18f, 0.20f, 0.04f);
            keyLight = kGo.AddComponent<Light>();
            keyLight.type = LightType.Point;
            keyLight.range = 1.4f;
            keyLight.intensity = 0.65f;
            keyLight.color = new Color(0.95f, 0.96f, 0.92f);
            keyLight.shadows = LightShadows.None;

            var rGo = new GameObject("PhoneRimLight");
            rGo.transform.SetParent(eye.transform, false);
            rGo.transform.localPosition = new Vector3(-0.20f, -0.06f, 0.26f);
            rimLight = rGo.AddComponent<Light>();
            rimLight.type = LightType.Point;
            rimLight.range = 1.3f;
            rimLight.intensity = 0.34f;
            rimLight.color = new Color(0.68f, 0.86f, 0.88f);
            rimLight.shadows = LightShadows.None;

            BuildOffscreenLcdCanvas();
            stageRoot.gameObject.SetActive(false);
            keyLight.enabled = false;
            rimLight.enabled = false;
        }

        public void SetReducedMotion(bool reduced) => reducedMotion = reduced;

        public void Open(
            GameObject template,
            Volume activeGrade,
            string postponeCopy,
            string apptCopy,
            bool readPostpone,
            bool readAppt,
            bool calledBay,
            PhoneScreenMode startScreen,
            Action<PhoneScreenMode> onEnterScreen,
            Action onDial,
            Action onClose)
        {
            Close();
            active = true;
            openedFrame = Time.frameCount;
            smsPostponeText = postponeCopy ?? "";
            smsApptText = apptCopy ?? "";
            readPostponeFlag = readPostpone;
            readApptFlag = readAppt;
            calledBayFlag = calledBay;
            screenMode = startScreen;
            inboxCursor = startScreen == PhoneScreenMode.ReadAppointmentSms ? 1 : 0;
            callAnimTimer = 0f;
            callFinishedNoAnswer = false;
            rotX = 4f;
            rotY = -5f;
            zoom = zoomTarget = 0.255f;
            lmbDown = lmbDragging = false;
            hoveredKey = PhoneButtonKind.None;
            onScreenEntered = onEnterScreen;
            onDialBay = onDial;
            onCloseRequested = onClose;

            BuildPhoneModel(template);
            InspectionSharpnessGuard.Acquire(activeGrade, allowBackgroundSoften: !reducedMotion);
            sharpnessAcquired = true;

            stageRoot.gameObject.SetActive(true);
            keyLight.enabled = true;
            rimLight.enabled = true;
            ApplyTransforms();
            RefreshLcd();
        }

        public void SyncFlags(bool readPostpone, bool readAppt, bool calledBay)
        {
            readPostponeFlag = readPostpone;
            readApptFlag = readAppt;
            calledBayFlag = calledBay;
            if (active) RefreshLcd();
        }

        public void PressButton(PhoneButtonKind kind)
        {
            if (!active || InputBlocked || kind == PhoneButtonKind.None) return;
            pressTimers[kind] = 0.14f;
            if (eye != null)
                audioBus?.From("latchClick", eye.transform.position + eye.transform.forward * 0.24f, 0.26f);

            switch (kind)
            {
                case PhoneButtonKind.Up:
                    if (screenMode == PhoneScreenMode.Inbox)
                    {
                        inboxCursor = (inboxCursor + 1) % 2;
                        RefreshLcd();
                    }
                    break;
                case PhoneButtonKind.Down:
                    if (screenMode == PhoneScreenMode.Inbox)
                    {
                        inboxCursor = (inboxCursor + 1) % 2;
                        RefreshLcd();
                    }
                    break;
                case PhoneButtonKind.Ok:
                case PhoneButtonKind.SoftLeft:
                    if (screenMode == PhoneScreenMode.Inbox)
                    {
                        screenMode = inboxCursor == 0 ? PhoneScreenMode.ReadPostponeSms : PhoneScreenMode.ReadAppointmentSms;
                        RefreshLcd();
                        onScreenEntered?.Invoke(screenMode);
                    }
                    else if (screenMode is (PhoneScreenMode.ReadPostponeSms or PhoneScreenMode.ReadAppointmentSms))
                    {
                        StartCallOnPhone();
                    }
                    else if (screenMode == PhoneScreenMode.CallingBay && callFinishedNoAnswer)
                    {
                        StartCallOnPhone();
                    }
                    break;
                case PhoneButtonKind.Call:
                    StartCallOnPhone();
                    break;
                case PhoneButtonKind.Back:
                    if (screenMode != PhoneScreenMode.Inbox)
                    {
                        screenMode = PhoneScreenMode.Inbox;
                        callAnimTimer = 0f;
                        RefreshLcd();
                    }
                    else
                    {
                        onCloseRequested?.Invoke();
                    }
                    break;
                case PhoneButtonKind.End:
                    if (screenMode == PhoneScreenMode.CallingBay)
                    {
                        screenMode = PhoneScreenMode.Inbox;
                        callAnimTimer = 0f;
                        RefreshLcd();
                    }
                    else
                    {
                        onCloseRequested?.Invoke();
                    }
                    break;
            }
        }

        private void StartCallOnPhone()
        {
            screenMode = PhoneScreenMode.CallingBay;
            callAnimTimer = 0.01f;
            callFinishedNoAnswer = false;
            RefreshLcd();
            onDialBay?.Invoke();
        }

        public void Close()
        {
            active = false;
            keyButtons.Clear();
            pressTimers.Clear();
            if (phoneInst != null)
            {
                Destroy(phoneInst);
                phoneInst = null;
            }
            if (screenMat != null)
            {
                Destroy(screenMat);
                screenMat = null;
            }
            onScreenEntered = null;
            onDialBay = null;
            onCloseRequested = null;
            if (stageRoot != null) stageRoot.gameObject.SetActive(false);
            if (keyLight != null) keyLight.enabled = false;
            if (rimLight != null) rimLight.enabled = false;
            if (sharpnessAcquired)
            {
                InspectionSharpnessGuard.Release();
                sharpnessAcquired = false;
            }
        }

        public void ProjectHotspots(List<ProjectedHotspot3D> output)
        {
            output.Clear();
            if (!active || eye == null) return;
            foreach (var kv in keyButtons)
            {
                var col = kv.Key;
                if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) continue;
                var kind = kv.Value.kind;
                // Only show the 4 main ergonomic hardware action dots (Up/Down/OK/Call/End) so the phone stays clean!
                if (kind == PhoneButtonKind.SoftLeft) continue;
                Vector3 sp = eye.WorldToScreenPoint(col.bounds.center);
                if (sp.z <= 0.05f) continue;
                string lbl = kind switch
                {
                    PhoneButtonKind.Up => "Lên",
                    PhoneButtonKind.Down => "Xuống",
                    PhoneButtonKind.Ok => screenMode == PhoneScreenMode.Inbox ? "Đọc tin nhắn" : "Gọi chú Bảy",
                    PhoneButtonKind.Call => "Gọi chú Bảy",
                    PhoneButtonKind.Back => screenMode == PhoneScreenMode.Inbox ? "Cất máy" : "Quay lại",
                    PhoneButtonKind.End => screenMode == PhoneScreenMode.CallingBay ? "Ngắt gọi" : "Cất máy",
                    _ => kv.Value.label
                };
                output.Add(new ProjectedHotspot3D("PHONE_" + kind, lbl, sp, hoveredKey == kind, false));
            }
        }

        private void BuildPhoneModel(GameObject template)
        {
            keyButtons.Clear();
            if (template == null) return;
            phoneInst = Instantiate(template, holder, false);
            phoneInst.name = "DiegeticPhone3DInstance";
            phoneInst.SetActive(true);
            phoneInst.transform.localPosition = Vector3.zero;
            phoneInst.transform.localRotation = Quaternion.identity;
            phoneInst.transform.localScale = Vector3.one * 1.55f; // Crisp comfortable reading scale at 0.255m!
            foreach (var c in phoneInst.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            foreach (var r in phoneInst.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;

            // Replace static Doc_PhoneScreen material on PhoneScreenFace with live emissive RenderTexture LCD!
            Transform screenFace = FindPart(phoneInst.transform, "PhoneScreenFace");
            if (screenFace == null)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "PhoneScreenFace";
                var qc = q.GetComponent<Collider>();
                if (qc != null) Destroy(qc);
                q.transform.SetParent(phoneInst.transform, false);
                q.transform.localPosition = new Vector3(0f, 0.023f, -0.0096f);
                q.transform.localScale = new Vector3(0.0415f, 0.0515f, 1f);
                screenFace = q.transform;
            }
            else
            {
                // Push screen face 1.2mm clear of phone bezel to guarantee zero z-fighting
                screenFace.localPosition += new Vector3(0f, 0f, -0.0014f);
            }
            var screenRend = screenFace.GetComponent<Renderer>();
            if (screenRend != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit")
                             ?? Shader.Find("Universal Render Pipeline/Unlit")
                             ?? Shader.Find("Sprites/Default");
                screenMat = new Material(shader) { name = "DiegeticPhoneLiveLcdMat" };
                screenMat.SetTexture("_BaseMap", lcdRt);
                screenMat.SetColor("_BaseColor", new Color(0.92f, 0.98f, 0.94f, 1f));
                screenMat.SetFloat("_Metallic", 0f);
                screenMat.SetFloat("_Smoothness", 0.32f);
                screenMat.SetTexture("_EmissionMap", lcdRt);
                screenMat.SetColor("_EmissionColor", new Color(0.92f, 0.98f, 0.94f, 1f));
                screenMat.EnableKeyword("_EMISSION");
                screenRend.sharedMaterial = screenMat;
                screenRend.shadowCastingMode = ShadowCastingMode.Off;
            }

            // Bind clickable 3D physical keys!
            BindKey(phoneInst.transform, "PhoneKeyUp", PhoneButtonKind.Up, "Lên");
            BindKey(phoneInst.transform, "PhoneKeyDown", PhoneButtonKind.Down, "Xuống");
            BindKey(phoneInst.transform, "PhoneKeyOk", PhoneButtonKind.Ok, "Chọn / Đọc");
            BindKey(phoneInst.transform, "PhoneKeySoftL", PhoneButtonKind.SoftLeft, "Tùy chọn");
            BindKey(phoneInst.transform, "PhoneKeyCall", PhoneButtonKind.Call, "Gọi chú Bảy");
            BindKey(phoneInst.transform, "PhoneKeyEnd", PhoneButtonKind.End, "Kết thúc / Cất máy");
            BindKey(phoneInst.transform, "PhoneKeyBack", PhoneButtonKind.Back, "Quay lại");
        }

        private void BindKey(Transform root, string partName, PhoneButtonKind kind, string label)
        {
            var tf = FindPart(root, partName);
            if (tf == null) return;
            var mf = tf.GetComponentInChildren<MeshFilter>(true);
            var host = mf != null ? mf.gameObject : tf.gameObject;
            var col = host.GetComponent<BoxCollider>();
            if (col == null) col = host.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.enabled = true;
            col.size = new Vector3(
                Mathf.Max(col.size.x, 0.011f),
                Mathf.Max(col.size.y, 0.006f),
                Mathf.Max(col.size.z, 0.006f));
            keyButtons[col] = (kind, tf, tf.localPosition, label);
        }

        private static Transform FindPart(Transform root, string prefix)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (string.Equals(t.name, prefix, StringComparison.OrdinalIgnoreCase) ||
                    t.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return t;
            return null;
        }

        private void BuildOffscreenLcdCanvas()
        {
            lcdRt = new RenderTexture(512, 640, 16, RenderTextureFormat.ARGB32)
            {
                name = "DiegeticPhoneLcdRT",
                filterMode = FilterMode.Trilinear,
                anisoLevel = 16
            };
            lcdRt.Create();

            lcdStageRoot = new GameObject("CTCC_PhoneLcdOffscreen");
            lcdStageRoot.transform.position = new Vector3(0f, -600f, 0f);
            SetLayerRecursive(lcdStageRoot, OffscreenLayer);

            var camGo = new GameObject("PhoneLcdCam");
            camGo.transform.SetParent(lcdStageRoot.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0f, -10f);
            lcdCam = camGo.AddComponent<Camera>();
            lcdCam.orthographic = true;
            lcdCam.orthographicSize = 5f;
            lcdCam.clearFlags = CameraClearFlags.SolidColor;
            lcdCam.backgroundColor = new Color(0.11f, 0.25f, 0.24f, 1f);
            lcdCam.cullingMask = 1 << OffscreenLayer;
            lcdCam.targetTexture = lcdRt;
            lcdCam.enabled = false; // On-demand Render() only = 0.0ms idle cost!

            var canvasGo = new GameObject("PhoneLcdCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(lcdStageRoot.transform, false);
            canvasGo.layer = OffscreenLayer;
            var cv = canvasGo.GetComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceCamera;
            cv.worldCamera = lcdCam;
            cv.planeDistance = 5f;
            var sc = canvasGo.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(512, 640);
            var root = canvasGo.GetComponent<RectTransform>();

            // Retro 2012 monochrome-teal backlit LCD frame
            LcdImg(root, "Bg", Vector2.zero, Vector2.one, new Color(0.11f, 0.25f, 0.24f, 1f));
            LcdImg(root, "TopBar", new Vector2(0, 0.885f), Vector2.one, new Color(0.07f, 0.16f, 0.16f, 1f));
            LcdText(root, "Carrier", new Vector2(0.04f, 0.89f), new Vector2(0.55f, 0.99f), "VN-MOBI  ▂▄▆", 22, true, TextAnchor.MiddleLeft, new Color(0.76f, 0.94f, 0.86f));
            lcdClock = LcdText(root, "Clock", new Vector2(0.58f, 0.89f), new Vector2(0.96f, 0.99f), "22:50", 24, true, TextAnchor.MiddleRight, new Color(0.92f, 0.98f, 0.93f));
            lcdHeader = LcdText(root, "Header", new Vector2(0.04f, 0.79f), new Vector2(0.96f, 0.88f), "HỘP THƯ ĐẾN", 23, true, TextAnchor.MiddleCenter, new Color(0.90f, 0.97f, 0.90f));
            LcdImg(root, "RuleTop", new Vector2(0.05f, 0.785f), new Vector2(0.95f, 0.790f), new Color(0.42f, 0.64f, 0.58f, 0.85f));

            // 1) Inbox list screen
            lcdInboxGroup = LcdRect(root, "InboxGroup", new Vector2(0.04f, 0.13f), new Vector2(0.96f, 0.77f)).gameObject;
            var r0 = LcdRect(lcdInboxGroup.transform, "Row0", new Vector2(0, 0.52f), new Vector2(1, 0.97f));
            lcdRow0Bg = LcdImg(r0, "Bg", Vector2.zero, Vector2.one, new Color(0.16f, 0.36f, 0.33f, 1f));
            lcdRow0Title = LcdText(r0, "T", new Vector2(0.04f, 0.54f), new Vector2(0.96f, 0.94f), "", 23, true, TextAnchor.MiddleLeft, new Color(0.98f, 0.95f, 0.74f));
            lcdRow0Sub = LcdText(r0, "S", new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.56f), "", 21, false, TextAnchor.UpperLeft, new Color(0.92f, 0.97f, 0.94f));

            var r1 = LcdRect(lcdInboxGroup.transform, "Row1", new Vector2(0, 0.04f), new Vector2(1, 0.49f));
            lcdRow1Bg = LcdImg(r1, "Bg", Vector2.zero, Vector2.one, new Color(0.09f, 0.21f, 0.20f, 1f));
            lcdRow1Title = LcdText(r1, "T", new Vector2(0.04f, 0.54f), new Vector2(0.96f, 0.94f), "", 23, true, TextAnchor.MiddleLeft, new Color(0.82f, 0.94f, 0.88f));
            lcdRow1Sub = LcdText(r1, "S", new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.56f), "", 21, false, TextAnchor.UpperLeft, new Color(0.82f, 0.91f, 0.86f));

            // 2) SMS Reader screen
            lcdReaderGroup = LcdRect(root, "ReaderGroup", new Vector2(0.05f, 0.13f), new Vector2(0.95f, 0.77f)).gameObject;
            lcdReaderSender = LcdText(lcdReaderGroup.transform, "Sender", new Vector2(0, 0.84f), new Vector2(1, 0.98f), "", 23, true, TextAnchor.MiddleLeft, new Color(0.98f, 0.95f, 0.74f));
            lcdReaderTime = LcdText(lcdReaderGroup.transform, "Time", new Vector2(0, 0.73f), new Vector2(1, 0.85f), "", 19, false, TextAnchor.MiddleLeft, new Color(0.72f, 0.88f, 0.82f));
            lcdReaderBody = LcdText(lcdReaderGroup.transform, "Body", new Vector2(0, 0.02f), new Vector2(1, 0.71f), "", 22, false, TextAnchor.UpperLeft, new Color(0.95f, 0.98f, 0.95f));
            lcdReaderBody.lineSpacing = 1.15f;
            lcdReaderGroup.SetActive(false);

            // 3) Calling screen
            lcdCallGroup = LcdRect(root, "CallGroup", new Vector2(0.05f, 0.13f), new Vector2(0.95f, 0.77f)).gameObject;
            lcdCallStatus = LcdText(lcdCallGroup.transform, "Status", new Vector2(0, 0.66f), new Vector2(1, 0.86f), "ĐANG GỌI...", 26, true, TextAnchor.MiddleCenter, new Color(0.98f, 0.95f, 0.74f));
            lcdCallName = LcdText(lcdCallGroup.transform, "Name", new Vector2(0, 0.44f), new Vector2(1, 0.64f), "Chú Bảy", 32, true, TextAnchor.MiddleCenter, new Color(0.95f, 0.98f, 0.95f));
            lcdCallSub = LcdText(lcdCallGroup.transform, "Sub", new Vector2(0, 0.24f), new Vector2(1, 0.42f), "0913 ••• 407", 22, false, TextAnchor.MiddleCenter, new Color(0.74f, 0.90f, 0.84f));
            lcdCallGroup.SetActive(false);

            // Bottom authentic 2012 Vietnamese softkey strip (ZERO PC hotkeys!)
            LcdImg(root, "SoftBar", Vector2.zero, new Vector2(1, 0.115f), new Color(0.06f, 0.14f, 0.14f, 1f));
            lcdSoftL = LcdText(root, "SoftL", new Vector2(0.04f, 0.01f), new Vector2(0.36f, 0.11f), "Đọc", 22, true, TextAnchor.MiddleLeft, new Color(0.84f, 0.96f, 0.88f));
            lcdSoftMid = LcdText(root, "SoftM", new Vector2(0.34f, 0.01f), new Vector2(0.66f, 0.11f), "Gọi", 22, true, TextAnchor.MiddleCenter, new Color(0.98f, 0.95f, 0.74f));
            lcdSoftR = LcdText(root, "SoftR", new Vector2(0.64f, 0.01f), new Vector2(0.96f, 0.11f), "Thoát", 22, true, TextAnchor.MiddleRight, new Color(0.84f, 0.96f, 0.88f));

            SetLayerRecursive(lcdStageRoot, OffscreenLayer);
        }

        private void RefreshLcd()
        {
            if (lcdCam == null) return;
            lcdInboxGroup.SetActive(screenMode == PhoneScreenMode.Inbox);
            lcdReaderGroup.SetActive(screenMode is (PhoneScreenMode.ReadPostponeSms or PhoneScreenMode.ReadAppointmentSms));
            lcdCallGroup.SetActive(screenMode == PhoneScreenMode.CallingBay);

            if (screenMode == PhoneScreenMode.Inbox)
            {
                lcdHeader.text = "HỘP THƯ ĐẾN (2)";
                Color selBg = new(0.18f, 0.42f, 0.37f, 1f);
                Color idleBg = new(0.09f, 0.21f, 0.20f, 1f);
                lcdRow0Bg.color = inboxCursor == 0 ? selBg : idleBg;
                lcdRow1Bg.color = inboxCursor == 1 ? selBg : idleBg;
                lcdRow0Title.text = (readPostponeFlag ? "  " : "● ") + "Chú Bảy · 22:48";
                lcdRow0Sub.text = "Tôi đang ở viện. Hoãn nhé...";
                lcdRow1Title.text = (readApptFlag ? "  " : "● ") + "Chú Bảy · 16/11";
                lcdRow1Sub.text = "Minh à, dọn kho thanh lý...";
                lcdSoftL.text = "Đọc";
                lcdSoftMid.text = "Gọi";
                lcdSoftR.text = "Thoát";
            }
            else if (screenMode == PhoneScreenMode.ReadPostponeSms)
            {
                lcdHeader.text = "TIN NHẮN ĐẾN";
                lcdReaderSender.text = "Chú Bảy";
                lcdReaderTime.text = "17/11/2012 · 22:48";
                lcdReaderBody.text = smsPostponeText;
                lcdSoftL.text = "Gọi Bảy";
                lcdSoftMid.text = "";
                lcdSoftR.text = "Trở về";
            }
            else if (screenMode == PhoneScreenMode.ReadAppointmentSms)
            {
                lcdHeader.text = "TIN NHẮN ĐẾN";
                lcdReaderSender.text = "Chú Bảy";
                lcdReaderTime.text = "16/11/2012 · 19:15";
                lcdReaderBody.text = smsApptText;
                lcdSoftL.text = "Gọi Bảy";
                lcdSoftMid.text = "";
                lcdSoftR.text = "Trở về";
            }
            else if (screenMode == PhoneScreenMode.CallingBay)
            {
                lcdHeader.text = "CUỘC GỌI ĐI";
                if (!callFinishedNoAnswer)
                {
                    int dots = Mathf.FloorToInt(callAnimTimer * 2.2f) % 4;
                    lcdCallStatus.text = "ĐANG GỌI" + new string('.', dots);
                    lcdCallSub.text = "Đang đổ chuông...";
                    lcdSoftL.text = "";
                    lcdSoftMid.text = "";
                    lcdSoftR.text = "Ngắt";
                }
                else
                {
                    lcdCallStatus.text = "KHÔNG TRẢ LỜI";
                    lcdCallSub.text = "Chuông đổ rồi tự ngắt";
                    lcdSoftL.text = "Gọi lại";
                    lcdSoftMid.text = "";
                    lcdSoftR.text = "Trở về";
                }
            }

            Canvas.ForceUpdateCanvases();
            lcdCam.Render();
        }

        private void Update()
        {
            if (!active || stageRoot == null) return;
            float dt = Time.unscaledDeltaTime;
            zoom = Mathf.MoveTowards(zoom, zoomTarget, dt * (reducedMotion ? 5f : 0.9f));

            // Animate physical keypad spring travel
            if (pressTimers.Count > 0)
            {
                var keysToUpdate = new List<PhoneButtonKind>(pressTimers.Keys);
                foreach (var k in keysToUpdate)
                {
                    float t = pressTimers[k] - dt;
                    if (t <= 0f) pressTimers.Remove(k);
                    else pressTimers[k] = t;
                }
            }
            foreach (var kv in keyButtons)
            {
                var (kind, tf, restPos, _) = kv.Value;
                if (tf == null) continue;
                float depressed = pressTimers.TryGetValue(kind, out float t) ? Mathf.Clamp01(t / 0.14f) : 0f;
                tf.localPosition = restPos + new Vector3(0f, 0f, depressed * 0.0014f);
            }

            // Animate live dialing status on 3D LCD
            if (screenMode == PhoneScreenMode.CallingBay && !callFinishedNoAnswer && callAnimTimer > 0f)
            {
                int prevTick = Mathf.FloorToInt(callAnimTimer * 2.2f);
                callAnimTimer += dt;
                int nowTick = Mathf.FloorToInt(callAnimTimer * 2.2f);
                if (callAnimTimer >= 3.2f)
                {
                    callFinishedNoAnswer = true;
                    RefreshLcd();
                }
                else if (nowTick != prevTick)
                {
                    RefreshLcd();
                }
            }

            ApplyTransforms();

            if (InputBlocked || Time.frameCount == openedFrame) return;

            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 pos = mouse.position.ReadValue();
                bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                hoveredKey = overUi ? PhoneButtonKind.None : RaycastKey(pos);

                if (mouse.leftButton.wasPressedThisFrame && !overUi)
                {
                    lmbDown = true;
                    lmbDragging = false;
                    lmbStart = pos;
                }
                if (lmbDown && mouse.leftButton.isPressed)
                {
                    if (!lmbDragging && (pos - lmbStart).sqrMagnitude > 36f)
                        lmbDragging = true;
                    if (lmbDragging)
                    {
                        Vector2 d = mouse.delta.ReadValue();
                        rotY = Mathf.Clamp(rotY - d.x * 0.30f, -42f, 42f);
                        rotX = Mathf.Clamp(rotX + d.y * 0.24f, -32f, 32f);
                    }
                }
                if (lmbDown && mouse.leftButton.wasReleasedThisFrame)
                {
                    bool click = !lmbDragging;
                    lmbDown = lmbDragging = false;
                    if (click && !overUi)
                    {
                        var hit = RaycastKey(pos);
                        if (hit != PhoneButtonKind.None) PressButton(hit);
                    }
                }

                float scroll = mouse.scroll.ReadValue().y;
                if (!overUi && Mathf.Abs(scroll) > 0.01f)
                    zoomTarget = Mathf.Clamp(zoomTarget - Mathf.Sign(scroll) * 0.018f, 0.195f, 0.36f);
            }

            var keys = Keyboard.current;
            if (keys != null)
            {
                if (keys.upArrowKey.wasPressedThisFrame || keys.wKey.wasPressedThisFrame) PressButton(PhoneButtonKind.Up);
                else if (keys.downArrowKey.wasPressedThisFrame || keys.sKey.wasPressedThisFrame) PressButton(PhoneButtonKind.Down);
                else if (keys.enterKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame) PressButton(PhoneButtonKind.Ok);
                else if (keys.cKey.wasPressedThisFrame) PressButton(PhoneButtonKind.Call);
                else if (keys.backspaceKey.wasPressedThisFrame) PressButton(PhoneButtonKind.Back);
            }
        }

        private PhoneButtonKind RaycastKey(Vector2 pointer)
        {
            if (eye == null) return PhoneButtonKind.None;
            Ray ray = eye.ScreenPointToRay(pointer);
            float bestDist = float.MaxValue;
            PhoneButtonKind best = PhoneButtonKind.None;
            foreach (var kv in keyButtons)
            {
                if (kv.Key == null || !kv.Key.enabled || !kv.Key.gameObject.activeInHierarchy) continue;
                if (kv.Key.Raycast(ray, out var hit, 2.0f) && hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    best = kv.Value.kind;
                }
            }
            return best;
        }

        private void ApplyTransforms()
        {
            stageRoot.localPosition = new Vector3(0f, -0.008f, zoom);
            float sway = reducedMotion ? 0f : Mathf.Sin(Time.unscaledTime * 1.15f) * 0.65f;
            holder.localRotation = Quaternion.Euler(rotX + sway * 0.3f, rotY + sway, 0f);
        }

        private static RectTransform LcdRect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static Image LcdImg(Transform parent, string name, Vector2 min, Vector2 max, Color col)
        {
            var rt = LcdRect(parent, name, min, max);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = col;
            img.raycastTarget = false;
            return img;
        }

        private Text LcdText(Transform parent, string name, Vector2 min, Vector2 max, string copy, int size, bool bold, TextAnchor align, Color col)
        {
            var rt = LcdRect(parent, name, min, max);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = bold && uiBold != null ? uiBold : (uiFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            t.fontSize = size;
            t.alignment = align;
            t.color = col;
            t.text = copy;
            t.raycastTarget = false;
            return t;
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursive(child.gameObject, layer);
        }

        private void OnDestroy()
        {
            Close();
            if (stageRoot != null) Destroy(stageRoot.gameObject);
            if (keyLight != null) Destroy(keyLight.gameObject);
            if (rimLight != null) Destroy(rimLight.gameObject);
            if (lcdStageRoot != null) Destroy(lcdStageRoot);
            if (lcdRt != null)
            {
                lcdRt.Release();
                Destroy(lcdRt);
            }
        }
    }
}
