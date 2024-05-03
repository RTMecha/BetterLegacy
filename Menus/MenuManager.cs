using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Managers;
using BetterLegacy.Patchers;
using LSFunctions;
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
            if (LSHelpers.IsUsingInputField())
                return;

            // For loading the Main Menu scene when you make a change to the menus.
            if (Input.GetKeyDown(MenuConfig.Instance.ReloadMainMenu.Value) && ArcadeManager.inst.ic && ArcadeManager.inst.ic.gameObject.scene.name == "Main Menu")
                SceneManager.inst.LoadScene("Main Menu");

            // For loading the Interface scene when you make a change to the menus.
            if (Input.GetKeyDown(MenuConfig.Instance.ReloadMainMenu.Value) && ArcadeManager.inst.ic && ArcadeManager.inst.ic.gameObject.scene.name == "Interface")
                SceneManager.inst.LoadScene("Interface");

            // For resetting menu selection, due to UnityExplorer removing the menu selection.
            if (Input.GetKeyDown(MenuConfig.Instance.SelectFirstButton.Value) && ArcadeManager.inst.ic && ArcadeManager.inst.ic.buttons != null && ArcadeManager.inst.ic.buttons.Count > 0)
            {
                ArcadeManager.inst.ic.currHoveredButton = ArcadeManager.inst.ic.buttons[0];
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(ArcadeManager.inst.ic.buttons[0]);
            }

            if (!Input.GetKeyDown(MenuConfig.Instance.LoadPageEditor.Value))
                return;

            if (GameManager.inst && !EditorManager.inst)
            {
                CoreHelper.LogWarning("Cannot enter Page Editor while in-game.");
                return;
            }

            if (!EditorManager.inst)
            {
                PageEditor.Init();
                return;
            }

            RTEditor.inst.ShowWarningPopup("Are you sure you want to load the Page Editor? Any unsaved changes will be lost!", delegate ()
            {
                if (EditorManager.inst.savingBeatmap)
                {
                    EditorManager.inst.DisplayNotification("Please wait until the beatmap finishes saving!", 2f, EditorManager.NotificationType.Error);
                    return;
                }

                DG.Tweening.DOTween.KillAll();
                DG.Tweening.DOTween.Clear(true);
                EditorManager.inst.loadedLevels.Clear();
                DataManager.inst.gameData = null;
                DataManager.inst.gameData = new GameData();
                DiscordController.inst.OnIconChange("");
                DiscordController.inst.OnStateChange("");
                CoreHelper.Log($"Quit to Main Menu");
                InputDataManager.inst.players.Clear();
                SceneManager.inst.LoadScene("Main Menu");
            }, delegate ()
            {
                EditorManager.inst.HideDialog("Warning Popup");
            });
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

            if (!string.IsNullOrEmpty(__instance.interfaceSettings.music))
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
