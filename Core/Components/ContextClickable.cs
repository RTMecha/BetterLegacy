using System;

using UnityEngine;
using UnityEngine.EventSystems;

namespace BetterLegacy.Core.Components
{
    public class ContextClickable : MonoBehaviour, IPointerClickHandler
    {
        public Action<PointerEventData> onClick;

        public void OnPointerClick(PointerEventData pointerEventData) => onClick?.Invoke(pointerEventData);
    }
}
