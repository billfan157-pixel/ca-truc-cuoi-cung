using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace CTCC.Editor
{
    public sealed partial class HospitalAuthoring
    {
        public static void UpdateRebirthTwo()
        {
            UpdateRegion("LOC01_Exterior", DressRebirthCourtyard);
            UpdateRegion("LOC02_Reception", DressRebirthReception);
            UpdateRegion("LOC03_Ward101", DressRebirthWard);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(BootstrapSceneFile);
            Debug.Log("CTCC_CH01_WORLD Authored LOC01–LOC03 and STAFF.");
        }

        private static void UpdateRegion(string file, Action<ZoneRegion> dress)
        {
            var stage = EditorSceneManager.OpenScene(Root + "/Scenes/" + file + ".unity");
            dress(Object.FindFirstObjectByType<ZoneRegion>());
            EditorSceneManager.SaveScene(stage);
        }

        private static Transform Named(Transform root, string name) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name)
                ?? throw new InvalidOperationException("CH01 authoring: missing " + name);

        private static SceneTarget Target(ZoneRegion region, string id) =>
            region.GetComponentsInChildren<SceneTarget>(true).Single(t => t.id == id);

        private static void Contact(SceneTarget target, Vector3 point, Vector3 facing = default)
        {
            if (target.handhold == null)
            {
                target.handhold = new GameObject("Point of contact").transform;
                target.handhold.SetParent(target.transform, false);
            }
            target.handhold.position = point;
            if (facing != Vector3.zero) target.handhold.rotation = Quaternion.LookRotation(facing);
        }

        private static Transform WorkingProp(ZoneRegion region, string oldName, string newName, bool pair)
        {
            var holder = Named(region.transform, oldName);
            var existing = holder.Find(newName);
            if (existing != null) return existing;
            region.finishes.RemoveAll(f => f.renderer != null && f.renderer.transform.IsChildOf(holder));
            while (holder.childCount != 0) Object.DestroyImmediate(holder.GetChild(0).gameObject);
            return AddWorkingModel(region, holder, newName, pair);
        }

        private static Transform AddWorkingModel(ZoneRegion region, Transform parent, string asset, bool pair)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Models + asset + ".fbx");
            if (prefab == null) throw new InvalidOperationException("Missing prop asset " + asset);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var surface in instance.GetComponentsInChildren<MeshRenderer>(true))
            {
                surface.shadowCastingMode = ShadowCastingMode.Off;
                Material[] reportMaterials = surface.sharedMaterials.Select(m => ReferenceMaterial(ReportKey(CleanMatName(m.name)))).ToArray();
                Material[] realMaterials = pair
                    ? surface.sharedMaterials.Select(m => ReferenceMaterial(AgedKey(ReportKey(CleanMatName(m.name))))).ToArray()
                    : reportMaterials;
                surface.sharedMaterials = realMaterials;
                if (pair && !realMaterials.SequenceEqual(reportMaterials))
                    region.finishes.Add(new LayerFinish { renderer = surface, reportMaterials = reportMaterials, realMaterials = realMaterials });
            }
            return instance.transform;
        }

        private static Material ReferenceMaterial(string key) =>
            AssetDatabase.LoadAssetAtPath<Material>(Root + "/Generated/Materials/Reference_" + key + ".mat")
            ?? throw new InvalidOperationException("Missing CH01 material " + key);

        private static Material RebirthMaterial(string name, Material original)
        {
            string path = Root + "/Generated/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(original);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void DressRebirthCourtyard(ZoneRegion region)
        {
            LightNamed(region, "Key below entry canopy").intensity = 6.1f;
            var street = LightNamed(region, "Street wash at the gate");
            street.intensity = 3.9f;
            street.range = 14.2f;
            LightNamed(region, "Gatehouse warm porch light").intensity = 2.7f;
            var bounce = region.transform.Find("Canopy bounce on rain-wet columns");
            if (bounce == null)
            {
                bounce = new GameObject("Canopy bounce on rain-wet columns", typeof(Light)).transform;
                bounce.SetParent(region.transform, false);
            }
            bounce.position = new Vector3(5.5f, 1.44f, -10.85f);
            var bounceLight = bounce.GetComponent<Light>();
            bounceLight.type = LightType.Point;
            bounceLight.shadows = LightShadows.None;
            bounceLight.intensity = 1.65f;
            bounceLight.range = 7.5f;
            bounceLight.color = new Color(.65f, .76f, .89f);
            var apron = Named(region.transform, "Courtyard wet concrete apron").GetComponent<Renderer>();
            var wet = RebirthMaterial("CH01_WetApron", ReferenceMaterial("WetAsphalt"));
            wet.SetFloat("_Smoothness", .76f);
            EditorUtility.SetDirty(wet);
            apron.sharedMaterial = wet;
            var canopy = Named(region.transform, "Canopy sodium cone").GetComponent<Renderer>();
            var beam = RebirthMaterial("CH01_CanopyBeam", ReferenceMaterial("BeamWarm"));
            beam.SetColor("_Tint", new Color(.95f, .72f, .42f, .083f));
            beam.SetVector("_ConeTop", new Vector4(4f, 2.66f, -8.65f, 0f));
            beam.SetVector("_ConeBottom", new Vector4(4f, .10f, -9.20f, 0f));
            beam.SetFloat("_Cull", (float)CullMode.Back);
            beam.EnableKeyword("_CTCC_ROUND_BEAM");
            EditorUtility.SetDirty(beam);
            canopy.sharedMaterial = beam;
            var mist = Named(region.transform, "LOC01 exterior courtyard mist").GetComponent<ParticleSystem>();
            mist.transform.position = new Vector3(4f, .37f, -15.6f);
            var mistMain = mist.main;
            mistMain.startSize = new ParticleSystem.MinMaxCurve(2.2f, 3.6f);
            mistMain.startColor = new Color(.62f, .78f, .86f, .21f);
            var mistShape = mist.shape;
            mistShape.scale = new Vector3(13.2f, .32f, 12.6f);
            var rain = Named(region.transform, "Exterior rain").GetComponent<ParticleSystem>();
            var rainMain = rain.main;
            rainMain.startColor = new Color(.63f, .78f, .90f, .46f);
            rainMain.startSize = .0095f;
            var rainLoop = region.roomLoops.Single(loop => loop.source.name == "Loop · Rain");
            rainLoop.source.transform.position = new Vector3(3.2f, 1.7f, -14.1f);
            rainLoop.source.minDistance = 3.0f;
            rainLoop.level = .70f;
            TuneDecal(region, "CourtyardPuddle_1", .92f, 13f);
            TuneDecal(region, "CourtyardPuddle_2", .88f, 13f);
            TuneDecal(region, "ApproachPuddle", .94f, 11f);
        }

        private static void DressRebirthReception(ZoneRegion region)
        {
            var prop = WorkingProp(region, "CTCC_PROP_ReceptionProps", "CTCC_PROP_ReceptionWorking", true);
            region.receptionHandset = Named(prop, "Handset");
            Bounds ledger = Named(prop, "DutyLedger").GetComponent<Renderer>().bounds;
            Contact(Target(region, "AN01"), new Vector3(ledger.max.x - .035f, ledger.max.y + .02f, ledger.center.z));
            var board = region.receptionBoard.GetComponent<Renderer>().bounds;
            Contact(Target(region, "BOARD"), board.center + Vector3.up * .04f);
            region.phoneAnchor.transform.position = region.receptionHandset.GetComponent<Renderer>().bounds.center;
            PutInDrawer(region, region.drawerDesk, "DrawerPencil", new Vector3(-.144f, -.020f, .072f), 71f);
            DrawerPaper(region, region.drawerDesk, "OBS_RECEPTION_DRAWER", "ReceptionRosterSheet");

            var countertop = Named(region.transform, "Wide counter top").GetComponent<Renderer>();
            var glaze = RebirthMaterial("CH01_CounterCeramic", ReferenceMaterial("Ceramic"));
            glaze.SetFloat("_Smoothness", .68f);
            glaze.SetColor("_BaseColor", new Color(.94f, .92f, .87f));
            EditorUtility.SetDirty(glaze);
            countertop.sharedMaterial = glaze;
            var partition = Named(region.transform, "CTCC_ENV_ReceptionPartition");
            var oldGlass = ReferenceMaterial("Glass");
            var glass = RebirthMaterial("CH01_ReceptionGlass", oldGlass);
            glass.SetFloat("_Smoothness", .64f);
            glass.SetColor("_BaseColor", new Color(.26f, .38f, .35f, .105f));
            EditorUtility.SetDirty(glass);
            foreach (var surface in partition.GetComponentsInChildren<Renderer>(true))
            {
                surface.sharedMaterials = ReplaceGlass(surface.sharedMaterials);
                var finish = region.finishes.Find(f => f.renderer == surface);
                if (finish == null) continue;
                if (finish.realMaterials != null) finish.realMaterials = ReplaceGlass(finish.realMaterials);
                if (finish.reportMaterials != null) finish.reportMaterials = ReplaceGlass(finish.reportMaterials);
            }

            var key = LightNamed(region, "Lan face soft key B");
            key.transform.position = new Vector3(3.20f, 1.93f, -3.32f);
            key.intensity = .83f;
            key.color = new Color(.96f, .97f, .92f);
            var rim = LightNamed(region, "Lan warm desk rim backlight B");
            rim.intensity = 1.13f;
            rim.color = new Color(1f, .86f, .65f);
            LightNamed(region, "Lan glass cool rim fill B").intensity = .25f;
            LightNamed(region, "Reception glass silhouette backlight").intensity = 1.56f;
            region.lanVoiceAnchor.transform.localPosition = new Vector3(0, 1.51f, .07f);
            region.lanVoiceAnchor.minDistance = 1.35f;
            region.lanVoiceAnchor.maxDistance = 11f;

            Material[] ReplaceGlass(Material[] materials) => materials.Select(m => m == oldGlass ? glass : m).ToArray();
        }

        private static void DressRebirthWard(ZoneRegion region)
        {
            var clipboard = region.f101WallRenderer.bounds.center;
            var form = Target(region, "F101");
            Contact(form, clipboard + Vector3.right * .075f, Vector3.right);
            form.frontOnly = true;
            var bin = WorkingProp(region, "CTCC_PROP_PedalBin", "CTCC_PROP_PedalBinWorking", true);
            if (region.pedalBin == null)
            {
                var hinge = Named(bin, "BinHinge").GetComponent<Renderer>().bounds.center + Vector3.up * .017f;
                var lid = Named(bin, "BinLid");
                var pedal = Named(bin, "BinPedal");
                Vector3 axis = bin.parent.right;
                Vector3 pedalCenter = pedal.GetComponent<Renderer>().bounds.center;
                var rig = bin.gameObject.AddComponent<Ch01PedalBin>();
                rig.lidAngle = Vector3.Dot(Vector3.Cross(axis, lid.GetComponent<Renderer>().bounds.center - hinge), Vector3.up) > 0 ? 76f : -76f;
                rig.lid = Pivot("Enamel lid hinge", lid, hinge);
                Vector3 pedalBase = Vector3.Lerp(new Vector3(bin.position.x, pedalCenter.y, bin.position.z), pedalCenter, .68f);
                rig.pedalAngle = Vector3.Dot(Vector3.Cross(axis, pedalCenter - pedalBase), Vector3.up) > 0 ? -13f : 13f;
                rig.pedal = Pivot("Foot pedal pivot", pedal, pedalBase);
                region.pedalBin = rig;

                Transform Pivot(string name, Transform part, Vector3 point)
                {
                    var pivot = new GameObject(name).transform;
                    pivot.SetParent(bin.parent, false);
                    pivot.position = point;
                    part.SetParent(pivot, true);
                    return pivot;
                }
            }

            PutInDrawer(region, region.drawerBed3, "DrawerGauze", new Vector3(.121f, -.021f, -.071f), -10f);
            PutInDrawer(region, region.drawerNurse, "DrawerClip", new Vector3(-.124f, -.018f, -.068f), 28f);
            DrawerPaper(region, region.drawerBed3, "OBS101D", "Bed3MedsSheet");
            DrawerPaper(region, region.drawerNurse, "OBS_SHIFT_LOG", "ShiftGuideDrawerSheet");
            var trace = WorkingProp(region, "CTCC_PROP_Bed4BlanketTrace", "CTCC_PROP_Bed4TracesWorking", false);
            TraceTarget("OBS101A_BLANKET", "Chăn gấp ở hốc giường", "Bed4CottonTrace");
            TraceTarget("OBS101A_SLIPPERS", "Đôi dép cao su nhỏ", "Bed4RubberTrace");
            var wheelMarks = Target(region, "OBS101A");
            wheelMarks.title = "Dấu bánh giường";
            wheelMarks.focusPriority = -1;
            var track = Named(region.transform, "FourthBedReportTrace_B");
            track.position = new Vector3(11.28f, .014f, 3.58f);
            wheelMarks.transform.position = track.position + Vector3.up * .10f;
            wheelMarks.GetComponent<BoxCollider>().size = new Vector3(.52f, .16f, .56f);
            Contact(wheelMarks, wheelMarks.transform.position);

            var cloth = region.curtainCloth;
            var occluder = cloth.Find("Curtain sight surface");
            if (occluder == null)
            {
                occluder = new GameObject("Curtain sight surface") { layer = HospitalScene.SightOccluderLayer }.transform;
                occluder.SetParent(cloth, false);
                var surface = occluder.gameObject.AddComponent<BoxCollider>();
                var bounds = cloth.GetComponent<MeshFilter>().sharedMesh.bounds;
                surface.center = bounds.center;
                surface.size = bounds.size;
                surface.isTrigger = true;
            }
            Target(region, "CURTAIN-101").visibilitySurface = occluder.GetComponent<Collider>();

            LightNamed(region, "Room 101 GI bounce B").intensity = .98f;
            LightNamed(region, "Room 101 floor bounce R").intensity = 1.29f;
            if (region.bed4TubeLight != null) region.bed4TubeLight.intensity = 1.31f;
            TuneDecal(region, "WindowWaterRun_1", .83f, 12f);
            TuneDecal(region, "WindowWaterRun_2", .72f, 12f);
            TuneDecal(region, "WindowWaterRun_3", .69f, 12f);
            TuneDecal(region, "WindowFloorDamp_1", .78f, 12f);
            TuneDecal(region, "WindowFloorDamp_2", .74f, 12f);
            TuneDecal(region, "FourthBedReportTrace_B", .90f, 10f);
            TuneDecal(region, "RefutedBed4DampWall", .78f, 10f);
            var drips = region.roomLoops.Single(loop => loop.source.name == "Loop · Drips");
            drips.source.transform.position = new Vector3(9.05f, .30f, 7.70f);
            drips.source.maxDistance = 6.5f;
            drips.level = .69f;
            foreach (var tube in region.roomLoops.Where(loop => loop.source.name == "Loop · Tube" && loop.source.transform.position.x > 5.5f))
            {
                tube.source.transform.position = new Vector3(9.74f, 3.25f, 5.62f);
                tube.source.maxDistance = 6.2f;
                tube.level = .35f;
            }

            void TraceTarget(string id, string title, string partName)
            {
                var model = Named(trace, partName);
                var marker = trace.Find(id);
                if (marker != null) return;
                GameObject item = new GameObject(id) { layer = HospitalScene.InteractLayer };
                item.transform.SetParent(trace, true);
                var bounds = model.GetComponent<Renderer>().bounds;
                item.transform.position = bounds.center + Vector3.up * .075f;
                item.transform.rotation = Quaternion.identity;
                var hit = item.AddComponent<BoxCollider>();
                hit.isTrigger = true;
                hit.size = bounds.size + Vector3.one * .13f;
                var target = item.AddComponent<SceneTarget>();
                target.id = id;
                target.title = title;
                target.focusPriority = 1;
                target.handhold = item.transform;
            }
        }

        private static void PutInDrawer(ZoneRegion region, Transform drawer, string part, Vector3 point, float yaw)
        {
            if (drawer.Find(part + " mount") != null) return;
            var mount = new GameObject(part + " mount").transform;
            mount.SetParent(drawer, false);
            var model = AddWorkingModel(region, mount, "CTCC_PROP_CH01DrawerContents", false);
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
                if (renderer.name != part) Object.DestroyImmediate(renderer.gameObject);
            mount.localPosition = point;
            mount.localRotation = Quaternion.Euler(0, yaw, 0);
        }

        private static void DrawerPaper(ZoneRegion region, Transform drawer, string id, string sheet)
        {
            var target = Target(region, id);
            var bounds = Named(drawer, sheet).GetComponent<Renderer>().bounds;
            target.transform.SetPositionAndRotation(bounds.center + Vector3.up * .032f, Quaternion.identity);
            target.GetComponent<BoxCollider>().size = new Vector3(bounds.size.x, .057f, bounds.size.z);
            Contact(target, target.transform.position);
        }

        private static Light LightNamed(ZoneRegion region, string name) => Named(region.transform, name).GetComponent<Light>();

        private static void TuneDecal(ZoneRegion region, string name, float opacity, float distance)
        {
            var projector = Named(region.transform, name).GetComponent<DecalProjector>();
            projector.fadeFactor = opacity;
            projector.drawDistance = distance;
        }
    }
}
