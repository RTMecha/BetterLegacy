using System;

using UnityEngine;
using UnityEngine.EventSystems;

namespace BetterLegacy.Editor.Components
{
    /// <summary>
    /// Provides a custom action to occur when different mouse buttons are clicked on a level button.
    /// </summary>
    public class FolderButtonFunction : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>
        /// Action to invoke when mouse clicked on level button.
        /// </summary>
        public Action<PointerEventData> onClick;

        public void OnPointerClick(PointerEventData eventData) => onClick?.Invoke(eventData);
    }
}
