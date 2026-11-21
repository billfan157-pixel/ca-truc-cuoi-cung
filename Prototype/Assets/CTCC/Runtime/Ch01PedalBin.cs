using UnityEngine;

namespace CTCC
{
    public sealed class Ch01PedalBin : MonoBehaviour
    {
        public Transform lid;
        public Transform pedal;
        public float lidAngle;
        public float pedalAngle;
        private float openness;
        public bool HeldOpen { get; private set; }
        public bool CanSeeInside => openness > .62f && HeldOpen;
        public bool ReducedMotion { get; set; }

        public void SetOpen(bool open, bool immediate = false)
        {
            HeldOpen = open;
            if (!immediate && !ReducedMotion) return;
            openness = open ? 1f : 0f;
            ApplyPose();
        }

        private void Update()
        {
            float next = Mathf.MoveTowards(openness, HeldOpen ? 1f : 0f,
                Time.unscaledDeltaTime * (HeldOpen ? 5.5f : 3.4f));
            if (Mathf.Approximately(openness, next)) return;
            openness = next;
            ApplyPose();
        }

        private void ApplyPose()
        {
            float eased = Mathf.SmoothStep(0, 1, openness);
            if (lid != null) lid.localRotation = Quaternion.Euler(lidAngle * eased, 0, 0);
            if (pedal != null) pedal.localRotation = Quaternion.Euler(pedalAngle * eased, 0, 0);
        }
    }
}
