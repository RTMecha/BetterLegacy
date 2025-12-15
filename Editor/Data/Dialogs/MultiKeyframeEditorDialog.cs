using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class MultiKeyframeEditorDialog : EditorDialog
    {
        public MultiKeyframeEditorDialog() : base(MULTI_KEYFRAME_EDITOR) { }

        public override void Init()
        {
            if (init)
                return;

            base.Init();

            var multiKeyframeEditor = GameObject.transform;

            multiKeyframeEditor.Find("spacer").gameObject.SetActive(false);
            multiKeyframeEditor.Find("Text").gameObject.SetActive(false);

            EditorThemeManager.ApplyGraphic(multiKeyframeEditor.GetComponent<Image>(), ThemeGroup.Background_1);

            var multiKeyframeEditorVLG = multiKeyframeEditor.GetComponent<VerticalLayoutGroup>();
            multiKeyframeEditorVLG.childControlWidth = false;
            multiKeyframeEditorVLG.childForceExpandWidth = false;

            var data = Creator.NewUIObject("data", multiKeyframeEditor);
            data.transform.AsRT().sizeDelta = new Vector2(740f, 100f);

            var dataVLG = data.AddComponent<VerticalLayoutGroup>();
            dataVLG.childControlHeight = false;
            dataVLG.childControlWidth = true;
            dataVLG.childForceExpandHeight = false;
            dataVLG.childForceExpandWidth = true;
            dataVLG.spacing = 4f;
            dataVLG.padding = new RectOffset(left: 8, right: 8, top: 8, bottom: 8);

            new LabelsElement("Time").Init(EditorElement.InitSettings.Default.Parent(data.transform));

            var time = EditorPrefabHolder.Instance.NumberInputField.Duplicate(data.transform, "time");
            time.transform.AsRT().anchoredPosition = new Vector2(8f, 32f);
            var timeStorage = time.GetComponent<InputFieldStorage>();
            timeStorage.OnValueChanged.ClearAll();
            if (timeStorage.Text == "100.000")
                timeStorage.SetTextWithoutNotify("10");

            EditorThemeManager.ApplyInputField(timeStorage);

            new LabelsElement("Ease Type").Init(EditorElement.InitSettings.Default.Parent(data.transform));

            var curves = EditorPrefabHolder.Instance.CurvesDropdown.Duplicate(data.transform, "curves");
            curves.transform.AsRT().anchoredPosition = new Vector2(191f, 0f);

            EditorThemeManager.ApplyDropdown(curves.GetComponent<Dropdown>());

            new LabelsElement("Value Index").Init(EditorElement.InitSettings.Default.Parent(data.transform));

            var valueIndex = EditorPrefabHolder.Instance.NumberInputField.Duplicate(data.transform, "value index");
            valueIndex.transform.AsRT().anchoredPosition = new Vector2(8f, 32f);
            var valueIndexStorage = valueIndex.GetComponent<InputFieldStorage>();
            valueIndexStorage.OnValueChanged.ClearAll();
            if (valueIndexStorage.Text == "100.000")
                valueIndexStorage.SetTextWithoutNotify("0");

            CoreHelper.Delete(valueIndexStorage.leftGreaterButton);
            CoreHelper.Delete(valueIndexStorage.middleButton);
            CoreHelper.Delete(valueIndexStorage.rightGreaterButton);

            EditorThemeManager.ApplyInputField(valueIndexStorage);

            new LabelsElement("Value").Init(EditorElement.InitSettings.Default.Parent(data.transform));

            var value = EditorPrefabHolder.Instance.NumberInputField.Duplicate(data.transform, "value");
            value.transform.AsRT().anchoredPosition = new Vector2(8f, 32f);
            var valueStorage = value.GetComponent<InputFieldStorage>();
            valueStorage.OnValueChanged.ClearAll();
            if (valueStorage.Text == "100.000")
                valueStorage.SetTextWithoutNotify("1.0");

            EditorThemeManager.ApplyInputField(valueStorage);

            new LabelsElement("Force Snap Time to BPM").Init(EditorElement.InitSettings.Default.Parent(data.transform));

            var snap = EditorPrefabHolder.Instance.Function1Button.Duplicate(data.transform, "snap bpm");
            snap.transform.localScale = Vector3.one;
            var snapStorage = snap.GetComponent<FunctionButtonStorage>();

            UIManager.SetRectTransform(snap.transform.AsRT(), new Vector2(8f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(404f, 32f));

            snapStorage.Text = "Snap";
            snapStorage.OnClick.NewListener(() =>
            {
                foreach (var kf in RTEventEditor.inst.SelectedKeyframes)
                {
                    if (kf.Index != 0)
                        kf.Time = RTEditor.SnapToBPM(kf.Time);
                    kf.RenderPos();
                }

                RTEventEditor.inst.RenderEventsDialog();
                RTLevel.Current?.UpdateEvents();
                EditorManager.inst.DisplayNotification($"Snapped all keyframes time!", 2f, EditorManager.NotificationType.Success);
            });

            EditorThemeManager.ApplyGraphic(snapStorage.button.image, ThemeGroup.Function_1, true);
            EditorThemeManager.ApplyGraphic(snapStorage.label, ThemeGroup.Function_1_Text);

            new LabelsElement("Align to First Selected").Init(EditorElement.InitSettings.Default.Parent(data.transform));

            var alignToFirstObject = EditorPrefabHolder.Instance.Function1Button.Duplicate(data.transform, "align");
            alignToFirstObject.transform.localScale = Vector3.one;
            var alignToFirstStorage = alignToFirstObject.GetComponent<FunctionButtonStorage>();

            UIManager.SetRectTransform(alignToFirstObject.transform.AsRT(), new Vector2(8f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(404f, 32f));

            alignToFirstStorage.Text = "Align";
            alignToFirstStorage.OnClick.NewListener(() =>
            {
                var list = RTEventEditor.inst.SelectedKeyframes.OrderBy(x => x.Time);
                var first = list.ElementAt(0);

                foreach (var kf in list)
                {
                    if (kf.Index != 0)
                        kf.Time = first.Time;
                    kf.RenderPos();
                }

                RTEventEditor.inst.RenderEventsDialog();
                RTLevel.Current?.UpdateEvents();
                EditorManager.inst.DisplayNotification($"Aligned all keyframes to the first keyframe!", 2f, EditorManager.NotificationType.Success);
            });

            EditorThemeManager.ApplyGraphic(alignToFirstStorage.button.image, ThemeGroup.Function_1, true);
            EditorThemeManager.ApplyGraphic(alignToFirstStorage.label, ThemeGroup.Function_1_Text);

            new LabelsElement("Paste All Keyframe Data").Init(EditorElement.InitSettings.Default.Parent(data.transform));

            var pasteAllObject = EditorPrefabHolder.Instance.Function1Button.Duplicate(data.transform, "paste");
            pasteAllObject.transform.localScale = Vector3.one;
            var pasteAllStorage = pasteAllObject.GetComponent<FunctionButtonStorage>();

            UIManager.SetRectTransform(pasteAllObject.transform.AsRT(), new Vector2(8f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(404f, 32f));

            pasteAllStorage.Text = "Paste";
            pasteAllStorage.OnClick.NewListener(() =>
            {
                foreach (var keyframe in RTEventEditor.inst.SelectedKeyframes)
                {
                    if (!(RTEventEditor.inst.copiedKeyframeDatas.Count > keyframe.Type) || RTEventEditor.inst.copiedKeyframeDatas[keyframe.Type] == null)
                        continue;

                    var kf = keyframe.eventKeyframe;
                    kf.curve = RTEventEditor.inst.copiedKeyframeDatas[keyframe.Type].curve;
                    kf.values = RTEventEditor.inst.copiedKeyframeDatas[keyframe.Type].values.Copy();
                    kf.randomValues = RTEventEditor.inst.copiedKeyframeDatas[keyframe.Type].randomValues.Copy();
                    kf.random = RTEventEditor.inst.copiedKeyframeDatas[keyframe.Type].random;
                    kf.relative = RTEventEditor.inst.copiedKeyframeDatas[keyframe.Type].relative;
                    keyframe.Render();
                }

                RTEventEditor.inst.RenderEventsDialog();
                RTLevel.Current?.UpdateEvents();
                EditorManager.inst.DisplayNotification($"Pasted all keyframe data to current selected keyframes!", 2f, EditorManager.NotificationType.Success);
            });

            EditorThemeManager.ApplyGraphic(pasteAllStorage.button.image, ThemeGroup.Function_1, true);
            EditorThemeManager.ApplyGraphic(pasteAllStorage.label, ThemeGroup.Function_1_Text);
        }
    }
}
