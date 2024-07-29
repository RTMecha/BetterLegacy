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
        [HarmonyPatch(nameof(SystemManager.Awake))]
        [HarmonyPostfix]
        static void AwakePostfix()
        {
            Debug.unityLogger.logEnabled = CoreConfig.Instance.DebugsOn.Value;

            if (!ExampleManager.inst) // Don't call Init due to SystemManager.inst.Awake() being called every time a scene is loaded. This prevents Example from saying he's already here.
                ExampleManager.Init();
        }

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
