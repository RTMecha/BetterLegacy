﻿using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Menus;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Interfaces;
using BetterLegacy.Menus.UI.Layouts;
using LSFunctions;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace BetterLegacy.Story
{
    public class StoryMenu : MenuBase
    {
        public StoryMenu() : base()
        {
            id = "1";

            layouts.Add("buttons", new MenuVerticalLayout
            {
                name = "buttons",
                childControlWidth = true,
                childForceExpandWidth = true,
                spacing = 4f,
                rect = RectValues.Default.AnchoredPosition(-500f, 200f).SizeDelta(800f, 200f),
            });

            layouts.Add("delete", new MenuVerticalLayout
            {
                name = "delete",
                childControlWidth = true,
                childForceExpandWidth = true,
                spacing = 4f,
                rect = RectValues.Default.AnchoredPosition(60f, 132f).SizeDelta(200f, 200f),
            });

            SetupUI();
        }

        public void SetupUI()
        {
            Clear();
            elements.Clear();

            elements.AddRange(GenerateTopBar("Story Menu", 6, 0f));

            exitFunc = () => { InterfaceManager.inst.SetCurrentInterface("0"); };

            elements.Add(new MenuEvent
            {
                id = "09",
                name = "Effects",
                func = () => { MenuEffectsManager.inst.UpdateChroma(0.1f); },
                length = 0f,
            });

            elements.Add(new MenuButton
            {
                id = "4918487",
                name = name,
                text = $"<b> [ RETURN ]",
                parentLayout = "buttons",
                selectionPosition = new Vector2Int(0, 0),
                rect = RectValues.Default.SizeDelta(200f, 64f),
                color = 6,
                opacity = 0.1f,
                textColor = 6,
                selectedColor = 6,
                selectedTextColor = 7,
                selectedOpacity = 1f,
                length = 1f,
                playBlipSound = true,
                func = () => { InterfaceManager.inst.SetCurrentInterface("0"); },
            });

            if (!StoryManager.inst || !RTFile.DirectoryExists(StoryManager.StoryAssetsPath) || !RTFile.FileExists($"{StoryManager.StoryAssetsPath}doc01_01.asset"))
            {
                elements.AddRange(GenerateBottomBar(6, 0f));
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                elements.Add(new MenuButton
                {
                    id = "4918487",
                    name = name,
                    text = $"<b> [ SLOT {(index + 1).ToString("00")} ]",
                    parentLayout = "buttons",
                    selectionPosition = new Vector2Int(0, index + 1),
                    rect = RectValues.Default.SizeDelta(200f, 64f),
                    color = 6,
                    opacity = 0.1f,
                    textColor = 6,
                    selectedColor = 6,
                    selectedTextColor = 7,
                    selectedOpacity = 1f,
                    length = 1f,
                    playBlipSound = true,
                    func = () =>
                    {
                        StoryManager.inst.SaveSlot = index;
                        LevelManager.IsArcade = false;
                        SceneManager.inst.LoadScene("Input Select");
                        LevelManager.OnInputsSelected = () => { SceneManager.inst.LoadScene("Interface"); };
                    },
                });
            }

            for (int i = 0; i < 3; i++)
            {
                int index = i;

                if (!RTFile.FileExists($"{RTFile.ApplicationDirectory}profile/story_saves_{(index + 1).ToString("00")}.lss"))
                    continue;

                elements.Add(new MenuButton
                {
                    id = "4918487",
                    name = name,
                    text = $"<align=center><b>[ DELETE ]   ",
                    parentLayout = "delete",
                    selectionPosition = new Vector2Int(1, index + 1),
                    rect = RectValues.Default.SizeDelta(200f, 64f),
                    opacity = 0.1f,
                    color = 6,
                    textColor = 6,
                    selectedOpacity = 1f,
                    selectedTextColor = 6,
                    length = 1f,
                    playBlipSound = true,
                    func = () =>
                    {
                        int slot = index;
                        new ConfirmMenu("Are you sure you want to delete this save slot?", () =>
                        {
                            InterfaceManager.inst.CloseMenus();
                            StoryManager.inst.SaveSlot = slot;
                            File.Delete(StoryManager.inst.StorySavesPath);
                            StoryManager.inst.SaveSlot = 0;
                            SetupUI();
                            InterfaceManager.inst.SetCurrentInterface("1");
                        }, () => { InterfaceManager.inst.SetCurrentInterface("1"); });
                    },
                });
            }

            elements.AddRange(GenerateBottomBar(6, 0f));
        }

        public override void UpdateTheme()
        {
            if (Parser.TryParse(MenuConfig.Instance.InterfaceThemeID.Value, -1) >= 0 && InterfaceManager.inst.themes.TryFind(x => x.id == MenuConfig.Instance.InterfaceThemeID.Value, out BeatmapTheme interfaceTheme))
                Theme = interfaceTheme;
            else
                Theme = InterfaceManager.inst.themes[0];

            base.UpdateTheme();
        }
    }
}
