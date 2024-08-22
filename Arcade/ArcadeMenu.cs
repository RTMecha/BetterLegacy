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

namespace BetterLegacy.Arcade
{
    // Probably not gonna use this
    public class ArcadeMenu : MenuBase
    {
        public static ArcadeMenu Current { get; set; }

        public ArcadeMenu() : base(false)
        {
            //InterfaceManager.inst.CurrentMenu = this;

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

            //CoreHelper.StartCoroutine(GenerateUI());
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

            //Current = new ArcadeMenu();
        }
    }
}
