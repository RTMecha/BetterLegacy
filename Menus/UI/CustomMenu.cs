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

namespace BetterLegacy.Menus.UI
{
    public class CustomMenu : MenuBase
    {
        public CustomMenu() : base(false)
        {

        }

        public override IEnumerator GenerateUI()
        {
            if (music)
                NewMenuManager.inst.PlayMusic(music);
            else if (RTFile.FileExists($"{Path.GetDirectoryName(filePath)}/{musicName}.ogg"))
            {
                CoreHelper.StartCoroutine(AlephNetworkManager.DownloadAudioClip($"{Path.GetDirectoryName(filePath)}/{musicName}.ogg", AudioType.OGGVORBIS, audioClip =>
                {
                    music = audioClip;
                    NewMenuManager.inst.PlayMusic(audioClip);
                }));
            }

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
                var parent = !string.IsNullOrEmpty(element.parentLayout) && layouts.ContainsKey(element.parentLayout) ? layouts[element.parentLayout].gameObject.transform : gameObject.transform;
                if (element is MenuButton menuButton)
                {
                    SetupButton(menuButton, parent);

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
                    while (menuText.isSpawning)
                        yield return null;
                }
                else
                {
                    SetupImage(element, parent);
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

        public string filePath;
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
                        case "image":
                            {
                                customMenu.elements.Add(new MenuImage
                                {
                                    name = jnElement["name"],
                                    parentLayout = jnElement["parent_layout"],
                                    icon = jnElement["icon"] != null ? SpriteManager.StringToSprite(jnElement["icon"]) : null,
                                    rectJSON = jnElement["rect"],
                                    color = jnElement["col"].AsInt,
                                    opacity = jnElement["opacity"] == null ? 1f : jnElement["opacity"].AsFloat,
                                    length = jnElement["anim_length"].AsFloat,
                                    playBlipSound = jnElement["play_blip_sound"].AsBool,
                                    funcJSON = jnElement["func"],
                                    reactiveSetting = ReactiveSetting.Parse(jnElement["reactive"], j),
                                    fromLoop = j > 0,
                                });
                                break;
                            }
                        case "text":
                            {
                                customMenu.elements.Add(new MenuText
                                {
                                    name = jnElement["name"],
                                    parentLayout = jnElement["parent_layout"],
                                    text = FontManager.inst.ReplaceProperties(jnElement["text"]),
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
                                    name = jnElement["name"],
                                    parentLayout = jnElement["parent_layout"],
                                    text = FontManager.inst.ReplaceProperties(jnElement["text"]),
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
