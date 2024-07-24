using BetterLegacy.Configs;
using BetterLegacy.Core;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(Dropdown))]
    public class DropdownPatch
    {
        [HarmonyPatch(nameof(Dropdown.Show))]
        [HarmonyPostfix]
        static void ShowPostfix(Dropdown __instance)
        {
            if (__instance.gameObject && __instance.transform && __instance.gameObject.TryGetComponent(out HideDropdownOptions hideDropdownOptions))
            {
                var content = __instance.transform.Find("Dropdown List/Viewport/Content");
                for (int i = 0; i < hideDropdownOptions.DisabledOptions.Count; i++)
                {
                    if (!(content.childCount > i + 1))
                        continue;

                    if (hideDropdownOptions.remove && hideDropdownOptions.DisabledOptions[i])
                        content.GetChild(i + 1).gameObject.SetActive(false);
                    else
                        content.GetChild(i + 1).GetComponent<Toggle>().interactable = !hideDropdownOptions.DisabledOptions[i];

                    content.GetChild(i + 1).AsRT().sizeDelta = new Vector2(0f, 32f);
                }
            }
        }
    }

    [HarmonyPatch(typeof(HideDropdownOptions))]
    public class HideDropdownOptionsPatch
    {
        [HarmonyPatch(nameof(HideDropdownOptions.OnPointerClick))]
        [HarmonyPrefix]
        static bool OnPointerClickPrefix() => false;
    }

    [HarmonyPatch(typeof(DropdownHovered))]
    public class DropdownHoveredPatch
    {
        [HarmonyPatch(nameof(DropdownHovered.OnPointerEnter))]
        [HarmonyPrefix]
        static bool OnPointerEnterPrefix(DropdownHovered __instance, PointerEventData __0)
        {
            var dropdown = __instance.GetComponent<Dropdown>();
            if (!dropdown || !EditorConfig.Instance.ShowDropdownOnHover.Value)
                return false;

            dropdown.Show();
            return false;
        }
    }

    [HarmonyPatch(typeof(LSFunctions.LSHelpers))]
    public class LSHelpersPatch
    {
        [HarmonyPatch(nameof(LSFunctions.LSHelpers.IsUsingInputField))]
        [HarmonyPrefix]
        static bool IsUsingInputFieldPrefix(ref bool __result)
        {
            __result = EventSystem.current && EventSystem.current.currentSelectedGameObject &&
                (EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() || EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>());
            return false;
        }
    }

    [HarmonyPatch(typeof(HoverTooltip))]
    public class HoverTooltipPatch
    {
        [HarmonyPatch(nameof(HoverTooltip.OnPointerEnter))]
        [HarmonyPrefix]
        static bool OnPointerEnterPrefix(HoverTooltip __instance)
        {
            var index = (int)CoreConfig.Instance.Language.Value;

            var tooltip = __instance.tooltipLangauges.Find(x => (int)x.language == index);
            var hasTooltip = tooltip != null;

            EditorManager.inst.SetTooltip(hasTooltip ? tooltip.keys : new List<string>(), hasTooltip ? tooltip.desc : "No tooltip added yet!", hasTooltip ? tooltip.hint : __instance.gameObject.name);
            return false;
        }

        [HarmonyPatch(nameof(HoverTooltip.OnPointerExit))]
        [HarmonyPrefix]
        static bool OnPointerExitPrefix() => false; // Don't want to have the actual mouse tooltip to disappear when I don't want it to.
    }
}
