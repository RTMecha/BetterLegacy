using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.EventSystems;

namespace BetterLegacy.Components
{
    public class ContextClickable : MonoBehaviour, IPointerClickHandler
    {
        public Action<PointerEventData> onClick;

        public void OnPointerClick(PointerEventData pointerEventData) => onClick?.Invoke(pointerEventData);
    }
}
