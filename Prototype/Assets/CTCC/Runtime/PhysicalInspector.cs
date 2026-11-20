using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CTCC
{
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
        private float openBlend;
        private bool hasBackSide;
        private bool showingBack;
        private InspectDetailTarget[] details = Array.Empty<InspectDetailTarget>();
        private int activeDetailIndex = -1;
        private Action onFlippedBack;
        private Action<string> onDetailFocused;
        private EnvironmentalAudio audioBus;

        public bool IsInspecting => active;
        public bool HasBackSide => hasBackSide;
        public bool ShowingBack => showingBack;
        public int ActiveDetailIndex => activeDetailIndex;
        public InspectDetailTarget[] Details => details;

        public void Initialize(Camera camera, EnvironmentalAudio audio = null)
        {
            eye = camera;
            audioBus = audio;
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

        public void Open(
            GameObject prefabOrTemplate,
            Texture documentTexture,
            float aspect,
            Volume activeGrade,
            Texture backTexture = null,
            InspectDetailTarget[] detailTargets = null,
            Action onFlipBack = null,
            Action<string> onDetail = null)
        {
            Close();
            active = true;
            openBlend = reducedMotion ? 1f : 0f;
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
            details = detailTargets ?? Array.Empty<InspectDetailTarget>();
            activeDetailIndex = -1;
            onFlippedBack = onFlipBack;
            onDetailFocused = onDetail;

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
            }
            else if (documentTexture != null)
            {
                currentModel = BuildDualSidedPaperSheet(documentTexture, backTexture, aspect);
                currentModel.transform.SetParent(holder, false);
                keyLight.intensity = 0.40f;
            }

            if (activeGrade != null && activeGrade.sharedProfile != null &&
                activeGrade.sharedProfile.TryGet<DepthOfField>(out var dof))
            {
                activeDof = dof;
                baseDofDist = dof.focusDistance.value;
                baseDofActive = dof.active;
                activeDof.active = !reducedMotion;
                activeDof.focusDistance.value = 0.40f;
            }

            stage.localPosition = new Vector3(-0.085f, -0.01f, zoom);
            stage.gameObject.SetActive(true);
            keyLight.enabled = true;
            rimLight.enabled = true;
            UpdateTransform();
        }

        public void Flip()
        {
            if (!active || !hasBackSide) return;
            showingBack = !showingBack;
            flipTarget = showingBack ? 180f : 0f;
            if (reducedMotion) flipAngle = flipTarget;
            rotX = 0f;
            rotY = 0f;
            panTarget = Vector2.zero;
            zoomTarget = 0.40f;
            activeDetailIndex = -1;
            if (eye != null) audioBus?.From("paperFlip", eye.transform.position + eye.transform.forward * 0.35f, 0.85f);
            if (showingBack) onFlippedBack?.Invoke();
            UpdateTransform();
        }

        public void FocusDetail(int index)
        {
            if (!active || details == null || index < 0 || index >= details.Length) return;
            var d = details[index];
            activeDetailIndex = index;
            if (hasBackSide && d.OnBackSide != showingBack)
            {
                showingBack = d.OnBackSide;
                flipTarget = showingBack ? 180f : 0f;
                if (reducedMotion) flipAngle = flipTarget;
                if (eye != null) audioBus?.From("paperFlip", eye.transform.position + eye.transform.forward * 0.35f, 0.80f);
                if (showingBack) onFlippedBack?.Invoke();
            }
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

        public void Close()
        {
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
            hasBackSide = false;
            showingBack = false;
            details = Array.Empty<InspectDetailTarget>();
            activeDetailIndex = -1;
            onFlippedBack = null;
            onDetailFocused = null;
            if (stage != null) stage.gameObject.SetActive(false);
            if (keyLight != null) keyLight.enabled = false;
            if (rimLight != null) rimLight.enabled = false;
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
            openBlend = Mathf.MoveTowards(openBlend, 1f, dt * (reducedMotion ? 12f : 5.5f));
            flipAngle = Mathf.MoveTowards(flipAngle, flipTarget, dt * (reducedMotion ? 1400f : 540f));
            panOffset = Vector2.MoveTowards(panOffset, panTarget, dt * (reducedMotion ? 5f : 0.65f));
            zoom = Mathf.MoveTowards(zoom, zoomTarget, dt * (reducedMotion ? 5f : 0.85f));

            var mouse = Mouse.current;
            var keys = Keyboard.current;

            if (mouse != null)
            {
                if (mouse.leftButton.isPressed || mouse.rightButton.isPressed)
                {
                    Vector2 d = mouse.delta.ReadValue();
                    rotY -= d.x * 0.32f;
                    rotX = Mathf.Clamp(rotX + d.y * 0.26f, -52f, 52f);
                    activeDetailIndex = -1;
                }
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
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
                if (keys.digit1Key.wasPressedThisFrame && details.Length >= 1) FocusDetail(0);
                if (keys.digit2Key.wasPressedThisFrame && details.Length >= 2) FocusDetail(1);
                if (keys.digit3Key.wasPressedThisFrame && details.Length >= 3) FocusDetail(2);
                if (keys.digit0Key.wasPressedThisFrame) ResetView();
            }

            UpdateTransform();
        }

        private void UpdateTransform()
        {
            float ease = 1f - Mathf.Pow(1f - openBlend, 3f);
            float baseX = -0.095f + panOffset.x;
            float baseY = Mathf.Lerp(-0.20f, 0.01f, ease) + panOffset.y;
            stage.localPosition = new Vector3(baseX, baseY, zoom);
            float idleSway = reducedMotion ? 0f : Mathf.Sin(Time.unscaledTime * 1.05f) * 1.1f;
            holder.localRotation = Quaternion.Euler(rotX + idleSway * 0.35f, rotY + flipAngle + idleSway, 0f);
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

        private GameObject BuildDualSidedPaperSheet(Texture frontTex, Texture backTex, float aspect)
        {
            float h = 0.285f;
            float w = h * Mathf.Clamp(aspect, 0.45f, 1.85f);
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
                backMaterial = MakePaperMaterial("InspectPaperBackMat", backTex, doubleSided: false);
                var backGo = new GameObject("BackFace");
                backGo.transform.SetParent(root.transform, false);
                backGo.AddComponent<MeshFilter>().sharedMesh = backMesh;
                var bmr = backGo.AddComponent<MeshRenderer>();
                bmr.sharedMaterial = backMaterial;
                bmr.shadowCastingMode = ShadowCastingMode.Off;
            }

            return root;
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
                    float curl = Mathf.Pow(u - 0.5f, 2f) * 0.026f + Mathf.Sin(v * Mathf.PI * 2f) * 0.0035f;
                    int idx = y * (nx + 1) + x;
                    verts[idx] = new Vector3((u - 0.5f) * w, (v - 0.5f) * h, -curl + zOffset);
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
            mesh.UploadMeshData(true);
            return mesh;
        }
    }
}
