using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace CTCC
{
    /// <summary>
    /// Renders Minh's currently equipped 3D physical item in first-person (brass gate key,
    /// oxidized Wing-C key, or held Layer B document sheet), provides 3D inspect templates for keys,
    /// and manages the physical world comparison clip beside F101 at AN02 plus Layer B sheet visibility.
    /// </summary>
    public sealed class HandItemPresenter : MonoBehaviour
    {
        private Camera eye;
        private Func<string, Texture> textureLookup;
        private Transform handRoot;
        private GameObject activeHeldModel;
        private string currentItemDefId = "";
        private bool reducedMotion;
        private float equipBlend = 1f;
        private float usePulse;
        private float swayPhase;

        // Cached 3D key templates for PhysicalInspector
        private GameObject gateKeyTemplate;
        private GameObject wingCKeyTemplate;
        private Material brassMat, ironMat, tagPaperMat, heldPaperMat;

        // Physical comparison clip mounted beside F101 in LOC03
        private GameObject compareClipRoot;
        private MeshRenderer compareSheetRenderer;
        private Material compareSheetMaterial;
        private SceneTarget compareClipTarget;
        private string currentPlacedItemId = "";

        // Optional world-sheet visibility tracking when Minh stows a Layer B paper
        private Renderer obs101CSheet;
        private Renderer obs101DSheet;
        private Renderer obsShiftSheet;
        private Renderer obs101BSheet;
        private bool worldSheetsBound;

        public string EquippedItemDefId => currentItemDefId;
        public string PlacedAn02ItemDefId => currentPlacedItemId;

        public void Initialize(Camera camera, Func<string, Texture> resolveTexture)
        {
            eye = camera;
            textureLookup = resolveTexture;

            handRoot = new GameObject("Minh Held Item Rig").transform;
            handRoot.SetParent(eye.transform, false);
            handRoot.localPosition = new Vector3(0.24f, -0.20f, 0.44f);

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            brassMat = new Material(lit) { name = "CTCC_HandKeyBrass" };
            brassMat.SetColor("_BaseColor", new Color(0.64f, 0.50f, 0.26f, 1f));
            brassMat.SetFloat("_Metallic", 0.78f);
            brassMat.SetFloat("_Smoothness", 0.44f);

            ironMat = new Material(lit) { name = "CTCC_HandKeyIron" };
            ironMat.SetColor("_BaseColor", new Color(0.38f, 0.40f, 0.42f, 1f));
            ironMat.SetFloat("_Metallic", 0.82f);
            ironMat.SetFloat("_Smoothness", 0.34f);

            tagPaperMat = new Material(lit) { name = "CTCC_HandKeyTag" };
            tagPaperMat.SetColor("_BaseColor", new Color(0.84f, 0.79f, 0.65f, 1f));
            tagPaperMat.SetFloat("_Metallic", 0f);
            tagPaperMat.SetFloat("_Smoothness", 0.08f);

            heldPaperMat = new Material(lit) { name = "CTCC_HeldPaperMat" };
            heldPaperMat.SetColor("_BaseColor", new Color(0.86f, 0.85f, 0.82f, 1f));
            heldPaperMat.SetFloat("_Metallic", 0f);
            heldPaperMat.SetFloat("_Smoothness", 0.06f);
            heldPaperMat.SetFloat("_Cull", 0f);

            gateKeyTemplate = BuildKeyModel("KeyTemplate_Gate", brassMat, includeTag: false);
            gateKeyTemplate.SetActive(false);
            wingCKeyTemplate = BuildKeyModel("KeyTemplate_WingC", ironMat, includeTag: true);
            wingCKeyTemplate.SetActive(false);
        }

        public void SetReducedMotion(bool reduced) => reducedMotion = reduced;

        public GameObject GetInspectTemplate(string itemDefId) => itemDefId switch
        {
            BackpackCatalog.GateKey => gateKeyTemplate,
            BackpackCatalog.WingCKey => wingCKeyTemplate,
            _ => null
        };

        public void TriggerUseAnimation()
        {
            if (!reducedMotion) usePulse = 1f;
        }

        public void SyncState(Investigation state, HospitalScene scene, bool hideViewModel)
        {
            var equipped = InventoryService.GetEquipped(state);
            string targetId = hideViewModel || equipped == null ? "" : equipped.itemDefId;
            if (!string.Equals(targetId, currentItemDefId, StringComparison.Ordinal))
            {
                currentItemDefId = targetId;
                RebuildHeldVisual();
            }
            if (handRoot != null)
                handRoot.gameObject.SetActive(!hideViewModel && activeHeldModel != null);

            EnsureWorldComparisonClip(scene);
            SyncComparisonClip(state, scene);
            SyncWorldStowedSheets(state, scene);
        }

        public void ResetWorldBindings()
        {
            worldSheetsBound = false;
            obs101BSheet = obs101CSheet = obs101DSheet = obsShiftSheet = null;
            if (compareClipRoot != null)
            {
                Destroy(compareClipRoot);
                compareClipRoot = null;
                compareSheetRenderer = null;
                compareClipTarget = null;
            }
            currentPlacedItemId = "";
        }

        private void RebuildHeldVisual()
        {
            if (activeHeldModel != null)
            {
                Destroy(activeHeldModel);
                activeHeldModel = null;
            }
            if (string.IsNullOrEmpty(currentItemDefId) || handRoot == null) return;

            if (currentItemDefId == BackpackCatalog.GateKey)
            {
                activeHeldModel = Instantiate(gateKeyTemplate, handRoot, false);
                activeHeldModel.name = "Held_GateKey";
                activeHeldModel.SetActive(true);
                activeHeldModel.transform.localPosition = Vector3.zero;
                activeHeldModel.transform.localRotation = Quaternion.Euler(14f, -28f, 18f);
                activeHeldModel.transform.localScale = Vector3.one * 0.92f;
            }
            else if (currentItemDefId == BackpackCatalog.WingCKey)
            {
                activeHeldModel = Instantiate(wingCKeyTemplate, handRoot, false);
                activeHeldModel.name = "Held_WingCKey";
                activeHeldModel.SetActive(true);
                activeHeldModel.transform.localPosition = Vector3.zero;
                activeHeldModel.transform.localRotation = Quaternion.Euler(16f, -32f, 14f);
                activeHeldModel.transform.localScale = Vector3.one * 0.92f;
            }
            else
            {
                var def = BackpackCatalog.Get(currentItemDefId);
                if (def != null && !string.IsNullOrEmpty(def.FrontTexture))
                {
                    var tex = textureLookup?.Invoke(def.FrontTexture);
                    activeHeldModel = BuildHeldPaperVisual(def.ShortLabel, tex);
                    activeHeldModel.transform.SetParent(handRoot, false);
                    activeHeldModel.transform.localPosition = new Vector3(-0.02f, -0.02f, 0.04f);
                    activeHeldModel.transform.localRotation = Quaternion.Euler(22f, -18f, 8f);
                }
            }
            equipBlend = reducedMotion ? 1f : 0f;
        }

        private GameObject BuildHeldPaperVisual(string label, Texture tex)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "HeldPaper_" + label;
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.localScale = new Vector3(0.18f, 0.24f, 1f);
            var mr = go.GetComponent<MeshRenderer>();
            heldPaperMat.SetTexture("_BaseMap", tex != null ? tex : Texture2D.whiteTexture);
            mr.sharedMaterial = heldPaperMat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        private GameObject BuildKeyModel(string name, Material metal, bool includeTag)
        {
            var root = new GameObject(name);
            root.transform.SetParent(transform, false);

            // Key bow (ring handle composed of 4 bevelled bars for crisp 3D silhouette)
            AddPart(root, "BowTop", PrimitiveType.Cube, new Vector3(0, 0, -0.056f), new Vector3(0.036f, 0.005f, 0.007f), metal);
            AddPart(root, "BowBottom", PrimitiveType.Cube, new Vector3(0, 0, -0.024f), new Vector3(0.036f, 0.005f, 0.007f), metal);
            AddPart(root, "BowLeft", PrimitiveType.Cube, new Vector3(-0.016f, 0, -0.040f), new Vector3(0.007f, 0.005f, 0.036f), metal);
            AddPart(root, "BowRight", PrimitiveType.Cube, new Vector3(0.016f, 0, -0.040f), new Vector3(0.007f, 0.005f, 0.036f), metal);

            // Collar + cylindrical shaft
            var collar = AddPart(root, "Collar", PrimitiveType.Cylinder, new Vector3(0, 0, -0.015f), new Vector3(0.012f, 0.005f, 0.012f), metal);
            collar.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            var shaft = AddPart(root, "Shaft", PrimitiveType.Cylinder, new Vector3(0, 0, 0.028f), new Vector3(0.0075f, 0.044f, 0.0075f), metal);
            shaft.transform.localRotation = Quaternion.Euler(90f, 0, 0);

            // Key teeth / ward bit
            AddPart(root, "BitMain", PrimitiveType.Cube, new Vector3(0.011f, 0, 0.052f), new Vector3(0.018f, 0.0042f, 0.024f), metal);
            AddPart(root, "BitNotch", PrimitiveType.Cube, new Vector3(0.015f, 0, 0.044f), new Vector3(0.008f, 0.0044f, 0.006f), metal);
            AddPart(root, "BitTip", PrimitiveType.Cube, new Vector3(0.013f, 0, 0.064f), new Vector3(0.013f, 0.0042f, 0.007f), metal);

            if (includeTag)
            {
                // Small paper archive tag tied to Wing-C key bow
                var wire = AddPart(root, "TagRing", PrimitiveType.Cube, new Vector3(-0.022f, 0, -0.050f), new Vector3(0.016f, 0.002f, 0.003f), ironMat);
                wire.transform.localRotation = Quaternion.Euler(0, 26f, 0);
                var tag = AddPart(root, "TagCard", PrimitiveType.Cube, new Vector3(-0.040f, -0.002f, -0.062f), new Vector3(0.030f, 0.002f, 0.020f), tagPaperMat);
                tag.transform.localRotation = Quaternion.Euler(8f, 22f, -6f);
            }

            return root;
        }

        private static GameObject AddPart(GameObject parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        private void EnsureWorldComparisonClip(HospitalScene scene)
        {
            if (scene == null || scene.f101WallRenderer == null) return;
            if (compareClipRoot != null) return;

            var parent = scene.f101WallRenderer.transform.parent;
            if (parent == null) return;

            // Mount physical comparison clip tightly beside F101 on Room 101's west interior wall (z = 5.79, right next to F101 at z = 5.50)
            compareClipRoot = new GameObject("AN02_ComparisonClipAnchor");
            compareClipRoot.transform.SetParent(parent, false);
            compareClipRoot.transform.position = new Vector3(5.724f, 1.365f, 5.79f);
            compareClipRoot.transform.rotation = Quaternion.LookRotation(Vector3.right);

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var boardMat = new Material(lit) { name = "CTCC_CompareBoardMat" };
            boardMat.SetColor("_BaseColor", new Color(0.24f, 0.19f, 0.15f, 1f));
            boardMat.SetFloat("_Smoothness", 0.20f);

            var backing = AddPart(compareClipRoot, "ClipBacking", PrimitiveType.Cube, new Vector3(0, 0, 0.004f), new Vector3(0.236f, 0.324f, 0.010f), boardMat);
            backing.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            AddPart(compareClipRoot, "MetalSpringClip", PrimitiveType.Cube, new Vector3(0, 0.145f, 0.014f), new Vector3(0.088f, 0.032f, 0.014f), ironMat);

            var sheet = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sheet.name = "ClippedComparisonSheet";
            var col = sheet.GetComponent<Collider>();
            if (col != null) Destroy(col);
            sheet.transform.SetParent(compareClipRoot.transform, false);
            sheet.transform.localPosition = new Vector3(0, -0.008f, 0.014f);
            sheet.transform.localRotation = Quaternion.Euler(0, 180f, 0);
            sheet.transform.localScale = new Vector3(0.214f, 0.285f, 1f);
            compareSheetMaterial = new Material(lit) { name = "CTCC_CompareSheetMat" };
            compareSheetMaterial.SetColor("_BaseColor", new Color(0.86f, 0.85f, 0.82f, 1f));
            compareSheetMaterial.SetFloat("_Smoothness", 0.06f);
            compareSheetRenderer = sheet.GetComponent<MeshRenderer>();
            compareSheetRenderer.sharedMaterial = compareSheetMaterial;
            compareSheetRenderer.shadowCastingMode = ShadowCastingMode.Off;
            compareSheetRenderer.enabled = false;

            var hit = new GameObject("COMPARE_CLIP_AN02") { layer = HospitalScene.InteractLayer };
            hit.transform.SetParent(compareClipRoot.transform, false);
            hit.transform.localPosition = new Vector3(0, 0, 0.035f);
            var box = hit.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(0.30f, 0.38f, 0.14f);
            compareClipTarget = hit.AddComponent<SceneTarget>();
            compareClipTarget.id = "COMPARE_CLIP_AN02";
            compareClipTarget.title = "Kẹp đối chiếu cạnh F101";
            compareClipTarget.focusPriority = 0;
            compareClipTarget.handhold = hit.transform;
            compareClipTarget.frontOnly = true;
            hit.transform.rotation = Quaternion.LookRotation(Vector3.right);
        }

        private void SyncComparisonClip(Investigation state, HospitalScene scene)
        {
            if (compareClipRoot == null) return;
            bool inLayerB = state != null && state.layer == Reality.B && state.chapter is ("CH01" or "CH02");
            compareClipRoot.SetActive(inLayerB);
            if (!inLayerB)
            {
                currentPlacedItemId = "";
                if (compareSheetRenderer != null) compareSheetRenderer.enabled = false;
                return;
            }

            var placed = InventoryService.GetPlacedAt(state, WorldAnchors.An02CompareClip);
            string nextId = placed?.itemDefId ?? "";
            if (nextId != currentPlacedItemId)
            {
                currentPlacedItemId = nextId;
                if (string.IsNullOrEmpty(nextId))
                {
                    if (compareSheetRenderer != null) compareSheetRenderer.enabled = false;
                    if (compareClipTarget != null) compareClipTarget.title = "Kẹp đối chiếu cạnh F101";
                }
                else
                {
                    var def = BackpackCatalog.Get(nextId);
                    var tex = def != null ? textureLookup?.Invoke(def.FrontTexture) : null;
                    if (compareSheetMaterial != null)
                        compareSheetMaterial.SetTexture("_BaseMap", tex != null ? tex : Texture2D.whiteTexture);
                    if (compareSheetRenderer != null) compareSheetRenderer.enabled = true;
                    if (compareClipTarget != null && def != null)
                        compareClipTarget.title = $"Tài liệu đang kẹp · {def.ShortLabel}";
                }
            }
        }

        private void SyncWorldStowedSheets(Investigation state, HospitalScene scene)
        {
            if (state == null || scene == null || scene.f101WallRenderer == null) return;
            if (!worldSheetsBound)
            {
                var wardScene = scene.f101WallRenderer.gameObject.scene;
                if (!wardScene.IsValid() || !wardScene.isLoaded) return;
                foreach (var root in wardScene.GetRootGameObjects())
                {
                    foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                    {
                        string n = r.gameObject.name;
                        if (n == "OBS101C_Bed2Chart") obs101CSheet = r;
                        else if (n == "Bed3MedsSheet") obs101DSheet = r;
                        else if (n == "ShiftGuideDrawerSheet") obsShiftSheet = r;
                        else if (n.Contains("Bed4ChartCard") || n.Contains("CardSheet") ||
                                 (r.sharedMaterial != null && r.sharedMaterial.name.Contains("ObsCard")))
                            obs101BSheet = r;
                    }
                }
                worldSheetsBound = true;
            }

            bool layerB = state.layer == Reality.B;
            SetSheetVisible(obs101BSheet, !layerB || !IsTakenFromFixture(state, BackpackCatalog.PaperObs101B));
            SetSheetVisible(obs101CSheet, !layerB || !IsTakenFromFixture(state, BackpackCatalog.PaperObs101C));
            SetSheetVisible(obs101DSheet, !layerB || !IsTakenFromFixture(state, BackpackCatalog.PaperObs101D));
            SetSheetVisible(obsShiftSheet, !layerB || !IsTakenFromFixture(state, BackpackCatalog.PaperShiftLog));
        }

        private static bool IsTakenFromFixture(Investigation state, string itemDefId)
        {
            var e = InventoryService.FindByDef(state, itemDefId);
            return e != null && e.status is (BackpackItemStatus.InBag or BackpackItemStatus.Equipped or BackpackItemStatus.Placed);
        }

        private static void SetSheetVisible(Renderer r, bool visible)
        {
            if (r != null && r.enabled != visible) r.enabled = visible;
        }

        private void Update()
        {
            if (handRoot == null || !handRoot.gameObject.activeSelf || activeHeldModel == null) return;
            float dt = Time.unscaledDeltaTime;
            equipBlend = Mathf.MoveTowards(equipBlend, 1f, dt * (reducedMotion ? 12f : 4.5f));
            usePulse = Mathf.MoveTowards(usePulse, 0f, dt * 3.8f);
            float ease = 1f - Mathf.Pow(1f - equipBlend, 3f);

            float swayX = 0f, swayY = 0f;
            if (!reducedMotion)
            {
                swayPhase += dt * 1.9f;
                swayX = Mathf.Sin(swayPhase) * 0.0035f;
                swayY = Mathf.Cos(swayPhase * 1.7f) * 0.0028f;
            }
            float thrust = Mathf.Sin(usePulse * Mathf.PI) * 0.065f;
            Vector3 rest = new(0.24f + swayX, -0.20f + swayY, 0.44f + thrust);
            Vector3 lowered = new(0.26f, -0.34f, 0.36f);
            handRoot.localPosition = Vector3.Lerp(lowered, rest, ease);
        }

        private void OnDestroy()
        {
            if (activeHeldModel != null) Destroy(activeHeldModel);
            if (gateKeyTemplate != null) Destroy(gateKeyTemplate);
            if (wingCKeyTemplate != null) Destroy(wingCKeyTemplate);
            if (handRoot != null) Destroy(handRoot.gameObject);
            if (compareClipRoot != null) Destroy(compareClipRoot);
            if (brassMat != null) Destroy(brassMat);
            if (ironMat != null) Destroy(ironMat);
            if (tagPaperMat != null) Destroy(tagPaperMat);
            if (heldPaperMat != null) Destroy(heldPaperMat);
            if (compareSheetMaterial != null) Destroy(compareSheetMaterial);
        }
    }
}
