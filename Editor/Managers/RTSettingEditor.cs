﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Dialogs;

namespace BetterLegacy.Editor.Managers
{
    public class RTSettingEditor : MonoBehaviour
    {
        #region Init

        public static RTSettingEditor inst;

        public static void Init() => SettingEditor.inst.gameObject.AddComponent<RTSettingEditor>();

        void Awake()
        {
            inst = this;
            StartCoroutine(SetupUI());

            try
            {
                Dialog = new EditorDialog(EditorDialog.SETTINGS_EDITOR);
                Dialog.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog
        }

        IEnumerator SetupUI()
        {
            dialog = EditorManager.inst.GetDialog("Settings Editor").Dialog.AsRT();

            EditorThemeManager.AddGraphic(dialog.GetComponent<Image>(), ThemeGroup.Background_1);
            var snap = dialog.Find("snap");

            var slider = snap.Find("bpm/slider").gameObject.GetComponent<Slider>();
            slider.maxValue = 999f;
            slider.minValue = 0f;

            DestroyImmediate(snap.Find("bpm/<").gameObject);
            DestroyImmediate(snap.Find("bpm/>").gameObject);
            EditorThemeManager.AddToggle(snap.Find("toggle/toggle").GetComponent<Toggle>());
            EditorThemeManager.AddLightText(snap.Find("toggle/title").GetComponent<Text>());
            EditorThemeManager.AddLightText(snap.Find("bpm/title").GetComponent<Text>());
            snap.Find("toggle/title").AsRT().sizeDelta = new Vector2(100f, 32f);
            snap.Find("bpm/title").AsRT().sizeDelta = new Vector2(100f, 32f);

            EditorThemeManager.AddGraphic(snap.transform.Find("title_/Panel/icon").GetComponent<Image>(), ThemeGroup.Light_Text);
            EditorThemeManager.AddLightText(snap.transform.Find("title_/title").GetComponent<Text>());

            var bpmSlider = snap.Find("bpm/slider").GetComponent<Slider>();
            EditorThemeManager.AddGraphic(bpmSlider.transform.Find("Background").GetComponent<Image>(), ThemeGroup.Slider_2, true);
            EditorThemeManager.AddGraphic(bpmSlider.image, ThemeGroup.Slider_2_Handle, true);

            EditorThemeManager.AddInputField(snap.Find("bpm/input").GetComponent<InputField>());

            var snapOffset = snap.Find("bpm").gameObject.Duplicate(dialog.Find("snap"), "bpm offset");
            var snapOffsetText = snapOffset.transform.Find("title").GetComponent<Text>();
            snapOffsetText.text = "BPM Offset";
            snapOffsetText.rectTransform.sizeDelta = new Vector2(100f, 32f);
            EditorThemeManager.AddLightText(snapOffsetText);

            var bpmOffsetSlider = snapOffset.transform.Find("slider").GetComponent<Slider>();
            EditorThemeManager.AddGraphic(bpmOffsetSlider.transform.Find("Background").GetComponent<Image>(), ThemeGroup.Slider_2, true);
            EditorThemeManager.AddGraphic(bpmOffsetSlider.image, ThemeGroup.Slider_2_Handle, true);

            EditorThemeManager.AddInputField(snapOffset.transform.Find("input").GetComponent<InputField>());

            snap.AsRT().sizeDelta = new Vector2(765f, 140f);

            var title1 = snap.GetChild(0).gameObject.Duplicate(dialog, "info title");
            var editorInformationText = title1.transform.Find("title").GetComponent<Text>();
            editorInformationText.text = "Editor Information";

            EditorThemeManager.AddGraphic(title1.transform.Find("Panel/icon").GetComponent<Image>(), ThemeGroup.Light_Text);
            EditorThemeManager.AddLightText(editorInformationText);

            info.Clear();
            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(dialog, "Scroll View");
            scrollView.transform.AsRT().sizeDelta = new Vector2(765f, 120f);
            var content = scrollView.transform.Find("Viewport/Content");

            var array = new string[]
            {
                "FPS",
                "Time in Editor",
                "Song Progress",
                "Level opened amount",

                "Object Count",
                "Total Object Count",
                "Objects Alive Count",
                "No Autokill Count",
                "Keyframe Offsets > Song Length Count",
                "Text Object Count",
                "Text Symbol Total Count",

                "BG Object Count",

                "Camera Position",
                "Camera Zoom",
                "Camera Rotation",
                "Event Count",
                "Theme Count",

                "Prefab External Count",
                "Prefab Internal Count",
                "Prefab Objects Count",

                "Timeline Bin Count",
                "Timeline Objects in Current Layer Count",
                "Markers Count",
            };

            for (int i = 0; i < array.Length; i++)
            {
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
                titleText.text = "  " + array[i];

                EditorThemeManager.AddLightText(titleText);

                var infoGO = Creator.NewUIObject("Title", baseInfo.transform);

                var infoText = infoGO.AddComponent<Text>();
                infoText.font = FontManager.inst.DefaultFont;
                infoText.fontSize = 19;
                infoText.alignment = TextAnchor.MiddleRight;
                infoText.text = "[ 0 ]";

                EditorThemeManager.AddLightText(infoText);

                info.Add(array[i], infoText);
            }

            // Doggo
            var loadingDoggo = Creator.NewUIObject("loading doggo", dialog);
            doggo = loadingDoggo.AddComponent<Image>();
            var loadingDoggoLE = loadingDoggo.AddComponent<LayoutElement>();

            loadingDoggo.transform.AsRT().anchoredPosition = new Vector2(UnityEngine.Random.Range(-320f, 320f), UnityEngine.Random.Range(-300f, -275f));
            float sizeRandom = 64f * UnityEngine.Random.Range(0.5f, 1f);
            loadingDoggo.transform.AsRT().sizeDelta = new Vector2(sizeRandom, sizeRandom);

            loadingDoggoLE.ignoreLayout = true;

            var title2 = dialog.Find("snap").GetChild(0).gameObject.Duplicate(dialog, "marker colors title");
            var markerColorsText = title2.transform.Find("title").GetComponent<Text>();
            markerColorsText.text = "Marker Colors / Editor Layer Colors";

            EditorThemeManager.AddGraphic(title2.transform.Find("Panel/icon").GetComponent<Image>(), ThemeGroup.Light_Text);

            EditorThemeManager.AddLightText(markerColorsText);

            var colorEditors = Creator.NewUIObject("Color Editors", dialog);
            var colorEditorsLayout = colorEditors.AddComponent<HorizontalLayoutGroup>();
            colorEditors.transform.AsRT().sizeDelta = new Vector2(765f, 240f);
            colorEditorsLayout.spacing = 16f;

            // Marker Colors
            markerColorsContent = EditorPrefabHolder.Instance.ScrollView.Duplicate(colorEditors.transform, "Scroll View").transform.Find("Viewport/Content");

            // Layer Colors
            layerColorsContent = EditorPrefabHolder.Instance.ScrollView.Duplicate(colorEditors.transform, "Scroll View").transform.Find("Viewport/Content");

            colorPrefab = Creator.NewUIObject("Color", transform);
            var tagPrefabImage = colorPrefab.AddComponent<Image>();
            tagPrefabImage.color = new Color(1f, 1f, 1f, 0.12f);
            var tagPrefabLayout = colorPrefab.AddComponent<HorizontalLayoutGroup>();
            tagPrefabLayout.childControlWidth = false;
            tagPrefabLayout.childForceExpandWidth = false;

            var input = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(colorPrefab.transform, "Input");
            input.transform.localScale = Vector3.one;
            input.transform.AsRT().sizeDelta = new Vector2(136f, 32f);
            var text = input.transform.Find("Text").GetComponent<Text>();
            text.alignment = TextAnchor.MiddleLeft;
            text.fontSize = 17;

            var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(colorPrefab.transform, "Delete");
            UIManager.SetRectTransform(delete.transform.AsRT(), new Vector2(748f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.one, new Vector2(32f, 32f));

            var analyzeBPM = EditorPrefabHolder.Instance.Function2Button.Duplicate(dialog.Find("snap/toggle"), "analyze");
            analyzeBPM.transform.AsRT().sizeDelta = new Vector2(140f, 32f);
            var analyzeBPMStorage = analyzeBPM.GetComponent<FunctionButtonStorage>();
            analyzeBPMStorage.text.text = "Analyze BPM";
            analyzeBPMStorage.button.onClick.ClearAll();
            analyzeBPMStorage.button.onClick.AddListener(() =>
            {
                CoreHelper.StartCoroutineAsync(UniBpmAnalyzer.IAnalyzeBPM(AudioManager.inst.CurrentAudioSource.clip, bpm =>
                {
                    EditorManager.inst.DisplayNotification($"Detected a BPM of {bpm}! Applied it to editor settings.", 2f, EditorManager.NotificationType.Success);

                    dialog.Find("snap/bpm/input").GetComponent<InputField>().text = bpm.ToString();
                }));
            });

            EditorThemeManager.AddSelectable(analyzeBPMStorage.button, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(analyzeBPMStorage.text, ThemeGroup.Function_2_Text);

            yield break;
        }

        #endregion

        #region Values

        public EditorDialog Dialog { get; set; }

        public RectTransform dialog;

        Dictionary<string, Text> info = new Dictionary<string, Text>();
        public Image doggo;

        Transform markerColorsContent;
        Transform layerColorsContent;

        GameObject colorPrefab;

        #endregion

        #region Methods

        void SetText(string name, string text)
        {
            if (!info.TryGetValue(name, out Text textComponent) || !textComponent)
                return;

            textComponent.text = $"[ {text} ]  ";
        }

        void Update()
        {
            if (CoreHelper.InEditor && EditorManager.inst.isEditing && EditorManager.inst.hasLoadedLevel &&
                GameData.Current && GameData.Current.events != null &&
                RTPrefabEditor.inst && Dialog && Dialog.GameObject)
            {
                var transform = Dialog.GameObject.transform;

                if (!transform || !transform.gameObject.activeInHierarchy)
                    return;

                try
                {
                    SetText("FPS", LegacyPlugin.FPSCounter.Text);
                    SetText("Time in Editor", RTString.SecondsToTime(RTEditor.inst.editorInfo.timer.time));
                    SetText("Song Progress", $"{RTString.Percentage(AudioManager.inst.CurrentAudioSource.time, AudioManager.inst.CurrentAudioSource.clip.length)}%");
                    SetText("Level opened amount", RTEditor.inst.editorInfo.openAmount.ToString());

                    SetText("Object Count", GameData.Current.beatmapObjects.FindAll(x => !x.fromPrefab).Count.ToString());
                    SetText("Total Object Count", GameData.Current.beatmapObjects.Count.ToString());
                    SetText("Objects Alive Count", GameData.Current.beatmapObjects.FindAll(x => x.Alive).Count.ToString());
                    SetText("No Autokill Count", GameData.Current.beatmapObjects.FindAll(x => x.autoKillType == BeatmapObject.AutoKillType.OldStyleNoAutokill).Count.ToString());
                    SetText("Keyframe Offsets > Song Length Count", GameData.Current.beatmapObjects.FindAll(x => x.autoKillOffset > AudioManager.inst.CurrentAudioSource.clip.length).Count.ToString());
                    SetText("Text Object Count", GameData.Current.beatmapObjects.FindAll(x => x.shape == 4 && x.objectType != BeatmapObject.ObjectType.Empty).Count.ToString());
                    SetText("Text Symbol Total Count", GameData.Current.beatmapObjects.Where(x => x.shape == 4 && x.objectType != BeatmapObject.ObjectType.Empty).Sum(x => x.text.Length).ToString());

                    SetText("BG Object Count", GameData.Current.backgroundObjects.Count.ToString());

                    SetText("Camera Position", $"X: {Camera.main.transform.position.x}, Y: {Camera.main.transform.position.y}");
                    SetText("Camera Zoom", Camera.main.orthographicSize.ToString());
                    SetText("Camera Rotation", Camera.main.transform.rotation.eulerAngles.z.ToString());
                    SetText("Event Count", GameData.Current.events.Sum(x => x.Count).ToString());
                    SetText("Theme Count", ThemeManager.inst.ThemeCount.ToString());

                    SetText("Prefab External Count", RTPrefabEditor.inst.PrefabPanels.Count.ToString());
                    SetText("Prefab Internal Count", GameData.Current.prefabs.Count.ToString());
                    SetText("Prefab Objects Count", GameData.Current.prefabObjects.Count.ToString());

                    SetText("Timeline Bin Count", EditorTimeline.inst.BinCount.ToString());
                    SetText("Timeline Objects in Current Layer Count", EditorTimeline.inst.timelineObjects.FindAll(x => x.Layer == EditorManager.inst.layer).Count.ToString());
                    SetText("Markers Count", GameData.Current.data.markers.Count.ToString());
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
            }
        }

        void SetBPMSlider(Slider slider, InputField input)
        {
            slider.onValueChanged.ClearAll();
            slider.value = RTEditor.inst.editorInfo.bpm;
            slider.onValueChanged.AddListener(_val =>
            {
                MetaData.Current.song.BPM = _val;
                RTEditor.inst.editorInfo.bpm = _val;
                SetBPMInputField(slider, input);
                EditorTimeline.inst.SetTimelineGridSize();
            });
        }

        void SetBPMInputField(Slider slider, InputField input)
        {
            input.onValueChanged.ClearAll();
            input.text = RTEditor.inst.editorInfo.bpm.ToString();
            input.onValueChanged.AddListener(_val =>
            {
                var bpm = Parser.TryParse(_val, 120f);
                MetaData.Current.song.BPM = bpm;
                RTEditor.inst.editorInfo.bpm = bpm;
                SetBPMSlider(slider, input);
                EditorTimeline.inst.SetTimelineGridSize();
            });
        }

        void SetBPMOffsetSlider(Slider slider, InputField input)
        {
            slider.onValueChanged.ClearAll();
            slider.value = RTEditor.inst.editorInfo.bpmOffset;
            slider.onValueChanged.AddListener(_val =>
            {
                RTEditor.inst.editorInfo.bpmOffset = _val;
                SetBPMOffsetInputField(slider, input);
                EditorTimeline.inst.SetTimelineGridSize();
                RTEditor.inst.SaveSettings();
            });
        }

        void SetBPMOffsetInputField(Slider slider, InputField input)
        {
            input.onValueChanged.ClearAll();
            input.text = RTEditor.inst.editorInfo.bpmOffset.ToString();
            input.onValueChanged.AddListener(_val =>
            {
                var bpm = Parser.TryParse(_val, 0f);
                RTEditor.inst.editorInfo.bpmOffset = bpm;
                SetBPMOffsetSlider(slider, input);
                EditorTimeline.inst.SetTimelineGridSize();
                RTEditor.inst.SaveSettings();
            });
        }

        public void OpenDialog()
        {
            EditorManager.inst.CancelInvoke("LoadingIconUpdate");
            EditorManager.inst.InvokeRepeating("LoadingIconUpdate", 0f, UnityEngine.Random.Range(0.01f, 0.4f));

            Dialog.Open();

            var transform = Dialog.GameObject.transform.AsRT();
            var loadingDoggoRect = transform.Find("loading doggo").AsRT();

            loadingDoggoRect.anchoredPosition = new Vector2(UnityEngine.Random.Range(-320f, 320f), UnityEngine.Random.Range(-310f, -340f));
            float sizeRandom = 64 * UnityEngine.Random.Range(0.5f, 1f);
            loadingDoggoRect.sizeDelta = new Vector2(sizeRandom, sizeRandom);

            var toggle = transform.Find("snap/toggle/toggle").GetComponent<Toggle>();
            toggle.onValueChanged.ClearAll();
            toggle.isOn = RTEditor.inst.editorInfo.bpmSnapActive;
            toggle.onValueChanged.AddListener(_val => RTEditor.inst.editorInfo.bpmSnapActive = _val);

            var slider = transform.Find("snap/bpm/slider").GetComponent<Slider>();
            var input = transform.Find("snap/bpm/input").GetComponent<InputField>();
            SetBPMSlider(slider, input);
            SetBPMInputField(slider, input);

            TriggerHelper.AddEventTriggers(input.gameObject,
                TriggerHelper.ScrollDelta(input, 1f));

            var sliderOffset = transform.Find("snap/bpm offset/slider").GetComponent<Slider>();
            var inputOffset = transform.Find("snap/bpm offset/input").GetComponent<InputField>();
            SetBPMOffsetSlider(sliderOffset, inputOffset);
            SetBPMOffsetInputField(sliderOffset, inputOffset);

            TriggerHelper.AddEventTriggers(inputOffset.gameObject,
                TriggerHelper.ScrollDelta(inputOffset));

            RenderMarkerColors();
            RenderLayerColors();
        }

        public void RenderMarkerColors()
        {
            LSHelpers.DeleteChildren(markerColorsContent);

            var add = PrefabEditor.inst.CreatePrefab.Duplicate(markerColorsContent, "Add");

            ((RectTransform)add.transform).sizeDelta = new Vector2(402f, 32f);
            var addText = add.transform.Find("Text").GetComponent<Text>();
            addText.text = "Add Marker Color";
            var addButton = add.GetComponent<Button>();
            addButton.onClick.ClearAll();
            addButton.onClick.AddListener(() =>
            {
                MarkerEditor.inst.markerColors.Add(LSColors.pink500);
                RTEditor.inst.SaveGlobalSettings();
                RenderMarkerColors();
            });

            EditorThemeManager.ApplyGraphic(addButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(addText, ThemeGroup.Add_Text);

            int num = 0;
            foreach (var markerColor in MarkerEditor.inst.markerColors)
            {
                int index = num;

                var gameObject = colorPrefab.Duplicate(markerColorsContent, "Color");
                gameObject.transform.AsRT().sizeDelta = new Vector2(402f, 32f);
                var image = gameObject.GetComponent<Image>();
                image.color = markerColor;

                EditorThemeManager.ApplyGraphic(image, ThemeGroup.Null, true, 2);

                var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                input.onValueChanged.ClearAll();
                input.onEndEdit.ClearAll();
                input.text = LSColors.ColorToHex(markerColor);
                input.onValueChanged.AddListener(_val =>
                {
                    MarkerEditor.inst.markerColors[index] = _val.Length == 6 ? LSColors.HexToColor(_val) : LSColors.pink500;
                    image.color = MarkerEditor.inst.markerColors[index];
                });
                input.onEndEdit.AddListener(_val => { RTEditor.inst.SaveGlobalSettings(); });

                EditorThemeManager.ApplyInputField(input);

                var deleteStorage = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                deleteStorage.button.onClick.ClearAll();
                deleteStorage.button.onClick.AddListener(() =>
                {
                    MarkerEditor.inst.markerColors.RemoveAt(index);
                    RenderMarkerColors();
                    RTEditor.inst.SaveGlobalSettings();
                });

                EditorThemeManager.ApplyGraphic(deleteStorage.baseImage, ThemeGroup.Delete);
                EditorThemeManager.ApplyGraphic(deleteStorage.image, ThemeGroup.Delete_Text);

                num++;
            }
        }

        public void RenderLayerColors()
        {
            LSHelpers.DeleteChildren(layerColorsContent);

            var add = PrefabEditor.inst.CreatePrefab.Duplicate(layerColorsContent, "Add");

            ((RectTransform)add.transform).sizeDelta = new Vector2(402f, 32f);
            var addText = add.transform.Find("Text").GetComponent<Text>();
            addText.text = "Add Layer Color";
            var addButton = add.GetComponent<Button>();
            addButton.onClick.ClearAll();
            addButton.onClick.AddListener(() =>
            {
                EditorManager.inst.layerColors.Add(LSColors.pink500);
                RTEditor.inst.SaveGlobalSettings();
                RenderLayerColors();
            });

            EditorThemeManager.ApplyGraphic(addButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(addText, ThemeGroup.Add_Text);

            int num = 0;
            foreach (var layerColor in EditorManager.inst.layerColors)
            {
                int index = num;

                var gameObject = colorPrefab.Duplicate(layerColorsContent, "Color");
                gameObject.transform.AsRT().sizeDelta = new Vector2(402f, 32f);
                var image = gameObject.GetComponent<Image>();
                image.color = layerColor;

                EditorThemeManager.ApplyGraphic(image, ThemeGroup.Null, true, 2);

                var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                input.onValueChanged.ClearAll();
                input.onEndEdit.ClearAll();
                input.text = LSColors.ColorToHex(layerColor);
                input.onValueChanged.AddListener(_val =>
                {
                    EditorManager.inst.layerColors[index] = _val.Length == 6 ? LSColors.HexToColor(_val) : LSColors.pink500;
                    image.color = EditorManager.inst.layerColors[index];
                });
                input.onEndEdit.AddListener(_val => { RTEditor.inst.SaveGlobalSettings(); });

                EditorThemeManager.ApplyInputField(input);

                var deleteStorage = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                deleteStorage.button.onClick.ClearAll();
                deleteStorage.button.onClick.AddListener(() =>
                {
                    EditorManager.inst.layerColors.RemoveAt(index);
                    RenderLayerColors();
                    RTEditor.inst.SaveGlobalSettings();
                });

                EditorThemeManager.ApplyGraphic(deleteStorage.baseImage, ThemeGroup.Delete);
                EditorThemeManager.ApplyGraphic(deleteStorage.image, ThemeGroup.Delete_Text);

                num++;
            }
        }

        #endregion
    }
}
