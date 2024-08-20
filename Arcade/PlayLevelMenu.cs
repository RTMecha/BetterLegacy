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

namespace BetterLegacy.Arcade
{
    public class PlayLevelMenu : MenuBase
    {
        public static PlayLevelMenu Current { get; set; }
        public static Level CurrentLevel { get; set; }

        public PlayLevelMenu() : base(false)
        {
            InterfaceManager.inst.CurrentMenu = this;

            elements.Add(new MenuImage
            {
                id = "35255236785",
                name = "Background",
                siblingIndex = 0,
                rectJSON = MenuImage.GenerateRectTransformJSON(Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero),
                color = 17,
                opacity = 1f,
                length = 0f,
            });

            elements.Add(new MenuButton
            {
                id = "626274",
                name = "Close Button",
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(-560f, 440f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(250f, 64f)),
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
                func = () =>
                {
                    if (MenuManager.inst)
                        AudioManager.inst.PlayMusic(MenuManager.inst.currentMenuMusicName, MenuManager.inst.currentMenuMusic);

                    InterfaceManager.inst.CloseMenus();
                },
            });

            //elements.Add(new MenuButton
            //{
            //    id = "4857529985",
            //    name = "Show Queue",
            //    rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(150f, 440f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400f, 64f)),
            //    selectionPosition = new Vector2Int(1, 0),
            //    text = $"<b><align=center><size=40>[ SHOW QUEUE: {LevelManager.ArcadeQueue.Count} ]",
            //    opacity = 0.1f,
            //    selectedOpacity = 1f,
            //    color = 6,
            //    selectedColor = 6,
            //    textColor = 6,
            //    selectedTextColor = 7,
            //    length = 0.5f,
            //    playBlipSound = true,
            //    func = () => { CoreHelper.Log($"Test"); },
            //});
            
            elements.Add(new MenuButton
            {
                id = "4857529985",
                name = "Copy ID",
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(550f, 440f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(250f, 64f)),
                selectionPosition = new Vector2Int(2, 0),
                text = $"<b><align=center><size=40>[ COPY ID ]",
                opacity = 0.1f,
                selectedOpacity = 1f,
                color = 6,
                selectedColor = 6,
                textColor = 6,
                selectedTextColor = 7,
                length = 0.5f,
                playBlipSound = true,
                func = () => { LSText.CopyToClipboard(CurrentLevel.id); },
            });

            elements.Add(new MenuImage
            {
                id = "5356325",
                name = "Backer",
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(250f, 100f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(844f, 512f)),
                opacity = 0.1f,
                color = 6,
                length = 0.1f,
            });

            elements.Add(new MenuImage
            {
                id = "84682758635",
                name = "Cover",
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(-430f, 100f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(512f, 512f)),
                icon = CurrentLevel.icon,
                opacity = 1f,
                val = 40f,
                length = 0.1f,
            });

            var name = CoreHelper.ReplaceFormatting(CurrentLevel.metadata.LevelBeatmap.name);
            int size = 110;
            if (name.Length > 13)
                size = (int)(size * ((float)13f / name.Length));

            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Title",
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(-80f, 280f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 100f)),
                text = $"<size={size}><b>{name}",
                hideBG = true,
                textColor = 6,
            });

            //elements.Add(new MenuText
            //{
            //    id = "4624859539",
            //    name = "ID",
            //    rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(0f, 200f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 100f)),
            //    text = "<size=30>ID: " + CurrentLevel.id + " (Click to copy)",
            //    hideBG = true,
            //    textColor = 6,
            //    func = () => { LSText.CopyToClipboard(CurrentLevel.id); },
            //});

            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Song",
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(-100f, 200f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 100f)),
                text = $"<size=40>Song:",
                hideBG = true,
                textColor = 6,
            });

            elements.Add(new MenuButton
            {
                id = "638553",
                name = "Song Button",
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(340f, 200f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(500f, 48f)),
                selectionPosition = new Vector2Int(0, 1),
                text = $" [ {CurrentLevel.metadata.LevelSong.title} ]",
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
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(-100f, 150f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 100f)),
                text = $"<size=40>Artist:",
                hideBG = true,
                textColor = 6,
            });

            elements.Add(new MenuButton
            {
                id = "638553",
                name = "Artist Button",
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(340f, 150f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(500f, 48f)),
                selectionPosition = new Vector2Int(0, 2),
                text = $" [ {CurrentLevel.metadata.LevelArtist.Name} ]",
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
                    if (CurrentLevel.metadata != null && !string.IsNullOrEmpty(CurrentLevel.metadata.LevelArtist.URL))
                        Application.OpenURL(CurrentLevel.metadata.LevelArtist.URL);
                },
            });

            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Creator",
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(-100f, 100f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 100f)),
                text = $"<size=40>Creator:",
                hideBG = true,
                textColor = 6,
            });

            elements.Add(new MenuButton
            {
                id = "638553",
                name = "Creator Button",
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(340f, 100f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(500f, 48f)),
                selectionPosition = new Vector2Int(0, 3),
                text = $" [ {CurrentLevel.metadata.LevelCreator.steam_name} ]",
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
                    if (CurrentLevel.metadata != null && !string.IsNullOrEmpty(CurrentLevel.metadata.LevelCreator.URL))
                        Application.OpenURL(CurrentLevel.metadata.LevelCreator.URL);
                },
            });

            var difficulty = CoreHelper.GetDifficulty(CurrentLevel.metadata.LevelSong.difficulty);
            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Difficulty",
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(-100f, 50f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 100f)),
                text = $"<size=40>Difficulty: <b><#{LSColors.ColorToHex(difficulty.color)}><voffset=-13><size=64>■</voffset><size=40>{difficulty.name}",
                hideBG = true,
                textColor = 6,
            });
            
            //elements.Add(new MenuText
            //{
            //    id = "4624859539",
            //    name = "Difficulty",
            //    rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(320f, 50f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(500f, 48f)),
            //    text = $"<size=40><align=center><b><#{LSColors.ColorToHex(difficulty.color)}><voffset=-13><size=64>■</voffset><size=40>{difficulty.name}",
            //    hideBG = true,
            //    textColor = 6,
            //});

            elements.Add(new MenuText
            {
                id = "4624859539",
                name = "Description Label",
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(250f, -20f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(800f, 100f)),
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
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(250f, -60f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(800f, 100f)),
                text = "<size=22>" + CurrentLevel.metadata.LevelSong.description,
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
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(-250f, -90f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 100f), -10f),
                text = $"<size=140><b><align=center><#{LSColors.ColorToHex(levelRank.color)}>{levelRank.name}",
                hideBG = true,
                textColor = 6,
            });

            elements.Add(new MenuButton
            {
                id = "3525734",
                name = "Play Button",
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(-430f, -220f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(512f, 64f)),
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
                    LevelManager.currentQueueIndex = 0;
                    if (LevelManager.ArcadeQueue.Count > 1)
                    {
                        LevelManager.CurrentLevel = LevelManager.ArcadeQueue[0];
                        CurrentLevel = LevelManager.CurrentLevel;
                    }

                    ArcadeMenuManager.inst.menuUI.SetActive(false);
                    InterfaceManager.inst.CloseMenus();
                    LevelManager.OnLevelEnd = ArcadeHelper.EndOfLevel;
                    CoreHelper.StartCoroutine(LevelManager.Play(CurrentLevel));
                },
            });

            var queueButton = new MenuButton
            {
                id = "3525734",
                name = "Queue Button",
                text = $"<size=40><b><align=center>[ {(LevelManager.ArcadeQueue.Has(x => x.id == CurrentLevel.id) ? "REMOVE FROM" : "ADD TO")} QUEUE ]",
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(-430f, -320f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(512f, 64f)),
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

            var ldmSetting = new MenuButton
            {
                id = "0",
                name = "LDM Setting",
                text = $"<size=40><b><align=center>[ LOW DETAIL: {(CoreConfig.Instance.LDM.Value ? "ON" : "OFF")} ]",
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(60f, -220f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400f, 64f)),
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
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(490f, -220f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(64f, 64f)),
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
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(340f, -220f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(64f, 64f)),
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
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(640f, -220f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(64f, 64f)),
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
                name = "Challenge Text",
                text = $"<size=40>CHALLENGE MODE:",
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(0f, -320f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(64f, 64f)),
                hideBG = true,
                color = 6,
                textColor = 6,
                length = 0.5f,
            });

            var challengeText = new MenuText
            {
                id = "0",
                name = "Challenge Text",
                text = $"<align=center>{PlayerManager.ChallengeModeNames[(int)PlayerManager.DifficultyMode]}",
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(490f, -320f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(64f, 64f)),
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
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(340f, -320f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(64f, 64f)),
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
                    var challenge = (int)PlayerManager.DifficultyMode - 1;
                    if (challenge < 0)
                    {
                        AudioManager.inst.PlaySound("Block");
                        return;
                    }

                    AudioManager.inst.PlaySound("blip");
                    PlayerManager.SetGameMode(challenge);
                    challengeText.text = $"<align=center>{PlayerManager.ChallengeModeNames[(int)PlayerManager.DifficultyMode]}";
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
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(640f, -320f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(64f, 64f)),
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
                    var challenge = (int)PlayerManager.DifficultyMode + 1;
                    if (challenge >= 5)
                    {
                        AudioManager.inst.PlaySound("Block");
                        return;
                    }

                    AudioManager.inst.PlaySound("blip");
                    PlayerManager.SetGameMode(challenge);
                    challengeText.text = $"<align=center>{PlayerManager.ChallengeModeNames[(int)PlayerManager.DifficultyMode]}";
                    challengeText.textUI.maxVisibleCharacters = challengeText.text.Length;
                    challengeText.textUI.text = challengeText.text;
                },
            });

            elements.Add(new MenuText
            {
                id = "0",
                name = "Credits",
                text = "<alpha=#45>UI design by catix",
                rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(640f, -480f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 100f)),
                hideBG = true,
                textColor = 6,
                length = 0.1f,
            });

            //int num = 2;
            //if (CurrentLevel.metadata != null && !string.IsNullOrEmpty(CurrentLevel.metadata.LevelArtist.URL))
            //{
            //    elements.Add(new MenuButton
            //    {
            //        id = "3525734",
            //        name = "Artist Button",
            //        parentLayout = "buttons",
            //        rectJSON = MenuImage.GenerateRectTransformJSON(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 100f)),
            //        selectionPosition = new Vector2Int(num, 1),
            //        text = "<b><align=center>[ VISIT ARTIST ]",
            //        opacity = 0.1f,
            //        selectedOpacity = 1f,
            //        color = 6,
            //        selectedColor = 6,
            //        textColor = 6,
            //        selectedTextColor = 7,
            //        length = 0.5f,
            //        playBlipSound = true,
            //        func = () => { Application.OpenURL(CurrentLevel.metadata.LevelArtist.URL); },
            //    });
            //    num++;
            //}

            //if (CurrentLevel.metadata != null && !string.IsNullOrEmpty(CurrentLevel.metadata.SongURL))
            //{
            //    elements.Add(new MenuButton
            //    {
            //        id = "3525734",
            //        name = "Artist Button",
            //        parentLayout = "buttons",
            //        rectJSON = MenuImage.GenerateRectTransformJSON(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 100f)),
            //        selectionPosition = new Vector2Int(num, 1),
            //        text = "<b><align=center>[ GET SONG ]",
            //        opacity = 0.1f,
            //        selectedOpacity = 1f,
            //        color = 6,
            //        selectedColor = 6,
            //        textColor = 6,
            //        selectedTextColor = 7,
            //        length = 0.5f,
            //        playBlipSound = true,
            //        func = () => { Application.OpenURL(CurrentLevel.metadata.SongURL); },
            //    });
            //    num++;
            //}

            //if (CurrentLevel.metadata != null && !string.IsNullOrEmpty(CurrentLevel.metadata.LevelCreator.URL))
            //{
            //    elements.Add(new MenuButton
            //    {
            //        id = "3525734",
            //        name = "Artist Button",
            //        parentLayout = "buttons",
            //        rectJSON = MenuImage.GenerateRectTransformJSON(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 100f)),
            //        selectionPosition = new Vector2Int(num, 1),
            //        text = "<b><align=center>[ VISIT CREATOR ]",
            //        opacity = 0.1f,
            //        selectedOpacity = 1f,
            //        color = 6,
            //        selectedColor = 6,
            //        textColor = 6,
            //        selectedTextColor = 7,
            //        length = 0.5f,
            //        playBlipSound = true,
            //        func = () => { Application.OpenURL(CurrentLevel.metadata.LevelCreator.URL); },
            //    });
            //    num++;
            //}

            //elements.Add(new MenuText
            //{
            //    id = "0",
            //    name = "Label",
            //    parentLayout = "settings",
            //    text = $"<b>Settings",
            //    rectJSON = MenuImage.GenerateRectTransformJSON(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(160f, 64f)),
            //    hideBG = true,
            //    color = 6,
            //    textColor = 6,
            //    length = 0.3f,
            //});

            //elements.Add(new MenuImage
            //{
            //    id = "0",
            //    name = "Spacer",
            //    parentLayout = "settings",
            //    rectJSON = MenuImage.GenerateRectTransformJSON(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(8f, 64f)),
            //    opacity = 1f,
            //    color = 6,
            //    length = 0.1f,
            //});

            //var ldmSetting = new MenuButton
            //{
            //    id = "0",
            //    name = "LDM Setting",
            //    parentLayout = "settings",
            //    text = $"<b><align=center><size=22>LDM = {(CoreConfig.Instance.LDM.Value ? "ON" : "OFF")}",
            //    rectJSON = MenuImage.GenerateRectTransformJSON(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(200f, 64f)),
            //    selectionPosition = new Vector2Int(0, 2),
            //    opacity = 0.1f,
            //    selectedOpacity = 1f,
            //    color = 6,
            //    selectedColor = 6,
            //    textColor = 6,
            //    selectedTextColor = 7,
            //    length = 0.5f,
            //    playBlipSound = true,
            //};
            //ldmSetting.func = () =>
            //{
            //    CoreConfig.Instance.LDM.Value = !CoreConfig.Instance.LDM.Value;
            //    ldmSetting.text = $"<b><align=center><size=22>LDM = {(CoreConfig.Instance.LDM.Value ? "ON" : "OFF")}";
            //    ldmSetting.textUI.maxVisibleCharacters = ldmSetting.text.Length;
            //    ldmSetting.textUI.text = ldmSetting.text;
            //};
            //elements.Add(ldmSetting);

            //var difficultyModes = Enum.GetNames(typeof(DifficultyMode));
            //for (int i = 0; i < difficultyModes.Length; i++)
            //{
            //    var selected = PlayerManager.DifficultyMode == (DifficultyMode)i;
            //    int index = i;
            //    var mode = difficultyModes[i];
            //    var element = new MenuButton
            //    {
            //        id = "0",
            //        name = mode,
            //        parentLayout = "settings",
            //        text = $"<b><align=center><size=22>{(selected ? ">" : "")} {mode} {(selected ? "<" : "")}",
            //        rectJSON = MenuImage.GenerateRectTransformJSON(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(150f, 64f)),
            //        selectionPosition = new Vector2Int(i + 1, 2),
            //        opacity = 0.1f,
            //        selectedOpacity = 1f,
            //        color = 6,
            //        selectedColor = 6,
            //        textColor = 6,
            //        selectedTextColor = 7,
            //        length = 0.5f,
            //        playBlipSound = true,
            //    };
            //    element.func = () =>
            //    {
            //        PlayerManager.SetGameMode(index);
            //        for (int i = 0; i < difficultyModeElements.Count; i++)
            //        {
            //            var selected = PlayerManager.DifficultyMode == (DifficultyMode)i;
            //            var element = difficultyModeElements[i];
            //            element.text = $"<b><align=center><size=22>{(selected ? ">" : "")} {element.name} {(selected ? "<" : "")}";
            //            element.textUI.maxVisibleCharacters = element.text.Length;
            //            element.textUI.text = element.text;
            //        }

            //    };
            //    difficultyModeElements.Add(element);
            //    elements.Add(element);
            //}

            //elements.Add(new MenuText
            //{
            //    id = "0",
            //    name = "Label",
            //    parentLayout = "speeds",
            //    text = $"<b>Speeds",
            //    rectJSON = MenuImage.GenerateRectTransformJSON(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(160f, 64f)),
            //    hideBG = true,
            //    color = 6,
            //    textColor = 6,
            //    length = 0.3f,
            //});

            //elements.Add(new MenuImage
            //{
            //    id = "0",
            //    name = "Spacer",
            //    parentLayout = "speeds",
            //    rectJSON = MenuImage.GenerateRectTransformJSON(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(8f, 64f)),
            //    opacity = 1f,
            //    color = 6,
            //    length = 0.1f,
            //});

            //var gameSpeeds = PlayerManager.GameSpeeds;
            //for (int i = 0; i < gameSpeeds.Count; i++)
            //{
            //    var selected = PlayerManager.ArcadeGameSpeed == i;
            //    int index = i;
            //    var speed = gameSpeeds[i].ToString();
            //    var element = new MenuButton
            //    {
            //        id = "0",
            //        name = speed,
            //        parentLayout = "speeds",
            //        text = $"<b><align=center><size=22>{(selected ? ">" : "")} {speed}x {(selected ? "<" : "")}",
            //        rectJSON = MenuImage.GenerateRectTransformJSON(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(150f, 64f)),
            //        selectionPosition = new Vector2Int(i, 3),
            //        opacity = 0.1f,
            //        selectedOpacity = 1f,
            //        color = 6,
            //        selectedColor = 6,
            //        textColor = 6,
            //        selectedTextColor = 7,
            //        length = 0.5f,
            //        playBlipSound = true,
            //    };
            //    element.func = () =>
            //    {
            //        PlayerManager.SetGameSpeed(index);
            //        AudioManager.inst.SetPitch(CoreHelper.Pitch);
            //        for (int i = 0; i < gameSpeedElements.Count; i++)
            //        {
            //            var selected = PlayerManager.ArcadeGameSpeed == i;
            //            var element = gameSpeedElements[i];
            //            element.text = $"<b><align=center><size=22>{(selected ? ">" : "")} {element.name}x {(selected ? "<" : "")}";
            //            element.textUI.maxVisibleCharacters = element.text.Length;
            //            element.textUI.text = element.text;
            //        }

            //    };
            //    gameSpeedElements.Add(element);
            //    elements.Add(element);
            //}

            allowEffects = false;
            layer = 10000;
            CoreHelper.StartCoroutine(GenerateUI());
        }

        public List<MenuButton> difficultyModeElements = new List<MenuButton>();
        public List<MenuButton> gameSpeedElements = new List<MenuButton>();

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
    }
}
