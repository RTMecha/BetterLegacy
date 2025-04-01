using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using LSFunctions;

using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Menus;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Layouts;
using BetterLegacy.Menus.UI.Interfaces;

namespace BetterLegacy.Arcade.Interfaces
{
    public class InputSelectMenu : MenuBase
    {
        public static InputSelectMenu Current { get; set; }

        public static void Init()
        {
            InputDataManager.inst.BindMenuKeys();
            InputDataManager.inst.ClearInputs();
            ArcadeHelper.fromLevel = false;
            InputDataManager.inst.playersCanJoin = true;

            if (MenuConfig.Instance.PlayInputSelectMusic.Value)
            {
                SoundManager.inst.PlayMusic(DefaultMusic.loading);
                CoreHelper.Notify($"Now playing: Creo - Staring Down the Barrels", InterfaceManager.inst.CurrentTheme.guiColor);
            }

            Current = new InputSelectMenu();
            InterfaceManager.inst.CurrentInterface = Current;
        }

        public InputSelectMenu()
        {
            name = "Input Select";
            regenerate = false;

            elements.AddRange(GenerateTopBar("Input Select | Specify Simulations", 6, 0f, false));

            layouts.Add("desc", new MenuVerticalLayout
            {
                name = "desc",
                rect = RectValues.Default.AnchoredPosition(-700f, 140f).SizeDelta(400f, 400f),
                regenerate = false,
            });

            elements.Add(new MenuText
            {
                id = LSText.randomNumString(16),
                name = "Text",
                text = "[BACK] or [ESCAPE] to return to previous menu.",
                parentLayout = "desc",
                length = 0.01f,
                rect = RectValues.Default.SizeDelta(300f, 46f),
                hideBG = true,
                textColor = 6,
                regenerate = false,
            });

            elements.Add(new MenuText
            {
                id = LSText.randomNumString(16),
                name = "Text",
                text = "[BOOST] or [SPACE] to add a simulation.",
                parentLayout = "desc",
                length = 0.01f,
                rect = RectValues.Default.SizeDelta(300f, 46f),
                hideBG = true,
                textColor = 6,
                regenerate = false,
            });

            layouts.Add("nanobots", new MenuVerticalLayout
            {
                name = "nanobots",
                rect = RectValues.Default.AnchoredPosition(-600f, -40f).SizeDelta(600f, 400f),
                regenerate = false,
            });

            for (int i = 0; i < 8; i++)
            {
                var menuText = new MenuText
                {
                    id = LSText.randomNumString(16),
                    name = "Text",
                    text = string.Empty,
                    parentLayout = "nanobots",
                    length = 0.01f,
                    rect = RectValues.Default.SizeDelta(300f, 46f),
                    hideBG = true,
                    textColor = 6,
                    opacity = 1f,
                    regenerate = false,
                };
                elements.Add(menuText);
                nanobots.Add(menuText);
                noTexts.Add($"<color={LSText.randomHex("666666")}>{LSText.randomString(36)}</color>");
                noColors.Add(LSText.randomHex("666666"));
            }

            elements.AddRange(GenerateBottomBar(6, 0f, false));

            changeTextCoroutine = CoroutineHelper.StartCoroutine(ChangeText());
            exitFunc = Exit;
            UpdateText(false);
            StartGeneration();
        }

        /// <summary>
        /// Action that occurs when a player selects all controller inputs in the Input Select screen and loads the next scene.
        /// </summary>
        public static Action OnInputsSelected { get; set; }

        public static Action OnExit { get; set; }

        Coroutine changeTextCoroutine;
        List<MenuText> nanobots = new List<MenuText>();
        List<string> noTexts = new List<string>();
        List<string> noColors = new List<string>();
        static List<Color> playerColors = new List<Color>()
        {
            LSColors.HexToColor(BeatmapTheme.PLAYER_1_COLOR),
            LSColors.HexToColor(BeatmapTheme.PLAYER_2_COLOR),
            LSColors.HexToColor(BeatmapTheme.PLAYER_3_COLOR),
            LSColors.HexToColor(BeatmapTheme.PLAYER_4_COLOR),
        };

        IEnumerator ChangeText()
        {
            for (int i = 0; i < nanobots.Count; i++)
            {
                if (UnityEngine.Random.value < 0.5f)
                {
                    noTexts[i] = $"<color={LSText.randomHex("666666")}>{LSText.randomString(36)}</color>";
                    noColors[i] = LSText.randomHex("666666");
                }
            }
            yield return CoroutineHelper.Seconds(UnityEngine.Random.Range(0f, 0.4f));
            changeTextCoroutine = CoroutineHelper.StartCoroutine(ChangeText());
            yield break;
        }

        void UpdateText(bool assignToUI = true)
        {
            for (int i = 0; i < nanobots.Count; i++)
            {
                string text;
                if (InputDataManager.inst.players.Count > i && InputDataManager.inst.players[i].active)
                {
                    var customPlayer = InputDataManager.inst.players[i];

                    string textColor = "#" + LSColors.ColorToHex(playerColors[customPlayer.index % playerColors.Count]);
                    string device = customPlayer.deviceType.ToString();
                    if (device != customPlayer.deviceModel)
                        device = customPlayer.deviceType.ToString() + " (" + customPlayer.deviceModel + ")";

                    text = customPlayer.index < 4 ?
                        $"<color={textColor}><size=200%>■</color><voffset=0.25em><size=100%> <b>Nanobot:</b> {customPlayer.index + 1}    <b>Input Device:</b> {device}" :
                        $"<color={textColor}><size=200%>●</color><voffset=0.25em><size=100%> <b>Nanobot:</b> {customPlayer.index + 1}    <b>Input Device:</b> {device}";
                }
                else
                {
                    string textColor = (noColors.Count > i) ? noColors[i] : "#666666";

                    text = i < 4 ?
                        $"<color={textColor}><size=200%>■</color><voffset=0.25em><size=100%> {(noTexts.Count > i ? noTexts[i] : string.Empty)}" :
                        $"<color={textColor}><size=200%>●</color><voffset=0.25em><size=100%> {(noTexts.Count > i ? noTexts[i] : string.Empty)}";
                }

                if (assignToUI && nanobots[i].textUI)
                {
                    nanobots[i].textUI.maxVisibleCharacters = 9999;
                    nanobots[i].textUI.text = text;
                }
                else if (!assignToUI)
                    nanobots[i].text = text;
            }
        }

        public void Continue()
        {
            InputDataManager.inst.playersCanJoin = false;

            OnInputsSelected?.Invoke();

            if (OnInputsSelected != null) // if we want to run a custom function instead of doing the normal methods.
            {
                OnInputsSelected = null;
                return;
            }

            if (LevelManager.IsArcade)
            {
                SceneHelper.LoadScene(SceneName.Arcade_Select, false);
                return;
            }

            SaveManager.inst.LoadCurrentStoryLevel();
        }

        public void Exit()
        {
            InputDataManager.inst.playersCanJoin = false;
            InputDataManager.inst.ClearInputs();

            OnExit?.Invoke();

            if (OnExit != null)
            {
                OnExit = null;
                return;
            }

            SceneHelper.LoadScene(SceneName.Main_Menu, false);
        }

        public override void OnTick()
        {
            if (generating)
                return;

            UpdateText();

            if (!CoreHelper.IsUsingInputField && InputDataManager.inst.players.Count > 0 && InputDataManager.inst.menuActions.Start.WasPressed)
                Continue();
        }

        public override void Clear()
        {
            base.Clear();
            if (changeTextCoroutine != null)
                CoroutineHelper.StopCoroutine(changeTextCoroutine);
        }
    }
}
