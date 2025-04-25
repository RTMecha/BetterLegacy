using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using SimpleJSON;
using SteamworksFacepunch.Ugc;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Menus;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Layouts;
using BetterLegacy.Menus.UI.Interfaces;

namespace BetterLegacy.Arcade.Interfaces
{
    public class ArcadeMenu : MenuBase
    {
        public static ArcadeMenu Current { get; set; }

        public enum Tab
        {
            Local,
            Online,
            Browser, // also allows you to download
            Queue,
            Steam
        }

        public static Tab CurrentTab { get; set; }
        public static int[] Pages { get; set; } = new int[]
        {
            0, // Local
            0, // Online
            0, // Browser
            0, // Queue
            0, // Steam
        };

        public static bool ViewOnline { get; set; }

        public const int MAX_LEVELS_PER_PAGE = 20;
        public const int MAX_QUEUED_PER_PAGE = 6;
        public const int MAX_STEAM_ONLINE_LEVELS_PER_PAGE = 50;

        public static string[] Searches { get; set; } = new string[]
        {
            "", // Local
            "", // Online
            "", // Browser
            "", // Queue
            "", // Steam
        };

        public ArcadeMenu() : base()
        {
            InterfaceManager.inst.CurrentInterface = this;
            name = "Arcade";

            ArcadeHelper.ResetModifiedStates();

            regenerate = false;

            elements.Add(new MenuEvent
            {
                id = "09",
                name = "Effects",
                func = MenuEffectsManager.inst.SetDefaultEffects,
                length = 0f,
                regenerate = false,
            });

            layouts.Add("tabs", new MenuHorizontalLayout
            {
                name = "tabs",
                rect = RectValues.HorizontalAnchored.AnchoredPosition(0f, 450f).SizeDelta(-126f, 100f),
                childControlWidth = true,
                childForceExpandWidth = true,
                spacing = 12f,
                regenerate = false,
            });

            elements.Add(new MenuButton
            {
                id = "0",
                name = "Close",
                parentLayout = "tabs",
                selectionPosition = Vector2Int.zero,
                text = "<align=center><b>[ RETURN ]",
                func = Exit,
                color = 6,
                opacity = 0.1f,
                textColor = 6,
                selectedColor = 6,
                selectedOpacity = 1f,
                selectedTextColor = 7,
                length = 0.1f,
                wait = false,
                regenerate = false,
            });

            for (int i = 0; i < 5; i++)
            {
                int index = i;
                elements.Add(new MenuButton
                {
                    id = (i + 1).ToString(),
                    name = "Tab",
                    parentLayout = "tabs",
                    selectionPosition = new Vector2Int(i + 1, 0),
                    text = $"<align=center><b>[ {((Tab)i).ToString().ToUpper()} ]",
                    func = () =>
                    {
                        CurrentTab = (Tab)index;
                        Init();
                    },
                    color = 6,
                    opacity = 0.1f,
                    textColor = 6,
                    selectedColor = 6,
                    selectedOpacity = 1f,
                    selectedTextColor = 7,
                    length = 0.1f,
                    wait = false,
                    regenerate = false,
                });
            }

            var currentPage = Pages[(int)CurrentTab];
            var currentSearch = Searches[(int)CurrentTab];

            switch (CurrentTab)
            {
                case Tab.Local: {
                        layouts.Add("local settings", new MenuHorizontalLayout
                        {
                            name = "local settings",
                            rect = RectValues.HorizontalAnchored.AnchoredPosition(0f, 350f).SizeDelta(-126f, 64f),
                            childForceExpandWidth = true,
                            regenerate = false,
                        });

                        elements.Add(new MenuInputField
                        {
                            id = "842848",
                            name = "Search Bar",
                            parentLayout = "local settings",
                            rect = RectValues.Default.SizeDelta(704f, 64f),
                            text = currentSearch,
                            valueChangedFunc = SearchLocalLevels,
                            placeholder = "Search levels...",
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            placeholderColor = 6,
                            length = 0.1f,
                            wait = false,
                            regenerate = false,
                        });

                        elements.Add(new MenuButton
                        {
                            id = "25428852",
                            name = "Reload Button",
                            text = "<align=center><b>[ RELOAD ]",
                            parentLayout = "local settings",
                            selectionPosition = new Vector2Int(0, 1),
                            rect = RectValues.Default.SizeDelta(200f, 64f),
                            func = LoadLevelsMenu.Init,
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            selectedColor = 6,
                            selectedOpacity = 1f,
                            selectedTextColor = 7,
                            length = 0.1f,
                            regenerate = false,
                        });

                        var sortButton = new MenuButton
                        {
                            id = "25428852",
                            name = "Sort Button",
                            text = $"<align=center><b>[ SORT: {ArcadeConfig.Instance.LocalLevelOrderby.Value} ]",
                            parentLayout = "local settings",
                            selectionPosition = new Vector2Int(1, 1),
                            rect = RectValues.Default.SizeDelta(400f, 64f),
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            selectedColor = 6,
                            selectedOpacity = 1f,
                            selectedTextColor = 7,
                            length = 0.1f,
                            regenerate = false,
                        };
                        sortButton.func = () =>
                        {
                            var num = (int)ArcadeConfig.Instance.LocalLevelOrderby.Value;
                            num++;
                            if (num >= Enum.GetNames(typeof(LevelSort)).Length)
                                num = 0;
                            ArcadeConfig.Instance.LocalLevelOrderby.Value = (LevelSort)num;
                            sortButton.text = $"<align=center><b>[ SORT: {ArcadeConfig.Instance.LocalLevelOrderby.Value} ]";
                            if (sortButton.textUI)
                            {
                                sortButton.textUI.maxVisibleCharacters = 9999;
                                sortButton.textUI.text = sortButton.text;
                            }
                        };
                        elements.Add(sortButton);

                        var ascendButton = new MenuButton
                        {
                            id = "25428852",
                            name = "Sort Button",
                            text = $"<align=center><b><rotate={(ArcadeConfig.Instance.LocalLevelAscend.Value ? "90" : "-90")}>>",
                            parentLayout = "local settings",
                            selectionPosition = new Vector2Int(2, 1),
                            rect = RectValues.Default.SizeDelta(64f, 64f),
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            selectedColor = 6,
                            selectedOpacity = 1f,
                            selectedTextColor = 7,
                            length = 0.1f,
                            regenerate = false,
                        };
                        ascendButton.func = () =>
                        {
                            ArcadeConfig.Instance.LocalLevelAscend.Value = !ArcadeConfig.Instance.LocalLevelAscend.Value;
                            ascendButton.text = $"<align=center><b><rotate={(ArcadeConfig.Instance.LocalLevelAscend.Value ? "90" : "-90")}>>";
                            if (ascendButton.textUI)
                            {
                                ascendButton.textUI.maxVisibleCharacters = 9999;
                                ascendButton.textUI.text = ascendButton.text;
                            }
                        };
                        elements.Add(ascendButton);

                        var pageField = new MenuInputField
                        {
                            id = "842848",
                            name = "Page Bar",
                            parentLayout = "local settings",
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            text = currentPage.ToString(),
                            textAnchor = TextAnchor.MiddleCenter,
                            valueChangedFunc = _val => SetLocalLevelsPage(Parser.TryParse(_val, Pages[(int)CurrentTab])),
                            placeholder = "Set page...",
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            placeholderColor = 6,
                            length = 0.1f,
                            wait = false,
                            regenerate = false,
                        };
                        pageField.triggers = new EventTrigger.Entry[]
                        {
                            TriggerHelper.CreateEntry(EventTriggerType.Scroll, eventData =>
                            {
                                var pointerEventData = (PointerEventData)eventData;

                                var inputField = pageField.inputField;
                                if (!inputField)
                                    return;

                                if (int.TryParse(inputField.text, out int result))
                                {
                                    bool large = Input.GetKey(KeyCode.LeftControl);

                                    if (pointerEventData.scrollDelta.y < 0f)
                                        result -= 1 * (large ? 10 : 1);
                                    if (pointerEventData.scrollDelta.y > 0f)
                                        result += 1 * (large ? 10 : 1);

                                    if (LocalLevelPageCount != 0)
                                        result = Mathf.Clamp(result, 0, LocalLevelPageCount);

                                    if (inputField.text != result.ToString())
                                    {
                                        inputField.text = result.ToString();
                                        SoundManager.inst.PlaySound(DefaultSounds.menuflip);
                                    }
                                }
                            }),
                        };

                        elements.Add(new MenuButton
                        {
                            id = "32848924",
                            name = "Prev Page",
                            text = "<align=center><b><",
                            parentLayout = "local settings",
                            selectionPosition = new Vector2Int(3, 1),
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            func = () =>
                            {
                                if (Pages[(int)CurrentTab] != 0 && pageField.inputField)
                                {
                                    pageField.inputField.text = (Pages[(int)CurrentTab] - 1).ToString();
                                    SoundManager.inst.PlaySound(DefaultSounds.menuflip);
                                }
                                else
                                    SoundManager.inst.PlaySound(DefaultSounds.Block);
                            },
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            selectedColor = 6,
                            selectedOpacity = 1f,
                            selectedTextColor = 7,
                            length = 0.1f,
                            regenerate = false,
                            playBlipSound = false,
                        });

                        elements.Add(pageField);

                        elements.Add(new MenuButton
                        {
                            id = "32848924",
                            name = "Next Page",
                            text = "<align=center><b>>",
                            parentLayout = "local settings",
                            selectionPosition = new Vector2Int(4, 1),
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            func = () =>
                            {
                                if (Pages[(int)CurrentTab] != LocalLevelPageCount)
                                {
                                    pageField.inputField.text = (Pages[(int)CurrentTab] + 1).ToString();
                                    SoundManager.inst.PlaySound(DefaultSounds.menuflip);
                                }
                                else
                                    SoundManager.inst.PlaySound(DefaultSounds.Block);
                            },
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            selectedColor = 6,
                            selectedOpacity = 1f,
                            selectedTextColor = 7,
                            length = 0.1f,
                            regenerate = false,
                            playBlipSound = false,
                        });

                        layouts.Add("levels", new MenuGridLayout
                        {
                            name = "levels",
                            rect = RectValues.Default.AnchoredPosition(-500f, 100f).SizeDelta(800f, 400f),
                            cellSize = new Vector2(350f, 180f),
                            spacing = new Vector2(12f, 12f),
                            constraint = GridLayoutGroup.Constraint.FixedColumnCount,
                            constraintCount = 5,
                            regenerate = false,
                        });

                        RefreshLocalLevels(false, false);

                        break;
                    }
                case Tab.Online: {
                        layouts.Add("online settings", new MenuHorizontalLayout
                        {
                            name = "online settings",
                            rect = RectValues.HorizontalAnchored.AnchoredPosition(0f, 350f).SizeDelta(-126f, 64f),
                            childForceExpandWidth = true,
                            regenerate = false,
                        });

                        elements.Add(new MenuInputField
                        {
                            id = "842848",
                            name = "Search Bar",
                            parentLayout = "online settings",
                            //rect = RectValues.HorizontalAnchored.AnchoredPosition(0f, 350f).SizeDelta(-126f, 64f),
                            rect = RectValues.Default.SizeDelta(1300, 64f),
                            text = currentSearch,
                            valueChangedFunc = SearchOnlineLevels,
                            placeholder = "Search levels...",
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            placeholderColor = 6,
                            length = 0f,
                            wait = false,
                            regenerate = false,
                        });

                        elements.Add(new MenuButton
                        {
                            id = "25428852",
                            name = "Search Button",
                            text = "<align=center><b>[ SEARCH ]",
                            parentLayout = "online settings",
                            selectionPosition = new Vector2Int(0, 1),
                            rect = RectValues.Default.SizeDelta(200f, 64f),
                            func = RefreshOnlineLevels().Start,
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            selectedColor = 6,
                            selectedOpacity = 1f,
                            selectedTextColor = 7,
                            length = 0.1f,
                            regenerate = false,
                        });

                        elements.Add(new MenuButton
                        {
                            id = "32848924",
                            name = "Prev Page",
                            text = "<align=center><b><",
                            parentLayout = "online settings",
                            selectionPosition = new Vector2Int(1, 1),
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            func = () =>
                            {
                                if (Pages[(int)CurrentTab] != 0)
                                    SetOnlineLevelsPage(Pages[(int)CurrentTab] - 1);
                                else
                                    SoundManager.inst.PlaySound(DefaultSounds.Block);
                            },
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            selectedColor = 6,
                            selectedOpacity = 1f,
                            selectedTextColor = 7,
                            length = 0.1f,
                            regenerate = false,
                        });

                        elements.Add(new MenuButton
                        {
                            id = "32848924",
                            name = "Next Page",
                            text = "<align=center><b>>",
                            parentLayout = "online settings",
                            selectionPosition = new Vector2Int(2, 1),
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            func = () => SetOnlineLevelsPage(Pages[(int)CurrentTab] + 1),
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            selectedColor = 6,
                            selectedOpacity = 1f,
                            selectedTextColor = 7,
                            length = 0.1f,
                            regenerate = false,
                        });

                        layouts.Add("levels", new MenuGridLayout
                        {
                            name = "levels",
                            rect = RectValues.Default.AnchoredPosition(-500f, 100f).SizeDelta(800f, 400f),
                            cellSize = new Vector2(350f, 180f),
                            spacing = new Vector2(12f, 12f),
                            constraint = GridLayoutGroup.Constraint.FixedColumnCount,
                            constraintCount = 5,
                            regenerate = false,
                        });

                        break;
                    }
                case Tab.Browser: {
                        layouts.Add("browser settings", new MenuHorizontalLayout
                        {
                            name = "browser settings",
                            rect = RectValues.HorizontalAnchored.AnchoredPosition(0f, 350f).SizeDelta(-126f, 64f),
                            childForceExpandWidth = true,
                            regenerate = false,
                        });

                        elements.Add(new MenuInputField
                        {
                            id = "842848",
                            name = "Search Bar",
                            parentLayout = "browser settings",
                            rect = RectValues.Default.SizeDelta(1368f, 64f),
                            text = currentSearch,
                            valueChangedFunc = SearchBrowser,
                            placeholder = "Search files...",
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            placeholderColor = 6,
                            length = 0.1f,
                            wait = false,
                            regenerate = false,
                        });

                        var pageField = new MenuInputField
                        {
                            id = "842848",
                            name = "Page Bar",
                            parentLayout = "browser settings",
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            text = currentPage.ToString(),
                            textAnchor = TextAnchor.MiddleCenter,
                            valueChangedFunc = _val => SetBrowserPage(Parser.TryParse(_val, Pages[(int)CurrentTab])),
                            placeholder = "Set page...",
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            placeholderColor = 6,
                            length = 0.1f,
                            wait = false,
                            regenerate = false,
                        };
                        pageField.triggers = new EventTrigger.Entry[]
                        {
                            TriggerHelper.CreateEntry(EventTriggerType.Scroll, eventData =>
                            {
                                var pointerEventData = (PointerEventData)eventData;

                                var inputField = pageField.inputField;
                                if (!inputField)
                                    return;

                                if (int.TryParse(inputField.text, out int result))
                                {
                                    bool large = Input.GetKey(KeyCode.LeftControl);

                                    if (pointerEventData.scrollDelta.y < 0f)
                                        result -= 1 * (large ? 10 : 1);
                                    if (pointerEventData.scrollDelta.y > 0f)
                                        result += 1 * (large ? 10 : 1);

                                    if (BrowserPageCount != 0)
                                        result = Mathf.Clamp(result, 0, BrowserPageCount);

                                    if (inputField.text != result.ToString())
                                    {
                                        inputField.text = result.ToString();
                                        SoundManager.inst.PlaySound(DefaultSounds.menuflip);
                                    }
                                }
                            }),
                        };

                        elements.Add(new MenuButton
                        {
                            id = "32848924",
                            name = "Prev Page",
                            text = "<align=center><b><",
                            parentLayout = "browser settings",
                            selectionPosition = new Vector2Int(0, 1),
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            func = () =>
                            {
                                if (Pages[(int)CurrentTab] != 0 && pageField.inputField)
                                {
                                    pageField.inputField.text = (Pages[(int)CurrentTab] - 1).ToString();
                                    SoundManager.inst.PlaySound(DefaultSounds.menuflip);
                                }
                                else
                                    SoundManager.inst.PlaySound(DefaultSounds.Block);
                            },
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            selectedColor = 6,
                            selectedOpacity = 1f,
                            selectedTextColor = 7,
                            length = 0.1f,
                            regenerate = false,
                            playBlipSound = false,
                        });

                        elements.Add(pageField);

                        elements.Add(new MenuButton
                        {
                            id = "32848924",
                            name = "Next Page",
                            text = "<align=center><b>>",
                            parentLayout = "browser settings",
                            selectionPosition = new Vector2Int(1, 1),
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            func = () =>
                            {
                                if (Pages[(int)CurrentTab] != BrowserPageCount)
                                {
                                    pageField.inputField.text = (Pages[(int)CurrentTab] + 1).ToString();
                                    SoundManager.inst.PlaySound(DefaultSounds.menuflip);
                                }
                                else
                                    SoundManager.inst.PlaySound(DefaultSounds.Block);
                            },
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            selectedColor = 6,
                            selectedOpacity = 1f,
                            selectedTextColor = 7,
                            length = 0.1f,
                            regenerate = false,
                            playBlipSound = false,
                        });

                        layouts.Add("levels", new MenuGridLayout
                        {
                            name = "levels",
                            rect = RectValues.Default.AnchoredPosition(-500f, 100f).SizeDelta(800f, 400f),
                            cellSize = new Vector2(350f, 180f),
                            spacing = new Vector2(12f, 12f),
                            constraint = GridLayoutGroup.Constraint.FixedColumnCount,
                            constraintCount = 5,
                            regenerate = false,
                        });

                        RefreshBrowserLevels(false);

                        // todo: make browser download zip levels and also browse local files for a level.
                        break;
                    }
                case Tab.Queue: {
                        layouts.Add("queue settings", new MenuHorizontalLayout
                        {
                            name = "queue settings",
                            rect = RectValues.HorizontalAnchored.AnchoredPosition(0f, 350f).SizeDelta(-126f, 64f),
                            childForceExpandWidth = true,
                            regenerate = false,
                        });

                        elements.Add(new MenuInputField
                        {
                            id = "842848",
                            name = "Search Bar",
                            parentLayout = "queue settings",
                            rect = RectValues.Default.SizeDelta(368f, 64f),
                            text = currentSearch,
                            valueChangedFunc = SearchQueuedLevels,
                            placeholder = "Search levels...",
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            placeholderColor = 6,
                            length = 0.1f,
                            wait = false,
                            regenerate = false,
                        });

                        elements.Add(new MenuButton
                        {
                            id = "25428852",
                            name = "Shuffle Button",
                            text = "<align=center><b>[ SHUFFLE ]",
                            parentLayout = "queue settings",
                            selectionPosition = new Vector2Int(0, 1),
                            rect = RectValues.Default.SizeDelta(200f, 64f),
                            func = () => ShuffleQueue(false),
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            selectedColor = 6,
                            selectedOpacity = 1f,
                            selectedTextColor = 7,
                            length = 0.1f,
                            regenerate = false,
                        });
                        
                        elements.Add(new MenuButton
                        {
                            id = "25428852",
                            name = "Shuffle Button",
                            text = "<align=center><b>[ PLAY ]",
                            parentLayout = "queue settings",
                            selectionPosition = new Vector2Int(1, 1),
                            rect = RectValues.Default.SizeDelta(200f, 64f),
                            func = StartQueue,
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            selectedColor = 6,
                            selectedOpacity = 1f,
                            selectedTextColor = 7,
                            length = 0.1f,
                            regenerate = false,
                        });

                        elements.Add(new MenuButton
                        {
                            id = "25428852",
                            name = "Clear Button",
                            text = "<align=center><b>[ CLEAR ]",
                            parentLayout = "queue settings",
                            selectionPosition = new Vector2Int(2, 1),
                            rect = RectValues.Default.SizeDelta(200f, 64f),
                            func = () =>
                            {
                                LevelManager.ArcadeQueue.Clear();
                                RefreshQueueLevels(true);
                            },
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            selectedColor = 6,
                            selectedOpacity = 1f,
                            selectedTextColor = 7,
                            length = 0.1f,
                            regenerate = false,
                        });
                        
                        elements.Add(new MenuButton
                        {
                            id = "25428852",
                            name = "Copy Button",
                            text = "<align=center><b>[ COPY ]",
                            parentLayout = "queue settings",
                            selectionPosition = new Vector2Int(3, 1),
                            rect = RectValues.Default.SizeDelta(200f, 64f),
                            func = ArcadeHelper.CopyArcadeQueue,
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            selectedColor = 6,
                            selectedOpacity = 1f,
                            selectedTextColor = 7,
                            length = 0.1f,
                            regenerate = false,
                        });
                        
                        elements.Add(new MenuButton
                        {
                            id = "25428852",
                            name = "Copy Button",
                            text = "<align=center><b>[ PASTE ]",
                            parentLayout = "queue settings",
                            selectionPosition = new Vector2Int(4, 1),
                            rect = RectValues.Default.SizeDelta(200f, 64f),
                            func = ArcadeHelper.PasteArcadeQueue,
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            selectedColor = 6,
                            selectedOpacity = 1f,
                            selectedTextColor = 7,
                            length = 0.1f,
                            regenerate = false,
                        });

                        var pageField = new MenuInputField
                        {
                            id = "842848",
                            name = "Page Bar",
                            parentLayout = "queue settings",
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            text = currentPage.ToString(),
                            textAnchor = TextAnchor.MiddleCenter,
                            valueChangedFunc = _val => SetQueuedLevelsPage(Parser.TryParse(_val, Pages[(int)CurrentTab])),
                            placeholder = "Set page...",
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            placeholderColor = 6,
                            length = 0.1f,
                            wait = false,
                            regenerate = false,
                        };
                        pageField.triggers = new EventTrigger.Entry[]
                        {
                            TriggerHelper.CreateEntry(EventTriggerType.Scroll, eventData =>
                            {
                                var pointerEventData = (PointerEventData)eventData;

                                var inputField = pageField.inputField;
                                if (!inputField)
                                    return;

                                if (int.TryParse(inputField.text, out int result))
                                {
                                    bool large = Input.GetKey(KeyCode.LeftControl);

                                    if (pointerEventData.scrollDelta.y < 0f)
                                        result -= 1 * (large ? 10 : 1);
                                    if (pointerEventData.scrollDelta.y > 0f)
                                        result += 1 * (large ? 10 : 1);

                                    if (QueuePageCount != 0)
                                        result = Mathf.Clamp(result, 0, QueuePageCount);

                                    if (inputField.text != result.ToString())
                                    {
                                        inputField.text = result.ToString();
                                        SoundManager.inst.PlaySound(DefaultSounds.menuflip);
                                    }
                                }
                            }),
                        };

                        elements.Add(new MenuButton
                        {
                            id = "32848924",
                            name = "Prev Page",
                            text = "<align=center><b><",
                            parentLayout = "queue settings",
                            selectionPosition = new Vector2Int(5, 1),
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            func = () =>
                            {
                                if (Pages[(int)CurrentTab] != 0 && pageField.inputField)
                                {
                                    pageField.inputField.text = (Pages[(int)CurrentTab] - 1).ToString();
                                    SoundManager.inst.PlaySound(DefaultSounds.menuflip);
                                }
                                else
                                    SoundManager.inst.PlaySound(DefaultSounds.Block);
                            },
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            selectedColor = 6,
                            selectedOpacity = 1f,
                            selectedTextColor = 7,
                            length = 0.1f,
                            regenerate = false,
                            playBlipSound = false,
                        });

                        elements.Add(pageField);

                        elements.Add(new MenuButton
                        {
                            id = "32848924",
                            name = "Next Page",
                            text = "<align=center><b>>",
                            parentLayout = "queue settings",
                            selectionPosition = new Vector2Int(6, 1),
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            func = () =>
                            {
                                if (Pages[(int)CurrentTab] != QueuePageCount)
                                {
                                    pageField.inputField.text = (Pages[(int)CurrentTab] + 1).ToString();
                                    SoundManager.inst.PlaySound(DefaultSounds.menuflip);
                                }
                                else
                                    SoundManager.inst.PlaySound(DefaultSounds.Block);
                            },
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            selectedColor = 6,
                            selectedOpacity = 1f,
                            selectedTextColor = 7,
                            length = 0.1f,
                            regenerate = false,
                            playBlipSound = false,
                        });

                        layouts.Add("levels", new MenuVerticalLayout
                        {
                            name = "levels",
                            rect = RectValues.Default.AnchoredPosition(-100f, 100f).SizeDelta(800f, 400f),
                            spacing = 12f,
                            childControlHeight = false,
                            childControlWidth = false,
                            regenerate = false,
                        });
                        
                        layouts.Add("deletes", new MenuVerticalLayout
                        {
                            name = "deletes",
                            rect = RectValues.Default.AnchoredPosition(760f, 100f).SizeDelta(800f, 400f),
                            spacing = 12f,
                            childControlHeight = false,
                            childControlWidth = false,
                            regenerate = false,
                        });

                        RefreshQueueLevels(false);

                        break;
                    }
                case Tab.Steam: {
                        layouts.Add("steam settings", new MenuHorizontalLayout
                        {
                            name = "steam settings",
                            rect = RectValues.HorizontalAnchored.AnchoredPosition(0f, 350f).SizeDelta(-126f, 64f),
                            childForceExpandWidth = true,
                            regenerate = false,
                        });

                        elements.Add(new MenuInputField
                        {
                            id = "842848",
                            name = "Search Bar",
                            parentLayout = "steam settings",
                            rect = RectValues.Default.SizeDelta(!ViewOnline ? 404f : 468f, 64f),
                            text = currentSearch,
                            valueChangedFunc = ViewOnline ? SearchOnlineSteamLevels : SearchSubscribedSteamLevels,
                            placeholder = "Search levels...",
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            placeholderColor = 6,
                            length = 0.1f,
                            wait = false,
                            regenerate = false,
                        });

                        if (ViewOnline)
                        {
                            elements.Add(new MenuButton
                            {
                                id = "25428852",
                                name = "Search Button",
                                text = "<align=center><b>[ SEARCH ]",
                                parentLayout = "steam settings",
                                selectionPosition = new Vector2Int(0, 1),
                                rect = RectValues.Default.SizeDelta(200f, 64f),
                                func = RefreshOnlineSteamLevels().Start,
                                color = 6,
                                opacity = 0.1f,
                                textColor = 6,
                                selectedColor = 6,
                                selectedOpacity = 1f,
                                selectedTextColor = 7,
                                length = 0.1f,
                                regenerate = false,
                            });

                            var sortButton = new MenuButton
                            {
                                id = "25428852",
                                name = "Sort Button",
                                text = $"<align=center><b>[ SORT: {ArcadeConfig.Instance.SteamWorkshopOrderby.Value} ]",
                                parentLayout = "steam settings",
                                selectionPosition = new Vector2Int(1, 1),
                                rect = RectValues.Default.SizeDelta(400f, 64f),
                                color = 6,
                                opacity = 0.1f,
                                textColor = 6,
                                selectedColor = 6,
                                selectedOpacity = 1f,
                                selectedTextColor = 7,
                                length = 0.1f,
                                regenerate = false,
                            };
                            sortButton.func = () =>
                            {
                                var num = (int)ArcadeConfig.Instance.SteamWorkshopOrderby.Value;
                                num++;
                                if (num >= Enum.GetNames(typeof(QuerySort)).Length)
                                    num = 0;
                                ArcadeConfig.Instance.SteamWorkshopOrderby.Value = (QuerySort)num;
                                sortButton.text = $"<align=center><b>[ SORT: {ArcadeConfig.Instance.SteamWorkshopOrderby.Value} ]";
                                if (sortButton.textUI)
                                {
                                    sortButton.textUI.maxVisibleCharacters = 9999;
                                    sortButton.textUI.text = sortButton.text;
                                }
                            };
                            elements.Add(sortButton);
                        }
                        else
                        {
                            elements.Add(new MenuButton
                            {
                                id = "25428852",
                                name = "Reload Button",
                                text = "<align=center><b>[ RELOAD ]",
                                parentLayout = "steam settings",
                                selectionPosition = new Vector2Int(0, 1),
                                rect = RectValues.Default.SizeDelta(200f, 64f),
                                func = LoadLevelsMenu.Init,
                                color = 6,
                                opacity = 0.1f,
                                textColor = 6,
                                selectedColor = 6,
                                selectedOpacity = 1f,
                                selectedTextColor = 7,
                                length = 0.1f,
                                regenerate = false,
                            });

                            var sortButton = new MenuButton
                            {
                                id = "25428852",
                                name = "Sort Button",
                                text = $"<align=center><b>[ SORT: {ArcadeConfig.Instance.SteamLevelOrderby.Value} ]",
                                parentLayout = "steam settings",
                                selectionPosition = new Vector2Int(1, 1),
                                rect = RectValues.Default.SizeDelta(400f, 64f),
                                color = 6,
                                opacity = 0.1f,
                                textColor = 6,
                                selectedColor = 6,
                                selectedOpacity = 1f,
                                selectedTextColor = 7,
                                length = 0.1f,
                                regenerate = false,
                            };
                            sortButton.func = () =>
                            {
                                var num = (int)ArcadeConfig.Instance.SteamLevelOrderby.Value;
                                num++;
                                if (num >= Enum.GetNames(typeof(LevelSort)).Length)
                                    num = 0;
                                ArcadeConfig.Instance.SteamLevelOrderby.Value = (LevelSort)num;
                                sortButton.text = $"<align=center><b>[ SORT: {ArcadeConfig.Instance.SteamLevelOrderby.Value} ]";
                                if (sortButton.textUI)
                                {
                                    sortButton.textUI.maxVisibleCharacters = 9999;
                                    sortButton.textUI.text = sortButton.text;
                                }
                            };
                            elements.Add(sortButton);

                            var ascendButton = new MenuButton
                            {
                                id = "25428852",
                                name = "Sort Button",
                                text = $"<align=center><b><rotate={(ArcadeConfig.Instance.SteamLevelAscend.Value ? "90" : "-90")}>>",
                                parentLayout = "steam settings",
                                selectionPosition = new Vector2Int(2, 1),
                                rect = RectValues.Default.SizeDelta(64f, 64f),
                                color = 6,
                                opacity = 0.1f,
                                textColor = 6,
                                selectedColor = 6,
                                selectedOpacity = 1f,
                                selectedTextColor = 7,
                                length = 0.1f,
                                regenerate = false,
                            };
                            ascendButton.func = () =>
                            {
                                ArcadeConfig.Instance.SteamLevelAscend.Value = !ArcadeConfig.Instance.SteamLevelAscend.Value;
                                ascendButton.text = $"<align=center><b><rotate={(ArcadeConfig.Instance.SteamLevelAscend.Value ? "90" : "-90")}>>";
                                if (ascendButton.textUI)
                                {
                                    ascendButton.textUI.maxVisibleCharacters = 9999;
                                    ascendButton.textUI.text = ascendButton.text;
                                }
                            };
                            elements.Add(ascendButton);
                        }

                        var pageField = new MenuInputField
                        {
                            id = "842848",
                            name = "Page Bar",
                            parentLayout = "steam settings",
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            text = currentPage.ToString(),
                            textAnchor = TextAnchor.MiddleCenter,
                            valueChangedFunc = _val =>
                            {
                                if (ViewOnline)
                                    SetOnlineSteamLevelsPage(Parser.TryParse(_val, Pages[(int)CurrentTab]));
                                else
                                    SetSubscribedSteamLevelsPage(Parser.TryParse(_val, Pages[(int)CurrentTab]));
                            },
                            placeholder = "Set page...",
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            placeholderColor = 6,
                            length = 0.1f,
                            wait = false,
                            regenerate = false,
                        };
                        pageField.triggers = new EventTrigger.Entry[]
                        {
                            TriggerHelper.CreateEntry(EventTriggerType.Scroll, eventData =>
                            {
                                var pointerEventData = (PointerEventData)eventData;

                                var inputField = pageField.inputField;
                                if (!inputField)
                                    return;

                                if (int.TryParse(inputField.text, out int result))
                                {
                                    bool large = Input.GetKey(KeyCode.LeftControl);

                                    if (pointerEventData.scrollDelta.y < 0f)
                                        result -= 1 * (large ? 10 : 1);
                                    if (pointerEventData.scrollDelta.y > 0f)
                                        result += 1 * (large ? 10 : 1);

                                    if (!ViewOnline && SubscribedSteamLevelPageCount != 0)
                                        result = Mathf.Clamp(result, 0, SubscribedSteamLevelPageCount);

                                    if (inputField.text != result.ToString())
                                    {
                                        inputField.text = result.ToString();
                                        SoundManager.inst.PlaySound(DefaultSounds.menuflip);
                                    }
                                }
                            }),
                        };

                        elements.Add(new MenuButton
                        {
                            id = "32848924",
                            name = "Prev Page",
                            text = "<align=center><b><",
                            parentLayout = "steam settings",
                            selectionPosition = new Vector2Int(!ViewOnline ? 3 : 2, 1),
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            func = () =>
                            {
                                if (Pages[(int)CurrentTab] != 0 && pageField.inputField)
                                {
                                    pageField.inputField.text = (Pages[(int)CurrentTab] - 1).ToString();
                                    SoundManager.inst.PlaySound(DefaultSounds.menuflip);
                                }
                                else
                                    SoundManager.inst.PlaySound(DefaultSounds.Block);
                            },
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            selectedColor = 6,
                            selectedOpacity = 1f,
                            selectedTextColor = 7,
                            length = 0.1f,
                            regenerate = false,
                            playBlipSound = false,
                        });

                        elements.Add(pageField);

                        elements.Add(new MenuButton
                        {
                            id = "32848924",
                            name = "Next Page",
                            text = "<align=center><b>>",
                            parentLayout = "steam settings",
                            selectionPosition = new Vector2Int(!ViewOnline ? 4 : 3, 1),
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            func = () =>
                            {
                                if (ViewOnline || Pages[(int)CurrentTab] != SubscribedSteamLevelPageCount)
                                {
                                    pageField.inputField.text = (Pages[(int)CurrentTab] + 1).ToString();
                                    SoundManager.inst.PlaySound(DefaultSounds.menuflip);
                                }
                                else
                                    SoundManager.inst.PlaySound(DefaultSounds.Block);
                            },
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            selectedColor = 6,
                            selectedOpacity = 1f,
                            selectedTextColor = 7,
                            length = 0.1f,
                            regenerate = false,
                            playBlipSound = false,
                        });
                        
                        elements.Add(new MenuButton
                        {
                            id = "32848924",
                            name = "Switch Steam View",
                            text = $"<align=center><b>[ {(ViewOnline ? "VIEW SUBSCRIBED" : "VIEW ONLINE")} ]",
                            parentLayout = "steam settings",
                            selectionPosition = new Vector2Int(!ViewOnline ? 5 : 4, 1),
                            rect = RectValues.Default.SizeDelta(300f, 64f),
                            func = () =>
                            {
                                ViewOnline = !ViewOnline;
                                Pages[(int)CurrentTab] = 0;
                                Init();
                            },
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            selectedColor = 6,
                            selectedOpacity = 1f,
                            selectedTextColor = 7,
                            length = 0.1f,
                            regenerate = false,
                        });

                        layouts.Add("levels", new MenuGridLayout
                        {
                            name = "levels",
                            rect = RectValues.Default.AnchoredPosition(-500f, 100f).SizeDelta(800f, 400f),
                            cellSize = new Vector2(350f, ViewOnline ? 70f : 180f),
                            spacing = new Vector2(12f, 12f),
                            constraint = GridLayoutGroup.Constraint.FixedColumnCount,
                            constraintCount = 5,
                            regenerate = false,
                        });

                        if (ViewOnline)
                            CoroutineHelper.StartCoroutine(RefreshOnlineSteamLevels());
                        else
                            RefreshSubscribedSteamLevels(false);

                        break;
                    }
            }

            defaultSelection = new Vector2Int(1, 0);
            exitFunc = Exit;
            if (CurrentTab != Tab.Steam || !ViewOnline)
                StartGeneration();
            onGenerateUIFinish = () =>
            {
                var fileDragAndDrop = canvas.GameObject.AddComponent<FileDragAndDrop>();
                fileDragAndDrop.onFilesDropped = dropInfos =>
                {
                    for (int i = 0; i < dropInfos.Count; i++)
                    {
                        var dropInfo = dropInfos[i];

                        CoreHelper.Log($"Dropped file: {dropInfo}");

                        dropInfo.filePath = RTFile.ReplaceSlash(dropInfo.filePath);

                        var attributes = File.GetAttributes(dropInfo.filePath);
                        if (attributes.HasFlag(FileAttributes.Directory))
                        {
                            if (Level.TryVerify(dropInfo.filePath, true, out Level level))
                            {
                                level.metadata.VerifyID(level.path);
                                level.UpdateDefaults();
                                SoundManager.inst.PlaySound(DefaultSounds.blip);
                                PlayLevelMenu.Init(level);
                            }
                            break;
                        }

                        if (dropInfo.filePath.EndsWith(Level.LEVEL_LSB) || dropInfo.filePath.EndsWith(Level.LEVEL_VGD))
                        {
                            if (Level.TryVerify(dropInfo.filePath.Remove("/" + Level.LEVEL_LSB).Remove("/" + Level.LEVEL_VGD), true, out Level level))
                            {
                                level.metadata.VerifyID(level.path);
                                level.UpdateDefaults();
                                SoundManager.inst.PlaySound(DefaultSounds.blip);
                                PlayLevelMenu.Init(level);
                            }
                            break;
                        }
                    }
                };
            };
            InterfaceManager.inst.PlayMusic();
        }

        #region Local

        public static int LocalLevelPageCount => (LocalLevelCollections.Count + LocalLevels.Count) / MAX_LEVELS_PER_PAGE;
        public static string LocalSearch => Searches[0];
        public static List<Level> LocalLevels => LevelManager.Levels.FindAll(level => !level.fromCollection && (string.IsNullOrEmpty(LocalSearch)
                        || level.id == LocalSearch
                        || level.metadata.song.tags.Contains(LocalSearch.ToLower())
                        || level.metadata.artist.Name.ToLower().Contains(LocalSearch.ToLower())
                        || level.metadata.creator.steam_name.ToLower().Contains(LocalSearch.ToLower())
                        || level.metadata.song.title.ToLower().Contains(LocalSearch.ToLower())
                        || level.metadata.song.getDifficulty().ToLower().Contains(LocalSearch.ToLower())));

        public static List<LevelCollection> LocalLevelCollections => LevelManager.LevelCollections.FindAll(collection => string.IsNullOrEmpty(LocalSearch)
                        || collection.id == LocalSearch
                        || collection.name.ToLower().Contains(LocalSearch.ToLower()));

        public void SearchLocalLevels(string search)
        {
            Searches[0] = search;
            Pages[0] = 0;

            RefreshLocalLevels(true);
        }

        public void SetLocalLevelsPage(int page)
        {
            Pages[0] = Mathf.Clamp(page, 0, LocalLevelPageCount);

            RefreshLocalLevels(true);
        }

        void ClearLocalLevelButtons()
        {
            var levelButtons = elements.FindAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name == "Rank" || x.name.Contains("Shine") || x.name.Contains("Lock"));

            for (int i = 0; i < levelButtons.Count; i++)
            {
                var levelButton = levelButtons[i];
                levelButton.Clear();
                CoreHelper.Destroy(levelButton.gameObject);
            }
            elements.RemoveAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name == "Rank" || x.name.Contains("Shine") || x.name.Contains("Lock"));
        }

        public void RefreshLocalLevels(bool regenerateUI, bool clear = true)
        {
            if (clear)
                ClearLocalLevelButtons();

            var currentPage = Pages[(int)CurrentTab] + 1;
            int max = currentPage * MAX_LEVELS_PER_PAGE;
            var currentSearch = Searches[(int)CurrentTab];

            var levels = LocalLevels;
            var collections = LocalLevelCollections;
            int num = 0;
            for (int i = 0; i < collections.Count; i++)
            {
                int index = num;
                if (index < max - MAX_LEVELS_PER_PAGE || index >= max)
                {
                    num++;
                    continue;
                }

                int column = (index % MAX_LEVELS_PER_PAGE) % 5;
                int row = (int)((index % MAX_LEVELS_PER_PAGE) / 5) + 2;

                var collection = collections[i];
                
                elements.Add(new MenuButton
                {
                    id = collection.id,
                    name = "Level Button",
                    parentLayout = "levels",
                    selectionPosition = new Vector2Int(column, row),
                    func = () =>
                    {
                        LevelManager.currentQueueIndex = 0;
                        LevelManager.CurrentLevelCollection = collection;
                        LevelCollectionMenu.Init(collection);
                    },
                    icon = collection.icon ?? LegacyPlugin.AtanPlaceholder,
                    iconRect = RectValues.Default.AnchoredPosition(-90, 30f),
                    text = "<size=24>" + collection.name,
                    textRect = RectValues.FullAnchored.AnchoredPosition(20f, -50f),
                    enableWordWrapping = true,
                    color = 6,
                    opacity = 0.1f,
                    textColor = 6,
                    selectedColor = 6,
                    selectedOpacity = 1f,
                    selectedTextColor = 7,
                    length = regenerateUI ? 0f : 0.01f,
                    wait = !regenerateUI,
                    playSound = !regenerateUI,
                    mask = true,
                });

                num++;
            }

            for (int i = 0; i < levels.Count; i++)
            {
                int index = num;
                if (index < max - MAX_LEVELS_PER_PAGE || index >= max)
                {
                    num++;
                    continue;
                }

                int column = (index % MAX_LEVELS_PER_PAGE) % 5;
                int row = (int)((index % MAX_LEVELS_PER_PAGE) / 5) + 2;

                var level = levels[i];
                var levelRank = LevelManager.GetLevelRank(level);

                var isSSRank = levelRank.name == "SS";

                MenuImage shine = null;

                var button = new MenuButton
                {
                    id = level.id,
                    name = "Level Button",
                    parentLayout = "levels",
                    selectionPosition = new Vector2Int(column, row),
                    icon = level.icon,
                    iconRect = RectValues.Default.AnchoredPosition(-90, 30f),
                    text = "<size=24>" + level.metadata?.beatmap?.name,
                    textRect = RectValues.FullAnchored.AnchoredPosition(20f, -50f),
                    enableWordWrapping = true,
                    color = 6,
                    opacity = 0.1f,
                    textColor = 6,
                    selectedColor = 6,
                    selectedOpacity = 1f,
                    selectedTextColor = 7,
                    length = regenerateUI ? 0f : 0.01f,
                    wait = !regenerateUI,
                    playSound = !regenerateUI,
                    mask = true,
                    playBlipSound = false,

                    allowOriginalHoverMethods = true,
                    enterFunc = () =>
                    {
                        if (!isSSRank)
                            return;

                        var animation = new RTAnimation($"{level.id} Level Shine")
                        {
                            animationHandlers = new List<AnimationHandlerBase>
                            {
                                new AnimationHandler<float>(new List<IKeyframe<float>>
                                {
                                    new FloatKeyframe(0f, -240f, Ease.Linear),
                                    new FloatKeyframe(1f, 240f, Ease.CircInOut),
                                }, x => { if (shine != null && shine.gameObject) shine.gameObject.transform.AsRT().anchoredPosition = new Vector2(x, 0f); }),
                            },
                            loop = true,
                        };

                        AnimationManager.inst.Play(animation);
                    },
                    exitFunc = () =>
                    {
                        if (AnimationManager.inst.TryFindAnimations(x => x.name == $"{level.id} Level Shine", out List<RTAnimation> animations))
                            for (int i = 0; i < animations.Count; i++)
                                AnimationManager.inst.Remove(animations[i].id);

                        if (!isSSRank)
                            return;

                        if (shine != null && shine.gameObject)
                            shine.gameObject.transform.AsRT().anchoredPosition = new Vector2(-240f, 0f);
                    },
                };
                MenuImage locked = null;

                var levelIsLocked = level.Locked;
                if (levelIsLocked)
                {
                    locked = new MenuImage
                    {
                        id = "0",
                        name = "Lock",
                        parent = button.id,
                        icon = LegacyPlugin.LockSprite,
                        rect = RectValues.Default.AnchoredPosition(80f, 40f).Pivot(0.5f, 0.8f).SizeDelta(80f, 100f),
                        useOverrideColor = true,
                        overrideColor = Color.white,
                        opacity = 1f,
                        length = 0f,
                        wait = false,
                    };
                }

                button.func = () =>
                {
                    if (levelIsLocked)
                    {
                        SoundManager.inst.PlaySound(DefaultSounds.Block);

                        var animation = new RTAnimation($"Blocked Level in Arcade {level.id}")
                        {
                            animationHandlers = new List<AnimationHandlerBase>
                            {
                                    new AnimationHandler<float>(new List<IKeyframe<float>>
                                    {
                                        new FloatKeyframe(0f, 15f, Ease.Linear),
                                        new FloatKeyframe(1f, 0f, Ease.ElasticOut),
                                    }, x => { if (button.gameObject) button.gameObject.transform.SetLocalRotationEulerZ(x); }),
                                    new AnimationHandler<float>(new List<IKeyframe<float>>
                                    {
                                        new FloatKeyframe(0f, 120f, Ease.Linear),
                                        new FloatKeyframe(2f, 0f, Ease.ElasticOut),
                                    }, x => { if (locked.gameObject) locked.gameObject.transform.SetLocalRotationEulerZ(x); }),
                            },
                        };
                        animation.onComplete = () =>
                        {
                            AnimationManager.inst.Remove(animation.id);
                            if (button.gameObject)
                                button.gameObject.transform.SetLocalRotationEulerZ(0f);
                            if (locked.gameObject)
                                locked.gameObject.transform.SetLocalRotationEulerZ(0f);
                        };

                        AnimationManager.inst.FindAnimationsByName(animation.name).ForEach(x =>
                        {
                            x.Pause();
                            AnimationManager.inst.Remove(x.id);
                        });
                        AnimationManager.inst.Play(animation);

                        return;
                    }

                    SoundManager.inst.PlaySound(DefaultSounds.blip);
                    PlayLevelMenu.Init(level);
                };
                elements.Add(button);
                if (levelIsLocked)
                    elements.Add(locked);

                elements.Add(new MenuImage
                {
                    id = "0",
                    name = "Difficulty",
                    parent = level.id,
                    rect = new RectValues(Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(8f, 0f)),
                    overrideColor = CoreHelper.GetDifficulty(level.metadata.song.difficulty).color,
                    useOverrideColor = true,
                    opacity = 1f,
                    roundedSide = SpriteHelper.RoundedSide.Left,
                    length = 0f,
                    wait = false,
                });

                if (levelRank.name != "-")
                    elements.Add(new MenuText
                    {
                        id = "0",
                        name = "Rank",
                        parent = level.id,
                        text = $"<size=70><b><align=center>{levelRank.name}",
                        rect = RectValues.Default.AnchoredPosition(65f, 25f).SizeDelta(64f, 64f),
                        overrideTextColor = levelRank.color,
                        useOverrideTextColor = true,
                        hideBG = true,
                        length = 0f,
                        wait = false,
                    });

                if (isSSRank)
                {
                    shine = new MenuImage
                    {
                        id = LSText.randomNumString(16),
                        name = "Shine Base",
                        parent = level.id,
                        rect = RectValues.Default.AnchoredPosition(-240f, 0f).Rotation(15f),
                        opacity = 0f,
                        length = 0f,
                        wait = false,
                    };

                    var shine1 = new MenuImage
                    {
                        id = "0",
                        name = "Shine 1",
                        parent = shine.id,
                        rect = RectValues.Default.AnchoredPosition(-12f, 0f).SizeDelta(8f, 400f),
                        overrideColor = ArcadeConfig.Instance.ShineColor.Value,
                        useOverrideColor = true,
                        opacity = 1f,
                        length = 0f,
                        wait = false,
                    };

                    var shine2 = new MenuImage
                    {
                        id = "0",
                        name = "Shine 2",
                        parent = shine.id,
                        rect = RectValues.Default.AnchoredPosition(12f, 0f).SizeDelta(20f, 400f),
                        overrideColor = ArcadeConfig.Instance.ShineColor.Value,
                        useOverrideColor = true,
                        opacity = 1f,
                        length = 0f,
                        wait = false,
                    };

                    elements.Add(shine);
                    elements.Add(shine1);
                    elements.Add(shine2);
                }

                num++;
            }

            if (regenerateUI)
                StartGeneration();
        }

        #endregion

        #region Online

        public static string SearchURL => $"{AlephNetwork.ARCADE_SERVER_URL}api/level/search";
        public static string CoverURL => $"{AlephNetwork.ARCADE_SERVER_URL}api/level/cover/";
        public static string DownloadURL => $"{AlephNetwork.ARCADE_SERVER_URL}api/level/zip/";

        public static string OnlineSearch => Searches[1];

        public static int OnlineLevelCount { get; set; }

        public static Dictionary<string, Sprite> OnlineLevelIcons { get; set; } = new Dictionary<string, Sprite>();

        public void SetOnlineLevelsPage(int page)
        {
            Pages[1] = page;
            CoroutineHelper.StartCoroutine(RefreshOnlineLevels());
        }

        public void SearchOnlineLevels(string search)
        {
            Searches[1] = search;
            Pages[1] = 0;
        }

        public IEnumerator RefreshOnlineLevels()
        {
            if (loadingOnlineLevels)
                yield break;

            var levelButtons = elements.FindAll(x => x.name == "Level Button" || x.name == "Difficulty");

            for (int i = 0; i < levelButtons.Count; i++)
            {
                var levelButton = levelButtons[i];
                levelButton.Clear();
                CoreHelper.Destroy(levelButton.gameObject);
            }
            elements.RemoveAll(x => x.name == "Level Button" || x.name == "Difficulty");

            var page = Pages[1];
            int currentPage = page + 1;

            var search = OnlineSearch;

            string query =
                string.IsNullOrEmpty(search) && page == 0 ? SearchURL :
                    string.IsNullOrEmpty(search) && page != 0 ? $"{SearchURL}?page={page}" :
                        !string.IsNullOrEmpty(search) && page == 0 ? $"{SearchURL}?query={AlephNetwork.ReplaceSpace(search)}" :
                            !string.IsNullOrEmpty(search) ? $"{SearchURL}?query={AlephNetwork.ReplaceSpace(search)}&page={page}" : "";

            CoreHelper.Log($"Search query: {query}");

            if (string.IsNullOrEmpty(query))
                yield break;

            loadingOnlineLevels = true;
            var headers = new Dictionary<string, string>();
            if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null)
                headers["Authorization"] = $"Bearer {LegacyPlugin.authData["access_token"].Value}";

            yield return CoroutineHelper.StartCoroutine(AlephNetwork.DownloadJSONFile(query, json =>
            {
                try
                {
                    var jn = JSON.Parse(json);

                    if (jn["items"] != null)
                    {
                        int num = 0;
                        for (int i = 0; i < jn["items"].Count; i++)
                        {
                            var item = jn["items"][i];

                            string id = item["id"];

                            string artist = item["artist"];
                            string title = item["title"];
                            string name = item["name"];
                            string creator = item["creator"];
                            string description = item["description"];
                            var difficulty = item["difficulty"].AsInt;

                            if (id == null || id == "0")
                                continue;

                            int index = i;
                            int column = (num % MAX_LEVELS_PER_PAGE) % 5;
                            int row = (int)((num % MAX_LEVELS_PER_PAGE) / 5) + 2;

                            var button = new MenuButton
                            {
                                id = id,
                                name = "Level Button",
                                parentLayout = "levels",
                                selectionPosition = new Vector2Int(column, row),
                                func = () => SelectOnlineLevel(item.AsObject),
                                iconRect = RectValues.Default.AnchoredPosition(-90, 30f),
                                text = "<size=24>" + name,
                                textRect = RectValues.FullAnchored.AnchoredPosition(20f, -50f),
                                enableWordWrapping = true,
                                icon = SteamWorkshop.inst.defaultSteamImageSprite,
                                color = 6,
                                opacity = 0.1f,
                                textColor = 6,
                                selectedColor = 6,
                                selectedOpacity = 1f,
                                selectedTextColor = 7,
                                length = 0.01f,
                            };
                            elements.Add(button);

                            elements.Add(new MenuImage
                            {
                                id = "0",
                                name = "Difficulty",
                                parent = id,
                                rect = new RectValues(Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(8f, 0f)),
                                overrideColor = CoreHelper.GetDifficulty(difficulty).color,
                                useOverrideColor = true,
                                opacity = 1f,
                                roundedSide = SpriteHelper.RoundedSide.Left,
                                length = 0f,
                                wait = false,
                            });

                            if (OnlineLevelIcons.TryGetValue(id, out Sprite sprite))
                                button.icon = sprite;
                            else
                            {
                                CoroutineHelper.StartCoroutine(AlephNetwork.DownloadBytes($"{CoverURL}{id}{FileFormat.JPG.Dot()}", bytes =>
                                {
                                    var sprite = SpriteHelper.LoadSprite(bytes);
                                    OnlineLevelIcons[id] = sprite;
                                    button.icon = sprite;
                                    if (button.iconUI)
                                        button.iconUI.sprite = sprite;
                                }, onError =>
                                {
                                    var sprite = SteamWorkshop.inst.defaultSteamImageSprite;
                                    OnlineLevelIcons[id] = sprite;
                                    button.icon = sprite;
                                    if (button.iconUI)
                                        button.iconUI.sprite = sprite;
                                }));
                            }

                            num++;
                        }
                    }

                    OnlineLevelCount = jn["count"].AsInt;
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
            }, headers));

            loadingOnlineLevels = false;
            StartGeneration();
            while (generating)
                yield return null;
        }

        public bool loadingOnlineLevels;

        public void SelectOnlineLevel(JSONObject onlineLevel) => DownloadLevelMenu.Init(onlineLevel);

        #endregion

        #region Browser

        public static int BrowserPageCount => BrowserFolders.Length / MAX_LEVELS_PER_PAGE;
        public static string BrowserSearch => Searches[2];
        public static string BrowserCurrentDirectory { get; set; } = RTFile.ApplicationDirectory;
        public static string[] BrowserFolders =>
            Directory.GetDirectories(BrowserCurrentDirectory)
                    .Where(x => string.IsNullOrEmpty(BrowserSearch) || Path.GetFileName(x).ToLower().Contains(BrowserSearch.ToLower()) || Level.TryVerify(x + "/", false, out Level level) &&
                        (level.id == BrowserSearch
                        || level.metadata.song.tags.Contains(BrowserSearch.ToLower())
                        || level.metadata.artist.Name.ToLower().Contains(BrowserSearch.ToLower())
                        || level.metadata.creator.steam_name.ToLower().Contains(BrowserSearch.ToLower())
                        || level.metadata.song.title.ToLower().Contains(BrowserSearch.ToLower())
                        || level.metadata.song.getDifficulty().ToLower().Contains(BrowserSearch.ToLower()))).ToArray();

        public void SearchBrowser(string search)
        {
            Searches[2] = search;
            Pages[2] = 0;

            var levelButtons = elements.FindAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name.Contains("Shine"));

            for (int i = 0; i < levelButtons.Count; i++)
            {
                var levelButton = levelButtons[i];
                levelButton.Clear();
                CoreHelper.Destroy(levelButton.gameObject);
            }
            elements.RemoveAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name.Contains("Shine"));
            RefreshBrowserLevels(true);
        }

        public void SetBrowserPage(int page)
        {
            Pages[2] = Mathf.Clamp(page, 0, BrowserPageCount);

            var levelButtons = elements.FindAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name.Contains("Shine"));

            for (int i = 0; i < levelButtons.Count; i++)
            {
                var levelButton = levelButtons[i];
                levelButton.Clear();
                CoreHelper.Destroy(levelButton.gameObject);
            }
            elements.RemoveAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name.Contains("Shine"));
            RefreshBrowserLevels(true);
        }

        public void RefreshBrowserLevels(bool regenerateUI)
        {
            var currentPage = Pages[(int)CurrentTab] + 1;
            int max = currentPage * (MAX_LEVELS_PER_PAGE - 1);
            var currentSearch = Searches[(int)CurrentTab];

            var directoryInfo = new DirectoryInfo(BrowserCurrentDirectory);

            // Return
            if (directoryInfo.Parent != null)
            {
                elements.Add(new MenuButton
                {
                    id = "525",
                    name = "Level Button",
                    parentLayout = "levels",
                    selectionPosition = new Vector2Int(0, 2),
                    func = () =>
                    {
                        BrowserCurrentDirectory = directoryInfo.Parent.FullName;
                        SetBrowserPage(0);
                    },
                    text = "<size=24> < UP A FOLDER",
                    textRect = RectValues.FullAnchored.AnchoredPosition(20f, -50f),
                    enableWordWrapping = true,
                    color = 6,
                    opacity = 0.1f,
                    textColor = 6,
                    selectedColor = 6,
                    selectedOpacity = 1f,
                    selectedTextColor = 7,
                    length = regenerateUI ? 0f : 0.01f,
                    wait = !regenerateUI,
                    playSound = !regenerateUI,
                    mask = true,
                });
            }

            var folders = BrowserFolders;
            int num = 1;
            for (int i = 0; i < folders.Length; i++)
            {
                int index = i;
                if (index < max - (MAX_LEVELS_PER_PAGE - 1) || index >= max)
                    continue;

                int column = (num % (MAX_LEVELS_PER_PAGE)) % 5;
                int row = (int)((num % (MAX_LEVELS_PER_PAGE)) / 5) + 2;

                var folder = RTFile.ReplaceSlash(folders[index]);

                if (Level.TryVerify(folder, true, out Level level))
                {
                    var isSSRank = LevelManager.GetLevelRank(level).name == "SS";

                    MenuImage shine = null;

                    var button = new MenuButton
                    {
                        id = level.id,
                        name = "Level Button",
                        parentLayout = "levels",
                        selectionPosition = new Vector2Int(column, row),
                        func = () => PlayLevelMenu.Init(level),
                        icon = level.icon,
                        iconRect = RectValues.Default.AnchoredPosition(-90, 30f),
                        text = "<size=24>" + level.metadata?.beatmap?.name,
                        textRect = RectValues.FullAnchored.AnchoredPosition(20f, -50f),
                        enableWordWrapping = true,
                        color = 6,
                        opacity = 0.1f,
                        textColor = 6,
                        selectedColor = 6,
                        selectedOpacity = 1f,
                        selectedTextColor = 7,
                        length = regenerateUI ? 0f : 0.01f,
                        wait = !regenerateUI,
                        playSound = !regenerateUI,
                        mask = true,

                        allowOriginalHoverMethods = true,
                        enterFunc = () =>
                        {
                            if (!isSSRank)
                                return;

                            var animation = new RTAnimation("Level Shine")
                            {
                                animationHandlers = new List<AnimationHandlerBase>
                            {
                                new AnimationHandler<float>(new List<IKeyframe<float>>
                                {
                                    new FloatKeyframe(0f, -240f, Ease.Linear),
                                    new FloatKeyframe(1f, 240f, Ease.CircInOut),
                                }, x => { if (shine != null && shine.gameObject) shine.gameObject.transform.AsRT().anchoredPosition = new Vector2(x, 0f); }),
                            },
                                loop = true,
                            };

                            AnimationManager.inst.Play(animation);
                        },
                        exitFunc = () =>
                        {
                            if (!isSSRank)
                                return;

                            if (AnimationManager.inst.TryFindAnimation(x => x.name.Contains("Level Shine"), out RTAnimation animation))
                                AnimationManager.inst.Remove(animation.id);

                            if (shine != null && shine.gameObject)
                                shine.gameObject.transform.AsRT().anchoredPosition = new Vector2(-240f, 0f);
                        },
                    };
                    elements.Add(button);

                    elements.Add(new MenuImage
                    {
                        id = "0",
                        name = "Difficulty",
                        parent = level.id,
                        rect = new RectValues(Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(8f, 0f)),
                        overrideColor = CoreHelper.GetDifficulty(level.metadata.song.difficulty).color,
                        useOverrideColor = true,
                        opacity = 1f,
                        roundedSide = SpriteHelper.RoundedSide.Left,
                        length = 0f,
                        wait = false,
                    });

                    if (isSSRank)
                    {
                        shine = new MenuImage
                        {
                            id = LSText.randomNumString(16),
                            name = "Shine Base",
                            parent = level.id,
                            rect = RectValues.Default.AnchoredPosition(-240f, 0f).Rotation(15f),
                            opacity = 0f,
                            length = 0f,
                            wait = false,
                        };

                        var shine1 = new MenuImage
                        {
                            id = "0",
                            name = "Shine 1",
                            parent = shine.id,
                            rect = RectValues.Default.AnchoredPosition(-12f, 0f).SizeDelta(8f, 400f),
                            overrideColor = ArcadeConfig.Instance.ShineColor.Value,
                            useOverrideColor = true,
                            opacity = 1f,
                            length = 0f,
                            wait = false,
                        };

                        var shine2 = new MenuImage
                        {
                            id = "0",
                            name = "Shine 2",
                            parent = shine.id,
                            rect = RectValues.Default.AnchoredPosition(12f, 0f).SizeDelta(20f, 400f),
                            overrideColor = ArcadeConfig.Instance.ShineColor.Value,
                            useOverrideColor = true,
                            opacity = 1f,
                            length = 0f,
                            wait = false,
                        };

                        elements.Add(shine);
                        elements.Add(shine1);
                        elements.Add(shine2);
                    }
                }
                else
                {
                    elements.Add(new MenuButton
                    {
                        id = "525",
                        name = "Level Button",
                        parentLayout = "levels",
                        selectionPosition = new Vector2Int(column, row),
                        func = () =>
                        {
                            BrowserCurrentDirectory = folder;
                            SetBrowserPage(0);
                        },
                        text = "<size=24>" + Path.GetFileName(folder),
                        textRect = RectValues.FullAnchored.AnchoredPosition(20f, -50f),
                        enableWordWrapping = true,
                        color = 6,
                        opacity = 0.1f,
                        textColor = 6,
                        selectedColor = 6,
                        selectedOpacity = 1f,
                        selectedTextColor = 7,
                        length = regenerateUI ? 0f : 0.01f,
                        wait = !regenerateUI,
                        playSound = !regenerateUI,
                        mask = true,
                    });
                }

                num++;
            }

            if (regenerateUI)
                StartGeneration();
        }

        #endregion

        #region Queue

        public static int QueuePageCount => QueueLevels.Count / MAX_QUEUED_PER_PAGE;
        public static string QueueSearch => Searches[3];
        public static List<Level> QueueLevels => LevelManager.ArcadeQueue.FindAll(level => !level.fromCollection && (string.IsNullOrEmpty(QueueSearch)
                        || level.id == QueueSearch
                        || level.metadata.song.tags.Contains(QueueSearch.ToLower())
                        || level.metadata.artist.Name.ToLower().Contains(QueueSearch.ToLower())
                        || level.metadata.creator.steam_name.ToLower().Contains(QueueSearch.ToLower())
                        || level.metadata.song.title.ToLower().Contains(QueueSearch.ToLower())
                        || level.metadata.song.getDifficulty().ToLower().Contains(QueueSearch.ToLower())));

        public void SearchQueuedLevels(string search)
        {
            Searches[3] = search;
            Pages[3] = 0;
            RefreshQueueLevels(true);
        }

        public void SetQueuedLevelsPage(int page)
        {
            Pages[3] = Mathf.Clamp(page, 0, QueuePageCount);
            RefreshQueueLevels(true);
        }

        public void RefreshQueueLevels(bool regenerateUI)
        {
            // x = 800f
            // y = 180f

            var levelButtons = elements.FindAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name == "Delete Queue Button" || x.name.Contains("Shine"));

            for (int i = 0; i < levelButtons.Count; i++)
            {
                var levelButton = levelButtons[i];
                levelButton.Clear();
                CoreHelper.Destroy(levelButton.gameObject);
            }
            elements.RemoveAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name == "Delete Queue Button" || x.name.Contains("Shine"));

            var currentPage = Pages[(int)CurrentTab] + 1;
            int max = currentPage * MAX_QUEUED_PER_PAGE;
            var currentSearch = Searches[(int)CurrentTab];

            var levels = QueueLevels;
            for (int i = 0; i < levels.Count; i++)
            {
                int index = i;
                if (index < max - MAX_QUEUED_PER_PAGE || index >= max)
                    continue;

                var level = levels[index];

                var isSSRank = LevelManager.GetLevelRank(level).name == "SS";

                MenuImage shine = null;

                var button = new MenuButton
                {
                    id = level.id,
                    name = "Level Button",
                    parentLayout = "levels",
                    rect = RectValues.Default.SizeDelta(800f, 120f),
                    selectionPosition = new Vector2Int(0, index + 2),
                    func = () => PlayLevelMenu.Init(level),
                    icon = level.icon,
                    iconRect = RectValues.Default.AnchoredPosition(-320f, 0f).SizeDelta(100f, 100f),
                    text = "<size=32>" + i.ToString() + " - " + level.metadata?.beatmap?.name,
                    textRect = RectValues.FullAnchored.AnchorMin(0.23f, 0f),
                    enableWordWrapping = true,
                    color = 6,
                    opacity = 0.1f,
                    textColor = 6,
                    selectedColor = 6,
                    selectedOpacity = 1f,
                    selectedTextColor = 7,
                    length = regenerateUI ? 0f : 0.01f,
                    wait = !regenerateUI,
                    playSound = !regenerateUI,
                    mask = true,

                    allowOriginalHoverMethods = true,
                    enterFunc = () =>
                    {
                        if (AnimationManager.inst.TryFindAnimations(x => x.name.Contains("Level Shine"), out List<RTAnimation> animations))
                            for (int i = 0; i < animations.Count; i++)
                                AnimationManager.inst.Remove(animations[i].id);

                        if (!isSSRank)
                            return;

                        var animation = new RTAnimation("Level Shine")
                        {
                            animationHandlers = new List<AnimationHandlerBase>
                            {
                                new AnimationHandler<float>(new List<IKeyframe<float>>
                                {
                                    new FloatKeyframe(0f, -440f, Ease.Linear),
                                    new FloatKeyframe(1f, 440f, Ease.CircInOut),
                                }, x => { if (shine != null && shine.gameObject) shine.gameObject.transform.AsRT().anchoredPosition = new Vector2(x, 0f); }),
                            },
                            loop = true,
                        };

                        AnimationManager.inst.Play(animation);
                    },
                    exitFunc = () =>
                    {
                        if (!isSSRank)
                            return;

                        if (AnimationManager.inst.TryFindAnimations(x => x.name.Contains("Level Shine"), out List<RTAnimation> animations))
                            for (int i = 0; i < animations.Count; i++)
                                AnimationManager.inst.Remove(animations[i].id);

                        if (shine != null && shine.gameObject)
                            shine.gameObject.transform.AsRT().anchoredPosition = new Vector2(-440f, 0f);
                    },
                };
                elements.Add(button);

                elements.Add(new MenuImage
                {
                    id = "0",
                    name = "Difficulty",
                    parent = level.id,
                    rect = new RectValues(new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, 0.5f), new Vector2(8f, 0f)),
                    overrideColor = CoreHelper.GetDifficulty(level.metadata.song.difficulty).color,
                    useOverrideColor = true,
                    opacity = 1f,
                    roundedSide = SpriteHelper.RoundedSide.Left,
                    length = 0f,
                    wait = false,
                });

                if (isSSRank)
                {
                    shine = new MenuImage
                    {
                        id = LSText.randomNumString(16),
                        name = "Shine Base",
                        parent = level.id,
                        rect = RectValues.Default.AnchoredPosition(-440f, 0f).Rotation(15f),
                        opacity = 0f,
                        length = 0f,
                        wait = false,
                    };

                    var shine1 = new MenuImage
                    {
                        id = "0",
                        name = "Shine 1",
                        parent = shine.id,
                        rect = RectValues.Default.AnchoredPosition(-12f, 0f).SizeDelta(8f, 400f),
                        overrideColor = ArcadeConfig.Instance.ShineColor.Value,
                        useOverrideColor = true,
                        opacity = 1f,
                        length = 0f,
                        wait = false,
                    };

                    var shine2 = new MenuImage
                    {
                        id = "0",
                        name = "Shine 2",
                        parent = shine.id,
                        rect = RectValues.Default.AnchoredPosition(12f, 0f).SizeDelta(20f, 400f),
                        overrideColor = ArcadeConfig.Instance.ShineColor.Value,
                        useOverrideColor = true,
                        opacity = 1f,
                        length = 0f,
                        wait = false,
                    };

                    elements.Add(shine);
                    elements.Add(shine1);
                    elements.Add(shine2);
                }

                var deleteButton = new MenuButton
                {
                    id = "0",
                    name = "Delete Queue Button",
                    parentLayout = "deletes",
                    rect = RectValues.Default.SizeDelta(300f, 120f),
                    selectionPosition = new Vector2Int(1, index + 2),
                    func = () =>
                    {
                        LevelManager.ArcadeQueue.RemoveAll(x => x.id == level.id);

                        SetQueuedLevelsPage(Pages[(int)CurrentTab]);
                    },
                    text = "<b><align=center>[ REMOVE ]",
                    opacity = 1f,
                    selectedOpacity = 1f,
                    color = 0,
                    selectedColor = 6,
                    textColor = 6,
                    selectedTextColor = 0,

                    length = regenerateUI ? 0f : 0.01f,
                    playSound = !regenerateUI,
                    wait = !regenerateUI,
                };

                elements.Add(deleteButton);
            }

            if (regenerateUI)
                StartGeneration();
        }

        public void StartQueue()
        {
            InterfaceManager.inst.CloseMenus();
            LevelManager.Play(LevelManager.ArcadeQueue[0], ArcadeHelper.EndOfLevel);
        }

        public void ShuffleQueue(bool play)
        {
            if (LevelManager.Levels.IsEmpty())
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

            var shuffleQueueAmount = ArcadeConfig.Instance.ShuffleQueueAmount.Value;

            var minRandom = UnityEngine.Random.Range(0, levels.Count - shuffleQueueAmount);

            for (int i = 0; i < queueRandom.Count; i++)
            {
                if (i >= minRandom && i - shuffleQueueAmount < minRandom)
                {
                    queue.Add(levels[queueRandom[i]]);
                }
            }

            LevelManager.currentQueueIndex = 0;
            LevelManager.ArcadeQueue.AddRange(queue);

            if (play)
                StartQueue();
            else
            {
                Pages[3] = 0;
                RefreshQueueLevels(true);
            }

            queueRandom.Clear();
            queueRandom = null;
        }

        #endregion

        #region Steam

        public static int SubscribedSteamLevelPageCount => SubscribedSteamLevels.Count / MAX_LEVELS_PER_PAGE;
        public static string SteamSearch => Searches[4];
        public static List<Level> SubscribedSteamLevels => SteamWorkshopManager.inst.Levels.FindAll(level => !level.fromCollection && (string.IsNullOrEmpty(SteamSearch)
                        || level.id == SteamSearch
                        || level.metadata.song.tags.Contains(SteamSearch.ToLower())
                        || level.metadata.artist.Name.ToLower().Contains(SteamSearch.ToLower())
                        || level.metadata.creator.steam_name.ToLower().Contains(SteamSearch.ToLower())
                        || level.metadata.song.title.ToLower().Contains(SteamSearch.ToLower())
                        || level.metadata.song.getDifficulty().ToLower().Contains(SteamSearch.ToLower())));
        public static Dictionary<string, Sprite> OnlineSteamLevelIcons { get; set; } = new Dictionary<string, Sprite>();

        public void SearchSubscribedSteamLevels(string search)
        {
            Searches[4] = search;
            Pages[4] = 0;

            RefreshSubscribedSteamLevels(true, true);
        }

        public void SetSubscribedSteamLevelsPage(int page)
        {
            Pages[4] = Mathf.Clamp(page, 0, SubscribedSteamLevelPageCount);

            RefreshSubscribedSteamLevels(true, true);
        }

        void ClearSubscribedSteamLevelButtons()
        {
            var levelButtons = elements.FindAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name == "Rank" || x.name.Contains("Shine"));

            for (int i = 0; i < levelButtons.Count; i++)
            {
                var levelButton = levelButtons[i];
                levelButton.Clear();
                CoreHelper.Destroy(levelButton.gameObject);
            }
            elements.RemoveAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name == "Rank" || x.name.Contains("Shine"));
        }

        public void RefreshSubscribedSteamLevels(bool regenerateUI, bool clear = false)
        {
            if (clear)
                ClearSubscribedSteamLevelButtons();

            var currentPage = Pages[(int)CurrentTab] + 1;
            int max = currentPage * MAX_LEVELS_PER_PAGE;
            var currentSearch = Searches[(int)CurrentTab];

            var levels = SubscribedSteamLevels;
            for (int i = 0; i < levels.Count; i++)
            {
                int index = i;
                if (index < max - MAX_LEVELS_PER_PAGE || index >= max)
                    continue;

                int column = (index % MAX_LEVELS_PER_PAGE) % 5;
                int row = (int)((index % MAX_LEVELS_PER_PAGE) / 5) + 2;

                var level = levels[index];
                var levelRank = LevelManager.GetLevelRank(level);

                var isSSRank = levelRank.name == "SS";

                MenuImage shine = null;

                var button = new MenuButton
                {
                    id = level.id,
                    name = "Level Button",
                    parentLayout = "levels",
                    selectionPosition = new Vector2Int(column, row),
                    func = () => PlayLevelMenu.Init(level),
                    icon = level.icon,
                    iconRect = RectValues.Default.AnchoredPosition(-90, 30f),
                    text = "<size=24>" + level.metadata?.beatmap?.name,
                    textRect = RectValues.FullAnchored.AnchoredPosition(20f, -50f),
                    enableWordWrapping = true,
                    color = 6,
                    opacity = 0.1f,
                    textColor = 6,
                    selectedColor = 6,
                    selectedOpacity = 1f,
                    selectedTextColor = 7,
                    length = regenerateUI ? 0f : 0.01f,
                    wait = !regenerateUI,
                    playSound = !regenerateUI,
                    mask = true,

                    allowOriginalHoverMethods = true,
                    enterFunc = () =>
                    {
                        if (!isSSRank)
                            return;

                        var animation = new RTAnimation($"{level.id} Level Shine")
                        {
                            animationHandlers = new List<AnimationHandlerBase>
                            {
                                new AnimationHandler<float>(new List<IKeyframe<float>>
                                {
                                    new FloatKeyframe(0f, -240f, Ease.Linear),
                                    new FloatKeyframe(1f, 240f, Ease.CircInOut),
                                }, x => { if (shine != null && shine.gameObject) shine.gameObject.transform.AsRT().anchoredPosition = new Vector2(x, 0f); }),
                            },
                            loop = true,
                        };

                        AnimationManager.inst.Play(animation);
                    },
                    exitFunc = () =>
                    {
                        if (AnimationManager.inst.TryFindAnimations(x => x.name == $"{level.id} Level Shine", out List<RTAnimation> animations))
                            for (int i = 0; i < animations.Count; i++)
                                AnimationManager.inst.Remove(animations[i].id);

                        if (!isSSRank)
                            return;

                        if (shine != null && shine.gameObject)
                            shine.gameObject.transform.AsRT().anchoredPosition = new Vector2(-240f, 0f);
                    },
                };
                elements.Add(button);

                elements.Add(new MenuImage
                {
                    id = "0",
                    name = "Difficulty",
                    parent = level.id,
                    rect = new RectValues(Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(8f, 0f)),
                    overrideColor = CoreHelper.GetDifficulty(level.metadata.song.difficulty).color,
                    useOverrideColor = true,
                    opacity = 1f,
                    roundedSide = SpriteHelper.RoundedSide.Left,
                    length = 0f,
                    wait = false,
                });

                if (levelRank.name != "-")
                    elements.Add(new MenuText
                    {
                        id = "0",
                        name = "Rank",
                        parent = level.id,
                        text = $"<size=70><b><align=center>{levelRank.name}",
                        rect = RectValues.Default.AnchoredPosition(65f, 25f).SizeDelta(64f, 64f),
                        overrideTextColor = levelRank.color,
                        useOverrideTextColor = true,
                        hideBG = true,
                        length = 0f,
                        wait = false,
                    });

                if (isSSRank)
                {
                    shine = new MenuImage
                    {
                        id = LSText.randomNumString(16),
                        name = "Shine Base",
                        parent = level.id,
                        rect = RectValues.Default.AnchoredPosition(-240f, 0f).Rotation(15f),
                        opacity = 0f,
                        length = 0f,
                        wait = false,
                    };

                    var shine1 = new MenuImage
                    {
                        id = "0",
                        name = "Shine 1",
                        parent = shine.id,
                        rect = RectValues.Default.AnchoredPosition(-12f, 0f).SizeDelta(8f, 400f),
                        overrideColor = ArcadeConfig.Instance.ShineColor.Value,
                        useOverrideColor = true,
                        opacity = 1f,
                        length = 0f,
                        wait = false,
                    };

                    var shine2 = new MenuImage
                    {
                        id = "0",
                        name = "Shine 2",
                        parent = shine.id,
                        rect = RectValues.Default.AnchoredPosition(12f, 0f).SizeDelta(20f, 400f),
                        overrideColor = ArcadeConfig.Instance.ShineColor.Value,
                        useOverrideColor = true,
                        opacity = 1f,
                        length = 0f,
                        wait = false,
                    };

                    elements.Add(shine);
                    elements.Add(shine1);
                    elements.Add(shine2);
                }
            }

            if (regenerateUI)
                StartGeneration();
        }

        public void SearchOnlineSteamLevels(string search)
        {
            Searches[4] = search;
            Pages[4] = 0;
        }
        
        public void SetOnlineSteamLevelsPage(int page)
        {
            Pages[4] = Mathf.Clamp(page, 0, int.MaxValue);
            CoroutineHelper.StartCoroutine(RefreshOnlineSteamLevels());
        }

        public IEnumerator RefreshOnlineSteamLevels()
        {
            var levelButtons = elements.FindAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name.Contains("Shine"));

            for (int i = 0; i < levelButtons.Count; i++)
            {
                var levelButton = levelButtons[i];
                levelButton.Clear();
                CoreHelper.Destroy(levelButton.gameObject);
            }
            elements.RemoveAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name.Contains("Shine"));

            yield return CoroutineHelper.Until(() => SteamWorkshopManager.inst.SearchAsync(SteamSearch, Pages[(int)CurrentTab] + 1, (Item item, int index) =>
            {
                int column = (index % MAX_STEAM_ONLINE_LEVELS_PER_PAGE) % 5;
                int row = (int)((index % MAX_STEAM_ONLINE_LEVELS_PER_PAGE) / 5) + 2;
                var id = item.Id.ToString();

                //CoreHelper.Log($"Item: {id}\nTitle: {item.Title}");

                var button = new MenuButton
                {
                    id = id,
                    name = "Level Button",
                    parentLayout = "levels",
                    selectionPosition = new Vector2Int(column, row),
                    func = () => SelectOnlineSteamLevel(item),
                    iconRect = RectValues.Default.AnchoredPosition(-134f, 0f).SizeDelta(64f, 64f),
                    text = "<size=24>" + $"{item.Title}",
                    textRect = RectValues.FullAnchored.AnchorMin(0.24f, 0f),
                    enableWordWrapping = true,
                    icon = SteamWorkshop.inst.defaultSteamImageSprite,
                    color = 6,
                    opacity = 0.1f,
                    textColor = 6,
                    selectedColor = 6,
                    selectedOpacity = 1f,
                    selectedTextColor = 7,
                    length = 0.01f,
                };
                elements.Add(button);

                if (!string.IsNullOrEmpty(id) && OnlineSteamLevelIcons.TryGetValue(id, out Sprite sprite))
                    button.icon = sprite;
                else
                {
                    CoroutineHelper.StartCoroutineAsync(AlephNetwork.DownloadBytes(item.PreviewImageUrl, bytes =>
                    {
                        Core.Threading.TickRunner.Main.Enqueue(() =>
                        {
                            var sprite = SpriteHelper.LoadSprite(bytes);
                            OnlineSteamLevelIcons[id] = sprite;
                            button.icon = sprite;
                            if (button.iconUI)
                                button.iconUI.sprite = sprite;
                        });
                    }, onError =>
                    {
                        Core.Threading.TickRunner.Main.Enqueue(() =>
                        {
                            var sprite = SteamWorkshop.inst.defaultSteamImageSprite;
                            OnlineSteamLevelIcons[id] = sprite;
                            button.icon = sprite;
                            if (button.iconUI)
                                button.iconUI.sprite = sprite;
                        });
                    }));
                }
            }).IsCompleted);
            StartGeneration();
        }

        public void SelectOnlineSteamLevel(Item item) => SteamLevelMenu.Init(item);

        #endregion

        /// <summary>
        /// Runs when the player wants to return to the Input Select screen.
        /// </summary>
        public void Exit()
        {
            InterfaceManager.inst.CloseMenus();
            SceneHelper.LoadScene(SceneName.Main_Menu, false);
        }

        public override void UpdateControls()
        {
            if (CurrentTab == Tab.Queue && !CoreHelper.IsUsingInputField && isOpen && !generating)
            {
                if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.C))
                    ArcadeHelper.CopyArcadeQueue();
                if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.V))
                    ArcadeHelper.PasteArcadeQueue();
            }

            base.UpdateControls();
        }

        public static void Init()
        {
            InterfaceManager.inst.CloseMenus();
            LevelManager.CurrentLevel = null;
            LevelManager.CurrentLevelCollection = null;
            LevelManager.currentLevelIndex = 0;
            LevelManager.currentQueueIndex = 0;
            Current = new ArcadeMenu();
        }
    }
}
