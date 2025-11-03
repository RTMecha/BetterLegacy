using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Arcade.Interfaces;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Menus.UI.Interfaces;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Layouts;
using BetterLegacy.Story;

namespace BetterLegacy.Menus
{
    /// <summary>
    /// Manages all things related to PA's interfaces.
    /// </summary>
    public class InterfaceManager : BaseManager<InterfaceManager, ManagerSettings>
    {
        #region Init

        /// <summary>
        /// The main directory to load interfaces from. Must end with a slash.
        /// </summary>
        public string MainDirectory { get; set; }

        public override void OnInit()
        {
            CurrentAudioSource = gameObject.AddComponent<AudioSource>();
            CurrentAudioSource.loop = true;
            MainDirectory = RTFile.ApplicationDirectory + "beatmaps/interfaces/";
            RTFile.CreateDirectory(MainDirectory);
        }

        public override void OnTick()
        {
            if (CurrentAudioSource.isPlaying)
                CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);
            else
                AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

            CurrentAudioSource.volume = SoundManager.inst.MusicVolume * AudioManager.inst.masterVol;

            if (!CurrentInterface)
                return;

            CurrentInterface.OnTick();
            CurrentInterface.UpdateControls();
            CurrentInterface.UpdateTheme();
        }

        #endregion

        #region Constants

        /// <summary>
        /// The normal menu music group.
        /// </summary>
        public const string RANDOM_MUSIC_NAME = "menu";
        /// <summary>
        /// ID of the main menu.
        /// </summary>
        public const string MAIN_MENU_ID = "0";
        /// <summary>
        /// ID of the story saves menu.
        /// </summary>
        public const string STORY_SAVES_MENU_ID = "1";
        /// <summary>
        /// ID of the chapter select menu.
        /// </summary>
        public const string CHAPTER_SELECT_MENU_ID = "2";
        /// <summary>
        /// ID of the profile menu.
        /// </summary>
        public const string PROFILE_MENU_ID = "3";
        /// <summary>
        /// ID of the extras menu.
        /// </summary>
        public const string EXTRAS_MENU_ID = "4";

        #endregion

        #region Music

        /// <summary>
        /// The current audio source the interfaces in-game use.
        /// </summary>
        public AudioSource CurrentAudioSource { get; set; }

        /// <summary>
        /// Samples of the current audio.
        /// </summary>
        public float[] samples = new float[256];

        /// <summary>
        /// Sets and plays the current music. If the player is in the game scene, play from a custom audio source, otherwise play from the main audio source.
        /// </summary>
        /// <param name="music">Music to play.</param>
        /// <param name="allowSame">If same audio should be forced to play. With this off, it will not play the song if the current song equals the passed one.</param>
        /// <param name="fadeDuration">The duration to fade from previous to current song.</param>
        /// <param name="loop">If the song should loop.</param>
        public void PlayMusic(AudioClip music, bool allowSame = false, float fadeDuration = 1f, bool loop = true)
        {
            if (CoreHelper.InEditor)
                return;

            if (!CoreHelper.InGame)
            {
                try
                {
                    if (!string.IsNullOrEmpty(music.name) && AudioManager.inst.CurrentAudioSource.clip && !string.IsNullOrEmpty(AudioManager.inst.CurrentAudioSource.clip.name) && music.name != AudioManager.inst.CurrentAudioSource.clip.name)
                        CoreHelper.Notify($"Now playing: {music.name}", CurrentTheme.guiColor);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogError($"Could not notify music name due to the exception: {ex}");
                }
                AudioManager.inst.PlayMusic(music.name, music, allowSame, fadeDuration, loop);
                return;
            }

            if (!CurrentAudioSource.clip || allowSame || music.name != CurrentAudioSource.clip.name)
                CurrentAudioSource.clip = music;

            if (!CurrentAudioSource.clip)
                return;

            CoreHelper.Log("Playing music");
            CurrentAudioSource.UnPause();
            CurrentAudioSource.time = 0f;
            CurrentAudioSource.loop = loop;
            CurrentAudioSource.Play();

            try
            {
                if (!string.IsNullOrEmpty(music.name) && !string.IsNullOrEmpty(CurrentAudioSource.clip.name) && music.name != CurrentAudioSource.clip.name)
                    CoreHelper.Notify($"Now playing: {music.name}", CurrentTheme.guiColor);
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Could not notify music name due to the exception: {ex}");
            }
        }

        /// <summary>
        /// Stops the current music.
        /// </summary>
        public void StopMusic()
        {
            if (!CoreHelper.InGame)
                AudioManager.inst.StopMusic();

            if (!CurrentAudioSource.clip)
                return;

            CurrentAudioSource.Stop();
            CurrentAudioSource.clip = null;
        }

        /// <summary>
        /// The chosen song to play. -1 is considered a reset value. If it is this value, a different song will be chosen and will not change unless this number is -1.
        /// </summary>
        public int randomIndex = -1;

        /// <summary>
        /// Plays a random song.
        /// </summary>
        public void PlayMusic()
        {
            if (CoreHelper.InEditor)
                return;

            if (!MenuConfig.Instance.PlayCustomMusic.Value)
            {
                CoreHelper.LogWarning("PlayCustomMusic setting is off, so play default music.");
                CurrentInterface?.PlayDefaultMusic();
                return;
            }

            if (CurrentInterface && !CurrentInterface.allowCustomMusic)
            {
                CoreHelper.LogWarning("CurrentMenu does not allow custom music.");
                CurrentInterface?.PlayDefaultMusic();
                return;
            }

            var directory = MenuConfig.Instance.MusicLoadMode.Value.GetDirectory();

            if (!RTFile.DirectoryExists(directory))
            {
                CoreHelper.LogWarning("Directory does not exist, so play default music.");
                CurrentInterface?.PlayDefaultMusic();
                return;
            }

            var isLevelFolder = CoreHelper.Equals(MenuConfig.Instance.MusicLoadMode.Value, MenuMusicLoadMode.StoryFolder, MenuMusicLoadMode.EditorFolder, MenuMusicLoadMode.ArcadeFolder);

            var oggFiles = Directory.GetFiles(directory, isLevelFolder ? Level.LEVEL_OGG : FileFormat.OGG.ToPattern(), SearchOption.AllDirectories);
            var wavFiles = Directory.GetFiles(directory, isLevelFolder ? Level.LEVEL_WAV : FileFormat.WAV.ToPattern(), SearchOption.AllDirectories);
            var mp3Files = Directory.GetFiles(directory, isLevelFolder ? Level.LEVEL_MP3 : FileFormat.MP3.ToPattern(), SearchOption.AllDirectories);

            var songFiles = oggFiles.Union(wavFiles).Union(mp3Files).ToArray();

            if (songFiles.Length < 1)
            {
                CoreHelper.LogWarning("No song files, so play default music.");
                CurrentInterface?.PlayDefaultMusic();
                return;
            }

            if (MenuConfig.Instance.MusicIndex.Value >= 0 && MenuConfig.Instance.MusicIndex.Value < songFiles.Length)
                randomIndex = MenuConfig.Instance.MusicIndex.Value;

            if (randomIndex < 0 || randomIndex >= songFiles.Length)
                randomIndex = UnityEngine.Random.Range(0, songFiles.Length);

            var songFileCurrent = songFiles[Mathf.Clamp(randomIndex, 0, songFiles.Length - 1)];

            if (string.IsNullOrEmpty(songFileCurrent))
            {
                CoreHelper.LogWarning("Path is empty for some reason, so play default music.");
                CurrentInterface?.PlayDefaultMusic();
                return;
            }

            var name = Path.GetFileName(songFileCurrent);
            var audioType = RTFile.GetAudioType(songFileCurrent);

            if (CoreHelper.InGame ? (CurrentAudioSource.clip && name == CurrentAudioSource.clip.name) : (AudioManager.inst.CurrentAudioSource.clip && name == AudioManager.inst.CurrentAudioSource.clip.name))
            {
                CoreHelper.LogWarning($"Audio \"{name}\" is the same as the current.");
                return;
            }

            if (audioType == AudioType.MPEG)
            {
                SetMusic(LSAudio.CreateAudioClipUsingMP3File(songFileCurrent), name);
                return;
            }

            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip($"file://{songFileCurrent}", audioType, audioClip =>
            {
                if (CoreHelper.InEditor)
                    return;

                SetMusic(audioClip, name);
            }));
        }

        void SetMusic(AudioClip audioClip, string name)
        {
            CoreHelper.Log($"Attempting to play music: {name}");
            audioClip.name = name;
            if (CurrentInterface)
                CurrentInterface.music = audioClip;
            PlayMusic(audioClip);
        }

        #endregion

        #region Interfaces

        /// <summary>
        /// The currently open interface.
        /// </summary>
        public MenuBase CurrentInterface { get; set; }

        /// <summary>
        /// The current interface generation sequence.
        /// </summary>
        public Coroutine CurrentGenerateUICoroutine { get; set; }

        /// <summary>
        /// The currently open interface list.
        /// </summary>
        public CustomMenuList CurrentInterfaceList { get; set; }

        /// <summary>
        /// All loaded interfaces.
        /// </summary>
        public List<MenuBase> interfaces = new List<MenuBase>();

        /// <summary>
        /// If the interfaces should speed up.
        /// </summary>
        public static bool SpeedUp => InputDataManager.inst.menuActions.Submit.IsPressed || Input.GetMouseButton(0);

        /// <summary>
        /// The current speed the interface should generate at.
        /// </summary>
        public static float InterfaceSpeed => inst.CurrentInterface && inst.CurrentInterface.forceInterfaceSpeed ? 1f : SpeedUp ? MenuConfig.Instance.SpeedUpSpeedMultiplier.Value : MenuConfig.Instance.RegularSpeedMultiplier.Value;

        /// <summary>
        /// Closes all interfaces and opens an interface.
        /// </summary>
        /// <param name="menu">Interface to open.</param>
        public void SetCurrentInterface(MenuBase menu)
        {
            CloseMenus();
            CurrentInterface = menu;
            menu.StartGeneration();
        }

        /// <summary>
        /// Closes all interfaces and opens an interface.
        /// </summary>
        /// <param name="id">Interface ID to find. If no interface is found, do nothing.</param>
        public void SetCurrentInterface(string id)
        {
            if (interfaces.TryFind(x => x.id == id, out MenuBase menu))
                SetCurrentInterface(menu);
        }

        /// <summary>
        /// Closes and clears all interfaces.
        /// </summary>
        public void CloseMenus()
        {
            CurrentInterface?.Clear();
            CurrentInterface = null;
            PauseMenu.Current = null;
            EndLevelMenu.Current = null;
            ArcadeMenu.Current = null;
            PlayLevelMenu.Current = null;
            AchievementListMenu.Current = null;
            DownloadLevelMenu.Current = null;
            SteamLevelMenu.Current = null;
            ProgressMenu.Current = null;
            LevelCollectionMenu.Current = null;
            LevelListMenu.Current = null;
            InputSelectMenu.Current = null;
            LoadLevelsMenu.Current = null;
            ControllerDisconnectedMenu.Current = null;
            ProfileMenu.Current = null;

            StopGenerating();

            CurrentInterfaceList?.CloseMenus();
        }

        /// <summary>
        /// Clears interface data and stops interface generation.
        /// </summary>
        /// <param name="clearThemes">If interface themes should be cleared.</param>
        /// <param name="stopGenerating">If the current interface should stop generating.</param>
        public void Clear(bool clearThemes = true, bool stopGenerating = true)
        {
            CurrentInterface?.Clear();
            CurrentInterface = null;

            CurrentInterfaceList?.Clear(clearThemes, stopGenerating);
            CurrentInterfaceList = null;

            for (int i = 0; i < interfaces.Count; i++)
            {
                try
                {
                    interfaces[i].Clear();
                }
                catch
                {

                }
            }
            interfaces.Clear();

            if (clearThemes)
                themes.Clear();

            if (stopGenerating)
                StopGenerating();
        }

        /// <summary>
        /// Stops the current interface from generating.
        /// </summary>
        public void StopGenerating()
        {
            if (CurrentGenerateUICoroutine == null)
                return;

            StopCoroutine(CurrentGenerateUICoroutine);
            CurrentGenerateUICoroutine = null;
        }

        /// <summary>
        /// Starts the story mode interface.
        /// </summary>
        /// <param name="chapterIndex">Chapter to load.</param>
        /// <param name="levelIndex">Level to use.</param>
        public void StartupStoryInterface(int chapterIndex, int levelIndex)
        {
            Clear(false, false);
            CoreHelper.InStory = true;

            if (Companion.Entity.Example.Current && Companion.Entity.Example.Current.model)
                Companion.Entity.Example.Current.model.SetActive(true); // if Example was disabled

            var storyStarted = StoryManager.inst.CurrentSave.LoadBool("StoryModeStarted", false);
            var chapter = StoryMode.Instance.chapters[chapterIndex];

            if (onReturnToStoryInterface != null)
            {
                onReturnToStoryInterface();
                onReturnToStoryInterface = null;
                return;
            }

            var path = storyStarted ? chapter.interfacePath : StoryMode.Instance.entryInterfacePath;

            ParseInterface(path);
        }

        /// <summary>
        /// Parses an interface from a path, adds it to the interfaces list and opens it.
        /// </summary>
        /// <param name="path">Path to an interface.</param>
        public void ParseInterface(string path, bool load = true, string openInterfaceID = null, List<string> branchChain = null, Dictionary<string, JSONNode> customVariables = null)
        {
            var jn = JSON.Parse(RTFile.ReadFromFile(path));

            if (!string.IsNullOrEmpty(jn["type"]) && jn["type"].Value.ToLower() == "list")
            {
                if (!load)
                    return;

                CurrentInterfaceList = CustomMenuList.Parse(jn, openInterfaceID: openInterfaceID, branchChain: branchChain, customVariables: customVariables);
                return;
            }

            MenuBase menu = CustomMenu.Parse(jn, customVariables);
            menu.filePath = path;
            if (interfaces.TryFind(x => x.id == menu.id, out MenuBase otherMenu))
                menu = otherMenu;
            else
                interfaces.Add(menu);

            if (!load)
                return;

            SetCurrentInterface(menu);
            PlayMusic();
        }

        /// <summary>
        /// Function to run when returning from a level to the story interface.
        /// </summary>
        public Action onReturnToStoryInterface;

        /// <summary>
        /// Starts the story mode interface.
        /// </summary>
        public void StartupStoryInterface() => StartupStoryInterface(StoryManager.inst.currentPlayingChapterIndex, StoryManager.inst.currentPlayingLevelSequenceIndex);

        /// <summary>
        /// Starts the main menu interface.
        /// </summary>
        public void StartupInterface()
        {
            Clear(false, false);
            CoreHelper.InStory = false;

            Companion.Entity.Example.Current?.model?.SetActive(true); // if Example was disabled

            ParseInterface(AssetPack.GetFile($"core/interfaces/main_menu{FileFormat.LSI.Dot()}"));

            interfaces.Add(new StoryMenu());

            if (!MenuConfig.Instance.ShowChangelog.Value || ChangeLogMenu.Seen || (!SceneHelper.LoadedGame && ModCompatibility.EditorOnStartupInstalled))
            {
                CoreHelper.Log($"Going to main menu.\n" +
                    $"ShowChangelog: {MenuConfig.Instance.ShowChangelog.Value}\n" +
                    $"Seen: {ChangeLogMenu.Seen}\n" +
                    $"Editor On Startup: {ModCompatibility.EditorOnStartupInstalled}");

                SetCurrentInterface(MAIN_MENU_ID);
                PlayMusic();
                AudioManager.inst.SetPitch(1f);

                SceneHelper.LoadedGame = true;
                return;
            }

            OpenChangelog();
            SceneHelper.LoadedGame = true;
        }

        void OpenChangelog()
        {
            try
            {
                CoreHelper.Log($"Loading changelog...\nIs loading scene: {SceneHelper.Loading}");
                if (RTFile.TryReadFromFile(RTFile.GetAsset($"changelog{FileFormat.TXT.Dot()}"), out string file))
                    new ChangeLogMenu(RTString.GetLines(file));
                else
                {
                    CoreHelper.LogError($"Couldn't read changelog file, continuing...");
                    SetCurrentInterface(MAIN_MENU_ID);
                    PlayMusic();
                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Error: {ex}");
            }
        }

        #endregion

        #region Themes

        /// <summary>
        /// The theme that should be used by the current interface.
        /// </summary>
        public BeatmapTheme CurrentTheme =>
            Parser.TryParse(MenuConfig.Instance.InterfaceThemeID.Value, -1) >= 0 && themes.TryFind(x => x.id == MenuConfig.Instance.InterfaceThemeID.Value, out BeatmapTheme interfaceTheme) ?
                interfaceTheme : themes != null && themes.Count > 0 ? themes[0] : ThemeManager.inst.DefaultThemes[1];

        /// <summary>
        /// Themes that should be used in the interface.
        /// </summary>
        public List<BeatmapTheme> themes = new List<BeatmapTheme>();

        /// <summary>
        /// Reloads all themes.
        /// </summary>
        public void LoadThemes()
        {
            themes.Clear();
            var jn = JSON.Parse(RTFile.ReadFromFile(RTFile.GetAsset($"builtin/default_interface_themes{FileFormat.LST.Dot()}")));
            for (int i = 0; i < jn["themes"].Count; i++)
                themes.Add(BeatmapTheme.Parse(jn["themes"][i]));

            var path = $"{RTFile.ApplicationDirectory}beatmaps/interfaces/themes";
            if (!RTFile.DirectoryExists(path))
                return;

            var files = Directory.GetFiles(path, FileFormat.LST.ToPattern());
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    jn = JSON.Parse(RTFile.ReadFromFile(files[i]));
                    themes.Add(BeatmapTheme.Parse(jn));
                }
                catch (Exception ex)
                {
                    CoreHelper.LogError($"Failed to load theme: {Path.GetFileName(files[i])}\nException: {ex}");
                }
            }
        }

        #endregion

        #region Interface Functions

        public MenuImageFunctions elementFunctions = new MenuImageFunctions();

        /// <summary>
        /// Parses text from spawn.
        /// </summary>
        /// <param name="input">Input string.</param>
        /// <returns>Returns parsed text.</returns>
        public string ParseSpawnText(string input)
        {
            RTString.RegexMatches(input, new Regex(@"{{Date=(.*?)}}"), match =>
            {
                input = input.Replace(match.Groups[0].ToString(), DateTime.Now.ToString(match.Groups[1].ToString()));
            });
            return elementFunctions.ParseText(input);
        }

        /// <summary>
        /// Parses text per-tick.
        /// </summary>
        /// <param name="input">Input string.</param>
        /// <returns>Returns parsed text.</returns>
        public string ParseTickText(string input)
        {
            RTString.RegexMatches(input, new Regex(@"{{AudioTimeSeconds=(.*?)}}"), match =>
            {
                input = input.Replace(match.Groups[0].ToString(), RTString.PreciseToSeconds(AudioManager.inst.CurrentAudioSource.time, match.Groups[1].ToString()));
            });
            RTString.RegexMatches(input, new Regex(@"{{AudioTimeMinutes=(.*?)}}"), match =>
            {
                input = input.Replace(match.Groups[0].ToString(), RTString.PreciseToMinutes(AudioManager.inst.CurrentAudioSource.time, match.Groups[1].ToString()));
            });
            RTString.RegexMatches(input, new Regex(@"{{AudioTimeHours=(.*?)}}"), match =>
            {
                input = input.Replace(match.Groups[0].ToString(), RTString.PreciseToHours(AudioManager.inst.CurrentAudioSource.time, match.Groups[1].ToString()));
            });
            RTString.RegexMatches(input, new Regex(@"{{AudioTimeMilliSeconds=(.*?)}}"), match =>
            {
                input = input.Replace(match.Groups[0].ToString(), RTString.PreciseToMilliSeconds(AudioManager.inst.CurrentAudioSource.time, match.Groups[1].ToString()));
            });
            return input.Replace("{{AudioTime}}", AudioManager.inst.CurrentAudioSource.time.ToString());
        }

        /// <summary>
        /// Parses an "if_func" JSON and returns the result. Supports both JSON Object and JSON Array.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <param name="thisElement">Interface element reference.</param>
        /// <param name="customVariables">Passed custom variables.</param>
        /// <returns>Returns true if the passed JSON functions is true, otherwise false.</returns>
        public bool ParseIfFunction(JSONNode jn, MenuImage thisElement = null, Dictionary<string, JSONNode> customVariables = null)
        {
            return elementFunctions.ParseIfFunction(jn, thisElement, customVariables);
        }

        /// <summary>
        /// Parses an entire func JSON. Supports both JSON Object and JSON Array.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <param name="thisElement">Interface element reference.</param>
        /// <param name="customVariables">Passed custom variables.</param>
        public void ParseFunction(JSONNode jn, MenuImage thisElement = null, Dictionary<string, JSONNode> customVariables = null)
        {
            elementFunctions.ParseFunction(jn, thisElement, customVariables);
        }

        /// <summary>
        /// Parses a "func" JSON and returns a variable from it based on the name and parameters.
        /// </summary>
        /// <param name="jn">The func JSON. Can be a <see cref="Lang"/>, string, variable key or func.</param>
        /// <param name="thisElement">Interface element reference.</param>
        /// <param name="customVariables">Passed custom variables.</param>
        /// <returns>Returns the variable returned from the JSON function.</returns>
        public JSONNode ParseVarFunction(JSONNode jn, MenuImage thisElement = null, Dictionary<string, JSONNode> customVariables = null)
        {
            return elementFunctions.ParseVarFunction(jn, thisElement, customVariables);
        }

        #endregion

        public class MenuImageFunctions : JSONFunctionParser<MenuImage>
        {
            public override bool IfFunction(JSONNode jn, string name, JSONNode parameters, MenuImage thisElement = null, Dictionary<string, JSONNode> customVariables = null)
            {
                try
                {
                    switch (name)
                    {
                        #region Main

                        case "CurrentInterfaceGenerating": {
                                return inst.CurrentInterface && inst.CurrentInterface.generating;
                            }

                        #endregion

                        #region Interface List

                        case "LIST_ContainsInterface": {
                                if (!inst.CurrentInterfaceList || parameters == null)
                                    break;

                                var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables);
                                if (id == null)
                                    break;

                                return inst.CurrentInterfaceList.Contains(id);
                            }

                        #endregion

                        #region Layout

                        case "LayoutChildCountEquals": {
                                if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !inst.CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                    break;

                                var isArray = parameters.IsArray;

                                return menuLayout.content.childCount == (isArray ? parameters[1].AsInt : parameters["count"].AsInt);
                            }
                        case "LayoutChildCountLesserEquals": {
                                if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !inst.CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                    break;

                                var isArray = parameters.IsArray;

                                return menuLayout.content.childCount <= (isArray ? parameters[1].AsInt : parameters["count"].AsInt);
                            }
                        case "LayoutChildCountGreaterEquals": {
                                if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !inst.CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                    break;

                                var isArray = parameters.IsArray;

                                return menuLayout.content.childCount >= (isArray ? parameters[1].AsInt : parameters["count"].AsInt);
                            }
                        case "LayoutChildCountLesser": {
                                if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !inst.CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                    break;

                                var isArray = parameters.IsArray;

                                return menuLayout.content.childCount < (isArray ? parameters[1].AsInt : parameters["count"].AsInt);
                            }
                        case "LayoutChildCountGreater": {
                                if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !inst.CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                    break;

                                var isArray = parameters.IsArray;

                                return menuLayout.content.childCount > (isArray ? parameters[1].AsInt : parameters["count"].AsInt);
                            }

                        case "LayoutScrollXEquals": {
                                if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !inst.CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                    break;

                                var isArray = parameters.IsArray;

                                return menuLayout.content.anchoredPosition.x == (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                            }
                        case "LayoutScrollXLesserEquals": {
                                if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !inst.CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                    break;

                                var isArray = parameters.IsArray;

                                return menuLayout.content.anchoredPosition.x <= (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                            }
                        case "LayoutScrollXGreaterEquals": {
                                if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !inst.CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                    break;

                                var isArray = parameters.IsArray;

                                return menuLayout.content.anchoredPosition.x >= (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                            }
                        case "LayoutScrollXLesser": {
                                if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !inst.CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                    break;

                                var isArray = parameters.IsArray;

                                return menuLayout.content.anchoredPosition.x < (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                            }
                        case "LayoutScrollXGreater": {
                                if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !inst.CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                    break;

                                var isArray = parameters.IsArray;

                                return menuLayout.content.anchoredPosition.x > (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                            }

                        case "LayoutScrollYEquals": {
                                if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !inst.CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                    break;

                                var isArray = parameters.IsArray;

                                return menuLayout.content.anchoredPosition.y == (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                            }
                        case "LayoutScrollYLesserEquals": {
                                if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !inst.CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                    break;

                                var isArray = parameters.IsArray;

                                return menuLayout.content.anchoredPosition.y <= (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                            }
                        case "LayoutScrollYGreaterEquals": {
                                if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !inst.CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                    break;

                                var isArray = parameters.IsArray;

                                return menuLayout.content.anchoredPosition.y >= (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                            }
                        case "LayoutScrollYLesser": {
                                if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !inst.CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                    break;

                                var isArray = parameters.IsArray;

                                return menuLayout.content.anchoredPosition.y < (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                            }
                        case "LayoutScrollYGreater": {
                                if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !inst.CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                    break;

                                var isArray = parameters.IsArray;

                                return menuLayout.content.anchoredPosition.y > (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                            }

                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    CoreHelper.LogError($"Had an error with parsing {jn}!\nException: {ex}");
                }

                return base.IfFunction(jn, name, parameters, thisElement, customVariables);
            }

            public override void Function(JSONNode jn, string name, JSONNode parameters, MenuImage thisElement = null, Dictionary<string, JSONNode> customVariables = null)
            {
                switch (name)
                {
                    #region Interface

                    #region Close

                    // Closes the interface and returns to the game (if user is in the Game scene).
                    // Function has no parameters.
                    case "Close": {
                            string id = inst.CurrentInterface?.id;
                            inst.CloseMenus();
                            inst.StopMusic();

                            RTBeatmap.Current?.Resume();

                            if (CoreHelper.InGame)
                                inst.interfaces.RemoveAll(x => x.id == id);

                            return;
                        }

                    #endregion

                    #region SetCurrentInterface

                    // Finds an interface with a matching ID and opens it.
                    // Supports both JSON array and JSON object.
                    // 
                    // - JSON Array Structure -
                    // 0 = id
                    // Example:
                    // [
                    //   "0" < main menus' ID is 0, so load that one. No other interface should have this ID.
                    // ]
                    // 
                    // - JSON Object Structure -
                    // "id"
                    // Example:
                    // {
                    //   "id": "0"
                    // }
                    case "SetCurrentInterface": {
                            if (parameters == null)
                                return;

                            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables);
                            if (id == null || !inst.interfaces.TryFind(x => x.id == id, out MenuBase menu))
                                return;

                            inst.SetCurrentInterface(menu);
                            inst.PlayMusic();

                            return;
                        }

                    #endregion

                    #region Reload

                    // Reloads the interface and sets it to the main menu. Only recommended if you want to return to the main menu and unload every other interface.
                    // Function has no parameters.
                    case "Reload": {
                            if (CoreHelper.InGame) // don't allow reload in game
                                return;

                            LegacyPlugin.ParseProfile();
                            AssetPack.LoadAssetPacks();
                            LegacyPlugin.LoadSplashText();
                            ChangeLogMenu.Seen = false;
                            inst.randomIndex = -1;
                            inst.StartupInterface();

                            return;
                        }

                    #endregion

                    #region Parse

                    // Loads an interface and opens it, clearing the current interface.
                    // Supports both JSON array and JSON object.
                    // 
                    // - JSON Array Structure -
                    // 0 = file name without extension (files' extension must be lsi).
                    // 1 = if interface should be opened.
                    // 2 = set main directory.
                    // 3 = branch ID to load if the interface that's being loaded is a list type.
                    // 4 = branch chain ID list.
                    // Example:
                    // [
                    //   "story_mode",
                    //   "True",
                    //   "{{BepInExAssetsDirectory}}Interfaces",
                    //   "643284542742",
                    //   [
                    //     "23567353643", < first interface opened
                    //     "643284542742" < second interface opened
                    //   ]
                    // ]
                    //
                    // - JSON Object Structure -
                    // "file"
                    // "load"
                    // "path"
                    // "id"
                    // "chain"
                    // Example:
                    // {
                    //   "file": "some_interface",
                    //   "load": "False",
                    //   "path": "beatmaps/interfaces", < (optional)
                    //   "id": "5325263" < ID of the interface to open if the file is a list
                    //   "chain": [
                    //     "23567353643",
                    //     "643284542742" < when the user exits this interface, they return to the previous
                    //   ]
                    // }
                    case "Parse": {
                            if (parameters == null)
                                return;

                            var file = ParseVarFunction(parameters.Get(0, "file"), thisElement, customVariables);
                            if (file == null)
                                return;

                            var mainDirectory = ParseVarFunction(parameters.Get(2, "path"));
                            if (mainDirectory != null)
                                inst.MainDirectory = mainDirectory;

                            if (!inst.MainDirectory.Contains(RTFile.ApplicationDirectory))
                                inst.MainDirectory = RTFile.CombinePaths(RTFile.ApplicationDirectory, inst.MainDirectory);

                            string path = file.Value.Contains(RTFile.ApplicationDirectory) || mainDirectory == null ? file : RTFile.CombinePaths(inst.MainDirectory, file + FileFormat.LSI.Dot());

                            if (!path.EndsWith(FileFormat.LSI.Dot()))
                                path += FileFormat.LSI.Dot();

                            if (!RTFile.FileExists(path))
                            {
                                CoreHelper.LogError($"Interface {file} does not exist!");
                                return;
                            }

                            var branchChain = new List<string>();
                            var jnBranchChain = ParseVarFunction(parameters.Get(4, "chain"), thisElement, customVariables);
                            if (jnBranchChain != null && jnBranchChain.IsArray)
                            {
                                for (int i = 0; i < jnBranchChain.Count; i++)
                                {
                                    var branch = ParseVarFunction(jnBranchChain[i], thisElement, customVariables);
                                    if (branch != null && branch.IsString)
                                        branchChain.Add(ParseVarFunction(jnBranchChain[i], thisElement, customVariables).Value);
                                }
                            }

                            inst.ParseInterface(path, ParseVarFunction(parameters.Get(1, "load"), thisElement, customVariables), ParseVarFunction(parameters.Get(3, "id"), thisElement, customVariables), branchChain, customVariables);

                            return;
                        }

                    #endregion

                    #region ClearInterfaces

                    // Clears all interfaces from the interfaces list.
                    // Function has no parameters.
                    case "ClearInterfaces": {
                            inst.interfaces.Clear();
                            return;
                        }

                    #endregion

                    #region SetCurrentPath

                    // Sets the main directory for the menus to use in some cases.
                    // Supports both JSON array and JSON object.
                    //
                    // - JSON Array Structure -
                    // 0 = path
                    // Example:
                    // [
                    //   "beatmaps/interfaces/"
                    // ]
                    //
                    // - JSON Object Structure -
                    // "path"
                    // Example:
                    // {
                    //   "path": "{{BepInExAssetsDirectory}}Interfaces" < doesn't always need to end in a slash. A {{AppDirectory}} variable exists, but not recommended to use here since it's automatically applied to the start of the path.
                    // }
                    case "SetCurrentPath": {
                            if (parameters)
                                return;

                            var path = ParseVarFunction(parameters.Get(0, "path"), thisElement, customVariables);
                            if (path == null)
                                return;

                            inst.MainDirectory = path;

                            if (!inst.MainDirectory.Contains(RTFile.ApplicationDirectory))
                                inst.MainDirectory = RTFile.CombinePaths(RTFile.ApplicationDirectory, inst.MainDirectory);

                            return;
                        }

                    #endregion

                    #region Confirm

                    // Opens the Confirmation interface, which allows the player to choose whether to do something or not.
                    // Supports both JSON array and JSON object.
                    //
                    // - JSON Array Structure -
                    // 0 = message
                    // 1 = confirm function
                    // 2 = cancel function
                    // Example:
                    // [
                    //   "Are you sure you want to CONFIRM?",
                    //   {
                    //     "name": "Log",
                    //     "params": [ "YES!" ]
                    //   },
                    //   {
                    //     "name": "Log",
                    //     "params": [ "No..." ]
                    //   }
                    // ]
                    //
                    // - JSON Object Structure -
                    // "msg"
                    // "confirm_func"
                    // "cancel_func"
                    // Example:
                    // {
                    //   "msg": "Are you sure you want to CONFIRM?",
                    //   "confirm_func": {
                    //     "name": "Log",
                    //     "params": [ "Yes..." ]
                    //   },
                    //   "cancel_func": {
                    //     "name": "Log",
                    //     "params": [ "NO!" ]
                    //   }
                    // }
                    case "Confirm": {
                            if (parameters == null)
                                return;

                            var currentMessage = ParseVarFunction(parameters.Get(0, "msg"), thisElement, customVariables);
                            if (currentMessage == null)
                                return;

                            var confirmFunc = ParseVarFunction(parameters.Get(1, "confirm_func"), thisElement, customVariables);
                            if (confirmFunc == null)
                                return;

                            var cancelFunc = ParseVarFunction(parameters.Get(2, "cancel_func"), thisElement, customVariables);
                            if (cancelFunc == null)
                                return;

                            ConfirmMenu.Init(currentMessage, () => ParseFunction(confirmFunc, thisElement, customVariables), () => ParseFunction(cancelFunc, thisElement, customVariables));

                            return;
                        }

                    #endregion

                    #region SetTheme

                    // Sets the current interface theme.
                    // Supports both JSON array and JSON object.
                    //
                    // - JSON Array Structure -
                    // 0 = theme JSON.
                    // 1 = game theme override.
                    // Example:
                    // [
                    //   { ... }, < Beatmap Theme JSON.
                    //   true < use the current game theme
                    // ]
                    //
                    // - JSON Object Structure -
                    // "theme"
                    // Example:
                    // {
                    //   "theme": { ... } < Beatmap Theme JSON.
                    //   "game_theme": false < "theme" overrides the interface theme in-game
                    // }
                    case "SetTheme": {
                            if (parameters == null)
                                return;

                            if (inst.CurrentInterface is not CustomMenu customMenu)
                                return;

                            customMenu.useGameTheme = parameters.Get(1, "game_theme");

                            var theme = ParseVarFunction(parameters.Get(0, "theme"), thisElement, customVariables);
                            if (theme == null)
                                return;

                            customMenu.loadedTheme = BeatmapTheme.Parse(theme);

                            return;
                        }

                    #endregion

                    #endregion

                    #region Interface List

                    case "LIST_OpenDefaultInterface": {
                            inst.CurrentInterfaceList?.OpenDefaultInterface();
                            return;
                        }
                    case "LIST_ExitInterface": {
                            inst.CurrentInterfaceList?.ExitInterface();
                            return;
                        }
                    case "LIST_SetCurrentInterface": {
                            if (parameters == null)
                                return;

                            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables);
                            if (id == null)
                                return;

                            inst.CurrentInterfaceList?.SetCurrentInterface(id);
                            return;
                        }
                    case "LIST_AddInterface": {
                            if (parameters == null)
                                return;

                            var interfaces = ParseVarFunction(parameters.Get(0, "interfaces"), thisElement, customVariables);
                            var openID = ParseVarFunction(parameters.Get(1, "open_id"), thisElement, customVariables);
                            inst.CurrentInterfaceList?.LoadInterfaces(interfaces);
                            inst.CurrentInterfaceList?.SetCurrentInterface(openID);
                            return;
                        }
                    case "LIST_RemoveInterface": {
                            if (parameters == null)
                                return;

                            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables);
                            if (id == null)
                                return;

                            inst.CurrentInterfaceList?.Remove(id);
                            return;
                        }
                    case "LIST_ClearInterfaces": {
                            inst.CurrentInterfaceList?.Clear();
                            return;
                        }
                    case "LIST_CloseInterfaces": {
                            inst.CurrentInterfaceList?.CloseMenus();
                            return;
                        }
                    case "LIST_ClearChain": {
                            inst.CurrentInterfaceList?.ClearChain();
                            return;
                        }

                    #endregion

                    #region Audio

                    #region PlaySound

                    // Plays a sound. Can either be a default one already loaded in the SoundLibrary or a custom one from the menu's folder.
                    // Supports both JSON array and JSON object.
                    //
                    // - JSON Array Structure -
                    // 0 = sound
                    // 1 = volume
                    // 2 = pitch
                    // Example:
                    // [
                    //   "blip" < plays the blip sound.
                    //   "0.3" < sound is quiet.
                    //   "2" < sound is fast.
                    // ]
                    //
                    // - JSON Object Structure -
                    // "sound"
                    // "vol"
                    // "pitch"
                    // Example:
                    // {
                    //   "sound": "some kind of sound.ogg" < since this sound does not exist in the SoundLibrary, search for a file with the name. If it exists, play the sound.
                    //   "vol": "1" < default
                    //   "pitch": "0.5" < slow
                    // }
                    case "PlaySound": {
                            if (parameters == null || !inst.CurrentInterface)
                                return;

                            string sound = ParseVarFunction(parameters.Get(0, "sound"));
                            if (string.IsNullOrEmpty(sound))
                                return;

                            float volume = 1f;
                            var volumeJN = ParseVarFunction(parameters.Get(1, "vol"));
                            if (volumeJN != null)
                                volume = volumeJN;
                        
                            float pitch = 1f;
                            var pitchJN = ParseVarFunction(parameters.Get(2, "pitch"));
                            if (pitchJN != null)
                                pitch = pitchJN;

                            if (SoundManager.inst.TryGetSound(sound, out AudioClip audioClip))
                            {
                                SoundManager.inst.PlaySound(audioClip, volume, pitch);
                                return;
                            }

                            var filePath = $"{Path.GetDirectoryName(inst.CurrentInterface.filePath)}{sound}";
                            if (!RTFile.FileExists(filePath))
                                return;

                            var audioType = RTFile.GetAudioType(filePath);
                            if (audioType == AudioType.MPEG)
                                SoundManager.inst.PlaySound(LSAudio.CreateAudioClipUsingMP3File(filePath), volume, pitch);
                            else
                                CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip($"file://{filePath}", audioType, audioClip => SoundManager.inst.PlaySound(audioClip, volume, pitch)));

                            return;
                        }

                    #endregion

                    #region PlayMusic

                    // Plays a music. Can either be a default internal song or one located in the menu path.
                    // Supports both JSON array and JSON object.
                    //
                    // - JSON Array Structure -
                    // 0 = song
                    // 1 = fade duration
                    // 2 = loop
                    // Example:
                    // [
                    //   "distance" < plays the distance song.
                    //   "2" < sets fade duration to 2.
                    //   "False" < doesn't loop
                    // ]
                    //
                    // - JSON Object Structure -
                    // "sound"
                    // Example:
                    // {
                    //   "sound": "some kind of song.ogg" < since this song does not exist in the SoundLibrary, search for a file with the name. If it exists, play the song.
                    // }
                    case "PlayMusic": {
                            if (parameters == null)
                            {
                                inst.PlayMusic();
                                return;
                            }

                            string music = ParseVarFunction(parameters.Get(0, "name"), thisElement, customVariables);

                            if (string.IsNullOrEmpty(music) || music.ToLower() == "default")
                            {
                                inst.PlayMusic();
                                return;
                            }

                            var fadeDuration = ParseVarFunction(parameters.GetOrDefault(1, "fade_duration", 0.5f), thisElement, customVariables);

                            var loop = ParseVarFunction(parameters.GetOrDefault(2, "loop", true), thisElement, customVariables);

                            var filePath = $"{Path.GetDirectoryName(inst.CurrentInterface.filePath)}{music}";
                            if (!RTFile.FileExists(filePath))
                            {
                                inst.PlayMusic(AudioManager.inst.GetMusic(music), fadeDuration: fadeDuration, loop: loop);
                                return;
                            }

                            var audioType = RTFile.GetAudioType(filePath);
                            if (audioType == AudioType.MPEG)
                                inst.PlayMusic(LSAudio.CreateAudioClipUsingMP3File(filePath), fadeDuration: fadeDuration, loop: loop);
                            else
                                CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip($"file://{filePath}", audioType, audioClip => inst.PlayMusic(audioClip, fadeDuration: fadeDuration, loop: loop)));

                            return;
                        }

                    #endregion

                    #region StopMusic

                    // Stops the currently playing music. Can be good for moments where we want silence.
                    // Function has no parameters.
                    case "StopMusic": {
                            inst.StopMusic();
                            return;
                        }

                    #endregion

                    #region PauseMusic

                    // Pauses the current music if it's currently playing.
                    case "PauseMusic": {
                            if (CoreHelper.InGame && parameters != null && (parameters.IsArray && !parameters[0].AsBool || parameters.IsObject && !parameters["game_audio"].AsBool))
                                inst.CurrentAudioSource.Pause();
                            else
                                AudioManager.inst.CurrentAudioSource.Pause();

                            return;
                        }

                    #endregion

                    #region ResumeMusic

                    // Resumes the current music if it was paused.
                    case "ResumeMusic": {
                            if (CoreHelper.InGame && parameters != null && (parameters.IsArray && !parameters[0].AsBool || parameters.IsObject && !parameters["game_audio"].AsBool))
                                inst.CurrentAudioSource.UnPause();
                            else
                                AudioManager.inst.CurrentAudioSource.UnPause();

                            return;
                        }

                    #endregion

                    #endregion

                    #region Elements

                    #region Move

                    case "Move": {
                            if (!thisElement.gameObject || parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["x"] == null || parameters["y"] == null))
                                return;

                            var jnX = ParseVarFunction(parameters.Get(0, "x"), thisElement, customVariables);
                            var jnY = ParseVarFunction(parameters.Get(1, "y"), thisElement, customVariables);

                            var variables = new Dictionary<string, float>
                            {
                                { "elementPosX", thisElement.gameObject.transform.localPosition.x },
                                { "elementPosY", thisElement.gameObject.transform.localPosition.y },
                            };

                            var x = string.IsNullOrEmpty(jnX) ? thisElement.gameObject.transform.localPosition.x : RTMath.Parse(jnX, variables);
                            var y = string.IsNullOrEmpty(jnY) ? thisElement.gameObject.transform.localPosition.y : RTMath.Parse(jnX, variables);

                            thisElement.gameObject.transform.localPosition = new Vector3(x, y, thisElement.gameObject.transform.localPosition.z);

                            return;
                        }

                    #endregion

                    #region SetElementActive

                    // Sets an element active or inactive.
                    // Supports both JSON array and JSON object.
                    //
                    // - JSON Array Structure -
                    // 0 = id
                    // 1 = actiive
                    // Example:
                    // [
                    //   "525778246", < finds an element with this ID.
                    //   "False" < sets the element inactive.
                    // ]
                    //
                    // - JSON Object Structure -
                    // "id"
                    // "active"
                    // Example:
                    // {
                    //   "id": "525778246",
                    //   "active": "True" < sets the element active
                    // }
                    case "SetElementActive": {
                            if (parameters == null || !inst.CurrentInterface)
                                return;

                            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables);
                            if (id == null)
                                return;

                            var active = ParseVarFunction(parameters.Get(1, "active"), thisElement, customVariables);

                            if (inst.CurrentInterface.elements.TryFind(x => x.id == id, out MenuImage menuImage) && menuImage.gameObject)
                                menuImage.gameObject.SetActive(active);

                            return;
                        }

                    #endregion

                    #region SetLayoutActive

                    // Sets a layout active or inactive.
                    // Supports both JSON array and JSON object.
                    //
                    // - JSON Array Structure -
                    // 0 = name
                    // 1 = active
                    // Example:
                    // [
                    //   "layout_name", < finds a layout with this ID.
                    //   "False" < sets the layout inactive.
                    // ]
                    //
                    // - JSON Object Structure -
                    // "name"
                    // "active"
                    // Example:
                    // {
                    //   "id": "layout_name",
                    //   "active": "True" < sets the layout active
                    // }
                    case "SetLayoutActive": {
                            if (parameters == null || !inst.CurrentInterface)
                                return;

                            var layoutName = ParseVarFunction(parameters.Get(0, "name"), thisElement, customVariables);
                            if (layoutName == null)
                                return;

                            var active = ParseVarFunction(parameters.Get(1, "active"), thisElement, customVariables);

                            if (inst.CurrentInterface.layouts.TryGetValue(layoutName, out MenuLayoutBase layout) && layout.gameObject)
                                layout.gameObject.SetActive(active);

                            return;
                        }

                    #endregion

                    #region AnimateID

                    // Finds an element with a matching ID and animates it.
                    // Supports both JSON array and JSON object.
                    //
                    // - JSON Array Structure -
                    // 0 = ID
                    // 1 = type (integer, 0 = position, 1 = scale, 2 = rotation)
                    // 2 = looping (boolean true or false)
                    // 3 = keyframes
                    // 4 = anim done func
                    // Example:
                    // [
                    //   "0", < ID
                    //   "0", < type
                    //   "True", < looping
                    //   {
                    //     "x": [
                    //       {
                    //         "t": "0", < usually a good idea to have the first keyframes' start time set to 0.
                    //         "val": "0",
                    //         "rel": "True", < if true and the keyframe is the first keyframe, offset from current transform value.
                    //         "ct": "Linear" < Easing / Curve Type.
                    //       },
                    //       {
                    //         "t": "1",
                    //         "val": "10", < moves X somewhere.
                    //         "rel": "True" < if true, adds to previous keyframe value.
                    //         // ct doesn't always need to exist. If it doesn't, then it'll automatically be Linear easing.
                    //       }
                    //     ],
                    //     "y": [
                    //       {
                    //         "t": "0",
                    //         "val": "0",
                    //         // relative is false by default, so no need to do "rel": "False". With it set to false, the objects' Y position will be snapped to 0 instead of offsetting from its original position.
                    //       }
                    //     ],
                    //     "z": [
                    //       {
                    //         "t": "0",
                    //         "val": "0",
                    //       }
                    //     ]
                    //   }, < keyframes
                    //   {
                    //     "name": "Log",
                    //     "params": [
                    //       "Animation done!"
                    //     ]
                    //   } < function to run when animation is complete.
                    // ]
                    // 
                    // - JSON Object Structure -
                    // "id"
                    // "type"
                    // "loop"
                    // "events" ("x", "y", "z")
                    // Example:
                    // {
                    //   "id": "0",
                    //   "type": "1", < animates scale
                    //   "loop": "False", < loop doesn't need to exist.
                    //   "events": {
                    //     "x": [
                    //       {
                    //         "t": "0",
                    //         "val": "0"
                    //       }
                    //     ],
                    //     "y": [
                    //       {
                    //         "t": "0",
                    //         "val": "0"
                    //       }
                    //     ],
                    //     "y": [
                    //       {
                    //         "t": "0",
                    //         "val": "0"
                    //       }
                    //     ]
                    //   },
                    //   "done_func": { < function code here
                    //   }
                    // }
                    case "AnimateID": {
                            if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["id"] == null || !thisElement)
                                return;

                            var isArray = parameters.IsArray;
                            string id = isArray ? parameters[0] : parameters["id"]; // ID of an object to animate
                            var type = Parser.TryParse(isArray ? parameters[1] : parameters["type"], 0); // which type to animate (e.g. 0 = position, 1 = scale, 2 = rotation)
                            var isColor = type == 3;

                            if (inst.CurrentInterface.elements.TryFind(x => x.id == id, out MenuImage element))
                            {
                                var animation = new RTAnimation($"Interface Element Animation {element.id}"); // no way element animation reference :scream:

                                animation.loop = isArray ? parameters[2].AsBool : parameters["loop"].AsBool;

                                var events = isArray ? parameters[3] : parameters["events"];

                                JSONNode lastX = null;
                                float x = 0f;
                                if (!isColor && events["x"] != null)
                                {
                                    List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                    for (int i = 0; i < events["x"].Count; i++)
                                    {
                                        var kf = events["x"][i];
                                        var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? element.GetTransform(type, 0) : 0f);
                                        x = kf["rel"].AsBool ? x + val : val;
                                        keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, x, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                        lastX = kf["val"];
                                    }
                                    animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, x => { element.SetTransform(type, 0, x); }));
                                }
                                if (isColor && events["x"] != null)
                                {
                                    List<IKeyframe<Color>> keyframes = new List<IKeyframe<Color>>();
                                    for (int i = 0; i < events["x"].Count; i++)
                                    {
                                        var kf = events["x"][i];
                                        var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? element.GetTransform(type, 0) : 0f);
                                        x = kf["rel"].AsBool ? x + val : val;
                                        keyframes.Add(new ThemeKeyframe(kf["t"].AsFloat, (int)x, 0.0f, 0.0f, 0.0f, 0.0f, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                        lastX = kf["val"];
                                    }
                                    animation.animationHandlers.Add(new AnimationHandler<Color>(keyframes, x =>
                                    {
                                        element.useOverrideColor = true;
                                        element.overrideColor = x;
                                    }));
                                }

                                JSONNode lastY = null;
                                float y = 0f;
                                if (!isColor && events["y"] != null)
                                {
                                    List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                    for (int i = 0; i < events["y"].Count; i++)
                                    {
                                        var kf = events["y"][i];
                                        var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? element.GetTransform(type, 1) : 0f);
                                        y = kf["rel"].AsBool ? y + val : val;
                                        keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, y, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                        lastY = kf["val"];
                                    }
                                    animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, x => { element.SetTransform(type, 1, x); }));
                                }

                                JSONNode lastZ = null;
                                float z = 0f;
                                if (!isColor && events["z"] != null)
                                {
                                    List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                    for (int i = 0; i < events["z"].Count; i++)
                                    {
                                        var kf = events["z"][i];
                                        var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? element.GetTransform(type, 2) : 0f);
                                        z = kf["rel"].AsBool ? z + val : val;
                                        keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, z, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                        lastZ = kf["val"];
                                    }
                                    animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, x => { element.SetTransform(type, 2, x); }));
                                }

                                animation.onComplete = () =>
                                {
                                    if (animation.loop)
                                    {
                                        if (isArray && parameters.Count > 4 && parameters[4] != null || parameters["done_func"] != null)
                                            ParseFunction(isArray ? parameters[4] : parameters["done_func"]);

                                        return;
                                    }

                                    AnimationManager.inst.Remove(animation.id);
                                    thisElement.animations.RemoveAll(x => x.id == animation.id);

                                    if (!isColor && lastX != null)
                                        element.SetTransform(type, 0, x);
                                    if (!isColor && lastY != null)
                                        element.SetTransform(type, 1, y);
                                    if (!isColor && lastZ != null)
                                        element.SetTransform(type, 2, z);
                                    if (isColor && lastX != null)
                                        element.overrideColor = CoreHelper.CurrentBeatmapTheme.GetObjColor((int)x);

                                    if (isArray && parameters.Count > 4 && parameters[4] != null || parameters["done_func"] != null)
                                        ParseFunction(isArray ? parameters[4] : parameters["done_func"]);
                                };

                                thisElement.animations.Add(animation);
                                AnimationManager.inst.Play(animation);
                            }

                            return;
                        }

                    #endregion

                    #region AnimateName

                    // Same as animate ID, except instead of searching for an elements' ID, you search for a name. In case you'd rather find an objects' name instead of ID.
                    // No example needed.
                    case "AnimateName": {
                            if (parameters == null || parameters.Count < 1 || !thisElement)
                                return;

                            var elementName = parameters[0]; // Name of an object to animate
                            var type = Parser.TryParse(parameters[1], 0); // which type to animate (e.g. 0 = position, 1 = scale, 2 = rotation)

                            if (inst.CurrentInterface.elements.TryFind(x => x.name == elementName, out MenuImage element))
                            {
                                var animation = new RTAnimation("Interface Element Animation"); // no way element animation reference :scream:

                                animation.loop = parameters[2].AsBool;

                                JSONNode lastX = null;
                                float x = 0f;
                                if (parameters[3]["x"] != null)
                                {
                                    List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                    for (int i = 0; i < parameters[3]["x"].Count; i++)
                                    {
                                        var kf = parameters[3]["x"][i];
                                        var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? element.GetTransform(type, 0) : 0f);
                                        x = kf["rel"].AsBool ? x + val : val;
                                        keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, x, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                        lastX = kf["val"];
                                    }
                                    animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, x => { element.SetTransform(type, 0, x); }));
                                }

                                JSONNode lastY = null;
                                float y = 0f;
                                if (parameters[3]["y"] != null)
                                {
                                    List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                    for (int i = 0; i < parameters[3]["y"].Count; i++)
                                    {
                                        var kf = parameters[3]["y"][i];
                                        var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? element.GetTransform(type, 1) : 0f);
                                        y = kf["rel"].AsBool ? y + val : val;
                                        keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, y, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                        lastY = kf["val"];
                                    }
                                    animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, x => { element.SetTransform(type, 1, x); }));
                                }

                                JSONNode lastZ = null;
                                float z = 0f;
                                if (parameters[3]["z"] != null)
                                {
                                    List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                    for (int i = 0; i < parameters[3]["z"].Count; i++)
                                    {
                                        var kf = parameters[3]["z"][i];
                                        var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? element.GetTransform(type, 2) : 0f);
                                        z = kf["rel"].AsBool ? z + val : val;
                                        keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, z, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                        lastZ = kf["val"];
                                    }
                                    animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, x => { element.SetTransform(type, 2, x); }));
                                }

                                animation.onComplete = () =>
                                {
                                    if (animation.loop)
                                    {
                                        if (parameters.Count > 4 && parameters[4] != null)
                                            ParseFunction(parameters[4]);
                                        return;
                                    }

                                    AnimationManager.inst.Remove(animation.id);
                                    thisElement.animations.RemoveAll(x => x.id == animation.id);

                                    if (lastX != null)
                                        element.SetTransform(type, 0, x);
                                    if (lastY != null)
                                        element.SetTransform(type, 1, y);
                                    if (lastZ != null)
                                        element.SetTransform(type, 2, z);

                                    if (parameters.Count <= 4 || parameters[4] == null)
                                        return;

                                    ParseFunction(parameters[4]);
                                };

                                thisElement.animations.Add(animation);
                                AnimationManager.inst.Play(animation);
                            }

                            return;
                        }

                    #endregion

                    #region StopAnimations

                    // Stops all local animations created from the element.
                    // Supports both JSON array and JSON object.
                    //
                    // - JSON Array Structure -
                    // 0 = stop (runs onComplete method)
                    // 1 = id
                    // 2 = name
                    // Example:
                    // [
                    //   "True", < makes the animation run its on complete function.
                    //   "0", < makes the animation run its on complete function.
                    //   "0" < makes the animation run its on complete function.
                    // ]
                    //
                    // - JSON Object Structure -
                    // "stop"
                    // "id"
                    // "name"
                    // Example:
                    // {
                    //   "run_done_func": "False", < doesn't run on complete functions.
                    //   "id": "0", < tries to find an element with the matching ID.
                    //   "name": "355367" < checks if the animations' name contains this. If it does, then stop the animation. (name is based on the element ID it animates)
                    // }
                    case "StopAnimations": {
                            if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["run_done_func"] == null || !thisElement)
                                return;

                            var stop = parameters.IsArray ? parameters[0].AsBool : parameters["run_done_func"].AsBool;

                            var animations = thisElement.animations;
                            string id = parameters.IsArray && parameters.Count > 1 ? parameters[1] : parameters.IsObject && parameters["id"] != null ? parameters["id"] : string.Empty;
                            if (!string.IsNullOrEmpty(id) && inst.CurrentInterface.elements.TryFind(x => x.id == id, out MenuImage menuImage))
                                animations = menuImage.animations;

                            string animName = parameters.IsArray && parameters.Count > 2 ? parameters[2] : parameters.IsObject && parameters["name"] != null ? parameters["name"] : string.Empty;

                            for (int i = 0; i < animations.Count; i++)
                            {
                                var animation = animations[i];
                                if (!string.IsNullOrEmpty(animName) && !animation.name.Remove("Interface Element Animation ").Contains(animName))
                                    continue;

                                if (stop)
                                    animation.onComplete?.Invoke();

                                animation.Pause();
                                AnimationManager.inst.Remove(animation.id);
                            }

                            return;
                        }

                    #endregion

                    #region SetColor

                    // Sets the elements' color slot.
                    // Supports both JSON array and JSON object.
                    //
                    // - JSON Array Structure -
                    // 0 = color
                    // Example:
                    // [
                    //   "2"
                    // ]
                    //
                    // - JSON Object Structure -
                    // "col"
                    // Example:
                    // {
                    //   "col": "17" < uses Beatmap Theme object color slots, so max should be 17 (including 0).
                    // }
                    case "SetColor": {
                            if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["col"] == null || !thisElement)
                                return;

                            thisElement.color = parameters.IsArray ? parameters[0].AsInt : parameters["col"].AsInt;

                            return;
                        }

                    #endregion

                    #region SetText

                    // Sets an objects' text.
                    // Supports both JSON array and JSON object.
                    // 
                    // - JSON Array Structure -
                    // 0 = id
                    // 0 = text
                    // Example:
                    // [
                    //   "100",
                    //   "This is a text example!"
                    // ]
                    // 
                    // - JSON Object Structure -
                    // "id"
                    // "text"
                    // Example:
                    // {
                    //   "id": "100",
                    //   "text": "This is a text example!"
                    // }
                    case "SetText": {
                            if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["id"] == null || parameters["text"] == null) || !inst.CurrentInterface)
                                return;

                            var text = ParseVarFunction(parameters.Get(0, "text"), thisElement, customVariables);
                            if (text == null || !text.IsString)
                                return;

                            var id = ParseVarFunction(parameters.Get(1, "id"), thisElement, customVariables);

                            var element = id == null ? thisElement : inst.CurrentInterface.elements.Find(x => x.id == id);
                            if (element is not MenuText menuText)
                                return;

                            menuText.text = text;
                            menuText.textUI.maxVisibleCharacters = text.Value.Length;
                            menuText.textUI.text = text;

                            return;
                        }

                    #endregion

                    #region RemoveElement

                    // Finds an element with a matching ID, destroys its object and removes it.
                    // Supports both JSON array and JSON object.
                    // 
                    // - JSON Array Structure -
                    // 0 = id
                    // Example:
                    // [
                    //   "522666" < ID to find
                    // ]
                    // 
                    // - JSON Object Structure -
                    // "id"
                    // Example:
                    // {
                    //   "id": "85298259"
                    // }
                    case "RemoveElement": {
                            if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["id"] == null)
                                return;

                            var id = parameters.IsArray ? parameters[0] : parameters["id"];
                            if (inst.CurrentInterface.elements.TryFind(x => x.id == id, out MenuImage element))
                            {
                                element.Clear();
                                if (element.gameObject)
                                    CoreHelper.Destroy(element.gameObject);
                                inst.CurrentInterface.elements.Remove(element);
                            }

                            return;
                        }

                    #endregion

                    #region RemoveMultipleElements

                    // Finds an element with a matching ID, destroys its object and removes it.
                    // Supports both JSON array and JSON object.
                    // 
                    // - JSON Array Structure -
                    // 0 = ids
                    // Example:
                    // [
                    //   [
                    //     "522666",
                    //     "2672",
                    //     "824788",
                    //   ]
                    // ]
                    // 
                    // - JSON Object Structure -
                    // "ids"
                    // Example:
                    // {
                    //   "ids": [
                    //     "522666",
                    //     "2672",
                    //     "824788",
                    //   ]
                    // }
                    case "RemoveMultipleElements": {
                            if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["ids"] == null)
                                return;

                            var ids = parameters.IsArray ? parameters[0] : parameters["ids"];
                            if (ids == null)
                                return;

                            for (int i = 0; i < ids.Count; i++)
                            {
                                var id = ids[i];
                                if (inst.CurrentInterface.elements.TryFind(x => x.id == id, out MenuImage element))
                                {
                                    element.Clear();
                                    if (element.gameObject)
                                        CoreHelper.Destroy(element.gameObject);
                                    inst.CurrentInterface.elements.Remove(element);
                                }
                            }

                            return;
                        }

                    #endregion

                    #region AddElement

                    // Adds a list of elements to the interface.
                    // Supports both JSON array and JSON object.
                    // 
                    // - JSON Array Structure -
                    // 0 = elements
                    // Example:
                    // [
                    //   {
                    //     "type": "Image",
                    //     "id": "5343663626",
                    //     "name": "BG",
                    //     "rect": {
                    //       "anc_pos": {
                    //         "x": "0",
                    //         "y": "0"
                    //       }
                    //     }
                    //   }
                    // ]
                    // 
                    // - JSON Object Structure -
                    // "elements"
                    // Example:
                    // {
                    //   "type": "Image",
                    //   "id": "5343663626",
                    //   "name": "BG",
                    //   "rect": {
                    //     "anc_pos": {
                    //       "x": "0",
                    //       "y": "0"
                    //     }
                    //   }
                    // }
                    case "AddElement": {
                            if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["elements"] == null)
                                return;

                            var customMenu = inst.CurrentInterface;
                            customMenu.elements.AddRange(CustomMenu.ParseElements(parameters.IsArray ? parameters[0] : parameters["elements"], customMenu.prefabs, customMenu.spriteAssets));

                            customMenu.StartGeneration();

                            return;
                        }

                    #endregion

                    #region ScrollLayout

                    case "ScrollLayout": {
                            if (parameters == null)
                                return;

                            var layoutName = ParseVarFunction(parameters.Get(0, "layout"), thisElement, customVariables);
                            if (layoutName == null || !layoutName.IsString)
                                return;

                            if (!inst.CurrentInterface.layouts.TryGetValue(layoutName, out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                return;

                            if (menuLayout is MenuGridLayout menuGridLayout)
                                menuGridLayout.Scroll(ParseVarFunction(parameters.Get(1, "x"), thisElement, customVariables), ParseVarFunction(parameters.Get(2, "y"), thisElement, customVariables), ParseVarFunction(parameters.Get(3, "x_additive"), thisElement, customVariables), ParseVarFunction(parameters.Get(4, "y_additive"), thisElement, customVariables));

                            if (menuLayout is MenuHorizontalOrVerticalLayout menuHorizontalLayout)
                                menuHorizontalLayout.Scroll(ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables), ParseVarFunction(parameters.Get(2, "additive"), thisElement, customVariables));

                            return;
                        }

                    #endregion

                    #region SetElementSelectable

                    // Sets an element selectable value. Buttons will deselect when selectable is turned off.
                    // Supports both JSON array and JSON object.
                    //
                    // - JSON Array Structure -
                    // 0 = id
                    // 1 = actiive
                    // Example:
                    // [
                    //   "525778246", < finds an element with this ID.
                    //   "False" < disables element selection.
                    // ]
                    //
                    // - JSON Object Structure -
                    // "id"
                    // "active"
                    // Example:
                    // {
                    //   "id": "525778246",
                    //   "selectable": "True" < sets the element as selectable.
                    // }
                    case "SetElementSelectable": {
                            if (parameters == null || !inst.CurrentInterface)
                                return;

                            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables);
                            if (id == null)
                                return;

                            var selectable = ParseVarFunction(parameters.Get(1, "selectable"), thisElement, customVariables);

                            if (inst.CurrentInterface.elements.TryFind(x => x.id == id, out MenuImage menuImage))
                                menuImage.selectable = selectable;

                            return;
                        }

                    #endregion

                    #region SetInputFieldText

                    // Sets an input field elements' text.
                    // Supports both JSON array and JSON object.
                    //
                    // - JSON Array Structure -
                    // 0 = id
                    // 1 = text
                    // 2 = trigger (optional)
                    // Example:
                    // [
                    //   "525778246", < finds an element with this ID.
                    //   "Text!" < sets the text
                    //   "True" < triggers the input field value changed function
                    // ]
                    //
                    // - JSON Object Structure -
                    // "id"
                    // "text"
                    // "trigger" (optional)
                    // Example:
                    // {
                    //   "id": "525778246",
                    //   "text": "What" < sets the text
                    //   "trigger": "False" < only sets the display text, does not trigger the input field value changed function
                    // }
                    case "SetInputFieldText": {
                            if (parameters == null || !inst.CurrentInterface)
                                return;

                            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables);
                            if (id == null)
                                return;

                            var text = ParseVarFunction(parameters.Get(1, "text"), thisElement, customVariables);

                            if (inst.CurrentInterface.elements.TryFind(x => x.id == id, out MenuImage menuImage) && menuImage is MenuInputField menuInputField && menuInputField.inputField)
                            {
                                if (ParseVarFunction(parameters.GetOrDefault(2, "trigger", true), thisElement, customVariables).AsBool)
                                    menuInputField.inputField.text = text;
                                else
                                    menuInputField.inputField.SetTextWithoutNotify(text);
                            }

                            return;
                        }

                    #endregion

                    #region ApplyElement

                    // Sets an input field elements' text.
                    // Supports both JSON array and JSON object.
                    //
                    // - JSON Array Structure -
                    // 0 = element JSON
                    // 1 = id
                    // Example:
                    // [
                    //   {
                    //     ... < JSON object representing element values
                    //   },
                    //   "525778246" < finds an element with this ID.
                    // ]
                    //
                    // - JSON Object Structure -
                    // "element"
                    // "id"
                    // Example:
                    // {
                    //   "element": {
                    //     ...
                    //   },
                    //   "id": null < id can be left null to specify the element this function runs from
                    // }
                    case "ApplyElement": {
                            if (parameters == null)
                                return;

                            var jnElement = ParseVarFunction(parameters.Get(0, "element"), thisElement, customVariables);
                            var id = ParseVarFunction(parameters.Get(1, "id"), thisElement, customVariables);

                            var element = id == null ? thisElement : inst.CurrentInterface.elements.Find(x => x.id == id);
                            if (!element)
                                return;

                            element.Read(jnElement, 0, 0, inst.CurrentInterface.spriteAssets, customVariables);

                            return;
                        }

                    #endregion

                    #endregion

                    #region Effects

                    #region SetDefaultEvents

                    case "SetDefaultEvents": {
                            if (CoreHelper.InGame || !MenuEffectsManager.inst)
                                return;

                            MenuEffectsManager.inst.SetDefaultEffects();

                            return;
                        }

                    #endregion

                    #region AnimateEvent

                    // Animates a specific type of event (e.g. camera).
                    // Supports both JSON array and JSON object.
                    //
                    // - JSON Array Structure -
                    // 0 = type (name, "MoveCamera", "ZoomCamera", "RotateCamera")
                    // 1 = looping (boolean true or false)
                    // 2 = keyframes
                    // 3 = anim done func
                    // Example:
                    // [
                    //   "MoveCamera",
                    //   "False",
                    //   {
                    //     "x": [
                    //       {
                    //         "t": "0",
                    //         "val": "0"
                    //       }
                    //     ],
                    //     "y": [
                    //       {
                    //         "t": "0",
                    //         "val": "0"
                    //       }
                    //     ]
                    //   }
                    // ]
                    // 
                    // - JSON Object Structure -
                    // "type"
                    // "loop"
                    // "events"
                    // "done_func"
                    // Example: (zooms the camera in and out)
                    // {
                    //   "type": "ZoomCamera",
                    //   "loop": "True",
                    //   "events": {
                    //     "x": [
                    //       {
                    //         "t": "0",
                    //         "val": "5" < 5 is the default camera zoom.
                    //       },
                    //       {
                    //         "t": "1",
                    //         "val": "7",
                    //         "ct": "InOutSine"
                    //       },
                    //       {
                    //         "t": "2",
                    //         "val": "5",
                    //         "ct": "InOutSine"
                    //       }
                    //     ]
                    //   }
                    // }
                    case "AnimateEvent": {
                            if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["type"] == null || !thisElement)
                                return;

                            var isArray = parameters.IsArray;
                            var type = isArray ? parameters[0] : parameters["type"];

                            if (type.IsNumber)
                                return;

                            var events = isArray ? parameters[2] : parameters["events"];
                            var animation = new RTAnimation($"Interface Element Animation {thisElement.id}"); // no way element animation reference :scream:

                            animation.loop = isArray ? parameters[1].AsBool : parameters["loop"].AsBool;

                            switch (type.Value)
                            {
                                case "MoveCamera": {
                                        JSONNode lastX = null;
                                        float x = 0f;
                                        if (events["x"] != null)
                                        {
                                            List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                            for (int i = 0; i < events["x"].Count; i++)
                                            {
                                                var kf = events["x"][i];
                                                var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localPosition.x : 0f);
                                                x = kf["rel"].AsBool ? x + val : val;
                                                keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, x, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                                lastX = kf["val"];
                                            }
                                            animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.MoveCameraX));
                                        }

                                        JSONNode lastY = null;
                                        float y = 0f;
                                        if (events["y"] != null)
                                        {
                                            List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                            for (int i = 0; i < events["y"].Count; i++)
                                            {
                                                var kf = events["y"][i];
                                                var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localPosition.y : 0f);
                                                y = kf["rel"].AsBool ? y + val : val;
                                                keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, y, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                                lastY = kf["val"];
                                            }
                                            animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.MoveCameraY));
                                        }

                                        animation.onComplete = () =>
                                        {
                                            if (animation.loop)
                                            {
                                                if (isArray && parameters.Count > 3 && parameters[3] != null || parameters["done_func"] != null)
                                                    ParseFunction(isArray ? parameters[3] : parameters["done_func"]);

                                                return;
                                            }

                                            AnimationManager.inst.Remove(animation.id);
                                            thisElement.animations.RemoveAll(x => x.id == animation.id);

                                            if (lastX != null)
                                                MenuEffectsManager.inst.MoveCameraX(x);
                                            if (lastY != null)
                                                MenuEffectsManager.inst.MoveCameraY(y);

                                            if (isArray && parameters.Count > 3 && parameters[3] != null || parameters["done_func"] != null)
                                                ParseFunction(isArray ? parameters[3] : parameters["done_func"]);
                                        };

                                        return;
                                    }
                                case "ZoomCamera": {
                                        JSONNode lastX = null;
                                        float x = 0f;
                                        if (events["x"] != null)
                                        {
                                            List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                            for (int i = 0; i < events["x"].Count; i++)
                                            {
                                                var kf = events["x"][i];
                                                var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.orthographicSize : 0f);
                                                x = kf["rel"].AsBool ? x + val : val;
                                                keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, x, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                                lastX = kf["val"];
                                            }
                                            animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.ZoomCamera));
                                        }

                                        animation.onComplete = () =>
                                        {
                                            if (animation.loop)
                                            {
                                                if (isArray && parameters.Count > 3 && parameters[3] != null || parameters["done_func"] != null)
                                                    ParseFunction(isArray ? parameters[3] : parameters["done_func"]);

                                                return;
                                            }

                                            AnimationManager.inst.Remove(animation.id);
                                            thisElement.animations.RemoveAll(x => x.id == animation.id);

                                            if (lastX != null)
                                                MenuEffectsManager.inst.ZoomCamera(x);

                                            if (isArray && parameters.Count > 3 && parameters[3] != null || parameters["done_func"] != null)
                                                ParseFunction(isArray ? parameters[3] : parameters["done_func"]);
                                        };

                                        return;
                                    }
                                case "RotateCamera": {
                                        JSONNode lastX = null;
                                        float x = 0f;
                                        if (events["x"] != null)
                                        {
                                            List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                            for (int i = 0; i < events["x"].Count; i++)
                                            {
                                                var kf = events["x"][i];
                                                var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localEulerAngles.z : 0f);
                                                x = kf["rel"].AsBool ? x + val : val;
                                                keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, x, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                                lastX = kf["val"];
                                            }
                                            animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.RotateCamera));
                                        }

                                        animation.onComplete = () =>
                                        {
                                            if (animation.loop)
                                            {
                                                if (isArray && parameters.Count > 3 && parameters[3] != null || parameters["done_func"] != null)
                                                    ParseFunction(isArray ? parameters[3] : parameters["done_func"]);

                                                return;
                                            }

                                            AnimationManager.inst.Remove(animation.id);
                                            thisElement.animations.RemoveAll(x => x.id == animation.id);

                                            if (lastX != null)
                                                MenuEffectsManager.inst.RotateCamera(x);

                                            if (isArray && parameters.Count > 3 && parameters[3] != null || parameters["done_func"] != null)
                                                ParseFunction(isArray ? parameters[3] : parameters["done_func"]);
                                        };

                                        return;
                                    }
                                case "Chroma":
                                case "Chromatic": {
                                        JSONNode lastX = null;
                                        float x = 0f;
                                        if (events["x"] != null)
                                        {
                                            List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                            for (int i = 0; i < events["x"].Count; i++)
                                            {
                                                var kf = events["x"][i];
                                                var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localEulerAngles.z : 0f);
                                                x = kf["rel"].AsBool ? x + val : val;
                                                keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, x, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                                lastX = kf["val"];
                                            }
                                            animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.UpdateChroma));
                                        }

                                        animation.onComplete = () =>
                                        {
                                            if (animation.loop)
                                            {
                                                if (isArray && parameters.Count > 3 && parameters[3] != null || parameters["done_func"] != null)
                                                    ParseFunction(isArray ? parameters[3] : parameters["done_func"]);

                                                return;
                                            }

                                            AnimationManager.inst.Remove(animation.id);
                                            thisElement.animations.RemoveAll(x => x.id == animation.id);

                                            if (lastX != null)
                                                MenuEffectsManager.inst.UpdateChroma(x);

                                            if (isArray && parameters.Count > 3 && parameters[3] != null || parameters["done_func"] != null)
                                                ParseFunction(isArray ? parameters[3] : parameters["done_func"]);
                                        };

                                        return;
                                    }
                                case "Bloom": {
                                        JSONNode lastX = null;
                                        float x = 0f;
                                        if (events["x"] != null)
                                        {
                                            List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                            for (int i = 0; i < events["x"].Count; i++)
                                            {
                                                var kf = events["x"][i];
                                                var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localEulerAngles.z : 0f);
                                                x = kf["rel"].AsBool ? x + val : val;
                                                keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, x, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                                lastX = kf["val"];
                                            }
                                            animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.UpdateBloomIntensity));
                                        }

                                        JSONNode lastY = null;
                                        float y = 0f;
                                        if (events["y"] != null)
                                        {
                                            List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                            for (int i = 0; i < events["y"].Count; i++)
                                            {
                                                var kf = events["y"][i];
                                                var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localEulerAngles.z : 0f);
                                                y = kf["rel"].AsBool ? y + val : val;
                                                keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, y, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                                lastY = kf["val"];
                                            }
                                            animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.UpdateBloomDiffusion));
                                        }

                                        JSONNode lastZ = null;
                                        float z = 0f;
                                        if (events["z"] != null)
                                        {
                                            List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                            for (int i = 0; i < events["z"].Count; i++)
                                            {
                                                var kf = events["z"][i];
                                                var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localEulerAngles.z : 0f);
                                                z = kf["rel"].AsBool ? z + val : val;
                                                keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, z, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                                lastZ = kf["val"];
                                            }
                                            animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.UpdateBloomThreshold));
                                        }

                                        JSONNode lastX2 = null;
                                        float x2 = 0f;
                                        if (events["x2"] != null)
                                        {
                                            List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                            for (int i = 0; i < events["x2"].Count; i++)
                                            {
                                                var kf = events["x2"][i];
                                                var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localEulerAngles.z : 0f);
                                                x2 = kf["rel"].AsBool ? x2 + val : val;
                                                keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, x2, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                                lastX2 = kf["val"];
                                            }
                                            animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.UpdateBloomAnamorphicRatio));
                                        }

                                        animation.onComplete = () =>
                                        {
                                            if (animation.loop)
                                            {
                                                if (isArray && parameters.Count > 3 && parameters[3] != null || parameters["done_func"] != null)
                                                    ParseFunction(isArray ? parameters[3] : parameters["done_func"]);

                                                return;
                                            }

                                            AnimationManager.inst.Remove(animation.id);
                                            thisElement.animations.RemoveAll(x => x.id == animation.id);

                                            if (lastX != null)
                                                MenuEffectsManager.inst.UpdateBloomIntensity(x);
                                            if (lastY != null)
                                                MenuEffectsManager.inst.UpdateBloomIntensity(y);
                                            if (lastZ != null)
                                                MenuEffectsManager.inst.UpdateBloomThreshold(z);
                                            if (lastX2 != null)
                                                MenuEffectsManager.inst.UpdateBloomAnamorphicRatio(x2);

                                            if (isArray && parameters.Count > 3 && parameters[3] != null || parameters["done_func"] != null)
                                                ParseFunction(isArray ? parameters[3] : parameters["done_func"]);
                                        };

                                        return;
                                    }
                                case "Lens":
                                case "LensDistort": {
                                        JSONNode lastX = null;
                                        float x = 0f;
                                        if (events["x"] != null)
                                        {
                                            List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                            for (int i = 0; i < events["x"].Count; i++)
                                            {
                                                var kf = events["x"][i];
                                                var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localEulerAngles.z : 0f);
                                                x = kf["rel"].AsBool ? x + val : val;
                                                keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, x, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                                lastX = kf["val"];
                                            }
                                            animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.UpdateLensDistortIntensity));
                                        }

                                        JSONNode lastY = null;
                                        float y = 0f;
                                        if (events["y"] != null)
                                        {
                                            List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                            for (int i = 0; i < events["y"].Count; i++)
                                            {
                                                var kf = events["y"][i];
                                                var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localEulerAngles.z : 0f);
                                                y = kf["rel"].AsBool ? y + val : val;
                                                keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, y, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                                lastY = kf["val"];
                                            }
                                            animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.UpdateLensDistortCenterX));
                                        }

                                        JSONNode lastZ = null;
                                        float z = 0f;
                                        if (events["z"] != null)
                                        {
                                            List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                            for (int i = 0; i < events["z"].Count; i++)
                                            {
                                                var kf = events["z"][i];
                                                var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localEulerAngles.z : 0f);
                                                z = kf["rel"].AsBool ? z + val : val;
                                                keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, z, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                                lastZ = kf["val"];
                                            }
                                            animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.UpdateLensDistortCenterY));
                                        }

                                        JSONNode lastX2 = null;
                                        float x2 = 0f;
                                        if (events["x2"] != null)
                                        {
                                            List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                            for (int i = 0; i < events["x2"].Count; i++)
                                            {
                                                var kf = events["x2"][i];
                                                var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localEulerAngles.z : 0f);
                                                x2 = kf["rel"].AsBool ? x2 + val : val;
                                                keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, x2, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                                lastX2 = kf["val"];
                                            }
                                            animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.UpdateLensDistortIntensityX));
                                        }

                                        JSONNode lastY2 = null;
                                        float y2 = 0f;
                                        if (events["y2"] != null)
                                        {
                                            List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                            for (int i = 0; i < events["y2"].Count; i++)
                                            {
                                                var kf = events["y2"][i];
                                                var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localEulerAngles.z : 0f);
                                                y2 = kf["rel"].AsBool ? y2 + val : val;
                                                keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, y2, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                                lastY2 = kf["val"];
                                            }
                                            animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.UpdateLensDistortIntensityY));
                                        }

                                        JSONNode lastZ2 = null;
                                        float z2 = 0f;
                                        if (events["z2"] != null)
                                        {
                                            List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                            for (int i = 0; i < events["z2"].Count; i++)
                                            {
                                                var kf = events["z2"][i];
                                                var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localEulerAngles.z : 0f);
                                                z2 = kf["rel"].AsBool ? z2 + val : val;
                                                keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, z2, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                                lastZ2 = kf["val"];
                                            }
                                            animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.UpdateLensDistortScale));
                                        }

                                        animation.onComplete = () =>
                                        {
                                            if (animation.loop)
                                            {
                                                if (isArray && parameters.Count > 3 && parameters[3] != null || parameters["done_func"] != null)
                                                    ParseFunction(isArray ? parameters[3] : parameters["done_func"]);

                                                return;
                                            }

                                            AnimationManager.inst.Remove(animation.id);
                                            thisElement.animations.RemoveAll(x => x.id == animation.id);

                                            if (lastX != null)
                                                MenuEffectsManager.inst.UpdateLensDistortIntensity(x);
                                            if (lastY != null)
                                                MenuEffectsManager.inst.UpdateLensDistortCenterX(y);
                                            if (lastZ != null)
                                                MenuEffectsManager.inst.UpdateLensDistortCenterY(z);
                                            if (lastX2 != null)
                                                MenuEffectsManager.inst.UpdateLensDistortIntensityX(x2);
                                            if (lastY2 != null)
                                                MenuEffectsManager.inst.UpdateLensDistortIntensityY(y2);
                                            if (lastZ2 != null)
                                                MenuEffectsManager.inst.UpdateLensDistortScale(z2);

                                            if (isArray && parameters.Count > 3 && parameters[3] != null || parameters["done_func"] != null)
                                                ParseFunction(isArray ? parameters[3] : parameters["done_func"]);
                                        };

                                        return;
                                    }
                            }

                            thisElement.animations.Add(animation);
                            AnimationManager.inst.Play(animation);

                            return;
                        }

                    #endregion

                    #region UpdateEvent

                    case "UpdateEvent": {
                            if (parameters == null)
                                return;

                            var effect = ParseVarFunction(parameters.Get(0, "effect"), thisElement, customVariables);
                            if (effect == null || !effect.IsString || !MenuEffectsManager.inst || !MenuEffectsManager.inst.functions.TryGetValue(effect, out Action<float> action))
                                return;

                            action?.Invoke(ParseVarFunction(parameters.Get(1, "amount").AsFloat, thisElement, customVariables));

                            return;
                        }

                    #endregion

                    #region SetEvent

                    case "SetEvent": {
                            if (parameters == null)
                                return;

                            var type = parameters.Get(0, "type");

                            if (type == null || !type.IsString)
                                return;

                            var values = parameters.Get(1, "values");

                            switch (type.Value)
                            {
                                case "MoveCamera": {
                                        var x = ParseVarFunction(values["x"], thisElement, customVariables);
                                        if (x != null)
                                            MenuEffectsManager.inst.MoveCameraX(x.AsFloat);

                                        var y = ParseVarFunction(values["x"], thisElement, customVariables);
                                        if (y != null)
                                            MenuEffectsManager.inst.MoveCameraX(y.AsFloat);

                                        return;
                                    }
                                case "ZoomCamera": {
                                        var amount = ParseVarFunction(values["amount"], thisElement, customVariables);
                                        if (amount != null)
                                            MenuEffectsManager.inst.ZoomCamera(amount.AsFloat);

                                        return;
                                    }
                                case "RotateCamera": {
                                        var amount = ParseVarFunction(values["amount"], thisElement, customVariables);
                                        if (amount != null)
                                            MenuEffectsManager.inst.RotateCamera(amount.AsFloat);

                                        return;
                                    }
                                case "Chroma":
                                case "Chromatic": {
                                        var intensity = ParseVarFunction(values["intensity"], thisElement, customVariables);
                                        if (intensity != null)
                                            MenuEffectsManager.inst.UpdateChroma(intensity.AsFloat);

                                        return;
                                    }
                                case "Bloom": {
                                        var intensity = ParseVarFunction(values["intensity"], thisElement, customVariables);
                                        if (intensity != null)
                                            MenuEffectsManager.inst.UpdateBloomIntensity(intensity.AsFloat);

                                        var diffusion = ParseVarFunction(values["diffusion"], thisElement, customVariables);
                                        if (diffusion != null)
                                            MenuEffectsManager.inst.UpdateBloomDiffusion(diffusion.AsFloat);

                                        var threshold = ParseVarFunction(values["threshold"], thisElement, customVariables);
                                        if (threshold != null)
                                            MenuEffectsManager.inst.UpdateBloomThreshold(threshold.AsFloat);

                                        var anamorphicRatio = ParseVarFunction(values["anamorphic_ratio"], thisElement, customVariables);
                                        if (anamorphicRatio != null)
                                            MenuEffectsManager.inst.UpdateBloomAnamorphicRatio(anamorphicRatio.AsFloat);

                                        var col = ParseVarFunction(values["col"], thisElement, customVariables);
                                        if (col != null)
                                            MenuEffectsManager.inst.UpdateBloomColor(col.IsString ? RTColors.HexToColor(col) : inst.CurrentInterface.Theme.GetFXColor(col.AsInt));

                                        return;
                                    }
                                case "Lens":
                                case "LensDistort": {
                                        var intensity = ParseVarFunction(values["intensity"], thisElement, customVariables);
                                        if (intensity != null)
                                            MenuEffectsManager.inst.UpdateLensDistortIntensity(intensity.AsFloat);

                                        var centerX = ParseVarFunction(values["center_x"], thisElement, customVariables);
                                        if (centerX != null)
                                            MenuEffectsManager.inst.UpdateLensDistortCenterX(centerX.AsFloat);

                                        var centerY = ParseVarFunction(values["center_y"], thisElement, customVariables);
                                        if (centerY != null)
                                            MenuEffectsManager.inst.UpdateLensDistortCenterY(centerY.AsFloat);

                                        var intensityX = ParseVarFunction(values["intensity_x"], thisElement, customVariables);
                                        if (intensityX != null)
                                            MenuEffectsManager.inst.UpdateLensDistortIntensityX(intensityX.AsFloat);

                                        var intensityY = ParseVarFunction(values["intensity_x"], thisElement, customVariables);
                                        if (intensityY != null)
                                            MenuEffectsManager.inst.UpdateLensDistortIntensityY(intensityY.AsFloat);

                                        var scale = ParseVarFunction(values["scale"], thisElement, customVariables);
                                        if (scale != null)
                                            MenuEffectsManager.inst.UpdateLensDistortScale(scale.AsFloat);

                                        return;
                                    }
                                case "Vignette": {
                                        var intensity = ParseVarFunction(values["intensity"], thisElement, customVariables);
                                        if (intensity != null)
                                            MenuEffectsManager.inst.UpdateVignetteIntensity(intensity.AsFloat);

                                        var centerX = ParseVarFunction(values["center_x"], thisElement, customVariables);
                                        if (centerX != null)
                                            MenuEffectsManager.inst.UpdateVignetteCenterX(centerX.AsFloat);

                                        var centerY = ParseVarFunction(values["center_y"], thisElement, customVariables);
                                        if (centerY != null)
                                            MenuEffectsManager.inst.UpdateVignetteCenterY(centerY.AsFloat);

                                        var smoothness = ParseVarFunction(values["smoothness"], thisElement, customVariables);
                                        if (smoothness != null)
                                            MenuEffectsManager.inst.UpdateVignetteSmoothness(smoothness.AsFloat);

                                        var roundness = ParseVarFunction(values["roundness"], thisElement, customVariables);
                                        if (roundness != null)
                                            MenuEffectsManager.inst.UpdateVignetteRoundness(roundness.AsFloat);

                                        var rounded = ParseVarFunction(values["rounded"], thisElement, customVariables);
                                        if (rounded != null)
                                            MenuEffectsManager.inst.UpdateVignetteRounded(rounded.AsBool);

                                        var col = ParseVarFunction(values["col"], thisElement, customVariables);
                                        if (col != null)
                                            MenuEffectsManager.inst.UpdateVignetteColor(col.IsString ? RTColors.HexToColor(col) : inst.CurrentInterface.Theme.GetFXColor(col.AsInt));

                                        return;
                                    }
                                case "AnalogGlitch": {
                                        var enabled = ParseVarFunction(values["enabled"], thisElement, customVariables);
                                        if (enabled != null)
                                            MenuEffectsManager.inst.UpdateAnalogGlitchEnabled(enabled.AsBool);

                                        var scanLineJitter = ParseVarFunction(values["scan_line_jitter"], thisElement, customVariables);
                                        if (scanLineJitter != null)
                                            MenuEffectsManager.inst.UpdateAnalogGlitchScanLineJitter(scanLineJitter.AsFloat);

                                        var verticalJump = ParseVarFunction(values["vertical_jump"], thisElement, customVariables);
                                        if (verticalJump != null)
                                            MenuEffectsManager.inst.UpdateAnalogGlitchVerticalJump(verticalJump.AsFloat);

                                        var horizontalShake = ParseVarFunction(values["horizontal_shake"], thisElement, customVariables);
                                        if (horizontalShake != null)
                                            MenuEffectsManager.inst.UpdateAnalogGlitchHorizontalShake(horizontalShake.AsFloat);

                                        var colorDrift = ParseVarFunction(values["color_drift"], thisElement, customVariables);
                                        if (colorDrift != null)
                                            MenuEffectsManager.inst.UpdateAnalogGlitchColorDrift(colorDrift.AsFloat);

                                        return;
                                    }
                                case "DigitalGlitch": {
                                        var intensity = ParseVarFunction(values["intensity"], thisElement, customVariables);
                                        if (intensity != null)
                                            MenuEffectsManager.inst.UpdateDigitalGlitch(intensity.AsFloat);

                                        return;
                                    }
                            }

                            return;
                        }

                    #endregion

                    #endregion

                    #region Levels

                    #region InitLevelMenu

                    // Initializes the level list menu from a specific path.
                    // Supports both JSON array and JSON object.
                    // 
                    // - JSON Array Structure -
                    // 0 = directory
                    // Example:
                    // [
                    //   "{{AppDirectory}}beatmaps/editor" < must contain levels with ".lsb" format.
                    // ]
                    // 
                    // - JSON Object Structure -
                    // "directory"
                    // Example:
                    // {
                    //   "directory": "" < if left empty, will use the interfaces' directory.
                    // }
                    case "InitLevelMenu": {
                            var directory = inst.MainDirectory;

                            if (parameters != null)
                            {
                                var directoryJN = parameters.Get(0, "directory");
                                if (directoryJN != null)
                                    directory = directoryJN;
                            }

                            if (string.IsNullOrEmpty(directory))
                                directory = inst.MainDirectory;

                            LevelListMenu.Init(Directory.GetDirectories(directory).Where(x => Level.Verify(x)).Select(x => new Level(RTFile.ReplaceSlash(x))).ToList());
                            return;
                        }

                    #endregion

                    #endregion

                    #region Online

                    #region ModderDiscord

                    // Opens the System Error Discord server link.
                    // Function has no parameters.
                    case "ModderDiscord": {
                            Application.OpenURL(AlephNetwork.MOD_DISCORD_URL);

                            return;
                        }

                    #endregion

                    #region SourceCode

                    // Opens the GitHub Source Code link.
                    // Function has no parameters.
                    case "SourceCode": {
                            Application.OpenURL(AlephNetwork.OPEN_SOURCE_URL);

                            return;
                        }

                    #endregion

                    #endregion

                    #region Specific

                    #region OpenChangelog

                    case "OpenChangelog": {
                            inst.OpenChangelog();
                            return;
                        }

                    #endregion

                    #region LoadLevels

                    case "LoadLevels": {
                            LoadLevelsMenu.Init(() =>
                            {
                                if (parameters["on_loading_end"] != null)
                                    ParseFunction(parameters["on_loading_end"], thisElement, customVariables);
                            });
                            return;
                        }

                    #endregion

                    #region OnInputsSelected

                    case "OnInputsSelected": {
                            InputSelectMenu.OnInputsSelected = () =>
                            {
                                if (parameters["continue"] != null)
                                    ParseFunction(parameters["continue"], thisElement, customVariables);
                            };
                            return;
                        }

                    #endregion

                    #region Profile

                    case "Profile": {
                            ProfileMenu.Init();

                            return;
                        }

                    #endregion

                    #region BeginStoryMode

                    // Begins the BetterLegacy story mode.
                    // Function has no parameters.
                    case "BeginStoryMode": {
                            LevelManager.IsArcade = false;
                            SceneHelper.LoadInputSelect(SceneHelper.LoadInterfaceScene);

                            return;
                        }

                    #endregion

                    #region LoadCurrentStoryInterface

                    case "LoadCurrentStoryInterface": {
                            inst.StartupStoryInterface();

                            return;
                        }

                    #endregion

                    #region LoadStoryInterface

                    case "LoadStoryInterface": {
                            inst.StartupStoryInterface(ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables).AsInt, ParseVarFunction(parameters.Get(1, "level"), thisElement, customVariables).AsInt);

                            return;
                        }

                    #endregion

                    #endregion
                }

                base.Function(jn, name, parameters, thisElement, customVariables);
            }

            public override JSONNode VarFunction(JSONNode jn, string name, JSONNode parameters, MenuImage thisElement = null, Dictionary<string, JSONNode> customVariables = null)
            {
                switch (name)
                {
                    #region ColorSource

                    // Parses a variable from a color source.
                    // Supports both JSON array and JSON object.
                    // 
                    // - JSON Array Structure -
                    // 0 = color source.
                    // 1 = index of the color slot. (optional)
                    // Example:
                    // [
                    //   "obj",
                    //   "2" < returns the object color slot at index 2
                    // ]
                    // 
                    // - JSON Object Structure -
                    // "source"
                    // "index" (optional)
                    // Example:
                    // {
                    //   "source": "bg" < index is optional
                    // }
                    case "ColorSource": {
                            var source = ParseVarFunction(parameters.Get(0, "source"), thisElement, customVariables).Value;
                            var index = ParseVarFunction(parameters.Get(1, "index"), thisElement, customVariables);
                            return (source switch
                            {
                                "gui_accent" => inst.CurrentTheme.guiAccentColor,
                                "bg" => inst.CurrentTheme.backgroundColor,
                                "player" => inst.CurrentTheme.playerColors.GetAt(index.AsInt),
                                "obj" => inst.CurrentTheme.objectColors.GetAt(index.AsInt),
                                "bgs" => inst.CurrentTheme.backgroundColors.GetAt(index.AsInt),
                                "fx" => inst.CurrentTheme.effectColors.GetAt(index.AsInt),
                                _ => inst.CurrentTheme.guiColor,
                            }).ToString();
                        }

                    #endregion
                }

                return base.VarFunction(jn, name, parameters, thisElement, customVariables);
            }
        }
    }
}
