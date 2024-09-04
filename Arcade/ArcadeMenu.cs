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

        public ArcadeMenu() : base()
        {
            InterfaceManager.inst.CurrentMenu = this;

            layouts.Add("tabs", new MenuHorizontalLayout
            {
                name = "tabs",
            });

            elements.Add(new MenuButton
            {
                id = "0",
                name = "Close",
                parentLayout = "tabs",
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
            });

            for (int i = 0; i < 6; i++)
            {
                int index = i;
                elements.Add(new MenuButton
                {
                    id = (i + 1).ToString(),
                    name = "Tab",
                    parentLayout = "tabs",
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
                });
            }

            var currentPage = Pages[(int)CurrentTab] + 1;
            int max = currentPage * MAX_LEVELS_PER_PAGE;

            switch (CurrentTab)
            {
                case Tab.Local:
                    {
                        elements.Add(new MenuInputField
                        {
                            id = "842848",
                            name = "Search Bar",
                            valueChangedFunc = SearchLocalLevels,
                        });

                        layouts.Add("levels", new MenuGridLayout
                        {
                            name = "levels",
                        });

                        int row = 0;
                        for (int i = 0; i < LevelManager.Levels.Count; i++)
                        {
                            int index = i;
                            if (index < max - MAX_LEVELS_PER_PAGE || index >= max)
                                return;
                            if (index % 5 == 4)
                                row++;

                            var level = LevelManager.Levels[index];
                            var button = new MenuButton
                            {
                                id = level.id,
                                name = "Level Button",
                                parentLayout = "levels",
                                selectionPosition = new Vector2Int(index % 5, row),
                                func = () => { CoreHelper.StartCoroutine(SelectLocalLevel(level)); },
                                icon = level.icon,
                                text = $"{level.metadata?.LevelBeatmap?.name}<b>By {level.metadata?.creator?.steam_name}",
                                color = 6,
                                opacity = 0.1f,
                                textColor = 6,
                                selectedColor = 6,
                                selectedOpacity = 1f,
                                selectedTextColor = 7,
                            };
                            elements.Add(button);

                            elements.Add(new MenuImage
                            {
                                id = "41894948",
                                name = "Difficulty",
                                overrideColor = level.metadata.song.getDifficultyColor(),
                                useOverrideColor = true,
                                opacity = 1f,
                                roundedSide = SpriteManager.RoundedSide.Left,
                            });
                        }
                        break;
                    }
            }


            //layouts.Add("tabs", new MenuHorizontalLayout
            //{
            //    name = "tabs",
            //    spacing = 4f,
            //    rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(0f, 470f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-32f, 64f)),
            //});

            //layouts.Add("levels", new MenuGridLayout
            //{
            //    name = "levels",
            //    spacing = new Vector2(8f, 8f),
            //    cellSize = new Vector2(350f, 200f),
            //    rectJSON = MenuImage.GenerateRectTransformJSON(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1800f, 800f)),
            //});

            //elements.Add(new MenuButton
            //{
            //    id = "0",
            //    name = "close",
            //    text = "<align=center><b>X",
            //    parentLayout = "tabs",
            //    selectionPosition = Vector2Int.zero,
            //    rectJSON = MenuImage.GenerateRectTransformJSON(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(78f, 78f)),
            //    opacity = 0.1f,
            //    val = -40f,
            //    textVal = 40f,
            //    selectedOpacity = 1f,
            //    selectedVal = 40f,
            //    selectedTextVal = -40f,
            //    length = 1f,
            //    playBlipSound = true,
            //    func = ArcadeHelper.LoadInputSelect,
            //});

            //int num = 1;
            //for (int i = 0; i < LevelManager.Levels.Count; i++)
            //{
            //    if (i >= 20)
            //        break;

            //    var level = LevelManager.Levels[i];
            //    elements.Add(new MenuButton
            //    {
            //        id = level.id,
            //        name = "level button",
            //        text = $"{level.metadata.LevelBeatmap.name}",
            //        icon = level.icon,
            //        parentLayout = "levels",
            //        selectionPosition = new Vector2Int(i % 5, num),
            //        rectJSON = MenuImage.GenerateRectTransformJSON(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(64f, 64f)),
            //        opacity = 0.1f,
            //        val = -40f,
            //        textVal = -40f,
            //        selectedOpacity = 1f,
            //        selectedVal = 40f,
            //        selectedTextVal = -40f,
            //        length = 1f,
            //        playBlipSound = true,
            //        func = () => { CoreHelper.Log($"Selected level: {level}"); },
            //    });
            //    if ((i % 5) == 4)
            //        num++;
            //}

            CoreHelper.StartCoroutine(GenerateUI());
        }

        public void SearchLocalLevels(string search)
        {
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
