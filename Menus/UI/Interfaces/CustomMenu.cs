using BetterLegacy.Core.Data;
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
        public CustomMenu() : base(false) { }
        
        public override void UpdateTheme()
        {
            if (useGameTheme && CoreHelper.InGame)
                Theme = CoreHelper.CurrentBeatmapTheme;

            base.UpdateTheme();
        }

        public bool useGameTheme;

        /// <summary>
        /// The theme to choose from in the default themes list. If the value is -1, then it will use the players' preferred theme.
        /// </summary>
        public int currentTheme = -1;

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
            customMenu.musicName = jn["music_name"];

            foreach (var keyValuePair in jn["layouts"].Linq)
            {
                if (customMenu.layouts.ContainsKey(keyValuePair.Key))
                    continue;

                var jnLayout = keyValuePair.Value;
                string layoutType = jnLayout["type"];
                switch (layoutType.ToLower())
                {
                    case "grid":
                        {
                            var gridLayout = MenuGridLayout.Parse(jnLayout);
                            gridLayout.name = keyValuePair.Key;
                            customMenu.layouts.Add(keyValuePair.Key, gridLayout);

                            break;
                        }
                    case "horizontal":
                        {
                            var horizontalLayout = MenuHorizontalLayout.Parse(jnLayout);
                            horizontalLayout.name = keyValuePair.Key;
                            customMenu.layouts.Add(keyValuePair.Key, horizontalLayout);

                            break;
                        }
                    case "vertical":
                        {
                            var verticalLayout = MenuVerticalLayout.Parse(jnLayout);
                            verticalLayout.name = keyValuePair.Key;
                            customMenu.layouts.Add(keyValuePair.Key, verticalLayout);

                            break;
                        }
                }
            }

            for (int i = 0; i < jn["elements"].Count; i++)
            {
                var jnElement = jn["elements"][i];
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
                                    customMenu.elements.Add(element);
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
                                    icon = jnElement["icon"] != null ? SpriteManager.StringToSprite(jnElement["icon"]) : null,
                                    rectJSON = jnElement["rect"],
                                    color = jnElement["col"].AsInt,
                                    opacity = jnElement["opacity"] == null ? 1f : jnElement["opacity"].AsFloat,
                                    length = jnElement["anim_length"].AsFloat,
                                    playBlipSound = jnElement["play_blip_sound"].AsBool,
                                    rounded = jnElement["rounded"] == null ? 1 : jnElement["rounded"].AsInt, // roundness can be prevented by setting rounded to 0.
                                    roundedSide = jnElement["rounded_side"] == null ? SpriteManager.RoundedSide.W : (SpriteManager.RoundedSide)jnElement["rounded_side"].AsInt, // default side should be Whole.
                                    funcJSON = jnElement["func"], // function to run when the element is clicked.
                                    spawnFuncJSON = jnElement["spawn_func"], // function to run when the element spawns.
                                    reactiveSetting = ReactiveSetting.Parse(jnElement["reactive"], j),
                                    fromLoop = j > 0, // if element has been spawned from the loop or if its the first / only of its kind.
                                    loop = loop,
                                };

                                if (jnElement["spawn_if_func"] == null || element.ParseIfFunction(jnElement["spawn_if_func"]))
                                    customMenu.elements.Add(element);
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
                                    icon = jnElement["icon"] != null ? SpriteManager.StringToSprite(jnElement["icon"]) : null,
                                    rectJSON = jnElement["rect"],
                                    textRectJSON = jnElement["text_rect"],
                                    iconRectJSON = jnElement["icon_rect"],
                                    hideBG = jnElement["hide_bg"].AsBool,
                                    color = jnElement["col"].AsInt,
                                    opacity = jnElement["opacity"] == null ? 1f : jnElement["opacity"].AsFloat,
                                    textColor = jnElement["text_col"].AsInt,
                                    length = jnElement["anim_length"].AsFloat,
                                    playBlipSound = jnElement["play_blip_sound"].AsBool,
                                    rounded = jnElement["rounded"] == null ? 1 : jnElement["rounded"].AsInt, // roundness can be prevented by setting rounded to 0.
                                    roundedSide = jnElement["rounded_side"] == null ? SpriteManager.RoundedSide.W : (SpriteManager.RoundedSide)jnElement["rounded_side"].AsInt, // default side should be Whole.
                                    funcJSON = jnElement["func"], // function to run when the element is clicked.
                                    spawnFuncJSON = jnElement["spawn_func"], // function to run when the element spawns.
                                    reactiveSetting = ReactiveSetting.Parse(jnElement["reactive"], j),
                                    fromLoop = j > 0, // if element has been spawned from the loop or if its the first / only of its kind.
                                    loop = loop,
                                };

                                if (jnElement["spawn_if_func"] == null || element.ParseIfFunction(jnElement["spawn_if_func"]))
                                    customMenu.elements.Add(element);

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
                                    icon = jnElement["icon"] != null ? SpriteManager.StringToSprite(jnElement["icon"]) : null,
                                    rectJSON = jnElement["rect"],
                                    textRectJSON = jnElement["text_rect"],
                                    iconRectJSON = jnElement["icon_rect"],
                                    hideBG = jnElement["hide_bg"].AsBool,
                                    color = jnElement["col"].AsInt,
                                    opacity = jnElement["opacity"] == null ? 1f : jnElement["opacity"].AsFloat,
                                    selectedOpacity = jnElement["sel_opacity"] == null ? 1f : jnElement["sel_opacity"].AsFloat,
                                    textColor = jnElement["text_col"].AsInt,
                                    selectedColor = jnElement["sel_col"].AsInt,
                                    selectedTextColor = jnElement["sel_text_col"].AsInt,
                                    length = jnElement["anim_length"].AsFloat,
                                    playBlipSound = jnElement["play_blip_sound"].AsBool,
                                    rounded = jnElement["rounded"] == null ? 1 : jnElement["rounded"].AsInt, // roundness can be prevented by setting rounded to 0.
                                    roundedSide = jnElement["rounded_side"] == null ? SpriteManager.RoundedSide.W : (SpriteManager.RoundedSide)jnElement["rounded_side"].AsInt, // default side should be Whole.
                                    funcJSON = jnElement["func"], // function to run when the element is clicked.
                                    spawnFuncJSON = jnElement["spawn_func"], // function to run when the element spawns.
                                    reactiveSetting = ReactiveSetting.Parse(jnElement["reactive"], j),
                                    fromLoop = j > 0, // if element has been spawned from the loop or if its the first / only of its kind.
                                    loop = loop,
                                };

                                if (jnElement["spawn_if_func"] == null || element.ParseIfFunction(jnElement["spawn_if_func"]))
                                    customMenu.elements.Add(element);

                                break;
                            }
                    }
            }

            customMenu.Theme = BeatmapTheme.Parse(jn["theme"]);
            customMenu.useGameTheme = jn["game_theme"].AsBool;

            return customMenu;
        }
    }
}
