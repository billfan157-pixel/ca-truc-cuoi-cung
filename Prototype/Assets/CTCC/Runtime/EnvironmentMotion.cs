using System.Collections.Generic;
using UnityEngine;

namespace CTCC
{
    public sealed class EnvironmentMotion : MonoBehaviour
    {
        public Transform[] blades;
        public Light[] tubes;
        public MeshFilter[] curtains;
        public bool reducedMotion;
        public bool reportLayer;
        public float fanSpeedScale = 1f;

        private readonly List<Transform> dynamicBlades = new();
        private readonly List<(Light light, float power)> dynamicTubes = new();
        private readonly List<Curtain> dynamicCloth = new();

        private sealed class Curtain
        {
            public MeshFilter filter;
            public Mesh mesh;
            public Vector3[] rest;
            public Vector3[] posed;
        }

        private void Awake()
        {
            Register(blades, tubes, curtains);
        }

        public void Register(Transform[] extraBlades, Light[] extraTubes, MeshFilter[] extraCurtains)
        {
            if (extraBlades != null)
            {
                foreach (var b in extraBlades)
                    if (b != null && !dynamicBlades.Contains(b)) dynamicBlades.Add(b);
            }
            if (extraTubes != null)
            {
                foreach (var l in extraTubes)
                {
                    if (l == null) continue;
                    bool exists = false;
                    for (int i = 0; i < dynamicTubes.Count; i++)
                        if (dynamicTubes[i].light == l) { exists = true; break; }
                    if (!exists) dynamicTubes.Add((l, l.intensity));
                }
            }
            if (extraCurtains != null)
            {
                foreach (var mf in extraCurtains)
                {
                    if (mf == null || mf.sharedMesh == null || !mf.sharedMesh.isReadable) continue;
                    bool exists = false;
                    for (int i = 0; i < dynamicCloth.Count; i++)
                        if (dynamicCloth[i].filter == mf) { exists = true; break; }
                    if (exists) continue;
                    var mesh = mf.mesh;
                    if (mesh != null && mesh.isReadable)
                    {
                        mesh.MarkDynamic();
                        dynamicCloth.Add(new Curtain { filter = mf, mesh = mesh, rest = mesh.vertices, posed = mesh.vertices });
                    }
                }
            }
        }

        public void Unregister(Transform[] extraBlades, Light[] extraTubes, MeshFilter[] extraCurtains)
        {
            if (extraBlades != null)
                foreach (var b in extraBlades) dynamicBlades.Remove(b);
            if (extraTubes != null)
                dynamicTubes.RemoveAll(t => t.light == null || System.Array.IndexOf(extraTubes, t.light) >= 0);
            if (extraCurtains != null)
                dynamicCloth.RemoveAll(c => c.filter == null || System.Array.IndexOf(extraCurtains, c.filter) >= 0);
        }

        private void Update()
        {
            float t = Time.unscaledTime;
            for (int i = dynamicBlades.Count - 1; i >= 0; i--)
            {
                var blade = dynamicBlades[i];
                if (blade == null) { dynamicBlades.RemoveAt(i); continue; }
                if (reportLayer && blade.gameObject.activeInHierarchy && !reducedMotion && fanSpeedScale > 0.01f)
                    blade.Rotate(0, Time.unscaledDeltaTime * (119 + i * 11) * fanSpeedScale, 0, Space.Self);
            }

            for (int i = dynamicTubes.Count - 1; i >= 0; i--)
            {
                var entry = dynamicTubes[i];
                if (entry.light == null) { dynamicTubes.RemoveAt(i); continue; }
                float pulse = Mathf.PerlinNoise(t * .38f, i * .86f);
                entry.light.intensity = entry.power * (reducedMotion ? 1f : .90f + pulse * .19f);
            }

            if (dynamicCloth.Count == 0 || Time.frameCount % 3 != 0)
                return;
            for (int k = dynamicCloth.Count - 1; k >= 0; k--)
            {
                var c = dynamicCloth[k];
                if (c.filter == null || c.mesh == null) { dynamicCloth.RemoveAt(k); continue; }
                if (!c.filter.gameObject.activeInHierarchy) continue;
                for (int i = 0; i < c.rest.Length; i++)
                {
                    var p = c.rest[i];
                    if (!reducedMotion)
                        p.z += Mathf.Sin(p.x * 3.5f + p.y * 1.2f + t * 1.66f + k) * Mathf.Clamp01(-p.y * .63f) * .045f;
                    c.posed[i] = p;
                }
                c.mesh.vertices = c.posed;
            }
        }
    }
}
