using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;
using HarmonyLib;
using LSFunctions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(SettingEditor))]
    public class SettingEditorPatch : MonoBehaviour
    {
        static SettingEditor Instance { get => SettingEditor.inst; set => SettingEditor.inst = value; }

        [HarmonyPatch(nameof(SettingEditor.Awake))]
        [HarmonyPrefix]
        static bool AwakePostfix(SettingEditor __instance)
        {
            if (Instance == null)
                Instance = __instance;
            else if (Instance != __instance)
            {
                Destroy(__instance.gameObject);
                return false;
            }

            CoreHelper.LogInit(__instance.className);

            return false;
        }

        [HarmonyPatch(nameof(SettingEditor.Render))]
        [HarmonyPrefix]
        static bool RenderPrefix()
        {
            RTSettingEditor.inst.OpenDialog();
            return false;
        }
    }
}
