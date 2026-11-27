using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CTCC.Editor
{
    public sealed partial class HospitalAuthoring
    {
        private const string Maps = Root + "/Art/Textures/";
        private const string Models = Root + "/Art/Meshes/";
        private readonly Dictionary<string, Material> decalMaterials = new();

        private void PrepareMaterials()
        {
            if (art)
            {
                ConfigureAssetImporters();
            }

            // Physically calibrated PBR materials (ZERO smoothness override bug: smoothScale=1.0 preserves authored _Mask alpha!)
            // Authentic Vietnamese smooth-troweled limewash & oil-painted wainscot bump calibration
            Lit("PlasterR", 1f, 1f, 1f, maps: "PlasterR", bump: 0.25f, ao: 0.78f);
            Lit("PlasterB", 1f, 1f, 1f, maps: "PlasterB", bump: 0.14f, ao: 0.65f);
            Lit("DadoR", 1f, 1f, 1f, maps: "DadoR", bump: 0.26f, ao: 0.76f);
            Lit("DadoB", 1f, 1f, 1f, maps: "DadoB", bump: 0.14f, ao: 0.65f);
            Lit("FloorR", 1f, 1f, 1f, maps: "FloorR", bump: 0.65f, ao: 0.85f);
            Lit("FloorB", 1f, 1f, 1f, maps: "FloorB", bump: 0.50f, ao: 0.75f);
            // Authentic Vietnamese cast terrazzo / washed concrete trim (never dark gạch bông tile pattern!)
            Lit("BaseTrim", 0.74f, 0.76f, 0.72f, maps: "TerrazzoBench", tint: true, bump: 0.36f, ao: 0.72f);

            Lit("WoodDoorB", 1f, 1f, 1f, maps: "WoodDoorB", bump: 0.35f, ao: 0.75f);
            Lit("WoodDoorR", 1f, 1f, 1f, maps: "WoodDoorR", bump: 0.52f, ao: 0.80f);
            Lit("DoorWood", 1f, 1f, 1f, maps: "WoodDoorR", bump: 0.50f, ao: 0.78f);
            Lit("Wood", 0.92f, 0.86f, 0.78f, maps: "WoodDoorR", tint: true, bump: 0.45f, ao: 0.78f);

            Lit("MetalPaint", 1f, 1f, 1f, maps: "PaintedMetalB", bump: 0.32f, ao: 0.72f);
            Lit("MetalOld", 1f, 1f, 1f, maps: "PaintedMetalR", bump: 0.55f, ao: 0.82f);
            Lit("CabinetPaint", 1.04f, 1.06f, 1.02f, maps: "PaintedMetalB", tint: true, bump: 0.32f, ao: 0.72f);
            Lit("CabinetOld", 1.02f, 1.04f, 1.00f, maps: "PaintedMetalR", tint: true, bump: 0.55f, ao: 0.82f);
            Lit("Frame", 0.72f, 0.80f, 0.74f, maps: "PaintedMetalR", tint: true, bump: 0.48f, ao: 0.78f);

            Lit("Steel", 1f, 1f, 1f, maps: "Steel", bump: 0.42f, ao: 0.75f);
            Lit("Brass", 0.76f, 0.61f, 0.34f, maps: "Steel", tint: true, bump: 0.35f, ao: 0.72f);
            Lit("Rubber", 0.085f, 0.090f, 0.085f, smooth: 0.28f);

            Lit("Cotton", 1f, 1f, 1f, maps: "Cotton", bump: 0.58f, ao: 0.82f);
            Lit("CottonOld", 1f, 1f, 1f, maps: "CottonOld", bump: 0.68f, ao: 0.88f);
            Lit("Textile", 0.56f, 0.74f, 0.64f, maps: "Cotton", tint: true, bump: 0.62f, ao: 0.84f);
            Lit("TextileOld", 0.48f, 0.59f, 0.53f, maps: "CottonOld", tint: true, bump: 0.72f, ao: 0.88f);
            Lit("Mattress", 1f, 1f, 1f, maps: "MattressB", bump: 0.48f, ao: 0.78f);
            Lit("MattressOld", 1f, 1f, 1f, maps: "MattressR", bump: 0.58f, ao: 0.82f);
            Lit("Curtain", 0.80f, 0.89f, 0.84f, maps: "Cotton", tint: true, twoSided: true, bump: 0.55f, ao: 0.80f);
            Lit("CurtainOld", 0.62f, 0.68f, 0.60f, maps: "CottonOld", tint: true, twoSided: true, bump: 0.65f, ao: 0.85f);

            Lit("Ceramic", 1f, 1f, 1f, maps: "Ceramic", bump: 0.35f, ao: 0.72f);
            Lit("EnamelBlue", 0.14f, 0.27f, 0.42f, maps: "Ceramic", tint: true, bump: 0.28f, ao: 0.70f);
            Lit("Paper", 0.80f, 0.75f, 0.60f, smooth: 0.14f);
            Lit("KraftPaper", 1f, 1f, 1f, maps: "KraftPaper", bump: 0.28f, ao: 0.70f);
            Lit("PlasticIvory", 0.76f, 0.73f, 0.61f, smooth: 0.44f);
            Lit("RedPaint", 0.58f, 0.13f, 0.11f, metal: 0.18f, smooth: 0.52f);
            Lit("GreenOxygen", 0.17f, 0.40f, 0.27f, maps: "PaintedMetalB", tint: true, bump: 0.35f, ao: 0.72f);
            Lit("SlipperYellow", 0.76f, 0.70f, 0.48f, smooth: 0.26f);
            Lit("ReedMat", 0.68f, 0.60f, 0.39f, maps: "CottonOld", tint: true, bump: 0.85f, ao: 0.85f);

            // M2 Exterior, ER & Character PBR Materials
            Lit("WetAsphalt", 1f, 1f, 1f, maps: "WetAsphalt", bump: 0.65f, ao: 0.82f);
            Lit("ExteriorFacade", 1f, 1f, 1f, maps: "ExteriorFacade", bump: 0.50f, ao: 0.82f);
            Lit("ExteriorFacadeB", 1f, 1f, 1f, maps: "ExteriorFacadeB", bump: 0.34f, ao: 0.72f);
            Lit("CampusStuccoR", 1f, 1f, 1f, maps: "CampusStuccoR", bump: 0.46f, ao: 0.80f);
            Lit("CampusStuccoB", 1f, 1f, 1f, maps: "CampusStuccoB", bump: 0.28f, ao: 0.68f);
            Lit("CorrugatedRoofR", 1f, 1f, 1f, maps: "CorrugatedRoofR", bump: 0.72f, ao: 0.86f);
            Lit("CorrugatedRoofB", 1f, 1f, 1f, maps: "CorrugatedRoofB", bump: 0.62f, ao: 0.76f);
            Lit("ConcretePavingR", 1f, 1f, 1f, maps: "ConcretePavingR", bump: 0.56f, ao: 0.84f);
            Lit("ConcretePavingB", 1f, 1f, 1f, maps: "ConcretePavingB", bump: 0.44f, ao: 0.74f);
            Lit("BrickInfill2004", 1f, 1f, 1f, maps: "BrickInfill2004", bump: 0.82f, ao: 0.86f);
            Lit("WeatheredBrick", 1f, 1f, 1f, maps: "WeatheredBrick", bump: 0.78f, ao: 0.85f);
            Lit("TerrazzoBench", 1f, 1f, 1f, maps: "TerrazzoBench", bump: 0.38f, ao: 0.76f);
            Lit("MotorbikePaint", 0.24f, 0.055f, 0.068f, metal: 0.48f, smooth: 0.78f);
            Lit("AmbulanceWhite", 0.84f, 0.85f, 0.82f, metal: 0.14f, smooth: 0.62f);
            Lit("HazardYellow", 0.85f, 0.68f, 0.14f, metal: 0.08f, smooth: 0.48f);

            // Campus Exterior Window Backing (R 2012 dark moonlit reflective glass <-> B 2005 operational warm-green fluorescent glow!)
            Lit("WindowDarkR", 0.055f, 0.085f, 0.105f, metal: 0.18f, smooth: 0.82f);
            Mat("WindowDarkR").SetColor("_EmissionColor", new Color(0.03f, 0.055f, 0.075f) * 0.45f);
            Mat("WindowDarkR").EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(Mat("WindowDarkR"));

            Lit("WindowLitB", 0.86f, 0.94f, 0.84f, metal: 0.05f, smooth: 0.74f);
            Mat("WindowLitB").SetColor("_EmissionColor", new Color(0.76f, 0.90f, 0.74f) * 1.35f);
            Mat("WindowLitB").EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(Mat("WindowLitB"));

            Lit("CampusLawn", 0.095f, 0.145f, 0.090f, smooth: 0.16f);
            Lit("HorizonTree", 0.035f, 0.055f, 0.045f, smooth: 0.10f);

            Lit("CrimsonGlow", 0.88f, 0.14f, 0.12f, smooth: 0.60f);
            Mat("CrimsonGlow").SetColor("_EmissionColor", new Color(0.95f, 0.16f, 0.12f) * 2.6f);
            Mat("CrimsonGlow").EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(Mat("CrimsonGlow"));

            Lit("WarmGlow", 0.98f, 0.80f, 0.48f, smooth: 0.70f);
            Mat("WarmGlow").SetColor("_EmissionColor", new Color(1.0f, 0.78f, 0.42f) * 2.8f);
            Mat("WarmGlow").EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(Mat("WarmGlow"));

            // LOC02 Reception Hero Blender 4.5 Cycles-Baked PBR Materials (Paired B 2005 <-> R 2012)
            HeroAtlasMat("LOC02_CounterTop", hasR: true, bump: 0.78f, ao: 0.92f);
            HeroAtlasMat("LOC02_CounterBody", hasR: true, bump: 0.84f, ao: 0.94f);
            HeroAtlasMat("LOC02_Partition", hasR: true, bump: 0.82f, ao: 0.92f);
            HeroAtlasMat("LOC02_Notices", hasR: true, bump: 0.30f, ao: 0.72f);
            HeroAtlasMat("LOC02_DeskProps", hasR: true, bump: 0.76f, ao: 0.92f);
            HeroAtlasMat("LOC02_DutyChair", hasR: true, bump: 0.80f, ao: 0.90f);
            HeroAtlasMat("LOC02_FileRack", hasR: true, bump: 0.78f, ao: 0.92f);
            HeroAtlasMat("LOC02_Plant", hasR: true, bump: 0.74f, ao: 0.90f);
            HeroAtlasMat("LOC02_WallClock", hasR: true, bump: 0.70f, ao: 0.88f);
            HeroAtlasMat("LOC02_DeskDrawer", hasR: true, bump: 0.78f, ao: 0.90f);
            HeroAtlasMat("LOC02_S1Door", hasR: true, bump: 0.86f, ao: 0.94f);
            HeroAtlasMat("LOC02_WaitingBench", hasR: true, bump: 0.92f, ao: 0.92f);
            HeroAtlasMat("LOC02_RosterBoard", hasR: true, bump: 0.70f, ao: 0.90f);
            HeroAtlasMat("LOC02_SwitchBoard", hasR: true, bump: 0.78f, ao: 0.90f);
            HeroAtlasMat("LOC02_CeilingArch", hasR: true, bump: 0.82f, ao: 0.90f);
            HeroAtlasMat("LOC02_CeilingFan", hasR: true, bump: 0.75f, ao: 0.90f);
            HeroAtlasMat("LOC02_DutyBook", hasR: false, bump: 0.65f, ao: 0.88f);
            HeroAtlasMat("LOC02_TeaSet", hasR: false, bump: 0.62f, ao: 0.88f);
            HeroAtlasMat("LOC02_SpareBattery", hasR: false, bump: 0.65f, ao: 0.88f);

            // LOC03 Ward 101 & Store Room Hero Blender 4.5 Cloth-Sim + Cycles-Baked PBR Materials (Paired B 2005 <-> R 2012)
            HeroAtlasMat("LOC03_WardBed", hasR: true, bump: 0.80f, ao: 0.92f);
            HeroAtlasMat("LOC03_WardBedLived", hasR: true, bump: 0.80f, ao: 0.92f);
            HeroAtlasMat("LOC03_Curtain", hasR: true, bump: 0.72f, ao: 0.88f, twoSided: true);
            HeroAtlasMat("LOC03_Bed4Trace", hasR: false, bump: 0.78f, ao: 0.90f);
            HeroAtlasMat("LOC03_BedsideCab", hasR: true, bump: 0.76f, ao: 0.92f);
            HeroAtlasMat("LOC03_Bed3Drawer", hasR: false, bump: 0.76f, ao: 0.90f);
            HeroAtlasMat("LOC03_MedCabinet", hasR: true, bump: 0.78f, ao: 0.92f);
            HeroAtlasMat("LOC03_WardSink", hasR: true, bump: 0.82f, ao: 0.92f);
            HeroAtlasMat("LOC03_WardStool", hasR: true, bump: 0.74f, ao: 0.90f);
            HeroAtlasMat("LOC03_WardArch", hasR: true, bump: 0.82f, ao: 0.90f);
            HeroAtlasMat("LOC03_Bed4Conduit", hasR: true, bump: 0.76f, ao: 0.88f);
            HeroAtlasMat("LOC03_WindowWide", hasR: true, bump: 0.78f, ao: 0.90f);
            HeroAtlasMat("LOC03_WindowMid", hasR: true, bump: 0.78f, ao: 0.90f);
            HeroAtlasMat("LOC03_WindowNarrow", hasR: true, bump: 0.78f, ao: 0.90f);
            HeroAtlasMat("LOC03_WardDoorFrame", hasR: true, bump: 0.84f, ao: 0.92f);
            HeroAtlasMat("LOC03_WardDoorLeaf", hasR: true, bump: 0.84f, ao: 0.92f);
            HeroAtlasMat("LOC03_TubeFixture", hasR: true, bump: 0.72f, ao: 0.88f);
            HeroAtlasMat("LOC03_WallOutlet", hasR: true, bump: 0.72f, ao: 0.88f);
            HeroAtlasMat("LOC03_IVStand", hasR: true, bump: 0.74f, ao: 0.90f);
            HeroAtlasMat("LOC03_ThermosMug", hasR: true, bump: 0.76f, ao: 0.90f);
            HeroAtlasMat("LOC03_WashbasinMat", hasR: true, bump: 0.82f, ao: 0.90f);
            HeroAtlasMat("LOC03_Slippers", hasR: true, bump: 0.76f, ao: 0.90f);
            HeroAtlasMat("LOC03_CareProps", hasR: false, bump: 0.72f, ao: 0.88f);
            HeroAtlasMat("LOC03_PedalBin", hasR: true, bump: 0.74f, ao: 0.90f);
            HeroAtlasMat("LOC03_Clipboard", hasR: false, bump: 0.74f, ao: 0.88f);
            HeroAtlasMat("LOC03_ChartHolder", hasR: false, bump: 0.74f, ao: 0.88f);
            HeroAtlasMat("LOC03_DryShelf", hasR: true, bump: 0.82f, ao: 0.92f);
            HeroAtlasMat("LOC03_StorePayoff", hasR: true, bump: 0.80f, ao: 0.92f);
            HeroAtlasMat("LOC03_PatientBag", hasR: true, bump: 0.80f, ao: 0.90f);

            Lit("MonitorScreen", 1f, 1f, 1f, smooth: 0.65f);
            if (art)
            {
                Mat("MonitorScreen").SetTexture("_BaseMap", Tex("MonitorECG"));
                Mat("MonitorScreen").SetTexture("_EmissionMap", Tex("MonitorECG"));
                Mat("MonitorScreen").SetColor("_EmissionColor", new Color(0.68f, 1.0f, 0.82f) * 1.35f);
                Mat("MonitorScreen").EnableKeyword("_EMISSION");
                EditorUtility.SetDirty(Mat("MonitorScreen"));
            }

            // M2-C High-Fidelity Digital Human URP 17.3 Forward+ Materials (Lan, Bé An, Gatekeeper)
            var dhSkinShader = Shader.Find("CTCC/DigitalHumanSkin");
            var dhEyeShader = Shader.Find("CTCC/DigitalHumanEye");
            var dhHairShader = Shader.Find("CTCC/DigitalHumanHair");
            var dhFabricShader = Shader.Find("CTCC/DigitalHumanFabric");
            var appHumanShader = Shader.Find("CTCC/ApparitionHuman");

            Material MakeSkinMat(string name, string prefix, float sss, float backScatter, float spec, float detailScale, float bump)
            {
                if (dhSkinShader != null && art)
                {
                    var m = MaterialAsset(name, dhSkinShader);
                    m.SetTexture("_BaseMap", Tex(prefix + "_Color"));
                    m.SetTexture("_BumpMap", Tex(prefix + "_Normal"));
                    m.SetTexture("_DetailNormalMap", Tex("SkinMicroPore_Normal"));
                    m.SetTexture("_MaskMap", Tex(prefix + "_Mask"));
                    m.SetColor("_BaseColor", Color.white);
                    m.SetFloat("_BumpScale", bump);
                    m.SetFloat("_DetailTiling", 14.0f);
                    m.SetFloat("_DetailScale", detailScale);
                    m.SetFloat("_DetailFadeDist", 2.8f);
                    m.SetFloat("_SmoothnessScale", 0.90f);
                    m.SetFloat("_OcclusionStrength", 0.72f);
                    m.SetColor("_SSSColor", new Color(0.86f, 0.24f, 0.14f, 1f));
                    m.SetFloat("_SSSStrength", sss);
                    m.SetFloat("_BackScatter", backScatter);
                    m.SetFloat("_SpecIntensity", spec);
                    EditorUtility.SetDirty(m);
                    return m;
                }
                return Lit(name, 1f, 1f, 1f, maps: prefix, bump: 0.22f, ao: 0.65f);
            }

            Material MakeFabricMat(string name, string prefix, Color tint, float sheen, float bump, float tile = 1.6f)
            {
                if (dhFabricShader != null && art)
                {
                    var m = MaterialAsset(name, dhFabricShader);
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        m.SetTexture("_BaseMap", Tex(prefix + "_Color"));
                        m.SetTexture("_BumpMap", Tex(prefix + "_Normal"));
                        m.SetTexture("_MaskMap", Tex(prefix + "_Mask"));
                        m.SetTextureScale("_BaseMap", new Vector2(tile, tile));
                        m.SetTextureScale("_BumpMap", new Vector2(tile, tile));
                        m.SetTextureScale("_MaskMap", new Vector2(tile, tile));
                    }
                    m.SetColor("_BaseColor", tint);
                    m.SetFloat("_BumpScale", bump);
                    m.SetFloat("_SmoothnessScale", 0.62f);
                    m.SetFloat("_OcclusionStrength", 0.78f);
                    m.SetColor("_SheenColor", new Color(0.92f, 0.93f, 0.90f, 1f));
                    m.SetFloat("_SheenIntensity", sheen);
                    m.SetFloat("_SheenRoughness", 0.56f);
                    EditorUtility.SetDirty(m);
                    return m;
                }
                return string.IsNullOrEmpty(prefix)
                    ? Lit(name, tint.r, tint.g, tint.b, smooth: 0.18f)
                    : Lit(name, tint.r, tint.g, tint.b, maps: prefix, bump: 0.30f, ao: 0.72f);
            }

            Material MakeHairMat(string name, string colorKey, string normKey, string flowKey, float cutoff, float specInt)
            {
                if (dhHairShader != null && art)
                {
                    var m = MaterialAsset(name, dhHairShader);
                    m.SetTexture("_BaseMap", Tex(colorKey));
                    if (!string.IsNullOrEmpty(normKey)) m.SetTexture("_BumpMap", Tex(normKey));
                    if (!string.IsNullOrEmpty(flowKey)) m.SetTexture("_FlowMap", Tex(flowKey));
                    m.SetColor("_BaseColor", Color.white);
                    m.SetFloat("_BumpScale", 0.55f);
                    m.SetFloat("_Cutoff", cutoff);
                    m.SetColor("_SpecColorPrimary", new Color(0.76f, 0.68f, 0.62f, 1f));
                    m.SetColor("_SpecColorSecondary", new Color(0.48f, 0.30f, 0.19f, 1f));
                    m.SetFloat("_SpecPowerPrimary", 84f);
                    m.SetFloat("_SpecPowerSecondary", 30f);
                    m.SetFloat("_SpecIntensity", specInt);
                    m.SetFloat("_BackScatter", 0.24f);
                    EditorUtility.SetDirty(m);
                    return m;
                }
                return Lit(name, 0.06f, 0.05f, 0.045f, smooth: 0.35f, twoSided: true);
            }

            // Skin (Lan, An, Gatekeeper)
            MakeSkinMat("LanSkin", "LanSkin", sss: 0.42f, backScatter: 0.16f, spec: 0.44f, detailScale: 0.22f, bump: 0.68f);
            MakeSkinMat("AnSkin", "AnSkin", sss: 0.62f, backScatter: 0.28f, spec: 0.62f, detailScale: 0.24f, bump: 0.62f);
            MakeSkinMat("GatekeeperSkin", "GatekeeperSkin", sss: 0.14f, backScatter: 0.05f, spec: 0.32f, detailScale: 0.45f, bump: 0.95f);

            // Anatomical Eyes (Cornea IOR 1.376 parallax refraction + anterior chamber caustics + wet tear-film catchlight)
            if (dhEyeShader != null && art)
            {
                var eyeM = MaterialAsset("LanEye", dhEyeShader);
                eyeM.SetTexture("_BaseMap", Tex("LanEye_Color"));
                eyeM.SetTexture("_BumpMap", Tex("LanEye_Normal"));
                eyeM.SetColor("_BaseColor", Color.white);
                eyeM.SetFloat("_BumpScale", 0.72f);
                eyeM.SetFloat("_IrisRadius", 0.122f);
                eyeM.SetFloat("_RefractionDepth", 0.015f);
                eyeM.SetColor("_CausticTint", new Color(0.70f, 0.40f, 0.19f, 1f));
                eyeM.SetFloat("_CausticStrength", 1.15f);
                eyeM.SetFloat("_CorneaRoughness", 0.028f);
                eyeM.SetFloat("_CorneaSpecIntensity", 1.48f);
                eyeM.SetFloat("_LimbalDarkening", 0.36f);
                EditorUtility.SetDirty(eyeM);
            }
            else
            {
                Lit("LanEye", 0.85f, 0.84f, 0.82f, smooth: 0.92f);
            }

            // Oral cavity (Teeth & Tongue)
            var teethM = Lit("LanTeeth", 1f, 1f, 1f, smooth: 0.76f);
            if (art) { teethM.SetTexture("_BaseMap", Tex("LanTeeth_Color")); EditorUtility.SetDirty(teethM); }
            var tongueM = Lit("LanTongue", 1f, 1f, 1f, smooth: 0.62f);
            if (art) { tongueM.SetTexture("_BaseMap", Tex("LanTongue_Color")); EditorUtility.SetDirty(tongueM); }

            // Hair, Eyebrows & Eyelashes (Anisotropic Kajiya-Kay dual-pass — silky dark Vietnamese hair, delicate feathered lashes!)
            MakeHairMat("LanHairCard", "LanHair_Color", "LanHair_Normal", "LanHair_Flow", cutoff: 0.36f, specInt: 0.42f);
            MakeHairMat("AnHairCard", "AnHair_Color", "AnHair_Normal", "AnHair_Flow", cutoff: 0.44f, specInt: 0.18f);
            MakeHairMat("GatekeeperHairCard", "GatekeeperHair_Color", "GatekeeperHair_Normal", "GatekeeperHair_Flow", cutoff: 0.44f, specInt: 0.14f);
            MakeHairMat("LanEyebrow", "LanEyebrow_Color", null, null, cutoff: 0.38f, specInt: 0.05f);
            MakeHairMat("LanEyelash", "LanEyelash_Color", null, null, cutoff: 0.45f, specInt: 0.04f);

            // Cloth-Simulated Garments (Charlie microfiber sheen)
            MakeFabricMat("NurseBlouse", "NurseBlouse", new Color(.94f, .94f, .92f), sheen: 0.16f, bump: 0.91f, tile: 1.6f);
            MakeFabricMat("NurseTrim", null, new Color(0.56f, 0.72f, 0.74f, 1f), sheen: 0.22f, bump: 0.40f, tile: 1.0f);
            MakeFabricMat("DarkTrouser", "DarkTrouser", Color.white, sheen: 0.22f, bump: 0.82f, tile: 1.5f);
            MakeFabricMat("CharredBlouse", "CharredBlouse", Color.white, sheen: 0.12f, bump: 1.05f, tile: 1.4f);
            MakeFabricMat("AnCotton", "AnCotton", Color.white, sheen: 0.28f, bump: 0.65f, tile: 1.4f);

            // Clinical Footwear, ID Badge, Hair Clip & Watch Trim
            Lit("ClinicalShoe", 1f, 1f, 1f, maps: "ClinicalShoe", bump: 0.52f, ao: 0.78f);
            var badgeM = Lit("LanBadge", 1f, 1f, 1f, smooth: 0.39f);
            if (art) { badgeM.SetTexture("_BaseMap", Tex("LanBadge")); EditorUtility.SetDirty(badgeM); }
            materials["LanBadgeMat"] = badgeM;
            Lit("MetalTrim", 0.74f, 0.76f, 0.78f, metal: 0.86f, smooth: 0.68f);
            // Keep line 61 Ceramic-textured EnamelBlue; calibrate matte worn Vietnamese honeycomb rubber ivory-ochre
            Lit("SlipperYellow", 0.76f, 0.70f, 0.49f, smooth: 0.22f);

            // Legacy compatibility aliases (if old FBX slots still referenced before scaling An/Gatekeeper)
            Lit("LanLip", 0.60f, 0.34f, 0.31f, smooth: 0.42f);
            materials["LanHair"] = Mat("LanHairCard");
            Lit("LanEyeWhite", 0.88f, 0.86f, 0.84f, smooth: 0.82f);
            Lit("LanIris", 0.14f, 0.09f, 0.06f, smooth: 0.88f);
            Lit("ObscuredFace", 1f, 1f, 1f, maps: "ObscuredFace", bump: 0.28f, ao: 0.85f);

            // Bé An Per-Slot Human-Beneath-Apparition Materials (preserves real child skin/hair/clothes/slippers!)
            Material MakeApparitionHumanMat(string name, string colorKey, string normKey, string maskKey, Color tint, float cutoff = 0f, float rimStr = 0.32f)
            {
                if (appHumanShader != null && art)
                {
                    var m = MaterialAsset(name, appHumanShader);
                    if (!string.IsNullOrEmpty(colorKey)) m.SetTexture("_BaseMap", Tex(colorKey));
                    if (!string.IsNullOrEmpty(normKey)) m.SetTexture("_BumpMap", Tex(normKey));
                    if (!string.IsNullOrEmpty(maskKey)) m.SetTexture("_MaskMap", Tex(maskKey));
                    m.SetColor("_BaseColor", tint);
                    m.SetFloat("_BumpScale", 0.68f);
                    m.SetFloat("_Cutoff", cutoff);
                    m.SetColor("_RimColor", new Color(0.76f, 0.90f, 0.88f, 1f));
                    m.SetFloat("_RimPower", 2.55f);
                    m.SetFloat("_RimStrength", rimStr);
                    m.SetFloat("_Desaturate", 0.12f);
                    m.SetFloat("_Alpha", 0f);
                    EditorUtility.SetDirty(m);
                    return m;
                }
                return Mat("AnCotton");
            }

            MakeApparitionHumanMat("AnSkin_Apparition", "AnSkin_Color", "AnSkin_Normal", "AnSkin_Mask", Color.white, 0f, 0.30f);
            MakeApparitionHumanMat("AnCotton_Apparition", "AnCotton_Color", "AnCotton_Normal", "AnCotton_Mask", Color.white, 0f, 0.34f);
            MakeApparitionHumanMat("AnHair_Apparition", "AnHair_Color", "AnHair_Normal", null, Color.white, 0.42f, 0.26f);
            MakeApparitionHumanMat("AnEye_Apparition", "LanEye_Color", "LanEye_Normal", null, Color.white, 0f, 0.18f);
            MakeApparitionHumanMat("AnTeeth_Apparition", "LanTeeth_Color", null, null, Color.white, 0f, 0.05f);
            MakeApparitionHumanMat("AnTongue_Apparition", "LanTongue_Color", null, null, Color.white, 0f, 0.03f);
            MakeApparitionHumanMat("AnEyebrow_Apparition", "LanEyebrow_Color", null, null, Color.white, 0.34f, 0.15f);
            MakeApparitionHumanMat("AnEyelash_Apparition", "LanEyelash_Color", null, null, Color.white, 0.34f, 0.15f);
            MakeApparitionHumanMat("AnSlipper_Apparition", null, null, null, new Color(0.88f, 0.72f, 0.24f, 1f), 0f, 0.28f);

            var appShader = Shader.Find("CTCC/ApparitionSoft");
            if (appShader != null)
            {
                var appMat = MaterialAsset("AnApparitionMat", appShader);
                appMat.SetColor("_BaseColor", new Color(0.76f, 0.88f, 0.86f, 1f));
                appMat.SetColor("_RimColor", new Color(0.72f, 0.96f, 0.92f, 1f));
                appMat.SetFloat("_RimPower", 2.35f);
                appMat.SetFloat("_Alpha", 0f);
                EditorUtility.SetDirty(appMat);
            }
            else
            {
                materials["AnApparitionMat"] = Mat("AnCotton");
            }

            Lit("Exterior", 0.58f, 0.60f, 0.58f, maps: "PlasterR", tint: true, bump: 0.48f, ao: 0.82f);
            Lit("Bay", 0.85f, 0.90f, 0.84f, maps: "DadoB", tint: true, bump: 0.25f, ao: 0.68f);
            Lit("Bark", 1f, 1f, 1f, maps: "Bark", bump: 0.95f, ao: 0.90f);
            Lit("Black", 0.055f, 0.062f, 0.060f, smooth: 0.30f);

            // Dusty interior cabinet & window glass: low specular reflectance so interior shelves show clearly!
            Lit("Glass", 0.18f, 0.24f, 0.22f, smooth: 0.42f, twoSided: true);
            Transparent(Mat("Glass"), new Color(0.18f, 0.24f, 0.22f, art ? 0.13f : 0.22f));
            Lit("FrostedGlass", 0.64f, 0.72f, 0.68f, smooth: 0.32f, twoSided: true);
            Transparent(Mat("FrostedGlass"), new Color(0.64f, 0.72f, 0.68f, 0.42f));
            Lit("SalineFluid", 0.76f, 0.86f, 0.88f, smooth: 0.82f, twoSided: true);
            Transparent(Mat("SalineFluid"), new Color(0.76f, 0.86f, 0.88f, 0.32f));

            // Organic feathered wet terrazzo floor sheen overlay (DampFloor alpha eliminates hard quad border!)
            var wetSheen = Lit("WetSheen", 0.16f, 0.22f, 0.24f, smooth: 0.90f);
            if (art) wetSheen.SetTexture("_BaseMap", Tex("DampFloor"));
            Transparent(wetSheen, new Color(0.22f, 0.30f, 0.32f, 0.48f));

            Lit("DeadTube", 0.46f, 0.50f, 0.48f, smooth: 0.76f);
            Lit("LiveTube", 0.88f, 0.95f, 0.87f, smooth: 0.72f);
            Mat("LiveTube").SetColor("_EmissionColor", new Color(0.72f, 0.95f, 0.83f) * 3.2f);
            Mat("LiveTube").EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(Mat("LiveTube"));

            if (art)
            {
                // Rain-streaked window glass shader
                var rainGlassShader = Shader.Find("CTCC/RainWindowGlass");
                if (rainGlassShader != null)
                {
                    var rg = MaterialAsset("RainGlassR", rainGlassShader);
                    rg.SetColor("_BaseColor", new Color(0.16f, 0.24f, 0.25f, 0.14f));
                    rg.SetColor("_SpecularColor", new Color(0.58f, 0.76f, 0.88f, 1f));
                    rg.SetFloat("_RainIntensity", 0.85f);
                    rg.SetFloat("_GrimeAmount", 0.42f);
                    rg.SetFloat("_StreakSpeed", 0.24f);
                    EditorUtility.SetDirty(rg);
                }
                else
                {
                    materials["RainGlassR"] = Mat("Glass");
                }

                // Tropical courtyard tree & elephant-ear shrub alpha-clip wind-sway foliage shaders
                var leafShader = Shader.Find("CTCC/TropicalFoliage");
                if (leafShader != null)
                {
                    var lf = MaterialAsset("TropicalLeaf", leafShader);
                    lf.SetTexture("_BaseMap", Tex("TropicalLeaf_Color"));
                    lf.SetColor("_BaseColor", new Color(0.78f, 0.88f, 0.74f, 1f));
                    lf.SetFloat("_Cutoff", 0.40f);
                    lf.SetFloat("_WindSpeed", 1.65f);
                    lf.SetFloat("_WindAmp", 0.055f);
                    lf.SetFloat("_Wetness", 0.62f);
                    EditorUtility.SetDirty(lf);

                    var sh = MaterialAsset("TropicalShrub", leafShader);
                    sh.SetTexture("_BaseMap", Tex("TropicalShrub_Color"));
                    sh.SetColor("_BaseColor", new Color(0.84f, 0.94f, 0.80f, 1f));
                    sh.SetFloat("_Cutoff", 0.35f);
                    sh.SetFloat("_WindSpeed", 1.45f);
                    sh.SetFloat("_WindAmp", 0.042f);
                    sh.SetFloat("_Wetness", 0.70f);
                    EditorUtility.SetDirty(sh);
                }
                else
                {
                    Lit("TropicalLeaf", 0.18f, 0.29f, 0.19f, smooth: 0.42f, twoSided: true);
                    Lit("TropicalShrub", 0.17f, 0.33f, 0.18f, smooth: 0.48f, twoSided: true);
                }

                foreach (string name in new[]
                {
                    "Room101", "Welcome", "Emergency", "Station", "FileA", "FileB", "FileC",
                    "Demolition", "Staff", "Store", "Socket1", "Socket2", "Socket3", "Socket4",
                    "F101", "F101_Back", "F101_SignedA", "F101_SignedB", "F101_SignedC",
                    "ObsCard", "ObsCard_Back", "Doc_Bed2Transfer", "Doc_Bed2Transfer_Back",
                    "Doc_Bed3Meds", "Doc_Bed3Meds_Back", "Doc_ShiftGuide", "Doc_ShiftGuide_Back",
                    "Doc_WardOverflow", "Doc_DutyRoster", "Doc_BayEnvelopeFront", "Doc_BayLetter", "Doc_BayLetter_Back",
                    "Doc_BayNote", "Doc_LanLetter2005", "Doc_LanLetter2005_Back", "Doc_Newspaper2005", "Doc_KeyCTag", "Doc_PhoneScreen",
                    "Sign_NursingAlcove",
                    "BedList", "Transfer", "Drawing", "WardRules", "ChartCard",
                    "Sign_EmergencyBox", "Sign_KhuBClosed", "Sign_ReceptionWindow", "Sign_HospitalGate",
                    "Calendar2005", "MonitorECG", "M31_KhuB", "M31_Kho", "M31_Service", "M31_Morgue",
                    "Sign_MainArch", "Sign_KB_Outpatient", "Sign_KC_Inpatient", "Sign_KD_Emergency",
                    "Sign_KE_Admin", "Sign_KF_Service", "Sign_NightGate", "Sign_AmbulanceGate",
                    "Sign_MorgueService", "Sign_CampusMap", "Sign_Pharmacy", "Sign_XRay",
                    "Sign_Ward102", "Sign_Ward201", "Sign_Ward202",
                    "Doc_Ch05_PostedMap2005", "Doc_Ch05_AsBuilt2004_E05"
                })
                {
                    var m = Lit(name, 1f, 1f, 1f, smooth: name is "Room101" or "Welcome" or "Emergency" or "Sign_HospitalGate" or "Sign_ReceptionWindow" or "Sign_NursingAlcove" or "Sign_MainArch" or "Sign_KB_Outpatient" or "Sign_KC_Inpatient" or "Sign_CampusMap" ? 0.42f : 0.14f, twoSided: true);
                    m.SetTexture("_BaseMap", Tex(name));
                    m.SetColor("_BaseColor", Color.white);
                    if (name == "Sign_EmergencyBox" || name == "Sign_KD_Emergency")
                    {
                        m.SetTexture("_EmissionMap", Tex(name));
                        m.SetColor("_EmissionColor", new Color(0.95f, 0.24f, 0.18f) * (name == "Sign_KD_Emergency" ? 2.15f : 1.85f));
                        m.EnableKeyword("_EMISSION");
                    }
                    EditorUtility.SetDirty(m);
                }

                foreach (string name in new[] { "DrawingPiece1", "DrawingPiece2", "DrawingPiece3", "ClockB", "ClockR", "CH01_PR01_Corner" })
                {
                    var m = Lit(name, 1f, 1f, 1f, smooth: 0.18f, twoSided: true);
                    m.SetTexture("_BaseMap", Tex(name));
                    Transparent(m, Color.white);
                }

                // True URP DBuffer Decal Projector materials (Base_Map + Normal_Map!)
                PrepareDecalMaterials();

                var beamShader = Shader.Find("CTCC/Window Light");
                if (beamShader == null) throw new InvalidOperationException("CTCC window beam shader missing");
                var beam = MaterialAsset("Beam", beamShader);
                beam.SetColor("_Tint", new Color(0.46f, 0.64f, 0.76f, 0.095f));
                beam.SetFloat("_MoteStrength", 0.45f);
                EditorUtility.SetDirty(beam);

                var beamFluorescent = MaterialAsset("BeamFluorescent", beamShader);
                beamFluorescent.SetColor("_Tint", new Color(0.72f, 0.84f, 0.74f, 0.030f));
                beamFluorescent.SetFloat("_MoteStrength", 0.28f);
                EditorUtility.SetDirty(beamFluorescent);

                var beamWarm = MaterialAsset("BeamWarm", beamShader);
                beamWarm.SetColor("_Tint", new Color(0.95f, 0.74f, 0.44f, 0.052f));
                beamWarm.SetFloat("_MoteStrength", 0.34f);
                EditorUtility.SetDirty(beamWarm);

                // Upgrade #6: Retain CTCC/InspectPaperRaking shader + reference material in standalone builds!
                var rakingShader = Shader.Find("CTCC/InspectPaperRaking");
                if (rakingShader != null)
                {
                    world.rakingPaperShader = rakingShader;
                    var rakingRef = MaterialAsset("InspectPaperRaking", rakingShader);
                    rakingRef.SetTexture("_BaseMap", Tex("ObsCard_Back"));
                    rakingRef.SetTexture("_IndentNormalMap", Tex("ObsCard_Back_Indent_Normal"));
                    rakingRef.SetTexture("_IndentMaskMap", Tex("ObsCard_Back_Indent_Mask"));
                    rakingRef.SetColor("_BaseColor", new Color(0.84f, 0.83f, 0.80f, 1f));
                    rakingRef.SetFloat("_IndentScale", 1.85f);
                    rakingRef.SetFloat("_RakingReveal", 0f);
                    EditorUtility.SetDirty(rakingRef);
                }

                // Retain URP/Lit {_NORMALMAP}-without-{_METALLICSPECGLOSSMAP} variant in standalone builds for PhysicalInspector sheets!
                var litShader = Shader.Find("Universal Render Pipeline/Lit");
                if (litShader != null)
                {
                    var inspectLit = MaterialAsset("InspectPaperLit", litShader);
                    inspectLit.SetColor("_BaseColor", new Color(0.86f, 0.85f, 0.82f, 1f));
                    inspectLit.SetFloat("_Smoothness", 0.05f);
                    inspectLit.SetFloat("_Metallic", 0f);
                    inspectLit.SetFloat("_Cull", 2f);
                    var fiber = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/Resources/CTCC/PaperFiber_Normal.png");
                    if (fiber != null)
                    {
                        inspectLit.SetTexture("_BumpMap", fiber);
                        inspectLit.SetFloat("_BumpScale", 0.16f);
                        inspectLit.EnableKeyword("_NORMALMAP");
                    }
                    EditorUtility.SetDirty(inspectLit);
                    world.inspectPaperTemplate = inspectLit;
                }
            }
        }

        private void ConfigureAssetImporters()
        {
            foreach (var f in Directory.GetFiles(Maps, "*.png"))
            {
                string path = f.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                bool normal = path.EndsWith("_Normal.png", StringComparison.Ordinal);
                bool linearData = path.EndsWith("_Mask.png", StringComparison.Ordinal) ||
                                  path.EndsWith("_Flow.png", StringComparison.Ordinal);
                bool repeat = path.EndsWith("_Color.png", StringComparison.Ordinal) || normal || linearData;
                bool leafOrHairAlpha = path.EndsWith("TropicalLeaf_Color.png", StringComparison.Ordinal) ||
                                       path.EndsWith("TropicalShrub_Color.png", StringComparison.Ordinal) ||
                                       path.EndsWith("HairCards_Color.png", StringComparison.Ordinal) ||
                                       path.EndsWith("LanHair_Color.png", StringComparison.Ordinal) ||
                                       path.EndsWith("AnHair_Color.png", StringComparison.Ordinal) ||
                                       path.EndsWith("GatekeeperHair_Color.png", StringComparison.Ordinal) ||
                                       path.EndsWith("LanEyebrow_Color.png", StringComparison.Ordinal) ||
                                       path.EndsWith("LanEyelash_Color.png", StringComparison.Ordinal);
                string family = Path.GetFileNameWithoutExtension(path).Split('_')[0];
                bool clampedAtlas = leafOrHairAlpha || family is "LanSkin" or "AnSkin" or "GatekeeperSkin"
                    or "LanEye" or "LanTeeth" or "LanTongue" or "LanBadge" or "LanHair" or "AnHair"
                    or "GatekeeperHair" or "HairCards" or "LanEyebrow" or "LanEyelash" or "ObscuredFace"
                    or "ObsCard" or "F101" or "LOC02" or "LOC03";
                bool heroAtlas = family is "LOC02" or "LOC03";
                bool stream = repeat && (!clampedAtlas || heroAtlas) && family is not ("WaterRun" or "PaintLoss" or "DampFloor" or "RustRun" or "WallBaseGrime" or "SkinMicroPore" or "TubeYellowStain" or "StretcherDragTrack" or "TerrazzoRainSeep");
                var wantedType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                var wrap = repeat && !clampedAtlas ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                bool wantAlpha = !repeat || leafOrHairAlpha;
                bool keepPaperSize = family is "Doc" or "F101" or "ObsCard" or "Calendar2005" or "BedList" or "Transfer" or "Drawing" or "DrawingPiece1" or "DrawingPiece2" or "DrawingPiece3" or "LanBadge" or "Sign";
                var npotScale = keepPaperSize ? TextureImporterNPOTScale.None : importer.npotScale;
                var wantFilter = (keepPaperSize || heroAtlas) ? FilterMode.Trilinear : FilterMode.Bilinear;
                int wantAniso = (keepPaperSize || heroAtlas) ? 16 : 8;
                float wantMipBias = keepPaperSize ? -0.5f : (heroAtlas ? -0.25f : 0f);
                var wantComp = (keepPaperSize || heroAtlas) ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Compressed;
                int wantPriority = heroAtlas ? 4 : 0;
                if (importer.textureType == wantedType && importer.sRGBTexture == !linearData &&
                    importer.wrapMode == wrap && importer.anisoLevel == wantAniso && importer.maxTextureSize == 2048 &&
                    importer.alphaIsTransparency == wantAlpha && importer.streamingMipmaps == stream &&
                    importer.streamingMipmapsPriority == wantPriority &&
                    importer.npotScale == npotScale && importer.filterMode == wantFilter &&
                    Mathf.Approximately(importer.mipMapBias, wantMipBias) && importer.textureCompression == wantComp)
                    continue;
                importer.textureType = wantedType;
                importer.sRGBTexture = !linearData;
                importer.wrapMode = wrap;
                importer.maxTextureSize = 2048;
                importer.anisoLevel = wantAniso;
                importer.filterMode = wantFilter;
                importer.mipMapBias = wantMipBias;
                importer.alphaIsTransparency = wantAlpha;
                importer.streamingMipmaps = stream;
                importer.streamingMipmapsPriority = wantPriority;
                importer.npotScale = npotScale;
                importer.textureCompression = wantComp;
                importer.SaveAndReimport();
            }

            // Ensure Resources/CTCC/Calendar2005_Previous.png also gets crisp Trilinear + aniso 16 + NPOT None + BC7!
            const string calPrevPath = Root + "/Art/Resources/CTCC/Calendar2005_Previous.png";
            if (AssetImporter.GetAtPath(calPrevPath) is TextureImporter calImp &&
                (calImp.anisoLevel != 16 || calImp.filterMode != FilterMode.Trilinear || calImp.npotScale != TextureImporterNPOTScale.None || !Mathf.Approximately(calImp.mipMapBias, -0.5f)))
            {
                calImp.wrapMode = TextureWrapMode.Clamp;
                calImp.anisoLevel = 16;
                calImp.filterMode = FilterMode.Trilinear;
                calImp.mipMapBias = -0.5f;
                calImp.npotScale = TextureImporterNPOTScale.None;
                calImp.textureCompression = TextureImporterCompression.CompressedHQ;
                calImp.SaveAndReimport();
            }

            foreach (var f in Directory.GetFiles(Models, "*.fbx"))
            {
                string path = f.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;
                bool isCharacter = Path.GetFileName(path).StartsWith("CTCC_CHR_", StringComparison.Ordinal);
                bool needReadable = path.EndsWith("CTCC_ENV_CurtainAssembly.fbx", StringComparison.Ordinal);

                if (isCharacter)
                {
                    var defaultClips = importer.defaultClipAnimations;
                    bool clipsMatch = defaultClips != null && importer.clipAnimations != null &&
                        importer.clipAnimations.Length == defaultClips.Length &&
                        importer.clipAnimations.Select(c => $"{c.name}|{c.takeName}|{Mathf.RoundToInt(c.lastFrame)}")
                            .SequenceEqual(defaultClips.Select(c => $"{c.name}|{c.takeName}|{Mathf.RoundToInt(c.lastFrame)}"));
                    if (importer.importBlendShapes && importer.importAnimation &&
                        importer.animationType == ModelImporterAnimationType.Generic &&
                        importer.importTangents == ModelImporterTangents.CalculateMikk && clipsMatch)
                        continue;
                    importer.importBlendShapes = true;
                    importer.importAnimation = true;
                    importer.animationType = ModelImporterAnimationType.Generic;
                    importer.importCameras = false;
                    importer.importLights = false;
                    importer.isReadable = false;
                    importer.importNormals = ModelImporterNormals.Import;
                    importer.importTangents = ModelImporterTangents.CalculateMikk;
                    importer.generateSecondaryUV = false;
                    if (defaultClips != null && defaultClips.Length > 0)
                    {
                        foreach (var clip in defaultClips)
                            clip.loopTime = !clip.name.Contains("Handover") && !clip.name.Contains("TurnFade");
                        importer.clipAnimations = defaultClips;
                    }
                    importer.SaveAndReimport();
                }
                else
                {
                    if (importer.importAnimation == false && importer.importTangents == ModelImporterTangents.CalculateMikk &&
                        importer.generateSecondaryUV == true && importer.isReadable == needReadable)
                        continue;
                    importer.importBlendShapes = false;
                    importer.importAnimation = false;
                    importer.importCameras = false;
                    importer.importLights = false;
                    importer.isReadable = needReadable;
                    importer.importNormals = ModelImporterNormals.Import;
                    importer.importTangents = ModelImporterTangents.CalculateMikk;
                    importer.generateSecondaryUV = true;
                    importer.SaveAndReimport();
                }
            }
        }

        private void PrepareDecalMaterials()
        {
            var template = AssetDatabase.LoadAssetAtPath<Material>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Decal.mat");
            var decalShader = template != null ? template.shader : Shader.Find("Shader Graphs/Decal");

            foreach (var spec in new (string key, string normalKey, float normalBlend)[]
            {
                ("WaterRun", "WaterRun_Normal", 0.72f),
                ("PaintLoss", "PaintLoss_Normal", 0.95f),
                ("DampFloor", "DampFloor_Normal", 0.45f),
                ("RustRun", "RustRun_Normal", 0.78f),
                ("WallBaseGrime", "WallBaseGrime_Normal", 0.70f),
                ("TubeYellowStain", "TubeYellowStain_Normal", 0.52f),
                ("StretcherDragTrack", "StretcherDragTrack_Normal", 0.60f),
                ("TerrazzoRainSeep", "TerrazzoRainSeep_Normal", 0.76f),
                ("WheelMarks", null, 0f),
                ("BedContact", null, 0f),
                ("CH01_Capillary", null, 0f),
                ("CH01_LimeRunoff", null, 0f),
                ("CH01_EnamelChips", null, 0f),
                ("CH01_WheelWear", null, 0f),
                ("CH01_VarnishWear", null, 0f),
            })
            {
                string path = Root + "/Generated/Materials/Decal_" + spec.key + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = template != null ? new Material(template) : new Material(decalShader);
                    AssetDatabase.CreateAsset(mat, path);
                }
                else if (decalShader != null && mat.shader != decalShader)
                {
                    mat.shader = decalShader;
                }
                mat.name = "Decal_" + spec.key;
                var baseTex = Tex(spec.key);
                mat.SetTexture("Base_Map", baseTex);
                mat.SetTexture("_BaseMap", baseTex);
                if (spec.normalKey != null && File.Exists(Maps + spec.normalKey + ".png"))
                {
                    var normTex = Tex(spec.normalKey);
                    mat.SetTexture("Normal_Map", normTex);
                    mat.SetFloat("Normal_Blend", spec.normalBlend);
                }
                else
                {
                    mat.SetTexture("Normal_Map", null);
                    mat.SetFloat("Normal_Blend", 0f);
                }
                EditorUtility.SetDirty(mat);
                decalMaterials[spec.key] = mat;
            }
        }

        private Material Lit(string key, float r, float g, float b, float metal = 0f, float smooth = 0.32f,
            string maps = null, bool tint = false, bool twoSided = false, float bump = 1.0f, float ao = 0.85f)
        {
            var mat = MaterialAsset(key, Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(r, g, b));
            mat.SetFloat("_Metallic", metal);
            mat.SetFloat("_Smoothness", smooth);
            mat.SetFloat("_Cull", twoSided ? 0 : 2);
            if (art && maps != null)
            {
                mat.SetTexture("_BaseMap", Tex(maps + "_Color"));
                if (!tint) mat.SetColor("_BaseColor", Color.white);
                mat.SetTexture("_BumpMap", Tex(maps + "_Normal"));
                mat.SetFloat("_BumpScale", bump);
                mat.SetTexture("_MetallicGlossMap", Tex(maps + "_Mask"));
                mat.SetTexture("_OcclusionMap", Tex(maps + "_Mask"));
                mat.SetFloat("_OcclusionStrength", ao);
                // Smoothness multiplier = 1.0f so authored per-pixel smoothness in _Mask.a is NEVER overwritten!
                mat.SetFloat("_Smoothness", 1.0f);
                mat.EnableKeyword("_NORMALMAP");
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                mat.EnableKeyword("_OCCLUSIONMAP");
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private void HeroAtlasMat(string atlas, bool hasR, float bump = 0.82f, float ao = 0.90f, bool twoSided = false)
        {
            if (!art)
            {
                if (hasR)
                {
                    Lit(atlas + "B", 0.45f, 0.50f, 0.46f, twoSided: twoSided);
                    Lit(atlas + "R", 0.32f, 0.36f, 0.34f, twoSided: twoSided);
                }
                else Lit(atlas, 0.55f, 0.54f, 0.50f, twoSided: twoSided);
                return;
            }
            var normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Maps + atlas + "_Normal.png");
            void BuildVariant(string key)
            {
                var mat = MaterialAsset(key, Shader.Find("Universal Render Pipeline/Lit"));
                mat.SetColor("_BaseColor", Color.white);
                mat.SetFloat("_Metallic", 1.0f);
                mat.SetFloat("_Smoothness", 1.0f);
                mat.SetFloat("_Cull", twoSided ? 0 : 2);
                var col = AssetDatabase.LoadAssetAtPath<Texture2D>(Maps + key + "_Color.png");
                var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(Maps + key + "_Mask.png");
                if (col != null) mat.SetTexture("_BaseMap", col);
                if (normalTex != null)
                {
                    mat.SetTexture("_BumpMap", normalTex);
                    mat.SetFloat("_BumpScale", bump);
                    mat.EnableKeyword("_NORMALMAP");
                }
                if (mask != null)
                {
                    mat.SetTexture("_MetallicGlossMap", mask);
                    mat.SetTexture("_OcclusionMap", mask);
                    mat.SetFloat("_OcclusionStrength", ao);
                    mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                    mat.EnableKeyword("_OCCLUSIONMAP");
                }
                EditorUtility.SetDirty(mat);
            }
            if (hasR)
            {
                BuildVariant(atlas + "B");
                BuildVariant(atlas + "R");
            }
            else BuildVariant(atlas);
        }

        private Material MaterialAsset(string key, Shader shader)
        {
            string flavor = art ? "Reference_" : "Blockout_";
            var path = Root + "/Generated/Materials/" + flavor + key + ".mat";
            var item = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (item == null)
            {
                item = new Material(shader);
                AssetDatabase.CreateAsset(item, path);
            }
            else if (item.shader != shader)
            {
                item.shader = shader;
            }
            item.name = key;
            materials[key] = item;
            return item;
        }

        private static void Transparent(Material m, Color tint)
        {
            m.SetColor("_BaseColor", tint);
            m.SetFloat("_Surface", 1);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetShaderPassEnabled("ShadowCaster", false);
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
        }

        private Material Mat(string key) => materials.TryGetValue(key, out var m) ? m : materials["MetalOld"];

        private static Texture2D Tex(string name) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(Maps + name + ".png") ?? throw new InvalidOperationException("Missing texture " + name);

        private GameObject Flat(string name, string material, Vector3 position, Vector3 normal, Vector2 scale, Transform parent = null)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name;
            q.transform.SetParent(ResolveParent(parent, position), false);
            q.transform.localPosition = position;
            q.transform.localRotation = normal.y > 0.95f
                ? Quaternion.Euler(90, 0, 0)
                : Quaternion.LookRotation(-normal, Vector3.up);
            q.transform.localScale = new Vector3(scale.x, scale.y, 1);
            UnityEngine.Object.DestroyImmediate(q.GetComponent<MeshCollider>());
            var renderer = q.GetComponent<Renderer>();
            renderer.sharedMaterial = Mat(material);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return q;
        }

        private void Decal(string name, string decalKey, Vector3 position, Vector3 surfaceNormal, float width, float height,
            float depth = 0.22f, Transform parent = null, float opacity = 1.0f, float rollDeg = 0f)
        {
            if (!art || !decalMaterials.TryGetValue(decalKey, out var mat)) return;
            var go = Group(name, ResolveParent(parent, position), position).gameObject;
            // DecalProjector projects along local +Z, so point +Z into the receiving surface (-surfaceNormal)
            Vector3 up = Mathf.Abs(surfaceNormal.y) > 0.92f ? Vector3.forward : Vector3.up;
            go.transform.localRotation = Quaternion.LookRotation(-surfaceNormal, up) * Quaternion.Euler(0, 0, rollDeg);
            var proj = go.AddComponent<DecalProjector>();
            proj.material = mat;
            proj.size = new Vector3(width, height, depth);
            proj.fadeFactor = opacity;
            proj.drawDistance = 30f;
            proj.fadeScale = 0.9f;
            proj.startAngleFade = 150f;
            proj.endAngleFade = 180f;
        }

        private void Sign(string name, string map, Vector3 position, Vector3 normal, Vector2 scale, Transform parent = null, bool metalBacking = true)
        {
            if (!art) return;
            Transform root = ResolveParent(parent, position);
            bool isPermRoot = parent == null || parent == permanent || (multiScene && zones.Values.Any(z => z.permanent == root));
            if (metalBacking)
            {
                var plate = PlaceBlenderModel("CTCC_ENV_EnamelSignPlate", root, position - normal * 0.002f, 0f,
                    pairRB: false, castShadows: false);
                plate.transform.localRotation = Quaternion.LookRotation(normal, Vector3.up);
                plate.transform.localScale = new Vector3(scale.x, scale.y / 0.38f, 1f);
                if (isPermRoot) staticArchitecture.Add(plate);
            }
            var q = Flat(name, map, position + normal * 0.004f, normal, scale * 0.96f, root);
            if (isPermRoot) staticArchitecture.Add(q);
        }

        private GameObject CapsuleTarget(string id, string title, Vector3 position, Vector3 size, Transform parent = null)
        {
            var marker = Group(id, ResolveParent(parent, position), position).gameObject;
            marker.layer = HospitalScene.InteractLayer;
            var trigger = marker.AddComponent<BoxCollider>();
            trigger.size = size;
            trigger.isTrigger = true;
            var target = marker.AddComponent<SceneTarget>();
            target.id = id;
            target.title = title;
            return marker;
        }

        /// <summary>
        /// Instantiate a Blender 4.5 FBX asset inside a clean pivot holder so single-mesh FBX prefabs
        /// NEVER lose Unity's native -90 X Z-up-to-Y-up root rotation, while preserving LODGroup & R/B material swaps!
        /// </summary>
        private GameObject PlaceBlenderModel(string fbxName, Transform parent, Vector3 localPos, float yawDeg = 0f,
            bool pairRB = true, bool forceAged = false, Vector3? scale = null, bool castShadows = true)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Models + fbxName + ".fbx");
            if (prefab == null) throw new InvalidOperationException("Missing Blender FBX asset: " + fbxName);

            var holder = new GameObject(fbxName);
            holder.transform.SetParent(ResolveParent(parent, localPos), false);
            holder.transform.localPosition = localPos;
            holder.transform.localRotation = Quaternion.Euler(0f, 180f + yawDeg, 0f);
            if (scale.HasValue) holder.transform.localScale = scale.Value;

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder.transform);
            inst.transform.localPosition = Vector3.zero;

            // Remap FBX placeholder materials to calibrated PBR materials + register R/B paired swaps
            foreach (var rend in inst.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!castShadows)
                    rend.shadowCastingMode = ShadowCastingMode.Off;
                var orig = rend.sharedMaterials;
                var realMats = new Material[orig.Length];
                var repMats = new Material[orig.Length];
                bool differs = false;
                for (int i = 0; i < orig.Length; i++)
                {
                    string rawName = orig[i] != null ? CleanMatName(orig[i].name) : "MetalPaint";
                    string repKey = ReportKey(rawName);
                    string realKey = AgedKey(repKey);
                    repMats[i] = Mat(repKey);
                    realMats[i] = Mat(forceAged ? realKey : (pairRB ? realKey : repKey));
                    if (realMats[i] != repMats[i]) differs = true;
                }
                rend.sharedMaterials = realMats;
                if (pairRB && differs)
                {
                    AddFinish(new LayerFinish
                    {
                        renderer = rend,
                        realMaterials = realMats,
                        reportMaterials = repMats
                    });
                }
            }
            return holder;
        }

        /// <summary>
        /// Instantiates a rigged, blendshaped, animated Blender 4.5 character FBX (Lan, Gatekeeper, Bé An),
        /// remaps SkinnedMeshRenderer PBR materials cleanly, and binds all embedded FBX AnimationClips to CharacterActor!
        /// </summary>
        private CharacterActor PlaceCharacterActor(string fbxName, Transform parent, Vector3 localPos, float yawDeg,
            string charId, bool isApparition = false, bool enableBlink = true, bool enableLipSync = true)
        {
            string fbxPath = Models + fbxName + ".fbx";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (prefab == null) throw new InvalidOperationException("Missing Blender character FBX: " + fbxName);

            var holder = new GameObject(fbxName + " · " + charId);
            holder.transform.SetParent(parent, false);
            holder.transform.localPosition = localPos;
            holder.transform.localRotation = Quaternion.Euler(0f, yawDeg, 0f);

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder.transform);
            inst.transform.localPosition = Vector3.zero;
            // The FBX importer maps Blender's authored front to local +Z.
            inst.transform.localRotation = Quaternion.identity;

            var actor = holder.AddComponent<CharacterActor>();
            actor.characterId = charId;
            actor.clips = ActorClips(fbxPath);
            actor.isApparition = isApparition;
            actor.enableBlink = enableBlink;
            actor.enableLipSync = enableLipSync;
            actor.enableLookAt = !isApparition;
            ConfigureActorModel(prefab, inst, isApparition, charId, key => Mat(key));

            // Attach toggleable physical 3D F101 Clipboard prop directly onto Lan's right-hand ClipboardSocket_R!
            if (charId.Contains("CHAR002"))
            {
                var sock = FindDescendant(inst.transform, "ClipboardSocket_R");
                if (sock != null)
                {
                    var clipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "CTCC_PROP_ClipboardF101.fbx");
                    if (clipPrefab != null)
                    {
                        var held = (GameObject)PrefabUtility.InstantiatePrefab(clipPrefab, sock);
                        held.name = "Lan_HeldClipboardF101";
                        held.transform.localPosition = new Vector3(0.02f, 0.07f, -0.02f);
                        held.transform.localRotation = Quaternion.Euler(-48f, 0f, 14f);
                        held.transform.localScale = Vector3.one * 0.90f;
                        foreach (var col in held.GetComponentsInChildren<Collider>(true))
                            UnityEngine.Object.DestroyImmediate(col);
                        foreach (var mr in held.GetComponentsInChildren<MeshRenderer>(true))
                        {
                            var ms = mr.sharedMaterials;
                            for (int i = 0; i < ms.Length; i++)
                            {
                                string mname = ms[i] != null ? CleanMatName(ms[i].name) : "";
                                if (mname == "Paper" || mname == "F101") ms[i] = Mat("F101");
                                else if (materials.ContainsKey(mname)) ms[i] = Mat(mname);
                            }
                            mr.sharedMaterials = ms;
                        }
                        held.SetActive(false);
                        actor.heldClipboard = held;
                    }
                }
            }
            return actor;
        }

        private static AnimationClip[] ActorClips(string fbxPath) => AssetDatabase.LoadAllAssetsAtPath(fbxPath)
            .OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal))
            .ToArray();

        private static void ConfigureActorModel(GameObject prefab, GameObject instance, bool apparition, string charId, Func<string, Material> resolve)
        {
            var byName = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).ToDictionary(r => r.name);
            var levels = new[] { new List<Renderer>(), new List<Renderer>(), new List<Renderer>() };
            bool isOrderly = charId != null && charId.Contains("CHAR006");
            bool isUnrecorded = charId != null && charId.Contains("CHAR007_Unrecorded");
            foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!byName.TryGetValue(renderer.name, out var source))
                    throw new InvalidOperationException($"Stale character renderer {renderer.name}");
                var slots = source.sharedMaterials;
                var mapped = new Material[slots.Length];
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i] == null) throw new InvalidOperationException($"Empty slot in {source.name}");
                    string key = CleanMatName(slots[i].name);
                    if (isOrderly && key == "CharredBlouse")
                        key = "NurseBlouse";
                    if (apparition)
                    {
                        key = key switch
                        {
                            "AnSkin" or "ObscuredFace" => "AnSkin_Apparition",
                            "AnCotton" => "AnCotton_Apparition",
                            "AnHairCard" or "GatekeeperHairCard" => "AnHair_Apparition",
                            "LanEye" => "AnEye_Apparition",
                            "LanEyebrow" => "AnEyebrow_Apparition",
                            "LanEyelash" => "AnEyelash_Apparition",
                            "LanTeeth" => "AnTeeth_Apparition",
                            "LanTongue" => "AnTongue_Apparition",
                            "SlipperYellow" => "AnSlipper_Apparition",
                            _ when isUnrecorded => "AnCotton_Apparition",
                            _ => throw new InvalidOperationException($"Unknown apparition material {key}")
                        };
                    }
                    mapped[i] = resolve(key);
                    if (mapped[i] == null) throw new InvalidOperationException($"Missing material {key}");
                }
                renderer.sharedMaterials = mapped;
                renderer.updateWhenOffscreen = false;
                renderer.shadowCastingMode = apparition ? ShadowCastingMode.Off : ShadowCastingMode.On;
                int level = renderer.name.EndsWith("_LOD0", StringComparison.Ordinal) ? 0 :
                    renderer.name.EndsWith("_LOD1", StringComparison.Ordinal) ? 1 :
                    renderer.name.EndsWith("_LOD2", StringComparison.Ordinal) ? 2 : -1;
                if (level < 0) throw new InvalidOperationException($"Missing character LOD suffix on {renderer.name}");
                levels[level].Add(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            if (levels.Any(level => level.Count == 0))
                throw new InvalidOperationException($"{instance.name} must supply all three LOD levels");

            var group = instance.GetComponent<LODGroup>() ?? instance.AddComponent<LODGroup>();
            group.SetLODs(new[]
            {
                new LOD(0.22f, levels[0].ToArray()), new LOD(0.08f, levels[1].ToArray()),
                new LOD(0.01f, levels[2].ToArray())
            });
            group.RecalculateBounds();
            PrefabUtility.RecordPrefabInstancePropertyModifications(group);
        }

        public static void RefreshCharacterModels()
        {
            Material Generated(string key) => AssetDatabase.LoadAssetAtPath<Material>(Root + "/Generated/Materials/Reference_" + key + ".mat");
            var lanSkin = Generated("LanSkin");
            lanSkin.SetFloat("_SSSStrength", 0.42f);
            lanSkin.SetFloat("_BackScatter", 0.16f);
            lanSkin.SetFloat("_SpecIntensity", 0.44f);
            lanSkin.SetFloat("_DetailScale", 0.22f);
            lanSkin.SetFloat("_BumpScale", 0.68f);
            EditorUtility.SetDirty(lanSkin);
            foreach (var mapping in new[] { ("AnTeeth_Apparition", "LanTeeth_Color", 0.05f), ("AnTongue_Apparition", "LanTongue_Color", 0.03f) })
            {
                var material = Generated(mapping.Item1);
                if (material == null)
                {
                    material = new Material(Generated("AnEye_Apparition"));
                    AssetDatabase.CreateAsset(material, Root + "/Generated/Materials/Reference_" + mapping.Item1 + ".mat");
                }
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/Textures/" + mapping.Item2 + ".png"));
                material.SetTexture("_BumpMap", null);
                material.SetFloat("_RimStrength", mapping.Item3);
                EditorUtility.SetDirty(material);
            }
            foreach (var zone in ZoneSpecs)
            {
                var scene = EditorSceneManager.OpenScene(Root + "/Scenes/" + zone.name + ".unity");
                var actors = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<CharacterActor>(true)).ToArray();
                foreach (var actor in actors)
                {
                    var group = actor.GetComponentInChildren<LODGroup>(true);
                    if (group == null) throw new InvalidOperationException($"Missing character model on {actor.name}");
                    var model = group.gameObject;
                    string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(model);
                    var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    ConfigureActorModel(source, model, actor.isApparition, actor.characterId, Generated);
                    actor.clips = ActorClips(path);
                    EditorUtility.SetDirty(actor);
                }
                if (actors.Length > 0) EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }

        private static Transform FindDescendant(Transform root, string exactName)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == exactName)
                    return t;
            return null;
        }

        private static string CleanMatName(string name)
        {
            int dot = name.IndexOf('.');
            if (dot > 0) name = name.Substring(0, dot);
            int sp = name.IndexOf(' ');
            if (sp > 0) name = name.Substring(0, sp);
            return name;
        }

        private static string ReportKey(string key)
        {
            if ((key.StartsWith("LOC02_", StringComparison.Ordinal) || key.StartsWith("LOC03_", StringComparison.Ordinal))
                && key.EndsWith("R", StringComparison.Ordinal))
                return key.Substring(0, key.Length - 1) + "B";
            return key switch
            {
                "PlasterR" => "PlasterB",
                "DadoR" => "DadoB",
                "FloorR" => "FloorB",
                "WoodDoor" or "WoodDoorR" => "WoodDoorB",
                "MetalOld" => "MetalPaint",
                "CabinetOld" => "CabinetPaint",
                "CottonOld" => "Cotton",
                "TextileOld" => "Textile",
                "MattressOld" => "Mattress",
                "CurtainOld" => "Curtain",
                "DeadTube" => "LiveTube",
                "ExteriorFacade" => "ExteriorFacadeB",
                "CampusStuccoR" => "CampusStuccoB",
                "CorrugatedRoofR" => "CorrugatedRoofB",
                "ConcretePavingR" => "ConcretePavingB",
                "WindowDarkR" => "WindowLitB",
                _ => key
            };
        }

        private static string AgedKey(string reportKey)
        {
            if ((reportKey.StartsWith("LOC02_", StringComparison.Ordinal) || reportKey.StartsWith("LOC03_", StringComparison.Ordinal))
                && reportKey.EndsWith("B", StringComparison.Ordinal))
                return reportKey.Substring(0, reportKey.Length - 1) + "R";
            return reportKey switch
            {
                "PlasterB" => "PlasterR",
                "DadoB" => "DadoR",
                "FloorB" => "FloorR",
                "WoodDoorB" => "WoodDoorR",
                "MetalPaint" => "MetalOld",
                "CabinetPaint" => "CabinetOld",
                "Cotton" => "CottonOld",
                "Textile" => "TextileOld",
                "Mattress" => "MattressOld",
                "Curtain" => "CurtainOld",
                "LiveTube" => "DeadTube",
                "ExteriorFacadeB" => "ExteriorFacade",
                "CampusStuccoB" => "CampusStuccoR",
                "CorrugatedRoofB" => "CorrugatedRoofR",
                "ConcretePavingB" => "ConcretePavingR",
                "WindowLitB" => "WindowDarkR",
                _ => reportKey
            };
        }

        private void BuildFurnishing()
        {
            // Reception & Duty Desk (LOC02) — Hero Blender 4.5 two-tier counter with preserved collision & heights!
            var desk = Group("Bàn trực · quầy hai cao độ", ResolveParent(permanent, new Vector3(1.45f, 0, -3.24f)), new Vector3(1.45f, 0, -3.24f));
            var plinthCol = Box("Terrazzo plinth", new Vector3(0, 0.06f, 0), new Vector3(1.35f, 0.12f, 3.88f), Mat("BaseTrim"), desk, true, isStatic: true);
            var apronCol = Surface("Counter apron", new Vector3(1.45f, 0.55f, -3.24f), new Vector3(1.25f, 0.98f, 3.8f), "DadoR", "DadoB", true, 1.8f, isStatic: true);
            var topCol = Box("Wide counter top", new Vector3(0, 1.08f, 0), new Vector3(1.48f, 0.095f, 4f), Mat("Ceramic"), desk, tile: 1.2f, isStatic: true);
            if (art)
            {
                // Preserve exact solid BoxColliders while hiding primitive cubes beneath our sculpted Blender counter!
                plinthCol.GetComponent<Renderer>().enabled = false;
                apronCol.GetComponent<Renderer>().enabled = false;
                topCol.GetComponent<Renderer>().enabled = false;
                var lowerApronCol = Box("Lower accessible apron", new Vector3(0.05f, 0.37f, -2.32f), new Vector3(1.16f, 0.74f, 0.64f), Mat("DadoR"), desk, true, tile: 1.8f, isStatic: true);
                var lowerEndCol = Box("Lower accessible end", new Vector3(0.05f, 0.78f, -2.32f), new Vector3(1.25f, 0.07f, 0.71f), Mat("Ceramic"), desk, true, isStatic: true);
                lowerApronCol.GetComponent<Renderer>().enabled = false;
                lowerEndCol.GetComponent<Renderer>().enabled = false;

                var heroCounter = PlaceBlenderModel("CTCC_LOC02_ReceptionCounter", desk, Vector3.zero, 0f, pairRB: true, castShadows: true);
                staticArchitecture.Add(heroCounter);

                // Blender Reception Props: open duty ledger + vintage landline phone + green desk lamp + folder tray + stamps!
                var recProps = PlaceBlenderModel("CTCC_PROP_ReceptionProps", desk, new Vector3(0.32f, 1.128f, -0.18f), 0f, pairRB: true, castShadows: true);
                staticArchitecture.Add(recProps);
                Decal("DeskContactAO", "BedContact", new Vector3(1.45f, 0.01f, -3.24f), Vector3.up, 1.85f, 4.45f, 0.25f, permanent, 0.85f);

                Sign("Quầy", "Station", new Vector3(2.085f, 0.67f, -3.35f), Vector3.right, new Vector2(0.93f, 0.35f));
                // Mounted on solid north pier of west wall (z in [-2.65..0]) above FileRack — never floats over S1 stairwell!
                Sign("Foundation plaque", "Welcome", new Vector3(-0.985f, 2.26f, -1.78f), Vector3.right, new Vector2(1.26f, 0.48f));

                // Real 3D Blender Wall Clock with baked high-res dial face + separate R (11:52) and B (03:17) 3D hands facing +X into LOC02!
                var clock = PlaceBlenderModel("CTCC_PROP_WallClock", permanent, new Vector3(-0.99f, 2.74f, -3.26f), -90f, pairRB: true, castShadows: true);
                var handsR = FindDescendant(clock.transform, "HandsR");
                var handsB = FindDescendant(clock.transform, "HandsB");
                if (multiScene)
                {
                    if (handsR != null) zones["LOC02"].region.clockHandsR = handsR.gameObject;
                    if (handsB != null) zones["LOC02"].region.clockHandsB = handsB.gameObject;
                }
                else
                {
                    if (handsR != null) world.clockHandsR = handsR.gameObject;
                    if (handsB != null) world.clockHandsB = handsB.gameObject;
                }
            }

            // Room 101 Beds 1-3 + Outlets 1-4 + Bedside Cabinets + IV Stands + Vietnamese Attendant Props!
            foreach (var bed in layout.beds)
            {
                bool fourth = bed.id == "4";
                Vector3 p = new Vector3(bed.x, 0, bed.z);
                if (!fourth)
                {
                    var root = Group("Bed " + bed.id, ResolveParent(permanent, p), p);
                    root.localRotation = Quaternion.Euler(0, bed.yaw, 0);
                    var proxy = Box("Bed clearance volume", new Vector3(0, 0.405f, 0),
                        new Vector3(layout.bedWidth, 0.81f, layout.bedLength), Mat("MetalPaint"), root, true);
                    if (art)
                    {
                        proxy.GetComponent<MeshRenderer>().enabled = false;
                        // Bed 2 uses lived-in hero variant (turned-back blue blanket + centered footboard frame aligned with OBS101C!)
                        PlaceBlenderModel(bed.id == "2" ? "CTCC_LOC03_WardBedLived" : "CTCC_PROP_WardBed", root, Vector3.zero, 0f, pairRB: true);
                        Decal("BedContact_" + bed.id, "BedContact", p + new Vector3(0, 0.01f, 0), Vector3.up, 1.38f, 2.48f, 0.24f, permanent, 0.88f);

                        var cabPos = p + new Vector3(0.74f, 0, bed.yaw == 0 ? 0.66f : -0.63f);
                        var cabinetRoot = Group("Tủ đầu giường " + bed.id, ResolveParent(permanent, cabPos), cabPos);
                        cabinetRoot.localRotation = root.localRotation;
                        var cabProxy = Box("Cabinet clearance", new Vector3(0, 0.36f, 0), new Vector3(0.54f, 0.72f, 0.45f), Mat("CabinetOld"), cabinetRoot, true);
                        cabProxy.GetComponent<Renderer>().enabled = false;
                        PlaceBlenderModel("CTCC_PROP_BedsideCabinet", cabinetRoot, Vector3.zero, 0f, pairRB: true);
                        Decal("CabContact_" + bed.id, "BedContact", cabPos + new Vector3(0, 0.01f, 0), Vector3.up, 0.72f, 0.64f, 0.22f, permanent, 0.78f);

                        // Iconic Vietnamese hospital ward storytelling props with authentic R (2012 abandoned) vs B (2005 operational) contrast!
                        if (bed.id == "1")
                        {
                            // Active 2005 operational IV stand & fresh sterile care tray appear ONLY in Layer B (cleared in abandoned 2012 R!)
                            var iv1 = PlaceBlenderModel("CTCC_PROP_IVStand", report, p + new Vector3(-0.68f, 0f, 0.35f), 25f, pairRB: false);
                            staticArchitecture.Add(iv1);
                            var mat1 = PlaceBlenderModel("CTCC_PROP_WashbasinAndMat", permanent, p + new Vector3(0.05f, 0f, -0.15f), 12f, pairRB: true, castShadows: false);
                            staticArchitecture.Add(mat1);
                            PlaceBlenderModel("CTCC_PROP_CH01CareSupplies", ResolveParent(report, cabPos),
                                cabPos + root.localRotation * new Vector3(.024f, .758f, -.014f), bed.yaw + 19f, pairRB: false, castShadows: false);
                            // Classic Vietnamese hospital attendant stool tucked cleanly beside Bed 1 cabinet (clear of walkways!)
                            var stool1 = PlaceBlenderModel("CTCC_LOC03_WardStool", permanent, new Vector3(8.02f, 0f, 6.92f), 18f, pairRB: true, castShadows: false);
                            staticArchitecture.Add(stool1);
                        }
                        else if (bed.id == "2")
                        {
                            // Vacuum thermos flask (phích nước vỏ sắt) + blue-rimmed enamel mug + towel on Bed 2 cabinet!
                            PlaceBlenderModel("CTCC_PROP_ThermosAndMug", cabinetRoot, new Vector3(0f, 0.758f, 0f), 18f, pairRB: true, castShadows: false);
                            // Second rolling IV stand beside Bed 2 head
                            var iv2 = PlaceBlenderModel("CTCC_PROP_IVStand", permanent, p + new Vector3(-0.66f, 0f, 0.48f), -42f, pairRB: true);
                            staticArchitecture.Add(iv2);
                            // Iconic yellow Vietnamese honeycomb slippers (dép tổ ong) resting naturally beside Bed 2!
                            PlaceBlenderModel("CTCC_PROP_HoneycombSlippers", permanent, p + new Vector3(0.66f, 0f, -0.08f), -18f, pairRB: true, castShadows: false);
                            // Second hospital attendant stool tucked neatly under north narrow window east of Bed 2 cabinet
                            var stool2 = PlaceBlenderModel("CTCC_LOC03_WardStool", permanent, new Vector3(11.72f, 0f, 7.56f), -34f, pairRB: true, castShadows: false);
                            staticArchitecture.Add(stool2);
                        }
                        else if (bed.id == "3")
                        {
                            // Under-bed washbasin & rolled sedge mat under Bed 3
                            PlaceBlenderModel("CTCC_PROP_WashbasinAndMat", permanent, p + new Vector3(0f, 0f, 0.08f), 172f, pairRB: true, castShadows: false);
                        }
                    }
                }
                else if (art)
                {
                    // Child's smaller pair of honeycomb slippers left near Bed 4 / store edge in R (visual anchor for SFX-AN!)
                    PlaceBlenderModel("CTCC_PROP_HoneycombSlippers", real, new Vector3(10.85f, 0f, 3.85f), 125f, pairRB: false, scale: Vector3.one * 0.78f, castShadows: false);
                }

                // Period Vietnamese surface-mounted double wall outlet & rocker switch + conduit stub + bed number plaque (1-4)
                // Core v3.6.2 (DEC-V361-CH01-01 / D5): Beds 1-3 physical outlets stay on permanent; Bed 4 outlet & numbered plaques
                // Socket1..Socket4 belong exclusively to Layer R (real) for CH02 PZ01 so CH01 Layer B never leaks CH02 outlet clues!
                float back = bed.yaw == 0 ? 7.99f : 2.01f;
                float height = bed.yaw == 0 ? 0.92f : 1.31f;
                var direction = bed.yaw == 0 ? Vector3.back : Vector3.forward;
                if (art)
                {
                    bool isBed4 = bed.id == "4";
                    var outletParent = isBed4 ? real : permanent;
                    var outlet = PlaceBlenderModel("CTCC_ENV_WallOutlet", outletParent, new Vector3(bed.x, height, back), bed.yaw == 0 ? 0f : 180f, pairRB: !isBed4, castShadows: false);
                    if (!isBed4) staticArchitecture.Add(outlet);
                    Sign("Socket" + bed.id, "Socket" + bed.id, new Vector3(bed.x + 0.16f, height + 0.10f, back + direction.z * 0.015f),
                        direction, new Vector2(0.135f, 0.119f), real, metalBacking: false);
                }
                else
                {
                    Box("Outlet " + bed.id, new Vector3(bed.x, height, back), new Vector3(0.16f, 0.12f, 0.038f), Mat("Paper"), bed.id == "4" ? real : null);
                }
            }

            // Hero Rolling 2-Door Glass-and-Louver Steel Medical Cabinet blocking GATE-STORE
            var loc03Perm = ResolveParent(permanent, new Vector3(13.34f, 0, 3.62f));
            var loc03Rep = ResolveParent(report, new Vector3(12.0f, 0, 4.0f));
            var cupboard = Group("Tủ sắt kính 2 cánh · dấu bánh hướng kho", loc03Perm, new Vector3(13.34f, 0, 3.62f));
            var cabClosed = cupboard.localPosition;
            var cabOpen = cupboard.localPosition + new Vector3(0.02f, 0, 1.61f);
            if (multiScene)
            {
                zones["LOC03"].region.cabinet = cupboard;
                zones["LOC03"].region.cabinetClosed = cabClosed;
                zones["LOC03"].region.cabinetOpen = cabOpen;
            }
            else
            {
                world.cabinet = cupboard;
                world.cabinetClosed = cabClosed;
                world.cabinetOpen = cabOpen;
            }
            var cabCollider = Box("Cabinet frame", new Vector3(0, 1.04f, 0), new Vector3(0.48f, 2.06f, 1.36f), Mat("CabinetOld"), cupboard, true);
            if (art)
            {
                cabCollider.GetComponent<Renderer>().enabled = false;
                PlaceBlenderModel("CTCC_PROP_MedicalCabinet", cupboard, Vector3.zero, 0f, pairRB: true);
                Decal("MedicalCabAO", "BedContact", new Vector3(0, 0.01f, 0), Vector3.up, 0.72f, 1.60f, 0.24f, cupboard, 0.85f);
                Sign("Store", "Store", new Vector3(13.69f, 2.94f, 3.62f), Vector3.left, new Vector2(0.72f, 0.27f));
            }
            world.interactables.ConfigureCabinet(cupboard, cabClosed);

            // Store Room Dry Archive Shelving Unit (Kho giường)
            var dryShelf = Group("Tủ khô giữ đồ vật 101", loc03Perm, new Vector3(15.71f, 0, 2.61f));
            var shelfProxy = Box("Shelf base", new Vector3(0, 0.40f, 0), new Vector3(2.08f, 0.80f, 0.52f), Mat("Wood"), dryShelf, true);
            if (art)
            {
                shelfProxy.GetComponent<Renderer>().enabled = false;
                var shelf = PlaceBlenderModel("CTCC_PROP_DryShelf", dryShelf, Vector3.zero, 0f, pairRB: true);
                staticArchitecture.Add(shelf);
            }

            // F101 Branch B Modules: Denied Bay solid partition (A), Pending Privacy Curtain (B), & Refuted Destabilized Alcove (C)
            var hidden = Group("A · vách áp hồ sơ", loc03Rep);
            var pendingObj = Group("B · rèm giường 4", loc03Rep).gameObject;
            var refutedObj = Group("C · bất nhất giường 4", loc03Rep).gameObject;
            if (multiScene)
            {
                zones["LOC03"].region.deniedBay = hidden.gameObject;
                zones["LOC03"].region.pendingBay = pendingObj;
                zones["LOC03"].region.refutedBay = refutedObj;
            }
            else
            {
                world.deniedBay = hidden.gameObject;
                world.pendingBay = pendingObj;
                world.refutedBay = refutedObj;
            }
            // Keep exact collider names ("Denied bay front", "Denied bay return") queried by HospitalScene.CanSeeWardChange & InputQA!
            var deniedFrontCol = Box("Denied bay front", new Vector3(12.14f, 1.8f, 4.32f), new Vector3(3.14f, 3.6f, 0.16f), Mat("Bay"), hidden, true, tile: 2.2f);
            var deniedReturnCol = Box("Denied bay return", new Vector3(10.65f, 1.80f, 3.15f), new Vector3(0.16f, 3.6f, 2.28f), Mat("Bay"), hidden, true, tile: 2.2f);
            if (art)
            {
                // Seamless institutional split hospital partition (terrazzo cove skirting + DadoB + dado stripe + PlasterB + corner molding)
                deniedFrontCol.GetComponent<Renderer>().enabled = false;
                deniedReturnCol.GetComponent<Renderer>().enabled = false;
                Box("Denied front wainscot", new Vector3(12.14f, 0.575f, 4.32f), new Vector3(3.14f, 1.15f, 0.16f), Mat("DadoB"), hidden, false, tile: 2.4f);
                Box("Denied front plaster", new Vector3(12.14f, 2.375f, 4.32f), new Vector3(3.14f, 2.45f, 0.16f), Mat("PlasterB"), hidden, false, tile: 2.4f);
                Box("Denied front skirting", new Vector3(12.14f, 0.048f, 4.32f), new Vector3(3.18f, 0.096f, 0.19f), Mat("BaseTrim"), hidden, false, tile: 0.8f);
                Box("Denied front dado stripe", new Vector3(12.14f, 1.152f, 4.32f), new Vector3(3.16f, 0.024f, 0.176f), Mat("Frame"), hidden, false);

                Box("Denied return wainscot", new Vector3(10.65f, 0.575f, 3.15f), new Vector3(0.16f, 1.15f, 2.28f), Mat("DadoB"), hidden, false, tile: 2.4f);
                Box("Denied return plaster", new Vector3(10.65f, 2.375f, 3.15f), new Vector3(0.16f, 2.45f, 2.28f), Mat("PlasterB"), hidden, false, tile: 2.4f);
                Box("Denied return skirting", new Vector3(10.65f, 0.048f, 3.15f), new Vector3(0.19f, 0.096f, 2.32f), Mat("BaseTrim"), hidden, false, tile: 0.8f);
                Box("Denied return dado stripe", new Vector3(10.65f, 1.152f, 3.15f), new Vector3(0.176f, 0.024f, 2.30f), Mat("Frame"), hidden, false);

                Box("Denied corner guard", new Vector3(10.575f, 1.80f, 4.395f), new Vector3(0.055f, 3.60f, 0.055f), Mat("Frame"), hidden, false);
                Box("Denied ceiling cornice F", new Vector3(12.14f, 3.55f, 4.32f), new Vector3(3.18f, 0.10f, 0.21f), Mat("PlasterB"), hidden, false, tile: 2.0f);
                Box("Denied ceiling cornice R", new Vector3(10.65f, 3.55f, 3.15f), new Vector3(0.21f, 0.10f, 2.32f), Mat("PlasterB"), hidden, false, tile: 2.0f);
                Decal("DeniedFrontAO", "BedContact", new Vector3(12.14f, 0.012f, 4.44f), Vector3.up, 3.16f, 0.28f, 0.20f, hidden, 0.72f);
                Decal("DeniedReturnAO", "BedContact", new Vector3(10.53f, 0.012f, 3.15f), Vector3.up, 0.28f, 2.30f, 0.20f, hidden, 0.72f);
            }

            if (art)
            {
                // Interactive Blender Curtain Assembly for Bed 4 (track-anchored bunching + R partial curtain!)
                var bCurtain = PlaceBlenderModel("CTCC_ENV_CurtainAssembly", pendingObj.transform, new Vector3(10.68f, 2.74f, 4.27f), 0f, pairRB: false);
                RegisterCurtainMotion(bCurtain, wireInteractive: true);
                CapsuleTarget("CURTAIN-101", "Rèm che giường 4", new Vector3(12.25f, 1.50f, 4.27f), new Vector3(0.95f, 1.50f, 0.22f), pendingObj.transform);

                // Refuted (C) Layer B consequence (DEC-V361-CH01-01): wide-parted curtain + spreading damp wall + peeling plaster + CH01-PR01 crayon corner (ZERO wheel tracks in B!)
                var cCurtain = PlaceBlenderModel("CTCC_ENV_CurtainAssembly", refutedObj.transform, new Vector3(10.48f, 2.74f, 4.27f), 0f, pairRB: false, scale: new Vector3(0.18f, 1f, 1.22f));
                RegisterCurtainMotion(cCurtain, wireInteractive: false);
                Decal("RefutedBed4DampWall", "WaterRun", new Vector3(12.15f, 1.18f, 2.02f), Vector3.forward, 2.05f, 1.68f, 0.25f, refutedObj.transform, 0.92f);
                Decal("RefutedBed4PeelWall", "PaintLoss", new Vector3(11.95f, 1.52f, 2.02f), Vector3.forward, 1.48f, 1.05f, 0.25f, refutedObj.transform, 0.85f);
                Flat("CH01_PR01_CornerTrace", "CH01_PR01_Corner", new Vector3(12.62f, 0.015f, 3.18f), Vector3.up, new Vector2(0.30f, 0.30f), refutedObj.transform);
                Lamp("Refuted alcove cold highlight", LightType.Point, new Vector3(11.65f, 1.45f, 3.35f), 0.72f, 3.9f, new Color(0.60f, 0.80f, 0.88f), refutedObj.transform);

                // Permanent curtain between Bed 1 & entrance sightline (shifted neatly clear of door swing, F101 clipboard & West ceiling fan!)
                var midCurtain = PlaceBlenderModel("CTCC_ENV_CurtainAssembly", permanent, new Vector3(6.52f, 2.75f, 5.68f), 0f, pairRB: true, scale: new Vector3(0.52f, 1f, 1f));
                RegisterCurtainMotion(midCurtain, wireInteractive: false);

                // Aged tattered R curtain partially drawn near Bed 4 alcove
                var rCurtain = PlaceBlenderModel("CTCC_ENV_CurtainAssembly", real, new Vector3(10.72f, 2.74f, 4.28f), 0f, pairRB: false, forceAged: true, scale: new Vector3(0.42f, 1f, 1f));
                RegisterCurtainMotion(rCurtain, wireInteractive: false);
            }
            else
            {
                Box("Soft partition proxy", new Vector3(11.85f, 1.68f, 4.25f), new Vector3(2.38f, 1.85f, 0.035f), Mat("Bay"), pendingObj.transform);
            }

            if (!art) return;
            DetailArchitecture();
            PropDetails();
            PlaceStorytellingDecals();
        }

        private void RegisterCurtainMotion(GameObject assembly, bool wireInteractive)
        {
            var clothTf = FindDescendant(assembly.transform, "CurtainCloth");
            if (clothTf == null) return;
            var mf = clothTf.GetComponent<MeshFilter>();
            if (mf != null)
            {
                if (multiScene) ZoneForTransform(clothTf)?.cloth.Add(mf);
                else movingCloth.Add(mf);
            }
            if (wireInteractive)
            {
                var openScale = new Vector3(clothTf.localScale.x * 0.28f, clothTf.localScale.y, clothTf.localScale.z * 1.25f);
                var closedPos = clothTf.localPosition;
                var openPos = closedPos + new Vector3(-0.74f, 0f, 0f);
                if (multiScene)
                {
                    zones["LOC03"].region.curtainCloth = clothTf;
                    zones["LOC03"].region.curtainClosedScale = clothTf.localScale;
                    zones["LOC03"].region.curtainOpenScale = openScale;
                    zones["LOC03"].region.curtainClosedPos = closedPos;
                    zones["LOC03"].region.curtainOpenPos = openPos;
                    zones["LOC03"].region.curtainHasPosTrack = true;
                }
                if (world.interactables != null)
                {
                    world.interactables.ConfigureCurtain(clothTf, clothTf.localScale, openScale, closedPos, openPos, true);
                }
            }
        }

        private void DetailArchitecture()
        {
            // Corridor Reinforced-Concrete Structural Bays: 4 Chamfered Pilasters w/ Stainless Corner Guards + Beams!
            foreach (float z in new[] { 1.0f, 5.25f, 9.50f, 13.75f })
            {
                Surface("Longitudinal lintel", new Vector3(4, 3.41f, z), new Vector3(3.04f, 0.36f, 0.32f), "PlasterR", "PlasterB", tile: 2.3f, isStatic: true);
                var pil = PlaceBlenderModel("CTCC_ENV_ColumnPilaster", permanent, new Vector3(2.64f, 0f, z), 0f, pairRB: true, castShadows: false);
                staticArchitecture.Add(pil);
            }

            // Corridor West-Wall Hardwood Handrail & Stretcher Crash Bumpers (clear of z=7.50 STAFF Cross-Door!) + East-Wall Conduits
            foreach (float z in new[] { 2.60f, 11.65f })
            {
                var rail = PlaceBlenderModel("CTCC_ENV_HandrailBay", permanent, new Vector3(2.50f, 0f, z), 0f, pairRB: true, scale: new Vector3(1f, 1f, 0.85f), castShadows: false);
                staticArchitecture.Add(rail);
            }
            for (int i = 0; i < 3; i++)
            {
                float z = i * 4.22f + 1.65f;
                var cond = PlaceBlenderModel("CTCC_ENV_ConduitRun", permanent, new Vector3(5.50f, 2.86f, z), 180f, pairRB: true, castShadows: false);
                staticArchitecture.Add(cond);
            }

            // Hero Cycles-Baked Room 101 Structural Ceiling Beams, Corner Pilasters & Continuous Galvanized Conduits (replaces legacy primitive cubes!)
            var wardArch = PlaceBlenderModel("CTCC_LOC03_WardArch", permanent, new Vector3(9.70f, 0f, 5.00f), 0f, pairRB: true, castShadows: false);
            staticArchitecture.Add(wardArch);
            // DEC-V361-CH01-01 canon purity: Bed 4 vertical south-wall conduit drop exists ONLY in Layer R (meeting R-only physical WallOutlet #4!)
            PlaceBlenderModel("CTCC_LOC03_Bed4ConduitR", real, new Vector3(9.70f, 0f, 5.00f), 0f, pairRB: false, forceAged: true, castShadows: false);

            // Hero 1987 Wall-Hung Ceramic Hospital Handwashing Basin on Room 101 East Wall (physically grounds the existing 'Broken capillary line' leak!)
            var wardSink = PlaceBlenderModel("CTCC_LOC03_WardSink", permanent, new Vector3(13.68f, 0f, 6.57f), 90f, pairRB: true, castShadows: true);
            staticArchitecture.Add(wardSink);
            Box("Ward sink collider", new Vector3(13.45f, 0.78f, 6.57f), new Vector3(0.46f, 0.42f, 0.62f), Mat("Ceramic"), permanent, solid: true)
                .GetComponent<Renderer>().enabled = false;
            Decal("WardSinkFloorAO", "BedContact", new Vector3(13.44f, 0.01f, 6.57f), Vector3.up, 0.66f, 0.96f, 0.22f, permanent, 0.68f);

            // Courtyard Entry Canopy (stepped 1987 concrete portico slab seated flush on exterior wall z=-8.10 + green fascia + brick column plinths & concrete capitals)
            Box("Courtyard entry canopy", new Vector3(4, 2.85f, -9.56f), new Vector3(4.55f, 0.22f, 2.92f), Mat("BaseTrim"), tile: 2f, isStatic: true);
            Surface("Canopy fascia band", new Vector3(4, 2.94f, -11.02f), new Vector3(4.66f, 0.30f, 0.08f), "DadoR", "DadoB", tile: 2.0f, isStatic: true);
            Box("Canopy drip lip", new Vector3(4, 3.10f, -11.03f), new Vector3(4.74f, 0.055f, 0.14f), Mat("BaseTrim"), tile: 1.5f, isStatic: true);
            Box("Canopy column L", new Vector3(1.88f, 1.39f, -10.5f), new Vector3(0.29f, 2.78f, 0.29f), Mat("ExteriorFacade"), solid: true, tile: 2f, isStatic: true);
            Box("Canopy column R", new Vector3(6.12f, 1.39f, -10.5f), new Vector3(0.29f, 2.78f, 0.29f), Mat("ExteriorFacade"), solid: true, tile: 2f, isStatic: true);
            foreach (float cx in new[] { 1.88f, 6.12f })
            {
                Box($"Canopy col plinth {cx:0.0}", new Vector3(cx, 0.34f, -10.5f), new Vector3(0.36f, 0.68f, 0.36f), Mat("WeatheredBrick"), tile: 1.2f, isStatic: true);
                Box($"Canopy col cap {cx:0.0}", new Vector3(cx, 2.69f, -10.5f), new Vector3(0.37f, 0.12f, 0.37f), Mat("BaseTrim"), isStatic: true);
            }
            PlaceBlenderModel("CTCC_PROP_BulkheadLamp", permanent, new Vector3(4.0f, 2.66f, -8.12f), 0f, pairRB: false, castShadows: false);
            Box("Path edge", new Vector3(8.40f, 0.08f, -15.02f), new Vector3(4.5f, 0.17f, 3.89f), Mat("Exterior"), solid: true, isStatic: true);
            Box("Garden soil", new Vector3(8.40f, 0.182f, -15.02f), new Vector3(4.30f, 0.043f, 3.69f), Mat("Bark"), isStatic: true);
            // Inner Light Court (Sân sáng north of Room 101: x=[5.6..14.2], z=[8.1..14.1]) wet concrete paving & garden border
            Surface("Inner light court floor", new Vector3(9.9f, -0.14f, 11.1f), new Vector3(8.8f, 0.20f, 6.2f), "ConcretePavingR", "ConcretePavingB", solid: false, tile: 2.2f, isStatic: true);
            Box("Light court planter curb", new Vector3(9.9f, 0.06f, 11.6f), new Vector3(5.6f, 0.22f, 2.4f), Mat("WeatheredBrick"), isStatic: true);
            Box("Light court planter soil", new Vector3(9.9f, 0.16f, 11.6f), new Vector3(5.3f, 0.05f, 2.1f), Mat("Bark"), isStatic: true);

            // Authentic 3D Branching Blender Courtyard Trees (cây bàng / xà cừ) inside Inner Light Court & Front Courtyard
            var treeSpots = new (Vector3 pos, float yaw, float scale, bool variantA)[]
            {
                (new Vector3(8.6f, 0.12f, 11.7f), 28f, 0.72f, true),
                (new Vector3(11.6f, 0.12f, 11.5f), 142f, 0.76f, false),
                (new Vector3(8.75f, 0.14f, -14.8f), 45f, 1.05f, true),
            };
            foreach (var tr in treeSpots)
            {
                var tree = PlaceBlenderModel(tr.variantA ? "CTCC_ENV_CourtyardTree_A" : "CTCC_ENV_CourtyardTree_B",
                    permanent, tr.pos, tr.yaw, pairRB: false, scale: Vector3.one * tr.scale, castShadows: false);
                staticArchitecture.Add(tree);
            }

            // Enameled Beveled Institutional Signage (Room 101 sign sits crisply above overhead transom;
            // Emergency entrance uses the single 3D illuminated CTCC_PROP_EmergencyLightBox in DressLoc04Emergency — no duplicate flat sign!)
            Sign("Room 101 enamel", "Room101", new Vector3(5.485f, 3.08f, 4.75f), Vector3.left, new Vector2(0.68f, 0.255f));
            Sign("Phòng 101 inside", "Room101", new Vector3(5.715f, 3.06f, 4.74f), Vector3.right, new Vector2(0.54f, 0.205f));
            if (!art) Sign("Main emergency sign", "Emergency", new Vector3(4, 2.60f, 15.885f), Vector3.back, new Vector2(1.16f, 0.435f));
            Sign("Staff sign", "Staff", new Vector3(0, 2.67f, 0.29f), Vector3.back, new Vector2(0.97f, 0.365f));
            Sign("Canopy letterboard", "Welcome", new Vector3(4, 3.44f, -11.04f), Vector3.back, new Vector2(2.62f, 0.88f));
            Sign("Demolition board", "Demolition", new Vector3(6.76f, 1.20f, -20.115f), Vector3.back, new Vector2(1.48f, 0.557f), real);
            Sign("Demolition marker return", "Demolition", new Vector3(6.76f, 1.20f, -19.885f), Vector3.forward, new Vector2(1.48f, 0.557f), real);

            // Framed Vietnamese Hospital Ward Regulations Notice Board inside Room 101 south wall
            Sign("WardRulesBoard", "WardRules", new Vector3(9.65f, 1.62f, 2.02f), Vector3.forward, new Vector2(0.56f, 0.74f));

            // Hero Perforated Stainless Waiting Hall Rows in Reception (LOC02 — 5 benches, authentic Vietnamese hospital waiting area!)
            // Preserves full 2.8m main entry->corridor axis (x in [2.6..5.4]), AP02 (4,-3.3), AP03 (6.55,-4.10), STAIRWELL & corner plant!
            var loc02Benches = new (string tag, Vector3 pos, float yaw, float lenScale)[]
            {
                ("EastS",   new Vector3(8.36f, 0f, -5.50f),  0f, 1.00f),
                ("EastN",   new Vector3(8.36f, 0f, -2.98f),  0f, 1.00f),
                ("CenterE", new Vector3(6.05f, 0f, -5.65f),  0f, 0.84f),
                ("SouthE",  new Vector3(6.72f, 0f, -7.42f), 90f, 1.00f),
                ("SouthW",  new Vector3(1.26f, 0f, -7.42f), 90f, 1.00f),
            };
            foreach (var b in loc02Benches)
            {
                var sc = Mathf.Approximately(b.lenScale, 1f) ? (Vector3?)null : new Vector3(1f, 1f, b.lenScale);
                var benchGo = PlaceBlenderModel("CTCC_LOC02_WaitingBench", permanent, b.pos, b.yaw, pairRB: true, scale: sc);
                bool rotated = Mathf.Abs(b.yaw) > 45f;
                float len = 2.08f * b.lenScale;
                var colSize = rotated ? new Vector3(len, 0.76f, 0.56f) : new Vector3(0.56f, 0.76f, len);
                Box("Bench collider " + b.tag, b.pos + new Vector3(0f, 0.38f, 0f), colSize, Mat("Steel"), permanent, solid: true)
                    .GetComponent<Renderer>().enabled = false;
                staticArchitecture.Add(benchGo);
                Decal("BenchAO_" + b.tag, "BedContact", b.pos + new Vector3(0f, 0.01f, 0f), Vector3.up,
                    rotated ? len + 0.20f : 0.78f, rotated ? 0.78f : len + 0.20f, 0.22f, permanent, 0.78f);
            }

            // Corridor PA Intercom Speaker outside Room 101 (visually anchors CH01-PA01..03 broadcasts!)
            var pa = PlaceBlenderModel("CTCC_PROP_PASpeaker", permanent, new Vector3(5.50f, 2.62f, 3.35f), 90f, pairRB: true, castShadows: false);
            staticArchitecture.Add(pa);

            // Corridor Wall-Mounted Fire Extinguisher beside Pier 2
            var ext = PlaceBlenderModel("CTCC_PROP_FireExtinguisher", permanent, new Vector3(2.64f, 0.95f, 5.56f), 180f, pairRB: true, castShadows: false);
            staticArchitecture.Add(ext);

            // Medical Foot-Pedal Waste Bin inside Room 101 near entrance corner
            var bin = PlaceBlenderModel("CTCC_PROP_PedalBin", permanent, new Vector3(6.12f, 0f, 2.38f), 32f, pairRB: true, castShadows: false);
            staticArchitecture.Add(bin);
        }

        private void PlaceStorytellingDecals()
        {
            // Moisture follows the failed flashing at one jamb. The other sills drain more cleanly.
            Decal("WindowWaterRun_1", "WaterRun", new Vector3(7.61f, .69f, 8f), Vector3.back, 1.81f, 1.15f, .22f, real, .78f, -4f);
            Decal("WindowWaterRun_2", "CH01_LimeRunoff", new Vector3(10.68f, .65f, 8f), Vector3.back, .90f, .99f, .18f, real, .49f, 4f);
            // Rain leak running down corridor pilaster conduit
            Decal("CorridorPipeWater", "WaterRun", new Vector3(2.50f, 1.48f, 4.39f), Vector3.right, 1.65f, 2.45f, 0.25f, real, 0.86f);

            // 2. Glistening monsoon damp puddles on terrazzo floor beneath Room 101 north windows + corridor foot track!
            Decal("WindowFloorDamp_1", "DampFloor", new Vector3(7.45f, 0.01f, 7.35f), Vector3.up, 2.10f, 1.25f, 0.22f, real, 0.90f);
            Decal("WindowFloorDamp_2", "DampFloor", new Vector3(10.25f, 0.01f, 7.22f), Vector3.up, 2.45f, 1.45f, 0.22f, real, 0.95f);
            Decal("CorridorFloorDamp", "DampFloor", new Vector3(4.55f, 0.01f, 5.45f), Vector3.up, 1.45f, 3.65f, 0.22f, real, 0.80f);

            // 3. Blistered peeling limewash patches with raised normal relief (exposing ochre undercoat & grey mortar)
            Decal("PaintLoss_East", "PaintLoss", new Vector3(13.70f, 1.55f, 6.25f), Vector3.left, 1.65f, 1.15f, 0.25f, real, 0.92f);
            Decal("PaintLoss_South", "PaintLoss", new Vector3(8.85f, 1.48f, 2.00f), Vector3.forward, 1.45f, 1.05f, 0.25f, real, 0.85f);
            Decal("PaintLoss_Corridor", "PaintLoss", new Vector3(5.50f, 1.65f, 6.55f), Vector3.left, 1.55f, 1.25f, 0.25f, real, 0.88f);
            Decal("PaintLoss_DoorHeader", "PaintLoss", new Vector3(5.50f, 2.65f, 3.85f), Vector3.left, 1.10f, 0.85f, 0.25f, real, 0.80f);

            // 4. Capillary rising damp + chalky white efflorescence along floor skirting (authentic Vietnamese monsoon aging!)
            Decal("BaseGrime_North101", "WallBaseGrime", new Vector3(9.75f, 0.34f, 8.00f), Vector3.back, 7.60f, 0.68f, 0.25f, real, 0.85f);
            Decal("BaseGrime_East101", "WallBaseGrime", new Vector3(13.70f, 0.34f, 5.85f), Vector3.left, 3.40f, 0.68f, 0.25f, real, 0.82f);
            Decal("BaseGrime_CorridorE", "WallBaseGrime", new Vector3(5.50f, 0.34f, 2.40f), Vector3.left, 3.80f, 0.68f, 0.25f, real, 0.82f);
            Decal("BaseGrime_CorridorE2", "WallBaseGrime", new Vector3(5.50f, 0.34f, 7.60f), Vector3.left, 4.40f, 0.68f, 0.25f, real, 0.82f);

            // 5. Concentrated iron-oxide rust streaks weeping below steel window corners & Room 101 enamel plaque
            Decal("RustWindow_1", "RustRun", new Vector3(8.38f, 0.82f, 8.00f), Vector3.back, 0.32f, 0.68f, 0.25f, real, 0.90f);
            Decal("RustWindow_2", "RustRun", new Vector3(11.28f, 0.82f, 8.00f), Vector3.back, 0.32f, 0.68f, 0.25f, real, 0.90f);
            Decal("RustRoomSign", "RustRun", new Vector3(5.50f, 2.86f, 4.46f), Vector3.left, 0.22f, 0.42f, 0.25f, real, 0.82f);

            // 6. Core Gameplay Clue (CH02 Layer R PZ01 ONLY — DEC-V361-CH01-01 removes wheel marks from CH01 Layer B!):
            Decal("FourthBedWheelMarks_R", "WheelMarks", new Vector3(11.66f, 0.01f, 3.25f), Vector3.up, 1.32f, 2.48f, 0.25f, real, 0.95f);

            // 7. Upgrade #7 Dedicated Organic Weathering Decal Pass (BOTH Layer R 2012 & lived-in Layer B 1987-2005!)
            // 7a. Ceiling T8 fluorescent ballast heat halos around all 7 fixtures
            var tubeSpots = new (string tag, Vector3 pos, float roll)[]
            {
                ("HallS", new Vector3(4.00f, 3.595f, 4.75f), 0f),
                ("HallN", new Vector3(4.00f, 3.595f, 10.60f), 0f),
                ("RecW", new Vector3(2.15f, 3.595f, -3.20f), 0f),
                ("RecE", new Vector3(6.32f, 3.595f, -4.58f), 0f),
                ("R101W", new Vector3(7.70f, 3.595f, 6.55f), 90f),
                ("R101E", new Vector3(11.65f, 3.595f, 3.45f), 90f),
                ("Staff", new Vector3(0.05f, 3.595f, 3.85f), 0f),
            };
            foreach (var ts in tubeSpots)
            {
                Decal("TubeHalo_B_" + ts.tag, "TubeYellowStain", ts.pos, Vector3.down, 1.85f, 0.86f, 0.22f, report, 0.42f, ts.roll);
                Decal("TubeHalo_R_" + ts.tag, "TubeYellowStain", ts.pos, Vector3.down, 2.00f, 0.96f, 0.22f, real, 0.82f, ts.roll);
            }

            // 7b. Hospital stretcher rubber wheel drag tracks down terrazzo corridors & reception threshold
            var dragSpots = new (string tag, Vector3 pos, float w, float h, float roll)[]
            {
                ("RecEntry", new Vector3(4.00f, 0.01f, -1.85f), 1.45f, 3.20f, 2f),
                ("HallS", new Vector3(4.00f, 0.01f, 2.55f), 1.45f, 4.10f, 0f),
                ("HallMid", new Vector3(4.02f, 0.01f, 7.65f), 1.45f, 4.40f, -2f),
                ("HallN", new Vector3(3.98f, 0.01f, 12.60f), 1.45f, 4.10f, 1f),
                ("StaffLane", new Vector3(0.05f, 0.01f, 5.45f), 1.25f, 4.00f, 0f),
            };
            foreach (var ds in dragSpots)
            {
                Decal("StretcherTrack_B_" + ds.tag, "StretcherDragTrack", ds.pos, Vector3.up, ds.w, ds.h, 0.20f, report, 0.44f, ds.roll);
                Decal("StretcherTrack_R_" + ds.tag, "StretcherDragTrack", ds.pos, Vector3.up, ds.w, ds.h, 0.20f, real, 0.82f, ds.roll);
            }

            // 7c. Terrazzo wall-base rain seepage & tropical limewash damp (subtle lived-in B + heavy decayed R)
            var seepSpots = new (string tag, Vector3 pos, Vector3 n, float w, float bAlpha, float rAlpha)[]
            {
                ("RecEast", new Vector3(8.48f, 0.36f, -5.20f), Vector3.left, 4.60f, 0.34f, 0.84f),
                ("RecWest", new Vector3(-0.98f, 0.36f, -6.20f), Vector3.right, 3.40f, 0.32f, 0.82f),
                ("HallWestS", new Vector3(2.50f, 0.36f, 3.60f), Vector3.right, 4.40f, 0.36f, 0.88f),
                ("HallWestN", new Vector3(2.50f, 0.36f, 9.60f), Vector3.right, 5.20f, 0.36f, 0.88f),
                ("HallEastN", new Vector3(5.50f, 0.36f, 10.60f), Vector3.left, 4.80f, 0.34f, 0.86f),
                ("StaffWest", new Vector3(-0.98f, 0.36f, 5.20f), Vector3.right, 5.60f, 0.34f, 0.85f),
                ("R101South", new Vector3(9.75f, 0.36f, 2.00f), Vector3.forward, 6.20f, 0.32f, 0.86f),
                ("R101NorthB", new Vector3(9.75f, 0.36f, 8.00f), Vector3.back, 6.80f, 0.34f, 0f),
            };
            foreach (var sp in seepSpots)
            {
                if (sp.bAlpha > 0f)
                    Decal("TerrazzoSeep_B_" + sp.tag, "TerrazzoRainSeep", sp.pos, sp.n, sp.w, 0.72f, 0.24f, report, sp.bAlpha);
                if (sp.rAlpha > 0f)
                    Decal("TerrazzoSeep_R_" + sp.tag, "TerrazzoRainSeep", sp.pos, sp.n, sp.w, 0.76f, 0.24f, real, sp.rAlpha);
            }
        }

        private void PropDetails()
        {
            // Legacy greybox/single-scene stretcher fallback (M2 DressLoc04Emergency places high-fidelity EmergencyStretcher!)
            if (!multiScene)
            {
                var trolley = Group("Cáng kiểm kích thước", permanent, new Vector3(7.17f, 0, 19.13f));
                trolley.localRotation = Quaternion.Euler(0, 16, 0);
                PlaceBlenderModel("CTCC_PROP_TransportTrolley", trolley, Vector3.zero, 0f, pairRB: true, forceAged: true);
                Box("Trolley clearance proxy", new Vector3(0, 0.43f, 0), new Vector3(0.72f, 0.86f, 2.05f), Mat("Steel"), trolley, true)
                    .GetComponent<Renderer>().enabled = false;
            }

            // Sculpted Blender Patient Canvas Bag in Room 101 Store (Kho giường)
            PlaceBlenderModel("CTCC_PROP_PatientBag", real, new Vector3(14.95f, 0.865f, 2.65f), 24f, pairRB: false, forceAged: true);
            for (int i = 0; i < 3; i++)
                Box("Folder", new Vector3(15.59f + 0.13f * i, 0.878f + i * 0.016f, 2.51f), new Vector3(0.28f, 0.013f, 0.26f),
                    Mat(i == 0 ? "Paper" : "KraftPaper"), real);
        }

        private void MarkInteractions()
        {
            CapsuleTarget("GATE-CONG", "Cổng phụ", new Vector3(4, 1.28f, -20.12f), new Vector3(1.10f, 1.18f, 0.22f));
            CapsuleTarget("SEAM", "Thử ra cổng", new Vector3(4, 1.09f, -20.46f), new Vector3(2, 1.6f, 0.2f));
            CapsuleTarget("AN01", "Sổ trực", new Vector3(1.95f, 1.17f, -3.42f), new Vector3(0.36f, 0.23f, 0.40f));
            CapsuleTarget("BOARD", "Phiếu bàn giao 101", new Vector3(2.14f, 1.21f, -2.91f), new Vector3(0.30f, 0.30f, 0.55f));
            // Core v3.6.2 (DEC-V36-CH01-01): E10a moved from ground canvas bag to the East Gate Pillar iron mailbox (MAILBOX in DressLoc01Exterior).
            CapsuleTarget("AN02", "Điểm neo phòng 101", new Vector3(6.51f, 0.55f, 5.14f), new Vector3(0.59f, 0.45f, 0.33f));

            // Hero Blender Wooden Clipboard F101 on Room 101 Wall (yaw=-90 faces +X into Room 101 so steel spring clip & pen sit cleanly in front!)
            if (art)
            {
                PlaceBlenderModel("CTCC_PROP_ClipboardF101", permanent, new Vector3(5.72f, 1.37f, 5.50f), -90f, pairRB: false, castShadows: false);
            }
            else
            {
                var file = Group("Bảng kẹp tại phòng 101", ResolveParent(permanent, new Vector3(5.758f, 1.37f, 5.50f)), new Vector3(5.758f, 1.37f, 5.50f));
                Box("Wall board", Vector3.zero, new Vector3(0.065f, 0.42f, 0.315f), Mat("DoorWood"), file);
            }
            CapsuleTarget("F101", "Xử lý phiếu 101", new Vector3(5.811f, 1.37f, 5.50f), new Vector3(0.14f, 0.52f, 0.52f));
            CapsuleTarget("OBS101A", "Dấu sử dụng tại vị trí giường 4", new Vector3(11.05f, 0.28f, 3.35f), new Vector3(0.86f, 0.44f, 0.90f), report);
            CapsuleTarget("OBS101B", "Thẻ bàn giao giường 4 (AS-05B)", new Vector3(11.35f, 1.34f, 4.48f), new Vector3(0.42f, 0.48f, 0.22f), report);
            CapsuleTarget("OBS101C", "Phiếu chuyển giường 02 (Đã hoàn tất)", new Vector3(10.25f, 0.84f, 5.92f), new Vector3(0.42f, 0.46f, 0.28f), report);
            CapsuleTarget("LAN_TALK", "Trò chuyện với Lan", new Vector3(2.17f, 1.57f, -3.24f), new Vector3(0.32f, 0.58f, 0.60f), report);
            CapsuleTarget("OBS_CLOCK_0317", "Đồng hồ treo tường (Dừng ở 03:17)", new Vector3(-0.92f, 2.42f, -3.26f), new Vector3(0.46f, 0.80f, 0.56f), permanent);
            CapsuleTarget("LIST", "Bản kê ba giường", new Vector3(6.65f, 1.34f, 2.127f), new Vector3(0.54f, 0.58f, 0.13f), real);
            CapsuleTarget("SOCKETS", "Bốn vị trí ổ cắm", new Vector3(11.54f, 1.31f, 2.115f), new Vector3(0.53f, 0.37f, 0.18f), real);
            CapsuleTarget("WHEELS", "Dấu bánh xe", new Vector3(11.66f, 0.10f, 3.57f), new Vector3(0.89f, 0.21f, 1.05f), real);
            CapsuleTarget("PZ01", "Đối chiếu chỗ kê giường", new Vector3(13.06f, 1.28f, 3.62f), new Vector3(0.18f, 0.66f, 0.51f), real);

            var loc03Perm = ResolveParent(permanent, new Vector3(14f, 0f, 3.6f));
            var storeObj = Group("E11 · physical provenance", loc03Perm).gameObject;
            var store = storeObj.transform;
            CapsuleTarget("DRAW1", "Mảnh tranh 1", new Vector3(14.68f, 0.98f, 2.73f), new Vector3(0.27f, 0.18f, 0.2f), store);
            CapsuleTarget("DRAW2", "Mảnh tranh 2", new Vector3(15.27f, 0.98f, 2.76f), new Vector3(0.29f, 0.18f, 0.23f), store);
            CapsuleTarget("DRAW3", "Mảnh tranh 3", new Vector3(16.30f, 1.807f, 2.76f), new Vector3(0.30f, 0.20f, 0.26f), store);
            CapsuleTarget("WRIST", "Vòng C01", new Vector3(15.71f, 0.972f, 2.76f), new Vector3(0.24f, 0.21f, 0.24f), store);
            CapsuleTarget("TRANSFER", "Phiếu chuyển 101/4", new Vector3(16.13f, 0.98f, 2.75f), new Vector3(0.30f, 0.20f, 0.25f), store);
            CapsuleTarget("G02", "Cửa cấp cứu", new Vector3(4, 1.25f, 15.82f), new Vector3(1.04f, 0.88f, 0.18f));
            CapsuleTarget("G02", "Cửa cấp cứu", new Vector3(0.05f, 1.25f, 15.82f), new Vector3(1.04f, 0.88f, 0.18f));
            CapsuleTarget("EXIT", "Sổ giao ca · cấp cứu", new Vector3(2.15f, 1.05f, 20.84f), new Vector3(0.72f, 0.64f, 0.54f));

            var statusAObj = Group("Biển xác nhận", loc03Perm).gameObject;
            var statusBObj = Group("Biển chờ", loc03Perm).gameObject;
            var statusCObj = Group("Biển phản chứng", loc03Perm).gameObject;
            if (multiScene)
            {
                zones["LOC03"].region.storeClues = storeObj;
                zones["LOC03"].region.statusA = statusAObj;
                zones["LOC03"].region.statusB = statusBObj;
                zones["LOC03"].region.statusC = statusCObj;
            }
            else
            {
                world.storeClues = storeObj;
                world.statusA = statusAObj;
                world.statusB = statusBObj;
                world.statusC = statusCObj;
            }
            if (!art) return;

            world.inspectImages = Array.ConvertAll(new[]
            {
                "F101", "F101_Back", "F101_SignedA", "F101_SignedB", "F101_SignedC",
                "F101_Back_Indent_Normal", "F101_Back_Indent_Mask",
                "ObsCard", "ObsCard_Back",
                "ObsCard_Back_Indent_Normal", "ObsCard_Back_Indent_Mask",
                "Doc_Bed2Transfer", "Doc_Bed2Transfer_Back",
                "Doc_Bed3Meds", "Doc_Bed3Meds_Back",
                "Doc_ShiftGuide", "Doc_ShiftGuide_Back",
                "Doc_WardOverflow", "Doc_DutyRoster", "Calendar2005",
                "Doc_BayEnvelopeFront", "Doc_BayLetter", "Doc_BayLetter_Back",
                "Doc_BayNote", "Doc_LanLetter2005", "Doc_LanLetter2005_Back", "Doc_Newspaper2005", "Doc_KeyCTag", "Doc_PhoneScreen",
                "Doc_Ch05_PostedMap2005", "Doc_Ch05_AsBuilt2004_E05",
                "BedList", "Transfer", "Drawing",
                "DrawingPiece1", "DrawingPiece2", "DrawingPiece3"
            }, id => new InspectImage { id = id, image = Tex(id) });

            // 3D Physical Inspector / Inventory / Interactive Hardware Model Templates (hidden container cloned on demand!)
            var templates = Group("Inspect3DTemplates", world.transform);
            var envTmpl = PlaceBlenderModel("CTCC_PROP_BayEnvelope", templates, Vector3.zero, 0f, pairRB: false);
            envTmpl.SetActive(false);
            var envInteractiveTmpl = PlaceBlenderModel("CTCC_PROP_EnvelopeInteractive", templates, Vector3.zero, 0f, pairRB: false);
            envInteractiveTmpl.SetActive(false);
            var gateKeyTmpl = PlaceBlenderModel("CTCC_PROP_GateKey", templates, Vector3.zero, 0f, pairRB: false);
            gateKeyTmpl.SetActive(false);
            var wingCKeyTmpl = PlaceBlenderModel("CTCC_PROP_WingCKey", templates, Vector3.zero, 0f, pairRB: false);
            wingCKeyTmpl.SetActive(false);
            var wristTmpl = PlaceBlenderModel("CTCC_PROP_WristbandC01", templates, Vector3.zero, 0f, pairRB: false);
            wristTmpl.SetActive(false);
            var phoneTmpl = PlaceBlenderModel("CTCC_PROP_MinhPhone", templates, Vector3.zero, 0f, pairRB: false);
            var phoneScreen = Flat("PhoneScreenFace", "Doc_PhoneScreen", new Vector3(0f, 0.023f, -0.0082f), Vector3.back, new Vector2(0.041f, 0.051f), phoneTmpl.transform);
            if (phoneScreen != null) phoneScreen.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            phoneTmpl.SetActive(false);
            world.inspectModels = new[]
            {
                new InspectModel { id = "E10a", template = envTmpl },
                new InspectModel { id = "ENVELOPE_3D", template = envInteractiveTmpl },
                new InspectModel { id = "GATE_KEY", template = gateKeyTmpl },
                new InspectModel { id = "WINGC_KEY", template = wingCKeyTmpl },
                new InspectModel { id = "WRIST", template = wristTmpl },
                new InspectModel { id = "PHONE", template = phoneTmpl }
            };

            // Ground bag & envelope at (3.17, 0, -21.44) removed per Core v3.6.2 (replaced by East Gate Pillar iron mailbox).

            var notice = new Vector3(5.492f, 1.92f, 5.96f);
            Sign("Form A", "FileA", notice, Vector3.left, new Vector2(0.91f, 0.34f), statusAObj.transform);
            Sign("Form B", "FileB", notice, Vector3.left, new Vector2(0.91f, 0.34f), statusBObj.transform);
            // Subtle neutral ward-side indicator for Unverified (B): clipped provisional review slip hanging off Bed 4 chart frame
            var unvTag = Box("Unverified bed4 review tag", new Vector3(11.18f, 1.38f, 4.54f), new Vector3(0.105f, 0.135f, 0.008f), Mat("Paper"), statusBObj.transform);
            unvTag.transform.localRotation = Quaternion.Euler(0f, 145f, -4f);
            var unvClip = Box("Unverified bed4 tag clip", new Vector3(11.18f, 1.448f, 4.54f), new Vector3(0.032f, 0.018f, 0.016f), Mat("Brass"), statusBObj.transform);
            unvClip.transform.localRotation = Quaternion.Euler(0f, 145f, -4f);
            Sign("Form C", "FileC", notice, Vector3.left, new Vector2(0.91f, 0.34f), statusCObj.transform);

            // Readable world-space document sheets atop physical props (F101 wall sheet dynamically swaps on A/B/C!)
            var f101WallFlat = Flat("F101", "F101", new Vector3(5.730f, 1.364f, 5.50f), Vector3.right, new Vector2(0.222f, 0.310f));
            var f101WallMr = f101WallFlat.GetComponent<Renderer>();
            if (multiScene)
            {
                zones["LOC03"].region.f101WallRenderer = f101WallMr;
                zones["LOC03"].region.f101MatPending = Mat("F101");
                zones["LOC03"].region.f101MatA = Mat("F101_SignedA");
                zones["LOC03"].region.f101MatB = Mat("F101_SignedB");
                zones["LOC03"].region.f101MatC = Mat("F101_SignedC");
            }
            else
            {
                world.f101WallRenderer = f101WallMr;
                world.f101MatPending = Mat("F101");
                world.f101MatA = Mat("F101_SignedA");
                world.f101MatB = Mat("F101_SignedB");
                world.f101MatC = Mat("F101_SignedC");
            }
            var receptionBoard = Flat("Reception F101 sheet", "F101", new Vector3(2.12f, 1.133f, -2.90f),
                Vector3.up, new Vector2(0.25f, 0.349f));
            if (multiScene) zones["LOC02"].region.receptionBoard = receptionBoard;

            // CH01-R Grounded Bed 4 Chart Holder (hidden cleanly behind deniedBay on F101 Affirmed per D5!), Bed 4 Blanket/Slippers trace, & Bed 2 Transfer Comparison Chart!
            var loc03RepRoot = ResolveParent(report, new Vector3(11.35f, 0f, 4.48f));
            var bed4Holder = PlaceBlenderModel("CTCC_PROP_Bed4ChartHolder", loc03RepRoot, new Vector3(11.35f, 0f, 4.48f), 145f, pairRB: false);
            if (multiScene) zones["LOC03"].region.bed4ChartHolder = bed4Holder;
            else world.bed4ChartHolder = bed4Holder;
            PlaceBlenderModel("CTCC_PROP_Bed4BlanketTrace", loc03RepRoot, new Vector3(11.08f, 0f, 3.16f), -22f, pairRB: false, castShadows: false);
            Flat("OBS101C_Bed2Chart", "Doc_Bed2Transfer", new Vector3(10.25f, 0.782f, 5.972f), Vector3.back, new Vector2(0.206f, 0.272f), loc03RepRoot);

            // CH01-R Interactive Bed 3 Drawer (DRAWER-BED3 -> reveals OBS101D Bed-3 routine medication comparison record!)
            // Seated flush over BedsideCabinet's upper drawer opening (y=0.625, front z=2.615) so it reads as ONE seamless drawer!
            var bed3DrawerRoot = Group("Drawer_Bed3_Root", loc03RepRoot, new Vector3(8.64f, 0.622f, 2.502f));
            bed3DrawerRoot.localRotation = Quaternion.Euler(0f, 180f, 0f);
            PlaceBlenderModel("CTCC_LOC03_Bed3Drawer", bed3DrawerRoot, Vector3.zero, 180f, pairRB: false, scale: new Vector3(0.96f, 1.04f, 0.88f), castShadows: false);
            Flat("Bed3MedsSheet", "Doc_Bed3Meds", new Vector3(0f, -0.025f, 0.01f), Vector3.up, new Vector2(0.21f, 0.27f), bed3DrawerRoot);
            CapsuleTarget("DRAWER-BED3", "Ngăn tủ đầu giường 03", new Vector3(0f, -0.02f, -0.17f), new Vector3(0.42f, 0.14f, 0.08f), bed3DrawerRoot);
            CapsuleTarget("OBS101D", "Phiếu phát thuốc giường 03", new Vector3(0f, 0.06f, 0.02f), new Vector3(0.32f, 0.14f, 0.24f), bed3DrawerRoot);
            if (multiScene)
            {
                zones["LOC03"].region.drawerBed3 = bed3DrawerRoot;
                zones["LOC03"].region.drawerBed3ClosedPos = bed3DrawerRoot.localPosition;
                zones["LOC03"].region.drawerBed3OpenOffset = new Vector3(0f, 0f, 0.28f);
            }
            world.interactables?.ConfigureDrawer("DRAWER-BED3", bed3DrawerRoot, bed3DrawerRoot.localPosition, new Vector3(0f, 0f, 0.28f));

            // CH01-R Interactive Reception Desk Drawer (DRAWER-DESK -> reveals OBS_RECEPTION_DRAWER Shift Roster!)
            var loc02PermRoot = ResolveParent(permanent, new Vector3(1.96f, 0.66f, -5.06f));
            var deskDrawerRoot = Group("Drawer_Reception_Root", loc02PermRoot, new Vector3(1.96f, 0.66f, -5.06f));
            deskDrawerRoot.localRotation = Quaternion.Euler(0f, -90f, 0f);
            PlaceBlenderModel("CTCC_LOC02_DeskDrawer", deskDrawerRoot, Vector3.zero, 180f, pairRB: true, scale: new Vector3(0.92f, 0.92f, 0.92f), castShadows: false);
            Flat("ReceptionRosterSheet", "Doc_DutyRoster", new Vector3(0f, -0.025f, 0.01f), Vector3.up, new Vector2(0.22f, 0.28f), deskDrawerRoot);
            CapsuleTarget("DRAWER-DESK", "Ngăn kéo quầy tiếp nhận", new Vector3(0f, -0.02f, -0.17f), new Vector3(0.44f, 0.14f, 0.08f), deskDrawerRoot);
            CapsuleTarget("OBS_RECEPTION_DRAWER", "Sổ phân ca trực 17–18/11/2005", new Vector3(0f, 0.06f, 0.02f), new Vector3(0.34f, 0.14f, 0.24f), deskDrawerRoot);
            if (multiScene)
            {
                zones["LOC02"].region.drawerDesk = deskDrawerRoot;
                zones["LOC02"].region.drawerDeskClosedPos = deskDrawerRoot.localPosition;
                zones["LOC02"].region.drawerDeskOpenOffset = new Vector3(0.28f, 0f, 0f);
            }
            world.interactables?.ConfigureDrawer("DRAWER-DESK", deskDrawerRoot, deskDrawerRoot.localPosition, new Vector3(0.28f, 0f, 0f));

            // CH01-R Compact STAFF Nursing / Admin Alcove (x=0.92 against east wall -> preserves 1.72m Door-B escape lane!)
            var staffPermRoot = ResolveParent(permanent, new Vector3(0.92f, 0f, 3.65f));
            var staffRepRoot = ResolveParent(report, new Vector3(0.92f, 0f, 3.65f));
            if (art && multiScene) zones["LOC03"].region.staffChair = MakeStaffChair(staffPermRoot);
            var nursingDesk = PlaceBlenderModel("CTCC_PROP_NursingFilingDesk", staffPermRoot, new Vector3(0.92f, 0f, 3.65f), 90f, pairRB: true);
            Box("Nursing desk col", new Vector3(0.92f, 0.46f, 3.65f), new Vector3(0.36f, 0.92f, 1.18f), Mat("CabinetPaint"), staffPermRoot, solid: true).GetComponent<Renderer>().enabled = false;
            staticArchitecture.Add(nursingDesk);
            Decal("StaffDeskAO", "BedContact", new Vector3(0.84f, 0.01f, 3.52f), Vector3.up, 0.85f, 1.65f, 0.22f, staffPermRoot, 0.78f);
            // Wall light switch beside STAFF cross-door frame
            staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_WallOutlet", staffPermRoot, new Vector3(1.10f, 1.24f, 6.66f), -90f, pairRB: true, castShadows: false));
            Sign("Nursing alcove plaque", "Sign_NursingAlcove", new Vector3(1.09f, 2.50f, 3.65f), Vector3.left, new Vector2(0.84f, 0.28f), staffPermRoot, metalBacking: true);
            CapsuleTarget("OBS_CORRIDOR_NOTICE", "Thông báo kê giường tạm phòng 101", new Vector3(0.92f, 1.88f, 3.65f), new Vector3(0.32f, 0.62f, 0.64f), staffRepRoot);

            var nurseDrawerRoot = Group("Drawer_NurseDesk_Root", staffRepRoot, new Vector3(0.90f, 0.77f, 3.92f));
            nurseDrawerRoot.localRotation = Quaternion.Euler(0f, 90f, 0f);
            PlaceBlenderModel("CTCC_PROP_InteractiveDrawer", nurseDrawerRoot, Vector3.zero, 180f, pairRB: false, scale: new Vector3(0.88f, 0.88f, 0.88f), castShadows: false);
            Flat("ShiftGuideDrawerSheet", "Doc_ShiftGuide", new Vector3(0f, -0.025f, 0.01f), Vector3.up, new Vector2(0.21f, 0.27f), nurseDrawerRoot);
            CapsuleTarget("DRAWER-NURSE", "Ngăn bàn điều dưỡng", new Vector3(0f, -0.02f, -0.17f), new Vector3(0.42f, 0.14f, 0.08f), nurseDrawerRoot);
            CapsuleTarget("OBS_SHIFT_LOG", "Quy chế bàn giao ca đêm", new Vector3(0f, 0.06f, 0.02f), new Vector3(0.32f, 0.14f, 0.24f), nurseDrawerRoot);
            if (multiScene)
            {
                zones["LOC03"].region.drawerNurse = nurseDrawerRoot;
                zones["LOC03"].region.drawerNurseClosedPos = nurseDrawerRoot.localPosition;
                zones["LOC03"].region.drawerNurseOpenOffset = new Vector3(-0.28f, 0f, 0f);
            }
            world.interactables?.ConfigureDrawer("DRAWER-NURSE", nurseDrawerRoot, nurseDrawerRoot.localPosition, new Vector3(-0.28f, 0f, 0f));

            Flat("Bản kê", "BedList", new Vector3(6.65f, 1.34f, 2.025f), Vector3.forward, new Vector2(0.31f, 0.41f), real);
            Flat("Transfer paper", "Transfer", new Vector3(16.13f, 0.868f, 2.75f), Vector3.up, new Vector2(0.224f, 0.30f), store);

            var drawingSpots = new[]
            {
                new Vector3(14.68f, 0.866f, 2.73f),
                new Vector3(15.27f, 0.868f, 2.76f),
                new Vector3(16.30f, 1.744f, 2.76f)
            };
            for (int i = 0; i < drawingSpots.Length; i++)
                Flat("Drawing fragment", "DrawingPiece" + (i + 1), drawingSpots[i], Vector3.up, new Vector2(0.095f, 0.195f), store);

            // Real 3D Curled Blender Wristband C01 on dry shelf in Store!
            PlaceBlenderModel("CTCC_PROP_WristbandC01", store, new Vector3(15.71f, 0.865f, 2.76f), 32f, pairRB: false);

            // Flush inlaid tarnished brass threshold anchor strips (vạch đồng AP04 & AP02) embedded cleanly into floor tile!
            Box("Pad · brass threshold marker", new Vector3(6.52f, 0.002f, 4.95f), new Vector3(0.94f, 0.004f, 0.064f), Mat("Brass"), isStatic: true);
            Box("Pad · brass reception marker", new Vector3(3.78f, 0.002f, -3.28f), new Vector3(0.064f, 0.004f, 1.12f), Mat("Brass"), isStatic: true);
            Box("ER ledger side counter", new Vector3(2.15f, 0.91f, 21.02f), new Vector3(1.17f, 0.10f, 0.72f), Mat("Ceramic"), solid: true, isStatic: true);
            Box("Ledger", new Vector3(2.15f, 0.973f, 20.85f), new Vector3(0.41f, 0.03f, 0.27f), Mat("Paper"), isStatic: true);
        }

        private void GroupFixedSurfaces()
        {
            var roots = multiScene
                ? zones.Values.Select(z => (z.permanent, (IEnumerable<LayerFinish>)z.region.finishes)).ToArray()
                : new[] { (permanent, (IEnumerable<LayerFinish>)world.finishes) };

            foreach (var (permRoot, zoneFinishes) in roots)
            {
                var changeable = new HashSet<Renderer>(zoneFinishes.Select(x => x.renderer));
                var fixedTrim = permRoot.Cast<Transform>()
                    .Select(t => t.GetComponent<MeshRenderer>())
                    .Where(r => r != null && r.enabled && !changeable.Contains(r) && r.sharedMaterials.Length == 1 &&
                              r.sharedMaterial.renderQueue < 3000 && r.GetComponent<MeshFilter>() != null &&
                              r.GetComponentInParent<LODGroup>() == null)
                    .GroupBy(r => (r.sharedMaterial,
                        Mathf.FloorToInt(r.transform.position.x / 7.5f), Mathf.FloorToInt(r.transform.position.z / 6.5f)))
                    .Where(g => g.Count() > 1).ToArray();
                foreach (var batch in fixedTrim)
                {
                    var joins = batch.Select(r => new CombineInstance
                    {
                        mesh = r.GetComponent<MeshFilter>().sharedMesh,
                        transform = r.localToWorldMatrix,
                        subMeshIndex = 0
                    }).ToArray();
                    var merged = new Mesh { name = "Fixed trim · " + batch.Key.Item1.name };
                    merged.CombineMeshes(joins);
                    StoreMesh(merged);
                    var item = Group(merged.name, permRoot).gameObject;
                    item.AddComponent<MeshFilter>().sharedMesh = merged;
                    var mr = item.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = batch.Key.Item1;
                    mr.receiveGI = ReceiveGI.LightProbes;
                    GameObjectUtility.SetStaticEditorFlags(item, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ContributeGI);
                    foreach (var r in batch) r.enabled = false;
                }
            }
        }
    }
}
