using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
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
            Lit("BaseTrim", 0.56f, 0.58f, 0.54f, maps: "FloorR", tint: true, bump: 0.45f, ao: 0.75f);

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
            Lit("Brass", 0.92f, 0.75f, 0.40f, maps: "Steel", tint: true, bump: 0.35f, ao: 0.72f);
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
            Lit("WeatheredBrick", 1f, 1f, 1f, maps: "WeatheredBrick", bump: 0.78f, ao: 0.85f);
            Lit("TerrazzoBench", 1f, 1f, 1f, maps: "TerrazzoBench", bump: 0.38f, ao: 0.76f);
            Lit("MotorbikePaint", 0.24f, 0.055f, 0.068f, metal: 0.48f, smooth: 0.78f);

            Lit("CrimsonGlow", 0.88f, 0.14f, 0.12f, smooth: 0.60f);
            Mat("CrimsonGlow").SetColor("_EmissionColor", new Color(0.95f, 0.16f, 0.12f) * 2.6f);
            Mat("CrimsonGlow").EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(Mat("CrimsonGlow"));

            Lit("WarmGlow", 0.98f, 0.80f, 0.48f, smooth: 0.70f);
            Mat("WarmGlow").SetColor("_EmissionColor", new Color(1.0f, 0.78f, 0.42f) * 2.8f);
            Mat("WarmGlow").EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(Mat("WarmGlow"));

            Lit("MonitorScreen", 1f, 1f, 1f, smooth: 0.65f);
            if (art)
            {
                Mat("MonitorScreen").SetTexture("_BaseMap", Tex("MonitorECG"));
                Mat("MonitorScreen").SetTexture("_EmissionMap", Tex("MonitorECG"));
                Mat("MonitorScreen").SetColor("_EmissionColor", new Color(0.68f, 1.0f, 0.82f) * 1.35f);
                Mat("MonitorScreen").EnableKeyword("_EMISSION");
                EditorUtility.SetDirty(Mat("MonitorScreen"));
            }

            // Character PBR Materials (Lan, Gatekeeper, Bé An)
            Lit("NurseBlouse", 1f, 1f, 1f, maps: "NurseBlouse", bump: 0.26f, ao: 0.72f);
            Lit("NurseTrim", 0.24f, 0.45f, 0.44f, smooth: 0.20f);
            Lit("DarkTrouser", 1f, 1f, 1f, maps: "DarkTrouser", bump: 0.32f, ao: 0.76f);
            Lit("LanSkin", 1f, 1f, 1f, maps: "LanSkin", bump: 0.18f, ao: 0.65f);
            Lit("LanLip", 0.60f, 0.34f, 0.31f, smooth: 0.42f);
            Lit("LanHair", 1f, 1f, 1f, maps: "LanHair", bump: 0.40f, ao: 0.75f);
            Lit("LanEyeWhite", 0.88f, 0.86f, 0.84f, smooth: 0.82f);
            Lit("LanIris", 0.14f, 0.09f, 0.06f, smooth: 0.88f);
            Lit("LanBadgeMat", 1f, 1f, 1f, smooth: 0.50f);
            if (art)
            {
                Mat("LanBadgeMat").SetTexture("_BaseMap", Tex("LanBadge"));
                EditorUtility.SetDirty(Mat("LanBadgeMat"));
            }
            Lit("CharredBlouse", 1f, 1f, 1f, maps: "CharredBlouse", bump: 0.55f, ao: 0.84f);
            Lit("ObscuredFace", 1f, 1f, 1f, maps: "ObscuredFace", bump: 0.28f, ao: 0.85f);
            Lit("AnCotton", 1f, 1f, 1f, maps: "AnCotton", bump: 0.28f, ao: 0.70f);
            Lit("AnSkin", 0.76f, 0.68f, 0.62f, smooth: 0.26f);

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
                    "F101", "ObsCard", "BedList", "Transfer", "Drawing", "WardRules", "ChartCard",
                    "Sign_EmergencyBox", "Sign_KhuBClosed", "Sign_ReceptionWindow", "Sign_HospitalGate",
                    "Calendar2005", "MonitorECG"
                })
                {
                    var m = Lit(name, 1f, 1f, 1f, smooth: name is "Room101" or "Welcome" or "Emergency" or "Sign_HospitalGate" or "Sign_ReceptionWindow" ? 0.42f : 0.16f, twoSided: true);
                    m.SetTexture("_BaseMap", Tex(name));
                    m.SetColor("_BaseColor", Color.white);
                    if (name == "Sign_EmergencyBox")
                    {
                        m.SetTexture("_EmissionMap", Tex(name));
                        m.SetColor("_EmissionColor", new Color(0.95f, 0.24f, 0.18f) * 1.85f);
                        m.EnableKeyword("_EMISSION");
                    }
                    EditorUtility.SetDirty(m);
                }

                foreach (string name in new[] { "DrawingPiece1", "DrawingPiece2", "DrawingPiece3", "ClockB", "ClockR" })
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
                beam.SetColor("_Tint", new Color(0.42f, 0.58f, 0.68f, 0.125f));
                beam.SetFloat("_MoteStrength", 0.48f);
                EditorUtility.SetDirty(beam);
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
                bool mask = path.EndsWith("_Mask.png", StringComparison.Ordinal);
                bool repeat = path.EndsWith("_Color.png", StringComparison.Ordinal) || normal || mask;
                bool leaf = path.EndsWith("TropicalLeaf_Color.png", StringComparison.Ordinal) ||
                            path.EndsWith("TropicalShrub_Color.png", StringComparison.Ordinal);
                string family = Path.GetFileNameWithoutExtension(path).Split('_')[0];
                bool stream = repeat && !leaf && family is not ("WaterRun" or "PaintLoss" or "DampFloor" or "RustRun" or "WallBaseGrime" or "LanSkin" or "LanHair" or "ObscuredFace");
                var wantedType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                var wrap = repeat && !leaf && family != "LanSkin" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                bool wantAlpha = !repeat || leaf;
                if (importer.textureType == wantedType && importer.sRGBTexture == !mask &&
                    importer.wrapMode == wrap && importer.anisoLevel == 8 && importer.maxTextureSize == 2048 &&
                    importer.alphaIsTransparency == wantAlpha && importer.streamingMipmaps == stream)
                    continue;
                importer.textureType = wantedType;
                importer.sRGBTexture = !mask;
                importer.wrapMode = wrap;
                importer.maxTextureSize = 2048;
                importer.anisoLevel = 8;
                importer.alphaIsTransparency = wantAlpha;
                importer.streamingMipmaps = stream;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
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
                    if (importer.importBlendShapes && importer.importAnimation &&
                        importer.animationType == ModelImporterAnimationType.Generic &&
                        importer.importTangents == ModelImporterTangents.CalculateMikk)
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
                    // Loop all embedded character clips smoothly (Idle, StalkWalk, TalkGesture, etc.)
                    var defaultClips = importer.defaultClipAnimations;
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
                ("WheelMarks", null, 0f),
                ("BedContact", null, 0f),
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
            m.SetInteger("_ZWrite", 0);
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
            // Blender characters face -Y (in Unity local -Z after FBX import), so 180 yaw faces +Z forward cleanly!
            inst.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.updateWhenOffscreen = true;
                smr.shadowCastingMode = isApparition ? ShadowCastingMode.Off : ShadowCastingMode.On;
                var orig = smr.sharedMaterials;
                var remapped = new Material[orig.Length];
                for (int i = 0; i < orig.Length; i++)
                {
                    if (isApparition)
                        remapped[i] = Mat("AnApparitionMat");
                    else
                    {
                        string raw = orig[i] != null ? CleanMatName(orig[i].name) : "NurseBlouse";
                        remapped[i] = Mat(raw);
                    }
                }
                smr.sharedMaterials = remapped;
            }

            var clips = AssetDatabase.LoadAllAssetsAtPath(fbxPath)
                .OfType<AnimationClip>()
                .Where(c => c != null && !c.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();

            var actor = holder.AddComponent<CharacterActor>();
            actor.characterId = charId;
            actor.clips = clips;
            actor.isApparition = isApparition;
            actor.enableBlink = enableBlink;
            actor.enableLipSync = enableLipSync;
            actor.enableLookAt = true;
            return actor;
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

        private static string ReportKey(string key) => key switch
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
            _ => key
        };

        private static string AgedKey(string reportKey) => reportKey switch
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
            _ => reportKey
        };

        private void BuildFurnishing()
        {
            // Reception & Duty Desk (LOC02)
            var desk = Group("Bàn trực · quầy hai cao độ", ResolveParent(permanent, new Vector3(1.45f, 0, -3.24f)), new Vector3(1.45f, 0, -3.24f));
            Box("Terrazzo plinth", new Vector3(0, 0.06f, 0), new Vector3(1.35f, 0.12f, 3.88f), Mat("BaseTrim"), desk, true, isStatic: true);
            Surface("Counter apron", new Vector3(1.45f, 0.55f, -3.24f), new Vector3(1.25f, 0.98f, 3.8f), "DadoR", "DadoB", true, 1.8f, isStatic: true);
            Box("Wide counter top", new Vector3(0, 1.08f, 0), new Vector3(1.48f, 0.095f, 4f), Mat("Ceramic"), desk, tile: 1.2f, isStatic: true);
            if (art)
            {
                // Grounded lower accessible reception counter wing with matching wainscot apron & plinth
                Box("Lower accessible apron", new Vector3(0.05f, 0.37f, -2.32f), new Vector3(1.16f, 0.74f, 0.64f), Mat("DadoR"), desk, true, tile: 1.8f, isStatic: true);
                Box("Lower accessible end", new Vector3(0.05f, 0.78f, -2.32f), new Vector3(1.25f, 0.07f, 0.71f), Mat("Ceramic"), desk, true, isStatic: true);
                // Blender Reception Props: open duty ledger + vintage landline phone + green desk lamp + folder tray!
                var recProps = PlaceBlenderModel("CTCC_PROP_ReceptionProps", desk, new Vector3(0.32f, 1.128f, -0.18f), 0f, pairRB: true, castShadows: false);
                staticArchitecture.Add(recProps);
                Decal("DeskContactAO", "BedContact", new Vector3(1.45f, 0.01f, -3.24f), Vector3.up, 1.85f, 4.35f, 0.25f, permanent, 0.80f);

                Sign("Quầy", "Station", new Vector3(2.085f, 0.67f, -3.35f), Vector3.right, new Vector2(0.93f, 0.35f));
                Sign("Foundation plaque", "Welcome", new Vector3(-0.985f, 2.1f, -3.1f), Vector3.right, new Vector2(1.34f, 0.52f));

                // Real 3D Blender Wall Clock with separate R (11:52) and B (03:17) hands!
                var clock = PlaceBlenderModel("CTCC_PROP_WallClock", permanent, new Vector3(-0.99f, 2.74f, -3.26f), 90f, pairRB: true, castShadows: false);
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
                var dial = Flat("Clock dial face", "ClockR", new Vector3(-0.955f, 2.74f, -3.26f), Vector3.right, new Vector2(0.34f, 0.34f), permanent);
                AddFinish(new LayerFinish { renderer = dial.GetComponent<Renderer>(), real = Mat("ClockR"), report = Mat("ClockB") });
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
                        PlaceBlenderModel("CTCC_PROP_WardBed", root, Vector3.zero, 0f, pairRB: true);
                        Decal("BedContact_" + bed.id, "BedContact", p + new Vector3(0, 0.01f, 0), Vector3.up, 1.38f, 2.48f, 0.24f, permanent, 0.88f);

                        var cabPos = p + new Vector3(0.74f, 0, bed.yaw == 0 ? 0.66f : -0.63f);
                        var cabinetRoot = Group("Tủ đầu giường " + bed.id, ResolveParent(permanent, cabPos), cabPos);
                        cabinetRoot.localRotation = root.localRotation;
                        var cabProxy = Box("Cabinet clearance", new Vector3(0, 0.36f, 0), new Vector3(0.54f, 0.72f, 0.45f), Mat("CabinetOld"), cabinetRoot, true);
                        cabProxy.GetComponent<Renderer>().enabled = false;
                        PlaceBlenderModel("CTCC_PROP_BedsideCabinet", cabinetRoot, Vector3.zero, 0f, pairRB: true);
                        Decal("CabContact_" + bed.id, "BedContact", cabPos + new Vector3(0, 0.01f, 0), Vector3.up, 0.72f, 0.64f, 0.22f, permanent, 0.78f);

                        // Iconic Vietnamese hospital ward storytelling props!
                        if (bed.id == "1")
                        {
                            // Rolling stainless IV stand beside Bed 1 + under-bed washbasin & rolled sedge mat (chiếu cói)
                            var iv1 = PlaceBlenderModel("CTCC_PROP_IVStand", permanent, p + new Vector3(-0.68f, 0f, 0.35f), 25f, pairRB: true);
                            staticArchitecture.Add(iv1);
                            var mat1 = PlaceBlenderModel("CTCC_PROP_WashbasinAndMat", permanent, p + new Vector3(0.05f, 0f, -0.15f), 12f, pairRB: true, castShadows: false);
                            staticArchitecture.Add(mat1);
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
                float back = bed.yaw == 0 ? 7.99f : 2.01f;
                float height = bed.yaw == 0 ? 0.92f : 1.31f;
                var direction = bed.yaw == 0 ? Vector3.back : Vector3.forward;
                if (art)
                {
                    var outlet = PlaceBlenderModel("CTCC_ENV_WallOutlet", permanent, new Vector3(bed.x, height, back), bed.yaw == 0 ? 0f : 180f, pairRB: true, castShadows: false);
                    staticArchitecture.Add(outlet);
                    Sign("Bed outlet number", "Socket" + bed.id, new Vector3(bed.x + 0.16f, height + 0.10f, back + direction.z * 0.015f),
                        direction, new Vector2(0.135f, 0.119f), permanent, metalBacking: false);
                }
                else
                {
                    Box("Outlet " + bed.id, new Vector3(bed.x, height, back), new Vector3(0.16f, 0.12f, 0.038f), Mat("Paper"));
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

            // F101 Branch B Modules: Denied Bay solid partition (A) & Pending Privacy Curtain (B)
            var hidden = Group("A · vách áp hồ sơ", loc03Rep);
            var pendingObj = Group("B · rèm giường 4", loc03Rep).gameObject;
            if (multiScene)
            {
                zones["LOC03"].region.deniedBay = hidden.gameObject;
                zones["LOC03"].region.pendingBay = pendingObj;
            }
            else
            {
                world.deniedBay = hidden.gameObject;
                world.pendingBay = pendingObj;
            }
            Box("Denied bay front", new Vector3(12.14f, 1.8f, 4.32f), new Vector3(3.14f, 3.6f, 0.16f), Mat("Bay"), hidden, true, tile: 2.2f);
            Box("Denied bay return", new Vector3(10.65f, 1.80f, 3.15f), new Vector3(0.16f, 3.6f, 2.28f), Mat("Bay"), hidden, true, tile: 2.2f);

            if (art)
            {
                // Interactive Blender Curtain Assembly for Bed 4 (both B pending curtain and R partial curtain!)
                var bCurtain = PlaceBlenderModel("CTCC_ENV_CurtainAssembly", pendingObj.transform, new Vector3(10.68f, 2.74f, 4.27f), 0f, pairRB: false);
                RegisterCurtainMotion(bCurtain, wireInteractive: true);
                CapsuleTarget("CURTAIN-101", "Rèm che giường 4", new Vector3(11.85f, 1.55f, 4.27f), new Vector3(2.1f, 1.6f, 0.35f), pendingObj.transform);

                // Permanent curtain between Bed 1 & entrance sightline (width 1.25 m)
                var midCurtain = PlaceBlenderModel("CTCC_ENV_CurtainAssembly", permanent, new Vector3(6.58f, 2.75f, 5.32f), 0f, pairRB: true, scale: new Vector3(0.54f, 1f, 1f));
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
                if (multiScene)
                {
                    zones["LOC03"].region.curtainCloth = clothTf;
                    zones["LOC03"].region.curtainClosedScale = clothTf.localScale;
                    zones["LOC03"].region.curtainOpenScale = openScale;
                }
                if (world.interactables != null)
                {
                    world.interactables.curtainCloth = clothTf;
                    world.interactables.curtainClosedScale = clothTf.localScale;
                    world.interactables.curtainOpenScale = openScale;
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

            // Corridor West-Wall Hardwood Handrail & Stretcher Crash Bumpers + East-Wall Galvanized Conduits & Junction Boxes!
            for (int i = 0; i < 3; i++)
            {
                float z = i * 4.22f + 1.65f;
                var rail = PlaceBlenderModel("CTCC_ENV_HandrailBay", permanent, new Vector3(2.50f, 0f, z), 0f, pairRB: true, castShadows: false);
                staticArchitecture.Add(rail);
                var cond = PlaceBlenderModel("CTCC_ENV_ConduitRun", permanent, new Vector3(5.50f, 2.86f, z), 180f, pairRB: true, castShadows: false);
                staticArchitecture.Add(cond);
            }

            // Deep Structural Reinforced-Concrete Beam Spanning Room 101 Ceiling (shared R/B landmark!)
            Surface("Deep beam spanning 101", new Vector3(9.77f, 3.40f, 5.00f), new Vector3(0.32f, 0.40f, 6.02f), "PlasterR", "PlasterB", tile: 2.2f, isStatic: true);
            // Perimeter ceiling cornice beams in Room 101
            Surface("North ring beam", new Vector3(9.75f, 3.46f, 7.98f), new Vector3(8.0f, 0.28f, 0.24f), "PlasterR", "PlasterB", tile: 2.2f, isStatic: true);
            Surface("South ring beam", new Vector3(9.75f, 3.46f, 2.02f), new Vector3(8.0f, 0.28f, 0.24f), "PlasterR", "PlasterB", tile: 2.2f, isStatic: true);

            // Courtyard Entry Canopy, Columns, Walkway & North Courtyard Ground
            Box("Courtyard entry canopy", new Vector3(4, 2.85f, -9.45f), new Vector3(4.55f, 0.22f, 3.13f), Mat("Exterior"), tile: 2f, isStatic: true);
            Box("Canopy column L", new Vector3(1.88f, 1.39f, -10.5f), new Vector3(0.29f, 2.78f, 0.29f), Mat("PlasterR"), solid: true, tile: 2f, isStatic: true);
            Box("Canopy column R", new Vector3(6.12f, 1.39f, -10.5f), new Vector3(0.29f, 2.78f, 0.29f), Mat("PlasterR"), solid: true, tile: 2f, isStatic: true);
            PlaceBlenderModel("CTCC_PROP_BulkheadLamp", permanent, new Vector3(4.0f, 2.66f, -8.12f), 0f, pairRB: false, castShadows: false);
            Box("Path edge", new Vector3(8.40f, 0.08f, -15.02f), new Vector3(4.5f, 0.17f, 3.89f), Mat("Exterior"), solid: true, isStatic: true);
            Box("Garden soil", new Vector3(8.40f, 0.182f, -15.02f), new Vector3(4.30f, 0.043f, 3.69f), Mat("Bark"), isStatic: true);
            Box("North exterior ground", new Vector3(11, -0.24f, 11.5f), new Vector3(16, 0.20f, 8.5f), Mat("Exterior"), tile: 2f, isStatic: true);
            // Low north garden perimeter brick wall so exterior feels enclosed & realistic from Room 101 windows
            Box("North yard wall", new Vector3(10.5f, 0.85f, 15.2f), new Vector3(16f, 2.1f, 0.25f), Mat("Exterior"), tile: 2.2f, isStatic: true);

            // Authentic 3D Branching Blender Courtyard Trees (cây bàng / xà cừ) clear of HALL & Room 101 north wall (z >= 12.2!)
            var treeSpots = new (Vector3 pos, float yaw, float scale, bool variantA)[]
            {
                (new Vector3(8.3f, -0.15f, 12.6f), 28f, 0.82f, true),
                (new Vector3(11.1f, -0.15f, 13.1f), 142f, 0.88f, false),
                (new Vector3(14.0f, -0.15f, 12.4f), 252f, 0.84f, true),
                (new Vector3(16.4f, -0.15f, 13.5f), 74f, 0.90f, false),
                (new Vector3(8.75f, 0.14f, -14.8f), 45f, 1.05f, true),
            };
            foreach (var tr in treeSpots)
            {
                var tree = PlaceBlenderModel(tr.variantA ? "CTCC_ENV_CourtyardTree_A" : "CTCC_ENV_CourtyardTree_B",
                    permanent, tr.pos, tr.yaw, pairRB: false, scale: Vector3.one * tr.scale, castShadows: false);
                staticArchitecture.Add(tree);
            }

            // Enameled Beveled Institutional Signage (Room 101 sign sits crisply above overhead transom!)
            Sign("Room 101 enamel", "Room101", new Vector3(5.485f, 3.08f, 4.75f), Vector3.left, new Vector2(0.68f, 0.255f));
            Sign("Phòng 101 inside", "Room101", new Vector3(5.715f, 3.06f, 4.74f), Vector3.right, new Vector2(0.54f, 0.205f));
            Sign("Main emergency sign", "Emergency", new Vector3(4, 2.60f, 15.885f), Vector3.back, new Vector2(1.16f, 0.435f));
            Sign("Staff sign", "Staff", new Vector3(0, 2.67f, 0.29f), Vector3.back, new Vector2(0.97f, 0.365f));
            Sign("Canopy letterboard", "Welcome", new Vector3(4, 3.25f, -8.105f), Vector3.back, new Vector2(2.62f, 0.90f));
            Sign("Demolition board", "Demolition", new Vector3(6.76f, 1.20f, -20.115f), Vector3.back, new Vector2(1.48f, 0.557f), real);
            Sign("Demolition marker return", "Demolition", new Vector3(6.76f, 1.20f, -19.885f), Vector3.forward, new Vector2(1.48f, 0.557f), real);

            // Framed Vietnamese Hospital Ward Regulations Notice Board inside Room 101 south wall
            Sign("WardRulesBoard", "WardRules", new Vector3(9.65f, 1.62f, 2.02f), Vector3.forward, new Vector2(0.56f, 0.74f));

            // 4-Seat Steel Corridor Waiting Bench in Reception (LOC02) + Second Bench in Corridor
            var bench1 = PlaceBlenderModel("CTCC_PROP_WaitingBench", permanent, new Vector3(7.24f, 0f, -5.12f), 0f, pairRB: true);
            Box("Bench collider", new Vector3(7.24f, 0.38f, -5.12f), new Vector3(0.54f, 0.76f, 2.12f), Mat("Steel"), permanent, solid: true).GetComponent<Renderer>().enabled = false;
            staticArchitecture.Add(bench1);
            Decal("Bench1AO", "BedContact", new Vector3(7.24f, 0.01f, -5.12f), Vector3.up, 0.78f, 2.30f, 0.22f, permanent, 0.80f);

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
            // 1. Gravity rainwater & tropical algae streaks directly beneath ALL 3 Room 101 north window sills (R 2012)
            Decal("WindowWaterRun_1", "WaterRun", new Vector3(7.35f, 0.62f, 8.00f), Vector3.back, 2.15f, 1.08f, 0.25f, real, 0.92f);
            Decal("WindowWaterRun_2", "WaterRun", new Vector3(10.28f, 0.60f, 8.00f), Vector3.back, 2.10f, 1.12f, 0.25f, real, 0.96f);
            Decal("WindowWaterRun_3", "WaterRun", new Vector3(12.39f, 0.65f, 8.00f), Vector3.back, 1.32f, 1.04f, 0.25f, real, 0.88f);
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

            // 6. Core Gameplay Clue: Crisp rubber caster indentation & drag trails from Bed 4 leading straight to MedicalCabinet!
            Decal("FourthBedWheelMarks_R", "WheelMarks", new Vector3(11.66f, 0.01f, 3.25f), Vector3.up, 1.32f, 2.48f, 0.25f, real, 0.95f);
            Decal("FourthBedReportTrace_B", "WheelMarks", new Vector3(10.87f, 0.01f, 4.44f), Vector3.up, 0.82f, 0.42f, 0.25f, report, 0.85f);
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
            PlaceBlenderModel("CTCC_PROP_PatientBag", real, new Vector3(14.95f, 0.865f, 2.65f), 24f, pairRB: false);
            for (int i = 0; i < 3; i++)
                Box("Folder", new Vector3(15.59f + 0.13f * i, 0.878f + i * 0.016f, 2.51f), new Vector3(0.28f, 0.013f, 0.26f),
                    Mat(i == 0 ? "Paper" : "KraftPaper"), real);
        }

        private void MarkInteractions()
        {
            CapsuleTarget("GATE-CONG", "Cổng phụ", new Vector3(4, 1.28f, -20.12f), new Vector3(1.10f, 1.18f, 0.22f));
            CapsuleTarget("SEAM", "Thử ra cổng", new Vector3(4, 1.09f, -20.46f), new Vector3(2, 1.6f, 0.2f));
            CapsuleTarget("AN01", "Sổ trực", new Vector3(2.18f, 1.20f, -3.73f), new Vector3(0.28f, 0.28f, 0.48f));
            CapsuleTarget("BOARD", "Phiếu bàn giao 101", new Vector3(2.14f, 1.21f, -2.91f), new Vector3(0.30f, 0.30f, 0.55f));
            CapsuleTarget("E10a", "Bì thư của Bảy", new Vector3(3.17f, 0.94f, -21.44f), new Vector3(0.43f, 0.49f, 0.44f));
            CapsuleTarget("AN02", "Điểm neo phòng 101", new Vector3(6.51f, 0.55f, 5.14f), new Vector3(0.59f, 0.45f, 0.33f));

            // Hero Blender Wooden Clipboard F101 on Room 101 Wall
            if (art)
            {
                PlaceBlenderModel("CTCC_PROP_ClipboardF101", permanent, new Vector3(5.72f, 1.37f, 5.50f), 90f, pairRB: false, castShadows: false);
            }
            else
            {
                var file = Group("Bảng kẹp tại phòng 101", ResolveParent(permanent, new Vector3(5.758f, 1.37f, 5.50f)), new Vector3(5.758f, 1.37f, 5.50f));
                Box("Wall board", Vector3.zero, new Vector3(0.065f, 0.42f, 0.315f), Mat("DoorWood"), file);
            }
            CapsuleTarget("F101", "Xử lý phiếu 101", new Vector3(5.811f, 1.37f, 5.50f), new Vector3(0.14f, 0.52f, 0.52f));
            CapsuleTarget("OBS101A", "Vết bánh và ổ cắm thứ tư", new Vector3(10.92f, 0.096f, 4.48f), new Vector3(0.62f, 0.19f, 0.71f), report);
            CapsuleTarget("OBS101B", "Thẻ bàn giao giường 4", new Vector3(11.35f, 1.41f, 4.453f), new Vector3(0.44f, 0.47f, 0.104f), report);
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
            CapsuleTarget("EXIT", "Sổ giao ca · cấp cứu", new Vector3(4, 1.05f, 20.84f), new Vector3(0.72f, 0.64f, 0.54f));

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
                "F101", "ObsCard", "BedList", "Transfer", "Drawing",
                "DrawingPiece1", "DrawingPiece2", "DrawingPiece3"
            }, id => new InspectImage { id = id, image = Tex(id) });

            // 3D Physical Inspector Model Templates (hidden container cloned on demand by PhysicalInspector!)
            var templates = Group("Inspect3DTemplates", world.transform);
            var envTmpl = PlaceBlenderModel("CTCC_PROP_BayEnvelope", templates, Vector3.zero, 0f, pairRB: false);
            envTmpl.SetActive(false);
            var wristTmpl = PlaceBlenderModel("CTCC_PROP_WristbandC01", templates, Vector3.zero, 0f, pairRB: false);
            wristTmpl.SetActive(false);
            world.inspectModels = new[]
            {
                new InspectModel { id = "E10a", template = envTmpl },
                new InspectModel { id = "WRIST", template = wristTmpl }
            };

            // Bảy's Kraft Envelope + Keys resting on canvas bag beside exterior gate
            PlaceBlenderModel("CTCC_PROP_PatientBag", permanent, new Vector3(3.17f, 0.0f, -21.44f), -28f, pairRB: false);
            PlaceBlenderModel("CTCC_PROP_BayEnvelope", permanent, new Vector3(3.17f, 0.22f, -21.44f), 14f, pairRB: false);

            var notice = new Vector3(5.492f, 1.92f, 5.96f);
            Sign("Form A", "FileA", notice, Vector3.left, new Vector2(0.91f, 0.34f), statusAObj.transform);
            Sign("Form B", "FileB", notice, Vector3.left, new Vector2(0.91f, 0.34f), statusBObj.transform);
            Sign("Form C", "FileC", notice, Vector3.left, new Vector2(0.91f, 0.34f), statusCObj.transform);

            // Readable world-space document sheets atop physical props
            Flat("F101", "F101", new Vector3(5.736f, 1.365f, 5.50f), Vector3.right, new Vector2(0.222f, 0.310f));
            Flat("Spare signed board", "F101", new Vector3(2.12f, 1.133f, -2.90f), Vector3.up, new Vector2(0.25f, 0.349f));
            Flat("OBS101B", "ObsCard", new Vector3(11.35f, 1.41f, 4.461f), Vector3.forward, new Vector2(0.162f, 0.22f), report);
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

            // Brass threshold anchor strip (vạch đồng AP04) embedded into Room 101 floor!
            Box("Pad · brass threshold marker", new Vector3(6.52f, 0.008f, 4.95f), new Vector3(0.96f, 0.014f, 0.092f), Mat("Brass"), isStatic: true);
            // Brass reception anchor strip (vạch đồng AP02) in front of reception counter!
            Box("Pad · brass reception marker", new Vector3(3.78f, 0.008f, -3.28f), new Vector3(0.092f, 0.014f, 1.15f), Mat("Brass"), isStatic: true);
            Box("Hub ledger ledge", new Vector3(4, 0.91f, 21.02f), new Vector3(1.17f, 0.10f, 0.72f), Mat("Ceramic"), solid: true, isStatic: true);
            Box("Ledger", new Vector3(4, 0.973f, 20.85f), new Vector3(0.41f, 0.03f, 0.27f), Mat("Paper"), isStatic: true);
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
