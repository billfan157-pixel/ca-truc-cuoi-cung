using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CTCC.Editor
{
    public sealed partial class HospitalAuthoring
    {
        private enum LanDeskAction { Write, Listen, IndicateBoard }

        // Derive three authored clips from the approved rig. The FBX and its standing library are reusable unchanged.
        private void AddLanReceptionClips(CharacterActor lan)
        {
            var rig = lan.transform.GetChild(0);
            var pen = Group("Lan working pen", rig, new Vector3(.15f, 1.16f, .54f));
            PenPart("Lacquered barrel", 0f, .108f, .0037f, Mat("Black"));
            PenPart("Brushed nib", -.057f, .014f, .0022f, Mat("Steel"));
            lan.receptionPen = pen.gameObject;
            lan.receptionPen.SetActive(false);

            BakeLanReceptionClips(lan);

            void PenPart(string label, float height, float length, float radius, Material material)
            {
                var part = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                part.name = label;
                part.transform.SetParent(pen, false);
                part.transform.localPosition = new Vector3(0, height, 0);
                part.transform.localScale = new Vector3(radius * 2, length * .5f, radius * 2);
                Object.DestroyImmediate(part.GetComponent<Collider>());
                var renderer = part.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private static void BakeLanReceptionClips(CharacterActor lan)
        {
            var imported = lan.clips.Where(c => c != null && !c.name.StartsWith("Lan_Reception", StringComparison.Ordinal)).ToArray();
            var idle = imported.Single(c => c.name.EndsWith("Lan_IdleDesk", StringComparison.Ordinal));
            var listen = imported.Single(c => c.name.EndsWith("Lan_Listen", StringComparison.Ordinal));
            var sample = Object.Instantiate(lan.transform.GetChild(0).gameObject);
            sample.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            try
            {
                var clips = new[]
                {
                    BakeLanDeskClip(sample, idle, "Lan_ReceptionWrite", LanDeskAction.Write, 3.6f),
                    BakeLanDeskClip(sample, listen, "Lan_ReceptionListen", LanDeskAction.Listen, 4f),
                    BakeLanDeskClip(sample, listen, "Lan_ReceptionBoard", LanDeskAction.IndicateBoard, 2.35f)
                };
                lan.clips = imported.Concat(clips).ToArray();
            }
            finally
            {
                Object.DestroyImmediate(sample);
            }
        }

        private static AnimationClip BakeLanDeskClip(GameObject rig, AnimationClip source, string name, LanDeskAction action, float duration)
        {
            var bones = rig.GetComponentsInChildren<Transform>(true)
                .Where(b => b.name.StartsWith("mixamorig:", StringComparison.Ordinal) || b.name == "ClipboardSocket_R")
                .ToDictionary(b => b.name);
            Transform Bone(string key) => bones["mixamorig:" + key];
            var hips = Bone("Hips");
            var head = Bone("Head");
            var spine = Bone("Spine");

            var rotations = new Dictionary<Transform, List<Keyframe>[]>();
            foreach (var bone in bones.Values)
                rotations.Add(bone, new[] { new List<Keyframe>(), new List<Keyframe>(), new List<Keyframe>(), new List<Keyframe>() });
            var pen = rig.transform.Find("Lan working pen");
            var positions = new Dictionary<Transform, List<Keyframe>[]>();
            foreach (var item in new[] { hips, pen })
                positions.Add(item, new[] { new List<Keyframe>(), new List<Keyframe>(), new List<Keyframe>() });
            rotations.Add(pen, new[] { new List<Keyframe>(), new List<Keyframe>(), new List<Keyframe>(), new List<Keyframe>() });
            const int samples = 36;
            for (int frame = 0; frame <= samples; frame++)
            {
                float phase = frame == samples ? 0f : frame / (float)samples;
                source.SampleAnimation(rig, phase * source.length);
                float breath = Mathf.Sin(phase * 2 * Mathf.PI);
                float seconds = duration * frame / samples;
                hips.position = new Vector3(-.006f + breath * .004f, .758f + breath * .003f, -.012f);
                foreach (string side in new[] { "Left", "Right" })
                {
                    float sign = side == "Left" ? -1f : 1f;
                    Transform thigh = Bone(side + "UpLeg");
                    Transform knee = Bone(side + "Leg");
                    Transform foot = Bone(side + "Foot");
                    var toe = Bone(side + "ToeBase");
                    AimAlong(thigh, knee, new Vector3(.12f * sign, -.11f, 1));
                    AimAlong(knee, foot, new Vector3(.035f * sign, -1f, -.18f));
                    AimAlong(foot, toe, new Vector3(.09f * sign, -.07f, 1));
                }

                float stroke = Mathf.Sin(phase * 2 * Mathf.PI * 4f);
                float across = Mathf.Sin(phase * 2 * Mathf.PI);
                float reach = action == LanDeskAction.IndicateBoard && frame != samples
                    ? Mathf.Pow(Mathf.Sin(Mathf.PI * phase), 3) : 0f;
                bool writing = action == LanDeskAction.Write;
                spine.rotation = Quaternion.AngleAxis(writing ? 5.5f : 1.8f, Vector3.right) * spine.rotation;
                head.rotation = Quaternion.AngleAxis(writing ? 12f + breath * .8f : 1.3f + breath * 1.4f, Vector3.right)
                    * Quaternion.AngleAxis(writing ? -3.5f : -1.7f, Vector3.forward) * head.rotation;
                SetDeskArm("Left", -.17f, new Vector3(-.104f, 1.149f + breath * .003f, .334f));
                SetDeskArm("Right", .17f, writing
                    ? new Vector3(.105f + across * .025f, 1.149f + Mathf.Abs(stroke) * .007f, .329f + stroke * .014f)
                    : new Vector3(.138f + reach * .043f, 1.150f + .078f * reach, .309f + .100f * reach));

                Vector3 penAxis = writing ? new Vector3(-.34f, .79f, -.50f).normalized
                    : new Vector3(-.95f, .03f, -.32f).normalized;
                Vector3 point = writing
                    ? new Vector3(.09f + across * .025f, 1.150f + Mathf.Abs(stroke) * .002f, .406f + stroke * .014f)
                    : new Vector3(.16f, 1.158f, .444f);
                pen.SetPositionAndRotation(point + penAxis * .063f, Quaternion.FromToRotation(Vector3.up, penAxis));

                foreach (var entry in positions)
                {
                    Vector3 translation = entry.Key.localPosition;
                    for (int axis = 0; axis < 3; axis++)
                        entry.Value[axis].Add(new Keyframe(seconds, translation[axis]));
                }
                foreach (var entry in rotations)
                {
                    Quaternion rotation = entry.Key.localRotation;
                    entry.Value[0].Add(new Keyframe(seconds, rotation.x));
                    entry.Value[1].Add(new Keyframe(seconds, rotation.y));
                    entry.Value[2].Add(new Keyframe(seconds, rotation.z));
                    entry.Value[3].Add(new Keyframe(seconds, rotation.w));
                }
            }

            var clip = new AnimationClip { name = name, frameRate = 30 };
            foreach (var entry in positions)
            {
                string path = AnimationUtility.CalculateTransformPath(entry.Key, rig.transform);
                for (int axis = 0; axis < 3; axis++)
                    AddCurve(path, "m_LocalPosition." + "xyz"[axis], entry.Value[axis]);
            }
            foreach (var entry in rotations)
            {
                string path = AnimationUtility.CalculateTransformPath(entry.Key, rig.transform);
                for (int axis = 0; axis < 4; axis++)
                    AddCurve(path, "m_LocalRotation." + "xyzw"[axis], entry.Value[axis]);
            }
            clip.EnsureQuaternionContinuity();
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = action != LanDeskAction.IndicateBoard;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            string asset = Root + "/Generated/" + name + ".anim";
            var stored = AssetDatabase.LoadAssetAtPath<AnimationClip>(asset);
            if (stored == null)
            {
                AssetDatabase.CreateAsset(clip, asset);
                return clip;
            }
            EditorUtility.CopySerialized(clip, stored);
            EditorUtility.SetDirty(stored);
            Object.DestroyImmediate(clip);
            return stored;

            void SetDeskArm(string side, float outward, Vector3 end)
            {
                Transform arm = Bone(side + "Arm");
                Transform elbow = Bone(side + "ForeArm");
                Transform hand = Bone(side + "Hand");
                float upper = Vector3.Distance(arm.position, elbow.position);
                float lower = Vector3.Distance(elbow.position, hand.position);
                Vector3 direction = (end - arm.position).normalized;
                float reach = Mathf.Clamp(Vector3.Distance(arm.position, end), Mathf.Abs(upper - lower) + .003f, upper + lower - .003f);
                float projection = (upper * upper + reach * reach - lower * lower) / (2f * reach);
                Vector3 bend = Vector3.ProjectOnPlane(new Vector3(outward, -.7f, -.02f), direction).normalized;
                Vector3 joint = arm.position + direction * projection + bend * Mathf.Sqrt(Mathf.Max(0f, upper * upper - projection * projection));
                AimAlong(arm, elbow, joint - arm.position);
                AimAlong(elbow, hand, end - elbow.position);
                if (bones.TryGetValue("mixamorig:" + side + "HandMiddle1", out var palm))
                    AimAlong(hand, palm, new Vector3(outward * -.22f, -.13f, 1f));
            }

            void AddCurve(string path, string attribute, List<Keyframe> keys)
            {
                bool constant = keys.All(k => Mathf.Abs(k.value - keys[0].value) < .00002f);
                var curve = constant ? AnimationCurve.Constant(0f, duration, keys[0].value)
                    : new AnimationCurve(keys.ToArray());
                for (int k = 0; k < curve.length; k++)
                    curve.SmoothTangents(k, 0f);
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), attribute), curve);
            }
        }

        private static void AimAlong(Transform joint, Transform child, Vector3 direction)
        {
            Vector3 from = child.position - joint.position;
            if (from.sqrMagnitude > .00001f)
                joint.rotation = Quaternion.FromToRotation(from, direction) * joint.rotation;
        }
    }
}
