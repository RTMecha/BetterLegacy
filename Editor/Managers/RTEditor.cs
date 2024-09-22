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

            CreateContextMenu();
            CreateFolderCreator();
        }

        void Update()
        {
            timeEditing = Time.time - timeOffset + savedTimeEditng;

            UpdateTimelineObjectColors();

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
                var key = KeybindManager.WatchKeyCode();

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

        public Transform timelinePreview;
        public RectTransform timelinePosition;

        #endregion

        #region Documentation

        public List<Document> documentations = new List<Document>();

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

            RTFile.WriteToFile(GameManager.inst.basePath + "editor.lse", jn.ToString(3));
        }

        public void LoadSettings()
        {
            if (!RTFile.FileExists(GameManager.inst.basePath + "editor.lse"))
            {
                savedTimeEditng = 0f;
                timeOffset = Time.time;
                return;
            }

            var jn = JSON.Parse(RTFile.ReadFromFile(GameManager.inst.basePath + "editor.lse"));

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

                SettingEditor.inst.SnapBPM = DataManager.inst.metaData.song.BPM;
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

        public List<TimelineObject> TimelineBeatmapObjects => timelineObjects.Where(x => x.IsBeatmapObject).ToList();
        public List<TimelineObject> TimelinePrefabObjects => timelineObjects.Where(x => x.IsPrefabObject).ToList();

        public void RemoveTimelineObject(TimelineObject timelineObject)
        {
            if (timelineObjects.TryFindIndex(x => x.ID == timelineObject.ID, out int a))
            {
                timelineObject.selected = false;
                Destroy(timelineObject.GameObject);
                timelineObjects.RemoveAt(a);
            }
        }

        public static Sprite GetKeyframeIcon(DataManager.LSAnimation a, DataManager.LSAnimation b)
            => ObjEditor.inst.KeyframeSprites[a.Name.Contains("Out") && b.Name.Contains("In") ? 3 : a.Name.Contains("Out") ? 2 : b.Name.Contains("In") ? 1 : 0];

        void UpdateTimelineObjectColors()
        {
            for (int i = 0; i < timelineObjects.Count; i++)
            {
                var timelineObject = timelineObjects[i];

                if (timelineObject.Data == null || !timelineObject.GameObject || !timelineObject.Image)
                    continue;

                bool isCurrentLayer = timelineObject.Layer == Layer && layerType == LayerType.Objects;
                timelineObject.GameObject.SetActive(isCurrentLayer);

                if (!isCurrentLayer)
                    continue;

                timelineObject.Image.color = timelineObject.selected ? ObjEditor.inst.SelectedColor :
                    timelineObject.IsBeatmapObject && !string.IsNullOrEmpty(timelineObject.GetData<BeatmapObject>().prefabID) ? timelineObject.GetData<BeatmapObject>().Prefab.PrefabType.Color :
                    timelineObject.IsPrefabObject ? timelineObject.GetData<PrefabObject>().Prefab.PrefabType.Color : ObjEditor.inst.NormalColor;
            }

            var theme = EditorThemeManager.CurrentTheme;
            var objectKeyframesRenderBinColor = EditorConfig.Instance.EventKeyframesRenderBinColor.Value;
            if (ObjectEditor.inst && ObjectEditor.inst.CurrentSelection && ObjectEditor.inst.CurrentSelection.IsBeatmapObject && ObjectEditor.inst.CurrentSelection.InternalSelections.Count > 0)
                foreach (var timelineObject in ObjectEditor.inst.CurrentSelection.InternalSelections)
                {
                    if (timelineObject.Data == null || !timelineObject.GameObject || !timelineObject.Image)
                        continue;

                    timelineObject.GameObject.SetActive(true);

                    var color = objectKeyframesRenderBinColor &&
                        theme.ContainsGroup($"Object Keyframe Color {timelineObject.Type + 1}") ?
                        theme.GetColor($"Object Keyframe Color {timelineObject.Type + 1}") : ObjEditor.inst.NormalColor;
                    color.a = 1f;

                    timelineObject.Image.color = timelineObject.selected ? !objectKeyframesRenderBinColor ? ObjEditor.inst.SelectedColor : EventEditor.inst.Selected : color;
                }

            var eventKeyframesRenderBinColor = EditorConfig.Instance.EventKeyframesRenderBinColor.Value;
            for (int i = 0; i < timelineKeyframes.Count; i++)
            {
                var timelineObject = timelineKeyframes[i];

                if (timelineObject.Data == null || !timelineObject.GameObject || !timelineObject.Image)
                    continue;

                int limit = timelineObject.Type / RTEventEditor.EventLimit;
                bool isCurrentLayer = limit == Layer && layerType == LayerType.Events;
                bool active = isCurrentLayer && (ShowModdedUI || timelineObject.Type < 10);

                timelineObject.GameObject.SetActive(active);

                if (!active)
                    continue;

                var color = eventKeyframesRenderBinColor &&
                    theme.ContainsGroup($"Event Color {timelineObject.Type % RTEventEditor.EventLimit + 1} Keyframe") ?
                    theme.GetColor($"Event Color {timelineObject.Type % RTEventEditor.EventLimit + 1} Keyframe") : ObjEditor.inst.NormalColor;
                color.a = 1f;

                timelineObject.Image.color = timelineObject.selected ? !eventKeyframesRenderBinColor ? ObjEditor.inst.SelectedColor : EventEditor.inst.Selected : color;
            }
        }

        #endregion

        #region Timeline Textures

        public IEnumerator AssignTimelineTexture()
        {
            var config = EditorConfig.Instance;
            var path = $"{GameManager.inst.basePath}waveform-{config.WaveformMode.Value.ToString().ToLower()}.png";
            var settingsPath = $"{RTFile.ApplicationDirectory}settings/waveform-{config.WaveformMode.Value.ToString().ToLower()}.png";

            SetTimelineSprite(null);

            if ((!EditorManager.inst.hasLoadedLevel && !EditorManager.inst.loading && !RTFile.FileExists(settingsPath) ||
                !RTFile.FileExists(path)) && !config.WaveformRerender.Value || config.WaveformRerender.Value)
            {
                int num = Mathf.Clamp((int)AudioManager.inst.CurrentAudioSource.clip.length * 48, 100, 15000);
                Texture2D waveform = null;

                if (config.WaveformMode.Value == WaveformType.Legacy)
                    yield return CoreHelper.StartCoroutineAsync(Legacy(AudioManager.inst.CurrentAudioSource.clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, config.WaveformBottomColor.Value, (Texture2D _tex) => { waveform = _tex; }));
                if (config.WaveformMode.Value == WaveformType.Beta)
                    yield return CoreHelper.StartCoroutineAsync(Beta(AudioManager.inst.CurrentAudioSource.clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, (Texture2D _tex) => { waveform = _tex; }));
                if (config.WaveformMode.Value == WaveformType.BetaFast)
                    yield return CoreHelper.StartCoroutineAsync(BetaFast(AudioManager.inst.CurrentAudioSource.clip, 1f, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, (Texture2D _tex) => { waveform = _tex; }));
                if (config.WaveformMode.Value == WaveformType.LegacyFast)
                    yield return CoreHelper.StartCoroutineAsync(LegacyFast(AudioManager.inst.CurrentAudioSource.clip, 1f, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, config.WaveformBottomColor.Value, (Texture2D _tex) => { waveform = _tex; }));

                var waveSprite = Sprite.Create(waveform, new Rect(0f, 0f, (float)num, 300f), new Vector2(0.5f, 0.5f), 100f);
                SetTimelineSprite(waveSprite);

                if (config.WaveformSaves.Value)
                    CoreHelper.StartCoroutineAsync(SaveWaveform(config));
            }
            else
            {
                CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadImageTexture("file://" + (!EditorManager.inst.hasLoadedLevel && !EditorManager.inst.loading ?
                settingsPath :
                path), texture2D => { SetTimelineSprite(SpriteHelper.CreateSprite(texture2D)); }));
            }

            SetTimelineGridSize();

            yield break;
        }

        public IEnumerator SaveWaveform(EditorConfig config)
        {
            var path = !EditorManager.inst.hasLoadedLevel && !EditorManager.inst.loading ?
                    $"{RTFile.ApplicationDirectory}settings/waveform-{config.WaveformMode.Value.ToString().ToLower()}.png" :
                    GameManager.inst.basePath + $"waveform-{config.WaveformMode.Value.ToString().ToLower()}.png";
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
                array3[j] /= (float)num;
            }
            for (int l = 0; l < array3.Length - 1; l++)
            {
                int num2 = 0;
                while ((float)num2 < (float)textureHeight * array3[l] + 1f)
                {
                    texture2D.SetPixel(textureWidth * l / array3.Length, (int)((float)textureHeight * (array3[l] + 1f) / 2f) - num2, waveform);
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
                array6[j] /= (float)num;
                array6[j] *= 0.85f;
            }
            for (int l = 0; l < array6.Length - 1; l++)
            {
                int num2 = 0;
                while ((float)num2 < (float)textureHeight * array6[l])
                {
                    texture2D.SetPixel(textureWidth * l / array6.Length, (int)((float)textureHeight * array6[l]) - num2, _top);
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
                array6[m] /= (float)num;
                array6[m] *= 0.85f;
            }
            for (int num3 = 0; num3 < array6.Length - 1; num3++)
            {
                int num4 = 0;
                while ((float)num4 < (float)textureHeight * array6[num3])
                {
                    int x = textureWidth * num3 / array6.Length;
                    int y = (int)array4[num3 * num + num4] - num4;
                    texture2D.SetPixel(x, y, texture2D.GetPixel(x, y) == _top ? MixColors(new List<Color> { _top, _bottom }) : _bottom);
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

        public IEnumerator BetaFast(AudioClip audio, float saturation, int width, int height, Color background, Color col, Action<Texture2D> action)
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

        public IEnumerator LegacyFast(AudioClip audio, float saturation, int width, int height, Color background, Color colTop, Color colBot, Action<Texture2D> action)
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

                    tex.SetPixel(x, y, tex.GetPixel(x, y) == colTop ? MixColors(new List<Color> { colTop, colBot }) : colBot);
                }
            }
            yield return Ninja.JumpToUnity;
            tex.Apply();

            action?.Invoke(tex);
            yield break;
        }

        public static Color MixColors(List<Color> colors)
        {
            var invertedColorSum = Color.black;
            foreach (var color in colors)
                invertedColorSum += Color.white - color;

            return Color.white - invertedColorSum / colors.Count;
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

            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + editorListPath))
            {
                editorPathField.interactable = false;
                ShowWarningPopup("No directory exists for this path. Do you want to create a new folder?", () =>
                {
                    Directory.CreateDirectory(RTFile.ApplicationDirectory + editorListPath);

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

        public void UpdateThemePath(bool forceReload)
        {
            if (!forceReload && !EditorConfig.Instance.SettingPathReloads.Value || themeListPath[themeListPath.Length - 1] == '/')
                return;

            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + themeListPath))
            {
                themePathField.interactable = false;
                ShowWarningPopup("No directory exists for this path. Do you want to create a new folder?", () =>
                {
                    Directory.CreateDirectory(RTFile.ApplicationDirectory + themeListPath);

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

            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + prefabListPath))
            {
                prefabPathField.interactable = false;
                ShowWarningPopup("No directory exists for this path. Do you want to create a new folder?", () =>
                {
                    Directory.CreateDirectory(RTFile.ApplicationDirectory + prefabListPath);

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

            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + editorListPath))
                Directory.CreateDirectory(RTFile.ApplicationDirectory + editorListPath);
            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + themeListPath))
                Directory.CreateDirectory(RTFile.ApplicationDirectory + themeListPath);
            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + prefabListPath))
                Directory.CreateDirectory(RTFile.ApplicationDirectory + prefabListPath);

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
            {
                jn["marker_colors"][i] = LSColors.ColorToHex(MarkerEditor.inst.markerColors[i]);
            }

            for (int i = 0; i < EditorManager.inst.layerColors.Count; i++)
            {
                jn["layer_colors"][i] = LSColors.ColorToHex(EditorManager.inst.layerColors[i]);
            }

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
                if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
                    if (ObjEditor.inst.currentKeyframe != 0)
                    {
                        var list = new List<TimelineObject>();
                        foreach (var timelineObject in ObjectEditor.inst.CurrentSelection.InternalSelections.Where(x => x.selected))
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
                        list.Where(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()).ToList(),
                        list.Where(x => x.IsPrefabObject).Select(x => x.GetData<PrefabObject>()).ToList());

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
                switch (layerType)
                {
                    case LayerType.Objects:
                        {
                            ObjectEditor.inst.RenderTimelineObjectsPositions();

                            if (prevLayerType != layerType)
                            {
                                if (CheckpointEditor.inst.checkpoints.Count > 0)
                                {
                                    foreach (var obj2 in CheckpointEditor.inst.checkpoints)
                                        Destroy(obj2);

                                    CheckpointEditor.inst.checkpoints.Clear();
                                }

                                CheckpointEditor.inst.CreateGhostCheckpoints();
                            }

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
            popupInstance.SearchField.PlaceholderText().text = placeholderText;
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
            timeField.PlaceholderText().text = "Set time...";
            timeField.PlaceholderText().alignment = TextAnchor.MiddleCenter;
            timeField.PlaceholderText().fontSize = 16;
            timeField.PlaceholderText().horizontalOverflow = HorizontalWrapMode.Overflow;
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
            editorLayerField.PlaceholderText().text = "Set layer...";
            editorLayerField.PlaceholderText().alignment = TextAnchor.MiddleCenter;
            editorLayerField.PlaceholderText().fontSize = 16;
            editorLayerField.PlaceholderText().horizontalOverflow = HorizontalWrapMode.Overflow;
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
            pitchField.PlaceholderText().text = "Pitch";
            pitchField.PlaceholderText().alignment = TextAnchor.MiddleCenter;
            pitchField.PlaceholderText().fontSize = 16;
            pitchField.PlaceholderText().horizontalOverflow = HorizontalWrapMode.Overflow;
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
        }

        void SetupTimelineTriggers()
        {
            var tltrig = EditorManager.inst.timeline.GetComponent<EventTrigger>();

            tltrig.triggers.Add(TriggerHelper.CreateEntry(EventTriggerType.PointerEnter, eventData => { isOverMainTimeline = true; }));
            tltrig.triggers.Add(TriggerHelper.CreateEntry(EventTriggerType.PointerExit, eventData => { isOverMainTimeline = false; }));
            tltrig.triggers.Add(TriggerHelper.StartDragTrigger());
            tltrig.triggers.Add(TriggerHelper.DragTrigger());
            tltrig.triggers.Add(TriggerHelper.EndDragTrigger());

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
                    int max = RTEventEditor.EventLimit * layer;
                    int min = max - RTEventEditor.EventLimit;
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
        }

        void SetupDropdowns()
        {
            titleBar = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar").transform;

            // Here we fix the naming issues with unmodded Legacy.
            var saveAs = EditorManager.inst.GetDialog("Save As Popup").Dialog.Find("New File Popup");
            EditorManager.inst.GetDialog("Save As Popup").Dialog.Find("New File Popup/level-name").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
            GameObject.Find("Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/left/theme/name").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
            GameObject.Find("Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/name/input").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;

            EditorHelper.AddEditorDropdown("Quit to Arcade", "", "File", titleBar.Find("File/File Dropdown/Quit to Main Menu/Image").GetComponent<Image>().sprite, () =>
            {
                ShowWarningPopup("Are you sure you want to quit to the arcade? Any unsaved progress will be lost!", ArcadeHelper.QuitToArcade, HideWarningPopup);
            }, 7);

            EditorHelper.AddEditorDropdown("Copy Level to Arcade", "", "File", SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_right_small.png"), () =>
            {
                if (!EditorManager.inst.hasLoadedLevel)
                {
                    EditorManager.inst.DisplayNotification("Load a level before trying to copy a level to the arcade folder!", 2f, EditorManager.NotificationType.Error);
                    return;
                }

                ShowWarningPopup("Are you sure you want to copy the level to the arcade folder?", () =>
                {
                    var name = MetaData.Current.LevelBeatmap.name;
                    name = CoreHelper.ReplaceFormatting(name); // for cases where a user has used symbols not allowed.
                    name = RTFile.ValidateDirectory(name);
                    var directory = $"{RTFile.ApplicationDirectory}{LevelManager.ListSlash}{name} [{MetaData.Current.arcadeID}]";

                    if (RTFile.DirectoryExists(directory))
                    {
                        var backupDirectory = directory.Replace("beatmaps", "beatmaps/arcade backups");
                        if (RTFile.DirectoryExists(backupDirectory))
                            Directory.Delete(backupDirectory, true);

                        //Directory.CreateDirectory(backupDirectory);
                        Directory.Move(directory, backupDirectory);
                    }

                    var files = Directory.GetFiles(GameManager.inst.basePath, "*", SearchOption.AllDirectories);
                    for (int i = 0; i < files.Length; i++)
                    {
                        var file = files[i];
                        if (!RTMetaDataEditor.inst.VerifyFile(Path.GetFileName(file)))
                            continue;

                        var fileDirectory = Path.GetDirectoryName(file).Replace("\\", "/");
                        var fileDestination = file.Replace(fileDirectory, directory);
                        if (!RTFile.DirectoryExists(Path.GetDirectoryName(fileDestination)))
                            Directory.CreateDirectory(Path.GetDirectoryName(fileDestination));

                        File.Copy(file, fileDestination, true);
                    }

                    EditorManager.inst.DisplayNotification($"Successfully copied {name} to {LevelManager.Path}!", 2f, EditorManager.NotificationType.Success);
                    HideWarningPopup();
                }, HideWarningPopup);
            }, 7);

            EditorHelper.AddEditorDropdown("Restart Editor", "", "File", ReloadSprite, () =>
            {
                DG.Tweening.DOTween.Clear();
                Updater.UpdateObjects(false);
                GameData.Current = null;
                GameData.Current = new GameData();
                TooltipHelper.InitTooltips();
                SceneManager.inst.LoadScene("Editor");
            }, 7);

            EditorHelper.AddEditorDropdown("Open Level Browser", "", "File", titleBar.Find("File/File Dropdown/Open/Image").GetComponent<Image>().sprite, () =>
            {
                EditorManager.inst.ShowDialog("Browser Popup");
                RefreshFileBrowserLevels();
            }, 3);

            EditorHelper.AddEditorDropdown("Convert VG to LS", "", "File", SearchSprite, () =>
            {
                EditorManager.inst.ShowDialog("Browser Popup");
                RTFileBrowser.inst.UpdateBrowser(Directory.GetCurrentDirectory(), new string[] { ".lsp", ".vgp", "lst", ".vgt", ".lsb", ".vgd" }, onSelectFile: _val =>
                {
                    bool failed = false;
                    if (_val.Contains(".lsp"))
                    {
                        var file = RTFile.ApplicationDirectory + prefabListSlash + Path.GetFileName(_val);
                        File.Copy(_val, file, RTFile.FileExists(file));
                        EditorManager.inst.DisplayNotification($"Copied {Path.GetFileName(_val)} to prefab ({prefabListPath}) folder.", 2f, EditorManager.NotificationType.Success);
                    }
                    else if (_val.Contains(".vgp"))
                    {
                        try
                        {
                            var file = RTFile.ReadFromFile(_val);

                            var vgjn = JSON.Parse(file);

                            var prefab = Prefab.ParseVG(vgjn);

                            var jn = prefab.ToJSON();

                            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + prefabListPath))
                                Directory.CreateDirectory(RTFile.ApplicationDirectory + prefabListPath);

                            string fileName = $"{prefab.Name.ToLower().Replace(" ", "_")}.lsp";
                            RTFile.WriteToFile(RTFile.ApplicationDirectory + prefabListSlash + fileName, jn.ToString());

                            file = null;
                            vgjn = null;
                            prefab = null;
                            jn = null;

                            EditorManager.inst.DisplayNotification($"Successfully converted {Path.GetFileName(_val)} to {fileName} and added it to your prefab ({prefabListPath}) folder.", 2f,
                                EditorManager.NotificationType.Success);

                            AchievementManager.inst.UnlockAchievement("time_machine");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(ex);
                            failed = true;
                        }
                    }
                    else if (_val.Contains(".lst"))
                    {
                        var file = RTFile.ApplicationDirectory + themeListSlash + Path.GetFileName(_val);
                        File.Copy(_val, file, RTFile.FileExists(file));
                        EditorManager.inst.DisplayNotification($"Copied {Path.GetFileName(_val)} to theme ({themeListPath}) folder.", 2f, EditorManager.NotificationType.Success);
                    }
                    else if (_val.Contains(".vgt"))
                    {
                        try
                        {
                            var file = RTFile.ReadFromFile(_val);

                            var vgjn = JSON.Parse(file);

                            var theme = BeatmapTheme.ParseVG(vgjn);

                            var jn = theme.ToJSON();

                            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + themeListPath))
                                Directory.CreateDirectory(RTFile.ApplicationDirectory + themeListPath);

                            var fileName = $"{theme.name.ToLower().Replace(" ", "_")}.lst";
                            RTFile.WriteToFile(RTFile.ApplicationDirectory + themeListSlash + fileName, jn.ToString());

                            file = null;
                            vgjn = null;
                            theme = null;
                            jn = null;

                            EditorManager.inst.DisplayNotification($"Successfully converted {Path.GetFileName(_val)} to {fileName} and added it to your theme ({themeListPath}) folder.", 2f,
                                EditorManager.NotificationType.Success);

                            AchievementManager.inst.UnlockAchievement("time_machine");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(ex);
                            failed = true;
                        }
                    }
                    else if (_val.Replace("\\", "/").Contains("/level.lsb"))
                    {
                        ShowWarningPopup("Warning! Selecting a level will copy all of its contents to your editor, are you sure you want to do this?", () =>
                        {
                            var path = _val.Replace("\\", "/").Replace("/level.lsb", "");

                            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);

                            foreach (var file in files)
                            {
                                var copyTo = file.Replace("\\", "/").Replace(Path.GetDirectoryName(path), RTFile.ApplicationDirectory + editorListPath);
                                File.Copy(file, copyTo, RTFile.FileExists(copyTo));
                            }

                            EditorManager.inst.DisplayNotification($"Copied {Path.GetFileName(path)} to level ({editorListPath}) folder.", 2f, EditorManager.NotificationType.Success);

                            HideWarningPopup();
                        }, () =>
                        {
                            HideWarningPopup();
                            EditorManager.inst.ShowDialog("Browser Popup");
                        });
                    }
                    else if (_val.Replace("\\", "/").Contains("/level.vgd"))
                    {
                        try
                        {
                            var path = _val.Replace("\\", "/").Replace("/level.vgd", "");

                            if (RTFile.FileExists(path + "/metadata.vgm") &&
                                (RTFile.FileExists(path + "/audio.ogg") || RTFile.FileExists(path + "/audio.wav") || RTFile.FileExists(path + "/audio.mp3") || RTFile.FileExists(path + "/level.ogg") || RTFile.FileExists(path + "/level.wav") || RTFile.FileExists(path + "/level.mp3")) &&
                                RTFile.FileExists(path + "/cover.jpg"))
                            {
                                var copyTo = path.Replace(Path.GetDirectoryName(path).Replace("\\", "/"), RTFile.ApplicationDirectory + editorListSlash);

                                if (!RTFile.DirectoryExists(copyTo))
                                    Directory.CreateDirectory(copyTo);

                                var metadataVGJSON = RTFile.ReadFromFile(path + "/metadata.vgm");

                                var metadataVGJN = JSON.Parse(metadataVGJSON);

                                var metadata = MetaData.ParseVG(metadataVGJN);

                                var metadataJN = metadata.ToJSON();

                                RTFile.WriteToFile(copyTo + "/metadata.lsb", metadataJN.ToString());

                                if (RTFile.FileExists(path + "/audio.ogg"))
                                    File.Copy(path + "/audio.ogg", copyTo + "/level.ogg", RTFile.FileExists(copyTo + "/level.ogg"));
                                if (RTFile.FileExists(path + "/audio.wav"))
                                    File.Copy(path + "/audio.wav", copyTo + "/level.wav", RTFile.FileExists(copyTo + "/level.wav"));
                                if (RTFile.FileExists(path + "/audio.mp3"))
                                    File.Copy(path + "/audio.mp3", copyTo + "/level.mp3", RTFile.FileExists(copyTo + "/level.mp3"));

                                if (RTFile.FileExists(path + "/level.ogg"))
                                    File.Copy(path + "/level.ogg", copyTo + "/level.ogg", RTFile.FileExists(copyTo + "/level.ogg"));
                                if (RTFile.FileExists(path + "/level.wav"))
                                    File.Copy(path + "/level.wav", copyTo + "/level.wav", RTFile.FileExists(copyTo + "/level.wav"));
                                if (RTFile.FileExists(path + "/level.mp3"))
                                    File.Copy(path + "/level.mp3", copyTo + "/level.mp3", RTFile.FileExists(copyTo + "/level.mp3"));

                                File.Copy(path + "/cover.jpg", copyTo + "/level.jpg", RTFile.FileExists(copyTo + "/level.jpg"));

                                var levelVGJSON = RTFile.ReadFromFile(path + "/level.vgd");

                                var levelVGJN = JSON.Parse(levelVGJSON);

                                var level = GameData.ParseVG(levelVGJN, false);

                                StartCoroutine(ProjectData.Writer.SaveData(copyTo + "/level.lsb", level, () =>
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
                                }, true));
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
                    }
                    else if (_val.Replace("\\", "/").Contains("/autosave_") && _val.Contains(".vgd"))
                    {
                        EditorManager.inst.DisplayNotification("Cannot select autosave.", 2f, EditorManager.NotificationType.Warning);
                        failed = true;
                    }

                    if (!failed)
                        EditorManager.inst.HideDialog("Browser Popup");
                });
            }, 4);

            var addFileToLevelFolder = EditorHelper.AddEditorDropdown("Add File to Level Folder", "", "File", SearchSprite, () =>
            {
                EditorManager.inst.ShowDialog("Browser Popup");
                RTFileBrowser.inst.UpdateBrowser(Directory.GetCurrentDirectory(), new string[] { ".ogg", ".wav", ".png", ".jpg", ".mp4", ".mov" }, onSelectFile: _val =>
                {
                    if (_val.Contains(".mp4") || _val.Contains(".mov"))
                    {
                        var copyTo = _val.Replace("\\", "/").Replace((Path.GetDirectoryName(_val) + "/").Replace("\\", "/"), RTFile.BasePath).Replace(Path.GetFileName(_val),
                            _val.Contains(".mp4") ? "bg.mp4" : "bg.mov");
                        File.Copy(_val, copyTo, RTFile.FileExists(copyTo));

                        if (RTFile.FileExists(copyTo) && CoreConfig.Instance.EnableVideoBackground.Value)
                            RTVideoManager.inst.Play(copyTo, 1f);
                        else
                            RTVideoManager.inst.Stop();

                        return;
                    }

                    var destination = _val.Replace("\\", "/").Replace((Path.GetDirectoryName(_val) + "/").Replace("\\", "/"), RTFile.BasePath);
                    File.Copy(_val, destination, RTFile.FileExists(destination));
                });
            }, 5);
            EditorHelper.SetComplexity(addFileToLevelFolder, Complexity.Normal);

            EditorHelper.AddEditorDropdown("Editor Preferences", "", "Edit", SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_preferences-white.png"), () =>
            {
                ConfigManager.inst.Show();
                ConfigManager.inst.SetTab(2);
            });

            EditorHelper.AddEditorDropdown("Clear Sprite Data", "", "Edit", titleBar.Find("File/File Dropdown/Quit to Main Menu/Image").GetComponent<Image>().sprite, () =>
            {
                ShowWarningPopup("Are you sure you want to clear sprite data? Any Image Shapes that use a stored image will have their images cleared and you will need to set them again.", () =>
                {
                    AssetManager.SpriteAssets.Clear();
                    HideWarningPopup();
                }, HideWarningPopup);
            });

            EditorHelper.AddEditorDropdown("Reset Event Offsets", "", "Edit", CloseSprite, () =>
            {
                RTEventManager.inst?.SetResetOffsets();

                EditorManager.inst.DisplayNotification("Event Offsets have been reset.", 1.4f, EditorManager.NotificationType.Success);
            });

            EditorHelper.AddEditorDropdown("Render Waveform", "", "Edit", ReloadSprite, () =>
            {
                if (EditorConfig.Instance.WaveformGenerate.Value)
                {
                    SetTimelineSprite(null);
                    StartCoroutine(AssignTimelineTexture());
                }
                else
                    SetTimelineSprite(null);
            });

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

                var beatmapObjects = GameData.Current.beatmapObjects.FindAll(x => x.integerVariable != 0);
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
            titleBar.Find("File/File Dropdown/Save As").gameObject.SetActive(true);
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

            var contextClickable = EditorManager.inst.GetDialog("Open File Popup").Dialog.gameObject.AddComponent<ContextClickable>();
            contextClickable.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                RefreshContextMenu(300f,
                    new ButtonFunction("Create folder", () =>
                    {
                        EditorManager.inst.ShowDialog("Folder Creator Popup");
                        RefreshFolderCreator($"{RTFile.ApplicationDirectory}{editorListPath}", () => UpdateEditorPath(true));
                    }),
                    new ButtonFunction("Paste", PasteLevel));
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

                RefreshContextMenu(300f,
                    new ButtonFunction("Set Level folder", () =>
                    {
                        EditorManager.inst.ShowDialog("Browser Popup");
                        RTFileBrowser.inst.UpdateBrowser(RTFile.ApplicationDirectory + "beatmaps", onSelectFolder: _val =>
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
                    }));
            };

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

                RefreshContextMenu(300f,
                    new ButtonFunction("Set Theme folder", () =>
                    {
                        EditorManager.inst.ShowDialog("Browser Popup");
                        RTFileBrowser.inst.UpdateBrowser(RTFile.ApplicationDirectory + "beatmaps", onSelectFolder: _val =>
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
                    }));
            };

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

                RefreshContextMenu(300f,
                    new ButtonFunction("Set Prefab folder", () =>
                    {
                        EditorManager.inst.ShowDialog("Browser Popup");
                        RTFileBrowser.inst.UpdateBrowser(RTFile.ApplicationDirectory + "beatmaps", onSelectFolder: _val =>
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
                    }));
            };

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
                RTFileBrowser.inst.UpdateBrowser(Directory.GetCurrentDirectory(), new string[] { ".ogg", ".wav", ".mp3" }, onSelectFile: _val =>
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
                    RTFileBrowser.inst.UpdateBrowser(Directory.GetCurrentDirectory(), new string[] { ".png" }, onSelectFile: _val =>
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

            EditorHelper.AddEditorDropdown("Search Objects", "", "Edit", SearchSprite, () =>
            {
                EditorManager.inst.ShowDialog("Object Search Popup");
                RefreshObjectSearch(x => ObjectEditor.inst.SetCurrentObject(ObjectEditor.inst.GetTimelineObject(x), Input.GetKey(KeyCode.LeftControl)));
            });
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

        void CreateMultiObjectEditor()
        {
            var eventButton = EditorPrefabHolder.Instance.Function1Button;

            var multiObjectEditorDialog = EditorManager.inst.GetDialog("Multi Object Editor").Dialog;

            EditorThemeManager.AddGraphic(multiObjectEditorDialog.GetComponent<Image>(), ThemeGroup.Background_1);

            var dataLeft = multiObjectEditorDialog.Find("data/left");

            dataLeft.gameObject.SetActive(true);

            var scrollView = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View").Duplicate(dataLeft);

            var parent = scrollView.transform.Find("Viewport/Content");

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

            // Layers
            {
                GenerateLabels(parent, 32f, "Set Group Layer");

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
            }

            // Depth
            {
                GenerateLabels(parent, 32f, "Set Group Depth");

                var inputFieldStorage = GenerateInputField(parent, "depth", "1", "Enter depth...", true);
                inputFieldStorage.leftButton.onClick.NewListener(() =>
                {
                    if (!int.TryParse(inputFieldStorage.inputField.text, out int num))
                        return;
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
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
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
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
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
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
                        if (timelineObject.IsBeatmapObject)
                            Updater.UpdateObject(timelineObject.GetData<BeatmapObject>(), "StartTime");
                        if (timelineObject.IsPrefabObject)
                            Updater.UpdatePrefab(timelineObject.GetData<PrefabObject>());

                        ObjectEditor.inst.RenderTimelineObjectPosition(timelineObject);
                    }
                });
                inputFieldStorage.middleButton.onClick.NewListener(() =>
                {
                    if (!float.TryParse(inputFieldStorage.inputField.text, out float num))
                        return;

                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                    {
                        timelineObject.Time = num;
                        if (timelineObject.IsBeatmapObject)
                            Updater.UpdateObject(timelineObject.GetData<BeatmapObject>(), "StartTime");
                        if (timelineObject.IsPrefabObject)
                            Updater.UpdatePrefab(timelineObject.GetData<PrefabObject>());

                        ObjectEditor.inst.RenderTimelineObjectPosition(timelineObject);
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
                        if (timelineObject.IsBeatmapObject)
                            Updater.UpdateObject(timelineObject.GetData<BeatmapObject>(), "StartTime");
                        if (timelineObject.IsPrefabObject)
                            Updater.UpdatePrefab(timelineObject.GetData<PrefabObject>());

                        ObjectEditor.inst.RenderTimelineObjectPosition(timelineObject);
                    }
                });
                TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDelta(inputFieldStorage.inputField));
            }

            // Autokill Offset
            {
                GenerateLabels(parent, 32f, "Set Autokill Offset");

                var inputFieldStorage = GenerateInputField(parent, "autokill offset", "0", "Enter autokill...", true);
                inputFieldStorage.leftButton.onClick.NewListener(() =>
                {
                    if (!float.TryParse(inputFieldStorage.inputField.text, out float num))
                        return;
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
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
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
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
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();
                        bm.autoKillOffset += num;
                        Updater.UpdateObject(bm, "Autokill");
                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                    }
                });
                TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDelta(inputFieldStorage.inputField));
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
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
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
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                    {
                        timelineObject.GetData<BeatmapObject>().name += inputFieldStorage.inputField.text;
                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                    }
                });

                EditorThemeManager.AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);
            }

            // Tags
            {
                GenerateLabels(parent, 32f, "Add a Tag");

                var multiNameSet = EditorPrefabHolder.Instance.NumberInputField.Duplicate(parent, "name");
                multiNameSet.transform.localScale = Vector3.one;
                var inputFieldStorage = multiNameSet.GetComponent<InputFieldStorage>();

                multiNameSet.transform.AsRT().sizeDelta = new Vector2(428f, 32f);

                inputFieldStorage.inputField.onValueChanged.ClearAll();
                inputFieldStorage.inputField.characterValidation = InputField.CharacterValidation.None;
                inputFieldStorage.inputField.characterLimit = 0;
                inputFieldStorage.inputField.text = "object group";
                inputFieldStorage.inputField.transform.AsRT().sizeDelta = new Vector2(300f, 32f);
                ((Text)inputFieldStorage.inputField.placeholder).text = "Enter a tag...";

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
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                    {
                        timelineObject.GetData<BeatmapObject>().tags.Add(inputFieldStorage.inputField.text);
                    }
                });

                EditorThemeManager.AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);
            }

            // Clear data
            {
                GenerateLabels(parent, 32f, "Clear data from objects");

                GenerateButtons(parent, 32f, 8f,
                     new ButtonFunction("Clear tags", () =>
                     {
                         ShowWarningPopup("You are about to clear tags from all selected objects, this <b>CANNOT</b> be undone!", () =>
                         {
                             foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
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
                             foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                             {
                                 var bm = timelineObject.GetData<BeatmapObject>();
                                 foreach (var tkf in timelineObject.InternalSelections)
                                 {
                                     Destroy(tkf.GameObject);
                                 }
                                 timelineObject.InternalSelections.Clear();
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
                             foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                             {
                                 beatmapObject.modifiers.Clear();
                                 Updater.UpdateObject(beatmapObject);
                             }

                             HideWarningPopup();
                         }, HideWarningPopup);
                     }) { FontSize = 16 });
            }

            // Optimization
            {
                var labels = GenerateLabels(parent, 32f, "Auto optimize objects");
                var buttons = GenerateButtons(parent, 32f, 0f, new ButtonFunction("Optimize", () =>
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                    {
                        var beatmapObject = timelineObject.GetData<BeatmapObject>();
                        beatmapObject.SetAutokillToScale(GameData.Current.beatmapObjects);
                        Updater.UpdateObject(beatmapObject, "Autokill");
                        ObjectEditor.inst.RenderTimelineObjectPosition(timelineObject);
                    }
                }));
                EditorHelper.SetComplexity(labels, Complexity.Normal);
                EditorHelper.SetComplexity(buttons, Complexity.Normal);
            }

            // Song Time Autokill
            {
                GenerateLabels(parent, 32f, "Set autokill to current time");
                GenerateButtons(parent, 32f, 0f, new ButtonFunction("Set", () =>
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
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
            }

            // Autokill Type
            {
                GenerateLabels(parent, 32f, "Set Autokill Type");

                GenerateButtons(parent, 48f, 8f,
                    new ButtonFunction("No Autokill", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.autoKillType = AutoKillType.OldStyleNoAutokill;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "Autokill");
                        }
                    }),
                    new ButtonFunction("Last KF", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.autoKillType = AutoKillType.LastKeyframe;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "Autokill");
                        }
                    }),
                    new ButtonFunction("Last KF Offset", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.autoKillType = AutoKillType.LastKeyframeOffset;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "Autokill");
                        }
                    }),
                    new ButtonFunction("Fixed Time", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.autoKillType = AutoKillType.FixedTime;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "Autokill");
                        }
                    }),
                    new ButtonFunction("Song Time", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.autoKillType = AutoKillType.SongTime;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "Autokill");
                        }
                    }));
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
                            foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
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
                GenerateLabels(parent, 32f, "Modify parent desync");

                GenerateButtons(parent, 32f, 8f,
                    new ButtonFunction("On", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.FindAll(x => x.IsBeatmapObject))
                        {
                            timelineObject.GetData<BeatmapObject>().desync = true;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                        }
                    }),
                    new ButtonFunction("Off", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.FindAll(x => x.IsBeatmapObject))
                        {
                            timelineObject.GetData<BeatmapObject>().desync = false;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                        }
                    }));
                GenerateButtons(parent, 32f, 0f, new ButtonFunction("Swap", () =>
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.FindAll(x => x.IsBeatmapObject))
                    {
                        timelineObject.GetData<BeatmapObject>().desync = !timelineObject.GetData<BeatmapObject>().desync;

                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                    }
                }));
            }

            // Force Snap BPM
            {
                GenerateLabels(parent, 32f, "Force Snap Start Time to BPM");
                GenerateButtons(parent, 32f, 8f, new ButtonFunction("Snap", () =>
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                    {
                        timelineObject.Time = SnapToBPM(timelineObject.Time);
                        if (timelineObject.IsBeatmapObject)
                            Updater.UpdateObject(timelineObject.GetData<BeatmapObject>(), "Start Time");
                        if (timelineObject.IsPrefabObject)
                            Updater.UpdatePrefab(timelineObject.GetData<PrefabObject>(), "Start Time");

                        ObjectEditor.inst.RenderTimelineObjectPosition(timelineObject);
                    }
                }));
            }

            // Object Type
            {
                GenerateLabels(parent, 32f, "Set Object Type");

                GenerateButtons(parent, 32f, 8f,
                    new ButtonFunction("Sub", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
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
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
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
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.objectType = BeatmapObject.ObjectType.Normal;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "ObjectType");
                        }
                    }),
                    new ButtonFunction(nameof(BeatmapObject.ObjectType.Helper), () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.objectType = BeatmapObject.ObjectType.Helper;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "ObjectType");
                        }
                    }),
                    new ButtonFunction("Deco", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.objectType = BeatmapObject.ObjectType.Decoration;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "ObjectType");
                        }
                    }),
                    new ButtonFunction(nameof(BeatmapObject.ObjectType.Empty), () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.objectType = BeatmapObject.ObjectType.Empty;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "ObjectType");
                        }
                    }),
                    new ButtonFunction(nameof(BeatmapObject.ObjectType.Solid), () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.objectType = BeatmapObject.ObjectType.Solid;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "ObjectType");
                        }
                    }));
            }
            
            // Gradient Type
            {
                GenerateLabels(parent, 32f, "Set Gradient Type");

                GenerateButtons(parent, 32f, 8f,
                    new ButtonFunction("Sub", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
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
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
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

                GenerateButtons(parent, 48f, 8f,
                    new ButtonFunction(nameof(BeatmapObject.GradientType.Normal), () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.gradientType = BeatmapObject.GradientType.Normal;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "GradientType");
                        }
                    }),
                    new ButtonFunction("Linear Right", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.gradientType = BeatmapObject.GradientType.RightLinear;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "GradientType");
                        }
                    }),
                    new ButtonFunction("Linear Left", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.gradientType = BeatmapObject.GradientType.LeftLinear;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "GradientType");
                        }
                    }),
                    new ButtonFunction("Radial In", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.gradientType = BeatmapObject.GradientType.OutInRadial;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "GradientType");
                        }
                    }),
                    new ButtonFunction("Radial Out", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.gradientType = BeatmapObject.GradientType.InOutRadial;

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            Updater.UpdateObject(bm, "GradientType");
                        }
                    }));
            }

            // Assign Objects to Prefab
            {
                GenerateLabels(parent, 32f, "Assign Objects to Prefab");
                GenerateButtons(parent, 32f, 8f,
                    new ButtonFunction("Assign", () =>
                    {
                        selectingMultiple = true;
                        prefabPickerEnabled = true;
                    }),
                    new ButtonFunction("Remove", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                        {
                            var beatmapObject = timelineObject.GetData<BeatmapObject>();
                            beatmapObject.prefabID = "";
                            beatmapObject.prefabInstanceID = "";
                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                        }
                    }));
            }

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
                        foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                        {
                            beatmapObject.background = true;
                            if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
                                levelObject.visualObject.GameObject.layer = beatmapObject.background ? 9 : 8;
                        }
                    }),
                    new ButtonFunction("Foreground", () =>
                    {
                        foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                        {
                            beatmapObject.background = false;
                            if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
                                levelObject.visualObject.GameObject.layer = beatmapObject.background ? 9 : 8;
                        }
                    }));
                var buttons2 = GenerateButtons(parent, 32f, 0f, new ButtonFunction("Swap", () =>
                {
                    foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
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
                        foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                        {
                            beatmapObject.LDM = true;
                            Updater.UpdateObject(beatmapObject);
                        }
                    }),
                    new ButtonFunction("Off", () =>
                    {
                        foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                        {
                            beatmapObject.LDM = false;
                            Updater.UpdateObject(beatmapObject);
                        }
                    }));
                var buttons2 = GenerateButtons(parent, 32f, 0f, new ButtonFunction("Swap", () =>
                {
                    foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                    {
                        beatmapObject.LDM = !beatmapObject.LDM;
                        Updater.UpdateObject(beatmapObject);
                    }
                }));
                EditorHelper.SetComplexity(labels, Complexity.Advanced);
                EditorHelper.SetComplexity(buttons1, Complexity.Advanced);
                EditorHelper.SetComplexity(buttons2, Complexity.Advanced);
            }

            // Sync object selection
            {
                GenerateLabels(parent, 32f, "Sync to specific object");

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
                    SyncObjectData("Parent", eventData, SelectObject.SetParent, false, true, "Parent");
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

                    }, false, true, "Keyframes");
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
            }

            // Replace Name
            {
                GenerateLabels(parent, 32f, "Replace Name");

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
                ((Text)oldNameIF.placeholder).text = "Enter old name...";
                ((Text)oldNameIF.placeholder).alignment = TextAnchor.MiddleLeft;
                ((Text)oldNameIF.placeholder).fontSize = 16;
                ((Text)oldNameIF.placeholder).color = new Color(0f, 0f, 0f, 0.3f);

                oldNameIF.onValueChanged.ClearAll();

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
                ((Text)newNameIF.placeholder).text = "Enter new name...";
                ((Text)newNameIF.placeholder).alignment = TextAnchor.MiddleLeft;
                ((Text)newNameIF.placeholder).fontSize = 16;
                ((Text)newNameIF.placeholder).color = new Color(0f, 0f, 0f, 0.3f);

                newNameIF.onValueChanged.ClearAll();

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
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();
                        bm.name = bm.name.Replace(oldNameIF.text, newNameIF.text);
                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                    }
                });
            }

            // Replace Tags
            {
                GenerateLabels(parent, 32f, "Replace Tags");

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
                ((Text)oldNameIF.placeholder).text = "Enter old tag...";
                ((Text)oldNameIF.placeholder).alignment = TextAnchor.MiddleLeft;
                ((Text)oldNameIF.placeholder).fontSize = 16;
                ((Text)oldNameIF.placeholder).color = new Color(0f, 0f, 0f, 0.3f);

                oldNameIF.onValueChanged.ClearAll();

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
                ((Text)newNameIF.placeholder).text = "Enter new tag...";
                ((Text)newNameIF.placeholder).alignment = TextAnchor.MiddleLeft;
                ((Text)newNameIF.placeholder).fontSize = 16;
                ((Text)newNameIF.placeholder).color = new Color(0f, 0f, 0f, 0.3f);

                newNameIF.onValueChanged.ClearAll();

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
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();
                        for (int i = 0; i < bm.tags.Count; i++)
                        {
                            bm.tags[i] = bm.tags[i].Replace(oldNameIF.text, newNameIF.text);
                        }
                    }
                });
            }

            // Replace Text
            {
                GenerateLabels(parent, 32f, "Replace Text");
                
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
                ((Text)oldNameIF.placeholder).text = "Enter old text...";
                ((Text)oldNameIF.placeholder).alignment = TextAnchor.MiddleLeft;
                ((Text)oldNameIF.placeholder).fontSize = 16;
                ((Text)oldNameIF.placeholder).color = new Color(0f, 0f, 0f, 0.3f);

                oldNameIF.onValueChanged.ClearAll();

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
                ((Text)newNameIF.placeholder).text = "Enter new text...";
                ((Text)newNameIF.placeholder).alignment = TextAnchor.MiddleLeft;
                ((Text)newNameIF.placeholder).fontSize = 16;
                ((Text)newNameIF.placeholder).color = new Color(0f, 0f, 0f, 0.3f);

                newNameIF.onValueChanged.ClearAll();

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
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();
                        bm.text = bm.text.Replace(oldNameIF.text, newNameIF.text);
                        Updater.UpdateObject(bm, "Shape");
                    }
                });
            }

            // Replace Modifier
            {
                GenerateLabels(parent, 32f, "Replace Modifier values");

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
                ((Text)oldNameIF.placeholder).text = "Enter old modifier...";
                ((Text)oldNameIF.placeholder).alignment = TextAnchor.MiddleLeft;
                ((Text)oldNameIF.placeholder).fontSize = 16;
                ((Text)oldNameIF.placeholder).color = new Color(0f, 0f, 0f, 0.3f);

                oldNameIF.onValueChanged.ClearAll();

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
                ((Text)newNameIF.placeholder).text = "Enter new modifier...";
                ((Text)newNameIF.placeholder).alignment = TextAnchor.MiddleLeft;
                ((Text)newNameIF.placeholder).fontSize = 16;
                ((Text)newNameIF.placeholder).color = new Color(0f, 0f, 0f, 0.3f);

                newNameIF.onValueChanged.ClearAll();

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
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
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
            }

            // Assign Colors
            {
                GenerateLabels(parent, 32f, "Assign colors");

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

                GenerateLabels(parent, 32f, "Opacity");

                var opacityIF = CreateInputField("opacity", "", "Enter value... (Keep empty to not set)", parent, isInteger: false);
                ((Text)opacityIF.placeholder).fontSize = 13;

                GenerateLabels(parent, 32f, "Hue");

                var hueIF = CreateInputField("hue", "", "Enter value... (Keep empty to not set)", parent, isInteger: false);
                ((Text)hueIF.placeholder).fontSize = 13;

                GenerateLabels(parent, 32f, "Saturation");

                var satIF = CreateInputField("sat", "", "Enter value... (Keep empty to not set)", parent, isInteger: false);
                ((Text)satIF.placeholder).fontSize = 13;

                GenerateLabels(parent, 32f, "Value (Brightness)");

                var valIF = CreateInputField("val", "", "Enter value... (Keep empty to not set)", parent, isInteger: false);
                ((Text)valIF.placeholder).fontSize = 13;

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

                GenerateLabels(parent, 32f, "Gradient Opacity");

                var opacityGradientIF = CreateInputField("opacity", "", "Enter value... (Keep empty to not set)", parent, isInteger: false);
                ((Text)opacityGradientIF.placeholder).fontSize = 13;

                GenerateLabels(parent, 32f, "Gradient Hue");

                var hueGradientIF = CreateInputField("hue", "", "Enter value... (Keep empty to not set)", parent, isInteger: false);
                ((Text)hueGradientIF.placeholder).fontSize = 13;

                GenerateLabels(parent, 32f, "Gradient Saturation");

                var satGradientIF = CreateInputField("sat", "", "Enter value... (Keep empty to not set)", parent, isInteger: false);
                ((Text)satGradientIF.placeholder).fontSize = 13;

                GenerateLabels(parent, 32f, "Gradient Value (Brightness)");

                var valGradientIF = CreateInputField("val", "", "Enter value... (Keep empty to not set)", parent, isInteger: false);
                ((Text)valGradientIF.placeholder).fontSize = 13;

                GenerateLabels(parent, 32f, "Ease Type");

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
                    GenerateLabels(parent, 32f, "Assign to all Color Keyframes");
                    GenerateButtons(parent, 32f, 8f,
                        new ButtonFunction("Set", () =>
                        {
                            DataManager.LSAnimation anim = default;
                            bool setCurve = curves.value != 0 && DataManager.inst.AnimationListDictionary.TryGetValue(curves.value - 1, out anim);
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
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
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
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
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
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
                }

                // Assign to Index
                {
                    GenerateLabels(parent, 32f, "Assign to Index");

                    var assignIndex = CreateInputField("index", "0", "Enter index...", parent, maxValue: int.MaxValue);
                    GenerateButtons(parent, 32f, 8f,
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

                                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
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
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
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

                                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
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
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
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

                                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
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
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();

                                SubKeyframeValues((EventKeyframe)bm.events[3][Mathf.Clamp(num, 0, bm.events[3].Count - 1)], curves,
                                    opacityIF.text, hueIF.text, satIF.text, valIF.text, opacityGradientIF.text, hueGradientIF.text, satGradientIF.text, valGradientIF.text);

                                Updater.UpdateObject(bm, "Keyframes");
                            }
                        }));
                }

                // Create Color Keyframe
                {
                    GenerateLabels(parent, 32f, "Create Color Keyframe");
                    GenerateButtons(parent, 32f, 0f, new ButtonFunction("Create", () =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
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
                                if (curves.value != 0 && DataManager.inst.AnimationListDictionary.ContainsKey(curves.value - 1))
                                    kf.curveType = DataManager.inst.AnimationListDictionary[curves.value - 1];

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
                }
            }

            // Paste
            {
                GenerateLabels(parent, 32f, "Paste Keyframe data (All types)");

                // All Types
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

                    var pasteAllTypesToAllObject = eventButton.Duplicate(pasteAllTypesBaseRT, name);
                    pasteAllTypesToAllObject.transform.localScale = Vector3.one;

                    ((RectTransform)pasteAllTypesToAllObject.transform).sizeDelta = new Vector2(180f, 32f);

                    var pasteAllTypesToAllText = pasteAllTypesToAllObject.transform.GetChild(0).GetComponent<Text>();
                    pasteAllTypesToAllText.text = "Paste to All";

                    EditorThemeManager.AddGraphic(pasteAllTypesToAllObject.GetComponent<Image>(), ThemeGroup.Paste, true);
                    EditorThemeManager.AddGraphic(pasteAllTypesToAllText, ThemeGroup.Paste_Text);

                    var pasteAllTypesToAll = pasteAllTypesToAllObject.GetComponent<Button>();
                    pasteAllTypesToAll.onClick.ClearAll();
                    pasteAllTypesToAll.onClick.AddListener(() =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            if (timelineObject.IsBeatmapObject)
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                if (ObjectEditor.inst.CopiedPositionData != null)
                                    for (int i = 0; i < bm.events[0].Count; i++)
                                    {
                                        var kf = (EventKeyframe)bm.events[0][i];
                                        kf.curveType = ObjectEditor.inst.CopiedPositionData.curveType;
                                        kf.eventValues = ObjectEditor.inst.CopiedPositionData.eventValues.Copy();
                                        kf.eventRandomValues = ObjectEditor.inst.CopiedPositionData.eventRandomValues.Copy();
                                        kf.random = ObjectEditor.inst.CopiedPositionData.random;
                                        kf.relative = ObjectEditor.inst.CopiedPositionData.relative;

                                        ObjectEditor.inst.RenderKeyframes(bm);
                                        ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                        Updater.UpdateObject(bm, "Keyframes");
                                        EditorManager.inst.DisplayNotification("Pasted position keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                    }
                                else
                                    EditorManager.inst.DisplayNotification("Position keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                                if (ObjectEditor.inst.CopiedScaleData != null)
                                    for (int i = 0; i < bm.events[1].Count; i++)
                                    {
                                        var kf = (EventKeyframe)bm.events[1][i];
                                        kf.curveType = ObjectEditor.inst.CopiedScaleData.curveType;
                                        kf.eventValues = ObjectEditor.inst.CopiedScaleData.eventValues.Copy();
                                        kf.eventRandomValues = ObjectEditor.inst.CopiedScaleData.eventRandomValues.Copy();
                                        kf.random = ObjectEditor.inst.CopiedScaleData.random;
                                        kf.relative = ObjectEditor.inst.CopiedScaleData.relative;

                                        ObjectEditor.inst.RenderKeyframes(bm);
                                        ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                        Updater.UpdateObject(bm, "Keyframes");
                                        EditorManager.inst.DisplayNotification("Pasted scale keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                    }
                                else
                                    EditorManager.inst.DisplayNotification("Scale keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                                if (ObjectEditor.inst.CopiedRotationData != null)
                                    for (int i = 0; i < bm.events[2].Count; i++)
                                    {
                                        var kf = (EventKeyframe)bm.events[2][i];
                                        kf.curveType = ObjectEditor.inst.CopiedRotationData.curveType;
                                        kf.eventValues = ObjectEditor.inst.CopiedRotationData.eventValues.Copy();
                                        kf.eventRandomValues = ObjectEditor.inst.CopiedRotationData.eventRandomValues.Copy();
                                        kf.random = ObjectEditor.inst.CopiedRotationData.random;
                                        kf.relative = ObjectEditor.inst.CopiedRotationData.relative;

                                        ObjectEditor.inst.RenderKeyframes(bm);
                                        ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                        Updater.UpdateObject(bm, "Keyframes");
                                        EditorManager.inst.DisplayNotification("Pasted rotation keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                    }
                                else
                                    EditorManager.inst.DisplayNotification("Rotation keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                                if (ObjectEditor.inst.CopiedColorData != null)
                                    for (int i = 0; i < bm.events[3].Count; i++)
                                    {
                                        var kf = (EventKeyframe)bm.events[3][i];
                                        kf.curveType = ObjectEditor.inst.CopiedColorData.curveType;
                                        kf.eventValues = ObjectEditor.inst.CopiedColorData.eventValues.Copy();
                                        kf.eventRandomValues = ObjectEditor.inst.CopiedColorData.eventRandomValues.Copy();
                                        kf.random = ObjectEditor.inst.CopiedColorData.random;
                                        kf.relative = ObjectEditor.inst.CopiedColorData.relative;

                                        ObjectEditor.inst.RenderKeyframes(bm);
                                        ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                        Updater.UpdateObject(bm, "Keyframes");
                                        EditorManager.inst.DisplayNotification("Pasted color keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                    }
                                else
                                    EditorManager.inst.DisplayNotification("Color keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                            }
                        }
                    });

                    var pasteAllTypesToIndexObject = eventButton.Duplicate(pasteAllTypesBaseRT, name);
                    pasteAllTypesToIndexObject.transform.localScale = Vector3.one;

                    ((RectTransform)pasteAllTypesToIndexObject.transform).sizeDelta = new Vector2(180f, 32f);

                    var pasteAllTypesToIndexText = pasteAllTypesToIndexObject.transform.GetChild(0).GetComponent<Text>();
                    pasteAllTypesToIndexText.text = "Paste to Index";

                    EditorThemeManager.AddGraphic(pasteAllTypesToIndexObject.GetComponent<Image>(), ThemeGroup.Paste, true);
                    EditorThemeManager.AddGraphic(pasteAllTypesToIndexText, ThemeGroup.Paste_Text);

                    var pasteAllTypesToIndex = pasteAllTypesToIndexObject.GetComponent<Button>();
                    pasteAllTypesToIndex.onClick.ClearAll();
                    pasteAllTypesToIndex.onClick.AddListener(() =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            if (timelineObject.IsBeatmapObject && int.TryParse(index.text, out int num))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                if (ObjectEditor.inst.CopiedPositionData != null)
                                {
                                    var kf = (EventKeyframe)bm.events[0][Mathf.Clamp(num, 0, bm.events[0].Count - 1)];
                                    kf.curveType = ObjectEditor.inst.CopiedPositionData.curveType;
                                    kf.eventValues = ObjectEditor.inst.CopiedPositionData.eventValues.Copy();
                                    kf.eventRandomValues = ObjectEditor.inst.CopiedPositionData.eventRandomValues.Copy();
                                    kf.random = ObjectEditor.inst.CopiedPositionData.random;
                                    kf.relative = ObjectEditor.inst.CopiedPositionData.relative;

                                    ObjectEditor.inst.RenderKeyframes(bm);
                                    ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                    Updater.UpdateObject(bm, "Keyframes");
                                    EditorManager.inst.DisplayNotification("Pasted position keyframe data to current selected keyframe.", 2f, EditorManager.NotificationType.Success);
                                }
                                else
                                    EditorManager.inst.DisplayNotification("Position keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                                if (ObjectEditor.inst.CopiedScaleData != null)
                                {
                                    var kf = (EventKeyframe)bm.events[1][Mathf.Clamp(num, 0, bm.events[1].Count - 1)];
                                    kf.curveType = ObjectEditor.inst.CopiedScaleData.curveType;
                                    kf.eventValues = ObjectEditor.inst.CopiedScaleData.eventValues.Copy();
                                    kf.eventRandomValues = ObjectEditor.inst.CopiedScaleData.eventRandomValues.Copy();
                                    kf.random = ObjectEditor.inst.CopiedScaleData.random;
                                    kf.relative = ObjectEditor.inst.CopiedScaleData.relative;

                                    ObjectEditor.inst.RenderKeyframes(bm);
                                    ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                    Updater.UpdateObject(bm, "Keyframes");
                                    EditorManager.inst.DisplayNotification("Pasted scale keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                }
                                else
                                    EditorManager.inst.DisplayNotification("Scale keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                                if (ObjectEditor.inst.CopiedRotationData != null)
                                {
                                    var kf = (EventKeyframe)bm.events[2][Mathf.Clamp(num, 0, bm.events[2].Count - 1)];
                                    kf.curveType = ObjectEditor.inst.CopiedRotationData.curveType;
                                    kf.eventValues = ObjectEditor.inst.CopiedRotationData.eventValues.Copy();
                                    kf.eventRandomValues = ObjectEditor.inst.CopiedRotationData.eventRandomValues.Copy();
                                    kf.random = ObjectEditor.inst.CopiedRotationData.random;
                                    kf.relative = ObjectEditor.inst.CopiedRotationData.relative;

                                    ObjectEditor.inst.RenderKeyframes(bm);
                                    ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                    Updater.UpdateObject(bm, "Keyframes");
                                    EditorManager.inst.DisplayNotification("Pasted rotation keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                }
                                else
                                    EditorManager.inst.DisplayNotification("Rotation keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                                if (ObjectEditor.inst.CopiedColorData != null)
                                {
                                    var kf = (EventKeyframe)bm.events[3][Mathf.Clamp(num, 0, bm.events[3].Count - 1)];
                                    kf.curveType = ObjectEditor.inst.CopiedColorData.curveType;
                                    kf.eventValues = ObjectEditor.inst.CopiedColorData.eventValues.Copy();
                                    kf.eventRandomValues = ObjectEditor.inst.CopiedColorData.eventRandomValues.Copy();
                                    kf.random = ObjectEditor.inst.CopiedColorData.random;
                                    kf.relative = ObjectEditor.inst.CopiedColorData.relative;

                                    ObjectEditor.inst.RenderKeyframes(bm);
                                    ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                    Updater.UpdateObject(bm, "Keyframes");
                                    EditorManager.inst.DisplayNotification("Pasted color keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                }
                                else
                                    EditorManager.inst.DisplayNotification("Color keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                            }
                        }
                    });
                }

                GenerateLabels(parent, 32f, "Paste Keyframe data (Position)");

                // Position
                {
                    var index = CreateInputField("index", "0", "Enter index...", parent, maxValue: int.MaxValue);

                    var pasteAllTypesBase = new GameObject("paste position");
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

                    var pasteAllTypesToAllObject = eventButton.Duplicate(pasteAllTypesBaseRT, name);
                    pasteAllTypesToAllObject.transform.localScale = Vector3.one;

                    ((RectTransform)pasteAllTypesToAllObject.transform).sizeDelta = new Vector2(180f, 32f);

                    var pasteAllTypesToAllText = pasteAllTypesToAllObject.transform.GetChild(0).GetComponent<Text>();
                    pasteAllTypesToAllText.text = "Paste to All";

                    EditorThemeManager.AddGraphic(pasteAllTypesToAllObject.GetComponent<Image>(), ThemeGroup.Paste, true);
                    EditorThemeManager.AddGraphic(pasteAllTypesToAllText, ThemeGroup.Paste_Text);

                    var pasteAllTypesToAll = pasteAllTypesToAllObject.GetComponent<Button>();
                    pasteAllTypesToAll.onClick.ClearAll();
                    pasteAllTypesToAll.onClick.AddListener(() =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            if (timelineObject.IsBeatmapObject)
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                if (ObjectEditor.inst.CopiedPositionData != null)
                                    for (int i = 0; i < bm.events[0].Count; i++)
                                    {
                                        var kf = (EventKeyframe)bm.events[0][i];
                                        kf.curveType = ObjectEditor.inst.CopiedPositionData.curveType;
                                        kf.eventValues = ObjectEditor.inst.CopiedPositionData.eventValues.Copy();
                                        kf.eventRandomValues = ObjectEditor.inst.CopiedPositionData.eventRandomValues.Copy();
                                        kf.random = ObjectEditor.inst.CopiedPositionData.random;
                                        kf.relative = ObjectEditor.inst.CopiedPositionData.relative;

                                        ObjectEditor.inst.RenderKeyframes(bm);
                                        ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                        Updater.UpdateObject(bm, "Keyframes");
                                        EditorManager.inst.DisplayNotification("Pasted position keyframe data to current selected keyframe.", 2f, EditorManager.NotificationType.Success);
                                    }
                                else
                                    EditorManager.inst.DisplayNotification("Position keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                            }
                        }
                    });

                    var pasteAllTypesToIndexObject = eventButton.Duplicate(pasteAllTypesBaseRT, name);
                    pasteAllTypesToIndexObject.transform.localScale = Vector3.one;

                    ((RectTransform)pasteAllTypesToIndexObject.transform).sizeDelta = new Vector2(180f, 32f);

                    var pasteAllTypesToIndexText = pasteAllTypesToIndexObject.transform.GetChild(0).GetComponent<Text>();
                    pasteAllTypesToIndexText.text = "Paste to Index";

                    EditorThemeManager.AddGraphic(pasteAllTypesToIndexObject.GetComponent<Image>(), ThemeGroup.Paste, true);
                    EditorThemeManager.AddGraphic(pasteAllTypesToIndexText, ThemeGroup.Paste_Text);

                    var pasteAllTypesToIndex = pasteAllTypesToIndexObject.GetComponent<Button>();
                    pasteAllTypesToIndex.onClick.ClearAll();
                    pasteAllTypesToIndex.onClick.AddListener(() =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            if (timelineObject.IsBeatmapObject && int.TryParse(index.text, out int num))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                if (ObjectEditor.inst.CopiedPositionData != null)
                                {
                                    var kf = (EventKeyframe)bm.events[0][Mathf.Clamp(num, 0, bm.events[0].Count - 1)];
                                    kf.curveType = ObjectEditor.inst.CopiedPositionData.curveType;
                                    kf.eventValues = ObjectEditor.inst.CopiedPositionData.eventValues.Copy();
                                    kf.eventRandomValues = ObjectEditor.inst.CopiedPositionData.eventRandomValues.Copy();
                                    kf.random = ObjectEditor.inst.CopiedPositionData.random;
                                    kf.relative = ObjectEditor.inst.CopiedPositionData.relative;

                                    ObjectEditor.inst.RenderKeyframes(bm);
                                    ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                    Updater.UpdateObject(bm, "Keyframes");
                                    EditorManager.inst.DisplayNotification("Pasted position keyframe data to current selected keyframe.", 2f, EditorManager.NotificationType.Success);
                                }
                                else
                                    EditorManager.inst.DisplayNotification("Position keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                            }
                        }
                    });
                }

                GenerateLabels(parent, 32f, "Paste Keyframe data (Scale)");

                // Scale
                {
                    var index = CreateInputField("index", "0", "Enter index...", parent, maxValue: int.MaxValue);

                    var pasteAllTypesBase = new GameObject("paste scale");
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

                    var pasteAllTypesToAllObject = eventButton.Duplicate(pasteAllTypesBaseRT, name);
                    pasteAllTypesToAllObject.transform.localScale = Vector3.one;

                    ((RectTransform)pasteAllTypesToAllObject.transform).sizeDelta = new Vector2(180f, 32f);

                    var pasteAllTypesToAllText = pasteAllTypesToAllObject.transform.GetChild(0).GetComponent<Text>();
                    pasteAllTypesToAllText.text = "Paste to All";

                    EditorThemeManager.AddGraphic(pasteAllTypesToAllObject.GetComponent<Image>(), ThemeGroup.Paste, true);
                    EditorThemeManager.AddGraphic(pasteAllTypesToAllText, ThemeGroup.Paste_Text);

                    var pasteAllTypesToAll = pasteAllTypesToAllObject.GetComponent<Button>();
                    pasteAllTypesToAll.onClick.ClearAll();
                    pasteAllTypesToAll.onClick.AddListener(() =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            if (timelineObject.IsBeatmapObject)
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                if (ObjectEditor.inst.CopiedScaleData != null)
                                    for (int i = 0; i < bm.events[1].Count; i++)
                                    {
                                        var kf = (EventKeyframe)bm.events[1][i];
                                        kf.curveType = ObjectEditor.inst.CopiedScaleData.curveType;
                                        kf.eventValues = ObjectEditor.inst.CopiedScaleData.eventValues.Copy();
                                        kf.eventRandomValues = ObjectEditor.inst.CopiedScaleData.eventRandomValues.Copy();
                                        kf.random = ObjectEditor.inst.CopiedScaleData.random;
                                        kf.relative = ObjectEditor.inst.CopiedScaleData.relative;

                                        ObjectEditor.inst.RenderKeyframes(bm);
                                        ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                        Updater.UpdateObject(bm, "Keyframes");
                                        EditorManager.inst.DisplayNotification("Pasted scale keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                    }
                                else
                                    EditorManager.inst.DisplayNotification("Scale keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                            }
                        }
                    });

                    var pasteAllTypesToIndexObject = eventButton.Duplicate(pasteAllTypesBaseRT, name);
                    pasteAllTypesToIndexObject.transform.localScale = Vector3.one;

                    ((RectTransform)pasteAllTypesToIndexObject.transform).sizeDelta = new Vector2(180f, 32f);

                    var pasteAllTypesToIndexText = pasteAllTypesToIndexObject.transform.GetChild(0).GetComponent<Text>();
                    pasteAllTypesToIndexText.text = "Paste to Index";

                    EditorThemeManager.AddGraphic(pasteAllTypesToIndexObject.GetComponent<Image>(), ThemeGroup.Paste, true);
                    EditorThemeManager.AddGraphic(pasteAllTypesToIndexText, ThemeGroup.Paste_Text);

                    var pasteAllTypesToIndex = pasteAllTypesToIndexObject.GetComponent<Button>();
                    pasteAllTypesToIndex.onClick.ClearAll();
                    pasteAllTypesToIndex.onClick.AddListener(() =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            if (timelineObject.IsBeatmapObject && int.TryParse(index.text, out int num))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                if (ObjectEditor.inst.CopiedScaleData != null)
                                {
                                    var kf = (EventKeyframe)bm.events[1][Mathf.Clamp(num, 0, bm.events[1].Count - 1)];
                                    kf.curveType = ObjectEditor.inst.CopiedScaleData.curveType;
                                    kf.eventValues = ObjectEditor.inst.CopiedScaleData.eventValues.Copy();
                                    kf.eventRandomValues = ObjectEditor.inst.CopiedScaleData.eventRandomValues.Copy();
                                    kf.random = ObjectEditor.inst.CopiedScaleData.random;
                                    kf.relative = ObjectEditor.inst.CopiedScaleData.relative;

                                    ObjectEditor.inst.RenderKeyframes(bm);
                                    ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                    Updater.UpdateObject(bm, "Keyframes");
                                    EditorManager.inst.DisplayNotification("Pasted scale keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                }
                                else
                                    EditorManager.inst.DisplayNotification("Scale keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                            }
                        }
                    });
                }

                GenerateLabels(parent, 32f, "Paste Keyframe data (Rotation)");

                // Rotation
                {
                    var index = CreateInputField("index", "0", "Enter index...", parent, maxValue: int.MaxValue);

                    var pasteAllTypesBase = new GameObject("paste rotation");
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

                    var pasteAllTypesToAllObject = eventButton.Duplicate(pasteAllTypesBaseRT, name);
                    pasteAllTypesToAllObject.transform.localScale = Vector3.one;

                    ((RectTransform)pasteAllTypesToAllObject.transform).sizeDelta = new Vector2(180f, 32f);

                    var pasteAllTypesToAllText = pasteAllTypesToAllObject.transform.GetChild(0).GetComponent<Text>();
                    pasteAllTypesToAllText.text = "Paste to All";

                    EditorThemeManager.AddGraphic(pasteAllTypesToAllObject.GetComponent<Image>(), ThemeGroup.Paste, true);
                    EditorThemeManager.AddGraphic(pasteAllTypesToAllText, ThemeGroup.Paste_Text);

                    var pasteAllTypesToAll = pasteAllTypesToAllObject.GetComponent<Button>();
                    pasteAllTypesToAll.onClick.ClearAll();
                    pasteAllTypesToAll.onClick.AddListener(() =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            if (timelineObject.IsBeatmapObject)
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                if (ObjectEditor.inst.CopiedRotationData != null)
                                    for (int i = 0; i < bm.events[2].Count; i++)
                                    {
                                        var kf = (EventKeyframe)bm.events[2][i];
                                        kf.curveType = ObjectEditor.inst.CopiedRotationData.curveType;
                                        kf.eventValues = ObjectEditor.inst.CopiedRotationData.eventValues.Copy();
                                        kf.eventRandomValues = ObjectEditor.inst.CopiedRotationData.eventRandomValues.Copy();
                                        kf.random = ObjectEditor.inst.CopiedRotationData.random;
                                        kf.relative = ObjectEditor.inst.CopiedRotationData.relative;

                                        ObjectEditor.inst.RenderKeyframes(bm);
                                        ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                        Updater.UpdateObject(bm, "Keyframes");
                                        EditorManager.inst.DisplayNotification("Pasted rotation keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                    }
                                else
                                    EditorManager.inst.DisplayNotification("Rotation keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                            }
                        }
                    });

                    var pasteAllTypesToIndexObject = eventButton.Duplicate(pasteAllTypesBaseRT, name);
                    pasteAllTypesToIndexObject.transform.localScale = Vector3.one;

                    ((RectTransform)pasteAllTypesToIndexObject.transform).sizeDelta = new Vector2(180f, 32f);

                    var pasteAllTypesToIndexText = pasteAllTypesToIndexObject.transform.GetChild(0).GetComponent<Text>();
                    pasteAllTypesToIndexText.text = "Paste to Index";

                    EditorThemeManager.AddGraphic(pasteAllTypesToIndexObject.GetComponent<Image>(), ThemeGroup.Paste, true);
                    EditorThemeManager.AddGraphic(pasteAllTypesToIndexText, ThemeGroup.Paste_Text);

                    var pasteAllTypesToIndex = pasteAllTypesToIndexObject.GetComponent<Button>();
                    pasteAllTypesToIndex.onClick.ClearAll();
                    pasteAllTypesToIndex.onClick.AddListener(() =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            if (timelineObject.IsBeatmapObject && int.TryParse(index.text, out int num))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                if (ObjectEditor.inst.CopiedRotationData != null)
                                {
                                    var kf = (EventKeyframe)bm.events[2][Mathf.Clamp(num, 0, bm.events[2].Count - 1)];
                                    kf.curveType = ObjectEditor.inst.CopiedRotationData.curveType;
                                    kf.eventValues = ObjectEditor.inst.CopiedRotationData.eventValues.Copy();
                                    kf.eventRandomValues = ObjectEditor.inst.CopiedRotationData.eventRandomValues.Copy();
                                    kf.random = ObjectEditor.inst.CopiedRotationData.random;
                                    kf.relative = ObjectEditor.inst.CopiedRotationData.relative;

                                    ObjectEditor.inst.RenderKeyframes(bm);
                                    ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                    Updater.UpdateObject(bm, "Keyframes");
                                    EditorManager.inst.DisplayNotification("Pasted rotation keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                }
                                else
                                    EditorManager.inst.DisplayNotification("Rotation keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                            }
                        }
                    });
                }

                GenerateLabels(parent, 32f, "Paste Keyframe data (Color)");

                // Color
                {
                    var index = CreateInputField("index", "0", "Enter index...", parent, maxValue: int.MaxValue);

                    var pasteAllTypesBase = new GameObject("paste color");
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

                    var pasteAllTypesToAllObject = eventButton.Duplicate(pasteAllTypesBaseRT, name);
                    pasteAllTypesToAllObject.transform.localScale = Vector3.one;

                    ((RectTransform)pasteAllTypesToAllObject.transform).sizeDelta = new Vector2(180f, 32f);

                    var pasteAllTypesToAllText = pasteAllTypesToAllObject.transform.GetChild(0).GetComponent<Text>();
                    pasteAllTypesToAllText.text = "Paste to All";

                    EditorThemeManager.AddGraphic(pasteAllTypesToAllObject.GetComponent<Image>(), ThemeGroup.Paste, true);
                    EditorThemeManager.AddGraphic(pasteAllTypesToAllText, ThemeGroup.Paste_Text);

                    var pasteAllTypesToAll = pasteAllTypesToAllObject.GetComponent<Button>();
                    pasteAllTypesToAll.onClick.ClearAll();
                    pasteAllTypesToAll.onClick.AddListener(() =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            if (timelineObject.IsBeatmapObject)
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                if (ObjectEditor.inst.CopiedColorData != null)
                                    for (int i = 0; i < bm.events[3].Count; i++)
                                    {
                                        var kf = (EventKeyframe)bm.events[3][i];
                                        kf.curveType = ObjectEditor.inst.CopiedColorData.curveType;
                                        kf.eventValues = ObjectEditor.inst.CopiedColorData.eventValues.Copy();
                                        kf.eventRandomValues = ObjectEditor.inst.CopiedColorData.eventRandomValues.Copy();
                                        kf.random = ObjectEditor.inst.CopiedColorData.random;
                                        kf.relative = ObjectEditor.inst.CopiedColorData.relative;

                                        ObjectEditor.inst.RenderKeyframes(bm);
                                        ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                        Updater.UpdateObject(bm, "Keyframes");
                                        EditorManager.inst.DisplayNotification("Pasted color keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                    }
                                else
                                    EditorManager.inst.DisplayNotification("Color keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                            }
                        }
                    });

                    var pasteAllTypesToIndexObject = eventButton.Duplicate(pasteAllTypesBaseRT, name);
                    pasteAllTypesToIndexObject.transform.localScale = Vector3.one;

                    ((RectTransform)pasteAllTypesToIndexObject.transform).sizeDelta = new Vector2(180f, 32f);

                    var pasteAllTypesToIndexText = pasteAllTypesToIndexObject.transform.GetChild(0).GetComponent<Text>();
                    pasteAllTypesToIndexText.text = "Paste to Index";

                    EditorThemeManager.AddGraphic(pasteAllTypesToIndexObject.GetComponent<Image>(), ThemeGroup.Paste, true);
                    EditorThemeManager.AddGraphic(pasteAllTypesToIndexText, ThemeGroup.Paste_Text);

                    var pasteAllTypesToIndex = pasteAllTypesToIndexObject.GetComponent<Button>();
                    pasteAllTypesToIndex.onClick.ClearAll();
                    pasteAllTypesToIndex.onClick.AddListener(() =>
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            if (timelineObject.IsBeatmapObject && int.TryParse(index.text, out int num))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                if (ObjectEditor.inst.CopiedColorData != null)
                                {
                                    var kf = (EventKeyframe)bm.events[3][Mathf.Clamp(num, 0, bm.events[3].Count - 1)];
                                    kf.curveType = ObjectEditor.inst.CopiedColorData.curveType;
                                    kf.eventValues = ObjectEditor.inst.CopiedColorData.eventValues.Copy();
                                    kf.eventRandomValues = ObjectEditor.inst.CopiedColorData.eventRandomValues.Copy();
                                    kf.random = ObjectEditor.inst.CopiedColorData.random;
                                    kf.relative = ObjectEditor.inst.CopiedColorData.relative;

                                    ObjectEditor.inst.RenderKeyframes(bm);
                                    ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                    Updater.UpdateObject(bm, "Keyframes");
                                    EditorManager.inst.DisplayNotification("Pasted color keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                }
                                else
                                    EditorManager.inst.DisplayNotification("Color keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                            }
                        }
                    });
                }
            }

            multiObjectEditorDialog.Find("data").AsRT().sizeDelta = new Vector2(810f, 730.11f);
            multiObjectEditorDialog.Find("data/left").AsRT().sizeDelta = new Vector2(355f, 730f);
        }

        void SyncObjectData(string nameContext, PointerEventData eventData, Action<TimelineObject, BeatmapObject> update, bool renderTimelineObject = false, bool updateObject = true, string updateContext = "")
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                RefreshContextMenu(600f,
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
            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
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
            if (curves.value != 0 && DataManager.inst.AnimationListDictionary.ContainsKey(curves.value - 1))
                kf.curveType = DataManager.inst.AnimationListDictionary[curves.value - 1];
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
            if (curves.value != 0 && DataManager.inst.AnimationListDictionary.ContainsKey(curves.value - 1))
                kf.curveType = DataManager.inst.AnimationListDictionary[curves.value - 1];
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

        public InputFieldStorage GenerateInputField(Transform parent, string name, string defaultValue, string placeholder, bool doMiddle = false, bool doLeftGreater = false, bool doRightGreater = false)
        {
            var gameObject = EditorPrefabHolder.Instance.NumberInputField.Duplicate(parent, name);
            gameObject.transform.localScale = Vector3.one;
            var inputFieldStorage = gameObject.GetComponent<InputFieldStorage>();
            inputFieldStorage.inputField.PlaceholderText().text = placeholder;

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
            inputFieldStorage.inputField.PlaceholderText().text = placeholder;

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

        Document GenerateDocument(string name, string description, List<Document.Element> elements)
        {
            var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(documentationPopup.Content, "Document");
            var documentation = new Document(gameObject, name, description);

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

            GenerateDocument("Introduction", "Welcome to Project Arrhythmia Legacy.", new List<Document.Element>
            {
                new Document.Element("Welcome to <b>Project Arrhythmia</b>!\nWhether you're new to the game, modding or have been around for a while, I'm sure this " +
                        "documentation will help massively in understanding the ins and outs of the editor and the game as a whole.", Document.Element.Type.Text),
                new Document.Element("These documents only list editor features and how to use them, everything else is listed in the Github wiki.", Document.Element.Type.Text),
                new Document.Element("<b>DOCUMENTATION INFO</b>", Document.Element.Type.Text),
                new Document.Element("<b>[VANILLA]</b> represents a feature from original Legacy, with very minor tweaks done to it if any.", Document.Element.Type.Text),
                new Document.Element("<b>[MODDED]</b> represents a feature added by mods. These features will not work in unmodded PA.", Document.Element.Type.Text),
                new Document.Element("<b>[PATCHED]</b> represents a feature modified by mods. They're either in newer versions of PA or are partially modded, meaning they might not work in regular PA.", Document.Element.Type.Text)
            });

            GenerateDocument("Credits", "All the people who helped the mod development in some way.", new List<Document.Element>
            {
                new Document.Element("Reimnop's Catalyst (PA object and animation optimization)", Document.Element.Type.Text),
                new Document.Element("<b>Source code</b>:\nhttps://github.com/Reimnop/Catalyst", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        Application.OpenURL("https://github.com/Reimnop/Catalyst");
                    }
                },

                new Document.Element("", Document.Element.Type.Text),
                new Document.Element("Keijiro Takahashi's KinoGlitch (AnalogGlitch and DigitalGlitch events)", Document.Element.Type.Text),
                new Document.Element("<b>Source code</b>:\nhttps://github.com/keijiro/KinoGlitch", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        Application.OpenURL("https://github.com/keijiro/KinoGlitch");
                    }
                },

                new Document.Element("", Document.Element.Type.Text),
                new Document.Element("WestHillApps' UniBpmAnalyzer", Document.Element.Type.Text),
                new Document.Element("<b>Source code</b>:\nhttps://github.com/WestHillApps/UniBpmAnalyzer", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        Application.OpenURL("https://github.com/WestHillApps/UniBpmAnalyzer");
                    }
                },

                new Document.Element("", Document.Element.Type.Text),
                new Document.Element("Nick Vogt's ColliderCreator (used for creating proper collision for the custom shapes)", Document.Element.Type.Text),
                new Document.Element("<b>Website</b>:\nhttps://www.h3xed.com/", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        Application.OpenURL("https://www.h3xed.com/");
                    }
                },
                new Document.Element("<b>Source code</b>:\nhttps://www.h3xed.com/programming/automatically-create-polygon-collider-2d-from-2d-mesh-in-unity", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        Application.OpenURL("https://www.h3xed.com/programming/automatically-create-polygon-collider-2d-from-2d-mesh-in-unity");
                    }
                },

                new Document.Element("", Document.Element.Type.Text),
                new Document.Element("Crafty Font for the Pixellet font.", Document.Element.Type.Text),
                new Document.Element("<b>Website</b>:\nhttps://craftyfont.gumroad.com/", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        Application.OpenURL("https://craftyfont.gumroad.com/");
                    }
                },

                new Document.Element("", Document.Element.Type.Text),
                new Document.Element("HAWTPIXEL for the File Deletion font.", Document.Element.Type.Text),
                new Document.Element("<b>Website</b>:\nhttps://www.hawtpixel.com/", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        Application.OpenURL("https://www.hawtpixel.com/");
                    }
                },

                new Document.Element("", Document.Element.Type.Text),
                new Document.Element("Sans Sans font.", Document.Element.Type.Text),
                new Document.Element("<b>Website</b>:\nhttps://font.download/font/sans", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        Application.OpenURL("https://font.download/font/sans");
                    }
                },

                new Document.Element("", Document.Element.Type.Text),
                new Document.Element("Fontworks for the RocknRoll font.", Document.Element.Type.Text),
                new Document.Element("<b>Website</b>:\nhttps://github.com/fontworks-fonts/RocknRoll", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        Application.OpenURL("https://github.com/fontworks-fonts/RocknRoll");
                    }
                },

                new Document.Element("", Document.Element.Type.Text),
                new Document.Element("ManiackersDesign for the Monomaniac One font.", Document.Element.Type.Text),
                new Document.Element("<b>Website</b>:\nhttps://github.com/ManiackersDesign/monomaniac", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        Application.OpenURL("https://github.com/ManiackersDesign/monomaniac");
                    }
                },

                new Document.Element("", Document.Element.Type.Text),
                new Document.Element("<b>SPECIAL THANKS</b>", Document.Element.Type.Text),
                new Document.Element("Pidge (developer of the game) - Obviously for making the game itself and inspiring some features in BetterLegacy.", Document.Element.Type.Text),
                new Document.Element("enchart - Massively helped RTMecha get into modding in the first place. Without enchart, none of this would have been possible.", Document.Element.Type.Text),
                new Document.Element("aiden_ytarame - Ported gradient objects from alpha to BetterLegacy.", Document.Element.Type.Text),
                new Document.Element("SleepyzGamer - Helped a lot in finding things", Document.Element.Type.Text),
                new Document.Element("KarasuTori - For motivating RTMecha to keep going and experimenting with modding.", Document.Element.Type.Text),
                new Document.Element("MoNsTeR and CubeCube for testing the mods, reporting bugs and giving suggestions.", Document.Element.Type.Text),
            });

            GenerateDocument("Beatmap Objects", "The very objects that make up Project Arrhythmia levels.", new List<Document.Element>
            {
                new Document.Element("<b>Beatmap Objects</b> are the objects people use to create a variety of things for their levels. " +
                        "Whether it be backgrounds, characters, attacks, you name it! Below is a list of data Beatmap Objects have.", Document.Element.Type.Text),
                new Document.Element("<b>ID [PATCHED]</b>\nThe ID is used for specifying a Beatmap Object, otherwise it'd most likely get lost in a sea of other objects! " +
                        "It's mostly used with parenting. This is patched because in unmodded PA, creators aren't able to see the ID of an object unless they look at the level.lsb.\n" +
                        "Clicking on the ID will copy it to your clipboard.", Document.Element.Type.Text),
                new Document.Element("<b>LDM (Low Detail Mode) [MODDED]</b>\nLDM is useful for having objects not render for lower end devices. If the option is on and the user has " +
                        "Low Detail Mode enabled through the RTFunctions mod config, the Beatmap Object will not render.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_id_ldm.png", Document.Element.Type.Image),
                new Document.Element("<b>Name [VANILLA]</b>\nNaming an object is incredibly helpful for readablility and knowing what an object does at a glance. " +
                        "Clicking your scroll wheel over it will flip any left / right.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_name_type.png", Document.Element.Type.Image),
                new Document.Element("<b>Tags [MODDED]</b>\nBeing able to group objects together or even specify things about an object is possible with Object Tags. This feature " +
                        "is mostly used by modifiers, but can be used in other ways such as a \"DontRotate\" tag which prevents Player Shapes from rotating automatically.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_tags.png", Document.Element.Type.Image),
                new Document.Element("<b>Locked [PATCHED]</b>\nIf on, prevents Beatmap Objects' start time from being changed. It's patched because unmodded PA doesn't " +
                        "have the toggle UI for this, however you can still use it in unmodded PA via hitting Ctrl + L.", Document.Element.Type.Text),
                new Document.Element("<b>Start Time [VANILLA]</b>\nUsed for when the Beatmap Object spawns.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_start_time.png", Document.Element.Type.Image),
                new Document.Element("<b>Time of Death [VANILLA]</b>\nUsed for when the Beatmap Object despawns." +
                        "\n<b>[PATCHED]</b> No Autokill - Beatmap Objects never despawn. This option is viable in modded PA due to heavily optimized object code, so don't worry " +
                        "about having a couple of objects with this. Just make sure to only use this when necessary, like for backgrounds or a persistent character." +
                        "\n<b>[VANILLA]</b> Last KF - Beatmap Objects despawn once all animations are finished. This does NOT include parent animations. When the level " +
                        "time reaches after the last keyframe, the object despawns." +
                        "\n<b>[VANILLA]</b> Last KF Offset - Same as above but at an offset." +
                        "\n<b>[VANILLA]</b> Fixed Time - Beatmap Objects despawn at a fixed time, regardless of animations. Fixed time is Beatmap Objects Start Time with an offset added to it." +
                        "\n<b>[VANILLA]</b> Song Time - Same as above, except it ignores the Beatmap Object Start Time, despawning the object at song time.", Document.Element.Type.Text),
                new Document.Element("<b>Collapse [VANILLA]</b>\nBeatmap Objects in the editor timeline have their length shortened to the smallest amount if this is on.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_tod.png", Document.Element.Type.Image),
                new Document.Element("<b>Parent Search [PATCHED]</b>\nHere you can search for an object to parent the Beatmap Object to. It includes Camera Parenting.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_parent_search.png", Document.Element.Type.Image),
                new Document.Element("<b>Camera Parent [MODDED]</b>\nBeatmap Objects parented to the camera will always follow it, depending on the parent settings. This includes " +
                        "anything that makes the camera follow the player. This feature does exist in modern PA, but doesn't work the same way this does.", Document.Element.Type.Text),
                new Document.Element("<b>Clear Parent [MODDED]</b>\nClicking this will remove the Beatmap Object from its parent.", Document.Element.Type.Text),
                new Document.Element("<b>Parent Picker [MODDED]</b>\nClicking this will activate a dropper. Right clicking will deactivate the dropper. Clicking on an object " +
                        "in the timeline will set the current selected Beatmap Objects parent to the selected Timeline Object.", Document.Element.Type.Text),
                new Document.Element("<b>Parent Display [VANILLA]</b>\nShows what the Beatmap Object is parented to. Clicking this button selects the parent. " +
                        "Hovering your mouse over it shows parent chain info in the Hover Info box.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_parent.png", Document.Element.Type.Image),
                new Document.Element("<b>Parent Settings [PATCHED]</b>\nParent settings can be adjusted here. Each of the below settings refer to both " +
                        "position / scale / rotation. Position, scale and rotation are the rows and the types of Parent Settings are the columns.", Document.Element.Type.Text),
                new Document.Element("<b>Parent Type [VANILLA]</b>\nWhether the Beatmap Object applies this type of animation from the parent. " +
                        "It is the first column in the Parent Settings UI.", Document.Element.Type.Text),
                new Document.Element("<b>Parent Offset [VANILLA]</b>\nParent animations applied to the Beatmap Objects own parent chain get delayed at this offset. Normally, only " +
                        "the objects current parent gets delayed. It is the second column in the Parent Settings UI.", Document.Element.Type.Text),
                new Document.Element("<b>Parent Additive [MODDED]</b>\nForces Parent Offset to apply to every parent chain connected to the Beatmap Object. With this off, it only " +
                        "uses the Beatmap Objects' current parent. For example, say we have objects A, B, C and D. With this on, D delays the animation of every parent. With this off, it delays only C. " +
                        "It is the third column in the Parent Settings UI.", Document.Element.Type.Text),
                new Document.Element("<b>Parent Parallax [MODDED]</b>\nParent animations are multiplied by this amount, allowing for a parallax effect. Say the amount was 2 and the parent " +
                        "moves to position X 20, the object would move to 40 due to it being multiplied by 2. " +
                        "It is the fourth column in the Parent Settings UI.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_parent_more.png", Document.Element.Type.Image),
                new Document.Element("<b>Origin [PATCHED]</b>\nOrigin is the offset applied to the visual of the Beatmap Object. Only usable for non-Empty object types. " +
                        "It's patched because of the number input fields instead of the direction buttons.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_origin.png", Document.Element.Type.Image),
                new Document.Element("<b>Shape [PATCHED]</b>\nShape is whatever the visual of the Beatmap Object displays as. This doesn't just include actual shapes but stuff " +
                        "like text, images and player models too. More shape types and options were added. Unmodded PA does not include Image Shape, Pentagon Shape, Misc Shape, Player Shape.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_shape.png", Document.Element.Type.Image),
                new Document.Element("<b>Render Depth [PATCHED]</b>\nDepth is how deep an object is in visual layers. Higher amount of Render Depth means the object is lower " +
                        "in the layers. Unmodded PA Legacy allows from 219 to -98. PA Alpha only allows from 40 to 0. Player is located at -60 depth. Z Axis Position keyframes use depth as a " +
                        "multiplied offset.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_depth.png", Document.Element.Type.Image),
                new Document.Element("<b>Render Type [MODDED]</b>\nRender Type is if the visual of the Beatmap Object renders in the 2D layer or the 3D layer, aka Foreground / Background.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_render_type.png", Document.Element.Type.Image),
                new Document.Element("<b>Layer [PATCHED]</b>\nLayer is what editor layer the Beatmap Object renders on. It can go as high as 2147483646. " +
                        "In unmodded PA its limited from layers 1 to 5, though in PA Editor Alpha another layer was introduced.", Document.Element.Type.Text),
                new Document.Element("<b>Bin [VANILLA]</b>\nBin is what row of the timeline the Beatmap Objects' timeline object renders on.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_editordata.png", Document.Element.Type.Image),
                new Document.Element("<b>Object Debug [MODDED]</b>\nThis UI element only generates if UnityExplorer is installed. If it is, clicking on either button will inspect " +
                        "the internal data of the respective item.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_object_debug.png", Document.Element.Type.Image),
                new Document.Element("<b>Integer Variable [MODDED]</b>\nEvery object has a whole number stored that Modifiers can use.", Document.Element.Type.Text),
                new Document.Element("<b>Modifiers [MODDED]</b>\nModifiers are made up of two different types: Triggers and Actions. " +
                            "Triggers check if a specified thing is happening and Actions do things depending on if any triggers are active or there aren't any. A detailed description of every modifier " +
                            "can be found in the Modifiers documentation. [WIP]", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_object_modifiers_edit.png", Document.Element.Type.Image),
            });

            GenerateDocument("Beatmap Object Keyframes (WIP)", "The things that animate objects in different ways.", new List<Document.Element>
            {
                new Document.Element("The keyframes in the Beatmap Objects' keyframe timeline allow animating several aspects of a Beatmap Objects' visual.", Document.Element.Type.Text),
                new Document.Element("<b>POSITION [PATCHED]</b>", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_pos_none.png", Document.Element.Type.Image),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_pos_normal.png", Document.Element.Type.Image),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_pos_toggle.png", Document.Element.Type.Image),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_pos_scale.png", Document.Element.Type.Image),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_pos_static_homing.png", Document.Element.Type.Image),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_pos_dynamic_homing.png", Document.Element.Type.Image),
                new Document.Element("<b>SCALE [VANILLA]</b>", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_sca_none.png", Document.Element.Type.Image),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_sca_normal.png", Document.Element.Type.Image),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_sca_toggle.png", Document.Element.Type.Image),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_sca_scale.png", Document.Element.Type.Image),
                new Document.Element("<b>ROTATION [VANILLA]</b>", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_rot_none.png", Document.Element.Type.Image),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_rot_normal.png", Document.Element.Type.Image),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_rot_toggle.png", Document.Element.Type.Image),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_rot_static_homing.png", Document.Element.Type.Image),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_rot_dynamic_homing.png", Document.Element.Type.Image),
                new Document.Element("<b>COLOR [PATCHED]</b>", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_col_none.png", Document.Element.Type.Image),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_col_dynamic_homing.png", Document.Element.Type.Image),
            });

            GenerateDocument("Prefabs", "A package of objects that can be transfered from level to level. They can also be added to the level as a Prefab Object.", new List<Document.Element>
            {
                    new Document.Element("Prefabs are collections of objects grouped together for easy transfering from level to level.", Document.Element.Type.Text),
                    new Document.Element("<b>Name [VANILLA]</b>\nThe name of the Prefab. External prefabs gets saved with this as its file name, but all lowercase and " +
                        "spaces replaced with underscores.", Document.Element.Type.Text),
                    new Document.Element("BepInEx/plugins/Assets/Documentation/doc_pc_name.png", Document.Element.Type.Image),
                    new Document.Element("<b>Offset [VANILLA]</b>\nThe delay set to every Prefab Objects' spawned objects related to this Prefab.", Document.Element.Type.Text),
                    new Document.Element("BepInEx/plugins/Assets/Documentation/doc_pc_offset.png", Document.Element.Type.Image),
                    new Document.Element("<b>Type [PATCHED]</b>\nThe group name and color of the Prefab. Good for color coding what a Prefab does at a glance.", Document.Element.Type.Text),
                    new Document.Element("BepInEx/plugins/Assets/Documentation/doc_pc_type.png", Document.Element.Type.Image),
                    new Document.Element("<b>Description [MODDED]</b>\nA good way to tell you and others what the Prefab does or contains in great detail.", Document.Element.Type.Text),
                    new Document.Element("BepInEx/plugins/Assets/Documentation/doc_pc_description.png", Document.Element.Type.Image),
                    new Document.Element("<b>Seletion List [PATCHED]</b>\nShows every object, you can toggle the selection on any of them to add them to the prefab. All selected " +
                        "objects will be copied into the Prefab. This is patched because the UI and the code for it already existed in Legacy, it was just unused.", Document.Element.Type.Text),
                    new Document.Element("BepInEx/plugins/Assets/Documentation/doc_pc_search.png", Document.Element.Type.Image),
                    new Document.Element("<b>Create [MODDED]</b>\nApplies all data and copies all selected objects to a new Prefab.", Document.Element.Type.Text),
                    new Document.Element("BepInEx/plugins/Assets/Documentation/doc_pc_create.png", Document.Element.Type.Image),

            });

            GenerateDocument("Prefab Objects", "Individual instances of prefabs that spawn the packed objects at specified offsets.", new List<Document.Element>
            {
                new Document.Element("Prefab Objects are a copied version of the original prefab, placed into the level. They take all the objects stored in the original prefab " +
                    "and add them to the level, meaning you can have multiple copies of the same group of objects. Editing the objects of the prefab by expanding it applies all changes to " +
                    "the prefab, updating every Prefab Object (once collapsed back into a Prefab Object).", Document.Element.Type.Text),
                new Document.Element("<b>Expand [VANILLA]</b>\nExpands all the objects contained within the original prefab into the level and deletes the Prefab Object.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_expand.png", Document.Element.Type.Image),
                new Document.Element("<b>Layer [PATCHED]</b>\nWhat Editor Layer the Prefab Object displays on. Can go from 1 to 2147483646. In unmodded Legacy its 1 to 5.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_layer.png", Document.Element.Type.Image),
                new Document.Element("<b>Time of Death [MODDED]</b>\nTime of Death allows every object spawned from the Prefab Object still alive at a certain point to despawn." +
                    "\nRegular - Just how the game handles Prefab Objects kill time normally." +
                    "\nStart Offset - Kill time is offset plus the Prefab Object start time." +
                    "\nSong Time - Kill time is song time, so no matter where you change the start time to the kill time remains the same.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_tod.png", Document.Element.Type.Image),
                new Document.Element("<b>Locked [PATCHED]</b>\nIf on, prevents Prefab Objects' start time from being changed. It's patched because unmodded PA doesn't " +
                    "have the toggle UI for this, however you can still use it in unmodded PA via hitting Ctrl + L.", Document.Element.Type.Text),
                new Document.Element("<b>Collapse [PATCHED]</b>\nIf on, collapses the Prefab Objects' timeline object. This is patched because it literally doesn't " +
                    "work in unmodded PA.", Document.Element.Type.Text),
                new Document.Element("<b>Start Time [VANILLA]</b>\nWhere the objects spawned from the Prefab Object start.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_time.png", Document.Element.Type.Image),
                new Document.Element("<b>Position Offset [PATCHED]</b>\nEvery objects' top-most-parent has its position set to this offset. Unmodded PA technically has this " +
                    "feature, but it's not editable in the editor.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_pos_offset.png", Document.Element.Type.Image),
                new Document.Element("<b>Scale Offset [PATCHED]</b>\nEvery objects' top-most-parent has its scale set to this offset. Unmodded PA technically has this " +
                    "feature, but it's not editable in the editor.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_sca_offset.png", Document.Element.Type.Image),
                new Document.Element("<b>Rotation Offset [PATCHED]</b>\nEvery objects' top-most-parent has its rotation set to this offset. Unmodded PA technically has this " +
                    "feature, but it's not editable in the editor.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_rot_offset.png", Document.Element.Type.Image),
                new Document.Element("<b>Repeat [MODDED]</b>\nWhen spawning the objects from the Prefab Object, every object gets repeated a set amount of times" +
                    "with their start offset added onto each time they repeat depending on the Repeat Offset Time set. The data for Repeat Count and Repeat Offset Time " +
                    "already existed in unmodded PA, it just went completely unused.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_repeat.png", Document.Element.Type.Image),
                new Document.Element("<b>Speed [MODDED]</b>\nHow fast each object spawned from the Prefab Object spawns and is animated.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_speed.png", Document.Element.Type.Image),
                new Document.Element("<b>Lead Time / Offset [VANILLA]</b>\nEvery Prefab Object starts at an added offset from the Offset amount. I have no idea why " +
                    "it's called Lead Time here even though its Offset everywhere else.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_lead.png", Document.Element.Type.Image),
                new Document.Element("<b>Name [MODDED]</b>\nChanges the name of the original Prefab related to the Prefab Object. This is modded because you couldn't " +
                    "change this in the Prefab Object editor.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_name.png", Document.Element.Type.Image),
                new Document.Element("<b>Type [MODDED]</b>\nChanges the Type of the original Prefab related to the Prefab Object. This is modded because you couldn't " +
                    "change this in the Prefab Object editor. (You can scroll-wheel over the input field to change the type easily)", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_type.png", Document.Element.Type.Image),
                new Document.Element("<b>Save [MODDED]</b>\nSaves all changes made to the original Prefab to any External Prefab with a matching name.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_save.png", Document.Element.Type.Image),
                new Document.Element("<b>Count [MODDED]</b>\nTells how many objects are in the original Prefab and how many Prefab Objects there are in the timeline " +
                    "for the Prefab. The Prefab Object Count goes unused for now...", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_counts.png", Document.Element.Type.Image),
            });

            GenerateDocument("Background Objects (WIP)", "The classic 3D style backgrounds.", new List<Document.Element>
            {
                new Document.Element("Background Object intro.", Document.Element.Type.Text),
            });

            GenerateDocument("Events (WIP)", "Effects to make your level pretty.", new List<Document.Element>
            {
                new Document.Element("Events intro.", Document.Element.Type.Text),
            });

            GenerateDocument("Text Objects", "Flavor your levels with text!", new List<Document.Element>
            {
                new Document.Element("Text Objects can be used in extensive ways, from conveying character dialogue to decoration. This document is for showcasing usable " +
                    "fonts and formats Text Objects can use. Also do note to ignore the spaces in the formattings as the UI text will just make the text like <b>this</b>.", Document.Element.Type.Text),
                new Document.Element("<b>- FORMATTING -</b>", Document.Element.Type.Text),
                new Document.Element("<b>[VANILLA]</b> < b> - For making text <b>BOLD</b>. Use </ b> to clear.", Document.Element.Type.Text),
                new Document.Element("<b>[VANILLA]</b> < i> - For making text <i>italic</i>. Use </ i> to clear.", Document.Element.Type.Text),
                new Document.Element("<b>- FONTS -</b>", Document.Element.Type.Text),
                new Document.Element(RTFile.BepInExAssetsPath + "Documentation/doc_fonts.png", Document.Element.Type.Image)
                { Function = () => { RTFile.OpenInFileBrowser.OpenFile(RTFile.ApplicationDirectory + RTFile.BepInExAssetsPath + "Documentation/doc_fonts.png"); }},
                new Document.Element("To use a font, do <font=Font Name>. To clear, do </font>. Click on one of the fonts below to copy the <font=Font Name> to your clipboard. " +
                    "Click on the image above to open the folder to the documentation assets folder where a higher resolution screenshot is located.", Document.Element.Type.Text),
                new Document.Element("<b>[MODDED]</b> Adam Warren Pro Bold - A comic style font.", Document.Element.Type.Text)
                { Function = () => {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard("<font=Adam Warren Pro Bold>");
                }},
                new Document.Element("<b>[MODDED]</b> Adam Warren Pro BoldItalic - A comic style font.", Document.Element.Type.Text)
                { Function = () => {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard("<font=Adam Warren Pro BoldItalic>");
                }},
                new Document.Element("<b>[MODDED]</b> Adam Warren Pro - A comic style font.", Document.Element.Type.Text)
                { Function = () => {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard("<font=Adam Warren Pro>");
                }},
                new Document.Element("<b>[MODDED]</b> Arrhythmia - The font from the earliest builds of Project Arrhythmia.", Document.Element.Type.Text)
                { Function = () => {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard("<font=Arrhythmia>");
                }},
                new Document.Element("<b>[MODDED]</b> BadaBoom BB - A comic style font.", Document.Element.Type.Text)
                { Function = () => {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard("<font=BadaBoom BB>");
                }},
                new Document.Element("<b>[MODDED]</b> Matoran Language 1 - The language used by the Matoran in the BIONICLE series.", Document.Element.Type.Text)
                { Function = () => {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard("<font=Matoran Language 1>");
                }},
                new Document.Element("<b>[MODDED]</b> Matoran Language 2 - The language used by the Matoran in the BIONICLE series.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard("<font=Matoran Language 2>");
                }},
                new Document.Element("<b>[MODDED]</b> Determination Mono - The font UNDERTALE/deltarune uses for its interfaces.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard("<font=Determination Mono>");
                }},
                new Document.Element("<b>[MODDED]</b> determination sans - sans undertale.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard("<font=determination sans>");
                }},
                new Document.Element($"<b>[MODDED]</b> Determination Wingdings - Beware the man who speaks in hands.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard($"<font=Determination Wingdings>");
                }},
                new Document.Element("<b>[MODDED]</b> Flow Circular - A fun line font suggested by ManIsLiS.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard("<font=Flow Circular>");
                }},
                new Document.Element("<b>[MODDED]</b> Fredoka One - The font from the Vitamin Games website.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard("<font=Fredoka One>");
                }},
                new Document.Element("<b>[MODDED]</b> Ancient Autobot - The launguage used by ancient Autobots in the original Transformers cartoon.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard("<font=Ancient Autobot>");
                }},
                new Document.Element("<b>[MODDED]</b> Hachicro - The font used by UNDERTALE's hit text.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard("<font=Hachicro>");
                }},
                new Document.Element($"<b>[MODDED]</b> Inconsolata Variable - The default PA font.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard($"<font=Inconsolata Variable>");
                }},
                new Document.Element($"<b>[VANILLA]</b> LiberationSans SDF - An extra font unmodded Legacy has.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard($"<font=LiberationSans SDF>");
                }},
                new Document.Element($"<b>[MODDED]</b> Komika Hand - A comic style font.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard($"<font=font>");
                }},
                new Document.Element($"<b>[MODDED]</b> Komika Hand Bold - A comic style font.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard($"<font=Komika Hand Bold>");
                }},
                new Document.Element($"<b>[MODDED]</b> Komika Slick - A comic style font.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard($"<font=Komika Slick>");
                }},
                new Document.Element($"<b>[MODDED]</b> Komika Slim - A comic style font.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard($"<font=Komika Slim>");
                }},
                new Document.Element($"<b>[MODDED]</b> Komika Hand BoldItalic - A comic style font.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard($"<font=Komika Hand BoldItalic>");
                }},
                new Document.Element($"<b>[MODDED]</b> Komika Hand Italic - A comic style font.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard($"<font=Komika Hand Italic>");
                }},
                new Document.Element($"<b>[MODDED]</b> Komika Jam - A comic style font.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard($"<font=Komika Jam>");
                }},
                new Document.Element($"<b>[MODDED]</b> Komika Jam Italic - A comic style font.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard($"<font=Komika Jam Italic>");
                }},
                new Document.Element($"<b>[MODDED]</b> Komika Slick Italic - A comic style font.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard($"<font=Komika Slick Italic>");
                }},
                new Document.Element($"<b>[MODDED]</b> Komika Slim Italic - A comic style font.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard($"<font=Komika Slim Italic>");
                }},
                new Document.Element($"<b>[MODDED]</b> Minecraft Text Bold - The font used for the text UI in Minecraft.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard($"<font=Minecraft Text Bold>");
                }},
                new Document.Element($"<b>[MODDED]</b> font - The font used for the text UI in Minecraft.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied Minecraft Text BoldItalic!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard($"<font=Minecraft Text BoldItalic>");
                }},
                new Document.Element($"<b>[MODDED]</b> Minecraft Text Italic - The font used for the text UI in Minecraft.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard($"<font=Minecraft Text Italic>");
                }},
                new Document.Element($"<b>[MODDED]</b> Minecraft Text - The font used for the text UI in Minecraft.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard($"<font=Minecraft Text>");
                }},
                new Document.Element($"<b>[MODDED]</b> Minecraftory - Geometry Dash font mainly used in Geometry Dash SubZero.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard($"<font=Minecraftory>");
                }},
                new Document.Element($"<b>[MODDED]</b> Monster Friend Back - A font based on UNDERTALE's title.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard($"<font=Monster Friend Back>");
                }},
                new Document.Element($"<b>[MODDED]</b> Monster Friend Fore - A font based on UNDERTALE's title.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard($"<font=Monster Friend Fore>");
                }},
                new Document.Element($"<b>[MODDED]</b> About Friend - A font suggested by Ama.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard($"<font=About Friend>");
                }},
                new Document.Element("<b>[MODDED]</b> Oxygene - The font from the title of Geometry Dash.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard("<font=Oxygene>");
                }},
                new Document.Element($"<b>[MODDED]</b> Piraka Theory - The language used by the Piraka in the BIONICLE series.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard($"<font=Piraka Theory>");
                }},
                new Document.Element($"<b>[MODDED]</b> Piraka - The language used by the Piraka in the BIONICLE series.", Document.Element.Type.Text)
                { Function = () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard($"<font=Piraka>");
                }},
                new Document.Element("<b>[MODDED]</b> Pusab - The font from the hit game Geometry Dash. And yes, it is the right one.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard("<font=Pusab>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Rahkshi - The font used for promoting the Rahkshi sets in the BIONICLE series.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Rahkshi>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Revue - The font used early 2000s Transformers titles.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Revue>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Transdings - A font that contains a ton of Transformer insignias / logos. Below is an image featuring each letter " +
                    $"of the alphabet.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Transdings>");
                    }
                },
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_tf.png", Document.Element.Type.Image),
                new Document.Element($"<b>[MODDED]</b> Transformers Movie - A font based on the Transformers movies title font.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Transformers Movie>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Nexa Book - A font suggested by CubeCube.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Nexa Book>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Nexa Bold - A font suggested by CubeCube.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Nexa Bold>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Angsana - A font suggested by KarasuTori. Supports non-English languages like Thai.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Angsana>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Angsana Bold - A font suggested by KarasuTori. Supports non-English languages like Thai.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Angsana Bold>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Angsana Italic - A font suggested by KarasuTori. Supports non-English languages like Thai.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Angsana Italic>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Angsana Bold Italic - A font suggested by KarasuTori. Supports non-English languages like Thai.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Angsana Bold Italic>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> VAG Rounded - A font suggested by KarasuTori. Supports non-English languages like Russian.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=VAG Rounded>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Comic Sans - You know the font.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Comic Sans>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Comic Sans Bold - You know the font.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Comic Sans Bold>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Comic Sans Hairline - You know the font.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Comic Sans Hairline>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Comic Sans Light - You know the font.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Comic Sans Light>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Pixellet - Neat pixel font that supports Thai.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Pixellet>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> File Deletion - A font pretty similar to the current PA title font.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=File Deletion>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Sans Sans - Sans Sans.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Sans Sans>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Monomaniac One - Japanese support font.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Monomaniac One>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> RocknRoll One - Japanese support font.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=RocknRoll One>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Roboto Mono Bold - Russian support font.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Roboto Mono Bold>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Roboto Mono Italic - Russian support font.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Roboto Mono Italic>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Roboto Mono Light - Russian support font.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Roboto Mono Light>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Roboto Mono Light 1 - Russian support font.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Roboto Mono Light 1>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Roboto Mono Light Italic - Russian support font.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Roboto Mono Light Italic>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Roboto Mono Light Italic 1 - Russian support font.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Roboto Mono Light Italic 1>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Roboto Mono Thin - Russian support font.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Roboto Mono Thin>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Roboto Mono Thin 1 - Russian support font.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Roboto Mono Thin 1>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Roboto Mono Thin Italic - Russian support font.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Roboto Mono Thin Italic>");
                    }
                },
                new Document.Element($"<b>[MODDED]</b> Roboto Mono Thin Italic 1 - Russian support font.", Document.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font=Roboto Mono Thin Italic 1>");
                    }
                },
            });

            GenerateDocument("Markers", "Organize and remember details about a level.", new List<Document.Element>
            {
                new Document.Element("Markers can organize certain parts of your level or help with aligning objects to a specific time.", Document.Element.Type.Text),
                new Document.Element("In the image below is two types of markers. The blue marker is the Audio Marker and the marker with a circle on the top is just a Marker. " +
                    "Left clicking on the Marker's circle knob moves the Audio Marker to the regular Marker. Right clicking the Marker's circle knob deletes it.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_marker_timeline.png", Document.Element.Type.Image),
                new Document.Element("<b>Name [VANILLA]</b>\nThe name of the Marker. This renders next to the Marker's circle knob in the timeline.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_marker_name.png", Document.Element.Type.Image),
                new Document.Element("<b>Time [VANILLA]</b>\nThe time the Marker renders at in the timeline.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_marker_time.png", Document.Element.Type.Image),
                new Document.Element("<b>Description [PATCHED]</b>\nDescription helps you remember details about specific parts of a song or even stuff about the level you're " +
                    "editing. Typing setLayer(1) will set the editor layer to 1 when the Marker is selected. You can also have it be setLayer(events), setLayer(objects), setLayer(toggle), which " +
                    "sets the layer type to those respective types (toggle switches between Events and Objects layer types). Fun fact, the title for description in the UI in unmodded Legacy " +
                    "said \"Name\" lol.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_marker_description.png", Document.Element.Type.Image),
                new Document.Element("<b>Colors [PATCHED]</b>\nWhat color the marker displays as. You can customize the colors in the Settings window.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_marker_colors.png", Document.Element.Type.Image),
                new Document.Element("<b>Index [MODDED]</b>\nThe number of the Marker in the list.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_marker_index.png", Document.Element.Type.Image),
                new Document.Element("On the right-hand-side of the Marker Editor window is a list of markers. At the top is a Search field and a Delete Markers button. " +
                    "Delete Markers clears every marker in the level and closes the Marker Editor.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_marker_delete.png", Document.Element.Type.Image),
            });

            GenerateDocument("Title Bar", "The thing at the top of the editor UI with dropdowns.", new List<Document.Element>
            {
                new Document.Element("Title Bar has the main functions for loading, saving and editing.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_td.png", Document.Element.Type.Image),
                new Document.Element("<b>File [PATCHED]</b>" +
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
                    "\n<b>[VANILLA]</b> Quit Game - Quits the game.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_td_file.png", Document.Element.Type.Image),
                new Document.Element("<b>Edit [PATCHED]</b>" +
                    "\nHow far can you edit in a modded editor?" +
                    "\n<b>[PATCHED]</b> Undo - Undoes the most recent action. Still heavily WIP. (sorry)" +
                    "\n<b>[PATCHED]</b> Redo - Same as above but goes back to the recent action when undone." +
                    "\n<b>[MODDED]</b> Search Objects - Search for specific objects by name or index. Hold Left Control to take yourself to the object in the timeline." +
                    "\n<b>[MODDED]</b> Preferences - Modify editor specific mod configs directly in the editor. Also known as Editor Properties." +
                    "\n<b>[MODDED]</b> Player Editor - Only shows if you have CreativePlayers installed. Opens the Player Editor." +
                    "\n<b>[MODDED]</b> View Keybinds - Customize the keybinds of the editor in any way you want.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_td_edit.png", Document.Element.Type.Image),
                new Document.Element("<b>View [PATCHED]</b>" +
                    "\nView specific things." +
                    "\n<b>[MODDED]</b> Get Example - Only shows if you have ExampleCompanion installed. It summons Example to the scene." +
                    "\n<b>[VANILLA]</b> Show Help - Toggles the Info box.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_td_view.png", Document.Element.Type.Image),
                new Document.Element("<b>Steam [VANILLA]</b>" +
                    "\nView Steam related things... even though modded PA doesn't use Steam anymore lol" +
                    "\n<b>[VANILLA]</b> Open Workshop - Opens a link to the Steam workshop." +
                    "\n<b>[VANILLA]</b> Publish / Update Level - Opens the Metadata Editor / Level Uploader.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_td_steam.png", Document.Element.Type.Image),
                new Document.Element("<b>Help [PATCHED]</b>" +
                    "\nGet some help." +
                    "\n<b>[MODDED]</b> Modder's Discord - Opens a link to the mod creator's Discord server." +
                    "\n<b>[MODDED]</b> Watch PA History - Since there are no <i>modded</i> guides yet, this just takes you to the System Error BTS PA History playlist." +
                    "\n<b>[MODDED]</b> Wiki / Documentation - In-editor documentation of everything the game has to offer. You're reading it right now!", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_td_help.png", Document.Element.Type.Image),
            });

            GenerateDocument("Timeline Bar", "The main toolbar used for editing main editor things such as audio time, editor layers, etc.", new List<Document.Element>
            {
                new Document.Element("The Timeline Bar is where you can see and edit general game and editor info.", Document.Element.Type.Text),
                new Document.Element("<b>Audio Time (Precise) [MODDED]</b>\nText shows the precise audio time. This can be edited to set a specific time for the audio.", Document.Element.Type.Text),
                new Document.Element("<b>Audio Time (Formatted) [VANILLA]</b>\nText shows the audio time formatted like \"minutes.seconds.milliseconds\". Clicking this sets the " +
                    "audio time to 0.", Document.Element.Type.Text),
                new Document.Element("<b>Pause / Play [VANILLA]</b>\nPressing this toggles if the song is playing or not.", Document.Element.Type.Text),
                new Document.Element("<b>Pitch [PATCHED]</b>\nThe speed of the song. Clicking the buttons adjust the pitch by 0.1, depending on the direction the button is facing.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_tb_audio.png", Document.Element.Type.Image),
                new Document.Element("<b>Editor Layer [PATCHED]</b>\nEditor Layer is what objects show in the timeline, depending on their own Editor Layer. " +
                    "It can go as high as 2147483646. In unmodded PA its limited from layers 1 to 5, though in PA Editor Alpha another layer was introduced.", Document.Element.Type.Text),
                new Document.Element("<b>Editor Layer Type [MODDED]</b>\nWhether the timeline shows objects or event keyframes / checkpoints.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_tb_layer.png", Document.Element.Type.Image),
                new Document.Element("<b>Prefab [VANILLA]</b>\nOpens the Prefab list popups (Internal & External).", Document.Element.Type.Text),
                new Document.Element("<b>Object [PATCHED]</b>\nOpens a popup featuring different object templates such as Decoration, Empty, etc. It's patched because " +
                    "Persistent was replaced with No Autokill.", Document.Element.Type.Text),
                new Document.Element("<b>Marker [VANILLA]</b>\nCreates a Marker.", Document.Element.Type.Text),
                new Document.Element("<b>BG [VANILLA]</b>\nOpens a popup to open the BG editor or create a new BG.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_tb_create.png", Document.Element.Type.Image),
                new Document.Element("<b>Preview Mode [VANILLA]</b>\nSwitches the game to Preview Mode.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_tb_preview_mode.png", Document.Element.Type.Image),
            });

            GenerateDocument("Keybinds (WIP)", "Perform specific actions when pressing set keys.", new List<Document.Element>
            {
                new Document.Element("Keybinds intro.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_keybind_list.png", Document.Element.Type.Image),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_keybind_editor.png", Document.Element.Type.Image),
            });

            GenerateDocument("Editor Properties (WIP)", "Configure the editor!", new List<Document.Element>
            {
                new Document.Element("Editor Properties intro.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_editor_properties.png", Document.Element.Type.Image),
            });

            GenerateDocument("Misc", "The stuff that didn't fit in a document of its own.", new List<Document.Element>
            {
                new Document.Element("<b>Editor Level Path [MODDED]</b>\nThe path within the Project Arrhythmia/beatmaps directory that is used for the editor level list.", Document.Element.Type.Text),
                new Document.Element("<b>Refresh [MODDED]</b>\nRefreshes the editor level list.", Document.Element.Type.Text),
                new Document.Element("<b>Descending [MODDED]</b>\nIf the editor level list should be descending or ascending.", Document.Element.Type.Text),
                new Document.Element("<b>Order [MODDED]</b>\nHow the editor level list should be ordered." +
                    "\nCover - Order by if the level has a cover or not." +
                    "\nArtist - Order by Artist Name." +
                    "\nCreator - Order by Creator Name." +
                    "\nFolder - Order by Folder Name." +
                    "\nTitle - Order by Song Title." +
                    "\nDifficulty - Order by (Easy, Normal, Hard, Expert, Expert+, Master, Animation)" +
                    "\nDate Edited - Order by last saved time, so recently edited levels appear at one side and older levels appear at the other.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_open_level_top.png", Document.Element.Type.Image),
                new Document.Element("<b>Loading Autosaves [MODDED]</b>\nHolding shift when you click on a level in the level list will open an Autosave popup instead of " +
                    "loading the level. This allows you to load any autosaved file so you don't need to go into the level folder and change one of the autosaves to the level.lsb.", Document.Element.Type.Text),
                new Document.Element("BepInEx/plugins/Assets/Documentation/doc_autosaves.png", Document.Element.Type.Image),
            });

            GenerateDocument("Object Modifiers", "Make your levels dynamic!", new List<Document.Element>
            {
                new Document.Element("ObjectModifiers adds a trigger / action based system to Beatmap Objects called \"Modifiers\". " +
                    "Modifiers have two types: Triggers check if something is happening and if it is, it activates any Action type modifiers. If there are no Triggers, then the Action modifiers " +
                    "activates. This document is heavily WIP and will be added to over time.", Document.Element.Type.Text),
                new Document.Element("<b>setPitch</b> - Modifies the speed of the game and the pitch of the audio. It sets a multiplied offset from the " +
                    "audio keyframe's pitch value. However unlike the event keyframe, setPitch can go into the negatives allowing for reversed audio.", Document.Element.Type.Text),
                new Document.Element("<b>addPitch</b> - Does the same as above, except adds to the pitch offset.", Document.Element.Type.Text),
                new Document.Element("<b>setMusicTime</b> - Sets the Audio Time to go to any point in the song, allowing for skipping specific sections of a song.", Document.Element.Type.Text),
                new Document.Element("<b>playSound</b> - Plays an external sound. The following details what each value in the modifier does." +
                    "\nPath - If global is on, path should be set to something within beatmaps/soundlibrary directory. If global is off, then the path should be set to something within the level " +
                    "folder that has level.lsb and metadata.lsb." +
                    "\nGlobal - Affects the above setting in the way described." +
                    "\nPitch - The speed of the sound played." +
                    "\nVolume - How loud the sound is." +
                    "\nLoop - If the sound should loop while the Modifier is active.", Document.Element.Type.Text),
                new Document.Element("<b>playSoundOnline</b> - Same as above except plays from a link. The global toggle does nothing here.", Document.Element.Type.Text),
                new Document.Element("<b>loadLevel</b> - Loads a level from the current level folder path.", Document.Element.Type.Text),
                new Document.Element("<b>loadLevelInternal</b> - Same as above, except it always loads from the current levels own path.", Document.Element.Type.Text),
            });

            if (CoreHelper.AprilFools)
            {
                var elements = new List<Document.Element>();

                elements.Add(new Document.Element("oops, i spilled my images everywhere...", Document.Element.Type.Text));

                var dir = Directory.GetFiles(RTFile.ApplicationDirectory, "*.png", SearchOption.AllDirectories);

                for (int i = 0; i < UnityEngine.Random.Range(0, Mathf.Clamp(dir.Length, 0, 20)); i++)
                    elements.Add(new Document.Element(dir[UnityEngine.Random.Range(0, dir.Length)].Replace("\\", "/").Replace(RTFile.ApplicationDirectory, ""), Document.Element.Type.Image));

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

        void CreateFolderCreator()
        {
            try
            {
                var folderCreator = EditorManager.inst.GetDialog("Save As Popup").Dialog.gameObject
                    .Duplicate(EditorManager.inst.GetDialog("Save As Popup").Dialog.GetParent(), "Folder Creator Popup");
                folderCreator.transform.localPosition = Vector3.zero;

                var folderCreatorPopup = folderCreator.transform.GetChild(0);

                var folderCreatorPopupPanel = folderCreatorPopup.Find("Panel");
                var folderCreatorPopupPanelTitle = folderCreatorPopupPanel.Find("Text").GetComponent<Text>();
                folderCreatorPopupPanelTitle.text = "Folder Creator";

                var close = folderCreatorPopupPanel.Find("x").GetComponent<Button>();
                close.onClick.ClearAll();
                close.onClick.AddListener(() => EditorManager.inst.HideDialog("Folder Creator Popup"));

                var folderNameLabel = folderCreatorPopup.Find("Level Name").GetComponent<Text>();
                folderNameLabel.text = "New folder name";

                var folderName = folderCreatorPopup.Find("level-name").GetComponent<InputField>();
                folderName.onValueChanged.ClearAll();
                folderName.text = "New Folder";
                folderName.characterLimit = 0;

                var submitImage = folderCreatorPopup.Find("submit").GetComponent<Image>();
                var submitText = folderCreatorPopup.Find("submit/text").GetComponent<Text>();
                submitText.text = "Create Folder";

                EditorThemeManager.AddGraphic(folderCreatorPopup.GetComponent<Image>(), ThemeGroup.Background_1, true);
                EditorThemeManager.AddGraphic(folderCreatorPopupPanel.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);

                EditorThemeManager.AddSelectable(close, ThemeGroup.Close, true);
                EditorThemeManager.AddGraphic(close.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

                EditorThemeManager.AddLightText(folderCreatorPopupPanelTitle);
                EditorThemeManager.AddLightText(folderNameLabel);
                EditorThemeManager.AddInputField(folderName);

                EditorThemeManager.AddGraphic(submitImage, ThemeGroup.Function_1, true);
                EditorThemeManager.AddGraphic(submitText, ThemeGroup.Function_1_Text);

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
            var folderName = Path.GetFileName(copiedLevelPath);
            var directory = Path.GetDirectoryName(copiedLevelPath).Replace("\\", "/");

            if (shouldCutLevel)
            {
                if (RTFile.DirectoryExists(copiedLevelPath.Replace(directory, $"{RTFile.ApplicationDirectory}{editorListPath}")))
                {
                    EditorManager.inst.DisplayNotification($"Level with the name \"{folderName}\" already exists in this location.", 2f, EditorManager.NotificationType.Warning);
                    return;
                }

                Directory.Move(copiedLevelPath, copiedLevelPath.Replace(directory, $"{RTFile.ApplicationDirectory}{editorListPath}"));
                UpdateEditorPath(true);
                EditorManager.inst.DisplayNotification($"Succesfully moved {folderName}!", 2f, EditorManager.NotificationType.Success);

                return;
            }

            var result = copiedLevelPath;
            int num = 0;
            while (Directory.Exists(result.Replace(directory, $"{RTFile.ApplicationDirectory}{editorListPath}")))
            {
                result = $"{copiedLevelPath} [{num}]";
                num++;
            }

            var files = Directory.GetFiles(copiedLevelPath, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i].Replace("\\", "/").Replace(copiedLevelPath, result).Replace(directory, $"{RTFile.ApplicationDirectory}{editorListPath}");
                var copyToDirectory = Path.GetDirectoryName(file);
                if (!RTFile.DirectoryExists(copyToDirectory))
                    Directory.CreateDirectory(copyToDirectory);

                File.Copy(files[i], file, true);
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

            bool anyFailed = false;
            var failedLevels = new List<string>();

            var list = new List<Coroutine>();
            var files = Directory.GetDirectories(RTFile.ApplicationDirectory + editorListPath);
            var showLevelFolders = config.ShowFoldersInLevelList.Value;
            var currentPath = editorPath;

            // Back
            if (showLevelFolders && Path.GetDirectoryName(RTFile.ApplicationDirectory + editorListPath).Replace("\\", "/") != RTFile.ApplicationDirectory + "beatmaps")
            {
                var gameObjectFolder = EditorManager.inst.folderButtonPrefab.Duplicate(transform, $"back");
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
                        RefreshContextMenu(300f,
                            new ButtonFunction("Create folder", () =>
                            {
                                EditorManager.inst.ShowDialog("Folder Creator Popup");
                                RefreshFolderCreator($"{RTFile.ApplicationDirectory}{editorListPath}", () => UpdateEditorPath(true));
                            }),
                            new ButtonFunction("Paste", PasteLevel));

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
                var path = file.Replace("\\", "/");
                var name = Path.GetFileName(path);

                if (!RTFile.FileExists(file + "/level.lsb"))
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
                            RefreshContextMenu(300f,
                                new ButtonFunction("Create folder", () =>
                                {
                                    EditorManager.inst.ShowDialog("Folder Creator Popup");
                                    RefreshFolderCreator($"{RTFile.ApplicationDirectory}{editorListPath}", () => UpdateEditorPath(true));
                                }),
                                new ButtonFunction("Paste", PasteLevel));

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

                var metadataStr = RTFile.ReadFromFile(file + "/metadata.lsb");

                if (metadataStr == null)
                {
                    Debug.LogError($"{EditorManager.inst.className}Could not load metadata for [{name}]!");
                    continue;
                }

                var metadata = MetaData.Parse(JSON.Parse(metadataStr));

                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(transform, $"Folder [{Path.GetFileName(path)}]");
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
                    LSText.ClampString(metadata.beatmap.date_edited, dateClamp));

                folderButtonStorage.text.horizontalOverflow = horizontalOverflow;
                folderButtonStorage.text.verticalOverflow = verticalOverflow;
                folderButtonStorage.text.fontSize = fontSize;

                var difficultyColor = metadata.song.difficulty >= 0 && metadata.song.difficulty < DataManager.inst.difficulties.Count ?
                    DataManager.inst.difficulties[metadata.song.difficulty].color : LSColors.themeColors["none"].color;

                TooltipHelper.AssignTooltip(gameObject, "Level List Button", 3f);
                gameObject.AddComponent<HoverTooltip>().tooltipLangauges.Add(new HoverTooltip.Tooltip
                {
                    desc = "<#" + LSColors.ColorToHex(difficultyColor) + ">" + metadata.artist.Name + " - " + metadata.song.title,
                    hint = $"</color><br>Folder: {name}<br>Date Edited: {metadata.beatmap.date_edited}<br>Date Created: {metadata.LevelBeatmap.date_created}<br>Description: {metadata.song.description}",
                });

                folderButtonStorage.button.onClick.ClearAll();
                folderButtonFunction.onClick = eventData =>
                {
                    if (choosingLevelTemplate)
                    {
                        if (string.IsNullOrEmpty(nameInput.text))
                        {
                            EditorManager.inst.DisplayNotification($"Level template name is empty. Name it something unique via the input field in the Level Template editor.", 3f, EditorManager.NotificationType.Error);
                            return;
                        }

                        if (nameInput.text[nameInput.text.Length - 1] == '/' || nameInput.text[nameInput.text.Length - 1] == '\\')
                        {
                            EditorManager.inst.DisplayNotification($"Name cannot end with a / or a \\.", 3f, EditorManager.NotificationType.Error);
                            return;
                        }

                        if (RTFile.DirectoryExists($"{RTFile.ApplicationDirectory}beatmaps/templates/{nameInput.text}"))
                        {
                            EditorManager.inst.DisplayNotification($"Level template with the name \"{nameInput.text}\" already exists! Set the name to something else.", 3f, EditorManager.NotificationType.Error);
                            return;
                        }

                        EditorManager.inst.HideDialog("Open File Popup");

                        ShowWarningPopup("Are you sure you want to make a new level template?", () =>
                        {
                            choosingLevelTemplate = false;

                            var copyTo = $"{RTFile.ApplicationDirectory}beatmaps/templates/{RTFile.ValidateDirectory(nameInput.text)}";
                            if (RTFile.DirectoryExists(copyTo))
                            {
                                EditorManager.inst.DisplayNotification($"Level template with the name \"{nameInput.text}\" already exists!", 3f, EditorManager.NotificationType.Error);
                                return;
                            }

                            Directory.CreateDirectory(copyTo);
                            File.Copy(file + "/level.lsb", copyTo + "/level.lsb");

                            if (currentTemplateSprite)
                                currentTemplateSprite.Save(copyTo + "/preview.png");

                            RefreshNewLevelTemplates();
                            HideWarningPopup();
                        }, () =>
                        {
                            EditorManager.inst.ShowDialog("Open File Popup");
                            HideWarningPopup();
                        });

                        return;
                    }

                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        RefreshContextMenu(300f,
                            new ButtonFunction("Show Autosaves", () =>
                            {
                                EditorManager.inst.ShowDialog("Autosave Popup");
                                RefreshAutosaveList(editorWrapper);
                            }),
                            new ButtonFunction("Create folder", () =>
                            {
                                EditorManager.inst.ShowDialog("Folder Creator Popup");
                                RefreshFolderCreator($"{RTFile.ApplicationDirectory}{editorListPath}", () => UpdateEditorPath(true));
                            }),
                            new ButtonFunction("Create template", () =>
                            {
                                if (string.IsNullOrEmpty(nameInput.text))
                                {
                                    EditorManager.inst.DisplayNotification($"Level template name is empty. Name it something unique via the input field in the Level Template editor.", 3f, EditorManager.NotificationType.Error);
                                    return;
                                }

                                if (nameInput.text[nameInput.text.Length - 1] == '/' || nameInput.text[nameInput.text.Length - 1] == '\\')
                                {
                                    EditorManager.inst.DisplayNotification($"Name cannot end with a / or a \\.", 3f, EditorManager.NotificationType.Error);
                                    return;
                                }

                                if (RTFile.DirectoryExists($"{RTFile.ApplicationDirectory}beatmaps/templates/{nameInput.text}"))
                                {
                                    EditorManager.inst.DisplayNotification($"Level template with the name \"{nameInput.text}\" already exists! Set the name to something else.", 3f, EditorManager.NotificationType.Error);
                                    return;
                                }

                                EditorManager.inst.HideDialog("Open File Popup");

                                ShowWarningPopup("Are you sure you want to make a new level template?", () =>
                                {
                                    choosingLevelTemplate = false;

                                    var copyTo = $"{RTFile.ApplicationDirectory}beatmaps/templates/{RTFile.ValidateDirectory(nameInput.text)}";
                                    if (RTFile.DirectoryExists(copyTo))
                                    {
                                        EditorManager.inst.DisplayNotification($"Level template with the name \"{nameInput.text}\" already exists!", 3f, EditorManager.NotificationType.Error);
                                        return;
                                    }

                                    Directory.CreateDirectory(copyTo);
                                    File.Copy(file + "/level.lsb", copyTo + "/level.lsb");

                                    if (currentTemplateSprite)
                                        currentTemplateSprite.Save(copyTo + "/preview.png");

                                    RefreshNewLevelTemplates();
                                    HideWarningPopup();
                                }, () =>
                                {
                                    EditorManager.inst.ShowDialog("Open File Popup");
                                    HideWarningPopup();
                                });
                            }),
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
                                    Directory.Delete(editorWrapper.folder, true);
                                    UpdateEditorPath(true);
                                    EditorManager.inst.DisplayNotification("Deleted level!", 2f, EditorManager.NotificationType.Success);
                                    HideWarningPopup();
                                }, HideWarningPopup);
                            }),
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
                            })
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
                
                list.Add(StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{file}/level.jpg", cover =>
                {
                    if (!cover)
                    {
                        anyFailed = true;
                        failedLevels.Add(Path.GetFileName(path));
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
                    anyFailed = true;
                    failedLevels.Add(Path.GetFileName(path));
                    iconImage.sprite = SteamWorkshop.inst.defaultSteamImageSprite;
                    editorWrapper.albumArt = SteamWorkshop.inst.defaultSteamImageSprite;
                    EditorManager.inst.loadedLevels.Add(editorWrapper);
                })));
            }

            if (list.Count >= 1)
                yield return StartCoroutine(LSHelpers.WaitForMultipleCoroutines(list, () =>
                {
                    if (anyFailed && config.ShowLevelsWithoutCoverNotification.Value)
                        EditorManager.inst.DisplayNotification($"Levels {FontManager.TextTranslater.ArrayToString(failedLevels.ToArray())} do not have covers!", 2f * (failedLevels.Count * 0.10f), EditorManager.NotificationType.Error);
                    if (EditorManager.inst.loadedLevels.Count > 0)
                    {
                        EditorManager.inst.ShowDialog("Open File Popup");
                        EditorManager.inst.RenderOpenBeatmapPopup();
                    }
                    else
                        EditorManager.inst.OpenNewLevelPopup();
                }));
            else
            {
                if (anyFailed && config.ShowLevelsWithoutCoverNotification.Value)
                    EditorManager.inst.DisplayNotification($"Levels {FontManager.TextTranslater.ArrayToString(failedLevels.ToArray())} do not have covers!", 2f * (failedLevels.Count * 0.10f), EditorManager.NotificationType.Error);
                if (EditorManager.inst.loadedLevels.Count > 0)
                {
                    EditorManager.inst.ShowDialog("Open File Popup");
                    EditorManager.inst.RenderOpenBeatmapPopup();
                }
                else
                    EditorManager.inst.OpenNewLevelPopup();
            }

            failedLevels.Clear();
            failedLevels = null;

            yield break;
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

            string code = $"{fullPath}/EditorLoad.cs";
            if (RTFile.FileExists(code))
            {
                var str = RTFile.ReadFromFile(code);
                if (RTCode.Validate(str))
                    yield return StartCoroutine(RTCode.IEvaluate(str));
            }

            SetLayer(0, LayerType.Objects);

            WindowController.ResetTitle();

            CoreHelper.Log("Clearing data...");

            for (int i = 0; i < timelineObjects.Count; i++)
            {
                var timelineObject = timelineObjects[i];
                Destroy(timelineObject.GameObject);

                for (int j = 0; j < timelineObject.InternalSelections.Count; j++)
                {
                    var kf = timelineObject.InternalSelections[j];
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
            string rawJSON = null;
            string rawMetadataJSON = null;
            AudioClip song = null;

            EditorManager.inst.ClearDialogs();
            EditorManager.inst.ShowDialog("File Info Popup");

            if (EditorManager.inst.hasLoadedLevel && EditorConfig.Instance.BackupPreviousLoadedLevel.Value && RTFile.DirectoryExists(GameManager.inst.path.Replace("/level.lsb", "")))
            {
                CoreHelper.Log("Backing up previous level...");

                SetFileInfo($"Backing up previous level [ {Path.GetFileName(GameManager.inst.path.Replace("/level.lsb", ""))} ]");

                this.StartCoroutineAsync(ProjectData.Writer.SaveData(GameManager.inst.path.Replace("level.lsb", "level-open-backup.lsb"), GameData.Current));

                CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");
            }

            CoreHelper.Log("Loading data...");

            SetFileInfo($"Loading Level Data for [ {name} ]");

            CoreHelper.Log($"Loading {(string.IsNullOrEmpty(autosave) ? "level.lsb" : autosave)}...");
            rawJSON = RTFile.ReadFromFile(fullPath + "/" + (string.IsNullOrEmpty(autosave) ? "level.lsb" : autosave));
            rawMetadataJSON = RTFile.ReadFromFile(fullPath + "/metadata.lsb");

            if (string.IsNullOrEmpty(rawMetadataJSON))
            {
                DataManager.inst.SaveMetadata(fullPath + "/metadata.lsb");
                rawMetadataJSON = RTFile.ReadFromFile(fullPath + "/metadata.lsb");
            }

            GameManager.inst.path = fullPath + "/level.lsb";
            GameManager.inst.basePath = fullPath + "/";
            GameManager.inst.levelName = name;
            SetFileInfo($"Loading Level Music for [ {name} ]\n\nIf this is taking more than a minute or two check if the song file (.ogg / .wav / .mp3) is corrupt. If not, then something went really wrong.");

            string errorMessage = "";
            bool hadError = false;
            if (RTFile.FileExists(fullPath + "/level.ogg"))
                yield return this.StartCoroutineAsync(AlephNetworkManager.DownloadAudioClip($"file://{fullPath}/level.ogg", AudioType.OGGVORBIS, x => { song = x; x = null; }, onError => { hadError = true; errorMessage = onError; }));
            else if (RTFile.FileExists(fullPath + "/level.wav"))
                yield return this.StartCoroutineAsync(AlephNetworkManager.DownloadAudioClip($"file://{fullPath}/level.wav", AudioType.WAV, x => { song = x; x = null; }, onError => { hadError = true; errorMessage = onError; }));
            else if (RTFile.FileExists(fullPath + "/level.mp3"))
                yield return song = LSAudio.CreateAudioClipUsingMP3File(fullPath + "/level.mp3");

            if (hadError)
            {
                bool audioExists = RTFile.FileExists(fullPath + "/level.ogg") || RTFile.FileExists(fullPath + "/level.wav") || RTFile.FileExists(fullPath + "/level.mp3");

                if (audioExists)
                    SetFileInfo($"Something went wrong when loading the song file. Either the file is corrupt or something went wrong internally.");
                else
                    SetFileInfo($"Song file does not exist.");

                EditorManager.inst.DisplayNotification($"Song file could not load due to {errorMessage}", 3f, EditorManager.NotificationType.Error);

                CoreHelper.LogError($"Level loading caught an error: {errorMessage}\n" +
                    $"level.ogg exists: {RTFile.FileExists(fullPath + "/level.ogg")}\n" +
                    $"level.wav exists: {RTFile.FileExists(fullPath + "/level.wav")}\n" +
                    $"level.mp3 exists: {RTFile.FileExists(fullPath + "/level.mp3")}\n");

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
            CoreHelper.UpdateDiscordStatus($"Editing: {DataManager.inst.metaData.song.title}", "In Editor", "editor");

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
            EventManager.inst.updateEvents();
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            RTEventManager.inst.SetResetOffsets();

            CoreHelper.Log("Creating timeline objects...");
            SetFileInfo($"Setting first object of [ {name} ]");
            StartCoroutine(ObjectEditor.inst.ICreateTimelineObjects());
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
            {
                ExampleManager.inst.SayDialogue(fromNewLevel ? "LoadedNewLevel" : "LoadedLevel");
            }

            EditorManager.inst.loading = false;
            fromNewLevel = false;

            rawJSON = null;
            rawMetadataJSON = null;
            song = null;

            CoreHelper.StopAndLogStopwatch(sw, $"Finished loading {name}");
            sw = null;

            yield break;
        }

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

            if (themeAddButton == null)
            {
                themeAddButton = EventEditor.inst.ThemeAdd.Duplicate(parent, "Create New");
                var tf = themeAddButton.transform;
                themeAddButton.SetActive(true);
                tf.localScale = Vector2.one;
                var button = themeAddButton.GetComponent<Button>();
                button.onClick.AddListener(() => { RTThemeEditor.inst.RenderThemeEditor(); });

                EditorThemeManager.AddGraphic(button.image, ThemeGroup.List_Button_2_Normal, true);
                EditorThemeManager.AddGraphic(themeAddButton.transform.Find("edit").GetComponent<Image>(), ThemeGroup.List_Button_2_Text);
                EditorThemeManager.AddGraphic(themeAddButton.transform.Find("text").GetComponent<Text>(), ThemeGroup.List_Button_2_Text);
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
            var files = Directory.GetFiles(RTFile.ApplicationDirectory + themeListPath, "*.lst");
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
                    var str = FontManager.TextTranslater.ArrayToString(array);

                    if (EditorManager.inst != null)
                        EditorManager.inst.DisplayNotification($"Unable to Load theme [{orig.name}] due to conflicting themes: {str}", 2f * array.Length, EditorManager.NotificationType.Error);
                }
                else
                {
                    DataManager.inst.BeatmapThemeIndexToID.Add(DataManager.inst.AllThemes.Count - 1, int.Parse(orig.id));
                    DataManager.inst.BeatmapThemeIDToIndex.Add(int.Parse(orig.id), DataManager.inst.AllThemes.Count - 1);

                    RTThemeEditor.inst.SetupThemePanel(orig, false);
                }

                if (jn["id"] == null)
                {
                    var beatmapTheme = BeatmapTheme.DeepCopy(orig);
                    beatmapTheme.id = LSText.randomNumString(BeatmapTheme.IDLength);
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
        public IEnumerator LoadPrefabs(PrefabEditor __instance)
        {
            if (prefabsLoading)
                yield break;

            prefabsLoading = true;

            while (!PrefabEditor.inst || !PrefabEditor.inst.externalContent)
                yield return null;

            RTPrefabEditor.inst.PrefabPanels.FindAll(x => x.Dialog == PrefabDialog.External).ForEach(x => Destroy(x.GameObject));
            RTPrefabEditor.inst.PrefabPanels.RemoveAll(x => x.Dialog == PrefabDialog.External);

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
                        RTEditor.inst.RefreshContextMenu(300f,
                            new RTEditor.ButtonFunction("Create folder", () =>
                            {
                                EditorManager.inst.ShowDialog("Folder Creator Popup");
                                RTEditor.inst.RefreshFolderCreator($"{RTFile.ApplicationDirectory}{RTEditor.prefabListPath}", () => RTEditor.inst.UpdatePrefabPath(true));
                            }),
                            new RTEditor.ButtonFunction("Paste", RTPrefabEditor.inst.PastePrefab)
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

            //while (!EditorManager.inst.folderButtonPrefab.GetComponent<FunctionButtonStorage>())
            //    yield return null;

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
                        RefreshContextMenu(300f,
                            new ButtonFunction("Create folder", () =>
                            {
                                EditorManager.inst.ShowDialog("Folder Creator Popup");
                                RefreshFolderCreator($"{RTFile.ApplicationDirectory}{prefabListPath}", () => UpdatePrefabPath(true));
                            }),
                            new ButtonFunction("Paste", RTPrefabEditor.inst.PastePrefab));

                        return;
                    }

                    if (prefabPathField.text == prefabPath)
                    {
                        prefabPathField.text = Path.GetDirectoryName(RTFile.ApplicationDirectory + prefabListPath).Replace("\\", "/").Replace(RTFile.ApplicationDirectory + "beatmaps/", "");
                        UpdatePrefabPath(false);
                    }
                };

                EditorThemeManager.ApplySelectable(folderButtonStorageFolder.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(folderButtonStorageFolder.text);
            }

            prefabExternalUpAFolderButton.SetActive(Path.GetDirectoryName(RTFile.ApplicationDirectory + prefabListPath).Replace("\\", "/") != RTFile.ApplicationDirectory + "beatmaps");

            foreach (var directory in Directory.GetDirectories(RTFile.ApplicationDirectory + prefabListPath, "*", SearchOption.TopDirectoryOnly))
            {
                var path = directory.Replace("\\", "/");
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
                        RefreshContextMenu(300f,
                            new ButtonFunction("Create folder", () =>
                            {
                                EditorManager.inst.ShowDialog("Folder Creator Popup");
                                RefreshFolderCreator($"{RTFile.ApplicationDirectory}{prefabListPath}", () => UpdatePrefabPath(true));
                            }),
                            new ButtonFunction("Paste", () => { }));

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

            int num = 0;
            foreach (var file in Directory.GetFiles(RTFile.ApplicationDirectory + prefabListPath, "*.lsp", SearchOption.TopDirectoryOnly))
            {
                var jn = JSON.Parse(RTFile.ReadFromFile(file));

                var prefab = Prefab.Parse(jn);
                prefab.objects.ForEach(x => { x.prefabID = ""; x.prefabInstanceID = ""; });
                prefab.filePath = file.Replace("\\", "/");

                StartCoroutine(RTPrefabEditor.inst.CreatePrefabButton(prefab, num, PrefabDialog.External, file, false, hoverSize,
                         nameHorizontalOverflow, nameVerticalOverflow, nameFontSize,
                         typeHorizontalOverflow, typeVerticalOverflow, typeFontSize,
                         deleteAnchoredPosition, deleteSizeDelta));

                num++;
            }

            prefabsLoading = false;

            yield break;
        }

        public IEnumerator UpdatePrefabs()
        {
            yield return inst.StartCoroutine(LoadPrefabs(PrefabEditor.inst));
            PrefabEditor.inst.ReloadExternalPrefabsInPopup();
            EditorManager.inst.DisplayNotification("Updated external prefabs!", 2f, EditorManager.NotificationType.Success);
            yield break;
        }

        public void SetAutoSave()
        {
            if (!RTFile.DirectoryExists(GameManager.inst.basePath + "autosaves"))
                Directory.CreateDirectory(GameManager.inst.basePath + "autosaves");

            string[] files = Directory.GetFiles(GameManager.inst.basePath + "autosaves", "autosave_*.lsb", SearchOption.TopDirectoryOnly);
            files.ToList().Sort();

            EditorManager.inst.autosaves.Clear();

            foreach (var file in files)
            {
                EditorManager.inst.autosaves.Add(file);
            }

            EditorManager.inst.CancelInvoke("AutoSaveLevel");
            CancelInvoke("AutoSaveLevel");
            InvokeRepeating("AutoSaveLevel", EditorConfig.Instance.AutosaveLoopTime.Value, EditorConfig.Instance.AutosaveLoopTime.Value);
        }

        public void AutoSaveLevel()
        {
            if (EditorManager.inst.loading)
                return;

            autoSaving = true;

            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Beatmap can't autosave until you load a level.", 3f, EditorManager.NotificationType.Error);
                return;
            }

            if (EditorManager.inst.savingBeatmap)
            {
                EditorManager.inst.DisplayNotification("Already attempting to save the beatmap!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            string autosavePath = $"{GameManager.inst.basePath}autosaves/autosave_{DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss")}.lsb";

            if (!RTFile.DirectoryExists(GameManager.inst.basePath + "autosaves"))
                Directory.CreateDirectory(GameManager.inst.basePath + "autosaves");

            EditorManager.inst.DisplayNotification("Autosaving backup!", 2f, EditorManager.NotificationType.Warning);

            EditorManager.inst.autosaves.Add(autosavePath);

            while (EditorManager.inst.autosaves.Count > EditorConfig.Instance.AutosaveLimit.Value)
            {
                var first = EditorManager.inst.autosaves[0];
                if (RTFile.FileExists(first))
                    File.Delete(first);

                EditorManager.inst.autosaves.RemoveAt(0);
            }

            this.StartCoroutineAsync(ProjectData.Writer.SaveData(autosavePath, GameData.Current));

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

            if (!EditorManager.inst.newAudioFile.ToLower().Contains(".ogg") && !EditorManager.inst.newAudioFile.ToLower().Contains(".wav") && !EditorManager.inst.newAudioFile.ToLower().Contains(".mp3"))
            {
                EditorManager.inst.DisplayNotification("The file you are trying to load doesn't appear to be a song file.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            if (!RTFile.FileExists(EditorManager.inst.newAudioFile))
            {
                EditorManager.inst.DisplayNotification("The file you are trying to load doesn't appear to exist.", 2f, EditorManager.NotificationType.Error);
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
                EditorManager.inst.DisplayNotification("The level you are trying to create already exists.", 2f, EditorManager.NotificationType.Error);
                return;
            }
            Directory.CreateDirectory(path);

            if (EditorManager.inst.newAudioFile.ToLower().Contains(".ogg"))
            {
                string destFileName = $"{path}/level.ogg";
                File.Copy(EditorManager.inst.newAudioFile, destFileName, true);
            }
            if (EditorManager.inst.newAudioFile.ToLower().Contains(".wav"))
            {
                string destFileName = $"{path}/level.wav";
                File.Copy(EditorManager.inst.newAudioFile, destFileName, true);
            }
            if (EditorManager.inst.newAudioFile.ToLower().Contains(".mp3"))
            {
                string destFileName = $"{path}/level.mp3";
                File.Copy(EditorManager.inst.newAudioFile, destFileName, true);
            }

            var json = currentLevelTemplate >= 0 && currentLevelTemplate < NewLevelTemplates.Count && RTFile.FileExists(NewLevelTemplates[currentLevelTemplate]) ? RTFile.ReadFromFile(NewLevelTemplates[currentLevelTemplate]) : null;

            var gameData = !string.IsNullOrEmpty(json) ? GameData.Parse(JSON.Parse(json), false) : CreateBaseBeatmap();

            StartCoroutine(ProjectData.Writer.SaveData($"{path}/level.lsb", gameData));
            var metaData = new MetaData();
            metaData.beatmap.game_version = "4.1.16";
            metaData.arcadeID = LSText.randomNumString(16);
            metaData.song.title = newLevelSongTitle;
            metaData.uploaderName = SteamWrapper.inst.user.displayName;
            metaData.creator.steam_name = SteamWrapper.inst.user.displayName;
            metaData.creator.steam_id = SteamWrapper.inst.user.id;
            metaData.LevelBeatmap.name = EditorManager.inst.newLevelName;

            DataManager.inst.metaData = metaData;

            fromNewLevel = true;
            DataManager.inst.SaveMetadata($"{path}/metadata.lsb");
            StartCoroutine(LoadLevel(path));
            EditorManager.inst.HideDialog("New File Popup");
        }

        public string newLevelSongTitle = "Inertia";

        public GameData CreateBaseBeatmap()
        {
            var gameData = new GameData();
            gameData.beatmapData = new LevelBeatmapData();
            gameData.beatmapData.levelData = new LevelData();
            gameData.beatmapData.editorData = new LevelEditorData();
            gameData.beatmapData.checkpoints.Add(new DataManager.GameData.BeatmapData.Checkpoint(false, "Base Checkpoint", 0f, Vector2.zero));

            if (gameData.eventObjects.allEvents == null)
                gameData.eventObjects.allEvents = new List<List<BaseEventKeyframe>>();
            gameData.eventObjects.allEvents.Clear();
            ProjectData.Reader.ClampEventListValues(gameData.eventObjects.allEvents, GameData.EventCount);

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
                beatmapObject.events[2].Add(new EventKeyframe(999f, new float[1] { 360000f }, new float[3]));

            beatmapObject.name = CoreHelper.AprilFools ? "trololololo" : "\"Default object cameo\" -Viral Mecha";
            beatmapObject.autoKillType = AutoKillType.LastKeyframeOffset;
            beatmapObject.autoKillOffset = 4f;
            beatmapObject.editorData.layer = 0;
            gameData.beatmapObjects.Add(beatmapObject);

            return gameData;
        }

        #endregion

        #region Refresh Popups / Dialogs

        public void RefreshContextMenu(float width, params ButtonFunction[] buttonFunctions)
        {
            contextMenu.transform.AsRT().anchoredPosition = Input.mousePosition * CoreHelper.ScreenScaleInverse;
            contextMenu.transform.AsRT().sizeDelta = new Vector2(width, 37f * buttonFunctions.Length);
            contextMenu.SetActive(true);
            LSHelpers.DeleteChildren(contextMenuLayout);
            for (int i = 0; i < buttonFunctions.Length; i++)
            {
                var buttonFunction = buttonFunctions[i];
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
            }
        }

        public void RefreshFolderCreator(string path, Action onSubmit)
        {
            var folderCreatorPopup = EditorManager.inst.GetDialog("Folder Creator Popup").Dialog.GetChild(0);
            var folderName = folderCreatorPopup.Find("level-name").GetComponent<InputField>();
            var submit = folderCreatorPopup.Find("submit").GetComponent<Button>();
            submit.onClick.ClearAll();
            submit.onClick.AddListener(() =>
            {
                var directory = Path.Combine(path, RTFile.ValidateDirectory(folderName.text));
                if (RTFile.DirectoryExists(directory))
                    return;

                Directory.CreateDirectory(directory);
                onSubmit?.Invoke();
            });
        }

        public void ShowObjectSearch(Action<BeatmapObject> onSelect, bool clearParent = false)
        {
            EditorManager.inst.ShowDialog("Object Search Popup");
            RefreshObjectSearch(onSelect, clearParent);
        }

        public void RefreshObjectSearch(Action<BeatmapObject> onSelect, bool clearParent = false)
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
                    foreach (var bm in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
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
                RefreshObjectSearch(onSelect, clearParent);
            });

            LSHelpers.DeleteChildren(content);

            var list = GameData.Current.beatmapObjects.FindAll(x => !x.fromPrefab);
            foreach (var beatmapObject in list)
            {
                var regex = new Regex(@"\[([0-9])\]");
                var match = regex.Match(objectSearchTerm);

                if (string.IsNullOrEmpty(objectSearchTerm) ||
                    match.Success && int.TryParse(match.Groups[1].ToString(), out int index) && index < GameData.Current.beatmapObjects.Count && GameData.Current.beatmapObjects.IndexOf(beatmapObject) == index ||
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

            if (ExampleManager.inst && ExampleManager.inst.Visible)
            {
                var warningPopup = EditorManager.inst.GetDialog("Warning Popup").Dialog.GetChild(0);
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
            close.onClick.AddListener(() => { cancelDelegate?.Invoke(); });

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
            #region Sorting

            Func<MetadataWrapper, bool> editorFolderSelector = x => x is EditorWrapper editorWrapper && !editorWrapper.isFolder;
            var loadedLevels = EditorManager.inst.loadedLevels;

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
                        EditorManager.inst.loadedLevels = (levelAscend ? loadedLevels.OrderBy(x => x.metadata is MetaData metadata ? metadata.LevelBeatmap.date_created : "") :
                            loadedLevels.OrderByDescending(x => x.metadata is MetaData metadata ? metadata.LevelBeatmap.date_created : "")).OrderBy(editorFolderSelector).ToList();
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

                editorWrapper.SetActive(editorWrapper.isFolder && CoreHelper.SearchString(EditorManager.inst.openFileSearch, Path.GetFileName(folder)) ||
                    !editorWrapper.isFolder && (CoreHelper.SearchString(EditorManager.inst.openFileSearch, Path.GetFileName(folder)) ||
                        metadata == null || metadata != null &&
                        (CoreHelper.SearchString(EditorManager.inst.openFileSearch, metadata.song.title) ||
                        CoreHelper.SearchString(EditorManager.inst.openFileSearch, metadata.artist.Name) ||
                        CoreHelper.SearchString(EditorManager.inst.openFileSearch, metadata.creator.steam_name) ||
                        CoreHelper.SearchString(EditorManager.inst.openFileSearch, metadata.song.description) ||
                        CoreHelper.SearchString(EditorManager.inst.openFileSearch, difficultyNames[Mathf.Clamp(metadata.song.difficulty, 0, difficultyNames.Length - 1)]))));

                editorWrapper.GameObject.transform.SetSiblingIndex(num);
                num++;
            }

            var transform = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("mask/content");

            if (transform.Find("back"))
            {
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
                    if (timelineObject.IsPrefabObject)
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
                if (list.Count == 1 && timelineObject.IsBeatmapObject)
                    StartCoroutine(ObjectEditor.RefreshObjectGUI(timelineObject.GetData<BeatmapObject>()));
                if (list.Count == 1 && timelineObject.IsPrefabObject)
                    RTPrefabEditor.inst.RenderPrefabObjectDialog(timelineObject.GetData<PrefabObject>());
            });

            EditorThemeManager.ApplySelectable(noParentButton, ThemeGroup.List_Button_1);
            EditorThemeManager.ApplyLightText(noParentText);

            if (CoreHelper.SearchString(__instance.parentSearch, "camera"))
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
                        if (timelineObject.IsPrefabObject)
                        {
                            var prefabObject = timelineObject.GetData<PrefabObject>();
                            prefabObject.parent = "CAMERA_PARENT";
                            Updater.UpdatePrefab(prefabObject);

                            continue;
                        }

                        var bm = timelineObject.GetData<BeatmapObject>();
                        bm.parent = "CAMERA_PARENT";
                        Updater.UpdateObject(bm);
                    }

                    EditorManager.inst.HideDialog("Parent Selector");
                    if (list.Count == 1 && timelineObject.IsBeatmapObject)
                        StartCoroutine(ObjectEditor.RefreshObjectGUI(timelineObject.GetData<BeatmapObject>()));
                    if (list.Count == 1 && timelineObject.IsPrefabObject)
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
                            if (timelineObject.IsPrefabObject)
                            {
                                var prefabObject = timelineObject.GetData<PrefabObject>();
                                prefabObject.parent = id;
                                Updater.UpdatePrefab(prefabObject);

                                continue;
                            }

                            var bm = timelineObject.GetData<BeatmapObject>();
                            TriggerHelper.SetParent(timelineObject, ObjectEditor.inst.GetTimelineObject((BeatmapObject)obj));
                            Updater.UpdateObject(bm);
                        }

                        EditorManager.inst.HideDialog("Parent Selector");
                        if (list.Count == 1 && timelineObject.IsBeatmapObject)
                            StartCoroutine(ObjectEditor.RefreshObjectGUI(timelineObject.GetData<BeatmapObject>()));
                        if (list.Count == 1 && timelineObject.IsPrefabObject)
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

        public void RefreshFileBrowserLevels() => RTFileBrowser.inst?.UpdateBrowser(RTFile.ApplicationDirectory, ".lsb", "level", x => StartCoroutine(LoadLevel(x.Replace("\\", "/").Replace("/level.lsb", ""))));

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

        public void SelectDocumentation(Document document)
        {
            documentationTitle.text = $"- {document.Name} -";

            var singleInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position/x");
            var label = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content").transform.GetChild(3).gameObject;

            LSHelpers.DeleteChildren(documentationContent);

            int num = 0;
            foreach (var element in document.elements)
            {
                switch (element.type)
                {
                    case Document.Element.Type.Text:
                        {
                            if (element.Data is not string || string.IsNullOrEmpty((string)element.Data))
                                break;

                            var bar = singleInput.Duplicate(documentationContent, "element");
                            DestroyImmediate(bar.GetComponent<InputField>());
                            DestroyImmediate(bar.GetComponent<EventInfo>());
                            DestroyImmediate(bar.GetComponent<EventTrigger>());

                            LSHelpers.DeleteChildren(bar.transform);
                            bar.transform.localScale = Vector3.one;
                            bar.transform.AsRT().sizeDelta = new Vector2(722f, 22f * LSText.WordWrap((string)element.Data, 67).Count);

                            var l = label.Duplicate(bar.transform, "label");
                            l.transform.localScale = Vector3.one;
                            var text = l.transform.GetChild(0).GetComponent<Text>();
                            text.text = (string)element.Data;
                            text.alignment = TextAnchor.UpperLeft;
                            EditorThemeManager.ApplyLightText(text);

                            l.transform.AsRT().sizeDelta = new Vector2(722f, 22f);
                            l.transform.GetChild(0).AsRT().anchoredPosition = new Vector2(10f, -5f);
                            l.transform.GetChild(0).AsRT().sizeDelta = new Vector2(722f, 22f);

                            var barImage = bar.GetComponent<Image>();
                            barImage.enabled = true;
                            barImage.fillCenter = true;

                            if (element.Function != null)
                            {
                                var button = bar.AddComponent<Button>();
                                button.onClick.AddListener(() => element.Function.Invoke());
                                button.image = barImage;
                                EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                            }
                            else
                            {
                                EditorThemeManager.ApplyGraphic(barImage, ThemeGroup.List_Button_1_Normal, true);
                            }

                            break;
                        }
                    case Document.Element.Type.Image:
                        {
                            if (element.Data is not string)
                                break;

                            var bar = singleInput.Duplicate(documentationContent, "element");
                            LSHelpers.DeleteChildren(bar.transform);
                            DestroyImmediate(bar.GetComponent<InputField>());
                            DestroyImmediate(bar.GetComponent<EventInfo>());
                            DestroyImmediate(bar.GetComponent<EventTrigger>());
                            DestroyImmediate(bar.GetComponent<HorizontalLayoutGroup>());

                            bar.transform.localScale = Vector3.one;

                            var imageObj = bar.Duplicate(bar.transform, "image");
                            imageObj.transform.AsRT().anchoredPosition = Vector2.zero;

                            LSHelpers.DeleteChildren(imageObj.transform);

                            var barImage = bar.GetComponent<Image>();
                            barImage.enabled = true;
                            barImage.fillCenter = true;
                            var barMask = bar.AddComponent<Mask>();

                            EditorThemeManager.ApplyGraphic(barImage, ThemeGroup.List_Button_1_Normal, true);

                            var imageObjImage = imageObj.GetComponent<Image>();
                            imageObjImage.enabled = true;
                            imageObjImage.color = new Color(1f, 1f, 1f, 1f);

                            if (RTFile.FileExists($"{RTFile.ApplicationDirectory}{(string)element.Data}"))
                                imageObjImage.sprite = SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{(string)element.Data}");
                            else
                                imageObjImage.enabled = false;

                            if (imageObjImage.sprite && imageObjImage.sprite.texture)
                            {
                                var width = Mathf.Clamp(imageObjImage.sprite.texture.width, 0, 718);
                                bar.transform.AsRT().sizeDelta = new Vector2(width, imageObjImage.sprite.texture.height);
                                imageObj.transform.AsRT().sizeDelta = new Vector2(width, imageObjImage.sprite.texture.height);
                            }

                            if (element.Function != null)
                            {
                                bar.AddComponent<Button>().onClick.AddListener(() => element.Function.Invoke());
                            }

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
                debuggerPopup.Content.GetChild(i).gameObject.SetActive(CoreHelper.SearchString(debugSearch, debugs[i]));
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

                    AnimationManager.inst.RemoveID(animation.id);
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

        public static Color GetObjectColor(BaseBeatmapObject beatmapObject, bool ignoreTransparency)
        {
            if (beatmapObject.objectType == ObjectType.Empty)
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
            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + "recycling"))
                Directory.CreateDirectory(RTFile.ApplicationDirectory + "recycling");

            Directory.Move(RTFile.ApplicationDirectory + editorListSlash + level, RTFile.ApplicationDirectory + "recycling/" + level);
        }

        public static float SnapToBPM(float time) => Mathf.RoundToInt((time + inst.bpmOffset) / (SettingEditor.inst.BPMMulti / EditorConfig.Instance.BPMSnapDivisions.Value)) * (SettingEditor.inst.BPMMulti / EditorConfig.Instance.BPMSnapDivisions.Value);

        public static void SetActive(GameObject gameObject, bool active)
        {
            gameObject.SetActive(active);
            gameObject.transform.parent.GetChild(gameObject.transform.GetSiblingIndex() - 1).gameObject.SetActive(active);
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
