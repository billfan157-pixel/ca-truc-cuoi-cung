using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CTCC.Editor
{
    public sealed partial class HospitalAuthoring
    {
        private void DressChapterOneSurfaces()
        {
            Transform court = zones["LOC01"].permanent;
            Transform courtR = zones["LOC01"].real;
            Transform wardR = zones["LOC03"].real;
            Transform deskR = zones["LOC02"].real;

            // Note: CTCC_LOC01_EntryCanopy now integrates a Cycles-baked galvanized gutter, collector hopper, downspout & splash block!
            staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_CH01ReplacementShutter", court,
                new Vector3(11.50f, 1.99f, -8.014f), 0, castShadows: false));
            staticArchitecture.Add(PlaceBlenderModel("CTCC_ENV_CH01ReplacementShutter", court,
                new Vector3(0.39f, 6.08f, -8.014f), 0, scale: new Vector3(0.94f, 0.96f, 1.10f), castShadows: false));

            Decal("Runoff below canopy pipe", "CH01_LimeRunoff", new Vector3(6.12f, 1.42f, -10.663f),
                Vector3.back, .30f, 2.52f, .12f, courtR, .83f);
            Decal("Uneven lime repair below the replacement sash", "CH01_LimeRunoff", new Vector3(11.4f, .59f, -8.032f),
                Vector3.back, 1.18f, 1.13f, .20f, courtR, .59f, -4f);
            Decal("Capillary rise along gate column", "CH01_Capillary", new Vector3(5.43f, .38f, -20.23f),
                Vector3.back, .87f, .75f, .13f, courtR);
            Decal("Gate plaque edge chips", "CH01_EnamelChips", new Vector3(2.57f, 1.56f, -20.245f),
                Vector3.back, .40f, .17f, .055f, courtR, .84f);
            Decal("Canopy sign edge chips", "CH01_EnamelChips", new Vector3(4f, 3.44f, -11.052f),
                Vector3.back, 2.50f, .836f, .067f, courtR, .63f);

            Decal("Damp behind reception bench", "CH01_Capillary", new Vector3(9.002f, .38f, -5.06f),
                Vector3.left, 2.45f, .78f, .13f, deskR, .79f);
            Decal("Sill varnish worn at the document slot", "CH01_VarnishWear", new Vector3(2.072f, 1.19f, -3.24f),
                Vector3.right, .67f, .12f, .065f, deskR, .89f);
            Decal("Counter plaque lacquer worn at corners", "CH01_EnamelChips", new Vector3(2.094f, .67f, -3.35f),
                Vector3.right, .89f, .338f, .052f, deskR, .76f);
            Decal("Entry grit fan in R", "CH01_WheelWear", new Vector3(4.11f, .013f, -6.88f),
                Vector3.up, 1.22f, 1.98f, .09f, deskR, .64f, -9f);

            // An old chip under the sill and a worn section of the route take different shapes.
            Decal("Broken capillary line from Ward 101 basin", "CH01_Capillary", new Vector3(13.64f, .44f, 6.57f),
                Vector3.left, 1.84f, .80f, .18f, wardR, .9f);
            Decal("Ward lever hand wear", "CH01_VarnishWear", new Vector3(5.486f, 1.02f, 4.085f),
                Vector3.left, .23f, .45f, .10f, wardR, .65f);
            Decal("Old wheel wear at the corridor turn", "CH01_WheelWear", new Vector3(4.14f, .013f, 6.00f),
                Vector3.up, 1.52f, 4.8f, .10f, wardR, .76f, 7f);
            Decal("B floor kept clear along the working route", "CH01_WheelWear", new Vector3(4.08f, .013f, 5.86f),
                Vector3.up, 1.33f, 4.1f, .1f, zones["LOC03"].report, .26f, 7f);
            Decal("Room 101 plaque corner wear", "CH01_EnamelChips", new Vector3(5.473f, 3.08f, 4.75f),
                Vector3.left, .653f, .245f, .049f, wardR, .75f);
        }

        private void ConfigureReceptionGlazing(GameObject partition)
        {
            var finishes = multiScene ? zones["LOC02"].region.finishes : world.finishes;
            var oldGlass = ReferenceMaterial("Glass");
            var glazing = Shader.Find("CTCC/ReceptionGlazing");
            if (glazing == null) throw new InvalidOperationException("Missing reception glass shader.");
            var clearGlass = Glaze("CH01_ReceptionGlass", new Color(.24f, .36f, .32f, .042f), .14f, .22f, .72f, .22f);
            var agedGlass = Glaze("CH01_ReceptionGlassR", new Color(.20f, .27f, .26f, .092f), .56f, .18f, .48f, .62f);
            foreach (var surface in partition.GetComponentsInChildren<Renderer>(true))
            {
                Material[] original = surface.sharedMaterials;
                if (!original.Any(IsGlazing)) continue;
                var finish = finishes.Find(f => f.renderer == surface);
                if (finish == null)
                {
                    finish = new LayerFinish { renderer = surface };
                    AddFinish(finish);
                }
                finish.realMaterials = ReplaceGlass(finish.realMaterials ?? original, agedGlass);
                finish.reportMaterials = ReplaceGlass(finish.reportMaterials ?? original, clearGlass);
                surface.sharedMaterials = finish.realMaterials;
            }

            Material Glaze(string name, Color tint, float residue, float highlight, float refl, float dustRough)
            {
                var material = RebirthMaterial(name, oldGlass);
                material.shader = glazing;
                material.SetColor("_Tint", tint);
                material.SetFloat("_EdgeSoil", residue);
                material.SetFloat("_BroadGlint", highlight);
                material.SetFloat("_ReflectionStrength", refl);
                material.SetFloat("_DustRoughness", dustRough);
                material.renderQueue = (int)RenderQueue.Transparent + 2;
                EditorUtility.SetDirty(material);
                return material;
            }

            bool IsGlazing(Material material) => material == oldGlass || material == clearGlass || material == agedGlass;
            Material[] ReplaceGlass(Material[] materials, Material target) =>
                materials.Select(m => IsGlazing(m) ? target : m).ToArray();

            BrushReceptionMetal(partition, finishes);
        }

        private static void BrushReceptionMetal(GameObject partition, System.Collections.Generic.List<LayerFinish> finishes)
        {
            var steel = ReferenceMaterial("Steel");
            var brushed = RebirthMaterial("CH01_BrushedReceptionSteel", steel);
            brushed.SetFloat("_Smoothness", .48f);
            brushed.SetColor("_BaseColor", new Color(.85f, .87f, .84f));
            EditorUtility.SetDirty(brushed);
            foreach (var surface in partition.GetComponentsInChildren<Renderer>(true))
            {
                Material[] original = surface.sharedMaterials;
                if (!original.Contains(steel)) continue;
                surface.sharedMaterials = ReplaceSteel(original);
                var finish = finishes.Find(f => f.renderer == surface);
                if (finish?.realMaterials != null) finish.realMaterials = ReplaceSteel(finish.realMaterials);
                if (finish?.reportMaterials != null) finish.reportMaterials = ReplaceSteel(finish.reportMaterials);
            }

            Material[] ReplaceSteel(Material[] source) => source.Select(m => m == steel ? brushed : m).ToArray();
        }

        private void DressLanWorkstation(Transform common, Transform deskMark)
        {
            // Native-height ergonomic counter stool-chair (seat 0.60m, zero Y-stretch!) + knees fit inside recessed kneehole!
            var chair = PlaceBlenderModel("CTCC_PROP_CH01DutyChair", common,
                deskMark.position - deskMark.forward * .07f, -88, castShadows: true);
            staticArchitecture.Add(chair);
            var shelf = PlaceBlenderModel("CTCC_PROP_CH01FileRack", common,
                new Vector3(-.985f, 1.38f, -1.92f), -90, castShadows: true);
            staticArchitecture.Add(shelf);
            // Open active duty book belongs to 2005 operational B shift (cleared in 2012 abandoned R!)
            var bookParent = multiScene ? zones["LOC02"].report : report;
            var book = PlaceBlenderModel("CTCC_PROP_CH01DutyBook", bookParent,
                deskMark.position + deskMark.forward * .435f + Vector3.up * 1.129f, 84, pairRB: false, castShadows: false);
            staticArchitecture.Add(book);
            Decal("Shaded tile beneath duty chair", "BedContact", new Vector3(.17f, .013f, -3.30f),
                Vector3.up, .70f, .78f, .10f, common, .62f);
        }

        private Material CreateCh01Asphalt()
        {
            Material wet = Lit("CH01_AsphaltRain", .84f, .89f, .92f, maps: "WetAsphalt",
                tint: true, bump: .48f, ao: .78f);
            wet.SetTexture("_MetallicGlossMap", Tex("CH01_AsphaltHumidity_Mask"));
            wet.SetTexture("_OcclusionMap", Tex("CH01_AsphaltHumidity_Mask"));
            wet.SetFloat("_Smoothness", .66f);
            return wet;
        }

        private void AddCh01Puddles()
        {
            var shader = Shader.Find("CTCC/RainRipples");
            if (shader == null) throw new System.InvalidOperationException("Missing CTCC/RainRipples.");
            var ripple = MaterialAsset("CH01_RainRipples", shader);
            ripple.SetColor("_Glint", new Color(.52f, .64f, .72f, .62f));
            EditorUtility.SetDirty(ripple);

            // Reflective organic monsoon puddles positioned along drainage lows & storm grates (leaving dry portico z > -10.95 dry!)
            var pockets = new (Vector3 position, Vector2 size, float yaw)[]
            {
                (new Vector3(5.05f, .016f, -21.55f), new Vector2(1.95f, 1.15f), 26),
                (new Vector3(2.90f, .016f, -23.10f), new Vector2(2.30f, 1.20f), -18),
                (new Vector3(2.38f, .017f, -16.95f), new Vector2(2.35f, 1.25f), 24),
                (new Vector3(5.58f, .017f, -13.75f), new Vector2(2.15f, 1.05f), -21),
                (new Vector3(6.48f, .017f, -10.88f), new Vector2(1.45f, .92f), -12),
                (new Vector3(11.15f, -.029f, 9.92f), new Vector2(1.85f, .92f), 14),
                (new Vector3(7.15f, -.029f, 10.21f), new Vector2(1.25f, .68f), -21)
            };
            foreach (var pocket in pockets)
            {
                // The inner court is owned by LOC03, regardless of the roof height above it.
                var parent = zones[pocket.position.z < 0 ? "LOC01" : "LOC03"].real;
                var surface = Flat("Rain rings on shallow water", "CH01_RainRipples", pocket.position,
                    Vector3.up, pocket.size, parent);
                surface.transform.rotation = Quaternion.Euler(0, pocket.yaw, 0) * surface.transform.rotation;
            }
        }
    }
}
