using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace CTCC
{
    public readonly struct InspectAnnotationField
    {
        public readonly string Id;
        public readonly Rect Uv;
        public readonly string Label;

        public InspectAnnotationField(string id, Rect uv, string label = null)
        {
            Id = id;
            Uv = uv;
            Label = label;
        }
    }

    public readonly struct InspectDetailTarget
    {
        public readonly string Id;
        public readonly string Label;
        public readonly Vector2 OffsetXY;
        public readonly float Zoom;
        public readonly bool OnBackSide;
        public readonly Rect Uv;

        public InspectDetailTarget(string id, string label, Vector2 offsetXY, float zoom = 0.27f,
            bool onBackSide = false, Rect? uv = null)
        {
            Id = id;
            Label = label;
            OffsetXY = offsetXY;
            Zoom = zoom;
            OnBackSide = onBackSide;
            // Derive a sensible interactive paper UV region from OffsetXY when an explicit UV rect isn't supplied.
            Uv = uv ?? new Rect(
                Mathf.Clamp01(0.5f - offsetXY.x * 2.2f - 0.16f),
                Mathf.Clamp01(0.5f - offsetXY.y * 2.2f - 0.10f),
                0.32f,
                0.20f);
        }
    }

    public readonly struct InspectPartHotspot
    {
        public readonly string Id;
        public readonly string PartName;
        public readonly string Label;
        public readonly Action OnClick;

        public InspectPartHotspot(string id, string partName, string label, Action onClick)
        {
            Id = id;
            PartName = partName;
            Label = label;
            OnClick = onClick;
        }
    }

    /// <summary>
    /// Phase 2.5 Physical Inspector 2.0 — Standalone 0-Sidebar 3D Evidence & Dual-Sided Document Viewer:
    /// - Zero oblique-angle blur: eliminates macro 55mm f/4.2 Bokeh DoF (uses far-only Gaussian DoF >1.30m via InspectionSharpnessGuard),
    ///   boosts URP renderScale to native 1.0x during inspection, suppresses player flashlight washout, and centers dead-on.
    /// - Reusable 3D hotspot architecture: direct LMB click on paper UV hotspots (details & ballpoint marks) and 3D mesh colliders;
    ///   LMB drag rotates smoothly (with fresh-press latch preventing opening-click jerk); RMB click / Space flips 2-sided paper;
    ///   Scroll / Q-Z zooms; smooth fold-unfold animation for envelope sheets; full keyboard & opt-in Large-Print accessibility.
    /// </summary>
    public sealed class PhysicalInspector : MonoBehaviour
    {
        private Camera eye;
        private Transform stage;
        private Transform holder;
        private Light keyLight;
        private Light rimLight;
        private GameObject currentModel;
        private Mesh frontMesh;
        private Mesh backMesh;
        private Mesh paperEdge;
        private Material paperEdgeMaterial;
        private Texture2D paperFiberNormal;
        private Material frontMaterial;
        private Material backMaterial;

        private float rotX, rotY, zoom = 0.39f;
        private float flipAngle, flipTarget;
        private Vector2 panOffset, panTarget;
        private float zoomTarget = 0.39f;
        private bool active;
        private bool standaloneCentered = true;
        private bool reducedMotion;
        private bool cameraAssist = true;
        private float openBlend, liftDuration;
        private float unfoldProgress = 1f;
        private bool returning;
        private Vector3 openFrom;
        private float paperWidth, paperHeight;
        private Mesh inkMesh;
        private MeshFilter ink;
        private Material inkMaterial;
        private GameObject worldProp;
        private Renderer worldSheet;
        private bool sheetWasEnabled;

        // Fresh-press LMB drag vs click disambiguation + RMB flip/mark disambiguation
        private int openedFrame = -1;
        private bool lmbPressedInInspect;
        private bool lmbDragging;
        private Vector2 lmbDownPos;
        private Vector2 rmbDownPos;
        private bool rmbPressedInInspect;

        private InspectAnnotationField[] annotationFields = Array.Empty<InspectAnnotationField>();
        private InspectPartHotspot[] partHotspots = Array.Empty<InspectPartHotspot>();
        private readonly Dictionary<Collider, InspectPartHotspot> partColliders = new();
        private Action<string> onMarkField;
        private bool hasBackSide;
        private bool showingBack;
        private InspectDetailTarget[] details = Array.Empty<InspectDetailTarget>();
        private int activeDetailIndex = -1;
        private string hoveredHotspotId;
        private Action<bool> onFlipped;
        private Action<string> onDetailFocused;
        private Action onRakingRevealed;
        private EnvironmentalAudio audioBus;
        private Shader rakingPaperShader;
        private string currentObservationId = "";
        private bool hasRakingIndent;
        private float rakingRevealFactor;
        private float rakingHoldSeconds;
        private bool rakingRevealed;
        private bool sharpnessAcquired;

        private static readonly int RakingRevealId = Shader.PropertyToID("_RakingReveal");
        private static readonly int LightDirWsId = Shader.PropertyToID("_LightDirWS");
        private static readonly int RimDirWsId = Shader.PropertyToID("_RimDirWS");

        public bool IsInspecting => active;
        public bool HasBackSide => hasBackSide;
        public bool ShowingBack => showingBack;
        public bool HasRakingIndent => hasRakingIndent;
        public float RakingRevealFactor => rakingRevealFactor;
        public bool RakingRevealed => rakingRevealed;
        public string CurrentObservationId => currentObservationId;
        public int ActiveDetailIndex => activeDetailIndex;
        public string HoveredHotspotId => hoveredHotspotId;
        public InspectDetailTarget[] Details => details;
        public Transform HolderTransform => holder;
        public GameObject CurrentModelRoot => currentModel;
        public bool InputBlocked { get; set; }

        public event Action<string> InspectOpened;
        public event Action<string> InspectClosed;

        public void Initialize(Camera camera, EnvironmentalAudio audio = null, Shader rakingShader = null)
        {
            eye = camera;
            audioBus = audio;
            rakingPaperShader = rakingShader;
            paperFiberNormal = Resources.Load<Texture2D>("CTCC/PaperFiber_Normal");
            stage = new GameObject("Physical Inspector Stage").transform;
            stage.SetParent(eye.transform, false);
            stage.localPosition = new Vector3(0f, 0.005f, 0.39f);

            holder = new GameObject("Inspect Holder").transform;
            holder.SetParent(stage, false);

            var keyGo = new GameObject("Inspect Key Light");
            keyGo.transform.SetParent(eye.transform, false);
            keyGo.transform.localPosition = new Vector3(0.24f, 0.24f, 0.08f);
            keyLight = keyGo.AddComponent<Light>();
            keyLight.type = LightType.Point;
            keyLight.range = 1.8f;
            keyLight.intensity = 0.60f;
            keyLight.color = new Color(1.0f, 0.96f, 0.90f);
            keyLight.shadows = LightShadows.None;

            var rimGo = new GameObject("Inspect Rim Light");
            rimGo.transform.SetParent(eye.transform, false);
            rimGo.transform.localPosition = new Vector3(-0.26f, -0.08f, 0.34f);
            rimLight = rimGo.AddComponent<Light>();
            rimLight.type = LightType.Point;
            rimLight.range = 1.6f;
            rimLight.intensity = 0.28f;
            rimLight.color = new Color(0.76f, 0.86f, 0.92f);
            rimLight.shadows = LightShadows.None;

            stage.gameObject.SetActive(false);
            keyLight.enabled = false;
            rimLight.enabled = false;
        }

        public void SetReducedMotion(bool reduced) => reducedMotion = reduced;
        public void SetCameraAssist(bool enabled) => cameraAssist = enabled;

        internal void SetInspectAnglesForQA(float xDeg, float yDeg, float zoomDist)
        {
            if (!active) return;
            rotX = Mathf.Clamp(xDeg, -82f, 82f);
            rotY = Mathf.Clamp(yDeg, -175f, 175f);
            zoom = zoomTarget = Mathf.Clamp(zoomDist, 0.19f, 0.62f);
            openBlend = 1f;
            unfoldProgress = 1f;
            if (stage != null)
                stage.localPosition = new Vector3(standaloneCentered ? 0f : -0.095f, -0.008f, zoom);
            if (holder != null)
                holder.localRotation = Quaternion.Euler(rotX, rotY + flipAngle, 0f);
        }

        public void Open(
            GameObject prefabOrTemplate,
            Texture documentTexture,
            float aspect,
            Volume activeGrade,
            Texture backTexture = null,
            InspectDetailTarget[] detailTargets = null,
            Action<bool> onFlip = null,
            Action<string> onDetail = null,
            string observationId = null,
            Texture backIndentNormal = null,
            Texture backIndentMask = null,
            Action onRakingReveal = null,
            Vector3? source = null,
            InspectAnnotationField[] annotationFields = null,
            string markedField = null,
            Action<string> onMark = null,
            bool liftModel = false,
            Renderer liftSheet = null,
            float pickupSeconds = .22f,
            bool standaloneCenter = true,
            bool animateUnfold = false,
            InspectPartHotspot[] modelHotspots = null,
            Vector3? initialModelEuler = null)
        {
            Close();
            active = true;
            standaloneCentered = standaloneCenter;
            openedFrame = Time.frameCount;
            lmbPressedInInspect = false;
            lmbDragging = false;
            rmbPressedInInspect = false;
            hoveredHotspotId = null;

            currentObservationId = observationId ?? "";
            liftDuration = Mathf.Max(.08f, pickupSeconds);
            openBlend = reducedMotion || !cameraAssist ? 1f : 0f;
            unfoldProgress = animateUnfold && !reducedMotion ? 0f : 1f;
            float baseX = standaloneCentered ? 0f : -0.095f;
            openFrom = new Vector3(baseX, -0.20f, 0.40f);
            if (source.HasValue && cameraAssist && !reducedMotion)
            {
                Vector3 local = eye.transform.InverseTransformPoint(source.Value);
                openFrom = new Vector3(Mathf.Clamp(local.x, -0.30f, 0.30f),
                    Mathf.Clamp(local.y, -0.24f, 0.19f), Mathf.Clamp(local.z, 0.46f, 0.86f));
            }
            rotX = 0f;
            rotY = 0f;
            zoom = standaloneCentered ? 0.38f : 0.41f;
            zoomTarget = zoom;
            flipAngle = 0f;
            flipTarget = 0f;
            showingBack = false;
            panOffset = Vector2.zero;
            panTarget = Vector2.zero;
            hasBackSide = prefabOrTemplate == null && backTexture != null;
            hasRakingIndent = hasBackSide && backIndentNormal != null;
            rakingRevealFactor = 0f;
            rakingHoldSeconds = 0f;
            rakingRevealed = false;
            details = detailTargets ?? Array.Empty<InspectDetailTarget>();
            activeDetailIndex = -1;
            onFlipped = onFlip;
            onDetailFocused = onDetail;
            onRakingRevealed = onRakingReveal;
            this.annotationFields = annotationFields ?? Array.Empty<InspectAnnotationField>();
            partHotspots = modelHotspots ?? Array.Empty<InspectPartHotspot>();
            partColliders.Clear();
            onMarkField = onMark;

            if (prefabOrTemplate != null)
            {
                var pivot = new GameObject("InspectModelPivot");
                pivot.transform.SetParent(holder, false);
                pivot.transform.localPosition = Vector3.zero;
                pivot.transform.localRotation = Quaternion.identity;

                var inst = Instantiate(prefabOrTemplate, pivot.transform, false);
                inst.SetActive(true);
                inst.transform.localPosition = Vector3.zero;
                foreach (var c in inst.GetComponentsInChildren<Collider>(true))
                    c.enabled = false;
                foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                    r.shadowCastingMode = ShadowCastingMode.Off;

                NormalizeModelScale(inst, 0.25f);
                pivot.transform.localRotation = initialModelEuler.HasValue
                    ? Quaternion.Euler(initialModelEuler.Value)
                    : Quaternion.Euler(-14f, 10f, -3f);
                currentModel = pivot;
                BindModelPartHotspots(inst);
                keyLight.intensity = 0.72f;
                if (liftModel && prefabOrTemplate.activeSelf)
                {
                    worldProp = prefabOrTemplate;
                    worldProp.SetActive(false);
                }
            }
            else if (documentTexture != null)
            {
                currentModel = BuildDualSidedPaperSheet(documentTexture, backTexture, aspect, backIndentNormal, backIndentMask);
                currentModel.transform.SetParent(holder, false);
                keyLight.intensity = 0.44f;
                SetMarkedField(markedField);
                if (liftSheet != null)
                {
                    worldSheet = liftSheet;
                    sheetWasEnabled = worldSheet.enabled;
                    worldSheet.enabled = false;
                }
            }

            // Acquire Phase 2.5 Visual Clarity Guard: native 1.0x renderScale + far-only Gaussian DoF (>1.30m, 0 near blur!)
            InspectionSharpnessGuard.Acquire(activeGrade, allowBackgroundSoften: cameraAssist && !reducedMotion);
            sharpnessAcquired = true;

            stage.localPosition = new Vector3(baseX, 0.005f, zoom);
            stage.gameObject.SetActive(true);
            keyLight.enabled = true;
            rimLight.enabled = true;
            UpdateTransform();
            InspectOpened?.Invoke(currentObservationId);
        }

        private void BindModelPartHotspots(GameObject inst)
        {
            partColliders.Clear();
            if (partHotspots == null || partHotspots.Length == 0 || inst == null) return;
            foreach (var hs in partHotspots)
            {
                if (string.IsNullOrEmpty(hs.PartName)) continue;
                Transform match = null;
                foreach (var t in inst.GetComponentsInChildren<Transform>(true))
                {
                    if (string.Equals(t.name, hs.PartName, StringComparison.OrdinalIgnoreCase) ||
                        t.name.StartsWith(hs.PartName, StringComparison.OrdinalIgnoreCase))
                    {
                        match = t;
                        break;
                    }
                }
                if (match == null) continue;
                var mf = match.GetComponentInChildren<MeshFilter>(true);
                var host = mf != null ? mf.gameObject : match.gameObject;
                var col = host.GetComponent<BoxCollider>();
                if (col == null) col = host.AddComponent<BoxCollider>();
                col.enabled = true;
                col.isTrigger = true;
                partColliders[col] = hs;
            }
        }

        /// <summary>
        /// Accessible keyboard shortcut tilting the reverse sheet into the 25-40 deg keyLight grazing sweet spot.
        /// Remains 100% razor sharp because far-only Gaussian DoF never blurs objects within 1.25m!
        /// </summary>
        public void TiltForRakingLight()
        {
            if (!active || !hasBackSide || !hasRakingIndent || InputBlocked) return;
            if (!showingBack) Flip();
            rotX = 30f;
            rotY = -32f;
            panTarget = new Vector2(0f, -0.02f);
            zoomTarget = 0.30f;
            activeDetailIndex = -1;
            if (reducedMotion)
            {
                panOffset = panTarget;
                zoom = zoomTarget;
            }
            UpdateTransform();
            EvaluateRakingReveal(0.60f);
        }

        public void Flip()
        {
            if (!active || !hasBackSide || InputBlocked) return;
            showingBack = !showingBack;
            flipTarget = showingBack ? 180f : 0f;
            if (reducedMotion) flipAngle = flipTarget;
            rotX = 0f;
            rotY = 0f;
            panTarget = Vector2.zero;
            zoomTarget = standaloneCentered ? 0.38f : 0.40f;
            activeDetailIndex = -1;
            if (eye != null) audioBus?.From("paperFlip", eye.transform.position + eye.transform.forward * 0.35f, 0.85f);
            UpdateTransform();
            onFlipped?.Invoke(showingBack);
        }

        public void FocusDetail(int index)
        {
            if (!active || InputBlocked || index < 0 || index >= details.Length) return;
            var d = details[index];
            if (d.OnBackSide != showingBack) return;
            activeDetailIndex = index;
            rotX = 0f;
            rotY = 0f;
            panTarget = d.OffsetXY;
            zoomTarget = Mathf.Clamp(d.Zoom, 0.24f, 0.55f);
            if (reducedMotion)
            {
                panOffset = panTarget;
                zoom = zoomTarget;
            }
            onDetailFocused?.Invoke(d.Id);
            UpdateTransform();
        }

        public void ResetView()
        {
            if (!active) return;
            rotX = 0f;
            rotY = 0f;
            panTarget = Vector2.zero;
            zoomTarget = standaloneCentered ? 0.38f : 0.41f;
            activeDetailIndex = -1;
            if (reducedMotion)
            {
                panOffset = panTarget;
                zoom = zoomTarget;
            }
            UpdateTransform();
        }

        public void SetMarkedField(string id)
        {
            if (inkMesh != null)
            {
                if (ink != null) ink.sharedMesh = null;
                Destroy(inkMesh);
                inkMesh = null;
            }
            if (string.IsNullOrEmpty(id) || currentModel == null) return;
            foreach (var field in annotationFields)
            {
                if (field.Id != id) continue;
                if (ink == null)
                {
                    var stroke = new GameObject("Ballpoint underline");
                    stroke.transform.SetParent(currentModel.transform, false);
                    ink = stroke.AddComponent<MeshFilter>();
                    inkMaterial = MakePaperMaterial("InspectAnnotationInk", Texture2D.whiteTexture, doubleSided: false);
                    inkMaterial.SetColor("_BaseColor", new Color(0.035f, 0.064f, 0.15f, 1f));
                    var renderer = stroke.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = inkMaterial;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                }
                inkMesh = BuildInkStroke(field.Uv);
                ink.sharedMesh = inkMesh;
                break;
            }
        }

        /// <summary>
        /// Projects all currently visible paper & 3D-mesh hotspots into screen coordinates for diegetic 3D marker rendering.
        /// </summary>
        public void ProjectHotspots(List<ProjectedHotspot3D> output, Func<string, bool> isMarked = null)
        {
            output.Clear();
            if (!active || eye == null || holder == null || currentModel == null || returning) return;
            if (Mathf.Abs(flipAngle - flipTarget) > 20f) return;

            // 1. 3D model part hotspots
            foreach (var kv in partColliders)
            {
                var col = kv.Key;
                if (col == null || !col.gameObject.activeInHierarchy) continue;
                Vector3 wp = col.bounds.center;
                Vector3 sp = eye.WorldToScreenPoint(wp);
                if (sp.z <= 0.05f) continue;
                output.Add(new ProjectedHotspot3D(kv.Value.Id, kv.Value.Label, sp,
                    hoveredHotspotId == kv.Value.Id, isMarked != null && isMarked(kv.Value.Id)));
            }

            // 2. Document UV hotspots (annotation fields + detail focus regions)
            if (paperWidth > 0.001f && paperHeight > 0.001f)
            {
                if (!showingBack)
                {
                    for (int i = 0; i < annotationFields.Length; i++)
                    {
                        var f = annotationFields[i];
                        Vector3 local = SheetPoint(paperWidth, paperHeight, f.Uv.center.x, f.Uv.center.y, -0.002f);
                        Vector3 sp = eye.WorldToScreenPoint(holder.TransformPoint(local));
                        if (sp.z <= 0.05f) continue;
                        bool marked = isMarked != null && isMarked(f.Id);
                        string lbl = !string.IsNullOrEmpty(f.Label) ? f.Label : (marked ? "Bỏ đánh dấu" : "Đánh dấu mục này");
                        output.Add(new ProjectedHotspot3D(f.Id, lbl, sp, hoveredHotspotId == f.Id, marked));
                    }
                }
                for (int i = 0; i < details.Length; i++)
                {
                    var d = details[i];
                    if (d.OnBackSide != showingBack) continue;
                    // Skip if an annotation field already covers the exact same Id on the front
                    bool covered = false;
                    if (!showingBack)
                        for (int k = 0; k < annotationFields.Length; k++)
                            if (annotationFields[k].Id == d.Id) { covered = true; break; }
                    if (covered) continue;
                    float u = showingBack ? (1f - d.Uv.center.x) : d.Uv.center.x;
                    Vector3 local = SheetPoint(paperWidth, paperHeight, u, d.Uv.center.y, showingBack ? 0.002f : -0.002f);
                    Vector3 sp = eye.WorldToScreenPoint(holder.TransformPoint(local));
                    if (sp.z <= 0.05f) continue;
                    output.Add(new ProjectedHotspot3D(d.Id, d.Label, sp, hoveredHotspotId == d.Id, activeDetailIndex == i));
                }
            }
        }

        private Mesh BuildInkStroke(Rect area)
        {
            const int segments = 12;
            var points = new Vector3[(segments + 1) * 2];
            var indices = new int[segments * 6];
            for (int i = 0; i <= segments; i++)
            {
                float u = Mathf.Lerp(area.xMin + 0.012f, area.xMax - 0.012f, i / (float)segments);
                float v = area.yMin + 0.015f + Mathf.Sin(i * 0.83f) * 0.0019f;
                points[i * 2] = SheetPoint(paperWidth, paperHeight, u, v, -0.00145f);
                points[i * 2 + 1] = SheetPoint(paperWidth, paperHeight, u, v + 0.0048f, -0.00145f);
                if (i == segments) break;
                int p = i * 2, j = i * 6;
                indices[j] = p; indices[j + 1] = p + 1; indices[j + 2] = p + 2;
                indices[j + 3] = p + 2; indices[j + 4] = p + 1; indices[j + 5] = p + 3;
            }
            var mesh = new Mesh { name = "InspectAnnotation", vertices = points, triangles = indices };
            mesh.RecalculateNormals();
            return mesh;
        }

        public void Close()
        {
            returning = false;
            bool wasActive = active;
            string closedObsId = currentObservationId;
            if (currentModel != null)
            {
                currentModel.SetActive(false);
                Destroy(currentModel);
                currentModel = null;
            }
            if (frontMesh != null) Destroy(frontMesh);
            if (backMesh != null) Destroy(backMesh);
            if (paperEdge != null) Destroy(paperEdge);
            if (paperEdgeMaterial != null) Destroy(paperEdgeMaterial);
            if (frontMaterial != null) Destroy(frontMaterial);
            if (backMaterial != null) Destroy(backMaterial);
            if (inkMesh != null) Destroy(inkMesh);
            if (inkMaterial != null) Destroy(inkMaterial);
            if (worldProp != null) worldProp.SetActive(true);
            if (worldSheet != null) worldSheet.enabled = sheetWasEnabled;
            worldProp = null;
            worldSheet = null;
            inkMesh = null;
            inkMaterial = null;
            ink = null;
            frontMesh = null;
            backMesh = null;
            paperEdge = null;
            paperEdgeMaterial = null;
            frontMaterial = null;
            backMaterial = null;
            if (sharpnessAcquired)
            {
                InspectionSharpnessGuard.Release();
                sharpnessAcquired = false;
            }
            active = false;
            lmbPressedInInspect = false;
            lmbDragging = false;
            rmbPressedInInspect = false;
            hoveredHotspotId = null;
            currentObservationId = "";
            hasBackSide = false;
            hasRakingIndent = false;
            rakingRevealFactor = 0f;
            rakingHoldSeconds = 0f;
            rakingRevealed = false;
            showingBack = false;
            paperWidth = 0f;
            paperHeight = 0f;
            details = Array.Empty<InspectDetailTarget>();
            partHotspots = Array.Empty<InspectPartHotspot>();
            partColliders.Clear();
            activeDetailIndex = -1;
            onFlipped = null;
            onDetailFocused = null;
            onRakingRevealed = null;
            onMarkField = null;
            annotationFields = Array.Empty<InspectAnnotationField>();
            if (stage != null) stage.gameObject.SetActive(false);
            if (keyLight != null) keyLight.enabled = false;
            if (rimLight != null) rimLight.enabled = false;
            if (wasActive)
                InspectClosed?.Invoke(closedObsId);
        }

        internal System.Collections.IEnumerator PutBack()
        {
            if (active && !reducedMotion && cameraAssist)
            {
                returning = true;
                Vector3 from = stage.localPosition;
                Quaternion rotation = holder.localRotation;
                const float duration = .19f;
                for (float t = 0; active && t < duration; t += Time.unscaledDeltaTime)
                {
                    float ease = Mathf.SmoothStep(0, 1, t / duration);
                    stage.localPosition = Vector3.Lerp(from, openFrom, ease);
                    holder.localRotation = Quaternion.Slerp(rotation, Quaternion.identity, ease);
                    yield return null;
                }
            }
            Close();
        }

        private void OnDestroy()
        {
            Close();
            if (stage != null) Destroy(stage.gameObject);
            if (keyLight != null) Destroy(keyLight.gameObject);
            if (rimLight != null) Destroy(rimLight.gameObject);
        }

        private void Update()
        {
            if (!active || currentModel == null || returning) return;

            float dt = Time.unscaledDeltaTime;
            openBlend = Mathf.MoveTowards(openBlend, 1f, dt / liftDuration);
            unfoldProgress = Mathf.MoveTowards(unfoldProgress, 1f, dt * (reducedMotion ? 8f : 3.2f));
            flipAngle = Mathf.MoveTowards(flipAngle, flipTarget, dt * (reducedMotion ? 1400f : 540f));
            panOffset = Vector2.MoveTowards(panOffset, panTarget, dt * (reducedMotion ? 5f : 0.65f));
            zoom = Mathf.MoveTowards(zoom, zoomTarget, dt * (reducedMotion ? 5f : 0.85f));

            if (InputBlocked)
            {
                UpdateTransform();
                return;
            }

            var mouse = Mouse.current;
            var keys = Keyboard.current;

            if (mouse != null && Time.frameCount != openedFrame)
            {
                Vector2 pointer = mouse.position.ReadValue();
                bool overUiButton = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                bool overInspectArea = (standaloneCentered || pointer.x < Screen.width * 0.625f) && !overUiButton;

                hoveredHotspotId = overInspectArea ? HitTestHotspotId(pointer) : null;

                // Fresh-press LMB: short click interacts with 3D hotspot; drag >6px orbits smoothly (zero opening-click jerk!)
                if (mouse.leftButton.wasPressedThisFrame && overInspectArea)
                {
                    lmbPressedInInspect = true;
                    lmbDragging = false;
                    lmbDownPos = pointer;
                }
                if (lmbPressedInInspect && mouse.leftButton.isPressed)
                {
                    if (!lmbDragging && (pointer - lmbDownPos).sqrMagnitude > 36f)
                        lmbDragging = true;
                    if (lmbDragging)
                    {
                        Vector2 d = mouse.delta.ReadValue();
                        rotY -= d.x * 0.32f;
                        rotX = Mathf.Clamp(rotX + d.y * 0.26f, -52f, 52f);
                        activeDetailIndex = -1;
                    }
                }
                if (lmbPressedInInspect && mouse.leftButton.wasReleasedThisFrame)
                {
                    bool wasClick = !lmbDragging;
                    lmbPressedInInspect = false;
                    lmbDragging = false;
                    if (wasClick && overInspectArea)
                    {
                        ClickHotspotAt(pointer);
                        if (InputBlocked || !active) return;
                    }
                }

                // RMB short click flips 2-sided document (or marks field if single-sided); RMB drag also rotates smoothly
                if (mouse.rightButton.wasPressedThisFrame && overInspectArea)
                {
                    rmbPressedInInspect = true;
                    rmbDownPos = pointer;
                }
                if (rmbPressedInInspect && mouse.rightButton.isPressed && (pointer - rmbDownPos).sqrMagnitude > 64f)
                {
                    Vector2 d = mouse.delta.ReadValue();
                    rotY -= d.x * 0.32f;
                    rotX = Mathf.Clamp(rotX + d.y * 0.26f, -52f, 52f);
                    activeDetailIndex = -1;
                }
                if (rmbPressedInInspect && mouse.rightButton.wasReleasedThisFrame)
                {
                    bool shortRmb = (pointer - rmbDownPos).sqrMagnitude <= 64f;
                    rmbPressedInInspect = false;
                    if (shortRmb && overInspectArea)
                    {
                        if (!showingBack && annotationFields.Length != 0 && TryMarkAtPointer(pointer))
                        {
                            if (InputBlocked || !active) return;
                        }
                        else if (hasBackSide)
                        {
                            Flip();
                        }
                    }
                }

                float scroll = mouse.scroll.ReadValue().y;
                if (overInspectArea && Mathf.Abs(scroll) > 0.01f)
                {
                    zoomTarget = Mathf.Clamp(zoomTarget - Mathf.Sign(scroll) * 0.028f, 0.22f, 0.58f);
                    zoom = zoomTarget;
                }
            }

            if (keys != null)
            {
                if (keys.aKey.isPressed) rotY += 90f * dt;
                if (keys.dKey.isPressed) rotY -= 90f * dt;
                if (keys.wKey.isPressed) rotX = Mathf.Clamp(rotX + 70f * dt, -52f, 52f);
                if (keys.sKey.isPressed) rotX = Mathf.Clamp(rotX - 70f * dt, -52f, 52f);
                if (keys.qKey.isPressed) { zoomTarget = Mathf.Clamp(zoomTarget - 0.34f * dt, 0.22f, 0.58f); zoom = zoomTarget; }
                if (keys.zKey.isPressed) { zoomTarget = Mathf.Clamp(zoomTarget + 0.34f * dt, 0.22f, 0.58f); zoom = zoomTarget; }
                if (hasBackSide && (keys.spaceKey.wasPressedThisFrame || keys.rKey.wasPressedThisFrame))
                    Flip();
                if (hasRakingIndent && showingBack && keys.tKey.wasPressedThisFrame)
                    TiltForRakingLight();
                if (keys.digit1Key.wasPressedThisFrame && details.Length >= 1) FocusDetail(0);
                if (keys.digit2Key.wasPressedThisFrame && details.Length >= 2) FocusDetail(1);
                if (keys.digit3Key.wasPressedThisFrame && details.Length >= 3) FocusDetail(2);
                if (keys.digit0Key.wasPressedThisFrame) ResetView();
                if (!showingBack && keys.mKey.wasPressedThisFrame && annotationFields.Length != 0)
                {
                    if (activeDetailIndex >= 0 && activeDetailIndex < details.Length)
                    {
                        string detail = details[activeDetailIndex].Id;
                        foreach (var field in annotationFields)
                            if (field.Id == detail) { MarkField(detail); break; }
                    }
                    else if (annotationFields.Length == 1)
                    {
                        MarkField(annotationFields[0].Id);
                    }
                }
            }

            UpdateTransform();
            EvaluateRakingReveal(dt);
        }

        private void EvaluateRakingReveal(float dt)
        {
            if (!hasRakingIndent || backMaterial == null)
            {
                rakingRevealFactor = 0f;
                return;
            }
            if (!showingBack)
            {
                rakingRevealFactor = Mathf.MoveTowards(rakingRevealFactor, 0f, dt * 4f);
                backMaterial.SetFloat(RakingRevealId, rakingRevealFactor);
                return;
            }

            float maxTilt = Mathf.Max(Mathf.Abs(rotX), Mathf.Abs(rotY));
            float rise = Mathf.InverseLerp(16f, 26f, maxTilt);
            float fall = 1f - Mathf.InverseLerp(41f, 52f, maxTilt);
            float target = Mathf.Clamp01(rise * fall);
            if (keyLight != null && holder != null)
            {
                Vector3 toKey = (keyLight.transform.position - holder.position).normalized;
                Vector3 toRim = rimLight != null ? (rimLight.transform.position - holder.position).normalized : -toKey;
                backMaterial.SetVector(LightDirWsId, new Vector4(toKey.x, toKey.y, toKey.z, 0f));
                backMaterial.SetVector(RimDirWsId, new Vector4(toRim.x, toRim.y, toRim.z, 0f));
            }
            rakingRevealFactor = Mathf.MoveTowards(rakingRevealFactor, target, dt * 5.2f);
            backMaterial.SetFloat(RakingRevealId, rakingRevealFactor);

            if (!rakingRevealed && rakingRevealFactor >= 0.72f)
            {
                rakingHoldSeconds += dt;
                if (rakingHoldSeconds >= 0.35f)
                {
                    rakingRevealed = true;
                    if (eye != null) audioBus?.From("paper", eye.transform.position + eye.transform.forward * 0.35f, 0.52f);
                    onRakingRevealed?.Invoke();
                }
            }
            else if (rakingRevealFactor < 0.50f)
            {
                rakingHoldSeconds = Mathf.Max(0f, rakingHoldSeconds - dt * 1.5f);
            }
        }

        private void UpdateTransform()
        {
            if (hasBackSide) rotY = Mathf.Clamp(rotY, -52f, 52f);
            float ease = 1f - Mathf.Pow(1f - openBlend, 3f);
            float baseX = standaloneCentered ? 0f : -0.095f;
            float baseY = standaloneCentered ? 0.005f : 0.01f;
            stage.localPosition = Vector3.Lerp(openFrom, new Vector3(baseX + panOffset.x, baseY + panOffset.y, zoom), ease);
            float idleSway = reducedMotion || !cameraAssist ? 0f : Mathf.Sin(Time.unscaledTime * 1.05f) * 0.9f;
            holder.localRotation = Quaternion.Euler(rotX + idleSway * 0.35f, rotY + flipAngle + idleSway, 0f);
            if (currentModel != null && paperWidth > 0.001f)
            {
                // Subtle physical paper unfold animation when lifted from envelope
                float uf = 1f - Mathf.Pow(1f - unfoldProgress, 3f);
                currentModel.transform.localScale = new Vector3(1f, Mathf.Lerp(0.52f, 1f, uf), 1f);
            }
        }

        private bool TryRayPaperUv(Vector2 pointer, out Vector2 uv)
        {
            uv = default;
            if (eye == null || holder == null || paperWidth <= 0.001f || paperHeight <= 0.001f) return false;
            if (Mathf.Abs(flipAngle - flipTarget) > 12f) return false;
            Ray ray = eye.ScreenPointToRay(pointer);
            var plane = new Plane(showingBack ? holder.forward : -holder.forward, holder.position);
            if (!plane.Raycast(ray, out float distance) || distance < 0.05f) return false;
            Vector3 hit = holder.InverseTransformPoint(ray.GetPoint(distance));
            float u = hit.x / paperWidth + 0.5f;
            float v = hit.y / paperHeight + 0.5f;
            if (showingBack) u = 1f - u;
            if (u < 0f || u > 1f || v < 0f || v > 1f) return false;
            uv = new Vector2(u, v);
            return true;
        }

        private string HitTestHotspotId(Vector2 pointer)
        {
            if (eye == null) return null;
            if (partColliders.Count > 0)
            {
                Ray ray = eye.ScreenPointToRay(pointer);
                float bestDist = float.MaxValue;
                string bestId = null;
                foreach (var kv in partColliders)
                {
                    if (kv.Key == null || !kv.Key.gameObject.activeInHierarchy) continue;
                    if (kv.Key.Raycast(ray, out var hit, 2.5f) && hit.distance < bestDist)
                    {
                        bestDist = hit.distance;
                        bestId = kv.Value.Id;
                    }
                }
                if (bestId != null) return bestId;
            }
            if (TryRayPaperUv(pointer, out Vector2 uv))
            {
                if (!showingBack)
                {
                    for (int i = 0; i < annotationFields.Length; i++)
                        if (annotationFields[i].Uv.Contains(uv)) return annotationFields[i].Id;
                }
                for (int i = 0; i < details.Length; i++)
                    if (details[i].OnBackSide == showingBack && details[i].Uv.Contains(uv)) return details[i].Id;
            }
            return null;
        }

        private void ClickHotspotAt(Vector2 pointer)
        {
            if (eye == null) return;
            if (partColliders.Count > 0)
            {
                Ray ray = eye.ScreenPointToRay(pointer);
                float bestDist = float.MaxValue;
                InspectPartHotspot? hitPart = null;
                foreach (var kv in partColliders)
                {
                    if (kv.Key == null || !kv.Key.gameObject.activeInHierarchy) continue;
                    if (kv.Key.Raycast(ray, out var hit, 2.5f) && hit.distance < bestDist)
                    {
                        bestDist = hit.distance;
                        hitPart = kv.Value;
                    }
                }
                if (hitPart.HasValue)
                {
                    hitPart.Value.OnClick?.Invoke();
                    return;
                }
            }
            if (TryRayPaperUv(pointer, out Vector2 uv))
            {
                if (!showingBack)
                {
                    for (int i = 0; i < annotationFields.Length; i++)
                    {
                        if (annotationFields[i].Uv.Contains(uv))
                        {
                            MarkField(annotationFields[i].Id);
                            return;
                        }
                    }
                }
                for (int i = 0; i < details.Length; i++)
                {
                    if (details[i].OnBackSide == showingBack && details[i].Uv.Contains(uv))
                    {
                        FocusDetail(i);
                        return;
                    }
                }
            }
        }

        private bool TryMarkAtPointer(Vector2 pointer)
        {
            if (showingBack || Mathf.Abs(flipAngle) > 8f) return false;
            if (!TryRayPaperUv(pointer, out Vector2 uv)) return false;
            foreach (var field in annotationFields)
                if (field.Uv.Contains(uv)) { MarkField(field.Id); return true; }
            return false;
        }

        private void MarkField(string id)
        {
            if (onMarkField == null) return;
            audioBus?.From("signaturePen", holder.position, 0.44f);
            onMarkField(id);
        }

        private static void NormalizeModelScale(GameObject root, float targetSpan)
        {
            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length == 0) return;
            Bounds combined = default;
            bool init = false;
            Matrix4x4 rootInv = root.transform.worldToLocalMatrix;
            foreach (var mf in filters)
            {
                if (mf.sharedMesh == null) continue;
                var b = mf.sharedMesh.bounds;
                Matrix4x4 m = rootInv * mf.transform.localToWorldMatrix;
                for (int ix = -1; ix <= 1; ix += 2)
                for (int iy = -1; iy <= 1; iy += 2)
                for (int iz = -1; iz <= 1; iz += 2)
                {
                    Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3(ix, iy, iz));
                    Vector3 localPt = m.MultiplyPoint3x4(corner);
                    if (!init) { combined = new Bounds(localPt, Vector3.zero); init = true; }
                    else combined.Encapsulate(localPt);
                }
            }
            if (!init) return;
            float maxDim = Mathf.Max(combined.size.x, combined.size.y, combined.size.z);
            if (maxDim > 0.001f)
            {
                float s = targetSpan / maxDim;
                root.transform.localScale = Vector3.one * s;
                root.transform.localPosition = -(root.transform.localRotation * (combined.center * s));
            }
        }

        private GameObject BuildDualSidedPaperSheet(Texture frontTex, Texture backTex, float aspect,
            Texture backIndentNormal = null, Texture backIndentMask = null)
        {
            float h = 0.285f;
            float w = h * Mathf.Clamp(aspect, 0.45f, 1.85f);
            paperWidth = w;
            paperHeight = h;
            var root = new GameObject("InspectDocument");

            frontMesh = BuildCurvedSheetMesh("InspectPaperFront", w, h, zOffset: -0.0004f, reverseFace: false);
            frontMaterial = MakePaperMaterial("InspectPaperFrontMat", frontTex, doubleSided: backTex == null);
            SetPaperFiber(frontMaterial);
            var frontGo = new GameObject("FrontFace");
            frontGo.transform.SetParent(root.transform, false);
            frontGo.AddComponent<MeshFilter>().sharedMesh = frontMesh;
            var fmr = frontGo.AddComponent<MeshRenderer>();
            fmr.sharedMaterial = frontMaterial;
            fmr.shadowCastingMode = ShadowCastingMode.Off;

            if (backTex != null)
            {
                backMesh = BuildCurvedSheetMesh("InspectPaperBack", w, h, zOffset: 0.0004f, reverseFace: true);
                backMaterial = backIndentNormal != null
                    ? MakeRakingPaperMaterial("InspectPaperBackMat", backTex, backIndentNormal, backIndentMask)
                    : MakePaperMaterial("InspectPaperBackMat", backTex, doubleSided: false);
                var backGo = new GameObject("BackFace");
                backGo.transform.SetParent(root.transform, false);
                backGo.AddComponent<MeshFilter>().sharedMesh = backMesh;
                var bmr = backGo.AddComponent<MeshRenderer>();
                bmr.sharedMaterial = backMaterial;
                bmr.shadowCastingMode = ShadowCastingMode.Off;
                if (backIndentNormal == null) SetPaperFiber(backMaterial);

                var cut = new GameObject("Paper cut edge");
                cut.transform.SetParent(root.transform, false);
                paperEdge = BuildCutEdge(w, h);
                cut.AddComponent<MeshFilter>().sharedMesh = paperEdge;
                paperEdgeMaterial = MakePaperMaterial("InspectPaperEdgeMat", Texture2D.whiteTexture, doubleSided: true);
                paperEdgeMaterial.SetColor("_BaseColor", new Color(.70f, .63f, .51f));
                var cutRenderer = cut.AddComponent<MeshRenderer>();
                cutRenderer.sharedMaterial = paperEdgeMaterial;
                cutRenderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            return root;
        }

        private Material MakeRakingPaperMaterial(string name, Texture tex, Texture indentNormal, Texture indentMask)
        {
            var rakingShader = rakingPaperShader != null ? rakingPaperShader : Shader.Find("CTCC/InspectPaperRaking");
            if (rakingShader == null)
                return MakePaperMaterial(name, tex, doubleSided: false);
            var mat = new Material(rakingShader) { name = name };
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", new Color(0.86f, 0.85f, 0.82f, 1f));
            mat.SetTexture("_IndentNormalMap", indentNormal);
            if (indentMask != null) mat.SetTexture("_IndentMaskMap", indentMask);
            mat.SetFloat("_IndentScale", 1.85f);
            if (paperFiberNormal != null)
                mat.SetTexture("_PaperFiber", paperFiberNormal);
            mat.SetFloat("_RakingReveal", 0f);
            mat.SetFloat("_Cull", 2f);
            return mat;
        }

        private static Material MakePaperMaterial(string name, Texture tex, bool doubleSided)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", new Color(0.86f, 0.85f, 0.82f, 1f));
            mat.SetFloat("_Smoothness", 0.05f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Cull", doubleSided ? 0f : 2f);
            return mat;
        }

        private void SetPaperFiber(Material material)
        {
            if (paperFiberNormal == null) return;
            material.SetTexture("_BumpMap", paperFiberNormal);
            material.SetFloat("_BumpScale", .16f);
            material.EnableKeyword("_NORMALMAP");
        }

        private static Mesh BuildCutEdge(float width, float height)
        {
            const int horizontal = 10, vertical = 12;
            const int count = horizontal * 2 + vertical * 2;
            var points = new Vector3[count * 2];
            var indices = new int[count * 6];
            for (int i = 0; i < count; i++)
            {
                Vector2 uv;
                if (i < horizontal) uv = new Vector2(i / (float)horizontal, 0);
                else if (i < horizontal + vertical) uv = new Vector2(1, (i - horizontal) / (float)vertical);
                else if (i < 2 * horizontal + vertical) uv = new Vector2(1 - (i - horizontal - vertical) / (float)horizontal, 1);
                else uv = new Vector2(0, 1 - (i - 2 * horizontal - vertical) / (float)vertical);

                int first = i * 2;
                int next = (first + 2) % points.Length;
                points[first] = SheetPoint(width, height, uv.x, uv.y, -.0004f);
                points[first + 1] = SheetPoint(width, height, uv.x, uv.y, .0004f);
                int start = i * 6;
                indices[start] = first;
                indices[start + 1] = next + 1;
                indices[start + 2] = first + 1;
                indices[start + 3] = first;
                indices[start + 4] = next;
                indices[start + 5] = next + 1;
            }
            var mesh = new Mesh { name = "InspectPaperEdge", vertices = points, triangles = indices };
            mesh.RecalculateNormals();
            mesh.UploadMeshData(true);
            return mesh;
        }

        private static Mesh BuildCurvedSheetMesh(string name, float w, float h, float zOffset, bool reverseFace)
        {
            int nx = 10, ny = 12;
            var verts = new Vector3[(nx + 1) * (ny + 1)];
            var uvs = new Vector2[verts.Length];
            var tris = new int[nx * ny * 6];
            int ti = 0;
            for (int y = 0; y <= ny; y++)
            {
                for (int x = 0; x <= nx; x++)
                {
                    float u = x / (float)nx;
                    float v = y / (float)ny;
                    int idx = y * (nx + 1) + x;
                    verts[idx] = SheetPoint(w, h, u, v, zOffset);
                    uvs[idx] = new Vector2(reverseFace ? (1f - u) : u, v);
                    if (x < nx && y < ny)
                    {
                        int a = idx;
                        int b = idx + 1;
                        int c = idx + nx + 1;
                        int d = c + 1;
                        if (!reverseFace)
                        {
                            tris[ti++] = a; tris[ti++] = c; tris[ti++] = b;
                            tris[ti++] = b; tris[ti++] = c; tris[ti++] = d;
                        }
                        else
                        {
                            tris[ti++] = a; tris[ti++] = b; tris[ti++] = c;
                            tris[ti++] = b; tris[ti++] = d; tris[ti++] = c;
                        }
                    }
                }
            }
            var mesh = new Mesh { name = name, vertices = verts, uv = uvs, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.UploadMeshData(true);
            return mesh;
        }

        private static Vector3 SheetPoint(float w, float h, float u, float v, float zOffset)
        {
            float curl = Mathf.Pow(u - 0.5f, 2f) * 0.022f + Mathf.Sin(v * Mathf.PI * 2f) * 0.0028f;
            return new Vector3((u - 0.5f) * w, (v - 0.5f) * h, -curl + zOffset);
        }
    }
}
