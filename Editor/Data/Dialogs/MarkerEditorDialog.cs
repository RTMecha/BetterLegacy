using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class MarkerEditorDialog : EditorDialog
    {
        public MarkerEditorDialog() : base(MARKER_EDITOR) { }

        #region UI

        public RectTransform LeftContent { get; set; }

        public Text IndexText { get; set; }

        public InputField NameField { get; set; }

        public InputFieldStorage TimeField { get; set; }

        public InputFieldStorage DurationField { get; set; }

        public InputField DescriptionField { get; set; }

        public RectTransform ColorsParent { get; set; }

        public List<GameObject> Colors { get; set; } = new List<GameObject>();

        public RectTransform LayersContent { get; set; }

        public List<Button> AnnotationToolButtons { get; set; } = new List<Button>();

        public RectTransform AnnotationColorsParent { get; set; }

        public List<GameObject> AnnotationColors { get; set; } = new List<GameObject>();

        public InputField AnnotationHexColorField { get; set; }

        public InputFieldStorage AnnotationOpacityField { get; set; }

        public InputFieldStorage AnnotationThicknessField { get; set; }

        public ToggleButtonStorage AnnotationFixedCameraToggle { get; set; }

        #endregion

        public override void Init()
        {
            if (init)
                return;

            base.Init();
            var dialog = GameObject.transform;

            MarkerEditor.inst.dialog = dialog;
            MarkerEditor.inst.left = dialog.Find("data/left");
            MarkerEditor.inst.right = dialog.Find("data/right");

            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(RTMarkerEditor.inst.transform, "Scroll View");
            LeftContent = scrollView.transform.Find("Viewport/Content").AsRT();

            scrollView.transform.AsRT().sizeDelta = new Vector2(735f, 690f);

            var leftContentLayoutGroup = LeftContent.GetComponent<VerticalLayoutGroup>();
            leftContentLayoutGroup.spacing = 8f;
            leftContentLayoutGroup.padding = new RectOffset(left: 1, right: 0, top: 8, bottom: 8);

            MarkerEditor.inst.left.TransferChildren(LeftContent);
            scrollView.transform.SetParent(MarkerEditor.inst.left);

            var indexparent = Creator.NewUIObject("index", LeftContent, 0);
            indexparent.transform.AsRT().pivot = new Vector2(0f, 1f);
            indexparent.transform.AsRT().sizeDelta = new Vector2(371f, 32f);

            var index = Creator.NewUIObject("text", indexparent.transform);
            RectValues.FullAnchored.Pivot(0f, 1f).AssignToRectTransform(index.transform.AsRT());

            IndexText = index.AddComponent<Text>();
            IndexText.text = "Index: 0";
            IndexText.font = FontManager.inst.DefaultFont;
            IndexText.color = new Color(0.9f, 0.9f, 0.9f);
            IndexText.alignment = TextAnchor.MiddleLeft;
            IndexText.fontSize = 16;
            IndexText.horizontalOverflow = HorizontalWrapMode.Overflow;

            EditorThemeManager.ApplyLightText(IndexText);

            EditorHelper.SetComplexity(indexparent, Complexity.Normal);

            // Makes label consistent with other labels. Originally said "Marker Time" where other labels do not mention "Marker".
            var timeLabel = LeftContent.GetChild(3).GetChild(0).GetComponent<Text>();
            timeLabel.text = "Time";
            // Fixes "Name" label.
            var descriptionLabel = LeftContent.GetChild(5).GetChild(0).GetComponent<Text>();
            descriptionLabel.text = "Description";

            EditorThemeManager.ApplyGraphic(dialog.GetComponent<Image>(), ThemeGroup.Background_1);

            EditorThemeManager.ApplyInputField(MarkerEditor.inst.right.Find("InputField").GetComponent<InputField>(), ThemeGroup.Search_Field_2);

            var scrollbar = MarkerEditor.inst.right.transform.Find("Scrollbar").GetComponent<Scrollbar>();
            EditorThemeManager.ApplyGraphic(scrollbar.GetComponent<Image>(), ThemeGroup.Scrollbar_2, true);
            EditorThemeManager.ApplyGraphic(scrollbar.image, ThemeGroup.Scrollbar_2_Handle, true);

            EditorThemeManager.ApplyLightText(LeftContent.GetChild(1).GetChild(0).GetComponent<Text>());
            EditorThemeManager.ApplyLightText(timeLabel);
            EditorThemeManager.ApplyLightText(descriptionLabel);

            NameField = LeftContent.Find("name").GetComponent<InputField>();
            DescriptionField = LeftContent.Find("desc").GetComponent<InputField>();
            //DescriptionField.transform.AsRT().sizeDelta = new Vector2(371f, 192f);
            DescriptionField.transform.AsRT().sizeDelta = new Vector2(371f, 256f);

            EditorThemeManager.ApplyInputField(NameField);
            EditorThemeManager.ApplyInputField(DescriptionField);

            var time = EditorPrefabHolder.Instance.NumberInputField.Duplicate(LeftContent, "time new", 4);
            CoreHelper.Delete(LeftContent.Find("time").gameObject);

            TimeField = time.GetComponent<InputFieldStorage>();
            EditorThemeManager.ApplyInputField(TimeField);

            time.name = "time";

            new Labels(Labels.InitSettings.Default.Parent(LeftContent).Name("duration_label").SiblingIndex(5), "Duration");
            var duration = EditorPrefabHolder.Instance.NumberInputField.Duplicate(LeftContent, "duration", 6);
            DurationField = duration.GetComponent<InputFieldStorage>();
            EditorThemeManager.ApplyInputField(DurationField);

            // fixes color slot spacing
            LeftContent.Find("color").GetComponent<GridLayoutGroup>().spacing = new Vector2(8f, 8f);

            ColorsParent = LeftContent.Find("color").AsRT();

            new Labels(Labels.InitSettings.Default.Parent(LeftContent).Name("layers_label"), "Layers to appear on");

            var tagScrollView = Creator.NewUIObject("Layers Scroll View", LeftContent);

            tagScrollView.transform.AsRT().sizeDelta = new Vector2(522f, 40f);
            var scroll = tagScrollView.AddComponent<ScrollRect>();

            scroll.horizontal = true;
            scroll.vertical = false;

            var image = tagScrollView.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.01f);

            var mask = tagScrollView.AddComponent<Mask>();

            var layersViewport = Creator.NewUIObject("Viewport", tagScrollView.transform);
            RectValues.FullAnchored.AssignToRectTransform(layersViewport.transform.AsRT());

            var layersContent = Creator.NewUIObject("Content", layersViewport.transform);

            var layersContentGLG = layersContent.AddComponent<GridLayoutGroup>();
            layersContentGLG.cellSize = new Vector2(200f, 32f);
            layersContentGLG.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            layersContentGLG.constraintCount = 1;
            layersContentGLG.childAlignment = TextAnchor.MiddleLeft;
            layersContentGLG.spacing = new Vector2(8f, 0f);

            var tagContentCSF = layersContent.AddComponent<ContentSizeFitter>();
            tagContentCSF.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            tagContentCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

            scroll.viewport = layersViewport.transform.AsRT();
            scroll.content = layersContent.transform.AsRT();

            LayersContent = scroll.content;

            var button = EditorPrefabHolder.Instance.DeleteButton.Duplicate(DescriptionField.transform, "edit");
            var buttonStorage = button.GetComponent<DeleteButtonStorage>();
            buttonStorage.Sprite = EditorSprites.EditSprite;
            EditorThemeManager.ApplySelectable(buttonStorage.button, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(buttonStorage.image, ThemeGroup.Function_2_Text);
            buttonStorage.OnClick.NewListener(() => RTTextEditor.inst.SetInputField(DescriptionField));
            RectValues.Default.AnchoredPosition(171f, 112f).SizeDelta(22f, 22f).AssignToRectTransform(buttonStorage.baseImage.rectTransform);
            EditorHelper.SetComplexity(button, "marker/layers", Complexity.Advanced);

            new Labels(Labels.InitSettings.Default.Parent(LeftContent).Name("annotation_label"), "Annotation");

            var toolsParent = new LayoutGroupElement(HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f).ChildControlWidth(false).ChildForceExpandWidth(false),
                new SpacerElement()
                {
                    size = new Vector2(16f, 32f),
                },
                new ButtonElement(ButtonElement.Type.Icon, "Copy", RTMarkerEditor.inst.CopyAnnotations, tooltipGroup: "Copy Annotations")
                {
                    buttonThemeGroup = ThemeGroup.Copy,
                    graphicThemeGroup = ThemeGroup.Copy_Text,
                    sprite = SpriteHelper.LoadSprite(AssetPack.GetFile("core/sprites/icons/operations/copy.png")),
                    sizeDelta = new Vector2(32f, 32f),
                },
                new ButtonElement(ButtonElement.Type.Icon, "Paste", RTMarkerEditor.inst.PasteAnnotations, tooltipGroup: "Paste Annotations")
                {
                    buttonThemeGroup = ThemeGroup.Paste,
                    graphicThemeGroup = ThemeGroup.Paste_Text,
                    sprite = SpriteHelper.LoadSprite(AssetPack.GetFile("core/sprites/icons/operations/paste.png")),
                    sizeDelta = new Vector2(32f, 32f),
                },
                new ButtonElement(ButtonElement.Type.Icon, "Delete", RTMarkerEditor.inst.ClearMarkerAnnotations, tooltipGroup: "Clear Annotations")
                {
                    buttonThemeGroup = ThemeGroup.Delete,
                    graphicThemeGroup = ThemeGroup.Delete_Text,
                    sprite = SpriteHelper.LoadSprite(AssetPack.GetFile("core/sprites/icons/operations/close.png")),
                    sizeDelta = new Vector2(32f, 32f),
                });
            toolsParent.Init(EditorElement.InitSettings.Default.Parent(LeftContent));
            var tools = EnumHelper.GetValues<AnnotationTool>();
            for (int i = 0; i < tools.Length; i++)
            {
                var tool = tools[i];
                var gameObject = EditorManager.inst.colorGUI.Duplicate(toolsParent.GameObject.transform, tool.ToString(), i);
                gameObject.transform.AsRT().sizeDelta = new Vector2(32f, 32f);
                var toolButton = gameObject.GetComponent<Button>();
                var icon = Creator.NewUIObject("icon", gameObject.transform, 0);
                RectValues.FullAnchored.AssignToRectTransform(icon.transform.AsRT());
                var iconImage = icon.AddComponent<Image>();
                iconImage.sprite = SpriteHelper.LoadSprite(AssetPack.GetFile(tool switch
                {
                    AnnotationTool.Draw => "core/sprites/icons/operations/edit.png",
                    AnnotationTool.Erase => "core/sprites/icons/operations/delete_sweep.png",
                    AnnotationTool.Delete => "core/sprites/icons/operations/delete.png",
                    AnnotationTool.Bucket => "core/sprites/icons/operations/bucket.png",
                    AnnotationTool.Move => "core/sprites/icons/empty.png",
                    _ => "core/sprites/icons/operations/close.png",
                }));

                EditorThemeManager.ApplyGraphic(gameObject.GetComponent<Image>(), ThemeGroup.Function_2_Normal);
                EditorThemeManager.ApplyGraphic(iconImage, ThemeGroup.Function_2_Text);
                EditorThemeManager.ApplyGraphic(gameObject.transform.GetChild(1).GetComponent<Image>(), ThemeGroup.Background_1);

                if (tool != AnnotationTool.None)
                    TooltipHelper.AssignTooltip(gameObject, tool.ToString() + " Annotation");

                AnnotationToolButtons.Add(toolButton);
            }

            new Labels(Labels.InitSettings.Default.Parent(LeftContent).Name("annotation color_label"), "Annotation Color");
            AnnotationColorsParent = ColorsParent.gameObject.Duplicate(LeftContent, "annotation colors").transform.AsRT();

            new Labels(Labels.InitSettings.Default.Parent(LeftContent).Name("annotation hex color_label"), "Annotation Hex Color");
            var annotationHexColor = NameField.gameObject.Duplicate(LeftContent, "annotation hex color");
            AnnotationHexColorField = annotationHexColor.GetComponent<InputField>();
            EditorThemeManager.ApplyInputField(AnnotationHexColorField);

            new Labels(Labels.InitSettings.Default.Parent(LeftContent).Name("annotation opacity_label"), "Annotation Opacity");
            var annotationOpacity = EditorPrefabHolder.Instance.NumberInputField.Duplicate(LeftContent, "annotation opacity");
            AnnotationOpacityField = annotationOpacity.GetComponent<InputFieldStorage>();
            CoreHelper.Delete(AnnotationOpacityField.middleButton);
            EditorThemeManager.ApplyInputField(AnnotationOpacityField);

            new Labels(Labels.InitSettings.Default.Parent(LeftContent).Name("annotation thickness_label"), "Annotation Thickness");
            var annotationThickness = EditorPrefabHolder.Instance.NumberInputField.Duplicate(LeftContent, "annotation thickness");
            AnnotationThicknessField = annotationThickness.GetComponent<InputFieldStorage>();
            CoreHelper.Delete(AnnotationThicknessField.middleButton);
            EditorThemeManager.ApplyInputField(AnnotationThicknessField);

            new Labels(Labels.InitSettings.Default.Parent(LeftContent).Name("annotation fixed camera_label"), "Annotation Fixed Camera");
            var annotationFixedCamera = EditorPrefabHolder.Instance.ToggleButton.Duplicate(LeftContent, "annotation fixed camera");
            AnnotationFixedCameraToggle = annotationFixedCamera.GetComponent<ToggleButtonStorage>();
            AnnotationFixedCameraToggle.Text = "Fixed";
            EditorThemeManager.ApplyToggle(AnnotationFixedCameraToggle);

            TooltipHelper.AssignTooltip(annotationFixedCamera, "Annotation Fixed Camera");
        }
    }
}
