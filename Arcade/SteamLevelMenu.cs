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
using LSFunctions;
using SimpleJSON;
using BetterLegacy.Core.Managers.Networking;
using System.Collections;
using System.IO;
using System.IO.Compression;
using SteamworksFacepunch.Ugc;
using System.Text.RegularExpressions;

namespace BetterLegacy.Arcade
{
    public class SteamLevelMenu : MenuBase
    {
        public static SteamLevelMenu Current { get; set; }

        public static Item CurrentSteamItem { get; set; }

        public SteamLevelMenu() : base()
        {
            InterfaceManager.inst.CurrentMenu = this;

            elements.Add(new MenuEvent
            {
                id = "09",
                name = "Effects",
                func = () => { MenuEffectsManager.inst.UpdateChroma(0.1f); },
                length = 0f,
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
                func = () => { LSText.CopyToClipboard(CurrentSteamItem.Id.ToString()); },
            });

            elements.Add(new MenuImage
            {
                id = "5356325",
                name = "Backer",
                rect = RectValues.Default.AnchoredPosition(250f, 100f).SizeDelta(900f, 600f),
                opacity = 0.1f,
                color = 6,
                length = 0.1f,
            });

            elements.Add(new MenuImage
            {
                id = "84682758635",
                name = "Cover",
                rect = RectValues.Default.AnchoredPosition(-500f, 100f).SizeDelta(600f, 600f),
                icon = ArcadeMenu.OnlineSteamLevelIcons.TryGetValue(CurrentSteamItem.Id.ToString(), out Sprite sprite) ? sprite : SteamWorkshop.inst.defaultSteamImageSprite,
                opacity = 1f,
                val = 40f,
                length = 0.1f,
            });

            var name = CoreHelper.ReplaceFormatting(CurrentSteamItem.Title);
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
            if (CoreHelper.RegexMatch(CurrentSteamItem.Description, new Regex(@"Song Title: (.*?)Song Artist: (.*?)Level Creator: (.*?)Level Difficulty: (.*?)Level Description: (.*?)"), out match))
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
            else if (CoreHelper.RegexMatch(CurrentSteamItem.Description, new Regex(@"(.*?) By (.*?) Level By: (.*?) Difficulty: (.*?) Description ([0-9a-zA-Z|| /.,!?]+)"), out match))
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

            //elements.Add(new MenuText
            //{
            //    id = "638553",
            //    name = "Song Button",
            //    rect = RectValues.Default.AnchoredPosition(340f, 240f).SizeDelta(500f, 48f),
            //    text = $" [ {jn["title"].Value} ]",
            //    opacity = 0f,
            //    color = 6,
            //    textColor = 6,
            //    length = 0.5f,
            //    playBlipSound = true,
            //});

            //elements.Add(new MenuText
            //{
            //    id = "4624859539",
            //    name = "Artist",
            //    rect = RectValues.Default.AnchoredPosition(-100f, 190f),
            //    text = $"<size=40>Artist:",
            //    hideBG = true,
            //    textColor = 6,
            //});

            //elements.Add(new MenuText
            //{
            //    id = "638553",
            //    name = "Artist Button",
            //    rect = RectValues.Default.AnchoredPosition(340f, 190f).SizeDelta(500f, 48f),
            //    text = $" [ {jn["artist"].Value} ]",
            //    opacity = 0f,
            //    color = 6,
            //    textColor = 6,
            //    length = 0.5f,
            //});

            //elements.Add(new MenuText
            //{
            //    id = "4624859539",
            //    name = "Creator",
            //    rect = RectValues.Default.AnchoredPosition(-100f, 140f),
            //    text = $"<size=40>Creator:",
            //    hideBG = true,
            //    textColor = 6,
            //});

            //elements.Add(new MenuText
            //{
            //    id = "638553",
            //    name = "Creator Button",
            //    rect = RectValues.Default.AnchoredPosition(340f, 140f).SizeDelta(500f, 48f),
            //    text = $" [ {jn["creator"].Value} ]",
            //    opacity = 0f,
            //    color = 6,
            //    textColor = 6,
            //    length = 0.5f,
            //});

            //var difficulty = CoreHelper.GetDifficulty(jn["difficulty"].AsInt);
            //elements.Add(new MenuText
            //{
            //    id = "4624859539",
            //    name = "Difficulty",
            //    rect = RectValues.Default.AnchoredPosition(-100f, 90f),
            //    text = $"<size=40>Difficulty: <b><#{LSColors.ColorToHex(difficulty.color)}><voffset=-13><size=64>■</voffset><size=40>{difficulty.name}",
            //    hideBG = true,
            //    textColor = 6,
            //});

            //elements.Add(new MenuText
            //{
            //    id = "4624859539",
            //    name = "Description Label",
            //    rect = RectValues.Default.AnchoredPosition(250f, 20f).SizeDelta(800f, 100f),
            //    text = "<size=40><b>Description:",
            //    hideBG = true,
            //    textColor = 6,
            //    enableWordWrapping = true,
            //    alignment = TMPro.TextAlignmentOptions.TopLeft,
            //});

            //elements.Add(new MenuText
            //{
            //    id = "4624859539",
            //    name = "Description",
            //    rect = RectValues.Default.AnchoredPosition(250f, -20f).SizeDelta(800f, 100f),
            //    text = "<size=22>" + jn["description"],
            //    hideBG = true,
            //    textColor = 6,
            //    enableWordWrapping = true,
            //    alignment = TMPro.TextAlignmentOptions.TopLeft,
            //});

            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Tags",
                rect = RectValues.Default.AnchoredPosition(250f, -200f).SizeDelta(800f, 100f),
                text = "<size=22><b>Tags</b>: " + FontManager.TextTranslater.ArrayToString(CurrentSteamItem.Tags),
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
                    func = () => { CoreHelper.StartCoroutine(SelectLocalLevel(level)); },
                });

            exitFunc = Close;

            allowEffects = false;
            layer = 10000;
            defaultSelection = new Vector2Int(0, 1);
            InterfaceManager.inst.CurrentGenerateUICoroutine = CoreHelper.StartCoroutine(GenerateUI());
        }

        public IEnumerator DownloadLevel()
        {
            CoreHelper.LogSeparator();
            var item = CurrentSteamItem;
            CoreHelper.Log($"Beginning {(!item.IsSubscribed ? "subscribing" : "unsubscribing")} of {item.Id}\nTitle: {item.Title}");
            InterfaceManager.inst.PlayMusic();
            InterfaceManager.inst.CloseMenus();

            ProgressMenu.Init($"Updating Steam item: {item.Id} - {item.Title}<br>Please wait...");

            if (!item.IsSubscribed)
            {
                CoreHelper.Log($"Subscribing...");
                item.Subscribe();
                CoreHelper.Log($"Download Pending: {item.IsDownloadPending}");
                CoreHelper.Log($"Downloading: {item.IsDownloading}");
                CoreHelper.Log($"Installed: {item.IsInstalled}");
                CoreHelper.Log($"Subscribed: {item.IsSubscribed}");

                yield return new WaitUntil(() =>
                {
                    CoreHelper.ReturnToUnity(() =>
                    {
                        if (!Input.GetKeyDown(KeyCode.A))
                            return;
                        CoreHelper.Log($"Download Pending: {item.IsDownloadPending}");
                        CoreHelper.Log($"Downloading: {item.IsDownloading}");
                        CoreHelper.Log($"Installed: {item.IsInstalled}");
                        CoreHelper.Log($"Subscribed: {item.IsSubscribed}");
                    });

                    if (item.DownloadAmount != 1f)
                    {
                        try
                        {
                            ProgressMenu.Current.UpdateProgress(item.DownloadAmount);
                        }
                        catch
                        {

                        }
                    }
                    return item.IsSubscribed && item.IsInstalled;
                });
            }
            else
            {
                CoreHelper.Log($"Unsubscribing...");
                item.Unsubscribe();
                item.Download();
                CoreHelper.Log($"Download Pending: {item.IsDownloadPending}");
                CoreHelper.Log($"Downloading: {item.IsDownloading}");
                CoreHelper.Log($"Installed: {item.IsInstalled}");
                CoreHelper.Log($"Subscribed: {item.IsSubscribed}");

                yield return new WaitUntil(() =>
                {
                    CoreHelper.ReturnToUnity(() =>
                    {
                        if (!Input.GetKeyDown(KeyCode.A))
                            return;
                        CoreHelper.Log($"Download Pending: {item.IsDownloadPending}");
                        CoreHelper.Log($"Downloading: {item.IsDownloading}");
                        CoreHelper.Log($"Installed: {item.IsInstalled}");
                        CoreHelper.Log($"Subscribed: {item.IsSubscribed}");
                    });

                    if (item.DownloadAmount != 1f)
                    {
                        try
                        {
                            ProgressMenu.Current.UpdateProgress(item.DownloadAmount);
                        }
                        catch
                        {

                        }
                    }
                    return !item.IsSubscribed && !item.IsDownloadPending && !item.IsDownloading;
                });
            }

            yield return new WaitForSeconds(0.1f);
            CoreHelper.Log($"{item.Id} Status: {(item.IsSubscribed ? "Subscribed" : "Unsubscribed")}");

            int levelIndex = -1;
            if (!item.IsSubscribed && SteamWorkshopManager.inst.Levels.TryFindIndex(x => x.metadata != null && x.id == item.Id.ToString(), out levelIndex))
            {
                CoreHelper.Log($"Unsubscribed > Remove level {SteamWorkshopManager.inst.Levels[levelIndex]}.");
                SteamWorkshopManager.inst.Levels.RemoveAt(levelIndex);
            }
            if (item.IsSubscribed)
            {
                var path = item.Directory.Last() != '/' ? item.Directory + "/" : item.Directory;
                CoreHelper.Log($"Subscribed > Add level {path}.");
                SteamWorkshopManager.inst.Levels.Add(new Level(path));
            }

            CoreHelper.Log($"Checking install state...");
            if (item.IsSubscribed && item.IsInstalled)
            {
                if (SteamWorkshopManager.inst.Levels.TryFind(x => x.metadata != null && x.id == item.Id.ToString(), out Level level))
                {
                    CoreHelper.StartCoroutine(SelectLocalLevel(level));
                    CoreHelper.Log($"Item is installed so opening.");
                }
                else
                {
                    ArcadeMenu.Init();
                    CoreHelper.Log($"Item doesn't exist.");
                }

                CoreHelper.LogSeparator();
                CurrentSteamItem = default;
                yield break;
            }

            while (InterfaceManager.inst.CurrentMenu != null && InterfaceManager.inst.CurrentMenu.generating)
                yield return null;

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
                CoreHelper.StartCoroutine(level.LoadAudioClipRoutine(() => { OpenPlayLevelMenu(level); }));
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
