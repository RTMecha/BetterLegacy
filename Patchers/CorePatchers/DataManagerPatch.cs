using BepInEx.Configuration;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
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
            AlephNetworkManager.Init();
            ModCompatibility.Init();
            ShapeManager.Init();
            UIManager.Init();
            QuickElementManager.Init();
            FontManager.Init();
            AssetManager.Init();
            LevelManager.Init();
            PlayerManager.Init();
            StoryManager.Init();

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


            __instance.levelRanks = new List<DataManager.LevelRank>
            {
                new DataManager.LevelRank("-", LSColors.GetThemeColor("none"), -1, -1, new string[] { "Maybe don't play in practice mode next time Hal?", "Nice practice! Let's go for real next time!", "Sometimes it's nice to just sit back and watch the pretty colors.", "Sometimes the patterns made by the virus seem too well orchestrated... almost like they're designed by a higher being?" }),
                new DataManager.LevelRank("SS", LSColors.GetThemeColor("easy"), 0, 0, new string[] { "That was incredible! Where did you learn all that? Oh right me {{QuickElement=smug}}", "WOW!! {{QuickElement=party}} Amazing job Hal!", "You're killin' it Hal! That virus is going down! {{QuickElement=surprise}}", "Didn't know you had moves like that programmed into you! {{QuickElement=surprise}}" }),
                new DataManager.LevelRank("S", LSColors.GetThemeColor("normal"), 1, 1, new string[] { "WOW!! {{QuickElement=party}} Amazing job Hal!", "You're killin' it Hal! That virus is going down! {{QuickElement=surprise}}", "Didn't know you had moves like that programmed into you! {{QuickElement=surprise}}" }),
                new DataManager.LevelRank("A", LSColors.GetThemeColor("normal"), 2, 3, new string[] { "Good job Hal! We might really be able to figure out that cure soon. {{QuickElement=happy}}", "Good job Hal! This new batch of nanobots is really something! {{QuickElement=happy}}", "Good! Now on to the next one! {{QuickElement=happy}}" }),
                new DataManager.LevelRank("B", LSColors.GetThemeColor("hard"), 4, 6, new string[] { "Hal you got to do better! Let's try to fail in less generations so we can move quicker towards that cure! {{QuickElement=nervous}}", "What do you think Hal? Should we move forward with iteration 23,647,647?" }),
                new DataManager.LevelRank("C", LSColors.GetThemeColor("hard"), 7, 9, new string[] { "Hal you got to do better! Let's try to fail in less generations so we can move quicker towards that cure! {{QuickElement=nervous}}", "What do you think Hal? Should we move forward with iteration 23,647,647?" }),
                new DataManager.LevelRank("D", LSColors.GetThemeColor("expert"), 10, 15, new string[] { "The next nanobot iteration will be better! Let's make sure of it! {{QuickElement=happy}}", "Maybe we should give those cyan nanobots another chance? {{QuickElement=nervous}}" }),
                new DataManager.LevelRank("F", LSColors.GetThemeColor("expert"), 16, int.MaxValue, new string[] { "The next nanobot iteration will be better! Let's make sure of it! {{QuickElement=happy}}", "Maybe we should give those cyan nanobots another chance? {{QuickElement=nervous}}" })
            };

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
                };
            }

            __instance.UpdateSettingString("versionNumber", "4.1.16");
            __instance.UpdateSettingBool("CanEdit", true);

            if (RTFile.FileExists(RTFile.ApplicationDirectory + "settings/menu.lss"))
            {
                string rawProfileJSON = FileManager.inst.LoadJSONFile("settings/menu.lss");

                JSONNode jn = JSON.Parse(rawProfileJSON);
                string note = "JSON code based on what exists in the files";
                if (note.Contains("note"))
                {
                }

                jn["TransRights"][0]["name"] = "<sprite name=trans_pa_logo> Yes";
                jn["TransRights"][0]["values"] = "<sprite name=trans_pa_logo>";
                jn["TransRights"][1]["name"] = "<sprite name=pa_logo> No";
                jn["TransRights"][1]["values"] = "<sprite name=pa_logo>";

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

                RTFile.WriteToFile("settings/menu.lss", jn.ToString(3));
            }

            InterfaceManager.Init();
            MenuManager.Init();

            try
            {
                var path = $"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}Example Parts/pa_example_m.lsp";
                if (RTFile.FileExists(path))
                {
                    LegacyPlugin.ExamplePrefab = Prefab.Parse(JSON.Parse(RTFile.ReadFromFile(path)));
                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Failed to parse Example prefab.\n{ex}");
            } // Example Prefab

            try
            {
                for (int i = 0; i < Instance.PrefabTypes.Count; i++)
                {
                    var p = Instance.PrefabTypes[i];
                    var prefabType = new PrefabType(p.Name, p.Color);
                    Instance.PrefabTypes[i] = prefabType;
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.ToString());
            }
        }

        [HarmonyPatch(nameof(DataManager.SaveData), typeof(string), typeof(DataManager.GameData))]
        [HarmonyPrefix]
        static bool SaveDataPrefix(DataManager __instance, ref IEnumerator __result, string __0, DataManager.GameData __1)
        {
            Debug.Log($"{__instance.className}GameData is modded: {__1 is Core.Data.GameData}");
            __result = ProjectData.Writer.SaveData(__0, (Core.Data.GameData)__1);
            return false;
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
            __result = ((Prefab)__0).ToJSON();
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

    [HarmonyPatch(typeof(BaseBeatmapTheme))]
    public class DataManagerBeatmapThemePatch
    {
        [HarmonyPatch(nameof(BaseBeatmapTheme.Lerp))]
        [HarmonyPrefix]
        static bool LerpPrefix(BaseBeatmapTheme __instance, ref BaseBeatmapTheme _start, ref BaseBeatmapTheme _end, float _val)
        {
            __instance.guiColor = Color.Lerp(_start.guiColor, _end.guiColor, _val);
            __instance.backgroundColor = Color.Lerp(_start.backgroundColor, _end.backgroundColor, _val);
            for (int i = 0; i < 4; i++)
            {
                if (_start.playerColors[i] != null && _end.playerColors[i] != null)
                {
                    __instance.playerColors[i] = Color.Lerp(_start.GetPlayerColor(i), _end.GetPlayerColor(i), _val);
                }
            }

            int maxObj = 9;
            if (_start.objectColors.Count > 9 && _end.objectColors.Count > 9)
            {
                maxObj = 18;
            }

            for (int j = 0; j < maxObj; j++)
            {
                if (_start.objectColors[j] != null && _end.objectColors[j] != null)
                {
                    __instance.objectColors[j] = Color.Lerp(_start.GetObjColor(j), _end.GetObjColor(j), _val);
                }
            }
            for (int k = 0; k < 9; k++)
            {
                if (_start.backgroundColors[k] != null && _end.backgroundColors[k] != null)
                {
                    __instance.backgroundColors[k] = Color.Lerp(_start.GetBGColor(k), _end.GetBGColor(k), _val);
                }
            }
            return false;
        }

        [HarmonyPatch(nameof(BaseBeatmapTheme.Parse))]
        [HarmonyPrefix]
        static bool ParsePrefix(BaseBeatmapTheme __instance, ref BaseBeatmapTheme __result, JSONNode __0, bool __1)
        {
            var beatmapTheme = new BaseBeatmapTheme();
            beatmapTheme.id = DataManager.inst.AllThemes.Count().ToString();
            if (__0["id"] != null)
                beatmapTheme.id = __0["id"];
            beatmapTheme.name = "name your themes!";
            if (__0["name"] != null)
                beatmapTheme.name = __0["name"];
            beatmapTheme.guiColor = LSColors.gray800;
            if (__0["gui"] != null)
                beatmapTheme.guiColor = LSColors.HexToColorAlpha(__0["gui"]);
            beatmapTheme.backgroundColor = LSColors.gray100;
            if (__0["bg"] != null)
                beatmapTheme.backgroundColor = LSColors.HexToColor(__0["bg"]);
            if (__0["players"] == null)
            {
                beatmapTheme.playerColors.Add(LSColors.HexToColorAlpha("E57373FF"));
                beatmapTheme.playerColors.Add(LSColors.HexToColorAlpha("64B5F6FF"));
                beatmapTheme.playerColors.Add(LSColors.HexToColorAlpha("81C784FF"));
                beatmapTheme.playerColors.Add(LSColors.HexToColorAlpha("FFB74DFF"));
            }
            else
            {
                int num = 0;
                foreach (KeyValuePair<string, JSONNode> keyValuePair in __0["players"].AsArray)
                {
                    JSONNode hex = keyValuePair;
                    if (num < 4)
                    {
                        if (hex != null)
                        {
                            beatmapTheme.playerColors.Add(LSColors.HexToColorAlpha(hex));
                        }
                        else
                            beatmapTheme.playerColors.Add(LSColors.pink500);
                        ++num;
                    }
                    else
                        break;
                }
                while (beatmapTheme.playerColors.Count < 4)
                    beatmapTheme.playerColors.Add(LSColors.pink500);
            }
            if (__0["objs"] == null)
            {
                beatmapTheme.objectColors.Add(LSColors.pink100);
                beatmapTheme.objectColors.Add(LSColors.pink200);
                beatmapTheme.objectColors.Add(LSColors.pink300);
                beatmapTheme.objectColors.Add(LSColors.pink400);
                beatmapTheme.objectColors.Add(LSColors.pink500);
                beatmapTheme.objectColors.Add(LSColors.pink600);
                beatmapTheme.objectColors.Add(LSColors.pink700);
                beatmapTheme.objectColors.Add(LSColors.pink800);
                beatmapTheme.objectColors.Add(LSColors.pink900);
                beatmapTheme.objectColors.Add(LSColors.pink100);
                beatmapTheme.objectColors.Add(LSColors.pink200);
                beatmapTheme.objectColors.Add(LSColors.pink300);
                beatmapTheme.objectColors.Add(LSColors.pink400);
                beatmapTheme.objectColors.Add(LSColors.pink500);
                beatmapTheme.objectColors.Add(LSColors.pink600);
                beatmapTheme.objectColors.Add(LSColors.pink700);
                beatmapTheme.objectColors.Add(LSColors.pink800);
                beatmapTheme.objectColors.Add(LSColors.pink900);
            }
            else
            {
                int num = 0;
                Color color = LSColors.pink500;
                foreach (KeyValuePair<string, JSONNode> keyValuePair in __0["objs"].AsArray)
                {
                    JSONNode hex = keyValuePair;
                    if (num < 18)
                    {
                        if (hex != null)
                        {
                            beatmapTheme.objectColors.Add(LSColors.HexToColorAlpha(hex));
                            color = LSColors.HexToColorAlpha(hex);
                        }
                        else
                        {
                            CoreHelper.LogError($"Some kind of object error at {num} in {beatmapTheme.name}.");
                            beatmapTheme.objectColors.Add(LSColors.pink500);
                        }
                        ++num;
                    }
                    else
                        break;
                }
                while (beatmapTheme.objectColors.Count < 18)
                    beatmapTheme.objectColors.Add(color);
            }
            if (__0["bgs"] == null)
            {
                beatmapTheme.backgroundColors.Add(LSColors.gray100);
                beatmapTheme.backgroundColors.Add(LSColors.gray200);
                beatmapTheme.backgroundColors.Add(LSColors.gray300);
                beatmapTheme.backgroundColors.Add(LSColors.gray400);
                beatmapTheme.backgroundColors.Add(LSColors.gray500);
                beatmapTheme.backgroundColors.Add(LSColors.gray600);
                beatmapTheme.backgroundColors.Add(LSColors.gray700);
                beatmapTheme.backgroundColors.Add(LSColors.gray800);
                beatmapTheme.backgroundColors.Add(LSColors.gray900);
            }
            else
            {
                int num = 0;
                Color color = LSColors.pink500;
                foreach (KeyValuePair<string, JSONNode> keyValuePair in __0["bgs"].AsArray)
                {
                    JSONNode hex = keyValuePair;
                    if (num < 9)
                    {
                        if (hex != null)
                        {
                            beatmapTheme.backgroundColors.Add(LSColors.HexToColor(hex));
                            color = LSColors.HexToColor(hex);
                        }
                        else
                            beatmapTheme.backgroundColors.Add(LSColors.pink500);
                        ++num;
                    }
                    else
                        break;
                }
                while (beatmapTheme.backgroundColors.Count < 9)
                    beatmapTheme.backgroundColors.Add(color);
            }
            if (__1)
            {
                DataManager.inst.CustomBeatmapThemes.Add(beatmapTheme);
                if (DataManager.inst.BeatmapThemeIDToIndex.ContainsKey(int.Parse(beatmapTheme.id)))
                {
                    string str = "";
                    for (int i = 0; i < DataManager.inst.AllThemes.Count; i++)
                    {
                        if (DataManager.inst.AllThemes[i].id == beatmapTheme.id)
                        {
                            str += DataManager.inst.AllThemes[i].name;
                            if (i != DataManager.inst.AllThemes.Count - 1)
                            {
                                str += ", ";
                            }
                        }
                    }

                    if (CoreHelper.InEditor)
                        EditorManager.inst.DisplayNotification($"Unable to Load theme [{beatmapTheme.id}-{beatmapTheme.name}]\nDue to conflicting themes: {str}", 2f, EditorManager.NotificationType.Error);
                    

                    CoreHelper.LogError($"Unable to load theme {beatmapTheme.name} due to the id ({beatmapTheme.id}) conflicting with these other themes: {str}.");
                }
                else
                {
                    DataManager.inst.BeatmapThemeIndexToID.Add(DataManager.inst.AllThemes.Count() - 1, int.Parse(beatmapTheme.id));
                    DataManager.inst.BeatmapThemeIDToIndex.Add(int.Parse(beatmapTheme.id), DataManager.inst.AllThemes.Count() - 1);
                }
            }
            __result = beatmapTheme;
            return false;
        }

        [HarmonyPatch(nameof(BaseBeatmapTheme.DeepCopy))]
        [HarmonyPrefix]
        static bool DeepCopyPrefix(ref BaseBeatmapTheme __result, BaseBeatmapTheme __0, bool __1 = false)
        {
            var themeCopy = new BaseBeatmapTheme();
            themeCopy.name = __0.name;
            themeCopy.playerColors = new List<Color>((from cols in __0.playerColors
                                                      select new Color(cols.r, cols.g, cols.b, cols.a)).ToList());
            themeCopy.objectColors = new List<Color>((from cols in __0.objectColors
                                                      select new Color(cols.r, cols.g, cols.b, cols.a)).ToList());
            themeCopy.guiColor = __0.guiColor;
            themeCopy.backgroundColor = __0.backgroundColor;
            themeCopy.backgroundColors = new List<Color>((from cols in __0.backgroundColors
                                                          select new Color(cols.r, cols.g, cols.b, cols.a)).ToList());
            AccessTools.Field(typeof(BaseBeatmapTheme), "expanded").SetValue(themeCopy, AccessTools.Field(typeof(BaseBeatmapTheme), "expanded").GetValue(__0));
            if (__1)
            {
                themeCopy.id = __0.id;
            }
            if (themeCopy.objectColors.Count < __0.objectColors.Count)
            {
                Color item = themeCopy.objectColors.Last();
                while (themeCopy.objectColors.Count < __0.objectColors.Count)
                {
                    themeCopy.objectColors.Add(item);
                }
            }

            while (themeCopy.objectColors.Count < 18)
            {
                themeCopy.objectColors.Add(themeCopy.objectColors[themeCopy.objectColors.Count - 1]);
            }

            if (themeCopy.backgroundColors.Count < 9)
            {
                Color item2 = themeCopy.backgroundColors.Last();
                while (themeCopy.backgroundColors.Count < 9)
                {
                    themeCopy.backgroundColors.Add(item2);
                }
            }
            __result = themeCopy;
            return false;
        }
    }

    [HarmonyPatch(typeof(BaseBeatmapObject))]
    public class DataManagerBeatmapObjectPatch
    {
        [HarmonyPatch(nameof(BaseBeatmapObject.ParseGameObject))]
        [HarmonyPrefix]
        static bool ParseGameObjectPrefix(ref BaseBeatmapObject __result, JSONNode __0)
        {
            __result = BeatmapObject.Parse(__0);
            return false;
        }
    }

    [HarmonyPatch(typeof(BasePrefab))]
    public class DataManagerPrefabPatch
    {
        [HarmonyPatch(nameof(BasePrefab.DeepCopy))]
        [HarmonyPrefix]
        static bool DeepCopyPrefix(ref BasePrefab __result, BasePrefab __0, bool __1 = true)
        {
            var prefab = new BasePrefab();
            prefab.Name = __0.Name;
            prefab.ID = (__1 ? LSText.randomString(16) : __0.ID);
            prefab.MainObjectID = __0.MainObjectID;
            prefab.Type = __0.Type;
            prefab.Offset = __0.Offset;
            prefab.objects = new List<BaseBeatmapObject>((from obj in __0.objects
                                                      select BaseBeatmapObject.DeepCopy(obj, false)).ToList());

            prefab.prefabObjects = new List<DataManager.GameData.PrefabObject>((from obj in __0.prefabObjects
                                                                                select DataManager.GameData.PrefabObject.DeepCopy(obj, false)).ToList());

            __result = prefab;
            return false;
        }
    }
}
