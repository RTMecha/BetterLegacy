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
            InterfaceManager.inst.CloseMenus();
            InterfaceManager.inst.CurrentMenu = this;

            if (!CoreHelper.InGame)
                elements.Add(new MenuEvent
                {
                    id = "09",
                    name = "Effects",
                    func = () => { MenuEffectsManager.inst.UpdateChroma(0.1f); },
                    length = 0f,
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
            });
            progressBar = new MenuImage
            {
                id = "2",
                name = "progress",
                parent = "1",
                rect = RectValues.Default.AnchorMax(0f, 0.5f).AnchorMin(0f, 0.5f).Pivot(0f, 0.5f).SizeDelta(0f, 64f),
                color = 6,
                opacity = 1f,
            };
            elements.Add(progressBar);
            InterfaceManager.inst.CurrentGenerateUICoroutine = CoreHelper.StartCoroutine(GenerateUI());
        }

        public void UpdateProgress(float progress)
        {
            if (progressBar == null || !progressBar.gameObject)
                return;

            progressBar.gameObject.transform.AsRT().sizeDelta = new UnityEngine.Vector2(900f * progress, 64f);
        }

        public MenuImage progressBar;

        public static void Init(string currentMessage)
        {
            InterfaceManager.inst.CloseMenus();
            Current = new ProgressMenu(currentMessage);
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
