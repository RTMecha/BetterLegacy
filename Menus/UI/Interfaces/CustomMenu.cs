using System.Collections.Generic;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Layouts;

namespace BetterLegacy.Menus.UI.Interfaces
{
    public class CustomMenu : MenuBase
    {
        public CustomMenu() : base() { }
        
        public override void UpdateTheme()
        {
            if (useGameTheme && CoreHelper.InGame)
                Theme = CoreHelper.CurrentBeatmapTheme;
            else if (loadedTheme != null)
                Theme = loadedTheme;
            else if (Parser.TryParse(currentTheme, -1) >= 0 && InterfaceManager.inst.themes.TryFind(x => x.id == currentTheme, out BeatmapTheme current))
                Theme = current;

            base.UpdateTheme();
        }

        public bool useGameTheme;

        /// <summary>
        /// The theme to choose from in the default themes list. If the value is -1, then it will use the players' preferred theme.
        /// </summary>
        public string currentTheme = "-1";

        /// <summary>
        /// The theme loaded from the file.
        /// </summary>
        public BeatmapTheme loadedTheme;

        /// <summary>
        /// Parses a Custom Menu from a JSON file.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns a parsed Custom Menu.</returns>
        public static CustomMenu Parse(JSONNode jn)
        {
            var customMenu = new CustomMenu();

            customMenu.id = jn["id"];
            customMenu.name = InterfaceManager.inst.ParseVarFunction(jn["name"]);
            customMenu.musicName = InterfaceManager.inst.ParseVarFunction(jn["music_name"]) ?? InterfaceManager.RANDOM_MUSIC_NAME;
            var allowCustomMusic = InterfaceManager.inst.ParseVarFunction(jn["allow_custom_music"]);
            if (allowCustomMusic != null)
                customMenu.allowCustomMusic = allowCustomMusic.AsBool;

            var type = InterfaceManager.inst.ParseVarFunction(jn["type"]);
            if (type != null)
            {
                switch (type.Value.ToLower())
                {
                    case "chat": {
                            var returnInterface = InterfaceManager.inst.ParseVarFunction(jn["return_interface"]);
                            var returnInterfacePath = InterfaceManager.inst.ParseVarFunction(jn["return_interface_path"]);
                            var seen = InterfaceManager.inst.ParseVarFunction(jn["seen"]);
                            var dialogue = InterfaceManager.inst.ParseVarFunction(jn["dialogue"]);
                            var dialogueCount = dialogue.Count;

                            System.Func<JSONArray> onScrollUpFuncJSON = () => new JSONArray
                            {
                                [0] = new JSONObject
                                {
                                    ["name"] = "ScrollLayout",
                                    ["params"] = new JSONArray
                                    {
                                        [0] = "chat_layout",
                                        [1] = "-68",
                                        [2] = "True",
                                    },
                                },
                                [1] = new JSONObject
                                {
                                    ["if_func"] = new JSONArray
                                    {
                                        [0] = new JSONObject
                                        {
                                            ["name"] = "LayoutScrollYGreater",
                                            ["params"] = new JSONArray { [0] = "chat_layout", [1] = "0" }
                                        },
                                        [1] = new JSONObject { ["name"] = "!CurrentInterfaceGenerating" }
                                    }
                                    ["name"] = "PlaySound",
                                    ["params"] = new JSONArray { [0] = "Click" }
                                },
                            };
                            System.Func<JSONArray> onScrollDownFuncJSON = () => new JSONArray
                            {
                                [0] = new JSONObject
                                {
                                    ["if_func"] = new JSONObject { ["name"] = "!CurrentInterfaceGenerating" },
                                    ["name"] = "ScrollLayout",
                                    ["params"] = new JSONArray
                                    {
                                        [0] = "chat_layout",
                                        [1] = "68",
                                        [2] = "True",
                                    },
                                },
                                [1] = new JSONObject
                                {
                                    ["if_func"] = new JSONObject { ["name"] = "!CurrentInterfaceGenerating" },
                                    ["name"] = "PlaySound",
                                    ["params"] = new JSONArray { [0] = "Click" }
                                },
                            };

                            customMenu.layouts["buttons"] = new MenuVerticalLayout
                            {
                                childControlWidth = true,
                                childForceExpandWidth = true,
                                spacing = 4f,
                                rect = RectValues.Default.AnchoredPosition(-500f, 250f).SizeDelta(800f, 200f),
                            };
                            customMenu.layouts["chat_layout"] = new MenuVerticalLayout
                            {
                                childControlWidth = true,
                                childForceExpandWidth = true,
                                spacing = 4f,
                                rect = RectValues.FullAnchored.AnchoredPosition(0f, -40f).SizeDelta(-256f, -470f),
                                scrollable = true,
                                minScroll = 0f,
                                maxScroll = (68f * dialogueCount) - 68f,
                                mask = true,
                                onScrollUpFuncJSON = onScrollUpFuncJSON(),
                                onScrollDownFuncJSON = onScrollDownFuncJSON(),
                            };

                            customMenu.elements.Add(new MenuEvent
                            {
                                id = "632626",
                                name = "Effect",
                                funcJSON = new JSONObject { ["name"] = "SetDefaultEvents" },
                                length = 0f,
                                wait = false,
                            });
                            var dateTime = System.DateTime.Now;
                            customMenu.elements.AddRange(GenerateTopBar($"PA Chat, stable {dateTime.ToString("yyyy")}.{dateTime.ToString("MM")}.{Story.StoryManager.inst.CurrentSave.Slot}", 6, 0f));
                            customMenu.elements.Add(new MenuImage
                            {
                                id = "6267583525",
                                name = "Bar",
                                color = 6,
                                opacity = 0.2f,
                                rect = RectValues.Default.AnchoredPosition(870f, -38f).SizeDelta(64f, 608f),
                                length = 0f,
                            });
                            customMenu.elements.AddRange(GenerateBottomBar(6, 0f));

                            var defaultLength = 4f;
                            var dateFormat = "{{Date=HH:mm:ss:tt}}";
                            var jnDefaults = InterfaceManager.inst.ParseVarFunction(jn["defaults"]);
                            if (jnDefaults != null)
                            {
                                var jnDefaultLength = InterfaceManager.inst.ParseVarFunction(jnDefaults["length"]);
                                if (jnDefaultLength != null)
                                    defaultLength = jnDefaultLength.AsFloat;

                                var jnDefaultDateFormat = InterfaceManager.inst.ParseVarFunction(jnDefaults["date_format"]);
                                if (!string.IsNullOrEmpty(jnDefaultDateFormat))
                                    dateFormat = jnDefaultDateFormat;
                            }

                            var forJN = Parser.NewJSONObject();
                            forJN["type"] = "For";
                            forJN["from"] = "JSON";
                            forJN["element_prefabs"][0] = new JSONObject
                            {
                                ["type"] = "Image",
                                ["id"] = "1",
                                ["name"] = "Parent",
                                ["parent_layout"] = "chat_layout",
                                ["rect"] = new JSONObject
                                {
                                    ["size"] = new JSONArray { [0] = "1400", [1] = "64" }
                                },
                                ["text_rect"] = new JSONObject
                                {
                                    ["size"] = new JSONArray { [0] = "-64", [1] = "0" }
                                },
                                ["col"] = "6",
                                ["opacity"] = "0.05",
                                ["anim_length"] = "0",
                                ["on_scroll_up_func"] = onScrollUpFuncJSON(),
                                ["on_scroll_down_func"] = onScrollDownFuncJSON(),
                            };
                            forJN["element_prefabs"][1] = new JSONObject
                            {
                                ["type"] = "Text",
                                ["id"] = "2",
                                ["name"] = "Name",
                                ["rect"] = new JSONObject
                                {
                                    ["anc_max"] = new JSONArray { [0] = "1", [1] = "0.5" },
                                    ["anc_min"] = new JSONArray { [0] = "1", [1] = "0.5" },
                                    ["size"] = new JSONArray { [0] = "1100", [1] = "64" },
                                },
                                ["text_rect"] = new JSONObject
                                {
                                    ["size"] = new JSONArray { [0] = "-64", [1] = "0" },
                                },
                                ["col"] = "6",
                                ["opacity"] = "0.05",
                                ["text_col"] = "6",
                                ["anim_length"] = "0",
                                ["on_scroll_up_func"] = onScrollUpFuncJSON(),
                                ["on_scroll_down_func"] = onScrollDownFuncJSON(),
                            };
                            forJN["element_prefabs"][2] = new JSONObject
                            {
                                ["type"] = "Text",
                                ["id"] = "3",
                                ["name"] = "Date",
                                ["rect"] = new JSONObject
                                {
                                    ["anc_max"] = new JSONArray { [0] = "1", [1] = "0.5" },
                                    ["anc_min"] = new JSONArray { [0] = "1", [1] = "0.5" },
                                    ["size"] = new JSONArray { [0] = "0", [1] = "64" },
                                },
                                ["text_rect"] = new JSONObject
                                {
                                    ["size"] = new JSONArray { [0] = "-64", [1] = "0" },
                                },
                                ["text"] = $"<align=right>{dateFormat} |",
                                ["hide_bg"] = "True",
                                ["text_col"] = "6",
                                ["anim_length"] = "0",
                                ["on_scroll_up_func"] = onScrollUpFuncJSON(),
                                ["on_scroll_down_func"] = onScrollDownFuncJSON(),
                            };
                            forJN["element_prefabs"][3] = new JSONObject
                            {
                                ["type"] = "Text",
                                ["id"] = "4",
                                ["name"] = "Chat",
                                ["rect"] = "FullAnchored",
                                ["text_rect"] = new JSONObject
                                {
                                    ["size"] = new JSONArray { [0] = "-64", [1] = "0" },
                                },
                                ["hide_bg"] = "True",
                                ["text_col"] = "6",
                                ["anim_length"] = defaultLength,
                                ["text_sound_repeat"] = "2",
                                ["text_sound_pitch_vary"] = "0.1",
                                ["on_scroll_up_func"] = onScrollUpFuncJSON(),
                                ["on_scroll_down_func"] = onScrollDownFuncJSON(),
                            };
                            forJN["element_prefabs"][4] = new JSONObject
                            {
                                ["type"] = "Event",
                                ["id"] = "5",
                                ["name"] = "Wait",
                                ["anim_length"] = "1",
                            };

                            for (int i = 0; i < dialogueCount; i++)
                            {
                                var jnDialogue = InterfaceManager.inst.ParseVarFunction(dialogue[i]);
                                if (jnDialogue == null)
                                    continue;

                                var id = LSText.randomNumString(16);
                                string character = InterfaceManager.inst.ParseVarFunction(jnDialogue["character"]);
                                string text = InterfaceManager.inst.ParseVarFunction(jnDialogue["text"]);
                                string color = InterfaceManager.inst.ParseVarFunction(jnDialogue["color"]);
                                string sound = InterfaceManager.inst.ParseVarFunction(jnDialogue["sound"]);

                                forJN["to"][i]["1"] = new JSONObject { ["id"] = id, };
                                forJN["to"][i]["2"] = new JSONObject
                                {
                                    ["parent"] = id,
                                    ["text"] = $"| {character}",
                                };
                                forJN["to"][i]["3"] = new JSONObject { ["parent"] = id, };
                                var jnDateFormat = InterfaceManager.inst.ParseVarFunction(jnDialogue["date_format"]);
                                if (jnDateFormat != null)
                                    forJN["to"][i]["3"]["text"] = $"<align=right>{jnDateFormat.Value} |";

                                var jnText = new JSONObject
                                {
                                    ["parent"] = id,
                                    ["text"] = text
                                };
                                var jnLength = InterfaceManager.inst.ParseVarFunction(jnDialogue["length"]);
                                if (jnLength != null)
                                    jnText["length"] = jnLength;
                                if (!string.IsNullOrEmpty(color))
                                    jnText["override_text_col"] = color;
                                if (!string.IsNullOrEmpty(sound))
                                    jnText["text_sound"] = sound;

                                var speeds = InterfaceManager.inst.ParseVarFunction(jnDialogue["speeds"]);
                                if (speeds != null)
                                {
                                    for (int j = 0; j < speeds.Count; j++)
                                    {
                                        var speed = InterfaceManager.inst.ParseVarFunction(speeds[j]);
                                        if (speed == null)
                                            continue;

                                        var position = InterfaceManager.inst.ParseVarFunction(speed["position"]);
                                        var speedValue = InterfaceManager.inst.ParseVarFunction(speed["speed"]);
                                        if (position == null || speedValue == null)
                                            continue;

                                        jnText["speeds"][j]["position"] = position;
                                        jnText["speeds"][j]["speed"] = speedValue;
                                    }
                                }

                                var soundVolume = InterfaceManager.inst.ParseVarFunction(jnDialogue["sound_volume"]);
                                if (soundVolume != null)
                                    jnText["text_sound_volume"] = soundVolume;

                                var soundPitch = InterfaceManager.inst.ParseVarFunction(jnDialogue["sound_pitch"]);
                                if (soundPitch != null)
                                    jnText["text_sound_pitch"] = soundPitch;

                                var soundPitchVary = InterfaceManager.inst.ParseVarFunction(jnDialogue["sound_pitch_vary"]);
                                if (soundPitchVary != null)
                                    jnText["text_sound_pitch_vary"] = soundPitchVary;

                                var soundRepeat = InterfaceManager.inst.ParseVarFunction(jnDialogue["sound_repeat"]);
                                if (soundRepeat != null)
                                    jnText["text_sound_repeat"] = soundRepeat;

                                forJN["to"][i]["4"] = jnText;

                                forJN["to"][i]["5"] = new JSONObject { ["anim_length"] = jnDialogue["wait"], };

                                if (i > 8)
                                {
                                    forJN["to"][i]["1"]["spawn_func"] = new JSONObject
                                    {
                                        ["name"] = "ScrollLayout",
                                        ["params"] = new JSONArray
                                        {
                                            [0] = "chat_layout",
                                            [1] = "68",
                                            [2] = "True",
                                        },
                                    };
                                }
                            }

                            var elements = Parser.NewJSONArray();
                            elements[0] = forJN;
                            customMenu.elements.AddRange(ParseElements(elements));

                            customMenu.elements.Add(new MenuImage
                            {
                                id = "6267583525",
                                name = "Bar",
                                color = 6,
                                opacity = 0.2f,
                                rect = RectValues.Default.AnchoredPosition(870f, -38f).SizeDelta(64f, 608f),
                                length = 0f,
                                wait = false,
                                onScrollUpFuncJSON = onScrollUpFuncJSON(),
                                onScrollDownFuncJSON = onScrollDownFuncJSON(),
                            });
                            customMenu.elements.Add(new MenuButton
                            {
                                id = "3627564652",
                                name = "Up Button",
                                selectionPosition = new Vector2Int(1, 0),
                                iconPath = RTFile.GetAsset("editor_gui_up.png"),
                                color = 6,
                                rect = RectValues.Default.AnchoredPosition(870f, 234f).SizeDelta(64f, 64f),
                                iconRect = RectValues.Default.SizeDelta(64f, 64f),
                                length = 0f,
                                wait = false,
                                funcJSON = onScrollUpFuncJSON(),
                                onScrollUpFuncJSON = onScrollUpFuncJSON(),
                                onScrollDownFuncJSON = onScrollDownFuncJSON(),
                            });
                            customMenu.elements.Add(new MenuButton
                            {
                                id = "95578583",
                                name = "Down Button",
                                selectionPosition = new Vector2Int(1, 1),
                                iconPath = RTFile.GetAsset("editor_gui_down.png"),
                                color = 6,
                                rect = RectValues.Default.AnchoredPosition(870f, -310f).SizeDelta(64f, 64f),
                                iconRect = RectValues.Default.SizeDelta(64f, 64f),
                                length = 0f,
                                wait = false,
                                funcJSON = onScrollDownFuncJSON(),
                                onScrollUpFuncJSON = onScrollUpFuncJSON(),
                                onScrollDownFuncJSON = onScrollDownFuncJSON(),
                            });
                            customMenu.elements.Add(new MenuButton
                            {
                                id = "15215",
                                name = "Back Button",
                                parentLayout = "buttons",
                                text = "<b> [ RETURN ]",
                                selectionPosition = Vector2Int.zero,
                                rect = RectValues.Default.SizeDelta(200f, 64f),
                                color = 6,
                                selectedColor = 6,
                                opacity = 0.1f,
                                selectedOpacity = 1f,
                                textColor = 6,
                                selectedTextColor = 7,
                                length = 1f,
                                playBlipSound = true,
                                funcJSON = new JSONArray
                                {
                                    [0] = new JSONObject { ["name"] = "ClearInterfaces" },
                                    [1] = new JSONObject
                                    {
                                        ["name"] = "Parse",
                                        ["params"] = new JSONObject
                                        {
                                            ["file"] = returnInterface,
                                            ["load"] = "True",
                                            ["path"] = returnInterfacePath,
                                        },
                                    },
                                    [2] = new JSONObject
                                    {
                                        ["name"] = "StorySaveBool",
                                        ["params"] = new JSONArray
                                        {
                                            [0] = seen,
                                            [1] = "True",
                                        },
                                    }
                                },
                            });

                            customMenu.elements.Add(new MenuImage
                            {
                                id = "374848",
                                name = "Background",
                                siblingIndex = 0,
                                rect = RectValues.FullAnchored,
                                opacity = 0f,
                                length = 0f,
                                wait = false,
                                onScrollUpFuncJSON = onScrollUpFuncJSON(),
                                onScrollDownFuncJSON = onScrollDownFuncJSON(),
                            });

                            return customMenu;
                        }
                }
            }

            customMenu.defaultSelection = Parser.TryParse(InterfaceManager.inst.ParseVarFunction(jn["default_select"]), Vector2Int.zero);
            customMenu.forceInterfaceSpeed = InterfaceManager.inst.ParseVarFunction(jn["force_speed"]).AsBool;

            var layer = InterfaceManager.inst.ParseVarFunction(jn["layer"]);
            if (layer != null)
                customMenu.layer = layer.AsInt;

            var pauseGame = InterfaceManager.inst.ParseVarFunction(jn["pause_game"]);
            if (pauseGame != null)
                customMenu.pauseGame = pauseGame.AsBool;
            else
                customMenu.pauseGame = true;

            var sprites = InterfaceManager.inst.ParseVarFunction(jn["sprites"]);
            if (sprites != null)
            {
                foreach (var keyValuePair in sprites.Linq)
                {
                    if (customMenu.spriteAssets.ContainsKey(keyValuePair.Key))
                        continue;

                    var value = InterfaceManager.inst.ParseVarFunction(keyValuePair.Value);
                    if (value == null)
                        continue;

                    customMenu.spriteAssets.Add(keyValuePair.Key, SpriteHelper.StringToSprite(value));
                }
            }

            customMenu.prefabs.AddRange(ParsePrefabs(InterfaceManager.inst.ParseVarFunction(jn["prefabs"])));
            ParseLayouts(customMenu.layouts, InterfaceManager.inst.ParseVarFunction(jn["layouts"]));
            customMenu.elements.AddRange(ParseElements(InterfaceManager.inst.ParseVarFunction(jn["elements"]), customMenu.prefabs, customMenu.spriteAssets));

            var theme = InterfaceManager.inst.ParseVarFunction(jn["theme"]);
            if (jn["theme"] != null)
                customMenu.loadedTheme = BeatmapTheme.Parse(theme);
            customMenu.useGameTheme = InterfaceManager.inst.ParseVarFunction(jn["game_theme"]).AsBool;

            if (CoreHelper.InGame)
                customMenu.exitFunc = InterfaceManager.inst.CloseMenus;

            customMenu.exitFuncJSON = InterfaceManager.inst.ParseVarFunction(jn["exit_func"]);

            return customMenu;
        }

        public static IEnumerable<MenuPrefab> ParsePrefabs(JSONNode jn)
        {
            if (jn == null || !jn.IsArray)
                yield break;

            for (int i = 0; i < jn.Count; i++)
            {
                var elementPrefab = InterfaceManager.inst.ParseVarFunction(jn[i]);
                if (elementPrefab.IsArray)
                {
                    var prefabs = ParsePrefabs(elementPrefab);
                    foreach (var prefab in prefabs)
                        yield return prefab;

                    continue;
                }
                yield return MenuPrefab.Parse(elementPrefab);
            }
        }

        public static void ParseLayouts(Dictionary<string, MenuLayoutBase> layouts, JSONNode jn)
        {
            if (jn == null)
                return;

            if (jn.IsObject)
            {
                foreach (var keyValuePair in jn.Linq)
                    ParseLayout(layouts, keyValuePair.Key, InterfaceManager.inst.ParseVarFunction(keyValuePair.Value));
            }

            if (jn.IsArray)
            {
                for (int i = 0; i < jn.Count; i++)
                {
                    var jnLayout = InterfaceManager.inst.ParseVarFunction(jn[i]);
                    if (jnLayout.IsArray)
                    {
                        ParseLayouts(layouts, jnLayout);
                        continue;
                    }

                    var key = InterfaceManager.inst.ParseVarFunction(jnLayout["name"]);
                    ParseLayout(layouts, key, jnLayout);
                }
            }
        }

        public static void ParseLayout(Dictionary<string, MenuLayoutBase> layouts, string key, JSONNode jnLayout)
        {
            if (string.IsNullOrEmpty(key) || layouts.ContainsKey(key) || jnLayout == null)
                return;

            string layoutType = jnLayout["type"];
            switch (layoutType.ToLower())
            {
                case "grid": {
                        var gridLayout = MenuGridLayout.Parse(jnLayout);
                        gridLayout.name = key;
                        layouts.Add(key, gridLayout);

                        break;
                    }
                case "horizontal": {
                        var horizontalLayout = MenuHorizontalLayout.Parse(jnLayout);
                        horizontalLayout.name = key;
                        layouts.Add(key, horizontalLayout);

                        break;
                    }
                case "vertical": {
                        var verticalLayout = MenuVerticalLayout.Parse(jnLayout);
                        verticalLayout.name = key;
                        layouts.Add(key, verticalLayout);

                        break;
                    }
            }
        }

        public static IEnumerable<MenuImage> ParseElements(JSONNode jn, List<MenuPrefab> prefabs = null, Dictionary<string, Sprite> spriteAssets = null)
        {
            if (jn == null || !jn.IsArray)
                yield break;

            for (int i = 0; i < jn.Count; i++)
            {
                var jnElement = InterfaceManager.inst.ParseVarFunction(jn[i]);
                if (jnElement == null)
                    continue;

                // handle grouped elements recursively
                if (jnElement.IsArray)
                {
                    var elements = ParseElements(jnElement, prefabs, spriteAssets);
                    foreach (var element in elements)
                        yield return element;

                    continue;
                }

                var elementType = InterfaceManager.inst.ParseVarFunction(jnElement["type"]);
                if (elementType == null)
                    continue;

                // loop function
                int loop = 1;
                var jnLoop = InterfaceManager.inst.ParseVarFunction(jnElement["loop"]);
                if (jnLoop != null)
                    loop = jnLoop.AsInt;
                if (loop < 1)
                    loop = 1;

                for (int loopIndex = 0; loopIndex < loop; loopIndex++)
                    switch (elementType.Value.ToLower())
                    {
                        case "auto": {
                                var name = InterfaceManager.inst.ParseVarFunction(jnElement["name"]);
                                if (name == null)
                                    break;

                                IEnumerable<MenuImage> collection = null;
                                switch (name.Value)
                                {
                                    case "TopBar": {
                                            var title = InterfaceManager.inst.ParseVarFunction(jnElement["title"]);
                                            collection = GenerateTopBar(
                                                title == null ? "Custom Menu" : Lang.Parse(title),
                                                InterfaceManager.inst.ParseVarFunction(jnElement["text_col"]).AsInt,
                                                InterfaceManager.inst.ParseVarFunction(jnElement["text_val"]).AsFloat);
                                            break;
                                        }
                                    case "BottomBar": {
                                            collection = GenerateBottomBar(
                                                InterfaceManager.inst.ParseVarFunction(jnElement["text_col"]).AsInt,
                                                InterfaceManager.inst.ParseVarFunction(jnElement["text_val"]).AsFloat);
                                            break;
                                        }
                                }

                                if (collection == null)
                                    break;

                                foreach (var item in collection)
                                    yield return item;

                                break;
                            }
                        case "prefab": {
                                var id = jnElement["id"];
                                if (prefabs == null || id == null || !prefabs.TryFind(x => x.id == id, out MenuPrefab prefab))
                                    break;

                                var from = InterfaceManager.inst.ParseVarFunction(jnElement["from"]);
                                if (from != null)
                                {
                                    var type = InterfaceManager.inst.ParseVarFunction(from["type"]);
                                    if (type == null)
                                        break;

                                    switch (type.Value.ToLower())
                                    {
                                        case "json": {
                                                var array = InterfaceManager.inst.ParseVarFunction(from["array"]);
                                                if (array == null)
                                                    break;

                                                for (int k = 0; k < array.Count; k++)
                                                {
                                                    var prefabObject = new MenuPrefabObject
                                                    {
                                                        prefabID = InterfaceManager.inst.ParseVarFunction(jnElement["pid"]),
                                                        prefab = prefab,
                                                        id = InterfaceManager.inst.ParseVarFunction(jnElement["id"]) ?? LSText.randomNumString(16),
                                                        name = InterfaceManager.inst.ParseVarFunction(jnElement["name"]),
                                                        length = InterfaceManager.inst.ParseVarFunction(jnElement["anim_length"]).AsFloat,
                                                        fromLoop = loopIndex > 0,
                                                        loop = loop,
                                                    };

                                                    var elementSpawnFunc = InterfaceManager.inst.ParseVarFunction(jnElement["spawn_if_func"], prefabObject);
                                                    if (elementSpawnFunc == null || InterfaceManager.inst.ParseIfFunction(elementSpawnFunc, prefabObject))
                                                    {
                                                        var arrayElements = InterfaceManager.inst.ParseVarFunction(array[k]["elements"], prefabObject);
                                                        foreach (var arrayElement in arrayElements)
                                                        {
                                                            var arrayElementJN = InterfaceManager.inst.ParseVarFunction(arrayElement.Value, prefabObject);
                                                            if (arrayElementJN == null)
                                                                continue;

                                                            prefabObject.elementSettings[arrayElement.Key] = arrayElementJN;
                                                        }

                                                        yield return prefabObject;
                                                    }
                                                }

                                                break;
                                            }
                                    }

                                    break;
                                }

                                var element = new MenuPrefabObject
                                {
                                    prefabID = InterfaceManager.inst.ParseVarFunction(jnElement["pid"]),
                                    prefab = prefab,
                                    id = InterfaceManager.inst.ParseVarFunction(jnElement["id"]) ?? LSText.randomNumString(16),
                                    name = InterfaceManager.inst.ParseVarFunction(jnElement["name"]),
                                    length = InterfaceManager.inst.ParseVarFunction(jnElement["anim_length"]).AsFloat, // how long the UI pauses for when this element spawns.
                                    fromLoop = loopIndex > 0, // if element has been spawned from the loop or if its the first / only of its kind.
                                    loop = loop,
                                };

                                var spawnFunc = InterfaceManager.inst.ParseVarFunction(jnElement["spawn_if_func"], element);
                                if (spawnFunc == null || InterfaceManager.inst.ParseIfFunction(spawnFunc, element))
                                    yield return element;

                                break;
                            }
                        case "for": {
                                var spawnFunc = InterfaceManager.inst.ParseVarFunction(jnElement["spawn_if_func"]);
                                if (spawnFunc != null && !InterfaceManager.inst.ParseIfFunction(spawnFunc))
                                    break;

                                var from = InterfaceManager.inst.ParseVarFunction(jnElement["from"]);
                                if (from == null || !from.IsString)
                                    break;

                                switch (from.Value.ToLower().Replace(" ", "").Replace("_", ""))
                                {
                                    case "json": {
                                            var jnElements = InterfaceManager.inst.ParseVarFunction(jnElement["element_prefabs"]);
                                            if (jnElements == null)
                                                break;

                                            var to = InterfaceManager.inst.ParseVarFunction(jnElement["to"]);
                                            if (to == null)
                                                break;

                                            var elements = ParseElements(jnElements, prefabs, spriteAssets);

                                            for (int k = 0; k < to.Count; k++)
                                            {
                                                foreach (var toElement in to[k])
                                                {
                                                    foreach (var element in elements)
                                                    {
                                                        if (element.id != toElement.Key)
                                                            continue;

                                                        var toSpawnFunc = InterfaceManager.inst.ParseVarFunction(toElement.Value["spawn_if_func"], element);
                                                        if (toSpawnFunc != null && !InterfaceManager.inst.ParseIfFunction(toSpawnFunc, element))
                                                            continue;

                                                        var jnToElement = InterfaceManager.inst.ParseVarFunction(toElement.Value, element);
                                                        if (jnToElement == null)
                                                            continue;

                                                        element.Read(jnToElement, loopIndex, loop, spriteAssets);
                                                        yield return element;
                                                    }
                                                }
                                            }

                                            break;
                                        }
                                    case "storyjson": {
                                            var jnElements = InterfaceManager.inst.ParseVarFunction(jnElement["element_prefabs"]);
                                            if (jnElements == null)
                                                break;

                                            var elements = ParseElements(jnElements, prefabs, spriteAssets);

                                            var to = InterfaceManager.inst.ParseVarFunction(jnElement["to"]);
                                            if (to == null)
                                                break;

                                            var storyJN = Story.StoryManager.inst.CurrentSave.LoadJSON(to);
                                            for (int k = 0; k < to.Count; k++)
                                            {
                                                foreach (var toElement in to[k])
                                                {
                                                    foreach (var element in elements)
                                                    {
                                                        if (element.id != toElement.Key)
                                                            continue;

                                                        var toSpawnFunc = InterfaceManager.inst.ParseVarFunction(toElement.Value["spawn_if_func"], element);
                                                        if (toSpawnFunc != null && !InterfaceManager.inst.ParseIfFunction(toSpawnFunc, element))
                                                            continue;

                                                        element.Read(toElement.Value, loopIndex, loop, spriteAssets);
                                                        yield return element;
                                                    }
                                                }
                                            }

                                            break;
                                        }
                                    case "text": {
                                            var jnDefault = InterfaceManager.inst.ParseVarFunction(jnElement["default"]);
                                            if (jnDefault == null)
                                                break;

                                            var element = MenuText.Parse(jnDefault, loopIndex, loop, spriteAssets);

                                            var to = InterfaceManager.inst.ParseVarFunction(jnElement["to"]);
                                            if (to == null)
                                                break;

                                            for (int k = 0; k < to.Count; k++)
                                            {
                                                var copy = MenuText.DeepCopy(element);
                                                copy.text = to[k];
                                                yield return copy;
                                            }
                                            break;
                                        }
                                }

                                break;
                            }
                        case "event": {
                                var element = MenuEvent.Parse(jnElement, loopIndex, loop, spriteAssets);

                                var spawnFunc = InterfaceManager.inst.ParseVarFunction(jnElement["spawn_if_func"], element);
                                if (spawnFunc == null || InterfaceManager.inst.ParseIfFunction(spawnFunc, element))
                                    yield return element;

                                break;
                            }
                        case "image": {
                                var element = MenuImage.Parse(jnElement, loopIndex, loop, spriteAssets);

                                var spawnFunc = InterfaceManager.inst.ParseVarFunction(jnElement["spawn_if_func"], element);
                                if (spawnFunc == null || InterfaceManager.inst.ParseIfFunction(spawnFunc, element))
                                    yield return element;

                                break;
                            }
                        case "text": {
                                var element = MenuText.Parse(jnElement, loopIndex, loop, spriteAssets);

                                var spawnFunc = InterfaceManager.inst.ParseVarFunction(jnElement["spawn_if_func"], element);
                                if (spawnFunc == null || InterfaceManager.inst.ParseIfFunction(spawnFunc, element))
                                    yield return element;

                                break;
                            }
                        case "button": {
                                var element = MenuButton.Parse(jnElement, loopIndex, loop, spriteAssets);

                                var spawnFunc = InterfaceManager.inst.ParseVarFunction(jnElement["spawn_if_func"], element);
                                if (spawnFunc == null || InterfaceManager.inst.ParseIfFunction(spawnFunc, element))
                                    yield return element;

                                break;
                            }
                    }
            }
        }
    }
}
