using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Patchers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BetterLegacy.Menus
{
    public class MenuManager : MonoBehaviour
    {
        public static MenuManager inst;

        public static void Init()
        {
            var gameObject = new GameObject("MenuManager");
            gameObject.transform.SetParent(SystemManager.inst.transform);
            gameObject.AddComponent<MenuManager>();
        }

        void Awake()
        {
            inst = this;
        }

        void Update()
        {
            if (ArcadeManager.inst.ic && ArcadeManager.inst.ic.gameObject.scene.name == "Main Menu" && Input.GetKeyDown(MenuConfig.Instance.ReloadMainMenu.Value))
            {
                SceneManager.inst.LoadScene("Main Menu");
            }

            // For resetting menu selection, due to UnityExplorer.
            if (Input.GetKeyDown(KeyCode.G) && ArcadeManager.inst.ic && ArcadeManager.inst.ic.buttons != null && ArcadeManager.inst.ic.buttons.Count > 0)
            {
                ArcadeManager.inst.ic.currHoveredButton = ArcadeManager.inst.ic.buttons[0];
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(ArcadeManager.inst.ic.buttons[0]);
            }
        }

        public static void SetupPageEditor()
        {
            PageEditor.Init();
        }

        public static string prevScene = "Main Menu";
        public static string prevBranch;
        public static string prevInterface = "beatmaps/menus/menu.lsm";
        public static bool fromPageLevel = false;

        public static int randomIndex = -1;

        public void PlayMusic(InterfaceController __instance)
        {
            var directory = RTFile.ApplicationDirectory + "settings/menus/";

            if (!MenuConfig.Instance.PlayCustomMusic.Value)
            {
                PlayDefaultMusic(__instance);
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
                PlayDefaultMusic(__instance);
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
                PlayDefaultMusic(__instance);
                return;
            }

            songs = songFiles;

            if (MenuConfig.Instance.MusicIndex.Value >= 0 && MenuConfig.Instance.MusicIndex.Value < songFiles.Length)
                randomIndex = MenuConfig.Instance.MusicIndex.Value;

            if (randomIndex < 0 || randomIndex >= songFiles.Length)
            {
                randomIndex = UnityEngine.Random.Range(0, songFiles.Length);
            }

            var songFileCurrent = songFiles[Mathf.Clamp(randomIndex, 0, songFiles.Length - 1)];

            if (string.IsNullOrEmpty(songFileCurrent))
            {
                PlayDefaultMusic(__instance);
                return;
            }

            __instance.StartCoroutine(FileManager.inst.LoadMusicFileRaw(songFileCurrent, false, delegate (AudioClip clip)
            {
                currentMenuMusic = clip;
                currentMenuMusicName = Path.GetFileName(songFileCurrent);

                AudioManager.inst.PlayMusic(Path.GetFileName(songFileCurrent), clip);
            }));
        }

        public static string[] songs;

        public void PlayDefaultMusic(InterfaceController __instance)
        {
            if (__instance.interfaceSettings.music == "menu")
            {
                string musicName = DataManager.inst.GetSettingEnumValues("MenuMusic", 0);
                currentMenuMusic = AudioManager.inst.library.GetMusicFromName(musicName);
                currentMenuMusicName = musicName;

                AudioManager.inst.PlayMusic(musicName, 0f);
                return;
            }
            if (__instance.interfaceSettings.music != null && __instance.interfaceSettings.music != "")
            {
                AudioManager.inst.PlayMusic(__instance.interfaceSettings.music, 0f);
            }
        }

        public string currentMenuMusicName;
        public AudioClip currentMenuMusic;


        public static IEnumerator ReturnToMenu()
        {
            SceneManager.inst.LoadScene(prevScene);

            while (!ArcadeManager.inst.ic)
                yield return null;

            if (!string.IsNullOrEmpty(prevBranch))
            {
                InterfaceControllerPatch.LoadInterface(ArcadeManager.inst.ic, prevInterface, false);
                ArcadeManager.inst.ic.SwitchBranch(prevBranch);
            }
        }

    }
}
