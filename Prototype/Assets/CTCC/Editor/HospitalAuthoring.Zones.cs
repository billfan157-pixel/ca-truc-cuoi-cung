using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CTCC.Editor
{
    public sealed partial class HospitalAuthoring
    {
        /// <summary>
        /// High-fidelity M2 Environment Dressing & Character Staging across LOC01–LOC04 using
        /// our Blender 4.5 architectural modules, historical props, PBR materials, and rigged characters.
        /// Every placement strictly respects Core 03 / layout.json clear corridors and AP00–AP05 pads.
        /// </summary>
        private void BuildM2ZoneDressing()
        {
            DressLoc05ArchiveWing();
            if (!art) return;
            DressLoc01Exterior();
            DressLoc02Reception();
            DressLoc03Ward101AndStore();
            DressLoc04Emergency();
        }

        private void DressLoc01Exterior()
        {
            var loc01 = multiScene ? zones["LOC01"] : null;
            Transform perm = loc01 != null ? loc01.permanent : permanent;
            Transform realRoot = loc01 != null ? loc01.real : real;

            // Replace flat exterior approach ground with gleaming rain-soaked wet asphalt surface
            var wet = CreateCh01Asphalt();
            Surface("Exterior wet asphalt approach", new Vector3(4.0f, -0.095f, -23.0f),
                new Vector3(18.0f, 0.20f, 7.2f), wet.name, "ConcretePavingB", solid: true,
                tile: 3.2f, parent: perm);
            Surface("Courtyard wet concrete apron", new Vector3(4.0f, -0.092f, -14.0f),
                new Vector3(17.8f, 0.20f, 11.8f), wet.name, "ConcretePavingB", tile: 3.6f, parent: perm);

            // Note: Full 3-story grounded K-A 1987 hospital elevation (33m wide x 11.7m tall + roof) is built
            // in Campus_Grounds (HospitalAuthoring.Campus.cs) so it never floats above the courtyard canopy!

            // Brick Security Gatehouse flanking east side of outer gate (strictly outside walking lane x in [2.8..5.2]!)
            var gatehouse = PlaceBlenderModel("CTCC_ENV_Gatehouse", perm, new Vector3(8.55f, 0f, -18.55f), 0f, pairRB: false);
            Box("Gatehouse collider", new Vector3(8.55f, 1.35f, -18.55f), new Vector3(2.85f, 2.70f, 2.55f),
                Mat("WeatheredBrick"), perm, solid: true).GetComponent<Renderer>().enabled = false;
            staticArchitecture.Add(gatehouse);
            // Warm tungsten security gatehouse porch spill anchored outside the gate-facing WEST (-X) caged lamp globe
            // plus soft abandoned guardhouse interior spill so the booth windows read warmly without blowing out the wall!
            Lamp("Gatehouse warm porch light", LightType.Point, new Vector3(6.85f, 2.22f, -19.45f), 1.65f, 7.8f,
                new Color(0.98f, 0.72f, 0.40f), perm);
            Lamp("Gatehouse interior desk glow", LightType.Point, new Vector3(8.35f, 1.48f, -18.75f), 0.95f, 4.2f,
                new Color(0.96f, 0.76f, 0.44f), perm);
            // Subtle cool monsoon moonlight rim illuminating Minh's motorbike & west brick gate flank (prevents black crush!)
            Lamp("Exterior motorbike cool rim R", LightType.Point, new Vector3(1.25f, 2.35f, -22.85f), 1.25f, 6.8f,
                new Color(0.52f, 0.72f, 0.88f), realRoot);

            // Authentic An Sinh 1987 Enamel Gate Plaque on west gate pillar + Night Gate subtitle on east pillar
            Sign("Hospital gate plaque", "Sign_HospitalGate", new Vector3(2.58f, 1.48f, -20.19f), Vector3.back,
                new Vector2(0.76f, 0.34f), perm, metalBacking: true);
            Sign("Night gate notice plaque", "Sign_NightGate", new Vector3(5.38f, 1.50f, -20.19f), Vector3.back,
                new Vector2(0.74f, 0.24f), perm, metalBacking: true);

            // Freestanding An Sinh Hospital 6-Block Campus Wayfinding Directory Board on west courtyard flank
            // Placed cleanly at x=-3.65, z=-14.8 (well outside walking axis x=[1.5..6.5] and clear of terrazzo benches at x=-1.35!)
            var mapBoard = Group("Campus wayfinding directory board", perm, new Vector3(-3.65f, 0f, -14.8f));
            mapBoard.localRotation = Quaternion.Euler(0f, 55f, 0f);
            Box("Map post L", new Vector3(-0.78f, 0.96f, 0f), new Vector3(0.08f, 1.92f, 0.08f), Mat("MetalOld"), mapBoard, solid: true, isStatic: true);
            Box("Map post R", new Vector3(0.78f, 0.96f, 0f), new Vector3(0.08f, 1.92f, 0.08f), Mat("MetalOld"), mapBoard, solid: true, isStatic: true);
            Box("Map backing", new Vector3(0f, 1.38f, 0f), new Vector3(1.66f, 1.24f, 0.06f), Mat("MetalOld"), mapBoard, solid: true, isStatic: true);
            Box("Map rain cap", new Vector3(0f, 2.02f, -0.04f), new Vector3(1.80f, 0.08f, 0.34f), Mat("CorrugatedRoofR"), mapBoard, isStatic: true);
            Flat("Campus directory graphic", "Sign_CampusMap", new Vector3(0f, 1.38f, -0.036f), Vector3.back, new Vector2(1.56f, 1.16f), mapBoard);
            staticArchitecture.Add(mapBoard.gameObject);

            // Perimeter Iron Fence Bays seated cleanly on ground y=0 (each module already includes 0.90m brick plinth,
            // coping, iron spear railings & 2.28m masonry pier matching the 2.14m outer gate pillars — zero 4m double-stacking!).
            foreach (float z in new[] { -18.0f, -14.0f, -10.0f })
            {
                staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_CourtyardFenceBay", perm, new Vector3(-5.0f, 0f, z), 90f, pairRB: false, castShadows: false));
                staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_CourtyardFenceBay", perm, new Vector3(13.0f, 0f, z), -90f, pairRB: false, castShadows: false));
            }
            // Front left wall (x=[-5.0..2.65]): piers at -5.20 & -1.35, clear beside west gate pillar at x=2.64
            staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_CourtyardFenceBay", perm, new Vector3(-3.20f, 0f, -20.0f), 0f, pairRB: false, castShadows: false));
            staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_CourtyardFenceBay", perm, new Vector3(0.65f, 0f, -20.0f), 0f, pairRB: false, castShadows: false));
            // Front right wall (x=[5.35..13.20], yaw=180 puts piers at 9.35 & 13.20): open iron railings frame gatehouse & back Demolition board!
            staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_CourtyardFenceBay", perm, new Vector3(7.35f, 0f, -20.0f), 180f, pairRB: false, castShadows: false));
            staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_CourtyardFenceBay", perm, new Vector3(11.20f, 0f, -20.0f), 180f, pairRB: false, castShadows: false));

            // Minh's Parked Commuter Motorbike outside outer gate (x = 1.45, z = -22.25 — completely clear of AP00 at 4, -21.2!)
            var bike = PlaceBlenderModel("CTCC_PROP_CommuterMotorbike", perm, new Vector3(1.45f, 0f, -22.25f), 42f, pairRB: false);
            Box("Motorbike collider", new Vector3(1.45f, 0.55f, -22.25f), new Vector3(0.82f, 1.10f, 1.95f),
                Mat("Black"), perm, solid: true).GetComponent<Renderer>().enabled = false;
            staticArchitecture.Add(bike);
            // Soft amber motorbike headlamp cone sweeping across wet asphalt & outer gate bars!
            var headlamp = Lamp("Motorbike headlamp cone", LightType.Spot, new Vector3(1.85f, 0.96f, -21.75f), 5.6f, 12.0f,
                new Color(1.0f, 0.88f, 0.62f), realRoot, shadow: false);
            headlamp.transform.LookAt(new Vector3(4.35f, 0.25f, -18.8f));
            headlamp.spotAngle = 56f;
            headlamp.innerSpotAngle = 24f;

            // Cast Terrazzo Courtyard Benches along courtyard edges (clear of central walking axis x = 4!)
            staticArchitecture.Add(PlaceBlenderModel("CTCC_PROP_TerrazzoBench", perm, new Vector3(-1.35f, 0f, -13.2f), 90f, pairRB: false));
            Box("Courtyard bench L col", new Vector3(-1.35f, 0.42f, -13.2f), new Vector3(0.60f, 0.84f, 1.56f),
                Mat("TerrazzoBench"), perm, solid: true).GetComponent<Renderer>().enabled = false;
            staticArchitecture.Add(PlaceBlenderModel("CTCC_PROP_TerrazzoBench", perm, new Vector3(-1.35f, 0f, -16.4f), 90f, pairRB: false));
            Box("Courtyard bench L2 col", new Vector3(-1.35f, 0.42f, -16.4f), new Vector3(0.60f, 0.84f, 1.56f),
                Mat("TerrazzoBench"), perm, solid: true).GetComponent<Renderer>().enabled = false;

            // Concrete Storm Drain Grates & Curbs flanking the rain-drenched walkway
            foreach (float z in new[] { -17.2f, -13.8f, -10.4f })
            {
                staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_DrainGrateAndCurb", perm, new Vector3(1.75f, 0f, z), 90f, pairRB: false, castShadows: false));
                staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_DrainGrateAndCurb", perm, new Vector3(6.25f, 0f, z), -90f, pairRB: false, castShadows: false));
            }

            // Lush Tropical Elephant-Ear Shrubs (cây ráy / chuối cảnh) framing courtyard planters & trees
            var shrubSpots = new (Vector3 p, float yaw, float s)[]
            {
                (new Vector3(7.45f, 0.16f, -14.2f), 18f, 1.05f),
                (new Vector3(9.65f, 0.16f, -15.6f), 132f, 0.92f),
                (new Vector3(7.85f, 0.16f, -16.2f), 245f, 0.88f),
                (new Vector3(-3.35f, 0f, -11.4f), 55f, 1.10f),
                (new Vector3(-3.65f, 0f, -18.2f), 210f, 0.95f),
                (new Vector3(10.8f, 0f, -10.8f), 310f, 1.00f),
            };
            foreach (var sp in shrubSpots)
            {
                staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_TropicalShrub", perm, sp.p, sp.yaw,
                    pairRB: false, scale: Vector3.one * sp.s, castShadows: false));
            }

            // The crossfall sheds water toward the drain grates and the canopy pipe.
            Decal("CourtyardPuddle_1", "DampFloor", new Vector3(2.28f, 0.01f, -17.00f), Vector3.up, 1.98f, 1.0f, .12f, realRoot, .76f, 31);
            Decal("CourtyardPuddle_2", "DampFloor", new Vector3(5.62f, 0.01f, -13.79f), Vector3.up, 1.78f, .76f, .12f, realRoot, .71f, -23);
            Decal("ApproachPuddle", "DampFloor", new Vector3(5.09f, 0.01f, -21.50f), Vector3.up, 1.45f, .72f, .12f, realRoot, .74f, 24);

            // Exterior reflection probe for glistening wet asphalt reflections
            var extProbe = CreateReflectionProbe("Local reflection · LOC01", new Vector3(4.0f, 2.1f, -14.5f), new Vector3(18.0f, 7.0f, 16.0f), perm);
            if (loc01 != null) loc01.probes.Add(extProbe);
        }

        private void DressLoc02Reception()
        {
            var loc02 = multiScene ? zones["LOC02"] : null;
            Transform perm = loc02 != null ? loc02.permanent : permanent;
            Transform realRoot = loc02 != null ? loc02.real : real;
            Transform repRoot = loc02 != null ? loc02.report : report;

            // Iconic Vietnamese Hospital Glazed Reception Partition mounted cleanly along east counter edge (x = 2.02, y = 1.125)
            // The pass-through and south end frame Lan and the F101 sheet in CH01.
            var partition = PlaceBlenderModel("CTCC_ENV_ReceptionPartition", perm, new Vector3(2.02f, 1.125f, -3.24f), 0f, pairRB: true);
            staticArchitecture.Add(partition);
            ConfigureReceptionGlazing(partition);
            Sign("Reception window plaque", "Sign_ReceptionWindow", new Vector3(2.09f, 2.34f, -3.24f), Vector3.right,
                new Vector2(0.86f, 0.24f), perm, metalBacking: true);

            // Authentic November 2005 Wall Calendar mounted on solid south west-wall pier (z in [-8..-5.35] — never floats over S1 stairwell!)
            Sign("Calendar Nov 2005", "Calendar2005", new Vector3(-0.985f, 1.56f, -5.85f), Vector3.right,
                new Vector2(0.46f, 0.64f), perm, metalBacking: false);

            // Reception Storytelling Details: true R vs B operational differentiation!
            // 2005 B active shift: Lan's warm ceramic teapot & cups set out on counter; 2012 R abandoned: tea tray cleared, fallen dusty slips!
            var details = PlaceBlenderModel("CTCC_PROP_ReceptionDetails", repRoot, new Vector3(1.62f, 1.128f, -4.52f), 12f, pairRB: false, castShadows: false);
            staticArchitecture.Add(details);
            var plant = PlaceBlenderModel("CTCC_PROP_ReceptionDetails", perm, new Vector3(8.15f, 0f, -7.15f), -35f, pairRB: true);
            // Hide tea tray on corner plant copy & hide plant on counter copy if both sub-meshes live inside ReceptionDetails
            var trayOnCorner = FindDescendant(plant.transform, "LanTeaSet");
            if (trayOnCorner != null) trayOnCorner.gameObject.SetActive(false);
            var potOnDesk = FindDescendant(details.transform, "PottedPlant");
            if (potOnDesk != null) potOnDesk.gameObject.SetActive(false);
            staticArchitecture.Add(plant);

            // 2012 R abandoned counter traces (tea ring wear + two stray weathered paper slips on floor/counter)
            Decal("ReceptionAbandonedTrayWearR", "CH01_VarnishWear", new Vector3(1.62f, 1.132f, -4.52f), Vector3.up, 0.42f, 0.36f, 0.08f, realRoot, 0.72f);
            Box("AbandonedCounterSlipA_R", new Vector3(1.56f, 1.132f, -4.42f), new Vector3(0.20f, 0.004f, 0.27f), Mat("KraftPaper"), realRoot).transform.localRotation = Quaternion.Euler(0, 17f, 0);
            Box("AbandonedFloorSlipB_R", new Vector3(6.62f, 0.004f, -4.85f), new Vector3(0.21f, 0.004f, 0.28f), Mat("Paper"), realRoot).transform.localRotation = Quaternion.Euler(0, -34f, 0);

            // Spare Flashlight Battery resting visibly on lower accessible reception counter wing (CH01-Q09!)
            var battRoot = Group("Spare flashlight battery", perm, new Vector3(1.75f, 0.835f, -5.50f));
            Box("Battery cell A", new Vector3(-0.024f, 0f, 0f), new Vector3(0.036f, 0.036f, 0.068f), Mat("EnamelBlue"), battRoot);
            Box("Battery cell B", new Vector3(0.024f, 0f, 0.008f), new Vector3(0.036f, 0.036f, 0.068f), Mat("EnamelBlue"), battRoot);
            Box("Battery cap A", new Vector3(-0.024f, 0f, 0.038f), new Vector3(0.018f, 0.018f, 0.008f), Mat("Brass"), battRoot);
            Box("Battery cap B", new Vector3(0.024f, 0f, 0.046f), new Vector3(0.018f, 0.018f, 0.008f), Mat("Brass"), battRoot);
            CapsuleTarget("BATTERY", "Pin đèn pin dự phòng", Vector3.zero, new Vector3(0.32f, 0.28f, 0.32f), battRoot);

            // Closed Khu B Stairwell Barrier in east reception bay (x = 7.95, z = -1.15) — CH01-Q11 authentic world boundary!
            var barrier = PlaceBlenderModel("CTCC_ENV_StairwellBarrier", perm, new Vector3(7.55f, 0f, -0.95f), 0f, pairRB: false);
            Box("Stairwell barrier col", new Vector3(7.55f, 0.72f, -0.95f), new Vector3(2.2f, 1.44f, 0.45f),
                Mat("MetalOld"), perm, solid: true).GetComponent<Renderer>().enabled = false;
            staticArchitecture.Add(barrier);
            Sign("Khu B closed sign", "Sign_KhuBClosed", new Vector3(7.55f, 1.18f, -1.18f), Vector3.back,
                new Vector2(0.84f, 0.42f), perm, metalBacking: true);
            CapsuleTarget("STAIRWELL", "Lối lên Khu B (Đã rào chắn)", new Vector3(7.55f, 1.15f, -1.35f), new Vector3(1.8f, 1.4f, 0.45f), perm);

            var glassSil = Lamp("Reception glass silhouette backlight", LightType.Point, new Vector3(-0.28f, 1.83f, -2.38f), 1.25f, 3.3f,
                new Color(0.96f, 0.80f, 0.54f), perm);

            var lanSilPt = Group("LanSilhouettePoint", perm, new Vector3(0.34f, 0f, -2.69f));
            var lanDeskPt = Group("LanDeskPoint", perm, new Vector3(.41f, 0f, -3.23f));
            lanDeskPt.localRotation = Quaternion.Euler(0f, 92f, 0f);
            DressLanWorkstation(perm, lanDeskPt);

            // Keep the standing mark for legacy actor checks and neutral reference captures.
            var lanStepPt = Group("LanStepOutPoint", perm, new Vector3(2.55f, 0f, -3.68f));
            lanStepPt.localRotation = Quaternion.Euler(0f, 36f, 0f);

            var lanActor = PlaceCharacterActor("CTCC_CHR_Lan", perm, lanDeskPt.position, 92f, "CHAR002_Lan",
                isApparition: false, enableBlink: true, enableLipSync: true);
            AddLanReceptionClips(lanActor);
            Lamp("Lan face soft key B", LightType.Point, new Vector3(1.54f, 1.94f, -2.75f), 1.40f, 3.55f,
                new Color(.98f, .94f, .88f), repRoot);
            Lamp("Lan warm desk rim backlight B", LightType.Point, new Vector3(-.35f, 1.56f, -3.88f), .67f, 2.8f,
                new Color(1f, .86f, .69f), repRoot);
            Lamp("Lan glass cool rim fill B", LightType.Point, new Vector3(.84f, 1.85f, -4.40f), .38f, 2.75f,
                new Color(.83f, .91f, .93f), repRoot);

            var lanVoice = CreateVoiceAnchor("LanVoiceAnchor", lanActor.transform, new Vector3(0f, 1.35f, .04f), 14f);
            var phoneVoice = CreateVoiceAnchor("PhoneAnchor", perm, new Vector3(1.65f, 1.25f, -3.55f), 12f);

            // Reception Reflection Probe
            var recProbe = CreateReflectionProbe("Local reflection · LOC02", new Vector3(4.0f, 1.85f, -4.0f), new Vector3(10.0f, 3.65f, 8.0f), perm);
            recProbe.intensity = .60f;

            if (loc02 != null)
            {
                loc02.region.lanActor = lanActor;
                loc02.region.lanSilhouettePoint = lanSilPt;
                loc02.region.lanDeskPoint = lanDeskPt;
                loc02.region.lanStepOutPoint = lanStepPt;
                loc02.region.lanVoiceAnchor = lanVoice;
                loc02.region.phoneAnchor = phoneVoice;
                loc02.region.spareBatteryProp = battRoot.gameObject;
                loc02.region.receptionGlassSilhouette = glassSil.gameObject;
                loc02.probes.Add(recProbe);
            }
        }

        private void DressLoc03Ward101AndStore()
        {
            var loc03 = multiScene ? zones["LOC03"] : null;
            Transform perm = loc03 != null ? loc03.permanent : permanent;
            Transform realRoot = loc03 != null ? loc03.real : real;

            // Hero Physical Payoff inside Store Room (Kho giường 101): Dismantled 4th Bed Frame + Folded Blankets!
            // Positioned cleanly along Store north wall (x=15.55, z=5.02) — completely clear of E11 dry shelf (z=2.61) and walkway (z=3.62)!
            var dismantled = PlaceBlenderModel("CTCC_PROP_DismantledBedFrame", realRoot, new Vector3(15.55f, 0f, 5.02f), 180f, pairRB: false);
            Box("Dismantled bed col", new Vector3(15.55f, 0.45f, 5.02f), new Vector3(1.95f, 0.90f, 0.72f),
                Mat("MetalOld"), realRoot, solid: true).GetComponent<Renderer>().enabled = false;
            Decal("DismantledBedAO", "BedContact", new Vector3(15.55f, 0.01f, 5.02f), Vector3.up, 2.2f, 1.0f, 0.25f, realRoot, 0.85f);

            // Lush Tropical Shrubs outside Room 101's 3 north windows (framing monsoon rain view!)
            staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_TropicalShrub", perm, new Vector3(7.35f, -0.14f, 10.6f), 42f, pairRB: false, scale: Vector3.one * 1.15f, castShadows: false));
            staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_TropicalShrub", perm, new Vector3(10.35f, -0.14f, 11.2f), 165f, pairRB: false, scale: Vector3.one * 1.05f, castShadows: false));
            staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_TropicalShrub", perm, new Vector3(12.85f, -0.14f, 10.4f), 280f, pairRB: false, scale: Vector3.one * 0.95f, castShadows: false));

            // Rigged CHAR007 / C01 Bé An Apparition (hidden by default; gentle Fresnel fade during CH02-T02 & post-VerifyAn!)
            var anActor = PlaceCharacterActor("CTCC_CHR_AnApparition", perm, new Vector3(10.85f, 0f, 3.65f), -68f, "CHAR007_An",
                isApparition: true, enableBlink: true, enableLipSync: true);
            anActor.gameObject.SetActive(false);

            var anVoice = CreateVoiceAnchor("AnVoiceAnchor", anActor.transform, new Vector3(0f, 1.02f, 0f), 11f);
            var paVoice = CreateVoiceAnchor("PaSpeakerAnchor", perm, new Vector3(5.50f, 2.62f, 3.35f), 22f);

            if (loc03 != null)
            {
                loc03.region.anActor = anActor;
                loc03.region.anVoiceAnchor = anVoice;
                loc03.region.paSpeakerAnchor = paVoice;
            }
        }

        private void DressLoc04Emergency()
        {
            var loc04 = multiScene ? zones["LOC04"] : null;
            Transform perm = loc04 != null ? loc04.permanent : permanent;
            Transform realRoot = loc04 != null ? loc04.real : real;

            // Illuminated Crimson Emergency Light Boxes above BOTH Door A (x=4.0) and Door B (x=0.05) facing corridor!
            var redLights = new List<Light>();
            foreach (float x in new[] { 4.0f, 0.05f })
            {
                var box = PlaceBlenderModel("CTCC_PROP_EmergencyLightBox", perm, new Vector3(x, 2.94f, 15.84f), 0f, pairRB: false, castShadows: false);
                staticArchitecture.Add(box);
                Sign("EmergencyBoxFace_" + x.ToString("0"), "Sign_EmergencyBox", new Vector3(x, 2.94f, 15.72f), Vector3.back,
                    new Vector2(0.76f, 0.24f), perm, metalBacking: false);
                var rl = Lamp("Emergency crimson glow " + x.ToString("0"), LightType.Point, new Vector3(x, 2.74f, 15.35f),
                    1.10f, 6.8f, new Color(0.92f, 0.22f, 0.16f), perm);
                redLights.Add(rl);
            }

            // Atmospheric ER Interior Overhead Fluorescent + Cool Triage Spill (clear, eerie clinical contrast!)
            Lamp("ER triage overhead R", LightType.Point, new Vector3(4.2f, 3.25f, 19.3f), 2.1f, 11.0f, new Color(0.70f, 0.86f, 0.84f), perm);
            Lamp("ER stretcher exam key", LightType.Spot, new Vector3(7.25f, 3.35f, 18.85f), 5.2f, 8.5f, new Color(0.84f, 0.96f, 0.90f), perm, shadow: false)
                .transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Lamp("ER west door B fill", LightType.Point, new Vector3(0.45f, 2.85f, 18.8f), 1.35f, 7.5f, new Color(0.62f, 0.78f, 0.82f), perm);

            // High-Fidelity Blender ER Equipment Bay (East Side x in [6.2..8.9] — strictly clear of AP05 & walking paths!)
            var erStretcher = PlaceBlenderModel("CTCC_PROP_EmergencyStretcher", perm, new Vector3(7.20f, 0f, 18.95f), 14f, pairRB: true);
            Box("ER stretcher col", new Vector3(7.20f, 0.44f, 18.95f), new Vector3(0.82f, 0.88f, 2.08f),
                Mat("Steel"), perm, solid: true).GetComponent<Renderer>().enabled = false;
            staticArchitecture.Add(erStretcher);
            Decal("ERStretcherAO", "BedContact", new Vector3(7.20f, 0.01f, 18.95f), Vector3.up, 1.15f, 2.35f, 0.24f, perm, 0.85f);

            // Patient Vital Signs Monitor Cart w/ softly glowing green ECG screen!
            var monCart = PlaceBlenderModel("CTCC_PROP_PatientMonitorCart", perm, new Vector3(8.22f, 0f, 19.95f), -122f, pairRB: false);
            Box("Monitor cart col", new Vector3(8.22f, 0.65f, 19.95f), new Vector3(0.58f, 1.30f, 0.56f),
                Mat("MetalOld"), perm, solid: true).GetComponent<Renderer>().enabled = false;
            staticArchitecture.Add(monCart);
            Lamp("Monitor green screen glow", LightType.Point, new Vector3(7.85f, 1.35f, 19.65f), 0.65f, 2.6f, new Color(0.26f, 0.95f, 0.56f), perm);

            // Rolling Green Medical Oxygen Cylinder Cart + Stainless Instrument Trolley + Wall Medical Gas Manifold
            var o2Cart = PlaceBlenderModel("CTCC_PROP_OxygenCylinderCart", perm, new Vector3(8.25f, 0f, 17.55f), -35f, pairRB: false);
            Box("O2 cart col", new Vector3(8.25f, 0.68f, 17.55f), new Vector3(0.48f, 1.36f, 0.48f),
                Mat("GreenOxygen"), perm, solid: true).GetComponent<Renderer>().enabled = false;
            staticArchitecture.Add(o2Cart);

            var instTrolley = PlaceBlenderModel("CTCC_PROP_InstrumentTrolley", perm, new Vector3(1.3f, 0f, 19.45f), -22f, pairRB: true);
            Box("Inst trolley col", new Vector3(1.3f, 0.46f, 19.45f), new Vector3(0.68f, 0.92f, 0.48f),
                Mat("Steel"), perm, solid: true).GetComponent<Renderer>().enabled = false;
            staticArchitecture.Add(instTrolley);

            staticArchitecture.Add(PlaceBlenderModel("CTCC_PROP_WallGasManifold", perm, new Vector3(8.98f, 1.48f, 19.0f), -90f, pairRB: false, castShadows: false));
            staticArchitecture.Add(PlaceBlenderModel("CTCC_PROP_IVStand", perm, new Vector3(6.48f, 0f, 18.25f), 25f, pairRB: true));

            // West ER Seating & Blocked Upper ICU Wing Barrier (strictly clear of Door B path x=0.05 -> x=4.0!)
            staticArchitecture.Add(PlaceBlenderModel("CTCC_PROP_WaitingBench", perm, new Vector3(-0.62f, 0f, 19.8f), 180f, pairRB: true));
            Box("ER bench col", new Vector3(-0.62f, 0.38f, 19.8f), new Vector3(0.56f, 0.76f, 2.10f),
                Mat("Steel"), perm, solid: true).GetComponent<Renderer>().enabled = false;
            staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_StairwellBarrier", perm, new Vector3(1.55f, 0f, 21.55f), 0f, pairRB: false));
            Box("ER barrier col", new Vector3(1.55f, 0.72f, 21.55f), new Vector3(2.15f, 1.44f, 0.45f),
                Mat("MetalOld"), perm, solid: true).GetComponent<Renderer>().enabled = false;
            Sign("ER wing closed sign", "Sign_KhuBClosed", new Vector3(1.55f, 1.18f, 21.32f), Vector3.back,
                new Vector2(0.80f, 0.40f), perm, metalBacking: true);
            CapsuleTarget("CH03_DOOR", "Lối sang Khu lưu trữ & Trạm điều dưỡng (CH03)", new Vector3(1.55f, 1.15f, 21.32f),
                new Vector3(1.4f, 1.4f, 0.45f), perm);

            // HE01 Frosted Glass Rolling Stretcher Shadow Prop (hidden until CH01 B04 corridor walk!)
            var shadowTrolley = PlaceBlenderModel("CTCC_PROP_TransportTrolley", perm, new Vector3(1.2f, 0f, 17.2f), 90f, pairRB: false);
            shadowTrolley.name = "HE01_TrolleyShadow";
            shadowTrolley.SetActive(false);

            // Rigged, Animated CHAR005 Kẻ Giữ Cửa (The Gatekeeper) — STRICTLY DORMANT until CH02 B06 after G02!
            var gkActor = PlaceCharacterActor("CTCC_CHR_Gatekeeper", perm, new Vector3(4.0f, 0f, -1.2f), 0f, "CHAR005_Gatekeeper",
                isApparition: false, enableBlink: false, enableLipSync: true);
            gkActor.gameObject.SetActive(false);
            var gkVoice = CreateVoiceAnchor("GatekeeperVoiceAnchor", gkActor.transform, new Vector3(0f, 1.78f, 0f), 20f);

            // Corridor stalk spline waypoints (south corridor -> Room 101 junction -> mid corridor -> ER glass threshold)
            var wpParent = Group("GatekeeperWaypoints", perm);
            var wps = new[]
            {
                Group("GK_WP0_South", wpParent, new Vector3(4.0f, 0f, -1.2f)),
                Group("GK_WP1_101", wpParent, new Vector3(4.0f, 0f, 4.75f)),
                Group("GK_WP2_Mid", wpParent, new Vector3(4.0f, 0f, 10.4f)),
                Group("GK_WP3_ERGlass", wpParent, new Vector3(4.0f, 0f, 14.3f)),
            };

            // ER Reflection Probe
            var erProbe = CreateReflectionProbe("Local reflection · LOC04", new Vector3(4.0f, 1.85f, 19.0f), new Vector3(10.0f, 3.65f, 6.2f), perm);

            if (loc04 != null)
            {
                loc04.region.gatekeeperActor = gkActor;
                loc04.region.gatekeeperVoiceAnchor = gkVoice;
                loc04.region.gatekeeperWaypoints = wps;
                loc04.region.trolleyShadowProp = shadowTrolley;
                loc04.emergencyRedLights.AddRange(redLights);
                loc04.probes.Add(erProbe);
            }
        }

        private void DressLoc05ArchiveWing()
        {
            var loc05 = multiScene ? zones["LOC05"] : null;
            var upper = loc05 != null ? loc05.permanent : permanent;
            Transform perm = Group("Archive dressing", upper, new Vector3(0, ArchiveLevel, 0));
            Transform ward = Group("Ward dressing", upper, new Vector3(0, WardLevel, WardOffset));
            Transform realRoot = Group("Archive R lights", loc05?.real ?? real, new Vector3(0, ArchiveLevel, 0));
            Transform wardR = Group("Ward R lights", loc05?.real ?? real, new Vector3(0, WardLevel, WardOffset));
            Transform repRoot = Group("Archive B lights", loc05?.report ?? report, new Vector3(0, ArchiveLevel, 0));
            Transform wardB = Group("Ward B lights", loc05?.report ?? report, new Vector3(0, WardLevel, WardOffset));
            Transform memRoot = Group("Archive memory", loc05?.memory ?? memory, new Vector3(0, ArchiveLevel, 0));

            // LOC05 Nurse Station Counter (east side x=6.15, z=27.2 — completely clear of AP06 at x=4.0, z=26.4!)
            Box("LOC05 nurse desk", new Vector3(6.15f, 0.44f, 27.25f), new Vector3(1.05f, 0.88f, 2.25f),
                Mat("Ceramic"), ward, solid: true, isStatic: true);
            Box("Pad · brass archive marker", new Vector3(-0.6f, 0.008f, 32.2f), new Vector3(0.88f, 0.014f, 0.092f),
                Mat("Brass"), perm, isStatic: true);

            // LOC07 Central Archive Reading Stand (x=-1.65, z=32.2) & Meeting Room Radio Side Table (x=-1.85, z=35.85)
            Box("AN03 archive lectern", new Vector3(-1.65f, 0.46f, 32.2f), new Vector3(0.62f, 0.92f, 0.86f),
                Mat("Wood"), perm, solid: true, isStatic: true);
            Box("Meeting room radio table", new Vector3(-1.85f, 0.42f, 35.85f), new Vector3(0.66f, 0.84f, 0.66f),
                Mat("Wood"), perm, solid: true, isStatic: true);

            if (art)
            {
                // Blender 4.5 CH03 Props & Architectural Modules
                var rackA = PlaceBlenderModel("CTCC_ENV_ArchiveShelfRack", perm, new Vector3(-2.85f, 0f, 31.20f), -90f, pairRB: true);
                Box("Archive rack A col", new Vector3(-2.85f, 1.05f, 31.20f), new Vector3(0.54f, 2.10f, 1.84f),
                    Mat("MetalOld"), perm, solid: true).GetComponent<Renderer>().enabled = false;
                staticArchitecture.Add(rackA);

                var rackB = PlaceBlenderModel("CTCC_ENV_ArchiveShelfRack", perm, new Vector3(-2.85f, 0f, 33.40f), -90f, pairRB: true);
                Box("Archive rack B col", new Vector3(-2.85f, 1.05f, 33.40f), new Vector3(0.54f, 2.10f, 1.84f),
                    Mat("MetalOld"), perm, solid: true).GetComponent<Renderer>().enabled = false;
                staticArchitecture.Add(rackB);

                staticArchitecture.Add(PlaceBlenderModel("CTCC_PROP_PatientRegisterE03", perm, new Vector3(-1.65f, 0.925f, 32.05f), -90f, pairRB: false));
                staticArchitecture.Add(PlaceBlenderModel("CTCC_PROP_KnittingBagC02", perm, new Vector3(-2.69f, 1.10f, 31.15f), -85f, pairRB: false));
                staticArchitecture.Add(PlaceBlenderModel("CTCC_PROP_UnnamedBoxC12", perm, new Vector3(-2.69f, 1.10f, 33.45f), -92f, pairRB: false));
                staticArchitecture.Add(PlaceBlenderModel("CTCC_PROP_VintageRadio", perm, new Vector3(-1.85f, 0.845f, 35.85f), 25f, pairRB: false));

                // Atmospheric R/B/M lighting in LOC05–07
                Lamp("LOC05 nurse station spill R", LightType.Point, new Vector3(4.2f, 2.65f, 27.2f), 3.6f, 8.5f, new Color(0.78f, 0.86f, 0.84f), wardR);
                Lamp("LOC07 archive rack spill R", LightType.Point, new Vector3(-0.9f, 2.15f, 32.35f), 3.4f, 7.7f, new Color(0.79f, 0.83f, 0.72f), realRoot);
                Lamp("LOC07 bypass spill", LightType.Point, new Vector3(0.3f, 2.7f, 36.6f), 2.35f, 6f, new Color(.70f, .82f, .79f), realRoot);
                Lamp("LOC06 duty corridor spill R", LightType.Point, new Vector3(4.0f, 2.65f, 33.5f), 2.7f, 9.0f, new Color(0.71f, 0.83f, 0.81f), wardR);
                Lamp("LOC05 nurse tube B", LightType.Point, new Vector3(4.2f, 3.15f, 27.2f), 2.15f, 9.5f, new Color(0.86f, 0.96f, 0.87f), wardB);
                Lamp("LOC07 archive tube B", LightType.Point, new Vector3(-0.8f, 3.15f, 32.5f), 2.10f, 9.5f, new Color(0.86f, 0.96f, 0.87f), repRoot);
                Lamp("LOC06 corridor tube B", LightType.Point, new Vector3(4.0f, 3.15f, 33.5f), 2.05f, 9.5f, new Color(0.86f, 0.96f, 0.87f), wardB);
            }

            // Reality.M Memory Vignette Prop & Warm Golden Lamp (CH03 AN03 — active strictly during CH03 M!)
            Transform ch03Mem = Group("CH03 · AN03 Memory Stage", memRoot);
            Lamp("Bà Lành memory vignette warm key M", LightType.Point, new Vector3(-1.18f, 1.94f, 33.38f),
                4.6f, 6.5f, new Color(1.0f, 0.83f, 0.60f), ch03Mem);
            if (art)
            {
                PlaceBlenderModel("CTCC_PROP_ArchiveChair", ch03Mem, new Vector3(-1.68f, 0, 33.08f), -76, pairRB: false);
                PlaceBlenderModel("CTCC_PROP_KnittingBagC02", ch03Mem, new Vector3(-1.66f, .49f, 33.04f),
                    -30, pairRB: false, scale: Vector3.one * .87f);
            }
            var knit = Group("Memory · named needle performance", ch03Mem, new Vector3(-1.59f, .62f, 33.05f));
            var needleA = Box("Bamboo needle, near", new Vector3(0, 0, -.032f), new Vector3(.009f, .009f, .23f), Mat("Wood"), knit);
            var needleB = Box("Bamboo needle, far", new Vector3(-.025f, .014f, .02f), new Vector3(.009f, .009f, .20f), Mat("Wood"), knit);
            needleA.transform.localRotation = Quaternion.Euler(18, -36, 0);
            needleB.transform.localRotation = Quaternion.Euler(-12, 45, 0);
            var memoryMotion = knit.gameObject.AddComponent<ArchiveMemoryMotion>();
            memoryMotion.needleA = needleA.transform;
            memoryMotion.needleB = needleB.transform;

            // Phase C (TASK013-A): CH05 AN05 Evacuation Memory Stage in LOC05 Khu B + S1 West Gallery (active strictly during CH05 M!)
            Transform ch05Mem = Group("CH05 · AN05 Evacuation Memory Stage", loc05?.memory ?? memory);
            Lamp("CH05 Khu B warm fire glow M", LightType.Point, new Vector3(-0.4f, WardLevel + 2.65f, 35.4f),
                4.4f, 12.5f, new Color(1.0f, 0.68f, 0.34f), ch05Mem);
            Lamp("CH05 S1 west gallery warm glow M", LightType.Point, new Vector3(-12.2f, WardLevel + 2.65f, 21.5f),
                4.0f, 14.0f, new Color(1.0f, 0.66f, 0.32f), ch05Mem);
            Lamp("CH05 S1 descent warm glow M", LightType.Point, new Vector3(-9.5f, WardLevel + 1.85f, -4.0f),
                3.6f, 12.5f, new Color(1.0f, 0.72f, 0.38f), ch05Mem);

            // Soft non-strobe directional smoke ribbons (disabled cleanly when reduceMotion == true per CH05-Q04!)
            Transform smokeRoot = Group("CH05 · Guidance Smoke Volumes", ch05Mem);
            var smokeMat = art && materials.TryGetValue("BeamWarm", out var bw) ? bw : Mat("Glass");
            Box("CH05 smoke ribbon Khu B", new Vector3(-2.6f, WardLevel + 2.45f, 36.8f), new Vector3(3.4f, .65f, 1.25f), smokeMat, smokeRoot, solid: false);
            Box("CH05 smoke ribbon Gallery N", new Vector3(-12.2f, WardLevel + 2.35f, 23.0f), new Vector3(2.2f, .72f, 5.2f), smokeMat, smokeRoot, solid: false);
            Box("CH05 smoke ribbon Gallery S", new Vector3(-12.2f, WardLevel + 2.45f, 7.5f), new Vector3(2.2f, .62f, 4.8f), smokeMat, smokeRoot, solid: false);

            // Pair 2 non-blocking corridor obstacle visual along S1 west gallery (clear lane on west side so player never clips!)
            var evacObstacle = Group("CH05 · Pair 2 Corridor Obstacle", ch05Mem, new Vector3(-11.45f, WardLevel, 21.5f));
            Box("Obstacle linen cart", new Vector3(0f, .44f, 0f), new Vector3(.62f, .88f, 1.24f), Mat("MetalOld"), evacObstacle, solid: false);

            // Dynamic evacuation wheelchair prop repositioned per active pair (Pair 1 -> Pair 2 -> Pair 3)
            Transform evacChair = Group("CH05 · Evac Wheelchair M", ch05Mem, new Vector3(-1.35f, WardLevel, 36.65f));
            if (art)
            {
                var chairMesh = PlaceBlenderModel("CTCC_PROP_TransportTrolley", evacChair, Vector3.zero, 90f, pairRB: false,
                    scale: new Vector3(0.74f, 0.86f, 0.58f));
                foreach (var c in chairMesh.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
            }
            else
            {
                Box("Evac chair M proxy", new Vector3(0f, .44f, 0f), new Vector3(.64f, .88f, .92f), Mat("Steel"), evacChair, solid: false);
            }

            // Reused modular rigged NPCs for CH05 Evacuation Memory (colliders stripped so movement is 100% snag-free!)
            CharacterActor colleagueActor = null;
            CharacterActor vinhActor = null;
            var patientActors = new List<CharacterActor>(6);
            if (art)
            {
                colleagueActor = PlaceCharacterActor("CTCC_CHR_Lan", ch05Mem, new Vector3(-2.35f, WardLevel, 36.85f), 90f,
                    "CH05_Colleague_Nurse", isApparition: false, enableBlink: false, enableLipSync: false);
                StripActorColliders(colleagueActor);

                vinhActor = PlaceCharacterActor("CTCC_CHR_Gatekeeper", ch05Mem, new Vector3(-12.2f, WardLevel, -3.2f), 0f,
                    "CH05_Director_Vinh", isApparition: false, enableBlink: false, enableLipSync: false);
                StripActorColliders(vinhActor);
                vinhActor.gameObject.SetActive(false);

                Vector3[] patientSpots =
                {
                    new(-1.85f, WardLevel, 35.85f), new(-1.25f, WardLevel, 36.95f), // Pair 1 (Khu B)
                    new(-12.35f, WardLevel, 22.85f), new(-12.65f, WardLevel, 20.35f), // Pair 2 (S1 Gallery)
                    new(-12.35f, WardLevel, -1.85f), new(-11.85f, WardLevel, -2.95f), // Pair 3 (S1 Stair Head)
                };
                string[] patientModels =
                {
                    "CTCC_CHR_Lan", "CTCC_CHR_Gatekeeper",
                    "CTCC_CHR_Gatekeeper", "CTCC_CHR_Lan",
                    "CTCC_CHR_Lan", "CTCC_CHR_Gatekeeper"
                };
                for (int p = 0; p < 6; p++)
                {
                    var pa = PlaceCharacterActor(patientModels[p], ch05Mem, patientSpots[p], p < 2 ? -90f : 180f,
                        $"CH05_Patient_{p + 1}", isApparition: false, enableBlink: false, enableLipSync: false);
                    StripActorColliders(pa);
                    pa.gameObject.SetActive(p < 2);
                    patientActors.Add(pa);
                }
            }

            // CH05 Layer M Interactive Targets (single-press E/K accessible, zero hold-drag!)
            CapsuleTarget("CH05_M_PAIR1", "Nhóm 1 (2 bệnh nhân) · Mở lối Khu B & phối hợp đẩy xe",
                new Vector3(-2.25f, WardLevel + 1.05f, 36.85f), new Vector3(1.15f, 1.35f, 1.15f), ch05Mem);
            CapsuleTarget("CH05_M_PAIR2", "Nhóm 2 (2 bệnh nhân) · Gọi đồng nghiệp & vượt vật cản hành lang S1",
                new Vector3(-12.2f, WardLevel + 1.05f, 21.5f), new Vector3(1.45f, 1.35f, 1.35f), ch05Mem);
            CapsuleTarget("CH05_M_EXIT", "Xe bệnh nhân AN05 · Trạng thái sơ tán / Tái hiện cố định / Thoát M",
                new Vector3(-0.18f, WardLevel + 1.08f, 32.85f), new Vector3(1.18f, 1.25f, 0.98f), ch05Mem);
            ch05Mem.gameObject.SetActive(false);

            // CH03 Interactive Targets (LOC05 Nurse Station, LOC07 Archive & Meeting Loop, LOC06 Duty Corridor)
            CapsuleTarget("E03", "Sổ đăng ký bệnh nhân 12 mã (E03)", new Vector3(-1.56f, 1.05f, 31.85f), new Vector3(0.55f, 0.29f, 0.40f), perm);
            Box("Folded note · B/2", new Vector3(-1.63f, .929f, 32.49f), new Vector3(.17f, .018f, .12f), Mat("Paper"), perm);
            CapsuleTarget("C02_NOTE", "Mảnh ghi chú kẹp sợi len đỏ", new Vector3(-1.57f, 1.04f, 32.57f), new Vector3(0.40f, 0.23f, 0.23f), perm);
            CapsuleTarget("AN03", "Điểm neo phòng hồ sơ & Sổ tên (AN03 / PZ03)", new Vector3(-1.65f, 1.08f, 32.20f), new Vector3(0.55f, 0.48f, 0.65f), perm);
            CapsuleTarget("C02_BAG", "Túi vải ở kệ số 2", new Vector3(-2.62f, 1.20f, 31.15f), new Vector3(0.48f, 0.42f, 0.48f), perm);
            CapsuleTarget("C02_LETTER", "Phong thư thăm bệnh", new Vector3(-2.65f, 1.54f, 31.65f), new Vector3(0.42f, 0.35f, 0.42f), perm);
            CapsuleTarget("C12_BOX", "Hộp đồ cá nhân không tên", new Vector3(-2.62f, 1.20f, 33.45f), new Vector3(0.48f, 0.42f, 0.48f), perm);
            CapsuleTarget("FS10_LOCK", "Ổ khóa tủ hồ sơ niêm phong năm 2005", new Vector3(2.87f, 1.48f, 36.4f), new Vector3(0.40f, 0.36f, 0.40f), ward);
            CapsuleTarget("RADIO_LOC07", "Đài cassette cũ trong phòng họp (Đánh lạc hướng)", new Vector3(-1.85f, 0.98f, 35.85f), new Vector3(0.48f, 0.42f, 0.48f), perm);
            CapsuleTarget("G03", "Cửa thoát khu lưu trữ (G03)", new Vector3(4.0f, 1.25f, 37.05f), new Vector3(1.04f, 0.88f, 0.22f), ward);
            CapsuleTarget("CH03_EXIT", "Hành lang chuyển tiếp CH04 (AP07)", new Vector3(4.0f, 1.15f, 39.45f), new Vector3(0.85f, 0.75f, 0.45f), ward);

            var radioPt = Group("Ch03RadioPoint", perm, new Vector3(-1.85f, 0.85f, 35.85f));
            var wpParent = Group("Ch03PatrolWaypoints", perm);
            var wps = new[]
            {
                Group("CH03_WP0_South", wpParent, new Vector3(2.6f, 0f, 31.0f)),
                Group("CH03_WP1_Mid", wpParent, new Vector3(2.5f, 0f, 36.5f)),
                Group("CH03_WP2_North", wpParent, new Vector3(-0.35f, 0f, 37.25f)),
            };

            if (loc05 != null)
            {
                loc05.region.ch03MemoryStage = ch03Mem.gameObject;
                loc05.region.ch05MemoryStage = ch05Mem.gameObject;
                loc05.region.ch05SmokeVisuals = smokeRoot.gameObject;
                loc05.region.ch05EvacWheelchair = evacChair;
                loc05.region.ch05ColleagueActor = colleagueActor;
                loc05.region.ch05VinhActor = vinhActor;
                loc05.region.ch05PatientActors = patientActors.ToArray();
                loc05.region.namedMemory = knit.gameObject;
                loc05.region.memoryMotion = memoryMotion;
                loc05.region.ch03PatrolWaypoints = wps;
                loc05.region.ch03RadioPoint = radioPt;
                if (art)
                {
                    var arcProbe = CreateReflectionProbe("Local reflection · LOC05", new Vector3(0f, 1.65f, 34f), new Vector3(7f, 3.3f, 9f), perm);
                    loc05.probes.Add(arcProbe);
                }
            }
        }

        private static void StripActorColliders(CharacterActor actor)
        {
            if (actor == null) return;
            foreach (var col in actor.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(col);
        }

        private static AudioSource CreateVoiceAnchor(string name, Transform parent, Vector3 localPos, float maxDist)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.dopplerLevel = 0f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 1.6f;
            src.maxDistance = maxDist;
            src.spatialBlend = 0.78f;
            return src;
        }
    }
}
