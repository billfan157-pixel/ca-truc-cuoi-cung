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
        private float[] tubePower;
        private Curtain[] cloth;

        private sealed class Curtain
        {
            public Mesh mesh;
            public Vector3[] rest;
            public Vector3[] posed;
        }

        private void Awake()
        {
            if (tubes != null)
            {
                tubePower = new float[tubes.Length];
                for (int i = 0; i < tubes.Length; i++)
                    tubePower[i] = tubes[i] != null ? tubes[i].intensity : 0f;
            }
            if (curtains != null)
            {
                cloth = new Curtain[curtains.Length];
                for (int i = 0; i < curtains.Length; i++)
                {
                    if (curtains[i] == null || curtains[i].sharedMesh == null || !curtains[i].sharedMesh.isReadable)
                        continue;
                    var mesh = curtains[i].mesh;
                    if (mesh != null && mesh.isReadable)
                    {
                        mesh.MarkDynamic();
                        cloth[i] = new Curtain { mesh = mesh, rest = mesh.vertices, posed = mesh.vertices };
                    }
                }
            }
        }

        private void Update()
        {
            float t = Time.unscaledTime;
            if (blades != null)
            {
                for (int i = 0; i < blades.Length; i++)
                    if (blades[i] != null && reportLayer && blades[i].gameObject.activeInHierarchy && !reducedMotion)
                        blades[i].Rotate(0, Time.unscaledDeltaTime * (119 + i * 11), 0, Space.Self);
            }
            if (tubes != null && tubePower != null)
            {
                for (int i = 0; i < tubes.Length; i++)
                {
                    if (tubes[i] == null) continue;
                    float pulse = Mathf.PerlinNoise(t * .38f, i * .86f);
                    tubes[i].intensity = tubePower[i] * (reducedMotion ? 1 : .90f + pulse * .19f);
                }
            }
            if (cloth == null || Time.frameCount % 3 != 0)
                return;
            for (int k = 0; k < cloth.Length; k++)
            {
                var c = cloth[k];
                if (c == null || curtains[k] == null || !curtains[k].gameObject.activeInHierarchy)
                    continue;
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
