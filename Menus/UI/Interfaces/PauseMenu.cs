﻿using BetterLegacy.Components;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using TMPro;
using BetterLegacy.Core.Data;
using LSFunctions;
using System.IO;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Configs;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Layouts;

namespace BetterLegacy.Menus.UI.Interfaces
{
    /// <summary>
    /// Menu used for pausing in-game.
    /// </summary>
    public class PauseMenu : MenuBase
    {
        public static PauseMenu Current { get; set; }

        public PauseMenu() : base(false)
        {
            if (!CoreHelper.InGame || CoreHelper.InEditor)
            {
                CoreHelper.LogError($"Cannot pause outside of the game!");
                return;
            }

            InterfaceManager.inst.CurrentMenu = this;

            layouts.Add("buttons", new MenuVerticalLayout
            {
                name = "buttons",
                childControlWidth = true,
                childForceExpandWidth = true,
                spacing = 4f,
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(-500f, 200f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(800f, 200f)),
            });

            elements.Add(new MenuImage
            {
                id = "35255236785",
                name = "Background",
                siblingIndex = 0,
                rectJSON = MenuImage.GenerateRectTransformJSON(Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero),
                color = 0,
                val = -999f,
                opacity = 0.7f,
                length = 0f,
            });

            elements.Add(new MenuText
            {
                id = "321",
                name = "Countdown",
                text = "<size=100>3",
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(0f, 10000f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(256f, 256f), 45f),
                textRectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-16f, -16f), -45f),
                color = 0,
                opacity = 0.4f,
                val = 40f,
                textVal = 40f,
                length = 0f,
            });

            elements.Add(new MenuText
            {
                id = "264726346",
                name = "Top Title",
                text = $"Pause Menu | BetterLegacy {LegacyPlugin.ModVersion}",
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(0f, 460f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 100f)),
                textRectJSON = SimpleJSON.JSON.Parse("{\"anc_pos\": { \"x\": \"-850\",\"y\": \"0\" } }"),
                hideBG = true,
                textVal = 40f,
                length = 0.6f,
            });
            
            elements.Add(new MenuText
            {
                id = "800",
                name = "Top Bar",
                text = "<size=56>----------------------------------------------------------------",
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(0f, 400f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 100f)),
                textRectJSON = SimpleJSON.JSON.Parse("{\"anc_pos\": { \"x\": \"-870\",\"y\": \"0\" } }"),
                hideBG = true,
                textVal = 40f,
                length = 0.6f,
            });

            string[] names = new string[6]
            {
                "Continue Button", // 0
                "Restart Button", // 1
                "Editor Button", // 2
                "Config Button", // 3
                "Arcade Button", // 4
                "Exit Button", // 5
            };
            string[] texts = new string[6]
            {
                "<b> [ CONTINUE ]", // 0
                "<b> [ RESTART ]", // 1
                "<b> [ EDITOR ]", // 2
                "<b> [ CONFIG ]", // 3
                "<b> [ RETURN TO ARCADE ]", // 4
                "<b> [ QUIT GAME ]", // 5
            };
            Action[] actions = new Action[6]
            {
                UnPause, // 0
                RestartLevel, // 1
                () => { SceneManager.inst.LoadScene("Editor", true); }, // 2
                ConfigManager.inst.Show, // 3
                GameManager.inst.QuitToArcade, // 4
                Application.Quit, // 5
            };

            int num = 0;
            for (int i = 0; i < names.Length; i++)
            {
                var name = names[i];
                var text = texts[i];
                var action = actions[i];

                if (i == 4 && CoreHelper.InStory)
                    continue;

                if (i == 5)
                {
                    elements.Add(new MenuImage
                    {
                        id = "5",
                        name = "Spacer",
                        parentLayout = "buttons",
                        rectJSON = SimpleJSON.JSON.Parse("{\"size\": { \"x\": \"200\",\"y\": \"64\" } }"),
                        opacity = 0f,
                        length = 0f,
                    });
                }

                elements.Add(new MenuButton
                {
                    id = num.ToString(),
                    name = name,
                    text = text,
                    parentLayout = "buttons",
                    selectionPosition = new Vector2Int(0, num),
                    rectJSON = SimpleJSON.JSON.Parse("{\"size\": { \"x\": \"200\",\"y\": \"64\" } }"),
                    opacity = 0.1f,
                    val = -40f,
                    textVal = 40f,
                    selectedOpacity = 1f,
                    selectedVal = 40f,
                    selectedTextVal = -40f,
                    length = 1f,
                    playBlipSound = true,
                    func = action,
                });
                num++;
            }

            elements.Add(new MenuText
            {
                id = "801",
                name = "Bottom Bar",
                text = "<size=56>----------------------------------------------------------------",
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(0f, -400f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 100f)),
                textRectJSON = SimpleJSON.JSON.Parse("{\"anc_pos\": { \"x\": \"-870\",\"y\": \"0\" } }"),
                hideBG = true,
                textVal = 40f,
                length = 0.6f,
            });
            
            elements.Add(new MenuText
            {
                id = "264726346",
                name = "Bottom Title",
                text = $"<align=right><#F05355><b>Project Arrhythmia</b></color> | Unified Operating System | Version {ProjectArrhythmia.GameVersion}",
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(0f, -460f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 100f)),
                textRectJSON = SimpleJSON.JSON.Parse("{\"anc_pos\": { \"x\": \"850\",\"y\": \"0\" } }"),
                hideBG = true,
                textVal = 40f,
                length = 0.6f,
            });

            CoreHelper.StartCoroutine(GenerateUI());
        }

        public override void UpdateTheme()
        {
            Theme = CoreHelper.CurrentBeatmapTheme;

            base.UpdateTheme();
        }

        IEnumerator StartCountdown()
        {
            MenuText countdown = null;
            for (int i = 0; i < elements.Count; i++)
            {
                if (elements[i].id == "321" && elements[i] is MenuText menuText)
                {
                    countdown = menuText;
                    continue;
                }
                if (elements[i].id == "35255236785")
                    continue;

                elements[i].Clear();
                UnityEngine.Object.Destroy(elements[i].gameObject);
            }

            for (int i = 3; i > 0; i--)
            {
                if (countdown == null || !countdown.textUI)
                    continue;

                countdown.gameObject.transform.AsRT().localPosition = Vector3.zero;

                if (i != 3)
                    AudioManager.inst.PlaySound("blip");

                var num = $"<align=center><size=120><b><font=Fredoka One>{i}";
                countdown.text = num;
                countdown.textUI.text = num;
                countdown.textUI.maxVisibleCharacters = 9999;
                yield return new WaitForSeconds(1f);
            }

            AudioManager.inst.PlaySound("blip");

            countdown.text = "<align=center><size=120><b><font=Fredoka One>GO!";
            countdown.textUI.text = "<align=center><size=120><b><font=Fredoka One>GO!";
            yield return new WaitForSeconds(1f);

            Current?.Clear();
            Current = null;
            if (InterfaceManager.inst.CurrentMenu is PauseMenu)
                InterfaceManager.inst.CurrentMenu = null;
            LSHelpers.HideCursor();
            AudioManager.inst.CurrentAudioSource.UnPause();
            GameManager.inst.gameState = GameManager.State.Playing;

            yield break;
        }

        public static void RestartLevel()
        {
            if (CoreHelper.InEditor)
                return;

            if (ArcadeHelper.endedLevel)
            {
                LevelManager.current -= 1;
                AudioManager.inst.SetMusicTime(0f);
                GameManager.inst.hits.Clear();
                GameManager.inst.deaths.Clear();
                ArcadeHelper.endedLevel = false;

                UnPause();
                LevelManager.LevelEnded = false;

                return;
            }

            AudioManager.inst.SetMusicTime(0f);
            GameManager.inst.hits.Clear();
            GameManager.inst.deaths.Clear();
            UnPause();
            ArcadeHelper.endedLevel = false;
        }

        public static void Pause()
        {
            if (!CoreHelper.Playing)
                return;

            LSHelpers.ShowCursor();
            AudioManager.inst.CurrentAudioSource.Pause();
            InputDataManager.inst.SetAllControllerRumble(0f);
            GameManager.inst.gameState = GameManager.State.Paused;
            ArcadeHelper.endedLevel = false;
            Current = new PauseMenu();
        }

        public static void UnPause()
        {
            if (!CoreHelper.Paused || Current == null)
                return;

            CoreHelper.StartCoroutine(Current.StartCountdown());
        }
    }
}
