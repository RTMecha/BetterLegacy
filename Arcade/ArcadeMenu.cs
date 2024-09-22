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
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using LSFunctions;
using System.Collections;
using BetterLegacy.Core.Managers.Networking;
using SimpleJSON;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.IO;
using InControl;

namespace BetterLegacy.Arcade
{
    // Probably not gonna use this
    public class ArcadeMenu : MenuBase
    {
        public static bool useThisUI = true;

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

        public const int MAX_LEVELS_PER_PAGE = 20;
        public const int MAX_QUEUED_PER_PAGE = 5;
        public const int MAX_STEAM_LEVELS_PER_PAGE = 35;

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
            InterfaceManager.inst.CurrentMenu = this;

            regenerate = false;

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
                func = () =>
                {
                    InterfaceManager.inst.CloseMenus();
                    SceneManager.inst.LoadScene("Input Select");
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
                case Tab.Local:
                    {
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
                            rect = RectValues.Default.SizeDelta(1368f, 64f),
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
                                        inputField.text = result.ToString();
                                }
                            }),
                        };

                        elements.Add(new MenuButton
                        {
                            id = "32848924",
                            name = "Prev Page",
                            text = "<align=center><b><",
                            parentLayout = "local settings",
                            selectionPosition = new Vector2Int(0, 1),
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            func = () =>
                            {
                                if (Pages[(int)CurrentTab] != 0 && pageField.inputField)
                                    pageField.inputField.text = (Pages[(int)CurrentTab] - 1).ToString();
                                else
                                    AudioManager.inst.PlaySound("Block");
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

                        elements.Add(pageField);

                        elements.Add(new MenuButton
                        {
                            id = "32848924",
                            name = "Next Page",
                            text = "<align=center><b>>",
                            parentLayout = "local settings",
                            selectionPosition = new Vector2Int(1, 1),
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            func = () =>
                            {
                                if (Pages[(int)CurrentTab] != LocalLevelPageCount)
                                    pageField.inputField.text = (Pages[(int)CurrentTab] + 1).ToString();
                                else
                                    AudioManager.inst.PlaySound("Block");
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
                            cellSize = new Vector2(350f, 180f),
                            spacing = new Vector2(12f, 12f),
                            constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount,
                            constraintCount = 5,
                            regenerate = false,
                        });

                        RefreshLocalLevels(false);

                        break;
                    }
                case Tab.Online:
                    {
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
                            length = 0.1f,
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
                            func = () => { CoreHelper.StartCoroutine(RefreshOnlineLevels()); },
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
                                    AudioManager.inst.PlaySound("Block");
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
                            func = () =>
                            {
                                SetOnlineLevelsPage(Pages[(int)CurrentTab] + 1);
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
                            cellSize = new Vector2(350f, 180f),
                            spacing = new Vector2(12f, 12f),
                            constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount,
                            constraintCount = 5,
                            regenerate = false,
                        });

                        break;
                    }
                case Tab.Browser:
                    {
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
                                        inputField.text = result.ToString();
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
                                    pageField.inputField.text = (Pages[(int)CurrentTab] - 1).ToString();
                                else
                                    AudioManager.inst.PlaySound("Block");
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
                                    pageField.inputField.text = (Pages[(int)CurrentTab] + 1).ToString();
                                else
                                    AudioManager.inst.PlaySound("Block");
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
                case Tab.Queue:
                    {
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
                            rect = RectValues.Default.SizeDelta(1368f, 64f),
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
                                        inputField.text = result.ToString();
                                }
                            }),
                        };

                        elements.Add(new MenuButton
                        {
                            id = "32848924",
                            name = "Prev Page",
                            text = "<align=center><b><",
                            parentLayout = "queue settings",
                            selectionPosition = new Vector2Int(0, 1),
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            func = () =>
                            {
                                if (Pages[(int)CurrentTab] != 0 && pageField.inputField)
                                    pageField.inputField.text = (Pages[(int)CurrentTab] - 1).ToString();
                                else
                                    AudioManager.inst.PlaySound("Block");
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

                        elements.Add(pageField);

                        elements.Add(new MenuButton
                        {
                            id = "32848924",
                            name = "Next Page",
                            text = "<align=center><b>>",
                            parentLayout = "queue settings",
                            selectionPosition = new Vector2Int(1, 1),
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            func = () =>
                            {
                                if (Pages[(int)CurrentTab] != QueuePageCount)
                                    pageField.inputField.text = (Pages[(int)CurrentTab] + 1).ToString();
                                else
                                    AudioManager.inst.PlaySound("Block");
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

                        layouts.Add("levels", new MenuVerticalLayout
                        {
                            name = "levels",
                            rect = RectValues.Default.AnchoredPosition(100f, 100f).SizeDelta(800f, 400f),
                            spacing = 12f,
                            childControlHeight = false,
                            childControlWidth = false,
                            regenerate = false,
                        });
                        
                        layouts.Add("deletes", new MenuVerticalLayout
                        {
                            name = "deletes",
                            rect = RectValues.Default.AnchoredPosition(960f, 100f).SizeDelta(800f, 400f),
                            spacing = 12f,
                            childControlHeight = false,
                            childControlWidth = false,
                            regenerate = false,
                        });

                        RefreshQueueLevels(false);

                        break;
                    }
                case Tab.Steam:
                    {
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
                            rect = RectValues.Default.SizeDelta(1068f, 64f),
                            text = currentSearch,
                            valueChangedFunc = SearchSubscribedSteamLevels,
                            placeholder = "Search levels...",
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
                            parentLayout = "steam settings",
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            text = currentPage.ToString(),
                            textAnchor = TextAnchor.MiddleCenter,
                            valueChangedFunc = _val => SetSubscribedSteamLevelsPage(Parser.TryParse(_val, Pages[(int)CurrentTab])),
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

                                    if (SubscribedSteamLevelPageCount != 0)
                                        result = Mathf.Clamp(result, 0, SubscribedSteamLevelPageCount);

                                    if (inputField.text != result.ToString())
                                        inputField.text = result.ToString();
                                }
                            }),
                        };

                        elements.Add(new MenuButton
                        {
                            id = "32848924",
                            name = "Prev Page",
                            text = "<align=center><b><",
                            parentLayout = "steam settings",
                            selectionPosition = new Vector2Int(0, 1),
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            func = () =>
                            {
                                if (Pages[(int)CurrentTab] != 0 && pageField.inputField)
                                    pageField.inputField.text = (Pages[(int)CurrentTab] - 1).ToString();
                                else
                                    AudioManager.inst.PlaySound("Block");
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

                        elements.Add(pageField);

                        elements.Add(new MenuButton
                        {
                            id = "32848924",
                            name = "Next Page",
                            text = "<align=center><b>>",
                            parentLayout = "steam settings",
                            selectionPosition = new Vector2Int(1, 1),
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            func = () =>
                            {
                                if (Pages[(int)CurrentTab] != SubscribedSteamLevelPageCount)
                                    pageField.inputField.text = (Pages[(int)CurrentTab] + 1).ToString();
                                else
                                    AudioManager.inst.PlaySound("Block");
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
                            text = "<align=center><b>[ VIEW ONLINE ]",
                            parentLayout = "steam settings",
                            selectionPosition = new Vector2Int(2, 1),
                            rect = RectValues.Default.SizeDelta(300f, 64f),
                            func = () =>
                            {

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
                            cellSize = new Vector2(350f, 105f),
                            spacing = new Vector2(12f, 12f),
                            constraint = GridLayoutGroup.Constraint.FixedColumnCount,
                            constraintCount = 5,
                            regenerate = false,
                        });

                        RefreshSubscribedSteamLevels(false);

                        break;
                    }
            }

            exitFunc = () => SceneManager.inst.LoadScene("Input Select");

            CoreHelper.StartCoroutine(GenerateUI());
        }

        #region Local

        public static int LocalLevelPageCount => LocalLevels.Count / MAX_LEVELS_PER_PAGE;
        public static string LocalSearch => Searches[0];
        public static List<Level> LocalLevels => LevelManager.Levels.FindAll(level => !level.fromCollection && (string.IsNullOrEmpty(LocalSearch)
                        || level.id == LocalSearch
                        || level.metadata.LevelSong.tags.Contains(LocalSearch.ToLower())
                        || level.metadata.artist.Name.ToLower().Contains(LocalSearch.ToLower())
                        || level.metadata.creator.steam_name.ToLower().Contains(LocalSearch.ToLower())
                        || level.metadata.song.title.ToLower().Contains(LocalSearch.ToLower())
                        || level.metadata.song.getDifficulty().ToLower().Contains(LocalSearch.ToLower())));

        public void SearchLocalLevels(string search)
        {
            Searches[0] = search;
            Pages[0] = 0;

            var levelButtons = elements.FindAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name.Contains("Shine"));

            for (int i = 0; i < levelButtons.Count; i++)
            {
                var levelButton = levelButtons[i];
                levelButton.Clear();
                CoreHelper.Destroy(levelButton.gameObject);
            }
            elements.RemoveAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name.Contains("Shine"));
            RefreshLocalLevels(true);
        }

        public void SetLocalLevelsPage(int page)
        {
            Pages[0] = Mathf.Clamp(page, 0, LocalLevelPageCount);

            var levelButtons = elements.FindAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name.Contains("Shine"));

            for (int i = 0; i < levelButtons.Count; i++)
            {
                var levelButton = levelButtons[i];
                levelButton.Clear();
                CoreHelper.Destroy(levelButton.gameObject);
            }
            elements.RemoveAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name.Contains("Shine"));
            RefreshLocalLevels(true);
        }

        public void RefreshLocalLevels(bool regenerateUI)
        {
            var currentPage = Pages[(int)CurrentTab] + 1;
            int max = currentPage * MAX_LEVELS_PER_PAGE;
            var currentSearch = Searches[(int)CurrentTab];

            var levels = LocalLevels;
            for (int i = 0; i < levels.Count; i++)
            {
                int index = i;
                if (index < max - MAX_LEVELS_PER_PAGE || index >= max)
                    continue;

                int column = (index % MAX_LEVELS_PER_PAGE) % 5;
                int row = (int)((index % MAX_LEVELS_PER_PAGE) / 5) + 2;

                var level = levels[index];

                var isSSRank = LevelManager.GetLevelRank(level).name == "SS";

                MenuImage shine = null;

                var button = new MenuButton
                {
                    id = level.id,
                    name = "Level Button",
                    parentLayout = "levels",
                    selectionPosition = new Vector2Int(column, row),
                    func = () => { CoreHelper.StartCoroutine(SelectLocalLevel(level)); },
                    icon = level.icon,
                    iconRect = RectValues.Default.AnchoredPosition(-90, 30f),
                    text = "<size=24>" + level.metadata?.LevelBeatmap?.name,
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
                    mask = true,

                    allowOriginalHoverMethods = true,
                    enterFunc = () =>
                    {
                        if (AnimationManager.inst.animations.TryFindAll(x => x.name.Contains("Level Shine"), out List<RTAnimation> animations))
                            for (int i = 0; i < animations.Count; i++)
                                AnimationManager.inst.RemoveID(animations[i].id);

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
                        if (AnimationManager.inst.animations.TryFindAll(x => x.name.Contains("Level Shine"), out List<RTAnimation> animations))
                            for (int i = 0; i < animations.Count; i++)
                                AnimationManager.inst.RemoveID(animations[i].id);

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
                CoreHelper.StartCoroutine(GenerateUI());
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

        #endregion

        #region Online

        public static string SearchURL => $"{AlephNetworkManager.ArcadeServerURL}api/level/search";
        public static string CoverURL => $"{AlephNetworkManager.ArcadeServerURL}api/level/cover/";
        public static string DownloadURL => $"{AlephNetworkManager.ArcadeServerURL}api/level/zip/";

        public static string OnlineSearch => Searches[1];

        public static int OnlineLevelCount { get; set; }

        public static Dictionary<string, Sprite> OnlineLevelIcons { get; set; } = new Dictionary<string, Sprite>();

        public void SetOnlineLevelsPage(int page)
        {
            Pages[1] = page;
        }

        public void SearchOnlineLevels(string search)
        {
            Searches[1] = search;
            Pages[1] = 0;
        }

        string ReplaceSpace(string search) => search.ToLower().Replace(" ", "+");

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
                        !string.IsNullOrEmpty(search) && page == 0 ? $"{SearchURL}?query={ReplaceSpace(search)}" :
                            !string.IsNullOrEmpty(search) ? $"{SearchURL}?query={ReplaceSpace(search)}&page={page}" : "";

            CoreHelper.Log($"Search query: {query}");

            if (string.IsNullOrEmpty(query))
                yield break;

            loadingOnlineLevels = true;
            var headers = new Dictionary<string, string>();
            if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null)
                headers["Authorization"] = $"Bearer {LegacyPlugin.authData["access_token"].Value}";

            yield return CoreHelper.StartCoroutine(AlephNetworkManager.DownloadJSONFile(query, json =>
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
                                func = () => { SelectOnlineLevel(item.AsObject); },
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
                                CoreHelper.StartCoroutine(AlephNetworkManager.DownloadBytes($"{CoverURL}{id}.jpg", bytes =>
                                {
                                    var sprite = SpriteHelper.LoadSprite(bytes);
                                    OnlineLevelIcons.Add(id, sprite);
                                    button.icon = sprite;
                                    if (button.iconUI)
                                        button.iconUI.sprite = sprite;
                                }, onError =>
                                {
                                    var sprite = SteamWorkshop.inst.defaultSteamImageSprite;
                                    OnlineLevelIcons.Add(id, sprite);
                                    button.icon = sprite;
                                    if (button.iconUI)
                                        button.iconUI.sprite = sprite;
                                }));
                            }

                            num++;
                        }
                    }

                    if (jn["count"] != null)
                    {
                        OnlineLevelCount = jn["count"].AsInt;
                    }
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
            }, headers));

            loadingOnlineLevels = false;
            CoreHelper.StartCoroutine(GenerateUI());
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
                        || level.metadata.LevelSong.tags.Contains(BrowserSearch.ToLower())
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

                var folder = folders[index].Replace("\\", "/");

                if (Level.TryVerify(folder + "/", true, out Level level))
                {
                    var isSSRank = LevelManager.GetLevelRank(level).name == "SS";

                    MenuImage shine = null;

                    var button = new MenuButton
                    {
                        id = level.id,
                        name = "Level Button",
                        parentLayout = "levels",
                        selectionPosition = new Vector2Int(column, row),
                        func = () => { CoreHelper.StartCoroutine(SelectLocalLevel(level)); },
                        icon = level.icon,
                        iconRect = RectValues.Default.AnchoredPosition(-90, 30f),
                        text = "<size=24>" + level.metadata?.LevelBeatmap?.name,
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

                            if (AnimationManager.inst.animations.TryFind(x => x.name.Contains("Level Shine"), out RTAnimation animation))
                                AnimationManager.inst.RemoveID(animation.id);

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
                        mask = true,
                    });
                }

                num++;
            }

            if (regenerateUI)
                CoreHelper.StartCoroutine(GenerateUI());
        }

        #endregion

        #region Queue

        public static int QueuePageCount => QueueLevels.Count / MAX_QUEUED_PER_PAGE;
        public static string QueueSearch => Searches[3];
        public static List<Level> QueueLevels => LevelManager.ArcadeQueue.FindAll(level => !level.fromCollection && (string.IsNullOrEmpty(QueueSearch)
                        || level.id == QueueSearch
                        || level.metadata.LevelSong.tags.Contains(QueueSearch.ToLower())
                        || level.metadata.artist.Name.ToLower().Contains(QueueSearch.ToLower())
                        || level.metadata.creator.steam_name.ToLower().Contains(QueueSearch.ToLower())
                        || level.metadata.song.title.ToLower().Contains(QueueSearch.ToLower())
                        || level.metadata.song.getDifficulty().ToLower().Contains(QueueSearch.ToLower())));

        public void SearchQueuedLevels(string search)
        {
            Searches[3] = search;
            Pages[3] = 0;

            var levelButtons = elements.FindAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name == "Delete Queue Button" || x.name.Contains("Shine"));

            for (int i = 0; i < levelButtons.Count; i++)
            {
                var levelButton = levelButtons[i];
                levelButton.Clear();
                CoreHelper.Destroy(levelButton.gameObject);
            }
            elements.RemoveAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name == "Delete Queue Button" || x.name.Contains("Shine"));
            RefreshQueueLevels(true);
        }

        public void SetQueuedLevelsPage(int page)
        {
            Pages[3] = Mathf.Clamp(page, 0, QueuePageCount);

            var levelButtons = elements.FindAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name == "Delete Queue Button" || x.name.Contains("Shine"));

            for (int i = 0; i < levelButtons.Count; i++)
            {
                var levelButton = levelButtons[i];
                levelButton.Clear();
                CoreHelper.Destroy(levelButton.gameObject);
            }
            elements.RemoveAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name == "Delete Queue Button" || x.name.Contains("Shine"));
            RefreshQueueLevels(true);
        }

        public void RefreshQueueLevels(bool regenerateUI)
        {
            // x = 800f
            // y = 180f

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
                    func = () => { CoreHelper.StartCoroutine(SelectLocalLevel(level)); },
                    icon = level.icon,
                    iconRect = RectValues.Default.AnchoredPosition(-320f, 0f).SizeDelta(100f, 100f),
                    text = "<size=32>" + i.ToString() + " - " + level.metadata?.LevelBeatmap?.name,
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
                    mask = true,

                    allowOriginalHoverMethods = true,
                    enterFunc = () =>
                    {
                        if (AnimationManager.inst.animations.TryFindAll(x => x.name.Contains("Level Shine"), out List<RTAnimation> animations))
                            for (int i = 0; i < animations.Count; i++)
                                AnimationManager.inst.RemoveID(animations[i].id);

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

                        if (AnimationManager.inst.animations.TryFindAll(x => x.name.Contains("Level Shine"), out List<RTAnimation> animations))
                            for (int i = 0; i < animations.Count; i++)
                                AnimationManager.inst.RemoveID(animations[i].id);

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
                    wait = !regenerateUI,
                };

                elements.Add(deleteButton);
            }

            if (regenerateUI)
                CoreHelper.StartCoroutine(GenerateUI());
        }

        #endregion

        #region Steam

        public static int SubscribedSteamLevelPageCount => SubscribedSteamLevels.Count / MAX_STEAM_LEVELS_PER_PAGE;
        public static string SteamSearch => Searches[4];
        public static List<Level> SubscribedSteamLevels => SteamWorkshopManager.inst.Levels.FindAll(level => !level.fromCollection && (string.IsNullOrEmpty(SteamSearch)
                        || level.id == SteamSearch
                        || level.metadata.LevelSong.tags.Contains(SteamSearch.ToLower())
                        || level.metadata.artist.Name.ToLower().Contains(SteamSearch.ToLower())
                        || level.metadata.creator.steam_name.ToLower().Contains(SteamSearch.ToLower())
                        || level.metadata.song.title.ToLower().Contains(SteamSearch.ToLower())
                        || level.metadata.song.getDifficulty().ToLower().Contains(SteamSearch.ToLower())));

        public void SearchSubscribedSteamLevels(string search)
        {
            Searches[4] = search;
            Pages[4] = 0;

            var levelButtons = elements.FindAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name.Contains("Shine"));

            for (int i = 0; i < levelButtons.Count; i++)
            {
                var levelButton = levelButtons[i];
                levelButton.Clear();
                CoreHelper.Destroy(levelButton.gameObject);
            }
            elements.RemoveAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name.Contains("Shine"));
            RefreshSubscribedSteamLevels(true);
        }

        public void SetSubscribedSteamLevelsPage(int page)
        {
            Pages[4] = Mathf.Clamp(page, 0, SubscribedSteamLevelPageCount);

            var levelButtons = elements.FindAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name.Contains("Shine"));

            for (int i = 0; i < levelButtons.Count; i++)
            {
                var levelButton = levelButtons[i];
                levelButton.Clear();
                CoreHelper.Destroy(levelButton.gameObject);
            }
            elements.RemoveAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name.Contains("Shine"));
            RefreshSubscribedSteamLevels(true);
        }

        public void RefreshSubscribedSteamLevels(bool regenerateUI)
        {
            var currentPage = Pages[(int)CurrentTab] + 1;
            int max = currentPage * MAX_STEAM_LEVELS_PER_PAGE;
            var currentSearch = Searches[(int)CurrentTab];

            var levels = SubscribedSteamLevels;
            for (int i = 0; i < levels.Count; i++)
            {
                int index = i;
                if (index < max - MAX_STEAM_LEVELS_PER_PAGE || index >= max)
                    continue;

                int column = (index % MAX_STEAM_LEVELS_PER_PAGE) % 5;
                int row = (int)((index % MAX_STEAM_LEVELS_PER_PAGE) / 5) + 2;

                var level = levels[index];

                var isSSRank = LevelManager.GetLevelRank(level).name == "SS";

                MenuImage shine = null;

                var button = new MenuButton
                {
                    id = level.id,
                    name = "Level Button",
                    parentLayout = "levels",
                    selectionPosition = new Vector2Int(column, row),
                    func = () => { CoreHelper.StartCoroutine(SelectLocalLevel(level)); },
                    icon = level.icon,
                    iconRect = RectValues.Default.AnchoredPosition(-134f, 0f).SizeDelta(64f, 64f),
                    text = "<size=24>" + level.metadata?.LevelBeatmap?.name,
                    textRect = RectValues.FullAnchored.AnchorMin(0.24f, 0f),
                    enableWordWrapping = true,
                    color = 6,
                    opacity = 0.1f,
                    textColor = 6,
                    selectedColor = 6,
                    selectedOpacity = 1f,
                    selectedTextColor = 7,
                    length = regenerateUI ? 0f : 0.01f,
                    wait = !regenerateUI,
                    mask = true,

                    allowOriginalHoverMethods = true,
                    enterFunc = () =>
                    {
                        if (AnimationManager.inst.animations.TryFindAll(x => x.name.Contains("Level Shine"), out List<RTAnimation> animations))
                            for (int i = 0; i < animations.Count; i++)
                                AnimationManager.inst.RemoveID(animations[i].id);

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
                        if (AnimationManager.inst.animations.TryFindAll(x => x.name.Contains("Level Shine"), out List<RTAnimation> animations))
                            for (int i = 0; i < animations.Count; i++)
                                AnimationManager.inst.RemoveID(animations[i].id);

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
                CoreHelper.StartCoroutine(GenerateUI());
        }

        #endregion

        public override void UpdateTheme()
        {
            if (Parser.TryParse(MenuConfig.Instance.InterfaceThemeID.Value, -1) >= 0 && InterfaceManager.inst.themes.TryFind(x => x.id == MenuConfig.Instance.InterfaceThemeID.Value, out BeatmapTheme interfaceTheme))
                Theme = interfaceTheme;
            else
                Theme = InterfaceManager.inst.themes[0];

            base.UpdateTheme();
        }

        public static void Init()
        {
            InterfaceManager.inst.CloseMenus();
            // testing
            if (ArcadeMenuManager.inst)
            {
                CoreHelper.Destroy(ArcadeMenuManager.inst);
                CoreHelper.Destroy(ArcadeMenuManager.inst.menuUI);
            }

            Current = new ArcadeMenu();
        }
    }
}
