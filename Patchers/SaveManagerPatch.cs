using BetterLegacy.Configs;
using BetterLegacy.Core;
using HarmonyLib;
using SimpleJSON;
using UnityEngine;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(SaveManager))]
    public class SaveManagerPatch : MonoBehaviour
    {
        [HarmonyPatch("ApplySettingsFile")]
        [HarmonyPostfix]
        static void ApplySettingsFilePostfix()
        {
            CoreConfig.Instance.prevFullscreen = CoreConfig.Instance.Fullscreen.Value;
            //CoreConfig.Instance.Fullscreen.Value = DataManager.inst.GetSettingBool("FullScreen", false);

            CoreConfig.Instance.prevResolution = CoreConfig.Instance.Resolution.Value;
            //CoreConfig.Instance.Resolution.Value = (Resolutions)DataManager.inst.GetSettingInt("Resolution_i", 5);

            CoreConfig.Instance.prevMasterVol = CoreConfig.Instance.MasterVol.Value;
            //CoreConfig.Instance.MasterVol.Value = DataManager.inst.GetSettingInt("MasterVolume", 9);

            CoreConfig.Instance.prevMusicVol = CoreConfig.Instance.MusicVol.Value;
            //CoreConfig.Instance.MusicVol.Value = DataManager.inst.GetSettingInt("MusicVolume", 9);

            CoreConfig.Instance.prevSFXVol = CoreConfig.Instance.SFXVol.Value;
            //CoreConfig.Instance.SFXVol.Value = DataManager.inst.GetSettingInt("EffectsVolume", 9);

            CoreConfig.Instance.prevLanguage = CoreConfig.Instance.Language.Value;
            //CoreConfig.Instance.Language.Value = (Language)DataManager.inst.GetSettingInt("Language_i", 0);

            CoreConfig.Instance.prevControllerRumble = CoreConfig.Instance.ControllerRumble.Value;
            //CoreConfig.Instance.ControllerRumble.Value = DataManager.inst.GetSettingBool("ControllerVibrate", true);

            DataManager.inst.UpdateSettingBool("FullScreen", CoreConfig.Instance.Fullscreen.Value);
            DataManager.inst.UpdateSettingInt("Resolution_i", (int)CoreConfig.Instance.Resolution.Value);
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

        [HarmonyPatch("OnApplicationQuit")]
        [HarmonyPrefix]
        static bool OnApplicationQuitPrefix()
        {
            DiscordController.inst.OnDisableDiscord();
            Debug.Log("Run Quit Function");
            PlayerPrefs.DeleteAll();
            return false;
        }

        [HarmonyPatch("LoadSavesFile")]
        [HarmonyPrefix]
        static bool LoadSavesFilePrefix() => false;
    }
}
