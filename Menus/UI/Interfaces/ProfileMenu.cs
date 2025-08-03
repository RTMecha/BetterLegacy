using UnityEngine;
using UnityEngine.EventSystems;

using LSFunctions;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Layouts;

namespace BetterLegacy.Menus.UI.Interfaces
{
    public class ProfileMenu : MenuBase
    {
        public static ProfileMenu Current { get; set; }

        public static void Init()
        {
            InterfaceManager.inst.CloseMenus();
            Current = new ProfileMenu();
        }

        public enum Tab
        {
            Level,
            Achievement,
        }

        public static Tab CurrentTab { get; set; }
        public static int[] Pages { get; set; } = new int[]
        {
            0, // Level
            0, // Achievement
        };

        public static int CurrentPage
        {
            get => Pages[(int)CurrentTab];
            set => Pages[(int)CurrentTab] = value;
        }

        public ProfileMenu() : base()
        {
            InterfaceManager.inst.CurrentInterface = this;
            id = InterfaceManager.PROFILE_MENU_ID;

            musicName = InterfaceManager.RANDOM_MUSIC_NAME;
            name = "Profile";

            elements.Add(new MenuEvent
            {
                id = "09",
                name = "Effects",
                func = MenuEffectsManager.inst.SetDefaultEffects,
                length = 0f,
                regenerate = false,
            });

            layouts.Add("settings", new MenuHorizontalLayout
            {
                name = "settings",
                rect = RectValues.HorizontalAnchored.AnchoredPosition(0f, 350f).SizeDelta(-126f, 64f),
                childForceExpandWidth = true,
                regenerate = false,
            });

            elements.Add(new MenuButton
            {
                id = "32848924",
                name = "Return",
                text = "<align=center><b>[ RETURN ]",
                parentLayout = "settings",
                selectionPosition = new Vector2Int(0, 0),
                rect = RectValues.Default.SizeDelta(400f, 64f),
                func = () => InterfaceManager.inst.SetCurrentInterface(InterfaceManager.EXTRAS_MENU_ID),
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

            elements.Add(new MenuImage
            {
                id = "1",
                name = "Spacer",
                parentLayout = "settings",
                length = 0.3f,
                rect = RectValues.Default.SizeDelta(100f, 64f),
                regenerate = false,
                opacity = 0f,
            });

            for (int i = 0; i < 2; i++)
            {
                int index = i;
                var name = i switch
                {
                    0 => "LEVEL SAVES",
                    1 => "ACHIEVEMENTS",
                    _ => string.Empty,
                };
                elements.Add(new MenuButton
                {
                    id = (i + 1).ToString(),
                    name = "Tab",
                    parentLayout = "settings",
                    selectionPosition = new Vector2Int(i + 1, 0),
                    text = $"<align=center><b>[ {name} ]",
                    rect = RectValues.Default.SizeDelta(400f, 64f),
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

            #region Page

            var pageField = new MenuInputField
            {
                id = "842848",
                name = "Page Bar",
                parentLayout = "settings",
                rect = RectValues.Default.SizeDelta(132f, 64f),
                text = CurrentPage.ToString(),
                textAnchor = TextAnchor.MiddleCenter,
                valueChangedFunc = _val =>
                {
                    if (!int.TryParse(_val, out int num))
                        return;

                    CurrentPage = Mathf.Clamp(num, 0, ElementCount);

                    switch (CurrentTab)
                    {
                        case Tab.Level: {
                                RefreshLevelSaveData(true);
                                break;
                            }
                        case Tab.Achievement: {
                                RefreshAchievements(true);
                                break;
                            }
                    }
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

                        if (AchievementPageCount != 0)
                            result = Mathf.Clamp(result, 0, ElementCount);

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
                parentLayout = "settings",
                selectionPosition = new Vector2Int(3, 0),
                rect = RectValues.Default.SizeDelta(132f, 64f),
                func = () =>
                {
                    if (CurrentPage != 0 && pageField.inputField)
                    {
                        pageField.inputField.text = (CurrentPage - 1).ToString();
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
                parentLayout = "settings",
                selectionPosition = new Vector2Int(4, 0),
                rect = RectValues.Default.SizeDelta(132f, 64f),
                func = () =>
                {
                    if (CurrentPage != ElementCount)
                    {
                        pageField.inputField.text = (CurrentPage + 1).ToString();
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

            #endregion

            layouts.Add("buttons", new MenuVerticalLayout
            {
                name = "buttons",
                spacing = 4f,
                childControlWidth = true,
                childForceExpandWidth = true,
                rect = RectValues.Default.SizeDelta(1400f, 600f),
                regenerate = false,
            });

            switch (CurrentTab)
            {
                case Tab.Level: {
                        RefreshLevelSaveData(false, false);
                        break;
                    }
                case Tab.Achievement: {
                        RefreshAchievements(false, false);
                        break;
                    }
            }

            exitFunc = () => InterfaceManager.inst.SetCurrentInterface(InterfaceManager.EXTRAS_MENU_ID);
            StartGeneration();
            InterfaceManager.inst.PlayMusic();
        }

        public const int MAX_ELEMENTS_PER_PAGE = 5;
        public static int ElementCount => CurrentTab switch
        {
            Tab.Level => LevelPageCount,
            Tab.Achievement => AchievementPageCount,
            _ => 0,
        };

        #region Level Save Data

        public static int LevelPageCount => LevelManager.Saves.Count / MAX_ELEMENTS_PER_PAGE;

        public void ClearLevelSaveDataButtons() => ClearElements(x => x.name == "Element Base" || x.name == "Delete Save");

        public void RefreshLevelSaveData(bool regenerateUI, bool clear = true)
        {
            if (clear)
                ClearLevelSaveDataButtons();

            var currentPage = CurrentPage + 1;
            int max = currentPage * MAX_ELEMENTS_PER_PAGE;

            int num = 0;
            for (int i = 0; i < LevelManager.Saves.Count; i++)
            {
                int index = num;
                if (index < max - MAX_ELEMENTS_PER_PAGE || index >= max)
                {
                    num++;
                    continue;
                }

                var save = LevelManager.Saves[i];
                var id = LSText.randomNumString(16);

                var rank = LevelManager.GetLevelRank(save.Hits);

                var elementBase = new MenuButton
                {
                    id = id,
                    name = "Element Base",
                    parentLayout = "buttons",
                    rect = RectValues.Default.SizeDelta(800f, 120f),
                    text = $" <size=40><b><#{LSColors.ColorToHex(rank.Color)}>{rank.DisplayName}</color></b></size>  {save.LevelName} - ID: {save.ID}",
                    selectionPosition = new Vector2Int(0, index + 1),
                    opacity = 0.1f,
                    selectedOpacity = 1f,
                    color = 6,
                    selectedColor = 6,
                    textColor = 6,
                    length = regenerateUI ? 0f : 0.01f,
                    wait = !regenerateUI,
                    playSound = !regenerateUI,
                };

                var delete = new MenuButton
                {
                    id = "0",
                    name = "Delete Save",
                    parent = id,
                    rect = RectValues.Default.AnchoredPosition(550f, 0f).SizeDelta(126f, 120f),
                    text = "[ DELETE ]",
                    selectionPosition = new Vector2Int(1, index + 1),
                    opacity = 1f,
                    selectedOpacity = 1f,
                    color = 0,
                    selectedColor = 6,
                    textColor = 6,
                    selectedTextColor = 0,
                    func = () =>
                    {
                        LevelManager.Saves.RemoveAt(index);
                        elements.RemoveAll(x => x.id == id);
                        CoreHelper.Destroy(elementBase.gameObject);
                        elementBase = null;
                    },
                    length = regenerateUI ? 0f : 0.01f,
                    wait = !regenerateUI,
                    playSound = !regenerateUI,
                };

                elements.Add(elementBase);
                elements.Add(delete);
                num++;
            }

            if (regenerateUI)
                StartGeneration();
        }

        #endregion

        #region Achievements

        public static int AchievementPageCount => AchievementManager.globalAchievements.Count / MAX_ELEMENTS_PER_PAGE;

        public void ClearAchievements() => ClearElements(x => x.name == "Achievement Button" || x.name == "Difficulty");

        public void RefreshAchievements(bool regenerateUI, bool clear = true)
        {
            if (clear)
                ClearAchievements();

            var currentPage =  CurrentPage + 1;
            int max = currentPage * MAX_ELEMENTS_PER_PAGE;

            var achievements = AchievementManager.globalAchievements;
            int num = 0;
            for (int i = 0; i < achievements.Count; i++)
            {
                var achievement = achievements[i];

                int index = num;
                if (index < max - MAX_ELEMENTS_PER_PAGE || index >= max)
                {
                    num++;
                    continue;
                }

                MenuButton menuButton;
                if (achievement.hidden && !achievement.unlocked)
                {
                    menuButton = new MenuButton
                    {
                        id = PAObjectBase.GetStringID(),
                        name = "Achievement Button",
                        parentLayout = "buttons",
                        selectionPosition = new Vector2Int(0, num + 1),
                        rect = RectValues.Default.SizeDelta(800f, 120f),
                        text = $"<b><size=24><font=Arrhythmia>HIDDEN ACHIEVEMENT</font></b>\n{achievement.GetHint()}",
                        //alignment = TMPro.TextAlignmentOptions.TopLeft,
                        textRect = RectValues.FullAnchored.AnchoredPosition(120f, 30f),
                        icon = LegacyPlugin.PALogoSprite,
                        iconRect = RectValues.Default.AnchoredPosition(-640f, 0f).SizeDelta(110f, 110f),
                        enableWordWrapping = true,
                        func = () => SoundManager.inst.PlaySound(DefaultSounds.Block),

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
                    };
                }
                else
                {
                    var icon = achievement.icon ?? LegacyPlugin.AtanPlaceholder;
                    if (!achievement.unlocked && achievement.lockedIcon)
                        icon = achievement.lockedIcon;

                    menuButton = new MenuButton
                    {
                        id = PAObjectBase.GetStringID(),
                        name = "Achievement Button",
                        parentLayout = "buttons",
                        selectionPosition = new Vector2Int(0, num + 1),
                        rect = RectValues.Default.SizeDelta(800f, 120f),
                        text = $"<size=30><b>{achievement.id} - <size=24><font=Arrhythmia>{achievement.name}</font></b>\n{achievement.description}",
                        //alignment = TMPro.TextAlignmentOptions.TopLeft,
                        textRect = RectValues.FullAnchored.AnchoredPosition(120f, 30f),
                        icon = achievement.icon ?? LegacyPlugin.AtanPlaceholder,
                        iconRect = RectValues.Default.AnchoredPosition(-640f, 0f).SizeDelta(110f, 110f),
                        enableWordWrapping = true,
                        func = () =>
                        {
                            if (achievement.unlocked)
                                SoundManager.inst.PlaySound(DefaultSounds.loadsound);
                            else
                                SoundManager.inst.PlaySound(DefaultSounds.Block);
                        },

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
                    };
                }
                elements.Add(menuButton);

                elements.Add(new MenuImage
                {
                    id = "0",
                    name = "Difficulty",
                    parent = menuButton.id,
                    rect = new RectValues(Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(8f, 0f)),
                    overrideColor = achievement.DifficultyType.Color,
                    useOverrideColor = true,
                    opacity = 1f,
                    roundedSide = SpriteHelper.RoundedSide.Left,
                    length = 0f,
                    wait = false,
                });

                num++;
            }

            if (regenerateUI)
                StartGeneration();
        }

        #endregion
    }
}
