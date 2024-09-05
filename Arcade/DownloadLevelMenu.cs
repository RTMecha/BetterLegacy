﻿using System;
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

namespace BetterLegacy.Arcade
{
    public class DownloadLevelMenu : MenuBase
    {
        public static DownloadLevelMenu Current { get; set; }

        public static JSONObject CurrentOnlineLevel { get; set; }

        public DownloadLevelMenu() : base()
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
                rect = RectValues.Default.AnchoredPosition(-560f, 440f).SizeDelta(250f, 64f),
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

            var jn = CurrentOnlineLevel;

            elements.Add(new MenuButton
            {
                id = "4857529985",
                name = "Copy ID",
                rect = RectValues.Default.AnchoredPosition(550f, 440f).SizeDelta(250f, 64f),
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
                func = () => { LSText.CopyToClipboard(jn["id"]); },
            });

            elements.Add(new MenuImage
            {
                id = "5356325",
                name = "Backer",
                rect = RectValues.Default.AnchoredPosition(250f, 100f).SizeDelta(844f, 512f),
                opacity = 0.1f,
                color = 6,
                length = 0.1f,
            });

            elements.Add(new MenuImage
            {
                id = "84682758635",
                name = "Cover",
                rect = RectValues.Default.AnchoredPosition(-430f, 100f).SizeDelta(512f, 512f),
                icon = ArcadeMenu.OnlineLevelIcons.ContainsKey(jn["id"]) ? ArcadeMenu.OnlineLevelIcons[jn["id"]] : SteamWorkshop.inst.defaultSteamImageSprite,
                opacity = 1f,
                val = 40f,
                length = 0.1f,
            });

            var name = CoreHelper.ReplaceFormatting(jn["name"]);
            int size = 110;
            if (name.Length > 13)
                size = (int)(size * ((float)13f / name.Length));

            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Title",
                rect = RectValues.Default.AnchoredPosition(-80f, 280f),
                text = $"<size={size}><b>{name}",
                hideBG = true,
                textColor = 6,
            });

            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Song",
                rect = RectValues.Default.AnchoredPosition(-100f, 200f),
                text = $"<size=40>Song:",
                hideBG = true,
                textColor = 6,
            });

            elements.Add(new MenuText
            {
                id = "638553",
                name = "Song Button",
                rect = RectValues.Default.AnchoredPosition(340f, 200f).SizeDelta(500f, 48f),
                text = $" [ {jn["title"].Value} ]",
                opacity = 0f,
                color = 6,
                textColor = 6,
                length = 0.5f,
                playBlipSound = true,
            });
            
            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Artist",
                rect = RectValues.Default.AnchoredPosition(-100f, 150f),
                text = $"<size=40>Artist:",
                hideBG = true,
                textColor = 6,
            });

            elements.Add(new MenuText
            {
                id = "638553",
                name = "Artist Button",
                rect = RectValues.Default.AnchoredPosition(340f, 150f).SizeDelta(500f, 48f),
                text = $" [ {jn["artist"].Value} ]",
                opacity = 0f,
                color = 6,
                textColor = 6,
                length = 0.5f,
            });

            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Creator",
                rect = RectValues.Default.AnchoredPosition(-100f, 100f),
                text = $"<size=40>Creator:",
                hideBG = true,
                textColor = 6,
            });

            elements.Add(new MenuText
            {
                id = "638553",
                name = "Creator Button",
                rect = RectValues.Default.AnchoredPosition(340f, 100f).SizeDelta(500f, 48f),
                text = $" [ {jn["creator"].Value} ]",
                opacity = 0f,
                color = 6,
                textColor = 6,
                length = 0.5f,
            });

            var difficulty = CoreHelper.GetDifficulty(jn["difficulty"].AsInt);
            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Difficulty",
                rect = RectValues.Default.AnchoredPosition(-100f, 50f),
                text = $"<size=40>Difficulty: <b><#{LSColors.ColorToHex(difficulty.color)}><voffset=-13><size=64>■</voffset><size=40>{difficulty.name}",
                hideBG = true,
                textColor = 6,
            });
            
            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Description Label",
                rect = RectValues.Default.AnchoredPosition(250f, -20f).SizeDelta(800f, 100f),
                text = "<size=40><b>Description:",
                hideBG = true,
                textColor = 6,
                enableWordWrapping = true,
                alignment = TMPro.TextAlignmentOptions.TopLeft,
            });
            
            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Description",
                rect = RectValues.Default.AnchoredPosition(250f, -60f).SizeDelta(800f, 100f),
                text = "<size=22>" + jn["description"],
                hideBG = true,
                textColor = 6,
                enableWordWrapping = true,
                alignment = TMPro.TextAlignmentOptions.TopLeft,
            });

            if (LevelManager.Levels.TryFind(x => x.metadata != null && x.metadata.serverID == jn["id"], out Level level))
            {
                elements.Add(new MenuButton
                {
                    id = "3525734",
                    name = "Download Button",
                    rect = RectValues.Default.AnchoredPosition(-430f, -220f).SizeDelta(512f, 64f),
                    selectionPosition = new Vector2Int(0, 1),
                    text = "<size=40><b><align=center>[ UPDATE ]",
                    opacity = 0.1f,
                    selectedOpacity = 1f,
                    color = 6,
                    selectedColor = 6,
                    textColor = 6,
                    selectedTextColor = 7,
                    length = 0.5f,
                    playBlipSound = true,
                    func = DownloadLevel,
                });

                elements.Add(new MenuButton
                {
                    id = "498145857",
                    name = "Play Button",
                    rect = RectValues.Default.AnchoredPosition(-430f, -320f).SizeDelta(512f, 64f),
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
            }
            else
            {
                elements.Add(new MenuButton
                {
                    id = "3525734",
                    name = "Download Button",
                    rect = RectValues.Default.AnchoredPosition(-430f, -220f).SizeDelta(512f, 64f),
                    selectionPosition = new Vector2Int(0, 1),
                    text = "<size=40><b><align=center>[ DOWNLOAD ]",
                    opacity = 0.1f,
                    selectedOpacity = 1f,
                    color = 6,
                    selectedColor = 6,
                    textColor = 6,
                    selectedTextColor = 7,
                    length = 0.5f,
                    playBlipSound = true,
                    func = DownloadLevel,
                });
            }

            exitFunc = Close;

            allowEffects = false;
            layer = 10000;
            defaultSelection = new Vector2Int(0, 1);
            InterfaceManager.inst.CurrentGenerateUICoroutine = CoreHelper.StartCoroutine(GenerateUI());
        }

        public void DownloadLevel()
        {
            var jn = CurrentOnlineLevel;
            Close();

            var directory = $"{RTFile.ApplicationDirectory}{LevelManager.ListSlash}{jn["id"].Value}";

            CoreHelper.StartCoroutine(AlephNetworkManager.DownloadBytes($"{ArcadeMenu.DownloadURL}{jn["id"].Value}.zip", bytes =>
            {
                if (RTFile.DirectoryExists(directory))
                    Directory.Delete(directory, true);

                Directory.CreateDirectory(directory);

                File.WriteAllBytes($"{directory}.zip", bytes);

                ZipFile.ExtractToDirectory($"{directory}.zip", directory);

                File.Delete($"{directory}.zip");

                var level = new Level(directory + "/");

                LevelManager.Levels.Add(level);

                if (ArcadeConfig.Instance.OpenOnlineLevelAfterDownload.Value)
                    PlayLevelMenu.Init(level);
            }));
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
                yield return CoreHelper.StartCoroutine(level.LoadAudioClipRoutine(() => { OpenPlayLevelMenu(level); }));
            else
                OpenPlayLevelMenu(level);
        }

        void OpenPlayLevelMenu(Level level)
        {
            AudioManager.inst.StopMusic();
            PlayLevelMenu.Init(level);
            AudioManager.inst.PlayMusic(level.metadata.song.title, level.music);
            AudioManager.inst.SetPitch(CoreHelper.Pitch);
        }

        public static void Init(JSONObject level)
        {
            InterfaceManager.inst.CloseMenus();
            CurrentOnlineLevel = level;
            Current = new DownloadLevelMenu();
        }

        public static void Close()
        {
            if (MenuManager.inst)
                AudioManager.inst.PlayMusic(MenuManager.inst.currentMenuMusicName, MenuManager.inst.currentMenuMusic);
            CurrentOnlineLevel = null;
            InterfaceManager.inst.CloseMenus();

            if (ArcadeMenu.useThisUI)
                ArcadeMenu.Init();
        }

        public override void Clear()
        {
            CurrentOnlineLevel = null;
            base.Clear();
        }
    }
}