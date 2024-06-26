using BetterLegacy.Configs;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Example;
using HarmonyLib;
using LSFunctions;
using UnityEngine;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(SystemManager))]
    public class SystemManagerPatch
    {
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        static void AwakePostfix()
        {
            Debug.unityLogger.logEnabled = CoreConfig.Instance.DebugsOn.Value;
            ExampleManager.Init();
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool UpdatePrefix()
        {
            if (LSHelpers.IsUsingInputField())
                return false;

            if (Input.GetKeyDown(CoreConfig.Instance.ScreenshotKey.Value))
                CoreHelper.TakeScreenshot();

            if (Input.GetKeyDown(CoreConfig.Instance.FullscreenKey.Value))
                CoreConfig.Instance.Fullscreen.Value = !CoreConfig.Instance.Fullscreen.Value;

            return false;
        }
    }
}
