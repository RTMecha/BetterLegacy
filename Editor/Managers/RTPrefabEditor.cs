using BetterLegacy.Components;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Example;
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
        public Transform prefabTypeContent;

        public Button prefabTypeReloadButton;

        public string NewPrefabTypeID { get; set; }
        public string NewPrefabDescription { get; set; }

        public List<PrefabPanel> PrefabPanels { get; set; } = new List<PrefabPanel>();

        public static bool ImportPrefabsDirectly { get; set; }

        #endregion

        public static void Init() => PrefabEditor.inst?.gameObject?.AddComponent<RTPrefabEditor>();

        void Awake() => inst = this;

        void Start()
        {
            StartCoroutine(SetupUI());
        }

        IEnumerator SetupUI()
        {
            while (!PrefabEditor.inst || !EditorManager.inst || !EditorManager.inst.EditorDialogsDictionary.ContainsKey("Prefab Popup") || EditorPrefabHolder.Instance == null || !EditorPrefabHolder.Instance.Function1Button)
                yield return null;

            // A
            {
                loadingPrefabTypes = true;
                PrefabEditor.inst.StartCoroutine(RTEditor.inst.LoadPrefabs(PrefabEditor.inst));
                PrefabEditor.inst.OffsetLine = PrefabEditor.inst.OffsetLinePrefab.Duplicate(EditorManager.inst.timeline.transform, "offset line");
                PrefabEditor.inst.OffsetLine.transform.AsRT().pivot = Vector2.one;

                var prefabPopup = EditorManager.inst.GetDialog("Prefab Popup").Dialog;
                PrefabEditor.inst.dialog = EditorManager.inst.GetDialog("Prefab Editor").Dialog;
                PrefabEditor.inst.externalPrefabDialog = prefabPopup.Find("external prefabs");
                PrefabEditor.inst.internalPrefabDialog = prefabPopup.Find("internal prefabs");

                var contextClickable = PrefabEditor.inst.externalPrefabDialog.gameObject.AddComponent<ContextClickable>();
                contextClickable.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    RTEditor.inst.RefreshContextMenu(300f,
                        new RTEditor.ButtonFunction("Create folder", () =>
                        {
                            EditorManager.inst.ShowDialog("Folder Creator Popup");
                            RTEditor.inst.RefreshFolderCreator($"{RTFile.ApplicationDirectory}{RTEditor.prefabListPath}", () => RTEditor.inst.UpdatePrefabPath(true));
                        }),
                        new RTEditor.ButtonFunction("Paste", PastePrefab)
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

                var prefabName = RTEditor.inst.defaultIF.Duplicate(prefabSelectorRight, "name");
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

                var prefabType = RTEditor.inst.timelineBar.transform.Find("event").gameObject
                    .Duplicate(prefabEditorData.Find("type"), "Show Type Editor");

                Destroy(prefabEditorData.Find("type/types").gameObject);

                UIManager.SetRectTransform(prefabType.transform.AsRT(), new Vector2(-370f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(742f, 34f));
                var prefabTypeButton = prefabType.GetComponent<Button>();
                prefabTypeButton.onClick.ClearAll();
                prefabTypeButton.onClick.AddListener(() =>
                {
                    OpenPrefabTypePopup(NewPrefabTypeID, id =>
                    {
                        PrefabEditor.inst.NewPrefabType = PrefabType.prefabTypeLSIDToIndex.TryGetValue(id, out int prefabTypeIndexLS) ? prefabTypeIndexLS : PrefabType.prefabTypeVGIDToIndex.TryGetValue(id, out int prefabTypeIndexVG) ? prefabTypeIndexVG : 0;
                        NewPrefabTypeID = id;
                        if (DataManager.inst.PrefabTypes.Select(x => x as PrefabType).ToList().TryFind(x => x.id == id, out PrefabType prefabType))
                        {
                            externalCreatorText.name = prefabType.Name;
                            externalCreatorImage.color = prefabType.Color;
                        }
                    });
                });
                externalCreatorText = prefabType.transform.Find("Text").GetComponent<Text>();
                externalCreatorImage = prefabTypeButton.image;

                prefabType.AddComponent<ContrastColors>().Init(externalCreatorText, externalCreatorImage);
                EditorThemeManager.AddGraphic(prefabTypeButton.image, ThemeGroup.Null, true);

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
                search.onValueChanged.AddListener(_val => { ReloadSelectionContent(); });

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

        }

        #region Prefab Objects

        /// <summary>
        /// Finds the timeline object with the associated PrefabObject ID.
        /// </summary>
        /// <param name="beatmapObject"></param>
        /// <returns>Returns either the related TimelineObject or a new TimelineObject if one doesn't exist for whatever reason.</returns>
        public TimelineObject GetTimelineObject(PrefabObject prefabObject)
        {
            if (!prefabObject.timelineObject)
                prefabObject.timelineObject = new TimelineObject(prefabObject);

            return prefabObject.timelineObject;
        }

        public bool advancedParent;

        public IEnumerator UpdatePrefabObjectTimes(PrefabObject currentPrefab)
        {
            var prefabObjects = GameData.Current.prefabObjects.FindAll(x => x.prefabID == currentPrefab.prefabID);
            for (int i = 0; i < prefabObjects.Count; i++)
            {
                var prefabObject = prefabObjects[i];

                if (prefabObject.editorData.layer == EditorManager.inst.layer)
                    ObjectEditor.inst.RenderTimelineObjectPosition(GetTimelineObject(prefabObject));

                StartCoroutine(Updater.IUpdatePrefab(prefabObject, "Start Time"));
            }
            yield break;
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
            parentParent.onClick.AddListener(() => { EditorManager.inst.OpenParentPopup(); });

            parentClear.onClick.ClearAll();

            parentPicker.onClick.ClearAll();
            parentPicker.onClick.AddListener(() => { RTEditor.inst.parentPickerEnabled = true; });

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
                    RTEditor.inst.timelineObjects.TryFind(x => x.ID == parent, out TimelineObject timelineObject))
                    ObjectEditor.inst.SetCurrentObject(timelineObject);
                else if (parent == "CAMERA_PARENT")
                {
                    RTEditor.inst.SetLayer(RTEditor.LayerType.Events);
                    EventEditor.inst.SetCurrentEvent(0, CoreHelper.ClosestEventKeyframe(0));
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
                    StartCoroutine(UpdatePrefabObjectTimes(prefabObject));
                }
            });
            TriggerHelper.IncreaseDecreaseButtons(offsetTime, t: right.transform.Find("time"));
            TriggerHelper.AddEventTriggers(offsetTime.gameObject, TriggerHelper.ScrollDelta(offsetTime));

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
                ObjectEditor.inst.RenderTimelineObject(GetTimelineObject(prefabObject));
            });

            var collapse = parent.Find("collapse").GetComponent<Toggle>();
            collapse.onValueChanged.ClearAll();
            collapse.isOn = prefabObject.editorData.collapse;
            collapse.onValueChanged.AddListener(_val =>
            {
                prefabObject.editorData.collapse = _val;
                ObjectEditor.inst.RenderTimelineObject(GetTimelineObject(prefabObject));
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
                    ObjectEditor.inst.RenderTimelineObject(GetTimelineObject(prefabObject));
                }
                else
                    EditorManager.inst.DisplayNotification("Text is not correct format!", 1f, EditorManager.NotificationType.Error);
            });

            TriggerHelper.IncreaseDecreaseButtons(startTime, t: parent);
            TriggerHelper.AddEventTriggers(startTime.gameObject, TriggerHelper.ScrollDelta(startTime));

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

                layers.image.color = RTEditor.GetLayerColor(prefabObject.editorData.layer);
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

                        prefabObject.editorData.layer = RTEditor.GetLayer(a);
                        layers.image.color = RTEditor.GetLayerColor(RTEditor.GetLayer(a));
                        ObjectEditor.inst.RenderTimelineObject(GetTimelineObject(prefabObject));
                    }
                    else
                        EditorManager.inst.DisplayNotification("Text is not correct format!", 1f, EditorManager.NotificationType.Error);
                });

                TriggerHelper.IncreaseDecreaseButtons(layers);
                TriggerHelper.AddEventTriggers(layers.gameObject, TriggerHelper.ScrollDeltaInt(layers, min: 1, max: int.MaxValue));
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
                speed.text = Mathf.Clamp(prefabObject.speed, 0.1f, Updater.MaxFastSpeed).ToString();
                speed.onValueChanged.AddListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        num = Mathf.Clamp(num, 0.1f, Updater.MaxFastSpeed);
                        prefabObject.speed = num;
                        Updater.UpdatePrefab(prefabObject, "Speed");
                    }
                });

                TriggerHelper.IncreaseDecreaseButtons(speed, min: 0.1f, max: Updater.MaxFastSpeed);
                TriggerHelper.AddEventTriggers(speed.gameObject, TriggerHelper.ScrollDelta(speed, min: 0.1f, max: Updater.MaxFastSpeed));
            }

            //Global Settings
            {
                nameIF.onValueChanged.ClearAll();
                nameIF.text = prefab.Name;
                nameIF.onValueChanged.AddListener(_val =>
                {
                    prefab.Name = _val;
                    foreach (var prefabObject in GameData.Current.prefabObjects.FindAll(x => x.prefabID == prefab.ID))
                        ObjectEditor.inst.RenderTimelineObject(GetTimelineObject(prefabObject));
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
                        ObjectEditor.inst.RenderTimelineObjects();
                    });
                });

                var savePrefab = prefabSelectorRight.Find("save prefab").GetComponent<Button>();
                savePrefab.onClick.ClearAll();
                savePrefab.onClick.AddListener(() =>
                {
                    savingToPrefab = true;
                    prefabToSaveFrom = prefab;

                    EditorManager.inst.ShowDialog("Prefab Popup");
                    var dialog = EditorManager.inst.GetDialog("Prefab Popup").Dialog;
                    dialog.GetChild(0).gameObject.SetActive(false);

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
            if (!ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
            {
                EditorManager.inst.DisplayNotification("Can't collapse non-object.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var bm = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();

            if (!bm || bm.prefabInstanceID == "")
            {
                EditorManager.inst.DisplayNotification("Beatmap Object does not have a Prefab Object reference.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var editorData = bm.editorData;
            string prefabInstanceID = bm.prefabInstanceID;
            float startTime = GameData.Current.beatmapObjects.Where(x => x.prefabInstanceID == prefabInstanceID).Min(x => x.StartTime);

            var prefab = GameData.Current.prefabs.Find(x => x.ID == bm.prefabID);

            var prefabObject = new PrefabObject(prefab.ID, startTime - prefab.Offset);
            prefabObject.editorData.Bin = editorData.Bin;
            prefabObject.editorData.layer = editorData.layer;
            var prefab2 = new Prefab(prefab.Name, prefab.Type, prefab.Offset, GameData.Current.beatmapObjects.FindAll(x => x.prefabInstanceID == prefabInstanceID), new List<PrefabObject>());

            prefab2.ID = prefab.ID;
            prefab2.typeID = prefab.typeID;

            int index = GameData.Current.prefabs.FindIndex(x => x.ID == bm.prefabID);
            GameData.Current.prefabs[index] = prefab2;
            var list = RTEditor.inst.TimelineBeatmapObjects.FindAll(x => x.GetData<BeatmapObject>().prefabInstanceID == prefabInstanceID);
            foreach (var timelineObject in list)
            {
                Destroy(timelineObject.GameObject);
                var a = RTEditor.inst.timelineObjects.FindIndex(x => x.ID == timelineObject.ID);
                if (a >= 0)
                    RTEditor.inst.timelineObjects.RemoveAt(a);
            }

            GameData.Current.beatmapObjects.Where(x => x.prefabInstanceID == prefabInstanceID && !x.fromPrefab).ToList().ForEach(x => Updater.UpdateObject(x, reinsert: false));
            GameData.Current.beatmapObjects.RemoveAll(x => x.prefabInstanceID == prefabInstanceID && !x.fromPrefab);
            GameData.Current.prefabObjects.Add(prefabObject);

            StartCoroutine(Updater.IAddPrefabToLevel(prefabObject));

            GameData.Current.prefabObjects.FindAll(x => x.prefabID == prefab.ID).ForEach(x => Updater.UpdatePrefab(x));

            ObjectEditor.inst.SetCurrentObject(GetTimelineObject(prefabObject));

            EditorManager.inst.DisplayNotification("Replaced all instances of Prefab!", 2f, EditorManager.NotificationType.Success);
        }

        public void ExpandCurrentPrefab()
        {
            if (!ObjectEditor.inst.CurrentSelection.IsPrefabObject)
            {
                EditorManager.inst.DisplayNotification("Can't expand non-prefab!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var prefabObject = ObjectEditor.inst.CurrentSelection.GetData<PrefabObject>();
            string id = prefabObject.ID;

            EditorManager.inst.ClearDialogs();

            Debug.Log($"{PrefabEditor.inst.className}Removing Prefab Object's spawned objects.");
            Updater.UpdatePrefab(prefabObject, false);

            RTEditor.inst.RemoveTimelineObject(RTEditor.inst.timelineObjects.Find(x => x.ID == id));

            GameData.Current.prefabObjects.RemoveAll(x => x.ID == id);
            GameData.Current.beatmapObjects.RemoveAll(x => x.prefabInstanceID == id && x.fromPrefab);
            ObjectEditor.inst.DeselectAllObjects();

            Debug.Log($"{PrefabEditor.inst.className}Expanding Prefab Object.");
            StartCoroutine(AddExpandedPrefabToLevel(prefabObject));

            ObjectEditor.inst.RenderTimelineObjects();

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

            if (RTEditor.inst.layerType == RTEditor.LayerType.Events)
                RTEditor.inst.SetLayer(RTEditor.LayerType.Objects);

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

            ObjectEditor.inst.SetCurrentObject(GetTimelineObject(prefabObject));

            if (prefab.Name.Contains("Example") && ExampleManager.inst && ExampleManager.inst.Visible)
            {
                ExampleManager.inst.Say("Hey, it's me!");
            }
        }

        public IEnumerator AddExpandedPrefabToLevel(PrefabObject prefabObject)
        {
            var updateExpandedObjectsYieldType = EditorConfig.Instance.UpdateExpandedObjectsYieldMode.Value;
            var expandObjectsYieldType = EditorConfig.Instance.ExpandObjectsYieldMode.Value;

            RTEditor.inst.ienumRunning = true;
            float delay = 0f;
            float audioTime = EditorManager.inst.CurrentAudioPos;

            var prefab = prefabObject.Prefab;

            var ids = prefab.objects.ToDictionary(x => x.id, x => LSText.randomString(16));

            EditorManager.inst.ClearDialogs();

            var sw = CoreHelper.StartNewStopwatch();

            var expandedObjects = new List<BeatmapObject>();
            for (int i = 0; i < prefab.objects.Count; i++)
            {
                var beatmapObject = prefab.objects[i];
                if (i > 0 && expandObjectsYieldType != YieldType.None)
                    yield return CoreHelper.GetYieldInstruction(expandObjectsYieldType, ref delay);

                var beatmapObjectCopy = BeatmapObject.DeepCopy((BeatmapObject)beatmapObject, false);
                if (ids.TryGetValue(beatmapObject.id, out string id))
                    beatmapObjectCopy.id = id;
                if (ids.TryGetValue(beatmapObject.parent, out string parentID))
                    beatmapObjectCopy.parent = parentID;
                else if (GameData.Current.beatmapObjects.FindIndex(x => x.id == beatmapObject.parent) == -1 && beatmapObjectCopy.parent != "CAMERA_PARENT")
                    beatmapObjectCopy.parent = "";

                beatmapObjectCopy.active = false;
                beatmapObjectCopy.fromPrefab = false;
                beatmapObjectCopy.prefabID = prefab.ID;
                beatmapObjectCopy.StartTime += prefabObject.StartTime + prefab.Offset;

                beatmapObjectCopy.editorData.layer = prefabObject.editorData.layer;
                beatmapObjectCopy.editorData.Bin = Mathf.Clamp(beatmapObjectCopy.editorData.Bin, 0, 14);

                if (!AssetManager.SpriteAssets.ContainsKey(beatmapObject.text) && prefab.SpriteAssets.TryGetValue(beatmapObject.text, out Sprite sprite))
                    AssetManager.SpriteAssets.Add(beatmapObject.text, sprite);

                beatmapObjectCopy.prefabInstanceID = prefabObject.ID;
                GameData.Current.beatmapObjects.Add(beatmapObjectCopy);
                if (Updater.levelProcessor && Updater.levelProcessor.converter != null && !Updater.levelProcessor.converter.beatmapObjects.ContainsKey(beatmapObjectCopy.id))
                    Updater.levelProcessor.converter.beatmapObjects.Add(beatmapObjectCopy.id, beatmapObjectCopy);

                expandedObjects.Add(beatmapObjectCopy);

                var timelineObject = new TimelineObject(beatmapObjectCopy);
                timelineObject.selected = true;
                ObjectEditor.inst.CurrentSelection = timelineObject;

                ObjectEditor.inst.RenderTimelineObject(timelineObject);
            }

            delay = 0f;
            for (int i = 0; i < expandedObjects.Count; i++)
            {
                if (i > 0 && updateExpandedObjectsYieldType != YieldType.None)
                    yield return CoreHelper.GetYieldInstruction(updateExpandedObjectsYieldType, ref delay);
                Updater.UpdateObject(expandedObjects[i]);
            }

            expandedObjects.Clear();
            expandedObjects = null;

            CoreHelper.StopAndLogStopwatch(sw);

            if (prefab.objects.Count > 1 || prefab.prefabObjects.Count > 1)
                EditorManager.inst.ShowDialog("Multi Object Editor", false);
            else if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
                ObjectEditor.inst.OpenDialog(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
            else if (ObjectEditor.inst.CurrentSelection.IsPrefabObject)
                PrefabEditor.inst.OpenPrefabDialog();

            EditorManager.inst.DisplayNotification($"Expanded Prefab Object {prefab.Name} in {sw.Elapsed}!.", 5f, EditorManager.NotificationType.Success, false);
            RTEditor.inst.ienumRunning = false;
            sw = null;
            yield break;
        }

        #endregion

        #region Prefab Types

        public void CreatePrefabTypesPopup()
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
            closeButton.onClick.AddListener(() => { EditorManager.inst.HideDialog("Prefab Types Popup"); });

            var refresh = Creator.NewUIObject("Refresh", panel.transform);
            UIManager.SetRectTransform(refresh.transform.AsRT(), new Vector2(-52f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(32f, 32f));

            var refreshImage = refresh.AddComponent<Image>();
            refreshImage.sprite = RTEditor.ReloadSprite;

            prefabTypeReloadButton = refresh.AddComponent<Button>();
            prefabTypeReloadButton.image = refreshImage;
            EditorThemeManager.AddSelectable(prefabTypeReloadButton, ThemeGroup.Function_2, false);

            var scrollRect = new GameObject("ScrollRect");
            scrollRect.transform.SetParent(gameObject.transform);
            scrollRect.transform.localScale = Vector3.one;
            var scrollRectRT = scrollRect.AddComponent<RectTransform>();
            scrollRectRT.anchoredPosition = new Vector2(0f, 0f);
            scrollRectRT.sizeDelta = new Vector2(400f, 600f);
            var scrollRectSR = scrollRect.AddComponent<ScrollRect>();
            scrollRectSR.scrollSensitivity = 20f;

            var mask = new GameObject("Mask");
            mask.transform.SetParent(scrollRectRT);
            mask.transform.localScale = Vector3.one;
            var maskRT = mask.AddComponent<RectTransform>();
            maskRT.anchoredPosition = new Vector2(0f, 0f);
            maskRT.anchorMax = new Vector2(1f, 1f);
            maskRT.anchorMin = new Vector2(0f, 0f);
            maskRT.sizeDelta = new Vector2(0f, 0f);

            var maskImage = mask.AddComponent<Image>();
            var maskMask = mask.AddComponent<Mask>();
            maskMask.showMaskGraphic = false;

            var content = new GameObject("Content");
            content.transform.SetParent(maskRT);
            content.transform.localScale = Vector3.one;

            var contentRT = content.AddComponent<RectTransform>();
            contentRT.anchoredPosition = new Vector2(0f, -16f);
            contentRT.anchorMax = new Vector2(0f, 1f);
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.pivot = new Vector2(0f, 1f);
            contentRT.sizeDelta = new Vector2(400f, 104f);

            prefabTypeContent = contentRT;

            var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var contentVLG = content.AddComponent<VerticalLayoutGroup>();
            contentVLG.childControlHeight = false;
            contentVLG.childForceExpandHeight = false;
            contentVLG.spacing = 4f;

            scrollRectSR.content = contentRT;

            var scrollbar = EditorManager.inst.GetDialog("Parent Selector").Dialog.Find("Scrollbar").gameObject.Duplicate(scrollRectRT, "Scrollbar");
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
            prefabTypePrefab = new GameObject("Prefab Type");
            prefabTypePrefab.transform.localScale = Vector3.one;
            var rectTransform = prefabTypePrefab.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(400f, 32f);
            var image = prefabTypePrefab.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.1f);

            var horizontalLayoutGroup = prefabTypePrefab.AddComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.childControlWidth = false;
            horizontalLayoutGroup.childForceExpandWidth = false;
            horizontalLayoutGroup.spacing = 4;

            var toggleType = prefabTypeTogglePrefab.Duplicate(rectTransform, "Toggle");
            toggleType.transform.localScale = Vector3.one;
            var toggleTypeRT = (RectTransform)toggleType.transform;
            toggleTypeRT.sizeDelta = new Vector2(32f, 32f);
            Destroy(toggleTypeRT.Find("text").gameObject);
            toggleTypeRT.Find("Background/Checkmark").GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

            var toggleTog = toggleType.GetComponent<Toggle>();
            toggleTog.enabled = true;
            toggleTog.group = null;

            var icon = new GameObject("Icon");
            icon.transform.localScale = Vector3.one;
            icon.transform.SetParent(toggleTypeRT);
            icon.transform.localScale = Vector3.one;
            var iconRT = icon.AddComponent<RectTransform>();
            iconRT.anchoredPosition = Vector2.zero;
            iconRT.sizeDelta = new Vector2(32f, 32f);

            var iconImage = icon.AddComponent<Image>();

            var nameGO = RTEditor.inst.defaultIF.Duplicate(rectTransform, "Name");
            nameGO.transform.localScale = Vector3.one;
            var nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.sizeDelta = new Vector2(132f, 32f);

            var nameTextRT = (RectTransform)nameRT.Find("Text");
            nameTextRT.anchoredPosition = new Vector2(0f, 0f);
            nameTextRT.sizeDelta = new Vector2(0f, 0f);

            nameTextRT.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

            var colorGO = RTEditor.inst.defaultIF.Duplicate(rectTransform, "Color");
            colorGO.transform.localScale = Vector3.one;
            var colorRT = colorGO.GetComponent<RectTransform>();
            colorRT.sizeDelta = new Vector2(90f, 32f);

            var colorTextRT = (RectTransform)colorRT.Find("Text");
            colorTextRT.anchoredPosition = new Vector2(0f, 0f);
            colorTextRT.sizeDelta = new Vector2(0f, 0f);

            colorTextRT.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

            var setIcon = EditorPrefabHolder.Instance.Function1Button.Duplicate(rectTransform, "Set Icon");
            ((RectTransform)setIcon.transform).sizeDelta = new Vector2(95f, 32f);

            Destroy(setIcon.GetComponent<LayoutElement>());

            var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(rectTransform, "Delete");
            delete.transform.localScale = Vector3.one;
            ((RectTransform)delete.transform).anchoredPosition = Vector2.zero;

            Destroy(delete.GetComponent<LayoutElement>());

            EditorHelper.AddEditorPopup("Prefab Types Popup", gameObject);
            gameObject.SetActive(false);

            EditorHelper.AddEditorDropdown("View Prefab Types", "", "View", RTEditor.inst.SearchSprite, () =>
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
        }

        public void SavePrefabTypes()
        {
            foreach (var prefabType in DataManager.inst.PrefabTypes.Select(x => x as PrefabType).Where(x => !x.isDefault))
            {
                var jn = prefabType.ToJSON();
                prefabType.filePath = RTFile.ApplicationDirectory + "beatmaps/prefabtypes/" + prefabType.Name.ToLower().Replace(" ", "_") + ".lspt";
                RTFile.WriteToFile(prefabType.filePath, jn.ToString(3));
            }
        }

        public static bool loadingPrefabTypes = false;
        public IEnumerator LoadPrefabTypes()
        {
            loadingPrefabTypes = true;
            DataManager.inst.PrefabTypes.Clear();

            var defaultPrefabTypesJN = JSON.Parse(RTFile.ReadFromFile($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}default_prefabtypes.lspt"));
            for (int i = 0; i < defaultPrefabTypesJN["prefab_types"].Count; i++)
                DataManager.inst.PrefabTypes.Add(PrefabType.Parse(defaultPrefabTypesJN["prefab_types"][i], true));

            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/prefabtypes"))
                Directory.CreateDirectory(RTFile.ApplicationDirectory + "beatmaps/prefabtypes");

            var files = Directory.GetFiles(RTFile.ApplicationDirectory + "beatmaps/prefabtypes", "*.lspt", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; i++)
            {
                var jn = JSON.Parse(RTFile.ReadFromFile(files[i]));
                var prefabType = PrefabType.Parse(jn);
                prefabType.filePath = files[i].Replace("\\", "/");
                DataManager.inst.PrefabTypes.Add(prefabType);
            }

            NewPrefabTypeID = ((PrefabType)DataManager.inst.PrefabTypes[0]).id;

            loadingPrefabTypes = false;

            yield break;
        }

        public void OpenPrefabTypePopup(string current, Action<string> onSelect)
        {
            EditorManager.inst.ShowDialog("Prefab Types Popup");
            RenderPrefabTypesPopup(current, onSelect);
        }

        public void RenderPrefabTypesPopup(string current, Action<string> onSelect) => StartCoroutine(IRenderPrefabTypesPopup(current, onSelect));

        IEnumerator IRenderPrefabTypesPopup(string current, Action<string> onSelect)
        {
            prefabTypeReloadButton.onClick.ClearAll();
            prefabTypeReloadButton.onClick.AddListener(() =>
            {
                StartCoroutine(LoadPrefabTypes());
                RenderPrefabTypesPopup(NewPrefabTypeID, onSelect);
            });

            LSHelpers.DeleteChildren(prefabTypeContent);

            var createPrefabType = PrefabEditor.inst.CreatePrefab.Duplicate(prefabTypeContent, "Create Prefab Type");
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
                var gameObject = prefabTypePrefab.Duplicate(prefabTypeContent, prefabType.Name);

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
                        EditorManager.inst.ShowDialog("Browser Popup");
                        RTFileBrowser.inst.UpdateBrowser(Directory.GetCurrentDirectory(), new string[] { ".png" }, onSelectFile: _val =>
                        {
                            prefabType.icon = SpriteHelper.LoadSprite(_val);
                            icon.sprite = prefabType.icon;

                            SavePrefabTypes();
                            EditorManager.inst.HideDialog("Browser Popup");
                        });
                    });

                setImageStorage.text.text = "Set Icon";

                EditorThemeManager.ApplyGraphic(setImageStorage.button.image, ThemeGroup.Function_1, true);
                EditorThemeManager.ApplyGraphic(setImageStorage.text, ThemeGroup.Function_1_Text);

                num++;
            }

            yield break;
        }

        //public void SetPrefabTypeIcon(Image icon, PrefabType prefabType)
        //{
        //    RTEditor.inst.ShowWarningPopup("Which file browser do you want to use?", () =>
        //    {
        //        var _val = Crosstales.FB.FileBrowser.OpenSingleFile("Select an icon!", RTFile.ApplicationDirectory, "png", "jpg");
        //        prefabType.icon = SpriteManager.LoadSprite(_val);
        //        icon.sprite = prefabType.icon;
        //    }, () =>
        //    {
        //        EditorManager.inst.ShowDialog("Browser Popup");
        //        RTFileBrowser.inst.UpdateBrowser(Directory.GetCurrentDirectory(), new string[] { ".png" }, onSelectFile: _val =>
        //        {
        //            prefabType.icon = SpriteManager.LoadSprite(_val);
        //            icon.sprite = prefabType.icon;

        //            EditorManager.inst.HideDialog("Browser Popup");
        //        });
        //    }, "Local Browser", "In-game Browser");
        //}

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

        public void CreatePrefabExternalDialog()
        {
            var editorDialog = EditorManager.inst.GetDialog("Multi Keyframe Editor (Object)").Dialog.gameObject.Duplicate(EditorManager.inst.dialogs, "PrefabExternalDialog");
            //editorDialog.transform.localScale = Vector3.one;
            //editorDialog.transform.position = new Vector3(1537.5f, 714.945f, 0f) * EditorManager.inst.ScreenScale;
            editorDialog.transform.AsRT().sizeDelta = new Vector2(0f, 32f);

            var editorDialogTitle = editorDialog.transform.GetChild(0);
            editorDialogTitle.GetComponent<Image>().color = LSColors.HexToColor("4C4C4C");
            var documentationTitle = editorDialogTitle.GetChild(0).GetComponent<Text>();
            documentationTitle.text = "- External Prefab View -";
            documentationTitle.color = new Color(0.9f, 0.9f, 0.9f, 1f);

            var editorDialogSpacer = editorDialog.transform.GetChild(1);
            editorDialogSpacer.AsRT().sizeDelta = new Vector2(765f, 54f);

            editorDialog.transform.GetChild(1).AsRT().sizeDelta = new Vector2(765f, 24f);

            var labelTypeBase = new GameObject("Type Label");
            labelTypeBase.transform.SetParent(editorDialog.transform);
            labelTypeBase.transform.localScale = Vector3.one;

            var labelTypeBaseRT = labelTypeBase.AddComponent<RectTransform>();
            labelTypeBaseRT.sizeDelta = new Vector2(765f, 32f);

            var labelType = editorDialog.transform.GetChild(2);
            labelType.SetParent(labelTypeBaseRT);
            labelType.localPosition = Vector3.zero;
            labelType.localScale = Vector3.one;
            labelType.AsRT().sizeDelta = new Vector2(725f, 32f);
            var labelTypeText = labelType.GetComponent<Text>();
            labelTypeText.text = "Type";
            labelTypeText.alignment = TextAnchor.UpperLeft;
            EditorThemeManager.AddLightText(labelTypeText);

            var prefabTypeBase = new GameObject("Prefab Type Base");
            prefabTypeBase.transform.SetParent(editorDialog.transform);
            prefabTypeBase.transform.localScale = Vector3.one;

            var prefabTypeBaseRT = prefabTypeBase.AddComponent<RectTransform>();
            prefabTypeBaseRT.sizeDelta = new Vector2(765f, 32f);

            var prefabEditorData = EditorManager.inst.GetDialog("Prefab Editor").Dialog.Find("data/type/Show Type Editor");

            var prefabType = prefabEditorData.gameObject.Duplicate(prefabTypeBaseRT, "Show Type Editor");

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

            var name = RTEditor.inst.defaultIF.Duplicate(nameTextBase1.transform);
            name.transform.localScale = Vector3.one;
            UIManager.SetRectTransform(name.transform.AsRT(), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(740f, 32f));

            externalNameField = name.GetComponent<InputField>();
            externalNameField.lineType = InputField.LineType.MultiLineNewline;
            externalNameField.PlaceholderText().text = "Set name...";
            externalNameField.PlaceholderText().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);

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

            var description = RTEditor.inst.defaultIF.Duplicate(descriptionTextBase1.transform);
            description.transform.localScale = Vector3.one;
            UIManager.SetRectTransform(description.transform.AsRT(), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(740f, 300f));

            externalDescriptionField = description.GetComponent<InputField>();
            externalDescriptionField.lineType = InputField.LineType.MultiLineNewline;
            externalDescriptionField.PlaceholderText().text = "Set description...";
            externalDescriptionField.PlaceholderText().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);

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

            EditorHelper.AddEditorDialog("Prefab External Dialog", editorDialog);

            EditorThemeManager.AddGraphic(editorDialog.GetComponent<Image>(), ThemeGroup.Background_1);
            EditorThemeManager.AddSelectable(importPrefabStorage.button, ThemeGroup.Function_2);
            EditorThemeManager.AddSelectable(exportToVGStorage.button, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(importPrefabStorage.text, ThemeGroup.Function_2_Text);
            EditorThemeManager.AddGraphic(exportToVGStorage.text, ThemeGroup.Function_2_Text);
        }

        public void RenderPrefabExternalDialog(PrefabPanel prefabPanel)
        {
            var prefab = prefabPanel.Prefab;

            externalTypeText.text = prefab.PrefabType.Name + " [ Click to Open Prefab Type Editor ]";
            externalTypeImage.color = prefab.PrefabType.Color;
            externalType.onClick.ClearAll();
            externalType.onClick.AddListener(() =>
            {
                OpenPrefabTypePopup(prefab.typeID, id =>
                {
                    prefab.Type = PrefabType.prefabTypeLSIDToIndex.TryGetValue(id, out int prefabTypeIndexLS) ? prefabTypeIndexLS : PrefabType.prefabTypeVGIDToIndex.TryGetValue(id, out int prefabTypeIndexVG) ? prefabTypeIndexVG : 0;
                    prefab.typeID = id;
                    var prefabType = prefab.PrefabType;
                    var color = prefabType.Color;
                    externalTypeImage.color = color;
                    externalTypeText.text = prefabType.Name + " [ Click to Open Prefab Type Editor ]";

                    prefabPanel.TypeImage.color = color;
                    prefabPanel.TypeIcon.sprite = prefabType.icon;
                    prefabPanel.TypeText.text = prefabType.Name;

                    if (!string.IsNullOrEmpty(prefab.filePath))
                        RTFile.WriteToFile(prefab.filePath, prefab.ToJSON().ToString());
                });
            });

            importPrefab.onClick.ClearAll();
            importPrefab.onClick.AddListener(() => { ImportPrefabIntoLevel(prefab); });

            exportToVG.onClick.ClearAll();
            exportToVG.onClick.AddListener(() =>
            {
                var exportPath = EditorConfig.Instance.ConvertPrefabLSToVGExportPath.Value;

                if (string.IsNullOrEmpty(exportPath))
                {
                    if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/exports"))
                        Directory.CreateDirectory(RTFile.ApplicationDirectory + "beatmaps/exports");
                    exportPath = RTFile.ApplicationDirectory + "beatmaps/exports/";
                }

                if (!string.IsNullOrEmpty(exportPath) && exportPath[exportPath.Length - 1] != '/')
                    exportPath += "/";

                if (!RTFile.DirectoryExists(Path.GetDirectoryName(exportPath)))
                {
                    EditorManager.inst.DisplayNotification("Directory does not exist.", 2f, EditorManager.NotificationType.Error);
                    return;
                }

                var vgjn = prefab.ToJSONVG();

                RTFile.WriteToFile($"{exportPath}{prefab.Name.ToLower()}.vgp", vgjn.ToString());

                EditorManager.inst.DisplayNotification($"Converted Prefab {prefab.Name.ToLower()}.lsp from LS format to VG format and saved to {prefab.Name.ToLower()}.vgp!", 4f, EditorManager.NotificationType.Success);

                AchievementManager.inst.UnlockAchievement("time_machine");
            });

            externalDescriptionField.onValueChanged.ClearAll();
            externalDescriptionField.onEndEdit.ClearAll();
            externalDescriptionField.text = prefab.description;
            externalDescriptionField.onValueChanged.AddListener(_val => { prefab.description = _val; });
            externalDescriptionField.onEndEdit.AddListener(_val =>
            {
                RTEditor.inst.DisablePrefabWatcher();

                if (!string.IsNullOrEmpty(prefab.filePath))
                    RTFile.WriteToFile(prefab.filePath, prefab.ToJSON().ToString());

                RTEditor.inst.EnablePrefabWatcher();
            });

            externalNameField.onValueChanged.ClearAll();
            externalNameField.onEndEdit.ClearAll();
            externalNameField.text = prefab.Name;
            externalNameField.onValueChanged.AddListener(_val => { prefab.Name = _val; });
            externalNameField.onEndEdit.AddListener(_val =>
            {
                RTEditor.inst.DisablePrefabWatcher();

                if (RTFile.FileExists(prefab.filePath))
                    File.Delete(prefab.filePath);

                var file = $"{RTFile.ApplicationDirectory}{RTEditor.prefabListSlash}{prefab.Name.ToLower().Replace(" ", "_")}.lsp";
                prefab.filePath = file;
                RTFile.WriteToFile(file, prefab.ToJSON().ToString());

                prefabPanel.Name.text = prefab.Name;

                RTEditor.inst.EnablePrefabWatcher();
            });
        }

        public void CreateNewPrefab()
        {
            if (ObjectEditor.inst.SelectedBeatmapObjects.Count <= 0)
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
                ObjectEditor.inst.SelectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()).ToList(),
                ObjectEditor.inst.SelectedPrefabObjects.Select(x => x.GetData<PrefabObject>()).ToList());

            prefab.description = NewPrefabDescription;
            prefab.typeID = NewPrefabTypeID;

            foreach (var beatmapObject in prefab.objects)
            {
                if (!prefab.SpriteAssets.ContainsKey(beatmapObject.text) && AssetManager.SpriteAssets.TryGetValue(beatmapObject.text, out Sprite sprite))
                    prefab.SpriteAssets.Add(beatmapObject.text, sprite);
            }

            if (createInternal)
            {
                ImportPrefabIntoLevel(prefab);
                EditorManager.inst.DisplayNotification($"Saving Prefab to level [{prefab.Name}]!", 2f, EditorManager.NotificationType.Warning);
            }
            else
                SavePrefab(prefab);

            EditorManager.inst.HideDialog("Prefab Popup");
            EditorManager.inst.HideDialog("Prefab Editor");
        }

        public void SavePrefab(Prefab prefab)
        {
            RTEditor.inst.DisablePrefabWatcher();

            EditorManager.inst.DisplayNotification($"Saving Prefab to System [{prefab.Name}]!", 2f, EditorManager.NotificationType.Warning);

            prefab.objects.ForEach(x => { x.prefabID = ""; x.prefabInstanceID = ""; });
            int count = PrefabPanels.Count;
            var file = $"{RTFile.ApplicationDirectory}{RTEditor.prefabListSlash}{prefab.Name.ToLower().Replace(" ", "_")}.lsp";
            prefab.filePath = file;

            var config = EditorConfig.Instance;

            var hoverSize = config.PrefabButtonHoverSize.Value;

            var nameHorizontalOverflow = config.PrefabExternalNameHorizontalWrap.Value;

            var nameVerticalOverflow = config.PrefabExternalNameVerticalWrap.Value;

            var nameFontSize = config.PrefabExternalNameFontSize.Value;

            var typeHorizontalOverflow = config.PrefabExternalTypeHorizontalWrap.Value;

            var typeVerticalOverflow = config.PrefabExternalTypeVerticalWrap.Value;

            var typeFontSize = config.PrefabExternalTypeFontSize.Value;

            var deleteAnchoredPosition = config.PrefabExternalDeleteButtonPos.Value;
            var deleteSizeDelta = config.PrefabExternalDeleteButtonSca.Value;

            StartCoroutine(CreatePrefabButton(prefab, count, PrefabDialog.External, $"{RTFile.ApplicationDirectory}{RTEditor.prefabListSlash}{prefab.Name.ToLower().Replace(" ", "_")}.lsp",
                false, hoverSize, nameHorizontalOverflow, nameVerticalOverflow, nameFontSize,
                typeHorizontalOverflow, typeVerticalOverflow, typeFontSize, deleteAnchoredPosition, deleteSizeDelta));

            RTFile.WriteToFile(file, prefab.ToJSON().ToString());
            EditorManager.inst.DisplayNotification($"Saved prefab [{prefab.Name}]!", 2f, EditorManager.NotificationType.Success);

            RTEditor.inst.EnablePrefabWatcher();
        }

        public void DeleteExternalPrefab(PrefabPanel prefabPanel)
        {
            RTEditor.inst.DisablePrefabWatcher();

            if (RTFile.FileExists(prefabPanel.FilePath))
                FileManager.inst.DeleteFileRaw(prefabPanel.FilePath);

            Destroy(prefabPanel.GameObject);
            PrefabPanels.RemoveAt(prefabPanel.Index);

            int num = 0;
            foreach (var p in PrefabPanels)
            {
                p.Index = num;
                num++;
            }

            RTEditor.inst.EnablePrefabWatcher();
        }

        public void DeleteInternalPrefab(int __0)
        {
            string id = GameData.Current.prefabs[__0].ID;

            GameData.Current.prefabs.RemoveAt(__0);

            GameData.Current.prefabObjects.FindAll(x => x.prefabID == id).ForEach(x =>
            {
                Updater.UpdatePrefab(x, false);

                var index = RTEditor.inst.timelineObjects.FindIndex(y => y.ID == x.ID);
                if (index >= 0)
                {
                    Destroy(RTEditor.inst.timelineObjects[index].GameObject);
                    RTEditor.inst.timelineObjects.RemoveAt(index);
                }
            });
            GameData.Current.prefabObjects.RemoveAll(x => x.prefabID == id);

            PrefabEditor.inst.ReloadInternalPrefabsInPopup();
        }

        public void OpenPopup()
        {
            EditorManager.inst.ClearPopups();
            EditorManager.inst.ShowDialog("Prefab Popup");
            PrefabEditor.inst.UpdateCurrentPrefab(PrefabEditor.inst.currentPrefab);

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

        public void ReloadSelectionContent()
        {
            LSHelpers.DeleteChildren(PrefabEditor.inst.gridContent, false);
            int num = 0;
            foreach (var beatmapObject in GameData.Current.beatmapObjects.FindAll(x => !x.fromPrefab))
            {
                if (CoreHelper.SearchString(PrefabEditor.inst.gridSearch.text, beatmapObject.name))
                {
                    var selection = PrefabEditor.inst.selectionPrefab.Duplicate(PrefabEditor.inst.gridContent, "grid");
                    var text = selection.transform.Find("text").GetComponent<Text>();
                    text.text = beatmapObject.name;

                    if (RTEditor.inst.timelineObjects.TryFind(x => x.ID == beatmapObject.id, out TimelineObject timelineObject))
                    {
                        var selectionToggle = selection.GetComponent<Toggle>();

                        selectionToggle.onValueChanged.ClearAll();
                        selectionToggle.isOn = timelineObject.selected;
                        selectionToggle.onValueChanged.AddListener(_val => { timelineObject.selected = _val; });
                        EditorThemeManager.ApplyToggle(selectionToggle, text: text);
                    }
                }
                num++;
            }
        }

        public void OpenDialog()
        {
            EditorManager.inst.ClearDialogs();
            EditorManager.inst.ShowDialog("Prefab Editor");

            var component = PrefabEditor.inst.dialog.Find("data/name/input").GetComponent<InputField>();
            component.onValueChanged.ClearAll();
            component.onValueChanged.AddListener(_val => { PrefabEditor.inst.NewPrefabName = _val; });

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

            TriggerHelper.AddEventTriggers(offsetInput.gameObject, TriggerHelper.ScrollDelta(offsetInput));

            if (DataManager.inst.PrefabTypes.TryFind(x => x is PrefabType prefabType && prefabType.id == NewPrefabTypeID, out DataManager.PrefabType vanillaPrefabType) && vanillaPrefabType is PrefabType prefabType)
            {
                externalCreatorText.text = prefabType.Name + " [ Click to Open Prefab Type Editor ]";
                externalCreatorImage.color = prefabType.Color;
            }

            var description = PrefabEditor.inst.dialog.Find("data/description/input").GetComponent<InputField>();
            description.onValueChanged.ClearAll();
            ((Text)description.placeholder).text = "Prefab Description";
            description.lineType = InputField.LineType.MultiLineNewline;
            description.characterLimit = 0;
            description.characterValidation = InputField.CharacterValidation.None;
            description.textComponent.alignment = TextAnchor.UpperLeft;
            NewPrefabDescription = string.IsNullOrEmpty(NewPrefabDescription) ? "What is your prefab like?" : NewPrefabDescription;
            description.text = NewPrefabDescription;
            description.onValueChanged.AddListener(_val => { NewPrefabDescription = _val; });

            ReloadSelectionContent();
        }

        public void UpdateCurrentPrefab(BasePrefab __0)
        {
            PrefabEditor.inst.currentPrefab = __0;

            bool prefabExists = PrefabEditor.inst.currentPrefab != null;

            selectQuickPrefabText.text = (!prefabExists ? "-Select Prefab-" : "<color=#669e37>-Prefab-</color>") + "\n" + (!prefabExists ? "n/a" : PrefabEditor.inst.currentPrefab.Name);
        }

        public void PastePrefab()
        {
            if (string.IsNullOrEmpty(copiedPrefabPath))
            {
                EditorManager.inst.DisplayNotification("No prefab has been copied yet!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            if (!RTFile.DirectoryExists(copiedPrefabPath))
            {
                EditorManager.inst.DisplayNotification("Copied prefab no longer exists.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var copiedPrefabsFolder = Path.GetDirectoryName(copiedPrefabPath).Replace("\\", "/");
            CoreHelper.Log($"Copied Folder: {copiedPrefabsFolder}");

            if (copiedPrefabsFolder == $"{RTFile.ApplicationDirectory}{RTEditor.prefabListPath}")
            {
                EditorManager.inst.DisplayNotification("Source and destination are the same.", 2f, EditorManager.NotificationType.Warning);
                return;
            }

            var destination = copiedPrefabPath.Replace(copiedPrefabsFolder, $"{RTFile.ApplicationDirectory}{RTEditor.prefabListPath}");
            CoreHelper.Log($"Destination: {destination}");
            if (RTFile.FileExists(destination))
            {
                EditorManager.inst.DisplayNotification("File already exists.", 2f, EditorManager.NotificationType.Warning);
                return;
            }

            if (shouldCutPrefab)
            {
                File.Move(copiedPrefabPath, destination);
                var prefab = Prefab.Parse(JSON.Parse(RTFile.ReadFromFile(destination)));
                EditorManager.inst.DisplayNotification($"Succesfully moved {prefab.Name}!", 2f, EditorManager.NotificationType.Success);
            }
            else
            {
                File.Copy(copiedPrefabPath, destination, true);
                var prefab = Prefab.Parse(JSON.Parse(RTFile.ReadFromFile(destination)));
                EditorManager.inst.DisplayNotification($"Succesfully pasted {prefab.Name}!", 2f, EditorManager.NotificationType.Success);
            }

            RTEditor.inst.UpdatePrefabPath(true);
        }

        public IEnumerator InternalPrefabs(bool _toggle = false)
        {
            var config = EditorConfig.Instance;

            // Here we add the Example prefab provided to you.
            if (!GameData.Current.prefabs.Exists(x => x.ID == LegacyPlugin.ExamplePrefab.ID) && config.PrefabExampleTemplate.Value)
                GameData.Current.prefabs.Add(Prefab.DeepCopy(LegacyPlugin.ExamplePrefab, false));

            yield return new WaitForSeconds(0.03f);

            LSHelpers.DeleteChildren(PrefabEditor.inst.internalContent);
            var gameObject = PrefabEditor.inst.CreatePrefab.Duplicate(PrefabEditor.inst.internalContent, "add new prefab");
            var text = gameObject.GetComponentInChildren<Text>();
            text.text = "New Internal Prefab";

            var hoverSize = config.PrefabButtonHoverSize.Value;

            var hover = gameObject.AddComponent<HoverUI>();
            hover.animateSca = true;
            hover.animatePos = false;
            hover.size = hoverSize;

            var createNewButton = gameObject.GetComponent<Button>();
            createNewButton.NewOnClickListener(() =>
            {
                if (RTEditor.inst.prefabPickerEnabled)
                    RTEditor.inst.prefabPickerEnabled = false;

                PrefabEditor.inst.OpenDialog();
                createInternal = true;
            });

            EditorThemeManager.ApplyGraphic(createNewButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(text, ThemeGroup.Add_Text);

            var nameHorizontalOverflow = config.PrefabInternalNameHorizontalWrap.Value;

            var nameVerticalOverflow = config.PrefabInternalNameVerticalWrap.Value;

            var nameFontSize = config.PrefabInternalNameFontSize.Value;

            var typeHorizontalOverflow = config.PrefabInternalTypeHorizontalWrap.Value;

            var typeVerticalOverflow = config.PrefabInternalTypeVerticalWrap.Value;

            var typeFontSize = config.PrefabInternalTypeFontSize.Value;

            var deleteAnchoredPosition = config.PrefabInternalDeleteButtonPos.Value;
            var deleteSizeDelta = config.PrefabInternalDeleteButtonSca.Value;

            var list = new List<Coroutine>();

            var prefabs = GameData.Current.prefabs;
            for (int i = 0; i < prefabs.Count; i++)
            {
                var prefab = (Prefab)prefabs[i];
                if (ContainsName(prefab, PrefabDialog.Internal))
                    list.Add(StartCoroutine(CreatePrefabButton(prefab, i, PrefabDialog.Internal, null, _toggle, hoverSize,
                        nameHorizontalOverflow, nameVerticalOverflow, nameFontSize,
                        typeHorizontalOverflow, typeVerticalOverflow, typeFontSize,
                        deleteAnchoredPosition, deleteSizeDelta)));
            }

            yield break;
        }

        public IEnumerator ExternalPrefabFiles(bool _toggle = false)
        {
            foreach (var prefabPanel in PrefabPanels.Where(x => x.Dialog == PrefabDialog.External))
            {
                prefabPanel.SetActive(
                    prefabPanel.isFolder ?
                        CoreHelper.SearchString(PrefabEditor.inst.externalSearchStr, Path.GetFileName(prefabPanel.FilePath)) :
                        ContainsName(prefabPanel.Prefab, PrefabDialog.External));
            }

            yield break;
        }

        public IEnumerator CreatePrefabButton(Prefab prefab, int index, PrefabDialog dialog, string file, bool _toggle, float hoversize,
            HorizontalWrapMode nameHorizontalWrapMode, VerticalWrapMode nameVerticalWrapMode, int nameFontSize,
            HorizontalWrapMode typeHorizontalWrapMode, VerticalWrapMode typeVerticalWrapMode, int typeFontSize,
            Vector2 deleteAnchoredPosition, Vector2 deleteSizeDelta)
        {
            bool isExternal = dialog == PrefabDialog.External;
            var gameObject = PrefabEditor.inst.AddPrefab.Duplicate(isExternal ? PrefabEditor.inst.externalContent : PrefabEditor.inst.internalContent);
            var tf = gameObject.transform;

            var hover = gameObject.AddComponent<HoverUI>();
            hover.animateSca = true;
            hover.animatePos = false;
            hover.size = hoversize;

            var storage = gameObject.GetComponent<PrefabPanelStorage>();

            var name = storage.nameText;
            var typeName = storage.typeNameText;
            var typeImage = storage.typeImage;
            var typeImageShade = storage.typeImageShade;
            var typeIconImage = storage.typeIconImage;
            var deleteRT = storage.deleteButton.transform.AsRT();
            var addPrefabObject = storage.button;
            var delete = storage.deleteButton;

            EditorThemeManager.ApplySelectable(addPrefabObject, ThemeGroup.List_Button_1);
            EditorThemeManager.ApplyLightText(name);
            EditorThemeManager.ApplyLightText(typeName);
            EditorThemeManager.ApplyGraphic(delete.image, ThemeGroup.Delete, true);
            EditorThemeManager.ApplyGraphic(delete.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Delete_Text);
            EditorThemeManager.ApplyGraphic(typeImage, ThemeGroup.Null, true);
            EditorThemeManager.ApplyGraphic(typeImageShade, ThemeGroup.Null, true);

            name.text = prefab.Name;

            var prefabType = prefab.PrefabType;

            typeName.text = prefabType.Name;
            typeImage.color = prefabType.Color;
            typeIconImage.sprite = prefabType.icon;

            TooltipHelper.AssignTooltip(gameObject, $"{dialog} Prefab List Button", 3.2f);
            TooltipHelper.AddHoverTooltip(gameObject,
                "<#" + LSColors.ColorToHex(typeImage.color) + ">" + prefab.Name + "</color>",
                "O: " + prefab.Offset +
                "<br>T: " + typeName.text +
                "<br>Count: " + prefab.objects.Count +
                "<br>Description: " + prefab.description);

            addPrefabObject.onClick.ClearAll();
            delete.onClick.ClearAll();

            name.horizontalOverflow = nameHorizontalWrapMode;
            name.verticalOverflow = nameVerticalWrapMode;
            name.fontSize = nameFontSize;

            typeName.horizontalOverflow = typeHorizontalWrapMode;
            typeName.verticalOverflow = typeVerticalWrapMode;
            typeName.fontSize = typeFontSize;

            deleteRT.anchoredPosition = deleteAnchoredPosition;
            deleteRT.sizeDelta = deleteSizeDelta;

            if (!isExternal)
            {
                delete.onClick.AddListener(() =>
                {
                    RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this prefab? (This is permanent!)", () =>
                    {
                        PrefabEditor.inst.DeleteInternalPrefab(index);
                        RTEditor.inst.HideWarningPopup();
                    }, RTEditor.inst.HideWarningPopup);
                });
                addPrefabObject.onClick.AddListener(() =>
                {
                    if (RTEditor.inst.prefabPickerEnabled)
                    {
                        var prefabInstanceID = LSText.randomString(16);
                        if (RTEditor.inst.selectingMultiple)
                        {
                            foreach (var otherTimelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                            {
                                var otherBeatmapObject = otherTimelineObject.GetData<BeatmapObject>();

                                otherBeatmapObject.prefabID = prefab.ID;
                                otherBeatmapObject.prefabInstanceID = prefabInstanceID;
                                ObjectEditor.inst.RenderTimelineObject(otherTimelineObject);
                            }
                        }
                        else if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
                        {
                            var currentBeatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();

                            currentBeatmapObject.prefabID = prefab.ID;
                            currentBeatmapObject.prefabInstanceID = prefabInstanceID;
                            ObjectEditor.inst.RenderTimelineObject(ObjectEditor.inst.CurrentSelection);
                            ObjectEditor.inst.OpenDialog(currentBeatmapObject);
                        }

                        RTEditor.inst.prefabPickerEnabled = false;

                        return;
                    }

                    if (!_toggle)
                    {
                        AddPrefabObjectToLevel(prefab);
                        EditorManager.inst.HideDialog("Prefab Popup");
                        return;
                    }
                    UpdateCurrentPrefab(prefab);
                    PrefabEditor.inst.ReloadInternalPrefabsInPopup();
                });
            }
            else
            {
                var prefabPanel = new PrefabPanel
                {
                    GameObject = gameObject,
                    Button = addPrefabObject,
                    DeleteButton = delete,
                    Dialog = dialog,
                    Name = name,
                    TypeText = typeName,
                    TypeImage = typeImage,
                    TypeIcon = typeIconImage,
                    Prefab = prefab,
                    Index = index,
                    FilePath = file
                };
                PrefabPanels.Add(prefabPanel);

                delete.onClick.AddListener(() =>
                {
                    RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this prefab? (This is permanent!)", () =>
                    {
                        DeleteExternalPrefab(prefabPanel);
                        RTEditor.inst.HideWarningPopup();
                    }, RTEditor.inst.HideWarningPopup);
                });
                addPrefabObject.onClick.AddListener(() =>
                {
                });

                var clickable = gameObject.AddComponent<ContextClickable>();
                clickable.onClick = eventData =>
                {
                    if (RTEditor.inst.prefabPickerEnabled)
                        RTEditor.inst.prefabPickerEnabled = false;

                    if (savingToPrefab && prefabToSaveFrom != null)
                    {
                        savingToPrefab = false;

                        var prefabToSaveTo = prefabPanel.Prefab;

                        prefabToSaveTo.objects = prefabToSaveFrom.objects.Clone();
                        prefabToSaveTo.prefabObjects = prefabToSaveFrom.prefabObjects.Clone();
                        prefabToSaveTo.Offset = prefabToSaveFrom.Offset;
                        prefabToSaveTo.Type = prefabToSaveFrom.Type;
                        prefabToSaveTo.typeID = prefabToSaveFrom.typeID;

                        var prefabType = prefab.PrefabType;

                        typeName.text = prefabType.Name;
                        typeImage.color = prefabType.Color;
                        typeIconImage.sprite = prefabType.icon;

                        TooltipHelper.AddHoverTooltip(gameObject,
                            "<#" + LSColors.ColorToHex(typeImage.color) + ">" + prefab.Name + "</color>",
                            "O: " + prefab.Offset +
                            "<br>T: " + typeName.text +
                            "<br>Count: " + prefabToSaveTo.objects.Count +
                            "<br>Description: " + prefabToSaveTo.description);

                        RTFile.WriteToFile(prefabToSaveTo.filePath, prefabToSaveTo.ToJSON().ToString());

                        EditorManager.inst.HideDialog("Prefab Popup");

                        prefabToSaveFrom = null;

                        EditorManager.inst.DisplayNotification("Applied all changes to External Prefab.", 2f, EditorManager.NotificationType.Success);

                        return;
                    }

                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        RTEditor.inst.RefreshContextMenu(300f,
                            new RTEditor.ButtonFunction("Import", () => { ImportPrefabIntoLevel(prefabPanel.Prefab); }),
                            new RTEditor.ButtonFunction("Open", () =>
                            {
                                EditorManager.inst.ShowDialog("Prefab External Dialog");
                                RenderPrefabExternalDialog(prefabPanel);
                            }),
                            new RTEditor.ButtonFunction("Create folder", () =>
                            {
                                EditorManager.inst.ShowDialog("Folder Creator Popup");
                                RTEditor.inst.RefreshFolderCreator($"{RTFile.ApplicationDirectory}{RTEditor.prefabListPath}", () => RTEditor.inst.UpdatePrefabPath(true));
                            }),
                            new RTEditor.ButtonFunction("Cut", () =>
                            {
                                shouldCutPrefab = true;
                                copiedPrefabPath = file;
                                EditorManager.inst.DisplayNotification($"Cut {prefab.Name}!", 1.5f, EditorManager.NotificationType.Success);
                                CoreHelper.Log($"Cut prefab: {copiedPrefabPath}");
                            }),
                            new RTEditor.ButtonFunction("Copy", () =>
                            {
                                shouldCutPrefab = false;
                                copiedPrefabPath = file;
                                EditorManager.inst.DisplayNotification($"Copied {prefab.Name}!", 1.5f, EditorManager.NotificationType.Success);
                                CoreHelper.Log($"Copied prefab: {copiedPrefabPath}");
                            }),
                            new RTEditor.ButtonFunction("Paste", PastePrefab),
                            new RTEditor.ButtonFunction("Delete", () =>
                            {
                                RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this prefab? (This is permanent!)", () =>
                                {
                                    DeleteExternalPrefab(prefabPanel);
                                    RTEditor.inst.HideWarningPopup();
                                }, RTEditor.inst.HideWarningPopup);
                            })
                            );

                        return;
                    }

                    if (!ImportPrefabsDirectly)
                    {
                        EditorManager.inst.ShowDialog("Prefab External Dialog");
                        RenderPrefabExternalDialog(prefabPanel);
                    }
                    else
                        ImportPrefabIntoLevel(prefabPanel.Prefab);
                };

                prefabPanel.SetActive(ContainsName(prefabPanel.Prefab, PrefabDialog.External));
            }

            yield break;
        }

        public bool ContainsName(Prefab _p, PrefabDialog _d)
        {
            string str = _d == PrefabDialog.External ?
                string.IsNullOrEmpty(PrefabEditor.inst.externalSearchStr) ? "" : PrefabEditor.inst.externalSearchStr.ToLower() :
                string.IsNullOrEmpty(PrefabEditor.inst.internalSearchStr) ? "" : PrefabEditor.inst.internalSearchStr.ToLower();
            return string.IsNullOrEmpty(str) || _p.Name.ToLower().Contains(str) || _p.PrefabType.Name.ToLower().Contains(str);
        }

        public void ImportPrefabIntoLevel(BasePrefab _prefab)
        {
            Debug.Log($"{PrefabEditor.inst.className}Adding Prefab: [{_prefab.Name}]");
            var tmpPrefab = Prefab.DeepCopy((Prefab)_prefab);
            int num = GameData.Current.prefabs.FindAll(x => Regex.Replace(x.Name, "( +\\[\\d+])", string.Empty) == tmpPrefab.Name).Count;
            if (num > 0)
                tmpPrefab.Name = $"{tmpPrefab.Name} [{num}]";

            GameData.Current.prefabs.Add(tmpPrefab);
            PrefabEditor.inst.ReloadInternalPrefabsInPopup();
        }

        #endregion
    }
}
