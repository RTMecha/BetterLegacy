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

        static bool currentCursorVisibility;

        public PauseMenu() : base()
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
                rect = RectValues.Default.AnchoredPosition(-500f, 200f).SizeDelta(800f, 200f),
            });

            layouts.Add("info", new MenuVerticalLayout
            {
                name = "info",
                childControlWidth = true,
                childForceExpandWidth = true,
                spacing = 4f,
                rect = RectValues.Default.AnchoredPosition(500f, 200f).SizeDelta(600f, 200f),
            });

            elements.Add(new MenuImage
            {
                id = "35255236785",
                name = "Background",
                siblingIndex = 0,
                rect = RectValues.FullAnchored,
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
                rect = RectValues.Default.AnchoredPosition(0f, 10000f).SizeDelta(256f, 256f).Rotation(45f),
                textRect = RectValues.FullAnchored.SizeDelta(-16f, -16f).Rotation(-45f),
                color = 0,
                opacity = 0.4f,
                val = 40f,
                textVal = 40f,
                length = 0f,
            });

            elements.AddRange(GenerateTopBar("Pause Menu"));

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
                CoreHelper.InStory ? "<b> [ QUIT TO MAIN MENU ]" : "<b> [ RETURN TO ARCADE ]", // 4
                "<b> [ QUIT GAME ]", // 5
            };
            Action[] actions = new Action[6]
            {
                UnPause, // 0
                () => ArcadeHelper.RestartLevel(UnPause), // 1
                SceneHelper.LoadEditorWithProgress, // 2
                ConfigManager.inst.Show, // 3
                ArcadeHelper.QuitToArcade, // 4
                Application.Quit, // 5
            };

            int num = 0;
            for (int i = 0; i < names.Length; i++)
            {
                var name = names[i];
                var text = texts[i];
                var action = actions[i];

                if (i == 5)
                {
                    elements.Add(new MenuImage
                    {
                        id = "5",
                        name = "Spacer",
                        parentLayout = "buttons",
                        rect = RectValues.Default.SizeDelta(200f, 64f),
                        opacity = 0f,
                        length = 0f,
                    });
                }

                if (i == 3 && ModCompatibility.UnityExplorerInstalled)
                {
                    elements.Add(new MenuButton
                    {
                        id = num.ToString(),
                        name = "Explorer Button",
                        text = "<b> [ SHOW EXPLORER ]",
                        parentLayout = "buttons",
                        selectionPosition = new Vector2Int(0, num),
                        rect = RectValues.Default.SizeDelta(200f, 64f),
                        opacity = 0.1f,
                        val = -40f,
                        textVal = 40f,
                        selectedOpacity = 1f,
                        selectedVal = 40f,
                        selectedTextVal = -40f,
                        length = 1f,
                        playBlipSound = true,
                        func = ModCompatibility.ShowExplorer,
                    });
                    num++;
                }

                if (i == 4 && LevelManager.Hub != null)
                {
                    elements.Add(new MenuButton
                    {
                        id = num.ToString(),
                        name = "Return Button",
                        text = "<b> [ RETURN TO HUB ]",
                        parentLayout = "buttons",
                        selectionPosition = new Vector2Int(0, num),
                        rect = RectValues.Default.SizeDelta(200f, 64f),
                        opacity = 0.1f,
                        val = -40f,
                        textVal = 40f,
                        selectedOpacity = 1f,
                        selectedVal = 40f,
                        selectedTextVal = -40f,
                        length = 1f,
                        playBlipSound = true,
                        func = ArcadeHelper.ReturnToHub,
                    });
                    num++;
                }

                if (i == 4 && CoreHelper.InStory)
                {
                    elements.Add(new MenuButton
                    {
                        id = num.ToString(),
                        name = "Return Button",
                        text = "<b> [ RETURN TO INTERFACE ]",
                        parentLayout = "buttons",
                        selectionPosition = new Vector2Int(0, num),
                        rect = RectValues.Default.SizeDelta(200f, 64f),
                        opacity = 0.1f,
                        val = -40f,
                        textVal = 40f,
                        selectedOpacity = 1f,
                        selectedVal = 40f,
                        selectedTextVal = -40f,
                        length = 1f,
                        playBlipSound = true,
                        func = SceneHelper.LoadInterfaceScene,
                    });
                    num++;
                }

                elements.Add(new MenuButton
                {
                    id = num.ToString(),
                    name = name,
                    text = text,
                    parentLayout = "buttons",
                    selectionPosition = new Vector2Int(0, num),
                    rect = RectValues.Default.SizeDelta(200f, 64f),
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
                id = "463472367",
                name = "Info Text",
                text = $"<align=right>Times hit: {GameManager.inst.hits.Count}",
                rect = RectValues.Default.SizeDelta(300f, 32f),
                hideBG = true,
                textVal = 40f,
                length = 0.3f,
                parentLayout = "info",
            });
            
            elements.Add(new MenuText
            {
                id = "738347853",
                name = "Info Text",
                text = $"<align=right>Times died: {GameManager.inst.deaths.Count}",
                rect = RectValues.Default.SizeDelta(300f, 32f),
                hideBG = true,
                textVal = 40f,
                length = 0.3f,
                parentLayout = "info",
            });

            var levelRank = LevelManager.GetLevelRank(LevelManager.CurrentLevel);

            elements.Add(new MenuText
            {
                id = "738347853",
                name = "Info Text",
                text = $"<align=right>Previous rank: <b><size=60><#{LSColors.ColorToHex(levelRank.color)}>{levelRank.name}</color>",
                rect = RectValues.Default.SizeDelta(300f, 32f),
                hideBG = true,
                textVal = 40f,
                length = 0.3f,
                parentLayout = "info",
            });

            levelRank = LevelManager.GetLevelRank(GameManager.inst.hits);

            elements.Add(new MenuText
            {
                id = "248576321",
                name = "Info Text",
                text = $"<align=right>Current rank: <b><size=60><#{LSColors.ColorToHex(levelRank.color)}>{levelRank.name}</color>",
                rect = RectValues.Default.SizeDelta(300f, 32f),
                hideBG = true,
                textVal = 40f,
                length = 0.3f,
                parentLayout = "info",
            });

            elements.Add(new MenuText
            {
                id = "738347853",
                name = "Info Text",
                text = $"<align=right>Time in level: {LevelManager.timeInLevel}",
                rect = RectValues.Default.SizeDelta(300f, 32f),
                hideBG = true,
                textVal = 40f,
                length = 0.3f,
                parentLayout = "info",
            });
            
            elements.Add(new MenuText
            {
                id = "738347853",
                name = "Info Text",
                text = $"<align=right>Level completed: {RTString.Percentage(AudioManager.inst.CurrentAudioSource.time, AudioManager.inst.CurrentAudioSource.clip.length)}%",
                rect = RectValues.Default.SizeDelta(300f, 32f),
                hideBG = true,
                textVal = 40f,
                length = 0.3f,
                parentLayout = "info",
            });

            if (!CoreHelper.InStory)
                elements.Add(new MenuText
                {
                    id = "7463783",
                    name = "Info Text",
                    text = $"<align=right>Difficulty mode: {PlayerManager.ChallengeMode}",
                    rect = RectValues.Default.SizeDelta(300f, 32f),
                    hideBG = true,
                    textVal = 40f,
                    length = 0.3f,
                    parentLayout = "info",
                });

            if (LevelManager.HasQueue)
            {
                elements.Add(new MenuText
                {
                    id = "7463783",
                    name = "Info Text",
                    text = $"<align=right>Queue: {LevelManager.currentQueueIndex + 1} / {LevelManager.ArcadeQueue.Count}",
                    rect = RectValues.Default.SizeDelta(300f, 32f),
                    hideBG = true,
                    textVal = 40f,
                    length = 0.3f,
                    parentLayout = "info",
                });
            }

            elements.AddRange(GenerateBottomBar());

            exitFunc = UnPause;

            InterfaceManager.inst.CurrentGenerateUICoroutine = CoreHelper.StartCoroutine(GenerateUI());
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
            elements.RemoveAll(x => x.id != "321" && x.id != "35255236785");

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
                yield return new WaitForSeconds(0.5f);
            }

            AudioManager.inst.PlaySound("blip");

            countdown.text = "<align=center><size=120><b><font=Fredoka One>GO!";
            countdown.textUI.text = "<align=center><size=120><b><font=Fredoka One>GO!";
            yield return new WaitForSeconds(0.2f);

            InterfaceManager.inst.CloseMenus();
            if (!currentCursorVisibility)
                LSHelpers.HideCursor();
            AudioManager.inst.CurrentAudioSource.UnPause();
            GameManager.inst.gameState = GameManager.State.Playing;

            yield break;
        }

        public static void Pause()
        {
            if (!CoreHelper.Playing)
                return;

            currentCursorVisibility = Cursor.visible;
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
