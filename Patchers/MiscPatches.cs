using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using HarmonyLib;
using System;
using System.Collections;
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

    [HarmonyPatch(typeof(InputField))]
    public class InputFieldPatch
    {
        [HarmonyPatch("Append", new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool AppendPrefix(InputField __instance, ref bool ___m_ReadOnly, ref bool ___m_TouchKeyboardAllowsInPlaceEditing, string __0)
        {
            if (___m_ReadOnly)
                return false;
            CoreHelper.StartCoroutine(IAppend(__instance, ___m_ReadOnly, ___m_TouchKeyboardAllowsInPlaceEditing, __0));
            return false;
        }

        static IEnumerator IAppend(InputField __instance, bool ___m_ReadOnly, bool ___m_TouchKeyboardAllowsInPlaceEditing, string __0)
        {
            if (!TouchScreenKeyboard.isSupported || ___m_TouchKeyboardAllowsInPlaceEditing)
            {
                int i = 0;
                int length = __0.Length;
                while (i < length)
                {
                    char c = __0[i];
                    if (c >= ' ' || c == '\t' || c == '\r' || c == '\n' || c == '\n')
                    {
                        Append(__instance, ___m_ReadOnly, ___m_TouchKeyboardAllowsInPlaceEditing, c, i == length - 1);
                    }
                    i++;
                }
            }
            yield break;
        }

        static void Append(InputField __instance, bool m_ReadOnly, bool m_TouchKeyboardAllowsInPlaceEditing, char input, bool sendOnValueChanged)
        {
            if (char.IsSurrogate(input))
                return;

            var type = typeof(InputField);
            if (!m_ReadOnly /*&& __instance.text.Length < 16382*/)
            {
                if (!TouchScreenKeyboard.isSupported || m_TouchKeyboardAllowsInPlaceEditing)
                {
                    int num = Math.Min(__instance.selectionFocusPosition, __instance.selectionAnchorPosition);
                    if (__instance.onValidateInput != null)
                    {
                        input = __instance.onValidateInput(__instance.text, num, input);
                    }
                    else if (__instance.characterValidation != InputField.CharacterValidation.None)
                    {
                        input = (char)AccessTools.Method(type, "Validate").Invoke(__instance, new object[] { __instance.text, num, input });
                    }
                    if (input != '\0')
                    {
                        Insert(type, __instance, m_ReadOnly, input, sendOnValueChanged);
                    }
                }
            }
        }

        static void Insert(Type inputFieldType, InputField __instance, bool m_ReadOnly, char c, bool sendOnValueChanged)
        {
            if (m_ReadOnly)
                return;

            string text = c.ToString();
            AccessTools.Method(inputFieldType, "Delete").Invoke(__instance, new object[] { });
            if (__instance.characterLimit <= 0 || __instance.text.Length < __instance.characterLimit)
            {
                AccessTools.Field(inputFieldType, "m_Text").SetValue(__instance, __instance.text.Insert((int)AccessTools.Field(inputFieldType, "m_CaretPosition").GetValue(__instance), text));

                var caretSelectedPosition = (__instance.selectionAnchorPosition += text.Length);
                ClampPos(__instance, ref caretSelectedPosition);
                AccessTools.Field(inputFieldType, "m_CaretSelectPosition").SetValue(__instance, caretSelectedPosition);

                // Probably not needed.
                //__instance.UpdateTouchKeyboardFromEditChanges();
                if (sendOnValueChanged)
                    AccessTools.Method(inputFieldType, "SendOnValueChanged").Invoke(__instance, new object[] { });
            }
        }

        static void ClampPos(InputField __instance, ref int pos)
        {
            if (pos < 0)
            {
                pos = 0;
            }
            else if (pos > __instance.text.Length)
            {
                pos = __instance.text.Length;
            }
        }
    }
}
