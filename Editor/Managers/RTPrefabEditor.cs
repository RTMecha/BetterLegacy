using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Popups;
using BetterLegacy.Companion;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BasePrefab = DataManager.GameData.Prefab;
using BetterLegacy.Companion.Entity;

namespace BetterLegacy.Editor.Managers
{
    public class RTPrefabEditor : MonoBehaviour
    {
        public static RTPrefabEditor inst;

        #region Variables

        public RectTransform prefabPopups;
        public Button selectQuickPrefabButton;
        public Text selectQuickPrefabText;

        public InputField prefabCreatorName;
        public InputField prefabCreatorOffset;
        public Slider prefabCreatorOffsetSlider;

        public bool savingToPrefab;
        public Prefab prefabToSaveFrom;

        public string externalSearchStr;
        public string internalSearchStr;

        public Transform prefabSelectorRight;
        public Transform prefabSelectorLeft;

        public FunctionButtonStorage typeSelector;
        public InputField nameIF;
        public Text objectCount;
        public Text prefabObjectCount;
        public Text prefabObjectTimelineCount;

        public bool createInternal;

        public bool selectingPrefab;

        public GameObject prefabTypePrefab;
        public GameObject prefabTypeTogglePrefab;

        public Button prefabTypeReloadButton;

        public string NewPrefabTypeID { get; set; }
        public string NewPrefabDescription { get; set; }

        public List<PrefabPanel> PrefabPanels { get; set; } = new List<PrefabPanel>();

        public static bool ImportPrefabsDirectly { get; set; }

        public EditorDialog PrefabCreator { get; set; }
        public EditorDialog PrefabObjectEditor { get; set; }
        public EditorDialog PrefabExternalEditor { get; set; }

        #endregion

        public static void Init() => PrefabEditor.inst?.gameObject?.AddComponent<RTPrefabEditor>();

        void Awake() => inst = this;

        void Start() => StartCoroutine(SetupUI());

        IEnumerator SetupUI()
        {
            while (!PrefabEditor.inst || !EditorManager.inst || !EditorManager.inst.EditorDialogsDictionary.ContainsKey("Prefab Popup") || EditorPrefabHolder.Instance == null || !EditorPrefabHolder.Instance.Function1Button)
                yield return null;

            // A
            {
                loadingPrefabTypes = true;
                PrefabEditor.inst.StartCoroutine(LoadPrefabs());
                PrefabEditor.inst.OffsetLine = PrefabEditor.inst.OffsetLinePrefab.Duplicate(EditorManager.inst.timeline.transform, "offset line");
                PrefabEditor.inst.OffsetLine.transform.AsRT().pivot = Vector2.one;

                var prefabPopup = EditorManager.inst.GetDialog("Prefab Popup").Dialog;
                PrefabEditor.inst.dialog = EditorManager.inst.GetDialog("Prefab Editor").Dialog;
                PrefabEditor.inst.externalPrefabDialog = prefabPopup.Find("external prefabs");
                PrefabEditor.inst.internalPrefabDialog = prefabPopup.Find("internal prefabs");

                var externalContextClickable = PrefabEditor.inst.externalPrefabDialog.gameObject.AddComponent<ContextClickable>();
                externalContextClickable.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Create folder", () =>
                        {
                            RTEditor.inst.ShowFolderCreator($"{RTFile.ApplicationDirectory}{RTEditor.prefabListPath}", () => { RTEditor.inst.UpdatePrefabPath(true); RTEditor.inst.HideNameEditor(); });
                        }),
                        new ButtonFunction("Create Prefab", () =>
                        {
                            PrefabEditor.inst.OpenDialog();
                            createInternal = false;
                        }),
                        new ButtonFunction(true),
                        new ButtonFunction("Paste", PastePrefab)
                        );
                };
                
                var internalContextClickable = PrefabEditor.inst.internalPrefabDialog.gameObject.AddComponent<ContextClickable>();
                internalContextClickable.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Create Prefab", () =>
                        {
                            PrefabEditor.inst.OpenDialog();
                            createInternal = true;
                        })
                        );
                };

                PrefabEditor.inst.externalSearch = PrefabEditor.inst.externalPrefabDialog.Find("search-box/search").GetComponent<InputField>();
                PrefabEditor.inst.internalSearch = PrefabEditor.inst.internalPrefabDialog.Find("search-box/search").GetComponent<InputField>();
                PrefabEditor.inst.externalContent = PrefabEditor.inst.externalPrefabDialog.Find("mask/content");
                PrefabEditor.inst.internalContent = PrefabEditor.inst.internalPrefabDialog.Find("mask/content");

                var externalSelectGUI = PrefabEditor.inst.externalPrefabDialog.gameObject.AddComponent<SelectGUI>();
                var internalSelectGUI = PrefabEditor.inst.internalPrefabDialog.gameObject.AddComponent<SelectGUI>();
                externalSelectGUI.ogPos = PrefabEditor.inst.externalPrefabDialog.position;
                internalSelectGUI.ogPos = PrefabEditor.inst.internalPrefabDialog.position;
                externalSelectGUI.target = PrefabEditor.inst.externalPrefabDialog;
                internalSelectGUI.target = PrefabEditor.inst.internalPrefabDialog;

                PrefabEditor.inst.internalPrefabDialog.Find("Panel/Text").GetComponent<Text>().text = "Internal Prefabs";

                PrefabEditor.inst.gridSearch = PrefabEditor.inst.dialog.Find("data/selection/search-box/search").GetComponent<InputField>();
                PrefabEditor.inst.gridContent = PrefabEditor.inst.dialog.Find("data/selection/mask/content");

                Destroy(PrefabEditor.inst.dialog.Find("data/type/types").GetComponent<VerticalLayoutGroup>());
            }

            // B
            {
                var dialog = EditorManager.inst.GetDialog("Prefab Selector").Dialog;
                prefabSelectorLeft = dialog.Find("data/left");
                prefabSelectorRight = dialog.Find("data/right");

                var objectEditorDialog = EditorManager.inst.GetDialog("Object Editor").Dialog;
                var contentBase = objectEditorDialog.Find("data/left/Scroll View/Viewport/Content");
                var scrollView = objectEditorDialog.Find("data/left/Scroll View").gameObject.Duplicate(prefabSelectorLeft);

                var parent = scrollView.transform.Find("Viewport/Content");

                LSHelpers.DeleteChildren(parent, true);

                var objectsToDelete = new List<GameObject>();
                for (int i = 0; i < parent.childCount; i++)
                    objectsToDelete.Add(parent.GetChild(i).gameObject);
                foreach (var child in objectsToDelete)
                    DestroyImmediate(child);

                scrollView.transform.AsRT().sizeDelta = new Vector2(383f, 690f);

                var objectsToParent = new List<Transform>();
                for (int i = 0; i < prefabSelectorLeft.childCount; i++)
                    objectsToParent.Add(prefabSelectorLeft.GetChild(i));
                foreach (var child in objectsToParent)
                    child.SetParent(parent);

                prefabSelectorLeft = parent;

                EditorHelper.LogAvailableInstances<PrefabEditor>();

                EditorThemeManager.AddGraphic(dialog.GetComponent<Image>(), ThemeGroup.Background_1);

                var eventDialogTMP = EditorManager.inst.GetDialog("Event Editor").Dialog.Find("data/right");

                var singleInput = eventDialogTMP.Find("move/position/x").gameObject.Duplicate(PrefabEditor.inst.transform);
                var vector2Input = eventDialogTMP.Find("move/position").gameObject.Duplicate(PrefabEditor.inst.transform);
                var labelTemp = eventDialogTMP.Find("move").transform.GetChild(8).gameObject.Duplicate(PrefabEditor.inst.transform);

                // Single
                {
                    var buttonLeft = singleInput.transform.Find("<").GetComponent<Button>();
                    var buttonRight = singleInput.transform.Find(">").GetComponent<Button>();

                    Destroy(buttonLeft.GetComponent<Animator>());
                    buttonLeft.transition = Selectable.Transition.ColorTint;

                    Destroy(buttonRight.GetComponent<Animator>());
                    buttonRight.transition = Selectable.Transition.ColorTint;
                }

                DestroyImmediate(prefabSelectorLeft.GetChild(4).gameObject);
                DestroyImmediate(prefabSelectorLeft.GetChild(4).gameObject);

                Action<Transform, string, string> labelGenerator = (Transform parent, string name, string x) =>
                {
                    var label = labelTemp.Duplicate(parent, $"{name.ToLower()} label");
                    var labelText = label.transform.GetChild(0).GetComponent<Text>();
                    labelText.text = x;
                    Destroy(label.transform.GetChild(1).gameObject);

                    EditorThemeManager.AddLightText(labelText);
                };

                Action<Transform, string, string, string> labelGenerator2 = (Transform parent, string name, string x, string y) =>
                {
                    var label = labelTemp.Duplicate(parent, $"{name.ToLower()} label");
                    var xLabel = label.transform.GetChild(0).GetComponent<Text>();
                    var yLabel = label.transform.GetChild(1).GetComponent<Text>();
                    xLabel.text = x;
                    yLabel.text = y;

                    EditorThemeManager.AddLightText(xLabel);
                    EditorThemeManager.AddLightText(yLabel);
                };

                // AutoKill
                labelGenerator(prefabSelectorLeft, "tod-dropdown", "Time of Death");

                var autoKillType = objectEditorDialog.Find("data/left/Scroll View/Viewport/Content/autokill/tod-dropdown").gameObject
                    .Duplicate(prefabSelectorLeft, "tod-dropdown", 14);
                var autoKillTypeDD = autoKillType.GetComponent<Dropdown>();
                autoKillTypeDD.options = CoreHelper.StringToOptionData("Regular", "Start Offset", "Song Time");

                EditorThemeManager.AddDropdown(autoKillTypeDD);

                var ako = singleInput.Duplicate(prefabSelectorLeft, "akoffset");
                EditorThemeManager.AddInputField(ako.GetComponent<InputField>());
                EditorThemeManager.AddSelectable(ako.transform.Find("<").GetComponent<Button>(), ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(ako.transform.Find(">").GetComponent<Button>(), ThemeGroup.Function_2, false);

                var setToCurrent = objectEditorDialog.Find("data/left/Scroll View/Viewport/Content/autokill/|").gameObject.Duplicate(prefabSelectorLeft.Find("akoffset"), "|");

                var setToCurrentButton = setToCurrent.GetComponent<Button>();
                Destroy(setToCurrent.GetComponent<Animator>());
                setToCurrentButton.transition = Selectable.Transition.ColorTint;

                EditorThemeManager.AddSelectable(setToCurrentButton, ThemeGroup.Function_2, false);

                // Parent
                var parentUI = contentBase.transform.Find("parent").gameObject.Duplicate(prefabSelectorLeft, "parent");
                var parent_more = contentBase.transform.Find("parent_more").gameObject.Duplicate(prefabSelectorLeft, "parent_more");

                var parentTextText = parentUI.transform.Find("text/text").GetComponent<Text>();
                var parentText = parentUI.transform.Find("text").GetComponent<Button>();
                var parentMore = parentUI.transform.Find("more").GetComponent<Button>();
                var parentParent = parentUI.transform.Find("parent").GetComponent<Button>();
                var parentClear = parentUI.transform.Find("clear parent").GetComponent<Button>();
                var parentPicker = parentUI.transform.Find("parent picker").GetComponent<Button>();
                var spawnOnce = parent_more.transform.Find("spawn_once").GetComponent<Toggle>();

                EditorThemeManager.AddGraphic(parentParent.image, ThemeGroup.Function_3, true);
                EditorThemeManager.AddGraphic(parentParent.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Function_3_Text);
                EditorThemeManager.AddSelectable(parentClear, ThemeGroup.Close);
                EditorThemeManager.AddGraphic(parentClear.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);
                EditorThemeManager.AddSelectable(parentPicker, ThemeGroup.Picker);
                EditorThemeManager.AddGraphic(parentPicker.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Picker_Icon);

                EditorThemeManager.AddSelectable(parentText, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(parentTextText, ThemeGroup.Function_2_Text);
                EditorThemeManager.AddSelectable(parentMore, ThemeGroup.Function_2, false);

                EditorThemeManager.AddToggle(spawnOnce, graphic: spawnOnce.transform.Find("Text").GetComponent<Text>());

                var array = new string[] { "pos", "sca", "rot", };
                for (int i = 0; i < 3; i++)
                {
                    var parentSetting = parent_more.transform.GetChild(i + 2);

                    var additive = parentSetting.Find($"{array[i]}_add");
                    var parallax = parentSetting.Find($"{array[i]}_parallax");

                    if (parentSetting.Find("text"))
                    {
                        var text = parentSetting.Find("text").GetComponent<Text>();
                        text.fontSize = 19;
                        EditorThemeManager.AddLightText(text);
                    }

                    var parentSettingType = parentSetting.Find(array[i]);
                    var parentSettingOffset = parentSetting.Find($"{array[i]}_offset");

                    EditorThemeManager.AddToggle(parentSettingType.GetComponent<Toggle>(), ThemeGroup.Background_1);
                    EditorThemeManager.AddGraphic(parentSettingType.transform.Find("Image").GetComponent<Image>(), ThemeGroup.Toggle_1_Check);
                    EditorThemeManager.AddInputField(parentSettingOffset.GetComponent<InputField>());
                    EditorThemeManager.AddToggle(additive.GetComponent<Toggle>(), ThemeGroup.Background_1);
                    var additiveImage = additive.transform.Find("Image").GetComponent<Image>();
                    EditorThemeManager.AddGraphic(additiveImage, ThemeGroup.Toggle_1_Check);
                    EditorThemeManager.AddInputField(parallax.GetComponent<InputField>());
                }

                // Time
                labelGenerator(prefabSelectorLeft, "time", "Time");

                var time = EditorPrefabHolder.Instance.NumberInputField.Duplicate(prefabSelectorLeft, "time");
                var timeStorage = time.GetComponent<InputFieldStorage>();
                EditorThemeManager.AddInputField(timeStorage.inputField);
                timeStorage.inputField.transform.AsRT().sizeDelta = new Vector2(135f, 32f);

                EditorThemeManager.AddSelectable(timeStorage.leftGreaterButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(timeStorage.leftButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(timeStorage.middleButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(timeStorage.rightButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(timeStorage.rightGreaterButton, ThemeGroup.Function_2, false);

                var timeParent = time.transform;

                var settingEditorDialog = EditorManager.inst.GetDialog("Settings Editor").Dialog;
                var locker = settingEditorDialog.Find("snap/toggle/toggle").gameObject.Duplicate(timeParent, "lock", 0);

                locker.transform.Find("Background/Checkmark").GetComponent<Image>().sprite = ObjEditor.inst.timelineObjectPrefabLock.transform.Find("lock (1)").GetComponent<Image>().sprite;

                EditorThemeManager.AddToggle(locker.GetComponent<Toggle>());

                var collapser = objectEditorDialog.Find("data/left/Scroll View/Viewport/Content/autokill/collapse").gameObject.Duplicate(timeParent, "collapse", 1);

                EditorThemeManager.AddToggle(collapser.GetComponent<Toggle>(), ThemeGroup.Background_1);

                for (int i = 0; i < collapser.transform.Find("dots").childCount; i++)
                    EditorThemeManager.AddGraphic(collapser.transform.Find("dots").GetChild(i).GetComponent<Image>(), ThemeGroup.Dark_Text);

                // Position
                labelGenerator2(prefabSelectorLeft, "pos", "Position X Offset", "Position Y Offset");

                var position = vector2Input.Duplicate(prefabSelectorLeft, "position");
                var positionX = position.transform.Find("x").gameObject.AddComponent<InputFieldSwapper>();
                positionX.Init(position.transform.Find("x").GetComponent<InputField>());
                var positionY = position.transform.Find("y").gameObject.AddComponent<InputFieldSwapper>();
                positionY.Init(position.transform.Find("y").GetComponent<InputField>());
                EditorThemeManager.AddInputFields(position, true, "");
                EditorHelper.SetComplexity(prefabSelectorLeft.Find($"pos label").gameObject, Complexity.Normal);
                EditorHelper.SetComplexity(position, Complexity.Normal);

                // Scale
                labelGenerator2(prefabSelectorLeft, "sca", "Scale X Offset", "Scale Y Offset");

                var scale = vector2Input.Duplicate(prefabSelectorLeft, "scale");
                var scaleX = scale.transform.Find("x").gameObject.AddComponent<InputFieldSwapper>();
                scaleX.Init(scale.transform.Find("x").GetComponent<InputField>());
                var scaleY = scale.transform.Find("y").gameObject.AddComponent<InputFieldSwapper>();
                scaleY.Init(scale.transform.Find("y").GetComponent<InputField>());
                EditorThemeManager.AddInputFields(scale, true, "");
                EditorHelper.SetComplexity(prefabSelectorLeft.Find($"sca label").gameObject, Complexity.Normal);
                EditorHelper.SetComplexity(scale, Complexity.Normal);

                // Rotation
                labelGenerator(prefabSelectorLeft, "rot", "Rotation Offset");

                var rot = vector2Input.Duplicate(prefabSelectorLeft, "rotation");
                Destroy(rot.transform.GetChild(1).gameObject);
                var rotX = rot.transform.Find("x").gameObject.AddComponent<InputFieldSwapper>();
                rotX.Init(rot.transform.Find("x").GetComponent<InputField>());
                EditorThemeManager.AddInputFields(rot, true, "");
                EditorHelper.SetComplexity(prefabSelectorLeft.Find($"rot label").gameObject, Complexity.Normal);
                EditorHelper.SetComplexity(rot, Complexity.Normal);

                // Repeat
                labelGenerator2(prefabSelectorLeft, "repeat", "Repeat Count", "Repeat Offset Time");

                var repeat = vector2Input.Duplicate(prefabSelectorLeft, "repeat");
                EditorThemeManager.AddInputFields(repeat, true, "");

                // Speed
                labelGenerator(prefabSelectorLeft, "speed", "Speed");

                var speed = singleInput.Duplicate(prefabSelectorLeft, "speed");
                EditorThemeManager.AddInputField(speed.GetComponent<InputField>());
                EditorThemeManager.AddSelectable(speed.transform.Find("<").GetComponent<Button>(), ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(speed.transform.Find(">").GetComponent<Button>(), ThemeGroup.Function_2, false);

                // Layers
                var layersIF = singleInput.Duplicate(prefabSelectorLeft.Find("editor"), "layers", 0).GetComponent<InputField>();
                layersIF.gameObject.AddComponent<ContrastColors>().Init(layersIF.textComponent, layersIF.image);
                EditorThemeManager.AddGraphic(layersIF.image, ThemeGroup.Null, true);
                EditorThemeManager.AddSelectable(layersIF.transform.Find("<").GetComponent<Button>(), ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(layersIF.transform.Find(">").GetComponent<Button>(), ThemeGroup.Function_2, false);

                // Name
                labelGenerator(prefabSelectorRight, "name", "Name");
                try
                {
                    EditorHelper.SetComplexity(prefabSelectorRight.Find("name label").gameObject, Complexity.Normal);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogError($"Why name label {ex}");
                }

                var prefabName = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(prefabSelectorRight, "name");
                prefabName.transform.localScale = Vector3.one;

                var prefabNameInputField = prefabName.GetComponent<InputField>();

                prefabNameInputField.characterValidation = InputField.CharacterValidation.None;
                prefabNameInputField.contentType = InputField.ContentType.Standard;
                prefabNameInputField.characterLimit = 0;
                nameIF = prefabNameInputField;
                EditorThemeManager.AddInputField(prefabNameInputField);
                try
                {
                    EditorHelper.SetComplexity(prefabName, Complexity.Normal);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogError($"Why name {ex}");
                }

                // Type
                labelGenerator(prefabSelectorRight, "type", "Type");
                try
                {
                    EditorHelper.SetComplexity(prefabSelectorRight.Find("type label").gameObject, Complexity.Normal);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogError($"Why type label {ex}");
                }

                var type = EditorPrefabHolder.Instance.Function1Button.Duplicate(prefabSelectorRight, "type");

                var typeButton = type.GetComponent<FunctionButtonStorage>();

                type.transform.AsRT().sizeDelta = new Vector2(371f, 32f);

                typeSelector = typeButton;
                type.gameObject.AddComponent<ContrastColors>().Init(typeButton.text, typeButton.button.image);
                EditorThemeManager.AddGraphic(typeButton.button.image, ThemeGroup.Null, true);
                try
                {
                    EditorHelper.SetComplexity(type, Complexity.Normal);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogError($"Why type {ex}");
                }

                var expandPrefabLabel = prefabSelectorLeft.GetChild(0).gameObject;
                var expandPrefabLabelText = expandPrefabLabel.transform.GetChild(0).GetComponent<Text>();
                var expandPrefab = prefabSelectorLeft.GetChild(1).gameObject;
                var expandPrefabButton = expandPrefab.GetComponent<Button>();
                var expandPrefabText = expandPrefab.transform.GetChild(0).GetComponent<Text>();
                EditorThemeManager.AddLightText(expandPrefabLabelText);
                Destroy(expandPrefab.GetComponent<Animator>());
                expandPrefabButton.transition = Selectable.Transition.ColorTint;
                EditorThemeManager.AddSelectable(expandPrefabButton, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(expandPrefabText, ThemeGroup.Function_2_Text);

                // Save Prefab
                var label = expandPrefabLabel.Duplicate(prefabSelectorRight, "save prefab label");
                label.transform.localScale = Vector3.one;
                var applyToAllText = label.transform.GetChild(0).GetComponent<Text>();
                applyToAllText.fontSize = 19;
                applyToAllText.text = "Apply to an External Prefab";

                var savePrefab = expandPrefab.Duplicate(prefabSelectorRight, "save prefab");
                savePrefab.transform.localScale = Vector3.one;
                var savePrefabText = savePrefab.transform.GetChild(0).GetComponent<Text>();
                savePrefabText.text = "Select Prefab";

                EditorThemeManager.AddLightText(applyToAllText);
                var savePrefabButton = savePrefab.GetComponent<Button>();
                Destroy(savePrefab.GetComponent<Animator>());
                savePrefabButton.transition = Selectable.Transition.ColorTint;
                EditorThemeManager.AddSelectable(savePrefabButton, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(savePrefabText, ThemeGroup.Function_2_Text);
                EditorHelper.SetComplexity(label, Complexity.Normal);
                EditorHelper.SetComplexity(savePrefab, Complexity.Normal);

                Action<string, string, Action<Text, string>> countGenerator = (string name, string count, Action<Text, string> text) =>
                {
                    var rotLabel = labelTemp.Duplicate(prefabSelectorRight, name);

                    Destroy(rotLabel.transform.GetChild(1).gameObject);

                    text(rotLabel.transform.GetChild(0).GetComponent<Text>(), count);
                    EditorHelper.SetComplexity(rotLabel, Complexity.Normal);
                };

                // Object Count
                countGenerator("count label", "Object Count: 0", (Text text, string count) =>
                {
                    objectCount = text;
                    objectCount.text = count;

                    EditorThemeManager.AddLightText(text);
                });

                // Prefab Object Count
                countGenerator("count label", "Prefab Object Count: 0", (Text text, string count) =>
                {
                    prefabObjectCount = text;
                    prefabObjectCount.text = count;

                    EditorThemeManager.AddLightText(text);
                });

                // Prefab Object Timeline Count
                countGenerator("count label", "Prefab Object (Timeline) Count: 0", (Text text, string count) =>
                {
                    prefabObjectTimelineCount = text;
                    prefabObjectTimelineCount.text = count;

                    EditorThemeManager.AddLightText(text);
                });

                DestroyImmediate(prefabSelectorRight.Find("time").gameObject);
                var offsetTime = EditorPrefabHolder.Instance.NumberInputField.Duplicate(prefabSelectorRight, "time", 1);
                offsetTime.transform.GetChild(0).name = "time";
                var offsetTimeStorage = offsetTime.GetComponent<InputFieldStorage>();
                Destroy(offsetTimeStorage.middleButton.gameObject);
                EditorThemeManager.AddInputField(offsetTimeStorage.inputField);
                EditorThemeManager.AddSelectable(offsetTimeStorage.leftGreaterButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(offsetTimeStorage.leftButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(offsetTimeStorage.rightButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(offsetTimeStorage.rightGreaterButton, ThemeGroup.Function_2, false);

                // Object Editor list

                var prefabEditorData = PrefabEditor.inst.dialog.Find("data");

                EditorThemeManager.AddGraphic(prefabEditorData.Find("title/Panel/icon").GetComponent<Image>(), ThemeGroup.Light_Text);
                EditorThemeManager.AddLightText(prefabEditorData.Find("title/title").GetComponent<Text>());
                EditorThemeManager.AddLightText(prefabEditorData.Find("name/title").GetComponent<Text>());
                EditorThemeManager.AddLightText(prefabEditorData.Find("offset/title").GetComponent<Text>());
                EditorThemeManager.AddLightText(prefabEditorData.Find("type/title").GetComponent<Text>());
                EditorThemeManager.AddInputField(prefabEditorData.Find("name/input").GetComponent<InputField>());

                Destroy(prefabEditorData.Find("offset/<").gameObject);
                Destroy(prefabEditorData.Find("offset/>").gameObject);

                var offsetSlider = prefabEditorData.Find("offset/slider").GetComponent<Slider>();
                EditorThemeManager.AddGraphic(offsetSlider.transform.Find("Background").GetComponent<Image>(), ThemeGroup.Slider_2, true);

                EditorThemeManager.AddGraphic(offsetSlider.image, ThemeGroup.Slider_2_Handle, true);
                EditorThemeManager.AddInputField(prefabEditorData.Find("offset/input").GetComponent<InputField>());

                var prefabType = EditorPrefabHolder.Instance.Function1Button.gameObject.Duplicate(prefabEditorData.Find("type"), "Show Type Editor");

                Destroy(prefabEditorData.Find("type/types").gameObject);

                UIManager.SetRectTransform(prefabType.transform.AsRT(), new Vector2(-370f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(652f, 34f));
                prefabCreatorTypeButton = prefabType.GetComponent<Button>();
                prefabCreatorTypeButton.onClick.ClearAll();
                externalCreatorText = prefabType.transform.Find("Text").GetComponent<Text>();
                externalCreatorImage = prefabCreatorTypeButton.image;

                prefabType.AddComponent<ContrastColors>().Init(externalCreatorText, externalCreatorImage);
                EditorThemeManager.AddGraphic(prefabCreatorTypeButton.image, ThemeGroup.Null, true);

                prefabEditorData.Find("spacer").AsRT().sizeDelta = new Vector2(749f, 32f);
                prefabEditorData.Find("type").AsRT().sizeDelta = new Vector2(749f, 48f);

                var descriptionGO = prefabEditorData.Find("name").gameObject.Duplicate(prefabEditorData, "description", 4);
                descriptionGO.transform.AsRT().sizeDelta = new Vector2(749f, 108f);
                var descriptionTitle = descriptionGO.transform.Find("title").GetComponent<Text>();
                descriptionTitle.text = "Desc";
                EditorThemeManager.AddLightText(descriptionTitle);
                var descriptionInputField = descriptionGO.transform.Find("input").GetComponent<InputField>();
                ((Text)descriptionInputField.placeholder).alignment = TextAnchor.UpperLeft;
                ((Text)descriptionInputField.placeholder).text = "Enter description...";
                EditorThemeManager.AddInputField(descriptionInputField);

                var selection = prefabEditorData.Find("selection");
                EditorHelper.SetComplexity(selection.gameObject, Complexity.Advanced);
                selection.AsRT().sizeDelta = new Vector2(749f, 300f);
                var search = selection.Find("search-box/search").GetComponent<InputField>();
                search.onValueChanged.ClearAll();
                search.onValueChanged.AddListener(_val => ReloadSelectionContent());

                EditorThemeManager.AddInputField(search, ThemeGroup.Search_Field_2);
                var selectionGroup = selection.Find("mask/content").GetComponent<GridLayoutGroup>();
                selectionGroup.cellSize = new Vector2(172.5f, 32f);
                selectionGroup.constraintCount = 4;

                EditorThemeManager.AddGraphic(selection.GetComponent<Image>(), ThemeGroup.Background_3, true);

                EditorThemeManager.AddGraphic(PrefabEditor.inst.dialog.Find("submit/submit").GetComponent<Image>(), ThemeGroup.Add, true);
                EditorThemeManager.AddGraphic(PrefabEditor.inst.dialog.Find("submit/submit/Text").GetComponent<Text>(), ThemeGroup.Add_Text);

                var scrollbar = selection.Find("scrollbar").gameObject;
                EditorThemeManager.AddScrollbar(scrollbar.GetComponent<Scrollbar>(), scrollbar.GetComponent<Image>(), ThemeGroup.Scrollbar_2, ThemeGroup.Scrollbar_2_Handle);
            }

            // C
            {
                var transform = PrefabEditor.inst.dialog.Find("data/type/types");

                var list = new List<GameObject>();
                for (int i = 1; i < transform.childCount; i++)
                {
                    var tf = transform.Find($"col_{i}");
                    if (tf)
                        list.Add(tf.gameObject);
                }

                foreach (var go in list)
                    Destroy(go);

                prefabTypeTogglePrefab = transform.GetChild(0).gameObject;
                prefabTypeTogglePrefab.transform.SetParent(transform);
            }

            CreatePrefabTypesPopup();
            CreatePrefabExternalDialog();
            StartCoroutine(LoadPrefabTypes());

            prefabPopups = EditorManager.inst.GetDialog("Prefab Popup").Dialog.AsRT();
            selectQuickPrefabButton = PrefabEditor.inst.internalPrefabDialog.Find("select_prefab/select_toggle").GetComponent<Button>();
            selectQuickPrefabText = PrefabEditor.inst.internalPrefabDialog.Find("select_prefab/selected_prefab").GetComponent<Text>();

            var selectToggle = selectQuickPrefabButton.gameObject.AddComponent<ContextClickable>();
            selectToggle.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Assign", () =>
                    {
                        selectQuickPrefabText.text = "<color=#669e37>Selecting</color>";
                        PrefabEditor.inst.ReloadInternalPrefabsInPopup(true);
                    }),
                    new ButtonFunction("Remove", () =>
                    {
                        PrefabEditor.inst.currentPrefab = null;
                        RenderPopup();
                    })
                    );
            };

            try
            {
                prefabCreatorName = PrefabEditor.inst.dialog.Find("data/name/input").GetComponent<InputField>();

                prefabCreatorOffsetSlider = PrefabEditor.inst.dialog.Find("data/offset/slider").GetComponent<Slider>();
                prefabCreatorOffset = PrefabEditor.inst.dialog.Find("data/offset/input").GetComponent<InputField>();

                // Editor Theme
                {
                    #region External

                    EditorThemeManager.AddGraphic(PrefabEditor.inst.externalPrefabDialog.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Bottom_Left_I);

                    var externalPanel = PrefabEditor.inst.externalPrefabDialog.Find("Panel");
                    externalPanel.AsRT().sizeDelta = new Vector2(32f, 32f);
                    EditorThemeManager.AddGraphic(externalPanel.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);

                    var externalClose = externalPanel.Find("x").GetComponent<Button>();
                    Destroy(externalClose.GetComponent<Animator>());
                    externalClose.transition = Selectable.Transition.ColorTint;
                    externalClose.image.rectTransform.anchoredPosition = Vector2.zero;
                    EditorThemeManager.AddSelectable(externalClose, ThemeGroup.Close);
                    EditorThemeManager.AddGraphic(externalClose.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

                    EditorThemeManager.AddLightText(externalPanel.Find("Text").GetComponent<Text>());

                    EditorThemeManager.AddScrollbar(PrefabEditor.inst.externalPrefabDialog.transform.Find("Scrollbar").GetComponent<Scrollbar>(), scrollbarRoundedSide: SpriteHelper.RoundedSide.Bottom_Right_I);

                    EditorThemeManager.AddInputField(PrefabEditor.inst.externalSearch, ThemeGroup.Search_Field_2);

                    #endregion

                    #region Internal

                    EditorThemeManager.AddGraphic(PrefabEditor.inst.internalPrefabDialog.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Bottom_Left_I);

                    var internalPanel = PrefabEditor.inst.internalPrefabDialog.Find("Panel");
                    internalPanel.AsRT().sizeDelta = new Vector2(32f, 32f);
                    EditorThemeManager.AddGraphic(internalPanel.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);

                    var internalClose = internalPanel.Find("x").GetComponent<Button>();
                    Destroy(internalClose.GetComponent<Animator>());
                    internalClose.transition = Selectable.Transition.ColorTint;
                    internalClose.image.rectTransform.anchoredPosition = Vector2.zero;
                    EditorThemeManager.AddSelectable(internalClose, ThemeGroup.Close);
                    EditorThemeManager.AddGraphic(internalClose.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

                    EditorThemeManager.AddLightText(internalPanel.Find("Text").GetComponent<Text>());

                    EditorThemeManager.AddScrollbar(PrefabEditor.inst.internalPrefabDialog.transform.Find("Scrollbar").GetComponent<Scrollbar>(), scrollbarRoundedSide: SpriteHelper.RoundedSide.Bottom_Right_I);

                    EditorThemeManager.AddInputField(PrefabEditor.inst.internalSearch, ThemeGroup.Search_Field_2);

                    EditorThemeManager.AddGraphic(PrefabEditor.inst.internalPrefabDialog.Find("select_prefab").GetComponent<Image>(), ThemeGroup.Background_2, true, roundedSide: SpriteHelper.RoundedSide.Bottom_Left_I);

                    EditorThemeManager.AddSelectable(selectQuickPrefabButton, ThemeGroup.Function_2);
                    EditorThemeManager.AddGraphic(selectQuickPrefabButton.transform.GetChild(0).GetComponent<Text>(), ThemeGroup.Function_2_Text);
                    EditorThemeManager.AddGraphic(selectQuickPrefabText, ThemeGroup.Light_Text);

                    #endregion
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            try
            {
                PrefabCreator = new EditorDialog(EditorDialog.PREFAB_EDITOR);
                PrefabCreator.Init();
                PrefabObjectEditor = new EditorDialog(EditorDialog.PREFAB_SELECTOR);
                PrefabObjectEditor.Init();
                PrefabExternalEditor = new EditorDialog(EditorDialog.PREFAB_EXTERNAL_EDITOR);
                PrefabExternalEditor.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog
        }

        public Button prefabCreatorTypeButton;

        #region Prefab Objects

        public bool advancedParent;

        public void UpdateOffsets(PrefabObject currentPrefab)
        {
            var prefabObjects = GameData.Current.prefabObjects.FindAll(x => x.prefabID == currentPrefab.prefabID);
            var isObjectLayer = EditorTimeline.inst.layerType == EditorTimeline.LayerType.Objects;
            for (int i = 0; i < prefabObjects.Count; i++)
            {
                var prefabObject = prefabObjects[i];

                if (isObjectLayer && prefabObject.editorData.layer == EditorTimeline.inst.Layer)
                    EditorTimeline.inst.GetTimelineObject(prefabObject).RenderPosLength();

                Updater.UpdatePrefab(prefabObject, "Drag");
            }
            Updater.RecalculateObjectStates();
        }

        public void UpdateModdedVisbility()
        {
            if (!prefabSelectorLeft.gameObject.activeInHierarchy)
                return;

            prefabSelectorLeft.Find("tod-dropdown label").gameObject.SetActive(RTEditor.ShowModdedUI);
            prefabSelectorLeft.Find("tod-dropdown").gameObject.SetActive(RTEditor.ShowModdedUI);
            prefabSelectorLeft.Find("akoffset").gameObject.SetActive(RTEditor.ShowModdedUI);
            prefabSelectorLeft.Find("parent").gameObject.SetActive(RTEditor.ShowModdedUI);
            prefabSelectorLeft.Find("parent_more").gameObject.SetActive(RTEditor.ShowModdedUI && advancedParent);
            prefabSelectorLeft.Find("repeat label").gameObject.SetActive(RTEditor.ShowModdedUI);
            prefabSelectorLeft.Find("repeat").gameObject.SetActive(RTEditor.ShowModdedUI);
            prefabSelectorLeft.Find("speed label").gameObject.SetActive(RTEditor.ShowModdedUI);
            prefabSelectorLeft.Find("speed").gameObject.SetActive(RTEditor.ShowModdedUI);
        }

        public void RenderPrefabObjectParent(PrefabObject prefabObject)
        {
            string parent = prefabObject.parent;

            var parentTextText = prefabSelectorLeft.Find("parent/text/text").GetComponent<Text>();
            var parentText = prefabSelectorLeft.Find("parent/text").GetComponent<Button>();
            var parentMore = prefabSelectorLeft.Find("parent/more").GetComponent<Button>();
            var parent_more = prefabSelectorLeft.Find("parent_more");
            var parentParent = prefabSelectorLeft.Find("parent/parent").GetComponent<Button>();
            var parentClear = prefabSelectorLeft.Find("parent/clear parent").GetComponent<Button>();
            var parentPicker = prefabSelectorLeft.Find("parent/parent picker").GetComponent<Button>();
            var spawnOnce = parent_more.Find("spawn_once").GetComponent<Toggle>();
            var parentInfo = parentText.GetComponent<HoverTooltip>();

            parentText.transform.AsRT().sizeDelta = new Vector2(!string.IsNullOrEmpty(parent) ? 201f : 241f, 32f);

            parentParent.onClick.ClearAll();
            parentParent.onClick.AddListener(() => ObjectEditor.inst.ShowParentSearch(EditorTimeline.inst.GetTimelineObject(prefabObject)));

            parentClear.onClick.ClearAll();

            parentPicker.onClick.ClearAll();
            parentPicker.onClick.AddListener(() => RTEditor.inst.parentPickerEnabled = true);

            parentClear.gameObject.SetActive(!string.IsNullOrEmpty(parent));

            parent_more.AsRT().sizeDelta = new Vector2(351f, 152f);

            if (string.IsNullOrEmpty(parent))
            {
                parentText.interactable = false;
                parentMore.interactable = false;
                parent_more.gameObject.SetActive(false);
                parentTextText.text = "No Parent Object";

                parentInfo.tooltipLangauges[0].hint = string.IsNullOrEmpty(parent) ? "Object not parented." : "No parent found.";
                parentText.onClick.ClearAll();
                parentMore.onClick.ClearAll();

                return;
            }

            string p = null;

            if (GameData.Current.beatmapObjects.TryFind(x => x.id == parent, out BeatmapObject beatmapObjectParent))
            {
                p = beatmapObjectParent.name;
                parentInfo.tooltipLangauges[0].hint = "Currently selected parent.";
            }
            else if (parent == "CAMERA_PARENT")
            {
                p = "[CAMERA]";
                parentInfo.tooltipLangauges[0].hint = "Object parented to the camera.";
            }

            parentText.interactable = p != null;
            parentMore.interactable = p != null;

            parent_more.gameObject.SetActive(p != null && advancedParent);

            parentClear.onClick.AddListener(() =>
            {
                prefabObject.parent = "";

                // Since parent has no affect on the timeline object, we will only need to update the physical object.
                Updater.UpdatePrefab(prefabObject);
                RenderPrefabObjectParent(prefabObject);
            });

            if (p == null)
            {
                parentTextText.text = "No Parent Object";
                parentInfo.tooltipLangauges[0].hint = string.IsNullOrEmpty(parent) ? "Object not parented." : "No parent found.";
                parentText.onClick.ClearAll();
                parentMore.onClick.ClearAll();

                return;
            }

            parentTextText.text = p;

            parentText.onClick.ClearAll();
            parentText.onClick.AddListener(() =>
            {
                if (GameData.Current.beatmapObjects.Find(x => x.id == parent) != null &&
                    parent != "CAMERA_PARENT" &&
                    EditorTimeline.inst.timelineObjects.TryFind(x => x.ID == parent, out TimelineObject timelineObject))
                    EditorTimeline.inst.SetCurrentObject(timelineObject);
                else if (parent == "CAMERA_PARENT")
                {
                    EditorTimeline.inst.SetLayer(EditorTimeline.LayerType.Events);
                    EventEditor.inst.SetCurrentEvent(0, GameData.Current.ClosestEventKeyframe(0));
                }
            });

            parentMore.onClick.ClearAll();
            parentMore.onClick.AddListener(() =>
            {
                advancedParent = !advancedParent;
                parent_more.gameObject.SetActive(RTEditor.ShowModdedUI && advancedParent);
            });
            parent_more.gameObject.SetActive(RTEditor.ShowModdedUI && advancedParent);

            spawnOnce.onValueChanged.ClearAll();
            spawnOnce.gameObject.SetActive(true);
            spawnOnce.isOn = prefabObject.desync;
            spawnOnce.onValueChanged.AddListener(_val =>
            {
                prefabObject.desync = _val;
                Updater.UpdatePrefab(prefabObject);
            });

            for (int i = 0; i < 3; i++)
            {
                var _p = parent_more.GetChild(i + 2);

                var parentOffset = prefabObject.parentOffsets[i];

                var index = i;

                // Parent Type
                var tog = _p.GetChild(2).GetComponent<Toggle>();
                tog.onValueChanged.ClearAll();
                tog.isOn = prefabObject.GetParentType(i);
                tog.onValueChanged.AddListener(_val =>
                {
                    prefabObject.SetParentType(index, _val);

                    // Since updating parent type has no affect on the timeline object, we will only need to update the physical object.
                    Updater.UpdatePrefab(prefabObject);
                });

                // Parent Offset
                var pif = _p.GetChild(3).GetComponent<InputField>();
                var lel = _p.GetChild(3).GetComponent<LayoutElement>();
                lel.minWidth = 64f;
                lel.preferredWidth = 64f;
                pif.onValueChanged.ClearAll();
                pif.text = parentOffset.ToString();
                pif.onValueChanged.AddListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        prefabObject.SetParentOffset(index, num);

                        // Since updating parent type has no affect on the timeline object, we will only need to update the physical object.
                        Updater.UpdatePrefab(prefabObject);
                    }
                });

                TriggerHelper.AddEventTriggers(pif.gameObject, TriggerHelper.ScrollDelta(pif));

                var additive = _p.GetChild(4).GetComponent<Toggle>();
                additive.onValueChanged.ClearAll();
                additive.gameObject.SetActive(true);
                var parallax = _p.GetChild(5).GetComponent<InputField>();
                parallax.onValueChanged.ClearAll();
                parallax.gameObject.SetActive(true);

                additive.isOn = prefabObject.parentAdditive[i] == '1';
                additive.onValueChanged.AddListener(_val =>
                {
                    prefabObject.SetParentAdditive(index, _val);
                    Updater.UpdatePrefab(prefabObject);
                });
                parallax.text = prefabObject.parentParallax[index].ToString();
                parallax.onValueChanged.AddListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        prefabObject.parentParallax[index] = num;

                        // Since updating parent type has no affect on the timeline object, we will only need to update the physical object.
                        Updater.UpdatePrefab(prefabObject);
                    }
                });

                TriggerHelper.AddEventTriggers(parallax.gameObject, TriggerHelper.ScrollDelta(parallax));
            }
        }

        public void RenderPrefabObjectDialog(PrefabObject prefabObject)
        {
            var __instance = PrefabEditor.inst;

            #region Original Code

            var prefab = prefabObject.Prefab;

            var right = EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/right");

            var offsetTime = right.Find("time/time").GetComponent<InputField>();
            offsetTime.onValueChanged.ClearAll();
            offsetTime.text = prefab.Offset.ToString();
            offsetTime.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float offset))
                {
                    prefab.Offset = offset;
                    UpdateOffsets(prefabObject);
                }
            });
            TriggerHelper.IncreaseDecreaseButtons(offsetTime, t: right.transform.Find("time"));
            TriggerHelper.AddEventTriggers(offsetTime.gameObject, TriggerHelper.ScrollDelta(offsetTime));

            var offsetContextMenu = offsetTime.gameObject.GetOrAddComponent<ContextClickable>();
            offsetContextMenu.onClick = null;
            offsetContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Set to Timeline Cursor", () =>
                    {
                        var distance = AudioManager.inst.CurrentAudioSource.time - prefabObject.StartTime;

                        prefab.Offset -= distance;

                        var prefabObjects = GameData.Current.prefabObjects.FindAll(x => x.prefabID == prefabObject.prefabID);
                        var isObjectLayer = EditorTimeline.inst.layerType == EditorTimeline.LayerType.Objects;
                        for (int i = 0; i < prefabObjects.Count; i++)
                        {
                            var prefabObj = prefabObjects[i];
                            prefabObj.StartTime += distance;

                            if (isObjectLayer && prefabObj.editorData.layer == EditorTimeline.inst.Layer)
                                EditorTimeline.inst.GetTimelineObject(prefabObj).RenderPosLength();

                            Updater.UpdatePrefab(prefabObj, "Drag");
                        }
                        Updater.RecalculateObjectStates();
                    }));
            };

            prefabSelectorLeft.Find("editor/layer").gameObject.SetActive(false);
            prefabSelectorLeft.Find("editor/bin").gameObject.SetActive(false);
            prefabSelectorLeft.GetChild(2).GetChild(1).gameObject.SetActive(false);

            #endregion

            UpdateModdedVisbility();

            #region My Code

            if (RTEditor.ShowModdedUI)
            {
                var todDropdown = prefabSelectorLeft.Find("tod-dropdown").GetComponent<Dropdown>();

                todDropdown.onValueChanged.ClearAll();
                todDropdown.value = (int)prefabObject.autoKillType;
                todDropdown.onValueChanged.AddListener(_val =>
                {
                    prefabObject.autoKillType = (PrefabObject.AutoKillType)_val;
                    Updater.UpdatePrefab(prefabObject, "autokill");
                });

                var akoffset = prefabSelectorLeft.Find("akoffset").GetComponent<InputField>();

                akoffset.onValueChanged.ClearAll();
                akoffset.characterValidation = InputField.CharacterValidation.None;
                akoffset.contentType = InputField.ContentType.Standard;
                akoffset.characterLimit = 0;
                akoffset.text = prefabObject.autoKillOffset.ToString();
                akoffset.onValueChanged.AddListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        prefabObject.autoKillOffset = num;
                        if (prefabObject.autoKillType != PrefabObject.AutoKillType.Regular)
                            Updater.UpdatePrefab(prefabObject, "autokill");
                    }
                });

                TriggerHelper.IncreaseDecreaseButtons(akoffset);
                TriggerHelper.AddEventTriggers(akoffset.gameObject, TriggerHelper.ScrollDelta(akoffset));

                var setAutokill = prefabSelectorLeft.Find("akoffset/|").GetComponent<Button>();
                setAutokill.onClick.ClearAll();
                setAutokill.onClick.AddListener(() =>
                {
                    prefabObject.autoKillOffset = prefabObject.autoKillType == PrefabObject.AutoKillType.StartTimeOffset ? prefabObject.StartTime + prefab.Offset :
                                                    prefabObject.autoKillType == PrefabObject.AutoKillType.SongTime ? AudioManager.inst.CurrentAudioSource.time : -1f;
                });

                RenderPrefabObjectParent(prefabObject);
            }

            var startTime = prefabSelectorLeft.Find("time/input").GetComponent<InputField>();

            var parent = startTime.transform.parent;
            var locked = parent.Find("lock").GetComponent<Toggle>();
            locked.onValueChanged.ClearAll();
            locked.isOn = prefabObject.editorData.locked;
            locked.onValueChanged.AddListener(_val =>
            {
                prefabObject.editorData.locked = _val;
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(prefabObject));
            });

            var collapse = parent.Find("collapse").GetComponent<Toggle>();
            collapse.onValueChanged.ClearAll();
            collapse.isOn = prefabObject.editorData.collapse;
            collapse.onValueChanged.AddListener(_val =>
            {
                prefabObject.editorData.collapse = _val;
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(prefabObject));
            });

            startTime.onValueChanged.ClearAll();
            startTime.text = prefabObject.StartTime.ToString();
            startTime.onValueChanged.AddListener(_val =>
            {
                if (prefabObject.editorData.locked)
                    return;

                if (float.TryParse(_val, out float n))
                {
                    n = Mathf.Clamp(n, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                    prefabObject.StartTime = n;
                    Updater.UpdatePrefab(prefabObject, "starttime");
                    EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(prefabObject));
                }
                else
                    EditorManager.inst.DisplayNotification("Text is not correct format!", 1f, EditorManager.NotificationType.Error);
            });

            TriggerHelper.IncreaseDecreaseButtons(startTime, t: parent);
            TriggerHelper.AddEventTriggers(startTime.gameObject, TriggerHelper.ScrollDelta(startTime));

            var startTimeContextMenu = startTime.gameObject.GetOrAddComponent<ContextClickable>();
            startTimeContextMenu.onClick = null;
            startTimeContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Go to Start Time", () =>
                    {
                        AudioManager.inst.SetMusicTime(prefabObject.StartTime);
                    }),
                    new ButtonFunction("Go to Spawn Time", () =>
                    {
                        AudioManager.inst.SetMusicTime(prefabObject.StartTime + prefab.Offset);
                    }));
            };

            var startTimeSet = parent.Find("|").GetComponent<Button>();
            startTimeSet.onClick.ClearAll();
            startTimeSet.onClick.AddListener(() =>
            {
                if (prefabObject.editorData.locked)
                    return;

                startTime.text = AudioManager.inst.CurrentAudioSource.time.ToString();
            });

            //Layer
            {
                int currentLayer = prefabObject.editorData.layer;

                var layers = prefabSelectorLeft.Find("editor/layers").GetComponent<InputField>();

                layers.image.color = EditorTimeline.GetLayerColor(prefabObject.editorData.layer);
                layers.onValueChanged.ClearAll();
                layers.text = (prefabObject.editorData.layer + 1).ToString();
                layers.onValueChanged.AddListener(_val =>
                {
                    if (int.TryParse(_val, out int n))
                    {
                        currentLayer = prefabObject.editorData.layer;
                        int a = n - 1;
                        if (a < 0)
                            layers.text = "1";

                        prefabObject.editorData.layer = EditorTimeline.GetLayer(a);
                        layers.image.color = EditorTimeline.GetLayerColor(EditorTimeline.GetLayer(a));
                        EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(prefabObject));
                    }
                    else
                        EditorManager.inst.DisplayNotification("Text is not correct format!", 1f, EditorManager.NotificationType.Error);
                });

                TriggerHelper.IncreaseDecreaseButtons(layers);
                TriggerHelper.AddEventTriggers(layers.gameObject, TriggerHelper.ScrollDeltaInt(layers, min: 1, max: int.MaxValue));

                var editorLayerContextMenu = layers.gameObject.GetOrAddComponent<ContextClickable>();
                editorLayerContextMenu.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Go to Editor Layer", () => EditorTimeline.inst.SetLayer(prefabObject.editorData.Layer, EditorTimeline.LayerType.Objects))
                        );
                };
            }

            for (int i = 0; i < 3; i++)
            {
                int index = i;

                string[] types = new string[]
                {
                    "position",
                    "scale",
                    "rotation"
                };

                string type = types[index];
                string inx = "/x";
                string iny = "/y";

                var currentKeyframe = prefabObject.events[index];

                var inputField = prefabSelectorLeft.Find(type + inx).GetComponent<InputField>();

                inputField.onValueChanged.ClearAll();
                inputField.text = currentKeyframe.eventValues[0].ToString();
                inputField.onValueChanged.AddListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentKeyframe.eventValues[0] = num;
                        Updater.UpdatePrefab(prefabObject, "offset");
                    }
                });

                TriggerHelper.IncreaseDecreaseButtons(inputField);

                if (index != 2)
                {
                    var inputField2 = prefabSelectorLeft.Find(type + iny).GetComponent<InputField>();

                    inputField2.onValueChanged.ClearAll();
                    inputField2.text = currentKeyframe.eventValues[1].ToString();
                    inputField2.onValueChanged.AddListener(_val =>
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            currentKeyframe.eventValues[1] = num;
                            Updater.UpdatePrefab(prefabObject, "offset");
                        }
                    });

                    TriggerHelper.IncreaseDecreaseButtons(inputField2);
                    TriggerHelper.AddEventTriggers(inputField2.gameObject,
                        TriggerHelper.ScrollDelta(inputField2, multi: true),
                        TriggerHelper.ScrollDeltaVector2(inputField, inputField2, 0.1f, 10f));

                    TriggerHelper.AddEventTriggers(inputField.gameObject,
                        TriggerHelper.ScrollDelta(inputField, multi: true),
                        TriggerHelper.ScrollDeltaVector2(inputField, inputField2, 0.1f, 10f));
                }
                else
                    TriggerHelper.AddEventTriggers(inputField.gameObject, TriggerHelper.ScrollDelta(inputField, 15f, 3f));
            }

            if (RTEditor.ShowModdedUI)
            {
                var repeatCount = prefabSelectorLeft.Find("repeat/x").GetComponent<InputField>();
                repeatCount.characterValidation = InputField.CharacterValidation.Integer;
                repeatCount.contentType = InputField.ContentType.Standard;
                repeatCount.characterLimit = 5;
                repeatCount.onValueChanged.ClearAll();
                repeatCount.text = Mathf.Clamp(prefabObject.RepeatCount, 0, 1000).ToString();
                repeatCount.onValueChanged.AddListener(_val =>
                {
                    if (int.TryParse(_val, out int num))
                    {
                        num = Mathf.Clamp(num, 0, 1000);
                        prefabObject.RepeatCount = num;
                        Updater.UpdatePrefab(prefabObject);
                    }
                });

                TriggerHelper.IncreaseDecreaseButtonsInt(repeatCount, max: 1000);
                TriggerHelper.AddEventTriggers(repeatCount.gameObject, TriggerHelper.ScrollDeltaInt(repeatCount, max: 1000));

                var repeatOffsetTime = prefabSelectorLeft.Find("repeat/y").GetComponent<InputField>();

                repeatOffsetTime.characterValidation = InputField.CharacterValidation.Decimal;
                repeatOffsetTime.contentType = InputField.ContentType.Standard;
                repeatOffsetTime.characterLimit = 0;
                repeatOffsetTime.onValueChanged.ClearAll();
                repeatOffsetTime.text = Mathf.Clamp(prefabObject.RepeatOffsetTime, 0f, 60f).ToString();
                repeatOffsetTime.onValueChanged.AddListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        num = Mathf.Clamp(num, 0f, 60f);
                        prefabObject.RepeatOffsetTime = num;
                        Updater.UpdatePrefab(prefabObject, "Start Time");
                    }
                });

                TriggerHelper.IncreaseDecreaseButtons(repeatOffsetTime, max: 60f);
                TriggerHelper.AddEventTriggers(repeatOffsetTime.gameObject, TriggerHelper.ScrollDelta(repeatOffsetTime, max: 60f));

                var speed = prefabSelectorLeft.Find("speed").GetComponent<InputField>();

                speed.characterValidation = InputField.CharacterValidation.Decimal;
                speed.contentType = InputField.ContentType.Standard;
                speed.characterLimit = 0;
                speed.onValueChanged.ClearAll();
                speed.text = Mathf.Clamp(prefabObject.speed, 0.1f, Updater.MAX_PREFAB_OBJECT_SPEED).ToString();
                speed.onValueChanged.AddListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        num = Mathf.Clamp(num, 0.1f, Updater.MAX_PREFAB_OBJECT_SPEED);
                        prefabObject.speed = num;
                        Updater.UpdatePrefab(prefabObject, "Speed");
                    }
                });

                TriggerHelper.IncreaseDecreaseButtons(speed, min: 0.1f, max: Updater.MAX_PREFAB_OBJECT_SPEED);
                TriggerHelper.AddEventTriggers(speed.gameObject, TriggerHelper.ScrollDelta(speed, min: 0.1f, max: Updater.MAX_PREFAB_OBJECT_SPEED));
            }

            //Global Settings
            {
                nameIF.onValueChanged.ClearAll();
                nameIF.text = prefab.Name;
                nameIF.onValueChanged.AddListener(_val =>
                {
                    prefab.Name = _val;
                    foreach (var prefabObject in GameData.Current.prefabObjects.FindAll(x => x.prefabID == prefab.ID))
                        EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(prefabObject));
                });

                typeSelector.button.image.color = prefab.PrefabType.Color;
                typeSelector.text.text = prefab.PrefabType.Name;
                typeSelector.button.onClick.ClearAll();
                typeSelector.button.onClick.AddListener(() =>
                {
                    OpenPrefabTypePopup(prefab.typeID, id =>
                    {
                        prefab.Type = PrefabType.prefabTypeLSIDToIndex.TryGetValue(id, out int prefabTypeIndexLS) ? prefabTypeIndexLS : PrefabType.prefabTypeVGIDToIndex.TryGetValue(id, out int prefabTypeIndexVG) ? prefabTypeIndexVG : 0;
                        prefab.typeID = id;
                        typeSelector.button.image.color = prefab.PrefabType.Color;
                        typeSelector.text.text = prefab.PrefabType.Name;
                        EditorTimeline.inst.RenderTimelineObjects();
                    });
                });

                var savePrefab = prefabSelectorRight.Find("save prefab").GetComponent<Button>();
                savePrefab.onClick.ClearAll();
                savePrefab.onClick.AddListener(() =>
                {
                    savingToPrefab = true;
                    prefabToSaveFrom = prefab;

                    RTEditor.inst.PrefabPopups.Open();
                    RTEditor.inst.PrefabPopups.GameObject.transform.GetChild(0).gameObject.SetActive(false);

                    if (__instance.externalContent != null)
                        __instance.ReloadExternalPrefabsInPopup();

                    EditorManager.inst.DisplayNotification("Select an External Prefab to apply changes to.", 2f);
                });

                objectCount.text = "Object Count: " + prefab.objects.Count.ToString();
                prefabObjectCount.text = "Prefab Object Count: " + prefab.prefabObjects.Count;
                prefabObjectTimelineCount.text = "Prefab Object (Timeline) Count: " + GameData.Current.prefabObjects.FindAll(x => x.prefabID == prefab.ID).Count;
            }

            #endregion
        }

        public void CollapseCurrentPrefab()
        {
            if (!EditorTimeline.inst.CurrentSelection.isBeatmapObject)
            {
                EditorManager.inst.DisplayNotification("Can't collapse non-object.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            Collapse(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());
        }

        public void ExpandCurrentPrefab()
        {
            if (!EditorTimeline.inst.CurrentSelection.isPrefabObject)
            {
                EditorManager.inst.DisplayNotification("Can't expand non-prefab!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            Expand(EditorTimeline.inst.CurrentSelection.GetData<PrefabObject>());
        }

        public void Collapse(BeatmapObject beatmapObject)
        {
            if (!beatmapObject || string.IsNullOrEmpty(beatmapObject.prefabInstanceID))
            {
                EditorManager.inst.DisplayNotification("Beatmap Object does not have a Prefab Object reference.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var gameData = GameData.Current;
            var editorData = beatmapObject.editorData;
            string prefabInstanceID = beatmapObject.prefabInstanceID;
            var objects = gameData.beatmapObjects.FindAll(x => x.prefabInstanceID == prefabInstanceID);
            float startTime = objects.Min(x => x.StartTime);

            int index = gameData.prefabs.FindIndex(x => x.ID == beatmapObject.prefabID);
            var originalPrefab = gameData.prefabs[index];

            var prefabObject = new PrefabObject(originalPrefab.ID, startTime - originalPrefab.Offset);
            prefabObject.editorData.Bin = editorData.Bin;
            prefabObject.editorData.layer = editorData.layer;
            var newPrefab = new Prefab(originalPrefab.Name, originalPrefab.Type, originalPrefab.Offset, objects, new List<PrefabObject>());

            newPrefab.ID = originalPrefab.ID;
            newPrefab.typeID = originalPrefab.typeID;

            gameData.prefabs[index] = newPrefab;
            EditorTimeline.inst.timelineObjects.ForLoopReverse((timelineObject, index) =>
            {
                if (timelineObject.isPrefabObject || timelineObject.GetData<BeatmapObject>().prefabInstanceID != prefabInstanceID)
                    return;

                if (timelineObject.GameObject)
                {
                    timelineObject.GameObject.transform.SetParent(null);
                    Destroy(timelineObject.GameObject);
                }
                EditorTimeline.inst.timelineObjects.RemoveAt(index);
            });

            gameData.prefabObjects.Add(prefabObject);
            gameData.beatmapObjects.ForLoopReverse((beatmapObject, index) =>
            {
                if (beatmapObject.prefabInstanceID != prefabInstanceID || beatmapObject.fromPrefab)
                    return;

                Updater.UpdateObject(beatmapObject, reinsert: false, recalculate: false);
                gameData.beatmapObjects.RemoveAt(index);
            });

            Updater.AddPrefabToLevel(prefabObject, recalculate: false);

            gameData.prefabObjects.FindAll(x => x.prefabID == originalPrefab.ID).ForEach(x => Updater.UpdatePrefab(x, recalculate: false));
            Updater.RecalculateObjectStates();

            EditorTimeline.inst.SetCurrentObject(EditorTimeline.inst.GetTimelineObject(prefabObject));

            EditorManager.inst.DisplayNotification("Replaced all instances of Prefab!", 2f, EditorManager.NotificationType.Success);
        }

        public void CollapseNew(BeatmapObject beatmapObject)
        {
            if (!beatmapObject || string.IsNullOrEmpty(beatmapObject.prefabInstanceID))
            {
                EditorManager.inst.DisplayNotification("Beatmap Object does not have a Prefab Object reference.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var gameData = GameData.Current;
            var editorData = beatmapObject.editorData;
            string prefabInstanceID = beatmapObject.prefabInstanceID;
            var objects = gameData.beatmapObjects.FindAll(x => x.prefabInstanceID == prefabInstanceID);
            float startTime = objects.Min(x => x.StartTime);

            int index = gameData.prefabs.FindIndex(x => x.ID == beatmapObject.prefabID);
            var originalPrefab = gameData.prefabs[index];
            var newPrefab = Prefab.DeepCopy(originalPrefab);

            var prefabObject = new PrefabObject(newPrefab.ID, startTime - newPrefab.Offset);
            prefabObject.editorData.Bin = editorData.Bin;
            prefabObject.editorData.layer = editorData.layer;

            newPrefab.typeID = originalPrefab.typeID;

            int num = GameData.Current.prefabs.FindAll(x => Regex.Replace(x.Name, "( +\\[\\d+])", string.Empty) == newPrefab.Name).Count;
            if (num > 0)
                newPrefab.Name = $"{newPrefab.Name} [{num}]";

            GameData.Current.prefabs.Add(newPrefab);

            EditorTimeline.inst.timelineObjects.ForLoopReverse((timelineObject, index) =>
            {
                if (timelineObject.isPrefabObject || timelineObject.GetData<BeatmapObject>().prefabInstanceID != prefabInstanceID)
                    return;

                if (timelineObject.GameObject)
                {
                    timelineObject.GameObject.transform.SetParent(null);
                    Destroy(timelineObject.GameObject);
                }
                EditorTimeline.inst.timelineObjects.RemoveAt(index);
            });

            gameData.prefabObjects.Add(prefabObject);
            gameData.beatmapObjects.ForLoopReverse((beatmapObject, index) =>
            {
                if (beatmapObject.prefabInstanceID != prefabInstanceID || beatmapObject.fromPrefab)
                    return;

                Updater.UpdateObject(beatmapObject, reinsert: false, recalculate: false);
                gameData.beatmapObjects.RemoveAt(index);
            });

            Updater.AddPrefabToLevel(prefabObject, recalculate: false);
            Updater.RecalculateObjectStates();

            EditorTimeline.inst.SetCurrentObject(EditorTimeline.inst.GetTimelineObject(prefabObject));

            EditorManager.inst.DisplayNotification("Created new Prefab!", 2f, EditorManager.NotificationType.Success);
        }

        public void Expand(PrefabObject prefabObject)
        {
            string id = prefabObject.ID;

            EditorDialog.CurrentDialog.Close();

            var sw = CoreHelper.StartNewStopwatch();

            Debug.Log($"{PrefabEditor.inst.className}Removing Prefab Object's spawned objects.");
            Updater.UpdatePrefab(prefabObject, false, false);

            EditorTimeline.inst.RemoveTimelineObject(EditorTimeline.inst.timelineObjects.Find(x => x.ID == id));

            GameData.Current.prefabObjects.RemoveAll(x => x.ID == id);
            EditorTimeline.inst.DeselectAllObjects();

            Debug.Log($"{PrefabEditor.inst.className}Expanding Prefab Object.");
            StartCoroutine(AddExpandedPrefabToLevel(prefabObject));

            EditorTimeline.inst.RenderTimelineObjects();

            prefabObject = null;
        }

        public void AddPrefabObjectToLevel(BasePrefab prefab)
        {
            var prefabObject = new PrefabObject
            {
                ID = LSText.randomString(16),
                prefabID = prefab.ID,
                StartTime = EditorManager.inst.CurrentAudioPos,
            };

            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
                EditorTimeline.inst.SetLayer(EditorTimeline.LayerType.Objects);

            prefabObject.editorData.layer = EditorManager.inst.layer;

            for (int i = 0; i < prefabObject.events.Count; i++)
                prefabObject.events[i] = new EventKeyframe(prefabObject.events[i]);

            if (EditorConfig.Instance.SpawnPrefabsAtCameraCenter.Value)
            {
                var pos = EventManager.inst.cam.transform.position;
                prefabObject.events[0].eventValues[0] = pos.x;
                prefabObject.events[0].eventValues[1] = pos.y;
            }

            // Set default scale
            prefabObject.events[1].eventValues[0] = 1f;
            prefabObject.events[1].eventValues[1] = 1f;

            GameData.Current.prefabObjects.Add(prefabObject);

            Updater.AddPrefabToLevel(prefabObject);

            EditorTimeline.inst.SetCurrentObject(EditorTimeline.inst.GetTimelineObject(prefabObject));

            if (prefab.Name.Contains("Example"))
                Example.Current?.brain?.Notice(ExampleBrain.Notices.EXAMPLE_REFERENCE);
        }

        public bool expanding;
        public IEnumerator AddExpandedPrefabToLevel(PrefabObject prefabObject)
        {
            var updateExpandedObjectsYieldType = EditorConfig.Instance.UpdateExpandedObjectsYieldMode.Value;
            var expandObjectsYieldType = EditorConfig.Instance.ExpandObjectsYieldMode.Value;

            RTEditor.inst.ienumRunning = true;
            expanding = true;
            float delay = 0f;
            float audioTime = EditorManager.inst.CurrentAudioPos;

            var prefab = prefabObject.Prefab;

            var objectIDs = new List<IDPair>();
            for (int j = 0; j < prefab.objects.Count; j++)
                objectIDs.Add(new IDPair(prefab.objects[j].id));

            var sw = CoreHelper.StartNewStopwatch();

            var expandedObjects = new List<BeatmapObject>();
            var notParented = new List<BeatmapObject>();
            for (int i = 0; i < prefab.objects.Count; i++)
            {
                var beatmapObject = prefab.objects[i];
                if (i > 0 && expandObjectsYieldType != YieldType.None)
                    yield return CoreHelper.GetYieldInstruction(expandObjectsYieldType, ref delay);

                var beatmapObjectCopy = BeatmapObject.DeepCopy((BeatmapObject)beatmapObject, false);

                beatmapObjectCopy.id = objectIDs[i].newID;

                if (!string.IsNullOrEmpty(beatmapObject.parent) && objectIDs.TryFind(x => x.oldID == beatmapObject.parent, out IDPair idPair))
                    beatmapObjectCopy.parent = idPair.newID;
                else if (!string.IsNullOrEmpty(beatmapObject.parent) && beatmapObjectCopy.parent != BeatmapObject.CAMERA_PARENT && GameData.Current.beatmapObjects.FindIndex(x => x.id == beatmapObject.parent) == -1)
                    beatmapObjectCopy.parent = "";

                beatmapObjectCopy.active = false;
                beatmapObjectCopy.fromPrefab = false;
                beatmapObjectCopy.prefabID = prefab.ID;
                beatmapObjectCopy.StartTime += prefabObject.StartTime + prefab.Offset;

                beatmapObjectCopy.editorData.layer = prefabObject.editorData.layer;
                beatmapObjectCopy.editorData.Bin = beatmapObjectCopy.editorData.Bin;

                if (beatmapObjectCopy.shape == 6 && !string.IsNullOrEmpty(beatmapObjectCopy.text) && prefab.SpriteAssets.TryGetValue(beatmapObjectCopy.text, out Sprite sprite))
                    AssetManager.SpriteAssets[beatmapObject.text] = sprite;

                beatmapObjectCopy.prefabInstanceID = prefabObject.ID;
                GameData.Current.beatmapObjects.Add(beatmapObjectCopy);
                if (Updater.levelProcessor && Updater.levelProcessor.converter != null)
                    Updater.levelProcessor.converter.beatmapObjects[beatmapObjectCopy.id] = beatmapObjectCopy;

                if (string.IsNullOrEmpty(beatmapObjectCopy.parent) || beatmapObjectCopy.parent == BeatmapObject.CAMERA_PARENT || GameData.Current.beatmapObjects.FindIndex(x => x.id == beatmapObject.parent) != -1) // prevent updating of parented objects since updating is recursive.
                    notParented.Add(beatmapObjectCopy);
                expandedObjects.Add(beatmapObjectCopy);

                var timelineObject = new TimelineObject(beatmapObjectCopy);
                timelineObject.Selected = true;
                EditorTimeline.inst.CurrentSelection = timelineObject;

                EditorTimeline.inst.RenderTimelineObject(timelineObject);
            }

            var list = notParented.Count > 0 ? notParented : expandedObjects;
            delay = 0f;
            for (int i = 0; i < notParented.Count; i++)
            {
                if (i > 0 && updateExpandedObjectsYieldType != YieldType.None)
                    yield return CoreHelper.GetYieldInstruction(updateExpandedObjectsYieldType, ref delay);
                Updater.UpdateObject(notParented[i], recalculate: false);
            }

            Updater.RecalculateObjectStates();

            notParented.Clear();
            notParented = null;
            expandedObjects.Clear();
            expandedObjects = null;

            CoreHelper.StopAndLogStopwatch(sw);

            if (prefab.objects.Count > 1 || prefab.prefabObjects.Count > 1)
                MultiObjectEditor.inst.Dialog.Open();
            else if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                ObjectEditor.inst.OpenDialog(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());
            else if (EditorTimeline.inst.CurrentSelection.isPrefabObject)
                PrefabEditor.inst.OpenPrefabDialog();

            EditorManager.inst.DisplayNotification($"Expanded Prefab Object {prefab.Name} in {sw.Elapsed}!.", 5f, EditorManager.NotificationType.Success, false);
            RTEditor.inst.ienumRunning = false;
            expanding = false;
            sw = null;
            yield break;
        }

        #endregion

        #region Prefab Types

        void CreatePrefabTypesPopup()
        {
            var parent = EditorManager.inst.GetDialog("Quick Actions Popup").Dialog.parent;
            var gameObject = Creator.NewUIObject("Prefab Types Popup", parent, 9);

            var baseImage = gameObject.AddComponent<Image>();
            EditorThemeManager.AddGraphic(baseImage, ThemeGroup.Background_1);
            var baseSelectGUI = gameObject.AddComponent<SelectGUI>();

            gameObject.transform.AsRT().anchoredPosition = new Vector2(340f, 0f);
            gameObject.transform.AsRT().sizeDelta = new Vector2(400f, 600f);

            baseSelectGUI.target = gameObject.transform;
            baseSelectGUI.OverrideDrag = true;

            var panel = EditorManager.inst.GetDialog("Save As Popup").Dialog.Find("New File Popup/Panel").gameObject.Duplicate(gameObject.transform, "Panel");
            var panelRT = (RectTransform)panel.transform;
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = new Vector2(32f, 32f);

            var title = panel.transform.Find("Text").GetComponent<Text>();
            title.text = "Prefab Type Editor / Selector";
            var closeButton = panel.transform.Find("x").GetComponent<Button>();
            closeButton.onClick.ClearAll();
            closeButton.onClick.AddListener(() => RTEditor.inst.PrefabTypesPopup.Close());

            var refresh = Creator.NewUIObject("Refresh", panel.transform);
            UIManager.SetRectTransform(refresh.transform.AsRT(), new Vector2(-52f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(32f, 32f));

            var refreshImage = refresh.AddComponent<Image>();
            refreshImage.sprite = EditorSprites.ReloadSprite;

            prefabTypeReloadButton = refresh.AddComponent<Button>();
            prefabTypeReloadButton.image = refreshImage;
            EditorThemeManager.AddSelectable(prefabTypeReloadButton, ThemeGroup.Function_2, false);

            var scrollRect = Creator.NewUIObject("ScrollRect", gameObject.transform);
            scrollRect.transform.AsRT().anchoredPosition = new Vector2(0f, 0f);
            scrollRect.transform.AsRT().sizeDelta = new Vector2(400f, 600f);
            var scrollRectSR = scrollRect.AddComponent<ScrollRect>();
            scrollRectSR.scrollSensitivity = 20f;

            var mask = Creator.NewUIObject("Mask", scrollRect.transform);
            RectValues.FullAnchored.AssignToRectTransform(mask.transform.AsRT());

            var maskImage = mask.AddComponent<Image>();
            var maskMask = mask.AddComponent<Mask>();
            maskMask.showMaskGraphic = false;

            var content = Creator.NewUIObject("Content", mask.transform);
            RectValues.Default.AnchoredPosition(0f, -16f).AnchorMax(0f, 1f).AnchorMin(0f, 1f).Pivot(0f, 1f).SizeDelta(400f, 104f).AssignToRectTransform(content.transform.AsRT());

            var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var contentVLG = content.AddComponent<VerticalLayoutGroup>();
            contentVLG.childControlHeight = false;
            contentVLG.childForceExpandHeight = false;
            contentVLG.spacing = 4f;

            scrollRectSR.content = content.transform.AsRT();

            var scrollbar = EditorManager.inst.GetDialog("Parent Selector").Dialog.Find("Scrollbar").gameObject.Duplicate(scrollRect.transform, "Scrollbar");
            scrollbar.transform.AsRT().anchoredPosition = Vector2.zero;
            scrollbar.transform.AsRT().sizeDelta = new Vector2(32f, 600f);
            scrollRectSR.verticalScrollbar = scrollbar.GetComponent<Scrollbar>();

            EditorThemeManager.AddGraphic(gameObject.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Bottom_Left_I);
            EditorThemeManager.AddGraphic(maskImage, ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Bottom_Left_I);
            EditorThemeManager.AddGraphic(panelRT.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);
            EditorThemeManager.AddSelectable(closeButton, ThemeGroup.Close);
            EditorThemeManager.AddGraphic(closeButton.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);
            EditorThemeManager.AddLightText(title);

            EditorThemeManager.AddScrollbar(scrollRectSR.verticalScrollbar, scrollbarRoundedSide: SpriteHelper.RoundedSide.Bottom_Right_I);

            // Prefab Type Prefab
            prefabTypePrefab = Creator.NewUIObject("Prefab Type", transform);
            prefabTypePrefab.transform.AsRT().sizeDelta = new Vector2(400f, 32f);
            var image = prefabTypePrefab.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.1f);

            var horizontalLayoutGroup = prefabTypePrefab.AddComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.childControlWidth = false;
            horizontalLayoutGroup.childForceExpandWidth = false;
            horizontalLayoutGroup.spacing = 4;

            var toggleType = prefabTypeTogglePrefab.Duplicate(prefabTypePrefab.transform, "Toggle");
            toggleType.transform.localScale = Vector3.one;
            toggleType.transform.AsRT().sizeDelta = new Vector2(32f, 32f);
            Destroy(toggleType.transform.Find("text").gameObject);
            toggleType.transform.Find("Background/Checkmark").GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

            var toggleTog = toggleType.GetComponent<Toggle>();
            toggleTog.enabled = true;
            toggleTog.group = null;

            var icon = Creator.NewUIObject("Icon", toggleType.transform);
            icon.transform.AsRT().anchoredPosition = Vector2.zero;
            icon.transform.AsRT().sizeDelta = new Vector2(32f, 32f);

            var iconImage = icon.AddComponent<Image>();

            var nameGO = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(prefabTypePrefab.transform, "Name");
            nameGO.transform.localScale = Vector3.one;
            var nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.sizeDelta = new Vector2(132f, 32f);

            var nameTextRT = nameRT.Find("Text").AsRT();
            nameTextRT.anchoredPosition = new Vector2(0f, 0f);
            nameTextRT.sizeDelta = new Vector2(0f, 0f);

            nameTextRT.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

            var colorGO = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(prefabTypePrefab.transform, "Color");
            colorGO.transform.localScale = Vector3.one;
            var colorRT = colorGO.GetComponent<RectTransform>();
            colorRT.sizeDelta = new Vector2(90f, 32f);

            var colorTextRT = colorRT.Find("Text").AsRT();
            colorTextRT.anchoredPosition = new Vector2(0f, 0f);
            colorTextRT.sizeDelta = new Vector2(0f, 0f);

            colorTextRT.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

            var setIcon = EditorPrefabHolder.Instance.Function1Button.Duplicate(prefabTypePrefab.transform, "Set Icon");
            setIcon.transform.AsRT().sizeDelta = new Vector2(95f, 32f);

            Destroy(setIcon.GetComponent<LayoutElement>());

            var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(prefabTypePrefab.transform, "Delete");
            delete.transform.localScale = Vector3.one;
            delete.transform.AsRT().anchoredPosition = Vector2.zero;

            Destroy(delete.GetComponent<LayoutElement>());

            EditorHelper.AddEditorPopup(EditorPopup.PREFAB_TYPES_POPUP, gameObject);
            gameObject.SetActive(false);

            EditorHelper.AddEditorDropdown("View Prefab Types", "", "View", EditorSprites.SearchSprite, () =>
            {
                OpenPrefabTypePopup(NewPrefabTypeID, id =>
                {
                    PrefabEditor.inst.NewPrefabType = PrefabType.prefabTypeLSIDToIndex.TryGetValue(id, out int prefabTypeIndexLS) ? prefabTypeIndexLS : PrefabType.prefabTypeVGIDToIndex.TryGetValue(id, out int prefabTypeIndexVG) ? prefabTypeIndexVG : 0;
                    NewPrefabTypeID = id;
                    if (DataManager.inst.PrefabTypes.Select(x => x as PrefabType).ToList().TryFind(x => x.id == id, out PrefabType prefabType))
                    {
                        externalCreatorText.name = prefabType.Name + " [ Click to Open Prefab Type Editor ]";
                        externalCreatorImage.color = prefabType.Color;
                    }
                });
            });

            RTEditor.inst.PrefabTypesPopup = new ContentPopup(EditorPopup.PREFAB_TYPES_POPUP);
            RTEditor.inst.PrefabTypesPopup.GameObject = gameObject;
            RTEditor.inst.PrefabTypesPopup.Content = content.transform.AsRT();
        }

        /// <summary>
        /// Saves all custom prefab types to the prefab types folder.
        /// </summary>
        public void SavePrefabTypes()
        {
            var prefabTypesPath = RTFile.CombinePaths(RTFile.ApplicationDirectory, "beatmaps/prefabtypes");
            foreach (var prefabType in DataManager.inst.PrefabTypes.Select(x => x as PrefabType).Where(x => !x.isDefault))
            {
                var jn = prefabType.ToJSON();
                prefabType.filePath = RTFile.CombinePaths(prefabTypesPath, RTFile.FormatLegacyFileName(prefabType.Name) + FileFormat.LSPT.Dot());
                RTFile.WriteToFile(prefabType.filePath, jn.ToString(3));
            }
        }

        public static bool loadingPrefabTypes = false;

        /// <summary>
        /// Loads all custom prefab types from the prefab types folder.
        /// </summary>
        public IEnumerator LoadPrefabTypes()
        {
            loadingPrefabTypes = true;
            DataManager.inst.PrefabTypes.Clear();

            var defaultPrefabTypesJN = JSON.Parse(RTFile.ReadFromFile($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}default_prefabtypes{FileFormat.LSPT.Dot()}"));
            for (int i = 0; i < defaultPrefabTypesJN["prefab_types"].Count; i++)
                DataManager.inst.PrefabTypes.Add(PrefabType.Parse(defaultPrefabTypesJN["prefab_types"][i], true));

            var prefabTypesPath = RTFile.CombinePaths(RTFile.ApplicationDirectory, "beatmaps/prefabtypes");
            RTFile.CreateDirectory(prefabTypesPath);

            var files = Directory.GetFiles(prefabTypesPath, FileFormat.LSPT.ToPattern(), SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; i++)
            {
                var jn = JSON.Parse(RTFile.ReadFromFile(files[i]));
                var prefabType = PrefabType.Parse(jn);
                prefabType.filePath = RTFile.ReplaceSlash(files[i]);
                DataManager.inst.PrefabTypes.Add(prefabType);
            }

            NewPrefabTypeID = ((PrefabType)DataManager.inst.PrefabTypes[0]).id;

            loadingPrefabTypes = false;

            yield break;
        }

        /// <summary>
        /// Opens the prefab types popup.
        /// </summary>
        /// <param name="current">The currently selected type ID.</param>
        /// <param name="onSelect">Action to occur when selecting.</param>
        public void OpenPrefabTypePopup(string current, Action<string> onSelect)
        {
            RTEditor.inst.PrefabTypesPopup.Open();
            RenderPrefabTypesPopup(current, onSelect);
        }

        /// <summary>
        /// Renders the prefab types popup.
        /// </summary>
        /// <param name="current">The currently selected type ID.</param>
        /// <param name="onSelect">Action to occur when selecting.</param>
        public void RenderPrefabTypesPopup(string current, Action<string> onSelect) => StartCoroutine(IRenderPrefabTypesPopup(current, onSelect));

        IEnumerator IRenderPrefabTypesPopup(string current, Action<string> onSelect)
        {
            prefabTypeReloadButton.onClick.ClearAll();
            prefabTypeReloadButton.onClick.AddListener(() =>
            {
                StartCoroutine(LoadPrefabTypes());
                RenderPrefabTypesPopup(NewPrefabTypeID, onSelect);
            });

            RTEditor.inst.PrefabTypesPopup.ClearContent();

            var createPrefabType = PrefabEditor.inst.CreatePrefab.Duplicate(RTEditor.inst.PrefabTypesPopup.Content, "Create Prefab Type");
            createPrefabType.transform.AsRT().sizeDelta = new Vector2(402f, 32f);
            var createPrefabTypeText = createPrefabType.transform.Find("Text").GetComponent<Text>();
            createPrefabTypeText.text = "Create New Prefab Type";
            var createPrefabTypeButton = createPrefabType.GetComponent<Button>();
            createPrefabTypeButton.onClick.ClearAll();
            createPrefabTypeButton.onClick.AddListener(() =>
            {
                string name = "New Type";
                int n = 0;
                while (DataManager.inst.PrefabTypes.Has(x => x.Name == name))
                {
                    name = $"New Type [{n}]";
                    n++;
                }

                var prefabType = new PrefabType(name, LSColors.pink500);
                prefabType.icon = LegacyPlugin.AtanPlaceholder;

                DataManager.inst.PrefabTypes.Add(prefabType);

                SavePrefabTypes();

                RenderPrefabTypesPopup(current, onSelect);
            });

            EditorThemeManager.ApplyGraphic(createPrefabTypeButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(createPrefabTypeText, ThemeGroup.Add_Text);

            int num = 0;
            foreach (var prefabType in DataManager.inst.PrefabTypes.Select(x => x as PrefabType))
            {
                int index = num;
                var gameObject = prefabTypePrefab.Duplicate(RTEditor.inst.PrefabTypesPopup.Content, prefabType.Name);

                var toggle = gameObject.transform.Find("Toggle").GetComponent<Toggle>();
                toggle.onValueChanged.ClearAll();
                toggle.isOn = current == prefabType.id;
                toggle.onValueChanged.AddListener(_val =>
                {
                    onSelect?.Invoke(prefabType.id);
                    RenderPrefabTypesPopup(prefabType.id, onSelect);
                });

                toggle.image.color = prefabType.Color;

                EditorThemeManager.ApplyGraphic(toggle.image, ThemeGroup.Null, true);
                EditorThemeManager.ApplyGraphic(toggle.graphic, ThemeGroup.Background_1);

                var icon = gameObject.transform.Find("Toggle/Icon").GetComponent<Image>();
                icon.sprite = prefabType.icon;

                var inputField = gameObject.transform.Find("Name").GetComponent<InputField>();
                inputField.onValueChanged.ClearAll();
                inputField.onEndEdit.ClearAll();
                inputField.characterValidation = InputField.CharacterValidation.None;
                inputField.characterLimit = 0;
                inputField.text = prefabType.Name;
                inputField.interactable = !prefabType.isDefault;
                if (!prefabType.isDefault)
                {
                    inputField.onValueChanged.AddListener(_val =>
                    {
                        string oldName = DataManager.inst.PrefabTypes[index].Name;

                        string name = _val;
                        int n = 0;
                        while (DataManager.inst.PrefabTypes.Has(x => x.Name == name))
                        {
                            name = $"{_val}[{n}]";
                            n++;
                        }

                        DataManager.inst.PrefabTypes[index].Name = name;

                        if (!RTFile.FileExists(prefabType.filePath))
                            return;

                        File.Delete(prefabType.filePath);
                    });
                    inputField.onEndEdit.AddListener(_val =>
                    {
                        SavePrefabTypes();
                        RenderPrefabTypesPopup(current, onSelect);
                    });
                }

                EditorThemeManager.AddInputField(inputField);

                var color = gameObject.transform.Find("Color").GetComponent<InputField>();
                color.onValueChanged.ClearAll();
                color.onEndEdit.ClearAll();
                color.characterValidation = InputField.CharacterValidation.None;
                color.characterLimit = 0;
                color.text = CoreHelper.ColorToHex(prefabType.Color);
                color.interactable = !prefabType.isDefault;
                if (!prefabType.isDefault)
                {
                    color.onValueChanged.AddListener(prefabType.AssignColor);
                    color.onEndEdit.AddListener(_val =>
                    {
                        RenderPrefabTypesPopup(current, onSelect);
                        SavePrefabTypes();
                    });
                }

                EditorThemeManager.AddInputField(color);

                var deleteStorage = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                deleteStorage.button.onClick.ClearAll();
                deleteStorage.button.interactable = !prefabType.isDefault;
                if (!prefabType.isDefault)
                    deleteStorage.button.onClick.AddListener(() =>
                    {
                        if (RTFile.FileExists(prefabType.filePath))
                            File.Delete(prefabType.filePath);

                        DataManager.inst.PrefabTypes.RemoveAt(index);

                        RenderPrefabTypesPopup(current, onSelect);
                        SavePrefabTypes();
                    });

                EditorThemeManager.ApplyGraphic(deleteStorage.baseImage, ThemeGroup.Delete, true);
                EditorThemeManager.ApplyGraphic(deleteStorage.image, ThemeGroup.Delete_Text);

                var setImageStorage = gameObject.transform.Find("Set Icon").GetComponent<FunctionButtonStorage>();
                setImageStorage.button.onClick.ClearAll();
                setImageStorage.button.interactable = !prefabType.isDefault;

                if (!prefabType.isDefault)
                    setImageStorage.button.onClick.AddListener(() =>
                    {
                        RTEditor.inst.BrowserPopup.Open();
                        RTFileBrowser.inst.UpdateBrowserFile(new string[] { FileFormat.PNG.Dot() }, onSelectFile: _val =>
                        {
                            prefabType.icon = SpriteHelper.LoadSprite(_val);
                            icon.sprite = prefabType.icon;

                            SavePrefabTypes();
                            RTEditor.inst.BrowserPopup.Close();
                        });
                    });

                setImageStorage.text.text = "Set Icon";

                EditorThemeManager.ApplyGraphic(setImageStorage.button.image, ThemeGroup.Function_1, true);
                EditorThemeManager.ApplyGraphic(setImageStorage.text, ThemeGroup.Function_1_Text);

                num++;
            }

            yield break;
        }

        #endregion

        #region Prefabs

        public bool shouldCutPrefab;
        public string copiedPrefabPath;

        public Button externalType;
        public Image externalTypeImage;
        public Text externalTypeText;

        public Image externalCreatorImage;
        public Text externalCreatorText;

        public Button importPrefab;
        public Button exportToVG;

        public InputField externalNameField;
        public InputField externalDescriptionField;

        public bool prefabsLoading;

        GameObject prefabExternalUpAFolderButton;
        public GameObject prefabExternalAddButton;

        public bool filterUsed;

        public IEnumerator LoadPrefabs()
        {
            if (prefabsLoading)
                yield break;

            prefabsLoading = true;

            while (!PrefabEditor.inst || !PrefabEditor.inst.externalContent)
                yield return null;

            for (int i = PrefabPanels.Count - 1; i >= 0; i--)
            {
                var prefabPanel = PrefabPanels[i];
                if (prefabPanel.Dialog == PrefabDialog.External)
                {
                    Destroy(prefabPanel.GameObject);
                    PrefabPanels.RemoveAt(i);
                }
            }

            if (!prefabExternalAddButton)
            {
                CoreHelper.DeleteChildren(PrefabEditor.inst.externalContent);

                prefabExternalAddButton = CreatePrefabButton(PrefabEditor.inst.externalContent, "New External Prefab", eventData =>
                {
                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator($"{RTFile.ApplicationDirectory}{RTEditor.prefabListPath}", () => { RTEditor.inst.UpdatePrefabPath(true); RTEditor.inst.HideNameEditor(); })),
                            new ButtonFunction("Create prefab", () =>
                            {
                                PrefabEditor.inst.OpenDialog();
                                createInternal = false;
                            }),
                            new ButtonFunction("Paste Prefab", PastePrefab)
                            );

                        return;
                    }

                    if (savingToPrefab && prefabToSaveFrom != null)
                    {
                        savingToPrefab = false;
                        SavePrefab(prefabToSaveFrom);

                        RTEditor.inst.PrefabPopups.Close();

                        prefabToSaveFrom = null;

                        EditorManager.inst.DisplayNotification("Applied all changes to new External Prefab.", 2f, EditorManager.NotificationType.Success);

                        return;
                    }

                    PrefabEditor.inst.OpenDialog();
                    createInternal = false;
                });
            }
            else
            {
                var hover = prefabExternalAddButton.GetComponent<HoverUI>();
                hover.animateSca = true;
                hover.animatePos = false;
                hover.size = EditorConfig.Instance.PrefabButtonHoverSize.Value;
            }

            while (loadingPrefabTypes)
                yield return null;

            // Back
            if (!prefabExternalUpAFolderButton)
            {
                prefabExternalUpAFolderButton = EditorManager.inst.folderButtonPrefab.Duplicate(PrefabEditor.inst.externalContent, "back");
                var folderButtonStorageFolder = prefabExternalUpAFolderButton.GetComponent<FunctionButtonStorage>();
                var folderButtonFunctionFolder = prefabExternalUpAFolderButton.AddComponent<FolderButtonFunction>();

                var hoverUIFolder = prefabExternalUpAFolderButton.AddComponent<HoverUI>();
                hoverUIFolder.size = EditorConfig.Instance.PrefabButtonHoverSize.Value;
                hoverUIFolder.animatePos = false;
                hoverUIFolder.animateSca = true;

                folderButtonStorageFolder.text.text = "< Up a folder";

                folderButtonStorageFolder.button.onClick.ClearAll();
                folderButtonFunctionFolder.onClick = eventData =>
                {
                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator($"{RTFile.ApplicationDirectory}{RTEditor.prefabListPath}", () => { RTEditor.inst.UpdatePrefabPath(true); RTEditor.inst.HideNameEditor(); })),
                            new ButtonFunction("Paste Prefab", PastePrefab));

                        return;
                    }

                    if (RTEditor.inst.prefabPathField.text == RTEditor.PrefabPath)
                    {
                        RTEditor.inst.prefabPathField.text = RTFile.GetDirectory(RTFile.ApplicationDirectory + RTEditor.prefabListPath).Replace(RTFile.ApplicationDirectory + "beatmaps/", "");
                        RTEditor.inst.UpdatePrefabPath(false);
                    }
                };

                EditorThemeManager.ApplySelectable(folderButtonStorageFolder.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(folderButtonStorageFolder.text);
            }

            prefabExternalUpAFolderButton.SetActive(RTFile.GetDirectory(RTFile.ApplicationDirectory + RTEditor.prefabListPath) != RTFile.ApplicationDirectory + "beatmaps");

            var directories = Directory.GetDirectories(RTFile.ApplicationDirectory + RTEditor.prefabListPath, "*", SearchOption.TopDirectoryOnly);

            for (int i = 0; i < directories.Length; i++)
            {
                var directory = directories[i];
                var prefabPanel = new PrefabPanel(i);
                prefabPanel.Init(directory);
                PrefabPanels.Add(prefabPanel);
            }

            var files = Directory.GetFiles(RTFile.ApplicationDirectory + RTEditor.prefabListPath, FileFormat.LSP.ToPattern(), SearchOption.TopDirectoryOnly);

            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var jn = JSON.Parse(RTFile.ReadFromFile(file));

                var prefab = Prefab.Parse(jn);
                prefab.objects.ForEach(x => (x as BeatmapObject).RemovePrefabReference());
                prefab.filePath = RTFile.ReplaceSlash(file);

                var prefabPanel = new PrefabPanel(PrefabDialog.External, i);
                prefabPanel.Init(prefab);
                PrefabPanels.Add(prefabPanel);
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

        void CreatePrefabExternalDialog()
        {
            var editorDialog = EditorManager.inst.GetDialog("Multi Keyframe Editor (Object)").Dialog.gameObject.Duplicate(EditorManager.inst.dialogs, "PrefabExternalDialog");
            editorDialog.transform.AsRT().sizeDelta = new Vector2(0f, 32f);

            var editorDialogTitle = editorDialog.transform.GetChild(0);
            editorDialogTitle.GetComponent<Image>().color = LSColors.HexToColor("4C4C4C");
            var documentationTitle = editorDialogTitle.GetChild(0).GetComponent<Text>();
            documentationTitle.text = "- External Prefab View -";
            documentationTitle.color = new Color(0.9f, 0.9f, 0.9f, 1f);

            var editorDialogSpacer = editorDialog.transform.GetChild(1);
            editorDialogSpacer.AsRT().sizeDelta = new Vector2(765f, 54f);

            editorDialog.transform.GetChild(1).AsRT().sizeDelta = new Vector2(765f, 24f);

            var labelTypeBase = Creator.NewUIObject("Type Label", editorDialog.transform);

            labelTypeBase.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var labelType = editorDialog.transform.GetChild(2);
            labelType.SetParent(labelTypeBase.transform);
            labelType.localPosition = Vector3.zero;
            labelType.localScale = Vector3.one;
            labelType.AsRT().sizeDelta = new Vector2(725f, 32f);
            var labelTypeText = labelType.GetComponent<Text>();
            labelTypeText.text = "Type";
            labelTypeText.alignment = TextAnchor.UpperLeft;
            EditorThemeManager.AddLightText(labelTypeText);

            var prefabTypeBase = Creator.NewUIObject("Prefab Type Base", editorDialog.transform);
            prefabTypeBase.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var prefabEditorData = EditorManager.inst.GetDialog("Prefab Editor").Dialog.Find("data/type/Show Type Editor");

            var prefabType = prefabEditorData.gameObject.Duplicate(prefabTypeBase.transform, "Show Type Editor");

            UIManager.SetRectTransform(prefabType.transform.AsRT(), new Vector2(-370f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(742f, 34f));
            externalType = prefabType.GetComponent<Button>();
            externalTypeText = prefabType.transform.Find("Text").GetComponent<Text>();
            externalTypeImage = prefabType.GetComponent<Image>();

            EditorThemeManager.AddGraphic(externalTypeImage, ThemeGroup.Null, true);

            prefabType.gameObject.AddComponent<ContrastColors>().Init(externalTypeText, externalTypeImage);

            RTEditor.GenerateSpacer("spacer2", editorDialog.transform, new Vector2(765f, 24f));

            var labelNameBase = Creator.NewUIObject("Name Label", editorDialog.transform);
            labelNameBase.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var labelName = labelType.gameObject.Duplicate(labelNameBase.transform);
            labelName.transform.localPosition = Vector3.zero;
            labelName.transform.localScale = Vector3.one;
            labelName.transform.AsRT().sizeDelta = new Vector2(725f, 32f);
            var labelNameText = labelName.GetComponent<Text>();
            labelNameText.text = "Name";
            labelNameText.alignment = TextAnchor.UpperLeft;
            EditorThemeManager.AddLightText(labelNameText);

            var nameTextBase1 = Creator.NewUIObject("Text Base 1", editorDialog.transform);
            nameTextBase1.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var name = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(nameTextBase1.transform);
            name.transform.localScale = Vector3.one;
            UIManager.SetRectTransform(name.transform.AsRT(), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(740f, 32f));

            externalNameField = name.GetComponent<InputField>();
            externalNameField.lineType = InputField.LineType.MultiLineNewline;
            externalNameField.GetPlaceholderText().text = "Set name...";
            externalNameField.GetPlaceholderText().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);

            EditorThemeManager.AddInputField(externalNameField);

            RTEditor.GenerateSpacer("spacer3", editorDialog.transform, new Vector2(765f, 4f));

            var labelDescriptionBase = Creator.NewUIObject("Description Label", editorDialog.transform);
            labelDescriptionBase.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var labelDescription = labelType.gameObject.Duplicate(labelDescriptionBase.transform);
            labelDescription.transform.localPosition = Vector3.zero;
            labelDescription.transform.localScale = Vector3.one;
            labelDescription.transform.AsRT().sizeDelta = new Vector2(725f, 32f);
            var labelDescriptionText = labelDescription.GetComponent<Text>();
            labelDescriptionText.text = "Description";
            labelDescriptionText.alignment = TextAnchor.UpperLeft;
            EditorThemeManager.AddLightText(labelDescriptionText);

            var descriptionTextBase1 = Creator.NewUIObject("Text Base 1", editorDialog.transform);
            descriptionTextBase1.transform.AsRT().sizeDelta = new Vector2(765f, 300f);

            var description = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(descriptionTextBase1.transform);
            description.transform.localScale = Vector3.one;
            UIManager.SetRectTransform(description.transform.AsRT(), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(740f, 300f));

            externalDescriptionField = description.GetComponent<InputField>();
            externalDescriptionField.lineType = InputField.LineType.MultiLineNewline;
            externalDescriptionField.GetPlaceholderText().text = "Set description...";
            externalDescriptionField.GetPlaceholderText().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);

            EditorThemeManager.AddInputField(externalDescriptionField);

            RTEditor.GenerateSpacer("spacer4", editorDialog.transform, new Vector2(765f, 80f));

            var buttonsBase = new GameObject("buttons base");
            buttonsBase.transform.SetParent(editorDialog.transform);
            buttonsBase.transform.localScale = Vector3.one;

            var buttonsBaseRT = buttonsBase.AddComponent<RectTransform>();
            buttonsBaseRT.sizeDelta = new Vector2(765f, 0f);

            var buttons = new GameObject("buttons");
            buttons.transform.SetParent(buttonsBaseRT);
            buttons.transform.localScale = Vector3.one;

            var buttonsHLG = buttons.AddComponent<HorizontalLayoutGroup>();
            buttonsHLG.spacing = 60f;

            buttons.transform.AsRT().sizeDelta = new Vector2(600f, 32f);

            var importPrefab = EditorPrefabHolder.Instance.Function2Button.Duplicate(buttons.transform, "import");
            var importPrefabStorage = importPrefab.GetComponent<FunctionButtonStorage>();
            importPrefab.SetActive(true);
            this.importPrefab = importPrefabStorage.button;
            importPrefabStorage.text.text = "Import Prefab";

            var exportToVG = EditorPrefabHolder.Instance.Function2Button.Duplicate(buttons.transform, "export");
            var exportToVGStorage = exportToVG.GetComponent<FunctionButtonStorage>();
            exportToVG.SetActive(true);
            this.exportToVG = exportToVGStorage.button;
            exportToVGStorage.text.text = "Convert to VG Format";

            EditorHelper.AddEditorDialog(EditorDialog.PREFAB_EXTERNAL_EDITOR, editorDialog);

            EditorThemeManager.AddGraphic(editorDialog.GetComponent<Image>(), ThemeGroup.Background_1);
            EditorThemeManager.AddSelectable(importPrefabStorage.button, ThemeGroup.Function_2);
            EditorThemeManager.AddSelectable(exportToVGStorage.button, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(importPrefabStorage.text, ThemeGroup.Function_2_Text);
            EditorThemeManager.AddGraphic(exportToVGStorage.text, ThemeGroup.Function_2_Text);
        }

        /// <summary>
        /// Converts a prefab to the VG format and saves it to a file.
        /// </summary>
        /// <param name="prefab">Prefab to convert.</param>
        public void ConvertPrefab(Prefab prefab)
        {
            var exportPath = EditorConfig.Instance.ConvertPrefabLSToVGExportPath.Value;

            if (string.IsNullOrEmpty(exportPath))
            {
                exportPath = RTFile.CombinePaths(RTFile.ApplicationDirectory, RTEditor.DEFAULT_EXPORTS_PATH);
                RTFile.CreateDirectory(exportPath);
            }

            exportPath = RTFile.AppendEndSlash(exportPath);

            if (!RTFile.DirectoryExists(RTFile.RemoveEndSlash(exportPath)))
            {
                EditorManager.inst.DisplayNotification("Directory does not exist.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var vgjn = prefab.ToJSONVG();

            var fileName = $"{RTFile.FormatAlphaFileName(prefab.Name)}{FileFormat.VGP.Dot()}";
            RTFile.WriteToFile(RTFile.CombinePaths(exportPath, fileName), vgjn.ToString());

            EditorManager.inst.DisplayNotification($"Converted Prefab {prefab.Name} from LS format to VG format and saved to {fileName}!", 4f, EditorManager.NotificationType.Success);

            AchievementManager.inst.UnlockAchievement("time_machine");
        }

        /// <summary>
        /// Renders the External Prefab Editor.
        /// </summary>
        /// <param name="prefabPanel"></param>
        public void RenderPrefabExternalDialog(PrefabPanel prefabPanel)
        {
            var prefab = prefabPanel.Prefab;
            var prefabType = prefab.PrefabType;
            var isExternal = prefabPanel.Dialog == PrefabDialog.External;

            externalTypeText.text = prefabType.Name + " [ Click to Open Prefab Type Editor ]";
            externalTypeImage.color = prefabType.Color;
            externalType.onClick.ClearAll();
            externalType.onClick.AddListener(() =>
            {
                OpenPrefabTypePopup(prefab.typeID, id =>
                {
                    prefab.Type = PrefabType.prefabTypeLSIDToIndex.TryGetValue(id, out int prefabTypeIndexLS) ? prefabTypeIndexLS : PrefabType.prefabTypeVGIDToIndex.TryGetValue(id, out int prefabTypeIndexVG) ? prefabTypeIndexVG : 0;
                    prefab.typeID = id;

                    var prefabType = prefab.PrefabType;
                    externalTypeImage.color = prefabType.Color;
                    externalTypeText.text = prefabType.Name + " [ Click to Open Prefab Type Editor ]";

                    if (isExternal && !string.IsNullOrEmpty(prefab.filePath))
                        RTFile.WriteToFile(prefab.filePath, prefab.ToJSON().ToString());

                    prefabPanel.RenderPrefabType(prefabType);
                    prefabPanel.RenderTooltip(prefab, prefabType);
                });
            });

            importPrefab.gameObject.SetActive(isExternal);
            importPrefab.onClick.ClearAll();
            if (isExternal)
                importPrefab.onClick.AddListener(() => ImportPrefabIntoLevel(prefab));

            exportToVG.gameObject.SetActive(isExternal);
            exportToVG.onClick.ClearAll();
            if (isExternal)
                exportToVG.onClick.AddListener(() => ConvertPrefab(prefab));

            externalDescriptionField.onValueChanged.ClearAll();
            externalDescriptionField.onEndEdit.ClearAll();
            externalDescriptionField.text = prefab.description;
            externalDescriptionField.onValueChanged.AddListener(_val => prefab.description = _val);
            externalDescriptionField.onEndEdit.AddListener(_val =>
            {
                if (!isExternal)
                {
                    prefabPanel.RenderTooltip();
                    return;
                }

                RTEditor.inst.DisablePrefabWatcher();

                if (!string.IsNullOrEmpty(prefab.filePath))
                    RTFile.WriteToFile(prefab.filePath, prefab.ToJSON().ToString());

                prefabPanel.RenderTooltip();

                RTEditor.inst.EnablePrefabWatcher();
            });

            externalNameField.onValueChanged.ClearAll();
            externalNameField.onEndEdit.ClearAll();
            externalNameField.text = prefab.Name;
            externalNameField.onValueChanged.AddListener(_val => prefab.Name = _val);
            externalNameField.onEndEdit.AddListener(_val =>
            {
                if (!isExternal)
                {
                    prefabPanel.RenderName();
                    prefabPanel.RenderTooltip();
                    EditorTimeline.inst.timelineObjects.ForLoop(timelineObject =>
                    {
                        if (timelineObject.isBeatmapObject || timelineObject.GetData<PrefabObject>().prefabID != prefab.ID)
                            return;

                        timelineObject.RenderText(prefab.Name);
                    });

                    return;
                }

                RTEditor.inst.DisablePrefabWatcher();

                RTFile.DeleteFile(prefab.filePath);

                var file = RTFile.CombinePaths(RTFile.ApplicationDirectory, RTEditor.prefabListPath, $"{RTFile.FormatLegacyFileName(prefab.Name)}{FileFormat.LSP.Dot()}");
                prefab.filePath = file;
                RTFile.WriteToFile(file, prefab.ToJSON().ToString());

                prefabPanel.RenderName();
                prefabPanel.RenderTooltip();

                RTEditor.inst.EnablePrefabWatcher();
            });
        }

        /// <summary>
        /// Creates a new prefab and saves it.
        /// </summary>
        public void CreateNewPrefab()
        {
            if (EditorTimeline.inst.SelectedBeatmapObjects.Count <= 0)
            {
                EditorManager.inst.DisplayNotification("Can't save prefab without any objects in it!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            if (string.IsNullOrEmpty(PrefabEditor.inst.NewPrefabName))
            {
                EditorManager.inst.DisplayNotification("Can't save prefab without a name!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var prefab = new Prefab(
                PrefabEditor.inst.NewPrefabName,
                PrefabEditor.inst.NewPrefabType,
                PrefabEditor.inst.NewPrefabOffset,
                EditorTimeline.inst.SelectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()).ToList(),
                EditorTimeline.inst.SelectedPrefabObjects.Select(x => x.GetData<PrefabObject>()).ToList());

            prefab.description = NewPrefabDescription;
            prefab.typeID = NewPrefabTypeID;

            foreach (var beatmapObject in prefab.objects)
            {
                if (!string.IsNullOrEmpty(beatmapObject.text) && beatmapObject.shape == 6 && AssetManager.SpriteAssets.TryGetValue(beatmapObject.text, out Sprite sprite))
                    prefab.SpriteAssets[beatmapObject.text] = sprite;
            }

            if (createInternal)
            {
                EditorManager.inst.DisplayNotification($"Saving Internal Prefab [{prefab.Name}] to level...", 1.5f, EditorManager.NotificationType.Warning);
                ImportPrefabIntoLevel(prefab);
                EditorManager.inst.DisplayNotification($"Saved Internal Prefab [{prefab.Name}]!", 2f, EditorManager.NotificationType.Success);
            }
            else
                SavePrefab(prefab);

            PrefabCreator.Close();
            OpenPopup();
        }

        /// <summary>
        /// Saves a prefab to a file.
        /// </summary>
        /// <param name="prefab">Prefab to save.</param>
        public void SavePrefab(Prefab prefab)
        {
            RTEditor.inst.DisablePrefabWatcher();

            EditorManager.inst.DisplayNotification($"Saving External Prefab [{prefab.Name}]...", 1.5f, EditorManager.NotificationType.Warning);

            prefab.objects.ForEach(x => (x as BeatmapObject).RemovePrefabReference());
            int count = PrefabPanels.Count;
            var file = RTFile.CombinePaths(RTFile.ApplicationDirectory, RTEditor.prefabListPath, $"{RTFile.FormatLegacyFileName(prefab.Name)}{FileFormat.LSP.Dot()}");
            prefab.filePath = file;

            var prefabPanel = new PrefabPanel(PrefabDialog.External, count);
            prefabPanel.Init(prefab);
            PrefabPanels.Add(prefabPanel);

            RTFile.WriteToFile(file, prefab.ToJSON().ToString());
            EditorManager.inst.DisplayNotification($"Saved External Prefab [{prefab.Name}]!", 2f, EditorManager.NotificationType.Success);

            RTEditor.inst.EnablePrefabWatcher();
        }

        /// <summary>
        /// Deletes an external prefab.
        /// </summary>
        /// <param name="prefabPanel">Prefab panel to delete and destroy.</param>
        public void DeleteExternalPrefab(PrefabPanel prefabPanel)
        {
            RTEditor.inst.DisablePrefabWatcher();

            RTFile.DeleteFile(prefabPanel.FilePath);

            Destroy(prefabPanel.GameObject);
            PrefabPanels.RemoveAt(prefabPanel.index);

            int num = 0;
            foreach (var p in PrefabPanels)
            {
                p.index = num;
                num++;
            }

            RTEditor.inst.EnablePrefabWatcher();
        }

        /// <summary>
        /// Deletes and internal prefab.
        /// </summary>
        /// <param name="index">Index of the prefab to remove.</param>
        public void DeleteInternalPrefab(int index)
        {
            string id = GameData.Current.prefabs[index].ID;

            GameData.Current.prefabs.RemoveAt(index);

            GameData.Current.prefabObjects.FindAll(x => x.prefabID == id).ForEach(x =>
            {
                Updater.UpdatePrefab(x, false);

                var index = EditorTimeline.inst.timelineObjects.FindIndex(y => y.ID == x.ID);
                if (index >= 0)
                {
                    Destroy(EditorTimeline.inst.timelineObjects[index].GameObject);
                    EditorTimeline.inst.timelineObjects.RemoveAt(index);
                }
            });

            GameData.Current.prefabObjects.RemoveAll(x => x.prefabID == id);

            PrefabEditor.inst.ReloadInternalPrefabsInPopup();
        }

        /// <summary>
        /// Deletes and internal prefab.
        /// </summary>
        /// <param name="id">ID of the prefab to remove.</param>
        public void DeleteInternalPrefab(string id)
        {
            if (GameData.Current.prefabs.TryFindIndex(x => x.ID == id, out int index))
                GameData.Current.prefabs.RemoveAt(index);

            for (int i = 0; i < GameData.Current.prefabObjects.Count; i++)
            {
                var prefabObject = GameData.Current.prefabObjects[i];
                if (prefabObject.prefabID != id)
                    continue;

                Updater.UpdatePrefab(prefabObject, false);

                if (EditorTimeline.inst.timelineObjects.TryFindIndex(x => x.ID == prefabObject.ID, out int j))
                {
                    Destroy(EditorTimeline.inst.timelineObjects[j].GameObject);
                    EditorTimeline.inst.timelineObjects.RemoveAt(j);
                }
            }

            GameData.Current.prefabObjects.RemoveAll(x => x.prefabID == id);

            PrefabEditor.inst.ReloadInternalPrefabsInPopup();
        }

        /// <summary>
        /// Opens the Internal and External Prefab popups.
        /// </summary>
        public void OpenPopup()
        {
            foreach (var editorPopup in RTEditor.inst.editorPopups)
            {
                if (editorPopup.Name == EditorPopup.PREFAB_POPUP)
                {
                    if (editorPopup.IsOpen)
                        continue;

                    editorPopup.Open();

                    continue;
                }

                editorPopup.Close();
            }

            RenderPopup();
        }

        /// <summary>
        /// Renders the Internal and External Prefab popups.
        /// </summary>
        public void RenderPopup()
        {
            UpdateCurrentPrefab(PrefabEditor.inst.currentPrefab as Prefab);

            PrefabEditor.inst.internalPrefabDialog.gameObject.SetActive(true);
            PrefabEditor.inst.externalPrefabDialog.gameObject.SetActive(true);

            selectQuickPrefabButton.onClick.ClearAll();
            selectQuickPrefabButton.onClick.AddListener(() =>
            {
                selectQuickPrefabText.text = "<color=#669e37>Selecting</color>";
                PrefabEditor.inst.ReloadInternalPrefabsInPopup(true);
            });

            PrefabEditor.inst.externalSearch.onValueChanged.ClearAll();
            PrefabEditor.inst.externalSearch.onValueChanged.AddListener(_val =>
            {
                PrefabEditor.inst.externalSearchStr = _val;
                PrefabEditor.inst.ReloadExternalPrefabsInPopup();
            });

            PrefabEditor.inst.internalSearch.onValueChanged.ClearAll();
            PrefabEditor.inst.internalSearch.onValueChanged.AddListener(_val =>
            {
                PrefabEditor.inst.internalSearchStr = _val;
                PrefabEditor.inst.ReloadInternalPrefabsInPopup();
            });

            savingToPrefab = false;
            prefabToSaveFrom = null;

            PrefabEditor.inst.ReloadExternalPrefabsInPopup();
            PrefabEditor.inst.ReloadInternalPrefabsInPopup();
        }

        /// <summary>
        /// Refreshes the Prefab Creator selection list.
        /// </summary>
        public void ReloadSelectionContent()
        {
            LSHelpers.DeleteChildren(PrefabEditor.inst.gridContent);
            foreach (var timelineObject in EditorTimeline.inst.timelineObjects)
            {
                if (!timelineObject.isBeatmapObject)
                    continue;

                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                if (!RTString.SearchString(PrefabEditor.inst.gridSearch.text, beatmapObject.name))
                    continue;

                var selection = PrefabEditor.inst.selectionPrefab.Duplicate(PrefabEditor.inst.gridContent, "grid");
                var text = selection.transform.Find("text").GetComponent<Text>();
                text.text = beatmapObject.name;

                var selectionToggle = selection.GetComponent<Toggle>();

                selectionToggle.onValueChanged.ClearAll();
                selectionToggle.isOn = timelineObject.Selected;
                selectionToggle.onValueChanged.AddListener(_val => timelineObject.Selected = _val);
                EditorThemeManager.ApplyToggle(selectionToggle, text: text);
            }
        }

        /// <summary>
        /// Opens the Prefab Creator dialog.
        /// </summary>
        public void OpenDialog()
        {
            PrefabCreator.Open();

            var component = PrefabEditor.inst.dialog.Find("data/name/input").GetComponent<InputField>();
            component.onValueChanged.ClearAll();
            component.onValueChanged.AddListener(_val => PrefabEditor.inst.NewPrefabName = _val);

            var offsetSlider = PrefabEditor.inst.dialog.Find("data/offset/slider").GetComponent<Slider>();
            var offsetInput = PrefabEditor.inst.dialog.Find("data/offset/input").GetComponent<InputField>();

            bool setting = false;
            offsetSlider.onValueChanged.ClearAll();
            offsetSlider.onValueChanged.AddListener(_val =>
            {
                if (!setting)
                {
                    setting = true;
                    PrefabEditor.inst.NewPrefabOffset = Mathf.Round(_val * 100f) / 100f;
                    offsetInput.text = PrefabEditor.inst.NewPrefabOffset.ToString();
                }
                setting = false;
            });

            offsetInput.onValueChanged.ClearAll();
            offsetInput.characterLimit = 0;
            offsetInput.onValueChanged.AddListener(_val =>
            {
                if (!setting && float.TryParse(_val, out float num))
                {
                    setting = true;
                    PrefabEditor.inst.NewPrefabOffset = num;
                    offsetSlider.value = num;
                }
                setting = false;
            });

            var offsetInputContextMenu = offsetInput.gameObject.GetOrAddComponent<ContextClickable>();
            offsetInputContextMenu.onClick = null;
            offsetInputContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Set to Timeline Cursor", () =>
                    {
                        var distance = AudioManager.inst.CurrentAudioSource.time - EditorTimeline.inst.SelectedObjects.Min(x => x.Time) + PrefabEditor.inst.NewPrefabOffset;
                        PrefabEditor.inst.NewPrefabOffset -= distance;
                        offsetInput.text = PrefabEditor.inst.NewPrefabOffset.ToString();
                    }));
            };
            var offsetSliderContextMenu = offsetSlider.gameObject.GetOrAddComponent<ContextClickable>();
            offsetSliderContextMenu.onClick = null;
            offsetSliderContextMenu.onClick = offsetInputContextMenu.onClick;

            TriggerHelper.AddEventTriggers(offsetInput.gameObject, TriggerHelper.ScrollDelta(offsetInput));

            if (DataManager.inst.PrefabTypes.TryFind(x => x is PrefabType prefabType && prefabType.id == NewPrefabTypeID, out DataManager.PrefabType vanillaPrefabType) && vanillaPrefabType is PrefabType prefabType)
            {
                externalCreatorText.text = prefabType.Name + " [ Click to Open Prefab Type Editor ]";
                externalCreatorImage.color = prefabType.Color;
            }

            prefabCreatorTypeButton.onClick.ClearAll();
            prefabCreatorTypeButton.onClick.AddListener(() =>
            {
                OpenPrefabTypePopup(NewPrefabTypeID, id =>
                {
                    PrefabEditor.inst.NewPrefabType = PrefabType.prefabTypeLSIDToIndex.TryGetValue(id, out int prefabTypeIndexLS) ? prefabTypeIndexLS : PrefabType.prefabTypeVGIDToIndex.TryGetValue(id, out int prefabTypeIndexVG) ? prefabTypeIndexVG : 0;
                    NewPrefabTypeID = id;
                    if (DataManager.inst.PrefabTypes.Select(x => x as PrefabType).ToList().TryFind(x => x.id == id, out PrefabType prefabType))
                    {
                        externalCreatorText.text = prefabType.Name + " [ Click to Open Prefab Type Editor ]";
                        externalCreatorImage.color = prefabType.Color;
                    }
                });
            });

            var description = PrefabEditor.inst.dialog.Find("data/description/input").GetComponent<InputField>();
            description.onValueChanged.ClearAll();
            ((Text)description.placeholder).text = "Prefab Description";
            description.lineType = InputField.LineType.MultiLineNewline;
            description.characterLimit = 0;
            description.characterValidation = InputField.CharacterValidation.None;
            description.textComponent.alignment = TextAnchor.UpperLeft;
            description.text = NewPrefabDescription;
            description.onValueChanged.AddListener(_val => NewPrefabDescription = _val);

            ReloadSelectionContent();
        }

        /// <summary>
        /// Updates the currently selected quick prefab.
        /// </summary>
        /// <param name="prefab">Prefab to set. Can be null to clear the selection.</param>
        public void UpdateCurrentPrefab(Prefab prefab)
        {
            PrefabEditor.inst.currentPrefab = prefab;

            bool prefabExists = PrefabEditor.inst.currentPrefab != null;

            selectQuickPrefabText.text = (!prefabExists ? "-Select Prefab-" : "<color=#669e37>-Prefab-</color>") + "\n" + (!prefabExists ? "n/a" : PrefabEditor.inst.currentPrefab.Name);
        }

        /// <summary>
        /// Pastes the copied prefab.
        /// </summary>
        public void PastePrefab()
        {
            if (string.IsNullOrEmpty(copiedPrefabPath))
            {
                EditorManager.inst.DisplayNotification("No prefab has been copied yet!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            if (!RTFile.FileExists(copiedPrefabPath))
            {
                EditorManager.inst.DisplayNotification("Copied prefab no longer exists.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var copiedPrefabsFolder = RTFile.GetDirectory(copiedPrefabPath);
            CoreHelper.Log($"Copied Folder: {copiedPrefabsFolder}");

            var prefabsPath = RTFile.CombinePaths(RTFile.ApplicationDirectory, RTEditor.prefabListPath);
            if (copiedPrefabsFolder == prefabsPath)
            {
                EditorManager.inst.DisplayNotification("Source and destination are the same.", 2f, EditorManager.NotificationType.Warning);
                return;
            }

            var destination = copiedPrefabPath.Replace(copiedPrefabsFolder, prefabsPath);
            CoreHelper.Log($"Destination: {destination}");
            if (RTFile.FileExists(destination))
            {
                EditorManager.inst.DisplayNotification("File already exists.", 2f, EditorManager.NotificationType.Warning);
                return;
            }

            if (shouldCutPrefab)
            {
                if (RTFile.MoveFile(copiedPrefabPath, destination))
                    EditorManager.inst.DisplayNotification($"Succesfully moved {Path.GetFileName(destination)}!", 2f, EditorManager.NotificationType.Success);
            }
            else
            {
                if (RTFile.CopyFile(copiedPrefabPath, destination))
                    EditorManager.inst.DisplayNotification($"Succesfully pasted {Path.GetFileName(destination)}!", 2f, EditorManager.NotificationType.Success);
            }

            RTEditor.inst.UpdatePrefabPath(true);
        }

        /// <summary>
        /// Refreshes the Internal Prefabs UI.
        /// </summary>
        /// <param name="updateCurrentPrefab">If the current quick prefab should be set instead of importing.</param>
        public IEnumerator RefreshInternalPrefabs(bool updateCurrentPrefab = false)
        {
            var config = EditorConfig.Instance;

            // Here we add the Example prefab provided to you.
            if (!GameData.Current.prefabs.Exists(x => x.ID == LegacyPlugin.ExamplePrefab.ID) && config.PrefabExampleTemplate.Value)
                GameData.Current.prefabs.Add(Prefab.DeepCopy(LegacyPlugin.ExamplePrefab, false));

            yield return new WaitForSeconds(0.03f);

            var searchFieldContextMenu = RTEditor.inst.PrefabPopups.InternalPrefabs.SearchField.gameObject.GetOrAddComponent<ContextClickable>();
            searchFieldContextMenu.onClick = null;
            searchFieldContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction($"Filter: Used [{(filterUsed ? "On": "Off")}]", () =>
                    {
                        filterUsed = !filterUsed;
                        PrefabEditor.inst.ReloadInternalPrefabsInPopup();
                    })
                    );
            };

            RTEditor.inst.PrefabPopups.InternalPrefabs.ClearContent();
            CreatePrefabButton(RTEditor.inst.PrefabPopups.InternalPrefabs.Content, "New Internal Prefab", eventData =>
            {
                if (eventData.button == PointerEventData.InputButton.Right)
                {
                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Create prefab", () =>
                        {
                            PrefabEditor.inst.OpenDialog();
                            createInternal = true;
                        })
                        );

                    return;
                }

                if (RTEditor.inst.prefabPickerEnabled)
                    RTEditor.inst.prefabPickerEnabled = false;

                PrefabEditor.inst.OpenDialog();
                createInternal = true;
            });

            var prefabs = GameData.Current.prefabs;
            for (int i = 0; i < prefabs.Count; i++)
            {
                var prefab = prefabs[i];
                if (ContainsName(prefab, PrefabDialog.Internal) && (!filterUsed || GameData.Current.prefabObjects.Any(x => x.prefabID == prefab.ID)))
                    new PrefabPanel(PrefabDialog.Internal, i).Init(prefab, updateCurrentPrefab);
            }

            yield break;
        }

        /// <summary>
        /// Creates the "New * Prefab" button.
        /// </summary>
        /// <param name="parent">Parent to set.</param>
        /// <param name="name">Name to display.</param>
        /// <param name="action">Action to run when button is clicked.</param>
        /// <returns>Returns the created game object.</returns>
        public GameObject CreatePrefabButton(Transform parent, string name, Action<PointerEventData> action)
        {
            var gameObject = PrefabEditor.inst.CreatePrefab.Duplicate(parent, "add new prefab");
            var text = gameObject.GetComponentInChildren<Text>();
            text.text = name;

            var hoverSize = EditorConfig.Instance.PrefabButtonHoverSize.Value;

            var hover = gameObject.AddComponent<HoverUI>();
            hover.animateSca = true;
            hover.animatePos = false;
            hover.size = hoverSize;

            var createNewButton = gameObject.GetComponent<Button>();
            createNewButton.onClick.ClearAll();

            var contextClickable = gameObject.AddComponent<ContextClickable>();
            contextClickable.onClick = action;

            EditorThemeManager.ApplyGraphic(createNewButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(text, ThemeGroup.Add_Text);

            return gameObject;
        }

        /// <summary>
        /// Renders the External Prefabs UI.
        /// </summary>
        public IEnumerator RenderExternalPrefabs()
        {
            foreach (var prefabPanel in PrefabPanels.Where(x => x.Dialog == PrefabDialog.External))
            {
                prefabPanel.SetActive(
                    prefabPanel.isFolder ?
                        RTString.SearchString(PrefabEditor.inst.externalSearchStr, Path.GetFileName(prefabPanel.FilePath)) :
                        ContainsName(prefabPanel.Prefab, PrefabDialog.External));
            }

            yield break;
        }

        /// <summary>
        /// Checks if the prefab is being searched for.
        /// </summary>
        /// <param name="prefab">Prefab reference.</param>
        /// <param name="dialog">Prefabs' dialog.</param>
        /// <returns>Returns true if the prefab is being searched for, otherwise returns false.</returns>
        public bool ContainsName(Prefab prefab, PrefabDialog dialog) => RTString.SearchString(dialog == PrefabDialog.External ? PrefabEditor.inst.externalSearchStr : PrefabEditor.inst.internalSearchStr, prefab.Name, prefab.PrefabType.Name);

        /// <summary>
        /// Imports a prefab into the internal prefabs list.
        /// </summary>
        /// <param name="prefab">Prefab to import.</param>
        public void ImportPrefabIntoLevel(Prefab prefab)
        {
            Debug.Log($"{PrefabEditor.inst.className}Adding Prefab: [{prefab.Name}]");
            var tmpPrefab = Prefab.DeepCopy(prefab);
            int num = GameData.Current.prefabs.FindAll(x => Regex.Replace(x.Name, "( +\\[\\d+])", string.Empty) == tmpPrefab.Name).Count;
            if (num > 0)
                tmpPrefab.Name = $"{tmpPrefab.Name} [{num}]";

            GameData.Current.prefabs.Add(tmpPrefab);
            PrefabEditor.inst.ReloadInternalPrefabsInPopup();
        }

        #endregion
    }
}
