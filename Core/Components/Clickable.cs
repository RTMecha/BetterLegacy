using System;

using UnityEngine;
using UnityEngine.EventSystems;

namespace BetterLegacy.Core.Components
{
    /// <summary>
    /// Interaction component.
    /// </summary>
    public class Clickable : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        /// <summary>
        /// Action invoked when object becomes enabled or disabled. Passed boolean value is if the object is true when the object is enabled, otherwise false.
        /// </summary>
        public Action<bool> onEnable;

        public Action<PointerEventData> onClick;
        public Action<PointerEventData> onDown;
        public Action<PointerEventData> onEnter;
        public Action<PointerEventData> onExit;
        public Action<PointerEventData> onUp;

        public Action<PointerEventData> onBeginDrag;
        public Action<PointerEventData> onDrag;
        public Action<PointerEventData> onEndDrag;

        public Action<PointerEventData> onScroll;

        void OnEnable() => onEnable?.Invoke(true);
        void OnDisable() => onEnable?.Invoke(false);

        public void OnBeginDrag(PointerEventData pointerEventData) => onBeginDrag?.Invoke(pointerEventData);
        public void OnDrag(PointerEventData pointerEventData) => onDrag?.Invoke(pointerEventData);
        public void OnEndDrag(PointerEventData pointerEventData) => onEndDrag?.Invoke(pointerEventData);

        public void OnPointerClick(PointerEventData pointerEventData) => onClick?.Invoke(pointerEventData);

        public void OnPointerDown(PointerEventData pointerEventData) => onDown?.Invoke(pointerEventData);

        public void OnPointerEnter(PointerEventData pointerEventData) => onEnter?.Invoke(pointerEventData);

        public void OnPointerExit(PointerEventData pointerEventData) => onExit?.Invoke(pointerEventData);

        public void OnPointerUp(PointerEventData pointerEventData) => onUp?.Invoke(pointerEventData);

        public void OnScroll(PointerEventData pointerEventData) => onScroll?.Invoke(pointerEventData);
    }
}
