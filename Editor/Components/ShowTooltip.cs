using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using BetterLegacy.Configs;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Managers;
using BetterLegacy.Core.Data;

namespace BetterLegacy.Editor.Components
{
    /// <summary>
    /// Component for displaying mouse tooltips.
    /// </summary>
    public class ShowTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!EditorConfig.Instance.MouseTooltipDisplay.Value || !EditorManager.inst.showHelp && EditorConfig.Instance.MouseTooltipRequiresHelp.Value)
                return;

            if (desc && hint)
            {
                Show(keys, desc, hint);
                return;
            }

            int index = tooltips.FindIndex(x => (int)((Tooltip)x).language == (int)CoreConfig.Instance.Language.Value);

            if (index < 0)
                return;
            Show(tooltips[index].keys, tooltips[index].desc, tooltips[index].hint);
        }

        void Show(List<string> keys, string desc, string hint)
        {
            RTEditor.inst.tooltipTimeOffset = Time.time;
            RTEditor.inst.maxTooltipTime = time * EditorConfig.Instance.MouseTooltipDisplayTime.Value * (hint.Length / 70f);
            RTEditor.inst.showTootip = true;

            cachedTooltip = EditorManager.inst.TooltipConverter(keys, desc, hint);
            if (RTEditor.inst.mouseTooltipText)
                RTEditor.inst.mouseTooltipText.text = cachedTooltip;

            if (RTEditor.inst.mouseTooltipText)
                LayoutRebuilder.ForceRebuildLayoutImmediate(RTEditor.inst.mouseTooltipText.rectTransform);
            if (RTEditor.inst.mouseTooltipRT)
                LayoutRebuilder.ForceRebuildLayoutImmediate(RTEditor.inst.mouseTooltipRT);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            RTEditor.inst.showTootip = false;

            if (!EditorConfig.Instance.HideMouseTooltipOnExit.Value)
                return;

            RTEditor.inst.tooltipActive = false;

            if (RTEditor.inst.mouseTooltip)
                RTEditor.inst.mouseTooltip.SetActive(false);
        }

        void OnDisable()
        {
            if (string.IsNullOrEmpty(cachedTooltip) || !RTEditor.inst.mouseTooltipText || RTEditor.inst.mouseTooltipText.text != cachedTooltip)
                return;

            RTEditor.inst.showTootip = false;
            RTEditor.inst.tooltipActive = false;

            if (RTEditor.inst.mouseTooltip)
                RTEditor.inst.mouseTooltip.SetActive(false);
        }

        /// <summary>
        /// Amount of time to display the mouse tooltip for.
        /// </summary>
        public float time = 2f;

        public List<string> keys;
        public Lang desc;
        public Lang hint;

        string cachedTooltip;

        [NonSerialized]
        public List<HoverTooltip.Tooltip> tooltips = new List<HoverTooltip.Tooltip>();
    }
}
