using System;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Layouts;

namespace BetterLegacy.Menus.UI.Interfaces
{
    public class ConfirmInterface : InterfaceBase
    {
        public ConfirmInterface(string currentMessage, Action confirm, Action cancel) : base()
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

            if (ProjectArrhythmia.State.InGame)
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
                id = "0",
                name = "message",
                text = $"<align=center>{currentMessage}",
                rect = RectValues.Default,
                hideBG = true,
                color = 0,
                opacity = 1f,
                val = ProjectArrhythmia.State.InGame ? 40f : 0f,
                textColor = ProjectArrhythmia.State.InGame ? 0 : 6,
                textVal = ProjectArrhythmia.State.InGame ? 40f : 0f,
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
                val = ProjectArrhythmia.State.InGame ? -40f : 0f,
                textColor = 6,
                textVal = ProjectArrhythmia.State.InGame ? 40f : 0f,
                selectedColor = 6,
                selectedOpacity = 1f,
                selectedVal = ProjectArrhythmia.State.InGame ? 40f : 0f,
                selectedTextColor = 7,
                selectedTextVal = ProjectArrhythmia.State.InGame ? -40f : 0f,

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
                val = ProjectArrhythmia.State.InGame ? -40f : 0f,
                textColor = 6,
                textVal = ProjectArrhythmia.State.InGame ? 40f : 0f,
                selectedColor = 6,
                selectedOpacity = 1f,
                selectedVal = ProjectArrhythmia.State.InGame ? 40f : 0f,
                selectedTextColor = 7,
                selectedTextVal = ProjectArrhythmia.State.InGame ? -40f : 0f,

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
            if (ProjectArrhythmia.State.InGame)
                RTBeatmap.Current?.Pause();
            new ConfirmInterface(currentMessage, confirm, cancel);
        }

        public override void UpdateTheme()
        {
            if (ProjectArrhythmia.State.InGame)
                Theme = CoreHelper.CurrentBeatmapTheme;

            base.UpdateTheme();
        }
    }
}
