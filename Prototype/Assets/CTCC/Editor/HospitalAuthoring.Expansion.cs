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
            RoomShell(service, "LOC11 · Tiền sảnh C", BasementLevel, 3.1f, 10.5f, 14.5f, 39f, 45f,
                westDoor: 42f, eastDoor: 42f);
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
            Box("LOC12 · 2004 infill", new(34.38f, BasementLevel + 1.12f, 42),
                new(.12f, 2.24f, 1.45f), Mat("WeatheredBrick"), service);
            var cabinetProxy = Box("LOC10 · tủ khô", new(-.9f, BasementLevel + .94f, 39),
                new(.6f, 1.88f, 1.36f), Mat("MetalOld"), service, solid: true);
            var panelProxy = Box("LOC09 · bảng điện", new(9.98f, BasementLevel + 1.37f, 34.5f),
                new(.28f, .84f, 1.12f), Mat("MetalOld"), service, solid: true);
            if (art)
            {
                cabinetProxy.GetComponent<Renderer>().enabled = false;
                panelProxy.GetComponent<Renderer>().enabled = false;
                staticArchitecture.Add(PlaceBlenderModel("CTCC_PROP_MorgueDryCabinet", service,
                    new(-.9f, BasementLevel, 39), 90f));
                staticArchitecture.Add(PlaceBlenderModel("CTCC_PROP_ServiceLoadPanel", service,
                    new(9.98f, BasementLevel + 1.37f, 34.5f), -90f));
                staticArchitecture.Add(PlaceBlenderModel("CTCC_PROP_MedicineCart", upper,
                    new(3.05f, WardLevel, 42), 90f));
            }
            CapsuleTarget("CH04_POWER", "Bảng tải điện khu kỹ thuật", new(9.72f, BasementLevel + 1.37f, 34.5f), new(.45f, .85f, 1.12f), service);
            CapsuleTarget("E04_E07", "Tủ khô · hồ sơ tiếp nhận", new(-.57f, BasementLevel + 1.25f, 39), new(.45f, .84f, 1.18f), service);
            CapsuleTarget("AN04", "Phòng trực · lịch và băng họp", new(3.88f, WardLevel + 1.16f, 46.78f), new(.7f, .8f, .4f), upper);
            for (int i = 0; i < 3; i++)
                Lamp("B1 · bulkhead", LightType.Point, new(7.4f, BasementLevel + 2.6f, 33.4f + 5f * i),
                    1.45f, 7.3f, new Color(.68f, .77f, .81f), service);
            Lamp("Nhà xác · desk spill", LightType.Point, new(1, BasementLevel + 2.49f, 39),
                1.95f, 6.7f, new Color(.79f, .82f, .72f), service);
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
                float middle = (from + to) * .5f;
                float center = y + low + tall * .5f;
                var part = Box(name, alongX ? new(middle, center, axis) : new(axis, center, middle),
                    alongX ? new(to - from, tall, .18f) : new(.18f, tall, to - from),
                    Mat("PlasterR"), parent, solid: true, tile: 2f, isStatic: true);
                AddFinish(new LayerFinish { renderer = part.GetComponent<Renderer>(), real = Mat("PlasterR"), report = Mat("PlasterB") });
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
