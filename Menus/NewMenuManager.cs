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

namespace BetterLegacy.Menus
{
    public class NewMenuManager : MonoBehaviour
    {
        public static NewMenuManager inst;
        public static void Init() => Creator.NewGameObject(nameof(NewMenuManager), SystemManager.inst.transform).AddComponent<NewMenuManager>();

        public float[] samples = new float[256];

        public MenuBase CurrentMenu { get; set; }

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
            if (!CurrentAudioSource.clip || allowSame || music.name != CurrentAudioSource.clip.name)
            {
                CoreHelper.Log("Playing music");
                CurrentAudioSource.clip = music;
                CurrentAudioSource.UnPause();
                CurrentAudioSource.time = 0f;
                CurrentAudioSource.Play();
            }
        }

        public void StopMusic()
        {
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
            if (MenuConfig.Instance.MusicLoadMode.Value == MenuMusicLoadMode.StoryFolder || MenuConfig.Instance.MusicLoadMode.Value == MenuMusicLoadMode.EditorFolder)
            {
                oggSearchPattern = "level.ogg";
                wavSearchPattern = "level.wav";
            }

            var oggFiles = Directory.GetFiles(directory, oggSearchPattern, SearchOption.AllDirectories);
            var wavFiles = Directory.GetFiles(directory, wavSearchPattern, SearchOption.AllDirectories);

            var songFiles = new string[oggFiles.Length + wavFiles.Length];

            for (int i = 0; i < oggFiles.Length; i++)
            {
                songFiles[i] = oggFiles[i];
            }
            for (int i = oggFiles.Length; i < songFiles.Length; i++)
            {
                songFiles[i] = wavFiles[i - oggFiles.Length];
            }

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

            if (CurrentAudioSource.clip && CurrentAudioSource.clip.name == Path.GetFileName(songFileCurrent))
                return;

            CoreHelper.StartCoroutine(AlephNetworkManager.DownloadAudioClip($"file://{songFileCurrent}", RTFile.GetAudioType(songFileCurrent), audioClip =>
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
                if (CurrentMenu != null)
                {
                    CurrentMenu.Clear();
                    CurrentMenu = null;
                }

                CurrentMenu = menu;
                StartCoroutine(menu.GenerateUI());
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
        }

        public void Test()
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

            AudioManager.inst.StopMusic();

            var path = $"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}Interfaces/test_menu.lsi";
            var jn = JSON.Parse(RTFile.ReadFromFile(path));

            var menu = CustomMenu.Parse(jn);
            menu.filePath = path;
            CurrentMenu = menu;
            StartCoroutine(menu.GenerateUI());
            interfaces.Add(menu);

            path = $"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}Interfaces/story_mode.lsi";
            jn = JSON.Parse(RTFile.ReadFromFile(path));

            menu = CustomMenu.Parse(jn);
            menu.filePath = path;
            interfaces.Add(menu);
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
