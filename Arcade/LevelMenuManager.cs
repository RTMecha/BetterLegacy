using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using DG.Tweening;
using InControl;
using LSFunctions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

namespace BetterLegacy.Arcade
{
    public class LevelMenuManager : MonoBehaviour
    {
        public static LevelMenuManager inst;

        public static GameObject settingsWindow;
        public static GameObject levelFolder;
        public static GameObject levelList;
        public static GameObject levelWindow;
        public static GameObject menuUI;
        public static InputField searchField;
        public static string searchTerm;
        public static int levelFilter = 0;
        public static bool levelAscend = true;
        public static Font inconsolataFont;
        public static float screenScale;
        public static float screenScaleInverse;
        public static Material fontMaterial;
        public static GameObject textMeshPro;
        public static bool onlyShowQueue = false;

        public static Scrollbar dragger;

        void Awake()
        {
            inst = this;
            inst.StartCoroutine(ClearScene());
        }

        void Update()
        {
            screenScale = CoreHelper.ScreenScale;
            screenScaleInverse = CoreHelper.ScreenScaleInverse;

            Camera.main.backgroundColor = LSColors.HexToColor(DataManager.inst.interfaceSettings["UITheme"][SaveManager.inst.settings.Video.UITheme]["values"]["bg"]);

            if (InputDataManager.inst.menuActions.Cancel.WasPressed && !CoreHelper.IsUsingInputField)
            {
                SceneManager.inst.LoadScene("Input Select");
            }
            LoopSongPreview();

            if (dragger != null)
            {
                if (InputDataManager.inst.menuActions.Up.IsPressed)
                    dragger.value += 0.001f * InputDataManager.inst.menuActions.Up.Value;
                if (InputDataManager.inst.menuActions.Down.IsPressed)
                    dragger.value -= 0.001f * InputDataManager.inst.menuActions.Down.Value;
            }
        }

        public static IEnumerator DeleteComponents()
        {
            Destroy(GameObject.Find("Interface"));
            Destroy(GameObject.Find("EventSystem").GetComponent<InControlInputModule>());
            Destroy(GameObject.Find("EventSystem").GetComponent<BaseInput>());
            GameObject.Find("EventSystem").AddComponent<StandaloneInputModule>();
            Destroy(GameObject.Find("Main Camera").GetComponent<InterfaceLoader>());
            Destroy(GameObject.Find("Main Camera").GetComponent<ArcadeController>());
            Destroy(GameObject.Find("Main Camera").GetComponent<FlareLayer>());
            Destroy(GameObject.Find("Main Camera").GetComponent<GUILayer>());
            yield break;
        }

        public static IEnumerator ClearScene()
        {
            LSHelpers.ShowCursor();
            yield return inst.StartCoroutine(DeleteComponents());

            var findButton = (from x in Resources.FindObjectsOfTypeAll<GameObject>()
                              where x.name == "Text Element"
                              select x).ToList();

            textMeshPro = findButton[0].transform.GetChild(1).gameObject;
            fontMaterial = findButton[0].transform.GetChild(1).GetComponent<TextMeshProUGUI>().fontMaterial;

            inconsolataFont = Font.GetDefault();

            levelFolder = FolderButton();

            LevelManager.currentQueueIndex = 0;
            LevelManager.ArcadeQueue.Clear();

            yield return inst.StartCoroutine(GenerateOpenFilePopup());

            menuUI.transform.SetParent(null);

            levelList.transform.SetParent(null);
            levelList.SetActive(true);

            //GameObject cameraObject = new GameObject("Main Camera");
            //var cam = cameraObject.AddComponent<Camera>();
            //cam.orthographic = true;

            levelList.transform.SetParent(menuUI.transform);
            levelList.transform.localPosition = Vector3.zero;
            levelList.GetComponent<RectTransform>().sizeDelta = new Vector2(1000f, 800f);

            levelWindow = Instantiate(levelFolder);
            levelWindow.transform.SetParent(menuUI.transform);
            levelWindow.transform.localScale = Vector3.one;
            var levelWindowRT = levelWindow.GetComponent<RectTransform>();
            levelWindowRT.anchoredPosition = new Vector2(2360f, 540f);
            levelWindowRT.sizeDelta = new Vector2(696f, 840f);
            levelWindowRT.pivot = new Vector2(0.5f, 0.5f);
            levelWindow.name = "folderLooker";
            Destroy(levelWindow.GetComponent<Button>());
            levelWindow.GetComponent<Image>().color = new Color(0.3106f, 0.2906f, 0.3506f, 1f);
            levelWindow.transform.localScale = Vector3.one * screenScale;

            //Artist
            {
                var folder = levelWindow.transform.Find("folder-name").gameObject;
                folder.name = "artist";
                var folderName = folder.GetComponent<TextMeshProUGUI>();
                folderName.text = "Something";
                folderName.alignment = TextAlignmentOptions.BottomLeft;
                folderName.fontSize = 22;
                folderName.color = offWhite;
                folder.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 260f);
                folder.GetComponent<RectTransform>().sizeDelta = new Vector2(-12f, -8f);
            }

            //Song
            {
                var folder = Instantiate(levelWindow.transform.Find("artist").gameObject);
                folder.transform.SetParent(levelWindowRT.transform);
                folder.transform.localScale = Vector3.one;
                folder.name = "song";
                var folderName = folder.GetComponent<TextMeshProUGUI>();
                folderName.text = "Something";
                folderName.alignment = TextAlignmentOptions.BottomLeft;
                folderName.fontSize = 22;
                folderName.color = offWhite;
                folder.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 240f);
                folder.GetComponent<RectTransform>().sizeDelta = new Vector2(-12f, -8f);
            }

            //Creator
            {
                var folder = Instantiate(levelWindow.transform.Find("artist").gameObject);
                folder.transform.SetParent(levelWindowRT.transform);
                folder.transform.localScale = Vector3.one;
                folder.name = "creator";
                var folderName = folder.GetComponent<TextMeshProUGUI>();
                folderName.text = "Something";
                folderName.alignment = TextAlignmentOptions.BottomLeft;
                folderName.fontSize = 22;
                folderName.color = offWhite;
                folder.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 220f);
                folder.GetComponent<RectTransform>().sizeDelta = new Vector2(-12f, -8f);
            }

            //Difficulty
            {
                var folder = Instantiate(levelWindow.transform.Find("artist").gameObject);
                folder.transform.SetParent(levelWindowRT.transform);
                folder.transform.localScale = Vector3.one;
                folder.name = "difficulty";
                var folderName = folder.GetComponent<TextMeshProUGUI>();
                folderName.text = "Something";
                folderName.alignment = TextAlignmentOptions.BottomLeft;
                folderName.fontSize = 22;
                folderName.color = offWhite;
                folder.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 200f);
                folder.GetComponent<RectTransform>().sizeDelta = new Vector2(-12f, -8f);
            }

            //Description
            {
                var folder = Instantiate(levelWindow.transform.Find("artist").gameObject);
                folder.transform.SetParent(levelWindowRT.transform);
                folder.transform.localScale = Vector3.one;
                folder.name = "description";
                var folderName = folder.GetComponent<TextMeshProUGUI>();
                folderName.text = "Something";
                folderName.alignment = TextAlignmentOptions.TopLeft;
                folderName.fontSize = 22;
                folderName.color = offWhite;
                folder.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -625f);
                folder.GetComponent<RectTransform>().sizeDelta = new Vector2(-12f, -8f);
            }

            var buttons = new GameObject("buttons");
            buttons.transform.SetParent(levelWindow.transform);
            buttons.transform.localScale = Vector3.one;
            var buttonsRT = buttons.AddComponent<RectTransform>();
            var gridLayout = buttons.AddComponent<GridLayoutGroup>();

            buttonsRT.anchoredPosition = new Vector2(-248f, -365f);

            gridLayout.cellSize = new Vector2(145f, 70f);
            gridLayout.spacing = new Vector2(22f, 22f);
            gridLayout.startAxis = GridLayoutGroup.Axis.Vertical;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;

            //Play
            {
                var playButton = Instantiate(levelFolder);
                playButton.transform.SetParent(buttons.transform);
                playButton.name = "play";
                playButton.transform.localScale = Vector3.one;
                var play = playButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                play.text = "[PLAY]";
                play.fontSize = 20;
                play.alignment = TextAlignmentOptions.Center;
                play.color = offWhite;

                var playRT = playButton.GetComponent<RectTransform>();
                playRT.pivot = new Vector2(0.5f, 0.5f);

                var playButtButt = playButton.GetComponent<Button>();
                playButtButt.onClick.RemoveAllListeners();
                playButtButt.onClick.AddListener(delegate ()
                {
                    LevelManager.currentQueueIndex = 0;
                    if (LevelManager.ArcadeQueue.Count > 1)
                    {
                        LevelManager.CurrentLevel = LevelManager.ArcadeQueue[0];
                    }

                    menuUI.SetActive(false);
                    LevelManager.OnLevelEnd = ArcadeHelper.EndOfLevel;
                    CoreHelper.StartCoroutine(LevelManager.Play(LevelManager.CurrentLevel));
                });
            }

            //Add to queue
            {
                var playButton = Instantiate(levelFolder);
                playButton.transform.SetParent(buttons.transform);
                playButton.name = "add";
                playButton.transform.localScale = Vector3.one;
                var play = playButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                play.text = "[ADD TO QUEUE]";
                play.fontSize = 20;
                play.alignment = TextAlignmentOptions.Center;
                play.color = offWhite;

                var playRT = playButton.GetComponent<RectTransform>();
                playRT.pivot = new Vector2(0.5f, 0.5f);

                var playButtButt = playButton.GetComponent<Button>();
                playButtButt.onClick.RemoveAllListeners();
            }

            //Get song
            {
                var playButton = Instantiate(levelFolder);
                playButton.transform.SetParent(buttons.transform);
                playButton.name = "get song";
                playButton.transform.localScale = Vector3.one;
                var play = playButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                play.text = "[GET SONG]";
                play.fontSize = 20;
                play.alignment = TextAlignmentOptions.Center;
                play.color = offWhite;

                var playRT = playButton.GetComponent<RectTransform>();
                playRT.pivot = new Vector2(0.5f, 0.5f);

                var playButtButt = playButton.GetComponent<Button>();
                playButtButt.onClick.RemoveAllListeners();
                playButtButt.onClick.AddListener(delegate ()
                {
                    Application.OpenURL(LevelManager.CurrentLevel.metadata.artist.getUrl());
                });
            }

            //Settings
            {
                var playButton = Instantiate(levelFolder);
                playButton.transform.SetParent(buttons.transform);
                playButton.name = "settings";
                playButton.transform.localScale = Vector3.one;
                var play = playButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                play.text = "[SETTINGS]";
                play.fontSize = 20;
                play.alignment = TextAlignmentOptions.Center;
                play.color = offWhite;

                var playRT = playButton.GetComponent<RectTransform>();
                playRT.pivot = new Vector2(0.5f, 0.5f);

                var playButtButt = playButton.GetComponent<Button>();
                playButtButt.onClick.RemoveAllListeners();
                playButtButt.onClick.AddListener(delegate ()
                {
                    CoreHelper.Log("Open Settings");
                    RevealSettings();
                });
            }

            GameObject iconBase = new GameObject("icon");
            iconBase.transform.SetParent(levelWindow.transform);
            iconBase.transform.SetAsFirstSibling();
            iconBase.transform.localScale = Vector3.one;
            iconBase.layer = 5;
            RectTransform iconBaseRT = iconBase.AddComponent<RectTransform>();
            iconBase.AddComponent<CanvasRenderer>();
            Image iconBaseImage = iconBase.AddComponent<Image>();

            iconBaseRT.anchoredPosition = new Vector2(0f, 130f);
            iconBaseRT.sizeDelta = new Vector2(512f, 512f);

            inst.StartCoroutine(FixPopup());

            iconBaseImage.sprite = SteamWorkshop.inst.defaultSteamImageSprite;

            var tex = Instantiate(textMeshPro);
            tex.transform.SetParent(iconBase.transform);
            tex.transform.localScale = new Vector3(6f, 6f, 1f);
            tex.transform.localPosition = new Vector3(100f, -220f, 0f);
            tex.transform.rotation = Quaternion.Euler(0f, 0f, 355f);
            tex.name = "LevelRank";

            LSHelpers.DeleteChildren(levelList.transform.Find("mask/content"));

            levelList.transform.Find("mask/content").GetComponent<GridLayoutGroup>().cellSize = new Vector2(984f, 32f);

            var refresh = levelList.transform.Find("reload").GetComponent<Button>();
            refresh.onClick.RemoveAllListeners();
            refresh.onClick.AddListener(delegate ()
            {
                inst.StartCoroutine(GenerateUIList());
                var menu = new GameObject("Load Level System");
                menu.AddComponent<LoadLevelsManager>();
            });
            //refresh.GetComponent<RectTransform>().anchoredPosition = new Vector2(560f, 832f);

            searchField = levelList.transform.Find("search-box/search").GetComponent<InputField>();
            searchField.onValueChanged.ClearAll();
            searchField.onValueChanged.AddListener(delegate (string _val)
            {
                searchTerm = _val;
                inst.StartCoroutine(GenerateUIList());
            });
            searchTerm = searchField.text;
            inst.StartCoroutine(GenerateUIList());

            var levelPath = levelList.transform.Find("story path").GetComponent<InputField>();
            levelPath.onValueChanged.RemoveAllListeners();
            levelPath.text = LevelManager.Path;
            levelPath.onValueChanged.AddListener(delegate (string _val)
            {
                LevelManager.Path = _val;
            });

            levelAscend = ArcadeConfig.Instance.LocalLevelAscend.Value;

            var toggleClone = levelList.transform.Find("toggle/toggle").GetComponent<Toggle>();
            //levelList.transform.Find("toggle").GetComponent<RectTransform>().anchoredPosition = new Vector2(1000f, 16f);
            toggleClone.onValueChanged.RemoveAllListeners();
            toggleClone.isOn = levelAscend;
            toggleClone.onValueChanged.AddListener(delegate (bool _val)
            {
                levelAscend = _val;
                Sort();
                inst.StartCoroutine(GenerateUIList());
            });

            var dropdownClone = levelList.transform.Find("orderby dropdown").GetComponent<Dropdown>();
            dropdownClone.onValueChanged.RemoveAllListeners();
            //dropdownClone.GetComponent<RectTransform>().anchoredPosition = new Vector2(901f, 816f);

            levelFilter = (int)ArcadeConfig.Instance.LocalLevelOrderby.Value;

            dropdownClone.value = levelFilter;
            dropdownClone.onValueChanged.AddListener(delegate (int _val)
            {
                levelFilter = _val;
                Sort();
                inst.StartCoroutine(GenerateUIList());
            });

            GameObject videoObject = new GameObject("VideoPlayer");
            videoPlayer = videoObject.AddComponent<VideoPlayer>();
            videoPlayer.targetCamera = Camera.main;
            videoPlayer.renderMode = VideoRenderMode.CameraFarPlane;
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.targetCameraAlpha = 0.5f;
            videoPlayer.timeSource = VideoTimeSource.GameTimeSource;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.isLooping = true;

            settingsWindow = Instantiate(levelFolder);
            settingsWindow.transform.SetParent(menuUI.transform);
            var settingsWindowRT = settingsWindow.GetComponent<RectTransform>();
            settingsWindowRT.anchoredPosition = new Vector2(1460f, -60f);
            settingsWindowRT.sizeDelta = new Vector2(696f, 110f);
            settingsWindowRT.pivot = new Vector2(0.5f, 0.5f);
            settingsWindow.name = "settings";
            Destroy(settingsWindow.GetComponent<Button>());
            settingsWindow.GetComponent<Image>().color = new Color(0.3106f, 0.2906f, 0.3506f, 1f);
            settingsWindow.transform.localScale = Vector3.one;

            var settingsName = settingsWindow.transform.Find("folder-name").GetComponent<TextMeshProUGUI>();
            settingsName.text = "Settings";
            settingsName.alignment = TextAlignmentOptions.Center;
            settingsName.fontSize = 26;
            settingsWindow.transform.Find("folder-name").GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 45f);
            settingsName.transform.localScale = Vector3.one;

            var difficultyName = Instantiate(settingsName);
            difficultyName.transform.SetParent(settingsWindow.transform);
            difficultyName.transform.localScale = Vector3.one;
            difficultyName.GetComponent<TextMeshProUGUI>().text = "[DIFFICULTY]";
            difficultyName.name = "difficulty-name";
            difficultyName.GetComponent<RectTransform>().anchoredPosition = new Vector2(-247f, 15f);

            var speedName = Instantiate(settingsName);
            speedName.transform.SetParent(settingsWindow.transform);
            speedName.transform.localScale = Vector3.one;
            speedName.GetComponent<TextMeshProUGUI>().text = "[SPEED MULT]";
            speedName.name = "speed-name";
            speedName.GetComponent<RectTransform>().anchoredPosition = new Vector2(-80f, 15f);

            var sooon1Name = Instantiate(settingsName);
            sooon1Name.transform.SetParent(settingsWindow.transform);
            sooon1Name.transform.localScale = Vector3.one;
            sooon1Name.GetComponent<TextMeshProUGUI>().text = "[COMING SOON]";
            sooon1Name.name = "soon1-name";
            sooon1Name.GetComponent<RectTransform>().anchoredPosition = new Vector2(87f, 15f);

            var sooon2Name = Instantiate(settingsName);
            sooon2Name.transform.SetParent(settingsWindow.transform);
            sooon2Name.transform.localScale = Vector3.one;
            sooon2Name.GetComponent<TextMeshProUGUI>().text = "[COMING SOON]";
            sooon2Name.name = "soon2-name";
            sooon2Name.GetComponent<RectTransform>().anchoredPosition = new Vector2(254f, 15f);

            var settingsbuttons = new GameObject("buttons");
            settingsbuttons.transform.SetParent(settingsWindow.transform);
            settingsbuttons.transform.localScale = Vector3.one;
            var settingsbuttonsRT = settingsbuttons.AddComponent<RectTransform>();
            var settingsgridLayout = settingsbuttons.AddComponent<GridLayoutGroup>();

            settingsbuttonsRT.anchoredPosition = new Vector2(-248f, -25f);

            settingsgridLayout.cellSize = new Vector2(145f, 40f);
            settingsgridLayout.spacing = new Vector2(22f, 22f);
            settingsgridLayout.startAxis = GridLayoutGroup.Axis.Vertical;
            settingsgridLayout.childAlignment = TextAnchor.MiddleCenter;

            //Difficulty
            {
                var difficulty = GenerateUIDropdown("difficulty", settingsbuttons.transform);
                var difficultyDD = (Dropdown)difficulty["Dropdown"];
                ((GameObject)difficulty["GameObject"]).transform.localScale = Vector3.one;
                difficultyDD.options = new List<Dropdown.OptionData>
                {
                    new Dropdown.OptionData("Zen"),
                    new Dropdown.OptionData("Normal"),
                    new Dropdown.OptionData("1 Life"),
                    new Dropdown.OptionData("1 Hit"),
                    new Dropdown.OptionData("Practice"),
                };

                difficultyDD.onValueChanged.RemoveAllListeners();
                difficultyDD.value = DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1);
                difficultyDD.onValueChanged.AddListener(delegate (int _val)
                {
                    DataManager.inst.UpdateSettingEnum("ArcadeDifficulty", _val);
                });
            }

            //Speed Multiplier
            {
                var difficulty = GenerateUIDropdown("speed", settingsbuttons.transform);
                var difficultyDD = (Dropdown)difficulty["Dropdown"];
                ((GameObject)difficulty["GameObject"]).transform.localScale = Vector3.one;
                difficultyDD.options = new List<Dropdown.OptionData>
                {
                    new Dropdown.OptionData("x0.1"),
                    new Dropdown.OptionData("x0.5"),
                    new Dropdown.OptionData("x0.8"),
                    new Dropdown.OptionData("x1.0"),
                    new Dropdown.OptionData("x1.2"),
                    new Dropdown.OptionData("x1.5"),
                    new Dropdown.OptionData("x2.0"),
                    new Dropdown.OptionData("x3.0"),
                };

                difficultyDD.onValueChanged.RemoveAllListeners();
                difficultyDD.value = DataManager.inst.GetSettingEnum("ArcadeGameSpeed", 3);
                difficultyDD.onValueChanged.AddListener(delegate (int _val)
                {
                    DataManager.inst.UpdateSettingEnum("ArcadeGameSpeed", _val);
                    AudioManager.inst.SetPitch(CoreHelper.Pitch);
                    if (videoPlayer.source != VideoSource.VideoClip)
                    {
                        videoPlayer.playbackSpeed = CoreHelper.Pitch;
                    }
                });
            }

            //Checkpoint SFX
            //Player SFX

            //Back
            {
                var playButton = Instantiate(levelFolder);
                playButton.transform.SetParent(menuUI.transform);
                playButton.transform.localScale = Vector3.one;
                playButton.name = "back";
                var play = playButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                play.text = "[RETURN]";
                play.fontSize = 20;
                play.alignment = TextAlignmentOptions.Center;
                play.color = offWhite;

                var playRT = playButton.GetComponent<RectTransform>();
                playRT.pivot = new Vector2(0.5f, 0.5f);
                playRT.anchoredPosition = new Vector2(140f, 1020f);
                playRT.sizeDelta = new Vector2(196f, 64f);

                var playButtButt = playButton.GetComponent<Button>();
                playButtButt.onClick.RemoveAllListeners();
                playButtButt.onClick.AddListener(delegate ()
                {
                    CoreHelper.Log("Exit");
                    SceneManager.inst.LoadScene("Input Select");
                });
            }

            yield break;
        }

        public static VideoPlayer videoPlayer;

        public static void RevealSettings()
        {
            settingsWindow.transform.DOLocalMove(new Vector3(500f, -480f, 0f), 1f).SetEase(DataManager.inst.AnimationList[3].Animation);
        }

        public static Color offWhite = new Color(0.8679f, 0.86f, 0.9f, 1f);

        public static void Sort()
        {
            LevelManager.Sort((LevelSort)levelFilter, levelAscend);
        }

        public static IEnumerator GenerateUIList()
        {
            LSHelpers.DeleteChildren(levelList.transform.Find("mask/content"));
            var go = new GameObject("spacer");
            go.transform.SetParent(levelList.transform.Find("mask/content"));
            go.AddComponent<RectTransform>();
            foreach (var level in LevelManager.Levels)
            {
                var metadata = level.metadata;

                string difficultyName = "none";
                if (metadata.song.difficulty == 0)
                {
                    difficultyName = "easy";
                }
                if (metadata.song.difficulty == 1)
                {
                    difficultyName = "normal";
                }
                if (metadata.song.difficulty == 2)
                {
                    difficultyName = "hard";
                }
                if (metadata.song.difficulty == 3)
                {
                    difficultyName = "expert";
                }
                if (metadata.song.difficulty == 4)
                {
                    difficultyName = "expert+";
                }
                if (metadata.song.difficulty == 5)
                {
                    difficultyName = "master";
                }
                if (metadata.song.difficulty == 6)
                {
                    difficultyName = "animation";
                }

                if (string.IsNullOrEmpty(searchTerm) || metadata.artist.Name.ToLower().Contains(searchTerm.ToLower()) || metadata.song.title.ToLower().Contains(searchTerm.ToLower()) || difficultyName.Contains(searchTerm.ToLower()))
                {
                    var tmpLevel = level;
                    var gameObject = Instantiate(levelFolder);
                    gameObject.name = metadata.song.title;
                    gameObject.transform.SetParent(levelList.transform.Find("mask/content"));
                    gameObject.transform.localScale = Vector3.one;
                    if (gameObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>())
                    {
                        var text = gameObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                        text.text = string.Format("{0}</color> - {1}</color> By {2}", metadata.artist.Name, metadata.song.title, metadata.creator.steam_name);
                        text.alignment = TextAlignmentOptions.Left;
                        text.color = offWhite;
                        text.fontSize = 26;

                        var textRT = text.GetComponent<RectTransform>();
                        textRT.anchoredPosition = new Vector2(48f, 0f);
                    }
                    else
                    {
                        var text = gameObject.transform.GetChild(0).GetComponent<Text>();
                        text.text = string.Format("{0} - {1} By {2}", metadata.artist.Name, metadata.song.title, metadata.creator.steam_name);
                        text.alignment = TextAnchor.MiddleLeft;

                        var textRT = text.GetComponent<RectTransform>();
                        textRT.anchoredPosition = new Vector2(48f, 0f);
                    }

                    var button = gameObject.GetComponent<Button>();
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(delegate ()
                    {
                        inst.StartCoroutine(SetSelectedSong(tmpLevel));
                    });

                    var icon = new GameObject("icon");
                    icon.transform.SetParent(gameObject.transform);
                    icon.transform.localScale = Vector3.one;
                    icon.layer = 5;
                    RectTransform iconRT = icon.AddComponent<RectTransform>();
                    icon.AddComponent<CanvasRenderer>();
                    Image iconImage = icon.AddComponent<Image>();

                    iconRT.anchoredPosition = new Vector2(-460f, 0f);
                    iconRT.sizeDelta = new Vector2(32f, 32f);

                    iconImage.sprite = level.icon;

                    GameObject difficulty = new GameObject("difficulty");
                    difficulty.transform.SetParent(gameObject.transform);
                    difficulty.transform.localScale = Vector3.one;
                    difficulty.layer = 5;
                    RectTransform difficultyRT = difficulty.AddComponent<RectTransform>();
                    difficulty.AddComponent<CanvasRenderer>();
                    Image difficultyImage = difficulty.AddComponent<Image>();

                    difficultyRT.anchoredPosition = new Vector2(-485f, 0f);
                    difficultyRT.sizeDelta = new Vector2(8f, 32f);

                    difficultyImage.color = metadata.song.getDifficultyColor();
                }
            }
            yield break;
        }

        public static bool started = false;
        public static void Show()
        {
            levelList.transform.DOLocalMove(new Vector3(-400f, 0f, 1f), 1f).SetEase(DataManager.inst.AnimationList[3].Animation);
            levelWindow.transform.DOLocalMove(new Vector3(500f, 0f, 0f), 1f).SetEase(DataManager.inst.AnimationList[3].Animation);
        }

        public static void LoopSongPreview()
        {
            //if (AudioManager.inst.CurrentAudioSource == null || !inst.selected)
            //    return;

            //if (previewStart >= 0f && previewLength > 0f && AudioManager.inst.CurrentAudioSource.time > previewStart + previewLength)
            //    AudioManager.inst.CurrentAudioSource.time = previewStart;
        }

        public bool selected = false;
        static float previewStart;
        static float previewLength;

        public static IEnumerator SetSelectedSong(Level level)
        {
            LevelManager.CurrentLevel = level;

            var metadata = level.metadata;

            if (!level.music)
            {
                inst.selected = false;
                level.LoadAudioClip();
                while (!level.music)
                    yield return null;
            }

            if (!started)
            {
                Show();
            }

            AudioManager.inst.StopMusic();
            CoreHelper.Log($"Trying to play [ {metadata.song.title} | {level.music} ]");
            AudioManager.inst.PlayMusic(metadata.song.title, level.music);

            previewStart = metadata.song.previewStart >= 0f ? metadata.song.previewStart : UnityEngine.Random.Range(0f, AudioManager.inst.CurrentAudioSource.clip.length / 2f);
            previewLength = metadata.song.previewLength > 0f ? metadata.song.previewLength : 30f;

            //AudioManager.inst.SetMusicTime(UnityEngine.Random.Range(0f, AudioManager.inst.CurrentAudioSource.clip.length / 2f));
            AudioManager.inst.SetPitch(CoreHelper.Pitch);

            if (RTFile.FileExists(level.path + "preview.mp4"))
            {
                if (videoPlayer.source == VideoSource.VideoClip)
                {
                    videoPlayer.targetCameraAlpha = 0f;
                    inst.StartCoroutine(SetCameraAlphaFade(20));
                    videoPlayer.playbackSpeed = CoreHelper.Pitch;
                }
                videoPlayer.url = level.path + "preview.mp4";
                videoPlayer.source = VideoSource.Url;
                videoPlayer.time = UnityEngine.Random.Range(0, (float)videoPlayer.length);
            }
            else
            {
                videoPlayer.url = "";
                videoPlayer.source = VideoSource.VideoClip;
            }

            var levelRank = LevelManager.GetLevelRank(level);

            levelWindow.transform.Find("icon/LevelRank").GetComponent<TextMeshProUGUI>().text = $"<#{LSColors.ColorToHex(levelRank.color)}>{levelRank.name}</color>";

            levelWindow.transform.Find("icon").GetComponent<Image>().sprite = level.icon;
            levelWindow.transform.Find("artist").GetComponent<TextMeshProUGUI>().text = string.Format("<b>Artist</b>: {0}", metadata.artist.Name);
            levelWindow.transform.Find("song").GetComponent<TextMeshProUGUI>().text = string.Format("<b>Song</b>: {0}", metadata.song.title);
            levelWindow.transform.Find("creator").GetComponent<TextMeshProUGUI>().text = string.Format("<b>Creator</b>: {0}", metadata.creator.steam_name);
            levelWindow.transform.Find("difficulty").GetComponent<TextMeshProUGUI>().text = $"<b>Difficulty</b>: <b><color=#{LSColors.ColorToHex(metadata.song.getDifficultyColor())}>{metadata.song.getDifficulty()}</color></b>";

            List<string> stringList = LSText.WordWrap(metadata.song.description, 60);
            string str = "";
            int num = Mathf.Clamp(stringList.Count, 0, 3);

            for (int i = 0; i < num; i++)
            {
                str += stringList[i] + "<br>";
            }

            levelWindow.transform.Find("description").GetComponent<TextMeshProUGUI>().text = string.Format("<b>Description</b>: <br>{0}", LSText.ClampString(str, 400));

            var add = levelWindow.transform.Find("buttons/add");
            add.GetChild(0).GetComponent<TextMeshProUGUI>().text = LevelManager.ArcadeQueue.Has(x => x.id == level.id) ? "[REMOVE FROM<br> QUEUE]" : "[ADD TO QUEUE]";
            var addButton = add.GetComponent<Button>();
            addButton.onClick.RemoveAllListeners();
            addButton.onClick.AddListener(delegate ()
            {
                if (LevelManager.ArcadeQueue.Has(x => x.id == level.id))
                {
                    LevelManager.ArcadeQueue.RemoveAll(x => x.id == level.id);
                }
                else
                {
                    LevelManager.ArcadeQueue.Add(level);
                }

                add.GetChild(0).GetComponent<TextMeshProUGUI>().text = LevelManager.ArcadeQueue.Has(x => x.id == level.id) ? "[REMOVE FROM<br> QUEUE]" : "[ADD TO QUEUE]";
            });

            inst.selected = true;
        }

        public static IEnumerator SetCameraAlphaFade(int duration)
        {
            float percent = 0f;
            for (int i = 0; i < duration; i++)
            {
                yield return new WaitForSeconds(0.05f);
                videoPlayer.targetCameraAlpha = percent / 2f;
                percent += 0.05f;
            }
            yield break;
        }

        public static IEnumerator FixPopup()
        {
            yield return new WaitForSeconds(0.2f);
            levelList.transform.localScale = Vector3.one;
            yield break;
        }

        public static IEnumerator GenerateOpenFilePopup()
        {
            var inter = new GameObject("Interface");
            inter.transform.localScale = Vector3.one * screenScale;
            menuUI = inter;
            var interfaceRT = inter.AddComponent<RectTransform>();
            interfaceRT.anchoredPosition = new Vector2(960f, 540f);
            interfaceRT.sizeDelta = new Vector2(1920f, 1080f);
            interfaceRT.pivot = new Vector2(0.5f, 0.5f);
            interfaceRT.anchorMin = Vector2.zero;
            interfaceRT.anchorMax = Vector2.zero;

            var canvas = inter.AddComponent<Canvas>();
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.Tangent;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.Normal;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.scaleFactor = screenScale;
            canvas.sortingOrder = 10000;

            var canvasScaler = inter.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(Screen.width, Screen.height);

            inter.AddComponent<GraphicRaycaster>();

            var openFilePopup = GenerateUIImage("Open File Popup", inter.transform);
            var parent = ((GameObject)openFilePopup["GameObject"]).transform;

            var openFilePopupRT = (RectTransform)openFilePopup["RectTransform"];
            var zeroFive = new Vector2(0.5f, 0.5f);
            SetRectTransform(openFilePopupRT, Vector2.zero, zeroFive, zeroFive, zeroFive, new Vector2(1000f, 800f));

            ((Image)openFilePopup["Image"]).color = new Color(0.1216f, 0.1216f, 0.1216f, 1f);

            var scrollRect = ((GameObject)openFilePopup["GameObject"]).AddComponent<ScrollRect>();

            var panel = GenerateUIImage("Panel", parent);

            var panelRT = (RectTransform)panel["RectTransform"];
            SetRectTransform(panelRT, Vector2.zero, Vector2.one, Vector2.up, Vector2.zero, new Vector2(32f, 32f));

            ((Image)panel["Image"]).color = new Color(0.1216f, 0.1216f, 0.1216f, 1f);

            var searchBox = new GameObject("search-box");
            searchBox.transform.SetParent(parent);
            var searchBoxRT = searchBox.AddComponent<RectTransform>();
            SetRectTransform(searchBoxRT, Vector2.zero, Vector2.one, Vector2.up, new Vector2(0.5f, 1f), new Vector2(0f, 32f));

            var search = GenerateUIInputField("search", searchBox.transform);
            var searchRT = (RectTransform)search["RectTransform"];
            SetRectTransform(searchRT, Vector2.zero, Vector2.one, Vector2.up, new Vector2(0.5f, 1f), new Vector2(0f, 32f));
            ((Text)search["Placeholder"]).text = "Search for a level...";
            ((Text)search["Placeholder"]).color = new Color(0.9373f, 0.9216f, 0.9373f, 0.502f);
            ((Text)search["Text"]).color = new Color(0.9333f, 0.9176f, 0.9333f, 1f);

            ((Image)search["Image"]).color = new Color(0.1961f, 0.1961f, 0.1961f, 1f);

            var mask = GenerateUIImage("mask", parent);
            var maskMask = ((GameObject)mask["GameObject"]).AddComponent<Mask>();
            var maskRT = (RectTransform)mask["RectTransform"];
            SetRectTransform(maskRT, new Vector2(0f, -16f), Vector2.one, Vector2.zero, zeroFive, new Vector2(0f, -32f));
            maskMask.showMaskGraphic = false;

            var content = new GameObject("content");
            content.transform.SetParent(((GameObject)mask["GameObject"]).transform);
            content.layer = 5;
            var contentRT = content.AddComponent<RectTransform>();
            SetRectTransform(contentRT, new Vector2(0f, 32f), Vector2.up, Vector2.up, Vector2.up, new Vector2(1000f, 4276f));

            var csf = content.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            csf.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var glg = content.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(984f, 32f);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 1;
            glg.spacing = new Vector2(0, 8);
            glg.startAxis = GridLayoutGroup.Axis.Vertical;
            glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
            glg.childAlignment = TextAnchor.UpperLeft;

            var scrollbar = GenerateUIImage("Scrollbar", parent);
            ((Image)scrollbar["Image"]).color = new Color(0.1216f, 0.1216f, 0.1216f, 1f);
            var scrollbarRT = (RectTransform)scrollbar["RectTransform"];
            SetRectTransform(scrollbarRT, Vector2.zero, Vector2.one, Vector2.right, new Vector2(0f, 0.5f), new Vector2(32f, 0f));

            var ssbar = ((GameObject)scrollbar["GameObject"]).AddComponent<Scrollbar>();

            dragger = ssbar;

            var slidingArea = new GameObject("Sliding Area");
            slidingArea.transform.SetParent(((GameObject)scrollbar["GameObject"]).transform);
            SetRectTransform(slidingArea.AddComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, zeroFive, new Vector2(-20f, -20f));

            var handle = GenerateUIImage("Handle", slidingArea.transform);
            var handleRT = (RectTransform)handle["RectTransform"];
            SetRectTransform(handleRT, Vector2.zero, Vector2.one, Vector2.zero, zeroFive, new Vector2(20f, 20f));
            ((Image)handle["Image"]).color = Color.white;

            scrollRect.content = contentRT;
            scrollRect.horizontal = false;
            scrollRect.scrollSensitivity = 20f;
            scrollRect.verticalScrollbar = ssbar;

            ssbar.direction = Scrollbar.Direction.BottomToTop;
            ssbar.numberOfSteps = 0;
            ssbar.handleRect = handleRT;

            var storyPath = GenerateUIInputField("story path", parent);
            SetRectTransform((RectTransform)storyPath["RectTransform"], new Vector2(-650f, 815f), Vector2.right, Vector2.right, zeroFive, new Vector2(254f, 34f));
            ((Image)storyPath["Image"]).color = new Color(0.9333f, 0.9176f, 0.9333f, 1f);
            ((Text)storyPath["Text"]).color = new Color(0.1216f, 0.1216f, 0.1216f, 1f);
            ((Text)storyPath["Placeholder"]).color = new Color(0.1216f, 0.1216f, 0.1216f, 1f);
            ((Text)storyPath["Placeholder"]).text = "Set a path...";

            var toggleClone = new GameObject("toggle");
            toggleClone.transform.SetParent(parent);
            toggleClone.layer = 5;
            var toggleCloneRT = toggleClone.AddComponent<RectTransform>();
            SetRectTransform(toggleCloneRT, new Vector2(1062f, 16f), Vector2.up, Vector2.up, zeroFive, new Vector2(757f, 32f));
            toggleClone.AddComponent<CanvasRenderer>();
            var horizontalGroup = toggleClone.AddComponent<HorizontalLayoutGroup>();
            horizontalGroup.childControlHeight = false;
            horizontalGroup.childControlWidth = false;
            horizontalGroup.childForceExpandHeight = false;
            horizontalGroup.childForceExpandWidth = false;
            horizontalGroup.childScaleHeight = false;
            horizontalGroup.childScaleWidth = false;
            horizontalGroup.spacing = 4;
            horizontalGroup.childAlignment = TextAnchor.UpperLeft;

            var titlet = GenerateUIText("title", toggleClone.transform);
            SetRectTransform((RectTransform)titlet["RectTransform"], new Vector2(55f, -16f), Vector2.up, Vector2.up, zeroFive, new Vector2(110f, 32f));
            var titletext = (Text)titlet["Text"];
            titletext.text = "Descending?";

            var e = (from x in Resources.FindObjectsOfTypeAll<Font>()
                     where x.name == "Inconsolata-Bold"
                     select x).ToList();

            if (e.Count > 0 && e[0] != null)
            {
                titletext.font = e[0];
            }
            else
            {
                titletext.font = inconsolataFont;
            }

            titletext.alignment = TextAnchor.MiddleLeft;
            titletext.fontSize = 20;
            titletext.horizontalOverflow = HorizontalWrapMode.Wrap;
            titletext.verticalOverflow = VerticalWrapMode.Truncate;

            var toggler = GenerateUIToggle("toggle", toggleClone.transform);
            SetRectTransform((RectTransform)toggler["RectTransform"], new Vector2(130f, -16f), Vector2.up, Vector2.up, zeroFive, new Vector2(32f, 32f));
            SetRectTransform((RectTransform)toggler["BackgroundRT"], Vector2.zero, Vector2.up, Vector2.up, Vector2.up, new Vector2(32f, 32f));
            SetRectTransform((RectTransform)toggler["CheckmarkRT"], Vector2.zero, zeroFive, zeroFive, zeroFive, new Vector2(20f, 20f));

            var dropdown = GenerateUIDropdown("orderby dropdown", parent);
            var actualDD = (Dropdown)dropdown["Dropdown"];
            actualDD.options = new List<Dropdown.OptionData>
            {
                new Dropdown.OptionData("Cover"),
                new Dropdown.OptionData("Artist"),
                new Dropdown.OptionData("Creator"),
                new Dropdown.OptionData("Folder"),
                new Dropdown.OptionData("Title"),
                new Dropdown.OptionData("Difficulty"),
                new Dropdown.OptionData("Date Edited")
            };

            SetRectTransform((RectTransform)dropdown["RectTransform"], new Vector2(933f, 816f), Vector2.zero, Vector2.zero, zeroFive, new Vector2(198f, 32f));

            var reload = GenerateUIButton("reload", parent);
            SetRectTransform((RectTransform)reload["RectTransform"], new Vector2(480f, 832f), Vector2.zero, Vector2.zero, Vector2.up, new Vector2(32f, 32f));
            GetImage((Image)reload["Image"], "BepInEx/plugins/Assets/editor_gui_refresh-white.png");

            levelList = (GameObject)openFilePopup["GameObject"];
            yield break;
        }

        public static GameObject FolderButton()
        {
            var button = GenerateUIButton("folder", null);
            var folderName = GenerateUITextMeshPro("folder-name", ((GameObject)button["GameObject"]).transform);

            var brt = (RectTransform)button["RectTransform"];
            SetRectTransform(brt, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.right, new Vector2(96f, 32f));

            var buttonButt = (Button)button["Button"];
            ColorBlock cb = buttonButt.colors;
            cb.normalColor = new Color(0.1647f, 0.1647f, 0.1647f, 1f);
            var def = new Color(0.2588f, 0.2588f, 0.2588f, 1f);
            cb.highlightedColor = def;
            cb.pressedColor = def;
            cb.selectedColor = def;
            cb.disabledColor = new Color(0.7843f, 0.7843f, 0.7843f, 0.502f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.2f;
            buttonButt.colors = cb;

            var fnrt = (RectTransform)folderName["RectTransform"];
            SetRectTransform(fnrt, new Vector2(2f, 0f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-12f, -8f));

            return (GameObject)button["GameObject"];
        }

        public static void SetRectTransform(RectTransform _rt, Vector2 _anchoredPos, Vector2 _anchorMax, Vector2 _anchorMin, Vector2 _pivot, Vector2 _sizeDelta)
        {
            _rt.anchoredPosition = _anchoredPos;
            _rt.anchorMax = _anchorMax;
            _rt.anchorMin = _anchorMin;
            _rt.pivot = _pivot;
            _rt.sizeDelta = _sizeDelta;
        }

        public static Dictionary<string, object> GenerateUIImage(string _name, Transform _parent)
        {
            var dictionary = new Dictionary<string, object>();
            var gameObject = new GameObject(_name);
            gameObject.transform.SetParent(_parent);
            gameObject.layer = 5;

            dictionary.Add("GameObject", gameObject);
            dictionary.Add("RectTransform", gameObject.AddComponent<RectTransform>());
            dictionary.Add("CanvasRenderer", gameObject.AddComponent<CanvasRenderer>());
            dictionary.Add("Image", gameObject.AddComponent<Image>());

            return dictionary;
        }

        public static Dictionary<string, object> GenerateUIText(string _name, Transform _parent)
        {
            var dictionary = new Dictionary<string, object>();
            var gameObject = new GameObject(_name);
            gameObject.transform.SetParent(_parent);
            gameObject.layer = 5;

            dictionary.Add("GameObject", gameObject);
            dictionary.Add("RectTransform", gameObject.AddComponent<RectTransform>());
            dictionary.Add("CanvasRenderer", gameObject.AddComponent<CanvasRenderer>());
            var text = gameObject.AddComponent<Text>();
            text.font = Font.GetDefault();
            text.fontSize = 20;
            dictionary.Add("Text", text);

            return dictionary;
        }

        public static Dictionary<string, object> GenerateUITextMeshPro(string _name, Transform _parent, bool _noFont = false)
        {
            var dictionary = new Dictionary<string, object>();
            var gameObject = Instantiate(textMeshPro);
            gameObject.name = _name;
            gameObject.transform.SetParent(_parent);

            dictionary.Add("GameObject", gameObject);
            dictionary.Add("RectTransform", gameObject.GetComponent<RectTransform>());
            dictionary.Add("CanvasRenderer", gameObject.GetComponent<CanvasRenderer>());
            var text = gameObject.GetComponent<TextMeshProUGUI>();

            if (_noFont)
            {
                var refer = MaterialReferenceManager.instance;
                var dictionary2 = (Dictionary<int, TMP_FontAsset>)refer.GetType().GetField("m_FontAssetReferenceLookup", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(refer);

                TMP_FontAsset tmpFont;
                if (dictionary2.ToList().Find(x => x.Value.name == "Arial").Value != null)
                {
                    tmpFont = dictionary2.ToList().Find(x => x.Value.name == "Arial").Value;
                }
                else
                {
                    tmpFont = dictionary2.ToList().Find(x => x.Value.name == "Liberation Sans SDF").Value;
                }

                text.font = tmpFont;
                text.fontSize = 20;
            }

            dictionary.Add("Text", text);

            return dictionary;
        }

        public static Dictionary<string, object> GenerateUIInputField(string _name, Transform _parent)
        {
            var dictionary = new Dictionary<string, object>();
            var image = GenerateUIImage(_name, _parent);
            var text = GenerateUIText("text", ((GameObject)image["GameObject"]).transform);
            var placeholder = GenerateUIText("placeholder", ((GameObject)image["GameObject"]).transform);

            SetRectTransform((RectTransform)text["RectTransform"], new Vector2(2f, 0f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-12f, -8f));
            SetRectTransform((RectTransform)placeholder["RectTransform"], new Vector2(2f, 0f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-12f, -8f));

            dictionary.Add("GameObject", image["GameObject"]);
            dictionary.Add("RectTransform", image["RectTransform"]);
            dictionary.Add("Image", image["Image"]);
            dictionary.Add("Text", text["Text"]);
            dictionary.Add("Placeholder", placeholder["Text"]);
            var inputField = ((GameObject)image["GameObject"]).AddComponent<InputField>();
            inputField.textComponent = (Text)text["Text"];
            inputField.placeholder = (Text)placeholder["Text"];
            dictionary.Add("InputField", inputField);

            return dictionary;
        }

        public static Dictionary<string, object> GenerateUIButton(string _name, Transform _parent)
        {
            var gameObject = GenerateUIImage(_name, _parent);
            gameObject.Add("Button", ((GameObject)gameObject["GameObject"]).AddComponent<Button>());

            return gameObject;
        }

        public static Dictionary<string, object> GenerateUIToggle(string _name, Transform _parent)
        {
            var dictionary = new Dictionary<string, object>();
            var gameObject = new GameObject(_name);
            gameObject.transform.SetParent(_parent);
            dictionary.Add("GameObject", gameObject);
            dictionary.Add("RectTransform", gameObject.AddComponent<RectTransform>());

            var bg = GenerateUIImage("Background", gameObject.transform);
            dictionary.Add("Background", bg["GameObject"]);
            dictionary.Add("BackgroundRT", bg["RectTransform"]);
            dictionary.Add("BackgroundImage", bg["Image"]);

            var checkmark = GenerateUIImage("Checkmark", ((GameObject)bg["GameObject"]).transform);
            dictionary.Add("Checkmark", checkmark["GameObject"]);
            dictionary.Add("CheckmarkRT", checkmark["RectTransform"]);
            dictionary.Add("CheckmarkImage", checkmark["Image"]);

            var toggle = gameObject.AddComponent<Toggle>();
            toggle.image = (Image)bg["Image"];
            toggle.targetGraphic = (Image)bg["Image"];
            toggle.graphic = (Image)checkmark["Image"];
            dictionary.Add("Toggle", toggle);

            ((Image)checkmark["Image"]).color = new Color(0.1216f, 0.1216f, 0.1216f, 1f);

            GetImage((Image)checkmark["Image"], "BepInEx/plugins/Assets/editor_gui_checkmark.png");

            return dictionary;
        }

        public static Dictionary<string, object> GenerateUIDropdown(string _name, Transform _parent)
        {
            var dictionary = new Dictionary<string, object>();
            var dropdownBase = GenerateUIImage(_name, _parent);
            dictionary.Add("GameObject", dropdownBase["GameObject"]);
            dictionary.Add("RectTransform", dropdownBase["RectTransform"]);
            dictionary.Add("Image", dropdownBase["Image"]);
            var dropdownD = ((GameObject)dropdownBase["GameObject"]).AddComponent<Dropdown>();
            dictionary.Add("Dropdown", dropdownD);

            var label = GenerateUIText("Label", ((GameObject)dropdownBase["GameObject"]).transform);
            ((Text)label["Text"]).color = new Color(0.1961f, 0.1961f, 0.1961f, 1f);
            ((Text)label["Text"]).alignment = TextAnchor.MiddleLeft;

            var arrow = GenerateUIImage("Arrow", ((GameObject)dropdownBase["GameObject"]).transform);
            var arrowImage = (Image)arrow["Image"];
            arrowImage.color = new Color(0.2157f, 0.2157f, 0.2196f, 1f);
            GetImage(arrowImage, "BepInEx/plugins/Assets/editor_gui_left.png");
            ((GameObject)arrow["GameObject"]).transform.rotation = Quaternion.Euler(0f, 0f, 90f);

            SetRectTransform((RectTransform)label["RectTransform"], new Vector2(-15.3f, 0f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-46.6f, 0f));
            SetRectTransform((RectTransform)arrow["RectTransform"], new Vector2(-2f, -0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0f), new Vector2(32f, 32f));

            var template = GenerateUIImage("Template", ((GameObject)dropdownBase["GameObject"]).transform);
            SetRectTransform((RectTransform)template["RectTransform"], new Vector2(0f, 2f), Vector2.right, Vector2.zero, new Vector2(0.5f, 1f), new Vector2(0f, 192f));
            var scrollRect = ((GameObject)template["GameObject"]).AddComponent<ScrollRect>();


            var viewport = GenerateUIImage("Viewport", ((GameObject)template["GameObject"]).transform);
            SetRectTransform((RectTransform)viewport["RectTransform"], Vector2.zero, Vector2.one, Vector2.zero, Vector2.up, Vector2.zero);
            var mask = ((GameObject)viewport["GameObject"]).AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var scrollbar = GenerateUIImage("Scrollbar", ((GameObject)template["GameObject"]).transform);
            SetRectTransform((RectTransform)scrollbar["RectTransform"], Vector2.zero, Vector2.one, Vector2.right, Vector2.one, new Vector2(20f, 0f));
            var ssbar = ((GameObject)scrollbar["GameObject"]).AddComponent<Scrollbar>();

            var slidingArea = new GameObject("Sliding Area");
            slidingArea.transform.SetParent(((GameObject)scrollbar["GameObject"]).transform);
            slidingArea.layer = 5;
            var slidingAreaRT = slidingArea.AddComponent<RectTransform>();
            SetRectTransform(slidingAreaRT, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-20f, -20f));

            var handle = GenerateUIImage("Handle", slidingArea.transform);
            SetRectTransform((RectTransform)handle["RectTransform"], Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(20f, 20f));
            ((Image)handle["Image"]).color = new Color(0.1216f, 0.1216f, 0.1216f, 1f);

            var content = new GameObject("Content");
            content.transform.SetParent(((GameObject)viewport["GameObject"]).transform);
            content.layer = 5;
            var contentRT = content.AddComponent<RectTransform>();
            SetRectTransform(contentRT, Vector2.zero, Vector2.one, Vector2.up, new Vector2(0.5f, 1f), new Vector2(0f, 32f));

            scrollRect.content = contentRT;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.vertical = true;
            scrollRect.verticalScrollbar = ssbar;
            scrollRect.viewport = (RectTransform)viewport["RectTransform"];
            ssbar.handleRect = (RectTransform)handle["RectTransform"];
            ssbar.direction = Scrollbar.Direction.BottomToTop;
            ssbar.numberOfSteps = 0;

            var item = new GameObject("Item");
            item.transform.SetParent(content.transform);
            item.layer = 5;
            var itemRT = item.AddComponent<RectTransform>();
            SetRectTransform(itemRT, Vector2.zero, new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 32f));
            var itemToggle = item.AddComponent<Toggle>();

            var itemBackground = GenerateUIImage("Item Background", item.transform);
            SetRectTransform((RectTransform)itemBackground["RectTransform"], Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);
            ((Image)itemBackground["Image"]).color = new Color(0.9608f, 0.9608f, 0.9608f, 1f);

            var itemCheckmark = GenerateUIImage("Item Checkmark", item.transform);
            SetRectTransform((RectTransform)itemCheckmark["RectTransform"], new Vector2(8f, 0f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(32f, 32f));
            var itemCheckImage = (Image)itemCheckmark["Image"];
            itemCheckImage.color = new Color(0.1216f, 0.1216f, 0.1216f, 1f);
            GetImage(itemCheckImage, "BepInEx/plugins/Assets/editor_gui_diamond.png");

            var itemLabel = GenerateUIText("Item Label", item.transform);
            SetRectTransform((RectTransform)itemLabel["RectTransform"], new Vector2(15f, 0.5f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-50f, -3f));
            var itemLabelText = (Text)itemLabel["Text"];
            itemLabelText.alignment = TextAnchor.MiddleLeft;
            itemLabelText.font = inconsolataFont;
            itemLabelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            itemLabelText.verticalOverflow = VerticalWrapMode.Truncate;
            itemLabelText.text = "Option A";
            itemLabelText.color = new Color(0.1961f, 0.1961f, 0.1961f, 1f);

            itemToggle.image = (Image)itemBackground["Image"];
            itemToggle.targetGraphic = (Image)itemBackground["Image"];
            itemToggle.graphic = itemCheckImage;

            dropdownD.captionText = (Text)label["Text"];
            dropdownD.itemText = itemLabelText;
            dropdownD.alphaFadeSpeed = 0.15f;
            dropdownD.template = (RectTransform)template["RectTransform"];
            ((GameObject)template["GameObject"]).SetActive(false);

            return dictionary;
        }

        public static void GetImage(Image _image, string _filePath)
        {
            if (RTFile.FileExists(_filePath))
                _image.sprite = SpriteHelper.LoadSprite(_filePath);
        }

        public static string publishedLevels;
    }
}
