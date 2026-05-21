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
using BetterLegacy.Core.Runtime;
using BetterLegacy.Menus;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Layouts;
using BetterLegacy.Menus.UI.Interfaces;

namespace BetterLegacy.Arcade.Interfaces
{
    /// <summary>
    /// Interface for playing user-made levels.
    /// </summary>
    public class ArcadeInterface : BaseInterface
    {
        #region Constructors
        
        public ArcadeInterface() : base()
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

            for (int i = 0; i < Tab.tabs.Length; i++)
            {
                int index = i;
                var tab = Tab.tabs[index];
                elements.Add(new MenuButton
                {
                    id = (i + 1).ToString(),
                    name = "Tab",
                    parentLayout = "tabs",
                    selectionPosition = new Vector2Int(i + 1, 0),
                    text = $"<align=center><b>[ {tab.DisplayName.ToUpper()} ]",
                    func = () =>
                    {
                        CurrentTab = tab;
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

            var currentPage = CurrentTab.page;
            var currentSearch = CurrentTab.searchTerm;

            switch (CurrentTab)
            {
                case 0: {
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

                        int x = 0;
                        if (!LevelManager.Levels.IsEmpty())
                        {
                            elements.Add(new MenuButton
                            {
                                id = "25428852",
                                name = "Reload Button",
                                text = "<align=center><b>[ RELOAD ]",
                                parentLayout = "local settings",
                                selectionPosition = new Vector2Int(x, 1),
                                rect = RectValues.Default.SizeDelta(200f, 64f),
                                func = LoadLevelsInterface.InitLocal,
                                color = 6,
                                opacity = 0.1f,
                                textColor = 6,
                                selectedColor = 6,
                                selectedOpacity = 1f,
                                selectedTextColor = 7,
                                length = 0.1f,
                                regenerate = false,
                            });
                            x++;
                        }
                        else
                        {
                            elements.Add(new MenuButton
                            {
                                id = "25428852",
                                name = "Reload Button",
                                text = "<size=50><align=center><b>[ RELOAD ]",
                                selectionPosition = new Vector2Int(0, 2),
                                rect = RectValues.Default.SizeDelta(300f, 128f),
                                func = LoadLevelsInterface.InitLocal,
                                color = 6,
                                opacity = 0.1f,
                                textColor = 6,
                                selectedColor = 6,
                                selectedOpacity = 1f,
                                selectedTextColor = 7,
                                length = 0.1f,
                                regenerate = false,
                            });
                        }

                        var sortButton = new MenuButton
                        {
                            id = "25428852",
                            name = "Sort Button",
                            text = $"<align=center><b>[ SORT: {ArcadeConfig.Instance.LocalLevelOrderby.Value} ]",
                            parentLayout = "local settings",
                            selectionPosition = new Vector2Int(x, 1),
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

                        x++;
                        var ascendButton = new MenuButton
                        {
                            id = "25428852",
                            name = "Sort Button",
                            text = $"<align=center><b><rotate={(ArcadeConfig.Instance.LocalLevelAscend.Value ? "90" : "-90")}>>",
                            parentLayout = "local settings",
                            selectionPosition = new Vector2Int(x, 1),
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

                        pageField = new MenuInputField
                        {
                            id = "842848",
                            name = "Page Bar",
                            parentLayout = "local settings",
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            text = currentPage.ToString(),
                            textAnchor = TextAnchor.MiddleCenter,
                            valueChangedFunc = _val => SetLocalLevelsPage(Parser.TryParse(_val, CurrentTab.page)),
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

                        x++;
                        elements.Add(new MenuButton
                        {
                            id = "32848924",
                            name = "Prev Page",
                            text = "<align=center><b><",
                            parentLayout = "local settings",
                            selectionPosition = new Vector2Int(x, 1),
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            func = () =>
                            {
                                if (CurrentTab.page != 0 && pageField.inputField)
                                {
                                    pageField.inputField.text = (CurrentTab.page - 1).ToString();
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

                        x++;
                        elements.Add(new MenuButton
                        {
                            id = "32848924",
                            name = "Next Page",
                            text = "<align=center><b>>",
                            parentLayout = "local settings",
                            selectionPosition = new Vector2Int(x, 1),
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            func = () =>
                            {
                                if (CurrentTab.page != LocalLevelPageCount)
                                {
                                    pageField.inputField.text = (CurrentTab.page + 1).ToString();
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
                    } // Local
                case 1: {
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
                            rect = RectValues.Default.SizeDelta(400, 64f),
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
                            func = () => CoroutineHelper.StartCoroutine(RefreshOnlineLevels()),
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            selectedColor = 6,
                            selectedOpacity = 1f,
                            selectedTextColor = 7,
                            length = 0.1f,
                            regenerate = false,
                        });

                        var subTab = Tab.Online.subTab;

                        elements.Add(new MenuButton
                        {
                            id = "25428852",
                            name = "Search Button",
                            text = $"<align=center><b>[ {(subTab == 0 ? "COLLECTIONS" : "LEVELS")} ]",
                            parentLayout = "online settings",
                            selectionPosition = new Vector2Int(1, 1),
                            rect = RectValues.Default.SizeDelta(300f, 64f),
                            func = () =>
                            {
                                Tab.Online.CycleSubTab();
                                Tab.Online.page = 0;
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

                        var sortButton = new MenuButton
                        {
                            id = "25428852",
                            name = "Sort Button",
                            text = $"<align=center><b>[ SORT: {(subTab == 0 ? ArcadeConfig.Instance.OnlineLevelOrderby.Value : ArcadeConfig.Instance.OnlineLevelCollectionOrderby.Value)} ]",
                            parentLayout = "online settings",
                            selectionPosition = new Vector2Int(2, 1),
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
                            switch (subTab)
                            {
                                case 0: {
                                        var num = (int)ArcadeConfig.Instance.OnlineLevelOrderby.Value;
                                        num++;
                                        if (num >= Enum.GetNames(typeof(OnlineLevelSort)).Length)
                                            num = 0;
                                        ArcadeConfig.Instance.OnlineLevelOrderby.Value = (OnlineLevelSort)num;
                                        sortButton.text = $"<align=center><b>[ SORT: {ArcadeConfig.Instance.OnlineLevelOrderby.Value} ]";
                                        if (sortButton.textUI)
                                        {
                                            sortButton.textUI.maxVisibleCharacters = 9999;
                                            sortButton.textUI.text = sortButton.text;
                                        }
                                        break;
                                    }
                                case 1: {
                                        var num = (int)ArcadeConfig.Instance.OnlineLevelCollectionOrderby.Value;
                                        num++;
                                        if (num >= Enum.GetNames(typeof(OnlineLevelCollectionSort)).Length)
                                            num = 0;
                                        ArcadeConfig.Instance.OnlineLevelCollectionOrderby.Value = (OnlineLevelCollectionSort)num;
                                        sortButton.text = $"<align=center><b>[ SORT: {ArcadeConfig.Instance.OnlineLevelCollectionOrderby.Value} ]";
                                        if (sortButton.textUI)
                                        {
                                            sortButton.textUI.maxVisibleCharacters = 9999;
                                            sortButton.textUI.text = sortButton.text;
                                        }
                                        break;
                                    }
                            }

                            if (ArcadeConfig.Instance.AutoSearch.Value)
                                CoroutineHelper.StartCoroutine(RefreshOnlineLevels());
                        };
                        elements.Add(sortButton);

                        var ascendButton = new MenuButton
                        {
                            id = "25428852",
                            name = "Sort Button",
                            text = $"<align=center><b><rotate={((subTab == 0 ? ArcadeConfig.Instance.OnlineLevelAscend.Value : ArcadeConfig.Instance.OnlineLevelCollectionAscend.Value) ? "90" : "-90")}>>",
                            parentLayout = "online settings",
                            selectionPosition = new Vector2Int(3, 1),
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
                            switch (subTab)
                            {
                                case 0: {
                                        ArcadeConfig.Instance.OnlineLevelAscend.Value = !ArcadeConfig.Instance.OnlineLevelAscend.Value;
                                        ascendButton.text = $"<align=center><b><rotate={(ArcadeConfig.Instance.OnlineLevelAscend.Value ? "90" : "-90")}>>";
                                        break;
                                    }
                                case 1: {
                                        ArcadeConfig.Instance.OnlineLevelCollectionAscend.Value = !ArcadeConfig.Instance.OnlineLevelCollectionAscend.Value;
                                        ascendButton.text = $"<align=center><b><rotate={(ArcadeConfig.Instance.OnlineLevelCollectionAscend.Value ? "90" : "-90")}>>";
                                        break;
                                    }
                            }

                            if (ascendButton.textUI)
                            {
                                ascendButton.textUI.maxVisibleCharacters = 9999;
                                ascendButton.textUI.text = ascendButton.text;
                            }

                            if (ArcadeConfig.Instance.AutoSearch.Value)
                                CoroutineHelper.StartCoroutine(RefreshOnlineLevels());
                        };
                        elements.Add(ascendButton);

                        pageField = new MenuInputField
                        {
                            id = "842848",
                            name = "Page Bar",
                            parentLayout = "online settings",
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            text = currentPage.ToString(),
                            textAnchor = TextAnchor.MiddleCenter,
                            valueChangedFunc = _val => SetOnlineLevelsPage(Parser.TryParse(_val, CurrentTab.page)),
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
                            parentLayout = "online settings",
                            selectionPosition = new Vector2Int(4, 1),
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            func = () =>
                            {
                                if (CurrentTab.page != 0)
                                    SetOnlineLevelsPage(CurrentTab.page - 1);
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

                        elements.Add(pageField);

                        elements.Add(new MenuButton
                        {
                            id = "32848924",
                            name = "Next Page",
                            text = "<align=center><b>>",
                            parentLayout = "online settings",
                            selectionPosition = new Vector2Int(5, 1),
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            func = () => SetOnlineLevelsPage(CurrentTab.page + 1),
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
                    } // Online
                case 2: {
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

                        pageField = new MenuInputField
                        {
                            id = "842848",
                            name = "Page Bar",
                            parentLayout = "browser settings",
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            text = currentPage.ToString(),
                            textAnchor = TextAnchor.MiddleCenter,
                            valueChangedFunc = _val => SetBrowserPage(Parser.TryParse(_val, CurrentTab.page)),
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
                                if (CurrentTab.page != 0 && pageField.inputField)
                                {
                                    pageField.inputField.text = (CurrentTab.page - 1).ToString();
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
                                if (CurrentTab.page != BrowserPageCount)
                                {
                                    pageField.inputField.text = (CurrentTab.page + 1).ToString();
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
                    } // Browser
                case 3: {
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

                        pageField = new MenuInputField
                        {
                            id = "842848",
                            name = "Page Bar",
                            parentLayout = "queue settings",
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            text = currentPage.ToString(),
                            textAnchor = TextAnchor.MiddleCenter,
                            valueChangedFunc = _val => SetQueuedLevelsPage(Parser.TryParse(_val, CurrentTab.page)),
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
                                if (CurrentTab.page != 0 && pageField.inputField)
                                {
                                    pageField.inputField.text = (CurrentTab.page - 1).ToString();
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
                                if (CurrentTab.page != QueuePageCount)
                                {
                                    pageField.inputField.text = (CurrentTab.page + 1).ToString();
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
                    } // Queue
                case 4: {
                        if (!RTSteamManager.inst.Initialized)
                        {
                            elements.Add(new MenuText
                            {
                                id = "5136326",
                                name = "Init message",
                                text = Lang.Current.GetOrDefault("arcade.steam.no_access", "<size=40><align=center><b>Steam could not be accessed.\nOpen Steam and relaunch the game to access the workshop."),
                                rect = RectValues.Default.SizeDelta(600f, 300f),
                                length = 0.1f,
                                regenerate = false,
                                hideBG = true,
                            });
                            break;
                        }

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
                            rect = RectValues.Default.SizeDelta(Tab.Steam.subTab == 0 ? 404f : 468f, 64f),
                            text = currentSearch,
                            valueChangedFunc = Tab.Steam.subTab == 1 ? SearchOnlineSteamLevels : SearchSubscribedSteamLevels,
                            placeholder = "Search levels...",
                            color = 6,
                            opacity = 0.1f,
                            textColor = 6,
                            placeholderColor = 6,
                            length = 0.1f,
                            wait = false,
                            regenerate = false,
                        });

                        int x = 0;
                        if (Tab.Steam.subTab == 1)
                        {
                            elements.Add(new MenuButton
                            {
                                id = "25428852",
                                name = "Search Button",
                                text = "<align=center><b>[ SEARCH ]",
                                parentLayout = "steam settings",
                                selectionPosition = new Vector2Int(x, 1),
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

                            x++;
                            var sortButton = new MenuButton
                            {
                                id = "25428852",
                                name = "Sort Button",
                                text = $"<align=center><b>[ SORT: {ArcadeConfig.Instance.SteamWorkshopOrderby.Value} ]",
                                parentLayout = "steam settings",
                                selectionPosition = new Vector2Int(x, 1),
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
                            if (!RTSteamManager.inst.Levels.IsEmpty())
                            {
                                elements.Add(new MenuButton
                                {
                                    id = "25428852",
                                    name = "Reload Button",
                                    text = "<align=center><b>[ RELOAD ]",
                                    parentLayout = "steam settings",
                                    selectionPosition = new Vector2Int(x, 1),
                                    rect = RectValues.Default.SizeDelta(200f, 64f),
                                    func = LoadLevelsInterface.InitSteam,
                                    color = 6,
                                    opacity = 0.1f,
                                    textColor = 6,
                                    selectedColor = 6,
                                    selectedOpacity = 1f,
                                    selectedTextColor = 7,
                                    length = 0.1f,
                                    regenerate = false,
                                });
                                x++;
                            }
                            else
                            {
                                elements.Add(new MenuButton
                                {
                                    id = "25428852",
                                    name = "Reload Button",
                                    text = "<size=50><align=center><b>[ RELOAD ]",
                                    selectionPosition = new Vector2Int(0, 2),
                                    rect = RectValues.Default.SizeDelta(300f, 128f),
                                    func = LoadLevelsInterface.InitSteam,
                                    color = 6,
                                    opacity = 0.1f,
                                    textColor = 6,
                                    selectedColor = 6,
                                    selectedOpacity = 1f,
                                    selectedTextColor = 7,
                                    length = 0.1f,
                                    regenerate = false,
                                });
                            }

                            var sortButton = new MenuButton
                            {
                                id = "25428852",
                                name = "Sort Button",
                                text = $"<align=center><b>[ SORT: {ArcadeConfig.Instance.SteamLevelOrderby.Value} ]",
                                parentLayout = "steam settings",
                                selectionPosition = new Vector2Int(x, 1),
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

                            x++;
                            var ascendButton = new MenuButton
                            {
                                id = "25428852",
                                name = "Sort Button",
                                text = $"<align=center><b><rotate={(ArcadeConfig.Instance.SteamLevelAscend.Value ? "90" : "-90")}>>",
                                parentLayout = "steam settings",
                                selectionPosition = new Vector2Int(x, 1),
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

                        pageField = new MenuInputField
                        {
                            id = "842848",
                            name = "Page Bar",
                            parentLayout = "steam settings",
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            text = currentPage.ToString(),
                            textAnchor = TextAnchor.MiddleCenter,
                            valueChangedFunc = _val =>
                            {
                                if (Tab.Steam.subTab == 1)
                                    SetOnlineSteamLevelsPage(Parser.TryParse(_val, CurrentTab.page));
                                else
                                    SetSubscribedSteamLevelsPage(Parser.TryParse(_val, CurrentTab.page));
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

                                    if (Tab.Steam.subTab == 0 && SubscribedSteamLevelPageCount != 0)
                                        result = Mathf.Clamp(result, 0, SubscribedSteamLevelPageCount);

                                    if (inputField.text != result.ToString())
                                    {
                                        inputField.text = result.ToString();
                                        SoundManager.inst.PlaySound(DefaultSounds.menuflip);
                                    }
                                }
                            }),
                        };

                        x++;
                        elements.Add(new MenuButton
                        {
                            id = "32848924",
                            name = "Prev Page",
                            text = "<align=center><b><",
                            parentLayout = "steam settings",
                            selectionPosition = new Vector2Int(x, 1),
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            func = () =>
                            {
                                if (CurrentTab.page != 0 && pageField.inputField)
                                {
                                    pageField.inputField.text = (CurrentTab.page - 1).ToString();
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

                        x++;
                        elements.Add(new MenuButton
                        {
                            id = "32848924",
                            name = "Next Page",
                            text = "<align=center><b>>",
                            parentLayout = "steam settings",
                            selectionPosition = new Vector2Int(x, 1),
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            func = () =>
                            {
                                if (Tab.Steam.subTab == 1 || CurrentTab.page != SubscribedSteamLevelPageCount)
                                {
                                    pageField.inputField.text = (CurrentTab.page + 1).ToString();
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

                        x++;
                        elements.Add(new MenuButton
                        {
                            id = "32848924",
                            name = "Switch Steam View",
                            text = $"<align=center><b>[ {(Tab.Steam.subTab == 1 ? "VIEW SUBSCRIBED" : "VIEW ONLINE")} ]",
                            parentLayout = "steam settings",
                            selectionPosition = new Vector2Int(x, 1),
                            rect = RectValues.Default.SizeDelta(300f, 64f),
                            func = () =>
                            {
                                Tab.Steam.CycleSubTab();
                                Tab.Steam.page = 0;
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
                            cellSize = new Vector2(350f, Tab.Steam.subTab == 1 ? 70f : 180f),
                            spacing = new Vector2(12f, 12f),
                            constraint = GridLayoutGroup.Constraint.FixedColumnCount,
                            constraintCount = 5,
                            regenerate = false,
                        });

                        if (Tab.Steam.subTab == 1)
                            CoroutineHelper.StartCoroutine(RefreshOnlineSteamLevels());
                        else
                            RefreshSubscribedSteamLevels(false);

                        break;
                    } // Steam
            }

            defaultSelection = new Vector2Int(1, 0);
            exitFunc = Exit;
            if (CurrentTab != Tab.Online && (CurrentTab != Tab.Steam || Tab.Steam.subTab == 0))
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
                                PlayLevelInterface.Init(level);
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
                                PlayLevelInterface.Init(level);
                            }
                            break;
                        }
                    }
                };
            };
            InterfaceManager.inst.PlayMusic();

            if (CurrentTab == Tab.Online)
                CoroutineHelper.StartCoroutine(RefreshOnlineLevels());
        }

        #endregion

        #region Values

        /// <summary>
        /// The current <see cref="ArcadeInterface"/>.
        /// </summary>
        public static ArcadeInterface Current { get; set; }

        /// <summary>
        /// The current tab.
        /// </summary>
        public static Tab CurrentTab { get; set; } = Tab.Local;

        /// <summary>
        /// Max amount of levels to view per page.
        /// </summary>
        public const int MAX_LEVELS_PER_PAGE = 20;

        /// <summary>
        /// Max amount of queued levels to view per page.
        /// </summary>
        public const int MAX_QUEUED_PER_PAGE = 6;

        /// <summary>
        /// Max amount of Steam Workshop levels to view per page.
        /// </summary>
        public const int MAX_STEAM_ONLINE_LEVELS_PER_PAGE = 50;

        MenuInputField pageField;

        #region Local

        /// <summary>
        /// Local level page count.
        /// </summary>
        public static int LocalLevelPageCount => (LocalLevelCollections.Count + LocalLevels.Count) / MAX_LEVELS_PER_PAGE;

        /// <summary>
        /// List of levels to view.
        /// </summary>
        public static List<Level> LocalLevels => LevelManager.Levels.FindAll(level => !level.fromCollection &&
            RTString.SearchString(Tab.Local.searchTerm,
                new SearchMatcher(level.id, SearchMatchType.Exact),
                new SearchListMatcher(level.metadata?.tags),
                level.metadata?.artist?.name,
                level.metadata?.creator?.name,
                level.metadata?.song?.title,
                level.metadata?.beatmap?.name,
                level.metadata?.song?.Difficulty.DisplayName.GetText()
                ));

        /// <summary>
        /// List of level collections to view.
        /// </summary>
        public static List<LevelCollection> LocalLevelCollections => LevelManager.LevelCollections.FindAll(collection => string.IsNullOrEmpty(Tab.Local.searchTerm)
                        || collection.id == Tab.Local.searchTerm
                        || collection.name.ToLower().Contains(Tab.Local.searchTerm.ToLower()));

        #endregion

        #region Online

        /// <summary>
        /// Online level count.
        /// </summary>
        public static int OnlineLevelCount { get; set; }

        /// <summary>
        /// Cached online level icons.
        /// </summary>
        public static Dictionary<string, Sprite> OnlineLevelIcons { get; set; } = new Dictionary<string, Sprite>();

        /// <summary>
        /// True if online levels are loading.
        /// </summary>
        public bool loadingOnlineLevels;

        #endregion

        #region Browser

        /// <summary>
        /// File browser page count.
        /// </summary>
        public static int BrowserPageCount => BrowserFolders.Length / MAX_LEVELS_PER_PAGE;

        /// <summary>
        /// The current directory for the file browser.
        /// </summary>
        public static string BrowserCurrentDirectory { get; set; } = RTFile.ApplicationDirectory;

        /// <summary>
        /// Array of browser folders in <see cref="BrowserCurrentDirectory"/>.
        /// </summary>
        public static string[] BrowserFolders =>
            Directory.GetDirectories(BrowserCurrentDirectory)
                    .Where(x => string.IsNullOrEmpty(Tab.Browser.searchTerm) || Path.GetFileName(x).ToLower().Contains(Tab.Browser.searchTerm.ToLower()) || Level.TryVerify(x + "/", false, out Level level) &&
                        (level.id == Tab.Browser.searchTerm
                        || level.metadata.tags.Contains(Tab.Browser.searchTerm.ToLower())
                        || level.metadata.artist.name.ToLower().Contains(Tab.Browser.searchTerm.ToLower())
                        || level.metadata.creator.name.ToLower().Contains(Tab.Browser.searchTerm.ToLower())
                        || level.metadata.song.title.ToLower().Contains(Tab.Browser.searchTerm.ToLower())
                        || level.metadata.song.Difficulty.DisplayName.ToLower().Contains(Tab.Browser.searchTerm.ToLower()))).ToArray();

        #endregion

        #region Queue

        /// <summary>
        /// Queued level page count.
        /// </summary>
        public static int QueuePageCount => QueueLevels.Count / MAX_QUEUED_PER_PAGE;

        /// <summary>
        /// List of queued levels to view.
        /// </summary>
        public static List<Level> QueueLevels => LevelManager.ArcadeQueue.FindAll(level => !level.fromCollection &&
            RTString.SearchString(Tab.Queue.searchTerm,
                new SearchMatcher(level.id, SearchMatchType.Exact),
                new SearchListMatcher(level.metadata.tags),
                level.metadata.artist.name,
                level.metadata.creator.name,
                level.metadata.song.title,
                level.metadata.beatmap.name,
                level.metadata.song.Difficulty.DisplayName.GetText()
                ));

        #endregion

        #region Steam

        /// <summary>
        /// Subscribed Steam Workshop level page count.
        /// </summary>
        public static int SubscribedSteamLevelPageCount => SubscribedSteamLevels.Count / MAX_LEVELS_PER_PAGE;

        /// <summary>
        /// List of subscribed Steam Workshop levels to view.
        /// </summary>
        public static List<Level> SubscribedSteamLevels => RTSteamManager.inst.Levels.FindAll(level => !level.fromCollection &&
            RTString.SearchString(Tab.Steam.searchTerm,
                new SearchMatcher(level.id, SearchMatchType.Exact),
                new SearchListMatcher(level.metadata.tags),
                level.metadata.artist.name,
                level.metadata.creator.name,
                level.metadata.song.title,
                level.metadata.beatmap.name,
                level.metadata.song.Difficulty.DisplayName.GetText()
                ));

        /// <summary>
        /// Cached Steam Workshop level icons.
        /// </summary>
        public static Dictionary<string, Sprite> OnlineSteamLevelIcons { get; set; } = new Dictionary<string, Sprite>();

        #endregion

        #endregion

        #region Functions

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

        /// <summary>
        /// Initializes <see cref="ArcadeInterface"/>.
        /// </summary>
        public static void Init()
        {
            InterfaceManager.inst.CloseMenus();
            LevelManager.CurrentLevel = null;
            LevelManager.CurrentLevelCollection = null;
            LevelManager.currentLevelIndex = 0;
            LevelManager.currentQueueIndex = 0;
            Current = new ArcadeInterface();
        }

        #region Local

        /// <summary>
        /// Searches the local levels.
        /// </summary>
        /// <param name="search">Search term.</param>
        public void SearchLocalLevels(string search)
        {
            Tab.Local.searchTerm = search;
            Tab.Local.page = 0;
            if (pageField && pageField.inputField)
                pageField.inputField.SetTextWithoutNotify("0");

            RefreshLocalLevels(true);
        }

        /// <summary>
        /// Sets the local levels page.
        /// </summary>
        /// <param name="page">Page to set.</param>
        public void SetLocalLevelsPage(int page)
        {
            Tab.Local.page = Mathf.Clamp(page, 0, LocalLevelPageCount);
            if (pageField && pageField.inputField)
                pageField.inputField.SetTextWithoutNotify(Tab.Local.page.ToString());

            RefreshLocalLevels(true);
        }

        void ClearLocalLevelButtons() => ClearElements(x => x.name == "Level Button" || x.name == "Difficulty" || x.name == "Rank" || x.name.Contains("Shine") || x.name.Contains("Lock"));

        /// <summary>
        /// Refreshes the local levels view.
        /// </summary>
        /// <param name="regenerateUI">If the UI should be regenerated.</param>
        /// <param name="clear">If the UI should be cleared.</param>
        public void RefreshLocalLevels(bool regenerateUI, bool clear = true)
        {
            if (clear)
                ClearLocalLevelButtons();

            var currentPage = CurrentTab.page + 1;
            int max = currentPage * MAX_LEVELS_PER_PAGE;
            var currentSearch = CurrentTab.searchTerm;

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

                var rank = collection.isFolder ? Rank.Null : collection.GetRank();

                var isSSRank = rank == Rank.SS;

                MenuImage shine = null;

                elements.Add(new MenuButton
                {
                    id = collection.id,
                    name = "Level Button",
                    parentLayout = "levels",
                    selectionPosition = new Vector2Int(column, row),
                    func = () =>
                    {
                        if (collection.isFolder)
                        {
                            CoreHelper.Log($"Set path to: {collection.path}");
                            LoadLevelsInterface.Init(collection.path);
                            return;
                        }

                        LevelManager.currentQueueIndex = 0;
                        LevelManager.CurrentLevelCollection = collection;
                        LevelCollectionInterface.Init(collection);
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

                    allowOriginalHoverMethods = true,
                    enterFunc = () =>
                    {
                        if (!isSSRank)
                            return;

                        var animation = new RTAnimation($"{collection.id} Level Shine")
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
                        if (AnimationManager.inst.TryFindAnimations(x => x.name == $"{collection.id} Level Shine", out List<RTAnimation> animations))
                            for (int i = 0; i < animations.Count; i++)
                                AnimationManager.inst.Remove(animations[i].id);

                        if (!isSSRank)
                            return;

                        if (shine != null && shine.gameObject)
                            shine.gameObject.transform.AsRT().anchoredPosition = new Vector2(-240f, 0f);
                    },
                });

                if (!collection.isFolder)
                    elements.Add(new MenuImage
                    {
                        id = "0",
                        name = "Difficulty",
                        parent = collection.id,
                        rect = new RectValues(Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(8f, 0f)),
                        overrideColor = collection.Difficulty.Color,
                        useOverrideColor = true,
                        opacity = 1f,
                        roundedSide = SpriteHelper.RoundedSide.Left,
                        length = 0f,
                        wait = false,
                    });

                if (rank != Rank.Null)
                    elements.Add(new MenuText
                    {
                        id = "0",
                        name = "Rank",
                        parent = collection.id,
                        text = $"<size=70><b><align=center>{rank.DisplayName}",
                        rect = RectValues.Default.AnchoredPosition(65f, 25f).SizeDelta(64f, 64f),
                        overrideTextColor = rank.Color,
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
                        parent = collection.id,
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
                var rank = LevelManager.GetLevelRank(level);

                var isSSRank = rank == Rank.SS;

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
                    PlayLevelInterface.Init(level);
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
                    overrideColor = level.metadata.song.Difficulty.Color,
                    useOverrideColor = true,
                    opacity = 1f,
                    roundedSide = SpriteHelper.RoundedSide.Left,
                    length = 0f,
                    wait = false,
                });

                if (rank != Rank.Null)
                    elements.Add(new MenuText
                    {
                        id = "0",
                        name = "Rank",
                        parent = level.id,
                        text = $"<size=70><b><align=center>{rank.DisplayName}",
                        rect = RectValues.Default.AnchoredPosition(65f, 25f).SizeDelta(64f, 64f),
                        overrideTextColor = rank.Color,
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

        /// <summary>
        /// Searches the online levels.
        /// </summary>
        /// <param name="search">Search term.</param>
        public void SearchOnlineLevels(string search)
        {
            Tab.Online.searchTerm = search;
            Tab.Online.page = 0;
            if (pageField && pageField.inputField)
                pageField.inputField.SetTextWithoutNotify("0");

            if (ArcadeConfig.Instance.AutoSearch.Value)
                CoroutineHelper.StartCoroutine(RefreshOnlineLevels());
        }

        /// <summary>
        /// Sets the online levels page.
        /// </summary>
        /// <param name="page">Page to set.</param>
        public void SetOnlineLevelsPage(int page)
        {
            Tab.Online.page = page;
            if (pageField && pageField.inputField)
                pageField.inputField.SetTextWithoutNotify(Tab.Online.page.ToString());

            if (ArcadeConfig.Instance.AutoSearch.Value)
                CoroutineHelper.StartCoroutine(RefreshOnlineLevels());
        }

        void ClearOnlineLevelButtons() => ClearElements(x => x.name == "Level Button" || x.name == "Difficulty");

        /// <summary>
        /// Refreshes the online levels view.
        /// </summary>
        public IEnumerator RefreshOnlineLevels()
        {
            if (loadingOnlineLevels)
                yield break;

            ClearOnlineLevelButtons();

            var currentTab = Tab.Online.subTab;

            var page = Tab.Online.page;
            int currentPage = page + 1;

            var search = Tab.Online.searchTerm;
            var sort = currentTab == 0 ? (int)ArcadeConfig.Instance.OnlineLevelOrderby.Value : (int)ArcadeConfig.Instance.OnlineLevelCollectionOrderby.Value;
            var ascend = currentTab == 0 ? ArcadeConfig.Instance.OnlineLevelAscend.Value : ArcadeConfig.Instance.OnlineLevelCollectionAscend.Value;

            string query = AlephNetwork.BuildQuery(currentTab == 0 ? AlephNetwork.LevelSearchURL : AlephNetwork.LevelCollectionSearchURL, search, page, sort, ascend);

            CoreHelper.Log($"Search query: {query}");

            if (string.IsNullOrEmpty(query))
                yield break;

            loadingOnlineLevels = true;
            var headers = new Dictionary<string, string>();
            if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null)
                headers["Authorization"] = $"Bearer {LegacyPlugin.authData["access_token"].Value}";

            CoreHelper.Log($"Downloading from query.");

            var coverURL = currentTab == 0 ? AlephNetwork.LevelCoverURL : AlephNetwork.LevelCollectionCoverURL;

            yield return CoroutineHelper.StartCoroutine(AlephNetwork.DownloadJSONFile(query, json =>
            {
                CoreHelper.Log($"Got result from query.");

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
                                func = () => DownloadLevelInterface.Init(item.AsObject, currentTab),
                                iconRect = RectValues.Default.AnchoredPosition(-90, 30f),
                                text = "<size=24>" + name,
                                textRect = RectValues.FullAnchored.AnchoredPosition(20f, -50f),
                                enableWordWrapping = true,
                                icon = LegacyPlugin.AtanPlaceholder,
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
                                overrideColor = CustomEnumHelper.GetValueOrDefault(difficulty, DifficultyType.Unknown).Color,
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
                                CoroutineHelper.StartCoroutine(AlephNetwork.DownloadBytes($"{coverURL}{id}{FileFormat.JPG.Dot()}?r" + UnityRandom.Range(0, int.MaxValue), bytes =>
                                {
                                    var sprite = SpriteHelper.LoadSprite(bytes);
                                    OnlineLevelIcons[id] = sprite;
                                    button.icon = sprite;
                                    if (button.iconUI)
                                        button.iconUI.sprite = sprite;
                                }, onError =>
                                {
                                    var sprite = LegacyPlugin.AtanPlaceholder;
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
                loadingOnlineLevels = false;
            }, headers));

            loadingOnlineLevels = false;
            StartGeneration();
        }

        #endregion
        
        #region Browser

        /// <summary>
        /// Searches the file browser.
        /// </summary>
        /// <param name="search">Search term.</param>
        public void SearchBrowser(string search)
        {
            Tab.Browser.searchTerm = search;
            Tab.Browser.page = 0;
            if (pageField && pageField.inputField)
                pageField.inputField.SetTextWithoutNotify("0");

            RefreshBrowserLevels(true);
        }

        /// <summary>
        /// Sets the file browser page.
        /// </summary>
        /// <param name="page">Page to set.</param>
        public void SetBrowserPage(int page)
        {
            Tab.Browser.page = Mathf.Clamp(page, 0, BrowserPageCount);
            if (pageField && pageField.inputField)
                pageField.inputField.SetTextWithoutNotify(Tab.Browser.page.ToString());

            RefreshBrowserLevels(true);
        }

        void ClearBrowserButtons() => ClearElements(x => x.name == "Level Button" || x.name == "Difficulty" || x.name.Contains("Shine"));

        /// <summary>
        /// Refreshes the file browser view.
        /// </summary>
        /// <param name="regenerateUI">If the UI should be regenerated.</param>
        public void RefreshBrowserLevels(bool regenerateUI)
        {
            ClearBrowserButtons();

            var currentPage = CurrentTab.page + 1;
            int max = currentPage * (MAX_LEVELS_PER_PAGE - 1);
            var currentSearch = CurrentTab.searchTerm;

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
                    var isSSRank = LevelManager.GetLevelRank(level) == Rank.SS;

                    MenuImage shine = null;

                    var button = new MenuButton
                    {
                        id = level.id,
                        name = "Level Button",
                        parentLayout = "levels",
                        selectionPosition = new Vector2Int(column, row),
                        func = () => PlayLevelInterface.Init(level),
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
                        overrideColor = level.metadata.song.Difficulty.Color,
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

        /// <summary>
        /// Searches the queued levels.
        /// </summary>
        /// <param name="search">Search term.</param>
        public void SearchQueuedLevels(string search)
        {
            Tab.Queue.searchTerm = search;
            Tab.Queue.page = 0;
            if (pageField && pageField.inputField)
                pageField.inputField.SetTextWithoutNotify("0");

            RefreshQueueLevels(true);
        }

        /// <summary>
        /// Sets the queued levels page.
        /// </summary>
        /// <param name="page">Page to set.</param>
        public void SetQueuedLevelsPage(int page)
        {
            Tab.Queue.page = Mathf.Clamp(page, 0, QueuePageCount);
            if (pageField && pageField.inputField)
                pageField.inputField.SetTextWithoutNotify(Tab.Queue.page.ToString());

            RefreshQueueLevels(true);
        }

        void ClearQueueLevelButtons() => ClearElements(x => x.name == "Level Button" || x.name == "Difficulty" || x.name == "Delete Queue Button" || x.name.Contains("Shine"));

        /// <summary>
        /// Refreshes the queued levels view.
        /// </summary>
        /// <param name="regenerateUI">If the UI should be regenerated.</param>
        public void RefreshQueueLevels(bool regenerateUI)
        {
            // x = 800f
            // y = 180f

            ClearQueueLevelButtons();

            var currentPage = CurrentTab.page + 1;
            int max = currentPage * MAX_QUEUED_PER_PAGE;
            var currentSearch = CurrentTab.searchTerm;

            var levels = QueueLevels;
            for (int i = 0; i < levels.Count; i++)
            {
                int index = i;
                if (index < max - MAX_QUEUED_PER_PAGE || index >= max)
                    continue;

                var level = levels[index];

                var isSSRank = LevelManager.GetLevelRank(level) == Rank.SS;

                MenuImage shine = null;

                var button = new MenuButton
                {
                    id = level.id,
                    name = "Level Button",
                    parentLayout = "levels",
                    rect = RectValues.Default.SizeDelta(800f, 120f),
                    selectionPosition = new Vector2Int(0, index + 2),
                    func = () => PlayLevelInterface.Init(level),
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
                    overrideColor = level.metadata.song.Difficulty.Color,
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

                        SetQueuedLevelsPage(CurrentTab.page);
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

        /// <summary>
        /// Begins the level queue.
        /// </summary>
        public void StartQueue()
        {
            InterfaceManager.inst.CloseMenus();
            LevelManager.Play(LevelManager.ArcadeQueue[0], RTBeatmap.Current.EndOfLevel);
        }

        /// <summary>
        /// Shuffles the level queue.
        /// </summary>
        /// <param name="play">If the queue should begin.</param>
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

            var levels = LevelManager.Levels.Union(RTSteamManager.inst.Levels).ToList();

            for (int i = 0; i < levels.Count; i++)
            {
                queueRandom.Add(i);
            }

            queueRandom = queueRandom.OrderBy(x => -(x - UnityRandom.Range(0, levels.Count))).ToList();

            var shuffleQueueAmount = ArcadeConfig.Instance.ShuffleQueueAmount.Value;

            var minRandom = UnityRandom.Range(0, levels.Count - shuffleQueueAmount);

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
                Tab.Queue.page = 0;
                RefreshQueueLevels(true);
            }

            queueRandom.Clear();
            queueRandom = null;
        }

        #endregion

        #region Steam

        /// <summary>
        /// Searches the subscribed Steam levels.
        /// </summary>
        /// <param name="search">Search term.</param>
        public void SearchSubscribedSteamLevels(string search)
        {
            Tab.Steam.searchTerm = search;
            Tab.Steam.page = 0;
            if (pageField && pageField.inputField)
                pageField.inputField.SetTextWithoutNotify("0");

            RefreshSubscribedSteamLevels(true, true);
        }

        /// <summary>
        /// Sets the subscribed Steam levels page.
        /// </summary>
        /// <param name="page">Page to set.</param>
        public void SetSubscribedSteamLevelsPage(int page)
        {
            Tab.Steam.page = Mathf.Clamp(page, 0, SubscribedSteamLevelPageCount);
            if (pageField && pageField.inputField)
                pageField.inputField.SetTextWithoutNotify(Tab.Steam.page.ToString());

            RefreshSubscribedSteamLevels(true, true);
        }

        void ClearSubscribedSteamLevelButtons() => ClearElements(x => x.name == "Level Button" || x.name == "Difficulty" || x.name == "Rank" || x.name.Contains("Shine"));

        /// <summary>
        /// Refreshes the subscribed Steam levels view.
        /// </summary>
        /// <param name="regenerateUI">If the UI should be regenerated.</param>
        /// <param name="clear">If the UI should be cleared.</param>
        public void RefreshSubscribedSteamLevels(bool regenerateUI, bool clear = false)
        {
            if (clear)
                ClearSubscribedSteamLevelButtons();

            var currentPage = CurrentTab.page + 1;
            int max = currentPage * MAX_LEVELS_PER_PAGE;
            var currentSearch = CurrentTab.searchTerm;

            var levels = SubscribedSteamLevels;
            for (int i = 0; i < levels.Count; i++)
            {
                int index = i;
                if (index < max - MAX_LEVELS_PER_PAGE || index >= max)
                    continue;

                int column = (index % MAX_LEVELS_PER_PAGE) % 5;
                int row = (int)((index % MAX_LEVELS_PER_PAGE) / 5) + 2;

                var level = levels[index];
                var rank = LevelManager.GetLevelRank(level);

                var isSSRank = rank == Rank.SS;

                MenuImage shine = null;

                var button = new MenuButton
                {
                    id = level.id,
                    name = "Level Button",
                    parentLayout = "levels",
                    selectionPosition = new Vector2Int(column, row),
                    func = () => PlayLevelInterface.Init(level),
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
                    overrideColor = level.metadata.song.Difficulty.Color,
                    useOverrideColor = true,
                    opacity = 1f,
                    roundedSide = SpriteHelper.RoundedSide.Left,
                    length = 0f,
                    wait = false,
                });

                if (rank != Rank.Null)
                    elements.Add(new MenuText
                    {
                        id = "0",
                        name = "Rank",
                        parent = level.id,
                        text = $"<size=70><b><align=center>{rank.DisplayName}",
                        rect = RectValues.Default.AnchoredPosition(65f, 25f).SizeDelta(64f, 64f),
                        overrideTextColor = rank.Color,
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

        /// <summary>
        /// Searches the online Steam levels.
        /// </summary>
        /// <param name="search">Search term.</param>
        public void SearchOnlineSteamLevels(string search)
        {
            Tab.Steam.searchTerm = search;
            Tab.Steam.page = 0;
        }

        /// <summary>
        /// Sets the online Steam levels page.
        /// </summary>
        /// <param name="page">Page to set.</param>
        public void SetOnlineSteamLevelsPage(int page)
        {
            Tab.Steam.page = Mathf.Clamp(page, 0, int.MaxValue);
            if (pageField && pageField.inputField)
                pageField.inputField.SetTextWithoutNotify(Tab.Steam.page.ToString());

            CoroutineHelper.StartCoroutine(RefreshOnlineSteamLevels());
        }

        void ClearOnlineSteamLevelButtons() => ClearElements(x => x.name == "Level Button" || x.name == "Difficulty" || x.name.Contains("Shine"));

        /// <summary>
        /// Refreshes the online Steam levels view.
        /// </summary>
        public IEnumerator RefreshOnlineSteamLevels()
        {
            ClearOnlineSteamLevelButtons();

            yield return CoroutineHelper.Until(() => RTSteamManager.inst.SearchAsync(Tab.Steam.searchTerm, CurrentTab.page + 1, (Item item, int index) =>
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
                    func = () => SteamLevelInterface.Init(item),
                    iconRect = RectValues.Default.AnchoredPosition(-134f, 0f).SizeDelta(64f, 64f),
                    text = "<size=24>" + $"{item.Title}",
                    textRect = RectValues.FullAnchored.AnchorMin(0.24f, 0f),
                    enableWordWrapping = true,
                    icon = LegacyPlugin.AtanPlaceholder,
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
                        LegacyPlugin.MainTick += () =>
                        {
                            var sprite = SpriteHelper.LoadSprite(bytes);
                            OnlineSteamLevelIcons[id] = sprite;
                            button.icon = sprite;
                            if (button.iconUI)
                                button.iconUI.sprite = sprite;
                        };
                    }, onError =>
                    {
                        LegacyPlugin.MainTick += () =>
                        {
                            var sprite = LegacyPlugin.AtanPlaceholder;
                            OnlineSteamLevelIcons[id] = sprite;
                            button.icon = sprite;
                            if (button.iconUI)
                                button.iconUI.sprite = sprite;
                        };
                    }));
                }
            }).IsCompleted);
            StartGeneration();
        }

        #endregion

        #endregion

        #region Sub Classes

        /// <summary>
        /// Represents a tab for the arcade interface.
        /// </summary>
        public class Tab : Exists
        {
            #region Constructors

            public Tab() { }

            public Tab(int ordinal, string displayName, int subTabCount = 1)
            {
                this.ordinal = ordinal;
                DisplayName = displayName;
                SubTabCount = subTabCount;
            }

            #endregion

            #region Values

            /// <summary>
            /// Tab view for local levels and level collections.
            /// </summary>
            public static Tab Local { get; set; } = new Tab(0, nameof(Local));

            /// <summary>
            /// Tab view for online levels and level collections.
            /// </summary>
            public static Tab Online { get; set; } = new Tab(1, nameof(Online), 2);

            /// <summary>
            /// Tab view for file browsing.
            /// </summary>
            public static Tab Browser { get; set; } = new Tab(2, nameof(Browser));

            /// <summary>
            /// Tab view for queued levels.
            /// </summary>
            public static Tab Queue { get; set; } = new Tab(3, nameof(Queue));

            /// <summary>
            /// Tab view for Steam Workshop levels.
            /// </summary>
            public static Tab Steam { get; set; } = new Tab(4, nameof(Steam), 2);

            /// <summary>
            /// Array of tabs.
            /// </summary>
            public static Tab[] tabs = new Tab[]
            {
                Local,
                Online,
                Browser,
                Queue,
                Steam,
            };

            /// <summary>
            /// Display name of the tab.
            /// </summary>
            public string DisplayName { get; }

            /// <summary>
            /// Amount of tabs.
            /// </summary>
            public int SubTabCount { get; }

            /// <summary>
            /// Tab search term.
            /// </summary>
            public string searchTerm;

            /// <summary>
            /// Tab page.
            /// </summary>
            public int page;

            /// <summary>
            /// Tab sub tab.
            /// </summary>
            public int subTab;

            /// <summary>
            /// Ordinal value of the tab.
            /// </summary>
            public int ordinal;

            #endregion

            #region Functions

            /// <summary>
            /// Cycles the sub tab.
            /// </summary>
            public void CycleSubTab()
            {
                subTab++;
                if (subTab >= SubTabCount)
                    subTab = 0;
            }

            #endregion

            #region Operators

            public static implicit operator int(Tab tab) => tab.ordinal;

            #endregion
        }

        #endregion
    }
}
