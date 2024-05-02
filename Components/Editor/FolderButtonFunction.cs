using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;

namespace BetterLegacy.Components
{
    public class FolderButtonFunction : MonoBehaviour, IPointerClickHandler
    {
        public Action<PointerEventData> onClick;

        public void OnPointerClick(PointerEventData eventData) => onClick?.Invoke(eventData);
    }
}
