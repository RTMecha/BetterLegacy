using System;
using System.Text.RegularExpressions;

using UnityEngine;

using BetterLegacy.Menus;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Interfaces;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Data;
using LSFunctions;
using BetterLegacy.Core.Managers.Networking;
using System.Collections;
using SteamworksFacepunch.Ugc;
using BetterLegacy.Core.Data.Level;

namespace BetterLegacy.Arcade.Interfaces
{
    public class SteamLevelMenu : MenuBase
    {
        public static SteamLevelMenu Current { get; set; }

        public static Item CurrentSteamItem { get; set; }

        public Action<Level> onSubscribedLevel;

        public SteamLevelMenu() : base()
        {
            this.name = "Arcade";

            elements.Add(new MenuEvent
            {
                id = "09",
                name = "Effects",
                func = MenuEffectsManager.inst.SetDefaultEffects,
                length = 0f,
                wait = false,
            });

            elements.Add(new MenuImage
            {
                id = "35255236785",
                name = "Background",
                siblingIndex = 0,
                rect = RectValues.FullAnchored,
                color = 17,
                opacity = 1f,
                length = 0f,
                wait = false,
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

            elements.Add(new MenuButton
            {
                id = "4857529985",
                name = "Copy ID",
                rect = RectValues.Default.AnchoredPosition(576f, 460f).SizeDelta(250f, 64f),
                selectionPosition = new Vector2Int(1, 0),
                text = $"<b><align=center><size=40>[ COPY ID ]",
                opacity = 0.1f,
                selectedOpacity = 1f,
                color = 6,
                selectedColor = 6,
                textColor = 6,
                selectedTextColor = 7,
                length = 0.5f,
                playBlipSound = true,
                func = () => LSText.CopyToClipboard(CurrentSteamItem.Id.ToString()),
            });

            elements.Add(new MenuImage
            {
                id = "5356325",
                name = "Backer",
                rect = RectValues.Default.AnchoredPosition(250f, 100f).SizeDelta(900f, 600f),
                opacity = 0.1f,
                color = 6,
                wait = false,
            });

            elements.Add(new MenuImage
            {
                id = "84682758635",
                name = "Cover",
                rect = RectValues.Default.AnchoredPosition(-500f, 100f).SizeDelta(600f, 600f),
                icon = ArcadeMenu.OnlineSteamLevelIcons.TryGetValue(CurrentSteamItem.Id.ToString(), out Sprite sprite) ? sprite : SteamWorkshop.inst.defaultSteamImageSprite,
                opacity = 1f,
                val = 40f,
                wait = false,
            });

            var name = RTString.ReplaceFormatting(CurrentSteamItem.Title);
            int size = 110;
            if (name.Length > 13)
                size = (int)(size * ((float)13f / name.Length));

            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Title",
                rect = RectValues.Default.AnchoredPosition(-80f, 320f),
                text = $"<size={size}><b>{name}",
                hideBG = true,
                textColor = 6,
            });

            Match match = null;
            if (RTString.RegexMatch(CurrentSteamItem.Description, new Regex(@"Song Title: (.*?)Song Artist: (.*?)Level Creator: (.*?)Level Difficulty: (.*?)Level Description: (.*?)"), out match))
            {
                elements.Add(new MenuText
                {
                    id = "4624859539",
                    name = "Song",
                    rect = RectValues.Default.AnchoredPosition(-120f, 120f).Pivot(0f, 0.5f).SizeDelta(750f, 100f),
                    text = $"<size=30><b>Song Title</b>: {match.Groups[1]}<br>" +
                           $"<b>Song Artist</b>: {match.Groups[2]}<br>" +
                           $"<b>Level Creator</b>: {match.Groups[3]}<br>" +
                           $"<b>Level Difficulty</b>: {match.Groups[4]}<br>" +
                           $"<b>Level Description</b>:<br><size=24>{match.Groups[5]}<br>",
                    alignment = TMPro.TextAlignmentOptions.TopLeft,
                    enableWordWrapping = true,
                    hideBG = true,
                    textColor = 6,
                });
            }
            else if (RTString.RegexMatch(CurrentSteamItem.Description, new Regex(@"(.*?) By (.*?) Level By: (.*?) Difficulty: (.*?) Description ([0-9a-zA-Z|| /.,!?]+)"), out match))
            {
                elements.Add(new MenuText
                {
                    id = "4624859539",
                    name = "Song",
                    rect = RectValues.Default.AnchoredPosition(-120f, 120f).Pivot(0f, 0.5f).SizeDelta(750f, 100f),
                    text = $"<size=30><b>{match.Groups[1]}</b><br>" +
                           $"<b>Level By</b>: {match.Groups[2]}<br>" +
                           $"<b>Difficulty</b>: {match.Groups[3]}<br>" +
                           $"<b>Description</b>:<br><size=24>{match.Groups[4]}<br>",
                    alignment = TMPro.TextAlignmentOptions.TopLeft,
                    enableWordWrapping = true,
                    hideBG = true,
                    textColor = 6,
                });
            }

            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Tags",
                rect = RectValues.Default.AnchoredPosition(250f, -200f).SizeDelta(800f, 100f),
                text = "<size=22><b>Tags</b>: " + RTString.ArrayToString(CurrentSteamItem.Tags),
                hideBG = true,
                textColor = 6,
                enableWordWrapping = true,
                alignment = TMPro.TextAlignmentOptions.TopLeft,
            });

            elements.Add(new MenuButton
            {
                id = "3525734",
                name = "Download Button",
                rect = RectValues.Default.AnchoredPosition(-500f, -260f).SizeDelta(600f, 64f),
                selectionPosition = new Vector2Int(0, 1),
                text = $"<size=40><b><align=center>[ {(!CurrentSteamItem.IsSubscribed ? "SUBSCRIBE" : "UNSUBSCRIBE")} ]",
                opacity = 0.1f,
                selectedOpacity = 1f,
                color = 6,
                selectedColor = 6,
                textColor = 6,
                selectedTextColor = 7,
                length = 0.5f,
                playBlipSound = true,
                func = () => { CoreHelper.StartCoroutine(DownloadLevel()); },
            });

            int pos = 2;
            if (SteamWorkshopManager.inst.Levels.TryFind(x => x.metadata != null && x.id == CurrentSteamItem.Id.ToString(), out Level level))
            {
                elements.Add(new MenuButton
                {
                    id = "498145857",
                    name = "Play Button",
                    rect = RectValues.Default.AnchoredPosition(-500f, -360f).SizeDelta(600f, 64f),
                    selectionPosition = new Vector2Int(0, pos),
                    text = "<size=40><b><align=center>[ OPEN LEVEL ]",
                    opacity = 0.1f,
                    selectedOpacity = 1f,
                    color = 6,
                    selectedColor = 6,
                    textColor = 6,
                    selectedTextColor = 7,
                    length = 0.5f,
                    playBlipSound = true,
                    func = () => PlayLevelMenu.Init(level),
                });
                pos = 3;
            }
            elements.Add(new MenuButton
            {
                id = "498145857",
                name = "Play Button",
                rect = RectValues.Default.AnchoredPosition(-500f, -360f).SizeDelta(600f, 64f),
                selectionPosition = new Vector2Int(0, pos),
                text = "<size=40><b><align=center>[ OPEN WORKSHOP ]",
                opacity = 0.1f,
                selectedOpacity = 1f,
                color = 6,
                selectedColor = 6,
                textColor = 6,
                selectedTextColor = 7,
                length = 0.5f,
                playBlipSound = true,
                func = () => SteamWorkshopManager.inst.OpenWorkshop(CurrentSteamItem.Id),
            });

            exitFunc = Close;

            allowEffects = false;
            layer = 10000;
            defaultSelection = new Vector2Int(0, 1);
            InterfaceManager.inst.SetCurrentInterface(this);
        }

        public bool downloading;

        public IEnumerator DownloadLevel()
        {
            downloading = true;
            yield return CoreHelper.StartCoroutine(SteamWorkshopManager.inst.ToggleSubscribedState(CurrentSteamItem, onSubscribedLevel));
            downloading = false;
            CurrentSteamItem = default;
        }

        public static void Init(Item item)
        {
            CurrentSteamItem = item;
            Current = new SteamLevelMenu();
        }

        public static void Close()
        {
            CurrentSteamItem = default;
            InterfaceManager.inst.CloseMenus();

            ArcadeMenu.Init();
        }

        public override void Clear()
        {
            CurrentSteamItem = default;
            base.Clear();
        }
    }
}
