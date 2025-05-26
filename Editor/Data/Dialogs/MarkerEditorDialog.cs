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

        public FunctionButtonStorage SnapBPMButton { get; set; }

        public InputField DescriptionField { get; set; }

        public FunctionButtonStorage ConvertToPlannerNoteButton { get; set; }

        public RectTransform ColorsParent { get; set; }

        public List<GameObject> Colors { get; set; } = new List<GameObject>();

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

            EditorThemeManager.AddInputField(NameField);
            EditorThemeManager.AddInputField(DescriptionField);

            var time = EditorPrefabHolder.Instance.NumberInputField.Duplicate(MarkerEditor.inst.left, "time new", 4);
            CoreHelper.Delete(MarkerEditor.inst.left.Find("time").gameObject);

            TimeField = time.GetComponent<InputFieldStorage>();
            EditorThemeManager.AddInputField(TimeField);

            time.name = "time";

            // fixes color slot spacing
            MarkerEditor.inst.left.Find("color").GetComponent<GridLayoutGroup>().spacing = new Vector2(8f, 8f);

            if (!EditorPrefabHolder.Instance.Function2Button)
            {
                CoreHelper.LogError("No Function 2 button for some reason.");
                return;
            }

            var makeNote = EditorPrefabHolder.Instance.Function2Button.Duplicate(MarkerEditor.inst.left, "convert to note", 8);
            ConvertToPlannerNoteButton = makeNote.GetComponent<FunctionButtonStorage>();
            ConvertToPlannerNoteButton.label.text = "Convert to Planner Note";
            ConvertToPlannerNoteButton.button.onClick.ClearAll();

            EditorThemeManager.AddSelectable(ConvertToPlannerNoteButton.button, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(ConvertToPlannerNoteButton.label, ThemeGroup.Function_2_Text);

            EditorHelper.SetComplexity(makeNote, Complexity.Advanced);

            var snapToBPM = EditorPrefabHolder.Instance.Function2Button.Duplicate(MarkerEditor.inst.left, "snap bpm", 5);
            SnapBPMButton = snapToBPM.GetComponent<FunctionButtonStorage>();
            SnapBPMButton.label.text = "Snap BPM";
            SnapBPMButton.button.onClick.ClearAll();

            EditorThemeManager.AddSelectable(SnapBPMButton.button, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(SnapBPMButton.label, ThemeGroup.Function_2_Text);

            EditorHelper.SetComplexity(snapToBPM, Complexity.Normal);

            ColorsParent = MarkerEditor.inst.left.Find("color").AsRT();

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

            var button = EditorPrefabHolder.Instance.DeleteButton.Duplicate(DescriptionField.transform, "edit");
            var buttonStorage = button.GetComponent<DeleteButtonStorage>();
            buttonStorage.image.sprite = EditorSprites.EditSprite;
            EditorThemeManager.ApplySelectable(buttonStorage.button, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(buttonStorage.image, ThemeGroup.Function_2_Text);
            buttonStorage.button.onClick.NewListener(() => RTTextEditor.inst.SetInputField(DescriptionField));
            UIManager.SetRectTransform(buttonStorage.baseImage.rectTransform, new Vector2(171f, 51f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(22f, 22f));
            EditorHelper.SetComplexity(button, Complexity.Advanced);
        }
    }
}
