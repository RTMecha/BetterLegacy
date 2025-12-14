using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Popups
{
    public class ProgressPopup : EditorPopup
    {
        public ProgressPopup() { }
        public ProgressPopup(string name) : base(name) { }

        public Text ProgressText { get; set; }
        public RectTransform ProgressBar { get; set; }

        public string Text
        {
            get => ProgressText.text;
            set => ProgressText.text = value;
        }

        public override void Init()
        {
            GameObject = Creator.NewUIObject("ProgressPopup", RTEditor.inst.popups);
            RectValues.Default.SizeDelta(500f, 300f).AssignToRectTransform(GameObject.transform.AsRT());
            var baseImage = GameObject.AddComponent<Image>();

            EditorThemeManager.ApplyGraphic(baseImage, ThemeGroup.Background_1, true);

            Dragger = GameObject.AddComponent<DraggableUI>();
            Dragger.target = GameObject.transform;
            Dragger.ogPos = GameObject.transform.position;

            var progressBase = Creator.NewUIObject("Progress Base", GameObject.transform);
            RectValues.Default.AnchoredPosition(0f, -100f).SizeDelta(300f, 32f).AssignToRectTransform(progressBase.transform.AsRT());
            var progressBaseImage = progressBase.AddComponent<Image>();
            progressBase.AddComponent<Mask>();

            EditorThemeManager.ApplyGraphic(progressBaseImage, ThemeGroup.Background_2, true);

            var progress = Creator.NewUIObject("Progress", progressBase.transform);
            RectValues.LeftAnchored.SizeDelta(0f, 32f).AssignToRectTransform(progress.transform.AsRT());
            var progressImage = progress.AddComponent<Image>();

            EditorThemeManager.ApplyGraphic(progressImage, ThemeGroup.Light_Text, true);

            ProgressBar = progress.transform.AsRT();
            ProgressText = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(GameObject.transform).GetComponent<Text>();
            RectValues.Default.AnchoredPosition(0f, 60f).SizeDelta(400f, 100f).AssignToRectTransform(ProgressText.rectTransform);
            ProgressText.alignment = TextAnchor.UpperLeft;
            EditorThemeManager.ApplyLightText(ProgressText);

            var close = EditorPrefabHolder.Instance.CloseButton.Duplicate(GameObject.transform);
            RectValues.TopRightAnchored.SizeDelta(32f, 32f).AssignToRectTransform(close.transform.AsRT());

            var closeButton = close.GetComponent<Button>();
            closeButton.onClick.NewListener(Close);

            EditorThemeManager.ApplySelectable(closeButton, ThemeGroup.Close);

            var closeX = close.transform.GetChild(0).gameObject;
            EditorThemeManager.ApplyGraphic(close.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

            GameObject.SetActive(false);
        }

        public void UpdateProgress(float value)
        {
            ProgressBar.sizeDelta = new Vector2(RTMath.Clamp(value * 600f, 0f, 300f), 32f);
        }

        public override void Render()
        {

        }
    }
}
