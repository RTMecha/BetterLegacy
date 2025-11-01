using System;
using System.Collections.Generic;
using UnityEngine;

using HarmonyLib;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Companion;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Menus;
using BetterLegacy.Story;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(DataManager))]
    public class DataManagerPatch
    {
        static bool run;

        public static DataManager Instance { get => DataManager.inst; set => DataManager.inst = value; }

        [HarmonyPatch(nameof(DataManager.Start))]
        [HarmonyPostfix]
        static void StartPostfix(DataManager __instance)
        {
            // Initialize managers
            ModCompatibility.Init();
            ShapeManager.Init();
            ThemeManager.Init();
            UIManager.Init();
            QuickElementManager.Init();
            FontManager.Init();
            LevelManager.Init();
            PlayerManager.Init();
            StoryManager.Init();
            CompanionManager.Init();

            SoundManager.Init();

            try
            {
                RTCode.Init();
            }
            catch (Exception ex)
            {
                Debug.LogError($"RTCode Evaluator failed to initialize.\n{ex}");
            }

            AnimationManager.Init();
            RTVideoManager.Init();
            ModifiersManager.Init();
            AchievementManager.Init();
            CursorManager.Init();

            __instance.languagesToIndex.Add("japanese", 2);
            __instance.languagesToIndex.Add("thai", 3);
            __instance.languagesToIndex.Add("russian", 4);
            __instance.languagesToIndex.Add("pirate", 5);

            __instance.indexToLangauge.Add(2, "japanese");
            __instance.indexToLangauge.Add(3, "thai");
            __instance.indexToLangauge.Add(4, "russian");
            __instance.indexToLangauge.Add(5, "pirate");

            if (RTFile.FileExists(RTFile.ApplicationDirectory + $"settings/menu{FileFormat.LSS.Dot()}"))
            {
                string rawProfileJSON = RTFile.ReadFromFile(RTFile.ApplicationDirectory + $"settings/menu{FileFormat.LSS.Dot()}");

                var jn = JSON.Parse(rawProfileJSON);

                jn["MenuMusic"][0]["name"] = "shuffle";
                jn["MenuMusic"][0]["values"] = "menu";
                jn["MenuMusic"][0]["function_call"] = "apply_menu_music";

                jn["MenuMusic"][1]["name"] = "barrels";
                jn["MenuMusic"][1]["values"] = "barrels";
                jn["MenuMusic"][1]["function_call"] = "apply_menu_music";

                jn["MenuMusic"][2]["name"] = "nostalgia";
                jn["MenuMusic"][2]["values"] = "nostalgia";
                jn["MenuMusic"][2]["function_call"] = "apply_menu_music";

                jn["MenuMusic"][3]["name"] = "arcade dream";
                jn["MenuMusic"][3]["values"] = "arcade_dream";
                jn["MenuMusic"][3]["function_call"] = "apply_menu_music";

                jn["MenuMusic"][4]["name"] = "distance";
                jn["MenuMusic"][4]["values"] = "distance";
                jn["MenuMusic"][4]["function_call"] = "apply_menu_music";

                jn["ArcadeDifficulty"][0]["name"] = "zen (invincible)";
                jn["ArcadeDifficulty"][0]["values"] = "zen";

                jn["ArcadeDifficulty"][1]["name"] = "normal";
                jn["ArcadeDifficulty"][1]["values"] = "normal";

                jn["ArcadeDifficulty"][2]["name"] = "1 life";
                jn["ArcadeDifficulty"][2]["values"] = "1_life";

                jn["ArcadeDifficulty"][3]["name"] = "1 hit";
                jn["ArcadeDifficulty"][3]["values"] = "1_hit";

                jn["ArcadeGameSpeed"][0]["name"] = "x0.5";
                jn["ArcadeGameSpeed"][0]["values"] = "0.5";

                jn["ArcadeGameSpeed"][1]["name"] = "x0.8";
                jn["ArcadeGameSpeed"][1]["values"] = "0.8";

                jn["ArcadeGameSpeed"][2]["name"] = "x1.0";
                jn["ArcadeGameSpeed"][2]["values"] = "1.0";

                jn["ArcadeGameSpeed"][3]["name"] = "x1.2";
                jn["ArcadeGameSpeed"][3]["values"] = "1.2";

                jn["ArcadeGameSpeed"][4]["name"] = "x1.5";
                jn["ArcadeGameSpeed"][4]["values"] = "1.5";

                jn["QualityLevel"][0]["name"] = "Low";
                jn["QualityLevel"][0]["values"] = "low";

                jn["QualityLevel"][1]["name"] = "Normal";
                jn["QualityLevel"][1]["values"] = "normal";

                jn["AntiAliasing"][0]["name"] = "None";
                jn["AntiAliasing"][0]["values"] = "0";

                jn["AntiAliasing"][1]["name"] = "x2";
                jn["AntiAliasing"][1]["values"] = "2";

                jn["SortingHuman"][0]["values"]["desc"] = "NEW";
                jn["SortingHuman"][0]["values"]["asc"] = "OLD";

                jn["SortingHuman"][0]["name"] = "date_downloaded";

                jn["SortingHuman"][1]["values"]["desc"] = "Z-A";
                jn["SortingHuman"][1]["values"]["asc"] = "A-Z";

                jn["SortingHuman"][1]["name"] = "song_name";

                jn["SortingHuman"][2]["values"]["desc"] = "Z-A";
                jn["SortingHuman"][2]["values"]["asc"] = "A-Z";

                jn["SortingHuman"][2]["name"] = "artist_name";

                jn["SortingHuman"][3]["values"]["desc"] = "Z-A";
                jn["SortingHuman"][3]["values"]["asc"] = "A-Z";

                jn["SortingHuman"][3]["name"] = "creator_name";

                jn["SortingHuman"][4]["values"]["desc"] = "HARD";
                jn["SortingHuman"][4]["values"]["asc"] = "EASY";

                jn["SortingHuman"][4]["name"] = "difficulty";

                DataManager.inst.interfaceSettings = jn;
            }
            else
            {
                JSONNode jn = JSON.Parse("{}");
                jn["UITheme"][0]["name"] = "Light";
                jn["UITheme"][0]["values"]["bg"] = "#E0E0E0";
                jn["UITheme"][0]["values"]["text"] = "#212121";
                jn["UITheme"][0]["values"]["highlight"] = "#424242";
                jn["UITheme"][0]["values"]["text-highlight"] = "#E0E0E0";
                jn["UITheme"][0]["values"]["buttonbg"] = "transparent";
                jn["UITheme"][0]["function_call"] = "apply_ui_theme";

                jn["UITheme"][1]["name"] = "Dark";
                jn["UITheme"][1]["values"]["bg"] = "#212121";
                jn["UITheme"][1]["values"]["text"] = "#E0E0E0";
                jn["UITheme"][1]["values"]["highlight"] = "#E0E0E0";
                jn["UITheme"][1]["values"]["text-highlight"] = "#212121";
                jn["UITheme"][1]["values"]["buttonbg"] = "transparent";
                jn["UITheme"][1]["function_call"] = "apply_ui_theme";

                jn["UITheme"][2]["name"] = "Alpha";
                jn["UITheme"][2]["values"]["bg"] = "#1E1E1E";
                jn["UITheme"][2]["values"]["text"] = "#ECECEC";
                jn["UITheme"][2]["values"]["highlight"] = "#F2762A";
                jn["UITheme"][2]["values"]["text-highlight"] = "#ECECEC";
                jn["UITheme"][2]["values"]["buttonbg"] = "transparent";
                jn["UITheme"][2]["function_call"] = "apply_ui_theme";

                jn["UITheme"][3]["name"] = "Beta";
                jn["UITheme"][3]["values"]["bg"] = "#F2F2F2";
                jn["UITheme"][3]["values"]["text"] = "#333333";
                jn["UITheme"][3]["values"]["highlight"] = "#F05355";
                jn["UITheme"][3]["values"]["text-highlight"] = "#F2F2F2";
                jn["UITheme"][3]["values"]["buttonbg"] = "transparent";
                jn["UITheme"][3]["function_call"] = "apply_ui_theme";

                jn["UITheme"][4]["name"] = "Neir";
                jn["UITheme"][4]["values"]["bg"] = "#D1CDB7";
                jn["UITheme"][4]["values"]["text"] = "#454138";
                jn["UITheme"][4]["values"]["highlight"] = "#454138";
                jn["UITheme"][4]["values"]["text-highlight"] = "#D1CDB7";
                jn["UITheme"][4]["values"]["buttonbg"] = "transparent";
                jn["UITheme"][4]["function_call"] = "apply_ui_theme";

                RTFile.WriteToFile(RTFile.ApplicationDirectory + $"settings/menu{FileFormat.LSS.Dot()}", jn.ToString(3));
            }

            InterfaceManager.Init();

            Instance.PrefabTypes.Clear();

            if (run)
                return;

            run = true;

            var commandLineArgs = Environment.GetCommandLineArgs();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Length: {commandLineArgs.Length}");
            foreach (var line in commandLineArgs)
                sb.AppendLine(line);
            CoreHelper.Log($"Command line\n: {sb}");

            if (commandLineArgs.Length > 1 && !string.IsNullOrEmpty(commandLineArgs[1]))
            {
                var path = commandLineArgs[1];
                path = path.Remove(Level.LEVEL_LSB);
                path = path.Remove(Level.LEVEL_VGD);
                path = path.Remove(Level.METADATA_LSB);
                path = path.Remove(Level.PLAYERS_LSB);

                if (path.EndsWith(FileFormat.ASSET.Dot()))
                {
                    CoroutineHelper.StartCoroutine(StoryLevel.LoadFromAsset(path, LevelManager.Play));
                    return;
                }

                if (!Level.Verify(path))
                    return;

                if (!ModCompatibility.EditorOnStartupInstalled)
                    LevelManager.Play(new Level(path));
                else
                    LegacyPlugin.LevelStartupPath = path;
            }
        }

        [HarmonyPatch(nameof(DataManager.SaveMetadata), typeof(string), typeof(DataManager.MetaData))]
        [HarmonyPrefix]
        static bool SaveMetadataPrefix(ref LSError __result)
        {
            var result = new LSError(false, "");
            __result = result;
            return false;
        }

        [HarmonyPatch(nameof(DataManager.GeneratePrefabJSON))]
        [HarmonyPrefix]
        static bool GeneratePrefabJSONPrefix(ref JSONNode __result)
        {
            __result = Parser.NewJSONObject();
            return false;
        }

        #region PlayerPrefs Patches

        [HarmonyPatch(nameof(DataManager.HasKey))]
        [HarmonyPrefix]
        static bool HasKeyPrefix(ref bool __result, string __0)
        {
            __result = ModCompatibility.sharedValues.ContainsKey(Instance.settingPrefix + __0);
            return false;
        }

        [HarmonyPatch(nameof(DataManager.SettingHasKey))]
        [HarmonyPrefix]
        static bool SettingHasKeyPrefix(ref bool __result, string __0)
        {
            __result = ModCompatibility.sharedValues.ContainsKey(Instance.settingPrefix + __0);
            return false;
        }

        [HarmonyPatch(nameof(DataManager.UpdateSettingEnum))]
        [HarmonyPrefix]
        static bool UpdateSettingEnumPrefix(string __0, int __1)
        {
            ModCompatibility.sharedValues[Instance.settingPrefix + __0] = __1;
            return false;
        }

        [HarmonyPatch(nameof(DataManager.GetSettingEnum))]
        [HarmonyPrefix]
        static bool GetSettingEnumPrefix(ref int __result, string __0, int __1)
        {
            if (ModCompatibility.sharedValues.TryGetValue(Instance.settingPrefix + __0, out object settingEnum) && settingEnum is int num)
            {
                __result = num;
                return false;
            }

            __result = __1;
            return false;
        }

        [HarmonyPatch(nameof(DataManager.UpdateSettingString))]
        [HarmonyPrefix]
        static bool UpdateSettingStringPrefix(string __0, string __1)
        {
            ModCompatibility.sharedValues[Instance.settingPrefix + __0] = __1;
            return false;
        }

        [HarmonyPatch(nameof(DataManager.GetSettingString), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool GetSettingStringPrefix1(ref string __result, string __0)
        {
            if (ModCompatibility.sharedValues.TryGetValue(Instance.settingPrefix + __0, out object settingString) && settingString is string num)
            {
                __result = num;
                return false;
            }

            __result = "";
            return false;
        }

        [HarmonyPatch(nameof(DataManager.GetSettingString), new Type[] { typeof(string), typeof(string) })]
        [HarmonyPrefix]
        static bool GetSettingStringPrefix2(ref string __result, string __0, string __1)
        {
            if (ModCompatibility.sharedValues.TryGetValue(Instance.settingPrefix + __0, out object settingString) && settingString is string num)
            {
                __result = num;
                return false;
            }

            __result = __1;
            return false;
        }

        [HarmonyPatch(nameof(DataManager.UpdateSettingInt))]
        [HarmonyPrefix]
        static bool UpdateSettingIntPrefix(string __0, int __1)
        {
            ModCompatibility.sharedValues[Instance.settingPrefix + __0] = __1;

            return false;
        }

        [HarmonyPatch(nameof(DataManager.GetSettingInt), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool GetSettingIntPrefix1(ref int __result, string __0)
        {
            if (ModCompatibility.sharedValues.TryGetValue(Instance.settingPrefix + __0, out object settingInt) && settingInt is int num)
            {
                __result = num;
                return false;
            }

            __result = 0;
            return false;
        }

        [HarmonyPatch(nameof(DataManager.GetSettingInt), new Type[] { typeof(string), typeof(int) })]
        [HarmonyPrefix]
        static bool GetSettingIntPrefix2(ref int __result, string __0, int __1)
        {
            if (ModCompatibility.sharedValues.TryGetValue(Instance.settingPrefix + __0, out object settingInt) && settingInt is int num)
            {
                __result = num;
                return false;
            }

            __result = __1;
            return false;
        }

        [HarmonyPatch(nameof(DataManager.UpdateSettingFloat))]
        [HarmonyPrefix]
        static bool UpdateSettingFloatPrefix(string __0, float __1)
        {
            ModCompatibility.sharedValues[Instance.settingPrefix + __0] = __1;
            return false;
        }

        [HarmonyPatch(nameof(DataManager.GetSettingFloat), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool GetSettingFloatPrefix1(ref float __result, string __0)
        {
            if (ModCompatibility.sharedValues.TryGetValue(Instance.settingPrefix + __0, out object settingFloat) && settingFloat is float num)
            {
                __result = num;
                return false;
            }

            __result = 0f;
            return false;
        }

        [HarmonyPatch(nameof(DataManager.GetSettingFloat), new Type[] { typeof(string), typeof(float) })]
        [HarmonyPrefix]
        static bool GetSettingFloatPrefix2(ref float __result, string __0, float __1)
        {
            if (ModCompatibility.sharedValues.TryGetValue(Instance.settingPrefix + __0, out object settingFloat) && settingFloat is float num)
            {
                __result = num;
                return false;
            }

            __result = __1;
            return false;
        }

        [HarmonyPatch(nameof(DataManager.UpdateSettingBool))]
        [HarmonyPrefix]
        static bool UpdateSettingBoolPrefix(string __0, bool __1)
        {
            ModCompatibility.sharedValues[Instance.settingPrefix + __0] = __1;

            return false;
        }

        [HarmonyPatch(nameof(DataManager.GetSettingBool), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool GetSettingBoolPrefix1(ref bool __result, string __0)
        {
            if (ModCompatibility.sharedValues.TryGetValue(Instance.settingPrefix + __0, out object settingBool) && settingBool is bool num)
            {
                __result = num;
                return false;
            }

            __result = false;
            return false;
        }

        [HarmonyPatch(nameof(DataManager.GetSettingBool), new Type[] { typeof(string), typeof(bool) })]
        [HarmonyPrefix]
        static bool GetSettingBoolPrefix2(ref bool __result, string __0, bool __1)
        {
            if (ModCompatibility.sharedValues.TryGetValue(Instance.settingPrefix + __0, out object settingBool) && settingBool is bool num)
            {
                __result = num;
                return false;
            }

            __result = __1;
            return false;
        }

        [HarmonyPatch(nameof(DataManager.UpdateSettingVector2D))]
        [HarmonyPrefix]
        static bool UpdateSettingVector2DPrefix(string __0, int __1, Vector2[] __2)
        {
            ModCompatibility.sharedValues[Instance.settingPrefix + __0 + "_i"] = __1;
            ModCompatibility.sharedValues[Instance.settingPrefix + __0 + "_x"] = __2[__1].x;
            ModCompatibility.sharedValues[Instance.settingPrefix + __0 + "_y"] = __2[__1].y;
            return false;
        }

        [HarmonyPatch(nameof(DataManager.GetSettingVector2D))]
        [HarmonyPrefix]
        static bool GetSettingVector2DPrefix(ref Vector2 __result, string __0)
        {
            if (ModCompatibility.sharedValues.TryGetValue(Instance.settingPrefix + __0 + "_x", out object xSetting) && xSetting is float x &&
                ModCompatibility.sharedValues.TryGetValue(Instance.settingPrefix + __0 + "_y", out object ySetting) && ySetting is float y)
            {
                __result = new Vector2(x, y);
                return false;
            }

            __result = Vector2.zero;
            return false;
        }

        [HarmonyPatch(nameof(DataManager.GetSettingVector2DIndex))]
        [HarmonyPrefix]
        static bool GetSettingVector2DIndexPrefix(ref int __result, string __0)
        {
            if (ModCompatibility.sharedValues.TryGetValue(Instance.settingPrefix + __0 + "_i", out object vector2IndexSetting) && vector2IndexSetting is int num)
            {
                __result = num;
                return false;
            }

            __result = 0;
            return false;
        }

        #endregion
    }

    [HarmonyPatch(typeof(DataManager.GameData))]
    public class DataManagerGameDataPatch : MonoBehaviour
    {
        [HarmonyPatch(nameof(DataManager.GameData.ParseBeatmap))]
        [HarmonyPrefix]
        static bool ParseBeatmapPrefix() => false;
    }
}
