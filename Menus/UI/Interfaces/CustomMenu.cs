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
            customMenu.name = jn["name"];
            customMenu.musicName = string.IsNullOrEmpty(jn["music_name"]) ? InterfaceManager.RANDOM_MUSIC_NAME : jn["music_name"];
            customMenu.allowCustomMusic = jn["allow_custom_music"] != null ? jn["allow_custom_music"].AsBool : true;

            if (jn["type"] != null)
            {
                switch (jn["type"].Value.ToLower())
                {
                    case "chat": {
                            var returnInterface = jn["return_interface"];
                            var returnInterfacePath = jn["return_interface_path"];
                            var seen = jn["seen"];
                            var dialogueCount = jn["dialogue"].Count;

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
                            if (jn["defaults"] != null)
                            {
                                var jnDefaults = jn["defaults"];
                                if (jnDefaults["length"] != null)
                                    defaultLength = jnDefaults["length"].AsFloat;
                                if (!string.IsNullOrEmpty(jnDefaults["date_format"]))
                                    dateFormat = jnDefaults["date_format"];
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
                                var id = LSText.randomNumString(16);
                                var jnDialogue = jn["dialogue"][i];
                                string character = jnDialogue["character"];
                                string text = jnDialogue["text"];
                                string color = jnDialogue["color"];
                                string sound = jnDialogue["sound"];

                                forJN["to"][i]["1"] = new JSONObject { ["id"] = id, };
                                forJN["to"][i]["2"] = new JSONObject
                                {
                                    ["parent"] = id,
                                    ["text"] = $"| {character}",
                                };
                                forJN["to"][i]["3"] = new JSONObject { ["parent"] = id, };
                                if (jnDialogue["date_format"] != null)
                                    forJN["to"][i]["3"]["text"] = $"<align=right>{jnDialogue["date_format"].Value} |";

                                var jnText = new JSONObject
                                {
                                    ["parent"] = id,
                                    ["text"] = text
                                };
                                if (jnDialogue["length"] != null)
                                    jnText["length"] = jnDialogue["length"];
                                if (!string.IsNullOrEmpty(color))
                                    jnText["override_text_col"] = color;
                                if (!string.IsNullOrEmpty(sound))
                                    jnText["text_sound"] = sound;

                                if (jnDialogue["speeds"] != null)
                                {
                                    for (int j = 0; j < jnDialogue["speeds"].Count; j++)
                                    {
                                        jnText["speeds"][j]["position"] = jnDialogue["speeds"][j]["position"];
                                        jnText["speeds"][j]["speed"] = jnDialogue["speeds"][j]["speed"];
                                    }
                                }

                                if (jnDialogue["sound_volume"] != null)
                                    jnText["text_sound_volume"] = jnDialogue["sound_volume"];
                                if (jnDialogue["sound_pitch"] != null)
                                    jnText["text_sound_pitch"] = jnDialogue["sound_pitch"];
                                if (jnDialogue["sound_pitch_vary"] != null)
                                    jnText["text_sound_pitch_vary"] = jnDialogue["sound_pitch_vary"];
                                if (jnDialogue["sound_repeat"] != null)
                                    jnText["text_sound_repeat"] = jnDialogue["sound_repeat"];

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

            customMenu.defaultSelection = Parser.TryParse(jn["default_select"], Vector2Int.zero);
            customMenu.forceInterfaceSpeed = jn["force_speed"].AsBool;
            if (jn["layer"] != null)
                customMenu.layer = jn["layer"].AsInt;
            if (jn["pause_game"] != null)
                customMenu.pauseGame = jn["pause_game"].AsBool;
            else
                customMenu.pauseGame = true;

            if (jn["sprites"] != null)
            {
                foreach (var keyValuePair in jn["sprites"].Linq)
                {
                    if (customMenu.spriteAssets.ContainsKey(keyValuePair.Key))
                        continue;

                    customMenu.spriteAssets.Add(keyValuePair.Key, SpriteHelper.StringToSprite(keyValuePair.Value));
                }
            }

            customMenu.prefabs.AddRange(ParsePrefabs(jn["prefabs"]));
            ParseLayouts(customMenu.layouts, jn["layouts"]);
            customMenu.elements.AddRange(ParseElements(jn["elements"], customMenu.prefabs, customMenu.spriteAssets));

            if (jn["theme"] != null)
                customMenu.loadedTheme = BeatmapTheme.Parse(jn["theme"]);
            customMenu.useGameTheme = jn["game_theme"].AsBool;

            if (CoreHelper.InGame)
                customMenu.exitFunc = InterfaceManager.inst.CloseMenus;

            customMenu.exitFuncJSON = jn["exit_func"];

            return customMenu;
        }

        public static IEnumerable<MenuPrefab> ParsePrefabs(JSONNode jn)
        {
            if (jn == null || !jn.IsArray)
                yield break;

            for (int i = 0; i < jn.Count; i++)
                yield return MenuPrefab.Parse(jn[i]);
        }

        public static void ParseLayouts(Dictionary<string, MenuLayoutBase> layouts, JSONNode jn)
        {
            if (jn == null || !jn.IsObject)
                return;

            foreach (var keyValuePair in jn.Linq)
            {
                if (layouts.ContainsKey(keyValuePair.Key))
                    continue;

                var jnLayout = keyValuePair.Value;
                string layoutType = jnLayout["type"];
                switch (layoutType.ToLower())
                {
                    case "grid": {
                            var gridLayout = MenuGridLayout.Parse(jnLayout);
                            gridLayout.name = keyValuePair.Key;
                            layouts.Add(keyValuePair.Key, gridLayout);

                            break;
                        }
                    case "horizontal": {
                            var horizontalLayout = MenuHorizontalLayout.Parse(jnLayout);
                            horizontalLayout.name = keyValuePair.Key;
                            layouts.Add(keyValuePair.Key, horizontalLayout);

                            break;
                        }
                    case "vertical": {
                            var verticalLayout = MenuVerticalLayout.Parse(jnLayout);
                            verticalLayout.name = keyValuePair.Key;
                            layouts.Add(keyValuePair.Key, verticalLayout);

                            break;
                        }
                }
            }

        }

        public static IEnumerable<MenuImage> ParseElements(JSONNode jn, List<MenuPrefab> prefabs = null, Dictionary<string, Sprite> spriteAssets = null)
        {
            if (jn == null || !jn.IsArray)
                yield break;

            for (int i = 0; i < jn.Count; i++)
            {
                var jnElement = jn[i];
                string elementType = jnElement["type"];

                // loop function
                int loop = 1;
                if (jnElement["loop"] != null)
                    loop = jnElement["loop"].AsInt;
                if (loop < 1)
                    loop = 1;

                for (int j = 0; j < loop; j++)
                    switch (elementType.ToLower())
                    {
                        case "auto": {
                                if (jnElement["name"] == null)
                                    break;

                                string name = jnElement["name"];
                                IEnumerable<MenuImage> collection = null;
                                switch (name)
                                {
                                    case "TopBar": {
                                            collection = GenerateTopBar(jnElement["title"] == null ? "Custom Menu" : Lang.Parse(jnElement["title"]), jnElement["text_col"].AsInt, jnElement["text_val"].AsFloat);
                                            break;
                                        }
                                    case "BottomBar": {
                                            collection = GenerateBottomBar(jnElement["text_col"].AsInt, jnElement["text_val"].AsFloat);
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
                                if (prefabs == null || jnElement["id"] == null || !prefabs.TryFind(x => x.id == jnElement["id"], out MenuPrefab prefab))
                                    break;

                                if (jnElement["from"] != null)
                                {
                                    var from = jnElement["from"];
                                    switch (from["type"].Value.ToLower())
                                    {
                                        case "json": {
                                                if (from["array"] == null)
                                                    break;

                                                for (int k = 0; k < from["array"].Count; k++)
                                                {
                                                    var prefabObject = new MenuPrefabObject
                                                    {
                                                        prefabID = jnElement["pid"],
                                                        prefab = prefab,
                                                        id = jnElement["id"] == null ? LSText.randomNumString(16) : jnElement["id"],
                                                        name = jnElement["name"],
                                                        length = jnElement["anim_length"].AsFloat,
                                                        fromLoop = j > 0,
                                                        loop = loop,
                                                    };

                                                    if (jnElement["spawn_if_func"] == null || InterfaceManager.inst.ParseIfFunction(jnElement["spawn_if_func"], prefabObject))
                                                    {
                                                        foreach (var array in from["array"][k]["elements"])
                                                            prefabObject.elementSettings[array.Key] = array.Value;

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
                                    prefabID = jnElement["pid"],
                                    prefab = prefab,
                                    id = jnElement["id"] == null ? LSText.randomNumString(16) : jnElement["id"],
                                    name = jnElement["name"],
                                    length = jnElement["anim_length"].AsFloat, // how long the UI pauses for when this element spawns.
                                    fromLoop = j > 0, // if element has been spawned from the loop or if its the first / only of its kind.
                                    loop = loop,
                                };

                                if (jnElement["spawn_if_func"] == null || InterfaceManager.inst.ParseIfFunction(jnElement["spawn_if_func"], element))
                                    yield return element;

                                //for (int k = 0; k < prefab.elements.Count; k++)
                                //{
                                //    var element = prefab.elements[k];
                                //    if (element is MenuEvent menuEvent)
                                //    {
                                //        yield return MenuEvent.DeepCopy(menuEvent, false);
                                //        continue;
                                //    }
                                //    if (element is MenuText menuText)
                                //    {
                                //        yield return MenuText.DeepCopy(menuText, false);
                                //        continue;
                                //    }
                                //    if (element is MenuButton menuButton)
                                //    {
                                //        yield return MenuButton.DeepCopy(menuButton, false);
                                //        continue;
                                //    }

                                //    yield return MenuImage.DeepCopy(element, false);
                                //}

                                break;
                            }
                        case "for": {
                                string from = jnElement["from"];
                                switch (from.ToLower().Replace(" ", "").Replace("_", ""))
                                {
                                    case "json": {
                                            var elements = ParseElements(jnElement["element_prefabs"], prefabs, spriteAssets);

                                            for (int k = 0; k < jnElement["to"].Count; k++)
                                            {
                                                foreach (var toElement in jnElement["to"][k])
                                                {
                                                    foreach (var element in elements)
                                                    {
                                                        if (element.id != toElement.Key)
                                                            continue;

                                                        if (toElement.Value["spawn_if_func"] != null && !InterfaceManager.inst.ParseIfFunction(toElement.Value["spawn_if_func"], element))
                                                            continue;

                                                        element.Read(toElement.Value, j, loop, spriteAssets);
                                                        yield return element;
                                                    }
                                                }
                                            }

                                            break;
                                        }
                                    case "storyjson": {
                                            var elements = ParseElements(jnElement["element_prefabs"], prefabs, spriteAssets);

                                            var storyJN = Story.StoryManager.inst.CurrentSave.LoadJSON(jnElement["to"]);
                                            for (int k = 0; k < storyJN["to"].Count; k++)
                                            {
                                                foreach (var toElement in storyJN["to"][k])
                                                {
                                                    foreach (var element in elements)
                                                    {
                                                        if (element.id != toElement.Key)
                                                            continue;

                                                        if (toElement.Value["spawn_if_func"] != null && !InterfaceManager.inst.ParseIfFunction(toElement.Value["spawn_if_func"], element))
                                                            continue;

                                                        element.Read(toElement.Value, j, loop, spriteAssets);
                                                        yield return element;
                                                    }
                                                }
                                            }

                                            break;
                                        }
                                    case "text": {
                                            var element = MenuText.Parse(jnElement["default"], j, loop, spriteAssets);

                                            for (int k = 0; k < jnElement["to"].Count; k++)
                                            {
                                                var copy = MenuText.DeepCopy(element);
                                                copy.text = jnElement["to"][k];
                                                yield return copy;
                                            }
                                            break;
                                        }
                                }

                                break;
                            }
                        case "event": {
                                var element = MenuEvent.Parse(jnElement, j, loop, spriteAssets);

                                if (jnElement["spawn_if_func"] == null || InterfaceManager.inst.ParseIfFunction(jnElement["spawn_if_func"], element))
                                    yield return element;

                                break;
                            }
                        case "image": {
                                var element = MenuImage.Parse(jnElement, j, loop, spriteAssets);

                                if (jnElement["spawn_if_func"] == null || InterfaceManager.inst.ParseIfFunction(jnElement["spawn_if_func"], element))
                                    yield return element;

                                break;
                            }
                        case "text": {
                                var element = MenuText.Parse(jnElement, j, loop, spriteAssets);
                                if (jnElement["spawn_if_func"] == null || InterfaceManager.inst.ParseIfFunction(jnElement["spawn_if_func"], element))
                                    yield return element;

                                break;
                            }
                        case "button": {
                                var element = MenuButton.Parse(jnElement, j, loop, spriteAssets);

                                if (jnElement["spawn_if_func"] == null || InterfaceManager.inst.ParseIfFunction(jnElement["spawn_if_func"], element))
                                    yield return element;

                                break;
                            }
                    }
            }
        }
    }
}
