using BetterLegacy.Configs;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetterLegacy.Components.Editor
{
    public class ShowTooltip : MonoBehaviour, IPointerEnterHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!EditorConfig.Instance.MouseTooltipDisplay.Value || !EditorManager.inst.showHelp)
                return;

            int index = tooltips.FindIndex(x => (int)((Tooltip)x).language == (int)CoreConfig.Instance.Language.Value);

            if (index < 0)
                return;

            RTEditor.inst.tooltipTimeOffset = Time.time;
            RTEditor.inst.maxTooltipTime = time;
            RTEditor.inst.tooltipActive = true;

            RTEditor.inst.mouseTooltip?.SetActive(true);

            if (RTEditor.inst.mouseTooltipText)
                RTEditor.inst.mouseTooltipText.text = EditorManager.inst.TooltipConverter(tooltips[index].keys, tooltips[index].desc, tooltips[index].hint);

            if (RTEditor.inst.mouseTooltipText)
                LayoutRebuilder.ForceRebuildLayoutImmediate(RTEditor.inst.mouseTooltipText.rectTransform);
            if (RTEditor.inst.mouseTooltipRT)
                LayoutRebuilder.ForceRebuildLayoutImmediate(RTEditor.inst.mouseTooltipRT);
        }

        public float time = 2f;
        [NonSerialized]
        public List<HoverTooltip.Tooltip> tooltips = new List<HoverTooltip.Tooltip>();
    }
}
