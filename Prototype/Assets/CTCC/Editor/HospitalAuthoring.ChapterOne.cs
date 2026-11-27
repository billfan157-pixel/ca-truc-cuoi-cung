using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CTCC.Editor
{
    public sealed partial class HospitalAuthoring
    {
        // Updates these two local objects in the baked scenes without rebaking GI.
        public static void UpdateChapterOneProps()
        {
            string zen = Root + "/Scenes/";
            var outside = EditorSceneManager.OpenScene(zen + "LOC01_Exterior.unity");
            var mailbox = UnityEngine.Object.FindObjectsByType<SceneTarget>(FindObjectsSortMode.None).FirstOrDefault(t => t.id == "MAILBOX");
            if (mailbox != null)
            {
                mailbox.transform.position = new Vector3(5.36f, 1.18f, -20.34f);
                mailbox.GetComponent<BoxCollider>().size = new Vector3(.36f, .44f, .24f);
            }
            EditorSceneManager.SaveScene(outside);

            var ward = EditorSceneManager.OpenScene(zen + "LOC03_Ward101.unity");
            var region = UnityEngine.Object.FindFirstObjectByType<ZoneRegion>();
            if (region.staffChair == null)
            {
                var root = region.transform.Find("Physical continuity");
                if (root == null) throw new InvalidOperationException("Missing ward scene root.");
                region.staffChair = MakeStaffChair(root);
                EditorSceneManager.SaveScene(ward);
            }
            Debug.Log("CTCC_CH01_PROPS Updated envelope and STAFF chair.");
        }

        private static Transform MakeStaffChair(Transform parent)
        {
            var chair = new GameObject("Ghế gỗ bàn điều dưỡng").transform;
            chair.SetParent(parent, false);
            chair.localPosition = new Vector3(.87f, 0f, 2.83f);
            chair.localRotation = Quaternion.Euler(0, 257f, 0);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "CTCC_PROP_ArchiveChair.fbx");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, chair);
            instance.transform.localScale = Vector3.one * .86f;
            var wood = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Generated/Materials/Reference_Wood.mat");
            if (wood == null) throw new InvalidOperationException("Missing chair material.");
            foreach (var mesh in instance.GetComponentsInChildren<MeshRenderer>())
            {
                var surfaces = mesh.sharedMaterials;
                for (int i = 0; i < surfaces.Length; i++) surfaces[i] = wood;
                mesh.sharedMaterials = surfaces;
            }
            var seat = new GameObject("Chair clearance") { layer = HospitalScene.SolidLayer };
            seat.transform.SetParent(chair, false);
            var body = seat.AddComponent<BoxCollider>();
            body.center = new Vector3(0f, .37f, 0f);
            body.size = new Vector3(.38f, .72f, .38f);
            return chair;
        }
    }
}
