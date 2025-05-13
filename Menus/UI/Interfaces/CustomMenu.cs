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

                                            var storyJN = Story.StoryManager.inst.LoadJSON(jnElement["to"]);
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
