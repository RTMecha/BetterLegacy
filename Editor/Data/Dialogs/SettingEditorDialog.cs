using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class SettingEditorDialog : EditorDialog
    {
        public SettingEditorDialog() : base(SETTINGS_EDITOR) { }

        #region Properties

        public Transform Content { get; set; }

        #region Snap Settings

        public Slider BPMSlider { get; set; }
        public InputField BPMInput { get; set; }
        public Toggle BPMToggle { get; set; }
        
        public Slider BPMOffsetSlider { get; set; }
        public InputField BPMOffsetInput { get; set; }
        
        public Slider BPMTimingSlider { get; set; }
        public InputField BPMTimingInput { get; set; }

        public FunctionButtonStorage AnalyzeBPMButton { get; set; }

        #endregion

        #region Info

        public GameObject InfoScrollView { get; set; }
        public Transform InfoContent { get; set; }

        #endregion

        #region Colors

        public Transform MarkerColorsContent { get; set; }
        public Transform LayerColorsContent { get; set; }

        #endregion

        public Image Doggo { get; set; }

        #endregion

        #region Fields

        public List<Info> infos = new List<Info>()
        {
            new Info("FPS", () => LegacyPlugin.FPSCounter.Text),
            new Info("Time in Editor", () => RTString.SecondsToTime(RTEditor.inst.editorInfo.timer.time)),
            new Info("Song Progress", () => $"{RTString.Percentage(AudioManager.inst.CurrentAudioSource.time, AudioManager.inst.CurrentAudioSource.clip.length)}%"),
            new Info("Level opened amount", () => RTEditor.inst.editorInfo.openAmount.ToString()),

            new Info("Object Count", () => GameData.Current?.beatmapObjects.FindAll(x => !x.fromPrefab).Count.ToString() ?? "null"),
            new Info("Total Object Count", () => GameData.Current?.beatmapObjects.Count.ToString() ?? "null"),
            new Info("Objects Alive Count", () => GameData.Current?.beatmapObjects.FindAll(x => x.Alive).Count.ToString() ?? "null"),
            new Info("No Autokill Count", () => GameData.Current?.beatmapObjects.FindAll(x => x.autoKillType == AutoKillType.NoAutokill).Count.ToString() ?? "null"),
            new Info("Keyframe Offsets > Song Length Count", () => GameData.Current?.beatmapObjects.FindAll(x => x.autoKillOffset > AudioManager.inst.CurrentAudioSource.clip.length).Count.ToString() ?? "null"),
            new Info("Text Object Count", () => GameData.Current?.beatmapObjects.FindAll(x => x.shape == 4 && x.objectType != BeatmapObject.ObjectType.Empty).Count.ToString() ?? "null"),
            new Info("Text Symbol Total Count", () => GameData.Current?.beatmapObjects.Where(x => x.shape == 4 && x.objectType != BeatmapObject.ObjectType.Empty).Sum(x => x.text.Length).ToString() ?? "null"),

            new Info("BG Object Count", () => GameData.Current?.backgroundObjects.Count.ToString() ?? "null"),

            new Info("Camera Position", () => $"X: {Camera.main.transform.position.x}, Y: {Camera.main.transform.position.y}"),
            new Info("Camera Zoom", () => Camera.main.orthographicSize.ToString()),
            new Info("Camera Rotation", () => Camera.main.transform.rotation.eulerAngles.z.ToString()),
            new Info("Event Count", () => GameData.Current && !GameData.Current.events.IsEmpty() ? GameData.Current.events.Sum(x => x.Count).ToString() : "null"),
            new Info("Theme Count", () => ThemeManager.inst.ThemeCount.ToString()),

            new Info("Prefab External Count", () => RTPrefabEditor.inst.PrefabPanels.Count.ToString()),
            new Info("Prefab Internal Count", () => GameData.Current?.prefabs.Count.ToString() ?? "null"),
            new Info("Prefab Objects Count", () => GameData.Current?.prefabObjects.Count.ToString() ?? "null"),

            new Info("Timeline Bin Count", () => EditorTimeline.inst.BinCount.ToString()),
            new Info("Timeline Objects in Current Layer Count", () => EditorTimeline.inst.timelineObjects.FindAll(x => x.IsCurrentLayer).Count.ToString()),
            new Info("Markers Count", () => GameData.Current?.data.markers.Count.ToString() ?? "null"),
        };

        #endregion

        #region Methods

        public override void Init()
        {
            base.Init();

            EditorThemeManager.AddGraphic(GameObject.GetComponent<Image>(), ThemeGroup.Background_1);

            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(GameObject.transform, "Scroll View");
            scrollView.transform.AsRT().sizeDelta = new Vector2(765f, 600f);
            Content = scrollView.transform.Find("Viewport/Content");
            EditorThemeManager.AddScrollbar(scrollView.transform.Find("Scrollbar Vertical").GetComponent<Scrollbar>());

            var snap = GameObject.transform.Find("snap");

            snap.SetParent(Content);

            BPMSlider = snap.Find("bpm/slider").GetComponent<Slider>();
            BPMSlider.maxValue = 999f;
            BPMSlider.minValue = 1f;

            BPMToggle = snap.Find("toggle/toggle").GetComponent<Toggle>();

            CoreHelper.Destroy(snap.Find("bpm/<").gameObject, true);
            CoreHelper.Destroy(snap.Find("bpm/>").gameObject, true);
            EditorThemeManager.AddToggle(BPMToggle);
            EditorThemeManager.AddLightText(snap.Find("toggle/title").GetComponent<Text>());
            EditorThemeManager.AddLightText(snap.Find("bpm/title").GetComponent<Text>());
            snap.Find("toggle/title").AsRT().sizeDelta = new Vector2(100f, 32f);
            snap.Find("bpm/title").AsRT().sizeDelta = new Vector2(100f, 32f);

            EditorThemeManager.AddGraphic(snap.transform.Find("title_/Panel/icon").GetComponent<Image>(), ThemeGroup.Light_Text);
            EditorThemeManager.AddLightText(snap.transform.Find("title_/title").GetComponent<Text>());

            EditorThemeManager.AddGraphic(BPMSlider.transform.Find("Background").GetComponent<Image>(), ThemeGroup.Slider_2, true);
            EditorThemeManager.AddGraphic(BPMSlider.image, ThemeGroup.Slider_2_Handle, true);

            BPMInput = snap.Find("bpm/input").GetComponent<InputField>();
            EditorThemeManager.AddInputField(BPMInput);

            var snapOffset = snap.Find("bpm").gameObject.Duplicate(snap, "bpm offset");
            var snapOffsetText = snapOffset.transform.Find("title").GetComponent<Text>();
            snapOffsetText.text = "BPM Offset";
            snapOffsetText.rectTransform.sizeDelta = new Vector2(100f, 32f);
            EditorThemeManager.AddLightText(snapOffsetText);

            BPMOffsetSlider = snapOffset.transform.Find("slider").GetComponent<Slider>();
            EditorThemeManager.AddGraphic(BPMOffsetSlider.transform.Find("Background").GetComponent<Image>(), ThemeGroup.Slider_2, true);
            EditorThemeManager.AddGraphic(BPMOffsetSlider.image, ThemeGroup.Slider_2_Handle, true);

            BPMOffsetInput = snapOffset.transform.Find("input").GetComponent<InputField>();
            EditorThemeManager.AddInputField(BPMOffsetInput);

            var snapSignature = snap.Find("bpm").gameObject.Duplicate(snap, "bpm signature");
            var snapSignatureText = snapSignature.transform.Find("title").GetComponent<Text>();
            snapSignatureText.text = "Signature";
            snapSignatureText.rectTransform.sizeDelta = new Vector2(100f, 32f);
            EditorThemeManager.AddLightText(snapSignatureText);

            BPMTimingSlider = snapSignature.transform.Find("slider").GetComponent<Slider>();
            EditorThemeManager.AddGraphic(BPMTimingSlider.transform.Find("Background").GetComponent<Image>(), ThemeGroup.Slider_2, true);
            EditorThemeManager.AddGraphic(BPMTimingSlider.image, ThemeGroup.Slider_2_Handle, true);

            BPMTimingSlider.wholeNumbers = true;

            BPMTimingInput = snapSignature.transform.Find("input").GetComponent<InputField>();
            EditorThemeManager.AddInputField(BPMTimingInput);

            snap.AsRT().sizeDelta = new Vector2(765f, 180f);

            var title1 = snap.GetChild(0).gameObject.Duplicate(Content, "info title");
            var editorInformationText = title1.transform.Find("title").GetComponent<Text>();
            editorInformationText.text = "Editor Information";

            EditorThemeManager.AddGraphic(title1.transform.Find("Panel/icon").GetComponent<Image>(), ThemeGroup.Light_Text);
            EditorThemeManager.AddLightText(editorInformationText);

            InfoScrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(Content, "Scroll View");
            InfoScrollView.transform.AsRT().sizeDelta = new Vector2(765f, 120f);
            InfoContent = InfoScrollView.transform.Find("Viewport/Content");

            for (int i = 0; i < infos.Count; i++)
            {
                var info = infos[i];
                var baseInfo = Creator.NewUIObject("Info", InfoContent);
                baseInfo.transform.AsRT().sizeDelta = new Vector2(750f, 32f);
                var iImage = baseInfo.AddComponent<Image>();

                iImage.color = new Color(1f, 1f, 1f, 0.12f);

                EditorThemeManager.AddGraphic(iImage, ThemeGroup.List_Button_1_Normal, true);

                baseInfo.AddComponent<HorizontalLayoutGroup>();

                var title = Creator.NewUIObject("Title", baseInfo.transform);

                var titleText = title.AddComponent<Text>();
                titleText.font = FontManager.inst.DefaultFont;
                titleText.fontSize = 19;
                titleText.alignment = TextAnchor.MiddleLeft;
                titleText.text = "  " + info.label;

                EditorThemeManager.AddLightText(titleText);

                var infoGO = Creator.NewUIObject("Title", baseInfo.transform);

                var infoText = infoGO.AddComponent<Text>();
                infoText.font = FontManager.inst.DefaultFont;
                infoText.fontSize = 19;
                infoText.alignment = TextAnchor.MiddleRight;
                infoText.text = "[ 0 ]";

                EditorThemeManager.AddLightText(infoText);

                info.text = infoText;
            }

            // Doggo
            var loadingDoggo = Creator.NewUIObject("loading doggo", GameObject.transform);
            Doggo = loadingDoggo.AddComponent<Image>();
            var loadingDoggoLE = loadingDoggo.AddComponent<LayoutElement>();

            loadingDoggo.transform.AsRT().anchoredPosition = new Vector2(UnityEngine.Random.Range(-320f, 320f), UnityEngine.Random.Range(-300f, -275f));
            float sizeRandom = 64f * UnityEngine.Random.Range(0.5f, 1f);
            loadingDoggo.transform.AsRT().sizeDelta = new Vector2(sizeRandom, sizeRandom);

            loadingDoggoLE.ignoreLayout = true;

            var title2 = snap.GetChild(0).gameObject.Duplicate(Content, "marker colors title");
            var markerColorsText = title2.transform.Find("title").GetComponent<Text>();
            markerColorsText.text = "Marker Colors / Editor Layer Colors";

            EditorThemeManager.AddGraphic(title2.transform.Find("Panel/icon").GetComponent<Image>(), ThemeGroup.Light_Text);

            EditorThemeManager.AddLightText(markerColorsText);

            var colorEditors = Creator.NewUIObject("Color Editors", Content);
            var colorEditorsLayout = colorEditors.AddComponent<HorizontalLayoutGroup>();
            colorEditors.transform.AsRT().sizeDelta = new Vector2(765f, 240f);
            colorEditorsLayout.spacing = 16f;

            // Marker Colors
            var markerColorsScrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(colorEditors.transform, "Scroll View");
            MarkerColorsContent = markerColorsScrollView.transform.Find("Viewport/Content");
            EditorThemeManager.AddScrollbar(markerColorsScrollView.transform.Find("Scrollbar Vertical").GetComponent<Scrollbar>());

            // Layer Colors
            var layerColorsScrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(colorEditors.transform, "Scroll View");
            LayerColorsContent = layerColorsScrollView.transform.Find("Viewport/Content");
            EditorThemeManager.AddScrollbar(layerColorsScrollView.transform.Find("Scrollbar Vertical").GetComponent<Scrollbar>());

            var analyzeBPM = EditorPrefabHolder.Instance.Function2Button.Duplicate(snap.Find("toggle"), "analyze");
            analyzeBPM.transform.AsRT().sizeDelta = new Vector2(140f, 32f);

            AnalyzeBPMButton = analyzeBPM.GetComponent<FunctionButtonStorage>();
            AnalyzeBPMButton.Text = "Analyze BPM";
            AnalyzeBPMButton.OnClick.NewListener(() =>
            {
                CoroutineHelper.StartCoroutineAsync(UniBpmAnalyzer.IAnalyzeBPM(AudioManager.inst.CurrentAudioSource.clip, bpm =>
                {
                    EditorManager.inst.DisplayNotification($"Detected a BPM of {bpm}! Applied it to editor settings.", 2f, EditorManager.NotificationType.Success);

                    BPMInput.text = bpm.ToString();
                    RTEditor.inst.editorInfo.analyzedBPM = true;
                }));
            });

            EditorThemeManager.AddSelectable(AnalyzeBPMButton.button, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(AnalyzeBPMButton.label, ThemeGroup.Function_2_Text);
        }

        #endregion

        public class Info
        {
            public Info(string label, Func<string> func)
            {
                this.label = label;
                this.func = func;
            }

            public string label;

            public Text text;

            public Func<string> func;

            public void Render()
            {
                if (text)
                    text.text = func?.Invoke();
            }
        }
    }
}
