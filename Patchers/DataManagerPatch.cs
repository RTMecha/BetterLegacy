using BepInEx.Configuration;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Menus;
using HarmonyLib;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BeatmapObject = DataManager.GameData.BeatmapObject;
using BeatmapTheme = DataManager.BeatmapTheme;
using Prefab = DataManager.GameData.Prefab;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(DataManager))]
    public class DataManagerPatch : MonoBehaviour
    {
        public static DataManager Instance { get => DataManager.inst; set => DataManager.inst = value; }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPostfix(DataManager __instance)
        {
            // Initialize managers
            AlephNetworkManager.Init();
            ModCompatibility.Init();
            ShapeManager.Init();
            UIManager.Init();
            QuickElementManager.Init();
            SpriteManager.Init();
            FontManager.Init();
            AssetManager.Init();
            AchievementManager.Init();
            LevelManager.Init();
            PlayerManager.Init();

            AudioManager.inst.gameObject.AddComponent<SoundManager>();
            ArcadeManager.inst.gameObject.AddComponent<RTArcade>();

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

            __instance.BeatmapThemes.Add(DataManager.inst.CreateTheme("PA Example Theme", "9",
                LSColors.HexToColor("212121"),
                LSColors.HexToColorAlpha("504040FF"),
                new List<Color>
                {
                    LSColors.HexToColorAlpha("E57373FF"),
                    LSColors.HexToColorAlpha("64B5F6FF"),
                    LSColors.HexToColorAlpha("81C784FF"),
                    LSColors.HexToColorAlpha("FFB74DFF")
                }, new List<Color>
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
                }, new List<Color>
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
                }));

            foreach (var beatmapTheme in __instance.BeatmapThemes)
            {
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

                for (int i = 0; i < beatmapTheme.backgroundColors.Count; i++)
                {
                    beatmapTheme.backgroundColors[i] = LSColors.fadeColor(beatmapTheme.backgroundColors[i], 1f);
                }
            }

            for (int i = 0; i < __instance.BeatmapThemes.Count; i++)
            {
                var beatmapTheme = __instance.BeatmapThemes[i];
                __instance.BeatmapThemes[i] = new Core.Data.BeatmapTheme
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

            LegacyPlugin.ParseProfile();

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

            MenuManager.Init();

            try
            {
                int num = 0;
                while (Instance.PrefabTypes.Count < 20)
                {
                    var prefabType = new DataManager.PrefabType
                    {
                        Color = Color.white,
                        Name = "NewType " + num.ToString()
                    };

                    Instance.PrefabTypes.Add(prefabType);
                    num++;
                }

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

        [HarmonyPatch("SaveData", typeof(string), typeof(DataManager.GameData))]
        [HarmonyPrefix]
        static bool SaveDataPrefix(DataManager __instance, ref IEnumerator __result, string __0, DataManager.GameData __1)
        {
            Debug.Log($"{__instance.className}GameData is modded: {__1 is Core.Data.GameData}");
            __result = ProjectData.Writer.SaveData(__0, (Core.Data.GameData)__1);
            return false;
        }

        [HarmonyPatch("SaveMetadata", typeof(string), typeof(DataManager.MetaData))]
        [HarmonyPrefix]
        static bool SaveMetadataPrefix(ref LSError __result, DataManager __instance, string __0, DataManager.MetaData __1)
        {
            var result = new LSError(false, "");
            JSONNode jn;
            try
            {
                if (__1 is Core.Data.MetaData)
                {
                    jn = ((Core.Data.MetaData)__1).ToJSON();

                    Debug.Log($"{__instance.className}Saving Metadata Full");
                    RTFile.WriteToFile(__0, jn.ToString());
                }
                else
                {
                    jn = JSON.Parse("{}");
                    jn["artist"]["name"] = __1.artist.Name;
                    jn["artist"]["link"] = __1.artist.Link;
                    jn["artist"]["linkType"] = __1.artist.LinkType.ToString();
                    jn["creator"]["steam_name"] = __1.creator.steam_name;
                    jn["creator"]["steam_id"] = __1.creator.steam_id.ToString();
                    jn["song"]["title"] = __1.song.title;
                    jn["song"]["difficulty"] = __1.song.difficulty.ToString();
                    jn["song"]["description"] = __1.song.description;
                    jn["song"]["bpm"] = __1.song.BPM.ToString();
                    jn["song"]["t"] = __1.song.time.ToString();
                    jn["song"]["preview_start"] = __1.song.BPM.ToString();
                    jn["song"]["preview_length"] = __1.song.time.ToString();
                    jn["beatmap"]["date_edited"] = __1.beatmap.date_edited;
                    jn["beatmap"]["version_number"] = __1.beatmap.version_number.ToString();
                    jn["beatmap"]["game_version"] = __1.beatmap.game_version;
                    jn["beatmap"]["workshop_id"] = __1.beatmap.workshop_id.ToString();

                    Debug.Log($"{__instance.className}Saving Metadata");
                    LSFile.WriteToFile(__0, jn.ToString());
                }
            }
            catch (System.Exception)
            {
                jn = JSON.Parse("{}");
                jn["artist"]["name"] = __1.artist.Name;
                jn["artist"]["link"] = __1.artist.Link;
                jn["artist"]["linkType"] = __1.artist.LinkType.ToString();
                jn["creator"]["steam_name"] = __1.creator.steam_name;
                jn["creator"]["steam_id"] = __1.creator.steam_id.ToString();
                jn["song"]["title"] = __1.song.title;
                jn["song"]["difficulty"] = __1.song.difficulty.ToString();
                jn["song"]["description"] = __1.song.description;
                jn["song"]["bpm"] = __1.song.BPM.ToString();
                jn["song"]["t"] = __1.song.time.ToString();
                jn["song"]["preview_start"] = __1.song.BPM.ToString();
                jn["song"]["preview_length"] = __1.song.time.ToString();
                jn["beatmap"]["date_edited"] = __1.beatmap.date_edited;
                jn["beatmap"]["version_number"] = __1.beatmap.version_number.ToString();
                jn["beatmap"]["game_version"] = __1.beatmap.game_version;
                jn["beatmap"]["workshop_id"] = __1.beatmap.workshop_id.ToString();

                Debug.Log($"{__instance.className}Saving Metadata");
                RTFile.WriteToFile(__0, jn.ToString());
            }

            __result = result;

            return false;
        }

        [HarmonyPatch("GeneratePrefabJSON")]
        [HarmonyPrefix]
        static bool GeneratePrefabJSON(ref JSONNode __result, Prefab __0)
        {
            __result = ((Core.Data.Prefab)__0).ToJSON();
            return false;
        }

        #region PlayerPrefs Patches

        [HarmonyPatch("HasKey")]
        [HarmonyPrefix]
        static bool HasKeyPrefix(ref bool __result, string __0)
        {
            __result = ModCompatibility.sharedFunctions.ContainsKey(Instance.settingPrefix + __0);
            return false;
        }

        [HarmonyPatch("SettingHasKey")]
        [HarmonyPrefix]
        static bool SettingHasKeyPrefix(ref bool __result, string __0)
        {
            __result = ModCompatibility.sharedFunctions.ContainsKey(Instance.settingPrefix + __0);
            return false;
        }

        [HarmonyPatch("UpdateSettingEnum")]
        [HarmonyPrefix]
        static bool UpdateSettingEnumPrefix(string __0, int __1)
        {
            ModCompatibility.sharedFunctions.AddSet(Instance.settingPrefix + __0, __1);
            return false;
        }

        [HarmonyPatch("GetSettingEnum")]
        [HarmonyPrefix]
        static bool GetSettingEnumPrefix(ref int __result, string __0, int __1)
        {
            if (ModCompatibility.sharedFunctions.ContainsKey(Instance.settingPrefix + __0) && ModCompatibility.sharedFunctions[Instance.settingPrefix + __0] is int num)
            {
                __result = num;
                return false;
            }

            __result = __1;
            return false;
        }

        [HarmonyPatch("UpdateSettingString")]
        [HarmonyPrefix]
        static bool UpdateSettingStringPrefix(string __0, string __1)
        {
            ModCompatibility.sharedFunctions.AddSet(Instance.settingPrefix + __0, __1);
            return false;
        }

        [HarmonyPatch("GetSettingString", new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool GetSettingStringPrefix1(ref string __result, string __0)
        {
            if (ModCompatibility.sharedFunctions.ContainsKey(Instance.settingPrefix + __0) && ModCompatibility.sharedFunctions[Instance.settingPrefix + __0] is string num)
            {
                __result = num;
                return false;
            }

            __result = "";
            return false;
        }

        [HarmonyPatch("GetSettingString", new Type[] { typeof(string), typeof(string) })]
        [HarmonyPrefix]
        static bool GetSettingStringPrefix2(ref string __result, string __0, string __1)
        {
            if (ModCompatibility.sharedFunctions.ContainsKey(Instance.settingPrefix + __0) && ModCompatibility.sharedFunctions[Instance.settingPrefix + __0] is string num)
            {
                __result = num;
                return false;
            }

            __result = __1;
            return false;
        }

        [HarmonyPatch("UpdateSettingInt")]
        [HarmonyPrefix]
        static bool UpdateSettingIntPrefix(string __0, int __1)
        {
            ModCompatibility.sharedFunctions.AddSet(Instance.settingPrefix + __0, __1);

            return false;
        }

        [HarmonyPatch("GetSettingInt", new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool GetSettingIntPrefix1(ref int __result, string __0)
        {
            if (ModCompatibility.sharedFunctions.ContainsKey(Instance.settingPrefix + __0) && ModCompatibility.sharedFunctions[Instance.settingPrefix + __0] is int num)
            {
                __result = num;
                return false;
            }

            __result = 0;
            return false;
        }

        [HarmonyPatch("GetSettingInt", new Type[] { typeof(string), typeof(int) })]
        [HarmonyPrefix]
        static bool GetSettingIntPrefix2(ref int __result, string __0, int __1)
        {
            if (ModCompatibility.sharedFunctions.ContainsKey(Instance.settingPrefix + __0) && ModCompatibility.sharedFunctions[Instance.settingPrefix + __0] is int num)
            {
                __result = num;
                return false;
            }

            __result = __1;
            return false;
        }

        [HarmonyPatch("UpdateSettingFloat")]
        [HarmonyPrefix]
        static bool UpdateSettingFloatPrefix(string __0, float __1)
        {
            ModCompatibility.sharedFunctions.AddSet(Instance.settingPrefix + __0, __1);
            return false;
        }

        [HarmonyPatch("GetSettingFloat", new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool GetSettingFloatPrefix1(ref float __result, string __0)
        {
            if (ModCompatibility.sharedFunctions.ContainsKey(Instance.settingPrefix + __0) && ModCompatibility.sharedFunctions[Instance.settingPrefix + __0] is float num)
            {
                __result = num;
                return false;
            }

            __result = 0f;
            return false;
        }

        [HarmonyPatch("GetSettingFloat", new Type[] { typeof(string), typeof(float) })]
        [HarmonyPrefix]
        static bool GetSettingFloatPrefix2(ref float __result, string __0, float __1)
        {
            if (ModCompatibility.sharedFunctions.ContainsKey(Instance.settingPrefix + __0) && ModCompatibility.sharedFunctions[Instance.settingPrefix + __0] is float num)
            {
                __result = num;
                return false;
            }

            __result = __1;
            return false;
        }

        [HarmonyPatch("UpdateSettingBool")]
        [HarmonyPrefix]
        static bool UpdateSettingBoolPrefix(string __0, bool __1)
        {
            ModCompatibility.sharedFunctions.AddSet(Instance.settingPrefix + __0, __1);

            return false;
        }

        [HarmonyPatch("GetSettingBool", new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool GetSettingBoolPrefix1(ref bool __result, string __0)
        {
            if (ModCompatibility.sharedFunctions.ContainsKey(Instance.settingPrefix + __0) && ModCompatibility.sharedFunctions[Instance.settingPrefix + __0] is bool num)
            {
                __result = num;
                return false;
            }

            __result = false;
            return false;
        }

        [HarmonyPatch("GetSettingBool", new Type[] { typeof(string), typeof(bool) })]
        [HarmonyPrefix]
        static bool GetSettingBoolPrefix2(ref bool __result, string __0, bool __1)
        {
            if (ModCompatibility.sharedFunctions.ContainsKey(Instance.settingPrefix + __0) && ModCompatibility.sharedFunctions[Instance.settingPrefix + __0] is bool num)
            {
                __result = num;
                return false;
            }

            __result = __1;
            return false;
        }

        [HarmonyPatch("UpdateSettingVector2D")]
        [HarmonyPrefix]
        static bool UpdateSettingVector2DPrefix(string __0, int __1, Vector2[] __2)
        {
            ModCompatibility.sharedFunctions.AddSet(Instance.settingPrefix + __0 + "_i", __1);
            ModCompatibility.sharedFunctions.AddSet(Instance.settingPrefix + __0 + "_x", __2[__1].x);
            ModCompatibility.sharedFunctions.AddSet(Instance.settingPrefix + __0 + "_y", __2[__1].y);
            return false;
        }

        [HarmonyPatch("GetSettingVector2D")]
        [HarmonyPrefix]
        static bool GetSettingVector2DPrefix(ref Vector2 __result, string __0)
        {
            if (ModCompatibility.sharedFunctions.ContainsKey(Instance.settingPrefix + __0 + "_x") && ModCompatibility.sharedFunctions[Instance.settingPrefix + __0 + "_x"] is float x &&
                ModCompatibility.sharedFunctions.ContainsKey(Instance.settingPrefix + __0 + "_y") && ModCompatibility.sharedFunctions[Instance.settingPrefix + __0 + "_y"] is float y)
            {
                __result = new Vector2(x, y);
                return false;
            }

            __result = Vector2.zero;
            return false;
        }

        [HarmonyPatch("GetSettingVector2DIndex")]
        [HarmonyPrefix]
        static bool GetSettingVector2DIndexPrefix(ref int __result, string __0)
        {
            if (ModCompatibility.sharedFunctions.ContainsKey(Instance.settingPrefix + __0 + "_i") && ModCompatibility.sharedFunctions[Instance.settingPrefix + __0 + "_i"] is int num)
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
        [HarmonyPatch("ParseBeatmap")]
        [HarmonyPrefix]
        static bool ParseBeatmapPatch(string _json) => false;
    }

    [HarmonyPatch(typeof(BeatmapTheme))]
    public class DataManagerBeatmapThemePatch
    {
        [HarmonyPatch("Lerp")]
        [HarmonyPrefix]
        static bool Lerp(BeatmapTheme __instance, ref BeatmapTheme _start, ref BeatmapTheme _end, float _val)
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

        [HarmonyPatch("Parse")]
        [HarmonyPrefix]
        static bool ParsePrefix(BeatmapTheme __instance, ref BeatmapTheme __result, JSONNode __0, bool __1)
        {
            BeatmapTheme beatmapTheme = new BeatmapTheme();
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

                    if (EditorManager.inst != null)
                    {
                        EditorManager.inst.DisplayNotification($"Unable to Load theme [{beatmapTheme.id}-{beatmapTheme.name}]\nDue to conflicting themes: {str}", 2f, EditorManager.NotificationType.Error);
                    }

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

        [HarmonyPatch("DeepCopy")]
        [HarmonyPrefix]
        static bool DeepCopyPatch(ref BeatmapTheme __result, BeatmapTheme __0, bool __1 = false)
        {
            var themeCopy = new BeatmapTheme();
            themeCopy.name = __0.name;
            themeCopy.playerColors = new List<Color>((from cols in __0.playerColors
                                                      select new Color(cols.r, cols.g, cols.b, cols.a)).ToList());
            themeCopy.objectColors = new List<Color>((from cols in __0.objectColors
                                                      select new Color(cols.r, cols.g, cols.b, cols.a)).ToList());
            themeCopy.guiColor = __0.guiColor;
            themeCopy.backgroundColor = __0.backgroundColor;
            themeCopy.backgroundColors = new List<Color>((from cols in __0.backgroundColors
                                                          select new Color(cols.r, cols.g, cols.b, cols.a)).ToList());
            AccessTools.Field(typeof(BeatmapTheme), "expanded").SetValue(themeCopy, AccessTools.Field(typeof(BeatmapTheme), "expanded").GetValue(__0));
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

    [HarmonyPatch(typeof(BeatmapObject))]
    public class DataManagerBeatmapObjectPatch
    {
        [HarmonyPatch("ParseGameObject")]
        [HarmonyPrefix]
        static bool ParseGameObjectPrefix(ref BeatmapObject __result, JSONNode __0)
        {
            __result = Core.Data.BeatmapObject.Parse(__0);
            return false;
        }
    }

    [HarmonyPatch(typeof(Prefab))]
    public class DataManagerPrefabPatch
    {
        [HarmonyPatch("DeepCopy")]
        [HarmonyPrefix]
        static bool DeepCopyPrefix(ref Prefab __result, Prefab __0, bool __1 = true)
        {
            Prefab prefab = new Prefab();
            prefab.Name = __0.Name;
            prefab.ID = (__1 ? LSText.randomString(16) : __0.ID);
            prefab.MainObjectID = __0.MainObjectID;
            prefab.Type = __0.Type;
            prefab.Offset = __0.Offset;
            prefab.objects = new List<BeatmapObject>((from obj in __0.objects
                                                      select BeatmapObject.DeepCopy(obj, false)).ToList());

            prefab.prefabObjects = new List<DataManager.GameData.PrefabObject>((from obj in __0.prefabObjects
                                                                                select DataManager.GameData.PrefabObject.DeepCopy(obj, false)).ToList());

            __result = prefab;
            return false;
        }
    }
}
