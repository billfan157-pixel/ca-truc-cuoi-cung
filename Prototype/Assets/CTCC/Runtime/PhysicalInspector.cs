using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CTCC
{
    public readonly struct InspectAnnotationField
    {
        public readonly string Id;
        public readonly Rect Uv;

        public InspectAnnotationField(string id, Rect uv)
        {
            Id = id;
            Uv = uv;
        }
    }

    public readonly struct InspectDetailTarget
    {
        public readonly string Id;
        public readonly string Label;
        public readonly Vector2 OffsetXY;
        public readonly float Zoom;
        public readonly bool OnBackSide;

        public InspectDetailTarget(string id, string label, Vector2 offsetXY, float zoom = 0.27f, bool onBackSide = false)
        {
            Id = id;
            Label = label;
            OffsetXY = offsetXY;
            Zoom = zoom;
            OnBackSide = onBackSide;
        }
    }

    /// <summary>
    /// Diegetic 3D investigative evidence & dual-sided document viewer (CH01-R).
    /// Supports calibrated lighting (zero washout), front/back physical flip (Space/R or button),
    /// keyboard & mouse detail focus zoom, and optional stage callbacks while preserving full
    /// accessibility through equivalent large-print text and zero-precision-mouse guarantees.
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
        private Material frontMaterial;
        private Material backMaterial;
        private DepthOfField activeDof;
        private float baseDofDist = 4.5f;
        private bool baseDofActive;

        private float rotX, rotY, zoom = 0.41f;
        private float flipAngle, flipTarget;
        private Vector2 panOffset, panTarget;
        private float zoomTarget = 0.41f;
        private bool active;
        private bool reducedMotion;
        private bool cameraAssist = true;
        private float openBlend;
        private Vector3 openFrom;
        private float paperWidth, paperHeight;
        private Mesh inkMesh;
        private MeshFilter ink;
        private Material inkMaterial;
        private GameObject worldProp;
        private Renderer worldSheet;
        private bool sheetWasEnabled;
        private Vector2 markPointerStart;
        private bool markClick;
        private InspectAnnotationField[] annotationFields = Array.Empty<InspectAnnotationField>();
        private Action<string> onMarkField;
        private bool hasBackSide;
        private bool showingBack;
        private InspectDetailTarget[] details = Array.Empty<InspectDetailTarget>();
        private int activeDetailIndex = -1;
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
        public InspectDetailTarget[] Details => details;
        public bool InputBlocked { get; set; }

        public event Action<string> InspectOpened;
        public event Action<string> InspectClosed;

        public void Initialize(Camera camera, EnvironmentalAudio audio = null, Shader rakingShader = null)
        {
            eye = camera;
            audioBus = audio;
            rakingPaperShader = rakingShader;
            stage = new GameObject("Physical Inspector Stage").transform;
            stage.SetParent(eye.transform, false);
            stage.localPosition = new Vector3(-0.085f, -0.01f, 0.41f);

            holder = new GameObject("Inspect Holder").transform;
            holder.SetParent(stage, false);

            var keyGo = new GameObject("Inspect Key Light");
            keyGo.transform.SetParent(eye.transform, false);
            keyGo.transform.localPosition = new Vector3(0.22f, 0.24f, 0.06f);
            keyLight = keyGo.AddComponent<Light>();
            keyLight.type = LightType.Point;
            keyLight.range = 1.6f;
            keyLight.intensity = 0.58f;
            keyLight.color = new Color(1.0f, 0.95f, 0.87f);
            keyLight.shadows = LightShadows.None;

            var rimGo = new GameObject("Inspect Rim Light");
            rimGo.transform.SetParent(eye.transform, false);
            rimGo.transform.localPosition = new Vector3(-0.26f, -0.10f, 0.34f);
            rimLight = rimGo.AddComponent<Light>();
            rimLight.type = LightType.Point;
            rimLight.range = 1.4f;
            rimLight.intensity = 0.24f;
            rimLight.color = new Color(0.72f, 0.84f, 0.90f);
            rimLight.shadows = LightShadows.None;

            stage.gameObject.SetActive(false);
            keyLight.enabled = false;
            rimLight.enabled = false;
        }

        public void SetReducedMotion(bool reduced) => reducedMotion = reduced;
        public void SetCameraAssist(bool enabled) => cameraAssist = enabled;

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
            Renderer liftSheet = null)
        {
            Close();
            active = true;
            currentObservationId = observationId ?? "";
            openBlend = reducedMotion || !cameraAssist ? 1f : 0f;
            openFrom = new Vector3(-0.095f, -0.20f, 0.41f);
            if (source.HasValue && cameraAssist && !reducedMotion)
            {
                Vector3 local = eye.transform.InverseTransformPoint(source.Value);
                openFrom = new Vector3(Mathf.Clamp(local.x, -0.30f, 0.30f),
                    Mathf.Clamp(local.y, -0.24f, 0.19f), Mathf.Clamp(local.z, 0.48f, 0.86f));
            }
            rotX = 0f;
            rotY = 0f;
            zoom = 0.41f;
            zoomTarget = 0.41f;
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
            onMarkField = onMark;
            markClick = false;

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
                pivot.transform.localRotation = Quaternion.Euler(-58f, 14f, -8f);
                currentModel = pivot;
                keyLight.intensity = 0.68f;
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
                keyLight.intensity = 0.40f;
                SetMarkedField(markedField);
                if (liftSheet != null)
                {
                    worldSheet = liftSheet;
                    sheetWasEnabled = worldSheet.enabled;
                    worldSheet.enabled = false;
                }
            }

            if (activeGrade != null && activeGrade.sharedProfile != null &&
                activeGrade.sharedProfile.TryGet<DepthOfField>(out var dof))
            {
                activeDof = dof;
                baseDofDist = dof.focusDistance.value;
                baseDofActive = dof.active;
                activeDof.active = cameraAssist && !reducedMotion;
            }

            stage.localPosition = new Vector3(-0.085f, -0.01f, zoom);
            stage.gameObject.SetActive(true);
            keyLight.enabled = true;
            rimLight.enabled = true;
            UpdateTransform();
            InspectOpened?.Invoke(currentObservationId);
        }

        /// <summary>
        /// Accessible keyboard/button shortcut that tilts the flipped sheet directly into the 25-40 deg
        /// keyLight grazing sweet spot so hidden ballpoint indentations catch light cleanly without fine mouse work.
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
            zoomTarget = 0.40f;
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
            zoomTarget = Mathf.Clamp(d.Zoom, 0.25f, 0.55f);
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
            zoomTarget = 0.41f;
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
            frontMaterial = null;
            backMaterial = null;
            if (activeDof != null)
            {
                activeDof.focusDistance.value = baseDofDist;
                activeDof.active = baseDofActive;
                activeDof = null;
            }
            active = false;
            currentObservationId = "";
            hasBackSide = false;
            hasRakingIndent = false;
            rakingRevealFactor = 0f;
            rakingHoldSeconds = 0f;
            rakingRevealed = false;
            showingBack = false;
            details = Array.Empty<InspectDetailTarget>();
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

        private void OnDestroy()
        {
            Close();
            if (stage != null) Destroy(stage.gameObject);
            if (keyLight != null) Destroy(keyLight.gameObject);
            if (rimLight != null) Destroy(rimLight.gameObject);
        }

        private void Update()
        {
            if (!active || currentModel == null) return;

            float dt = Time.unscaledDeltaTime;
            openBlend = Mathf.MoveTowards(openBlend, 1f, dt / 0.22f);
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

            if (mouse != null)
            {
                Vector2 pointer = mouse.position.ReadValue();
                bool overPaperSide = pointer.x < Screen.width * 0.625f;
                if (mouse.rightButton.wasPressedThisFrame)
                {
                    markClick = overPaperSide && !showingBack && annotationFields.Length != 0;
                    markPointerStart = pointer;
                }
                if (markClick && (pointer - markPointerStart).sqrMagnitude > 64f) markClick = false;
                if (markClick && mouse.rightButton.wasReleasedThisFrame)
                {
                    markClick = false;
                    MarkAtPointer(pointer);
                    if (InputBlocked) return;
                }
                if (overPaperSide && (mouse.leftButton.isPressed || mouse.rightButton.isPressed && !markClick))
                {
                    Vector2 d = mouse.delta.ReadValue();
                    rotY -= d.x * 0.32f;
                    rotX = Mathf.Clamp(rotX + d.y * 0.26f, -52f, 52f);
                    activeDetailIndex = -1;
                }
                float scroll = mouse.scroll.ReadValue().y;
                if (overPaperSide && Mathf.Abs(scroll) > 0.01f)
                {
                    zoomTarget = Mathf.Clamp(zoomTarget - Mathf.Sign(scroll) * 0.03f, 0.24f, 0.60f);
                    zoom = zoomTarget;
                }
            }

            if (keys != null)
            {
                if (keys.aKey.isPressed) rotY += 90f * dt;
                if (keys.dKey.isPressed) rotY -= 90f * dt;
                if (keys.wKey.isPressed) rotX = Mathf.Clamp(rotX + 70f * dt, -52f, 52f);
                if (keys.sKey.isPressed) rotX = Mathf.Clamp(rotX - 70f * dt, -52f, 52f);
                if (keys.qKey.isPressed) { zoomTarget = Mathf.Clamp(zoomTarget - 0.34f * dt, 0.24f, 0.60f); zoom = zoomTarget; }
                if (keys.zKey.isPressed) { zoomTarget = Mathf.Clamp(zoomTarget + 0.34f * dt, 0.24f, 0.60f); zoom = zoomTarget; }
                if (hasBackSide && (keys.spaceKey.wasPressedThisFrame || keys.rKey.wasPressedThisFrame))
                    Flip();
                if (hasRakingIndent && showingBack && keys.tKey.wasPressedThisFrame)
                    TiltForRakingLight();
                if (keys.digit1Key.wasPressedThisFrame && details.Length >= 1) FocusDetail(0);
                if (keys.digit2Key.wasPressedThisFrame && details.Length >= 2) FocusDetail(1);
                if (keys.digit3Key.wasPressedThisFrame && details.Length >= 3) FocusDetail(2);
                if (keys.digit0Key.wasPressedThisFrame) ResetView();
                if (!showingBack && keys.mKey.wasPressedThisFrame && activeDetailIndex >= 0)
                {
                    string detail = details[activeDetailIndex].Id;
                    foreach (var field in annotationFields)
                        if (field.Id == detail) { MarkField(detail); break; }
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

            // Sweet spot: tilting rotX or rotY into 25..40 degrees toward keyLight catches grazing specular light!
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
            // Tilt a sheet for reading without rotating past the edge of the selected side.
            if (hasBackSide) rotY = Mathf.Clamp(rotY, -52f, 52f);
            float ease = 1f - Mathf.Pow(1f - openBlend, 3f);
            stage.localPosition = Vector3.Lerp(openFrom, new Vector3(-0.095f + panOffset.x, 0.01f + panOffset.y, zoom), ease);
            if (activeDof != null && activeDof.active)
                activeDof.focusDistance.value = zoom;
            float idleSway = reducedMotion || !cameraAssist ? 0f : Mathf.Sin(Time.unscaledTime * 1.05f) * 1.1f;
            holder.localRotation = Quaternion.Euler(rotX + idleSway * 0.35f, rotY + flipAngle + idleSway, 0f);
        }

        private void MarkAtPointer(Vector2 pointer)
        {
            if (showingBack || Mathf.Abs(flipAngle) > 8f) return;
            Ray ray = eye.ScreenPointToRay(pointer);
            var plane = new Plane(-holder.forward, holder.position);
            if (!plane.Raycast(ray, out float distance)) return;
            Vector3 hit = holder.InverseTransformPoint(ray.GetPoint(distance));
            Vector2 uv = new(hit.x / paperWidth + 0.5f, hit.y / paperHeight + 0.5f);
            foreach (var field in annotationFields)
                if (field.Uv.Contains(uv)) { MarkField(field.Id); return; }
        }

        private void MarkField(string id)
        {
            if (onMarkField == null) return;
            audioBus?.From("signaturePen", holder.position, 0.40f);
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
                root.transform.localPosition = -combined.center * s;
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
            mat.SetColor("_BaseColor", new Color(0.84f, 0.83f, 0.80f, 1f));
            mat.SetTexture("_IndentNormalMap", indentNormal);
            if (indentMask != null) mat.SetTexture("_IndentMaskMap", indentMask);
            mat.SetFloat("_IndentScale", 1.85f);
            mat.SetFloat("_RakingReveal", 0f);
            mat.SetFloat("_Cull", 2f);
            return mat;
        }

        private static Material MakePaperMaterial(string name, Texture tex, bool doubleSided)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            mat.SetTexture("_BaseMap", tex);
            // Slightly restrained albedo tint keeps ink typography rich under URP tonemapping with zero highlight washout
            mat.SetColor("_BaseColor", new Color(0.84f, 0.83f, 0.80f, 1f));
            mat.SetFloat("_Smoothness", 0.06f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Cull", doubleSided ? 0f : 2f);
            return mat;
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
            float curl = Mathf.Pow(u - 0.5f, 2f) * 0.026f + Mathf.Sin(v * Mathf.PI * 2f) * 0.0035f;
            return new Vector3((u - 0.5f) * w, (v - 0.5f) * h, -curl + zOffset);
        }
    }
}
