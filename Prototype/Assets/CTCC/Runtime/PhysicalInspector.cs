using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CTCC
{
    /// <summary>
    /// Diegetic 3D close-up evidence & document viewer.
    /// Brings a lit, rotatable 3D physical artifact right in front of the player camera with subtle DOF blur,
    /// supporting both mouse-drag and full keyboard rotation/zoom (accessibility preserved).
    /// </summary>
    public sealed class PhysicalInspector : MonoBehaviour
    {
        private Camera eye;
        private Transform stage;
        private Transform holder;
        private Light keyLight;
        private Light rimLight;
        private GameObject currentModel;
        private Mesh documentMesh;
        private Material documentMaterial;
        private DepthOfField activeDof;
        private float baseDofDist = 4.5f;
        private bool baseDofActive;
        private float rotX, rotY, zoom = 0.44f;
        private bool active;
        private bool reducedMotion;
        private float openBlend;

        public bool IsInspecting => active;

        public void Initialize(Camera camera)
        {
            eye = camera;
            stage = new GameObject("Physical Inspector Stage").transform;
            stage.SetParent(eye.transform, false);
            stage.localPosition = new Vector3(0f, -0.025f, 0.44f);

            holder = new GameObject("Inspect Holder").transform;
            holder.SetParent(stage, false);

            var keyGo = new GameObject("Inspect Key Light");
            keyGo.transform.SetParent(eye.transform, false);
            keyGo.transform.localPosition = new Vector3(0.24f, 0.22f, 0.08f);
            keyLight = keyGo.AddComponent<Light>();
            keyLight.type = LightType.Point;
            keyLight.range = 1.6f;
            keyLight.intensity = 1.05f;
            keyLight.color = new Color(1.0f, 0.94f, 0.84f);
            keyLight.shadows = LightShadows.None;

            var rimGo = new GameObject("Inspect Rim Light");
            rimGo.transform.SetParent(eye.transform, false);
            rimGo.transform.localPosition = new Vector3(-0.28f, -0.10f, 0.35f);
            rimLight = rimGo.AddComponent<Light>();
            rimLight.type = LightType.Point;
            rimLight.range = 1.4f;
            rimLight.intensity = 0.40f;
            rimLight.color = new Color(0.70f, 0.84f, 0.92f);
            rimLight.shadows = LightShadows.None;

            stage.gameObject.SetActive(false);
            keyLight.enabled = false;
            rimLight.enabled = false;
        }

        public void SetReducedMotion(bool reduced) => reducedMotion = reduced;

        public void Open(GameObject prefabOrTemplate, Texture documentTexture, float aspect, Volume activeGrade)
        {
            Close();
            active = true;
            openBlend = reducedMotion ? 1f : 0f;
            rotX = 0f;
            rotY = 0f;
            zoom = 0.43f;

            if (prefabOrTemplate != null)
            {
                var pivot = new GameObject("InspectModelPivot");
                pivot.transform.SetParent(holder, false);
                pivot.transform.localPosition = Vector3.zero;
                pivot.transform.localRotation = Quaternion.identity;

                var inst = Instantiate(prefabOrTemplate, pivot.transform, false);
                inst.SetActive(true);
                inst.transform.localPosition = Vector3.zero;
                // Disable colliders on preview replica so it never blocks raycasts or player capsule
                foreach (var c in inst.GetComponentsInChildren<Collider>(true))
                    c.enabled = false;
                foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                    r.shadowCastingMode = ShadowCastingMode.Off;

                NormalizeModelScale(inst, 0.25f);
                // Present flat-lying hero artifacts (Bảy's envelope + keys, wristband) angled cleanly face-up toward camera!
                pivot.transform.localRotation = Quaternion.Euler(-62f, 16f, -10f);
                currentModel = pivot;
            }
            else if (documentTexture != null)
            {
                currentModel = BuildCurvedPaperSheet(documentTexture, aspect);
                currentModel.transform.SetParent(holder, false);
            }

            if (activeGrade != null && activeGrade.sharedProfile != null &&
                activeGrade.sharedProfile.TryGet<DepthOfField>(out var dof))
            {
                activeDof = dof;
                baseDofDist = dof.focusDistance.value;
                baseDofActive = dof.active;
                activeDof.active = !reducedMotion;
                activeDof.focusDistance.value = 0.42f;
            }

            stage.localPosition = new Vector3(-0.08f, -0.01f, zoom);
            stage.gameObject.SetActive(true);
            keyLight.enabled = true;
            rimLight.enabled = true;
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
            if (documentMesh != null) Destroy(documentMesh);
            if (documentMaterial != null) Destroy(documentMaterial);
            documentMesh = null;
            documentMaterial = null;
            if (activeDof != null)
            {
                activeDof.focusDistance.value = baseDofDist;
                activeDof.active = baseDofActive;
                activeDof = null;
            }
            active = false;
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

            openBlend = Mathf.MoveTowards(openBlend, 1f, Time.unscaledDeltaTime * (reducedMotion ? 10f : 5.2f));
            var mouse = Mouse.current;
            var keys = Keyboard.current;

            if (mouse != null)
            {
                if (mouse.leftButton.isPressed || mouse.rightButton.isPressed)
                {
                    Vector2 d = mouse.delta.ReadValue();
                    rotY -= d.x * 0.34f;
                    rotX = Mathf.Clamp(rotX + d.y * 0.28f, -55f, 55f);
                }
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                    zoom = Mathf.Clamp(zoom - Mathf.Sign(scroll) * 0.03f, 0.25f, 0.62f);
            }

            if (keys != null)
            {
                float dt = Time.unscaledDeltaTime;
                if (keys.aKey.isPressed) rotY += 95f * dt;
                if (keys.dKey.isPressed) rotY -= 95f * dt;
                if (keys.wKey.isPressed) rotX = Mathf.Clamp(rotX + 75f * dt, -55f, 55f);
                if (keys.sKey.isPressed) rotX = Mathf.Clamp(rotX - 75f * dt, -55f, 55f);
                if (keys.qKey.isPressed) zoom = Mathf.Clamp(zoom - 0.35f * dt, 0.25f, 0.62f);
                if (keys.zKey.isPressed) zoom = Mathf.Clamp(zoom + 0.35f * dt, 0.25f, 0.62f);
            }

            UpdateTransform();
        }

        private void UpdateTransform()
        {
            float ease = 1f - Mathf.Pow(1f - openBlend, 3f);
            stage.localPosition = new Vector3(-0.085f, Mathf.Lerp(-0.22f, 0.01f, ease), zoom);
            float idleSway = reducedMotion ? 0f : Mathf.Sin(Time.unscaledTime * 1.15f) * 1.4f;
            holder.localRotation = Quaternion.Euler(rotX + idleSway * 0.4f, rotY + idleSway, 0f);
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

        private GameObject BuildCurvedPaperSheet(Texture tex, float aspect)
        {
            float h = 0.28f;
            float w = h * Mathf.Clamp(aspect, 0.45f, 1.85f);
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
                    // Subtle natural paper curl & central fold crease so it reads as physical paper in 3D!
                    float curl = (Mathf.Pow(u - 0.5f, 2f) * 0.032f + Mathf.Sin(v * Mathf.PI * 2f) * 0.004f);
                    int idx = y * (nx + 1) + x;
                    verts[idx] = new Vector3((u - 0.5f) * w, (v - 0.5f) * h, -curl);
                    uvs[idx] = new Vector2(u, v);
                    if (x < nx && y < ny)
                    {
                        int a = idx;
                        int b = idx + 1;
                        int c = idx + nx + 1;
                        int d = c + 1;
                        tris[ti++] = a; tris[ti++] = c; tris[ti++] = b;
                        tris[ti++] = b; tris[ti++] = c; tris[ti++] = d;
                    }
                }
            }
            documentMesh = new Mesh { name = "InspectPaperSheet", vertices = verts, uv = uvs, triangles = tris };
            documentMesh.RecalculateNormals();
            documentMesh.UploadMeshData(true);

            var go = new GameObject("InspectDocument");
            go.AddComponent<MeshFilter>().sharedMesh = documentMesh;
            documentMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "InspectPaperMaterial" };
            documentMaterial.SetTexture("_BaseMap", tex);
            documentMaterial.SetColor("_BaseColor", Color.white);
            documentMaterial.SetFloat("_Smoothness", 0.14f);
            documentMaterial.SetFloat("_Cull", 0);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = documentMaterial;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }
    }
}
