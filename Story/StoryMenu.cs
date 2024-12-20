using BetterLegacy.Configs;
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
        public const int MAX_SAVE_SLOTS = 9;

        public StoryMenu() : base()
        {
            id = InterfaceManager.STORY_SAVES_MENU_ID;
            name = "Story";

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

            elements.Add(new MenuEvent
            {
                id = "09",
                name = "Effects",
                func = MenuEffectsManager.inst.SetDefaultEffects,
                length = 0f,
            });

            elements.AddRange(GenerateTopBar("Story Menu", 6, 0f));

            exitFunc = () => InterfaceManager.inst.SetCurrentInterface(InterfaceManager.MAIN_MENU_ID);

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
                func = () => InterfaceManager.inst.SetCurrentInterface(InterfaceManager.MAIN_MENU_ID),
            });

            if (!StoryManager.inst || !RTFile.DirectoryExists(StoryManager.StoryAssetsPath))
            {
                elements.AddRange(GenerateBottomBar(6, 0f));
                return;
            }

            for (int i = 0; i < MAX_SAVE_SLOTS; i++)
            {
                int index = i;
                string progress = "";
                var fileExists = RTFile.FileExists($"{RTFile.ApplicationDirectory}profile/story_saves_{RTString.ToStoryNumber(index)}{FileFormat.LSS.Dot()}");
                if (fileExists)
                {
                    var saveSlot = new SaveSlot(i);
                    var chapterIndex = RTMath.Clamp(saveSlot.ChapterIndex, 0, StoryMode.Instance.chapters.Count - 1);
                    var levelSequenceIndex = RTMath.Clamp(saveSlot.LevelSequenceIndex, 0, StoryMode.Instance.chapters[chapterIndex].Count - 1);
                    progress = $" | DOC{RTString.ToStoryNumber(chapterIndex)} - SIM{RTString.ToStoryNumber(levelSequenceIndex)} |";
                    saveSlot.storySavesJSON = null;
                    saveSlot = null;
                }

                elements.Add(new MenuButton
                {
                    id = "4918487",
                    name = name,
                    text = $"<b> [ SLOT {(index + 1).ToString("00")}{progress} ]",
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
                        StoryManager.inst.SaveString("LastPlayedModVersion", LegacyPlugin.ModVersion.ToString());
                        SceneHelper.LoadInputSelect(SceneHelper.LoadInterfaceScene);
                    },
                });

                if (!fileExists)
                {
                    elements.Add(new MenuImage
                    {
                        id = "636662526",
                        name = "Spacer",
                        parentLayout = "delete",
                        rect = RectValues.Default.SizeDelta(200f, 64f),
                        opacity = 0f,
                        length = 0f,
                        wait = false,
                        playBlipSound = false,
                    });
                    continue;
                }

                elements.Add(new MenuButton
                {
                    id = "4918487",
                    name = "delete button",
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
                            RTFile.DeleteFile(StoryManager.inst.StorySavesPath);
                            StoryManager.inst.SaveSlot = 0;
                            SetupUI();
                            InterfaceManager.inst.SetCurrentInterface(InterfaceManager.STORY_SAVES_MENU_ID);
                        }, () => InterfaceManager.inst.SetCurrentInterface(InterfaceManager.STORY_SAVES_MENU_ID));
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
