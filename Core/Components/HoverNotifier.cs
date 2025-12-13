using System;

using UnityEngine;
using UnityEngine.EventSystems;

namespace BetterLegacy.Core.Components
{
    public class HoverNotifier : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action<bool, PointerEventData> notifier;
        public Action<PointerEventData> onEnter;
        public Action<PointerEventData> onExit;

        public void OnPointerEnter(PointerEventData pointerEventData)
        {
            notifier?.Invoke(true, pointerEventData);
            onEnter?.Invoke(pointerEventData);
        }

        public void OnPointerExit(PointerEventData pointerEventData)
        {
            notifier?.Invoke(false, pointerEventData);
            onExit?.Invoke(pointerEventData);
        }
    }
}
