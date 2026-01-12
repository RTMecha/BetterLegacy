using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using TMPro;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    /// <summary>
    /// Represents the object editor dialog for editing a <see cref="BeatmapObject"/>.
    /// </summary>
    public class ObjectEditorDialog : EditorDialog, ITagDialog, IParentDialog, IAnimationDialog, IEditorLayerUI, IPrefabableDialog, IShapeableDialog
    {
        public ObjectEditorDialog() : base(OBJECT_EDITOR) { }

        #region Object Values

        public RectTransform Content { get; set; }

        #region Top Properties

        public RectTransform IDBase { get; set; }
        public Text IDText { get; set; }
        public Text LDMLabel { get; set; }
        public Toggle LDMToggle { get; set; }

        #endregion

        #region Name Area

        public InputField NameField { get; set; }
        public Dropdown ObjectTypeDropdown { get; set; }
        public RectTransform TagsScrollView { get; set; }
        public RectTransform TagsContent { get; set; }

        #endregion

        #region Start Time / Autokill

        public InputFieldStorage StartTimeField { get; set; }

        public Dropdown AutokillDropdown { get; set; }
        public InputField AutokillField { get; set; }
        public Button AutokillSetButton { get; set; }
        public Toggle CollapseToggle { get; set; }

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

        #region Origin

        public RectTransform OriginParent { get; set; }
        public InputFieldStorage OriginXField { get; set; }
        public InputFieldStorage OriginYField { get; set; }

        public List<Toggle> OriginXToggles { get; set; } = new List<Toggle>();
        public List<Toggle> OriginYToggles { get; set; } = new List<Toggle>();

        #endregion

        #region Gradient / Shape

        public Text ColorBlendModeLabel { get; set; }
        public Dropdown ColorBlendModeDropdown { get; set; }

        public Text ShapesLabel { get; set; }

        public RectTransform GradientParent { get; set; }
        public List<Toggle> GradientToggles { get; set; } = new List<Toggle>();
        public InputFieldStorage GradientScale { get; set; }
        public InputFieldStorage GradientRotation { get; set; }

        public RectTransform ShapeTypesParent { get; set; }
        public RectTransform ShapeOptionsParent { get; set; }

        public List<Toggle> ShapeToggles { get; set; } = new List<Toggle>();
        public List<List<Toggle>> ShapeOptionToggles { get; set; } = new List<List<Toggle>>();

        public List<int> UnsupportedShapes { get; set; }

        #endregion

        #region Render Depth / Type

        public RectTransform DepthParent { get; set; }
        public InputFieldStorage DepthField { get; set; }
        public Slider DepthSlider { get; set; }
        public Button DepthSliderLeftButton { get; set; }
        public Button DepthSliderRightButton { get; set; }
        public Dropdown RenderTypeDropdown { get; set; }

        #endregion

        #region Editor Settings

        public RectTransform EditorSettingsParent { get; set; }
        public InputField EditorLayerField { get; set; }
        public RectTransform EditorLayerTogglesParent { get; set; }
        public Toggle[] EditorLayerToggles { get; set; }
        public Slider BinSlider { get; set; }

        public InputFieldStorage EditorIndexField { get; set; }
        public InputField EditorGroupField { get; set; }

        public InputField BaseColorField { get; set; }
        public InputField SelectColorField { get; set; }
        public InputField TextColorField { get; set; }
        public InputField MarkColorField { get; set; }

        #endregion

        #region Prefab

        public GameObject PrefabName { get; set; }
        public Text PrefabNameText { get; set; }
        public GameObject CollapsePrefabLabel { get; set; }
        public FunctionButtonStorage CollapsePrefabButton { get; set; }
        public GameObject AssignPrefabLabel { get; set; }
        public FunctionButtonStorage AssignPrefabButton { get; set; }
        public FunctionButtonStorage RemovePrefabButton { get; set; }

        #endregion

        #region Unity Explorer

        public Text UnityExplorerLabel { get; set; }

        public FunctionButtonStorage InspectBeatmapObjectButton { get; set; }
        public FunctionButtonStorage InspectRuntimeObjectButton { get; set; }
        public FunctionButtonStorage InspectTimelineObjectButton { get; set; }

        #endregion

        public InputField AnimIDField { get; set; }

        public ModifiersEditorDialog ModifiersDialog { get; set; }

        #endregion

        #region Keyframe Editors

        /// <summary>
        /// The currently open object keyframe editor.
        /// </summary>
        public KeyframeDialog CurrentKeyframeDialog { get; set; }

        /// <summary>
        /// A list containing all the event keyframe editors.
        /// </summary>
        public List<KeyframeDialog> keyframeDialogs = new List<KeyframeDialog>();
        public List<KeyframeDialog> KeyframeDialogs { get => keyframeDialogs; set => keyframeDialogs = value; }

        public KeyframeTimeline Timeline { get; set; }

        #endregion

        public override void Init()
        {
            if (init)
                return;

            base.Init();

            Content = ObjEditor.inst.ObjectView.transform.AsRT();

            var dialog = ObjEditor.inst.ObjectView.transform.parent.parent.parent.parent.parent; // lol wtf
            var right = dialog.Find("data/right");
            var colorDialog = ObjEditor.inst.KeyframeDialogs[3].transform;

            right.gameObject.AddComponent<Mask>();

            var todDropdown = Content.Find("autokill/tod-dropdown");
            var hide = todDropdown.GetComponent<HideDropdownOptions>();
            hide.DisabledOptions[0] = false;
            hide.remove = true;
            var template = todDropdown.transform.Find("Template/Viewport/Content").gameObject;
            var vlg = template.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;

            var csf = template.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.MinSize;

            Content.Find("name/name").GetComponent<InputField>().characterLimit = 0;

            // Labels
            for (int j = 0; j < Content.childCount; j++)
            {
                var label = Content.GetChild(j);
                if (label.name == "label" || label.name == "collapselabel")
                {
                    for (int k = 0; k < label.childCount; k++)
                    {
                        var labelText = label.GetChild(k).GetComponent<Text>();
                        EditorThemeManager.ApplyLightText(labelText);
                    }
                }
            }

            for (int i = 0; i < ObjEditor.inst.KeyframeDialogs.Count; i++)
            {
                var kfdialog = ObjEditor.inst.KeyframeDialogs[i].transform;

                for (int j = 0; j < kfdialog.childCount; j++)
                {
                    var label = kfdialog.GetChild(j);
                    if (label.name == "label")
                    {
                        for (int k = 0; k < label.childCount; k++)
                        {
                            var labelText = label.GetChild(k).GetComponent<Text>();
                            EditorThemeManager.ApplyLightText(labelText);
                        }
                    }
                }
            }

            #region Object Values

            // Depth
            {
                var spacer = Creator.NewUIObject("depth input", Content, 15);

                var spHLG = spacer.AddComponent<HorizontalLayoutGroup>();

                spacer.transform.AsRT().sizeDelta = new Vector2(30f, 30f);
                spHLG.childControlWidth = false;
                spHLG.childForceExpandWidth = false;
                spHLG.spacing = 8;

                var depth = EditorPrefabHolder.Instance.NumberInputField.Duplicate(spacer.transform, "depth");
                var depthInputFieldStorage = depth.GetComponent<InputFieldStorage>();
                depth.transform.localScale = Vector3.one;
                depthInputFieldStorage.transform.AsRT().sizeDelta = new Vector2(170f, 32f);
                depthInputFieldStorage.inputField.image.rectTransform.sizeDelta = new Vector2(110f, 32f);

                depthInputFieldStorage.inputField.onValueChanged.ClearAll();

                CoreHelper.Delete(depthInputFieldStorage.leftGreaterButton.gameObject);
                CoreHelper.Delete(depthInputFieldStorage.middleButton.gameObject);
                CoreHelper.Delete(depthInputFieldStorage.rightGreaterButton.gameObject);

                var sliderObject = Content.Find("depth/depth").gameObject;

                var depthLeft = Content.Find("depth/<").gameObject;
                var depthRight = Content.Find("depth/>").gameObject;
                EditorHelper.SetComplexity(depthLeft, Complexity.Simple);
                EditorHelper.SetComplexity(depthRight, Complexity.Simple);
                EditorThemeManager.ApplySelectable(depthLeft.GetComponent<Button>(), ThemeGroup.Function_2, false);
                EditorThemeManager.ApplySelectable(depthRight.GetComponent<Button>(), ThemeGroup.Function_2, false);

                sliderObject.transform.AsRT().sizeDelta = new Vector2(352f, 32f);
                Content.Find("depth").AsRT().sizeDelta = new Vector2(261f, 32f);

                EditorThemeManager.ApplyInputField(depthInputFieldStorage.inputField);
                EditorThemeManager.ApplySelectable(depthInputFieldStorage.leftButton, ThemeGroup.Function_2, false);
                EditorThemeManager.ApplySelectable(depthInputFieldStorage.rightButton, ThemeGroup.Function_2, false);

                var depthSlider = sliderObject.GetComponent<Slider>();
                var depthSliderImage = sliderObject.transform.Find("Image").GetComponent<Image>();
                depthSlider.colors = UIManager.SetColorBlock(depthSlider.colors, Color.white, new Color(0.9f, 0.9f, 0.9f), Color.white, Color.white, Color.white);

                EditorThemeManager.ApplyGraphic(depthSliderImage, ThemeGroup.Slider_2, true);
                EditorThemeManager.ApplyGraphic(depthSlider.image, ThemeGroup.Slider_2_Handle, true);
            }

            // Lock
            {
                var timeParent = Content.Find("time");

                var locker = EditorPrefabHolder.Instance.Toggle.Duplicate(timeParent.transform, "lock", 0);
                locker.transform.localScale = Vector3.one;

                var timeLayout = timeParent.GetComponent<HorizontalLayoutGroup>();
                timeLayout.childControlWidth = false;
                timeLayout.childForceExpandWidth = false;

                locker.transform.AsRT().sizeDelta = new Vector2(32f, 32f);

                var time = timeParent.Find("time");
                time.AsRT().sizeDelta = new Vector2(151, 32f);
                var lockToggle = locker.GetComponent<Toggle>();

                ((Image)lockToggle.graphic).sprite = ObjEditor.inst.timelineObjectPrefabLock.transform.Find("lock (1)").GetComponent<Image>().sprite;

                EditorThemeManager.ApplyToggle(lockToggle);

                timeParent.Find("<<").AsRT().sizeDelta = new Vector2(32f, 32f);
                timeParent.Find("<").AsRT().sizeDelta = new Vector2(16f, 32f);
                timeParent.Find("|").AsRT().sizeDelta = new Vector2(16f, 32f);
                timeParent.Find(">").AsRT().sizeDelta = new Vector2(16f, 32f);
                timeParent.Find(">>").AsRT().sizeDelta = new Vector2(32f, 32f);

                CoreHelper.Destroy(timeParent.Find("<<").GetComponent<Animator>(), true);
                var leftGreaterButton = timeParent.Find("<<").GetComponent<Button>();
                leftGreaterButton.transition = Selectable.Transition.ColorTint;
                CoreHelper.Destroy(timeParent.Find("<").GetComponent<Animator>(), true);
                var leftButton = timeParent.Find("<").GetComponent<Button>();
                leftButton.transition = Selectable.Transition.ColorTint;
                CoreHelper.Destroy(timeParent.Find("|").GetComponent<Animator>(), true);
                var middleButton = timeParent.Find("|").GetComponent<Button>();
                middleButton.transition = Selectable.Transition.ColorTint;
                CoreHelper.Destroy(timeParent.Find(">").GetComponent<Animator>(), true);
                var rightButton = timeParent.Find(">").GetComponent<Button>();
                rightButton.transition = Selectable.Transition.ColorTint;
                CoreHelper.Destroy(timeParent.Find(">>").GetComponent<Animator>(), true);
                var rightGreaterButton = timeParent.Find(">>").GetComponent<Button>();
                rightGreaterButton.transition = Selectable.Transition.ColorTint;

                EditorThemeManager.ApplySelectable(leftGreaterButton, ThemeGroup.Function_2, false);
                EditorThemeManager.ApplySelectable(leftButton, ThemeGroup.Function_2, false);
                EditorThemeManager.ApplySelectable(middleButton, ThemeGroup.Function_2, false);
                EditorThemeManager.ApplySelectable(rightButton, ThemeGroup.Function_2, false);
                EditorThemeManager.ApplySelectable(rightGreaterButton, ThemeGroup.Function_2, false);
                EditorThemeManager.ApplyInputField(timeParent.Find("time").GetComponent<InputField>());
            }

            // Origin X / Y
            {
                var contentOriginTF = Content.transform.Find("origin").transform;

                try
                {
                    for (int i = 1; i <= 3; i++)
                    {
                        var origin = contentOriginTF.Find("origin-x/" + i);
                        EditorThemeManager.ApplyToggle(origin.GetComponent<Toggle>(), ThemeGroup.Background_1);
                        EditorThemeManager.ApplyGraphic(origin.Find("Image").GetComponent<Image>(), ThemeGroup.Toggle_1_Check);
                    }
                    for (int i = 1; i <= 3; i++)
                    {
                        var origin = contentOriginTF.Find("origin-y/" + i);
                        EditorThemeManager.ApplyToggle(origin.GetComponent<Toggle>(), ThemeGroup.Background_1);
                        EditorThemeManager.ApplyGraphic(origin.Find("Image").GetComponent<Image>(), ThemeGroup.Toggle_1_Check);
                    }
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }

                var originX = EditorPrefabHolder.Instance.NumberInputField.Duplicate(contentOriginTF, "x");
                var originXInputFieldStorage = originX.GetComponent<InputFieldStorage>();
                originX.transform.localScale = Vector3.one;
                originXInputFieldStorage.transform.AsRT().sizeDelta = new Vector2(170f, 32f);
                originXInputFieldStorage.inputField.image.rectTransform.sizeDelta = new Vector2(110f, 32f);

                originXInputFieldStorage.inputField.onValueChanged.ClearAll();

                var originY = EditorPrefabHolder.Instance.NumberInputField.Duplicate(contentOriginTF, "y");
                var originYInputFieldStorage = originY.GetComponent<InputFieldStorage>();
                originY.transform.localScale = Vector3.one;
                originYInputFieldStorage.transform.AsRT().sizeDelta = new Vector2(170f, 32f);
                originYInputFieldStorage.inputField.image.rectTransform.sizeDelta = new Vector2(110f, 32f);

                originYInputFieldStorage.inputField.onValueChanged.ClearAll();

                CoreHelper.Delete(originXInputFieldStorage.leftGreaterButton.gameObject);
                CoreHelper.Delete(originXInputFieldStorage.middleButton.gameObject);
                CoreHelper.Delete(originXInputFieldStorage.rightGreaterButton.gameObject);

                CoreHelper.Delete(originYInputFieldStorage.leftGreaterButton.gameObject);
                CoreHelper.Delete(originYInputFieldStorage.middleButton.gameObject);
                CoreHelper.Delete(originYInputFieldStorage.rightGreaterButton.gameObject);

                EditorThemeManager.ApplyInputField(originXInputFieldStorage.inputField);
                EditorThemeManager.ApplySelectable(originXInputFieldStorage.leftButton, ThemeGroup.Function_2, false);
                EditorThemeManager.ApplySelectable(originXInputFieldStorage.rightButton, ThemeGroup.Function_2, false);

                EditorThemeManager.ApplyInputField(originYInputFieldStorage.inputField);
                EditorThemeManager.ApplySelectable(originYInputFieldStorage.leftButton, ThemeGroup.Function_2, false);
                EditorThemeManager.ApplySelectable(originYInputFieldStorage.rightButton, ThemeGroup.Function_2, false);
            }

            // Layers
            {
                var editor = Content.Find("editor");

                editor.Find("layer").AsRT().sizeDelta = new Vector2(168.5f, 32f);
                editor.Find("layer").gameObject.SetActive(false);

                var layers = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(editor, "layers", 0);

                var layersIF = layers.GetComponent<InputField>();

                layersIF.textComponent.alignment = TextAnchor.MiddleCenter;
                layersIF.characterValidation = InputField.CharacterValidation.Integer;

                var edhlg = Content.transform.Find("editor").GetComponent<HorizontalLayoutGroup>();
                edhlg.childControlWidth = false;
                edhlg.childForceExpandWidth = false;

                layers.transform.AsRT().sizeDelta = new Vector2(100f, 32f);
                Content.Find("editor/bin").AsRT().sizeDelta = new Vector2(237f, 32f);

                layers.AddComponent<ContrastColors>().Init(layersIF.textComponent, layersIF.image);

                EditorThemeManager.ApplyGraphic(layersIF.image, ThemeGroup.Null, true);

                var binSlider = Content.Find("editor/bin").GetComponent<Slider>();
                var binSliderImage = binSlider.transform.Find("Image").GetComponent<Image>();
                binSlider.colors = UIManager.SetColorBlock(binSlider.colors, Color.white, new Color(0.9f, 0.9f, 0.9f), Color.white, Color.white, Color.white);
                EditorThemeManager.ApplyGraphic(binSliderImage, ThemeGroup.Slider_2, true);
                EditorThemeManager.ApplyGraphic(binSlider.image, ThemeGroup.Slider_2_Handle, true);
            }

            // Clear Parent
            {
                var parent = Content.Find("parent");
                var hlg = parent.GetComponent<HorizontalLayoutGroup>();
                hlg.childControlWidth = false;
                hlg.spacing = 4f;

                parent.transform.Find("text").AsRT().sizeDelta = new Vector2(201f, 32f);

                var resetParent = EditorPrefabHolder.Instance.CloseButton.Duplicate(parent.transform, "clear parent", 1);

                var resetParentButton = resetParent.GetComponent<Button>();

                var parentPicker = EditorPrefabHolder.Instance.CloseButton.Duplicate(parent.transform, "parent picker", 2);

                var parentPickerButton = parentPicker.GetComponent<Button>();

                parentPickerButton.onClick.NewListener(() => RTEditor.inst.parentPickerEnabled = true);

                var parentPickerIcon = parentPicker.transform.GetChild(0).GetComponent<Image>();
                parentPickerIcon.sprite = EditorSprites.DropperSprite;

                var searchParent = parent.transform.Find("parent").GetComponent<Image>();
                EditorThemeManager.ApplyGraphic(searchParent, ThemeGroup.Function_3, true);
                EditorThemeManager.ApplyGraphic(searchParent.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Function_3_Text);

                CoreHelper.Destroy(resetParent.GetComponent<Animator>(), true);
                resetParentButton.transition = Selectable.Transition.ColorTint;
                EditorThemeManager.ApplySelectable(resetParentButton, ThemeGroup.Close);
                EditorThemeManager.ApplyGraphic(resetParent.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

                CoreHelper.Destroy(parentPicker.GetComponent<Animator>(), true);
                parentPickerButton.transition = Selectable.Transition.ColorTint;
                EditorThemeManager.ApplySelectable(parentPickerButton, ThemeGroup.Picker);
                EditorThemeManager.ApplyGraphic(parentPickerIcon, ThemeGroup.Picker_Icon);

                parent.transform.Find("parent").AsRT().sizeDelta = new Vector2(32f, 32f);
                parent.transform.Find("more").AsRT().sizeDelta = new Vector2(32f, 32f);
            }

            // ID & LDM
            {
                var id = EditorPrefabHolder.Instance.Labels.Duplicate(Content, "id", 0);
                EditorHelper.SetComplexity(id, Complexity.Normal);

                id.transform.AsRT().sizeDelta = new Vector2(515, 32f);
                id.transform.GetChild(0).AsRT().sizeDelta = new Vector2(226f, 32f);

                var text = id.transform.GetChild(0).GetComponent<Text>();
                text.fontSize = 18;
                text.text = "ID:";
                text.alignment = TextAnchor.MiddleLeft;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;

                var image = id.AddComponent<Image>();
                EditorThemeManager.ApplyGraphic(image, ThemeGroup.Background_2, true);

                var ldmLabel = text.gameObject.Duplicate(id.transform, "title").GetComponent<Text>();
                ldmLabel.rectTransform.sizeDelta = new Vector2(44f, 32f);
                ldmLabel.text = "LDM";
                ldmLabel.fontStyle = FontStyle.Bold;
                ldmLabel.fontSize = 20;

                var ldm = EditorPrefabHolder.Instance.Toggle.Duplicate(id.transform, "ldm");
                var ldmToggle = ldm.GetComponent<Toggle>();

                EditorThemeManager.ApplyLightText(text);
                EditorThemeManager.ApplyLightText(ldmLabel);
                EditorThemeManager.ApplyToggle(ldmToggle);
            }

            // Object Tags
            {
                var index = Content.Find("name").GetSiblingIndex() + 1;
                new Labels(Labels.InitSettings.Default.Parent(Content).SiblingIndex(index).Name("tags_label"), "Tags");

                // Tags Scroll View/Viewport/Content
                var tagScrollView = Creator.NewUIObject("Tags Scroll View", Content, index + 1);

                tagScrollView.transform.AsRT().sizeDelta = new Vector2(522f, 40f);
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
                scroll.content = tagContent.transform.AsRT();
            }

            // Render Type
            {
                var index = Content.Find("depth").GetSiblingIndex() + 1;
                new Labels(Labels.InitSettings.Default.Parent(Content).SiblingIndex(index).Name("rendertype_label"), "Render Type");

                var renderType = EditorPrefabHolder.Instance.Dropdown.Duplicate(Content, "rendertype", index + 1);
                var renderTypeDD = renderType.GetComponent<Dropdown>();
                renderTypeDD.options = CoreHelper.StringToOptionData("Foreground", "Background", "UI");

                EditorThemeManager.ApplyDropdown(renderTypeDD);
            }

            // Parent Settings
            {
                var array = new string[] { "pos", "sca", "rot" };
                for (int i = 0; i < 3; i++)
                {
                    var parent = Content.Find("parent_more").GetChild(i + 1);

                    CoreHelper.Delete(parent.Find("<<"));
                    CoreHelper.Delete(parent.Find("<"));
                    CoreHelper.Delete(parent.Find(">"));
                    CoreHelper.Delete(parent.Find(">>"));

                    var additive = parent.GetChild(2).gameObject.Duplicate(parent, $"{array[i]}_add");
                    var parallax = parent.GetChild(3).gameObject.Duplicate(parent, $"{array[i]}_parallax");

                    if (parent.Find("text"))
                    {
                        var text = parent.Find("text").GetComponent<Text>();
                        text.fontSize = 19;
                        EditorThemeManager.ApplyLightText(text);
                    }

                    var type = parent.GetChild(2).gameObject;
                    var offset = parent.GetChild(3).gameObject;

                    var lel = offset.GetComponent<LayoutElement>();
                    lel.minWidth = 64f;
                    lel.preferredWidth = 64f;

                    EditorThemeManager.ApplyToggle(type.GetComponent<Toggle>(), ThemeGroup.Background_3);
                    var typeImage = type.transform.Find("Image").GetComponent<Image>();
                    EditorThemeManager.ApplyGraphic(typeImage, ThemeGroup.Toggle_1_Check);
                    EditorThemeManager.ApplyInputField(offset.GetComponent<InputField>());
                    EditorThemeManager.ApplyToggle(additive.GetComponent<Toggle>(), ThemeGroup.Background_3);
                    var additiveImage = additive.transform.Find("Image").GetComponent<Image>();
                    EditorThemeManager.ApplyGraphic(additiveImage, ThemeGroup.Toggle_1_Check);
                    EditorThemeManager.ApplyInputField(parallax.GetComponent<InputField>());

                    var typePath = AssetPack.GetFile($"core/sprites/icons/{array[i]}_toggle.png");
                    if (RTFile.FileExists(typePath))
                        typeImage.sprite = SpriteHelper.LoadSprite(typePath);
                    
                    var addPath = AssetPack.GetFile($"core/sprites/icons/{array[i]}_addtoggle.png");
                    if (RTFile.FileExists(addPath))
                        additiveImage.sprite = SpriteHelper.LoadSprite(addPath);
                }
            }

            // Timeline Object adjustments
            {
                var gameObject = ObjEditor.inst.timelineObjectPrefab.Duplicate(ObjectEditor.inst.transform, ObjEditor.inst.timelineObjectPrefab.name);
                var icons = gameObject.transform.Find("icons");

                if (!icons.gameObject.GetComponent<HorizontalLayoutGroup>())
                {
                    var timelineObjectStorage = gameObject.AddComponent<TimelineObjectStorage>();

                    var @lock = ObjEditor.inst.timelineObjectPrefabLock.Duplicate(icons);
                    @lock.name = "lock";
                    @lock.transform.AsRT().anchoredPosition = Vector3.zero;

                    var dots = ObjEditor.inst.timelineObjectPrefabDots.Duplicate(icons);
                    dots.name = "dots";
                    dots.transform.AsRT().anchoredPosition = Vector3.zero;

                    var hlg = icons.gameObject.AddComponent<HorizontalLayoutGroup>();
                    hlg.childControlWidth = false;
                    hlg.childForceExpandWidth = false;
                    hlg.spacing = -4f;
                    hlg.childAlignment = TextAnchor.UpperRight;

                    @lock.transform.AsRT().sizeDelta = new Vector2(20f, 20f);
                    dots.transform.AsRT().sizeDelta = new Vector2(32f, 20f);

                    var b = Creator.NewUIObject("type", icons);
                    b.transform.AsRT().sizeDelta = new Vector2(20f, 20f);

                    var bImage = b.AddComponent<Image>();
                    bImage.color = new Color(0f, 0f, 0f, 0.45f);

                    var icon = Creator.NewUIObject("type", b.transform);
                    icon.transform.AsRT().anchoredPosition = Vector2.zero;
                    icon.transform.AsRT().sizeDelta = new Vector2(20f, 20f);

                    icon.AddComponent<Image>();

                    var hoverUI = gameObject.AddComponent<HoverUI>();
                    hoverUI.animatePos = false;
                    hoverUI.animateSca = true;

                    timelineObjectStorage.hoverUI = hoverUI;
                    timelineObjectStorage.image = gameObject.GetComponent<Image>();
                    timelineObjectStorage.eventTrigger = gameObject.GetComponent<EventTrigger>() ?? gameObject.AddComponent<EventTrigger>();
                    timelineObjectStorage.text = gameObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                }

                ObjEditor.inst.timelineObjectPrefab = gameObject;

                var gameObject2 = ObjEditor.inst.objTimelinePrefab.Duplicate(ObjEditor.inst.transform, ObjEditor.inst.objTimelinePrefab.name);

                var lockedKeyframe = ObjEditor.inst.timelineObjectPrefabLock.Duplicate(gameObject2.transform, "lock");
                lockedKeyframe.transform.AsRT().anchoredPosition = new Vector2(6f, 0f);
                lockedKeyframe.transform.AsRT().sizeDelta = new Vector2(15f, 15f);

                ObjEditor.inst.objTimelinePrefab = gameObject2;
            }

            // Parent Desync
            {
                var parentMore = Content.Find("parent_more");
                var parentDesync = EditorPrefabHolder.Instance.ToggleButton.Duplicate(parentMore, "desync", 1);
                var parentDesyncButtonToggle = parentDesync.GetComponent<ToggleButtonStorage>();
                parentDesyncButtonToggle.label.text = "Parent Desync";

                EditorThemeManager.ApplyToggle(parentDesyncButtonToggle.toggle, graphic: parentDesyncButtonToggle.label);
                parentMore.AsRT().sizeDelta = new Vector2(351f, 152f);
            }

            // Unity Explorer
            try
            {
                if (ModCompatibility.UnityExplorerInstalled)
                {
                    var index = Content.Find("editor").GetSiblingIndex() + 1;

                    // Setup label
                    var label = EditorPrefabHolder.Instance.Labels.Duplicate(Content, "unity explorer label", index);
                    var labelText = label.transform.GetChild(0).GetComponent<Text>();
                    labelText.text = "Unity Explorer";
                    EditorThemeManager.ApplyLightText(labelText);

                    // Inspect Beatmap Object
                    var inspectBeatmapObject = EditorPrefabHolder.Instance.Function2Button.Duplicate(Content, "inspectbeatmapobject", index + 1);
                    var inspectBeatmapObjectButton = inspectBeatmapObject.GetComponent<FunctionButtonStorage>();
                    inspectBeatmapObjectButton.Text = "Inspect Beatmap Object";

                    EditorThemeManager.ApplySelectable(inspectBeatmapObjectButton.button, ThemeGroup.Function_2);
                    EditorThemeManager.ApplyGraphic(inspectBeatmapObjectButton.label, ThemeGroup.Function_2_Text);

                    // Inspect Level Object
                    var inspectLevelObject = EditorPrefabHolder.Instance.Function2Button.Duplicate(Content, "inspectlevelobject", index + 2);
                    var inspectLevelObjectButton = inspectLevelObject.GetComponent<FunctionButtonStorage>();
                    inspectLevelObjectButton.Text = "Inspect Runtime Object";

                    EditorThemeManager.ApplySelectable(inspectLevelObjectButton.button, ThemeGroup.Function_2);
                    EditorThemeManager.ApplyGraphic(inspectLevelObjectButton.label, ThemeGroup.Function_2_Text);

                    // Inspect Timeline Object
                    var inspectTimelineObject = EditorPrefabHolder.Instance.Function2Button.Duplicate(Content, "inspecttimelineobject", index + 3);
                    var inspectTimelineObjectButton = inspectTimelineObject.GetComponent<FunctionButtonStorage>();
                    inspectTimelineObjectButton.Text = "Inspect Timeline Object";

                    EditorThemeManager.ApplySelectable(inspectTimelineObjectButton.button, ThemeGroup.Function_2);
                    EditorThemeManager.ApplyGraphic(inspectTimelineObjectButton.label, ThemeGroup.Function_2_Text);
                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Had an error in trying to setup Unity Explorer. Exception: {ex}");
            }

            // Editor Themes
            {
                EditorThemeManager.ApplyGraphic(dialog.GetComponent<Image>(), ThemeGroup.Background_1);
                EditorThemeManager.ApplyGraphic(right.GetComponent<Image>(), ThemeGroup.Background_3);
                EditorThemeManager.ApplyInputField(Content.Find("name/name").GetComponent<InputField>());
                EditorThemeManager.ApplyDropdown(Content.Find("name/object-type").GetComponent<Dropdown>());
                EditorThemeManager.ApplyDropdown(todDropdown.GetComponent<Dropdown>());

                var autokill = Content.Find("autokill");
                EditorThemeManager.ApplyInputField(autokill.Find("tod-value").GetComponent<InputField>());

                var setAutokillButton = autokill.Find("|").GetComponent<Button>();
                CoreHelper.Destroy(setAutokillButton.GetComponent<Animator>());
                setAutokillButton.transition = Selectable.Transition.ColorTint;
                EditorThemeManager.ApplySelectable(setAutokillButton, ThemeGroup.Function_2, false);

                var collapse = autokill.Find("collapse").GetComponent<Toggle>();

                EditorThemeManager.ApplyToggle(collapse, ThemeGroup.Background_1);

                for (int i = 0; i < collapse.transform.Find("dots").childCount; i++)
                    EditorThemeManager.ApplyGraphic(collapse.transform.Find("dots").GetChild(i).GetComponent<Image>(), ThemeGroup.Dark_Text);

                var parentButton = Content.Find("parent/text").GetComponent<Button>();
                EditorThemeManager.ApplySelectable(parentButton, ThemeGroup.Function_2);
                EditorThemeManager.ApplyGraphic(parentButton.transform.GetChild(0).GetComponent<Text>(), ThemeGroup.Function_2_Text);

                var moreButton = Content.Find("parent/more").GetComponent<Button>();
                CoreHelper.Destroy(moreButton.GetComponent<Animator>());
                moreButton.transition = Selectable.Transition.ColorTint;
                EditorThemeManager.ApplySelectable(moreButton, ThemeGroup.Function_2, false);

                EditorThemeManager.ApplyInputField(Content.transform.Find("shapesettings/5").GetComponent<InputField>());

                var zoomSliderBase = ObjEditor.inst.zoomSlider.transform.parent;

                var gameObject = Creator.NewUIObject("zoom back", zoomSliderBase.parent, 1);
                var image = gameObject.AddComponent<Image>();
                RectValues.BottomLeftAnchored.SizeDelta(128f, 25f).AssignToRectTransform(image.rectTransform);
                EditorThemeManager.ApplyScrollbar(dialog.Find("data/left/Scroll View/Scrollbar Vertical").GetComponent<Scrollbar>());
            }

            #endregion

            #region Keyframe Editors

            // Colors
            {
                var colorParent = colorDialog.Find("color");
                colorParent.GetComponent<GridLayoutGroup>().spacing = new Vector2(9.32f, 9.32f);
                colorParent.GetPreviousSibling().name = "color_label";

                for (int i = 1; i < 19; i++)
                {
                    if (i >= 10)
                        colorParent.Find("9").gameObject.Duplicate(colorParent, i.ToString());

                    var toggle = colorParent.Find(i.ToString()).GetComponent<Toggle>();

                    EditorThemeManager.ApplyGraphic(toggle.image, ThemeGroup.Null, true);
                    EditorThemeManager.ApplyGraphic(toggle.graphic, ThemeGroup.Background_3);
                }
            }

            // Homing Buttons
            {
                var staticHomingSpritePath = AssetPack.GetFile($"core/sprites/icons/homing_static{FileFormat.PNG.Dot()}");
                var dynamicHomingSpritePath = AssetPack.GetFile($"core/sprites/icons/homing_dynamic{FileFormat.PNG.Dot()}");

                var position = ObjEditor.inst.KeyframeDialogs[0].transform;
                var randomPosition = position.transform.Find("random");
                CoreHelper.Destroy(randomPosition.GetComponent<ToggleGroup>());
                randomPosition.Find("interval-input/x").gameObject.SetActive(false);
                var homingStaticPosition = randomPosition.Find("none").gameObject.Duplicate(randomPosition, "homing-static", 4);

                if (RTFile.FileExists(staticHomingSpritePath))
                    homingStaticPosition.transform.Find("Image").GetComponent<Image>().sprite = SpriteHelper.LoadSprite(staticHomingSpritePath);

                EditorThemeManager.ApplyToggle(homingStaticPosition.GetComponent<Toggle>(), ThemeGroup.Background_3);
                EditorThemeManager.ApplyGraphic(homingStaticPosition.transform.Find("Image").GetComponent<Image>(), ThemeGroup.Toggle_1_Check);

                var homingDynamicPosition = randomPosition.Find("none").gameObject.Duplicate(randomPosition, "homing-dynamic", 5);

                if (RTFile.FileExists(dynamicHomingSpritePath))
                    homingDynamicPosition.transform.Find("Image").GetComponent<Image>().sprite = SpriteHelper.LoadSprite(dynamicHomingSpritePath);

                EditorThemeManager.ApplyToggle(homingDynamicPosition.GetComponent<Toggle>(), ThemeGroup.Background_3);
                EditorThemeManager.ApplyGraphic(homingDynamicPosition.transform.Find("Image").GetComponent<Image>(), ThemeGroup.Toggle_1_Check);

                var scale = ObjEditor.inst.KeyframeDialogs[1].transform;
                var scaleRotation = scale.Find("random");
                CoreHelper.Destroy(scaleRotation.GetComponent<ToggleGroup>());

                var rotation = ObjEditor.inst.KeyframeDialogs[2].transform;
                var randomRotation = rotation.Find("random");
                CoreHelper.Destroy(randomRotation.GetComponent<ToggleGroup>());
                randomRotation.Find("interval-input/x").gameObject.SetActive(false);
                var homingStaticRotation = randomRotation.Find("none").gameObject.Duplicate(randomRotation, "homing-static", 3);

                if (RTFile.FileExists(staticHomingSpritePath))
                    homingStaticRotation.transform.Find("Image").GetComponent<Image>().sprite = SpriteHelper.LoadSprite(staticHomingSpritePath);

                EditorThemeManager.ApplyToggle(homingStaticRotation.GetComponent<Toggle>(), ThemeGroup.Background_3);
                EditorThemeManager.ApplyGraphic(homingStaticRotation.transform.Find("Image").GetComponent<Image>(), ThemeGroup.Toggle_1_Check);

                var homingDynamicRotation = randomRotation.Find("none").gameObject.Duplicate(randomRotation, "homing-dynamic", 4);

                if (RTFile.FileExists(dynamicHomingSpritePath))
                    homingDynamicRotation.transform.Find("Image").GetComponent<Image>().sprite = SpriteHelper.LoadSprite(dynamicHomingSpritePath);

                EditorThemeManager.ApplyToggle(homingDynamicRotation.GetComponent<Toggle>(), ThemeGroup.Background_3);
                EditorThemeManager.ApplyGraphic(homingDynamicRotation.transform.Find("Image").GetComponent<Image>(), ThemeGroup.Toggle_1_Check);

                var rRotation = rotation.Find("r_rotation");
                var rRotationX = rRotation.Find("x");

                var rRotationY = rRotationX.gameObject.Duplicate(rRotation, "y");

                var rRotationLabel = rotation.Find("r_rotation_label");
                var l = rRotationLabel.GetChild(0);
                var max = l.gameObject.Duplicate(rRotationLabel, "text");

                CoreHelper.Destroy(rRotation.GetComponent<EventTrigger>());

                var rAxis = EditorPrefabHolder.Instance.Dropdown.Duplicate(position, "r_axis", 14);
                var rAxisDD = rAxis.GetComponent<Dropdown>();
                rAxisDD.options = CoreHelper.StringToOptionData("Both", "X Only", "Y Only");

                EditorThemeManager.ApplyDropdown(rAxisDD);
            }

            // Position Z
            {
                var positionBase = ObjEditor.inst.KeyframeDialogs[0].transform.Find("position");

                var posZ = positionBase.Find("x").gameObject.Duplicate(positionBase, "z");

                CoreHelper.Destroy(positionBase.GetComponent<HorizontalLayoutGroup>(), true);
                var grp = positionBase.gameObject.AddComponent<GridLayoutGroup>();

                CoreHelper.Destroy(positionBase.Find("x/input").GetComponent<LayoutElement>(), true);
                CoreHelper.Destroy(positionBase.Find("y/input").GetComponent<LayoutElement>(), true);
                CoreHelper.Destroy(positionBase.Find("z/input").GetComponent<LayoutElement>(), true);

                var xLayout = positionBase.Find("x/input").GetComponent<LayoutElement>();
                var yLayout = positionBase.Find("y/input").GetComponent<LayoutElement>();
                var zLayout = positionBase.Find("z/input").GetComponent<LayoutElement>();

                xLayout.preferredWidth = -1;
                yLayout.preferredWidth = -1;
                zLayout.preferredWidth = -1;

                var labels = ObjEditor.inst.KeyframeDialogs[0].transform.GetChild(8);
                var posZLabel = labels.GetChild(1).gameObject.Duplicate(labels, "text");
                var posZLabelText = posZLabel.GetComponent<Text>();
                posZLabelText.text = "Position Z";
                EditorThemeManager.ApplyLightText(posZLabelText);

                positionBase.AsRT().sizeDelta = new Vector2(553f, 32f);
            }

            // Opacity
            {
                new LabelsElement("Opacity").Init(EditorElement.InitSettings.Default.Parent(colorDialog).Name("opacity_label"));

                var opacity = ObjEditor.inst.KeyframeDialogs[2].transform.Find("rotation").gameObject.Duplicate(colorDialog, "opacity");
                opacity.transform.Find("x/input").AsRT().sizeDelta = new Vector2(136f, 32f);

                var collision = EditorPrefabHolder.Instance.ToggleButton.Duplicate(opacity.transform, "collision");
                var collisionToggle = collision.GetComponent<ToggleButtonStorage>();
                collisionToggle.Text = "Collide";

                var inputFieldStorage = opacity.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
                inputFieldStorage.Assign();

                EditorThemeManager.ApplyInputField(inputFieldStorage);
                EditorThemeManager.ApplyToggle(collisionToggle);
            }

            // Hue / Sat / Val
            {
                new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Hue", "Saturation", "Value").Init(
                    EditorElement.InitSettings.Default
                    .Parent(colorDialog)
                    .Name("huesatval_label")
                    .Rect(new RectValues(new Vector2(8f, -330f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(371f, 20f))));

                var hsv = ObjEditor.inst.KeyframeDialogs[1].transform.Find("scale").gameObject.Duplicate(colorDialog);
                hsv.name = "huesatval";

                hsv.transform.GetChild(1).gameObject.Duplicate(hsv.transform, "z");

                for (int i = 0; i < hsv.transform.childCount; i++)
                {
                    var child = hsv.transform.GetChild(i);
                    if (!child.GetComponent<InputFieldSwapper>())
                    {
                        var inputField = child.GetComponent<InputField>();
                        var swapper = child.gameObject.AddComponent<InputFieldSwapper>();
                        swapper.inputField = inputField;

                        inputField.characterValidation = InputField.CharacterValidation.None;
                        inputField.contentType = InputField.ContentType.Standard;
                        inputField.keyboardType = TouchScreenKeyboardType.Default;
                    }

                    var horizontal = child.GetComponent<HorizontalLayoutGroup>();
                    var input = child.Find("input").AsRT();

                    horizontal.childControlWidth = false;

                    input.sizeDelta = new Vector2(60f, 32f);

                    var layout = child.GetComponent<LayoutElement>();
                    layout.minWidth = 109f;

                    var inputFieldStorage = child.gameObject.GetOrAddComponent<InputFieldStorage>();
                    inputFieldStorage.Assign();
                    EditorThemeManager.ApplyInputField(inputFieldStorage);
                }
            }

            // Relative / Copy / Paste
            {
                for (int i = 0; i < 4; i++)
                {
                    var parent = ObjEditor.inst.KeyframeDialogs[i].transform;
                    if (i != 3)
                    {
                        var toggleLabel = EditorPrefabHolder.Instance.Labels.gameObject.Duplicate(parent, "relative-label");
                        var toggleLabelText = toggleLabel.transform.GetChild(0).GetComponent<Text>();
                        toggleLabelText.text = "Value Additive";
                        var toggle = EditorPrefabHolder.Instance.ToggleButton.Duplicate(parent, "relative");
                        var toggleButtonStorage = toggle.GetComponent<ToggleButtonStorage>();
                        toggleButtonStorage.Text = "Relative";

                        EditorThemeManager.ApplyLightText(toggleLabelText);
                        EditorThemeManager.ApplyLightText(toggleButtonStorage.label);
                        EditorThemeManager.ApplyToggle(toggleButtonStorage.toggle, graphic: toggleButtonStorage.label);

                        if (i != 1)
                        {
                            var fleeToggle = EditorPrefabHolder.Instance.ToggleButton.Duplicate(parent, "flee");
                            var fleeToggleStorage = fleeToggle.GetComponent<ToggleButtonStorage>();
                            fleeToggleStorage.Text = "Flee";

                            EditorThemeManager.ApplyLightText(fleeToggleStorage.label);
                            EditorThemeManager.ApplyToggle(fleeToggleStorage.toggle, graphic: fleeToggleStorage.label);
                        }

                        var flipX = EditorPrefabHolder.Instance.Function1Button.Duplicate(parent, "flipx");
                        flipX.transform.AsRT().sizeDelta = new Vector2(366f, 32f);
                        var flipXStorage = flipX.GetComponent<FunctionButtonStorage>();
                        flipXStorage.Text = "Flip X";
                        flipXStorage.OnClick.NewListener(() =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>().TimelineKeyframes.Where(x => x.Selected))
                            {
                                var eventKeyframe = timelineObject.eventKeyframe;
                                eventKeyframe.values[0] = -eventKeyframe.values[0];
                            }

                            var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                            Timeline.RenderDialog(beatmapObject);
                        });

                        EditorThemeManager.ApplyGraphic(flipXStorage.button.image, ThemeGroup.Function_1, true);
                        EditorThemeManager.ApplyGraphic(flipXStorage.label, ThemeGroup.Function_1_Text);
                        EditorHelper.SetComplexity(flipX, Complexity.Normal);

                        if (i != 2)
                        {
                            var flipY = EditorPrefabHolder.Instance.Function1Button.Duplicate(parent, "flipy");
                            flipY.transform.AsRT().sizeDelta = new Vector2(366f, 32f);
                            var flipYStorage = flipY.GetComponent<FunctionButtonStorage>();
                            flipYStorage.Text = "Flip Y";
                            flipYStorage.OnClick.NewListener(() =>
                            {
                                foreach (var timelineObject in EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>().TimelineKeyframes.Where(x => x.Selected))
                                {
                                    var eventKeyframe = timelineObject.eventKeyframe;
                                    eventKeyframe.values[1] = -eventKeyframe.values[1];
                                }

                                var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                                Timeline.RenderDialog(beatmapObject);
                            });

                            EditorThemeManager.ApplyGraphic(flipYStorage.button.image, ThemeGroup.Function_1, true);
                            EditorThemeManager.ApplyGraphic(flipYStorage.label, ThemeGroup.Function_1_Text);
                            EditorHelper.SetComplexity(flipY, Complexity.Normal);
                        }
                        if (i == 0)
                        {
                            var flipZ = EditorPrefabHolder.Instance.Function1Button.Duplicate(parent, "flipz");
                            flipZ.transform.AsRT().sizeDelta = new Vector2(366f, 32f);
                            var flipZStorage = flipZ.GetComponent<FunctionButtonStorage>();
                            flipZStorage.Text = "Flip Z";
                            flipZStorage.OnClick.NewListener(() =>
                            {
                                foreach (var timelineObject in EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>().TimelineKeyframes.Where(x => x.Selected))
                                {
                                    var eventKeyframe = timelineObject.eventKeyframe;
                                    eventKeyframe.values[2] = -eventKeyframe.values[2];
                                }

                                var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                                Timeline.RenderDialog(beatmapObject);
                            });

                            EditorThemeManager.ApplyGraphic(flipZStorage.button.image, ThemeGroup.Function_1, true);
                            EditorThemeManager.ApplyGraphic(flipZStorage.label, ThemeGroup.Function_1_Text);
                            EditorHelper.SetComplexity(flipZ, Complexity.Advanced);
                        }
                    }

                    var edit = parent.Find("edit");
                    EditorHelper.SetComplexity(edit.Find("spacer").gameObject, Complexity.Simple);

                    var copy = EditorPrefabHolder.Instance.Function1Button.Duplicate(edit, "copy", 5);
                    var copyText = copy.transform.GetChild(0).GetComponent<Text>();
                    copyText.text = "Copy";
                    copy.transform.AsRT().sizeDelta = new Vector2(70f, 32f);

                    EditorThemeManager.ApplyGraphic(copy.GetComponent<Image>(), ThemeGroup.Copy, true);
                    EditorThemeManager.ApplyGraphic(copyText, ThemeGroup.Copy_Text);
                    EditorHelper.SetComplexity(copy, Complexity.Normal);

                    var paste = EditorPrefabHolder.Instance.Function1Button.Duplicate(edit, "paste", 6);
                    var pasteText = paste.transform.GetChild(0).GetComponent<Text>();
                    pasteText.text = "Paste";
                    paste.transform.AsRT().sizeDelta = new Vector2(70f, 32f);

                    EditorThemeManager.ApplyGraphic(paste.GetComponent<Image>(), ThemeGroup.Paste, true);
                    EditorThemeManager.ApplyGraphic(pasteText, ThemeGroup.Paste_Text);
                    EditorHelper.SetComplexity(paste, Complexity.Normal);
                }
            }

            // remove unnecessary spacers
            CoreHelper.Delete(ObjEditor.inst.KeyframeDialogs[2].transform.GetChild(1).gameObject);
            CoreHelper.Delete(colorDialog.GetChild(1).gameObject);

            var multiKF = ObjEditor.inst.KeyframeDialogs[4];
            multiKF.transform.AsRT().anchorMax = new Vector2(0f, 1f);
            multiKF.transform.AsRT().anchorMin = new Vector2(0f, 1f);

            // Shift Dialogs
            CoreHelper.Destroy(right.GetComponent<VerticalLayoutGroup>(), true);

            // Multi Keyframe Editor
            {
                var multiKeyframeEditor = multiKF.transform;

                multiKeyframeEditor.GetChild(1).gameObject.SetActive(false);

                RTEditor.GenerateLabels("time_label", multiKeyframeEditor, new Label("Time"));

                var timeBase = Creator.NewUIObject("time", multiKeyframeEditor);
                timeBase.transform.AsRT().sizeDelta = new Vector2(765f, 38f);

                var time = EditorPrefabHolder.Instance.NumberInputField.Duplicate(timeBase.transform, "time");
                new RectValues(Vector2.zero, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(300f, 32f)).AssignToRectTransform(time.transform.AsRT());
                time.GetComponent<HorizontalLayoutGroup>().spacing = 5f;

                var timeStorage = time.GetComponent<InputFieldStorage>();
                timeStorage.inputField.gameObject.name = "time";

                EditorThemeManager.ApplyInputField(timeStorage.inputField);
                EditorThemeManager.ApplySelectable(timeStorage.leftGreaterButton, ThemeGroup.Function_2, false);
                EditorThemeManager.ApplySelectable(timeStorage.leftButton, ThemeGroup.Function_2, false);
                EditorThemeManager.ApplySelectable(timeStorage.middleButton, ThemeGroup.Function_2, false);
                EditorThemeManager.ApplySelectable(timeStorage.rightButton, ThemeGroup.Function_2, false);
                EditorThemeManager.ApplySelectable(timeStorage.rightGreaterButton, ThemeGroup.Function_2, false);

                RTEditor.GenerateLabels("curve_label", multiKeyframeEditor, new Label("Ease Type"));

                var curveBase = Creator.NewUIObject("curves", multiKeyframeEditor);
                curveBase.transform.AsRT().sizeDelta = new Vector2(765f, 38f);
                var curveBaseLayout = curveBase.AddComponent<HorizontalLayoutGroup>();
                curveBaseLayout.childControlWidth = false;
                curveBaseLayout.childForceExpandWidth = false;
                curveBaseLayout.spacing = 4f;

                var curves = EditorPrefabHolder.Instance.CurvesDropdown.Duplicate(curveBase.transform, "curves");
                curves.transform.AsRT().sizeDelta = new Vector2(230f, 38f);
                var curvesDropdown = curves.GetComponent<Dropdown>();
                EditorThemeManager.ApplyDropdown(curvesDropdown);
                RTEditor.inst.SetupEaseDropdown(curvesDropdown);

                var curvesApply = EditorPrefabHolder.Instance.Function1Button.Duplicate(curveBase.transform, "apply");
                curvesApply.transform.AsRT().sizeDelta = new Vector2(132f, 38f);
                var curvesApplyFunctionButton = curvesApply.GetComponent<FunctionButtonStorage>();
                curvesApplyFunctionButton.Text = "Apply Curves";
                EditorThemeManager.ApplyGraphic(curvesApplyFunctionButton.button.image, ThemeGroup.Function_1, true);
                EditorThemeManager.ApplyGraphic(curvesApplyFunctionButton.label, ThemeGroup.Function_1_Text);

                RTEditor.GenerateLabels("value index_label", multiKeyframeEditor, new Label("Value Index / Value"));

                var valueBase = Creator.NewUIObject("value base", multiKeyframeEditor);
                valueBase.transform.AsRT().sizeDelta = new Vector2(364f, 32f);

                var valueBaseHLG = valueBase.AddComponent<HorizontalLayoutGroup>();
                valueBaseHLG.childControlHeight = false;
                valueBaseHLG.childControlWidth = false;
                valueBaseHLG.childForceExpandHeight = false;
                valueBaseHLG.childForceExpandWidth = false;

                var valueIndex = EditorPrefabHolder.Instance.NumberInputField.Duplicate(valueBase.transform, "value index");
                valueIndex.transform.Find("input").AsRT().sizeDelta = new Vector2(60f, 32f);
                valueIndex.transform.AsRT().sizeDelta = new Vector2(130f, 32f);

                var valueIndexStorage = valueIndex.GetComponent<InputFieldStorage>();
                CoreHelper.Delete(valueIndexStorage.leftGreaterButton);
                CoreHelper.Delete(valueIndexStorage.middleButton);
                CoreHelper.Delete(valueIndexStorage.rightGreaterButton);
                EditorThemeManager.ApplyInputField(valueIndexStorage.inputField);
                EditorThemeManager.ApplySelectable(valueIndexStorage.leftButton, ThemeGroup.Function_2, false);
                EditorThemeManager.ApplySelectable(valueIndexStorage.rightButton, ThemeGroup.Function_2, false);

                var value = EditorPrefabHolder.Instance.NumberInputField.Duplicate(valueBase.transform, "value");
                value.transform.Find("input").AsRT().sizeDelta = new Vector2(128f, 32f);
                value.transform.AsRT().sizeDelta = new Vector2(200f, 32f);

                var valueStorage = value.GetComponent<InputFieldStorage>();
                CoreHelper.Delete(valueStorage.leftGreaterButton);
                CoreHelper.Delete(valueStorage.rightGreaterButton);
                EditorThemeManager.ApplyInputField(valueStorage.inputField);
                EditorThemeManager.ApplySelectable(valueStorage.leftButton, ThemeGroup.Function_2, false);
                EditorThemeManager.ApplySelectable(valueStorage.middleButton, ThemeGroup.Function_2, false);
                EditorThemeManager.ApplySelectable(valueStorage.rightButton, ThemeGroup.Function_2, false);

                RTEditor.GenerateLabels("snap_label", multiKeyframeEditor, new Label("Force Snap Time to BPM"));

                var snapToBPMObject = EditorPrefabHolder.Instance.Function1Button.Duplicate(multiKeyframeEditor, "snap bpm");
                snapToBPMObject.transform.localScale = Vector3.one;

                ((RectTransform)snapToBPMObject.transform).sizeDelta = new Vector2(368f, 32f);

                var snapToBPMText = snapToBPMObject.transform.GetChild(0).GetComponent<Text>();
                snapToBPMText.text = "Snap";

                var snapToBPM = snapToBPMObject.GetComponent<Button>();
                snapToBPM.onClick.NewListener(() =>
                {
                    var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                    foreach (var timelineObject in beatmapObject.TimelineKeyframes.Where(x => x.Selected))
                    {
                        if (timelineObject.Index != 0)
                            timelineObject.Time = RTEditor.SnapToBPM(timelineObject.Time);

                        float st = beatmapObject.StartTime;

                        st = -(st - RTEditor.SnapToBPM(st + timelineObject.Time));

                        float timePosition = KeyframeTimeline.TimeTimelineCalc(st);

                        timelineObject.GameObject.transform.AsRT().anchoredPosition = new Vector2(timePosition, 0f);

                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);

                        timelineObject.Render();
                    }
                });

                EditorThemeManager.ApplyGraphic(snapToBPM.image, ThemeGroup.Function_1, true);
                EditorThemeManager.ApplyGraphic(snapToBPMText, ThemeGroup.Function_1_Text);

                RTEditor.GenerateLabels("paste_label", multiKeyframeEditor, new Label("All Types"));

                var pasteAllObject = EditorPrefabHolder.Instance.Function1Button.Duplicate(multiKeyframeEditor, "paste");
                pasteAllObject.transform.localScale = Vector3.one;

                ((RectTransform)pasteAllObject.transform).sizeDelta = new Vector2(368f, 32f);

                var pasteAllText = pasteAllObject.transform.GetChild(0).GetComponent<Text>();
                pasteAllText.text = "Paste";

                var pasteAll = pasteAllObject.GetComponent<Button>();
                pasteAll.onClick.NewListener(() =>
                {
                    var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                    var list = beatmapObject.TimelineKeyframes.Where(x => x.Selected);

                    foreach (var timelineObject in list)
                        ObjectEditor.inst.Dialog.Timeline.SetCopiedData(timelineObject.Type, timelineObject.eventKeyframe, beatmapObject);

                    Timeline.RenderKeyframes(beatmapObject);
                    Timeline.RenderDialog(beatmapObject);
                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                    EditorManager.inst.DisplayNotification("Pasted keyframe data to selected keyframes!", 2f, EditorManager.NotificationType.Success);
                });

                EditorThemeManager.ApplyGraphic(pasteAll.image, ThemeGroup.Paste, true);
                EditorThemeManager.ApplyGraphic(pasteAllText, ThemeGroup.Paste_Text);

                RTEditor.GenerateLabels("paste_label", multiKeyframeEditor, new Label("Position / Scale"));

                var pastePosScaObject = new GameObject("paste pos sca base");
                pastePosScaObject.transform.SetParent(multiKeyframeEditor);
                pastePosScaObject.transform.localScale = Vector3.one;

                var pastePosScaRT = pastePosScaObject.AddComponent<RectTransform>();
                pastePosScaRT.sizeDelta = new Vector2(364f, 32f);

                var pastePosScaHLG = pastePosScaObject.AddComponent<HorizontalLayoutGroup>();
                pastePosScaHLG.childControlHeight = false;
                pastePosScaHLG.childControlWidth = false;
                pastePosScaHLG.childForceExpandHeight = false;
                pastePosScaHLG.childForceExpandWidth = false;
                pastePosScaHLG.spacing = 8f;

                var pastePosObject = EditorPrefabHolder.Instance.Function1Button.Duplicate(pastePosScaRT, "paste");
                pastePosObject.transform.localScale = Vector3.one;

                ((RectTransform)pastePosObject.transform).sizeDelta = new Vector2(180f, 32f);

                var pastePosText = pastePosObject.transform.GetChild(0).GetComponent<Text>();
                pastePosText.text = "Paste Pos";

                var pastePos = pastePosObject.GetComponent<Button>();
                pastePos.onClick.NewListener(() =>
                {
                    var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                    var list = beatmapObject.TimelineKeyframes.Where(x => x.Selected && x.Type == 0);

                    foreach (var timelineObject in list)
                        ObjectEditor.inst.Dialog.Timeline.SetCopiedData(0, timelineObject.eventKeyframe, beatmapObject);

                    Timeline.RenderKeyframes(beatmapObject);
                    Timeline.RenderDialog(beatmapObject);
                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                    EditorManager.inst.DisplayNotification("Pasted position keyframe data to selected position keyframes!", 3f, EditorManager.NotificationType.Success);
                });

                EditorThemeManager.ApplyGraphic(pastePos.image, ThemeGroup.Paste, true);
                EditorThemeManager.ApplyGraphic(pastePosText, ThemeGroup.Paste_Text);

                var pasteScaObject = EditorPrefabHolder.Instance.Function1Button.Duplicate(pastePosScaRT, "paste");
                pasteScaObject.transform.localScale = Vector3.one;

                ((RectTransform)pasteScaObject.transform).sizeDelta = new Vector2(180f, 32f);

                var pasteScaText = pasteScaObject.transform.GetChild(0).GetComponent<Text>();
                pasteScaText.text = "Paste Scale";

                var pasteSca = pasteScaObject.GetComponent<Button>();
                pasteSca.onClick.NewListener(() =>
                {
                    var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                    var list = beatmapObject.TimelineKeyframes.Where(x => x.Selected && x.Type == 1);

                    foreach (var timelineObject in list)
                        ObjectEditor.inst.Dialog.Timeline.SetCopiedData(1, timelineObject.eventKeyframe, beatmapObject);

                    Timeline.RenderKeyframes(beatmapObject);
                    Timeline.RenderDialog(beatmapObject);
                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                    EditorManager.inst.DisplayNotification("Pasted scale keyframe data to selected scale keyframes!", 3f, EditorManager.NotificationType.Success);
                });

                EditorThemeManager.ApplyGraphic(pasteSca.image, ThemeGroup.Paste, true);
                EditorThemeManager.ApplyGraphic(pasteScaText, ThemeGroup.Paste_Text);

                RTEditor.GenerateLabels("paste_label", multiKeyframeEditor, new Label("Rotation / Color"));

                var pasteRotColObject = new GameObject("paste rot col base");
                pasteRotColObject.transform.SetParent(multiKeyframeEditor);
                pasteRotColObject.transform.localScale = Vector3.one;

                var pasteRotColObjectRT = pasteRotColObject.AddComponent<RectTransform>();
                pasteRotColObjectRT.sizeDelta = new Vector2(364f, 32f);

                var pasteRotColObjectHLG = pasteRotColObject.AddComponent<HorizontalLayoutGroup>();
                pasteRotColObjectHLG.childControlHeight = false;
                pasteRotColObjectHLG.childControlWidth = false;
                pasteRotColObjectHLG.childForceExpandHeight = false;
                pasteRotColObjectHLG.childForceExpandWidth = false;
                pasteRotColObjectHLG.spacing = 8f;

                var pasteRotObject = EditorPrefabHolder.Instance.Function1Button.Duplicate(pasteRotColObjectRT, "paste");
                pasteRotObject.transform.localScale = Vector3.one;

                ((RectTransform)pasteRotObject.transform).sizeDelta = new Vector2(180f, 32f);

                var pasteRotText = pasteRotObject.transform.GetChild(0).GetComponent<Text>();
                pasteRotText.text = "Paste Rot";

                var pasteRot = pasteRotObject.GetComponent<Button>();
                pasteRot.onClick.NewListener(() =>
                {
                    var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                    var list = beatmapObject.TimelineKeyframes.Where(x => x.Selected && x.Type == 2);

                    foreach (var timelineObject in list)
                        ObjectEditor.inst.Dialog.Timeline.SetCopiedData(2, timelineObject.eventKeyframe, beatmapObject);

                    Timeline.RenderKeyframes(beatmapObject);
                    Timeline.RenderDialog(beatmapObject);
                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                    EditorManager.inst.DisplayNotification("Pasted rotation keyframe data to selected rotation keyframes!", 3f, EditorManager.NotificationType.Success);
                });

                EditorThemeManager.ApplyGraphic(pasteRot.image, ThemeGroup.Paste, true);
                EditorThemeManager.ApplyGraphic(pasteRotText, ThemeGroup.Paste_Text);

                var pasteColObject = EditorPrefabHolder.Instance.Function1Button.Duplicate(pasteRotColObjectRT, "paste");
                pasteColObject.transform.localScale = Vector3.one;

                ((RectTransform)pasteColObject.transform).sizeDelta = new Vector2(180f, 32f);

                var pasteColText = pasteColObject.transform.GetChild(0).GetComponent<Text>();
                pasteColText.text = "Paste Col";

                var pasteCol = pasteColObject.GetComponent<Button>();
                pasteCol.onClick.NewListener(() =>
                {
                    var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                    var list = beatmapObject.TimelineKeyframes.Where(x => x.Selected && x.Type == 3);

                    foreach (var timelineObject in list)
                        ObjectEditor.inst.Dialog.Timeline.SetCopiedData(3, timelineObject.eventKeyframe, beatmapObject);

                    Timeline.RenderKeyframes(beatmapObject);
                    Timeline.RenderDialog(beatmapObject);
                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                    EditorManager.inst.DisplayNotification("Pasted color keyframe data to selected color keyframes!", 3f, EditorManager.NotificationType.Success);
                });

                EditorThemeManager.ApplyGraphic(pasteCol.image, ThemeGroup.Paste, true);
                EditorThemeManager.ApplyGraphic(pasteColText, ThemeGroup.Paste_Text);
            }

            #endregion

            #region Keyframe Timeline

            // Markers
            {
                var markers = Creator.NewUIObject("Markers", ObjEditor.inst.objTimelineSlider.transform);
                RectValues.FullAnchored.AssignToRectTransform(markers.transform.AsRT());
            }

            #endregion

            #region Top Properties

            IDBase = Content.Find("id").AsRT();
            IDText = IDBase.Find("text").GetComponent<Text>();
            LDMLabel = IDBase.Find("title").GetComponent<Text>();
            LDMToggle = IDBase.Find("ldm").GetComponent<Toggle>();

            #endregion

            #region Name Area

            NameField = Content.Find("name/name").GetComponent<InputField>();
            ObjectTypeDropdown = Content.Find("name/object-type").GetComponent<Dropdown>();
            TooltipHelper.AssignTooltip(ObjectTypeDropdown.gameObject, "Object Type");
            if (ObjectTypeDropdown.template)
                ObjectTypeDropdown.template.sizeDelta = new Vector2(64f, 140f);

            TagsScrollView = Content.Find("Tags Scroll View").AsRT();
            TagsContent = Content.Find("Tags Scroll View/Viewport/Content").AsRT();

            #endregion

            #region Start Time

            StartTimeField = Content.Find("time").gameObject.AddComponent<InputFieldStorage>();
            StartTimeField.Assign(StartTimeField.gameObject);

            #endregion

            #region Autokill

            AutokillDropdown = Content.Find("autokill/tod-dropdown").GetComponent<Dropdown>();
            CoreHelper.Destroy(AutokillDropdown.GetComponent<HoverTooltip>());
            TooltipHelper.AssignTooltip(AutokillDropdown.gameObject, "Autokill Type");
            AutokillField = Content.Find("autokill/tod-value").GetComponent<InputField>();
            AutokillSetButton = Content.Find("autokill/|").GetComponent<Button>();
            CollapseToggle = Content.Find("autokill/collapse").GetComponent<Toggle>();

            #endregion

            #region Parent

            ParentButton = Content.Find("parent/text").gameObject.AddComponent<FunctionButtonStorage>();
            ParentButton.button = ParentButton.GetComponent<Button>();
            ParentButton.label = ParentButton.transform.Find("text").GetComponent<Text>();
            ParentInfo = ParentButton.GetComponent<HoverTooltip>();
            ParentMoreButton = Content.Find("parent/more").GetComponent<Button>();
            ParentSettingsParent = Content.Find("parent_more").gameObject;
            ParentDesyncToggle = ParentSettingsParent.transform.Find("desync").GetComponent<Toggle>();
            ParentSearchButton = Content.Find("parent/parent").GetComponent<Button>();
            ParentClearButton = Content.Find("parent/clear parent").GetComponent<Button>();
            ParentPickerButton = Content.Find("parent/parent picker").GetComponent<Button>();

            var parentSettingsSizeFitter = ParentSettingsParent.gameObject.GetOrAddComponent<ContentSizeFitter>();
            parentSettingsSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

            for (int i = 0; i < 3; i++)
            {
                var name = i switch
                {
                    0 => "pos",
                    1 => "sca",
                    _ => "rot"
                };

                var row = ParentSettingsParent.transform.Find($"{name}_row");
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
            }

            var parentSettingsBG = ParentSettingsParent.transform.Find("bg").GetComponent<Image>();
            parentSettingsBG.gameObject.SetActive(true);
            EditorThemeManager.ApplyGraphic(parentSettingsBG, ThemeGroup.Background_3, true);

            var parentSettingsLine = Creator.NewUIObject("line", ParentSettingsParent.transform);
            RectValues.Default.SizeDelta(356f, 6f).AssignToRectTransform(parentSettingsLine.transform.AsRT());
            var parentSettingsLineImage = parentSettingsLine.AddComponent<Image>();
            EditorThemeManager.ApplyGraphic(parentSettingsLineImage, ThemeGroup.Toggle_1, true);

            #endregion

            #region Origin

            OriginParent = Content.Find("origin").AsRT();
            OriginXField = OriginParent.Find("x").gameObject.GetComponent<InputFieldStorage>();
            TriggerHelper.InversableField(OriginXField);
            OriginYField = OriginParent.Find("y").gameObject.GetComponent<InputFieldStorage>();
            TriggerHelper.InversableField(OriginYField);

            for (int i = 0; i < 3; i++)
            {
                OriginXToggles.Add(OriginParent.Find("origin-x").GetChild(i).GetComponent<Toggle>());
                OriginYToggles.Add(OriginParent.Find("origin-y").GetChild(i).GetComponent<Toggle>());
            }

            #endregion

            #region Gradient / Shape

            // Color Blend
            {
                var label = EditorPrefabHolder.Instance.Labels.Duplicate(Content, "colorblend_label");
                var index = Content.Find("shape").GetSiblingIndex() - 1;
                label.transform.SetSiblingIndex(index);

                ColorBlendModeLabel = label.transform.GetChild(0).GetComponent<Text>();
                ColorBlendModeLabel.text = "Color Blend Mode";
                EditorThemeManager.ApplyLightText(ColorBlendModeLabel);

                var colorBlendMode = EditorPrefabHolder.Instance.Dropdown.Duplicate(Content, "colorblend", index + 1);
                ColorBlendModeDropdown = colorBlendMode.GetComponent<Dropdown>();
                ColorBlendModeDropdown.options = CoreHelper.ToOptionData<ColorBlendMode>();

                EditorThemeManager.ApplyDropdown(ColorBlendModeDropdown);
            }

            // Gradient
            {
                try
                {
                    var index = Content.Find("shape").GetSiblingIndex();
                    Content.GetChild(index - 1).GetComponentInChildren<Text>().text = "Gradient / Shape";
                    var gradient = Content.Find("shape").gameObject.Duplicate(Content, "gradienttype", index);
                    GradientParent = gradient.transform.AsRT();

                    CoreHelper.DestroyChildren(GradientParent, 1, GradientParent.childCount - 1);

                    CoreHelper.Destroy(gradient.GetComponent<ToggleGroup>());

                    // Normal
                    {
                        var normalToggle = GradientParent.GetChild(0);
                        var normalToggleImage = normalToggle.Find("Image").GetComponent<Image>();
                        normalToggleImage.sprite = EditorSprites.CloseSprite;

                        EditorThemeManager.ApplyGraphic(normalToggleImage, ThemeGroup.Toggle_1_Check);
                        var tog = normalToggle.GetComponent<Toggle>();
                        tog.group = null;
                        EditorThemeManager.ApplyToggle(tog, ThemeGroup.Background_1);
                    }

                    // Right
                    {
                        var normalToggle = GradientParent.GetChild(0).gameObject.Duplicate(gradient.transform, "2");
                        var normalToggleImage = normalToggle.transform.Find("Image").GetComponent<Image>();
                        normalToggleImage.sprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/gradients/linear_right{FileFormat.PNG.Dot()}"));

                        EditorThemeManager.ApplyGraphic(normalToggleImage, ThemeGroup.Toggle_1_Check);
                        var tog = normalToggle.GetComponent<Toggle>();
                        tog.group = null;
                        EditorThemeManager.ApplyToggle(tog, ThemeGroup.Background_1);
                    }

                    // Left
                    {
                        var normalToggle = GradientParent.GetChild(0).gameObject.Duplicate(gradient.transform, "3");
                        var normalToggleImage = normalToggle.transform.Find("Image").GetComponent<Image>();
                        normalToggleImage.sprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/gradients/linear_left{FileFormat.PNG.Dot()}"));

                        EditorThemeManager.ApplyGraphic(normalToggleImage, ThemeGroup.Toggle_1_Check);
                        var tog = normalToggle.GetComponent<Toggle>();
                        tog.group = null;
                        EditorThemeManager.ApplyToggle(tog, ThemeGroup.Background_1);
                    }

                    // In
                    {
                        var normalToggle = GradientParent.GetChild(0).gameObject.Duplicate(gradient.transform, "4");
                        var normalToggleImage = normalToggle.transform.Find("Image").GetComponent<Image>();
                        normalToggleImage.sprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/gradients/radial_in{FileFormat.PNG.Dot()}"));

                        EditorThemeManager.ApplyGraphic(normalToggleImage, ThemeGroup.Toggle_1_Check);
                        var tog = normalToggle.GetComponent<Toggle>();
                        tog.group = null;
                        EditorThemeManager.ApplyToggle(tog, ThemeGroup.Background_1);
                    }

                    // Out
                    {
                        var normalToggle = GradientParent.GetChild(0).gameObject.Duplicate(gradient.transform, "5");
                        var normalToggleImage = normalToggle.transform.Find("Image").GetComponent<Image>();
                        normalToggleImage.sprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/gradients/radial_out{FileFormat.PNG.Dot()}"));

                        EditorThemeManager.ApplyGraphic(normalToggleImage, ThemeGroup.Toggle_1_Check);
                        var tog = normalToggle.GetComponent<Toggle>();
                        tog.group = null;
                        EditorThemeManager.ApplyToggle(tog, ThemeGroup.Background_1);
                    }

                    // Gradient Settings
                    {
                        var gradientScale = EditorPrefabHolder.Instance.NumberInputField.Duplicate(Content, "gradientscale", index + 1);
                        var gradientRotation = EditorPrefabHolder.Instance.NumberInputField.Duplicate(Content, "gradientrotation", index + 2);
                        var gradientScaleStorage = gradientScale.GetComponent<InputFieldStorage>();
                        var gradientRotationStorage = gradientRotation.GetComponent<InputFieldStorage>();

                        CoreHelper.Delete(gradientScaleStorage.addButton.gameObject);
                        CoreHelper.Delete(gradientScaleStorage.subButton.gameObject);
                        CoreHelper.Delete(gradientScaleStorage.leftGreaterButton.gameObject);
                        CoreHelper.Delete(gradientScaleStorage.middleButton.gameObject);
                        CoreHelper.Delete(gradientScaleStorage.rightGreaterButton.gameObject);

                        EditorThemeManager.ApplyInputField(gradientScaleStorage.inputField);
                        EditorThemeManager.ApplySelectable(gradientScaleStorage.leftButton, ThemeGroup.Function_2, false);
                        EditorThemeManager.ApplySelectable(gradientScaleStorage.rightButton, ThemeGroup.Function_2, false);

                        CoreHelper.Delete(gradientRotationStorage.addButton.gameObject);
                        CoreHelper.Delete(gradientRotationStorage.subButton.gameObject);
                        CoreHelper.Delete(gradientRotationStorage.leftGreaterButton.gameObject);
                        CoreHelper.Delete(gradientRotationStorage.middleButton.gameObject);
                        CoreHelper.Delete(gradientRotationStorage.rightGreaterButton.gameObject);

                        EditorThemeManager.ApplyInputField(gradientRotationStorage.inputField);
                        EditorThemeManager.ApplySelectable(gradientRotationStorage.leftButton, ThemeGroup.Function_2, false);
                        EditorThemeManager.ApplySelectable(gradientRotationStorage.rightButton, ThemeGroup.Function_2, false);

                        var gradientScaleLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(gradientScale.transform, "label", 0);
                        var gradientScaleLabelText = gradientScaleLabel.GetComponent<Text>();
                        gradientScaleLabelText.alignment = TextAnchor.MiddleLeft;
                        gradientScaleLabelText.text = "Scale";
                        gradientScaleLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                        EditorThemeManager.ApplyLightText(gradientScaleLabelText);
                        var gradientScaleLabelLayout = gradientScaleLabel.AddComponent<LayoutElement>();
                        gradientScaleLabelLayout.minWidth = 100f;

                        var gradientRotationLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(gradientRotation.transform, "label", 0);
                        var gradientRotationLabelText = gradientRotationLabel.GetComponent<Text>();
                        gradientRotationLabelText.alignment = TextAnchor.MiddleLeft;
                        gradientRotationLabelText.text = "Rotation";
                        gradientRotationLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                        EditorThemeManager.ApplyLightText(gradientRotationLabelText);
                        var gradientRotationLabelLayout = gradientRotationLabel.AddComponent<LayoutElement>();
                        gradientRotationLabelLayout.minWidth = 100f;
                    }

                    var shift = EditorPrefabHolder.Instance.ToggleButton.Duplicate(colorDialog, "shift", 16);
                    var shiftToggleButton = shift.GetComponent<ToggleButtonStorage>();
                    shiftToggleButton.label.text = "Shift Dialog Down";
                    shiftToggleButton.toggle.SetIsOnWithoutNotify(false);
                    shiftToggleButton.toggle.onValueChanged.NewListener(_val =>
                    {
                        ObjectEditor.inst.colorShifted = _val;
                        shiftToggleButton.label.text = _val ? "Shift Dialog Up" : "Shift Dialog Down";
                        var animation = new RTAnimation("shift color UI");
                        animation.animationHandlers = new List<AnimationHandlerBase>
                        {
                            new AnimationHandler<float>(new List<IKeyframe<float>>
                            {
                                new FloatKeyframe(0f, _val ? 0f : 195f, Ease.Linear),
                                new FloatKeyframe(0.3f, _val ? 195f : 0f, Ease.CircOut),
                                new FloatKeyframe(0.32f, _val ? 195f : 0f, Ease.Linear),
                            }, x => { if (ObjEditor.inst) ObjEditor.inst.KeyframeDialogs[3].transform.AsRT().anchoredPosition = new Vector2(0f, x); }),
                        };

                        animation.onComplete = () =>
                        {
                            if (ObjEditor.inst)
                                ObjEditor.inst.KeyframeDialogs[3].transform.AsRT().anchoredPosition = new Vector2(0f, _val ? 195f : 0f);
                            AnimationManager.inst.Remove(animation.id);
                        };

                        AnimationManager.inst.Play(animation);
                    });

                    EditorThemeManager.ApplySelectable(shiftToggleButton.toggle, ThemeGroup.Function_2);
                    EditorThemeManager.ApplyGraphic(shiftToggleButton.label, ThemeGroup.Function_2_Text);

                    var endColorLabel = colorDialog.Find("color_label").gameObject.Duplicate(colorDialog, "gradient_color_label");
                    endColorLabel.GetComponentInChildren<Text>().text = "End Color";
                    var endColor = colorDialog.Find("color").gameObject.Duplicate(colorDialog, "gradient_color");

                    var endOpacityLabel = colorDialog.Find("opacity_label").gameObject.Duplicate(colorDialog, "gradient_opacity_label");
                    endOpacityLabel.GetComponentInChildren<Text>().text = "End Opacity";
                    var endOpacity = colorDialog.Find("opacity").gameObject.Duplicate(colorDialog, "gradient_opacity");
                    CoreHelper.Delete(endOpacity.transform.Find("collision"));

                    new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "End Hue", "End Sat", "End Val").Init(
                        EditorElement.InitSettings.Default
                        .Parent(colorDialog)
                        .Name("gradient_huesatval_label")
                        .Rect(new RectValues(new Vector2(8f, -330f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(371f, 20f))));

                    colorDialog.Find("huesatval").gameObject.Duplicate(colorDialog, "gradient_huesatval");

                    ObjEditor.inst.colorButtons.Clear();
                    for (int i = 1; i <= 18; i++)
                        ObjEditor.inst.colorButtons.Add(colorDialog.Find("color/" + i).GetComponent<Toggle>());

                    ObjectEditor.inst.gradientColorButtons.Clear();
                    for (int i = 0; i < endColor.transform.childCount; i++)
                        ObjectEditor.inst.gradientColorButtons.Add(endColor.transform.GetChild(i).GetComponent<Toggle>());
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
            }

            GradientParent = Content.Find("gradienttype").AsRT();
            ShapesLabel = Content.GetChild(GradientParent.GetSiblingIndex() - 1).GetChild(0).GetComponent<Text>();
            for (int i = 0; i < GradientParent.childCount; i++)
                GradientToggles.Add(GradientParent.GetChild(i).GetComponent<Toggle>());
            GradientScale = Content.Find("gradientscale").GetComponent<InputFieldStorage>();
            GradientRotation = Content.Find("gradientrotation").GetComponent<InputFieldStorage>();

            InitShapes();

            #endregion

            #region Render Depth / Type

            DepthParent = Content.Find("depth").AsRT();
            DepthField = Content.Find("depth input/depth").GetComponent<InputFieldStorage>();

            DepthSlider = Content.Find("depth/depth").GetComponent<Slider>();
            DepthSliderLeftButton = DepthParent.Find("<").GetComponent<Button>();
            DepthSliderRightButton = DepthParent.Find(">").GetComponent<Button>();
            RenderTypeDropdown = Content.Find("rendertype").GetComponent<Dropdown>();

            #endregion

            #region Editor Settings

            EditorSettingsParent = Content.Find("editor").AsRT();
            EditorSettingsParent.GetComponent<HorizontalLayoutGroup>().childControlWidth = true;

            var editorSettingsIndex = EditorSettingsParent.GetSiblingIndex() + 1;
            var editorIndexLabel = EditorPrefabHolder.Instance.Labels.Duplicate(Content, "indexer_label", editorSettingsIndex);
            var editorIndexLabelText = editorIndexLabel.transform.GetChild(0).GetComponent<Text>();
            editorIndexLabelText.text = "Editor Index";
            EditorThemeManager.ApplyLightText(editorIndexLabelText);

            var indexEditor = EditorPrefabHolder.Instance.NumberInputField.Duplicate(Content, "indexer", editorSettingsIndex + 1);
            EditorIndexField = indexEditor.GetComponent<InputFieldStorage>();

            CoreHelper.Delete(EditorIndexField.middleButton);
            EditorThemeManager.ApplyInputField(EditorIndexField);
            
            EditorLayerField = EditorSettingsParent.Find("layers").GetComponent<InputField>();
            EditorLayerField.image = EditorLayerField.GetComponent<Image>();
            BinSlider = EditorSettingsParent.Find("bin").GetComponent<Slider>();
            EditorLayerTogglesParent = EditorSettingsParent.Find("layer").AsRT();
            EditorLayerToggles = EditorLayerTogglesParent.GetComponentsInChildren<Toggle>();
            CoreHelper.Destroy(EditorLayerTogglesParent.GetComponent<ToggleGroup>());
            RTEditor.inst.SetupEditorLayers(this);

            EditorHelper.SetComplexity(EditorLayerTogglesParent.gameObject, "editor_layer_toggles", Complexity.Simple);
            EditorHelper.SetComplexity(EditorLayerField.gameObject, "editor_layer_field", Complexity.Normal);

            var editorGroupLabel = EditorPrefabHolder.Instance.Labels.Duplicate(Content, "group_label", editorSettingsIndex + 2);
            var editorGroupLabelText = editorGroupLabel.transform.GetChild(0).GetComponent<Text>();
            editorGroupLabelText.text = "Editor Group";
            EditorThemeManager.ApplyLightText(editorGroupLabelText);

            var editorGroup = EditorPrefabHolder.Instance.StringInputField.Duplicate(Content, "group", editorSettingsIndex + 3);
            EditorGroupField = editorGroup.GetComponent<InputField>();

            EditorThemeManager.ApplyInputField(EditorGroupField);

            var baseColorParent = EditorPrefabHolder.Instance.Labels.Duplicate(Content, "base color", editorSettingsIndex + 4);
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

            var selectColorParent = EditorPrefabHolder.Instance.Labels.Duplicate(Content, "select color", editorSettingsIndex + 5);
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

            var textColorParent = EditorPrefabHolder.Instance.Labels.Duplicate(Content, "text color", editorSettingsIndex + 6);
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

            var markColorParent = EditorPrefabHolder.Instance.Labels.Duplicate(Content, "mark color", editorSettingsIndex + 7);
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

            #region Prefab

            CollapsePrefabLabel = Content.Find("collapselabel").gameObject;

            var applyPrefab = Content.Find("applyprefab");
            var siblingIndex = applyPrefab.GetSiblingIndex();

            var prefabName = EditorPrefabHolder.Instance.Labels.Duplicate(Content, "prefab name", siblingIndex - 1);
            PrefabName = prefabName;
            PrefabNameText = prefabName.transform.GetChild(0).GetComponent<Text>();
            PrefabNameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            EditorThemeManager.ApplyLightText(PrefabNameText);

            var applyPrefabText = applyPrefab.transform.GetChild(0).GetComponent<Text>();

            var applyPrefabButton = applyPrefab.GetComponent<Button>();
            CoreHelper.Destroy(applyPrefab.GetComponent<Animator>());
            applyPrefabButton.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.ApplySelectable(applyPrefabButton, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(applyPrefabText, ThemeGroup.Function_2_Text);

            CollapsePrefabButton = applyPrefab.gameObject.GetOrAddComponent<FunctionButtonStorage>();
            CollapsePrefabButton.label = applyPrefabText;
            CollapsePrefabButton.button = applyPrefabButton;

            AssignPrefabLabel = EditorPrefabHolder.Instance.Labels.Duplicate(Content, "assignlabel", siblingIndex + 3);
            var assignPrefabLabelText = AssignPrefabLabel.transform.GetChild(0).GetComponent<Text>();
            assignPrefabLabelText.text = "Assign Object to a Prefab";
            assignPrefabLabelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            EditorThemeManager.ApplyLightText(assignPrefabLabelText);

            var assignPrefab = EditorPrefabHolder.Instance.Function2Button.Duplicate(Content, "assign prefab", siblingIndex + 4);
            AssignPrefabButton = assignPrefab.GetOrAddComponent<FunctionButtonStorage>();
            AssignPrefabButton.Text = "Assign";
            EditorThemeManager.ApplySelectable(AssignPrefabButton.button, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(AssignPrefabButton.label, ThemeGroup.Function_2_Text);

            var removePrefab = EditorPrefabHolder.Instance.Function2Button.Duplicate(Content, "remove prefab", siblingIndex + 5);
            RemovePrefabButton = removePrefab.GetOrAddComponent<FunctionButtonStorage>();
            RemovePrefabButton.Text = "Remove";
            EditorThemeManager.ApplySelectable(RemovePrefabButton.button, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(RemovePrefabButton.label, ThemeGroup.Function_2_Text);

            EditorHelper.SetComplexity(AssignPrefabLabel, Complexity.Normal);
            EditorHelper.SetComplexity(assignPrefab, Complexity.Normal);
            EditorHelper.SetComplexity(removePrefab, Complexity.Normal);

            #endregion

            #region Unity Explorer

            if (ModCompatibility.UnityExplorerInstalled)
            {
                UnityExplorerLabel = Content.Find("unity explorer label").GetChild(0).GetComponent<Text>();
                InspectBeatmapObjectButton = Content.Find("inspectbeatmapobject").GetComponent<FunctionButtonStorage>();
                InspectRuntimeObjectButton = Content.Find("inspectlevelobject").GetComponent<FunctionButtonStorage>();
                InspectTimelineObjectButton = Content.Find("inspecttimelineobject").GetComponent<FunctionButtonStorage>();
            }

            #endregion

            var animIDLabel = EditorPrefabHolder.Instance.Labels.Duplicate(Content, "anim_label");
            var animIDLabelText = animIDLabel.transform.GetChild(0).GetComponent<Text>();
            animIDLabelText.text = "Animation ID";
            EditorThemeManager.ApplyLightText(animIDLabelText);

            var animID = EditorPrefabHolder.Instance.StringInputField.Duplicate(Content, "anim");
            AnimIDField = animID.GetComponent<InputField>();

            EditorThemeManager.ApplyInputField(AnimIDField);
            AnimIDField.gameObject.GetOrAddComponent<InputFieldSwapper>().Init(AnimIDField, InputFieldSwapper.Type.String);

            try
            {
                ModifiersDialog = new ModifiersEditorDialog();
                ModifiersDialog.getReference = () => EditorTimeline.inst.CurrentSelection.GetData<Core.Data.Modifiers.IModifierReference>();
                ModifiersDialog.Init(Content);
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Had an error with setting up modifies UI: {ex}");
            }

            for (int i = 0; i < ObjEditor.inst.KeyframeDialogs.Count; i++)
            {
                var keyframeDialog = new KeyframeDialog(i);
                keyframeDialog.GameObject = ObjEditor.inst.KeyframeDialogs[i];
                keyframeDialog.isMulti = i == 4;
                keyframeDialog.isObjectKeyframe = true;
                keyframeDialog.Init();
                keyframeDialogs.Add(keyframeDialog);
            }

            Timeline = new KeyframeTimeline();
            Timeline.startColorsReference = ObjEditor.inst.colorButtons;
            Timeline.endColorsReference = ObjectEditor.inst.gradientColorButtons;
            Timeline.setTime = true;
            Timeline.Init(this);
        }

        bool updatedShapes;
        void InitShapes()
        {
            CoreHelper.Log($"Caching values...");
            var shape = ObjEditor.inst.ObjectView.transform.Find("shape");
            var shapeSettings = ObjEditor.inst.ObjectView.transform.Find("shapesettings");

            ShapeTypesParent = shape.AsRT();
            ShapeOptionsParent = shapeSettings.AsRT();

            // Make Shape list scrollable, for any more shapes I decide to add.
            {
                var scroll = shape.gameObject.AddComponent<ScrollRect>();
                shape.gameObject.AddComponent<Mask>();
                var image = shape.gameObject.AddComponent<Image>();

                scroll.scrollSensitivity = 20f;
                scroll.horizontal = true;
                scroll.vertical = false;
                scroll.content = shape.AsRT();
                scroll.viewport = shape.AsRT();
                image.color = new Color(1f, 1f, 1f, 0.01f);
            }

            ObjectEditor.inst.shapeButtonPrefab = shape.Find("1").gameObject.Duplicate(ObjectEditor.inst.transform);
            ObjectEditor.inst.shapeButtonPrefab.GetComponent<Toggle>().group = null;

            var shapeGLG = shape.gameObject.GetOrAddComponent<GridLayoutGroup>();
            shapeGLG.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            shapeGLG.constraintCount = 1;
            shapeGLG.spacing = new Vector2(7.6f, 0f);

            if (updatedShapes)
                return;

            CoreHelper.Log($"Removing...");
            CoreHelper.Destroy(shape.GetComponent<ToggleGroup>(), true);

            var toDestroy = new List<GameObject>();

            for (int i = 0; i < shape.childCount; i++)
                toDestroy.Add(shape.GetChild(i).gameObject);

            for (int i = 0; i < shapeSettings.childCount; i++)
            {
                if (i != 4 && i != 6)
                    for (int j = 0; j < shapeSettings.GetChild(i).childCount; j++)
                        toDestroy.Add(shapeSettings.GetChild(i).GetChild(j).gameObject);
            }

            foreach (var obj in toDestroy)
                CoreHelper.Delete(obj);

            toDestroy = null;

            CoreHelper.Log($"Adding shapes...");
            for (int i = 0; i < ShapeManager.inst.Shapes2D.Count; i++)
            {
                var shapeType = (ShapeType)i;

                var obj = ObjectEditor.inst.shapeButtonPrefab.Duplicate(shape, (i + 1).ToString(), i);
                if (obj.transform.Find("Image") && obj.transform.Find("Image").gameObject.TryGetComponent(out Image image))
                {
                    image.sprite = ShapeManager.inst.Shapes2D[i].icon;
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

                ShapeToggles.Add(shapeToggle);

                ShapeOptionToggles.Add(new List<Toggle>());

                var so = shapeSettings.Find((i + 1).ToString());

                if (shapeType != ShapeType.Text && shapeType != ShapeType.Image && shapeType != ShapeType.Polygon)
                {
                    if (!so)
                    {
                        so = shapeSettings.Find("6").gameObject.Duplicate(shapeSettings, (i + 1).ToString()).transform;
                        CoreHelper.DestroyChildren(so);
                    }

                    var rect = so.AsRT();
                    if (!so.GetComponent<ScrollRect>())
                    {
                        var scroll = so.gameObject.AddComponent<ScrollRect>();
                        so.gameObject.AddComponent<Mask>();
                        var ad = so.gameObject.AddComponent<Image>();

                        scroll.scrollSensitivity = 20f;
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
                            image1.sprite = ShapeManager.inst.Shapes2D[i][j].icon;
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

                        ShapeOptionToggles[i].Add(shapeOptionToggle);

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

                if (shapeType == ShapeType.Image)
                {
                    var select = so.Find("select").GetComponent<Button>();
                    CoreHelper.Destroy(select.GetComponent<Animator>());
                    select.transition = Selectable.Transition.ColorTint;
                    EditorThemeManager.ApplySelectable(select, ThemeGroup.Function_2, false);

                    EditorThemeManager.ApplyLightText(so.Find("text").GetComponent<Text>());

                    var setData = EditorPrefabHolder.Instance.Function1Button.Duplicate(so, "set", 5);
                    var setDataText = setData.transform.GetChild(0).GetComponent<Text>();
                    setDataText.text = "Store Data";
                    ((RectTransform)setData.transform).sizeDelta = new Vector2(70f, 32f);

                    setData.GetComponent<LayoutElement>().minWidth = 130f;

                    EditorThemeManager.ApplyGraphic(setData.GetComponent<Image>(), ThemeGroup.Function_1, true);
                    EditorThemeManager.ApplyGraphic(setDataText, ThemeGroup.Function_1_Text);
                }

                if (shapeType == ShapeType.Polygon)
                {
                    if (!so)
                    {
                        so = shapeSettings.Find("6").gameObject.Duplicate(shapeSettings, (i + 1).ToString()).transform;
                        CoreHelper.DestroyChildren(so);
                    }

                    var rect = so.AsRT();
                    CoreHelper.Destroy(so.GetComponent<ScrollRect>(), true);
                    CoreHelper.Destroy(so.GetComponent<HorizontalLayoutGroup>(), true);

                    so.gameObject.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.05f);

                    var verticalLayoutGroup = so.gameObject.GetOrAddComponent<VerticalLayoutGroup>();
                    verticalLayoutGroup.spacing = 4f;

                    // Polygon Settings
                    {
                        #region Radius

                        var radius = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "radius");
                        var radiusStorage = radius.GetComponent<InputFieldStorage>();

                        CoreHelper.Delete(radiusStorage.addButton);
                        CoreHelper.Delete(radiusStorage.subButton);
                        CoreHelper.Delete(radiusStorage.leftGreaterButton);
                        CoreHelper.Delete(radiusStorage.middleButton);
                        CoreHelper.Delete(radiusStorage.rightGreaterButton);

                        EditorThemeManager.ApplyInputField(radiusStorage);

                        var radiusLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(radius.transform, "label", 0);
                        var radiusLabelText = radiusLabel.GetComponent<Text>();
                        radiusLabelText.alignment = TextAnchor.MiddleLeft;
                        radiusLabelText.text = "Radius";
                        radiusLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                        EditorThemeManager.ApplyLightText(radiusLabelText);
                        var radiusLabelLayout = radiusLabel.AddComponent<LayoutElement>();
                        radiusLabelLayout.minWidth = 100f;

                        #endregion

                        #region Sides

                        var sides = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "sides");
                        var sidesStorage = sides.GetComponent<InputFieldStorage>();

                        CoreHelper.Delete(sidesStorage.addButton);
                        CoreHelper.Delete(sidesStorage.subButton);
                        CoreHelper.Delete(sidesStorage.leftGreaterButton);
                        CoreHelper.Delete(sidesStorage.middleButton);
                        CoreHelper.Delete(sidesStorage.rightGreaterButton);

                        EditorThemeManager.ApplyInputField(sidesStorage);

                        var sidesLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(sides.transform, "label", 0);
                        var sidesLabelText = sidesLabel.GetComponent<Text>();
                        sidesLabelText.alignment = TextAnchor.MiddleLeft;
                        sidesLabelText.text = "Sides";
                        sidesLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                        EditorThemeManager.ApplyLightText(sidesLabelText);
                        var sidesLabelLayout = sidesLabel.AddComponent<LayoutElement>();
                        sidesLabelLayout.minWidth = 100f;

                        #endregion

                        #region Roundness

                        var roundness = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "roundness");
                        var roundnessStorage = roundness.GetComponent<InputFieldStorage>();

                        CoreHelper.Delete(roundnessStorage.addButton);
                        CoreHelper.Delete(roundnessStorage.subButton);
                        CoreHelper.Delete(roundnessStorage.leftGreaterButton);
                        CoreHelper.Delete(roundnessStorage.middleButton);
                        CoreHelper.Delete(roundnessStorage.rightGreaterButton);

                        EditorThemeManager.ApplyInputField(roundnessStorage);

                        var roundnessLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(roundness.transform, "label", 0);
                        var roundnessLabelText = roundnessLabel.GetComponent<Text>();
                        roundnessLabelText.alignment = TextAnchor.MiddleLeft;
                        roundnessLabelText.text = "Roundness";
                        roundnessLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                        EditorThemeManager.ApplyLightText(roundnessLabelText);
                        var roundnessLabelLayout = roundnessLabel.AddComponent<LayoutElement>();
                        roundnessLabelLayout.minWidth = 100f;

                        #endregion

                        #region Thickness

                        var thickness = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "thickness");
                        var thicknessStorage = thickness.GetComponent<InputFieldStorage>();

                        CoreHelper.Delete(thicknessStorage.addButton);
                        CoreHelper.Delete(thicknessStorage.subButton);
                        CoreHelper.Delete(thicknessStorage.leftGreaterButton);
                        CoreHelper.Delete(thicknessStorage.middleButton);
                        CoreHelper.Delete(thicknessStorage.rightGreaterButton);

                        EditorThemeManager.ApplyInputField(thicknessStorage);

                        var thicknessLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(thickness.transform, "label", 0);
                        var thicknessLabelText = thicknessLabel.GetComponent<Text>();
                        thicknessLabelText.alignment = TextAnchor.MiddleLeft;
                        thicknessLabelText.text = "Thickness";
                        thicknessLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                        EditorThemeManager.ApplyLightText(thicknessLabelText);
                        var thicknessLabelLayout = thicknessLabel.AddComponent<LayoutElement>();
                        thicknessLabelLayout.minWidth = 100f;

                        #endregion

                        #region Thickness Offset

                        var thicknessOffset = Creator.NewUIObject("thickness offset", so);
                        var thicknessOffsetLayout = thicknessOffset.AddComponent<HorizontalLayoutGroup>();

                        var thicknessOffsetLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(thicknessOffset.transform, "label");
                        var thicknessOffsetLabelText = thicknessOffsetLabel.GetComponent<Text>();
                        thicknessOffsetLabelText.alignment = TextAnchor.MiddleLeft;
                        thicknessOffsetLabelText.text = "Thick Offset";
                        thicknessOffsetLabelText.rectTransform.sizeDelta = new Vector2(130f, 32f);
                        EditorThemeManager.ApplyLightText(thicknessOffsetLabelText);
                        var thicknessOffsetLabelLayout = thicknessOffsetLabel.AddComponent<LayoutElement>();
                        thicknessOffsetLabelLayout.minWidth = 130f;

                        var thicknessOffsetX = EditorPrefabHolder.Instance.NumberInputField.Duplicate(thicknessOffset.transform, "x");
                        var thicknessOffsetXStorage = thicknessOffsetX.GetComponent<InputFieldStorage>();

                        CoreHelper.Delete(thicknessOffsetXStorage.addButton);
                        CoreHelper.Delete(thicknessOffsetXStorage.subButton);
                        CoreHelper.Delete(thicknessOffsetXStorage.leftGreaterButton);
                        CoreHelper.Delete(thicknessOffsetXStorage.middleButton);
                        CoreHelper.Delete(thicknessOffsetXStorage.rightGreaterButton);

                        EditorThemeManager.ApplyInputField(thicknessOffsetXStorage);
                            
                        var thicknessOffsetY = EditorPrefabHolder.Instance.NumberInputField.Duplicate(thicknessOffset.transform, "y");
                        var thicknessOffsetYStorage = thicknessOffsetY.GetComponent<InputFieldStorage>();

                        CoreHelper.Delete(thicknessOffsetYStorage.addButton);
                        CoreHelper.Delete(thicknessOffsetYStorage.subButton);
                        CoreHelper.Delete(thicknessOffsetYStorage.leftGreaterButton);
                        CoreHelper.Delete(thicknessOffsetYStorage.middleButton);
                        CoreHelper.Delete(thicknessOffsetYStorage.rightGreaterButton);

                        EditorThemeManager.ApplyInputField(thicknessOffsetYStorage);

                        #endregion
                            
                        #region Thickness Scale

                        var thicknessScale = Creator.NewUIObject("thickness scale", so);
                        var thicknessScaleLayout = thicknessScale.AddComponent<HorizontalLayoutGroup>();

                        var thicknessScaleLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(thicknessScale.transform, "label");
                        var thicknessScaleLabelText = thicknessScaleLabel.GetComponent<Text>();
                        thicknessScaleLabelText.alignment = TextAnchor.MiddleLeft;
                        thicknessScaleLabelText.text = "Thick Scale";
                        thicknessScaleLabelText.rectTransform.sizeDelta = new Vector2(130f, 32f);
                        EditorThemeManager.ApplyLightText(thicknessScaleLabelText);
                        var thicknessScaleLabelLayout = thicknessScaleLabel.AddComponent<LayoutElement>();
                        thicknessScaleLabelLayout.minWidth = 130f;

                        var thicknessScaleX = EditorPrefabHolder.Instance.NumberInputField.Duplicate(thicknessScale.transform, "x");
                        var thicknessScaleXStorage = thicknessScaleX.GetComponent<InputFieldStorage>();

                        CoreHelper.Delete(thicknessScaleXStorage.addButton);
                        CoreHelper.Delete(thicknessScaleXStorage.subButton);
                        CoreHelper.Delete(thicknessScaleXStorage.leftGreaterButton);
                        CoreHelper.Delete(thicknessScaleXStorage.middleButton);
                        CoreHelper.Delete(thicknessScaleXStorage.rightGreaterButton);

                        EditorThemeManager.ApplyInputField(thicknessScaleXStorage);
                            
                        var thicknessScaleY = EditorPrefabHolder.Instance.NumberInputField.Duplicate(thicknessScale.transform, "y");
                        var thicknessScaleYStorage = thicknessScaleY.GetComponent<InputFieldStorage>();

                        CoreHelper.Delete(thicknessScaleYStorage.addButton);
                        CoreHelper.Delete(thicknessScaleYStorage.subButton);
                        CoreHelper.Delete(thicknessScaleYStorage.leftGreaterButton);
                        CoreHelper.Delete(thicknessScaleYStorage.middleButton);
                        CoreHelper.Delete(thicknessScaleYStorage.rightGreaterButton);

                        EditorThemeManager.ApplyInputField(thicknessScaleYStorage);

                        #endregion

                        #region Thickness Rotation

                        var thicknessRotation = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "thickness angle");
                        var thicknessRotationStorage = thicknessRotation.GetComponent<InputFieldStorage>();

                        CoreHelper.Delete(thicknessRotationStorage.addButton);
                        CoreHelper.Delete(thicknessRotationStorage.subButton);
                        CoreHelper.Delete(thicknessRotationStorage.leftGreaterButton);
                        CoreHelper.Delete(thicknessRotationStorage.middleButton);
                        CoreHelper.Delete(thicknessRotationStorage.rightGreaterButton);

                        EditorThemeManager.ApplyInputField(thicknessRotationStorage);

                        var thicknessRotationLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(thicknessRotation.transform, "label", 0);
                        var thicknessRotationLabelText = thicknessRotationLabel.GetComponent<Text>();
                        thicknessRotationLabelText.alignment = TextAnchor.MiddleLeft;
                        thicknessRotationLabelText.text = "Thick Angle";
                        thicknessRotationLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                        EditorThemeManager.ApplyLightText(thicknessRotationLabelText);
                        var thicknessRotationLabelLayout = thicknessRotationLabel.AddComponent<LayoutElement>();
                        thicknessRotationLabelLayout.minWidth = 100f;

                        #endregion

                        #region Slices

                        var slices = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "slices");
                        var slicesStorage = slices.GetComponent<InputFieldStorage>();

                        CoreHelper.Delete(slicesStorage.addButton);
                        CoreHelper.Delete(slicesStorage.subButton);
                        CoreHelper.Delete(slicesStorage.leftGreaterButton);
                        CoreHelper.Delete(slicesStorage.middleButton);
                        CoreHelper.Delete(slicesStorage.rightGreaterButton);

                        EditorThemeManager.ApplyInputField(slicesStorage);

                        var slicesLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(slices.transform, "label", 0);
                        var slicesLabelText = slicesLabel.GetComponent<Text>();
                        slicesLabelText.alignment = TextAnchor.MiddleLeft;
                        slicesLabelText.text = "Slices";
                        slicesLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                        EditorThemeManager.ApplyLightText(slicesLabelText);
                        var slicesLabelLayout = slicesLabel.AddComponent<LayoutElement>();
                        slicesLabelLayout.minWidth = 100f;

                        #endregion

                        #region Angle

                        var rotation = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "rotation");
                        var rotationStorage = rotation.GetComponent<InputFieldStorage>();

                        CoreHelper.Delete(rotationStorage.addButton);
                        CoreHelper.Delete(rotationStorage.subButton);
                        CoreHelper.Delete(rotationStorage.leftGreaterButton);
                        CoreHelper.Delete(rotationStorage.middleButton);
                        CoreHelper.Delete(rotationStorage.rightGreaterButton);

                        EditorThemeManager.ApplyInputField(rotationStorage);

                        var rotationLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(rotation.transform, "label", 0);
                        var rotationLabelText = rotationLabel.GetComponent<Text>();
                        rotationLabelText.alignment = TextAnchor.MiddleLeft;
                        rotationLabelText.text = "Angle";
                        rotationLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                        EditorThemeManager.ApplyLightText(rotationLabelText);
                        var rotationLabelLayout = rotationLabel.AddComponent<LayoutElement>();
                        rotationLabelLayout.minWidth = 100f;

                        #endregion
                    }
                }
            }

            var textIF = shapeSettings.Find("5").GetComponent<InputField>();
            if (!textIF.transform.Find("edit"))
            {
                var button = EditorPrefabHolder.Instance.DeleteButton.Duplicate(textIF.transform, "edit");
                var buttonStorage = button.GetComponent<DeleteButtonStorage>();
                buttonStorage.Sprite = EditorSprites.EditSprite;
                EditorThemeManager.ApplySelectable(buttonStorage.button, ThemeGroup.Function_2);
                EditorThemeManager.ApplyGraphic(buttonStorage.image, ThemeGroup.Function_2_Text);
                buttonStorage.OnClick.NewListener(() => RTTextEditor.inst.SetInputField(textIF));
                UIManager.SetRectTransform(buttonStorage.baseImage.rectTransform, new Vector2(160f, 24f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(22f, 22f));
                EditorHelper.SetComplexity(button, Complexity.Advanced);
            }

            updatedShapes = true;
        }
    }
}
