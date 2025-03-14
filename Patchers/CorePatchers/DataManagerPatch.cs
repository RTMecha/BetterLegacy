using BetterLegacy.Companion;
using BetterLegacy.Companion.Entity;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Menus;
using BetterLegacy.Story;
using HarmonyLib;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BaseBeatmapTheme = DataManager.BeatmapTheme;
using BasePrefab = DataManager.GameData.Prefab;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(DataManager))]
    public class DataManagerPatch
    {
        public static DataManager Instance { get => DataManager.inst; set => DataManager.inst = value; }

        [HarmonyPatch(nameof(DataManager.Start))]
        [HarmonyPostfix]
        static void StartPostfix(DataManager __instance)
        {
            LegacyPlugin.ParseProfile();

            // Initialize managers
            ModCompatibility.Init();
            ShapeManager.Init();
            UIManager.Init();
            QuickElementManager.Init();
            FontManager.Init();
            AssetManager.Init();
            LevelManager.Init();
            PlayerManager.Init();
            StoryManager.Init();
            CompanionManager.Init();

            AudioManager.inst.gameObject.AddComponent<SoundManager>();

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

            __instance.difficulties = new List<DataManager.Difficulty>
            {
                new DataManager.Difficulty("Easy", LSColors.GetThemeColor("easy")),
                new DataManager.Difficulty("Normal", LSColors.GetThemeColor("normal")),
                new DataManager.Difficulty("Hard", LSColors.GetThemeColor("hard")),
                new DataManager.Difficulty("Expert", LSColors.GetThemeColor("expert")),
                new DataManager.Difficulty("Expert+", LSColors.GetThemeColor("expert+")),
                new DataManager.Difficulty("Master", new Color(0.25f, 0.01f, 0.01f)),
                new DataManager.Difficulty("Animation", LSColors.GetThemeColor("none"))
            };

            __instance.linkTypes = new List<DataManager.LinkType>
            {
                new DataManager.LinkType("Spotify", "https://open.spotify.com/artist/{0}"),
                new DataManager.LinkType("SoundCloud", "https://soundcloud.com/{0}"),
                new DataManager.LinkType("Bandcamp", "https://{0}.bandcamp.com"),
                new DataManager.LinkType("YouTube", "https://www.youtube.com/c/{0}"),
                new DataManager.LinkType("Newgrounds", "https://{0}.newgrounds.com/")
            };

            try
            {
                var sayings = JSON.Parse(RTFile.ReadFromFile(RTFile.FileExists($"{RTFile.ApplicationDirectory}profile/sayings{FileFormat.JSON.Dot()}") ? $"{RTFile.ApplicationDirectory}profile/sayings{FileFormat.JSON.Dot()}" : $"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}sayings{FileFormat.JSON.Dot()}"))["sayings"];

                if (sayings != null)
                    __instance.levelRanks = new List<DataManager.LevelRank>
                    {
                        new DataManager.LevelRank("-", LSColors.GetThemeColor("none"), -1, -1, sayings["null"].Children.Select(x => x.Value).ToArray()),
                        new DataManager.LevelRank("SS", LSColors.GetThemeColor("easy"), 0, 0, sayings["ss"].Children.Select(x => x.Value).ToArray()),
                        new DataManager.LevelRank("S", LSColors.GetThemeColor("normal"), 1, 1, sayings["s"].Children.Select(x => x.Value).ToArray()),
                        new DataManager.LevelRank("A", LSColors.GetThemeColor("normal"), 2, 3, sayings["a"].Children.Select(x => x.Value).ToArray()),
                        new DataManager.LevelRank("B", LSColors.GetThemeColor("hard"), 4, 6, sayings["b"].Children.Select(x => x.Value).ToArray()),
                        new DataManager.LevelRank("C", LSColors.GetThemeColor("hard"), 7, 9, sayings["c"].Children.Select(x => x.Value).ToArray()),
                        new DataManager.LevelRank("D", LSColors.GetThemeColor("expert"), 10, 15, sayings["d"].Children.Select(x => x.Value).ToArray()),
                        new DataManager.LevelRank("F", LSColors.GetThemeColor("expert"), 16, int.MaxValue, sayings["f"].Children.Select(x => x.Value).ToArray())
                    };
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Failed to set LevelRank sayings.\nException: {ex}");
            }

            //Themes
            __instance.BeatmapThemes[0].name = "PA Machine";
            __instance.BeatmapThemes[1].name = "PA Anarchy";
            __instance.BeatmapThemes[2].name = "PA Day Night";
            __instance.BeatmapThemes[3].name = "PA Donuts";
            __instance.BeatmapThemes[4].name = "PA Classic";
            __instance.BeatmapThemes[5].name = "PA New";
            __instance.BeatmapThemes[6].name = "PA Dark";
            __instance.BeatmapThemes[7].name = "PA White On Black";
            __instance.BeatmapThemes[8].name = "PA Black On White";

            __instance.BeatmapThemes.Add(new BaseBeatmapTheme
            {
                name = "PA Example Theme",
                id = "9",
                backgroundColor = LSColors.HexToColor("212121"),
                guiColor = LSColors.HexToColorAlpha("504040"),
                playerColors = new List<Color>
                {
                    LSColors.HexToColorAlpha("E57373FF"),
                    LSColors.HexToColorAlpha("64B5F6FF"),
                    LSColors.HexToColorAlpha("81C784FF"),
                    LSColors.HexToColorAlpha("FFB74DFF")
                },
                objectColors = new List<Color>
                {
                    LSColors.HexToColorAlpha("3F59FCFF"),
                    LSColors.HexToColorAlpha("3AD4F5FF"),
                    LSColors.HexToColorAlpha("E91E63FF"),
                    LSColors.HexToColorAlpha("E91E63FF"),
                    LSColors.HexToColorAlpha("E91E63FF"),
                    LSColors.HexToColorAlpha("E91E63FF"),
                    LSColors.HexToColorAlpha("E91E6345"),
                    LSColors.HexToColorAlpha("FFFFFFFF"),
                    LSColors.HexToColorAlpha("000000FF")
                },
                backgroundColors = new List<Color>
                {
                    LSColors.HexToColor("212121"),
                    LSColors.HexToColor("E91E63"),
                    LSColors.HexToColor("E91E63"),
                    LSColors.HexToColor("E91E63"),
                    LSColors.HexToColor("E91E63"),
                    LSColors.HexToColor("E91E63"),
                    LSColors.HexToColor("E91E63"),
                    LSColors.HexToColor("E91E63"),
                    LSColors.HexToColor("E91E63")
                }
            });

            for (int i = 0; i < __instance.BeatmapThemes.Count; i++)
            {
                var beatmapTheme = __instance.BeatmapThemes[i];

                if (beatmapTheme.objectColors.Count < 18)
                    while (beatmapTheme.objectColors.Count < 18)
                    {
                        beatmapTheme.objectColors.Add(beatmapTheme.objectColors[beatmapTheme.objectColors.Count - 1]);
                    }
                if (beatmapTheme.backgroundColors.Count < 9)
                    while (beatmapTheme.backgroundColors.Count < 9)
                    {
                        beatmapTheme.backgroundColors.Add(beatmapTheme.backgroundColors[beatmapTheme.backgroundColors.Count - 1]);
                    }

                beatmapTheme.backgroundColor = LSColors.fadeColor(beatmapTheme.backgroundColor, 1f);

                for (int j = 0; j < beatmapTheme.backgroundColors.Count; j++)
                    beatmapTheme.backgroundColors[j] = LSColors.fadeColor(beatmapTheme.backgroundColors[j], 1f);

                __instance.BeatmapThemes[i] = new BeatmapTheme
                {
                    id = beatmapTheme.id,
                    name = beatmapTheme.name,
                    expanded = beatmapTheme.expanded,
                    backgroundColor = beatmapTheme.backgroundColor,
                    guiAccentColor = beatmapTheme.guiColor,
                    guiColor = beatmapTheme.guiColor,
                    playerColors = beatmapTheme.playerColors,
                    objectColors = beatmapTheme.objectColors,
                    backgroundColors = beatmapTheme.backgroundColors,
                    effectColors = beatmapTheme.objectColors.Clone(),
                    isDefault = true,
                };
            }

            __instance.UpdateSettingString("versionNumber", ProjectArrhythmia.GAME_VERSION);
            __instance.UpdateSettingBool("CanEdit", true);

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
            MenuManager.Init();

            try
            {
                var path = Example.GetFile($"pa_example_m{FileFormat.LSP.Dot()}");
                if (RTFile.FileExists(path))
                    LegacyPlugin.ExamplePrefab = Prefab.Parse(JSON.Parse(RTFile.ReadFromFile(path)));
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Failed to parse Example prefab.\n{ex}");
            } // Example Prefab

            Instance.PrefabTypes.Clear();
        }

        [HarmonyPatch(nameof(DataManager.SaveMetadata), typeof(string), typeof(DataManager.MetaData))]
        [HarmonyPrefix]
        static bool SaveMetadataPrefix(ref LSError __result, DataManager __instance, string __0, DataManager.MetaData __1)
        {
            var result = new LSError(false, "");
            JSONNode jn;
            try
            {
                    jn = ((MetaData)__1).ToJSON();

                    Debug.Log($"{__instance.className}Saving Metadata Full");
                    RTFile.WriteToFile(__0, jn.ToString());
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            __result = result;

            return false;
        }

        [HarmonyPatch(nameof(DataManager.GeneratePrefabJSON))]
        [HarmonyPrefix]
        static bool GeneratePrefabJSONPrefix(ref JSONNode __result, BasePrefab __0)
        {
            __result = JSON.Parse("{}");
            return false;
        }

        #region PlayerPrefs Patches

        [HarmonyPatch(nameof(DataManager.HasKey))]
        [HarmonyPrefix]
        static bool HasKeyPrefix(ref bool __result, string __0)
        {
            __result = ModCompatibility.sharedFunctions.ContainsKey(Instance.settingPrefix + __0);
            return false;
        }

        [HarmonyPatch(nameof(DataManager.SettingHasKey))]
        [HarmonyPrefix]
        static bool SettingHasKeyPrefix(ref bool __result, string __0)
        {
            __result = ModCompatibility.sharedFunctions.ContainsKey(Instance.settingPrefix + __0);
            return false;
        }

        [HarmonyPatch(nameof(DataManager.UpdateSettingEnum))]
        [HarmonyPrefix]
        static bool UpdateSettingEnumPrefix(string __0, int __1)
        {
            ModCompatibility.sharedFunctions[Instance.settingPrefix + __0] = __1;
            return false;
        }

        [HarmonyPatch(nameof(DataManager.GetSettingEnum))]
        [HarmonyPrefix]
        static bool GetSettingEnumPrefix(ref int __result, string __0, int __1)
        {
            if (ModCompatibility.sharedFunctions.TryGetValue(Instance.settingPrefix + __0, out object settingEnum) && settingEnum is int num)
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
            ModCompatibility.sharedFunctions[Instance.settingPrefix + __0] = __1;
            return false;
        }

        [HarmonyPatch(nameof(DataManager.GetSettingString), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool GetSettingStringPrefix1(ref string __result, string __0)
        {
            if (ModCompatibility.sharedFunctions.TryGetValue(Instance.settingPrefix + __0, out object settingString) && settingString is string num)
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
            if (ModCompatibility.sharedFunctions.TryGetValue(Instance.settingPrefix + __0, out object settingString) && settingString is string num)
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
            ModCompatibility.sharedFunctions[Instance.settingPrefix + __0] = __1;

            return false;
        }

        [HarmonyPatch(nameof(DataManager.GetSettingInt), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool GetSettingIntPrefix1(ref int __result, string __0)
        {
            if (ModCompatibility.sharedFunctions.TryGetValue(Instance.settingPrefix + __0, out object settingInt) && settingInt is int num)
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
            if (ModCompatibility.sharedFunctions.TryGetValue(Instance.settingPrefix + __0, out object settingInt) && settingInt is int num)
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
            ModCompatibility.sharedFunctions[Instance.settingPrefix + __0] = __1;
            return false;
        }

        [HarmonyPatch(nameof(DataManager.GetSettingFloat), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool GetSettingFloatPrefix1(ref float __result, string __0)
        {
            if (ModCompatibility.sharedFunctions.TryGetValue(Instance.settingPrefix + __0, out object settingFloat) && settingFloat is float num)
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
            if (ModCompatibility.sharedFunctions.TryGetValue(Instance.settingPrefix + __0, out object settingFloat) && settingFloat is float num)
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
            ModCompatibility.sharedFunctions[Instance.settingPrefix + __0] = __1;

            return false;
        }

        [HarmonyPatch(nameof(DataManager.GetSettingBool), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool GetSettingBoolPrefix1(ref bool __result, string __0)
        {
            if (ModCompatibility.sharedFunctions.TryGetValue(Instance.settingPrefix + __0, out object settingBool) && settingBool is bool num)
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
            if (ModCompatibility.sharedFunctions.TryGetValue(Instance.settingPrefix + __0, out object settingBool) && settingBool is bool num)
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
            ModCompatibility.sharedFunctions[Instance.settingPrefix + __0 + "_i"] = __1;
            ModCompatibility.sharedFunctions[Instance.settingPrefix + __0 + "_x"] = __2[__1].x;
            ModCompatibility.sharedFunctions[Instance.settingPrefix + __0 + "_y"] = __2[__1].y;
            return false;
        }

        [HarmonyPatch(nameof(DataManager.GetSettingVector2D))]
        [HarmonyPrefix]
        static bool GetSettingVector2DPrefix(ref Vector2 __result, string __0)
        {
            if (ModCompatibility.sharedFunctions.TryGetValue(Instance.settingPrefix + __0 + "_x", out object xSetting) && xSetting is float x &&
                ModCompatibility.sharedFunctions.TryGetValue(Instance.settingPrefix + __0 + "_y", out object ySetting) && ySetting is float y)
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
            if (ModCompatibility.sharedFunctions.TryGetValue(Instance.settingPrefix + __0 + "_i", out object vector2IndexSetting) && vector2IndexSetting is int num)
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
