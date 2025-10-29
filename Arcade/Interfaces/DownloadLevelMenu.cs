using System;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Menus;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Interfaces;

namespace BetterLegacy.Arcade.Interfaces
{
    public class DownloadLevelMenu : MenuBase
    {
        public static DownloadLevelMenu Current { get; set; }

        public static JSONObject CurrentOnlineLevel { get; set; }
        public static int Type { get; set; }

        public Action<Level> onDownloadComplete;

        public Action<LevelCollection> onDownloadCollectionComplete;

        public DownloadLevelMenu() : base()
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

            var jn = CurrentOnlineLevel;

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
                func = () => LSText.CopyToClipboard(jn["id"]),
            });

            elements.Add(new MenuImage
            {
                id = "5356325",
                name = "Backer",
                rect = RectValues.Default.AnchoredPosition(250f, 100f).SizeDelta(900f, 600f),
                opacity = 0.1f,
                color = 6,
                length = 0f,
                wait = false,
            });

            elements.Add(new MenuImage
            {
                id = "84682758635",
                name = "Cover",
                rect = RectValues.Default.AnchoredPosition(-500f, 100f).SizeDelta(600f, 600f),
                icon = ArcadeMenu.OnlineLevelIcons.TryGetValue(jn["id"], out Sprite sprite) ? sprite : LegacyPlugin.AtanPlaceholder,
                opacity = 1f,
                val = 40f,
                length = 0f,
                wait = false,
            });

            var name = RTString.ReplaceFormatting(jn["name"]);
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

            if (Type == 0)
            {
                elements.Add(new MenuText
                {
                    id = "4624859539",
                    name = "Song",
                    rect = RectValues.Default.AnchoredPosition(-100f, 240f),
                    text = $"<size=40>Song:",
                    hideBG = true,
                    textColor = 6,
                });
                elements.Add(new MenuText
                {
                    id = "638553",
                    name = "Song Button",
                    rect = RectValues.Default.AnchoredPosition(340f, 240f).SizeDelta(500f, 48f),
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
                    rect = RectValues.Default.AnchoredPosition(-100f, 190f),
                    text = $"<size=40>Artist:",
                    hideBG = true,
                    textColor = 6,
                });
                elements.Add(new MenuText
                {
                    id = "638553",
                    name = "Artist Button",
                    rect = RectValues.Default.AnchoredPosition(340f, 190f).SizeDelta(500f, 48f),
                    text = $" [ {jn["artist"].Value} ]",
                    opacity = 0f,
                    color = 6,
                    textColor = 6,
                    length = 0.5f,
                });
            }
            
            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Creator",
                rect = RectValues.Default.AnchoredPosition(-100f, 140f),
                text = $"<size=40>Creator:",
                hideBG = true,
                textColor = 6,
            });
            elements.Add(new MenuText
            {
                id = "638553",
                name = "Creator Button",
                rect = RectValues.Default.AnchoredPosition(340f, 140f).SizeDelta(500f, 48f),
                text = $" [ {jn["creator"].Value} ]",
                opacity = 0f,
                color = 6,
                textColor = 6,
                length = 0.5f,
            });

            var difficulty = CustomEnumHelper.GetValueOrDefault(jn["difficulty"].AsInt, DifficultyType.Unknown);
            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Difficulty",
                rect = RectValues.Default.AnchoredPosition(-100f, 90f),
                text = $"<size=40>Difficulty: <b><#{LSColors.ColorToHex(difficulty.Color)}><voffset=-13><size=64>■</voffset><size=40>{difficulty.DisplayName}",
                hideBG = true,
                textColor = 6,
            });
            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Description Label",
                rect = RectValues.Default.AnchoredPosition(250f, 20f).SizeDelta(800f, 100f),
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
                rect = RectValues.Default.AnchoredPosition(250f, -20f).SizeDelta(800f, 100f),
                text = "<size=22>" + jn["description"],
                hideBG = true,
                textColor = 6,
                enableWordWrapping = true,
                alignment = TMPro.TextAlignmentOptions.TopLeft,
            });

            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Tags",
                rect = RectValues.Default.AnchoredPosition(250f, -200f).SizeDelta(800f, 100f),
                text = "<size=22><b>Tags</b>: " + jn["tags"].Value,
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
                    rect = RectValues.Default.AnchoredPosition(-500f, -260f).SizeDelta(600f, 64f),
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
                    func = () => PlayLevelMenu.Init(level),
                });
            }
            else if (LevelManager.LevelCollections.TryFind(x => x.serverID == jn["id"], out LevelCollection levelCollection))
            {
                elements.Add(new MenuButton
                {
                    id = "3525734",
                    name = "Download Button",
                    rect = RectValues.Default.AnchoredPosition(-500f, -260f).SizeDelta(600f, 64f),
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
                    rect = RectValues.Default.AnchoredPosition(-500f, -360f).SizeDelta(600f, 64f),
                    selectionPosition = new Vector2Int(0, 2),
                    text = "<size=40><b><align=center>[ OPEN COLLECTION ]",
                    opacity = 0.1f,
                    selectedOpacity = 1f,
                    color = 6,
                    selectedColor = 6,
                    textColor = 6,
                    selectedTextColor = 7,
                    length = 0.5f,
                    playBlipSound = true,
                    func = () => LevelCollectionMenu.Init(levelCollection),
                });
            }
            else
            {
                elements.Add(new MenuButton
                {
                    id = "3525734",
                    name = "Download Button",
                    rect = RectValues.Default.AnchoredPosition(-500f, -260f).SizeDelta(600f, 64f),
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
            InterfaceManager.inst.SetCurrentInterface(this);
        }

        public void DownloadLevel()
        {
            var jn = CurrentOnlineLevel;

            switch (Type)
            {
                case 0: {
                        AlephNetwork.DownloadLevel(jn, level =>
                        {
                            CurrentOnlineLevel = null;
                            InterfaceManager.inst.CloseMenus();

                            if (onDownloadComplete != null)
                            {
                                onDownloadComplete(level);
                                onDownloadComplete = null;
                                return;
                            }

                            if (ArcadeConfig.Instance.OpenOnlineLevelAfterDownload.Value)
                                PlayLevelMenu.Init(level);
                        }, onError =>
                        {
                            Close();
                            CoreHelper.Log($"Failed to download item: {jn}");
                        });
                        break;
                    }
                case 1: {
                        AlephNetwork.DownloadLevelCollection(jn, levelCollection =>
                        {
                            CurrentOnlineLevel = null;
                            Type = 0;
                            InterfaceManager.inst.CloseMenus();

                            if (onDownloadCollectionComplete != null)
                            {
                                onDownloadCollectionComplete(levelCollection);
                                onDownloadCollectionComplete = null;
                                return;
                            }

                            if (ArcadeConfig.Instance.OpenOnlineLevelAfterDownload.Value)
                                LevelCollectionMenu.Init(levelCollection);
                        }, onError =>
                        {
                            Close();
                            CoreHelper.Log($"Failed to download item: {jn}");
                        });
                        break;
                    }
            }
        }

        public static void Init(JSONObject level, int type = 0)
        {
            RTBeatmap.Current?.Pause();
            CurrentOnlineLevel = level;
            Type = type;
            Current = new DownloadLevelMenu();
        }

        public static void Close()
        {
            RTBeatmap.Current?.Resume();
            CurrentOnlineLevel = null;
            Type = 0;
            InterfaceManager.inst.CloseMenus();

            ArcadeMenu.Init();
        }

        public override void Clear()
        {
            CurrentOnlineLevel = null;
            base.Clear();
        }
    }
}
