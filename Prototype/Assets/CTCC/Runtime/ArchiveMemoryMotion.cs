using UnityEngine;

namespace CTCC
{
    public sealed class ArchiveMemoryMotion : MonoBehaviour
    {
        public Transform needleA;
        public Transform needleB;
        private Quaternion originalA;
        private Quaternion originalB;
        private float began;

        private void OnEnable()
        {
            originalA = needleA.localRotation;
            originalB = needleB.localRotation;
            began = Time.unscaledTime;
        }

        private void Update()
        {
            float phase = (Time.unscaledTime - began) * 4.2f;
            needleA.localRotation = originalA * Quaternion.Euler(Mathf.Sin(phase) * 8f, 0, 0);
            needleB.localRotation = originalB * Quaternion.Euler(Mathf.Sin(phase + .75f) * -6f, 0, 0);
        }

        private void OnDisable()
        {
            needleA.localRotation = originalA;
            needleB.localRotation = originalB;
        }
    }
}
