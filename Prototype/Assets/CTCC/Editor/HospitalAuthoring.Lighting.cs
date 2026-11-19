using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace CTCC.Editor
{
    public sealed partial class HospitalAuthoring
    {
        private void LightAndStage()
        {
            ConfigureUrpPipeline();

            // Clear Unity default bright procedural skybox so dark hospital reflections NEVER glow white!
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.17f, 0.24f, 0.29f);
            RenderSettings.ambientEquatorColor = new Color(0.115f, 0.155f, 0.175f);
            RenderSettings.ambientGroundColor = new Color(0.068f, 0.064f, 0.055f);
            RenderSettings.fog = art;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0155f;
            RenderSettings.reflectionIntensity = 0.78f;

            // Camera stays in Bootstrap persistent scene (positioned at AP00)
            Vector3 ap00Pos = layout.pads.Single(p => p.id == "AP00") is var p0 ? new Vector3(p0.x, 0.015f, p0.z) : new Vector3(4f, 0.015f, -21.2f);
            var rig = Group("Seeded player view", world.transform, ap00Pos + Vector3.up * 1.62f);
            var camera = rig.gameObject.AddComponent<Camera>();
            rig.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.042f, 0.068f, 0.092f);
            camera.fieldOfView = 62f;
            camera.nearClipPlane = 0.055f;
            camera.farClipPlane = 68f;
            camera.allowHDR = true;
            camera.allowMSAA = false;
            var parameters = camera.GetUniversalAdditionalCameraData();
            parameters.renderPostProcessing = art;
            parameters.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            parameters.antialiasingQuality = AntialiasingQuality.Medium;
            rig.gameObject.AddComponent<AudioListener>();

            // Directional overcast monsoon moonlight lives in Bootstrap so all additive zones share consistent sun/moon shadows!
            var moon = Group("Overcast monsoon moonlight", permanent).gameObject.AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.transform.rotation = Quaternion.Euler(34f, 192f, 0f);
            moon.color = new Color(0.58f, 0.76f, 0.90f);
            moon.intensity = art ? 0.92f : 1.7f;
            moon.shadows = LightShadows.Soft;
            moon.shadowStrength = 0.86f;
            moon.shadowBias = 0.04f;
            moon.shadowNormalBias = 0.35f;

            // --- R (2012) Atmospheric Warm/Cool Contrast Lighting (auto-routed to LOC01..04!) ---
            var coolNight = new Color(0.48f, 0.68f, 0.84f);
            var warmSodium = new Color(0.95f, 0.72f, 0.44f);
            var subtleBounce = new Color(0.54f, 0.68f, 0.76f);

            var canopyLamp = Lamp("Key below entry canopy", LightType.Spot, new Vector3(4.0f, 2.68f, -8.65f), 5.2f, 12.0f, warmSodium, permanent, shadow: art);
            canopyLamp.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            canopyLamp.spotAngle = 124f;
            canopyLamp.innerSpotAngle = 68f;
            canopyLamp.shadowStrength = 0.85f;
            Lamp("Street wash at the gate", LightType.Point, new Vector3(6.03f, 3.60f, -19.75f), 2.15f, 13.0f, new Color(0.76f, 0.78f, 0.70f), permanent);
            Lamp("Bounced entry spill", LightType.Point, new Vector3(4.6f, 2.35f, -4.5f), 0.92f, 8.8f, coolNight, permanent);
            Lamp("Emergency glass return", LightType.Point, new Vector3(6.20f, 2.65f, 19.48f), 1.05f, 9.8f, coolNight, permanent);
            Lamp("Wayfinding on staff corridor", LightType.Point, new Vector3(0.2f, 2.66f, 8.3f), 0.72f, 11.0f, coolNight, permanent);

            // Calibrated horror bounce fills in R (deep atmospheric shadows without crushing clues or architectural depth!)
            Lamp("Room 101 floor bounce R", LightType.Point, new Vector3(9.4f, 1.45f, 5.2f), 0.82f, 8.2f, subtleBounce, real);
            Lamp("Back wall warm dust bounce R", LightType.Point, new Vector3(12.2f, 2.15f, 4.4f), 0.65f, 6.8f, new Color(0.80f, 0.74f, 0.60f), real);
            Lamp("Dry store readable spill R", LightType.Point, new Vector3(15.6f, 2.45f, 3.65f), 1.15f, 6.2f, new Color(0.86f, 0.78f, 0.62f), real);
            var corrSpill = Lamp("Corridor doorway spill R", LightType.Spot, new Vector3(6.6f, 2.45f, 4.75f), 3.2f, 9.2f, coolNight, real, shadow: art);
            corrSpill.transform.LookAt(new Vector3(3.1f, 0.35f, 4.75f));
            corrSpill.spotAngle = 82f;
            corrSpill.innerSpotAngle = 42f;
            Lamp("Corridor ambient depth R", LightType.Point, new Vector3(4.0f, 2.55f, 4.95f), 0.84f, 8.4f, coolNight, real);
            Lamp("Corridor mid bounce R", LightType.Point, new Vector3(4.0f, 2.55f, 10.6f), 0.68f, 8.2f, coolNight, real);
            Lamp("Corridor far red emergency hint R", LightType.Point, new Vector3(4.2f, 2.75f, 14.2f), 1.05f, 8.5f, new Color(0.88f, 0.28f, 0.22f), real);
            Lamp("Desk readable spill R", LightType.Point, new Vector3(2.35f, 2.45f, -3.5f), 0.85f, 7.2f, new Color(0.72f, 0.78f, 0.74f), real);
            Lamp("Reception desk lamp warm glow", LightType.Point, new Vector3(1.88f, 1.56f, -3.45f), 1.35f, 4.2f, new Color(0.98f, 0.76f, 0.46f), permanent);

            // --- B (2005) Institutional Operational Fluorescent T8 Fixtures ---
            var cleanTube = new Color(0.86f, 0.96f, 0.87f);
            Tube(new Vector3(4.00f, 3.59f, 4.75f), 0f, cleanTube, 3.1f, castShadow: art);
            Tube(new Vector3(4.00f, 3.59f, 10.60f), 0f, cleanTube, 2.3f, castShadow: false);
            Tube(new Vector3(2.15f, 3.59f, -3.20f), 0f, cleanTube, 2.8f, castShadow: false);
            Tube(new Vector3(6.32f, 3.59f, -4.58f), 0f, cleanTube, 2.2f, castShadow: false);
            Tube(new Vector3(7.80f, 3.59f, 5.00f), 90f, cleanTube, 3.4f, castShadow: art);
            Tube(new Vector3(11.55f, 3.59f, 5.20f), 90f, cleanTube, 3.1f, castShadow: art);

            Lamp("Room 101 GI bounce B", LightType.Point, new Vector3(9.65f, 0.85f, 5.0f), 0.82f, 8.8f, new Color(0.76f, 0.82f, 0.72f), report);
            Lamp("Corridor GI bounce B", LightType.Point, new Vector3(4.0f, 0.95f, 5.5f), 0.65f, 8.8f, new Color(0.74f, 0.80f, 0.72f), report);
            Lamp("Reception GI bounce B", LightType.Point, new Vector3(4.0f, 1.05f, -3.5f), 0.78f, 8.5f, new Color(0.78f, 0.84f, 0.76f), report);

            if (!art)
                return;

            // --- Hero Shadow-Casting Window Spotlights in R ---
            var winSpecs = new (float x, float intensity, bool shadow)[]
            {
                (7.35f, 9.6f, true),
                (10.28f, 8.9f, true),
                (12.39f, 5.6f, false)
            };
            foreach (var w in winSpecs)
            {
                var from = new Vector3(w.x, 2.55f, 10.45f);
                var to = new Vector3(w.x + 0.35f, 0.15f, 4.35f);
                var beam = Lamp("Window key moonlight", LightType.Spot, from, w.intensity, 12.8f, new Color(0.58f, 0.78f, 0.94f), real, shadow: w.shadow);
                beam.transform.LookAt(to);
                beam.spotAngle = 62f;
                beam.innerSpotAngle = 32f;
                if (w.shadow)
                {
                    beam.shadowStrength = 0.90f;
                    beam.shadowBias = 0.025f;
                    beam.shadowNormalBias = 0.22f;
                }
                LightShaft(new Vector3(w.x, 1.92f, 7.98f), to, w.x > 11.5f ? 0.65f : 0.96f, 1.05f, real);
            }
            Lamp("Courtyard distant amber glow", LightType.Point, new Vector3(10.2f, 3.2f, 13.8f), 6.8f, 16f, new Color(0.92f, 0.64f, 0.36f), real);

            // --- Zone-Owned Box-Projected Reflection Probes (LOC03 Ward + Corridor; LOC01/02/04 added in Zones.cs!) ---
            var rp101 = CreateReflectionProbe("Local reflection · LOC03", new Vector3(9.71f, 1.75f, 5.05f), new Vector3(8.4f, 3.65f, 6.2f), ResolveParent(permanent, new Vector3(9.71f, 0, 5.05f)));
            var rpHall = CreateReflectionProbe("Local reflection · HALL", new Vector3(4.0f, 1.75f, 5.8f), new Vector3(3.3f, 3.65f, 14.0f), ResolveParent(permanent, new Vector3(4.0f, 0, 5.8f)));
            if (multiScene)
            {
                zones["LOC03"].probes.Add(rp101);
                zones["LOC03"].probes.Add(rpHall);
            }
            else
            {
                world.localReflection = rp101;
                world.corridorReflection = rpHall;
            }

            // --- Blender 3-Blade Enamel Ceiling Fans ---
            Fan(new Vector3(7.55f, 3.58f, 5.05f));
            Fan(new Vector3(11.35f, 3.58f, 5.05f));
            Fan(new Vector3(4.30f, 3.58f, -3.94f));

            // --- Weather & Atmosphere: Monsoon Rain + Floating Dust Motes ---
            Rain(new Vector3(9.72f, 4.1f, 9.2f), new Vector3(9.2f, 0.18f, 2.2f), 380);
            Rain(new Vector3(4, 4.6f, -13.6f), new Vector3(15, 0.21f, 13.5f), 460);
            DustMotes(new Vector3(9.35f, 1.65f, 5.85f), new Vector3(5.6f, 2.1f, 3.4f));

            // --- Cinematic Horror URP Post-Processing Profiles (Calibrated R 2012 vs B 2005!) ---
            world.realGrade = Grade("2012", exposure: 0.42f, saturation: -12f, contrast: 12f,
                filter: new Color(0.92f, 0.97f, 1.0f), vignette: 0.26f, grain: 0.17f, ca: 0.045f,
                shadowTone: new Color(0.30f, 0.44f, 0.52f), highlightTone: new Color(0.88f, 0.84f, 0.76f));
            world.reportGrade = Grade("2005", exposure: -0.02f, saturation: -6f, contrast: 10f,
                filter: new Color(0.96f, 1.0f, 0.94f), vignette: 0.16f, grain: 0.07f, ca: 0.022f,
                shadowTone: new Color(0.36f, 0.48f, 0.44f), highlightTone: new Color(0.95f, 0.98f, 0.91f));

            var motion = Group("Ceiling fans and fabric", permanent).gameObject.AddComponent<EnvironmentMotion>();
            motion.blades = movingFans.ToArray();
            motion.tubes = livingTubes.ToArray();
            motion.curtains = movingCloth.ToArray();
            world.motion = motion;
            world.atmosphere = BuildAudio();
        }

        /// <summary>
        /// M2 Measured Indirect GI System: places a dense 3-tier LightProbeGroup spanning LOC01–LOC04,
        /// runs Unity 6.3 Progressive CPU Lightmapper for BOTH R (2012) and B (2005) lighting scenarios,
        /// and stores reusable LightProbes_R.asset & LightProbes_B.asset for atomic <0.05ms R<->B GI swaps!
        /// </summary>
        private void BakeDualLightProbes()
        {
            SceneManager.SetActiveScene(bootstrapScene);
            var probeHost = Group("Dual-Scenario GI LightProbes", permanent).gameObject;
            var group = probeHost.AddComponent<LightProbeGroup>();
            var pts = new List<Vector3>();

            void AddGrid(float x0, float x1, int nx, float z0, float z1, int nz, float[] ys)
            {
                for (int ix = 0; ix < nx; ix++)
                    for (int iz = 0; iz < nz; iz++)
                    {
                        float x = nx == 1 ? (x0 + x1) * 0.5f : Mathf.Lerp(x0, x1, ix / (float)(nx - 1));
                        float z = nz == 1 ? (z0 + z1) * 0.5f : Mathf.Lerp(z0, z1, iz / (float)(nz - 1));
                        foreach (float y in ys) pts.Add(new Vector3(x, y, z));
                    }
            }

            float[] interiorHeights = { 0.55f, 1.58f, 2.75f };
            float[] exteriorHeights = { 0.65f, 1.85f, 3.20f };
            // LOC01 Courtyard & Gate approach
            AddGrid(1.0f, 7.0f, 3, -22.0f, -9.0f, 5, exteriorHeights);
            // LOC02 Reception Lobby & Desk (around Lan!)
            AddGrid(0.2f, 7.8f, 4, -7.2f, -0.8f, 3, interiorHeights);
            // LOC03 Main Corridor + Staff Corridor
            AddGrid(3.0f, 5.0f, 2, 0.8f, 15.2f, 6, interiorHeights);
            AddGrid(-0.5f, 0.8f, 2, 1.0f, 15.0f, 4, new[] { 0.7f, 1.8f });
            // LOC03 Room 101 + Store Room (Kho giường)
            AddGrid(6.2f, 13.2f, 4, 2.5f, 7.5f, 3, interiorHeights);
            AddGrid(14.3f, 16.6f, 2, 2.5f, 5.0f, 2, new[] { 0.75f, 1.85f });
            // LOC04 Emergency Bay
            AddGrid(0.2f, 8.2f, 4, 16.8f, 21.4f, 3, interiorHeights);

            group.probePositions = pts.ToArray();

            const string lsPath = Root + "/Generated/CTCC_ProbeGI.lighting";
            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(lsPath);
            if (settings == null)
            {
                settings = new LightingSettings();
                AssetDatabase.CreateAsset(settings, lsPath);
            }
            settings.bakedGI = true;
            settings.realtimeGI = false;
            settings.lightmapper = LightingSettings.Lightmapper.ProgressiveCPU;
            settings.directSampleCount = 16;
            settings.indirectSampleCount = 48;
            settings.environmentSampleCount = 24;
            settings.maxBounces = 2;
            settings.lightmapResolution = 2f; // Low lightmap res since all dynamic R/B surfaces use ReceiveGI.LightProbes!
            settings.ao = true;
            settings.aoMaxDistance = 0.85f;
            EditorUtility.SetDirty(settings);
            Lightmapping.lightingSettings = settings;
            Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;

            // Collect all scene lights and temporarily set Mixed so Progressive CPU bounces their energy into LightProbes!
            var allLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var originalBakeTypes = new Dictionary<Light, LightmapBakeType>(allLights.Length);
            foreach (var l in allLights)
            {
                originalBakeTypes[l] = l.lightmapBakeType;
                l.lightmapBakeType = LightmapBakeType.Mixed;
            }

            try
            {
                // 1) Bake Scenario R (2012)
                world.Present(false, 0, false, false, false, false);
                var t0 = EditorApplication.timeSinceStartup;
                bool okR = Lightmapping.Bake();
                double secR = EditorApplication.timeSinceStartup - t0;
                world.realLightProbes = SaveBakedProbesAsset(Root + "/Generated/LightProbes_R.asset");

                // 2) Bake Scenario B (2005)
                world.Present(true, 0, true, true, false, false);
                var t1 = EditorApplication.timeSinceStartup;
                bool okB = Lightmapping.Bake();
                double secB = EditorApplication.timeSinceStartup - t1;
                world.reportLightProbes = SaveBakedProbesAsset(Root + "/Generated/LightProbes_B.asset");

                int countR = world.realLightProbes != null ? world.realLightProbes.count : 0;
                int countB = world.reportLightProbes != null ? world.reportLightProbes.count : 0;
                Debug.Log($"CTCC_GI_PROBES_BAKE okR={okR} ({secR:F1}s, {countR} probes) okB={okB} ({secB:F1}s, {countB} probes)");
            }
            finally
            {
                foreach (var kv in originalBakeTypes)
                    if (kv.Key != null) kv.Key.lightmapBakeType = kv.Value;
                world.Present(false, 0, false, false, false, false);
                if (world.realLightProbes != null)
                    LightmapSettings.lightProbes = world.realLightProbes;
            }
        }

        private static LightProbes SaveBakedProbesAsset(string assetPath)
        {
            if (LightmapSettings.lightProbes == null || LightmapSettings.lightProbes.count == 0)
                return null;
            if (AssetDatabase.LoadAssetAtPath<LightProbes>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);
            var copy = UnityEngine.Object.Instantiate(LightmapSettings.lightProbes);
            copy.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(copy, assetPath);
            EditorUtility.SetDirty(copy);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<LightProbes>(assetPath);
        }

        private void ConfigureUrpPipeline()
        {
            if (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset") is { } pipeline)
            {
                pipeline.renderScale = art ? 0.88f : 1.0f;
                pipeline.shadowDistance = 24f;
                pipeline.shadowCascadeCount = 1;
                pipeline.useSRPBatcher = true;
                pipeline.supportsCameraDepthTexture = true;
                pipeline.supportsCameraOpaqueTexture = false;

                var pipeSo = new SerializedObject(pipeline);
                var mainRes = pipeSo.FindProperty("m_MainLightShadowmapResolution");
                if (mainRes != null) mainRes.intValue = 2048;
                var addAtlas = pipeSo.FindProperty("m_AdditionalLightsShadowmapResolution");
                if (addAtlas != null) addAtlas.intValue = 2048;
                var addPerObj = pipeSo.FindProperty("m_AdditionalLightsPerObjectLimit");
                if (addPerObj != null) addPerObj.intValue = 8;
                pipeSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(pipeline);
            }

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
            if (rendererData == null) return;

            bool hasDecal = false;
            foreach (var feat in rendererData.rendererFeatures)
            {
                if (feat == null) continue;
                if (feat is ScreenSpaceAmbientOcclusion ssao)
                {
                    var so = new SerializedObject(ssao);
                    var s = so.FindProperty("m_Settings");
                    if (s != null)
                    {
                        s.FindPropertyRelative("Intensity").floatValue = 0.86f;
                        s.FindPropertyRelative("Radius").floatValue = 0.56f;
                        s.FindPropertyRelative("DirectLightingStrength").floatValue = 0.34f;
                        s.FindPropertyRelative("Samples").enumValueIndex = 1;
                        s.FindPropertyRelative("NormalSamples").enumValueIndex = 1;
                        s.FindPropertyRelative("Downsample").boolValue = true;
                        s.FindPropertyRelative("AfterOpaque").boolValue = false;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(ssao);
                    }
                }
                else if (feat.GetType().Name == "DecalRendererFeature")
                {
                    hasDecal = true;
                }
            }

            if (!hasDecal)
            {
                var decalFeat = ScriptableObject.CreateInstance<DecalRendererFeature>();
                decalFeat.name = "DecalRendererFeature";
                var dso = new SerializedObject(decalFeat);
                var ds = dso.FindProperty("m_Settings");
                if (ds != null)
                {
                    var tech = ds.FindPropertyRelative("technique");
                    if (tech != null) tech.enumValueIndex = 1;
                    var maxDist = ds.FindPropertyRelative("maxDrawDistance");
                    if (maxDist != null) maxDist.floatValue = 35f;
                    var dbuf = ds.FindPropertyRelative("dBufferSettings");
                    if (dbuf != null)
                    {
                        var surf = dbuf.FindPropertyRelative("surfaceData");
                        if (surf != null) surf.enumValueIndex = 2;
                    }
                    dso.ApplyModifiedPropertiesWithoutUndo();
                }
                AssetDatabase.AddObjectToAsset(decalFeat, rendererData);
                var rso = new SerializedObject(rendererData);
                var listProp = rso.FindProperty("m_RendererFeatures");
                int idx = listProp.arraySize;
                listProp.InsertArrayElementAtIndex(idx);
                listProp.GetArrayElementAtIndex(idx).objectReferenceValue = decalFeat;
                rso.ApplyModifiedPropertiesWithoutUndo();
                rendererData.SetDirty();
                EditorUtility.SetDirty(rendererData);
            }
            AssetDatabase.SaveAssets();
        }

        private ReflectionProbe CreateReflectionProbe(string name, Vector3 pos, Vector3 size, Transform parent = null)
        {
            var probe = Group(name, ResolveParent(parent, pos), pos).gameObject.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
            probe.resolution = 256;
            probe.boxProjection = true;
            probe.size = size;
            probe.intensity = 0.88f;
            probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
            probe.backgroundColor = new Color(0.08f, 0.12f, 0.14f);
            return probe;
        }

        private Light Lamp(string label, LightType type, Vector3 p, float intensity, float range, Color color,
            Transform parent, bool shadow = false)
        {
            var l = Group(label, ResolveParent(parent, p), p).gameObject.AddComponent<Light>();
            l.type = type;
            l.range = range;
            l.intensity = intensity;
            l.color = color;
            l.shadows = shadow ? LightShadows.Soft : LightShadows.None;
            if (shadow)
            {
                l.shadowStrength = 0.82f;
                l.shadowBias = 0.03f;
                l.shadowNormalBias = 0.25f;
            }
            return l;
        }

        private void Tube(Vector3 p, float yaw, Color clean, float intensity, bool castShadow = false)
        {
            var lit = Lamp("Mains tube pool", castShadow ? LightType.Spot : LightType.Point,
                p + Vector3.down * 0.14f, castShadow ? intensity * 2.2f : intensity, 7.8f, clean, report, shadow: castShadow);
            if (castShadow)
            {
                lit.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                lit.spotAngle = 128f;
                lit.innerSpotAngle = 72f;
                lit.shadowStrength = 0.72f;
            }
            if (multiScene) ZoneAt(p.z).tubes.Add(lit);
            else livingTubes.Add(lit);
            if (!art) return;
            var fixture = PlaceBlenderModel("CTCC_ENV_FluorescentFixture", permanent, p, yaw, pairRB: true);
            staticArchitecture.Add(fixture);
        }

        private void Fan(Vector3 p)
        {
            var fan = PlaceBlenderModel("CTCC_ENV_CeilingFan", permanent, p, 0f, pairRB: true);
            var rotor = FindDescendant(fan.transform, "FanRotor");
            if (rotor != null)
            {
                if (multiScene) ZoneAt(p.z).fans.Add(rotor);
                else movingFans.Add(rotor);
            }
        }

        private Volume Grade(string key, float exposure, float saturation, float contrast, Color filter,
            float vignette, float grain, float ca, Color shadowTone, Color highlightTone)
        {
            string path = Root + "/Generated/" + key + "-grade.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            T Part<T>() where T : VolumeComponent
            {
                if (profile.TryGet<T>(out var component)) return component;
                component = profile.Add<T>(true);
                AssetDatabase.AddObjectToAsset(component, profile);
                return component;
            }
            var tone = Part<Tonemapping>();
            tone.mode.Override(TonemappingMode.ACES);

            var color = Part<ColorAdjustments>();
            color.postExposure.Override(exposure);
            color.contrast.Override(contrast);
            color.saturation.Override(saturation);
            color.colorFilter.Override(filter);

            var split = Part<SplitToning>();
            split.shadows.Override(shadowTone);
            split.highlights.Override(highlightTone);
            split.balance.Override(-15f);

            var bloom = Part<Bloom>();
            bloom.intensity.Override(0.30f);
            bloom.threshold.Override(1.06f);
            bloom.scatter.Override(0.62f);

            var vig = Part<Vignette>();
            vig.intensity.Override(vignette);
            vig.smoothness.Override(0.45f);
            vig.color.Override(new Color(0.02f, 0.04f, 0.045f));

            var fg = Part<FilmGrain>();
            fg.type.Override(FilmGrainLookup.Thin1);
            fg.intensity.Override(grain);
            fg.response.Override(0.75f);

            var chrom = Part<ChromaticAberration>();
            chrom.intensity.Override(ca);

            var lens = Part<LensDistortion>();
            lens.intensity.Override(0f);

            var dof = Part<DepthOfField>();
            dof.active = false;
            dof.mode.Override(DepthOfFieldMode.Bokeh);
            dof.focusDistance.Override(0.42f);
            dof.focalLength.Override(55f);
            dof.aperture.Override(4.2f);

            foreach (var comp in profile.components) EditorUtility.SetDirty(comp);
            EditorUtility.SetDirty(profile);
            var v = Group("Color grade · " + key, permanent).gameObject.AddComponent<Volume>();
            v.isGlobal = true;
            v.sharedProfile = profile;
            return v;
        }

        private void LightShaft(Vector3 source, Vector3 end, float width, float height, Transform parent)
        {
            var node = Group("Soft window scatter", ResolveParent(parent, source));
            Vector3 left = Vector3.right * (width * 0.5f), up = Vector3.up * (height * 0.5f);
            var mesh = new Mesh { name = "Authored window shaft" };
            mesh.vertices = new[]
            {
                source - left, source + left, end + left * 2.1f, end - left * 2.1f,
                source - up * 0.9f, source + up * 0.9f, end + up * 1.35f, end - up * 0.52f
            };
            mesh.uv = new[]
            {
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
            };
            mesh.triangles = new[] { 0, 1, 2, 2, 3, 0, 4, 5, 6, 6, 7, 4 };
            mesh.RecalculateBounds();
            StoreMesh(mesh);
            node.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = node.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Mat("Beam");
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private void DustMotes(Vector3 center, Vector3 boxSize)
        {
            var p = Group("Room 101 floating dust motes", ResolveParent(real, center), center).gameObject.AddComponent<ParticleSystem>();
            var main = p.main;
            main.loop = true;
            main.startLifetime = 6.5f;
            main.startSpeed = 0.025f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.005f, 0.011f);
            main.startColor = new Color(0.70f, 0.84f, 0.88f, 0.22f);
            main.maxParticles = 110;
            main.prewarm = true;
            var shape = p.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = boxSize;
            var emission = p.emission;
            emission.rateOverTime = 16f;
            var vel = p.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-0.035f, 0.035f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.02f, 0.018f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.035f, 0.035f);

            const string path = Root + "/Generated/Materials/DustMote.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_BaseMap", Tex("SoftParticle"));
            Transparent(mat, new Color(0.78f, 0.90f, 0.94f, 0.24f));
            var renderer = p.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private void Rain(Vector3 position, Vector3 footprint, float rate)
        {
            var p = Group("Exterior rain", ResolveParent(real, position), position).gameObject.AddComponent<ParticleSystem>();
            var main = p.main;
            main.loop = true;
            main.startLifetime = 0.68f;
            main.startSpeed = 0f;
            main.startSize = 0.012f;
            main.startColor = new Color(0.62f, 0.78f, 0.88f, 0.36f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 620;
            var shape = p.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = footprint;
            var emission = p.emission;
            emission.rateOverTime = rate;
            var velocity = p.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = 0.92f;
            velocity.y = -9.2f;
            velocity.z = -0.32f;
            const string path = Root + "/Generated/Materials/WeatherRain.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            Transparent(mat, new Color(0.71f, 0.84f, 0.92f, 0.34f));
            var renderer = p.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mat;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.8f;
            renderer.velocityScale = 0.085f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private EnvironmentalAudio BuildAudio()
        {
            const string audioPath = Root + "/Art/Audio/";
            AudioClip Clip(string id) => AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath + id + ".wav");
            var root = Group("Spatial ambience and prop cues", permanent);
            var bus = root.gameObject.AddComponent<EnvironmentalAudio>();

            AudioSource Source(string name, Transform parent, Vector3 point, float max)
            {
                var item = Group(name, parent, point).gameObject.AddComponent<AudioSource>();
                item.playOnAwake = false;
                item.dopplerLevel = 0;
                item.rolloffMode = AudioRolloffMode.Linear;
                item.minDistance = 1.5f;
                item.maxDistance = max;
                item.spatialBlend = 0.96f;
                return item;
            }
            var bootstrapLoops = new List<RoomLoop>();
            void AddLoop(string id, Vector3 point, float level, float max, bool b, bool occluded = false)
            {
                Transform loopParent = multiScene ? ZoneAt(point.z).permanent : root;
                var item = Source("Loop · " + id, loopParent, point, max);
                item.loop = true;
                item.clip = Clip(id);
                var rl = new RoomLoop { source = item, level = level, report = b, exteriorOccluded = occluded };
                if (multiScene) ZoneAt(point.z).loops.Add(rl);
                else bootstrapLoops.Add(rl);
            }

            AddLoop("Rain", new Vector3(4, 1.2f, -12.2f), 0.62f, 24, false, occluded: true);
            AddLoop("Roof", new Vector3(9.4f, 2.3f, 8.85f), 0.72f, 15, false, occluded: true);
            AddLoop("Rumble", new Vector3(5.52f, 2.2f, -10.0f), 0.48f, 24, false, occluded: true);
            AddLoop("Drips", new Vector3(9.85f, 0.39f, 7.65f), 0.72f, 9, false, occluded: false);
            AddLoop("Fan", new Vector3(1.36f, 2.85f, -3.77f), 0.56f, 13, true);
            AddLoop("Fan", new Vector3(9.31f, 2.94f, 5.41f), 0.64f, 11, true);
            AddLoop("Tube", new Vector3(4.07f, 3.31f, 4.75f), 0.52f, 10, true);
            AddLoop("Tube", new Vector3(9.65f, 3.31f, 5.10f), 0.44f, 9, true);
            AddLoop("EmergencyHum", new Vector3(4.0f, 2.50f, 19.2f), 0.42f, 10, false, occluded: false);

            bus.loops = bootstrapLoops.ToArray();
            bus.propCue = Source("Localized interaction cue", root, Vector3.zero, 16);
            bus.paper = Clip("Paper");
            bus.stamp = Clip("Stamp");
            bus.slippers = Clip("Slipper");
            bus.step = Clip("Step");
            bus.stepWet = Clip("StepWet");
            bus.stepCorridor = Clip("StepCorridor");
            bus.switchLayer = Clip("Switch");
            bus.switchToB = Clip("SwitchToB");
            bus.switchToR = Clip("SwitchToR");
            bus.doorCreak = Clip("DoorCreak");
            bus.doorLatch = Clip("DoorLatch");
            bus.curtainSlide = Clip("CurtainSlide");
            bus.cabinetRoll = Clip("CabinetRoll");

            // M2 Spatial SFX & Original 5-note Lan Motif
            bus.keyJingle = Clip("KeyJingle");
            bus.trolleyRoll = Clip("TrolleyRoll");
            bus.phoneBuzz = Clip("PhoneBuzz");
            bus.paStatic = Clip("PAStatic");
            bus.breakerCut = Clip("BreakerCut");
            bus.emergencyHum = Clip("EmergencyHum");
            bus.distantThunder = Clip("DistantThunder");
            bus.lanMotif = Clip("MUS_LanMotif");

            // Load canonical Vietnamese Temp VO clips (TASK009-B) with hard LOCK-V22-02 filter
            var voList = new List<VoiceClipEntry>();
            string voDir = audioPath + "VO/";
            if (Directory.Exists(voDir))
            {
                foreach (var wav in Directory.GetFiles(voDir, "*.wav").OrderBy(x => x))
                {
                    string id = Path.GetFileNameWithoutExtension(wav);
                    if (id == "CH01-D16b" || id == "CH04-D06") continue;
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(wav.Replace('\\', '/'));
                    if (clip != null) voList.Add(new VoiceClipEntry { id = id, clip = clip });
                }
            }
            bus.voiceClips = voList.ToArray();
            Debug.Log($"CTCC_AUDIO_WIRED {voList.Count} VO clips, lanMotif={(bus.lanMotif != null)}");

            void AddReverb(string rName, Vector3 pos, float minD, float maxD, AudioReverbPreset preset)
            {
                var rz = Group(rName, root, pos).gameObject.AddComponent<AudioReverbZone>();
                rz.minDistance = minD;
                rz.maxDistance = maxD;
                rz.reverbPreset = preset;
            }
            AddReverb("Reverb · Room 101 Tiled Ward", new Vector3(9.64f, 1.65f, 4.98f), 2.8f, 5.6f, AudioReverbPreset.Room);
            AddReverb("Reverb · Long Tiled Corridor", new Vector3(4.00f, 1.65f, 7.50f), 2.2f, 7.8f, AudioReverbPreset.Hallway);
            AddReverb("Reverb · Dry Store Closet", new Vector3(15.50f, 1.45f, 3.60f), 1.2f, 2.8f, AudioReverbPreset.Bathroom);
            AddReverb("Reverb · Emergency Bay", new Vector3(4.00f, 1.65f, 19.0f), 2.5f, 6.2f, AudioReverbPreset.Hallway);

            return bus;
        }
    }
}
