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
            // Keep the upper landing and centreline. Set back the first tread to make a flat ground landing.
            Flight(upper, "S1 · Lễ tân → Khu B", new(layout.s1Discharge.firstTreadX, 0, -4), new(-11, WardLevel, -4), 2.7f);
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
            var loc08GateRoot = Group("LOC08 · Khóa G05", upper);
            Box("LOC08 G05 door panel", new(3.5f, ArchiveLevel + 1.225f, 32.3f),
                new(.16f, 2.45f, 1.80f), Mat("MetalOld"), loc08GateRoot, solid: true);
            CapsuleTarget("LOC08_GATE", "Cửa Phòng Hành chính",
                new(3.32f, ArchiveLevel + 1.25f, 32.3f), new(.36f, .95f, 1.35f), loc08GateRoot);

            // LOC08 Administrative Threshold Desk & CH05 -> CH06 Handoff (CP03 / AP11, stops before CH06!)
            var loc08Handoff = Group("LOC08 · CH05 Handoff Desk", upper, new(6.85f, ArchiveLevel, 32.3f));
            Box("LOC08 admin desk", new(0f, .44f, 0f), new(.90f, .88f, 1.75f), Mat("Wood"), loc08Handoff, solid: true, isStatic: true);
            Box("LOC08 report binder", new(-.12f, .905f, 0f), new(.36f, .05f, .48f), Mat("KraftPaper"), loc08Handoff, solid: false, isStatic: true);
            var handoffMarker = Group("LOC08 · Handoff Marker", loc08Handoff, new(-.12f, .94f, 0f));
            Box("CP03 brass badge", Vector3.zero, new(.28f, .025f, .36f), Mat("Brass"), handoffMarker, solid: false);
            handoffMarker.gameObject.SetActive(false);
            Lamp("LOC08 admin spill R", LightType.Point, new(6.1f, ArchiveLevel + 2.55f, 32.3f), 3.1f, 7.5f, new Color(.84f, .88f, .78f), upper);
            CapsuleTarget("CH05_HANDOFF", "Hồ sơ Phòng Hành chính",
                new(6.48f, ArchiveLevel + 1.08f, 32.3f), new(.72f, .68f, .92f), upper);
            if (multiScene)
            {
                zones["LOC05"].region.loc08GateBlocker = loc08GateRoot.gameObject;
                zones["LOC05"].region.ch05HandoffMarker = handoffMarker.gameObject;
            }
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

                // Dynamic movable CHAR006 Orderly + Medicine Cart rig in LOC05/LOC06 (not static-batched!)
                var cart = PlaceBlenderModel("CTCC_PROP_MedicineCart", upper,
                    new(5.8f, WardLevel, 36.8f), 90f, pairRB: false);
                foreach (var col in cart.GetComponentsInChildren<Collider>(true))
                    UnityEngine.Object.DestroyImmediate(col);

                var orderlyActor = PlaceCharacterActor("CTCC_CHR_Gatekeeper", upper,
                    new Vector3(5.8f, WardLevel, 36.8f), 90f, "CHAR006_Orderly",
                    isApparition: false, enableBlink: false, enableLipSync: false);
                orderlyActor.gameObject.SetActive(false);

                // Harmless translucent CHAR007 Unrecorded apparition in LOC10 (strictly outside AP09 & separate from C12)
                var unrecordedActor = PlaceCharacterActor("CTCC_CHR_Gatekeeper", service,
                    new Vector3(-0.2f, BasementLevel, 41.15f), 90f, "CHAR007_Unrecorded",
                    isApparition: true, enableBlink: false, enableLipSync: false);
                foreach (var col in unrecordedActor.GetComponentsInChildren<Collider>(true))
                    UnityEngine.Object.DestroyImmediate(col);
                unrecordedActor.gameObject.SetActive(false);

                if (multiScene)
                {
                    zones["LOC05"].region.orderlyActor = orderlyActor;
                    zones["LOC05"].region.orderlyCart = cart.transform;
                    zones["LOC09"].region.unrecordedActor = unrecordedActor;
                }
            }
            else
            {
                var cartProxy = Box("Orderly · medicine cart proxy", new(5.8f, WardLevel + 0.48f, 36.8f),
                    new(.55f, .92f, .86f), Mat("Steel"), upper, solid: false);
                if (multiScene) zones["LOC05"].region.orderlyCart = cartProxy.transform;
            }

            // PZ04 physical schedule board, room-status slip, nursing status board, and observable route arrow indicator
            // Placed on the East wall of LOC06 (clear of GATE-G03 open leaf swinging north along x~3.05) and North wall of LOC05.
            var schedBoard = Group("PZ04 · Schedule & Status Board", upper, new(5.28f, WardLevel + 1.48f, 44.85f));
            Box("Schedule frame", Vector3.zero, new(.05f, .64f, .88f), Mat("Wood"), schedBoard, solid: false, isStatic: true);
            Box("Schedule sheet", new(-.03f, 0f, 0f), new(.01f, .54f, .76f), Mat("Paper"), schedBoard, solid: false, isStatic: true);
            CapsuleTarget("PZ04_SCHEDULE", "Bảng lịch đẩy xe thuốc · 3 điểm dừng (PZ04)",
                new(5.22f, WardLevel + 1.48f, 44.85f), new(.42f, .76f, .92f), upper);

            Box("PZ04 · Room status slip", new(5.28f, WardLevel + 1.24f, 46.05f), new(.04f, .34f, .28f), Mat("Paper"), upper, solid: false, isStatic: true);
            CapsuleTarget("PZ04_ROOM_SLIP", "Phiếu tình trạng buồng Khu B (PZ04)",
                new(5.20f, WardLevel + 1.24f, 46.05f), new(.44f, .52f, .48f), upper);

            Box("PZ04 · Ward status board", new(5.65f, WardLevel + 1.38f, 38.08f), new(.64f, .46f, .06f), Mat("Paper"), upper, solid: false, isStatic: true);
            CapsuleTarget("PZ04_STATUS_BOARD", "Bảng tình trạng buồng Khu B · Đối chiếu tuyến xe (PZ04)",
                new(5.65f, WardLevel + 1.38f, 37.98f), new(.78f, .64f, .46f), upper);

            // Observable route indicator arrows above LOC06 south doorway (visible from both LOC06 & LOC05!)
            var arrowDefault = Group("PZ04 · Route Arrow Default", upper, new(4.0f, WardLevel + 2.52f, 44.08f));
            Box("Route default plate", Vector3.zero, new(.76f, .18f, .06f), Mat("Brass"), arrowDefault, solid: false);
            arrowDefault.gameObject.SetActive(false);

            var arrowRerouted = Group("PZ04 · Route Arrow Rerouted", upper, new(4.0f, WardLevel + 2.52f, 44.08f));
            Box("Route updated shaft", new(-.06f, 0f, 0f), new(.56f, .12f, .07f), Mat("LiveTube"), arrowRerouted, solid: false);
            var tipA = Box("Route updated head A", new(.24f, .06f, 0f), new(.22f, .06f, .07f), Mat("LiveTube"), arrowRerouted, solid: false);
            tipA.transform.localRotation = Quaternion.Euler(0f, 0f, -32f);
            var tipB = Box("Route updated head B", new(.24f, -.06f, 0f), new(.22f, .06f, .07f), Mat("LiveTube"), arrowRerouted, solid: false);
            tipB.transform.localRotation = Quaternion.Euler(0f, 0f, 32f);
            arrowRerouted.gameObject.SetActive(false);

            // Directional handoff slip in LOC06 pointing to the canonical AN05 patient wheelchair in LOC05 Khu B
            var loc06Slip = Group("LOC06 · AN05 Direction Plaque", upper, new(4.84f, WardLevel + 1.04f, 46.72f));
            Box("AN05 note base", Vector3.zero, new(.38f, .05f, .38f), Mat("Brass"), loc06Slip, solid: false);
            CapsuleTarget("AN05_HANDOFF_NOTE", "Ghi chú bàn giao → Xe bệnh nhân AN05 tại Khu B",
                new(4.84f, WardLevel + 1.15f, 46.72f), new(.52f, .58f, .52f), upper);

            // Canonical AN05 Anchor (Core 02/04/07): Patient wheelchair/stretcher parked along south wall of LOC05 Khu B
            // (completely clear of CH03 archive walks, CH04 Orderly route at z=36.6, and AP06 at x=4.0!)
            var an05Root = Group("AN05 · Xe bệnh nhân Khu B", upper, new(-0.65f, WardLevel, 32.85f));
            if (art)
            {
                var chair = PlaceBlenderModel("CTCC_PROP_TransportTrolley", an05Root, Vector3.zero, 0f, pairRB: true,
                    scale: new Vector3(0.76f, 0.88f, 0.62f));
                staticArchitecture.Add(chair);
            }
            else
            {
                Box("AN05 wheelchair proxy", new(0f, .46f, 0f), new(1.02f, .92f, .72f), Mat("Steel"), an05Root, solid: false);
            }
            Box("AN05 wheelchair collider", new(0f, .46f, 0f), new(1.02f, .92f, .72f), Mat("Steel"), an05Root, solid: true)
                .GetComponent<Renderer>().enabled = false;
            var an05Visual = Group("AN05 · Brass Anchor Halo", an05Root, new(0f, .012f, 0f));
            Box("AN05 brass ring", Vector3.zero, new(1.36f, .016f, 1.06f), Mat("Brass"), an05Visual, solid: false);
            Box("AN05 strip", new(.66f, .016f, 0f), new(.10f, .022f, .54f), Mat("LiveTube"), an05Visual, solid: false);
            an05Visual.gameObject.SetActive(false);
            CapsuleTarget("AN05", "Xe bệnh nhân Khu B · Điểm neo ký ức AN05 (CH05)",
                new(-0.18f, WardLevel + 1.05f, 32.85f), new(1.18f, 1.10f, .92f), upper);

            // Phase D (TASK013-B): LOC09 Technical Blueprint Cabinet & PZ07 Drafting Table (North wall x=7.4, z=44.65)
            var pz07Table = Group("LOC09 · Tủ bản vẽ kỹ thuật & Bàn căn PZ07", service, new(7.4f, BasementLevel, 44.65f));
            Box("PZ07 drafting table", new(0f, .46f, 0f), new(2.15f, .92f, .92f), Mat("MetalOld"), pz07Table, solid: true, isStatic: true);
            Box("PZ07 blueprint rack", new(0f, 1.56f, .36f), new(2.05f, 1.24f, .14f), Mat("Wood"), pz07Table, solid: false, isStatic: true);
            if (art)
            {
                Flat("PostedMap2005_Sheet", "Doc_Ch05_PostedMap2005", new(-.48f, .932f, -.04f), Vector3.up, new(.84f, .64f), pz07Table);
                Flat("AsBuilt2004_E05_Sheet", "Doc_Ch05_AsBuilt2004_E05", new(.48f, .932f, -.04f), Vector3.up, new(.84f, .64f), pz07Table);
            }
            else
            {
                Box("PostedMap2005 proxy", new(-.48f, .935f, -.04f), new(.76f, .02f, .56f), Mat("Paper"), pz07Table);
                Box("AsBuilt2004 E05 proxy", new(.48f, .935f, -.04f), new(.76f, .02f, .56f), Mat("EnamelBlue"), pz07Table);
            }
            Lamp("LOC09 blueprint drafting lamp", LightType.Point, new(7.4f, BasementLevel + 2.15f, 43.95f), 2.8f, 5.5f, new Color(.94f, .90f, .74f), service);
            CapsuleTarget("CH05_BLUEPRINTS", "Tủ bản vẽ B1 · Sơ đồ 2005 & Hoàn công 2004 (E05 / PZ07)",
                new(7.4f, BasementLevel + 1.12f, 44.20f), new(1.95f, .78f, .90f), service);

            // Phase D Optional (DEC-V32-03): LOC09 AS-05 Solvent Tank Valve w/ aged cut padlock & wedged air damper (West wall x=5.08, z=35.55)
            var valveRoot = Group("CTCC_PROP_LOC09_ValveCut", service, new(5.08f, BasementLevel, 35.55f));
            if (art)
            {
                staticArchitecture.Add(PlaceBlenderModel("CTCC_PROP_OxygenCylinderCart", valveRoot, new(.06f, 0f, -.20f), 90f, pairRB: false));
                staticArchitecture.Add(PlaceBlenderModel("CTCC_PROP_WallGasManifold", valveRoot, new(-.46f, 1.42f, 0f), 90f, pairRB: false, castShadows: false));
            }
            else
            {
                Box("AS05 solvent tank proxy", new(.06f, .68f, -.20f), new(.46f, 1.36f, .46f), Mat("GreenOxygen"), valveRoot, solid: false);
            }
            Box("Valve tank col", new(.06f, .68f, -.20f), new(.52f, 1.36f, .52f), Mat("MetalOld"), valveRoot, solid: true)
                .GetComponent<Renderer>().enabled = false;
            Box("Cut brass padlock", new(-.22f, 1.24f, .16f), new(.08f, .10f, .05f), Mat("Brass"), valveRoot, solid: false, isStatic: true);
            Box("Air damper duct", new(-.36f, 1.92f, .24f), new(.22f, .44f, .72f), Mat("MetalOld"), valveRoot, solid: false, isStatic: true);
            Box("Rusted damper wedge", new(-.24f, 1.84f, .24f), new(.14f, .06f, .06f), Mat("Brass"), valveRoot, solid: false, isStatic: true);
            CapsuleTarget("LOC09_VALVE", "Van bồn dung môi AS-05 & tấm chắn gió (Quan sát tùy chọn)",
                new(5.48f, BasementLevel + 1.48f, 35.72f), new(.68f, 1.02f, 1.02f), service);

            if (multiScene)
            {
                zones["LOC05"].region.pz04RouteArrowDefault = arrowDefault.gameObject;
                zones["LOC05"].region.pz04RouteArrowRerouted = arrowRerouted.gameObject;
                zones["LOC05"].region.an05AnchorVisual = an05Visual.gameObject;
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

        private void BuildS1CourtyardDischarge(Transform shell, Transform realRoot)
        {
            // Campus Grounds is visual background for navigation. The outlet is part of the reception tile.
            if (multiScene)
            {
                shell = zones["LOC02"].permanent;
                realRoot = zones["LOC02"].real;
            }
            var opening = layout.s1Discharge;
            float half = opening.width / 2f;
            float left = opening.x - half;
            float right = opening.x + half;
            var origin = new Vector3(opening.x, opening.y, opening.z);
            var frame = Group("S1 courtyard discharge", shell, origin);

            Infill("West jamb", -3.05f, left, 0f, 3.6f);
            Infill("East jamb", right, -1.05f, 0f, 3.6f);
            Infill("Lintel", left, right, opening.height, 3.6f);

            // Flush landing, passage and threshold connect the stair toe to the courtyard slab.
            Deck(shell, "S1 ground landing", opening.y, opening.firstTreadX - .10f, -1f, -5.40f, -2.60f);
            Deck(shell, "S1 discharge passage", opening.y, -2.95f, -1f, opening.z + .22f, -5.40f);
            Deck(shell, "S1 flush courtyard threshold", opening.y, left - .08f, right + .08f, -8f, opening.z + .22f);
            Surface("S1 passage west wall", new Vector3(-2.98f, 1.80f, -6.62f), new Vector3(.16f, 3.60f, 2.48f),
                "PlasterR", "PlasterB", solid: true, tile: 1.8f, isStatic: true, parent: shell);

            var metal = Mat("MetalOld");
            foreach (float x in new[] { -half + .035f, half - .035f })
                Box("S1 steel frame upright", new Vector3(x, opening.height / 2f, 0f),
                    new Vector3(.07f, opening.height, .16f), metal, frame, solid: true);
            Box("S1 steel frame head", new Vector3(0f, opening.height - .035f, 0f),
                new Vector3(opening.width, .07f, .16f), metal, frame, solid: true);
            var hinge = Group("S1 discharge hinge", frame, new Vector3(-half + .075f, 0f, -.12f));
            float leafWidth = opening.width - .15f;
            Box("S1 discharge steel leaf", new Vector3(leafWidth / 2f, (opening.height - .07f) / 2f, 0f),
                new Vector3(leafWidth, opening.height - .11f, .067f), metal, hinge, solid: true);
            Box("S1 push bar", new Vector3(leafWidth / 2f, 1.04f, .072f), new Vector3(leafWidth - .19f, .045f, .048f), metal, hinge);

            var salvage = Group("S1 discharge salvage and mesh", realRoot, origin + Vector3.back * .32f);
            Box("S1 salvage rack base", new Vector3(0f, .31f, -.05f), new Vector3(opening.width - .09f, .62f, .37f), Mat("WoodDoorR"), salvage, solid: true);
            Box("S1 mesh barrier bottom rail", new Vector3(0f, .62f, .02f), new Vector3(opening.width - .06f, .07f, .08f), metal, salvage, solid: true);
            Box("S1 mesh barrier top rail", new Vector3(0f, 1.78f, .02f), new Vector3(opening.width - .06f, .07f, .08f), metal, salvage);
            for (int i = -6; i <= 6; i++)
                Box("S1 mesh vertical wire", new Vector3(i * .095f, 1.20f, .02f), new Vector3(.023f, 1.16f, .034f), metal, salvage);
            for (int i = 0; i < 8; i++)
                Box("S1 mesh horizontal wire", new Vector3(0f, .70f + i * .145f, .02f), new Vector3(opening.width - .10f, .024f, .036f), metal, salvage);
            Box("S1 mesh collision", new Vector3(0f, 1.20f, .02f), new Vector3(opening.width - .08f, 1.16f, .068f),
                metal, salvage, solid: true).GetComponent<MeshRenderer>().enabled = false;

            var door = new DoorPose { pivot = hinge, openAngle = 93f };
            // Phase C (CH05 Layer M): S1 courtyard discharge assembly point & tally board (6/6), active strictly in CH05 M!
            Transform loc02Mem = multiScene ? zones["LOC02"].memory : memory;
            var s1Assembly = Group("CH05 · S1 Courtyard Assembly (Layer M)", loc02Mem, origin + Vector3.back * 0.45f);
            Box("Evac tally board", new(.95f, 1.38f, -.55f), new(.08f, .76f, .58f), Mat("Wood"), s1Assembly, solid: false);
            Box("Evac tally sheet", new(.90f, 1.38f, -.55f), new(.02f, .62f, .44f), Mat("Paper"), s1Assembly, solid: false);
            Lamp("CH05 S1 assembly warm flood M", LightType.Point, new(0f, 2.55f, -.65f), 4.2f, 11f, new Color(1.0f, 0.74f, 0.40f), s1Assembly);
            CapsuleTarget("CH05_M_PAIR3_ASSEMBLY", "Điểm tập kết sân ngoài S1 · Đưa Nhóm 3 & chốt danh sách 6/6",
                new Vector3(0f, 1.15f, -0.65f), new(1.45f, 1.45f, 1.15f), s1Assembly);
            if (multiScene)
            {
                zones["LOC02"].region.s1DischargeDoor = door;
                zones["LOC02"].region.s1DischargeSalvage = salvage.gameObject;
                zones["LOC02"].region.ch05MemoryStage = s1Assembly.gameObject;
            }
            else
            {
                world.s1DischargeDoor = door;
                world.s1DischargeSalvage = salvage.gameObject;
            }

            void Infill(string label, float from, float to, float bottom, float top) =>
                Surface("K-A S1 discharge " + label, new Vector3((from + to) / 2f, opening.y + (bottom + top) / 2f, opening.z),
                    new Vector3(to - from, top - bottom, .28f), "ExteriorFacade", "ExteriorFacadeB", solid: true,
                    tile: 2.2f, isStatic: true, parent: shell);
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
