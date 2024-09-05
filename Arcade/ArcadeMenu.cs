using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BetterLegacy.Menus;
using BetterLegacy.Menus.UI;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Layouts;
using BetterLegacy.Menus.UI.Interfaces;
using UnityEngine;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using LSFunctions;
using System.Collections;

namespace BetterLegacy.Arcade
{
    // Probably not gonna use this
    public class ArcadeMenu : MenuBase
    {
        public static bool useThisUI = false;

        public static ArcadeMenu Current { get; set; }

        public enum Tab
        {
            Local,
            Online,
            Browser, // also allows you to download
            Queue,
            Steam
        }

        public static Tab CurrentTab { get; set; }
        public static int[] Pages { get; set; } = new int[]
        {
            0, // Local
            0, // Online
            0, // Browser
            0, // Queue
            0, // Steam
        };

        public const int MAX_LEVELS_PER_PAGE = 20;

        public static string[] Searches { get; set; } = new string[]
        {
            "", // Local
            "", // Online
            "", // Browser
            "", // Queue
            "", // Steam
        };

        public ArcadeMenu() : base()
        {
            InterfaceManager.inst.CurrentMenu = this;

            layouts.Add("tabs", new MenuHorizontalLayout
            {
                name = "tabs",
                rect = RectValues.HorizontalAnchored.AnchoredPosition(0f, 450f).SizeDelta(-126f, 100f),
                childControlWidth = true,
                childForceExpandWidth = true,
                spacing = 12f,
            });

            elements.Add(new MenuButton
            {
                id = "0",
                name = "Close",
                parentLayout = "tabs",
                selectionPosition = Vector2Int.zero,
                text = "<align=center><b>[ RETURN ]",
                func = () =>
                {
                    InterfaceManager.inst.CloseMenus();
                    SceneManager.inst.LoadScene("Input Select");
                },
                color = 6,
                opacity = 0.1f,
                textColor = 6,
                selectedColor = 6,
                selectedOpacity = 1f,
                selectedTextColor = 7,
                length = 0.1f,
            });

            for (int i = 0; i < 5; i++)
            {
                int index = i;
                elements.Add(new MenuButton
                {
                    id = (i + 1).ToString(),
                    name = "Tab",
                    parentLayout = "tabs",
                    selectionPosition = new Vector2Int(i + 1, 0),
                    text = $"<align=center><b>[ {(Tab)i} ]",
                    func = () =>
                    {
                        CurrentTab = (Tab)index;
                        Init();
                    },
                    color = 6,
                    opacity = 0.1f,
                    textColor = 6,
                    selectedColor = 6,
                    selectedOpacity = 1f,
                    selectedTextColor = 7,
                    length = 0.1f,
                });
            }

            var currentPage = Pages[(int)CurrentTab] + 1;
            int max = currentPage * MAX_LEVELS_PER_PAGE;
            var currentSearch = Searches[(int)CurrentTab];

            switch (CurrentTab)
            {
                case Tab.Local:
                    {
                        elements.Add(new MenuInputField
                        {
                            id = "842848",
                            name = "Search Bar",
                            defaultText = currentSearch,
                            valueChangedFunc = SearchLocalLevels,
                            length = 0.1f,
                        });

                        layouts.Add("levels", new MenuGridLayout
                        {
                            name = "levels",
                            rect = RectValues.Default.AnchoredPosition(-500f, 100f).SizeDelta(800f, 400f),
                            cellSize = new Vector2(350f, 180f),
                            spacing = new Vector2(12f, 12f),
                            constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount,
                            constraintCount = 5,
                        });

                        for (int i = 0; i < LevelManager.Levels.Count; i++)
                        {
                            int index = i;
                            if (index < max - MAX_LEVELS_PER_PAGE || index >= max)
                                continue;

                            int column = (index % MAX_LEVELS_PER_PAGE) % 5;
                            int row = (int)((index % MAX_LEVELS_PER_PAGE) / 5) + 1;

                            var level = LevelManager.Levels[index];
                            var button = new MenuButton
                            {
                                id = level.id,
                                name = "Level Button",
                                parentLayout = "levels",
                                selectionPosition = new Vector2Int(column, row),
                                func = () => { CoreHelper.StartCoroutine(SelectLocalLevel(level)); },
                                icon = level.icon,
                                iconRect = RectValues.Default.AnchoredPosition(-90, 30f),
                                text = "<size=24>" + level.metadata?.LevelBeatmap?.name,
                                textRect = RectValues.FullAnchored.AnchoredPosition(20f, -50f),
                                enableWordWrapping = true,
                                color = 6,
                                opacity = 0.1f,
                                textColor = 6,
                                selectedColor = 6,
                                selectedOpacity = 1f,
                                selectedTextColor = 7,
                                length = 0.1f,

                                allowOriginalHoverMethods = true,
                                enterFunc = () =>
                                {
                                },
                                exitFunc = () =>
                                {
                                },
                            };
                            elements.Add(button);

                            elements.Add(new MenuImage
                            {
                                id = "0",
                                name = "Difficulty",
                                parent = level.id,
                                rect = new RectValues(Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(8f, 0f)),
                                overrideColor = CoreHelper.GetDifficulty(level.metadata.song.difficulty).color,
                                useOverrideColor = true,
                                opacity = 1f,
                                roundedSide = SpriteManager.RoundedSide.Left,
                                length = 0f,
                            });
                        }
                        break;
                    }
                case Tab.Online:
                    {
                        elements.Add(new MenuInputField
                        {
                            id = "842848",
                            name = "Search Bar",
                            defaultText = currentSearch,
                            valueChangedFunc = x => Searches[(int)CurrentTab] = x,
                            length = 0.1f,
                        });

                        elements.Add(new MenuButton
                        {
                            id = "25428852",
                            name = "Search Button",
                            text = "<align=center><b>[ SEARCH ]",
                            selectionPosition = new Vector2Int(0, 1),
                            func = () =>
                            {

                            },
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            selectedColor = 6,
                            selectedOpacity = 1f,
                            selectedTextColor = 7,
                            length = 0.1f,
                        });

                        break;
                    }
            }

            CoreHelper.StartCoroutine(GenerateUI());
        }

        public void SearchLocalLevels(string search)
        {
            Searches[0] = search;
            var levels = LevelManager.Levels.Where(x => x.id == search).ToList();
            var levelButtons = elements.Where(x => x.name == "Level Button").ToList();

            for (int i = 0; i < levelButtons.Count; i++)
            {
                var levelButton = levelButtons[i];
                levelButton?.gameObject.SetActive(levels.Has(x => x.id == levelButton.id));
            }
        }

        public IEnumerator SelectLocalLevel(Level level)
        {
            if (!level.music)
                yield return CoreHelper.StartCoroutine(level.LoadAudioClipRoutine(() => { OpenPlayLevelMenu(level); }));
            else
                OpenPlayLevelMenu(level);
        }

        void OpenPlayLevelMenu(Level level)
        {
            AudioManager.inst.StopMusic();
            PlayLevelMenu.Init(level);
            AudioManager.inst.PlayMusic(level.metadata.song.title, level.music);
            AudioManager.inst.SetPitch(CoreHelper.Pitch);
        }

        public override void UpdateTheme()
        {
            if (Parser.TryParse(MenuConfig.Instance.InterfaceThemeID.Value, -1) >= 0 && InterfaceManager.inst.themes.TryFind(x => x.id == MenuConfig.Instance.InterfaceThemeID.Value, out BeatmapTheme interfaceTheme))
                Theme = interfaceTheme;
            else
                Theme = InterfaceManager.inst.themes[0];

            base.UpdateTheme();
        }

        public static void Init()
        {
            InterfaceManager.inst.CloseMenus();
            // testing
            if (ArcadeMenuManager.inst)
            {
                CoreHelper.Destroy(ArcadeMenuManager.inst);
                CoreHelper.Destroy(ArcadeMenuManager.inst.menuUI);
            }

            Current = new ArcadeMenu();
        }
    }
}
