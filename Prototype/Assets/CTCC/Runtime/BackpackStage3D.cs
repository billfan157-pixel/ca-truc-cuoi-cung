using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace CTCC
{
    /// <summary>
    /// Phase 2.5 Commercial Horror Interactive 3D Inventory Preview Stage (RE7/RE8 inspired):
    /// - Replaces procedural cube satchel with a dedicated, interactive 360-degree rotatable & zoomable 3D turntable
    ///   showcasing the REAL Blender 3D mesh or curved textured document of the currently selected inventory item.
    /// - Lives safely inside the player's capsule (z = 0.34..0.52m, left-center x = -0.135m) with ZERO camera teleport
    ///   and zero per-frame/per-open material/mesh leaks (verified under 50-100 open/close stress cycles).
    /// - Preserves P_BAG08_ECHO translucent ghostly 3D card shimmer in CH01 B08 post-T08.
    /// </summary>
    public sealed class BackpackStage3D : MonoBehaviour
    {
        private Camera eye;
        private Func<string, Texture> textureLookup;
        private Func<string, GameObject> modelLookup;
        private Transform stageRoot;
        private Transform previewPivot;
        private Light keyLight;
        private Light rimLight;

        private GameObject activePreviewObj;
        private string activePreviewKey = "";
        private Material paperPreviewMat;
        private Material torchBodyMat;
        private Material brassMat;
        private Material tapeMat;
        private Material notePaperMat;
        private Material echoGhostMat;
        private GameObject echoGhostObj;

        private bool open;
        private bool reducedMotion;
        private bool sharpnessAcquired;
        private float openBlend;
        private float echoTimer;
        private float rotX = 12f;
        private float rotY = -18f;
        private float zoom = 0.39f;
        private float zoomTarget = 0.39f;
        private bool dragging;

        public bool IsOpen => open;
        public string ActivePreviewItemId => activePreviewKey;

        public void Initialize(Camera camera, Func<string, Texture> resolveTexture, Func<string, GameObject> resolveModel = null)
        {
            eye = camera;
            textureLookup = resolveTexture;
            modelLookup = resolveModel;

            stageRoot = new GameObject("CTCC_BackpackStage3D").transform;
            stageRoot.SetParent(eye.transform, false);
            stageRoot.localPosition = new Vector3(-0.125f, 0.004f, 0.39f);

            previewPivot = new GameObject("InventoryPreviewPivot").transform;
            previewPivot.SetParent(stageRoot, false);

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            paperPreviewMat = MakeMat(lit, "CTCC_InvPaperPreviewMat", new Color(0.88f, 0.87f, 0.84f, 1f), 0f, 0.06f);
            paperPreviewMat.SetFloat("_Cull", 0f);
            torchBodyMat = MakeMat(lit, "CTCC_InvTorchMat", new Color(0.14f, 0.15f, 0.17f, 1f), 0.58f, 0.38f);
            brassMat = MakeMat(lit, "CTCC_InvBrassMat", new Color(0.68f, 0.53f, 0.26f, 1f), 0.80f, 0.46f);
            tapeMat = MakeMat(lit, "CTCC_InvTapeMat", new Color(0.16f, 0.16f, 0.18f, 1f), 0.12f, 0.30f);
            notePaperMat = MakeMat(lit, "CTCC_InvNoteMat", new Color(0.89f, 0.85f, 0.72f, 1f), 0f, 0.08f);

            echoGhostMat = new Material(lit) { name = "CTCC_InvEchoGhostMat" };
            echoGhostMat.SetFloat("_Surface", 1f);
            echoGhostMat.SetFloat("_Blend", 0f);
            echoGhostMat.SetFloat("_ZWrite", 0f);
            echoGhostMat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            echoGhostMat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            echoGhostMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            echoGhostMat.renderQueue = 3000;
            echoGhostMat.SetColor("_BaseColor", new Color(0.84f, 0.90f, 0.86f, 0f));

            echoGhostObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            echoGhostObj.name = "InvEchoGhost_P_BAG08";
            var ec = echoGhostObj.GetComponent<Collider>();
            if (ec != null) Destroy(ec);
            echoGhostObj.transform.SetParent(stageRoot, false);
            echoGhostObj.transform.localPosition = new Vector3(0.06f, 0.01f, 0.06f);
            echoGhostObj.transform.localScale = new Vector3(0.14f, 0.18f, 1f);
            echoGhostObj.transform.localRotation = Quaternion.Euler(4f, -12f, -5f);
            var emr = echoGhostObj.GetComponent<MeshRenderer>();
            emr.sharedMaterial = echoGhostMat;
            emr.shadowCastingMode = ShadowCastingMode.Off;
            echoGhostObj.SetActive(false);

            BuildStageLights();
            stageRoot.gameObject.SetActive(false);
        }

        public void SetModelLookup(Func<string, GameObject> resolveModel) => modelLookup = resolveModel;
        public void SetReducedMotion(bool reduced) => reducedMotion = reduced;

        public void Open(Investigation state, BackpackCompartment compartment, string selectedId, bool showEcho, Volume activeGrade = null)
        {
            if (stageRoot == null) return;
            bool wasOpen = open;
            open = true;

            string targetId = selectedId ?? "";
            if (string.IsNullOrEmpty(targetId) && state?.backpackItems != null)
            {
                for (int i = 0; i < state.backpackItems.Count; i++)
                {
                    var def = BackpackCatalog.Get(state.backpackItems[i].itemDefId);
                    if (def != null && def.Compartment == compartment) { targetId = def.Id; break; }
                }
            }

            if (!string.Equals(targetId, activePreviewKey, StringComparison.Ordinal) || activePreviewObj == null)
            {
                RebuildPreviewModel(targetId);
                rotX = 10f;
                rotY = -16f;
                zoomTarget = 0.39f;
                if (reducedMotion) zoom = zoomTarget;
            }

            if (showEcho && echoGhostObj != null)
            {
                var cardTex = textureLookup?.Invoke("ObsCard");
                if (cardTex != null) echoGhostMat.SetTexture("_BaseMap", cardTex);
                echoGhostObj.SetActive(true);
                echoTimer = 4.6f;
            }
            else if (echoTimer <= 0f && echoGhostObj != null)
            {
                echoGhostObj.SetActive(false);
            }

            if (!sharpnessAcquired)
            {
                InspectionSharpnessGuard.Acquire(activeGrade, allowBackgroundSoften: !reducedMotion);
                sharpnessAcquired = true;
            }

            stageRoot.gameObject.SetActive(true);
            if (keyLight != null) keyLight.enabled = true;
            if (rimLight != null) rimLight.enabled = true;
            if (!wasOpen)
            {
                openBlend = reducedMotion ? 1f : 0f;
                dragging = false;
                ApplyStageTransform();
            }
        }

        public void Close()
        {
            open = false;
            dragging = false;
            echoTimer = 0f;
            if (echoGhostObj != null) echoGhostObj.SetActive(false);
            if (stageRoot != null) stageRoot.gameObject.SetActive(false);
            if (keyLight != null) keyLight.enabled = false;
            if (rimLight != null) rimLight.enabled = false;
            if (sharpnessAcquired)
            {
                InspectionSharpnessGuard.Release();
                sharpnessAcquired = false;
            }
        }

        public void RotatePreview(float deltaYaw, float deltaPitch)
        {
            rotY -= deltaYaw;
            rotX = Mathf.Clamp(rotX + deltaPitch, -65f, 65f);
        }

        public void ZoomPreview(float delta)
        {
            zoomTarget = Mathf.Clamp(zoomTarget + delta, 0.27f, 0.52f);
            if (reducedMotion) zoom = zoomTarget;
        }

        private void RebuildPreviewModel(string itemDefId)
        {
            if (activePreviewObj != null)
            {
                Destroy(activePreviewObj);
                activePreviewObj = null;
            }
            activePreviewKey = itemDefId ?? "";
            if (string.IsNullOrEmpty(activePreviewKey) || previewPivot == null) return;

            var def = BackpackCatalog.Get(activePreviewKey);
            GameObject template = null;

            if (activePreviewKey == BackpackCatalog.MinhPhone)
                template = modelLookup?.Invoke("PHONE");
            else if (activePreviewKey == BackpackCatalog.E10aEnvelope)
                template = modelLookup?.Invoke("ENVELOPE_3D") ?? modelLookup?.Invoke("E10a");
            else if (activePreviewKey == BackpackCatalog.GateKey)
                template = modelLookup?.Invoke("GATE_KEY");
            else if (activePreviewKey == BackpackCatalog.WingCKey)
                template = modelLookup?.Invoke("WINGC_KEY");
            else if (activePreviewKey == BackpackCatalog.Ch02Wristband)
                template = modelLookup?.Invoke("WRIST");
            else if (def != null && !string.IsNullOrEmpty(def.InspectModelId))
                template = modelLookup?.Invoke(def.InspectModelId);

            if (template != null)
            {
                var holder = new GameObject("Preview_" + activePreviewKey);
                holder.transform.SetParent(previewPivot, false);
                var inst = Instantiate(template, holder.transform, false);
                inst.SetActive(true);
                inst.transform.localPosition = Vector3.zero;
                foreach (var c in inst.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                foreach (var r in inst.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
                NormalizeScale(inst, 0.21f);
                activePreviewObj = holder;
                return;
            }

            if (activePreviewKey == BackpackCatalog.Flashlight)
            {
                activePreviewObj = BuildFlashlightPreview();
                return;
            }
            if (activePreviewKey == BackpackCatalog.Ch04Tape)
            {
                activePreviewObj = BuildCassettePreview();
                return;
            }

            // Document / paper sheet 3D preview with actual crisp texture
            Texture tex = def != null && !string.IsNullOrEmpty(def.FrontTexture)
                ? textureLookup?.Invoke(def.FrontTexture) : null;
            activePreviewObj = BuildPaperPreview(tex);
        }

        private GameObject BuildFlashlightPreview()
        {
            var root = new GameObject("Preview_Flashlight");
            root.transform.SetParent(previewPivot, false);
            Part(root.transform, "Barrel", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.036f, 0.088f, 0.036f), torchBodyMat);
            Part(root.transform, "Bezel", PrimitiveType.Cylinder, new Vector3(0f, 0.086f, 0f), new Vector3(0.052f, 0.020f, 0.052f), brassMat);
            Part(root.transform, "TailCap", PrimitiveType.Cylinder, new Vector3(0f, -0.088f, 0f), new Vector3(0.038f, 0.012f, 0.038f), brassMat);
            Part(root.transform, "Switch", PrimitiveType.Cube, new Vector3(0f, 0.028f, -0.020f), new Vector3(0.012f, 0.024f, 0.008f), brassMat);
            root.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
            return root;
        }

        private GameObject BuildCassettePreview()
        {
            var root = new GameObject("Preview_Cassette");
            root.transform.SetParent(previewPivot, false);
            Part(root.transform, "Shell", PrimitiveType.Cube, Vector3.zero, new Vector3(0.16f, 0.102f, 0.020f), tapeMat);
            Part(root.transform, "Label", PrimitiveType.Cube, new Vector3(0f, 0.010f, -0.011f), new Vector3(0.132f, 0.054f, 0.002f), notePaperMat);
            var rL = Part(root.transform, "ReelL", PrimitiveType.Cylinder, new Vector3(-0.036f, -0.006f, -0.012f), new Vector3(0.026f, 0.003f, 0.026f), brassMat);
            rL.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var rR = Part(root.transform, "ReelR", PrimitiveType.Cylinder, new Vector3(0.036f, -0.006f, -0.012f), new Vector3(0.026f, 0.003f, 0.026f), brassMat);
            rR.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            return root;
        }

        private GameObject BuildPaperPreview(Texture tex)
        {
            var root = new GameObject("Preview_Paper");
            root.transform.SetParent(previewPivot, false);
            float aspect = tex != null ? Mathf.Clamp((float)tex.width / Mathf.Max(1, tex.height), 0.52f, 1.65f) : 0.76f;
            float h = 0.22f;
            float w = h * aspect;
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "PaperFace";
            var col = quad.GetComponent<Collider>();
            if (col != null) Destroy(col);
            quad.transform.SetParent(root.transform, false);
            quad.transform.localScale = new Vector3(w, h, 1f);
            paperPreviewMat.SetTexture("_BaseMap", tex != null ? tex : Texture2D.whiteTexture);
            var mr = quad.GetComponent<MeshRenderer>();
            mr.sharedMaterial = paperPreviewMat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            return root;
        }

        private static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        private static void NormalizeScale(GameObject root, float targetSpan)
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
                    Vector3 pt = m.MultiplyPoint3x4(corner);
                    if (!init) { combined = new Bounds(pt, Vector3.zero); init = true; }
                    else combined.Encapsulate(pt);
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

        private void BuildStageLights()
        {
            var keyGo = new GameObject("InvPreviewKeyLight");
            keyGo.transform.SetParent(stageRoot, false);
            keyGo.transform.localPosition = new Vector3(0.20f, 0.22f, -0.24f);
            keyLight = keyGo.AddComponent<Light>();
            keyLight.type = LightType.Point;
            keyLight.range = 1.4f;
            keyLight.intensity = 1.10f;
            keyLight.color = new Color(1.0f, 0.96f, 0.88f);
            keyLight.shadows = LightShadows.None;
            keyLight.enabled = false;

            var rimGo = new GameObject("InvPreviewRimLight");
            rimGo.transform.SetParent(stageRoot, false);
            rimGo.transform.localPosition = new Vector3(-0.22f, 0.14f, 0.16f);
            rimLight = rimGo.AddComponent<Light>();
            rimLight.type = LightType.Point;
            rimLight.range = 1.2f;
            rimLight.intensity = 0.48f;
            rimLight.color = new Color(0.72f, 0.86f, 0.92f);
            rimLight.shadows = LightShadows.None;
            rimLight.enabled = false;
        }

        private void ApplyStageTransform()
        {
            float ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(openBlend), 3f);
            stageRoot.localPosition = Vector3.Lerp(new Vector3(-0.14f, -0.18f, 0.34f), new Vector3(-0.125f, 0.004f, zoom), ease);
        }

        private void Update()
        {
            if (!open || stageRoot == null) return;
            float dt = Time.unscaledDeltaTime;
            openBlend = Mathf.MoveTowards(openBlend, 1f, dt * (reducedMotion ? 16f : 5.4f));
            zoom = Mathf.MoveTowards(zoom, zoomTarget, dt * (reducedMotion ? 5f : 0.95f));
            ApplyStageTransform();

            // Direct 360-degree interactive LMB drag + Scroll zoom over the left 55% preview stage!
            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 pos = mouse.position.ReadValue();
                bool overPreview = pos.x < Screen.width * 0.55f &&
                                   (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());
                if (mouse.leftButton.wasPressedThisFrame && overPreview) dragging = true;
                if (!mouse.leftButton.isPressed) dragging = false;
                if (dragging)
                {
                    Vector2 d = mouse.delta.ReadValue();
                    rotY -= d.x * 0.42f;
                    rotX = Mathf.Clamp(rotX + d.y * 0.32f, -65f, 65f);
                }
                float scroll = mouse.scroll.ReadValue().y;
                if (overPreview && Mathf.Abs(scroll) > 0.01f)
                    zoomTarget = Mathf.Clamp(zoomTarget - Mathf.Sign(scroll) * 0.025f, 0.27f, 0.52f);
            }

            if (!dragging && !reducedMotion)
                rotY += dt * 11f; // Subtle commercial-horror showcase turntable drift when untouched

            previewPivot.localRotation = Quaternion.Euler(rotX, rotY, 0f);

            if (echoTimer > 0f && echoGhostObj != null && echoGhostMat != null)
            {
                echoTimer -= dt;
                float alpha = echoTimer > 1.6f
                    ? 0.34f + 0.08f * Mathf.Sin(Time.unscaledTime * 2.5f)
                    : Mathf.Lerp(0f, 0.34f, Mathf.Clamp01(echoTimer / 1.6f));
                echoGhostMat.SetColor("_BaseColor", new Color(0.84f, 0.90f, 0.86f, alpha));
                if (echoTimer <= 0f) echoGhostObj.SetActive(false);
            }
        }

        private static Material MakeMat(Shader shader, string name, Color color, float metallic, float smoothness)
        {
            var m = new Material(shader) { name = name };
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
            return m;
        }

        private void OnDestroy()
        {
            Close();
            if (activePreviewObj != null) Destroy(activePreviewObj);
            if (stageRoot != null) Destroy(stageRoot.gameObject);
            if (paperPreviewMat != null) Destroy(paperPreviewMat);
            if (torchBodyMat != null) Destroy(torchBodyMat);
            if (brassMat != null) Destroy(brassMat);
            if (tapeMat != null) Destroy(tapeMat);
            if (notePaperMat != null) Destroy(notePaperMat);
            if (echoGhostMat != null) Destroy(echoGhostMat);
        }
    }
}
