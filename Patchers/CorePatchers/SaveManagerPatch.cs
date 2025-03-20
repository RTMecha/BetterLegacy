using UnityEngine;

using HarmonyLib;

using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(SaveManager))]
    public class SaveManagerPatch : MonoBehaviour
    {
        [HarmonyPatch(nameof(SaveManager.ApplySettingsFile))]
        [HarmonyPostfix]
        static void ApplySettingsFilePostfix()
        {
            CoreConfig.Instance.prevFullscreen = CoreConfig.Instance.Fullscreen.Value;

            CoreConfig.Instance.prevResolution = CoreConfig.Instance.Resolution.Value;

            CoreConfig.Instance.prevMasterVol = CoreConfig.Instance.MasterVol.Value;

            CoreConfig.Instance.prevMusicVol = CoreConfig.Instance.MusicVol.Value;

            CoreConfig.Instance.prevSFXVol = CoreConfig.Instance.SFXVol.Value;

            CoreConfig.Instance.prevLanguage = CoreConfig.Instance.Language.Value;

            CoreConfig.Instance.prevControllerRumble = CoreConfig.Instance.ControllerRumble.Value;

            DataManager.inst.UpdateSettingBool("FullScreen", CoreConfig.Instance.Fullscreen.Value);
            DataManager.inst.UpdateSettingInt("Resolution_i", (int)CoreConfig.Instance.Resolution.Value);
            DataManager.inst.UpdateSettingInt("UITheme", MenuConfig.Instance.Theme.Value);
            DataManager.inst.UpdateSettingInt("MasterVolume", CoreConfig.Instance.MasterVol.Value);
            DataManager.inst.UpdateSettingInt("MusicVolume", CoreConfig.Instance.MusicVol.Value);
            DataManager.inst.UpdateSettingInt("EffectsVolume", CoreConfig.Instance.SFXVol.Value);
            DataManager.inst.UpdateSettingInt("Language_i", (int)CoreConfig.Instance.Language.Value);
            DataManager.inst.UpdateSettingBool("ControllerVibrate", CoreConfig.Instance.ControllerRumble.Value);

            if (RTFile.FileExists(RTFile.ApplicationDirectory + "settings/functions.lss"))
            {
                string rawProfileJSON = RTFile.ReadFromFile(RTFile.ApplicationDirectory + "settings/functions.lss");

                var jn = JSON.Parse(rawProfileJSON);

                if (string.IsNullOrEmpty(jn["general"]["updated_speed"]))
                {
                    jn["general"]["updated_speed"] = "True";
                    DataManager.inst.UpdateSettingEnum("ArcadeGameSpeed", DataManager.inst.GetSettingEnum("ArcadeGameSpeed", 2) + 1);

                    RTFile.WriteToFile(RTFile.ApplicationDirectory + "settings/functions.lss", jn.ToString(3));
                }
            }
            else
            {
                var jn = JSON.Parse("{}");

                jn["general"]["updated_speed"] = "True";
                DataManager.inst.UpdateSettingEnum("ArcadeGameSpeed", DataManager.inst.GetSettingEnum("ArcadeGameSpeed", 2) + 1);

                RTFile.WriteToFile(RTFile.ApplicationDirectory + "settings/functions.lss", jn.ToString(3));
            }
        }

        [HarmonyPatch(nameof(SaveManager.ApplyVideoSettings))]
        [HarmonyPrefix]
        static bool ApplyVideoSettingsPrefix()
        {
            var resolution = CoreHelper.CurrentResolution;
            Screen.SetResolution(resolution.x, resolution.y, CoreConfig.Instance.Fullscreen.Value);
            
            QualitySettings.vSyncCount = CoreConfig.Instance.VSync.Value ? 1 : 0;
            QualitySettings.antiAliasing = DataManager.inst.GetSettingEnum("AntiAliasing", 0);
            Application.targetFrameRate = CoreConfig.Instance.FPSLimit.Value;
            CoreHelper.Log($"Apply Video Settings\nResolution: [{Screen.currentResolution}]\nFullscreen: [{Screen.fullScreen}]\nVSync Count: [{QualitySettings.vSyncCount}]");

            return false;
        }

        [HarmonyPatch(nameof(SaveManager.OnApplicationQuit))]
        [HarmonyPrefix]
        static bool OnApplicationQuitPrefix()
        {
            DiscordController.inst.OnDisableDiscord();
            Debug.Log("Run Quit Function");
            PlayerPrefs.DeleteAll();
            return false;
        }

        [HarmonyPatch(nameof(SaveManager.LoadSavesFile))]
        [HarmonyPrefix]
        static bool LoadSavesFilePrefix() => false;
    }
}
