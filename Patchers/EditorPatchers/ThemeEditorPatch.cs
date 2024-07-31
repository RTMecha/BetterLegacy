using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Managers;
using HarmonyLib;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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

            Destroy(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/theme/themes/viewport/content").GetComponent<VerticalLayoutGroup>());

            return false;
        }

        [HarmonyPatch(nameof(ThemeEditor.Start))]
        [HarmonyPrefix]
        static bool StartPrefix() => false;

        [HarmonyPatch(nameof(ThemeEditor.DeleteTheme))]
        [HarmonyPrefix]
        static bool DeleteThemePrefix(DataManager.BeatmapTheme __0)
        {
            RTThemeEditor.inst.DeleteTheme((BeatmapTheme)__0);
            return false;
        }

        [HarmonyPatch(nameof(ThemeEditor.SaveTheme))]
        [HarmonyPrefix]
        static bool SaveThemePrefix(DataManager.BeatmapTheme __0)
        {
            RTThemeEditor.inst.SaveTheme((BeatmapTheme)__0);
            return false;
        }

        [HarmonyPatch(nameof(ThemeEditor.LoadThemes))]
        [HarmonyPrefix]
        static bool LoadThemesPrefix(ref IEnumerator __result)
        {
            __result = RTEditor.inst.LoadThemes();
            return false;
        }
    }
}
