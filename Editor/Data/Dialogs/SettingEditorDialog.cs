using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
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

        #region Snap Settings

        public Slider BPMSlider { get; set; }
        public InputField BPMInput { get; set; }
        public Toggle BPMToggle { get; set; }
        
        public Slider BPMOffsetSlider { get; set; }
        public InputField BPMOffsetInput { get; set; }

        public FunctionButtonStorage AnalyzeBPMButton { get; set; }

        #endregion

        public Image Doggo { get; set; }

        #region Colors

        public Transform MarkerColorsContent { get; set; }
        public Transform LayerColorsContent { get; set; }

        #endregion

        #endregion

        #region Fields

        public List<Info> infos = new List<Info>()
        {
            new Info("FPS", () => LegacyPlugin.FPSCounter.Text),
            new Info("Time in Editor", () => RTString.SecondsToTime(RTEditor.inst.editorInfo.timer.time)),
            new Info("Song Progress", () => $"{RTString.Percentage(AudioManager.inst.CurrentAudioSource.time, AudioManager.inst.CurrentAudioSource.clip.length)}%"),
            new Info("Level opened amount", () => RTEditor.inst.editorInfo.openAmount.ToString()),

            new Info("Object Count", () => GameData.Current.beatmapObjects.FindAll(x => !x.fromPrefab).Count.ToString()),
            new Info("Total Object Count", () => GameData.Current.beatmapObjects.Count.ToString()),
            new Info("Objects Alive Count", () => GameData.Current.beatmapObjects.FindAll(x => x.Alive).Count.ToString()),
            new Info("No Autokill Count", () => GameData.Current.beatmapObjects.FindAll(x => x.autoKillType == BeatmapObject.AutoKillType.OldStyleNoAutokill).Count.ToString()),
            new Info("Keyframe Offsets > Song Length Count", () => GameData.Current.beatmapObjects.FindAll(x => x.autoKillOffset > AudioManager.inst.CurrentAudioSource.clip.length).Count.ToString()),
            new Info("Text Object Count", () => GameData.Current.beatmapObjects.FindAll(x => x.shape == 4 && x.objectType != BeatmapObject.ObjectType.Empty).Count.ToString()),
            new Info("Text Symbol Total Count", () => GameData.Current.beatmapObjects.Where(x => x.shape == 4 && x.objectType != BeatmapObject.ObjectType.Empty).Sum(x => x.text.Length).ToString()),

            new Info("BG Object Count", () => GameData.Current.backgroundObjects.Count.ToString()),

            new Info("Camera Position", () => $"X: {Camera.main.transform.position.x}, Y: {Camera.main.transform.position.y}"),
            new Info("Camera Zoom", () => Camera.main.orthographicSize.ToString()),
            new Info("Camera Rotation", () => Camera.main.transform.rotation.eulerAngles.z.ToString()),
            new Info("Event Count", () => GameData.Current.events.Sum(x => x.Count).ToString()),
            new Info("Theme Count", () => ThemeManager.inst.ThemeCount.ToString()),

            new Info("Prefab External Count", () => RTPrefabEditor.inst.PrefabPanels.Count.ToString()),
            new Info("Prefab Internal Count", () => GameData.Current.prefabs.Count.ToString()),
            new Info("Prefab Objects Count", () => GameData.Current.prefabObjects.Count.ToString()),

            new Info("Timeline Bin Count", () => EditorTimeline.inst.BinCount.ToString()),
            new Info("Timeline Objects in Current Layer Count", () => EditorTimeline.inst.timelineObjects.FindAll(x => x.Layer == EditorManager.inst.layer).Count.ToString()),
            new Info("Markers Count", () => GameData.Current.data.markers.Count.ToString()),
        };

        #endregion

        #region Methods

        public override void Init()
        {
            base.Init();

            EditorThemeManager.AddGraphic(GameObject.GetComponent<Image>(), ThemeGroup.Background_1);
            var snap = GameObject.transform.Find("snap");

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

            snap.AsRT().sizeDelta = new Vector2(765f, 140f);

            var title1 = snap.GetChild(0).gameObject.Duplicate(GameObject.transform, "info title");
            var editorInformationText = title1.transform.Find("title").GetComponent<Text>();
            editorInformationText.text = "Editor Information";

            EditorThemeManager.AddGraphic(title1.transform.Find("Panel/icon").GetComponent<Image>(), ThemeGroup.Light_Text);
            EditorThemeManager.AddLightText(editorInformationText);

            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(GameObject.transform, "Scroll View");
            scrollView.transform.AsRT().sizeDelta = new Vector2(765f, 120f);
            var content = scrollView.transform.Find("Viewport/Content");

            for (int i = 0; i < infos.Count; i++)
            {
                var info = infos[i];
                var baseInfo = Creator.NewUIObject("Info", content);
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

            var title2 = GameObject.transform.Find("snap").GetChild(0).gameObject.Duplicate(GameObject.transform, "marker colors title");
            var markerColorsText = title2.transform.Find("title").GetComponent<Text>();
            markerColorsText.text = "Marker Colors / Editor Layer Colors";

            EditorThemeManager.AddGraphic(title2.transform.Find("Panel/icon").GetComponent<Image>(), ThemeGroup.Light_Text);

            EditorThemeManager.AddLightText(markerColorsText);

            var colorEditors = Creator.NewUIObject("Color Editors", GameObject.transform);
            var colorEditorsLayout = colorEditors.AddComponent<HorizontalLayoutGroup>();
            colorEditors.transform.AsRT().sizeDelta = new Vector2(765f, 240f);
            colorEditorsLayout.spacing = 16f;

            // Marker Colors
            MarkerColorsContent = EditorPrefabHolder.Instance.ScrollView.Duplicate(colorEditors.transform, "Scroll View").transform.Find("Viewport/Content");

            // Layer Colors
            LayerColorsContent = EditorPrefabHolder.Instance.ScrollView.Duplicate(colorEditors.transform, "Scroll View").transform.Find("Viewport/Content");

            var analyzeBPM = EditorPrefabHolder.Instance.Function2Button.Duplicate(GameObject.transform.Find("snap/toggle"), "analyze");
            analyzeBPM.transform.AsRT().sizeDelta = new Vector2(140f, 32f);
            AnalyzeBPMButton = analyzeBPM.GetComponent<FunctionButtonStorage>();
            AnalyzeBPMButton.label.text = "Analyze BPM";
            AnalyzeBPMButton.button.onClick.NewListener(() =>
            {
                CoreHelper.StartCoroutineAsync(UniBpmAnalyzer.IAnalyzeBPM(AudioManager.inst.CurrentAudioSource.clip, bpm =>
                {
                    EditorManager.inst.DisplayNotification($"Detected a BPM of {bpm}! Applied it to editor settings.", 2f, EditorManager.NotificationType.Success);

                    BPMInput.text = bpm.ToString();
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
