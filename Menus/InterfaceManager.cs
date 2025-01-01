using BetterLegacy.Core;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BetterLegacy.Core.Data;
using System.Collections;
using SimpleJSON;
using BetterLegacy.Configs;
using System.IO;
using BetterLegacy.Menus.UI.Interfaces;
using LSFunctions;
using System.Text.RegularExpressions;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Layouts;
using BetterLegacy.Story;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Arcade.Interfaces;

namespace BetterLegacy.Menus
{
    /// <summary>
    /// Manages all things related to PA's interfaces.
    /// </summary>
    public class InterfaceManager : MonoBehaviour
    {
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

        #region Constants

        /// <summary>
        /// The normal menu music group.
        /// </summary>
        public const string RANDOM_MUSIC_NAME = "menu";
        public const string MAIN_MENU_ID = "0";
        public const string STORY_SAVES_MENU_ID = "1";
        public const string CHAPTER_SELECT_MENU_ID = "2";
        public const string PROFILE_MENU_ID = "3";

        #endregion

        void Awake()
        {
            inst = this;

            CurrentAudioSource = gameObject.AddComponent<AudioSource>();
            CurrentAudioSource.loop = true;
            var path = RTFile.ApplicationDirectory + "beatmaps/interfaces/";
            MainDirectory = path;
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

            CurrentInterface.UpdateControls();
            CurrentInterface.UpdateTheme();
        }

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

            var directory = MenuConfig.Instance.MusicLoadMode.Value switch
            {
                MenuMusicLoadMode.ArcadeFolder => $"{RTFile.ApplicationDirectory}{LevelManager.ListPath}",
                MenuMusicLoadMode.StoryFolder => $"{RTFile.ApplicationDirectory}beatmaps/story",
                MenuMusicLoadMode.EditorFolder => $"{RTFile.ApplicationDirectory}beatmaps/editor",
                MenuMusicLoadMode.InterfacesFolder => $"{RTFile.ApplicationDirectory}beatmaps/interfaces/music",
                MenuMusicLoadMode.GlobalFolder => MenuConfig.Instance.MusicGlobalPath.Value,
                _ => RTFile.ApplicationDirectory + "settings/menus",
            };

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

            CoreHelper.StartCoroutine(AlephNetwork.DownloadAudioClip($"file://{songFileCurrent}", audioType, audioClip =>
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
        public static float InterfaceSpeed => SpeedUp ? MenuConfig.Instance.SpeedUpSpeedMultiplier.Value : MenuConfig.Instance.RegularSpeedMultiplier.Value;

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
            if (Example.ExampleManager.inst)
                Example.ExampleManager.inst.SetActive(true); // if Example was disabled

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
            if (Example.ExampleManager.inst)
                Example.ExampleManager.inst.SetActive(true); // if Example was disabled

            Parse(RTFile.GetAsset($"Interfaces/main_menu{FileFormat.LSI.Dot()}"));

            interfaces.Add(new StoryMenu());

            if (!MenuConfig.Instance.ShowChangelog.Value || ChangeLogMenu.Seen)
            {
                SetCurrentInterface(MAIN_MENU_ID);
                PlayMusic();
                return;
            }

            try
            {
                StartCoroutine(IStartupInterface());
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Error: {ex}");
            }
        }

        IEnumerator IStartupInterface()
        {
            CoreHelper.Log($"Is loading scene: {SceneHelper.Loading}");

            yield return StartCoroutine(AlephNetwork.DownloadJSONFile(ChangeLogMenu.UPDATE_NOTES_URL, x =>
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

                var lines = RTString.GetLines(x);

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];

                    var regex = new Regex(@"(.*?) > \[(.*?) ([0-9]+), ([0-9]+)]");
                    var match = regex.Match(line);
                    if (match.Success)
                    {
                        line = line.Replace(match.Groups[0].ToString(), $"<b>{match.Groups[0]}</b>");

                        if (i != 0)
                            break; // only current update should show.
                    }

                    changeLogMenu.AddUpdateNote(line);
                }

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

                if (CoreHelper.InEditor || SceneHelper.Loading)
                    CloseMenus();

                ChangeLogMenu.Seen = true;
            }, onError =>
            {
                CoreHelper.LogError($"Couldn't reach updates file, continuing...\nError: {onError}");
                SetCurrentInterface(MAIN_MENU_ID);
                PlayMusic();
            }));
            yield break;
        }

        #endregion

        #region Themes

        /// <summary>
        /// The theme that should be used by the current interface.
        /// </summary>
        public BeatmapTheme CurrentTheme =>
            Parser.TryParse(MenuConfig.Instance.InterfaceThemeID.Value, -1) >= 0 && themes.TryFind(x => x.id == MenuConfig.Instance.InterfaceThemeID.Value, out BeatmapTheme interfaceTheme) ?
                interfaceTheme : themes != null && themes.Count > 0 ? themes[0] : DataManager.inst.BeatmapThemes[1] as BeatmapTheme;

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

        // todo: move functions and text parsing here
        #region Interface Functions



        #endregion
    }

    public class ChangeLogMenu : MenuBase
    {
        /// <summary>
        /// The URL for the BetterLegacy update notes.
        /// </summary>
        public const string UPDATE_NOTES_URL = "https://raw.githubusercontent.com/RTMecha/BetterLegacy/master/updates.lss";

        public ChangeLogMenu() : base()
        {
            musicName = InterfaceManager.RANDOM_MUSIC_NAME;
            exitFunc = () => InterfaceManager.inst.SetCurrentInterface(InterfaceManager.MAIN_MENU_ID);
        }

        public static bool Seen { get; set; }

        public void AddUpdateNote(string note)
        {
            elements.Add(new MenuText
            {
                id = LSText.randomNumString(16),
                name = "Update Note",
                text = note,
                parentLayout = "updates",
                rect = RectValues.Default.SizeDelta(0f, 36f),
                hideBG = true,
                textColor = 6
            });
        }
    }
}
