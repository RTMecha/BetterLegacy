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
    public class ChapterSelectMenu : MenuBase
    {
        public ChapterSelectMenu() : base()
        {
            id = "2";

            layouts.Add("buttons", new MenuVerticalLayout
            {
                name = "buttons",
                childControlWidth = true,
                childForceExpandWidth = true,
                spacing = 4f,
                rect = RectValues.Default.AnchoredPosition(-500f, 200f).SizeDelta(800f, 200f),
            });

            elements.AddRange(GenerateTopBar("Chapter Select Menu", 6, 0f));

            exitFunc = () => { InterfaceManager.inst.StartupStoryInterface(); };

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
                func = () => { InterfaceManager.inst.StartupStoryInterface(); },
            });

            for (int i = 1; i <= StoryMode.Instance.chapters.Count; i++)
            {
                int index = i - 1;
                if (RTFile.FileExists($"{StoryManager.StoryAssetsPath}doc{i.ToString("00")}_01.asset") && StoryManager.inst.LoadInt("Chapter", 0) >= index)
                {
                    elements.Add(new MenuButton
                    {
                        id = "4918487",
                        name = name,
                        text = $"<b> [ DOC {(index + 1).ToString("00")} \"{StoryMode.Instance.chapters[index].name}\" ]",
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
                            StoryManager.inst.SaveInt("Chapter", index);
                            InterfaceManager.inst.StartupStoryInterface();
                        },
                    });
                }
            }
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
