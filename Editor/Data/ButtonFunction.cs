using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.EventSystems;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Handles a buttons' UI in the editor.
    /// </summary>
    public class ButtonFunction
    {
        public ButtonFunction(bool isSpacer, float spacerSize = 4f)
        {
            IsSpacer = isSpacer;
            SpacerSize = spacerSize;
        }

        public ButtonFunction(string name, Action action)
        {
            Name = name;
            Action = action;
        }

        public ButtonFunction(string name, Action<PointerEventData> onClick)
        {
            Name = name;
            OnClick = onClick;
        }

        public bool IsSpacer { get; set; }
        public float SpacerSize { get; set; } = 4f;
        public string Name { get; set; }
        public int FontSize { get; set; } = 20;
        public Action Action { get; set; }
        public Action<PointerEventData> OnClick { get; set; }
    }
}
