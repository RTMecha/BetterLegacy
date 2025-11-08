using System;

using UnityEngine;

using LSFunctions;

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
    public class PlayLevelMenu : MenuBase
    {
        public static PlayLevelMenu Current { get; set; }
        public static Level CurrentLevel { get; set; }

        public PlayLevelMenu() : base()
        {
            this.name = CurrentLevel?.metadata?.beatmap?.name;

            ArcadeHelper.ResetModifiedStates();

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
                    func = () => LSText.CopyToClipboard(CurrentLevel.metadata?.serverID),
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
                func = () => LSText.CopyToClipboard(CurrentLevel.metadata?.arcadeID),
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
                id = "526526526",
                name = "Type",
                rect = RectValues.Default.AnchoredPosition(650f, 350f).SizeDelta(64f, 64f),
                useOverrideColor = true,
                overrideColor = Color.white,
                opacity = 1f,
                length = 0f,
                icon = CurrentLevel.IsVG ? LegacyPlugin.PAVGLogoSprite : LegacyPlugin.PALogoSprite,
                wait = false,
            });

            elements.Add(new MenuImage
            {
                id = "84682758635",
                name = "Cover",
                rect = RectValues.Default.AnchoredPosition(-500f, 100f).SizeDelta(600f, 600f),
                icon = CurrentLevel.icon,
                opacity = 1f,
                val = 40f,
                length = 0f,
                wait = false,
            });

            var name = RTString.ReplaceFormatting(CurrentLevel.metadata.beatmap.name);
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

            var title = RTString.ReplaceFormatting(CurrentLevel.metadata.song.title);
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

            var artist = RTString.ReplaceFormatting(CurrentLevel.metadata.artist.name);
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

            var creator = RTString.ReplaceFormatting(CurrentLevel.metadata.creator.name);
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
                        LevelListMenu.Init($"{AlephNetwork.ArcadeServerURL}api/Level/user/{currentLevel.metadata.uploaderID}");
                        LevelListMenu.close = () => Init(currentLevel);
                    }
                },
            });

            var difficulty = CurrentLevel.metadata.song.Difficulty;
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
                text = "<size=22><b>Tags</b>: " + RTString.ListToString(CurrentLevel.metadata.tags),
                hideBG = true,
                textColor = 6,
                enableWordWrapping = true,
                alignment = TMPro.TextAlignmentOptions.TopLeft,
            });

            var rank = LevelManager.GetLevelRank(CurrentLevel);
            elements.Add(new MenuText
            {
                id = "92595",
                name = "Rank",
                rect = RectValues.Default.AnchoredPosition(-250f, -90f).Rotation(-10f),
                text = $"<size=140><b><align=center><#{LSColors.ColorToHex(rank.Color)}>{rank.DisplayName}",
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
                    var collection = LevelManager.CurrentLevelCollection;
                    if (collection)
                    {
                        if (LevelManager.currentLevelIndex < 0)
                            LevelManager.currentLevelIndex = 0;

                        while (LevelManager.currentLevelIndex < collection.Count - 1 && collection.levelInformation[LevelManager.currentLevelIndex].skip) // skip the level during normal playthrough
                            LevelManager.currentLevelIndex++;

                        if (collection.Count > 1)
                            LevelManager.CurrentLevel = collection[LevelManager.currentLevelIndex];
                        else
                            LevelManager.CurrentLevel = CurrentLevel;
                    }
                    else if (!LevelManager.ArcadeQueue.IsEmpty())
                    {
                        LevelManager.currentQueueIndex = 0;
                        LevelManager.CurrentLevel = LevelManager.ArcadeQueue[0];
                    }
                    else
                        LevelManager.CurrentLevel = CurrentLevel;

                    if (!LevelManager.CurrentLevel)
                        return;

                    var metadata = LevelManager.CurrentLevel.metadata;

                    if (!metadata)
                        return;

                    if (metadata.IsIncompatible())
                    {
                        SoundManager.inst.PlaySound(DefaultSounds.Block);
                        CoreHelper.Notify(metadata.GetIncompatibleMessage(), InterfaceManager.inst.CurrentTheme.guiColor);
                        return;
                    }

                    if (metadata.beatmap && !metadata.beatmap.PlayersCanjoin(PlayerManager.Players.Count))
                    {
                        var count = metadata.beatmap.preferredPlayerCount;
                        SoundManager.inst.PlaySound(DefaultSounds.Block);
                        CoreHelper.Notify($"Cannot play this level as it only works with [ {count} ] player{(count == BeatmapMetaData.PreferredPlayerCount.One ? string.Empty : "s")}.", InterfaceManager.inst.CurrentTheme.guiColor);
                        return;
                    }

                    InterfaceManager.inst.CloseMenus();

                    LevelManager.Play(LevelManager.CurrentLevel, RTBeatmap.Current.EndOfLevel);
                },
            });

            if (!LevelManager.CurrentLevelCollection)
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
                text = $"<align=center>{CoreConfig.Instance.GameSpeedSetting.Value.DisplayName} SPEED",
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
                    var speed = CoreConfig.Instance.GameSpeedSetting.Value - 1;
                    if (speed < 0)
                    {
                        SoundManager.inst.PlaySound(DefaultSounds.Block);
                        return;
                    }

                    SoundManager.inst.PlaySound(DefaultSounds.blip);
                    CoreConfig.Instance.GameSpeedSetting.Value = speed;
                    AudioManager.inst.SetPitch(CoreConfig.Instance.GameSpeedSetting.Value.Pitch);
                    speedText.text = $"<align=center>{CoreConfig.Instance.GameSpeedSetting.Value.DisplayName} SPEED";
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
                    var speed = CoreConfig.Instance.GameSpeedSetting.Value + 1;
                    if (speed >= CoreConfig.Instance.GameSpeedSetting.Value.GetBoxedValues().Length)
                    {
                        SoundManager.inst.PlaySound(DefaultSounds.Block);
                        return;
                    }

                    SoundManager.inst.PlaySound(DefaultSounds.blip);
                    CoreConfig.Instance.GameSpeedSetting.Value = speed;
                    AudioManager.inst.SetPitch(CoreConfig.Instance.GameSpeedSetting.Value.Pitch);
                    speedText.text = $"<align=center>{CoreConfig.Instance.GameSpeedSetting.Value.DisplayName} SPEED";
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
                text = $"<align=center>{CoreConfig.Instance.ChallengeModeSetting.Value.DisplayName}",
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
                    var challenge = CoreConfig.Instance.ChallengeModeSetting.Value - 1;
                    if (challenge < 0)
                    {
                        SoundManager.inst.PlaySound(DefaultSounds.Block);
                        return;
                    }

                    SoundManager.inst.PlaySound(DefaultSounds.blip);
                    CoreConfig.Instance.ChallengeModeSetting.Value = challenge;
                    challengeText.text = $"<align=center>{CoreConfig.Instance.ChallengeModeSetting.Value.DisplayName}";
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
                    var challenge = CoreConfig.Instance.ChallengeModeSetting.Value + 1;
                    if (challenge >= CoreConfig.Instance.ChallengeModeSetting.Value.GetBoxedValues().Length)
                    {
                        SoundManager.inst.PlaySound(DefaultSounds.Block);
                        return;
                    }

                    SoundManager.inst.PlaySound(DefaultSounds.blip);
                    CoreConfig.Instance.ChallengeModeSetting.Value = challenge;
                    challengeText.text = $"<align=center>{CoreConfig.Instance.ChallengeModeSetting.Value.DisplayName}";
                    challengeText.textUI.maxVisibleCharacters = challengeText.text.Length;
                    challengeText.textUI.text = challengeText.text;
                },
            });

            if (!CurrentLevel.achievements.IsEmpty())
            {
                elements.Add(new MenuButton
                {
                    id = "3525734",
                    name = "Achievements Button",
                    text = $"<size=40><b><align=center>[ VIEW ACHIEVEMENTS ]",
                    rect = RectValues.Default.AnchoredPosition(-500f, !LevelManager.CurrentLevelCollection ? -460f : 360f).SizeDelta(600f, 64f),
                    selectionPosition = new Vector2Int(0, !LevelManager.CurrentLevelCollection ? 6 : 5),
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
                        var currentLevel = CurrentLevel;
                        AchievementListMenu.Init(CurrentLevel, 0, () => Init(currentLevel));
                    },
                });
            }

            exitFunc = Close;

            layer = 10000;
            defaultSelection = new Vector2Int(0, 4);

            InterfaceManager.inst.SetCurrentInterface(this);
        }

        public static void Init(Level level)
        {
            if (!level.music)
                CoroutineHelper.StartCoroutine(level.LoadAudioClipRoutine(() => InternalInit(level)));
            else
                InternalInit(level);
        }

        static void InternalInit(Level level)
        {
            var playMusic = AudioManager.inst.CurrentAudioSource.clip != level.music;
            if (playMusic)
                AudioManager.inst.StopMusic();
            CurrentLevel = level;
            Current = new PlayLevelMenu();
            if (playMusic)
                AudioManager.inst.PlayMusic(level.metadata.song.title, level.music);
            AudioManager.inst.SetPitch(CoreConfig.Instance.GameSpeedSetting.Value.Pitch);
        }

        public static void Close()
        {
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
