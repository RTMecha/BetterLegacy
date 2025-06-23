using System;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Layouts;

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

            exitFunc = cancel;

            InterfaceManager.inst.SetCurrentInterface(this);
        }

        public static void Init(string currentMessage, Action confirm, Action cancel)
        {
            if (CoreHelper.InGame)
                RTBeatmap.Current?.Pause();
            new ConfirmMenu(currentMessage, confirm, cancel);
        }

        public override void UpdateTheme()
        {
            if (CoreHelper.InGame)
                Theme = CoreHelper.CurrentBeatmapTheme;

            base.UpdateTheme();
        }
    }
}
