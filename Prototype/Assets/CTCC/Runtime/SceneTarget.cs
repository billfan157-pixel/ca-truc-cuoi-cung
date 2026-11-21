using UnityEngine;

namespace CTCC
{
    public sealed class SceneTarget : MonoBehaviour
    {
        public string id;
        public string title;
        public int focusPriority;
        public Collider visibilitySurface;
        public Transform handhold;
        public bool frontOnly;
        internal Ch01FieldObject fieldObject;
        internal GameObject inspectModel;

        internal const float Reach = 2.60f;
        internal const float SightTolerance = .085f;
        internal Vector3 Contact => handhold != null ? handhold.position : transform.position;

        internal bool VisibleFrom(Vector3 origin, Vector3 contact)
        {
            if (frontOnly && handhold != null && Vector3.Dot(origin - Contact, handhold.forward) <= SightTolerance)
                return false;
            Vector3 offset = contact - origin;
            float distance = offset.magnitude;
            return distance <= SightTolerance ||
                !Physics.Raycast(origin, offset / distance, out var hit, distance - SightTolerance,
                    HospitalScene.SightMask, QueryTriggerInteraction.Collide) ||
                hit.collider == visibilitySurface;
        }
    }
}
