﻿using BetterLegacy.Core.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SimpleJSON;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core;
using UnityEngine;
using BetterLegacy.Core.Helpers;
using System.IO;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Configs;
using LSFunctions;
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
            else if (Parser.TryParse(MenuConfig.Instance.InterfaceThemeID.Value, -1) >= 0 && InterfaceManager.inst.themes.TryFind(x => x.id == MenuConfig.Instance.InterfaceThemeID.Value, out BeatmapTheme interfaceTheme))
                Theme = interfaceTheme;
            else
                Theme = InterfaceManager.inst.themes[0];

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

        public Dictionary<string, Sprite> spriteAssets = new Dictionary<string, Sprite>();

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
            customMenu.layer = jn["layer"] != null ? jn["layer"].AsInt : 900;

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
                    case "grid":
                        {
                            var gridLayout = MenuGridLayout.Parse(jnLayout);
                            gridLayout.name = keyValuePair.Key;
                            layouts.Add(keyValuePair.Key, gridLayout);

                            break;
                        }
                    case "horizontal":
                        {
                            var horizontalLayout = MenuHorizontalLayout.Parse(jnLayout);
                            horizontalLayout.name = keyValuePair.Key;
                            layouts.Add(keyValuePair.Key, horizontalLayout);

                            break;
                        }
                    case "vertical":
                        {
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
                        case "auto":
                            {
                                if (jnElement["name"] == null)
                                    break;

                                string name = jnElement["name"];
                                IEnumerable<MenuImage> collection = null;
                                switch (name)
                                {
                                    case "TopBar":
                                        {
                                            collection = GenerateTopBar(jnElement["title"] == null ? "Custom Menu" : jnElement["title"], jnElement["text_col"].AsInt, jnElement["text_val"].AsFloat);
                                            break;
                                        }
                                    case "BottomBar":
                                        {
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
                        case "prefab":
                            {
                                if (prefabs == null || jnElement["id"] == null || !prefabs.TryFind(x => x.id == jnElement["id"], out MenuPrefab prefab))
                                    break;

                                var element = new MenuPrefabObject
                                {
                                    prefabID = jnElement["id"],
                                    prefab = prefab,
                                    id = jnElement["id"] == null ? LSText.randomNumString(16) : jnElement["id"],
                                    name = jnElement["name"],
                                    length = jnElement["anim_length"].AsFloat, // how long the UI pauses for when this element spawns.
                                    fromLoop = j > 0, // if element has been spawned from the loop or if its the first / only of its kind.
                                    loop = loop,
                                };

                                if (jnElement["spawn_if_func"] == null || element.ParseIfFunction(jnElement["spawn_if_func"]))
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
                        case "event":
                            {
                                var element = new MenuEvent
                                {
                                    id = jnElement["id"] == null ? LSText.randomNumString(16) : jnElement["id"],
                                    name = jnElement["name"],
                                    length = jnElement["anim_length"].AsFloat, // how long the UI pauses for when this element spawns.
                                    funcJSON = jnElement["func"], // the function to run.
                                    fromLoop = j > 0, // if element has been spawned from the loop or if its the first / only of its kind.
                                    loop = loop,
                                };

                                if (jnElement["spawn_if_func"] == null || element.ParseIfFunction(jnElement["spawn_if_func"]))
                                    yield return element;
                                break;
                            }
                        case "image":
                            {
                                var element = new MenuImage
                                {
                                    id = jnElement["id"] == null ? LSText.randomNumString(16) : jnElement["id"],
                                    name = jnElement["name"],
                                    parentLayout = jnElement["parent_layout"],
                                    parent = jnElement["parent"],
                                    siblingIndex = jnElement["sibling_index"] == null ? -1 : jnElement["sibling_index"].AsInt,
                                    icon = jnElement["icon"] != null ? spriteAssets.TryGetValue(jnElement["icon"], out Sprite sprite) ? sprite : SpriteHelper.StringToSprite(jnElement["icon"]) : null,
                                    rect = RectValues.TryParse(jnElement["rect"], RectValues.Default),
                                    color = jnElement["col"].AsInt,
                                    opacity = jnElement["opacity"] == null ? 1f : jnElement["opacity"].AsFloat,
                                    hue = jnElement["hue"].AsFloat,
                                    sat = jnElement["sat"].AsFloat,
                                    val = jnElement["val"].AsFloat,
                                    length = jnElement["anim_length"].AsFloat,
                                    playBlipSound = jnElement["play_blip_sound"].AsBool,
                                    rounded = jnElement["rounded"] == null ? 1 : jnElement["rounded"].AsInt, // roundness can be prevented by setting rounded to 0.
                                    roundedSide = jnElement["rounded_side"] == null ? SpriteHelper.RoundedSide.W : (SpriteHelper.RoundedSide)jnElement["rounded_side"].AsInt, // default side should be Whole.
                                    funcJSON = jnElement["func"], // function to run when the element is clicked.
                                    spawnFuncJSON = jnElement["spawn_func"], // function to run when the element spawns.
                                    reactiveSetting = ReactiveSetting.Parse(jnElement["reactive"], j),
                                    mask = jnElement["mask"].AsBool,
                                    fromLoop = j > 0, // if element has been spawned from the loop or if its the first / only of its kind.
                                    loop = loop,
                                };

                                if (jnElement["spawn_if_func"] == null || element.ParseIfFunction(jnElement["spawn_if_func"]))
                                    yield return element;
                                break;
                            }
                        case "text":
                            {
                                var element = new MenuText
                                {
                                    id = jnElement["id"] == null ? LSText.randomNumString(16) : jnElement["id"],
                                    name = jnElement["name"],
                                    parentLayout = jnElement["parent_layout"],
                                    parent = jnElement["parent"],
                                    siblingIndex = jnElement["sibling_index"] == null ? -1 : jnElement["sibling_index"].AsInt,
                                    text = FontManager.TextTranslater.ReplaceProperties(jnElement["text"]),
                                    icon = jnElement["icon"] != null ? spriteAssets.TryGetValue(jnElement["icon"], out Sprite sprite) ? sprite : SpriteHelper.StringToSprite(jnElement["icon"]) : null,
                                    rect = RectValues.TryParse(jnElement["rect"], RectValues.Default),
                                    textRect = RectValues.TryParse(jnElement["text_rect"], RectValues.FullAnchored),
                                    iconRect = RectValues.TryParse(jnElement["icon_rect"], RectValues.Default),
                                    hideBG = jnElement["hide_bg"].AsBool,
                                    color = jnElement["col"].AsInt,
                                    opacity = jnElement["opacity"] == null ? 1f : jnElement["opacity"].AsFloat,
                                    hue = jnElement["hue"].AsFloat,
                                    sat = jnElement["sat"].AsFloat,
                                    val = jnElement["val"].AsFloat,
                                    textColor = jnElement["text_col"].AsInt,
                                    textHue = jnElement["text_hue"].AsFloat,
                                    textSat = jnElement["text_sat"].AsFloat,
                                    textVal = jnElement["text_val"].AsFloat,
                                    length = jnElement["anim_length"].AsFloat,
                                    playBlipSound = jnElement["play_blip_sound"].AsBool,
                                    rounded = jnElement["rounded"] == null ? 1 : jnElement["rounded"].AsInt, // roundness can be prevented by setting rounded to 0.
                                    roundedSide = jnElement["rounded_side"] == null ? SpriteHelper.RoundedSide.W : (SpriteHelper.RoundedSide)jnElement["rounded_side"].AsInt, // default side should be Whole.
                                    funcJSON = jnElement["func"], // function to run when the element is clicked.
                                    spawnFuncJSON = jnElement["spawn_func"], // function to run when the element spawns.
                                    reactiveSetting = ReactiveSetting.Parse(jnElement["reactive"], j),
                                    mask = jnElement["mask"].AsBool,
                                    fromLoop = j > 0, // if element has been spawned from the loop or if its the first / only of its kind.
                                    loop = loop,
                                };

                                if (jnElement["spawn_if_func"] == null || element.ParseIfFunction(jnElement["spawn_if_func"]))
                                    yield return element;

                                break;
                            }
                        case "button":
                            {
                                var element = new MenuButton
                                {
                                    id = jnElement["id"] == null ? LSText.randomNumString(16) : jnElement["id"],
                                    name = jnElement["name"],
                                    parentLayout = jnElement["parent_layout"],
                                    parent = jnElement["parent"],
                                    siblingIndex = jnElement["sibling_index"] == null ? -1 : jnElement["sibling_index"].AsInt,
                                    text = FontManager.TextTranslater.ReplaceProperties(jnElement["text"]),
                                    selectionPosition = new Vector2Int(jnElement["select"]["x"].AsInt, jnElement["select"]["y"].AsInt),
                                    autoAlignSelectionPosition = jnElement["align_select"].AsBool,
                                    icon = jnElement["icon"] != null ? spriteAssets.TryGetValue(jnElement["icon"], out Sprite sprite) ? sprite : SpriteHelper.StringToSprite(jnElement["icon"]) : null,
                                    rect = RectValues.TryParse(jnElement["rect"], RectValues.Default),
                                    textRect = RectValues.TryParse(jnElement["text_rect"], RectValues.FullAnchored),
                                    iconRect = RectValues.TryParse(jnElement["icon_rect"], RectValues.Default),
                                    hideBG = jnElement["hide_bg"].AsBool,
                                    color = jnElement["col"].AsInt,
                                    opacity = jnElement["opacity"] == null ? 1f : jnElement["opacity"].AsFloat,
                                    hue = jnElement["hue"].AsFloat,
                                    sat = jnElement["sat"].AsFloat,
                                    val = jnElement["val"].AsFloat,
                                    textColor = jnElement["text_col"].AsInt,
                                    textHue = jnElement["text_hue"].AsFloat,
                                    textSat = jnElement["text_sat"].AsFloat,
                                    textVal = jnElement["text_val"].AsFloat,
                                    selectedColor = jnElement["sel_col"].AsInt,
                                    selectedOpacity = jnElement["sel_opacity"] == null ? 1f : jnElement["sel_opacity"].AsFloat,
                                    selectedHue = jnElement["sel_hue"].AsFloat,
                                    selectedSat = jnElement["sel_sat"].AsFloat,
                                    selectedVal = jnElement["sel_val"].AsFloat,
                                    selectedTextColor = jnElement["sel_text_col"].AsInt,
                                    selectedTextHue = jnElement["sel_text_hue"].AsFloat,
                                    selectedTextSat = jnElement["sel_text_sat"].AsFloat,
                                    selectedTextVal = jnElement["sel_text_val"].AsFloat,
                                    length = jnElement["anim_length"].AsFloat,
                                    playBlipSound = jnElement["play_blip_sound"].AsBool,
                                    rounded = jnElement["rounded"] == null ? 1 : jnElement["rounded"].AsInt, // roundness can be prevented by setting rounded to 0.
                                    roundedSide = jnElement["rounded_side"] == null ? SpriteHelper.RoundedSide.W : (SpriteHelper.RoundedSide)jnElement["rounded_side"].AsInt, // default side should be Whole.
                                    funcJSON = jnElement["func"], // function to run when the element is clicked.
                                    spawnFuncJSON = jnElement["spawn_func"], // function to run when the element spawns.
                                    enterFuncJSON = jnElement["enter_func"], // function to run when the element is hovered over.
                                    exitFuncJSON = jnElement["exit_func"], // function to run when the element is hovered over.
                                    reactiveSetting = ReactiveSetting.Parse(jnElement["reactive"], j),
                                    mask = jnElement["mask"].AsBool,
                                    fromLoop = j > 0, // if element has been spawned from the loop or if its the first / only of its kind.
                                    loop = loop,
                                };

                                if (jnElement["spawn_if_func"] == null || element.ParseIfFunction(jnElement["spawn_if_func"]))
                                    yield return element;

                                break;
                            }
                    }
            }
        }
    }
}
