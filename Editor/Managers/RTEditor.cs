using BepInEx.Configuration;
using BetterLegacy.Components;
using BetterLegacy.Components.Editor;
using BetterLegacy.Components.Player;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Core.Optimization.Objects;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Example;
using CielaSpike;
using Crosstales.FB;
using HarmonyLib;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AutoKillType = DataManager.GameData.BeatmapObject.AutoKillType;
using BaseBackgroundObject = DataManager.GameData.BackgroundObject;
using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BaseEventKeyframe = DataManager.GameData.EventKeyframe;
using EventKeyframeSelection = EventEditor.KeyframeSelection;
using MetadataWrapper = EditorManager.MetadataWrapper;
using ObjectType = DataManager.GameData.BeatmapObject.ObjectType;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Base modded editor manager.
    /// </summary>
    public class RTEditor : MonoBehaviour
    {
        public static RTEditor inst;
        public List<EditorThemeManager.EditorTheme> EditorThemes => EditorThemeManager.EditorThemes;

        public float timeInEditorOffset;
        public static void Init(EditorManager editorManager) => editorManager?.gameObject?.AddComponent<RTEditor>();

        void Awake()
        {
            inst = this;

            timeOffset = Time.time;
            timeInEditorOffset = Time.time;

            try
            {
                if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + editorListPath))
                    Directory.CreateDirectory(RTFile.ApplicationDirectory + editorListPath);
                if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + prefabListPath))
                    Directory.CreateDirectory(RTFile.ApplicationDirectory + prefabListPath);
                if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + themeListPath))
                    Directory.CreateDirectory(RTFile.ApplicationDirectory + themeListPath);

                PrefabWatcher = new FileSystemWatcher
                {
                    Path = RTFile.ApplicationDirectory + prefabListPath,
                    Filter = "*.lsp",
                };
                EnablePrefabWatcher();

                ThemeWatcher = new FileSystemWatcher
                {
                    Path = RTFile.ApplicationDirectory + themeListPath,
                    Filter = "*.lst"
                };
                EnableThemeWatcher();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }

            popups = GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups").transform;
            wholeTimeline = EditorManager.inst.timelineSlider.transform.parent.parent;

            var prefabParent = Creator.NewGameObject("prefabs", transform);
            var prefabHolder = EditorPrefabHolder.Instance;
            prefabHolder.PrefabParent = prefabParent.transform;

            if (ObjEditor.inst)
            {
                prefabHolder.NumberInputField = ObjEditor.inst.ObjectView.transform.Find("time").gameObject.Duplicate(prefabHolder.PrefabParent, "float input");

                var floatInputFieldStorage = prefabHolder.NumberInputField.AddComponent<InputFieldStorage>();
                floatInputFieldStorage.leftGreaterButton = prefabHolder.NumberInputField.transform.Find("<<").GetComponent<Button>();
                DestroyImmediate(floatInputFieldStorage.leftGreaterButton.GetComponent<Animator>());
                floatInputFieldStorage.leftGreaterButton.transition = Selectable.Transition.ColorTint;
                floatInputFieldStorage.leftButton = prefabHolder.NumberInputField.transform.Find("<").GetComponent<Button>();
                DestroyImmediate(floatInputFieldStorage.leftButton.GetComponent<Animator>());
                floatInputFieldStorage.leftButton.transition = Selectable.Transition.ColorTint;
                floatInputFieldStorage.middleButton = prefabHolder.NumberInputField.transform.Find("|").GetComponent<Button>();
                DestroyImmediate(floatInputFieldStorage.middleButton.GetComponent<Animator>());
                floatInputFieldStorage.middleButton.transition = Selectable.Transition.ColorTint;
                floatInputFieldStorage.rightButton = prefabHolder.NumberInputField.transform.Find(">").GetComponent<Button>();
                DestroyImmediate(floatInputFieldStorage.rightButton.GetComponent<Animator>());
                floatInputFieldStorage.rightButton.transition = Selectable.Transition.ColorTint;
                floatInputFieldStorage.rightGreaterButton = prefabHolder.NumberInputField.transform.Find(">>").GetComponent<Button>();
                DestroyImmediate(floatInputFieldStorage.rightGreaterButton.GetComponent<Animator>());
                floatInputFieldStorage.rightGreaterButton.transition = Selectable.Transition.ColorTint;
                floatInputFieldStorage.inputField = prefabHolder.NumberInputField.transform.Find("time").GetComponent<InputField>();
                floatInputFieldStorage.inputField.characterValidation = InputField.CharacterValidation.None;
                floatInputFieldStorage.inputField.characterLimit = 0;
                prefabHolder.NumberInputField.transform.Find("time").gameObject.name = "input";

                if (prefabHolder.NumberInputField.transform.Find("lock"))
                    DestroyImmediate(prefabHolder.NumberInputField.transform.Find("lock").gameObject);

                prefabHolder.StringInputField = floatInputFieldStorage.inputField.gameObject.Duplicate(prefabHolder.PrefabParent, "string input");

                prefabHolder.Function2Button = ObjEditor.inst.ObjectView.transform.Find("applyprefab").gameObject.Duplicate(prefabHolder.PrefabParent, "function 2 button");

                var functionButtonStorage = prefabHolder.Function2Button.AddComponent<FunctionButtonStorage>();
                functionButtonStorage.button = prefabHolder.Function2Button.GetComponent<Button>();
                functionButtonStorage.text = prefabHolder.Function2Button.transform.GetChild(0).GetComponent<Text>();
                Destroy(prefabHolder.Function2Button.GetComponent<Animator>());
                functionButtonStorage.button.transition = Selectable.Transition.ColorTint;

                prefabHolder.Dropdown = ObjEditor.inst.KeyframeDialogs[0].transform.Find("curves").gameObject.Duplicate(prefabHolder.PrefabParent, "dropdown");
            }

            if (PrefabEditor.inst)
            {
                prefabHolder.DeleteButton = PrefabEditor.inst.AddPrefab.transform.Find("delete").gameObject.Duplicate(prefabHolder.PrefabParent, "delete");
                var deleteButtonStorage = prefabHolder.DeleteButton.AddComponent<DeleteButtonStorage>();
                deleteButtonStorage.button = prefabHolder.DeleteButton.GetComponent<Button>();
                deleteButtonStorage.baseImage = deleteButtonStorage.button.image;
                deleteButtonStorage.image = prefabHolder.DeleteButton.transform.GetChild(0).GetComponent<Image>();

                timelineBar = GameObject.Find("TimelineBar/GameObject");
                timeDefault = timelineBar.transform.GetChild(0).gameObject;
                timeDefault.name = "Time Default";

                var defaultInputField = timelineBar.transform.Find("Time");
                defaultIF = defaultInputField.gameObject;
                defaultIF.SetActive(true);
                defaultInputField.SetParent(transform);
                defaultInputField.localScale = Vector3.one;
                EditorManager.inst.speedText.transform.parent.SetParent(transform);

                if (defaultIF.TryGetComponent(out InputField frick))
                    frick.textComponent.fontSize = 18;

                tagPrefab = Creator.NewUIObject("Tag", transform);
                var tagPrefabImage = tagPrefab.AddComponent<Image>();
                tagPrefabImage.color = new Color(1f, 1f, 1f, 1f);
                var tagPrefabLayout = tagPrefab.AddComponent<HorizontalLayoutGroup>();
                tagPrefabLayout.childControlWidth = false;
                tagPrefabLayout.childForceExpandWidth = false;

                var input = defaultIF.Duplicate(tagPrefab.transform, "Input");
                input.transform.localScale = Vector3.one;
                input.transform.AsRT().sizeDelta = new Vector2(136f, 32f);
                var text = input.transform.Find("Text").GetComponent<Text>();
                text.alignment = TextAnchor.MiddleCenter;
                text.fontSize = 17;

                var delete = prefabHolder.DeleteButton.Duplicate(tagPrefab.transform, "Delete");
                UIManager.SetRectTransform(delete.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.one, Vector2.one, new Vector2(32f, 32f));
            }

            prefabHolder.Toggle = EditorManager.inst.GetDialog("Settings Editor").Dialog.Find("snap/toggle/toggle").gameObject.Duplicate(prefabHolder.PrefabParent, "toggle");

            prefabHolder.Function1Button = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/event").Duplicate(prefabHolder.PrefabParent, "function 1 button");
            var functionButton1Storage = prefabHolder.Function1Button.AddComponent<FunctionButtonStorage>();
            functionButton1Storage.button = prefabHolder.Function1Button.GetComponent<Button>();
            functionButton1Storage.button.onClick.ClearAll();
            functionButton1Storage.text = prefabHolder.Function1Button.transform.GetChild(0).GetComponent<Text>();

            CloseSprite = SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_close.png");
            ReloadSprite = SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_refresh-white.png");

            if (!RTFile.FileExists(EditorSettingsPath))
                CreateGlobalSettings();
            else
                LoadGlobalSettings();

            SetupNotificationValues();
            SetupTimelineBar();
            SetupTimelineTriggers();
            SetupSelectGUI();
            SetupCreateObjects();
            SetupDropdowns();
            SetupDoggo();
            SetupFileBrowser();
            SetupPaths();
            SetupTimelinePreview();
            SetupTimelineElements();
            SetupGrid();
            SetupTimelineGrid();
            SetupNewFilePopup();
            CreatePreviewCover();
            CreateObjectSearch();
            CreateWarningPopup();
            CreateMultiObjectEditor();
            CreateDocumentation();
            CreateDebug();
            CreateAutosavePopup();
            SetupMiscEditorThemes();
            CreateScreenshotsView();

            // Editor initializations
            RTMarkerEditor.Init();
            RTMetaDataEditor.Init();
            RTEventEditor.Init();
            RTThemeEditor.Init();
            RTPrefabEditor.Init();

            TextEditor.Init();
            KeybindManager.Init();
            PlayerEditor.Init();
            ObjectModifiersEditor.Init();
            LevelCombiner.Init();
            ProjectPlannerManager.Init();
            UploadedLevelsManager.Init();

            mousePicker = Creator.NewUIObject("picker", EditorManager.inst.dialogs.parent);
            mousePicker.transform.localScale = Vector3.one;
            mousePickerRT = mousePicker.transform.AsRT();

            var img = Creator.NewUIObject("image", mousePickerRT);
            img.transform.localScale = Vector3.one;

            img.transform.AsRT().anchoredPosition = new Vector2(-930f, -520f);
            img.transform.AsRT().sizeDelta = new Vector2(32f, 32f);

            var image = img.AddComponent<Image>();

            dropperSprite = SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_dropper.png");
            image.sprite = dropperSprite;

            timelineTime = EditorManager.inst.timelineTime.GetComponent<Text>();
            SetNotificationProperties();

            timelineSlider = EditorManager.inst.timelineSlider.GetComponent<Slider>();
            TriggerHelper.AddEventTriggers(timelineSlider.gameObject, TriggerHelper.CreateEntry(EventTriggerType.PointerDown, eventData =>
            {
                if (!EditorConfig.Instance.DraggingMainCursorFix.Value)
                    return;

                changingTime = true;
                newTime = timelineSlider.value / EditorManager.inst.Zoom;
                AudioManager.inst.SetMusicTime(Mathf.Clamp(timelineSlider.value / EditorManager.inst.Zoom, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
            }), TriggerHelper.CreateEntry(EventTriggerType.PointerUp, eventData =>
            {
                if (!EditorConfig.Instance.DraggingMainCursorFix.Value)
                    return;

                newTime = timelineSlider.value / EditorManager.inst.Zoom;
                AudioManager.inst.SetMusicTime(Mathf.Clamp(timelineSlider.value / EditorManager.inst.Zoom, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
                changingTime = false;
            }));
            timelineSlider.onValueChanged.AddListener(x =>
            {
                if (EditorConfig.Instance.UpdateHomingKeyframesDrag.Value)
                    System.Threading.Tasks.Task.Run(Updater.UpdateHomingKeyframes);
            });

            DestroyImmediate(EditorManager.inst.mouseTooltip);
            mouseTooltip = EditorManager.inst.notificationPrefabs[0].Duplicate(EditorManager.inst.notification.transform.parent, "tooltip");
            EditorManager.inst.mouseTooltip = mouseTooltip;
            mouseTooltipRT = mouseTooltip.transform.AsRT();
            UIManager.SetRectTransform(mouseTooltipRT, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(250f, 32f));
            mouseTooltipRT.localScale = new Vector3(0.9f, 0.9f, 1f);
            mouseTooltipText = mouseTooltip.transform.Find("text").GetComponent<TextMeshProUGUI>();

            EditorThemeManager.AddGraphic(mouseTooltip.GetComponent<Image>(), ThemeGroup.Notification_Background, true);
            EditorThemeManager.AddGraphic(mouseTooltipRT.Find("bg/bg").GetComponent<Image>(), ThemeGroup.Notification_Info, true, roundedSide: SpriteHelper.RoundedSide.Top);
            EditorThemeManager.AddLightText(mouseTooltipText);
            EditorThemeManager.AddGraphic(mouseTooltipRT.Find("bg/Image").GetComponent<Image>(), ThemeGroup.Light_Text);
            EditorThemeManager.AddLightText(mouseTooltipRT.Find("bg/title").GetComponent<Text>());

            var timelineParent = Creator.NewUIObject("Timeline Objects", EditorManager.inst.timeline.transform, 1);
            timelineObjectsParent = timelineParent.transform.AsRT();
            RectValues.FullAnchored.AssignToRectTransform(timelineObjectsParent);

            CreateContextMenu();
            CreateFolderCreator();
        }

        void Update()
        {
            timeEditing = Time.time - timeOffset + savedTimeEditng;

            if (Input.GetMouseButtonDown(1) && (parentPickerEnabled || prefabPickerEnabled || onSelectTimelineObject != null))
            {
                parentPickerEnabled = false;
                prefabPickerEnabled = false;
                onSelectTimelineObject = null;
            }

            var pickerActive = parentPickerEnabled || prefabPickerEnabled || onSelectTimelineObject != null;
            mousePicker?.SetActive(pickerActive);

            if (mousePicker && mousePickerRT && pickerActive)
                mousePickerRT.anchoredPosition = Input.mousePosition * CoreHelper.ScreenScaleInverse;
            UpdateTooltip();

            if (!changingTime && EditorConfig.Instance.DraggingMainCursorFix.Value)
            {
                newTime = Mathf.Clamp(AudioManager.inst.CurrentAudioSource.time, 0f, AudioManager.inst.CurrentAudioSource.clip.length) * EditorManager.inst.Zoom;
                timelineSlider.value = newTime;
            }
            else if (EditorConfig.Instance.DraggingMainCursorFix.Value)
            {
                newTime = timelineSlider.value / EditorManager.inst.Zoom;
                AudioManager.inst.SetMusicTime(Mathf.Clamp(timelineSlider.value / EditorManager.inst.Zoom, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
            }

            try
            {
                if (previewGrid)
                {
                    var enabled = EditorConfig.Instance.PreviewGridEnabled.Value && EditorManager.inst.isEditing;
                    previewGrid.enabled = enabled;

                    if (enabled)
                    {
                        var camPos = EventManager.inst.camPos;
                        previewGrid.rectTransform.anchoredPosition =
                            new Vector2(-40f / previewGrid.gridSize.x, -40f / previewGrid.gridSize.y) + new Vector2((int)(camPos.x / 40f) * 40f, (int)(camPos.y / 40f) * 40f);
                    }
                }
            }
            catch
            {

            }

            if (selectingKey)
            {
                var key = CoreHelper.GetKeyCodeDown();

                if (key != KeyCode.None)
                {
                    selectingKey = false;

                    setKey?.Invoke(key);
                    onKeySet?.Invoke();
                }
            }

            if (GameManager.inst.timeline && timelinePreview)
                timelinePreview.gameObject.SetActive(GameManager.inst.timeline.activeSelf);

            if (CoreHelper.Playing && timelinePreview && AudioManager.inst.CurrentAudioSource.clip != null && GameManager.inst.timeline && GameManager.inst.timeline.activeSelf)
            {
                float num = AudioManager.inst.CurrentAudioSource.time * 400f / AudioManager.inst.CurrentAudioSource.clip.length;
                if (timelinePosition)
                {
                    timelinePosition.anchoredPosition = new Vector2(num, 0f);
                }

                timelinePreview.localPosition = GameManager.inst.timeline.transform.localPosition;
                timelinePreview.localScale = GameManager.inst.timeline.transform.localScale;
                timelinePreview.localRotation = GameManager.inst.timeline.transform.localRotation;

                for (int i = 0; i < checkpointImages.Count; i++)
                {
                    if (GameStorageManager.inst.checkpointImages.Count > i)
                        checkpointImages[i].color = GameStorageManager.inst.checkpointImages[i].color;
                }

                timelinePreviewPlayer.color = GameStorageManager.inst.timelinePlayer.color;
                timelinePreviewLeftCap.color = GameStorageManager.inst.timelineLeftCap.color;
                timelinePreviewRightCap.color = GameStorageManager.inst.timelineRightCap.color;
                timelinePreviewLine.color = GameStorageManager.inst.timelineLine.color;
            }

            if (timeEditing > 36000f)
                AchievementManager.inst.UnlockAchievement("serious_dedication");
            if (timeEditing > 86400f)
                AchievementManager.inst.UnlockAchievement("true_dedication");

            // Only want this during April Fools.
            //if (CoreHelper.AprilFools && RandomHelper.PercentChanceSingle(0.001f))
            //{
            //    var array = new string[]
            //    {
            //        "BRO",
            //        "Go touch some grass.",
            //        "Hello, hello? I wanted to record this message for you to get you settled in your first night. The animatronic characters DO get a bit quirky at night",
            //        "",
            //        "L + Ratio",
            //        "Hi Diggy",
            //        "Hi KarasuTori",
            //        "Hi MoNsTeR",
            //        "Hi RTMecha",
            //        "Hi Example",
            //        $"Hi {CoreConfig.Instance.DisplayName.Value}!",
            //        "Kweeble kweeble kweeble",
            //        "Testing... is this thing on?",
            //        "When life gives you lemons, don't make lemonade.",
            //        "AMONGUS",
            //        "I fear no man, but THAT thing, it scares me.",
            //        "/summon minecraft:wither",
            //        "Autobots, transform and roll out.",
            //        "sands undertraveler",
            //    };

            //    EditorManager.inst.DisplayNotification(array[UnityEngine.Random.Range(0, array.Length)], 4f, EditorManager.NotificationType.Info);
            //}
        }

        public bool showTootip;

        void UpdateTooltip()
        {
            tooltipTime = Time.time - tooltipTimeOffset;

            if (showTootip && tooltipTime >= EditorConfig.Instance.MouseTooltipHoverTime.Value)
            {
                showTootip = false;
                tooltipActive = true;

                mouseTooltip?.SetActive(true);

                if (mouseTooltipText)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(mouseTooltipText.rectTransform);
                if (mouseTooltipRT)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(mouseTooltipRT);
            }

            if (tooltipActive)
            {
                float num = CoreHelper.ScreenScaleInverse;
                float x = mouseTooltipRT.sizeDelta.x;
                float y = mouseTooltipRT.sizeDelta.y;
                var tooltipOffset = Vector3.zero;

                // flips tooltip if mouse is close to the edge of the screen.
                if ((Input.mousePosition.x + x) * num >= 1920f)
                    tooltipOffset.x -= x + 8f;
                else
                    tooltipOffset.x = 8f;

                // flips tooltip if mouse is close to the edge of the screen.
                if ((Input.mousePosition.y + y) * num >= 1080f)
                    tooltipOffset.y -= y;

                var position = (Input.mousePosition + tooltipOffset) * num;
                position.x = Mathf.Clamp(position.x, 40f, 1880f);
                position.y = Mathf.Clamp(position.y, 40f, 1040f);
                mouseTooltipRT.anchoredPosition = position;
            }

            if (tooltipTime - EditorConfig.Instance.MouseTooltipHoverTime.Value > maxTooltipTime && tooltipActive)
            {
                tooltipActive = false;
                mouseTooltip?.SetActive(false);
            }

            if (!EditorConfig.Instance.MouseTooltipDisplay.Value || !EditorManager.inst.showHelp)
            {
                mouseTooltip?.SetActive(false);
            }
        }

        void OnDestroy()
        {
            CoreHelper.LogError($"RTEditor was destroyed!");
            EditorConfig.UpdateEditorComplexity = null;
        }

        #region Variables

        public const float DEFAULT_CONTEXT_MENU_WIDTH = 300f;
        public const string DEFAULT_OBJECT_NAME = "\"Default object cameo\" -Viral Mecha";
        public const string BASE_CHECKPOINT_NAME = "Base Checkpoint";

        public Action<TimelineObject> onSelectTimelineObject;

        public GridRenderer previewGrid;

        public GameObject timeDefault;

        public Transform popups;

        public GameObject tagPrefab;

        public bool shouldCutLevel;
        public string copiedLevelPath;

        public EditorThemeManager.Element PreviewCover { get; set; }

        public static bool ShowModdedUI { get; set; }
        public static bool NotSimple => EditorConfig.Instance.EditorComplexity.Value != Complexity.Simple;

        public bool ienumRunning;

        public GameObject defaultIF;

        public string objectSearchTerm = "";

        public Transform titleBar;

        public static List<Dropdown> EasingDropdowns { get; set; } = new List<Dropdown>();

        List<MultiColorButton> multiColorButtons = new List<MultiColorButton>();
        List<MultiColorButton> multiGradientColorButtons = new List<MultiColorButton>();
        int currentMultiColorSelection = -1;
        int currentMultiGradientColorSelection = -1;

        #region Dragging

        public float dragOffset = -1f;
        public int dragBinOffset = -100;

        public static bool DraggingPlaysSound { get; set; }
        public static bool DraggingPlaysSoundBPM { get; set; }

        #endregion

        #region Tooltip

        public TextMeshProUGUI tooltipText;

        public bool tooltipActive;
        public float tooltipTime;
        public float tooltipTimeOffset;
        public float maxTooltipTime = 2f;

        #endregion

        #region Timeline

        public Slider timelineSlider;

        public Image timelineSliderHandle;
        public Image timelineSliderRuler;
        public Image keyframeTimelineSliderHandle;
        public Image keyframeTimelineSliderRuler;

        public bool isOverMainTimeline;
        public bool changingTime;
        public float newTime;

        public GridRenderer timelineGridRenderer;
        public Transform wholeTimeline;

        #endregion

        #region Loading and sorting

        public bool canUpdateThemes = true;
        public bool canUpdatePrefabs = true;

        public Dropdown levelOrderDropdown;
        public Toggle levelAscendToggle;
        public InputField editorPathField;
        public InputField themePathField;
        public InputField prefabPathField;

        public bool themesLoading = false;

        public GameObject themeAddButton;

        public bool prefabsLoading = false;
        public GameObject prefabExternalAddButton;

        #endregion

        #region Mouse Picker

        public GameObject mousePicker;
        RectTransform mousePickerRT;
        public GameObject mouseTooltip;
        public RectTransform mouseTooltipRT;
        public TextMeshProUGUI mouseTooltipText;
        public bool parentPickerEnabled = false;
        public bool prefabPickerEnabled = false;
        public bool selectingMultiple = false;

        #endregion

        #region Timeline Bar

        public GameObject timelineBar;
        public InputField timeField;
        public Text timelineTime;
        public InputField pitchField;
        public InputField editorLayerField;
        public Image editorLayerImage;
        public Toggle eventLayerToggle;

        #endregion

        #region File Info

        public Text fileInfoText;
        public Image doggoImage;

        #endregion

        #region Sprites

        public Sprite dropperSprite;

        Sprite searchSprite;
        public Sprite SearchSprite
        {
            get
            {
                if (!searchSprite)
                    searchSprite = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/parent/parent/image").GetComponent<Image>().sprite;
                return searchSprite;
            }
        }

        public static Sprite CloseSprite { get; set; }
        public static Sprite ReloadSprite { get; set; }

        #endregion

        #region Key selection

        public bool selectingKey = false;
        public Action onKeySet;
        public Action<KeyCode> setKey;

        #endregion

        #region Timeline

        public Image timelinePreviewPlayer;
        public Image timelinePreviewLine;
        public Image timelinePreviewLeftCap;
        public Image timelinePreviewRightCap;
        public List<Image> checkpointImages = new List<Image>();

        public RectTransform timelineObjectsParent;
        public Transform timelinePreview;
        public RectTransform timelinePosition;

        #endregion

        #region Documentation

        public List<EditorDocument> documentations = new List<EditorDocument>();

        public Text documentationTitle;
        public string documentationSearch;
        public Transform documentationContent;
        public Popup documentationPopup;

        #endregion

        #region Debugger

        public Popup debuggerPopup;
        public List<string> debugs = new List<string>();
        public List<GameObject> customFunctions = new List<GameObject>();
        public string debugSearch;

        #endregion

        #region Autosave

        public bool autoSaving = false;

        public string autosaveSearch;
        public Transform autosaveContent;
        public InputField autosaveSearchField;

        public static float timeSinceAutosaved;

        #endregion

        #region Screenshots

        public Transform screenshotContent;
        public InputField screenshotPageField;

        public int screenshotPage;
        public int screenshotsPerPage = 5;

        public int CurrentScreenshotPage => screenshotPage + 1;
        public int MinScreenshots => MaxScreenshots - screenshotsPerPage;
        public int MaxScreenshots => CurrentScreenshotPage * screenshotsPerPage;

        public int screenshotCount;

        #endregion

        #region New level creator

        public bool choosingLevelTemplate;
        public int currentLevelTemplate = -1;

        public Transform newLevelTemplateContent;
        public GameObject newLevelTemplatePrefab;
        public Sprite newLevelTemplateBaseSprite;
        public InputField nameInput;
        public Sprite currentTemplateSprite;

        public bool fromNewLevel;

        public List<string> NewLevelTemplates { get; set; } = new List<string>();

        #endregion

        #endregion

        #region Settings

        public float bpmOffset = 0f;

        public float timeOffset;
        public float timeEditing;
        public float savedTimeEditng;
        public int openAmount;

        public int levelFilter = 0;
        public bool levelAscend = true;

        public void SaveSettings()
        {
            var jn = JSON.Parse("{}");

            //original JSON: z
            jn["timeline"]["zoom"] = EditorManager.inst.zoomFloat.ToString("f3");
            //original JSON: tsc
            jn["timeline"]["position"] = EditorManager.inst.timelineScrollRectBar.value.ToString("f2");
            //original JSON: l
            jn["timeline"]["layer_type"] = ((int)layerType).ToString();
            jn["timeline"]["layer"] = EditorManager.inst.layer.ToString();
            //original JSON: t
            jn["editor"]["editing_time"] = timeEditing.ToString();
            //original JSON: a
            jn["editor"]["open_amount"] = openAmount.ToString();
            //original JSON: sn
            jn["misc"]["bpm_snap_active"] = SettingEditor.inst.SnapActive.ToString();
            //original JSON: so
            jn["misc"]["bpm_offset"] = bpmOffset.ToString();
            //original JSON: t
            jn["misc"]["time"] = AudioManager.inst.CurrentAudioSource.time.ToString();

            RTFile.WriteToFile(RTFile.CombinePaths(RTFile.BasePath, Level.EDITOR_LSE), jn.ToString(3));
        }

        public void LoadSettings()
        {
            if (!RTFile.FileExists(RTFile.CombinePaths(RTFile.BasePath, Level.EDITOR_LSE)))
            {
                savedTimeEditng = 0f;
                timeOffset = Time.time;
                return;
            }

            var jn = JSON.Parse(RTFile.ReadFromFile(RTFile.CombinePaths(RTFile.BasePath, Level.EDITOR_LSE)));

            if (jn["timeline"] != null)
            {
                var layer = 0;
                var layerType = LayerType.Objects;

                float zoom = 0.05f;
                float position = 0f;

                if (jn["timeline"]["z"] != null)
                    zoom = jn["timeline"]["z"].AsFloat;

                if (jn["timeline"]["tsc"] != null)
                    position = jn["timeline"]["tsc"].AsFloat;

                if (jn["timeline"]["zoom"] != null)
                    zoom = jn["timeline"]["zoom"].AsFloat;

                if (jn["timeline"]["position"] != null)
                    position = jn["timeline"]["position"].AsFloat;

                SetTimeline(zoom, position);

                if (jn["timeline"]["layer_type"] != null)
                    layerType = (LayerType)jn["timeline"]["layer_type"].AsInt;

                this.layerType = layerType;

                if (jn["timeline"]["l"] != null)
                    layer = jn["timeline"]["l"].AsInt;

                if (jn["timeline"]["layer"] != null)
                    layer = jn["timeline"]["layer"].AsInt;

                SetLayer(layer, false);
            }

            if (jn["editor"] != null)
            {
                if (jn["editor"]["t"] != null)
                    savedTimeEditng = jn["editor"]["t"].AsFloat;
                if (jn["editor"]["editing_time"] != null)
                    savedTimeEditng = jn["editor"]["editing_time"].AsFloat;

                if (jn["editor"]["a"] != null)
                    openAmount = jn["editor"]["a"].AsInt + 1;
                if (jn["editor"]["open_amount"] != null)
                    openAmount = jn["editor"]["open_amount"].AsInt + 1;
            }

            if (jn["misc"] != null)
            {
                if (jn["misc"]["sn"] != null)
                    SettingEditor.inst.SnapActive = jn["misc"]["sn"].AsBool;
                if (jn["misc"]["bpm_snap_active"] != null)
                    SettingEditor.inst.SnapActive = jn["misc"]["bpm_snap_active"].AsBool;

                var bpmOffset = 0f;
                if (jn["misc"]["so"] != null)
                    bpmOffset = jn["misc"]["so"].AsFloat;
                if (jn["misc"]["bpm_offset"] != null)
                    bpmOffset = jn["misc"]["bpm_offset"].AsFloat;
                this.bpmOffset = bpmOffset;

                float time = -1f;
                if (jn["misc"]["t"] != null)
                    time = jn["misc"]["t"].AsFloat;
                if (jn["misc"]["time"] != null)
                    time = jn["misc"]["time"].AsFloat;
                if (time >= 0f && time < AudioManager.inst.CurrentAudioSource.clip.length && EditorConfig.Instance.LevelLoadsLastTime.Value)
                    AudioManager.inst.SetMusicTime(time);

                SettingEditor.inst.SnapBPM = MetaData.Current.song.BPM;
            }

            prevLayer = EditorManager.inst.layer;
            prevLayerType = layerType;

            SetTimelineGridSize();
        }

        #endregion

        #region Notifications

        public List<string> notifications = new List<string>();

        public void DisplayNotification(string name, string text, float time, EditorManager.NotificationType type)
        {
            StartCoroutine(DisplayNotificationLoop(name, text, time, type));
        }

        public void DisplayCustomNotification(string _name, string _text, float _time, Color _base, Color _top, Color _icCol, string _title, Sprite _icon = null)
        {
            StartCoroutine(DisplayCustomNotificationLoop(_name, _text, _time, _base, _top, _icCol, _title, _icon));
        }

        public void RebuildNotificationLayout()
        {
            try
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.Find("info/text").AsRT());
                LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.Find("info").AsRT());
                LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.AsRT());
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"There was some sort of error with rebuilding the layout. {ex}");
            }
        }

        public IEnumerator DisplayNotificationLoop(string name, string text, float time, EditorManager.NotificationType type)
        {
            var config = EditorConfig.Instance;

            Debug.Log("<color=#F6AC1A>Editor</color><color=#2FCBD6>Management</color>\nNotification: " + name + "\nText: " + text + "\nTime: " + time + "\nType: " + type);

            if (!notifications.Contains(name) && notifications.Count < 20 && config.NotificationsDisplay.Value)
            {
                var notif = Instantiate(EditorManager.inst.notificationPrefabs[(int)type], Vector3.zero, Quaternion.identity);
                Destroy(notif, time * EditorConfig.Instance.NotificationDisplayTime.Value);

                Graphic textComponent = type == EditorManager.NotificationType.Info ? notif.transform.Find("text").GetComponent<TextMeshProUGUI>() : notif.transform.Find("text").GetComponent<Text>();

                if (type == EditorManager.NotificationType.Info)
                    ((TextMeshProUGUI)textComponent).text = text;
                else
                    ((Text)textComponent).text = text;

                notif.transform.SetParent(EditorManager.inst.notification.transform);
                if (config.NotificationDirection.Value == VerticalDirection.Down)
                    notif.transform.SetAsFirstSibling();
                notif.transform.localScale = Vector3.one;

                EditorThemeManager.ApplyGraphic(notif.GetComponent<Image>(), ThemeGroup.Notification_Background, true);
                EditorThemeManager.ApplyGraphic(notif.transform.Find("bg/bg").GetComponent<Image>(), EditorThemeManager.EditorTheme.GetGroup($"Notification {type}"), true, roundedSide: SpriteHelper.RoundedSide.Top);
                EditorThemeManager.ApplyGraphic(textComponent, ThemeGroup.Light_Text);
                EditorThemeManager.ApplyGraphic(notif.transform.Find("bg/Image").GetComponent<Image>(), ThemeGroup.Light_Text);
                EditorThemeManager.ApplyLightText(notif.transform.Find("bg/title").GetComponent<Text>());

                RebuildNotificationLayout();

                notifications.Add(name);

                yield return new WaitForSeconds(time * EditorConfig.Instance.NotificationDisplayTime.Value);
                notifications.Remove(name);
            }
            yield break;
        }

        public IEnumerator DisplayCustomNotificationLoop(string name, string text, float time, Color baseColor, Color topColor, Color iconCOlor, string _title, Sprite _icon = null)
        {
            var config = EditorConfig.Instance;

            if (!notifications.Contains(name) && notifications.Count < 20 && config.NotificationsDisplay.Value)
            {
                notifications.Add(name);
                var gameObject = Instantiate(EditorManager.inst.notificationPrefabs[0], Vector3.zero, Quaternion.identity);
                Destroy(gameObject, time * EditorConfig.Instance.NotificationDisplayTime.Value);
                gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>().text = text;
                gameObject.transform.SetParent(EditorManager.inst.notification.transform);
                if (config.NotificationDirection.Value == VerticalDirection.Down)
                    gameObject.transform.SetAsFirstSibling();
                gameObject.transform.localScale = Vector3.one;

                gameObject.GetComponent<Image>().color = baseColor;
                var bg = gameObject.transform.Find("bg");
                var img = bg.Find("Image").GetComponent<Image>();
                bg.Find("bg").GetComponent<Image>().color = topColor;
                if (_icon != null)
                    img.sprite = _icon;

                img.color = iconCOlor;
                bg.Find("title").GetComponent<Text>().text = _title;

                LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.Find("info/text").AsRT());
                LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.Find("info").AsRT());
                LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.AsRT());

                yield return new WaitForSeconds(time * EditorConfig.Instance.NotificationDisplayTime.Value);
                notifications.Remove(name);
            }

            yield break;
        }

        void SetupNotificationValues()
        {
            var config = EditorConfig.Instance;

            var notifyRT = EditorManager.inst.notification.GetComponent<RectTransform>();
            var notifyGroup = EditorManager.inst.notification.GetComponent<VerticalLayoutGroup>();
            notifyRT.sizeDelta = new Vector2(config.NotificationWidth.Value, 632f);
            EditorManager.inst.notification.transform.localScale =
                new Vector3(config.NotificationSize.Value, config.NotificationSize.Value, 1f);

            if (config.NotificationDirection.Value == VerticalDirection.Down)
            {
                notifyRT.anchoredPosition = new Vector2(8f, 408f);
                notifyGroup.childAlignment = TextAnchor.LowerLeft;
            }

            if (config.NotificationDirection.Value == VerticalDirection.Up)
            {
                notifyRT.anchoredPosition = new Vector2(8f, 410f);
                notifyGroup.childAlignment = TextAnchor.UpperLeft;
            }

            tooltipText = EditorManager.inst.tooltip.GetComponent<TextMeshProUGUI>();
            var tooltip = EditorManager.inst.tooltip.transform.parent.gameObject;
            EditorThemeManager.AddGraphic(tooltip.GetComponent<Image>(), ThemeGroup.Notification_Background, true);
            EditorThemeManager.AddGraphic(tooltip.transform.Find("bg/bg").GetComponent<Image>(), ThemeGroup.Notification_Info, true, roundedSide: SpriteHelper.RoundedSide.Top);
            EditorThemeManager.AddLightText(tooltipText);
            EditorThemeManager.AddGraphic(tooltip.transform.Find("bg/Image").GetComponent<Image>(), ThemeGroup.Light_Text);
            EditorThemeManager.AddLightText(tooltip.transform.Find("bg/title").GetComponent<Text>());
        }

        #endregion

        #region Timeline

        Image timelineImage;
        public Image TimelineImage
        {
            get
            {
                if (!timelineImage)
                    timelineImage = EditorManager.inst.timeline.GetComponent<Image>();
                return timelineImage;
            }
        }

        Image timelineOverlayImage;
        public Image TimelineOverlayImage
        {
            get
            {
                if (!timelineOverlayImage)
                    timelineOverlayImage = EditorManager.inst.timelineWaveformOverlay.GetComponent<Image>();
                return timelineOverlayImage;
            }
        }

        /// <summary>
        /// Sets the main timeline zoom and position.
        /// </summary>
        /// <param name="zoom">The amount to zoom in.</param>
        /// <param name="position">The position to set the timeline scroll. If the value is less that 0, it will automatically calculate the position to match the audio time.</param>
        /// <param name="render">If the timeline should render.</param>
        /// <param name="log">If the zoom amount should be logged.</param>
        public void SetTimeline(float zoom, float position = -1f, bool render = true, bool log = true)
        {
            try
            {
                var timelineTime = GetTimelineTime();

                float prevZoom = EditorManager.inst.zoomFloat;
                EditorManager.inst.zoomFloat = Mathf.Clamp01(zoom);
                EditorManager.inst.zoomVal =
                    LSMath.InterpolateOverCurve(EditorManager.inst.ZoomCurve, EditorManager.inst.zoomBounds.x, EditorManager.inst.zoomBounds.y, EditorManager.inst.zoomFloat);

                if (render)
                    EditorManager.inst.RenderTimeline();

                CoreHelper.StartCoroutine(SetTimelinePosition(timelineTime, position));

                EditorManager.inst.zoomSlider.onValueChanged.ClearAll();
                EditorManager.inst.zoomSlider.value = EditorManager.inst.zoomFloat;
                EditorManager.inst.zoomSlider.onValueChanged.AddListener(_val => { EditorManager.inst.Zoom = _val; });

                if (log)
                    CoreHelper.Log($"SET MAIN ZOOM\n" +
                        $"ZoomFloat: {EditorManager.inst.zoomFloat}\n" +
                        $"ZoomVal: {EditorManager.inst.zoomVal}\n" +
                        $"ZoomBounds: {EditorManager.inst.zoomBounds}\n" +
                        $"Timeline Position: {EditorManager.inst.timelineScrollRectBar.value}\n" +
                        $"Timeline Time: {timelineTime}\n" +
                        $"Timeline Position Calculation: {(AudioManager.inst.CurrentAudioSource.clip == null ? -1f : position >= 0f ? position : (EditorConfig.Instance.UseMouseAsZoomPoint.Value ? timelineTime : AudioManager.inst.CurrentAudioSource.time) / AudioManager.inst.CurrentAudioSource.clip.length)}");
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Had an error with setting zoom. Exception: {ex}");
            }
        }

        // i have no idea why the timeline scrollbar doesn't like to be set in the frame the zoom is also set in.
        IEnumerator SetTimelinePosition(float timelineTime, float position = 0f)
        {
            yield return new WaitForFixedUpdate();
            var pos = position >= 0f ? position : AudioManager.inst.CurrentAudioSource.clip == null ? 0f : (EditorConfig.Instance.UseMouseAsZoomPoint.Value ? timelineTime : AudioManager.inst.CurrentAudioSource.time) / AudioManager.inst.CurrentAudioSource.clip.length;
            EditorManager.inst.timelineScrollRectBar.value = pos;
            CoreHelper.Log($"Pos: {pos} - Scrollbar: {EditorManager.inst.timelineScrollRectBar.value}");
        }

        public float GetTimelineTime(float _offset = 0f)
        {
            float num = Input.mousePosition.x;
            num += Mathf.Abs(EditorManager.inst.timeline.transform.AsRT().position.x);
            if (SettingEditor.inst.SnapActive && !Input.GetKey(KeyCode.LeftAlt))
                return SnapToBPM(num * EditorManager.inst.ScreenScaleInverse / EditorManager.inst.Zoom);
            return num * EditorManager.inst.ScreenScaleInverse / EditorManager.inst.Zoom + _offset;
        }

        #endregion

        #region Timeline Objects
        
        public List<TimelineObject> timelineObjects = new List<TimelineObject>();
        public List<TimelineObject> timelineKeyframes = new List<TimelineObject>();

        public List<TimelineObject> TimelineBeatmapObjects => timelineObjects.Where(x => x.isBeatmapObject).ToList();
        public List<TimelineObject> TimelinePrefabObjects => timelineObjects.Where(x => x.isPrefabObject).ToList();

        public void RemoveTimelineObject(TimelineObject timelineObject)
        {
            if (timelineObjects.TryFindIndex(x => x.ID == timelineObject.ID, out int a))
            {
                timelineObject.Selected = false;
                Destroy(timelineObject.GameObject);
                timelineObjects.RemoveAt(a);
            }
        }

        public static Sprite GetKeyframeIcon(DataManager.LSAnimation a, DataManager.LSAnimation b)
            => ObjEditor.inst.KeyframeSprites[a.Name.Contains("Out") && b.Name.Contains("In") ? 3 : a.Name.Contains("Out") ? 2 : b.Name.Contains("In") ? 1 : 0];

        void UpdateTimelineObjects()
        {
            for (int i = 0; i < timelineObjects.Count; i++)
                timelineObjects[i].UpdateVisibleState();

            if (ObjectEditor.inst && ObjectEditor.inst.CurrentSelection && ObjectEditor.inst.CurrentSelection.isBeatmapObject && ObjectEditor.inst.CurrentSelection.InternalTimelineObjects.Count > 0)
                for (int i = 0; i < ObjectEditor.inst.CurrentSelection.InternalTimelineObjects.Count; i++)
                    ObjectEditor.inst.CurrentSelection.InternalTimelineObjects[i].UpdateVisibleState();

            for (int i = 0; i < timelineKeyframes.Count; i++)
                timelineKeyframes[i].UpdateVisibleState();
        }

        #endregion

        #region Timeline Textures

        public IEnumerator AssignTimelineTexture()
        {
            var config = EditorConfig.Instance;
            var path = RTFile.CombinePaths(RTFile.BasePath, $"waveform-{config.WaveformMode.Value.ToString().ToLower()}{FileFormat.PNG.Dot()}");
            var settingsPath = RTFile.CombinePaths(RTFile.ApplicationDirectory, $"settings/waveform-{config.WaveformMode.Value.ToString().ToLower()}{FileFormat.PNG.Dot()}");

            SetTimelineSprite(null);

            if ((!EditorManager.inst.hasLoadedLevel && !EditorManager.inst.loading && !RTFile.FileExists(settingsPath) ||
                !RTFile.FileExists(path)) && !config.WaveformRerender.Value || config.WaveformRerender.Value)
            {
                int num = Mathf.Clamp((int)AudioManager.inst.CurrentAudioSource.clip.length * 48, 100, 15000);
                Texture2D waveform = null;

                switch (config.WaveformMode.Value)
                {
                    case WaveformType.Legacy:
                        {
                            yield return CoreHelper.StartCoroutineAsync(Legacy(AudioManager.inst.CurrentAudioSource.clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, config.WaveformBottomColor.Value, (Texture2D _tex) => { waveform = _tex; }));
                            break;
                        }
                    case WaveformType.Beta:
                        {
                            yield return CoreHelper.StartCoroutineAsync(Beta(AudioManager.inst.CurrentAudioSource.clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, (Texture2D _tex) => { waveform = _tex; }));
                            break;
                        }
                    case WaveformType.Modern:
                        {
                            yield return CoreHelper.StartCoroutineAsync(Modern(AudioManager.inst.CurrentAudioSource.clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, (Texture2D _tex) => { waveform = _tex; }));
                            break;
                        }
                    case WaveformType.LegacyFast:
                        {
                            yield return CoreHelper.StartCoroutineAsync(LegacyFast(AudioManager.inst.CurrentAudioSource.clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, config.WaveformBottomColor.Value, (Texture2D _tex) => { waveform = _tex; }));
                            break;
                        }
                    case WaveformType.BetaFast:
                        {
                            yield return CoreHelper.StartCoroutineAsync(BetaFast(AudioManager.inst.CurrentAudioSource.clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, (Texture2D _tex) => { waveform = _tex; }));
                            break;
                        }
                    case WaveformType.ModernFast:
                        {
                            yield return CoreHelper.StartCoroutineAsync(ModernFast(AudioManager.inst.CurrentAudioSource.clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, (Texture2D _tex) => { waveform = _tex; }));
                            break;
                        }
                }

                var waveSprite = Sprite.Create(waveform, new Rect(0f, 0f, num, 300f), new Vector2(0.5f, 0.5f), 100f);
                SetTimelineSprite(waveSprite);

                if (config.WaveformSaves.Value)
                    CoreHelper.StartCoroutineAsync(SaveWaveform(config));
            }
            else
            {
                CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadImageTexture("file://" + (!EditorManager.inst.hasLoadedLevel && !EditorManager.inst.loading ?
                settingsPath :
                path), texture2D => SetTimelineSprite(SpriteHelper.CreateSprite(texture2D))));
            }

            SetTimelineGridSize();

            yield break;
        }

        public IEnumerator SaveWaveform(EditorConfig config)
        {
            var path = !EditorManager.inst.hasLoadedLevel && !EditorManager.inst.loading ?
                    RTFile.CombinePaths(RTFile.ApplicationDirectory, $"settings/waveform-{config.WaveformMode.Value.ToString().ToLower()}{FileFormat.PNG.Dot()}") :
                    RTFile.CombinePaths(RTFile.BasePath, $"waveform-{config.WaveformMode.Value.ToString().ToLower()}{FileFormat.PNG.Dot()}");
            var bytes = TimelineImage.sprite.texture.EncodeToPNG();

            File.WriteAllBytes(path, bytes);

            yield break;
        }

        public void SetTimelineSprite(Sprite sprite)
        {
            TimelineImage.sprite = sprite;
            TimelineOverlayImage.sprite = TimelineImage.sprite;
        }

        public IEnumerator Beta(AudioClip clip, int textureWidth, int textureHeight, Color background, Color waveform, Action<Texture2D> action)
        {
            yield return Ninja.JumpToUnity;
            CoreHelper.Log("Generating Beta Waveform");
            int num = 100;
            var texture2D = new Texture2D(textureWidth, textureHeight, EditorConfig.Instance.WaveformTextureFormat.Value, false);
            yield return Ninja.JumpBack;

            var array = new Color[texture2D.width * texture2D.height];
            for (int i = 0; i < array.Length; i++)
                array[i] = background;

            texture2D.SetPixels(array);
            num = clip.frequency / num;
            float[] array2 = new float[clip.samples * clip.channels];
            clip.GetData(array2, 0);
            float[] array3 = new float[array2.Length / num];
            for (int j = 0; j < array3.Length; j++)
            {
                array3[j] = 0f;
                for (int k = 0; k < num; k++)
                    array3[j] += Mathf.Abs(array2[j * num + k]);
                array3[j] /= num;
            }
            for (int l = 0; l < array3.Length - 1; l++)
            {
                int num2 = 0;
                while (num2 < textureHeight * array3[l] + 1f)
                {
                    texture2D.SetPixel(textureWidth * l / array3.Length, (int)(textureHeight * (array3[l] + 1f) / 2f) - num2, waveform);
                    num2++;
                }
            }
            yield return Ninja.JumpToUnity;
            texture2D.wrapMode = TextureWrapMode.Clamp;
            texture2D.filterMode = FilterMode.Point;
            texture2D.Apply();
            action(texture2D);
            yield break;
        }

        public IEnumerator Legacy(AudioClip clip, int textureWidth, int textureHeight, Color background, Color _top, Color _bottom, Action<Texture2D> action)
        {
            yield return Ninja.JumpToUnity;

            CoreHelper.Log("Generating Legacy Waveform");
            int num = 160;
            num = clip.frequency / num;
            var texture2D = new Texture2D(textureWidth, textureHeight, EditorConfig.Instance.WaveformTextureFormat.Value, false);

            yield return Ninja.JumpBack;
            Color[] array = new Color[texture2D.width * texture2D.height];
            for (int i = 0; i < array.Length; i++)
                array[i] = background;

            texture2D.SetPixels(array);
            float[] array3 = new float[clip.samples];
            float[] array4 = new float[clip.samples];
            float[] array5 = new float[clip.samples * clip.channels];
            clip.GetData(array5, 0);
            if (clip.channels > 1)
            {
                array3 = array5.Where((float value, int index) => index % 2 != 0).ToArray();
                array4 = array5.Where((float value, int index) => index % 2 == 0).ToArray();
            }
            else
            {
                array3 = array5;
                array4 = array5;
            }
            float[] array6 = new float[array3.Length / num];
            for (int j = 0; j < array6.Length; j++)
            {
                array6[j] = 0f;
                for (int k = 0; k < num; k++)
                {
                    array6[j] += Mathf.Abs(array3[j * num + k]);
                }
                array6[j] /= num;
                array6[j] *= 0.85f;
            }
            for (int l = 0; l < array6.Length - 1; l++)
            {
                int num2 = 0;
                while (num2 < textureHeight * array6[l])
                {
                    texture2D.SetPixel(textureWidth * l / array6.Length, (int)(textureHeight * array6[l]) - num2, _top);
                    num2++;
                }
            }
            array6 = new float[array4.Length / num];
            for (int m = 0; m < array6.Length; m++)
            {
                array6[m] = 0f;
                for (int n = 0; n < num; n++)
                {
                    array6[m] += Mathf.Abs(array4[m * num + n]);
                }
                array6[m] /= num;
                array6[m] *= 0.85f;
            }
            for (int num3 = 0; num3 < array6.Length - 1; num3++)
            {
                int num4 = 0;
                while (num4 < textureHeight * array6[num3])
                {
                    int x = textureWidth * num3 / array6.Length;
                    int y = (int)array4[num3 * num + num4] - num4;
                    texture2D.SetPixel(x, y, texture2D.GetPixel(x, y) == _top ? CoreHelper.MixColors(new List<Color> { _top, _bottom }) : _bottom);
                    num4++;
                }
            }
            yield return Ninja.JumpToUnity;
            texture2D.wrapMode = TextureWrapMode.Clamp;
            texture2D.filterMode = FilterMode.Point;
            texture2D.Apply();
            action?.Invoke(texture2D);
            yield break;
        }

        public IEnumerator Modern(AudioClip clip, int textureWidth, int textureHeight, Color background, Color waveform, Action<Texture2D> action)
        {
            yield return Ninja.JumpToUnity;
            CoreHelper.Log("Generating Modern Waveform");
            int num = 100;
            var texture2D = new Texture2D(textureWidth, textureHeight, EditorConfig.Instance.WaveformTextureFormat.Value, false);
            yield return Ninja.JumpBack;

            var array = new Color[texture2D.width * texture2D.height];
            for (int i = 0; i < array.Length; i++)
                array[i] = background;

            texture2D.SetPixels(array);
            num = clip.frequency / num;
            float[] array2 = new float[clip.samples * clip.channels];
            clip.GetData(array2, 0);
            float[] array3 = new float[array2.Length / num];
            for (int j = 0; j < array3.Length; j++)
            {
                array3[j] = 0f;
                for (int k = 0; k < num; k++)
                    array3[j] += Mathf.Abs(array2[j * num + k]);
                array3[j] /= (float)num;
            }
            for (int l = 0; l < array3.Length - 1; l++)
            {
                int num2 = 0;
                while (num2 < textureHeight * array3[l] + 1f)
                {
                    texture2D.SetPixel(textureWidth * l / array3.Length, (int)(textureHeight * (array3[l] + 1f)) - num2, waveform);
                    num2++;
                }
            }
            yield return Ninja.JumpToUnity;
            texture2D.wrapMode = TextureWrapMode.Clamp;
            texture2D.filterMode = FilterMode.Point;
            texture2D.Apply();
            action(texture2D);
            yield break;
        }

        public IEnumerator BetaFast(AudioClip audio, int width, int height, Color background, Color col, Action<Texture2D> action)
        {
            yield return Ninja.JumpToUnity;
            CoreHelper.Log("Generating Beta Waveform (Fast)");
            var tex = new Texture2D(width, height, EditorConfig.Instance.WaveformTextureFormat.Value, false);
            yield return Ninja.JumpBack;

            float[] samples = new float[audio.samples * audio.channels];
            float[] waveform = new float[width];
            audio.GetData(samples, 0);
            float packSize = ((float)samples.Length / (float)width);
            int s = 0;
            for (float i = 0; Mathf.RoundToInt(i) < samples.Length && s < waveform.Length; i += packSize)
            {
                waveform[s] = Mathf.Abs(samples[Mathf.RoundToInt(i)]);
                s++;
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tex.SetPixel(x, y, background);
                }
            }

            for (int x = 0; x < waveform.Length; x++)
            {
                for (int y = 0; y <= waveform[x] * ((float)height * .75f); y++)
                {
                    tex.SetPixel(x, (height / 2) + y, col);
                    tex.SetPixel(x, (height / 2) - y, col);
                }
            }
            yield return Ninja.JumpToUnity;
            tex.Apply();

            action?.Invoke(tex);
            yield break;
        }

        public IEnumerator LegacyFast(AudioClip audio, int width, int height, Color background, Color colTop, Color colBot, Action<Texture2D> action)
        {
            yield return Ninja.JumpToUnity;
            CoreHelper.Log("Generating Legacy Waveform (Fast)");
            var tex = new Texture2D(width, height, EditorConfig.Instance.WaveformTextureFormat.Value, false);
            yield return Ninja.JumpBack;

            float[] samples = new float[audio.samples * audio.channels];
            float[] waveform = new float[width];
            audio.GetData(samples, 0);
            float packSize = ((float)samples.Length / (float)width);
            int s = 0;
            for (float i = 0; Mathf.RoundToInt(i) < samples.Length && s < waveform.Length; i += packSize)
            {
                waveform[s] = Mathf.Abs(samples[Mathf.RoundToInt(i)]);
                s++;
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tex.SetPixel(x, y, background);
                }
            }

            for (int x = 0; x < waveform.Length; x++)
            {
                for (int y = 0; y <= waveform[x] * ((float)height * .75f); y++)
                {
                    tex.SetPixel(x, height - y, colTop);

                    tex.SetPixel(x, y, tex.GetPixel(x, y) == colTop ? CoreHelper.MixColors(new List<Color> { colTop, colBot }) : colBot);
                }
            }
            yield return Ninja.JumpToUnity;
            tex.Apply();

            action?.Invoke(tex);
            yield break;
        }

        public IEnumerator ModernFast(AudioClip audio, int width, int height, Color background, Color col, Action<Texture2D> action)
        {
            yield return Ninja.JumpToUnity;
            CoreHelper.Log("Generating Modern Waveform (Fast)");
            var tex = new Texture2D(width, height, EditorConfig.Instance.WaveformTextureFormat.Value, false);
            yield return Ninja.JumpBack;

            float[] samples = new float[audio.samples * audio.channels];
            float[] waveform = new float[width];
            audio.GetData(samples, 0);
            float packSize = ((float)samples.Length / (float)width);
            int s = 0;
            for (float i = 0; Mathf.RoundToInt(i) < samples.Length && s < waveform.Length; i += packSize)
            {
                waveform[s] = Mathf.Abs(samples[Mathf.RoundToInt(i)]);
                s++;
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tex.SetPixel(x, y, background);
                }
            }

            for (int x = 0; x < waveform.Length; x++)
            {
                for (int y = 0; y <= waveform[x] * ((float)height * .75f); y++)
                {
                    tex.SetPixel(x, y, col);
                    //tex.SetPixel(x, (height / 2) - y, col);
                }
            }
            yield return Ninja.JumpToUnity;
            tex.Apply();

            action?.Invoke(tex);
            yield break;
        }

        public float timelineGridRenderMultiSizeCloser = 40f;
        public float timelineGridRenderMultiSizeClose = 20f;
        public float timelineGridUnrenderSize = 6f;
        public void SetTimelineGridSize()
        {
            if (!AudioManager.inst || !AudioManager.inst.CurrentAudioSource || !AudioManager.inst.CurrentAudioSource.clip)
            {
                if (timelineGridRenderer)
                    timelineGridRenderer.enabled = false;
                return;
            }

            var clipLength = AudioManager.inst.CurrentAudioSource.clip.length;

            float x = SettingEditor.inst.SnapBPM / 60f;

            var closer = timelineGridRenderMultiSizeCloser * x;
            var close = timelineGridRenderMultiSizeClose * x;
            var unrender = timelineGridUnrenderSize * x;

            var bpm = EditorManager.inst.Zoom > closer ? SettingEditor.inst.SnapBPM : EditorManager.inst.Zoom > close ? SettingEditor.inst.SnapBPM / 2f : SettingEditor.inst.SnapBPM / 4f;
            var snapDivisions = EditorConfig.Instance.BPMSnapDivisions.Value * 2f;
            if (timelineGridRenderer && EditorManager.inst.Zoom > unrender && EditorConfig.Instance.TimelineGridEnabled.Value)
            {
                timelineGridRenderer.enabled = false;
                timelineGridRenderer.gridCellSize.x = ((int)bpm / (int)snapDivisions) * (int)clipLength;
                timelineGridRenderer.gridSize.x = clipLength * bpm / (snapDivisions * 1.875f);
                timelineGridRenderer.enabled = true;
            }
            else if (timelineGridRenderer)
                timelineGridRenderer.enabled = false;
        }

        #endregion

        #region Paths

        public static string EditorSettingsPath => $"{RTFile.ApplicationDirectory}settings/editor.lss";

        public static string EditorPath
        {
            get => editorPath;
            set
            {
                editorPath = value;
                // Makes the editor path always in the beatmaps folder.
                editorListPath = $"beatmaps/{editorPath}";
                editorListSlash = $"beatmaps/{editorPath}/";
            }
        }
        static string editorPath = "editor";
        public static string editorListPath = "beatmaps/editor";
        public static string editorListSlash = "beatmaps/editor/";

        public void UpdateEditorPath(bool forceReload)
        {
            if (!forceReload && !EditorConfig.Instance.SettingPathReloads.Value || editorListPath[editorListPath.Length - 1] == '/')
                return;

            var editorPath = RTFile.CombinePaths(RTFile.ApplicationDirectory, editorListPath);
            if (!RTFile.DirectoryExists(editorPath))
            {
                editorPathField.interactable = false;
                ShowWarningPopup("No directory exists for this path. Do you want to create a new folder?", () =>
                {
                    RTFile.CreateDirectory(editorPath);

                    SaveGlobalSettings();

                    EditorManager.inst.GetLevelList();

                    HideWarningPopup();
                    editorPathField.interactable = true;
                }, () =>
                {
                    HideWarningPopup();
                    editorPathField.interactable = true;
                });

                return;
            }

            SaveGlobalSettings();

            EditorManager.inst.GetLevelList();
        }

        public static string ThemePath
        {
            get => themePath;
            set
            {
                themePath = value;
                // Makes the themes path always in the beatmaps folder.
                themeListPath = $"beatmaps/{themePath}";
                themeListSlash = $"beatmaps/{themePath}/";
            }
        }
        static string themePath = "themes";
        public static string themeListPath = "beatmaps/themes";
        public static string themeListSlash = "beatmaps/themes/";

        public const string DEFAULT_EXPORTS_PATH = "beatmaps/exports";

        public void UpdateThemePath(bool forceReload)
        {
            if (!forceReload && !EditorConfig.Instance.SettingPathReloads.Value || themeListPath[themeListPath.Length - 1] == '/')
                return;

            var themePath = RTFile.CombinePaths(RTFile.ApplicationDirectory, themeListPath);
            if (!RTFile.DirectoryExists(themePath))
            {
                themePathField.interactable = false;
                ShowWarningPopup("No directory exists for this path. Do you want to create a new folder?", () =>
                {
                    RTFile.CreateDirectory(themePath);

                    SaveGlobalSettings();

                    StartCoroutine(LoadThemes(true));
                    EventEditor.inst.RenderEventsDialog();

                    HideWarningPopup();
                    themePathField.interactable = true;
                }, () =>
                {
                    HideWarningPopup();
                    themePathField.interactable = true;
                });

                return;
            }

            SaveGlobalSettings();

            StartCoroutine(LoadThemes(true));
            EventEditor.inst.RenderEventsDialog();
        }

        public static string PrefabPath
        {
            get => prefabPath;
            set
            {
                prefabPath = value;
                // Makes the prefabs path always in the beatmaps folder.
                prefabListPath = $"beatmaps/{prefabPath}";
                prefabListSlash = $"beatmaps/{prefabPath}/";
            }
        }
        static string prefabPath = "prefabs";
        public static string prefabListPath = "beatmaps/prefabs";
        public static string prefabListSlash = "beatmaps/prefabs/";

        public void UpdatePrefabPath(bool forceReload)
        {
            if (!forceReload && !EditorConfig.Instance.SettingPathReloads.Value || prefabListPath[prefabListPath.Length - 1] == '/')
                return;

            var prefabPath = RTFile.CombinePaths(RTFile.ApplicationDirectory, prefabListPath);
            if (!RTFile.DirectoryExists(prefabPath))
            {
                prefabPathField.interactable = false;
                ShowWarningPopup("No directory exists for this path. Do you want to create a new folder?", () =>
                {
                    RTFile.CreateDirectory(prefabPath);

                    SaveGlobalSettings();

                    StartCoroutine(UpdatePrefabs());

                    HideWarningPopup();
                    prefabPathField.interactable = true;
                }, () =>
                {
                    HideWarningPopup();
                    prefabPathField.interactable = true;
                });

                return;
            }

            SaveGlobalSettings();

            StartCoroutine(UpdatePrefabs());
        }

        public void UpdateOrderDropdown()
        {
            if (!levelOrderDropdown)
                return;

            levelOrderDropdown.onValueChanged.ClearAll();
            levelOrderDropdown.value = levelFilter;
            levelOrderDropdown.onValueChanged.AddListener(_val =>
            {
                levelFilter = _val;
                StartCoroutine(RefreshLevelList());
                SaveGlobalSettings();
            });
        }

        public void UpdateAscendToggle()
        {
            if (!levelAscendToggle)
                return;

            levelAscendToggle.onValueChanged.ClearAll();
            levelAscendToggle.isOn = levelAscend;
            levelAscendToggle.onValueChanged.AddListener(_val =>
            {
                levelAscend = _val;
                StartCoroutine(RefreshLevelList());
                SaveGlobalSettings();
            });
        }

        public void CreateGlobalSettings()
        {
            if (RTFile.FileExists(EditorSettingsPath))
                return;

            var jn = JSON.Parse("{}");

            jn["sort"]["asc"] = "True";
            jn["sort"]["order"] = "0";

            EditorPath = "editor";
            jn["paths"]["editor"] = EditorPath;

            ThemePath = "themes";
            jn["paths"]["themes"] = ThemePath;

            PrefabPath = "prefabs";
            jn["paths"]["prefabs"] = PrefabPath;

            for (int i = 0; i < MarkerEditor.inst.markerColors.Count; i++)
                jn["marker_colors"][i] = LSColors.ColorToHex(MarkerEditor.inst.markerColors[i]);

            EditorManager.inst.layerColors.RemoveAt(5);
            for (int i = 0; i < EditorManager.inst.layerColors.Count; i++)
                jn["layer_colors"][i] = LSColors.ColorToHex(EditorManager.inst.layerColors[i]);

            RTFile.WriteToFile(EditorSettingsPath, jn.ToString(3));
        }

        public void LoadGlobalSettings()
        {
            if (!RTFile.FileExists(EditorSettingsPath))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(EditorSettingsPath));

            if (!string.IsNullOrEmpty(jn["sort"]["asc"]))
                levelAscend = jn["sort"]["asc"].AsBool;
            if (!string.IsNullOrEmpty(jn["sort"]["order"]))
                levelFilter = jn["sort"]["order"].AsInt;

            UpdateOrderDropdown();
            UpdateAscendToggle();

            if (!string.IsNullOrEmpty(jn["paths"]["editor"]))
                EditorPath = jn["paths"]["editor"];
            if (!string.IsNullOrEmpty(jn["paths"]["themes"]))
                ThemePath = jn["paths"]["themes"];
            if (!string.IsNullOrEmpty(jn["paths"]["prefabs"]))
                PrefabPath = jn["paths"]["prefabs"];

            RTFile.CreateDirectory(RTFile.CombinePaths(RTFile.ApplicationDirectory, editorListPath));
            RTFile.CreateDirectory(RTFile.CombinePaths(RTFile.ApplicationDirectory, themeListPath));
            RTFile.CreateDirectory(RTFile.CombinePaths(RTFile.ApplicationDirectory, prefabListPath));

            SetWatcherPaths();

            if (jn["marker_colors"] != null)
            {
                MarkerEditor.inst.markerColors.Clear();
                for (int i = 0; i < jn["marker_colors"].Count; i++)
                    MarkerEditor.inst.markerColors.Add(LSColors.HexToColor(jn["marker_colors"][i]));
            }
            else
            {
                for (int i = 0; i < MarkerEditor.inst.markerColors.Count; i++)
                    jn["marker_colors"][i] = LSColors.ColorToHex(MarkerEditor.inst.markerColors[i]);

                RTFile.WriteToFile(EditorSettingsPath, jn.ToString(3));
            }

            if (jn["layer_colors"] != null)
            {
                EditorManager.inst.layerColors.Clear();
                for (int i = 0; i < jn["layer_colors"].Count; i++)
                    EditorManager.inst.layerColors.Add(LSColors.HexToColor(jn["layer_colors"][i]));
            }
            else
            {
                for (int i = 0; i < EditorManager.inst.layerColors.Count; i++)
                    jn["layer_colors"][i] = LSColors.ColorToHex(EditorManager.inst.layerColors[i]);

                RTFile.WriteToFile(EditorSettingsPath, jn.ToString(3));
            }
        }

        public void SaveGlobalSettings()
        {
            var jn = JSON.Parse("{}");

            jn["sort"]["asc"] = levelAscend.ToString();
            jn["sort"]["order"] = levelFilter.ToString();

            jn["paths"]["editor"] = EditorPath;
            jn["paths"]["themes"] = ThemePath;
            jn["paths"]["prefabs"] = PrefabPath;

            SetWatcherPaths();

            for (int i = 0; i < MarkerEditor.inst.markerColors.Count; i++)
                jn["marker_colors"][i] = LSColors.ColorToHex(MarkerEditor.inst.markerColors[i]);

            for (int i = 0; i < EditorManager.inst.layerColors.Count; i++)
                jn["layer_colors"][i] = LSColors.ColorToHex(EditorManager.inst.layerColors[i]);

            RTFile.WriteToFile(EditorSettingsPath, jn.ToString(3));
        }

        public void DisablePrefabWatcher()
        {
            canUpdatePrefabs = false;
            PrefabWatcher.EnableRaisingEvents = false;
            try
            {
                PrefabWatcher.Changed -= OnPrefabPathChanged;
                PrefabWatcher.Created -= OnPrefabPathChanged;
                PrefabWatcher.Deleted -= OnPrefabPathChanged;
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Couldn't remove OnPrefabPathChanged method.\nException: {ex}");
            }
        }

        public void EnablePrefabWatcher()
        {
            try
            {
                PrefabWatcher.Changed += OnPrefabPathChanged;
                PrefabWatcher.Created += OnPrefabPathChanged;
                PrefabWatcher.Deleted += OnPrefabPathChanged;
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Couldn't add OnPrefabPathChanged method.\nException: {ex}");
            }
            PrefabWatcher.EnableRaisingEvents = true;
        }
        
        public void DisableThemeWatcher()
        {
            canUpdateThemes = false;
            ThemeWatcher.EnableRaisingEvents = false;
            try
            {
                ThemeWatcher.Changed -= OnThemePathChanged;
                ThemeWatcher.Created -= OnThemePathChanged;
                ThemeWatcher.Deleted -= OnThemePathChanged;
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Couldn't remove OnThemePathChanged method.\nException: {ex}");
            }
        }

        public void EnableThemeWatcher()
        {
            try
            {
                ThemeWatcher.Changed += OnThemePathChanged;
                ThemeWatcher.Created += OnThemePathChanged;
                ThemeWatcher.Deleted += OnThemePathChanged;
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Couldn't add OnThemePathChanged method.\nException: {ex}");
            }
            ThemeWatcher.EnableRaisingEvents = true;
        }

        public void SetWatcherPaths()
        {
            DisablePrefabWatcher();
            if (RTFile.DirectoryExists(RTFile.ApplicationDirectory + prefabListPath))
            {
                PrefabWatcher.Path = RTFile.ApplicationDirectory + prefabListPath;
                EnablePrefabWatcher();
            }
            DisableThemeWatcher();
            if (RTFile.DirectoryExists(RTFile.ApplicationDirectory + themeListPath))
            {
                ThemeWatcher.Path = RTFile.ApplicationDirectory + themeListPath;
                EnableThemeWatcher();
            }
        }

        void OnPrefabPathChanged(object sender, FileSystemEventArgs e)
        {
            if (canUpdatePrefabs && EditorConfig.Instance.UpdatePrefabListOnFilesChanged.Value)
                CoreHelper.StartCoroutineAsync(UpdatePrefabPath());
            canUpdatePrefabs = true;
        }

        void OnThemePathChanged(object sender, FileSystemEventArgs e)
        {
            if (canUpdateThemes && EditorConfig.Instance.UpdateThemeListOnFilesChanged.Value)
                StartCoroutine(UpdateThemePath());
            canUpdateThemes = true;
        }

        IEnumerator UpdateThemePath()
        {
            yield return Ninja.JumpToUnity;
            CoreHelper.Log($"------- [UPDATING THEME FILEWATCHER] -------");
            StartCoroutine(LoadThemes(EventEditor.inst.dialogRight.GetChild(4).gameObject.activeInHierarchy));
            yield break;
        }

        IEnumerator UpdatePrefabPath()
        {
            yield return Ninja.JumpToUnity;
            CoreHelper.Log($"------- [UPDATING PREFAB FILEWATCHER] -------");
            StartCoroutine(UpdatePrefabs());
            yield break;
        }

        public FileSystemWatcher PrefabWatcher { get; set; }
        public FileSystemWatcher ThemeWatcher { get; set; }

        #endregion

        #region Objects

        public void Duplicate(bool _regen = true) => Copy(false, true, _regen);

        public void Copy(bool _cut = false, bool _dup = false, bool _regen = true)
        {
            if ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Background)) || EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Background))
            {
                BackgroundEditor.inst.CopyBackground();
                if (!_cut)
                {
                    EditorManager.inst.DisplayNotification("Copied Background Object", 1f, EditorManager.NotificationType.Success, false);
                }
                else
                {
                    BackgroundEditor.inst.DeleteBackground(BackgroundEditor.inst.currentObj);
                    EditorManager.inst.DisplayNotification("Cut Background Object", 1f, EditorManager.NotificationType.Success, false);
                }
                if (_dup)
                {
                    EditorManager.inst.Paste();
                }
            }

            if ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Checkpoint)) || EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Checkpoint))
            {
                if (!_dup)
                {
                    CheckpointEditor.inst.CopyCheckpoint();
                    if (!_cut)
                    {
                        EditorManager.inst.DisplayNotification("Copied Checkpoint", 1f, EditorManager.NotificationType.Success, false);
                    }
                    else
                    {
                        BackgroundEditor.inst.DeleteBackground(BackgroundEditor.inst.currentObj);
                        EditorManager.inst.DisplayNotification("Cut Checkpoint", 1f, EditorManager.NotificationType.Success, false);
                    }
                }
                else
                {
                    EditorManager.inst.DisplayNotification("Can't duplicate Checkpoint", 1f, EditorManager.NotificationType.Error, false);
                }
            }

            if (!isOverMainTimeline && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Object))
            {
                if (!_dup)
                {
                    ObjEditor.inst.CopyAllSelectedEvents();
                    if (!_cut)
                    {
                        EditorManager.inst.DisplayNotification("Copied Object Keyframe", 1f, EditorManager.NotificationType.Success, false);
                    }
                    else
                    {
                        StartCoroutine(ObjectEditor.inst.DeleteKeyframes());
                        EditorManager.inst.DisplayNotification("Cut Object Keyframe", 1f, EditorManager.NotificationType.Success, false);
                    }
                }
                else
                {
                    EditorManager.inst.DisplayNotification("Can't duplicate Object Keyframe", 1f, EditorManager.NotificationType.Error, false);
                }
            }

            if (isOverMainTimeline && layerType == LayerType.Events)
            {
                if (!_dup)
                {
                    EventEditor.inst.CopyAllSelectedEvents();
                    if (!_cut)
                    {
                        EditorManager.inst.DisplayNotification("Copied Event Keyframe", 1f, EditorManager.NotificationType.Success, false);
                    }
                    else
                    {
                        StartCoroutine(RTEventEditor.inst.DeleteKeyframes());
                        EditorManager.inst.DisplayNotification("Cut Event Keyframe", 1f, EditorManager.NotificationType.Success, false);
                    }
                }
                else
                {
                    EditorManager.inst.DisplayNotification("Can't duplicate Event Keyframe", 1f, EditorManager.NotificationType.Error, false);
                }
            }

            if (isOverMainTimeline && layerType == LayerType.Objects)
            {
                var offsetTime = ObjectEditor.inst.SelectedObjects.Min(x => x.Time);

                ObjEditor.inst.CopyObject();
                if (!_cut)
                {
                    EditorManager.inst.DisplayNotification("Copied Beatmap Object", 1f, EditorManager.NotificationType.Success, false);
                }
                else
                {
                    StartCoroutine(ObjectEditor.inst.DeleteObjects());
                    EditorManager.inst.DisplayNotification("Cut Beatmap Object", 1f, EditorManager.NotificationType.Success, false);
                }
                if (_dup)
                {
                    Paste(offsetTime, _regen);
                }
            }
        }

        public void Paste(float _offsetTime = 0f, bool _regen = true)
        {
            if (isOverMainTimeline && layerType == LayerType.Objects)
            {
                ObjectEditor.inst.PasteObject(_offsetTime, _regen);
            }

            if (isOverMainTimeline && layerType == LayerType.Events)
            {
                if (EditorConfig.Instance.CopyPasteGlobal.Value && RTFile.FileExists($"{Application.persistentDataPath}/copied_events.lsev"))
                {
                    var jn = JSON.Parse(RTFile.ReadFromFile($"{Application.persistentDataPath}/copied_events.lsev"));

                    RTEventEditor.inst.copiedEventKeyframes.Clear();

                    for (int i = 0; i < GameData.EventTypes.Length; i++)
                    {
                        if (jn["events"][GameData.EventTypes[i]] != null)
                        {
                            for (int j = 0; j < jn["events"][GameData.EventTypes[i]].Count; j++)
                            {
                                var timelineObject = new TimelineObject(EventKeyframe.Parse(jn["events"][GameData.EventTypes[i]][j], i, GameData.DefaultKeyframes[i].eventValues.Length));
                                timelineObject.Type = i;
                                timelineObject.Index = j;
                                RTEventEditor.inst.copiedEventKeyframes.Add(timelineObject);
                            }
                        }
                    }
                }

                RTEventEditor.inst.PasteEvents();
                EditorManager.inst.DisplayNotification($"Pasted Event Keyframe{(RTEventEditor.inst.copiedEventKeyframes.Count > 1 ? "s" : "")}", 1f, EditorManager.NotificationType.Success);
            }

            if (!isOverMainTimeline && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Object))
            {
                ObjEditor.inst.PasteKeyframes();
                EditorManager.inst.DisplayNotification($"Pasted Object Keyframe{(ObjectEditor.inst.copiedObjectKeyframes.Count > 1 ? "s" : "")}", 1f, EditorManager.NotificationType.Success);
            }

            if ((isOverMainTimeline && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Checkpoint)) || EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Checkpoint))
            {
                CheckpointEditor.inst.PasteCheckpoint();
                EditorManager.inst.DisplayNotification("Pasted Checkpoint", 1f, EditorManager.NotificationType.Success);
            }

            if (EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Background))
            {
                BackgroundEditor.inst.PasteBackground();
                EditorManager.inst.DisplayNotification("Pasted Background Object", 1f, EditorManager.NotificationType.Success);
            }
        }

        public void Delete()
        {
            if (!isOverMainTimeline && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Object))
            {
                if (ObjectEditor.inst.CurrentSelection.isBeatmapObject)
                    if (ObjEditor.inst.currentKeyframe != 0)
                    {
                        var list = new List<TimelineObject>();
                        foreach (var timelineObject in ObjectEditor.inst.CurrentSelection.InternalTimelineObjects.Where(x => x.Selected))
                            list.Add(timelineObject);
                        var beatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();

                        EditorManager.inst.history.Add(new History.Command("Delete Keyframes", () =>
                        {
                            StartCoroutine(ObjectEditor.inst.DeleteKeyframes());
                        }, () =>
                        {
                            ObjectEditor.inst.PasteKeyframes(beatmapObject, list, false);
                        }));

                        StartCoroutine(ObjectEditor.inst.DeleteKeyframes());
                    }
                    else
                        EditorManager.inst.DisplayNotification("Can't delete first keyframe.", 1f, EditorManager.NotificationType.Error);
                return;
            }
            if (isOverMainTimeline && layerType == LayerType.Objects)
            {
                if (GameData.Current.beatmapObjects.Count > 1 && ObjectEditor.inst.SelectedObjectCount != GameData.Current.beatmapObjects.Count)
                {
                    var list = new List<TimelineObject>();
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        list.Add(timelineObject);

                    EditorManager.inst.ClearDialogs(EditorManager.EditorDialog.DialogType.Object, EditorManager.EditorDialog.DialogType.Prefab);

                    float startTime = 0f;

                    var startTimeList = new List<float>();
                    foreach (var bm in list)
                        startTimeList.Add(bm.Time);

                    startTimeList = (from x in startTimeList
                                     orderby x ascending
                                     select x).ToList();

                    startTime = startTimeList[0];

                    var prefab = new Prefab("deleted objects", 0, startTime,
                        list.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()).ToList(),
                        list.Where(x => x.isPrefabObject).Select(x => x.GetData<PrefabObject>()).ToList());

                    EditorManager.inst.history.Add(new History.Command("Delete Objects", () =>
                    {
                        Delete();
                    }, () =>
                    {
                        ObjectEditor.inst.DeselectAllObjects();
                        StartCoroutine(ObjectEditor.inst.AddPrefabExpandedToLevel(prefab, true, 0f, true, retainID: true));
                    }));

                    StartCoroutine(ObjectEditor.inst.DeleteObjects());
                }
                else
                    EditorManager.inst.DisplayNotification("Can't delete only Beatmap Object", 1f, EditorManager.NotificationType.Error);
                return;
            }
            if (isOverMainTimeline && layerType == LayerType.Events)
            {
                if (RTEventEditor.inst.SelectedKeyframes.Count > 0 && !RTEventEditor.inst.SelectedKeyframes.Has(x => x.Index == 0))
                {
                    EditorManager.inst.ClearDialogs(EditorManager.EditorDialog.DialogType.Event);

                    var list = new List<TimelineObject>();
                    foreach (var timelineObject in RTEventEditor.inst.SelectedKeyframes)
                        list.Add(timelineObject);

                    EditorManager.inst.history.Add(new History.Command("Delete Event Keyframes", () =>
                    {
                        StartCoroutine(RTEventEditor.inst.DeleteKeyframes(list));
                    }, () =>
                    {
                        RTEventEditor.inst.PasteEvents(list, false);
                    }));

                    StartCoroutine(RTEventEditor.inst.DeleteKeyframes());
                }
                else
                    EditorManager.inst.DisplayNotification("Can't delete first Event Keyframe.", 1f, EditorManager.NotificationType.Error);
                return;
            }

            if (EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Background))
            {
                BackgroundEditor.inst.DeleteBackground(BackgroundEditor.inst.currentObj);
                return;
            }

            if (EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Checkpoint))
            {
                if (CheckpointEditor.inst.currentObj != 0)
                {
                    CheckpointEditor.inst.DeleteCheckpoint(CheckpointEditor.inst.currentObj);
                    EditorManager.inst.DisplayNotification("Deleted Checkpoint.", 1f, EditorManager.NotificationType.Success);
                }
                else
                    EditorManager.inst.DisplayNotification("Can't delete first Checkpoint.", 1f, EditorManager.NotificationType.Error);
                return;
            }
        }

        #endregion

        #region Layers

        /// <summary>
        /// The current editor layer.
        /// </summary>
        public int Layer
        {
            get => Mathf.Clamp(EditorManager.inst.layer, 0, int.MaxValue);
            set => EditorManager.inst.layer = Mathf.Clamp(value, 0, int.MaxValue);
        }

        int prevLayer;
        LayerType prevLayerType;
        public LayerType layerType;

        public enum LayerType
        {
            Objects,
            Events
        }

        public static int GetLayer(int _layer) => Mathf.Clamp(_layer, 0, int.MaxValue);

        public static string GetLayerString(int _layer) => (_layer + 1).ToString();

        public static Color GetLayerColor(int _layer) => _layer < EditorManager.inst.layerColors.Count ? EditorManager.inst.layerColors[_layer] : Color.white;

        public void SetLayer(LayerType layerType) => SetLayer(0, layerType);

        public void SetLayer(int layer, bool setHistory = true) => SetLayer(layer, layerType, setHistory);

        /// <summary>
        /// Sets the current editor layer.
        /// </summary>
        /// <param name="layer">The layer to set.</param>
        /// <param name="setHistory">If the action should be undoable.</param>
        public void SetLayer(int layer, LayerType layerType, bool setHistory = true)
        {
            if (layer == 68)
                AchievementManager.inst.UnlockAchievement("editor_layer_lol");
            
            if (layer == 554)
                AchievementManager.inst.UnlockAchievement("editor_layer_funny");

            DataManager.inst.UpdateSettingInt("EditorLayer", layer);
            var oldLayer = Layer;
            var oldLayerType = this.layerType;

            Layer = layer;
            this.layerType = layerType;
            TimelineOverlayImage.color = GetLayerColor(layer);
            editorLayerImage.color = GetLayerColor(layer);

            editorLayerField.onValueChanged.RemoveAllListeners();
            editorLayerField.text = (layer + 1).ToString();
            editorLayerField.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                    SetLayer(Mathf.Clamp(num - 1, 0, int.MaxValue));
            });

            eventLayerToggle.onValueChanged.ClearAll();
            eventLayerToggle.isOn = layerType == LayerType.Events;
            eventLayerToggle.onValueChanged.AddListener(_val => { SetLayer(_val ? LayerType.Events : LayerType.Objects); });

            RTEventEditor.inst.SetEventActive(layerType == LayerType.Events);

            if (prevLayer != layer || prevLayerType != layerType)
            {
                UpdateTimelineObjects();
                switch (layerType)
                {
                    case LayerType.Objects:
                        {
                            ObjectEditor.inst.RenderTimelineObjectsPositions();

                            if (prevLayerType != layerType)
                                CheckpointEditor.inst.CreateGhostCheckpoints();

                            break;
                        }
                    case LayerType.Events:
                        {
                            RTEventEditor.inst.RenderEventObjects();
                            CheckpointEditor.inst.CreateCheckpoints();

                            RTEventEditor.inst.RenderLayerBins();

                            break;
                        }
                }
            }

            prevLayerType = layerType;
            prevLayer = layer;

            var tmpLayer = Layer;
            var tmpLayerType = this.layerType;
            if (setHistory)
            {
                EditorManager.inst.history.Add(new History.Command("Change Layer", () =>
                {
                    CoreHelper.Log($"Redone layer: {tmpLayer}");
                    SetLayer(tmpLayer, tmpLayerType, false);
                }, () =>
                {
                    CoreHelper.Log($"Undone layer: {oldLayer}");
                    SetLayer(oldLayer, oldLayerType, false);
                }), false);
            }
        }

        #endregion

        #region Generate UI

        public static GameObject GenerateSpacer(string name, Transform parent, Vector2 size)
        {
            var spacer = Creator.NewUIObject(name, parent);
            spacer.transform.AsRT().sizeDelta = size;

            return spacer;
        }

        public Popup GeneratePopup(string name, string title, Vector2 defaultPosition, Vector2 size, Action<string> refreshSearch = null, Action close = null, string placeholderText = "Search...")
        {
            var popupInstance = new Popup();
            popupInstance.Name = name;
            var popup = EditorManager.inst.GetDialog("Parent Selector").Dialog.gameObject.Duplicate(popups, name);
            popupInstance.GameObject = popup;
            popup.transform.localPosition = Vector3.zero;

            var inSize = size == Vector2.zero ? new Vector2(600f, 450f) : size;
            popup.transform.AsRT().anchoredPosition = defaultPosition;
            popup.transform.AsRT().sizeDelta = inSize;
            popupInstance.TopPanel = popup.transform.Find("Panel").AsRT();
            popupInstance.TopPanel.sizeDelta = new Vector2(inSize.x + 32f, 32f);
            var text = popupInstance.TopPanel.Find("Text").GetComponent<Text>();
            text.text = title;

            popup.transform.Find("search-box").AsRT().sizeDelta = new Vector2(inSize.x, 32f);
            popupInstance.Grid = popup.transform.Find("mask/content").GetComponent<GridLayoutGroup>();
            popupInstance.Grid.cellSize = new Vector2(inSize.x - 5f, 32f);
            popup.transform.Find("Scrollbar").AsRT().sizeDelta = new Vector2(32f, inSize.y);

            popupInstance.Close = popupInstance.TopPanel.Find("x").GetComponent<Button>();
            popupInstance.Close.onClick.ClearAll();
            popupInstance.Close.onClick.AddListener(() =>
            {
                EditorManager.inst.HideDialog(name);
                close?.Invoke();
            });

            popupInstance.SearchField = popup.transform.Find("search-box/search").GetComponent<InputField>();
            popupInstance.SearchField.onValueChanged.ClearAll();
            popupInstance.SearchField.onValueChanged.AddListener(_val => refreshSearch?.Invoke(_val));
            popupInstance.SearchField.GetPlaceholderText().text = placeholderText;
            popupInstance.Content = popup.transform.Find("mask/content");

            EditorHelper.AddEditorPopup(name, popup);

            EditorThemeManager.AddGraphic(popup.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Bottom_Left_I);

            EditorThemeManager.AddGraphic(popupInstance.TopPanel.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);

            EditorThemeManager.AddSelectable(popupInstance.Close, ThemeGroup.Close);

            EditorThemeManager.AddGraphic(popupInstance.Close.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

            EditorThemeManager.AddLightText(text);

            var scrollbar = popup.transform.Find("Scrollbar").GetComponent<Scrollbar>();
            scrollbar.value = 1f;
            EditorThemeManager.AddScrollbar(scrollbar, scrollbarRoundedSide: SpriteHelper.RoundedSide.Bottom_Right_I);

            EditorThemeManager.AddInputField(popup.transform.Find("search-box/search").GetComponent<InputField>(), ThemeGroup.Search_Field_1, 1, SpriteHelper.RoundedSide.Bottom);

            return popupInstance;
        }

        void SetupTimelineBar()
        {
            for (int i = 1; i <= 5; i++)
                timelineBar.transform.Find(i.ToString()).SetParent(transform);

            Destroy(GameObject.Find("TimelineBar/GameObject/6").GetComponent<EventTrigger>());

            eventLayerToggle = GameObject.Find("TimelineBar/GameObject/6").GetComponent<Toggle>();

            var timeObj = defaultIF.Duplicate(timelineBar.transform, "Time Input", 0);
            timeObj.transform.localScale = Vector3.one;

            timeField = timeObj.GetComponent<InputField>();

            TooltipHelper.AssignTooltip(timeObj, timeObj.name, 3f);

            timeObj.SetActive(true);
            timeField.GetPlaceholderText().text = "Set time...";
            timeField.GetPlaceholderText().alignment = TextAnchor.MiddleCenter;
            timeField.GetPlaceholderText().fontSize = 16;
            timeField.GetPlaceholderText().horizontalOverflow = HorizontalWrapMode.Overflow;
            timeField.characterValidation = InputField.CharacterValidation.Decimal;

            timeField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    AudioManager.inst.SetMusicTime(num);
            });

            TriggerHelper.AddEventTriggers(timeObj, TriggerHelper.ScrollDelta(timeField));

            EditorHelper.SetComplexity(timeField.gameObject, Complexity.Normal);

            var layersObj = timeObj.Duplicate(timelineBar.transform, "layers", 7);
            layersObj.SetActive(true);
            layersObj.transform.localScale = Vector3.one;

            TooltipHelper.AssignTooltip(layersObj, "Editor Layer", 3f);

            editorLayerField = layersObj.GetComponent<InputField>();
            editorLayerField.textComponent.alignment = TextAnchor.MiddleCenter;

            editorLayerField.text = GetLayerString(EditorManager.inst.layer);

            editorLayerImage = editorLayerField.image;

            layersObj.AddComponent<ContrastColors>().Init(editorLayerField.textComponent, editorLayerImage);

            editorLayerField.characterValidation = InputField.CharacterValidation.None;
            editorLayerField.contentType = InputField.ContentType.Standard;
            editorLayerField.GetPlaceholderText().text = "Set layer...";
            editorLayerField.GetPlaceholderText().alignment = TextAnchor.MiddleCenter;
            editorLayerField.GetPlaceholderText().fontSize = 16;
            editorLayerField.GetPlaceholderText().horizontalOverflow = HorizontalWrapMode.Overflow;
            editorLayerField.onValueChanged.RemoveAllListeners();
            editorLayerField.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                    SetLayer(Mathf.Clamp(num - 1, 0, int.MaxValue));
            });

            editorLayerImage.color = GetLayerColor(EditorManager.inst.layer);

            TriggerHelper.AddEventTriggers(layersObj,
                TriggerHelper.ScrollDeltaInt(editorLayerField, 1, 1, int.MaxValue), TriggerHelper.CreateEntry(EventTriggerType.PointerDown, eventData =>
                {
                    if (((PointerEventData)eventData).button == PointerEventData.InputButton.Middle)
                        CoreHelper.ListObjectLayers();
                }));

            var pitchObj = timeObj.Duplicate(timelineBar.transform, "pitch", 5);
            pitchObj.SetActive(true);
            pitchObj.transform.localScale = Vector3.one;
            TooltipHelper.AssignTooltip(pitchObj, "Pitch", 3f);

            pitchField = pitchObj.GetComponent<InputField>();
            pitchField.GetPlaceholderText().text = "Pitch";
            pitchField.GetPlaceholderText().alignment = TextAnchor.MiddleCenter;
            pitchField.GetPlaceholderText().fontSize = 16;
            pitchField.GetPlaceholderText().horizontalOverflow = HorizontalWrapMode.Overflow;
            pitchField.onValueChanged.ClearAll();
            pitchField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    AudioManager.inst.SetPitch(num);

                    if (num < 0f)
                        AchievementManager.inst.UnlockAchievement("editor_reverse_speed");
                }
            });

            TriggerHelper.AddEventTriggers(pitchObj, TriggerHelper.ScrollDelta(pitchField, 0.1f, 10f));

            pitchObj.GetComponent<LayoutElement>().minWidth = 64f;
            pitchField.textComponent.alignment = TextAnchor.MiddleCenter;

            pitchObj.AddComponent<InputFieldSwapper>();

            var timelineBarBase = timelineBar.transform.parent.gameObject;
            EditorThemeManager.AddGraphic(timelineBarBase.GetComponent<Image>(), ThemeGroup.Timeline_Bar);
            EditorThemeManager.AddSelectable(timeDefault.AddComponent<Button>(), ThemeGroup.List_Button_1);
            EditorThemeManager.AddLightText(timeDefault.transform.GetChild(0).GetComponent<Text>());

            EditorThemeManager.AddInputField(timeField);

            var play = timelineBar.transform.Find("play").gameObject;
            Destroy(play.GetComponent<Animator>());
            var playButton = play.GetComponent<Button>();
            playButton.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.AddSelectable(playButton, ThemeGroup.Function_2, false);

            var leftPitch = timelineBar.transform.Find("<").gameObject;
            Destroy(leftPitch.GetComponent<Animator>());
            var leftPitchButton = leftPitch.GetComponent<Button>();
            leftPitchButton.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.AddSelectable(leftPitchButton, ThemeGroup.Function_2, false);

            EditorThemeManager.AddInputField(pitchField);

            var rightPitch = timelineBar.transform.Find(">").gameObject;
            Destroy(rightPitch.GetComponent<Animator>());
            var rightPitchButton = rightPitch.GetComponent<Button>();
            rightPitchButton.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.AddSelectable(rightPitchButton, ThemeGroup.Function_2, false);

            // Leave this group empty since the color is already handled via the custom layer colors. This is only here for the rounded corners.
            EditorThemeManager.AddGraphic(editorLayerField.image, ThemeGroup.Null, true);
            EditorThemeManager.AddGraphic(eventLayerToggle.image, ThemeGroup.Event_Check, true);
            EditorThemeManager.AddGraphic(eventLayerToggle.transform.Find("Background/Text").GetComponent<Text>(), ThemeGroup.Event_Check_Text);

            EditorThemeManager.AddGraphic(eventLayerToggle.graphic, ThemeGroup.Timeline_Bar);

            var prefabButton = timelineBar.transform.Find("prefab").gameObject;
            EditorThemeManager.AddGraphic(prefabButton.GetComponent<Image>(), ThemeGroup.Prefab, true);
            EditorThemeManager.AddGraphic(prefabButton.transform.GetChild(0).GetComponent<Text>(), ThemeGroup.Prefab_Text);

            var objectButton = timelineBar.transform.Find("object").gameObject;
            EditorThemeManager.AddGraphic(objectButton.GetComponent<Image>(), ThemeGroup.Object, true);
            EditorThemeManager.AddGraphic(objectButton.transform.GetChild(0).GetComponent<Text>(), ThemeGroup.Object_Text);

            var markerButton = timelineBar.transform.Find("event").gameObject;
            EditorThemeManager.AddGraphic(markerButton.GetComponent<Image>(), ThemeGroup.Marker, true);
            EditorThemeManager.AddGraphic(markerButton.transform.GetChild(0).GetComponent<Text>(), ThemeGroup.Marker_Text);

            var checkpointButton = timelineBar.transform.Find("checkpoint").gameObject;
            EditorThemeManager.AddGraphic(checkpointButton.GetComponent<Image>(), ThemeGroup.Checkpoint, true);
            EditorThemeManager.AddGraphic(checkpointButton.transform.GetChild(0).GetComponent<Text>(), ThemeGroup.Checkpoint_Text);

            var backgroundButton = timelineBar.transform.Find("background").gameObject;
            EditorThemeManager.AddGraphic(backgroundButton.GetComponent<Image>(), ThemeGroup.Background_Object, true);
            EditorThemeManager.AddGraphic(backgroundButton.transform.GetChild(0).GetComponent<Text>(), ThemeGroup.Background_Object_Text);

            var playTest = timelineBar.transform.Find("playtest").gameObject;
            Destroy(playTest.GetComponent<Animator>());
            var playTestButton = playTest.GetComponent<Button>();
            playTestButton.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.AddSelectable(playTestButton, ThemeGroup.Function_2, false);

            var objectContextMenu = objectButton.AddComponent<ContextClickable>();
            objectContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                ShowContextMenu(DEFAULT_CONTEXT_MENU_WIDTH,
                    new ButtonFunction("Options", () => { EditorManager.inst.ShowDialog("Object Options Popup"); }),
                    new ButtonFunction("More Options", ShowObjectTemplates)
                    );
            };

            var prefabContextMenu = prefabButton.AddComponent<ContextClickable>();
            prefabContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                ShowContextMenu(DEFAULT_CONTEXT_MENU_WIDTH,
                    new ButtonFunction("Show Prefabs", PrefabEditor.inst.OpenPopup),
                    new ButtonFunction(true),
                    new ButtonFunction("Create Internal Prefab", () =>
                    {
                        PrefabEditor.inst.OpenDialog();
                        RTPrefabEditor.inst.createInternal = true;
                    }),
                    new ButtonFunction("Create External Prefab", () =>
                    {
                        PrefabEditor.inst.OpenDialog();
                        RTPrefabEditor.inst.createInternal = false;
                    })
                    );
            };

            var markerContextMenu = markerButton.AddComponent<ContextClickable>();
            markerContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                ShowContextMenu(DEFAULT_CONTEXT_MENU_WIDTH,
                    new ButtonFunction("Show Markers", () =>
                    {
                        if (!RTMarkerEditor.inst.CurrentMarker)
                        {
                            EditorManager.inst.DisplayNotification("Select / create a Marker first!", 1.5f, EditorManager.NotificationType.Warning);
                            return;
                        }

                        RTMarkerEditor.inst.OpenDialog(RTMarkerEditor.inst.CurrentMarker);
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Create Marker", MarkerEditor.inst.CreateNewMarker),
                    new ButtonFunction("Create Marker at Object", () =>
                    {
                        if (ObjectEditor.inst.CurrentSelection && ObjectEditor.inst.CurrentSelection.TimelineReference != TimelineObject.TimelineReferenceType.Null)
                            RTMarkerEditor.inst.CreateNewMarker(ObjectEditor.inst.CurrentSelection.Time);
                    })
                    );
            };

            var checkpointContextMenu = checkpointButton.AddComponent<ContextClickable>();
            checkpointContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                ShowContextMenu(DEFAULT_CONTEXT_MENU_WIDTH,
                    new ButtonFunction("Show Checkpoints", () =>
                    {
                        if (Patchers.CheckpointEditorPatch.currentCheckpoint == null || !GameData.IsValid || CheckpointEditor.inst.currentObj < 0 || CheckpointEditor.inst.currentObj >= GameData.Current.beatmapData.checkpoints.Count)
                        {
                            EditorManager.inst.DisplayNotification("Select / create a Checkpoint first!", 1.5f, EditorManager.NotificationType.Warning);
                            return;
                        }

                        CheckpointEditor.inst.OpenDialog(CheckpointEditor.inst.currentObj);
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Create Checkpoint", CheckpointEditor.inst.CreateNewCheckpoint),
                    new ButtonFunction("Create Checkpoint at Object", () =>
                    {
                        if (ObjectEditor.inst.CurrentSelection && ObjectEditor.inst.CurrentSelection.TimelineReference != TimelineObject.TimelineReferenceType.Null)
                            CheckpointEditor.inst.CreateNewCheckpoint(ObjectEditor.inst.CurrentSelection.Time, Vector2.zero);
                    })
                    );
            };

            var playTestContextMenu = playTest.AddComponent<ContextClickable>();
            playTestContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                ShowContextMenu(DEFAULT_CONTEXT_MENU_WIDTH,
                    new ButtonFunction("Playtest", () =>
                    {
                        EditorManager.inst.ToggleEditor();
                    }),
                    new ButtonFunction("Playtest Zen", () =>
                    {
                        EditorConfig.Instance.EditorZenMode.Value = true;
                        EditorManager.inst.ToggleEditor();
                    }),
                    new ButtonFunction("Playtest Normal", () =>
                    {
                        EditorConfig.Instance.EditorZenMode.Value = false;
                        EditorManager.inst.ToggleEditor();
                    })
                    );
            };

            var eventLayerContextMenu = eventLayerToggle.gameObject.AddComponent<ContextClickable>();
            eventLayerContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                ShowContextMenu(DEFAULT_CONTEXT_MENU_WIDTH,
                    new ButtonFunction("Toggle Layer Type", () => { SetLayer(Layer, layerType == LayerType.Events ? LayerType.Objects : LayerType.Events); }),
                    new ButtonFunction("View Objects", () => { SetLayer(Layer, LayerType.Objects); }),
                    new ButtonFunction("View Events", () => { SetLayer(Layer, LayerType.Events); })
                    );
            };

            var layerContextMenu = layersObj.AddComponent<ContextClickable>();
            layerContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                ShowContextMenu(DEFAULT_CONTEXT_MENU_WIDTH,
                    new ButtonFunction("List Layers with Objects", CoreHelper.ListObjectLayers),
                    new ButtonFunction("Next Free Layer", () =>
                    {
                        var layer = 0;
                        while (GameData.Current.beatmapObjects.Has(x => x.editorData != null && x.editorData.layer == layer))
                            layer++;
                        SetLayer(layer, LayerType.Objects);
                    })
                    );
            };
        }

        void SetupTimelineTriggers()
        {
            var tltrig = EditorManager.inst.timeline.GetComponent<EventTrigger>();

            tltrig.triggers.Add(TriggerHelper.CreateEntry(EventTriggerType.PointerEnter, eventData => { isOverMainTimeline = true; }));
            tltrig.triggers.Add(TriggerHelper.CreateEntry(EventTriggerType.PointerExit, eventData => { isOverMainTimeline = false; }));
            tltrig.triggers.Add(TriggerHelper.StartDragTrigger());
            tltrig.triggers.Add(TriggerHelper.DragTrigger());
            tltrig.triggers.Add(TriggerHelper.EndDragTrigger());
            tltrig.triggers.Add(TriggerHelper.CreateEntry(EventTriggerType.PointerClick, eventData =>
            {
                if (((PointerEventData)eventData).button != PointerEventData.InputButton.Right)
                    return;

                ShowContextMenu(300f,
                    new ButtonFunction("Create New", () => { ObjectEditor.inst.CreateNewNormalObject(); }),
                    new ButtonFunction("Update Everything", () =>
                    {
                        BackgroundManager.inst.UpdateBackgrounds();
                        EventManager.inst.updateEvents();
                        Updater.UpdateObjects();
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Cut", () =>
                    {
                        ObjEditor.inst.CopyObject();
                        CoreHelper.StartCoroutine(ObjectEditor.inst.DeleteObjects());
                    }),
                    new ButtonFunction("Copy", ObjEditor.inst.CopyObject),
                    new ButtonFunction("Paste", () => { ObjectEditor.inst.PasteObject(); }),
                    new ButtonFunction("Duplicate", () =>
                    {
                        var offsetTime = ObjectEditor.inst.SelectedObjects.Min(x => x.Time);

                        ObjEditor.inst.CopyObject();
                        ObjectEditor.inst.PasteObject(offsetTime);
                    }),
                    new ButtonFunction("Paste (Keep Prefab)", () => { ObjectEditor.inst.PasteObject(0f, false); }),
                    new ButtonFunction("Duplicate (Keep Prefab)", () =>
                    {
                        var offsetTime = ObjectEditor.inst.SelectedObjects.Min(x => x.Time);

                        ObjEditor.inst.CopyObject();
                        ObjectEditor.inst.PasteObject(offsetTime, false);
                    }),
                    new ButtonFunction("Delete", () => { CoreHelper.StartCoroutine(ObjectEditor.inst.DeleteObjects()); })
                    );

            }));

            for (int i = 0; i < EventEditor.inst.EventHolders.transform.childCount - 1; i++)
            {
                int type = i;
                var et = EventEditor.inst.EventHolders.transform.GetChild(i).GetComponent<EventTrigger>();
                et.triggers.Clear();
                et.triggers.Add(TriggerHelper.CreateEntry(EventTriggerType.PointerEnter, eventData => { isOverMainTimeline = true; }));
                et.triggers.Add(TriggerHelper.CreateEntry(EventTriggerType.PointerExit, eventData => { isOverMainTimeline = false; }));
                et.triggers.Add(TriggerHelper.StartDragTrigger());
                et.triggers.Add(TriggerHelper.DragTrigger());
                et.triggers.Add(TriggerHelper.EndDragTrigger());
                et.triggers.Add(TriggerHelper.CreateEntry(EventTriggerType.PointerDown, eventData =>
                {
                    var pointerEventData = (PointerEventData)eventData;

                    var layer = Layer + 1;
                    int max = RTEventEditor.EVENT_LIMIT * layer;
                    int min = max - RTEventEditor.EVENT_LIMIT;
                    var currentEvent = min + type;

                    CoreHelper.Log($"EventHolder: {type}\nMax: {max}\nMin: {min}\nCurrent Event: {currentEvent}");
                    if (pointerEventData.button == PointerEventData.InputButton.Right)
                    {
                        if (RTEventEditor.EventTypes.Length > currentEvent && (ShowModdedUI && GameData.Current.eventObjects.allEvents.Count > currentEvent || 10 > currentEvent))
                            RTEventEditor.inst.NewKeyframeFromTimeline(currentEvent);
                    }
                    if (pointerEventData.button == PointerEventData.InputButton.Middle)
                    {
                        if (RTEventEditor.EventTypes.Length > currentEvent && (ShowModdedUI && GameData.Current.eventObjects.allEvents.Count > currentEvent || 10 > currentEvent))
                        {
                            var index = GameData.Current.eventObjects.allEvents[currentEvent].FindLastIndex(x => x.eventTime < EditorManager.inst.GetTimelineTime());

                            if (index >= 0)
                                RTEventEditor.inst.SetCurrentEvent(currentEvent, index);
                        }
                    }
                }));
            }

            TriggerHelper.AddEventTriggers(EditorManager.inst.timelineScrollbar, TriggerHelper.CreateEntry(EventTriggerType.Scroll, eventData =>
            {
                var pointerEventData = (PointerEventData)eventData;

                var scrollBar = EditorManager.inst.timelineScrollRectBar;
                float multiply = Input.GetKey(KeyCode.LeftAlt) ? 0.1f : Input.GetKey(KeyCode.LeftControl) ? 10f : 1f;

                scrollBar.value = pointerEventData.scrollDelta.y > 0f ? scrollBar.value + (0.005f * multiply) : pointerEventData.scrollDelta.y < 0f ? scrollBar.value - (0.005f * multiply) : 0f;
            }));

            EditorThemeManager.AddScrollbar(EditorManager.inst.timelineScrollbar.GetComponent<Scrollbar>(),
                scrollbarGroup: ThemeGroup.Timeline_Scrollbar_Base, handleGroup: ThemeGroup.Timeline_Scrollbar, canSetScrollbarRounded: false);

            EditorThemeManager.AddGraphic(EditorManager.inst.timelineSlider.transform.Find("Background").GetComponent<Image>(), ThemeGroup.Timeline_Time_Scrollbar);

            EditorThemeManager.AddGraphic(wholeTimeline.GetComponent<Image>(), ThemeGroup.Timeline_Time_Scrollbar);

            var zoomSliderBase = EditorManager.inst.zoomSlider.transform.parent;
            EditorThemeManager.AddGraphic(zoomSliderBase.GetComponent<Image>(), ThemeGroup.Background_1, true);
            EditorThemeManager.AddGraphic(zoomSliderBase.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Slider_2);
            EditorThemeManager.AddGraphic(zoomSliderBase.transform.GetChild(2).GetComponent<Image>(), ThemeGroup.Slider_2);

            EditorThemeManager.AddGraphic(EditorManager.inst.zoomSlider.transform.Find("Background").GetComponent<Image>(), ThemeGroup.Slider_2, true);
            EditorThemeManager.AddGraphic(EditorManager.inst.zoomSlider.transform.Find("Fill Area/Fill").GetComponent<Image>(), ThemeGroup.Slider_2, true);
            EditorThemeManager.AddGraphic(EditorManager.inst.zoomSlider.image, ThemeGroup.Slider_2_Handle, true);
        }

        void SetupSelectGUI()
        {
            var openFilePopup = EditorManager.inst.GetDialog("Open File Popup").Dialog;
            var parentSelector = EditorManager.inst.GetDialog("Parent Selector").Dialog;
            var saveAsPopup = EditorManager.inst.GetDialog("Save As Popup").Dialog;
            var quickActionsPopup = EditorManager.inst.GetDialog("Quick Actions Popup").Dialog;

            var openFilePopupSelect = openFilePopup.gameObject.AddComponent<SelectGUI>();
            openFilePopupSelect.target = openFilePopup;
            openFilePopupSelect.ogPos = openFilePopup.position;

            var parentSelectorSelect = parentSelector.gameObject.AddComponent<SelectGUI>();
            parentSelectorSelect.target = parentSelector;
            parentSelectorSelect.ogPos = parentSelector.position;

            var saveAsPopupSelect = saveAsPopup.Find("New File Popup").gameObject.AddComponent<SelectGUI>();
            saveAsPopupSelect.target = saveAsPopup;
            saveAsPopupSelect.ogPos = saveAsPopup.position;

            var quickActionsPopupSelect = quickActionsPopup.gameObject.AddComponent<SelectGUI>();
            quickActionsPopupSelect.target = quickActionsPopup;
            quickActionsPopupSelect.ogPos = quickActionsPopup.position;
        }

        void SetupCreateObjects()
        {
            var dialog = EditorManager.inst.GetDialog("Object Options Popup").Dialog;

            var persistent = dialog.Find("persistent").gameObject.GetComponent<Button>();
            dialog.Find("persistent/text").gameObject.GetComponent<Text>().text = "No Autokill";
            persistent.onClick.ClearAll();
            persistent.onClick.AddListener(() => { ObjectEditor.inst.CreateNewNoAutokillObject(); });

            var empty = dialog.Find("empty").gameObject.GetComponent<Button>();
            empty.onClick.ClearAll();
            empty.onClick.AddListener(() => { ObjectEditor.inst.CreateNewEmptyObject(); });

            var decoration = dialog.Find("decoration").gameObject.GetComponent<Button>();
            decoration.onClick.ClearAll();
            decoration.onClick.AddListener(() => { ObjectEditor.inst.CreateNewDecorationObject(); });

            var helper = dialog.Find("helper").gameObject.GetComponent<Button>();
            helper.onClick.ClearAll();
            helper.onClick.AddListener(() => { ObjectEditor.inst.CreateNewHelperObject(); });

            var normal = dialog.Find("normal").gameObject.GetComponent<Button>();
            normal.onClick.ClearAll();
            normal.onClick.AddListener(() => { ObjectEditor.inst.CreateNewNormalObject(); });

            var circle = dialog.Find("shapes/circle").gameObject.GetComponent<Button>();
            circle.onClick.ClearAll();
            circle.onClick.AddListener(() => { ObjectEditor.inst.CreateNewCircleObject(); });

            var triangle = dialog.Find("shapes/triangle").gameObject.GetComponent<Button>();
            triangle.onClick.ClearAll();
            triangle.onClick.AddListener(() => { ObjectEditor.inst.CreateNewTriangleObject(); });

            var text = dialog.Find("shapes/text").gameObject.GetComponent<Button>();
            text.onClick.ClearAll();
            text.onClick.AddListener(() => { ObjectEditor.inst.CreateNewTextObject(); });

            var hexagon = dialog.Find("shapes/hexagon").gameObject.GetComponent<Button>();
            hexagon.onClick.ClearAll();
            hexagon.onClick.AddListener(() => { ObjectEditor.inst.CreateNewHexagonObject(); });

            objectTemplatePopup = GeneratePopup("Object Templates Popup", "Pick a template", Vector2.zero, new Vector2(600f, 400f), RefreshObjectTemplates, placeholderText: "Search for template...");
        }

        Popup objectTemplatePopup;

        public void ShowObjectTemplates()
        {
            EditorManager.inst.ShowDialog("Object Templates Popup");
            RefreshObjectTemplates(objectTemplatePopup.SearchField.text);
        }

        void RefreshObjectTemplates(string search)
        {
            LSHelpers.DeleteChildren(objectTemplatePopup.Content);
            
            for (int i = 0; i < objectOptions.Count; i++)
                if (RTString.SearchString(search, objectOptions[i].name))
                    GenerateObjectTemplate(objectOptions[i].name, objectOptions[i].hint, objectOptions[i].Create);
        }

        List<ObjectOption> objectOptions = new List<ObjectOption>()
        {
            new ObjectOption("Normal", "A regular square object that hits the player.", timelineObject =>
            {
                var bm = timelineObject.GetData<BeatmapObject>();

                Updater.UpdateObject(bm);
                ObjectEditor.inst.RenderTimelineObject(timelineObject);
                ObjectEditor.inst.OpenDialog(bm);
            }),
            new ObjectOption("Helper", "A regular square object that is transparent and doesn't hit the player. This can be used to warn players of an attack.", timelineObject =>
            {
                var bm = timelineObject.GetData<BeatmapObject>();
                bm.objectType = BeatmapObject.ObjectType.Helper;
                bm.name = nameof(BeatmapObject.ObjectType.Helper);

                Updater.UpdateObject(bm);
                ObjectEditor.inst.RenderTimelineObject(timelineObject);
                ObjectEditor.inst.OpenDialog(bm);
            }),
            new ObjectOption("Decoration", "A regular square object that is opaque and doesn't hit the player.", timelineObject =>
            {
                var bm = timelineObject.GetData<BeatmapObject>();
                bm.objectType = BeatmapObject.ObjectType.Decoration;
                bm.name = nameof(BeatmapObject.ObjectType.Decoration);

                Updater.UpdateObject(bm);
                ObjectEditor.inst.RenderTimelineObject(timelineObject);
                ObjectEditor.inst.OpenDialog(bm);
            }),
            new ObjectOption("Solid", "A regular square object that doesn't allow the player to passh through.", timelineObject =>
            {
                var bm = timelineObject.GetData<BeatmapObject>();
                bm.objectType = BeatmapObject.ObjectType.Solid;
                bm.name = nameof(BeatmapObject.ObjectType.Solid);

                Updater.UpdateObject(bm);
                ObjectEditor.inst.RenderTimelineObject(timelineObject);
                ObjectEditor.inst.OpenDialog(bm);
            }),
            new ObjectOption("Alpha Helper", "A regular square object that is transparent and doesn't hit the player. This can be used to warn players of an attack.", timelineObject =>
            {
                var bm = timelineObject.GetData<BeatmapObject>();
                bm.objectType = BeatmapObject.ObjectType.Decoration;
                bm.name = nameof(BeatmapObject.ObjectType.Helper);
                bm.events[3][0].eventValues[1] = 0.65f;

                Updater.UpdateObject(bm);
                ObjectEditor.inst.RenderTimelineObject(timelineObject);
                ObjectEditor.inst.OpenDialog(bm);
            }),
            new ObjectOption("Empty Hitbox", "A square object that is invisible but still has a collision and can hit the player.", timelineObject =>
            {
                var bm = timelineObject.GetData<BeatmapObject>();
                bm.objectType = BeatmapObject.ObjectType.Normal;
                bm.name = "Collision";
                bm.events[3][0].eventValues[1] = 1f;

                Updater.UpdateObject(bm);
                ObjectEditor.inst.RenderTimelineObject(timelineObject);
                ObjectEditor.inst.OpenDialog(bm);
            }),
            new ObjectOption("Empty Solid", "A square object that is invisible but still has a collision and prevents the player from passing through.", timelineObject =>
            {
                var bm = timelineObject.GetData<BeatmapObject>();
                bm.objectType = BeatmapObject.ObjectType.Solid;
                bm.name = "Collision";
                bm.events[3][0].eventValues[1] = 1f;

                Updater.UpdateObject(bm);
                ObjectEditor.inst.RenderTimelineObject(timelineObject);
                ObjectEditor.inst.OpenDialog(bm);
            }),
            new ObjectOption("Text", "A text object that can be used for dialogue.", timelineObject =>
            {
                var bm = timelineObject.GetData<BeatmapObject>();
                bm.objectType = BeatmapObject.ObjectType.Decoration;
                bm.name = "Text";
                bm.text = "A text object that can be used for dialogue.";
                bm.shape = 4;
                bm.shapeOption = 0;

                Updater.UpdateObject(bm);
                ObjectEditor.inst.RenderTimelineObject(timelineObject);
                ObjectEditor.inst.OpenDialog(bm);
            }),
            new ObjectOption("Text Sequence", "A text object that can be used for dialogue. Includes a textSequence modifier.", timelineObject =>
            {
                var bm = timelineObject.GetData<BeatmapObject>();
                bm.objectType = BeatmapObject.ObjectType.Decoration;
                bm.name = "Text";
                bm.text = "A text object that can be used for dialogue. Includes a textSequence modifier.";
                bm.shape = 4;
                bm.shapeOption = 0;
                if (ModifiersManager.defaultBeatmapObjectModifiers.TryFind(x => x.Name == "textSequence", out Modifier<BeatmapObject> modifier))
                    bm.modifiers.Add(Modifier<BeatmapObject>.DeepCopy(modifier, bm));

                Updater.UpdateObject(bm);
                ObjectEditor.inst.RenderTimelineObject(timelineObject);
                ObjectEditor.inst.OpenDialog(bm);
            }),
        };

        class ObjectOption
        {
            public ObjectOption(string name, string hint, Action<TimelineObject> action)
            {
                this.name = name;
                this.hint = hint;
                this.action = action;
            }

            public string name;
            public string hint;
            public Action<TimelineObject> action;

            public void Create()
            {
                try
                {
                    ObjectEditor.inst.CreateNewObject(action);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
            }
        }

        void GenerateObjectTemplate(string name, string hint, Action action)
        {
            var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectTemplatePopup.Content, "Function");

            gameObject.AddComponent<HoverTooltip>().tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                desc = name,
                hint = hint
            });

            var button = gameObject.GetComponent<Button>();
            button.onClick.ClearAll();
            button.onClick.AddListener(() => { action?.Invoke(); });

            EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
            var text = gameObject.transform.GetChild(0).GetComponent<Text>();
            text.text = name;
            EditorThemeManager.ApplyLightText(text);
        }

        void SetupDropdowns()
        {
            titleBar = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar").transform;

            // Here we fix the naming issues with unmodded Legacy.
            var saveAs = EditorManager.inst.GetDialog("Save As Popup").Dialog.Find("New File Popup");
            EditorManager.inst.GetDialog("Save As Popup").Dialog.Find("New File Popup/level-name").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
            GameObject.Find("Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/left/theme/name").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
            GameObject.Find("Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/name/input").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;

            var quitToArcade = EditorHelper.AddEditorDropdown("Quit to Arcade", "", "File", titleBar.Find("File/File Dropdown/Quit to Main Menu/Image").GetComponent<Image>().sprite, () =>
            {
                ShowWarningPopup("Are you sure you want to quit to the arcade? Any unsaved progress will be lost!", ArcadeHelper.QuitToArcade, HideWarningPopup);
            }, 7);
            EditorHelper.SetComplexity(quitToArcade, Complexity.Normal);

            var copyLevelToArcade = EditorHelper.AddEditorDropdown("Copy Level to Arcade", "", "File", SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_right_small.png"), () =>
            {
                if (!EditorManager.inst.hasLoadedLevel)
                {
                    EditorManager.inst.DisplayNotification("Load a level before trying to copy a level to the arcade folder!", 2f, EditorManager.NotificationType.Error);
                    return;
                }

                ShowWarningPopup("Are you sure you want to copy the level to the arcade folder?", () =>
                {
                    var name = MetaData.Current.beatmap.name;
                    name = RTString.ReplaceFormatting(name); // for cases where a user has used symbols not allowed.
                    name = RTFile.ValidateDirectory(name);
                    var directory = RTFile.CombinePaths(RTFile.ApplicationDirectory, LevelManager.ListSlash, $"{name} [{MetaData.Current.arcadeID}]");

                    if (RTFile.DirectoryExists(directory))
                    {
                        var backupDirectory = directory.Replace("beatmaps", "beatmaps/arcade backups");
                        RTFile.DeleteDirectory(backupDirectory);
                        RTFile.MoveDirectory(directory, backupDirectory);
                    }

                    var files = Directory.GetFiles(RTFile.BasePath, "*", SearchOption.AllDirectories);
                    for (int i = 0; i < files.Length; i++)
                    {
                        var file = files[i];
                        if (!RTMetaDataEditor.inst.VerifyFile(Path.GetFileName(file)))
                            continue;

                        var fileDirectory = RTFile.GetDirectory(file);
                        var fileDestination = file.Replace(fileDirectory, directory);
                        RTFile.CreateDirectory(RTFile.GetDirectory(fileDestination));
                        RTFile.CopyFile(file, fileDestination);
                    }

                    EditorManager.inst.DisplayNotification($"Successfully copied {name} to {LevelManager.Path}!", 2f, EditorManager.NotificationType.Success);
                    HideWarningPopup();
                }, HideWarningPopup);
            }, 7);
            EditorHelper.SetComplexity(copyLevelToArcade, Complexity.Normal);

            var restartEditor = EditorHelper.AddEditorDropdown("Restart Editor", "", "File", ReloadSprite, () =>
            {
                DG.Tweening.DOTween.Clear();
                Updater.UpdateObjects(false);
                GameData.Current = null;
                GameData.Current = new GameData();
                TooltipHelper.InitTooltips();
                SceneHelper.LoadEditorWithProgress();
            }, 7);
            EditorHelper.SetComplexity(restartEditor, Complexity.Normal);

            var openLevelBrowser = EditorHelper.AddEditorDropdown("Open Level Browser", "", "File", titleBar.Find("File/File Dropdown/Open/Image").GetComponent<Image>().sprite, () =>
            {
                EditorManager.inst.ShowDialog("Browser Popup");
                RefreshFileBrowserLevels();
            }, 3);
            EditorHelper.SetComplexity(openLevelBrowser, Complexity.Normal);

            var convertVGToLS = EditorHelper.AddEditorDropdown("Convert VG to LS", "", "File", SearchSprite, ConvertVGToLS, 4);
            EditorHelper.SetComplexity(convertVGToLS, Complexity.Normal);

            var addFileToLevelFolder = EditorHelper.AddEditorDropdown("Add File to Level", "", "File", SearchSprite, () =>
            {
                if (!EditorManager.inst.hasLoadedLevel)
                {
                    EditorManager.inst.DisplayNotification("Cannot add a file to level until a level has been loaded.", 4f, EditorManager.NotificationType.Warning);
                    return;
                }

                EditorManager.inst.ShowDialog("Browser Popup");
                RTFileBrowser.inst.UpdateBrowserFile(RTFile.DotFormats(FileFormat.OGG, FileFormat.WAV, FileFormat.PNG, FileFormat.JPG, FileFormat.MP4, FileFormat.LSP, FileFormat.VGP), onSelectFile: _val =>
                {
                    var selectedFile = RTFile.ReplaceSlash(_val);
                    var fileFormat = RTFile.GetFileFormat(selectedFile);

                    switch (fileFormat)
                    {
                        case FileFormat.MP4:
                        case FileFormat.MOV:
                            {
                                var copyTo = selectedFile.Replace(RTFile.AppendEndSlash(RTFile.GetDirectory(_val)), RTFile.AppendEndSlash(RTFile.BasePath)).Replace(Path.GetFileName(_val),
                                    RTFile.FileIsFormat(_val, FileFormat.MP4) ? $"bg{FileFormat.MP4.Dot()}" : $"bg{FileFormat.MOV.Dot()}");

                                if (RTFile.CopyFile(selectedFile, copyTo) && CoreConfig.Instance.EnableVideoBackground.Value)
                                {
                                    RTVideoManager.inst.Play(copyTo, 1f);
                                    EditorManager.inst.DisplayNotification($"Copied file {Path.GetFileName(selectedFile)} and started Video BG!", 2f, EditorManager.NotificationType.Success);
                                }
                                else
                                    RTVideoManager.inst.Stop();

                                return;
                            }
                        case FileFormat.LSP:
                            {
                                var prefab = Prefab.Parse(JSON.Parse(RTFile.ReadFromFile(selectedFile)));

                                RTPrefabEditor.inst.OpenPopup();
                                RTPrefabEditor.inst.ImportPrefabIntoLevel(prefab);
                                EditorManager.inst.DisplayNotification($"Imported prefab {Path.GetFileName(selectedFile)} into level!", 2f, EditorManager.NotificationType.Success);

                                return;
                            }
                        case FileFormat.VGP:
                            {
                                var prefab = Prefab.ParseVG(JSON.Parse(RTFile.ReadFromFile(selectedFile)));

                                RTPrefabEditor.inst.OpenPopup();
                                RTPrefabEditor.inst.ImportPrefabIntoLevel(prefab);
                                EditorManager.inst.DisplayNotification($"Converted & imported prefab {Path.GetFileName(selectedFile)} into level!", 2f, EditorManager.NotificationType.Success);

                                return;
                            }
                    }

                    var destination = selectedFile.Replace(RTFile.AppendEndSlash(RTFile.GetDirectory(selectedFile)), RTFile.AppendEndSlash(RTFile.BasePath));
                    if (RTFile.CopyFile(selectedFile, destination))
                        EditorManager.inst.DisplayNotification($"Copied file {Path.GetFileName(selectedFile)}!", 2f, EditorManager.NotificationType.Success);
                    else
                        EditorManager.inst.DisplayNotification($"Could not copy file {Path.GetFileName(selectedFile)}.", 2f, EditorManager.NotificationType.Error);
                });
            }, 5);
            EditorHelper.SetComplexity(addFileToLevelFolder, Complexity.Normal);

            var reloadLevel = EditorHelper.AddEditorDropdown("Reload Level", "", "File", ReloadSprite, () =>
            {
                if (!EditorManager.inst.hasLoadedLevel)
                {
                    EditorManager.inst.DisplayNotification("Cannot reload a level without one already loaded.", 2f, EditorManager.NotificationType.Warning);
                    return;
                }

                ShowWarningPopup("Are you sure you want to reload the level?", () =>
                {
                    var path = RTFile.BasePath;
                    if (RTFile.DirectoryExists(path))
                    {
                        if (GameData.IsValid)
                            GameData.Current.SaveData(RTFile.CombinePaths(path, "reload-level-backup.lsb"));
                        StartCoroutine(LoadLevel(RTFile.RemoveEndSlash(path)));
                    }
                    else
                        EditorManager.inst.DisplayNotification("Level does not exist.", 2f, EditorManager.NotificationType.Error);
                }, HideWarningPopup);
            }, 4);
            EditorHelper.SetComplexity(reloadLevel, Complexity.Normal);

            EditorHelper.AddEditorDropdown("Editor Preferences", "", "Edit", SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_preferences-white.png"), () =>
            {
                ConfigManager.inst.SetTab(2);
                ConfigManager.inst.Show();
            });

            var clearSpriteData = EditorHelper.AddEditorDropdown("Clear Sprite Data", "", "Edit", titleBar.Find("File/File Dropdown/Quit to Main Menu/Image").GetComponent<Image>().sprite, () =>
            {
                ShowWarningPopup("Are you sure you want to clear sprite data? Any Image Shapes that use a stored image will have their images cleared and you will need to set them again.", () =>
                {
                    AssetManager.SpriteAssets.Clear();
                    HideWarningPopup();
                }, HideWarningPopup);
            });
            EditorHelper.SetComplexity(clearSpriteData, Complexity.Advanced);

            var clearModifierPrefabs = EditorHelper.AddEditorDropdown("Clear Modifier Prefabs", "", "Edit", titleBar.Find("File/File Dropdown/Quit to Main Menu/Image").GetComponent<Image>().sprite, () =>
            {
                ShowWarningPopup("Are you sure you want to remove all Prefab Objects spawned from modifiers?", () =>
                {
                    var prefabObjects = GameData.Current.prefabObjects;
                    for (int i = prefabObjects.Count - 1; i >= 0; i--)
                    {
                        var prefabObject = prefabObjects[i];
                        if (prefabObject.fromModifier)
                        {
                            Updater.UpdatePrefab(prefabObject, false);
                            prefabObjects.RemoveAt(i);
                        }
                    }

                    HideWarningPopup();
                }, HideWarningPopup);
            });
            EditorHelper.SetComplexity(clearModifierPrefabs, Complexity.Advanced);

            var resetEventOffsets = EditorHelper.AddEditorDropdown("Reset Event Offsets", "", "Edit", CloseSprite, () =>
            {
                RTEventManager.inst?.SetResetOffsets();

                EditorManager.inst.DisplayNotification("Event Offsets have been reset.", 1.4f, EditorManager.NotificationType.Success);
            });
            EditorHelper.SetComplexity(resetEventOffsets, Complexity.Advanced);

            var renderWaveform = EditorHelper.AddEditorDropdown("Render Waveform", "", "Edit", ReloadSprite, () =>
            {
                if (EditorConfig.Instance.WaveformGenerate.Value)
                {
                    SetTimelineSprite(null);
                    StartCoroutine(AssignTimelineTexture());
                }
                else
                    SetTimelineSprite(null);
            });
            EditorHelper.SetComplexity(renderWaveform, Complexity.Normal);

            var deactivateModifiers = EditorHelper.AddEditorDropdown("Deactivate Modifiers", "", "Edit", CloseSprite, () =>
            {
                if (!GameData.IsValid)
                    return;

                if (!EditorManager.inst.hasLoadedLevel)
                {
                    EditorManager.inst.DisplayNotification("Load a level first!", 1f, EditorManager.NotificationType.Warning);
                    return;
                }

                var beatmapObjects = GameData.Current.beatmapObjects.FindAll(x => x.modifiers.Count > 0);
                for (int i = 0; i < beatmapObjects.Count; i++)
                {
                    var beatmapObject = beatmapObjects[i];

                    for (int j = 0; j < beatmapObject.modifiers.Count; j++)
                    {
                        var modifier = beatmapObject.modifiers[j];
                        modifier.active = false;
                        modifier.Inactive?.Invoke(modifier);
                    }
                }

                EditorManager.inst.DisplayNotification("Modifiers have been deactivated.", 1.4f, EditorManager.NotificationType.Success);
            });
            EditorHelper.SetComplexity(deactivateModifiers, Complexity.Advanced);

            var resetObjectVariables = EditorHelper.AddEditorDropdown("Reset object variables", "", "Edit", CloseSprite, () =>
            {
                if (!GameData.IsValid)
                    return;

                if (!EditorManager.inst.hasLoadedLevel)
                {
                    EditorManager.inst.DisplayNotification("Load a level first!", 1f, EditorManager.NotificationType.Warning);
                    return;
                }

                var beatmapObjects = GameData.Current.beatmapObjects;
                for (int i = 0; i < beatmapObjects.Count; i++)
                {
                    var beatmapObject = beatmapObjects[i];

                    beatmapObject.integerVariable = 0;
                }

                EditorManager.inst.DisplayNotification("Reset all integer variables to 0.", 1.4f, EditorManager.NotificationType.Success);
            });
            EditorHelper.SetComplexity(resetObjectVariables, Complexity.Advanced);

            EditorHelper.AddEditorDropdown("Get Example", "", "View", SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_example-white.png"), ExampleManager.Init);
            
            EditorHelper.AddEditorDropdown("Show Config Manager", "", "View", SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_preferences-white.png"), ConfigManager.inst.Show);

            titleBar.Find("Steam/Text").GetComponent<Text>().text = "Upload";
            var steamLayoutElement = titleBar.Find("Steam").GetComponent<LayoutElement>();
            steamLayoutElement.minWidth = 95f;
            steamLayoutElement.preferredWidth = 95f;

            EditorHelper.AddEditorDropdown("Login", "", "Steam", SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_login.png"), () =>
            {
                if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null && LegacyPlugin.authData["refresh_token"] != null)
                {
                    CoreHelper.StartCoroutine(RTMetaDataEditor.inst.RefreshTokens(null));
                    return;
                }
                RTMetaDataEditor.inst.ShowLoginPopup(null);
            });

            titleBar.Find("Help/Help Dropdown/Join Discord/Text").GetComponent<Text>().text = "Modder's Discord";
            titleBar.Find("Help/Help Dropdown/Watch Tutorials/Text").AsRT().sizeDelta = new Vector2(200f, 0f);
            titleBar.Find("Help/Help Dropdown/Watch Tutorials/Text").GetComponent<Text>().text = "Watch Mod Showcases";
            titleBar.Find("Help/Help Dropdown/Community Guides").gameObject.SetActive(false);
            titleBar.Find("Help/Help Dropdown/Which songs can I use?").gameObject.SetActive(false);
            var saveAsDropdown = titleBar.Find("File/File Dropdown/Save As").gameObject;
            saveAsDropdown.SetActive(true);
            EditorHelper.SetComplexity(saveAsDropdown, Complexity.Normal);
        }

        void SetupDoggo()
        {
            var fileInfoPopup = EditorManager.inst.GetDialog("File Info Popup").Dialog;

            fileInfoText = fileInfoPopup.Find("text").GetComponent<Text>();

            var doggoBase = Creator.NewUIObject("loading base", fileInfoPopup);
            var doggoLayout = doggoBase.AddComponent<LayoutElement>();
            doggoLayout.ignoreLayout = true;
            doggoBase.transform.AsRT().anchoredPosition = new Vector2(0f, -75f);
            doggoBase.transform.AsRT().sizeDelta = new Vector2(122f, 122f);

            var doggoBaseImage = doggoBase.AddComponent<Image>();
            var doggoMask = doggoBase.AddComponent<Mask>();
            doggoMask.showMaskGraphic = false;
            EditorThemeManager.AddGraphic(doggoBaseImage, ThemeGroup.Null, true);

            var doggoObject = Creator.NewUIObject("loading", doggoBase.transform);
            doggoObject.transform.localScale = Vector3.one;

            doggoImage = doggoObject.AddComponent<Image>();

            doggoObject.transform.AsRT().anchoredPosition = Vector2.zero;
            doggoObject.transform.AsRT().sizeDelta = new Vector2(122f, 122f);
            doggoImage.sprite = EditorManager.inst.loadingImage.sprite;

            fileInfoPopup.transform.AsRT().sizeDelta = new Vector2(500f, 320f);
        }

        void SetupPaths()
        {
            var sortList = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown")
                .Duplicate(EditorManager.inst.GetDialog("Open File Popup").Dialog);

            levelOrderDropdown = sortList.GetComponent<Dropdown>();
            EditorThemeManager.AddDropdown(levelOrderDropdown);

            var config = EditorConfig.Instance;

            var sortListRT = sortList.transform.AsRT();
            sortListRT.anchoredPosition = config.OpenLevelDropdownPosition.Value;
            var sortListTip = sortList.GetComponent<HoverTooltip>();
            sortListTip.tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                desc = "Sort the order of your levels.",
                hint = "<b>Cover</b> Sort by if level has a set cover. (Default)" +
                    "<br><b>Artist</b> Sort by song artist." +
                    "<br><b>Creator</b> Sort by level creator." +
                    "<br><b>Folder</b> Sort by level folder name." +
                    "<br><b>Title</b> Sort by song title." +
                    "<br><b>Difficulty</b> Sort by level difficulty." +
                    "<br><b>Date Edited</b> Sort by date edited / created."
            });

            Destroy(sortList.GetComponent<HideDropdownOptions>());
            levelOrderDropdown.onValueChanged.ClearAll();
            levelOrderDropdown.options.Clear();
            levelOrderDropdown.options = CoreHelper.StringToOptionData("Cover", "Artist", "Creator", "Folder", "Title", "Difficulty", "Date Edited", "Date Created");
            levelOrderDropdown.value = levelFilter;
            levelOrderDropdown.onValueChanged.AddListener(_val =>
            {
                levelFilter = _val;
                StartCoroutine(RefreshLevelList());
                SaveGlobalSettings();
            });
            EditorHelper.SetComplexity(sortList, Complexity.Normal);

            var checkDes = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle")
                .Duplicate(EditorManager.inst.GetDialog("Open File Popup").Dialog);

            var checkDesRT = checkDes.GetComponent<RectTransform>();
            checkDesRT.anchoredPosition = config.OpenLevelTogglePosition.Value;

            checkDes.transform.Find("title").GetComponent<Text>().enabled = false;
            checkDes.transform.Find("title").AsRT().sizeDelta = new Vector2(110f, 32f);

            levelAscendToggle = checkDes.transform.Find("toggle").GetComponent<Toggle>();
            levelAscendToggle.onValueChanged.ClearAll();
            levelAscendToggle.isOn = levelAscend;
            levelAscendToggle.onValueChanged.AddListener(_val =>
            {
                levelAscend = _val;
                StartCoroutine(RefreshLevelList());
                SaveGlobalSettings();
            });

            EditorThemeManager.AddToggle(levelAscendToggle);

            TooltipHelper.AddHoverTooltip(levelAscendToggle.gameObject, new List<HoverTooltip.Tooltip> { sortListTip.tooltipLangauges[0] });
            EditorHelper.SetComplexity(checkDes, Complexity.Normal);

            var contextClickable = EditorManager.inst.GetDialog("Open File Popup").Dialog.gameObject.AddComponent<ContextClickable>();
            contextClickable.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                ShowContextMenu(DEFAULT_CONTEXT_MENU_WIDTH,
                    new ButtonFunction("Create folder", () =>
                    {
                        ShowFolderCreator($"{RTFile.ApplicationDirectory}{editorListPath}", () => { UpdateEditorPath(true); HideNameEditor(); });
                    }),
                    new ButtonFunction("Create level", EditorManager.inst.OpenNewLevelPopup),
                    new ButtonFunction("Paste", PasteLevel),
                    new ButtonFunction("Open List in File Explorer", OpenLevelListFolder));
            };

            #region Level Path

            var levelPathGameObject = defaultIF.Duplicate(EditorManager.inst.GetDialog("Open File Popup").Dialog, "editor path");
            ((RectTransform)levelPathGameObject.transform).anchoredPosition = config.OpenLevelEditorPathPos.Value;
            ((RectTransform)levelPathGameObject.transform).sizeDelta = new Vector2(config.OpenLevelEditorPathLength.Value, 32f);

            (levelPathGameObject.GetComponent<HoverTooltip>() ?? levelPathGameObject.AddComponent<HoverTooltip>()).tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                keys = new List<string> { "Right click to select a folder" },
                desc = "Level list path",
                hint = "Input the path you want to load levels from within the beatmaps folder. For example: inputting \"editor\" into the input field will load levels from beatmaps/editor. You can also set it to sub-directories, like: \"editor/pa levels\" will take levels from \"beatmaps/editor/pa levels\"."
            });
            TooltipHelper.AssignTooltip(levelPathGameObject, "Editor Path", 3f);

            editorPathField = levelPathGameObject.GetComponent<InputField>();
            editorPathField.characterValidation = InputField.CharacterValidation.None;
            editorPathField.onValueChanged.ClearAll();
            editorPathField.onEndEdit.ClearAll();
            editorPathField.textComponent.alignment = TextAnchor.MiddleLeft;
            editorPathField.textComponent.fontSize = 16;
            editorPathField.text = EditorPath;
            editorPathField.onValueChanged.AddListener(_val => { EditorPath = _val; });
            editorPathField.onEndEdit.AddListener(_val => { UpdateEditorPath(false); });

            EditorThemeManager.AddInputField(editorPathField);

            var levelClickable = levelPathGameObject.AddComponent<Clickable>();
            levelClickable.onDown = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                ShowContextMenu(DEFAULT_CONTEXT_MENU_WIDTH,
                    new ButtonFunction("Set Level folder", () =>
                    {
                        EditorManager.inst.ShowDialog("Browser Popup");
                        RTFileBrowser.inst.UpdateBrowserFolder(_val =>
                        {
                            if (!_val.Replace("\\", "/").Contains(RTFile.ApplicationDirectory + "beatmaps/"))
                            {
                                EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                                return;
                            }

                            editorPathField.text = _val.Replace("\\", "/").Replace(RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/", "");
                            EditorManager.inst.DisplayNotification($"Set Editor path to {EditorPath}!", 2f, EditorManager.NotificationType.Success);
                            EditorManager.inst.HideDialog("Browser Popup");
                            UpdateEditorPath(false);
                        });
                    }),
                    new ButtonFunction("Open List in File Explorer", OpenLevelListFolder));
            };
            EditorHelper.SetComplexity(levelPathGameObject, Complexity.Advanced);

            var levelListReloader = GameObject.Find("TimelineBar/GameObject/play")
                .Duplicate(EditorManager.inst.GetDialog("Open File Popup").Dialog, "reload");
            levelListReloader.transform.AsRT().anchoredPosition = config.OpenLevelListRefreshPosition.Value;
            levelListReloader.transform.AsRT().sizeDelta = new Vector2(32f, 32f);

            (levelListReloader.GetComponent<HoverTooltip>() ?? levelListReloader.AddComponent<HoverTooltip>()).tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                desc = "Refresh level list",
                hint = "Clicking this will reload the level list."
            });

            var levelListReloaderButton = levelListReloader.GetComponent<Button>();
            levelListReloaderButton.onClick.ClearAll();
            levelListReloaderButton.onClick.AddListener(() => { UpdateEditorPath(true); });

            EditorThemeManager.AddSelectable(levelListReloaderButton, ThemeGroup.Function_2, false);

            levelListReloaderButton.image.sprite = ReloadSprite;

            #endregion

            #region Theme Path

            var themePathBase = EditorManager.inst.GetDialog("Event Editor").Dialog.Find("data/right/theme").GetChild(2).gameObject
                .Duplicate(EditorManager.inst.GetDialog("Event Editor").Dialog.Find("data/right/theme"), "themepathers", 8);

            var themePathGameObject = defaultIF.Duplicate(themePathBase.transform, "themes path");
            themePathGameObject.transform.AsRT().anchoredPosition = new Vector2(80f, 0f);
            themePathGameObject.transform.AsRT().sizeDelta = new Vector2(160f, 34f);

            (themePathGameObject.GetComponent<HoverTooltip>() ?? themePathGameObject.AddComponent<HoverTooltip>()).tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                keys = new List<string> { "Right click to select a folder", },
                desc = "Theme list path",
                hint = "Input the path you want to load themes from within the beatmaps folder. For example: inputting \"themes\" into the input field will load themes from beatmaps/themes. You can also set it to sub-directories, like: \"themes/pa colors\" will take levels from \"beatmaps/themes/pa colors\"."
            });

            themePathField = themePathGameObject.GetComponent<InputField>();
            themePathField.characterValidation = InputField.CharacterValidation.None;
            themePathField.onValueChanged.ClearAll();
            themePathField.onEndEdit.ClearAll();
            themePathField.textComponent.alignment = TextAnchor.MiddleLeft;
            themePathField.textComponent.fontSize = 16;
            themePathField.text = ThemePath;
            themePathField.onValueChanged.AddListener(_val => { ThemePath = _val; });
            themePathField.onEndEdit.AddListener(_val => { UpdateThemePath(false); });

            EditorThemeManager.AddInputField(themePathField);

            var themeClickable = themePathGameObject.AddComponent<Clickable>();
            themeClickable.onDown = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                ShowContextMenu(DEFAULT_CONTEXT_MENU_WIDTH,
                    new ButtonFunction("Set Theme folder", () =>
                    {
                        EditorManager.inst.ShowDialog("Browser Popup");
                        RTFileBrowser.inst.UpdateBrowserFolder(_val =>
                        {
                            if (!_val.Replace("\\", "/").Contains(RTFile.ApplicationDirectory + "beatmaps/"))
                            {
                                EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                                return;
                            }

                            themePathField.text = _val.Replace("\\", "/").Replace(RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/", "");
                            EditorManager.inst.DisplayNotification($"Set Theme path to {ThemePath}!", 2f, EditorManager.NotificationType.Success);
                            EditorManager.inst.HideDialog("Browser Popup");
                            UpdateThemePath(false);
                        });
                    }),
                    new ButtonFunction("Open List in File Explorer", OpenThemeListFolder));
            };

            EditorHelper.SetComplexity(themePathGameObject, Complexity.Advanced);

            var themeListReload = GameObject.Find("TimelineBar/GameObject/play").Duplicate(themePathBase.transform, "reload themes");
            themeListReload.transform.AsRT().anchoredPosition = new Vector2(166f, 35f);
            themeListReload.transform.AsRT().sizeDelta = new Vector2(32f, 32f);

            (themeListReload.GetComponent<HoverTooltip>() ?? themeListReload.AddComponent<HoverTooltip>()).tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                desc = "Refresh theme list",
                hint = "Clicking this will reload the theme list."
            });

            var themeListReloadButton = themeListReload.GetComponent<Button>();
            themeListReloadButton.onClick.ClearAll();
            themeListReloadButton.onClick.AddListener(() => { UpdateThemePath(true); });

            EditorThemeManager.AddSelectable(themeListReloadButton, ThemeGroup.Function_2, false);

            themeListReloadButton.image.sprite = ReloadSprite;

            var themePage = EditorPrefabHolder.Instance.NumberInputField.Duplicate(themePathBase.transform, "page");
            UIManager.SetRectTransform(themePage.transform.AsRT(), new Vector2(205f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 32f));
            var themePageStorage = themePage.GetComponent<InputFieldStorage>();
            RTThemeEditor.eventPageStorage = themePageStorage;
            themePage.GetComponent<HorizontalLayoutGroup>().spacing = 2f;
            themePageStorage.inputField.image.rectTransform.sizeDelta = new Vector2(60f, 32f);

            themePageStorage.inputField.onValueChanged.ClearAll();
            themePageStorage.inputField.text = "0";
            themePageStorage.inputField.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int p))
                {
                    RTThemeEditor.inst.eventThemePage = Mathf.Clamp(p, 0, RTThemeEditor.inst.ThemesCount / RTThemeEditor.eventThemesPerPage);

                    StartCoroutine(RTThemeEditor.inst.RenderThemeList(
                        EditorManager.inst.GetDialog("Event Editor").Dialog.Find("data/right/theme/theme-search").GetComponent<InputField>().text));
                }
            });

            themePageStorage.leftGreaterButton.onClick.ClearAll();
            themePageStorage.leftGreaterButton.onClick.AddListener(() => { themePageStorage.inputField.text = "0"; });

            themePageStorage.leftButton.onClick.ClearAll();
            themePageStorage.leftButton.onClick.AddListener(() =>
            {
                if (int.TryParse(themePageStorage.inputField.text, out int p))
                    themePageStorage.inputField.text = Mathf.Clamp(p - 1, 0, RTThemeEditor.inst.ThemesCount / RTThemeEditor.eventThemesPerPage).ToString();
            });

            themePageStorage.rightButton.onClick.ClearAll();
            themePageStorage.rightButton.onClick.AddListener(() =>
            {
                if (int.TryParse(themePageStorage.inputField.text, out int p))
                    themePageStorage.inputField.text = Mathf.Clamp(p + 1, 0, RTThemeEditor.inst.ThemesCount / RTThemeEditor.eventThemesPerPage).ToString();
            });

            themePageStorage.rightGreaterButton.onClick.ClearAll();
            themePageStorage.rightGreaterButton.onClick.AddListener(() =>
            {
                themePageStorage.inputField.text = (RTThemeEditor.inst.ThemesCount / RTThemeEditor.eventThemesPerPage).ToString();
            });

            Destroy(themePageStorage.middleButton.gameObject);

            EditorThemeManager.AddInputField(themePageStorage.inputField);
            EditorThemeManager.AddSelectable(themePageStorage.leftGreaterButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(themePageStorage.leftButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(themePageStorage.rightButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(themePageStorage.rightGreaterButton, ThemeGroup.Function_2, false);

            #endregion

            #region Prefab Path

            var prefabPathGameObject = defaultIF.Duplicate(EditorManager.inst.GetDialog("Prefab Popup").Dialog.Find("external prefabs"), "prefabs path");

            prefabPathGameObject.transform.AsRT().anchoredPosition = config.PrefabExternalPrefabPathPos.Value;
            prefabPathGameObject.transform.AsRT().sizeDelta = new Vector2(config.PrefabExternalPrefabPathLength.Value, 32f);

            prefabPathGameObject.AddComponent<HoverTooltip>().tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                keys = new List<string> { "Right click to select a folder", },
                desc = "Prefab list path",
                hint = "Input the path you want to load prefabs from within the beatmaps folder. For example: inputting \"prefabs\" into the input field will load levels from beatmaps/prefabs. You can also set it to sub-directories, like: \"prefabs/pa characters\" will take levels from \"beatmaps/prefabs/pa characters\"."
            });

            prefabPathField = prefabPathGameObject.GetComponent<InputField>();
            prefabPathField.characterValidation = InputField.CharacterValidation.None;
            prefabPathField.onValueChanged.ClearAll();
            prefabPathField.onEndEdit.ClearAll();
            prefabPathField.textComponent.alignment = TextAnchor.MiddleLeft;
            prefabPathField.textComponent.fontSize = 16;
            prefabPathField.text = PrefabPath;
            prefabPathField.onValueChanged.AddListener(_val => { PrefabPath = _val; });
            prefabPathField.onEndEdit.AddListener(_val => { UpdatePrefabPath(false); });

            EditorThemeManager.AddInputField(prefabPathField);

            var prefabPathClickable = prefabPathGameObject.AddComponent<Clickable>();
            prefabPathClickable.onDown = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                ShowContextMenu(DEFAULT_CONTEXT_MENU_WIDTH,
                    new ButtonFunction("Set Prefab folder", () =>
                    {
                        EditorManager.inst.ShowDialog("Browser Popup");
                        RTFileBrowser.inst.UpdateBrowserFolder(_val =>
                        {
                            if (!_val.Replace("\\", "/").Contains(RTFile.ApplicationDirectory + "beatmaps/"))
                            {
                                EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                                return;
                            }

                            prefabPathField.text = _val.Replace("\\", "/").Replace(RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/", "");
                            EditorManager.inst.DisplayNotification($"Set Prefab path to {PrefabPath}!", 2f, EditorManager.NotificationType.Success);
                            EditorManager.inst.HideDialog("Browser Popup");
                            UpdatePrefabPath(false);
                        });
                    }),
                    new ButtonFunction("Open List in File Explorer", OpenPrefabListFolder));
            };

            EditorHelper.SetComplexity(prefabPathGameObject, Complexity.Advanced);

            var prefabListReload = GameObject.Find("TimelineBar/GameObject/play")
                .Duplicate(EditorManager.inst.GetDialog("Prefab Popup").Dialog.Find("external prefabs"), "reload prefabs");
            prefabListReload.transform.AsRT().anchoredPosition = config.PrefabExternalPrefabRefreshPos.Value;
            prefabListReload.transform.AsRT().sizeDelta = new Vector2(32f, 32f);

            (prefabListReload.GetComponent<HoverTooltip>() ?? prefabListReload.AddComponent<HoverTooltip>()).tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                desc = "Refresh prefab list",
                hint = "Clicking this will reload the prefab list."
            });

            var prefabListReloadButton = prefabListReload.GetComponent<Button>();
            prefabListReloadButton.onClick.ClearAll();
            prefabListReloadButton.onClick.AddListener(() => { UpdatePrefabPath(true); } );

            EditorThemeManager.AddSelectable(prefabListReloadButton, ThemeGroup.Function_2, false);

            prefabListReloadButton.image.sprite = ReloadSprite;

            #endregion
        }

        void SetupFileBrowser()
        {
            var fileBrowser = EditorManager.inst.GetDialog("New File Popup").Dialog.Find("Browser Popup").gameObject.Duplicate(EditorManager.inst.GetDialog("New File Popup").Dialog.parent, "Browser Popup");
            fileBrowser.gameObject.SetActive(false);
            UIManager.SetRectTransform(fileBrowser.transform.AsRT(), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(600f, 364f));
            var close = fileBrowser.transform.Find("Panel/x").GetComponent<Button>();
            close.onClick.ClearAll();
            close.onClick.AddListener(() => { EditorManager.inst.HideDialog("Browser Popup"); });
            fileBrowser.transform.Find("GameObject").gameObject.SetActive(false);

            var selectGUI = fileBrowser.AddComponent<SelectGUI>();
            selectGUI.target = fileBrowser.transform;

            var rtfb = fileBrowser.AddComponent<RTFileBrowser>();
            var fileBrowserBase = fileBrowser.GetComponent<FileBrowserTest>();
            rtfb.viewport = fileBrowserBase.viewport;
            rtfb.backPrefab = fileBrowserBase.backPrefab;

            rtfb.folderPrefab = fileBrowserBase.folderPrefab.Duplicate(EditorPrefabHolder.Instance.PrefabParent, fileBrowserBase.folderPrefab.name);
            var folderPrefabStorage = rtfb.folderPrefab.AddComponent<FunctionButtonStorage>();
            folderPrefabStorage.button = rtfb.folderPrefab.GetComponent<Button>();
            folderPrefabStorage.text = rtfb.folderPrefab.transform.GetChild(0).GetComponent<Text>();

            rtfb.folderBar = fileBrowserBase.folderBar;
            rtfb.oggFileInput = fileBrowserBase.oggFileInput;
            rtfb.filePrefab = fileBrowserBase.filePrefab.Duplicate(EditorPrefabHolder.Instance.PrefabParent, fileBrowserBase.filePrefab.name);
            var filePrefabStorage = rtfb.filePrefab.AddComponent<FunctionButtonStorage>();
            filePrefabStorage.button = rtfb.filePrefab.GetComponent<Button>();
            filePrefabStorage.text = rtfb.filePrefab.transform.GetChild(0).GetComponent<Text>();

            Destroy(fileBrowserBase);

            EditorHelper.AddEditorPopup("Browser Popup", fileBrowser);

            EditorThemeManager.AddGraphic(fileBrowser.GetComponent<Image>(), ThemeGroup.Background_1, true);

            var panel = fileBrowser.transform.Find("Panel").gameObject;
            EditorThemeManager.AddGraphic(panel.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);
            EditorThemeManager.AddSelectable(close, ThemeGroup.Close);

            EditorThemeManager.AddGraphic(close.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

            EditorThemeManager.AddLightText(panel.transform.Find("Text").GetComponent<TextMeshProUGUI>());

            EditorThemeManager.AddInputField(fileBrowser.transform.Find("folder-bar").GetComponent<InputField>());
        }

        void SetupTimelinePreview()
        {
            GameManager.inst.playerGUI.transform.Find("Interface").gameObject.SetActive(false); // Set the Interface inactive so the duplicate doesn't get in the way of the editor.
            var gui = GameManager.inst.playerGUI.Duplicate(EditorManager.inst.dialogs.parent);
            GameManager.inst.playerGUI.transform.Find("Interface").gameObject.SetActive(true);
            gui.transform.SetSiblingIndex(0);

            Destroy(gui.transform.Find("Health").gameObject);
            Destroy(gui.transform.Find("Interface").gameObject);

            gui.transform.localPosition = new Vector3(-382.5f, 184.05f, 0f);
            gui.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            gui.SetActive(true);
            timelinePreview = gui.transform.Find("Timeline");
            timelinePosition = timelinePreview.Find("Base/position").GetComponent<RectTransform>();

            timelinePreviewPlayer = timelinePreview.Find("Base/position").GetComponent<Image>();
            timelinePreviewLeftCap = timelinePreview.Find("Base/Image").GetComponent<Image>();
            timelinePreviewRightCap = timelinePreview.Find("Base/Image 1").GetComponent<Image>();
            timelinePreviewLine = timelinePreview.Find("Base").GetComponent<Image>();
        }

        void SetupTimelineElements()
        {
            CoreHelper.Log($"Setting Timeline Cursor Colors");

            var config = EditorConfig.Instance;

            try
            {
                var wholeTimeline = EditorManager.inst.timeline.transform.parent.parent;

                timelineSliderHandle = wholeTimeline.Find("Slider_Parent/Slider/Handle Slide Area/Image/Handle").GetComponent<Image>();
                timelineSliderHandle.color = config.TimelineCursorColor.Value;

                timelineSliderRuler = wholeTimeline.Find("Slider_Parent/Slider/Handle Slide Area/Image").GetComponent<Image>();
                timelineSliderRuler.color = config.TimelineCursorColor.Value;

                var keyframeTimelineHandle = EditorManager.inst.GetDialog("Object Editor").Dialog.Find("timeline/Scroll View/Viewport/Content/time_slider/Handle Slide Area/Handle");
                keyframeTimelineSliderHandle = keyframeTimelineHandle.Find("Image").GetComponent<Image>();
                keyframeTimelineSliderHandle.color = config.KeyframeCursorColor.Value;

                keyframeTimelineSliderRuler = keyframeTimelineHandle.GetComponent<Image>();
                keyframeTimelineSliderRuler.color = config.KeyframeCursorColor.Value;
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"SetupTimelineElements Error {ex}");
            }
        }

        void SetupTimelineGrid()
        {
            var grid = Creator.NewUIObject("grid", EditorManager.inst.timeline.transform, 0);

            grid.AddComponent<CanvasRenderer>();

            var gridLayout = grid.AddComponent<LayoutElement>();
            gridLayout.ignoreLayout = true;

            UIManager.SetRectTransform(grid.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero);

            timelineGridRenderer = grid.AddComponent<GridRenderer>();
            timelineGridRenderer.isTimeline = true;

            var config = EditorConfig.Instance;

            timelineGridRenderer.color = config.TimelineGridColor.Value;
            timelineGridRenderer.thickness = config.TimelineGridThickness.Value;

            timelineGridRenderer.enabled = config.TimelineGridEnabled.Value;

            var gridCanvasGroup = grid.AddComponent<CanvasGroup>();
            gridCanvasGroup.blocksRaycasts = false;
            gridCanvasGroup.interactable = false;
        }

        void SetupGrid()
        {
            try
            {
                LSRenderManager.inst.FrontWorldCanvas.transform.localPosition = new Vector3(0f, 0f, -10000f);
                var grid = Creator.NewUIObject("Grid", LSRenderManager.inst.FrontWorldCanvas);
                
                previewGrid = grid.AddComponent<GridRenderer>();
                previewGrid.gridCellSize = new Vector2Int(80, 80);
                previewGrid.gridCellSpacing = new Vector2(10f, 10f);
                previewGrid.gridSize = new Vector2(1f, 1f);
                previewGrid.color = new Color(1f, 1f, 1f);
                previewGrid.thickness = 0.1f;

                grid.transform.AsRT().anchoredPosition = new Vector2(-40f, -40f);
                grid.transform.AsRT().sizeDelta = Vector2.one;


                UpdateGrid();
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"SetupGrid Exception {ex}");
            }
        }

        public void UpdateGrid()
        {
            if (!previewGrid)
                return;

            if (!EditorConfig.Instance.PreviewGridEnabled.Value)
                return;

            var gridSize = EditorConfig.Instance.PreviewGridSize.Value;
            previewGrid.gridSize = new Vector2(gridSize, gridSize);
            previewGrid.color = EditorConfig.Instance.PreviewGridColor.Value;
            previewGrid.thickness = EditorConfig.Instance.PreviewGridThickness.Value;
            previewGrid.SetVerticesDirty();
        }

        public void UpdateTimeline()
        {
            if (!timelinePreview || !AudioManager.inst.CurrentAudioSource.clip || !GameData.IsValid || GameData.Current.beatmapData == null)
                return;

            for (int i = 0; i < checkpointImages.Count; i++)
            {
                if (checkpointImages[i] && checkpointImages[i].gameObject)
                    Destroy(checkpointImages[i].gameObject);
            }

            checkpointImages.Clear();
            LSHelpers.DeleteChildren(timelinePreview.Find("elements"));
            foreach (var checkpoint in GameData.Current.beatmapData.checkpoints)
            {
                if (checkpoint.time <= 0.5f)
                    continue;

                var gameObject = GameManager.inst.checkpointPrefab.Duplicate(timelinePreview.Find("elements"), $"Checkpoint [{checkpoint.name}] - [{checkpoint.time}]");
                float num = checkpoint.time * 400f / AudioManager.inst.CurrentAudioSource.clip.length;
                gameObject.transform.AsRT().anchoredPosition = new Vector2(num, 0f);
                checkpointImages.Add(gameObject.GetComponent<Image>());
            }
        }

        void SetupNewFilePopup()
        {
            var newFilePopupBase = EditorManager.inst.GetDialog("New File Popup").Dialog;

            var newFilePopup = newFilePopupBase.Find("New File Popup");

            var newFilePopupDetection = newFilePopup.gameObject.AddComponent<Clickable>();
            newFilePopupDetection.onEnable = _val =>
            {
                if (_val)
                    return;

                if (choosingLevelTemplate)
                    EditorManager.inst.HideDialog("Open File Popup");

                choosingLevelTemplate = false;
                EditorManager.inst.HideDialog("New Level Template Dialog");
            };

            EditorThemeManager.AddGraphic(newFilePopup.GetComponent<Image>(), ThemeGroup.Background_1, true);

            var newFilePopupPanel = newFilePopup.Find("Panel").gameObject;
            EditorThemeManager.AddGraphic(newFilePopupPanel.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);

            var newFilePopupClose = newFilePopupPanel.transform.Find("x").gameObject;
            EditorThemeManager.AddSelectable(newFilePopupClose.GetComponent<Button>(), ThemeGroup.Close);

            var newFilePopupCloseX = newFilePopupClose.transform.GetChild(0).gameObject;
            EditorThemeManager.AddGraphic(newFilePopupClose.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

            var newFilePopupTitle = newFilePopupPanel.transform.Find("Title").gameObject;
            EditorThemeManager.AddLightText(newFilePopupPanel.transform.Find("Title").GetComponent<TextMeshProUGUI>());

            var openFilePopupSelect = newFilePopup.gameObject.AddComponent<SelectGUI>();
            openFilePopupSelect.target = newFilePopup;
            openFilePopupSelect.ogPos = newFilePopup.position;

            var pather = newFilePopup.Find("GameObject");
            var spacer = newFilePopup.GetChild(6);

            var path = pather.Find("song-filename").GetComponent<InputField>();

            spacer.gameObject.Duplicate(newFilePopup, "spacer", 7);

            var browseBase = pather.gameObject.Duplicate(newFilePopup, "browse", 7);

            Destroy(browseBase.transform.GetChild(0).gameObject);
            Destroy(pather.GetChild(1).gameObject);

            newFilePopup.Find("Song Filename").GetComponent<Text>().text = "Song Path";

            var browseLocal = browseBase.transform.Find("browse");
            var browseLocalText = browseLocal.Find("Text").GetComponent<Text>();
            browseLocalText.text = "Local Browser";
            var browseLocalButton = browseLocal.GetComponent<Button>();
            browseLocalButton.onClick.ClearAll();
            browseLocalButton.onClick.AddListener(() =>
            {
                string text = FileBrowser.OpenSingleFile("Select a song to use!", RTFile.ApplicationDirectory, "ogg", "wav", "mp3");
                if (!string.IsNullOrEmpty(text))
                    path.text = text;
            });

            var browseInternal = browseLocal.gameObject.Duplicate(browseBase.transform, "internal browse");
            var browseInternalText = browseInternal.transform.Find("Text").GetComponent<Text>();
            browseInternalText.text = "In-game Browser";
            var browseInternalButton = browseInternal.GetComponent<Button>();
            browseInternalButton.onClick.ClearAll();
            browseInternalButton.onClick.AddListener(() =>
            {
                EditorManager.inst.ShowDialog("Browser Popup");
                RTFileBrowser.inst.UpdateBrowserFile(RTFile.AudioDotFormats, onSelectFile: _val =>
                {
                    if (!string.IsNullOrEmpty(_val))
                    {
                        path.text = _val;
                        EditorManager.inst.HideDialog("Browser Popup");
                    }
                });
            });

            var chooseTemplate = browseLocal.gameObject.Duplicate(newFilePopup, "choose template", 8);
            var chooseTemplateText = chooseTemplate.transform.Find("Text").GetComponent<Text>();
            chooseTemplateText.text = "Choose Template";
            var chooseTemplateButton = chooseTemplate.GetComponent<Button>();
            chooseTemplateButton.onClick.ClearAll();
            chooseTemplateButton.onClick.AddListener(() =>
            {
                RefreshNewLevelTemplates();
                EditorManager.inst.ShowDialog("New Level Template Dialog");
            });
            chooseTemplate.transform.AsRT().sizeDelta = new Vector2(384f, 32f);

            spacer.gameObject.Duplicate(newFilePopup, "spacer", 8);

            var hlg = browseBase.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;

            pather.gameObject.AddComponent<HorizontalLayoutGroup>();

            var levelNameLabel = newFilePopup.Find("Level Name");
            var levelName = newFilePopup.Find("level-name");
            var songTitleLabel = levelNameLabel.gameObject.Duplicate(newFilePopup, "Song Name", 4);
            var songTitle = levelName.gameObject.Duplicate(newFilePopup, "song-title", 5);
            var songTitleLabelText = songTitleLabel.GetComponent<Text>();
            songTitleLabelText.text = "Song Title";
            var songTitleInputField = songTitle.GetComponent<InputField>();
            songTitleInputField.onValueChanged.ClearAll();
            songTitleInputField.onEndEdit.ClearAll();
            songTitleInputField.text = newLevelSongTitle;
            songTitleInputField.onValueChanged.AddListener(x => { newLevelSongTitle = x; });

            EditorThemeManager.AddLightText(newFilePopup.Find("Level Name").GetComponent<Text>());
            EditorThemeManager.AddInputField(newFilePopup.Find("level-name").GetComponent<InputField>());

            EditorThemeManager.AddLightText(songTitleLabelText);
            EditorThemeManager.AddInputField(songTitleInputField);

            EditorThemeManager.AddInputField(path);

            EditorThemeManager.AddGraphic(browseLocalButton.image, ThemeGroup.Function_2_Normal, true);
            EditorThemeManager.AddGraphic(browseLocalText, ThemeGroup.Function_2_Text);

            EditorThemeManager.AddGraphic(browseInternalButton.image, ThemeGroup.Function_2_Normal, true);
            EditorThemeManager.AddGraphic(browseInternalText, ThemeGroup.Function_2_Text);

            EditorThemeManager.AddGraphic(chooseTemplateButton.image, ThemeGroup.Function_2_Normal, true);
            EditorThemeManager.AddGraphic(chooseTemplateText, ThemeGroup.Function_2_Text);

            var create = newFilePopup.Find("submit").GetComponent<Button>();
            Destroy(create.GetComponent<Animator>());
            create.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.AddGraphic(create.image, ThemeGroup.Add, true);

            EditorThemeManager.AddGraphic(create.transform.Find("text").GetComponent<Text>(), ThemeGroup.Add_Text);

            CreateNewLevelTemplateDialog();
        }

        void CreateNewLevelTemplateDialog()
        {
            var editorDialogObject = Instantiate(EditorManager.inst.GetDialog("Multi Keyframe Editor (Object)").Dialog.gameObject);
            var editorDialogTransform = editorDialogObject.transform;
            editorDialogObject.name = "NewLevelTemplateDialog";
            editorDialogObject.layer = 5;
            editorDialogTransform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs").transform);
            editorDialogTransform.localScale = Vector3.one;
            editorDialogTransform.position = new Vector3(1537.5f, 714.945f, 0f) * EditorManager.inst.ScreenScale;
            editorDialogTransform.AsRT().sizeDelta = new Vector2(0f, 32f);

            EditorThemeManager.AddGraphic(editorDialogObject.GetComponent<Image>(), ThemeGroup.Background_1);

            var editorDialogTitle = editorDialogTransform.GetChild(0);
            var editorDialogTitleImage = editorDialogTitle.GetComponent<Image>();
            var editorDialogTitleText = editorDialogTitle.GetChild(0).GetComponent<Text>();
            editorDialogTitleText.text = "- New Level Template -";

            EditorThemeManager.AddGraphic(editorDialogTitleImage, ThemeGroup.Add);
            EditorThemeManager.AddGraphic(editorDialogTitleText, ThemeGroup.Add_Text);

            var editorDialogSpacer = editorDialogTransform.GetChild(1);
            editorDialogSpacer.AsRT().sizeDelta = new Vector2(765f, 54f);

            Destroy(editorDialogTransform.GetChild(2).gameObject);

            EditorHelper.AddEditorDialog("New Level Template Dialog", editorDialogObject);

            var scrollView = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View"));
            newLevelTemplateContent = scrollView.transform.Find("Viewport/Content");
            scrollView.transform.SetParent(editorDialogTransform);
            scrollView.transform.localScale = Vector3.one;
            scrollView.name = "Scroll View";

            LSHelpers.DeleteChildren(newLevelTemplateContent);

            var scrollViewLE = scrollView.AddComponent<LayoutElement>();
            scrollViewLE.ignoreLayout = true;

            scrollView.transform.AsRT().anchoredPosition = new Vector2(392.5f, 280f);
            scrollView.transform.AsRT().sizeDelta = new Vector2(735f, 542f);

            newLevelTemplatePrefab = EditorManager.inst.folderButtonPrefab.Duplicate(transform, "Template");

            newLevelTemplatePrefab.transform.AsRT().sizeDelta = new Vector2(734f, 200f);

            var newLevelTemplatePrefabPreviewBase = new GameObject("Preview Base");
            newLevelTemplatePrefabPreviewBase.transform.SetParent(newLevelTemplatePrefab.transform);
            newLevelTemplatePrefabPreviewBase.transform.localScale = Vector3.one;
            var newLevelTemplatePrefabPreviewBaseRT = newLevelTemplatePrefabPreviewBase.AddComponent<RectTransform>();
            var newLevelTemplatePrefabPreviewBaseImage = newLevelTemplatePrefabPreviewBase.AddComponent<Image>();
            var newLevelTemplatePrefabPreviewBaseMask = newLevelTemplatePrefabPreviewBase.AddComponent<Mask>();
            newLevelTemplatePrefabPreviewBaseMask.showMaskGraphic = false;

            newLevelTemplatePrefabPreviewBaseRT.anchoredPosition = new Vector2(-200f, 0f);
            newLevelTemplatePrefabPreviewBaseRT.sizeDelta = new Vector2(312f, 175.5f);

            var newLevelTemplatePrefabPreview = new GameObject("Preview");
            newLevelTemplatePrefabPreview.transform.SetParent(newLevelTemplatePrefabPreviewBaseRT);
            newLevelTemplatePrefabPreview.transform.localScale = Vector3.one;
            var newLevelTemplatePrefabPreviewRT = newLevelTemplatePrefabPreview.AddComponent<RectTransform>();
            var newLevelTemplatePrefabPreviewImage = newLevelTemplatePrefabPreview.AddComponent<Image>();

            newLevelTemplatePrefabPreviewRT.anchoredPosition = Vector2.zero;
            newLevelTemplatePrefabPreviewRT.anchorMax = Vector2.one;
            newLevelTemplatePrefabPreviewRT.anchorMin = Vector2.zero;
            newLevelTemplatePrefabPreviewRT.sizeDelta = Vector2.zero;

            var newLevelTemplatePrefabTitle = newLevelTemplatePrefab.transform.GetChild(0);
            newLevelTemplatePrefabTitle.name = "Title";
            newLevelTemplatePrefabTitle.AsRT().anchoredPosition = new Vector2(350f, 0f);
            newLevelTemplatePrefabTitle.AsRT().sizeDelta = new Vector2(32f, 32f);

            var noLevel = newLevelTemplatePrefabTitle.gameObject.Duplicate(newLevelTemplatePrefab.transform, "No Preview");
            noLevel.transform.AsRT().anchoredPosition = new Vector2(-200f, 0f);
            noLevel.transform.AsRT().sizeDelta = new Vector2(32f, 32f);
            var noLevelText = noLevel.GetComponent<Text>();
            noLevelText.alignment = TextAnchor.MiddleCenter;
            noLevelText.fontSize = 20;
            noLevelText.text = "No Preview";
            noLevel.SetActive(false);

            StartCoroutine(AlephNetworkManager.DownloadImageTexture($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}default_template.png", texture2D =>
            {
                newLevelTemplateBaseSprite = SpriteHelper.CreateSprite(texture2D);
            }));

            var gameObject = new GameObject("create");
            gameObject.transform.SetParent(editorDialogTransform);
            gameObject.transform.SetSiblingIndex(2);
            gameObject.transform.localScale = Vector3.one;
            var rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(765f, 32f);

            var createLevelTemplateButton = EditorPrefabHolder.Instance.Function2Button.Duplicate(rectTransform, "create");
            UIManager.SetRectTransform(createLevelTemplateButton.transform.AsRT(), new Vector2(200f, 42f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(322f, 32f));
            var createLevelTemplateButtonStorage = createLevelTemplateButton.GetComponent<FunctionButtonStorage>();
            createLevelTemplateButtonStorage.text.text = "Create a new template";
            createLevelTemplateButtonStorage.button.onClick.ClearAll();
            createLevelTemplateButtonStorage.button.onClick.AddListener(() =>
            {
                choosingLevelTemplate = true;
                EditorManager.inst.ShowDialog("Open File Popup");
                EditorManager.inst.RenderOpenBeatmapPopup();

                EditorManager.inst.DisplayNotification("Choose a level to create a template from.", 4f, EditorManager.NotificationType.Info);
            });

            var gameObject2 = new GameObject("name");
            gameObject2.transform.SetParent(editorDialogTransform);
            gameObject2.transform.SetSiblingIndex(3);
            gameObject2.transform.localScale = Vector3.one;
            var rectTransform2 = gameObject2.AddComponent<RectTransform>();
            rectTransform2.sizeDelta = new Vector2(765f, 32f);

            nameInput = EditorPrefabHolder.Instance.NumberInputField.GetComponent<InputFieldStorage>().inputField.gameObject.Duplicate(rectTransform2, "name").GetComponent<InputField>();
            nameInput.onValueChanged.ClearAll();
            nameInput.text = "New Level Template";
            UIManager.SetRectTransform(nameInput.image.rectTransform, new Vector2(160f, 42f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400f, 32f));

            var gameObject3 = new GameObject("preview");
            gameObject3.transform.SetParent(editorDialogTransform);
            gameObject3.transform.SetSiblingIndex(4);
            gameObject3.transform.localScale = Vector3.one;
            var rectTransform3 = gameObject3.AddComponent<RectTransform>();
            rectTransform3.sizeDelta = new Vector2(765f, 32f);

            var preview = new GameObject("preview");
            preview.transform.SetParent(rectTransform3);
            preview.transform.localScale = Vector3.one;

            var previewImage = preview.AddComponent<Image>();
            UIManager.SetRectTransform(previewImage.rectTransform, new Vector2(-200f, 76f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(240f, 135f));

            var choosePreviewButton = EditorPrefabHolder.Instance.Function2Button.Duplicate(rectTransform3, "choose");
            UIManager.SetRectTransform(choosePreviewButton.transform.AsRT(), new Vector2(200f, 42f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(322f, 32f));
            var choosePreviewButtonStorage = choosePreviewButton.GetComponent<FunctionButtonStorage>();
            choosePreviewButtonStorage.text.text = "Select a preview";
            choosePreviewButtonStorage.button.onClick.ClearAll();
            choosePreviewButtonStorage.button.onClick.AddListener(() =>
            {
                ShowWarningPopup("Select a file browser.", () =>
                {
                    string text = FileBrowser.OpenSingleFile("Select a preview image to use!", RTFile.ApplicationDirectory, "png");
                    if (!string.IsNullOrEmpty(text))
                    {
                        var sprite = SpriteHelper.LoadSprite(text);

                        if (sprite.texture.width != 480 || sprite.texture.height != 270)
                        {
                            EditorManager.inst.DisplayNotification("Preview image resolution must be 480p x 270p", 3f, EditorManager.NotificationType.Warning);
                            EditorManager.inst.HideDialog("Warning Popup");
                            return;
                        }

                        currentTemplateSprite = sprite;
                        previewImage.sprite = currentTemplateSprite;
                    }
                    HideWarningPopup();
                }, () =>
                {
                    EditorManager.inst.ShowDialog("Browser Popup");
                    RTFileBrowser.inst.UpdateBrowserFile(new string[] { ".png" }, onSelectFile: _val =>
                    {
                        if (string.IsNullOrEmpty(_val))
                            return;

                        EditorManager.inst.HideDialog("Browser Popup");
                        var sprite = SpriteHelper.LoadSprite(_val);

                        if (sprite.texture.width != 480 || sprite.texture.height != 270)
                        {
                            EditorManager.inst.DisplayNotification("Preview image resolution must be 480p x 270p", 3f, EditorManager.NotificationType.Warning);
                            return;
                        }

                        currentTemplateSprite = sprite;
                        previewImage.sprite = currentTemplateSprite;
                    });
                    HideWarningPopup();
                }, "System Browser", "Editor Browser");
            });
        }

        void CreatePreviewCover()
        {
            var gameObject = Creator.NewUIObject("Preview Cover", EditorManager.inst.dialogs.parent, 1);

            var rectTransform = gameObject.transform.AsRT();
            var image = gameObject.AddComponent<Image>();

            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(10000f, 10000f);

            PreviewCover = new EditorThemeManager.Element(ThemeGroup.Preview_Cover, gameObject, new List<Component> { image, });
            EditorThemeManager.AddElement(PreviewCover);

            gameObject.SetActive(!CoreHelper.AprilFools);
        }

        void CreateObjectSearch()
        {
            var objectSearchPopup = GeneratePopup("Object Search Popup", "Object Search", Vector2.zero, new Vector2(600f, 450f), _val =>
            {
                objectSearchTerm = _val;
                RefreshObjectSearch(x => ObjectEditor.inst.SetCurrentObject(ObjectEditor.inst.GetTimelineObject(x), Input.GetKey(KeyCode.LeftControl)));
            }, placeholderText: "Search for object...");

            var dropdown = EditorHelper.AddEditorDropdown("Search Objects", "", "Edit", SearchSprite, () =>
            {
                EditorManager.inst.ShowDialog("Object Search Popup");
                RefreshObjectSearch(x => ObjectEditor.inst.SetCurrentObject(ObjectEditor.inst.GetTimelineObject(x), Input.GetKey(KeyCode.LeftControl)));
            });

            EditorHelper.SetComplexity(dropdown, Complexity.Normal);
        }

        void CreateWarningPopup()
        {
            var warningPopup = EditorManager.inst.GetDialog("Save As Popup").Dialog.gameObject
                .Duplicate(EditorManager.inst.GetDialog("Save As Popup").Dialog.GetParent(), "Warning Popup");
            warningPopup.transform.localPosition = Vector3.zero;

            var main = warningPopup.transform.GetChild(0);

            var spacer1 = Creator.NewUIObject("spacerL", main);
            spacer1.AddComponent<LayoutElement>();
            var horiz = spacer1.AddComponent<HorizontalLayoutGroup>();
            horiz.spacing = 22f;

            spacer1.transform.AsRT().sizeDelta = new Vector2(292f, 40f);

            var submit1 = main.Find("submit");
            submit1.SetParent(spacer1.transform);

            var submit2 = Instantiate(submit1);
            var submit2TF = submit2.transform;

            submit2TF.SetParent(spacer1.transform);
            submit2TF.localScale = Vector3.one;

            submit1.name = "submit1";
            submit2.name = "submit2";

            var submit1Image = submit1.GetComponent<Image>();
            submit1Image.color = new Color(1f, 0.2137f, 0.2745f, 1f);
            var submit2Image = submit2.GetComponent<Image>();
            submit2Image.color = new Color(0.302f, 0.7137f, 0.6745f, 1f);

            var submit1Button = submit1.GetComponent<Button>();
            var submit2Button = submit2.GetComponent<Button>();

            submit1Button.onClick.ClearAll();

            submit2Button.onClick.ClearAll();

            Destroy(main.Find("level-name").gameObject);

            var sizeFitter = main.GetComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            main.transform.AsRT().sizeDelta = new Vector2(400f, 160f);

            main.Find("Level Name").AsRT().sizeDelta = new Vector2(292f, 64f);

            var panel = main.Find("Panel");

            var close = panel.Find("x").GetComponent<Button>();
            close.onClick.ClearAll();
            close.onClick.AddListener(() => { EditorManager.inst.HideDialog("Warning Popup"); });

            var title = panel.Find("Text").GetComponent<Text>();
            title.text = "Warning!";

            EditorHelper.AddEditorPopup("Warning Popup", warningPopup);

            EditorThemeManager.AddGraphic(main.GetComponent<Image>(), ThemeGroup.Background_1, true);
            EditorThemeManager.AddGraphic(panel.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);

            EditorThemeManager.AddSelectable(close, ThemeGroup.Close, true);
            EditorThemeManager.AddGraphic(close.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

            EditorThemeManager.AddLightText(title);

            EditorThemeManager.AddLightText(main.Find("Level Name").GetComponent<Text>());
            EditorThemeManager.AddGraphic(submit1Image, ThemeGroup.Warning_Confirm, true);
            EditorThemeManager.AddGraphic(submit1Button.transform.GetChild(0).GetComponent<Text>(), ThemeGroup.Add_Text);
            EditorThemeManager.AddGraphic(submit2Image, ThemeGroup.Warning_Cancel, true);
            EditorThemeManager.AddGraphic(submit2Image.transform.GetChild(0).GetComponent<Text>(), ThemeGroup.Add_Text);
        }

        #region Multi Object Editor

        void CreateMultiObjectEditor()
        {
            var eventButton = EditorPrefabHolder.Instance.Function1Button;

            var multiObjectEditorDialog = EditorManager.inst.GetDialog("Multi Object Editor").Dialog;

            EditorThemeManager.AddGraphic(multiObjectEditorDialog.GetComponent<Image>(), ThemeGroup.Background_1);

            var dataLeft = multiObjectEditorDialog.Find("data/left");

            dataLeft.gameObject.SetActive(true);

            var scrollView = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View").Duplicate(dataLeft);

            var parent = scrollView.transform.Find("Viewport/Content");
            multiObjectContent = parent;

            LSHelpers.DeleteChildren(parent);

            scrollView.transform.AsRT().anchoredPosition = new Vector2(240f, 345f);
            scrollView.transform.AsRT().sizeDelta = new Vector2(410f, 690f);

            dataLeft.Find("Object Editor Title").AsRT().anchoredPosition = new Vector2(405f, -16f);
            dataLeft.Find("Object Editor Title").AsRT().sizeDelta = new Vector2(760f, 32f);
            multiObjectEditorDialog.Find("data/right/Object Editor Title").gameObject.SetActive(false);

            var list = new List<GameObject>();
            list.Add(dataLeft.GetChild(1).gameObject);
            list.Add(dataLeft.GetChild(3).gameObject);
            for (int i = 0; i < list.Count; i++)
                Destroy(list[i]);

            var textHolder = multiObjectEditorDialog.Find("data/right/text holder/Text");
            var textHolderText = textHolder.GetComponent<Text>();

            EditorThemeManager.AddLightText(textHolderText);

            var updateMultiObjectInfo = textHolder.gameObject.AddComponent<UpdateMultiObjectInfo>();
            updateMultiObjectInfo.Text = textHolderText;

            textHolderText.fontSize = 22;

            textHolder.AsRT().anchoredPosition = new Vector2(0f, -125f);

            textHolder.AsRT().sizeDelta = new Vector2(-68f, 0f);

            Destroy(dataLeft.GetComponent<VerticalLayoutGroup>());

            GenerateLabels(parent, 32f, new LabelSettings("- Main Properties -", 22, FontStyle.Bold, TextAnchor.MiddleCenter));
            // Layers
            {
                GenerateLabels(parent, 32f, "Set Group Editor Layer");

                var inputFieldStorage = GenerateInputField(parent, "layer", "1", "Enter layer...", true, true);
                inputFieldStorage.GetComponent<HorizontalLayoutGroup>().spacing = 0f;
                inputFieldStorage.leftGreaterButton.onClick.NewListener(() =>
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        timelineObject.Layer = 0;
                });
                inputFieldStorage.leftButton.onClick.NewListener(() =>
                {
                    if (!int.TryParse(inputFieldStorage.inputField.text, out int num))
                        return;
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        timelineObject.Layer = Mathf.Clamp(timelineObject.Layer - num, 0, int.MaxValue);
                });
                inputFieldStorage.middleButton.onClick.NewListener(() =>
                {
                    if (!int.TryParse(inputFieldStorage.inputField.text, out int num))
                        return;
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        timelineObject.Layer = Mathf.Clamp(num - 1, 0, int.MaxValue);
                });
                inputFieldStorage.rightButton.onClick.NewListener(() =>
                {
                    if (!int.TryParse(inputFieldStorage.inputField.text, out int num))
                        return;
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        timelineObject.Layer = Mathf.Clamp(timelineObject.Layer + num, 0, int.MaxValue);
                });
                TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDeltaInt(inputFieldStorage.inputField));

                EditorHelper.SetComplexity(inputFieldStorage.leftGreaterButton.gameObject, Complexity.Normal);

                GenerateButtons(parent, 32f, 0f, new ButtonFunction("Move to Current Editor Layer", () =>
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        timelineObject.Layer = Layer;
                }));
            }

            // Depth
            {
                GenerateLabels(parent, 32f, "Set Group Render Depth");

                var inputFieldStorage = GenerateInputField(parent, "depth", "1", "Enter depth...", true);
                inputFieldStorage.leftButton.onClick.NewListener(() =>
                {
                    if (!int.TryParse(inputFieldStorage.inputField.text, out int num))
                        return;
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();
                        bm.Depth -= num;
                        Updater.UpdateObject(bm, "Depth");
                    }
                });
                inputFieldStorage.middleButton.onClick.NewListener(() =>
                {
                    if (!int.TryParse(inputFieldStorage.inputField.text, out int num))
                        return;
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();
                        bm.Depth = num;
                        Updater.UpdateObject(bm, "Depth");
                    }
                });
                inputFieldStorage.rightButton.onClick.NewListener(() =>
                {
                    if (!int.TryParse(inputFieldStorage.inputField.text, out int num))
                        return;
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();
                        bm.Depth += num;
                        Updater.UpdateObject(bm, "Depth");
                    }
                });
                TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDeltaInt(inputFieldStorage.inputField));
            }

            // Song Time
            {
                GenerateLabels(parent, 32f, "Set Song Time");

                var inputFieldStorage = GenerateInputField(parent, "time", "1", "Enter time...", true);
                inputFieldStorage.leftButton.onClick.NewListener(() =>
                {
                    if (!float.TryParse(inputFieldStorage.inputField.text, out float num))
                        return;
                    //float first = ObjectEditor.inst.SelectedObjects.Min(x => x.Time);

                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                    {
                        //timelineObject.Time = AudioManager.inst.CurrentAudioSource.time - first + timelineObject.Time + num;
                        timelineObject.Time = timelineObject.Time - num;
                        if (timelineObject.isBeatmapObject)
                            Updater.UpdateObject(timelineObject.GetData<BeatmapObject>(), "StartTime");
                        if (timelineObject.isPrefabObject)
                            Updater.UpdatePrefab(timelineObject.GetData<PrefabObject>());

                        timelineObject.UpdatePosLength();
                    }
                });
                inputFieldStorage.middleButton.onClick.NewListener(() =>
                {
                    if (!float.TryParse(inputFieldStorage.inputField.text, out float num))
                        return;

                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                    {
                        timelineObject.Time = num;
                        if (timelineObject.isBeatmapObject)
                            Updater.UpdateObject(timelineObject.GetData<BeatmapObject>(), "StartTime");
                        if (timelineObject.isPrefabObject)
                            Updater.UpdatePrefab(timelineObject.GetData<PrefabObject>());

                        timelineObject.UpdatePosLength();
                    }
                });
                inputFieldStorage.rightButton.onClick.NewListener(() =>
                {
                    if (!float.TryParse(inputFieldStorage.inputField.text, out float num))
                        return;
                    //float first = ObjectEditor.inst.SelectedObjects.Min(x => x.Time);

                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                    {
                        //timelineObject.Time = AudioManager.inst.CurrentAudioSource.time - first + timelineObject.Time - num;
                        timelineObject.Time = timelineObject.Time + num;
                        if (timelineObject.isBeatmapObject)
                            Updater.UpdateObject(timelineObject.GetData<BeatmapObject>(), "StartTime");
                        if (timelineObject.isPrefabObject)
                            Updater.UpdatePrefab(timelineObject.GetData<PrefabObject>());

                        timelineObject.UpdatePosLength();
                    }
                });
                TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDelta(inputFieldStorage.inputField));
            }

            // Autokill Offset
            {
                var labels = GenerateLabels(parent, 32f, "Set Autokill Offset");

                var inputFieldStorage = GenerateInputField(parent, "autokill offset", "0", "Enter autokill...", true);
                inputFieldStorage.leftButton.onClick.NewListener(() =>
                {
                    if (!float.TryParse(inputFieldStorage.inputField.text, out float num))
                        return;
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();
                        bm.autoKillOffset -= num;
                        Updater.UpdateObject(bm, "Autokill");
                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                    }
                });
                inputFieldStorage.middleButton.onClick.NewListener(() =>
                {
                    if (!float.TryParse(inputFieldStorage.inputField.text, out float num))
                        return;
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();
                        bm.autoKillOffset = num;
                        Updater.UpdateObject(bm, "Autokill");
                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                    }
                });
                inputFieldStorage.rightButton.onClick.NewListener(() =>
                {
                    if (!float.TryParse(inputFieldStorage.inputField.text, out float num))
                        return;
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();
                        bm.autoKillOffset += num;
                        Updater.UpdateObject(bm, "Autokill");
                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                    }
                });
                TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDelta(inputFieldStorage.inputField));

                EditorHelper.SetComplexity(labels, Complexity.Normal);
                EditorHelper.SetComplexity(inputFieldStorage.gameObject, Complexity.Normal);
            }

            // Name
            {
                GenerateLabels(parent, 32f, "Set Name");

                var multiNameSet = EditorPrefabHolder.Instance.NumberInputField.Duplicate(parent, "name");
                multiNameSet.transform.localScale = Vector3.one;
                var inputFieldStorage = multiNameSet.GetComponent<InputFieldStorage>();

                multiNameSet.transform.AsRT().sizeDelta = new Vector2(428f, 32f);

                inputFieldStorage.inputField.onValueChanged.ClearAll();
                inputFieldStorage.inputField.characterValidation = InputField.CharacterValidation.None;
                inputFieldStorage.inputField.characterLimit = 0;
                inputFieldStorage.inputField.text = "name";
                inputFieldStorage.inputField.transform.AsRT().sizeDelta = new Vector2(300f, 32f);
                ((Text)inputFieldStorage.inputField.placeholder).text = "Enter name...";

                EditorThemeManager.AddInputField(inputFieldStorage.inputField);

                Destroy(inputFieldStorage.leftGreaterButton.gameObject);
                Destroy(inputFieldStorage.leftButton.gameObject);
                Destroy(inputFieldStorage.rightGreaterButton.gameObject);

                inputFieldStorage.middleButton.onClick.ClearAll();
                inputFieldStorage.middleButton.onClick.AddListener(() =>
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                    {
                        timelineObject.GetData<BeatmapObject>().name = inputFieldStorage.inputField.text;
                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                    }
                });

                EditorThemeManager.AddSelectable(inputFieldStorage.middleButton, ThemeGroup.Function_2, false);

                inputFieldStorage.rightButton.name = "+";

                var addFilePath = RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/add.png";

                if (RTFile.FileExists(addFilePath))
                    inputFieldStorage.rightButton.image.sprite = SpriteHelper.LoadSprite(addFilePath);

                var mtnLeftLE = inputFieldStorage.rightButton.gameObject.AddComponent<LayoutElement>();
                mtnLeftLE.ignoreLayout = true;

                inputFieldStorage.rightButton.transform.AsRT().anchoredPosition = new Vector2(339f, 0f);
                inputFieldStorage.rightButton.transform.AsRT().sizeDelta = new Vector2(32f, 32f);

                inputFieldStorage.rightButton.onClick.ClearAll();
                inputFieldStorage.rightButton.onClick.AddListener(() =>
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                    {
                        timelineObject.GetData<BeatmapObject>().name += inputFieldStorage.inputField.text;
                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                    }
                });

                EditorThemeManager.AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);
            }

            // Tags
            {
                var labels = GenerateLabels(parent, 32f, "Add a Tag");

                var multiNameSet = EditorPrefabHolder.Instance.NumberInputField.Duplicate(parent, "name");
                multiNameSet.transform.localScale = Vector3.one;
                var inputFieldStorage = multiNameSet.GetComponent<InputFieldStorage>();

                multiNameSet.transform.AsRT().sizeDelta = new Vector2(428f, 32f);

                inputFieldStorage.inputField.onValueChanged.ClearAll();
                inputFieldStorage.inputField.characterValidation = InputField.CharacterValidation.None;
                inputFieldStorage.inputField.characterLimit = 0;
                inputFieldStorage.inputField.text = "object group";
                inputFieldStorage.inputField.transform.AsRT().sizeDelta = new Vector2(300f, 32f);
                inputFieldStorage.inputField.GetPlaceholderText().text = "Enter a tag...";

                EditorThemeManager.AddInputField(inputFieldStorage.inputField);

                Destroy(inputFieldStorage.leftGreaterButton.gameObject);
                Destroy(inputFieldStorage.leftButton.gameObject);
                Destroy(inputFieldStorage.middleButton.gameObject);
                Destroy(inputFieldStorage.rightGreaterButton.gameObject);

                inputFieldStorage.rightButton.name = "+";

                var addFilePath = RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/add.png";

                if (RTFile.FileExists(addFilePath))
                    inputFieldStorage.rightButton.image.sprite = SpriteHelper.LoadSprite(addFilePath);

                var mtnLeftLE = inputFieldStorage.rightButton.gameObject.AddComponent<LayoutElement>();
                mtnLeftLE.ignoreLayout = true;

                inputFieldStorage.rightButton.transform.AsRT().anchoredPosition = new Vector2(339f, 0f);
                inputFieldStorage.rightButton.transform.AsRT().sizeDelta = new Vector2(32f, 32f);

                inputFieldStorage.rightButton.onClick.ClearAll();
                inputFieldStorage.rightButton.onClick.AddListener(() =>
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        timelineObject.GetData<BeatmapObject>().tags.Add(inputFieldStorage.inputField.text);
                });

                EditorThemeManager.AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);

                EditorHelper.SetComplexity(labels, Complexity.Advanced);
                EditorHelper.SetComplexity(multiNameSet, Complexity.Advanced);
            }

            // Timeline Object Index
            {
                GenerateLabels(parent, 32f, "Set Group Index");

                var inputFieldStorage = GenerateInputField(parent, "indexer", "1", "Enter index...", true, true, true);
                inputFieldStorage.GetComponent<HorizontalLayoutGroup>().spacing = 0f;
                inputFieldStorage.leftGreaterButton.onClick.NewListener(() => { EditorHelper.SetSelectedObjectIndexes(0); });
                inputFieldStorage.leftButton.onClick.NewListener(() =>
                {
                    if (int.TryParse(inputFieldStorage.inputField.text, out int num))
                        EditorHelper.AddSelectedObjectIndexes(-num);
                });
                inputFieldStorage.middleButton.onClick.NewListener(() =>
                {
                    if (int.TryParse(inputFieldStorage.inputField.text, out int num))
                        EditorHelper.SetSelectedObjectIndexes(num);
                });
                inputFieldStorage.rightButton.onClick.NewListener(() =>
                {
                    if (int.TryParse(inputFieldStorage.inputField.text, out int num))
                        EditorHelper.AddSelectedObjectIndexes(num);
                });
                inputFieldStorage.rightGreaterButton.onClick.NewListener(() => { EditorHelper.SetSelectedObjectIndexes(timelineObjects.Count); });
                TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDeltaInt(inputFieldStorage.inputField));

                EditorHelper.SetComplexity(inputFieldStorage.leftGreaterButton.gameObject, Complexity.Advanced);
            }

            GeneratePad(parent);
            GenerateLabels(parent, 32f, new LabelSettings("- Actions -", 22, FontStyle.Bold, TextAnchor.MiddleCenter));

            // Clear data
            {
                var labels = GenerateLabels(parent, 32f, "Clear data from objects");

                var buttons1 = GenerateButtons(parent, 32f, 8f,
                     new ButtonFunction("Clear tags", () =>
                     {
                         ShowWarningPopup("You are about to clear tags from all selected objects, this <b>CANNOT</b> be undone!", () =>
                         {
                             foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                             {
                                 beatmapObject.tags.Clear();
                             }

                             HideWarningPopup();
                         }, HideWarningPopup);
                     }) { FontSize = 16 },
                     new ButtonFunction("Clear anims", () =>
                     {
                         ShowWarningPopup("You are about to clear animations from all selected objects, this <b>CANNOT</b> be undone!", () =>
                         {
                             foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                             {
                                 var bm = timelineObject.GetData<BeatmapObject>();
                                 foreach (var tkf in timelineObject.InternalTimelineObjects)
                                 {
                                     Destroy(tkf.GameObject);
                                 }
                                 timelineObject.InternalTimelineObjects.Clear();
                                 for (int i = 0; i < bm.events.Count; i++)
                                 {
                                     bm.events[i] = bm.events[i].OrderBy(x => x.eventTime).ToList();
                                     var firstKF = EventKeyframe.DeepCopy((EventKeyframe)bm.events[i][0], false);
                                     bm.events[i].Clear();
                                     bm.events[i].Add(firstKF);
                                 }
                                 if (ObjectEditor.inst.SelectedObjects.Count == 1)
                                 {
                                     ObjectEditor.inst.ResizeKeyframeTimeline(bm);
                                     ObjectEditor.inst.RenderKeyframes(bm);
                                 }

                                 Updater.UpdateObject(bm, "Keyframes");
                                 ObjectEditor.inst.RenderTimelineObject(timelineObject);
                             }

                             HideWarningPopup();
                         }, HideWarningPopup);
                     }) { FontSize = 16 },
                     new ButtonFunction("Clear modifiers", () =>
                     {
                         ShowWarningPopup("You are about to clear modifiers from all selected objects, this <b>CANNOT</b> be undone!", () =>
                         {
                             foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                             {
                                 beatmapObject.modifiers.Clear();
                                 Updater.UpdateObject(beatmapObject, recalculate: false);
                             }
                             Updater.RecalculateObjectStates();

                             HideWarningPopup();
                         }, HideWarningPopup);
                     }) { FontSize = 16 });

                EditorHelper.SetComplexity(labels, Complexity.Normal);
                EditorHelper.SetComplexity(buttons1, Complexity.Normal);
            }

            // Optimization
            {
                var labels = GenerateLabels(parent, 32f, "Auto optimize objects");
                var buttons1 = GenerateButtons(parent, 32f, 0f, new ButtonFunction("Optimize", () =>
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                    {
                        var beatmapObject = timelineObject.GetData<BeatmapObject>();
                        beatmapObject.SetAutokillToScale(GameData.Current.beatmapObjects);
                        Updater.UpdateObject(beatmapObject, "Autokill");
                        timelineObject.UpdatePosLength();
                    }
                }));

                EditorHelper.SetComplexity(labels, Complexity.Advanced);
                EditorHelper.SetComplexity(buttons1, Complexity.Advanced);
            }

            // Song Time Autokill
            {
                var labels = GenerateLabels(parent, 32f, "Set autokill to current time");
                var buttons1 = GenerateButtons(parent, 32f, 0f, new ButtonFunction("Set", () =>
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                    {
                        var beatmapObject = timelineObject.GetData<BeatmapObject>();

                        float num = 0f;

                        if (beatmapObject.autoKillType == AutoKillType.SongTime)
                            num = AudioManager.inst.CurrentAudioSource.time;
                        else num = AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime;

                        if (num < 0f)
                            num = 0f;

                        beatmapObject.autoKillOffset = num;

                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                        Updater.UpdateObject(beatmapObject, "Autokill");
                    }
                }));

                EditorHelper.SetComplexity(labels, Complexity.Normal);
                EditorHelper.SetComplexity(buttons1, Complexity.Normal);
            }

            GeneratePad(parent);
            GenerateLabels(parent, 32f, new LabelSettings("- Object Properties -", 22, FontStyle.Bold, TextAnchor.MiddleCenter));

            // Autokill Type
            {
                var labels = GenerateLabels(parent, 32f, "Set Autokill Type");

                var buttons1 = GenerateButtons(parent, 48f, 8f,
                    new ButtonFunction("No Autokill", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.autoKillType = AutoKillType.OldStyleNoAutokill;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "Autokill");
                        }
                    }),
                    new ButtonFunction("Last KF", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.autoKillType = AutoKillType.LastKeyframe;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "Autokill");
                        }
                    }),
                    new ButtonFunction("Last KF Offset", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.autoKillType = AutoKillType.LastKeyframeOffset;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "Autokill");
                        }
                    }),
                    new ButtonFunction("Fixed Time", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.autoKillType = AutoKillType.FixedTime;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "Autokill");
                        }
                    }),
                    new ButtonFunction("Song Time", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.autoKillType = AutoKillType.SongTime;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "Autokill");
                        }
                    }));

                EditorHelper.SetComplexity(labels, Complexity.Normal);
                EditorHelper.SetComplexity(buttons1, Complexity.Normal);
            }

            // Set Parent
            {
                GenerateLabels(parent, 32f, "Set Parent");
                GenerateButtons(parent, 32f, 8f,
                    new ButtonFunction("Search list", EditorManager.inst.OpenParentPopup),
                    new ButtonFunction("Picker", () =>
                    {
                        parentPickerEnabled = true;
                        selectingMultiple = true;
                    }),
                    new ButtonFunction("Remove", () =>
                    {
                        ShowWarningPopup("Are you sure you want to remove parents from all selected objects? This <b>CANNOT</b> be undone!", () =>
                        {
                            foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                            {
                                beatmapObject.parent = "";
                                Updater.UpdateObject(beatmapObject);
                            }

                            HideWarningPopup();
                        }, HideWarningPopup);
                    }));
            }

            // Parent Desync
            {
                var labels = GenerateLabels(parent, 32f, "Modify parent desync");

                var buttons1 = GenerateButtons(parent, 32f, 8f,
                    new ButtonFunction("On", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.FindAll(x => x.isBeatmapObject))
                        {
                            timelineObject.GetData<BeatmapObject>().desync = true;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                        }
                    }),
                    new ButtonFunction("Off", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.FindAll(x => x.isBeatmapObject))
                        {
                            timelineObject.GetData<BeatmapObject>().desync = false;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                        }
                    }));
                var buttons2 = GenerateButtons(parent, 32f, 0f, new ButtonFunction("Swap", () =>
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.FindAll(x => x.isBeatmapObject))
                    {
                        timelineObject.GetData<BeatmapObject>().desync = !timelineObject.GetData<BeatmapObject>().desync;

                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                    }
                }));

                EditorHelper.SetComplexity(labels, Complexity.Advanced);
                EditorHelper.SetComplexity(buttons1, Complexity.Advanced);
                EditorHelper.SetComplexity(buttons2, Complexity.Advanced);
            }

            // Force Snap BPM
            {
                var labels = GenerateLabels(parent, 32f, "Force Snap Start Time to BPM");
                var buttons1 = GenerateButtons(parent, 32f, 8f, new ButtonFunction("Snap", () =>
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                    {
                        timelineObject.Time = SnapToBPM(timelineObject.Time);
                        if (timelineObject.isBeatmapObject)
                            Updater.UpdateObject(timelineObject.GetData<BeatmapObject>(), "Start Time");
                        if (timelineObject.isPrefabObject)
                            Updater.UpdatePrefab(timelineObject.GetData<PrefabObject>(), "Start Time");

                        timelineObject.UpdatePosLength();
                    }
                }), new ButtonFunction("Snap Offset", () =>
                {
                    var time = ObjectEditor.inst.SelectedObjects.Min(x => x.Time);
                    var snappedTime = SnapToBPM(time);
                    var distance = -time + snappedTime;
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                    {
                        timelineObject.Time += distance;
                        if (timelineObject.isBeatmapObject)
                            Updater.UpdateObject(timelineObject.GetData<BeatmapObject>(), "Start Time");
                        if (timelineObject.isPrefabObject)
                            Updater.UpdatePrefab(timelineObject.GetData<PrefabObject>(), "Start Time");

                        timelineObject.UpdatePosLength();
                    }
                }));

                EditorHelper.SetComplexity(labels, Complexity.Normal);
                EditorHelper.SetComplexity(buttons1, Complexity.Normal);
            }

            // Object Type
            {
                GenerateLabels(parent, 32f, "Set Object Type");

                var buttons1 = GenerateButtons(parent, 32f, 8f,
                    new ButtonFunction("Sub", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();

                            int objectType = (int)bm.objectType;

                            objectType--;
                            if (objectType < 0)
                                objectType = 4;

                            bm.objectType = (BeatmapObject.ObjectType)objectType;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "ObjectType");
                        }
                    }),
                    new ButtonFunction("Add", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();

                            int objectType = (int)bm.objectType;

                            objectType++;
                            if (objectType > 4)
                                objectType = 0;

                            bm.objectType = (BeatmapObject.ObjectType)objectType;


                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "ObjectType");
                        }
                    }));

                GenerateButtons(parent, 48f, 8f,
                    new ButtonFunction(nameof(BeatmapObject.ObjectType.Normal), () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.objectType = BeatmapObject.ObjectType.Normal;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "ObjectType");
                        }
                    }),
                    new ButtonFunction(nameof(BeatmapObject.ObjectType.Helper), () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.objectType = BeatmapObject.ObjectType.Helper;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "ObjectType");
                        }
                    }),
                    new ButtonFunction("Deco", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.objectType = BeatmapObject.ObjectType.Decoration;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "ObjectType");
                        }
                    }),
                    new ButtonFunction(nameof(BeatmapObject.ObjectType.Empty), () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.objectType = BeatmapObject.ObjectType.Empty;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "ObjectType");
                        }
                    }),
                    new ButtonFunction(nameof(BeatmapObject.ObjectType.Solid), () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.objectType = BeatmapObject.ObjectType.Solid;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "ObjectType");
                        }
                    }));

                EditorHelper.SetComplexity(buttons1, Complexity.Advanced);
            }
            
            // Gradient Type
            {
                var labels = GenerateLabels(parent, 32f, "Set Gradient Type");

                var buttons1 = GenerateButtons(parent, 32f, 8f,
                    new ButtonFunction("Sub", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();

                            int gradientType = (int)bm.gradientType;

                            gradientType--;
                            if (gradientType < 0)
                                gradientType = 4;

                            bm.gradientType = (BeatmapObject.GradientType)gradientType;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "GradientType");
                        }
                    }),
                    new ButtonFunction("Add", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            int gradientType = (int)bm.gradientType;

                            gradientType--;
                            if (gradientType > 4)
                                gradientType = 0;

                            bm.gradientType = (BeatmapObject.GradientType)gradientType;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "GradientType");
                        }
                    }));

                var buttons2 = GenerateButtons(parent, 48f, 8f,
                    new ButtonFunction(nameof(BeatmapObject.GradientType.Normal), () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.gradientType = BeatmapObject.GradientType.Normal;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "GradientType");
                        }
                    }),
                    new ButtonFunction("Linear Right", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.gradientType = BeatmapObject.GradientType.RightLinear;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "GradientType");
                        }
                    }),
                    new ButtonFunction("Linear Left", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.gradientType = BeatmapObject.GradientType.LeftLinear;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "GradientType");
                        }
                    }),
                    new ButtonFunction("Radial In", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.gradientType = BeatmapObject.GradientType.OutInRadial;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "GradientType");
                        }
                    }),
                    new ButtonFunction("Radial Out", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.gradientType = BeatmapObject.GradientType.InOutRadial;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "GradientType");
                        }
                    }));

                EditorHelper.SetComplexity(labels, Complexity.Normal);
                EditorHelper.SetComplexity(buttons1, Complexity.Normal);
                EditorHelper.SetComplexity(buttons2, Complexity.Normal);
            }

            // Shape
            {
                GenerateLabels(parent, 32f, "Shape");
                //shapeSiblingIndex = parent.childCount;
                RenderMultiShape();
            }

            // Assign Objects to Prefab
            {
                var labels = GenerateLabels(parent, 32f, "Assign Objects to Prefab");
                var buttons1 = GenerateButtons(parent, 32f, 8f,
                    new ButtonFunction("Assign", () =>
                    {
                        selectingMultiple = true;
                        prefabPickerEnabled = true;
                    }),
                    new ButtonFunction("Remove", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        {
                            timelineObject.GetData<BeatmapObject>().RemovePrefabReference();
                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                        }
                    }));

                EditorHelper.SetComplexity(labels, Complexity.Normal);
                EditorHelper.SetComplexity(buttons1, Complexity.Normal);
            }

            GeneratePad(parent);
            GenerateLabels(parent, 32f, new LabelSettings("- Toggles -", 22, FontStyle.Bold, TextAnchor.MiddleCenter));

            // Lock
            {
                GenerateLabels(parent, 32f, "Modify time lock state");

                GenerateButtons(parent, 32f, 8f,
                    new ButtonFunction("On", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            timelineObject.Locked = true;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                        }
                    }),
                    new ButtonFunction("Off", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            timelineObject.Locked = false;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                        }
                    }));
                GenerateButtons(parent, 32f, 0f, new ButtonFunction("Swap", () =>
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                    {
                        timelineObject.Locked = !timelineObject.Locked;

                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                    }
                }));
            }

            // Collapse
            {
                GenerateLabels(parent, 32f, "Modify timeline collapse state");

                GenerateButtons(parent, 32f, 8f,
                    new ButtonFunction("On", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            timelineObject.Collapse = true;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                        }
                    }),
                    new ButtonFunction("Off", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            timelineObject.Collapse = false;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                        }
                    }));
                GenerateButtons(parent, 32f, 0f, new ButtonFunction("Swap", () =>
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                    {
                        timelineObject.Collapse = !timelineObject.Collapse;

                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                    }
                }));
            }

            // Render Type
            {
                var labels = GenerateLabels(parent, 32f, "Modify Object Render Type");

                var buttons1 = GenerateButtons(parent, 32f, 8f,
                    new ButtonFunction("Background", () =>
                    {
                        foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                        {
                            beatmapObject.background = true;
                            if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
                                levelObject.visualObject.GameObject.layer = beatmapObject.background ? 9 : 8;
                        }
                    }),
                    new ButtonFunction("Foreground", () =>
                    {
                        foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                        {
                            beatmapObject.background = false;
                            if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
                                levelObject.visualObject.GameObject.layer = beatmapObject.background ? 9 : 8;
                        }
                    }));
                var buttons2 = GenerateButtons(parent, 32f, 0f, new ButtonFunction("Swap", () =>
                {
                    foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                    {
                        beatmapObject.background = !beatmapObject.background;
                        if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
                            levelObject.visualObject.GameObject.layer = beatmapObject.background ? 9 : 8;
                    }
                }));

                EditorHelper.SetComplexity(labels, Complexity.Advanced);
                EditorHelper.SetComplexity(buttons1, Complexity.Advanced);
                EditorHelper.SetComplexity(buttons2, Complexity.Advanced);
            }

            // LDM
            {
                var labels = GenerateLabels(parent, 32f, "Modify Low Detail Mode");

                var buttons1 = GenerateButtons(parent, 32f, 8f,
                    new ButtonFunction("On", () =>
                    {
                        foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                        {
                            beatmapObject.LDM = true;
                            Updater.UpdateObject(beatmapObject);
                        }
                    }),
                    new ButtonFunction("Off", () =>
                    {
                        foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                        {
                            beatmapObject.LDM = false;
                            Updater.UpdateObject(beatmapObject);
                        }
                    }));
                var buttons2 = GenerateButtons(parent, 32f, 0f, new ButtonFunction("Swap", () =>
                {
                    foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                    {
                        beatmapObject.LDM = !beatmapObject.LDM;
                        Updater.UpdateObject(beatmapObject);
                    }
                }));

                EditorHelper.SetComplexity(labels, Complexity.Advanced);
                EditorHelper.SetComplexity(buttons1, Complexity.Advanced);
                EditorHelper.SetComplexity(buttons2, Complexity.Advanced);
            }

            GeneratePad(parent);
            GenerateLabels(parent, 32f, new LabelSettings("- Pasting -", 22, FontStyle.Bold, TextAnchor.MiddleCenter));

            // Paste Modifier
            {
                var labels = GenerateLabels(parent, 32f, "Paste Modifier to Selected");
                var buttons1 = GenerateButtons(parent, 32f, 8f,
                    new ButtonFunction("Paste", () =>
                    {
                        if (ObjectModifiersEditor.copiedModifier == null)
                        {
                            EditorManager.inst.DisplayNotification("Copy a modifier first!", 1.5f, EditorManager.NotificationType.Warning);
                            return;
                        }

                        foreach (var beatmapObject in ObjectEditor.inst.SelectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()))
                            beatmapObject.modifiers.Add(Modifier<BeatmapObject>.DeepCopy(ObjectModifiersEditor.copiedModifier, beatmapObject));

                        EditorManager.inst.DisplayNotification("Pasted Modifier!", 1.5f, EditorManager.NotificationType.Success);
                    }));

                EditorHelper.SetComplexity(labels, Complexity.Advanced);
                EditorHelper.SetComplexity(buttons1, Complexity.Advanced);
            }
            
            // Paste Keyframes
            {
                var labels = GenerateLabels(parent, 32f, "Paste Keyframes to Selected");
                var buttons1 = GenerateButtons(parent, 32f, 8f,
                    new ButtonFunction("Paste", EditorHelper.PasteKeyframes));

                EditorHelper.SetComplexity(labels, Complexity.Normal);
                EditorHelper.SetComplexity(buttons1, Complexity.Normal);
            }
            
            // Repeat Paste Keyframes
            {
                var labels = GenerateLabels(parent, 32f, "Repeat Paste Keyframes to Selected");

                var repeatCountInputField = GenerateInputField(parent, "repeat count", "1", "Enter count...", false, false);
                TriggerHelper.IncreaseDecreaseButtonsInt(repeatCountInputField);
                TriggerHelper.AddEventTriggers(repeatCountInputField.inputField.gameObject, TriggerHelper.ScrollDeltaInt(repeatCountInputField.inputField));
                var repeatOffsetTimeInputField = GenerateInputField(parent, "repeat offset time", "1", "Enter offset time...", false, false);
                TriggerHelper.IncreaseDecreaseButtons(repeatOffsetTimeInputField);
                TriggerHelper.AddEventTriggers(repeatOffsetTimeInputField.inputField.gameObject, TriggerHelper.ScrollDelta(repeatOffsetTimeInputField.inputField));

                var buttons1 = GenerateButtons(parent, 32f, 8f,
                    new ButtonFunction("Paste", () => { EditorHelper.RepeatPasteKeyframes(Parser.TryParse(repeatCountInputField.inputField.text, 0), Parser.TryParse(repeatOffsetTimeInputField.inputField.text, 1f)); }));

                EditorHelper.SetComplexity(repeatCountInputField.gameObject, Complexity.Advanced);
                EditorHelper.SetComplexity(repeatOffsetTimeInputField.gameObject, Complexity.Advanced);

                EditorHelper.SetComplexity(labels, Complexity.Advanced);
                EditorHelper.SetComplexity(buttons1, Complexity.Advanced);
            }

            GeneratePad(parent);

            // Sync object selection
            {
                var labels = GenerateLabels(parent, 32f, "Sync to specific object");

                var syncLayout = Creator.NewUIObject("sync layout", parent);
                syncLayout.transform.AsRT().sizeDelta = new Vector2(390f, 210f);
                var multiSyncGLG = syncLayout.AddComponent<GridLayoutGroup>();
                multiSyncGLG.spacing = new Vector2(4f, 4f);
                multiSyncGLG.cellSize = new Vector2(61.6f, 49f);

                GenerateButton(syncLayout.transform, new ButtonFunction("ST", eventData =>
                {
                    SyncObjectData("Start Time", eventData, (timelineObject, beatmapObject) =>
                    {
                        timelineObject.GetData<BeatmapObject>().StartTime = beatmapObject.StartTime;
                    }, true, true, "StartTime");
                })); // Start Time
                GenerateButton(syncLayout.transform, new ButtonFunction("N", eventData =>
                {
                    SyncObjectData("Name", eventData, (timelineObject, beatmapObject) =>
                    {
                        timelineObject.GetData<BeatmapObject>().name = beatmapObject.name;
                    }, true, false);
                })); // Name
                GenerateButton(syncLayout.transform, new ButtonFunction("OT", eventData =>
                {
                    SyncObjectData("Object Type", eventData, (timelineObject, beatmapObject) =>
                    {
                        timelineObject.GetData<BeatmapObject>().objectType = beatmapObject.objectType;
                    }, true, true, "ObjectType");
                })); // Object Type
                GenerateButton(syncLayout.transform, new ButtonFunction("AKT", eventData =>
                {
                    SyncObjectData("AutoKill Type", eventData, (timelineObject, beatmapObject) =>
                    {
                        timelineObject.GetData<BeatmapObject>().autoKillType = beatmapObject.autoKillType;
                    }, true, true, "AutoKill");
                })); // Autokill Type
                GenerateButton(syncLayout.transform, new ButtonFunction("AKO", eventData =>
                {
                    SyncObjectData("AutoKill Offset", eventData, (timelineObject, beatmapObject) =>
                    {
                        timelineObject.GetData<BeatmapObject>().autoKillOffset = beatmapObject.autoKillOffset;
                    }, true, true, "AutoKill");
                })); // Autokill Offset
                GenerateButton(syncLayout.transform, new ButtonFunction("P", eventData =>
                {
                    SyncObjectData("Parent", eventData, (TimelineObject currentSelection, BeatmapObject beatmapObjectToParentTo) =>
                    {
                        CoreHelper.SetParent(currentSelection, beatmapObjectToParentTo, recalculate: false, renderParent: false);
                    }, false, true, "Parent");
                })); // Parent
                GenerateButton(syncLayout.transform, new ButtonFunction("PD", eventData =>
                {
                    SyncObjectData("Parent Desync", eventData, (timelineObject, beatmapObject) =>
                    {
                        timelineObject.GetData<BeatmapObject>().desync = beatmapObject.desync;
                    }, false, true, "Parent");
                })); // Parent Desync
                GenerateButton(syncLayout.transform, new ButtonFunction("PT", eventData =>
                {
                    SyncObjectData("Parent Types", eventData, (timelineObject, beatmapObject) =>
                    {
                        timelineObject.GetData<BeatmapObject>().parentType = beatmapObject.parentType;
                    }, false, true, "ParentType");
                })); // Parent Type
                GenerateButton(syncLayout.transform, new ButtonFunction("PO", eventData =>
                {
                    SyncObjectData("Parent Offsets", eventData, (timelineObject, beatmapObject) =>
                    {
                        timelineObject.GetData<BeatmapObject>().parentOffsets = beatmapObject.parentOffsets.Clone();
                    }, false, true, "ParentOffset");
                })); // Parent Offset
                GenerateButton(syncLayout.transform, new ButtonFunction("PA", eventData =>
                {
                    SyncObjectData("Parent Additive", eventData, (timelineObject, beatmapObject) =>
                    {
                        timelineObject.GetData<BeatmapObject>().parentAdditive = beatmapObject.parentAdditive;
                    }, false, true, "ParentOffset");
                })); // Parent Additive
                GenerateButton(syncLayout.transform, new ButtonFunction("PP", eventData =>
                {
                    SyncObjectData("Parent Parallax", eventData, (timelineObject, beatmapObject) =>
                    {
                        timelineObject.GetData<BeatmapObject>().parallaxSettings = beatmapObject.parallaxSettings.Copy();
                    }, false, true, "ParentOffset");
                })); // Parent Parallax
                GenerateButton(syncLayout.transform, new ButtonFunction("O", eventData =>
                {
                    SyncObjectData("Origin", eventData, (timelineObject, beatmapObject) =>
                    {
                        timelineObject.GetData<BeatmapObject>().origin = beatmapObject.origin;
                    }, false, true, "Origin");
                })); // Origin
                GenerateButton(syncLayout.transform, new ButtonFunction("S", eventData =>
                {
                    SyncObjectData("Shape", eventData, (timelineObject, beatmapObject) =>
                    {
                        timelineObject.GetData<BeatmapObject>().shape = beatmapObject.shape;
                        timelineObject.GetData<BeatmapObject>().shapeOption = beatmapObject.shapeOption;
                    }, false, true, "Shape");
                })); // Shape
                GenerateButton(syncLayout.transform, new ButtonFunction("T", eventData =>
                {
                    SyncObjectData("Text", eventData, (timelineObject, beatmapObject) =>
                    {
                        timelineObject.GetData<BeatmapObject>().text = beatmapObject.text;
                    }, false, true, "Text");
                })); // Text
                GenerateButton(syncLayout.transform, new ButtonFunction("D", eventData =>
                {
                    SyncObjectData("Depth", eventData, (timelineObject, beatmapObject) =>
                    {
                        timelineObject.GetData<BeatmapObject>().Depth = beatmapObject.Depth;
                    }, false, true, "Depth");
                })); // Depth
                GenerateButton(syncLayout.transform, new ButtonFunction("KF", eventData =>
                {
                    SyncObjectData("Keyframes", eventData, (timelineObject, beatmapObject) =>
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();

                        for (int i = 0; i < bm.events.Count; i++)
                        {
                            bm.events[i].Clear();
                            for (int j = 0; j < beatmapObject.events[i].Count; j++)
                                bm.events[i].Add(EventKeyframe.DeepCopy((EventKeyframe)beatmapObject.events[i][j]));
                        }

                    }, true, true, "Keyframes");
                })); // Keyframes
                GenerateButton(syncLayout.transform, new ButtonFunction("MOD", eventData =>
                {
                    SyncObjectData("Modifiers", eventData, (timelineObject, beatmapObject) =>
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();

                        bm.modifiers.AddRange(beatmapObject.modifiers.Select(x => Modifier<BeatmapObject>.DeepCopy(x, bm)));
                    }, false, true);
                })); // Modifiers
                GenerateButton(syncLayout.transform, new ButtonFunction("IGN", eventData =>
                {
                    SyncObjectData("Ignore Lifespan", eventData, (timelineObject, beatmapObject) =>
                    {
                        timelineObject.GetData<BeatmapObject>().ignoreLifespan = beatmapObject.ignoreLifespan;
                    }, false, false);
                })); // Ignore lifespan
                GenerateButton(syncLayout.transform, new ButtonFunction("TAG", eventData =>
                {
                    SyncObjectData("Tags", eventData, (timelineObject, beatmapObject) =>
                    {
                        timelineObject.GetData<BeatmapObject>().tags = beatmapObject.tags.Clone();
                    }, false, false);
                })); // Tags
                GenerateButton(syncLayout.transform, new ButtonFunction("RT", eventData =>
                {
                    SyncObjectData("Render Type", eventData, (timelineObject, beatmapObject) =>
                    {
                        timelineObject.GetData<BeatmapObject>().background = beatmapObject.background;
                    }, false, true);
                })); // Render Type
                GenerateButton(syncLayout.transform, new ButtonFunction("PR", eventData =>
                {
                    SyncObjectData("Prefab Reference", eventData, (timelineObject, beatmapObject) =>
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();
                        bm.prefabID = beatmapObject.prefabID;
                        bm.prefabInstanceID = beatmapObject.prefabInstanceID;
                    }, true, false);
                })); // Prefab

                EditorHelper.SetComplexity(labels, Complexity.Advanced);
                EditorHelper.SetComplexity(syncLayout, Complexity.Advanced);
            }

            GeneratePad(parent, Complexity.Advanced);

            var replaceLabels = GenerateLabels(parent, 32f, new LabelSettings("- Replace strings -", 22, FontStyle.Bold, TextAnchor.MiddleCenter));
            EditorHelper.SetComplexity(replaceLabels, Complexity.Advanced);

            // Replace Name
            {
                var labels = GenerateLabels(parent, 32f, "Replace Name");

                var replaceName = Creator.NewUIObject("replace name", parent);
                replaceName.transform.AsRT().sizeDelta = new Vector2(390f, 32f);
                var multiSyncGLG = replaceName.AddComponent<GridLayoutGroup>();
                multiSyncGLG.spacing = new Vector2(8f, 8f);
                multiSyncGLG.cellSize = new Vector2(124f, 32f);

                var oldName = defaultIF.Duplicate(replaceName.transform, "old name");

                Destroy(oldName.GetComponent<EventTrigger>());
                var oldNameIF = oldName.GetComponent<InputField>();
                oldNameIF.characterValidation = InputField.CharacterValidation.None;
                oldNameIF.textComponent.alignment = TextAnchor.MiddleLeft;
                oldNameIF.textComponent.fontSize = 16;
                oldNameIF.text = "Old Name";
                oldNameIF.GetPlaceholderText().text = "Enter old name...";
                oldNameIF.GetPlaceholderText().alignment = TextAnchor.MiddleLeft;
                oldNameIF.GetPlaceholderText().fontSize = 16;
                oldNameIF.GetPlaceholderText().color = new Color(0f, 0f, 0f, 0.3f);

                oldNameIF.onValueChanged.ClearAll();

                EditorHelper.AddInputFieldContextMenu(oldNameIF);
                var oldNameSwapper = oldName.AddComponent<InputFieldSwapper>();
                oldNameSwapper.Init(oldNameIF, InputFieldSwapper.Type.String);

                EditorThemeManager.AddInputField(oldNameIF);

                var newName = defaultIF.Duplicate(replaceName.transform, "new name");

                Destroy(newName.GetComponent<EventTrigger>());
                var newNameIF = newName.GetComponent<InputField>();
                newNameIF.characterValidation = InputField.CharacterValidation.None;
                newNameIF.textComponent.alignment = TextAnchor.MiddleLeft;
                newNameIF.textComponent.fontSize = 16;
                newNameIF.text = "New Name";
                newNameIF.GetPlaceholderText().text = "Enter new name...";
                newNameIF.GetPlaceholderText().alignment = TextAnchor.MiddleLeft;
                newNameIF.GetPlaceholderText().fontSize = 16;
                newNameIF.GetPlaceholderText().color = new Color(0f, 0f, 0f, 0.3f);

                newNameIF.onValueChanged.ClearAll();

                EditorHelper.AddInputFieldContextMenu(newNameIF);
                var newNameSwapper = newName.AddComponent<InputFieldSwapper>();
                newNameSwapper.Init(newNameIF, InputFieldSwapper.Type.String);

                EditorThemeManager.AddInputField(newNameIF);

                var replace = eventButton.Duplicate(replaceName.transform, "replace");
                replace.transform.localScale = Vector3.one;
                replace.transform.AsRT().sizeDelta = new Vector2(66f, 32f);
                replace.GetComponent<LayoutElement>().minWidth = 32f;

                var replaceText = replace.transform.GetChild(0).GetComponent<Text>();

                replaceText.text = "Replace";

                EditorThemeManager.AddGraphic(replace.GetComponent<Image>(), ThemeGroup.Function_1, true);
                EditorThemeManager.AddGraphic(replaceText, ThemeGroup.Function_1_Text);

                var button = replace.GetComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(() =>
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();
                        bm.name = bm.name.Replace(oldNameIF.text, newNameIF.text);
                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                    }
                });

                EditorHelper.SetComplexity(labels, Complexity.Advanced);
                EditorHelper.SetComplexity(replaceName, Complexity.Advanced);
            }

            // Replace Tags
            {
                var labels = GenerateLabels(parent, 32f, "Replace Tags");

                var replaceName = Creator.NewUIObject("replace tags", parent);
                replaceName.transform.AsRT().sizeDelta = new Vector2(390f, 32f);
                var multiSyncGLG = replaceName.AddComponent<GridLayoutGroup>();
                multiSyncGLG.spacing = new Vector2(8f, 8f);
                multiSyncGLG.cellSize = new Vector2(124f, 32f);

                var oldName = defaultIF.Duplicate(replaceName.transform, "old tag");

                Destroy(oldName.GetComponent<EventTrigger>());
                var oldNameIF = oldName.GetComponent<InputField>();
                oldNameIF.characterValidation = InputField.CharacterValidation.None;
                oldNameIF.textComponent.alignment = TextAnchor.MiddleLeft;
                oldNameIF.textComponent.fontSize = 16;
                oldNameIF.text = "Old Tag";
                oldNameIF.GetPlaceholderText().text = "Enter old tag...";
                oldNameIF.GetPlaceholderText().alignment = TextAnchor.MiddleLeft;
                oldNameIF.GetPlaceholderText().fontSize = 16;
                oldNameIF.GetPlaceholderText().color = new Color(0f, 0f, 0f, 0.3f);

                oldNameIF.onValueChanged.ClearAll();

                EditorHelper.AddInputFieldContextMenu(oldNameIF);
                var oldNameSwapper = oldName.AddComponent<InputFieldSwapper>();
                oldNameSwapper.Init(oldNameIF, InputFieldSwapper.Type.String);

                EditorThemeManager.AddInputField(oldNameIF);

                var newName = defaultIF.Duplicate(replaceName.transform, "new tag");

                Destroy(newName.GetComponent<EventTrigger>());
                var newNameIF = newName.GetComponent<InputField>();
                newNameIF.characterValidation = InputField.CharacterValidation.None;
                newNameIF.textComponent.alignment = TextAnchor.MiddleLeft;
                newNameIF.textComponent.fontSize = 16;
                newNameIF.text = "New Tag";
                newNameIF.GetPlaceholderText().text = "Enter new tag...";
                newNameIF.GetPlaceholderText().alignment = TextAnchor.MiddleLeft;
                newNameIF.GetPlaceholderText().fontSize = 16;
                newNameIF.GetPlaceholderText().color = new Color(0f, 0f, 0f, 0.3f);

                newNameIF.onValueChanged.ClearAll();

                EditorHelper.AddInputFieldContextMenu(newNameIF);
                var newNameSwapper = newName.AddComponent<InputFieldSwapper>();
                newNameSwapper.Init(newNameIF, InputFieldSwapper.Type.String);

                EditorThemeManager.AddInputField(newNameIF);

                var replace = eventButton.Duplicate(replaceName.transform, "replace");
                replace.transform.localScale = Vector3.one;
                replace.transform.AsRT().sizeDelta = new Vector2(66f, 32f);
                replace.GetComponent<LayoutElement>().minWidth = 32f;

                var replaceText = replace.transform.GetChild(0).GetComponent<Text>();

                replaceText.text = "Replace";

                EditorThemeManager.AddGraphic(replace.GetComponent<Image>(), ThemeGroup.Function_1, true);
                EditorThemeManager.AddGraphic(replaceText, ThemeGroup.Function_1_Text);

                var button = replace.GetComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(() =>
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();
                        for (int i = 0; i < bm.tags.Count; i++)
                        {
                            bm.tags[i] = bm.tags[i].Replace(oldNameIF.text, newNameIF.text);
                        }
                    }
                });

                EditorHelper.SetComplexity(labels, Complexity.Advanced);
                EditorHelper.SetComplexity(replaceName, Complexity.Advanced);
            }

            // Replace Text
            {
                var labels = GenerateLabels(parent, 32f, "Replace Text");
                
                var replaceName = Creator.NewUIObject("replace text", parent);
                replaceName.transform.AsRT().sizeDelta = new Vector2(390f, 32f);
                var multiSyncGLG = replaceName.AddComponent<GridLayoutGroup>();
                multiSyncGLG.spacing = new Vector2(8f, 8f);
                multiSyncGLG.cellSize = new Vector2(124f, 32f);

                var oldName = defaultIF.Duplicate(replaceName.transform, "old text");

                Destroy(oldName.GetComponent<EventTrigger>());
                var oldNameIF = oldName.GetComponent<InputField>();
                oldNameIF.characterValidation = InputField.CharacterValidation.None;
                oldNameIF.textComponent.alignment = TextAnchor.MiddleLeft;
                oldNameIF.textComponent.fontSize = 16;
                oldNameIF.text = "Old Text";
                oldNameIF.GetPlaceholderText().text = "Enter old text...";
                oldNameIF.GetPlaceholderText().alignment = TextAnchor.MiddleLeft;
                oldNameIF.GetPlaceholderText().fontSize = 16;
                oldNameIF.GetPlaceholderText().color = new Color(0f, 0f, 0f, 0.3f);

                oldNameIF.onValueChanged.ClearAll();

                EditorHelper.AddInputFieldContextMenu(oldNameIF);
                var oldNameSwapper = oldName.AddComponent<InputFieldSwapper>();
                oldNameSwapper.Init(oldNameIF, InputFieldSwapper.Type.String);

                EditorThemeManager.AddInputField(oldNameIF);

                var newName = defaultIF.Duplicate(replaceName.transform, "new text");

                Destroy(newName.GetComponent<EventTrigger>());
                var newNameIF = newName.GetComponent<InputField>();
                newNameIF.characterValidation = InputField.CharacterValidation.None;
                newNameIF.textComponent.alignment = TextAnchor.MiddleLeft;
                newNameIF.textComponent.fontSize = 16;
                newNameIF.text = "New Text";
                newNameIF.GetPlaceholderText().text = "Enter new text...";
                newNameIF.GetPlaceholderText().alignment = TextAnchor.MiddleLeft;
                newNameIF.GetPlaceholderText().fontSize = 16;
                newNameIF.GetPlaceholderText().color = new Color(0f, 0f, 0f, 0.3f);

                newNameIF.onValueChanged.ClearAll();

                EditorHelper.AddInputFieldContextMenu(newNameIF);
                var newNameSwapper = newName.AddComponent<InputFieldSwapper>();
                newNameSwapper.Init(newNameIF, InputFieldSwapper.Type.String);

                EditorThemeManager.AddInputField(newNameIF);

                var replace = eventButton.Duplicate(replaceName.transform, "replace");
                replace.transform.localScale = Vector3.one;
                replace.transform.AsRT().sizeDelta = new Vector2(66f, 32f);
                replace.GetComponent<LayoutElement>().minWidth = 32f;

                var replaceText = replace.transform.GetChild(0).GetComponent<Text>();

                replaceText.text = "Replace";

                EditorThemeManager.AddGraphic(replace.GetComponent<Image>(), ThemeGroup.Function_1, true);
                EditorThemeManager.AddGraphic(replaceText, ThemeGroup.Function_1_Text);

                var button = replace.GetComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(() =>
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();
                        bm.text = bm.text.Replace(oldNameIF.text, newNameIF.text);
                        Updater.UpdateObject(bm, "Shape");
                    }
                });

                EditorHelper.SetComplexity(labels, Complexity.Advanced);
                EditorHelper.SetComplexity(replaceName, Complexity.Advanced);
            }

            // Replace Modifier
            {
                var labels = GenerateLabels(parent, 32f, "Replace Modifier values");

                var replaceName = Creator.NewUIObject("replace modifier", parent);
                replaceName.transform.AsRT().sizeDelta = new Vector2(390f, 32f);
                var multiSyncGLG = replaceName.AddComponent<GridLayoutGroup>();
                multiSyncGLG.spacing = new Vector2(8f, 8f);
                multiSyncGLG.cellSize = new Vector2(124f, 32f);

                var oldName = defaultIF.Duplicate(replaceName.transform, "old modifier");

                Destroy(oldName.GetComponent<EventTrigger>());
                var oldNameIF = oldName.GetComponent<InputField>();
                oldNameIF.characterValidation = InputField.CharacterValidation.None;
                oldNameIF.textComponent.alignment = TextAnchor.MiddleLeft;
                oldNameIF.textComponent.fontSize = 16;
                oldNameIF.text = "Old Modifier";
                oldNameIF.GetPlaceholderText().text = "Enter old modifier...";
                oldNameIF.GetPlaceholderText().alignment = TextAnchor.MiddleLeft;
                oldNameIF.GetPlaceholderText().fontSize = 16;
                oldNameIF.GetPlaceholderText().color = new Color(0f, 0f, 0f, 0.3f);

                oldNameIF.onValueChanged.ClearAll();

                EditorHelper.AddInputFieldContextMenu(oldNameIF);
                var oldNameSwapper = oldName.AddComponent<InputFieldSwapper>();
                oldNameSwapper.Init(oldNameIF, InputFieldSwapper.Type.String);

                EditorThemeManager.AddInputField(oldNameIF);

                var newName = defaultIF.Duplicate(replaceName.transform, "new modifier");

                Destroy(newName.GetComponent<EventTrigger>());
                var newNameIF = newName.GetComponent<InputField>();
                newNameIF.characterValidation = InputField.CharacterValidation.None;
                newNameIF.textComponent.alignment = TextAnchor.MiddleLeft;
                newNameIF.textComponent.fontSize = 16;
                newNameIF.text = "New Modifier";
                newNameIF.GetPlaceholderText().text = "Enter new modifier...";
                newNameIF.GetPlaceholderText().alignment = TextAnchor.MiddleLeft;
                newNameIF.GetPlaceholderText().fontSize = 16;
                newNameIF.GetPlaceholderText().color = new Color(0f, 0f, 0f, 0.3f);

                newNameIF.onValueChanged.ClearAll();

                EditorHelper.AddInputFieldContextMenu(newNameIF);
                var newNameSwapper = newName.AddComponent<InputFieldSwapper>();
                newNameSwapper.Init(newNameIF, InputFieldSwapper.Type.String);

                EditorThemeManager.AddInputField(newNameIF);

                var replace = eventButton.Duplicate(replaceName.transform, "replace");
                replace.transform.localScale = Vector3.one;
                replace.transform.AsRT().sizeDelta = new Vector2(66f, 32f);
                replace.GetComponent<LayoutElement>().minWidth = 32f;

                var replaceText = replace.transform.GetChild(0).GetComponent<Text>();

                replaceText.text = "Replace";

                EditorThemeManager.AddGraphic(replace.GetComponent<Image>(), ThemeGroup.Function_1, true);
                EditorThemeManager.AddGraphic(replaceText, ThemeGroup.Function_1_Text);

                var button = replace.GetComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(() =>
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();

                        foreach (var modifier in bm.modifiers)
                        {
                            for (int i = 1; i < modifier.commands.Count; i++)
                            {
                                modifier.commands[i] = modifier.commands[i].Replace(oldNameIF.text, newNameIF.text);
                            }

                            modifier.value = modifier.value.Replace(oldNameIF.text, newNameIF.text);
                        }
                    }
                });

                EditorHelper.SetComplexity(labels, Complexity.Advanced);
                EditorHelper.SetComplexity(replaceName, Complexity.Advanced);
            }

            GeneratePad(parent);

            // Assign Colors
            {
                var labels1 = GenerateLabels(parent, 32f, new LabelSettings("- Assign colors -", 22, FontStyle.Bold, TextAnchor.MiddleCenter));

                var labelsColor = GenerateLabels(parent, 32f, "Primary Color");

                var disable = EditorPrefabHolder.Instance.Function2Button.Duplicate(parent, "disable color");
                var disableX = EditorManager.inst.colorGUI.transform.Find("Image").gameObject.Duplicate(disable.transform, "x");
                var disableXImage = disableX.GetComponent<Image>();
                disableXImage.sprite = CloseSprite;
                RectValues.Default.AnchoredPosition(-170f, 0f).SizeDelta(32f, 32f).AssignToRectTransform(disableXImage.rectTransform);
                var disableButtonStorage = disable.GetComponent<FunctionButtonStorage>();
                disableButtonStorage.button.onClick.ClearAll();
                disableButtonStorage.button.onClick.AddListener(() =>
                {
                    disableX.gameObject.SetActive(true);
                    currentMultiColorSelection = -1;
                    UpdateMultiColorButtons();
                });
                disableButtonStorage.text.text = "Don't set color";
                EditorThemeManager.AddGraphic(disableXImage, ThemeGroup.Function_2_Text);
                EditorThemeManager.AddGraphic(disableButtonStorage.text, ThemeGroup.Function_2_Text);
                EditorThemeManager.AddSelectable(disableButtonStorage.button, ThemeGroup.Function_2);

                var colorLayout = Creator.NewUIObject("color layout", parent);
                colorLayout.transform.AsRT().sizeDelta = new Vector2(390f, 76f);
                var colorLayoutGLG = colorLayout.AddComponent<GridLayoutGroup>();
                colorLayoutGLG.spacing = new Vector2(4f, 4f);
                colorLayoutGLG.cellSize = new Vector2(36f, 36f);

                for (int i = 0; i < 18; i++)
                {
                    var index = i;
                    var colorGUI = EditorManager.inst.colorGUI.Duplicate(colorLayout.transform, (i + 1).ToString());
                    var assigner = colorGUI.AddComponent<AssignToTheme>();
                    assigner.Index = i;
                    var image = colorGUI.GetComponent<Image>();
                    assigner.Graphic = image;

                    var selected = colorGUI.transform.GetChild(0).gameObject;
                    selected.SetActive(false);

                    var button = colorGUI.GetComponent<Button>();
                    button.onClick.ClearAll();
                    button.onClick.AddListener(() =>
                    {
                        disableX.gameObject.SetActive(false);
                        currentMultiColorSelection = index;
                        UpdateMultiColorButtons();
                    });

                    multiColorButtons.Add(new MultiColorButton
                    {
                        Button = button,
                        Image = image,
                        Selected = selected
                    });
                }

                var labels2 = GenerateLabels(parent, 32f, "Primary Opacity");

                var opacityIF = CreateInputField("opacity", "", "Enter value... (Keep empty to not set)", parent, isInteger: false);
                ((Text)opacityIF.placeholder).fontSize = 13;

                var labels3 = GenerateLabels(parent, 32f, "Primary Hue");

                var hueIF = CreateInputField("hue", "", "Enter value... (Keep empty to not set)", parent, isInteger: false);
                ((Text)hueIF.placeholder).fontSize = 13;

                var labels4 = GenerateLabels(parent, 32f, "Primary Saturation");

                var satIF = CreateInputField("sat", "", "Enter value... (Keep empty to not set)", parent, isInteger: false);
                ((Text)satIF.placeholder).fontSize = 13;

                var labels5 = GenerateLabels(parent, 32f, "Primary Value (Brightness)");

                var valIF = CreateInputField("val", "", "Enter value... (Keep empty to not set)", parent, isInteger: false);
                ((Text)valIF.placeholder).fontSize = 13;

                var labelsSecondaryColor = GenerateLabels(parent, 32f, "Secondary Color");

                var disableGradient = EditorPrefabHolder.Instance.Function2Button.Duplicate(parent, "disable color");
                var disableGradientX = EditorManager.inst.colorGUI.transform.Find("Image").gameObject.Duplicate(disableGradient.transform, "x");
                var disableGradientXImage = disableGradientX.GetComponent<Image>();
                disableGradientXImage.sprite = CloseSprite;
                RectValues.Default.AnchoredPosition(-170f, 0f).SizeDelta(32f, 32f).AssignToRectTransform(disableGradientXImage.rectTransform);
                var disableGradientButtonStorage = disableGradient.GetComponent<FunctionButtonStorage>();
                disableGradientButtonStorage.button.onClick.ClearAll();
                disableGradientButtonStorage.button.onClick.AddListener(() =>
                {
                    disableGradientX.gameObject.SetActive(true);
                    currentMultiGradientColorSelection = -1;
                    UpdateMultiColorButtons();
                });
                disableGradientButtonStorage.text.text = "Don't set color";
                EditorThemeManager.AddGraphic(disableGradientXImage, ThemeGroup.Function_2_Text);
                EditorThemeManager.AddGraphic(disableGradientButtonStorage.text, ThemeGroup.Function_2_Text);
                EditorThemeManager.AddSelectable(disableGradientButtonStorage.button, ThemeGroup.Function_2);

                var colorGradientLayout = Creator.NewUIObject("color layout", parent);
                colorGradientLayout.transform.AsRT().sizeDelta = new Vector2(390f, 76f);
                var colorGradientLayoutGLG = colorGradientLayout.AddComponent<GridLayoutGroup>();
                colorGradientLayoutGLG.spacing = new Vector2(4f, 4f);
                colorGradientLayoutGLG.cellSize = new Vector2(36f, 36f);

                for (int i = 0; i < 18; i++)
                {
                    var index = i;
                    var colorGUI = EditorManager.inst.colorGUI.Duplicate(colorGradientLayout.transform, (i + 1).ToString());
                    var assigner = colorGUI.AddComponent<AssignToTheme>();
                    assigner.Index = i;
                    var image = colorGUI.GetComponent<Image>();
                    assigner.Graphic = image;

                    var selected = colorGUI.transform.GetChild(0).gameObject;
                    selected.SetActive(false);

                    var button = colorGUI.GetComponent<Button>();
                    button.onClick.ClearAll();
                    button.onClick.AddListener(() =>
                    {
                        disableGradientX.gameObject.SetActive(false);
                        currentMultiGradientColorSelection = index;
                        UpdateMultiColorButtons();
                    });

                    multiGradientColorButtons.Add(new MultiColorButton
                    {
                        Button = button,
                        Image = image,
                        Selected = selected
                    });
                }

                var labels6 = GenerateLabels(parent, 32f, "Secondary Opacity");

                var opacityGradientIF = CreateInputField("opacity", "", "Enter value... (Keep empty to not set)", parent, isInteger: false);
                ((Text)opacityGradientIF.placeholder).fontSize = 13;

                var labels7 = GenerateLabels(parent, 32f, "Secondary Hue");

                var hueGradientIF = CreateInputField("hue", "", "Enter value... (Keep empty to not set)", parent, isInteger: false);
                ((Text)hueGradientIF.placeholder).fontSize = 13;

                var labels8 = GenerateLabels(parent, 32f, "Secondary Saturation");

                var satGradientIF = CreateInputField("sat", "", "Enter value... (Keep empty to not set)", parent, isInteger: false);
                ((Text)satGradientIF.placeholder).fontSize = 13;

                var labels9 = GenerateLabels(parent, 32f, "Secondary Value (Brightness)");

                var valGradientIF = CreateInputField("val", "", "Enter value... (Keep empty to not set)", parent, isInteger: false);
                ((Text)valGradientIF.placeholder).fontSize = 13;

                var labels10 = GenerateLabels(parent, 32f, "Ease Type");

                var curvesObject = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/position/curves").Duplicate(parent, "curves");
                var curves = curvesObject.GetComponent<Dropdown>();
                curves.onValueChanged.ClearAll();
                curves.options.Insert(0, new Dropdown.OptionData("None (Doesn't Set Easing)"));

                TriggerHelper.AddEventTriggers(curves.gameObject, TriggerHelper.CreateEntry(EventTriggerType.Scroll, baseEventData =>
                {
                    if (!EditorConfig.Instance.ScrollOnEasing.Value)
                        return;

                    var pointerEventData = (PointerEventData)baseEventData;
                    if (pointerEventData.scrollDelta.y > 0f)
                        curves.value = curves.value == 0 ? curves.options.Count - 1 : curves.value - 1;
                    if (pointerEventData.scrollDelta.y < 0f)
                        curves.value = curves.value == curves.options.Count - 1 ? 0 : curves.value + 1;
                }));

                EditorThemeManager.AddDropdown(curves);

                // Assign to All
                {
                    var labels = GenerateLabels(parent, 32f, "Assign to all Color Keyframes");
                    var buttons1 = GenerateButtons(parent, 32f, 8f,
                        new ButtonFunction("Set", () =>
                        {
                            DataManager.LSAnimation anim = default;
                            bool setCurve = curves.value != 0 && DataManager.inst.AnimationListDictionary.TryGetValue(curves.value - 1, out anim);
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();

                                for (int i = 0; i < bm.events[3].Count; i++)
                                {
                                    var kf = bm.events[3][i];
                                    if (setCurve)
                                        kf.curveType = anim;
                                    if (currentMultiColorSelection >= 0)
                                        kf.eventValues[0] = Mathf.Clamp(currentMultiColorSelection, 0, 18);
                                    if (!string.IsNullOrEmpty(opacityIF.text))
                                        kf.eventValues[1] = -Mathf.Clamp(Parser.TryParse(opacityIF.text, 1f), 0f, 1f) + 1f;
                                    if (!string.IsNullOrEmpty(hueIF.text))
                                        kf.eventValues[2] = Parser.TryParse(hueIF.text, 0f);
                                    if (!string.IsNullOrEmpty(satIF.text))
                                        kf.eventValues[3] = Parser.TryParse(satIF.text, 0f);
                                    if (!string.IsNullOrEmpty(valIF.text))
                                        kf.eventValues[4] = Parser.TryParse(valIF.text, 0f);

                                    // Gradient
                                    if (currentMultiGradientColorSelection >= 0)
                                        kf.eventValues[5] = Mathf.Clamp(currentMultiGradientColorSelection, 0, 18);
                                    if (!string.IsNullOrEmpty(opacityGradientIF.text))
                                        kf.eventValues[6] = -Mathf.Clamp(Parser.TryParse(opacityGradientIF.text, 1f), 0f, 1f) + 1f;
                                    if (!string.IsNullOrEmpty(hueGradientIF.text))
                                        kf.eventValues[7] = Parser.TryParse(hueGradientIF.text, 0f);
                                    if (!string.IsNullOrEmpty(satGradientIF.text))
                                        kf.eventValues[8] = Parser.TryParse(satGradientIF.text, 0f);
                                    if (!string.IsNullOrEmpty(valGradientIF.text))
                                        kf.eventValues[9] = Parser.TryParse(valGradientIF.text, 0f);
                                }

                                Updater.UpdateObject(bm, "Keyframes");
                            }
                        }),
                        new ButtonFunction("Add", () =>
                        {
                            DataManager.LSAnimation anim = default;
                            bool setCurve = curves.value != 0 && DataManager.inst.AnimationListDictionary.TryGetValue(curves.value - 1, out anim);
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();

                                for (int i = 0; i < bm.events[3].Count; i++)
                                {
                                    var kf = bm.events[3][i];
                                    if (setCurve)
                                        kf.curveType = anim;
                                    if (currentMultiColorSelection >= 0)
                                        kf.eventValues[0] = Mathf.Clamp(currentMultiColorSelection, 0, 18); // color slots can't be added onto.
                                    if (!string.IsNullOrEmpty(opacityIF.text))
                                        kf.eventValues[1] = Mathf.Clamp(kf.eventValues[1] - Parser.TryParse(opacityIF.text, 1f), 0f, 1f);
                                    if (!string.IsNullOrEmpty(hueIF.text))
                                        kf.eventValues[2] += Parser.TryParse(hueIF.text, 0f);
                                    if (!string.IsNullOrEmpty(satIF.text))
                                        kf.eventValues[3] += Parser.TryParse(satIF.text, 0f);
                                    if (!string.IsNullOrEmpty(valIF.text))
                                        kf.eventValues[4] += Parser.TryParse(valIF.text, 0f);

                                    // Gradient
                                    if (currentMultiGradientColorSelection >= 0)
                                        kf.eventValues[5] = Mathf.Clamp(currentMultiGradientColorSelection, 0, 18); // color slots can't be added onto.
                                    if (!string.IsNullOrEmpty(opacityGradientIF.text))
                                        kf.eventValues[6] = Mathf.Clamp(kf.eventValues[6] - Parser.TryParse(opacityGradientIF.text, 1f), 0f, 1f);
                                    if (!string.IsNullOrEmpty(hueGradientIF.text))
                                        kf.eventValues[7] += Parser.TryParse(hueGradientIF.text, 0f);
                                    if (!string.IsNullOrEmpty(satGradientIF.text))
                                        kf.eventValues[8] += Parser.TryParse(satGradientIF.text, 0f);
                                    if (!string.IsNullOrEmpty(valGradientIF.text))
                                        kf.eventValues[9] += Parser.TryParse(valGradientIF.text, 0f);
                                }

                                Updater.UpdateObject(bm, "Keyframes");
                            }
                        }),
                        new ButtonFunction("Sub", () =>
                        {
                            DataManager.LSAnimation anim = default;
                            bool setCurve = curves.value != 0 && DataManager.inst.AnimationListDictionary.TryGetValue(curves.value - 1, out anim);
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();

                                for (int i = 0; i < bm.events[3].Count; i++)
                                {
                                    var kf = bm.events[3][i];
                                    if (setCurve)
                                        kf.curveType = anim;
                                    if (currentMultiColorSelection >= 0)
                                        kf.eventValues[0] = Mathf.Clamp(currentMultiColorSelection, 0, 18); // color slots can't be added onto.
                                    if (!string.IsNullOrEmpty(opacityIF.text))
                                        kf.eventValues[1] = Mathf.Clamp(kf.eventValues[1] + Parser.TryParse(opacityIF.text, 1f), 0f, 1f);
                                    if (!string.IsNullOrEmpty(hueIF.text))
                                        kf.eventValues[2] -= Parser.TryParse(hueIF.text, 0f);
                                    if (!string.IsNullOrEmpty(satIF.text))
                                        kf.eventValues[3] -= Parser.TryParse(satIF.text, 0f);
                                    if (!string.IsNullOrEmpty(valIF.text))
                                        kf.eventValues[4] -= Parser.TryParse(valIF.text, 0f);

                                    // Gradient
                                    if (currentMultiGradientColorSelection >= 0)
                                        kf.eventValues[5] = Mathf.Clamp(currentMultiGradientColorSelection, 0, 18); // color slots can't be added onto.
                                    if (!string.IsNullOrEmpty(opacityGradientIF.text))
                                        kf.eventValues[6] = Mathf.Clamp(kf.eventValues[6] + Parser.TryParse(opacityGradientIF.text, 1f), 0f, 1f);
                                    if (!string.IsNullOrEmpty(hueGradientIF.text))
                                        kf.eventValues[7] -= Parser.TryParse(hueGradientIF.text, 0f);
                                    if (!string.IsNullOrEmpty(satGradientIF.text))
                                        kf.eventValues[8] -= Parser.TryParse(satGradientIF.text, 0f);
                                    if (!string.IsNullOrEmpty(valGradientIF.text))
                                        kf.eventValues[9] -= Parser.TryParse(valGradientIF.text, 0f);
                                }

                                Updater.UpdateObject(bm, "Keyframes");
                            }
                        }));

                    EditorHelper.SetComplexity(labels, Complexity.Advanced);
                    EditorHelper.SetComplexity(buttons1, Complexity.Advanced);
                }

                // Assign to Index
                {
                    var labels = GenerateLabels(parent, 32f, "Assign to Index");

                    var assignIndex = CreateInputField("index", "0", "Enter index...", parent, maxValue: int.MaxValue);
                    var buttons1 = GenerateButtons(parent, 32f, 8f,
                        new ButtonFunction("Set", () =>
                        {
                            if (assignIndex.text.Contains(","))
                            {
                                var split = assignIndex.text.Split(',');

                                for (int i = 0; i < split.Length; i++)
                                {
                                    var text = split[i];
                                    if (!int.TryParse(text, out int a))
                                        continue;

                                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                                    {
                                        var bm = timelineObject.GetData<BeatmapObject>();

                                        SetKeyframeValues((EventKeyframe)bm.events[3][Mathf.Clamp(a, 0, bm.events[3].Count - 1)], curves,
                                            opacityIF.text, hueIF.text, satIF.text, valIF.text, opacityGradientIF.text, hueGradientIF.text, satGradientIF.text, valGradientIF.text);

                                        Updater.UpdateObject(bm, "Keyframes");
                                    }
                                }

                                return;
                            }

                            if (!int.TryParse(assignIndex.text, out int num))
                                return;
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();

                                SetKeyframeValues((EventKeyframe)bm.events[3][Mathf.Clamp(num, 0, bm.events[3].Count - 1)], curves,
                                    opacityIF.text, hueIF.text, satIF.text, valIF.text, opacityGradientIF.text, hueGradientIF.text, satGradientIF.text, valGradientIF.text);

                                Updater.UpdateObject(bm, "Keyframes");
                            }
                        }),
                        new ButtonFunction("Add", () =>
                        {
                            if (assignIndex.text.Contains(","))
                            {
                                var split = assignIndex.text.Split(',');

                                for (int i = 0; i < split.Length; i++)
                                {
                                    var text = split[i];
                                    if (!int.TryParse(text, out int a))
                                        return;

                                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                                    {
                                        var bm = timelineObject.GetData<BeatmapObject>();

                                        AddKeyframeValues((EventKeyframe)bm.events[3][Mathf.Clamp(a, 0, bm.events[3].Count - 1)], curves,
                                            opacityIF.text, hueIF.text, satIF.text, valIF.text, opacityGradientIF.text, hueGradientIF.text, satGradientIF.text, valGradientIF.text);

                                        Updater.UpdateObject(bm, "Keyframes");
                                    }
                                }

                                return;
                            }

                            if (!int.TryParse(assignIndex.text, out int num))
                                return;
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();

                                AddKeyframeValues((EventKeyframe)bm.events[3][Mathf.Clamp(num, 0, bm.events[3].Count - 1)], curves,
                                    opacityIF.text, hueIF.text, satIF.text, valIF.text, opacityGradientIF.text, hueGradientIF.text, satGradientIF.text, valGradientIF.text);

                                Updater.UpdateObject(bm, "Keyframes");
                            }
                        }),
                        new ButtonFunction("Sub", () =>
                        {
                            if (assignIndex.text.Contains(","))
                            {
                                var split = assignIndex.text.Split(',');

                                for (int i = 0; i < split.Length; i++)
                                {
                                    var text = split[i];
                                    if (!int.TryParse(text, out int a))
                                        return;

                                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                                    {
                                        var bm = timelineObject.GetData<BeatmapObject>();

                                        SubKeyframeValues((EventKeyframe)bm.events[3][Mathf.Clamp(a, 0, bm.events[3].Count - 1)], curves,
                                            opacityIF.text, hueIF.text, satIF.text, valIF.text, opacityGradientIF.text, hueGradientIF.text, satGradientIF.text, valGradientIF.text);

                                        Updater.UpdateObject(bm, "Keyframes");
                                    }
                                }

                                return;
                            }

                            if (!int.TryParse(assignIndex.text, out int num))
                                return;
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();

                                SubKeyframeValues((EventKeyframe)bm.events[3][Mathf.Clamp(num, 0, bm.events[3].Count - 1)], curves,
                                    opacityIF.text, hueIF.text, satIF.text, valIF.text, opacityGradientIF.text, hueGradientIF.text, satGradientIF.text, valGradientIF.text);

                                Updater.UpdateObject(bm, "Keyframes");
                            }
                        }));

                    EditorHelper.SetComplexity(labels, Complexity.Advanced);
                    try
                    {
                        EditorHelper.SetComplexity(assignIndex.transform.parent.gameObject, Complexity.Normal);
                    }
                    catch (Exception ex)
                    {
                        CoreHelper.LogException(ex);
                    }
                    EditorHelper.SetComplexity(buttons1, Complexity.Advanced);
                }

                // Create Color Keyframe
                {
                    var labels = GenerateLabels(parent, 32f, "Create Color Keyframe");
                    var buttons1 = GenerateButtons(parent, 32f, 0f, new ButtonFunction("Create", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();

                            var currentTime = AudioManager.inst.CurrentAudioSource.time;

                            if (currentTime < bm.StartTime) // don't want people creating keyframes before the objects' start time.
                                continue;

                            var index = bm.events[3].FindLastIndex(x => currentTime > bm.StartTime + x.eventTime);

                            if (index >= 0 && currentTime > bm.StartTime)
                            {
                                var kf = EventKeyframe.DeepCopy((EventKeyframe)bm.events[3][index]);
                                kf.eventTime = currentTime - bm.StartTime;
                                if (curves.value != 0 && DataManager.inst.AnimationListDictionary.TryGetValue(curves.value - 1, out DataManager.LSAnimation anim))
                                    kf.curveType = anim;

                                if (currentMultiColorSelection >= 0)
                                    kf.eventValues[0] = Mathf.Clamp(currentMultiColorSelection, 0, 18);
                                if (!string.IsNullOrEmpty(opacityIF.text))
                                    kf.eventValues[1] = -Mathf.Clamp(Parser.TryParse(opacityIF.text, 1f), 0f, 1f) + 1f;
                                if (!string.IsNullOrEmpty(hueIF.text))
                                    kf.eventValues[2] = Parser.TryParse(hueIF.text, 0f);
                                if (!string.IsNullOrEmpty(satIF.text))
                                    kf.eventValues[3] = Parser.TryParse(satIF.text, 0f);
                                if (!string.IsNullOrEmpty(valIF.text))
                                    kf.eventValues[4] = Parser.TryParse(valIF.text, 0f);

                                // Gradient
                                if (currentMultiGradientColorSelection >= 0)
                                    kf.eventValues[5] = Mathf.Clamp(currentMultiGradientColorSelection, 0, 18);
                                if (!string.IsNullOrEmpty(opacityGradientIF.text))
                                    kf.eventValues[6] = -Mathf.Clamp(Parser.TryParse(opacityGradientIF.text, 1f), 0f, 1f) + 1f;
                                if (!string.IsNullOrEmpty(hueGradientIF.text))
                                    kf.eventValues[7] = Parser.TryParse(hueGradientIF.text, 0f);
                                if (!string.IsNullOrEmpty(satGradientIF.text))
                                    kf.eventValues[8] = Parser.TryParse(satGradientIF.text, 0f);
                                if (!string.IsNullOrEmpty(valGradientIF.text))
                                    kf.eventValues[9] = Parser.TryParse(valGradientIF.text, 0f);

                                bm.events[3].Add(kf);
                            }

                            Updater.UpdateObject(bm, "Keyframes");
                            ObjectEditor.inst.RenderTimelineObject(ObjectEditor.inst.GetTimelineObject(bm));
                        }
                    }));

                    EditorHelper.SetComplexity(labels, Complexity.Advanced);
                    EditorHelper.SetComplexity(buttons1, Complexity.Advanced);
                }

                EditorHelper.SetComplexity(labelsColor, Complexity.Advanced);
                EditorHelper.SetComplexity(labelsSecondaryColor, Complexity.Advanced);
                EditorHelper.SetComplexity(labels1, Complexity.Advanced);
                EditorHelper.SetComplexity(labels2, Complexity.Advanced);
                EditorHelper.SetComplexity(labels3, Complexity.Advanced);
                EditorHelper.SetComplexity(labels4, Complexity.Advanced);
                EditorHelper.SetComplexity(labels5, Complexity.Advanced);
                EditorHelper.SetComplexity(labels6, Complexity.Advanced);
                EditorHelper.SetComplexity(labels7, Complexity.Advanced);
                EditorHelper.SetComplexity(labels8, Complexity.Advanced);
                EditorHelper.SetComplexity(labels9, Complexity.Advanced);
                EditorHelper.SetComplexity(labels10, Complexity.Advanced);

                EditorHelper.SetComplexity(disable, Complexity.Advanced);
                EditorHelper.SetComplexity(colorLayout, Complexity.Advanced);
                EditorHelper.SetComplexity(disableGradient, Complexity.Advanced);
                EditorHelper.SetComplexity(colorGradientLayout, Complexity.Advanced);
                EditorHelper.SetComplexity(curvesObject, Complexity.Advanced);
                try
                {
                    EditorHelper.SetComplexity(opacityIF.transform.parent.gameObject, Complexity.Advanced);
                    EditorHelper.SetComplexity(hueIF.transform.parent.gameObject, Complexity.Advanced);
                    EditorHelper.SetComplexity(satIF.transform.parent.gameObject, Complexity.Advanced);
                    EditorHelper.SetComplexity(valIF.transform.parent.gameObject, Complexity.Advanced);
                    EditorHelper.SetComplexity(opacityGradientIF.transform.parent.gameObject, Complexity.Advanced);
                    EditorHelper.SetComplexity(hueGradientIF.transform.parent.gameObject, Complexity.Advanced);
                    EditorHelper.SetComplexity(satGradientIF.transform.parent.gameObject, Complexity.Advanced);
                    EditorHelper.SetComplexity(valGradientIF.transform.parent.gameObject, Complexity.Advanced);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
            }

            GeneratePad(parent, Complexity.Normal);
            var pastingDataLabels = GenerateLabels(parent, 32f, new LabelSettings("- Pasting Data -", 22, FontStyle.Bold, TextAnchor.MiddleCenter));
            EditorHelper.SetComplexity(pastingDataLabels, Complexity.Normal);

            // Paste Data
            {
                var allTypesLabel = GenerateLabels(parent, 32f, "Paste Keyframe data (All types)");

                // All Types
                {
                    GeneratePasteKeyframeData(parent, () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedBeatmapObjects)
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();

                            for (int i = 0; i < bm.events.Count; i++)
                            {
                                var copiedKeyframeData = ObjectEditor.inst.GetCopiedData(i);
                                if (copiedKeyframeData == null)
                                    continue;

                                for (int j = 0; j < bm.events[i].Count; j++)
                                {
                                    var kf = (EventKeyframe)bm.events[i][j];
                                    kf.curveType = copiedKeyframeData.curveType;
                                    kf.eventValues = copiedKeyframeData.eventValues.Copy();
                                    kf.eventRandomValues = copiedKeyframeData.eventRandomValues.Copy();
                                    kf.random = copiedKeyframeData.random;
                                    kf.relative = copiedKeyframeData.relative;

                                    Updater.UpdateObject(bm, "Keyframes");
                                }
                            }
                        }
                        EditorManager.inst.DisplayNotification("Pasted keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                    }, _val =>
                    {
                        if (int.TryParse(_val, out int num))
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedBeatmapObjects)
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();

                                for (int i = 0; i < bm.events.Count; i++)
                                {
                                    var copiedKeyframeData = ObjectEditor.inst.GetCopiedData(i);
                                    if (copiedKeyframeData == null)
                                        continue;

                                    var kf = (EventKeyframe)bm.events[i][Mathf.Clamp(num, 0, bm.events[i].Count - 1)];
                                    kf.curveType = copiedKeyframeData.curveType;
                                    kf.eventValues = copiedKeyframeData.eventValues.Copy();
                                    kf.eventRandomValues = copiedKeyframeData.eventRandomValues.Copy();
                                    kf.random = copiedKeyframeData.random;
                                    kf.relative = copiedKeyframeData.relative;

                                    Updater.UpdateObject(bm, "Keyframes");
                                }
                            }
                            EditorManager.inst.DisplayNotification("Pasted keyframe data to current selected keyframe.", 2f, EditorManager.NotificationType.Success);
                        }
                    });
                }

                EditorHelper.SetComplexity(allTypesLabel, Complexity.Advanced);

                for (int i = 0; i < 4; i++)
                {
                    string name = i switch
                    {
                        0 => "Position",
                        1 => "Scale",
                        2 => "Rotation",
                        3 => "Color",
                        _ => "Null",
                    };
                    var typeLabel = GenerateLabels(parent, 32f, $"Paste Keyframe data ({name})");
                    GeneratePasteKeyframeData(parent, i, name);
                    EditorHelper.SetComplexity(typeLabel, Complexity.Advanced);
                }
            }

            multiObjectEditorDialog.Find("data").AsRT().sizeDelta = new Vector2(810f, 730.11f);
            multiObjectEditorDialog.Find("data/left").AsRT().sizeDelta = new Vector2(355f, 730f);
        }

        int shapeSiblingIndex = 42;
        bool updatedShapes;
        bool updatedText;
        public List<Toggle> shapeToggles = new List<Toggle>();
        public List<List<Toggle>> shapeOptionToggles = new List<List<Toggle>>();

        Transform multiShapes;
        Transform multiShapeSettings;
        public Vector2Int multiShapeSelection;
        public Transform multiObjectContent;

        /// <summary>
        /// Renders the Shape ToggleGroup.
        /// </summary>
        public void RenderMultiShape()
        {
            CoreHelper.Log($"Running {nameof(RenderMultiShape)}");
            if (!ObjectEditor.inst || !ObjectEditor.inst.shapeButtonPrefab || !ShapeManager.inst.loadedShapes)
            {
                CoreHelper.WaitUntil(() => ObjectEditor.inst && ObjectEditor.inst.shapeButtonPrefab && ShapeManager.inst.loadedShapes, RenderMultiShape);
                return;
            }

            if (!multiShapes)
            {
                var shapes = ObjEditor.inst.ObjectView.transform.Find("shape").gameObject.Duplicate(multiObjectContent, "shape", shapeSiblingIndex);
                var shapeOption = ObjEditor.inst.ObjectView.transform.Find("shapesettings").gameObject.Duplicate(multiObjectContent, "shapesettings", shapeSiblingIndex + 1);
                multiShapes = shapes.transform;
                multiShapeSettings = shapeOption.transform;

                multiShapes.AsRT().sizeDelta = new Vector2(388.4f, 32f);
                multiShapeSettings.AsRT().sizeDelta = new Vector2(351f, 32f);
            }

            var shape = multiShapes;
            var shapeSettings = multiShapeSettings;

            var shapeGLG = shape.GetComponent<GridLayoutGroup>();
            shapeGLG.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            shapeGLG.constraintCount = 1;
            shapeGLG.spacing = new Vector2(7.6f, 0f);

            if (!updatedShapes)
            {
                // Initial removing
                DestroyImmediate(shape.GetComponent<ToggleGroup>());

                var toDestroy = new List<GameObject>();

                for (int i = 0; i < shape.childCount; i++)
                {
                    toDestroy.Add(shape.GetChild(i).gameObject);
                }

                for (int i = 0; i < shapeSettings.childCount; i++)
                {
                    if (i != 4 && i != 6)
                        for (int j = 0; j < shapeSettings.GetChild(i).childCount; j++)
                        {
                            toDestroy.Add(shapeSettings.GetChild(i).GetChild(j).gameObject);
                        }
                }

                foreach (var obj in toDestroy)
                    DestroyImmediate(obj);

                toDestroy = null;

                for (int i = 0; i < ShapeManager.inst.Shapes2D.Count; i++)
                {
                    var obj = ObjectEditor.inst.shapeButtonPrefab.Duplicate(shape, (i + 1).ToString(), i);
                    if (obj.transform.Find("Image") && obj.transform.Find("Image").gameObject.TryGetComponent(out Image image))
                    {
                        image.sprite = ShapeManager.inst.Shapes2D[i][0].Icon;
                        EditorThemeManager.ApplyGraphic(image, ThemeGroup.Toggle_1_Check);
                    }

                    if (!obj.GetComponent<HoverUI>())
                    {
                        var hoverUI = obj.AddComponent<HoverUI>();
                        hoverUI.animatePos = false;
                        hoverUI.animateSca = true;
                        hoverUI.size = 1.1f;
                    }

                    var shapeToggle = obj.GetComponent<Toggle>();
                    EditorThemeManager.ApplyToggle(shapeToggle, ThemeGroup.Background_1);
                    shapeToggle.group = null;

                    shapeToggles.Add(shapeToggle);

                    shapeOptionToggles.Add(new List<Toggle>());

                    if (i != 4 && i != 6)
                    {
                        if (!shapeSettings.Find((i + 1).ToString()))
                        {
                            var sh = shapeSettings.Find("6").gameObject.Duplicate(shapeSettings, (i + 1).ToString());
                            LSHelpers.DeleteChildren(sh.transform, true);

                            var d = new List<GameObject>();
                            for (int j = 0; j < sh.transform.childCount; j++)
                            {
                                d.Add(sh.transform.GetChild(j).gameObject);
                            }
                            foreach (var go in d)
                                DestroyImmediate(go);
                            d.Clear();
                            d = null;
                        }

                        var so = shapeSettings.Find((i + 1).ToString());

                        var rect = (RectTransform)so;
                        if (!so.GetComponent<ScrollRect>())
                        {
                            var scroll = so.gameObject.AddComponent<ScrollRect>();
                            so.gameObject.AddComponent<Mask>();
                            var ad = so.gameObject.AddComponent<Image>();

                            scroll.horizontal = true;
                            scroll.vertical = false;
                            scroll.content = rect;
                            scroll.viewport = rect;
                            ad.color = new Color(1f, 1f, 1f, 0.01f);
                        }

                        for (int j = 0; j < ShapeManager.inst.Shapes2D[i].Count; j++)
                        {
                            var opt = ObjectEditor.inst.shapeButtonPrefab.Duplicate(shapeSettings.GetChild(i), (j + 1).ToString(), j);
                            if (opt.transform.Find("Image") && opt.transform.Find("Image").gameObject.TryGetComponent(out Image image1))
                            {
                                image1.sprite = ShapeManager.inst.Shapes2D[i][j].Icon;
                                EditorThemeManager.ApplyGraphic(image1, ThemeGroup.Toggle_1_Check);
                            }

                            if (!opt.GetComponent<HoverUI>())
                            {
                                var hoverUI = opt.AddComponent<HoverUI>();
                                hoverUI.animatePos = false;
                                hoverUI.animateSca = true;
                                hoverUI.size = 1.1f;
                            }

                            var shapeOptionToggle = opt.GetComponent<Toggle>();
                            EditorThemeManager.ApplyToggle(shapeOptionToggle, ThemeGroup.Background_1);
                            shapeOptionToggle.group = null;

                            shapeOptionToggles[i].Add(shapeOptionToggle);

                            var layoutElement = opt.AddComponent<LayoutElement>();
                            layoutElement.layoutPriority = 1;
                            layoutElement.minWidth = 32f;

                            ((RectTransform)opt.transform).sizeDelta = new Vector2(32f, 32f);

                            if (!opt.GetComponent<HoverUI>())
                            {
                                var he = opt.AddComponent<HoverUI>();
                                he.animatePos = false;
                                he.animateSca = true;
                                he.size = 1.1f;
                            }
                        }

                        ObjectEditor.inst.LastGameObject(shapeSettings.GetChild(i));
                    }
                }

                if (ObjectManager.inst.objectPrefabs.Count > 9)
                {
                    var playerSprite = SpriteHelper.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_player.png");
                    int i = shape.childCount;
                    var obj = ObjectEditor.inst.shapeButtonPrefab.Duplicate(shape, (i + 1).ToString());
                    if (obj.transform.Find("Image") && obj.transform.Find("Image").gameObject.TryGetComponent(out Image image))
                    {
                        image.sprite = playerSprite;
                        EditorThemeManager.ApplyGraphic(image, ThemeGroup.Toggle_1_Check);
                    }

                    var so = shapeSettings.Find((i + 1).ToString());

                    if (!so)
                    {
                        so = shapeSettings.Find("6").gameObject.Duplicate(shapeSettings, (i + 1).ToString()).transform;
                        LSHelpers.DeleteChildren(so, true);

                        var d = new List<GameObject>();
                        for (int j = 0; j < so.transform.childCount; j++)
                        {
                            d.Add(so.transform.GetChild(j).gameObject);
                        }
                        foreach (var go in d)
                            DestroyImmediate(go);
                        d.Clear();
                        d = null;
                    }

                    var rect = (RectTransform)so;
                    if (!so.GetComponent<ScrollRect>())
                    {
                        var scroll = so.gameObject.AddComponent<ScrollRect>();
                        so.gameObject.AddComponent<Mask>();
                        var ad = so.gameObject.AddComponent<Image>();

                        scroll.horizontal = true;
                        scroll.vertical = false;
                        scroll.content = rect;
                        scroll.viewport = rect;
                        ad.color = new Color(1f, 1f, 1f, 0.01f);
                    }

                    var shapeToggle = obj.GetComponent<Toggle>();
                    shapeToggles.Add(shapeToggle);
                    EditorThemeManager.ApplyToggle(shapeToggle, ThemeGroup.Background_1);
                    shapeToggle.group = null;

                    shapeOptionToggles.Add(new List<Toggle>());

                    for (int j = 0; j < ObjectManager.inst.objectPrefabs[9].options.Count; j++)
                    {
                        var opt = ObjectEditor.inst.shapeButtonPrefab.Duplicate(shapeSettings.GetChild(i), (j + 1).ToString(), j);
                        if (opt.transform.Find("Image") && opt.transform.Find("Image").gameObject.TryGetComponent(out Image image1))
                        {
                            image1.sprite = playerSprite;
                            EditorThemeManager.ApplyGraphic(image1, ThemeGroup.Toggle_1_Check);
                        }

                        var shapeOptionToggle = opt.GetComponent<Toggle>();
                        EditorThemeManager.ApplyToggle(shapeOptionToggle, ThemeGroup.Background_1);
                        shapeOptionToggle.group = null;

                        shapeOptionToggles[i].Add(shapeOptionToggle);

                        var layoutElement = opt.AddComponent<LayoutElement>();
                        layoutElement.layoutPriority = 1;
                        layoutElement.minWidth = 32f;

                        ((RectTransform)opt.transform).sizeDelta = new Vector2(32f, 32f);

                        if (!opt.GetComponent<HoverUI>())
                        {
                            var he = opt.AddComponent<HoverUI>();
                            he.animatePos = false;
                            he.animateSca = true;
                            he.size = 1.1f;
                        }
                    }

                    ObjectEditor.inst.LastGameObject(shapeSettings.GetChild(i));
                }

                updatedShapes = true;
            }

            LSHelpers.SetActiveChildren(shapeSettings, false);

            if (multiShapeSelection.x == 4)
            {
                shapeSettings.AsRT().sizeDelta = new Vector2(351f, 74f);
                var child = shapeSettings.GetChild(4);
                child.AsRT().sizeDelta = new Vector2(351f, 74f);
                child.Find("Text").GetComponent<Text>().alignment = TextAnchor.UpperLeft;
                child.Find("Placeholder").GetComponent<Text>().alignment = TextAnchor.UpperLeft;
                child.GetComponent<InputField>().lineType = InputField.LineType.MultiLineNewline;
            }
            else
            {
                shapeSettings.AsRT().sizeDelta = new Vector2(351f, 32f);
                shapeSettings.GetChild(4).AsRT().sizeDelta = new Vector2(351f, 32f);
            }

            shapeSettings.GetChild(multiShapeSelection.x).gameObject.SetActive(true);

            int num = 0;
            foreach (var toggle in shapeToggles)
            {
                int index = num;
                toggle.onValueChanged.ClearAll();
                toggle.isOn = multiShapeSelection.x == index;
                toggle.gameObject.SetActive(ShowModdedUI || index < ObjectEditor.UnmoddedShapeCounts.Length);

                if (ShowModdedUI || index < ObjectEditor.UnmoddedShapeCounts.Length)
                    toggle.onValueChanged.AddListener(_val =>
                    {
                        multiShapeSelection = new Vector2Int(index, 0);

                        foreach (var beatmapObject in ObjectEditor.inst.SelectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()))
                        {
                            beatmapObject.shape = multiShapeSelection.x;
                            beatmapObject.shapeOption = multiShapeSelection.y;

                            if (beatmapObject.gradientType != BeatmapObject.GradientType.Normal && (index == 4 || index == 6 || index == 10))
                                beatmapObject.shape = 0;

                            Updater.UpdateObject(beatmapObject, "Shape");
                        }

                        RenderMultiShape();
                    });


                num++;
            }

            if (multiShapeSelection.x != 4 && multiShapeSelection.x != 6)
            {
                num = 0;
                foreach (var toggle in shapeOptionToggles[multiShapeSelection.x])
                {
                    int index = num;
                    toggle.onValueChanged.ClearAll();
                    toggle.isOn = multiShapeSelection.y == index;
                    toggle.gameObject.SetActive(ShowModdedUI || index < ObjectEditor.UnmoddedShapeCounts[multiShapeSelection.x]);

                    if (ShowModdedUI || index < ObjectEditor.UnmoddedShapeCounts[multiShapeSelection.x])
                        toggle.onValueChanged.AddListener(_val =>
                        {
                            multiShapeSelection.y = index;

                            foreach (var beatmapObject in ObjectEditor.inst.SelectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()))
                            {
                                beatmapObject.shape = multiShapeSelection.x;
                                beatmapObject.shapeOption = multiShapeSelection.y;

                                if (beatmapObject.gradientType != BeatmapObject.GradientType.Normal && (index == 4 || index == 6 || index == 10))
                                    beatmapObject.shape = 0;

                                Updater.UpdateObject(beatmapObject, "Shape");
                            }

                            RenderMultiShape();
                        });

                    num++;
                }
            }
            else if (multiShapeSelection.x == 4)
            {
                var textIF = shapeSettings.Find("5").GetComponent<InputField>();
                textIF.onValueChanged.ClearAll();
                if (!updatedText)
                {
                    updatedText = true;
                    textIF.text = "";
                }
                textIF.onValueChanged.AddListener(_val =>
                {
                    foreach (var beatmapObject in ObjectEditor.inst.SelectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()))
                    {
                        beatmapObject.text = _val;

                        Updater.UpdateObject(beatmapObject, "Shape");
                    }
                });

                if (!textIF.transform.Find("edit"))
                {
                    var button = EditorPrefabHolder.Instance.DeleteButton.Duplicate(textIF.transform, "edit");
                    var buttonStorage = button.GetComponent<DeleteButtonStorage>();
                    buttonStorage.image.sprite = KeybindManager.inst.editSprite;
                    EditorThemeManager.ApplySelectable(buttonStorage.button, ThemeGroup.Function_2);
                    EditorThemeManager.ApplyGraphic(buttonStorage.image, ThemeGroup.Function_2_Text);
                    buttonStorage.button.onClick.ClearAll();
                    buttonStorage.button.onClick.AddListener(() => { TextEditor.inst.SetInputField(textIF); });
                    UIManager.SetRectTransform(buttonStorage.baseImage.rectTransform, new Vector2(160f, 24f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(22f, 22f));
                    EditorHelper.SetComplexity(button, Complexity.Advanced);
                }
                else
                {
                    var button = textIF.transform.Find("edit").gameObject;
                    var buttonStorage = button.GetComponent<DeleteButtonStorage>();
                    buttonStorage.button.onClick.ClearAll();
                    buttonStorage.button.onClick.AddListener(() => { TextEditor.inst.SetInputField(textIF); });
                }
            }
            else if (multiShapeSelection.x == 6)
            {
                var select = shapeSettings.Find("7/select").GetComponent<Button>();
                select.onClick.ClearAll();
                select.onClick.AddListener(() =>
                {
                    var editorPath = RTFile.ApplicationDirectory + RTEditor.editorListSlash + EditorManager.inst.currentLoadedLevel;
                    string jpgFile = FileBrowser.OpenSingleFile("Select an image!", editorPath, new string[] { "png", "jpg" });
                    CoreHelper.Log($"Selected file: {jpgFile}");
                    if (!string.IsNullOrEmpty(jpgFile))
                    {
                        string jpgFileLocation = editorPath + "/" + Path.GetFileName(jpgFile);
                        CoreHelper.Log($"jpgFileLocation: {jpgFileLocation}");

                        var levelPath = jpgFile.Replace("\\", "/").Replace(editorPath + "/", "");
                        CoreHelper.Log($"levelPath: {levelPath}");

                        if (!RTFile.FileExists(jpgFileLocation) && !jpgFile.Replace("\\", "/").Contains(editorPath))
                        {
                            File.Copy(jpgFile, jpgFileLocation);
                            CoreHelper.Log($"Copied file to : {jpgFileLocation}");
                        }
                        else
                            jpgFileLocation = editorPath + "/" + levelPath;

                        CoreHelper.Log($"jpgFileLocation: {jpgFileLocation}");

                        foreach (var beatmapObject in ObjectEditor.inst.SelectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()))
                        {
                            beatmapObject.text = jpgFileLocation.Replace(jpgFileLocation.Substring(0, jpgFileLocation.LastIndexOf('/') + 1), "");

                            Updater.UpdateObject(beatmapObject, "Shape");
                        }
                        RenderMultiShape();
                    }
                });
                shapeSettings.Find("7/text").GetComponent<Text>().text = "Select an image";

                if (shapeSettings.Find("7/set"))
                    CoreHelper.Destroy(shapeSettings.Find("7/set").gameObject);
            }
        }

        void GeneratePad(Transform parent)
        {
            var gameObject = Creator.NewUIObject("padder", parent);
            var image = gameObject.AddComponent<Image>();
            image.rectTransform.sizeDelta = new Vector2(395f, 4f);
            EditorThemeManager.AddGraphic(image, ThemeGroup.Background_3);
        }
        
        void GeneratePad(Transform parent, Complexity complexity, bool onlySpecificComplexity = false)
        {
            var gameObject = Creator.NewUIObject("padder", parent);
            var image = gameObject.AddComponent<Image>();
            image.rectTransform.sizeDelta = new Vector2(395f, 4f);
            EditorThemeManager.AddGraphic(image, ThemeGroup.Background_3);
            EditorHelper.SetComplexity(gameObject, complexity, onlySpecificComplexity);
        }

        void GeneratePasteKeyframeData(Transform parent, int type, string name)
        {
            GeneratePasteKeyframeData(parent, () =>
            {
                var copiedKeyframeData = ObjectEditor.inst.GetCopiedData(type);
                if (copiedKeyframeData == null)
                {
                    EditorManager.inst.DisplayNotification($"{name} keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                    return;
                }

                foreach (var timelineObject in ObjectEditor.inst.SelectedBeatmapObjects)
                {
                    var bm = timelineObject.GetData<BeatmapObject>();
                    for (int i = 0; i < bm.events[type].Count; i++)
                    {
                        var kf = (EventKeyframe)bm.events[type][i];
                        kf.curveType = copiedKeyframeData.curveType;
                        kf.eventValues = copiedKeyframeData.eventValues.Copy();
                        kf.eventRandomValues = copiedKeyframeData.eventRandomValues.Copy();
                        kf.random = copiedKeyframeData.random;
                        kf.relative = copiedKeyframeData.relative;

                        Updater.UpdateObject(bm, "Keyframes");
                    }
                }
                EditorManager.inst.DisplayNotification($"Pasted {name.ToLower()} keyframe data to current selected keyframe.", 2f, EditorManager.NotificationType.Success);
            }, _val =>
            {
                var copiedKeyframeData = ObjectEditor.inst.GetCopiedData(type);
                string name = type switch
                {
                    0 => "Position",
                    1 => "Scale",
                    2 => "Rotation",
                    3 => "Color",
                    _ => "Null"
                };
                if (copiedKeyframeData == null)
                {
                    EditorManager.inst.DisplayNotification($"{name} keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                    return;
                }

                if (int.TryParse(_val, out int num))
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedBeatmapObjects)
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();

                        var kf = (EventKeyframe)bm.events[type][Mathf.Clamp(num, 0, bm.events[type].Count - 1)];
                        kf.curveType = copiedKeyframeData.curveType;
                        kf.eventValues = copiedKeyframeData.eventValues.Copy();
                        kf.eventRandomValues = copiedKeyframeData.eventRandomValues.Copy();
                        kf.random = copiedKeyframeData.random;
                        kf.relative = copiedKeyframeData.relative;

                        Updater.UpdateObject(bm, "Keyframes");
                    }
                    EditorManager.inst.DisplayNotification($"Pasted {name.ToLower()} keyframe data to current selected keyframe.", 2f, EditorManager.NotificationType.Success);
                }
            });
        }

        void GeneratePasteKeyframeData(Transform parent, Action pasteAll, Action<string> pasteToIndex)
        {
            var index = CreateInputField("index", "0", "Enter index...", parent, maxValue: int.MaxValue);

            var pasteAllTypesBase = new GameObject("paste all types");
            pasteAllTypesBase.transform.SetParent(parent);
            pasteAllTypesBase.transform.localScale = Vector3.one;

            var pasteAllTypesBaseRT = pasteAllTypesBase.AddComponent<RectTransform>();
            pasteAllTypesBaseRT.sizeDelta = new Vector2(390f, 32f);

            var pasteAllTypesBaseHLG = pasteAllTypesBase.AddComponent<HorizontalLayoutGroup>();
            pasteAllTypesBaseHLG.childControlHeight = false;
            pasteAllTypesBaseHLG.childControlWidth = false;
            pasteAllTypesBaseHLG.childForceExpandHeight = false;
            pasteAllTypesBaseHLG.childForceExpandWidth = false;
            pasteAllTypesBaseHLG.spacing = 8f;

            var pasteAllTypesToAllObject = EditorPrefabHolder.Instance.Function1Button.Duplicate(pasteAllTypesBaseRT, name);
            pasteAllTypesToAllObject.transform.localScale = Vector3.one;

            ((RectTransform)pasteAllTypesToAllObject.transform).sizeDelta = new Vector2(180f, 32f);

            var pasteAllTypesToAllText = pasteAllTypesToAllObject.transform.GetChild(0).GetComponent<Text>();
            pasteAllTypesToAllText.text = "Paste to All";

            EditorThemeManager.AddGraphic(pasteAllTypesToAllObject.GetComponent<Image>(), ThemeGroup.Paste, true);
            EditorThemeManager.AddGraphic(pasteAllTypesToAllText, ThemeGroup.Paste_Text);

            var pasteAllTypesToAll = pasteAllTypesToAllObject.GetComponent<Button>();
            pasteAllTypesToAll.onClick.ClearAll();
            pasteAllTypesToAll.onClick.AddListener(() => { pasteAll?.Invoke(); });

            var pasteAllTypesToIndexObject = EditorPrefabHolder.Instance.Function1Button.Duplicate(pasteAllTypesBaseRT, name);
            pasteAllTypesToIndexObject.transform.localScale = Vector3.one;

            ((RectTransform)pasteAllTypesToIndexObject.transform).sizeDelta = new Vector2(180f, 32f);

            var pasteAllTypesToIndexText = pasteAllTypesToIndexObject.transform.GetChild(0).GetComponent<Text>();
            pasteAllTypesToIndexText.text = "Paste to Index";

            EditorThemeManager.AddGraphic(pasteAllTypesToIndexObject.GetComponent<Image>(), ThemeGroup.Paste, true);
            EditorThemeManager.AddGraphic(pasteAllTypesToIndexText, ThemeGroup.Paste_Text);

            var pasteAllTypesToIndex = pasteAllTypesToIndexObject.GetComponent<Button>();
            pasteAllTypesToIndex.onClick.ClearAll();
            pasteAllTypesToIndex.onClick.AddListener(() => { pasteToIndex?.Invoke(index.text); });

            EditorHelper.SetComplexity(index.transform.parent.gameObject, Complexity.Advanced);
            EditorHelper.SetComplexity(pasteAllTypesBase, Complexity.Advanced);
        }

        void SyncObjectData(string nameContext, PointerEventData eventData, Action<TimelineObject, BeatmapObject> update, bool renderTimelineObject = false, bool updateObject = true, string updateContext = "")
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                ShowContextMenu(300f,
                    new ButtonFunction($"Sync {nameContext} via Search", () =>
                    {
                        ShowObjectSearch(beatmapObject =>
                        {
                            SyncObjectData(timelineObject => { update?.Invoke(timelineObject, beatmapObject); }, renderTimelineObject, updateObject, updateContext);
                            EditorManager.inst.HideDialog("Object Search Popup");
                        });
                    }),
                    new ButtonFunction($"Sync {nameContext} via Picker", () =>
                    {
                        onSelectTimelineObject = to =>
                        {
                            var beatmapObject = to.GetData<BeatmapObject>();
                            SyncObjectData(timelineObject => { update?.Invoke(timelineObject, beatmapObject); }, renderTimelineObject, updateObject, updateContext);
                        };
                    }));

                return;
            }

            ShowObjectSearch(beatmapObject =>
            {
                SyncObjectData(timelineObject => { update?.Invoke(timelineObject, beatmapObject); }, renderTimelineObject, updateObject, updateContext);
                EditorManager.inst.HideDialog("Object Search Popup");
            });
        }

        void SyncObjectData(Action<TimelineObject> update, bool renderTimelineObject = false, bool updateObject = true, string updateContext = "")
        {
            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject))
            {
                update?.Invoke(timelineObject);

                if (renderTimelineObject)
                    ObjectEditor.inst.RenderTimelineObject(timelineObject);

                if (!updateObject)
                    continue;

                if (!string.IsNullOrEmpty(updateContext))
                    Updater.UpdateObject(timelineObject.GetData<BeatmapObject>(), updateContext);
                else
                    Updater.UpdateObject(timelineObject.GetData<BeatmapObject>());
            }
        }

        public void SetKeyframeValues(EventKeyframe kf, Dropdown curves,
            string opacity, string hue, string sat, string val, string opacityGradient, string hueGradient, string satGradient, string valGradient)
        {
            if (curves.value != 0 && DataManager.inst.AnimationListDictionary.TryGetValue(curves.value - 1, out DataManager.LSAnimation anim))
                kf.curveType = anim;
            if (currentMultiColorSelection >= 0)
                kf.eventValues[0] = Mathf.Clamp(currentMultiColorSelection, 0, 18);
            if (!string.IsNullOrEmpty(opacity))
                kf.eventValues[1] = Mathf.Clamp(kf.eventValues[1] - Parser.TryParse(opacity, 1f), 0f, 1f);
            if (!string.IsNullOrEmpty(hue))
                kf.eventValues[2] = Parser.TryParse(sat, 0f);
            if (!string.IsNullOrEmpty(sat))
                kf.eventValues[3] = Parser.TryParse(sat, 0f);
            if (!string.IsNullOrEmpty(val))
                kf.eventValues[4] = Parser.TryParse(val, 0f);

            // Gradient
            if (currentMultiGradientColorSelection >= 0)
                kf.eventValues[5] = Mathf.Clamp(currentMultiGradientColorSelection, 0, 18);
            if (!string.IsNullOrEmpty(opacityGradient))
                kf.eventValues[6] = -Mathf.Clamp(Parser.TryParse(opacityGradient, 1f), 0f, 1f) + 1f;
            if (!string.IsNullOrEmpty(hueGradient))
                kf.eventValues[7] = Parser.TryParse(hueGradient, 0f);
            if (!string.IsNullOrEmpty(satGradient))
                kf.eventValues[8] = Parser.TryParse(satGradient, 0f);
            if (!string.IsNullOrEmpty(valGradient))
                kf.eventValues[9] = Parser.TryParse(valGradient, 0f);
        }
        
        public void AddKeyframeValues(EventKeyframe kf, Dropdown curves,
            string opacity, string hue, string sat, string val, string opacityGradient, string hueGradient, string satGradient, string valGradient)
        {
            if (curves.value != 0 && DataManager.inst.AnimationListDictionary.TryGetValue(curves.value - 1, out DataManager.LSAnimation anim))
                kf.curveType = anim;
            if (currentMultiColorSelection >= 0)
                kf.eventValues[0] = Mathf.Clamp(currentMultiColorSelection, 0, 18);
            if (!string.IsNullOrEmpty(opacity))
                kf.eventValues[1] = Mathf.Clamp(kf.eventValues[1] - Parser.TryParse(opacity, 1f), 0f, 1f);
            if (!string.IsNullOrEmpty(hue))
                kf.eventValues[2] += Parser.TryParse(hue, 0f);
            if (!string.IsNullOrEmpty(sat))
                kf.eventValues[3] += Parser.TryParse(sat, 0f);
            if (!string.IsNullOrEmpty(val))
                kf.eventValues[4] += Parser.TryParse(val, 0f);

            // Gradient
            if (currentMultiGradientColorSelection >= 0)
                kf.eventValues[5] = Mathf.Clamp(currentMultiGradientColorSelection, 0, 18); // color slots can't be added onto.
            if (!string.IsNullOrEmpty(opacityGradient))
                kf.eventValues[6] = Mathf.Clamp(kf.eventValues[6] - Parser.TryParse(opacityGradient, 1f), 0f, 1f);
            if (!string.IsNullOrEmpty(hueGradient))
                kf.eventValues[7] += Parser.TryParse(hueGradient, 0f);
            if (!string.IsNullOrEmpty(satGradient))
                kf.eventValues[8] += Parser.TryParse(satGradient, 0f);
            if (!string.IsNullOrEmpty(valGradient))
                kf.eventValues[9] += Parser.TryParse(valGradient, 0f);
        }
        
        public void SubKeyframeValues(EventKeyframe kf, Dropdown curves,
            string opacity, string hue, string sat, string val, string opacityGradient, string hueGradient, string satGradient, string valGradient)
        {
            if (curves.value != 0 && DataManager.inst.AnimationListDictionary.TryGetValue(curves.value - 1, out DataManager.LSAnimation anim))
                kf.curveType = anim;
            if (currentMultiColorSelection >= 0)
                kf.eventValues[0] = Mathf.Clamp(currentMultiColorSelection, 0, 18);
            if (!string.IsNullOrEmpty(opacity))
                kf.eventValues[1] = Mathf.Clamp(kf.eventValues[1] + Parser.TryParse(opacity, 1f), 0f, 1f);
            if (!string.IsNullOrEmpty(hue))
                kf.eventValues[2] -= Parser.TryParse(hue, 0f);
            if (!string.IsNullOrEmpty(sat))
                kf.eventValues[3] -= Parser.TryParse(sat, 0f);
            if (!string.IsNullOrEmpty(val))
                kf.eventValues[4] -= Parser.TryParse(val, 0f);

            // Gradient
            if (currentMultiGradientColorSelection >= 0)
                kf.eventValues[5] = Mathf.Clamp(currentMultiGradientColorSelection, 0, 18); // color slots can't be added onto.
            if (!string.IsNullOrEmpty(opacityGradient))
                kf.eventValues[6] = Mathf.Clamp(kf.eventValues[6] + Parser.TryParse(opacityGradient, 1f), 0f, 1f);
            if (!string.IsNullOrEmpty(hueGradient))
                kf.eventValues[7] -= Parser.TryParse(hueGradient, 0f);
            if (!string.IsNullOrEmpty(satGradient))
                kf.eventValues[8] -= Parser.TryParse(satGradient, 0f);
            if (!string.IsNullOrEmpty(valGradient))
                kf.eventValues[9] -= Parser.TryParse(valGradient, 0f);
        }

        public GameObject GenerateLabels(Transform parent, float sizeY, params string[] labels)
        {
            var labelBase = Creator.NewUIObject("label", parent);
            labelBase.transform.AsRT().sizeDelta = new Vector2(0f, sizeY);
            labelBase.AddComponent<HorizontalLayoutGroup>();
            var labelPrefab = EditorManager.inst.folderButtonPrefab.transform.GetChild(0).gameObject;

            for (int i = 0; i < labels.Length; i++)
            {
                var label = labelPrefab.Duplicate(labelBase.transform, "text");
                var labelText = label.GetComponent<Text>();
                labelText.text = labels[i];
                EditorThemeManager.AddLightText(labelText);
            }

            return labelBase;
        }
        
        public GameObject GenerateLabels(Transform parent, float sizeY, params LabelSettings[] labels)
        {
            var labelBase = Creator.NewUIObject("label", parent);
            labelBase.transform.AsRT().sizeDelta = new Vector2(0f, sizeY);
            labelBase.AddComponent<HorizontalLayoutGroup>();
            var labelPrefab = EditorManager.inst.folderButtonPrefab.transform.GetChild(0).gameObject;

            for (int i = 0; i < labels.Length; i++)
            {
                var label = labelPrefab.Duplicate(labelBase.transform, "text");
                var labelText = label.GetComponent<Text>();
                labels[i].Apply(labelText);
                EditorThemeManager.AddLightText(labelText);
            }

            return labelBase;
        }

        public class LabelSettings
        {
            public LabelSettings(string text)
            {
                this.text = text;
            }
            
            public LabelSettings(string text, int fontSize) : this(text)
            {
                this.fontSize = fontSize;
            }
            
            public LabelSettings(string text, int fontSize, FontStyle fontStyle) : this(text, fontSize)
            {
                this.fontStyle = fontStyle;
            }
            
            public LabelSettings(string text, int fontSize, FontStyle fontStyle, TextAnchor alignment) : this(text, fontSize, fontStyle)
            {
                this.alignment = alignment;
            }

            public TextAnchor alignment = TextAnchor.MiddleLeft;
            public string text;
            public int fontSize = 20;
            public FontStyle fontStyle = FontStyle.Normal;
            public HorizontalWrapMode horizontalWrap = HorizontalWrapMode.Overflow;
            public VerticalWrapMode verticalWrap = VerticalWrapMode.Truncate;

            public void Apply(Text text)
            {
                text.text = this;
                text.alignment = alignment;
                text.fontSize = fontSize;
                text.fontStyle = fontStyle;
                text.horizontalOverflow = horizontalWrap;
                text.verticalOverflow = verticalWrap;
            }

            public static implicit operator string(LabelSettings labelSettings) => labelSettings.text;
        }

        public InputFieldStorage GenerateInputField(Transform parent, string name, string defaultValue, string placeholder, bool doMiddle = false, bool doLeftGreater = false, bool doRightGreater = false)
        {
            var gameObject = EditorPrefabHolder.Instance.NumberInputField.Duplicate(parent, name);
            gameObject.transform.localScale = Vector3.one;
            var inputFieldStorage = gameObject.GetComponent<InputFieldStorage>();
            inputFieldStorage.inputField.GetPlaceholderText().text = placeholder;

            gameObject.transform.AsRT().sizeDelta = new Vector2(428f, 32f);

            inputFieldStorage.inputField.onValueChanged.ClearAll();
            inputFieldStorage.inputField.text = defaultValue;
            inputFieldStorage.inputField.transform.AsRT().sizeDelta = new Vector2(300f, 32f);

            if (doLeftGreater)
                EditorThemeManager.AddSelectable(inputFieldStorage.leftGreaterButton, ThemeGroup.Function_2, false);
            else
                Destroy(inputFieldStorage.leftGreaterButton.gameObject);
            
            if (doRightGreater)
                EditorThemeManager.AddSelectable(inputFieldStorage.rightGreaterButton, ThemeGroup.Function_2, false);
            else
                Destroy(inputFieldStorage.rightGreaterButton.gameObject);

            if (doMiddle)
                EditorThemeManager.AddSelectable(inputFieldStorage.middleButton, ThemeGroup.Function_2, false);
            else
                Destroy(inputFieldStorage.middleButton.gameObject);

            EditorThemeManager.AddSelectable(inputFieldStorage.leftButton, ThemeGroup.Function_2, false);

            EditorThemeManager.AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);

            EditorThemeManager.AddInputField(inputFieldStorage.inputField);

            return inputFieldStorage;
        }

        /// <summary>
        /// Generates a horizontal group of buttons.
        /// </summary>
        /// <param name="parent">The transform to parent the buttons group to.</param>
        /// <param name="sizeY">The Y size of the base. Default is 32 or 48.</param>
        /// <param name="spacing">Spacing for the layout group. Default is 8.</param>
        /// <param name="buttons">Array of buttons to generate.</param>
        public GameObject GenerateButtons(Transform parent, float sizeY, float spacing, params ButtonFunction[] buttons)
        {
            var p = Creator.NewUIObject("buttons", parent);
            p.transform.AsRT().sizeDelta = new Vector2(0f, sizeY);
            var pHLG = p.AddComponent<HorizontalLayoutGroup>();
            pHLG.spacing = spacing;

            for (int i = 0; i < buttons.Length; i++)
                GenerateButton(p.transform, buttons[i]);

            return p;
        }

        public GameObject GenerateButton(Transform parent, ButtonFunction buttonFunction)
        {
            var button = EditorPrefabHolder.Instance.Function1Button.Duplicate(parent, buttonFunction.Name);
            var buttonStorage = button.GetComponent<FunctionButtonStorage>();
            if (buttonFunction.OnClick != null)
            {
                var clickable = button.AddComponent<ContextClickable>();
                clickable.onClick = buttonFunction.OnClick;
            }
            else
            {
                buttonStorage.button.onClick.ClearAll();
                buttonStorage.button.onClick.AddListener(() => { buttonFunction.Action?.Invoke(); });
            }
            buttonStorage.text.fontSize = buttonFunction.FontSize;
            buttonStorage.text.text = buttonFunction.Name;

            EditorThemeManager.AddGraphic(buttonStorage.button.image, ThemeGroup.Function_1, true);
            EditorThemeManager.AddGraphic(buttonStorage.text, ThemeGroup.Function_1_Text);

            return button;
        }

        InputField CreateInputField(string name, string value, string placeholder, Transform parent, float length = 340f, bool isInteger = true, float minValue = 0f, float maxValue = 0f)
        {
            var gameObject = EditorPrefabHolder.Instance.NumberInputField.Duplicate(parent, name);
            gameObject.transform.localScale = Vector3.one;
            var inputFieldStorage = gameObject.GetComponent<InputFieldStorage>();

            inputFieldStorage.inputField.image.rectTransform.sizeDelta = new Vector2(length, 32f);
            inputFieldStorage.inputField.GetPlaceholderText().text = placeholder;

            gameObject.transform.AsRT().sizeDelta = new Vector2(428f, 32f);

            inputFieldStorage.inputField.text = value;

            if (isInteger)
            {
                TriggerHelper.AddEventTriggers(gameObject, TriggerHelper.ScrollDeltaInt(inputFieldStorage.inputField, min: (int)minValue, max: (int)maxValue));
                TriggerHelper.IncreaseDecreaseButtonsInt(inputFieldStorage.inputField, min: (int)minValue, max: (int)maxValue, t: gameObject.transform);
            }
            else
            {
                TriggerHelper.AddEventTriggers(gameObject, TriggerHelper.ScrollDelta(inputFieldStorage.inputField, max: int.MaxValue));
                TriggerHelper.IncreaseDecreaseButtons(inputFieldStorage.inputField, min: (int)minValue, max: (int)maxValue, t: gameObject.transform);
            }

            EditorThemeManager.AddInputField(inputFieldStorage.inputField);

            Destroy(inputFieldStorage.leftGreaterButton.gameObject);
            Destroy(inputFieldStorage.middleButton.gameObject);
            Destroy(inputFieldStorage.rightGreaterButton.gameObject);
            EditorThemeManager.AddSelectable(inputFieldStorage.leftButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);

            return inputFieldStorage.inputField;
        }

        void UpdateMultiColorButtons()
        {
            for (int i = 0; i < multiColorButtons.Count; i++)
                multiColorButtons[i].Selected.SetActive(currentMultiColorSelection == i);

            for (int i = 0; i < multiGradientColorButtons.Count; i++)
                multiGradientColorButtons[i].Selected.SetActive(currentMultiGradientColorSelection == i);
        }

        #endregion

        EditorDocument.Element FontElement(EditorDocument.SupportType condition, string name, string desc) => new EditorDocument.Element($"<b>[{condition}]</b> {name} - {desc}", EditorDocument.Element.Type.Text, () =>
        {
            EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
            LSText.CopyToClipboard($"<font={name}>");
        });

        EditorDocument.Element LinkElement(string text, string url) => new EditorDocument.Element(text, EditorDocument.Element.Type.Text, () => Application.OpenURL(url));

        EditorDocument GenerateDocument(string name, string description, List<EditorDocument.Element> elements)
        {
            var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(documentationPopup.Content, "Document");
            var documentation = new EditorDocument(gameObject, name, description);

            EditorThemeManager.AddSelectable(gameObject.GetComponent<Button>(), ThemeGroup.List_Button_1);

            documentation.elements.AddRange(elements);

            var htt = gameObject.AddComponent<HoverTooltip>();
            htt.tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                desc = name,
                hint = description
            });

            var text = gameObject.transform.GetChild(0).GetComponent<Text>();

            text.text = documentation.Name;
            EditorThemeManager.AddLightText(text);

            documentations.Add(documentation);
            return documentation;
        }

        void CreateDocumentation() // NEED TO UPDATE!
        {
            documentationPopup = GeneratePopup("Documentation Popup", "Documentation", Vector2.zero, new Vector2(600f, 450f), _val =>
            {
                documentationSearch = _val;
                RefreshDocumentation();
            }, placeholderText: "Search for document...");

            EditorHelper.AddEditorDropdown("Wiki / Documentation", "", "Help", SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_question.png"), () =>
            {
                EditorManager.inst.ShowDialog("Documentation Popup");
                RefreshDocumentation();
            });

            var editorDialogObject = Instantiate(EditorManager.inst.GetDialog("Multi Keyframe Editor (Object)").Dialog.gameObject);
            var editorDialogTransform = editorDialogObject.transform;
            editorDialogObject.name = "DocumentationDialog";
            editorDialogObject.layer = 5;
            editorDialogTransform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs").transform);
            editorDialogTransform.localScale = Vector3.one;
            editorDialogTransform.position = new Vector3(1537.5f, 714.945f, 0f) * EditorManager.inst.ScreenScale;
            editorDialogTransform.AsRT().sizeDelta = new Vector2(0f, 32f);

            var editorDialogTitle = editorDialogTransform.GetChild(0);
            editorDialogTitle.GetComponent<Image>().color = LSColors.HexToColor("D89356");
            documentationTitle = editorDialogTitle.GetChild(0).GetComponent<Text>();
            documentationTitle.text = "- Documentation -";

            var editorDialogSpacer = editorDialogTransform.GetChild(1);
            editorDialogSpacer.AsRT().sizeDelta = new Vector2(765f, 54f);

            Destroy(editorDialogTransform.GetChild(2).gameObject);

            EditorHelper.AddEditorDialog("Documentation Dialog", editorDialogObject);

            var scrollView = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View"));
            documentationContent = scrollView.transform.Find("Viewport/Content");
            scrollView.transform.SetParent(editorDialogTransform);
            scrollView.transform.localScale = Vector3.one;
            scrollView.name = "Scroll View";

            LSHelpers.DeleteChildren(documentationContent);

            var scrollViewLE = scrollView.AddComponent<LayoutElement>();
            scrollViewLE.ignoreLayout = true;

            scrollView.transform.AsRT().anchoredPosition = new Vector2(392.5f, 320f);
            scrollView.transform.AsRT().sizeDelta = new Vector2(735f, 638f);

            EditorThemeManager.AddGraphic(editorDialogObject.GetComponent<Image>(), ThemeGroup.Background_1);

            documentationContent.GetComponent<VerticalLayoutGroup>().spacing = 12f;

            GenerateDocument("Introduction", "Welcome to Project Arrhythmia Legacy.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("Welcome to <b>Project Arrhythmia</b>!\nWhether you're new to the game, modding or have been around for a while, I'm sure this " +
                        "documentation will help massively in understanding the ins and outs of the editor and the game as a whole.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("These documents only list editor features and how to use them, everything else is listed in the Github wiki.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>DOCUMENTATION INFO</b>", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[VANILLA]</b> represents a feature from original Legacy, with very minor tweaks done to it if any.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[MODDED]</b> represents a feature added by BetterLegacy. These features will not work in unmodded PA.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[PATCHED]</b> represents a feature modified by BetterLegacy. They're either in newer versions of PA or are partially modded, meaning they might not work in regular PA.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[ALPHA]</b> represents a feature ported from alpha to BetterLegacy and are not present in Legacy.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[LEGACY]</b> represents a Legacy feature that was discontinued in alpha.", EditorDocument.Element.Type.Text)
            });

            GenerateDocument("Credits", "All the people who helped the mod development in some way.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("Reimnop's Catalyst (PA object and animation optimization)", EditorDocument.Element.Type.Text),
                LinkElement("<b>Source code</b>:\nhttps://github.com/Reimnop/Catalyst", "https://github.com/Reimnop/Catalyst"),
                
                new EditorDocument.Element("Reimnop's ILMath (fast math parser / evaluator)", EditorDocument.Element.Type.Text),
                LinkElement("<b>Source code</b>:\nhttps://github.com/Reimnop/ILMath", "https://github.com/Reimnop/ILMath"),

                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Keijiro Takahashi's KinoGlitch (AnalogGlitch and DigitalGlitch events)", EditorDocument.Element.Type.Text),
                LinkElement("<b>Source code</b>:\nhttps://github.com/keijiro/KinoGlitch", "https://github.com/keijiro/KinoGlitch"),

                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("WestHillApps' UniBpmAnalyzer", EditorDocument.Element.Type.Text),
                LinkElement("<b>Source code</b>:\nhttps://github.com/WestHillApps/UniBpmAnalyzer", "https://github.com/WestHillApps/UniBpmAnalyzer"),

                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Nick Vogt's ColliderCreator (used for creating proper collision for the custom shapes)", EditorDocument.Element.Type.Text),
                LinkElement("<b>Website</b>:\nhttps://www.h3xed.com/", "https://www.h3xed.com/"),
                LinkElement("<b>Source code</b>:\nhttps://www.h3xed.com/programming/automatically-create-polygon-collider-2d-from-2d-mesh-in-unity", "https://www.h3xed.com/programming/automatically-create-polygon-collider-2d-from-2d-mesh-in-unity"),

                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Crafty Font for the Pixellet font.", EditorDocument.Element.Type.Text),
                LinkElement("<b>Website</b>:\nhttps://craftyfont.gumroad.com/", "https://craftyfont.gumroad.com/"),

                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("HAWTPIXEL for the File Deletion font.", EditorDocument.Element.Type.Text),
                LinkElement("<b>Website</b>:\nhttps://www.hawtpixel.com/", "https://www.hawtpixel.com/"),

                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Sans Sans font.", EditorDocument.Element.Type.Text),
                LinkElement("<b>Website</b>:\nhttps://www.font.download/font/sans", "https://www.font.download/font/sans"),

                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Fontworks for the RocknRoll font.", EditorDocument.Element.Type.Text),
                LinkElement("<b>Website</b>:\nhttps://github.com/fontworks-fonts/RocknRoll", "https://github.com/fontworks-fonts/RocknRoll"),

                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("ManiackersDesign for the Monomaniac One font.", EditorDocument.Element.Type.Text),
                LinkElement("<b>Website</b>:\nhttps://github.com/ManiackersDesign/monomaniac", "https://github.com/ManiackersDesign/monomaniac"),

                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>SPECIAL THANKS</b>", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Pidge (developer of the game) - Obviously for making the game itself and inspiring some features in BetterLegacy.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("enchart - Massively helped RTMecha get into modding in the first place. Without enchart, none of this would have been possible.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("aiden_ytarame - Ported gradient objects from alpha to BetterLegacy.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("SleepyzGamer - Helped a lot in finding things", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("KarasuTori - For motivating RTMecha to keep going and experimenting with modding.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("MoNsTeR and CubeCube for testing the mods, reporting bugs and giving suggestions.", EditorDocument.Element.Type.Text),
            });

            GenerateDocument("Uploading a Level", "Guidelines on uploading levels.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("Want to post a level you made somewhere? Well, make sure you read these guidelines first.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("If you made a level in BetterLegacy <i>with</i> modded features or the song is not verified*, you <b>cannot</b> upload it to the Steam Workshop. You can only share it through the Arcade server or some other means.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Some modded features (as specified in the Introductions section) are more likely to break vanilla PA than others, but just to be safe: continue with the above point.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Even if you add warnings to your level where you state that it is modded, it doesn't matter because most people don't read titles / descriptions and will probably end up blaming the game for being broken, even if it really isn't.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("If you made a level in BetterLegacy <i>without</i> modded features and the song is verified, then you can upload it to the Steam Workshop. But you <b>MUST</b> verify that the level works in vanilla first by playtesting it all the way through in both vanilla Alpha and vanilla Legacy.\n\n" +
                                    "- If you want to playtest in Alpha, you can click the Convert to VG format button in Upload > Publish / Update Level, navigating to beatmaps/exports and moving the exported level to the beatmaps/editor folder where your Project Arrhythmia folder is.\n" +
                                    "- If you want to playtest in Legacy, you can just copy the level to your beatmaps/editor folder where your Project Arrhythmia folder is.\n" +
                                    "- Some features like object gradients, ColorGrading (hue only), Gradient event, Player Force event, and Prefab Transforms (position, scale, rotation) can be used in alpha, but it's still recommended to playtest in vanilla Alpha anyways.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("* Non-verified songs are allowed on the Arcade server because the Arcade server is not officially endorsed by Vitamin Games nor the Steam Workshop, much like how some other music-based games have an unofficial server for hosting levels. If a song is super-protected, however, it has a high chance of being taken down regardless of that fact.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("When uploading to the Arcade server, you can include tags to better define your level. When uploading a level, you have to keep these tag guidelines in mind:\n" +
                                    "- Tags are best with underscores replacing spaces, e.g. <b>\"boss_level\"</b>.\n" +
                                    "- Uppercase and lowercase is acceptable.\n" +
                                    "- Keep tags relevant to the level.\n" +
                                    "- Include series name. If the level is not a part of a series, then skip this.\n" +
                                    "- Include any important characters involved, e.g. <b>\"para\"</b>. If there are no characters, then skip this.\n" +
                                    "- Include the general theme of the level, e.g. <b>\"spooky\"</b>.\n" +
                                    "- If your level covers subject matter most people will find uncomfortable, PLEASE include it in the tags so people know to avoid (e.g. gore (<i>looking at you KarasuTori</i>)). If you do not follow this, then the level will be removed." +
                                    "- Add <b>\"joke_level\"</b> or <b>\"meme\"</b> to tags if your level isn't serious one. Can be both tags.\n" +
                                    "- If your level has the possibility to cause an epileptic seizure, include <b>\"flashing_lights\"</b>." +
                                    "- If your level features specific functions (like Window movement, video BG, etc), please tag that as well.\n" +
                                    "- Add <b>\"high_detail\"</b> to the tags if the level has a ton of objects / is lagging.\n" +
                                    "- Add <b>\"reupload\"</b> or <b>\"remake\"</b> to the tags if your level is such.\n" +
                                    "- Unfinished levels can be uploaded, but will need the <b>\"wip\"</b> tag. (do not flood the server with these)\n" +
                                    "- Older levels can be uploaded, but will need the <b>\"old\"</b> tag.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<i>Do not</i> reupload other people's levels to the server unless you have their permission. If you do reupload, it has to have been a level from the Steam workshop.\n" +
                                    "Remakes are allowed, as long as you have permission from the original creator(s).", EditorDocument.Element.Type.Text),
                LinkElement("If you want to comment on a level, join the System Error Discord server (<color=#0084FF>https://discord.gg/nB27X2JZcY</color>), go to <b>#arcade-server-uploads</b>, create a thread (if there isn't already one) and make your comment there.\n" +
                "You can click this element to open the Discord link.", "https://discord.gg/nB27X2JZcY"),
                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("When updating a level, make sure you include a message in the changelogs to let users know what changed in your level. Feel free to format it with dot points, or as a regular sentence / paragraph.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("If you have any issues with these guidelines or have a suggestion, please contact the BetterLegacy developer (RTMecha) and discuss it with him civilly.", EditorDocument.Element.Type.Text),
            });

            GenerateDocument("[PATCHED] Beatmap Objects", "The very objects that make up Project Arrhythmia levels.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("<b>Beatmap Objects</b> are the objects people use to create a variety of things for their levels. " +
                        "Whether it be backgrounds, characters, attacks, you name it! Below is a list of data Beatmap Objects have.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>ID [PATCHED]</b>\nThe ID is used for specifying a Beatmap Object, otherwise it'd most likely get lost in a sea of other objects! " +
                        "It's mostly used with parenting. This is patched because in unmodded PA, creators aren't able to see the ID of an object unless they look at the level.lsb.\n" +
                        "Clicking on the ID will copy it to your clipboard.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>LDM (Low Detail Mode) [MODDED]</b>\nLDM is useful for having objects not render for lower end devices. If the option is on and the user has " +
                        "Low Detail Mode enabled through the RTFunctions mod config, the Beatmap Object will not render.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_id_ldm.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Name [VANILLA]</b>\nNaming an object is incredibly helpful for readablility and knowing what an object does at a glance. " +
                        "Clicking your scroll wheel over it will flip any left / right.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_name_type.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Tags [MODDED]</b>\nBeing able to group objects together or even specify things about an object is possible with Object Tags. This feature " +
                        "is mostly used by modifiers, but can be used in other ways such as a \"DontRotate\" tag which prevents Player Shapes from rotating automatically.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_tags.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Locked [PATCHED]</b>\nIf on, prevents Beatmap Objects' start time from being changed. It's patched because unmodded PA doesn't " +
                        "have the toggle UI for this, however you can still use it in unmodded PA via hitting Ctrl + L.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Start Time [VANILLA]</b>\nUsed for when the Beatmap Object spawns.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_start_time.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Time of Death [VANILLA]</b>\nUsed for when the Beatmap Object despawns." +
                        "\n<b>[PATCHED]</b> No Autokill - Beatmap Objects never despawn. This option is viable in modded PA due to heavily optimized object code, so don't worry " +
                        "about having a couple of objects with this. Just make sure to only use this when necessary, like for backgrounds or a persistent character." +
                        "\n<b>[VANILLA]</b> Last KF - Beatmap Objects despawn once all animations are finished. This does NOT include parent animations. When the level " +
                        "time reaches after the last keyframe, the object despawns." +
                        "\n<b>[VANILLA]</b> Last KF Offset - Same as above but at an offset." +
                        "\n<b>[VANILLA]</b> Fixed Time - Beatmap Objects despawn at a fixed time, regardless of animations. Fixed time is Beatmap Objects Start Time with an offset added to it." +
                        "\n<b>[VANILLA]</b> Song Time - Same as above, except it ignores the Beatmap Object Start Time, despawning the object at song time.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Collapse [VANILLA]</b>\nBeatmap Objects in the editor timeline have their length shortened to the smallest amount if this is on.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_tod.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Parent Search [PATCHED]</b>\nHere you can search for an object to parent the Beatmap Object to. It includes Camera Parenting.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_parent_search.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Camera Parent [MODDED]</b>\nBeatmap Objects parented to the camera will always follow it, depending on the parent settings. This includes " +
                        "anything that makes the camera follow the player. This feature does exist in modern PA, but doesn't work the same way this does.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Clear Parent [MODDED]</b>\nClicking this will remove the Beatmap Object from its parent.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Parent Picker [ALPHA]</b>\nClicking this will activate a dropper. Right clicking will deactivate the dropper. Clicking on an object " +
                        "in the timeline will set the current selected Beatmap Objects parent to the selected Timeline Object.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Parent Display [VANILLA]</b>\nShows what the Beatmap Object is parented to. Clicking this button selects the parent. " +
                        "Hovering your mouse over it shows parent chain info in the Hover Info box.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_parent.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Parent Settings [PATCHED]</b>\nParent settings can be adjusted here. Each of the below settings refer to both " +
                        "position / scale / rotation. Position, scale and rotation are the rows and the types of Parent Settings are the columns.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Parent Type [VANILLA]</b>\nWhether the Beatmap Object applies this type of animation from the parent. " +
                        "It is the first column in the Parent Settings UI.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Parent Offset [VANILLA]</b>\nParent animations applied to the Beatmap Objects own parent chain get delayed at this offset. Normally, only " +
                        "the objects current parent gets delayed. It is the second column in the Parent Settings UI.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Parent Additive [MODDED]</b>\nForces Parent Offset to apply to every parent chain connected to the Beatmap Object. With this off, it only " +
                        "uses the Beatmap Objects' current parent. For example, say we have objects A, B, C and D. With this on, D delays the animation of every parent. With this off, it delays only C. " +
                        "It is the third column in the Parent Settings UI.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Parent Parallax [MODDED]</b>\nParent animations are multiplied by this amount, allowing for a parallax effect. Say the amount was 2 and the parent " +
                        "moves to position X 20, the object would move to 40 due to it being multiplied by 2. " +
                        "It is the fourth column in the Parent Settings UI.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_parent_more.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Origin [PATCHED]</b>\nOrigin is the offset applied to the visual of the Beatmap Object. Only usable for non-Empty object types. " +
                        "It's patched because of the number input fields instead of the direction buttons.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_origin.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Shape [PATCHED]</b>\nShape is whatever the visual of the Beatmap Object displays as. This doesn't just include actual shapes but stuff " +
                        "like text, images and player models too. More shape types and options were added. Unmodded PA does not include Image Shape, Pentagon Shape, Misc Shape, Player Shape.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_shape.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Render Depth [PATCHED]</b>\nDepth is how deep an object is in visual layers. Higher amount of Render Depth means the object is lower " +
                        "in the layers. Unmodded PA Legacy allows from 219 to -98. PA Alpha only allows from 40 to 0. Player is located at -60 depth. Z Axis Position keyframes use depth as a " +
                        "multiplied offset.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_depth.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Render Type [MODDED]</b>\nRender Type is if the visual of the Beatmap Object renders in the 2D layer or the 3D layer, aka Foreground / Background.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_render_type.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Layer [PATCHED]</b>\nLayer is what editor layer the Beatmap Object renders on. It can go as high as 2147483646. " +
                        "In unmodded PA its limited from layers 1 to 5, though in PA Editor Alpha another layer was introduced.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Bin [VANILLA]</b>\nBin is what row of the timeline the Beatmap Objects' timeline object renders on.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_editordata.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Object Debug [MODDED]</b>\nThis UI element only generates if UnityExplorer is installed. If it is, clicking on either button will inspect " +
                        "the internal data of the respective item.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_object_debug.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Integer Variable [MODDED]</b>\nEvery object has a whole number stored that Modifiers can use.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Modifiers [MODDED]</b>\nModifiers are made up of two different types: Triggers and Actions. " +
                            "Triggers check if a specified thing is happening and Actions do things depending on if any triggers are active or there aren't any. A detailed description of every modifier " +
                            "can be found in the Modifiers documentation. [WIP]", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_object_modifiers_edit.png", EditorDocument.Element.Type.Image),
            });

            GenerateDocument("[PATCHED] Beatmap Object Keyframes (WIP)", "The things that animate objects in different ways.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("The keyframes in the Beatmap Objects' keyframe timeline allow animating several aspects of a Beatmap Objects' visual.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>POSITION [PATCHED]</b>", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_pos_none.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_pos_normal.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_pos_toggle.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_pos_scale.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_pos_static_homing.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_pos_dynamic_homing.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>SCALE [VANILLA]</b>", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_sca_none.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_sca_normal.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_sca_toggle.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_sca_scale.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>ROTATION [VANILLA]</b>", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_rot_none.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_rot_normal.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_rot_toggle.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_rot_static_homing.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_rot_dynamic_homing.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>COLOR [PATCHED]</b>", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_col_none.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_col_dynamic_homing.png", EditorDocument.Element.Type.Image),
            });

            GenerateDocument("[PATCHED] Prefabs", "A package of objects that can be transfered from level to level. They can also be added to the level as a Prefab Object.", new List<EditorDocument.Element>
            {
                    new EditorDocument.Element("Prefabs are collections of objects grouped together for easy transfering from level to level.", EditorDocument.Element.Type.Text),
                    new EditorDocument.Element("<b>Name [VANILLA]</b>\nThe name of the Prefab. External prefabs gets saved with this as its file name, but all lowercase and " +
                        "spaces replaced with underscores.", EditorDocument.Element.Type.Text),
                    new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_pc_name.png", EditorDocument.Element.Type.Image),
                    new EditorDocument.Element("<b>Offset [VANILLA]</b>\nThe delay set to every Prefab Objects' spawned objects related to this Prefab.", EditorDocument.Element.Type.Text),
                    new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_pc_offset.png", EditorDocument.Element.Type.Image),
                    new EditorDocument.Element("<b>Type [PATCHED]</b>\nThe group name and color of the Prefab. Good for color coding what a Prefab does at a glance.", EditorDocument.Element.Type.Text),
                    new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_pc_type.png", EditorDocument.Element.Type.Image),
                    new EditorDocument.Element("<b>Description [MODDED]</b>\nA good way to tell you and others what the Prefab does or contains in great detail.", EditorDocument.Element.Type.Text),
                    new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_pc_description.png", EditorDocument.Element.Type.Image),
                    new EditorDocument.Element("<b>Seletion List [PATCHED]</b>\nShows every object, you can toggle the selection on any of them to add them to the prefab. All selected " +
                        "objects will be copied into the Prefab. This is patched because the UI and the code for it already existed in Legacy, it was just unused.", EditorDocument.Element.Type.Text),
                    new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_pc_search.png", EditorDocument.Element.Type.Image),
                    new EditorDocument.Element("<b>Create [MODDED]</b>\nApplies all data and copies all selected objects to a new Prefab.", EditorDocument.Element.Type.Text),
                    new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_pc_create.png", EditorDocument.Element.Type.Image),

            });

            GenerateDocument("[PATCHED] Prefab Objects (OUTDATED)", "Individual instances of prefabs that spawn the packed objects at specified offsets.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("Prefab Objects are a copied version of the original prefab, placed into the level. They take all the objects stored in the original prefab " +
                    "and add them to the level, meaning you can have multiple copies of the same group of objects. Editing the objects of the prefab by expanding it applies all changes to " +
                    "the prefab, updating every Prefab Object (once collapsed back into a Prefab Object).", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Expand [VANILLA]</b>\nExpands all the objects contained within the original prefab into the level and deletes the Prefab Object.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_expand.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Layer [PATCHED]</b>\nWhat Editor Layer the Prefab Object displays on. Can go from 1 to 2147483646. In unmodded Legacy its 1 to 5.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_layer.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Time of Death [MODDED]</b>\nTime of Death allows every object spawned from the Prefab Object still alive at a certain point to despawn." +
                    "\nRegular - Just how the game handles Prefab Objects kill time normally." +
                    "\nStart Offset - Kill time is offset plus the Prefab Object start time." +
                    "\nSong Time - Kill time is song time, so no matter where you change the start time to the kill time remains the same.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_tod.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Locked [PATCHED]</b>\nIf on, prevents Prefab Objects' start time from being changed. It's patched because unmodded PA doesn't " +
                    "have the toggle UI for this, however you can still use it in unmodded PA via hitting Ctrl + L.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Collapse [PATCHED]</b>\nIf on, collapses the Prefab Objects' timeline object. This is patched because it literally doesn't " +
                    "work in unmodded PA.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Start Time [VANILLA]</b>\nWhere the objects spawned from the Prefab Object start.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_time.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Position Offset [PATCHED]</b>\nEvery objects' top-most-parent has its position set to this offset. Unmodded PA technically has this " +
                    "feature, but it's not editable in the editor.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_pos_offset.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Scale Offset [PATCHED]</b>\nEvery objects' top-most-parent has its scale set to this offset. Unmodded PA technically has this " +
                    "feature, but it's not editable in the editor.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_sca_offset.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Rotation Offset [PATCHED]</b>\nEvery objects' top-most-parent has its rotation set to this offset. Unmodded PA technically has this " +
                    "feature, but it's not editable in the editor.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_rot_offset.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Repeat [MODDED]</b>\nWhen spawning the objects from the Prefab Object, every object gets repeated a set amount of times" +
                    "with their start offset added onto each time they repeat depending on the Repeat Offset Time set. The data for Repeat Count and Repeat Offset Time " +
                    "already existed in unmodded PA, it just went completely unused.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_repeat.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Speed [MODDED]</b>\nHow fast each object spawned from the Prefab Object spawns and is animated.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_speed.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Lead Time / Offset [VANILLA]</b>\nEvery Prefab Object starts at an added offset from the Offset amount. I have no idea why " +
                    "it's called Lead Time here even though its Offset everywhere else.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_lead.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Name [MODDED]</b>\nChanges the name of the original Prefab related to the Prefab Object. This is modded because you couldn't " +
                    "change this in the Prefab Object editor.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_name.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Type [MODDED]</b>\nChanges the Type of the original Prefab related to the Prefab Object. This is modded because you couldn't " +
                    "change this in the Prefab Object editor. (You can scroll-wheel over the input field to change the type easily)", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_type.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Save [MODDED]</b>\nSaves all changes made to the original Prefab to any External Prefab with a matching name.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_save.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Count [MODDED]</b>\nTells how many objects are in the original Prefab and how many Prefab Objects there are in the timeline " +
                    "for the Prefab. The Prefab Object Count goes unused for now...?", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_counts.png", EditorDocument.Element.Type.Image),
            });

            GenerateDocument("[LEGACY] Background Objects (WIP)", "The classic 3D style backgrounds.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("Background Object intro.", EditorDocument.Element.Type.Text),
            });

            GenerateDocument("[PATCHED] Events", "Effects to make your level pretty or to animate properties of the level.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("BetterLegacy has a total of 40 event types. Below is a list of them all and what they do:", EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>[VANILLA]</b> Move - Moves the camera left, right, up and down.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[VANILLA]</b> Position X\n" +
                                    "<b>[VANILLA]</b> Position Y", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[VANILLA]</b> Zoom - Zooms the camera in and out.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[VANILLA]</b> Zoom", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[VANILLA]</b> Rotate - Rotates the camera around.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[VANILLA]</b> Rotation", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[PATCHED]</b> Shake - Shakes the camera.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[VANILLA]</b> Shake Intensity\n" +
                                    "<b>[MODDED]</b> Direction X\n" +
                                    "<b>[MODDED]</b> Direction Y\n" +
                                    "<b>[MODDED]</b> Smoothness\n" +
                                    "<b>[MODDED]</b> Speed", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[PATCHED]</b> Theme - The current set of colors to use for a level.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[VANILLA]</b> Theme selection", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[VANILLA]</b> Chroma - Stretches the colors from the border of the screen.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[VANILLA]</b> Chromatic Aberation Amount", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[PATCHED]</b> Bloom - Applies a glowy effect on the screen.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[VANILLA]</b> Bloom Amount\n" +
                                    "<b>[ALPHA]</b> Diffusion\n" +
                                    "<b>[MODDED]</b> Threshold\n" +
                                    "<b>[MODDED]</b> Anamorphic Ratio\n" +
                                    "<b>[ALPHA]</b> Color\n" +
                                    "<b>[MODDED]</b> HSV (Hue / Saturation / Value)", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[PATCHED]</b> Vignette - Applies a dark / light border around the screen.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[VANILLA]</b> Intensity\n" +
                                    "<b>[VANILLA]</b> Smoothness\n" +
                                    "<b>[LEGACY]</b> Rounded\n" +
                                    "<b>[VANILLA]</b> Roundness\n" +
                                    "<b>[VANILLA]</b> Center Position\n" +
                                    "<b>[ALPHA]</b> Color\n" +
                                    "<b>[MODDED]</b> HSV (Hue / Saturation / Value)", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[PATCHED]</b> Lens - Pushes the center of the screen in and out.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[VANILLA]</b> Lens Distort Amount\n" +
                                    "<b>[ALPHA]</b> Center X\n" +
                                    "<b>[ALPHA]</b> Center Y\n" +
                                    "<b>[MODDED]</b> Intensity X\n" +
                                    "<b>[MODDED]</b> Intensity Y\n" +
                                    "<b>[MODDED]</b> Scale", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[VANILLA]</b> Grain - Adds a static effect to the screen (or makes it flash in Legacy if Grain size is 0.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[VANILLA]</b> Intensity Amount\n" +
                                    "<b>[VANILLA]</b> Size of Grains\n" +
                                    "<b>[VANILLA]</b> Colored", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[MODDED]</b> ColorGrading - Affects the colors of the whole level, regardless of theme. If the level is converted to the VG format, this event will be turned into the Hue event and will only have the Hueshift value.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[ALPHA]</b> Hueshift\n" +
                                    "<b>[MODDED]</b> Contrast\n" +
                                    "<b>[MODDED]</b> Saturation\n" +
                                    "<b>[MODDED]</b> Temperature\n" +
                                    "<b>[MODDED]</b> Tint\n" +
                                    "<b>[MODDED]</b> Gamma X\n" +
                                    "<b>[MODDED]</b> Gamma Y\n" +
                                    "<b>[MODDED]</b> Gamma Z\n" +
                                    "<b>[MODDED]</b> Gamma W", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[ALPHA]</b> Gradient - Applies a gradient over the screen. If the level is converted to the VG format, all values (except OHSV) will be carried over.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[ALPHA]</b> Intensity\n" +
                                    "<b>[ALPHA]</b> Rotation\n" +
                                    "<b>[ALPHA]</b> Color Top\n" +
                                    "<b>[MODDED]</b> OHSV Top (Opacity / Hue / Saturation / Value)\n" +
                                    "<b>[ALPHA]</b> Color Bottom\n" +
                                    "<b>[MODDED]</b> OHSV Bottom (Opacity / Hue / Saturation / Value)\n" +
                                    "<b>[ALPHA]</b> Mode", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[ALPHA]</b> Player Force - Forces the player to constantly move in a direction. If the level is converted to the VG format, all values will be carried over to the Player event.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[ALPHA]</b> Force X\n" +
                                    "<b>[ALPHA]</b> Force Y", EditorDocument.Element.Type.Text),
                // todo: add the rest
            });

            GenerateDocument("[PATCHED] Text Objects", "Flavor your levels with text!", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("Text Objects can be used in extensive ways, from conveying character dialogue to decoration. This document is for showcasing usable " +
                    "fonts and formats Text Objects can use.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>- FORMATTING -</b>", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[VANILLA]</b> <noparse><b></noparse> - For making text <b>BOLD</b>. Use <noparse></b></noparse> to clear.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[VANILLA]</b> <noparse><i></noparse> - For making text <i>italic</i>. Use <noparse></i></noparse> to clear.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[VANILLA]</b> <noparse><u></noparse> - Gives text an <u>underline</u>. Use <noparse></u></noparse> to clear.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[VANILLA]</b> <noparse><s></noparse> - Gives text a <s>strikethrough</s>. Use <noparse></s></noparse> to clear.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[VANILLA]</b> <br <pos=136>> - Line break.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[VANILLA]</b> <noparse><material=LiberationSans SDF - Outline></noparse> - Outline effect that only works on the <b><i>LiberationSans SDF</b></i> font.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[VANILLA]</b> <noparse><line-height=25></noparse> - Type any number you want in place of the 25. Use <noparse></line-height></noparse> to clear.", EditorDocument.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied material!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<material=LiberationSans SDF - Outline>");
                    }
                },

                new EditorDocument.Element("<b><size=36><align=center>- FONTS -</b>", EditorDocument.Element.Type.Text, 40f),
                new EditorDocument.Element(RTFile.BepInExAssetsPath + "Documentation/doc_fonts.png", EditorDocument.Element.Type.Image)
                { Function = () => { RTFile.OpenInFileBrowser.OpenFile(RTFile.ApplicationDirectory + RTFile.BepInExAssetsPath + "Documentation/doc_fonts.png"); }},
                new EditorDocument.Element("To use a font, do <font=Font Name>. To clear, do <noparse></font></noparse>. Click on one of the fonts below to copy the <font=Font Name> to your clipboard. " +
                    "Click on the image above to open the folder to the documentation assets folder where a higher resolution screenshot is located.", EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b><size=30><align=center>- PROJECT ARRHYTHMIA FONTS -</b>", EditorDocument.Element.Type.Text, 32f),
                FontElement(EditorDocument.SupportType.MODDED, "Arrhythmia", "The font from the earliest builds of Project Arrhythmia."),
                FontElement(EditorDocument.SupportType.MODDED, "Fredoka One", "The font used in the Vitamin Games website."),
                FontElement(EditorDocument.SupportType.MODDED, "Inconsolata Variable", "The default PA font."),
                FontElement(EditorDocument.SupportType.MODDED, "LiberationSans SDF", "An extra font Vanilla Legacy has."),
                FontElement(EditorDocument.SupportType.MODDED, "File Deletion", "A font pretty similar to the modern PA title font."),

                new EditorDocument.Element("<b><size=30><align=center>- GEOMETRY DASH FONTS -</b>", EditorDocument.Element.Type.Text, 32f),
                FontElement(EditorDocument.SupportType.MODDED, "Oxygene", "The font used for the title of Geometry Dash."),
                FontElement(EditorDocument.SupportType.MODDED, "Pusab", "The main font used in the hit game Geometry Dash. And yes, it is the right one."),
                FontElement(EditorDocument.SupportType.MODDED, "Minecraftory", "Geometry Dash font mainly used in Geometry Dash SubZero."),

                new EditorDocument.Element("<b><size=30><align=center>- COMIC FONTS -</b>", EditorDocument.Element.Type.Text, 32f),
                FontElement(EditorDocument.SupportType.MODDED, "Adam Warren Pro Bold", "A comic style font."),
                FontElement(EditorDocument.SupportType.MODDED, "Adam Warren Pro BoldItalic", "A comic style font."),
                FontElement(EditorDocument.SupportType.MODDED, "Adam Warren Pro", "A comic style font."),
                FontElement(EditorDocument.SupportType.MODDED, "BadaBoom BB", "A comic style font."),
                FontElement(EditorDocument.SupportType.MODDED, "Komika Hand", "A comic style font."),
                FontElement(EditorDocument.SupportType.MODDED, "Komika Hand Bold", "A comic style font."),
                FontElement(EditorDocument.SupportType.MODDED, "Komika Slim", "A comic style font."),
                FontElement(EditorDocument.SupportType.MODDED, "Komika Hand BoldItalic", "A comic style font."),
                FontElement(EditorDocument.SupportType.MODDED, "Komika Hand Italic", "A comic style font."),
                FontElement(EditorDocument.SupportType.MODDED, "Komika Jam", "A comic style font."),
                FontElement(EditorDocument.SupportType.MODDED, "Komika Jam Italic", "A comic style font."),
                FontElement(EditorDocument.SupportType.MODDED, "Komika Slick Italic", "A comic style font."),
                FontElement(EditorDocument.SupportType.MODDED, "Komika Slim Italic", "A comic style font."),

                new EditorDocument.Element("<b><size=30><align=center>- BIONICLE FONTS -</b>", EditorDocument.Element.Type.Text, 32f),
                FontElement(EditorDocument.SupportType.MODDED, "Matoran Language 1", "The language used by the Matoran in the BIONICLE series."),
                FontElement(EditorDocument.SupportType.MODDED, "Matoran Language 2", "The language used by the Matoran in the BIONICLE series."),
                FontElement(EditorDocument.SupportType.MODDED, "Piraka Theory", "The language used by the Piraka in the BIONICLE series."),
                FontElement(EditorDocument.SupportType.MODDED, "Piraka", "The language used by the Piraka in the BIONICLE series."),
                FontElement(EditorDocument.SupportType.MODDED, "Rahkshi", "The font used for promoting the Rahkshi sets in the BIONICLE series."),

                new EditorDocument.Element("<b><size=30><align=center>- UNDERTALE / DELTARUNE FONTS -</b>", EditorDocument.Element.Type.Text, 32f),
                FontElement(EditorDocument.SupportType.MODDED, "Monster Friend Back", "A font based on UNDERTALE's title."),
                FontElement(EditorDocument.SupportType.MODDED, "Monster Friend Fore", "A font based on UNDERTALE's title."),
                FontElement(EditorDocument.SupportType.MODDED, "Determination Mono", "The font UNDERTALE/deltarune uses for its interfaces."),
                FontElement(EditorDocument.SupportType.MODDED, "determination sans", "sans undertale."),
                FontElement(EditorDocument.SupportType.MODDED, "Determination Wingdings", "Beware the man who speaks in hands."),
                FontElement(EditorDocument.SupportType.MODDED, "Hachicro", "The font used by UNDERTALE's hit text."),

                new EditorDocument.Element("<b><size=30><align=center>- TRANSFORMERS FONTS -</b>", EditorDocument.Element.Type.Text, 32f),
                FontElement(EditorDocument.SupportType.MODDED, "Ancient Autobot", "The language used by ancient Autobots in the original Transformers cartoon."),
                FontElement(EditorDocument.SupportType.MODDED, "Revue", "The font used early 2000s Transformers titles."),
                FontElement(EditorDocument.SupportType.MODDED, "Revue 1", "The font used early 2000s Transformers titles."),
                FontElement(EditorDocument.SupportType.MODDED, "Transdings", "A font that contains a ton of Transformer insignias / logos. Below is an image featuring each letter of the alphabet."),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_tf.png", EditorDocument.Element.Type.Image),
                FontElement(EditorDocument.SupportType.MODDED, "Transformers Movie", "A font based on the Transformers movies title font."),

                new EditorDocument.Element("<b><size=30><align=center>- LANGUAGE SUPPORT FONTS -</b>", EditorDocument.Element.Type.Text, 32f),
                FontElement(EditorDocument.SupportType.MODDED, "Monomaniac One", "Japanese support font."),
                FontElement(EditorDocument.SupportType.MODDED, "Roboto Mono Bold", "Russian support font."),
                FontElement(EditorDocument.SupportType.MODDED, "Roboto Mono Italic", "Russian support font."),
                FontElement(EditorDocument.SupportType.MODDED, "Roboto Mono Light 1", "Russian support font."),
                FontElement(EditorDocument.SupportType.MODDED, "Roboto Mono Light Italic", "Russian support font."),
                FontElement(EditorDocument.SupportType.MODDED, "Roboto Mono Light Italic 1", "Russian support font."),
                FontElement(EditorDocument.SupportType.MODDED, "Roboto Mono Thin", "Russian support font."),
                FontElement(EditorDocument.SupportType.MODDED, "Roboto Mono Thin 1", "Russian support font."),
                FontElement(EditorDocument.SupportType.MODDED, "Roboto Mono Thin Italic", "Russian support font."),
                FontElement(EditorDocument.SupportType.MODDED, "Roboto Mono Thin Italic 1", "Russian support font."),
                FontElement(EditorDocument.SupportType.MODDED, "Pixellet", "A neat pixel font that supports Thai."),
                FontElement(EditorDocument.SupportType.MODDED, "Angsana Bold", "A font suggested by KarasuTori. Supports non-English languages like Thai."),
                FontElement(EditorDocument.SupportType.MODDED, "Angsana Italic", "A font suggested by KarasuTori. Supports non-English languages like Thai."),
                FontElement(EditorDocument.SupportType.MODDED, "Angsana Bold Italic", "A font suggested by KarasuTori. Supports non-English languages like Thai."),
                FontElement(EditorDocument.SupportType.MODDED, "VAG Rounded", "A font suggested by KarasuTori. Supports non-English languages like Russian."),

                new EditorDocument.Element("<b><size=30><align=center>- OTHER FONTS -</b>", EditorDocument.Element.Type.Text, 32f),
                FontElement(EditorDocument.SupportType.MODDED, "About Friend", "A font suggested by Ama."),
                FontElement(EditorDocument.SupportType.MODDED, "Flow Circular", "A fun line font suggested by ManIsLiS."),
                FontElement(EditorDocument.SupportType.MODDED, "Minecraft Text Bold", "The font used for the text UI in Minecraft."),
                FontElement(EditorDocument.SupportType.MODDED, "Minecraft Text BoldItalic", "The font used for the text UI in Minecraft."),
                FontElement(EditorDocument.SupportType.MODDED, "Minecraft Text", "The font used for the text UI in Minecraft."),
                FontElement(EditorDocument.SupportType.MODDED, "Nexa Book", "A font suggested by CubeCube."),
                FontElement(EditorDocument.SupportType.MODDED, "Nexa Bold", "A font suggested by CubeCube."),
                FontElement(EditorDocument.SupportType.MODDED, "Comic Sans", "You know the font."),
                FontElement(EditorDocument.SupportType.MODDED, "Comic Sans Bold", "You know the font."),
                FontElement(EditorDocument.SupportType.MODDED, "Comic Sans Hairline", "You know the font."),
                FontElement(EditorDocument.SupportType.MODDED, "Comic Sans Light", "You know the font."),
                FontElement(EditorDocument.SupportType.MODDED, "Sans Sans", "Sans Sans."),
                FontElement(EditorDocument.SupportType.MODDED, "Angsana Z", "Classical font."),
            });

            GenerateDocument("[MODDED] Player Editor (WIP)", "Time to get creative with creative players!", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("In BetterLegacy, you can create a fully customized Player Model.", EditorDocument.Element.Type.Text),
            });

            GenerateDocument("[PATCHED] Markers (OUTDATED)", "Organize and remember details about a level.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("Markers can organize certain parts of your level or help with aligning objects to a specific time.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("In the image below is two types of markers. The blue marker is the Audio Marker and the marker with a circle on the top is just a Marker. " +
                    "Left clicking on the Marker's circle knob moves the Audio Marker to the regular Marker. Right clicking the Marker's circle knob deletes it.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_marker_timeline.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Name [VANILLA]</b>\nThe name of the Marker. This renders next to the Marker's circle knob in the timeline.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_marker_name.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Time [VANILLA]</b>\nThe time the Marker renders at in the timeline.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_marker_time.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Description [PATCHED]</b>\nDescription helps you remember details about specific parts of a song or even stuff about the level you're " +
                    "editing. Typing setLayer(1) will set the editor layer to 1 when the Marker is selected. You can also have it be setLayer(events), setLayer(objects), setLayer(toggle), which " +
                    "sets the layer type to those respective types (toggle switches between Events and Objects layer types). Fun fact, the title for description in the UI in unmodded Legacy " +
                    "said \"Name\" lol.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_marker_description.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Colors [PATCHED]</b>\nWhat color the marker displays as. You can customize the colors in the Settings window.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_marker_colors.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Index [MODDED]</b>\nThe number of the Marker in the list.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_marker_index.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("On the right-hand-side of the Marker Editor window is a list of markers. At the top is a Search field and a Delete Markers button. " +
                    "Delete Markers clears every marker in the level and closes the Marker Editor.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_marker_delete.png", EditorDocument.Element.Type.Image),
            });

            GenerateDocument("[PATCHED] Title Bar (OUTDATED)", "The thing at the top of the editor UI with dropdowns.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("Title Bar has the main functions for loading, saving and editing.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_td.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>File [PATCHED]</b>" +
                    "\nPowerful functions related to the application or files." +
                    "\n<b>[VANILLA]</b> New Level - Creates a new level." +
                    "\n<b>[VANILLA]</b> Open Level - Opens the level list popup, where you can search and select a level to load." +
                    "\n<b>[VANILLA]</b> Open Level Folder - Opens the current loaded level's folder in your local file explorer." +
                    "\n<b>[MODDED]</b> Open Level Browser - Opens a built-in browser to open a level from anywhere on your computer." +
                    "\n<b>[MODDED]</b> Level Combiner - Combines multiple levels together." +
                    "\n<b>[VANILLA]</b> Save - Saves the current level." +
                    "\n<b>[PATCHED]</b> Save As - Saves a copy of the current level." +
                    "\n<b>[VANILLA]</b> Toggle Play Mode - Opens preview mode." +
                    "\n<b>[MODDED]</b> Switch to Arcade Mode - Switches to the handling of level loading in Arcade." +
                    "\n<b>[MODDED]</b> Quit to Arcade - Opens the Input Select scene just before loading arcade levels." +
                    "\n<b>[VANILLA]</b> Quit to Main Menu - Exits to the main menu." +
                    "\n<b>[VANILLA]</b> Quit Game - Quits the game.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_td_file.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Edit [PATCHED]</b>" +
                    "\nHow far can you edit in a modded editor?" +
                    "\n<b>[PATCHED]</b> Undo - Undoes the most recent action. Still heavily WIP. (sorry)" +
                    "\n<b>[PATCHED]</b> Redo - Same as above but goes back to the recent action when undone." +
                    "\n<b>[MODDED]</b> Search Objects - Search for specific objects by name or index. Hold Left Control to take yourself to the object in the timeline." +
                    "\n<b>[MODDED]</b> Preferences - Modify editor specific mod configs directly in the editor. Also known as Editor Properties." +
                    "\n<b>[MODDED]</b> Player Editor - Only shows if you have CreativePlayers installed. Opens the Player Editor." +
                    "\n<b>[MODDED]</b> View Keybinds - Customize the keybinds of the editor in any way you want.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_td_edit.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>View [PATCHED]</b>" +
                    "\nView specific things." +
                    "\n<b>[MODDED]</b> Get Example - Only shows if you have ExampleCompanion installed. It summons Example to the scene." +
                    "\n<b>[VANILLA]</b> Show Help - Toggles the Info box.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_td_view.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Steam [VANILLA]</b>" +
                    "\nView Steam related things... even though modded PA doesn't use Steam anymore lol" +
                    "\n<b>[VANILLA]</b> Open Workshop - Opens a link to the Steam workshop." +
                    "\n<b>[VANILLA]</b> Publish / Update Level - Opens the Metadata Editor / Level Uploader.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_td_steam.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Help [PATCHED]</b>" +
                    "\nGet some help." +
                    "\n<b>[MODDED]</b> Modder's Discord - Opens a link to the mod creator's Discord server." +
                    "\n<b>[MODDED]</b> Watch PA History - Since there are no <i>modded</i> guides yet, this just takes you to the System Error BTS PA History playlist." +
                    "\n<b>[MODDED]</b> Wiki / Documentation - In-editor documentation of everything the game has to offer. You're reading it right now!", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_td_help.png", EditorDocument.Element.Type.Image),
            });

            GenerateDocument("[PATCHED] Timeline Bar (OUTDATED)", "The main toolbar used for editing main editor things such as audio time, editor layers, etc.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("The Timeline Bar is where you can see and edit general game and editor info.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Audio Time (Precise) [MODDED]</b>\nText shows the precise audio time. This can be edited to set a specific time for the audio.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Audio Time (Formatted) [VANILLA]</b>\nText shows the audio time formatted like \"minutes.seconds.milliseconds\". Clicking this sets the " +
                    "audio time to 0.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Pause / Play [VANILLA]</b>\nPressing this toggles if the song is playing or not.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Pitch [PATCHED]</b>\nThe speed of the song. Clicking the buttons adjust the pitch by 0.1, depending on the direction the button is facing.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_tb_audio.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Editor Layer [PATCHED]</b>\nEditor Layer is what objects show in the timeline, depending on their own Editor Layer. " +
                    "It can go as high as 2147483646. In unmodded PA its limited from layers 1 to 5, though in PA Editor Alpha another layer was introduced.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Editor Layer Type [MODDED]</b>\nWhether the timeline shows objects or event keyframes / checkpoints.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_tb_layer.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Prefab [VANILLA]</b>\nOpens the Prefab list popups (Internal & External).", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Object [PATCHED]</b>\nOpens a popup featuring different object templates such as Decoration, Empty, etc. It's patched because " +
                    "Persistent was replaced with No Autokill.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Marker [VANILLA]</b>\nCreates a Marker.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>BG [VANILLA]</b>\nOpens a popup to open the BG editor or create a new BG.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_tb_create.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Preview Mode [VANILLA]</b>\nSwitches the game to Preview Mode.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_tb_preview_mode.png", EditorDocument.Element.Type.Image),
            });

            GenerateDocument("[MODDED] Keybinds (WIP)", "Perform specific actions when pressing set keys.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("Keybinds intro.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_keybind_list.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_keybind_editor.png", EditorDocument.Element.Type.Image),
            });

            GenerateDocument("[MODDED] Object Modifiers", "Make your levels dynamic!", new List<EditorDocument.Element>
            {
                //new EditorDocument.Element("BetterLegacy adds a trigger / action based system to Beatmap Objects called \"Modifiers\". " +
                //    "Modifiers have two types: Triggers check if something is happening and if it is, it activates any Action type modifiers. If there are no Triggers, then the Action modifiers " +
                //    "activates. This document is heavily WIP and will be added to over time.", EditorDocument.Element.Type.Text),
                //new EditorDocument.Element("<b>setPitch</b> - Modifies the speed of the game and the pitch of the audio. It sets a multiplied offset from the " +
                //    "audio keyframe's pitch value. However unlike the event keyframe, setPitch can go into the negatives allowing for reversed audio.", EditorDocument.Element.Type.Text),
                //new EditorDocument.Element("<b>addPitch</b> - Does the same as above, except adds to the pitch offset.", EditorDocument.Element.Type.Text),
                //new EditorDocument.Element("<b>setMusicTime</b> - Sets the Audio Time to go to any point in the song, allowing for skipping specific sections of a song.", EditorDocument.Element.Type.Text),
                //new EditorDocument.Element("<b>playSound</b> - Plays an external sound. The following details what each value in the modifier does." +
                //    "\nPath - If global is on, path should be set to something within beatmaps/soundlibrary directory. If global is off, then the path should be set to something within the level " +
                //    "folder that has level.lsb and metadata.lsb." +
                //    "\nGlobal - Affects the above setting in the way described." +
                //    "\nPitch - The speed of the sound played." +
                //    "\nVolume - How loud the sound is." +
                //    "\nLoop - If the sound should loop while the Modifier is active.", EditorDocument.Element.Type.Text),
                //new EditorDocument.Element("<b>playSoundOnline</b> - Same as above except plays from a link. The global toggle does nothing here.", EditorDocument.Element.Type.Text),
                //new EditorDocument.Element("<b>loadLevel</b> - Loads a level from the current level folder path.", EditorDocument.Element.Type.Text),
                //new EditorDocument.Element("<b>loadLevelInternal</b> - Same as above, except it always loads from the current levels own path.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Some objects in BetterLegacy have a list of \"modifiers\" that can be used to affect that object in a lot of different ways. This documentation only documents Beatmap Object Modifiers, but some other objects have them too.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>TRIGGERS</b>\n" +
                                        "These types of modifiers will check if something is happening and if it is, will allow other modifiers in the list to run.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>ACTIONS</b>\n" +
                                        "Action modifiers modify the level or object if it runs.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("If an object is active or \"Ignore Lifespan\" is turned on, the modifier loop will run.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("With \"Order Matters\" off, it requires all trigger modifiers to be active to run the action modifiers, regardless of order.\n" +
                                "With it on, it considers the order of the modifiers. It'll check triggers up until it hits an action and if all " +
                                "(or some, depends on the else if toggle) triggers are active, then it'll run the next set of action modifiers. Below is an example:", EditorDocument.Element.Type.Text),
                new EditorDocument.Element(
                                "- Action 1 (runs regardless of triggers since there are no triggers before it)\n\n" +
                                "- Trigger 1\n" +
                                "- Trigger 2\n" +
                                "- Action 2 (runs only if Triggers 1 and 2 are triggered)\n\n" +
                                "- Trigger 3\n" +
                                "- Action 3 (runs only if Trigger 3 is triggered. Doesn't care about Triggers 1 and 2)\n\n" +
                                "- Trigger 4\n" +
                                "- Trigger 5 (else if)\n" +
                                "- Action 4 (runs only if Trigger 4 or 5 are triggered. Doesn't care about triggers before Action 3)\n" +
                                "- Action 5 (same as Action 4, this just shows you can have multiple actions after a set of triggers)"
                                , EditorDocument.Element.Type.Text, 370f),
                new EditorDocument.Element("Modifiers can be told to only run once whenever it runs by turning \"Constant\" off. Some modifiers have specific behavior with it on / off.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Triggers can be turned into a \"Not Gate\" by turning \"Not\" on. This will require the triggers' check to be the opposite of what it would normally be.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Triggers also have an \"else if\" toggle that will optionally be checked if the previous triggers were not triggered.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Some modifiers support affecting a group of Beatmap Objects if they're being used for such. You can customize if the group should only be within the Prefab the object spawned from by turning \"Prefab Group Only\" on.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("You can organize modifiers in the editor by right clicking them and using the \"Move\" functions.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Below is a list of a few modifier groups and what they generally do. Does not include every modifier as there's far too many to count (300+).", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>AUDIO MODIFIERS</b>\n" +
                                        "These all have some relevance to the current audio, or its own audio.\n" +
                                        "- setPitch (Action)\n" +
                                        "- addPitch (Action)\n" +
                                        "- setMusicTime (Action)\n" +
                                        "- playSound (Action)\n" +
                                        "- audioSource (Action)\n" +
                                        "- pitchEquals (Trigger)\n" +
                                        "- musicTimeGreater (Trigger)\n" +
                                        "- musicTimeLesser (Trigger)\n" +
                                        "- musicPlaying (Trigger)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>LEVEL MODIFIERS</b>\n" +
                                        "Not to be confused with the Level Modifiers that can be converted to Alpha level triggers. These modifiers have a connection with the level or multiple in some way.\n" +
                                        "- loadLevelID (Action)\n" +
                                        "- endLevel (Action)\n" +
                                        "- inZenMode (Trigger)\n" +
                                        "- inNormal (Trigger)\n" +
                                        "- levelRankEquals (Trigger)\n" +
                                        "- levelCompleted (Trigger)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>COMPONENT MODIFIERS</b>\n" +
                                        "These act as a component that directly modifies how the object looks / works.\n" +
                                        "- blur (Action)\n" +
                                        "- blurColored (Action)\n" +
                                        "- doubleSided (Action)\n" +
                                        "- particleSystem (Action)\n" +
                                        "- trailRenderer (Action)\n" +
                                        "- rigidbody (Action) < Required for objectCollide triggers to work.\n" +
                                        "- gravity (Action)\n" +
                                        "- objectCollide (Trigger)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>PREFAB MODIFIERS</b>\n" +
                                        "These can spawn / despawn Prefabs directly into a level.\n" +
                                        "- spawnPrefab (Action)\n" +
                                        "- spawnPrefabOffset (Action)\n" +
                                        "- spawnMultiPrefab (Action)\n" +
                                        "- spawnMultiPrefabOffset (Action)\n" +
                                        "- clearSpawnedPrefabs (Action)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>PLAYER MODIFIERS</b>\n" +
                                        "Again, not to be confused with the Player Modifiers that Player Models use. These affect the Players.\n" +
                                        "- playerHeal (Action)\n" +
                                        "- playerHit (Action)\n" +
                                        "- playerKill (Action)\n" +
                                        "- playerBoost (Action)\n" +
                                        "- gameMode (Action)\n" +
                                        "- setPlayerModel (Action)\n" +
                                        "- blackHole (Action)\n" +
                                        "- playerCollide (Trigger)\n" +
                                        "- playerHealthEquals (Trigger)\n" +
                                        "- playerDeathsEquals (Trigger)\n" +
                                        "- playerBoosting (Trigger)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>VARIABLE MODIFIERS</b>\n" +
                                        "Stores and compares variables for use across the level.\n" +
                                        "- addVariable (Action)\n" +
                                        "- setVariable (Action)\n" +
                                        "- variableEquals (Trigger)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>ENABLE / DISABLE MODIFIERS</b>\n" +
                                        "Hides / unhides an object.\n" +
                                        "- enableObject (Action)\n" +
                                        "- enableObjectTree (Action)\n" +
                                        "- disableObject (Action)\n" +
                                        "- disableObjectTree (Action)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>REACTIVE MODIFIERS</b>\n" +
                                        "Makes an object react to the audio, like the Background Objects do already.\n" +
                                        "- reactivePosChain (Action)\n" +
                                        "- reactiveScaChain (Action)\n" +
                                        "- reactiveRotChain (Action)\n" +
                                        "- reactiveCol (Action)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>EVENT MODIFIERS</b>\n" +
                                        "Offsets Event Keyframes.\n" +
                                        "- eventOffset (Action)\n" +
                                        "- eventOffsetAnimate (Action)\n" +
                                        "- vignetteTracksPlayer (Action)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>COLOR MODIFIERS</b>\n" +
                                        "The color of the object.\n" +
                                        "- lerpColor (Action)\n" +
                                        "- setAlpha (Action)\n" +
                                        "- copyColor (Action)\n" +
                                        "- setColorHex (Action)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>TEXT MODIFIERS</b>\n" +
                                        "The text of a text object can be changed with these.\n" +
                                        "- setText (Action)\n" +
                                        "- formatText (Action)\n" +
                                        "- textSequence (Action)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>ANIMATION MODIFIERS</b>\n" +
                                        "Dynamically (or statically) animate objects in various ways.\n" +
                                        "- animateObject (Action)\n" +
                                        "- applyAnimation (Action)\n" +
                                        "- copyAxis (Action)\n" +
                                        "- legacyTail (Action)\n" +
                                        "- axisEquals (Trigger)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>SHAPE MODIFIERS</b>\n" +
                                        "Affects the shape of the object.\n" +
                                        "- translateShape (Action)\n" +
                                        "- backgroundShape (Action)\n" +
                                        "- sphereShape (Action)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>INPUT MODIFIERS</b>\n" +
                                        "Set of triggers related to keyboards, controllers and the mouse cursor.\n" +
                                        "- keyPressDown (Trigger)\n" +
                                        "- controlPressDown (Trigger)\n" +
                                        "- mouseButtonDown (Trigger)\n" +
                                        "- mouseOver (Trigger)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>MISC MODIFIERS</b>\n" +
                                        "These don't really fit into a category but are interesting enough to let you know they exist.\n" +
                                        "- setWindowTitle (Action)\n" +
                                        "- setDiscordStatus (Action)\n" +
                                        "- loadInterface (Action)\n" +
                                        "- disableModifier (Trigger)\n"
                                        , EditorDocument.Element.Type.Text),
            });

            GenerateDocument("[MODDED] Math Evaluation", "Some places you can write out a math equation and get a result from it.", new List<EditorDocument.Element>
            {
                LinkElement("Math Evaluation is implemented from ILMath, created by Reimnop. If you want to know more, visit the link: https://github.com/Reimnop/ILMath", "https://github.com/Reimnop/ILMath"),
                new EditorDocument.Element("Below is a list of variables that can be used with math evaluation.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>deathCount</b> - Amount of deaths in a level (Arcade only).\n" +
                                     "<b>hitCount</b> - Amount of hits in a level (Arcade only).\n" +
                                     "<b>actionMoveX</b> - WASD / joystick move.\n" +
                                     "<b>actionMoveY</b> - WASD / joystick move.\n" +
                                     "<b>time</b> - Unity's time property. (seconds since game start)\n" +
                                     "<b>deltaTime</b> - Unity's deltaTime property.\n" +
                                     "<b>audioTime</b> - The main audio time.\n" +
                                     "<b>smoothedTime</b> - Catalyst animation time.\n" +
                                     "<b>volume</b> - Current music volume.\n" +
                                     "<b>pitch</b> - Current music pitch (or game speed).\n" +
                                     "<b>forwardPitch</b> - Current music pitch (or game speed) but always above 0.001.\n" +
                                     "<b>camPosX</b> - Position X of the camera.\n" +
                                     "<b>camPosY</b> - Position Y of the camera.\n" +
                                     "<b>camZoom</b> - Zoom of the camera.\n" +
                                     "<b>camRot</b> - Rotation of the camera.\n" +
                                     "<b>player0PosX</b> - Position X of a specific player. You can swap the 0 out with a different number to get a different players' position X. If that number is higher than or equal to the player count, the result will be 0.\n" +
                                     "<b>player0PosY</b> - Position Y of a specific player. You can do the same as the above variable.\n" +
                                     "<b>player0Rot</b> - Rotation of a specific player. You can do the same as the above variable.\n" +
                                     "<b>player0Health</b> - Health of a specific player. You can do the same as the above variable.\n" +
                                     "<b>playerHealthTotal</b> - Health of all players in total.\n" +
                                     "<b>mousePosX</b> - Position X of the mouse cursor.\n" +
                                     "<b>mousePosY</b> - Position Y of the mouse cursor.\n" +
                                     "<b>screenWidth</b> - Width of the Application Window.\n" +
                                     "<b>screenHeight</b> - Height of the Application Window.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("If you have a few functions listed, follow this example:\n" +
                                    "clamp(random(), 0, 1) + clamp(random(034) * 0.1, 0, 1) * pitch"
                                    , EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Below is a list of functions that can be used with math evaluation.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element(
                                    "<b>sin(value)</b> - sin function.\n" +
                                    "<b>cos(value)</b> - cos function.\n" +
                                    "<b>atan(value)</b> - atan function.\n" +
                                    "<b>tan(value)</b> - tan function.\n" +
                                    "<b>asin(value)</b> - asin function.\n" +
                                    "<b>sqrt(value)</b> - sqrt function.\n" +
                                    "<b>abs(value)</b> - abs function.\n" +
                                    "<b>min(a, b)</b> - Limits a to always be below b.\n" +
                                    "<b>max(a, b)</b> - Limits a to always be above b.\n" +
                                    "<b>clamp(value, min, max)</b> - Limits the value to always be between min and max.\n" +
                                    "<b>clampZero(value, min, max)</b> - If both min and max are zero, then it will not clamp.\n" +
                                    "<b>pow(f, p)</b> - pow function.\n" +
                                    "<b>exp(power)</b> - exponent function.\n" +
                                    "<b>log(f)</b> - log function.\n" +
                                    "<b>log(f, p)</b> - log function.\n" +
                                    "<b>log10(f)</b> - log function.\n" +
                                    "<b>ceil(f)</b> - ceiling function.\n" +
                                    "<b>floor(f)</b> - floor function.\n" +
                                    "<b>round(f)</b> - Rounds 'f' to the nearest whole number.\n" +
                                    "<b>sign(f)</b> - sign function.\n" +
                                    "<b>lerp(a, b, t)</b> - Interpolates between a (start) and b (end) depending on t (time).\n" +
                                    "<b>lerpAngle(a, b, t)</b> - Interpolates between a (start) and b (end) depending on t (time).\n" +
                                    "<b>inverseLerp(a, b, t)</b> - Interpolates between a (start) and b (end) depending on t (time).\n" +
                                    "<b>moveTowards(current, target, maxDelta)</b> - Moves current towards the target depending on maxDelta.\n" +
                                    "<b>moveTowardsAngle(current, target, maxDelta)</b> - Moves current towards the target depending on maxDelta.\n" +
                                    "<b>smoothStep(from, to, t)</b> - Like lerp, except smoothly interpolates.\n" +
                                    "<b>gamma(value, absmax, gamma)</b> - gamma function.\n" +
                                    "<b>approximately(a, b)</b> - Checks if a and b are almost or fully equal.\n" +
                                    "<b>repeat(t, length)</b> - Repeats t by length.\n" +
                                    "<b>pingPong(t, length)</b> - Ping-pongs between t by length.\n" +
                                    "<b>deltaAngle(current, target)</b> - Calculates an angle from current to target.\n" +
                                    "<b>random()</b> - Gets a random seed and returns a non-whole number by the seed.\n" +
                                    "<b>random(seed)</b> - Returns a non-whole number by the seed. Example: random(4104) will always result in the same random number.\n" +
                                    "<b>random(seed, index)</b> - Returns a consistent non-whole number by the seed and index.\n" +
                                    "<b>randomRange(seed, min, max)</b> - Returns a non-whole number between min and max by the seed.\n" +
                                    "<b>randomRange(seed, min, max, index)</b> - Returns a consistent non-whole number between min and max by the seed and index.\n" +
                                    "<b>randomInt()</b> - Gets a random seed and returns a whole number by the seed. Example: random(4104) will always result in the same random number.\n" +
                                    "<b>randomInt(seed)</b> - Returns a whole number by the seed.\n" +
                                    "<b>randomInt(seed, index)</b> - Returns a consistent whole number by the seed and index.\n" +
                                    "<b>randomRangeInt(seed, min, max)</b> - Returns a whole number between min and max by the seed.\n" +
                                    "<b>randomRangeInt(seed, min, max, index)</b> - Returns a consistent whole number between min and max by the seed and index.\n" +
                                    "<b>roundToNearestNumber(value, multipleOf)</b> - Rounds the value to the nearest 'multipleOf'.\n" +
                                    "<b>roundToNearestDecimal(value, places)</b> - Ensures a specific decimal length. E.g. value is 0.51514656 and places 3 would make the value 0.515.\n" +
                                    "<b>percentage(t, length)</b> - Calculates a percentage (100%) from t and length.\n" +
                                    "<b>equals(a, b, trueResult, falseResult)</b> - If a and b are equal, then the function returns the trueResult, otherwise returns the falseResult.\n" +
                                    "<b>lesserEquals(a, b, trueResult, falseResult)</b> - If a and b are lesser or equal, then the function returns the trueResult, otherwise returns the falseResult.\n" +
                                    "<b>greaterEquals(a, b, trueResult, falseResult)</b> - If a and b are greater or equal, then the function returns the trueResult, otherwise returns the falseResult.\n" +
                                    "<b>lesser(a, b, trueResult, falseResult)</b> - If a and b are lesser, then the function returns the trueResult, otherwise returns the falseResult.\n" +
                                    "<b>greater(a, b, trueResult, falseResult)</b> - If a and b are greater, then the function returns the trueResult, otherwise returns the falseResult.\n" +
                                    "<b>findAxis#object_group(int fromType, int fromAxis, float time)</b> - Finds an object with the matching tag, takes an axis from fromType and fromAxis and interpolates it via time.\n" +
                                    "<b>findOffset#object_group(int fromType, int fromAxis)</b> - Finds an object with the matching tag and takes an axis from fromType and fromAxis.\n" +
                                    "<b>findObject#object_group#Property()</b> - Finds an object with the matching tag and a property of the object.\n" +
                                    "Acceptible properties:\n" +
                                    "StartTime\n" +
                                    "Depth\n" +
                                    "IntVariable\n" +
                                    "<b>findInterpolateChain#object_group(float time, int type int axis, int includeDepth [0 = false 1 = true], int includeOffsets [0 = false 1 = true], int includeSelf [0 = false 1 = true])</b> - Finds an object with the matching tag and interpolates its full animation value. If type is not position aka type = 0, then don't have includeDepth in the parameters.\n" +
                                    "<b>easing#curveType(t)</b> - Calculates an easing from the easing list. \"curveType\" is easings such as \"Linear\", \"Instant\", etc.\n" +
                                    "<b>int(value)</b> - Casts floating point numbers (non-whole numbers) into integers (whole numbers).\n" +
                                    "<b>sampleAudio(int sample, float intensity)</b> - Takes a sample of the currently playing audio.\n" +
                                    "<b>vectorAngle(float firstX, float firstY, float firstZ, float secondX, float secondY, float secondZ)</b> - Calculates rotation where first would be looking at second.\n" +
                                    "<b>distance(float firstX, [optional] float firstY, float [optional] firstZ, float secondX, [optional] float secondY, [optional] float secondZ)</b> - Calculates the distance between first and second. Y and Z values are optional.\n" +
                                    "<b>date#format()</b> - Takes a specific part of the current date and uses it.\n" +
                                    "Acceptable formats:\n" +
                                    "yyyy = full year (e.g. 2019)\n" +
                                    "yy = decade year (e.g. 19)\n" +
                                    "MM = month (e.g. 06)\n" +
                                    "dd = day (e.g. 15)\n" +
                                    "HH = 24 hour (e.g. 13)\n" +
                                    "hh = 12 hour (e.g. 1)\n" +
                                    "mm = minute (e.g. 59)\n" +
                                    "ss = second (e.g. 12)\n" +
                                    "<b>mirrorNegative(float value)</b> - ensures the value is always a positive number. If it's lesser than 0, then it will reverse the number to positive. E.G: -4.2 to 4.2.\n" +
                                    "<b>mirrorPositive(float value)</b> - ensures the value is always a negative number. If it's greater than 0, then it will reverse the number to negative. E.G: 4.2 to -4.2.\n" +
                                    "<b>worldToViewportPointX(float x, [optional] float y, [optional] float z)</b> translates a position to the camera viewport and gets the X value. Y and Z values are optional.\n" +
                                    "<b>worldToViewportPointY(float x, [optional] float y, [optional] float z)</b> translates a position to the camera viewport and gets the Y value. Y and Z values are optional.\n" +
                                    "<b>worldToViewportPointZ(float x, [optional] float y, [optional] float z)</b> translates a position to the camera viewport and gets the Z value. Y and Z values are optional.\n", EditorDocument.Element.Type.Text),
            });

            //DateTime.Now.ToString("ff"); // yes
            //DateTime.Now.ToString("fff"); // yes
            //DateTime.Now.ToString("ffff"); // yes
            //DateTime.Now.ToString("fffff"); // yes
            //DateTime.Now.ToString("ffffff"); // yes
            //DateTime.Now.ToString("fffffff"); // yes
            //DateTime.Now.ToString("FF"); // yes
            //DateTime.Now.ToString("FFF"); // yes
            //DateTime.Now.ToString("FFFF"); // yes
            //DateTime.Now.ToString("FFFFF"); // yes
            //DateTime.Now.ToString("FFFFFF"); // yes
            //DateTime.Now.ToString("FFFFFFF"); // yes
            //DateTime.Now.ToString("yyyy"); // yes = year full
            //DateTime.Now.ToString("yy"); // yes = year decade
            //DateTime.Now.ToString("MM"); // yes = month
            //DateTime.Now.ToString("dd"); // yes = day
            //DateTime.Now.ToString("HH"); // yes = 24 hour
            //DateTime.Now.ToString("hh"); // yes = 12 hour
            //DateTime.Now.ToString("mm"); // yes = minute
            //DateTime.Now.ToString("ss"); // yes = second

            GenerateDocument("Misc (OUTDATED)", "The stuff that didn't fit in a document of its own.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("<b>Editor Level Path [MODDED]</b>\nThe path within the Project Arrhythmia/beatmaps directory that is used for the editor level list.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Refresh [MODDED]</b>\nRefreshes the editor level list.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Descending [MODDED]</b>\nIf the editor level list should be descending or ascending.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Order [MODDED]</b>\nHow the editor level list should be ordered." +
                    "\nCover - Order by if the level has a cover or not." +
                    "\nArtist - Order by Artist Name." +
                    "\nCreator - Order by Creator Name." +
                    "\nFolder - Order by Folder Name." +
                    "\nTitle - Order by Song Title." +
                    "\nDifficulty - Order by (Easy, Normal, Hard, Expert, Expert+, Master, Animation)" +
                    "\nDate Edited - Order by last saved time, so recently edited levels appear at one side and older levels appear at the other.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_open_level_top.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Loading Autosaves [MODDED]</b>\nHolding shift when you click on a level in the level list will open an Autosave popup instead of " +
                    "loading the level. This allows you to load any autosaved file so you don't need to go into the level folder and change one of the autosaves to the level.lsb.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_autosaves.png", EditorDocument.Element.Type.Image),
            });

            if (CoreHelper.AprilFools)
            {
                var elements = new List<EditorDocument.Element>();

                elements.Add(new EditorDocument.Element("oops, i spilled my images everywhere...", EditorDocument.Element.Type.Text));

                var dir = Directory.GetFiles(RTFile.ApplicationDirectory, "*.png", SearchOption.AllDirectories);

                for (int i = 0; i < UnityEngine.Random.Range(0, Mathf.Clamp(dir.Length, 0, 20)); i++)
                    elements.Add(new EditorDocument.Element(dir[UnityEngine.Random.Range(0, dir.Length)].Replace("\\", "/").Replace(RTFile.ApplicationDirectory, ""), EditorDocument.Element.Type.Image));

                GenerateDocument("April Fools!", "fol.", elements);
            }
        }

        public GameObject GenerateDebugButton(string name, string hint, Action action)
        {
            var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(debuggerPopup.Content, "Function");
            debugs.Add(name);

            gameObject.AddComponent<HoverTooltip>().tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                desc = name,
                hint = hint
            });

            var button = gameObject.GetComponent<Button>();
            button.onClick.ClearAll();
            button.onClick.AddListener(() => { action?.Invoke(); });

            EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
            var text = gameObject.transform.GetChild(0).GetComponent<Text>();
            text.text = name;
            EditorThemeManager.ApplyLightText(text);
            return gameObject;
        }

        void CreateDebug()
        {
            if (!ModCompatibility.UnityExplorerInstalled)
                return;

            debuggerPopup = GeneratePopup("Debugger Popup", "Debugger (Only use this if you know what you're doing)", Vector2.zero, new Vector2(600f, 450f), _val =>
            {
                debugSearch = _val;
                RefreshDebugger();
            }, placeholderText: "Search for function...");

            var reload = GameObject.Find("TimelineBar/GameObject/play")
                .Duplicate(debuggerPopup.TopPanel, "reload");
            UIManager.SetRectTransform(reload.transform.AsRT(), new Vector2(-42f, 0f), Vector2.one, Vector2.one, Vector2.one, new Vector2(32f, 32f));

            (reload.GetComponent<HoverTooltip>() ?? reload.AddComponent<HoverTooltip>()).tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                desc = "Refresh the function list",
                hint = "Clicking this will reload the function list."
            });

            var reloadButton = reload.GetComponent<Button>();
            reloadButton.onClick.ClearAll();
            reloadButton.onClick.AddListener(() => { ReloadFunctions(); });

            EditorThemeManager.AddSelectable(reloadButton, ThemeGroup.Function_2, false);

            reloadButton.image.sprite = ReloadSprite;

            EditorHelper.AddEditorDropdown("Debugger", "", "View", SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}debugger.png"), () =>
            {
                EditorManager.inst.ShowDialog("Debugger Popup");
                RefreshDebugger();
            });

            EditorHelper.AddEditorDropdown("Show Explorer", "", "View", SearchSprite, ModCompatibility.ShowExplorer);

            GenerateDebugButton(
                "Inspect DataManager",
                "DataManager is a pretty important storage component of Project Arrhythmia. It contains the GameData, all the external Beatmap Themes, etc.",
                () => { ModCompatibility.Inspect(DataManager.inst); });

            GenerateDebugButton(
                "Inspect EditorManager",
                "EditorManager handles the main unmodded editor related things.",
                () => { ModCompatibility.Inspect(EditorManager.inst); });

            GenerateDebugButton(
                "Inspect RTEditor",
                "EditorManager handles the main modded editor related things.",
                () => { ModCompatibility.Inspect(inst); });

            GenerateDebugButton(
                "Inspect ObjEditor",
                "ObjEditor is the component that handles regular object editor stuff.",
                () => { ModCompatibility.Inspect(ObjEditor.inst); });

            GenerateDebugButton(
                "Inspect ObjectEditor",
                "ObjectEditor is the component that handles modded object editor stuff.",
                () => { ModCompatibility.Inspect(ObjectEditor.inst); });

            GenerateDebugButton(
                "Inspect ObjectManager",
                "ObjectManager is the component that handles regular object stuff.",
                () => { ModCompatibility.Inspect(ObjectManager.inst); });

            GenerateDebugButton(
                "Inspect GameManager",
                "GameManager normally handles all the level loading, however now it's handled by LevelManager.",
                () => { ModCompatibility.Inspect(GameManager.inst); });

            GenerateDebugButton(
                "Inspect Example",
                "ExampleManager handles everything to do with Example, your little companion.",
                () => { ModCompatibility.Inspect(ExampleManager.inst); });

            GenerateDebugButton(
                "Inspect Object Editor UI",
                "Take a closer look at the Object Editor UI since the parent tree for it is pretty deep.",
                () => { ModCompatibility.Inspect(ObjEditor.inst.ObjectView); });

            GenerateDebugButton(
                "Inspect LevelProcessor",
                "LevelProcessor is the main handler for updating object animation and spawning / despawning objects.",
                () => { ModCompatibility.Inspect(Updater.levelProcessor); });

            GenerateDebugButton(
                "Inspect Current GameData",
                "GameData stores all the main level data.",
                () => { ModCompatibility.Inspect(GameData.Current); });

            GenerateDebugButton(
                "Inspect Current MetaData",
                "MetaData stores all the extra level info.",
                () => { ModCompatibility.Inspect(MetaData.Current); });

            GenerateDebugButton(
                "Current Event Keyframe",
                "The current selected Event Keyframe. Based on the type and index number.",
                () => { ModCompatibility.Inspect(RTEventEditor.inst.CurrentSelectedKeyframe); });

            ReloadFunctions();
        }

        void ReloadFunctions()
        {
            var functions = RTFile.ApplicationDirectory + "beatmaps/functions";
            if (!RTFile.DirectoryExists(functions))
                return;

            customFunctions.ForEach(x => Destroy(x));
            customFunctions.Clear();
            debugs.RemoveAll(x => x.Contains("Custom Code Function"));

            var files = Directory.GetFiles(functions, "*.cs");
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];

                customFunctions.Add(GenerateDebugButton(
                    $"Custom Code Function: {Path.GetFileName(file)}",
                    "A custom code file. Make sure you know what you're doing before using this.",
                    () =>
                    {
                        var hadError = false;
                        Exception exception = null;
                        RTCode.Evaluate(RTFile.ReadFromFile(file), x => { hadError = true; exception = x; } );

                        if (hadError)
                            EditorManager.inst.DisplayNotification($"Couldn't evaluate {Path.GetFileName(file)}. Please verify your code and try again. Exception: {exception}", 2f, EditorManager.NotificationType.Error);
                        else
                            EditorManager.inst.DisplayNotification($"Evaluated {Path.GetFileName(file)}!", 2f, EditorManager.NotificationType.Success);
                    }));
            }

            RefreshDebugger();
        }

        void CreateAutosavePopup()
        {
            var autosavePopup = GeneratePopup("Autosave Popup", "Autosaves", new Vector2(572f, 0f), new Vector2(460f, 350f), placeholderText: "Search autosaves...");
            autosaveSearchField = autosavePopup.SearchField;
            autosaveContent = autosavePopup.Content;
        }

        void SetupMiscEditorThemes()
        {
            var checkpointEditor = EditorManager.inst.GetDialog("Checkpoint Editor").Dialog;
            if (CheckpointEditor.inst.right == null)
                CheckpointEditor.inst.right = checkpointEditor.Find("data/right");

            if (CheckpointEditor.inst.left == null)
                CheckpointEditor.inst.left = checkpointEditor.Find("data/left");

            EditorThemeManager.AddGraphic(checkpointEditor.GetComponent<Image>(), ThemeGroup.Background_1);
            EditorThemeManager.AddGraphic(CheckpointEditor.inst.right.GetComponent<Image>(), ThemeGroup.Background_3);

            EditorThemeManager.AddInputField(CheckpointEditor.inst.right.Find("search").GetComponent<InputField>(), ThemeGroup.Search_Field_2);

            var scrollbar = CheckpointEditor.inst.right.Find("checkpoints/Scrollbar Vertical").GetComponent<Scrollbar>();
            EditorThemeManager.AddScrollbar(scrollbar, scrollbarGroup: ThemeGroup.Scrollbar_2, handleGroup: ThemeGroup.Scrollbar_2_Handle);

            var edit = CheckpointEditor.inst.left.Find("edit");
            for (int i = 0; i < edit.childCount; i++)
            {
                var button = edit.GetChild(i);
                var buttonComponent = button.GetComponent<Button>();

                if (!buttonComponent)
                    continue;

                if (button.name == "del")
                {
                    var buttonBG = button.GetChild(0).GetComponent<Image>();

                    EditorThemeManager.AddGraphic(buttonBG, ThemeGroup.Delete_Keyframe_BG);

                    EditorThemeManager.AddSelectable(buttonComponent, ThemeGroup.Delete_Keyframe_Button, false);

                    continue;
                }

                Destroy(button.GetComponent<Animator>());
                buttonComponent.transition = Selectable.Transition.ColorTint;

                EditorThemeManager.AddSelectable(buttonComponent, ThemeGroup.Function_2, false);
            }

            // Labels
            for (int i = 0; i < CheckpointEditor.inst.left.childCount; i++)
            {
                var label = CheckpointEditor.inst.left.GetChild(i);

                if (!(label.name == "label" || label.name == "curves_label"))
                    continue;

                for (int j = 0; j < label.childCount; j++)
                    EditorThemeManager.AddLightText(label.GetChild(j).GetComponent<Text>());
            }

            EditorThemeManager.AddInputField(CheckpointEditor.inst.left.Find("name").GetComponent<InputField>());
            var time = CheckpointEditor.inst.left.Find("time");
            EditorThemeManager.AddInputField(time.Find("time").GetComponent<InputField>());
            for (int i = 1; i < time.childCount; i++)
            {
                var button = time.GetChild(i);
                var buttonComponent = button.GetComponent<Button>();

                Destroy(button.GetComponent<Animator>());
                buttonComponent.transition = Selectable.Transition.ColorTint;

                EditorThemeManager.AddSelectable(buttonComponent, ThemeGroup.Function_2, false);
            }

            var position = CheckpointEditor.inst.left.Find("position");
            for (int i = 0; i < position.childCount; i++)
            {
                var child = position.GetChild(i);
                EditorThemeManager.AddInputField(child.GetComponent<InputField>());

                for (int j = 1; j < child.childCount; j++)
                {
                    var button = child.GetChild(j);
                    var buttonComponent = button.GetComponent<Button>();

                    Destroy(button.GetComponent<Animator>());
                    buttonComponent.transition = Selectable.Transition.ColorTint;

                    EditorThemeManager.AddSelectable(buttonComponent, ThemeGroup.Function_2, false);
                }
            }

            CoreHelper.Log($"Setting Object Options Popup");
            // Object Options
            {
                var options = EditorManager.inst.GetDialog("Object Options Popup").Dialog;

                EditorThemeManager.AddGraphic(options.GetComponent<Image>(), ThemeGroup.Background_1, true);
                EditorThemeManager.AddGraphic(options.Find("arrow").GetComponent<Image>(), ThemeGroup.Background_1);

                for (int i = 1; i < options.childCount - 1; i++)
                {
                    var child = options.GetChild(i);

                    EditorThemeManager.AddGraphic(child.GetComponent<Image>(), ThemeGroup.Function_3, true);
                    EditorThemeManager.AddGraphic(child.GetChild(0).GetComponent<Text>(), ThemeGroup.Function_3_Text);
                }

                for (int i = 0; i < options.Find("shapes").childCount; i++)
                {
                    var child = options.Find("shapes").GetChild(i);

                    EditorThemeManager.AddGraphic(child.GetComponent<Image>(), ThemeGroup.Function_3, true);
                    EditorThemeManager.AddGraphic(child.GetChild(0).GetComponent<Image>(), ThemeGroup.Function_3_Text);
                }
            }

            // BG Options
            {
                var options = EditorManager.inst.GetDialog("BG Options Popup").Dialog;

                EditorThemeManager.AddGraphic(options.GetComponent<Image>(), ThemeGroup.Background_1, true);
                EditorThemeManager.AddGraphic(options.Find("arrow").GetComponent<Image>(), ThemeGroup.Background_1);

                for (int i = 1; i < options.childCount; i++)
                {
                    var child = options.GetChild(i);

                    EditorThemeManager.AddGraphic(child.GetComponent<Image>(), ThemeGroup.Function_3, true);
                    EditorThemeManager.AddGraphic(child.GetChild(0).GetComponent<Text>(), ThemeGroup.Function_3_Text);
                }
            }
        }
        void CreateScreenshotsView()
        {
            var editorDialogObject = EditorManager.inst.GetDialog("Multi Keyframe Editor (Object)").Dialog.gameObject.Duplicate(EditorManager.inst.dialogs);
            editorDialogObject.name = "ScreenshotDialog";
            editorDialogObject.transform.position = new Vector3(1537.5f, 714.945f, 0f) * EditorManager.inst.ScreenScale;
            editorDialogObject.transform.localScale = Vector3.one;
            editorDialogObject.transform.AsRT().sizeDelta = new Vector2(0f, 32f);

            var editorDialogTitle = editorDialogObject.transform.GetChild(0);
            editorDialogTitle.GetComponent<Image>().color = LSColors.HexToColor("00FF8C");
            var title = editorDialogTitle.GetChild(0).GetComponent<Text>();
            title.text = "- Screenshots -";

            var editorDialogSpacer = editorDialogObject.transform.GetChild(1);
            editorDialogSpacer.AsRT().sizeDelta = new Vector2(765f, 54f);

            Destroy(editorDialogObject.transform.GetChild(2).gameObject);

            EditorHelper.AddEditorDialog("Screenshot Dialog", editorDialogObject);

            var page = EditorPrefabHolder.Instance.NumberInputField.Duplicate(editorDialogObject.transform.Find("spacer"));
            var pageStorage = page.GetComponent<InputFieldStorage>();
            screenshotPageField = pageStorage.inputField;

            pageStorage.inputField.onValueChanged.ClearAll();
            pageStorage.inputField.text = screenshotPage.ToString();
            pageStorage.inputField.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int p))
                {
                    screenshotPage = Mathf.Clamp(p, 0, screenshotCount / screenshotsPerPage);
                    RefreshScreenshots();
                }
            });

            pageStorage.leftGreaterButton.onClick.ClearAll();
            pageStorage.leftGreaterButton.onClick.AddListener(() =>
            {
                if (int.TryParse(pageStorage.inputField.text, out int p))
                    pageStorage.inputField.text = "0";
            });

            pageStorage.leftButton.onClick.ClearAll();
            pageStorage.leftButton.onClick.AddListener(() =>
            {
                if (int.TryParse(pageStorage.inputField.text, out int p))
                    pageStorage.inputField.text = Mathf.Clamp(p - 1, 0, screenshotCount / screenshotsPerPage).ToString();
            });

            pageStorage.rightButton.onClick.ClearAll();
            pageStorage.rightButton.onClick.AddListener(() =>
            {
                if (int.TryParse(pageStorage.inputField.text, out int p))
                    pageStorage.inputField.text = Mathf.Clamp(p + 1, 0, screenshotCount / screenshotsPerPage).ToString();
            });

            pageStorage.rightGreaterButton.onClick.ClearAll();
            pageStorage.rightGreaterButton.onClick.AddListener(() =>
            {
                if (int.TryParse(pageStorage.inputField.text, out int p))
                    pageStorage.inputField.text = (screenshotCount / screenshotsPerPage).ToString();
            });

            Destroy(pageStorage.middleButton.gameObject);

            EditorThemeManager.AddInputField(pageStorage.inputField);
            EditorThemeManager.AddSelectable(pageStorage.leftGreaterButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(pageStorage.leftButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(pageStorage.rightButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(pageStorage.rightGreaterButton, ThemeGroup.Function_2, false);

            var scrollView = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View").Duplicate(editorDialogObject.transform, "Scroll View");
            screenshotContent = scrollView.transform.Find("Viewport/Content");
            scrollView.transform.localScale = Vector3.one;

            LSHelpers.DeleteChildren(screenshotContent);

            var scrollViewLE = scrollView.AddComponent<LayoutElement>();
            scrollViewLE.ignoreLayout = true;

            scrollView.transform.AsRT().anchoredPosition = new Vector2(392.5f, 320f);
            scrollView.transform.AsRT().sizeDelta = new Vector2(735f, 638f);

            EditorThemeManager.AddGraphic(editorDialogObject.GetComponent<Image>(), ThemeGroup.Background_1);

            EditorHelper.AddEditorDropdown("View Screenshots", "", "View", SearchSprite, () =>
            {
                EditorManager.inst.ShowDialog("Screenshot Dialog");
                RefreshScreenshots();
            });
        }

        GameObject contextMenu;
        RectTransform contextMenuLayout;

        void CreateContextMenu()
        {
            try
            {
                var parent = EditorManager.inst.dialogs.parent;

                contextMenu = Creator.NewUIObject("Context Menu", parent);
                RectValues.Default.AnchorMax(0f, 0f).AnchorMin(0f, 0f).Pivot(0f, 1f).SizeDelta(126f, 300f).AssignToRectTransform(contextMenu.transform.AsRT());
                var contextMenuImage = contextMenu.AddComponent<Image>();

                var contextMenuLayout = Creator.NewUIObject("Context Menu Layout", contextMenu.transform);
                RectValues.FullAnchored.SizeDelta(-8f, -8f).AssignToRectTransform(contextMenuLayout.transform.AsRT());
                this.contextMenuLayout = contextMenuLayout.transform.AsRT();

                var contextMenuLayoutVLG = contextMenuLayout.AddComponent<VerticalLayoutGroup>();
                contextMenuLayoutVLG.childControlHeight = false;
                contextMenuLayoutVLG.childForceExpandHeight = false;
                contextMenuLayoutVLG.spacing = 4f;

                var disable = contextMenu.AddComponent<Clickable>();
                disable.onExit = pointerEventData => { contextMenu.SetActive(false); };

                EditorThemeManager.AddGraphic(contextMenuImage, ThemeGroup.Background_2, true);
                contextMenu.SetActive(false);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        Text folderCreatorTitle;
        Text folderCreatorNameLabel;
        InputField folderCreatorName;
        Button folderCreatorSubmit;
        Text folderCreatorSubmitText;

        void CreateFolderCreator()
        {
            try
            {
                var folderCreator = EditorManager.inst.GetDialog("Save As Popup").Dialog.gameObject
                    .Duplicate(EditorManager.inst.GetDialog("Save As Popup").Dialog.GetParent(), "Folder Creator Popup");
                folderCreator.transform.localPosition = Vector3.zero;

                var folderCreatorPopup = folderCreator.transform.GetChild(0);

                var folderCreatorPopupPanel = folderCreatorPopup.Find("Panel");
                folderCreatorTitle = folderCreatorPopupPanel.Find("Text").GetComponent<Text>();
                folderCreatorTitle.text = "Folder Creator";

                var close = folderCreatorPopupPanel.Find("x").GetComponent<Button>();
                close.onClick.ClearAll();
                close.onClick.AddListener(() => EditorManager.inst.HideDialog("Folder Creator Popup"));

                folderCreatorNameLabel = folderCreatorPopup.Find("Level Name").GetComponent<Text>();
                folderCreatorNameLabel.text = "New folder name";

                folderCreatorName = folderCreatorPopup.Find("level-name").GetComponent<InputField>();
                folderCreatorName.onValueChanged.ClearAll();
                folderCreatorName.text = "New Folder";
                folderCreatorName.characterLimit = 0;

                folderCreatorSubmit = folderCreatorPopup.Find("submit").GetComponent<Button>();
                var submitImage = folderCreatorPopup.Find("submit").GetComponent<Image>();
                folderCreatorSubmitText = folderCreatorPopup.Find("submit/text").GetComponent<Text>();
                folderCreatorSubmitText.text = "Create Folder";

                EditorThemeManager.AddGraphic(folderCreatorPopup.GetComponent<Image>(), ThemeGroup.Background_1, true);
                EditorThemeManager.AddGraphic(folderCreatorPopupPanel.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);

                EditorThemeManager.AddSelectable(close, ThemeGroup.Close, true);
                EditorThemeManager.AddGraphic(close.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

                EditorThemeManager.AddLightText(folderCreatorTitle);
                EditorThemeManager.AddLightText(folderCreatorNameLabel);
                EditorThemeManager.AddInputField(folderCreatorName);

                EditorThemeManager.AddGraphic(submitImage, ThemeGroup.Function_1, true);
                EditorThemeManager.AddGraphic(folderCreatorSubmitText, ThemeGroup.Function_1_Text);

                EditorHelper.AddEditorPopup("Folder Creator Popup", folderCreator);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        #endregion

        #region Saving / Loading

        public void SetFileInfo(string text)
        {
            if (EditorConfig.Instance.Debug.Value)
                CoreHelper.Log(text);

            fileInfoText?.SetText(text);
        }

        public void PasteLevel()
        {
            if (string.IsNullOrEmpty(copiedLevelPath))
            {
                EditorManager.inst.DisplayNotification("No level has been copied yet!", 2f, EditorManager.NotificationType.Error);
                return;
            }
            
            if (!RTFile.DirectoryExists(copiedLevelPath))
            {
                EditorManager.inst.DisplayNotification("Copied level no longer exists.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var folderName = Path.GetFileName(copiedLevelPath);
            var directory = RTFile.GetDirectory(copiedLevelPath);
            var editorPath = RTFile.CombinePaths(RTFile.ApplicationDirectory, editorListPath);

            if (shouldCutLevel)
            {
                if (RTFile.DirectoryExists(copiedLevelPath.Replace(directory, editorPath)))
                {
                    EditorManager.inst.DisplayNotification($"Level with the name \"{folderName}\" already exists in this location.", 2f, EditorManager.NotificationType.Warning);
                    return;
                }

                Directory.Move(copiedLevelPath, copiedLevelPath.Replace(directory, editorPath));
                UpdateEditorPath(true);
                EditorManager.inst.DisplayNotification($"Succesfully moved {folderName}!", 2f, EditorManager.NotificationType.Success);

                return;
            }

            var result = copiedLevelPath;
            int num = 0;
            while (Directory.Exists(result.Replace(directory, editorPath)))
            {
                result = $"{copiedLevelPath} [{num}]";
                num++;
            }

            var files = Directory.GetFiles(copiedLevelPath, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i].Replace("\\", "/").Replace(copiedLevelPath, result).Replace(directory, editorPath);
                var copyToDirectory = Path.GetDirectoryName(file);
                RTFile.CreateDirectory(copyToDirectory);

                RTFile.CopyFile(files[i], file);
            }

            UpdateEditorPath(true);
            EditorManager.inst.DisplayNotification($"Succesfully pasted {folderName}!", 2f, EditorManager.NotificationType.Success);
        }

        public IEnumerator LoadLevels()
        {
            EditorManager.inst.loadedLevels.Clear();

            var config = EditorConfig.Instance;

            var olfnm = config.OpenLevelFolderNameMax;
            var olsnm = config.OpenLevelSongNameMax;
            var olanm = config.OpenLevelArtistNameMax;
            var olcnm = config.OpenLevelCreatorNameMax;
            var oldem = config.OpenLevelDescriptionMax;
            var oldam = config.OpenLevelDateMax;

            int foldClamp = olfnm.Value < 3 ? olfnm.Value : (int)olfnm.DefaultValue;
            int songClamp = olsnm.Value < 3 ? olsnm.Value : (int)olsnm.DefaultValue;
            int artiClamp = olanm.Value < 3 ? olanm.Value : (int)olanm.DefaultValue;
            int creaClamp = olcnm.Value < 3 ? olcnm.Value : (int)olcnm.DefaultValue;
            int descClamp = oldem.Value < 3 ? oldem.Value : (int)oldem.DefaultValue;
            int dateClamp = oldam.Value < 3 ? oldam.Value : (int)oldam.DefaultValue;

            var transform = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("mask/content");
            var close = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel/x");

            var horizontalOverflow = config.OpenLevelTextHorizontalWrap.Value;
            var verticalOverflow = config.OpenLevelTextVerticalWrap.Value;
            var fontSize = config.OpenLevelTextFontSize.Value;
            var format = config.OpenLevelTextFormatting.Value;
            var buttonHoverSize = config.OpenLevelButtonHoverSize.Value;

            var iconPosition = config.OpenLevelCoverPosition.Value;
            var iconScale = config.OpenLevelCoverScale.Value;

            var showDeleteButton = config.OpenLevelShowDeleteButton.Value;

            LSHelpers.DeleteChildren(transform);

            var list = new List<Coroutine>();
            var files = Directory.GetDirectories(RTFile.ApplicationDirectory + editorListPath);
            var showLevelFolders = config.ShowFoldersInLevelList.Value;
            var currentPath = editorPath;

            // Back
            if (showLevelFolders && Path.GetDirectoryName(RTFile.ApplicationDirectory + editorListPath).Replace("\\", "/") != RTFile.ApplicationDirectory + "beatmaps")
            {
                var gameObjectFolder = EditorManager.inst.folderButtonPrefab.Duplicate(transform, "back");
                var folderButtonStorageFolder = gameObjectFolder.GetComponent<FunctionButtonStorage>();
                var folderButtonFunctionFolder = gameObjectFolder.AddComponent<FolderButtonFunction>();

                var hoverUIFolder = gameObjectFolder.AddComponent<HoverUI>();
                hoverUIFolder.size = buttonHoverSize;
                hoverUIFolder.animatePos = false;
                hoverUIFolder.animateSca = true;

                folderButtonStorageFolder.text.text = "< Up a folder";

                folderButtonStorageFolder.text.horizontalOverflow = horizontalOverflow;
                folderButtonStorageFolder.text.verticalOverflow = verticalOverflow;
                folderButtonStorageFolder.text.fontSize = fontSize;

                folderButtonStorageFolder.button.onClick.ClearAll();
                folderButtonFunctionFolder.onClick = eventData =>
                {
                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        ShowContextMenu(300f,
                            new ButtonFunction("Create folder", () => ShowFolderCreator($"{RTFile.ApplicationDirectory}{editorListPath}", () => { UpdateEditorPath(true); HideNameEditor(); })),
                            new ButtonFunction("Create level", EditorManager.inst.OpenNewLevelPopup),
                            new ButtonFunction("Paste", PasteLevel),
                            new ButtonFunction("Open List in File Explorer", OpenLevelListFolder));

                        return;
                    }

                    if (editorPathField.text == currentPath)
                    {
                        editorPathField.text = Path.GetDirectoryName(RTFile.ApplicationDirectory + editorListPath).Replace("\\", "/").Replace(RTFile.ApplicationDirectory + "beatmaps/", "");
                        UpdateEditorPath(false);
                    }
                };

                EditorThemeManager.ApplySelectable(folderButtonStorageFolder.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(folderButtonStorageFolder.text);
            }

            foreach (var file in files)
            {
                var path = RTFile.ReplaceSlash(file);
                var name = Path.GetFileName(path);

                if (!RTFile.FileExists(RTFile.CombinePaths(path, Level.LEVEL_LSB)))
                {
                    if (!showLevelFolders)
                        continue;

                    var gameObjectFolder = EditorManager.inst.folderButtonPrefab.Duplicate(transform, $"Folder [{name}]");
                    var folderButtonStorageFolder = gameObjectFolder.GetComponent<FunctionButtonStorage>();
                    var folderButtonFunctionFolder = gameObjectFolder.AddComponent<FolderButtonFunction>();

                    var editorWrapperFolder = new EditorWrapper(gameObjectFolder, null, path, null);
                    editorWrapperFolder.isFolder = true;

                    var hoverUIFolder = gameObjectFolder.AddComponent<HoverUI>();
                    hoverUIFolder.size = buttonHoverSize;
                    hoverUIFolder.animatePos = false;
                    hoverUIFolder.animateSca = true;

                    folderButtonStorageFolder.text.text = name;

                    folderButtonStorageFolder.text.horizontalOverflow = horizontalOverflow;
                    folderButtonStorageFolder.text.verticalOverflow = verticalOverflow;
                    folderButtonStorageFolder.text.fontSize = fontSize;

                    folderButtonStorageFolder.button.onClick.ClearAll();
                    folderButtonFunctionFolder.onClick = eventData =>
                    {
                        if (!path.Contains(RTFile.ApplicationDirectory + "beatmaps/"))
                        {
                            EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                            return;
                        }

                        if (eventData.button == PointerEventData.InputButton.Right)
                        {
                            ShowContextMenu(300f,
                                new ButtonFunction("Open folder", () =>
                                {
                                    editorPathField.text = path.Replace(RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/", "");
                                    UpdateEditorPath(false);
                                }),
                                new ButtonFunction("Create folder", () => ShowFolderCreator($"{RTFile.ApplicationDirectory}{editorListPath}", () => { UpdateEditorPath(true); HideNameEditor(); })),
                                new ButtonFunction("Create level", EditorManager.inst.OpenNewLevelPopup),
                                new ButtonFunction(true),
                                new ButtonFunction("Rename", () => ShowNameEditor("Folder Renamer", "Folder name", "Rename", () =>
                                    {
                                        RTFile.MoveDirectory(path, path.Replace(name, RTFile.ValidateDirectory(folderCreatorName.text)).Replace("\\", "/"));

                                        UpdateEditorPath(true);
                                        HideNameEditor();
                                    })),
                                new ButtonFunction("Paste", PasteLevel),
                                new ButtonFunction("Delete", () =>
                                {
                                    ShowWarningPopup("Are you <b>100%</b> sure you want to delete this folder? This <b>CANNOT</b> be undone! Always make sure you have backups.", () =>
                                    {
                                        RTFile.DeleteDirectory(path);
                                        UpdateEditorPath(true);
                                        EditorManager.inst.DisplayNotification("Deleted folder!", 2f, EditorManager.NotificationType.Success);
                                        HideWarningPopup();
                                    }, HideWarningPopup);
                                }),
                                new ButtonFunction(true),
                                new ButtonFunction("ZIP Folder", () => ZIPLevel(path, name)),
                                new ButtonFunction("Copy Path", () => LSText.CopyToClipboard(path)),
                                new ButtonFunction("Open in File Explorer", () => RTFile.OpenInFileBrowser.Open(path)),
                                new ButtonFunction("Open List in File Explorer", OpenLevelListFolder));

                            return;
                        }

                        editorPathField.text = path.Replace(RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/", "");
                        UpdateEditorPath(false);
                    };

                    EditorThemeManager.ApplySelectable(folderButtonStorageFolder.button, ThemeGroup.List_Button_1);
                    EditorThemeManager.ApplyLightText(folderButtonStorageFolder.text);

                    EditorManager.inst.loadedLevels.Add(editorWrapperFolder);

                    continue;
                }

                var metadataJSON = RTFile.ReadFromFile(RTFile.CombinePaths(path, Level.METADATA_LSB));

                if (string.IsNullOrEmpty(metadataJSON))
                {
                    Debug.LogError($"{EditorManager.inst.className}Could not load metadata for [{name}]!");
                    continue;
                }

                var metadata = MetaData.Parse(JSON.Parse(metadataJSON));

                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(transform, $"Folder [{name}]");
                var folderButtonStorage = gameObject.GetComponent<FunctionButtonStorage>();
                var folderButtonFunction = gameObject.AddComponent<FolderButtonFunction>();

                var editorWrapper = new EditorWrapper(gameObject, metadata, path, SteamWorkshop.inst.defaultSteamImageSprite);

                var hoverUI = gameObject.AddComponent<HoverUI>();
                hoverUI.size = buttonHoverSize;
                hoverUI.animatePos = false;
                hoverUI.animateSca = true;

                folderButtonStorage.text.text = string.Format(format,
                    LSText.ClampString(name, foldClamp),
                    LSText.ClampString(metadata.song.title, songClamp),
                    LSText.ClampString(metadata.artist.Name, artiClamp),
                    LSText.ClampString(metadata.creator.steam_name, creaClamp),
                    metadata.song.difficulty,
                    LSText.ClampString(metadata.song.description, descClamp),
                    LSText.ClampString(metadata.beatmap.date_edited, dateClamp),
                    LSText.ClampString(metadata.beatmap.date_created, dateClamp),
                    LSText.ClampString(metadata.beatmap.date_published, dateClamp));

                folderButtonStorage.text.horizontalOverflow = horizontalOverflow;
                folderButtonStorage.text.verticalOverflow = verticalOverflow;
                folderButtonStorage.text.fontSize = fontSize;

                var difficultyColor = metadata.song.difficulty >= 0 && metadata.song.difficulty < DataManager.inst.difficulties.Count ?
                    DataManager.inst.difficulties[metadata.song.difficulty].color : LSColors.themeColors["none"].color;

                TooltipHelper.AssignTooltip(gameObject, "Level List Button", 3f);
                gameObject.AddComponent<HoverTooltip>().tooltipLangauges.Add(new HoverTooltip.Tooltip
                {
                    desc = "<#" + LSColors.ColorToHex(difficultyColor) + ">" + metadata.artist.Name + " - " + metadata.song.title,
                    hint = $"</color><br>Folder: {name}<br>Date Edited: {metadata.beatmap.date_edited}<br>Date Created: {metadata.beatmap.date_created}<br>Description: {metadata.song.description}",
                });

                folderButtonStorage.button.onClick.ClearAll();
                folderButtonFunction.onClick = eventData =>
                {
                    if (choosingLevelTemplate)
                    {
                        CreateTemplate(path);

                        return;
                    }

                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        ShowContextMenu(300f,
                            new ButtonFunction("Open", () =>
                            {
                                StartCoroutine(LoadLevel(path));
                                EditorManager.inst.HideDialog("Open File Popup");
                            }),
                            new ButtonFunction("Show Autosaves", () =>
                            {
                                EditorManager.inst.ShowDialog("Autosave Popup");
                                RefreshAutosaveList(editorWrapper);
                            }),
                            new ButtonFunction("Convert to VG", () => ConvertLevel(path, name)),
                            new ButtonFunction(true),
                            new ButtonFunction("Create folder", () => ShowFolderCreator($"{RTFile.ApplicationDirectory}{editorListPath}", () => { UpdateEditorPath(true); HideNameEditor(); })),
                            new ButtonFunction("Create template", () => CreateTemplate(path)),
                            new ButtonFunction("Create level", EditorManager.inst.OpenNewLevelPopup),
                            new ButtonFunction(true),
                            new ButtonFunction("Rename", () => ShowNameEditor("Folder Renamer", "Folder name", "Rename", () =>
                                {
                                    var destination = RTFile.ReplaceSlash(path.Replace(name, RTFile.ValidateDirectory(folderCreatorName.text)));
                                    RTFile.MoveDirectory(path, destination);
                                    metadata.beatmap.name = folderCreatorName.text;
                                    RTFile.WriteToFile(RTFile.CombinePaths(destination, Level.METADATA_LSB), metadata.ToJSON().ToString());

                                    UpdateEditorPath(true);
                                    HideNameEditor();
                                })),
                            new ButtonFunction("Cut", () =>
                            {
                                shouldCutLevel = true;
                                copiedLevelPath = editorWrapper.folder;
                                EditorManager.inst.DisplayNotification($"Cut {name}!", 1.5f, EditorManager.NotificationType.Success);
                                CoreHelper.Log($"Cut level: {copiedLevelPath}");
                            }),
                            new ButtonFunction("Copy", () =>
                            {
                                shouldCutLevel = false;
                                copiedLevelPath = editorWrapper.folder;
                                EditorManager.inst.DisplayNotification($"Copied {name}!", 1.5f, EditorManager.NotificationType.Success);
                                CoreHelper.Log($"Copied level: {copiedLevelPath}");
                            }),
                            new ButtonFunction("Paste", PasteLevel),
                            new ButtonFunction("Delete", () =>
                            {
                                ShowWarningPopup("Are you sure you want to delete this level? This CANNOT be undone!", () =>
                                {
                                    RTFile.DeleteDirectory(editorWrapper.folder);
                                    UpdateEditorPath(true);
                                    EditorManager.inst.DisplayNotification("Deleted level!", 2f, EditorManager.NotificationType.Success);
                                    HideWarningPopup();
                                }, HideWarningPopup);
                            }),
                            new ButtonFunction(true),
                            new ButtonFunction("Copy Arcade ID", () =>
                            {
                                if (editorWrapper.metadata is MetaData metadata)
                                {
                                    if (string.IsNullOrEmpty(metadata.arcadeID) || metadata.arcadeID == "0")
                                    {
                                        EditorManager.inst.DisplayNotification($"Level does not have an ID assigned to it yet. Open the level, save it and try again.", 3.3f, EditorManager.NotificationType.Warning);
                                        return;
                                    }

                                    LSText.CopyToClipboard(metadata.arcadeID);
                                    EditorManager.inst.DisplayNotification($"Copied Arcade ID ({metadata.arcadeID}) to your clipboard.", 2f, EditorManager.NotificationType.Success);
                                }
                            }),
                            new ButtonFunction("Copy Server ID", () =>
                            {
                                if (editorWrapper.metadata is MetaData metadata)
                                {
                                    if (string.IsNullOrEmpty(metadata.serverID) || metadata.serverID == "0")
                                    {
                                        EditorManager.inst.DisplayNotification($"Your level needs to be uploaded to the arcade server before you can copy the server ID.", 3.5f, EditorManager.NotificationType.Warning);
                                        return;
                                    }

                                    LSText.CopyToClipboard(metadata.serverID);
                                    EditorManager.inst.DisplayNotification($"Copied Server ID ({metadata.serverID}) to your clipboard.", 2f, EditorManager.NotificationType.Success);
                                }
                            }),
                            new ButtonFunction(true),
                            new ButtonFunction("ZIP Level", () => ZIPLevel(path, name)),
                            new ButtonFunction("Copy Path", () => LSText.CopyToClipboard(path)),
                            new ButtonFunction("Open in File Explorer", () => RTFile.OpenInFileBrowser.Open(path)),
                            new ButtonFunction("Open List in File Explorer", OpenLevelListFolder)
                        );

                        return;
                    }

                    StartCoroutine(LoadLevel(path));
                    EditorManager.inst.HideDialog("Open File Popup");
                };

                EditorThemeManager.ApplySelectable(folderButtonStorage.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(folderButtonStorage.text);

                var iconBase = new GameObject("icon base");
                iconBase.transform.SetParent(gameObject.transform);
                iconBase.transform.localScale = Vector3.one;
                var iconBaseRT = iconBase.AddComponent<RectTransform>();
                var iconBaseImage = iconBase.AddComponent<Image>();
                iconBase.AddComponent<Mask>().showMaskGraphic = false;
                iconBaseRT.anchoredPosition = iconPosition;
                iconBaseRT.sizeDelta = iconScale;
                EditorThemeManager.ApplyGraphic(iconBaseImage, ThemeGroup.Null, true);

                var icon = new GameObject("icon");
                icon.transform.SetParent(iconBaseRT);
                icon.transform.localScale = Vector3.one;
                var iconRT = icon.AddComponent<RectTransform>();
                var iconImage = icon.AddComponent<Image>();

                iconRT.anchoredPosition = Vector3.zero;
                iconRT.sizeDelta = iconScale;

                // Delete
                if (showDeleteButton)
                {
                    var delete = close.gameObject.Duplicate(gameObject.transform, "delete");

                    delete.transform.AsRT().anchoredPosition = new Vector2(-5f, 0f);

                    string levelName = path;

                    var deleteButton = delete.GetComponent<Button>();
                    deleteButton.onClick.ClearAll();
                    deleteButton.onClick.AddListener(() =>
                    {
                        ShowWarningPopup("Are you sure you want to delete this level? (It will be moved to a recycling folder)", () =>
                        {
                            DeleteLevelFunction(levelName);
                            EditorManager.inst.DisplayNotification("Deleted level!", 2f, EditorManager.NotificationType.Success);
                            EditorManager.inst.GetLevelList();
                            HideWarningPopup();
                        }, HideWarningPopup);
                    });
                }
                
                list.Add(StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{RTFile.CombinePaths(path, Level.LEVEL_JPG)}", cover =>
                {
                    if (!cover)
                    {
                        iconImage.sprite = SteamWorkshop.inst.defaultSteamImageSprite;
                        editorWrapper.albumArt = SteamWorkshop.inst.defaultSteamImageSprite;
                        EditorManager.inst.loadedLevels.Add(editorWrapper);
                        return;
                    }

                    var sprite = SpriteHelper.CreateSprite(cover);
                    iconImage.sprite = sprite;
                    editorWrapper.albumArt = sprite;

                    EditorManager.inst.loadedLevels.Add(editorWrapper);
                }, (errorMsg, handlerText) =>
                {
                    iconImage.sprite = SteamWorkshop.inst.defaultSteamImageSprite;
                    editorWrapper.albumArt = SteamWorkshop.inst.defaultSteamImageSprite;
                    EditorManager.inst.loadedLevels.Add(editorWrapper);
                })));
            }

            if (list.Count >= 1)
                yield return StartCoroutine(LSHelpers.WaitForMultipleCoroutines(list, OpenLevelPopup));
            else
                OpenLevelPopup();

            yield break;
        }

        void CreateTemplate(string file)
        {
            if (string.IsNullOrEmpty(nameInput.text))
            {
                EditorManager.inst.DisplayNotification($"Level template name is empty. Name it something unique via the input field in the Level Template editor.", 3f, EditorManager.NotificationType.Error);
                return;
            }

            EditorManager.inst.HideDialog("Open File Popup");

            ShowWarningPopup("Are you sure you want to make a new level template?", () =>
            {
                choosingLevelTemplate = false;

                var copyTo = RTFile.CombinePaths(RTFile.ApplicationDirectory, "beatmaps/templates", RTFile.ValidateDirectory(nameInput.text));

                RTFile.CreateDirectory(copyTo);
                RTFile.CopyFile(RTFile.CombinePaths(file, Level.LEVEL_LSB), RTFile.CombinePaths(copyTo, Level.LEVEL_LSB));

                if (currentTemplateSprite)
                    currentTemplateSprite.Save(RTFile.CombinePaths(copyTo, "preview.png"));

                RefreshNewLevelTemplates();
                HideWarningPopup();
            }, () =>
            {
                EditorManager.inst.ShowDialog("Open File Popup");
                HideWarningPopup();
            });

        }

        void OpenLevelPopup()
        {
            if (!EditorConfig.Instance.OpenNewLevelCreatorIfNoLevels.Value || EditorManager.inst.loadedLevels.Count > 0)
            {
                EditorManager.inst.ShowDialog("Open File Popup");
                EditorManager.inst.RenderOpenBeatmapPopup();
            }
            else
                EditorManager.inst.OpenNewLevelPopup();
        }

        public IEnumerator LoadLevel(Level level)
        {
            yield return LoadLevel(RTFile.RemoveEndSlash(level.path));
        }

        /// <summary>
        /// Loads a level in the editor from a full path. For example: E:/4.1.16/beatmaps/editor/New Awesome Beatmap.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="autosave"></param>
        /// <returns></returns>
        public IEnumerator LoadLevel(string fullPath, string autosave = "")
        {
            // i have no idea what is causing the memory issue here

            EditorManager.inst.loading = true;
            var sw = CoreHelper.StartNewStopwatch();

            RTPlayer.GameMode = GameMode.Regular;

            string code = RTFile.CombinePaths(fullPath, "EditorLoad.cs");
            if (RTFile.FileExists(code))
            {
                var str = RTFile.ReadFromFile(code);
                if (RTCode.Validate(str))
                    yield return StartCoroutine(RTCode.IEvaluate(str));
            }

            SetLayer(0, LayerType.Objects);

            WindowController.ResetTitle();

            RandomHelper.UpdateSeed();

            CoreHelper.Log("Clearing data...");

            for (int i = 0; i < timelineObjects.Count; i++)
            {
                var timelineObject = timelineObjects[i];
                Destroy(timelineObject.GameObject);

                for (int j = 0; j < timelineObject.InternalTimelineObjects.Count; j++)
                {
                    var kf = timelineObject.InternalTimelineObjects[j];
                    Destroy(kf.GameObject);
                }
                timelineObjects[i] = null;
            }
            timelineObjects.Clear();

            for (int i = 0; i < timelineKeyframes.Count; i++)
            {
                var timelineObject = timelineKeyframes[i];
                Destroy(timelineObject.GameObject);
                timelineKeyframes[i] = null;
            }
            timelineKeyframes.Clear();

            for (int i = 0; i < ObjEditor.inst.TimelineParents.Count; i++)
                LSHelpers.DeleteChildren(ObjEditor.inst.TimelineParents[i]);

            if (GameData.IsValid)
            {
                var bgs = GameData.Current.backgroundObjects;
                for (int i = 0; i < bgs.Count; i++)
                {
                    var bg = bgs[i];
                    for (int j = 0; j < bg.gameObjects.Count; j++)
                    {
                        Destroy(bg.gameObjects[j]);
                        bg.gameObjects[j] = null;
                    }
                    for (int j = 0; j < bg.renderers.Count; j++)
                        bg.renderers[j] = null;
                    for (int j = 0; j < bg.transforms.Count; j++)
                        bg.transforms[j] = null;
                    bg.gameObjects.Clear();
                }
            }

            Updater.UpdateObjects(false);

            // We stop and play the doggo bop animation in case the user has looked at the settings dialog.
            EditorManager.inst.CancelInvoke("LoadingIconUpdate");
            EditorManager.inst.InvokeRepeating("LoadingIconUpdate", 0f, 0.05f);

            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            var name = Path.GetFileName(fullPath);

            EditorManager.inst.currentLoadedLevel = name;
            EditorManager.inst.SetPitch(1f);

            EditorManager.inst.timelineScrollRectBar.value = 0f;
            GameManager.inst.gameState = GameManager.State.Loading;

            EditorManager.inst.ClearDialogs();
            EditorManager.inst.ShowDialog("File Info Popup");

            var previousLevel = GameManager.inst.path.Replace("/" + Level.LEVEL_LSB, "");
            if (EditorManager.inst.hasLoadedLevel && EditorConfig.Instance.BackupPreviousLoadedLevel.Value && RTFile.DirectoryExists(previousLevel))
            {
                CoreHelper.Log("Backing up previous level...");

                SetFileInfo($"Backing up previous level [ {Path.GetFileName(previousLevel)} ]");

                GameData.Current.SaveData(GameManager.inst.path.Replace(Level.LEVEL_LSB, "level-open-backup.lsb"));

                CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");
            }

            CoreHelper.Log("Loading data...");

            SetFileInfo($"Loading Level Data for [ {name} ]");

            string rawJSON = null;
            string rawMetadataJSON = null;
            AudioClip song = null;

            var fromFile = (string.IsNullOrEmpty(autosave) ? Level.LEVEL_LSB : autosave);
            CoreHelper.Log($"Loading {fromFile}...");
            rawJSON = RTFile.ReadFromFile(RTFile.CombinePaths(fullPath, fromFile));
            rawMetadataJSON = RTFile.ReadFromFile(RTFile.CombinePaths(fullPath, Level.METADATA_LSB));

            if (string.IsNullOrEmpty(rawMetadataJSON))
            {
                DataManager.inst.SaveMetadata(RTFile.CombinePaths(fullPath, Level.METADATA_LSB));
                rawMetadataJSON = RTFile.ReadFromFile(RTFile.CombinePaths(fullPath, Level.METADATA_LSB));
            }

            GameManager.inst.path = RTFile.CombinePaths(fullPath, Level.LEVEL_LSB);
            RTFile.BasePath = RTFile.AppendEndSlash(fullPath);
            GameManager.inst.levelName = name;
            SetFileInfo($"Loading Level Music for [ {name} ]\n\nIf this is taking more than a minute or two check if the song file (.ogg / .wav / .mp3) is corrupt. If not, then something went really wrong.");

            string errorMessage = "";
            bool hadError = false;
            if (RTFile.FileExists(RTFile.CombinePaths(fullPath, Level.LEVEL_OGG)))
                yield return this.StartCoroutineAsync(AlephNetworkManager.DownloadAudioClip($"file://{RTFile.CombinePaths(fullPath, Level.LEVEL_OGG)}", AudioType.OGGVORBIS, x => { song = x; x = null; }, onError => { hadError = true; errorMessage = onError; }));
            else if (RTFile.FileExists(RTFile.CombinePaths(fullPath, Level.LEVEL_WAV)))
                yield return this.StartCoroutineAsync(AlephNetworkManager.DownloadAudioClip($"file://{RTFile.CombinePaths(fullPath, Level.LEVEL_WAV)}", AudioType.WAV, x => { song = x; x = null; }, onError => { hadError = true; errorMessage = onError; }));
            else if (RTFile.FileExists(RTFile.CombinePaths(fullPath, Level.LEVEL_MP3)))
            {
                Exception e = null;
                yield return this.StartCoroutineAsync(CoreHelper.DoAction(() =>
                {
                    try
                    {
                        song = LSAudio.CreateAudioClipUsingMP3File(RTFile.CombinePaths(fullPath, Level.LEVEL_MP3));
                    }
                    catch (Exception ex)
                    {
                        e = ex;
                        hadError = true;
                    }
                }));

                if (hadError)
                {
                    if (e != null)
                        CoreHelper.LogException(e);

                    if (!RTFile.FileExists(RTFile.CombinePaths(fullPath, Level.LEVEL_MP3)))
                        SetFileInfo("Song does not exist.");
                    else
                    {
                        try
                        {
                            var file = new FileInfo(RTFile.CombinePaths(fullPath, Level.LEVEL_MP3));
                            SetFileInfo($"There was a problem with loading the MP3 file. Could it be due to the filesize of [{file.Length}]?");
                        }
                        catch (Exception ex)
                        {
                            CoreHelper.LogException(ex);
                        }
                    }

                    yield break;
                }
            }

            if (hadError)
            {
                bool audioExists = RTFile.FileExists(RTFile.CombinePaths(fullPath, Level.LEVEL_OGG)) || RTFile.FileExists(RTFile.CombinePaths(fullPath, Level.LEVEL_WAV)) || RTFile.FileExists(RTFile.CombinePaths(fullPath, Level.LEVEL_MP3));

                if (audioExists)
                    SetFileInfo($"Something went wrong when loading the song file. Either the file is corrupt or something went wrong internally.");
                else
                    SetFileInfo($"Song file does not exist.");

                EditorManager.inst.DisplayNotification($"Song file could not load due to {errorMessage}", 3f, EditorManager.NotificationType.Error);

                CoreHelper.LogError($"Level loading caught an error: {errorMessage}\n" +
                    $"level.ogg exists: {RTFile.FileExists(RTFile.CombinePaths(fullPath, Level.LEVEL_OGG))}\n" +
                    $"level.wav exists: {RTFile.FileExists(RTFile.CombinePaths(fullPath, Level.LEVEL_WAV))}\n" +
                    $"level.mp3 exists: {RTFile.FileExists(RTFile.CombinePaths(fullPath, Level.LEVEL_MP3))}\n");

                yield break;
            }

            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            CoreHelper.Log("Setting up Video...");
            yield return StartCoroutine(RTVideoManager.inst.Setup(fullPath));
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            GameManager.inst.gameState = GameManager.State.Parsing;
            CoreHelper.Log("Parsing data...");
            SetFileInfo($"Parsing Level Data for [ {name} ]");
            if (!string.IsNullOrEmpty(rawJSON) && !string.IsNullOrEmpty(rawMetadataJSON))
            {
                try
                {
                    MetaData.Current = null;
                    MetaData.Current = MetaData.Parse(JSON.Parse(rawMetadataJSON));

                    if (MetaData.Current.arcadeID == null || MetaData.Current.arcadeID == "0" || MetaData.Current.arcadeID == "-1")
                        MetaData.Current.arcadeID = LSText.randomNumString(16);

                    if (MetaData.Current.beatmap.game_version != "4.1.16" && MetaData.Current.beatmap.game_version != "20.4.4")
                        rawJSON = LevelManager.UpdateBeatmap(rawJSON, MetaData.Current.beatmap.game_version);

                    GameData.Current = null;
                    GameData.Current = GameData.Parse(JSON.Parse(rawJSON), false);
                }
                catch (Exception ex)
                {
                    SetFileInfo($"Something went wrong when parsing the level data. Press the open log folder key ({CoreConfig.Instance.OpenPAPersistentFolder.Value}) and send the Player.log file to Mecha.");

                    EditorManager.inst.DisplayNotification("Level could not load.", 3f, EditorManager.NotificationType.Error);

                    CoreHelper.LogError($"Level loading caught an error: {ex}");

                    yield break;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(rawJSON) && !string.IsNullOrEmpty(rawMetadataJSON))
                    SetFileInfo($"level.lsb is empty or corrupt.");

                if (!string.IsNullOrEmpty(rawJSON) && string.IsNullOrEmpty(rawMetadataJSON))
                    SetFileInfo($"metadata.lsb is empty or corrupt.");

                if (string.IsNullOrEmpty(rawJSON) && string.IsNullOrEmpty(rawMetadataJSON))
                    SetFileInfo($"Both level.lsb and metadata.lsb are empty or corrupt.");

                EditorManager.inst.DisplayNotification("Level could not load.", 3f, EditorManager.NotificationType.Error);

                yield break;
            }
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            PreviewCover?.gameObject?.SetActive(false);

            CoreHelper.Log("Playing level music...");
            SetFileInfo($"Playing Music for [ {name} ]\n\nIf it doesn't, then something went wrong!");
            AudioManager.inst.PlayMusic(null, song, true, 0f, true);
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");
            if (EditorConfig.Instance.WaveformGenerate.Value)
            {
                CoreHelper.Log("Assigning waveform textures...");
                SetFileInfo($"Assigning Waveform Textures for [ {name} ]");
                SetTimelineSprite(null);
                StartCoroutine(AssignTimelineTexture());
                CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");
            }
            else
            {
                CoreHelper.Log("Skipping waveform textures...");
                SetFileInfo($"Skipping Waveform Textures for [ {name} ]");
                SetTimelineSprite(null);
                CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");
            }

            CoreHelper.Log("Updating timeline...");
            SetFileInfo($"Updating Timeline for [ {name} ]");
            EditorManager.inst.UpdateTimelineSizes();
            GameManager.inst.UpdateTimeline();
            MetadataEditor.inst.Render();
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            CheckpointEditor.inst.CreateGhostCheckpoints();

            SetFileInfo($"Updating states for [ {name} ]");
            CoreHelper.UpdateDiscordStatus($"Editing: {MetaData.Current.song.title}", "In Editor", "editor");

            CoreHelper.Log("Spawning players...");
            PlayerManager.LoadGlobalModels();
            PlayerManager.RespawnPlayers();

            RTPlayer.SetGameDataProperties();
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            CoreHelper.Log("Updating objects...");
            StartCoroutine(Updater.IUpdateObjects(true));
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            CoreHelper.Log("Updating timeline objects...");
            EventEditor.inst.CreateEventObjects();
            BackgroundManager.inst.UpdateBackgrounds();
            GameManager.inst.UpdateTheme();

            RTMarkerEditor.inst.CreateMarkers();
            RTMarkerEditor.inst.markerLooping = false;
            RTMarkerEditor.inst.markerLoopBegin = null;
            RTMarkerEditor.inst.markerLoopEnd = null;

            EventManager.inst.updateEvents();
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            RTEventManager.inst.SetResetOffsets();

            CoreHelper.Log("Creating timeline objects...");
            SetFileInfo($"Setting first object of [ {name} ]");
            StartCoroutine(ObjectEditor.inst.ICreateTimelineObjects(true));
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            CheckpointEditor.inst.SetCurrentCheckpoint(0);

            SetFileInfo("Done!");
            EditorManager.inst.HideDialog("File Info Popup");
            EditorManager.inst.CancelInvoke("LoadingIconUpdate");

            GameManager.inst.ResetCheckpoints(true);
            GameManager.inst.gameState = GameManager.State.Playing;

            EditorManager.inst.DisplayNotification($"{name} Level loaded", 2f, EditorManager.NotificationType.Success);
            EditorManager.inst.UpdatePlayButton();
            EditorManager.inst.hasLoadedLevel = true;

            SetAutoSave();

            TriggerHelper.AddEventTriggers(timeField.gameObject, TriggerHelper.ScrollDelta(timeField, max: AudioManager.inst.CurrentAudioSource.clip.length));

            // Load Settings like timeline position, editor layer, bpm active, etc
            LoadSettings();
            EditorManager.inst.RenderTimeline();

            if (EditorConfig.Instance.LevelPausesOnStart.Value)
            {
                AudioManager.inst.CurrentAudioSource.Pause();
                EditorManager.inst.UpdatePlayButton();
            }

            if (ExampleManager.inst && ExampleManager.inst.Visible)
                ExampleManager.inst.SayDialogue(fromNewLevel ? "LoadedNewLevel" : "LoadedLevel");

            EditorManager.inst.loading = false;
            fromNewLevel = false;

            rawJSON = null;
            rawMetadataJSON = null;
            song = null;

            CoreHelper.StopAndLogStopwatch(sw, $"Finished loading {name}");
            sw = null;

            yield break;
        }

        GameObject themeUpFolderButton;
        public IEnumerator LoadThemes(bool refreshGUI = false)
        {
            if (themesLoading)
                yield break;

            themesLoading = true;

            while (!EventEditor.inst || !EventEditor.inst.dialogRight || !GameData.IsValid)
                yield return null;

            DataManager.inst.CustomBeatmapThemes.Clear();
            DataManager.inst.BeatmapThemeIDToIndex.Clear();
            DataManager.inst.BeatmapThemeIndexToID.Clear();

            if (GameData.IsValid)
                GameData.Current.beatmapThemes.Clear();

            var dialogTmp = EventEditor.inst.dialogRight.GetChild(4);
            var parent = dialogTmp.Find("themes/viewport/content");

            if (RTThemeEditor.inst.ThemePanels.Count > 0)
                RTThemeEditor.inst.ThemePanels.ForEach(x => Destroy(x.GameObject));
            RTThemeEditor.inst.ThemePanels.Clear();

            if (!themeAddButton)
            {
                themeAddButton = EventEditor.inst.ThemeAdd.Duplicate(parent, "Create New");
                var tf = themeAddButton.transform;
                themeAddButton.SetActive(true);
                tf.localScale = Vector2.one;
                var button = themeAddButton.GetComponent<Button>();
                button.onClick.AddListener(() => RTThemeEditor.inst.RenderThemeEditor());

                var contextClickable = themeAddButton.AddComponent<ContextClickable>();
                contextClickable.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    ShowContextMenu(300f,
                        new ButtonFunction("Create folder", () => ShowFolderCreator($"{RTFile.ApplicationDirectory}{themeListPath}", () => { UpdateThemePath(true); HideNameEditor(); })),
                        new ButtonFunction("Create theme", () => RTThemeEditor.inst.RenderThemeEditor()),
                        new ButtonFunction(true),
                        new ButtonFunction("Paste", RTThemeEditor.inst.PasteTheme));
                };

                EditorThemeManager.AddGraphic(button.image, ThemeGroup.List_Button_2_Normal, true);
                EditorThemeManager.AddGraphic(themeAddButton.transform.Find("edit").GetComponent<Image>(), ThemeGroup.List_Button_2_Text);
                EditorThemeManager.AddGraphic(themeAddButton.transform.Find("text").GetComponent<Text>(), ThemeGroup.List_Button_2_Text);
            }

            try
            {
                var hoverSize = EditorConfig.Instance.OpenLevelButtonHoverSize.Value;

                // Back
                if (!themeUpFolderButton)
                {
                    themeUpFolderButton = EditorManager.inst.folderButtonPrefab.Duplicate(RTThemeEditor.inst.themeKeyframeContent, "back");
                    var folderButtonStorageFolder = themeUpFolderButton.GetComponent<FunctionButtonStorage>();
                    var folderButtonFunctionFolder = themeUpFolderButton.AddComponent<FolderButtonFunction>();

                    var hoverUIFolder = themeUpFolderButton.AddComponent<HoverUI>();
                    hoverUIFolder.size = hoverSize;
                    hoverUIFolder.animatePos = false;
                    hoverUIFolder.animateSca = true;

                    folderButtonStorageFolder.text.text = "< Up a folder";

                    folderButtonStorageFolder.button.onClick.ClearAll();
                    folderButtonFunctionFolder.onClick = eventData =>
                    {
                        if (eventData.button == PointerEventData.InputButton.Right)
                        {
                            ShowContextMenu(300f,
                                new ButtonFunction("Create folder", () => ShowFolderCreator($"{RTFile.ApplicationDirectory}{themeListPath}", () => { UpdateThemePath(true); HideNameEditor(); })),
                                new ButtonFunction("Create theme", () => RTThemeEditor.inst.RenderThemeEditor()),
                                new ButtonFunction("Paste", RTThemeEditor.inst.PasteTheme));

                            return;
                        }

                        if (themePathField.text == themePath)
                        {
                            themePathField.text = RTFile.GetDirectory(RTFile.ApplicationDirectory + themeListPath).Replace(RTFile.ApplicationDirectory + "beatmaps/", "");
                            UpdateThemePath(false);
                        }
                    };

                    EditorThemeManager.ApplySelectable(folderButtonStorageFolder.button, ThemeGroup.List_Button_2);
                    EditorThemeManager.ApplyGraphic(folderButtonStorageFolder.text, ThemeGroup.List_Button_2_Text);
                }

                themeUpFolderButton.SetActive(RTFile.GetDirectory(RTFile.ApplicationDirectory + themeListPath) != RTFile.ApplicationDirectory + "beatmaps");

                foreach (var directory in Directory.GetDirectories(RTFile.ApplicationDirectory + themeListPath, "*", SearchOption.TopDirectoryOnly))
                {
                    var path = RTFile.ReplaceSlash(directory);
                    var fileName = Path.GetFileName(directory);

                    var gameObjectFolder = EditorManager.inst.folderButtonPrefab.Duplicate(RTThemeEditor.inst.themeKeyframeContent, $"Folder [{fileName}]");
                    var folderButtonStorageFolder = gameObjectFolder.GetComponent<FunctionButtonStorage>();
                    var folderButtonFunctionFolder = gameObjectFolder.AddComponent<FolderButtonFunction>();

                    var hoverUIFolder = gameObjectFolder.AddComponent<HoverUI>();
                    hoverUIFolder.size = hoverSize;
                    hoverUIFolder.animatePos = false;
                    hoverUIFolder.animateSca = true;

                    folderButtonStorageFolder.text.text = fileName;

                    folderButtonStorageFolder.button.onClick.ClearAll();
                    folderButtonFunctionFolder.onClick = eventData =>
                    {
                        if (!path.Contains(RTFile.ApplicationDirectory + "beatmaps/"))
                        {
                            EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                            return;
                        }

                        if (eventData.button == PointerEventData.InputButton.Right)
                        {
                            ShowContextMenu(300f,
                                new ButtonFunction("Open folder", () =>
                                {
                                    themePathField.text = path.Replace(RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/", "");
                                    UpdateThemePath(false);
                                }),
                                new ButtonFunction("Create folder", () => ShowFolderCreator($"{RTFile.ApplicationDirectory}{themeListPath}", () => { UpdateThemePath(true); HideNameEditor(); })),
                                new ButtonFunction("Create theme", () => RTThemeEditor.inst.RenderThemeEditor()),
                                new ButtonFunction(true),
                                new ButtonFunction("Paste", RTThemeEditor.inst.PasteTheme),
                                new ButtonFunction("Delete", () =>
                                {
                                    ShowWarningPopup("Are you <b>100%</b> sure you want to delete this folder? This <b>CANNOT</b> be undone! Always make sure you have backups.", () =>
                                    {
                                        RTFile.DeleteDirectory(path);
                                        UpdateThemePath(true);
                                        EditorManager.inst.DisplayNotification("Deleted folder!", 2f, EditorManager.NotificationType.Success);
                                        HideWarningPopup();
                                    }, HideWarningPopup);
                                }));

                            return;
                        }

                        themePathField.text = path.Replace(RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/", "");
                        UpdateThemePath(false);
                    };

                    EditorThemeManager.ApplySelectable(folderButtonStorageFolder.button, ThemeGroup.List_Button_2);
                    EditorThemeManager.ApplyGraphic(folderButtonStorageFolder.text, ThemeGroup.List_Button_2_Text);

                    RTThemeEditor.inst.ThemePanels.Add(new ThemePanel
                    {
                        GameObject = gameObjectFolder,
                        FilePath = directory,
                        isFolder = true,
                    });
                }

            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            var layer = RTThemeEditor.inst.eventThemePage + 1;
            int max = layer * RTThemeEditor.eventThemesPerPage;

            int num = 0;
            foreach (var beatmapTheme in DataManager.inst.BeatmapThemes.Select(x => x as BeatmapTheme))
            {
                DataManager.inst.BeatmapThemeIDToIndex.Add(num, num);
                DataManager.inst.BeatmapThemeIndexToID.Add(num, num);

                RTThemeEditor.inst.SetupThemePanel(beatmapTheme, true);

                num++;
            }

            var search = EventEditor.inst.dialogRight.GetChild(4).Find("theme-search").GetComponent<InputField>().text;
            var files = Directory.GetFiles(RTFile.ApplicationDirectory + themeListPath, FileFormat.LST.ToPattern());
            foreach (var file in files)
            {
                var jn = JSON.Parse(RTFile.ReadFromFile(file));
                var orig = BeatmapTheme.Parse(jn);
                orig.filePath = file.Replace("\\", "/");
                DataManager.inst.CustomBeatmapThemes.Add(orig);

                if (jn["id"] != null && GameData.IsValid && GameData.Current.beatmapThemes != null && !GameData.Current.beatmapThemes.ContainsKey(jn["id"]))
                    GameData.Current.beatmapThemes.Add(jn["id"], orig);

                if (DataManager.inst.BeatmapThemeIDToIndex.ContainsKey(int.Parse(orig.id)))
                {
                    var array = DataManager.inst.CustomBeatmapThemes.Where(x => x.id == orig.id).Select(x => x.name).ToArray();
                    var str = RTString.ArrayToString(array);

                    if (CoreHelper.InEditor)
                        EditorManager.inst.DisplayNotification($"Unable to load Theme [{orig.name}] due to conflicting themes: {str}", 2f * array.Length, EditorManager.NotificationType.Error);

                    RTThemeEditor.inst.SetupThemePanel(orig, false, true);
                }
                else
                {
                    DataManager.inst.BeatmapThemeIndexToID.Add(DataManager.inst.AllThemes.Count - 1, int.Parse(orig.id));
                    DataManager.inst.BeatmapThemeIDToIndex.Add(int.Parse(orig.id), DataManager.inst.AllThemes.Count - 1);

                    try
                    {
                        RTThemeEditor.inst.SetupThemePanel(orig, false);
                    }
                    catch (Exception ex)
                    {
                        EditorManager.inst.DisplayNotification($"Unable to load Theme [{orig.name}] for some reason. Press {CoreConfig.Instance.OpenPAPersistentFolder.Value} key to open the log folder and give the Player.log file to RTMecha.", 8f, EditorManager.NotificationType.Error);
                        CoreHelper.LogException(ex);
                    }
                }

                if (jn["id"] == null)
                {
                    var beatmapTheme = BeatmapTheme.DeepCopy(orig);
                    beatmapTheme.id = LSText.randomNumString(BeatmapTheme.ID_LENGTH);
                    DataManager.inst.CustomBeatmapThemes.Remove(orig);
                    FileManager.inst.DeleteFileRaw(file);
                    RTThemeEditor.inst.SaveTheme(beatmapTheme);
                    DataManager.inst.CustomBeatmapThemes.Add(beatmapTheme);
                }
            }
            themesLoading = false;

            if (refreshGUI)
            {
                var themeSearch = dialogTmp.Find("theme-search").GetComponent<InputField>();
                yield return StartCoroutine(RTThemeEditor.inst.RenderThemeList(themeSearch.text));
            }

            yield break;
        }

        GameObject prefabExternalUpAFolderButton;
        public IEnumerator LoadPrefabs()
        {
            if (prefabsLoading)
                yield break;

            prefabsLoading = true;

            while (!PrefabEditor.inst || !PrefabEditor.inst.externalContent)
                yield return null;

            for (int i = RTPrefabEditor.inst.PrefabPanels.Count - 1; i >= 0; i--)
            {
                var prefabPanel = RTPrefabEditor.inst.PrefabPanels[i];
                if (prefabPanel.Dialog == PrefabDialog.External)
                {
                    Destroy(prefabPanel.GameObject);
                    RTPrefabEditor.inst.PrefabPanels.RemoveAt(i);
                }
            }

            var config = EditorConfig.Instance;

            var hoverSize = config.PrefabButtonHoverSize.Value;

            if (!prefabExternalAddButton)
            {
                DeleteChildren(PrefabEditor.inst.externalContent);

                prefabExternalAddButton = PrefabEditor.inst.CreatePrefab.Duplicate(PrefabEditor.inst.externalContent, "add new prefab");
                var text = prefabExternalAddButton.GetComponentInChildren<Text>();
                text.text = "New External Prefab";

                var hover = prefabExternalAddButton.AddComponent<HoverUI>();
                hover.animateSca = true;
                hover.animatePos = false;
                hover.size = hoverSize;

                var button = prefabExternalAddButton.GetComponent<Button>();
                button.onClick.ClearAll();

                var contextClickable = prefabExternalAddButton.AddComponent<ContextClickable>();
                contextClickable.onClick = eventData =>
                {
                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        ShowContextMenu(300f,
                            new ButtonFunction("Create folder", () => ShowFolderCreator($"{RTFile.ApplicationDirectory}{prefabListPath}", () => { UpdatePrefabPath(true); HideNameEditor(); })),
                            new ButtonFunction("Create prefab", () =>
                            {
                                PrefabEditor.inst.OpenDialog();
                                RTPrefabEditor.inst.createInternal = false;
                            }),
                            new ButtonFunction("Paste Prefab", RTPrefabEditor.inst.PastePrefab)
                            );

                        return;
                    }

                    if (RTPrefabEditor.inst.savingToPrefab && RTPrefabEditor.inst.prefabToSaveFrom != null)
                    {
                        RTPrefabEditor.inst.savingToPrefab = false;
                        RTPrefabEditor.inst.SavePrefab(RTPrefabEditor.inst.prefabToSaveFrom);

                        EditorManager.inst.HideDialog("Prefab Popup");

                        RTPrefabEditor.inst.prefabToSaveFrom = null;

                        EditorManager.inst.DisplayNotification("Applied all changes to new External Prefab.", 2f, EditorManager.NotificationType.Success);

                        return;
                    }

                    PrefabEditor.inst.OpenDialog();
                    RTPrefabEditor.inst.createInternal = false;
                };

                EditorThemeManager.AddGraphic(button.image, ThemeGroup.Add, true);
                EditorThemeManager.AddGraphic(text, ThemeGroup.Add_Text);
            }
            else
            {
                var hover = prefabExternalAddButton.GetComponent<HoverUI>();
                hover.animateSca = true;
                hover.animatePos = false;
                hover.size = hoverSize;
            }

            bool isExternal = true;

            var nameHorizontalOverflow = isExternal ? config.PrefabExternalNameHorizontalWrap.Value : config.PrefabInternalNameHorizontalWrap.Value;

            var nameVerticalOverflow = isExternal ? config.PrefabExternalNameVerticalWrap.Value : config.PrefabInternalNameVerticalWrap.Value;

            var nameFontSize = isExternal ? config.PrefabExternalNameFontSize.Value : config.PrefabInternalNameFontSize.Value;

            var typeHorizontalOverflow = isExternal ? config.PrefabExternalTypeHorizontalWrap.Value : config.PrefabInternalTypeHorizontalWrap.Value;

            var typeVerticalOverflow = isExternal ? config.PrefabExternalTypeVerticalWrap.Value : config.PrefabInternalTypeVerticalWrap.Value;

            var typeFontSize = isExternal ? config.PrefabExternalTypeFontSize.Value : config.PrefabInternalTypeFontSize.Value;

            var deleteAnchoredPosition = isExternal ? config.PrefabExternalDeleteButtonPos.Value : config.PrefabInternalDeleteButtonPos.Value;
            var deleteSizeDelta = isExternal ? config.PrefabExternalDeleteButtonSca.Value : config.PrefabInternalDeleteButtonSca.Value;

            while (RTPrefabEditor.loadingPrefabTypes)
                yield return null;

            // Back
            if (!prefabExternalUpAFolderButton)
            {
                prefabExternalUpAFolderButton = EditorManager.inst.folderButtonPrefab.Duplicate(PrefabEditor.inst.externalContent, "back");
                var folderButtonStorageFolder = prefabExternalUpAFolderButton.GetComponent<FunctionButtonStorage>();
                var folderButtonFunctionFolder = prefabExternalUpAFolderButton.AddComponent<FolderButtonFunction>();

                var hoverUIFolder = prefabExternalUpAFolderButton.AddComponent<HoverUI>();
                hoverUIFolder.size = hoverSize;
                hoverUIFolder.animatePos = false;
                hoverUIFolder.animateSca = true;

                folderButtonStorageFolder.text.text = "< Up a folder";

                folderButtonStorageFolder.button.onClick.ClearAll();
                folderButtonFunctionFolder.onClick = eventData =>
                {
                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        ShowContextMenu(300f,
                            new ButtonFunction("Create folder", () => ShowFolderCreator($"{RTFile.ApplicationDirectory}{prefabListPath}", () => { UpdatePrefabPath(true); HideNameEditor(); })),
                            new ButtonFunction("Paste Prefab", RTPrefabEditor.inst.PastePrefab));

                        return;
                    }

                    if (prefabPathField.text == prefabPath)
                    {
                        prefabPathField.text = RTFile.GetDirectory(RTFile.ApplicationDirectory + prefabListPath).Replace(RTFile.ApplicationDirectory + "beatmaps/", "");
                        UpdatePrefabPath(false);
                    }
                };

                EditorThemeManager.ApplySelectable(folderButtonStorageFolder.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(folderButtonStorageFolder.text);
            }

            prefabExternalUpAFolderButton.SetActive(RTFile.GetDirectory(RTFile.ApplicationDirectory + prefabListPath) != RTFile.ApplicationDirectory + "beatmaps");

            var directories = Directory.GetDirectories(RTFile.ApplicationDirectory + prefabListPath, "*", SearchOption.TopDirectoryOnly);

            for (int i = 0; i < directories.Length; i++)
            {
                var directory = directories[i];
                var path = RTFile.ReplaceSlash(directory);
                var fileName = Path.GetFileName(directory);

                var gameObjectFolder = EditorManager.inst.folderButtonPrefab.Duplicate(PrefabEditor.inst.externalContent, $"Folder [{fileName}]");
                var folderButtonStorageFolder = gameObjectFolder.GetComponent<FunctionButtonStorage>();
                var folderButtonFunctionFolder = gameObjectFolder.AddComponent<FolderButtonFunction>();

                var hoverUIFolder = gameObjectFolder.AddComponent<HoverUI>();
                hoverUIFolder.size = hoverSize;
                hoverUIFolder.animatePos = false;
                hoverUIFolder.animateSca = true;

                folderButtonStorageFolder.text.text = fileName;

                folderButtonStorageFolder.button.onClick.ClearAll();
                folderButtonFunctionFolder.onClick = eventData =>
                {
                    if (!path.Contains(RTFile.ApplicationDirectory + "beatmaps/"))
                    {
                        EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                        return;
                    }

                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        ShowContextMenu(300f,
                            new ButtonFunction("Open folder", () =>
                            {
                                prefabPathField.text = path.Replace(RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/", "");
                                UpdatePrefabPath(false);
                            }),
                            new ButtonFunction("Create folder", () => ShowFolderCreator($"{RTFile.ApplicationDirectory}{prefabListPath}", () => { UpdatePrefabPath(true); HideNameEditor(); })),
                            new ButtonFunction(true),
                            new ButtonFunction("Paste Prefab", RTPrefabEditor.inst.PastePrefab),
                            new ButtonFunction("Delete", () => ShowWarningPopup("Are you <b>100%</b> sure you want to delete this folder? This <b>CANNOT</b> be undone! Always make sure you have backups.", () =>
                                {
                                    Directory.Delete(path, true);
                                    UpdatePrefabPath(true);
                                    EditorManager.inst.DisplayNotification("Deleted folder!", 2f, EditorManager.NotificationType.Success);
                                    HideWarningPopup();
                                }, HideWarningPopup))
                            );

                        return;
                    }

                    prefabPathField.text = path.Replace(RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/", "");
                    UpdatePrefabPath(false);
                };

                EditorThemeManager.ApplySelectable(folderButtonStorageFolder.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(folderButtonStorageFolder.text);

                RTPrefabEditor.inst.PrefabPanels.Add(new PrefabPanel
                {
                    GameObject = gameObjectFolder,
                    FilePath = directory,
                    Dialog = PrefabDialog.External,
                    isFolder = true,
                });
            }

            var files = Directory.GetFiles(RTFile.ApplicationDirectory + prefabListPath, FileFormat.LSP.ToPattern(), SearchOption.TopDirectoryOnly);

            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var jn = JSON.Parse(RTFile.ReadFromFile(file));

                var prefab = Prefab.Parse(jn);
                prefab.objects.ForEach(x => (x as BeatmapObject).RemovePrefabReference());
                prefab.filePath = RTFile.ReplaceSlash(file);

                RTPrefabEditor.inst.CreatePrefabButton(prefab, i, PrefabDialog.External, file, false, hoverSize,
                         nameHorizontalOverflow, nameVerticalOverflow, nameFontSize,
                         typeHorizontalOverflow, typeVerticalOverflow, typeFontSize,
                         deleteAnchoredPosition, deleteSizeDelta);
            }

            prefabsLoading = false;

            yield break;
        }

        public IEnumerator UpdatePrefabs()
        {
            yield return inst.StartCoroutine(LoadPrefabs());
            PrefabEditor.inst.ReloadExternalPrefabsInPopup();
            EditorManager.inst.DisplayNotification("Updated external prefabs!", 2f, EditorManager.NotificationType.Success);
            yield break;
        }

        public void SetAutoSave()
        {
            var autosavesDirectory = RTFile.CombinePaths(RTFile.BasePath, "autosaves");
            RTFile.CreateDirectory(autosavesDirectory);
            var files = Directory.GetFiles(autosavesDirectory, $"autosave_*{FileFormat.LSB.Dot()}", SearchOption.TopDirectoryOnly);

            EditorManager.inst.autosaves.Clear();
            EditorManager.inst.autosaves.AddRange(files);

            EditorManager.inst.CancelInvoke(nameof(AutoSaveLevel));
            CancelInvoke(nameof(AutoSaveLevel));
            InvokeRepeating(nameof(AutoSaveLevel), EditorConfig.Instance.AutosaveLoopTime.Value, EditorConfig.Instance.AutosaveLoopTime.Value);
        }

        public void AutoSaveLevel()
        {
            if (EditorManager.inst.loading)
                return;

            autoSaving = true;

            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Beatmap can't autosave until you load a level.", 3f, EditorManager.NotificationType.Error);
                autoSaving = false;
                return;
            }

            if (EditorManager.inst.savingBeatmap)
            {
                EditorManager.inst.DisplayNotification("Already attempting to save the beatmap!", 2f, EditorManager.NotificationType.Error);
                autoSaving = false;
                return;
            }

            var autosavesDirectory = RTFile.CombinePaths(RTFile.BasePath, "autosaves");
            var autosavePath = RTFile.CombinePaths(autosavesDirectory, $"autosave_{DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss")}{FileFormat.LSB.Dot()}");

            RTFile.CreateDirectory(autosavesDirectory);

            EditorManager.inst.DisplayNotification("Autosaving backup...", 2f, EditorManager.NotificationType.Warning);

            EditorManager.inst.autosaves.Add(autosavePath);

            while (EditorManager.inst.autosaves.Count > EditorConfig.Instance.AutosaveLimit.Value)
            {
                var first = EditorManager.inst.autosaves[0];
                if (RTFile.FileExists(first))
                    File.Delete(first);

                EditorManager.inst.autosaves.RemoveAt(0);
            }

            GameData.Current.SaveData(autosavePath);

            EditorManager.inst.DisplayNotification("Autosaved backup!", 2f, EditorManager.NotificationType.Success);

            autoSaving = false;
        }

        public void SaveBackup()
        {
            if (EditorManager.inst.loading)
                return;

            autoSaving = true;

            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Beatmap can't autosave until you load a level.", 3f, EditorManager.NotificationType.Error);
                autoSaving = false;
                return;
            }

            if (EditorManager.inst.savingBeatmap)
            {
                EditorManager.inst.DisplayNotification("Already attempting to save the beatmap!", 2f, EditorManager.NotificationType.Error);
                autoSaving = false;
                return;
            }

            var autosavesDirectory = RTFile.CombinePaths(RTFile.BasePath, "autosaves");
            var autosavePath = RTFile.CombinePaths(autosavesDirectory, $"backup_{DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss")}{FileFormat.LSB.Dot()}");

            RTFile.CreateDirectory(autosavesDirectory);

            EditorManager.inst.DisplayNotification("Saving backup...", 2f, EditorManager.NotificationType.Warning);

            GameData.Current.SaveData(autosavePath);

            EditorManager.inst.DisplayNotification("Autosaved backup!", 2f, EditorManager.NotificationType.Success);

            autoSaving = false;
        }

        public void CreateNewLevel()
        {
            if (string.IsNullOrEmpty(EditorManager.inst.newAudioFile))
            {
                EditorManager.inst.DisplayNotification("The file path is empty.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            if (!RTFile.FileExists(EditorManager.inst.newAudioFile))
            {
                EditorManager.inst.DisplayNotification("The file you are trying to load doesn't appear to exist.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var audioFormat = RTFile.GetFileFormat(EditorManager.inst.newAudioFile);
            if (!RTFile.ValidAudio(audioFormat))
            {
                EditorManager.inst.DisplayNotification($"The file you are trying to load doesn't appear to be a song file.\nDetected format: {audioFormat}", 6f, EditorManager.NotificationType.Error);
                return;
            }

            bool setNew = false;
            int num = 0;
            string p = RTFile.ApplicationDirectory + editorListSlash + EditorManager.inst.newLevelName;
            while (RTFile.DirectoryExists(p))
            {
                p = RTFile.ApplicationDirectory + editorListSlash + EditorManager.inst.newLevelName + " - " + num.ToString();
                num += 1;
                setNew = true;

            }
            if (setNew)
                EditorManager.inst.newLevelName += " - " + num.ToString();

            var path = RTFile.ApplicationDirectory + editorListSlash + EditorManager.inst.newLevelName;

            if (RTFile.DirectoryExists(path))
            {
                EditorManager.inst.DisplayNotification($"The level you are trying to create already exists.\nName: {Path.GetFileName(path)}", 4f, EditorManager.NotificationType.Error);
                return;
            }
            Directory.CreateDirectory(path);

            RTFile.CopyFile(EditorManager.inst.newAudioFile, RTFile.CombinePaths(path, $"level{audioFormat.Dot()}"));

            var json =
                currentLevelTemplate >= 0 && currentLevelTemplate < NewLevelTemplates.Count && RTFile.FileExists(NewLevelTemplates[currentLevelTemplate]) ?
                    RTFile.ReadFromFile(NewLevelTemplates[currentLevelTemplate]) : null;

            var gameData = !string.IsNullOrEmpty(json) ? GameData.Parse(JSON.Parse(json), false) : CreateBaseBeatmap();

            gameData.SaveData(RTFile.CombinePaths(path, Level.LEVEL_LSB));
            var metaData = new MetaData();
            metaData.beatmap.game_version = ProjectArrhythmia.GameVersion.ToString();
            metaData.arcadeID = LSText.randomNumString(16);
            metaData.song.title = newLevelSongTitle;
            metaData.uploaderName = SteamWrapper.inst.user.displayName;
            metaData.creator.steam_name = SteamWrapper.inst.user.displayName;
            metaData.creator.steam_id = SteamWrapper.inst.user.id;
            metaData.beatmap.name = EditorManager.inst.newLevelName;

            DataManager.inst.metaData = metaData;

            fromNewLevel = true;
            DataManager.inst.SaveMetadata(RTFile.CombinePaths(path, Level.METADATA_LSB));
            StartCoroutine(LoadLevel(path));
            EditorManager.inst.HideDialog("New File Popup");
        }

        public string newLevelSongTitle = "Inertia";

        public GameData CreateBaseBeatmap()
        {
            var gameData = new GameData();
            gameData.beatmapData = new LevelBeatmapData();
            gameData.beatmapData.levelData = new LevelData()
            {
                limitPlayer = false,
            };
            gameData.beatmapData.editorData = new LevelEditorData();
            gameData.beatmapData.checkpoints.Add(new DataManager.GameData.BeatmapData.Checkpoint(false, BASE_CHECKPOINT_NAME, 0f, Vector2.zero));

            if (gameData.eventObjects.allEvents == null)
                gameData.eventObjects.allEvents = new List<List<BaseEventKeyframe>>();
            gameData.eventObjects.allEvents.Clear();
            GameData.ClampEventListValues(gameData.eventObjects.allEvents);

            for (int i = 0; i < (CoreHelper.AprilFools ? 45 : 25); i++)
            {
                var backgroundObject = new BackgroundObject();
                backgroundObject.name = "bg - " + i;
                if (UnityEngine.Random.value > 0.5f)
                {
                    backgroundObject.scale = new Vector2(UnityEngine.Random.Range(2, 8), UnityEngine.Random.Range(2, 8));
                }
                else
                {
                    float num = UnityEngine.Random.Range(2, 6);
                    backgroundObject.scale = new Vector2(num, num);
                }
                backgroundObject.pos = new Vector2(UnityEngine.Random.Range(-48, 48), UnityEngine.Random.Range(-32, 32));
                backgroundObject.color = UnityEngine.Random.Range(1, 6);
                backgroundObject.layer = UnityEngine.Random.Range(0, 6);
                backgroundObject.reactive = UnityEngine.Random.value > 0.5f;
                if (backgroundObject.reactive)
                {
                    switch (UnityEngine.Random.Range(0, 4))
                    {
                        case 0:
                            backgroundObject.reactiveType = BaseBackgroundObject.ReactiveType.LOW;
                            break;
                        case 1:
                            backgroundObject.reactiveType = BaseBackgroundObject.ReactiveType.MID;
                            break;
                        case 2:
                            backgroundObject.reactiveType = BaseBackgroundObject.ReactiveType.HIGH;
                            break;
                    }
                    backgroundObject.reactiveScale = UnityEngine.Random.Range(0.01f, 0.04f);
                }

                var randomShape = UnityEngine.Random.Range(0, ShapeManager.inst.Shapes3D.Count - 1);
                if (CoreHelper.AprilFools)
                {
                    if (randomShape != 4 && randomShape != 6)
                        backgroundObject.shape = ShapeManager.inst.Shapes3D[randomShape][0];
                }

                gameData.backgroundObjects.Add(backgroundObject);
            }

            var beatmapObject = ObjectEditor.CreateNewBeatmapObject(0.5f, false);
            beatmapObject.events[0].Add(new EventKeyframe(4f, new float[3] { 10f, 0f, 0f }, new float[3]));
            if (CoreHelper.AprilFools)
                beatmapObject.events[2].Add(new EventKeyframe(12f, new float[1] { 360000f }, new float[3]));

            beatmapObject.name = CoreHelper.AprilFools ? "trololololo" : DEFAULT_OBJECT_NAME;
            beatmapObject.autoKillType = AutoKillType.LastKeyframeOffset;
            beatmapObject.autoKillOffset = 4f;
            beatmapObject.editorData.layer = 0;
            gameData.beatmapObjects.Add(beatmapObject);

            return gameData;
        }

        #endregion

        #region Refresh Popups / Dialogs

        public float contextButtonHeight = 37f;
        public float contextSpacerHeight = 6f;

        public void ShowContextMenu(float width, List<ButtonFunction> buttonFunctions) => ShowContextMenu(width, buttonFunctions.ToArray());
        public void ShowContextMenu(float width, params ButtonFunction[] buttonFunctions)
        {
            float height = 0f;
            contextMenu.SetActive(true);
            LSHelpers.DeleteChildren(contextMenuLayout);
            for (int i = 0; i < buttonFunctions.Length; i++)
            {
                var buttonFunction = buttonFunctions[i];

                if (buttonFunction.IsSpacer)
                {
                    var g = Creator.NewUIObject("sp", contextMenuLayout);
                    var image = g.AddComponent<Image>();
                    image.rectTransform.sizeDelta = new Vector2(0f, buttonFunction.SpacerSize);
                    EditorThemeManager.ApplyGraphic(image, ThemeGroup.Background_3);
                    height += contextSpacerHeight;
                    continue;
                }

                var gameObject = EditorPrefabHolder.Instance.Function2Button.Duplicate(contextMenuLayout);
                var buttonStorage = gameObject.GetComponent<FunctionButtonStorage>();

                buttonStorage.button.onClick.ClearAll();
                buttonStorage.button.onClick.AddListener(() =>
                {
                    contextMenu.SetActive(false);
                    buttonFunction.Action?.Invoke();
                });
                buttonStorage.text.alignment = TextAnchor.MiddleLeft;
                buttonStorage.text.text = buttonFunction.Name;
                buttonStorage.text.rectTransform.sizeDelta = new Vector2(-12f, 0f);

                EditorThemeManager.ApplySelectable(buttonStorage.button, ThemeGroup.Function_2);
                EditorThemeManager.ApplyGraphic(buttonStorage.text, ThemeGroup.Function_2_Text);
                height += contextButtonHeight;
            }

            var pos = Input.mousePosition * CoreHelper.ScreenScaleInverse;
            pos.y = Mathf.Clamp(pos.y, height, float.PositiveInfinity);
            contextMenu.transform.AsRT().anchoredPosition = pos;
            contextMenu.transform.AsRT().sizeDelta = new Vector2(width, height);
        }

        public void ShowFolderCreator(string path, Action onSubmit)
        {
            EditorManager.inst.ShowDialog("Folder Creator Popup");
            RefreshFolderCreator(path, onSubmit);
        }

        public void RefreshFolderCreator(string path, Action onSubmit) => RefreshNameEditor("Folder Creator", "New folder name", "Create Folder", () =>
        {
            var directory = RTFile.CombinePaths(path, RTFile.ValidateDirectory(folderCreatorName.text));

            if (RTFile.CreateDirectory(directory))
                onSubmit?.Invoke();
        });

        public void HideNameEditor() => EditorManager.inst.HideDialog("Folder Creator Popup");

        public void ShowNameEditor(string title, string nameLabel, string submitText, Action onSubmit)
        {
            EditorManager.inst.ShowDialog("Folder Creator Popup");
            RefreshNameEditor(title, nameLabel, submitText, onSubmit);
        }

        public void RefreshNameEditor(string title, string nameLabel, string submitText, Action onSubmit)
        {
            folderCreatorTitle.text = title;
            folderCreatorNameLabel.text = nameLabel;
            folderCreatorSubmitText.text = submitText;

            folderCreatorSubmit.onClick.ClearAll();
            folderCreatorSubmit.onClick.AddListener(() => onSubmit?.Invoke());

        }

        public void ShowObjectSearch(Action<BeatmapObject> onSelect, bool clearParent = false, List<BeatmapObject> beatmapObjects = null)
        {
            EditorManager.inst.ShowDialog("Object Search Popup");
            RefreshObjectSearch(onSelect, clearParent, beatmapObjects);
        }

        public void RefreshObjectSearch(Action<BeatmapObject> onSelect, bool clearParent = false, List<BeatmapObject> beatmapObjects = null)
        {
            var dialog = EditorManager.inst.GetDialog("Object Search Popup").Dialog;
            var content = dialog.Find("mask/content");

            if (clearParent)
            {
                var buttonPrefab = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(content, "Clear Parents");
                buttonPrefab.transform.GetChild(0).GetComponent<Text>().text = "Clear Parents";

                var button = buttonPrefab.GetComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(() =>
                {
                    foreach (var bm in ObjectEditor.inst.SelectedObjects.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                    {
                        bm.parent = "";
                        Updater.UpdateObject(bm);
                    }
                });

                var x = EditorManager.inst.GetDialog("Object Search Popup").Dialog.Find("Panel/x/Image").GetComponent<Image>().sprite;
                var image = buttonPrefab.transform.Find("Image").GetComponent<Image>();
                image.color = Color.red;
                image.sprite = x;
            }

            var searchBar = dialog.Find("search-box/search").GetComponent<InputField>();
            searchBar.onValueChanged.ClearAll();
            searchBar.onValueChanged.AddListener(_val =>
            {
                objectSearchTerm = _val;
                RefreshObjectSearch(onSelect, clearParent, beatmapObjects);
            });

            LSHelpers.DeleteChildren(content);

            if (beatmapObjects == null)
                beatmapObjects = GameData.Current.beatmapObjects;

            var list = beatmapObjects.FindAll(x => !x.fromPrefab);
            foreach (var beatmapObject in list)
            {
                var regex = new Regex(@"\[([0-9])\]");
                var match = regex.Match(objectSearchTerm);

                if (string.IsNullOrEmpty(objectSearchTerm) ||
                    match.Success && int.TryParse(match.Groups[1].ToString(), out int index) && index < beatmapObjects.Count && beatmapObjects.IndexOf(beatmapObject) == index ||
                    beatmapObject.id == objectSearchTerm ||
                    beatmapObject.name.ToLower().Contains(objectSearchTerm.ToLower()))
                {
                    string nm = $"[{(list.IndexOf(beatmapObject) + 1).ToString("0000")}/{list.Count.ToString("0000")} - {beatmapObject.id}] : {beatmapObject.name}";
                    var buttonPrefab = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(content, nm);
                    var buttonText = buttonPrefab.transform.GetChild(0).GetComponent<Text>();
                    buttonText.text = nm;

                    var button = buttonPrefab.GetComponent<Button>();
                    button.onClick.ClearAll();
                    button.onClick.AddListener(() => { onSelect?.Invoke(beatmapObject); });

                    var image = buttonPrefab.transform.Find("Image").GetComponent<Image>();
                    image.color = GetObjectColor(beatmapObject, false);

                    var shape = Mathf.Clamp(beatmapObject.shape, 0, ShapeManager.inst.Shapes2D.Count - 1);
                    var shapeOption = Mathf.Clamp(beatmapObject.shapeOption, 0, ShapeManager.inst.Shapes2D[shape].Count - 1);

                    image.sprite = ShapeManager.inst.Shapes2D[shape][shapeOption].Icon;

                    EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                    EditorThemeManager.ApplyLightText(buttonText);

                    string desc = "";
                    string hint = "";

                    if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
                    {
                        var transform = levelObject.visualObject.GameObject.transform;

                        string parent = "";
                        if (!string.IsNullOrEmpty(beatmapObject.parent))
                            parent = "<br>P: " + beatmapObject.parent + " (" + beatmapObject.GetParentType() + ")";
                        else
                            parent = "<br>P: No Parent" + " (" + beatmapObject.GetParentType() + ")";

                        string text = "";
                        if (beatmapObject.shape != 4 || beatmapObject.shape != 6)
                            text = "<br>S: " + CoreHelper.GetShape(beatmapObject.shape, beatmapObject.shapeOption) +
                                "<br>T: " + beatmapObject.text;
                        if (beatmapObject.shape == 4)
                            text = "<br>S: Text" +
                                "<br>T: " + beatmapObject.text;
                        if (beatmapObject.shape == 6)
                            text = "<br>S: Image" +
                                "<br>T: " + beatmapObject.text;

                        string ptr = "";
                        if (!string.IsNullOrEmpty(beatmapObject.prefabID) && !string.IsNullOrEmpty(beatmapObject.prefabInstanceID))
                            ptr = "<br><#" + CoreHelper.ColorToHex((beatmapObject).Prefab.PrefabType.Color) + ">PID: " + beatmapObject.prefabID + " | PIID: " + beatmapObject.prefabInstanceID + "</color>";
                        else
                            ptr = "<br>Not from prefab";

                        desc = "N/ST: " + beatmapObject.name + " [ " + beatmapObject.StartTime + " ]";
                        hint = "ID: {" + beatmapObject.id + "}" +
                            parent +
                            "<br>Alive: " + beatmapObject.Alive.ToString() +
                            "<br>Origin: {X: " + beatmapObject.origin.x + ", Y: " + beatmapObject.origin.y + "}" +
                            text +
                            "<br>Depth: " + beatmapObject.Depth +
                            "<br>ED: {L: " + beatmapObject.editorData.layer + ", B: " + beatmapObject.editorData.Bin + "}" +
                            "<br>POS: {X: " + transform.position.x + ", Y: " + transform.position.y + "}" +
                            "<br>SCA: {X: " + transform.localScale.x + ", Y: " + transform.localScale.y + "}" +
                            "<br>ROT: " + transform.eulerAngles.z +
                            "<br>COL: " + "<#" + CoreHelper.ColorToHex(GetObjectColor(beatmapObject, false)) + ">" + "█ <b>#" + CoreHelper.ColorToHex(GetObjectColor(beatmapObject, true)) + "</b></color>" +
                            ptr;

                        TooltipHelper.AddHoverTooltip(buttonPrefab, desc, hint);
                    }
                }
            }
        }

        public void HideWarningPopup() => EditorManager.inst.HideDialog("Warning Popup");

        public void ShowWarningPopup(string warning, UnityAction confirmDelegate, UnityAction cancelDelegate, string confirm = "Yes", string cancel = "No")
        {
            EditorManager.inst.ShowDialog("Warning Popup");
            RefreshWarningPopup(warning, confirmDelegate, cancelDelegate, confirm, cancel);

            var warningPopup = EditorManager.inst.GetDialog("Warning Popup").Dialog.GetChild(0);
            if (ExampleManager.inst && ExampleManager.inst.Visible && Vector2.Distance(ExampleManager.inst.TotalPosition, warningPopup.localPosition + new Vector3(140f, 200f)) > 20f)
            {
                ExampleManager.inst.Move(
                    new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0.4f, warningPopup.localPosition.x + 120f, Ease.SineOut),
                        new FloatKeyframe(0.6f, warningPopup.localPosition.x + 140f, Ease.SineInOut),
                    }, new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0.5f, warningPopup.localPosition.y + 200f, Ease.SineInOut),
                    });
                ExampleManager.inst.BrowsRaise();
            }
        }

        public void RefreshWarningPopup(string warning, UnityAction confirmDelegate, UnityAction cancelDelegate, string confirm = "Yes", string cancel = "No")
        {
            var warningPopup = EditorManager.inst.GetDialog("Warning Popup").Dialog.GetChild(0);

            var close = warningPopup.Find("Panel/x").GetComponent<Button>();
            close.onClick.ClearAll();
            close.onClick.AddListener(() => cancelDelegate?.Invoke());

            warningPopup.Find("Level Name").GetComponent<Text>().text = warning;

            var submit1 = warningPopup.Find("spacerL/submit1");
            var submit2 = warningPopup.Find("spacerL/submit2");

            var submit1Button = submit1.GetComponent<Button>();
            var submit2Button = submit2.GetComponent<Button>();

            submit1.Find("text").GetComponent<Text>().text = confirm;
            submit2.Find("text").GetComponent<Text>().text = cancel;

            submit1Button.onClick.ClearAll();
            submit2Button.onClick.ClearAll();

            submit1Button.onClick.AddListener(confirmDelegate);
            submit2Button.onClick.AddListener(cancelDelegate);
        }

        public IEnumerator RefreshLevelList()
        {
            CoreHelper.Log($"Level Search: {EditorManager.inst.openFileSearch}\nLevel Sort: {levelAscend} - {levelFilter}");

            #region Sorting

            Func<MetadataWrapper, bool> editorFolderSelector = x => x is EditorWrapper editorWrapper && !editorWrapper.isFolder;
            var loadedLevels = EditorManager.inst.loadedLevels.Select(x => x as EditorWrapper);

            switch (levelFilter)
            {
                case 0:
                    {
                        EditorManager.inst.loadedLevels = (levelAscend ? loadedLevels.OrderBy(x => x.albumArt != SteamWorkshop.inst.defaultSteamImageSprite) :
                            loadedLevels.OrderByDescending(x => x.albumArt != SteamWorkshop.inst.defaultSteamImageSprite)).OrderBy(editorFolderSelector).ToList();
                        break;
                    }
                case 1:
                    {
                        EditorManager.inst.loadedLevels = (levelAscend ? loadedLevels.OrderBy(x => x.metadata?.artist?.Name) :
                            loadedLevels.OrderByDescending(x => x.metadata?.artist?.Name)).OrderBy(editorFolderSelector).ToList();
                        break;
                    }
                case 2:
                    {
                        EditorManager.inst.loadedLevels = (levelAscend ? loadedLevels.OrderBy(x => x.metadata?.creator?.steam_name) :
                            loadedLevels.OrderByDescending(x => x.metadata?.creator?.steam_name)).OrderBy(editorFolderSelector).ToList();
                        break;
                    }
                case 3:
                    {
                        EditorManager.inst.loadedLevels = (levelAscend ? loadedLevels.OrderBy(x => x.folder) :
                            loadedLevels.OrderByDescending(x => x.folder)).OrderBy(editorFolderSelector).ToList();
                        break;
                    }
                case 4:
                    {
                        EditorManager.inst.loadedLevels = (levelAscend ? loadedLevels.OrderBy(x => x.metadata?.song?.title) :
                            loadedLevels.OrderByDescending(x => x.metadata?.song?.title)).OrderBy(editorFolderSelector).ToList();
                        break;
                    }
                case 5:
                    {
                        EditorManager.inst.loadedLevels = (levelAscend ? loadedLevels.OrderBy(x => x.metadata?.song?.difficulty) :
                            loadedLevels.OrderByDescending(x => x.metadata?.song?.difficulty)).OrderBy(editorFolderSelector).ToList();
                        break;
                    }
                case 6:
                    {
                        EditorManager.inst.loadedLevels = (levelAscend ? loadedLevels.OrderBy(x => x.metadata?.beatmap?.date_edited) :
                            loadedLevels.OrderByDescending(x => x.metadata?.beatmap?.date_edited)).OrderBy(editorFolderSelector).ToList();
                        break;
                    }
                case 7:
                    {
                        EditorManager.inst.loadedLevels = (levelAscend ? loadedLevels.OrderBy(x => x.metadata is MetaData metadata ? metadata.beatmap.date_created : "") :
                            loadedLevels.OrderByDescending(x => x.metadata is MetaData metadata ? metadata.beatmap.date_created : "")).OrderBy(editorFolderSelector).ToList();
                        break;
                    }
            }

            #endregion

            int num = 0;
            foreach (var editorWrapper in EditorManager.inst.loadedLevels.Where(x => x is EditorWrapper).Select(x => x as EditorWrapper))
            {
                var folder = editorWrapper.folder;
                var metadata = editorWrapper.metadata;

                string[] difficultyNames = new string[]
                {
                    "easy",
                    "normal",
                    "hard",
                    "expert",
                    "expert+",
                    "master",
                    "animation",
                    "Unknown difficulty",
                };

                editorWrapper.SetActive(editorWrapper.isFolder && RTString.SearchString(EditorManager.inst.openFileSearch, Path.GetFileName(folder)) ||
                    !editorWrapper.isFolder && (RTString.SearchString(EditorManager.inst.openFileSearch, Path.GetFileName(folder)) ||
                        metadata == null || metadata != null &&
                        (RTString.SearchString(EditorManager.inst.openFileSearch, metadata.song.title) ||
                        RTString.SearchString(EditorManager.inst.openFileSearch, metadata.artist.Name) ||
                        RTString.SearchString(EditorManager.inst.openFileSearch, metadata.creator.steam_name) ||
                        RTString.SearchString(EditorManager.inst.openFileSearch, metadata.song.description) ||
                        RTString.SearchString(EditorManager.inst.openFileSearch, difficultyNames[Mathf.Clamp(metadata.song.difficulty, 0, difficultyNames.Length - 1)]))));

                editorWrapper.GameObject.transform.SetSiblingIndex(num);
                num++;
            }

            var transform = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("mask/content");

            if (transform.Find("back"))
            {
                yield return null;
                transform.Find("back").SetAsFirstSibling();
            }

            yield break;
        }

        public void RefreshParentSearch(EditorManager __instance, TimelineObject timelineObject)
        {
            var transform = __instance.GetDialog("Parent Selector").Dialog.Find("mask/content");

            LSHelpers.DeleteChildren(transform);

            var noParent = __instance.folderButtonPrefab.Duplicate(transform, "No Parent");
            noParent.transform.localScale = Vector3.one;
            var noParentText = noParent.transform.GetChild(0).GetComponent<Text>();
            noParentText.text = "No Parent";
            var noParentButton = noParent.GetComponent<Button>();
            noParentButton.onClick.ClearAll();
            noParentButton.onClick.AddListener(() =>
            {
                var list = ObjectEditor.inst.SelectedObjects;
                foreach (var timelineObject in list)
                {
                    if (timelineObject.isPrefabObject)
                    {
                        var prefabObject = timelineObject.GetData<PrefabObject>();
                        prefabObject.parent = "";
                        Updater.UpdatePrefab(prefabObject);
                        RTPrefabEditor.inst.RenderPrefabObjectDialog(prefabObject);

                        continue;
                    }

                    var bm = timelineObject.GetData<BeatmapObject>();
                    bm.parent = "";
                    Updater.UpdateObject(bm);
                }

                EditorManager.inst.HideDialog("Parent Selector");
                if (list.Count == 1 && timelineObject.isBeatmapObject)
                    StartCoroutine(ObjectEditor.RefreshObjectGUI(timelineObject.GetData<BeatmapObject>()));
                if (list.Count == 1 && timelineObject.isPrefabObject)
                    RTPrefabEditor.inst.RenderPrefabObjectDialog(timelineObject.GetData<PrefabObject>());
            });

            EditorThemeManager.ApplySelectable(noParentButton, ThemeGroup.List_Button_1);
            EditorThemeManager.ApplyLightText(noParentText);

            if (RTString.SearchString(__instance.parentSearch, "camera"))
            {
                var cam = __instance.folderButtonPrefab.Duplicate(transform, "Camera");
                var camText = cam.transform.GetChild(0).GetComponent<Text>();
                var camButton = cam.GetComponent<Button>();

                camText.text = "Camera";
                camButton.onClick.ClearAll();
                camButton.onClick.AddListener(() =>
                {
                    var list = ObjectEditor.inst.SelectedObjects;
                    foreach (var timelineObject in list)
                    {
                        if (timelineObject.isPrefabObject)
                        {
                            var prefabObject = timelineObject.GetData<PrefabObject>();
                            prefabObject.parent = BeatmapObject.CAMERA_PARENT;
                            Updater.UpdatePrefab(prefabObject);

                            continue;
                        }

                        var bm = timelineObject.GetData<BeatmapObject>();
                        bm.parent = BeatmapObject.CAMERA_PARENT;
                        Updater.UpdateObject(bm);
                    }

                    EditorManager.inst.HideDialog("Parent Selector");
                    if (list.Count == 1 && timelineObject.isBeatmapObject)
                        StartCoroutine(ObjectEditor.RefreshObjectGUI(timelineObject.GetData<BeatmapObject>()));
                    if (list.Count == 1 && timelineObject.isPrefabObject)
                        RTPrefabEditor.inst.RenderPrefabObjectDialog(timelineObject.GetData<PrefabObject>());
                });

                EditorThemeManager.ApplySelectable(camButton, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(camText);
            }

            foreach (var obj in GameData.Current.beatmapObjects)
            {
                if (obj.fromPrefab)
                    continue;

                int index = GameData.Current.beatmapObjects.IndexOf(obj);
                if ((string.IsNullOrEmpty(__instance.parentSearch) || (obj.name + " " + index.ToString("0000")).ToLower().Contains(__instance.parentSearch.ToLower())) && obj.id != timelineObject.ID)
                {
                    bool canParent = true;
                    if (!string.IsNullOrEmpty(obj.parent))
                    {
                        string parentID = timelineObject.ID;
                        while (!string.IsNullOrEmpty(parentID))
                        {
                            if (parentID == obj.parent)
                            {
                                canParent = false;
                                break;
                            }

                            int parentIndex = GameData.Current.beatmapObjects.FindIndex(x => x.parent == parentID);

                            parentID = parentIndex != -1 ? GameData.Current.beatmapObjects[parentIndex].id : null;
                        }
                    }

                    if (!canParent)
                        continue;

                    string s = $"{obj.name} {index.ToString("0000")}";
                    var objectToParent = __instance.folderButtonPrefab.Duplicate(transform, s);
                    var objectToParentText = objectToParent.transform.GetChild(0).GetComponent<Text>();
                    var objectToParentButton = objectToParent.GetComponent<Button>();

                    objectToParentText.text = s;
                    objectToParentButton.onClick.ClearAll();
                    objectToParentButton.onClick.AddListener(() =>
                    {
                        string id = obj.id;

                        var list = ObjectEditor.inst.SelectedObjects;
                        foreach (var timelineObject in list)
                        {
                            if (timelineObject.isPrefabObject)
                            {
                                var prefabObject = timelineObject.GetData<PrefabObject>();
                                prefabObject.parent = id;
                                Updater.UpdatePrefab(prefabObject);

                                continue;
                            }

                            var bm = timelineObject.GetData<BeatmapObject>();
                            TriggerHelper.SetParent(timelineObject, ObjectEditor.inst.GetTimelineObject(obj));
                            Updater.UpdateObject(bm);
                        }

                        EditorManager.inst.HideDialog("Parent Selector");
                        if (list.Count == 1 && timelineObject.isBeatmapObject)
                            StartCoroutine(ObjectEditor.RefreshObjectGUI(timelineObject.GetData<BeatmapObject>()));
                        if (list.Count == 1 && timelineObject.isPrefabObject)
                            RTPrefabEditor.inst.RenderPrefabObjectParent(timelineObject.GetData<PrefabObject>());

                        Debug.Log($"{__instance.className}Set Parent ID: {id}");
                    });

                    EditorThemeManager.ApplySelectable(objectToParentButton, ThemeGroup.List_Button_1);
                    EditorThemeManager.ApplyLightText(objectToParentText);
                }
            }
        }

        void DeleteChildren(Transform transform)
        {
            var listToDelete = new List<GameObject>();
            for (int i = 0; i < transform.childCount; i++)
                listToDelete.Add(transform.GetChild(i).gameObject);
            for (int i = 0; i < listToDelete.Count; i++)
                DestroyImmediate(listToDelete[i]);
            listToDelete.Clear();
            listToDelete = null;
        }

        public void RefreshFileBrowserLevels() => RTFileBrowser.inst?.UpdateBrowserFile(".lsb", "level", x => StartCoroutine(LoadLevel(x.Replace("\\", "/").Replace("/level.lsb", ""))));

        public void RefreshDocumentation()
        {
            if (documentations.Count > 0)
                foreach (var document in documentations)
                {
                    var active = string.IsNullOrEmpty(documentationSearch) || document.Name.ToLower().Contains(documentationSearch.ToLower());
                    document.PopupButton?.SetActive(active);
                    if (active && document.PopupButton && document.PopupButton.TryGetComponent(out Button button))
                    {
                        button.onClick.ClearAll();
                        button.onClick.AddListener(() => { SelectDocumentation(document); });
                    }
                }
        }

        public void ShowDocumentation(string name)
        {
            if (documentations.TryFind(x => x.Name == name, out EditorDocument document))
                SelectDocumentation(document);
        }

        public void SelectDocumentation(EditorDocument document)
        {
            documentationTitle.text = $"- {document.Name} -";

            var singleInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position/x");
            var font = FontManager.inst.allFontAssets["Inconsolata Variable"];

            LSHelpers.DeleteChildren(documentationContent);

            int num = 0;
            foreach (var element in document.elements)
            {
                switch (element.type)
                {
                    case EditorDocument.Element.Type.Text:
                        {
                            if (string.IsNullOrEmpty(element.Data))
                                break;

                                var gameObject = Creator.NewUIObject("element", documentationContent, RectValues.Default
                                    .AnchoredPosition(1f, 0f)
                                    .AnchorMax(0f, 1f)
                                    .AnchorMin(0f, 1f)
                                    .Pivot(0f, 1f)
                                    .SizeDelta(722f, element.Autosize || element.Height == 0f ? (22f * LSText.WordWrap(element.Data, 67).Count) : element.Height));

                                var horizontalLayoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();

                                horizontalLayoutGroup.childControlHeight = false;
                                horizontalLayoutGroup.childControlWidth = false;
                                horizontalLayoutGroup.childForceExpandWidth = false;
                                horizontalLayoutGroup.spacing = 8f;
                                var image = gameObject.AddComponent<Image>();

                                var labels = Creator.NewUIObject("label", gameObject.transform, RectValues.Default
                                    .AnchorMax(0f, 1f)
                                    .AnchorMin(0f, 1f)
                                    .Pivot(0f, 1f)
                                    .SizeDelta(722f, 22f));

                                var labelsHLG = labels.AddComponent<HorizontalLayoutGroup>();

                                labelsHLG.childControlHeight = false;
                                labelsHLG.childControlWidth = false;
                                labelsHLG.spacing = 8f;

                                var label = Creator.NewUIObject("text", labels.transform);

                                var text = label.AddComponent<TextMeshProUGUI>();
                                text.font = font;
                                text.fontSize = 20;
                                text.enableWordWrapping = true;
                                text.text = element.Data;
                                EditorThemeManager.ApplyGraphic(text, ThemeGroup.Light_Text);

                                if (element.Function != null)
                                {
                                    var button = gameObject.AddComponent<Button>();
                                    button.onClick.AddListener(element.RunFunction);
                                    button.image = image;
                                    EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                                }
                                else
                                    EditorThemeManager.ApplyGraphic(image, ThemeGroup.List_Button_1_Normal, true);

                                RectValues.Default
                                    .AnchorMax(0f, 1f)
                                    .AnchorMin(0f, 1f)
                                    .Pivot(0f, 1f)
                                    .SizeDelta(722f, 22f).AssignToRectTransform(label.transform.AsRT());

                            break;
                        }
                    case EditorDocument.Element.Type.Image:
                        {
                            if (string.IsNullOrEmpty(element.Data))
                                break;

                            var gameObject = Creator.NewUIObject("element", documentationContent, RectValues.Default
                                .AnchoredPosition(1f, -48f)
                                .AnchorMax(0f, 1f)
                                .AnchorMin(0f, 1f)
                                .Pivot(0f, 1f)
                                .SizeDelta(720f, 432f));

                            var baseImage = gameObject.AddComponent<Image>();
                            var mask = gameObject.AddComponent<Mask>();

                            EditorThemeManager.ApplyGraphic(baseImage, ThemeGroup.List_Button_1_Normal, true);

                            var imageObject = Creator.NewUIObject("image", gameObject.transform, RectValues.Default
                                .AnchorMax(0f, 1f)
                                .AnchorMin(0f, 1f)
                                .Pivot(0f, 1f)
                                .SizeDelta(632f, 432f));

                            var image = imageObject.AddComponent<Image>();
                            image.color = new Color(1f, 1f, 1f, 1f);

                            if (RTFile.FileExists($"{RTFile.ApplicationDirectory}{element.Data}"))
                                image.sprite = SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{element.Data}");
                            else
                                image.enabled = false;

                            if (image.sprite && image.sprite.texture)
                            {
                                var width = Mathf.Clamp(image.sprite.texture.width, 0, 718);
                                gameObject.transform.AsRT().sizeDelta = new Vector2(width, image.sprite.texture.height);
                                imageObject.transform.AsRT().sizeDelta = new Vector2(width, image.sprite.texture.height);
                            }

                            if (element.Function != null)
                                imageObject.AddComponent<Button>().onClick.AddListener(element.RunFunction);

                            break;
                        }
                }

                // Spacer
                if (num != document.elements.Count - 1)
                {
                    var bar = singleInput.Duplicate(documentationContent, "spacer");
                    Destroy(bar.GetComponent<InputField>());
                    Destroy(bar.GetComponent<EventInfo>());
                    Destroy(bar.GetComponent<EventTrigger>());

                    LSHelpers.DeleteChildren(bar.transform);
                    bar.transform.localScale = Vector3.one;
                    bar.transform.AsRT().sizeDelta = new Vector2(764f, 2f);

                    var barImage = bar.GetComponent<Image>();
                    barImage.enabled = true;

                    EditorThemeManager.ApplyGraphic(barImage, ThemeGroup.Light_Text, true);
                }
                num++;
            }

            EditorManager.inst.ShowDialog("Documentation Dialog");
        }

        public void RefreshDebugger()
        {
            for (int i = 0; i < debugs.Count; i++)
                debuggerPopup.Content.GetChild(i).gameObject.SetActive(RTString.SearchString(debugSearch, debugs[i]));
        }

        public void RefreshAutosaveList(EditorWrapper editorWrapper)
        {
            autosaveSearchField.onValueChanged.ClearAll();
            autosaveSearchField.onValueChanged.AddListener(_val =>
            {
                autosaveSearch = _val;
                RefreshAutosaveList(editorWrapper);
            });

            var buttonHoverSize = EditorConfig.Instance.OpenLevelButtonHoverSize.Value;

            LSHelpers.DeleteChildren(autosaveContent);

            var files = Directory.GetFiles(editorWrapper.folder, "autosave_*.lsb", SearchOption.AllDirectories).Union(Directory.GetFiles(editorWrapper.folder, "backup_*.lsb", SearchOption.AllDirectories));

            foreach (var file in files)
            {
                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(autosaveContent, $"Folder [{Path.GetFileName(file)}]");
                var folderButtonStorage = gameObject.GetComponent<FunctionButtonStorage>();

                var hoverUI = gameObject.AddComponent<HoverUI>();
                hoverUI.size = buttonHoverSize;
                hoverUI.animatePos = false;
                hoverUI.animateSca = true;

                folderButtonStorage.text.text = Path.GetFileName(file);

                folderButtonStorage.button.onClick.ClearAll();
                folderButtonStorage.button.onClick.AddListener(() =>
                {
                    StartCoroutine(LoadLevel(editorWrapper.folder, file.Replace("\\", "/").Replace(editorWrapper.folder + "/", "")));
                    EditorManager.inst.HideDialog("Open File Popup");
                });

                string tmpFile = file;

                var backup = EditorPrefabHolder.Instance.Function1Button.Duplicate(gameObject.transform, "backup");
                var backupHolder = backup.GetComponent<FunctionButtonStorage>();
                backup.transform.localScale = Vector3.one;
                UIManager.SetRectTransform(backup.transform.AsRT(), new Vector2(450f, 0f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(80f, 28f));
                backupHolder.text.text = "Backup";
                backupHolder.button.onClick.ClearAll();
                backupHolder.button.onClick.AddListener(() =>
                {
                    var fi = new FileInfo(tmpFile);

                    tmpFile = tmpFile.Contains("autosave_") ? tmpFile.Replace("autosave_", "backup_") : tmpFile.Replace("backup_", "autosave_");

                    if (fi.Exists)
                        fi.MoveTo(tmpFile);

                    var fileName = Path.GetFileName(tmpFile);
                    folderButtonStorage.text.text = fileName;
                    gameObject.name = $"Folder [{fileName}]";

                    folderButtonStorage.button.onClick.ClearAll();
                    folderButtonStorage.button.onClick.AddListener(() =>
                    {
                        StartCoroutine(LoadLevel(editorWrapper.folder, tmpFile.Replace("\\", "/").Replace(editorWrapper.folder + "/", "")));
                        EditorManager.inst.HideDialog("Open File Popup");
                    });
                });

                EditorThemeManager.ApplySelectable(folderButtonStorage.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyGraphic(backupHolder.button.image, ThemeGroup.Function_1, true);
                EditorThemeManager.ApplyGraphic(backupHolder.text, ThemeGroup.Function_1_Text);
            }
        }

        public void RefreshNewLevelTemplates()
        {
            NewLevelTemplates.Clear();
            LSHelpers.DeleteChildren(newLevelTemplateContent);
            var texts = new List<Text>();

            var baseLevelTemplateGameObject = newLevelTemplatePrefab.Duplicate(newLevelTemplateContent);
            var basePreviewBase = baseLevelTemplateGameObject.transform.Find("Preview Base");
            basePreviewBase.Find("Preview").GetComponent<Image>().sprite = newLevelTemplateBaseSprite;

            var baseTitle = baseLevelTemplateGameObject.transform.Find("Title").GetComponent<Text>();
            baseTitle.text = $"Default Template{(currentLevelTemplate == -1 ? " [SELECTED]" : "")}";

            var baseButton = baseLevelTemplateGameObject.GetComponent<Button>();
            baseButton.onClick.ClearAll();
            baseButton.onClick.AddListener(() =>
            {
                currentLevelTemplate = -1;
                EditorManager.inst.DisplayNotification($"Set level template to default.", 1.6f, EditorManager.NotificationType.Success);

                UpdateSelectedTemplate(baseTitle, texts);
            });

            EditorThemeManager.ApplySelectable(baseButton, ThemeGroup.List_Button_1);
            EditorThemeManager.ApplyGraphic(basePreviewBase.GetComponent<Image>(), ThemeGroup.Null, true);
            EditorThemeManager.ApplyLightText(baseTitle);

            var baseDirectory = $"{RTFile.ApplicationDirectory}beatmaps/templates";

            if (!RTFile.DirectoryExists(baseDirectory))
                return;

            int num = 0;
            var directories = Directory.GetDirectories(baseDirectory, "*", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < directories.Length; i++)
            {
                var directory = directories[i];

                if (!RTFile.FileExists(directory + "/level.lsb"))
                    continue;

                var fileName = Path.GetFileName(directory);
                int index = num;

                var levelTemplateGameObject = newLevelTemplatePrefab.Duplicate(newLevelTemplateContent);

                var previewBase = levelTemplateGameObject.transform.Find("Preview Base");
                var previewImage = previewBase.Find("Preview").GetComponent<Image>();

                var button = levelTemplateGameObject.GetComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(() =>
                {
                    currentLevelTemplate = index;
                    EditorManager.inst.DisplayNotification($"Set level template to {fileName} [{currentLevelTemplate}]", 2f, EditorManager.NotificationType.Success);

                    UpdateSelectedTemplate(baseTitle, texts);
                });

                var title = levelTemplateGameObject.transform.Find("Title").GetComponent<Text>();
                title.text = $"{fileName}{(currentLevelTemplate == index ? " [SELECTED]" : "")}";
                texts.Add(title);

                EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyGraphic(previewBase.GetComponent<Image>(), ThemeGroup.Null, true);
                EditorThemeManager.ApplyLightText(title);

                if (RTFile.FileExists(directory + "/preview.png"))
                    previewImage.sprite = SpriteHelper.LoadSprite(directory + "/preview.png");
                else
                {
                    previewImage.color = new Color(1f, 1f, 1f, 0.1f);
                    previewImage.sprite = newLevelTemplateBaseSprite;
                    levelTemplateGameObject.transform.Find("No Preview").gameObject.SetActive(true);
                }

                NewLevelTemplates.Add(directory + "/level.lsb");
                num++;
            }

            currentLevelTemplate = Mathf.Clamp(currentLevelTemplate, -1, NewLevelTemplates.Count - 1);

            UpdateSelectedTemplate(baseTitle, texts);
        }

        void UpdateSelectedTemplate(Text baseTitle, List<Text> texts)
        {
            baseTitle.text = $"Default Template{(currentLevelTemplate == -1 ? " [SELECTED]" : "")}";
            for (int i = 0; i < texts.Count; i++)
            {
                var fileName = Path.GetFileName(Path.GetDirectoryName(NewLevelTemplates[i]));

                texts[i].text = $"{fileName}{(currentLevelTemplate == i ? " [SELECTED]" : "")}";
            }
        }

        public void PlayDialogAnimation(GameObject gameObject, string dialogName, bool active)
        {
            var play = EditorConfig.Instance.PlayEditorAnimations.Value;

            DialogAnimation dialogAnimation = null;
            var hasAnimation = DialogAnimations.TryFind(x => x.name == dialogName, out dialogAnimation);

            if (play && hasAnimation && gameObject.activeSelf != active)
            {
                if (!dialogAnimation.Active)
                {
                    gameObject.SetActive(active);

                    return;
                }

                var dialog = gameObject.transform;

                var scrollbar = dialog.GetComponentsInChildren<Scrollbar>().ToList();
                var scrollAmounts = scrollbar.Select(x => x.value).ToList();

                var animation = new RTAnimation("Popup Open");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, active ? dialogAnimation.PosStart.x : dialogAnimation.PosEnd.x, Ease.Linear),
                        new FloatKeyframe(active ? dialogAnimation.PosXStartDuration : dialogAnimation.PosXEndDuration, active ? dialogAnimation.PosEnd.x : dialogAnimation.PosStart.x, active ? Ease.GetEaseFunction(dialogAnimation.PosXStartEase) : Ease.GetEaseFunction(dialogAnimation.PosXEndEase)),
                        new FloatKeyframe(active ? dialogAnimation.PosXStartDuration : dialogAnimation.PosXEndDuration + 0.01f, active ? dialogAnimation.PosEnd.x : dialogAnimation.PosStart.x, Ease.Linear),
                    }, x =>
                    {
                        if (dialogAnimation.PosActive)
                        {
                            var pos = dialog.localPosition;
                            pos.x = x;
                            dialog.localPosition = pos;
                        }
                    }),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, active ? dialogAnimation.PosStart.y : dialogAnimation.PosEnd.y, Ease.Linear),
                        new FloatKeyframe(active ? dialogAnimation.PosYStartDuration : dialogAnimation.PosYEndDuration, active ? dialogAnimation.PosEnd.y : dialogAnimation.PosStart.y, active ? Ease.GetEaseFunction(dialogAnimation.PosYStartEase) : Ease.GetEaseFunction(dialogAnimation.PosYEndEase)),
                        new FloatKeyframe(active ? dialogAnimation.PosYStartDuration : dialogAnimation.PosYEndDuration + 0.01f, active ? dialogAnimation.PosEnd.y : dialogAnimation.PosStart.y, Ease.Linear),
                    }, x =>
                    {
                        if (dialogAnimation.PosActive)
                        {
                            var pos = dialog.localPosition;
                            pos.y = x;
                            dialog.localPosition = pos;
                        }
                    }),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, active ? dialogAnimation.ScaStart.x : dialogAnimation.ScaEnd.x, Ease.Linear),
                        new FloatKeyframe(active ? dialogAnimation.ScaXStartDuration : dialogAnimation.ScaXEndDuration, active ? dialogAnimation.ScaEnd.x : dialogAnimation.ScaStart.x, active ? Ease.GetEaseFunction(dialogAnimation.ScaXStartEase) : Ease.GetEaseFunction(dialogAnimation.ScaXEndEase)),
                        new FloatKeyframe(active ? dialogAnimation.ScaXStartDuration : dialogAnimation.ScaXEndDuration + 0.01f, active ? dialogAnimation.ScaEnd.x : dialogAnimation.ScaStart.x, Ease.Linear),
                    }, x =>
                    {
                        if (dialogAnimation.ScaActive)
                        {
                            var pos = dialog.localScale;
                            pos.x = x;
                            dialog.localScale = pos;

                            for (int i = 0; i < scrollbar.Count; i++)
                                scrollbar[i].value = scrollAmounts[i];
                        }
                    }),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, active ? dialogAnimation.ScaStart.y : dialogAnimation.ScaEnd.y, Ease.Linear),
                        new FloatKeyframe(active ? dialogAnimation.ScaYStartDuration : dialogAnimation.ScaYEndDuration, active ? dialogAnimation.ScaEnd.y : dialogAnimation.ScaStart.y, active ? Ease.GetEaseFunction(dialogAnimation.ScaYStartEase) : Ease.GetEaseFunction(dialogAnimation.ScaYEndEase)),
                        new FloatKeyframe(active ? dialogAnimation.ScaYStartDuration : dialogAnimation.ScaYEndDuration + 0.01f, active ? dialogAnimation.ScaEnd.y : dialogAnimation.ScaStart.y, Ease.Linear),
                    }, x =>
                    {
                        if (dialogAnimation.ScaActive)
                        {
                            var pos = dialog.localScale;
                            pos.y = x;
                            dialog.localScale = pos;

                            for (int i = 0; i < scrollbar.Count; i++)
                                scrollbar[i].value = scrollAmounts[i];
                        }
                    }),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, active ? dialogAnimation.RotStart : dialogAnimation.RotEnd, Ease.Linear),
                        new FloatKeyframe(active ? dialogAnimation.RotStartDuration : dialogAnimation.RotEndDuration, active ? dialogAnimation.RotEnd : dialogAnimation.RotStart, active ? Ease.GetEaseFunction(dialogAnimation.RotStartEase) : Ease.GetEaseFunction(dialogAnimation.RotEndEase)),
                        new FloatKeyframe(active ? dialogAnimation.RotStartDuration : dialogAnimation.RotEndDuration + 0.01f, active ? dialogAnimation.RotEnd : dialogAnimation.RotStart, Ease.Linear),
                    }, x =>
                    {
                        if (dialogAnimation.RotActive)
                            dialog.localRotation = Quaternion.Euler(0f, 0f, x);
                    }),
                };
                animation.id = LSText.randomNumString(16);

                animation.onComplete = () =>
                {
                    dialog.gameObject.SetActive(active);

                    if (dialogAnimation.PosActive)
                        dialog.localPosition = new Vector3(dialogAnimation.PosEnd.x, dialogAnimation.PosEnd.y, 0f);
                    if (dialogAnimation.ScaActive)
                        dialog.localScale = new Vector3(dialogAnimation.ScaEnd.x, dialogAnimation.ScaEnd.y, 1f);
                    if (dialogAnimation.RotActive)
                        dialog.localRotation = Quaternion.Euler(0f, 0f, dialogAnimation.RotEnd);

                    AnimationManager.inst.Remove(animation.id);
                };

                AnimationManager.inst.Play(animation);
            }

            if (!play || !hasAnimation || active)
                gameObject.SetActive(active);
        }

        public void SetDialogStatus(string dialogName, bool active, bool focus = true)
        {
            if (!EditorManager.inst.EditorDialogsDictionary.TryGetValue(dialogName, out EditorManager.EditorDialog editorDialog))
            {
                Debug.LogError($"{EditorManager.inst.className}Can't load dialog [{dialogName}].");
                return;
            }

            PlayDialogAnimation(editorDialog.Dialog.gameObject, dialogName, active);

            if (active)
            {
                if (focus)
                    EditorManager.inst.currentDialog = editorDialog;
                if (!EditorManager.inst.ActiveDialogs.Contains(editorDialog))
                    EditorManager.inst.ActiveDialogs.Add(editorDialog);
            }
            else
            {
                EditorManager.inst.ActiveDialogs.Remove(editorDialog);
                if (EditorManager.inst.currentDialog == editorDialog && focus)
                    EditorManager.inst.currentDialog = EditorManager.inst.ActiveDialogs.Count > 0 ? EditorManager.inst.ActiveDialogs.Last() : new EditorManager.EditorDialog();
            }
        }

        public void RefreshScreenshots()
        {
            var directory = RTFile.ApplicationDirectory + CoreConfig.Instance.ScreenshotsPath.Value;

            LSHelpers.DeleteChildren(screenshotContent);
            var files = Directory.GetFiles(directory, "*.png", SearchOption.TopDirectoryOnly);
            screenshotCount = files.Length;

            if (screenshotCount > screenshotsPerPage)
                TriggerHelper.AddEventTriggers(screenshotPageField.gameObject, TriggerHelper.ScrollDeltaInt(screenshotPageField, max: screenshotCount / screenshotsPerPage));
            else
                TriggerHelper.AddEventTriggers(screenshotPageField.gameObject);

            for (int i = 0; i < files.Length; i++)
            {
                if (!(i >= MinScreenshots && i < MaxScreenshots))
                    continue;

                var index = i;

                var gameObject = Creator.NewUIObject("screenshot", screenshotContent);
                gameObject.transform.localScale = Vector3.one;
                gameObject.transform.AsRT().sizeDelta = new Vector2(720f, 405f);

                var image = gameObject.AddComponent<Image>();
                image.enabled = false;

                var button = gameObject.AddComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(() => { System.Diagnostics.Process.Start(files[index]); });
                button.colors = UIManager.SetColorBlock(button.colors, Color.white, new Color(0.9f, 0.9f, 0.9f), new Color(0.7f, 0.7f, 0.7f), Color.white, Color.red);

                StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{files[i]}", texture2D =>
                {
                    if (!image)
                        return;

                    image.enabled = true;
                    image.sprite = SpriteHelper.CreateSprite(texture2D);
                }));
            }
        }

        #endregion

        #region Misc Functions

        public void OpenLevelListFolder() => RTFile.OpenInFileBrowser.Open(RTFile.CombinePaths(RTFile.ApplicationDirectory, editorListPath));
        public void OpenThemeListFolder() => RTFile.OpenInFileBrowser.Open(RTFile.CombinePaths(RTFile.ApplicationDirectory, themeListPath));
        public void OpenPrefabListFolder() => RTFile.OpenInFileBrowser.Open(RTFile.CombinePaths(RTFile.ApplicationDirectory, prefabListPath));

        public void ZIPLevel(string currentPath, string fileName)
        {
            EditorManager.inst.DisplayNotification($"Zipping {fileName}...", 2f, EditorManager.NotificationType.Warning);

            IZIPLevel(currentPath, fileName).StartAsync();
        }

        public IEnumerator IZIPLevel(string currentPath, string fileName)
        {
            bool failed;
            try
            {
                var zipPath = currentPath + ".zip";
                RTFile.DeleteFile(zipPath);

                System.IO.Compression.ZipFile.CreateFromDirectory(currentPath, zipPath);

                failed = false;
            }
            catch (Exception ex)
            {
                failed = true;
                CoreHelper.LogException(ex);
            }

            yield return Ninja.JumpToUnity;
            if (failed)
                EditorManager.inst.DisplayNotification($"Had an error with zipping the folder. Check the logs!", 2f, EditorManager.NotificationType.Error);
            else
                EditorManager.inst.DisplayNotification($"Successfully zipped the folder to {fileName}.zip!", 2f, EditorManager.NotificationType.Success);
            yield break;
        }

        /// <summary>
        /// Converts a level to VG format and outputs it to the exports folder.
        /// </summary>
        /// <param name="currentPath">Does not end with /level.lsb or a /.</param>
        /// <param name="fileName">The name of the folder.</param>
        public void ConvertLevel(string currentPath, string fileName)
        {
            var exportPath = EditorConfig.Instance.ConvertLevelLSToVGExportPath.Value;

            if (string.IsNullOrEmpty(exportPath))
            {
                var output = RTFile.CombinePaths(RTFile.ApplicationDirectory, DEFAULT_EXPORTS_PATH);
                RTFile.CreateDirectory(output);
                exportPath = output + "/";
            }

            exportPath = RTFile.AppendEndSlash(exportPath);

            if (!RTFile.DirectoryExists(Path.GetDirectoryName(exportPath)))
            {
                EditorManager.inst.DisplayNotification("Directory does not exist.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var gamedata = GameData.ReadFromFile(RTFile.CombinePaths(currentPath, Level.LEVEL_LSB), ArrhythmiaType.LS);
            var metadata = MetaData.ReadFromFile(RTFile.CombinePaths(currentPath, Level.METADATA_LSB), ArrhythmiaType.LS, false);

            var vgd = gamedata.ToJSONVG();

            var vgm = metadata.ToJSONVG();

            var path = exportPath + fileName;

            RTFile.CreateDirectory(path);

            if (RTFile.FileExists(RTFile.CombinePaths(currentPath, Level.LEVEL_OGG)))
                RTFile.CopyFile(RTFile.CombinePaths(currentPath, Level.LEVEL_OGG), RTFile.CombinePaths(path, Level.AUDIO_OGG));
            if (RTFile.FileExists(RTFile.CombinePaths(currentPath, Level.LEVEL_WAV)))
                RTFile.CopyFile(RTFile.CombinePaths(currentPath, Level.LEVEL_WAV), RTFile.CombinePaths(path, Level.AUDIO_WAV));
            if (RTFile.FileExists(RTFile.CombinePaths(currentPath, Level.LEVEL_MP3)))
                RTFile.CopyFile(RTFile.CombinePaths(currentPath, Level.LEVEL_MP3), RTFile.CombinePaths(path, Level.AUDIO_MP3));

            if (RTFile.FileExists(RTFile.CombinePaths(currentPath, Level.LEVEL_JPG)))
                RTFile.CopyFile(RTFile.CombinePaths(currentPath, Level.LEVEL_JPG), RTFile.CombinePaths(path, Level.COVER_JPG));

            try
            {
                RTFile.WriteToFile(RTFile.CombinePaths(path, Level.METADATA_VGM), vgm.ToString());
            }
            catch (Exception ex)
            {
                Debug.LogError($"{MetadataEditor.inst.className}Convert to VG error (MetaData) {ex}");
            }

            try
            {
                
                RTFile.WriteToFile(RTFile.CombinePaths(path, Level.LEVEL_VGD), vgd.ToString());
            }
            catch (Exception ex)
            {
                Debug.LogError($"{MetadataEditor.inst.className}Convert to VG error (GameData) {ex}");
            }

            EditorManager.inst.DisplayNotification($"Converted Level \"{fileName}\" from LS format to VG format and saved to {Path.GetFileName(path)}.", 4f,
                EditorManager.NotificationType.Success);

            AchievementManager.inst.UnlockAchievement("time_machine");
        }

        public static void SetNotificationProperties()
        {
            CoreHelper.Log($"Setting Notification values");
            var notifyRT = EditorManager.inst.notification.transform.AsRT();
            var notifyGroup = EditorManager.inst.notification.GetComponent<VerticalLayoutGroup>();
            notifyRT.sizeDelta = new Vector2(EditorConfig.Instance.NotificationWidth.Value, 632f);
            EditorManager.inst.notification.transform.localScale =
                new Vector3(EditorConfig.Instance.NotificationSize.Value, EditorConfig.Instance.NotificationSize.Value, 1f);

            var direction = EditorConfig.Instance.NotificationDirection.Value;

            notifyRT.anchoredPosition = new Vector2(8f, direction == VerticalDirection.Up ? 408f : 410f);
            notifyGroup.childAlignment = direction != VerticalDirection.Up ? TextAnchor.LowerLeft : TextAnchor.UpperLeft;
        }

        public static Color GetObjectColor(BeatmapObject beatmapObject, bool ignoreTransparency)
        {
            if (beatmapObject.objectType == BeatmapObject.ObjectType.Empty)
                return Color.white;

            if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.Renderer)
            {
                var color = AudioManager.inst.CurrentAudioSource.time < beatmapObject.StartTime ? CoreHelper.CurrentBeatmapTheme.GetObjColor((int)beatmapObject.events[3][0].eventValues[0])
                    : AudioManager.inst.CurrentAudioSource.time > beatmapObject.StartTime + beatmapObject.GetObjectLifeLength() && beatmapObject.autoKillType != AutoKillType.OldStyleNoAutokill
                    ? CoreHelper.CurrentBeatmapTheme.GetObjColor((int)beatmapObject.events[3][beatmapObject.events[3].Count - 1].eventValues[0])
                    : levelObject.visualObject.Renderer.material.HasProperty("_Color") ? levelObject.visualObject.Renderer.material.color : Color.white;

                if (ignoreTransparency)
                    color.a = 1f;

                return color;
            }

            return Color.white;
        }

        public static void DeleteLevelFunction(string level)
        {
            var recyclingPath = RTFile.CombinePaths(RTFile.ApplicationDirectory, "recycling");
            RTFile.CreateDirectory(recyclingPath);
            Directory.Move(RTFile.CombinePaths(RTFile.ApplicationDirectory, editorListSlash, level), RTFile.CombinePaths(recyclingPath, level));
        }

        public static float SnapToBPM(float time) => Mathf.RoundToInt((time + inst.bpmOffset) / (SettingEditor.inst.BPMMulti / EditorConfig.Instance.BPMSnapDivisions.Value)) * (SettingEditor.inst.BPMMulti / EditorConfig.Instance.BPMSnapDivisions.Value) - inst.bpmOffset;

        public static float SnapToBPM(float time, float offset, float divisions, float bpm) => Mathf.RoundToInt((time + offset) / (60f / bpm / divisions)) * (60f / bpm / divisions) - offset;

        public static void SetActive(GameObject gameObject, bool active)
        {
            gameObject.SetActive(active);
            gameObject.transform.parent.GetChild(gameObject.transform.GetSiblingIndex() - 1).gameObject.SetActive(active);
        }

        public void ConvertVGToLS()
        {
            EditorManager.inst.ShowDialog("Browser Popup");
            RTFileBrowser.inst.UpdateBrowserFile(RTFile.DotFormats(FileFormat.LSP, FileFormat.VGP, FileFormat.LST, FileFormat.VGT, FileFormat.LSB, FileFormat.VGD), onSelectFile: _val =>
            {
                bool failed = false;
                var selectedFile = _val.Replace("\\", "/");

                var fileFormat = RTFile.GetFileFormat(selectedFile);
                switch (fileFormat)
                {
                    case FileFormat.LSP:
                        {
                            var file = RTFile.CombinePaths(RTFile.ApplicationDirectory, prefabListSlash, Path.GetFileName(selectedFile));
                            if (RTFile.CopyFile(selectedFile, file))
                            {
                                EditorManager.inst.DisplayNotification($"Copied {Path.GetFileName(selectedFile)} to prefab ({prefabListPath}) folder.", 2f, EditorManager.NotificationType.Success);
                                StartCoroutine(UpdatePrefabPath());
                            }
                            else
                                EditorManager.inst.DisplayNotification($"Could not copy {Path.GetFileName(selectedFile)} as it already exists in the prefab ({prefabListPath}) folder.", 3f, EditorManager.NotificationType.Error);

                            break;
                        }
                    case FileFormat.VGP:
                        {
                            try
                            {
                                var file = RTFile.ReadFromFile(selectedFile);

                                var vgjn = JSON.Parse(file);

                                var prefab = Prefab.ParseVG(vgjn);

                                var jn = prefab.ToJSON();

                                RTFile.CreateDirectory(RTFile.CombinePaths(RTFile.ApplicationDirectory, prefabListPath));

                                string fileName = $"{RTFile.FormatLegacyFileName(prefab.Name)}{FileFormat.LSP.Dot()}";
                                RTFile.WriteToFile(RTFile.CombinePaths(RTFile.ApplicationDirectory, prefabListSlash, fileName), jn.ToString());

                                file = null;
                                vgjn = null;
                                prefab = null;
                                jn = null;

                                EditorManager.inst.DisplayNotification($"Successfully converted {Path.GetFileName(selectedFile)} to {fileName} and added it to your prefab ({prefabListPath}) folder.", 2f,
                                    EditorManager.NotificationType.Success);

                                AchievementManager.inst.UnlockAchievement("time_machine");
                                StartCoroutine(UpdatePrefabPath());
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError(ex);
                                failed = true;
                            }

                            break;
                        }
                    case FileFormat.LST:
                        {
                            var file = RTFile.CombinePaths(RTFile.ApplicationDirectory, themeListSlash, Path.GetFileName(selectedFile));
                            if (RTFile.CopyFile(selectedFile, file))
                            {
                                EditorManager.inst.DisplayNotification($"Copied {Path.GetFileName(selectedFile)} to theme ({themeListPath}) folder.", 2f, EditorManager.NotificationType.Success);
                                StartCoroutine(UpdateThemePath());
                            }
                            else
                                EditorManager.inst.DisplayNotification($"Could not copy {Path.GetFileName(selectedFile)} as it already exists in the theme ({themeListPath}) folder.", 3f, EditorManager.NotificationType.Error);

                            break;
                        }
                    case FileFormat.VGT:
                        {
                            try
                            {
                                var file = RTFile.ReadFromFile(selectedFile);

                                var vgjn = JSON.Parse(file);

                                var theme = BeatmapTheme.ParseVG(vgjn);

                                var jn = theme.ToJSON();

                                RTFile.CreateDirectory(RTFile.CombinePaths(RTFile.ApplicationDirectory, themeListPath));

                                var fileName = $"{RTFile.FormatLegacyFileName(theme.name)}{FileFormat.LST.Dot()}";
                                RTFile.WriteToFile(RTFile.CombinePaths(RTFile.ApplicationDirectory, themeListSlash, fileName), jn.ToString());

                                file = null;
                                vgjn = null;
                                theme = null;
                                jn = null;

                                EditorManager.inst.DisplayNotification($"Successfully converted {Path.GetFileName(selectedFile)} to {fileName} and added it to your theme ({themeListPath}) folder.", 2f,
                                    EditorManager.NotificationType.Success);

                                AchievementManager.inst.UnlockAchievement("time_machine");
                                StartCoroutine(UpdateThemePath());
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError(ex);
                                failed = true;
                            }

                            break;
                        }
                    case FileFormat.LSB:
                        {
                            if (Path.GetFileName(selectedFile) != "level.lsb")
                            {
                                EditorManager.inst.DisplayNotification("Cannot select non-level.", 2f, EditorManager.NotificationType.Warning);
                                failed = true;
                                break;
                            }

                            ShowWarningPopup("Warning! Selecting a level will copy all of its contents to your editor, are you sure you want to do this?", () =>
                            {
                                var path = selectedFile.Replace("/level.lsb", "");

                                var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);

                                bool copied = false;
                                foreach (var file in files)
                                {
                                    var copyTo = file.Replace("\\", "/").Replace(Path.GetDirectoryName(path), RTFile.CombinePaths(RTFile.ApplicationDirectory, editorListPath));
                                    if (RTFile.CopyFile(file, copyTo))
                                        copied = true;
                                }

                                if (copied)
                                {
                                    EditorManager.inst.DisplayNotification($"Copied {Path.GetFileName(path)} to level ({editorListPath}) folder.", 2f, EditorManager.NotificationType.Success);
                                    UpdateEditorPath(true);
                                }
                                else
                                    EditorManager.inst.DisplayNotification($"Could not copy {Path.GetFileName(path)}.", 3f, EditorManager.NotificationType.Error);

                                HideWarningPopup();
                            }, () =>
                            {
                                HideWarningPopup();
                                EditorManager.inst.ShowDialog("Browser Popup");
                            });

                            break;
                        }
                    case FileFormat.VGD:
                        {
                            if (selectedFile.Contains("/autosave_"))
                            {
                                EditorManager.inst.DisplayNotification("Cannot select autosave.", 2f, EditorManager.NotificationType.Warning);
                                failed = true;
                                break;
                            }

                            try
                            {
                                var path = selectedFile.Replace("/level.vgd", "");

                                if (Level.Verify(path))
                                {
                                    var copyTo = path.Replace(Path.GetDirectoryName(path).Replace("\\", "/"), RTFile.CombinePaths(RTFile.ApplicationDirectory, editorListSlash)) + " Convert";

                                    RTFile.CreateDirectory(copyTo);

                                    #region Audio

                                    bool copiedAudioFile = false;
                                    bool audioFileExists = false;
                                    if (RTFile.FileExists(RTFile.CombinePaths(path, Level.AUDIO_OGG)))
                                    {
                                        audioFileExists = true;
                                        copiedAudioFile = RTFile.CopyFile(RTFile.CombinePaths(path, Level.AUDIO_OGG), RTFile.CombinePaths(copyTo, Level.LEVEL_OGG));
                                    }
                                    if (RTFile.FileExists(RTFile.CombinePaths(path, Level.AUDIO_WAV)))
                                    {
                                        audioFileExists = true;
                                        copiedAudioFile = RTFile.CopyFile(RTFile.CombinePaths(path, Level.AUDIO_WAV), RTFile.CombinePaths(copyTo, Level.LEVEL_WAV));
                                    }
                                    if (RTFile.FileExists(RTFile.CombinePaths(path, Level.AUDIO_MP3)))
                                    {
                                        audioFileExists = true;
                                        copiedAudioFile = RTFile.CopyFile(RTFile.CombinePaths(path, Level.AUDIO_MP3), RTFile.CombinePaths(copyTo, Level.LEVEL_MP3));
                                    }

                                    if (RTFile.FileExists(RTFile.CombinePaths(path, Level.LEVEL_OGG)))
                                    {
                                        audioFileExists = true;
                                        copiedAudioFile = RTFile.CopyFile(RTFile.CombinePaths(path, Level.LEVEL_OGG), RTFile.CombinePaths(copyTo, Level.LEVEL_OGG));
                                    }
                                    if (RTFile.FileExists(RTFile.CombinePaths(path, Level.LEVEL_WAV)))
                                    {
                                        audioFileExists = true;
                                        copiedAudioFile = RTFile.CopyFile(RTFile.CombinePaths(path, Level.LEVEL_WAV), RTFile.CombinePaths(copyTo, Level.LEVEL_WAV));
                                    }
                                    if (RTFile.FileExists(RTFile.CombinePaths(path, Level.LEVEL_MP3)))
                                    {
                                        audioFileExists = true;
                                        copiedAudioFile = RTFile.CopyFile(RTFile.CombinePaths(path, Level.LEVEL_MP3), RTFile.CombinePaths(copyTo, Level.LEVEL_MP3));
                                    }

                                    if (!copiedAudioFile)
                                    {
                                        if (!audioFileExists)
                                            EditorManager.inst.DisplayNotification("No audio file exists.", 3f, EditorManager.NotificationType.Error);
                                        else
                                            EditorManager.inst.DisplayNotification("Cannot overwrite the same file. Please rename the VG level folder you want to convert.", 6f, EditorManager.NotificationType.Error);

                                        return;
                                    }

                                    #endregion

                                    if (RTFile.FileExists(RTFile.CombinePaths(path, Level.COVER_JPG)))
                                        RTFile.CopyFile(RTFile.CombinePaths(path, Level.COVER_JPG), RTFile.CombinePaths(copyTo, Level.LEVEL_JPG));

                                    #region Data

                                    var metadataVGJSON = RTFile.ReadFromFile(RTFile.CombinePaths(path, Level.METADATA_VGM));

                                    var metadataVGJN = JSON.Parse(metadataVGJSON);

                                    var metadata = MetaData.ParseVG(metadataVGJN);

                                    var metadataJN = metadata.ToJSON();

                                    RTFile.WriteToFile(RTFile.CombinePaths(copyTo, Level.METADATA_LSB), metadataJN.ToString());

                                    var levelVGJSON = RTFile.ReadFromFile(RTFile.CombinePaths(path, Level.LEVEL_VGD));

                                    var levelVGJN = JSON.Parse(levelVGJSON);

                                    var level = GameData.ParseVG(levelVGJN, false, metadata.Version);

                                    level.SaveData(RTFile.CombinePaths(copyTo, Level.LEVEL_LSB), () =>
                                    {
                                        EditorManager.inst.DisplayNotification($"Successfully converted {Path.GetFileName(path)} to {Path.GetFileName(copyTo)} and added it to your level ({editorListPath}) folder.", 2f,
                                            EditorManager.NotificationType.Success);

                                        metadataVGJSON = null;
                                        metadataVGJN = null;
                                        metadata = null;
                                        metadataJN = null;
                                        levelVGJSON = null;
                                        levelVGJN = null;
                                        level = null;

                                        AchievementManager.inst.UnlockAchievement("time_machine");
                                        UpdateEditorPath(true);
                                    }, true);

                                    #endregion
                                }
                                else
                                {
                                    EditorManager.inst.DisplayNotification("Could not convert since some needed files are missing!", 2f, EditorManager.NotificationType.Error);
                                    failed = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                EditorManager.inst.DisplayNotification($"There was an error in converting the VG level. Press {CoreConfig.Instance.OpenPAPersistentFolder.Value} to open the log folder and send the Player.log file to @rtmecha.", 5f, EditorManager.NotificationType.Error);
                                Debug.LogError(ex);
                                failed = true;
                            }

                            break;
                        }
                }

                if (!failed)
                    EditorManager.inst.HideDialog("Browser Popup");
            });
        }

        #endregion

        #region Constructors

        public List<DialogAnimation> DialogAnimations { get; set; } = new List<DialogAnimation>
        {
            new DialogAnimation("Open File Popup")
            {
                ActiveConfig = EditorConfig.Instance.OpenFilePopupActive,

                PosActiveConfig = EditorConfig.Instance.OpenFilePopupPosActive,
                PosOpenConfig = EditorConfig.Instance.OpenFilePopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.OpenFilePopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.OpenFilePopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.OpenFilePopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.OpenFilePopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.OpenFilePopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.OpenFilePopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.OpenFilePopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.OpenFilePopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.OpenFilePopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.OpenFilePopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.OpenFilePopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.OpenFilePopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.OpenFilePopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.OpenFilePopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.OpenFilePopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.OpenFilePopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.OpenFilePopupRotActive,
                RotOpenConfig = EditorConfig.Instance.OpenFilePopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.OpenFilePopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.OpenFilePopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.OpenFilePopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.OpenFilePopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.OpenFilePopupRotCloseEase,
            },
            new DialogAnimation("New File Popup")
            {
                ActiveConfig = EditorConfig.Instance.NewFilePopupActive,

                PosActiveConfig = EditorConfig.Instance.NewFilePopupPosActive,
                PosOpenConfig = EditorConfig.Instance.NewFilePopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.NewFilePopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.NewFilePopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.NewFilePopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.NewFilePopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.NewFilePopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.NewFilePopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.NewFilePopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.NewFilePopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.NewFilePopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.NewFilePopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.NewFilePopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.NewFilePopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.NewFilePopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.NewFilePopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.NewFilePopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.NewFilePopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.NewFilePopupRotActive,
                RotOpenConfig = EditorConfig.Instance.NewFilePopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.NewFilePopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.NewFilePopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.NewFilePopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.NewFilePopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.NewFilePopupRotCloseEase,
            },
            new DialogAnimation("Save As Popup")
            {
                ActiveConfig = EditorConfig.Instance.SaveAsPopupActive,

                PosActiveConfig = EditorConfig.Instance.SaveAsPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.SaveAsPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.SaveAsPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.SaveAsPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.SaveAsPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.SaveAsPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.SaveAsPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.SaveAsPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.SaveAsPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.SaveAsPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.SaveAsPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.SaveAsPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.SaveAsPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.SaveAsPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.SaveAsPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.SaveAsPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.SaveAsPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.SaveAsPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.SaveAsPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.SaveAsPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.SaveAsPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.SaveAsPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.SaveAsPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.SaveAsPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.SaveAsPopupRotCloseEase,
            },
            new DialogAnimation("Quick Actions Popup")
            {
                ActiveConfig = EditorConfig.Instance.NewFilePopupActive,

                PosActiveConfig = EditorConfig.Instance.NewFilePopupPosActive,
                PosOpenConfig = EditorConfig.Instance.NewFilePopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.NewFilePopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.NewFilePopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.NewFilePopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.NewFilePopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.NewFilePopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.NewFilePopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.NewFilePopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.NewFilePopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.NewFilePopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.NewFilePopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.NewFilePopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.NewFilePopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.NewFilePopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.NewFilePopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.NewFilePopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.NewFilePopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.NewFilePopupRotActive,
                RotOpenConfig = EditorConfig.Instance.NewFilePopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.NewFilePopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.NewFilePopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.NewFilePopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.NewFilePopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.NewFilePopupRotCloseEase,
            },
            new DialogAnimation("Parent Selector")
            {
                ActiveConfig = EditorConfig.Instance.ParentSelectorPopupActive,

                PosActiveConfig = EditorConfig.Instance.ParentSelectorPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.ParentSelectorPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.ParentSelectorPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.ParentSelectorPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.ParentSelectorPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.ParentSelectorPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.ParentSelectorPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.ParentSelectorPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.ParentSelectorPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.ParentSelectorPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.ParentSelectorPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.ParentSelectorPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.ParentSelectorPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.ParentSelectorPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.ParentSelectorPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.ParentSelectorPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.ParentSelectorPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.ParentSelectorPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.ParentSelectorPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.ParentSelectorPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.ParentSelectorPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.ParentSelectorPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.ParentSelectorPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.ParentSelectorPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.ParentSelectorPopupRotCloseEase,
            },
            new DialogAnimation("Prefab Popup")
            {
                ActiveConfig = EditorConfig.Instance.PrefabPopupActive,

                PosActiveConfig = EditorConfig.Instance.PrefabPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.PrefabPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.PrefabPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.PrefabPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.PrefabPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.PrefabPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.PrefabPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.PrefabPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.PrefabPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.PrefabPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.PrefabPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.PrefabPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.PrefabPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.PrefabPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.PrefabPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.PrefabPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.PrefabPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.PrefabPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.PrefabPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.PrefabPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.PrefabPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.PrefabPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.PrefabPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.PrefabPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.PrefabPopupRotCloseEase,
            },
            new DialogAnimation("Object Options Popup")
            {
                ActiveConfig = EditorConfig.Instance.NewFilePopupActive,

                PosActiveConfig = EditorConfig.Instance.NewFilePopupPosActive,
                PosOpenConfig = EditorConfig.Instance.NewFilePopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.NewFilePopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.NewFilePopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.NewFilePopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.NewFilePopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.NewFilePopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.NewFilePopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.NewFilePopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.NewFilePopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.NewFilePopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.NewFilePopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.NewFilePopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.NewFilePopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.NewFilePopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.NewFilePopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.NewFilePopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.NewFilePopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.NewFilePopupRotActive,
                RotOpenConfig = EditorConfig.Instance.NewFilePopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.NewFilePopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.NewFilePopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.NewFilePopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.NewFilePopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.NewFilePopupRotCloseEase,
            },
            new DialogAnimation("BG Options Popup")
            {
                ActiveConfig = EditorConfig.Instance.BGOptionsPopupActive,

                PosActiveConfig = EditorConfig.Instance.BGOptionsPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.BGOptionsPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.BGOptionsPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.BGOptionsPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.BGOptionsPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.BGOptionsPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.BGOptionsPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.BGOptionsPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.BGOptionsPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.BGOptionsPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.BGOptionsPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.BGOptionsPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.BGOptionsPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.BGOptionsPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.BGOptionsPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.BGOptionsPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.BGOptionsPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.BGOptionsPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.BGOptionsPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.BGOptionsPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.BGOptionsPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.BGOptionsPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.BGOptionsPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.BGOptionsPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.BGOptionsPopupRotCloseEase,
            },
            new DialogAnimation("Browser Popup")
            {
                ActiveConfig = EditorConfig.Instance.BrowserPopupActive,

                PosActiveConfig = EditorConfig.Instance.BrowserPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.BrowserPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.BrowserPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.BrowserPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.BrowserPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.BrowserPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.BrowserPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.BrowserPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.BrowserPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.BrowserPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.BrowserPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.BrowserPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.BrowserPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.BrowserPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.BrowserPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.BrowserPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.BrowserPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.BrowserPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.BrowserPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.BrowserPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.BrowserPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.BrowserPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.BrowserPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.BrowserPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.BrowserPopupRotCloseEase,
            },
            new DialogAnimation("Object Search Popup")
            {
                ActiveConfig = EditorConfig.Instance.ObjectSearchPopupActive,

                PosActiveConfig = EditorConfig.Instance.ObjectSearchPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.ObjectSearchPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.ObjectSearchPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.ObjectSearchPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.ObjectSearchPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.ObjectSearchPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.ObjectSearchPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.ObjectSearchPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.ObjectSearchPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.ObjectSearchPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.ObjectSearchPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.ObjectSearchPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.ObjectSearchPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.ObjectSearchPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.ObjectSearchPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.ObjectSearchPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.ObjectSearchPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.ObjectSearchPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.ObjectSearchPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.ObjectSearchPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.ObjectSearchPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.ObjectSearchPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.ObjectSearchPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.ObjectSearchPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.ObjectSearchPopupRotCloseEase,
            },
            new DialogAnimation("Warning Popup")
            {
                ActiveConfig = EditorConfig.Instance.WarningPopupActive,

                PosActiveConfig = EditorConfig.Instance.WarningPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.WarningPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.WarningPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.WarningPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.WarningPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.WarningPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.WarningPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.WarningPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.WarningPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.WarningPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.WarningPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.WarningPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.WarningPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.WarningPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.WarningPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.WarningPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.WarningPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.WarningPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.WarningPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.WarningPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.WarningPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.WarningPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.WarningPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.WarningPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.WarningPopupRotCloseEase,
            },
            new DialogAnimation("Folder Creator Popup")
            {
                ActiveConfig = EditorConfig.Instance.FilePopupActive,

                PosActiveConfig = EditorConfig.Instance.FilePopupPosActive,
                PosOpenConfig = EditorConfig.Instance.FilePopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.FilePopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.FilePopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.FilePopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.FilePopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.FilePopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.FilePopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.FilePopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.FilePopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.FilePopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.FilePopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.FilePopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.FilePopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.FilePopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.FilePopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.FilePopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.FilePopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.FilePopupRotActive,
                RotOpenConfig = EditorConfig.Instance.FilePopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.FilePopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.FilePopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.FilePopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.FilePopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.FilePopupRotCloseEase,
            },
            new DialogAnimation("Text Editor")
            {
                ActiveConfig = EditorConfig.Instance.TextEditorActive,

                PosActiveConfig = EditorConfig.Instance.TextEditorPosActive,
                PosOpenConfig = EditorConfig.Instance.TextEditorPosOpen,
                PosCloseConfig = EditorConfig.Instance.TextEditorPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.TextEditorPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.TextEditorPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.TextEditorPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.TextEditorPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.TextEditorPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.TextEditorPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.TextEditorScaActive,
                ScaOpenConfig = EditorConfig.Instance.TextEditorScaOpen,
                ScaCloseConfig = EditorConfig.Instance.TextEditorScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.TextEditorScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.TextEditorScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.TextEditorScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.TextEditorScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.TextEditorScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.TextEditorScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.TextEditorRotActive,
                RotOpenConfig = EditorConfig.Instance.TextEditorRotOpen,
                RotCloseConfig = EditorConfig.Instance.TextEditorRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.TextEditorRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.TextEditorRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.TextEditorRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.TextEditorRotCloseEase,
            },
            new DialogAnimation("Editor Properties Popup")
            {
                ActiveConfig = EditorConfig.Instance.EditorPropertiesPopupActive,

                PosActiveConfig = EditorConfig.Instance.EditorPropertiesPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.EditorPropertiesPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.EditorPropertiesPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.EditorPropertiesPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.EditorPropertiesPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.EditorPropertiesPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.EditorPropertiesPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.EditorPropertiesPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.EditorPropertiesPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.EditorPropertiesPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.EditorPropertiesPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.EditorPropertiesPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.EditorPropertiesPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.EditorPropertiesPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.EditorPropertiesPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.EditorPropertiesPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.EditorPropertiesPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.EditorPropertiesPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.EditorPropertiesPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.EditorPropertiesPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.EditorPropertiesPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.EditorPropertiesPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.EditorPropertiesPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.EditorPropertiesPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.EditorPropertiesPopupRotCloseEase,
            },
            new DialogAnimation("Documentation Popup")
            {
                ActiveConfig = EditorConfig.Instance.DocumentationPopupActive,

                PosActiveConfig = EditorConfig.Instance.DocumentationPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.DocumentationPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.DocumentationPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.DocumentationPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.DocumentationPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.DocumentationPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.DocumentationPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.DocumentationPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.DocumentationPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.DocumentationPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.DocumentationPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.DocumentationPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.DocumentationPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.DocumentationPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.DocumentationPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.DocumentationPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.DocumentationPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.DocumentationPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.DocumentationPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.DocumentationPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.DocumentationPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.DocumentationPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.DocumentationPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.DocumentationPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.DocumentationPopupRotCloseEase,
            },
            new DialogAnimation("Debugger Popup")
            {
                ActiveConfig = EditorConfig.Instance.DebuggerPopupActive,

                PosActiveConfig = EditorConfig.Instance.DebuggerPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.DebuggerPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.DebuggerPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.DebuggerPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.DebuggerPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.DebuggerPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.DebuggerPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.DebuggerPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.DebuggerPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.DebuggerPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.DebuggerPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.DebuggerPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.DebuggerPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.DebuggerPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.DebuggerPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.DebuggerPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.DebuggerPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.DebuggerPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.DebuggerPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.DebuggerPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.DebuggerPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.DebuggerPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.DebuggerPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.DebuggerPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.DebuggerPopupRotCloseEase,
            },
            new DialogAnimation("Autosaves Popup")
            {
                ActiveConfig = EditorConfig.Instance.AutosavesPopupActive,

                PosActiveConfig = EditorConfig.Instance.AutosavesPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.AutosavesPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.AutosavesPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.AutosavesPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.AutosavesPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.AutosavesPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.AutosavesPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.AutosavesPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.AutosavesPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.AutosavesPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.AutosavesPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.AutosavesPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.AutosavesPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.AutosavesPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.AutosavesPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.AutosavesPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.AutosavesPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.AutosavesPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.AutosavesPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.AutosavesPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.AutosavesPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.AutosavesPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.AutosavesPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.AutosavesPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.AutosavesPopupRotCloseEase,
            },
            new DialogAnimation("Default Modifiers Popup")
            {
                ActiveConfig = EditorConfig.Instance.DefaultModifiersPopupActive,

                PosActiveConfig = EditorConfig.Instance.DefaultModifiersPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.DefaultModifiersPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.DefaultModifiersPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.DefaultModifiersPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.DefaultModifiersPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.DefaultModifiersPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.DefaultModifiersPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.DefaultModifiersPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.DefaultModifiersPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.DefaultModifiersPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.DefaultModifiersPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.DefaultModifiersPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.DefaultModifiersPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.DefaultModifiersPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.DefaultModifiersPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.DefaultModifiersPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.DefaultModifiersPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.DefaultModifiersPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.DefaultModifiersPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.DefaultModifiersPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.DefaultModifiersPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.DefaultModifiersPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.DefaultModifiersPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.DefaultModifiersPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.DefaultModifiersPopupRotCloseEase,
            },
            new DialogAnimation("Keybind List Popup")
            {
                ActiveConfig = EditorConfig.Instance.KeybindListPopupActive,

                PosActiveConfig = EditorConfig.Instance.KeybindListPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.KeybindListPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.KeybindListPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.KeybindListPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.KeybindListPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.KeybindListPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.KeybindListPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.KeybindListPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.KeybindListPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.KeybindListPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.KeybindListPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.KeybindListPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.KeybindListPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.KeybindListPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.KeybindListPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.KeybindListPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.KeybindListPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.KeybindListPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.KeybindListPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.KeybindListPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.KeybindListPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.KeybindListPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.KeybindListPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.KeybindListPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.KeybindListPopupRotCloseEase,
            },
            new DialogAnimation("Theme Popup")
            {
                ActiveConfig = EditorConfig.Instance.ThemePopupActive,

                PosActiveConfig = EditorConfig.Instance.ThemePopupPosActive,
                PosOpenConfig = EditorConfig.Instance.ThemePopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.ThemePopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.ThemePopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.ThemePopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.ThemePopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.ThemePopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.ThemePopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.ThemePopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.ThemePopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.ThemePopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.ThemePopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.ThemePopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.ThemePopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.ThemePopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.ThemePopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.ThemePopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.ThemePopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.ThemePopupRotActive,
                RotOpenConfig = EditorConfig.Instance.ThemePopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.ThemePopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.ThemePopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.ThemePopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.ThemePopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.ThemePopupRotCloseEase,
            },
            new DialogAnimation("Prefab Types Popup")
            {
                ActiveConfig = EditorConfig.Instance.PrefabTypesPopupActive,

                PosActiveConfig = EditorConfig.Instance.PrefabTypesPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.PrefabTypesPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.PrefabTypesPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.PrefabTypesPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.PrefabTypesPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.PrefabTypesPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.PrefabTypesPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.PrefabTypesPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.PrefabTypesPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.PrefabTypesPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.PrefabTypesPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.PrefabTypesPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.PrefabTypesPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.PrefabTypesPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.PrefabTypesPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.PrefabTypesPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.PrefabTypesPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.PrefabTypesPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.PrefabTypesPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.PrefabTypesPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.PrefabTypesPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.PrefabTypesPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.PrefabTypesPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.PrefabTypesPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.PrefabTypesPopupRotCloseEase,
            },
            new DialogAnimation("File Dropdown")
            {
                ActiveConfig = EditorConfig.Instance.FileDropdownActive,

                PosActiveConfig = EditorConfig.Instance.FileDropdownPosActive,
                PosOpenConfig = EditorConfig.Instance.FileDropdownPosOpen,
                PosCloseConfig = EditorConfig.Instance.FileDropdownPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.FileDropdownPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.FileDropdownPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.FileDropdownPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.FileDropdownPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.FileDropdownPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.FileDropdownPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.FileDropdownScaActive,
                ScaOpenConfig = EditorConfig.Instance.FileDropdownScaOpen,
                ScaCloseConfig = EditorConfig.Instance.FileDropdownScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.FileDropdownScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.FileDropdownScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.FileDropdownScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.FileDropdownScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.FileDropdownScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.FileDropdownScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.FileDropdownRotActive,
                RotOpenConfig = EditorConfig.Instance.FileDropdownRotOpen,
                RotCloseConfig = EditorConfig.Instance.FileDropdownRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.FileDropdownRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.FileDropdownRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.FileDropdownRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.FileDropdownRotCloseEase,
            },
            new DialogAnimation("Edit Dropdown")
            {
                ActiveConfig = EditorConfig.Instance.EditDropdownActive,

                PosActiveConfig = EditorConfig.Instance.EditDropdownPosActive,
                PosOpenConfig = EditorConfig.Instance.EditDropdownPosOpen,
                PosCloseConfig = EditorConfig.Instance.EditDropdownPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.EditDropdownPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.EditDropdownPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.EditDropdownPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.EditDropdownPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.EditDropdownPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.EditDropdownPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.EditDropdownScaActive,
                ScaOpenConfig = EditorConfig.Instance.EditDropdownScaOpen,
                ScaCloseConfig = EditorConfig.Instance.EditDropdownScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.EditDropdownScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.EditDropdownScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.EditDropdownScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.EditDropdownScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.EditDropdownScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.EditDropdownScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.EditDropdownRotActive,
                RotOpenConfig = EditorConfig.Instance.EditDropdownRotOpen,
                RotCloseConfig = EditorConfig.Instance.EditDropdownRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.EditDropdownRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.EditDropdownRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.EditDropdownRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.EditDropdownRotCloseEase,
            },
            new DialogAnimation("View Dropdown")
            {
                ActiveConfig = EditorConfig.Instance.ViewDropdownActive,

                PosActiveConfig = EditorConfig.Instance.ViewDropdownPosActive,
                PosOpenConfig = EditorConfig.Instance.ViewDropdownPosOpen,
                PosCloseConfig = EditorConfig.Instance.ViewDropdownPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.ViewDropdownPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.ViewDropdownPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.ViewDropdownPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.ViewDropdownPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.ViewDropdownPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.ViewDropdownPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.ViewDropdownScaActive,
                ScaOpenConfig = EditorConfig.Instance.ViewDropdownScaOpen,
                ScaCloseConfig = EditorConfig.Instance.ViewDropdownScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.ViewDropdownScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.ViewDropdownScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.ViewDropdownScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.ViewDropdownScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.ViewDropdownScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.ViewDropdownScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.ViewDropdownRotActive,
                RotOpenConfig = EditorConfig.Instance.ViewDropdownRotOpen,
                RotCloseConfig = EditorConfig.Instance.ViewDropdownRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.ViewDropdownRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.ViewDropdownRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.ViewDropdownRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.ViewDropdownRotCloseEase,
            },
            new DialogAnimation("Steam Dropdown")
            {
                ActiveConfig = EditorConfig.Instance.SteamDropdownActive,

                PosActiveConfig = EditorConfig.Instance.SteamDropdownPosActive,
                PosOpenConfig = EditorConfig.Instance.SteamDropdownPosOpen,
                PosCloseConfig = EditorConfig.Instance.SteamDropdownPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.SteamDropdownPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.SteamDropdownPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.SteamDropdownPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.SteamDropdownPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.SteamDropdownPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.SteamDropdownPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.SteamDropdownScaActive,
                ScaOpenConfig = EditorConfig.Instance.SteamDropdownScaOpen,
                ScaCloseConfig = EditorConfig.Instance.SteamDropdownScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.SteamDropdownScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.SteamDropdownScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.SteamDropdownScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.SteamDropdownScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.SteamDropdownScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.SteamDropdownScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.SteamDropdownRotActive,
                RotOpenConfig = EditorConfig.Instance.SteamDropdownRotOpen,
                RotCloseConfig = EditorConfig.Instance.SteamDropdownRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.SteamDropdownRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.SteamDropdownRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.SteamDropdownRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.SteamDropdownRotCloseEase,
            },
            new DialogAnimation("Help Dropdown")
            {
                ActiveConfig = EditorConfig.Instance.HelpDropdownActive,

                PosActiveConfig = EditorConfig.Instance.HelpDropdownPosActive,
                PosOpenConfig = EditorConfig.Instance.HelpDropdownPosOpen,
                PosCloseConfig = EditorConfig.Instance.HelpDropdownPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.HelpDropdownPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.HelpDropdownPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.HelpDropdownPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.HelpDropdownPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.HelpDropdownPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.HelpDropdownPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.HelpDropdownScaActive,
                ScaOpenConfig = EditorConfig.Instance.HelpDropdownScaOpen,
                ScaCloseConfig = EditorConfig.Instance.HelpDropdownScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.HelpDropdownScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.HelpDropdownScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.HelpDropdownScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.HelpDropdownScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.HelpDropdownScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.HelpDropdownScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.HelpDropdownRotActive,
                RotOpenConfig = EditorConfig.Instance.HelpDropdownRotOpen,
                RotCloseConfig = EditorConfig.Instance.HelpDropdownRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.HelpDropdownRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.HelpDropdownRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.HelpDropdownRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.HelpDropdownRotCloseEase,
            },
        };

        public class Popup
        {
            public string Name { get; set; }
            public GameObject GameObject { get; set; }
            public Button Close { get; set; }
            public InputField SearchField { get; set; }
            public Transform Content { get; set; }
            public GridLayoutGroup Grid { get; set; }
            public RectTransform TopPanel { get; set; }
        }

        class MultiColorButton
        {
            public Button Button { get; set; }
            public Image Image { get; set; }
            public GameObject Selected { get; set; }
        }

        public class ButtonFunction
        {
            public ButtonFunction(bool isSpacer, float spacerSize = 4f)
            {
                IsSpacer = isSpacer;
                SpacerSize = spacerSize;
            }

            public ButtonFunction(string name, Action action)
            {
                Name = name;
                Action = action;
            }
            
            public ButtonFunction(string name, Action<PointerEventData> onClick)
            {
                Name = name;
                OnClick = onClick;
            }

            public bool IsSpacer { get; set; }
            public float SpacerSize { get; set; } = 4f;
            public string Name { get; set; }
            public int FontSize { get; set; } = 20;
            public Action Action { get; set; }
            public Action<PointerEventData> OnClick { get; set; }
        }

        public class DialogAnimation : Exists
        {
            public DialogAnimation(string name)
            {
                this.name = name;
            }

            public string name;

            #region Configs

            public Setting<bool> ActiveConfig { get; set; }

            // Position
            public Setting<bool> PosActiveConfig { get; set; }
            public Setting<Vector2> PosOpenConfig { get; set; }
            public Setting<Vector2> PosCloseConfig { get; set; }
            public Setting<Vector2> PosOpenDurationConfig { get; set; }
            public Setting<Vector2> PosCloseDurationConfig { get; set; }
            public Setting<Easings> PosXOpenEaseConfig { get; set; }
            public Setting<Easings> PosXCloseEaseConfig { get; set; }
            public Setting<Easings> PosYOpenEaseConfig { get; set; }
            public Setting<Easings> PosYCloseEaseConfig { get; set; }

            // Scale
            public Setting<bool> ScaActiveConfig { get; set; }
            public Setting<Vector2> ScaOpenConfig { get; set; }
            public Setting<Vector2> ScaCloseConfig { get; set; }
            public Setting<Vector2> ScaOpenDurationConfig { get; set; }
            public Setting<Vector2> ScaCloseDurationConfig { get; set; }
            public Setting<Easings> ScaXOpenEaseConfig { get; set; }
            public Setting<Easings> ScaXCloseEaseConfig { get; set; }
            public Setting<Easings> ScaYOpenEaseConfig { get; set; }
            public Setting<Easings> ScaYCloseEaseConfig { get; set; }

            // Rotation
            public Setting<bool> RotActiveConfig { get; set; }
            public Setting<float> RotOpenConfig { get; set; }
            public Setting<float> RotCloseConfig { get; set; }
            public Setting<float> RotOpenDurationConfig { get; set; }
            public Setting<float> RotCloseDurationConfig { get; set; }
            public Setting<Easings> RotOpenEaseConfig { get; set; }
            public Setting<Easings> RotCloseEaseConfig { get; set; }

            #endregion

            public bool Active => ActiveConfig.Value;

            public bool PosActive => PosActiveConfig.Value;
            public Vector2 PosStart => PosCloseConfig.Value;
            public Vector2 PosEnd => PosOpenConfig.Value;
            public float PosXStartDuration => PosOpenDurationConfig.Value.x;
            public float PosXEndDuration => PosCloseDurationConfig.Value.x;
            public string PosXStartEase => PosXOpenEaseConfig.Value.ToString();
            public string PosXEndEase => PosXCloseEaseConfig.Value.ToString();
            public float PosYStartDuration => PosOpenDurationConfig.Value.y;
            public float PosYEndDuration => PosCloseDurationConfig.Value.y;
            public string PosYStartEase => PosYOpenEaseConfig.Value.ToString();
            public string PosYEndEase => PosYCloseEaseConfig.Value.ToString();

            public bool ScaActive => ScaActiveConfig.Value;
            public Vector2 ScaStart => ScaCloseConfig.Value;
            public Vector2 ScaEnd => ScaOpenConfig.Value;
            public float ScaXStartDuration => ScaOpenDurationConfig.Value.x;
            public float ScaXEndDuration => ScaCloseDurationConfig.Value.x;
            public string ScaXStartEase => ScaXOpenEaseConfig.Value.ToString();
            public string ScaXEndEase => ScaXCloseEaseConfig.Value.ToString();
            public float ScaYStartDuration => ScaOpenDurationConfig.Value.y;
            public float ScaYEndDuration => ScaCloseDurationConfig.Value.y;
            public string ScaYStartEase => ScaYOpenEaseConfig.Value.ToString();
            public string ScaYEndEase => ScaYCloseEaseConfig.Value.ToString();

            public bool RotActive => RotActiveConfig.Value;
            public float RotStart => RotCloseConfig.Value;
            public float RotEnd => RotOpenConfig.Value;
            public float RotStartDuration => RotOpenDurationConfig.Value;
            public float RotEndDuration => RotCloseDurationConfig.Value;
            public string RotStartEase => RotOpenEaseConfig.Value.ToString();
            public string RotEndEase => RotCloseEaseConfig.Value.ToString();
        }

        #endregion
    }
}
