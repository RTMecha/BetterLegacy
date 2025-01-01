using BetterLegacy.Configs;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetterLegacy.Editor.Components
{
    /// <summary>
    /// Component for displaying mouse tooltips.
    /// </summary>
    public class ShowTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!EditorConfig.Instance.MouseTooltipDisplay.Value || !EditorManager.inst.showHelp)
                return;

            int index = tooltips.FindIndex(x => (int)((Tooltip)x).language == (int)CoreConfig.Instance.Language.Value);

            if (index < 0)
                return;

            RTEditor.inst.tooltipTimeOffset = Time.time;
            RTEditor.inst.maxTooltipTime = time * EditorConfig.Instance.MouseTooltipDisplayTime.Value * (tooltips[index].hint.Length / 70f);
            RTEditor.inst.showTootip = true;

            if (RTEditor.inst.mouseTooltipText)
                RTEditor.inst.mouseTooltipText.text = EditorManager.inst.TooltipConverter(tooltips[index].keys, tooltips[index].desc, tooltips[index].hint);

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

            RTEditor.inst.mouseTooltip?.SetActive(false);
        }

        /// <summary>
        /// Amount of time to display the mouse tooltip for.
        /// </summary>
        public float time = 2f;
        [NonSerialized]
        public List<HoverTooltip.Tooltip> tooltips = new List<HoverTooltip.Tooltip>();
    }
}
