using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Menus;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Interfaces;
using BetterLegacy.Core.Managers;
using BetterLegacy.Configs;
using LSFunctions;

namespace BetterLegacy.Arcade
{
    public class LevelCollectionMenu : MenuBase
    {
        public static LevelCollectionMenu Current { get; set; }
        public static LevelCollection CurrentCollection { get; set; }

        public LevelCollectionMenu() : base()
        {
            InterfaceManager.inst.CurrentMenu = this;

            elements.Add(new MenuImage
            {
                id = "35255236785",
                name = "Background",
                siblingIndex = 0,
                rect = RectValues.FullAnchored,
                color = 17,
                opacity = 1f,
                length = 0f,
            });

            elements.Add(new MenuButton
            {
                id = "626274",
                name = "Close Button",
                rect = RectValues.Default.AnchoredPosition(-676f, 460f).SizeDelta(250f, 64f),
                selectionPosition = Vector2Int.zero,
                text = "<b><align=center><size=40>[ RETURN ]",
                opacity = 0.1f,
                selectedOpacity = 1f,
                color = 6,
                selectedColor = 6,
                textColor = 6,
                selectedTextColor = 7,
                length = 0.5f,
                playBlipSound = true,
                func = Close,
            });

            if (!string.IsNullOrEmpty(CurrentCollection.serverID))
            {
                elements.Add(new MenuButton
                {
                    id = "4857529985",
                    name = "Copy ID",
                    rect = RectValues.Default.AnchoredPosition(60f, 460f).SizeDelta(400f, 64f),
                    selectionPosition = new Vector2Int(1, 0),
                    text = $"<b><align=center><size=40>[ COPY SERVER ID ]",
                    opacity = 0.1f,
                    selectedOpacity = 1f,
                    color = 6,
                    selectedColor = 6,
                    textColor = 6,
                    selectedTextColor = 7,
                    length = 0.5f,
                    playBlipSound = true,
                    func = () => { LSText.CopyToClipboard(CurrentCollection.serverID); },
                });
            }

            elements.Add(new MenuButton
            {
                id = "4857529985",
                name = "Copy ID",
                rect = RectValues.Default.AnchoredPosition(500f, 460f).SizeDelta(400f, 64f),
                selectionPosition = new Vector2Int(2, 0),
                text = $"<b><align=center><size=40>[ COPY ARCADE ID ]",
                opacity = 0.1f,
                selectedOpacity = 1f,
                color = 6,
                selectedColor = 6,
                textColor = 6,
                selectedTextColor = 7,
                length = 0.5f,
                playBlipSound = true,
                func = () => { LSText.CopyToClipboard(CurrentCollection.id); },
            });

        }

        public override void UpdateTheme()
        {
            if (Parser.TryParse(MenuConfig.Instance.InterfaceThemeID.Value, -1) >= 0 && InterfaceManager.inst.themes.TryFind(x => x.id == MenuConfig.Instance.InterfaceThemeID.Value, out BeatmapTheme interfaceTheme))
                Theme = interfaceTheme;
            else
                Theme = InterfaceManager.inst.themes[0];

            base.UpdateTheme();
        }

        public static void Init(LevelCollection collection)
        {
            InterfaceManager.inst.CloseMenus();
            CurrentCollection = collection;
            Current = new LevelCollectionMenu();
        }

        public static void Close()
        {
            if (MenuManager.inst)
                AudioManager.inst.PlayMusic(MenuManager.inst.currentMenuMusicName, MenuManager.inst.currentMenuMusic);
            LevelManager.CurrentLevelCollection = null;
            InterfaceManager.inst.CloseMenus();

            ArcadeMenu.Init();
        }

        public override void Clear()
        {
            CurrentCollection = null;
            base.Clear();
        }
    }
}
