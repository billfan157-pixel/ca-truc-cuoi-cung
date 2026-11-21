using UnityEngine;

namespace CTCC
{
    public sealed class SceneTarget : MonoBehaviour
    {
        public string id;
        public string title;
        public int focusPriority;
        public Collider visibilitySurface;
        internal Ch01FieldObject fieldObject;
        internal GameObject inspectModel;
    }
}
