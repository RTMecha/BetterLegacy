﻿using System;
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
using SteamworksFacepunch.Ugc;

namespace BetterLegacy.Arcade
{
    public class LevelListMenu : MenuBase
    {
        public static LevelListMenu Current { get; set; }

        public static bool ViewOnline { get; set; }

        public static int Page { get; set; }
        public static string Search { get; set; }

        public LevelListMenu() : base()
        {
            name = "Level List";

            regenerate = false;

            elements.Add(new MenuEvent
            {
                id = "09",
                name = "Effects",
                func = MenuEffectsManager.inst.SetDefaultEffects,
                length = 0f,
                regenerate = false,
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
                regenerate = false,
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
                length = 0.1f,
                regenerate = false,
                playBlipSound = true,
                func = Close,
            });

            var currentPage = Page;
            var currentSearch = Search;

            if (!ViewOnline)
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
                    valueChangedFunc = _val => SetLocalLevelsPage(Parser.TryParse(_val, Page)),
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
                        if (Page != 0 && pageField.inputField)
                            pageField.inputField.text = (Page - 1).ToString();
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
                    parentLayout = "local settings",
                    selectionPosition = new Vector2Int(1, 1),
                    rect = RectValues.Default.SizeDelta(132f, 64f),
                    func = () =>
                    {
                        if (Page != LocalLevelPageCount)
                            pageField.inputField.text = (Page + 1).ToString();
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

                RefreshLocalLevels(false);
            }
            else
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
                        if (Page != 0)
                            SetOnlineLevelsPage(Page - 1);
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
                    func = () => SetOnlineLevelsPage(Page + 1),
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
            }

            exitFunc = Close;
            allowEffects = false;
            layer = 10000;
            defaultSelection = new Vector2Int(0, 4);
            InterfaceManager.inst.SetCurrentInterface(this);
            InterfaceManager.inst.PlayMusic();
        }

        #region Local

        public static List<Level> Levels { get; set; }
        public static int LocalLevelPageCount => LocalLevels.Count / ArcadeMenu.MAX_LEVELS_PER_PAGE;
        public static List<Level> LocalLevels => Levels.FindAll(level => !level || string.IsNullOrEmpty(Search)
                        || level.id == Search
                        || level.metadata.song.tags.Contains(Search.ToLower())
                        || level.metadata.artist.Name.ToLower().Contains(Search.ToLower())
                        || level.metadata.creator.steam_name.ToLower().Contains(Search.ToLower())
                        || level.metadata.song.title.ToLower().Contains(Search.ToLower())
                        || level.metadata.song.getDifficulty().ToLower().Contains(Search.ToLower()));

        public void SearchLocalLevels(string search)
        {
            Search = search;
            Page = 0;

            var levelButtons = elements.FindAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name.Contains("Shine") || x.name.Contains("Lock"));

            for (int i = 0; i < levelButtons.Count; i++)
            {
                var levelButton = levelButtons[i];
                levelButton.Clear();
                CoreHelper.Destroy(levelButton.gameObject);
            }
            elements.RemoveAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name.Contains("Shine") || x.name.Contains("Lock"));
            RefreshLocalLevels(true);
        }

        public void SetLocalLevelsPage(int page)
        {
            Page = Mathf.Clamp(page, 0, LocalLevelPageCount);

            var levelButtons = elements.FindAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name.Contains("Shine") || x.name.Contains("Lock"));

            for (int i = 0; i < levelButtons.Count; i++)
            {
                var levelButton = levelButtons[i];
                levelButton.Clear();
                CoreHelper.Destroy(levelButton.gameObject);
            }
            elements.RemoveAll(x => x.name == "Level Button" || x.name == "Difficulty" || x.name.Contains("Shine") || x.name.Contains("Lock"));
            RefreshLocalLevels(true);
        }

        public void RefreshLocalLevels(bool regenerateUI)
        {
            var currentPage = Page + 1;
            int max = currentPage * ArcadeMenu.MAX_LEVELS_PER_PAGE;
            var currentSearch = Search;

            var levels = LocalLevels;
            for (int i = 0; i < levels.Count; i++)
            {
                int index = i;
                if (index < max - ArcadeMenu.MAX_LEVELS_PER_PAGE || index >= max)
                    continue;

                int column = (index % ArcadeMenu.MAX_LEVELS_PER_PAGE) % 5;
                int row = (int)((index % ArcadeMenu.MAX_LEVELS_PER_PAGE) / 5) + 2;

                var level = levels[index];

                if (level == null)
                {
                    if (LevelManager.CurrentLevelCollection && LevelManager.CurrentLevelCollection.levelInformation.TryFind(x => x.index == index, out LevelCollection.LevelInfo levelInfo))
                    {
                        CoreHelper.Log($"A collection level was not found. It was probably not installed.\n" +
                            $"Level Name: {levelInfo.name}\n" +
                            $"Song Title: {levelInfo.songTitle}\n" +
                            $"Creator: {levelInfo.creator}\n" +
                            $"Arcade ID: {levelInfo.arcadeID}\n" +
                            $"Server ID: {levelInfo.serverID}\n" +
                            $"Workshop ID: {levelInfo.workshopID}");

                        elements.Add(new MenuButton
                        {
                            id = levelInfo.id,
                            name = "Level Button",
                            parentLayout = "levels",
                            selectionPosition = new Vector2Int(column, row),
                            icon = level.icon,
                            iconRect = RectValues.Default.AnchoredPosition(-90, 30f),
                            text = "<size=24><#FF000045>" + levelInfo.name,
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
                            playBlipSound = false,
                            func = () =>
                            {
                                SoundManager.inst.PlaySound(DefaultSounds.Block);
                                CoreHelper.Log($"A collection level was not found. It was probably not installed.\n" +
                                    $"Level Name: {levelInfo.name}\n" +
                                    $"Song Title: {levelInfo.songTitle}\n" +
                                    $"Creator: {levelInfo.creator}\n" +
                                    $"Arcade ID: {levelInfo.arcadeID}\n" +
                                    $"Server ID: {levelInfo.serverID}\n" +
                                    $"Workshop ID: {levelInfo.workshopID}");
                            }
                        });
                    }
                    else
                        CoreHelper.Log($"Level was not found.");

                    continue;
                }

                var isSSRank = LevelManager.GetLevelRank(level).name == "SS";

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
                    mask = true,
                    playBlipSound = false,

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
                        if (AnimationManager.inst.TryFindAnimations(x => x.name.Contains("Level Shine"), out List<RTAnimation> animations))
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
                    LevelManager.currentLevelIndex = index;
                    CoreHelper.StartCoroutine(SelectLocalLevel(level));
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

        public IEnumerator SelectLocalLevel(Level level)
        {
            if (!level.music)
                yield return CoreHelper.StartCoroutine(level.LoadAudioClipRoutine(() => OpenPlayLevelMenu(level)));
            else
                OpenPlayLevelMenu(level);
        }

        void OpenPlayLevelMenu(Level level)
        {
            AudioManager.inst.StopMusic();
            var levels = Levels;
            PlayLevelMenu.close = () =>
            {
                Init(levels);
                levels = null;
            };
            PlayLevelMenu.Init(level);
            AudioManager.inst.PlayMusic(level.metadata.song.title, level.music);
            AudioManager.inst.SetPitch(CoreHelper.Pitch);
        }

        #endregion

        #region Online

        public static string SearchURL { get; set; } = $"{AlephNetworkManager.ARCADE_SERVER_URL}api/level/search";

        public static int OnlineLevelCount { get; set; }

        public static Dictionary<string, Sprite> OnlineLevelIcons { get; set; } = new Dictionary<string, Sprite>();

        public void SetOnlineLevelsPage(int page)
        {
            Page = page;
        }

        public void SearchOnlineLevels(string search)
        {
            Search = search;
            Page = 0;
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

            var page = Page;
            int currentPage = page + 1;

            var search = Search;

            string query =
                string.IsNullOrEmpty(search) && page == 0 ? SearchURL :
                    string.IsNullOrEmpty(search) && page != 0 ? $"{SearchURL}?page={page}" :
                        !string.IsNullOrEmpty(search) && page == 0 ? $"{SearchURL}?query={AlephNetworkManager.ReplaceSpace(search)}" :
                            !string.IsNullOrEmpty(search) ? $"{SearchURL}?query={AlephNetworkManager.ReplaceSpace(search)}&page={page}" : "";

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
                            int column = (num % ArcadeMenu.MAX_LEVELS_PER_PAGE) % 5;
                            int row = (int)((num % ArcadeMenu.MAX_LEVELS_PER_PAGE) / 5) + 2;

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
                                CoreHelper.StartCoroutine(AlephNetworkManager.DownloadBytes($"{ArcadeMenu.CoverURL}{id}.jpg", bytes =>
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

        public override void UpdateTheme()
        {
            if (Parser.TryParse(MenuConfig.Instance.InterfaceThemeID.Value, -1) >= 0 && InterfaceManager.inst.themes.TryFind(x => x.id == MenuConfig.Instance.InterfaceThemeID.Value, out BeatmapTheme interfaceTheme))
                Theme = interfaceTheme;
            else
                Theme = InterfaceManager.inst.themes[0];

            base.UpdateTheme();
        }

        public static void Init(List<Level> levels)
        {
            InterfaceManager.inst.CloseMenus();
            ViewOnline = false;
            Levels = levels;
            Current = new LevelListMenu();
        }

        public static void Init(string url)
        {
            InterfaceManager.inst.CloseMenus();
            ViewOnline = true;
            SearchURL = url;
            Current = new LevelListMenu();
        }

        public static void Close()
        {
            Levels = null;
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
            Levels = null;
            base.Clear();
        }
    }
}
