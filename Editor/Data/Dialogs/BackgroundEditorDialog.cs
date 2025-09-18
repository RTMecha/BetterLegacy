using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class BackgroundEditorDialog : EditorDialog, IContentUI, ITagDialog
    {
        public BackgroundEditorDialog() : base(BACKGROUND_EDITOR) { }

        #region Left

        public RectTransform Left { get; set; }

        public RectTransform LeftContent { get; set; }

        public Toggle ActiveToggle { get; set; }

        public InputField NameField { get; set; }

        public RectTransform TagsScrollView { get; set; }

        public RectTransform TagsContent { get; set; }

        #region Start Time / Autokill

        public InputFieldStorage StartTimeField { get; set; }

        public Dropdown AutokillDropdown { get; set; }
        public InputField AutokillField { get; set; }
        public Button AutokillSetButton { get; set; }
        public Toggle CollapseToggle { get; set; }

        #endregion

        #region Transforms

        public Vector2InputFieldStorage PositionFields { get; set; }

        public Vector2InputFieldStorage ScaleFields { get; set; }

        public InputField RotationField { get; set; }

        public Slider RotationSlider { get; set; }

        public InputFieldStorage DepthField { get; set; }

        public InputFieldStorage IterationsField { get; set; }

        public InputFieldStorage ZPositionField { get; set; }

        public InputFieldStorage ZScaleField { get; set; }

        public Vector2InputFieldStorage DepthRotation { get; set; }

        #endregion

        public RectTransform ColorsParent { get; set; }

        public Vector3InputFieldStorage HueSatVal { get; set; }

        public RectTransform FadeColorsParent { get; set; }

        public Vector3InputFieldStorage FadeHueSatVal { get; set; }

        public ToggleButtonStorage FadeToggle { get; set; }

        #region Editor Settings

        public RectTransform EditorSettingsParent { get; set; }
        public Slider BinSlider { get; set; }
        public InputField EditorLayerField { get; set; }
        public Toggle[] EditorLayerToggles { get; set; }

        public InputFieldStorage EditorIndexField { get; set; }

        public InputField BaseColorField { get; set; }
        public InputField SelectColorField { get; set; }
        public InputField TextColorField { get; set; }
        public InputField MarkColorField { get; set; }

        #endregion

        #region Prefab

        public GameObject CollapsePrefabLabel { get; set; }
        public FunctionButtonStorage CollapsePrefabButton { get; set; }
        public GameObject AssignPrefabLabel { get; set; }
        public FunctionButtonStorage AssignPrefabButton { get; set; }
        public FunctionButtonStorage RemovePrefabButton { get; set; }

        #endregion

        #region Reactive

        public List<Toggle> ReactiveRanges { get; set; } = new List<Toggle>();

        public InputField ReactiveIntensityField { get; set; }
        public Slider ReactiveIntensitySlider { get; set; }

        public List<GameObject> CustomReactive { get; set; } = new List<GameObject>();

        public Vector2InputFieldStorage ReactivePositionSamplesFields { get; set; }

        public Vector2InputFieldStorage ReactivePositionIntensityFields { get; set; }
        
        public Vector2InputFieldStorage ReactiveScaleSamplesFields { get; set; }

        public Vector2InputFieldStorage ReactiveScaleIntensityFields { get; set; }

        public InputFieldStorage ReactiveRotationSampleField { get; set; }
        
        public InputFieldStorage ReactiveRotationIntensityField { get; set; }

        public RectTransform ReactiveColorsParent { get; set; }

        public InputFieldStorage ReactiveColorSampleField { get; set; }
        
        public InputFieldStorage ReactiveColorIntensityField { get; set; }
        
        public InputFieldStorage ReactiveZPositionSampleField { get; set; }
        
        public InputFieldStorage ReactiveZPositionIntensityField { get; set; }

        #endregion

        public ModifiersEditorDialog ModifiersDialog { get; set; }

        #endregion

        #region Right

        public RectTransform Right { get; set; }

        public InputField SearchField { get; set; }

        public Transform Content { get; set; }

        public GridLayoutGroup Grid { get; set; }

        public Scrollbar ContentScrollbar { get; set; }

        public string SearchTerm { get => SearchField.text; set => SearchField.text = value; }

        public void ClearContent() => LSHelpers.DeleteChildren(Content);

        #endregion

        public override void Init()
        {
            if (init)
                return;

            base.Init();

            var dialog = GameObject.transform;

            Left = dialog.Find("data/left").AsRT();
            Right = dialog.Find("data/right").AsRT();

            #region Right

            Right.Find("create").GetComponent<HoverTooltip>().tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                desc = "Create New Background Object",
                hint = "Press this to create a new background object."
            });

            var create = Right.Find("create").GetComponent<Button>();
            create.onClick.NewListener(RTBackgroundEditor.inst.CreateNewBackground);

            var destroyAll = Right.Find("create").gameObject.Duplicate(Right, "destroy", 2);
            destroyAll.transform.localScale = Vector3.one;

            var destroyAllText = destroyAll.transform.GetChild(0).GetComponent<Text>();
            destroyAllText.text = "Delete All Backgrounds";
            destroyAll.transform.GetChild(0).localScale = Vector3.one;

            var destroyAllButtons = destroyAll.GetComponent<Button>();
            destroyAllButtons.onClick.NewListener(() => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete all backgrounds?", () =>
            {
                RTBackgroundEditor.inst.DeleteAllBackgrounds();
                RTEditor.inst.HideWarningPopup();
            }, RTEditor.inst.HideWarningPopup));

            var destroyAllTip = destroyAll.GetComponent<HoverTooltip>();

            destroyAllTip.tooltipLangauges.Clear();
            destroyAllTip.tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                desc = "Destroy All Objects",
                hint = "Press this to destroy all background objects, EXCEPT the first one."
            });

            var copy = Right.Find("create").gameObject.Duplicate(Right, "copy", 3);
            copy.transform.localScale = Vector3.one;

            var copyText = copy.transform.GetChild(0).GetComponent<Text>();
            copyText.text = "Copy Backgrounds";
            copy.transform.GetChild(0).localScale = Vector3.one;

            var copyButtons = copy.GetComponent<Button>();
            copyButtons.onClick.NewListener(() =>
            {
                RTBackgroundEditor.inst.copiedBackgroundObjects.Clear();
                RTBackgroundEditor.inst.copiedBackgroundObjects.AddRange(GameData.Current.backgroundObjects.Select(x => x.Copy()));
                EditorManager.inst.DisplayNotification("Copied all Background Objects.", 2f, EditorManager.NotificationType.Success);
            });

            var copyTip = copy.GetComponent<HoverTooltip>();

            copyTip.tooltipLangauges.Clear();
            copyTip.tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                desc = "Copy all backgrounds",
                hint = "Copies all backgrounds."
            });

            var paste = Right.Find("create").gameObject.Duplicate(Right, "paste", 4);
            paste.transform.localScale = Vector3.one;

            var pasteText = paste.transform.GetChild(0).GetComponent<Text>();
            pasteText.text = "Paste Backgrounds";
            paste.transform.GetChild(0).localScale = Vector3.one;

            var pasteButtons = paste.GetComponent<Button>();
            pasteButtons.onClick.NewListener(() =>
            {
                RTBackgroundEditor.inst.PasteBackgrounds();
            });

            var pasteTip = paste.GetComponent<HoverTooltip>();

            pasteTip.tooltipLangauges.Clear();
            pasteTip.tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                desc = "Paste backgrounds",
                hint = "Pastes all backgrounds from copied Backgrounds list."
            });

            var createBGs = Left.Find("name").gameObject.Duplicate(Right, "create bgs", 2);

            var name = createBGs.transform.Find("name").GetComponent<InputField>();

            name.onValueChanged.ClearAll();

            CoreHelper.Destroy(createBGs.transform.Find("active").gameObject);
            name.transform.localScale = Vector3.one;
            name.text = "12";
            name.characterValidation = InputField.CharacterValidation.Integer;
            name.transform.AsRT().sizeDelta = new Vector2(80f, 34f);

            var createAll = Right.Find("create").gameObject.Duplicate(createBGs.transform, "create");
            createAll.transform.localScale = Vector3.one;

            createAll.transform.AsRT().sizeDelta = new Vector2(278f, 34f);
            var createAllText = createAll.transform.GetChild(0).GetComponent<Text>();
            createAllText.text = "Create Backgrounds";
            createAll.transform.GetChild(0).localScale = Vector3.one;

            var buttonCreate = createAll.GetComponent<Button>();
            buttonCreate.onClick.NewListener(() =>
            {
                if (int.TryParse(name.text, out int result) && result >= 0)
                    RTBackgroundEditor.inst.CreateBackgrounds(result);
            });

            Right.Find("backgrounds").AsRT().sizeDelta = new Vector2(366f, 440f);

            SearchField = Right.Find("search").GetComponent<InputField>();
            SearchField.onValueChanged.ClearAll();
            SearchField.onValueChanged.AddListener(_val => RTBackgroundEditor.inst.UpdateBackgroundList());
            Content = Right.Find("backgrounds/viewport/content");
            ContentScrollbar = Right.Find("backgrounds/Scrollbar Vertical").GetComponent<Scrollbar>();

            #region Editor Themes

            EditorThemeManager.AddGraphic(dialog.GetComponent<Image>(), ThemeGroup.Background_1);
            EditorThemeManager.AddGraphic(Right.GetComponent<Image>(), ThemeGroup.Background_3);
            EditorThemeManager.AddInputField(Right.Find("search").GetComponent<InputField>(), ThemeGroup.Search_Field_2);
            EditorThemeManager.AddScrollbar(Right.Find("backgrounds/Scrollbar Vertical").GetComponent<Scrollbar>(), scrollbarGroup: ThemeGroup.Scrollbar_2, handleGroup: ThemeGroup.Scrollbar_2_Handle);
            EditorThemeManager.AddGraphic(create.image, ThemeGroup.Add, true);
            EditorThemeManager.AddGraphic(Right.Find("create").GetChild(0).GetComponent<Text>(), ThemeGroup.Add_Text);
            EditorThemeManager.AddInputField(name);
            EditorThemeManager.AddGraphic(buttonCreate.image, ThemeGroup.Add, true);
            EditorThemeManager.AddGraphic(createAllText, ThemeGroup.Add_Text);
            EditorThemeManager.AddGraphic(destroyAllButtons.image, ThemeGroup.Delete, true);
            EditorThemeManager.AddGraphic(destroyAllText, ThemeGroup.Delete_Text);
            EditorThemeManager.AddGraphic(copyButtons.image, ThemeGroup.Copy, true);
            EditorThemeManager.AddGraphic(copyText, ThemeGroup.Copy_Text);
            EditorThemeManager.AddGraphic(pasteButtons.image, ThemeGroup.Paste, true);
            EditorThemeManager.AddGraphic(pasteText, ThemeGroup.Paste_Text);

            #endregion

            #endregion

            #region Left

            //Set UI Parents
            {
                var listtoadd = new List<Transform>();
                for (int i = 0; i < Left.childCount; i++)
                    listtoadd.Add(Left.GetChild(i));

                var e = EditorPrefabHolder.Instance.ScrollView.Duplicate(Left, "Object Scroll View");

                var scrollView2 = e.transform;

                var content = scrollView2.Find("Viewport/Content");

                var scrollViewRT = scrollView2.AsRT();
                scrollViewRT.anchoredPosition = new Vector2(188f, -353f);
                scrollViewRT.sizeDelta = new Vector2(370f, 690f);

                foreach (var l in listtoadd)
                {
                    l.SetParent(content);
                    l.transform.localScale = Vector3.one;
                }

                LeftContent = content.AsRT();
            }

            // Adjustments
            {
                var position = LeftContent.Find("position");
                var scale = LeftContent.Find("scale");

                PositionFields = position.gameObject.AddComponent<Vector2InputFieldStorage>();
                PositionFields.Assign();
                ScaleFields = scale.gameObject.AddComponent<Vector2InputFieldStorage>();
                ScaleFields.Assign();

                CoreHelper.Destroy(position.GetComponent<HorizontalLayoutGroup>(), true);
                CoreHelper.Destroy(scale.GetComponent<HorizontalLayoutGroup>(), true);

                position.Find("x").GetComponent<HorizontalLayoutGroup>().spacing = 4f;
                position.Find("y").GetComponent<HorizontalLayoutGroup>().spacing = 4f;
                position.Find("x/text-field").AsRT().sizeDelta = new Vector2(125f, 32f);
                position.Find("y/text-field").AsRT().sizeDelta = new Vector2(125f, 32f);

                scale.Find("x").GetComponent<HorizontalLayoutGroup>().spacing = 4f;
                scale.Find("y").GetComponent<HorizontalLayoutGroup>().spacing = 4f;
                scale.Find("x/text-field").AsRT().sizeDelta = new Vector2(125f, 32f);
                scale.Find("y/text-field").AsRT().sizeDelta = new Vector2(125f, 32f);

                LeftContent.Find("color").GetComponent<GridLayoutGroup>().spacing = new Vector2(7.7f, 0f);

                RotationField = LeftContent.Find("rotation/x").GetComponent<InputField>();

                RotationSlider = LeftContent.Find("rotation/slider").GetComponent<Slider>();
                RotationSlider.maxValue = 360f;
                RotationSlider.minValue = -360f;

                FadeToggle = LeftContent.Find("fade").gameObject.GetOrAddComponent<ToggleButtonStorage>();
                FadeToggle.Assign();
            }

            ActiveToggle = LeftContent.Find("name/active").GetComponent<Toggle>();
            NameField = LeftContent.Find("name/name").GetComponent<InputField>();

            var shape = ObjectEditor.inst.Dialog.ShapeTypesParent.gameObject;
            var shapeOption = ObjectEditor.inst.Dialog.ShapeOptionsParent.gameObject;

            var labelShape = EditorPrefabHolder.Instance.Labels.Duplicate(LeftContent, "label", 12);
            labelShape.transform.GetChild(0).GetComponent<Text>().text = "Shape";

            var shapeBG = shape.Duplicate(LeftContent, "shape", 13);

            var shapeOptionBG = shapeOption.Duplicate(LeftContent, "shapesettings", 14);
            var shapeSettings = shapeOptionBG.transform;

            // Depth
            {
                CoreHelper.Delete(LeftContent.Find("depth").gameObject);

                var iterations = LeftContent.Find("position").gameObject.Duplicate(LeftContent, "depth", 3);
                CoreHelper.Delete(iterations.transform.GetChild(1).gameObject);

                DepthField = iterations.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
                DepthField.Assign();
            }

            // Iterations
            {
                var iLabel = EditorPrefabHolder.Instance.Labels.Duplicate(LeftContent, "label", 4);
                iLabel.transform.GetChild(0).GetComponent<Text>().text = "Iterations";

                var iterations = LeftContent.Find("position").gameObject.Duplicate(LeftContent, "iterations", 5);
                CoreHelper.Delete(iterations.transform.GetChild(1).gameObject);

                IterationsField = iterations.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
                IterationsField.Assign();
            }

            // ZPosition
            {
                var iLabel = EditorPrefabHolder.Instance.Labels.Duplicate(LeftContent, "label", 8);
                iLabel.transform.GetChild(0).GetComponent<Text>().text = "Position Z";

                var iterations = LeftContent.Find("position").gameObject.Duplicate(LeftContent, "zposition", 9);
                CoreHelper.Delete(iterations.transform.GetChild(1).gameObject);

                ZPositionField = iterations.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
                ZPositionField.Assign();
            }

            // ZScale
            {
                var iLabel = EditorPrefabHolder.Instance.Labels.Duplicate(LeftContent, "label", 12);
                iLabel.transform.GetChild(0).GetComponent<Text>().text = "Scale Z";

                var iterations = LeftContent.Find("position").gameObject.Duplicate(LeftContent, "zscale", 13);
                CoreHelper.Delete(iterations.transform.GetChild(1).gameObject);

                ZScaleField = iterations.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
                ZScaleField.Assign();
            }

            // Reactive
            {
                var reactiveRanges = LeftContent.Find("reactive-ranges");
                CoreHelper.Destroy(reactiveRanges.GetComponent<ToggleGroup>());

                reactiveRanges.GetComponent<GridLayoutGroup>().cellSize = new Vector2(62f, 32f);

                for (int i = 0; i < reactiveRanges.childCount; i++)
                {
                    var reactiveRange = reactiveRanges.GetChild(i);
                    if (reactiveRange.gameObject.TryGetComponent(out Toggle reactiveRangeToggle))
                    {
                        reactiveRangeToggle.group = null;
                        ReactiveRanges.Add(reactiveRangeToggle);

                        var label = reactiveRange.Find("Label").GetComponent<Text>();
                        label.fontSize = 16;
                    }
                }

                var custom = reactiveRanges.GetChild(3).gameObject.Duplicate(reactiveRanges, "custom");
                custom.transform.GetChild(1).GetComponent<Text>().text = "Custom";

                ReactiveRanges.Add(custom.GetComponent<Toggle>());

                var reactive = LeftContent.Find("reactive");

                ReactiveIntensityField = reactive.Find("x").GetComponent<InputField>();
                ReactiveIntensitySlider = reactive.Find("slider").GetComponent<Slider>();
                ReactiveIntensitySlider.transform.AsRT().sizeDelta = new Vector2(205f, 32f);

                // Reactive Position
                {
                    // Samples
                    CustomReactive.Add(RTEditor.GenerateLabels("label", LeftContent, 24, false,
                        new Label("Reactive Position Samples") { horizontalWrap = HorizontalWrapMode.Overflow }));

                    var reactivePositionSamples = LeftContent.Find("position").gameObject.Duplicate(LeftContent, "reactive-position-samples", 25);
                    ReactivePositionSamplesFields = reactivePositionSamples.AddComponent<Vector2InputFieldStorage>();
                    ReactivePositionSamplesFields.Assign();
                    CustomReactive.Add(reactivePositionSamples);

                    // Intensity
                    CustomReactive.Add(RTEditor.GenerateLabels("label", LeftContent, 26, false,
                        new Label("Reactive Position Intensity") { horizontalWrap = HorizontalWrapMode.Overflow }));

                    var reactivePositionIntensity = LeftContent.Find("position").gameObject.Duplicate(LeftContent, "reactive-position-intensity", 27);
                    ReactivePositionIntensityFields = reactivePositionIntensity.AddComponent<Vector2InputFieldStorage>();
                    ReactivePositionIntensityFields.Assign();
                    CustomReactive.Add(reactivePositionIntensity);
                }

                // Reactive Scale
                {
                    // Samples
                    CustomReactive.Add(RTEditor.GenerateLabels("label", LeftContent, 28, false,
                        new Label("Reactive Scale Samples") { horizontalWrap = HorizontalWrapMode.Overflow }));

                    var reactiveScaleSamples = LeftContent.Find("position").gameObject.Duplicate(LeftContent, "reactive-scale-samples", 29);
                    ReactiveScaleSamplesFields = reactiveScaleSamples.AddComponent<Vector2InputFieldStorage>();
                    ReactiveScaleSamplesFields.Assign();
                    CustomReactive.Add(reactiveScaleSamples);

                    // Intensity
                    CustomReactive.Add(RTEditor.GenerateLabels("label", LeftContent, 30, false,
                        new Label("Reactive Scale Intensity") { horizontalWrap = HorizontalWrapMode.Overflow }));

                    var reactiveScaleIntensity = LeftContent.Find("position").gameObject.Duplicate(LeftContent, "reactive-scale-intensity", 31);
                    ReactiveScaleIntensityFields = reactiveScaleIntensity.AddComponent<Vector2InputFieldStorage>();
                    ReactiveScaleIntensityFields.Assign();
                    CustomReactive.Add(reactiveScaleIntensity);
                }

                // Reactive Rotation
                {
                    // Sample
                    CustomReactive.Add(RTEditor.GenerateLabels("label", LeftContent, 32, false,
                        new Label("Reactive Rotation Samples") { horizontalWrap = HorizontalWrapMode.Overflow }));

                    var reactiveRotationSample = LeftContent.Find("position").gameObject.Duplicate(LeftContent, "reactive-rotation-sample", 33);

                    CoreHelper.Delete(reactiveRotationSample.transform.Find("y").gameObject);

                    ReactiveRotationSampleField = reactiveRotationSample.AddComponent<InputFieldStorage>();
                    ReactiveRotationSampleField.Assign(reactiveRotationSample.transform.Find("x").gameObject);
                    CustomReactive.Add(reactiveRotationSample);

                    // Intensity
                    CustomReactive.Add(RTEditor.GenerateLabels("label", LeftContent, 34, false,
                        new Label("Reactive Rotation Intensity") { horizontalWrap = HorizontalWrapMode.Overflow }));

                    var reactiveRotationIntensity = LeftContent.Find("position").gameObject.Duplicate(LeftContent, "reactive-rotation-intensity", 35);

                    CoreHelper.Delete(reactiveRotationIntensity.transform.Find("y").gameObject);

                    ReactiveRotationIntensityField = reactiveRotationIntensity.AddComponent<InputFieldStorage>();
                    ReactiveRotationIntensityField.Assign(reactiveRotationIntensity.transform.Find("x").gameObject);
                    CustomReactive.Add(reactiveRotationIntensity);
                }

                // Reactive Color
                {
                    // Sample
                    CustomReactive.Add(RTEditor.GenerateLabels("label", LeftContent, 36, false,
                        new Label("Reactive Color Sample") { horizontalWrap = HorizontalWrapMode.Overflow }));

                    var reactiveColorSample = LeftContent.Find("position").gameObject.Duplicate(LeftContent, "reactive-color-sample", 37);

                    CoreHelper.Delete(reactiveColorSample.transform.Find("y").gameObject);

                    ReactiveColorSampleField = reactiveColorSample.AddComponent<InputFieldStorage>();
                    ReactiveColorSampleField.Assign(reactiveColorSample.transform.Find("x").gameObject);
                    CustomReactive.Add(reactiveColorSample);

                    // Intensity
                    CustomReactive.Add(RTEditor.GenerateLabels("label", LeftContent, 38, false,
                        new Label("Reactive Color Intensity") { horizontalWrap = HorizontalWrapMode.Overflow }));

                    var reactiveColorIntensity = LeftContent.Find("position").gameObject.Duplicate(LeftContent, "reactive-color-intensity", 39);

                    CoreHelper.Delete(reactiveColorIntensity.transform.Find("y").gameObject);

                    ReactiveColorIntensityField = reactiveColorIntensity.AddComponent<InputFieldStorage>();
                    ReactiveColorIntensityField.Assign(reactiveColorIntensity.transform.Find("x").gameObject);
                    CustomReactive.Add(reactiveColorIntensity);

                    // Color Slot
                    CustomReactive.Add(RTEditor.GenerateLabels("label", LeftContent, 40, false,
                        new Label("Reactive Color") { horizontalWrap = HorizontalWrapMode.Overflow }));

                    var reactiveColor = LeftContent.Find("color").gameObject.Duplicate(LeftContent, "reactive-color", 41);
                    CustomReactive.Add(reactiveColor);
                    ReactiveColorsParent = reactiveColor.transform.AsRT();
                }

                // Reactive Z
                {
                    // Sample
                    CustomReactive.Add(RTEditor.GenerateLabels("label", LeftContent, 42, false,
                        new Label("Reactive Z Sample") { horizontalWrap = HorizontalWrapMode.Overflow }));

                    var reactiveZPositionSample = LeftContent.Find("position").gameObject.Duplicate(LeftContent, "reactive-z-sample", 43);

                    CoreHelper.Delete(reactiveZPositionSample.transform.Find("y").gameObject);

                    ReactiveZPositionSampleField = reactiveZPositionSample.AddComponent<InputFieldStorage>();
                    ReactiveZPositionSampleField.Assign(reactiveZPositionSample.transform.Find("x").gameObject);
                    CustomReactive.Add(reactiveZPositionSample);

                    // Intensity
                    CustomReactive.Add(RTEditor.GenerateLabels("label", LeftContent, 44, false,
                        new Label("Reactive Z Intensity") { horizontalWrap = HorizontalWrapMode.Overflow }));

                    var reactiveZPositionIntensity = LeftContent.Find("position").gameObject.Duplicate(LeftContent, "reactive-z-intensity", 45);

                    CoreHelper.Delete(reactiveZPositionIntensity.transform.Find("y").gameObject);

                    ReactiveZPositionIntensityField = reactiveZPositionIntensity.AddComponent<InputFieldStorage>();
                    ReactiveZPositionIntensityField.Assign(reactiveZPositionIntensity.transform.Find("x").gameObject);
                    CustomReactive.Add(reactiveZPositionIntensity);
                }
            }

            // Fade Color
            {
                var colorLabel = EditorPrefabHolder.Instance.Labels.Duplicate(LeftContent, "label", 16);
                colorLabel.transform.GetChild(0).GetComponent<Text>().text = "Fade Color";

                var color = LeftContent.Find("color");
                var fadeColor = LeftContent.Find("color").gameObject.Duplicate(LeftContent, "fade-color", 17);

                ColorsParent = color.AsRT();
                FadeColorsParent = fadeColor.transform.AsRT();
            }

            // Rotation
            {
                var index = LeftContent.Find("rotation").GetSiblingIndex() + 2;

                var iLabel = EditorPrefabHolder.Instance.Labels.Duplicate(LeftContent, "label", index - 1);
                iLabel.transform.GetChild(0).GetComponent<Text>().text = "3D Rotation";

                var iterations = LeftContent.Find("position").gameObject.Duplicate(LeftContent, "depth-rotation", index);
                DepthRotation = iterations.gameObject.GetOrAddComponent<Vector2InputFieldStorage>();
                DepthRotation.Assign();
            }

            // Hue / Sat / Val (Fade)
            {
                var colorLabel = EditorPrefabHolder.Instance.Labels.Duplicate(LeftContent, "label", 20);
                colorLabel.GetOrAddComponent<HorizontalLayoutGroup>();

                var label1 = colorLabel.transform.GetChild(0);
                label1.GetComponent<Text>().text = "Hue";
                label1.transform.AsRT().sizeDelta = new Vector2(100f, 20f);
                var label2 = label1.gameObject.Duplicate(colorLabel.transform, "text");
                label2.GetComponent<Text>().text = "Saturation";
                label2.transform.AsRT().sizeDelta = new Vector2(100f, 20f);
                var label3 = label1.gameObject.Duplicate(colorLabel.transform, "text");
                label3.GetComponent<Text>().text = "Value";
                label3.transform.AsRT().sizeDelta = new Vector2(100f, 20f);

                var iterations = LeftContent.Find("position").gameObject.Duplicate(LeftContent, "fadehuesatval", 21);
                iterations.GetOrAddComponent<HorizontalLayoutGroup>();
                FadeHueSatVal = iterations.gameObject.GetOrAddComponent<Vector3InputFieldStorage>();
                FadeHueSatVal.Assign();

                FadeHueSatVal.x.inputField.transform.AsRT().sizeDelta = new Vector2(70f, 32f);
                FadeHueSatVal.y.inputField.transform.AsRT().sizeDelta = new Vector2(70f, 32f);

                FadeHueSatVal.z = iterations.transform.Find("x").gameObject.Duplicate(iterations.transform, "z").GetOrAddComponent<InputFieldStorage>();
                FadeHueSatVal.z.Assign();

                for (int i = 0; i < iterations.transform.childCount; i++)
                    iterations.transform.GetChild(i).GetChild(0).AsRT().sizeDelta = new Vector2(70f, 32f);
            }

            // Hue / Sat / Val (Color)
            {
                var colorLabel = EditorPrefabHolder.Instance.Labels.Duplicate(LeftContent, "label", 24);
                colorLabel.GetOrAddComponent<HorizontalLayoutGroup>();

                var label1 = colorLabel.transform.GetChild(0);
                label1.GetComponent<Text>().text = "Hue";
                label1.transform.AsRT().sizeDelta = new Vector2(100f, 20f);
                var label2 = label1.gameObject.Duplicate(colorLabel.transform, "text");
                label2.GetComponent<Text>().text = "Saturation";
                label2.transform.AsRT().sizeDelta = new Vector2(100f, 20f);
                var label3 = label1.gameObject.Duplicate(colorLabel.transform, "text");
                label3.GetComponent<Text>().text = "Value";
                label3.transform.AsRT().sizeDelta = new Vector2(100f, 20f);

                var iterations = LeftContent.Find("position").gameObject.Duplicate(LeftContent, "huesatval", 25);
                iterations.GetOrAddComponent<HorizontalLayoutGroup>();
                HueSatVal = iterations.gameObject.GetOrAddComponent<Vector3InputFieldStorage>();
                HueSatVal.Assign();

                HueSatVal.x.inputField.transform.AsRT().sizeDelta = new Vector2(70f, 32f);
                HueSatVal.y.inputField.transform.AsRT().sizeDelta = new Vector2(70f, 32f);

                HueSatVal.z = iterations.transform.Find("x").gameObject.Duplicate(iterations.transform, "z").GetOrAddComponent<InputFieldStorage>();
                HueSatVal.z.Assign();

                for (int i = 0; i < iterations.transform.childCount; i++)
                    iterations.transform.GetChild(i).GetChild(0).AsRT().sizeDelta = new Vector2(70f, 32f);
            }

            // Tags
            {
                var iLabel = EditorPrefabHolder.Instance.Labels.Duplicate(LeftContent, "label", 2);
                iLabel.transform.GetChild(0).GetComponent<Text>().text = "Tags";

                // Tags Scroll View/Viewport/Content
                var tagScrollView = Creator.NewUIObject("Tags Scroll View", LeftContent, 3);
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
            }

            // Modifiers
            {
                try
                {
                    ModifiersDialog = new ModifiersEditorDialog();
                    ModifiersDialog.Init(LeftContent);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogError($"Had an error with setting up modifies UI: {ex}");
                }
            }

            // Start Time
            {
                RTEditor.GenerateLabels("label", LeftContent, 4, false,
                    new Label("Start Time") { horizontalWrap = HorizontalWrapMode.Overflow });

                var time = ObjectEditor.inst.Dialog.StartTimeField.gameObject.Duplicate(LeftContent, "time", 5);
                StartTimeField = time.GetComponent<InputFieldStorage>();

                EditorThemeManager.AddInputField(StartTimeField.inputField);
                EditorThemeManager.AddSelectable(StartTimeField.leftGreaterButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(StartTimeField.leftButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(StartTimeField.middleButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(StartTimeField.rightButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(StartTimeField.rightGreaterButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddToggle(StartTimeField.lockToggle);
            }

            // Autokill
            {
                RTEditor.GenerateLabels("label", LeftContent, 6, false,
                    new Label("Time of Death") { horizontalWrap = HorizontalWrapMode.Overflow });

                var autokill = ObjectEditor.inst.Dialog.AutokillDropdown.transform.parent.gameObject.Duplicate(LeftContent, "autokill", 7);

                AutokillDropdown = autokill.transform.Find("tod-dropdown").GetComponent<Dropdown>();
                TooltipHelper.AssignTooltip(AutokillDropdown.gameObject, "Autokill Type");
                AutokillField = autokill.transform.Find("tod-value").GetComponent<InputField>();
                AutokillSetButton = autokill.transform.Find("|").GetComponent<Button>();
                CollapseToggle = autokill.transform.Find("collapse").GetComponent<Toggle>();

                CoreHelper.Destroy(AutokillSetButton.GetComponent<Animator>());
                AutokillSetButton.transition = Selectable.Transition.ColorTint;

                EditorThemeManager.AddDropdown(AutokillDropdown);
                EditorThemeManager.AddInputField(AutokillField);
                EditorThemeManager.AddSelectable(AutokillSetButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddToggle(CollapseToggle, ThemeGroup.Background_1);
                for (int i = 0; i < CollapseToggle.transform.Find("dots").childCount; i++)
                    EditorThemeManager.AddGraphic(CollapseToggle.transform.Find("dots").GetChild(i).GetComponent<Image>(), ThemeGroup.Dark_Text);
            }

            // Editor Settings
            {
                RTEditor.GenerateLabels("editorlabel", LeftContent, 35, false,
                    new Label("Editor Layer") { horizontalWrap = HorizontalWrapMode.Overflow },
                    new Label("Editor Bin") { horizontalWrap = HorizontalWrapMode.Overflow }
                    );

                EditorSettingsParent = ObjectEditor.inst.Dialog.EditorSettingsParent.gameObject.Duplicate(LeftContent, "editor", 36).transform.AsRT();
                try
                {
                    EditorLayerField = EditorSettingsParent.Find("layers").GetComponent<InputField>();
                    EditorLayerField.image = EditorLayerField.GetComponent<Image>();
                    BinSlider = EditorSettingsParent.Find("bin").GetComponent<Slider>();

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

                    RTEditor.GenerateLabels("indexer_label", LeftContent, 37, false,
                        new Label("Editor Index") { horizontalWrap = HorizontalWrapMode.Overflow });

                    var indexer = EditorPrefabHolder.Instance.NumberInputField.Duplicate(LeftContent, "indexer", 38);
                    EditorIndexField = indexer.GetComponent<InputFieldStorage>();
                    CoreHelper.Delete(EditorIndexField.middleButton);
                    EditorThemeManager.AddInputField(EditorIndexField);

                    var baseColorParent = EditorPrefabHolder.Instance.Labels.Duplicate(LeftContent, "base color", 39);
                    baseColorParent.transform.AsRT().sizeDelta = new Vector2(351f, 32f);
                    var baseColorLabel = baseColorParent.transform.GetChild(0).GetComponent<Text>();
                    baseColorLabel.alignment = TextAnchor.MiddleLeft;
                    baseColorLabel.text = "Editor Base Color";
                    baseColorLabel.rectTransform.sizeDelta = new Vector2(232f, 32f);
                    EditorThemeManager.AddLightText(baseColorLabel);
                    var baseColor = EditorPrefabHolder.Instance.StringInputField.Duplicate(baseColorParent.transform, "hex");
                    BaseColorField = baseColor.GetComponent<InputField>();
                    BaseColorField.transform.AsRT().sizeDelta = new Vector2(100f, 32f);
                    BaseColorField.GetPlaceholderText().text = "Enter Color";
                    BaseColorField.GetPlaceholderText().fontSize = 15;
                    EditorThemeManager.AddInputField(BaseColorField);

                    var selectColorParent = EditorPrefabHolder.Instance.Labels.Duplicate(LeftContent, "select color", 40);
                    selectColorParent.transform.AsRT().sizeDelta = new Vector2(351f, 32f);
                    var selectColorLabel = selectColorParent.transform.GetChild(0).GetComponent<Text>();
                    selectColorLabel.alignment = TextAnchor.MiddleLeft;
                    selectColorLabel.text = "Editor Select Color";
                    selectColorLabel.rectTransform.sizeDelta = new Vector2(232f, 32f);
                    EditorThemeManager.AddLightText(selectColorLabel);
                    var selectColor = EditorPrefabHolder.Instance.StringInputField.Duplicate(selectColorParent.transform, "hex");
                    SelectColorField = selectColor.GetComponent<InputField>();
                    SelectColorField.transform.AsRT().sizeDelta = new Vector2(100f, 32f);
                    SelectColorField.GetPlaceholderText().text = "Enter Color";
                    SelectColorField.GetPlaceholderText().fontSize = 15;
                    EditorThemeManager.AddInputField(SelectColorField);

                    var textColorParent = EditorPrefabHolder.Instance.Labels.Duplicate(LeftContent, "text color", 41);
                    textColorParent.transform.AsRT().sizeDelta = new Vector2(351f, 32f);
                    var textColorLabel = textColorParent.transform.GetChild(0).GetComponent<Text>();
                    textColorLabel.alignment = TextAnchor.MiddleLeft;
                    textColorLabel.text = "Editor Text Color";
                    textColorLabel.rectTransform.sizeDelta = new Vector2(232f, 32f);
                    EditorThemeManager.AddLightText(textColorLabel);
                    var textColor = EditorPrefabHolder.Instance.StringInputField.Duplicate(textColorParent.transform, "hex");
                    TextColorField = textColor.GetComponent<InputField>();
                    TextColorField.transform.AsRT().sizeDelta = new Vector2(100f, 32f);
                    TextColorField.GetPlaceholderText().text = "Enter Color";
                    TextColorField.GetPlaceholderText().fontSize = 15;
                    EditorThemeManager.AddInputField(TextColorField);

                    var markColorParent = EditorPrefabHolder.Instance.Labels.Duplicate(LeftContent, "mark color", 42);
                    markColorParent.transform.AsRT().sizeDelta = new Vector2(351f, 32f);
                    var markColorLabel = markColorParent.transform.GetChild(0).GetComponent<Text>();
                    markColorLabel.alignment = TextAnchor.MiddleLeft;
                    markColorLabel.text = "Editor Mark Color";
                    markColorLabel.rectTransform.sizeDelta = new Vector2(232f, 32f);
                    EditorThemeManager.AddLightText(markColorLabel);
                    var markColor = EditorPrefabHolder.Instance.StringInputField.Duplicate(markColorParent.transform, "hex");
                    MarkColorField = markColor.GetComponent<InputField>();
                    MarkColorField.transform.AsRT().sizeDelta = new Vector2(100f, 32f);
                    MarkColorField.GetPlaceholderText().text = "Enter Color";
                    MarkColorField.GetPlaceholderText().fontSize = 15;
                    EditorThemeManager.AddInputField(MarkColorField);

                }
                catch (Exception ex)
                {
                    CoreHelper.LogError($"Had an error in generating editor settings UI for BG editor. Exception: {ex}");
                }
            }

            // Prefab Reference
            {
                CollapsePrefabLabel = RTEditor.GenerateLabels("collapselabel", LeftContent, 43, false,
                    new Label("Prefab Collapse / Apply to All") { horizontalWrap = HorizontalWrapMode.Overflow });

                var collapsePrefab = EditorPrefabHolder.Instance.Function2Button.Duplicate(LeftContent, "applyprefab", 40);
                CollapsePrefabButton = collapsePrefab.GetComponent<FunctionButtonStorage>();
                CollapsePrefabButton.label.text = "Apply";

                EditorThemeManager.AddSelectable(CollapsePrefabButton.button, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(CollapsePrefabButton.label, ThemeGroup.Function_2_Text);

                AssignPrefabLabel = RTEditor.GenerateLabels("assignlabel", LeftContent, 44, false,
                    new Label("Assign Object to Prefab") { horizontalWrap = HorizontalWrapMode.Overflow });

                var assignPrefab = EditorPrefabHolder.Instance.Function2Button.Duplicate(LeftContent, "assignprefab", 45);
                AssignPrefabButton = assignPrefab.GetComponent<FunctionButtonStorage>();
                AssignPrefabButton.label.text = "Assign";

                EditorThemeManager.AddSelectable(AssignPrefabButton.button, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(AssignPrefabButton.label, ThemeGroup.Function_2_Text);

                var removePrefab = EditorPrefabHolder.Instance.Function2Button.Duplicate(LeftContent, "removeprefab", 46);
                RemovePrefabButton = removePrefab.GetComponent<FunctionButtonStorage>();
                RemovePrefabButton.label.text = "Remove";

                EditorThemeManager.AddSelectable(RemovePrefabButton.button, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(RemovePrefabButton.label, ThemeGroup.Function_2_Text);
            }

            //// Depth
            //{
            //    RTEditor.GenerateLabels("label", LeftContent, 8, false,
            //               new LabelSettings("Background Layer") { horizontalWrap = HorizontalWrapMode.Overflow });

            //    var iterations = LeftContent.Find("position").gameObject.Duplicate(LeftContent, "layer", 9);
            //    CoreHelper.Delete(iterations.transform.GetChild(1).gameObject);

            //    LayerField = iterations.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            //    LayerField.Assign();
            //}

            CoreHelper.Destroy(ActiveToggle.GetComponent<Animator>());
            ActiveToggle.transition = Selectable.Transition.ColorTint;
            ActiveToggle.colors = UIManager.SetColorBlock(ActiveToggle.colors, Color.white, new Color(0.7f, 0.7f, 0.7f), new Color(0.7f, 0.7f, 0.7f), new Color(0.7f, 0.7f, 0.7f), new Color(0.7f, 0.7f, 0.7f));
            EditorThemeManager.AddToggle(ActiveToggle);
            LeftContent.Find("name/name").AsRT().sizeDelta = new Vector2(300f, 32f);
            EditorThemeManager.AddInputField(NameField);
            //EditorThemeManager.AddInputFields(LeftContent.Find("layer").gameObject, true, "");
            EditorThemeManager.AddInputFields(LeftContent.Find("depth").gameObject, true, "");
            EditorThemeManager.AddInputFields(LeftContent.Find("iterations").gameObject, true, "");
            EditorThemeManager.AddInputFields(LeftContent.Find("zposition").gameObject, true, "");
            EditorThemeManager.AddInputFields(LeftContent.Find("zscale").gameObject, true, "");
            EditorThemeManager.AddInputFields(LeftContent.Find("position").gameObject, true, "");
            EditorThemeManager.AddInputFields(LeftContent.Find("scale").gameObject, true, "");
            EditorThemeManager.AddInputFields(LeftContent.Find("depth-rotation").gameObject, true, "");
            EditorThemeManager.AddInputField(LeftContent.Find("rotation/x").GetComponent<InputField>());

            EditorThemeManager.AddInputFields(LeftContent.Find("fadehuesatval").gameObject, true, "");
            EditorThemeManager.AddInputFields(LeftContent.Find("huesatval").gameObject, true, "");

            var rotationSliderImage = LeftContent.Find("rotation/slider/Image").GetComponent<Image>();
            var rotationSlider = LeftContent.Find("rotation/slider").GetComponent<Slider>();
            rotationSlider.colors = UIManager.SetColorBlock(rotationSlider.colors, Color.white, new Color(0.9f, 0.9f, 0.9f), Color.white, Color.white, Color.white);
            rotationSlider.transform.AsRT().sizeDelta = new Vector2(207f, 32f);

            EditorThemeManager.AddSlider(rotationSlider, rotationSliderImage);

            for (int i = 0; i < LeftContent.Find("reactive-ranges").childCount; i++)
            {
                var child = LeftContent.Find("reactive-ranges").GetChild(i);
                var toggle = child.GetComponent<Toggle>();
                var background = toggle.image;
                var checkmark = toggle.graphic;

                EditorThemeManager.AddGraphic(background, ThemeGroup.Function_2_Normal, true);
                EditorThemeManager.AddGraphic(checkmark, ThemeGroup.Function_2_Highlighted);
                EditorThemeManager.AddGraphic(child.Find("Label").GetComponent<Text>(), ThemeGroup.Function_2_Text);
            }

            EditorThemeManager.AddInputField(LeftContent.Find("reactive/x").GetComponent<InputField>());

            var reactiveSliderImage = LeftContent.Find("reactive/slider/Image").GetComponent<Image>();
            var reactiveSlider = LeftContent.Find("reactive/slider").GetComponent<Slider>();
            reactiveSlider.colors = UIManager.SetColorBlock(reactiveSlider.colors, Color.white, new Color(0.9f, 0.9f, 0.9f), Color.white, Color.white, Color.white);
            reactiveSlider.transform.AsRT().sizeDelta = new Vector2(207f, 32f);

            EditorThemeManager.AddSlider(reactiveSlider, reactiveSliderImage);

            EditorThemeManager.AddInputFields(LeftContent.Find("reactive-position-samples").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(LeftContent.Find("reactive-position-intensity").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(LeftContent.Find("reactive-scale-samples").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(LeftContent.Find("reactive-scale-intensity").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(LeftContent.Find("reactive-rotation-sample").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(LeftContent.Find("reactive-rotation-intensity").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(LeftContent.Find("reactive-color-sample").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(LeftContent.Find("reactive-color-intensity").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(LeftContent.Find("reactive-z-sample").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(LeftContent.Find("reactive-z-intensity").gameObject, true, "Background Editor Reactive");

            var fade = LeftContent.Find("fade");
            var fadeToggle = fade.GetComponent<Toggle>();
            var fadeBackground = fadeToggle.image;
            var fadeCheckmark = fadeToggle.graphic;

            EditorThemeManager.AddGraphic(fadeBackground, ThemeGroup.Function_2_Normal, true);
            EditorThemeManager.AddGraphic(fadeCheckmark, ThemeGroup.Function_2_Highlighted, true);
            EditorThemeManager.AddGraphic(fade.Find("Label").GetComponent<Text>(), ThemeGroup.Function_2_Text, true);

            // Labels
            for (int i = 0; i < LeftContent.childCount; i++)
            {
                var child = LeftContent.GetChild(i);
                if (child.name != "label")
                    continue;

                for (int j = 0; j < child.childCount; j++)
                    EditorThemeManager.AddLightText(child.GetChild(j).GetComponent<Text>());
            }

            #endregion
        }
    }
}
