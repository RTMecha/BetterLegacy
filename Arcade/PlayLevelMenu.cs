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
using BetterLegacy.Core.Managers.Networking;

namespace BetterLegacy.Arcade
{
    public class PlayLevelMenu : MenuBase
    {
        public static PlayLevelMenu Current { get; set; }
        public static Level CurrentLevel { get; set; }

        public PlayLevelMenu() : base()
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

            if (CurrentLevel.metadata != null && !string.IsNullOrEmpty(CurrentLevel.metadata.serverID))
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
                    func = () => { LSText.CopyToClipboard(CurrentLevel.metadata?.serverID); },
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
                func = () => { LSText.CopyToClipboard(CurrentLevel.metadata?.arcadeID); },
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
                id = "526526526",
                name = "Type",
                rect = RectValues.Default.AnchoredPosition(650f, 350f).SizeDelta(64f, 64f),
                useOverrideColor = true,
                overrideColor = Color.white,
                opacity = 1f,
                length = 0f,
                icon = CurrentLevel.IsVG ? LegacyPlugin.PAVGLogoSprite : LegacyPlugin.PALogoSprite,
            });

            elements.Add(new MenuImage
            {
                id = "84682758635",
                name = "Cover",
                rect = RectValues.Default.AnchoredPosition(-500f, 100f).SizeDelta(600f, 600f),
                icon = CurrentLevel.icon,
                opacity = 1f,
                val = 40f,
                length = 0.1f,
            });

            var name = CoreHelper.ReplaceFormatting(CurrentLevel.metadata.beatmap.name);
            int size = 110;
            if (name.Length > 13 && name.Length <= 40)
                size = (int)(size * ((float)13f / name.Length));
            if (name.Length > 40)
                name = LSText.ClampString(name, 40);

            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Title",
                rect = RectValues.Default.AnchoredPosition(-80f, 320f),
                text = $"<size={size}><b>{name}",
                hideBG = true,
                textColor = 6,
            });

            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Song",
                rect = RectValues.Default.AnchoredPosition(-100f, 240f),
                text = $"<size=40>Song:",
                hideBG = true,
                textColor = 6,
            });

            var title = CoreHelper.ReplaceFormatting(CurrentLevel.metadata.song.title);
            size = 32;
            if (title.Length > 24 && title.Length <= 40)
                size = (int)(size * ((float)24f / title.Length));
            if (title.Length > 40)
                title = LSText.ClampString(title, 40);

            elements.Add(new MenuButton
            {
                id = "638553",
                name = "Song Button",
                rect = RectValues.Default.AnchoredPosition(340f, 240f).SizeDelta(500f, 48f),
                selectionPosition = new Vector2Int(0, 1),
                text = $"<size={size}> [ {CurrentLevel.metadata.song.title} ]",
                opacity = 0f,
                selectedOpacity = 1f,
                color = 6,
                selectedColor = 6,
                textColor = 6,
                selectedTextColor = 7,
                length = 0.5f,
                playBlipSound = true,
                func = () =>
                {
                    if (CurrentLevel.metadata != null && !string.IsNullOrEmpty(CurrentLevel.metadata.SongURL))
                        Application.OpenURL(CurrentLevel.metadata.SongURL);
                },
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

            var artist = CoreHelper.ReplaceFormatting(CurrentLevel.metadata.artist.Name);
            size = 32;
            if (artist.Length > 24 && artist.Length <= 40)
                size = (int)(size * ((float)24f / artist.Length));
            if (artist.Length > 40)
                title = LSText.ClampString(title, 40);

            elements.Add(new MenuButton
            {
                id = "638553",
                name = "Artist Button",
                rect = RectValues.Default.AnchoredPosition(340f, 190f).SizeDelta(500f, 48f),
                selectionPosition = new Vector2Int(0, 2),
                text = $"<size={size}> [ {artist} ]",
                opacity = 0f,
                selectedOpacity = 1f,
                color = 6,
                selectedColor = 6,
                textColor = 6,
                selectedTextColor = 7,
                length = 0.5f,
                playBlipSound = true,
                func = () =>
                {
                    if (CurrentLevel.metadata != null && !string.IsNullOrEmpty(CurrentLevel.metadata.artist.URL))
                        Application.OpenURL(CurrentLevel.metadata.artist.URL);
                },
            });

            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Creator",
                rect = RectValues.Default.AnchoredPosition(-100f, 140f),
                text = $"<size=40>Creator:",
                hideBG = true,
                textColor = 6,
            });

            var creator = CoreHelper.ReplaceFormatting(CurrentLevel.metadata.creator.steam_name);
            size = 32;
            if (creator.Length > 24 && creator.Length <= 40)
                size = (int)(size * ((float)24f / creator.Length));
            if (creator.Length > 40)
                title = LSText.ClampString(title, 40);

            elements.Add(new MenuButton
            {
                id = "638553",
                name = "Creator Button",
                rect = RectValues.Default.AnchoredPosition(340f, 140f).SizeDelta(500f, 48f),
                selectionPosition = new Vector2Int(0, 3),
                text = $"<size={size}> [ {creator} ]",
                opacity = 0f,
                selectedOpacity = 1f,
                color = 6,
                selectedColor = 6,
                textColor = 6,
                selectedTextColor = 7,
                length = 0.5f,
                playBlipSound = true,
                func = () =>
                {
                    var currentLevel = CurrentLevel;
                    if (currentLevel.metadata != null && !string.IsNullOrEmpty(currentLevel.metadata.uploaderID))
                    {
                        LevelListMenu.Init($"{AlephNetworkManager.ArcadeServerURL}api/Level/user/{currentLevel.metadata.uploaderID}");
                        LevelListMenu.close = () => Init(currentLevel);
                    }
                },
            });

            var difficulty = CoreHelper.GetDifficulty(CurrentLevel.metadata.song.difficulty);
            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Difficulty",
                rect = RectValues.Default.AnchoredPosition(-100f, 90f),
                text = $"<size=40>Difficulty: <b><#{LSColors.ColorToHex(difficulty.color)}><voffset=-13><size=64>■</voffset><size=40>{difficulty.name}",
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
                rect = RectValues.Default.AnchoredPosition(250f, -60f).SizeDelta(800f, 170f),
                text = "<size=22>" + CurrentLevel.metadata.song.description,
                hideBG = true,
                textColor = 6,
                enableWordWrapping = true,
                alignment = TMPro.TextAlignmentOptions.TopLeft,
                overflowMode = TMPro.TextOverflowModes.Truncate,
            });
            
            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Tags",
                rect = RectValues.Default.AnchoredPosition(250f, -200f).SizeDelta(800f, 100f),
                text = "<size=22><b>Tags</b>: " + FontManager.TextTranslater.ArrayToString(CurrentLevel.metadata.song.tags),
                hideBG = true,
                textColor = 6,
                enableWordWrapping = true,
                alignment = TMPro.TextAlignmentOptions.TopLeft,
            });

            var levelRank = LevelManager.GetLevelRank(CurrentLevel);
            elements.Add(new MenuText
            {
                id = "92595",
                name = "Rank",
                rect = RectValues.Default.AnchoredPosition(-250f, -90f).Rotation(-10f),
                text = $"<size=140><b><align=center><#{LSColors.ColorToHex(levelRank.color)}>{levelRank.name}",
                hideBG = true,
                textColor = 6,
            });

            elements.Add(new MenuButton
            {
                id = "3525734",
                name = "Play Button",
                rect = RectValues.Default.AnchoredPosition(-500f, -260f).SizeDelta(600f, 64f),
                selectionPosition = new Vector2Int(0, 4),
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
                    if (LevelManager.CurrentLevelCollection != null)
                    {
                        if (LevelManager.currentLevelIndex < 0)
                            LevelManager.currentLevelIndex = 0;
                        if (LevelManager.CurrentLevelCollection.Count > 1)
                            LevelManager.CurrentLevel = LevelManager.CurrentLevelCollection[LevelManager.currentLevelIndex];
                        else
                            LevelManager.CurrentLevel = CurrentLevel;
                    }
                    else if (LevelManager.ArcadeQueue.Count > 1)
                    {
                        LevelManager.currentQueueIndex = 0;
                        LevelManager.CurrentLevel = LevelManager.ArcadeQueue[0];
                    }
                    else
                        LevelManager.CurrentLevel = CurrentLevel;

                    InterfaceManager.inst.CloseMenus();
                    LevelManager.OnLevelEnd = ArcadeHelper.EndOfLevel;
                    CoreHelper.StartCoroutine(LevelManager.Play(LevelManager.CurrentLevel));
                },
            });

            if (LevelManager.CurrentLevelCollection == null)
            {
                var queueButton = new MenuButton
                {
                    id = "3525734",
                    name = "Queue Button",
                    text = $"<size=40><b><align=center>[ {(LevelManager.ArcadeQueue.Has(x => x.id == CurrentLevel.id) ? "REMOVE FROM" : "ADD TO")} QUEUE ]",
                    rect = RectValues.Default.AnchoredPosition(-500f, -360f).SizeDelta(600f, 64f),
                    selectionPosition = new Vector2Int(0, 5),
                    opacity = 0.1f,
                    selectedOpacity = 1f,
                    color = 6,
                    selectedColor = 6,
                    textColor = 6,
                    selectedTextColor = 7,
                    length = 0.5f,
                    playBlipSound = true,
                };
                queueButton.func = () =>
                {
                    if (LevelManager.ArcadeQueue.Has(x => x.id == CurrentLevel.id))
                    {
                        CoreHelper.Log($"Remove from Queue {CurrentLevel.id}");
                        LevelManager.ArcadeQueue.RemoveAll(x => x.id == CurrentLevel.id);
                    }
                    else
                    {
                        CoreHelper.Log($"Add to Queue {CurrentLevel.id}");
                        LevelManager.ArcadeQueue.Add(CurrentLevel);
                    }

                    queueButton.text = $"<b><align=center>[ {(LevelManager.ArcadeQueue.Has(x => x.id == CurrentLevel.id) ? "REMOVE FROM" : "ADD TO")} QUEUE ]";
                    queueButton.textUI.maxVisibleCharacters = queueButton.text.Length;
                    queueButton.textUI.text = queueButton.text;
                };

                elements.Add(queueButton);
            }

            var ldmSetting = new MenuButton
            {
                id = "0",
                name = "LDM Setting",
                text = $"<size=40><b><align=center>[ LOW DETAIL: {(CoreConfig.Instance.LDM.Value ? "ON" : "OFF")} ]",
                rect = RectValues.Default.AnchoredPosition(60f, -260f).SizeDelta(400f, 64f),
                selectionPosition = new Vector2Int(1, 4),
                opacity = 0.1f,
                selectedOpacity = 1f,
                color = 6,
                selectedColor = 6,
                textColor = 6,
                selectedTextColor = 7,
                length = 0.5f,
                playBlipSound = true,
            };
            ldmSetting.func = () =>
            {
                CoreConfig.Instance.LDM.Value = !CoreConfig.Instance.LDM.Value;
                ldmSetting.text = $"<size=40><b><align=center>[ LOW DETAIL: {(CoreConfig.Instance.LDM.Value ? "ON" : "OFF")} ]";
                ldmSetting.textUI.maxVisibleCharacters = ldmSetting.text.Length;
                ldmSetting.textUI.text = ldmSetting.text;
            };
            elements.Add(ldmSetting);

            var speedText = new MenuText
            {
                id = "0",
                name = "Speed Text",
                text = $"<align=center>{CoreHelper.Pitch.ToString("0.0")}x SPEED",
                rect = RectValues.Default.AnchoredPosition(510f, -260f).SizeDelta(64f, 64f),
                hideBG = true,
                color = 6,
                textColor = 6,
                length = 0.5f,
            };

            elements.Add(new MenuButton
            {
                id = "0",
                name = "Decrease Speed",
                text = "<size=40><b><align=center><",
                rect = RectValues.Default.AnchoredPosition(350f, -260f).SizeDelta(64f, 64f),
                selectionPosition = new Vector2Int(2, 4),
                opacity = 0.1f,
                selectedOpacity = 1f,
                color = 6,
                selectedColor = 6,
                textColor = 6,
                selectedTextColor = 7,
                length = 0.5f,
                playBlipSound = false,
                func = () =>
                {
                    var speed = PlayerManager.ArcadeGameSpeed - 1;
                    if (speed < 0)
                    {
                        AudioManager.inst.PlaySound("Block");
                        return;
                    }
                    AudioManager.inst.PlaySound("blip");

                    PlayerManager.SetGameSpeed(speed);
                    AudioManager.inst.SetPitch(CoreHelper.Pitch);
                    speedText.text = $"<align=center>{CoreHelper.Pitch.ToString("0.0")}x SPEED";
                    speedText.textUI.maxVisibleCharacters = speedText.text.Length;
                    speedText.textUI.text = speedText.text;
                },
            });

            elements.Add(speedText);

            elements.Add(new MenuButton
            {
                id = "0",
                name = "Increase Speed",
                text = "<size=40><b><align=center>>",
                rect = RectValues.Default.AnchoredPosition(670f, -260f).SizeDelta(64f, 64f),
                selectionPosition = new Vector2Int(3, 4),
                opacity = 0.1f,
                selectedOpacity = 1f,
                color = 6,
                selectedColor = 6,
                textColor = 6,
                selectedTextColor = 7,
                length = 0.5f,
                playBlipSound = false,
                func = () =>
                {
                    var speed = PlayerManager.ArcadeGameSpeed + 1;
                    if (speed >= PlayerManager.GameSpeeds.Count)
                    {
                        AudioManager.inst.PlaySound("Block");
                        return;
                    }

                    AudioManager.inst.PlaySound("blip");
                    PlayerManager.SetGameSpeed(speed);
                    AudioManager.inst.SetPitch(CoreHelper.Pitch);
                    speedText.text = $"<align=center>{CoreHelper.Pitch.ToString("0.0")}x SPEED";
                    speedText.textUI.maxVisibleCharacters = speedText.text.Length;
                    speedText.textUI.text = speedText.text;
                },
            });

            elements.Add(new MenuText
            {
                id = "0",
                name = "Challenge Label",
                text = $"<size=40>CHALLENGE MODE:",
                rect = RectValues.Default.AnchoredPosition(0f, -360f).SizeDelta(64f, 64f),
                hideBG = true,
                color = 6,
                textColor = 6,
                length = 0.5f,
            });

            var challengeText = new MenuText
            {
                id = "0",
                name = "Challenge Text",
                text = $"<align=center>{PlayerManager.ChallengeModeNames[(int)PlayerManager.ChallengeMode]}",
                rect = RectValues.Default.AnchoredPosition(510f, -360f).SizeDelta(64f, 64f),
                hideBG = true,
                color = 6,
                textColor = 6,
                length = 0.5f,
            };

            elements.Add(new MenuButton
            {
                id = "0",
                name = "Decrease Challenge",
                text = "<size=40><b><align=center><",
                rect = RectValues.Default.AnchoredPosition(350f, -360f).SizeDelta(64f, 64f),
                selectionPosition = new Vector2Int(2, 5),
                opacity = 0.1f,
                selectedOpacity = 1f,
                color = 6,
                selectedColor = 6,
                textColor = 6,
                selectedTextColor = 7,
                length = 0.5f,
                playBlipSound = false,
                func = () =>
                {
                    var challenge = (int)PlayerManager.ChallengeMode - 1;
                    if (challenge < 0)
                    {
                        AudioManager.inst.PlaySound("Block");
                        return;
                    }

                    AudioManager.inst.PlaySound("blip");
                    PlayerManager.SetChallengeMode(challenge);
                    challengeText.text = $"<align=center>{PlayerManager.ChallengeModeNames[(int)PlayerManager.ChallengeMode]}";
                    challengeText.textUI.maxVisibleCharacters = challengeText.text.Length;
                    challengeText.textUI.text = challengeText.text;
                },
            });

            elements.Add(challengeText);

            elements.Add(new MenuButton
            {
                id = "0",
                name = "Increase Challenge",
                text = "<size=40><b><align=center>>",
                rect = RectValues.Default.AnchoredPosition(670f, -360f).SizeDelta(64f, 64f),
                selectionPosition = new Vector2Int(3, 5),
                opacity = 0.1f,
                selectedOpacity = 1f,
                color = 6,
                selectedColor = 6,
                textColor = 6,
                selectedTextColor = 7,
                length = 0.5f,
                playBlipSound = false,
                func = () =>
                {
                    var challenge = (int)PlayerManager.ChallengeMode + 1;
                    if (challenge >= 5)
                    {
                        AudioManager.inst.PlaySound("Block");
                        return;
                    }

                    AudioManager.inst.PlaySound("blip");
                    PlayerManager.SetChallengeMode(challenge);
                    challengeText.text = $"<align=center>{PlayerManager.ChallengeModeNames[(int)PlayerManager.ChallengeMode]}";
                    challengeText.textUI.maxVisibleCharacters = challengeText.text.Length;
                    challengeText.textUI.text = challengeText.text;
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

        public static void Init(Level level)
        {
            InterfaceManager.inst.CloseMenus();
            CurrentLevel = level;
            Current = new PlayLevelMenu();
        }

        public static void Close()
        {
            if (MenuManager.inst)
                AudioManager.inst.PlayMusic(MenuManager.inst.currentMenuMusicName, MenuManager.inst.currentMenuMusic);
            InterfaceManager.inst.CloseMenus();

            if (close == null)
                ArcadeMenu.Init();
            else
            {
                close();
                close = null;
            }
        }

        public static Action close;

        public override void Clear()
        {
            CurrentLevel = null;
            base.Clear();
        }
    }
}
