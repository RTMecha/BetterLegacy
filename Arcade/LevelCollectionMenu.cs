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
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Arcade
{
    public class LevelCollectionMenu : MenuBase
    {
        public static LevelCollectionMenu Current { get; set; }
        public static LevelCollection CurrentCollection { get; set; }

        public LevelCollectionMenu() : base()
        {
            InterfaceManager.inst.CurrentMenu = this;
            this.name = CurrentCollection.name;

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
                    func = () => LSText.CopyToClipboard(CurrentCollection.serverID),
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
                func = () => LSText.CopyToClipboard(CurrentCollection.id),
            });

            elements.Add(new MenuImage
            {
                id = "5356325",
                name = "Backer",
                rect = RectValues.Default.AnchoredPosition(200f, 0f).SizeDelta(900f, 685f),
                opacity = 0.1f,
                color = 6,
                length = 0f,
                wait = false,
            });

            elements.Add(new MenuImage
            {
                id = "84682758635",
                name = "Cover",
                rect = RectValues.Default.AnchoredPosition(-550f, 100f).SizeDelta(500f, 500f),
                icon = CurrentCollection.icon,
                opacity = 1f,
                val = 40f,
                length = 0f,
                wait = false,
            });
            
            elements.Add(new MenuImage
            {
                id = "84682758635",
                name = "Banner",
                rect = RectValues.Default.AnchoredPosition(200f, 200f).SizeDelta(900f, 300f),
                icon = CurrentCollection.banner,
                opacity = 1f,
                val = 40f,
                length = 0f,
                wait = false,
            });

            var name = RTString.ReplaceFormatting(CurrentCollection.name);
            int size = 110;
            if (name.Length > 13)
                size = (int)(size * ((float)13f / name.Length));

            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Title",
                rect = RectValues.Default.AnchoredPosition(-190f, 10f),
                text = $"<size={size}><b>{name}",
                hideBG = true,
                textColor = 6,
            });

            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Description Label",
                rect = RectValues.Default.AnchoredPosition(160f, -90f).SizeDelta(800f, 100f),
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
                rect = RectValues.Default.AnchoredPosition(160f, -140f).SizeDelta(800f, 100f),
                text = "<size=22>" + CurrentCollection.description,
                hideBG = true,
                textColor = 6,
                enableWordWrapping = true,
                alignment = TMPro.TextAlignmentOptions.TopLeft,
            });

            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Tags",
                rect = RectValues.Default.AnchoredPosition(160f, -350f).SizeDelta(800f, 100f),
                text = "<size=22><b>Tags</b>: " + RTString.ArrayToString(CurrentCollection.tags),
                hideBG = true,
                textColor = 6,
                enableWordWrapping = true,
                alignment = TMPro.TextAlignmentOptions.TopLeft,
            });

            elements.Add(new MenuButton
            {
                id = "3525734",
                name = "Play Button",
                rect = RectValues.Default.AnchoredPosition(-550f, -210f).SizeDelta(500f, 64f),
                selectionPosition = new Vector2Int(0, 1),
                text = "<size=40><b><align=center>[ PLAY ]",
                opacity = 0.1f,
                selectedOpacity = 1f,
                color = 6,
                selectedColor = 6,
                textColor = 6,
                selectedTextColor = 7,
                length = 0.5f,
                playBlipSound = true,
                func = () =>
                {
                    LevelManager.currentLevelIndex = CurrentCollection.EntryLevelIndex;
                    if (LevelManager.currentLevelIndex < 0)
                        LevelManager.currentLevelIndex = 0;
                    if (CurrentCollection.Count > 1)
                        LevelManager.CurrentLevel = CurrentCollection[LevelManager.currentLevelIndex];

                    if (!LevelManager.CurrentLevel)
                    {
                        if (CurrentCollection.nullLevels.TryFind(x => x.index == LevelManager.currentLevelIndex, out LevelCollection.NullLevel nullLevel))
                        {
                            CoreHelper.Log($"A collection level was not found. It was probably not installed.\n" +
                                $"Level Name: {nullLevel.name}\n" +
                                $"Song Title: {nullLevel.songTitle}\n" +
                                $"Creator: {nullLevel.creator}\n" +
                                $"Arcade ID: {nullLevel.arcadeID}\n" +
                                $"Server ID: {nullLevel.serverID}\n" +
                                $"Workshop ID: {nullLevel.workshopID}");
                        }
                        else
                            CoreHelper.Log($"Level was not found.");

                        return;
                    }

                    CoreHelper.StartCoroutine(SelectLocalLevel(LevelManager.CurrentLevel));
                },
            });

            elements.Add(new MenuButton
            {
                id = "3525734",
                name = "Levels Button",
                text = "<size=40><b><align=center>[ LEVELS ]",
                rect = RectValues.Default.AnchoredPosition(-550f, -310f).SizeDelta(500f, 64f),
                selectionPosition = new Vector2Int(0, 2),
                opacity = 0.1f,
                selectedOpacity = 1f,
                color = 6,
                selectedColor = 6,
                textColor = 6,
                selectedTextColor = 7,
                length = 0.5f,
                playBlipSound = true,
                func = () =>
                {
                    var currentCollection = CurrentCollection;
                    LevelListMenu.close = () => Init(currentCollection);
                    LevelListMenu.Init(currentCollection.levels);
                },
            });

            exitFunc = Close;
            allowEffects = false;
            layer = 10000;
            defaultSelection = new Vector2Int(0, 4);
            InterfaceManager.inst.CurrentGenerateUICoroutine = CoreHelper.StartCoroutine(GenerateUI());
        }

        public override void UpdateTheme()
        {
            if (Parser.TryParse(MenuConfig.Instance.InterfaceThemeID.Value, -1) >= 0 && InterfaceManager.inst.themes.TryFind(x => x.id == MenuConfig.Instance.InterfaceThemeID.Value, out BeatmapTheme interfaceTheme))
                Theme = interfaceTheme;
            else
                Theme = InterfaceManager.inst.themes[0];

            base.UpdateTheme();
        }

        IEnumerator SelectLocalLevel(Level level)
        {
            if (!level.music)
                yield return CoreHelper.StartCoroutine(level.LoadAudioClipRoutine(() => OpenPlayLevelMenu(level)));
            else
                OpenPlayLevelMenu(level);
        }

        void OpenPlayLevelMenu(Level level)
        {
            AudioManager.inst.StopMusic();
            var collection = CurrentCollection;
            PlayLevelMenu.close = () =>
            {
                Init(collection);
                collection = null;
            };
            PlayLevelMenu.Init(level);
            AudioManager.inst.PlayMusic(level?.metadata?.song?.title, level.music);
            AudioManager.inst.SetPitch(CoreHelper.Pitch);
        }

        public static void Init(LevelCollection collection)
        {
            InterfaceManager.inst.CloseMenus();
            CurrentCollection = collection;
            Current = new LevelCollectionMenu();
        }

        public static void Close()
        {
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
