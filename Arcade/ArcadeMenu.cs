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
using System.Collections;
using BetterLegacy.Core.Managers.Networking;
using SimpleJSON;

namespace BetterLegacy.Arcade
{
    // Probably not gonna use this
    public class ArcadeMenu : MenuBase
    {
        public static bool useThisUI = false;

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
                    text = $"<align=center><b>[ {(Tab)i} ]",
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

            var currentPage = Pages[(int)CurrentTab] + 1;
            int max = currentPage * MAX_LEVELS_PER_PAGE;
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
                            //rect = RectValues.HorizontalAnchored.AnchoredPosition(0f, 350f).SizeDelta(-126f, 64f),
                            rect = RectValues.Default.SizeDelta(1500, 64f),
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
                            id = "32848924",
                            name = "Prev Page",
                            text = "<align=center><b><",
                            parentLayout = "local settings",
                            selectionPosition = new Vector2Int(0, 1),
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            func = () =>
                            {
                                if (Pages[(int)CurrentTab] != 0)
                                    SetLocalLevelsPage(Pages[(int)CurrentTab] - 1);
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
                            parentLayout = "local settings",
                            selectionPosition = new Vector2Int(1, 1),
                            rect = RectValues.Default.SizeDelta(132f, 64f),
                            func = () =>
                            {
                                if (Pages[(int)CurrentTab] != LocalLevelPageCount)
                                    SetLocalLevelsPage(Pages[(int)CurrentTab] + 1);
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
                        // todo: make browser download zip levels and also browse local files for a level.
                        break;
                    }
                case Tab.Queue:
                    {

                        break;
                    }
                case Tab.Steam:
                    {

                        break;
                    }
            }

            exitFunc = () => SceneManager.inst.LoadScene("Input Select");

            CoreHelper.StartCoroutine(GenerateUI());
        }

        #region Local

        public static int LocalLevelPageCount => LocalLevels.Count / MAX_LEVELS_PER_PAGE;
        public static string LocalSearch => Searches[0];
        public static List<Level> LocalLevels => LevelManager.Levels.Where(level => !level.fromCollection && (string.IsNullOrEmpty(LocalSearch)
                        || level.id == LocalSearch
                        || level.metadata.LevelSong.tags.Contains(LocalSearch)
                        || level.metadata.artist.Name.ToLower().Contains(LocalSearch.ToLower())
                        || level.metadata.creator.steam_name.ToLower().Contains(LocalSearch.ToLower())
                        || level.metadata.song.title.ToLower().Contains(LocalSearch.ToLower())
                        || level.metadata.song.getDifficulty().ToLower().Contains(LocalSearch.ToLower()))).ToList();

        public void SearchLocalLevels(string search)
        {
            Searches[0] = search;
            Pages[0] = 0;

            var levelButtons = elements.Where(x => x.name == "Level Button").ToList();

            for (int i = 0; i < levelButtons.Count; i++)
            {
                var levelButton = levelButtons[i];
                levelButton.Clear();
                CoreHelper.Destroy(levelButton.gameObject);
            }
            elements.RemoveAll(x => x.name == "Level Button");
            RefreshLocalLevels(true);
        }

        public void SetLocalLevelsPage(int page)
        {
            Pages[0] = Mathf.Clamp(page, 0, LocalLevelPageCount);

            var levelButtons = elements.Where(x => x.name == "Level Button").ToList();

            for (int i = 0; i < levelButtons.Count; i++)
            {
                var levelButton = levelButtons[i];
                levelButton.Clear();
                CoreHelper.Destroy(levelButton.gameObject);
            }
            elements.RemoveAll(x => x.name == "Level Button");
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
                    length = 0.01f,

                    allowOriginalHoverMethods = true,
                    enterFunc = () =>
                    {
                        CoreHelper.Log($"Start shine");
                    },
                    exitFunc = () =>
                    {
                        CoreHelper.Log($"End shine");
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
                    roundedSide = SpriteManager.RoundedSide.Left,
                    length = 0f,
                    wait = false,
                });
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

            var levelButtons = elements.Where(x => x.name == "Level Button").ToList();

            for (int i = 0; i < levelButtons.Count; i++)
            {
                var levelButton = levelButtons[i];
                levelButton.Clear();
                CoreHelper.Destroy(levelButton.gameObject);
            }
            elements.RemoveAll(x => x.name == "Level Button");

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
            {
                yield break;
            }

            loadingOnlineLevels = true;
            var headers = new Dictionary<string, string>();
            if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null)
                headers["Authorization"] = $"Bearer {LegacyPlugin.authData["access_token"].Value}";

            Dictionary<string, MenuImage> ids = new Dictionary<string, MenuImage>();

            yield return CoreHelper.StartCoroutine(AlephNetworkManager.DownloadJSONFile(query, json =>
            {
                try
                {
                    var jn = JSON.Parse(json);

                    if (jn["items"] != null)
                    {
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
                            int column = (index % MAX_LEVELS_PER_PAGE) % 5;
                            int row = (int)((index % MAX_LEVELS_PER_PAGE) / 5) + 2;

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
                                roundedSide = SpriteManager.RoundedSide.Left,
                                length = 0f,
                                wait = false,
                            });

                            ids.Add(id, button);

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
            }, headers));

            foreach (var keyValuePair in ids)
            {
                var id = keyValuePair.Key;
                var button = keyValuePair.Value;
                if (!OnlineLevelIcons.ContainsKey(id))
                {
                    yield return CoreHelper.StartCoroutine(AlephNetworkManager.DownloadBytes($"{CoverURL}{id}.jpg", bytes =>
                    {
                        var sprite = SpriteManager.LoadSprite(bytes);
                        OnlineLevelIcons.Add(id, sprite);
                        button.icon = sprite;
                    }, onError =>
                    {
                        var sprite = SteamWorkshop.inst.defaultSteamImageSprite;
                        OnlineLevelIcons.Add(id, sprite);
                        button.icon = sprite;
                    }));
                }
                else
                {
                    button.icon = OnlineLevelIcons[id];
                }
            }

            loadingOnlineLevels = false;
            CoreHelper.StartCoroutine(GenerateUI());
        }

        public bool loadingOnlineLevels;

        public void SelectOnlineLevel(JSONObject onlineLevel)
        {
            if (LevelManager.Levels.TryFind(x => x.metadata != null && x.metadata.serverID == onlineLevel["id"], out Level level))
            {
                CoreHelper.StartCoroutine(SelectLocalLevel(level));
                return;
            }

            DownloadLevelMenu.Init(onlineLevel);
        }

        #endregion

        #region Browser

        #endregion

        #region Queue

        public void RefreshQueueLevels(bool regenerateUI)
        {
            if (regenerateUI)
                CoreHelper.StartCoroutine(GenerateUI());
        }

        #endregion

        #region Steam

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
