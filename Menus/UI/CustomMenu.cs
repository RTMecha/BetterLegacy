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

namespace BetterLegacy.Menus.UI
{
    public class CustomMenu : MenuBase
    {
        public CustomMenu() : base(false)
        {

        }

        public override IEnumerator GenerateUI()
        {
            NewMenuManager.inst.PlayMusic();

            var canvas = UIManager.GenerateUICanvas(nameof(CustomMenu), null, sortingOrder: 900);
            this.canvas = canvas;
            canvas.Canvas.scaleFactor = 1f;
            canvas.CanvasScaler.referenceResolution = new Vector2(1920f, 1080f);

            var gameObject = Creator.NewUIObject("Base Layout", canvas.Canvas.transform);
            UIManager.SetRectTransform(gameObject.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);

            for (int i = 0; i < layouts.Count; i++)
            {
                var layout = layouts.ElementAt(i).Value;
                if (layout is MenuGridLayout gridLayout)
                {
                    SetupGridLayout(gridLayout, gameObject.transform);
                }
                if (layout is MenuHorizontalLayout horizontalLayout)
                {
                    SetupHorizontalLayout(horizontalLayout, gameObject.transform);
                }
                if (layout is MenuVerticalLayout verticalLayout)
                {
                    SetupVerticalLayout(verticalLayout, gameObject.transform);
                }
            }

            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];

                if (element is MenuEvent menuEvent)
                {
                    menuEvent.TriggerEvent();
                    while (menuEvent.isSpawning)
                        yield return null;

                    continue;
                }

                var parent = !string.IsNullOrEmpty(element.parentLayout) && layouts.ContainsKey(element.parentLayout) ? layouts[element.parentLayout].gameObject.transform : !string.IsNullOrEmpty(element.parent) && elements.TryFind(x => x.id == element.parent, out MenuImage menuParent) && menuParent.gameObject ? menuParent.gameObject.transform : gameObject.transform;

                if (element is MenuButton menuButton)
                {
                    SetupButton(menuButton, parent);
                    if (menuButton.siblingIndex >= 0 && menuButton.siblingIndex < menuButton.gameObject.transform.parent.childCount)
                        menuButton.gameObject.transform.SetSiblingIndex(menuButton.siblingIndex);

                    while (element.isSpawning)
                        yield return null;

                    menuButton.clickable.onClick = p =>
                    {
                        if (menuButton.playBlipSound)
                            AudioManager.inst.PlaySound("blip");
                        menuButton.ParseFunction(menuButton.funcJSON);
                    };

                    continue;
                }

                if (element is MenuText menuText)
                {
                    SetupText(menuText, parent);
                    if (menuText.siblingIndex >= 0 && menuText.siblingIndex < menuText.gameObject.transform.parent.childCount)
                        menuText.gameObject.transform.SetSiblingIndex(menuText.siblingIndex);
                    while (menuText.isSpawning)
                        yield return null;
                }
                else
                {
                    SetupImage(element, parent);
                    if (element.siblingIndex >= 0 && element.siblingIndex < element.gameObject.transform.parent.childCount)
                        element.gameObject.transform.SetSiblingIndex(element.siblingIndex);
                    while (element.isSpawning)
                        yield return null;
                }

                element.clickable.onClick = p =>
                {
                    if (element.playBlipSound)
                        AudioManager.inst.PlaySound("blip");
                    element.ParseFunction(element.funcJSON);
                };
            }

            if (elements.TryFind(x => x is MenuButton, out MenuImage menuImage) && menuImage is MenuButton button)
            {
                button.OnEnter();
            }

            isOpen = true;

            yield break;
        }

        public override void UpdateTheme()
        {
            if (useGameTheme && CoreHelper.InGame)
                Theme = CoreHelper.CurrentBeatmapTheme;

            base.UpdateTheme();
        }

        public bool useGameTheme;

        public override BeatmapTheme Theme { get; set; }

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
                                customMenu.elements.Add(new MenuEvent
                                {
                                    id = jnElement["id"] == null ? LSText.randomNumString(16) : jnElement["id"],
                                    name = jnElement["name"],
                                    length = jnElement["anim_length"].AsFloat,
                                    funcJSON = jnElement["func"],
                                    fromLoop = j > 0,
                                });
                                break;
                            }
                        case "image":
                            {
                                customMenu.elements.Add(new MenuImage
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
                                    rounded = jnElement["rounded"] == null ? 1 : jnElement["rounded"].AsInt,
                                    roundedSide = jnElement["rounded_side"] == null ? SpriteManager.RoundedSide.W : (SpriteManager.RoundedSide)jnElement["rounded_side"].AsInt,
                                    funcJSON = jnElement["func"],
                                    reactiveSetting = ReactiveSetting.Parse(jnElement["reactive"], j),
                                    fromLoop = j > 0,
                                }); ;
                                break;
                            }
                        case "text":
                            {
                                customMenu.elements.Add(new MenuText
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
                                    rounded = jnElement["rounded"] == null ? 1 : jnElement["rounded"].AsInt,
                                    roundedSide = jnElement["rounded_side"] == null ? SpriteManager.RoundedSide.W : (SpriteManager.RoundedSide)jnElement["rounded_side"].AsInt,
                                    funcJSON = jnElement["func"],
                                    reactiveSetting = ReactiveSetting.Parse(jnElement["reactive"], j),
                                    fromLoop = j > 0,
                                });

                                break;
                            }
                        case "button":
                            {
                                customMenu.elements.Add(new MenuButton
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
                                    rounded = jnElement["rounded"] == null ? 1 : jnElement["rounded"].AsInt,
                                    roundedSide = jnElement["rounded_side"] == null ? SpriteManager.RoundedSide.W : (SpriteManager.RoundedSide)jnElement["rounded_side"].AsInt,
                                    funcJSON = jnElement["func"],
                                    reactiveSetting = ReactiveSetting.Parse(jnElement["reactive"], j),
                                    fromLoop = j > 0,
                                });

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
