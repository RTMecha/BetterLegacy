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
    public class InterfaceManager : MonoBehaviour
    {
        #region Init

        /// <summary>
        /// The global instance reference.
        /// </summary>
        public static InterfaceManager inst;

        /// <summary>
        /// Initializes <see cref="InterfaceManager"/>.
        /// </summary>
        public static void Init() => Creator.NewGameObject(nameof(InterfaceManager), SystemManager.inst.transform).AddComponent<InterfaceManager>();

        /// <summary>
        /// The main directory to load interfaces from. Must end with a slash.
        /// </summary>
        public string MainDirectory { get; set; }

        void Awake()
        {
            inst = this;

            CurrentAudioSource = gameObject.AddComponent<AudioSource>();
            CurrentAudioSource.loop = true;
            MainDirectory = RTFile.ApplicationDirectory + "beatmaps/interfaces/";
            RTFile.CreateDirectory(MainDirectory);
        }

        void Update()
        {
            if (CurrentAudioSource.isPlaying)
                CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);
            else
                AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

            CurrentAudioSource.volume = DataManager.inst.GetSettingInt("MusicVolume", 9) / 9f * AudioManager.inst.masterVol;

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
            DownloadLevelMenu.Current = null;
            SteamLevelMenu.Current = null;
            ProgressMenu.Current = null;
            LevelCollectionMenu.Current = null;
            LevelListMenu.Current = null;
            InputSelectMenu.Current = null;
            LoadLevelsMenu.Current = null;
            ControllerDisconnectedMenu.Current = null;

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

            ParseInterface(RTFile.GetAsset($"Interfaces/main_menu{FileFormat.LSI.Dot()}"));

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
                {
                    var changeLogMenu = new ChangeLogMenu();

                    changeLogMenu.layouts.Add("updates", new MenuVerticalLayout
                    {
                        name = "updates",
                        childControlWidth = true,
                        childForceExpandWidth = true,
                        spacing = 4f,
                        rect = RectValues.FullAnchored.AnchoredPosition(0f, -32f).SizeDelta(-64f, -256f),
                    });

                    changeLogMenu.elements.Add(new MenuText
                    {
                        id = "1",
                        name = "Title",
                        text = "<size=60><b>BetterLegacy Changelog",
                        rect = RectValues.Default.AnchoredPosition(-640f, 440f).SizeDelta(400f, 64f),
                        icon = LegacyPlugin.PALogoSprite,
                        iconRect = RectValues.Default.AnchoredPosition(-256f, 0f).SizeDelta(64f, 64f),
                        hideBG = true,
                        textColor = 6
                    });

                    var lines = RTString.GetLines(file);
                    RTString.GetLines(file).ForLoop(changeLogMenu.AddUpdateNote);

                    changeLogMenu.elements.Add(new MenuButton
                    {
                        id = "0",
                        name = "Next Menu Button",
                        text = "<b><align=center>[ NEXT ]",
                        rect = RectValues.Default.AnchoredPosition(0f, -400f).SizeDelta(300f, 64f),
                        func = () => SetCurrentInterface(MAIN_MENU_ID),
                        opacity = 0.1f,
                        selectedOpacity = 1f,
                        color = 6,
                        selectedColor = 6,
                        textColor = 6,
                        selectedTextColor = 7,
                        length = 1f,
                    });

                    SetCurrentInterface(changeLogMenu);
                    PlayMusic();
                    AudioManager.inst.SetPitch(1f);

                    ChangeLogMenu.Seen = true;
                }
                else
                {
                    CoreHelper.LogError($"Couldn't read changelog file, continuing...");
                    SetCurrentInterface(MAIN_MENU_ID);
                    PlayMusic();
                    AudioManager.inst.SetPitch(1f);
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
            var jn = JSON.Parse(RTFile.ReadFromFile(RTFile.GetAsset($"Interfaces/default_themes{FileFormat.LST.Dot()}")));
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
            return ParseText(input);
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
        /// Parses text.
        /// </summary>
        /// <param name="input">Input string.</param>
        /// <returns>Returns parsed text.</returns>
        public string ParseText(string input, Dictionary<string, JSONNode> customVariables = null) => RTString.ParseText(input, customVariables);

        /// <summary>
        /// Parses an "if_func" JSON and returns the result. Supports both JSON Object and JSON Array.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <param name="thisElement">Interface element reference.</param>
        /// <param name="customVariables">Passed custom variables.</param>
        /// <returns>Returns true if the passed JSON functions is true, otherwise false.</returns>
        public bool ParseIfFunction(JSONNode jn, MenuImage thisElement = null, Dictionary<string, JSONNode> customVariables = null)
        {
            if (jn == null)
                return true;

            if (jn.IsObject || jn.IsString)
                return ParseIfFunctionSingle(jn, thisElement, customVariables);

            bool result = true;

            if (jn.IsArray)
            {
                for (int i = 0; i < jn.Count; i++)
                {
                    var checkJN = jn[i];
                    var value = ParseIfFunction(checkJN, thisElement, customVariables);

                    // if json is array then count it as an else if statement
                    var elseIf = checkJN.IsArray || checkJN["otherwise"].AsBool;

                    if (elseIf && !result && value)
                        result = true;

                    if (!elseIf && !value)
                        result = false;
                }
            }

            return result;
        }

        /// <summary>
        /// Parses a singular "if_func" JSON.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <param name="thisElement">Interface element reference.</param>
        /// <param name="customVariables">Passed custom variables.</param>
        /// <returns>Returns true if the passed JSON function is true, otherwise false.</returns>
        public bool ParseIfFunctionSingle(JSONNode jn, MenuImage thisElement = null, Dictionary<string, JSONNode> customVariables = null)
        {
            if (jn == null)
                return false;

            var jnFunc = ParseVarFunction(jn["func"], thisElement, customVariables);
            if (jnFunc != null)
                ParseFunction(jnFunc, thisElement, customVariables);

            var parameters = jn["params"];
            string name = jn.IsString ? jn : jn["name"];
            var not = !jn.IsString && jn["not"].AsBool; // If true, then check if the function is not true.

            if (string.IsNullOrEmpty(name))
                return false;

            // parse ! operator
            while (name.StartsWith("!"))
            {
                name = name.Substring(1, name.Length - 1);
                not = !not;
            }

            try
            {
                switch (name)
                {
                    #region Main

                    case "True": return true;
                    case "False": return false;
                    case "GetSettingBool": {
                            if (parameters == null)
                                break;

                            var value = DataManager.inst.GetSettingBool(ParseVarFunction(parameters.Get(0, "setting"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables).AsBool);
                            return !not ? value : !value;
                        }
                    case "GetSettingIntEquals": {
                            if (parameters == null)
                                break;

                            var value = DataManager.inst.GetSettingInt(ParseVarFunction(parameters.Get(0, "setting"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables).AsInt) == ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "GetSettingIntLesserEquals": {
                            if (parameters == null)
                                break;

                            var value = DataManager.inst.GetSettingInt(ParseVarFunction(parameters.Get(0, "setting"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables).AsInt) <= ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "GetSettingIntGreaterEquals": {
                            if (parameters == null)
                                break;

                            var value = DataManager.inst.GetSettingInt(ParseVarFunction(parameters.Get(0, "setting"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables).AsInt) >= ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "GetSettingIntLesser": {
                            if (parameters == null)
                                break;

                            var value = DataManager.inst.GetSettingInt(ParseVarFunction(parameters.Get(0, "setting"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables).AsInt) < ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "GetSettingIntGreater": {
                            if (parameters == null)
                                break;

                            var value = DataManager.inst.GetSettingInt(ParseVarFunction(parameters.Get(0, "setting"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables).AsInt) > ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "IsScene": {
                            if (parameters == null)
                                break;

                            var value = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == ParseVarFunction(parameters.Get(0, "scene"), thisElement, customVariables);
                            return !not ? value : !value;
                        }

                    case "CurrentInterfaceGenerating": {
                            var value = CurrentInterface && CurrentInterface.generating;
                            return !not ? value : !value;
                        }

                    #endregion

                    #region Interface List

                    case "LIST_ContainsInterface": {
                            if (!CurrentInterfaceList || parameters == null)
                                break;

                            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables);
                            if (id == null)
                                break;

                            CurrentInterfaceList.Contains(id);
                            break;
                        }

                    #endregion

                    #region Player

                    case "PlayerCountEquals": {
                            if (parameters == null)
                                break;

                            var value = InputDataManager.inst.players.Count == ParseVarFunction(parameters.Get(0, "count"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "PlayerCountLesserEquals": {
                            if (parameters == null)
                                break;

                            var value = InputDataManager.inst.players.Count <= ParseVarFunction(parameters.Get(0, "count"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "PlayerCountGreaterEquals": {
                            if (parameters == null)
                                break;

                            var value = InputDataManager.inst.players.Count >= ParseVarFunction(parameters.Get(0, "count"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "PlayerCountLesser": {
                            if (parameters == null)
                                break;

                            var value = InputDataManager.inst.players.Count < ParseVarFunction(parameters.Get(0, "count"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "PlayerCountGreater": {
                            if (parameters == null)
                                break;

                            var value = InputDataManager.inst.players.Count > ParseVarFunction(parameters.Get(0, "count"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }

                    #endregion

                    #region Profile
                        
                    case "DisplayNameEquals": {
                            if (parameters == null)
                                break;

                            var value = CoreConfig.Instance.DisplayName.Value == ParseVarFunction(parameters.Get(0, "user"), thisElement, customVariables).Value;
                            return !not ? value : !value;
                        }
                        
                    case "ProfileLoadIntEquals": {
                            if (parameters == null || !LegacyPlugin.player || LegacyPlugin.player.memory == null)
                                break;

                            var varName = ParseVarFunction(parameters.Get(0, "var_name"));
                            if (varName == null || !varName.IsString)
                                break;

                            var profileValue = LegacyPlugin.player.memory[ParseVarFunction(parameters.Get(0, "var_name"), thisElement, customVariables).Value];
                            if (profileValue == null)
                                profileValue = ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables);

                            var value = profileValue.AsInt == ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "ProfileLoadIntLesserEquals": {
                            if (parameters == null || !LegacyPlugin.player || LegacyPlugin.player.memory == null)
                                break;

                            var varName = ParseVarFunction(parameters.Get(0, "var_name"));
                            if (varName == null || !varName.IsString)
                                break;

                            var profileValue = LegacyPlugin.player.memory[ParseVarFunction(parameters.Get(0, "var_name"), thisElement, customVariables).Value];
                            if (profileValue == null)
                                profileValue = ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables);

                            var value = profileValue.AsInt <= ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "ProfileLoadIntGreaterEquals": {
                            if (parameters == null || !LegacyPlugin.player || LegacyPlugin.player.memory == null)
                                break;

                            var varName = ParseVarFunction(parameters.Get(0, "var_name"));
                            if (varName == null || !varName.IsString)
                                break;

                            var profileValue = LegacyPlugin.player.memory[ParseVarFunction(parameters.Get(0, "var_name"), thisElement, customVariables).Value];
                            if (profileValue == null)
                                profileValue = ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables);

                            var value = profileValue.AsInt >= ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "ProfileLoadIntLesser": {
                            if (parameters == null || !LegacyPlugin.player || LegacyPlugin.player.memory == null)
                                break;

                            var varName = ParseVarFunction(parameters.Get(0, "var_name"));
                            if (varName == null || !varName.IsString)
                                break;

                            var profileValue = LegacyPlugin.player.memory[ParseVarFunction(parameters.Get(0, "var_name"), thisElement, customVariables).Value];
                            if (profileValue == null)
                                profileValue = ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables);

                            var value = profileValue.AsInt < ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "ProfileLoadIntGreater": {
                            if (parameters == null || !LegacyPlugin.player || LegacyPlugin.player.memory == null)
                                break;

                            var varName = ParseVarFunction(parameters.Get(0, "var_name"));
                            if (varName == null || !varName.IsString)
                                break;

                            var profileValue = LegacyPlugin.player.memory[ParseVarFunction(parameters.Get(0, "var_name"), thisElement, customVariables).Value];
                            if (profileValue == null)
                                profileValue = ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables);

                            var value = profileValue.AsInt > ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "ProfileLoadBool": {
                            if (parameters == null)
                                break;

                            var value = LegacyPlugin.player && LegacyPlugin.player.memory != null && LegacyPlugin.player.memory[ParseVarFunction(parameters.Get(0, "var_name"), thisElement, customVariables).Value].AsBool;
                            return !not ? value : !value;
                        }

                    #endregion

                    #region Story Chapter

                    case "StoryChapterEquals": {
                            if (parameters == null)
                                break;

                            var value = StoryManager.inst.CurrentSave.LoadInt("Chapter", 0) == ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "StoryChapterLesserEquals": {
                            if (parameters == null)
                                break;

                            var value = StoryManager.inst.CurrentSave.LoadInt("Chapter", 0) <= ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "StoryChapterGreaterEquals": {
                            if (parameters == null)
                                break;

                            var value = StoryManager.inst.CurrentSave.LoadInt("Chapter", 0) >= ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "StoryChapterLesser": {
                            if (parameters == null)
                                break;

                            var value = StoryManager.inst.CurrentSave.LoadInt("Chapter", 0) < ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "StoryChapterGreater": {
                            if (parameters == null)
                                break;

                            var value = StoryManager.inst.CurrentSave.LoadInt("Chapter", 0) > ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "StoryInstalled": {
                            var value = StoryManager.inst && RTFile.DirectoryExists(StoryManager.StoryAssetsPath);
                            return !not ? value : !value;
                        }
                    case "StoryLoadIntEquals": {
                            if (parameters == null)
                                break;

                            var value = StoryManager.inst.CurrentSave.LoadInt(ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default")).AsInt) == ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "StoryLoadIntLesserEquals": {
                            if (parameters == null)
                                break;

                            var value = StoryManager.inst.CurrentSave.LoadInt(ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default")).AsInt) <= ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "StoryLoadIntGreaterEquals": {
                            if (parameters == null)
                                break;

                            var value = StoryManager.inst.CurrentSave.LoadInt(ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default")).AsInt) >= ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "StoryLoadIntLesser": {
                            if (parameters == null)
                                break;

                            var value = StoryManager.inst.CurrentSave.LoadInt(ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default")).AsInt) < ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "StoryLoadIntGreater": {
                            if (parameters == null)
                                break;

                            var value = StoryManager.inst.CurrentSave.LoadInt(ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default")).AsInt) > ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "StoryLoadBool": {
                            if (parameters == null)
                                break;

                            var value = StoryManager.inst.CurrentSave.LoadBool(ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables).AsBool);
                            return !not ? value : !value;
                        }

                    #endregion

                    #region Layout

                    case "LayoutChildCountEquals": {
                            if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                break;

                            var isArray = parameters.IsArray;

                            var value = menuLayout.content.childCount == (isArray ? parameters[1].AsInt : parameters["count"].AsInt);
                            return !not ? value : !value;
                        }
                    case "LayoutChildCountLesserEquals": {
                            if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                break;

                            var isArray = parameters.IsArray;

                            var value = menuLayout.content.childCount <= (isArray ? parameters[1].AsInt : parameters["count"].AsInt);
                            return !not ? value : !value;
                        }
                    case "LayoutChildCountGreaterEquals": {
                            if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                break;

                            var isArray = parameters.IsArray;

                            var value = menuLayout.content.childCount >= (isArray ? parameters[1].AsInt : parameters["count"].AsInt);
                            return !not ? value : !value;
                        }
                    case "LayoutChildCountLesser": {
                            if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                break;

                            var isArray = parameters.IsArray;

                            var value = menuLayout.content.childCount < (isArray ? parameters[1].AsInt : parameters["count"].AsInt);
                            return !not ? value : !value;
                        }
                    case "LayoutChildCountGreater": {
                            if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                break;

                            var isArray = parameters.IsArray;

                            var value = menuLayout.content.childCount > (isArray ? parameters[1].AsInt : parameters["count"].AsInt);
                            return !not ? value : !value;
                        }

                    case "LayoutScrollXEquals": {
                            if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                break;

                            var isArray = parameters.IsArray;

                            var value = menuLayout.content.anchoredPosition.x == (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                            return !not ? value : !value;
                        }
                    case "LayoutScrollXLesserEquals": {
                            if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                break;

                            var isArray = parameters.IsArray;

                            var value = menuLayout.content.anchoredPosition.x <= (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                            return !not ? value : !value;
                        }
                    case "LayoutScrollXGreaterEquals": {
                            if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                break;

                            var isArray = parameters.IsArray;

                            var value = menuLayout.content.anchoredPosition.x >= (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                            return !not ? value : !value;
                        }
                    case "LayoutScrollXLesser": {
                            if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                break;

                            var isArray = parameters.IsArray;

                            var value = menuLayout.content.anchoredPosition.x < (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                            return !not ? value : !value;
                        }
                    case "LayoutScrollXGreater": {
                            if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                break;

                            var isArray = parameters.IsArray;

                            var value = menuLayout.content.anchoredPosition.x > (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                            return !not ? value : !value;
                        }

                    case "LayoutScrollYEquals": {
                            if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                break;

                            var isArray = parameters.IsArray;

                            var value = menuLayout.content.anchoredPosition.y == (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                            return !not ? value : !value;
                        }
                    case "LayoutScrollYLesserEquals": {
                            if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                break;

                            var isArray = parameters.IsArray;

                            var value = menuLayout.content.anchoredPosition.y <= (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                            return !not ? value : !value;
                        }
                    case "LayoutScrollYGreaterEquals": {
                            if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                break;

                            var isArray = parameters.IsArray;

                            var value = menuLayout.content.anchoredPosition.y >= (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                            return !not ? value : !value;
                        }
                    case "LayoutScrollYLesser": {
                            if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                break;

                            var isArray = parameters.IsArray;

                            var value = menuLayout.content.anchoredPosition.y < (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                            return !not ? value : !value;
                        }
                    case "LayoutScrollYGreater": {
                            if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                                break;

                            var isArray = parameters.IsArray;

                            var value = menuLayout.content.anchoredPosition.y > (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                            return !not ? value : !value;
                        }

                    #endregion

                    #region LevelRanks

                    case "ChapterFullyRanked": {
                            if (parameters == null)
                                break;

                            var chapter = ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables).AsInt;
                            var minRank = ParseVarFunction(parameters.GetOrDefault(1, "min_rank", StoryManager.CHAPTER_RANK_REQUIREMENT)).AsInt;
                            var maxRank = ParseVarFunction(parameters.GetOrDefault(2, "max_rank", 1)).AsInt;
                            var bonus = ParseVarFunction(parameters.Get(3, "bonus"), thisElement, customVariables).AsBool;

                            var levelIDs = bonus ? StoryMode.Instance.bonusChapters : StoryMode.Instance.chapters;

                            var value =
                                chapter < levelIDs.Count &&
                                levelIDs[chapter].levels.All(x => x.bonus ||
                                                StoryManager.inst.CurrentSave.Saves.TryFind(y => y.ID == x.id, out SaveData playerData) &&
                                                LevelManager.GetLevelRank(playerData) >= maxRank && LevelManager.GetLevelRank(playerData) <= minRank);

                            return !not ? value : !value;
                        }
                    case "LevelRankEquals": {
                            if (parameters == null)
                                break;

                            var value = LevelManager.CurrentLevel.saveData && LevelManager.GetLevelRank(LevelManager.CurrentLevel) == ParseVarFunction(parameters.Get(0, "rank"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "LevelRankLesserEquals": {
                            if (parameters == null)
                                break;

                            var value = LevelManager.CurrentLevel.saveData && LevelManager.GetLevelRank(LevelManager.CurrentLevel) <= ParseVarFunction(parameters.Get(0, "rank"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "LevelRankGreaterEquals": {
                            if (parameters == null)
                                break;

                            var value = LevelManager.CurrentLevel.saveData && LevelManager.GetLevelRank(LevelManager.CurrentLevel) >= ParseVarFunction(parameters.Get(0, "rank"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "LevelRankLesser": {
                            if (parameters == null)
                                break;

                            var value = LevelManager.CurrentLevel.saveData && LevelManager.GetLevelRank(LevelManager.CurrentLevel) < ParseVarFunction(parameters.Get(0, "rank"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "LevelRankGreater": {
                            if (parameters == null)
                                break;

                            var value = LevelManager.CurrentLevel.saveData && LevelManager.GetLevelRank(LevelManager.CurrentLevel) > ParseVarFunction(parameters.Get(0, "rank"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "StoryLevelRankEquals": {
                            if (parameters == null)
                                break;

                            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables).Value;

                            var value = StoryManager.inst.CurrentSave.Saves.TryFind(x => x.ID == id, out SaveData playerData) && LevelManager.GetLevelRank(LevelManager.CurrentLevel) == ParseVarFunction(parameters.Get(1, "rank"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "StoryLevelRankLesserEquals": {
                            if (parameters == null)
                                break;

                            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables).Value;

                            var value = StoryManager.inst.CurrentSave.Saves.TryFind(x => x.ID == id, out SaveData playerData) && LevelManager.GetLevelRank(LevelManager.CurrentLevel) <= ParseVarFunction(parameters.Get(1, "rank"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "StoryLevelRankGreaterEquals": {
                            if (parameters == null)
                                break;

                            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables).Value;

                            var value = StoryManager.inst.CurrentSave.Saves.TryFind(x => x.ID == id, out SaveData playerData) && LevelManager.GetLevelRank(LevelManager.CurrentLevel) >= ParseVarFunction(parameters.Get(1, "rank"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "StoryLevelRankLesser": {
                            if (parameters == null)
                                break;

                            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables).Value;

                            var value = StoryManager.inst.CurrentSave.Saves.TryFind(x => x.ID == id, out SaveData playerData) && LevelManager.GetLevelRank(LevelManager.CurrentLevel) < ParseVarFunction(parameters.Get(1, "rank"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }
                    case "StoryLevelRankGreater": {
                            if (parameters == null)
                                break;

                            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables).Value;

                            var value = StoryManager.inst.CurrentSave.Saves.TryFind(x => x.ID == id, out SaveData playerData) && LevelManager.GetLevelRank(LevelManager.CurrentLevel) > ParseVarFunction(parameters.Get(1, "rank"), thisElement, customVariables).AsInt;
                            return !not ? value : !value;
                        }

                     #endregion
                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Had an error with parsing {jn}!\nException: {ex}");
            }

            return false;
        }

        /// <summary>
        /// Parses an entire func JSON. Supports both JSON Object and JSON Array.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <param name="thisElement">Interface element reference.</param>
        /// <param name="customVariables">Passed custom variables.</param>
        public void ParseFunction(JSONNode jn, MenuImage thisElement = null, Dictionary<string, JSONNode> customVariables = null)
        {
            // allow multiple functions to occur.
            if (jn.IsArray)
            {
                for (int i = 0; i < jn.Count; i++)
                    ParseFunction(ParseVarFunction(jn[i], thisElement, customVariables), thisElement, customVariables);

                return;
            }

            ParseFunctionSingle(jn, thisElement, customVariables);
        }

        /// <summary>
        /// Parses a singular "func" JSON and performs an action based on the name and parameters.
        /// </summary>
        /// <param name="jn">The func JSON. Must have a name and a params array, but can be a string if the function has no parameters. If it has a "if_func", then it will parse and check if it's true.</param>
        /// <param name="thisElement">Interface element reference.</param>
        /// <param name="customVariables">Passed custom variables.</param>
        public void ParseFunctionSingle(JSONNode jn, MenuImage thisElement = null, Dictionary<string, JSONNode> customVariables = null)
        {
            var jnIfFunc = ParseVarFunction(jn["if_func"], thisElement, customVariables);
            if (jnIfFunc != null)
            {
                if (!ParseIfFunction(jnIfFunc, thisElement, customVariables))
                    return;
            }

            var parameters = jn["params"];
            string name = jn.IsString ? jn : jn["name"];

            switch (name)
            {
                #region Main

                #region LoadScene

                // Loads a Unity scene.
                // Supports both JSON array and JSON object.
                // Scenes:
                // - Main Menu
                // - Input Select
                // - Game
                // - Editor
                // - Interface
                // - post_level
                // - Arcade Select
                // 
                // - JSON Array Structure -
                // 0 = scene to load
                // 1 = show loading screen
                // 2 = set is arcade (used if the scene you're going to is input select, and afterwards you want to travel to the arcade scene.
                // Example:
                // [
                //   "Input Select",
                //   "False",
                //   "True" // true, so after all inputs have been assigned and user continues, go to arcade scene.
                // ]
                // 
                // - JSON Object Structure -
                // "scene"
                // "show_loading"
                // "is_arcade"
                // Example:
                // {
                //   "scene": "Input Select",
                //   "show_loading": "True",
                //   "is_arcade": "False" < if is_arcade is null or is false, load story mode after Input Select.
                // }
                case "LoadScene": {
                        if (parameters == null)
                            break;

                        var sceneName = ParseVarFunction(parameters.Get(0, "scene"), thisElement, customVariables);
                        if (sceneName == null || !sceneName.IsString)
                            break;

                        LevelManager.IsArcade = ParseVarFunction(parameters.Get(2, "is_arcade"), thisElement, customVariables);
                        var showLoading = ParseVarFunction(parameters.Get(1, "show_loading"), thisElement, customVariables);

                        if (showLoading != null)
                            SceneManager.inst.LoadScene(sceneName, Parser.TryParse(showLoading, true));
                        else
                            SceneManager.inst.LoadScene(sceneName);

                        break;
                    }

                #endregion

                #region UpdateSettingBool

                // Updates or adds a global setting of boolean true / false type.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = setting name
                // 1 = value
                // 2 = relative
                // Example:
                // [
                //   "IsArcade",
                //   "False",
                //   "True" < swaps the bool value instead of setting it.
                // ]
                // 
                // - JSON Object Structure -
                // "setting"
                // "value"
                // "relative"
                // Example:
                // {
                //   "setting": "IsArcade",
                //   "value": "True",
                //   "relative": "False"
                // }
                case "UpdateSettingBool": {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["setting"] == null || parameters["value"] == null))
                            break;

                        var isArray = parameters.IsArray;
                        var settingName = isArray ? parameters[0] : parameters["setting"];
                        DataManager.inst.UpdateSettingBool(settingName,
                            isArray && parameters.Count > 2 && parameters[2].AsBool || parameters.IsObject && parameters["relative"] != null && parameters["relative"].AsBool ? !DataManager.inst.GetSettingBool(settingName) : isArray ? parameters[1].AsBool : parameters["value"].AsBool);

                        break;
                    }

                #endregion

                #region UpdateSettingInt

                // Updates or adds a global setting of integer type.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = setting name
                // 1 = value
                // 2 = relative
                // Example:
                // [
                //   "SomeKindOfNumber",
                //   "False",
                //   "True" < adds onto the current numbers' value instead of just setting it.
                // ]
                // 
                // - JSON Object Structure -
                // "setting"
                // "value"
                // "relative"
                // Example:
                // {
                //   "setting": "SomeKindOfNumber",
                //   "value": "True",
                //   "relative": "False"
                // }
                case "UpdateSettingInt": {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["setting"] == null || parameters["value"] == null))
                            break;

                        var isArray = parameters.IsArray;
                        var settingName = isArray ? parameters[0] : parameters["setting"];

                        DataManager.inst.UpdateSettingInt(settingName,
                            (isArray ? parameters[1].AsInt : parameters["value"].AsInt) +
                            (isArray && parameters.Count > 2 && parameters[2].AsBool ||
                            parameters.IsObject && parameters["relative"] != null && parameters["relative"].AsBool ?
                                    DataManager.inst.GetSettingInt(settingName) : 0));

                        break;
                    }

                #endregion

                #region Wait

                // Waits a set amount of seconds and runs a function.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = time
                // 1 = function
                // Example:
                // [
                //   "1", < waits 1 second
                //   {
                //     "name": "PlaySound", < runs PlaySound func after 1 second.
                //     "params": [ "blip" ]
                //   }
                // ]
                // 
                // - JSON Object Structure -
                // "t"
                // "func"
                // Example:
                // {
                //   "t": "1",
                //   "func": {
                //     "name": "PlaySound",
                //     "params": {
                //       "sound": "blip"
                //     }
                //   }
                // }
                case "Wait": {
                        if (parameters == null)
                            break;

                        var t = ParseVarFunction(parameters.Get(0, "t"), thisElement, customVariables);
                        if (t == null)
                            break;

                        var func = ParseVarFunction(parameters.Get(1, "func"), thisElement, customVariables);
                        if (func == null)
                            break;

                        CoroutineHelper.PerformActionAfterSeconds(t, () =>
                        {
                            try
                            {
                                ParseFunction(func);
                            }
                            catch (Exception ex)
                            {
                                CoreHelper.LogError($"Had an exception with Wait function.\nException: {ex}");
                            }
                        });

                        break;
                    }

                #endregion

                #region Log

                // Sends a log. Can be viewed via the BepInEx console, UnityExplorer console (if Unity logging is on) or the Player.log file. Message will include the BetterLegacy class name.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = message
                // Example:
                // [
                //   "Hello world!"
                // ]
                // 
                // - JSON Object Structure -
                // "msg"
                // Example:
                // {
                //   "msg": "Hello world!"
                // }
                case "Log": {
                        if (parameters == null)
                            break;

                        var msg = ParseVarFunction(parameters.Get(0, "msg"), thisElement, customVariables);
                        if (msg == null)
                            break;
                        
                        CoreHelper.Log(msg);

                        break;
                    }

                #endregion

                #region Notify

                // Sends a notification.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = message
                // Example:
                // [
                //   "Hello world!",
                //   "FFFFFF",
                //   "20"
                // ]
                // 
                // - JSON Object Structure -
                // "msg"
                // "col"
                // "size"
                // Example:
                // {
                //   "msg": "Hello world!",
                //   "col": "000000",
                //   "size": "40"
                // }
                case "Notify": {
                        if (parameters == null)
                            break;

                        var color = CurrentTheme.guiColor;
                        var jnColor = ParseVarFunction(parameters.Get(1, "col"), thisElement, customVariables);
                        if (jnColor != null)
                            color = RTColors.HexToColor(jnColor);

                        var fontSize = 30;
                        var jnFontSize = ParseVarFunction(parameters.Get(2, "size"), thisElement, customVariables);
                        if (jnFontSize != null)
                            fontSize = jnFontSize.AsInt;

                        CoreHelper.Notify(ParseVarFunction(parameters.Get(0, "msg"), thisElement, customVariables), color, fontSize);
                        break;
                    }

                #endregion

                #region ExitGame

                // Exits the game.
                // Function has no parameters.
                case "ExitGame": {
                        Application.Quit();
                        break;
                    }

                #endregion

                #region Config

                // Opens the Config Manager UI.
                // Function has no parameters.
                case "Config": {
                        ConfigManager.inst.Show();
                        break;
                    }

                #endregion

                #region ForLoop

                case "ForLoop": {
                        if (parameters == null)
                            break;

                        var varName = ParseVarFunction(parameters.Get(3, "var_name"), thisElement, customVariables);
                        if (varName == null || !varName.IsString || varName == string.Empty)
                            varName = "index";
                        var index = ParseVarFunction(parameters.Get(0, "index"), thisElement, customVariables).AsInt;
                        var count = ParseVarFunction(parameters.Get(1, "count"), thisElement, customVariables).AsInt;
                        var func = ParseVarFunction(parameters.Get(2, "func"), thisElement, customVariables);
                        if (func == null)
                            break;

                        index = RTMath.Clamp(index, 0, count - 1);

                        for (int j = index; j < count; j++)
                        {
                            var loopVar = new Dictionary<string, JSONNode>();
                            if (customVariables != null)
                            {
                                foreach (var keyValuePair in customVariables)
                                    loopVar[keyValuePair.Key] = keyValuePair.Value;
                            }

                            loopVar[varName.Value] = j;

                            ParseFunction(func, thisElement, loopVar);
                        }

                        break;
                    }

                #endregion

                #region CacheVariable

                case "CacheVariable": {
                        if (parameters == null)
                            break;

                        var varName = ParseVarFunction(parameters.Get(0, "var_name"), thisElement, customVariables);
                        if (varName == null || !varName.IsString)
                            break;

                        var value = ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables);
                        if (value == null)
                            break;

                        customVariables[varName] = value;

                        break;
                    }

                #endregion

                #endregion

                #region Interface

                #region Close

                // Closes the interface and returns to the game (if user is in the Game scene).
                // Function has no parameters.
                case "Close": {
                        string id = CurrentInterface?.id;
                        CloseMenus();
                        StopMusic();

                        RTBeatmap.Current?.Resume();

                        if (CoreHelper.InGame)
                            interfaces.RemoveAll(x => x.id == id);

                        break;
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
                            break;

                        var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables);
                        if (id == null || !interfaces.TryFind(x => x.id == id, out MenuBase menu))
                            break;

                        SetCurrentInterface(menu);
                        PlayMusic();

                        break;
                    }

                #endregion

                #region Reload

                // Reloads the interface and sets it to the main menu. Only recommended if you want to return to the main menu and unload every other interface.
                // Function has no parameters.
                case "Reload": {
                        LegacyPlugin.ParseProfile();
                        LegacyPlugin.LoadSplashText();
                        ChangeLogMenu.Seen = false;
                        randomIndex = -1;
                        StartupInterface();

                        break;
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
                // 3 = branch ID to load if the interface that's being loaded is a list type
                // Example:
                // [
                //   "story_mode",
                //   "True",
                //   "{{BepInExAssetsDirectory}}Interfaces",
                //   "643284542742"
                // ]
                //
                // - JSON Object Structure -
                // "file"
                // "load"
                // "path"
                // "id"
                // Example:
                // {
                //   "file": "some_interface",
                //   "load": "False",
                //   "path": "beatmaps/interfaces", < (optional)
                //   "id": "5325263" < ID of the interface to open if the file is a list
                // }
                case "Parse": {
                        if (parameters == null)
                            break;

                        var file = ParseVarFunction(parameters.Get(0, "file"), thisElement, customVariables);
                        if (file == null)
                            break;

                        var mainDirectory = ParseVarFunction(parameters.Get(2, "path"));
                        if (mainDirectory != null)
                            MainDirectory = mainDirectory;

                        if (!MainDirectory.Contains(RTFile.ApplicationDirectory))
                            MainDirectory = RTFile.CombinePaths(RTFile.ApplicationDirectory, MainDirectory);

                        var path = RTFile.CombinePaths(MainDirectory, file + FileFormat.LSI.Dot());

                        if (!RTFile.FileExists(path))
                        {
                            CoreHelper.LogError($"Interface {file} does not exist!");

                            break;
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

                        ParseInterface(path, ParseVarFunction(parameters.Get(1, "load"), thisElement, customVariables), ParseVarFunction(parameters.Get(3, "id"), thisElement, customVariables), branchChain);

                        break;
                    }

                #endregion

                #region ClearInterfaces

                // Clears all interfaces from the interfaces list.
                // Function has no parameters.
                case "ClearInterfaces": {
                        interfaces.Clear();
                        break;
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
                            break;

                        var path = ParseVarFunction(parameters.Get(0, "path"), thisElement, customVariables);
                        if (path == null)
                            break;

                        MainDirectory = path;

                        if (!MainDirectory.Contains(RTFile.ApplicationDirectory))
                            MainDirectory = RTFile.CombinePaths(RTFile.ApplicationDirectory, MainDirectory);

                        break;
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
                            break;

                        var currentMessage = ParseVarFunction(parameters.Get(0, "msg"), thisElement, customVariables);
                        if (currentMessage == null)
                            break;

                        var confirmFunc = ParseVarFunction(parameters.Get(1, "confirm_func"), thisElement, customVariables);
                        if (confirmFunc == null)
                            break;

                        var cancelFunc = ParseVarFunction(parameters.Get(2, "cancel_func"), thisElement, customVariables);
                        if (cancelFunc == null)
                            break;

                        ConfirmMenu.Init(currentMessage, () => ParseFunction(confirmFunc, thisElement, customVariables), () => ParseFunction(cancelFunc, thisElement, customVariables));

                        break;
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
                            break;

                        if (CurrentInterface is not CustomMenu customMenu)
                            break;

                        customMenu.useGameTheme = parameters.Get(1, "game_theme");

                        var theme = ParseVarFunction(parameters.Get(0, "theme"), thisElement, customVariables);
                        if (theme == null)
                            break;

                        customMenu.loadedTheme = BeatmapTheme.Parse(theme);

                        break;
                    }

                #endregion

                #endregion

                #region Interface List

                case "LIST_OpenDefaultInterface": {
                        CurrentInterfaceList?.OpenDefaultInterface();
                        break;
                    }
                case "LIST_ExitInterface": {
                        CurrentInterfaceList?.ExitInterface();
                        break;
                    }
                case "LIST_SetCurrentInterface": {
                        if (parameters == null)
                            break;

                        var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables);
                        if (id == null)
                            break;

                        CurrentInterfaceList?.SetCurrentInterface(id);
                        break;
                    }
                case "LIST_AddInterface": {
                        if (parameters == null)
                            break;

                        var interfaces = ParseVarFunction(parameters.Get(0, "interfaces"), thisElement, customVariables);
                        var openID = ParseVarFunction(parameters.Get(1, "open_id"), thisElement, customVariables);
                        CurrentInterfaceList?.LoadInterfaces(interfaces);
                        CurrentInterfaceList?.SetCurrentInterface(openID);
                        break;
                    }
                case "LIST_RemoveInterface": {
                        if (parameters == null)
                            break;

                        var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables);
                        if (id == null)
                            break;

                        CurrentInterfaceList?.Remove(id);
                        break;
                    }
                case "LIST_ClearInterfaces": {
                        CurrentInterfaceList?.Clear();
                        break;
                    }
                case "LIST_CloseInterfaces": {
                        CurrentInterfaceList?.CloseMenus();
                        break;
                    }
                case "LIST_ClearChain": {
                        CurrentInterfaceList?.ClearChain();
                        break;
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
                        if (parameters == null || !CurrentInterface)
                            break;

                        string sound = ParseVarFunction(parameters.Get(0, "sound"));
                        if (string.IsNullOrEmpty(sound))
                            return;

                        float volume = 1f;
                        var volumeJN = ParseVarFunction(parameters.Get(1, "vol"));
                        if (volumeJN != null)
                            volume = volumeJN;
                        
                        float pitch = 1f;
                        var pitchJN = ParseVarFunction(parameters.Get(1, "pitch"));
                        if (pitchJN != null)
                            pitch = pitchJN;

                        if (SoundManager.inst.TryGetSound(sound, out AudioClip audioClip))
                        {
                            SoundManager.inst.PlaySound(audioClip, volume, pitch);
                            break;
                        }

                        var filePath = $"{Path.GetDirectoryName(CurrentInterface.filePath)}{sound}";
                        if (!RTFile.FileExists(filePath))
                            return;

                        var audioType = RTFile.GetAudioType(filePath);
                        if (audioType == AudioType.MPEG)
                            SoundManager.inst.PlaySound(LSAudio.CreateAudioClipUsingMP3File(filePath), volume, pitch);
                        else
                            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip($"file://{filePath}", audioType, audioClip => SoundManager.inst.PlaySound(audioClip, volume, pitch)));

                        break;
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
                            PlayMusic();
                            break;
                        }

                        string music = ParseVarFunction(parameters.Get(0, "name"), thisElement, customVariables);

                        if (string.IsNullOrEmpty(music) || music.ToLower() == "default")
                        {
                            PlayMusic();
                            break;
                        }

                        var fadeDuration = ParseVarFunction(parameters.GetOrDefault(1, "fade_duration", 0.5f), thisElement, customVariables);

                        var loop = ParseVarFunction(parameters.GetOrDefault(2, "loop", true), thisElement, customVariables);

                        var filePath = $"{Path.GetDirectoryName(CurrentInterface.filePath)}{music}";
                        if (!RTFile.FileExists(filePath))
                        {
                            PlayMusic(AudioManager.inst.GetMusic(music), fadeDuration: fadeDuration, loop: loop);
                            return;
                        }

                        var audioType = RTFile.GetAudioType(filePath);
                        if (audioType == AudioType.MPEG)
                            PlayMusic(LSAudio.CreateAudioClipUsingMP3File(filePath), fadeDuration: fadeDuration, loop: loop);
                        else
                            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip($"file://{filePath}", audioType, audioClip => PlayMusic(audioClip, fadeDuration: fadeDuration, loop: loop)));

                        break;
                    }

                #endregion

                #region StopMusic

                // Stops the currently playing music. Can be good for moments where we want silence.
                // Function has no parameters.
                case "StopMusic": {
                        StopMusic();
                        break;
                    }

                #endregion

                #region PauseMusic

                // Pauses the current music if it's currently playing.
                case "PauseMusic": {
                        if (CoreHelper.InGame && parameters != null && (parameters.IsArray && !parameters[0].AsBool || parameters.IsObject && !parameters["game_audio"].AsBool))
                            CurrentAudioSource.Pause();
                        else
                            AudioManager.inst.CurrentAudioSource.Pause();

                        break;
                    }

                #endregion

                #region ResumeMusic

                // Resumes the current music if it was paused.
                case "ResumeMusic": {
                        if (CoreHelper.InGame && parameters != null && (parameters.IsArray && !parameters[0].AsBool || parameters.IsObject && !parameters["game_audio"].AsBool))
                            CurrentAudioSource.UnPause();
                        else
                            AudioManager.inst.CurrentAudioSource.UnPause();

                        break;
                    }

                #endregion

                #endregion

                #region Elements

                #region Move

                case "Move": {
                        if (!gameObject || parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["x"] == null || parameters["y"] == null))
                            break;

                        var jnX = ParseVarFunction(parameters.Get(0, "x"), thisElement, customVariables);
                        var jnY = ParseVarFunction(parameters.Get(1, "y"), thisElement, customVariables);

                        var variables = new Dictionary<string, float>
                        {
                            { "elementPosX", gameObject.transform.localPosition.x },
                            { "elementPosY", gameObject.transform.localPosition.y },
                        };

                        var x = string.IsNullOrEmpty(jnX) ? gameObject.transform.localPosition.x : RTMath.Parse(jnX, variables);
                        var y = string.IsNullOrEmpty(jnY) ? gameObject.transform.localPosition.y : RTMath.Parse(jnX, variables);

                        gameObject.transform.localPosition = new Vector3(x, y, gameObject.transform.localPosition.z);

                        break;
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
                        if (parameters == null || !CurrentInterface)
                            break;

                        var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables);
                        if (id == null)
                            break;

                        var active = ParseVarFunction(parameters.Get(1, "active"), thisElement, customVariables);

                        if (CurrentInterface.elements.TryFind(x => x.id == id, out MenuImage menuImage) && menuImage.gameObject)
                            menuImage.gameObject.SetActive(active);

                        break;
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
                        if (parameters == null || !CurrentInterface)
                            break;

                        var layoutName = ParseVarFunction(parameters.Get(0, "name"), thisElement, customVariables);
                        if (layoutName == null)
                            break;

                        var active = ParseVarFunction(parameters.Get(1, "active"), thisElement, customVariables);

                        if (CurrentInterface.layouts.TryGetValue(layoutName, out MenuLayoutBase layout) && layout.gameObject)
                            layout.gameObject.SetActive(active);

                        break;
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

                        if (CurrentInterface.elements.TryFind(x => x.id == id, out MenuImage element))
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

                        break;
                    }

                #endregion

                #region AnimateName

                // Same as animate ID, except instead of searching for an elements' ID, you search for a name. In case you'd rather find an objects' name instead of ID.
                // No example needed.
                case "AnimateName": {
                        if (parameters == null || parameters.Count < 1 || !thisElement)
                            break;

                        var elementName = parameters[0]; // Name of an object to animate
                        var type = Parser.TryParse(parameters[1], 0); // which type to animate (e.g. 0 = position, 1 = scale, 2 = rotation)

                        if (CurrentInterface.elements.TryFind(x => x.name == elementName, out MenuImage element))
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

                        break;
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
                        string id = parameters.IsArray && parameters.Count > 1 ? parameters[1] : parameters.IsObject && parameters["id"] != null ? parameters["id"] : "";
                        if (!string.IsNullOrEmpty(id) && CurrentInterface.elements.TryFind(x => x.id == id, out MenuImage menuImage))
                            animations = menuImage.animations;

                        string animName = parameters.IsArray && parameters.Count > 2 ? parameters[2] : parameters.IsObject && parameters["name"] != null ? parameters["name"] : "";

                        for (int i = 0; i < animations.Count; i++)
                        {
                            var animation = animations[i];
                            if (!string.IsNullOrEmpty(animName) && !animation.name.Replace("Interface Element Animation ", "").Contains(animName))
                                continue;

                            if (stop)
                                animation.onComplete?.Invoke();

                            animation.Pause();
                            AnimationManager.inst.Remove(animation.id);
                        }

                        break;
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
                            break;

                        thisElement.color = parameters.IsArray ? parameters[0].AsInt : parameters["col"].AsInt;

                        break;
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
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["id"] == null || parameters["text"] == null) || !CurrentInterface)
                            return;

                        var isArray = parameters.IsArray;
                        string array = isArray ? parameters[0] : parameters["id"];
                        string text = isArray ? parameters[1] : parameters["text"];

                        if (CurrentInterface.elements.TryFind(x => x.id == array, out MenuImage menuImage) && menuImage is MenuText menuText)
                        {
                            menuText.text = text;
                            menuText.textUI.maxVisibleCharacters = text.Length;
                            menuText.textUI.text = text;
                        }

                        break;
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
                            break;

                        var id = parameters.IsArray ? parameters[0] : parameters["id"];
                        if (CurrentInterface.elements.TryFind(x => x.id == id, out MenuImage element))
                        {
                            element.Clear();
                            if (element.gameObject)
                                CoreHelper.Destroy(element.gameObject);
                            CurrentInterface.elements.Remove(element);
                        }

                        break;
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
                            break;

                        var ids = parameters.IsArray ? parameters[0] : parameters["ids"];
                        if (ids == null)
                            break;

                        for (int i = 0; i < ids.Count; i++)
                        {
                            var id = ids[i];
                            if (CurrentInterface.elements.TryFind(x => x.id == id, out MenuImage element))
                            {
                                element.Clear();
                                if (element.gameObject)
                                    CoreHelper.Destroy(element.gameObject);
                                CurrentInterface.elements.Remove(element);
                            }
                        }

                        break;
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
                            break;

                        var customMenu = CurrentInterface;
                        customMenu.elements.AddRange(CustomMenu.ParseElements(parameters.IsArray ? parameters[0] : parameters["elements"], customMenu.prefabs, customMenu.spriteAssets));

                        customMenu.StartGeneration();

                        break;
                    }

                #endregion

                #region ScrollLayout

                case "ScrollLayout": {
                        if (parameters == null)
                            break;

                        var layoutName = ParseVarFunction(parameters.Get(0, "layout"), thisElement, customVariables);
                        if (layoutName == null || !layoutName.IsString)
                            return;

                        if (!CurrentInterface.layouts.TryGetValue(layoutName, out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                            break;

                        if (menuLayout is MenuGridLayout menuGridLayout)
                            menuGridLayout.Scroll(ParseVarFunction(parameters.Get(1, "x"), thisElement, customVariables), ParseVarFunction(parameters.Get(2, "y"), thisElement, customVariables), ParseVarFunction(parameters.Get(3, "x_additive"), thisElement, customVariables), ParseVarFunction(parameters.Get(4, "y_additive"), thisElement, customVariables));

                        if (menuLayout is MenuHorizontalOrVerticalLayout menuHorizontalLayout)
                            menuHorizontalLayout.Scroll(ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables), ParseVarFunction(parameters.Get(2, "additive"), thisElement, customVariables));

                        break;
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
                        if (parameters == null || !CurrentInterface)
                            break;

                        var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables);
                        if (id == null)
                            break;

                        var selectable = ParseVarFunction(parameters.Get(1, "selectable"), thisElement, customVariables);

                        if (CurrentInterface.elements.TryFind(x => x.id == id, out MenuImage menuImage))
                            menuImage.selectable = selectable;

                        break;
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
                        if (parameters == null || !CurrentInterface)
                            break;

                        var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables);
                        if (id == null)
                            break;

                        var text = ParseVarFunction(parameters.Get(1, "text"), thisElement, customVariables);

                        if (CurrentInterface.elements.TryFind(x => x.id == id, out MenuImage menuImage) && menuImage is MenuInputField menuInputField && menuInputField.inputField)
                        {
                            if (ParseVarFunction(parameters.GetOrDefault(2, "trigger", true), thisElement, customVariables).AsBool)
                                menuInputField.inputField.text = text;
                            else
                                menuInputField.inputField.SetTextWithoutNotify(text);
                        }

                        break;
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
                            break;

                        var jnElement = ParseVarFunction(parameters.Get(0, "element"), thisElement, customVariables);
                        var id = ParseVarFunction(parameters.Get(1, "id"), thisElement, customVariables);

                        var element = id == null ? thisElement : CurrentInterface.elements.Find(x => x.id == id);
                        if (!element)
                            break;

                        element.Read(jnElement, 0, 0, CurrentInterface.spriteAssets, customVariables);

                        break;
                    }

                #endregion

                #endregion

                #region Effects

                #region SetDefaultEvents

                case "SetDefaultEvents": {
                        if (CoreHelper.InGame || !MenuEffectsManager.inst)
                            break;

                        MenuEffectsManager.inst.SetDefaultEffects();

                        break;
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

                                    break;
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

                                    break;
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

                                    break;
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

                                    break;
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

                                    break;
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

                                    break;
                                }
                        }

                        thisElement.animations.Add(animation);
                        AnimationManager.inst.Play(animation);

                        break;
                    }

                #endregion

                #region UpdateEvent

                case "UpdateEvent": {
                        if (parameters == null)
                            break;

                        var effect = ParseVarFunction(parameters.Get(0, "effect"), thisElement, customVariables);
                        if (effect == null || !effect.IsString || !MenuEffectsManager.inst || !MenuEffectsManager.inst.functions.TryGetValue(effect, out Action<float> action))
                            break;

                        action?.Invoke(ParseVarFunction(parameters.Get(1, "amount").AsFloat, thisElement, customVariables));

                        break;
                    }

                #endregion

                #region SetEvent

                case "SetEvent": {
                        if (parameters == null)
                            break;

                        var type = parameters.Get(0, "type");

                        if (type == null || !type.IsString)
                            break;

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

                                    break;
                                }
                            case "ZoomCamera": {
                                    var amount = ParseVarFunction(values["amount"], thisElement, customVariables);
                                    if (amount != null)
                                        MenuEffectsManager.inst.ZoomCamera(amount.AsFloat);

                                    break;
                                }
                            case "RotateCamera": {
                                    var amount = ParseVarFunction(values["amount"], thisElement, customVariables);
                                    if (amount != null)
                                        MenuEffectsManager.inst.RotateCamera(amount.AsFloat);

                                    break;
                                }
                            case "Chroma":
                            case "Chromatic": {
                                    var intensity = ParseVarFunction(values["intensity"], thisElement, customVariables);
                                    if (intensity != null)
                                        MenuEffectsManager.inst.UpdateChroma(intensity.AsFloat);

                                    break;
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
                                        MenuEffectsManager.inst.UpdateBloomColor(col.IsString ? RTColors.HexToColor(col) : CurrentInterface.Theme.GetFXColor(col.AsInt));

                                    break;
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

                                    break;
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
                                        MenuEffectsManager.inst.UpdateVignetteColor(col.IsString ? RTColors.HexToColor(col) : CurrentInterface.Theme.GetFXColor(col.AsInt));

                                    break;
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

                                    break;
                                }
                            case "DigitalGlitch": {
                                    var intensity = ParseVarFunction(values["intensity"], thisElement, customVariables);
                                    if (intensity != null)
                                        MenuEffectsManager.inst.UpdateDigitalGlitch(intensity.AsFloat);

                                    break;
                                }
                        }

                        break;
                    }

                #endregion

                #endregion

                #region Levels

                #region LoadLevel

                // Finds a level by its' ID and loads it. On,y work if the user has already loaded levels.
                // Supports both JSON array and JSON object.
                //
                // - JSON Array Structure -
                // 0 = id
                // Example:
                // [
                //   "6365672" < loads level with this as its ID.
                // ]
                // 
                // - JSON Object Structure -
                // "id"
                // Example:
                // {
                //   "id": "6365672"
                // }
                case "LoadLevel": {
                        if (parameters == null)
                            break;

                        var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables);
                        if (id == null || !id.IsString)
                            break;

                        if (LevelManager.Levels.TryFind(x => x.id == id, out Level level))
                            LevelManager.Play(level);

                        break;
                    }

                #endregion

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
                        var directory = MainDirectory;

                        if (parameters != null)
                        {
                            var directoryJN = parameters.Get(0, "directory");
                            if (directoryJN != null)
                                directory = directoryJN;
                        }

                        if (string.IsNullOrEmpty(directory))
                            directory = MainDirectory;

                        LevelListMenu.Init(Directory.GetDirectories(directory).Where(x => Level.Verify(x)).Select(x => new Level(RTFile.ReplaceSlash(x))).ToList());
                        break;
                    }

                #endregion

                #endregion

                #region Online

                #region SetDiscordStatus

                // Sets the users' Discord status.
                // Supports both JSON array and JSON object.
                //
                // - JSON Array Structure -
                // 0 = state
                // 1 = details
                // 2 = icon
                // 3 = art
                // Example:
                // [
                //   "Navigating Main Menu",
                //   "In Menus",
                //   "menu", < accepts values: arcade, editor, play, menu
                //   "pa_logo_white" < accepts values: pa_logo_white, pa_logo_black
                // ]
                //
                // - JSON Object Structure -
                // "state"
                // "details"
                // "icon"
                // "art"
                // Example:
                // {
                //   "state": "Interfacing or soemthing Idk",
                //   "details": "In the Interface",
                //   "icon": "play"
                //   // if art is null, then the default art will be pa_logo_white.
                // }
                case "SetDiscordStatus": {
                        if (parameters == null)
                            break;

                        CoreHelper.UpdateDiscordStatus(
                            parameters.Get(0, "state"),
                            parameters.Get(1, "details"),
                            parameters.Get(2, "icon"),
                            parameters.GetOrDefault(3, "art", "pa_logo_white"));

                        break;
                    }

                #endregion

                #region OpenLink

                case "OpenLink": {
                        var linkType = Parser.TryParse(ParseVarFunction(parameters.Get(0, "link_type"), thisElement, customVariables), URLSource.Artist);
                        var site = ParseVarFunction(parameters.Get(1, "site"), thisElement, customVariables);
                        var link = ParseVarFunction(parameters.Get(2, "link"), thisElement, customVariables);

                        var url = AlephNetwork.GetURL(linkType, site, link);
                        CoreHelper.Log($"Opening URL: {url}");
                        Application.OpenURL(url);

                        break;
                    }

                #endregion
                    
                #region ModderDiscord

                // Opens the System Error Discord server link.
                // Function has no parameters.
                case "ModderDiscord": {
                        Application.OpenURL(AlephNetwork.MOD_DISCORD_URL);

                        break;
                    }

                #endregion

                #region SourceCode

                // Opens the GitHub Source Code link.
                // Function has no parameters.
                case "SourceCode": {
                        Application.OpenURL(AlephNetwork.OPEN_SOURCE_URL);

                        break;
                    }

                #endregion

                #endregion

                #region Specific

                #region OpenChangelog

                case "OpenChangelog": {
                        OpenChangelog();
                        break;
                    }

                #endregion

                #region LoadLevels

                case "LoadLevels": {
                        LoadLevelsMenu.Init(() =>
                        {
                            if (parameters["on_loading_end"] != null)
                                ParseFunction(parameters["on_loading_end"], thisElement, customVariables);
                        });
                        break;
                    }

                #endregion

                #region OnInputsSelected

                case "OnInputsSelected": {
                        InputSelectMenu.OnInputsSelected = () =>
                        {
                            if (parameters["continue"] != null)
                                ParseFunction(parameters["continue"], thisElement, customVariables);
                        };
                        break;
                    }

                #endregion

                #region Profile

                case "Profile": {
                        CloseMenus();
                        var profileMenu = new ProfileMenu();
                        CurrentInterface = profileMenu;

                        break;
                    }

                #endregion

                #region BeginStoryMode

                // Begins the BetterLegacy story mode.
                // Function has no parameters.
                case "BeginStoryMode": {
                        LevelManager.IsArcade = false;
                        SceneHelper.LoadInputSelect(SceneHelper.LoadInterfaceScene);

                        break;
                    }

                #endregion

                #region LoadStoryLevel

                case "LoadStoryLevel": {
                        if (parameters == null)
                            break;

                        var chapter = ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables);
                        if (chapter == null)
                            break;

                        var level = ParseVarFunction(parameters.Get(1, "level"), thisElement, customVariables);
                        if (level == null)
                            break;

                        var cutsceneIndex = ParseVarFunction(parameters.Get(2, "cutscene_index"), thisElement, customVariables).AsInt;
                        var bonus = ParseVarFunction(parameters.Get(4, "bonus"), thisElement, customVariables).AsBool;
                        var skipCutscenes = ParseVarFunction(parameters.GetOrDefault(5, "skip_cutscenes", true), thisElement, customVariables).AsBool;

                        StoryManager.inst.ContinueStory = ParseVarFunction(parameters.Get(3, "continue"), thisElement, customVariables).AsBool;

                        ArcadeHelper.ResetModifiedStates();
                        StoryManager.inst.Play(chapter.AsInt, level.AsInt, cutsceneIndex, bonus, skipCutscenes);

                        break;
                    }

                #endregion

                #region LoadStoryCutscene

                case "LoadStoryCutscene": {
                        if (parameters == null)
                            break;

                        var chapter = ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables);
                        if (chapter == null)
                            break;

                        var level = ParseVarFunction(parameters.Get(1, "level"), thisElement, customVariables);
                        if (level == null)
                            break;

                        var isArray = parameters.IsArray;
                        var cutsceneDestinationJN = ParseVarFunction(parameters.Get(2, "cutscene_destination"), thisElement, customVariables);
                        var cutsceneDestination = Parser.TryParse(cutsceneDestinationJN, CutsceneDestination.Pre);
                        var cutsceneIndex = ParseVarFunction(parameters.Get(3, "cutscene_index"), thisElement, customVariables).AsInt;
                        var bonus = ParseVarFunction(parameters.Get(4, "bonus"), thisElement, customVariables).AsBool;

                        StoryManager.inst.ContinueStory = false;

                        ArcadeHelper.ResetModifiedStates();
                        StoryManager.inst.PlayCutscene(chapter, level, cutsceneDestination, cutsceneIndex, bonus);

                        break;
                    }

                #endregion

                #region PlayAllCutscenes

                case "PlayAllCutscenes": {
                        if (parameters == null)
                            return;

                        var chapter = ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables);
                        if (chapter == null)
                            break;

                        StoryManager.inst.PlayAllCutscenes(chapter, ParseVarFunction(parameters.Get(1, "bonus"), thisElement, customVariables).AsBool);

                        break;
                    }

                #endregion

                #region LoadStoryLevelPath

                case "LoadStoryLevelPath": {
                        if (parameters == null)
                            return;

                        var path = ParseVarFunction(parameters.Get(0, "path"), thisElement, customVariables);
                        if (path == null || !path.IsString)
                            break;

                        var songName = ParseVarFunction(parameters.Get(1, "song"), thisElement, customVariables);

                        StoryManager.inst.ContinueStory = ParseVarFunction(parameters.Get(2, "continue"), thisElement, customVariables).AsBool;

                        ArcadeHelper.ResetModifiedStates();
                        StoryManager.inst.Play(path, songName);

                        break;
                    }

                #endregion

                #region LoadNextStoryLevel

                case "LoadNextStoryLevel": {
                        StoryManager.inst.ContinueStory = true;

                        int chapter = StoryManager.inst.CurrentSave.ChapterIndex;
                        StoryManager.inst.Play(chapter, StoryManager.inst.CurrentSave.LoadInt($"DOC{RTString.ToStoryNumber(chapter)}Progress", 0), 0, StoryManager.inst.inBonusChapter);

                        break;
                    }

                #endregion

                #region LoadCurrentStoryInterface

                case "LoadCurrentStoryInterface": {
                        StartupStoryInterface();

                        break;
                    }

                #endregion

                #region LoadStoryInterface

                case "LoadStoryInterface": {
                        StartupStoryInterface(ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables).AsInt, ParseVarFunction(parameters.Get(1, "level"), thisElement, customVariables).AsInt);

                        break;
                    }

                #endregion

                #region LoadChapterTransition

                case "LoadChapterTransition": {
                        var isArray = parameters.IsArray;

                        StoryManager.inst.ContinueStory = true;

                        var chapter = StoryManager.inst.CurrentSave.ChapterIndex;
                        StoryManager.inst.Play(chapter, StoryMode.Instance.chapters[chapter].Count, 0, StoryManager.inst.inBonusChapter);

                        break;
                    }

                #endregion

                #region SaveProfileValue

                case "SaveProfileValue": {
                        if (parameters == null || !LegacyPlugin.player || LegacyPlugin.player.memory == null)
                            break;

                        var varName = ParseVarFunction(parameters.Get(0, "var_name"), thisElement, customVariables);
                        if (varName == null || !varName.IsString)
                            break;
                        
                        var value = ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables);
                        if (value == null)
                            break;

                        LegacyPlugin.player.memory[varName.Value] = value;
                        LegacyPlugin.SaveProfile();

                        break;
                    }

                #endregion

                #region StorySaveBool

                case "StorySaveBool": {
                        if (parameters == null)
                            break;

                        var saveName = ParseVarFunction(parameters.Get(0, "name"), thisElement, customVariables);
                        if (saveName == null || !saveName.IsString)
                            break;

                        var value = ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables);

                        if (ParseVarFunction(parameters.Get(2, "toggle"), thisElement, customVariables).AsBool)
                            value = !StoryManager.inst.CurrentSave.LoadBool(saveName, false);

                        StoryManager.inst.CurrentSave.SaveBool(saveName, value.AsBool);

                        break;
                    }

                #endregion

                #region StorySaveInt

                case "StorySaveInt": {
                        if (parameters == null)
                            break;

                        var saveName = ParseVarFunction(parameters.Get(0, "name"), thisElement, customVariables);
                        if (saveName == null || !saveName.IsString)
                            break;

                        var value = ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables);
                        
                        if (ParseVarFunction(parameters.Get(2, "relative"), thisElement, customVariables).AsBool)
                            value += StoryManager.inst.CurrentSave.LoadInt(saveName, 0);

                        StoryManager.inst.CurrentSave.SaveInt(saveName, value.AsInt);

                        break;
                    }

                #endregion

                #region StorySaveFloat

                case "StorySaveFloat": {
                        if (parameters == null)
                            break;

                        var saveName = ParseVarFunction(parameters.Get(0, "name"), thisElement, customVariables);
                        if (saveName == null || !saveName.IsString)
                            break;

                        var value = ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables);
                        
                        if (ParseVarFunction(parameters.Get(2, "relative"), thisElement, customVariables).AsBool)
                            value += StoryManager.inst.CurrentSave.LoadFloat(saveName, 0);

                        StoryManager.inst.CurrentSave.SaveFloat(saveName, value.AsFloat);

                        break;
                    }

                #endregion

                #region StorySaveString

                case "StorySaveString": {
                        if (parameters == null)
                            break;

                        var saveName = ParseVarFunction(parameters.Get(0, "name"), thisElement, customVariables);
                        if (saveName == null || !saveName.IsString)
                            break;

                        var value = ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables);
                        if (ParseVarFunction(parameters.Get(2, "relative"), thisElement, customVariables).AsBool)
                            value += StoryManager.inst.CurrentSave.LoadString(saveName, string.Empty);

                        StoryManager.inst.CurrentSave.SaveString(saveName, value);

                        break;
                    }

                #endregion

                #region StorySaveJSON

                case "StorySaveJSON": {
                        if (parameters == null)
                            break;

                        var saveName = ParseVarFunction(parameters.Get(0, "name"), thisElement, customVariables);
                        if (saveName == null || !saveName.IsString)
                            break;

                        var value = ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables);

                        StoryManager.inst.CurrentSave.SaveNode(saveName, value);

                        break;
                    }

                #endregion

                #region LoadStorySaveJSONText

                case "LoadStorySaveStringText": {
                        if (parameters == null)
                            break;

                        var saveName = ParseVarFunction(parameters.Get(0, "name"), thisElement, customVariables);
                        if (saveName == null || !saveName.IsString)
                            break;

                        var text = StoryManager.inst.CurrentSave.LoadString(saveName, string.Empty);

                        if (thisElement is MenuText menuText)
                            menuText.text = text;

                        break;
                    }

                #endregion

                #endregion
            }
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
            // if json is null or it's an array, just return itself.
            if (jn == null || jn.IsArray)
                return jn;

            // item is a singular string
            if (jn.IsString)
                return customVariables != null && customVariables.TryGetValue(jn.Value, out JSONNode customVariable) ? ParseVarFunction(customVariable, thisElement, customVariables) : ParseText(jn, customVariables);

            // item is lang (need to make sure it actually IS a lang by checking for language names)
            if (Lang.TryParse(jn, out Lang lang))
                return ParseText(lang, customVariables);

            var parameters = jn["params"];
            string name = jn["name"];

            switch (name)
            {
                #region Switch

                // Parses a variable from a switch argument.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = integer variable.
                // 1 = default item.
                // 2 = array of items to return based on the integer variable provided.
                // Example:
                // [
                //   2,
                //   "This is the default item!",
                //   [
                //     "Item 0",
                //     "Item 1",
                //     "Item 2", < since the integer variable is "2", this item will be returned.
                //     "Item 3"
                //   ]
                // ]
                // 
                // - JSON Object Structure -
                // "var"
                // "default"
                // "case"
                // Example:
                // {
                //   "var": "-1",
                //   "default": "i AM DEFAULT, no SPEAK TO me", < since "var" is out of the range of the case, it returns this default item.
                //   "case": [
                //     "Some kind of item.",
                //     "Another item...",
                //     {
                //       "value": "The item is an object?!" < items can be objects.
                //     }
                //   ]
                // }
                case "Switch": {
                        if (parameters == null)
                            break;

                        var variable = ParseVarFunction(parameters.Get(0, "var"), thisElement, customVariables);
                        var defaultItem = ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables);
                        var caseParam = ParseVarFunction(parameters.Get(2, "case"), thisElement, customVariables);

                        if (caseParam.IsArray && (!variable.IsNumber || variable < 0 || variable >= caseParam.Count))
                            return defaultItem;
                        if (caseParam.IsObject && (!variable.IsString || caseParam[variable.Value] == null))
                            return defaultItem;

                        return ParseVarFunction(variable.IsNumber ? caseParam[variable.AsInt] : caseParam[variable.Value], thisElement, customVariables);
                    }

                #endregion

                #region If

                // Parses a variable from an if argument.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // The item itself is an array, so these values represent items' values in the array.
                // "if" = check function.
                // "return" = returns a specified item.
                // "else" = if this item should be returned instead if the previous result is false. This value is optional.
                // Example:
                // [
                //   {
                //     "if": "True",
                //     "return": "I have a place."
                //   },
                //   {
                //     "if": "False", < because this is false and "else" is true, the return value of this item is returned.
                //     "else": True,
                //     "return": "I no longer have a place."
                //   }
                // ]
                // 
                // - JSON Object Structure -
                // "if"
                // "return"
                // Example:
                // {
                //   "if": "True",
                //   "return": {
                //     "value": "i AM value!!!"
                //   }
                // }
                case "If": {
                        if (parameters == null)
                            break;

                        if (parameters.IsArray)
                        {
                            JSONNode variable = null;
                            var result = false;
                            for (int i = 0; i < parameters.Count; i++)
                            {
                                var check = parameters[i];
                                if (check.IsString)
                                {
                                    if (!result)
                                        return check;
                                    continue;
                                }

                                var ifCheck = check["if"];

                                if (ifCheck == null && !result)
                                    return check.IsNull ? check : check["return"];

                                var elseCheck = check["else"].AsBool;
                                if (result && !elseCheck)
                                    continue;

                                result = ParseIfFunction(ifCheck, thisElement);
                                if (result)
                                    variable = check["return"];
                            }

                            return variable;
                        }

                        if (parameters["if"] != null && parameters["return"] != null && ParseIfFunction(parameters["if"], thisElement))
                            return parameters["return"];

                        break;
                    }

                #endregion

                #region Bool

                // Parses a true or false variable from an if argument.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // The item itself is an array, so these values represent items' values in the array.
                // "if" = check function.
                // "else" = if this item should be returned instead if the previous result is false. This value is optional.
                // Example:
                // [
                //   {
                //     "if": "True"
                //   },
                //   {
                //     "if": "False", < because this is false and "else" is true, the return value of this item is returned.
                //     "else": True
                //   }
                // ]
                // 
                // - JSON Object Structure -
                // "if"
                // Example:
                // {
                //   "if": "True" < "True" is returned because the boolean function is true
                // }
                case "Bool": {
                        if (parameters == null)
                            break;

                        if (parameters.IsArray)
                        {
                            var result = false;
                            for (int i = 0; i < parameters.Count; i++)
                            {
                                var check = parameters[i];
                                var elseCheck = check["else"].AsBool;
                                if (result && !elseCheck)
                                    continue;

                                result = ParseIfFunction(check["if"], thisElement);
                            }

                            return result.ToString();
                        }

                        return (parameters["if"] != null && parameters["return"] != null && ParseIfFunction(parameters["if"], thisElement)).ToString();
                    }

                #endregion

                #region StoryLoadBoolVar

                // Parses a variable from the current story save.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = variable name to load from the story save.
                // 1 = default value if there is no value.
                // Example:
                // [
                //   "DOC02WATER",
                //   "False"
                // ]
                // 
                // - JSON Object Structure -
                // "load"
                // "default"
                // Example:
                // {
                //   "load": "NULL",
                //   "default": "False" < returns this value since NULL does not exist.
                // }
                case "StoryLoadBoolVar": {
                        return StoryManager.inst.CurrentSave.LoadBool(parameters.Get(0, "load"), parameters.Get(1, "default")).ToString();
                    }

                #endregion

                #region StoryLoadIntVar

                // Parses a variable from the current story save.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = variable name to load from the story save.
                // 1 = default value if there is no value.
                // Example:
                // [
                //   "DOC02WATER",
                //   "0"
                // ]
                // 
                // - JSON Object Structure -
                // "load"
                // "default"
                // Example:
                // {
                //   "load": "NULL",
                //   "default": "0" < returns this value since NULL does not exist.
                // }
                case "StoryLoadIntVar": {
                        return StoryManager.inst.CurrentSave.LoadInt(parameters.Get(0, "load"), parameters.Get(1, "default")).ToString();
                    }

                #endregion

                #region StoryLoadFloatVar

                // Parses a variable from the current story save.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = variable name to load from the story save.
                // 1 = default value if there is no value.
                // Example:
                // [
                //   "DOC02WATER",
                //   "0"
                // ]
                // 
                // - JSON Object Structure -
                // "load"
                // "default"
                // Example:
                // {
                //   "load": "NULL",
                //   "default": "0" < returns this value since NULL does not exist.
                // }
                case "StoryLoadFloatVar": {
                        return StoryManager.inst.CurrentSave.LoadFloat(parameters.Get(0, "load"), parameters.Get(1, "default")).ToString();
                    }

                #endregion

                #region StoryLoadStringVar

                // Parses a variable from the current story save.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = variable name to load from the story save.
                // 1 = default value if there is no value.
                // Example:
                // [
                //   "DOC02WATER",
                //   "False"
                // ]
                // 
                // - JSON Object Structure -
                // "load"
                // "default"
                // Example:
                // {
                //   "load": "NULL",
                //   "default": "False" < returns this value since NULL does not exist.
                // }
                case "StoryLoadStringVar": {
                        return StoryManager.inst.CurrentSave.LoadString(parameters.Get(0, "load"), parameters.Get(1, "default")).ToString();
                    }

                #endregion

                #region StoryLoadJSONVar

                // Parses a variable from the current story save.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = variable name to load from the story save.
                // 1 = default value if there is no value.
                // Example:
                // [
                //   "DOC02WATER",
                //   "False"
                // ]
                // 
                // - JSON Object Structure -
                // "load"
                // "default"
                // Example:
                // {
                //   "load": "NULL",
                //   "default": "False" < returns this value since NULL does not exist.
                // }
                case "StoryLoadJSONVar": {
                        return StoryManager.inst.CurrentSave.LoadJSON(parameters.Get(0, "load")).ToString();
                    }

                #endregion

                #region FormatString

                // Parses a variable from a formatted string.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = string to format.
                // 1 = array of values to format to the string.
                // Example:
                // [
                //   "Format {0}!", < returns "Format this!".
                //   [
                //     "this"
                //   ]
                // ]
                // 
                // - JSON Object Structure -
                // "format"
                // "args"
                // Example:
                // {
                //   "format": "Noo don't {0}!",
                //   "args": [
                //     {
                //       "name": "Switch",
                //       "params": {
                //         "var": "0",
                //         "default": "format default",
                //         "case": [
                //           "format me",
                //           "format yourself"
                //         ]
                //       }
                //     }
                //   ]
                // }
                case "FormatString": {
                        if (parameters == null)
                            break;

                        var str = ParseVarFunction(parameters.Get(0, "format"), thisElement, customVariables);
                        var args = ParseVarFunction(parameters.Get(1, "args"), thisElement, customVariables);
                        if (args == null || !args.IsArray)
                            return str;

                        var strArgs = new object[args.Count];
                        for (int i = 0; i < strArgs.Length; i++)
                            strArgs[i] = ParseVarFunction(args[i], thisElement, customVariables).Value;

                        return string.Format(str, strArgs);
                    }

                #endregion

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
                            "gui_accent" => CurrentTheme.guiAccentColor,
                            "bg" => CurrentTheme.backgroundColor,
                            "player" => CurrentTheme.playerColors.GetAt(index.AsInt),
                            "obj" => CurrentTheme.objectColors.GetAt(index.AsInt),
                            "bgs" => CurrentTheme.backgroundColors.GetAt(index.AsInt),
                            "fx" => CurrentTheme.effectColors.GetAt(index.AsInt),
                            _ => CurrentTheme.guiColor,
                        }).ToString();
                    }

                #endregion

                #region ToStoryNumber

                case "ToStoryNumber": {
                        if (parameters == null)
                            break;

                        var number = ParseVarFunction(parameters.Get(0, "num"), thisElement, customVariables);
                        if (number == null || !number.IsNumber)
                            break;

                        return RTString.ToStoryNumber(number.AsInt);
                    }

                #endregion

                #region LoadProfileValue

                case "LoadProfileValue": {
                        if (parameters == null || !LegacyPlugin.player || LegacyPlugin.player.memory == null)
                            break;

                        var varName = ParseVarFunction(parameters.Get(0, "var_name"), thisElement, customVariables);
                        if (varName == null || !varName.IsString)
                            break;
                        return LegacyPlugin.player.memory[varName.Value];
                    }

                #endregion

                #region StoryLevelID

                case "StoryLevelID": {
                        if (parameters == null)
                            break;

                        var chapterIndex = ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables);
                        var levelIndex = ParseVarFunction(parameters.Get(1, "level"), thisElement, customVariables);
                        var bonus = ParseVarFunction(parameters.Get(2, "bonus"), thisElement, customVariables);
                        var chapters = bonus ? StoryMode.Instance.bonusChapters : StoryMode.Instance.chapters;

                        return chapters.TryGetAt(chapterIndex.AsInt, out StoryMode.Chapter chapter) && chapter.levels.TryGetAt(levelIndex.AsInt, out StoryMode.LevelSequence level) ? level.id : ParseVarFunction(parameters.Get(3, "default"), thisElement, customVariables);
                    }

                #endregion

                #region StoryLevelName

                case "StoryLevelName": {
                        if (parameters == null)
                            break;

                        var chapterIndex = ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables);
                        var levelIndex = ParseVarFunction(parameters.Get(1, "level"), thisElement, customVariables);
                        var bonus = ParseVarFunction(parameters.Get(2, "bonus"), thisElement, customVariables);
                        var chapters = bonus ? StoryMode.Instance.bonusChapters : StoryMode.Instance.chapters;

                        return chapters.TryGetAt(chapterIndex.AsInt, out StoryMode.Chapter chapter) && chapter.levels.TryGetAt(levelIndex.AsInt, out StoryMode.LevelSequence level) ? level.name : ParseVarFunction(parameters.Get(3, "default"), thisElement, customVariables);
                    }

                #endregion
                    
                #region StoryLevelSongTitle

                case "StoryLevelSongTitle": {
                        if (parameters == null)
                            break;

                        var chapterIndex = ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables);
                        var levelIndex = ParseVarFunction(parameters.Get(1, "level"), thisElement, customVariables);
                        var bonus = ParseVarFunction(parameters.Get(2, "bonus"), thisElement, customVariables);
                        var chapters = bonus ? StoryMode.Instance.bonusChapters : StoryMode.Instance.chapters;

                        return chapters.TryGetAt(chapterIndex.AsInt, out StoryMode.Chapter chapter) && chapter.levels.TryGetAt(levelIndex.AsInt, out StoryMode.LevelSequence level) ? level.songTitle : ParseVarFunction(parameters.Get(3, "default"), thisElement, customVariables);
                    }

                #endregion
                    
                #region StoryLevelCount

                case "StoryLevelCount": {
                        if (parameters == null)
                            break;

                        var chapterIndex = ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables);
                        var bonus = ParseVarFunction(parameters.Get(1, "bonus"), thisElement, customVariables);
                        var chapters = bonus ? StoryMode.Instance.bonusChapters : StoryMode.Instance.chapters;

                        return chapters.TryGetAt(chapterIndex.AsInt, out StoryMode.Chapter chapter) ? chapter.Count : ParseVarFunction(parameters.Get(2, "default"), thisElement, customVariables);
                    }

                #endregion

                #region ParseMath

                // Parses a variable from a math evaluation.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = math evaluation.
                // 1 = variables array.
                // Example:
                // [
                //   "1 + 1", < returns 2
                //   [ ] < variables can be left empty
                // ]
                // 
                // - JSON Object Structure -
                // "evaluate"
                // "vars"
                // Example:
                // {
                //   "evaluate": "10 + VAR1 + VAR2", < returns 20
                //   "vars": [ < can include multiple variables
                //     {
                //       "n": "VAR1",
                //       "v": "5" < variable is just 5
                //     },
                //     {
                //       "n": "VAR2",
                //       "v": { < variable can be a parse function
                //         "name": "ParseMath",
                //         "params": [
                //           "1 * 5"
                //         ]
                //       }
                //     }
                //   ]
                // }
                case "ParseMath": {
                        if (parameters == null)
                            break;

                        Dictionary<string, float> vars = null;

                        if (jn["vars"] != null)
                        {
                            vars = new Dictionary<string, float>();
                            for (int i = 0; i < jn["vars"].Count; i++)
                            {
                                var item = jn["vars"][i];
                                if (string.IsNullOrEmpty(item["n"]))
                                    continue;
                                var val = ParseVarFunction(item["v"], thisElement, customVariables);
                                if (!val.IsNumber)
                                    continue;
                                vars[name] = val;
                            }
                        }

                        return RTMath.Parse(ParseVarFunction(parameters.Get(0, "evaluate"), thisElement, customVariables), vars).ToString();
                    }

                    #endregion
            }

            return jn;
        }

        #endregion
    }
}
