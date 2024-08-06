using BetterLegacy.Components;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Menus;
using Crosstales.FB;
using InControl;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Cursor = UnityEngine.Cursor;
using Ease = BetterLegacy.Core.Animation.Ease;
using Screen = UnityEngine.Screen;

#pragma warning disable CS0618 // Type or member is obsolete
namespace BetterLegacy.Arcade
{
    /// <summary>
    /// The current arcade UI handler class.
    /// </summary>
    public class ArcadeMenuManager : MonoBehaviour
    {
        /// <summary>
        /// ArcadeMenuManager instance reference.
        /// </summary>
        public static ArcadeMenuManager inst;

        #region Base

        /// <summary>
        /// The base GameObject.
        /// </summary>
        public GameObject menuUI;

        // The colors of the UI based on menu themes.
        public Color textColor;
        public Color highlightColor;
        public Color textHighlightColor;
        public Color buttonBGColor;

        public static Vector2 ZeroFive => new Vector2(0.5f, 0.5f);
        public static Color ShadeColor => new Color(0f, 0f, 0f, 0.3f);

        #endregion

        #region Page

        /// <summary>
        /// The current maximum amount of levels per page.
        /// </summary>
        public static int MaxLevelsPerPage { get; set; } = 20;

        /// <summary>
        /// The list containing the current page of each tab.
        /// </summary>
        public List<int> CurrentPage { get; set; } = new List<int>
        {
            0,
            0,
            0,
            0,
            0,
            0,
        };

        #endregion

        #region Selection

        /// <summary>
        /// Current selection by X and Y coordinates.
        /// </summary>
        public Vector2Int selected;

        /// <summary>
        /// Selection coordinate limiter.
        /// </summary>
        public List<int> SelectionLimit { get; set; } = new List<int>();

        #endregion

        #region Tabs

        /// <summary>
        /// RectTransform parent of the tabs.
        /// </summary>
        public RectTransform TabContent { get; set; }

        /// <summary>
        /// The list for each base.
        /// </summary>
        public List<RectTransform> RegularBases { get; set; } = new List<RectTransform>();

        /// <summary>
        /// The list for each content.
        /// </summary>
        public List<RectTransform> RegularContents { get; set; } = new List<RectTransform>();

        /// <summary>
        /// A list containing all tabs.
        /// </summary>
        public List<Tab> Tabs { get; set; } = new List<Tab>();

        /// <summary>
        /// The current selected tab.
        /// </summary>
        public int CurrentTab { get; set; } = 0;

        #endregion

        #region Settings

        /// <summary>
        /// The settings list depending on which page is currently being viewed.
        /// </summary>
        List<List<Tab>> Settings { get; set; } = new List<List<Tab>>
        {
            new List<Tab>(),
            new List<Tab>(),
            new List<Tab>(),
            new List<Tab>(),
            new List<Tab>(),
            new List<Tab>(),
        };

        #endregion

        #region Open

        /// <summary>
        /// If local level view has been opened.
        /// </summary>
        public bool OpenedLocalLevel { get; set; }

        /// <summary>
        /// If online level view has been opened.
        /// </summary>
        public bool OpenedOnlineLevel { get; set; }

        /// <summary>
        /// If Steam level view has been opened.
        /// </summary>
        public bool OpenedSteamLevel { get; set; }

        #endregion

        #region Prefabs

        /// <summary>
        /// The prefab for levels to be used.
        /// </summary>
        public GameObject levelPrefab;

        #endregion

        /// <summary>
        /// If UI has initialized yet.
        /// </summary>
        public bool init = false;

        void Awake()
        {
            inst = this;
            StartCoroutine(SetupScene());
        }

        void Update()
        {
            UpdateTheme();

            // Check if main UI should be updated or not.
            if (!init || OpenedLocalLevel || OpenedOnlineLevel || OpenedSteamLevel)
                return;

            UpdateControls();

            // Update all tab colors.
            for (int i = 0; i < Tabs.Count; i++)
            {
                Tabs[i].Text.color = selected.y == 0 && i == selected.x ? textHighlightColor : textColor;
                Tabs[i].Image.color = selected.y == 0 && i == selected.x ? highlightColor : Color.Lerp(buttonBGColor, Color.white, 0.01f);
            }

            // Update all setting colors.
            for (int i = 0; i < Settings[CurrentTab].Count; i++)
            {
                var setting = Settings[CurrentTab][i];
                setting.Text.color = selected.y == setting.Position.y && setting.Position.x == selected.x ? textHighlightColor : textColor;
                setting.Image.color = selected.y == setting.Position.y && setting.Position.x == selected.x ? highlightColor : Color.Lerp(buttonBGColor, Color.white, 0.01f);
            }

            try
            {
                // Update local levels
                if (CurrentTab == 0)
                {
                    localPageField.caretColor = highlightColor;
                    localSearchField.caretColor = highlightColor;

                    var selectOnly = ArcadeConfig.Instance.OnlyShowShineOnSelected.Value;
                    var speed = ArcadeConfig.Instance.ShineSpeed.Value;
                    var maxDelay = ArcadeConfig.Instance.ShineMaxDelay.Value;
                    var minDelay = ArcadeConfig.Instance.ShineMinDelay.Value;
                    var color = ArcadeConfig.Instance.ShineColor.Value;

                    foreach (var level in LocalLevels)
                    {
                        if (loadingLocalLevels)
                            break;

                        var isSelected = selected.x == level.Position.x && selected.y - 2 == level.Position.y;

                        level.Title.color = isSelected ? textHighlightColor : textColor;
                        level.BaseImage.color = isSelected ? highlightColor : buttonBGColor;

                        var levelRank = LevelManager.GetLevelRank(level.Level);

                        if (level.Level.metadata.song.difficulty != 6)
                        {
                            var shineController = level.ShineController;

                            shineController.speed = speed;
                            shineController.maxDelay = maxDelay;
                            shineController.minDelay = minDelay;
                            shineController.offset = 260f;
                            shineController.offsetOverShoot = 32f;
                            level.shine1?.SetColor(color);
                            level.shine2?.SetColor(color);

                            if ((selectOnly && isSelected || !selectOnly) && levelRank.name == "SS" && shineController.currentLoop == 0)
                            {
                                shineController.LoopAnimation(-1, LSColors.yellow400);
                            }

                            if ((selectOnly && !isSelected || levelRank.name != "SS") && shineController.currentLoop == -1)
                            {
                                shineController.StopAnimation();
                            }
                        }

                        if (level.selected != isSelected)
                        {
                            level.selected = isSelected;
                            if (level.selected)
                            {
                                if (level.ExitAnimation != null)
                                {
                                    AnimationManager.inst.RemoveID(level.ExitAnimation.id);
                                }

                                level.EnterAnimation = new RTAnimation("Enter Animation");
                                level.EnterAnimation.animationHandlers = new List<AnimationHandlerBase>
                                {
                                    new AnimationHandler<float>(new List<IKeyframe<float>>
                                    {
                                        new FloatKeyframe(0f, 1f, Ease.Linear),
                                        new FloatKeyframe(0.3f, 1.1f, Ease.CircOut),
                                        new FloatKeyframe(0.31f, 1.1f, Ease.Linear),
                                    }, delegate (float x)
                                    {
                                        if (level.RectTransform != null)
                                            level.RectTransform.localScale = new Vector3(x, x, 1f);
                                    }),
                                };
                                level.EnterAnimation.onComplete = () => { AnimationManager.inst.RemoveID(level.EnterAnimation.id); };
                                AnimationManager.inst.Play(level.EnterAnimation);
                            }
                            else
                            {
                                if (level.EnterAnimation != null)
                                {
                                    AnimationManager.inst.RemoveID(level.EnterAnimation.id);
                                }

                                level.ExitAnimation = new RTAnimation("Exit Animation");
                                level.ExitAnimation.animationHandlers = new List<AnimationHandlerBase>
                                {
                                    new AnimationHandler<float>(new List<IKeyframe<float>>
                                    {
                                        new FloatKeyframe(0f, 1.1f, Ease.Linear),
                                        new FloatKeyframe(0.3f, 1f, Ease.BounceOut),
                                        new FloatKeyframe(0.31f, 1f, Ease.Linear),
                                    }, delegate (float x)
                                    {
                                        if (level.RectTransform != null)
                                            level.RectTransform.localScale = new Vector3(x, x, 1f);
                                    }),
                                };
                                level.ExitAnimation.onComplete = () => { AnimationManager.inst.RemoveID(level.ExitAnimation.id); };
                                AnimationManager.inst.Play(level.ExitAnimation);
                            }
                        }

                        if (isSelected && !CoreHelper.IsUsingInputField && InputDataManager.inst.menuActions.Submit.WasPressed)
                        {
                            level.Clickable?.onClick?.Invoke(null);
                        }
                    }
                }

                // Update online levels
                if (CurrentTab == 1)
                {
                    onlinePageField.caretColor = highlightColor;
                    onlineSearchField.caretColor = highlightColor;

                    foreach (var level in OnlineLevels)
                    {
                        if (loadingOnlineLevels)
                            break;

                        var isSelected = selected.x == level.Position.x && selected.y - 3 == level.Position.y;

                        level.TitleText.color = isSelected ? textHighlightColor : textColor;
                        level.BaseImage.color = isSelected ? highlightColor : buttonBGColor;

                        if (isSelected && !CoreHelper.IsUsingInputField && InputDataManager.inst.menuActions.Submit.WasPressed)
                        {
                            level.Clickable?.onClick?.Invoke(null);
                        }

                        if (level.selected != isSelected)
                        {
                            level.selected = isSelected;
                            if (level.selected)
                            {
                                if (level.ExitAnimation != null)
                                {
                                    AnimationManager.inst.RemoveID(level.ExitAnimation.id);
                                }

                                level.EnterAnimation = new RTAnimation("Enter Animation");
                                level.EnterAnimation.animationHandlers = new List<AnimationHandlerBase>
                                {
                                    new AnimationHandler<float>(new List<IKeyframe<float>>
                                    {
                                        new FloatKeyframe(0f, 1f, Ease.Linear),
                                        new FloatKeyframe(0.3f, 1.1f, Ease.CircOut),
                                        new FloatKeyframe(0.31f, 1.1f, Ease.Linear),
                                    }, delegate (float x)
                                    {
                                        if (level.RectTransform != null)
                                            level.RectTransform.localScale = new Vector3(x, x, 1f);
                                    }),
                                };
                                level.EnterAnimation.onComplete = () => { AnimationManager.inst.RemoveID(level.EnterAnimation.id); };
                                AnimationManager.inst.Play(level.EnterAnimation);
                            }
                            else
                            {
                                if (level.EnterAnimation != null)
                                {
                                    AnimationManager.inst.RemoveID(level.EnterAnimation.id);
                                }

                                level.ExitAnimation = new RTAnimation("Exit Animation");
                                level.ExitAnimation.animationHandlers = new List<AnimationHandlerBase>
                                {
                                    new AnimationHandler<float>(new List<IKeyframe<float>>
                                    {
                                        new FloatKeyframe(0f, 1.1f, Ease.Linear),
                                        new FloatKeyframe(0.3f, 1f, Ease.BounceOut),
                                        new FloatKeyframe(0.31f, 1f, Ease.Linear),
                                    }, delegate (float x)
                                    {
                                        if (level.RectTransform != null)
                                            level.RectTransform.localScale = new Vector3(x, x, 1f);
                                    }),
                                };
                                level.ExitAnimation.onComplete = () => { AnimationManager.inst.RemoveID(level.ExitAnimation.id); };
                                AnimationManager.inst.Play(level.ExitAnimation);
                            }
                        }

                    }
                }

                // Update queue levels
                if (CurrentTab == 4)
                {
                    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.V))
                    {
                        RTArcade.PasteArcadeQueue();
                    }

                    queuePageField.caretColor = highlightColor;
                    queueSearchField.caretColor = highlightColor;

                    foreach (var level in QueueLevels)
                    {
                        if (loadingQueuedLevels)
                            break;

                        var isSelected = selected.x == level.Position.x && selected.y - 2 == level.Position.y;

                        level.Title.color = isSelected ? textHighlightColor : textColor;
                        level.BaseImage.color = isSelected ? highlightColor : buttonBGColor;

                        var levelRank = LevelManager.GetLevelRank(level.Level);

                        if (level.selected != isSelected)
                        {
                            level.selected = isSelected;
                            if (level.selected)
                            {
                                if (level.ExitAnimation != null)
                                {
                                    AnimationManager.inst.RemoveID(level.ExitAnimation.id);
                                }

                                level.EnterAnimation = new RTAnimation("Enter Animation");
                                level.EnterAnimation.animationHandlers = new List<AnimationHandlerBase>
                                {
                                    new AnimationHandler<float>(new List<IKeyframe<float>>
                                    {
                                        new FloatKeyframe(0f, 1f, Ease.Linear),
                                        new FloatKeyframe(0.3f, 1.04f, Ease.CircOut),
                                        new FloatKeyframe(0.31f, 1.04f, Ease.Linear),
                                    }, delegate (float x)
                                    {
                                        if (level.RectTransform != null)
                                            level.RectTransform.localScale = new Vector3(x, x, 1f);
                                    }),
                                };
                                level.EnterAnimation.onComplete = () => { AnimationManager.inst.RemoveID(level.EnterAnimation.id); };
                                AnimationManager.inst.Play(level.EnterAnimation);
                            }
                            else
                            {
                                if (level.EnterAnimation != null)
                                {
                                    AnimationManager.inst.RemoveID(level.EnterAnimation.id);
                                }

                                level.ExitAnimation = new RTAnimation("Exit Animation");
                                level.ExitAnimation.animationHandlers = new List<AnimationHandlerBase>
                                {
                                    new AnimationHandler<float>(new List<IKeyframe<float>>
                                    {
                                        new FloatKeyframe(0f, 1.04f, Ease.Linear),
                                        new FloatKeyframe(0.3f, 1f, Ease.BounceOut),
                                        new FloatKeyframe(0.31f, 1f, Ease.Linear),
                                    }, delegate (float x)
                                    {
                                        if (level.RectTransform != null)
                                            level.RectTransform.localScale = new Vector3(x, x, 1f);
                                    }),
                                };
                                level.ExitAnimation.onComplete = () => { AnimationManager.inst.RemoveID(level.ExitAnimation.id); };
                                AnimationManager.inst.Play(level.ExitAnimation);
                            }
                        }

                        if (isSelected && !CoreHelper.IsUsingInputField && InputDataManager.inst.menuActions.Submit.WasPressed)
                        {
                            level.Clickable?.onClick?.Invoke(null);
                        }
                    }
                }

                // Update Steam levels
                if (CurrentTab == 5)
                {
                    steamPageField.caretColor = highlightColor;
                    steamSearchField.caretColor = highlightColor;

                    var selectOnly = ArcadeConfig.Instance.OnlyShowShineOnSelected.Value;
                    var speed = ArcadeConfig.Instance.ShineSpeed.Value;
                    var maxDelay = ArcadeConfig.Instance.ShineMaxDelay.Value;
                    var minDelay = ArcadeConfig.Instance.ShineMinDelay.Value;
                    var color = ArcadeConfig.Instance.ShineColor.Value;

                    if (steamViewType == SteamViewType.Subscribed && SteamWorkshopManager.inst.hasLoaded)
                        foreach (var level in SubscribedSteamLevels)
                        {
                            if (loadingSteamLevels)
                                break;

                            var isSelected = selected.x == level.Position.x && selected.y - 2 == level.Position.y;

                            level.Title.color = isSelected ? textHighlightColor : textColor;
                            level.BaseImage.color = isSelected ? highlightColor : buttonBGColor;

                            var levelRank = LevelManager.GetLevelRank(level.Level);

                            if (level.Level.metadata.song.difficulty != 6)
                            {
                                var shineController = level.ShineController;

                                shineController.speed = speed;
                                shineController.maxDelay = maxDelay;
                                shineController.minDelay = minDelay;
                                shineController.offset = 260f;
                                shineController.offsetOverShoot = 32f;
                                level.shine1?.SetColor(color);
                                level.shine2?.SetColor(color);

                                if ((selectOnly && isSelected || !selectOnly) && levelRank.name == "SS" && shineController.currentLoop == 0)
                                {
                                    shineController.LoopAnimation(-1, LSColors.yellow400);
                                }

                                if ((selectOnly && !isSelected || levelRank.name != "SS") && shineController.currentLoop == -1)
                                {
                                    shineController.StopAnimation();
                                }
                            }

                            if (level.selected != isSelected)
                            {
                                level.selected = isSelected;
                                if (level.selected)
                                {
                                    if (level.ExitAnimation != null)
                                    {
                                        AnimationManager.inst.RemoveID(level.ExitAnimation.id);
                                    }

                                    level.EnterAnimation = new RTAnimation("Enter Animation");
                                    level.EnterAnimation.animationHandlers = new List<AnimationHandlerBase>
                                    {
                                        new AnimationHandler<float>(new List<IKeyframe<float>>
                                        {
                                            new FloatKeyframe(0f, 1f, Ease.Linear),
                                            new FloatKeyframe(0.3f, 1.1f, Ease.CircOut),
                                            new FloatKeyframe(0.31f, 1.1f, Ease.Linear),
                                        }, delegate (float x)
                                        {
                                            if (level.RectTransform != null)
                                                level.RectTransform.localScale = new Vector3(x, x, 1f);
                                        }),
                                    };
                                    level.EnterAnimation.onComplete = () => { AnimationManager.inst.RemoveID(level.EnterAnimation.id); };
                                    AnimationManager.inst.Play(level.EnterAnimation);
                                }
                                else
                                {
                                    if (level.EnterAnimation != null)
                                    {
                                        AnimationManager.inst.RemoveID(level.EnterAnimation.id);
                                    }

                                    level.ExitAnimation = new RTAnimation("Exit Animation");
                                    level.ExitAnimation.animationHandlers = new List<AnimationHandlerBase>
                                    {
                                        new AnimationHandler<float>(new List<IKeyframe<float>>
                                        {
                                            new FloatKeyframe(0f, 1.1f, Ease.Linear),
                                            new FloatKeyframe(0.3f, 1f, Ease.BounceOut),
                                            new FloatKeyframe(0.31f, 1f, Ease.Linear),
                                        }, delegate (float x)
                                        {
                                            if (level.RectTransform != null)
                                                level.RectTransform.localScale = new Vector3(x, x, 1f);
                                        }),
                                    };
                                    level.ExitAnimation.onComplete = () => { AnimationManager.inst.RemoveID(level.ExitAnimation.id); };
                                    AnimationManager.inst.Play(level.ExitAnimation);
                                }
                            }

                            if (isSelected && !CoreHelper.IsUsingInputField && InputDataManager.inst.menuActions.Submit.WasPressed)
                            {
                                level.Clickable?.onClick?.Invoke(null);
                            }
                        }

                    if (steamViewType == SteamViewType.Online)
                        foreach (var level in OnlineSteamLevels)
                        {
                            if (loadingSteamLevels)
                                break;

                            var isSelected = selected.x == level.Position.x && selected.y - 2 == level.Position.y;

                            level.TitleText.color = isSelected ? textHighlightColor : textColor;
                            level.BaseImage.color = isSelected ? highlightColor : buttonBGColor;

                            if (level.selected != isSelected)
                            {
                                level.selected = isSelected;
                                if (level.selected)
                                {
                                    if (level.ExitAnimation != null)
                                    {
                                        AnimationManager.inst.RemoveID(level.ExitAnimation.id);
                                    }

                                    level.EnterAnimation = new RTAnimation("Enter Animation");
                                    level.EnterAnimation.animationHandlers = new List<AnimationHandlerBase>
                                    {
                                        new AnimationHandler<float>(new List<IKeyframe<float>>
                                        {
                                            new FloatKeyframe(0f, 1f, Ease.Linear),
                                            new FloatKeyframe(0.3f, 1.1f, Ease.CircOut),
                                            new FloatKeyframe(0.31f, 1.1f, Ease.Linear),
                                        }, delegate (float x)
                                        {
                                            if (level.RectTransform != null)
                                                level.RectTransform.localScale = new Vector3(x, x, 1f);
                                        }),
                                    };
                                    level.EnterAnimation.onComplete = () => { AnimationManager.inst.RemoveID(level.EnterAnimation.id); };
                                    AnimationManager.inst.Play(level.EnterAnimation);
                                }
                                else
                                {
                                    if (level.EnterAnimation != null)
                                    {
                                        AnimationManager.inst.RemoveID(level.EnterAnimation.id);
                                    }

                                    level.ExitAnimation = new RTAnimation("Exit Animation");
                                    level.ExitAnimation.animationHandlers = new List<AnimationHandlerBase>
                                    {
                                        new AnimationHandler<float>(new List<IKeyframe<float>>
                                        {
                                            new FloatKeyframe(0f, 1.1f, Ease.Linear),
                                            new FloatKeyframe(0.3f, 1f, Ease.BounceOut),
                                            new FloatKeyframe(0.31f, 1f, Ease.Linear),
                                        }, delegate (float x)
                                        {
                                            if (level.RectTransform != null)
                                                level.RectTransform.localScale = new Vector3(x, x, 1f);
                                        }),
                                    };
                                    level.ExitAnimation.onComplete = () => { AnimationManager.inst.RemoveID(level.ExitAnimation.id); };
                                    AnimationManager.inst.Play(level.ExitAnimation);
                                }
                            }

                            if (isSelected && !CoreHelper.IsUsingInputField && InputDataManager.inst.menuActions.Submit.WasPressed)
                            {
                                level.Clickable?.onClick?.Invoke(null);
                            }
                        }
                }
            }
            catch
            {

            }
        }

        /// <summary>
        /// Control system.
        /// </summary>
        void UpdateControls()
        {
            if (CoreHelper.IsUsingInputField || loadingOnlineLevels || loadingLocalLevels)
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

            // Enter tab
            if (actions.Submit.WasPressed && selected.y == 0)
            {
                AudioManager.inst.PlaySound("blip");
                SelectTab();
            }

            // Click setting 1
            if (actions.Submit.WasPressed && selected.y == 1)
            {
                Settings[CurrentTab][selected.x].Clickable.onClick?.Invoke(null);
            }

            // Click setting 2
            if (actions.Submit.WasPressed && selected.y == 2 && CurrentTab == 1)
            {
                Settings[CurrentTab][selected.x].Clickable.onClick?.Invoke(null);
            }

            // Set tabs
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha1))
            {
                AudioManager.inst.PlaySound("blip");
                selected.y = 0;
                selected.x = 1;
                SelectTab();
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha2))
            {
                AudioManager.inst.PlaySound("blip");
                selected.y = 0;
                selected.x = 2;
                SelectTab();
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha3))
            {
                AudioManager.inst.PlaySound("blip");
                selected.y = 0;
                selected.x = 3;
                SelectTab();
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha4))
            {
                AudioManager.inst.PlaySound("blip");
                selected.y = 0;
                selected.x = 4;
                SelectTab();
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha5))
            {
                AudioManager.inst.PlaySound("blip");
                selected.y = 0;
                selected.x = 5;
                SelectTab();
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha6))
            {
                AudioManager.inst.PlaySound("blip");
                selected.y = 0;
                selected.x = 6;
                SelectTab();
            }
        }

        void UpdateTheme()
        {
            var currentTheme = DataManager.inst.interfaceSettings["UITheme"][SaveManager.inst.settings.Video.UITheme];

            Camera.main.backgroundColor = LSColors.HexToColor(currentTheme["values"]["bg"]);
            textColor = currentTheme["values"]["text"] == "transparent" ? ShadeColor : LSColors.HexToColor(currentTheme["values"]["text"]);
            highlightColor = currentTheme["values"]["highlight"] == "transparent" ? ShadeColor : LSColors.HexToColor(currentTheme["values"]["highlight"]);
            textHighlightColor = currentTheme["values"]["text-highlight"] == "transparent" ? ShadeColor : LSColors.HexToColor(currentTheme["values"]["text-highlight"]);
            buttonBGColor = currentTheme["values"]["buttonbg"] == "transparent" ? ShadeColor : LSColors.HexToColor(currentTheme["values"]["buttonbg"]);
        }

        public bool CanSelect => Cursor.visible && !loadingOnlineLevels && !loadingLocalLevels;

        #region Setup

        public IEnumerator DeleteComponents()
        {
            Destroy(GameObject.Find("Interface"));

            var eventSystem = GameObject.Find("EventSystem");
            Destroy(eventSystem.GetComponent<InControlInputModule>());
            Destroy(eventSystem.GetComponent<BaseInput>());
            eventSystem.AddComponent<StandaloneInputModule>();

            var mainCamera = GameObject.Find("Main Camera");
            Destroy(mainCamera.GetComponent<InterfaceLoader>());
            Destroy(mainCamera.GetComponent<ArcadeController>());
            Destroy(mainCamera.GetComponent<FlareLayer>());
            Destroy(mainCamera.GetComponent<GUILayer>());
            yield break;
        }

        public IEnumerator SetupScene()
        {
            LSHelpers.ShowCursor();
            yield return StartCoroutine(DeleteComponents());
            UpdateTheme();

            if (MenuManager.inst)
            {
                AudioManager.inst.PlayMusic(MenuManager.inst.currentMenuMusicName, MenuManager.inst.currentMenuMusic);
            }

            LevelManager.current = 0;

            #region Interface Setup

            var inter = new GameObject("Arcade UI");
            inter.transform.localScale = Vector3.one * CoreHelper.ScreenScale;
            menuUI = inter;
            inter.AddComponent<CursorManager>();
            var interfaceRT = inter.AddComponent<RectTransform>();
            interfaceRT.anchoredPosition = new Vector2(960f, 540f);
            interfaceRT.sizeDelta = new Vector2(1920f, 1080f);
            interfaceRT.pivot = new Vector2(0.5f, 0.5f);
            interfaceRT.anchorMin = Vector2.zero;
            interfaceRT.anchorMax = Vector2.zero;

            var canvas = inter.AddComponent<Canvas>();
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.Tangent;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.Normal;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.scaleFactor = CoreHelper.ScreenScale;
            canvas.sortingOrder = 10000;

            var canvasScaler = inter.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(Screen.width, Screen.height);

            inter.AddComponent<GraphicRaycaster>();

            var selectionBase = new GameObject("Selection Base");
            selectionBase.transform.SetParent(inter.transform);
            selectionBase.transform.localScale = Vector3.one;

            var selectionBaseRT = selectionBase.AddComponent<RectTransform>();
            selectionBaseRT.anchoredPosition = Vector2.zero;

            var playLevelMenuBase = new GameObject("Play Level Menu");
            playLevelMenuBase.transform.SetParent(inter.transform);
            playLevelMenuBase.transform.localScale = Vector3.one;

            var playLevelMenuBaseRT = playLevelMenuBase.AddComponent<RectTransform>();
            playLevelMenuBaseRT.anchoredPosition = new Vector2(0f, -1080f);
            playLevelMenuBaseRT.sizeDelta = new Vector2(1920f, 1080f);

            var playLevelMenuImage = playLevelMenuBase.AddComponent<Image>();

            var playLevelMenu = playLevelMenuBase.AddComponent<PlayLevelMenuManager>();
            playLevelMenu.rectTransform = playLevelMenuBaseRT;
            playLevelMenu.background = playLevelMenuImage;

            StartCoroutine(playLevelMenu.SetupPlayLevelMenu());

            var downloadLevelMenuBase = new GameObject("Download Level Menu");
            downloadLevelMenuBase.transform.SetParent(inter.transform);
            downloadLevelMenuBase.transform.localScale = Vector3.one;

            var downloadLevelMenuBaseRT = downloadLevelMenuBase.AddComponent<RectTransform>();
            downloadLevelMenuBaseRT.anchoredPosition = new Vector2(0f, -1080f);
            downloadLevelMenuBaseRT.sizeDelta = new Vector2(1920f, 1080f);

            var downloadLevelMenuImage = downloadLevelMenuBase.AddComponent<Image>();

            var downloadLevelMenu = downloadLevelMenuBase.AddComponent<DownloadLevelMenuManager>();
            downloadLevelMenu.rectTransform = downloadLevelMenuBaseRT;
            downloadLevelMenu.background = downloadLevelMenuImage;

            StartCoroutine(downloadLevelMenu.SetupDownloadLevelMenu());

            var steamLevelMenuBase = new GameObject("Steam Level Menu");
            steamLevelMenuBase.transform.SetParent(inter.transform);
            steamLevelMenuBase.transform.localScale = Vector3.one;

            var steamLevelMenuBaseRT = steamLevelMenuBase.AddComponent<RectTransform>();
            steamLevelMenuBaseRT.anchoredPosition = new Vector2(0f, -1080f);
            steamLevelMenuBaseRT.sizeDelta = new Vector2(1920f, 1080f);

            var steamLevelMenuImage = steamLevelMenuBase.AddComponent<Image>();

            var steamLevelMenu = steamLevelMenuBase.AddComponent<SteamLevelMenuManager>();
            steamLevelMenu.rectTransform = steamLevelMenuBaseRT;
            steamLevelMenu.background = steamLevelMenuImage;

            StartCoroutine(steamLevelMenu.SetupSteamLevelMenu());

            #endregion

            #region Tabs

            var topBar = UIManager.GenerateUIImage("Top Bar", selectionBaseRT);

            TabContent = (RectTransform)topBar["RectTransform"];
            UIManager.SetRectTransform(TabContent, new Vector2(0f, 480f), ZeroFive, ZeroFive, ZeroFive, new Vector2(1920f, 115f));

            //((Image)topBar["Image"]).color = buttonBGColor;

            topBar.GetObject<Image>().color = buttonBGColor;

            string[] tabNames = new string[]
            {
                "Local",
                "Online",
                "Browser",
                "Download",
                "Queue",
                "Steam",
            };

            SelectionLimit.Add(1);

            var close = GenerateTab();
            close.RectTransform.anchoredPosition = new Vector2(-904f, 0f);
            close.RectTransform.sizeDelta = new Vector2(100f, 100f);
            close.Text.alignment = TextAlignmentOptions.Center;
            close.Text.fontSize = 72;
            close.Text.text = "<scale=1.3><pos=6>X";
            close.Text.color = textColor;
            close.Image.color = Color.Lerp(buttonBGColor, Color.white, 0.01f);
            close.Clickable.onClick = delegate (PointerEventData pointerEventData)
            {
                AudioManager.inst.PlaySound("blip");
                SelectTab();
            };
            close.Clickable.onEnter = delegate (PointerEventData pointerEventData)
            {
                if (!CanSelect)
                    return;

                AudioManager.inst.PlaySound("LeftRight");
                selected.y = 0;
                selected.x = 0;
            };

            if (ArcadeConfig.Instance.TabsRoundedness.Value != 0)
                SpriteManager.SetRoundedSprite(close.Image, ArcadeConfig.Instance.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
            else
                close.Image.sprite = null;

            for (int i = 0; i < 6; i++)
            {
                int index = i;

                var tab = GenerateTab();

                tab.RectTransform.anchoredPosition = new Vector2(-700f + (i * 300), 0f);
                tab.RectTransform.sizeDelta = new Vector2(290f, 100f);
                tab.Text.alignment = TextAlignmentOptions.Center;
                tab.Text.text = tabNames[Mathf.Clamp(i, 0, tabNames.Length - 1)];
                tab.Text.color = textColor;
                tab.Image.color = Color.Lerp(buttonBGColor, Color.white, 0.01f);

                if (ArcadeConfig.Instance.TabsRoundedness.Value != 0)
                    SpriteManager.SetRoundedSprite(tab.Image, ArcadeConfig.Instance.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                else
                    tab.Image.sprite = null;

                tab.Clickable.onClick = delegate (PointerEventData pointerEventData)
                {
                    AudioManager.inst.PlaySound("blip");
                    SelectTab();
                };
                tab.Clickable.onEnter = delegate (PointerEventData pointerEventData)
                {
                    if (!CanSelect)
                        return;

                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y = 0;
                    selected.x = index + 1;
                };
                SelectionLimit[0]++;
            }

            #endregion

            // Settings
            SelectionLimit.Add(0);

            // Local
            {
                var local = new GameObject("Local");
                local.transform.SetParent(selectionBaseRT);
                local.transform.localScale = Vector3.one;

                var localRT = local.AddComponent<RectTransform>();
                localRT.anchoredPosition = Vector3.zero;
                localRT.sizeDelta = new Vector2(0f, 0f);

                RegularBases.Add(localRT);

                // Settings
                {
                    var localSettingsBar = UIManager.GenerateUIImage("Settings Bar", localRT);

                    UIManager.SetRectTransform(localSettingsBar.GetObject<RectTransform>(), new Vector2(0f, 360f), ZeroFive, ZeroFive, ZeroFive, new Vector2(1920f, 120f));

                    localSettingsBar.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.01f);

                    var reload = UIManager.GenerateUIImage("Reload", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(reload.GetObject<RectTransform>(), new Vector2(-600f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(200f, 64f));

                    var reloadClickable = reload.GetObject<GameObject>().AddComponent<Clickable>();

                    reload.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                    if (ArcadeConfig.Instance.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(reload.GetObject<Image>(), ArcadeConfig.Instance.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        reload.GetObject<Image>().sprite = null;

                    var reloadText = UIManager.GenerateUITextMeshPro("Text", reload.GetObject<RectTransform>());
                    UIManager.SetRectTransform(reloadText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    reloadText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    reloadText.GetObject<TextMeshProUGUI>().fontSize = 32;
                    reloadText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                    reloadText.GetObject<TextMeshProUGUI>().text = "[RELOAD]";

                    Settings[0].Add(new Tab
                    {
                        GameObject = reload.GetObject<GameObject>(),
                        RectTransform = reload.GetObject<RectTransform>(),
                        Clickable = reloadClickable,
                        Image = reload.GetObject<Image>(),
                        Text = reloadText.GetObject<TextMeshProUGUI>(),
                        Position = new Vector2Int(0, 1),
                    });

                    var prevPage = UIManager.GenerateUIImage("Previous", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(prevPage.GetObject<RectTransform>(), new Vector2(500f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(80f, 64f));

                    var prevPageClickable = prevPage.GetObject<GameObject>().AddComponent<Clickable>();

                    prevPage.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                    if (ArcadeConfig.Instance.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(prevPage.GetObject<Image>(), ArcadeConfig.Instance.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        prevPage.GetObject<Image>().sprite = null;

                    var prevPageText = UIManager.GenerateUITextMeshPro("Text", prevPage.GetObject<RectTransform>());
                    UIManager.SetRectTransform(prevPageText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    prevPageText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    prevPageText.GetObject<TextMeshProUGUI>().fontSize = 64;
                    prevPageText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                    prevPageText.GetObject<TextMeshProUGUI>().text = "<";

                    Settings[0].Add(new Tab
                    {
                        GameObject = prevPage.GetObject<GameObject>(),
                        RectTransform = prevPage.GetObject<RectTransform>(),
                        Clickable = prevPageClickable,
                        Image = prevPage.GetObject<Image>(),
                        Text = prevPageText.GetObject<TextMeshProUGUI>(),
                        Position = new Vector2Int(1, 1),
                    });

                    var pageField = UIManager.GenerateUIInputField("Page", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(pageField.GetObject<RectTransform>(), new Vector2(650f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(150f, 64f));
                    pageField.GetObject<Image>().color = CoreHelper.InvertColorHue(CoreHelper.InvertColorValue(Color.Lerp(buttonBGColor, Color.black, 0.2f)));

                    ((Text)pageField["Placeholder"]).alignment = TextAnchor.MiddleCenter;
                    ((Text)pageField["Placeholder"]).text = "Page...";
                    ((Text)pageField["Placeholder"]).color = LSColors.fadeColor(CoreHelper.InvertColorHue(CoreHelper.InvertColorValue(textColor)), 0.2f);
                    localPageField = pageField.GetObject<InputField>();
                    localPageField.onValueChanged.ClearAll();
                    localPageField.textComponent.alignment = TextAnchor.MiddleCenter;
                    localPageField.textComponent.fontSize = 30;
                    localPageField.textComponent.color = CoreHelper.InvertColorHue(CoreHelper.InvertColorValue(textColor));
                    localPageField.text = DataManager.inst.GetSettingInt("CurrentArcadePage", 0).ToString();

                    if (ArcadeConfig.Instance.PageFieldRoundness.Value != 0)
                        SpriteManager.SetRoundedSprite(localPageField.image, ArcadeConfig.Instance.PageFieldRoundness.Value, SpriteManager.RoundedSide.W);
                    else
                        localPageField.image.sprite = null;

                    var nextPage = UIManager.GenerateUIImage("Next", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(nextPage.GetObject<RectTransform>(), new Vector2(800f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(80f, 64f));

                    var nextPageClickable = nextPage.GetObject<GameObject>().AddComponent<Clickable>();

                    nextPage.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                    if (ArcadeConfig.Instance.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(nextPage.GetObject<Image>(), ArcadeConfig.Instance.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        nextPage.GetObject<Image>().sprite = null;

                    var nextPageText = UIManager.GenerateUITextMeshPro("Text", nextPage.GetObject<RectTransform>());
                    UIManager.SetRectTransform(nextPageText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    nextPageText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    nextPageText.GetObject<TextMeshProUGUI>().fontSize = 64;
                    nextPageText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                    nextPageText.GetObject<TextMeshProUGUI>().text = ">";

                    Settings[0].Add(new Tab
                    {
                        GameObject = nextPage.GetObject<GameObject>(),
                        RectTransform = nextPage.GetObject<RectTransform>(),
                        Clickable = nextPageClickable,
                        Image = nextPage.GetObject<Image>(),
                        Text = nextPageText.GetObject<TextMeshProUGUI>(),
                        Position = new Vector2Int(2, 1),
                    });

                    localPageField.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (int.TryParse(_val, out int p))
                        {
                            p = Mathf.Clamp(p, 0, LocalPageCount);
                            SetLocalLevelsPage(p);

                            DataManager.inst.UpdateSettingInt("CurrentArcadePage", p);
                        }
                    });

                    reloadClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(0, 1);
                    };
                    reloadClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        AudioManager.inst.PlaySound("blip");
                        var menu = new GameObject("Load Level System");
                        menu.AddComponent<LoadLevelsManager>();
                    };

                    prevPageClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(1, 1);
                    };
                    prevPageClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        if (int.TryParse(localPageField.text, out int p))
                        {
                            if (p > 0)
                            {
                                AudioManager.inst.PlaySound("blip");
                                localPageField.text = Mathf.Clamp(p - 1, 0, LocalPageCount).ToString();
                            }
                            else
                            {
                                AudioManager.inst.PlaySound("Block");
                            }
                        }
                    };

                    nextPageClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(2, 1);
                    };
                    nextPageClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        if (int.TryParse(localPageField.text, out int p))
                        {
                            if (p < LocalPageCount)
                            {
                                AudioManager.inst.PlaySound("blip");
                                localPageField.text = Mathf.Clamp(p + 1, 0, LocalPageCount).ToString();
                            }
                            else
                            {
                                AudioManager.inst.PlaySound("Block");
                            }
                        }
                    };
                }

                var left = UIManager.GenerateUIImage("Left", localRT);
                UIManager.SetRectTransform(left.GetObject<RectTransform>(), new Vector2(-880f, 300f), ZeroFive, ZeroFive, new Vector2(0.5f, 1f), new Vector2(160f, 838f));
                left.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.04f);

                var right = UIManager.GenerateUIImage("Right", localRT);
                UIManager.SetRectTransform(right.GetObject<RectTransform>(), new Vector2(880f, 300f), ZeroFive, ZeroFive, new Vector2(0.5f, 1f), new Vector2(160f, 838f));
                right.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.04f);

                var regularContent = new GameObject("Regular Content");
                regularContent.transform.SetParent(localRT);
                regularContent.transform.localScale = Vector3.one;

                var regularContentRT = regularContent.AddComponent<RectTransform>();
                regularContentRT.anchoredPosition = Vector2.zero;
                regularContentRT.sizeDelta = Vector3.zero;

                RegularContents.Add(regularContentRT);

                // Prefab
                {
                    var level = UIManager.GenerateUIImage("Level", transform);
                    UIManager.SetRectTransform(level.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, new Vector2(300f, 180f));
                    levelPrefab = level.GetObject<GameObject>();
                    levelPrefab.AddComponent<Mask>();

                    var clickable = levelPrefab.AddComponent<Clickable>();

                    var levelDifficulty = UIManager.GenerateUIImage("Difficulty", level.GetObject<RectTransform>());

                    var levelTitle = UIManager.GenerateUITextMeshPro("Title", level.GetObject<RectTransform>());

                    levelTitle.GetObject<TextMeshProUGUI>().text = "BRO";

                    var levelIconBase = UIManager.GenerateUIImage("Icon Base", level.GetObject<RectTransform>());

                    levelIconBase.GetObject<GameObject>().AddComponent<Mask>();

                    var levelIcon = UIManager.GenerateUIImage("Icon", levelIconBase.GetObject<RectTransform>());

                    levelIcon.GetObject<Image>().sprite = SteamWorkshop.inst.defaultSteamImageSprite;

                    var levelRankShadow = UIManager.GenerateUITextMeshPro("Rank Shadow", level.GetObject<RectTransform>());

                    levelRankShadow.GetObject<TextMeshProUGUI>().text = "F";

                    var levelRank = UIManager.GenerateUITextMeshPro("Rank", level.GetObject<RectTransform>());

                    levelRank.GetObject<TextMeshProUGUI>().text = "F";

                    var shine = ArcadeHelper.buttonPrefab.transform.Find("shine").gameObject
                        .Duplicate(level.GetObject<RectTransform>(), "Shine");

                    var shineController = shine.GetComponent<ShineController>();
                    shineController.maxDelay = 1f;
                    shineController.minDelay = 0.2f;
                    shineController.offset = 260f;
                    shineController.offsetOverShoot = 32f;
                    shineController.speed = 0.7f;
                    shine.transform.AsRT().sizeDelta = new Vector2(300f, 24f);
                    shine.transform.GetChild(0).AsRT().sizeDelta = new Vector2(300f, 8f);
                }

                var searchField = UIManager.GenerateUIInputField("Search", localRT);

                UIManager.SetRectTransform(searchField.GetObject<RectTransform>(), new Vector2(0f, 270f), ZeroFive, ZeroFive, ZeroFive, new Vector2(1600f, 60f));

                if (ArcadeConfig.Instance.MiscRounded.Value)
                    SpriteManager.SetRoundedSprite(searchField.GetObject<Image>(), 1, SpriteManager.RoundedSide.Bottom);
                else
                    searchField.GetObject<Image>().sprite = null;

                localSearchFieldImage = searchField.GetObject<Image>();

                searchField.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.black, 0.2f);

                ((Text)searchField["Placeholder"]).alignment = TextAnchor.MiddleLeft;
                ((Text)searchField["Placeholder"]).text = "Search for level...";
                ((Text)searchField["Placeholder"]).color = LSColors.fadeColor(textColor, 0.2f);
                localSearchField = searchField.GetObject<InputField>();
                localSearchField.onValueChanged.ClearAll();
                localSearchField.textComponent.alignment = TextAnchor.MiddleLeft;
                localSearchField.textComponent.color = textColor;
                localSearchField.onValueChanged.AddListener(delegate (string _val)
                {
                    LocalSearchTerm = _val;
                });
            }

            // Online
            {
                var online = new GameObject("Online");
                online.transform.SetParent(selectionBaseRT);
                online.transform.localScale = Vector3.one;

                var onlineRT = online.AddComponent<RectTransform>();
                onlineRT.anchoredPosition = Vector3.zero;
                onlineRT.sizeDelta = new Vector2(0f, 0f);

                RegularBases.Add(onlineRT);

                // Settings
                {
                    var localSettingsBar = UIManager.GenerateUIImage("Settings Bar", onlineRT);

                    UIManager.SetRectTransform(localSettingsBar.GetObject<RectTransform>(), new Vector2(0f, 360f), ZeroFive, ZeroFive, ZeroFive, new Vector2(1920f, 120f));

                    localSettingsBar.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.01f);

                    var prevPage = UIManager.GenerateUIImage("Previous", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(prevPage.GetObject<RectTransform>(), new Vector2(500f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(80f, 64f));

                    var prevPageClickable = prevPage.GetObject<GameObject>().AddComponent<Clickable>();

                    prevPage.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                    if (ArcadeConfig.Instance.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(prevPage.GetObject<Image>(), ArcadeConfig.Instance.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        prevPage.GetObject<Image>().sprite = null;

                    var prevPageText = UIManager.GenerateUITextMeshPro("Text", prevPage.GetObject<RectTransform>());
                    UIManager.SetRectTransform(prevPageText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    prevPageText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    prevPageText.GetObject<TextMeshProUGUI>().fontSize = 64;
                    prevPageText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                    prevPageText.GetObject<TextMeshProUGUI>().text = "<";

                    Settings[1].Add(new Tab
                    {
                        GameObject = prevPage.GetObject<GameObject>(),
                        RectTransform = prevPage.GetObject<RectTransform>(),
                        Clickable = prevPageClickable,
                        Image = prevPage.GetObject<Image>(),
                        Text = prevPageText.GetObject<TextMeshProUGUI>(),
                        Position = new Vector2Int(0, 1),
                    });

                    var pageField = UIManager.GenerateUIInputField("Page", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(pageField.GetObject<RectTransform>(), new Vector2(650f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(150f, 64f));
                    pageField.GetObject<Image>().color = CoreHelper.InvertColorHue(CoreHelper.InvertColorValue(Color.Lerp(buttonBGColor, Color.black, 0.2f)));

                    ((Text)pageField["Placeholder"]).alignment = TextAnchor.MiddleCenter;
                    ((Text)pageField["Placeholder"]).text = "Page...";
                    ((Text)pageField["Placeholder"]).color = LSColors.fadeColor(CoreHelper.InvertColorHue(CoreHelper.InvertColorValue(textColor)), 0.2f);
                    onlinePageField = pageField.GetObject<InputField>();
                    onlinePageField.onValueChanged.ClearAll();
                    onlinePageField.textComponent.alignment = TextAnchor.MiddleCenter;
                    onlinePageField.textComponent.fontSize = 30;
                    onlinePageField.textComponent.color = CoreHelper.InvertColorHue(CoreHelper.InvertColorValue(textColor));
                    onlinePageField.text = DataManager.inst.GetSettingInt("CurrentArcadePage", 0).ToString();

                    if (ArcadeConfig.Instance.PageFieldRoundness.Value != 0)
                        SpriteManager.SetRoundedSprite(onlinePageField.image, ArcadeConfig.Instance.PageFieldRoundness.Value, SpriteManager.RoundedSide.W);
                    else
                        onlinePageField.image.sprite = null;

                    var nextPage = UIManager.GenerateUIImage("Next", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(nextPage.GetObject<RectTransform>(), new Vector2(800f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(80f, 64f));

                    var nextPageClickable = nextPage.GetObject<GameObject>().AddComponent<Clickable>();

                    nextPage.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                    if (ArcadeConfig.Instance.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(nextPage.GetObject<Image>(), ArcadeConfig.Instance.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        nextPage.GetObject<Image>().sprite = null;

                    var nextPageText = UIManager.GenerateUITextMeshPro("Text", nextPage.GetObject<RectTransform>());
                    UIManager.SetRectTransform(nextPageText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    nextPageText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    nextPageText.GetObject<TextMeshProUGUI>().fontSize = 64;
                    nextPageText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                    nextPageText.GetObject<TextMeshProUGUI>().text = ">";

                    Settings[1].Add(new Tab
                    {
                        GameObject = nextPage.GetObject<GameObject>(),
                        RectTransform = nextPage.GetObject<RectTransform>(),
                        Clickable = nextPageClickable,
                        Image = nextPage.GetObject<Image>(),
                        Text = nextPageText.GetObject<TextMeshProUGUI>(),
                        Position = new Vector2Int(1, 1),
                    });

                    onlinePageField.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (CanSelect && int.TryParse(_val, out int p))
                        {
                            p = Mathf.Clamp(p, 0, OnlineLevelCount);
                            SetOnlineLevelsPage(p);

                            DataManager.inst.UpdateSettingInt("CurrentArcadePage", p);
                        }
                    });

                    prevPageClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(0, 1);
                    };
                    prevPageClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        if (int.TryParse(onlinePageField.text, out int p))
                        {
                            if (p > 0)
                            {
                                AudioManager.inst.PlaySound("blip");
                                onlinePageField.text = Mathf.Clamp(p - 1, 0, OnlineLevelCount).ToString();
                            }
                            else
                            {
                                AudioManager.inst.PlaySound("Block");
                            }
                        }
                    };

                    nextPageClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(1, 1);
                    };
                    nextPageClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        if (int.TryParse(onlinePageField.text, out int p))
                        {
                            if (p < OnlineLevelCount)
                            {
                                AudioManager.inst.PlaySound("blip");
                                onlinePageField.text = Mathf.Clamp(p + 1, 0, OnlineLevelCount).ToString();
                            }
                            else
                            {
                                AudioManager.inst.PlaySound("Block");
                            }
                        }
                    };
                }

                var left = UIManager.GenerateUIImage("Left", onlineRT);
                UIManager.SetRectTransform(left.GetObject<RectTransform>(), new Vector2(-880f, 300f), ZeroFive, ZeroFive, new Vector2(0.5f, 1f), new Vector2(160f, 838f));
                left.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.04f);

                var right = UIManager.GenerateUIImage("Right", onlineRT);
                UIManager.SetRectTransform(right.GetObject<RectTransform>(), new Vector2(880f, 300f), ZeroFive, ZeroFive, new Vector2(0.5f, 1f), new Vector2(160f, 838f));
                right.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.04f);

                var regularContent = new GameObject("Regular Content");
                regularContent.transform.SetParent(onlineRT);
                regularContent.transform.localScale = Vector3.one;

                var regularContentRT = regularContent.AddComponent<RectTransform>();
                regularContentRT.anchoredPosition = Vector2.zero;
                regularContentRT.sizeDelta = Vector3.zero;

                RegularContents.Add(regularContentRT);

                var searchField = UIManager.GenerateUIInputField("Search", onlineRT);

                UIManager.SetRectTransform(searchField.GetObject<RectTransform>(), new Vector2(-100f, 270f), ZeroFive, ZeroFive, ZeroFive, new Vector2(1400f, 60f));

                if (ArcadeConfig.Instance.MiscRounded.Value)
                    SpriteManager.SetRoundedSprite(searchField.GetObject<Image>(), 1, SpriteManager.RoundedSide.Bottom);
                else
                    searchField.GetObject<Image>().sprite = null;

                onlineSearchFieldImage = searchField.GetObject<Image>();

                searchField.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.black, 0.2f);

                ((Text)searchField["Placeholder"]).alignment = TextAnchor.MiddleLeft;
                ((Text)searchField["Placeholder"]).text = "Search for level...";
                ((Text)searchField["Placeholder"]).color = LSColors.fadeColor(textColor, 0.2f);
                onlineSearchField = searchField.GetObject<InputField>();
                onlineSearchField.onValueChanged.ClearAll();
                onlineSearchField.textComponent.alignment = TextAnchor.MiddleLeft;
                onlineSearchField.textComponent.color = textColor;
                onlineSearchField.onValueChanged.AddListener(delegate (string _val)
                {
                    OnlineSearchTerm = _val;
                });

                var reload = UIManager.GenerateUIImage("Reload", onlineRT);
                UIManager.SetRectTransform(reload.GetObject<RectTransform>(), new Vector2(700f, 270f), ZeroFive, ZeroFive, ZeroFive, new Vector2(200f, 60f));

                var reloadClickable = reload.GetObject<GameObject>().AddComponent<Clickable>();

                reload.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                if (ArcadeConfig.Instance.TabsRoundedness.Value != 0)
                    SpriteManager.SetRoundedSprite(reload.GetObject<Image>(), ArcadeConfig.Instance.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                else
                    reload.GetObject<Image>().sprite = null;

                var reloadText = UIManager.GenerateUITextMeshPro("Text", reload.GetObject<RectTransform>());
                UIManager.SetRectTransform(reloadText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                reloadText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                reloadText.GetObject<TextMeshProUGUI>().fontSize = 32;
                reloadText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                reloadText.GetObject<TextMeshProUGUI>().text = "[SEARCH]";

                reloadClickable.onEnter = delegate (PointerEventData pointerEventData)
                {
                    if (!CanSelect)
                        return;

                    AudioManager.inst.PlaySound("LeftRight");
                    selected = new Vector2Int(0, 2);
                };
                reloadClickable.onClick = delegate (PointerEventData pointerEventData)
                {
                    AudioManager.inst.PlaySound("blip");
                    StartCoroutine(SearchOnlineLevels());
                };

                Settings[1].Add(new Tab
                {
                    GameObject = reload.GetObject<GameObject>(),
                    RectTransform = reload.GetObject<RectTransform>(),
                    Clickable = reloadClickable,
                    Image = reload.GetObject<Image>(),
                    Text = reloadText.GetObject<TextMeshProUGUI>(),
                    Position = new Vector2Int(0, 2),
                });

            }

            // Browser
            {
                var browser = new GameObject("Browser");
                browser.transform.SetParent(selectionBaseRT);
                browser.transform.localScale = Vector3.one;

                var browserRT = browser.AddComponent<RectTransform>();
                browserRT.anchoredPosition = Vector3.zero;
                browserRT.sizeDelta = new Vector2(0f, 0f);

                RegularBases.Add(browserRT);

                // Settings
                {
                    var localSettingsBar = UIManager.GenerateUIImage("Settings Bar", browserRT);

                    UIManager.SetRectTransform(localSettingsBar.GetObject<RectTransform>(), new Vector2(0f, 360f), ZeroFive, ZeroFive, ZeroFive, new Vector2(1920f, 120f));

                    localSettingsBar.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.01f);

                    var reload = UIManager.GenerateUIImage("Select", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(reload.GetObject<RectTransform>(), new Vector2(-600f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(400f, 64f));

                    var reloadClickable = reload.GetObject<GameObject>().AddComponent<Clickable>();

                    reload.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                    if (ArcadeConfig.Instance.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(reload.GetObject<Image>(), ArcadeConfig.Instance.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        reload.GetObject<Image>().sprite = null;

                    var reloadText = UIManager.GenerateUITextMeshPro("Text", reload.GetObject<RectTransform>());
                    UIManager.SetRectTransform(reloadText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    reloadText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    reloadText.GetObject<TextMeshProUGUI>().fontSize = 32;
                    reloadText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                    reloadText.GetObject<TextMeshProUGUI>().text = "[USE LOCAL BROWSER]";

                    Settings[2].Add(new Tab
                    {
                        GameObject = reload.GetObject<GameObject>(),
                        RectTransform = reload.GetObject<RectTransform>(),
                        Clickable = reloadClickable,
                        Image = reload.GetObject<Image>(),
                        Text = reloadText.GetObject<TextMeshProUGUI>(),
                        Position = new Vector2Int(0, 1),
                    });

                    reloadClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(0, 1);
                    };
                    reloadClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        AudioManager.inst.PlaySound("blip");
                        OpenLocalBrowser();
                    };
                }

                RegularContents.Add(null);
            }

            // Download
            {
                var download = new GameObject("Download");
                download.transform.SetParent(selectionBaseRT);
                download.transform.localScale = Vector3.one;

                var downloadRT = download.AddComponent<RectTransform>();
                downloadRT.anchoredPosition = Vector3.zero;
                downloadRT.sizeDelta = new Vector2(0f, 0f);

                RegularBases.Add(downloadRT);

                // Settings
                {
                    var localSettingsBar = UIManager.GenerateUIImage("Settings Bar", downloadRT);

                    UIManager.SetRectTransform(localSettingsBar.GetObject<RectTransform>(), new Vector2(0f, 360f), ZeroFive, ZeroFive, ZeroFive, new Vector2(1920f, 120f));

                    localSettingsBar.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.01f);

                    var pageField = UIManager.GenerateUIInputField("URL", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(pageField.GetObject<RectTransform>(), new Vector2(-300f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(1250f, 64f));
                    pageField.GetObject<Image>().color = CoreHelper.InvertColorHue(CoreHelper.InvertColorValue(Color.Lerp(buttonBGColor, Color.black, 0.2f)));

                    ((Text)pageField["Placeholder"]).alignment = TextAnchor.MiddleLeft;
                    ((Text)pageField["Placeholder"]).text = "Set URL...";
                    ((Text)pageField["Placeholder"]).color = LSColors.fadeColor(CoreHelper.InvertColorHue(CoreHelper.InvertColorValue(textColor)), 0.2f);
                    downloadField = pageField.GetObject<InputField>();
                    downloadField.onValueChanged.ClearAll();
                    downloadField.textComponent.alignment = TextAnchor.MiddleLeft;
                    downloadField.textComponent.fontSize = 30;
                    downloadField.textComponent.color = CoreHelper.InvertColorHue(CoreHelper.InvertColorValue(textColor));
                    downloadField.text = "";

                    if (ArcadeConfig.Instance.MiscRounded.Value)
                        SpriteManager.SetRoundedSprite(downloadField.image, 1, SpriteManager.RoundedSide.W);
                    else
                        downloadField.image.sprite = null;

                    var reload = UIManager.GenerateUIImage("Download", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(reload.GetObject<RectTransform>(), new Vector2(600f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(400f, 64f));

                    var reloadClickable = reload.GetObject<GameObject>().AddComponent<Clickable>();

                    reload.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                    if (ArcadeConfig.Instance.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(reload.GetObject<Image>(), ArcadeConfig.Instance.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        reload.GetObject<Image>().sprite = null;

                    var reloadText = UIManager.GenerateUITextMeshPro("Text", reload.GetObject<RectTransform>());
                    UIManager.SetRectTransform(reloadText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    reloadText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    reloadText.GetObject<TextMeshProUGUI>().fontSize = 32;
                    reloadText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                    reloadText.GetObject<TextMeshProUGUI>().text = "[DOWNLOAD]";

                    Settings[3].Add(new Tab
                    {
                        GameObject = reload.GetObject<GameObject>(),
                        RectTransform = reload.GetObject<RectTransform>(),
                        Clickable = reloadClickable,
                        Image = reload.GetObject<Image>(),
                        Text = reloadText.GetObject<TextMeshProUGUI>(),
                        Position = new Vector2Int(0, 1),
                    });

                    reloadClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(0, 1);
                    };
                    reloadClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        AudioManager.inst.PlaySound("blip");
                        DownloadLevel();
                    };
                }

                RegularContents.Add(null);
            }

            // Queue
            {
                var queue = new GameObject("Queue");
                queue.transform.SetParent(selectionBaseRT);
                queue.transform.localScale = Vector3.one;

                var queueRT = queue.AddComponent<RectTransform>();
                queueRT.anchoredPosition = Vector3.zero;
                queueRT.sizeDelta = new Vector2(0f, 0f);

                RegularBases.Add(queueRT);
                // Settings
                {
                    var localSettingsBar = UIManager.GenerateUIImage("Settings Bar", queueRT);

                    UIManager.SetRectTransform(localSettingsBar.GetObject<RectTransform>(), new Vector2(0f, 360f), ZeroFive, ZeroFive, ZeroFive, new Vector2(1920f, 120f));

                    localSettingsBar.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.01f);

                    var reload = UIManager.GenerateUIImage("Shuffle", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(reload.GetObject<RectTransform>(), new Vector2(-760f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(360f, 64f));

                    var reloadClickable = reload.GetObject<GameObject>().AddComponent<Clickable>();

                    reload.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                    if (ArcadeConfig.Instance.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(reload.GetObject<Image>(), ArcadeConfig.Instance.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        reload.GetObject<Image>().sprite = null;

                    var reloadText = UIManager.GenerateUITextMeshPro("Text", reload.GetObject<RectTransform>());
                    UIManager.SetRectTransform(reloadText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    reloadText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    reloadText.GetObject<TextMeshProUGUI>().fontSize = 32;
                    reloadText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                    reloadText.GetObject<TextMeshProUGUI>().text = "[SHUFFLE & PLAY]";

                    reloadClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(0, 1);
                    };
                    reloadClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        AudioManager.inst.PlaySound("blip");
                        ShuffleQueue(true);
                    };

                    Settings[4].Add(new Tab
                    {
                        GameObject = reload.GetObject<GameObject>(),
                        RectTransform = reload.GetObject<RectTransform>(),
                        Clickable = reloadClickable,
                        Image = reload.GetObject<Image>(),
                        Text = reloadText.GetObject<TextMeshProUGUI>(),
                        Position = new Vector2Int(0, 1),
                    });

                    var shuffle = UIManager.GenerateUIImage("Shuffle", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(shuffle.GetObject<RectTransform>(), new Vector2(-460f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(200f, 64f));

                    var shuffleClickable = shuffle.GetObject<GameObject>().AddComponent<Clickable>();

                    shuffle.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                    if (ArcadeConfig.Instance.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(shuffle.GetObject<Image>(), ArcadeConfig.Instance.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        shuffle.GetObject<Image>().sprite = null;

                    var shuffleText = UIManager.GenerateUITextMeshPro("Text", shuffle.GetObject<RectTransform>());
                    UIManager.SetRectTransform(shuffleText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    shuffleText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    shuffleText.GetObject<TextMeshProUGUI>().fontSize = 32;
                    shuffleText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                    shuffleText.GetObject<TextMeshProUGUI>().text = "[SHUFFLE]";

                    shuffleClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(1, 1);
                    };
                    shuffleClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        AudioManager.inst.PlaySound("blip");
                        ShuffleQueue(false);
                    };

                    Settings[4].Add(new Tab
                    {
                        GameObject = shuffle.GetObject<GameObject>(),
                        RectTransform = shuffle.GetObject<RectTransform>(),
                        Clickable = shuffleClickable,
                        Image = shuffle.GetObject<Image>(),
                        Text = shuffleText.GetObject<TextMeshProUGUI>(),
                        Position = new Vector2Int(1, 1),
                    });

                    var play = UIManager.GenerateUIImage("Play", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(play.GetObject<RectTransform>(), new Vector2(-240f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(200f, 64f));

                    var playClickable = play.GetObject<GameObject>().AddComponent<Clickable>();

                    play.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                    if (ArcadeConfig.Instance.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(play.GetObject<Image>(), ArcadeConfig.Instance.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        play.GetObject<Image>().sprite = null;

                    var playText = UIManager.GenerateUITextMeshPro("Text", play.GetObject<RectTransform>());
                    UIManager.SetRectTransform(playText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    playText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    playText.GetObject<TextMeshProUGUI>().fontSize = 32;
                    playText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                    playText.GetObject<TextMeshProUGUI>().text = "[PLAY]";

                    playClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(2, 1);
                    };
                    playClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        if (LevelManager.ArcadeQueue.Count < 1)
                        {
                            CoreHelper.LogError($"Arcade Queue does not contain any levels!");
                            return;
                        }

                        AudioManager.inst.PlaySound("blip");
                        if (ArcadeConfig.Instance.QueuePlaysLevel.Value)
                        {
                            menuUI.SetActive(false);
                            LevelManager.OnLevelEnd = ArcadeHelper.EndOfLevel;
                            CoreHelper.StartCoroutine(LevelManager.Play(LevelManager.ArcadeQueue[0]));
                        }
                        else
                        {
                            StartCoroutine(SelectLocalLevel(LevelManager.ArcadeQueue[0]));
                        }
                    };

                    Settings[4].Add(new Tab
                    {
                        GameObject = play.GetObject<GameObject>(),
                        RectTransform = play.GetObject<RectTransform>(),
                        Clickable = playClickable,
                        Image = play.GetObject<Image>(),
                        Text = playText.GetObject<TextMeshProUGUI>(),
                        Position = new Vector2Int(2, 1),
                    });

                    var clear = UIManager.GenerateUIImage("Clear", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(clear.GetObject<RectTransform>(), new Vector2(-20f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(200f, 64f));

                    var clearClickable = clear.GetObject<GameObject>().AddComponent<Clickable>();

                    clear.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                    if (ArcadeConfig.Instance.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(clear.GetObject<Image>(), ArcadeConfig.Instance.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        clear.GetObject<Image>().sprite = null;

                    var clearText = UIManager.GenerateUITextMeshPro("Text", clear.GetObject<RectTransform>());
                    UIManager.SetRectTransform(clearText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    clearText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    clearText.GetObject<TextMeshProUGUI>().fontSize = 32;
                    clearText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                    clearText.GetObject<TextMeshProUGUI>().text = "[CLEAR]";

                    clearClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(3, 1);
                    };
                    clearClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        AudioManager.inst.PlaySound("blip");
                        LevelManager.ArcadeQueue.Clear();
                        StartCoroutine(RefreshQueuedLevels());
                    };

                    Settings[4].Add(new Tab
                    {
                        GameObject = clear.GetObject<GameObject>(),
                        RectTransform = clear.GetObject<RectTransform>(),
                        Clickable = clearClickable,
                        Image = clear.GetObject<Image>(),
                        Text = clearText.GetObject<TextMeshProUGUI>(),
                        Position = new Vector2Int(3, 1),
                    });

                    var copy = UIManager.GenerateUIImage("Copy", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(copy.GetObject<RectTransform>(), new Vector2(170f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(160f, 64f));

                    var copyClickable = copy.GetObject<GameObject>().AddComponent<Clickable>();

                    copy.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                    if (ArcadeConfig.Instance.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(copy.GetObject<Image>(), ArcadeConfig.Instance.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        copy.GetObject<Image>().sprite = null;

                    var copyText = UIManager.GenerateUITextMeshPro("Text", copy.GetObject<RectTransform>());
                    UIManager.SetRectTransform(copyText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    copyText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    copyText.GetObject<TextMeshProUGUI>().fontSize = 32;
                    copyText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                    copyText.GetObject<TextMeshProUGUI>().text = "[COPY]";

                    copyClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(4, 1);
                    };
                    copyClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        AudioManager.inst.PlaySound("blip");
                        RTArcade.CopyArcadeQueue();
                    };

                    Settings[4].Add(new Tab
                    {
                        GameObject = copy.GetObject<GameObject>(),
                        RectTransform = copy.GetObject<RectTransform>(),
                        Clickable = copyClickable,
                        Image = copy.GetObject<Image>(),
                        Text = copyText.GetObject<TextMeshProUGUI>(),
                        Position = new Vector2Int(4, 1),
                    });

                    var paste = UIManager.GenerateUIImage("Paste", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(paste.GetObject<RectTransform>(), new Vector2(350f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(180f, 64f));

                    var pasteClickable = paste.GetObject<GameObject>().AddComponent<Clickable>();

                    paste.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                    if (ArcadeConfig.Instance.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(paste.GetObject<Image>(), ArcadeConfig.Instance.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        paste.GetObject<Image>().sprite = null;

                    var pasteText = UIManager.GenerateUITextMeshPro("Text", paste.GetObject<RectTransform>());
                    UIManager.SetRectTransform(pasteText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    pasteText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    pasteText.GetObject<TextMeshProUGUI>().fontSize = 32;
                    pasteText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                    pasteText.GetObject<TextMeshProUGUI>().text = "[PASTE]";

                    pasteClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(5, 1);
                    };
                    pasteClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        AudioManager.inst.PlaySound("blip");
                        RTArcade.PasteArcadeQueue();
                    };

                    Settings[4].Add(new Tab
                    {
                        GameObject = paste.GetObject<GameObject>(),
                        RectTransform = paste.GetObject<RectTransform>(),
                        Clickable = pasteClickable,
                        Image = paste.GetObject<Image>(),
                        Text = pasteText.GetObject<TextMeshProUGUI>(),
                        Position = new Vector2Int(5, 1),
                    });

                    var prevPage = UIManager.GenerateUIImage("Previous", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(prevPage.GetObject<RectTransform>(), new Vector2(500f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(80f, 64f));

                    var prevPageClickable = prevPage.GetObject<GameObject>().AddComponent<Clickable>();

                    prevPage.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                    if (ArcadeConfig.Instance.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(prevPage.GetObject<Image>(), ArcadeConfig.Instance.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        prevPage.GetObject<Image>().sprite = null;

                    var prevPageText = UIManager.GenerateUITextMeshPro("Text", prevPage.GetObject<RectTransform>());
                    UIManager.SetRectTransform(prevPageText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    prevPageText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    prevPageText.GetObject<TextMeshProUGUI>().fontSize = 64;
                    prevPageText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                    prevPageText.GetObject<TextMeshProUGUI>().text = "<";

                    Settings[4].Add(new Tab
                    {
                        GameObject = prevPage.GetObject<GameObject>(),
                        RectTransform = prevPage.GetObject<RectTransform>(),
                        Clickable = prevPageClickable,
                        Image = prevPage.GetObject<Image>(),
                        Text = prevPageText.GetObject<TextMeshProUGUI>(),
                        Position = new Vector2Int(6, 1),
                    });

                    var pageField = UIManager.GenerateUIInputField("Page", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(pageField.GetObject<RectTransform>(), new Vector2(650f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(150f, 64f));
                    pageField.GetObject<Image>().color = CoreHelper.InvertColorHue(CoreHelper.InvertColorValue(Color.Lerp(buttonBGColor, Color.black, 0.2f)));

                    ((Text)pageField["Placeholder"]).alignment = TextAnchor.MiddleCenter;
                    ((Text)pageField["Placeholder"]).text = "Page...";
                    ((Text)pageField["Placeholder"]).color = LSColors.fadeColor(CoreHelper.InvertColorHue(CoreHelper.InvertColorValue(textColor)), 0.2f);
                    queuePageField = pageField.GetObject<InputField>();
                    queuePageField.onValueChanged.ClearAll();
                    queuePageField.textComponent.alignment = TextAnchor.MiddleCenter;
                    queuePageField.textComponent.fontSize = 30;
                    queuePageField.textComponent.color = CoreHelper.InvertColorHue(CoreHelper.InvertColorValue(textColor));
                    queuePageField.text = DataManager.inst.GetSettingInt("CurrentArcadePage", 0).ToString();

                    if (ArcadeConfig.Instance.PageFieldRoundness.Value != 0)
                        SpriteManager.SetRoundedSprite(queuePageField.image, ArcadeConfig.Instance.PageFieldRoundness.Value, SpriteManager.RoundedSide.W);
                    else
                        queuePageField.image.sprite = null;

                    var nextPage = UIManager.GenerateUIImage("Next", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(nextPage.GetObject<RectTransform>(), new Vector2(800f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(80f, 64f));

                    var nextPageClickable = nextPage.GetObject<GameObject>().AddComponent<Clickable>();

                    nextPage.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                    if (ArcadeConfig.Instance.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(nextPage.GetObject<Image>(), ArcadeConfig.Instance.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        nextPage.GetObject<Image>().sprite = null;

                    var nextPageText = UIManager.GenerateUITextMeshPro("Text", nextPage.GetObject<RectTransform>());
                    UIManager.SetRectTransform(nextPageText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    nextPageText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    nextPageText.GetObject<TextMeshProUGUI>().fontSize = 64;
                    nextPageText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                    nextPageText.GetObject<TextMeshProUGUI>().text = ">";

                    Settings[4].Add(new Tab
                    {
                        GameObject = nextPage.GetObject<GameObject>(),
                        RectTransform = nextPage.GetObject<RectTransform>(),
                        Clickable = nextPageClickable,
                        Image = nextPage.GetObject<Image>(),
                        Text = nextPageText.GetObject<TextMeshProUGUI>(),
                        Position = new Vector2Int(7, 1),
                    });

                    queuePageField.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (int.TryParse(_val, out int p))
                        {
                            p = Mathf.Clamp(p, 0, QueuePageCount);
                            SetQueueLevelsPage(p);

                            DataManager.inst.UpdateSettingInt("CurrentArcadePage", p);
                        }
                    });

                    prevPageClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(6, 1);
                    };
                    prevPageClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        if (int.TryParse(queuePageField.text, out int p))
                        {
                            if (p > 0)
                            {
                                AudioManager.inst.PlaySound("blip");
                                queuePageField.text = Mathf.Clamp(p - 1, 0, QueuePageCount).ToString();
                            }
                            else
                            {
                                AudioManager.inst.PlaySound("Block");
                            }
                        }
                    };

                    nextPageClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(7, 1);
                    };
                    nextPageClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        if (int.TryParse(queuePageField.text, out int p))
                        {
                            if (p < QueuePageCount)
                            {
                                AudioManager.inst.PlaySound("blip");
                                queuePageField.text = Mathf.Clamp(p + 1, 0, QueuePageCount).ToString();
                            }
                            else
                            {
                                AudioManager.inst.PlaySound("Block");
                            }
                        }
                    };
                }

                var left = UIManager.GenerateUIImage("Left", queueRT);
                UIManager.SetRectTransform(left.GetObject<RectTransform>(), new Vector2(-880f, 300f), ZeroFive, ZeroFive, new Vector2(0.5f, 1f), new Vector2(160f, 838f));
                left.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.04f);

                var right = UIManager.GenerateUIImage("Right", queueRT);
                UIManager.SetRectTransform(right.GetObject<RectTransform>(), new Vector2(880f, 300f), ZeroFive, ZeroFive, new Vector2(0.5f, 1f), new Vector2(160f, 838f));
                right.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.04f);

                var regularContent = new GameObject("Regular Content");
                regularContent.transform.SetParent(queueRT);
                regularContent.transform.localScale = Vector3.one;

                var regularContentRT = regularContent.AddComponent<RectTransform>();
                regularContentRT.anchoredPosition = Vector2.zero;
                regularContentRT.sizeDelta = Vector3.zero;

                RegularContents.Add(regularContentRT);

                var searchField = UIManager.GenerateUIInputField("Search", queueRT);

                UIManager.SetRectTransform(searchField.GetObject<RectTransform>(), new Vector2(0f, 270f), ZeroFive, ZeroFive, ZeroFive, new Vector2(1600f, 60f));

                if (ArcadeConfig.Instance.MiscRounded.Value)
                    SpriteManager.SetRoundedSprite(searchField.GetObject<Image>(), 1, SpriteManager.RoundedSide.Bottom);
                else
                    searchField.GetObject<Image>().sprite = null;

                queueSearchFieldImage = searchField.GetObject<Image>();

                searchField.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.black, 0.2f);

                ((Text)searchField["Placeholder"]).alignment = TextAnchor.MiddleLeft;
                ((Text)searchField["Placeholder"]).text = "Search for level...";
                ((Text)searchField["Placeholder"]).color = LSColors.fadeColor(textColor, 0.2f);
                queueSearchField = searchField.GetObject<InputField>();
                queueSearchField.onValueChanged.ClearAll();
                queueSearchField.textComponent.alignment = TextAnchor.MiddleLeft;
                queueSearchField.textComponent.color = textColor;
                queueSearchField.onValueChanged.AddListener(delegate (string _val)
                {
                    QueueSearchTerm = _val;
                });
            }

            // Steam
            {
                var steam = new GameObject("Steam");
                steam.transform.SetParent(selectionBaseRT);
                steam.transform.localScale = Vector3.one;

                var steamRT = steam.AddComponent<RectTransform>();
                steamRT.anchoredPosition = Vector3.zero;
                steamRT.sizeDelta = new Vector2(0f, 0f);

                RegularBases.Add(steamRT);

                // Settings
                {
                    var localSettingsBar = UIManager.GenerateUIImage("Settings Bar", steamRT);

                    UIManager.SetRectTransform(localSettingsBar.GetObject<RectTransform>(), new Vector2(0f, 360f), ZeroFive, ZeroFive, ZeroFive, new Vector2(1920f, 120f));

                    localSettingsBar.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.01f);

                    var reload = UIManager.GenerateUIImage("Reload", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(reload.GetObject<RectTransform>(), new Vector2(-600f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(200f, 64f));

                    var reloadClickable = reload.GetObject<GameObject>().AddComponent<Clickable>();

                    reload.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                    if (ArcadeConfig.Instance.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(reload.GetObject<Image>(), ArcadeConfig.Instance.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        reload.GetObject<Image>().sprite = null;

                    var reloadText = UIManager.GenerateUITextMeshPro("Text", reload.GetObject<RectTransform>());
                    UIManager.SetRectTransform(reloadText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    reloadText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    reloadText.GetObject<TextMeshProUGUI>().fontSize = 32;
                    reloadText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                    reloadText.GetObject<TextMeshProUGUI>().text = "[RELOAD]";

                    Settings[5].Add(new Tab
                    {
                        GameObject = reload.GetObject<GameObject>(),
                        RectTransform = reload.GetObject<RectTransform>(),
                        Clickable = reloadClickable,
                        Image = reload.GetObject<Image>(),
                        Text = reloadText.GetObject<TextMeshProUGUI>(),
                        Position = new Vector2Int(0, 1),
                    });

                    var viewType = UIManager.GenerateUIImage("View Type", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(viewType.GetObject<RectTransform>(), new Vector2(-350f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(230f, 64f));

                    var viewTypeClickable = viewType.GetObject<GameObject>().AddComponent<Clickable>();

                    viewType.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                    if (ArcadeConfig.Instance.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(viewType.GetObject<Image>(), ArcadeConfig.Instance.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        viewType.GetObject<Image>().sprite = null;

                    var viewTypeText = UIManager.GenerateUITextMeshPro("Text", viewType.GetObject<RectTransform>());
                    UIManager.SetRectTransform(viewTypeText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    viewTypeText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    viewTypeText.GetObject<TextMeshProUGUI>().fontSize = 32;
                    viewTypeText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                    viewTypeText.GetObject<TextMeshProUGUI>().text = "[SUBSCRIBED]";

                    Settings[5].Add(new Tab
                    {
                        GameObject = viewType.GetObject<GameObject>(),
                        RectTransform = viewType.GetObject<RectTransform>(),
                        Clickable = viewTypeClickable,
                        Image = viewType.GetObject<Image>(),
                        Text = viewTypeText.GetObject<TextMeshProUGUI>(),
                        Position = new Vector2Int(1, 1),
                    });

                    var prevPage = UIManager.GenerateUIImage("Previous", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(prevPage.GetObject<RectTransform>(), new Vector2(500f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(80f, 64f));

                    var prevPageClickable = prevPage.GetObject<GameObject>().AddComponent<Clickable>();

                    prevPage.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                    if (ArcadeConfig.Instance.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(prevPage.GetObject<Image>(), ArcadeConfig.Instance.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        prevPage.GetObject<Image>().sprite = null;

                    var prevPageText = UIManager.GenerateUITextMeshPro("Text", prevPage.GetObject<RectTransform>());
                    UIManager.SetRectTransform(prevPageText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    prevPageText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    prevPageText.GetObject<TextMeshProUGUI>().fontSize = 64;
                    prevPageText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                    prevPageText.GetObject<TextMeshProUGUI>().text = "<";

                    Settings[5].Add(new Tab
                    {
                        GameObject = prevPage.GetObject<GameObject>(),
                        RectTransform = prevPage.GetObject<RectTransform>(),
                        Clickable = prevPageClickable,
                        Image = prevPage.GetObject<Image>(),
                        Text = prevPageText.GetObject<TextMeshProUGUI>(),
                        Position = new Vector2Int(2, 1),
                    });

                    var pageField = UIManager.GenerateUIInputField("Page", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(pageField.GetObject<RectTransform>(), new Vector2(650f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(150f, 64f));
                    pageField.GetObject<Image>().color = CoreHelper.InvertColorHue(CoreHelper.InvertColorValue(Color.Lerp(buttonBGColor, Color.black, 0.2f)));

                    ((Text)pageField["Placeholder"]).alignment = TextAnchor.MiddleCenter;
                    ((Text)pageField["Placeholder"]).text = "Page...";
                    ((Text)pageField["Placeholder"]).color = LSColors.fadeColor(CoreHelper.InvertColorHue(CoreHelper.InvertColorValue(textColor)), 0.2f);
                    steamPageField = pageField.GetObject<InputField>();
                    steamPageField.onValueChanged.ClearAll();
                    steamPageField.textComponent.alignment = TextAnchor.MiddleCenter;
                    steamPageField.textComponent.fontSize = 30;
                    steamPageField.textComponent.color = CoreHelper.InvertColorHue(CoreHelper.InvertColorValue(textColor));
                    steamPageField.text = DataManager.inst.GetSettingInt("CurrentArcadePage", 0).ToString();

                    if (ArcadeConfig.Instance.PageFieldRoundness.Value != 0)
                        SpriteManager.SetRoundedSprite(steamPageField.image, ArcadeConfig.Instance.PageFieldRoundness.Value, SpriteManager.RoundedSide.W);
                    else
                        steamPageField.image.sprite = null;

                    var nextPage = UIManager.GenerateUIImage("Next", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(nextPage.GetObject<RectTransform>(), new Vector2(800f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(80f, 64f));

                    var nextPageClickable = nextPage.GetObject<GameObject>().AddComponent<Clickable>();

                    nextPage.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                    if (ArcadeConfig.Instance.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(nextPage.GetObject<Image>(), ArcadeConfig.Instance.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        nextPage.GetObject<Image>().sprite = null;

                    var nextPageText = UIManager.GenerateUITextMeshPro("Text", nextPage.GetObject<RectTransform>());
                    UIManager.SetRectTransform(nextPageText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    nextPageText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    nextPageText.GetObject<TextMeshProUGUI>().fontSize = 64;
                    nextPageText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                    nextPageText.GetObject<TextMeshProUGUI>().text = ">";

                    Settings[5].Add(new Tab
                    {
                        GameObject = nextPage.GetObject<GameObject>(),
                        RectTransform = nextPage.GetObject<RectTransform>(),
                        Clickable = nextPageClickable,
                        Image = nextPage.GetObject<Image>(),
                        Text = nextPageText.GetObject<TextMeshProUGUI>(),
                        Position = new Vector2Int(3, 1),
                    });

                    steamPageField.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (int.TryParse(_val, out int p))
                        {
                            p = Mathf.Clamp(p, 0, SteamPageCount);
                            SetSteamLevelsPage(p);

                            DataManager.inst.UpdateSettingInt("CurrentArcadePage", p);
                        }
                    });

                    reloadClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(0, 1);
                    };
                    reloadClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        AudioManager.inst.PlaySound("blip");
                        steamViewType = SteamViewType.Subscribed;
                        StartCoroutine(SetSteamSearch());
                    };

                    viewTypeClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(1, 1);
                    };
                    viewTypeClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        AudioManager.inst.PlaySound("blip");
                        steamViewType = steamViewType == SteamViewType.Online ? SteamViewType.Subscribed : SteamViewType.Online;
                        if (steamViewType == SteamViewType.Online)
                            StartCoroutine(SetSteamSearch());
                        else
                            StartCoroutine(RefreshSubscribedSteamLevels());

                        viewTypeText.GetObject<TextMeshProUGUI>().text = $"[{(steamViewType == SteamViewType.Online ? "ONLINE" : "SUBSCRIBED")}]";
                    };

                    prevPageClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(2, 1);
                    };
                    prevPageClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        if (int.TryParse(steamPageField.text, out int p))
                        {
                            if (p > 0)
                            {
                                AudioManager.inst.PlaySound("blip");
                                steamPageField.text = Mathf.Clamp(p - 1, 0, steamViewType == SteamViewType.Online ? int.MaxValue : SteamPageCount).ToString();
                            }
                            else
                            {
                                AudioManager.inst.PlaySound("Block");
                            }
                        }
                    };

                    nextPageClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(3, 1);
                    };
                    nextPageClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        if (int.TryParse(steamPageField.text, out int p))
                        {
                            if (p < (steamViewType == SteamViewType.Online ? int.MaxValue : SteamPageCount))
                            {
                                AudioManager.inst.PlaySound("blip");
                                steamPageField.text = Mathf.Clamp(p + 1, 0, steamViewType == SteamViewType.Online ? int.MaxValue : SteamPageCount).ToString();
                            }
                            else
                            {
                                AudioManager.inst.PlaySound("Block");
                            }
                        }
                    };
                }

                var left = UIManager.GenerateUIImage("Left", steamRT);
                UIManager.SetRectTransform(left.GetObject<RectTransform>(), new Vector2(-880f, 300f), ZeroFive, ZeroFive, new Vector2(0.5f, 1f), new Vector2(160f, 838f));
                left.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.04f);

                var right = UIManager.GenerateUIImage("Right", steamRT);
                UIManager.SetRectTransform(right.GetObject<RectTransform>(), new Vector2(880f, 300f), ZeroFive, ZeroFive, new Vector2(0.5f, 1f), new Vector2(160f, 838f));
                right.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.04f);

                var regularContent = new GameObject("Regular Content");
                regularContent.transform.SetParent(steamRT);
                regularContent.transform.localScale = Vector3.one;

                var regularContentRT = regularContent.AddComponent<RectTransform>();
                regularContentRT.anchoredPosition = Vector2.zero;
                regularContentRT.sizeDelta = Vector3.zero;

                RegularContents.Add(regularContentRT);

                var searchField = UIManager.GenerateUIInputField("Search", steamRT);

                UIManager.SetRectTransform(searchField.GetObject<RectTransform>(), new Vector2(0f, 270f), ZeroFive, ZeroFive, ZeroFive, new Vector2(1600f, 60f));

                if (ArcadeConfig.Instance.MiscRounded.Value)
                    SpriteManager.SetRoundedSprite(searchField.GetObject<Image>(), 1, SpriteManager.RoundedSide.Bottom);
                else
                    searchField.GetObject<Image>().sprite = null;

                steamSearchFieldImage = searchField.GetObject<Image>();

                searchField.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.black, 0.2f);

                ((Text)searchField["Placeholder"]).alignment = TextAnchor.MiddleLeft;
                ((Text)searchField["Placeholder"]).text = "Search for Steam level...";
                ((Text)searchField["Placeholder"]).color = LSColors.fadeColor(textColor, 0.2f);
                steamSearchField = searchField.GetObject<InputField>();
                steamSearchField.onValueChanged.ClearAll();
                steamSearchField.textComponent.alignment = TextAnchor.MiddleLeft;
                steamSearchField.textComponent.color = textColor;
                steamSearchField.onValueChanged.AddListener(delegate (string _val)
                {
                    SteamSearchTerm = _val;
                });
            }

            selected.x = 1;
            SelectTab();

            init = true;

            yield break;
        }

        #endregion

        public void UpdateMiscRoundness()
        {
            if (ArcadeConfig.Instance.PageFieldRoundness.Value != 0)
            {
                SpriteManager.SetRoundedSprite(localPageField.image, ArcadeConfig.Instance.PageFieldRoundness.Value, SpriteManager.RoundedSide.W);
                SpriteManager.SetRoundedSprite(onlinePageField.image, ArcadeConfig.Instance.PageFieldRoundness.Value, SpriteManager.RoundedSide.W);
                SpriteManager.SetRoundedSprite(queuePageField.image, ArcadeConfig.Instance.PageFieldRoundness.Value, SpriteManager.RoundedSide.W);
                SpriteManager.SetRoundedSprite(steamPageField.image, ArcadeConfig.Instance.PageFieldRoundness.Value, SpriteManager.RoundedSide.W);
            }
            else
            {
                localPageField.image.sprite = null;
                onlinePageField.image.sprite = null;
                queuePageField.image.sprite = null;
                steamPageField.image.sprite = null;
            }

            if (ArcadeConfig.Instance.MiscRounded.Value)
            {
                SpriteManager.SetRoundedSprite(localSearchFieldImage, 1, SpriteManager.RoundedSide.Bottom);
                SpriteManager.SetRoundedSprite(onlineSearchFieldImage, 1, SpriteManager.RoundedSide.Bottom);
                SpriteManager.SetRoundedSprite(queueSearchFieldImage, 1, SpriteManager.RoundedSide.Bottom);
                SpriteManager.SetRoundedSprite(steamSearchFieldImage, 1, SpriteManager.RoundedSide.Bottom);
            }
            else
            {
                localSearchFieldImage.sprite = null;
                onlineSearchFieldImage.sprite = null;
                queueSearchFieldImage.sprite = null;
                steamSearchFieldImage.sprite = null;
            }
        }

        #region Tabs

        Tab GenerateTab()
        {
            var tabBase = UIManager.GenerateUIImage($"Tab {Tabs.Count}", TabContent);
            var tabText = UIManager.GenerateUITextMeshPro("Text", tabBase.GetObject<RectTransform>());

            var tab = new Tab
            {
                GameObject = tabBase.GetObject<GameObject>(),
                RectTransform = tabBase.GetObject<RectTransform>(),
                Text = tabText.GetObject<TextMeshProUGUI>(),
                Image = tabBase.GetObject<Image>(),
                Clickable = tabBase.GetObject<GameObject>().AddComponent<Clickable>()
            };

            Tabs.Add(tab);
            return tab;
        }

        public void SelectTab()
        {
            CoreHelper.Log($"Selected [X: {selected.x} - Y: {selected.y}]");

            if (selected.x == 0)
                SceneManager.inst.LoadScene("Input Select");
            else
            {
                CurrentTab = selected.x - 1;

                int num = 1;
                foreach (var baseItem in RegularBases)
                {
                    baseItem.gameObject.SetActive(selected.x == num);
                    num++;
                }

                switch (selected.x)
                {
                    case 1:
                        {
                            SelectionLimit[1] = 3;

                            StartCoroutine(RefreshLocalLevels());
                            break;
                        }
                    case 2:
                        {
                            SelectionLimit[1] = 2;

                            var count = SelectionLimit.Count;
                            SelectionLimit.RemoveRange(2, count - 2);

                            SelectionLimit.Add(1);

                            break;
                        }
                    case 3:
                        {
                            SelectionLimit[1] = 1;
                            var count = SelectionLimit.Count;
                            SelectionLimit.RemoveRange(2, count - 2);

                            break;
                        }
                    case 4:
                        {
                            SelectionLimit[1] = 1;
                            var count = SelectionLimit.Count;
                            SelectionLimit.RemoveRange(2, count - 2);

                            break;
                        }
                    case 5:
                        {
                            SelectionLimit[1] = 8;

                            StartCoroutine(RefreshQueuedLevels());

                            break;
                        }
                    case 6:
                        {
                            SelectionLimit[1] = 4;

                            if (steamViewType == SteamViewType.Subscribed)
                                StartCoroutine(SteamWorkshopManager.inst.hasLoaded ? RefreshSubscribedSteamLevels() : SetSteamSearch());

                            break;
                        }
                }
            }
        }

        public void UpdateTabRoundness()
        {
            foreach (var tab in Tabs)
            {
                if (ArcadeConfig.Instance.TabsRoundedness.Value != 0)
                    SpriteManager.SetRoundedSprite(tab.Image, ArcadeConfig.Instance.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                else
                    tab.Image.sprite = null;
            }

            for (int i = 0; i < Settings.Count; i++)
            {
                foreach (var tab in Settings[i])
                {
                    if (ArcadeConfig.Instance.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(tab.Image, ArcadeConfig.Instance.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        tab.Image.sprite = null;
                }
            }
        }

        #endregion

        #region Local

        public List<LocalLevelButton> LocalLevels { get; set; } = new List<LocalLevelButton>();

        public Image localSearchFieldImage;
        public InputField localSearchField;

        string localSearchTerm;
        public string LocalSearchTerm
        {
            get => localSearchTerm;
            set
            {
                localSearchTerm = value;
                selected = new Vector2Int(0, 2);
                if (localPageField.text != "0")
                    localPageField.text = "0";
                else
                    StartCoroutine(RefreshLocalLevels());
            }
        }

        public int LocalPageCount => ILocalLevels.Count() / MaxLevelsPerPage;

        public IEnumerable<Level> ILocalLevels => LevelManager.Levels.Where(level => string.IsNullOrEmpty(LocalSearchTerm)
                        || level.id == LocalSearchTerm
                        || level.metadata.LevelSong.tags.Contains(LocalSearchTerm)
                        || level.metadata.artist.Name.ToLower().Contains(LocalSearchTerm.ToLower())
                        || level.metadata.creator.steam_name.ToLower().Contains(LocalSearchTerm.ToLower())
                        || level.metadata.song.title.ToLower().Contains(LocalSearchTerm.ToLower())
                        || level.metadata.song.getDifficulty().ToLower().Contains(LocalSearchTerm.ToLower()));

        public void SetLocalLevelsPage(int page)
        {
            CurrentPage[0] = page;
            StartCoroutine(RefreshLocalLevels());
        }

        public InputField localPageField;

        Vector2 localLevelsAlignment = new Vector2(-640f, 138f);

        bool loadingLocalLevels;
        public IEnumerator RefreshLocalLevels()
        {
            loadingLocalLevels = true;
            LSHelpers.DeleteChildren(RegularContents[0]);
            LocalLevels.Clear();
            int currentPage = CurrentPage[0] + 1;

            int max = currentPage * MaxLevelsPerPage;

            float top = localLevelsAlignment.y;
            float left = localLevelsAlignment.x;

            int currentRow = -1;

            var count = SelectionLimit.Count;
            SelectionLimit.RemoveRange(2, count - 2);

            if (LevelManager.Levels.Count > 0)
            {
                TriggerHelper.AddEventTriggers(localPageField.gameObject, TriggerHelper.ScrollDeltaInt(localPageField, max: LocalPageCount));
            }
            else
            {
                TriggerHelper.AddEventTriggers(localPageField.gameObject);
            }

            int num = 0;
            foreach (var level in ILocalLevels)
            {
                if (level.id != null && level.id != "0" && num >= max - MaxLevelsPerPage && num < max)
                {
                    var gameObject = levelPrefab.Duplicate(RegularContents[0]);

                    int column = (num % MaxLevelsPerPage) % 5;
                    int row = (int)((num % MaxLevelsPerPage) / 5);

                    if (currentRow != row)
                    {
                        currentRow = row;
                        SelectionLimit.Add(1);
                    }
                    else
                    {
                        SelectionLimit[row + 2]++;
                    }

                    float x = left + (column * 320f);
                    float y = top - (row * 190f);

                    gameObject.transform.AsRT().anchoredPosition = new Vector2(x, y);

                    var clickable = gameObject.GetComponent<Clickable>();
                    clickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected.x = column;
                        selected.y = row + 2;
                    };
                    clickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        AudioManager.inst.PlaySound("blip");
                        StartCoroutine(SelectLocalLevel(level));
                    };

                    var image = gameObject.GetComponent<Image>();
                    image.color = buttonBGColor;

                    var difficulty = gameObject.transform.Find("Difficulty").GetComponent<Image>();
                    UIManager.SetRectTransform(difficulty.rectTransform, Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(8f, 0f));
                    difficulty.color = CoreHelper.GetDifficulty(level.metadata.song.difficulty).color;

                    if (ArcadeConfig.Instance.LocalLevelsRoundness.Value != 0)
                        SpriteManager.SetRoundedSprite(image, ArcadeConfig.Instance.LocalLevelsRoundness.Value, SpriteManager.RoundedSide.W);
                    else
                        image.sprite = null;

                    var title = gameObject.transform.Find("Title").GetComponent<TextMeshProUGUI>();
                    UIManager.SetRectTransform(title.rectTransform, new Vector2(0f, -60f), ZeroFive, ZeroFive, ZeroFive, new Vector2(280f, 60f));

                    title.fontSize = 20;
                    title.fontStyle = FontStyles.Bold;
                    title.enableWordWrapping = true;
                    title.overflowMode = TextOverflowModes.Truncate;
                    title.color = textColor;
                    title.text = level.metadata.LevelBeatmap.name;

                    var iconBase = gameObject.transform.Find("Icon Base").GetComponent<Image>();
                    iconBase.rectTransform.anchoredPosition = new Vector2(-90f, 30f);

                    if (ArcadeConfig.Instance.LocalLevelsIconRoundness.Value != 0)
                        SpriteManager.SetRoundedSprite(iconBase, ArcadeConfig.Instance.LocalLevelsIconRoundness.Value, SpriteManager.RoundedSide.W);
                    else
                        iconBase.sprite = null;

                    var icon = gameObject.transform.Find("Icon Base/Icon").GetComponent<Image>();
                    icon.rectTransform.anchoredPosition = Vector2.zero;

                    icon.sprite = level.icon ?? SteamWorkshop.inst.defaultSteamImageSprite;

                    var rank = gameObject.transform.Find("Rank").GetComponent<TextMeshProUGUI>();
                    var rankShadow = gameObject.transform.Find("Rank Shadow").GetComponent<TextMeshProUGUI>();
                    rank.gameObject.SetActive(level.metadata.song.difficulty != 6);
                    rankShadow.gameObject.SetActive(level.metadata.song.difficulty != 6);

                    if (level.metadata.song.difficulty != 6)
                    {
                        UIManager.SetRectTransform(rank.rectTransform, new Vector2(90f, 30f), ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                        rank.transform.localRotation = Quaternion.Euler(0f, 0f, 356f);

                        var levelRank = LevelManager.GetLevelRank(level);
                        rank.fontSize = 64;
                        rank.text = $"<align=right><color=#{CoreHelper.ColorToHex(levelRank.color)}><b>{levelRank.name}</b></color>";

                        UIManager.SetRectTransform(rankShadow.rectTransform, new Vector2(87f, 28f), ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                        rankShadow.transform.localRotation = Quaternion.Euler(0f, 0f, 356f);

                        rankShadow.fontSize = 68;
                        rankShadow.text = $"<align=right><color=#00000035><b>{levelRank.name}</b></color>";
                    }

                    var shineController = gameObject.transform.Find("Shine").GetComponent<ShineController>();

                    shineController.maxDelay = 1f;
                    shineController.minDelay = 0.2f;
                    shineController.offset = 260f;
                    shineController.offsetOverShoot = 32f;
                    shineController.speed = 0.7f;

                    LocalLevels.Add(new LocalLevelButton
                    {
                        Position = new Vector2Int(column, row),
                        GameObject = gameObject,
                        Clickable = clickable,
                        RectTransform = gameObject.transform.AsRT(),
                        BaseImage = image,
                        DifficultyImage = difficulty,
                        Title = title,
                        BaseIcon = iconBase,
                        Icon = icon,
                        Level = level,
                        ShineController = shineController,
                        shine1 = gameObject.transform.Find("Shine").GetComponent<Image>(),
                        shine2 = gameObject.transform.Find("Shine/Image").GetComponent<Image>(),
                        Rank = rank,
                    });
                }

                num++;
            }

            loadingLocalLevels = false;
            yield break;
        }

        public void UpdateLocalLevelsRoundness()
        {
            foreach (var level in LocalLevels)
            {
                if (ArcadeConfig.Instance.LocalLevelsRoundness.Value != 0)
                    SpriteManager.SetRoundedSprite(level.BaseImage, ArcadeConfig.Instance.LocalLevelsRoundness.Value, SpriteManager.RoundedSide.W);
                else
                    level.BaseImage.sprite = null;

                if (ArcadeConfig.Instance.LocalLevelsIconRoundness.Value != 0)
                    SpriteManager.SetRoundedSprite(level.BaseIcon, ArcadeConfig.Instance.LocalLevelsIconRoundness.Value, SpriteManager.RoundedSide.W);
                else
                    level.BaseIcon.sprite = null;
            }
        }

        public IEnumerator SelectLocalLevel(Level level)
        {
            if (!level.music)
            {
                yield return StartCoroutine(level.LoadAudioClipRoutine(delegate ()
                {
                    AudioManager.inst.StopMusic();
                    PlayLevelMenuManager.inst?.OpenLevel(level);
                    AudioManager.inst.PlayMusic(level.metadata.song.title, level.music);
                    AudioManager.inst.SetPitch(CoreHelper.Pitch);
                }));
            }
            else
            {
                AudioManager.inst.StopMusic();
                PlayLevelMenuManager.inst?.OpenLevel(level);
                AudioManager.inst.PlayMusic(level.metadata.song.title, level.music);
                AudioManager.inst.SetPitch(CoreHelper.Pitch);
            }

            yield break;
        }

        #endregion

        #region Online

        public Image onlineSearchFieldImage;
        public InputField onlineSearchField;

        string onlineSearchTerm;
        public string OnlineSearchTerm
        {
            get => onlineSearchTerm;
            set => onlineSearchTerm = value;
        }

        public int OnlineLevelCount { get; set; }

        public static string SearchURL => $"{AlephNetworkManager.ArcadeServerURL}api/level/search";
        public static string CoverURL => $"{AlephNetworkManager.ArcadeServerURL}api/level/cover/";
        public static string DownloadURL => $"{AlephNetworkManager.ArcadeServerURL}api/level/zip/";

        public void SetOnlineLevelsPage(int page)
        {
            CurrentPage[1] = page;
            //StartCoroutine(SearchOnlineLevels());
        }

        public InputField onlinePageField;

        Vector2 onlineLevelsAlignment = new Vector2(-640f, 138f);

        bool loadingOnlineLevels;
        public IEnumerator SearchOnlineLevels()
        {
            if (!AlephNetworkManager.ServerFinished)
                yield break;

            var page = CurrentPage[1];
            int currentPage = CurrentPage[1] + 1;

            var search = OnlineSearchTerm;

            string query = string.IsNullOrEmpty(search) && page == 0 ? SearchURL : string.IsNullOrEmpty(search) && page != 0 ? $"{SearchURL}?page={page}" : !string.IsNullOrEmpty(search) && page == 0 ? $"{SearchURL}?query={ReplaceSpace(search)}" : !string.IsNullOrEmpty(search) ? $"{SearchURL}?query={ReplaceSpace(search)}&page={page}" : "";

            CoreHelper.Log($"Search query: {query}");

            loadingOnlineLevels = true;
            LSHelpers.DeleteChildren(RegularContents[1]);
            OnlineLevels.Clear();

            if (string.IsNullOrEmpty(query))
            {
                loadingOnlineLevels = false;

                yield break;
            }

            int max = currentPage * MaxLevelsPerPage;

            float top = onlineLevelsAlignment.y;
            float left = onlineLevelsAlignment.x;

            int currentRow = -1;

            var count = SelectionLimit.Count;
            SelectionLimit.RemoveRange(3, count - 3);

            yield return StartCoroutine(AlephNetworkManager.DownloadJSONFile(query, delegate (string j)
            {
                try
                {
                    var jn = JSON.Parse(j);

                    if (jn["items"] != null)
                    {
                        for (int i = 0; i < jn["items"].Count; i++)
                        {
                            var item = jn["items"][i];

                            string id = item["id"];

                            string artist = item["artist"];
                            string title = item["title"];
                            string creator = item["creator"];
                            string description = item["description"];
                            var difficulty = item["difficulty"].AsInt;

                            if (id != null && id != "0")
                            {
                                var gameObject = levelPrefab.Duplicate(RegularContents[1]);

                                int column = (i % MaxLevelsPerPage) % 5;
                                int row = (int)((i % MaxLevelsPerPage) / 5);

                                if (currentRow != row)
                                {
                                    currentRow = row;
                                    SelectionLimit.Add(1);
                                }
                                else
                                {
                                    SelectionLimit[row + 3]++;
                                }

                                float x = left + (column * 320f);
                                float y = top - (row * 190f);

                                gameObject.transform.AsRT().anchoredPosition = new Vector2(x, y);

                                var clickable = gameObject.GetComponent<Clickable>();

                                var image = gameObject.GetComponent<Image>();
                                image.color = buttonBGColor;

                                var difficultyImage = gameObject.transform.Find("Difficulty").GetComponent<Image>();
                                UIManager.SetRectTransform(difficultyImage.rectTransform, Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(8f, 0f));
                                difficultyImage.color = CoreHelper.GetDifficulty(difficulty).color;

                                if (ArcadeConfig.Instance.LocalLevelsRoundness.Value != 0)
                                    SpriteManager.SetRoundedSprite(image, ArcadeConfig.Instance.LocalLevelsRoundness.Value, SpriteManager.RoundedSide.W);
                                else
                                    image.sprite = null;

                                var titleText = gameObject.transform.Find("Title").GetComponent<TextMeshProUGUI>();
                                UIManager.SetRectTransform(titleText.rectTransform, new Vector2(0f, -60f), ZeroFive, ZeroFive, ZeroFive, new Vector2(280f, 60f));

                                titleText.fontSize = 20;
                                titleText.fontStyle = FontStyles.Bold;
                                titleText.enableWordWrapping = true;
                                titleText.overflowMode = TextOverflowModes.Truncate;
                                titleText.color = textColor;
                                titleText.text = $"{title}";

                                var iconBase = gameObject.transform.Find("Icon Base").GetComponent<Image>();
                                iconBase.rectTransform.anchoredPosition = new Vector2(-90f, 30f);

                                if (ArcadeConfig.Instance.LocalLevelsIconRoundness.Value != 0)
                                    SpriteManager.SetRoundedSprite(iconBase, ArcadeConfig.Instance.LocalLevelsIconRoundness.Value, SpriteManager.RoundedSide.W);
                                else
                                    iconBase.sprite = null;

                                var icon = gameObject.transform.Find("Icon Base/Icon").GetComponent<Image>();
                                icon.rectTransform.anchoredPosition = Vector2.zero;

                                icon.sprite = SteamWorkshop.inst.defaultSteamImageSprite;

                                Destroy(gameObject.transform.Find("Rank").gameObject);
                                Destroy(gameObject.transform.Find("Rank Shadow").gameObject);
                                Destroy(gameObject.transform.Find("Shine").gameObject);

                                int num = -1;

                                int.TryParse(id, out num);

                                if (!OnlineLevelIcons.ContainsKey(id) && num >= 0)
                                {
                                    StartCoroutine(AlephNetworkManager.DownloadBytes($"{CoverURL}{num}", delegate (byte[] bytes)
                                    {
                                        var sprite = SpriteManager.LoadSprite(bytes);
                                        OnlineLevelIcons.Add(id, sprite);
                                        icon.sprite = sprite;
                                    }, delegate (string onError)
                                    {
                                        OnlineLevelIcons.Add(id, SteamWorkshop.inst.defaultSteamImageSprite);
                                        icon.sprite = SteamWorkshop.inst.defaultSteamImageSprite;
                                    }));
                                }
                                else if (OnlineLevelIcons.ContainsKey(id))
                                {
                                    icon.sprite = OnlineLevelIcons[id];
                                }
                                else
                                {
                                    OnlineLevelIcons.Add(id, SteamWorkshop.inst.defaultSteamImageSprite);
                                    icon.sprite = SteamWorkshop.inst.defaultSteamImageSprite;
                                }

                                var level = new OnlineLevelButton
                                {
                                    ID = id,
                                    Artist = artist,
                                    Creator = creator,
                                    Description = description,
                                    Difficulty = difficulty,
                                    Title = title,
                                    Position = new Vector2Int(column, row),
                                    GameObject = gameObject,
                                    Clickable = clickable,
                                    RectTransform = gameObject.transform.AsRT(),
                                    BaseImage = image,
                                    DifficultyImage = difficultyImage,
                                    TitleText = titleText,
                                    BaseIcon = iconBase,
                                    Icon = icon,
                                };

                                clickable.onEnter = delegate (PointerEventData pointerEventData)
                                {
                                    if (!CanSelect)
                                        return;

                                    AudioManager.inst.PlaySound("LeftRight");
                                    selected.x = column;
                                    selected.y = row + 3;
                                };
                                clickable.onClick = delegate (PointerEventData pointerEventData)
                                {
                                    AudioManager.inst.PlaySound("blip");
                                    SelectOnlineLevel(level);
                                };

                                OnlineLevels.Add(level);
                            }
                        }
                    }

                    if (jn["count"] != null)
                    {
                        OnlineLevelCount = jn["count"].AsInt;
                    }
                }
                catch (Exception ex)
                {
                    CoreHelper.LogError($"{ex}");
                }
            }));

            if (OnlineLevels.Count > 0)
            {
                TriggerHelper.AddEventTriggers(onlinePageField.gameObject, TriggerHelper.ScrollDeltaInt(onlinePageField, max: OnlineLevelCount));
            }
            else
            {
                TriggerHelper.AddEventTriggers(onlinePageField.gameObject);
            }

            loadingOnlineLevels = false;
        }

        public List<OnlineLevelButton> OnlineLevels { get; set; } = new List<OnlineLevelButton>();

        public Dictionary<string, Sprite> OnlineLevelIcons { get; set; } = new Dictionary<string, Sprite>();

        string ReplaceSpace(string search) => search.ToLower().Replace(" ", "+");

        public void SelectOnlineLevel(OnlineLevelButton onlineLevel)
        {
            DownloadLevelMenuManager.inst?.OpenLevel(onlineLevel);
        }

        #endregion

        #region Browser

        public void OpenLocalBrowser()
        {
            string text = FileBrowser.OpenSingleFile("Select a level to play!", RTFile.ApplicationDirectory, "lsb", "vgd");
            if (!string.IsNullOrEmpty(text))
            {
                text = text.Replace("\\", "/");

                if (!text.Contains("/level.lsb") && !text.Contains("/level.vgd"))
                {
                    CoreHelper.LogError($"Please select an actual level{(text.Contains("/metadata.lsb") ? " and not the metadata!" : ".")}");
                    return;
                }

                var path = text.Replace("/level.lsb", "").Replace("/level.vgd", "");

                if (!RTFile.FileExists($"{path}/metadata.lsb") && !RTFile.FileExists($"{path}/metadata.vgm"))
                {
                    CoreHelper.LogError($"No metadata!");
                    return;
                }

                if (!RTFile.FileExists($"{path}/level.ogg") && !RTFile.FileExists($"{path}/level.wav") && !RTFile.FileExists($"{path}/level.mp3")
                 && !RTFile.FileExists($"{path}/audio.ogg") && !RTFile.FileExists($"{path}/audio.wav") && !RTFile.FileExists($"{path}/audio.mp3"))
                {
                    CoreHelper.LogError($"No song!");
                    return;
                }

                MetaData metadata = RTFile.FileExists($"{path}/metadata.vgm") ? MetaData.ParseVG(JSON.Parse(RTFile.ReadFromFile($"{path}/metadata.vgm"))) : MetaData.Parse(JSON.Parse(RTFile.ReadFromFile($"{path}/metadata.lsb")));

                if ((string.IsNullOrEmpty(metadata.serverID) || metadata.serverID == "-1")
                    && (string.IsNullOrEmpty(metadata.LevelBeatmap.beatmap_id) && metadata.LevelBeatmap.beatmap_id == "-1" || metadata.LevelBeatmap.beatmap_id == "0")
                    && (string.IsNullOrEmpty(metadata.arcadeID) || metadata.arcadeID == "-1" || metadata.arcadeID == "0"))
                {
                    metadata.arcadeID = LSText.randomNumString(16);
                    var metadataJN = metadata.ToJSON();
                    RTFile.WriteToFile($"{path}/metadata.lsb", metadataJN.ToString(3));
                }

                var level = new Level(path + "/");

                StartCoroutine(SelectLocalLevel(level));
            }
        }

        #endregion

        #region Download

        public InputField downloadField;

        public void DownloadLevel()
        {
            if (string.IsNullOrEmpty(downloadField.text))
            {
                CoreHelper.LogError($"URL must not be empty!");
                return;
            }

            StartCoroutine(AlephNetworkManager.DownloadBytes(downloadField.text, delegate (byte[] bytes)
            {
                try
                {
                    var path = $"{RTFile.ApplicationDirectory}{LevelManager.ListPath}";
                    string name = "/downloaded level";
                    int num = 0;
                    while (Directory.Exists(path + name))
                    {
                        num++;
                        name = $"/downloaded level {num}";
                    }

                    var directory = path + name;

                    Directory.CreateDirectory(directory);

                    File.WriteAllBytes($"{directory}.zip", bytes);

                    try
                    {
                        ZipFile.ExtractToDirectory($"{directory}.zip", $"{directory}");
                    }
                    catch (Exception ex)
                    {
                        CoreHelper.LogError($"{ex}");

                        File.Delete($"{directory}.zip");
                        Directory.Delete(directory, true);

                        return;
                    }

                    File.Delete($"{directory}.zip");

                    MetaData metadata = RTFile.FileExists($"{directory}/metadata.vgm") ? MetaData.ParseVG(JSON.Parse(RTFile.ReadFromFile($"{directory}/metadata.vgm"))) : MetaData.Parse(JSON.Parse(RTFile.ReadFromFile($"{directory}/metadata.lsb")));

                    var level = new Level(directory + "/");

                    LevelManager.Levels.Add(level);

                    if (CurrentTab == 0)
                    {
                        StartCoroutine(RefreshLocalLevels());
                    }
                    else
                    {
                        if (ArcadeConfig.Instance.OpenOnlineLevelAfterDownload.Value)
                        {
                            StartCoroutine(SelectLocalLevel(level));
                        }
                    }
                }
                catch (Exception ex)
                {
                    CoreHelper.LogError($"{ex}");
                }
            }));
        }

        #endregion

        #region Queue

        public List<LocalLevelButton> QueueLevels { get; set; } = new List<LocalLevelButton>();

        public Image queueSearchFieldImage;
        public InputField queueSearchField;

        string queueSearchTerm;
        public string QueueSearchTerm
        {
            get => queueSearchTerm;
            set
            {
                queueSearchTerm = value;
                selected = new Vector2Int(0, 2);
                if (queuePageField.text != "0")
                    queuePageField.text = "0";
                else
                    StartCoroutine(RefreshQueuedLevels());
            }
        }

        public int QueuePageCount => (IQueueLevels.Count() - 1) / MaxQueuedLevelsPerPage;

        public IEnumerable<Level> IQueueLevels => LevelManager.ArcadeQueue.Where(level => string.IsNullOrEmpty(QueueSearchTerm)
                        || level.id == QueueSearchTerm
                        || level.metadata.artist.Name.ToLower().Contains(QueueSearchTerm.ToLower())
                        || level.metadata.creator.steam_name.ToLower().Contains(QueueSearchTerm.ToLower())
                        || level.metadata.song.title.ToLower().Contains(QueueSearchTerm.ToLower())
                        || level.metadata.song.getDifficulty().ToLower().Contains(QueueSearchTerm.ToLower()));

        public void SetQueueLevelsPage(int page)
        {
            CurrentPage[4] = page;
            StartCoroutine(RefreshQueuedLevels());
        }

        public InputField queuePageField;

        Vector2 queueLevelsAlignment = new Vector2(0f, 138f);

        public static int MaxQueuedLevelsPerPage { get; set; } = 4;

        bool loadingQueuedLevels;

        public IEnumerator RefreshQueuedLevels()
        {
            loadingQueuedLevels = true;
            LSHelpers.DeleteChildren(RegularContents[4]);
            QueueLevels.Clear();
            int currentPage = CurrentPage[4] + 1;

            int max = currentPage * MaxQueuedLevelsPerPage;

            float top = queueLevelsAlignment.y;

            var count = SelectionLimit.Count;
            SelectionLimit.RemoveRange(2, count - 2);

            int num = 0;
            foreach (var level in IQueueLevels)
            {
                if (level.id != null && level.id != "0" && num >= max - MaxQueuedLevelsPerPage && num < max)
                {
                    var gameObject = levelPrefab.Duplicate(RegularContents[4]);

                    int row = num % MaxQueuedLevelsPerPage;

                    SelectionLimit.Add(1);

                    float y = top - (row * 190f);

                    gameObject.transform.AsRT().anchoredPosition = new Vector2(0f, y);
                    gameObject.transform.AsRT().sizeDelta = new Vector2(1570f, 180f);

                    var clickable = gameObject.GetComponent<Clickable>();
                    clickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected.x = 0;
                        selected.y = row + 2;
                    };
                    clickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        AudioManager.inst.PlaySound("blip");
                        LevelManager.ArcadeQueue.RemoveAll(x => x.id == level.id);
                        StartCoroutine(RefreshQueuedLevels());
                    };

                    var image = gameObject.GetComponent<Image>();
                    image.color = buttonBGColor;

                    var difficulty = gameObject.transform.Find("Difficulty").GetComponent<Image>();
                    UIManager.SetRectTransform(difficulty.rectTransform, Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(8f, 0f));
                    difficulty.color = CoreHelper.GetDifficulty(level.metadata.song.difficulty).color;

                    if (ArcadeConfig.Instance.LocalLevelsRoundness.Value != 0)
                        SpriteManager.SetRoundedSprite(image, ArcadeConfig.Instance.LocalLevelsRoundness.Value, SpriteManager.RoundedSide.W);
                    else
                        image.sprite = null;

                    var title = gameObject.transform.Find("Title").GetComponent<TextMeshProUGUI>();
                    UIManager.SetRectTransform(title.rectTransform, new Vector2(-300f, 60f), ZeroFive, ZeroFive, ZeroFive, new Vector2(600f, 60f));

                    title.fontSize = 20;
                    title.fontStyle = FontStyles.Bold;
                    title.enableWordWrapping = true;
                    title.overflowMode = TextOverflowModes.Truncate;
                    title.color = textColor;
                    title.text = level.metadata.LevelBeatmap.name;

                    var iconBase = gameObject.transform.Find("Icon Base").GetComponent<Image>();
                    iconBase.rectTransform.anchoredPosition = new Vector2(-690f, 0f);
                    iconBase.rectTransform.sizeDelta = new Vector2(132f, 132f);

                    if (ArcadeConfig.Instance.LocalLevelsIconRoundness.Value != 0)
                        SpriteManager.SetRoundedSprite(iconBase, ArcadeConfig.Instance.LocalLevelsIconRoundness.Value, SpriteManager.RoundedSide.W);
                    else
                        iconBase.sprite = null;

                    var icon = gameObject.transform.Find("Icon Base/Icon").GetComponent<Image>();
                    icon.rectTransform.anchoredPosition = Vector2.zero;
                    icon.rectTransform.sizeDelta = new Vector2(132f, 132f);

                    icon.sprite = level.icon ?? SteamWorkshop.inst.defaultSteamImageSprite;

                    var rank = gameObject.transform.Find("Rank").GetComponent<TextMeshProUGUI>();

                    UIManager.SetRectTransform(rank.rectTransform, new Vector2(590f, 30f), ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    rank.transform.localRotation = Quaternion.Euler(0f, 0f, 356f);

                    var levelRank = LevelManager.GetLevelRank(level);
                    rank.fontSize = 120;
                    rank.text = $"<color=#{CoreHelper.ColorToHex(levelRank.color)}><b>{levelRank.name}</b></color>";

                    var rankShadow = gameObject.transform.Find("Rank Shadow").GetComponent<TextMeshProUGUI>();

                    UIManager.SetRectTransform(rankShadow.rectTransform, new Vector2(587f, 28f), ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    rankShadow.transform.localRotation = Quaternion.Euler(0f, 0f, 356f);

                    rankShadow.fontSize = 124;
                    rankShadow.text = $"<color=#00000035><b>{levelRank.name}</b></color>";

                    Destroy(gameObject.transform.Find("Shine").gameObject);

                    QueueLevels.Add(new LocalLevelButton
                    {
                        Position = new Vector2Int(0, row),
                        GameObject = gameObject,
                        Clickable = clickable,
                        RectTransform = gameObject.transform.AsRT(),
                        BaseImage = image,
                        DifficultyImage = difficulty,
                        Title = title,
                        BaseIcon = iconBase,
                        Icon = icon,
                        Level = level,
                        Rank = rank,
                    });
                }

                num++;
            }

            if (IQueueLevels.Count() > 0)
            {
                TriggerHelper.AddEventTriggers(queuePageField.gameObject, TriggerHelper.ScrollDeltaInt(queuePageField, max: QueuePageCount));
            }
            else
            {
                TriggerHelper.AddEventTriggers(queuePageField.gameObject);
            }

            loadingQueuedLevels = false;
            yield break;
        }

        public void ShuffleQueue(bool play)
        {
            if (LevelManager.Levels.Count < 1)
            {
                CoreHelper.LogError($"No levels to shuffle!");
                return;
            }

            LevelManager.ArcadeQueue.Clear();

            var queueRandom = new List<int>();
            var queue = new List<Level>();

            var levels = LevelManager.Levels.Union(SteamWorkshopManager.inst.Levels).ToList();

            for (int i = 0; i < levels.Count; i++)
            {
                queueRandom.Add(i);
            }

            queueRandom = queueRandom.OrderBy(x => -(x - UnityEngine.Random.Range(0, levels.Count))).ToList();

            var minRandom = UnityEngine.Random.Range(0, levels.Count - ArcadeConfig.Instance.ShuffleQueueAmount.Value);

            for (int i = 0; i < queueRandom.Count; i++)
            {
                if (i >= minRandom && i - ArcadeConfig.Instance.ShuffleQueueAmount.Value < minRandom)
                {
                    queue.Add(levels[queueRandom[i]]);
                }
            }

            LevelManager.current = 0;
            LevelManager.ArcadeQueue.AddRange(queue);

            if (play)
            {
                menuUI.SetActive(false);
                LevelManager.OnLevelEnd = ArcadeHelper.EndOfLevel;
                CoreHelper.StartCoroutine(LevelManager.Play(LevelManager.ArcadeQueue[0]));
            }
            else
            {
                StartCoroutine(RefreshQueuedLevels());
            }

            queueRandom.Clear();
            queueRandom = null;
        }

        #endregion

        #region Steam

        public SteamViewType steamViewType = SteamViewType.Subscribed;

        public enum SteamViewType
        {
            Subscribed,
            Online
        }

        public List<LocalLevelButton> SubscribedSteamLevels { get; set; } = new List<LocalLevelButton>();
        public List<SteamLevelButton> OnlineSteamLevels { get; set; } = new List<SteamLevelButton>();

        public Image steamSearchFieldImage;
        public InputField steamSearchField;

        string steamSearchTerm;
        public string SteamSearchTerm
        {
            get => steamSearchTerm;
            set
            {
                steamSearchTerm = value;
                selected = new Vector2Int(0, 2);

                if (steamPageField.text != "0")
                {
                    steamPageField.text = "0";
                    return;
                }

                if (steamViewType == SteamViewType.Subscribed)
                    StartCoroutine(RefreshSubscribedSteamLevels());
                if (steamViewType == SteamViewType.Online)
                {
                    SearchSteam();

                    //StartCoroutine(SteamWorkshopManager.inst.Search(SteamSearchTerm, CurrentPage[5] + 1, delegate ()
                    //{
                    //    StartCoroutine(RefreshOnlineSteamLevels());
                    //}));
                }
            }
        }

        public int SteamPageCount => SteamWorkshopManager.inst.Levels.Count() / MaxSteamLevelsPerPage;

        public IEnumerable<Level> ISteamLevels => SteamWorkshopManager.inst.Levels.Where(level => string.IsNullOrEmpty(SteamSearchTerm)
                        || level.id == SteamSearchTerm
                        || level.metadata.artist.Name.ToLower().Contains(SteamSearchTerm.ToLower())
                        || level.metadata.creator.steam_name.ToLower().Contains(SteamSearchTerm.ToLower())
                        || level.metadata.song.title.ToLower().Contains(SteamSearchTerm.ToLower())
                        || level.metadata.song.getDifficulty().ToLower().Contains(SteamSearchTerm.ToLower()));

        public void SetSteamLevelsPage(int page)
        {
            CurrentPage[5] = page;
            if (steamViewType == SteamViewType.Subscribed)
                StartCoroutine(RefreshSubscribedSteamLevels());
            if (steamViewType == SteamViewType.Online)
            {
                SearchSteam();

                //StartCoroutine(SteamWorkshopManager.inst.Search(SteamSearchTerm, CurrentPage[5] + 1, delegate ()
                //{
                //    StartCoroutine(RefreshOnlineSteamLevels());
                //}));
            }
        }

        public InputField steamPageField;

        Vector2 steamLevelsAlignment = new Vector2(-640f, 180f);

        public IEnumerator SetSteamSearch()
        {
            if (steamViewType == SteamViewType.Subscribed)
            {
                LSHelpers.DeleteChildren(RegularContents[5]);
                yield return StartCoroutine(SteamWorkshopManager.inst.GetSubscribedItems());

                SteamWorkshopManager.inst.Levels = LevelManager.SortLevels(SteamWorkshopManager.inst.Levels, (int)ArcadeConfig.Instance.SteamLevelOrderby.Value, ArcadeConfig.Instance.SteamLevelAscend.Value);

                StartCoroutine(RefreshSubscribedSteamLevels());
            }

            if (steamViewType == SteamViewType.Online)
            {
                SearchSteam();

                //StartCoroutine(SteamWorkshopManager.inst.Search(SteamSearchTerm, CurrentPage[5] + 1, delegate ()
                //{
                //    StartCoroutine(RefreshOnlineSteamLevels());
                //}));
            }
        }

        public void SearchSteam()
        {
            StartCoroutine(RefreshOnlineSteamLevels());
        }

        public async void SearchSteamAsync()
        {
            await SteamWorkshopManager.inst.SearchAsync(SteamSearchTerm, CurrentPage[5] + 1);
            await SteamWorkshopManager.inst.SearchAsync(SteamSearchTerm, CurrentPage[5] + 1);
            StartCoroutine(RefreshOnlineSteamLevels());
        }

        public static int MaxSteamLevelsPerPage => 35;

        bool loadingSteamLevels;
        public IEnumerator RefreshSubscribedSteamLevels()
        {
            loadingSteamLevels = true;
            LSHelpers.DeleteChildren(RegularContents[5]);
            SubscribedSteamLevels.Clear();
            int currentPage = CurrentPage[5] + 1;

            int max = currentPage * MaxSteamLevelsPerPage;

            float top = steamLevelsAlignment.y;
            float left = steamLevelsAlignment.x;

            int currentRow = -1;

            var count = SelectionLimit.Count;
            SelectionLimit.RemoveRange(2, count - 2);

            if (SteamWorkshopManager.inst.Levels.Count > 0)
            {
                TriggerHelper.AddEventTriggers(steamPageField.gameObject, TriggerHelper.ScrollDeltaInt(steamPageField, max: SteamPageCount));
            }
            else
            {
                TriggerHelper.AddEventTriggers(steamPageField.gameObject);
            }

            int num = 0;
            foreach (var level in ISteamLevels)
            {
                if (level.id != null && level.id != "0" && num >= max - MaxSteamLevelsPerPage && num < max)
                {
                    var gameObject = levelPrefab.Duplicate(RegularContents[5]);

                    int column = (num % MaxSteamLevelsPerPage) % 5;
                    int row = (int)((num % MaxSteamLevelsPerPage) / 5);

                    if (currentRow != row)
                    {
                        currentRow = row;
                        SelectionLimit.Add(1);
                    }
                    else
                    {
                        SelectionLimit[row + 2]++;
                    }

                    float x = left + (column * 320f);
                    float y = top - (row * 110f);

                    gameObject.transform.AsRT().anchoredPosition = new Vector2(x, y);
                    gameObject.transform.AsRT().sizeDelta = new Vector2(300f, 94f);

                    var clickable = gameObject.GetComponent<Clickable>();
                    clickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected.x = column;
                        selected.y = row + 2;
                    };
                    clickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        AudioManager.inst.PlaySound("blip");
                        StartCoroutine(SelectLocalLevel(level));
                    };

                    var image = gameObject.GetComponent<Image>();
                    image.color = buttonBGColor;

                    var difficulty = gameObject.transform.Find("Difficulty").GetComponent<Image>();
                    UIManager.SetRectTransform(difficulty.rectTransform, Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(8f, 0f));
                    difficulty.color = CoreHelper.GetDifficulty(level.metadata.song.difficulty).color;

                    if (ArcadeConfig.Instance.SteamLevelsRoundness.Value != 0)
                        SpriteManager.SetRoundedSprite(image, ArcadeConfig.Instance.SteamLevelsRoundness.Value, SpriteManager.RoundedSide.W);
                    else
                        image.sprite = null;

                    var title = gameObject.transform.Find("Title").GetComponent<TextMeshProUGUI>();
                    UIManager.SetRectTransform(title.rectTransform, new Vector2(8f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(170f, 132f));

                    title.fontSize = 11;
                    title.fontStyle = FontStyles.Bold;
                    title.enableWordWrapping = true;
                    title.overflowMode = TextOverflowModes.Truncate;
                    title.color = textColor;
                    title.text = $"Artist: {LSText.ClampString(level.metadata.artist.Name, 38)}\n" +
                        $"Title: {LSText.ClampString(level.metadata.song.title, 38)}\n" +
                        $"Creator: {LSText.ClampString(level.metadata.creator.steam_name, 38)}";

                    var iconBase = gameObject.transform.Find("Icon Base").GetComponent<Image>();
                    iconBase.rectTransform.anchoredPosition = new Vector2(-110f, 8f);
                    iconBase.rectTransform.sizeDelta = new Vector2(64f, 64f);

                    if (ArcadeConfig.Instance.SteamLevelsIconRoundness.Value != 0)
                        SpriteManager.SetRoundedSprite(iconBase, ArcadeConfig.Instance.SteamLevelsIconRoundness.Value, SpriteManager.RoundedSide.W);
                    else
                        iconBase.sprite = null;

                    var icon = gameObject.transform.Find("Icon Base/Icon").GetComponent<Image>();
                    icon.rectTransform.anchoredPosition = Vector2.zero;
                    icon.rectTransform.sizeDelta = new Vector2(64f, 64f);

                    icon.sprite = level.icon ?? SteamWorkshop.inst.defaultSteamImageSprite;

                    var rank = gameObject.transform.Find("Rank").GetComponent<TextMeshProUGUI>();
                    var rankShadow = gameObject.transform.Find("Rank Shadow").GetComponent<TextMeshProUGUI>();
                    rank.gameObject.SetActive(level.metadata.song.difficulty != 6);
                    rankShadow.gameObject.SetActive(level.metadata.song.difficulty != 6);

                    if (level.metadata.song.difficulty != 6)
                    {
                        UIManager.SetRectTransform(rank.rectTransform, new Vector2(115f, 20f), ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                        rank.transform.localRotation = Quaternion.Euler(0f, 0f, 356f);

                        var levelRank = LevelManager.GetLevelRank(level);
                        rank.fontSize = 42;
                        rank.text = $"<align=right><color=#{CoreHelper.ColorToHex(levelRank.color)}><b>{levelRank.name}</b></color>";

                        UIManager.SetRectTransform(rankShadow.rectTransform, new Vector2(112f, 18f), ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                        rankShadow.transform.localRotation = Quaternion.Euler(0f, 0f, 356f);

                        rankShadow.fontSize = 48;
                        rankShadow.text = $"<align=right><color=#00000035><b>{levelRank.name}</b></color>";
                    }

                    var shineController = gameObject.transform.Find("Shine").GetComponent<ShineController>();

                    shineController.maxDelay = 1f;
                    shineController.minDelay = 0.2f;
                    shineController.offset = 260f;
                    shineController.offsetOverShoot = 32f;
                    shineController.speed = 0.7f;

                    SubscribedSteamLevels.Add(new LocalLevelButton
                    {
                        Position = new Vector2Int(column, row),
                        GameObject = gameObject,
                        Clickable = clickable,
                        RectTransform = gameObject.transform.AsRT(),
                        BaseImage = image,
                        DifficultyImage = difficulty,
                        Title = title,
                        BaseIcon = iconBase,
                        Icon = icon,
                        Level = level,
                        ShineController = shineController,
                        shine1 = gameObject.transform.Find("Shine").GetComponent<Image>(),
                        shine2 = gameObject.transform.Find("Shine/Image").GetComponent<Image>(),
                        Rank = rank,
                    });
                }

                num++;
            }

            loadingSteamLevels = false;
            yield break;
        }

        public IEnumerator RefreshOnlineSteamLevels()
        {
            loadingSteamLevels = true;
            LSHelpers.DeleteChildren(RegularContents[5]);
            OnlineSteamLevels.Clear();
            int currentPage = CurrentPage[5] + 1;

            int max = currentPage * MaxSteamLevelsPerPage;

            float top = steamLevelsAlignment.y + 20f;
            float left = steamLevelsAlignment.x;

            int currentRow = -1;

            var count = SelectionLimit.Count;
            SelectionLimit.RemoveRange(2, count - 2);

            TriggerHelper.AddEventTriggers(steamPageField.gameObject, TriggerHelper.ScrollDeltaInt(steamPageField, max: int.MaxValue));

            var searchTerm = SteamSearchTerm;

            yield return new WaitUntil(() => SteamWorkshopManager.inst.SearchAsync(SteamSearchTerm, CurrentPage[5] + 1, delegate (SteamworksFacepunch.Ugc.Item item, int num)
            {
                if (searchTerm != SteamSearchTerm || currentPage != CurrentPage[5] + 1)
                {
                    return;
                }

                var gameObject = levelPrefab.Duplicate(RegularContents[5]);

                int column = (num) % 5;
                int row = (int)((num) / 5);

                if (currentRow != row)
                {
                    currentRow = row;
                    SelectionLimit.Add(1);
                }
                else
                {
                    SelectionLimit[row + 2]++;
                }

                float x = left + (column * 320f);
                float y = top - (row * 70f);

                gameObject.transform.AsRT().anchoredPosition = new Vector2(x, y);
                gameObject.transform.AsRT().sizeDelta = new Vector2(300f, 64f);

                var clickable = gameObject.GetComponent<Clickable>();
                clickable.onEnter = delegate (PointerEventData pointerEventData)
                {
                    if (!CanSelect)
                        return;

                    AudioManager.inst.PlaySound("LeftRight");
                    selected.x = column;
                    selected.y = row + 2;
                };

                var image = gameObject.GetComponent<Image>();
                image.color = buttonBGColor;

                Destroy(gameObject.transform.Find("Difficulty").gameObject);

                if (ArcadeConfig.Instance.SteamLevelsRoundness.Value != 0)
                    SpriteManager.SetRoundedSprite(image, ArcadeConfig.Instance.SteamLevelsRoundness.Value, SpriteManager.RoundedSide.W);
                else
                    image.sprite = null;

                var title = gameObject.transform.Find("Title").GetComponent<TextMeshProUGUI>();
                UIManager.SetRectTransform(title.rectTransform, new Vector2(8f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(170f, 132f));

                title.fontSize = 11;
                title.fontStyle = FontStyles.Bold;
                title.enableWordWrapping = true;
                title.overflowMode = TextOverflowModes.Truncate;
                title.color = textColor;
                title.text = $"Title: {LSText.ClampString(item.Title, 38)}\n" +
                    $"Creator: {LSText.ClampString(item.Owner.Name, 38)}\n" +
                    $"Subscribed: {(item.IsSubscribed ? "Yes" : "No")}";

                var iconBase = gameObject.transform.Find("Icon Base").GetComponent<Image>();
                iconBase.rectTransform.anchoredPosition = new Vector2(-110f, 0f);
                iconBase.rectTransform.sizeDelta = new Vector2(48f, 48f);

                if (ArcadeConfig.Instance.SteamLevelsIconRoundness.Value != 0)
                    SpriteManager.SetRoundedSprite(iconBase, ArcadeConfig.Instance.SteamLevelsIconRoundness.Value, SpriteManager.RoundedSide.W);
                else
                    iconBase.sprite = null;

                var icon = gameObject.transform.Find("Icon Base/Icon").GetComponent<Image>();
                icon.rectTransform.anchoredPosition = Vector2.zero;
                icon.rectTransform.sizeDelta = new Vector2(48f, 48f);

                if (!steamPreviews.ContainsKey(item.Id))
                {
                    StartCoroutine(AlephNetworkManager.DownloadBytes(item.PreviewImageUrl, delegate (byte[] bytes)
                    {
                        var sprite = SpriteManager.LoadSprite(bytes);

                        if (!steamPreviews.ContainsKey(item.Id))
                            steamPreviews.Add(item.Id, sprite);

                        try
                        {
                            icon.sprite = sprite;
                        }
                        catch
                        {

                        }
                    }, delegate (string onError)
                    {
                        var sprite = SteamWorkshop.inst.defaultSteamImageSprite;

                        if (!steamPreviews.ContainsKey(item.Id))
                            steamPreviews.Add(item.Id, sprite);

                        icon.sprite = sprite;
                    }));
                }
                else
                {
                    icon.sprite = steamPreviews[item.Id];
                }

                Destroy(gameObject.transform.Find("Rank").gameObject);
                Destroy(gameObject.transform.Find("Rank Shadow").gameObject);
                Destroy(gameObject.transform.Find("Shine").gameObject);

                var button = new SteamLevelButton
                {
                    Position = new Vector2Int(column, row),
                    Title = item.Title,
                    Description = item.Description,
                    Creator = item.Owner.Name,
                    GameObject = gameObject,
                    Clickable = clickable,
                    RectTransform = gameObject.transform.AsRT(),
                    BaseImage = image,
                    TitleText = title,
                    BaseIcon = iconBase,
                    Icon = icon,
                    Item = item,
                };

                clickable.onClick = delegate (PointerEventData pointerEventData)
                {
                    AudioManager.inst.PlaySound("blip");
                    SteamLevelMenuManager.inst?.OpenLevel(button);
                };

                OnlineSteamLevels.Add(button);
            }).IsCompleted);

            //int num = 0;
            //foreach (var level in SteamWorkshopManager.inst.SearchItems)
            //{
            //    var gameObject = localLevelPrefab.Duplicate(RegularContents[5]);

            //    int column = (num) % 5;
            //    int row = (int)((num) / 5);

            //    if (currentRow != row)
            //    {
            //        currentRow = row;
            //        SelectionLimit.Add(1);
            //    }
            //    else
            //    {
            //        SelectionLimit[row + 2]++;
            //    }

            //    float x = left + (column * 320f);
            //    float y = top - (row * 110f);

            //    gameObject.transform.AsRT().anchoredPosition = new Vector2(x, y);
            //    gameObject.transform.AsRT().sizeDelta = new Vector2(300f, 94f);

            //    var clickable = gameObject.GetComponent<Clickable>();
            //    clickable.onEnter = delegate (PointerEventData pointerEventData)
            //    {
            //        if (!CanSelect)
            //            return;

            //        AudioManager.inst.PlaySound("LeftRight");
            //        selected.x = column;
            //        selected.y = row + 2;
            //    };

            //    var image = gameObject.GetComponent<Image>();
            //    image.color = buttonBGColor;

            //    Destroy(gameObject.transform.Find("Difficulty").gameObject);

            //    if (ArcadePlugin.SteamLevelsRoundness.Value != 0)
            //        SpriteManager.SetRoundedSprite(image, ArcadePlugin.SteamLevelsRoundness.Value, SpriteManager.RoundedSide.W);
            //    else
            //        image.sprite = null;

            //    var title = gameObject.transform.Find("Title").GetComponent<TextMeshProUGUI>();
            //    UIManager.SetRectTransform(title.rectTransform, new Vector2(8f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(170f, 132f));

            //    title.fontSize = 11;
            //    title.fontStyle = FontStyles.Bold;
            //    title.enableWordWrapping = true;
            //    title.overflowMode = TextOverflowModes.Truncate;
            //    title.color = textColor;
            //    title.text = $"Title: {LSText.ClampString(level.Title, 38)}\n" +
            //        $"Creator: {LSText.ClampString(level.Owner.Name, 38)}\n" +
            //        $"Subscribed: {(level.IsSubscribed ? "Yes" : "No")}";

            //    var iconBase = gameObject.transform.Find("Icon Base").GetComponent<Image>();
            //    iconBase.rectTransform.anchoredPosition = new Vector2(-110f, 8f);
            //    iconBase.rectTransform.sizeDelta = new Vector2(64f, 64f);

            //    if (ArcadePlugin.SteamLevelsIconRoundness.Value != 0)
            //        SpriteManager.SetRoundedSprite(iconBase, ArcadePlugin.SteamLevelsIconRoundness.Value, SpriteManager.RoundedSide.W);
            //    else
            //        iconBase.sprite = null;

            //    var icon = gameObject.transform.Find("Icon Base/Icon").GetComponent<Image>();
            //    icon.rectTransform.anchoredPosition = Vector2.zero;
            //    icon.rectTransform.sizeDelta = new Vector2(64f, 64f);

            //    if (!steamPreviews.ContainsKey(level.Id))
            //    {
            //        StartCoroutine(AlephNetworkManager.DownloadBytes(level.PreviewImageUrl, delegate (byte[] bytes)
            //        {
            //            var sprite = SpriteManager.LoadSprite(bytes);

            //            if (!steamPreviews.ContainsKey(level.Id))
            //                steamPreviews.Add(level.Id, sprite);

            //            try
            //            {
            //                icon.sprite = sprite;
            //            }
            //            catch
            //            {

            //            }
            //        }, delegate (string onError)
            //        {
            //            var sprite = SteamWorkshop.inst.defaultSteamImageSprite;

            //            if (!steamPreviews.ContainsKey(level.Id))
            //                steamPreviews.Add(level.Id, sprite);

            //            icon.sprite = sprite;
            //        }));
            //    }
            //    else
            //    {
            //        icon.sprite = steamPreviews[level.Id];
            //    }

            //    Destroy(gameObject.transform.Find("Rank").gameObject);
            //    Destroy(gameObject.transform.Find("Rank Shadow").gameObject);
            //    Destroy(gameObject.transform.Find("Shine").gameObject);

            //    var button = new SteamLevelButton
            //    {
            //        Position = new Vector2Int(column, row),
            //        Title = level.Title,
            //        Description = level.Description,
            //        Creator = level.Owner.Name,
            //        GameObject = gameObject,
            //        Clickable = clickable,
            //        RectTransform = gameObject.transform.AsRT(),
            //        BaseImage = image,
            //        TitleText = title,
            //        BaseIcon = iconBase,
            //        Icon = icon,
            //        Item = level,
            //    };

            //    clickable.onClick = delegate (PointerEventData pointerEventData)
            //    {
            //        AudioManager.inst.PlaySound("blip");
            //        SteamLevelMenuManager.inst?.OpenLevel(button);
            //    };

            //    OnlineSteamLevels.Add(button);

            //    num++;
            //}

            loadingSteamLevels = false;
            yield break;
        }

        public Dictionary<SteamworksFacepunch.Data.PublishedFileId, Sprite> steamPreviews = new Dictionary<SteamworksFacepunch.Data.PublishedFileId, Sprite>();

        public void UpdateSteamLevelsRoundness()
        {
            if (steamViewType == SteamViewType.Subscribed)
                foreach (var level in SubscribedSteamLevels)
                {
                    if (ArcadeConfig.Instance.SteamLevelsRoundness.Value != 0)
                        SpriteManager.SetRoundedSprite(level.BaseImage, ArcadeConfig.Instance.SteamLevelsRoundness.Value, SpriteManager.RoundedSide.W);
                    else
                        level.BaseImage.sprite = null;

                    if (ArcadeConfig.Instance.SteamLevelsIconRoundness.Value != 0)
                        SpriteManager.SetRoundedSprite(level.BaseIcon, ArcadeConfig.Instance.SteamLevelsIconRoundness.Value, SpriteManager.RoundedSide.W);
                    else
                        level.BaseIcon.sprite = null;
                }

            if (steamViewType == SteamViewType.Online)
                foreach (var level in OnlineSteamLevels)
                {
                    if (ArcadeConfig.Instance.SteamLevelsRoundness.Value != 0)
                        SpriteManager.SetRoundedSprite(level.BaseImage, ArcadeConfig.Instance.SteamLevelsRoundness.Value, SpriteManager.RoundedSide.W);
                    else
                        level.BaseImage.sprite = null;

                    if (ArcadeConfig.Instance.SteamLevelsIconRoundness.Value != 0)
                        SpriteManager.SetRoundedSprite(level.BaseIcon, ArcadeConfig.Instance.SteamLevelsIconRoundness.Value, SpriteManager.RoundedSide.W);
                    else
                        level.BaseIcon.sprite = null;
                }
        }

        #endregion

        public class SteamLevelButton
        {
            public SteamLevelButton()
            {

            }

            public string Title { get; set; } = string.Empty;
            public string Creator { get; set; } = string.Empty;

            public string Description { get; set; } = string.Empty;

            public Vector2Int Position { get; set; }

            public GameObject GameObject { get; set; }
            public RectTransform RectTransform { get; set; }
            public TextMeshProUGUI TitleText { get; set; }
            public Image BaseImage { get; set; }
            public Image BaseIcon { get; set; }
            public Image Icon { get; set; }

            public Clickable Clickable { get; set; }

            public RTAnimation EnterAnimation { get; set; }
            public RTAnimation ExitAnimation { get; set; }

            public bool selected;

            public SteamworksFacepunch.Ugc.Item Item { get; set; }

            public void Subscribe()
            {
                Item.Subscribe();

                Item.Download();

                CoreHelper.Log($"{Item.Directory}");
            }

            public void Unsubscribe()
            {
                var id = Item.Id;

                Item.Unsubscribe();

                if (SteamWorkshopManager.inst.Levels.Has(x => x.id == id.ToString()))
                {
                    SteamWorkshopManager.inst.Levels.RemoveAll(x => x.id == id.ToString());
                }

                if (inst.SubscribedSteamLevels.Has(x => x.Level.id == id.ToString()) && inst.steamViewType == SteamViewType.Subscribed)
                {
                    inst.StartCoroutine(inst.RefreshSubscribedSteamLevels());
                }
            }
        }

        public class OnlineLevelButton
        {
            public OnlineLevelButton()
            {

            }

            public string ID { get; set; } = string.Empty;

            public string Artist { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Creator { get; set; } = string.Empty;

            public string Description { get; set; } = string.Empty;
            public int Difficulty { get; set; }

            public Vector2Int Position { get; set; }

            public GameObject GameObject { get; set; }
            public RectTransform RectTransform { get; set; }
            public TextMeshProUGUI TitleText { get; set; }
            public Image BaseImage { get; set; }
            public Image BaseIcon { get; set; }
            public Image Icon { get; set; }
            public Image DifficultyImage { get; set; }

            public Clickable Clickable { get; set; }

            public RTAnimation EnterAnimation { get; set; }
            public RTAnimation ExitAnimation { get; set; }

            public bool selected;

            public IEnumerator DownloadLevel()
            {
                var directory = $"{RTFile.ApplicationDirectory}{LevelManager.ListSlash}{ID}";

                if (LevelManager.Levels.Has(x => x.id == ID) || RTFile.DirectoryExists(directory))
                {
                    CoreHelper.LogError($"Level already exists! No update system in place yet.");

                    yield break;
                }

                yield return inst.StartCoroutine(AlephNetworkManager.DownloadBytes($"{DownloadURL}{ID}", delegate (byte[] bytes)
                {
                    Directory.CreateDirectory(directory);

                    File.WriteAllBytes($"{directory}.zip", bytes);

                    ZipFile.ExtractToDirectory($"{directory}.zip", $"{directory}");

                    File.Delete($"{directory}.zip");

                    MetaData metadata = RTFile.FileExists($"{directory}/metadata.vgm") ? MetaData.ParseVG(JSON.Parse(RTFile.ReadFromFile($"{directory}/metadata.vgm"))) : MetaData.Parse(JSON.Parse(RTFile.ReadFromFile($"{directory}/metadata.lsb")));

                    var level = new Level(directory + "/");

                    LevelManager.Levels.Add(level);

                    if (inst.CurrentTab == 0)
                    {
                        inst.StartCoroutine(inst.RefreshLocalLevels());
                    }
                    else if (inst.OpenedOnlineLevel)
                    {
                        DownloadLevelMenuManager.inst.Close(delegate ()
                        {
                            if (ArcadeConfig.Instance.OpenOnlineLevelAfterDownload.Value)
                            {
                                inst.StartCoroutine(inst.SelectLocalLevel(level));
                            }
                        });
                    }
                }));

                yield break;
            }
        }

        public class LocalLevelButton
        {
            public LocalLevelButton()
            {

            }

            public Vector2Int Position { get; set; }

            public Level Level { get; set; }

            public GameObject GameObject { get; set; }
            public RectTransform RectTransform { get; set; }
            public TextMeshProUGUI Title { get; set; }
            public TextMeshProUGUI Rank { get; set; }
            public Image BaseImage { get; set; }
            public Image BaseIcon { get; set; }
            public Image Icon { get; set; }
            public Image DifficultyImage { get; set; }

            public Clickable Clickable { get; set; }

            public ShineController ShineController { get; set; }
            public Image shine1;
            public Image shine2;

            public RTAnimation EnterAnimation { get; set; }
            public RTAnimation ExitAnimation { get; set; }

            public bool selected;
        }

        public class Tab
        {
            public GameObject GameObject { get; set; }
            public RectTransform RectTransform { get; set; }
            public TextMeshProUGUI Text { get; set; }
            public Image Image { get; set; }
            public Clickable Clickable { get; set; }

            public Vector2Int Position { get; set; }
        }
    }

    public static class ArcadeExtension
    {
        static readonly Dictionary<string, string> namespaceHelpers = new Dictionary<string, string>
        {
            { "UnityEngine.GameObject", "GameObject" },
            { "UnityEngine.RectTransform", "RectTransform" },
            { "UnityEngine.UI.Image", "Image" },
            { "UnityEngine.UI.Text", "Text" },
            { "UnityEngine.UI.Button", "Button" },
            { "UnityEngine.UI.Toggle", "Toggle" },
            { "UnityEngine.UI.InputField", "InputField" },
            { "UnityEngine.UI.Dropdown", "Dropdown" },
            { "TMPro.TextMeshProUGUI", "Text" },
        };

        public static T GetObject<T>(this Dictionary<string, object> dictionary)
            => namespaceHelpers.ContainsKey(typeof(T).ToString()) && dictionary.ContainsKey(namespaceHelpers[typeof(T).ToString()]) ?
            (T)dictionary[namespaceHelpers[typeof(T).ToString()]] : default;
    }
}

#pragma warning restore CS0618 // Type or member is obsolete