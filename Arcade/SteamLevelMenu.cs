using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;

using UnityEngine;

using BetterLegacy.Menus;
using BetterLegacy.Menus.UI;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Layouts;
using BetterLegacy.Menus.UI.Interfaces;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using LSFunctions;
using SimpleJSON;
using BetterLegacy.Core.Managers.Networking;
using System.Collections;

using SteamworksFacepunch;
using SteamworksFacepunch.Ugc;


namespace BetterLegacy.Arcade
{
    public class SteamLevelMenu : MenuBase
    {
        public static SteamLevelMenu Current { get; set; }

        public static Item CurrentSteamItem { get; set; }

        public SteamLevelMenu() : base()
        {
            InterfaceManager.inst.CurrentMenu = this;
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

            if (SteamWorkshopManager.inst.Levels.TryFind(x => x.metadata != null && x.id == CurrentSteamItem.Id.ToString(), out Level level))
                elements.Add(new MenuButton
                {
                    id = "498145857",
                    name = "Play Button",
                    rect = RectValues.Default.AnchoredPosition(-500f, -360f).SizeDelta(600f, 64f),
                    selectionPosition = new Vector2Int(0, 2),
                    text = "<size=40><b><align=center>[ OPEN LEVEL ]",
                    opacity = 0.1f,
                    selectedOpacity = 1f,
                    color = 6,
                    selectedColor = 6,
                    textColor = 6,
                    selectedTextColor = 7,
                    length = 0.5f,
                    playBlipSound = true,
                    func = SelectLocalLevel(level).Start,
                });

            exitFunc = Close;

            allowEffects = false;
            layer = 10000;
            defaultSelection = new Vector2Int(0, 1);
            InterfaceManager.inst.CurrentGenerateUICoroutine = CoreHelper.StartCoroutine(GenerateUI());
        }

        public bool downloading;
        void LogItem(Item item) => CoreHelper.Log(
                            $"Downloading item: {item.Id} ({item.Title})\n" +
                            $"Download Pending: {item.IsDownloadPending}\n" +
                            $"Downloading: {item.IsDownloading}\n" +
                            $"Download Amount: {item.DownloadAmount}\n" +
                            $"Installed: {item.IsInstalled}\n" +
                            $"Subscribed: {item.IsSubscribed}");

        public IEnumerator DownloadLevel()
        {
            CoreHelper.LogSeparator();
            var item = CurrentSteamItem;
            CoreHelper.Log($"Beginning {(!item.IsSubscribed ? "subscribing" : "unsubscribing")} of {item.Id}\nTitle: {item.Title}");
            InterfaceManager.inst.PlayMusic();
            InterfaceManager.inst.CloseMenus();

            ProgressMenu.Init($"Updating Steam item: {item.Id} - {item.Title}<br>Please wait...");

            var subscribed = item.IsSubscribed;
            downloading = true;
            if (!subscribed)
            {
                CoreHelper.Log($"Subscribing...");
                yield return item.Subscribe();

                LogItem(item);
                yield return item.DownloadAsync(progress =>
                {
                    try
                    {
                        ProgressMenu.Current.UpdateProgress(item.DownloadAmount);
                    }
                    catch
                    {

                    }
                });

                while (!item.IsInstalled || item.IsDownloadPending || item.IsDownloading)
                {
                    if (Input.GetKeyDown(KeyCode.A))
                        LogItem(item);

                    try
                    {
                        ProgressMenu.Current.UpdateProgress(item.DownloadAmount);
                    }
                    catch
                    {

                    }

                    yield return null;
                }
                ProgressMenu.Current.UpdateProgress(1f);

                subscribed = true;
                LogItem(item);
            }
            else
            {
                CoreHelper.Log($"Unsubscribing...");
                yield return item.Unsubscribe();

                LogItem(item);
                yield return item.DownloadAsync();

                subscribed = false;
                LogItem(item);
            }
            downloading = false;

            yield return new WaitForSeconds(0.1f);
            CoreHelper.Log($"{item.Id} Status: {(subscribed ? "Subscribed" : "Unsubscribed")}");

            while (InterfaceManager.inst.CurrentMenu != null && InterfaceManager.inst.CurrentMenu.generating)
                yield return null;

            int levelIndex = -1;
            if (!subscribed && SteamWorkshopManager.inst.Levels.TryFindIndex(x => x.metadata != null && x.id == item.Id.ToString(), out levelIndex))
            {
                CoreHelper.Log($"Unsubscribed > Remove level {SteamWorkshopManager.inst.Levels[levelIndex]}.");
                SteamWorkshopManager.inst.Levels.RemoveAt(levelIndex);
            }

            if (subscribed && item.IsInstalled && Level.TryVerify(item.Directory, true, out Level level))
            {
                CoreHelper.Log($"Subscribed > Add level {level.path}.");
                SteamWorkshopManager.inst.Levels.Add(level);
                CoreHelper.StartCoroutine(SelectLocalLevel(level));
                CoreHelper.Log($"Item is installed so opening.");
                CoreHelper.LogSeparator();
                CurrentSteamItem = default;
                yield break;
            }
            else if (subscribed)
                CoreHelper.LogError($"Item doesn't exist.");

            CoreHelper.Log($"Finished.");
            CoreHelper.LogSeparator();
            CurrentSteamItem = default;
            ArcadeMenu.Init();
        }

        public override void UpdateTheme()
        {
            if (Parser.TryParse(MenuConfig.Instance.InterfaceThemeID.Value, -1) >= 0 && InterfaceManager.inst.themes.TryFind(x => x.id == MenuConfig.Instance.InterfaceThemeID.Value, out BeatmapTheme interfaceTheme))
                Theme = interfaceTheme;
            else
                Theme = InterfaceManager.inst.themes[0];

            base.UpdateTheme();
        }

        public IEnumerator SelectLocalLevel(Level level)
        {
            if (!level.music)
                CoreHelper.StartCoroutine(level.LoadAudioClipRoutine(() => OpenPlayLevelMenu(level)));
            else
                OpenPlayLevelMenu(level);
            yield break;
        }

        void OpenPlayLevelMenu(Level level)
        {
            AudioManager.inst.StopMusic();
            PlayLevelMenu.Init(level);
            AudioManager.inst.PlayMusic(level.metadata.song.title, level.music);
            AudioManager.inst.SetPitch(CoreHelper.Pitch);
        }

        public static void Init(Item item)
        {
            InterfaceManager.inst.CloseMenus();
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
