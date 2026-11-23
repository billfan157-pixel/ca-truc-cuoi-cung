using UnityEngine;

namespace CTCC.Editor
{
    public sealed partial class HospitalAuthoring
    {
        private const float WardLevel = 4f;
        private const float ArchiveLevel = 8f;
        private const float BasementLevel = -3.4f;
        private const float WardOffset = 7.1f;

        private void BuildHospitalExpansion()
        {
            var upper = multiScene ? zones["LOC05"].permanent : Group("Upper hospital", permanent);
            var service = multiScene ? zones["LOC09"].permanent : Group("Basement service corridor", permanent);
            if (multiScene)
            {
                var grade = zones["LOC04"];
                zones["LOC02"].region.upperConnectors = new[]
                {
                    Box("S1 · Khu B", new(-1.07f, 1.2f, -4), new(.12f, 2.4f, 2.65f),
                        Mat("WoodDoorR"), zones["LOC02"].permanent, solid: true)
                };
                grade.region.upperConnectors = new[]
                {
                    Box("S3 · Khu B", new(4, 1.2f, 22.02f), new(2.65f, 2.4f, .12f),
                        Mat("WoodDoorR"), grade.permanent, solid: true)
                };
                grade.region.serviceConnector = Box("S2 · Kỹ thuật B1", new(7, 1.2f, 22.02f), new(2.65f, 2.4f, .12f),
                    Mat("MetalOld"), grade.permanent, solid: true);
            }

            // S1 is the independent public core. The emergency transfer stair joins the same ward,
            // and the east service flight reaches B1 without cutting through the ward.
            Flight(upper, "S1 · Lễ tân → Khu B", new(-1.0f, 0, -4), new(-11, WardLevel, -4), 2.7f);
            Deck(upper, "S1 · West gallery", WardLevel, -13.7f, -11, -5.35f, 38.2f, true);
            Deck(upper, "S1 · Ward bridge", WardLevel, -11, -3.5f, 35.9f, 38.2f, true);
            Flight(upper, "S3 · Cấp cứu → Khu B", new(4, 0, 21.99f), new(4, WardLevel, 32.1f), 2.7f);
            Deck(service, "S2 · Upper threshold", 0f, 5.65f, 8.35f, 21.65f, 22.75f);
            Flight(service, "S2 · Cấp cứu → Kỹ thuật", new(7, 0, 22.7f), new(7, BasementLevel, 30.89f), 2.7f);
            Deck(service, "S2 · Lower threshold", BasementLevel, 5.65f, 8.35f, 30.87f, 31.55f);
            Flight(upper, "S1 · Khu B → Kho", new(-3.5f, WardLevel, 34.35f), new(-13.5f, ArchiveLevel, 34.35f), 2.2f);
            Deck(upper, "S1 · Archive landing", ArchiveLevel, -16.2f, -13.5f, 33.25f, 38.15f, true);
            Deck(upper, "S1 · Archive bridge", ArchiveLevel, -13.5f, -3.5f, 35.45f, 38.15f, true);

            RoomShell(upper, "LOC05 · Khu B", WardLevel, 3.6f, -3.5f, 7.5f, 32.1f, 38.2f,
                southDoor: 4f, northDoor: 4f, westDoor: 37.05f, secondWestDoor: 34.35f);
            RoomShell(upper, "LOC06 · Phòng trực", WardLevel, 3.6f, 2.6f, 5.4f, 38.2f, 47.1f,
                southDoor: 4f);
            RoomShell(upper, "LOC07 · Kho", ArchiveLevel, 3.3f, -3.5f, 3.5f, 29.5f, 38.5f,
                westDoor: 36.8f, eastDoor: 32.3f);
            RoomShell(upper, "LOC08 · Giám đốc", ArchiveLevel, 3.3f, 3.5f, 8.5f, 29.5f, 35.5f,
                westDoor: 32.3f);
            WallStrip(upper, "LOC08 · Khóa G05", ArchiveLevel, 2.45f, false, 3.5f, 31.4f, 33.2f);
            // Shelving breaks sightlines while leaving two ways around each end.
            Box("LOC07 · freestanding storage partition", new(1.1f, ArchiveLevel + 1f, 34f),
                new(.55f, 2f, 3.3f), Mat("MetalOld"), upper, solid: true);

            var exit = Group("GATE-G03", upper, new(3.1f, WardLevel, 44.3f));
            Box("Duty door leaf", new(.9f, 1.1f, 0), new(1.8f, 2.2f, .1f), Mat("WoodDoorR"), exit, solid: true);
            WallStrip(upper, "G03 left jamb", WardLevel, 3.6f, true, 44.3f, 2.6f, 3.1f);
            WallStrip(upper, "G03 right jamb", WardLevel, 3.6f, true, 44.3f, 4.9f, 5.4f);
            Box("G03 lintel", new(4, WardLevel + 2.98f, 44.3f), new(1.8f, 1.24f, .2f), Mat("PlasterR"), upper, solid: true);
            var gate = new DoorPose { pivot = exit, openAngle = -92f };
            if (multiScene) zones["LOC05"].region.archiveExitDoor = gate;
            else world.archiveExitDoor = gate;

            RoomShell(service, "LOC09 · Kỹ thuật", BasementLevel, 3.1f, 4.5f, 10.5f, 31.1f, 45.5f,
                southDoor: 7f, westDoor: 39f, eastDoor: 42f);
            RoomShell(service, "LOC10 · Nhà xác", BasementLevel, 3.1f, -1.5f, 4.5f, 36f, 42f, eastDoor: 39f);
            // W-Q04: Discreet West service door in LOC10 leading toward independent rear morgue ramp (clear of dry cabinet at z=39, locked, never connects to public lobby or Khu C!)
            Box("LOC10 · Cửa dịch vụ nhà xác (khóa)", new(-1.42f, BasementLevel + 1.15f, 40.95f),
                new(.12f, 2.30f, 1.32f), Mat("MetalOld"), service, solid: true, isStatic: true);
            if (art)
                Sign("Morgue service sign", "Sign_MorgueService", new(-1.35f, BasementLevel + 2.55f, 40.95f), Vector3.right, new(1.15f, 0.34f), service);

            var morgueLock = Box("Cabinet wing lock", new(4.5f, BasementLevel + 1.19f, 39),
                new(.10f, 2.38f, 1.80f), Mat("MetalOld"), service, solid: true);
            morgueLock.SetActive(false);

            // LOC11 opens North (x=12.5, z=45) to the B1 exterior perimeter inspection gallery so E05b (2004 sealed rear door)
            // is directly observable from R BEFORE G06 while keeping LOC12's G06 door and 2004 sealed east wall 100% impenetrable!
            RoomShell(service, "LOC11 · Tiền sảnh C", BasementLevel, 3.1f, 10.5f, 14.5f, 39f, 45f,
                northDoor: 12.5f, westDoor: 42f, eastDoor: 42f);
            RoomShell(service, "LOC12 · Khu C", BasementLevel, 3.1f, 14.5f, 34.5f, 35f, 49f, westDoor: 42f);
            WallStrip(service, "LOC12 · Cửa khóa ngoài G06", BasementLevel, 2.3f, false, 14.5f, 41.1f, 42.9f);
            for (int bay = 0; bay < 3; bay++)
            {
                for (int bed = 0; bed < 4; bed++)
                    Box($"LOC12 · Bay {bay + 1} bed {bed + 1}",
                        new(17.2f + bay * 6f + (bed % 2) * 2.3f, BasementLevel + .4f, 37.5f + (bed / 2) * 8.3f),
                        new(.88f, .8f, 2.05f), Mat("MetalPaint"), service, solid: true);
                if (bay < 2)
                    WallStrip(service, "Khu C · partition", BasementLevel, 2.6f, false, 21 + bay * 6, 35, 40.65f);
            }
            // Interior face of 2004 infill inside LOC12 + exterior face assembly E05b in the sunken east inspection trench!
            Box("LOC12 · 2004 infill", new(34.38f, BasementLevel + 1.12f, 42),
                new(.12f, 2.24f, 1.45f), Mat(art ? "BrickInfill2004" : "WeatheredBrick"), service);

            // Core 03 §3.5 / W-Q03: Accessible B1 Exterior Observation Gallery (LOC11 north door -> North perimeter -> East sunken trench facing E05b!)
            BuildB1E05bObservationGallery(service);
            var cabinetProxy = Box("LOC10 · tủ khô", new(-.9f, BasementLevel + .94f, 39),
                new(.6f, 1.88f, 1.36f), Mat("MetalOld"), service, solid: true);
            var panelProxy = Box("LOC09 · bảng điện", new(9.98f, BasementLevel + 1.37f, 34.5f),
                new(.28f, .84f, 1.12f), Mat("MetalOld"), service, solid: true);
            if (art)
            {
                cabinetProxy.GetComponent<Renderer>().enabled = false;
                panelProxy.GetComponent<Renderer>().enabled = false;
                staticArchitecture.Add(PlaceBlenderModel("CTCC_PROP_MorgueDryCabinet", service,
                    new(-.9f, BasementLevel, 39), -90f));
                staticArchitecture.Add(PlaceBlenderModel("CTCC_PROP_ServiceLoadPanel", service,
                    new(9.98f, BasementLevel + 1.37f, 34.5f), 90f));
                staticArchitecture.Add(PlaceBlenderModel("CTCC_PROP_MedicineCart", upper,
                    new(3.05f, WardLevel, 42), 90f));
            }
            CapsuleTarget("CH04_POWER", "Bảng tải điện khu kỹ thuật", new(9.72f, BasementLevel + 1.37f, 34.5f), new(.45f, .85f, 1.12f), service);
            CapsuleTarget("E04_E07", "Tủ khô · hồ sơ tiếp nhận", new(-.57f, BasementLevel + 1.25f, 39), new(.45f, .84f, 1.18f), service);
            CapsuleTarget("AN04", "Phòng trực · lịch và băng họp", new(3.88f, WardLevel + 1.16f, 46.78f), new(.7f, .8f, .4f), upper);
            for (int i = 0; i < 3; i++)
                Lamp("B1 · bulkhead", LightType.Point, new(7.4f, BasementLevel + 2.6f, 33.4f + 5f * i),
                    2.3f, 7.3f, new Color(.74f, .84f, .80f), service);
            Lamp("Nhà xác · desk spill", LightType.Point, new(1, BasementLevel + 2.49f, 39),
                2.8f, 6.7f, new Color(.85f, .90f, .77f), service);
            var relayLight = Lamp("Workshop · circuit fill", LightType.Point, new(7.7f, BasementLevel + 2.62f, 36.3f),
                4.0f, 8f, new Color(.82f, .95f, .82f), service);
            var cabinetLight = Lamp("Nhà xác · switched task light", LightType.Point, new(.7f, BasementLevel + 2.3f, 39.2f),
                6.1f, 5.5f, new Color(1f, .92f, .79f), service);
            relayLight.enabled = false;
            cabinetLight.enabled = false;
            if (multiScene)
            {
                zones["LOC09"].region.morgueLock = morgueLock;
                zones["LOC09"].region.circuitLights = new[] { relayLight, cabinetLight };
            }
            Sign("Ward stairs", "M31_KhuB", new(4, 2.85f, 21.985f), Vector3.back, new(1.72f, .45f));
            Sign("Service stairs", "M31_Service", new(7, 2.85f, 21.985f), Vector3.back, new(1.72f, .45f));
            Sign("Archive stairs", "M31_Kho", new(-3.39f, WardLevel + 2.76f, 34.35f), Vector3.right, new(1.72f, .45f), upper);
            Sign("Morgue entrance", "M31_Morgue", new(4.605f, BasementLevel + 2.77f, 39), Vector3.right, new(1.45f, .38f), service);
        }

        /// <summary>
        /// Core 03 §3.5 / W-Q03: Walkable B1 perimeter inspection gallery allowing player in R (before G06)
        /// to walk from LOC11 north door around LOC12's exterior north wall to the east sunken light-trench
        /// at (35.8, -3.4, 42.0) and directly inspect the 2004 sealed bricked-in rear door E05b + broken steps
        /// leading up 1.50m to the Rear Sunken Court (y = -1.90m). Zero entry into LOC12!
        /// </summary>
        private void BuildB1E05bObservationGallery(Transform service)
        {
            var gal = Group("B1 · Hành lang kỹ thuật quan sát E05b", service);
            float y = BasementLevel;
            const float h = 3.05f;

            // Leg 1: North vestibule from LOC11 (x=[11.3..13.7], z=[44.8..51.5] overlaps LOC11 floor cleanly!)
            Deck(gal, "E05b gallery west leg floor", y, 11.3f, 13.7f, 44.8f, 51.5f);
            Box("E05b gallery west ceiling", new(12.5f, y + h + .1f, 48.25f), new(2.6f, .2f, 6.6f), Mat("PlasterR"), gal, solid: true, isStatic: true);
            WallStrip(gal, "E05b leg1 west", y, h, false, 11.3f, 45.0f, 51.5f);
            WallStrip(gal, "E05b leg1 east", y, h, false, 13.7f, 45.0f, 49.3f);

            // Leg 2: East-West north perimeter passage behind LOC12 north wall (x=[13.5..37.2], z=[49.3..51.5])
            Deck(gal, "E05b gallery north leg floor", y, 13.5f, 37.2f, 49.3f, 51.5f);
            Box("E05b gallery north ceiling", new(25.45f, y + h + .1f, 50.4f), new(23.6f, .2f, 2.4f), Mat("PlasterR"), gal, solid: true, isStatic: true);
            WallStrip(gal, "E05b leg2 north", y, h, true, 51.5f, 11.3f, 37.2f);
            WallStrip(gal, "E05b leg2 south outer", y, h, true, 49.3f, 13.7f, 34.7f);

            // Leg 3: East sunken observation pit facing LOC12's 2004 sealed east rear door (x=[34.7..37.2], z=[39.5..49.5])
            // Open above y=-1.90 to the Rear Sunken Court so rain & night moonlight fall onto the bricked-in door!
            Deck(gal, "E05b east pit floor", y, 34.7f, 37.2f, 39.5f, 49.5f);
            WallStrip(gal, "E05b pit east retaining wall", y, 1.55f, false, 37.2f, 39.5f, 51.5f);
            WallStrip(gal, "E05b pit south retaining wall", y, 1.65f, true, 39.5f, 34.7f, 37.2f);
            // Low protective masonry rail along sunken court lip so player stays in B1 pit without falling out of bounds
            Box("E05b pit east upper rail", new(37.2f, y + 2.35f, 44.4f), new(.18f, 1.60f, 9.8f), Mat("MetalOld"), gal, solid: true).GetComponent<Renderer>().enabled = false;

            // Hero Blender 4.5 architectural evidence: 2004 sealed bricked-in rear door on exterior face of LOC12 (x=34.58, z=42.0)!
            if (art)
            {
                var sealedDoor = PlaceBlenderModel("CTCC_ENV_B1SealedDoorE05b", gal, new(34.62f, y, 42.0f), -90f, pairRB: false);
                staticArchitecture.Add(sealedDoor);
                Decal("E05b_ExteriorWaterSeep", "WaterRun", new(34.66f, y + 1.35f, 42.0f), Vector3.right, 1.95f, 2.15f, 0.28f, gal, 0.90f);
                // Broken disused exterior concrete stair stub rising from pit (-3.40m) toward Rear Sunken Court (-1.90m, 1.50m rise)
                for (int s = 0; s < 5; s++)
                {
                    Box($"E05b_OldStep_{s}", new(36.75f, y + 0.14f + s * 0.28f, 40.25f + s * 0.28f),
                        new(0.85f, 0.28f, 0.30f), Mat("BaseTrim"), gal, solid: true, isStatic: true);
                }
            }
            else
            {
                Box("E05b exterior brick face", new(34.62f, y + 1.12f, 42.0f), new(.16f, 2.24f, 1.50f), Mat("WeatheredBrick"), gal, solid: true);
            }

            // Physical observation anchor target for E05b (non-breaking pre-G06 evidence landmark!)
            CapsuleTarget("E05B_SEALED_DOOR", "Dấu cửa sau Khu C bị xây bịt năm 2004 (E05b)",
                new(35.05f, y + 1.25f, 42.0f), new(0.75f, 1.65f, 1.45f), gal);

            // Subtle bulkhead navigation lights along B1 inspection gallery & cool moonlight down into the east pit
            Lamp("B1 E05b gallery bulkhead W", LightType.Point, new(12.5f, y + 2.55f, 49.8f), 1.85f, 8.0f, new Color(.74f, .84f, .80f), gal);
            Lamp("B1 E05b gallery bulkhead M", LightType.Point, new(24.5f, y + 2.55f, 50.4f), 1.95f, 9.5f, new Color(.74f, .84f, .80f), gal);
            Lamp("B1 E05b gallery bulkhead E", LightType.Point, new(35.8f, y + 2.55f, 48.2f), 2.15f, 9.0f, new Color(.74f, .84f, .80f), gal);
            Lamp("B1 E05b sunken court moon key", LightType.Point, new(36.1f, y + 2.65f, 42.0f), 3.20f, 8.5f, new Color(.58f, .76f, .92f), gal);
        }

        private void Flight(Transform parent, string name, Vector3 start, Vector3 end, float width)
        {
            Vector3 horizontal = Vector3.ProjectOnPlane(end - start, Vector3.up);
            float run = horizontal.magnitude;
            float rise = end.y - start.y;
            var flight = Group(name, parent, start);
            flight.localRotation = Quaternion.LookRotation(horizontal);
            int steps = Mathf.CeilToInt(Mathf.Abs(rise) / .17f);
            var ramp = Box("Continuous stair collider", new(0, rise / 2f - .075f, run / 2f),
                new(width, .14f, Vector2.Distance(new(0, 0), new(run, rise)) + .05f),
                Mat("FloorR"), flight, solid: true);
            ramp.transform.localRotation = Quaternion.Euler(-Mathf.Atan2(rise, run) * Mathf.Rad2Deg, 0, 0);
            ramp.GetComponent<Renderer>().enabled = false;
            for (int i = 0; i < steps; i++)
            {
                float progress = (i + .5f) / steps;
                if (art)
                    PlaceBlenderModel("CTCC_ENV_TerrazzoTread", flight, new(0, rise * progress, run * progress),
                        rise > 0 ? 0f : 180f, pairRB: false, scale: new Vector3(width, 1f, run / steps), castShadows: false);
                else
                    Box("Terrazzo tread", new(0, rise * progress - .12f, run * progress),
                        new(width, .25f, run / steps + .012f), Mat("FloorR"), flight);
            }
            foreach (float side in new[] { -.5f, .5f })
            {
                var rail = Box("Stair guard", new(width * side, rise / 2f + .48f, run / 2f),
                    new(.1f, 1.16f, Mathf.Sqrt(run * run + rise * rise)), Mat("MetalOld"), flight, solid: true);
                rail.transform.localRotation = ramp.transform.localRotation;
            }
        }

        private void Deck(Transform parent, string name, float y, float x0, float x1, float z0, float z1, bool rail = false)
        {
            var floor = Box(name, new((x0 + x1) / 2f, y - .1f, (z0 + z1) / 2f),
                new(x1 - x0, .2f, z1 - z0), Mat("FloorR"), parent, solid: true, tile: 1.6f, isStatic: true);
            AddFinish(new LayerFinish { renderer = floor.GetComponent<Renderer>(), real = Mat("FloorR"), report = Mat("FloorB") });
            if (rail)
            {
                // The connecting edge remains open where stairs and bridges meet.
                bool longitudinal = (z1 - z0) > (x1 - x0);
                Box("Gallery exterior guard", longitudinal ? new(x0, y + .47f, (z0 + z1) / 2f) :
                    new((x0 + x1) / 2f, y + .47f, z0), longitudinal ? new(.12f, 1.05f, z1 - z0) :
                    new(x1 - x0, 1.05f, .12f), Mat("MetalOld"), parent, solid: true);
            }
        }

        private void RoomShell(Transform parent, string name, float y, float height, float x0, float x1, float z0, float z1,
            float southDoor = float.NaN, float northDoor = float.NaN, float westDoor = float.NaN, float eastDoor = float.NaN,
            float secondWestDoor = float.NaN)
        {
            parent = Group(name, parent);
            Deck(parent, name + " · floor", y, x0, x1, z0, z1);
            Box(name + " · slab", new((x0 + x1) / 2f, y + height + .1f, (z0 + z1) / 2f),
                new(x1 - x0, .2f, z1 - z0), Mat("PlasterR"), parent, solid: true, tile: 2f, isStatic: true);
            WallStrip(parent, "South", y, height, true, z0, x0, x1, southDoor);
            WallStrip(parent, "North", y, height, true, z1, x0, x1, northDoor);
            if (float.IsNaN(secondWestDoor))
                WallStrip(parent, "West", y, height, false, x0, z0, z1, westDoor);
            else
            {
                float split = (westDoor + secondWestDoor) / 2f;
                WallStrip(parent, "West (stairs)", y, height, false, x0, z0, split, secondWestDoor);
                WallStrip(parent, "West (gallery)", y, height, false, x0, split, z1, westDoor);
            }
            WallStrip(parent, "East", y, height, false, x1, z0, z1, eastDoor);
        }

        private void WallStrip(Transform parent, string name, float y, float height, bool alongX,
            float axis, float a, float b, float door = float.NaN)
        {
            void Segment(float from, float to, float low, float tall)
            {
                if (to <= from) return;
                if (low == 0 && tall > 1.2f)
                {
                    Segment(from, to, 0, 1.15f);
                    Segment(from, to, 1.15f, tall - 1.15f);
                    return;
                }
                float middle = (from + to) * .5f;
                float center = y + low + tall * .5f;
                string real = low == 0 && tall < 1.2f ? "DadoR" : "PlasterR";
                string report = real == "DadoR" ? "DadoB" : "PlasterB";
                var part = Box(name, alongX ? new(middle, center, axis) : new(axis, center, middle),
                    alongX ? new(to - from, tall, .18f) : new(.18f, tall, to - from),
                    Mat(real), parent, solid: true, tile: 2f, isStatic: true);
                AddFinish(new LayerFinish { renderer = part.GetComponent<Renderer>(), real = Mat(real), report = Mat(report) });
            }
            if (float.IsNaN(door)) Segment(a, b, 0, height);
            else
            {
                const float halfWidth = .91f;
                Segment(a, door - halfWidth, 0, height);
                Segment(door + halfWidth, b, 0, height);
                Segment(door - halfWidth, door + halfWidth, 2.42f, height - 2.42f);
            }
        }
    }
}
