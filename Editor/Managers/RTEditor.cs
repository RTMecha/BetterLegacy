using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using CielaSpike;
using TMPro;
using SimpleJSON;
using Crosstales.FB;

using BetterLegacy.Arcade.Managers;
using BetterLegacy.Companion;
using BetterLegacy.Companion.Entity;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Core.Optimization.Objects;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Popups;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Base modded editor manager.
    /// </summary>
    public class RTEditor : MonoBehaviour
    {
        #region Init

        /// <summary>
        /// The <see cref="RTEditor"/> global instance reference.
        /// </summary>
        public static RTEditor inst;

        /// <summary>
        /// Initializes <see cref="RTEditor"/>.
        /// </summary>
        public static void Init() => EditorManager.inst.gameObject.AddComponent<RTEditor>();

        void Awake()
        {
            inst = this;

            InitFileWatchers();

            CreateGlobalSettings();
            LoadGlobalSettings();

            try
            {
                ShapeManager.inst.SetupShapes();
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"There was an error with loading the shapes in the editor: {ex}");
            }

            CacheEditor();
            CacheSprites();
            RegisterPopups();
            InitPrefabs();
            InitUI();
            InitEditors();
            FinalSetup();
        }

        void OnDestroy()
        {
            CoreHelper.LogError($"RTEditor was destroyed!");
            EditorConfig.UpdateEditorComplexity = null;
            EditorConfig.AdjustPositionInputsChanged = null;
        }

        #region Full Init

        // 1 - cache editor values
        void CacheEditor()
        {
            titleBar = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar").transform;
            popups = GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups").transform;
            EditorTimeline.inst.wholeTimeline = EditorManager.inst.timelineSlider.transform.parent.parent;
            EditorTimeline.inst.bins = EditorTimeline.inst.wholeTimeline.Find("Bins");
            timelineBar = EditorManager.inst.playButton.transform.parent.gameObject;
            EditorTimeline.inst.timelineImage = EditorManager.inst.timeline.GetComponent<Image>();
            EditorTimeline.inst.timelineOverlayImage = EditorManager.inst.timelineWaveformOverlay.GetComponent<Image>();

            notificationsParent = EditorManager.inst.notification.transform.AsRT();
            tooltipText = EditorManager.inst.tooltip.GetComponent<TextMeshProUGUI>();

            // Here we fix the naming issues with unmodded Legacy. (e.g. naming a theme doesn't allow for symbols and naming a prefab doesn't allow for spaces)
            EditorManager.inst.GetDialog("Save As Popup").Dialog.Find("New File Popup/level-name").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
            GameObject.Find("Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/name/input").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
        }

        // 2 - cache sprites
        void CacheSprites()
        {
            EditorSprites.Init();

            EditorSprites.DottedLineSprite = ObjEditor.inst.KeyframeEndPrefab.GetComponent<Image>().sprite;
        }

        // 3 - register existing popups
        void RegisterPopups()
        {
            try
            {
                NewLevelPopup = new EditorPopup(EditorPopup.NEW_FILE_POPUP);
                NewLevelPopup.Assign(NewLevelPopup.GetLegacyDialog().Dialog.gameObject);
                NewLevelPopup.title = NewLevelPopup.TMPTitle.text;
                NewLevelPopup.size = NewLevelPopup.GameObject.transform.GetChild(0).AsRT().sizeDelta;

                OpenLevelPopup = new ContentPopup(EditorPopup.OPEN_FILE_POPUP);
                OpenLevelPopup.Assign(OpenLevelPopup.GetLegacyDialog().Dialog.gameObject);
                OpenLevelPopup.title = OpenLevelPopup.Title.text;
                OpenLevelPopup.size = OpenLevelPopup.GameObject.transform.AsRT().sizeDelta;
                OpenLevelPopup.refreshSearch = EditorManager.inst.UpdateOpenBeatmapSearch;

                InfoPopup = new InfoPopup(EditorPopup.FILE_INFO_POPUP);
                InfoPopup.Assign(InfoPopup.GetLegacyDialog().Dialog.gameObject);

                ParentSelectorPopup = new ContentPopup(EditorPopup.PARENT_SELECTOR);
                ParentSelectorPopup.Assign(ParentSelectorPopup.GetLegacyDialog().Dialog.gameObject);
                ParentSelectorPopup.title = ParentSelectorPopup.Title.text;
                ParentSelectorPopup.size = ParentSelectorPopup.GameObject.transform.AsRT().sizeDelta;
                ParentSelectorPopup.refreshSearch = EditorManager.inst.UpdateParentSearch;

                SaveAsPopup = new EditorPopup(EditorPopup.SAVE_AS_POPUP);
                SaveAsPopup.Assign(SaveAsPopup.GetLegacyDialog().Dialog.gameObject);
                SaveAsPopup.title = SaveAsPopup.Title.text;
                SaveAsPopup.size = SaveAsPopup.GameObject.transform.AsRT().sizeDelta;

                PrefabPopups = new PrefabPopup(EditorPopup.PREFAB_POPUP);
                var prefabDialog = PrefabPopups.GetLegacyDialog().Dialog;
                PrefabPopups.GameObject = prefabDialog.gameObject;
                PrefabPopups.InternalPrefabs = new ContentPopup("internal prefabs");
                PrefabPopups.InternalPrefabs.Assign(prefabDialog.Find("internal prefabs").gameObject);
                PrefabPopups.ExternalPrefabs = new ContentPopup("external prefabs");
                PrefabPopups.ExternalPrefabs.Assign(prefabDialog.Find("external prefabs").gameObject);

                ObjectOptionsPopup = new EditorPopup(EditorPopup.OBJECT_OPTIONS_POPUP);
                ObjectOptionsPopup.GameObject = ObjectOptionsPopup.GetLegacyDialog().Dialog.gameObject;

                BGObjectOptionsPopup = new EditorPopup(EditorPopup.BG_OPTIONS_POPUP);
                BGObjectOptionsPopup.GameObject = BGObjectOptionsPopup.GetLegacyDialog().Dialog.gameObject;

                editorPopups.Add(NewLevelPopup);
                editorPopups.Add(OpenLevelPopup);
                editorPopups.Add(InfoPopup);
                editorPopups.Add(ParentSelectorPopup);
                editorPopups.Add(SaveAsPopup);
                editorPopups.Add(PrefabPopups);
                editorPopups.Add(ObjectOptionsPopup);
                editorPopups.Add(BGObjectOptionsPopup);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        // 4 - create editor prefabs
        void InitPrefabs()
        {
            var prefabParent = Creator.NewGameObject("prefabs", transform);
            var prefabHolder = EditorPrefabHolder.Instance;
            prefabHolder.PrefabParent = prefabParent.transform;

            timeDefault = timelineBar.transform.GetChild(0).gameObject;
            timeDefault.name = "Time Default";

            var defaultInputField = timelineBar.transform.Find("Time");
            prefabHolder.DefaultInputField = defaultInputField.gameObject;
            prefabHolder.DefaultInputField.SetActive(true);
            defaultInputField.SetParent(prefabHolder.PrefabParent);
            defaultInputField.localScale = Vector3.one;
            EditorManager.inst.speedText.transform.parent.SetParent(transform);

            if (EditorPrefabHolder.Instance.DefaultInputField.TryGetComponent(out InputField frick))
                frick.textComponent.fontSize = 18;

            if (ObjEditor.inst)
            {
                prefabHolder.NumberInputField = ObjEditor.inst.ObjectView.transform.Find("time").gameObject.Duplicate(prefabHolder.PrefabParent, "float input");

                var floatInputFieldStorage = prefabHolder.NumberInputField.AddComponent<InputFieldStorage>();
                floatInputFieldStorage.Assign(prefabHolder.NumberInputField);
                DestroyImmediate(floatInputFieldStorage.leftGreaterButton.GetComponent<Animator>());
                floatInputFieldStorage.leftGreaterButton.transition = Selectable.Transition.ColorTint;
                DestroyImmediate(floatInputFieldStorage.leftButton.GetComponent<Animator>());
                floatInputFieldStorage.leftButton.transition = Selectable.Transition.ColorTint;
                DestroyImmediate(floatInputFieldStorage.middleButton.GetComponent<Animator>());
                floatInputFieldStorage.middleButton.transition = Selectable.Transition.ColorTint;
                DestroyImmediate(floatInputFieldStorage.rightButton.GetComponent<Animator>());
                floatInputFieldStorage.rightButton.transition = Selectable.Transition.ColorTint;
                DestroyImmediate(floatInputFieldStorage.rightGreaterButton.GetComponent<Animator>());
                floatInputFieldStorage.rightGreaterButton.transition = Selectable.Transition.ColorTint;
                floatInputFieldStorage.inputField.characterValidation = InputField.CharacterValidation.None;
                floatInputFieldStorage.inputField.characterLimit = 0;
                prefabHolder.NumberInputField.transform.Find("time").gameObject.name = "input";

                if (prefabHolder.NumberInputField.transform.Find("lock"))
                    DestroyImmediate(prefabHolder.NumberInputField.transform.Find("lock").gameObject);

                var middleButtonLayout = floatInputFieldStorage.middleButton.GetComponent<LayoutElement>();
                middleButtonLayout.minWidth = 8f;
                middleButtonLayout.preferredWidth = 8f;

                floatInputFieldStorage.subButton = floatInputFieldStorage.middleButton.gameObject.Duplicate(prefabHolder.NumberInputField.transform, "sub").GetComponent<Button>();
                floatInputFieldStorage.subButton.image = floatInputFieldStorage.subButton.GetComponent<Image>();
                floatInputFieldStorage.subButton.image.sprite = SpriteHelper.LoadSprite(RTFile.GetAsset("editor_gui_sub.png"));
                var subButtonLayout = floatInputFieldStorage.subButton.GetComponent<LayoutElement>();
                subButtonLayout.minWidth = 16f;
                subButtonLayout.preferredWidth = 16f;

                floatInputFieldStorage.subButton.gameObject.SetActive(false);
                floatInputFieldStorage.addButton = floatInputFieldStorage.middleButton.gameObject.Duplicate(prefabHolder.NumberInputField.transform, "add").GetComponent<Button>();
                floatInputFieldStorage.addButton.image = floatInputFieldStorage.addButton.GetComponent<Image>();
                floatInputFieldStorage.addButton.image.sprite = SpriteHelper.LoadSprite(RTFile.GetAsset("editor_gui_add.png"));
                var addButtonLayout = floatInputFieldStorage.addButton.GetComponent<LayoutElement>();
                addButtonLayout.minWidth = 32f;
                addButtonLayout.preferredWidth = 32f;
                floatInputFieldStorage.addButton.gameObject.SetActive(false);

                prefabHolder.StringInputField = floatInputFieldStorage.inputField.gameObject.Duplicate(prefabHolder.PrefabParent, "string input");

                prefabHolder.Function2Button = ObjEditor.inst.ObjectView.transform.Find("applyprefab").gameObject.Duplicate(prefabHolder.PrefabParent, "function 2 button");

                var functionButtonStorage = prefabHolder.Function2Button.AddComponent<FunctionButtonStorage>();
                functionButtonStorage.button = prefabHolder.Function2Button.GetComponent<Button>();
                functionButtonStorage.label = prefabHolder.Function2Button.transform.GetChild(0).GetComponent<Text>();
                Destroy(prefabHolder.Function2Button.GetComponent<Animator>());
                functionButtonStorage.button.transition = Selectable.Transition.ColorTint;

                prefabHolder.Dropdown = ObjEditor.inst.ObjectView.transform.Find("autokill/tod-dropdown").gameObject.Duplicate(prefabHolder.PrefabParent, "dropdown");
                var dropdownStorage = prefabHolder.Dropdown.AddComponent<DropdownStorage>();

                dropdownStorage.dropdown = prefabHolder.Dropdown.GetComponent<Dropdown>();
                dropdownStorage.templateGrid = dropdownStorage.dropdown.template.Find("Viewport/Content").gameObject.AddComponent<GridLayoutGroup>();
                dropdownStorage.templateGrid.cellSize = new Vector2(1000f, 32f);
                dropdownStorage.templateGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                dropdownStorage.templateGrid.constraintCount = 1;

                dropdownStorage.templateFitter = dropdownStorage.templateGrid.gameObject.AddComponent<ContentSizeFitter>();
                dropdownStorage.templateFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
                dropdownStorage.dropdown.options.Clear();
                dropdownStorage.hideOptions = prefabHolder.Dropdown.GetComponent<HideDropdownOptions>();
                dropdownStorage.hideOptions.DisabledOptions.Clear();
                if (prefabHolder.Dropdown.TryGetComponent(out HoverTooltip dropdownHoverTooltip))
                    dropdownHoverTooltip.tooltipLangauges.Clear();

                dropdownStorage.arrow = prefabHolder.Dropdown.transform.Find("Arrow").GetComponent<Image>();

                prefabHolder.Labels = ObjEditor.inst.ObjectView.transform.ChildList().First(x => x.name == "label").gameObject.Duplicate(prefabHolder.PrefabParent, "label");
                if (prefabHolder.Labels.transform.childCount > 1)
                    CoreHelper.Destroy(prefabHolder.Labels.transform.GetChild(1).gameObject, true);

                prefabHolder.CollapseToggle = ObjEditor.inst.ObjectView.transform.Find("autokill/collapse").gameObject.Duplicate(prefabHolder.PrefabParent, "collapse");

                prefabHolder.ScrollView = ObjEditor.inst.ObjectView.transform.parent.parent.gameObject.Duplicate(prefabHolder.PrefabParent, "Scroll View");
                CoreHelper.DestroyChildren(prefabHolder.ScrollView.transform.Find("Viewport/Content"));

                prefabHolder.CurvesDropdown = ObjEditor.inst.KeyframeDialogs[0].transform.Find("curves").gameObject.Duplicate(prefabHolder.PrefabParent, "curves");
                var curvesDropdownStorage = prefabHolder.CurvesDropdown.AddComponent<DropdownStorage>();

                curvesDropdownStorage.dropdown = prefabHolder.CurvesDropdown.GetComponent<Dropdown>();
                curvesDropdownStorage.templateGrid = curvesDropdownStorage.dropdown.template.Find("Viewport/Content").gameObject.AddComponent<GridLayoutGroup>();
                curvesDropdownStorage.templateGrid.cellSize = new Vector2(1000f, 32f);
                curvesDropdownStorage.templateGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                curvesDropdownStorage.templateGrid.constraintCount = 1;

                curvesDropdownStorage.templateFitter = curvesDropdownStorage.templateGrid.gameObject.AddComponent<ContentSizeFitter>();
                curvesDropdownStorage.templateFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
                curvesDropdownStorage.dropdown.options.Clear();
                curvesDropdownStorage.hideOptions = prefabHolder.CurvesDropdown.AddComponent<HideDropdownOptions>();
                curvesDropdownStorage.hideOptions.DisabledOptions.Clear();
                if (prefabHolder.CurvesDropdown.TryGetComponent(out HoverTooltip curvesDropdownHoverTooltip))
                    curvesDropdownHoverTooltip.tooltipLangauges.Clear();

                curvesDropdownStorage.arrow = prefabHolder.CurvesDropdown.transform.Find("Arrow").GetComponent<Image>();

                prefabHolder.ColorsLayout = ObjEditor.inst.KeyframeDialogs[3].transform.Find("color").gameObject.Duplicate(prefabHolder.PrefabParent, "color");
                prefabHolder.Slider = ObjEditor.inst.ObjectView.transform.Find("depth/depth").gameObject.Duplicate(prefabHolder.PrefabParent, "Slider");
            }

            if (PrefabEditor.inst)
            {
                prefabHolder.DeleteButton = PrefabEditor.inst.AddPrefab.transform.Find("delete").gameObject.Duplicate(prefabHolder.PrefabParent, "delete");
                var deleteButtonStorage = prefabHolder.DeleteButton.AddComponent<DeleteButtonStorage>();
                deleteButtonStorage.Assign(prefabHolder.DeleteButton);

                prefabHolder.Tag = Creator.NewUIObject("Tag", transform);
                var tagPrefabImage = prefabHolder.Tag.AddComponent<Image>();
                tagPrefabImage.color = new Color(1f, 1f, 1f, 1f);
                var tagPrefabLayout = prefabHolder.Tag.AddComponent<HorizontalLayoutGroup>();
                tagPrefabLayout.childControlWidth = false;
                tagPrefabLayout.childForceExpandWidth = false;

                var input = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(prefabHolder.Tag.transform, "Input");
                input.transform.localScale = Vector3.one;
                input.transform.AsRT().sizeDelta = new Vector2(136f, 32f);
                var text = input.transform.Find("Text").GetComponent<Text>();
                text.alignment = TextAnchor.MiddleCenter;
                text.fontSize = 17;

                var delete = prefabHolder.DeleteButton.Duplicate(prefabHolder.Tag.transform, "Delete");
                new RectValues(Vector2.zero, Vector2.one, Vector2.one, Vector2.one, new Vector2(32f, 32f)).AssignToRectTransform(delete.transform.AsRT());
            }

            prefabHolder.Toggle = EditorManager.inst.GetDialog("Settings Editor").Dialog.Find("snap/toggle/toggle").gameObject.Duplicate(prefabHolder.PrefabParent, "toggle");

            prefabHolder.Function1Button = timelineBar.transform.Find("event").gameObject.Duplicate(prefabHolder.PrefabParent, "function 1 button");
            var functionButton1Storage = prefabHolder.Function1Button.AddComponent<FunctionButtonStorage>();
            functionButton1Storage.button = prefabHolder.Function1Button.GetComponent<Button>();
            functionButton1Storage.button.onClick.ClearAll();
            functionButton1Storage.label = prefabHolder.Function1Button.transform.GetChild(0).GetComponent<Text>();

            prefabHolder.ToggleButton = EditorManager.inst.GetDialog("Event Editor").Dialog.Find("data/right/grain/colored").gameObject.Duplicate(prefabHolder.PrefabParent, "toggle button");
            var toggleButtonStorage = prefabHolder.ToggleButton.AddComponent<ToggleButtonStorage>();
            toggleButtonStorage.Assign(prefabHolder.ToggleButton);

            prefabHolder.SpriteButton = timelineBar.transform.Find("play").gameObject.Duplicate(prefabHolder.PrefabParent, "Sprite Button");
            DestroyImmediate(prefabHolder.SpriteButton.GetComponent<Animator>());
            prefabHolder.SpriteButton.GetComponent<Button>().transition = Selectable.Transition.ColorTint;

            var openFilePopup = EditorManager.inst.GetDialog("Open File Popup").Dialog;
            prefabHolder.CloseButton = openFilePopup.Find("Panel/x").gameObject.Duplicate(prefabHolder.PrefabParent, "x");
            prefabHolder.Scrollbar = openFilePopup.Find("Scrollbar").gameObject.Duplicate(prefabHolder.PrefabParent, "Scrollbar");

            EditorTimeline.inst.binPrefab = EditorTimeline.inst.bins.GetChild(0).gameObject.Duplicate(prefabHolder.PrefabParent, "bin");

            prefabHolder.Dialog = EditorManager.inst.GetDialog("Multi Keyframe Editor (Object)").Dialog.gameObject.Duplicate(prefabHolder.PrefabParent, "Dialog");
            var dialogStorage = prefabHolder.Dialog.AddComponent<EditorDialogStorage>();
            dialogStorage.topPanel = prefabHolder.Dialog.transform.GetChild(0).GetComponent<Image>();
            dialogStorage.title = dialogStorage.topPanel.transform.GetChild(0).GetComponent<Text>();
        }

        // 5 - setup misc editor UI
        void InitUI()
        {
            SetupNotificationValues();
            SetupTimelineBar();
            SetupTimelineTriggers();
            SetupSelectGUI();
            SetupCreateObjects();
            SetupDropdowns();
            SetupDoggo();
            SetupPaths();
            SetupTimelinePreview();
            SetupTimelineElements();
            SetupGrid();
            SetupTimelineGrid();
            SetupNewFilePopup();
            CreatePreviewCover();
            CreateObjectSearch();
            CreateDebug();
            CreateAutosavePopup();
            CreateScreenshotsView();
            CreateFontSelector();
        }

        // 6 - initialize editors
        void InitEditors()
        {
            RTSettingEditor.Init();
            RTMarkerEditor.Init();
            RTMetaDataEditor.Init();
            RTEventEditor.Init();
            RTThemeEditor.Init();
            RTPrefabEditor.Init();
            ObjectEditor.Init();

            MultiObjectEditor.Init();
            TextEditor.Init();
            KeybindEditor.Init();
            PlayerEditor.Init();
            ObjectModifiersEditor.Init();
            LevelCombiner.Init();
            ProjectPlanner.Init();
            UploadedLevelsManager.Init();

            EditorDocumentation.Init();
        }

        // 7 - finalization
        void FinalSetup()
        {
            mousePicker = Creator.NewUIObject("picker", EditorManager.inst.dialogs.parent);
            mousePicker.transform.localScale = Vector3.one;
            mousePickerRT = mousePicker.transform.AsRT();

            var img = Creator.NewUIObject("image", mousePickerRT);
            img.transform.localScale = Vector3.one;

            img.transform.AsRT().anchoredPosition = new Vector2(-930f, -520f);
            img.transform.AsRT().sizeDelta = new Vector2(32f, 32f);

            var image = img.AddComponent<Image>();

            image.sprite = EditorSprites.DropperSprite;

            timelineTime = EditorManager.inst.timelineTime.GetComponent<Text>();
            SetNotificationProperties();

            EditorTimeline.inst.timelineSlider = EditorManager.inst.timelineSlider.GetComponent<Slider>();
            TriggerHelper.AddEventTriggers(EditorTimeline.inst.timelineSlider.gameObject, TriggerHelper.CreateEntry(EventTriggerType.PointerDown, eventData =>
            {
                if (!EditorConfig.Instance.DraggingMainCursorFix.Value)
                    return;

                EditorTimeline.inst.changingTime = true;
                EditorTimeline.inst.newTime = EditorTimeline.inst.timelineSlider.value / EditorManager.inst.Zoom;
                AudioManager.inst.SetMusicTime(Mathf.Clamp(EditorTimeline.inst.timelineSlider.value / EditorManager.inst.Zoom, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
            }), TriggerHelper.CreateEntry(EventTriggerType.PointerUp, eventData =>
            {
                if (!EditorConfig.Instance.DraggingMainCursorFix.Value)
                    return;

                EditorTimeline.inst.newTime = EditorTimeline.inst.timelineSlider.value / EditorManager.inst.Zoom;
                AudioManager.inst.SetMusicTime(Mathf.Clamp(EditorTimeline.inst.timelineSlider.value / EditorManager.inst.Zoom, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
                EditorTimeline.inst.changingTime = false;
            }));
            EditorTimeline.inst.timelineSlider.onValueChanged.AddListener(x =>
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
            EditorTimeline.inst.timelineObjectsParent = timelineParent.transform.AsRT();
            RectValues.FullAnchored.AssignToRectTransform(EditorTimeline.inst.timelineObjectsParent);

            try
            {
                CheckpointEditorDialog = new EditorDialog(EditorDialog.CHECKPOINT_EDITOR);
                CheckpointEditorDialog.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog

            SetupFileBrowser();
            CreateFolderCreator();
            CreateWarningPopup();
            EditorContextMenu.Init();

            SetupMiscEditorThemes();
        }

        #endregion

        #endregion

        #region Tick Update

        void Update()
        {
            editorInfo?.timer.Update();

            UpdatePicker();
            UpdateTooltip();
            EditorTimeline.inst.UpdateBinControls();
            EditorTimeline.inst.UpdateTimeChange();
            UpdatePreview();
            UpdateKey();

            if (RTEditor.inst.editorInfo.time > 36000f)
                AchievementManager.inst.UnlockAchievement("serious_dedication");
            if (RTEditor.inst.editorInfo.time > 86400f)
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

        void UpdatePicker()
        {
            if (Input.GetMouseButtonDown(1) && (parentPickerEnabled || prefabPickerEnabled || EditorTimeline.inst.onSelectTimelineObject != null))
            {
                parentPickerEnabled = false;
                prefabPickerEnabled = false;
                EditorTimeline.inst.onSelectTimelineObject = null;
            }

            var pickerActive = parentPickerEnabled || prefabPickerEnabled || EditorTimeline.inst.onSelectTimelineObject != null;
            mousePicker?.SetActive(pickerActive);

            if (mousePicker && mousePickerRT && pickerActive)
                mousePickerRT.anchoredPosition = Input.mousePosition * CoreHelper.ScreenScaleInverse;
        }

        void UpdateTooltip()
        {
            tooltipTime = Time.time - tooltipTimeOffset;

            if (!mouseTooltip)
                return;

            if (showTootip && tooltipTime >= EditorConfig.Instance.MouseTooltipHoverTime.Value)
            {
                showTootip = false;
                tooltipActive = true;

                mouseTooltip.SetActive(true);

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
                mouseTooltip.SetActive(false);
            }

            if (!EditorConfig.Instance.MouseTooltipDisplay.Value || !EditorManager.inst.showHelp && EditorConfig.Instance.MouseTooltipRequiresHelp.Value)
                mouseTooltip.SetActive(false);
        }

        void UpdatePreview()
        {
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
                    if (RTGameManager.inst.checkpointImages.Count > i)
                        checkpointImages[i].color = RTGameManager.inst.checkpointImages[i].color;
                }

                timelinePreviewPlayer.color = RTGameManager.inst.timelinePlayer.color;
                timelinePreviewLeftCap.color = RTGameManager.inst.timelineLeftCap.color;
                timelinePreviewRightCap.color = RTGameManager.inst.timelineRightCap.color;
                timelinePreviewLine.color = RTGameManager.inst.timelineLine.color;
            }
        }

        void UpdateKey()
        {
            if (!selectingKey)
                return;

            var key = CoreHelper.GetKeyCodeDown();

            if (key == KeyCode.None)
                return;

            selectingKey = false;

            setKey?.Invoke(key);
        }

        #endregion

        #region Constants

        /// <summary>
        /// Default object cameo
        /// </summary>
        public const string DEFAULT_OBJECT_NAME = "\"Default object cameo\" -Viral Mecha";

        /// <summary>
        /// Represents the local system browser (e.g. File Explorer)
        /// </summary>
        public const string SYSTEM_BROWSER = "System Browser";

        /// <summary>
        /// Represents the built-in file browser.
        /// </summary>
        public const string EDITOR_BROWSER = "Editor Browser";

        #endregion

        #region Variables

        #region Misc

        /// <summary>
        /// A list of easing dropdowns.
        /// </summary>
        public static List<Dropdown> EasingDropdowns { get; set; } = new List<Dropdown>();

        /// <summary>
        /// If advanced features should display.
        /// </summary>
        public static bool ShowModdedUI { get; set; }

        /// <summary>
        /// If the editor shouldn't be simple.
        /// </summary>
        public static bool NotSimple => EditorConfig.Instance.EditorComplexity.Value != Complexity.Simple;

        /// <summary>
        /// Hides the preview area until a level is loaded.
        /// </summary>
        public EditorThemeManager.Element PreviewCover { get; set; }

        /// <summary>
        /// Grid of the preview area.
        /// </summary>
        public GridRenderer previewGrid;

        public GameObject timeDefault;

        public bool ienumRunning;

        /// <summary>
        /// The top panel of the editor with the dropdowns.
        /// </summary>
        public Transform titleBar;

        public InputField folderCreatorName;
        Text folderCreatorTitle;
        Text folderCreatorNameLabel;
        Button folderCreatorSubmit;
        Text folderCreatorSubmitText;

        GameObject fontSelectionPrefab;

        #endregion

        #region Levels

        public Level CurrentLevel { get; set; }
        public List<LevelPanel> LevelPanels { get; set; } = new List<LevelPanel>();

        public bool fromNewLevel;

        public string newLevelSongTitle = "Inertia";

        public bool shouldCutLevel;
        public string copiedLevelPath;

        #endregion

        #region Popups

        public Transform popups;

        public List<EditorPopup> editorPopups = new List<EditorPopup>();

        public EditorPopup NewLevelPopup { get; set; }

        public ContentPopup OpenLevelPopup { get; set; }

        public InfoPopup InfoPopup { get; set; }

        public ContentPopup ParentSelectorPopup { get; set; }

        public EditorPopup SaveAsPopup { get; set; }

        public PrefabPopup PrefabPopups { get; set; }

        public EditorPopup ObjectOptionsPopup { get; set; }
        public EditorPopup BGObjectOptionsPopup { get; set; }

        public ContentPopup ObjectTemplatePopup { get; set; }

        public ContentPopup DebuggerPopup { get; set; }

        public ContentPopup AutosavePopup { get; set; }
        public ContentPopup PlayerModelsPopup { get; set; }

        public ContentPopup DocumentationPopup { get; set; }

        public EditorPopup WarningPopup { get; set; }

        public EditorPopup BrowserPopup { get; set; }

        public ContentPopup ObjectSearchPopup { get; set; }

        public EditorPopup NamePopup { get; set; }

        public ContentPopup PrefabTypesPopup { get; set; }

        public ContentPopup ThemesPopup { get; set; }

        public EditorPopup TextEditorPopup { get; set; }
        public ContentPopup FontSelectorPopup { get; set; }

        public ContentPopup KeybindListPopup { get; set; }

        #endregion

        #region Dragging

        public float dragOffset = -1f;
        public int dragBinOffset = -100;

        public static bool DraggingPlaysSound { get; set; }
        public static bool DraggingPlaysSoundBPM { get; set; }

        #endregion

        #region Loading & sorting

        public bool canUpdateThemes = true;
        public bool canUpdatePrefabs = true;

        public Dropdown levelOrderDropdown;
        public Toggle levelAscendToggle;
        public InputField editorPathField;
        public InputField themePathField;
        public InputField prefabPathField;

        #endregion

        #region Mouse Picker & Tooltip

        // tootlip

        public bool showTootip;

        public TextMeshProUGUI tooltipText;

        public bool tooltipActive;
        public float tooltipTime;
        public float tooltipTimeOffset;
        public float maxTooltipTime = 2f;

        public GameObject mouseTooltip;
        public RectTransform mouseTooltipRT;
        public TextMeshProUGUI mouseTooltipText;

        // picker

        public GameObject mousePicker;
        RectTransform mousePickerRT;

        public bool parentPickerEnabled;
        public bool prefabPickerEnabled;
        public bool selectingMultiple;

        #endregion

        #region Timeline Bar

        /// <summary>
        /// The main editor toolbar.
        /// </summary>
        public GameObject timelineBar;

        /// <summary>
        /// The modded song time field.
        /// </summary>
        public InputField timeField;

        /// <summary>
        /// The vanilla song time button text.
        /// </summary>
        public Text timelineTime;

        /// <summary>
        /// The modded audio pitch field.
        /// </summary>
        public InputField pitchField;

        /// <summary>
        /// The modded editor layer field.
        /// </summary>
        public InputField editorLayerField;

        /// <summary>
        /// The modded editor layer fields' image.
        /// </summary>
        public Image editorLayerImage;

        /// <summary>
        /// The event layer toggle. If on, renders <see cref="LayerType.Events"/>, otherwise renders <see cref="LayerType.Objects"/>.
        /// </summary>
        public Toggle eventLayerToggle;

        #endregion

        #region Key selection

        /// <summary>
        /// If keyboard is currently being checked for any input.
        /// </summary>
        public bool selectingKey = false;

        /// <summary>
        /// Action to run when a key is selected
        /// </summary>
        public Action<KeyCode> setKey;

        #endregion

        #region Game Timeline

        public Image timelinePreviewPlayer;
        public Image timelinePreviewLine;
        public Image timelinePreviewLeftCap;
        public Image timelinePreviewRightCap;
        public List<Image> checkpointImages = new List<Image>();

        public Transform timelinePreview;
        public RectTransform timelinePosition;

        #endregion

        #region Debugger

        public List<string> debugs = new List<string>();
        public List<GameObject> customFunctions = new List<GameObject>();
        public string debugSearch;

        #endregion

        #region Autosave

        public bool autosaving;

        public string autosaveSearch;

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

        #endregion

        #region Settings

        public EditorInfo editorInfo = new EditorInfo();

        /// <summary>
        /// Saves the current editor settings.
        /// </summary>
        public void SaveSettings()
        {
            if (!editorInfo)
                editorInfo = new EditorInfo();

            editorInfo.ApplyFrom();
            RTFile.WriteToFile(RTFile.CombinePaths(RTFile.BasePath, Level.EDITOR_LSE), editorInfo.ToJSON().ToString(3));
        }

        /// <summary>
        /// Loads the current levels' editor settings.
        /// </summary>
        public void LoadSettings()
        {
            EditorTimeline.inst.pinnedEditorLayers.Clear();

            if (!RTFile.FileExists(RTFile.CombinePaths(RTFile.BasePath, Level.EDITOR_LSE)))
            {
                editorInfo = new EditorInfo();
                editorInfo.timer.Reset();

                return;
            }

            var jn = JSON.Parse(RTFile.ReadFromFile(RTFile.CombinePaths(RTFile.BasePath, Level.EDITOR_LSE)));

            try
            {
                editorInfo = EditorInfo.Parse(jn);
                editorInfo.ApplyTo();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            EditorTimeline.inst.prevLayer = EditorTimeline.inst.Layer;
            EditorTimeline.inst.prevLayerType = EditorTimeline.inst.layerType;

            EditorTimeline.inst.SetTimelineGridSize();
        }

        #endregion

        #region Notifications

        /// <summary>
        /// List of all current notifications.
        /// </summary>
        public List<string> notifications = new List<string>();

        /// <summary>
        /// The parent for the notifications.
        /// </summary>
        public RectTransform notificationsParent;

        /// <summary>
        /// Displays an editor notification.
        /// </summary>
        /// <param name="name">Name of the notification.</param>
        /// <param name="text">Text to display.</param>
        /// <param name="time">Time the notification should be on screen for.</param>
        /// <param name="type">Type of notification to spawn.</param>
        public void DisplayNotification(string name, string text, float time, EditorManager.NotificationType type) => StartCoroutine(DisplayNotificationLoop(name, text, time, type));

        /// <summary>
        /// Displays a custom notification.
        /// </summary>
        /// <param name="name">Name of the notification.</param>
        /// <param name="text">Text to display.</param>
        /// <param name="time">Time the notification should be on screen for.</param>
        /// <param name="baseColor">Color of the notification base.</param>
        /// <param name="panelColor">Color of the notification panel.</param>
        /// <param name="iconColor">Color of the notification icon.</param>
        /// <param name="title">Title of the notification.</param>
        /// <param name="icon">Icon of the notification.</param>
        public void DisplayCustomNotification(string name, string text, float time, Color baseColor, Color panelColor, Color iconColor, string title, Sprite icon = null) => StartCoroutine(DisplayCustomNotificationLoop(name, text, time, baseColor, panelColor, iconColor, title, icon));

        /// <summary>
        /// Rebuilds the notification layout in case it breaks.
        /// </summary>
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

        /// <summary>
        /// Updates the notification settings.
        /// </summary>
        public void UpdateNotificationConfig()
        {
            var notifyGroup = EditorManager.inst.notification.GetComponent<VerticalLayoutGroup>();
            notificationsParent.sizeDelta = new Vector2(EditorConfig.Instance.NotificationWidth.Value, 632f);
            EditorManager.inst.notification.transform.localScale = new Vector3(EditorConfig.Instance.NotificationSize.Value, EditorConfig.Instance.NotificationSize.Value, 1f);

            var direction = EditorConfig.Instance.NotificationDirection.Value;

            notificationsParent.anchoredPosition = new Vector2(8f, direction == VerticalDirection.Up ? 408f : 410f);
            notifyGroup.childAlignment = direction != VerticalDirection.Up ? TextAnchor.LowerLeft : TextAnchor.UpperLeft;
        }

        #region Internal

        IEnumerator DisplayNotificationLoop(string name, string text, float time, EditorManager.NotificationType type)
        {
            Debug.Log("<color=#F6AC1A>Editor</color><color=#2FCBD6>Management</color>\nNotification: " + name + "\nText: " + text + "\nTime: " + time + "\nType: " + type);

            if (!notifications.Contains(name) && notifications.Count < 20 && EditorConfig.Instance.NotificationsDisplay.Value)
            {
                var notif = Instantiate(EditorManager.inst.notificationPrefabs[(int)type], Vector3.zero, Quaternion.identity);
                Destroy(notif, time * EditorConfig.Instance.NotificationDisplayTime.Value);

                Graphic textComponent = type == EditorManager.NotificationType.Info ? notif.transform.Find("text").GetComponent<TextMeshProUGUI>() : notif.transform.Find("text").GetComponent<Text>();

                if (type == EditorManager.NotificationType.Info)
                    ((TextMeshProUGUI)textComponent).text = text;
                else
                    ((Text)textComponent).text = text;

                notif.transform.SetParent(EditorManager.inst.notification.transform);
                if (EditorConfig.Instance.NotificationDirection.Value == VerticalDirection.Down)
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

        IEnumerator DisplayCustomNotificationLoop(string name, string text, float time, Color baseColor, Color topColor, Color iconCOlor, string _title, Sprite _icon = null)
        {
            if (!notifications.Contains(name) && notifications.Count < 20 && EditorConfig.Instance.NotificationsDisplay.Value)
            {
                notifications.Add(name);
                var gameObject = Instantiate(EditorManager.inst.notificationPrefabs[0], Vector3.zero, Quaternion.identity);
                Destroy(gameObject, time * EditorConfig.Instance.NotificationDisplayTime.Value);
                gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>().text = text;
                gameObject.transform.SetParent(EditorManager.inst.notification.transform);
                if (EditorConfig.Instance.NotificationDirection.Value == VerticalDirection.Down)
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
            var tooltip = EditorManager.inst.tooltip.transform.parent.gameObject;
            EditorThemeManager.AddGraphic(tooltip.GetComponent<Image>(), ThemeGroup.Notification_Background, true);
            EditorThemeManager.AddGraphic(tooltip.transform.Find("bg/bg").GetComponent<Image>(), ThemeGroup.Notification_Info, true, roundedSide: SpriteHelper.RoundedSide.Top);
            EditorThemeManager.AddLightText(tooltipText);
            EditorThemeManager.AddGraphic(tooltip.transform.Find("bg/Image").GetComponent<Image>(), ThemeGroup.Light_Text);
            EditorThemeManager.AddLightText(tooltip.transform.Find("bg/title").GetComponent<Text>());

            UpdateNotificationConfig();
        }

        #endregion

        #endregion

        #region Paths

        #region Values

        public const string DEFAULT_EXPORTS_PATH = "beatmaps/exports";

        /// <summary>
        /// Watches the current prefab folder for changes. If any changes are made, update the prefab list.
        /// </summary>
        public FileSystemWatcher PrefabWatcher { get; set; }

        /// <summary>
        /// Watches the current theme folder for changes. If any changes are made, update the theme list.
        /// </summary>
        public FileSystemWatcher ThemeWatcher { get; set; }

        /// <summary>
        /// The level sort.
        /// </summary>
        public LevelSort levelSort = 0;

        /// <summary>
        /// If the level sort should ascend.
        /// </summary>
        public bool levelAscend = true;

        public static string EditorSettingsPath => $"{RTFile.ApplicationDirectory}settings/editor{FileFormat.LSS.Dot()}";

        /// <summary>
        /// The path editor levels should load from.
        /// </summary>
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

        /// <summary>
        /// The path themes should load from.
        /// </summary>
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

        /// <summary>
        /// The path prefabs should load from.
        /// </summary>
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

        #endregion

        #region Functions

        /// <summary>
        /// Updates the editor list and path.
        /// </summary>
        /// <param name="forceReload">If function should be forced to run.</param>
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

        /// <summary>
        /// Updates the theme list and path.
        /// </summary>
        /// <param name="forceReload">If function should be forced to run.</param>
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

                    StartCoroutine(RTThemeEditor.inst.LoadThemes(true));
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

            StartCoroutine(RTThemeEditor.inst.LoadThemes(true));
            EventEditor.inst.RenderEventsDialog();
        }

        /// <summary>
        /// Updates the prefab list and path.
        /// </summary>
        /// <param name="forceReload">If function should be forced to run.</param>
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

                    StartCoroutine(RTPrefabEditor.inst.UpdatePrefabs());

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

            StartCoroutine(RTPrefabEditor.inst.UpdatePrefabs());
        }

        /// <summary>
        /// Updates the level sort dropdown.
        /// </summary>
        public void UpdateOrderDropdown()
        {
            if (!levelOrderDropdown)
                return;

            levelOrderDropdown.onValueChanged.ClearAll();
            levelOrderDropdown.value = (int)levelSort;
            levelOrderDropdown.onValueChanged.AddListener(_val =>
            {
                levelSort = (LevelSort)_val;
                StartCoroutine(RefreshLevelList());
                SaveGlobalSettings();
            });
        }

        /// <summary>
        /// Updates the level ascend toggle.
        /// </summary>
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

        /// <summary>
        /// Creates the global settings if it doesn't exist.
        /// </summary>
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

        /// <summary>
        /// Loads the global editor settings.
        /// </summary>
        public void LoadGlobalSettings()
        {
            if (!RTFile.FileExists(EditorSettingsPath))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(EditorSettingsPath));

            if (!string.IsNullOrEmpty(jn["sort"]["asc"]))
                levelAscend = jn["sort"]["asc"].AsBool;
            if (!string.IsNullOrEmpty(jn["sort"]["order"]))
                levelSort = (LevelSort)jn["sort"]["order"].AsInt;

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

        /// <summary>
        /// Saves the global editor settings.
        /// </summary>
        public void SaveGlobalSettings()
        {
            var jn = JSON.Parse("{}");

            jn["sort"]["asc"] = levelAscend.ToString();
            jn["sort"]["order"] = ((int)levelSort).ToString();

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

        /// <summary>
        /// Disables the prefab file watcher.
        /// </summary>
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

        /// <summary>
        /// Enables the prefab file watcher.
        /// </summary>
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
        
        /// <summary>
        /// Disables the theme file watcher.
        /// </summary>
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

        /// <summary>
        /// Enables the theme file watcher.
        /// </summary>
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

        /// <summary>
        /// Updates the file watcher paths.
        /// </summary>
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

        #region Internal

        void InitFileWatchers()
        {
            try
            {
                RTFile.CreateDirectory(RTFile.ApplicationDirectory + editorListPath);
                RTFile.CreateDirectory(RTFile.ApplicationDirectory + prefabListPath);
                RTFile.CreateDirectory(RTFile.ApplicationDirectory + themeListPath);

                PrefabWatcher = new FileSystemWatcher
                {
                    Path = RTFile.ApplicationDirectory + prefabListPath,
                    Filter = FileFormat.LSP.ToPattern(),
                };
                EnablePrefabWatcher();

                ThemeWatcher = new FileSystemWatcher
                {
                    Path = RTFile.ApplicationDirectory + themeListPath,
                    Filter = FileFormat.LST.ToPattern()
                };
                EnableThemeWatcher();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
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
            StartCoroutine(RTThemeEditor.inst.LoadThemes(RTThemeEditor.inst.Dialog.GameObject.activeInHierarchy));
            yield break;
        }

        IEnumerator UpdatePrefabPath()
        {
            yield return Ninja.JumpToUnity;
            CoreHelper.Log($"------- [UPDATING PREFAB FILEWATCHER] -------");
            StartCoroutine(RTPrefabEditor.inst.UpdatePrefabs());
            yield break;
        }

        #endregion

        #endregion

        #endregion

        #region Objects

        // todo: clean this up bruh
        public void Duplicate(bool _regen = true) => Copy(false, true, _regen);

        public void Copy(bool _cut = false, bool _dup = false, bool _regen = true)
        {
            if (!EditorTimeline.inst.isOverMainTimeline && RTBackgroundEditor.inst.Dialog.IsCurrent)
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

            if (!EditorTimeline.inst.isOverMainTimeline && CheckpointEditorDialog.IsCurrent)
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

            if (!EditorTimeline.inst.isOverMainTimeline && ObjectEditor.inst.Dialog.IsCurrent)
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

            if (!EditorTimeline.inst.isOverMainTimeline)
                return;

            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
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

            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Objects)
            {
                var offsetTime = EditorTimeline.inst.SelectedObjects.Min(x => x.Time);

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
            if (EditorTimeline.inst.isOverMainTimeline && EditorTimeline.inst.layerType == EditorTimeline.LayerType.Objects)
                ObjectEditor.inst.PasteObject(_offsetTime, _regen);

            if (EditorTimeline.inst.isOverMainTimeline && EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
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
                                var timelineObject = new TimelineKeyframe(EventKeyframe.Parse(jn["events"][GameData.EventTypes[i]][j], i, GameData.DefaultKeyframes[i].values.Length));
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

            if (EditorTimeline.inst.isOverMainTimeline)
                return;

            if (!EditorTimeline.inst.isOverMainTimeline && ObjectEditor.inst.Dialog.IsCurrent)
            {
                ObjEditor.inst.PasteKeyframes();
                EditorManager.inst.DisplayNotification($"Pasted Object Keyframe{(ObjectEditor.inst.copiedObjectKeyframes.Count > 1 ? "s" : "")}", 1f, EditorManager.NotificationType.Success);
            }

            if (!EditorTimeline.inst.isOverMainTimeline && CheckpointEditorDialog.IsCurrent)
            {
                CheckpointEditor.inst.PasteCheckpoint();
                EditorManager.inst.DisplayNotification("Pasted Checkpoint", 1f, EditorManager.NotificationType.Success);
            }

            if (!EditorTimeline.inst.isOverMainTimeline && RTBackgroundEditor.inst.Dialog.IsCurrent)
            {
                BackgroundEditor.inst.PasteBackground();
                EditorManager.inst.DisplayNotification("Pasted Background Object", 1f, EditorManager.NotificationType.Success);
            }
        }

        public void Delete()
        {
            if (!EditorTimeline.inst.isOverMainTimeline && ObjectEditor.inst.Dialog.IsCurrent)
            {
                if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                    if (ObjEditor.inst.currentKeyframe != 0)
                    {
                        var list = new List<TimelineKeyframe>();
                        foreach (var timelineObject in EditorTimeline.inst.CurrentSelection.InternalTimelineObjects.Where(x => x.Selected))
                            list.Add(timelineObject);
                        var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();

                        EditorManager.inst.history.Add(new History.Command("Delete Keyframes", ObjectEditor.inst.DeleteKeyframes().Start, () => ObjectEditor.inst.PasteKeyframes(beatmapObject, list, false)));

                        StartCoroutine(ObjectEditor.inst.DeleteKeyframes());
                    }
                    else
                        EditorManager.inst.DisplayNotification("Can't delete first keyframe.", 1f, EditorManager.NotificationType.Error);
                return;
            }
            if (EditorTimeline.inst.isOverMainTimeline && EditorTimeline.inst.layerType == EditorTimeline.LayerType.Objects)
            {
                if (GameData.Current.beatmapObjects.Count > 1 && EditorTimeline.inst.SelectedObjectCount != GameData.Current.beatmapObjects.Count)
                {
                    var list = new List<TimelineObject>();
                    foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                        list.Add(timelineObject);

                    EditorDialog.CurrentDialog?.Close();

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

                    EditorManager.inst.history.Add(new History.Command("Delete Objects", Delete, () =>
                    {
                        EditorTimeline.inst.DeselectAllObjects();
                        StartCoroutine(ObjectEditor.inst.AddPrefabExpandedToLevel(prefab, true, 0f, true, retainID: true));
                    }));

                    StartCoroutine(ObjectEditor.inst.DeleteObjects());
                }
                else
                    EditorManager.inst.DisplayNotification("Can't delete only Beatmap Object", 1f, EditorManager.NotificationType.Error);
                return;
            }
            if (EditorTimeline.inst.isOverMainTimeline && EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
            {
                if (RTEventEditor.inst.SelectedKeyframes.Count > 0 && !RTEventEditor.inst.SelectedKeyframes.Has(x => x.Index == 0))
                {
                    EditorDialog.CurrentDialog?.Close();

                    var list = new List<TimelineKeyframe>();
                    foreach (var timelineObject in RTEventEditor.inst.SelectedKeyframes)
                        list.Add(timelineObject);

                    EditorManager.inst.history.Add(new History.Command("Delete Event Keyframes", RTEventEditor.inst.DeleteKeyframes(list).Start, () => RTEventEditor.inst.PasteEvents(list, false)));

                    StartCoroutine(RTEventEditor.inst.DeleteKeyframes());
                }
                else
                    EditorManager.inst.DisplayNotification("Can't delete first Event Keyframe.", 1f, EditorManager.NotificationType.Error);
                return;
            }

            if (RTBackgroundEditor.inst.Dialog.IsCurrent)
            {
                BackgroundEditor.inst.DeleteBackground(BackgroundEditor.inst.currentObj);
                return;
            }

            if (CheckpointEditorDialog.IsCurrent)
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

        #region Generate UI

        public static GameObject GenerateSpacer(string name, Transform parent, Vector2 size)
        {
            var spacer = Creator.NewUIObject(name, parent);
            spacer.transform.AsRT().sizeDelta = size;

            return spacer;
        }

        public static GameObject GenerateLabels(string name, Transform parent, params LabelSettings[] labels) => GenerateLabels(name, parent, -1, labels);

        public static GameObject GenerateLabels(string name, Transform parent, int siblingIndex, params LabelSettings[] labels)
        {
            var label = EditorPrefabHolder.Instance.Labels.Duplicate(parent, name, siblingIndex);
            var first = label.transform.GetChild(0);

            for (int i = 0; i < labels.Length; i++)
            {
                var labelSetting = labels[i];
                if (i >= label.transform.childCount)
                    first.gameObject.Duplicate(label.transform, first.name);

                var child = label.transform.GetChild(i);
                var labelText = child.GetComponent<Text>();
                labelSetting.Apply(labelText);

                EditorThemeManager.AddLightText(labelText);
            }

            return label;
        }

        /// <summary>
        /// Generates a content popup.
        /// </summary>
        /// <param name="name">Name of the editor popup.</param>
        /// <param name="title">Title to render.</param>
        /// <param name="defaultPosition">Default position to set.</param>
        /// <param name="size">Size of the editor popup.</param>
        /// <param name="refreshSearch">Function to run when the user types in the search field.</param>
        /// <param name="close">Function to run when the user closes the editor popup.</param>
        /// <param name="placeholderText">Search field placeholder text.</param>
        /// <returns>Returns a generated content popup.</returns>
        public ContentPopup GeneratePopup(string name, string title, Vector2 defaultPosition, Vector2 size, Action<string> refreshSearch = null, Action close = null, string placeholderText = "Search...")
        {
            var editorPopup = new ContentPopup(name, title, defaultPosition, size, refreshSearch, close, placeholderText);
            editorPopup.Init();
            editorPopups.Add(editorPopup);
            return editorPopup;
        }

        #region Internal

        void SetupTimelineBar()
        {
            if (EditorManager.inst.markerTimeline)
                EditorManager.inst.markerTimeline.SetActive(EditorConfig.Instance.ShowMarkers.Value);

            for (int i = 1; i <= 5; i++)
                timelineBar.transform.Find(i.ToString()).SetParent(transform);

            eventLayerToggle = timelineBar.transform.Find("6").GetComponent<Toggle>();
            Destroy(eventLayerToggle.GetComponent<EventTrigger>());

            var timeObj = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(timelineBar.transform, "Time Input", 0);
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

            editorLayerField.text = EditorTimeline.GetLayerString(EditorManager.inst.layer);

            editorLayerImage = editorLayerField.image;

            layersObj.AddComponent<ContrastColors>().Init(editorLayerField.textComponent, editorLayerImage);

            editorLayerField.characterValidation = InputField.CharacterValidation.None;
            editorLayerField.contentType = InputField.ContentType.Standard;
            editorLayerField.GetPlaceholderText().text = "Set layer...";
            editorLayerField.GetPlaceholderText().alignment = TextAnchor.MiddleCenter;
            editorLayerField.GetPlaceholderText().fontSize = 16;
            editorLayerField.GetPlaceholderText().horizontalOverflow = HorizontalWrapMode.Overflow;
            editorLayerField.onValueChanged.ClearAll();
            editorLayerField.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                    EditorTimeline.inst.SetLayer(Mathf.Clamp(num - 1, 0, int.MaxValue));
            });

            editorLayerImage.color = EditorTimeline.GetLayerColor(EditorManager.inst.layer);

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

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Options", ObjectOptionsPopup.Open),
                    new ButtonFunction("More Options", ObjectEditor.inst.ShowObjectTemplates)
                    );
            };

            var prefabContextMenu = prefabButton.AddComponent<ContextClickable>();
            prefabContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
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

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Open Marker Editor", () =>
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
                        if (EditorTimeline.inst.CurrentSelection && EditorTimeline.inst.CurrentSelection.TimelineReference != TimelineObject.TimelineReferenceType.Null)
                            RTMarkerEditor.inst.CreateNewMarker(EditorTimeline.inst.CurrentSelection.Time);
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Show Markers", () =>
                    {
                        EditorConfig.Instance.ShowMarkers.Value = true;
                        EditorManager.inst.DisplayNotification("Markers will now display.", 1.5f, EditorManager.NotificationType.Success);
                    }),
                    new ButtonFunction("Hide Markers", () =>
                    {
                        EditorConfig.Instance.ShowMarkers.Value = false;
                        EditorManager.inst.DisplayNotification("Markers will now be hidden.", 1.5f, EditorManager.NotificationType.Success);
                    })
                    );
            };

            var checkpointContextMenu = checkpointButton.AddComponent<ContextClickable>();
            checkpointContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Open Checkpoint Editor", () =>
                    {
                        if (Patchers.CheckpointEditorPatch.currentCheckpoint == null || !GameData.Current || CheckpointEditor.inst.currentObj < 0 || CheckpointEditor.inst.currentObj >= GameData.Current.data.checkpoints.Count)
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
                        if (EditorTimeline.inst.CurrentSelection && EditorTimeline.inst.CurrentSelection.TimelineReference != TimelineObject.TimelineReferenceType.Null)
                            CheckpointEditor.inst.CreateNewCheckpoint(EditorTimeline.inst.CurrentSelection.Time, Vector2.zero);
                    })
                    );
            };

            var playTestContextMenu = playTest.AddComponent<ContextClickable>();
            playTestContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Playtest", EditorManager.inst.ToggleEditor),
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

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Toggle Layer Type", () => EditorTimeline.inst.SetLayer(EditorTimeline.inst.Layer, EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events ? EditorTimeline.LayerType.Objects : EditorTimeline.LayerType.Events)),
                    new ButtonFunction("View Objects", () => EditorTimeline.inst.SetLayer(EditorTimeline.inst.Layer, EditorTimeline.LayerType.Objects)),
                    new ButtonFunction("View Events", () => EditorTimeline.inst.SetLayer(EditorTimeline.inst.Layer, EditorTimeline.LayerType.Events))
                    );
            };

            var layerContextMenu = layersObj.AddComponent<ContextClickable>();
            layerContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("List Layers with Objects", CoreHelper.ListObjectLayers),
                    new ButtonFunction("Next Free Layer", () =>
                    {
                        var layer = 0;
                        while (GameData.Current.beatmapObjects.Has(x => x.editorData && x.editorData.Layer == layer))
                            layer++;
                        EditorTimeline.inst.SetLayer(layer, EditorTimeline.LayerType.Objects);
                    }),
                    new ButtonFunction("Toggle Object Preview Visibility", () =>
                    {
                        EditorConfig.Instance.OnlyObjectsOnCurrentLayerVisible.Value = !EditorConfig.Instance.OnlyObjectsOnCurrentLayerVisible.Value;
                    })//,
                    //new ButtonFunction("Pin Editor Layer", () =>
                    //{

                    //})//,
                    //new ButtonFunction("View Pinned Editor Layers", () =>
                    //{

                    //})
                    );
            };
        }

        void SetupTimelineTriggers()
        {
            #region Bin Controls

            var binScroll = EditorPrefabHolder.Instance.Slider.Duplicate(EditorTimeline.inst.wholeTimeline, "Bin Scrollbar");
            var binScrollImage = binScroll.transform.Find("Image").GetComponent<Image>();
            EditorTimeline.inst.binSlider = binScroll.GetComponent<Slider>();
            EditorTimeline.inst.binSlider.onValueChanged.ClearAll();
            EditorTimeline.inst.binSlider.wholeNumbers = false;
            EditorTimeline.inst.binSlider.direction = Slider.Direction.TopToBottom;
            EditorTimeline.inst.binSlider.minValue = 0f;
            EditorTimeline.inst.binSlider.maxValue = 1f;
            EditorTimeline.inst.binSlider.value = 0f;
            EditorTimeline.inst.binSlider.onValueChanged.AddListener(_val =>
            {
                EditorTimeline.inst.BinScroll = EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events ? 0f : _val;
                EditorTimeline.inst.RenderBinPosition();
            });
            RectValues.Default.AnchoredPosition(960f, 134f).Pivot(1f, 1f).SizeDelta(32f, 268f).AssignToRectTransform(binScroll.transform.AsRT());
            RectValues.Default.AnchoredPosition(24f, 0f).AnchorMax(1f, 1f).AnchorMin(0f, 1f).Pivot(1f, 0.5f).SizeDelta(48f, 32f).AssignToRectTransform(EditorTimeline.inst.binSlider.handleRect);
            RectValues.FullAnchored.SizeDelta(0f, 32f).AssignToRectTransform(binScrollImage.rectTransform);

            EditorTimeline.inst.binSlider.colors = UIManager.SetColorBlock(EditorTimeline.inst.binSlider.colors, Color.white, new Color(0.9f, 0.9f, 0.9f), Color.white, Color.white, Color.white);

            TriggerHelper.AddEventTriggers(binScroll, TriggerHelper.CreateEntry(EventTriggerType.Scroll, eventData => EditorTimeline.inst.binSlider.value += ((PointerEventData)eventData).scrollDelta.y * -EditorConfig.Instance.BinControlScrollAmount.Value * 0.5f));

            #endregion

            TriggerHelper.AddEventTriggers(EditorManager.inst.timeline,
                TriggerHelper.CreateEntry(EventTriggerType.PointerEnter, eventData =>
                {
                    EditorTimeline.inst.isOverMainTimeline = true;
                    SetDialogStatus("Timeline", true);
                }),
                TriggerHelper.CreateEntry(EventTriggerType.PointerExit, eventData => EditorTimeline.inst.isOverMainTimeline = false),
                TriggerHelper.StartDragTrigger(), TriggerHelper.DragTrigger(), TriggerHelper.EndDragTrigger(),
                TriggerHelper.CreateEntry(EventTriggerType.PointerClick, eventData =>
                {
                    if (((PointerEventData)eventData).button != PointerEventData.InputButton.Right)
                        return;

                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Create New", () => ObjectEditor.inst.CreateNewNormalObject()),
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
                        new ButtonFunction("Paste", () => ObjectEditor.inst.PasteObject()),
                        new ButtonFunction("Duplicate", () =>
                        {
                            var offsetTime = EditorTimeline.inst.SelectedObjects.Min(x => x.Time);

                            ObjEditor.inst.CopyObject();
                            ObjectEditor.inst.PasteObject(offsetTime);
                        }),
                        new ButtonFunction("Paste (Keep Prefab)", () => ObjectEditor.inst.PasteObject(0f, false)),
                        new ButtonFunction("Duplicate (Keep Prefab)", () =>
                        {
                            var offsetTime = EditorTimeline.inst.SelectedObjects.Min(x => x.Time);

                            ObjEditor.inst.CopyObject();
                            ObjectEditor.inst.PasteObject(offsetTime, false);
                        }),
                        new ButtonFunction("Delete", ObjectEditor.inst.DeleteObjects().Start),
                        new ButtonFunction(true),
                        new ButtonFunction("Add Bin", EditorTimeline.inst.AddBin),
                        new ButtonFunction("Remove Bin", EditorTimeline.inst.RemoveBin),
                        new ButtonFunction("Set Max Bin Count", () => EditorTimeline.inst.SetBinCount(EditorTimeline.MAX_BINS)),
                        new ButtonFunction("Set Default Bin Count", () => EditorTimeline.inst.SetBinCount(EditorTimeline.DEFAULT_BIN_COUNT))
                        );
                }));

            for (int i = 0; i < EventEditor.inst.EventHolders.transform.childCount - 1; i++)
            {
                int type = i;
                var et = EventEditor.inst.EventHolders.transform.GetChild(i).GetComponent<EventTrigger>();
                et.triggers.Clear();
                et.triggers.Add(TriggerHelper.CreateEntry(EventTriggerType.PointerEnter, eventData => EditorTimeline.inst.isOverMainTimeline = true));
                et.triggers.Add(TriggerHelper.CreateEntry(EventTriggerType.PointerExit, eventData => EditorTimeline.inst.isOverMainTimeline = false));
                et.triggers.Add(TriggerHelper.StartDragTrigger());
                et.triggers.Add(TriggerHelper.DragTrigger());
                et.triggers.Add(TriggerHelper.EndDragTrigger());
                et.triggers.Add(TriggerHelper.CreateEntry(EventTriggerType.PointerDown, eventData =>
                {
                    var pointerEventData = (PointerEventData)eventData;

                    var currentEvent = (RTEventEditor.EVENT_LIMIT * (EditorTimeline.inst.Layer + 1)) - RTEventEditor.EVENT_LIMIT + type;

                    switch (pointerEventData.button)
                    {
                        case PointerEventData.InputButton.Right:
                                if (RTEventEditor.EventTypes.Length > currentEvent && (ShowModdedUI && GameData.Current.events.Count > currentEvent || 10 > currentEvent))
                                    RTEventEditor.inst.NewKeyframeFromTimeline(currentEvent);
                                break;
                        case PointerEventData.InputButton.Middle:
                            if (RTEventEditor.EventTypes.Length > currentEvent && (ShowModdedUI && GameData.Current.events.Count > currentEvent || 10 > currentEvent) && GameData.Current.events[currentEvent].TryFindLastIndex(x => x.time < EditorTimeline.inst.GetTimelineTime(false), out int index))
                                RTEventEditor.inst.SetCurrentEvent(currentEvent, index);
                            break;
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

            #region Editor Themes

            EditorThemeManager.AddGraphic(binScrollImage, ThemeGroup.Slider_2, true);
            EditorThemeManager.AddGraphic(EditorTimeline.inst.binSlider.image, ThemeGroup.Slider_2_Handle, true);

            EditorThemeManager.AddScrollbar(EditorManager.inst.timelineScrollbar.GetComponent<Scrollbar>(),
                scrollbarGroup: ThemeGroup.Timeline_Scrollbar_Base, handleGroup: ThemeGroup.Timeline_Scrollbar, canSetScrollbarRounded: false);

            EditorThemeManager.AddGraphic(EditorManager.inst.timelineSlider.transform.Find("Background").GetComponent<Image>(), ThemeGroup.Timeline_Time_Scrollbar);

            EditorThemeManager.AddGraphic(EditorTimeline.inst.wholeTimeline.GetComponent<Image>(), ThemeGroup.Timeline_Time_Scrollbar);

            var zoomSliderBase = EditorManager.inst.zoomSlider.transform.parent;
            EditorThemeManager.AddGraphic(zoomSliderBase.GetComponent<Image>(), ThemeGroup.Background_1, true);
            EditorThemeManager.AddGraphic(zoomSliderBase.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Slider_2);
            EditorThemeManager.AddGraphic(zoomSliderBase.transform.GetChild(2).GetComponent<Image>(), ThemeGroup.Slider_2);

            EditorThemeManager.AddGraphic(EditorManager.inst.zoomSlider.transform.Find("Background").GetComponent<Image>(), ThemeGroup.Slider_2, true);
            EditorThemeManager.AddGraphic(EditorManager.inst.zoomSlider.transform.Find("Fill Area/Fill").GetComponent<Image>(), ThemeGroup.Slider_2, true);
            EditorThemeManager.AddGraphic(EditorManager.inst.zoomSlider.image, ThemeGroup.Slider_2_Handle, true);

            #endregion

            EditorTimeline.inst.ClampTimeline(false);

            TriggerHelper.AddEventTriggers(EditorTimeline.inst.wholeTimeline.gameObject,
                TriggerHelper.CreateEntry(EventTriggerType.PointerEnter, eventData =>
                {
                    EditorTimeline.inst.isOverMainTimeline = true;
                    SetDialogStatus("Timeline", true);
                }),
                TriggerHelper.CreateEntry(EventTriggerType.PointerExit, eventData => EditorTimeline.inst.isOverMainTimeline = false),
                TriggerHelper.StartDragTrigger(), TriggerHelper.DragTrigger(), TriggerHelper.EndDragTrigger());
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
            persistent.onClick.AddListener(() => ObjectEditor.inst.CreateNewNoAutokillObject());

            var empty = dialog.Find("empty").gameObject.GetComponent<Button>();
            empty.onClick.ClearAll();
            empty.onClick.AddListener(() => ObjectEditor.inst.CreateNewEmptyObject());

            var decoration = dialog.Find("decoration").gameObject.GetComponent<Button>();
            decoration.onClick.ClearAll();
            decoration.onClick.AddListener(() => ObjectEditor.inst.CreateNewDecorationObject());

            var helper = dialog.Find("helper").gameObject.GetComponent<Button>();
            helper.onClick.ClearAll();
            helper.onClick.AddListener(() => ObjectEditor.inst.CreateNewHelperObject());

            var normal = dialog.Find("normal").gameObject.GetComponent<Button>();
            normal.onClick.ClearAll();
            normal.onClick.AddListener(() => ObjectEditor.inst.CreateNewNormalObject());

            var circle = dialog.Find("shapes/circle").gameObject.GetComponent<Button>();
            circle.onClick.ClearAll();
            circle.onClick.AddListener(() => ObjectEditor.inst.CreateNewCircleObject());

            var triangle = dialog.Find("shapes/triangle").gameObject.GetComponent<Button>();
            triangle.onClick.ClearAll();
            triangle.onClick.AddListener(() => ObjectEditor.inst.CreateNewTriangleObject());

            var text = dialog.Find("shapes/text").gameObject.GetComponent<Button>();
            text.onClick.ClearAll();
            text.onClick.AddListener(() => ObjectEditor.inst.CreateNewTextObject());

            var hexagon = dialog.Find("shapes/hexagon").gameObject.GetComponent<Button>();
            hexagon.onClick.ClearAll();
            hexagon.onClick.AddListener(() => ObjectEditor.inst.CreateNewHexagonObject());

            ObjectTemplatePopup = GeneratePopup(EditorPopup.OBJECT_TEMPLATES_POPUP, "Pick a template", Vector2.zero, new Vector2(600f, 400f), placeholderText: "Search for template...");
        }

        void SetupDropdowns()
        {
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

            var restartEditor = EditorHelper.AddEditorDropdown("Restart Editor", "", "File", EditorSprites.ReloadSprite, () =>
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
                BrowserPopup.Open();
                RefreshFileBrowserLevels();
            }, 3);
            EditorHelper.SetComplexity(openLevelBrowser, Complexity.Normal);

            var convertVGToLS = EditorHelper.AddEditorDropdown("Convert VG to LS", "", "File", EditorSprites.SearchSprite, ConvertVGToLS, 4);
            EditorHelper.SetComplexity(convertVGToLS, Complexity.Normal);

            var addFileToLevelFolder = EditorHelper.AddEditorDropdown("Add File to Level", "", "File", EditorSprites.SearchSprite, () =>
            {
                if (!EditorManager.inst.hasLoadedLevel)
                {
                    EditorManager.inst.DisplayNotification("Cannot add a file to level until a level has been loaded.", 4f, EditorManager.NotificationType.Warning);
                    return;
                }

                BrowserPopup.Open();
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

            var reloadLevel = EditorHelper.AddEditorDropdown("Reload Level", "", "File", EditorSprites.ReloadSprite, () =>
            {
                if (!EditorManager.inst.hasLoadedLevel)
                {
                    EditorManager.inst.DisplayNotification("Cannot reload a level without one already loaded.", 2f, EditorManager.NotificationType.Warning);
                    return;
                }

                ShowWarningPopup("Are you sure you want to reload the level?", () =>
                {
                    if (CurrentLevel)
                    {
                        if (GameData.Current)
                            GameData.Current.SaveData(RTFile.CombinePaths(CurrentLevel.path, "reload-level-backup.lsb"));
                        StartCoroutine(LoadLevel(CurrentLevel));
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

            var resetEventOffsets = EditorHelper.AddEditorDropdown("Reset Event Offsets", "", "Edit", EditorSprites.CloseSprite, () =>
            {
                RTEventManager.inst?.SetResetOffsets();

                EditorManager.inst.DisplayNotification("Event Offsets have been reset.", 1.4f, EditorManager.NotificationType.Success);
            });
            EditorHelper.SetComplexity(resetEventOffsets, Complexity.Advanced);

            var renderWaveform = EditorHelper.AddEditorDropdown("Render Waveform", "", "Edit", EditorSprites.ReloadSprite, () =>
            {
                if (EditorConfig.Instance.WaveformGenerate.Value)
                {
                    EditorTimeline.inst.SetTimelineSprite(null);
                    StartCoroutine(EditorTimeline.inst.AssignTimelineTexture());
                }
                else
                    EditorTimeline.inst.SetTimelineSprite(null);
            });
            EditorHelper.SetComplexity(renderWaveform, Complexity.Normal);

            var deactivateModifiers = EditorHelper.AddEditorDropdown("Deactivate Modifiers", "", "Edit", EditorSprites.CloseSprite, () =>
            {
                if (!GameData.Current)
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

            var resetObjectVariables = EditorHelper.AddEditorDropdown("Reset object variables", "", "Edit", EditorSprites.CloseSprite, () =>
            {
                if (!GameData.Current)
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

            EditorHelper.AddEditorDropdown("Get Example", "", "View", SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_example-white.png"), Example.Init);
            
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
            titleBar.Find("Help/Help Dropdown/Community Guides/Text").GetComponent<Text>().text = "Open Source";
            titleBar.Find("Help/Help Dropdown/Which songs can I use?").gameObject.SetActive(false);
            var saveAsDropdown = titleBar.Find("File/File Dropdown/Save As").gameObject;
            saveAsDropdown.SetActive(true);
            EditorHelper.SetComplexity(saveAsDropdown, Complexity.Normal);
        }

        void SetupDoggo()
        {
            var doggoBase = Creator.NewUIObject("loading base", InfoPopup.GameObject.transform);
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

            InfoPopup.Doggo = doggoObject.AddComponent<Image>();

            doggoObject.transform.AsRT().anchoredPosition = Vector2.zero;
            doggoObject.transform.AsRT().sizeDelta = new Vector2(122f, 122f);
            InfoPopup.Doggo.sprite = EditorManager.inst.loadingImage.sprite;

            InfoPopup.RenderSize(new Vector2(500f, 320f));
        }

        void SetupPaths()
        {
            var openFilePopup = OpenLevelPopup.GameObject.transform;
            var panel = OpenLevelPopup.TopPanel;

            var sortList = EditorPrefabHolder.Instance.Dropdown.Duplicate(panel, "level sort");
            new RectValues(new Vector2(-132f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), RectValues.CenterPivot, new Vector2(200f, 32f)).AssignToRectTransform(sortList.transform.AsRT());

            levelOrderDropdown = sortList.GetComponent<Dropdown>();
            EditorThemeManager.AddDropdown(levelOrderDropdown);

            var config = EditorConfig.Instance;

            TooltipHelper.RemoveTooltip(sortList);
            TooltipHelper.AssignTooltip(sortList, "Level Sort Dropdown");

            Destroy(sortList.GetComponent<HideDropdownOptions>());
            levelOrderDropdown.onValueChanged.ClearAll();
            levelOrderDropdown.options.Clear();
            levelOrderDropdown.options = CoreHelper.StringToOptionData("Cover", "Artist", "Creator", "Folder", "Title", "Difficulty", "Date Edited", "Date Created");
            levelOrderDropdown.value = (int)levelSort;
            levelOrderDropdown.onValueChanged.AddListener(_val =>
            {
                levelSort = (LevelSort)_val;
                StartCoroutine(RefreshLevelList());
                SaveGlobalSettings();
            });
            EditorHelper.SetComplexity(sortList, Complexity.Normal);

            var checkDes = EditorPrefabHolder.Instance.Toggle.Duplicate(panel, "ascend");
            new RectValues(new Vector2(-252f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), RectValues.CenterPivot, new Vector2(32f, 32f)).AssignToRectTransform(checkDes.transform.AsRT());

            TooltipHelper.RemoveTooltip(checkDes);
            TooltipHelper.AssignTooltip(checkDes, "Level Ascend Toggle");

            levelAscendToggle = checkDes.GetComponent<Toggle>();
            levelAscendToggle.onValueChanged.ClearAll();
            levelAscendToggle.isOn = levelAscend;
            levelAscendToggle.onValueChanged.AddListener(_val =>
            {
                levelAscend = _val;
                StartCoroutine(RefreshLevelList());
                SaveGlobalSettings();
            });

            EditorThemeManager.AddToggle(levelAscendToggle);

            EditorHelper.SetComplexity(checkDes, Complexity.Normal);

            var contextClickable = openFilePopup.gameObject.AddComponent<ContextClickable>();
            contextClickable.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Create folder", () =>
                    {
                        ShowFolderCreator($"{RTFile.ApplicationDirectory}{editorListPath}", () => { UpdateEditorPath(true); HideNameEditor(); });
                    }),
                    new ButtonFunction("Create level", EditorManager.inst.OpenNewLevelPopup),
                    new ButtonFunction("Paste", PasteLevel),
                    new ButtonFunction("Open List in File Explorer", OpenLevelListFolder));
            };

            #region Level Path

            var levelPathGameObject = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(openFilePopup, "editor path");
            ((RectTransform)levelPathGameObject.transform).anchoredPosition = config.OpenLevelEditorPathPos.Value;
            ((RectTransform)levelPathGameObject.transform).sizeDelta = new Vector2(config.OpenLevelEditorPathLength.Value, 32f);

            TooltipHelper.AssignTooltip(levelPathGameObject, "Editor Path", 3f);

            editorPathField = levelPathGameObject.GetComponent<InputField>();
            editorPathField.characterValidation = InputField.CharacterValidation.None;
            editorPathField.onValueChanged.ClearAll();
            editorPathField.onEndEdit.ClearAll();
            editorPathField.textComponent.alignment = TextAnchor.MiddleLeft;
            editorPathField.textComponent.fontSize = 16;
            editorPathField.text = EditorPath;
            editorPathField.onValueChanged.AddListener(_val => EditorPath = _val);
            editorPathField.onEndEdit.AddListener(_val => UpdateEditorPath(false));

            EditorThemeManager.AddInputField(editorPathField);

            var levelClickable = levelPathGameObject.AddComponent<Clickable>();
            levelClickable.onDown = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Set Level folder", () =>
                    {
                        BrowserPopup.Open();
                        RTFileBrowser.inst.UpdateBrowserFolder(_val =>
                        {
                            if (!_val.Replace("\\", "/").Contains(RTFile.ApplicationDirectory + "beatmaps/"))
                            {
                                EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                                return;
                            }

                            editorPathField.text = _val.Replace("\\", "/").Replace(RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/", "");
                            EditorManager.inst.DisplayNotification($"Set Editor path to {EditorPath}!", 2f, EditorManager.NotificationType.Success);
                            RTEditor.inst.BrowserPopup.Close();
                            UpdateEditorPath(false);
                        });
                    }),
                    new ButtonFunction("Open List in File Explorer", OpenLevelListFolder));
            };
            EditorHelper.SetComplexity(levelPathGameObject, Complexity.Advanced);

            var levelListReloader = EditorPrefabHolder.Instance.SpriteButton.Duplicate(openFilePopup, "reload");
            levelListReloader.transform.AsRT().anchoredPosition = config.OpenLevelListRefreshPosition.Value;
            levelListReloader.transform.AsRT().sizeDelta = new Vector2(32f, 32f);

            (levelListReloader.GetComponent<HoverTooltip>() ?? levelListReloader.AddComponent<HoverTooltip>()).tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                desc = "Refresh level list",
                hint = "Clicking this will reload the level list."
            });

            var levelListReloaderButton = levelListReloader.GetComponent<Button>();
            levelListReloaderButton.onClick.ClearAll();
            levelListReloaderButton.onClick.AddListener(() => UpdateEditorPath(true));

            EditorThemeManager.AddSelectable(levelListReloaderButton, ThemeGroup.Function_2, false);

            levelListReloaderButton.image.sprite = EditorSprites.ReloadSprite;

            #endregion

            #region Theme Path

            var themePathBase = EditorManager.inst.GetDialog("Event Editor").Dialog.Find("data/right/theme").GetChild(2).gameObject
                .Duplicate(EditorManager.inst.GetDialog("Event Editor").Dialog.Find("data/right/theme"), "themepathers", 8);

            var themePathGameObject = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(themePathBase.transform, "themes path");
            themePathGameObject.transform.AsRT().anchoredPosition = new Vector2(80f, 0f);
            themePathGameObject.transform.AsRT().sizeDelta = new Vector2(160f, 34f);

            TooltipHelper.AssignTooltip(themePathGameObject, "Theme Path", 3f);

            themePathField = themePathGameObject.GetComponent<InputField>();
            themePathField.characterValidation = InputField.CharacterValidation.None;
            themePathField.onValueChanged.ClearAll();
            themePathField.onEndEdit.ClearAll();
            themePathField.textComponent.alignment = TextAnchor.MiddleLeft;
            themePathField.textComponent.fontSize = 16;
            themePathField.text = ThemePath;
            themePathField.onValueChanged.AddListener(_val => ThemePath = _val);
            themePathField.onEndEdit.AddListener(_val => UpdateThemePath(false));

            EditorThemeManager.AddInputField(themePathField);

            var themeClickable = themePathGameObject.AddComponent<Clickable>();
            themeClickable.onDown = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Set Theme folder", () =>
                    {
                        BrowserPopup.Open();
                        RTFileBrowser.inst.UpdateBrowserFolder(_val =>
                        {
                            if (!_val.Replace("\\", "/").Contains(RTFile.ApplicationDirectory + "beatmaps/"))
                            {
                                EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                                return;
                            }

                            themePathField.text = _val.Replace("\\", "/").Replace(RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/", "");
                            EditorManager.inst.DisplayNotification($"Set Theme path to {ThemePath}!", 2f, EditorManager.NotificationType.Success);
                            BrowserPopup.Close();
                            UpdateThemePath(false);
                        });
                    }),
                    new ButtonFunction("Open List in File Explorer", OpenThemeListFolder),
                    new ButtonFunction("Set as Default for Level", () =>
                    {
                        editorInfo.themePath = themePath;
                        EditorManager.inst.DisplayNotification($"Set current theme folder [ {themePath} ] as the default for the level!", 5f, EditorManager.NotificationType.Success);
                    }, "Theme Default Path"),
                    new ButtonFunction("Remove Default", () =>
                    {
                        editorInfo.themePath = null;
                        EditorManager.inst.DisplayNotification($"Removed default theme folder.", 5f, EditorManager.NotificationType.Success);
                    }, "Theme Default Path"));
            };

            EditorHelper.SetComplexity(themePathGameObject, Complexity.Advanced);

            var themeListReload = EditorPrefabHolder.Instance.SpriteButton.Duplicate(themePathBase.transform, "reload themes");
            themeListReload.transform.AsRT().anchoredPosition = new Vector2(166f, 35f);
            themeListReload.transform.AsRT().sizeDelta = new Vector2(32f, 32f);

            (themeListReload.GetComponent<HoverTooltip>() ?? themeListReload.AddComponent<HoverTooltip>()).tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                desc = "Refresh theme list",
                hint = "Clicking this will reload the theme list."
            });

            var themeListReloadButton = themeListReload.GetComponent<Button>();
            themeListReloadButton.onClick.ClearAll();
            themeListReloadButton.onClick.AddListener(() => UpdateThemePath(true));

            EditorThemeManager.AddSelectable(themeListReloadButton, ThemeGroup.Function_2, false);

            themeListReloadButton.image.sprite = EditorSprites.ReloadSprite;

            var themePage = EditorPrefabHolder.Instance.NumberInputField.Duplicate(themePathBase.transform, "page");
            UIManager.SetRectTransform(themePage.transform.AsRT(), new Vector2(205f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 32f));
            var themePageStorage = themePage.GetComponent<InputFieldStorage>();
            RTThemeEditor.eventPageStorage = themePageStorage;
            themePage.GetComponent<HorizontalLayoutGroup>().spacing = 2f;
            themePageStorage.inputField.image.rectTransform.sizeDelta = new Vector2(60f, 32f);

            themePageStorage.inputField.GetComponent<LayoutElement>().minWidth = 60f;
            themePageStorage.inputField.onValueChanged.ClearAll();
            themePageStorage.inputField.text = "0";
            themePageStorage.inputField.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int p))
                {
                    RTThemeEditor.inst.eventThemePage = Mathf.Clamp(p, 0, RTThemeEditor.inst.ThemesCount / RTThemeEditor.eventThemesPerPage);

                    StartCoroutine(RTThemeEditor.inst.RenderThemeList(RTThemeEditor.inst.Dialog.SearchTerm));
                }
            });

            themePageStorage.leftGreaterButton.onClick.ClearAll();
            themePageStorage.leftGreaterButton.onClick.AddListener(() => themePageStorage.inputField.text = "0");

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
            themePageStorage.rightGreaterButton.onClick.AddListener(() => themePageStorage.inputField.text = (RTThemeEditor.inst.ThemesCount / RTThemeEditor.eventThemesPerPage).ToString());

            Destroy(themePageStorage.middleButton.gameObject);

            EditorThemeManager.AddInputField(themePageStorage.inputField);
            EditorThemeManager.AddSelectable(themePageStorage.leftGreaterButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(themePageStorage.leftButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(themePageStorage.rightButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(themePageStorage.rightGreaterButton, ThemeGroup.Function_2, false);

            #endregion

            #region Prefab Path

            var prefabPathGameObject = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(PrefabPopups.ExternalPrefabs.GameObject.transform, "prefabs path");

            prefabPathGameObject.transform.AsRT().anchoredPosition = config.PrefabExternalPrefabPathPos.Value;
            prefabPathGameObject.transform.AsRT().sizeDelta = new Vector2(config.PrefabExternalPrefabPathLength.Value, 32f);

            TooltipHelper.AssignTooltip(prefabPathGameObject, "Prefab Path", 3f);

            prefabPathField = prefabPathGameObject.GetComponent<InputField>();
            prefabPathField.characterValidation = InputField.CharacterValidation.None;
            prefabPathField.onValueChanged.ClearAll();
            prefabPathField.onEndEdit.ClearAll();
            prefabPathField.textComponent.alignment = TextAnchor.MiddleLeft;
            prefabPathField.textComponent.fontSize = 16;
            prefabPathField.text = PrefabPath;
            prefabPathField.onValueChanged.AddListener(_val => PrefabPath = _val);
            prefabPathField.onEndEdit.AddListener(_val => UpdatePrefabPath(false));

            EditorThemeManager.AddInputField(prefabPathField);

            var prefabPathClickable = prefabPathGameObject.AddComponent<Clickable>();
            prefabPathClickable.onDown = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Set Prefab folder", () =>
                    {
                        BrowserPopup.Open();
                        RTFileBrowser.inst.UpdateBrowserFolder(_val =>
                        {
                            if (!_val.Replace("\\", "/").Contains(RTFile.ApplicationDirectory + "beatmaps/"))
                            {
                                EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                                return;
                            }

                            prefabPathField.text = _val.Replace("\\", "/").Replace(RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/", "");
                            EditorManager.inst.DisplayNotification($"Set Prefab path to {PrefabPath}!", 2f, EditorManager.NotificationType.Success);
                            BrowserPopup.Close();
                            UpdatePrefabPath(false);
                        });
                    }),
                    new ButtonFunction("Open List in File Explorer", OpenPrefabListFolder),
                    new ButtonFunction("Set as Default for Level", () =>
                    {
                        editorInfo.prefabPath = prefabPath;
                        EditorManager.inst.DisplayNotification($"Set current prefab folder [ {prefabPath} ] as the default for the level!", 5f, EditorManager.NotificationType.Success);
                    }, "Prefab Default Path"),
                    new ButtonFunction("Remove Default", () =>
                    {
                        editorInfo.prefabPath = null;
                        EditorManager.inst.DisplayNotification($"Removed default prefab folder.", 5f, EditorManager.NotificationType.Success);
                    }, "Prefab Default Path"));
            };

            EditorHelper.SetComplexity(prefabPathGameObject, Complexity.Advanced);

            var prefabListReload = EditorPrefabHolder.Instance.SpriteButton.Duplicate(PrefabPopups.ExternalPrefabs.GameObject.transform, "reload prefabs");
            prefabListReload.transform.AsRT().anchoredPosition = config.PrefabExternalPrefabRefreshPos.Value;
            prefabListReload.transform.AsRT().sizeDelta = new Vector2(32f, 32f);

            (prefabListReload.GetComponent<HoverTooltip>() ?? prefabListReload.AddComponent<HoverTooltip>()).tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                desc = "Refresh prefab list",
                hint = "Clicking this will reload the prefab list."
            });

            var prefabListReloadButton = prefabListReload.GetComponent<Button>();
            prefabListReloadButton.onClick.ClearAll();
            prefabListReloadButton.onClick.AddListener(() => UpdatePrefabPath(true));

            EditorThemeManager.AddSelectable(prefabListReloadButton, ThemeGroup.Function_2, false);

            prefabListReloadButton.image.sprite = EditorSprites.ReloadSprite;

            #endregion
        }

        void SetupFileBrowser()
        {
            var fileBrowser = NewLevelPopup.GameObject.transform.Find("Browser Popup").gameObject.Duplicate(EditorManager.inst.GetDialog("New File Popup").Dialog.parent, "Browser Popup");
            fileBrowser.gameObject.SetActive(false);
            UIManager.SetRectTransform(fileBrowser.transform.AsRT(), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(600f, 364f));
            var close = fileBrowser.transform.Find("Panel/x").GetComponent<Button>();
            close.onClick.ClearAll();
            close.onClick.AddListener(() => BrowserPopup.Close());
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
            folderPrefabStorage.label = rtfb.folderPrefab.transform.GetChild(0).GetComponent<Text>();

            rtfb.folderBar = fileBrowserBase.folderBar;
            rtfb.oggFileInput = fileBrowserBase.oggFileInput;
            rtfb.filePrefab = fileBrowserBase.filePrefab.Duplicate(EditorPrefabHolder.Instance.PrefabParent, fileBrowserBase.filePrefab.name);
            var filePrefabStorage = rtfb.filePrefab.AddComponent<FunctionButtonStorage>();
            filePrefabStorage.button = rtfb.filePrefab.GetComponent<Button>();
            filePrefabStorage.label = rtfb.filePrefab.transform.GetChild(0).GetComponent<Text>();

            Destroy(fileBrowserBase);

            EditorHelper.AddEditorPopup(EditorPopup.BROWSER_POPUP, fileBrowser);

            EditorThemeManager.AddGraphic(fileBrowser.GetComponent<Image>(), ThemeGroup.Background_1, true);

            var panel = fileBrowser.transform.Find("Panel").gameObject;
            EditorThemeManager.AddGraphic(panel.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);
            EditorThemeManager.AddSelectable(close, ThemeGroup.Close);

            EditorThemeManager.AddGraphic(close.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

            EditorThemeManager.AddLightText(panel.transform.Find("Text").GetComponent<TextMeshProUGUI>());

            EditorThemeManager.AddInputField(fileBrowser.transform.Find("folder-bar").GetComponent<InputField>());

            try
            {
                BrowserPopup = new EditorPopup(EditorPopup.BROWSER_POPUP);
                BrowserPopup.Assign(BrowserPopup.GetLegacyDialog().Dialog.gameObject);
                BrowserPopup.size = BrowserPopup.GameObject.transform.AsRT().sizeDelta;
                editorPopups.Add(BrowserPopup);

                if (BrowserPopup.Title)
                    BrowserPopup.title = BrowserPopup.Title.text;
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        void SetupTimelinePreview()
        {
            if (GameManager.inst.playerGUI.transform.TryFind("Interface", out Transform ic))
                Destroy(ic.gameObject); // Destroys the Interface so the duplicate doesn't get in the way of the editor.

            var gui = GameManager.inst.playerGUI.Duplicate(EditorManager.inst.dialogs.parent);
            gui.transform.SetSiblingIndex(0);

            Destroy(gui.transform.Find("Health").gameObject);
            if (gui.transform.TryFind("Interface", out Transform dupIC))
                Destroy(dupIC.gameObject);

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

            try
            {
                var top = Creator.NewUIObject("Top", EditorTimeline.inst.wholeTimeline, 3); // creates a cover so the bin scrolling doesn't overlay outside the regular slider range.
                RectValues.Default.AnchorMax(0f, 1f).AnchorMin(0f, 1f).Pivot(0f, 1f).SizeDelta(1920f, 25f).AssignToRectTransform(top.transform.AsRT());
                EditorThemeManager.AddGraphic(top.AddComponent<Image>(), ThemeGroup.Background_3);

                EditorTimeline.inst.timelineSliderHandle = EditorTimeline.inst.wholeTimeline.Find("Slider_Parent/Slider/Handle Slide Area/Image/Handle").GetComponent<Image>();
                EditorTimeline.inst.timelineSliderRuler = EditorTimeline.inst.wholeTimeline.Find("Slider_Parent/Slider/Handle Slide Area/Image").GetComponent<Image>();
                var keyframeTimelineHandle = EditorManager.inst.GetDialog("Object Editor").Dialog.Find("timeline/Scroll View/Viewport/Content/time_slider/Handle Slide Area/Handle");
                EditorTimeline.inst.keyframeTimelineSliderHandle = keyframeTimelineHandle.Find("Image").GetComponent<Image>();
                EditorTimeline.inst.keyframeTimelineSliderRuler = keyframeTimelineHandle.GetComponent<Image>();

                EditorTimeline.inst.UpdateTimelineColors();
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

            UIManager.SetRectTransform(grid.transform.AsRT(), Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero, Vector2.zero);

            EditorTimeline.inst.timelineGridRenderer = grid.AddComponent<GridRenderer>();

            var config = EditorConfig.Instance;

            EditorTimeline.inst.timelineGridRenderer.color = config.TimelineGridColor.Value;
            EditorTimeline.inst.timelineGridRenderer.thickness = config.TimelineGridThickness.Value;

            EditorTimeline.inst.timelineGridRenderer.enabled = config.TimelineGridEnabled.Value;

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

        void SetupNewFilePopup()
        {
            var newFilePopupBase = NewLevelPopup.GameObject.transform;

            var newFilePopup = newFilePopupBase.Find("New File Popup");

            var newFilePopupDetection = newFilePopup.gameObject.AddComponent<Clickable>();
            newFilePopupDetection.onEnable = _val =>
            {
                if (_val)
                    return;

                if (LevelTemplateEditor.inst.choosingLevelTemplate)
                    OpenLevelPopup.Close();

                LevelTemplateEditor.inst.choosingLevelTemplate = false;
                LevelTemplateEditor.inst.Dialog.Close();
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
            browseLocalText.text = SYSTEM_BROWSER;
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
            browseInternalText.text = EDITOR_BROWSER;
            var browseInternalButton = browseInternal.GetComponent<Button>();
            browseInternalButton.onClick.ClearAll();
            browseInternalButton.onClick.AddListener(() =>
            {
                BrowserPopup.Open();
                RTFileBrowser.inst.UpdateBrowserFile(RTFile.AudioDotFormats, onSelectFile: _val =>
                {
                    if (!string.IsNullOrEmpty(_val))
                    {
                        path.text = _val;
                        BrowserPopup.Close();
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
                LevelTemplateEditor.inst.RefreshNewLevelTemplates();
                LevelTemplateEditor.inst.Dialog.Open();
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
            songTitleInputField.onValueChanged.AddListener(x => newLevelSongTitle = x);

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

            LevelTemplateEditor.Init();
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
            ObjectSearchPopup = GeneratePopup(EditorPopup.OBJECT_SEARCH_POPUP, "Object Search", Vector2.zero, new Vector2(600f, 450f), placeholderText: "Search for object...");

            var dropdown = EditorHelper.AddEditorDropdown("Search Objects", "", "Edit", EditorSprites.SearchSprite, () =>
            {
                ObjectSearchPopup.Open();
                ObjectEditor.inst.RefreshObjectSearch(x => EditorTimeline.inst.SetCurrentObject(EditorTimeline.inst.GetTimelineObject(x), Input.GetKey(KeyCode.LeftControl)));
            });

            EditorHelper.SetComplexity(dropdown, Complexity.Normal);
        }

        void CreateWarningPopup()
        {
            var warningPopup = SaveAsPopup.GameObject.Duplicate(popups, "Warning Popup");
            warningPopup.transform.AsRT().anchoredPosition = Vector2.zero;

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
            close.onClick.AddListener(HideWarningPopup);

            var title = panel.Find("Text").GetComponent<Text>();
            title.text = "Warning!";

            EditorHelper.AddEditorPopup(EditorPopup.WARNING_POPUP, warningPopup);

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

            try
            {
                WarningPopup = new EditorPopup(EditorPopup.WARNING_POPUP);
                WarningPopup.Assign(warningPopup);
                editorPopups.Add(WarningPopup);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        GameObject GenerateDebugButton(string name, string hint, Action action)
        {
            var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(DebuggerPopup.Content, "Function");
            debugs.Add(name);

            gameObject.AddComponent<HoverTooltip>().tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                desc = name,
                hint = hint
            });

            var button = gameObject.GetComponent<Button>();
            button.onClick.ClearAll();
            button.onClick.AddListener(() => action?.Invoke());

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

            DebuggerPopup = GeneratePopup(EditorPopup.DEBUGGER_POPUP, "Debugger (Only use this if you know what you're doing)", Vector2.zero, new Vector2(600f, 450f), _val =>
            {
                debugSearch = _val;
                RefreshDebugger();
            }, placeholderText: "Search for function...");

            var reload = EditorPrefabHolder.Instance.SpriteButton.Duplicate(DebuggerPopup.TopPanel, "reload");
            UIManager.SetRectTransform(reload.transform.AsRT(), new Vector2(-42f, 0f), Vector2.one, Vector2.one, Vector2.one, new Vector2(32f, 32f));

            reload.GetOrAddComponent<HoverTooltip>().tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                desc = "Refresh the function list",
                hint = "Clicking this will reload the function list."
            });

            var reloadButton = reload.GetComponent<Button>();
            reloadButton.onClick.ClearAll();
            reloadButton.onClick.AddListener(ReloadFunctions);

            EditorThemeManager.AddSelectable(reloadButton, ThemeGroup.Function_2, false);

            reloadButton.image.sprite = EditorSprites.ReloadSprite;

            EditorHelper.AddEditorDropdown("Debugger", "", "View", SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}debugger{FileFormat.PNG.Dot()}"), () =>
            {
                DebuggerPopup.Open();
                RefreshDebugger();
            });

            EditorHelper.AddEditorDropdown("Show Explorer", "", "View", EditorSprites.SearchSprite, ModCompatibility.ShowExplorer);

            GenerateDebugButton(
                "Inspect DataManager",
                "DataManager is a pretty important storage component of Project Arrhythmia. It contains the GameData, all the external Beatmap Themes, etc.",
                () => ModCompatibility.Inspect(DataManager.inst));

            GenerateDebugButton(
                "Inspect EditorManager",
                "EditorManager handles the main unmodded editor related things.",
                () => ModCompatibility.Inspect(EditorManager.inst));

            GenerateDebugButton(
                "Inspect RTEditor",
                "EditorManager handles the main modded editor related things.",
                () => ModCompatibility.Inspect(inst));

            GenerateDebugButton(
                "Inspect ObjEditor",
                "ObjEditor is the component that handles regular object editor stuff.",
                () => ModCompatibility.Inspect(ObjEditor.inst));

            GenerateDebugButton(
                "Inspect ObjectEditor",
                "ObjectEditor is the component that handles modded object editor stuff.",
                () => ModCompatibility.Inspect(ObjectEditor.inst));

            GenerateDebugButton(
                "Inspect ObjectManager",
                "ObjectManager is the component that handles regular object stuff.",
                () => ModCompatibility.Inspect(ObjectManager.inst));

            GenerateDebugButton(
                "Inspect GameManager",
                "GameManager normally handles all the level loading, however now it's handled by LevelManager.",
                () => ModCompatibility.Inspect(GameManager.inst));

            GenerateDebugButton(
                "Inspect CompanionManager",
                "CompanionManager handles everything to do with Example, your little companion.",
                () => ModCompatibility.Inspect(CompanionManager.inst));
            
            GenerateDebugButton(
                "Inspect Example",
                "Example...",
                () => ModCompatibility.Inspect(Example.Current));

            GenerateDebugButton(
                "Inspect Object Editor UI",
                "Take a closer look at the Object Editor UI since the parent tree for it is pretty deep.",
                () => ModCompatibility.Inspect(ObjEditor.inst.ObjectView));

            GenerateDebugButton(
                "Inspect LevelProcessor",
                "LevelProcessor is the main handler for updating object animation and spawning / despawning objects.",
                () => ModCompatibility.Inspect(Updater.levelProcessor));

            GenerateDebugButton(
                "Inspect Current GameData",
                "GameData stores all the main level data.",
                () => ModCompatibility.Inspect(GameData.Current));

            GenerateDebugButton(
                "Inspect Current MetaData",
                "MetaData stores all the extra level info.",
                () => ModCompatibility.Inspect(MetaData.Current));

            GenerateDebugButton(
                "Current Event Keyframe",
                "The current selected Event Keyframe. Based on the type and index number.",
                () => ModCompatibility.Inspect(RTEventEditor.inst.CurrentSelectedKeyframe));

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

            var files = Directory.GetFiles(functions, FileFormat.CS.ToPattern());
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

        void CreateAutosavePopup() => AutosavePopup = GeneratePopup(EditorPopup.AUTOSAVE_POPUP, "Open / Backup an Autosave", new Vector2(572f, 0f), new Vector2(460f, 350f), placeholderText: "Search autosaves...");

        void SetupMiscEditorThemes()
        {
            var checkpointEditor = CheckpointEditorDialog.GameObject.transform;
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
                var options = ObjectOptionsPopup.GameObject.transform;

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
                var options = BGObjectOptionsPopup.GameObject.transform;

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
            var editorDialogObject = EditorPrefabHolder.Instance.Dialog.Duplicate(EditorManager.inst.dialogs, "ScreenshotDialog");
            editorDialogObject.transform.AsRT().anchoredPosition = new Vector2(0f, 16f);
            editorDialogObject.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            var dialogStorage = editorDialogObject.GetComponent<EditorDialogStorage>();

            dialogStorage.topPanel.color = LSColors.HexToColor("00FF8C");
            dialogStorage.title.text = "- Screenshots -";

            var editorDialogSpacer = editorDialogObject.transform.GetChild(1);
            editorDialogSpacer.AsRT().sizeDelta = new Vector2(765f, 54f);

            Destroy(editorDialogObject.transform.GetChild(2).gameObject);

            EditorHelper.AddEditorDialog(EditorDialog.SCREENSHOTS, editorDialogObject);

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

            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(editorDialogObject.transform, "Scroll View");
            screenshotContent = scrollView.transform.Find("Viewport/Content");
            scrollView.transform.localScale = Vector3.one;

            LSHelpers.DeleteChildren(screenshotContent);

            var scrollViewLE = scrollView.AddComponent<LayoutElement>();
            scrollViewLE.ignoreLayout = true;

            scrollView.transform.AsRT().anchoredPosition = new Vector2(392.5f, 320f);
            scrollView.transform.AsRT().sizeDelta = new Vector2(735f, 638f);

            EditorThemeManager.AddGraphic(editorDialogObject.GetComponent<Image>(), ThemeGroup.Background_1);

            EditorHelper.AddEditorDropdown("View Screenshots", "", "View", EditorSprites.SearchSprite, () =>
            {
                ScreenshotsDialog.Open();
                RefreshScreenshots();
            });

            try
            {
                ScreenshotsDialog = new EditorDialog(EditorDialog.SCREENSHOTS);
                ScreenshotsDialog.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog
        }

        void CreateFolderCreator()
        {
            try
            {
                var folderCreator = SaveAsPopup.GameObject.Duplicate(popups, "Folder Creator Popup");
                folderCreator.transform.localPosition = Vector3.zero;

                var folderCreatorPopup = folderCreator.transform.GetChild(0);

                var folderCreatorPopupPanel = folderCreatorPopup.Find("Panel");
                folderCreatorTitle = folderCreatorPopupPanel.Find("Text").GetComponent<Text>();
                folderCreatorTitle.text = "Folder Creator";

                var close = folderCreatorPopupPanel.Find("x").GetComponent<Button>();
                close.onClick.ClearAll();
                close.onClick.AddListener(HideNameEditor);

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

                EditorHelper.AddEditorPopup(EditorPopup.FOLDER_CREATOR_POPUP, folderCreator);

                NamePopup = new EditorPopup(EditorPopup.FOLDER_CREATOR_POPUP);
                NamePopup.Assign(NamePopup.GetLegacyDialog().Dialog.gameObject);
                NamePopup.size = NamePopup.GameObject.transform.AsRT().sizeDelta;
                editorPopups.Add(NamePopup);

                if (NamePopup.Title)
                    NamePopup.title = NamePopup.Title.text;
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        void CreateFontSelector()
        {
            FontSelectorPopup = GeneratePopup(EditorPopup.FONT_SELECTOR_POPUP, "Select a Font", Vector2.zero, new Vector2(600f, 400f), placeholderText: "Search fonts...");

            fontSelectionPrefab = Creator.NewUIObject("element", transform);
            RectValues.Default.SizeDelta(0f, 32f).AssignToRectTransform(fontSelectionPrefab.transform.AsRT());

            var horizontalLayoutGroup = fontSelectionPrefab.AddComponent<HorizontalLayoutGroup>();

            horizontalLayoutGroup.childControlHeight = false;
            horizontalLayoutGroup.childControlWidth = false;
            horizontalLayoutGroup.childForceExpandWidth = false;
            horizontalLayoutGroup.spacing = 8f;
            fontSelectionPrefab.AddComponent<Image>();
            fontSelectionPrefab.AddComponent<Button>();

            var labels = Creator.NewUIObject("label", fontSelectionPrefab.transform);
            RectValues.FullAnchored.Pivot(0f, 1f).SizeDelta(722f, 22f).AssignToRectTransform(labels.transform.AsRT());

            var labelsHLG = labels.AddComponent<HorizontalLayoutGroup>();

            labelsHLG.childControlHeight = false;
            labelsHLG.childControlWidth = false;
            labelsHLG.spacing = 8f;

            var label = Creator.NewUIObject("text", labels.transform);
            RectValues.FullAnchored.Pivot(0f, 1f).SizeDelta(722f, 22f).AssignToRectTransform(label.transform.AsRT());

            var text = label.AddComponent<TextMeshProUGUI>();
            text.font = FontManager.inst.allFontAssets["Inconsolata Variable"];
            text.fontSize = 20;
            text.enableWordWrapping = true;
            text.text = "font";
        }

        #endregion

        #endregion

        #region Saving / Loading

        /// <summary>
        /// Pastes the copied / cut level.
        /// </summary>
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

        /// <summary>
        /// Loads editor levels from the current editor folder.
        /// </summary>
        public IEnumerator LoadLevels()
        {
            LevelPanels.Clear();

            OpenLevelPopup.ClearContent();

            var list = new List<Coroutine>();
            var files = Directory.GetDirectories(RTFile.ApplicationDirectory + editorListPath);
            var currentPath = editorPath;

            // Back
            if (EditorConfig.Instance.ShowFoldersInLevelList.Value && RTFile.GetDirectory(RTFile.ApplicationDirectory + editorListPath) != RTFile.ApplicationDirectory + "beatmaps")
            {
                var gameObjectFolder = EditorManager.inst.folderButtonPrefab.Duplicate(OpenLevelPopup.Content, "back");
                var folderButtonStorageFolder = gameObjectFolder.GetComponent<FunctionButtonStorage>();
                var folderButtonFunctionFolder = gameObjectFolder.AddComponent<FolderButtonFunction>();

                var hoverUIFolder = gameObjectFolder.AddComponent<HoverUI>();
                hoverUIFolder.size = EditorConfig.Instance.OpenLevelButtonHoverSize.Value;
                hoverUIFolder.animatePos = false;
                hoverUIFolder.animateSca = true;

                folderButtonStorageFolder.label.text = "< Up a folder";

                folderButtonStorageFolder.label.horizontalOverflow = EditorConfig.Instance.OpenLevelTextHorizontalWrap.Value;
                folderButtonStorageFolder.label.verticalOverflow = EditorConfig.Instance.OpenLevelTextVerticalWrap.Value;
                folderButtonStorageFolder.label.fontSize = EditorConfig.Instance.OpenLevelTextFontSize.Value;

                folderButtonStorageFolder.button.onClick.ClearAll();
                folderButtonFunctionFolder.onClick = eventData =>
                {
                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonFunction("Create folder", () => ShowFolderCreator($"{RTFile.ApplicationDirectory}{editorListPath}", () => { UpdateEditorPath(true); HideNameEditor(); })),
                            new ButtonFunction("Create level", EditorManager.inst.OpenNewLevelPopup),
                            new ButtonFunction("Paste", PasteLevel),
                            new ButtonFunction("Open List in File Explorer", OpenLevelListFolder));

                        return;
                    }

                    if (editorPathField.text == currentPath)
                    {
                        editorPathField.text = RTFile.GetDirectory(RTFile.ApplicationDirectory + editorListPath).Replace(RTFile.ApplicationDirectory + "beatmaps/", "");
                        UpdateEditorPath(false);
                    }
                };

                EditorThemeManager.ApplySelectable(folderButtonStorageFolder.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(folderButtonStorageFolder.label);
            }

            foreach (var file in files)
            {
                var path = RTFile.ReplaceSlash(file);

                var levelPanel = new LevelPanel();

                if (!Level.TryVerify(path, false, out Level level))
                {
                    if (!EditorConfig.Instance.ShowFoldersInLevelList.Value)
                        continue;

                    levelPanel.Init(path);
                    LevelPanels.Add(levelPanel);

                    list.Add(levelPanel.LoadImageCoroutine($"folder_icon{FileFormat.PNG.Dot()}"));

                    continue;
                }

                levelPanel.Init(level);

                if (RTFile.FileExists(RTFile.CombinePaths(path, Level.LEVEL_JPG)))
                    list.Add(levelPanel.LoadImageCoroutine(Level.LEVEL_JPG, LevelPanels.Add));
                else if (RTFile.FileExists(RTFile.CombinePaths(path, Level.COVER_JPG)))
                    list.Add(levelPanel.LoadImageCoroutine(Level.COVER_JPG, LevelPanels.Add));
                else
                {
                    levelPanel.SetDefaultIcon();
                    LevelPanels.Add(levelPanel);
                }
            }

            if (list.Count >= 1)
                yield return StartCoroutine(LSHelpers.WaitForMultipleCoroutines(list, OpenLevelPopupOnFinish));
            else
                OpenLevelPopupOnFinish();

            yield break;
        }

        /// <summary>
        /// Loads a level in the editor.
        /// </summary>
        /// <param name="level">Level to load and edit.</param>
        public IEnumerator LoadLevel(Level level)
        {
            CurrentLevel = level;
            var fullPath = RTFile.RemoveEndSlash(level.path);
            var currentFile = level.CurrentFile;
            level.currentFile = null; // reset since autosave loading should be temporary.

            EditorManager.inst.loading = true;
            var sw = CoreHelper.StartNewStopwatch();

            RTPlayer.GameMode = GameMode.Regular;

            string code = RTFile.CombinePaths(fullPath, $"EditorLoad{FileFormat.CS.Dot()}");
            if (RTFile.FileExists(code))
            {
                var str = RTFile.ReadFromFile(code);
                if (RTCode.Validate(str))
                    yield return StartCoroutine(RTCode.IEvaluate(str));
            }

            EditorTimeline.inst.SetLayer(0, EditorTimeline.LayerType.Objects);

            WindowController.ResetTitle();

            RandomHelper.UpdateSeed();

            CoreHelper.Log("Clearing data...");

            for (int i = 0; i < EditorTimeline.inst.timelineObjects.Count; i++)
            {
                var timelineObject = EditorTimeline.inst.timelineObjects[i];
                if (timelineObject.GameObject)
                    Destroy(timelineObject.GameObject);

                if (timelineObject.InternalTimelineObjects != null)
                    for (int j = 0; j < timelineObject.InternalTimelineObjects.Count; j++)
                    {
                        var kf = timelineObject.InternalTimelineObjects[j];
                        if (kf.GameObject)
                            Destroy(kf.GameObject);
                    }
                EditorTimeline.inst.timelineObjects[i] = null;
            }
            EditorTimeline.inst.timelineObjects.Clear();

            for (int i = 0; i < EditorTimeline.inst.timelineKeyframes.Count; i++)
            {
                var timelineObject = EditorTimeline.inst.timelineKeyframes[i];
                if (timelineObject.GameObject)
                    Destroy(timelineObject.GameObject);
                EditorTimeline.inst.timelineKeyframes[i] = null;
            }
            EditorTimeline.inst.timelineKeyframes.Clear();

            for (int i = 0; i < ObjEditor.inst.TimelineParents.Count; i++)
                LSHelpers.DeleteChildren(ObjEditor.inst.TimelineParents[i]);

            if (GameData.Current)
            {
                var bgs = GameData.Current.backgroundObjects;
                for (int i = 0; i < bgs.Count; i++)
                {
                    var bg = bgs[i];
                    for (int j = 0; j < bg.gameObjects.Count; j++)
                    {
                        if (bg.gameObjects[j])
                            Destroy(bg.gameObjects[j]);
                        bg.gameObjects[j] = null;
                    }
                    for (int j = 0; j < bg.renderers.Count; j++)
                        bg.renderers[j] = null;
                    for (int j = 0; j < bg.transforms.Count; j++)
                        bg.transforms[j] = null;

                    bg.gameObjects.Clear();
                    bg.renderers.Clear();
                    bg.transforms.Clear();
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

            EditorManager.inst.ClearPopups();
            EditorDialog.CurrentDialog?.Close();
            InfoPopup.Open();

            if (EditorManager.inst.hasLoadedLevel && EditorConfig.Instance.BackupPreviousLoadedLevel.Value && RTFile.FileExists(GameManager.inst.path))
            {
                CoreHelper.Log("Backing up previous level...");

                InfoPopup.SetInfo($"Backing up previous level [ {Path.GetFileName(RTFile.RemoveEndSlash(GameManager.inst.path))} ]");

                GameData.Current.SaveData(RTFile.CombinePaths(RTFile.BasePath, $"level-open-backup{FileFormat.LSB.Dot()}"));

                CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");
            }

            CoreHelper.Log("Loading data...");

            InfoPopup.SetInfo($"Loading Level Data for [ {name} ]");

            CoreHelper.Log($"Loading {currentFile}...");

            GameManager.inst.path = RTFile.CombinePaths(fullPath, Level.LEVEL_LSB);
            RTFile.BasePath = RTFile.AppendEndSlash(fullPath);
            GameManager.inst.levelName = name;
            InfoPopup.SetInfo($"Loading Level Music for [ {name} ]\n\nIf this is taking more than a minute or two check if the song file (.ogg / .wav / .mp3) is corrupt. If not, then something went really wrong.");

            yield return CoreHelper.StartCoroutine(level.LoadAudioClipRoutine());

            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            CoreHelper.Log("Setting up Video...");
            yield return StartCoroutine(RTVideoManager.inst.Setup(fullPath));
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            GameManager.inst.gameState = GameManager.State.Parsing;
            CoreHelper.Log("Parsing data...");
            InfoPopup.SetInfo($"Parsing Level Data for [ {name} ]");
            string rawJSON = RTFile.ReadFromFile(RTFile.CombinePaths(fullPath, currentFile));
            if (string.IsNullOrEmpty(rawJSON))
            {
                InfoPopup.SetInfo($"{currentFile} is empty or corrupt.");
                EditorManager.inst.DisplayNotification("Level could not load.", 3f, EditorManager.NotificationType.Error);

                yield break;
            }

            try
            {
                MetaData.Current = null;
                MetaData.Current = level.metadata;

                if (MetaData.Current.arcadeID == null || MetaData.Current.arcadeID == "0" || MetaData.Current.arcadeID == "-1")
                    MetaData.Current.arcadeID = LSText.randomNumString(16);

                if (ProjectArrhythmia.RequireUpdate(MetaData.Current.beatmap.game_version))
                    rawJSON = LevelManager.UpdateBeatmap(rawJSON, MetaData.Current.beatmap.game_version);

                GameData.Current = null;
                GameData.Current = level.IsVG ?
                    GameData.ParseVG(JSON.Parse(rawJSON), MetaData.Current.Version) :
                    GameData.Parse(JSON.Parse(rawJSON));
            }
            catch (Exception ex)
            {
                InfoPopup.SetInfo($"Something went wrong when parsing the level data. Press the open log folder key ({CoreConfig.Instance.OpenPAPersistentFolder.Value}) and send the Player.log file to Mecha.");

                EditorManager.inst.DisplayNotification("Level could not load.", 3f, EditorManager.NotificationType.Error);

                CoreHelper.LogError($"Level loading caught an error: {ex}");

                yield break;
            }

            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            if (PreviewCover != null && PreviewCover.gameObject)
                PreviewCover.gameObject.SetActive(false);

            CoreHelper.Log("Playing level music...");
            InfoPopup.SetInfo($"Playing Music for [ {name} ]\n\nIf it doesn't, then something went wrong!");
            SetCurrentAudio(level.music);
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            if (EditorConfig.Instance.WaveformGenerate.Value)
            {
                CoreHelper.Log("Assigning waveform textures...");
                InfoPopup.SetInfo($"Assigning Waveform Textures for [ {name} ]");
                EditorTimeline.inst.SetTimelineSprite(null);
                StartCoroutine(EditorTimeline.inst.AssignTimelineTexture());
                CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");
            }
            else
            {
                CoreHelper.Log("Skipping waveform textures...");
                InfoPopup.SetInfo($"Skipping Waveform Textures for [ {name} ]");
                EditorTimeline.inst.SetTimelineSprite(null);
                CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");
            }

            CoreHelper.Log("Updating timeline...");
            InfoPopup.SetInfo($"Updating Timeline for [ {name} ]");
            EditorManager.inst.UpdateTimelineSizes();
            GameManager.inst.UpdateTimeline();
            RTMetaDataEditor.inst.RenderEditor();
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            CheckpointEditor.inst.CreateGhostCheckpoints();

            InfoPopup.SetInfo($"Updating states for [ {name} ]");
            if (CoreConfig.Instance.DiscordTimestampUpdatesPerLevel.Value)
                DiscordController.inst.presence.startTimestamp = SteamworksFacepunch.Epoch.Current;
            CoreHelper.UpdateDiscordStatus($"Editing: {MetaData.Current.song.title}", "In Editor", "editor");

            CoreHelper.Log("Spawning players...");
            try
            {
                PlayersData.Load(level.GetFile(Level.PLAYERS_LSB));
                PlayerManager.SpawnPlayersOnStart();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            RTPlayer.SetGameDataProperties();
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            CoreHelper.Log("Updating objects...");
            StartCoroutine(Updater.IUpdateObjects(true));
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            CoreHelper.Log("Updating timeline objects...");
            EventEditor.inst.CreateEventObjects();
            BackgroundManager.inst.UpdateBackgrounds();
            //GameManager.inst.UpdateTheme();

            RTMarkerEditor.inst.CreateMarkers();
            RTMarkerEditor.inst.markerLooping = false;
            RTMarkerEditor.inst.markerLoopBegin = null;
            RTMarkerEditor.inst.markerLoopEnd = null;

            EventManager.inst.updateEvents();
            GameData.Current.UpdateUsedThemes();

            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            RTEventManager.inst.SetResetOffsets();

            CoreHelper.Log("Creating timeline objects...");
            InfoPopup.SetInfo($"Setting first object of [ {name} ]");
            StartCoroutine(EditorTimeline.inst.ICreateTimelineObjects());
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            CheckpointEditor.inst.SetCurrentCheckpoint(0);

            InfoPopup.SetInfo("Done!");
            InfoPopup.Close();
            EditorManager.inst.CancelInvoke("LoadingIconUpdate");

            RTGameManager.inst.ResetCheckpoint();
            GameManager.inst.gameState = GameManager.State.Playing;

            EditorManager.inst.DisplayNotification($"{name} Level loaded", 2f, EditorManager.NotificationType.Success);
            EditorManager.inst.UpdatePlayButton();
            EditorManager.inst.hasLoadedLevel = true;

            SetAutosave();

            TriggerHelper.AddEventTriggers(timeField.gameObject, TriggerHelper.ScrollDelta(timeField, max: AudioManager.inst.CurrentAudioSource.clip.length));

            // Load Settings like timeline position, editor layer, bpm active, etc
            LoadSettings();
            EditorManager.inst.RenderTimeline();
            EditorTimeline.inst.RenderBins();

            if (EditorConfig.Instance.LevelPausesOnStart.Value)
            {
                AudioManager.inst.CurrentAudioSource.Pause();
                EditorManager.inst.UpdatePlayButton();
            }

            Example.Current?.brain?.Notice(ExampleBrain.Notices.LOADED_LEVEL);

            EditorManager.inst.loading = false;
            fromNewLevel = false;

            rawJSON = null;

            CoreHelper.StopAndLogStopwatch(sw, $"Finished loading {name}");
            sw = null;

            yield break;
        }

        /// <summary>
        /// Sets the current audio the editor should use.
        /// </summary>
        /// <param name="audioClip">Audio to set.</param>
        public void SetCurrentAudio(AudioClip audioClip) => AudioManager.inst.PlayMusic(null, audioClip, true, 0f, true);

        /// <summary>
        /// Restarts the autosave loop.
        /// </summary>
        public void SetAutosave()
        {
            var autosavesDirectory = RTFile.CombinePaths(RTFile.BasePath, "autosaves");
            RTFile.CreateDirectory(autosavesDirectory);
            var files = Directory.GetFiles(autosavesDirectory, $"autosave_{FileFormat.LSB.ToPattern()}", SearchOption.TopDirectoryOnly);

            EditorManager.inst.autosaves.Clear();
            EditorManager.inst.autosaves.AddRange(files);

            EditorManager.inst.CancelInvoke(nameof(AutosaveLevel));
            CancelInvoke(nameof(AutosaveLevel));
            InvokeRepeating(nameof(AutosaveLevel), EditorConfig.Instance.AutosaveLoopTime.Value, EditorConfig.Instance.AutosaveLoopTime.Value);
        }

        /// <summary>
        /// Autosaves the level.
        /// </summary>
        public void AutosaveLevel()
        {
            if (EditorManager.inst.loading)
                return;

            autosaving = true;

            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Beatmap can't autosave until you load a level.", 3f, EditorManager.NotificationType.Error);
                autosaving = false;
                return;
            }

            if (EditorManager.inst.savingBeatmap)
            {
                EditorManager.inst.DisplayNotification("Already attempting to save the beatmap!", 2f, EditorManager.NotificationType.Error);
                autosaving = false;
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

            Example.Current?.brain?.Notice(ExampleBrain.Notices.EDITOR_AUTOSAVED);

            autosaving = false;
        }

        /// <summary>
        /// Saves a backup of the current level.
        /// </summary>
        public void SaveBackup()
        {
            if (EditorManager.inst.loading)
                return;

            autosaving = true;

            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Beatmap can't autosave until you load a level.", 3f, EditorManager.NotificationType.Error);
                autosaving = false;
                return;
            }

            if (EditorManager.inst.savingBeatmap)
            {
                EditorManager.inst.DisplayNotification("Already attempting to save the beatmap!", 2f, EditorManager.NotificationType.Error);
                autosaving = false;
                return;
            }

            var autosavesDirectory = RTFile.CombinePaths(RTFile.BasePath, "autosaves");
            var autosavePath = RTFile.CombinePaths(autosavesDirectory, $"backup_{DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss")}{FileFormat.LSB.Dot()}");

            RTFile.CreateDirectory(autosavesDirectory);

            EditorManager.inst.DisplayNotification("Saving backup...", 2f, EditorManager.NotificationType.Warning);

            GameData.Current.SaveData(autosavePath);

            EditorManager.inst.DisplayNotification("Autosaved backup!", 2f, EditorManager.NotificationType.Success);

            autosaving = false;
        }

        /// <summary>
        /// Saves a backup of a level.
        /// </summary>
        /// <param name="levelPanel">Level to save the backup of.</param>
        public void SaveBackup(LevelPanel levelPanel)
        {
            if (EditorManager.inst.loading)
                return;

            autosaving = true;

            if (EditorManager.inst.savingBeatmap)
            {
                EditorManager.inst.DisplayNotification("Already attempting to save the beatmap!", 2f, EditorManager.NotificationType.Error);
                autosaving = false;
                return;
            }

            var autosavesDirectory = RTFile.CombinePaths(levelPanel.FolderPath, "autosaves");
            var autosavePath = RTFile.CombinePaths(autosavesDirectory, $"backup_{DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss")}{FileFormat.LSB.Dot()}");

            RTFile.CreateDirectory(autosavesDirectory);

            EditorManager.inst.DisplayNotification("Saving backup...", 2f, EditorManager.NotificationType.Warning);

            RTFile.CopyFile(levelPanel.Level.GetFile(levelPanel.Level.CurrentFile), autosavePath);

            EditorManager.inst.DisplayNotification("Saved backup!", 2f, EditorManager.NotificationType.Success);

            autosaving = false;
        }

        /// <summary>
        /// Creates a new level and loads it.
        /// </summary>
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

            var gameData = LevelTemplateEditor.inst.CurrentTemplate.GetGameData();

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

            var levelPanel = new LevelPanel();
            levelPanel.Init(new Level(path));
            LevelPanels.Add(levelPanel);
            StartCoroutine(LoadLevel(levelPanel.Level));
            NewLevelPopup.Close();
        }

        /// <summary>
        /// Creates the default beatmap.
        /// </summary>
        /// <returns>Returns the base beatmap.</returns>
        public GameData CreateBaseBeatmap()
        {
            var gameData = new GameData();
            gameData.data = new LevelBeatmapData();
            gameData.data.level = new LevelData()
            {
                limitPlayer = false,
            };
            gameData.data.editor = new LevelEditorData();
            gameData.data.checkpoints.Add(new Checkpoint(Checkpoint.BASE_CHECKPOINT_NAME, 0f, Vector2.zero));

            if (gameData.events == null)
                gameData.events = new List<List<EventKeyframe>>();
            gameData.events.Clear();
            GameData.ClampEventListValues(gameData.events);

            for (int i = 0; i < (CoreHelper.AprilFools ? 45 : 25); i++)
            {
                var backgroundObject = new BackgroundObject();
                backgroundObject.name = "bg - " + i;
                if (UnityEngine.Random.value > 0.5f)
                    backgroundObject.scale = new Vector2(UnityEngine.Random.Range(2, 8), UnityEngine.Random.Range(2, 8));
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
                    backgroundObject.reactiveType = (BackgroundObject.ReactiveType)UnityEngine.Random.Range(0, 4);
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

            var beatmapObject = ObjectEditor.inst.CreateNewBeatmapObject(0.5f);
            beatmapObject.events[0].Add(new EventKeyframe(4f, new float[3] { 10f, 0f, 0f }, new float[3]));
            if (CoreHelper.AprilFools)
                beatmapObject.events[2].Add(new EventKeyframe(12f, new float[1] { 360000f }, new float[3]));

            beatmapObject.name = CoreHelper.AprilFools ? "trololololo" : DEFAULT_OBJECT_NAME;
            beatmapObject.autoKillType = BeatmapObject.AutoKillType.LastKeyframeOffset;
            beatmapObject.autoKillOffset = 4f;
            beatmapObject.editorData.Layer = 0;
            gameData.beatmapObjects.Add(beatmapObject);

            return gameData;
        }

        void OpenLevelPopupOnFinish()
        {
            if (!EditorConfig.Instance.OpenNewLevelCreatorIfNoLevels.Value || LevelPanels.Count > 0)
            {
                OpenLevelPopup.Open();
                EditorManager.inst.RenderOpenBeatmapPopup();
            }
            else
                EditorManager.inst.OpenNewLevelPopup();
        }

        #endregion

        #region Refresh Popups / Dialogs

        public List<EditorDialog> editorDialogs = new List<EditorDialog>();

        public EditorDialog CheckpointEditorDialog { get; set; }
        public EditorDialog ScreenshotsDialog { get; set; }

        #region Folder Creator / Name Editor

        /// <summary>
        /// Shows the Folder Creator Popup.
        /// </summary>
        /// <param name="path">Path to create a folder in.</param>
        /// <param name="onSubmit">Function to run when Submit is clicked.</param>
        public void ShowFolderCreator(string path, Action onSubmit)
        {
            NamePopup.Open();
            RefreshFolderCreator(path, onSubmit);
        }

        /// <summary>
        /// Renders the Folder Creator Popup.
        /// </summary>
        /// <param name="path">Path to create a folder in.</param>
        /// <param name="onSubmit">Function to run when Submit is clicked.</param>
        public void RefreshFolderCreator(string path, Action onSubmit) => RefreshNameEditor("Folder Creator", "New folder name", "Create Folder", () =>
        {
            var directory = RTFile.CombinePaths(path, RTFile.ValidateDirectory(folderCreatorName.text));

            if (RTFile.CreateDirectory(directory))
                onSubmit?.Invoke();
        });

        /// <summary>
        /// Hides the name editor.
        /// </summary>
        public void HideNameEditor() => NamePopup.Close();

        /// <summary>
        /// Shows the Name Editor Popup.
        /// </summary>
        /// <param name="title">Name of the popup to render.</param>
        /// <param name="nameLabel">Label of the name field.</param>
        /// <param name="submitText">Submit button text.</param>
        /// <param name="onSubmit">Function to run when submit is clicked.</param>
        public void ShowNameEditor(string title, string nameLabel, string submitText, Action onSubmit)
        {
            NamePopup.Open();
            RefreshNameEditor(title, nameLabel, submitText, onSubmit);
        }

        /// <summary>
        /// Renders the Name Editor Popup.
        /// </summary>
        /// <param name="title">Name of the popup to render.</param>
        /// <param name="nameLabel">Label of the name field.</param>
        /// <param name="submitText">Submit button text.</param>
        /// <param name="onSubmit">Function to run when submit is clicked.</param>
        public void RefreshNameEditor(string title, string nameLabel, string submitText, Action onSubmit)
        {
            folderCreatorTitle.text = title;
            folderCreatorNameLabel.text = nameLabel;
            folderCreatorSubmitText.text = submitText;

            folderCreatorSubmit.onClick.ClearAll();
            folderCreatorSubmit.onClick.AddListener(() => onSubmit?.Invoke());
        }

        #endregion

        #region Warning Popup

        /// <summary>
        /// Hides the warning popup.
        /// </summary>
        public void HideWarningPopup() => WarningPopup.Close();

        /// <summary>
        /// Shows the warning popup.
        /// </summary>
        /// <param name="warning">The warning message.</param>
        /// <param name="onConfirm">Function to run when the user confirms.</param>
        /// <param name="onCancel">Function to run when the user cancels.</param>
        /// <param name="confirm">Confirm button text.</param>
        /// <param name="cancel">Cancel button text.</param>
        /// <param name="onClose">Function to run when the user closes the popup.</param>
        public void ShowWarningPopup(string warning, UnityAction onConfirm, UnityAction onCancel, string confirm = "Yes", string cancel = "No", Action onClose = null)
        {
            WarningPopup.Open();
            RefreshWarningPopup(warning, onConfirm, onCancel, confirm, cancel, onClose);

            Example.Current?.brain?.Notice(ExampleBrain.Notices.WARNING_POPUP);
        }

        /// <summary>
        /// Renders the warning popup.
        /// </summary>
        /// <param name="warning">The warning message.</param>
        /// <param name="onConfirm">Function to run when the user confirms.</param>
        /// <param name="onCancel">Function to run when the user cancels.</param>
        /// <param name="confirm">Confirm button text.</param>
        /// <param name="cancel">Cancel button text.</param>
        /// <param name="onClose">Function to run when the user closes the popup.</param>
        public void RefreshWarningPopup(string warning, UnityAction onConfirm, UnityAction onCancel, string confirm = "Yes", string cancel = "No", Action onClose = null)
        {
            var warningPopup = WarningPopup.GameObject.transform.GetChild(0);

            var close = warningPopup.Find("Panel/x").GetComponent<Button>();
            close.onClick.ClearAll();
            close.onClick.AddListener(() =>
            {
                if (onClose != null)
                {
                    onClose();
                    return;
                }

                onCancel?.Invoke();
            });

            warningPopup.Find("Level Name").GetComponent<Text>().text = warning;

            var submit1 = warningPopup.Find("spacerL/submit1");
            var submit2 = warningPopup.Find("spacerL/submit2");

            var submit1Button = submit1.GetComponent<Button>();
            var submit2Button = submit2.GetComponent<Button>();

            submit1.Find("text").GetComponent<Text>().text = confirm;
            submit2.Find("text").GetComponent<Text>().text = cancel;

            submit1Button.onClick.ClearAll();
            submit2Button.onClick.ClearAll();

            submit1Button.onClick.AddListener(onConfirm);
            submit2Button.onClick.AddListener(onCancel);
        }

        #endregion

        #region Levels

        /// <summary>
        /// Refreshes the search and sort of the editor levels.
        /// </summary>
        public IEnumerator RefreshLevelList()
        {
            CoreHelper.Log($"Level Search: {EditorManager.inst.openFileSearch}\nLevel Sort: {levelAscend} - {levelSort}");

            var levelPanels = levelSort switch
            {
                LevelSort.Cover => LevelPanels.Order(x => x.Level && x.Level.icon != SteamWorkshop.inst.defaultSteamImageSprite, !levelAscend),
                LevelSort.Artist => LevelPanels.Order(x => x.Level?.metadata?.artist?.Name ?? string.Empty, !levelAscend),
                LevelSort.Creator => LevelPanels.Order(x => x.Level?.metadata?.creator?.steam_name ?? string.Empty, !levelAscend),
                LevelSort.File => LevelPanels.Order(x => x.FolderPath, !levelAscend),
                LevelSort.Title => LevelPanels.Order(x => x.Level?.metadata?.song?.title ?? string.Empty, !levelAscend),
                LevelSort.Difficulty => LevelPanels.Order(x => x.Level?.metadata?.song?.difficulty ?? 0, !levelAscend),
                LevelSort.DateEdited => LevelPanels.Order(x => x.Level?.metadata?.beatmap?.date_edited ?? string.Empty, !levelAscend),
                LevelSort.DateCreated => LevelPanels.Order(x => x.Level?.metadata?.beatmap?.date_created ?? string.Empty, !levelAscend),
                LevelSort.DatePublished => LevelPanels.Order(x => x.Level?.metadata?.beatmap?.date_published ?? string.Empty, !levelAscend),
                _ => LevelPanels,
            };

            //levelPanels = levelPanels.Order(x => x.IconImage.sprite != EditorSprites.OpenSprite, true); // sort folders without icons at the bottom
            levelPanels = levelPanels.Order(x => x.isFolder, true); // folders should always be at the top.

            int num = 0;
            foreach (var editorWrapper in levelPanels)
            {
                var folder = editorWrapper.FolderPath;
                var metadata = editorWrapper.Level?.metadata;

                editorWrapper.SetActive(editorWrapper.isFolder && RTString.SearchString(EditorManager.inst.openFileSearch, Path.GetFileName(folder)) ||
                    !editorWrapper.isFolder && (RTString.SearchString(EditorManager.inst.openFileSearch, Path.GetFileName(folder)) ||
                        metadata == null || metadata != null &&
                        (RTString.SearchString(EditorManager.inst.openFileSearch, metadata.song.title) ||
                        RTString.SearchString(EditorManager.inst.openFileSearch, metadata.artist.Name) ||
                        RTString.SearchString(EditorManager.inst.openFileSearch, metadata.creator.steam_name) ||
                        RTString.SearchString(EditorManager.inst.openFileSearch, metadata.song.description) ||
                        RTString.SearchString(EditorManager.inst.openFileSearch, LevelPanel.difficultyNames[Mathf.Clamp(metadata.song.difficulty, 0, LevelPanel.difficultyNames.Length - 1)]))));

                editorWrapper.GameObject.transform.SetSiblingIndex(num);
                num++;
            }

            var transform = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("mask/content");

            if (transform.Find("back"))
            {
                yield return null;
                if (transform.Find("back"))
                    transform.Find("back").SetAsFirstSibling();
            }

            yield break;
        }

        /// <summary>
        /// Refreshes the editor file browsing.
        /// </summary>
        public void RefreshFileBrowserLevels() => RTFileBrowser.inst.UpdateBrowserFile(FileFormat.LSB.Dot(), "level", x => StartCoroutine(LoadLevel(new Level(RTFile.ReplaceSlash(x).Remove("/" + Level.LEVEL_LSB)))));

        /// <summary>
        /// Refreshes the autosaves and backups of a level.
        /// </summary>
        /// <param name="levelPanel">Level to get autosaves from.</param>
        public void RefreshAutosaveList(LevelPanel levelPanel)
        {
            AutosavePopup.SearchField.onValueChanged.ClearAll();
            AutosavePopup.SearchField.onValueChanged.AddListener(_val =>
            {
                autosaveSearch = _val;
                RefreshAutosaveList(levelPanel);
            });

            AutosavePopup.ClearContent();

            if (levelPanel.isFolder)
            {
                EditorManager.inst.DisplayNotification("Folders can't have autosaves / backups.", 1.5f, EditorManager.NotificationType.Warning);
                return;
            }

            var files =
                Directory.GetFiles(levelPanel.FolderPath, $"autosave_{FileFormat.LSB.ToPattern()}", SearchOption.AllDirectories)
                .Union(Directory.GetFiles(levelPanel.FolderPath, $"backup_{FileFormat.LSB.ToPattern()}", SearchOption.AllDirectories));

            foreach (var file in files)
            {
                var path = RTFile.ReplaceSlash(file);
                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(AutosavePopup.Content, $"Folder [{Path.GetFileName(file)}]");
                var folderButtonStorage = gameObject.GetComponent<FunctionButtonStorage>();
                folderButtonStorage.button.onClick.ClearAll();

                string tmpFile = path;

                var contextMenu = gameObject.AddComponent<FolderButtonFunction>();
                contextMenu.onClick = eventData =>
                {
                    switch (eventData.button)
                    {
                        // just realized I could collapse the switch case blocks like this
                        case PointerEventData.InputButton.Left:
                            {
                                levelPanel.Level.currentFile = tmpFile.Remove(RTFile.AppendEndSlash(levelPanel.FolderPath));

                                StartCoroutine(LoadLevel(levelPanel.Level));
                                OpenLevelPopup.Close();
                                break;
                            }
                        case PointerEventData.InputButton.Right:
                            {
                                EditorContextMenu.inst.ShowContextMenu(
                                    new ButtonFunction("Open", () =>
                                    {
                                        levelPanel.Level.currentFile = tmpFile.Remove(RTFile.AppendEndSlash(levelPanel.FolderPath));

                                        StartCoroutine(LoadLevel(levelPanel.Level));
                                        OpenLevelPopup.Close();
                                    }),
                                    new ButtonFunction("Toggle Backup State", () =>
                                    {
                                        var fi = new FileInfo(tmpFile);

                                        tmpFile = tmpFile.Contains("autosave_") ? tmpFile.Replace("autosave_", "backup_") : tmpFile.Replace("backup_", "autosave_");

                                        if (fi.Exists)
                                            fi.MoveTo(tmpFile);

                                        RefreshAutosaveList(levelPanel);
                                    }, "Autosave Toggle Backup State"),
                                    new ButtonFunction("Delete", () =>
                                    {
                                        ShowWarningPopup("Are you sure you want to delete this autosave? This is permanent!", () =>
                                        {
                                            RTFile.DeleteFile(tmpFile);
                                            RefreshAutosaveList(levelPanel);
                                        }, HideWarningPopup);
                                    })
                                    );
                                break;
                            }
                    }
                };

                var hoverUI = gameObject.AddComponent<HoverUI>();
                hoverUI.size = EditorConfig.Instance.OpenLevelButtonHoverSize.Value;
                hoverUI.animatePos = false;
                hoverUI.animateSca = true;

                folderButtonStorage.label.text = Path.GetFileName(file);

                var backup = EditorPrefabHolder.Instance.Function1Button.Duplicate(gameObject.transform, "backup");
                var backupHolder = backup.GetComponent<FunctionButtonStorage>();
                backup.transform.localScale = Vector3.one;
                UIManager.SetRectTransform(backup.transform.AsRT(), new Vector2(450f, 0f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(80f, 28f));
                backupHolder.label.text = "Backup";
                backupHolder.button.onClick.ClearAll();
                backupHolder.button.onClick.AddListener(() =>
                {
                    var fi = new FileInfo(tmpFile);

                    tmpFile = tmpFile.Contains("autosave_") ? tmpFile.Replace("autosave_", "backup_") : tmpFile.Replace("backup_", "autosave_");

                    if (fi.Exists)
                        fi.MoveTo(tmpFile);

                    RefreshAutosaveList(levelPanel);
                });

                TooltipHelper.AssignTooltip(backup, "Autosave Toggle Backup State");

                EditorThemeManager.ApplySelectable(folderButtonStorage.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyGraphic(backupHolder.button.image, ThemeGroup.Function_1, true);
                EditorThemeManager.ApplyGraphic(backupHolder.label, ThemeGroup.Function_1_Text);
            }
        }

        #endregion

        #region Preview

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
            if (!timelinePreview || !AudioManager.inst.CurrentAudioSource.clip || !GameData.Current || GameData.Current.data == null)
                return;

            for (int i = 0; i < checkpointImages.Count; i++)
            {
                if (checkpointImages[i] && checkpointImages[i].gameObject)
                    Destroy(checkpointImages[i].gameObject);
            }

            checkpointImages.Clear();
            LSHelpers.DeleteChildren(timelinePreview.Find("elements"));
            foreach (var checkpoint in GameData.Current.data.checkpoints)
            {
                if (checkpoint.time <= 0.5f)
                    continue;

                var gameObject = GameManager.inst.checkpointPrefab.Duplicate(timelinePreview.Find("elements"), $"Checkpoint [{checkpoint.name}] - [{checkpoint.time}]");
                float num = checkpoint.time * 400f / AudioManager.inst.CurrentAudioSource.clip.length;
                gameObject.transform.AsRT().anchoredPosition = new Vector2(num, 0f);
                checkpointImages.Add(gameObject.GetComponent<Image>());
            }
        }

        #endregion

        public void ShowFontSelector(Action<string> onFontSelected)
        {
            FontSelectorPopup.Open();
            RefreshFontSelector(onFontSelected);
        }

        public void RefreshFontSelector(Action<string> onFontSelected)
        {
            FontSelectorPopup.SearchField.onValueChanged.ClearAll();
            FontSelectorPopup.SearchField.onValueChanged.AddListener(_val => RefreshFontSelector(onFontSelected));
            FontSelectorPopup.ClearContent();

            foreach (var font in FontManager.inst.allFonts)
            {
                if (!RTString.SearchString(FontSelectorPopup.SearchTerm, font.Key))
                    continue;

                var gameObject = fontSelectionPrefab.Duplicate(FontSelectorPopup.Content, font.Key);
                RectValues.Default.SizeDelta(0f, 32f).AssignToRectTransform(gameObject.transform.AsRT());

                var image = gameObject.GetComponent<Image>();

                var text = gameObject.transform.Find("label/text").GetComponent<TextMeshProUGUI>();
                text.text = $"<font={font.Key}>ABCDEF abcdef 123</font> - {font.Key}";

                var button = gameObject.GetComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(() =>
                {
                    onFontSelected?.Invoke($"<font={font.Key}>");
                    FontSelectorPopup.Close();
                });

                RectValues.FullAnchored.Pivot(0f, 1f).SizeDelta(722f, 22f).AssignToRectTransform(gameObject.transform.Find("label").AsRT());
                RectValues.FullAnchored.Pivot(0f, 1f).SizeDelta(722f, 22f).AssignToRectTransform(gameObject.transform.Find("label/text").AsRT());

                EditorThemeManager.ApplyGraphic(text, ThemeGroup.Light_Text);
                EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
            }
        }

        /// <summary>
        /// Refreshes the debugger.
        /// </summary>
        public void RefreshDebugger()
        {
            for (int i = 0; i < debugs.Count; i++)
                DebuggerPopup.Content.GetChild(i).gameObject.SetActive(RTString.SearchString(debugSearch, debugs[i]));
        }

        /// <summary>
        /// Refreshes the screenshots in the screenshots folder.
        /// </summary>
        public void RefreshScreenshots()
        {
            var directory = RTFile.ApplicationDirectory + CoreConfig.Instance.ScreenshotsPath.Value;

            LSHelpers.DeleteChildren(screenshotContent);
            var files = Directory.GetFiles(directory, FileFormat.PNG.ToPattern(), SearchOption.TopDirectoryOnly);
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

                StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{files[i]}", texture2D =>
                {
                    if (!image)
                        return;

                    image.enabled = true;
                    image.sprite = SpriteHelper.CreateSprite(texture2D);
                }));
            }
        }

        /// <summary>
        /// Plays an editor dialogs' animation.
        /// </summary>
        /// <param name="gameObject">Game object of the editor dialog.</param>
        /// <param name="dialogName">Name of the editor dialog.</param>
        /// <param name="active">Active state to apply to the editor dialog.</param>
        public void PlayDialogAnimation(GameObject gameObject, string dialogName, bool active)
        {
            var play = EditorConfig.Instance.PlayEditorAnimations.Value;

            EditorAnimation dialogAnimation = null;
            var hasAnimation = editorAnimations.TryFind(x => x.name == dialogName, out dialogAnimation);

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
                        dialog.localPosition = new Vector3(active ? dialogAnimation.PosEnd.x : dialogAnimation.PosStart.x, active ? dialogAnimation.PosEnd.y : dialogAnimation.PosStart.y, 0f);
                    if (dialogAnimation.ScaActive)
                        dialog.localScale = new Vector3(active ? dialogAnimation.ScaEnd.x : dialogAnimation.ScaStart.x, dialogAnimation.ScaEnd.y, 1f);
                    if (dialogAnimation.RotActive)
                        dialog.localRotation = Quaternion.Euler(0f, 0f, active ? dialogAnimation.RotEnd : dialogAnimation.RotStart);

                    AnimationManager.inst.Remove(animation.id);
                };

                AnimationManager.inst.Play(animation);
            }

            if (!play || !hasAnimation || active)
                gameObject.SetActive(active);
        }

        public void ShowDialog(string name) => EditorManager.inst.ShowDialog(name);

        public void HideDialog(string name) => EditorManager.inst.HideDialog(name);

        /// <summary>
        /// Sets a dialogs' status.
        /// </summary>
        /// <param name="dialogName">The dialog to set the status of.</param>
        /// <param name="active">The active state.</param>
        /// <param name="focus">If the dialog should set as the current dialog.</param>
        public void SetDialogStatus(string dialogName, bool active, bool focus = true)
        {
            if (editorDialogs.TryFind(x => x.Name == dialogName, out EditorDialog dialog))
            {
                if (focus && active)
                {
                    EditorDialog.CurrentDialog?.Close();
                    EditorDialog.CurrentDialog = dialog;
                }

                dialog.SetActive(active);
                return;
            }

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

        // todo: replace this with a different system that can be customized more and is better coding-wise.
        public List<EditorAnimation> editorAnimations = new List<EditorAnimation>
        {
            new EditorAnimation(EditorPopup.OPEN_FILE_POPUP)
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
            new EditorAnimation(EditorPopup.NEW_FILE_POPUP)
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
            new EditorAnimation(EditorPopup.SAVE_AS_POPUP)
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
            new EditorAnimation(EditorPopup.QUICK_ACTIONS_POPUP)
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
            new EditorAnimation(EditorPopup.PARENT_SELECTOR)
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
            new EditorAnimation(EditorPopup.PREFAB_POPUP)
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
            new EditorAnimation(EditorPopup.OBJECT_OPTIONS_POPUP)
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
            new EditorAnimation(EditorPopup.BG_OPTIONS_POPUP)
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
            new EditorAnimation(EditorPopup.BROWSER_POPUP)
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
            new EditorAnimation(EditorPopup.OBJECT_SEARCH_POPUP)
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
            new EditorAnimation(EditorPopup.OBJECT_TEMPLATES_POPUP)
            {
                ActiveConfig = EditorConfig.Instance.ObjectTemplatePopupActive,

                PosActiveConfig = EditorConfig.Instance.ObjectTemplatePopupPosActive,
                PosOpenConfig = EditorConfig.Instance.ObjectTemplatePopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.ObjectTemplatePopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.ObjectTemplatePopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.ObjectTemplatePopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.ObjectTemplatePopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.ObjectTemplatePopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.ObjectTemplatePopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.ObjectTemplatePopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.ObjectTemplatePopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.ObjectTemplatePopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.ObjectTemplatePopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.ObjectTemplatePopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.ObjectTemplatePopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.ObjectTemplatePopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.ObjectTemplatePopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.ObjectTemplatePopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.ObjectTemplatePopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.ObjectTemplatePopupRotActive,
                RotOpenConfig = EditorConfig.Instance.ObjectTemplatePopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.ObjectTemplatePopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.ObjectTemplatePopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.ObjectTemplatePopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.ObjectTemplatePopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.ObjectTemplatePopupRotCloseEase,
            },
            new EditorAnimation(EditorPopup.WARNING_POPUP)
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
            new EditorAnimation(EditorPopup.FOLDER_CREATOR_POPUP)
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
            new EditorAnimation(EditorPopup.TEXT_EDITOR)
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
            new EditorAnimation(EditorPopup.DOCUMENTATION_POPUP)
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
            new EditorAnimation(EditorPopup.DEBUGGER_POPUP)
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
            new EditorAnimation(EditorPopup.AUTOSAVE_POPUP)
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
            new EditorAnimation(EditorPopup.DEFAULT_MODIFIERS_POPUP)
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
            new EditorAnimation(EditorPopup.KEYBIND_LIST_POPUP)
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
            new EditorAnimation(EditorPopup.THEME_POPUP)
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
            new EditorAnimation(EditorPopup.PREFAB_TYPES_POPUP)
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
            new EditorAnimation(EditorPopup.FONT_SELECTOR_POPUP)
            {
                ActiveConfig = EditorConfig.Instance.FontSelectorPopupActive,

                PosActiveConfig = EditorConfig.Instance.FontSelectorPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.FontSelectorPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.FontSelectorPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.FontSelectorPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.FontSelectorPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.FontSelectorPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.FontSelectorPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.FontSelectorPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.FontSelectorPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.FontSelectorPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.FontSelectorPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.FontSelectorPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.FontSelectorPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.FontSelectorPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.FontSelectorPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.FontSelectorPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.FontSelectorPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.FontSelectorPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.FontSelectorPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.FontSelectorPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.FontSelectorPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.FontSelectorPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.FontSelectorPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.FontSelectorPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.FontSelectorPopupRotCloseEase,
            },

            new EditorAnimation("File Dropdown")
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
            new EditorAnimation("Edit Dropdown")
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
            new EditorAnimation("View Dropdown")
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
            new EditorAnimation("Steam Dropdown")
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
            new EditorAnimation("Help Dropdown")
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

        #endregion

        #region Misc Functions

        public void OpenLevelListFolder() => RTFile.OpenInFileBrowser.Open(RTFile.CombinePaths(RTFile.ApplicationDirectory, editorListPath));
        public void OpenThemeListFolder() => RTFile.OpenInFileBrowser.Open(RTFile.CombinePaths(RTFile.ApplicationDirectory, themeListPath));
        public void OpenPrefabListFolder() => RTFile.OpenInFileBrowser.Open(RTFile.CombinePaths(RTFile.ApplicationDirectory, prefabListPath));

        public void ZIPLevel(string currentPath)
        {
            EditorManager.inst.DisplayNotification($"Zipping {Path.GetFileName(RTFile.RemoveEndSlash(currentPath))}...", 2f, EditorManager.NotificationType.Warning);

            IZIPLevel(currentPath).StartAsync();
        }

        public IEnumerator IZIPLevel(string currentPath)
        {
            bool failed;
            var zipPath = RTFile.RemoveEndSlash(currentPath) + FileFormat.ZIP.Dot();
            try
            {
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
                EditorManager.inst.DisplayNotification($"Successfully zipped the folder to {Path.GetFileName(zipPath)}!", 2f, EditorManager.NotificationType.Success);
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

            RTFile.CopyFile(RTFile.CombinePaths(currentPath, Level.LEVEL_OGG), RTFile.CombinePaths(path, Level.AUDIO_OGG));
            RTFile.CopyFile(RTFile.CombinePaths(currentPath, Level.LEVEL_WAV), RTFile.CombinePaths(path, Level.AUDIO_WAV));
            RTFile.CopyFile(RTFile.CombinePaths(currentPath, Level.LEVEL_MP3), RTFile.CombinePaths(path, Level.AUDIO_MP3));

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

            if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.renderer)
            {
                var color = AudioManager.inst.CurrentAudioSource.time < beatmapObject.StartTime ? CoreHelper.CurrentBeatmapTheme.GetObjColor((int)beatmapObject.events[3][0].values[0])
                    : AudioManager.inst.CurrentAudioSource.time > beatmapObject.StartTime + beatmapObject.GetObjectLifeLength() && beatmapObject.autoKillType != BeatmapObject.AutoKillType.OldStyleNoAutokill
                    ? CoreHelper.CurrentBeatmapTheme.GetObjColor((int)beatmapObject.events[3][beatmapObject.events[3].Count - 1].values[0])
                    : levelObject.visualObject.renderer.material.HasProperty("_Color") ? levelObject.visualObject.renderer.material.color : Color.white;

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
            RTFile.MoveDirectory(RTFile.CombinePaths(RTFile.ApplicationDirectory, editorListSlash, level), RTFile.CombinePaths(recyclingPath, level));
        }

        public static float SnapToBPM(float time)
        {
            var signature = RTSettingEditor.inst.BPMMulti / inst.editorInfo.timeSignature;
            return (Mathf.RoundToInt((time - inst.editorInfo.bpmOffset) / signature) * signature) + inst.editorInfo.bpmOffset;
        }

        public static float SnapToBPM(float time, float offset, float divisions, float bpm) => Mathf.RoundToInt((time + offset) / (60f / bpm / divisions)) * (60f / bpm / divisions) - offset;

        public static void SetActive(GameObject gameObject, bool active)
        {
            gameObject.SetActive(active);
            gameObject.transform.parent.GetChild(gameObject.transform.GetSiblingIndex() - 1).gameObject.SetActive(active);
        }

        public void ConvertVGToLS()
        {
            BrowserPopup.Open();
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

                                string fileName = $"{RTFile.FormatLegacyFileName(prefab.name)}{FileFormat.LSP.Dot()}";
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
                                BrowserPopup.Open();
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

                                    var level = GameData.ParseVG(levelVGJN, metadata.Version);

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
                    BrowserPopup.Close();
            });
        }

        const string STORY_LEVELS_COMPILER_PATH = "C:/Users/Mecha/Documents/Project Arrhythmia/Unity/BetterLegacyEditor/Assets/Story Levels";

        /// <summary>
        /// Debug only.
        /// </summary>
        public void ToStoryLevel(int chapter, int level, string type = "", int cutscene = 0)
        {
            var path = RTFile.BasePath;
            var doc = $"doc{RTString.ToStoryNumber(chapter)}";
            var saveTo = RTFile.CombinePaths(STORY_LEVELS_COMPILER_PATH, doc, $"{doc}_{RTString.ToStoryNumber(level)}{(string.IsNullOrEmpty(type) ? "" : "_" + type + RTString.ToStoryNumber(cutscene))}");

            RTFile.CreateDirectory(saveTo);
            RTFile.CopyFile(RTFile.CombinePaths(path, Level.LEVEL_LSB), RTFile.CombinePaths(saveTo, $"level{FileFormat.JSON.Dot()}"));
            RTFile.CopyFile(RTFile.CombinePaths(path, Level.METADATA_LSB), RTFile.CombinePaths(saveTo, $"metadata{FileFormat.JSON.Dot()}"));
            RTFile.CopyFile(RTFile.CombinePaths(path, Level.PLAYERS_LSB), RTFile.CombinePaths(saveTo, $"players{FileFormat.JSON.Dot()}"));
            RTFile.CopyFile(RTFile.CombinePaths(path, Level.LEVEL_OGG), RTFile.CombinePaths(saveTo, $"song{FileFormat.OGG.Dot()}"));
            RTFile.CopyFile(RTFile.CombinePaths(path, Level.LEVEL_JPG), RTFile.CombinePaths(saveTo, Level.COVER_JPG));

            EditorManager.inst.DisplayNotification("Saved the level to the story level compiler.", 2f, EditorManager.NotificationType.Success);
        }

        #endregion
    }
}
