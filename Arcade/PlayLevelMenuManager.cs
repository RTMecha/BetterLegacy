using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using LSFunctions;

using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.EventSystems;
using BetterLegacy.Core;
using BetterLegacy.Core.Managers;
using BetterLegacy.Configs;
using BetterLegacy.Components;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Menus;

namespace BetterLegacy.Arcade
{
    public class PlayLevelMenuManager : MonoBehaviour
    {
        public static PlayLevelMenuManager inst;

        public RectTransform rectTransform;
        public Image background;

        public bool inSettings;

        public Level CurrentLevel { get; set; }

        void Awake()
        {
            inst = this;
        }

        void Update()
        {
            if (!inSettings)
            {
                for (int i = 0; i < Buttons.Count; i++)
                {
                    for (int j = 0; j < Buttons[i].Count; j++)
                    {
                        var isSelected = j == selected.x && i == selected.y;
                        Buttons[i][j].Text.color = isSelected ? ArcadeMenuManager.inst.textHighlightColor : ArcadeMenuManager.inst.textColor;
                        Buttons[i][j].Image.color = isSelected ? ArcadeMenuManager.inst.highlightColor : Color.Lerp(ArcadeMenuManager.inst.buttonBGColor, Color.white, 0.01f);
                    }
                }
            }
            else
            {
                for (int i = 0; i < Settings.Count; i++)
                {
                    for (int j = 0; j < Settings[i].Count; j++)
                    {
                        var isSelected = j == selected.x && i == selected.y;
                        Settings[i][j].Text.color = isSelected ? ArcadeMenuManager.inst.textHighlightColor : ArcadeMenuManager.inst.textColor;
                        Settings[i][j].Image.color = isSelected ? ArcadeMenuManager.inst.highlightColor : Color.Lerp(ArcadeMenuManager.inst.buttonBGColor, Color.white, 0.01f);
                    }
                }
            }

            title.color = ArcadeMenuManager.inst.textColor;
            artist.color = ArcadeMenuManager.inst.textColor;
            creator.color = ArcadeMenuManager.inst.textColor;
            difficulty.color = ArcadeMenuManager.inst.textColor;
            description.color = ArcadeMenuManager.inst.textColor;
            settingsImage2.color = Color.Lerp(ArcadeMenuManager.inst.buttonBGColor, Color.white, 0.03f);

            var currentTheme = DataManager.inst.interfaceSettings["UITheme"][SaveManager.inst.settings.Video.UITheme];

            background.color = LSColors.HexToColor(currentTheme["values"]["bg"]);
            settingsImage1.color = background.color;

            if (!ArcadeMenuManager.inst || !ArcadeMenuManager.inst.OpenedLocalLevel)
                return;

            UpdateControls();

        }

        public bool animating;

        public Vector2Int selected;

        public List<int> SelectionLimit { get; set; } = new List<int>();
        public List<int> SettingsSelectionLimit { get; set; } = new List<int>();

        public List<List<ArcadeMenuManager.Tab>> Buttons = new List<List<ArcadeMenuManager.Tab>>
        {
            new List<ArcadeMenuManager.Tab>(),
            new List<ArcadeMenuManager.Tab>(),
        };

        public List<List<ArcadeMenuManager.Tab>> Settings = new List<List<ArcadeMenuManager.Tab>>();

        void UpdateControls()
        {
            if (LSHelpers.IsUsingInputField())
                return;

            var actions = InputDataManager.inst.menuActions;

            if (actions.Left.WasPressed)
            {
                if (selected.x > 0)
                {
                    AudioManager.inst.PlaySound("LeftRight");
                    selected.x--;
                }
                else
                    AudioManager.inst.PlaySound("Block");
            }

            if (actions.Right.WasPressed)
            {
                if (selected.x < (inSettings ? SettingsSelectionLimit[selected.y] - 1 : SelectionLimit[selected.y] - 1))
                {
                    AudioManager.inst.PlaySound("LeftRight");
                    selected.x++;
                }
                else
                    AudioManager.inst.PlaySound("Block");
            }

            if (actions.Up.WasPressed)
            {
                if (selected.y > 0)
                {
                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y--;
                    selected.x = Mathf.Clamp(selected.x, 0, inSettings ? SettingsSelectionLimit[selected.y] - 1 : SelectionLimit[selected.y] - 1);
                }
                else
                    AudioManager.inst.PlaySound("Block");
            }

            if (actions.Down.WasPressed)
            {
                if (selected.y < (inSettings ? SettingsSelectionLimit.Count - 1 : SelectionLimit.Count - 1))
                {
                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y++;
                    selected.x = Mathf.Clamp(selected.x, 0, inSettings ? SettingsSelectionLimit[selected.y] - 1 : SelectionLimit[selected.y] - 1);
                }
                else if (!inSettings)
                    AudioManager.inst.PlaySound("Block");
                else
                {
                    inSettings = false;
                    CloseSettings();
                }
            }

            if (actions.Submit.WasPressed)
            {
                (inSettings ? Settings : Buttons)[selected.y][selected.x].Clickable?.onClick?.Invoke(null);
            }
        }
        
        public IEnumerator SetupPlayLevelMenu()
        {
            SelectionLimit.Add(1);

            var close = GenerateButton(0, rectTransform, Buttons);
            close.RectTransform.anchoredPosition = new Vector2(-904f, 485f);
            close.RectTransform.sizeDelta = new Vector2(100f, 100f);
            close.Text.alignment = TextAlignmentOptions.Center;
            close.Text.fontSize = 72;
            close.Text.text = "<scale=1.3><pos=6>X";
            close.Text.color = ArcadeMenuManager.inst.textColor;
            close.Image.color = Color.Lerp(ArcadeMenuManager.inst.buttonBGColor, Color.white, 0.01f);
            close.Clickable.onClick = delegate (PointerEventData pointerEventData)
            {
                if (inSettings)
                    return;

                AudioManager.inst.PlaySound("blip");
                Close();
            };
            close.Clickable.onEnter = delegate (PointerEventData pointerEventData)
            {
                if (inSettings)
                    return;

                AudioManager.inst.PlaySound("LeftRight");
                selected.y = 0;
                selected.x = 0;
            };

            SelectionLimit.Add(0);

            // Play
            {
                var play = GenerateButton(1, rectTransform, Buttons);
                play.RectTransform.anchoredPosition = new Vector2(-700f, -400f);
                play.RectTransform.sizeDelta = new Vector2(300f, 100f);
                play.Text.alignment = TextAlignmentOptions.Center;
                play.Text.fontSize = 32;
                play.Text.text = "[PLAY]";
                play.Text.color = ArcadeMenuManager.inst.textColor;
                play.Image.color = Color.Lerp(ArcadeMenuManager.inst.buttonBGColor, Color.white, 0.01f);
                play.Clickable.onClick = delegate (PointerEventData pointerEventData)
                {
                    if (inSettings)
                        return;

                    AudioManager.inst.PlaySound("blip");
                    CoreHelper.Log($"Play");

                    if (animating || CurrentLevel == null)
                    {
                        Debug.LogError($"NullReferenceException! how the heck did you get here");
                        return;
                    }

                    LevelManager.current = 0;
                    if (LevelManager.ArcadeQueue.Count > 1)
                    {
                        LevelManager.CurrentLevel = LevelManager.ArcadeQueue[0];
                    }

                    ArcadeMenuManager.inst.menuUI.SetActive(false);
                    LevelManager.OnLevelEnd = ArcadeHelper.EndOfLevel;
                    CoreHelper.StartCoroutine(LevelManager.Play(CurrentLevel));
                };
                play.Clickable.onEnter = delegate (PointerEventData pointerEventData)
                {
                    if (inSettings || !Cursor.visible)
                        return;

                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y = 1;
                    selected.x = 0;
                };

                SelectionLimit[1]++;
            }
            
            // Add to Queue
            {
                var play = GenerateButton(1, rectTransform, Buttons);
                play.RectTransform.anchoredPosition = new Vector2(-350f, -400f);
                play.RectTransform.sizeDelta = new Vector2(300f, 100f);
                play.Text.alignment = TextAlignmentOptions.Center;
                play.Text.fontSize = 32;
                play.Text.text = "[ADD TO QUEUE]";
                play.Text.color = ArcadeMenuManager.inst.textColor;
                play.Image.color = Color.Lerp(ArcadeMenuManager.inst.buttonBGColor, Color.white, 0.01f);
                play.Clickable.onClick = delegate (PointerEventData pointerEventData)
                {
                    if (inSettings)
                        return;

                    AudioManager.inst.PlaySound("blip");
                    CoreHelper.Log($"Add to Queue {CurrentLevel.id}");
                    if (LevelManager.ArcadeQueue.Has(x => x.id == CurrentLevel.id))
                        LevelManager.ArcadeQueue.RemoveAll(x => x.id == CurrentLevel.id);
                    else
                        LevelManager.ArcadeQueue.Add(CurrentLevel);

                    play.Text.text = LevelManager.ArcadeQueue.Has(x => x.id == CurrentLevel.id) ? "[REMOVE FROM QUEUE]" : "[ADD TO QUEUE]";
                };
                play.Clickable.onEnter = delegate (PointerEventData pointerEventData)
                {
                    if (inSettings || !Cursor.visible)
                        return;

                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y = 1;
                    selected.x = 1;
                };

                SelectionLimit[1]++;
            }
            
            // Visit Artist
            {
                var play = GenerateButton(1, rectTransform, Buttons);
                play.RectTransform.anchoredPosition = new Vector2(0f, -400f);
                play.RectTransform.sizeDelta = new Vector2(300f, 100f);
                play.Text.alignment = TextAlignmentOptions.Center;
                play.Text.fontSize = 32;
                play.Text.text = "[VISIT ARTIST]";
                play.Text.color = ArcadeMenuManager.inst.textColor;
                play.Image.color = Color.Lerp(ArcadeMenuManager.inst.buttonBGColor, Color.white, 0.01f);
                play.Clickable.onClick = delegate (PointerEventData pointerEventData)
                {
                    if (inSettings)
                        return;

                    AudioManager.inst.PlaySound("blip");
                    if (CurrentLevel.metadata.LevelArtist.URL != null)
                        Application.OpenURL(CurrentLevel.metadata.LevelArtist.URL);
                };
                play.Clickable.onEnter = delegate (PointerEventData pointerEventData)
                {
                    if (inSettings || !Cursor.visible)
                        return;

                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y = 1;
                    selected.x = 2;
                };

                SelectionLimit[1]++;
            }
            
            // Get Song
            {
                var play = GenerateButton(1, rectTransform, Buttons);
                play.RectTransform.anchoredPosition = new Vector2(350f, -400f);
                play.RectTransform.sizeDelta = new Vector2(300f, 100f);
                play.Text.alignment = TextAlignmentOptions.Center;
                play.Text.fontSize = 32;
                play.Text.text = "[GET SONG]";
                play.Text.color = ArcadeMenuManager.inst.textColor;
                play.Image.color = Color.Lerp(ArcadeMenuManager.inst.buttonBGColor, Color.white, 0.01f);
                play.Clickable.onClick = delegate (PointerEventData pointerEventData)
                {
                    if (inSettings)
                        return;

                    AudioManager.inst.PlaySound("blip");
                    if (CurrentLevel.metadata.LevelArtist.URL != null)
                        Application.OpenURL(CurrentLevel.metadata.LevelArtist.URL);
                };
                play.Clickable.onEnter = delegate (PointerEventData pointerEventData)
                {
                    if (inSettings || !Cursor.visible)
                        return;

                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y = 1;
                    selected.x = 3;
                };

                SelectionLimit[1]++;
            }
            
            // Settings
            {
                var play = GenerateButton(1, rectTransform, Buttons);
                play.RectTransform.anchoredPosition = new Vector2(700f, -400f);
                play.RectTransform.sizeDelta = new Vector2(300f, 100f);
                play.Text.alignment = TextAlignmentOptions.Center;
                play.Text.fontSize = 32;
                play.Text.text = "[SETTINGS]";
                play.Text.color = ArcadeMenuManager.inst.textColor;
                play.Image.color = Color.Lerp(ArcadeMenuManager.inst.buttonBGColor, Color.white, 0.01f);
                play.Clickable.onClick = delegate (PointerEventData pointerEventData)
                {
                    AudioManager.inst.PlaySound("blip");

                    if (inSettings)
                    {
                        inSettings = false;
                        CloseSettings();
                        return;
                    }

                    inSettings = true;
                    OpenSettings();
                };
                play.Clickable.onEnter = delegate (PointerEventData pointerEventData)
                {
                    if (inSettings || !Cursor.visible)
                        return;

                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y = 1;
                    selected.x = 4;
                };

                SelectionLimit[1]++;
            }

            var coverBase1 = UIManager.GenerateUIImage($"Cover Base", rectTransform);
            UIManager.SetRectTransform(coverBase1.GetObject<RectTransform>(), new Vector2(-600f, 64f), ArcadeMenuManager.ZeroFive, ArcadeMenuManager.ZeroFive, ArcadeMenuManager.ZeroFive, new Vector2(512f, 512f));
            coverBase = coverBase1.GetObject<Image>();
            coverBase1.GetObject<GameObject>().AddComponent<Mask>();
            
            var coverBase2 = UIManager.GenerateUIImage($"Cover", coverBase1.GetObject<RectTransform>());
            UIManager.SetRectTransform(coverBase2.GetObject<RectTransform>(), Vector2.zero, ArcadeMenuManager.ZeroFive, ArcadeMenuManager.ZeroFive, ArcadeMenuManager.ZeroFive, new Vector2(512f, 512f));
            cover = coverBase2.GetObject<Image>();

            var titleBase = UIManager.GenerateUITextMeshPro("Title", rectTransform);
            UIManager.SetRectTransform(titleBase.GetObject<RectTransform>(), new Vector2(200f, 256f), ArcadeMenuManager.ZeroFive, ArcadeMenuManager.ZeroFive, ArcadeMenuManager.ZeroFive, new Vector2(1000f, 100f));
            title = titleBase.GetObject<TextMeshProUGUI>();
            title.fontSize = 48;
            title.fontStyle = FontStyles.Bold;
            title.overflowMode = TextOverflowModes.Truncate;
            
            var artistBase = UIManager.GenerateUITextMeshPro("Artist", rectTransform);
            UIManager.SetRectTransform(artistBase.GetObject<RectTransform>(), new Vector2(200f, 200f), ArcadeMenuManager.ZeroFive, ArcadeMenuManager.ZeroFive, ArcadeMenuManager.ZeroFive, new Vector2(1000f, 100f));
            artist = artistBase.GetObject<TextMeshProUGUI>();
            artist.overflowMode = TextOverflowModes.Truncate;
            
            var creatorBase = UIManager.GenerateUITextMeshPro("Creator", rectTransform);
            UIManager.SetRectTransform(creatorBase.GetObject<RectTransform>(), new Vector2(200f, 160f), ArcadeMenuManager.ZeroFive, ArcadeMenuManager.ZeroFive, ArcadeMenuManager.ZeroFive, new Vector2(1000f, 100f));
            creator = creatorBase.GetObject<TextMeshProUGUI>();
            creator.overflowMode = TextOverflowModes.Truncate;
            
            var difficultyBase = UIManager.GenerateUITextMeshPro("Difficulty", rectTransform);
            UIManager.SetRectTransform(difficultyBase.GetObject<RectTransform>(), new Vector2(200f, 120f), ArcadeMenuManager.ZeroFive, ArcadeMenuManager.ZeroFive, ArcadeMenuManager.ZeroFive, new Vector2(1000f, 100f));
            difficulty = difficultyBase.GetObject<TextMeshProUGUI>();
            difficulty.overflowMode = TextOverflowModes.Truncate;
            
            var descriptionBase = UIManager.GenerateUITextMeshPro("Description", rectTransform);
            UIManager.SetRectTransform(descriptionBase.GetObject<RectTransform>(), new Vector2(200f, -50f), ArcadeMenuManager.ZeroFive, ArcadeMenuManager.ZeroFive, ArcadeMenuManager.ZeroFive, new Vector2(1000f, 300f));
            description = descriptionBase.GetObject<TextMeshProUGUI>();
            description.enableWordWrapping = true;
            description.overflowMode = TextOverflowModes.Truncate;

            var settingsBase1 = UIManager.GenerateUIImage("Settings Base", rectTransform);
            UIManager.SetRectTransform(settingsBase1.GetObject<RectTransform>(), new Vector2(700f, -140f), ArcadeMenuManager.ZeroFive, ArcadeMenuManager.ZeroFive, ArcadeMenuManager.ZeroFive, new Vector2(300f, 420f));

            var mask = settingsBase1.GetObject<GameObject>().AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var settingsBase2 = UIManager.GenerateUIImage("Settings", settingsBase1.GetObject<RectTransform>());
            UIManager.SetRectTransform(settingsBase2.GetObject<RectTransform>(), new Vector2(0f, -410f), ArcadeMenuManager.ZeroFive, ArcadeMenuManager.ZeroFive, ArcadeMenuManager.ZeroFive, new Vector2(300f, 400f));

            settings = settingsBase2.GetObject<RectTransform>();
            settingsImage1 = settingsBase2.GetObject<Image>();

            var settingsBase3 = UIManager.GenerateUIImage("Settings Shade", settingsBase2.GetObject<RectTransform>());
            UIManager.SetRectTransform(settingsBase3.GetObject<RectTransform>(), new Vector2(0f, 0f), ArcadeMenuManager.ZeroFive, ArcadeMenuManager.ZeroFive, ArcadeMenuManager.ZeroFive, new Vector2(300f, 400f));

            settingsImage2 = settingsBase3.GetObject<Image>();

            // Settings
            {
                Settings.Add(new List<ArcadeMenuManager.Tab>());
                SettingsSelectionLimit.Add(0);

                var zen = GenerateButton(0, settings, Settings);
                zen.RectTransform.anchoredPosition = new Vector2(-74f, 170f);
                zen.RectTransform.sizeDelta = new Vector2(140f, 48f);
                zen.Text.alignment = TextAlignmentOptions.Center;
                zen.Text.fontSize = 14;
                zen.Text.text = $"[ZEN MODE = {PlayerManager.IsZenMode}]";
                zen.Text.color = ArcadeMenuManager.inst.textColor;
                zen.Image.color = Color.Lerp(ArcadeMenuManager.inst.buttonBGColor, Color.white, 0.01f);
                zen.Clickable.onEnter = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings || !Cursor.visible)
                        return;

                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y = 0;
                    selected.x = 0;
                };

                SettingsSelectionLimit[0]++;
                
                var speed1 = GenerateButton(0, settings, Settings);
                speed1.RectTransform.anchoredPosition = new Vector2(74f, 170f);
                speed1.RectTransform.sizeDelta = new Vector2(140f, 48f);
                speed1.Text.alignment = TextAlignmentOptions.Center;
                speed1.Text.fontSize = 14;
                speed1.Text.text = $"[0.1x = {PlayerManager.ArcadeGameSpeed == 0}]";
                speed1.Text.color = ArcadeMenuManager.inst.textColor;
                speed1.Image.color = Color.Lerp(ArcadeMenuManager.inst.buttonBGColor, Color.white, 0.01f);
                speed1.Clickable.onEnter = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings || !Cursor.visible)
                        return;

                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y = 0;
                    selected.x = 1;
                };

                SettingsSelectionLimit[0]++;

                Settings.Add(new List<ArcadeMenuManager.Tab>());
                SettingsSelectionLimit.Add(0);

                var normal = GenerateButton(1, settings, Settings);
                normal.RectTransform.anchoredPosition = new Vector2(-74f, 120f);
                normal.RectTransform.sizeDelta = new Vector2(140f, 48f);
                normal.Text.alignment = TextAlignmentOptions.Center;
                normal.Text.fontSize = 14;
                normal.Text.text = $"[NORMAL = {PlayerManager.IsNormal}]";
                normal.Text.color = ArcadeMenuManager.inst.textColor;
                normal.Image.color = Color.Lerp(ArcadeMenuManager.inst.buttonBGColor, Color.white, 0.01f);
                normal.Clickable.onEnter = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings || !Cursor.visible)
                        return;

                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y = 1;
                    selected.x = 0;
                };

                SettingsSelectionLimit[1]++;

                var speed2 = GenerateButton(1, settings, Settings);
                speed2.RectTransform.anchoredPosition = new Vector2(74f, 120f);
                speed2.RectTransform.sizeDelta = new Vector2(140f, 48f);
                speed2.Text.alignment = TextAlignmentOptions.Center;
                speed2.Text.fontSize = 14;
                speed2.Text.text = $"[0.5x = {PlayerManager.ArcadeGameSpeed == 1}]";
                speed2.Text.color = ArcadeMenuManager.inst.textColor;
                speed2.Image.color = Color.Lerp(ArcadeMenuManager.inst.buttonBGColor, Color.white, 0.01f);
                speed2.Clickable.onEnter = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings || !Cursor.visible)
                        return;

                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y = 1;
                    selected.x = 1;
                };

                SettingsSelectionLimit[1]++;

                Settings.Add(new List<ArcadeMenuManager.Tab>());
                SettingsSelectionLimit.Add(0);

                var life = GenerateButton(2, settings, Settings);
                life.RectTransform.anchoredPosition = new Vector2(-74f, 70f);
                life.RectTransform.sizeDelta = new Vector2(140f, 48f);
                life.Text.alignment = TextAlignmentOptions.Center;
                life.Text.fontSize = 14;
                life.Text.text = $"[1 LIFE = {PlayerManager.Is1Life}]";
                life.Text.color = ArcadeMenuManager.inst.textColor;
                life.Image.color = Color.Lerp(ArcadeMenuManager.inst.buttonBGColor, Color.white, 0.01f);
                life.Clickable.onEnter = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings || !Cursor.visible)
                        return;

                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y = 2;
                    selected.x = 0;
                };

                SettingsSelectionLimit[2]++;

                var speed3 = GenerateButton(2, settings, Settings);
                speed3.RectTransform.anchoredPosition = new Vector2(74f, 70f);
                speed3.RectTransform.sizeDelta = new Vector2(140f, 48f);
                speed3.Text.alignment = TextAlignmentOptions.Center;
                speed3.Text.fontSize = 14;
                speed3.Text.text = $"[0.8x = {PlayerManager.ArcadeGameSpeed == 2}]";
                speed3.Text.color = ArcadeMenuManager.inst.textColor;
                speed3.Image.color = Color.Lerp(ArcadeMenuManager.inst.buttonBGColor, Color.white, 0.01f);
                speed3.Clickable.onEnter = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings || !Cursor.visible)
                        return;

                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y = 2;
                    selected.x = 1;
                };

                SettingsSelectionLimit[2]++;

                Settings.Add(new List<ArcadeMenuManager.Tab>());
                SettingsSelectionLimit.Add(0);

                var hit = GenerateButton(3, settings, Settings);
                hit.RectTransform.anchoredPosition = new Vector2(-74f, 20f);
                hit.RectTransform.sizeDelta = new Vector2(140f, 48f);
                hit.Text.alignment = TextAlignmentOptions.Center;
                hit.Text.fontSize = 14;
                hit.Text.text = $"[NO HIT = {PlayerManager.IsNoHit}]";
                hit.Text.color = ArcadeMenuManager.inst.textColor;
                hit.Image.color = Color.Lerp(ArcadeMenuManager.inst.buttonBGColor, Color.white, 0.01f);
                hit.Clickable.onEnter = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings || !Cursor.visible)
                        return;

                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y = 3;
                    selected.x = 0;
                };

                SettingsSelectionLimit[3]++;

                var speed4 = GenerateButton(3, settings, Settings);
                speed4.RectTransform.anchoredPosition = new Vector2(74f, 20f);
                speed4.RectTransform.sizeDelta = new Vector2(140f, 48f);
                speed4.Text.alignment = TextAlignmentOptions.Center;
                speed4.Text.fontSize = 14;
                speed4.Text.text = $"[1.0x = {PlayerManager.ArcadeGameSpeed == 3}]";
                speed4.Text.color = ArcadeMenuManager.inst.textColor;
                speed4.Image.color = Color.Lerp(ArcadeMenuManager.inst.buttonBGColor, Color.white, 0.01f);
                speed4.Clickable.onEnter = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings || !Cursor.visible)
                        return;

                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y = 3;
                    selected.x = 1;
                };

                SettingsSelectionLimit[3]++;

                Settings.Add(new List<ArcadeMenuManager.Tab>());
                SettingsSelectionLimit.Add(0);

                var practice = GenerateButton(4, settings, Settings);
                practice.RectTransform.anchoredPosition = new Vector2(-74f, -30f);
                practice.RectTransform.sizeDelta = new Vector2(140f, 48f);
                practice.Text.alignment = TextAlignmentOptions.Center;
                practice.Text.fontSize = 14;
                practice.Text.text = $"[PRACTICE = {PlayerManager.IsPractice}]";
                practice.Text.color = ArcadeMenuManager.inst.textColor;
                practice.Image.color = Color.Lerp(ArcadeMenuManager.inst.buttonBGColor, Color.white, 0.01f);
                practice.Clickable.onEnter = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings || !Cursor.visible)
                        return;

                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y = 4;
                    selected.x = 0;
                };

                SettingsSelectionLimit[4]++;

                var speed5 = GenerateButton(4, settings, Settings);
                speed5.RectTransform.anchoredPosition = new Vector2(74f, -30f);
                speed5.RectTransform.sizeDelta = new Vector2(140f, 48f);
                speed5.Text.alignment = TextAlignmentOptions.Center;
                speed5.Text.fontSize = 14;
                speed5.Text.text = $"[1.2x = {PlayerManager.ArcadeGameSpeed == 4}]";
                speed5.Text.color = ArcadeMenuManager.inst.textColor;
                speed5.Image.color = Color.Lerp(ArcadeMenuManager.inst.buttonBGColor, Color.white, 0.01f);
                speed5.Clickable.onEnter = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings || !Cursor.visible)
                        return;

                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y = 4;
                    selected.x = 1;
                };

                SettingsSelectionLimit[4]++;

                Settings.Add(new List<ArcadeMenuManager.Tab>());
                SettingsSelectionLimit.Add(0);

                var ldm = GenerateButton(5, settings, Settings);
                ldm.RectTransform.anchoredPosition = new Vector2(-74f, -80f);
                ldm.RectTransform.sizeDelta = new Vector2(140f, 48f);
                ldm.Text.alignment = TextAlignmentOptions.Center;
                ldm.Text.fontSize = 14;
                ldm.Text.text = $"[LDM = {CoreConfig.Instance.LDM.Value}]";
                ldm.Text.color = ArcadeMenuManager.inst.textColor;
                ldm.Image.color = Color.Lerp(ArcadeMenuManager.inst.buttonBGColor, Color.white, 0.01f);
                ldm.Clickable.onEnter = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings || !Cursor.visible)
                        return;

                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y = 5;
                    selected.x = 0;
                };

                SettingsSelectionLimit[5]++;

                var speed6 = GenerateButton(5, settings, Settings);
                speed6.RectTransform.anchoredPosition = new Vector2(74f, -80f);
                speed6.RectTransform.sizeDelta = new Vector2(140f, 48f);
                speed6.Text.alignment = TextAlignmentOptions.Center;
                speed6.Text.fontSize = 14;
                speed6.Text.text = $"[1.5x = {PlayerManager.ArcadeGameSpeed == 5}]";
                speed6.Text.color = ArcadeMenuManager.inst.textColor;
                speed6.Image.color = Color.Lerp(ArcadeMenuManager.inst.buttonBGColor, Color.white, 0.01f);
                speed6.Clickable.onEnter = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings || !Cursor.visible)
                        return;

                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y = 5;
                    selected.x = 1;
                };

                SettingsSelectionLimit[5]++;
                
                Settings.Add(new List<ArcadeMenuManager.Tab>());
                SettingsSelectionLimit.Add(0);

                var speed7 = GenerateButton(6, settings, Settings);
                speed7.RectTransform.anchoredPosition = new Vector2(74f, -130f);
                speed7.RectTransform.sizeDelta = new Vector2(140f, 48f);
                speed7.Text.alignment = TextAlignmentOptions.Center;
                speed7.Text.fontSize = 14;
                speed7.Text.text = $"[2.0x = {PlayerManager.ArcadeGameSpeed == 6}]";
                speed7.Text.color = ArcadeMenuManager.inst.textColor;
                speed7.Image.color = Color.Lerp(ArcadeMenuManager.inst.buttonBGColor, Color.white, 0.01f);
                speed7.Clickable.onEnter = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings || !Cursor.visible)
                        return;

                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y = 6;
                    selected.x = 0;
                };

                SettingsSelectionLimit[6]++;
                
                Settings.Add(new List<ArcadeMenuManager.Tab>());
                SettingsSelectionLimit.Add(0);

                var speed8 = GenerateButton(7, settings, Settings);
                speed8.RectTransform.anchoredPosition = new Vector2(74f, -180f);
                speed8.RectTransform.sizeDelta = new Vector2(140f, 48f);
                speed8.Text.alignment = TextAlignmentOptions.Center;
                speed8.Text.fontSize = 14;
                speed8.Text.text = $"[3.0x = {PlayerManager.ArcadeGameSpeed == 7}]";
                speed8.Text.color = ArcadeMenuManager.inst.textColor;
                speed8.Image.color = Color.Lerp(ArcadeMenuManager.inst.buttonBGColor, Color.white, 0.01f);
                speed8.Clickable.onEnter = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings || !Cursor.visible)
                        return;

                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y = 7;
                    selected.x = 0;
                };

                SettingsSelectionLimit[7]++;

                zen.Clickable.onClick = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings)
                        return;

                    AudioManager.inst.PlaySound("blip");
                    PlayerManager.SetGameMode(0);
                    zen.Text.text = $"[ZEN = {PlayerManager.IsZenMode}]";
                    normal.Text.text = $"[NORMAL = {PlayerManager.IsNormal}]";
                    life.Text.text = $"[1 LIFE = {PlayerManager.Is1Life}]";
                    hit.Text.text = $"[NO HIT = {PlayerManager.IsNoHit}]";
                    practice.Text.text = $"[PRACTICE = {PlayerManager.IsPractice}]";
                };
                normal.Clickable.onClick = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings)
                        return;

                    AudioManager.inst.PlaySound("blip");
                    PlayerManager.SetGameMode(1);
                    zen.Text.text = $"[ZEN = {PlayerManager.IsZenMode}]";
                    normal.Text.text = $"[NORMAL = {PlayerManager.IsNormal}]";
                    life.Text.text = $"[1 LIFE = {PlayerManager.Is1Life}]";
                    hit.Text.text = $"[NO HIT = {PlayerManager.IsNoHit}]";
                    practice.Text.text = $"[PRACTICE = {PlayerManager.IsPractice}]";
                };
                life.Clickable.onClick = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings)
                        return;

                    AudioManager.inst.PlaySound("blip");
                    PlayerManager.SetGameMode(2);
                    zen.Text.text = $"[ZEN = {PlayerManager.IsZenMode}]";
                    normal.Text.text = $"[NORMAL = {PlayerManager.IsNormal}]";
                    life.Text.text = $"[1 LIFE = {PlayerManager.Is1Life}]";
                    hit.Text.text = $"[NO HIT = {PlayerManager.IsNoHit}]";
                    practice.Text.text = $"[PRACTICE = {PlayerManager.IsPractice}]";
                };
                hit.Clickable.onClick = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings)
                        return;

                    AudioManager.inst.PlaySound("blip");
                    PlayerManager.SetGameMode(3);
                    zen.Text.text = $"[ZEN = {PlayerManager.IsZenMode}]";
                    normal.Text.text = $"[NORMAL = {PlayerManager.IsNormal}]";
                    life.Text.text = $"[1 LIFE = {PlayerManager.Is1Life}]";
                    hit.Text.text = $"[NO HIT = {PlayerManager.IsNoHit}]";
                    practice.Text.text = $"[PRACTICE = {PlayerManager.IsPractice}]";
                };
                practice.Clickable.onClick = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings)
                        return;

                    AudioManager.inst.PlaySound("blip");
                    PlayerManager.SetGameMode(4);
                    zen.Text.text = $"[ZEN = {PlayerManager.IsZenMode}]";
                    normal.Text.text = $"[NORMAL = {PlayerManager.IsNormal}]";
                    life.Text.text = $"[1 LIFE = {PlayerManager.Is1Life}]";
                    hit.Text.text = $"[NO HIT = {PlayerManager.IsNoHit}]";
                    practice.Text.text = $"[PRACTICE = {PlayerManager.IsPractice}]";
                };
                ldm.Clickable.onClick = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings)
                        return;

                    AudioManager.inst.PlaySound("blip");
                    CoreConfig.Instance.LDM.Value = !CoreConfig.Instance.LDM.Value;
                    ldm.Text.text = $"[LDM = {CoreConfig.Instance.LDM.Value}]";
                };

                speed1.Clickable.onClick = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings)
                        return;

                    AudioManager.inst.PlaySound("blip");

                    DataManager.inst.UpdateSettingEnum("ArcadeGameSpeed", 0);
                    AudioManager.inst.SetPitch(CoreHelper.Pitch);
                    var arcadeSpeed = PlayerManager.ArcadeGameSpeed;

                    speed1.Text.text = $"[0.1x = {arcadeSpeed == 0}]";
                    speed2.Text.text = $"[0.5x = {arcadeSpeed == 1}]";
                    speed3.Text.text = $"[0.8x = {arcadeSpeed == 2}]";
                    speed4.Text.text = $"[1.0x = {arcadeSpeed == 3}]";
                    speed5.Text.text = $"[1.2x = {arcadeSpeed == 4}]";
                    speed6.Text.text = $"[1.5x = {arcadeSpeed == 5}]";
                    speed7.Text.text = $"[2.0x = {arcadeSpeed == 6}]";
                    speed8.Text.text = $"[3.0x = {arcadeSpeed == 7}]";
                };
                speed2.Clickable.onClick = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings)
                        return;

                    AudioManager.inst.PlaySound("blip");

                    DataManager.inst.UpdateSettingEnum("ArcadeGameSpeed", 1);
                    AudioManager.inst.SetPitch(CoreHelper.Pitch);
                    var arcadeSpeed = PlayerManager.ArcadeGameSpeed;

                    speed1.Text.text = $"[0.1x = {arcadeSpeed == 0}]";
                    speed1.Text.text = $"[0.1x = {arcadeSpeed == 0}]";
                    speed2.Text.text = $"[0.5x = {arcadeSpeed == 1}]";
                    speed3.Text.text = $"[0.8x = {arcadeSpeed == 2}]";
                    speed4.Text.text = $"[1.0x = {arcadeSpeed == 3}]";
                    speed5.Text.text = $"[1.2x = {arcadeSpeed == 4}]";
                    speed6.Text.text = $"[1.5x = {arcadeSpeed == 5}]";
                    speed7.Text.text = $"[2.0x = {arcadeSpeed == 6}]";
                    speed8.Text.text = $"[3.0x = {arcadeSpeed == 7}]";
                };
                speed3.Clickable.onClick = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings)
                        return;

                    AudioManager.inst.PlaySound("blip");

                    DataManager.inst.UpdateSettingEnum("ArcadeGameSpeed", 2);
                    AudioManager.inst.SetPitch(CoreHelper.Pitch);
                    var arcadeSpeed = PlayerManager.ArcadeGameSpeed;

                    speed1.Text.text = $"[0.1x = {arcadeSpeed == 0}]";
                    speed1.Text.text = $"[0.1x = {arcadeSpeed == 0}]";
                    speed2.Text.text = $"[0.5x = {arcadeSpeed == 1}]";
                    speed3.Text.text = $"[0.8x = {arcadeSpeed == 2}]";
                    speed4.Text.text = $"[1.0x = {arcadeSpeed == 3}]";
                    speed5.Text.text = $"[1.2x = {arcadeSpeed == 4}]";
                    speed6.Text.text = $"[1.5x = {arcadeSpeed == 5}]";
                    speed7.Text.text = $"[2.0x = {arcadeSpeed == 6}]";
                    speed8.Text.text = $"[3.0x = {arcadeSpeed == 7}]";
                };
                speed4.Clickable.onClick = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings)
                        return;

                    AudioManager.inst.PlaySound("blip");

                    DataManager.inst.UpdateSettingEnum("ArcadeGameSpeed", 3);
                    AudioManager.inst.SetPitch(CoreHelper.Pitch);
                    var arcadeSpeed = PlayerManager.ArcadeGameSpeed;

                    speed1.Text.text = $"[0.1x = {arcadeSpeed == 0}]";
                    speed1.Text.text = $"[0.1x = {arcadeSpeed == 0}]";
                    speed2.Text.text = $"[0.5x = {arcadeSpeed == 1}]";
                    speed3.Text.text = $"[0.8x = {arcadeSpeed == 2}]";
                    speed4.Text.text = $"[1.0x = {arcadeSpeed == 3}]";
                    speed5.Text.text = $"[1.2x = {arcadeSpeed == 4}]";
                    speed6.Text.text = $"[1.5x = {arcadeSpeed == 5}]";
                    speed7.Text.text = $"[2.0x = {arcadeSpeed == 6}]";
                    speed8.Text.text = $"[3.0x = {arcadeSpeed == 7}]";
                };
                speed5.Clickable.onClick = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings)
                        return;

                    AudioManager.inst.PlaySound("blip");

                    DataManager.inst.UpdateSettingEnum("ArcadeGameSpeed", 4);
                    AudioManager.inst.SetPitch(CoreHelper.Pitch);
                    var arcadeSpeed = PlayerManager.ArcadeGameSpeed;

                    speed1.Text.text = $"[0.1x = {arcadeSpeed == 0}]";
                    speed1.Text.text = $"[0.1x = {arcadeSpeed == 0}]";
                    speed2.Text.text = $"[0.5x = {arcadeSpeed == 1}]";
                    speed3.Text.text = $"[0.8x = {arcadeSpeed == 2}]";
                    speed4.Text.text = $"[1.0x = {arcadeSpeed == 3}]";
                    speed5.Text.text = $"[1.2x = {arcadeSpeed == 4}]";
                    speed6.Text.text = $"[1.5x = {arcadeSpeed == 5}]";
                    speed7.Text.text = $"[2.0x = {arcadeSpeed == 6}]";
                    speed8.Text.text = $"[3.0x = {arcadeSpeed == 7}]";
                };
                speed6.Clickable.onClick = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings)
                        return;

                    AudioManager.inst.PlaySound("blip");

                    DataManager.inst.UpdateSettingEnum("ArcadeGameSpeed", 5);
                    AudioManager.inst.SetPitch(CoreHelper.Pitch);
                    var arcadeSpeed = PlayerManager.ArcadeGameSpeed;

                    speed1.Text.text = $"[0.1x = {arcadeSpeed == 0}]";
                    speed1.Text.text = $"[0.1x = {arcadeSpeed == 0}]";
                    speed2.Text.text = $"[0.5x = {arcadeSpeed == 1}]";
                    speed3.Text.text = $"[0.8x = {arcadeSpeed == 2}]";
                    speed4.Text.text = $"[1.0x = {arcadeSpeed == 3}]";
                    speed5.Text.text = $"[1.2x = {arcadeSpeed == 4}]";
                    speed6.Text.text = $"[1.5x = {arcadeSpeed == 5}]";
                    speed7.Text.text = $"[2.0x = {arcadeSpeed == 6}]";
                    speed8.Text.text = $"[3.0x = {arcadeSpeed == 7}]";
                };
                speed7.Clickable.onClick = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings)
                        return;

                    AudioManager.inst.PlaySound("blip");

                    DataManager.inst.UpdateSettingEnum("ArcadeGameSpeed", 6);
                    AudioManager.inst.SetPitch(CoreHelper.Pitch);
                    var arcadeSpeed = PlayerManager.ArcadeGameSpeed;

                    speed1.Text.text = $"[0.1x = {arcadeSpeed == 0}]";
                    speed1.Text.text = $"[0.1x = {arcadeSpeed == 0}]";
                    speed2.Text.text = $"[0.5x = {arcadeSpeed == 1}]";
                    speed3.Text.text = $"[0.8x = {arcadeSpeed == 2}]";
                    speed4.Text.text = $"[1.0x = {arcadeSpeed == 3}]";
                    speed5.Text.text = $"[1.2x = {arcadeSpeed == 4}]";
                    speed6.Text.text = $"[1.5x = {arcadeSpeed == 5}]";
                    speed7.Text.text = $"[2.0x = {arcadeSpeed == 6}]";
                    speed8.Text.text = $"[3.0x = {arcadeSpeed == 7}]";
                };
                speed8.Clickable.onClick = delegate (PointerEventData pointerEventData)
                {
                    if (!inSettings)
                        return;

                    AudioManager.inst.PlaySound("blip");

                    DataManager.inst.UpdateSettingEnum("ArcadeGameSpeed", 7);
                    AudioManager.inst.SetPitch(CoreHelper.Pitch);
                    var arcadeSpeed = PlayerManager.ArcadeGameSpeed;

                    speed1.Text.text = $"[0.1x = {arcadeSpeed == 0}]";
                    speed1.Text.text = $"[0.1x = {arcadeSpeed == 0}]";
                    speed2.Text.text = $"[0.5x = {arcadeSpeed == 1}]";
                    speed3.Text.text = $"[0.8x = {arcadeSpeed == 2}]";
                    speed4.Text.text = $"[1.0x = {arcadeSpeed == 3}]";
                    speed5.Text.text = $"[1.2x = {arcadeSpeed == 4}]";
                    speed6.Text.text = $"[1.5x = {arcadeSpeed == 5}]";
                    speed7.Text.text = $"[2.0x = {arcadeSpeed == 6}]";
                    speed8.Text.text = $"[3.0x = {arcadeSpeed == 7}]";
                };
            }

            UpdateRoundness();

            yield break;
        }

        public Image coverBase;
        public Image cover;
        public TextMeshProUGUI title;
        public TextMeshProUGUI artist;
        public TextMeshProUGUI creator;
        public TextMeshProUGUI difficulty;
        public TextMeshProUGUI description;

        public RectTransform settings;
        public Image settingsImage1;
        public Image settingsImage2;

        ArcadeMenuManager.Tab GenerateButton(int row, Transform parent, List<List<ArcadeMenuManager.Tab>> tabs)
        {
            var tabBase = UIManager.GenerateUIImage($"Button {row}", parent);
            tabBase.GetObject<RectTransform>().sizeDelta = Vector3.one;
            var tabText = UIManager.GenerateUITextMeshPro("Text", tabBase.GetObject<RectTransform>());
            tabText.GetObject<RectTransform>().sizeDelta = Vector3.one;

            var tab = new ArcadeMenuManager.Tab
            {
                GameObject = tabBase.GetObject<GameObject>(),
                RectTransform = tabBase.GetObject<RectTransform>(),
                Text = tabText.GetObject<TextMeshProUGUI>(),
                Image = tabBase.GetObject<Image>(),
                Clickable = tabBase.GetObject<GameObject>().AddComponent<Clickable>()
            };

            tabs[row].Add(tab);
            return tab;
        }

        public void UpdateRoundness()
        {
            for (int i = 0; i < Buttons.Count; i++)
            {
                for (int j = 0; j < Buttons[i].Count; j++)
                {
                    if (ArcadeConfig.Instance.PlayLevelMenuButtonsRoundness.Value != 0)
                        SpriteManager.SetRoundedSprite(Buttons[i][j].Image, ArcadeConfig.Instance.PlayLevelMenuButtonsRoundness.Value, SpriteManager.RoundedSide.W);
                    else
                        Buttons[i][j].Image.sprite = null;
                }
            }

            if (ArcadeConfig.Instance.PlayLevelMenuIconRoundness.Value != 0)
                SpriteManager.SetRoundedSprite(coverBase, ArcadeConfig.Instance.PlayLevelMenuIconRoundness.Value, SpriteManager.RoundedSide.W);
            else
                coverBase.sprite = null;
        }

        string openID;
        string closeID;
        public void OpenSettings()
        {
            selected = new Vector2Int(0, 0);

            if (openID != null)
            {
                AnimationManager.inst.RemoveID(openID);
            }
            
            if (closeID != null)
            {
                AnimationManager.inst.RemoveID(closeID);
            }

            var animation = new RTAnimation("Open Settings Animation");
            animation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, -410f, Ease.Linear),
                    new FloatKeyframe(0.2f, -10f, Ease.BackOut),
                    new FloatKeyframe(0.21f, -10f, Ease.Linear),
                }, delegate (float x)
                {
                    if (settings != null)
                        settings.anchoredPosition = new Vector2(0f, x);
                }),
            };
            animation.onComplete = delegate ()
            {
                openID = null;
                AnimationManager.inst.RemoveID(animation.id);
            };
            openID = animation.id;
            AnimationManager.inst.Play(animation);
        }

        public void CloseSettings()
        {
            selected = new Vector2Int(4, 1);

            if (openID != null)
            {
                AnimationManager.inst.RemoveID(openID);
            }

            if (closeID != null)
            {
                AnimationManager.inst.RemoveID(closeID);
            }

            var animation = new RTAnimation("Close Settings Animation");
            animation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, -10f, Ease.Linear),
                    new FloatKeyframe(0.1f, -410f, Ease.BackIn),
                    new FloatKeyframe(0.11f, -410f, Ease.Linear),
                }, delegate (float x)
                {
                    if (settings != null)
                        settings.anchoredPosition = new Vector2(0f, x);
                }),
            };
            animation.onComplete = delegate ()
            {
                closeID = null;
                AnimationManager.inst.RemoveID(animation.id);
            };
            closeID = animation.id;
            AnimationManager.inst.Play(animation);
        }

        /// <summary>
        /// Opens the Play Level Menu.
        /// </summary>
        /// <param name="level">Level to open.</param>
        public void OpenLevel(Level level)
        {
            CoreHelper.Log($"Set level: {level.metadata.song.title}");
            animating = true;
            CurrentLevel = level;
            cover.sprite = level.icon;
            title.text = level.metadata.song.title;
            artist.text = $"<b>Song by</b>: {level.metadata.artist.Name}";
            creator.text = $"<b>Level by</b>: {level.metadata.creator.steam_name}";
            var d = CoreHelper.GetDifficulty(level.metadata.song.difficulty);
            difficulty.text = $"<b>Difficulty</b>: <color=#{CoreHelper.ColorToHex(d.color)}>{d.name}</color>";
            description.text = level.metadata.song.description;

            Buttons[1][1].Text.text = LevelManager.ArcadeQueue.Has(x => x.id == level.id) ? "[REMOVE FROM QUEUE]" : "[ADD TO QUEUE]";
            Buttons[1][2].GameObject.SetActive(level.metadata.LevelArtist.URL != null);

            CoreHelper.UpdateDiscordStatus($"Selected: {level.metadata.song.title}", "In Arcade", "arcade");

            var animation = new RTAnimation("Open Play Level Menu Animation");
            animation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, -1080f, Ease.Linear),
                    new FloatKeyframe(0.5f, 0f, Ease.CircOut),
                    new FloatKeyframe(0.51f, 0f, Ease.Linear),
                }, delegate (float x)
                {
                    rectTransform.anchoredPosition = new Vector2(0f, x);
                }),
            };
            animation.onComplete = delegate ()
            {
                AnimationManager.inst.RemoveID(animation.id);
                animating = false;

                ArcadeMenuManager.inst.OpenedLocalLevel = true;
            };
            AnimationManager.inst.Play(animation);
        }

        public void Close()
        {
            animating = true;

            var animation = new RTAnimation("Close Play Level Menu Animation");
            animation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, 0f, Ease.Linear),
                    new FloatKeyframe(0.5f, -1080f, Ease.CircOut),
                    new FloatKeyframe(0.51f, -1080f, Ease.Linear),
                }, delegate (float x)
                {
                    rectTransform.anchoredPosition = new Vector2(0f, x);
                }),
            };
            animation.onComplete = delegate ()
            {
                AnimationManager.inst.RemoveID(animation.id);
                animating = false;

                if (MenuManager.inst)
                {
                    AudioManager.inst.PlayMusic(MenuManager.inst.currentMenuMusicName, MenuManager.inst.currentMenuMusic);
                }

                ArcadeMenuManager.inst.OpenedLocalLevel = false;
            };
            AnimationManager.inst.Play(animation);
        }
    }
}
