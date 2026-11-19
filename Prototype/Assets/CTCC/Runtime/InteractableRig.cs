using UnityEngine;

namespace CTCC
{
    /// <summary>
    /// Smooth physical animation driver for hinged hospital doors, interactive privacy curtains,
    /// and the rolling steel medical cabinet blocking the Room 101 Store doorway.
    /// Instant snap is used during QA / transactions when immediate collider placement is required.
    /// </summary>
    public sealed class InteractableRig : MonoBehaviour
    {
        public Transform curtainCloth;
        public Vector3 curtainClosedScale = Vector3.one;
        public Vector3 curtainOpenScale = new Vector3(0.28f, 1f, 1.32f);
        public bool curtainPulled;

        private float curtainBlend;
        private Transform cabinet;
        private Vector3 cabinetTarget;
        private bool cabinetMoving;
        private float doorCurrent;
        private float doorTarget;
        private Transform doorPivot;
        private bool doorMoving;
        private bool reducedMotion;

        public void ConfigureCabinet(Transform cab, Vector3 initialPos)
        {
            cabinet = cab;
            cabinetTarget = initialPos;
            cabinetMoving = false;
            if (cabinet != null) cabinet.localPosition = initialPos;
        }

        public void ConfigureDoor(Transform pivot, float initialAngle)
        {
            doorPivot = pivot;
            doorCurrent = initialAngle;
            doorTarget = initialAngle;
            doorMoving = false;
        }

        public void SetReducedMotion(bool reduced) => reducedMotion = reduced;

        public void MoveCabinet(Vector3 target, bool instant, EnvironmentalAudio audioBus)
        {
            cabinetTarget = target;
            if (cabinet == null) return;
            if (instant || reducedMotion)
            {
                cabinet.localPosition = target;
                cabinetMoving = false;
                Physics.SyncTransforms();
                return;
            }
            if ((cabinet.localPosition - target).sqrMagnitude > 0.01f)
            {
                cabinetMoving = true;
                audioBus?.From("cabinetRoll", cabinet.position + Vector3.up * 0.8f);
            }
        }

        public void MoveDoor(Transform pivot, float targetAngle, bool instant, EnvironmentalAudio audioBus)
        {
            doorPivot = pivot;
            doorTarget = targetAngle;
            if (doorPivot == null) return;
            if (instant || reducedMotion)
            {
                doorCurrent = targetAngle;
                doorPivot.localRotation = Quaternion.Euler(0f, targetAngle, 0f);
                doorMoving = false;
                Physics.SyncTransforms();
                return;
            }
            if (Mathf.Abs(Mathf.DeltaAngle(doorCurrent, targetAngle)) > 1.5f)
            {
                doorMoving = true;
                audioBus?.From(Mathf.Abs(targetAngle) > 10f ? "doorCreak" : "doorLatch", doorPivot.position + Vector3.up * 1.1f);
            }
        }

        public bool ToggleCurtain(EnvironmentalAudio audioBus)
        {
            curtainPulled = !curtainPulled;
            if (curtainCloth != null)
                audioBus?.From("curtainSlide", curtainCloth.position);
            return curtainPulled;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (curtainCloth != null)
            {
                float target = curtainPulled ? 1f : 0f;
                curtainBlend = Mathf.MoveTowards(curtainBlend, target, dt * (reducedMotion ? 8f : 1.75f));
                float ease = curtainBlend * curtainBlend * (3f - 2f * curtainBlend);
                curtainCloth.localScale = Vector3.Lerp(curtainClosedScale, curtainOpenScale, ease);
            }

            if (cabinetMoving && cabinet != null)
            {
                cabinet.localPosition = Vector3.MoveTowards(cabinet.localPosition, cabinetTarget, dt * 1.25f);
                if ((cabinet.localPosition - cabinetTarget).sqrMagnitude < 0.0004f)
                {
                    cabinet.localPosition = cabinetTarget;
                    cabinetMoving = false;
                }
                Physics.SyncTransforms();
            }

            if (doorMoving && doorPivot != null)
            {
                doorCurrent = Mathf.MoveTowardsAngle(doorCurrent, doorTarget, dt * 135f);
                doorPivot.localRotation = Quaternion.Euler(0f, doorCurrent, 0f);
                if (Mathf.Abs(Mathf.DeltaAngle(doorCurrent, doorTarget)) < 0.25f)
                {
                    doorCurrent = doorTarget;
                    doorPivot.localRotation = Quaternion.Euler(0f, doorTarget, 0f);
                    doorMoving = false;
                }
                Physics.SyncTransforms();
            }
        }
    }
}
