using BetterLegacy.Components;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor;
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

        [HarmonyPatch("Awake")]
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

                var b = new GameObject("type");
                b.transform.SetParent(type);
                b.transform.localScale = Vector3.one;

                var bRT = b.AddComponent<RectTransform>();
                bRT.anchoredPosition = Vector2.zero;
                bRT.sizeDelta = new Vector2(28f, 28f);

                var bImage = b.AddComponent<Image>();
                bImage.color = new Color(0f, 0f, 0f, 0.45f);

                var icon = new GameObject("type");
                icon.transform.SetParent(bRT);
                icon.transform.localScale = Vector3.one;

                var iconRT = icon.AddComponent<RectTransform>();
                iconRT.anchoredPosition = Vector2.zero;
                iconRT.sizeDelta = new Vector2(28f, 28f);

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

            PrefabEditorManager.Init(__instance);

            return false;
        }

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static bool StartPrefix()
        {
            PrefabEditorManager.loadingPrefabTypes = true;
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

            if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/type/types", out GameObject gm) && gm.TryGetComponent(out VerticalLayoutGroup component))
            {
                Destroy(component);
            }

            var dialog = EditorManager.inst.GetDialog("Prefab Selector").Dialog;
            PrefabEditorManager.inst.prefabSelectorLeft = dialog.Find("data/left");
            PrefabEditorManager.inst.prefabSelectorRight = dialog.Find("data/right");

            var contentBase = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content");
            var scrollView = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View").Duplicate(PrefabEditorManager.inst.prefabSelectorLeft);

            var parent = scrollView.transform.Find("Viewport/Content");

            LSHelpers.DeleteChildren(parent, true);

            var objectsToDelete = new List<GameObject>();
            for (int i = 0; i < parent.childCount; i++)
                objectsToDelete.Add(parent.GetChild(i).gameObject);
            foreach (var child in objectsToDelete)
                DestroyImmediate(child);

            scrollView.transform.AsRT().sizeDelta = new Vector2(383f, 690f);

            var objectsToParent = new List<Transform>();
            for (int i = 0; i < PrefabEditorManager.inst.prefabSelectorLeft.childCount; i++)
                objectsToParent.Add(PrefabEditorManager.inst.prefabSelectorLeft.GetChild(i));
            foreach (var child in objectsToParent)
                child.SetParent(parent);

            PrefabEditorManager.inst.prefabSelectorLeft = parent;

            EditorHelper.LogAvailableInstances<PrefabEditor>();

            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Background_1, dialog.gameObject, new List<Component>
            {
                dialog.GetComponent<Image>(),
            }));

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

            DestroyImmediate(PrefabEditorManager.inst.prefabSelectorLeft.GetChild(4).gameObject);
            DestroyImmediate(PrefabEditorManager.inst.prefabSelectorLeft.GetChild(4).gameObject);

            Action<Transform, string, string> labelGenerator = delegate (Transform parent, string name, string x)
            {
                var label = labelTemp.Duplicate(parent, $"{name.ToLower()} label");
                var labelText = label.transform.GetChild(0).GetComponent<Text>();
                labelText.text = x;
                Destroy(label.transform.GetChild(1).gameObject);

                EditorThemeManager.AddLightText(labelText);
            };

            Action<Transform, string, string, string> labelGenerator2 = delegate (Transform parent, string name, string x, string y)
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
            labelGenerator(PrefabEditorManager.inst.prefabSelectorLeft, "tod-dropdown", "Time of Death");

            var autoKillType = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown")
                .Duplicate(PrefabEditorManager.inst.prefabSelectorLeft, "tod-dropdown", 14);
            var autoKillTypeDD = autoKillType.GetComponent<Dropdown>();
            autoKillType.GetComponent<Dropdown>().options = new List<Dropdown.OptionData>
            {
                new Dropdown.OptionData("Regular"),
                new Dropdown.OptionData("Start Offset"),
                new Dropdown.OptionData("Song Time"),
            };
            autoKillType.GetComponent<HideDropdownOptions>().DisabledOptions = new List<bool>
            {
                false,
                false,
                false,
            };

            EditorThemeManager.AddDropdown(autoKillTypeDD);

            var ako = singleInput.Duplicate(PrefabEditorManager.inst.prefabSelectorLeft, "akoffset");
            EditorThemeManager.AddInputField(ako.GetComponent<InputField>());
            EditorThemeManager.AddSelectable(ako.transform.Find("<").GetComponent<Button>(), ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(ako.transform.Find(">").GetComponent<Button>(), ThemeGroup.Function_2, false);

            var setToCurrent = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/|").Duplicate(PrefabEditorManager.inst.prefabSelectorLeft.Find("akoffset"), "|");

            var setToCurrentButton = setToCurrent.GetComponent<Button>();
            Destroy(setToCurrent.GetComponent<Animator>());
            setToCurrentButton.transition = Selectable.Transition.ColorTint;

            EditorThemeManager.AddSelectable(setToCurrentButton, ThemeGroup.Function_2, false);

            // Parent
            var parentUI = contentBase.transform.Find("parent").gameObject.Duplicate(PrefabEditorManager.inst.prefabSelectorLeft, "parent");
            var parent_more = contentBase.transform.Find("parent_more").gameObject.Duplicate(PrefabEditorManager.inst.prefabSelectorLeft, "parent_more");

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
            labelGenerator(PrefabEditorManager.inst.prefabSelectorLeft, "time", "Time");

            var time = EditorPrefabHolder.Instance.NumberInputField.Duplicate(PrefabEditorManager.inst.prefabSelectorLeft, "time");
            var timeStorage = time.GetComponent<InputFieldStorage>();
            EditorThemeManager.AddInputField(timeStorage.inputField);
            timeStorage.inputField.transform.AsRT().sizeDelta = new Vector2(135f, 32f);

            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Function_2, timeStorage.leftGreaterButton.gameObject, new List<Component>
            {
                timeStorage.leftGreaterButton.image,
                timeStorage.leftGreaterButton,
            }, isSelectable: true));

            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Function_2, timeStorage.leftButton.gameObject, new List<Component>
            {
                timeStorage.leftButton.image,
                timeStorage.leftButton,
            }, isSelectable: true));

            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Function_2, timeStorage.rightButton.gameObject, new List<Component>
            {
                timeStorage.rightButton.image,
                timeStorage.rightButton,
            }, isSelectable: true));

            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Function_2, timeStorage.middleButton.gameObject, new List<Component>
            {
                timeStorage.middleButton.image,
                timeStorage.middleButton,
            }, isSelectable: true));

            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Function_2, timeStorage.rightGreaterButton.gameObject, new List<Component>
            {
                timeStorage.rightGreaterButton.image,
                timeStorage.rightGreaterButton,
            }, isSelectable: true));

            var timeParent = time.transform;

            var locker = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle/toggle").Duplicate(timeParent, "lock", 0);

            locker.transform.Find("Background/Checkmark").GetComponent<Image>().sprite = ObjEditor.inst.timelineObjectPrefabLock.transform.Find("lock (1)").GetComponent<Image>().sprite;

            EditorThemeManager.AddToggle(locker.GetComponent<Toggle>());

            var collapser = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/collapse").Duplicate(timeParent, "collapse", 1);

            EditorThemeManager.AddToggle(collapser.GetComponent<Toggle>(), ThemeGroup.Background_1);

            for (int i = 0; i < collapser.transform.Find("dots").childCount; i++)
            {
                var dot = collapser.transform.Find("dots").GetChild(i).GetComponent<Image>();
                EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Dark_Text, dot.gameObject, new List<Component>
                {
                    dot,
                }));
            }

            // Position
            labelGenerator2(PrefabEditorManager.inst.prefabSelectorLeft, "pos", "Position X Offset", "Position Y Offset");

            var position = vector2Input.Duplicate(PrefabEditorManager.inst.prefabSelectorLeft, "position");
            var positionX = position.transform.Find("x").gameObject.AddComponent<InputFieldSwapper>();
            positionX.Init(position.transform.Find("x").GetComponent<InputField>());
            var positionY = position.transform.Find("y").gameObject.AddComponent<InputFieldSwapper>();
            positionY.Init(position.transform.Find("y").GetComponent<InputField>());
            EditorThemeManager.AddInputFields(position, true, "");

            // Scale
            labelGenerator2(PrefabEditorManager.inst.prefabSelectorLeft, "sca", "Scale X Offset", "Scale Y Offset");

            var scale = vector2Input.Duplicate(PrefabEditorManager.inst.prefabSelectorLeft, "scale");
            var scaleX = scale.transform.Find("x").gameObject.AddComponent<InputFieldSwapper>();
            scaleX.Init(scale.transform.Find("x").GetComponent<InputField>());
            var scaleY = scale.transform.Find("y").gameObject.AddComponent<InputFieldSwapper>();
            scaleY.Init(scale.transform.Find("y").GetComponent<InputField>());
            EditorThemeManager.AddInputFields(scale, true, "");

            // Rotation
            labelGenerator(PrefabEditorManager.inst.prefabSelectorLeft, "rot", "Rotation Offset");

            var rot = vector2Input.Duplicate(PrefabEditorManager.inst.prefabSelectorLeft, "rotation");
            Destroy(rot.transform.GetChild(1).gameObject);
            var rotX = rot.transform.Find("x").gameObject.AddComponent<InputFieldSwapper>();
            rotX.Init(rot.transform.Find("x").GetComponent<InputField>());
            EditorThemeManager.AddInputFields(rot, true, "");

            // Repeat
            labelGenerator2(PrefabEditorManager.inst.prefabSelectorLeft, "repeat", "Repeat Count", "Repeat Offset Time");

            var repeat = vector2Input.Duplicate(PrefabEditorManager.inst.prefabSelectorLeft, "repeat");
            EditorThemeManager.AddInputFields(repeat, true, "");

            // Speed
            labelGenerator(PrefabEditorManager.inst.prefabSelectorLeft, "speed", "Speed");

            var speed = singleInput.Duplicate(PrefabEditorManager.inst.prefabSelectorLeft, "speed");
            EditorThemeManager.AddInputField(speed.GetComponent<InputField>());
            EditorThemeManager.AddSelectable(speed.transform.Find("<").GetComponent<Button>(), ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(speed.transform.Find(">").GetComponent<Button>(), ThemeGroup.Function_2, false);

            // Layers
            var layersIF = singleInput.Duplicate(PrefabEditorManager.inst.prefabSelectorLeft.Find("editor"), "layers", 0).GetComponent<InputField>();
            layersIF.gameObject.AddComponent<ContrastColors>().Init(layersIF.textComponent, layersIF.image);
            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Null, layersIF.gameObject, new List<Component> { layersIF }, true, 1, SpriteManager.RoundedSide.W));
            EditorThemeManager.AddSelectable(layersIF.transform.Find("<").GetComponent<Button>(), ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(layersIF.transform.Find(">").GetComponent<Button>(), ThemeGroup.Function_2, false);

            // Name
            labelGenerator(PrefabEditorManager.inst.prefabSelectorRight, "name", "Name");

            var prefabName = RTEditor.inst.defaultIF.Duplicate(PrefabEditorManager.inst.prefabSelectorRight, "name");
            prefabName.transform.localScale = Vector3.one;

            prefabName.GetComponentAndPerformAction(delegate (InputField inputField)
            {
                inputField.characterValidation = InputField.CharacterValidation.None;
                inputField.contentType = InputField.ContentType.Standard;
                inputField.characterLimit = 0;
                PrefabEditorManager.inst.nameIF = inputField;
                EditorThemeManager.AddInputField(inputField);
            });

            // Type
            labelGenerator(PrefabEditorManager.inst.prefabSelectorRight, "type", "Type");

            var type = singleInput.Duplicate(PrefabEditorManager.inst.prefabSelectorRight, "type");

            type.GetComponentAndPerformAction(delegate (InputField inputField)
            {
                PrefabEditorManager.inst.typeImage = inputField.image;
                inputField.characterValidation = InputField.CharacterValidation.None;
                inputField.contentType = InputField.ContentType.Standard;
                PrefabEditorManager.inst.typeIF = inputField;
                inputField.gameObject.AddComponent<ContrastColors>().Init(inputField.textComponent, inputField.image);

                EditorThemeManager.AddSelectable(type.transform.Find("<").GetComponent<Button>(), ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(type.transform.Find(">").GetComponent<Button>(), ThemeGroup.Function_2, false);
            });

            var expandPrefabLabel = PrefabEditorManager.inst.prefabSelectorLeft.GetChild(0).gameObject;
            var expandPrefabLabelText = expandPrefabLabel.transform.GetChild(0).GetComponent<Text>();
            var expandPrefab = PrefabEditorManager.inst.prefabSelectorLeft.GetChild(1).gameObject;
            var expandPrefabButton = expandPrefab.GetComponent<Button>();
            var expandPrefabText = expandPrefab.transform.GetChild(0).GetComponent<Text>();
            EditorThemeManager.AddLightText(expandPrefabLabelText);
            Destroy(expandPrefab.GetComponent<Animator>());
            expandPrefabButton.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.AddSelectable(expandPrefabButton, ThemeGroup.Function_2);
            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Function_2_Text, expandPrefabText.gameObject, new List<Component>
            {
                expandPrefabText
            }));

            // Save Prefab
            var label = expandPrefabLabel.Duplicate(PrefabEditorManager.inst.prefabSelectorRight, "save prefab label");
            label.transform.localScale = Vector3.one;
            var applyToAllText = label.transform.GetChild(0).GetComponent<Text>();
            applyToAllText.fontSize = 19;
            applyToAllText.text = "Apply to an External Prefab";

            var savePrefab = expandPrefab.Duplicate(PrefabEditorManager.inst.prefabSelectorRight, "save prefab");
            savePrefab.transform.localScale = Vector3.one;
            var savePrefabText = savePrefab.transform.GetChild(0).GetComponent<Text>();
            savePrefabText.text = "Select Prefab";

            EditorThemeManager.AddLightText(applyToAllText);
            var savePrefabButton = savePrefab.GetComponent<Button>();
            Destroy(savePrefab.GetComponent<Animator>());
            savePrefabButton.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.AddSelectable(savePrefabButton, ThemeGroup.Function_2);
            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Function_2_Text, savePrefabText.gameObject, new List<Component>
            {
                savePrefabText
            }));

            Action<string, string, Action<Text, string>> countGenerator = delegate (string name, string count, Action<Text, string> text)
            {
                var rotLabel = labelTemp.Duplicate(PrefabEditorManager.inst.prefabSelectorRight, name);

                Destroy(rotLabel.transform.GetChild(1).gameObject);

                text(rotLabel.transform.GetChild(0).GetComponent<Text>(), count);
            };

            // Object Count
            countGenerator("count label", "Object Count: 0", delegate (Text text, string count)
            {
                PrefabEditorManager.inst.objectCount = text;
                PrefabEditorManager.inst.objectCount.text = count;

                EditorThemeManager.AddLightText(text);
            });

            // Prefab Object Count
            countGenerator("count label", "Prefab Object Count: 0", delegate (Text text, string count)
            {
                PrefabEditorManager.inst.prefabObjectCount = text;
                PrefabEditorManager.inst.prefabObjectCount.text = count;

                EditorThemeManager.AddLightText(text);
            });

            // Prefab Object Timeline Count
            countGenerator("count label", "Prefab Object (Timeline) Count: 0", delegate (Text text, string count)
            {
                PrefabEditorManager.inst.prefabObjectTimelineCount = text;
                PrefabEditorManager.inst.prefabObjectTimelineCount.text = count;

                EditorThemeManager.AddLightText(text);
            });

            DestroyImmediate(PrefabEditorManager.inst.prefabSelectorRight.Find("time").gameObject);
            var offsetTime = EditorPrefabHolder.Instance.NumberInputField.Duplicate(PrefabEditorManager.inst.prefabSelectorRight, "time", 1);
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

            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Light_Text, prefabEditorData.Find("title/Panel/icon").gameObject, new List<Component>
            {
                prefabEditorData.Find("title/Panel/icon").GetComponent<Image>(),
            }));
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
            prefabTypeButton.onClick.AddListener(delegate ()
            {
                PrefabEditorManager.inst.OpenPrefabTypePopup(PrefabEditor.inst.NewPrefabType, delegate (int index)
                {
                    PrefabEditor.inst.NewPrefabType = index;
                    if (PrefabEditor.inst.dialog)
                        PrefabEditor.inst.dialog.Find("data/type/Show Type Editor").GetComponent<Image>().color =
                            DataManager.inst.PrefabTypes[Mathf.Clamp(PrefabEditor.inst.NewPrefabType, 0, DataManager.inst.PrefabTypes.Count - 1)].Color;
                });
            });
            prefabType.AddComponent<ContrastColors>().Init(prefabTypeText, prefabTypeButton.image);
            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Null, prefabType, new List<Component>
            {
                prefabTypeButton.image,
            }, true, 1, SpriteManager.RoundedSide.W));

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
            search.onValueChanged.AddListener(delegate (string _val)
            {
                PrefabEditorManager.inst.ReloadSelectionContent();
            });

            EditorThemeManager.AddInputField(search, ThemeGroup.Search_Field_2);
            var selectionGroup = selection.Find("mask/content").GetComponent<GridLayoutGroup>();
            selectionGroup.cellSize = new Vector2(172.5f, 32f);
            selectionGroup.constraintCount = 4;

            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Background_3, selection.gameObject, new List<Component>
            {
                selection.GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.W));

            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Add, Instance.dialog.Find("submit/submit").gameObject, new List<Component>
            {
                Instance.dialog.Find("submit/submit").GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.W));

            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Add_Text, Instance.dialog.Find("submit/submit/Text").gameObject, new List<Component>
            {
                Instance.dialog.Find("submit/submit/Text").GetComponent<Text>(),
            }));

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

        [HarmonyPatch("Update")]
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

        [HarmonyPatch("CreateNewPrefab")]
        [HarmonyPrefix]
        static bool CreateNewPrefabPrefix()
        {
            PrefabEditorManager.inst.CreateNewPrefab();
            return false;
        }

        [HarmonyPatch("SavePrefab")]
        [HarmonyPrefix]
        static bool SavePrefabPrefix(BasePrefab __0)
        {
            PrefabEditorManager.inst.SavePrefab((Prefab)__0);
            return false;
        }

        [HarmonyPatch("DeleteExternalPrefab")]
        [HarmonyPrefix]
        static bool DeleteExternalPrefabPrefix(int __0)
        {
            return false;
        }

        [HarmonyPatch("DeleteInternalPrefab")]
        [HarmonyPrefix]
        static bool DeleteInternalPrefabPrefix(int __0)
        {
            PrefabEditorManager.inst.DeleteInternalPrefab(__0);
            return false;
        }

        [HarmonyPatch("ExpandCurrentPrefab")]
        [HarmonyPrefix]
        static bool ExpandCurrentPrefabPrefix()
        {
            PrefabEditorManager.inst.ExpandCurrentPrefab();
            return false;
        }

        [HarmonyPatch("CollapseCurrentPrefab")]
        [HarmonyPrefix]
        static bool CollapseCurrentPrefabPrefix()
        {
            if (EditorConfig.Instance.ShowCollapsePrefabWarning.Value)
            {
                RTEditor.inst.ShowWarningPopup("Are you sure you want to collapse this Prefab group and save the changes to the Internal Prefab?", delegate ()
                {
                    PrefabEditorManager.inst.CollapseCurrentPrefab();
                    EditorManager.inst.HideDialog("Warning Popup");
                }, delegate ()
                {
                    EditorManager.inst.HideDialog("Warning Popup");
                });

                return false;
            }

            PrefabEditorManager.inst.CollapseCurrentPrefab();
            return false;
        }

        [HarmonyPatch("ReloadExternalPrefabsInPopup")]
        [HarmonyPostfix]
        static void SetPopupSizesPostfix()
        {
            //Internal Config
            {
                var internalPrefab = PrefabEditor.inst.internalPrefabDialog;

                internalPrefab.Find("mask/content").GetComponentAndPerformAction(delegate (GridLayoutGroup x)
                {
                    x.spacing = EditorConfig.Instance.PrefabInternalSpacing.Value;
                    x.cellSize = EditorConfig.Instance.PrefabInternalCellSize.Value;
                    x.constraint = EditorConfig.Instance.PrefabInternalConstraintMode.Value;
                    x.constraintCount = EditorConfig.Instance.PrefabInternalConstraint.Value;
                    x.startAxis = EditorConfig.Instance.PrefabInternalStartAxis.Value;
                });

                internalPrefab.AsRT().anchoredPosition = EditorConfig.Instance.PrefabInternalPopupPos.Value;
                internalPrefab.AsRT().sizeDelta = EditorConfig.Instance.PrefabInternalPopupSize.Value;

                internalPrefab.GetComponent<ScrollRect>().horizontal = EditorConfig.Instance.PrefabInternalHorizontalScroll.Value;
            }

            //External Config
            {
                var externalPrefab = PrefabEditor.inst.externalPrefabDialog;

                externalPrefab.Find("mask/content").GetComponentAndPerformAction(delegate (GridLayoutGroup x)
                {
                    x.spacing = EditorConfig.Instance.PrefabExternalSpacing.Value;
                    x.cellSize = EditorConfig.Instance.PrefabExternalCellSize.Value;
                    x.constraint = EditorConfig.Instance.PrefabExternalConstraintMode.Value;
                    x.constraintCount = EditorConfig.Instance.PrefabExternalConstraint.Value;
                    x.startAxis = EditorConfig.Instance.PrefabExternalStartAxis.Value;
                });

                externalPrefab.AsRT().anchoredPosition = EditorConfig.Instance.PrefabExternalPopupPos.Value;
                externalPrefab.AsRT().sizeDelta = EditorConfig.Instance.PrefabExternalPopupSize.Value;

                externalPrefab.GetComponent<ScrollRect>().horizontal = EditorConfig.Instance.PrefabExternalHorizontalScroll.Value;
            }
        }

        [HarmonyPatch("ReloadExternalPrefabsInPopup")]
        [HarmonyPrefix]
        static bool ReloadExternalPrefabsInPopupPatch(bool __0)
        {
            if (Instance.externalPrefabDialog == null || Instance.externalSearch == null || Instance.externalContent == null)
            {
                Debug.LogErrorFormat("External Prefabs Error: \n{0}\n{1}\n{2}", Instance.externalPrefabDialog, Instance.externalSearch, Instance.externalContent);
            }
            Debug.Log("Loading External Prefabs Popup");
            RTEditor.inst.StartCoroutine(PrefabEditorManager.inst.ExternalPrefabFiles(__0));
            return false;
        }

        [HarmonyPatch("ReloadInternalPrefabsInPopup")]
        [HarmonyPrefix]
        static bool ReloadInternalPrefabsInPopupPatch(bool __0)
        {
            if (Instance.internalPrefabDialog == null || Instance.internalSearch == null || Instance.internalContent == null)
            {
                Debug.LogErrorFormat("Internal Prefabs Error: \n{0}\n{1}\n{2}", Instance.internalPrefabDialog, Instance.internalSearch, Instance.internalContent);
            }
            Debug.Log("Loading Internal Prefabs Popup");
            RTEditor.inst.StartCoroutine(PrefabEditorManager.inst.InternalPrefabs(__0));
            return false;
        }

        [HarmonyPatch("LoadExternalPrefabs")]
        [HarmonyPrefix]
        static bool LoadExternalPrefabsPrefix(PrefabEditor __instance, ref IEnumerator __result)
        {
            __result = RTEditor.inst.LoadPrefabs(__instance);
            return false;
        }

        [HarmonyPatch("OpenPrefabDialog")]
        [HarmonyPrefix]
        static bool OpenPrefabDialogPrefix(PrefabEditor __instance)
        {
            EditorManager.inst.ClearDialogs();

            bool isPrefab = ObjectEditor.inst.CurrentSelection != null && ObjectEditor.inst.CurrentSelection.Data != null && ObjectEditor.inst.CurrentSelection.IsPrefabObject;
            if (!isPrefab)
            {
                Debug.LogError($"{__instance.className}Cannot select non-Prefab with this editor!");
                EditorManager.inst.ShowDialog("Object Editor", false);
                return false;
            }

            EditorManager.inst.ShowDialog("Prefab Selector");
            PrefabEditorManager.inst.RenderPrefabObjectDialog(ObjectEditor.inst.CurrentSelection.GetData<PrefabObject>());

            return false;
        }

        [HarmonyPatch("OpenDialog")]
        [HarmonyPrefix]
        static bool OpenDialogPrefix()
        {
            PrefabEditorManager.inst.OpenDialog();

            return false;
        }

        [HarmonyPatch("OpenPopup")]
        [HarmonyPrefix]
        static bool OpenPopupPrefix()
        {
            PrefabEditorManager.inst.OpenPopup();

            return false;
        }

        [HarmonyPatch("ImportPrefabIntoLevel")]
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

        [HarmonyPatch("AddPrefabObjectToLevel")]
        [HarmonyPrefix]
        static bool AddPrefabObjectToLevel(BasePrefab __0)
        {
            PrefabEditorManager.inst.AddPrefabObjectToLevel(__0);
            return false;
        }
    }
}
