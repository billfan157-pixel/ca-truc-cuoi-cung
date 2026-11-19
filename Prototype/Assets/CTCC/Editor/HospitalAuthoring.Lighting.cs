using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
            RenderSettings.ambientSkyColor = new Color(0.13f, 0.19f, 0.23f);
            RenderSettings.ambientEquatorColor = new Color(0.08f, 0.11f, 0.13f);
            RenderSettings.ambientGroundColor = new Color(0.045f, 0.042f, 0.036f);
            RenderSettings.fog = art;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.018f;
            RenderSettings.reflectionIntensity = 0.72f;

            var rig = Group("Seeded player view", world.transform, world.Pad("AP00").position + Vector3.up * 1.62f);
            var camera = rig.gameObject.AddComponent<Camera>();
            rig.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.042f, 0.068f, 0.092f);
            camera.fieldOfView = 62f;
            camera.nearClipPlane = 0.055f;
            camera.farClipPlane = 58f;
            camera.allowHDR = true;
            camera.allowMSAA = false;
            var parameters = camera.GetUniversalAdditionalCameraData();
            parameters.renderPostProcessing = art;
            parameters.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            parameters.antialiasingQuality = AntialiasingQuality.Medium;
            rig.gameObject.AddComponent<AudioListener>();

            // Directional overcast monsoon moonlight (angled to sweep across yard & windows!)
            var moon = Group("Overcast monsoon moonlight", permanent).gameObject.AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.transform.rotation = Quaternion.Euler(34f, 192f, 0f);
            moon.color = new Color(0.56f, 0.74f, 0.88f);
            moon.intensity = art ? 0.85f : 1.7f;
            moon.shadows = LightShadows.Soft;
            moon.shadowStrength = 0.88f;
            moon.shadowBias = 0.04f;
            moon.shadowNormalBias = 0.35f;

            // --- R (2012) Atmospheric Warm/Cool Contrast Lighting ---
            var coolNight = new Color(0.48f, 0.68f, 0.84f);
            var warmSodium = new Color(0.95f, 0.72f, 0.44f);
            var subtleBounce = new Color(0.52f, 0.65f, 0.72f);

            // Warm exterior sodium bulkhead lamp under courtyard canopy (downward Spot = 1 shadow pass vs 6!)
            var canopyLamp = Lamp("Key below entry canopy", LightType.Spot, new Vector3(4.0f, 2.68f, -8.65f), 4.8f, 11.5f, warmSodium, permanent, shadow: art);
            canopyLamp.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            canopyLamp.spotAngle = 124f;
            canopyLamp.innerSpotAngle = 68f;
            canopyLamp.shadowStrength = 0.85f;
            Lamp("Street wash at the gate", LightType.Point, new Vector3(6.03f, 3.60f, -19.75f), 1.85f, 12.0f, new Color(0.74f, 0.76f, 0.68f), permanent);
            Lamp("Bounced entry spill", LightType.Point, new Vector3(4.6f, 2.35f, -4.5f), 0.72f, 8.2f, coolNight, permanent);
            Lamp("Emergency glass return", LightType.Point, new Vector3(6.20f, 2.65f, 19.48f), 0.88f, 9.8f, coolNight, permanent);
            Lamp("Wayfinding on staff corridor", LightType.Point, new Vector3(0.2f, 2.66f, 8.3f), 0.48f, 10.5f, coolNight, permanent);

            // Subtle dark horror bounce fills in R (low enough that shadows breathe, clear enough for clues!)
            Lamp("Room 101 floor bounce R", LightType.Point, new Vector3(9.4f, 1.35f, 5.2f), 0.58f, 7.5f, subtleBounce, real);
            Lamp("Back wall warm dust bounce R", LightType.Point, new Vector3(12.2f, 2.15f, 4.4f), 0.44f, 6.2f, new Color(0.78f, 0.72f, 0.58f), real);
            Lamp("Dry store readable spill R", LightType.Point, new Vector3(15.6f, 2.38f, 3.65f), 0.78f, 5.4f, new Color(0.84f, 0.76f, 0.60f), real);
            // Corridor moody spill: warm faint emergency/exit hint at far end + cool doorway spill from Room 101
            var corrSpill = Lamp("Corridor doorway spill R", LightType.Spot, new Vector3(6.6f, 2.45f, 4.75f), 2.8f, 8.8f, coolNight, real, shadow: art);
            corrSpill.transform.LookAt(new Vector3(3.1f, 0.35f, 4.75f));
            corrSpill.spotAngle = 82f;
            corrSpill.innerSpotAngle = 42f;
            Lamp("Corridor ambient depth R", LightType.Point, new Vector3(4.0f, 2.55f, 4.95f), 0.62f, 7.6f, coolNight, real);
            Lamp("Corridor far red emergency hint R", LightType.Point, new Vector3(4.2f, 2.75f, 14.2f), 0.95f, 8.2f, new Color(0.86f, 0.30f, 0.24f), real);
            Lamp("Desk readable spill R", LightType.Point, new Vector3(2.05f, 2.45f, -3.5f), 0.62f, 6.5f, new Color(0.68f, 0.76f, 0.72f), real);
            // Warm vintage green desk lamp pool illuminating Reception duty logbook & phone!
            Lamp("Reception desk lamp warm glow", LightType.Point, new Vector3(1.88f, 1.56f, -3.45f), 1.15f, 3.6f, new Color(0.98f, 0.76f, 0.46f), permanent);

            // --- B (2005) Institutional Operational Fluorescent T8 Fixtures (with real soft shadows!) ---
            var cleanTube = new Color(0.86f, 0.96f, 0.87f);
            Tube(new Vector3(4.00f, 3.59f, 4.75f), 0f, cleanTube, 3.1f, castShadow: art);
            Tube(new Vector3(4.00f, 3.59f, 10.60f), 0f, cleanTube, 2.2f, castShadow: false);
            Tube(new Vector3(2.15f, 3.59f, -3.20f), 0f, cleanTube, 2.6f, castShadow: false);
            Tube(new Vector3(6.32f, 3.59f, -4.58f), 0f, cleanTube, 2.0f, castShadow: false);
            // Primary Room 101 dual T8 fixtures casting crisp downward hospital shadows in B!
            Tube(new Vector3(7.80f, 3.59f, 5.00f), 90f, cleanTube, 3.4f, castShadow: art);
            Tube(new Vector3(11.55f, 3.59f, 5.20f), 90f, cleanTube, 3.1f, castShadow: art);

            // Subtle warm floor bounce in B so shadows aren't dead black
            Lamp("Room 101 GI bounce B", LightType.Point, new Vector3(9.65f, 0.85f, 5.0f), 0.72f, 8.5f, new Color(0.76f, 0.82f, 0.72f), report);
            Lamp("Corridor GI bounce B", LightType.Point, new Vector3(4.0f, 0.95f, 5.5f), 0.55f, 8.5f, new Color(0.74f, 0.80f, 0.72f), report);

            if (!art)
                return;

            // --- Hero Shadow-Casting Window Spotlights in R (casting iron security grille shadows across floor & beds!) ---
            var winSpecs = new (float x, float intensity, bool shadow)[]
            {
                (7.35f, 9.2f, true),
                (10.28f, 8.5f, true),
                (12.39f, 5.2f, false)
            };
            foreach (var w in winSpecs)
            {
                var from = new Vector3(w.x, 2.55f, 10.45f);
                var to = new Vector3(w.x + 0.35f, 0.15f, 4.35f);
                var beam = Lamp("Window key moonlight", LightType.Spot, from, w.intensity, 12.5f, new Color(0.58f, 0.78f, 0.94f), real, shadow: w.shadow);
                beam.transform.LookAt(to);
                beam.spotAngle = 62f;
                beam.innerSpotAngle = 32f;
                if (w.shadow)
                {
                    beam.shadowStrength = 0.92f;
                    beam.shadowBias = 0.025f;
                    beam.shadowNormalBias = 0.22f;
                }
                LightShaft(new Vector3(w.x, 1.92f, 7.98f), to, w.x > 11.5f ? 0.65f : 0.96f, 1.05f, real);
            }
            // Distant exterior amber courtyard street-lamp glow visible through wet trees outside north windows!
            Lamp("Courtyard distant amber glow", LightType.Point, new Vector3(10.2f, 3.2f, 13.8f), 6.5f, 16f, new Color(0.92f, 0.64f, 0.36f), real);

            // --- Dual 256px Box-Projected Reflection Probes (Room 101 + Corridor!) ---
            world.localReflection = CreateReflectionProbe("Local reflection · LOC03", new Vector3(9.71f, 1.75f, 5.05f), new Vector3(8.4f, 3.65f, 6.2f));
            world.corridorReflection = CreateReflectionProbe("Local reflection · HALL", new Vector3(4.0f, 1.75f, 5.8f), new Vector3(3.3f, 3.65f, 14.0f));

            // --- Blender 3-Blade Enamel Ceiling Fans (separate FanRotor spun smoothly in B!) ---
            Fan(new Vector3(7.55f, 3.58f, 5.05f));
            Fan(new Vector3(11.35f, 3.58f, 5.05f));
            Fan(new Vector3(4.30f, 3.58f, -3.94f));

            // --- Weather & Atmosphere: Monsoon Rain + Floating Dust Motes inside Window Light Shafts ---
            Rain(new Vector3(9.72f, 4.1f, 9.2f), new Vector3(9.2f, 0.18f, 2.2f), 380);
            Rain(new Vector3(4, 4.6f, -13.6f), new Vector3(13, 0.21f, 10.8f), 420);
            DustMotes(new Vector3(9.35f, 1.65f, 5.85f), new Vector3(5.6f, 2.1f, 3.4f));

            // --- Cinematic Horror URP Post-Processing Profiles (R 2012 vs B 2005) ---
            world.realGrade = Grade("2012", exposure: 0.28f, saturation: -14f, contrast: 16f,
                filter: new Color(0.91f, 0.97f, 1.0f), vignette: 0.30f, grain: 0.20f, ca: 0.055f,
                shadowTone: new Color(0.28f, 0.42f, 0.50f), highlightTone: new Color(0.86f, 0.82f, 0.74f));
            world.reportGrade = Grade("2005", exposure: -0.04f, saturation: -6f, contrast: 10f,
                filter: new Color(0.96f, 1.0f, 0.94f), vignette: 0.18f, grain: 0.08f, ca: 0.025f,
                shadowTone: new Color(0.36f, 0.48f, 0.44f), highlightTone: new Color(0.95f, 0.98f, 0.91f));

            var motion = Group("Ceiling fans and fabric", permanent).gameObject.AddComponent<EnvironmentMotion>();
            motion.blades = movingFans.ToArray();
            motion.tubes = livingTubes.ToArray();
            motion.curtains = movingCloth.ToArray();
            world.motion = motion;
            world.atmosphere = BuildAudio();
        }

        private void ConfigureUrpPipeline()
        {
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline)
            {
                pipeline.renderScale = art ? 0.88f : 1.0f;
                pipeline.shadowDistance = 24f;
                pipeline.shadowCascadeCount = 1;
                pipeline.useSRPBatcher = true;
                pipeline.supportsCameraDepthTexture = true;
                pipeline.supportsCameraOpaqueTexture = false;

                // Single 2048 main shadow cascade + 2048 Additional Lights Shadow Atlas = crisp shadows + high Intel Arc 1080p FPS!
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

            // Upgrade PC_Renderer.asset: strong SSAO contact shadows (half-res bilateral blur for 60fps on iGPU!) + DBuffer Decals
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
                        s.FindPropertyRelative("Samples").enumValueIndex = 1; // Medium (8 samples)
                        s.FindPropertyRelative("NormalSamples").enumValueIndex = 1; // Medium
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
                    if (tech != null) tech.enumValueIndex = 1; // DBuffer
                    var maxDist = ds.FindPropertyRelative("maxDrawDistance");
                    if (maxDist != null) maxDist.floatValue = 35f;
                    var dbuf = ds.FindPropertyRelative("dBufferSettings");
                    if (dbuf != null)
                    {
                        var surf = dbuf.FindPropertyRelative("surfaceData");
                        if (surf != null) surf.enumValueIndex = 2; // AlbedoNormalMAOS
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

        private ReflectionProbe CreateReflectionProbe(string name, Vector3 pos, Vector3 size)
        {
            var probe = Group(name, permanent, pos).gameObject.AddComponent<ReflectionProbe>();
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

        private static Light Lamp(string label, LightType type, Vector3 p, float intensity, float range, Color color,
            Transform parent, bool shadow = false)
        {
            var l = Group(label, parent, p).gameObject.AddComponent<Light>();
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
            // Downward institutional spot pool in B (casts authentic crisp bed & cabinet shadows!)
            var lit = Lamp("Mains tube pool", castShadow ? LightType.Spot : LightType.Point,
                p + Vector3.down * 0.14f, castShadow ? intensity * 2.2f : intensity, 7.6f, clean, report, shadow: castShadow);
            if (castShadow)
            {
                lit.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                lit.spotAngle = 128f;
                lit.innerSpotAngle = 72f;
                lit.shadowStrength = 0.72f;
            }
            livingTubes.Add(lit);
            if (!art) return;
            var fixture = PlaceBlenderModel("CTCC_ENV_FluorescentFixture", permanent, p, yaw, pairRB: true);
            staticArchitecture.Add(fixture);
        }

        private void Fan(Vector3 p)
        {
            var fan = PlaceBlenderModel("CTCC_ENV_CeilingFan", permanent, p, 0f, pairRB: true);
            var rotor = FindDescendant(fan.transform, "FanRotor");
            if (rotor != null) movingFans.Add(rotor);
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
            bloom.intensity.Override(0.32f);
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

            // Bokeh DOF configured ready for 3D PhysicalInspector close-ups (disabled during free exploration!)
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
            var node = Group("Soft window scatter", parent);
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
            var p = Group("Room 101 floating dust motes", real, center).gameObject.AddComponent<ParticleSystem>();
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
            var p = Group("Exterior rain", real, position).gameObject.AddComponent<ParticleSystem>();
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

            AudioSource Source(string name, Vector3 point, float max)
            {
                var item = Group(name, root, point).gameObject.AddComponent<AudioSource>();
                item.playOnAwake = false;
                item.dopplerLevel = 0;
                item.rolloffMode = AudioRolloffMode.Linear;
                item.minDistance = 1.5f;
                item.maxDistance = max;
                item.spatialBlend = 0.96f;
                return item;
            }
            RoomLoop Loop(string id, Vector3 point, float level, float max, bool b, bool occluded = false)
            {
                var item = Source(id, point, max);
                item.loop = true;
                item.clip = Clip(id);
                return new RoomLoop { source = item, level = level, report = b, exteriorOccluded = occluded };
            }

            bus.loops = new[]
            {
                Loop("Rain", new Vector3(4, 1.2f, -12.2f), 0.62f, 24, false, occluded: true),
                Loop("Roof", new Vector3(9.4f, 2.3f, 8.85f), 0.72f, 15, false, occluded: true),
                Loop("Rumble", new Vector3(5.52f, 2.2f, 0.10f), 0.48f, 24, false, occluded: true),
                Loop("Drips", new Vector3(9.85f, 0.39f, 7.65f), 0.72f, 9, false, occluded: false),
                Loop("Fan", new Vector3(1.36f, 2.85f, -3.77f), 0.56f, 13, true),
                Loop("Fan", new Vector3(9.31f, 2.94f, 5.41f), 0.64f, 11, true),
                Loop("Tube", new Vector3(4.07f, 3.31f, 4.75f), 0.52f, 10, true),
                Loop("Tube", new Vector3(9.65f, 3.31f, 5.10f), 0.44f, 9, true)
            };
            bus.propCue = Source("Localized interaction cue", Vector3.zero, 15);
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

            // Three spatially distinct Audio Reverb Zones: Room 101 Ward, Hard Tiled Corridor, Tight Store Closet!
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

            return bus;
        }
    }
}
