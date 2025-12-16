using System;
using System.Collections.Generic;

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
using BetterLegacy.Editor.Managers.Settings;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Manages editing editor level settings.
    /// <br></br>Wraps <see cref="SettingEditor"/>.
    /// </summary>
    public class RTSettingEditor : BaseEditor<RTSettingEditor, RTSettingEditorSettings, SettingEditor>
    {
        #region Values

        public override SettingEditor BaseInstance { get => SettingEditor.inst; set => SettingEditor.inst = value; }

        public SettingEditorDialog Dialog { get; set; }

        public float BPMMulti => 60f / (RTEditor.inst.editorInfo?.bpm ?? 140f);

        GameObject colorPrefab;

        #endregion

        #region Methods

        public override void OnInit()
        {
            try
            {
                Dialog = new SettingEditorDialog();
                Dialog.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog

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
        }

        public override void OnTick()
        {
            if (CoreHelper.InEditor && EditorManager.inst.isEditing && EditorManager.inst.hasLoadedLevel &&
                GameData.Current && GameData.Current.events != null &&
                RTPrefabEditor.inst && Dialog && Dialog.GameObject)
            {
                try
                {
                    if (Dialog.GameObject.activeInHierarchy)
                        Dialog.infos.ForLoop(info => info.Render());
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
            }
        }

        void SetBPMSlider(Slider slider, InputField input)
        {
            slider.SetValueWithoutNotify(RTEditor.inst.editorInfo.bpm);
            slider.onValueChanged.NewListener(_val =>
            {
                MetaData.Current.song.bpm = _val;
                RTEditor.inst.editorInfo.bpm = _val;
                SetBPMInputField(slider, input);
                EditorTimeline.inst.SetTimelineGridSize();
            });
        }

        void SetBPMInputField(Slider slider, InputField input)
        {
            input.SetTextWithoutNotify(RTEditor.inst.editorInfo.bpm.ToString());
            input.onValueChanged.NewListener(_val =>
            {
                var variables = new Dictionary<string, float>
                {
                    { "bpm", RTEditor.inst.editorInfo.bpm },
                    { "bpmOffset", RTEditor.inst.editorInfo.bpmOffset },
                    { "timeSignature", RTEditor.inst.editorInfo.timeSignature },
                };

                if (RTMath.TryParse(_val, RTEditor.inst.editorInfo.bpm, variables, out float bpm))
                {
                    MetaData.Current.song.bpm = bpm;
                    RTEditor.inst.editorInfo.bpm = bpm;
                    SetBPMSlider(slider, input);
                    EditorTimeline.inst.SetTimelineGridSize();
                }
            });
            input.onEndEdit.NewListener(_val => RenderDialog());
        }

        void SetBPMOffsetSlider(Slider slider, InputField input)
        {
            slider.SetValueWithoutNotify(RTEditor.inst.editorInfo.bpmOffset);
            slider.onValueChanged.NewListener(_val =>
            {
                RTEditor.inst.editorInfo.bpmOffset = _val;
                SetBPMOffsetInputField(slider, input);
                EditorTimeline.inst.SetTimelineGridSize();
            });
        }

        void SetBPMOffsetInputField(Slider slider, InputField input)
        {
            input.SetTextWithoutNotify(RTEditor.inst.editorInfo.bpmOffset.ToString());
            input.onValueChanged.NewListener(_val =>
            {
                var variables = new Dictionary<string, float>
                {
                    { "bpm", RTEditor.inst.editorInfo.bpm },
                    { "bpmOffset", RTEditor.inst.editorInfo.bpmOffset },
                    { "timeSignature", RTEditor.inst.editorInfo.timeSignature },
                };

                if (RTMath.TryParse(_val, RTEditor.inst.editorInfo.bpmOffset, variables, out float bpm))
                {
                    RTEditor.inst.editorInfo.bpmOffset = bpm;
                    SetBPMOffsetSlider(slider, input);
                    EditorTimeline.inst.SetTimelineGridSize();
                }
            });
            input.onEndEdit.NewListener(_val => RenderDialog());
        }

        void SetBPMTimingSlider(Slider slider, InputField input)
        {
            slider.SetValueWithoutNotify(RTEditor.inst.editorInfo.timeSignature);
            slider.onValueChanged.NewListener(_val =>
            {
                RTEditor.inst.editorInfo.timeSignature = _val;
                SetBPMTimingInputField(slider, input);
                EditorTimeline.inst.SetTimelineGridSize();
            });
        }

        void SetBPMTimingInputField(Slider slider, InputField input)
        {
            input.SetTextWithoutNotify(RTEditor.inst.editorInfo.timeSignature.ToString());
            input.onValueChanged.NewListener(_val =>
            {
                var variables = new Dictionary<string, float>
                {
                    { "bpm", RTEditor.inst.editorInfo.bpm },
                    { "bpmOffset", RTEditor.inst.editorInfo.bpmOffset },
                    { "timeSignature", RTEditor.inst.editorInfo.timeSignature },
                };

                if (RTMath.TryParse(_val, RTEditor.inst.editorInfo.timeSignature, variables, out float bpm))
                {
                    RTEditor.inst.editorInfo.timeSignature = bpm;
                    SetBPMTimingSlider(slider, input);
                    EditorTimeline.inst.SetTimelineGridSize();
                }
            });
            input.onEndEdit.NewListener(_val => RenderDialog());
        }

        public void OpenDialog()
        {
            Dialog.Open();
            RenderDialog();
        }

        public void RenderDialog()
        {
            EditorManager.inst.CancelInvoke(nameof(EditorManager.LoadingIconUpdate));
            EditorManager.inst.InvokeRepeating(nameof(EditorManager.LoadingIconUpdate), 0f, UnityRandom.Range(0.01f, 0.4f));

            var transform = Dialog.GameObject.transform.AsRT();
            var loadingDoggoRect = Dialog.Doggo.rectTransform;

            loadingDoggoRect.anchoredPosition = new Vector2(UnityRandom.Range(-320f, 320f), UnityRandom.Range(-310f, -340f));
            float sizeRandom = 64 * UnityRandom.Range(0.5f, 1f);
            loadingDoggoRect.sizeDelta = new Vector2(sizeRandom, sizeRandom);

            Dialog.BPMToggle.SetIsOnWithoutNotify(RTEditor.inst.editorInfo.bpmSnapActive);
            Dialog.BPMToggle.onValueChanged.NewListener(_val => RTEditor.inst.editorInfo.bpmSnapActive = _val);

            SetBPMSlider(Dialog.BPMSlider, Dialog.BPMInput);
            SetBPMInputField(Dialog.BPMSlider, Dialog.BPMInput);

            TriggerHelper.AddEventTriggers(Dialog.BPMInput.gameObject,
                TriggerHelper.ScrollDelta(Dialog.BPMInput, 1f));

            SetBPMOffsetSlider(Dialog.BPMOffsetSlider, Dialog.BPMOffsetInput);
            SetBPMOffsetInputField(Dialog.BPMOffsetSlider, Dialog.BPMOffsetInput);

            TriggerHelper.AddEventTriggers(Dialog.BPMOffsetInput.gameObject,
                TriggerHelper.ScrollDelta(Dialog.BPMOffsetInput));

            SetBPMTimingSlider(Dialog.BPMTimingSlider, Dialog.BPMTimingInput);
            SetBPMTimingInputField(Dialog.BPMTimingSlider, Dialog.BPMTimingInput);

            TriggerHelper.AddEventTriggers(Dialog.BPMTimingInput.gameObject,
                TriggerHelper.ScrollDelta(Dialog.BPMTimingInput));

            RenderMarkerColors();
            RenderLayerColors();
        }

        public void RenderMarkerColors()
        {
            LSHelpers.DeleteChildren(Dialog.MarkerColorsContent);

            var add = EditorPrefabHolder.Instance.CreateAddButton(Dialog.MarkerColorsContent);
            add.Text = "Add Marker Color";
            add.OnClick.NewListener(() =>
            {
                MarkerEditor.inst.markerColors.Add(RTColors.errorColor);
                RTEditor.inst.SaveGlobalSettings();
                RenderMarkerColors();
            });

            int num = 0;
            foreach (var markerColor in MarkerEditor.inst.markerColors)
            {
                int index = num;

                var gameObject = colorPrefab.Duplicate(Dialog.MarkerColorsContent, "Color");
                gameObject.transform.AsRT().sizeDelta = new Vector2(402f, 32f);
                var image = gameObject.GetComponent<Image>();
                image.color = markerColor;

                EditorThemeManager.ApplyGraphic(image, ThemeGroup.Null, true, 2);

                var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                input.SetTextWithoutNotify(LSColors.ColorToHex(markerColor));
                input.onValueChanged.NewListener(_val =>
                {
                    MarkerEditor.inst.markerColors[index] = _val.Length == 6 ? LSColors.HexToColor(_val) : LSColors.pink500;
                    image.color = MarkerEditor.inst.markerColors[index];
                });
                input.onEndEdit.NewListener(_val => RTEditor.inst.SaveGlobalSettings());

                EditorThemeManager.ApplyInputField(input);

                var deleteStorage = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                deleteStorage.OnClick.NewListener(() =>
                {
                    MarkerEditor.inst.markerColors.RemoveAt(index);
                    RenderMarkerColors();
                    RTEditor.inst.SaveGlobalSettings();
                });

                EditorThemeManager.ApplyDeleteButton(deleteStorage);

                num++;
            }
        }

        public void RenderLayerColors()
        {
            LSHelpers.DeleteChildren(Dialog.LayerColorsContent);

            var add = EditorPrefabHolder.Instance.CreateAddButton(Dialog.LayerColorsContent);
            add.Text = "Add Layer Color";
            add.OnClick.NewListener(() =>
            {
                EditorManager.inst.layerColors.Add(RTColors.errorColor);
                RTEditor.inst.SaveGlobalSettings();
                RenderLayerColors();
            });

            int num = 0;
            foreach (var layerColor in EditorManager.inst.layerColors)
            {
                int index = num;

                var gameObject = colorPrefab.Duplicate(Dialog.LayerColorsContent, "Color");
                gameObject.transform.AsRT().sizeDelta = new Vector2(402f, 32f);
                var image = gameObject.GetComponent<Image>();
                image.color = layerColor;

                EditorThemeManager.ApplyGraphic(image, ThemeGroup.Null, true, 2);

                var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                input.SetTextWithoutNotify(LSColors.ColorToHex(layerColor));
                input.onValueChanged.NewListener(_val =>
                {
                    EditorManager.inst.layerColors[index] = _val.Length == 6 ? LSColors.HexToColor(_val) : LSColors.pink500;
                    image.color = EditorManager.inst.layerColors[index];
                });
                input.onEndEdit.NewListener(_val => RTEditor.inst.SaveGlobalSettings());

                EditorThemeManager.ApplyInputField(input);

                var deleteStorage = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                deleteStorage.OnClick.NewListener(() =>
                {
                    EditorManager.inst.layerColors.RemoveAt(index);
                    RenderLayerColors();
                    RTEditor.inst.SaveGlobalSettings();
                });

                EditorThemeManager.ApplyDeleteButton(deleteStorage);

                num++;
            }
        }

        #endregion
    }
}
