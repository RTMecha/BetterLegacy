using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class PrefabObjectEditorDialog : EditorDialog
    {
        public PrefabObjectEditorDialog() : base(PREFAB_SELECTOR) { }

        #region Instance Values

        public RectTransform Left { get; set; }

        public RectTransform LeftContent { get; set; }

        #region Start Time / Autokill

        public InputFieldStorage StartTimeField { get; set; }

        public Dropdown AutokillDropdown { get; set; }
        public InputFieldStorage AutokillField { get; set; }
        public Toggle CollapseToggle { get; set; }

        #endregion

        #region Editor

        public RectTransform EditorSettingsParent { get; set; }
        public InputField EditorLayerField { get; set; }
        public Slider BinSlider { get; set; }
        public Toggle[] EditorLayerToggles { get; set; }

        public InputFieldStorage EditorIndexField { get; set; }

        #endregion

        #region Transforms

        public InputFieldStorage[][] TransformFields { get; set; } = new InputFieldStorage[3][];
        public InputFieldStorage[][] RandomTransformFields { get; set; } = new InputFieldStorage[3][];
        public Toggle[] PositionRandomToggles { get; set; }
        public Toggle[] ScaleRandomToggles { get; set; }
        public Toggle[] RotationRandomToggles { get; set; }
        public InputField[] RandomIntervalFields { get; set; } = new InputField[3];

        #endregion

        #region Repeat

        public InputFieldStorage RepeatCountField { get; set; }

        public InputFieldStorage RepeatOffsetTimeField { get; set; }

        public InputFieldStorage SpeedField { get; set; }

        #endregion

        #region Unity Explorer

        public FunctionButtonStorage InspectPrefabObject { get; set; }

        public FunctionButtonStorage InspectTimelineObject { get; set; }

        #endregion

        #endregion

        #region Global Values

        public RectTransform Right { get; set; }

        public InputFieldStorage OffsetField { get; set; }

        public InputField NameField { get; set; }

        public FunctionButtonStorage PrefabTypeSelectorButton { get; set; }

        public FunctionButtonStorage SavePrefabButton { get; set; }

        public Text ObjectCountText { get; set; }

        public Text PrefabObjectCountText { get; set; }

        public Text BackgroundObjectCountText { get; set; }

        public Text TimelineObjectCountText { get; set; }

        public FunctionButtonStorage InspectPrefab { get; set; }

        #endregion

        public override void Init()
        {
            if (init)
                return;

            base.Init();

            Left = GameObject.transform.Find("data/left").AsRT();
            Right = GameObject.transform.Find("data/right").AsRT();

            #region Instance Values

            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(Left, "Scroll View");

            LeftContent = scrollView.transform.Find("Viewport/Content").AsRT();

            scrollView.transform.AsRT().sizeDelta = new Vector2(383f, 690f);

            var objectsToParent = new List<Transform>();
            for (int i = 0; i < Left.childCount; i++)
                objectsToParent.Add(Left.GetChild(i));
            foreach (var child in objectsToParent)
                child.SetParent(LeftContent);

            EditorHelper.LogAvailableInstances<PrefabEditor>();

            EditorThemeManager.AddGraphic(GameObject.GetComponent<Image>(), ThemeGroup.Background_1);

            CoreHelper.Delete(LeftContent.GetChild(4).gameObject);
            CoreHelper.Delete(LeftContent.GetChild(4).gameObject);

            #region Expand

            var expandPrefabLabel = LeftContent.GetChild(0).gameObject;
            var expandPrefabLabelText = expandPrefabLabel.transform.GetChild(0).GetComponent<Text>();
            var expandPrefab = LeftContent.GetChild(1).gameObject;
            var expandPrefabButton = expandPrefab.GetComponent<Button>();
            var expandPrefabText = expandPrefab.transform.GetChild(0).GetComponent<Text>();
            EditorThemeManager.AddLightText(expandPrefabLabelText);
            CoreHelper.RemoveAnimator(expandPrefabButton);
            EditorThemeManager.AddSelectable(expandPrefabButton, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(expandPrefabText, ThemeGroup.Function_2_Text);

            #endregion

            #region Start Time

            RTEditor.GenerateLabels("time label", LeftContent, new Label("Start Time"));

            var time = EditorPrefabHolder.Instance.NumberInputField.Duplicate(LeftContent, "time");
            StartTimeField = time.GetComponent<InputFieldStorage>();

            EditorThemeManager.AddInputField(StartTimeField.inputField);
            EditorThemeManager.AddSelectable(StartTimeField.leftGreaterButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(StartTimeField.leftButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(StartTimeField.middleButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(StartTimeField.rightButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(StartTimeField.rightGreaterButton, ThemeGroup.Function_2, false);

            var locker = EditorPrefabHolder.Instance.Toggle.Duplicate(time.transform, "lock", 0);

            locker.transform.Find("Background/Checkmark").GetComponent<Image>().sprite = ObjEditor.inst.timelineObjectPrefabLock.transform.Find("lock (1)").GetComponent<Image>().sprite;
            locker.gameObject.GetOrAddComponent<LayoutElement>().minWidth = 32f;

            StartTimeField.Assign(StartTimeField.gameObject);
            StartTimeField.inputField.transform.AsRT().sizeDelta = new Vector2(135f, 32f);

            EditorThemeManager.AddToggle(StartTimeField.lockToggle);

            #endregion

            #region Autokill

            RTEditor.GenerateLabels("tod-dropdown label", LeftContent, new Label("Time of Death"));

            var autoKillParent = Creator.NewUIObject("autokill", LeftContent);
            autoKillParent.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            var autoKillParentLayout = autoKillParent.AddComponent<HorizontalLayoutGroup>();
            autoKillParentLayout.childControlHeight = false;
            autoKillParentLayout.childControlWidth = false;
            autoKillParentLayout.childForceExpandHeight = true;
            autoKillParentLayout.childForceExpandWidth = false;
            autoKillParentLayout.childScaleHeight = false;
            autoKillParentLayout.childScaleWidth = false;
            autoKillParentLayout.spacing = 8f;
            autoKillParentLayout.childAlignment = TextAnchor.UpperLeft;

            var autoKillType = EditorPrefabHolder.Instance.Dropdown.Duplicate(autoKillParent.transform, "tod-dropdown");
            AutokillDropdown = autoKillType.GetComponent<Dropdown>();
            AutokillDropdown.options = CoreHelper.StringToOptionData("Regular", "Start Time", "Song Time");

            EditorThemeManager.AddDropdown(AutokillDropdown);

            var ako = EditorPrefabHolder.Instance.NumberInputField.Duplicate(autoKillParent.transform, "akoffset");
            ako.transform.AsRT().sizeDelta = new Vector2(100f, 32f);
            AutokillField = ako.GetComponent<InputFieldStorage>();
            CoreHelper.Delete(AutokillField.leftButton.gameObject);
            CoreHelper.Delete(AutokillField.leftGreaterButton.gameObject);
            CoreHelper.Delete(AutokillField.rightButton.gameObject);
            CoreHelper.Delete(AutokillField.rightGreaterButton.gameObject);
            EditorThemeManager.AddInputField(AutokillField);
            AutokillField.middleButton.transform.SetSiblingIndex(AutokillField.transform.childCount - 1);

            AutokillField.inputField.characterValidation = InputField.CharacterValidation.None;
            AutokillField.inputField.contentType = InputField.ContentType.Standard;
            AutokillField.inputField.characterLimit = 0;

            var collapser = EditorPrefabHolder.Instance.CollapseToggle.Duplicate(autoKillParent.transform, "collapse");
            CollapseToggle = collapser.GetComponent<Toggle>();
            collapser.gameObject.GetOrAddComponent<LayoutElement>().minWidth = 32f;

            EditorThemeManager.AddToggle(CollapseToggle, ThemeGroup.Background_1);

            for (int i = 0; i < collapser.transform.Find("dots").childCount; i++)
                EditorThemeManager.AddGraphic(collapser.transform.Find("dots").GetChild(i).GetComponent<Image>(), ThemeGroup.Dark_Text);

            #endregion

            #region Parent

            RTEditor.GenerateLabels("parent label", LeftContent, new Label("Parent Object"));

            var parentUI = ObjEditor.inst.ObjectView.transform.Find("parent").gameObject.Duplicate(LeftContent, "parent");
            var parent_more = ObjEditor.inst.ObjectView.transform.Find("parent_more").gameObject.Duplicate(LeftContent, "parent_more");

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

            #endregion

            #region Transform Offsets

            // Position
            var posLabel = RTEditor.GenerateLabels("pos label", LeftContent, new Label("Position X Offset", new Vector2(175f, 20f)), new Label("Position Y Offset", new Vector2(175f, 20f)));

            var position = EditorPrefabHolder.Instance.Vector2InputFields.Duplicate(LeftContent, "position");
            var positionStorage = position.GetComponent<Vector2InputFieldStorage>();
            positionStorage.x.inputField.transform.AsRT().sizeDelta = new Vector2(110f, 32f);
            positionStorage.y.inputField.transform.AsRT().sizeDelta = new Vector2(110f, 32f);
            if (positionStorage.x.transform.Find("input"))
                positionStorage.x.transform.Find("input").AsRT().sizeDelta = new Vector2(110f, 32f);
            if (positionStorage.y.transform.Find("input"))
                positionStorage.y.transform.Find("input").AsRT().sizeDelta = new Vector2(110f, 32f);
            TriggerHelper.InversableField(positionStorage.x);
            TriggerHelper.InversableField(positionStorage.y);
            EditorThemeManager.AddInputField(positionStorage);
            TransformFields[0] = new InputFieldStorage[2] { positionStorage.x, positionStorage.y };

            EditorHelper.SetComplexity(posLabel, Complexity.Normal);
            EditorHelper.SetComplexity(position, Complexity.Normal);

            var r_posLabel = RTEditor.GenerateLabels("r_position label", LeftContent, new Label("Random X", new Vector2(175f, 20f)), new Label("Random Y", new Vector2(175f, 20f)));

            var r_position = EditorPrefabHolder.Instance.Vector2InputFields.Duplicate(LeftContent, "r_position");
            var r_positionStorage = r_position.GetComponent<Vector2InputFieldStorage>();
            r_positionStorage.x.inputField.transform.AsRT().sizeDelta = new Vector2(110f, 32f);
            r_positionStorage.y.inputField.transform.AsRT().sizeDelta = new Vector2(110f, 32f);
            if (r_positionStorage.x.transform.Find("input"))
                r_positionStorage.x.transform.Find("input").AsRT().sizeDelta = new Vector2(110f, 32f);
            if (r_positionStorage.y.transform.Find("input"))
                r_positionStorage.y.transform.Find("input").AsRT().sizeDelta = new Vector2(110f, 32f);
            TriggerHelper.InversableField(r_positionStorage.x);
            TriggerHelper.InversableField(r_positionStorage.y);
            EditorThemeManager.AddInputField(r_positionStorage);
            RandomTransformFields[0] = new InputFieldStorage[2] { r_positionStorage.x, r_positionStorage.y };

            EditorHelper.SetComplexity(r_posLabel, Complexity.Normal);
            EditorHelper.SetComplexity(r_position, Complexity.Normal);

            var randomPrefab = ObjEditor.inst.KeyframeDialogs[0].transform.Find("random").gameObject;
            var randomPositionLabel = RTEditor.GenerateLabels("position-random-label", LeftContent, new Label("Randomize"));
            var randomPosition = randomPrefab.Duplicate(LeftContent, "position-random");
            CoreHelper.DestroyChildren(randomPosition.transform, x => x.name == "homing-static" || x.name == "homing-dynamic");

            PositionRandomToggles = randomPosition.GetComponentsInChildren<Toggle>();
            foreach (var toggle in PositionRandomToggles)
            {
                EditorThemeManager.AddToggle(toggle, ThemeGroup.Background_3);
                EditorThemeManager.AddGraphic(toggle.transform.Find("Image").GetComponent<Image>(), ThemeGroup.Toggle_1_Check);
            }
            var randomPositionInterval = randomPosition.transform.Find("interval-input").GetComponent<InputField>();
            EditorThemeManager.AddInputField(randomPositionInterval);
            RandomIntervalFields[0] = randomPositionInterval;

            EditorHelper.SetComplexity(randomPositionLabel, Complexity.Normal);
            EditorHelper.SetComplexity(randomPosition, Complexity.Normal);

            // Scale
            var scaLabel = RTEditor.GenerateLabels("sca label", LeftContent, new Label("Scale X Offset", new Vector2(175f, 20f)), new Label("Scale Y Offset", new Vector2(175f, 20f)));

            var scale = EditorPrefabHolder.Instance.Vector2InputFields.Duplicate(LeftContent, "scale");
            var scaleStorage = scale.GetComponent<Vector2InputFieldStorage>();
            scaleStorage.x.inputField.transform.AsRT().sizeDelta = new Vector2(110f, 32f);
            scaleStorage.y.inputField.transform.AsRT().sizeDelta = new Vector2(110f, 32f);
            if (scaleStorage.x.transform.Find("input"))
                scaleStorage.x.transform.Find("input").AsRT().sizeDelta = new Vector2(110f, 32f);
            if (scaleStorage.y.transform.Find("input"))
                scaleStorage.y.transform.Find("input").AsRT().sizeDelta = new Vector2(110f, 32f);
            TriggerHelper.InversableField(scaleStorage.x);
            TriggerHelper.InversableField(scaleStorage.y);
            EditorThemeManager.AddInputField(scaleStorage);
            TransformFields[1] = new InputFieldStorage[2] { scaleStorage.x, scaleStorage.y };

            EditorHelper.SetComplexity(scaLabel, Complexity.Normal);
            EditorHelper.SetComplexity(scale, Complexity.Normal);

            var r_scaLabel = RTEditor.GenerateLabels("r_scale label", LeftContent, new Label("Random X", new Vector2(175f, 20f)), new Label("Random Y", new Vector2(175f, 20f)));

            var r_scale = EditorPrefabHolder.Instance.Vector2InputFields.Duplicate(LeftContent, "r_scale");
            var r_scaleStorage = r_scale.GetComponent<Vector2InputFieldStorage>();
            r_scaleStorage.x.inputField.transform.AsRT().sizeDelta = new Vector2(110f, 32f);
            r_scaleStorage.y.inputField.transform.AsRT().sizeDelta = new Vector2(110f, 32f);
            if (r_scaleStorage.x.transform.Find("input"))
                r_scaleStorage.x.transform.Find("input").AsRT().sizeDelta = new Vector2(110f, 32f);
            if (r_scaleStorage.y.transform.Find("input"))
                r_scaleStorage.y.transform.Find("input").AsRT().sizeDelta = new Vector2(110f, 32f);
            TriggerHelper.InversableField(r_scaleStorage.x);
            TriggerHelper.InversableField(r_scaleStorage.y);
            EditorThemeManager.AddInputField(r_scaleStorage);
            RandomTransformFields[1] = new InputFieldStorage[2] { r_scaleStorage.x, r_scaleStorage.y };

            EditorHelper.SetComplexity(r_scaLabel, Complexity.Normal);
            EditorHelper.SetComplexity(r_scale, Complexity.Normal);

            var randomScaleLabel = RTEditor.GenerateLabels("scale-random-label", LeftContent, new Label("Randomize"));
            var randomScale = randomPrefab.Duplicate(LeftContent, "scale-random");
            CoreHelper.DestroyChildren(randomScale.transform, x => x.name == "homing-static" || x.name == "homing-dynamic");

            ScaleRandomToggles = randomScale.GetComponentsInChildren<Toggle>();
            foreach (var toggle in ScaleRandomToggles)
            {
                EditorThemeManager.AddToggle(toggle, ThemeGroup.Background_3);
                EditorThemeManager.AddGraphic(toggle.transform.Find("Image").GetComponent<Image>(), ThemeGroup.Toggle_1_Check);
            }
            var randomScaleInterval = randomScale.transform.Find("interval-input").GetComponent<InputField>();
            EditorThemeManager.AddInputField(randomScaleInterval);
            RandomIntervalFields[1] = randomScaleInterval;

            EditorHelper.SetComplexity(randomScaleLabel, Complexity.Normal);
            EditorHelper.SetComplexity(randomScale, Complexity.Normal);

            // Rotation
            var rotLabel = RTEditor.GenerateLabels("rot label", LeftContent, new Label("Rotation Offset"));

            var rotation = EditorPrefabHolder.Instance.NumberInputField.Duplicate(LeftContent, "rotation");
            var rotationStorage = rotation.GetComponent<InputFieldStorage>();
            rotationStorage.transform.AsRT().sizeDelta = new Vector2(110f, 32f);
            rotationStorage.inputField.transform.AsRT().sizeDelta = new Vector2(110f, 32f);
            CoreHelper.Delete(rotationStorage.middleButton.gameObject);
            TriggerHelper.InversableField(rotationStorage);
            EditorThemeManager.AddInputField(rotationStorage);
            TransformFields[2] = new InputFieldStorage[1] { rotationStorage };

            EditorHelper.SetComplexity(rotLabel, Complexity.Normal);
            EditorHelper.SetComplexity(rotation, Complexity.Normal);

            var r_rotLabel = RTEditor.GenerateLabels("r_rotation label", LeftContent, new Label("Random"));

            var r_rotation = EditorPrefabHolder.Instance.NumberInputField.Duplicate(LeftContent, "r_rotation");
            var r_rotationStorage = r_rotation.GetComponent<InputFieldStorage>();
            r_rotationStorage.transform.AsRT().sizeDelta = new Vector2(110f, 32f);
            r_rotationStorage.inputField.transform.AsRT().sizeDelta = new Vector2(110f, 32f);
            CoreHelper.Delete(r_rotationStorage.middleButton.gameObject);
            TriggerHelper.InversableField(r_rotationStorage);
            EditorThemeManager.AddInputField(r_rotationStorage);
            RandomTransformFields[2] = new InputFieldStorage[1] { r_rotationStorage };

            var randomRotationLabel = RTEditor.GenerateLabels("rotation-random-label", LeftContent, new Label("Randomize"));
            var randomRotation = randomPrefab.Duplicate(LeftContent, "rotation-random");
            CoreHelper.DestroyChildren(randomRotation.transform, x => x.name == "scale" || x.name == "homing-static" || x.name == "homing-dynamic");

            RotationRandomToggles = randomRotation.GetComponentsInChildren<Toggle>();
            foreach (var toggle in RotationRandomToggles)
            {
                EditorThemeManager.AddToggle(toggle, ThemeGroup.Background_3);
                EditorThemeManager.AddGraphic(toggle.transform.Find("Image").GetComponent<Image>(), ThemeGroup.Toggle_1_Check);
            }
            var randomRotationInterval = randomRotation.transform.Find("interval-input").GetComponent<InputField>();
            EditorThemeManager.AddInputField(randomRotationInterval);
            RandomIntervalFields[2] = randomRotationInterval;

            EditorHelper.SetComplexity(randomRotationLabel, Complexity.Normal);
            EditorHelper.SetComplexity(randomRotation, Complexity.Normal);

            #endregion

            #region Repeat / Speed

            // Repeat
            RTEditor.GenerateLabels("repeat label", LeftContent, new Label("Repeat Count", new Vector2(175f, 20f)), new Label("Repeat Time", new Vector2(175f, 20f)));

            var repeat = EditorPrefabHolder.Instance.Vector2InputFields.Duplicate(LeftContent, "repeat");
            var repeatStorage = repeat.GetComponent<Vector2InputFieldStorage>();
            repeatStorage.x.inputField.transform.AsRT().sizeDelta = new Vector2(110f, 32f);
            repeatStorage.y.inputField.transform.AsRT().sizeDelta = new Vector2(110f, 32f);
            if (repeatStorage.x.transform.Find("input"))
                repeatStorage.x.transform.Find("input").AsRT().sizeDelta = new Vector2(110f, 32f);
            if (repeatStorage.y.transform.Find("input"))
                repeatStorage.y.transform.Find("input").AsRT().sizeDelta = new Vector2(110f, 32f);

            RepeatCountField = repeatStorage.x;
            RepeatCountField.Assign(RepeatCountField.gameObject);
            RepeatCountField.inputField.characterValidation = InputField.CharacterValidation.Integer;
            RepeatCountField.inputField.contentType = InputField.ContentType.Standard;
            RepeatCountField.inputField.characterLimit = 5;

            RepeatOffsetTimeField = repeatStorage.y;
            RepeatOffsetTimeField.Assign(RepeatOffsetTimeField.gameObject);
            RepeatOffsetTimeField.inputField.characterValidation = InputField.CharacterValidation.Decimal;
            RepeatOffsetTimeField.inputField.contentType = InputField.ContentType.Standard;
            RepeatOffsetTimeField.inputField.characterLimit = 0;

            EditorThemeManager.AddInputField(RepeatCountField);
            EditorThemeManager.AddInputField(RepeatOffsetTimeField);

            // Speed
            RTEditor.GenerateLabels("speed label", LeftContent, new Label("Speed"));

            var speed = EditorPrefabHolder.Instance.NumberInputField.Duplicate(LeftContent, "speed");
            SpeedField = speed.GetComponent<InputFieldStorage>();
            SpeedField.Assign(SpeedField.gameObject);

            SpeedField.inputField.characterValidation = InputField.CharacterValidation.Decimal;
            SpeedField.inputField.contentType = InputField.ContentType.Standard;
            SpeedField.inputField.characterLimit = 0;

            CoreHelper.Delete(SpeedField.leftGreaterButton.gameObject);
            CoreHelper.Delete(SpeedField.middleButton.gameObject);
            CoreHelper.Delete(SpeedField.rightGreaterButton.gameObject);

            EditorThemeManager.AddInputField(SpeedField);

            #endregion

            #region Editor Settings

            EditorSettingsParent = LeftContent.Find("editor").AsRT();
            var editorSettingsIndex = EditorSettingsParent.GetSiblingIndex();
            CoreHelper.Delete(LeftContent.GetChild(editorSettingsIndex - 1).gameObject);
            RTEditor.GenerateLabels("editor label", LeftContent, new Label("Editor Layer"), new Label("Editor Bin"));

            EditorSettingsParent.Find("layer").gameObject.SetActive(false);
            EditorSettingsParent.SetSiblingIndex(LeftContent.childCount - 1);

            var layers = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(EditorSettingsParent, "layers", 0);
            EditorLayerField = layers.GetComponent<InputField>();
            EditorLayerField.textComponent.alignment = TextAnchor.MiddleCenter;
            layers.gameObject.AddComponent<ContrastColors>().Init(EditorLayerField.textComponent, EditorLayerField.image);
            EditorThemeManager.AddGraphic(EditorLayerField.image, ThemeGroup.Null, true);

            var edhlg = EditorSettingsParent.GetComponent<HorizontalLayoutGroup>();
            edhlg.childControlWidth = false;
            edhlg.childForceExpandWidth = false;

            layers.transform.AsRT().sizeDelta = new Vector2(100f, 32f);

            BinSlider = EditorSettingsParent.Find("bin").GetComponent<Slider>();
            BinSlider.transform.AsRT().sizeDelta = new Vector2(237f, 32f);
            var binSliderImage = BinSlider.transform.Find("Image").GetComponent<Image>();
            BinSlider.colors = UIManager.SetColorBlock(BinSlider.colors, Color.white, new Color(0.9f, 0.9f, 0.9f), Color.white, Color.white, Color.white);
            EditorThemeManager.AddGraphic(binSliderImage, ThemeGroup.Slider_2, true);
            EditorThemeManager.AddGraphic(BinSlider.image, ThemeGroup.Slider_2_Handle, true);

            EditorLayerToggles = EditorSettingsParent.Find("layer").GetComponentsInChildren<Toggle>();
            CoreHelper.Destroy(EditorSettingsParent.Find("layer").GetComponent<ToggleGroup>());
            int layerNum = 0;
            foreach (var toggle in EditorLayerToggles)
            {
                toggle.group = null;
                CoreHelper.Destroy(toggle.GetComponent<EventTrigger>());
                EditorThemeManager.AddGraphic(toggle.image, layerNum switch
                {
                    0 => ThemeGroup.Layer_1,
                    1 => ThemeGroup.Layer_2,
                    2 => ThemeGroup.Layer_3,
                    3 => ThemeGroup.Layer_4,
                    4 => ThemeGroup.Layer_5,
                    _ => ThemeGroup.Null,
                });
                EditorThemeManager.AddGraphic(toggle.graphic, ThemeGroup.Timeline_Bar);
                toggle.gameObject.AddComponent<ContrastColors>().Init(toggle.transform.Find("Background/Text").GetComponent<Text>(), toggle.image);
                layerNum++;
            }

            var label = EditorPrefabHolder.Instance.Labels.Duplicate(LeftContent, "indexer_label");

            var labelText = label.transform.GetChild(0).GetComponent<Text>();
            labelText.text = "Editor Index";
            EditorThemeManager.AddLightText(labelText);

            var indexEditor = EditorPrefabHolder.Instance.NumberInputField.Duplicate(LeftContent, "indexer");
            EditorIndexField = indexEditor.GetComponent<InputFieldStorage>();
            if (EditorIndexField.middleButton)
                CoreHelper.Delete(EditorIndexField.middleButton.gameObject);
            EditorThemeManager.AddInputField(EditorIndexField);

            #endregion

            #region Unity Explorer

            if (ModCompatibility.UnityExplorerInstalled)
            {
                RTEditor.GenerateLabels("inspect label", LeftContent, new Label("Unity Explorer"));

                var inspectPrefabObject = EditorPrefabHolder.Instance.Function2Button.Duplicate(LeftContent, "inspect prefab object");
                InspectPrefabObject = inspectPrefabObject.GetComponent<FunctionButtonStorage>();
                InspectPrefabObject.label.text = "Inspect Prefab Object";
                EditorThemeManager.AddSelectable(InspectPrefabObject.button, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(InspectPrefabObject.label, ThemeGroup.Function_2_Text);

                var inspectTimelineObject = EditorPrefabHolder.Instance.Function2Button.Duplicate(LeftContent, "inspect timeline object");
                InspectTimelineObject = inspectTimelineObject.GetComponent<FunctionButtonStorage>();
                InspectTimelineObject.label.text = "Inspect Timeline Object";
                EditorThemeManager.AddSelectable(InspectTimelineObject.button, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(InspectTimelineObject.label, ThemeGroup.Function_2_Text);
            }

            #endregion

            #endregion

            #region Global Values

            #region Offset

            var offsetLabel = Right.GetChild(0);
            var offsetLabelText = offsetLabel.GetChild(0).GetComponent<Text>();
            offsetLabelText.text = "Global Offset Time";
            EditorThemeManager.AddLightText(offsetLabelText);

            CoreHelper.Delete(Right.Find("time").gameObject);
            var offsetTime = EditorPrefabHolder.Instance.NumberInputField.Duplicate(Right, "time", 1);
            OffsetField = offsetTime.GetComponent<InputFieldStorage>();
            CoreHelper.Delete(OffsetField.middleButton.gameObject);
            EditorThemeManager.AddInputField(OffsetField);

            #endregion

            #region Name

            // Name
            var nameLabel = RTEditor.GenerateLabels("name label", Right, new Label("Name"));
            EditorHelper.SetComplexity(nameLabel, Complexity.Normal);

            var prefabName = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(Right, "name");
            prefabName.transform.localScale = Vector3.one;

            NameField = prefabName.GetComponent<InputField>();

            NameField.characterValidation = InputField.CharacterValidation.None;
            NameField.contentType = InputField.ContentType.Standard;
            NameField.characterLimit = 0;

            EditorThemeManager.AddInputField(NameField);
            EditorHelper.SetComplexity(prefabName, Complexity.Normal);

            #endregion

            #region Type

            // Type
            var typeLabel = RTEditor.GenerateLabels("type label", Right, new Label("Type"));
            EditorHelper.SetComplexity(typeLabel, Complexity.Normal);

            var type = EditorPrefabHolder.Instance.Function1Button.Duplicate(Right, "type");

            PrefabTypeSelectorButton = type.GetComponent<FunctionButtonStorage>();

            type.transform.AsRT().sizeDelta = new Vector2(371f, 32f);
            type.gameObject.AddComponent<ContrastColors>().Init(PrefabTypeSelectorButton.label, PrefabTypeSelectorButton.button.image);

            EditorThemeManager.AddGraphic(PrefabTypeSelectorButton.button.image, ThemeGroup.Null, true);
            EditorHelper.SetComplexity(type, Complexity.Normal);

            #endregion

            #region Save Prefab

            var savePrefabLabel = new Labels(Labels.InitSettings.Default.Name("save prefab label"), new Label("Apply to an External Prefab"));

            var savePrefab = EditorPrefabHolder.Instance.Function2Button.Duplicate(Right, "save prefab");
            SavePrefabButton = savePrefab.GetComponent<FunctionButtonStorage>();
            SavePrefabButton.label.text = "Select Prefab";

            EditorHelper.SetComplexity(savePrefabLabel.GameObject, Complexity.Normal);
            EditorHelper.SetComplexity(savePrefab, Complexity.Normal);
            EditorThemeManager.AddSelectable(SavePrefabButton.button, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(SavePrefabButton.label, ThemeGroup.Function_2_Text);

            #endregion

            #region Inspect

            if (ModCompatibility.UnityExplorerInstalled)
            {
                var inspectPrefabLabel = new Labels(Labels.InitSettings.Default.Name("inspect prefab label"), new Label("Unity Explorer"));

                var inspectPrefab = EditorPrefabHolder.Instance.Function2Button.Duplicate(Right, "inspect prefab");
                InspectPrefab = inspectPrefab.GetComponent<FunctionButtonStorage>();
                InspectPrefab.label.text = "Inspect Prefab";

                EditorHelper.SetComplexity(inspectPrefabLabel.GameObject, Complexity.Advanced);
                EditorHelper.SetComplexity(inspectPrefab, Complexity.Advanced);
                EditorThemeManager.AddSelectable(InspectPrefab.button, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(InspectPrefab.label, ThemeGroup.Function_2_Text);
            }

            #endregion

            #region Counters

            // Object Count
            var objectCounter = RTEditor.GenerateLabels("object count label", Right, new Label("Object Count: 0") { horizontalWrap = HorizontalWrapMode.Overflow, });
            ObjectCountText = objectCounter.transform.GetChild(0).GetComponent<Text>();
            EditorHelper.SetComplexity(objectCounter, Complexity.Normal);

            // Prefab Object Count
            var prefabObjectCounter = RTEditor.GenerateLabels("prefab object count label", Right, new Label("Prefab Object Count: 0") { horizontalWrap = HorizontalWrapMode.Overflow, });
            PrefabObjectCountText = prefabObjectCounter.transform.GetChild(0).GetComponent<Text>();
            EditorHelper.SetComplexity(prefabObjectCounter, Complexity.Normal);

            // Prefab Object Count
            var backgroundObjectCounter = RTEditor.GenerateLabels("background object count label", Right, new Label("Background Object Count: 0") { horizontalWrap = HorizontalWrapMode.Overflow, });
            BackgroundObjectCountText = backgroundObjectCounter.transform.GetChild(0).GetComponent<Text>();
            EditorHelper.SetComplexity(backgroundObjectCounter, Complexity.Normal);

            // Timeline Object Count
            var timelineObjectCounter = RTEditor.GenerateLabels("timeline object count label", Right, new Label("Timeline Object Count: 0") { horizontalWrap = HorizontalWrapMode.Overflow, });
            TimelineObjectCountText = timelineObjectCounter.transform.GetChild(0).GetComponent<Text>();
            EditorHelper.SetComplexity(timelineObjectCounter, Complexity.Normal);

            #endregion

            #endregion
        }
    }
}
