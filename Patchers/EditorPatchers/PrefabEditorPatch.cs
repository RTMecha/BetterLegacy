using BetterLegacy.Components;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;
using HarmonyLib;
using LSFunctions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using BasePrefab = DataManager.GameData.Prefab;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(PrefabEditor))]
    public class PrefabEditorPatch : MonoBehaviour
    {
        static PrefabEditor Instance { get => PrefabEditor.inst; set => PrefabEditor.inst = value; }

        [HarmonyPatch(nameof(PrefabEditor.Awake))]
        [HarmonyPrefix]
        static bool AwakePrefix(PrefabEditor __instance)
        {
            if (Instance == null)
                Instance = __instance;
            else if (Instance != __instance)
            {
                Destroy(__instance.gameObject);
                return false;
            }

            CoreHelper.LogInit(__instance.className);

            // Prefab Type Icon
            {
                var gameObject = PrefabEditor.inst.AddPrefab.Duplicate(__instance.transform, PrefabEditor.inst.AddPrefab.name);

                var type = gameObject.transform.Find("category");
                type.GetComponent<LayoutElement>().minWidth = 32f;

                var b = Creator.NewUIObject("type", type);
                b.transform.AsRT().anchoredPosition = Vector2.zero;
                b.transform.AsRT().sizeDelta = new Vector2(28f, 28f);

                var bImage = b.AddComponent<Image>();
                bImage.color = new Color(0f, 0f, 0f, 0.45f);

                var icon = Creator.NewUIObject("type", b.transform);
                icon.transform.AsRT().anchoredPosition = Vector2.zero;
                icon.transform.AsRT().sizeDelta = new Vector2(28f, 28f);

                icon.AddComponent<Image>();

                var storage = gameObject.AddComponent<PrefabPanelStorage>();

                var tf = gameObject.transform;
                storage.nameText = tf.Find("name").GetComponent<Text>();
                storage.typeNameText = tf.Find("type-name").GetComponent<Text>();
                storage.typeImage = tf.Find("category").GetComponent<Image>();
                storage.typeImageShade = tf.Find("category/type").GetComponent<Image>();
                storage.typeIconImage = tf.Find("category/type/type").GetComponent<Image>();
                storage.button = gameObject.GetComponent<Button>();
                storage.deleteButton = tf.Find("delete").GetComponent<Button>();

                PrefabEditor.inst.AddPrefab = gameObject;
            }

            RTPrefabEditor.Init(__instance);

            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.Start))]
        [HarmonyPrefix]
        static bool StartPrefix()
        {
            RTPrefabEditor.loadingPrefabTypes = true;
            Instance.StartCoroutine(RTEditor.inst.LoadPrefabs(Instance));
            Instance.OffsetLine = Instance.OffsetLinePrefab.Duplicate(EditorManager.inst.timeline.transform, "offset line");
            Instance.OffsetLine.transform.AsRT().pivot = Vector2.one;

            Instance.dialog = EditorManager.inst.GetDialog("Prefab Editor").Dialog;
            Instance.externalPrefabDialog = EditorManager.inst.GetDialog("Prefab Popup").Dialog.Find("external prefabs");
            Instance.internalPrefabDialog = EditorManager.inst.GetDialog("Prefab Popup").Dialog.Find("internal prefabs");
            Instance.externalSearch = Instance.externalPrefabDialog.Find("search-box/search").GetComponent<InputField>();
            Instance.internalSearch = Instance.internalPrefabDialog.Find("search-box/search").GetComponent<InputField>();
            Instance.externalContent = Instance.externalPrefabDialog.Find("mask/content");
            Instance.internalContent = Instance.internalPrefabDialog.Find("mask/content");

            var externalSelectGUI = Instance.externalPrefabDialog.gameObject.AddComponent<SelectGUI>();
            var internalSelectGUI = Instance.internalPrefabDialog.gameObject.AddComponent<SelectGUI>();
            externalSelectGUI.ogPos = Instance.externalPrefabDialog.position;
            internalSelectGUI.ogPos = Instance.internalPrefabDialog.position;
            externalSelectGUI.target = Instance.externalPrefabDialog;
            internalSelectGUI.target = Instance.internalPrefabDialog;

            Instance.internalPrefabDialog.Find("Panel/Text").GetComponent<Text>().text = "Internal Prefabs";

            Instance.gridSearch = Instance.dialog.Find("data/selection/search-box/search").GetComponent<InputField>();
            Instance.gridContent = Instance.dialog.Find("data/selection/mask/content");

            Destroy(Instance.dialog.Find("data/type/types").GetComponent<VerticalLayoutGroup>());

            var dialog = EditorManager.inst.GetDialog("Prefab Selector").Dialog;
            RTPrefabEditor.inst.prefabSelectorLeft = dialog.Find("data/left");
            RTPrefabEditor.inst.prefabSelectorRight = dialog.Find("data/right");

            var contentBase = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content");
            var scrollView = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View").Duplicate(RTPrefabEditor.inst.prefabSelectorLeft);

            var parent = scrollView.transform.Find("Viewport/Content");

            LSHelpers.DeleteChildren(parent, true);

            var objectsToDelete = new List<GameObject>();
            for (int i = 0; i < parent.childCount; i++)
                objectsToDelete.Add(parent.GetChild(i).gameObject);
            foreach (var child in objectsToDelete)
                DestroyImmediate(child);

            scrollView.transform.AsRT().sizeDelta = new Vector2(383f, 690f);

            var objectsToParent = new List<Transform>();
            for (int i = 0; i < RTPrefabEditor.inst.prefabSelectorLeft.childCount; i++)
                objectsToParent.Add(RTPrefabEditor.inst.prefabSelectorLeft.GetChild(i));
            foreach (var child in objectsToParent)
                child.SetParent(parent);

            RTPrefabEditor.inst.prefabSelectorLeft = parent;

            EditorHelper.LogAvailableInstances<PrefabEditor>();

            EditorThemeManager.AddGraphic(dialog.GetComponent<Image>(), ThemeGroup.Background_1);

            var eventDialogTMP = EditorManager.inst.GetDialog("Event Editor").Dialog.Find("data/right");

            var singleInput = eventDialogTMP.Find("move/position/x").gameObject.Duplicate(Instance.transform);
            var vector2Input = eventDialogTMP.Find("move/position").gameObject.Duplicate(Instance.transform);
            var labelTemp = eventDialogTMP.Find("move").transform.GetChild(8).gameObject.Duplicate(Instance.transform);

            // Single
            {
                var buttonLeft = singleInput.transform.Find("<").GetComponent<Button>();
                var buttonRight = singleInput.transform.Find(">").GetComponent<Button>();

                Destroy(buttonLeft.GetComponent<Animator>());
                buttonLeft.transition = Selectable.Transition.ColorTint;

                Destroy(buttonRight.GetComponent<Animator>());
                buttonRight.transition = Selectable.Transition.ColorTint;
            }

            DestroyImmediate(RTPrefabEditor.inst.prefabSelectorLeft.GetChild(4).gameObject);
            DestroyImmediate(RTPrefabEditor.inst.prefabSelectorLeft.GetChild(4).gameObject);

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
            labelGenerator(RTPrefabEditor.inst.prefabSelectorLeft, "tod-dropdown", "Time of Death");

            var autoKillType = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown")
                .Duplicate(RTPrefabEditor.inst.prefabSelectorLeft, "tod-dropdown", 14);
            var autoKillTypeDD = autoKillType.GetComponent<Dropdown>();
            autoKillTypeDD.options = CoreHelper.StringToOptionData("Regular", "Start Offset", "Song Time");
            autoKillType.GetComponent<HideDropdownOptions>().DisabledOptions = new List<bool>
            {
                false,
                false,
                false,
            };

            EditorThemeManager.AddDropdown(autoKillTypeDD);

            var ako = singleInput.Duplicate(RTPrefabEditor.inst.prefabSelectorLeft, "akoffset");
            EditorThemeManager.AddInputField(ako.GetComponent<InputField>());
            EditorThemeManager.AddSelectable(ako.transform.Find("<").GetComponent<Button>(), ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(ako.transform.Find(">").GetComponent<Button>(), ThemeGroup.Function_2, false);

            var setToCurrent = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/|").Duplicate(RTPrefabEditor.inst.prefabSelectorLeft.Find("akoffset"), "|");

            var setToCurrentButton = setToCurrent.GetComponent<Button>();
            Destroy(setToCurrent.GetComponent<Animator>());
            setToCurrentButton.transition = Selectable.Transition.ColorTint;

            EditorThemeManager.AddSelectable(setToCurrentButton, ThemeGroup.Function_2, false);

            // Parent
            var parentUI = contentBase.transform.Find("parent").gameObject.Duplicate(RTPrefabEditor.inst.prefabSelectorLeft, "parent");
            var parent_more = contentBase.transform.Find("parent_more").gameObject.Duplicate(RTPrefabEditor.inst.prefabSelectorLeft, "parent_more");

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

            var array = new string[]
            {
                "pos",
                "sca",
                "rot",
            };
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
            labelGenerator(RTPrefabEditor.inst.prefabSelectorLeft, "time", "Time");

            var time = EditorPrefabHolder.Instance.NumberInputField.Duplicate(RTPrefabEditor.inst.prefabSelectorLeft, "time");
            var timeStorage = time.GetComponent<InputFieldStorage>();
            EditorThemeManager.AddInputField(timeStorage.inputField);
            timeStorage.inputField.transform.AsRT().sizeDelta = new Vector2(135f, 32f);

            EditorThemeManager.AddSelectable(timeStorage.leftGreaterButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(timeStorage.leftButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(timeStorage.middleButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(timeStorage.rightButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(timeStorage.rightGreaterButton, ThemeGroup.Function_2, false);

            var timeParent = time.transform;

            var locker = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle/toggle").Duplicate(timeParent, "lock", 0);

            locker.transform.Find("Background/Checkmark").GetComponent<Image>().sprite = ObjEditor.inst.timelineObjectPrefabLock.transform.Find("lock (1)").GetComponent<Image>().sprite;

            EditorThemeManager.AddToggle(locker.GetComponent<Toggle>());

            var collapser = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/collapse").Duplicate(timeParent, "collapse", 1);

            EditorThemeManager.AddToggle(collapser.GetComponent<Toggle>(), ThemeGroup.Background_1);

            for (int i = 0; i < collapser.transform.Find("dots").childCount; i++)
                EditorThemeManager.AddGraphic(collapser.transform.Find("dots").GetChild(i).GetComponent<Image>(), ThemeGroup.Dark_Text);

            // Position
            labelGenerator2(RTPrefabEditor.inst.prefabSelectorLeft, "pos", "Position X Offset", "Position Y Offset");

            var position = vector2Input.Duplicate(RTPrefabEditor.inst.prefabSelectorLeft, "position");
            var positionX = position.transform.Find("x").gameObject.AddComponent<InputFieldSwapper>();
            positionX.Init(position.transform.Find("x").GetComponent<InputField>());
            var positionY = position.transform.Find("y").gameObject.AddComponent<InputFieldSwapper>();
            positionY.Init(position.transform.Find("y").GetComponent<InputField>());
            EditorThemeManager.AddInputFields(position, true, "");

            // Scale
            labelGenerator2(RTPrefabEditor.inst.prefabSelectorLeft, "sca", "Scale X Offset", "Scale Y Offset");

            var scale = vector2Input.Duplicate(RTPrefabEditor.inst.prefabSelectorLeft, "scale");
            var scaleX = scale.transform.Find("x").gameObject.AddComponent<InputFieldSwapper>();
            scaleX.Init(scale.transform.Find("x").GetComponent<InputField>());
            var scaleY = scale.transform.Find("y").gameObject.AddComponent<InputFieldSwapper>();
            scaleY.Init(scale.transform.Find("y").GetComponent<InputField>());
            EditorThemeManager.AddInputFields(scale, true, "");

            // Rotation
            labelGenerator(RTPrefabEditor.inst.prefabSelectorLeft, "rot", "Rotation Offset");

            var rot = vector2Input.Duplicate(RTPrefabEditor.inst.prefabSelectorLeft, "rotation");
            Destroy(rot.transform.GetChild(1).gameObject);
            var rotX = rot.transform.Find("x").gameObject.AddComponent<InputFieldSwapper>();
            rotX.Init(rot.transform.Find("x").GetComponent<InputField>());
            EditorThemeManager.AddInputFields(rot, true, "");

            // Repeat
            labelGenerator2(RTPrefabEditor.inst.prefabSelectorLeft, "repeat", "Repeat Count", "Repeat Offset Time");

            var repeat = vector2Input.Duplicate(RTPrefabEditor.inst.prefabSelectorLeft, "repeat");
            EditorThemeManager.AddInputFields(repeat, true, "");

            // Speed
            labelGenerator(RTPrefabEditor.inst.prefabSelectorLeft, "speed", "Speed");

            var speed = singleInput.Duplicate(RTPrefabEditor.inst.prefabSelectorLeft, "speed");
            EditorThemeManager.AddInputField(speed.GetComponent<InputField>());
            EditorThemeManager.AddSelectable(speed.transform.Find("<").GetComponent<Button>(), ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(speed.transform.Find(">").GetComponent<Button>(), ThemeGroup.Function_2, false);

            // Layers
            var layersIF = singleInput.Duplicate(RTPrefabEditor.inst.prefabSelectorLeft.Find("editor"), "layers", 0).GetComponent<InputField>();
            layersIF.gameObject.AddComponent<ContrastColors>().Init(layersIF.textComponent, layersIF.image);
            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Null, layersIF.gameObject, new List<Component> { layersIF }, true, 1, SpriteManager.RoundedSide.W));
            EditorThemeManager.AddSelectable(layersIF.transform.Find("<").GetComponent<Button>(), ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(layersIF.transform.Find(">").GetComponent<Button>(), ThemeGroup.Function_2, false);

            // Name
            labelGenerator(RTPrefabEditor.inst.prefabSelectorRight, "name", "Name");

            var prefabName = RTEditor.inst.defaultIF.Duplicate(RTPrefabEditor.inst.prefabSelectorRight, "name");
            prefabName.transform.localScale = Vector3.one;

            var prefabNameInputField = prefabName.GetComponent<InputField>();

            prefabNameInputField.characterValidation = InputField.CharacterValidation.None;
            prefabNameInputField.contentType = InputField.ContentType.Standard;
            prefabNameInputField.characterLimit = 0;
            RTPrefabEditor.inst.nameIF = prefabNameInputField;
            EditorThemeManager.AddInputField(prefabNameInputField);

            // Type
            labelGenerator(RTPrefabEditor.inst.prefabSelectorRight, "type", "Type");

            var type = singleInput.Duplicate(RTPrefabEditor.inst.prefabSelectorRight, "type");

            var typeInputField = type.GetComponent<InputField>();

            RTPrefabEditor.inst.typeImage = typeInputField.image;
            typeInputField.characterValidation = InputField.CharacterValidation.None;
            typeInputField.contentType = InputField.ContentType.Standard;
            RTPrefabEditor.inst.typeIF = typeInputField;
            typeInputField.gameObject.AddComponent<ContrastColors>().Init(typeInputField.textComponent, typeInputField.image);

            EditorThemeManager.AddSelectable(type.transform.Find("<").GetComponent<Button>(), ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(type.transform.Find(">").GetComponent<Button>(), ThemeGroup.Function_2, false);

            var expandPrefabLabel = RTPrefabEditor.inst.prefabSelectorLeft.GetChild(0).gameObject;
            var expandPrefabLabelText = expandPrefabLabel.transform.GetChild(0).GetComponent<Text>();
            var expandPrefab = RTPrefabEditor.inst.prefabSelectorLeft.GetChild(1).gameObject;
            var expandPrefabButton = expandPrefab.GetComponent<Button>();
            var expandPrefabText = expandPrefab.transform.GetChild(0).GetComponent<Text>();
            EditorThemeManager.AddLightText(expandPrefabLabelText);
            Destroy(expandPrefab.GetComponent<Animator>());
            expandPrefabButton.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.AddSelectable(expandPrefabButton, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(expandPrefabText, ThemeGroup.Function_2_Text);

            // Save Prefab
            var label = expandPrefabLabel.Duplicate(RTPrefabEditor.inst.prefabSelectorRight, "save prefab label");
            label.transform.localScale = Vector3.one;
            var applyToAllText = label.transform.GetChild(0).GetComponent<Text>();
            applyToAllText.fontSize = 19;
            applyToAllText.text = "Apply to an External Prefab";

            var savePrefab = expandPrefab.Duplicate(RTPrefabEditor.inst.prefabSelectorRight, "save prefab");
            savePrefab.transform.localScale = Vector3.one;
            var savePrefabText = savePrefab.transform.GetChild(0).GetComponent<Text>();
            savePrefabText.text = "Select Prefab";

            EditorThemeManager.AddLightText(applyToAllText);
            var savePrefabButton = savePrefab.GetComponent<Button>();
            Destroy(savePrefab.GetComponent<Animator>());
            savePrefabButton.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.AddSelectable(savePrefabButton, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(savePrefabText, ThemeGroup.Function_2_Text);

            Action<string, string, Action<Text, string>> countGenerator = (string name, string count, Action<Text, string> text) =>
            {
                var rotLabel = labelTemp.Duplicate(RTPrefabEditor.inst.prefabSelectorRight, name);

                Destroy(rotLabel.transform.GetChild(1).gameObject);

                text(rotLabel.transform.GetChild(0).GetComponent<Text>(), count);
            };

            // Object Count
            countGenerator("count label", "Object Count: 0", (Text text, string count) =>
            {
                RTPrefabEditor.inst.objectCount = text;
                RTPrefabEditor.inst.objectCount.text = count;

                EditorThemeManager.AddLightText(text);
            });

            // Prefab Object Count
            countGenerator("count label", "Prefab Object Count: 0", (Text text, string count) =>
            {
                RTPrefabEditor.inst.prefabObjectCount = text;
                RTPrefabEditor.inst.prefabObjectCount.text = count;

                EditorThemeManager.AddLightText(text);
            });

            // Prefab Object Timeline Count
            countGenerator("count label", "Prefab Object (Timeline) Count: 0", (Text text, string count) =>
            {
                RTPrefabEditor.inst.prefabObjectTimelineCount = text;
                RTPrefabEditor.inst.prefabObjectTimelineCount.text = count;

                EditorThemeManager.AddLightText(text);
            });

            DestroyImmediate(RTPrefabEditor.inst.prefabSelectorRight.Find("time").gameObject);
            var offsetTime = EditorPrefabHolder.Instance.NumberInputField.Duplicate(RTPrefabEditor.inst.prefabSelectorRight, "time", 1);
            offsetTime.transform.GetChild(0).name = "time";
            var offsetTimeStorage = offsetTime.GetComponent<InputFieldStorage>();
            Destroy(offsetTimeStorage.middleButton.gameObject);
            EditorThemeManager.AddInputField(offsetTimeStorage.inputField);
            EditorThemeManager.AddSelectable(offsetTimeStorage.leftGreaterButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(offsetTimeStorage.leftButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(offsetTimeStorage.rightButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(offsetTimeStorage.rightGreaterButton, ThemeGroup.Function_2, false);

            // Object Editor list

            var prefabEditorData = Instance.dialog.Find("data");

            EditorThemeManager.AddGraphic(prefabEditorData.Find("title/Panel/icon").GetComponent<Image>(), ThemeGroup.Light_Text);
            EditorThemeManager.AddLightText(prefabEditorData.Find("title/title").GetComponent<Text>());
            EditorThemeManager.AddLightText(prefabEditorData.Find("name/title").GetComponent<Text>());
            EditorThemeManager.AddLightText(prefabEditorData.Find("offset/title").GetComponent<Text>());
            EditorThemeManager.AddLightText(prefabEditorData.Find("type/title").GetComponent<Text>());
            EditorThemeManager.AddInputField(prefabEditorData.Find("name/input").GetComponent<InputField>());

            Destroy(prefabEditorData.Find("offset/<").gameObject);
            Destroy(prefabEditorData.Find("offset/>").gameObject);

            var offsetSlider = prefabEditorData.Find("offset/slider").GetComponent<Slider>();
            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Slider_2, offsetSlider.transform.Find("Background").gameObject, new List<Component>
            {
                offsetSlider.transform.Find("Background").GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.W));

            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Slider_2_Handle, offsetSlider.gameObject, new List<Component>
            {
                offsetSlider.image,
            }, true, 1, SpriteManager.RoundedSide.W));
            EditorThemeManager.AddInputField(prefabEditorData.Find("offset/input").GetComponent<InputField>());

            var prefabType = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/event")
                .Duplicate(prefabEditorData.Find("type"), "Show Type Editor");

            Destroy(prefabEditorData.Find("type/types").gameObject);

            ((RectTransform)prefabType.transform).sizeDelta = new Vector2(132f, 34f);
            var prefabTypeText = prefabType.transform.Find("Text").GetComponent<Text>();
            prefabTypeText.text = "Open Prefab Type Editor";
            var prefabTypeButton = prefabType.GetComponent<Button>();
            prefabTypeButton.onClick.ClearAll();
            prefabTypeButton.onClick.AddListener(() =>
            {
                RTPrefabEditor.inst.OpenPrefabTypePopup(PrefabEditor.inst.NewPrefabType, index =>
                {
                    PrefabEditor.inst.NewPrefabType = index;
                    if (PrefabEditor.inst.dialog)
                        PrefabEditor.inst.dialog.Find("data/type/Show Type Editor").GetComponent<Image>().color =
                            DataManager.inst.PrefabTypes[Mathf.Clamp(PrefabEditor.inst.NewPrefabType, 0, DataManager.inst.PrefabTypes.Count - 1)].Color;
                });
            });

            prefabType.AddComponent<ContrastColors>().Init(prefabTypeText, prefabTypeButton.image);
            EditorThemeManager.AddGraphic(prefabTypeButton.image, ThemeGroup.Null, true);

            ((RectTransform)prefabEditorData.Find("spacer")).sizeDelta = new Vector2(749f, 32f);
            ((RectTransform)prefabEditorData.Find("type")).sizeDelta = new Vector2(749f, 48f);

            var descriptionGO = prefabEditorData.Find("name").gameObject.Duplicate(prefabEditorData, "description", 4);
            ((RectTransform)descriptionGO.transform).sizeDelta = new Vector2(749f, 108f);
            var descriptionTitle = descriptionGO.transform.Find("title").GetComponent<Text>();
            descriptionTitle.text = "Desc";
            EditorThemeManager.AddLightText(descriptionTitle);
            var descriptionInputField = descriptionGO.transform.Find("input").GetComponent<InputField>();
            ((Text)descriptionInputField.placeholder).alignment = TextAnchor.UpperLeft;
            ((Text)descriptionInputField.placeholder).text = "Enter description...";
            EditorThemeManager.AddInputField(descriptionInputField);

            var selection = prefabEditorData.Find("selection");
            selection.gameObject.SetActive(true);
            selection.AsRT().sizeDelta = new Vector2(749f, 300f);
            var search = selection.Find("search-box/search").GetComponent<InputField>();
            search.onValueChanged.ClearAll();
            search.onValueChanged.AddListener(_val => { RTPrefabEditor.inst.ReloadSelectionContent(); });

            EditorThemeManager.AddInputField(search, ThemeGroup.Search_Field_2);
            var selectionGroup = selection.Find("mask/content").GetComponent<GridLayoutGroup>();
            selectionGroup.cellSize = new Vector2(172.5f, 32f);
            selectionGroup.constraintCount = 4;

            EditorThemeManager.AddGraphic(selection.GetComponent<Image>(), ThemeGroup.Background_3, true);

            EditorThemeManager.AddGraphic(Instance.dialog.Find("submit/submit").GetComponent<Image>(), ThemeGroup.Add, true);
            EditorThemeManager.AddGraphic(Instance.dialog.Find("submit/submit/Text").GetComponent<Text>(), ThemeGroup.Add_Text);

            var scrollbar = selection.Find("scrollbar").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Scrollbar_2, scrollbar, new List<Component>
            {
                scrollbar.GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.W));

            var scrollbarHandle = scrollbar.transform.Find("sliding_area/Handle").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Scrollbar_2_Handle, scrollbarHandle, new List<Component>
            {
                scrollbarHandle.GetComponent<Image>(),
                scrollbar.GetComponent<Scrollbar>()
            }, true, 1, SpriteManager.RoundedSide.W, true));

            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.Update))]
        [HarmonyPrefix]
        static bool UpdatePrefix()
        {
            if (Instance.dialog && Instance.dialog.gameObject.activeSelf)
            {
                float num;
                if (ObjectEditor.inst.SelectedObjects.Count <= 0)
                    num = 0f;
                else
                    num = ObjectEditor.inst.SelectedObjects.Min(x => x.Time);

                if (!Instance.OffsetLine.activeSelf && ObjectEditor.inst.SelectedObjects.Count > 0)
                {
                    Instance.OffsetLine.transform.SetAsLastSibling();
                    Instance.OffsetLine.SetActive(true);
                }
                ((RectTransform)Instance.OffsetLine.transform).anchoredPosition = new Vector2(Instance.posCalc(num - Instance.NewPrefabOffset), 0f);
            }
            if (((!Instance.dialog || !Instance.dialog.gameObject.activeSelf) || ObjectEditor.inst.SelectedBeatmapObjects.Count <= 0) && Instance.OffsetLine.activeSelf)
            {
                Instance.OffsetLine.SetActive(false);
            }
            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.CreateNewPrefab))]
        [HarmonyPrefix]
        static bool CreateNewPrefabPrefix()
        {
            RTPrefabEditor.inst.CreateNewPrefab();
            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.SavePrefab))]
        [HarmonyPrefix]
        static bool SavePrefabPrefix(BasePrefab __0)
        {
            RTPrefabEditor.inst.SavePrefab((Prefab)__0);
            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.DeleteExternalPrefab))]
        [HarmonyPrefix]
        static bool DeleteExternalPrefabPrefix(int __0) => false;

        [HarmonyPatch(nameof(PrefabEditor.DeleteInternalPrefab))]
        [HarmonyPrefix]
        static bool DeleteInternalPrefabPrefix(int __0)
        {
            RTPrefabEditor.inst.DeleteInternalPrefab(__0);
            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.ExpandCurrentPrefab))]
        [HarmonyPrefix]
        static bool ExpandCurrentPrefabPrefix()
        {
            RTPrefabEditor.inst.ExpandCurrentPrefab();
            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.CollapseCurrentPrefab))]
        [HarmonyPrefix]
        static bool CollapseCurrentPrefabPrefix()
        {
            if (EditorConfig.Instance.ShowCollapsePrefabWarning.Value)
            {
                RTEditor.inst.ShowWarningPopup("Are you sure you want to collapse this Prefab group and save the changes to the Internal Prefab?", () =>
                {
                    RTPrefabEditor.inst.CollapseCurrentPrefab();
                    EditorManager.inst.HideDialog("Warning Popup");
                }, () => { EditorManager.inst.HideDialog("Warning Popup"); });

                return false;
            }

            RTPrefabEditor.inst.CollapseCurrentPrefab();
            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.ReloadExternalPrefabsInPopup))]
        [HarmonyPostfix]
        static void ReloadExternalPrefabsInPopupPostfix()
        {
            CoreHelper.Log($"Run patch: {nameof(ReloadExternalPrefabsInPopupPostfix)}");

            //Internal Config
            {
                var internalPrefab = PrefabEditor.inst.internalPrefabDialog;

                var internalPrefabGLG = internalPrefab.Find("mask/content").GetComponent<GridLayoutGroup>();

                internalPrefabGLG.spacing = EditorConfig.Instance.PrefabInternalSpacing.Value;
                internalPrefabGLG.cellSize = EditorConfig.Instance.PrefabInternalCellSize.Value;
                internalPrefabGLG.constraint = EditorConfig.Instance.PrefabInternalConstraintMode.Value;
                internalPrefabGLG.constraintCount = EditorConfig.Instance.PrefabInternalConstraint.Value;
                internalPrefabGLG.startAxis = EditorConfig.Instance.PrefabInternalStartAxis.Value;

                internalPrefab.AsRT().anchoredPosition = EditorConfig.Instance.PrefabInternalPopupPos.Value;
                internalPrefab.AsRT().sizeDelta = EditorConfig.Instance.PrefabInternalPopupSize.Value;

                internalPrefab.GetComponent<ScrollRect>().horizontal = EditorConfig.Instance.PrefabInternalHorizontalScroll.Value;
            }

            //External Config
            {
                var externalPrefab = PrefabEditor.inst.externalPrefabDialog;

                var externalPrefabGLG = externalPrefab.Find("mask/content").GetComponent<GridLayoutGroup>();

                externalPrefabGLG.spacing = EditorConfig.Instance.PrefabExternalSpacing.Value;
                externalPrefabGLG.cellSize = EditorConfig.Instance.PrefabExternalCellSize.Value;
                externalPrefabGLG.constraint = EditorConfig.Instance.PrefabExternalConstraintMode.Value;
                externalPrefabGLG.constraintCount = EditorConfig.Instance.PrefabExternalConstraint.Value;
                externalPrefabGLG.startAxis = EditorConfig.Instance.PrefabExternalStartAxis.Value;

                externalPrefab.AsRT().anchoredPosition = EditorConfig.Instance.PrefabExternalPopupPos.Value;
                externalPrefab.AsRT().sizeDelta = EditorConfig.Instance.PrefabExternalPopupSize.Value;

                externalPrefab.GetComponent<ScrollRect>().horizontal = EditorConfig.Instance.PrefabExternalHorizontalScroll.Value;
            }
        }

        [HarmonyPatch(nameof(PrefabEditor.ReloadExternalPrefabsInPopup))]
        [HarmonyPrefix]
        static bool ReloadExternalPrefabsInPopupPrefix(bool __0)
        {
            if (Instance.externalPrefabDialog == null || Instance.externalSearch == null || Instance.externalContent == null)
            {
                Debug.LogErrorFormat("External Prefabs Error: \n{0}\n{1}\n{2}", Instance.externalPrefabDialog, Instance.externalSearch, Instance.externalContent);
            }
            Debug.Log("Loading External Prefabs Popup");
            RTEditor.inst.StartCoroutine(RTPrefabEditor.inst.ExternalPrefabFiles(__0));
            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.ReloadInternalPrefabsInPopup))]
        [HarmonyPrefix]
        static bool ReloadInternalPrefabsInPopupPrefix(bool __0)
        {
            if (Instance.internalPrefabDialog == null || Instance.internalSearch == null || Instance.internalContent == null)
            {
                Debug.LogErrorFormat("Internal Prefabs Error: \n{0}\n{1}\n{2}", Instance.internalPrefabDialog, Instance.internalSearch, Instance.internalContent);
            }
            Debug.Log("Loading Internal Prefabs Popup");
            RTEditor.inst.StartCoroutine(RTPrefabEditor.inst.InternalPrefabs(__0));
            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.LoadExternalPrefabs))]
        [HarmonyPrefix]
        static bool LoadExternalPrefabsPrefix(PrefabEditor __instance, ref IEnumerator __result)
        {
            __result = RTEditor.inst.LoadPrefabs(__instance);
            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.OpenPrefabDialog))]
        [HarmonyPrefix]
        static bool OpenPrefabDialogPrefix()
        {
            EditorManager.inst.ClearDialogs();

            bool isPrefab = ObjectEditor.inst.CurrentSelection != null && ObjectEditor.inst.CurrentSelection.Data != null && ObjectEditor.inst.CurrentSelection.IsPrefabObject;
            if (!isPrefab)
            {
                Debug.LogError($"{Instance.className}Cannot select non-Prefab with this editor!");
                EditorManager.inst.ShowDialog("Object Editor", false);
                return false;
            }

            EditorManager.inst.ShowDialog("Prefab Selector");
            RTPrefabEditor.inst.RenderPrefabObjectDialog(ObjectEditor.inst.CurrentSelection.GetData<PrefabObject>());

            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.OpenDialog))]
        [HarmonyPrefix]
        static bool OpenDialogPrefix()
        {
            RTPrefabEditor.inst.OpenDialog();

            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.OpenPopup))]
        [HarmonyPrefix]
        static bool OpenPopupPrefix()
        {
            RTPrefabEditor.inst.OpenPopup();

            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.ImportPrefabIntoLevel))]
        [HarmonyPrefix]
        static bool ImportPrefabIntoLevelPrefix(PrefabEditor __instance, BasePrefab __0)
        {
            CoreHelper.Log($"Adding Prefab [{__0.Name}]");

            var tmpPrefab = Prefab.DeepCopy((Prefab)__0);
            int num = DataManager.inst.gameData.prefabs.FindAll(x => Regex.Replace(x.Name, "( +\\[\\d+])", string.Empty) == tmpPrefab.Name).Count();
            if (num > 0)
                tmpPrefab.Name = $"{tmpPrefab.Name}[{num}]";

            DataManager.inst.gameData.prefabs.Add(tmpPrefab);
            __instance.ReloadInternalPrefabsInPopup();

            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.AddPrefabObjectToLevel))]
        [HarmonyPrefix]
        static bool AddPrefabObjectToLevelPrefix(BasePrefab __0)
        {
            RTPrefabEditor.inst.AddPrefabObjectToLevel(__0);
            return false;
        }
    }
}
