using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Menus.UI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Menus.UI.Interfaces
{
    public class ProgressMenu : MenuBase
    {
        public static ProgressMenu Current { get; set; }

        public ProgressMenu(string currentMessage) : base()
        {
            musicName = InterfaceManager.RANDOM_MUSIC_NAME;
            name = "Progress";

            if (!CoreHelper.InGame)
                elements.Add(new MenuEvent
                {
                    id = "09",
                    name = "Effects",
                    func = MenuEffectsManager.inst.SetDefaultEffects,
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
                textColor = 6,
            });

            elements.Add(new MenuImage
            {
                id = "1",
                name = "progress base",
                rect = RectValues.Default.AnchoredPosition(0f, -100f).SizeDelta(900f, 64f),
                color = 6,
                opacity = 0.1f,
                length = 0f,
                wait = false,
            });
            progressBar = new MenuImage
            {
                id = "2",
                name = "progress",
                parent = "1",
                rect = RectValues.Default.AnchorMax(0f, 0.5f).AnchorMin(0f, 0.5f).Pivot(0f, 0.5f).SizeDelta(0f, 64f),
                color = 6,
                opacity = 1f,
                length = 0f,
                wait = false,
            };
            elements.Add(progressBar);

            InterfaceManager.inst.SetCurrentInterface(this);
        }

        public void UpdateProgress(float progress)
        {
            if (progressBar && progressBar.gameObject)
                progressBar.gameObject.transform.AsRT().sizeDelta = new UnityEngine.Vector2(900f * progress, 64f);
        }

        public MenuImage progressBar;

        public static void Init(string currentMessage) => Current = new ProgressMenu(currentMessage);

        public override void UpdateTheme()
        {
            if (CoreHelper.InGame)
                Theme = CoreHelper.CurrentBeatmapTheme;

            base.UpdateTheme();
        }
    }
}
