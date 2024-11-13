using BetterLegacy.Components;
using BetterLegacy.Core;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BetterLegacy.Core.Data;
using System.Collections;

using BetterLegacy.Menus.UI;
using SimpleJSON;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Configs;
using System.IO;
using BetterLegacy.Menus.UI.Interfaces;
using LSFunctions;
using System.Text.RegularExpressions;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Layouts;
using BetterLegacy.Arcade;
using BetterLegacy.Patchers;
using BetterLegacy.Story;

namespace BetterLegacy.Menus
{
    public class InterfaceManager : MonoBehaviour
    {
        public static InterfaceManager inst;
        public static void Init() => Creator.NewGameObject(nameof(InterfaceManager), SystemManager.inst.transform).AddComponent<InterfaceManager>();

        public float[] samples = new float[256];

        public const string RANDOM_MUSIC_NAME = "menu";
        public const string MAIN_MENU_ID = "0";
        public const string STORY_SAVES_MENU_ID = "1";
        public const string CHAPTER_SELECT_MENU_ID = "2";
        public const string PROFILE_MENU_ID = "3";

        public MenuBase CurrentMenu { get; set; }
        public Coroutine CurrentGenerateUICoroutine { get; set; }

        public List<MenuBase> interfaces = new List<MenuBase>();

        public AudioSource CurrentAudioSource { get; set; }

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
            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/interfaces"))
                Directory.CreateDirectory(RTFile.ApplicationDirectory + "beatmaps/interfaces");
        }

        void Update()
        {
            if (CurrentAudioSource.isPlaying)
                CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);
            else
                AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

            CurrentAudioSource.volume = DataManager.inst.GetSettingInt("MusicVolume", 9) / 9f * AudioManager.inst.masterVol;

            if (CurrentMenu == null)
                return;

            CurrentMenu.UpdateControls();
            CurrentMenu.UpdateTheme();
        }

        public void PlayMusic(AudioClip music, bool allowSame = false)
        {
            if (CoreHelper.InEditor)
                return;

            if (!CoreHelper.InGame)
            {
                AudioManager.inst.PlayMusic(music.name, music);
                return;
            }

            if (!CurrentAudioSource.clip || allowSame || music.name != CurrentAudioSource.clip.name)
                CurrentAudioSource.clip = music;

            if (!CurrentAudioSource.clip)
                return;

            CoreHelper.Log("Playing music");
            CurrentAudioSource.UnPause();
            CurrentAudioSource.time = 0f;
            CurrentAudioSource.Play();
        }

        public void StopMusic()
        {
            if (!CoreHelper.InGame)
            {
                AudioManager.inst.StopMusic();
                return;
            }

            CurrentAudioSource.Stop();
            CurrentAudioSource.clip = null;
        }

        public int randomIndex = -1;
        public void PlayMusic()
        {
            var directory = RTFile.ApplicationDirectory + "settings/menus/";

            if (!MenuConfig.Instance.PlayCustomMusic.Value)
            {
                CoreHelper.LogWarning("PlayCustomMusic setting is off, so play default music.");
                CurrentMenu?.PlayDefaultMusic();
                return;
            }

            if (CurrentMenu != null && !CurrentMenu.allowCustomMusic)
            {
                CoreHelper.LogWarning("CurrentMenu does not allow custom music.");
                CurrentMenu?.PlayDefaultMusic();
                return;
            }

            switch (MenuConfig.Instance.MusicLoadMode.Value)
            {
                case MenuMusicLoadMode.StoryFolder:
                    {
                        directory = RTFile.ApplicationDirectory + "beatmaps/story";
                        break;
                    }
                case MenuMusicLoadMode.EditorFolder:
                    {
                        directory = RTFile.ApplicationDirectory + "beatmaps/editor";
                        break;
                    }
                case MenuMusicLoadMode.GlobalFolder:
                    {
                        directory = MenuConfig.Instance.MusicGlobalPath.Value;
                        break;
                    }
            }

            if (!RTFile.DirectoryExists(directory))
            {
                CoreHelper.LogWarning("Directory does not exist, so play default music.");
                CurrentMenu?.PlayDefaultMusic();
                return;
            }

            string oggSearchPattern = "*.ogg";
            string wavSearchPattern = "*.wav";
            string mp3SearchPattern = "*.mp3";
            if (MenuConfig.Instance.MusicLoadMode.Value == MenuMusicLoadMode.StoryFolder || MenuConfig.Instance.MusicLoadMode.Value == MenuMusicLoadMode.EditorFolder)
            {
                oggSearchPattern = "level.ogg";
                wavSearchPattern = "level.wav";
                mp3SearchPattern = "level.mp3";
            }

            var oggFiles = Directory.GetFiles(directory, oggSearchPattern, SearchOption.AllDirectories);
            var wavFiles = Directory.GetFiles(directory, wavSearchPattern, SearchOption.AllDirectories);
            var mp3Files = Directory.GetFiles(directory, mp3SearchPattern, SearchOption.AllDirectories);

            var songFiles = oggFiles.Union(wavFiles).Union(mp3Files).ToArray();

            if (songFiles.Length < 1)
            {
                CoreHelper.LogWarning("No song files, so play default music.");
                CurrentMenu?.PlayDefaultMusic();
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
                CurrentMenu?.PlayDefaultMusic();
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
                CoreHelper.Log($"Attempting to play music: {songFileCurrent}");
                var audioClip = LSAudio.CreateAudioClipUsingMP3File(songFileCurrent);
                audioClip.name = name;
                if (CurrentMenu != null)
                    CurrentMenu.music = audioClip;
                PlayMusic(audioClip);
                return;
            }

            CoreHelper.StartCoroutine(AlephNetworkManager.DownloadAudioClip($"file://{songFileCurrent}", audioType, audioClip =>
            {
                if (CoreHelper.InEditor)
                    return;

                CoreHelper.Log($"Attempting to play music: {songFileCurrent}");
                audioClip.name = name;
                if (CurrentMenu != null)
                    CurrentMenu.music = audioClip;
                PlayMusic(audioClip);
            }));
        }

        public void SetCurrentInterface(string id)
        {
            if (interfaces.TryFind(x => x.id == id, out MenuBase menu))
            {
                CloseMenus();
                CurrentMenu = menu;
                CurrentGenerateUICoroutine = StartCoroutine(menu.GenerateUI());
            }
        }

        public List<BeatmapTheme> themes = new List<BeatmapTheme>();

        public void CloseMenus()
        {
            CurrentMenu?.Clear();
            CurrentMenu = null;
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

        public void Clear(bool clearThemes = true, bool stopGenerating = true)
        {
            if (CurrentMenu != null)
            {
                CurrentMenu.Clear();
                CurrentMenu = null;
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

        public void StopGenerating()
        {
            if (CurrentGenerateUICoroutine == null)
                return;

            StopCoroutine(CurrentGenerateUICoroutine);
            CurrentGenerateUICoroutine = null;
        }

        public void LoadThemes()
        {
            themes.Clear();
            var jn = JSON.Parse(RTFile.ReadFromFile($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}Interfaces/default_themes.lst"));
            for (int i = 0; i < jn["themes"].Count; i++)
                themes.Add(BeatmapTheme.Parse(jn["themes"][i]));

            if (!RTFile.DirectoryExists($"{RTFile.ApplicationDirectory}beatmaps/interfaces/themes"))
                return;

            var files = Directory.GetFiles($"{RTFile.ApplicationDirectory}beatmaps/interfaces/themes");
            for (int i = 0; i < files.Length; i++)
            {
                jn = JSON.Parse(RTFile.ReadFromFile(files[i]));
                themes.Add(BeatmapTheme.Parse(jn));
            }
        }

        public void StartupStoryInterface(int chapterIndex, int levelIndex)
        {
            Clear(false, false);
            CoreHelper.InStory = true;

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

        public void Parse(string path)
        {
            var jn = JSON.Parse(RTFile.ReadFromFile(path));

            var menu = CustomMenu.Parse(jn);
            menu.filePath = path;
            interfaces.Add(menu);

            SetCurrentInterface(menu.id);
            PlayMusic();
        }

        public Action onReturnToStoryInterface;

        public void StartupStoryInterface() => StartupStoryInterface(StoryManager.inst.currentPlayingChapterIndex, StoryManager.inst.currentPlayingLevelSequenceIndex);

        public void StartupInterface()
        {
            Clear(false, false);
            CoreHelper.InStory = false;

            Parse($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}Interfaces/main_menu.lsi");

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
            CoreHelper.Log($"Is loading scene: {SceneManagerPatch.loading}");

            yield return StartCoroutine(AlephNetworkManager.DownloadJSONFile("https://raw.githubusercontent.com/RTMecha/BetterLegacy/master/updates.lss", x =>
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

                var lines = CoreHelper.GetLines(x);

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
                    func = () => { SetCurrentInterface("0"); },
                    opacity = 0.1f,
                    selectedOpacity = 1f,
                    color = 6,
                    selectedColor = 6,
                    textColor = 6,
                    selectedTextColor = 7,
                    length = 1f,
                });

                CurrentMenu = changeLogMenu;
                CurrentGenerateUICoroutine = StartCoroutine(changeLogMenu.GenerateUI());
                PlayMusic();

                if (CoreHelper.InEditor || SceneManagerPatch.loading)
                {
                    CloseMenus();
                }

                ChangeLogMenu.Seen = true;
            }, onError =>
            {
                CoreHelper.LogError($"Couldn't reach updates file, continuing...\nError: {onError}");
                SetCurrentInterface(MAIN_MENU_ID);
                PlayMusic();
            }));
            yield break;
        }
    }

    public class ChangeLogMenu : MenuBase
    {
        public ChangeLogMenu() : base()
        {
            musicName = InterfaceManager.RANDOM_MUSIC_NAME;
            exitFunc = () => { InterfaceManager.inst.SetCurrentInterface(InterfaceManager.MAIN_MENU_ID); };
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

        public override void UpdateTheme()
        {
            if (Parser.TryParse(MenuConfig.Instance.InterfaceThemeID.Value, -1) >= 0 && InterfaceManager.inst.themes.TryFind(x => x.id == MenuConfig.Instance.InterfaceThemeID.Value, out BeatmapTheme interfaceTheme))
                Theme = interfaceTheme;
            else
                Theme = InterfaceManager.inst.themes[0];

            base.UpdateTheme();
        }
    }
}
