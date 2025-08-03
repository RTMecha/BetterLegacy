using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.EventSystems;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Menus;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Interfaces;
using BetterLegacy.Menus.UI.Layouts;

namespace BetterLegacy.Arcade.Interfaces
{
    public class AchievementListMenu : MenuBase
    {
        public static AchievementListMenu Current { get; set; }

        public int CurrentPage { get; set; }

        public Level CurrentLevel { get; set; }

        public List<Achievement> Achievements { get; set; }

        public AchievementListMenu(Level level, int page, Action onReturn)
        {
            CurrentLevel = level;
            CurrentPage = page;
            Achievements = CurrentLevel.GetAchievements();

            id = "63562464";

            musicName = InterfaceManager.RANDOM_MUSIC_NAME;
            name = "Achievements";

            elements.Add(new MenuEvent
            {
                id = "09",
                name = "Effects",
                func = MenuEffectsManager.inst.SetDefaultEffects,
                length = 0f,
                regenerate = false,
            });

            layouts.Add("achievements", new MenuVerticalLayout
            {
                name = "achievements",
                spacing = 4f,
                childControlWidth = true,
                childForceExpandWidth = true,
                rect = RectValues.Default.SizeDelta(1400f, 600f),
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
                id = "1",
                name = "Arcade Button",
                text = "<b><align=center>[ RETURN ]",
                parentLayout = "settings",
                autoAlignSelectionPosition = true,
                color = 6,
                opacity = 0.1f,
                textColor = 6,
                selectedColor = 6,
                selectedTextColor = 7,
                selectedOpacity = 1f,
                length = 0.3f,
                playBlipSound = true,
                rect = RectValues.Default.SizeDelta(400f, 64f),
                func = onReturn,
                regenerate = false,
            });

            elements.Add(new MenuImage
            {
                id = "1",
                name = "Spacer",
                parentLayout = "settings",
                length = 0.3f,
                rect = RectValues.Default.SizeDelta(900f, 64f),
                regenerate = false,
                opacity = 0f,
            });

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

                    CurrentPage = Mathf.Clamp(num, 0, AchievementPageCount);
                    RefreshAchievements(true);
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
                            result = Mathf.Clamp(result, 0, AchievementPageCount);

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
                selectionPosition = new Vector2Int(1, 0),
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
                selectionPosition = new Vector2Int(2, 0),
                rect = RectValues.Default.SizeDelta(132f, 64f),
                func = () =>
                {
                    if (CurrentPage != AchievementPageCount)
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

            RefreshAchievements(false, false);

            exitFunc = onReturn;

            layer = 10000;

            InterfaceManager.inst.SetCurrentInterface(this);
        }

        public static void Init(Level level, int page, Action onReturn)
        {
            Current?.Clear();
            Current = new AchievementListMenu(level, page, onReturn);
        }

        public const int MAX_ACHIEVEMENTS_PER_PAGE = 5;
        public int AchievementPageCount => Achievements.Count / MAX_ACHIEVEMENTS_PER_PAGE;

        public void ClearAchievements() => ClearElements(x => x.name == "Achievement Button" || x.name == "Difficulty");

        public void RefreshAchievements(bool regenerateUI, bool clear = true)
        {
            if (clear)
                ClearAchievements();

            var currentPage = CurrentPage + 1;
            int max = currentPage * MAX_ACHIEVEMENTS_PER_PAGE;

            var achievements = Achievements;
            int num = 0;
            for (int i = 0; i < achievements.Count; i++)
            {
                var achievement = achievements[i];

                int index = num;
                if (index < max - MAX_ACHIEVEMENTS_PER_PAGE || index >= max)
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
                        parentLayout = "achievements",
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
                        parentLayout = "achievements",
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
    }
}
