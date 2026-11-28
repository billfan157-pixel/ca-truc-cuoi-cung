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
            RenderSettings.ambientSkyColor = new Color(.21f, .27f, .303f);
            RenderSettings.ambientEquatorColor = new Color(.151f, .185f, .199f);
            RenderSettings.ambientGroundColor = new Color(0.068f, 0.064f, 0.055f);
            RenderSettings.fog = art;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0072f;
            RenderSettings.reflectionIntensity = 0.88f;

            // Camera stays in Bootstrap persistent scene (positioned at AP00); farClipPlane 190m covers full 100x110m campus!
            Vector3 ap00Pos = layout.pads.Single(p => p.id == "AP00") is var p0 ? new Vector3(p0.x, 0.015f, p0.z) : new Vector3(4f, 0.015f, -21.2f);
            var rig = Group("Seeded player view", world.transform, ap00Pos + Vector3.up * 1.62f);
            var camera = rig.gameObject.AddComponent<Camera>();
            rig.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            // Atmospheric stormy indigo-slate monsoon night sky so 3-story An Sinh K-A roofline, corrugated roof & water tower silhouette dramatically!
            camera.backgroundColor = new Color(0.088f, 0.132f, 0.172f);
            camera.fieldOfView = 62f;
            camera.nearClipPlane = 0.055f;
            camera.farClipPlane = 190f;
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
            moon.color = new Color(.69f, .79f, .88f);
            moon.intensity = art ? 1.38f : 1.7f;
            moon.shadows = LightShadows.Soft;
            moon.shadowStrength = .76f;
            moon.shadowBias = 0.04f;
            moon.shadowNormalBias = 0.35f;

            // --- R (2012) Atmospheric Warm/Cool Contrast Lighting (auto-routed to LOC01..04!) ---
            var coolNight = new Color(0.48f, 0.68f, 0.84f);
            var warmSodium = new Color(0.95f, 0.72f, 0.44f);
            var subtleBounce = new Color(.65f, .73f, .76f);

            var canopyLamp = Lamp("Key below entry canopy", LightType.Spot, new Vector3(4.0f, 2.68f, -8.65f), 4.1f, 12.0f, warmSodium, permanent, shadow: art);
            canopyLamp.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            canopyLamp.spotAngle = 124f;
            canopyLamp.innerSpotAngle = 68f;
            canopyLamp.shadowStrength = 0.85f;
            Lamp("Street wash at the gate", LightType.Point, new Vector3(5.85f, 4.10f, -21.15f), 4.2f, 15.0f, new Color(.82f, .83f, .78f), permanent);
            if (art)
            {
                Lamp("Sodium spill reflected by the wet court", LightType.Point, new Vector3(3.07f, 1.65f, -12.8f),
                    1.45f, 11.2f, new Color(.85f, .78f, .65f), real);
                var threshold = Lamp("Street-side wash along gate ironwork", LightType.Spot, new Vector3(3.9f, 3.85f, -23.3f),
                    3.8f, 10.5f, new Color(.82f, .80f, .72f), real);
                threshold.transform.LookAt(new Vector3(4.15f, 1.55f, -20.0f));
                threshold.spotAngle = 91f;
                threshold.innerSpotAngle = 51f;
                Lamp("Return from under the entry eave", LightType.Point, new Vector3(5.42f, 1.53f, -10.8f),
                    .91f, 8.6f, subtleBounce, real);
            }
            Lamp("Bounced entry spill", LightType.Point, new Vector3(4.6f, 2.35f, -4.5f), 1.24f, 8.8f, coolNight, permanent);
            Lamp("Emergency glass return", LightType.Point, new Vector3(6.20f, 2.65f, 19.48f), 1.48f, 9.8f, coolNight, permanent);
            Lamp("Wayfinding on staff corridor", LightType.Point, new Vector3(0.2f, 2.66f, 8.3f), 0.72f, 11.0f, coolNight, permanent);

            // Calibrated horror bounce fills in R (tight Forward+ cluster ranges so zero light bleeds into Store Room or Corridor!)
            Lamp("Room 101 floor bounce R", LightType.Point, new Vector3(9.32f, 1.76f, 5.46f), 1.62f, 6.8f, subtleBounce, real);
            Lamp("Back wall warm dust bounce R", LightType.Point, new Vector3(12.15f, 2.50f, 4.02f), .95f, 5.0f, new Color(0.80f, 0.74f, 0.60f), real);
            Lamp("Dry store readable spill R", LightType.Point, new Vector3(15.6f, 2.45f, 3.65f), 1.72f, 4.6f, new Color(0.86f, 0.78f, 0.62f), real);
            var corrSpill = Lamp("Corridor doorway spill R", LightType.Spot, new Vector3(6.6f, 2.45f, 4.75f), 3.2f, 9.2f, coolNight, real, shadow: art);
            corrSpill.transform.LookAt(new Vector3(3.1f, 0.35f, 4.75f));
            corrSpill.spotAngle = 82f;
            corrSpill.innerSpotAngle = 42f;
            Lamp("Corridor south spill R", LightType.Point, new Vector3(4.0f, 2.55f, 2.45f), 0.92f, 5.8f, coolNight, real);
            Lamp("Corridor ambient depth R", LightType.Point, new Vector3(4.0f, 2.55f, 5.25f), 1.12f, 7.6f, coolNight, real);
            Lamp("Corridor mid bounce R", LightType.Point, new Vector3(4.0f, 2.55f, 10.6f), 1.10f, 7.2f, coolNight, real);
            Lamp("Corridor far red emergency hint R", LightType.Point, new Vector3(4.0f, 2.75f, 14.15f), 1.15f, 7.2f, new Color(0.88f, 0.28f, 0.22f), real);
            // STAFF Corridor R (2012): tightly clustered ranges (<=5.4m) so STAFF lights never bleed into HALL Forward+ tiles!
            Lamp("Staff desk ambient R", LightType.Point, new Vector3(-0.12f, 2.15f, 3.55f), 0.78f, 5.2f, new Color(0.52f, 0.68f, 0.78f), real);
            Lamp("Staff north red emergency hint R", LightType.Point, new Vector3(0.05f, 2.68f, 13.65f), 0.96f, 5.4f, new Color(0.88f, 0.28f, 0.22f), real);
            Lamp("Desk readable spill R", LightType.Point, new Vector3(2.90f, 2.26f, -3.86f), 1.45f, 8.05f, new Color(0.66f, 0.76f, 0.78f), real);
            // Focused warm tungsten desk lamp cone emanating directly from inside the sculpted green-enamel bell shade!
            var deskConeR = Lamp("Reception desk lamp warm glow", LightType.Spot, new Vector3(1.56f, 1.50f, -3.94f), 2.15f, 3.60f, new Color(1.0f, 0.78f, 0.48f), permanent, shadow: false);
            deskConeR.transform.LookAt(new Vector3(1.58f, 1.12f, -3.45f));
            deskConeR.spotAngle = 108f;
            deskConeR.innerSpotAngle = 42f;
            var deskConeB = Lamp("Reception desk lamp active B", LightType.Spot, new Vector3(1.56f, 1.50f, -3.94f), 1.68f, 3.85f, new Color(1.0f, 0.85f, 0.60f), report, shadow: false);
            deskConeB.transform.LookAt(new Vector3(1.52f, 1.12f, -3.40f));
            deskConeB.spotAngle = 114f;
            deskConeB.innerSpotAngle = 48f;
            // Subtle cool monsoon entry spill from courtyard doorway into abandoned R lobby (sculpts counter relief & wet floor sheen!)
            var entrySpillR = Lamp("Reception entry monsoon spill R", LightType.Spot, new Vector3(4.05f, 2.38f, -7.75f), 2.65f, 9.2f, new Color(0.56f, 0.74f, 0.90f), real, shadow: false);
            entrySpillR.transform.LookAt(new Vector3(2.15f, 0.82f, -3.40f));
            entrySpillR.spotAngle = 96f;
            entrySpillR.innerSpotAngle = 42f;
            // Subtle warm lobby floor/apron bounce in B so carved ironwood wainscot, brass kickplate & built-in drawer read with 3D depth
            Lamp("Reception counter front fill B", LightType.Point, new Vector3(3.15f, 0.98f, -3.95f), 0.78f, 5.8f, new Color(0.92f, 0.90f, 0.80f), report);
            // Warm localized gooseneck reading lamp pool over STAFF nursing desk in 2005 B (dark & cold in 2012 R!)
            var staffSconceB = Lamp("Staff desk sconce B", LightType.Spot, new Vector3(0.68f, 1.56f, 3.30f), 2.35f, 3.8f, new Color(1.0f, 0.86f, 0.62f), report, shadow: false);
            staffSconceB.transform.LookAt(new Vector3(0.88f, 0.90f, 3.45f));
            staffSconceB.spotAngle = 112f;
            staffSconceB.innerSpotAngle = 46f;

            // --- B (2005) Institutional Operational Fluorescent T8 Fixtures ---
            var cleanTube = new Color(.94f, .97f, .89f);
            Tube(new Vector3(4.00f, 3.59f, 4.75f), 0f, cleanTube, 3.1f, castShadow: art);
            Tube(new Vector3(4.00f, 3.59f, 10.60f), 0f, cleanTube, 2.5f, castShadow: false);
            Tube(new Vector3(1.28f, 3.59f, -3.21f), 0f, cleanTube, 2.35f, castShadow: art);
            Tube(new Vector3(6.32f, 3.59f, -4.58f), 0f, cleanTube, 2.75f, castShadow: false);
            // Room 101 T8 fluorescent fixtures offset cleanly clear of ceiling fan sweep circles (West N bay & East S Bed-4 bay!)
            Tube(new Vector3(7.70f, 3.59f, 6.55f), 90f, cleanTube, 3.4f, castShadow: art);
            var bed4Tube = Tube(new Vector3(11.65f, 3.59f, 3.45f), 90f, cleanTube, 3.1f, castShadow: art);
            Tube(new Vector3(0.05f, 3.59f, 3.85f), 0f, cleanTube, 2.15f, castShadow: false, range: 5.4f);
            Tube(new Vector3(0.05f, 3.59f, 11.80f), 0f, cleanTube, 2.05f, castShadow: false, range: 5.4f);
            if (multiScene && bed4Tube != null) zones["LOC03"].region.bed4TubeLight = bed4Tube;
            else if (bed4Tube != null) world.bed4TubeLight = bed4Tube;

            Lamp("Room 101 GI bounce B", LightType.Point, new Vector3(9.65f, 0.92f, 5.0f), 0.88f, 7.2f, new Color(0.76f, 0.82f, 0.72f), report);
            Lamp("Corridor GI bounce B", LightType.Point, new Vector3(4.0f, 0.95f, 5.2f), 0.68f, 7.4f, new Color(0.74f, 0.80f, 0.72f), report);
            Lamp("Corridor GI bounce N B", LightType.Point, new Vector3(4.0f, 0.98f, 11.6f), 0.62f, 6.2f, new Color(0.74f, 0.80f, 0.72f), report);
            Lamp("Staff GI bounce B", LightType.Point, new Vector3(0.05f, 1.02f, 7.60f), 0.58f, 5.6f, new Color(0.76f, 0.82f, 0.74f), report);
            // Soft operational B-only threshold fills at north ER Door A & Door B so sage enamel & stainless kickplates read crisply in 2005 B
            // while keeping 2012 R purely crimson-dramatic (tight Forward+ ranges prevent cross-corridor tile overlap)!
            Lamp("Hall north ER threshold fill B", LightType.Point, new Vector3(4.00f, 3.05f, 14.35f), 0.95f, 4.4f, new Color(0.90f, 0.95f, 0.88f), report);
            Lamp("Staff north ER threshold fill B", LightType.Point, new Vector3(0.05f, 3.05f, 14.35f), 0.82f, 3.8f, new Color(0.90f, 0.95f, 0.88f), report);
            Lamp("Reception GI bounce B", LightType.Point, new Vector3(3.35f, 1.10f, -3.5f), .83f, 7.4f, new Color(.86f, .87f, .81f), report);

            if (!art)
                return;

            // --- Hero Shadow-Casting Window Spotlights in R ---
            var winSpecs = new (float x, float intensity, bool shadow)[]
            {
                (7.35f, 8.3f, true),
                (10.28f, 7.5f, true),
                (12.39f, 5.0f, false)
            };
            foreach (var w in winSpecs)
            {
                var from = new Vector3(w.x, 2.55f, 10.45f);
                var to = new Vector3(w.x + 0.35f, 0.15f, 4.35f);
                var beam = Lamp("Window key moonlight", LightType.Spot, from, w.intensity, 9.2f, new Color(0.58f, 0.78f, 0.94f), real, shadow: w.shadow);
                beam.transform.LookAt(to);
                beam.spotAngle = 62f;
                beam.innerSpotAngle = 32f;
                if (w.shadow)
                {
                    beam.shadowStrength = 0.90f;
                    beam.shadowBias = 0.025f;
                    beam.shadowNormalBias = 0.22f;
                    var urpLight = beam.GetUniversalAdditionalLightData();
                    if (urpLight != null)
                    {
                        var so = new SerializedObject(urpLight);
                        var tierProp = so.FindProperty("m_AdditionalLightsShadowResolutionTier");
                        if (tierProp != null) tierProp.intValue = UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                if (w.shadow) LightShaft(new Vector3(w.x, 1.92f, 7.98f), to, .71f, .72f, real);
            }
            Lamp("Courtyard distant amber glow", LightType.Point, new Vector3(10.2f, 3.2f, 13.8f), 6.8f, 16f, new Color(0.92f, 0.64f, 0.36f), real);

            // --- Zone-Owned Box-Projected Reflection Probes (LOC03 Ward + HALL + STAFF; LOC01/02/04 added in Zones.cs!) ---
            var rp101 = CreateReflectionProbe("Local reflection · LOC03", new Vector3(9.71f, 1.75f, 5.05f), new Vector3(8.4f, 3.65f, 6.2f), ResolveParent(permanent, new Vector3(9.71f, 0, 5.05f)));
            var rpHall = CreateReflectionProbe("Local reflection · HALL", new Vector3(4.0f, 1.75f, 8.0f), new Vector3(3.3f, 3.65f, 16.0f), ResolveParent(permanent, new Vector3(4.0f, 0, 8.0f)));
            var rpStaff = CreateReflectionProbe("Local reflection · STAFF", new Vector3(0.05f, 1.75f, 8.0f), new Vector3(2.35f, 3.65f, 16.0f), ResolveParent(permanent, new Vector3(0.05f, 0, 8.0f)));
            if (multiScene)
            {
                zones["LOC03"].probes.Add(rp101);
                zones["LOC03"].probes.Add(rpHall);
                zones["LOC03"].probes.Add(rpStaff);
            }
            else
            {
                world.localReflection = rp101;
                world.corridorReflection = rpHall;
            }

            // --- Hero Cycles-Baked 3-Blade Enamel Ceiling Fans (centered in structural bays, 1.5m+ clear of T8 tubes & curtain tracks!) ---
            Fan(new Vector3(7.70f, 3.58f, 4.95f), "CTCC_LOC02_CeilingFan");
            Fan(new Vector3(11.65f, 3.58f, 5.12f), "CTCC_LOC02_CeilingFan");
            Fan(new Vector3(4.30f, 3.58f, -3.94f), "CTCC_LOC02_CeilingFan");

            // --- Weather & Atmosphere: Monsoon Rain, Exterior Courtyard Mist, Dust Motes & Volumetric Cones (Upgrade #7) ---
            Rain("Light court rain", new Vector3(9.72f, 4.1f, 9.2f), new Vector3(9.2f, .18f, 2.2f), 285,
                multiScene ? zones["LOC03"].real : real);
            Rain("Exterior rain", new Vector3(4f, 4.6f, -15.18f), new Vector3(15f, .21f, 9.4f), 370, real);
            Rain("Canopy eave runoff", new Vector3(5.94f, 2.73f, -11.06f), new Vector3(.66f, .025f, .060f), 23, real, runoff: true);
            Rain("Ward 101 eave runoff", new Vector3(10.56f, 2.92f, 8.19f), new Vector3(.50f, .03f, .060f), 18,
                multiScene ? zones["LOC03"].real : real, runoff: true);
            if (multiScene) AddCh01Puddles();
            DustMotes("Room 101 floating dust motes R", new Vector3(9.35f, 1.65f, 5.85f), new Vector3(5.6f, 2.1f, 3.4f), real,
                new Color(0.70f, 0.84f, 0.88f, 0.22f), maxParticles: 110, rate: 16f);
            DustMotes("Corridor floating dust motes B", new Vector3(4.00f, 2.15f, 7.60f), new Vector3(2.4f, 2.2f, 10.5f), report,
                new Color(0.78f, 0.90f, 0.80f, 0.19f), maxParticles: 65, rate: 10f);
            DustMotes("Corridor floating dust motes R", new Vector3(4.00f, 2.00f, 7.60f), new Vector3(2.4f, 2.0f, 10.5f), real,
                new Color(0.62f, 0.78f, 0.86f, 0.17f), maxParticles: 55, rate: 8f);
            DustMotes("Reception floating dust motes B", new Vector3(4.10f, 2.05f, -3.80f), new Vector3(5.4f, 2.0f, 4.4f), report,
                new Color(0.84f, 0.90f, 0.82f, 0.17f), maxParticles: 48, rate: 7.5f);
            CourtyardMist(new Vector3(4.0f, 0.72f, -15.6f), new Vector3(14.5f, 0.85f, 12.6f), real);

            // Volumetric downward light cones under corridor fluorescent tubes & exterior canopy (ALL use smooth _CTCC_ROUND_BEAM!)
            VolumetricCone("Corridor tube cone S B", new Vector3(4.00f, 3.54f, 4.75f), new Vector3(4.00f, 0.85f, 4.75f), 0.36f, 1.18f, "BeamFluorescent", report, "CH01_HallBeamS");
            VolumetricCone("Corridor tube cone N B", new Vector3(4.00f, 3.54f, 10.60f), new Vector3(4.00f, 0.85f, 10.60f), 0.36f, 1.18f, "BeamFluorescent", report, "CH01_HallBeamN");
            VolumetricCone("Canopy sodium cone", new Vector3(4.00f, 2.66f, -8.65f), new Vector3(4.00f, 0.10f, -9.20f), 0.34f, 1.85f, "BeamWarm", permanent, "CH01_CanopyBeam");

            // --- Cinematic Horror URP Post-Processing Profiles (Calibrated R 2012 vs B 2005!) ---
            world.realGrade = Grade("2012", exposure: .53f, saturation: -12f, contrast: 5f,
                filter: new Color(.955f, .976f, 1f), vignette: .21f, grain: .13f, ca: .022f,
                shadowTone: new Color(.42f, .48f, .51f), highlightTone: new Color(.64f, .59f, .54f), bloomStrength: .21f);
            world.reportGrade = Grade("2005", exposure: .05f, saturation: -8f, contrast: 6f,
                filter: new Color(.987f, 1f, .972f), vignette: .14f, grain: .07f, ca: .012f,
                shadowTone: new Color(.463f, .51f, .49f), highlightTone: new Color(.57f, .55f, .50f), bloomStrength: .14f);

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
            // Four levels of falloff across the glass and the chair, shared between the R/B bakes.
            AddGrid(.32f, 2.28f, 3, -3.94f, -2.66f, 3, new[] { 1.20f, 1.94f });
            AddGrid(2.05f, 5.98f, 3, -11.05f, -10.44f, 2, new[] { .44f, 1.42f });
            // LOC03 Main Corridor + Staff Corridor (dense 3-tier Dual GI sampling across HALL & STAFF + DOOR-STAFF cross-passage!)
            AddGrid(3.0f, 5.0f, 2, 0.8f, 15.2f, 7, interiorHeights);
            AddGrid(-0.55f, 0.72f, 2, 0.9f, 15.1f, 6, interiorHeights);
            pts.Add(new Vector3(1.80f, 1.35f, 7.50f));
            pts.Add(new Vector3(1.80f, 2.10f, 7.50f));
            // LOC03 Room 101 + Store Room (Kho giường)
            AddGrid(6.2f, 13.2f, 5, 2.5f, 7.5f, 4, interiorHeights);
            AddGrid(14.3f, 16.6f, 2, 2.5f, 5.0f, 2, new[] { 0.75f, 1.85f });
            // LOC04 Emergency Bay
            AddGrid(0.2f, 8.2f, 4, 16.8f, 21.4f, 3, interiorHeights);
            // LOC05–08 Upper Floors (Khu B +4m, Archive & Director +8m) + B1 Technical / Morgue / E05b Gallery (-3.4m)
            AddGrid(-2.6f, 6.4f, 4, 32.5f, 45.5f, 4, new[] { 4.6f, 5.8f, 6.9f });
            AddGrid(-2.6f, 7.2f, 4, 30.0f, 38.0f, 3, new[] { 8.6f, 9.8f, 10.8f });
            AddGrid(0.0f, 14.0f, 4, 32.0f, 45.0f, 3, new[] { -2.8f, -1.6f });
            AddGrid(12.5f, 36.2f, 5, 42.0f, 50.8f, 2, new[] { -2.8f, -1.6f });
            // Inner Light Court (Sân sáng) & Front Campus Approach for accurate exterior GI bounce
            AddGrid(6.5f, 13.5f, 2, 9.0f, 13.2f, 2, exteriorHeights);
            AddGrid(-18.0f, 24.0f, 4, -24.0f, -12.0f, 2, new[] { 1.2f, 4.8f });

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
                var softQuality = pipeSo.FindProperty("m_SoftShadowQuality");
                if (softQuality != null) softQuality.intValue = 1;
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

        private Light Tube(Vector3 p, float yaw, Color clean, float intensity, bool castShadow = false, float range = 7.8f)
        {
            var lit = Lamp("Mains tube pool", castShadow ? LightType.Spot : LightType.Point,
                p + Vector3.down * 0.14f, castShadow ? intensity * 2.2f : intensity, range, clean, report, shadow: castShadow);
            if (castShadow)
            {
                lit.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                lit.spotAngle = 128f;
                lit.innerSpotAngle = 72f;
                lit.shadowStrength = 0.72f;
            }
            if (multiScene) ZoneAt(p).tubes.Add(lit);
            else livingTubes.Add(lit);
            if (!art) return lit;
            var fixture = PlaceBlenderModel("CTCC_ENV_FluorescentFixture", permanent, p, yaw, pairRB: true);
            staticArchitecture.Add(fixture);
            return lit;
        }

        private void Fan(Vector3 p, string modelName = "CTCC_ENV_CeilingFan")
        {
            var fan = PlaceBlenderModel(modelName, permanent, p, 0f, pairRB: true);
            var rotor = FindDescendant(fan.transform, "FanRotor");
            if (rotor != null)
            {
                if (multiScene) ZoneAt(p).fans.Add(rotor);
                else movingFans.Add(rotor);
            }
        }

        private Volume Grade(string key, float exposure, float saturation, float contrast, Color filter,
            float vignette, float grain, float ca, Color shadowTone, Color highlightTone, float bloomStrength)
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
            bloom.intensity.Override(bloomStrength);
            bloom.threshold.Override(1.18f);
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

        private void VolumetricCone(string name, Vector3 top, Vector3 bottom, float topRadius, float bottomRadius, string matKey, Transform parent, string roundMatName = null)
        {
            var node = Group(name, ResolveParent(parent, top));
            const int segs = 16;
            var verts = new Vector3[(segs + 1) * 2];
            var uvs = new Vector2[(segs + 1) * 2];
            var tris = new int[segs * 6]; // Single-sided outward open frustum (half the overdraw, zero hard back-face double-blend!)
            int ti = 0;
            for (int i = 0; i <= segs; i++)
            {
                float a = (i / (float)segs) * Mathf.PI * 2f;
                float u = i / (float)segs;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                verts[i * 2] = top + dir * topRadius;
                verts[i * 2 + 1] = bottom + dir * bottomRadius;
                uvs[i * 2] = new Vector2(u, 0f);
                uvs[i * 2 + 1] = new Vector2(u, 1f);
                if (i < segs)
                {
                    int a0 = i * 2, a1 = a0 + 1, b0 = a0 + 2, b1 = a0 + 3;
                    // Unity left-handed clockwise outward front-face winding (works with _Cull = Back!)
                    tris[ti++] = a0; tris[ti++] = a1; tris[ti++] = b0;
                    tris[ti++] = b0; tris[ti++] = a1; tris[ti++] = b1;
                }
            }
            var mesh = new Mesh { name = name, vertices = verts, uv = uvs, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            StoreMesh(mesh);
            node.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = node.gameObject.AddComponent<MeshRenderer>();
            Material mat = Mat(matKey);
            if (!string.IsNullOrEmpty(roundMatName))
            {
                mat = RebirthMaterial(roundMatName, mat);
                mat.SetVector("_ConeTop", new Vector4(top.x, top.y, top.z, 0f));
                mat.SetVector("_ConeBottom", new Vector4(bottom.x, bottom.y, bottom.z, 0f));
                mat.SetFloat("_Cull", (float)CullMode.Back);
                mat.EnableKeyword("_CTCC_ROUND_BEAM");
                EditorUtility.SetDirty(mat);
            }
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private void DustMotes(string name, Vector3 center, Vector3 boxSize, Transform layerRoot, Color tint, int maxParticles = 85, float rate = 12f)
        {
            var p = Group(name, ResolveParent(layerRoot, center), center).gameObject.AddComponent<ParticleSystem>();
            var main = p.main;
            main.loop = true;
            main.startLifetime = 6.5f;
            main.startSpeed = 0.024f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.005f, 0.011f);
            main.startColor = tint;
            main.maxParticles = maxParticles;
            main.prewarm = true;
            var shape = p.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = boxSize;
            var emission = p.emission;
            emission.rateOverTime = rate;
            var vel = p.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-0.032f, 0.032f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.018f, 0.016f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.032f, 0.032f);

            const string path = Root + "/Generated/Materials/DustMote.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_BaseMap", Tex("SoftParticle"));
            Transparent(mat, new Color(0.84f, 0.92f, 0.94f, 0.24f));
            var renderer = p.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            if (multiScene) ZoneAt(center).atmosphericParticles.Add(p);
            else atmosphericParticles.Add(p);
        }

        private void CourtyardMist(Vector3 center, Vector3 boxSize, Transform layerRoot)
        {
            var p = Group("LOC01 exterior courtyard mist", ResolveParent(layerRoot, center), center).gameObject.AddComponent<ParticleSystem>();
            var main = p.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 15f);
            main.startSpeed = 0.06f;
            main.startSize = new ParticleSystem.MinMaxCurve(2.8f, 4.8f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(0.62f, 0.78f, 0.86f, 0.24f);
            main.maxParticles = 48;
            main.prewarm = true;
            var shape = p.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = boxSize;
            var emission = p.emission;
            emission.rateOverTime = 4.6f;
            var vel = p.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(0.10f, 0.22f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.01f, 0.02f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
            var colLife = p.colorOverLifetime;
            colLife.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.88f, 0.28f), new GradientAlphaKey(0.82f, 0.72f), new GradientAlphaKey(0f, 1f) });
            colLife.color = new ParticleSystem.MinMaxGradient(grad);

            const string path = Root + "/Generated/Materials/CourtyardMist.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_BaseMap", Tex("CourtyardMistTex"));
            Transparent(mat, new Color(0.72f, 0.84f, 0.90f, 0.28f));
            var renderer = p.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            if (multiScene) ZoneAt(center).atmosphericParticles.Add(p);
            else atmosphericParticles.Add(p);
        }

        private void Rain(string name, Vector3 position, Vector3 footprint, float rate, Transform parent, bool runoff = false)
        {
            var p = Group(name, ResolveParent(parent, position), position).gameObject.AddComponent<ParticleSystem>();
            var main = p.main;
            main.loop = true;
            main.startLifetime = runoff ? new ParticleSystem.MinMaxCurve(.48f, .62f) : new ParticleSystem.MinMaxCurve(.48f, .65f);
            main.startSpeed = 0f;
            main.startSize = runoff ? new ParticleSystem.MinMaxCurve(.007f, .013f) : new ParticleSystem.MinMaxCurve(.004f, .009f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(.48f, .57f, .66f, .19f),
                new Color(.66f, .76f, .84f, runoff ? .50f : .37f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = runoff ? 24 : 270;
            var shape = p.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = footprint;
            var emission = p.emission;
            emission.rateOverTime = rate;
            var velocity = p.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = runoff ? new ParticleSystem.MinMaxCurve(-.10f, .14f) : new ParticleSystem.MinMaxCurve(1.06f, 1.83f);
            velocity.y = runoff ? new ParticleSystem.MinMaxCurve(-5.0f, -4.55f) : new ParticleSystem.MinMaxCurve(-9.7f, -7.3f);
            velocity.z = runoff ? new ParticleSystem.MinMaxCurve(-.08f, .08f) : new ParticleSystem.MinMaxCurve(-.61f, -.14f);
            const string path = Root + "/Generated/Materials/WeatherRain.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            Transparent(mat, new Color(.79f, .87f, .92f, .62f));
            var renderer = p.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mat;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = runoff ? 1.45f : 2.1f;
            renderer.velocityScale = runoff ? .018f : .037f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            var particles = multiScene ? ZoneForTransform(p.transform).atmosphericParticles : atmosphericParticles;
            particles.Add(p);
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
                Transform loopParent = multiScene ? ZoneAt(point).permanent : root;
                var item = Source("Loop · " + id, loopParent, point, max);
                item.loop = true;
                item.clip = Clip(id);
                var rl = new RoomLoop { source = item, level = level, report = b, exteriorOccluded = occluded };
                if (multiScene)
                {
                    var zone = ZoneAt(point);
                    zone.loops.Add(rl);
                    if (zone.id == "LOC02" && id == "Fan") zone.region.receptionFan = item;
                }
                else bootstrapLoops.Add(rl);
            }

            AddLoop("Rain", new Vector3(4, 1.2f, -12.2f), 0.62f, 24, false, occluded: true);
            AddLoop("Roof", new Vector3(9.4f, 2.3f, 8.85f), 0.72f, 15, false, occluded: true);
            AddLoop("Roof", new Vector3(-16.0f, 3.2f, -10.0f), 0.44f, 28, false, occluded: true);
            AddLoop("Rumble", new Vector3(5.52f, 2.2f, -10.0f), 0.48f, 24, false, occluded: true);
            AddLoop("Drips", new Vector3(5.97f, .30f, -10.98f), .25f, 6.5f, false);
            AddLoop("Drips", new Vector3(10.42f, .37f, 8.46f), .50f, 8f, false);
            AddLoop("Drips", new Vector3(35.8f, -2.5f, 42.0f), 0.56f, 10, false, occluded: false);
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
            bus.drawerSlide = Clip("DrawerSlide");
            bus.paperFlip = Clip("PaperFlip");
            bus.clockTick = Clip("ClockTick");
            bus.distantSteps = Clip("DistantSteps");
            bus.chairScrape = Clip("ChairScrape");
            bus.partitionKnock = Clip("PartitionKnock");
            bus.lanMotif = Clip("MUS_LanMotif");

            // Load canonical Vietnamese Temp VO clips (TASK009-B) with hard LOCK-V22-02 & DEC-VO-01 needsVO filter!
            var allowedVoIds = LoadAllowedVoIdsFromDialogueJson();
            var voList = new List<VoiceClipEntry>();
            int skippedNonVo = 0;
            string voDir = audioPath + "VO/";
            if (Directory.Exists(voDir))
            {
                foreach (var wav in Directory.GetFiles(voDir, "*.wav").OrderBy(x => x))
                {
                    string id = Path.GetFileNameWithoutExtension(wav);
                    if (id == "CH01-D16b" || id == "CH04-D06") continue;
                    if (allowedVoIds.Count > 0 && !allowedVoIds.Contains(id))
                    {
                        skippedNonVo++;
                        continue;
                    }
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(wav.Replace('\\', '/'));
                    if (clip != null) voList.Add(new VoiceClipEntry { id = id, clip = clip });
                }
            }
            bus.voiceClips = voList.ToArray();
            Debug.Log($"CTCC_AUDIO_WIRED {voList.Count} VO clips (DEC-VO-01 filtered out {skippedNonVo} non-VO WAVs), lanMotif={(bus.lanMotif != null)}");

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

        [Serializable]
        private sealed class DialogueFilterVariant
        {
            public string variantId;
            public bool needsVO;
        }

        [Serializable]
        private sealed class DialogueFilterEntry
        {
            public string id;
            public bool needsVO;
            public DialogueFilterVariant[] variants;
        }

        [Serializable]
        private sealed class DialogueFilterSheet
        {
            public DialogueFilterEntry[] entries;
        }

        /// <summary>
        /// Core v3.6.2 (DEC-VO-01 / D6): Only audio clips explicitly flagged needsVO=true in Dialogue.json
        /// may be wired into the build, excluding all 9 forbidden CH01 WAVs and 4 forbidden CH02-03 WAVs.
        /// </summary>
        private static HashSet<string> LoadAllowedVoIdsFromDialogueJson()
        {
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string jsonPath = Root + "/Art/Resources/CTCC/Dialogue.json";
            if (!File.Exists(jsonPath))
                return allowed;
            var sheet = JsonUtility.FromJson<DialogueFilterSheet>(File.ReadAllText(jsonPath));
            if (sheet?.entries == null)
                return allowed;
            foreach (var entry in sheet.entries)
            {
                if (entry == null) continue;
                if (entry.variants != null && entry.variants.Length > 0)
                {
                    foreach (var v in entry.variants)
                    {
                        if (v != null && v.needsVO && !string.IsNullOrEmpty(v.variantId))
                            allowed.Add(v.variantId);
                    }
                    // Also honor top-level ID if any variant uses the parent ID with needsVO=true
                    if (entry.needsVO && !string.IsNullOrEmpty(entry.id) &&
                        !Array.Exists(entry.variants, v => v != null && v.variantId == entry.id && !v.needsVO))
                        allowed.Add(entry.id);
                }
                else if (entry.needsVO && !string.IsNullOrEmpty(entry.id))
                {
                    allowed.Add(entry.id);
                }
            }
            return allowed;
        }
    }
}
