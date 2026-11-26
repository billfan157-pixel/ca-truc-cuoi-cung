using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CTCC
{
    /// <summary>
    /// Renders Minh's currently equipped 3D physical item in first-person (brass gate key,
    /// oxidized Wing-C key, C01 wristband, CH04 cassette tape, or held paper/drawing sheets),
    /// provides 3D inspect templates for tools, manages ALL physical world comparison anchors
    /// (AN02 wall clip beside F101 + 2-slot comparison surfaces at Reception, STAFF desk, and Room 101),
    /// and syncs stowed Layer B world-sheet visibility.
    /// </summary>
    public sealed class HandItemPresenter : MonoBehaviour
    {
        private sealed class AnchorSlotVisual
        {
            public string AnchorId;
            public MeshRenderer SheetRenderer;
            public Material SheetMaterial;
            public GameObject PropFallbackVisual;
            public string CurrentItemId = "";
        }

        private Camera eye;
        private Func<string, Texture> textureLookup;
        private Transform handRoot;
        private GameObject activeHeldModel;
        private string currentItemDefId = "";
        private bool reducedMotion;
        private float equipBlend = 1f;
        private float usePulse;
        private float swayPhase;
        private float walkBobPhase;
        private Vector3 lastEyeWorldPos;
        private bool hasEyeWorldPos;

        // Cached 3D tool templates for PhysicalInspector
        private GameObject gateKeyTemplate;
        private GameObject wingCKeyTemplate;
        private GameObject cassetteTapeTemplate;
        private GameObject wristbandTemplate;
        private Material brassMat, ironMat, tagPaperMat, heldPaperMat, boardMat, traySteelMat, tapeMat, wristMat;

        // World comparison surfaces (parented to this Bootstrap component so zone reloads restore cleanly without mutating baked scene colliders)
        private GameObject compareClipAn02Root;
        private SceneTarget compareClipAn02Target;
        private GameObject compareDeskStaffRoot;
        private GameObject compareDeskReceptionRoot;
        private GameObject compareDeskWard101Root;
        private readonly Dictionary<string, AnchorSlotVisual> anchorSlots = new(StringComparer.Ordinal);
        private bool surfacesBuilt;

        // Optional world-sheet visibility tracking when Minh stows a Layer B paper
        private Renderer obs101CSheet;
        private Renderer obs101DSheet;
        private Renderer obsShiftSheet;
        private Renderer obs101BSheet;
        private bool worldSheetsBound;

        public string EquippedItemDefId => currentItemDefId;
        public string PlacedAn02ItemDefId =>
            anchorSlots.TryGetValue(WorldAnchors.An02CompareClip, out var v) ? v.CurrentItemId : "";

        public string PlacedItemAt(string anchorId) =>
            !string.IsNullOrEmpty(anchorId) && anchorSlots.TryGetValue(anchorId, out var v) ? v.CurrentItemId : "";

        public void Initialize(Camera camera, Func<string, Texture> resolveTexture)
        {
            eye = camera;
            textureLookup = resolveTexture;

            handRoot = new GameObject("Minh Held Item Rig").transform;
            handRoot.SetParent(eye.transform, false);
            handRoot.localPosition = new Vector3(0.24f, -0.20f, 0.44f);

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            brassMat = MakeMat(lit, "CTCC_HandKeyBrass", new Color(0.64f, 0.50f, 0.26f, 1f), 0.78f, 0.44f);
            ironMat = MakeMat(lit, "CTCC_HandKeyIron", new Color(0.38f, 0.40f, 0.42f, 1f), 0.82f, 0.34f);
            tagPaperMat = MakeMat(lit, "CTCC_HandKeyTag", new Color(0.84f, 0.79f, 0.65f, 1f), 0f, 0.08f);
            heldPaperMat = MakeMat(lit, "CTCC_HeldPaperMat", new Color(0.86f, 0.85f, 0.82f, 1f), 0f, 0.06f);
            heldPaperMat.SetFloat("_Cull", 0f);
            boardMat = MakeMat(lit, "CTCC_CompareBoardMat", new Color(0.24f, 0.19f, 0.15f, 1f), 0f, 0.20f);
            traySteelMat = MakeMat(lit, "CTCC_CompareTraySteel", new Color(0.24f, 0.28f, 0.26f, 1f), 0.55f, 0.36f);
            tapeMat = MakeMat(lit, "CTCC_HandCassetteMat", new Color(0.15f, 0.15f, 0.17f, 1f), 0.12f, 0.28f);
            wristMat = MakeMat(lit, "CTCC_HandWristMat", new Color(0.86f, 0.88f, 0.85f, 1f), 0f, 0.30f);

            gateKeyTemplate = BuildKeyModel("KeyTemplate_Gate", brassMat, includeTag: false);
            gateKeyTemplate.SetActive(false);
            wingCKeyTemplate = BuildKeyModel("KeyTemplate_WingC", ironMat, includeTag: true);
            wingCKeyTemplate.SetActive(false);
            cassetteTapeTemplate = BuildCassetteModel("TapeTemplate_E06a");
            cassetteTapeTemplate.SetActive(false);
            wristbandTemplate = BuildWristbandModel("WristTemplate_C01");
            wristbandTemplate.SetActive(false);
        }

        public void SetReducedMotion(bool reduced) => reducedMotion = reduced;

        public GameObject GetInspectTemplate(string itemDefId) => itemDefId switch
        {
            BackpackCatalog.GateKey => gateKeyTemplate,
            BackpackCatalog.WingCKey => wingCKeyTemplate,
            BackpackCatalog.Ch04Tape => cassetteTapeTemplate,
            BackpackCatalog.Ch02Wristband => wristbandTemplate,
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

            EnsureComparisonSurfaces(scene);
            SyncComparisonSurfaces(state, scene);
            SyncWorldStowedSheets(state, scene);
        }

        public void ResetWorldBindings()
        {
            worldSheetsBound = false;
            obs101BSheet = obs101CSheet = obs101DSheet = obsShiftSheet = null;
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
            else if (currentItemDefId == BackpackCatalog.Ch04Tape)
            {
                activeHeldModel = Instantiate(cassetteTapeTemplate, handRoot, false);
                activeHeldModel.name = "Held_CassetteTape";
                activeHeldModel.SetActive(true);
                activeHeldModel.transform.localPosition = new Vector3(-0.01f, -0.01f, 0.02f);
                activeHeldModel.transform.localRotation = Quaternion.Euler(18f, -22f, 8f);
            }
            else if (currentItemDefId == BackpackCatalog.Ch02Wristband)
            {
                activeHeldModel = Instantiate(wristbandTemplate, handRoot, false);
                activeHeldModel.name = "Held_WristbandC01";
                activeHeldModel.SetActive(true);
                activeHeldModel.transform.localPosition = Vector3.zero;
                activeHeldModel.transform.localRotation = Quaternion.Euler(20f, -18f, 10f);
            }
            else
            {
                var def = BackpackCatalog.Get(currentItemDefId);
                if (def != null)
                {
                    var tex = !string.IsNullOrEmpty(def.FrontTexture) ? textureLookup?.Invoke(def.FrontTexture) : null;
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

            AddPart(root, "BowTop", PrimitiveType.Cube, new Vector3(0, 0, -0.056f), new Vector3(0.036f, 0.005f, 0.007f), metal);
            AddPart(root, "BowBottom", PrimitiveType.Cube, new Vector3(0, 0, -0.024f), new Vector3(0.036f, 0.005f, 0.007f), metal);
            AddPart(root, "BowLeft", PrimitiveType.Cube, new Vector3(-0.016f, 0, -0.040f), new Vector3(0.007f, 0.005f, 0.036f), metal);
            AddPart(root, "BowRight", PrimitiveType.Cube, new Vector3(0.016f, 0, -0.040f), new Vector3(0.007f, 0.005f, 0.036f), metal);

            var collar = AddPart(root, "Collar", PrimitiveType.Cylinder, new Vector3(0, 0, -0.015f), new Vector3(0.012f, 0.005f, 0.012f), metal);
            collar.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            var shaft = AddPart(root, "Shaft", PrimitiveType.Cylinder, new Vector3(0, 0, 0.028f), new Vector3(0.0075f, 0.044f, 0.0075f), metal);
            shaft.transform.localRotation = Quaternion.Euler(90f, 0, 0);

            AddPart(root, "BitMain", PrimitiveType.Cube, new Vector3(0.011f, 0, 0.052f), new Vector3(0.018f, 0.0042f, 0.024f), metal);
            AddPart(root, "BitNotch", PrimitiveType.Cube, new Vector3(0.015f, 0, 0.044f), new Vector3(0.008f, 0.0044f, 0.006f), metal);
            AddPart(root, "BitTip", PrimitiveType.Cube, new Vector3(0.013f, 0, 0.064f), new Vector3(0.013f, 0.0042f, 0.007f), metal);

            if (includeTag)
            {
                var wire = AddPart(root, "TagRing", PrimitiveType.Cube, new Vector3(-0.022f, 0, -0.050f), new Vector3(0.016f, 0.002f, 0.003f), ironMat);
                wire.transform.localRotation = Quaternion.Euler(0, 26f, 0);
                var tag = AddPart(root, "TagCard", PrimitiveType.Cube, new Vector3(-0.040f, -0.002f, -0.062f), new Vector3(0.030f, 0.002f, 0.020f), tagPaperMat);
                tag.transform.localRotation = Quaternion.Euler(8f, 22f, -6f);
            }

            return root;
        }

        private GameObject BuildCassetteModel(string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(transform, false);
            AddPart(root, "Shell", PrimitiveType.Cube, Vector3.zero, new Vector3(0.10f, 0.064f, 0.014f), tapeMat);
            AddPart(root, "Label", PrimitiveType.Cube, new Vector3(0f, 0.006f, -0.0075f), new Vector3(0.082f, 0.034f, 0.002f), tagPaperMat);
            var reelL = AddPart(root, "ReelL", PrimitiveType.Cylinder, new Vector3(-0.022f, -0.004f, -0.008f), new Vector3(0.016f, 0.002f, 0.016f), ironMat);
            reelL.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            var reelR = AddPart(root, "ReelR", PrimitiveType.Cylinder, new Vector3(0.022f, -0.004f, -0.008f), new Vector3(0.016f, 0.002f, 0.016f), ironMat);
            reelR.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            return root;
        }

        private GameObject BuildWristbandModel(string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(transform, false);
            AddPart(root, "BandOuter", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.068f, 0.012f, 0.048f), wristMat);
            AddPart(root, "InsertCard", PrimitiveType.Cube, new Vector3(0f, 0f, -0.025f), new Vector3(0.038f, 0.018f, 0.004f), tagPaperMat);
            AddPart(root, "SnapClasp", PrimitiveType.Cube, new Vector3(0f, 0f, 0.024f), new Vector3(0.012f, 0.014f, 0.008f), ironMat);
            return root;
        }

        private void EnsureComparisonSurfaces(HospitalScene scene)
        {
            if (surfacesBuilt || scene == null) return;
            surfacesBuilt = true;
            var lit = Shader.Find("Universal Render Pipeline/Lit");

            // 1) AN02 Physical Comparison Clip tightly beside F101 in Room 101 (z = 5.79, right next to F101 at z = 5.50)
            compareClipAn02Root = new GameObject("AN02_ComparisonClipAnchor");
            compareClipAn02Root.transform.SetParent(transform, false);
            compareClipAn02Root.transform.position = new Vector3(5.724f, 1.365f, 5.79f);
            compareClipAn02Root.transform.rotation = Quaternion.LookRotation(Vector3.right);
            AddPart(compareClipAn02Root, "ClipBacking", PrimitiveType.Cube, new Vector3(0, 0, 0.004f), new Vector3(0.236f, 0.324f, 0.010f), boardMat);
            AddPart(compareClipAn02Root, "MetalSpringClip", PrimitiveType.Cube, new Vector3(0, 0.145f, 0.014f), new Vector3(0.088f, 0.032f, 0.014f), ironMat);
            RegisterVerticalSlot(compareClipAn02Root.transform, WorldAnchors.An02CompareClip, new Vector3(0, -0.008f, 0.014f), new Vector2(0.214f, 0.285f), lit);
            compareClipAn02Target = AddSurfaceTarget(compareClipAn02Root.transform, "COMPARE_CLIP_AN02", "Kẹp đối chiếu cạnh F101",
                new Vector3(0, 0, 0.035f), new Vector3(0.30f, 0.38f, 0.14f), frontOnly: true);

            // 2) STAFF Nursing Desk 2-slot physical comparison surface (x = 0.88, y = 0.926, z = 3.55 & 3.85; player stands west at x ~ -0.15 looking +X)
            compareDeskStaffRoot = new GameObject("STAFF_ComparisonDeskAnchor");
            compareDeskStaffRoot.transform.SetParent(transform, false);
            compareDeskStaffRoot.transform.position = new Vector3(0.88f, 0.926f, 3.70f);
            compareDeskStaffRoot.transform.rotation = Quaternion.LookRotation(Vector3.right);
            AddPart(compareDeskStaffRoot, "StaffMatBase", PrimitiveType.Cube, new Vector3(0f, 0.002f, 0f), new Vector3(0.62f, 0.005f, 0.30f), boardMat);
            RegisterFlatSlot(compareDeskStaffRoot.transform, WorldAnchors.StaffDeskLeft, new Vector3(-0.15f, 0.008f, 0f), new Vector2(0.21f, 0.27f), lit);
            RegisterFlatSlot(compareDeskStaffRoot.transform, WorldAnchors.StaffDeskRight, new Vector3(0.15f, 0.008f, 0f), new Vector2(0.21f, 0.27f), lit);
            AddSurfaceTarget(compareDeskStaffRoot.transform, "COMPARE_DESK_STAFF", "Bàn đối chiếu điều dưỡng (2 ô)",
                new Vector3(0f, 0.08f, 0f), new Vector3(0.64f, 0.18f, 0.34f), frontOnly: false);

            // 3) Reception Counter 2-slot physical comparison surface (open north ceramic end: x = 1.86, y = 1.131, z = -1.96)
            compareDeskReceptionRoot = new GameObject("RECEPTION_ComparisonDeskAnchor");
            compareDeskReceptionRoot.transform.SetParent(transform, false);
            compareDeskReceptionRoot.transform.position = new Vector3(1.86f, 1.131f, -1.96f);
            compareDeskReceptionRoot.transform.rotation = Quaternion.LookRotation(Vector3.left);
            AddPart(compareDeskReceptionRoot, "ReceptionMatBase", PrimitiveType.Cube, new Vector3(0f, 0.002f, 0f), new Vector3(0.62f, 0.005f, 0.30f), boardMat);
            RegisterFlatSlot(compareDeskReceptionRoot.transform, WorldAnchors.ReceptionDeskLeft, new Vector3(-0.15f, 0.008f, 0f), new Vector2(0.21f, 0.27f), lit);
            RegisterFlatSlot(compareDeskReceptionRoot.transform, WorldAnchors.ReceptionDeskRight, new Vector3(0.15f, 0.008f, 0f), new Vector2(0.21f, 0.27f), lit);
            AddSurfaceTarget(compareDeskReceptionRoot.transform, "COMPARE_DESK_RECEPTION", "Mặt quầy đối chiếu (2 ô)",
                new Vector3(0f, 0.08f, 0f), new Vector3(0.64f, 0.18f, 0.34f), frontOnly: false);

            // 4) Room 101 2-slot physical examination & CH02 drawing assembly tray table (south wall clear space: 7.08, 0, 2.26)
            compareDeskWard101Root = new GameObject("WARD101_ComparisonTableAnchor");
            compareDeskWard101Root.transform.SetParent(transform, false);
            compareDeskWard101Root.transform.position = new Vector3(7.08f, 0f, 2.26f);
            compareDeskWard101Root.transform.rotation = Quaternion.LookRotation(Vector3.back);
            // Vintage steel tray table legs + rimmed top (pure visual, zero solid collider so NavMesh & ActiveWardColliders stay untouched)
            AddPart(compareDeskWard101Root, "TableTop", PrimitiveType.Cube, new Vector3(0f, 0.78f, 0f), new Vector3(0.68f, 0.022f, 0.36f), traySteelMat);
            AddPart(compareDeskWard101Root, "TableMat", PrimitiveType.Cube, new Vector3(0f, 0.793f, 0f), new Vector3(0.62f, 0.006f, 0.31f), boardMat);
            AddPart(compareDeskWard101Root, "LegFL", PrimitiveType.Cylinder, new Vector3(-0.30f, 0.39f, -0.14f), new Vector3(0.022f, 0.39f, 0.022f), ironMat);
            AddPart(compareDeskWard101Root, "LegFR", PrimitiveType.Cylinder, new Vector3(0.30f, 0.39f, -0.14f), new Vector3(0.022f, 0.39f, 0.022f), ironMat);
            AddPart(compareDeskWard101Root, "LegBL", PrimitiveType.Cylinder, new Vector3(-0.30f, 0.39f, 0.14f), new Vector3(0.022f, 0.39f, 0.022f), ironMat);
            AddPart(compareDeskWard101Root, "LegBR", PrimitiveType.Cylinder, new Vector3(0.30f, 0.39f, 0.14f), new Vector3(0.022f, 0.39f, 0.022f), ironMat);
            AddPart(compareDeskWard101Root, "LowerShelf", PrimitiveType.Cube, new Vector3(0f, 0.22f, 0f), new Vector3(0.62f, 0.014f, 0.30f), traySteelMat);
            RegisterFlatSlot(compareDeskWard101Root.transform, WorldAnchors.Ward101TableLeft, new Vector3(-0.155f, 0.800f, 0f), new Vector2(0.22f, 0.27f), lit);
            RegisterFlatSlot(compareDeskWard101Root.transform, WorldAnchors.Ward101TableRight, new Vector3(0.155f, 0.800f, 0f), new Vector2(0.22f, 0.27f), lit);
            AddSurfaceTarget(compareDeskWard101Root.transform, "COMPARE_DESK_101", "Bàn đối chiếu & ghép hồ sơ (2 ô)",
                new Vector3(0f, 0.86f, 0f), new Vector3(0.68f, 0.22f, 0.38f), frontOnly: false);
        }

        private void RegisterVerticalSlot(Transform parent, string anchorId, Vector3 localPos, Vector2 size, Shader lit)
        {
            var sheet = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sheet.name = "SlotSheet_" + anchorId;
            var col = sheet.GetComponent<Collider>();
            if (col != null) Destroy(col);
            sheet.transform.SetParent(parent, false);
            sheet.transform.localPosition = localPos;
            sheet.transform.localRotation = Quaternion.Euler(0, 180f, 0);
            sheet.transform.localScale = new Vector3(size.x, size.y, 1f);
            var mat = MakeMat(lit, "CTCC_AnchorSheet_" + anchorId, new Color(0.86f, 0.85f, 0.82f, 1f), 0f, 0.06f);
            var mr = sheet.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.enabled = false;
            anchorSlots[anchorId] = new AnchorSlotVisual { AnchorId = anchorId, SheetRenderer = mr, SheetMaterial = mat };
        }

        private void RegisterFlatSlot(Transform parent, string anchorId, Vector3 localPos, Vector2 size, Shader lit)
        {
            var sheet = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sheet.name = "SlotSheet_" + anchorId;
            var col = sheet.GetComponent<Collider>();
            if (col != null) Destroy(col);
            sheet.transform.SetParent(parent, false);
            sheet.transform.localPosition = localPos;
            sheet.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            sheet.transform.localScale = new Vector3(size.x, size.y, 1f);
            var mat = MakeMat(lit, "CTCC_AnchorSheet_" + anchorId, new Color(0.87f, 0.86f, 0.83f, 1f), 0f, 0.06f);
            var mr = sheet.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.enabled = false;

            var propVisual = Instantiate(wristbandTemplate, parent, false);
            propVisual.name = "SlotProp_" + anchorId;
            propVisual.transform.localPosition = localPos + Vector3.up * 0.018f;
            propVisual.transform.localScale = Vector3.one * 1.1f;
            propVisual.SetActive(false);

            anchorSlots[anchorId] = new AnchorSlotVisual
            {
                AnchorId = anchorId,
                SheetRenderer = mr,
                SheetMaterial = mat,
                PropFallbackVisual = propVisual
            };
        }

        private static SceneTarget AddSurfaceTarget(Transform parent, string id, string title, Vector3 localPos, Vector3 size, bool frontOnly)
        {
            var hit = new GameObject(id) { layer = HospitalScene.InteractLayer };
            hit.transform.SetParent(parent, false);
            hit.transform.localPosition = localPos;
            var box = hit.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            var st = hit.AddComponent<SceneTarget>();
            st.id = id;
            st.title = title;
            st.focusPriority = 0;
            st.handhold = hit.transform;
            st.frontOnly = frontOnly;
            return st;
        }

        private void SyncComparisonSurfaces(Investigation state, HospitalScene scene)
        {
            if (state == null || scene == null) return;
            bool hasLoc02 = scene.HasZone("LOC02") || !scene.IsAdditiveMode;
            bool hasLoc03 = scene.HasZone("LOC03") || !scene.IsAdditiveMode;
            bool ch01Or02 = state.chapter is ("CH01" or "CH02");
            bool inLayerB = state.layer == Reality.B;

            if (compareClipAn02Root != null)
                compareClipAn02Root.SetActive(hasLoc03 && ch01Or02 && inLayerB);
            if (compareDeskStaffRoot != null)
                compareDeskStaffRoot.SetActive(hasLoc03 && ch01Or02 && inLayerB);
            if (compareDeskReceptionRoot != null)
                compareDeskReceptionRoot.SetActive(hasLoc02 && ch01Or02 && state.Happened("CH01-T02"));
            if (compareDeskWard101Root != null)
                compareDeskWard101Root.SetActive(hasLoc03 && ch01Or02);

            foreach (var kv in anchorSlots)
            {
                var slot = kv.Value;
                bool allowed = WorldAnchors.AllowedInLayer(slot.AnchorId, state.layer);
                var placed = allowed ? InventoryService.GetPlacedAt(state, slot.AnchorId) : null;
                string nextId = placed?.itemDefId ?? "";
                if (nextId == slot.CurrentItemId) continue;

                slot.CurrentItemId = nextId;
                if (string.IsNullOrEmpty(nextId))
                {
                    if (slot.SheetRenderer != null) slot.SheetRenderer.enabled = false;
                    if (slot.PropFallbackVisual != null) slot.PropFallbackVisual.SetActive(false);
                    if (slot.AnchorId == WorldAnchors.An02CompareClip && compareClipAn02Target != null)
                        compareClipAn02Target.title = "Kẹp đối chiếu cạnh F101";
                }
                else
                {
                    var def = BackpackCatalog.Get(nextId);
                    bool is3DProp = def != null && string.IsNullOrEmpty(def.FrontTexture) && slot.PropFallbackVisual != null;
                    if (is3DProp)
                    {
                        if (slot.SheetRenderer != null) slot.SheetRenderer.enabled = false;
                        slot.PropFallbackVisual.SetActive(true);
                    }
                    else
                    {
                        if (slot.PropFallbackVisual != null) slot.PropFallbackVisual.SetActive(false);
                        var tex = def != null ? textureLookup?.Invoke(def.FrontTexture) : null;
                        if (slot.SheetMaterial != null)
                            slot.SheetMaterial.SetTexture("_BaseMap", tex != null ? tex : Texture2D.whiteTexture);
                        if (slot.SheetRenderer != null) slot.SheetRenderer.enabled = true;
                    }
                    if (slot.AnchorId == WorldAnchors.An02CompareClip && compareClipAn02Target != null && def != null)
                        compareClipAn02Target.title = $"Tài liệu đang kẹp · {def.ShortLabel}";
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

        private static Material MakeMat(Shader shader, string name, Color color, float metallic, float smoothness)
        {
            var m = new Material(shader) { name = name };
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
            return m;
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
                float moveSpeed = 0f;
                if (eye != null)
                {
                    Vector3 cur = eye.transform.position;
                    if (hasEyeWorldPos && dt > 0.0001f)
                    {
                        Vector3 d = cur - lastEyeWorldPos;
                        d.y = 0f;
                        moveSpeed = Mathf.Clamp(d.magnitude / dt, 0f, 4.5f);
                    }
                    lastEyeWorldPos = cur;
                    hasEyeWorldPos = true;
                }
                float moveBlend = Mathf.Clamp01(moveSpeed / 2.6f);
                walkBobPhase += dt * Mathf.Lerp(1.6f, 8.2f, moveBlend);
                swayX = Mathf.Sin(swayPhase) * 0.0032f + Mathf.Cos(walkBobPhase * 0.5f) * 0.0055f * moveBlend;
                swayY = Mathf.Cos(swayPhase * 1.7f) * 0.0024f + Mathf.Abs(Mathf.Sin(walkBobPhase * 0.5f)) * -0.0065f * moveBlend;
            }
            else
            {
                hasEyeWorldPos = false;
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
            if (cassetteTapeTemplate != null) Destroy(cassetteTapeTemplate);
            if (wristbandTemplate != null) Destroy(wristbandTemplate);
            if (handRoot != null) Destroy(handRoot.gameObject);
            if (compareClipAn02Root != null) Destroy(compareClipAn02Root);
            if (compareDeskStaffRoot != null) Destroy(compareDeskStaffRoot);
            if (compareDeskReceptionRoot != null) Destroy(compareDeskReceptionRoot);
            if (compareDeskWard101Root != null) Destroy(compareDeskWard101Root);
            foreach (var kv in anchorSlots)
                if (kv.Value.SheetMaterial != null) Destroy(kv.Value.SheetMaterial);
            DestroyMat(brassMat);
            DestroyMat(ironMat);
            DestroyMat(tagPaperMat);
            DestroyMat(heldPaperMat);
            DestroyMat(boardMat);
            DestroyMat(traySteelMat);
            DestroyMat(tapeMat);
            DestroyMat(wristMat);
        }

        private static void DestroyMat(Material m) { if (m != null) Destroy(m); }
    }
}
