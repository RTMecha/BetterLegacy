﻿using System.Collections;

using UnityEngine;

using HarmonyLib;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(ThemeEditor))]
    public class ThemeEditorPatch : MonoBehaviour
    {
        public static ThemeEditor Instance { get => ThemeEditor.inst; set => ThemeEditor.inst = value; }

        public static string className = "[<color=#3E6D73>ThemeEditor</color>] \n";

        [HarmonyPatch(nameof(ThemeEditor.Awake))]
        [HarmonyPrefix]
        static bool AwakePrefix(ThemeEditor __instance)
        {
            if (Instance == null)
                Instance = __instance;
            else if (Instance != __instance)
            {
                Destroy(__instance.gameObject);
                return false;
            }

            CoreHelper.LogInit(className);

            return false;
        }

        [HarmonyPatch(nameof(ThemeEditor.Start))]
        [HarmonyPrefix]
        static bool StartPrefix() => false;

        [HarmonyPatch(nameof(ThemeEditor.DeleteTheme))]
        [HarmonyPrefix]
        static bool DeleteThemePrefix() => false;

        [HarmonyPatch(nameof(ThemeEditor.SaveTheme))]
        [HarmonyPrefix]
        static bool SaveThemePrefix() => false;

        [HarmonyPatch(nameof(ThemeEditor.LoadThemes))]
        [HarmonyPrefix]
        static bool LoadThemesPrefix(ref IEnumerator __result)
        {
            __result = RTThemeEditor.inst.LoadThemes();
            return false;
        }
    }
}
