using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CTCC
{
    internal sealed class NotebookDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        internal Action<Vector2> Begin;
        internal Action<Vector2> Move;
        internal Action<Vector2> End;

        public void OnBeginDrag(PointerEventData eventData) { if (eventData.button == PointerEventData.InputButton.Left) Begin?.Invoke(eventData.position); }
        public void OnDrag(PointerEventData eventData) { if (eventData.button == PointerEventData.InputButton.Left) Move?.Invoke(eventData.position); }
        public void OnEndDrag(PointerEventData eventData) { if (eventData.button == PointerEventData.InputButton.Left) End?.Invoke(eventData.position); }
    }
}
