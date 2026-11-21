using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CTCC
{
    internal sealed class PaperPointer : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IScrollHandler, IPointerClickHandler
    {
        internal Action<Vector2> Pan;
        internal Action<float, Vector2> Zoom;
        internal Action<Vector2, bool> Press;
        internal Action Resize;
        private Vector2 previous;

        public void OnBeginDrag(PointerEventData eventData) => previous = eventData.pressPosition;

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                Pan?.Invoke(eventData.position - previous);
            previous = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData) { }
        public void OnScroll(PointerEventData eventData) => Zoom?.Invoke(eventData.scrollDelta.y, eventData.position);
        public void OnPointerClick(PointerEventData eventData) =>
            Press?.Invoke(eventData.position, eventData.button == PointerEventData.InputButton.Right);

        private void OnRectTransformDimensionsChange() => Resize?.Invoke();
    }
}