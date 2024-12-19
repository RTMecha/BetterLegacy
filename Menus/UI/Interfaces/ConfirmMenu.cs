using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Layouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Menus.UI.Interfaces
{
    public class ConfirmMenu : MenuBase
    {
        public ConfirmMenu(string currentMessage, Action confirm, Action cancel) : base()
        {
            musicName = InterfaceManager.RANDOM_MUSIC_NAME;
            name = "Confirm";

            layouts.Add("buttons", new MenuHorizontalLayout
            {
                name = "buttons",
                rect = RectValues.Default.AnchoredPosition(0f, -330f).SizeDelta(1200f, 64f),
                spacing = 32f,
                childControlWidth = true,
                childForceExpandWidth = true,
            });

            elements.Add(new MenuText
            {
                id = "0",
                name = "message",
                text = $"<align=center>{currentMessage}",
                rect = RectValues.Default,
                hideBG = true,
                textColor = 6,
            });

            elements.Add(new MenuButton
            {
                id = "1",
                name = "Arcade Button",
                text = "<b><align=center>[ CONFIRM ]",
                parentLayout = "buttons",
                autoAlignSelectionPosition = true,
                color = 6,
                opacity = 0.1f,
                textColor = 6,
                selectedColor = 6,
                selectedTextColor = 7,
                selectedOpacity = 1f,
                length = 0.3f,
                playBlipSound = true,
                rect = RectValues.Default.SizeDelta(100f, 64f),
                func = confirm,
            });
            
            elements.Add(new MenuButton
            {
                id = "1",
                name = "Arcade Button",
                text = "<b><align=center>[ CANCEL ]",
                parentLayout = "buttons",
                autoAlignSelectionPosition = true,
                color = 6,
                opacity = 0.1f,
                textColor = 6,
                selectedColor = 6,
                selectedTextColor = 7,
                selectedOpacity = 1f,
                length = 0.3f,
                playBlipSound = true,
                rect = RectValues.Default.SizeDelta(100f, 64f),
                func = cancel,
            });

            InterfaceManager.inst.SetCurrentInterface(this);
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
