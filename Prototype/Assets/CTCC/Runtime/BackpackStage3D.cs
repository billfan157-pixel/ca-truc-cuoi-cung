using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CTCC
{
    /// <summary>
    /// Diegetic 3D open weathered-canvas investigation backpack rendered on the left side of the
    /// player's FPS view whenever Tab/J opens the Backpack (Proposal V1.1 §3.1 / BP-02).
    /// Displays Minh's actual owned physical tools, stowed papers/evidence, pinned Pocket Note,
    /// and conditional translucent P_BAG08_ECHO shimmer inside the empty sleeve without ever
    /// teleporting the camera or allocating per-open meshes/materials.
    /// </summary>
    public sealed class BackpackStage3D : MonoBehaviour
    {
        private Camera eye;
        private Func<string, Texture> textureLookup;
        private Transform stageRoot;
        private Transform bagPivot;
        private Light keyLight;
        private Light rimLight;

        // Shared materials (created once, destroyed cleanly on OnDestroy)
        private Material canvasOuterMat;
        private Material canvasInnerMat;
        private Material leatherMat;
        private Material brassMat;
        private Material ironMat;
        private Material torchBodyMat;
        private Material notePaperMat;
        private Material envelopeMat;
        private Material tapeBodyMat;
        private Material wristbandMat;
        private Material echoGhostMat;
        private readonly Material[] docSlotMats = new Material[3];

        // Dynamic item visuals inside the open canvas satchel
        private Transform flashlightVisual;
        private Transform phoneVisual;
        private Transform gateKeyVisual;
        private Transform wingCKeyVisual;
        private Transform envelopeVisual;
        private Transform wristbandVisual;
        private Transform tapeVisual;
        private Transform pocketNoteVisual;
        private Transform echoGhostVisual;
        private readonly Transform[] docSlotVisuals = new Transform[3];
        private readonly string[] docSlotItemIds = new string[3];

        private readonly Dictionary<string, (Transform tf, Vector3 basePos)> selectableVisuals = new(StringComparer.Ordinal);
        private string selectedItemDefId = "";
        private BackpackCompartment activeCompartment = BackpackCompartment.Tools;
        private bool open;
        private bool reducedMotion;
        private float openBlend;
        private float echoTimer;
        private Quaternion targetBagRot = Quaternion.Euler(20f, 16f, -3f);

        public bool IsOpen => open;

        public void Initialize(Camera camera, Func<string, Texture> resolveTexture)
        {
            eye = camera;
            textureLookup = resolveTexture;

            stageRoot = new GameObject("CTCC_BackpackStage3D").transform;
            stageRoot.SetParent(eye.transform, false);
            // Kept strictly inside player's 0.38m capsule radius (z = 0.34) so it NEVER clips into walls/clipboards,
            // while centered cleanly in the open left 40% viewport beside the right-docked Backpack UI.
            stageRoot.localPosition = new Vector3(-0.145f, -0.012f, 0.34f);

            bagPivot = new GameObject("CanvasSatchelPivot").transform;
            bagPivot.SetParent(stageRoot, false);
            bagPivot.localScale = Vector3.one * 0.56f;
            bagPivot.localRotation = targetBagRot;

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            canvasOuterMat = MakeMat(lit, "CTCC_BagCanvasOuter", new Color(0.165f, 0.205f, 0.160f, 1f), 0f, 0.10f);
            canvasInnerMat = MakeMat(lit, "CTCC_BagCanvasInner", new Color(0.105f, 0.125f, 0.102f, 1f), 0f, 0.06f);
            leatherMat = MakeMat(lit, "CTCC_BagLeather", new Color(0.24f, 0.15f, 0.095f, 1f), 0f, 0.24f);
            brassMat = MakeMat(lit, "CTCC_BagBrass", new Color(0.68f, 0.53f, 0.26f, 1f), 0.78f, 0.45f);
            ironMat = MakeMat(lit, "CTCC_BagIron", new Color(0.38f, 0.40f, 0.43f, 1f), 0.80f, 0.34f);
            torchBodyMat = MakeMat(lit, "CTCC_BagTorch", new Color(0.14f, 0.15f, 0.17f, 1f), 0.55f, 0.36f);
            notePaperMat = MakeMat(lit, "CTCC_BagPocketNote", new Color(0.89f, 0.85f, 0.72f, 1f), 0f, 0.08f);
            envelopeMat = MakeMat(lit, "CTCC_BagEnvelope", new Color(0.76f, 0.66f, 0.49f, 1f), 0f, 0.08f);
            tapeBodyMat = MakeMat(lit, "CTCC_BagTape", new Color(0.16f, 0.16f, 0.18f, 1f), 0.12f, 0.28f);
            wristbandMat = MakeMat(lit, "CTCC_BagWristband", new Color(0.86f, 0.88f, 0.86f, 1f), 0f, 0.30f);

            for (int i = 0; i < 3; i++)
            {
                docSlotMats[i] = MakeMat(lit, $"CTCC_BagDocSlot_{i}", new Color(0.88f, 0.87f, 0.82f, 1f), 0f, 0.06f);
                docSlotMats[i].SetFloat("_Cull", 0f);
            }

            echoGhostMat = new Material(lit) { name = "CTCC_BagEchoGhost" };
            echoGhostMat.SetFloat("_Surface", 1f);
            echoGhostMat.SetFloat("_Blend", 0f);
            echoGhostMat.SetFloat("_ZWrite", 0f);
            echoGhostMat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            echoGhostMat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            echoGhostMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            echoGhostMat.renderQueue = 3000;
            echoGhostMat.SetColor("_BaseColor", new Color(0.84f, 0.90f, 0.86f, 0f));

            BuildSatchelGeometry();
            BuildStageLights();
            stageRoot.gameObject.SetActive(false);
        }

        public void SetReducedMotion(bool reduced) => reducedMotion = reduced;

        public void Open(Investigation state, BackpackCompartment compartment, string selectedId, bool showEcho)
        {
            if (stageRoot == null) return;
            bool wasOpen = open;
            open = true;
            activeCompartment = compartment;
            selectedItemDefId = selectedId ?? "";

            SyncContents(state, showEcho);
            targetBagRot = compartment switch
            {
                BackpackCompartment.Tools => Quaternion.Euler(22f, 22f, -3f),
                BackpackCompartment.Papers => Quaternion.Euler(20f, 12f, -2f),
                _ => Quaternion.Euler(14f, 16f, -1f)
            };

            stageRoot.gameObject.SetActive(true);
            if (keyLight != null) keyLight.enabled = true;
            if (rimLight != null) rimLight.enabled = true;
            if (!wasOpen)
            {
                openBlend = reducedMotion ? 1f : 0f;
                ApplyStageTransform();
            }
            else if (reducedMotion)
            {
                bagPivot.localRotation = targetBagRot;
            }
        }

        public void Close()
        {
            open = false;
            echoTimer = 0f;
            if (stageRoot != null) stageRoot.gameObject.SetActive(false);
            if (keyLight != null) keyLight.enabled = false;
            if (rimLight != null) rimLight.enabled = false;
        }

        private void SyncContents(Investigation state, bool showEcho)
        {
            if (state == null) return;
            var envTex = textureLookup?.Invoke("Doc_BayEnvelopeFront");
            if (envTex != null && envelopeMat != null) envelopeMat.SetTexture("_BaseMap", envTex);

            SetActive(flashlightVisual, InventoryService.HasPhysical(state, BackpackCatalog.Flashlight));
            SetActive(phoneVisual, InventoryService.HasPhysical(state, BackpackCatalog.MinhPhone));
            SetActive(gateKeyVisual, InventoryService.HasPhysical(state, BackpackCatalog.GateKey));
            SetActive(wingCKeyVisual, InventoryService.HasPhysical(state, BackpackCatalog.WingCKey));
            SetActive(envelopeVisual, InventoryService.HasPhysical(state, BackpackCatalog.E10aEnvelope));
            SetActive(wristbandVisual, InventoryService.HasPhysical(state, BackpackCatalog.Ch02Wristband));
            SetActive(tapeVisual, InventoryService.HasPhysical(state, BackpackCatalog.Ch04Tape));

            // Fill up to 3 physical paper slots inside the center document sleeve
            int used = 0;
            for (int i = 0; i < 3; i++) { docSlotItemIds[i] = ""; SetActive(docSlotVisuals[i], false); }
            for (int i = 0; i < state.backpackItems.Count && used < 3; i++)
            {
                var e = state.backpackItems[i];
                if (e == null || e.status == BackpackItemStatus.MemoryRef || e.status == BackpackItemStatus.Placed)
                    continue;
                if (e.itemDefId == BackpackCatalog.E10aEnvelope) continue;
                var def = BackpackCatalog.Get(e.itemDefId);
                if (def == null || def.Compartment != BackpackCompartment.Papers) continue;
                var tex = !string.IsNullOrEmpty(def.FrontTexture) ? textureLookup?.Invoke(def.FrontTexture) : null;
                docSlotMats[used].SetTexture("_BaseMap", tex != null ? tex : Texture2D.whiteTexture);
                docSlotItemIds[used] = def.Id;
                SetActive(docSlotVisuals[used], true);
                used++;
            }

            if (showEcho && echoGhostVisual != null)
            {
                var cardTex = textureLookup?.Invoke("ObsCard");
                if (cardTex != null) echoGhostMat.SetTexture("_BaseMap", cardTex);
                echoGhostVisual.gameObject.SetActive(true);
                echoTimer = 4.6f;
            }
            else if (echoTimer <= 0f && echoGhostVisual != null)
            {
                echoGhostVisual.gameObject.SetActive(false);
            }
        }

        private void BuildStageLights()
        {
            var keyGo = new GameObject("BackpackKeyLight");
            keyGo.transform.SetParent(stageRoot, false);
            keyGo.transform.localPosition = new Vector3(0.14f, 0.16f, -0.18f);
            keyGo.transform.LookAt(stageRoot);
            keyLight = keyGo.AddComponent<Light>();
            keyLight.type = LightType.Point;
            keyLight.range = 0.95f;
            keyLight.intensity = 1.15f;
            keyLight.color = new Color(1.0f, 0.94f, 0.82f);
            keyLight.shadows = LightShadows.None;
            keyLight.enabled = false;

            var rimGo = new GameObject("BackpackRimLight");
            rimGo.transform.SetParent(stageRoot, false);
            rimGo.transform.localPosition = new Vector3(-0.16f, 0.12f, 0.10f);
            rimLight = rimGo.AddComponent<Light>();
            rimLight.type = LightType.Point;
            rimLight.range = 0.85f;
            rimLight.intensity = 0.45f;
            rimLight.color = new Color(0.70f, 0.84f, 0.88f);
            rimLight.shadows = LightShadows.None;
            rimLight.enabled = false;
        }

        private void BuildSatchelGeometry()
        {
            // Main weathered canvas body shell (hollow pocket aesthetic)
            Part(bagPivot, "BagBackWall", PrimitiveType.Cube, new Vector3(0f, -0.02f, 0.042f), new Vector3(0.31f, 0.23f, 0.016f), canvasOuterMat);
            Part(bagPivot, "BagBase", PrimitiveType.Cube, new Vector3(0f, -0.132f, 0f), new Vector3(0.31f, 0.018f, 0.095f), canvasOuterMat);
            Part(bagPivot, "BagSideL", PrimitiveType.Cube, new Vector3(-0.148f, -0.02f, 0f), new Vector3(0.016f, 0.22f, 0.092f), canvasOuterMat);
            Part(bagPivot, "BagSideR", PrimitiveType.Cube, new Vector3(0.148f, -0.02f, 0f), new Vector3(0.016f, 0.22f, 0.092f), canvasOuterMat);
            Part(bagPivot, "BagFrontPocket", PrimitiveType.Cube, new Vector3(0f, -0.062f, -0.044f), new Vector3(0.305f, 0.145f, 0.014f), canvasOuterMat);
            Part(bagPivot, "BagInnerLining", PrimitiveType.Cube, new Vector3(0f, -0.03f, 0.030f), new Vector3(0.284f, 0.205f, 0.008f), canvasInnerMat);
            Part(bagPivot, "BagDivider", PrimitiveType.Cube, new Vector3(-0.042f, -0.045f, -0.004f), new Vector3(0.008f, 0.17f, 0.072f), canvasInnerMat);

            // Folded-open top canvas flap (holds Pocket Note card!)
            var flap = new GameObject("BagOpenFlap").transform;
            flap.SetParent(bagPivot, false);
            flap.localPosition = new Vector3(0f, 0.095f, 0.044f);
            flap.localRotation = Quaternion.Euler(-24f, 0f, 0f);
            Part(flap, "FlapCloth", PrimitiveType.Cube, new Vector3(0f, 0.068f, 0f), new Vector3(0.305f, 0.145f, 0.012f), canvasOuterMat);
            Part(flap, "FlapTrimL", PrimitiveType.Cube, new Vector3(-0.095f, 0.068f, -0.008f), new Vector3(0.022f, 0.142f, 0.006f), leatherMat);
            Part(flap, "FlapTrimR", PrimitiveType.Cube, new Vector3(0.095f, 0.068f, -0.008f), new Vector3(0.022f, 0.142f, 0.006f), leatherMat);

            // Front leather straps & brass buckles + zipper rail
            Part(bagPivot, "StrapL", PrimitiveType.Cube, new Vector3(-0.095f, -0.072f, -0.053f), new Vector3(0.024f, 0.125f, 0.006f), leatherMat);
            Part(bagPivot, "StrapR", PrimitiveType.Cube, new Vector3(0.095f, -0.072f, -0.053f), new Vector3(0.024f, 0.125f, 0.006f), leatherMat);
            Part(bagPivot, "BuckleL", PrimitiveType.Cube, new Vector3(-0.095f, -0.042f, -0.058f), new Vector3(0.032f, 0.022f, 0.008f), brassMat);
            Part(bagPivot, "BuckleR", PrimitiveType.Cube, new Vector3(0.095f, -0.042f, -0.058f), new Vector3(0.032f, 0.022f, 0.008f), brassMat);
            Part(bagPivot, "ZipperTrack", PrimitiveType.Cube, new Vector3(0f, 0.012f, -0.046f), new Vector3(0.28f, 0.008f, 0.006f), brassMat);

            // Compartment 3: Pinned Pocket Note + Pencil on inner flap
            pocketNoteVisual = new GameObject("PocketNoteCard").transform;
            pocketNoteVisual.SetParent(flap, false);
            pocketNoteVisual.localPosition = new Vector3(0.01f, 0.066f, -0.011f);
            pocketNoteVisual.localRotation = Quaternion.Euler(0f, 0f, -3f);
            Part(pocketNoteVisual, "NoteSheet", PrimitiveType.Cube, Vector3.zero, new Vector3(0.118f, 0.088f, 0.003f), notePaperMat);
            Part(pocketNoteVisual, "NotePin", PrimitiveType.Cube, new Vector3(0f, 0.040f, -0.003f), new Vector3(0.036f, 0.008f, 0.005f), brassMat);
            var pencil = Part(pocketNoteVisual, "Pencil", PrimitiveType.Cylinder, new Vector3(0.068f, -0.004f, -0.004f), new Vector3(0.007f, 0.044f, 0.007f), leatherMat);
            pencil.transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
            selectableVisuals["POCKET_MAIN"] = (pocketNoteVisual, pocketNoteVisual.localPosition);

            // Compartment 1 (Left Tool Sleeve): Flashlight, Brass Gate Key, Iron Wing-C Key, Wristband, Cassette
            flashlightVisual = new GameObject("BagFlashlight").transform;
            flashlightVisual.SetParent(bagPivot, false);
            flashlightVisual.localPosition = new Vector3(-0.105f, 0.015f, -0.008f);
            flashlightVisual.localRotation = Quaternion.Euler(8f, 0f, 11f);
            Part(flashlightVisual, "Body", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.026f, 0.062f, 0.026f), torchBodyMat);
            Part(flashlightVisual, "Bezel", PrimitiveType.Cylinder, new Vector3(0f, 0.064f, 0f), new Vector3(0.034f, 0.012f, 0.034f), brassMat);
            selectableVisuals[BackpackCatalog.Flashlight] = (flashlightVisual, flashlightVisual.localPosition);

            phoneVisual = new GameObject("BagMinhPhone").transform;
            phoneVisual.SetParent(bagPivot, false);
            phoneVisual.localPosition = new Vector3(-0.088f, 0.036f, -0.028f);
            phoneVisual.localRotation = Quaternion.Euler(10f, 14f, -6f);
            Part(phoneVisual, "PhoneBody", PrimitiveType.Cube, Vector3.zero, new Vector3(0.034f, 0.068f, 0.010f), torchBodyMat);
            Part(phoneVisual, "PhoneScreen", PrimitiveType.Cube, new Vector3(0f, 0.012f, -0.0055f), new Vector3(0.026f, 0.030f, 0.002f), wristbandMat);
            selectableVisuals[BackpackCatalog.MinhPhone] = (phoneVisual, phoneVisual.localPosition);

            gateKeyVisual = BuildMiniKey("BagGateKey", brassMat, hasTag: false);
            gateKeyVisual.SetParent(bagPivot, false);
            gateKeyVisual.localPosition = new Vector3(-0.070f, 0.028f, -0.016f);
            gateKeyVisual.localRotation = Quaternion.Euler(14f, 22f, -12f);
            selectableVisuals[BackpackCatalog.GateKey] = (gateKeyVisual, gateKeyVisual.localPosition);

            wingCKeyVisual = BuildMiniKey("BagWingCKey", ironMat, hasTag: true);
            wingCKeyVisual.SetParent(bagPivot, false);
            wingCKeyVisual.localPosition = new Vector3(-0.062f, 0.022f, 0.008f);
            wingCKeyVisual.localRotation = Quaternion.Euler(10f, -18f, -18f);
            selectableVisuals[BackpackCatalog.WingCKey] = (wingCKeyVisual, wingCKeyVisual.localPosition);

            wristbandVisual = new GameObject("BagWristband").transform;
            wristbandVisual.SetParent(bagPivot, false);
            wristbandVisual.localPosition = new Vector3(-0.102f, 0.044f, 0.015f);
            wristbandVisual.localRotation = Quaternion.Euler(20f, 15f, -14f);
            Part(wristbandVisual, "Loop", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.042f, 0.009f, 0.032f), wristbandMat);
            selectableVisuals[BackpackCatalog.Ch02Wristband] = (wristbandVisual, wristbandVisual.localPosition);

            tapeVisual = new GameObject("BagCassetteTape").transform;
            tapeVisual.SetParent(bagPivot, false);
            tapeVisual.localPosition = new Vector3(-0.088f, 0.018f, -0.025f);
            tapeVisual.localRotation = Quaternion.Euler(6f, 12f, 5f);
            Part(tapeVisual, "Shell", PrimitiveType.Cube, Vector3.zero, new Vector3(0.064f, 0.042f, 0.010f), tapeBodyMat);
            Part(tapeVisual, "Label", PrimitiveType.Cube, new Vector3(0f, 0.004f, -0.006f), new Vector3(0.048f, 0.020f, 0.002f), notePaperMat);
            selectableVisuals[BackpackCatalog.Ch04Tape] = (tapeVisual, tapeVisual.localPosition);

            // Compartment 2 (Right Document Sleeve): E10a Envelope + up to 3 physical paper sheets + B08 Echo ghost
            envelopeVisual = new GameObject("BagE10aEnvelope").transform;
            envelopeVisual.SetParent(bagPivot, false);
            envelopeVisual.localPosition = new Vector3(0.046f, 0.022f, -0.018f);
            envelopeVisual.localRotation = Quaternion.Euler(6f, -4f, -4f);
            var envQuad = Part(envelopeVisual, "EnvelopeFront", PrimitiveType.Quad, new Vector3(0f, 0f, -0.005f), new Vector3(0.135f, 0.095f, 1f), envelopeMat);
            envQuad.transform.localRotation = Quaternion.identity;
            Part(envelopeVisual, "EnvelopeThick", PrimitiveType.Cube, Vector3.zero, new Vector3(0.136f, 0.096f, 0.007f), envelopeMat);
            selectableVisuals[BackpackCatalog.E10aEnvelope] = (envelopeVisual, envelopeVisual.localPosition);

            Vector3[] docOffsets =
            {
                new(0.032f, 0.042f, 0.002f),
                new(0.068f, 0.048f, 0.012f),
                new(0.048f, 0.052f, 0.020f)
            };
            float[] docTilts = { 3f, -5f, 1f };
            for (int i = 0; i < 3; i++)
            {
                var slot = new GameObject($"BagDocSlot_{i}").transform;
                slot.SetParent(bagPivot, false);
                slot.localPosition = docOffsets[i];
                slot.localRotation = Quaternion.Euler(4f, -2f, docTilts[i]);
                Part(slot, "Sheet", PrimitiveType.Quad, Vector3.zero, new Vector3(0.105f, 0.135f, 1f), docSlotMats[i]);
                slot.gameObject.SetActive(false);
                docSlotVisuals[i] = slot;
            }

            echoGhostVisual = new GameObject("BagEchoGhost_P_BAG08").transform;
            echoGhostVisual.SetParent(bagPivot, false);
            echoGhostVisual.localPosition = new Vector3(0.052f, 0.046f, -0.006f);
            echoGhostVisual.localRotation = Quaternion.Euler(5f, -3f, -4f);
            Part(echoGhostVisual, "EchoQuad", PrimitiveType.Quad, Vector3.zero, new Vector3(0.108f, 0.140f, 1f), echoGhostMat);
            echoGhostVisual.gameObject.SetActive(false);
        }

        private Transform BuildMiniKey(string name, Material metal, bool hasTag)
        {
            var root = new GameObject(name).transform;
            Part(root, "Bow", PrimitiveType.Cylinder, new Vector3(0f, 0.032f, 0f), new Vector3(0.022f, 0.004f, 0.022f), metal).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part(root, "Shaft", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0f), new Vector3(0.0055f, 0.026f, 0.0055f), metal);
            Part(root, "Bit", PrimitiveType.Cube, new Vector3(0.007f, -0.018f, 0f), new Vector3(0.012f, 0.014f, 0.004f), metal);
            if (hasTag)
                Part(root, "Tag", PrimitiveType.Cube, new Vector3(-0.018f, 0.044f, 0f), new Vector3(0.020f, 0.014f, 0.002f), notePaperMat);
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

        private static Material MakeMat(Shader shader, string name, Color color, float metallic, float smoothness)
        {
            var m = new Material(shader) { name = name };
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
            return m;
        }

        private static void SetActive(Transform tf, bool active)
        {
            if (tf != null && tf.gameObject.activeSelf != active)
                tf.gameObject.SetActive(active);
        }

        private void ApplyStageTransform()
        {
            float ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(openBlend), 3f);
            stageRoot.localPosition = Vector3.Lerp(new Vector3(-0.15f, -0.19f, 0.30f), new Vector3(-0.145f, -0.012f, 0.34f), ease);
        }

        private void Update()
        {
            if (!open || stageRoot == null) return;
            float dt = Time.unscaledDeltaTime;
            openBlend = Mathf.MoveTowards(openBlend, 1f, dt * (reducedMotion ? 16f : 5.2f));
            ApplyStageTransform();

            if (!reducedMotion)
            {
                float breath = Mathf.Sin(Time.unscaledTime * 1.4f) * 1.1f;
                Quaternion idleTarget = targetBagRot * Quaternion.Euler(breath, breath * 0.6f, 0f);
                bagPivot.localRotation = Quaternion.Slerp(bagPivot.localRotation, idleTarget, 1f - Mathf.Exp(-7f * dt));
            }
            else
            {
                bagPivot.localRotation = targetBagRot;
            }

            // Gently raise the currently selected item out of its canvas pocket
            foreach (var kv in selectableVisuals)
            {
                var (tf, basePos) = kv.Value;
                if (tf == null || !tf.gameObject.activeSelf) continue;
                bool isSel = string.Equals(kv.Key, selectedItemDefId, StringComparison.Ordinal) ||
                             (kv.Key == "POCKET_MAIN" && activeCompartment == BackpackCompartment.PocketNote);
                Vector3 goal = isSel ? basePos + new Vector3(0f, 0.024f, -0.012f) : basePos;
                tf.localPosition = reducedMotion ? goal : Vector3.Lerp(tf.localPosition, goal, 1f - Mathf.Exp(-10f * dt));
            }

            for (int i = 0; i < 3; i++)
            {
                var slot = docSlotVisuals[i];
                if (slot == null || !slot.gameObject.activeSelf) continue;
                bool isSel = !string.IsNullOrEmpty(docSlotItemIds[i]) &&
                             string.Equals(docSlotItemIds[i], selectedItemDefId, StringComparison.Ordinal);
                float baseY = i == 0 ? 0.042f : i == 1 ? 0.048f : 0.052f;
                Vector3 goal = new(slot.localPosition.x, isSel ? baseY + 0.028f : baseY, slot.localPosition.z);
                slot.localPosition = reducedMotion ? goal : Vector3.Lerp(slot.localPosition, goal, 1f - Mathf.Exp(-10f * dt));
            }

            if (echoTimer > 0f && echoGhostVisual != null && echoGhostMat != null)
            {
                echoTimer -= dt;
                float alpha = echoTimer > 1.6f
                    ? 0.36f + 0.08f * Mathf.Sin(Time.unscaledTime * 2.5f)
                    : Mathf.Lerp(0f, 0.36f, Mathf.Clamp01(echoTimer / 1.6f));
                echoGhostMat.SetColor("_BaseColor", new Color(0.84f, 0.90f, 0.86f, alpha));
                if (echoTimer <= 0f) echoGhostVisual.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (stageRoot != null) Destroy(stageRoot.gameObject);
            DestroyMat(canvasOuterMat);
            DestroyMat(canvasInnerMat);
            DestroyMat(leatherMat);
            DestroyMat(brassMat);
            DestroyMat(ironMat);
            DestroyMat(torchBodyMat);
            DestroyMat(notePaperMat);
            DestroyMat(envelopeMat);
            DestroyMat(tapeBodyMat);
            DestroyMat(wristbandMat);
            DestroyMat(echoGhostMat);
            for (int i = 0; i < docSlotMats.Length; i++) DestroyMat(docSlotMats[i]);
        }

        private static void DestroyMat(Material m)
        {
            if (m != null) Destroy(m);
        }
    }
}
