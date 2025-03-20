using UnityEngine;

using HarmonyLib;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Managers;

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
