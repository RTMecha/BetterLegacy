using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class PrefabObjectEditorDialog : EditorDialog, ITagDialog, IParentDialog, IEditorLayerUI
    {
        public PrefabObjectEditorDialog() : base(PREFAB_SELECTOR) { }

        #region Instance Values

        public RectTransform Left { get; set; }

        public RectTransform LeftContent { get; set; }

        #region Tags

        public RectTransform TagsScrollView { get; set; }

        public RectTransform TagsContent { get; set; }

        #endregion

        #region Parent

        public FunctionButtonStorage ParentButton { get; set; }
        public HoverTooltip ParentInfo { get; set; }
        public Button ParentMoreButton { get; set; }
        public GameObject ParentSettingsParent { get; set; }
        public Toggle ParentDesyncToggle { get; set; }
        public Button ParentSearchButton { get; set; }
        public Button ParentClearButton { get; set; }
        public Button ParentPickerButton { get; set; }

        public List<ParentSetting> ParentSettings { get; set; } = new List<ParentSetting>();

        #endregion

        #region Start Time / Autokill

        public InputFieldStorage StartTimeField { get; set; }

        public Dropdown AutokillDropdown { get; set; }
        public InputFieldStorage AutokillField { get; set; }
        public Toggle CollapseToggle { get; set; }

        #endregion

        #region Instance Data

        public FunctionButtonStorage CopyInstanceDataButton { get; set; }
        public FunctionButtonStorage PasteInstanceDataButton { get; set; }
        public FunctionButtonStorage RemoveInstanceDataButton { get; set; }

        #endregion

        #region Editor

        public RectTransform EditorSettingsParent { get; set; }
        public InputField EditorLayerField { get; set; }
        public RectTransform EditorLayerTogglesParent { get; set; }
        public Toggle[] EditorLayerToggles { get; set; }
        public Slider BinSlider { get; set; }

        public InputFieldStorage EditorIndexField { get; set; }

        public InputField BaseColorField { get; set; }
        public InputField SelectColorField { get; set; }
        public InputField TextColorField { get; set; }
        public InputField MarkColorField { get; set; }

        #endregion

        #region Transforms

        public InputFieldStorage[][] TransformFields { get; set; } = new InputFieldStorage[3][];
        public InputFieldStorage[][] RandomTransformFields { get; set; } = new InputFieldStorage[3][];
        public Toggle[] PositionRandomToggles { get; set; }
        public Toggle[] ScaleRandomToggles { get; set; }
        public Toggle[] RotationRandomToggles { get; set; }
        public InputField[] RandomIntervalFields { get; set; } = new InputField[3];

        public InputFieldStorage DepthField { get; set; }

        #endregion

        #region Repeat

        public InputFieldStorage RepeatCountField { get; set; }

        public InputFieldStorage RepeatOffsetTimeField { get; set; }

        public InputFieldStorage SpeedField { get; set; }

        #endregion

        #region Unity Explorer

        public FunctionButtonStorage InspectPrefabObject { get; set; }
        public FunctionButtonStorage InspectRuntimeObjectButton { get; set; }
        public FunctionButtonStorage InspectTimelineObject { get; set; }

        #endregion

        public ModifiersEditorDialog ModifiersDialog { get; set; }

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

        public FunctionButtonStorage DefaultInstanceDataButton { get; set; }
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

            EditorThemeManager.ApplyGraphic(GameObject.GetComponent<Image>(), ThemeGroup.Background_1);

            CoreHelper.Delete(LeftContent.GetChild(4).gameObject);
            CoreHelper.Delete(LeftContent.GetChild(4).gameObject);

            #region Expand

            var expandPrefabLabel = LeftContent.GetChild(0).gameObject;
            var expandPrefabLabelText = expandPrefabLabel.transform.GetChild(0).GetComponent<Text>();
            var expandPrefab = LeftContent.GetChild(1).gameObject;
            var expandPrefabButton = expandPrefab.GetComponent<Button>();
            var expandPrefabText = expandPrefab.transform.GetChild(0).GetComponent<Text>();
            EditorThemeManager.ApplyLightText(expandPrefabLabelText);
            CoreHelper.RemoveAnimator(expandPrefabButton);
            EditorThemeManager.ApplySelectable(expandPrefabButton, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(expandPrefabText, ThemeGroup.Function_2_Text);

            #endregion

            #region Tags

            RTEditor.GenerateLabels("time label", LeftContent, new Label("Tags"));

            // Tags Scroll View/Viewport/Content
            var tagScrollView = Creator.NewUIObject("Tags Scroll View", LeftContent);
            TagsScrollView = tagScrollView.transform.AsRT();
            TagsScrollView.sizeDelta = new Vector2(522f, 40f);

            var scroll = tagScrollView.AddComponent<ScrollRect>();
            scroll.scrollSensitivity = 20f;
            scroll.horizontal = true;
            scroll.vertical = false;

            var image = tagScrollView.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.01f);

            var mask = tagScrollView.AddComponent<Mask>();

            var tagViewport = Creator.NewUIObject("Viewport", tagScrollView.transform);
            RectValues.FullAnchored.AssignToRectTransform(tagViewport.transform.AsRT());

            var tagContent = Creator.NewUIObject("Content", tagViewport.transform);
            TagsContent = tagContent.transform.AsRT();

            var tagContentGLG = tagContent.AddComponent<GridLayoutGroup>();
            tagContentGLG.cellSize = new Vector2(168f, 32f);
            tagContentGLG.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            tagContentGLG.constraintCount = 1;
            tagContentGLG.childAlignment = TextAnchor.MiddleLeft;
            tagContentGLG.spacing = new Vector2(8f, 0f);

            var tagContentCSF = tagContent.AddComponent<ContentSizeFitter>();
            tagContentCSF.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            tagContentCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

            scroll.viewport = tagViewport.transform.AsRT();
            scroll.content = TagsContent;

            #endregion

            #region Start Time

            RTEditor.GenerateLabels("time label", LeftContent, new Label("Start Time"));

            var time = EditorPrefabHolder.Instance.NumberInputField.Duplicate(LeftContent, "time");
            StartTimeField = time.GetComponent<InputFieldStorage>();

            EditorThemeManager.ApplyInputField(StartTimeField.inputField);
            EditorThemeManager.ApplySelectable(StartTimeField.leftGreaterButton, ThemeGroup.Function_2, false);
            EditorThemeManager.ApplySelectable(StartTimeField.leftButton, ThemeGroup.Function_2, false);
            EditorThemeManager.ApplySelectable(StartTimeField.middleButton, ThemeGroup.Function_2, false);
            EditorThemeManager.ApplySelectable(StartTimeField.rightButton, ThemeGroup.Function_2, false);
            EditorThemeManager.ApplySelectable(StartTimeField.rightGreaterButton, ThemeGroup.Function_2, false);

            var locker = EditorPrefabHolder.Instance.Toggle.Duplicate(time.transform, "lock", 0);

            locker.transform.Find("Background/Checkmark").GetComponent<Image>().sprite = ObjEditor.inst.timelineObjectPrefabLock.transform.Find("lock (1)").GetComponent<Image>().sprite;
            locker.gameObject.GetOrAddComponent<LayoutElement>().minWidth = 32f;

            StartTimeField.Assign(StartTimeField.gameObject);
            StartTimeField.inputField.transform.AsRT().sizeDelta = new Vector2(135f, 32f);

            EditorThemeManager.ApplyToggle(StartTimeField.lockToggle);

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
            TooltipHelper.AssignTooltip(AutokillDropdown.gameObject, "Prefab Object Autokill Type");

            EditorThemeManager.ApplyDropdown(AutokillDropdown);

            var ako = EditorPrefabHolder.Instance.NumberInputField.Duplicate(autoKillParent.transform, "akoffset");
            ako.transform.AsRT().sizeDelta = new Vector2(100f, 32f);
            AutokillField = ako.GetComponent<InputFieldStorage>();
            CoreHelper.Delete(AutokillField.leftButton.gameObject);
            CoreHelper.Delete(AutokillField.leftGreaterButton.gameObject);
            CoreHelper.Delete(AutokillField.rightButton.gameObject);
            CoreHelper.Delete(AutokillField.rightGreaterButton.gameObject);
            EditorThemeManager.ApplyInputField(AutokillField);
            AutokillField.middleButton.transform.SetSiblingIndex(AutokillField.transform.childCount - 1);

            AutokillField.inputField.characterValidation = InputField.CharacterValidation.None;
            AutokillField.inputField.contentType = InputField.ContentType.Standard;
            AutokillField.inputField.characterLimit = 0;

            var collapser = EditorPrefabHolder.Instance.CollapseToggle.Duplicate(autoKillParent.transform, "collapse");
            CollapseToggle = collapser.GetComponent<Toggle>();
            collapser.gameObject.GetOrAddComponent<LayoutElement>().minWidth = 32f;

            EditorThemeManager.ApplyToggle(CollapseToggle, ThemeGroup.Background_1);

            for (int i = 0; i < collapser.transform.Find("dots").childCount; i++)
                EditorThemeManager.ApplyGraphic(collapser.transform.Find("dots").GetChild(i).GetComponent<Image>(), ThemeGroup.Dark_Text);

            #endregion

            #region Parent

            //public FunctionButtonStorage ParentButton { get; set; }
            //public HoverTooltip ParentInfo { get; set; }
            //public Button ParentMoreButton { get; set; }
            //public GameObject ParentSettingsParent { get; set; }
            //public Toggle ParentDesyncToggle { get; set; }
            //public Button ParentSearchButton { get; set; }
            //public Button ParentClearButton { get; set; }
            //public Button ParentPickerButton { get; set; }

            //public List<ParentSetting> ParentSettings { get; set; } = new List<ParentSetting>();

            RTEditor.GenerateLabels("parent label", LeftContent, new Label("Parent Object"));

            var parentUI = ObjEditor.inst.ObjectView.transform.Find("parent").gameObject.Duplicate(LeftContent, "parent");
            ParentSettingsParent = ObjEditor.inst.ObjectView.transform.Find("parent_more").gameObject.Duplicate(LeftContent, "parent_more");
            var parentSettingsSizeFitter = ParentSettingsParent.gameObject.GetOrAddComponent<ContentSizeFitter>();
            parentSettingsSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

            ParentButton = parentUI.transform.Find("text").GetComponent<FunctionButtonStorage>();
            ParentInfo = ParentButton.label.gameObject.GetOrAddComponent<HoverTooltip>();
            if (ParentInfo.tooltipLangauges.IsEmpty())
                ParentInfo.tooltipLangauges.Add(new Tooltip() { language = Language.English, });
            ParentMoreButton = parentUI.transform.Find("more").GetComponent<Button>();
            ParentSearchButton = parentUI.transform.Find("parent").GetComponent<Button>();
            ParentClearButton = parentUI.transform.Find("clear parent").GetComponent<Button>();
            ParentPickerButton = parentUI.transform.Find("parent picker").GetComponent<Button>();
            ParentDesyncToggle = ParentSettingsParent.transform.Find("desync").GetComponent<Toggle>();

            EditorThemeManager.ApplyGraphic(ParentSearchButton.image, ThemeGroup.Function_3, true);
            EditorThemeManager.ApplyGraphic(ParentSearchButton.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Function_3_Text);
            EditorThemeManager.ApplySelectable(ParentClearButton, ThemeGroup.Close);
            EditorThemeManager.ApplyGraphic(ParentClearButton.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);
            EditorThemeManager.ApplySelectable(ParentPickerButton, ThemeGroup.Picker);
            EditorThemeManager.ApplyGraphic(ParentPickerButton.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Picker_Icon);

            EditorThemeManager.ApplySelectable(ParentButton.button, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(ParentButton.label, ThemeGroup.Function_2_Text);
            EditorThemeManager.ApplySelectable(ParentMoreButton, ThemeGroup.Function_2, false);

            EditorThemeManager.ApplyToggle(ParentDesyncToggle, graphic: ParentDesyncToggle.transform.Find("Text").GetComponent<Text>());

            for (int i = 0; i < 3; i++)
            {
                var name = i switch
                {
                    0 => "pos",
                    1 => "sca",
                    _ => "rot"
                };

                var row = ParentSettingsParent.transform.GetChild(i + 2);
                var parentSetting = new ParentSetting()
                {
                    row = row,
                    label = row.Find("text").GetComponent<Text>(),
                    activeToggle = row.Find(name).GetComponent<Toggle>(),
                    offsetField = row.Find($"{name}_offset").GetComponent<InputField>(),
                    additiveToggle = row.Find($"{name}_add").GetComponent<Toggle>(),
                    parallaxField = row.Find($"{name}_parallax").GetComponent<InputField>(),
                };
                ParentSettings.Add(parentSetting);

                parentSetting.label.fontSize = 19;
                EditorThemeManager.ApplyLightText(parentSetting.label);

                EditorThemeManager.ApplyToggle(parentSetting.activeToggle, ThemeGroup.Background_1);
                EditorThemeManager.ApplyGraphic(parentSetting.activeToggle.transform.Find("Image").GetComponent<Image>(), ThemeGroup.Toggle_1_Check);
                EditorThemeManager.ApplyInputField(parentSetting.offsetField);
                EditorThemeManager.ApplyToggle(parentSetting.additiveToggle, ThemeGroup.Background_1);
                EditorThemeManager.ApplyGraphic(parentSetting.additiveToggle.transform.Find("Image").GetComponent<Image>(), ThemeGroup.Toggle_1_Check);
                EditorThemeManager.ApplyInputField(parentSetting.parallaxField);
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
            EditorThemeManager.ApplyInputField(positionStorage);
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
            EditorThemeManager.ApplyInputField(r_positionStorage);
            RandomTransformFields[0] = new InputFieldStorage[2] { r_positionStorage.x, r_positionStorage.y };

            EditorHelper.SetComplexity(r_posLabel, Complexity.Normal);
            EditorHelper.SetComplexity(r_position, Complexity.Normal);

            var randomPrefab = ObjEditor.inst.KeyframeDialogs[0].transform.Find("random").gameObject;
            var randomPositionLabel = RTEditor.GenerateLabels("position-random-label", LeftContent, "Randomize");
            var randomPosition = randomPrefab.Duplicate(LeftContent, "position-random");
            CoreHelper.DestroyChildren(randomPosition.transform, x => x.name == "homing-static" || x.name == "homing-dynamic");

            PositionRandomToggles = randomPosition.GetComponentsInChildren<Toggle>();
            foreach (var toggle in PositionRandomToggles)
            {
                EditorThemeManager.ApplyToggle(toggle, ThemeGroup.Background_3);
                EditorThemeManager.ApplyGraphic(toggle.transform.Find("Image").GetComponent<Image>(), ThemeGroup.Toggle_1_Check);
            }
            var randomPositionInterval = randomPosition.transform.Find("interval-input").GetComponent<InputField>();
            EditorThemeManager.ApplyInputField(randomPositionInterval);
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
            EditorThemeManager.ApplyInputField(scaleStorage);
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
            EditorThemeManager.ApplyInputField(r_scaleStorage);
            RandomTransformFields[1] = new InputFieldStorage[2] { r_scaleStorage.x, r_scaleStorage.y };

            EditorHelper.SetComplexity(r_scaLabel, Complexity.Normal);
            EditorHelper.SetComplexity(r_scale, Complexity.Normal);

            var randomScaleLabel = RTEditor.GenerateLabels("scale-random-label", LeftContent, "Randomize");
            var randomScale = randomPrefab.Duplicate(LeftContent, "scale-random");
            CoreHelper.DestroyChildren(randomScale.transform, x => x.name == "homing-static" || x.name == "homing-dynamic");

            ScaleRandomToggles = randomScale.GetComponentsInChildren<Toggle>();
            foreach (var toggle in ScaleRandomToggles)
            {
                EditorThemeManager.ApplyToggle(toggle, ThemeGroup.Background_3);
                EditorThemeManager.ApplyGraphic(toggle.transform.Find("Image").GetComponent<Image>(), ThemeGroup.Toggle_1_Check);
            }
            var randomScaleInterval = randomScale.transform.Find("interval-input").GetComponent<InputField>();
            EditorThemeManager.ApplyInputField(randomScaleInterval);
            RandomIntervalFields[1] = randomScaleInterval;

            EditorHelper.SetComplexity(randomScaleLabel, Complexity.Normal);
            EditorHelper.SetComplexity(randomScale, Complexity.Normal);

            // Rotation
            var rotLabel = RTEditor.GenerateLabels("rot label", LeftContent, "Rotation Offset");

            var rotation = EditorPrefabHolder.Instance.NumberInputField.Duplicate(LeftContent, "rotation");
            var rotationStorage = rotation.GetComponent<InputFieldStorage>();
            rotationStorage.transform.AsRT().sizeDelta = new Vector2(110f, 32f);
            rotationStorage.inputField.transform.AsRT().sizeDelta = new Vector2(110f, 32f);
            CoreHelper.Delete(rotationStorage.middleButton.gameObject);
            TriggerHelper.InversableField(rotationStorage);
            EditorThemeManager.ApplyInputField(rotationStorage);
            TransformFields[2] = new InputFieldStorage[1] { rotationStorage };

            EditorHelper.SetComplexity(rotLabel, Complexity.Normal);
            EditorHelper.SetComplexity(rotation, Complexity.Normal);

            var r_rotLabel = RTEditor.GenerateLabels("r_rotation label", LeftContent, "Random");

            var r_rotation = EditorPrefabHolder.Instance.NumberInputField.Duplicate(LeftContent, "r_rotation");
            var r_rotationStorage = r_rotation.GetComponent<InputFieldStorage>();
            r_rotationStorage.transform.AsRT().sizeDelta = new Vector2(110f, 32f);
            r_rotationStorage.inputField.transform.AsRT().sizeDelta = new Vector2(110f, 32f);
            CoreHelper.Delete(r_rotationStorage.middleButton.gameObject);
            TriggerHelper.InversableField(r_rotationStorage);
            EditorThemeManager.ApplyInputField(r_rotationStorage);
            RandomTransformFields[2] = new InputFieldStorage[1] { r_rotationStorage };

            var randomRotationLabel = RTEditor.GenerateLabels("rotation-random-label", LeftContent, new Label("Randomize"));
            var randomRotation = randomPrefab.Duplicate(LeftContent, "rotation-random");
            CoreHelper.DestroyChildren(randomRotation.transform, x => x.name == "scale" || x.name == "homing-static" || x.name == "homing-dynamic");

            RotationRandomToggles = randomRotation.GetComponentsInChildren<Toggle>();
            foreach (var toggle in RotationRandomToggles)
            {
                EditorThemeManager.ApplyToggle(toggle, ThemeGroup.Background_3);
                EditorThemeManager.ApplyGraphic(toggle.transform.Find("Image").GetComponent<Image>(), ThemeGroup.Toggle_1_Check);
            }
            var randomRotationInterval = randomRotation.transform.Find("interval-input").GetComponent<InputField>();
            EditorThemeManager.ApplyInputField(randomRotationInterval);
            RandomIntervalFields[2] = randomRotationInterval;

            EditorHelper.SetComplexity(randomRotationLabel, Complexity.Normal);
            EditorHelper.SetComplexity(randomRotation, Complexity.Normal);

            // Depth
            var depthLabel = RTEditor.GenerateLabels("depth label", LeftContent, "Depth");
            var depth = EditorPrefabHolder.Instance.NumberInputField.Duplicate(LeftContent, "depth");
            DepthField = depth.GetComponent<InputFieldStorage>();
            CoreHelper.Delete(DepthField.middleButton);
            TriggerHelper.InversableField(DepthField);
            EditorThemeManager.ApplyInputField(DepthField);

            EditorHelper.SetComplexity(depthLabel, Complexity.Normal);
            EditorHelper.SetComplexity(depth, Complexity.Advanced);

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

            EditorThemeManager.ApplyInputField(RepeatCountField);
            EditorThemeManager.ApplyInputField(RepeatOffsetTimeField);

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

            EditorThemeManager.ApplyInputField(SpeedField);

            #endregion

            #region Instance Data

            RTEditor.GenerateLabels("copy label", LeftContent, new Label("Instance Data"));

            var copyInstanceData = EditorPrefabHolder.Instance.Function1Button.Duplicate(LeftContent, "copy instance data");
            copyInstanceData.transform.AsRT().sizeDelta = new Vector2(356f, 32f);
            CopyInstanceDataButton = copyInstanceData.GetComponent<FunctionButtonStorage>();
            CopyInstanceDataButton.Text = "Copy Data";
            EditorThemeManager.ApplyGraphic(CopyInstanceDataButton.button.image, ThemeGroup.Copy, true);
            EditorThemeManager.ApplyGraphic(CopyInstanceDataButton.label, ThemeGroup.Copy_Text);
            TooltipHelper.AssignTooltip(copyInstanceData, "Copy Prefab Instance Data");
            
            var pasteInstanceData = EditorPrefabHolder.Instance.Function1Button.Duplicate(LeftContent, "paste instance data");
            pasteInstanceData.transform.AsRT().sizeDelta = new Vector2(356f, 32f);
            PasteInstanceDataButton = pasteInstanceData.GetComponent<FunctionButtonStorage>();
            PasteInstanceDataButton.Text = "Paste Data";
            EditorThemeManager.ApplyGraphic(PasteInstanceDataButton.button.image, ThemeGroup.Paste, true);
            EditorThemeManager.ApplyGraphic(PasteInstanceDataButton.label, ThemeGroup.Paste_Text);
            TooltipHelper.AssignTooltip(pasteInstanceData, "Paste Prefab Instance Data");

            var removeInstanceData = EditorPrefabHolder.Instance.Function1Button.Duplicate(LeftContent, "remove instance data");
            removeInstanceData.transform.AsRT().sizeDelta = new Vector2(356f, 32f);
            RemoveInstanceDataButton = removeInstanceData.GetComponent<FunctionButtonStorage>();
            RemoveInstanceDataButton.Text = "Remove Copied Data";
            EditorThemeManager.ApplyGraphic(RemoveInstanceDataButton.button.image, ThemeGroup.Delete, true);
            EditorThemeManager.ApplyGraphic(RemoveInstanceDataButton.label, ThemeGroup.Delete_Text);
            TooltipHelper.AssignTooltip(removeInstanceData, "Remove Prefab Instance Data");

            #endregion

            #region Editor Settings

            EditorSettingsParent = LeftContent.Find("editor").AsRT();
            EditorSettingsParent.GetComponent<HorizontalLayoutGroup>().childControlWidth = true;

            var editorSettingsIndex = EditorSettingsParent.GetSiblingIndex();
            CoreHelper.Delete(LeftContent.GetChild(editorSettingsIndex - 1).gameObject);
            RTEditor.GenerateLabels("editor label", LeftContent, new Label("Editor Layer"), new Label("Editor Bin"));

            EditorSettingsParent.Find("layer").gameObject.SetActive(false);
            EditorSettingsParent.SetSiblingIndex(LeftContent.childCount - 1);

            var layers = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(EditorSettingsParent, "layers", 0);
            EditorLayerField = layers.GetComponent<InputField>();
            EditorLayerField.textComponent.alignment = TextAnchor.MiddleCenter;
            layers.gameObject.AddComponent<ContrastColors>().Init(EditorLayerField.textComponent, EditorLayerField.image);
            EditorThemeManager.ApplyGraphic(EditorLayerField.image, ThemeGroup.Null, true);

            var edhlg = EditorSettingsParent.GetComponent<HorizontalLayoutGroup>();
            edhlg.childControlWidth = false;
            edhlg.childForceExpandWidth = false;

            layers.transform.AsRT().sizeDelta = new Vector2(100f, 32f);

            BinSlider = EditorSettingsParent.Find("bin").GetComponent<Slider>();
            BinSlider.transform.AsRT().sizeDelta = new Vector2(237f, 32f);
            var binSliderImage = BinSlider.transform.Find("Image").GetComponent<Image>();
            BinSlider.colors = UIManager.SetColorBlock(BinSlider.colors, Color.white, new Color(0.9f, 0.9f, 0.9f), Color.white, Color.white, Color.white);
            EditorThemeManager.ApplyGraphic(binSliderImage, ThemeGroup.Slider_2, true);
            EditorThemeManager.ApplyGraphic(BinSlider.image, ThemeGroup.Slider_2_Handle, true);

            EditorLayerTogglesParent = EditorSettingsParent.Find("layer").AsRT();
            EditorLayerToggles = EditorLayerTogglesParent.GetComponentsInChildren<Toggle>();
            CoreHelper.Destroy(EditorLayerTogglesParent.GetComponent<ToggleGroup>());
            RTEditor.inst.SetupEditorLayers(this);

            EditorHelper.SetComplexity(EditorLayerTogglesParent.gameObject, "editor_layer_toggles", Complexity.Simple);
            EditorHelper.SetComplexity(EditorLayerField.gameObject, "editor_layer_field", Complexity.Normal);

            var label = EditorPrefabHolder.Instance.Labels.Duplicate(LeftContent, "indexer_label");

            var labelText = label.transform.GetChild(0).GetComponent<Text>();
            labelText.text = "Editor Index";
            EditorThemeManager.ApplyLightText(labelText);

            var indexEditor = EditorPrefabHolder.Instance.NumberInputField.Duplicate(LeftContent, "indexer");
            EditorIndexField = indexEditor.GetComponent<InputFieldStorage>();
            if (EditorIndexField.middleButton)
                CoreHelper.Delete(EditorIndexField.middleButton.gameObject);
            EditorThemeManager.ApplyInputField(EditorIndexField);

            var baseColorParent = EditorPrefabHolder.Instance.Labels.Duplicate(LeftContent, "base color");
            baseColorParent.transform.AsRT().sizeDelta = new Vector2(351f, 32f);
            var baseColorLabel = baseColorParent.transform.GetChild(0).GetComponent<Text>();
            baseColorLabel.alignment = TextAnchor.MiddleLeft;
            baseColorLabel.text = "Editor Base Color";
            baseColorLabel.rectTransform.sizeDelta = new Vector2(232f, 32f);
            EditorThemeManager.ApplyLightText(baseColorLabel);
            var baseColor = EditorPrefabHolder.Instance.StringInputField.Duplicate(baseColorParent.transform, "hex");
            BaseColorField = baseColor.GetComponent<InputField>();
            BaseColorField.transform.AsRT().sizeDelta = new Vector2(100f, 32f);
            BaseColorField.GetPlaceholderText().text = "Enter Color";
            BaseColorField.GetPlaceholderText().fontSize = 15;
            EditorThemeManager.ApplyInputField(BaseColorField);

            var selectColorParent = EditorPrefabHolder.Instance.Labels.Duplicate(LeftContent, "select color");
            selectColorParent.transform.AsRT().sizeDelta = new Vector2(351f, 32f);
            var selectColorLabel = selectColorParent.transform.GetChild(0).GetComponent<Text>();
            selectColorLabel.alignment = TextAnchor.MiddleLeft;
            selectColorLabel.text = "Editor Select Color";
            selectColorLabel.rectTransform.sizeDelta = new Vector2(232f, 32f);
            EditorThemeManager.ApplyLightText(selectColorLabel);
            var selectColor = EditorPrefabHolder.Instance.StringInputField.Duplicate(selectColorParent.transform, "hex");
            SelectColorField = selectColor.GetComponent<InputField>();
            SelectColorField.transform.AsRT().sizeDelta = new Vector2(100f, 32f);
            SelectColorField.GetPlaceholderText().text = "Enter Color";
            SelectColorField.GetPlaceholderText().fontSize = 15;
            EditorThemeManager.ApplyInputField(SelectColorField);

            var textColorParent = EditorPrefabHolder.Instance.Labels.Duplicate(LeftContent, "text color");
            textColorParent.transform.AsRT().sizeDelta = new Vector2(351f, 32f);
            var textColorLabel = textColorParent.transform.GetChild(0).GetComponent<Text>();
            textColorLabel.alignment = TextAnchor.MiddleLeft;
            textColorLabel.text = "Editor Text Color";
            textColorLabel.rectTransform.sizeDelta = new Vector2(232f, 32f);
            EditorThemeManager.ApplyLightText(textColorLabel);
            var textColor = EditorPrefabHolder.Instance.StringInputField.Duplicate(textColorParent.transform, "hex");
            TextColorField = textColor.GetComponent<InputField>();
            TextColorField.transform.AsRT().sizeDelta = new Vector2(100f, 32f);
            TextColorField.GetPlaceholderText().text = "Enter Color";
            TextColorField.GetPlaceholderText().fontSize = 15;
            EditorThemeManager.ApplyInputField(TextColorField);

            var markColorParent = EditorPrefabHolder.Instance.Labels.Duplicate(LeftContent, "mark color");
            markColorParent.transform.AsRT().sizeDelta = new Vector2(351f, 32f);
            var markColorLabel = markColorParent.transform.GetChild(0).GetComponent<Text>();
            markColorLabel.alignment = TextAnchor.MiddleLeft;
            markColorLabel.text = "Editor Mark Color";
            markColorLabel.rectTransform.sizeDelta = new Vector2(232f, 32f);
            EditorThemeManager.ApplyLightText(markColorLabel);
            var markColor = EditorPrefabHolder.Instance.StringInputField.Duplicate(markColorParent.transform, "hex");
            MarkColorField = markColor.GetComponent<InputField>();
            MarkColorField.transform.AsRT().sizeDelta = new Vector2(100f, 32f);
            MarkColorField.GetPlaceholderText().text = "Enter Color";
            MarkColorField.GetPlaceholderText().fontSize = 15;
            EditorThemeManager.ApplyInputField(MarkColorField);

            #endregion

            #region Unity Explorer

            if (ModCompatibility.UnityExplorerInstalled)
            {
                RTEditor.GenerateLabels("inspect label", LeftContent, new Label("Unity Explorer"));

                var inspectPrefabObject = EditorPrefabHolder.Instance.Function2Button.Duplicate(LeftContent, "inspect prefab object");
                InspectPrefabObject = inspectPrefabObject.GetComponent<FunctionButtonStorage>();
                InspectPrefabObject.Text = "Inspect Prefab Object";
                EditorThemeManager.ApplySelectable(InspectPrefabObject.button, ThemeGroup.Function_2);
                EditorThemeManager.ApplyGraphic(InspectPrefabObject.label, ThemeGroup.Function_2_Text);

                var inspectRuntimeObject = EditorPrefabHolder.Instance.Function2Button.Duplicate(LeftContent, "inspect runtime object");
                InspectRuntimeObjectButton = inspectRuntimeObject.GetComponent<FunctionButtonStorage>();
                InspectRuntimeObjectButton.Text = "Inspect Runtime Object";
                EditorThemeManager.ApplySelectable(InspectRuntimeObjectButton.button, ThemeGroup.Function_2);
                EditorThemeManager.ApplyGraphic(InspectRuntimeObjectButton.label, ThemeGroup.Function_2_Text);

                var inspectTimelineObject = EditorPrefabHolder.Instance.Function2Button.Duplicate(LeftContent, "inspect timeline object");
                InspectTimelineObject = inspectTimelineObject.GetComponent<FunctionButtonStorage>();
                InspectTimelineObject.Text = "Inspect Timeline Object";
                EditorThemeManager.ApplySelectable(InspectTimelineObject.button, ThemeGroup.Function_2);
                EditorThemeManager.ApplyGraphic(InspectTimelineObject.label, ThemeGroup.Function_2_Text);
            }

            #endregion

            try
            {
                ModifiersDialog = new ModifiersEditorDialog();
                ModifiersDialog.getReference = () => EditorTimeline.inst.CurrentSelection.GetData<Core.Data.Modifiers.IModifierReference>();
                ModifiersDialog.Init(LeftContent);
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Had an error with setting up modifies UI: {ex}");
            }

            #endregion

            #region Global Values

            #region Offset

            var offsetLabel = Right.GetChild(0);
            var offsetLabelText = offsetLabel.GetChild(0).GetComponent<Text>();
            offsetLabelText.text = "Global Offset Time";
            EditorThemeManager.ApplyLightText(offsetLabelText);

            CoreHelper.Delete(Right.Find("time").gameObject);
            var offsetTime = EditorPrefabHolder.Instance.NumberInputField.Duplicate(Right, "time", 1);
            OffsetField = offsetTime.GetComponent<InputFieldStorage>();
            CoreHelper.Delete(OffsetField.middleButton.gameObject);
            EditorThemeManager.ApplyInputField(OffsetField);

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

            EditorThemeManager.ApplyInputField(NameField);
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

            EditorThemeManager.ApplyGraphic(PrefabTypeSelectorButton.button.image, ThemeGroup.Null, true);
            EditorHelper.SetComplexity(type, Complexity.Normal);

            #endregion

            #region Save to a Prefab

            var savePrefabLabel = new Labels(Labels.InitSettings.Default.Name("save prefab label").Parent(Right), new Label("Overwrite External Prefab") { horizontalWrap = HorizontalWrapMode.Overflow });

            var savePrefab = EditorPrefabHolder.Instance.Function2Button.Duplicate(Right, "save prefab");
            SavePrefabButton = savePrefab.GetComponent<FunctionButtonStorage>();
            SavePrefabButton.Text = "Save to a Prefab";

            EditorHelper.SetComplexity(savePrefabLabel.GameObject, Complexity.Normal);
            EditorHelper.SetComplexity(savePrefab, Complexity.Normal);
            EditorThemeManager.ApplySelectable(SavePrefabButton.button, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(SavePrefabButton.label, ThemeGroup.Function_2_Text);

            #endregion

            #region Default Instance Data

            var defaultInstanceDataLabel = new Labels(Labels.InitSettings.Default.Name("default instance data label").Parent(Right), new Label("Default Instance Data"));

            var defaultInstanceData = EditorPrefabHolder.Instance.Function2Button.Duplicate(Right, "default instance data");
            DefaultInstanceDataButton = defaultInstanceData.GetComponent<FunctionButtonStorage>();
            DefaultInstanceDataButton.Text = "Null";

            EditorHelper.SetComplexity(defaultInstanceDataLabel.GameObject, Complexity.Advanced);
            EditorHelper.SetComplexity(defaultInstanceData, Complexity.Advanced);
            EditorThemeManager.ApplySelectable(DefaultInstanceDataButton.button, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(DefaultInstanceDataButton.label, ThemeGroup.Function_2_Text);

            #endregion

            #region Inspect

            if (ModCompatibility.UnityExplorerInstalled)
            {
                var inspectPrefabLabel = new Labels(Labels.InitSettings.Default.Name("inspect prefab label").Parent(Right), new Label("Unity Explorer"));

                var inspectPrefab = EditorPrefabHolder.Instance.Function2Button.Duplicate(Right, "inspect prefab");
                InspectPrefab = inspectPrefab.GetComponent<FunctionButtonStorage>();
                InspectPrefab.Text = "Inspect Prefab";

                EditorHelper.SetComplexity(inspectPrefabLabel.GameObject, Complexity.Advanced);
                EditorHelper.SetComplexity(inspectPrefab, Complexity.Advanced);
                EditorThemeManager.ApplySelectable(InspectPrefab.button, ThemeGroup.Function_2);
                EditorThemeManager.ApplyGraphic(InspectPrefab.label, ThemeGroup.Function_2_Text);
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
