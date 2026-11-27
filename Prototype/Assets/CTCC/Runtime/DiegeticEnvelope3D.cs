using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace CTCC
{
    public enum EnvelopePartKind
    {
        None = 0,
        Flap = 1,
        BayNote = 2,
        LanLetter = 3,
        Newspaper = 4,
        WingCKey = 5
    }

    /// <summary>
    /// Phase 2.5 Physical Envelope 3D Interactive Controller (Requirement E):
    /// - Completely eliminates tab-based and ActionChoice envelope menus.
    /// - Spawns CTCC_PROP_EnvelopeInteractive dead-center under InspectionSharpnessGuard (0 blur, 0 sidebar).
    /// - Player rotates the 3D kraft envelope, clicks the external tucked Bay note directly, clicks/drags the top flap open
    ///   (142 deg hinged animation + contents sliding out), and clicks the folded 2005 letter, 19/11/2005 newspaper, or Wing-C key
    ///   to pull them out and unfold/read them on the physical 3D object.
    /// - Strictly preserves the E10a epistemic contract: opening the flap NEVER auto-reads any part (0/4).
    /// </summary>
    public sealed class DiegeticEnvelope3D : MonoBehaviour
    {
        // Harness-only fault-injection hook for v36-p25-envelope-autoread negative probe (false in all production runs!)
        internal static bool QaInjectEnvelopeAutoReadForTesting;

        private Camera eye;
        private EnvironmentalAudio audioBus;
        private Func<string, Texture> textureLookup;
        private Transform stageRoot;
        private Transform holder;
        private Light keyLight;
        private Light rimLight;

        private GameObject envelopeInst;
        private Transform flapPivot;
        private Transform noteSlot;
        private Transform letterSlot;
        private Transform newspaperSlot;
        private Transform keySlot;
        private Vector3 letterBasePos, newspaperBasePos, keyBasePos;
        private Material frontFaceMat;

        private readonly Dictionary<Collider, (EnvelopePartKind kind, string label)> partColliders = new();
        private bool active;
        private bool flapOpen;
        private float flapAngle;
        private float slideOut;
        private bool reducedMotion;
        private bool sharpnessAcquired;
        private int openedFrame = -1;
        private float rotX = 6f, rotY = -8f, zoom = 0.34f, zoomTarget = 0.34f;
        private bool lmbDown, lmbDragging;
        private Vector2 lmbStart;
        private EnvelopePartKind hoveredPart;
        private Action<EnvelopePartKind> onPartSelected;
        private Action onFlapOpened;

        public bool IsOpen => active;
        public bool IsFlapOpen => flapOpen;
        public EnvelopePartKind HoveredPart => hoveredPart;
        public bool InputBlocked { get; set; }

        public void Initialize(Camera camera, EnvironmentalAudio audio, Func<string, Texture> resolveTexture)
        {
            eye = camera;
            audioBus = audio;
            textureLookup = resolveTexture;

            stageRoot = new GameObject("CTCC_DiegeticEnvelope3D").transform;
            stageRoot.SetParent(eye.transform, false);
            stageRoot.localPosition = new Vector3(0f, -0.01f, 0.34f);

            holder = new GameObject("EnvelopeHolder").transform;
            holder.SetParent(stageRoot, false);

            var kGo = new GameObject("EnvelopeKeyLight");
            kGo.transform.SetParent(eye.transform, false);
            kGo.transform.localPosition = new Vector3(0.22f, 0.24f, 0.08f);
            keyLight = kGo.AddComponent<Light>();
            keyLight.type = LightType.Point;
            keyLight.range = 1.6f;
            keyLight.intensity = 0.72f;
            keyLight.color = new Color(1.0f, 0.95f, 0.86f);
            keyLight.shadows = LightShadows.None;

            var rGo = new GameObject("EnvelopeRimLight");
            rGo.transform.SetParent(eye.transform, false);
            rGo.transform.localPosition = new Vector3(-0.24f, -0.08f, 0.30f);
            rimLight = rGo.AddComponent<Light>();
            rimLight.type = LightType.Point;
            rimLight.range = 1.4f;
            rimLight.intensity = 0.30f;
            rimLight.color = new Color(0.76f, 0.86f, 0.92f);
            rimLight.shadows = LightShadows.None;

            stageRoot.gameObject.SetActive(false);
            keyLight.enabled = false;
            rimLight.enabled = false;
        }

        public void SetReducedMotion(bool reduced) => reducedMotion = reduced;

        public void Open(GameObject template, Volume activeGrade, bool startOpen,
            Action<EnvelopePartKind> onSelectPart, Action onOpenedFlap = null)
        {
            Close();
            active = true;
            flapOpen = startOpen;
            flapAngle = startOpen ? 142f : 0f;
            slideOut = startOpen ? 1f : 0f;
            openedFrame = Time.frameCount;
            rotX = 6f;
            rotY = -6f;
            zoom = zoomTarget = 0.34f;
            lmbDown = lmbDragging = false;
            hoveredPart = EnvelopePartKind.None;
            onPartSelected = onSelectPart;
            onFlapOpened = onOpenedFlap;

            BuildEnvelopeModel(template);
            InspectionSharpnessGuard.Acquire(activeGrade, allowBackgroundSoften: !reducedMotion);
            sharpnessAcquired = true;

            stageRoot.gameObject.SetActive(true);
            keyLight.enabled = true;
            rimLight.enabled = true;
            ApplyTransforms();
        }

        public void ToggleFlap()
        {
            if (!active || InputBlocked) return;
            SetFlapOpen(!flapOpen);
        }

        internal void TriggerPart(EnvelopePartKind kind)
        {
            if (!active || InputBlocked) return;
            if (kind == EnvelopePartKind.Flap) ToggleFlap();
            else if (kind == EnvelopePartKind.BayNote || (flapOpen && kind != EnvelopePartKind.None))
                onPartSelected?.Invoke(kind);
        }

        public void SetFlapOpen(bool open)
        {
            if (!active || flapOpen == open) return;
            flapOpen = open;
            if (reducedMotion)
            {
                flapAngle = flapOpen ? 142f : 0f;
                slideOut = flapOpen ? 1f : 0f;
            }
            if (eye != null)
                audioBus?.From(flapOpen ? "paperFlip" : "paper", eye.transform.position + eye.transform.forward * 0.32f, 0.72f);
            UpdateInnerPartColliders();
            if (flapOpen)
                onFlapOpened?.Invoke();
        }

        public void Close()
        {
            active = false;
            partColliders.Clear();
            if (envelopeInst != null)
            {
                Destroy(envelopeInst);
                envelopeInst = null;
            }
            if (frontFaceMat != null)
            {
                Destroy(frontFaceMat);
                frontFaceMat = null;
            }
            for (int i = 0; i < partMats.Count; i++)
                if (partMats[i] != null) Destroy(partMats[i]);
            partMats.Clear();
            flapPivot = noteSlot = letterSlot = newspaperSlot = keySlot = null;
            onPartSelected = null;
            onFlapOpened = null;
            if (stageRoot != null) stageRoot.gameObject.SetActive(false);
            if (keyLight != null) keyLight.enabled = false;
            if (rimLight != null) rimLight.enabled = false;
            if (sharpnessAcquired)
            {
                InspectionSharpnessGuard.Release();
                sharpnessAcquired = false;
            }
        }

        public void ProjectHotspots(List<ProjectedHotspot3D> output, Func<EnvelopePartKind, bool> isRead = null)
        {
            output.Clear();
            if (!active || eye == null) return;
            foreach (var kv in partColliders)
            {
                var col = kv.Key;
                if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) continue;
                var kind = kv.Value.kind;
                Vector3 sp = eye.WorldToScreenPoint(col.bounds.center);
                if (sp.z <= 0.05f) continue;
                string label = kind == EnvelopePartKind.Flap
                    ? (flapOpen ? "Gấp nắp phong bì" : "Mở nắp phong bì")
                    : kv.Value.label;
                bool marked = isRead != null && isRead(kind);
                output.Add(new ProjectedHotspot3D(kind.ToString(), label, sp, hoveredPart == kind, marked));
            }
        }

        private void BuildEnvelopeModel(GameObject template)
        {
            partColliders.Clear();
            if (template != null)
            {
                envelopeInst = Instantiate(template, holder, false);
                envelopeInst.name = "Envelope3DInstance";
                envelopeInst.SetActive(true);
                envelopeInst.transform.localPosition = Vector3.zero;
                envelopeInst.transform.localRotation = Quaternion.identity;
                envelopeInst.transform.localScale = Vector3.one;
                foreach (var c in envelopeInst.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                foreach (var r in envelopeInst.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;

                // Mount legible vintage Vietnamese envelope front directly onto envelopeInst (clean upright Unity space: -Z faces camera!)
                var envTex = textureLookup?.Invoke("Doc_BayEnvelopeFront");
                if (envTex != null)
                {
                    var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    quad.name = "EnvelopeAddressPrint";
                    var qc = quad.GetComponent<Collider>();
                    if (qc != null) Destroy(qc);
                    quad.transform.SetParent(envelopeInst.transform, false);
                    quad.transform.localPosition = new Vector3(0f, -0.002f, -0.0095f);
                    quad.transform.localRotation = Quaternion.identity;
                    quad.transform.localScale = new Vector3(0.222f, 0.146f, 1f);
                    frontFaceMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "DiegeticEnvelopeFrontMat" };
                    frontFaceMat.SetTexture("_BaseMap", envTex);
                    frontFaceMat.SetColor("_BaseColor", new Color(0.91f, 0.88f, 0.82f, 1f));
                    frontFaceMat.SetFloat("_Smoothness", 0.06f);
                    var mr = quad.GetComponent<MeshRenderer>();
                    mr.sharedMaterial = frontFaceMat;
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                }

                // Wire hinged flap pivot at top edge (y = +0.076, z = +0.004 in Unity coords)
                var flapTf = FindPart(envelopeInst.transform, "EnvelopeFlap");
                if (flapTf != null)
                {
                    flapPivot = new GameObject("FlapHingePivot").transform;
                    flapPivot.SetParent(envelopeInst.transform, false);
                    flapPivot.localPosition = new Vector3(0f, 0.076f, 0.004f);
                    flapPivot.localRotation = Quaternion.identity;
                    flapTf.SetParent(flapPivot, true);
                    RegisterPartCollider(flapTf, EnvelopePartKind.Flap, "Mở nắp phong bì");
                }

                // Build clean upright Unity-space interactive wrappers for tucked note & inner documents
                // so Blender's -90 X FBX child rotation never twists texture quads sideways!
                var rawNote = FindPart(envelopeInst.transform, "EnvSlot_BayNote");
                if (rawNote != null) rawNote.gameObject.SetActive(false);
                var rawLetter = FindPart(envelopeInst.transform, "EnvSlot_LanLetter");
                if (rawLetter != null) rawLetter.gameObject.SetActive(false);
                var rawNews = FindPart(envelopeInst.transform, "EnvSlot_Newspaper");
                if (rawNews != null) rawNews.gameObject.SetActive(false);
                keySlot = FindPart(envelopeInst.transform, "EnvSlot_WingCKey");

                noteSlot = CreateUprightDocCard(envelopeInst.transform, "EnvCard_BayNote", "Doc_BayNote",
                    new Vector3(0.088f, -0.044f, -0.0135f), Quaternion.Euler(0f, 0f, -4.5f), new Vector2(0.068f, 0.048f));
                RegisterPartCollider(noteSlot, EnvelopePartKind.BayNote, "Giấy nhắn ngoài bì");

                letterSlot = CreateUprightDocCard(envelopeInst.transform, "EnvCard_LanLetter", "Doc_LanLetter2005",
                    new Vector3(-0.042f, 0.016f, -0.0020f), Quaternion.Euler(0f, 0f, 4.0f), new Vector2(0.098f, 0.128f));
                letterBasePos = letterSlot.localPosition;
                RegisterPartCollider(letterSlot, EnvelopePartKind.LanLetter, "Thư cũ của Lan");

                newspaperSlot = CreateUprightDocCard(envelopeInst.transform, "EnvCard_Newspaper", "Doc_Newspaper2005",
                    new Vector3(0.036f, 0.014f, 0.0010f), Quaternion.Euler(0f, 0f, -3.0f), new Vector2(0.100f, 0.118f));
                newspaperBasePos = newspaperSlot.localPosition;
                RegisterPartCollider(newspaperSlot, EnvelopePartKind.Newspaper, "Mẩu báo 19/11/2005");

                if (keySlot != null)
                {
                    keyBasePos = keySlot.localPosition;
                    RegisterPartCollider(keySlot, EnvelopePartKind.WingCKey, "Chìa cũ & thẻ giấy");
                }

                UpdateInnerPartColliders();
            }
        }

        private void RegisterPartCollider(Transform partTf, EnvelopePartKind kind, string label)
        {
            var mf = partTf.GetComponentInChildren<MeshFilter>(true);
            var host = mf != null ? mf.gameObject : partTf.gameObject;
            var col = host.GetComponent<BoxCollider>();
            if (col == null) col = host.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.enabled = true;
            // Slightly pad thin paper colliders so clicking feels effortless and crisp
            col.size = new Vector3(
                Mathf.Max(col.size.x, 0.036f),
                Mathf.Max(col.size.y, 0.036f),
                Mathf.Max(col.size.z, 0.018f));
            partColliders[col] = (kind, label);
        }

        private void UpdateInnerPartColliders()
        {
            foreach (var kv in partColliders)
            {
                if (kv.Key == null) continue;
                var kind = kv.Value.kind;
                bool isInner = kind is (EnvelopePartKind.LanLetter or EnvelopePartKind.Newspaper or EnvelopePartKind.WingCKey);
                kv.Key.enabled = !isInner || flapOpen;
            }
        }

        private static Transform FindPart(Transform root, string prefix)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (string.Equals(t.name, prefix, StringComparison.OrdinalIgnoreCase) ||
                    t.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return t;
            return null;
        }

        private void Update()
        {
            if (!active || stageRoot == null) return;
            float dt = Time.unscaledDeltaTime;

            float targetFlap = flapOpen ? 142f : 0f;
            float targetSlide = flapOpen ? 1f : 0f;
            flapAngle = Mathf.MoveTowards(flapAngle, targetFlap, dt * (reducedMotion ? 900f : 340f));
            slideOut = Mathf.MoveTowards(slideOut, targetSlide, dt * (reducedMotion ? 10f : 3.2f));
            zoom = Mathf.MoveTowards(zoom, zoomTarget, dt * (reducedMotion ? 5f : 0.9f));

            ApplyTransforms();

            if (InputBlocked || Time.frameCount == openedFrame) return;

            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 pos = mouse.position.ReadValue();
                bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                hoveredPart = overUi ? EnvelopePartKind.None : RaycastPart(pos);

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
                        rotY = Mathf.Clamp(rotY - d.x * 0.36f, -165f, 165f);
                        rotX = Mathf.Clamp(rotX + d.y * 0.28f, -52f, 52f);
                    }
                }
                if (lmbDown && mouse.leftButton.wasReleasedThisFrame)
                {
                    bool click = !lmbDragging;
                    lmbDown = lmbDragging = false;
                    if (click && !overUi)
                    {
                        var hit = RaycastPart(pos);
                        if (hit == EnvelopePartKind.Flap) ToggleFlap();
                        else if (hit != EnvelopePartKind.None) onPartSelected?.Invoke(hit);
                    }
                }

                float scroll = mouse.scroll.ReadValue().y;
                if (!overUi && Mathf.Abs(scroll) > 0.01f)
                    zoomTarget = Mathf.Clamp(zoomTarget - Mathf.Sign(scroll) * 0.024f, 0.24f, 0.50f);
            }

            var keys = Keyboard.current;
            if (keys != null)
            {
                if (keys.aKey.isPressed) rotY = Mathf.Clamp(rotY + 90f * dt, -165f, 165f);
                if (keys.dKey.isPressed) rotY = Mathf.Clamp(rotY - 90f * dt, -165f, 165f);
                if (keys.wKey.isPressed) rotX = Mathf.Clamp(rotX + 70f * dt, -52f, 52f);
                if (keys.sKey.isPressed) rotX = Mathf.Clamp(rotX - 70f * dt, -52f, 52f);
                if (keys.spaceKey.wasPressedThisFrame || keys.oKey.wasPressedThisFrame) ToggleFlap();
                if (keys.digit1Key.wasPressedThisFrame) onPartSelected?.Invoke(EnvelopePartKind.BayNote);
                if (flapOpen && keys.digit2Key.wasPressedThisFrame) onPartSelected?.Invoke(EnvelopePartKind.LanLetter);
                if (flapOpen && keys.digit3Key.wasPressedThisFrame) onPartSelected?.Invoke(EnvelopePartKind.Newspaper);
                if (flapOpen && keys.digit4Key.wasPressedThisFrame) onPartSelected?.Invoke(EnvelopePartKind.WingCKey);
            }
        }

        private EnvelopePartKind RaycastPart(Vector2 pointer)
        {
            if (eye == null) return EnvelopePartKind.None;
            Ray ray = eye.ScreenPointToRay(pointer);
            float bestDist = float.MaxValue;
            EnvelopePartKind best = EnvelopePartKind.None;
            foreach (var kv in partColliders)
            {
                if (kv.Key == null || !kv.Key.enabled || !kv.Key.gameObject.activeInHierarchy) continue;
                if (kv.Key.Raycast(ray, out var hit, 2.5f) && hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    best = kv.Value.kind;
                }
            }
            return best;
        }

        private readonly List<Material> partMats = new();

        private Transform CreateUprightDocCard(Transform parent, string name, string texKey, Vector3 localPos, Quaternion localRot, Vector2 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            var qc = go.GetComponent<Collider>();
            if (qc != null) Destroy(qc);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var tex = textureLookup?.Invoke(texKey);
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "DiegeticEnvPart_" + texKey };
            if (tex != null) m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", new Color(0.96f, 0.95f, 0.91f, 1f));
            m.SetFloat("_Smoothness", 0.06f);
            m.SetFloat("_Cull", 0f);
            partMats.Add(m);
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = m;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            return go.transform;
        }

        private void ApplyTransforms()
        {
            stageRoot.localPosition = new Vector3(0f, flapOpen ? -0.036f : -0.008f, zoom);
            float sway = reducedMotion ? 0f : Mathf.Sin(Time.unscaledTime * 1.1f) * 0.8f;
            holder.localRotation = Quaternion.Euler(rotX + sway * 0.35f, rotY + sway, 0f);
            if (flapPivot != null)
                flapPivot.localRotation = Quaternion.Euler(flapAngle, 0f, 0f);
            float slideEase = 1f - Mathf.Pow(1f - Mathf.Clamp01(slideOut), 3f);
            if (letterSlot != null)
            {
                letterSlot.gameObject.SetActive(slideEase > 0.04f);
                letterSlot.localPosition = letterBasePos + new Vector3(-0.010f * slideEase, 0.096f * slideEase, 0f);
            }
            if (newspaperSlot != null)
            {
                newspaperSlot.gameObject.SetActive(slideEase > 0.04f);
                newspaperSlot.localPosition = newspaperBasePos + new Vector3(0.014f * slideEase, 0.088f * slideEase, 0f);
            }
            if (keySlot != null)
                keySlot.localPosition = keyBasePos + new Vector3(0.054f * slideEase, 0.068f * slideEase, -0.006f * slideEase);
        }

        private void OnDestroy()
        {
            Close();
            if (stageRoot != null) Destroy(stageRoot.gameObject);
            if (keyLight != null) Destroy(keyLight.gameObject);
            if (rimLight != null) Destroy(rimLight.gameObject);
        }
    }
}
