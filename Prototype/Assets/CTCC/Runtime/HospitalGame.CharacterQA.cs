using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CTCC
{
    public sealed partial class HospitalGame
    {
        private static readonly string[] PerformanceShapes =
        {
            "Blink_L", "Blink_R", "Viseme_PP", "Viseme_FF", "Viseme_TH", "Viseme_CH",
            "Viseme_AA", "Viseme_EE", "Viseme_IH", "Viseme_OH", "Viseme_OO", "Viseme_UH",
            "Viseme_MM", "Jaw_Open", "Brow_Concern", "Mouth_Warm", "Mouth_Sorrow",
            "Brow_InnerUp", "Brow_OuterUp_L", "Brow_OuterUp_R", "Mouth_Smile_L", "Mouth_Smile_R"
        };

        private void VerifyDigitalHumanContracts()
        {
            string fault = Argument("-ctcc-qa-probe");
            CheckCharacter(scene.LanActor, "Lan", new[] { "Uniform", "Teeth", "Tongue", "HairClip" },
                new[] { "IdleDesk", "StepOut", "Listen", "TalkGesture", "Handover" }, "ClipboardSocket_R",
                fault == "char-mesh");
            CheckCharacter(scene.AnActor, "An", new[] { "Clothes", "Teeth", "Tongue" },
                new[] { "IdleLook", "TurnFade" });
            CheckCharacter(scene.GatekeeperActor, "GK", new[] { "Coat", "KeyRing" },
                new[] { "IdleStill", "StalkWalk", "InspectReach" }, "KeyRing");

            var lines = new[] { "CH01-D03", "CH01-D05", "CH01-D06", "CH01-D07", "CH01-D08",
                "CH01-D09", "CH01-D16", "CH02-D03", "CH02-D05" };
            foreach (string line in lines)
            {
                var track = CharacterActor.LoadLipSyncTrack(line);
                if (fault == "char-track" && line == lines[0] && track != null)
                    track.frames[1].t = -1f;
                Require(CharacterActor.ValidLipSyncTrack(track, line) && voice.TrackMatchesAudio(line, track) &&
                    track.text == Line(line), $"Invalid character track or stale audio/text: {line}");
            }
            verification.Add("CHAR-Q01/Q07/Q08 PASS: named anatomy and face curves on all built LODs, required clips/sockets, skin bindings, shaders and 9 audio tracks.");
        }

        private static void CheckCharacter(CharacterActor actor, string prefix, string[] additionalParts,
            string[] requiredClips, string socket = null, bool removeEye = false)
        {
            Require(actor != null, $"{prefix}: missing actor");
            var bones = actor.GetComponentsInChildren<Transform>(true).Select(t => t.name).ToHashSet();
            foreach (string part in new[] { "Hips", "Head", "Neck", "Jaw", "LeftEye", "RightEye",
                         "LeftHand", "RightHand", "LeftFoot", "RightFoot" })
                Require(bones.Contains("mixamorig:" + part), $"{prefix}: missing bone {part}");
            Require(socket == null || bones.Contains(socket), $"{prefix}: missing socket {socket}");
            Require(prefix != "Lan" || actor.heldClipboard != null, "Lan: missing bound F101 clipboard");
            var actions = actor.clips.Where(clip => clip != null)
                .Select(clip => clip.name.Substring(clip.name.LastIndexOf('|') + 1)).ToHashSet();
            Require(actions.SetEquals(requiredClips.Select(action => $"{prefix}_{action}")), $"{prefix}: changed clip contract");

            var group = actor.GetComponentInChildren<LODGroup>(true);
            Require(group != null && group.lodCount == 3 && group.size > 0, $"{prefix}: expected three LODs with finite bounds");
            var levels = group.GetLODs();
            if (removeEye)
            {
                levels[1].renderers = levels[1].renderers.Where(r => !r.name.Contains("_Eye_L_")).ToArray();
                group.SetLODs(levels);
            }
            var parts = new[] { "Body", "Hair", "Eye_L", "Eye_R" }.Concat(additionalParts);
            for (int lod = 0; lod < levels.Length; lod++)
            {
                var renderers = levels[lod].renderers;
                foreach (string part in parts)
                {
                    string name = $"{prefix}_{part}_LOD{lod}";
                    Require(renderers.SingleOrDefault(r => r != null && r.name == name) is SkinnedMeshRenderer,
                        $"{prefix}: missing skinned component {name}");
                }
                foreach (Renderer renderer in renderers)
                {
                    var skin = renderer as SkinnedMeshRenderer;
                    Require(skin != null && skin.sharedMesh != null && skin.rootBone != null &&
                        skin.bones.Length == skin.sharedMesh.bindposeCount && skin.bones.All(bone => bone != null) &&
                        skin.localBounds.extents.sqrMagnitude > 0f && !skin.updateWhenOffscreen,
                        $"{prefix}: broken renderer, bind pose or bounds at LOD{lod}");
                    var materials = skin.sharedMaterials;
                    Require(materials.Length == skin.sharedMesh.subMeshCount &&
                        materials.All(m => m != null && m.shader.isSupported &&
                            (!actor.isApparition || m.shader.name == "CTCC/ApparitionHuman")),
                        $"{prefix}: invalid material slots at {skin.name}");
                    if (skin.name == $"{prefix}_Body_LOD{lod}")
                        foreach (string shape in PerformanceShapes)
                            Require(skin.sharedMesh.GetBlendShapeIndex(shape) >= 0, $"{skin.name}: missing {shape}");
                }
            }
        }

        private IEnumerator VerifyForcedFaceLods(CharacterActor lan, Camera camera, RenderTexture target)
        {
            camera.enabled = true;
            var group = lan.GetComponentInChildren<LODGroup>();
            var levels = group.GetLODs();
            lan.PlayAction("Lan_TalkGesture", restart: true);
            lan.ForceSpeechTime("CH01-D03", 0.10f);
            // Includes steady speaking, scene-camera culling, activation and return to neutral.
            for (int lod = 0; lod < levels.Length; lod++)
            {
                group.ForceLOD(lod);
                for (int i = 0; i < 16; i++) yield return null;
                var face = (SkinnedMeshRenderer)levels[lod].renderers.Single(r => r.name == $"Lan_Body_LOD{lod}");
                int aa = face.sharedMesh.GetBlendShapeIndex("Viseme_AA");
                Require(face.isVisible && face.GetBlendShapeWeight(aa) > 5f,
                    $"Lan: LOD{lod} did not synchronize speech (visible={face.isVisible}, weight={face.GetBlendShapeWeight(aa):F2})");
                SaveFrame(camera, target, $"m2c1-lan-lod{lod}-speech");
            }
            group.ForceLOD(-1);
            lan.MeasureAnimationAllocations = true;
            for (int i = 0; i < 120; i++) yield return null;
            lan.MeasureAnimationAllocations = false;
            Require(lan.MeasuredAnimationFrames >= 120 && lan.AnimationAllocationBytes == 0,
                $"Lan: {lan.AnimationAllocationBytes} managed bytes in {lan.MeasuredAnimationFrames} measured animation updates");

            int interrupted = 0;
            voice.Speak("CH01-D03", "Lan", Line("CH01-D03"), scene.LanVoiceAnchor, lan,
                onComplete: finished => { if (!finished) interrupted++; });
            for (int i = 0; i < 3; i++) yield return null;
            Require(lan.ActiveLineId == "CH01-D03", "Lan: audio did not bind to the face");
            voice.SetPaused(true);
            int pauseSample = lan.voiceSource.timeSamples;
            yield return new WaitForSecondsRealtime(0.15f);
            Require(voice.IsSpeaking && !lan.voiceSource.isPlaying && lan.voiceSource.timeSamples == pauseSample,
                "Lan: audio timeline advanced during pause");
            voice.SetPaused(false);
            yield return new WaitForSecondsRealtime(0.15f);
            Require(lan.voiceSource.isPlaying && lan.voiceSource.timeSamples > pauseSample,
                "Lan: audio timeline did not resume");
            if (Argument("-ctcc-qa-probe") != "char-interruption") voice.StopFor(lan);
            Require(interrupted == 1 && lan.ActiveLineId == null && !voice.IsSpeaking,
                "Lan: interrupted character voice did not release face, voice source and callback");
            for (int i = 0; i < 12; i++)
            {
                lan.gameObject.SetActive(false);
                yield return null;
                lan.gameObject.SetActive(true);
                lan.PlayAction("Lan_IdleDesk", restart: true);
                yield return null;
            }
            Require(lan.ActiveLineId == null && lan.ActiveActionName == "Lan_IdleDesk", "Lan: stale performance after activation");
            camera.enabled = false;
            verification.Add($"CHAR-Q05/Q06/Q08 PASS: three forced Lan face LODs, {lan.MeasuredAnimationFrames} steady-state updates at 0 managed bytes, pause/resume, interrupted voice and 12 reactivations.");
        }
    }
}
