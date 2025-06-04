using System;
using System.Collections;

using UnityEngine;
using UnityEngine.EventSystems;

using HarmonyLib;

using InControl;

using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Menus;
using BetterLegacy.Menus.UI.Interfaces;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(InterfaceController))]
    public class InterfaceControllerPatch : MonoBehaviour
    {
        [HarmonyPatch(nameof(InterfaceController.Start))]
        [HarmonyPrefix]
        static bool StartPrefix(InterfaceController __instance)
        {
            if (CoreHelper.InEditor)
                __instance.gameObject.SetActive(false);

            try
            {
                if (SceneHelper.CurrentSceneType != SceneType.Editor)
                {
                    var eventSystem = GameObject.Find("EventSystem").GetComponent<EventSystem>();

                    Destroy(eventSystem.GetComponent<InControlInputModule>());
                    Destroy(eventSystem.GetComponent<BaseInput>());

                    eventSystem.gameObject.GetOrAddComponent<StandaloneInputModule>();
                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Error was had with exception: {ex}");
            }

            foreach (var quickElement in QuickElementManager.AllQuickElements)
            {
                if (!__instance.quickElements.ContainsKey(quickElement.Key))
                    __instance.quickElements.Add(quickElement.Key, quickElement.Value);
            }

            DataManager.inst.UpdateSettingString("colon", ":");

            if (!DataManager.inst.HasKey("MasterVolume"))
                __instance.ResetAudioSettings();

            if (!DataManager.inst.HasKey("Resolution_i"))
                __instance.ResetVideoSettings();

            InputDataManager.inst.BindMenuKeys();
            __instance.MainPanel = __instance.transform.Find("Panel");

            InterfaceManager.inst.Clear();

            CoreHelper.Log($"Load On Start: {__instance.loadOnStart}");

            var scene = __instance.gameObject.scene;

            CoreHelper.Log($"Getting scene...");

            if (scene.name == SceneName.Main_Menu.ToName())
                InterfaceManager.inst.StartupInterface();
            
            if (scene.name == SceneName.Interface.ToName())
                InterfaceManager.inst.StartupStoryInterface();
            
            if (scene.name == SceneName.Input_Select.ToName())
                InputSelectMenu.Init();

            Destroy(__instance.gameObject);

            return false;
        }

        [HarmonyPatch(nameof(InterfaceController.LoadInterface), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool LoadInterfacePrefix() => false;

        [HarmonyPatch(nameof(InterfaceController.Update))]
        [HarmonyPrefix]
        static bool UpdatePrefix() => false;

        [HarmonyPatch(nameof(InterfaceController.handleEvent))]
        [HarmonyPrefix]
        static bool handleEventPrefix(ref IEnumerator __result)
        {
            __result = CoroutineHelper.DoAction(() => CoreHelper.Log($"Ran {nameof(InterfaceController.handleEvent)}"));
            return false;
        }

        [HarmonyPatch(nameof(InterfaceController.AddElement))]
        [HarmonyPrefix]
        static bool AddElementPrefix(ref IEnumerator __result)
        {
            __result = CoroutineHelper.DoAction(() => CoreHelper.Log($"Ran {nameof(InterfaceController.AddElement)}"));
            return false;
        }

        [HarmonyPatch(nameof(InterfaceController.ParseLilScript))]
        [HarmonyPrefix]
        static bool ParseLilScriptPrefix() => false;
    }
}
