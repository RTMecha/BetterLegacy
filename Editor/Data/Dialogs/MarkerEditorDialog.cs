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

        public Text IndexText { get; set; }

        public InputField NameField { get; set; }

        public InputFieldStorage TimeField { get; set; }

        public InputField DescriptionField { get; set; }

        public RectTransform ColorsParent { get; set; }

        public List<GameObject> Colors { get; set; } = new List<GameObject>();

        public RectTransform LayersContent { get; set; }

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

            var indexparent = Creator.NewUIObject("index", MarkerEditor.inst.left, 0);
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

            EditorThemeManager.AddLightText(IndexText);

            EditorHelper.SetComplexity(indexparent, Complexity.Normal);

            // Makes label consistent with other labels. Originally said "Marker Time" where other labels do not mention "Marker".
            var timeLabel = MarkerEditor.inst.left.GetChild(3).GetChild(0).GetComponent<Text>();
            timeLabel.text = "Time";
            // Fixes "Name" label.
            var descriptionLabel = MarkerEditor.inst.left.GetChild(5).GetChild(0).GetComponent<Text>();
            descriptionLabel.text = "Description";

            EditorThemeManager.AddGraphic(dialog.GetComponent<Image>(), ThemeGroup.Background_1);

            EditorThemeManager.AddInputField(MarkerEditor.inst.right.Find("InputField").GetComponent<InputField>(), ThemeGroup.Search_Field_2);

            var scrollbar = MarkerEditor.inst.right.transform.Find("Scrollbar").GetComponent<Scrollbar>();
            EditorThemeManager.ApplyGraphic(scrollbar.GetComponent<Image>(), ThemeGroup.Scrollbar_2, true);
            EditorThemeManager.ApplyGraphic(scrollbar.image, ThemeGroup.Scrollbar_2_Handle, true);

            EditorThemeManager.AddLightText(MarkerEditor.inst.left.GetChild(1).GetChild(0).GetComponent<Text>());
            EditorThemeManager.AddLightText(timeLabel);
            EditorThemeManager.AddLightText(descriptionLabel);

            NameField = MarkerEditor.inst.left.Find("name").GetComponent<InputField>();
            DescriptionField = MarkerEditor.inst.left.Find("desc").GetComponent<InputField>();
            //DescriptionField.transform.AsRT().sizeDelta = new Vector2(371f, 192f);
            DescriptionField.transform.AsRT().sizeDelta = new Vector2(371f, 256f);

            EditorThemeManager.AddInputField(NameField);
            EditorThemeManager.AddInputField(DescriptionField);

            var time = EditorPrefabHolder.Instance.NumberInputField.Duplicate(MarkerEditor.inst.left, "time new", 4);
            CoreHelper.Delete(MarkerEditor.inst.left.Find("time").gameObject);

            TimeField = time.GetComponent<InputFieldStorage>();
            EditorThemeManager.AddInputField(TimeField);

            time.name = "time";

            // fixes color slot spacing
            MarkerEditor.inst.left.Find("color").GetComponent<GridLayoutGroup>().spacing = new Vector2(8f, 8f);

            ColorsParent = MarkerEditor.inst.left.Find("color").AsRT();

            new Labels(Labels.InitSettings.Default.Parent(MarkerEditor.inst.left).Name("layers_label"), "Layers to appear on");

            var tagScrollView = Creator.NewUIObject("Layers Scroll View", MarkerEditor.inst.left);

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
            buttonStorage.image.sprite = EditorSprites.EditSprite;
            EditorThemeManager.ApplySelectable(buttonStorage.button, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(buttonStorage.image, ThemeGroup.Function_2_Text);
            buttonStorage.button.onClick.NewListener(() => RTTextEditor.inst.SetInputField(DescriptionField));
            RectValues.Default.AnchoredPosition(171f, 112f).SizeDelta(22f, 22f).AssignToRectTransform(buttonStorage.baseImage.rectTransform);
            EditorHelper.SetComplexity(button, "marker/layers", Complexity.Advanced);

            var prefab = MarkerEditor.inst.markerPrefab;
            var prefabCopy = prefab.Duplicate(RTMarkerEditor.inst.transform, prefab.name);
            var markerStorage = prefabCopy.AddComponent<MarkerStorage>();
            CoreHelper.Destroy(prefabCopy.GetComponent<MarkerHelper>());
            var flagStart = Creator.NewUIObject("flag start", prefabCopy.transform, 0);
            markerStorage.flagStart = flagStart.AddComponent<Image>();
            markerStorage.flagStart.sprite = EditorSprites.FlagStartSprite;
            RectValues.Default.AnchoredPosition(36f, 0f).SizeDelta(60f, 60f).AssignToRectTransform(markerStorage.flagStart.rectTransform);
            flagStart.SetActive(false);
            var flagEnd = Creator.NewUIObject("flag end", prefabCopy.transform, 1);
            markerStorage.flagEnd = flagEnd.AddComponent<Image>();
            markerStorage.flagEnd.sprite = EditorSprites.FlagEndSprite;
            RectValues.Default.AnchoredPosition(-36f, 0f).SizeDelta(60f, 60f).AssignToRectTransform(markerStorage.flagEnd.rectTransform);
            flagEnd.SetActive(false);
            markerStorage.handle = prefabCopy.GetComponent<Image>();
            markerStorage.line = prefabCopy.transform.Find("line").GetComponent<Image>();
            markerStorage.label = prefabCopy.transform.Find("Text").GetComponent<Text>();
            markerStorage.hoverTooltip = prefabCopy.GetComponent<HoverTooltip>();
            MarkerEditor.inst.markerPrefab = prefabCopy;
        }
    }
}
