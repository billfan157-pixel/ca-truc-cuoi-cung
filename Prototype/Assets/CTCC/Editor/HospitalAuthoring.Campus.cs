using System.Collections.Generic;
using UnityEngine;

namespace CTCC.Editor
{
    public sealed partial class HospitalAuthoring
    {
        /// <summary>
        /// Full An Sinh Hospital Campus Expansion (Core 03 Campus-v0 / DEC-WORLD-01 / 1987-2005-2012).
        /// Builds all 6 functional blocks (K-A Main 3F+B1, K-B Outpatient 2F, K-C Inpatient 3F,
        /// K-D Emergency 1F + Ambulance Canopy, K-E Admin 2F, K-F Logistics 1F + 17.5m Water Tower),
        /// 4 segregated circulation streams & gates, covered walkways, Inner Light Court (Sân sáng),
        /// Rear Sunken Court (-1.90m), utility streetlamps, and rich R (2012) vs B (2005) duality.
        ///
        /// Strictly preserves all locked core gameplay coordinates (LOC01-LOC12, Room 101, AP00-AP09, AN03,
        /// gates G01-G08, S1/S2/S3 stairs, and CH01-CH08 progression).
        /// </summary>
        private void BuildFullCampus()
        {
            Transform extPerm = multiScene ? zones["CAMPUS_EXT"].permanent : Group("Campus Grounds", permanent);
            Transform extReal = multiScene ? zones["CAMPUS_EXT"].real : Group("Campus Grounds R", real);
            Transform extRep = multiScene ? zones["CAMPUS_EXT"].report : Group("Campus Grounds B", report);

            Transform wingPerm = multiScene ? zones["CAMPUS_WINGS"].permanent : Group("Campus Interiors", permanent);
            Transform wingReal = multiScene ? zones["CAMPUS_WINGS"].real : Group("Campus Interiors R", real);
            Transform wingRep = multiScene ? zones["CAMPUS_WINGS"].report : Group("Campus Interiors B", report);

            BuildSiteGroundAndRoads(extPerm, extReal, extRep);
            BuildPerimeterWallAndGates(extPerm, extReal, extRep);

            // 6 Architectural Blocks (using modular Blender 4.5 assets w/ LOD0-2 & automatic R/B material pairing!)
            BuildBlockKA_MainHospital(extPerm, extReal, extRep, wingPerm, wingRep);
            BuildBlockKB_OutpatientWing(extPerm, extReal, extRep, wingPerm, wingRep);
            BuildBlockKD_EmergencyWing(extPerm, extReal, extRep, wingPerm, wingRep);
            BuildBlockKC_InpatientWing(extPerm, extReal, extRep, wingPerm, wingRep);
            BuildBlockKE_AdminWing(extPerm, extReal, extRep);
            BuildBlockKF_LogisticsAndWaterTower(extPerm, extReal, extRep);

            // Connecting Walkways, Rear Sunken Court, Landscape, Streetlamps & Distant Night Horizon
            BuildCoveredLinksAndRearCourt(extPerm, extReal, extRep, wingPerm);
            BuildCampusLandscapeAndLighting(extPerm, extReal, extRep);
        }

        // -----------------------------------------------------------------------------------------
        // 1. SITE GROUND, 4 CIRCULATION ROADS, SIDEWALKS & OUTER HOSPITAL AVENUE
        // -----------------------------------------------------------------------------------------
        private void BuildSiteGroundAndRoads(Transform perm, Transform realRoot, Transform repRoot)
        {
            var g = Group("Site Ground & Roads", perm);

            // Full 100x110m campus turf/earth substrate split cleanly around the two sky-open sunken courts:
            //   1) Rear Sunken Court (-1.90m) & B1 E05b trench: x in [34.4..52.2], z in [34.8..45.2]
            //   2) West Morgue Sunken Service Ramp (-3.40m): x in [-20.0..-1.4], z in [37.5..43.2]
            // Sits below y=-0.16 so LOC01 wet apron y=0 never z-fights!
            Box("Campus terrain South", new Vector3(8f, -0.26f, -7.1f), new Vector3(132f, 0.20f, 83.8f),
                Mat("CampusLawn"), g, solid: false, isStatic: true);
            Box("Campus terrain North", new Vector3(8f, -0.26f, 72.1f), new Vector3(132f, 0.20f, 53.8f),
                Mat("CampusLawn"), g, solid: false, isStatic: true);
            Box("Campus terrain Mid-West", new Vector3(-39.0f, -0.26f, 40.0f), new Vector3(38.0f, 0.20f, 10.6f),
                Mat("CampusLawn"), g, solid: false, isStatic: true);
            Box("Campus terrain Mid-Center", new Vector3(16.5f, -0.26f, 40.0f), new Vector3(35.8f, 0.20f, 10.6f),
                Mat("CampusLawn"), g, solid: false, isStatic: true);
            Box("Campus terrain Mid-East", new Vector3(63.1f, -0.26f, 40.0f), new Vector3(21.8f, 0.20f, 10.6f),
                Mat("CampusLawn"), g, solid: false, isStatic: true);
            Box("Campus terrain Morgue-South lip", new Vector3(-10.7f, -0.26f, 36.15f), new Vector3(18.6f, 0.20f, 2.7f),
                Mat("CampusLawn"), g, solid: false, isStatic: true);
            Box("Campus terrain Morgue-North lip", new Vector3(-10.7f, -0.26f, 44.2f), new Vector3(18.6f, 0.20f, 2.0f),
                Mat("CampusLawn"), g, solid: false, isStatic: true);

            // Outer South Hospital Avenue (z in [-39..-30.2]) + concrete roadside sidewalks
            Box("South public avenue asphalt", new Vector3(8f, -0.135f, -34.8f), new Vector3(130f, 0.20f, 9.2f),
                Mat("WetAsphalt"), g, solid: false, tile: 4.5f, isStatic: true);
            Box("South road sidewalk curb", new Vector3(8f, -0.055f, -30.15f), new Vector3(114f, 0.14f, 0.90f),
                Mat("ConcretePavingR"), g, solid: false, tile: 2.0f, isStatic: true);

            // Recessed Night Gate approach apron connecting South Avenue (-30) smoothly to LOC01 approach (-26.6)
            Box("Night gate outer approach apron", new Vector3(4f, -0.115f, -28.0f), new Vector3(18.2f, 0.20f, 4.8f),
                Mat("WetAsphalt"), g, solid: false, tile: 3.4f, isStatic: true);

            // STREAM 1 — PUBLIC OUTPATIENT ROAD & FORECOURT (Main Gate -30,-30 -> K-B -> K-A West Courtyard)
            Box("Public main gate driveway", new Vector3(-30f, -0.11f, -19.0f), new Vector3(9.0f, 0.20f, 22.5f),
                Mat("WetAsphalt"), g, solid: false, tile: 3.6f, isStatic: true);
            Surface("Public outpatient forecourt plaza", new Vector3(-16.5f, -0.105f, -18.5f), new Vector3(21.5f, 0.20f, 21.0f),
                "ConcretePavingR", "ConcretePavingB", solid: false, tile: 2.8f, isStatic: true, parent: g);
            Surface("West courtyard walkway K-B to K-A", new Vector3(-18.5f, -0.105f, 16.0f), new Vector3(7.2f, 0.20f, 48.0f),
                "ConcretePavingR", "ConcretePavingB", solid: false, tile: 2.4f, isStatic: true, parent: g);
            Surface("West garden path to K-E", new Vector3(-30.0f, -0.105f, 34.5f), new Vector3(6.0f, 0.20f, 15.0f),
                "ConcretePavingR", "ConcretePavingB", solid: false, tile: 2.4f, isStatic: true, parent: g);

            // STREAM 2 — DEDICATED AMBULANCE DRIVEWAY (East Gate 53,-30 -> K-D Ambulance Canopy 43..49, 16..29)
            Box("Ambulance driveway main", new Vector3(52.5f, -0.11f, 3.0f), new Vector3(9.2f, 0.20f, 66.5f),
                Mat("WetAsphalt"), g, solid: false, tile: 3.8f, isStatic: true);
            Box("Ambulance turning apron", new Vector3(46.0f, -0.108f, 22.5f), new Vector3(13.5f, 0.20f, 17.5f),
                Mat("WetAsphalt"), g, solid: false, tile: 3.4f, isStatic: true);
            // Painted red-white curb markers along ambulance driveway west edge
            for (float z = -26f; z <= 10f; z += 4.5f)
            {
                Box("Ambulance curb white", new Vector3(47.8f, -0.02f, z), new Vector3(0.24f, 0.14f, 2.2f), Mat("AmbulanceWhite"), g, isStatic: true);
                Box("Ambulance curb red", new Vector3(47.8f, -0.02f, z + 2.25f), new Vector3(0.24f, 0.14f, 2.2f), Mat("RedPaint"), g, isStatic: true);
            }
            // Front east garden apron between LOC01 courtyard (x=13) and Ambulance Road (x=47.8)
            Surface("East front terrace walk", new Vector3(28.5f, -0.105f, 4.0f), new Vector3(21.0f, 0.20f, 18.0f),
                "ConcretePavingR", "ConcretePavingB", solid: false, tile: 2.8f, isStatic: true, parent: g);

            // STREAM 4 — NORTH SERVICE ROAD, K-F LOADING YARD & MORGUE DISCREET BRANCH (Service Gate 14, 80)
            Box("North service road", new Vector3(14.0f, -0.11f, 67.5f), new Vector3(8.5f, 0.20f, 25.5f),
                Mat("WetAsphalt"), g, solid: false, tile: 3.8f, isStatic: true);
            Box("Service cross lane to morgue", new Vector3(-3.0f, -0.11f, 56.0f), new Vector3(36.0f, 0.20f, 5.2f),
                Mat("WetAsphalt"), g, solid: false, tile: 3.6f, isStatic: true);
            Box("Morgue discreet west branch", new Vector3(-20.0f, -0.11f, 47.5f), new Vector3(5.2f, 0.20f, 18.0f),
                Mat("WetAsphalt"), g, solid: false, tile: 3.6f, isStatic: true);
            Box("Service east loop to rear court", new Vector3(33.5f, -0.11f, 75.5f), new Vector3(42.0f, 0.20f, 5.4f),
                Mat("WetAsphalt"), g, solid: false, tile: 3.8f, isStatic: true);
            Box("East rear court connector road", new Vector3(52.5f, -0.11f, 58.0f), new Vector3(6.8f, 0.20f, 38.0f),
                Mat("WetAsphalt"), g, solid: false, tile: 3.8f, isStatic: true);
        }

        // -----------------------------------------------------------------------------------------
        // 2. CONTINUOUS 100x110m PERIMETER WALL & 4 DIFFERENTIATED GATES
        // -----------------------------------------------------------------------------------------
        private void BuildPerimeterWallAndGates(Transform perm, Transform realRoot, Transform repRoot)
        {
            var g = Group("Perimeter Walls & 4 Gates", perm);

            void FenceRun(Vector3 start, Vector3 end, float yawDeg)
            {
                float dist = Vector3.Distance(start, end);
                int bays = Mathf.Max(1, Mathf.RoundToInt(dist / 4.0f));
                Vector3 dir = (end - start) / bays;
                for (int i = 0; i < bays; i++)
                {
                    Vector3 center = start + dir * (i + 0.5f);
                    if (art)
                    {
                        var bay = PlaceBlenderModel("CTCC_ENV_CourtyardFenceBay", g, center, yawDeg, pairRB: false, castShadows: false);
                        staticArchitecture.Add(bay);
                    }
                    else
                    {
                        Box("Fence proxy", center + Vector3.up * 1.1f, new Vector3(Mathf.Abs(dir.x) > 0.5f ? 4f : 0.24f, 2.2f, Mathf.Abs(dir.z) > 0.5f ? 4f : 0.24f),
                            Mat("WeatheredBrick"), g, isStatic: true);
                    }
                }
            }

            // South perimeter along z = -30 (skipping Main Gate [-35..-25], Night Gate alcove [-5..13], Ambulance Gate [48..58])
            FenceRun(new Vector3(-42f, 0f, -30f), new Vector3(-35f, 0f, -30f), 0f);
            FenceRun(new Vector3(-25f, 0f, -30f), new Vector3(-5f, 0f, -30f), 0f);
            // Recessed Night Gate approach sidewalls along x=-5 and x=13 from z=-30 to z=-20 (frames AP00 without blocking!)
            FenceRun(new Vector3(-5f, 0f, -30f), new Vector3(-5f, 0f, -20f), 90f);
            FenceRun(new Vector3(13f, 0f, -30f), new Vector3(13f, 0f, -20f), -90f);
            FenceRun(new Vector3(13f, 0f, -30f), new Vector3(48f, 0f, -30f), 0f);

            // West perimeter along x = -42 (z = -30..80)
            FenceRun(new Vector3(-42f, 0f, -30f), new Vector3(-42f, 0f, 80f), 90f);
            // East perimeter along x = 58 (z = -30..80)
            FenceRun(new Vector3(58f, 0f, -30f), new Vector3(58f, 0f, 80f), -90f);
            // North perimeter along z = 80 (skipping North Service Gate [10..18])
            FenceRun(new Vector3(-42f, 0f, 80f), new Vector3(10f, 0f, 80f), 180f);
            FenceRun(new Vector3(18f, 0f, 80f), new Vector3(58f, 0f, 80f), 180f);

            // DEC-V331-01 seals the existing public and service openings in R.
            BuildDemolitionGateSeal(realRoot, "K-B", new Vector3(-30f, 0f, -30f), 10f);
            BuildDemolitionGateSeal(realRoot, "K-D", new Vector3(53f, 0f, -30f), 10f);
            BuildDemolitionGateSeal(realRoot, "K-F", new Vector3(14f, 0f, 79.8f), 8f);

            if (!art) return;

            // GATE 1: MAIN PUBLIC ENTRANCE ARCHWAY (Cổng chính at -30, -30) + Main Security Guardhouse
            var mainArch = PlaceBlenderModel("CTCC_ENV_MainHospitalArchGate", g, new Vector3(-30f, 0f, -30f), 0f, pairRB: true);
            staticArchitecture.Add(mainArch);
            Sign("Main hospital arch sign S", "Sign_MainArch", new Vector3(-30f, 4.66f, -30.32f), Vector3.back, new Vector2(5.48f, 0.88f), g, metalBacking: false);
            Sign("Main hospital arch sign N", "Sign_MainArch", new Vector3(-30f, 4.66f, -29.68f), Vector3.forward, new Vector2(5.48f, 0.88f), g, metalBacking: false);
            var mainGuard = PlaceBlenderModel("CTCC_ENV_Gatehouse", g, new Vector3(-23.2f, 0f, -27.5f), 0f, pairRB: true);
            staticArchitecture.Add(mainGuard);
            Lamp("Main gate archway warm glow", LightType.Point, new Vector3(-30f, 4.2f, -31.8f), 6.5f, 19f, new Color(0.98f, 0.74f, 0.42f), perm);

            // GATE 3: EAST AMBULANCE GATE (Cổng cấp cứu at 53, -30)
            foreach (float gx in new[] { 48.2f, 57.6f })
            {
                Box("Amb gate pillar", new Vector3(gx, 1.65f, -30f), new Vector3(0.62f, 3.30f, 0.62f), Mat("CampusStuccoR"), g, isStatic: true);
                Box("Amb gate cap", new Vector3(gx, 3.36f, -30f), new Vector3(0.76f, 0.16f, 0.76f), Mat("BaseTrim"), g, isStatic: true);
            }
            Box("Amb gate header", new Vector3(52.9f, 3.85f, -30f), new Vector3(10.0f, 0.72f, 0.42f), Mat("RedPaint"), g, isStatic: true);
            Sign("Ambulance gate sign", "Sign_AmbulanceGate", new Vector3(52.9f, 3.85f, -30.24f), Vector3.back, new Vector2(5.2f, 0.92f), g, metalBacking: false);
            Lamp("Ambulance gate crimson beacon", LightType.Point, new Vector3(52.9f, 3.55f, -31.6f), 4.8f, 16f, new Color(0.95f, 0.28f, 0.22f), perm);

            // GATE 4: NORTH SERVICE GATE (Cổng hậu cần at 14, 80)
            foreach (float gx in new[] { 10.2f, 17.8f })
                Box("Service gate pillar", new Vector3(gx, 1.55f, 80f), new Vector3(0.54f, 3.10f, 0.54f), Mat("BaseTrim"), g, isStatic: true);
            Box("Service gate closed leaf", new Vector3(14f, 1.20f, 80f), new Vector3(7.1f, 2.35f, 0.10f), Mat("MetalOld"), g, isStatic: true);
            Sign("Service gate sign", "Sign_KF_Service", new Vector3(14f, 2.65f, 79.88f), Vector3.back, new Vector2(3.6f, 0.85f), g, metalBacking: true);
        }

        private void BuildDemolitionGateSeal(Transform realRoot, string gate, Vector3 center, float width)
        {
            var root = Group(gate + " demolition seal", realRoot, center);
            Box(gate + " solid corrugated fence", new Vector3(0f, 1.04f, 0f), new Vector3(width + .08f, 2.2f, .14f),
                Mat("CorrugatedRoofR"), root, solid: true, tile: 1.2f);
            var metal = Mat("MetalOld");
            float inside = center.z < 0f ? 1f : -1f;
            foreach (float x in new[] { -width * .25f, -.095f, .095f, width * .25f })
                Box("Fence frame", new Vector3(x, 1.04f, inside * .10f), new Vector3(.08f, 2.2f, .09f), metal, root);

            // Interlocked links at the leaf seam, on the inward side of each exit.
            for (int index = 0; index < 6; index++)
            {
                float x = (index - 2.5f) * .064f;
                var link = Group("Chain link", root, new Vector3(x, 1.24f + .15f * Mathf.Abs(x), inside * .18f));
                link.localRotation = Quaternion.Euler(index % 2 == 0 ? 0f : 75f, 0f, x < 0f ? -9f : 9f);
                foreach (float y in new[] { -.023f, .023f })
                    Box("Link rail", new Vector3(0f, y, 0f), new Vector3(.084f, .012f, .012f), metal, link);
                foreach (float end in new[] { -.037f, .037f })
                    Box("Link end", new Vector3(end, 0f, 0f), new Vector3(.012f, .046f, .012f), metal, link);
            }
            Box("Seal padlock", new Vector3(0f, 1.20f, inside * .19f), new Vector3(.09f, .085f, .042f), Mat("Brass"), root);
        }

        // -----------------------------------------------------------------------------------------
        // 3. BLOCK K-A — MAIN 3-STORY 1987 HOSPITAL BLOCK (+B1) WRAPPING LOCKED LOC02..LOC08
        // -----------------------------------------------------------------------------------------
        private void BuildBlockKA_MainHospital(Transform perm, Transform realRoot, Transform repRoot, Transform wingPerm, Transform wingRep)
        {
            var g = Group("Block K-A · Main 3F Hospital (1987)", perm);
            var interior = Group("K-A · Background Interior Depth", wingPerm);

            void Bay1987(Vector3 pos, float yaw, bool breeze = false)
            {
                if (!art)
                {
                    Box("K-A mass", pos + Vector3.up * 2f, new Vector3(4f, 4f, 0.3f), Mat("ExteriorFacade"), g, isStatic: true);
                    return;
                }
                string model = breeze ? "CTCC_ENV_StairwellBreezeBay_1987" : "CTCC_ENV_MainFacadeBay_1987";
                var obj = PlaceBlenderModel(model, g, pos, yaw, pairRB: true, castShadows: pos.y < 8.5f && pos.z < 16f);
                staticArchitecture.Add(obj);
            }

            // --- A. SOUTH FRONT ELEVATION (z = -8.0, full 33m span x = -15..+18, 3 stories Y=0..12m!) ---
            // Ground floor flanks OUTSIDE locked LOC02 lobby (x in [-1.1..9.1] stays 100% untouched!)
            foreach (float x in new[] { -13.0f, -9.0f, -5.0f })
                Bay1987(new Vector3(x, 0f, -8.0f), 0f);
            BuildS1CourtyardDischarge(g, realRoot);
            // East ground flank (x = 9.1..18.0)
            foreach (float x in new[] { 11.1f, 15.1f })
                Bay1987(new Vector3(x, 0f, -8.0f), 0f);
            Box("K-A South ground corner E", new Vector3(17.55f, 1.80f, -7.86f), new Vector3(0.95f, 3.60f, 0.28f), Mat("ExteriorFacade"), g, isStatic: true);

            // Floors 2 (Y=4.0) & 3 (Y=8.0): full monumental 3-story South elevation across ENTIRE width x=-15..+18!
            // Center bay at x=4.0 directly above reception canopy uses iconic perforated stairwell breeze blocks!
            foreach (float y in new[] { 4.0f, 8.0f })
            {
                foreach (float x in new[] { -12.0f, -8.0f, -4.0f, 0.0f, 4.0f, 8.0f, 12.0f, 16.0f })
                    Bay1987(new Vector3(x, y, -8.0f), 0f, breeze: Mathf.Abs(x - 4.0f) < 0.1f);
                // West end corner closure x=-15..-14
                Surface($"K-A South corner W y={y}", new Vector3(-14.5f, y + 2.0f, -7.86f), new Vector3(1.1f, 4.0f, 0.30f), "ExteriorFacade", "ExteriorFacadeB", tile: 2.2f, isStatic: true, parent: g);
            }
            // Ground-floor brick plinth along exposed K-A front flanks
            var discharge = layout.s1Discharge;
            float westJamb = discharge.x - discharge.width / 2f;
            float eastJamb = discharge.x + discharge.width / 2f;
            Box("K-A plinth W", new Vector3((-14.9f + westJamb) / 2f, 0.42f, -8.05f),
                new Vector3(westJamb + 14.9f, 0.84f, 0.36f), Mat("WeatheredBrick"), g, tile: 1.4f, isStatic: true);
            Box("K-A plinth S1 east jamb", new Vector3((eastJamb - 1.1f) / 2f, 0.42f, -8.05f),
                new Vector3(-1.1f - eastJamb, 0.84f, 0.36f), Mat("WeatheredBrick"), g, tile: 1.4f, isStatic: true);
            Box("K-A plinth E", new Vector3(13.6f, 0.42f, -8.05f), new Vector3(8.8f, 0.84f, 0.36f), Mat("WeatheredBrick"), g, tile: 1.4f, isStatic: true);

            // --- B. WEST ELEVATION (x = -15.0, z = -8..40, 3 stories enclosing S1 West Gallery!) ---
            // Ground floor (z=-6..10, step back to x=-8 north of z=10 per Campus-v0) + Floors 2 & 3!
            foreach (float z in new[] { -6.0f, -2.0f, 2.0f, 6.0f, 10.0f, 14.0f, 18.0f, 22.0f, 26.0f, 30.0f, 34.0f, 38.0f })
            {
                if (z <= 10.5f)
                    Bay1987(new Vector3(-15.0f, 0f, z), 90f, breeze: Mathf.Abs(z + 2.0f) < 0.1f);
                // Floor 2 encloses S1 West Gallery (x=[-13.7..-11]) with open-air breeze blocks & windows at x=-15!
                Bay1987(new Vector3(-15.0f, 4.0f, z), 90f, breeze: z < 32f && ((int)Mathf.Round(z) % 8 == 2));
                // Floor 3 steps out to x=-17.0 at z>=30 to clear S1 Archive Landing (x=-16.2) cleanly!
                float x3 = z >= 29.5f ? -17.0f : -15.0f;
                Bay1987(new Vector3(x3, 8.0f, z), 90f, breeze: z >= 32f);
            }
            // Ground floor step-in wall along x=-8.0 from z=12..40
            foreach (float z in new[] { 14.0f, 18.0f, 22.0f, 26.0f, 30.0f, 34.0f, 38.0f })
                Bay1987(new Vector3(-8.0f, 0f, z), 90f);
            // West wing underside soffit slab at y=3.90 (x=[-15..-8], z=[12..40]) supporting overhanging 2F S1 gallery
            Surface("K-A West gallery soffit", new Vector3(-11.5f, 3.85f, 26.0f), new Vector3(7.2f, 0.28f, 28.4f), "BaseTrim", "PlasterB", tile: 2.2f, isStatic: true, parent: g);
            // Square concrete columns supporting overhanging West 2F gallery
            for (float z = 14f; z <= 38f; z += 6f)
                Box($"K-A West colonnade col z={z}", new Vector3(-14.6f, 1.92f, z), new Vector3(0.42f, 3.84f, 0.42f), Mat("ExteriorFacade"), g, isStatic: true);

            // --- C. EAST ELEVATION (x = 18.0, z = -8..40, facing K-D & East Lawn) ---
            foreach (float y in new[] { 0f, 4.0f, 8.0f })
            {
                foreach (float z in new[] { -6.0f, -2.0f, 2.0f, 6.0f, 10.0f, 14.0f, 18.0f, 22.0f, 26.0f, 30.0f, 34.0f, 38.0f })
                {
                    // On Ground floor, skip z in [14..32] where K-D Emergency connects directly onto K-A East!
                    if (y < 0.5f && z >= 13.5f && z <= 32.5f) continue;
                    // On Floor 3 north of z=12, keep main block stepped at x=10.0 (enclosing LOC08 Director x=8.5!)
                    float xe = (y > 7.5f && z > 12f) ? 10.5f : 18.0f;
                    Bay1987(new Vector3(xe, y, z), -90f);
                }
            }

            // --- D. NORTH CORE ELEVATION (x = -6..10, z = 40..50, 2 stories enclosing LOC06 Duty Corridor!) ---
            foreach (float y in new[] { 0f, 4.0f })
            {
                foreach (float x in new[] { -4.0f, 0.0f, 4.0f, 8.0f })
                    Bay1987(new Vector3(x, y, 50.0f), 180f);
                foreach (float z in new[] { 42.0f, 46.0f })
                {
                    Bay1987(new Vector3(-6.0f, y, z), 90f);
                    Bay1987(new Vector3(10.0f, y, z), -90f);
                }
            }
            // Floor 3 North wall closing behind LOC07/08 at z=40
            foreach (float x in new[] { -14.0f, -10.0f, -6.0f, -2.0f, 2.0f, 6.0f, 8.5f })
                Bay1987(new Vector3(x, 8.0f, 40.0f), 180f);

            // --- E. INNER LIGHT COURT (SÂN SÁNG: x=[5.6..14.2], z=[8.1..14.1]) — HERO VIEW OUT ROOM 101 WINDOWS! ---
            // Opposite North wall of light court at z=14.1 (Ground, Floor 2 & Floor 3 facing South toward Room 101!)
            foreach (float y in new[] { 0f, 4.0f, 8.0f })
            {
                Bay1987(new Vector3(7.9f, y, 14.1f), 0f);
                Bay1987(new Vector3(11.9f, y, 14.1f), 0f);
            }
            // East wall of light court at x=14.2 (Ground, Floor 2 & Floor 3 facing West into light court!)
            foreach (float y in new[] { 0f, 4.0f, 8.0f })
                Bay1987(new Vector3(14.2f, y, 11.1f), 90f);
            // West wall of light court above HALL (Floor 2 & Floor 3 at x=5.6 facing East!)
            foreach (float y in new[] { 4.0f, 8.0f })
                Bay1987(new Vector3(5.6f, y, 11.1f), -90f);
            // South wall of light court directly ABOVE Room 101's 3 windows (Floor 2 & Floor 3 at z=8.1 facing North!)
            foreach (float y in new[] { 4.0f, 8.0f })
            {
                Bay1987(new Vector3(7.9f, y, 8.1f), 180f);
                Bay1987(new Vector3(11.9f, y, 8.1f), 180f);
            }
            // Dark structural corner seals around the 4 light-court corners (west piers start above ground-floor ceiling y=3.70 so ZERO bleed into HALL east wall!)
            foreach (var c in new[] { (5.6f, 8.1f, true), (14.2f, 8.1f, false), (5.6f, 14.1f, true), (14.2f, 14.1f, false) })
                Box("LightCourt corner pier", new Vector3(c.Item1, c.Item3 ? 7.85f : 6.0f, c.Item2), new Vector3(0.45f, c.Item3 ? 8.30f : 12.0f, 0.45f), Mat("BaseTrim"), g, isStatic: true);

            // --- F. K-A ROOF SLAB (+11.70m), PARAPET, PITCHED CORRUGATED OVER-ROOF & ROOFTOP WATER TANKS ---
            // Split flat roof slabs around the open Inner Light Court (x=[5.6..14.2], z=[8.1..14.1]) so sky & rain enter!
            Box("K-A Roof slab South", new Vector3(1.5f, 11.82f, 0.0f), new Vector3(33.4f, 0.28f, 16.4f), Mat("BaseTrim"), g, isStatic: true);
            Box("K-A Roof slab West-Mid", new Vector3(-4.7f, 11.82f, 11.1f), new Vector3(20.6f, 0.28f, 6.4f), Mat("BaseTrim"), g, isStatic: true);
            Box("K-A Roof slab East-Mid", new Vector3(16.1f, 11.82f, 11.1f), new Vector3(4.0f, 0.28f, 6.4f), Mat("BaseTrim"), g, isStatic: true);
            Box("K-A Roof slab North-3F", new Vector3(-3.2f, 11.82f, 27.0f), new Vector3(28.0f, 0.28f, 26.4f), Mat("BaseTrim"), g, isStatic: true);
            Box("K-A Roof slab East-2F terrace", new Vector3(14.2f, 7.82f, 27.0f), new Vector3(8.0f, 0.28f, 26.2f), Mat("BaseTrim"), g, isStatic: true);
            Box("K-A Roof slab North-2F wing", new Vector3(2.0f, 7.82f, 45.0f), new Vector3(16.4f, 0.28f, 10.4f), Mat("BaseTrim"), g, isStatic: true);

            // Rooftop parapet trim around South 3F main roofline
            Box("K-A Parapet South", new Vector3(1.5f, 12.18f, -8.05f), new Vector3(33.4f, 0.65f, 0.30f), Mat("ExteriorFacade"), g, isStatic: true);
            Box("K-A Parapet Copings", new Vector3(1.5f, 12.54f, -8.05f), new Vector3(33.6f, 0.12f, 0.42f), Mat("BaseTrim"), g, isStatic: true);

            if (art)
            {
                // Pitched corrugated-iron tropical heat-shield roof modules atop K-A South & North 3F ridges
                foreach (float rx in new[] { -10.5f, -2.5f, 5.5f, 13.5f })
                    staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_CorrugatedRoofModule", g, new Vector3(rx, 11.95f, -3.5f), 0f, pairRB: true, castShadows: false));
                foreach (float rx in new[] { -11.0f, -3.0f, 5.0f })
                    staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_CorrugatedRoofModule", g, new Vector3(rx, 11.95f, 27.5f), 0f, pairRB: true, castShadows: false));
                // Rooftop stainless-steel water tank cluster + concrete stair bulkhead + lightning mast!
                staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_RooftopTankCluster", g, new Vector3(10.5f, 11.95f, 2.0f), 0f, pairRB: true, castShadows: false));
                staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_RooftopTankCluster", g, new Vector3(-8.5f, 11.95f, 34.0f), 180f, pairRB: true, castShadows: false));

                // Background contextual plaques on K-A outer/upper facades
                Sign("K-A Ward 102 sign", "Sign_Ward102", new Vector3(14.08f, 2.72f, 11.1f), Vector3.left, new Vector2(0.82f, 0.30f), g, metalBacking: true);
                Sign("K-A Ward 201 sign", "Sign_Ward201", new Vector3(7.9f, 6.72f, 14.02f), Vector3.back, new Vector2(0.92f, 0.32f), g, metalBacking: true);
                Sign("K-A Ward 202 sign", "Sign_Ward202", new Vector3(11.9f, 6.72f, 14.02f), Vector3.back, new Vector2(0.92f, 0.32f), g, metalBacking: true);
            }

            // Background interior room backing shells behind ground-floor West/East K-A windows (inside Campus_Interiors)
            Box("K-A Ground West room floor", new Vector3(-8.0f, -0.05f, 1.0f), new Vector3(13.2f, 0.14f, 17.0f), Mat("FloorR"), interior, isStatic: true);
            Box("K-A Ground East room floor", new Vector3(13.5f, -0.05f, -3.0f), new Vector3(8.4f, 0.14f, 9.2f), Mat("FloorR"), interior, isStatic: true);
            // B-layer warm interior window glow fills inside K-A South ground flanks & light-court rooms
            Lamp("K-A West flank interior B", LightType.Point, new Vector3(-9.0f, 2.5f, -5.0f), 2.8f, 11f, new Color(0.86f, 0.96f, 0.86f), wingRep);
            Lamp("K-A East flank interior B", LightType.Point, new Vector3(13.5f, 2.5f, -4.5f), 2.8f, 10f, new Color(0.86f, 0.96f, 0.86f), wingRep);
        }

        // -----------------------------------------------------------------------------------------
        // 4. BLOCK K-B — 2-STORY OUTPATIENT CLINIC WING (1998-2002: x=[-38..-22], z=[-8..28])
        // -----------------------------------------------------------------------------------------
        private void BuildBlockKB_OutpatientWing(Transform perm, Transform realRoot, Transform repRoot, Transform wingPerm, Transform wingRep)
        {
            var g = Group("Block K-B · Outpatient Clinic 2F (2002)", perm);
            var interior = Group("K-B · Outpatient Waiting Interior", wingPerm);

            void Bay2002(Vector3 pos, float yaw)
            {
                if (!art)
                {
                    Box("K-B bay", pos + Vector3.up * 2f, new Vector3(4f, 4f, 0.3f), Mat("CampusStuccoR"), g, isStatic: true);
                    return;
                }
                staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_ClinicFacadeBay_2002", g, pos, yaw, pairRB: true, castShadows: false));
            }

            // Base concrete plinth & interior floor slabs (Ground Y=0, Floor 2 Y=4, Roof Y=8)
            Box("K-B plinth", new Vector3(-30f, 0.15f, 10f), new Vector3(16.6f, 0.32f, 36.6f), Mat("BaseTrim"), g, isStatic: true);
            Box("K-B Floor 2 slab", new Vector3(-30f, 3.92f, 10f), new Vector3(16.2f, 0.24f, 36.2f), Mat("BaseTrim"), g, isStatic: true);
            Box("K-B Roof slab", new Vector3(-30f, 7.92f, 10f), new Vector3(16.6f, 0.26f, 36.6f), Mat("BaseTrim"), g, isStatic: true);

            // 4 modular elevations across Floor 1 & Floor 2!
            foreach (float y in new[] { 0f, 4.0f })
            {
                // South front (z=-8) & North back (z=28): 4 bays across x=-36..-24
                foreach (float x in new[] { -36f, -32f, -28f, -24f })
                {
                    Bay2002(new Vector3(x, y, -8f), 0f);
                    Bay2002(new Vector3(x, y, 28f), 180f);
                }
                // East courtyard elevation (x=-22, facing K-A & LOC01!) & West elevation (x=-38): 9 bays z=-6..26
                for (float z = -6f; z <= 26f; z += 4f)
                {
                    Bay2002(new Vector3(-22f, y, z), -90f);
                    Bay2002(new Vector3(-38f, y, z), 90f);
                }
            }

            // Ground-floor South Entrance Portico Canopy (projecting to z=-11.2 over Main Outpatient Entry)
            Box("K-B Entry canopy slab", new Vector3(-30f, 3.45f, -9.6f), new Vector3(8.4f, 0.28f, 3.4f), Mat("BaseTrim"), g, isStatic: true);
            Box("K-B Entry canopy fascia", new Vector3(-30f, 3.55f, -11.3f), new Vector3(8.5f, 0.52f, 0.14f), Mat("DadoR"), g, isStatic: true);
            foreach (float cx in new[] { -33.6f, -26.4f })
                Box($"K-B Portico col {cx}", new Vector3(cx, 1.72f, -10.8f), new Vector3(0.38f, 3.44f, 0.38f), Mat("CampusStuccoR"), g, isStatic: true);

            if (art)
            {
                // Pitched corrugated iron roof along K-B length (Y=8.0, 2 rows x 4 modules)
                for (float z = -4f; z <= 24f; z += 8f)
                {
                    staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_CorrugatedRoofModule", g, new Vector3(-34f, 8.05f, z), 90f, pairRB: true, castShadows: false));
                    staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_CorrugatedRoofModule", g, new Vector3(-26f, 8.05f, z), 90f, pairRB: true, castShadows: false));
                }
                // Departmental Fascia Signs visible from Main Gate AND Night Courtyard!
                Sign("K-B Main Sign S", "Sign_KB_Outpatient", new Vector3(-30f, 4.45f, -8.38f), Vector3.back, new Vector2(5.6f, 1.15f), g, metalBacking: true);
                Sign("K-B East Courtyard Sign", "Sign_KB_Outpatient", new Vector3(-21.62f, 4.45f, 0.0f), Vector3.right, new Vector2(5.2f, 1.05f), g, metalBacking: true);
                Sign("K-B Pharmacy Sign", "Sign_Pharmacy", new Vector3(-21.62f, 2.85f, -5.8f), Vector3.right, new Vector2(2.1f, 0.65f), g, metalBacking: true);

                // Patient Motorbike Parking Shelters (Nhà xe bệnh nhân) in K-B front plaza
                staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_ParkingShedBay", g, new Vector3(-37.5f, 0f, -19.5f), 90f, pairRB: true, castShadows: false));
                staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_ParkingShedBay", g, new Vector3(-37.5f, 0f, -13.5f), 90f, pairRB: true, castShadows: false));
                // Exterior terrazzo waiting benches along K-B East colonnade
                foreach (float bz in new[] { -4f, 10f, 18f })
                    staticArchitecture.Add(PlaceBlenderModel("CTCC_PROP_TerrazzoBench", interior, new Vector3(-20.6f, 0f, bz), -90f, pairRB: false, castShadows: false));
            }

            // Subtle K-B canopy lighting (warm dim security in R, crisp outpatient tubes in B!)
            Lamp("K-B canopy porch R", LightType.Point, new Vector3(-30f, 3.1f, -9.8f), 3.6f, 14f, new Color(0.86f, 0.72f, 0.50f), realRoot);
            Lamp("K-B canopy tubes B", LightType.Point, new Vector3(-30f, 3.1f, -9.8f), 6.2f, 18f, new Color(0.88f, 0.96f, 0.88f), repRoot);
            Lamp("K-B east facade wash B", LightType.Point, new Vector3(-19.5f, 3.4f, 2.0f), 4.5f, 16f, new Color(0.84f, 0.94f, 0.86f), wingRep);
        }

        // -----------------------------------------------------------------------------------------
        // 5. BLOCK K-D — EMERGENCY DEPARTMENT & AMBULANCE CANOPY (1995-2000: x=[18..43], z=[14..32])
        // -----------------------------------------------------------------------------------------
        private void BuildBlockKD_EmergencyWing(Transform perm, Transform realRoot, Transform repRoot, Transform wingPerm, Transform wingRep)
        {
            var g = Group("Block K-D · Emergency & Ambulance Wing", perm);
            var interior = Group("K-D · Trauma & Observation Interior", wingPerm);

            // Main K-D 1-story tall emergency mass (x=[18..43], z=[14..32], height 4.25m + parapet)
            Box("K-D floor plinth", new Vector3(30.5f, 0.08f, 23.0f), new Vector3(25.4f, 0.24f, 18.4f), Mat("BaseTrim"), g, isStatic: true);
            Box("K-D roof slab", new Vector3(30.5f, 4.32f, 23.0f), new Vector3(25.6f, 0.28f, 18.6f), Mat("BaseTrim"), g, isStatic: true);
            Box("K-D red parapet band S", new Vector3(30.5f, 4.66f, 13.85f), new Vector3(25.6f, 0.42f, 0.28f), Mat("RedPaint"), g, isStatic: true);
            Box("K-D red parapet band E", new Vector3(43.15f, 4.66f, 23.0f), new Vector3(0.28f, 0.42f, 18.6f), Mat("RedPaint"), g, isStatic: true);
            Box("K-D parapet N", new Vector3(30.5f, 4.66f, 32.15f), new Vector3(25.6f, 0.42f, 0.28f), Mat("CampusStuccoR"), g, isStatic: true);

            // Transition bridge bay connecting locked LOC04 East (x=9.1..18.0, z=14..22) smoothly into K-D!
            Box("K-D to LOC04 bridge roof", new Vector3(13.6f, 3.85f, 18.0f), new Vector3(9.2f, 0.26f, 8.4f), Mat("BaseTrim"), g, isStatic: true);
            Surface("K-D to LOC04 bridge floor", new Vector3(13.6f, -0.05f, 18.0f), new Vector3(9.2f, 0.16f, 8.4f), "FloorR", "FloorB", tile: 2.0f, isStatic: true, parent: g);

            if (art)
            {
                // Modular 2002 clinical facades wrapping K-D South (z=14), North (z=32), and East (x=43)
                for (float x = 20f; x <= 40f; x += 4f)
                {
                    staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_ClinicFacadeBay_2002", g, new Vector3(x, 0f, 14f), 0f, pairRB: true, castShadows: false));
                    staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_ClinicFacadeBay_2002", g, new Vector3(x, 0f, 32f), 180f, pairRB: true, castShadows: false));
                }
                foreach (float z in new[] { 16f, 20f, 24f, 28f, 30f })
                    staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_ClinicFacadeBay_2002", g, new Vector3(43f, 0f, z), -90f, pairRB: true, castShadows: false));

                // HERO AMBULANCE PORTE-COCHÈRE (Mái đón xe cấp cứu: x=[43..49.5], z=[16..29], yaw=-90 projects East!)
                var canopy = PlaceBlenderModel("CTCC_ENV_AmbulanceCanopy", g, new Vector3(43.0f, 0f, 22.5f), -90f, pairRB: true, castShadows: false);
                staticArchitecture.Add(canopy);

                // Illuminated Crimson 'CẤP CỨU · EMERGENCY' signs (East facing Ambulance Road AND South facing LOC01 Courtyard!)
                Sign("KD Emergency Roof Sign E", "Sign_KD_Emergency", new Vector3(49.54f, 5.52f, 22.5f), Vector3.right, new Vector2(5.4f, 1.38f), g, metalBacking: false);
                // South-facing rooftop crimson lightbox directly visible from LOC01 Courtyard diagonal (x=7.2, z=-17)!
                Box("KD South Sign Box", new Vector3(24.5f, 5.45f, 14.0f), new Vector3(5.6f, 1.52f, 0.36f), Mat("MetalOld"), g, isStatic: true);
                Sign("KD Emergency Roof Sign S", "Sign_KD_Emergency", new Vector3(24.5f, 5.45f, 13.79f), Vector3.back, new Vector2(5.35f, 1.36f), g, metalBacking: false);
                Sign("KD XRay Plaque", "Sign_XRay", new Vector3(36.0f, 2.85f, 13.85f), Vector3.back, new Vector2(2.2f, 0.68f), g, metalBacking: true);

                // Parked 1990s Vietnamese Hospital Ambulance under Porte-Cochère!
                // Active parked cleanly under canopy in B (2005); slightly angled derelict/empty beside apron in R (2012)!
                var ambB = PlaceBlenderModel("CTCC_PROP_VintageAmbulance", repRoot, new Vector3(46.5f, 0f, 22.5f), 0f, pairRB: false, castShadows: false);
                var ambR = PlaceBlenderModel("CTCC_PROP_VintageAmbulance", realRoot, new Vector3(47.2f, 0f, 11.2f), 28f, pairRB: false, forceAged: true, castShadows: false);
                staticArchitecture.Add(ambB);
                staticArchitecture.Add(ambR);

                // Background Trauma/Observation Bay dressing inside K-D (Campus_Interiors)
                for (int i = 0; i < 3; i++)
                    staticArchitecture.Add(PlaceBlenderModel("CTCC_PROP_EmergencyStretcher", interior, new Vector3(22f + i * 4.5f, 0.2f, 21f), 0f, pairRB: true, castShadows: false));
                staticArchitecture.Add(PlaceBlenderModel("CTCC_PROP_OxygenCylinderCart", interior, new Vector3(41.2f, 0.16f, 21.0f), -90f, pairRB: false, castShadows: false));
            }

            // Iconic Crimson Emergency Canopy & South Lightbox Spill (creates unforgettable landmark glow in night fog!)
            Lamp("KD South Emergency Crimson Glow", LightType.Point, new Vector3(24.5f, 5.2f, 12.2f), 7.5f, 22f, new Color(0.95f, 0.22f, 0.16f), perm);
            Lamp("KD Ambulance Canopy Crimson Glow R", LightType.Point, new Vector3(46.8f, 3.85f, 22.5f), 5.5f, 18f, new Color(0.92f, 0.28f, 0.20f), realRoot);
            Lamp("KD Ambulance Canopy Flood B", LightType.Point, new Vector3(46.8f, 3.85f, 22.5f), 9.5f, 22f, new Color(0.92f, 0.98f, 0.92f), repRoot);
            Lamp("KD Trauma Interior B", LightType.Point, new Vector3(28.0f, 3.4f, 22.5f), 5.5f, 18f, new Color(0.86f, 0.96f, 0.88f), wingRep);
        }

        // -----------------------------------------------------------------------------------------
        // 6. BLOCK K-C — 3-STORY INPATIENT WARD BLOCK (2001-2004: x=[22..48], z=[46..68], 56 beds)
        // -----------------------------------------------------------------------------------------
        private void BuildBlockKC_InpatientWing(Transform perm, Transform realRoot, Transform repRoot, Transform wingPerm, Transform wingRep)
        {
            var g = Group("Block K-C · Inpatient Ward 3F (2004)", perm);

            Box("K-C plinth", new Vector3(35f, 0.14f, 57f), new Vector3(26.4f, 0.30f, 22.4f), Mat("BaseTrim"), g, isStatic: true);
            foreach (float y in new[] { 4.0f, 8.0f, 12.0f })
                Box($"K-C slab y={y}", new Vector3(35f, y - 0.08f, 57f), new Vector3(26.4f, 0.24f, 22.4f), Mat("BaseTrim"), g, isStatic: true);

            if (art)
            {
                // South front elevation (z=46, facing courtyard & K-D!): 6 x 3 floors of cantilevered patient balconies!
                foreach (float y in new[] { 0f, 4.0f, 8.0f })
                {
                    for (float x = 24f; x <= 44f; x += 4f)
                    {
                        staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_WardBalconyBay_2004", g, new Vector3(x, y, 46f), 0f, pairRB: true, castShadows: false));
                        staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_ClinicFacadeBay_2002", g, new Vector3(x, y, 68f), 180f, pairRB: true, castShadows: false));
                    }
                    // East end stairwell breeze-block tower + West flank
                    for (float z = 48f; z <= 66f; z += 4f)
                    {
                        bool stair = Mathf.Abs(z - 56f) < 2.5f;
                        staticArchitecture.Add(PlaceBlenderModel(stair ? "CTCC_ENV_StairwellBreezeBay_1987" : "CTCC_ENV_ClinicFacadeBay_2002",
                            g, new Vector3(48f, y, z), -90f, pairRB: true, castShadows: false));
                        staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_ClinicFacadeBay_2002", g, new Vector3(22f, y, z), 90f, pairRB: true, castShadows: false));
                    }
                }
                // Pitched corrugated metal roof + rooftop stainless water tank cluster
                for (float x = 26f; x <= 44f; x += 8f)
                {
                    staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_CorrugatedRoofModule", g, new Vector3(x, 12.1f, 52.5f), 0f, pairRB: true, castShadows: false));
                    staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_CorrugatedRoofModule", g, new Vector3(x, 12.1f, 61.5f), 0f, pairRB: true, castShadows: false));
                }
                staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_RooftopTankCluster", g, new Vector3(36f, 12.1f, 57f), 0f, pairRB: true, castShadows: false));
                Sign("K-C Inpatient Sign", "Sign_KC_Inpatient", new Vector3(35f, 12.45f, 44.55f), Vector3.back, new Vector2(6.8f, 1.25f), g, metalBacking: true);
            }
            else
            {
                Box("K-C mass", new Vector3(35f, 6.0f, 57f), new Vector3(26f, 12f, 22f), Mat("CampusStuccoR"), g, isStatic: true);
            }

            // Subtle upper-balcony night beacon (cool moonlit presence in R, operational floor glow in B!)
            Lamp("K-C South balconies B F2", LightType.Point, new Vector3(35f, 6.5f, 42.5f), 8.5f, 26f, new Color(0.84f, 0.94f, 0.85f), repRoot);
            Lamp("K-C South balconies R moon", LightType.Point, new Vector3(35f, 8.5f, 41.0f), 4.5f, 26f, new Color(0.46f, 0.64f, 0.78f), realRoot);
        }

        // -----------------------------------------------------------------------------------------
        // 7. BLOCK K-E — 2-STORY ADMINISTRATION & RECORDS BLOCK (1990s: x=[-40..-26], z=[41..63])
        // -----------------------------------------------------------------------------------------
        private void BuildBlockKE_AdminWing(Transform perm, Transform realRoot, Transform repRoot)
        {
            var g = Group("Block K-E · Administration 2F", perm);

            Box("K-E plinth", new Vector3(-33f, 0.14f, 52f), new Vector3(14.4f, 0.30f, 22.4f), Mat("BaseTrim"), g, isStatic: true);
            Box("K-E floor2 slab", new Vector3(-33f, 3.92f, 52f), new Vector3(14.4f, 0.24f, 22.4f), Mat("BaseTrim"), g, isStatic: true);
            Box("K-E roof slab", new Vector3(-33f, 7.92f, 52f), new Vector3(14.6f, 0.26f, 22.6f), Mat("BaseTrim"), g, isStatic: true);

            if (art)
            {
                foreach (float y in new[] { 0f, 4.0f })
                {
                    foreach (float x in new[] { -38f, -34f, -30f, -28f })
                    {
                        staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_ClinicFacadeBay_2002", g, new Vector3(x, y, 41f), 0f, pairRB: true, castShadows: false));
                        staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_ClinicFacadeBay_2002", g, new Vector3(x, y, 63f), 180f, pairRB: true, castShadows: false));
                    }
                    for (float z = 43f; z <= 61f; z += 4f)
                    {
                        staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_ClinicFacadeBay_2002", g, new Vector3(-26f, y, z), -90f, pairRB: true, castShadows: false));
                        staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_ClinicFacadeBay_2002", g, new Vector3(-40f, y, z), 90f, pairRB: true, castShadows: false));
                    }
                }
                for (float z = 46f; z <= 58f; z += 8f)
                {
                    staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_CorrugatedRoofModule", g, new Vector3(-36.5f, 8.05f, z), 90f, pairRB: true, castShadows: false));
                    staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_CorrugatedRoofModule", g, new Vector3(-29.5f, 8.05f, z), 90f, pairRB: true, castShadows: false));
                }
                Sign("K-E Admin Sign", "Sign_KE_Admin", new Vector3(-25.62f, 4.45f, 52f), Vector3.right, new Vector2(5.0f, 1.05f), g, metalBacking: true);
            }
            else
            {
                Box("K-E mass", new Vector3(-33f, 3.8f, 52f), new Vector3(14f, 7.6f, 22f), Mat("CampusStuccoR"), g, isStatic: true);
            }
        }

        // -----------------------------------------------------------------------------------------
        // 8. BLOCK K-F — LOGISTICS, KITCHEN, LAUNDRY, BOILER & 17.5m HOSPITAL WATER TOWER
        // -----------------------------------------------------------------------------------------
        private void BuildBlockKF_LogisticsAndWaterTower(Transform perm, Transform realRoot, Transform repRoot)
        {
            var g = Group("Block K-F · Logistics & Water Tower", perm);

            // Industrial 1-story logistics block (x=[-14..10], z=[59..73], H=4.1m)
            Box("K-F main shell", new Vector3(-2f, 2.05f, 66f), new Vector3(24.0f, 4.10f, 14.0f), Mat("ExteriorFacade"), g, tile: 2.8f, isStatic: true);
            Box("K-F brick plinth", new Vector3(-2f, 0.55f, 66f), new Vector3(24.2f, 1.10f, 14.2f), Mat("WeatheredBrick"), g, tile: 1.5f, isStatic: true);
            // East loading dock canopy facing North Service Road (x=10..12.8, z=61..71)
            Box("K-F loading dock slab", new Vector3(11.0f, 0.42f, 66f), new Vector3(2.2f, 0.84f, 10.0f), Mat("BaseTrim"), g, isStatic: true);
            Box("K-F loading dock canopy", new Vector3(11.4f, 3.55f, 66f), new Vector3(3.0f, 0.16f, 10.6f), Mat("CorrugatedRoofR"), g, isStatic: true);
            // Roll-up steel loading bay shutters
            foreach (float dz in new[] { 63.2f, 68.8f })
                Box($"K-F bay shutter {dz}", new Vector3(10.06f, 1.95f, dz), new Vector3(0.14f, 2.75f, 3.2f), Mat("MetalOld"), g, isStatic: true);
            // Brick boiler chimney stack on north-west corner of K-F
            Box("K-F boiler chimney", new Vector3(-12.5f, 5.8f, 71.5f), new Vector3(1.15f, 11.6f, 1.15f), Mat("WeatheredBrick"), g, tile: 1.4f, isStatic: true);

            if (art)
            {
                for (float rx = -10f; rx <= 6f; rx += 8f)
                    staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_CorrugatedRoofModule", g, new Vector3(rx, 4.15f, 66f), 0f, pairRB: true, castShadows: false));
                // Iconic 17.5m Vietnamese Hospital Concrete Water Tower (Đài nước An Sinh) rising on north skyline!
                var tower = PlaceBlenderModel("CTCC_ENV_WaterTowerAndRoofKit", g, new Vector3(-18.5f, 0f, 66.5f), 0f, pairRB: true, castShadows: false);
                staticArchitecture.Add(tower);
                Sign("K-F Service Sign", "Sign_KF_Service", new Vector3(10.2f, 4.15f, 66f), Vector3.right, new Vector2(4.6f, 0.98f), g, metalBacking: true);
            }
            Lamp("Water tower aviation red beacon", LightType.Point, new Vector3(-18.5f, 16.6f, 66.5f), 3.8f, 14f, new Color(0.95f, 0.18f, 0.14f), perm);
        }

        // -----------------------------------------------------------------------------------------
        // 9. COVERED CONNECTING WALKWAYS & SUNKEN REAR COURT (-1.90m, Core 03 §3.5)
        // -----------------------------------------------------------------------------------------
        private void BuildCoveredLinksAndRearCourt(Transform perm, Transform realRoot, Transform repRoot, Transform wingPerm)
        {
            var g = Group("Covered Walkways & Rear Sunken Court", perm);

            if (art)
            {
                // Covered Link 1: K-B to K-A (x=[-22..-15], z=5.0)
                staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_CoveredWalkwayBay", g, new Vector3(-20.0f, 0f, 5.0f), 0f, pairRB: true, castShadows: false));
                staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_CoveredWalkwayBay", g, new Vector3(-16.5f, 0f, 5.0f), 0f, pairRB: true, castShadows: false));

                // Covered Link 2: 2-Story Covered Bridge K-A to K-C (x=[10..22], z=48.0, Ground & Floor 2!)
                foreach (float y in new[] { 0f, 4.0f })
                {
                    foreach (float x in new[] { 12.0f, 16.0f, 20.0f })
                        staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_CoveredWalkwayBay", g, new Vector3(x, y, 48.0f), 0f, pairRB: true, castShadows: false));
                }
            }
            else
            {
                Box("Link KB-KA", new Vector3(-18.5f, 1.6f, 5.0f), new Vector3(7f, 3.2f, 4f), Mat("BaseTrim"), g, isStatic: true);
                Box("Link KA-KC", new Vector3(16.0f, 1.6f, 48.0f), new Vector3(12f, 3.2f, 4f), Mat("BaseTrim"), g, isStatic: true);
            }
            Lamp("Link KB-KA fluorescent B", LightType.Point, new Vector3(-18.5f, 2.65f, 5.0f), 3.6f, 12f, new Color(0.86f, 0.96f, 0.86f), repRoot);
            Lamp("Link KA-KC fluorescent B", LightType.Point, new Vector3(16.0f, 2.65f, 48.0f), 3.6f, 13f, new Color(0.86f, 0.96f, 0.86f), repRoot);

            // REAR SUNKEN COURT (Sân sau hạ cốt y = -1.90m: x=[37.2..49.0], z=[35.0..45.0])
            // Leaves x=[34.7..37.2] open for the B1 E05b sunken observation trench (-3.40m) built in LOC09_ServiceWing!
            var rc = Group("Rear Sunken Court (-1.90m)", wingPerm);
            Surface("Rear sunken court slab", new Vector3(43.1f, -2.00f, 40.0f), new Vector3(12.0f, 0.20f, 10.2f),
                "ConcretePavingR", "ConcretePavingB", solid: false, tile: 2.4f, isStatic: true, parent: rc);
            // Concrete retaining walls bounding the -1.90m court against 0.00m grade on South (z=35) and North (z=45)
            Box("Rear court retaining wall S", new Vector3(43.1f, -0.95f, 34.9f), new Vector3(12.2f, 2.0f, 0.28f), Mat("BaseTrim"), rc, isStatic: true);
            Box("Rear court retaining wall N", new Vector3(43.1f, -0.95f, 45.1f), new Vector3(12.2f, 2.0f, 0.28f), Mat("BaseTrim"), rc, isStatic: true);
            // Sloped concrete ramp descending from East service road (x=51.5, y=0) down into Rear Sunken Court (x=47.5, y=-1.90)
            var ramp = Box("Rear court vehicle ramp", new Vector3(49.8f, -1.02f, 40.0f), new Vector3(5.6f, 0.22f, 4.6f), Mat("WetAsphalt"), rc, tile: 2.2f, isStatic: true);
            ramp.transform.localRotation = Quaternion.Euler(0f, 0f, 19.5f);

            // Discreet West Morgue Ramp descending from West Service Branch (-20, 40.4, y=0) toward LOC10 exterior wall (-1.5, 40.95, y=-3.4)
            Box("Morgue exterior service pit", new Vector3(-5.8f, -3.50f, 40.4f), new Vector3(8.5f, 0.20f, 5.2f), Mat("WetAsphalt"), rc, tile: 2.2f, isStatic: true);
            Box("Morgue pit retaining N", new Vector3(-5.8f, -1.70f, 43.1f), new Vector3(8.8f, 3.6f, 0.26f), Mat("BaseTrim"), rc, isStatic: true);
            Box("Morgue pit retaining S", new Vector3(-5.8f, -1.70f, 37.7f), new Vector3(8.8f, 3.6f, 0.26f), Mat("BaseTrim"), rc, isStatic: true);
            var morgueRamp = Box("Morgue service ramp", new Vector3(-13.8f, -1.75f, 40.4f), new Vector3(11.2f, 0.22f, 4.4f), Mat("WetAsphalt"), rc, tile: 2.4f, isStatic: true);
            morgueRamp.transform.localRotation = Quaternion.Euler(0f, 0f, -17.5f);
        }

        // -----------------------------------------------------------------------------------------
        // 10. LANDSCAPE, STREET UTILITY POLES, ARCHITECTURAL LIGHTING & NIGHT HORIZON
        // -----------------------------------------------------------------------------------------
        private void BuildCampusLandscapeAndLighting(Transform perm, Transform realRoot, Transform repRoot)
        {
            var g = Group("Campus Landscape, Poles & Night Horizon", perm);

            if (art)
            {
                // Concrete Utility Poles + Sodium Streetlamps along South Avenue, Public Court, Ambulance Drive & Service Road
                var poles = new (Vector3 pos, float yaw, bool addLight)[]
                {
                    (new Vector3(-37.0f, 0f, -31.0f), 180f, false),
                    (new Vector3(-18.0f, 0f, -31.0f), 180f, true),
                    (new Vector3(-7.0f, 0f, -28.0f), 180f, true),
                    (new Vector3(16.0f, 0f, -29.0f), 180f, true),
                    (new Vector3(34.0f, 0f, -31.0f), 180f, true),
                    (new Vector3(56.5f, 0f, -16.0f), 90f, true),
                    (new Vector3(56.5f, 0f, 6.0f), 90f, true),
                    (new Vector3(56.5f, 0f, 36.0f), 90f, false),
                    (new Vector3(-19.5f, 0f, -14.0f), -90f, true),
                    (new Vector3(17.2f, 0f, 66.0f), 90f, false),
                };
                foreach (var pl in poles)
                {
                    staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_UtilityPoleAndLamp", g, pl.pos, pl.yaw, pairRB: false, castShadows: false));
                    if (pl.addLight)
                    {
                        Vector3 lampPos = pl.pos + Quaternion.Euler(0, pl.yaw, 0) * new Vector3(0, 6.6f, 2.0f);
                        Lamp("Campus sodium streetlamp", LightType.Point, lampPos, 7.8f, 22f, new Color(0.98f, 0.72f, 0.38f), perm);
                    }
                }

                // Tropical Hospital Campus Trees (cây bàng / xà cừ / phượng vĩ) framing courtyards & avenues
                // Carefully placed outside every doorway, road lane, sightline cone to K-B/K-A/K-D signs, and locked core!
                var campusTrees = new (Vector3 pos, float yaw, float scale, bool varA)[]
                {
                    // West garden between K-B and K-A
                    (new Vector3(-17.8f, 0f, -23.5f), 25f, 1.35f, true),
                    (new Vector3(-19.2f, 0f, -10.5f), 110f, 1.20f, false),
                    (new Vector3(-18.6f, 0f, 15.5f), 210f, 1.25f, true),
                    (new Vector3(-18.4f, 0f, 29.0f), 65f, 1.30f, false),
                    (new Vector3(-24.0f, 0f, 36.0f), 160f, 1.25f, true),
                    // East lawn framing K-D Emergency & Ambulance approach (kept low/wide so 'CẤP CỨU' sign stays clear!)
                    (new Vector3(22.5f, 0f, -24.5f), 85f, 1.35f, false),
                    (new Vector3(36.0f, 0f, -24.0f), 240f, 1.40f, true),
                    (new Vector3(38.0f, 0f, -12.0f), 15f, 1.25f, false),
                    (new Vector3(34.5f, 0f, 5.5f), 175f, 1.15f, true),
                    // North & Rear perimeter trees
                    (new Vector3(-34.0f, 0f, 71.0f), 40f, 1.45f, true),
                    (new Vector3(-22.0f, 0f, 74.5f), 195f, 1.40f, false),
                    (new Vector3(34.0f, 0f, 74.0f), 305f, 1.35f, true),
                    (new Vector3(53.0f, 0f, 68.0f), 120f, 1.40f, false),
                    // Outer avenue flanking trees across the street
                    (new Vector3(-36.0f, 0f, -38.5f), 15f, 1.50f, true),
                    (new Vector3(-12.0f, 0f, -38.5f), 145f, 1.45f, false),
                    (new Vector3(18.0f, 0f, -38.5f), 260f, 1.50f, true),
                    (new Vector3(44.0f, 0f, -38.5f), 60f, 1.45f, false),
                };
                foreach (var t in campusTrees)
                {
                    staticArchitecture.Add(PlaceBlenderModel(t.varA ? "CTCC_ENV_CourtyardTree_A" : "CTCC_ENV_CourtyardTree_B",
                        g, t.pos, t.yaw, pairRB: false, scale: Vector3.one * t.scale, castShadows: false));
                }

                // Courtyard elephant-ear shrubs lining K-A front planters & Inner Light Court
                var campusShrubs = new (Vector3 p, float yaw, float s)[]
                {
                    (new Vector3(-7.5f, 0f, -9.2f), 25f, 1.15f),
                    (new Vector3(-12.0f, 0f, -9.2f), 160f, 1.10f),
                    (new Vector3(15.2f, 0f, -9.2f), 80f, 1.15f),
                    (new Vector3(19.5f, 0f, -6.5f), 210f, 1.25f),
                    (new Vector3(20.5f, 0f, 11.5f), 45f, 1.20f),
                    (new Vector3(8.0f, 0.14f, 11.3f), 90f, 1.05f),
                    (new Vector3(10.5f, 0.14f, 12.1f), 220f, 1.10f),
                };
                foreach (var sp in campusShrubs)
                    staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_TropicalShrub", g, sp.p, sp.yaw, pairRB: false, scale: Vector3.one * sp.s, castShadows: false));
            }

            // Moonlit architectural bounce fills so 3-story K-A, K-B, K-D, and Inner Light Court read richly in R (2012)
            // instead of falling into pitch-black voids, while keeping horror contrast!
            Lamp("K-A Upper Facade Moon Wash R", LightType.Point, new Vector3(4.0f, 8.6f, -15.5f), 26.0f, 42f, new Color(0.52f, 0.72f, 0.90f), realRoot);
            Lamp("K-A Upper Facade Flank W R", LightType.Point, new Vector3(-9.5f, 7.8f, -15.0f), 16.0f, 32f, new Color(0.50f, 0.70f, 0.88f), realRoot);
            Lamp("K-A Upper Facade Flank E R", LightType.Point, new Vector3(15.5f, 7.8f, -15.0f), 16.0f, 32f, new Color(0.50f, 0.70f, 0.88f), realRoot);
            Lamp("K-A Upper Facade Night Glow B", LightType.Point, new Vector3(4.0f, 8.6f, -15.5f), 22.0f, 44f, new Color(0.80f, 0.92f, 0.84f), repRoot);
            Lamp("K-B Courtyard Moon Wash R", LightType.Point, new Vector3(-20.0f, 7.8f, -13.0f), 20.0f, 36f, new Color(0.50f, 0.70f, 0.88f), realRoot);
            Lamp("K-B Courtyard Operational B", LightType.Point, new Vector3(-20.0f, 7.8f, -13.0f), 18.0f, 36f, new Color(0.80f, 0.92f, 0.84f), repRoot);
            Lamp("Inner Light Court Moon Fill R", LightType.Point, new Vector3(9.9f, 5.4f, 11.1f), 12.5f, 20f, new Color(0.54f, 0.74f, 0.90f), realRoot);
            Lamp("Inner Light Court Ward Glow B", LightType.Point, new Vector3(9.9f, 5.4f, 11.1f), 13.0f, 20f, new Color(0.84f, 0.94f, 0.86f), repRoot);
            Lamp("East Wing Overview Moon R", LightType.Point, new Vector3(26.0f, 8.8f, 3.0f), 22.0f, 38f, new Color(0.50f, 0.70f, 0.88f), realRoot);
            Lamp("Canopy Sign Subtle Wash", LightType.Point, new Vector3(4.0f, 3.85f, -12.6f), 4.2f, 10f, new Color(0.86f, 0.90f, 0.84f), perm);
            // Floor 2 S1 West Gallery subtle breeze-block moonlight & 2005 operational tube fill
            Lamp("S1 West Gallery Moon Mid R", LightType.Point, new Vector3(-12.3f, 6.4f, 14.0f), 4.2f, 16f, new Color(0.50f, 0.70f, 0.86f), realRoot);
            Lamp("S1 West Gallery Moon North R", LightType.Point, new Vector3(-12.3f, 6.4f, 31.0f), 4.8f, 16f, new Color(0.52f, 0.72f, 0.88f), realRoot);
            Lamp("S1 West Gallery Operational B", LightType.Point, new Vector3(-12.3f, 6.4f, 22.0f), 6.5f, 22f, new Color(0.84f, 0.94f, 0.86f), repRoot);

            // Distant dark tropical treeline silhouette enclosure beyond site perimeter so camera never sees a hard void cliff!
            Box("Distant treeline S", new Vector3(8f, 5.5f, -52f), new Vector3(170f, 13f, 2f), Mat("HorizonTree"), g, isStatic: true);
            Box("Distant treeline N", new Vector3(8f, 6.5f, 96f), new Vector3(170f, 15f, 2f), Mat("HorizonTree"), g, isStatic: true);
            Box("Distant treeline W", new Vector3(-62f, 6.0f, 22f), new Vector3(2f, 14f, 152f), Mat("HorizonTree"), g, isStatic: true);
            Box("Distant treeline E", new Vector3(78f, 6.0f, 22f), new Vector3(2f, 14f, 152f), Mat("HorizonTree"), g, isStatic: true);
        }
    }
}
