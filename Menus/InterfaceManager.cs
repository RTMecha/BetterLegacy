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

namespace BetterLegacy.Menus
{
    public class InterfaceManager : MonoBehaviour
    {
        public static InterfaceManager inst;
        public static void Init() => Creator.NewGameObject(nameof(InterfaceManager), SystemManager.inst.transform).AddComponent<InterfaceManager>();

        public float[] samples = new float[256];

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
            if (!CoreHelper.InGame)
            {
                AudioManager.inst.PlayMusic(null, music);
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

            if (!MenuConfig.Instance.PlayCustomMusic.Value || CurrentMenu != null && !CurrentMenu.allowCustomMusic)
            {
                CoreHelper.LogWarning("PlayCustomMusic setting is off, so play default music.");
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

            if (CurrentAudioSource.clip && CurrentAudioSource.clip.name == Path.GetFileName(songFileCurrent) || AudioManager.inst.CurrentAudioSource.clip && AudioManager.inst.CurrentAudioSource.clip.name == Path.GetFileName(songFileCurrent))
                return;

            var audioType = RTFile.GetAudioType(songFileCurrent);
            if (audioType == AudioType.MPEG)
            {
                CoreHelper.Log($"Attempting to play music: {songFileCurrent}");
                var audioClip = LSAudio.CreateAudioClipUsingMP3File(songFileCurrent);
                CurrentMenu.music = audioClip;
                CurrentMenu.music.name = Path.GetFileName(songFileCurrent);
                PlayMusic(audioClip);
                return;
            }

            CoreHelper.StartCoroutine(AlephNetworkManager.DownloadAudioClip($"file://{songFileCurrent}", audioType, audioClip =>
            {
                CoreHelper.Log($"Attempting to play music: {songFileCurrent}");
                CurrentMenu.music = audioClip;
                CurrentMenu.music.name = Path.GetFileName(songFileCurrent);
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

            if (CurrentGenerateUICoroutine != null)
            {
                StopCoroutine(CurrentGenerateUICoroutine);
                CurrentGenerateUICoroutine = null;
            }
        }

        public void Clear()
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
            themes.Clear();

            if (CurrentGenerateUICoroutine != null)
            {
                StopCoroutine(CurrentGenerateUICoroutine);
                CurrentGenerateUICoroutine = null;
            }
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

        public void StartupInterface()
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

            var path = $"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}Interfaces/main_menu.lsi";
            var jn = JSON.Parse(RTFile.ReadFromFile(path));

            var menu = CustomMenu.Parse(jn);
            menu.filePath = path;
            interfaces.Add(menu);

            path = $"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}Interfaces/story_mode.lsi";
            jn = JSON.Parse(RTFile.ReadFromFile(path));

            menu = CustomMenu.Parse(jn);
            menu.filePath = path;
            interfaces.Add(menu);

            if (!MenuConfig.Instance.ShowChangelog.Value || ChangeLogMenu.Seen)
            {
                SetCurrentInterface("0");
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
                    //rect = new RectValues(new Vector2(0f, -32f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-64f, -256f)),
                    rect = RectValues.FullAnchored.AnchoredPosition(0f, -32f).SizeDelta(-64f, -256f),
                });

                changeLogMenu.elements.Add(new MenuText
                {
                    id = "1",
                    name = "Title",
                    text = "<size=60><b>BetterLegacy Changelog",
                    //rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(-620f, 440f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400f, 64f)),
                    rect = RectValues.Default.AnchoredPosition(-640f, 440f).SizeDelta(400f, 64f),
                    icon = LegacyPlugin.PALogoSprite,
                    //iconRectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(-256f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(64f, 64f)),
                    iconRect = RectValues.Default.AnchoredPosition(-256f, 0f).SizeDelta(64f, 64f),
                    hideBG = true,
                    textColor = 6
                });

                var lines = CoreHelper.GetLines(x);

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];

                    var regex = new Regex(@"([0-9]+).([0-9]+).([0-9]+) > \[(.*?) ([0-9]+), ([0-9]+)]");
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
                    //rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(0f, -400f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 64f)),
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
                SetCurrentInterface("0");
                PlayMusic();
            }));
            yield break;
        }
    }

    public class ChangeLogMenu : MenuBase
    {
        public ChangeLogMenu() : base(false)
        {
            exitFunc = () => { InterfaceManager.inst.SetCurrentInterface("0"); };
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
                //rectJSON = MenuImage.GenerateRectTransformJSON(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 36f)),
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

    //public class ArcadeMenuTest : MenuBase
    //{
    //    public ArcadeMenuTest() : base() { }

    //    public override IEnumerator GenerateUI()
    //    {
    //        var canvas = UIManager.GenerateUICanvas("Arcade Menu", null);
    //        this.canvas = canvas.Canvas.gameObject;

    //        var gameObject = Creator.NewUIObject("Layout", canvas.Canvas.transform);
    //        gameObject.transform.AsRT().anchoredPosition = Vector2.zero;

    //        var text = CreateText("Test Text", gameObject.transform, "This is a test!", new Vector2(0f, 200f), new Vector2(200f, 64f));
    //        elements.Add(text);

    //        while (text.isSpawning)
    //            yield return null;

    //        var button1 = CreateButton("Test 1", gameObject.transform, new Vector2Int(0, 0), "Test 1", new Vector2(-100f, 0f), new Vector2(100f, 64f));
    //        button1.clickable.onClick = pointerEventData =>
    //        {
    //            if (!isOpen)
    //                return;

    //            AudioManager.inst.PlaySound("blip");
    //        };
    //        elements.Add(button1);

    //        while (button1.isSpawning)
    //            yield return null;

    //        var button2 = CreateButton("Test 2", gameObject.transform, new Vector2Int(1, 0), "Test 2", new Vector2(100f, 0f), new Vector2(100f, 64f));
    //        button2.clickable.onClick = pointerEventData =>
    //        {
    //            if (!isOpen)
    //                return;

    //            AudioManager.inst.PlaySound("blip");
    //        };
    //        elements.Add(button2);

    //        while (button2.isSpawning)
    //            yield return null;

    //        var button3 = CreateButton("Test 3", gameObject.transform, new Vector2Int(0, 1), "Test 3", new Vector2(0f, -100f), new Vector2(100f, 64f));
    //        button3.clickable.onClick = pointerEventData =>
    //        {
    //            if (!isOpen)
    //                return;

    //            AudioManager.inst.PlaySound("blip");
    //        };
    //        elements.Add(button3);

    //        while (button3.isSpawning)
    //            yield return null;

    //        isOpen = true;
    //        yield break;
    //    }

    //    public override BeatmapTheme Theme { get; set; } = new BeatmapTheme()
    //    {
    //        name = "Default Theme",
    //        backgroundColor = new Color(0.9f, 0.9f, 0.9f),
    //        guiColor = new Color(0.5f, 0.5f, 0.5f),
    //        guiAccentColor = new Color(1f, 0.6f, 0f),
    //        playerColors = new List<Color>
    //        {
    //            new Color(0.5f, 0.5f, 0.5f), // 0
    //            new Color(0.5f, 0.5f, 0.5f), // 1
    //            new Color(0.5f, 0.5f, 0.5f), // 2
    //            new Color(0.5f, 0.5f, 0.5f), // 3
    //        },
    //        objectColors = new List<Color>
    //        {
    //            new Color(0.5f, 0.5f, 0.5f), // 0
    //            new Color(0.5f, 0.5f, 0.5f), // 1
    //            new Color(0.5f, 0.5f, 0.5f), // 2
    //            new Color(0.5f, 0.5f, 0.5f), // 3
    //            new Color(0.5f, 0.5f, 0.5f), // 4
    //            new Color(0.5f, 0.5f, 0.5f), // 5
    //            new Color(0.5f, 0.5f, 0.5f), // 6
    //            new Color(0.5f, 0.5f, 0.5f), // 7
    //            new Color(0.5f, 0.5f, 0.5f), // 8
    //            new Color(0.5f, 0.5f, 0.5f), // 9
    //            new Color(0.5f, 0.5f, 0.5f), // 10
    //            new Color(0.5f, 0.5f, 0.5f), // 11
    //            new Color(0.5f, 0.5f, 0.5f), // 12
    //            new Color(0.5f, 0.5f, 0.5f), // 13
    //            new Color(0.5f, 0.5f, 0.5f), // 14
    //            new Color(0.5f, 0.5f, 0.5f), // 15
    //            new Color(0.5f, 0.5f, 0.5f), // 16
    //            new Color(0.5f, 0.5f, 0.5f), // 17
    //        },
    //        effectColors = new List<Color>
    //        {
    //            new Color(0.5f, 0.5f, 0.5f), // 0
    //            new Color(0.5f, 0.5f, 0.5f), // 1
    //            new Color(0.5f, 0.5f, 0.5f), // 2
    //            new Color(0.5f, 0.5f, 0.5f), // 3
    //            new Color(0.5f, 0.5f, 0.5f), // 4
    //            new Color(0.5f, 0.5f, 0.5f), // 5
    //            new Color(0.5f, 0.5f, 0.5f), // 6
    //            new Color(0.5f, 0.5f, 0.5f), // 7
    //            new Color(0.5f, 0.5f, 0.5f), // 8
    //            new Color(0.5f, 0.5f, 0.5f), // 9
    //            new Color(0.5f, 0.5f, 0.5f), // 10
    //            new Color(0.5f, 0.5f, 0.5f), // 11
    //            new Color(0.5f, 0.5f, 0.5f), // 12
    //            new Color(0.5f, 0.5f, 0.5f), // 13
    //            new Color(0.5f, 0.5f, 0.5f), // 14
    //            new Color(0.5f, 0.5f, 0.5f), // 15
    //            new Color(0.5f, 0.5f, 0.5f), // 16
    //            new Color(0.5f, 0.5f, 0.5f), // 17
    //        },
    //        backgroundColors = new List<Color>
    //        {
    //            new Color(0.5f, 0.5f, 0.5f), // 0
    //            new Color(0.5f, 0.5f, 0.5f), // 1
    //            new Color(0.5f, 0.5f, 0.5f), // 2
    //            new Color(0.5f, 0.5f, 0.5f), // 3
    //            new Color(0.5f, 0.5f, 0.5f), // 4
    //            new Color(0.5f, 0.5f, 0.5f), // 5
    //            new Color(0.5f, 0.5f, 0.5f), // 6
    //            new Color(0.5f, 0.5f, 0.5f), // 7
    //            new Color(0.5f, 0.5f, 0.5f), // 8
    //        },
    //    };
    //}
}
