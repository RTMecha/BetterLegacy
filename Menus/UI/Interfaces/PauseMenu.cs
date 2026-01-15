using System;
using System.Collections;

using UnityEngine;

using LSFunctions;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
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

        public PauseMenu() : base()
        {
            if (!ProjectArrhythmia.State.InGame || ProjectArrhythmia.State.InEditor)
            {
                CoreHelper.LogError($"Cannot pause outside of the game!");
                return;
            }

            onGenerateUIFinish = () => InputDataManager.inst.SetAllControllerRumble(0f);

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
                wait = false,
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

            int index = 0;
            for (int i = 0; i < buttonElements.Length; i++)
            {
                var buttonElement = buttonElements[i];

                var active = buttonElement.check == null || buttonElement.check.Invoke();

                if (!active)
                    continue;

                MenuImage element = buttonElement.isSpacer ?
                    new MenuImage
                    {
                        id = i.ToString(),
                        name = "Spacer",
                        parentLayout = "buttons",
                        rect = RectValues.Default.SizeDelta(200f, 64f),
                        opacity = 0f,
                        length = 0f,
                    } :
                    new MenuButton
                    {
                        id = index.ToString(),
                        name = buttonElement.name,
                        text = buttonElement.text,
                        parentLayout = "buttons",
                        selectionPosition = new Vector2Int(0, index),
                        rect = RectValues.Default.SizeDelta(200f, 64f),
                        opacity = 0.1f,
                        val = -40f,
                        textVal = 40f,
                        selectedOpacity = 1f,
                        selectedVal = 40f,
                        selectedTextVal = -40f,
                        length = 1f,
                        playBlipSound = true,
                        func = buttonElement.func,
                    };
                elements.Add(element);

                if (!buttonElement.isSpacer)
                    index++;
            }

            for (int i = 0; i < infoElements.Length; i++)
            {
                var infoElement = infoElements[i];

                var active = infoElement.check == null || infoElement.check.Invoke();

                if (!active)
                    continue;

                elements.Add(new MenuText
                {
                    id = "463472367",
                    name = "Info Text",
                    text = infoElement.text?.Invoke() ?? string.Empty,
                    rect = RectValues.Default.SizeDelta(300f, 32f),
                    hideBG = true,
                    textVal = 40f,
                    length = 0.3f,
                    parentLayout = "info",
                });
            }

            elements.AddRange(GenerateBottomBar());

            exitFunc = UnPause;

            InterfaceManager.inst.SetCurrentInterface(this);
        }

        public override void UpdateTheme()
        {
            Theme = CoreHelper.CurrentBeatmapTheme;

            base.UpdateTheme();
        }

        #region Methods

        #region Internal

        IEnumerator StartCountdown(Action onCooldownEnd)
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
                CoreHelper.Delete(elements[i].gameObject);
            }
            elements.RemoveAll(x => x.id != "321" && x.id != "35255236785");

            int pitch = 0;
            var speedUp = false;
            for (int i = 3; i > 0; i--)
            {
                if (countdown == null || !countdown.textUI)
                    continue;

                countdown.gameObject.transform.AsRT().localPosition = Vector3.zero;

                if (i != 3)
                    SoundManager.inst.PlaySound(DefaultSounds.blip);

                var num = $"<align=center><size=120><b><font=Fredoka One>{i}";
                countdown.text = num;
                countdown.textUI.text = num;
                countdown.textUI.maxVisibleCharacters = 9999;
                yield return CoroutineHelper.Seconds(speedUp ? 0.1f : 0.5f);
                speedUp = InterfaceManager.SpeedUp;
                pitch++;
            }

            SoundManager.inst.PlaySound(DefaultSounds.blip);

            countdown.text = "<align=center><size=120><b><font=Fredoka One>GO!";
            countdown.textUI.text = "<align=center><size=120><b><font=Fredoka One>GO!";
            yield return CoroutineHelper.Seconds(InterfaceManager.SpeedUp ? 0.05f : 0.2f);

            CountdownEnd(onCooldownEnd);

            yield break;
        }

        IEnumerator SkipCountdown(Action onCooldownEnd)
        {
            yield return CoroutineHelper.Seconds(0.3f);
            CountdownEnd(onCooldownEnd);
        }

        static void CountdownEnd(Action onCooldownEnd)
        {
            InterfaceManager.inst.CloseMenus();
            CursorManager.inst.HideCursor();
            onCooldownEnd?.Invoke();
            RTBeatmap.Current?.Resume();
        }

        #endregion

        /// <summary>
        /// Initializes the pause menu.
        /// </summary>
        public static void Pause()
        {
            if (!ProjectArrhythmia.State.Playing)
                return;

            RTBeatmap.Current?.Pause();
            ArcadeHelper.endedLevel = false;
            Current = new PauseMenu();
        }

        /// <summary>
        /// Unpauses the game and starts the countdown sequence if it is enabled.
        /// </summary>
        public static void UnPause() => UnPause(null);

        /// <summary>
        /// Unpauses the game and starts the countdown sequence if it is enabled.
        /// </summary>
        /// <param name="onCooldownEnd">Action to run when the game is fully unpaused.</param>
        public static void UnPause(Action onCooldownEnd)
        {
            if (!ProjectArrhythmia.State.Paused || !Current)
                return;

            Current.unpausing = true;
            CoroutineHelper.StartCoroutine(CoreConfig.Instance.PlayPauseCountdown.Value ? Current.StartCountdown(onCooldownEnd) : Current.SkipCountdown(onCooldownEnd));
        }

        #endregion

        public bool unpausing;

        /// <summary>
        /// Array of pause menu elements.
        /// </summary>
        public ButtonElement[] buttonElements = new ButtonElement[]
        {
            new ButtonElement
            {
                name = "Continue Button",
                text = "<b> [ CONTINUE ]",
                func = UnPause,
            },
            new ButtonElement
            {
                name = "Restart Button",
                text = "<b> [ RESTART ]",
                func = () => UnPause(ArcadeHelper.RestartLevel),
            },
            new ButtonElement
            {
                name = "Editor Button",
                text = "<b> [ EDITOR ]",
                func = SceneHelper.LoadEditorWithProgress,
            },
            new ButtonElement
            {
                check = () => ModCompatibility.UnityExplorerInstalled,
                name = "Explorer Button",
                text = "<b> [ SHOW EXPLORER ]",
                func = ModCompatibility.ShowExplorer,
            },
            new ButtonElement
            {
                name = "Config Button",
                text = "<b> [ CONFIG ]",
                func = ConfigManager.inst.Show,
            },
            new ButtonElement
            {
                check = () => LevelManager.Hub,
                name = "Return Button",
                text = "<b> [ RETURN TO HUB ]",
                func = ArcadeHelper.ReturnToHub,
            },
            new ButtonElement
            {
                check = () => ProjectArrhythmia.State.InStory,
                name = "Return Button",
                text = "<b> [ RETURN TO INTERFACE ]",
                func = SceneHelper.LoadInterfaceScene,
            },
            new ButtonElement
            {
                name = "Arcade Button",
                text = ProjectArrhythmia.State.InStory ? "<b> [ QUIT TO MAIN MENU ]" : "<b> [ RETURN TO ARCADE ]",
                func = ArcadeHelper.QuitToArcade,
            },
            new ButtonElement
            {
                isSpacer = true,
            },
            new ButtonElement
            {
                name = "Exit Button",
                text = "<b> [ QUIT GAME ]",
                func = LegacyPlugin.QuitGame,
            },
        };

        /// <summary>
        /// Represents a pause menu button.
        /// </summary>
        public class ButtonElement
        {
            /// <summary>
            /// Checks if the element should spawn.
            /// </summary>
            public Func<bool> check;
            public string name;
            public string text;
            public Action func;
            public bool isSpacer;
        }

        /// <summary>
        /// Array of pause menu information elements.
        /// </summary>
        public InfoElement[] infoElements = new InfoElement[]
        {
            new InfoElement
            {
                text = () => $"<align=right>Times hit: {RTBeatmap.Current.hits.Count}",
            },
            new InfoElement
            {
                text = () => $"<align=right>Times died: {RTBeatmap.Current.deaths.Count}",
            },
            new InfoElement
            {
                text = () =>
                {
                    var rank = LevelManager.GetLevelRank(LevelManager.CurrentLevel);
                    return $"<align=right>Previous rank: <b><#{LSColors.ColorToHex(rank.Color)}>{rank.DisplayName}</color></b>";
                },
            },
            new InfoElement
            {
                text = () =>
                {
                    var rank = LevelManager.GetLevelRank(RTBeatmap.Current.hits);
                    return $"<align=right>Current rank: <b><#{LSColors.ColorToHex(rank.Color)}>{rank.DisplayName}</color></b>";
                },
            },
            new InfoElement
            {
                text = () => $"<align=right>Time in level: {RTBeatmap.Current.levelTimer.time}",
            },
            new InfoElement
            {
                text = () => $"<align=right>Level completed: {RTString.Percentage(AudioManager.inst.CurrentAudioSource.time, AudioManager.inst.CurrentAudioSource.clip.length)}%",
            },
            new InfoElement
            {
                check = () => !ProjectArrhythmia.State.InStory || LevelManager.CurrentLevel.saveData && LevelManager.CurrentLevel.saveData.Completed,
                text = () => $"<align=right>Game Speed: {RTBeatmap.Current.gameSpeed.DisplayName}",
            },
            new InfoElement
            {
                check = () => !ProjectArrhythmia.State.InStory || LevelManager.CurrentLevel.saveData && LevelManager.CurrentLevel.saveData.Completed,
                text = () => $"<align=right>Challenge mode: {RTBeatmap.Current.challengeMode.DisplayName}",
            },
            new InfoElement
            {
                check = () => LevelManager.HasQueue,
                text = () => $"<align=right>Queue: {LevelManager.currentQueueIndex + 1} / {LevelManager.ArcadeQueue.Count}",
            },
        };

        /// <summary>
        /// Represents information the pause menu displays.
        /// </summary>
        public class InfoElement
        {
            public Func<bool> check;
            public Func<string> text;
        }
    }
}
