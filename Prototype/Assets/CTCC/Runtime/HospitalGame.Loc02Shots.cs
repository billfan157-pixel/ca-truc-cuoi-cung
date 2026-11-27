using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CTCC
{
    // LOC02 Reception visual evidence tool (docs/graphics/loc02-reception-hero.md).
    // Renders a fixed gallery of same-angle stills in both layers and an optional 30 fps fly-through,
    // so before/after comparisons use identical cameras. It never mutates durable save data.
    //   -ctcc-loc02-shots <dir>            stills (R and B) into <dir>
    //   -ctcc-loc02-shots-file <json>      optional replacement shot list (see Loc02ShotFile)
    //   -ctcc-loc02-video <fps>            additionally record frames_R / frames_B (default off)
    public sealed partial class HospitalGame
    {
        [Serializable]
        private sealed class Loc02Shot
        {
            public string name;
            public Vector3 eye;
            public Vector3 look;
            public float fov = 0f;      // 0 = keep the gameplay camera FOV
            public string layers = "RB";
            public bool torch;
        }

        [Serializable]
        private sealed class Loc02ShotFile { public Loc02Shot[] shots; public Loc02Shot[] path; }

        private static Loc02Shot S(string name, float ex, float ey, float ez, float lx, float ly, float lz, string layers = "RB", bool torch = false, float fov = 0f) =>
            new Loc02Shot { name = name, eye = new Vector3(ex, ey, ez), look = new Vector3(lx, ly, lz), layers = layers, torch = torch, fov = fov };

        private static Loc02Shot[] DefaultLoc02Shots() => new[]
        {
            S("L02-01-arrival",        4.00f, 1.62f, -7.10f,  1.60f, 1.30f, -3.30f),
            S("L02-02-hero-wide",      4.35f, 1.62f, -4.65f,  1.50f, 1.35f, -3.50f),
            S("L02-03-counter-front",  3.60f, 1.62f, -3.24f,  1.10f, 1.25f, -3.24f),
            S("L02-04-counter-north",  3.40f, 1.62f, -1.50f,  1.00f, 1.20f, -4.20f),
            S("L02-05-ledger-close",   2.62f, 1.56f, -3.60f,  1.95f, 1.13f, -3.42f),
            S("L02-06-slot-tray",      2.70f, 1.45f, -3.30f,  2.02f, 1.20f, -3.24f),
            S("L02-07-glass-upper",    3.10f, 1.64f, -2.75f,  1.90f, 1.62f, -3.30f),
            S("L02-08-lan-station",    3.05f, 1.60f, -3.40f,  0.55f, 1.15f, -3.25f),
            S("L02-09-behind-desk",   -0.55f, 1.62f, -1.40f,  0.80f, 1.05f, -3.55f),
            S("L02-10-file-rack",      0.20f, 1.55f, -3.10f, -0.95f, 1.35f, -1.92f),
            S("L02-11-wall-clock",     2.40f, 1.60f, -3.24f, -0.99f, 2.62f, -3.26f),
            S("L02-12-waiting-bench",  7.30f, 1.62f, -1.30f,  2.40f, 1.25f, -5.00f),
            S("L02-13-ceiling-fan",    3.80f, 1.60f, -4.40f,  3.80f, 3.00f, -4.40f),
            S("L02-14-floor-wear",     3.50f, 1.62f, -6.20f,  2.40f, 0.05f, -4.30f),
            S("L02-15-torch-counter",  4.60f, 1.62f, -4.60f,  1.60f, 1.20f, -3.40f, "R", true),
            S("L02-16-bench-east",     4.65f, 1.48f, -3.45f,  7.24f, 0.65f, -5.12f),
        };

        private IEnumerator Loc02ShotRun(string directory)
        {
            Directory.CreateDirectory(directory);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            var camera = player.Eye;
            bool prevEnabled = camera.enabled;
            float prevFov = camera.fieldOfView;
            Vector3 prevLocalPos = camera.transform.localPosition;
            Quaternion prevLocalRot = camera.transform.localRotation;
            var restore = State.Copy();
            var done = new List<string>();
            var target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            string failure = null;
            yield return null;

            var body = Loc02ShotBody(directory, camera, target, prevFov, done);
            while (true)
            {
                object yielded;
                try
                {
                    if (!body.MoveNext()) break;
                    yielded = body.Current;
                }
                catch (Exception e)
                {
                    failure = e.ToString();
                    Debug.LogException(e);
                    break;
                }
                yield return yielded;
            }
            bool passed = failure == null;

            camera.fieldOfView = prevFov;
            camera.transform.localPosition = prevLocalPos;
            camera.transform.localRotation = prevLocalRot;
            camera.enabled = prevEnabled;
            player.SetTorchEnabled(false);
            target.Release();
            Destroy(target);

            var report = new List<string>
            {
                "quality=" + QualitySettings.names[QualitySettings.GetQualityLevel()],
                "api=" + SystemInfo.graphicsDeviceType,
                "gpu=" + SystemInfo.graphicsDeviceName,
                "result=" + (passed ? "PASS" : "FAIL"),
            };
            report.AddRange(done);
            if (failure != null) report.Add(failure);
            File.WriteAllLines(Path.Combine(directory, "loc02-shots.txt"), report);
            Debug.Log("CTCC_LOC02_SHOTS " + (passed ? "PASS " : "FAIL ") + done.Count);
            transaction.Present(restore);
            yield return null;
            FinishQa(passed);
        }

        private IEnumerator Loc02ShotBody(string directory, Camera camera, RenderTexture target, float baseFov, List<string> done)
        {
            Loc02Shot[] shots = DefaultLoc02Shots();
            Loc02Shot[] path = null;
            string shotFile = Argument("-ctcc-loc02-shots-file");
            if (!string.IsNullOrEmpty(shotFile) && File.Exists(shotFile))
            {
                var parsed = JsonUtility.FromJson<Loc02ShotFile>(File.ReadAllText(shotFile));
                if (parsed?.shots != null && parsed.shots.Length > 0) shots = parsed.shots;
                if (parsed?.path != null && parsed.path.Length > 1) path = parsed.path;
            }

            target.Create();
            camera.enabled = false;
            player.SetTorchEnabled(false);
            player.Teleport(scene.Pad("AP02"));
            for (int i = 0; i < 20; i++) yield return null;

            foreach (var layer in new[] { Reality.R, Reality.B })
            {
                PresentLoc02Layer(layer);
                for (int i = 0; i < 70; i++) yield return null;
                foreach (var shot in shots)
                {
                    if (!shot.layers.Contains(layer == Reality.R ? "R" : "B")) continue;
                    PlaceLoc02Camera(camera, shot.eye, shot.look, shot.fov > 1f ? shot.fov : baseFov);
                    player.SetTorchEnabled(shot.torch);
                    for (int i = 0; i < 6; i++) yield return null;
                    string name = shot.name + "-" + (layer == Reality.R ? "R" : "B");
                    SaveFrame(camera, target, name);
                    done.Add(name);
                }
                player.SetTorchEnabled(false);
            }

            string video = Argument("-ctcc-loc02-video");
            if (string.IsNullOrEmpty(video)) yield break;
            int fps = Mathf.Clamp(int.Parse(video), 10, 60);
            var route = path ?? DefaultLoc02Path();
            foreach (var layer in new[] { Reality.R, Reality.B })
            {
                PresentLoc02Layer(layer);
                for (int i = 0; i < 70; i++) yield return null;
                string layerName = layer == Reality.R ? "R" : "B";
                string frames = Path.Combine(directory, "frames_" + layerName);
                Directory.CreateDirectory(frames);
                var previousFolder = qaFolder;
                qaFolder = frames;
                int frame = 0;
                for (int leg = 0; leg < route.Length - 1; leg++)
                {
                    float seconds = Mathf.Max(0.5f, Vector3.Distance(route[leg].eye, route[leg + 1].eye) / 0.85f);
                    int count = Mathf.CeilToInt(seconds * fps);
                    for (int k = 0; k < count; k++)
                    {
                        float t = Mathf.SmoothStep(0f, 1f, k / (float)count);
                        var eye = Vector3.Lerp(route[leg].eye, route[leg + 1].eye, t);
                        var look = Vector3.Lerp(route[leg].look, route[leg + 1].look, t);
                        PlaceLoc02Camera(camera, eye, look, baseFov);
                        yield return null;
                        SaveFrame(camera, target, "f" + frame.ToString("D5"));
                        frame++;
                    }
                }
                qaFolder = previousFolder;
                done.Add("video-" + layerName + "-" + frame + "f@" + fps);
            }
        }
        private void PresentLoc02Layer(Reality layer)
        {
            var snap = State.Copy();
            snap.layer = layer;
            snap.chapter = "CH01";
            snap.Remember("CH01-gate-open");
            snap.Remember("CH01-T02");
            if (layer == Reality.B) snap.Remember("CH01-T03");
            transaction.Present(snap);
            scene.PresentCasework(snap);
            lanDirector?.SyncState(snap);
        }

        private static void PlaceLoc02Camera(Camera camera, Vector3 eye, Vector3 look, float fov)
        {
            camera.fieldOfView = fov;
            camera.farClipPlane = Mathf.Max(camera.farClipPlane, 60f);
            camera.transform.position = eye;
            camera.transform.LookAt(look);
        }

        private static Loc02Shot[] DefaultLoc02Path() => new[]
        {
            S("p0", 4.20f, 1.62f, -7.20f, 1.60f, 1.30f, -3.40f),
            S("p1", 4.10f, 1.62f, -5.10f, 1.50f, 1.25f, -3.30f),
            S("p2", 3.40f, 1.62f, -3.60f, 1.30f, 1.20f, -3.20f),
            S("p3", 3.20f, 1.60f, -2.40f, 1.40f, 1.40f, -3.60f),
            S("p4", 3.60f, 1.62f, -4.80f, 0.90f, 1.45f, -3.00f),
        };
    }
}

