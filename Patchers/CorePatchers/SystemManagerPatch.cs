using UnityEngine;

using HarmonyLib;

using BetterLegacy.Configs;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(SystemManager))]
    public class SystemManagerPatch
    {
        [HarmonyPatch(nameof(SystemManager.Awake))]
        [HarmonyPostfix]
        static void AwakePostfix() => Debug.unityLogger.logEnabled = CoreConfig.Instance.DebugsOn.Value;

        [HarmonyPatch(nameof(SystemManager.Update))]
        [HarmonyPrefix]
        static bool UpdatePrefix()
        {
            if (CoreHelper.IsUsingInputField)
                return false;

            if (Input.GetKeyDown(CoreConfig.Instance.ScreenshotKey.Value))
                CoreHelper.TakeScreenshot();

            if (Input.GetKeyDown(CoreConfig.Instance.FullscreenKey.Value))
                CoreConfig.Instance.Fullscreen.Value = !CoreConfig.Instance.Fullscreen.Value;

            return false;
        }
    }
}
