using BetterLegacy.Components;
using BetterLegacy.Configs;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using LSFunctions;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetterLegacy.Arcade
{
    public class DownloadLevelMenuManager : MonoBehaviour
    {
        public static DownloadLevelMenuManager inst;

        public RectTransform rectTransform;
        public Image background;

        public ArcadeMenuManager.OnlineLevelButton CurrentLevel { get; set; }

        void Awake()
        {
            inst = this;
        }

        void Update()
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

            title.color = ArcadeMenuManager.inst.textColor;
            artist.color = ArcadeMenuManager.inst.textColor;
            creator.color = ArcadeMenuManager.inst.textColor;
            difficulty.color = ArcadeMenuManager.inst.textColor;
            description.color = ArcadeMenuManager.inst.textColor;

            var currentTheme = DataManager.inst.interfaceSettings["UITheme"][SaveManager.inst.settings.Video.UITheme];

            background.color = LSColors.HexToColor(currentTheme["values"]["bg"]);

            if (!ArcadeMenuManager.inst || !ArcadeMenuManager.inst.OpenedOnlineLevel)
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
                if (selected.x < SelectionLimit[selected.y] - 1)
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
                    selected.x = Mathf.Clamp(selected.x, 0, SelectionLimit[selected.y] - 1);
                }
                else
                    AudioManager.inst.PlaySound("Block");
            }

            if (actions.Down.WasPressed)
            {
                if (selected.y < SelectionLimit.Count - 1)
                {
                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y++;
                    selected.x = Mathf.Clamp(selected.x, 0, SelectionLimit[selected.y] - 1);
                }
                else
                    AudioManager.inst.PlaySound("Block");
            }

            if (actions.Submit.WasPressed)
            {
                Buttons[selected.y][selected.x].Clickable?.onClick?.Invoke(null);
            }
        }

        public IEnumerator SetupDownloadLevelMenu()
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
                AudioManager.inst.PlaySound("blip");
                Close();
            };
            close.Clickable.onEnter = delegate (PointerEventData pointerEventData)
            {
                AudioManager.inst.PlaySound("LeftRight");
                selected.y = 0;
                selected.x = 0;
            };

            SelectionLimit.Add(0);

            // Download
            {
                var play = GenerateButton(1, rectTransform, Buttons);
                play.RectTransform.anchoredPosition = new Vector2(-700f, -400f);
                play.RectTransform.sizeDelta = new Vector2(300f, 100f);
                play.Text.alignment = TextAlignmentOptions.Center;
                play.Text.fontSize = 32;
                play.Text.text = "[DOWNLOAD]";
                play.Text.color = ArcadeMenuManager.inst.textColor;
                play.Image.color = Color.Lerp(ArcadeMenuManager.inst.buttonBGColor, Color.white, 0.01f);
                play.Clickable.onClick = delegate (PointerEventData pointerEventData)
                {
                    AudioManager.inst.PlaySound("blip");
                    CoreHelper.Log($"Start downloading");

                    if (animating || CurrentLevel == null)
                    {
                        CoreHelper.LogError($"NullReferenceException! how the heck did you get here");
                        return;
                    }

                    StartCoroutine(CurrentLevel.DownloadLevel());
                };
                play.Clickable.onEnter = delegate (PointerEventData pointerEventData)
                {
                    if (!Cursor.visible)
                        return;

                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y = 1;
                    selected.x = 0;
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

        /// <summary>
        /// Opens the Play Level Menu.
        /// </summary>
        /// <param name="level">Level to open.</param>
        public void OpenLevel(ArcadeMenuManager.OnlineLevelButton level)
        {
            CoreHelper.Log($"Set level: {level.Title}");
            animating = true;
            CurrentLevel = level;
            cover.sprite = level.Icon.sprite;
            title.text = level.Title;
            artist.text = $"<b>Song by</b>: {level.Artist}";
            creator.text = $"<b>Level by</b>: {level.Creator}";
            var d = CoreHelper.GetDifficulty(level.Difficulty);
            difficulty.text = $"<b>Difficulty</b>: <color=#{CoreHelper.ColorToHex(d.color)}>{d.name}</color>";
            description.text = level.Description;

            CoreHelper.UpdateDiscordStatus($"Selected Online: {level.Title}", "In Arcade", "arcade");

            var animation = new RTAnimation("Open Download Level Menu Animation");
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

                ArcadeMenuManager.inst.OpenedOnlineLevel = true;
            };
            AnimationManager.inst.Play(animation);
        }

        public void Close(Action onComplete = null)
        {
            animating = true;

            var animation = new RTAnimation("Close Download Level Menu Animation");
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

                ArcadeMenuManager.inst.OpenedOnlineLevel = false;

                onComplete?.Invoke();
            };
            AnimationManager.inst.Play(animation);
        }
    }
}
