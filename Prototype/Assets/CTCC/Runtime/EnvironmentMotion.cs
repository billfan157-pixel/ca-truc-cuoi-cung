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
            tubePower = new float[tubes.Length];
            for (int i = 0; i < tubes.Length; i++)
                tubePower[i] = tubes[i].intensity;
            cloth = new Curtain[curtains.Length];
            for (int i = 0; i < curtains.Length; i++)
            {
                var mesh = curtains[i].mesh;
                mesh.MarkDynamic();
                cloth[i] = new Curtain { mesh = mesh, rest = mesh.vertices, posed = mesh.vertices };
            }
        }

        private void Update()
        {
            float t = Time.unscaledTime;
            for (int i = 0; i < blades.Length; i++)
                if (reportLayer && blades[i].gameObject.activeInHierarchy && !reducedMotion)
                    blades[i].Rotate(0, Time.unscaledDeltaTime * (119 + i * 11), 0, Space.Self);
            for (int i = 0; i < tubes.Length; i++)
            {
                float pulse = Mathf.PerlinNoise(t * .38f, i * .86f);
                tubes[i].intensity = tubePower[i] * (reducedMotion ? 1 : .90f + pulse * .19f);
            }
            if (Time.frameCount % 3 != 0)
                return;
            for (int k = 0; k < cloth.Length; k++)
            {
                if (!curtains[k].gameObject.activeInHierarchy)
                    continue;
                var c = cloth[k];
                for (int i = 0; i < c.rest.Length; i++)
                {
                    var p = c.rest[i];
                    if (!reducedMotion)
                        p.z += Mathf.Sin(p.x*3.5f + p.y*1.2f + t*1.66f + k) * Mathf.Clamp01(-p.y * .63f) * .065f;
                    c.posed[i] = p;
                }
                c.mesh.vertices = c.posed;
            }
        }
    }
}
