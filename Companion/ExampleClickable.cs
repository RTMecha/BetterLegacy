using System;

using UnityEngine;
using UnityEngine.EventSystems;

namespace BetterLegacy.Companion
{
    public class ExampleClickable : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Action<PointerEventData> onClick;
        public Action<PointerEventData> onDown;
        public Action<PointerEventData> onUp;

        public void OnPointerClick(PointerEventData pointerEventData) => onClick?.Invoke(pointerEventData);

        public void OnPointerDown(PointerEventData pointerEventData) => onDown?.Invoke(pointerEventData);

        public void OnPointerUp(PointerEventData pointerEventData) => onUp?.Invoke(pointerEventData);
    }
}
