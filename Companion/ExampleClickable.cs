using System;

using UnityEngine;
using UnityEngine.EventSystems;

namespace BetterLegacy.Companion
{
    public class ExampleClickable : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler
    {
        public Action<PointerEventData> onClick;
        public Action<PointerEventData> onDown;
        public Action<PointerEventData> onEnter;
        public Action<PointerEventData> onExit;
        public Action<PointerEventData> onUp;

        public void OnPointerClick(PointerEventData pointerEventData) => onClick?.Invoke(pointerEventData);

        public void OnPointerDown(PointerEventData pointerEventData) => onDown?.Invoke(pointerEventData);

        public void OnPointerEnter(PointerEventData pointerEventData) => onEnter?.Invoke(pointerEventData);

        public void OnPointerExit(PointerEventData pointerEventData) => onExit?.Invoke(pointerEventData);

        public void OnPointerUp(PointerEventData pointerEventData) => onUp?.Invoke(pointerEventData);
    }
}
