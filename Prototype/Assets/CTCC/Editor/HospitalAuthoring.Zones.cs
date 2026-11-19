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
            var asphalt = Box("Exterior wet asphalt approach", new Vector3(4.0f, -0.095f, -23.0f),
                new Vector3(18.0f, 0.20f, 7.2f), Mat("WetAsphalt"), perm, solid: true, tile: 3.2f, isStatic: true);
            var courtAsphalt = Box("Courtyard wet concrete apron", new Vector3(4.0f, -0.092f, -14.0f),
                new Vector3(17.8f, 0.20f, 11.8f), Mat("WetAsphalt"), perm, solid: false, tile: 3.6f, isStatic: true);

            // Hero 3-Story 1987 Vietnamese Hospital Exterior Facade rising above the lobby entrance (z = -8.02, y = 3.60)
            var facade = PlaceBlenderModel("CTCC_ENV_HospitalFacade", perm, new Vector3(4.0f, 3.60f, -8.02f), 0f, pairRB: false);
            staticArchitecture.Add(facade);

            // Brick Security Gatehouse flanking east side of outer gate (strictly outside walking lane x in [2.8..5.2]!)
            var gatehouse = PlaceBlenderModel("CTCC_ENV_Gatehouse", perm, new Vector3(8.55f, 0f, -18.55f), 0f, pairRB: false);
            Box("Gatehouse collider", new Vector3(8.55f, 1.35f, -18.55f), new Vector3(2.85f, 2.70f, 2.55f),
                Mat("WeatheredBrick"), perm, solid: true).GetComponent<Renderer>().enabled = false;
            staticArchitecture.Add(gatehouse);
            // Warm tungsten security gatehouse porch spill illuminating the wet entrance gate
            Lamp("Gatehouse warm porch light", LightType.Point, new Vector3(7.05f, 2.38f, -18.85f), 2.4f, 8.5f,
                new Color(0.98f, 0.72f, 0.40f), perm);

            // Authentic An Sinh 1987 Enamel Gate Plaque on west gate pillar
            Sign("Hospital gate plaque", "Sign_HospitalGate", new Vector3(2.58f, 1.48f, -20.19f), Vector3.back,
                new Vector2(0.76f, 0.34f), perm, metalBacking: true);

            // Perimeter Iron Fence Bays atop low courtyard brick walls (west x=-5, east x=13, front z=-20)
            foreach (float z in new[] { -18.0f, -14.0f, -10.0f })
            {
                staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_CourtyardFenceBay", perm, new Vector3(-5.0f, 0.85f, z), 90f, pairRB: false, castShadows: false));
                staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_CourtyardFenceBay", perm, new Vector3(13.0f, 0.85f, z), -90f, pairRB: false, castShadows: false));
            }
            staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_CourtyardFenceBay", perm, new Vector3(-1.8f, 1.72f, -20.0f), 0f, pairRB: false, castShadows: false));
            staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_CourtyardFenceBay", perm, new Vector3(9.4f, 1.72f, -20.0f), 0f, pairRB: false, castShadows: false));

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

            // Exterior rain sheen decals along wet courtyard approach
            Decal("CourtyardPuddle_1", "DampFloor", new Vector3(4.0f, 0.01f, -16.5f), Vector3.up, 3.6f, 4.2f, 0.25f, realRoot, 0.86f);
            Decal("CourtyardPuddle_2", "DampFloor", new Vector3(4.0f, 0.01f, -11.8f), Vector3.up, 3.2f, 3.4f, 0.25f, realRoot, 0.82f);
            Decal("ApproachPuddle", "DampFloor", new Vector3(3.6f, 0.01f, -21.2f), Vector3.up, 3.4f, 2.8f, 0.25f, realRoot, 0.85f);

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
            // Leaves open pass-through window + clear south end so Lan steps out smoothly in CH01 B03!
            var partition = PlaceBlenderModel("CTCC_ENV_ReceptionPartition", perm, new Vector3(2.02f, 1.125f, -3.24f), 0f, pairRB: true);
            staticArchitecture.Add(partition);
            Sign("Reception window plaque", "Sign_ReceptionWindow", new Vector3(2.09f, 2.34f, -3.24f), Vector3.right,
                new Vector2(0.86f, 0.24f), perm, metalBacking: true);

            // Authentic November 2005 Wall Calendar behind reception counter (shared/report detail!)
            Sign("Calendar Nov 2005", "Calendar2005", new Vector3(-0.985f, 1.56f, -4.65f), Vector3.right,
                new Vector2(0.46f, 0.64f), perm, metalBacking: false);

            // Reception Storytelling Details: ceramic teapot & cups + potted lobby snake-plant
            var details = PlaceBlenderModel("CTCC_PROP_ReceptionDetails", perm, new Vector3(1.62f, 1.128f, -4.52f), 12f, pairRB: true, castShadows: false);
            staticArchitecture.Add(details);
            var plant = PlaceBlenderModel("CTCC_PROP_ReceptionDetails", perm, new Vector3(8.15f, 0f, -7.15f), -35f, pairRB: true);
            // Hide tea tray on corner plant copy & hide plant on counter copy if both sub-meshes live inside ReceptionDetails
            var trayOnCorner = FindDescendant(plant.transform, "LanTeaSet");
            if (trayOnCorner != null) trayOnCorner.gameObject.SetActive(false);
            var potOnDesk = FindDescendant(details.transform, "PottedPlant");
            if (potOnDesk != null) potOnDesk.gameObject.SetActive(false);
            staticArchitecture.Add(plant);

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

            // Warm backlit glass silhouette accent behind Reception counter (draws Minh inside in CH01 B01-B02!)
            var glassSil = Lamp("Reception glass silhouette backlight", LightType.Point, new Vector3(0.35f, 1.95f, -3.24f), 2.3f, 5.2f,
                new Color(0.96f, 0.80f, 0.54f), perm);

            // Rigged, BlendShaped, Animated CHAR002 Trần Ngọc Lan!
            var lanSilPt = Group("LanSilhouettePoint", perm, new Vector3(0.65f, 0f, -3.24f));
            lanSilPt.localRotation = Quaternion.Euler(0f, 148f, 0f);
            var lanDeskPt = Group("LanDeskPoint", perm, new Vector3(0.32f, 0f, -3.24f));
            lanDeskPt.localRotation = Quaternion.Euler(0f, 92f, 0f);
            // Step-out position clear of desk (x=2.08) and AP02 (x=3.78) so Lan stands face-to-face 1.15m from Minh!
            var lanStepPt = Group("LanStepOutPoint", perm, new Vector3(2.65f, 0f, -3.28f));
            lanStepPt.localRotation = Quaternion.Euler(0f, 90f, 0f);

            var lanActor = PlaceCharacterActor("CTCC_CHR_Lan", perm, lanDeskPt.position, 92f, "CHAR002_Lan",
                isApparition: false, enableBlink: true, enableLipSync: true);
            // Warm portrait key light attached subtly above Lan's encounter spot so her eyes & expression shine cleanly in B!
            Lamp("Lan face soft key B", LightType.Point, new Vector3(3.10f, 2.05f, -3.45f), 1.45f, 4.2f,
                new Color(0.96f, 0.94f, 0.86f), repRoot);

            var lanVoice = CreateVoiceAnchor("LanVoiceAnchor", lanActor.transform, new Vector3(0f, 1.48f, 0f), 14f);
            var phoneVoice = CreateVoiceAnchor("PhoneAnchor", perm, new Vector3(1.65f, 1.25f, -3.55f), 12f);

            // Reception Reflection Probe
            var recProbe = CreateReflectionProbe("Local reflection · LOC02", new Vector3(4.0f, 1.85f, -4.0f), new Vector3(10.0f, 3.65f, 8.0f), perm);

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
                isApparition: true, enableBlink: false, enableLipSync: false);
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

            var instTrolley = PlaceBlenderModel("CTCC_PROP_InstrumentTrolley", perm, new Vector3(6.05f, 0f, 20.65f), -22f, pairRB: true);
            Box("Inst trolley col", new Vector3(6.05f, 0.46f, 20.65f), new Vector3(0.68f, 0.92f, 0.48f),
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

            // HE01 Frosted Glass Rolling Stretcher Shadow Prop (hidden until CH01 B04 corridor walk!)
            var shadowTrolley = PlaceBlenderModel("CTCC_PROP_TransportTrolley", perm, new Vector3(1.2f, 0f, 17.2f), 90f, pairRB: false);
            shadowTrolley.name = "HE01_TrolleyShadow";
            shadowTrolley.SetActive(false);

            // Rigged, Animated CHAR005 Kẻ Giữ Cửa (The Gatekeeper) — STRICTLY DORMANT until CH02 B06 after G02!
            var gkActor = PlaceCharacterActor("CTCC_CHR_Gatekeeper", perm, new Vector3(4.0f, 0f, -1.2f), 0f, "CHAR005_Gatekeeper",
                isApparition: false, enableBlink: false, enableLipSync: false);
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
