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
        public const string MAIN_MENU_ID = "0";
        public const string STORY_SAVES_MENU_ID = "1";
        public const string CHAPTER_SELECT_MENU_ID = "2";
        public const string PROFILE_MENU_ID = "3";
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
        }

        /// <summary>
        /// Clears interface data and stops interface generation.
        /// </summary>
        /// <param name="clearThemes">If interface themes should be cleared.</param>
        /// <param name="stopGenerating">If the current interface should stop generating.</param>
        public void Clear(bool clearThemes = true, bool stopGenerating = true)
        {
            if (CurrentInterface)
            {
                CurrentInterface.Clear();
                CurrentInterface = null;
            }

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

            var storyStarted = StoryManager.inst.LoadBool("StoryModeStarted", false);
            var chapter = StoryMode.Instance.chapters[chapterIndex];

            if (onReturnToStoryInterface != null)
            {
                onReturnToStoryInterface();
                onReturnToStoryInterface = null;
                return;
            }

            var path = storyStarted ? chapter.interfacePath : StoryMode.Instance.entryInterfacePath;

            Parse(path);
        }

        /// <summary>
        /// Parses an interface from a path, adds it to the interfaces list and opens it.
        /// </summary>
        /// <param name="path">Path to an interface.</param>
        public void Parse(string path)
        {
            var jn = JSON.Parse(RTFile.ReadFromFile(path));

            var menu = CustomMenu.Parse(jn);
            menu.filePath = path;
            interfaces.Add(menu);

            SetCurrentInterface(menu.id);
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

            Parse(RTFile.GetAsset($"Interfaces/main_menu{FileFormat.LSI.Dot()}"));

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

        public string ParseSpawnText(string input)
        {
            RTString.RegexMatches(input, new Regex(@"{{Date=(.*?)}}"), match =>
            {
                input = input.Replace(match.Groups[0].ToString(), DateTime.Now.ToString(match.Groups[1].ToString()));
            });
            return ParseText(input);
        }

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

        public string ParseText(string input) => RTString.ParseText(input);

        /// <summary>
        /// Parses an entire func JSON. Supports both JSON Object and JSON Array.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        public void ParseFunction(JSONNode jn, MenuImage thisElement = null)
        {
            if (jn.IsArray)
            {
                for (int i = 0; i < jn.Count; i++)
                    ParseFunctionSingle(jn[i], thisElement: thisElement);

                return;
            } // Allow multiple functions to occur.

            ParseFunctionSingle(jn, thisElement: thisElement);
        }

        /// <summary>
        /// Parses an "if_func" JSON and returns the result. Supports both JSON Object and JSON Array.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns true if the passed JSON functions is true, otherwise false.</returns>
        public bool ParseIfFunction(JSONNode jn, MenuImage thisElement = null)
        {
            if (jn == null)
                return true;

            if (jn.IsObject)
                return ParseIfFunctionSingle(jn, thisElement);

            bool canProceed = true;

            if (jn.IsArray)
            {
                for (int i = 0; i < jn.Count; i++)
                {
                    var value = ParseIfFunctionSingle(jn[i], thisElement);
                    if (!jn[i]["otherwise"].AsBool && !value)
                        canProceed = false;

                    if (jn[i]["otherwise"].AsBool && value)
                        canProceed = true;

                    //if (jn[i]["and"].AsBool && !value)
                    //    canProceed = false;
                }
            }

            return canProceed;
        }

        /// <summary>
        /// Parses a singular "if_func" JSON.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns true if the passed JSON function is true, otherwise false.</returns>
        public bool ParseIfFunctionSingle(JSONNode jn, MenuImage thisElement = null)
        {
            var parameters = jn["params"];
            string name = jn["name"];
            var not = jn["not"].AsBool; // If true, then check if the function is not true.

            switch (name)
            {
                case "True": return true;
                case "False": return false;
                case "GetSettingBool":
                    {
                        if (parameters == null)
                            break;

                        var value = DataManager.inst.GetSettingBool(parameters.IsArray ? parameters[0] : parameters["setting"], parameters.IsArray ? parameters[1].AsBool : parameters["default"].AsBool);
                        return !not ? value : !value;
                    }
                case "GetSettingIntEquals":
                    {
                        if (parameters == null)
                            break;

                        var value = DataManager.inst.GetSettingInt(parameters.IsArray ? parameters[0] : parameters["setting"], parameters.IsArray ? parameters[1].AsInt : parameters["default"].AsInt) == (parameters.IsArray ? parameters[2].AsInt : parameters["value"].AsInt);
                        return !not ? value : !value;
                    }
                case "GetSettingIntLesserEquals":
                    {
                        if (parameters == null)
                            break;

                        var value = DataManager.inst.GetSettingInt(parameters.IsArray ? parameters[0] : parameters["setting"], parameters.IsArray ? parameters[1].AsInt : parameters["default"].AsInt) <= (parameters.IsArray ? parameters[2].AsInt : parameters["value"].AsInt);
                        return !not ? value : !value;
                    }
                case "GetSettingIntGreaterEquals":
                    {
                        if (parameters == null)
                            break;

                        var value = DataManager.inst.GetSettingInt(parameters.IsArray ? parameters[0] : parameters["setting"], parameters.IsArray ? parameters[1].AsInt : parameters["default"].AsInt) >= (parameters.IsArray ? parameters[2].AsInt : parameters["value"].AsInt);
                        return !not ? value : !value;
                    }
                case "GetSettingIntLesser":
                    {
                        if (parameters == null)
                            break;

                        var value = DataManager.inst.GetSettingInt(parameters.IsArray ? parameters[0] : parameters["setting"], parameters.IsArray ? parameters[1].AsInt : parameters["default"].AsInt) < (parameters.IsArray ? parameters[2].AsInt : parameters["value"].AsInt);
                        return !not ? value : !value;
                    }
                case "GetSettingIntGreater":
                    {
                        if (parameters == null)
                            break;

                        var value = DataManager.inst.GetSettingInt(parameters.IsArray ? parameters[0] : parameters["setting"], parameters.IsArray ? parameters[1].AsInt : parameters["default"].AsInt) > (parameters.IsArray ? parameters[2].AsInt : parameters["value"].AsInt);
                        return !not ? value : !value;
                    }
                case "IsScene":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["scene"] == null)
                            break;

                        var value = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == (parameters.IsArray ? parameters[0] : parameters["scene"]);
                        return !not ? value : !value;
                    }

                #region Player

                case "PlayerCountEquals":
                    {
                        if (parameters == null)
                            break;

                        var value = InputDataManager.inst.players.Count == (parameters.IsArray ? parameters[0].AsInt : parameters["count"].AsInt);
                        return !not ? value : !value;
                    }
                case "PlayerCountLesserEquals":
                    {
                        if (parameters == null)
                            break;

                        var value = InputDataManager.inst.players.Count <= (parameters.IsArray ? parameters[0].AsInt : parameters["count"].AsInt);
                        return !not ? value : !value;
                    }
                case "PlayerCountGreaterEquals":
                    {
                        if (parameters == null)
                            break;

                        var value = InputDataManager.inst.players.Count >= (parameters.IsArray ? parameters[0].AsInt : parameters["count"].AsInt);
                        return !not ? value : !value;
                    }
                case "PlayerCountLesser":
                    {
                        if (parameters == null)
                            break;

                        var value = InputDataManager.inst.players.Count < (parameters.IsArray ? parameters[0].AsInt : parameters["count"].AsInt);
                        return !not ? value : !value;
                    }
                case "PlayerCountGreater":
                    {
                        if (parameters == null)
                            break;

                        var value = InputDataManager.inst.players.Count > (parameters.IsArray ? parameters[0].AsInt : parameters["count"].AsInt);
                        return !not ? value : !value;
                    }

                #endregion

                #region Story Chapter

                case "StoryChapterEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["chapter"] == null)
                            break;

                        var value = StoryManager.inst.LoadInt("Chapter", 0) == (parameters.IsArray ? parameters[0].AsInt : parameters["chapter"].AsInt);
                        return !not ? value : !value;
                    }
                case "StoryChapterLesserEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["chapter"] == null)
                            break;

                        var value = StoryManager.inst.LoadInt("Chapter", 0) <= (parameters.IsArray ? parameters[0].AsInt : parameters["chapter"].AsInt);
                        return !not ? value : !value;
                    }
                case "StoryChapterGreaterEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["chapter"] == null)
                            break;

                        var value = StoryManager.inst.LoadInt("Chapter", 0) >= (parameters.IsArray ? parameters[0].AsInt : parameters["chapter"].AsInt);
                        return !not ? value : !value;
                    }
                case "StoryChapterLesser":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["chapter"] == null)
                            break;

                        var value = StoryManager.inst.LoadInt("Chapter", 0) < (parameters.IsArray ? parameters[0].AsInt : parameters["chapter"].AsInt);
                        return !not ? value : !value;
                    }
                case "StoryChapterGreater":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["chapter"] == null)
                            break;

                        var value = StoryManager.inst.LoadInt("Chapter", 0) > (parameters.IsArray ? parameters[0].AsInt : parameters["chapter"].AsInt);
                        return !not ? value : !value;
                    }
                case "DisplayNameEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["user"] == null)
                            break;

                        var value = CoreConfig.Instance.DisplayName.Value == (parameters.IsArray ? parameters[0].Value : parameters["user"].Value);
                        return !not ? value : !value;
                    }
                case "StoryInstalled":
                    {
                        var value = StoryManager.inst && RTFile.DirectoryExists(StoryManager.StoryAssetsPath);
                        return !not ? value : !value;
                    }
                case "StoryLoadIntEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 3 || parameters.IsObject && parameters["load"] == null)
                            break;

                        var value = StoryManager.inst.LoadInt(parameters.IsArray ? parameters[0] : parameters["load"], Parser.TryParse(parameters.IsArray ? parameters[1] : parameters["default"], 0)) == Parser.TryParse(parameters.IsArray ? parameters[2] : parameters["value"], 0);
                        return !not ? value : !value;
                    }
                case "StoryLoadIntLesserEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 3 || parameters.IsObject && parameters["load"] == null)
                            break;

                        var value = StoryManager.inst.LoadInt(parameters.IsArray ? parameters[0] : parameters["load"], Parser.TryParse(parameters.IsArray ? parameters[1] : parameters["default"], 0)) <= Parser.TryParse(parameters.IsArray ? parameters[2] : parameters["value"], 0);
                        return !not ? value : !value;
                    }
                case "StoryLoadIntGreaterEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 3 || parameters.IsObject && parameters["load"] == null)
                            break;

                        var value = StoryManager.inst.LoadInt(parameters.IsArray ? parameters[0] : parameters["load"], Parser.TryParse(parameters.IsArray ? parameters[1] : parameters["default"], 0)) >= Parser.TryParse(parameters.IsArray ? parameters[2] : parameters["value"], 0);
                        return !not ? value : !value;
                    }
                case "StoryLoadIntLesser":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 3 || parameters.IsObject && parameters["load"] == null)
                            break;

                        var value = StoryManager.inst.LoadInt(parameters.IsArray ? parameters[0] : parameters["load"], Parser.TryParse(parameters.IsArray ? parameters[1] : parameters["default"], 0)) < Parser.TryParse(parameters.IsArray ? parameters[2] : parameters["value"], 0);
                        return !not ? value : !value;
                    }
                case "StoryLoadIntGreater":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 3 || parameters.IsObject && parameters["load"] == null)
                            break;

                        var value = StoryManager.inst.LoadInt(parameters.IsArray ? parameters[0] : parameters["load"], Parser.TryParse(parameters.IsArray ? parameters[1] : parameters["default"], 0)) > Parser.TryParse(parameters.IsArray ? parameters[2] : parameters["value"], 0);
                        return !not ? value : !value;
                    }
                case "StoryLoadBool":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["load"] == null)
                            break;

                        var value = StoryManager.inst.LoadBool(parameters.IsArray ? parameters[0] : parameters["load"], Parser.TryParse(parameters.IsArray ? parameters[1] : parameters["default"], false));
                        return !not ? value : !value;
                    }

                #endregion

                #region Layout

                case "LayoutChildCountEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                            break;

                        var isArray = parameters.IsArray;

                        var value = menuLayout.content.childCount == (isArray ? parameters[1].AsInt : parameters["count"].AsInt);
                        return !not ? value : !value;
                    }
                case "LayoutChildCountLesserEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                            break;

                        var isArray = parameters.IsArray;

                        var value = menuLayout.content.childCount <= (isArray ? parameters[1].AsInt : parameters["count"].AsInt);
                        return !not ? value : !value;
                    }
                case "LayoutChildCountGreaterEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                            break;

                        var isArray = parameters.IsArray;

                        var value = menuLayout.content.childCount >= (isArray ? parameters[1].AsInt : parameters["count"].AsInt);
                        return !not ? value : !value;
                    }
                case "LayoutChildCountLesser":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                            break;

                        var isArray = parameters.IsArray;

                        var value = menuLayout.content.childCount < (isArray ? parameters[1].AsInt : parameters["count"].AsInt);
                        return !not ? value : !value;
                    }
                case "LayoutChildCountGreater":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                            break;

                        var isArray = parameters.IsArray;

                        var value = menuLayout.content.childCount > (isArray ? parameters[1].AsInt : parameters["count"].AsInt);
                        return !not ? value : !value;
                    }

                case "LayoutScrollXEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                            break;

                        var isArray = parameters.IsArray;

                        var value = menuLayout.content.anchoredPosition.x == (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                        return !not ? value : !value;
                    }
                case "LayoutScrollXLesserEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                            break;

                        var isArray = parameters.IsArray;

                        var value = menuLayout.content.anchoredPosition.x <= (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                        return !not ? value : !value;
                    }
                case "LayoutScrollXGreaterEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                            break;

                        var isArray = parameters.IsArray;

                        var value = menuLayout.content.anchoredPosition.x >= (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                        return !not ? value : !value;
                    }
                case "LayoutScrollXLesser":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                            break;

                        var isArray = parameters.IsArray;

                        var value = menuLayout.content.anchoredPosition.x < (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                        return !not ? value : !value;
                    }
                case "LayoutScrollXGreater":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                            break;

                        var isArray = parameters.IsArray;

                        var value = menuLayout.content.anchoredPosition.x > (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                        return !not ? value : !value;
                    }

                case "LayoutScrollYEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                            break;

                        var isArray = parameters.IsArray;

                        var value = menuLayout.content.anchoredPosition.y == (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                        return !not ? value : !value;
                    }
                case "LayoutScrollYLesserEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                            break;

                        var isArray = parameters.IsArray;

                        var value = menuLayout.content.anchoredPosition.y <= (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                        return !not ? value : !value;
                    }
                case "LayoutScrollYGreaterEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                            break;

                        var isArray = parameters.IsArray;

                        var value = menuLayout.content.anchoredPosition.y >= (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                        return !not ? value : !value;
                    }
                case "LayoutScrollYLesser":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                            break;

                        var isArray = parameters.IsArray;

                        var value = menuLayout.content.anchoredPosition.y < (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                        return !not ? value : !value;
                    }
                case "LayoutScrollYGreater":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                            break;

                        var isArray = parameters.IsArray;

                        var value = menuLayout.content.anchoredPosition.y > (isArray ? parameters[1].AsFloat : parameters["count"].AsFloat);
                        return !not ? value : !value;
                    }

                #endregion

                #region LevelRanks

                case "ChapterFullyRanked":
                    {
                        var isArray = parameters.IsArray;
                        var chapter = Parser.TryParse(isArray ? parameters[0] : parameters["chapter"], 0);
                        var minRank = (isArray ? parameters.Count < 2 : parameters["min_rank"] == null) ? StoryManager.CHAPTER_RANK_REQUIREMENT :
                                    Parser.TryParse(isArray ? parameters[1] : parameters["min_rank"], 0);
                        var maxRank = (isArray ? parameters.Count < 3 : parameters["max_rank"] == null) ? 1 :
                                    Parser.TryParse(isArray ? parameters[2] : parameters["max_rank"], 0);
                        var bonus = (isArray ? parameters.Count < 4 : parameters["bonus"] == null) ? false :
                                    Parser.TryParse(isArray ? parameters[3] : parameters["bonus"], false);

                        var levelIDs = bonus ? StoryMode.Instance.bonusChapters : StoryMode.Instance.chapters;

                        var value =
                            chapter < levelIDs.Count &&
                            levelIDs[chapter].levels.All(x => x.bonus ||
                                            StoryManager.inst.Saves.TryFind(y => y.ID == x.id, out SaveData playerData) &&
                                            LevelManager.levelRankIndexes[LevelManager.GetLevelRank(playerData).name] >= maxRank &&
                                            LevelManager.levelRankIndexes[LevelManager.GetLevelRank(playerData).name] <= minRank);

                        return !not ? value : !value;
                    }
                case "LevelRankEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["rank"] == null)
                            break;

                        var value = LevelManager.CurrentLevel.saveData && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(LevelManager.CurrentLevel).name] == (parameters.IsArray ? parameters[0].AsInt : parameters["rank"].AsInt);
                        return !not ? value : !value;
                    }
                case "LevelRankLesserEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["rank"] == null)
                            break;

                        var value = LevelManager.CurrentLevel.saveData && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(LevelManager.CurrentLevel).name] <= (parameters.IsArray ? parameters[0].AsInt : parameters["rank"].AsInt);
                        return !not ? value : !value;
                    }
                case "LevelRankGreaterEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["rank"] == null)
                            break;

                        var value = LevelManager.CurrentLevel.saveData && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(LevelManager.CurrentLevel).name] >= (parameters.IsArray ? parameters[0].AsInt : parameters["rank"].AsInt);
                        return !not ? value : !value;
                    }
                case "LevelRankLesser":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["rank"] == null)
                            break;

                        var value = LevelManager.CurrentLevel.saveData && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(LevelManager.CurrentLevel).name] < (parameters.IsArray ? parameters[0].AsInt : parameters["rank"].AsInt);
                        return !not ? value : !value;
                    }
                case "LevelRankGreater":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["rank"] == null)
                            break;

                        var value = LevelManager.CurrentLevel.saveData && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(LevelManager.CurrentLevel).name] > (parameters.IsArray ? parameters[0].AsInt : parameters["rank"].AsInt);
                        return !not ? value : !value;
                    }
                case "StoryLevelRankEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["id"] == null || parameters["rank"] == null))
                            break;

                        var isArray = parameters.IsArray;
                        var id = isArray ? parameters[0].Value : parameters["id"].Value;

                        var value = StoryManager.inst.Saves.TryFind(x => x.ID == id, out SaveData playerData) && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(playerData).name] == (parameters.IsArray ? parameters[1].AsInt : parameters["rank"].AsInt);
                        return !not ? value : !value;
                    }
                case "StoryLevelRankLesserEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["id"] == null || parameters["rank"] == null))
                            break;

                        var isArray = parameters.IsArray;
                        var id = isArray ? parameters[0].Value : parameters["id"].Value;

                        var value = StoryManager.inst.Saves.TryFind(x => x.ID == id, out SaveData playerData) && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(playerData).name] <= (parameters.IsArray ? parameters[1].AsInt : parameters["rank"].AsInt);
                        return !not ? value : !value;
                    }
                case "StoryLevelRankGreaterEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["id"] == null || parameters["rank"] == null))
                            break;

                        var isArray = parameters.IsArray;
                        var id = isArray ? parameters[0].Value : parameters["id"].Value;

                        var value = StoryManager.inst.Saves.TryFind(x => x.ID == id, out SaveData playerData) && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(playerData).name] >= (parameters.IsArray ? parameters[1].AsInt : parameters["rank"].AsInt);
                        return !not ? value : !value;
                    }
                case "StoryLevelRankLesser":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["id"] == null || parameters["rank"] == null))
                            break;

                        var isArray = parameters.IsArray;
                        var id = isArray ? parameters[0].Value : parameters["id"].Value;

                        var value = StoryManager.inst.Saves.TryFind(x => x.ID == id, out SaveData playerData) && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(playerData).name] < (parameters.IsArray ? parameters[1].AsInt : parameters["rank"].AsInt);
                        return !not ? value : !value;
                    }
                case "StoryLevelRankGreater":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["id"] == null || parameters["rank"] == null))
                            break;

                        var isArray = parameters.IsArray;
                        var id = isArray ? parameters[0].Value : parameters["id"].Value;

                        var value = StoryManager.inst.Saves.TryFind(x => x.ID == id, out SaveData playerData) && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(playerData).name] > (parameters.IsArray ? parameters[1].AsInt : parameters["rank"].AsInt);
                        return !not ? value : !value;
                    }
                    #endregion
            }

            return false;
        }

        /// <summary>
        /// Parses a singular "func" JSON and performs an action based on the name and parameters.
        /// </summary>
        /// <param name="jn">The func JSON. Must have a name and a params array. If it has a "if_func", then it will parse and check if it's true.</param>
        public void ParseFunctionSingle(JSONNode jn, bool allowIfFunc = true, MenuImage thisElement = null)
        {
            if (jn["if_func"] != null && allowIfFunc)
            {
                if (!ParseIfFunction(jn["if_func"], thisElement))
                    return;
            }

            var parameters = jn["params"];
            string name = jn["name"];

            switch (name)
            {
                #region Main Functions

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
                case "LoadScene":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["scene"] == null)
                            break;

                        var isArcade = parameters.IsArray && parameters.Count >= 3 ? parameters[2].AsBool : parameters.IsObject ? parameters["is_arcade"].AsBool : false;

                        LevelManager.IsArcade = isArcade;

                        if (parameters.IsArray && parameters.Count >= 2 || parameters.IsObject && parameters["show_loading"] != null)
                            SceneManager.inst.LoadScene(parameters.IsArray ? parameters[0] : parameters["scene"], Parser.TryParse(parameters.IsArray ? parameters[1] : parameters["show_loading"], true));
                        else
                            SceneManager.inst.LoadScene(parameters.IsArray ? parameters[0] : parameters["scene"]);

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
                case "UpdateSettingBool":
                    {
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
                case "UpdateSettingInt":
                    {
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
                case "Wait":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["t"] == null || parameters["func"] == null))
                            break;

                        var isArray = parameters.IsArray;
                        var t = isArray ? parameters[0].AsFloat : parameters["t"].AsFloat;
                        JSONNode func = isArray ? parameters[1] : parameters["func"];

                        CoroutineHelper.PerformActionAfterSeconds(t, () =>
                        {
                            try
                            {
                                ParseFunction(func);
                            }
                            catch
                            {

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
                case "Log":
                    {
                        if (parameters != null && (parameters.IsArray && parameters.Count >= 1 || parameters.IsObject && parameters["msg"]))
                            CoreHelper.Log(parameters.IsArray ? parameters[0] : parameters["msg"]);

                        break;
                    }

                #endregion

                #region ExitGame

                // Exits the game.
                // Function has no parameters.
                case "ExitGame":
                    {
                        Application.Quit();
                        break;
                    }

                #endregion

                #region Config

                // Opens the Config Manager UI.
                // Function has no parameters.
                case "Config":
                    {
                        ConfigManager.inst.Show();
                        break;
                    }

                #endregion

                #endregion

                #region Interface

                #region Close

                // Closes the interface and returns to the game (if user is in the Game scene).
                // Function has no parameters.
                case "Close":
                    {
                        string id = CurrentInterface?.id;
                        CloseMenus();
                        StopMusic();

                        if (CoreHelper.InGame)
                        {
                            AudioManager.inst.CurrentAudioSource.UnPause();
                            GameManager.inst.gameState = GameManager.State.Playing;
                            interfaces.RemoveAll(x => x.id == id);
                        }

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
                case "SetCurrentInterface":
                    {
                        if (parameters != null && (parameters.IsArray && parameters.Count >= 1 || parameters.IsObject && parameters["id"] != null) &&
                                interfaces.TryFind(x => x.id == (parameters.IsArray ? parameters[0] : parameters["id"]), out MenuBase menu))
                        {
                            SetCurrentInterface(menu);
                            PlayMusic();
                        }

                        break;
                    }

                #endregion

                #region Reload

                // Reloads the interface and sets it to the main menu. Only recommended if you want to return to the main menu and unload every other interface.
                // Function has no parameters.
                case "Reload":
                    {
                        var splashTextPath = $"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}splashes.txt";
                        if (RTFile.FileExists(splashTextPath))
                        {
                            var splashes = RTString.GetLines(RTFile.ReadFromFile(splashTextPath));
                            var splashIndex = UnityEngine.Random.Range(0, splashes.Length);
                            LegacyPlugin.SplashText = splashes[splashIndex];
                        }
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
                // Example:
                // [
                //   "story_mode",
                //   "True",
                //   "{{BepInExAssetsDirectory}}Interfaces"
                // ]
                //
                // - JSON Object Structure -
                // "file"
                // "load"
                // "path"
                // Example:
                // {
                //   "file": "some_interface",
                //   "load": "False",
                //   "path": "beatmaps/interfaces" < doesn't need to exist
                // }
                case "Parse":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["file"] == null)
                            break;

                        if (parameters.IsArray && parameters.Count > 2 || parameters.IsObject && parameters["path"] != null)
                            MainDirectory = ParseText(parameters.IsArray ? parameters[2] : parameters["path"]);

                        if (!MainDirectory.Contains(RTFile.ApplicationDirectory))
                            MainDirectory = RTFile.CombinePaths(RTFile.ApplicationDirectory, MainDirectory);

                        var path = RTFile.CombinePaths(MainDirectory, $"{(parameters.IsArray ? parameters[0].Value : parameters["file"].Value)}{FileFormat.LSI.Dot()}");

                        if (!RTFile.FileExists(path))
                        {
                            CoreHelper.LogError($"Interface {(parameters.IsArray ? parameters[0] : parameters["file"])} does not exist!");

                            break;
                        }

                        var interfaceJN = JSON.Parse(RTFile.ReadFromFile(path));

                        var menu = CustomMenu.Parse(interfaceJN);
                        menu.filePath = path;

                        var load = parameters.IsArray && (parameters.Count < 2 || Parser.TryParse(parameters[1], false)) || parameters.IsObject && Parser.TryParse(parameters["load"], true);

                        if (interfaces.TryFind(x => x.id == menu.id, out MenuBase otherMenu))
                        {
                            if (load)
                            {
                                SetCurrentInterface(otherMenu);
                                PlayMusic();
                            }

                            break;
                        }

                        interfaces.Add(menu);

                        if (load)
                        {
                            SetCurrentInterface(menu);
                            PlayMusic();
                        }

                        break;
                    }

                #endregion

                #region ClearInterfaces

                // Clears all interfaces from the interfaces list.
                // Function has no parameters.
                case "ClearInterfaces":
                    {
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
                case "SetCurrentPath":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["path"] == null)
                            return;

                        MainDirectory = RTFile.ParsePaths(parameters.IsArray ? parameters[0] : parameters["path"]);

                        if (!MainDirectory.Contains(RTFile.ApplicationDirectory))
                            MainDirectory = RTFile.CombinePaths(RTFile.ApplicationDirectory, MainDirectory);

                        break;
                    }

                #endregion

                #endregion

                #region Audio

                #region PlaySound

                // Plays a sound. Can either be a default one already loaded in the SoundLibrary or a custom one from the menu's folder.
                // Supports both JSON array and JSON object.
                //
                // - JSON Array Structure -
                // 0 = sound
                // Example:
                // [
                //   "blip" < plays the blip sound.
                // ]
                //
                // - JSON Object Structure -
                // "sound"
                // Example:
                // {
                //   "sound": "some kind of sound.ogg" < since this sound does not exist in the SoundLibrary, search for a file with the name. If it exists, play the sound.
                // }
                case "PlaySound":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["sound"] == null || !CurrentInterface)
                            break;

                        string sound = parameters.IsArray ? parameters[0] : parameters["sound"];

                        if (SoundManager.inst.TryGetSound(sound, out AudioClip audioClip))
                        {
                            AudioManager.inst.PlaySound(audioClip);
                            break;
                        }

                        var filePath = $"{Path.GetDirectoryName(CurrentInterface.filePath)}{sound}";
                        if (!RTFile.FileExists(filePath))
                            return;

                        var audioType = RTFile.GetAudioType(filePath);
                        if (audioType == AudioType.MPEG)
                            AudioManager.inst.PlaySound(LSAudio.CreateAudioClipUsingMP3File(filePath));
                        else
                            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip($"file://{filePath}", audioType, AudioManager.inst.PlaySound));

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
                case "PlayMusic":
                    {
                        if (parameters == null || parameters.IsArray && (parameters.Count < 1 || parameters[0].Value.ToLower() == "default") || parameters.IsObject && (parameters["name"] == null || parameters["name"].Value.ToLower() == "default"))
                        {
                            PlayMusic();
                            break;
                        }

                        var isArray = parameters.IsArray;
                        var isObject = parameters.IsObject;
                        string music = isArray ? parameters[0] : parameters["name"];
                        var fadeDuration = 0.5f;
                        if (isArray && parameters.Count > 1 || isObject && parameters["fade_duration"] != null)
                            fadeDuration = isArray ? parameters[1].AsFloat : parameters["fade_duration"].AsFloat;

                        var loop = true;
                        if (isArray && parameters.Count > 2 || isObject && parameters["loop"] != null)
                            loop = isArray ? parameters[2].AsBool : parameters["loop"].AsBool;

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
                case "StopMusic":
                    {
                        StopMusic();
                        break;
                    }

                #endregion

                #region PauseMusic

                // Pauses the current music if it's currently playing.
                case "PauseMusic":
                    {
                        if (CoreHelper.InGame && parameters != null && (parameters.IsArray && !parameters[0].AsBool || parameters.IsObject && !parameters["game_audio"].AsBool))
                            CurrentAudioSource.Pause();
                        else
                            AudioManager.inst.CurrentAudioSource.Pause();

                        break;
                    }

                #endregion

                #region ResumeMusic

                // Resumes the current music if it was paused.
                case "ResumeMusic":
                    {
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

                case "Move":
                    {
                        if (!gameObject || parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["x"] == null || parameters["y"] == null))
                            break;

                        JSONNode jnX = parameters.IsArray ? parameters[0] : parameters["x"];
                        JSONNode jnY = parameters.IsArray ? parameters[1] : parameters["y"];

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
                case "SetElementActive":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && parameters["id"] == null || !CurrentInterface)
                            return;

                        if (CurrentInterface.elements.TryFind(x => x.id == (parameters.IsArray ? parameters[0] : parameters["id"]), out MenuImage menuImage) &&
                            menuImage.gameObject && bool.TryParse(parameters.IsArray ? parameters[1] : parameters["active"], out bool active))
                        {
                            menuImage.gameObject.SetActive(active);
                        }

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
                case "SetLayoutActive":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && parameters["name"] == null || !CurrentInterface)
                            return;

                        if (CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["name"], out MenuLayoutBase layout) &&
                            layout.gameObject && bool.TryParse(parameters.IsArray ? parameters[1] : parameters["active"], out bool active))
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
                case "AnimateID":
                    {
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

                // Same as animate ID, except instead of searching for an elements' ID, you search for a name.
                // No example needed.
                case "AnimateName": // in case you'd rather find an objects' name instead of ID.
                    {
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
                case "StopAnimations":
                    {
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
                case "SetColor":
                    {
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
                case "SetText":
                    {
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
                case "RemoveElement":
                    {
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
                case "RemoveMultipleElements":
                    {
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
                case "AddElement":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["elements"] == null)
                            break;

                        var customMenu = CurrentInterface;
                        customMenu.elements.AddRange(CustomMenu.ParseElements(parameters.IsArray ? parameters[0] : parameters["elements"], customMenu.prefabs, customMenu.spriteAssets));

                        customMenu.StartGeneration();

                        break;
                    }

                #endregion

                #region ScrollLayout

                case "ScrollLayout":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["layout"] == null || !CurrentInterface.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["layout"], out MenuLayoutBase menuLayout) || !menuLayout.scrollable)
                            break;

                        var isArray = parameters.IsArray;

                        if (menuLayout is MenuGridLayout menuGridLayout)
                            menuGridLayout.Scroll(isArray ? parameters[1].AsFloat : parameters["x"].AsFloat, isArray ? parameters[2].AsFloat : parameters["y"], isArray ? parameters[3].AsBool : parameters["x_additive"].AsBool, isArray ? parameters[4].AsBool : parameters["y_additive"].AsBool);

                        if (menuLayout is MenuHorizontalLayout menuHorizontalLayout)
                            menuHorizontalLayout.Scroll(isArray ? parameters[1].AsFloat : parameters["value"].AsFloat, isArray ? parameters[2].AsBool : parameters["additive"].AsBool);

                        if (menuLayout is MenuVerticalLayout menuVerticalLayout)
                            menuVerticalLayout.Scroll(isArray ? parameters[1].AsFloat : parameters["value"].AsFloat, isArray ? parameters[2].AsBool : parameters["additive"].AsBool);

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
                case "SetElementSelectable":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && parameters["id"] == null || !CurrentInterface)
                            return;

                        if (CurrentInterface.elements.TryFind(x => x.id == (parameters.IsArray ? parameters[0] : parameters["id"]), out MenuImage menuImage) &&
                            bool.TryParse(parameters.IsArray ? parameters[1] : parameters["selectable"], out bool selectable))
                        {
                            menuImage.selectable = selectable;
                        }

                        break;
                    }

                #endregion

                #endregion

                #region Effects

                #region SetDefaultEvents

                case "SetDefaultEvents":
                    {
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
                case "AnimateEvent":
                    {
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
                            case "MoveCamera":
                                {
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
                            case "ZoomCamera":
                                {
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
                            case "RotateCamera":
                                {
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
                            case "Chromatic":
                                {
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
                            case "Bloom":
                                {
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
                            case "LensDistort":
                                {
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

                case "UpdateEvent":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["effect"] == null || !MenuEffectsManager.inst || !MenuEffectsManager.inst.functions.TryGetValue(parameters.IsArray ? parameters[0] : parameters["effect"], out Action<float> action))
                            break;

                        action?.Invoke(parameters.IsArray ? parameters[1].AsFloat : parameters["amount"].AsFloat);

                        break;
                    }

                #endregion

                #region SetEvent

                case "SetEvent":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["type"] == null)
                            break;

                        var isArray = parameters.IsArray;
                        var type = isArray ? parameters[0] : parameters["type"];

                        if (type.IsNumber)
                            break;

                        var values = isArray ? parameters[1] : parameters["values"];

                        switch (type.Value)
                        {
                            case "MoveCamera":
                                {
                                    if (values["x"] != null)
                                    {
                                        MenuEffectsManager.inst.MoveCameraX(values["x"].AsFloat);
                                    }

                                    if (values["y"] != null)
                                    {
                                        MenuEffectsManager.inst.MoveCameraX(values["y"].AsFloat);
                                    }

                                    break;
                                }
                            case "ZoomCamera":
                                {
                                    if (values["amount"] != null)
                                    {
                                        MenuEffectsManager.inst.ZoomCamera(values["amount"].AsFloat);
                                    }

                                    break;
                                }
                            case "RotateCamera":
                                {
                                    if (values["amount"] != null)
                                    {
                                        MenuEffectsManager.inst.RotateCamera(values["amount"].AsFloat);
                                    }

                                    break;
                                }
                            case "Chroma":
                            case "Chromatic":
                                {
                                    if (values["intensity"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateChroma(values["intensity"].AsFloat);
                                    }

                                    break;
                                }
                            case "Bloom":
                                {
                                    if (values["intensity"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateBloomIntensity(values["intensity"].AsFloat);
                                    }

                                    if (values["diffusion"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateBloomDiffusion(values["diffusion"].AsFloat);
                                    }

                                    if (values["threshold"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateBloomThreshold(values["threshold"].AsFloat);
                                    }

                                    if (values["anamorphic_ratio"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateBloomAnamorphicRatio(values["anamorphic_ratio"].AsFloat);
                                    }

                                    if (values["col"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateBloomColor(CurrentInterface.Theme.GetFXColor(values["col"].AsInt));
                                    }

                                    break;
                                }
                            case "Lens":
                            case "LensDistort":
                                {
                                    if (values["intensity"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateLensDistortIntensity(values["intensity"].AsFloat);
                                    }

                                    if (values["center_x"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateLensDistortCenterX(values["center_x"].AsFloat);
                                    }

                                    if (values["center_y"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateLensDistortCenterY(values["center_y"].AsFloat);
                                    }

                                    if (values["intensity_x"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateLensDistortIntensityX(values["intensity_x"].AsFloat);
                                    }

                                    if (values["intensity_y"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateLensDistortIntensityY(values["intensity_y"].AsFloat);
                                    }

                                    if (values["scale"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateLensDistortScale(values["scale"].AsFloat);
                                    }

                                    break;
                                }
                            case "Vignette":
                                {
                                    if (values["intensity"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateVignetteIntensity(values["intensity"].AsFloat);
                                    }

                                    if (values["center_x"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateVignetteCenterX(values["center_x"].AsFloat);
                                    }

                                    if (values["center_y"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateVignetteCenterY(values["center_y"].AsFloat);
                                    }

                                    if (values["smoothness"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateVignetteSmoothness(values["smoothness"].AsFloat);
                                    }

                                    if (values["roundness"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateVignetteRoundness(values["roundness"].AsFloat);
                                    }

                                    if (values["rounded"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateVignetteRounded(values["rounded"].AsBool);
                                    }

                                    if (values["col"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateVignetteColor(CurrentInterface.Theme.GetFXColor(values["col"].AsInt));
                                    }

                                    break;
                                }
                            case "AnalogGlitch":
                                {
                                    if (values["enabled"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateAnalogGlitchEnabled(values["enabled"].AsBool);
                                    }

                                    if (values["scan_line_jitter"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateAnalogGlitchScanLineJitter(values["scan_line_jitter"].AsFloat);
                                    }

                                    if (values["vertical_jump"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateAnalogGlitchVerticalJump(values["vertical_jump"].AsFloat);
                                    }

                                    if (values["horizontal_shake"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateAnalogGlitchHorizontalShake(values["horizontal_shake"].AsFloat);
                                    }

                                    if (values["color_drift"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateAnalogGlitchColorDrift(values["color_drift"].AsFloat);
                                    }

                                    break;
                                }
                            case "DigitalGlitch":
                                {
                                    if (values["intensity"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateDigitalGlitch(values["intensity"].AsFloat);
                                    }

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
                case "LoadLevel":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["id"] == null)
                            break;

                        if (LevelManager.Levels.TryFind(x => x.id == (parameters.IsArray ? parameters[0] : parameters["id"]), out Level level))
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
                case "InitLevelMenu":
                    {
                        var directory = MainDirectory;
                        if (parameters != null && (parameters.IsArray && parameters.Count > 0 || parameters["directory"] != null))
                            directory = parameters.IsArray ? parameters[1] : parameters["directory"];
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
                case "SetDiscordStatus":
                    {
                        if (parameters == null)
                            return;

                        if (parameters.IsObject)
                        {
                            CoreHelper.UpdateDiscordStatus(parameters["state"], parameters["details"], parameters["icon"], parameters["art"] != null ? parameters["art"] : "pa_logo_white");

                            return;
                        }

                        if (parameters.IsArray && parameters.Count < 1)
                            return;

                        CoreHelper.UpdateDiscordStatus(parameters[0], parameters[1], parameters[2], parameters.Count > 3 ? parameters[3] : "pa_logo_white");

                        break;
                    }

                #endregion

                #region OpenLink

                case "OpenLink":
                    {
                        var linkType = Parser.TryParse(parameters.IsArray ? parameters[0] : parameters["link_type"], URLSource.Artist);

                        var url = AlephNetwork.GetURL(linkType, parameters.IsArray ? parameters[1].AsInt : parameters["site"], parameters.IsArray ? parameters[2] : parameters["link"]);

                        Application.OpenURL(url);

                        break;
                    }

                #endregion

                #endregion

                #region Specific Functions

                case "OpenChangelog":
                    {
                        OpenChangelog();
                        break;
                    }

                #region LoadLevels

                case "LoadLevels":
                    {
                        LoadLevelsMenu.Init(() =>
                        {
                            if (parameters["on_loading_end"] != null)
                                ParseFunction(parameters["on_loading_end"], thisElement);
                        });
                        break;
                    }

                #endregion

                #region OnInputsSelected

                case "OnInputsSelected":
                    {
                        InputSelectMenu.OnInputsSelected = () =>
                        {
                            if (parameters["continue"] != null)
                                ParseFunction(parameters["continue"], thisElement);
                        };
                        break;
                    }

                #endregion

                #region Profile

                case "Profile":
                    {
                        CloseMenus();
                        var profileMenu = new ProfileMenu();
                        CurrentInterface = profileMenu;

                        break;
                    }

                #endregion

                #region BeginStoryMode

                // Begins the BetterLegacy story mode.
                // Function has no parameters.
                case "BeginStoryMode":
                    {
                        LevelManager.IsArcade = false;
                        SceneHelper.LoadInputSelect(SceneHelper.LoadInterfaceScene);

                        break;
                    }

                #endregion

                #region LoadStoryLevel

                case "LoadStoryLevel":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && (parameters["chapter"] == null || parameters["level"] == null))
                            return;

                        var isArray = parameters.IsArray;
                        var chapter = isArray ? parameters[0].AsInt : parameters["chapter"].AsInt;
                        var level = isArray ? parameters[1].AsInt : parameters["level"].AsInt;
                        var bonus = isArray && parameters.Count > 3 ? parameters[3].AsBool : parameters["bonus"] != null ? parameters["bonus"].AsBool : false;
                        var skipCutscenes = isArray && parameters.Count > 4 ? parameters[4].AsBool : parameters["skip_cutscenes"] != null ? parameters["skip_cutscenes"].AsBool : true;

                        StoryManager.inst.ContinueStory = isArray && parameters.Count > 2 && parameters[2].AsBool || parameters.IsObject && parameters["continue"].AsBool;

                        ArcadeHelper.ResetModifiedStates();
                        StoryManager.inst.Play(chapter, level, bonus, skipCutscenes);

                        break;
                    }

                #endregion

                #region LoadStoryLevelPath

                case "LoadStoryLevelPath":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && (parameters["chapter"] == null || parameters["level"] == null))
                            return;

                        var isArray = parameters.IsArray;
                        var path = isArray ? parameters[0] : parameters["path"];
                        var songName = isArray ? parameters[1] : parameters["song"];

                        StoryManager.inst.ContinueStory = isArray && parameters.Count > 2 && parameters[2].AsBool || parameters.IsObject && parameters["continue"].AsBool;

                        ArcadeHelper.ResetModifiedStates();
                        StoryManager.inst.Play(path, songName);

                        break;
                    }

                #endregion

                #region LoadNextStoryLevel

                case "LoadNextStoryLevel":
                    {
                        var isArray = parameters.IsArray;

                        StoryManager.inst.ContinueStory = true;

                        int chapter = StoryManager.inst.ChapterIndex;
                        StoryManager.inst.Play(chapter, StoryManager.inst.LoadInt($"DOC{RTString.ToStoryNumber(chapter)}Progress", 0), StoryManager.inst.inBonusChapter);

                        break;
                    }

                #endregion

                #region LoadCurrentStoryInterface

                case "LoadCurrentStoryInterface":
                    {
                        StartupStoryInterface();

                        break;
                    }

                #endregion

                #region LoadStoryInterface

                case "LoadStoryInterface":
                    {
                        StartupStoryInterface(parameters.IsArray ? parameters[0].AsInt : parameters["chapter"].AsInt, parameters.IsArray ? parameters[1].AsInt : parameters["level"].AsInt);

                        break;
                    }

                #endregion

                #region LoadChapterTransition

                case "LoadChapterTransition":
                    {
                        var isArray = parameters.IsArray;

                        StoryManager.inst.ContinueStory = true;

                        var chapter = StoryManager.inst.ChapterIndex;
                        StoryManager.inst.Play(chapter, StoryMode.Instance.chapters[chapter].Count, StoryManager.inst.inBonusChapter);

                        break;
                    }

                #endregion

                #region StorySaveBool

                case "StorySaveBool":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["name"] == null || parameters["value"] == null))
                            break;

                        var isArray = parameters.IsArray;
                        string saveName = isArray ? parameters[0] : parameters["name"];
                        var value = isArray ? parameters[1].AsBool : parameters["value"].AsBool;
                        if (isArray ? parameters.Count > 2 && parameters[2].AsBool : parameters["toggle"] != null && parameters["toggle"].AsBool)
                            value = !StoryManager.inst.LoadBool(saveName, value);

                        StoryManager.inst.SaveBool(saveName, value);

                        break;
                    }

                #endregion

                #region StorySaveInt

                case "StorySaveInt":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["name"] == null || parameters["value"] == null))
                            break;

                        var isArray = parameters.IsArray;
                        string saveName = isArray ? parameters[0] : parameters["name"];
                        var value = isArray ? parameters[1].AsInt : parameters["value"].AsInt;
                        if (isArray ? parameters.Count > 2 && parameters[2].AsBool : parameters["relative"] != null && parameters["relative"].AsBool)
                            value += StoryManager.inst.LoadInt(saveName, value);

                        StoryManager.inst.SaveInt(saveName, value);

                        break;
                    }

                #endregion

                #region StorySaveFloat

                case "StorySaveFloat":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["name"] == null || parameters["value"] == null))
                            break;

                        var isArray = parameters.IsArray;
                        string saveName = isArray ? parameters[0] : parameters["name"];
                        var value = isArray ? parameters[1].AsFloat : parameters["value"].AsFloat;
                        if (isArray ? parameters.Count > 2 && parameters[2].AsBool : parameters["relative"] != null && parameters["relative"].AsBool)
                            value += StoryManager.inst.LoadFloat(saveName, value);

                        StoryManager.inst.SaveFloat(saveName, value);

                        break;
                    }

                #endregion

                #region StorySaveString

                case "StorySaveString":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["name"] == null || parameters["value"] == null))
                            break;

                        var isArray = parameters.IsArray;
                        string saveName = isArray ? parameters[0] : parameters["name"];
                        var value = isArray ? parameters[1].Value : parameters["value"].Value;
                        if (isArray ? parameters.Count > 2 && parameters[2].AsBool : parameters["relative"] != null && parameters["relative"].AsBool)
                            value += StoryManager.inst.LoadString(saveName, value);

                        StoryManager.inst.SaveString(saveName, value);

                        break;
                    }

                #endregion

                #region StorySaveJSON

                case "StorySaveJSON":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["name"] == null || parameters["value"] == null))
                            break;

                        var isArray = parameters.IsArray;
                        string saveName = isArray ? parameters[0] : parameters["name"];
                        var value = isArray ? parameters[1] : parameters["value"];

                        StoryManager.inst.SaveNode(saveName, value);

                        break;
                    }

                #endregion

                #region LoadStorySaveJSONText

                case "LoadStorySaveStringText":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["name"] == null || parameters["value"] == null) || !thisElement)
                            break;

                        var isArray = parameters.IsArray;
                        string saveName = isArray ? parameters[0] : parameters["name"];

                        var text = StoryManager.inst.LoadString(saveName, "");

                        if (thisElement is MenuText menuText)
                            menuText.text = text;

                        break;
                    }

                #endregion

                #region ModderDiscord

                // Opens the System Error Discord server link.
                // Function has no parameters.
                case "ModderDiscord":
                    {
                        Application.OpenURL(AlephNetwork.MOD_DISCORD_URL);

                        break;
                    }

                    #endregion

                #endregion
            }
        }

        #endregion
    }
}
