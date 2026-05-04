using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Timeline;
using BetterLegacy.Editor.Managers.Settings;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Manages editing <see cref="Marker"/>s.
    /// <br></br>Wraps <see cref="MarkerEditor"/>.
    /// </summary>
    public class RTMarkerEditor : BaseEditor<RTMarkerEditor, RTMarkerEditorSettings, MarkerEditor>
    {
        #region Values

        public override MarkerEditor BaseInstance { get => MarkerEditor.inst; set => MarkerEditor.inst = value; }

        /// <summary>
        /// Dialog of the editor.
        /// </summary>
        public MarkerEditorDialog Dialog { get; set; }

        /// <summary>
        /// List of timeline markers.
        /// </summary>
        public List<TimelineMarker> timelineMarkers = new List<TimelineMarker>();

        /// <summary>
        /// The current selected marker.
        /// </summary>
        public TimelineMarker CurrentMarker { get; set; }

        /// <summary>
        /// Copied marker.
        /// </summary>
        public Marker markerCopy;

        /// <summary>
        /// If a marker is dragging.
        /// </summary>
        public bool dragging;

        /// <summary>
        /// Time to offset from the markers.
        /// </summary>
        public float dragTimeOffset;

        /// <summary>
        /// Copied list of markers.
        /// </summary>
        public List<Marker> copiedMarkers = new List<Marker>();

        #region Looping

        /// <summary>
        /// If marker looping is enabled.
        /// </summary>
        public bool markerLooping;
        /// <summary>
        /// Marker to loop to.
        /// </summary>
        public TimelineMarker markerLoopBegin;
        /// <summary>
        /// Marker to end loop.
        /// </summary>
        public TimelineMarker markerLoopEnd;

        #endregion

        #region Marker Functions

        /// <summary>
        /// Functions to run from a markers' description.
        /// </summary>
        public List<MarkerFunction> markerFunctions = new List<MarkerFunction>
        {
            new MarkerFunction(new Regex(@"setLayer\((.*?)\)"), match =>
            {
                var matchGroup = match.Groups[1].ToString();
                if (matchGroup.ToLower() == "events" || matchGroup.ToLower() == "check" || matchGroup.ToLower() == "event/check" || matchGroup.ToLower() == "event")
                    EditorTimeline.inst.SetLayer(EditorTimeline.LayerType.Events);
                else if (matchGroup.ToLower() == "object" || matchGroup.ToLower() == "objects")
                    EditorTimeline.inst.SetLayer(EditorTimeline.LayerType.Objects);
                else if (matchGroup.ToLower() == "toggle" || matchGroup.ToLower() == "swap")
                    EditorTimeline.inst.SetLayer(EditorTimeline.inst.layerType == EditorTimeline.LayerType.Objects ? EditorTimeline.LayerType.Events : EditorTimeline.LayerType.Objects);
                else if (int.TryParse(matchGroup, out int layer))
                    EditorTimeline.inst.SetLayer(Mathf.Clamp(layer - 1, 0, int.MaxValue));
            }),
            new MarkerFunction(new Regex(@"setBin\((.*?)\)"), match =>
            {
                var matchGroup = match.Groups[1].ToString();
                if (int.TryParse(matchGroup, out int result))
                    EditorTimeline.inst.SetBinPosition(result);
            }),
            new MarkerFunction(new Regex(@"setTimeline\((.*?)\)"), match =>
            {
                if (int.TryParse(match.Groups[1].ToString(), out int zoom))
                    EditorTimeline.inst.SetTimeline(zoom, match.Groups.Count > 1 && float.TryParse(match.Groups[2].ToString(), out float position) ? position : -1f);
            }),
        };

        /// <summary>
        /// Runs a custom function from a marker.
        /// </summary>
        public class MarkerFunction
        {
            public MarkerFunction(Regex regex, Action<Match> result)
            {
                Regex = regex;
                Result = result;
            }

            /// <summary>
            /// If the function should run automatically when the marker is opened.
            /// </summary>
            public bool Auto { get; set; } = true;

            /// <summary>
            /// The pattern to search for in the markers' description.
            /// </summary>
            public Regex Regex { get; set; }

            /// <summary>
            /// The match result.
            /// </summary>
            public Action<Match> Result { get; set; }
        }

        #endregion

        #region Annotation

        /// <summary>
        /// The current annotation stroke.
        /// </summary>
        public Annotation currentStroke;

        /// <summary>
        /// The current horizontally mirrored annotation stroke.
        /// </summary>
        public Annotation mirrorHorizontalStroke;

        /// <summary>
        /// The current vertically mirrored annotation stroke.
        /// </summary>
        public Annotation mirrorVerticalStroke;

        /// <summary>
        /// Annotation settings.
        /// </summary>
        public AnnotationSettings Settings => RTEditor.inst?.editorInfo?.annotationSettings;

        /// <summary>
        /// If an annotation is currently being drawn.
        /// </summary>
        public bool drawingAnnotation;

        /// <summary>
        /// Eraser radius.
        /// </summary>
        public float eraserRadius = 0.5f;

        /// <summary>
        /// Bucket radius.
        /// </summary>
        public float bucketRadius = 0.5f;

        /// <summary>
        /// Copied list of annotations.
        /// </summary>
        public List<Annotation> copiedAnnotations = new List<Annotation>();

        /// <summary>
        /// If annotations are currently being translated (moved / rotated).
        /// </summary>
        public bool movingAnnotations;

        Vector2 startMovePos;
        List<List<Vector2>> pointsCache = new List<List<Vector2>>();

        #endregion

        #endregion

        #region Functions

        public override void OnInit()
        {
            try
            {
                Dialog = new MarkerEditorDialog();
                Dialog.Init();
                var activeState = Dialog.GameObject.AddComponent<ActiveState>();
                activeState.onStateChanged = state =>
                {
                    if (state || !EditorConfig.Instance.DeselectMarkersOnDialogClosed.Value)
                        return;

                    dragging = false;
                    for (int i = 0; i < timelineMarkers.Count; i++)
                    {
                        var timelineMarker = timelineMarkers[i];
                        timelineMarker.Selected = false;
                        timelineMarker.dragging = false;
                    }
                    CurrentMarker = null;
                };

                var prefab = MarkerEditor.inst.markerPrefab;
                var prefabCopy = prefab.Duplicate(transform, prefab.name);
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
                markerStorage.area = Creator.NewUIObject("area", markerStorage.line.transform).AddComponent<Image>();
                markerStorage.area.color = new Color(1f, 1f, 1f, 0.1f);
                new RectValues(Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, 0.5f), new Vector2(32f, 0f)).AssignToRectTransform(markerStorage.area.rectTransform);
                MarkerEditor.inst.markerPrefab = prefabCopy;
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog
        }

        public override void OnTick()
        {
            AnnotationTick();

            if (dragging && Input.GetMouseButtonUp((int)EditorConfig.Instance.MarkerDragButton.Value))
                StopDragging();

            var timelineTime = EditorTimeline.inst.GetTimelineTime(RTEditor.inst.editorInfo.bpmSnapActive && EditorConfig.Instance.BPMSnapsMarkers.Value);

            for (int i = 0; i < timelineMarkers.Count; i++)
            {
                var timelineMarker = timelineMarkers[i];
                if (!timelineMarker.dragging)
                    continue;

                timelineMarker.Time = Mathf.Round(Mathf.Clamp(timelineTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length) * 1000f) / 1000f + (dragTimeOffset + timelineMarker.timeOffset);
                timelineMarker.RenderPosition();
            }

            if (dragging && CurrentMarker && Dialog.IsCurrent)
                RenderTime(CurrentMarker.Marker);

            if (EditorManager.inst.loading || !markerLooping || GameData.Current.data.markers.Count <= 0 || !markerLoopBegin || !markerLoopEnd)
                return;

            if (AudioManager.inst.CurrentAudioSource.time > markerLoopEnd.Time)
            {
                switch (EditorConfig.Instance.MarkerLoopBehavior.Value)
                {
                    case MarkerLoopBehavior.Loop: {
                            AudioManager.inst.SetMusicTime(markerLoopBegin.Time);
                            break;
                        }
                    case MarkerLoopBehavior.StopAtStart: {
                            AudioManager.inst.SetMusicTime(markerLoopBegin.Time);
                            RTEditor.inst.SetPlaying(false);
                            break;
                        }
                    case MarkerLoopBehavior.StopAtEnd: {
                            RTEditor.inst.SetPlaying(false);
                            break;
                        }
                }
            }
        }

        #region Editor Rendering

        /// <summary>
        /// Opens the Marker editor.
        /// </summary>
        /// <param name="timelineMarker">The marker to edit.</param>
        public void OpenDialog(TimelineMarker timelineMarker)
        {
            Dialog.Open();
            RenderDialog(timelineMarker);
        }

        /// <summary>
        /// Renders the Marker editor.
        /// </summary>
        public void RenderDialog() => RenderDialog(CurrentMarker);

        /// <summary>
        /// Renders the Marker editor.
        /// </summary>
        /// <param name="timelineMarker">The marker to edit.</param>
        public void RenderDialog(TimelineMarker timelineMarker)
        {
            UpdateMarkerList();
            RenderMarkers();

            var marker = timelineMarker.Marker;

            RenderLabel(timelineMarker);
            RenderColors(marker);
            RenderLayers(marker);
            RenderNameEditor(marker);
            RenderDescriptionEditor(marker);
            RenderTime(marker);
            RenderDuration(marker);

            RenderAnnotationTool();
            RenderAnnotationMirror();
            RenderAnnotationColors();
            RenderAnnotationHexColor();
            RenderAnnotationOpacity();
            RenderAnnotationThickness();
            RenderAnnotationFixedCamera();

            CheckDescription(marker);
        }

        /// <summary>
        /// Updates the label of the marker.
        /// </summary>
        /// <param name="timelineMarker">Marker to use.</param>
        public void RenderLabel(TimelineMarker timelineMarker) => Dialog.IndexText.text = $"Index: {timelineMarker.Index} ID: {timelineMarker.Marker.id}";

        /// <summary>
        /// Updates the name input field.
        /// </summary>
        /// <param name="marker">Marker to edit.</param>
        public void RenderNameEditor(Marker marker)
        {
            Dialog.NameField.SetTextWithoutNotify(timelineMarkers.Count(x => x.Selected) > 1 ? string.Empty : marker.name);
            Dialog.NameField.onValueChanged.NewListener(SetName);
        }

        /// <summary>
        /// Updates the description input field.
        /// </summary>
        /// <param name="marker">Marker to edit.</param>
        public void RenderDescriptionEditor(Marker marker)
        {
            Dialog.DescriptionField.SetTextWithoutNotify(timelineMarkers.Count(x => x.Selected) > 1 ? string.Empty : marker.desc);
            Dialog.DescriptionField.onValueChanged.NewListener(SetDescription);
        }

        /// <summary>
        /// Updates the time editor functions.
        /// </summary>
        /// <param name="marker">Marker to edit.</param>
        public void RenderTime(Marker marker)
        {
            Dialog.TimeField.SetTextWithoutNotify(marker.time.ToString());
            Dialog.TimeField.OnValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    SetTime(num);
            });

            TriggerHelper.AddEventTriggers(Dialog.TimeField.inputField.gameObject, TriggerHelper.ScrollDelta(Dialog.TimeField.inputField));
            TriggerHelper.IncreaseDecreaseButtons(Dialog.TimeField);

            Dialog.TimeField.middleButton.onClick.NewListener(() => Dialog.TimeField.Text = AudioManager.inst.CurrentAudioSource.time.ToString());

            EditorContextMenu.AddContextMenu(Dialog.TimeField.inputField.gameObject,
                new ButtonElement("Snap to BPM", () => Dialog.TimeField.Text = RTEditor.SnapToBPM(marker.time).ToString()));
        }

        /// <summary>
        /// Updates the duration editor functions.
        /// </summary>
        /// <param name="marker">Marker to edit.</param>
        public void RenderDuration(Marker marker)
        {
            Dialog.DurationField.SetTextWithoutNotify(marker.duration.ToString());
            Dialog.DurationField.OnValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    SetDuration(num);
            });

            TriggerHelper.AddEventTriggers(Dialog.DurationField.inputField.gameObject, TriggerHelper.ScrollDelta(Dialog.DurationField.inputField));
            TriggerHelper.IncreaseDecreaseButtons(Dialog.DurationField);

            Dialog.DurationField.middleButton.onClick.NewListener(() => Dialog.DurationField.inputField.text = (AudioManager.inst.CurrentAudioSource.time - marker.time).ToString());

            EditorContextMenu.AddContextMenu(Dialog.DurationField.inputField.gameObject,
                new ButtonElement("Snap to BPM", () => Dialog.DurationField.inputField.text = (RTEditor.SnapToBPM(marker.time + marker.duration) - marker.time).ToString()));
        }

        /// <summary>
        /// Updates the color slots.
        /// </summary>
        /// <param name="marker">Marker to edit.</param>
        public void RenderColors(Marker marker)
        {
            LSHelpers.DeleteChildren(Dialog.ColorsParent);
            Dialog.Colors.Clear();
            int num = 0;
            foreach (var color in MarkerEditor.inst.markerColors)
            {
                int colorIndex = num;
                var gameObject = EditorManager.inst.colorGUI.Duplicate(Dialog.ColorsParent, "marker color");
                gameObject.transform.localScale = Vector3.one;

                var markerColorSelection = gameObject.transform.Find("Image").gameObject;
                markerColorSelection.SetActive(marker.color == colorIndex);
                Dialog.Colors.Add(markerColorSelection);

                var button = gameObject.GetComponent<Button>();
                button.image.color = color;
                button.onClick.NewListener(() =>
                {
                    Debug.Log($"{EditorManager.inst.className}Set Marker {colorIndex}'s color to {colorIndex}");
                    SetColor(colorIndex);
                    UpdateColorSelection();
                });

                EditorContextMenu.AddContextMenu(gameObject,
                    new ButtonElement("Use", () =>
                    {
                        Debug.Log($"{EditorManager.inst.className}Set Marker {colorIndex}'s color to {colorIndex}");
                        SetColor(colorIndex);
                        UpdateColorSelection();
                    }),
                    new ButtonElement("Set as Default", () => EditorConfig.Instance.MarkerDefaultColor.Value = colorIndex),
                    new ButtonElement("Edit Colors", RTSettingEditor.inst.OpenDialog));

                EditorThemeManager.ApplyGraphic(button.image, ThemeGroup.Null, true);
                EditorThemeManager.ApplyGraphic(gameObject.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Background_1);

                num++;
            }
        }

        /// <summary>
        /// Updates the color toggle list.
        /// </summary>
        public void UpdateColorSelection()
        {
            var marker = CurrentMarker.Marker;
            for (int i = 0; i < Dialog.Colors.Count; i++)
                Dialog.Colors[i].SetActive(marker.color == i);
        }

        /// <summary>
        /// Updates the visible editor layers.
        /// </summary>
        public void RenderLayers(Marker marker)
        {
            LSHelpers.DeleteChildren(Dialog.LayersContent);
            int num = 0;
            foreach (var layer in marker.layers)
            {
                int index = num;
                var numberField = EditorPrefabHolder.Instance.NumberInputField.Duplicate(Dialog.LayersContent);
                var numberFieldStorage = numberField.GetComponent<InputFieldStorage>();
                //CoreHelper.Destroy(numberField.GetComponent<EventTrigger>());
                CoreHelper.Destroy(numberFieldStorage.eventTrigger);

                numberFieldStorage.inputField.SetTextWithoutNotify(layer.ToString());
                numberFieldStorage.inputField.onValueChanged.NewListener(_val =>
                {
                    if (int.TryParse(_val, out int num))
                        marker.layers[index] = RTMath.Clamp(num, 0, int.MaxValue);

                    marker.timelineMarker?.Render();
                });
                numberFieldStorage.inputField.onEndEdit.NewListener(_val =>
                {
                    if (RTMath.TryParse(_val, 0f, out float num))
                        marker.layers[index] = RTMath.Clamp((int)num, 0, int.MaxValue);

                    marker.timelineMarker?.Render();
                });

                numberFieldStorage.middleButton.onClick.NewListener(() => numberFieldStorage.inputField.text = EditorTimeline.inst.Layer.ToString());

                TriggerHelper.IncreaseDecreaseButtonsInt(numberFieldStorage);
                TriggerHelper.AddEventTriggers(numberFieldStorage.inputField.gameObject, TriggerHelper.ScrollDeltaInt(numberFieldStorage.inputField, max: int.MaxValue));

                EditorThemeManager.ApplyInputField(numberFieldStorage);

                var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(numberField.transform, "delete");
                var deleteButtonStorage = delete.GetComponent<DeleteButtonStorage>();
                delete.GetComponent<LayoutElement>().ignoreLayout = false;

                deleteButtonStorage.OnClick.NewListener(() =>
                {
                    marker.layers.RemoveAt(index);
                    RenderLayers(marker);
                });

                EditorThemeManager.ApplyDeleteButton(deleteButtonStorage);

                var layout = numberField.GetComponent<HorizontalLayoutGroup>();
                layout.spacing = 4f;

                var image = numberField.AddComponent<Image>();
                EditorThemeManager.ApplyGraphic(image, ThemeGroup.Background_3, true);
                num++;
            }

            var add = EditorPrefabHolder.Instance.CreateAddButton(Dialog.LayersContent);
            add.Text = "Add Layer";
            add.OnClick.NewListener(() =>
            {
                marker.layers.Add(EditorTimeline.inst.Layer);
                RenderLayers(marker);
            });
        }

        /// <summary>
        /// Updates the current annotation editor.
        /// </summary>
        public void RenderAnnotationTool()
        {
            for (int i = 0; i < Dialog.AnnotationToolButtons.Count; i++)
            {
                var index = i;
                var button = Dialog.AnnotationToolButtons[i];
                button.transform.GetChild(1).gameObject.SetActive(index == (int)Settings.tool);
                button.onClick.NewListener(() =>
                {
                    AbortActiveStroke();
                    Settings.tool = (AnnotationTool)index;
                    RenderAnnotationTool();
                });
            }
        }

        /// <summary>
        /// Updates the annotation mirror states.
        /// </summary>
        public void RenderAnnotationMirror()
        {
            Dialog.AnnotationHorizontalMirrorToggle.SetIsOnWithoutNotify(Settings.mirrorDrawingHorizontal);
            Dialog.AnnotationHorizontalMirrorToggle.OnValueChanged.NewListener(_val => Settings.mirrorDrawingHorizontal = _val);
            Dialog.AnnotationVerticalMirrorToggle.SetIsOnWithoutNotify(Settings.mirrorDrawingVertical);
            Dialog.AnnotationVerticalMirrorToggle.OnValueChanged.NewListener(_val => Settings.mirrorDrawingVertical = _val);
        }

        /// <summary>
        /// Updates the annotation color slots.
        /// </summary>
        public void RenderAnnotationColors()
        {
            LSHelpers.DeleteChildren(Dialog.AnnotationColorsParent);
            Dialog.AnnotationColors.Clear();
            int num = 0;
            foreach (var color in MarkerEditor.inst.markerColors)
            {
                int colorIndex = num;
                var gameObject = EditorManager.inst.colorGUI.Duplicate(Dialog.AnnotationColorsParent, "marker color");
                gameObject.transform.localScale = Vector3.one;

                var markerColorSelection = gameObject.transform.Find("Image").gameObject;
                markerColorSelection.SetActive(Settings.color == colorIndex);
                Dialog.AnnotationColors.Add(markerColorSelection);

                var button = gameObject.GetComponent<Button>();
                button.image.color = color;
                button.onClick.NewListener(() =>
                {
                    Settings.color = colorIndex;
                    UpdateAnnotationColorSelection();
                });

                EditorContextMenu.AddContextMenu(gameObject,
                    new ButtonElement("Use", () =>
                    {
                        Settings.color = colorIndex;
                        UpdateAnnotationColorSelection();
                    }),
                    new ButtonElement("Set as Default", () => EditorConfig.Instance.MarkerDefaultColor.Value = colorIndex),
                    new ButtonElement("Edit Colors", RTSettingEditor.inst.OpenDialog));

                EditorThemeManager.ApplyGraphic(button.image, ThemeGroup.Null, true);
                EditorThemeManager.ApplyGraphic(gameObject.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Background_1);

                num++;
            }
        }

        /// <summary>
        /// Updates the annotation color toggle list.
        /// </summary>
        public void UpdateAnnotationColorSelection()
        {
            for (int i = 0; i < Dialog.AnnotationColors.Count; i++)
                Dialog.AnnotationColors[i].SetActive(Settings.color == i);
        }

        /// <summary>
        /// Updates the hex color editor field.
        /// </summary>
        public void RenderAnnotationHexColor()
        {
            Dialog.AnnotationHexColorField.SetTextWithoutNotify(Settings.hexColor);
            Dialog.AnnotationHexColorField.onValueChanged.NewListener(_val => Settings.hexColor = _val);

            EditorContextMenu.AddContextMenu(Dialog.AnnotationHexColorField.gameObject,
                EditorContextMenu.GetEditorColorFunctions(Dialog.AnnotationHexColorField, () => Settings.hexColor));
        }

        /// <summary>
        /// Updates the opacity editor field.
        /// </summary>
        public void RenderAnnotationOpacity()
        {
            Dialog.AnnotationOpacityField.SetTextWithoutNotify(Settings.opacity.ToString());
            Dialog.AnnotationOpacityField.OnValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    Settings.opacity = RTMath.Clamp(num, 0f, 1f);
            });

            TriggerHelper.AddEventTriggers(Dialog.AnnotationOpacityField.inputField.gameObject, TriggerHelper.ScrollDelta(Dialog.AnnotationOpacityField.inputField, max: 1f));
            TriggerHelper.IncreaseDecreaseButtons(Dialog.AnnotationOpacityField, max: 1f);
        }

        /// <summary>
        /// Updates the thickness editor field.
        /// </summary>
        public void RenderAnnotationThickness()
        {
            Dialog.AnnotationThicknessField.SetTextWithoutNotify(Settings.thickness.ToString());
            Dialog.AnnotationThicknessField.OnValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    Settings.thickness = RTMath.Clamp(num, 0.1f, 100f);
            });

            TriggerHelper.AddEventTriggers(Dialog.AnnotationThicknessField.inputField.gameObject, TriggerHelper.ScrollDelta(Dialog.AnnotationThicknessField.inputField, min: 0.1f, max: 100f));
            TriggerHelper.IncreaseDecreaseButtons(Dialog.AnnotationThicknessField, min: 0.1f, max: 100f);
        }

        /// <summary>
        /// Updates the annotation fixed camera toggle.
        /// </summary>
        public void RenderAnnotationFixedCamera()
        {
            Dialog.AnnotationFixedCameraToggle.SetIsOnWithoutNotify(Settings.fixedCamera);
            Dialog.AnnotationFixedCameraToggle.OnValueChanged.NewListener(_val => Settings.fixedCamera = _val);
        }

        /// <summary>
        /// Checks the description of a marker, running specific functions.
        /// </summary>
        /// <param name="marker">Marker to check.</param>
        public void CheckDescription(Marker marker)
        {
            if (string.IsNullOrEmpty(marker.desc))
                return;

            foreach (var markerFunction in markerFunctions)
                if (markerFunction.Auto)
                    RTString.RegexMatches(marker.desc, markerFunction.Regex, markerFunction.Result);
        }

        /// <summary>
        /// Checks the description of a marker, running specific functions.
        /// </summary>
        /// <param name="marker">Marker to check.</param>
        public void RunMarkerFunctions(Marker marker)
        {
            if (string.IsNullOrEmpty(marker.desc))
                return;

            foreach (var markerFunction in markerFunctions)
                RTString.RegexMatches(marker.desc, markerFunction.Regex, markerFunction.Result);
        }

        /// <summary>
        /// Updates the marker editor list.
        /// </summary>
        public void UpdateMarkerList()
        {
            var parent = MarkerEditor.inst.right.Find("markers/list");
            LSHelpers.DeleteChildren(parent);

            //Delete Markers
            {
                var delete = EditorPrefabHolder.Instance.Function1Button.Duplicate(parent, "delete markers");
                var deleteStorage = delete.GetComponent<FunctionButtonStorage>();

                deleteStorage.Text = "Delete Markers";
                deleteStorage.OnClick.NewListener(ClearMarkers);

                var hover = delete.GetComponent<HoverUI>();
                if (hover)
                    Destroy(hover);

                if (delete.GetComponent<HoverTooltip>())
                {
                    var tt = delete.GetComponent<HoverTooltip>();
                    tt.tooltipLangauges.Clear();
                    tt.tooltipLangauges.Add(TooltipHelper.NewTooltip("Delete all markers.", "Clicking this will delete every marker in the level.", new List<string>()));
                }

                EditorThemeManager.ApplyGraphic(deleteStorage.button.image, ThemeGroup.Delete);
                EditorThemeManager.ApplyGraphic(deleteStorage.label, ThemeGroup.Delete_Text);
            }

            int num = 0;
            foreach (var marker in GameData.Current.data.markers)
            {
                if (!RTString.SearchString(MarkerEditor.inst.sortedName, marker.name) && !RTString.SearchString(MarkerEditor.inst.sortedName, marker.desc))
                {
                    num++;
                    if (marker.timelineMarker && marker.timelineMarker.panel)
                        marker.timelineMarker.panel.Clear();
                    continue;
                }

                var index = num;

                var markerButton = marker.timelineMarker.panel;

                var gameObject = MarkerEditor.inst.markerButtonPrefab.Duplicate(parent, marker.name);
                markerButton.GameObject = gameObject;
                markerButton.Name = gameObject.transform.Find("name").GetComponent<Text>();
                markerButton.Time = gameObject.transform.Find("pos").GetComponent<Text>();
                markerButton.Color = gameObject.transform.Find("color").GetComponent<Image>();

                markerButton.RenderName();
                markerButton.RenderTime();
                markerButton.RenderColor();

                markerButton.Button = gameObject.GetComponent<Button>();
                markerButton.Button.onClick.AddListener(() => SetCurrentMarker(timelineMarkers[index], true));

                var contextClickable = gameObject.AddComponent<ContextClickable>();
                contextClickable.onClick = eventData =>
                {
                    if (eventData.button == PointerEventData.InputButton.Right)
                        ShowMarkerContextMenu(timelineMarkers[index]);
                };

                TooltipHelper.AddHoverTooltip(gameObject, $"<#{LSColors.ColorToHex(marker.timelineMarker.Color)}>{marker.name} [ {marker.time} ]</color>", marker.desc, new List<string>());

                EditorThemeManager.ApplyGraphic(markerButton.Button.image, ThemeGroup.List_Button_2_Normal, true);
                EditorThemeManager.ApplyGraphic(markerButton.Color, ThemeGroup.Null, true);
                EditorThemeManager.ApplyGraphic(markerButton.Name, ThemeGroup.List_Button_2_Text);
                EditorThemeManager.ApplyGraphic(markerButton.Time, ThemeGroup.List_Button_2_Text);
                num++;
            }
        }

        #endregion

        #region Rendering

        /// <summary>
        /// Creates a new marker or finds a marker at a specific time and selects it.
        /// </summary>
        /// <param name="time">If a marker is found nearby this time, select that marker. Otherwise, create a new marker with this time.</param>
        public void CreateNewMarker(float time)
        {
            Marker marker;
            if (GameData.Current.data.markers.TryFind(x => time > x.time - 0.01f && time < x.time + 0.01f && (EditorConfig.Instance.ShowMarkersOnAllLayers.Value || x.VisibleOnLayer(EditorTimeline.inst.Layer)), out Marker baseMarker))
                marker = baseMarker;
            else
            {
                marker = new Marker(string.Empty, string.Empty, Mathf.Clamp(EditorConfig.Instance.MarkerDefaultColor.Value, 0, MarkerEditor.inst.markerColors.Count - 1), time);
                GameData.Current.data.markers.Add(marker);
            }

            OrderMarkers();

            if (timelineMarkers.TryFind(x => x.Marker.id == marker.id, out TimelineMarker timelineMarker))
                SetCurrentMarker(timelineMarker);
        }

        /// <summary>
        /// Deletes the marker at an index.
        /// </summary>
        /// <param name="index">Index of the marker to delete.</param>
        public void DeleteMarker(int index)
        {
            GameData.Current.data.markers.RemoveAt(index);
            if (index - 1 >= 0)
                SetCurrentMarker(timelineMarkers[index - 1]);
            else
                RTCheckpointEditor.inst.SetCurrentCheckpoint(0);
            CreateMarkers();
        }

        /// <summary>
        /// Sets the current marker and opens the Marker editor.
        /// </summary>
        /// <param name="index">Index of the timeline marker to set.</param>
        /// <param name="bringTo">If the timeline time should be brought to the timeline marker.</param>
        /// <param name="moveTimeline">If the timeline should be shifted to the marker.</param>
        /// <param name="showDialog">If the Marker editor should open.</param>
        public void SetCurrentMarker(int index, bool bringTo = false, bool moveTimeline = false, bool showDialog = true) => SetCurrentMarker(timelineMarkers[index], bringTo, moveTimeline, showDialog);

        /// <summary>
        /// Sets the current marker and opens the Marker editor.
        /// </summary>
        /// <param name="timelineMarker">Timeline marker to set.</param>
        /// <param name="bringTo">If the timeline time should be brought to the timeline marker.</param>
        /// <param name="moveTimeline">If the timeline should be shifted to the marker.</param>
        /// <param name="showDialog">If the Marker editor should open.</param>
        public void SetCurrentMarker(TimelineMarker timelineMarker, bool bringTo = false, bool moveTimeline = false, bool showDialog = true)
        {
            MarkerEditor.inst.currentMarker = timelineMarker.Index;
            CoreHelper.Log($"Set marker to {timelineMarker.Index}");

            if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
            {
                timelineMarker.Selected = !timelineMarker.Selected;
                if (timelineMarkers.Count(x => x.Selected) < 1)
                    timelineMarker.Selected = true;
            }
            else
            {
                for (int i = 0; i < timelineMarkers.Count; i++)
                    timelineMarkers[i].Selected = false;
                timelineMarker.Selected = true;
            }

            CurrentMarker = timelineMarker;

            if (showDialog)
                OpenDialog(CurrentMarker);

            if (!bringTo)
                return;

            AudioManager.inst.SetMusicTime(Mathf.Clamp(CurrentMarker.Time, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
            AudioManager.inst.CurrentAudioSource.Pause();
            EditorManager.inst.UpdatePlayButton();

            if (moveTimeline)
                EditorTimeline.inst.SetTimelinePosition(AudioManager.inst.CurrentAudioSource.time / AudioManager.inst.CurrentAudioSource.clip.length);
        }

        /// <summary>
        /// Shows the marker context menu for a timeline marker.
        /// </summary>
        /// <param name="timelineMarker">Timeline marker to use.</param>
        public void ShowMarkerContextMenu(TimelineMarker timelineMarker) => EditorContextMenu.inst.ShowContextMenu(
            new ButtonElement("Open", () => SetCurrentMarker(timelineMarker)),
            new ButtonElement("Open & Bring To", () => SetCurrentMarker(timelineMarker, true)),
            new ButtonElement("Select All Markers", () =>
            {
                for (int i = 0; i < timelineMarkers.Count; i++)
                {
                    var timelineMarker = timelineMarkers[i];
                    timelineMarker.Selected = true;
                }
            }),
            new SpacerElement(),
            new ButtonElement("Copy", () =>
            {
                if (!timelineMarker.Marker)
                    return;

                markerCopy = timelineMarker.Marker.Copy();
                EditorManager.inst.DisplayNotification("Copied Marker", 1.5f, EditorManager.NotificationType.Success);
            }),
            new ButtonElement("Copy Selected", CopySelectedMarkers),
            new ButtonElement("Copy All", CopyAllMarkers),
            new ButtonElement("Paste", () =>
            {
                if (!markerCopy)
                {
                    EditorManager.inst.DisplayNotification("No copied Marker yet!", 1.5f, EditorManager.NotificationType.Error);
                    return;
                }

                var marker = markerCopy.Copy();
                marker.time = RTEditor.inst.editorInfo.bpmSnapActive && EditorConfig.Instance.BPMSnapsPasted.Value && EditorConfig.Instance.BPMSnapsMarkers.Value ? RTEditor.SnapToBPM(EditorManager.inst.CurrentAudioPos) : EditorManager.inst.CurrentAudioPos;
                GameData.Current.data.markers.Add(marker);
                CreateMarker(GameData.Current.data.markers.Count - 1);
                OrderMarkers();
                EditorManager.inst.DisplayNotification("Pasted Marker", 1.5f, EditorManager.NotificationType.Success);
            }),
            new ButtonElement("Paste All", PasteMarkers),
            new ButtonElement("Delete", () => DeleteMarker(timelineMarker.Index)),
            new ButtonElement("Delete Selected", () =>
            {
                OrderMarkers();
                var time = AudioManager.inst.CurrentAudioSource.clip.length;
                for (int i = timelineMarkers.Count - 1; i >= 0; i--)
                {
                    var timelineMarker = timelineMarkers[i];
                    if (timelineMarker.Selected)
                    {
                        time = timelineMarker.Time;
                        GameData.Current.data.markers.RemoveAt(timelineMarker.Index);
                    }
                }
                var index = timelineMarkers.FindIndex(x => x.Time < time);
                if (index >= 0)
                    SetCurrentMarker(index);
                else
                    RTCheckpointEditor.inst.SetCurrentCheckpoint(0);
                CreateMarkers();
            }),
            new SpacerElement(),
            new ButtonElement("Start Marker Looping", () =>
            {
                if (!markerLoopBegin && !markerLoopEnd)
                {
                    markerLooping = false;
                    EditorManager.inst.DisplayNotification("Cannot start Marker looping without a start and end Marker set.", 3f, EditorManager.NotificationType.Warning);
                    return;
                }
                    
                if (!markerLoopBegin)
                {
                    markerLooping = false;
                    EditorManager.inst.DisplayNotification("Cannot start Marker looping without a start Marker set.", 3f, EditorManager.NotificationType.Warning);
                    return;
                }
                    
                if (!markerLoopEnd)
                {
                    markerLooping = false;
                    EditorManager.inst.DisplayNotification("Cannot start Marker looping without an end Marker set.", 3f, EditorManager.NotificationType.Warning);
                    return;
                }

                markerLooping = true;
                EditorManager.inst.DisplayNotification("Started Marker looping!", 1.5f, EditorManager.NotificationType.Success);
            }),
            new ButtonElement("Stop Marker Looping", () => markerLooping = false),
            new ButtonElement("Set Begin Loop", () =>
            {
                if (markerLoopEnd && markerLoopEnd.Marker && timelineMarker.Marker && markerLoopEnd.Marker.id == timelineMarker.Marker.id)
                {
                    EditorManager.inst.DisplayNotification("Cannot set this Marker as the start of the Marker loop as it is also the end.", 3f, EditorManager.NotificationType.Warning);
                    return;
                }

                markerLoopBegin?.RenderFlags(false, false);
                markerLoopBegin = timelineMarker;
                markerLoopBegin.RenderFlags(true, false);

                if (markerLoopBegin && markerLoopEnd)
                {
                    markerLooping = true;
                    EditorManager.inst.DisplayNotification("Marker set to start of Marker loop and began the loop.", 2f, EditorManager.NotificationType.Success);
                }
                else
                    EditorManager.inst.DisplayNotification("Marker has been set to the start of the Marker loop.", 3f, EditorManager.NotificationType.Success);
            }),
            new ButtonElement("Set End Loop", () =>
            {
                if (markerLoopBegin && markerLoopBegin.Marker && timelineMarker.Marker && markerLoopBegin.Marker.id == timelineMarker.Marker.id)
                {
                    EditorManager.inst.DisplayNotification("Cannot set this Marker as the end of the Marker loop as it is also the start.", 3f, EditorManager.NotificationType.Warning);
                    return;
                }

                markerLoopEnd?.RenderFlags(false, false);
                markerLoopEnd = timelineMarker;
                markerLoopEnd.RenderFlags(false, true);

                if (markerLoopBegin && markerLoopEnd)
                {
                    markerLooping = true;
                    EditorManager.inst.DisplayNotification("Marker set to end of Marker loop and began the loop.", 2f, EditorManager.NotificationType.Success);
                }
                else
                    EditorManager.inst.DisplayNotification("Marker has been set to the end of the Marker loop.", 3f, EditorManager.NotificationType.Success);
            }),
            new ButtonElement("Clear Marker Loop", () =>
            {
                markerLoopBegin?.RenderFlags(false, false);
                markerLoopBegin = null;
                markerLoopEnd?.RenderFlags(false, false);
                markerLoopEnd = null;
                markerLooping = false;
                EditorManager.inst.DisplayNotification("Stopped and cleared Marker loop.", 3f, EditorManager.NotificationType.Success);
            }),
            new SpacerElement(),
            new ButtonElement("Set to Current Layer", () => ForSelectedMarkers(timelineMarker, timelineMarker =>
            {
                timelineMarker.Marker.layers.Clear();
                timelineMarker.Marker.layers.Add(EditorTimeline.inst.Layer);
            })),
            new ButtonElement("Add Current Layer", () => ForSelectedMarkers(timelineMarker, timelineMarker =>
            {
                if (!timelineMarker.Marker.layers.Contains(EditorTimeline.inst.Layer))
                    timelineMarker.Marker.layers.Add(EditorTimeline.inst.Layer);
            })),
            new ButtonElement("Remove Layers", () => ForSelectedMarkers(timelineMarker, timelineMarker => timelineMarker.Marker.layers.Clear())),
            new SpacerElement(),
            new ButtonElement("Run Functions", () => RunMarkerFunctions(timelineMarker.Marker)),
            new SpacerElement(),
            new ButtonElement("Convert to Planner Note", timelineMarker.ToPlannerNote)
            );

        /// <summary>
        /// Creates a timeline marker for the marker at a specific index.
        /// </summary>
        /// <param name="index">Index of the marker.</param>
        public void CreateMarker(int index)
        {
            var timelineMarker = new TimelineMarker(GameData.Current.data.markers[index]);
            timelineMarker.Init(index);
            timelineMarkers.Add(timelineMarker);
        }

        /// <summary>
        /// Creates all markers.
        /// </summary>
        public void CreateMarkers()
        {
            if (timelineMarkers.Count > 0)
            {
                for (int i = 0; i < timelineMarkers.Count; i++)
                    if (timelineMarkers[i].GameObject)
                        CoreHelper.Destroy(timelineMarkers[i].GameObject);
                timelineMarkers.Clear();
            }

            markerLoopBegin = null;
            markerLoopEnd = null;
            markerLooping = false;

            if (!GameData.Current)
                return;

            int num = 0;
            foreach (var marker in GameData.Current.data.markers)
            {
                int index = num;
                CreateMarker(index);
                num++;
            }

            RenderMarkers();
        }

        /// <summary>
        /// Renders all markers.
        /// </summary>
        public void RenderMarkers()
        {
            if (!GameData.Current || !GameData.Current.data || GameData.Current.data.markers == null)
                return;

            for (int i = 0; i < GameData.Current.data.markers.Count; i++)
            {
                var marker = GameData.Current.data.markers[i];

                if (!marker.timelineMarker)
                    CreateMarker(i);

                marker.timelineMarker.Index = i;
                marker.timelineMarker.Render();
            }

            timelineMarkers = timelineMarkers.OrderBy(x => x.Index).ToList();
        }

        /// <summary>
        /// Deselects all markers.
        /// </summary>
        public void DeselectMarkers()
        {
            for (int i = 0; i < timelineMarkers.Count; i++)
                timelineMarkers[i].Selected = false;
        }

        /// <summary>
        /// Sets the state of dragging timeline markers.
        /// </summary>
        /// <param name="dragging">Dragging state to set.</param>
        public void SetDragging(bool dragging, float timeOffset = 0f)
        {
            this.dragging = dragging;
            for (int i = 0; i < timelineMarkers.Count; i++)
            {
                var timelineMarker = timelineMarkers[i];
                timelineMarker.timeOffset = timelineMarker.Marker.time - timeOffset;
                timelineMarker.dragging = dragging && timelineMarker.Selected;
            }
        }

        /// <summary>
        /// Stops dragging all markers.
        /// </summary>
        public void StopDragging()
        {
            SetDragging(false);
            OrderMarkers();

            if (Dialog.IsCurrent && CurrentMarker)
            {
                RenderLabel(CurrentMarker);
                UpdateMarkerList();
            }
        }

        /// <summary>
        /// Orders the markers by time.
        /// </summary>
        public void OrderMarkers()
        {
            if (!GameData.Current)
                return;

            GameData.Current.data.markers = GameData.Current.data.markers.OrderBy(x => x.time).ToList();

            RenderMarkers();
        }

        /// <summary>
        /// Clears all Markers from the current level.
        /// </summary>
        public void ClearMarkers() => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete ALL markers? (This is irreversible!)", () =>
        {
            EditorManager.inst.DisplayNotification($"Deleted {GameData.Current.data.markers.Count} markers!", 2f, EditorManager.NotificationType.Success);
            GameData.Current.data.markers.Clear();
            UpdateMarkerList();
            CreateMarkers();
            Dialog.Close();
            RTCheckpointEditor.inst.SetCurrentCheckpoint(0);
        });

        /// <summary>
        /// Copies all Markers to the copied Markers list.
        /// </summary>
        public void CopyAllMarkers()
        {
            copiedMarkers.Clear();
            copiedMarkers.AddRange(GameData.Current.data.markers.Select(x => x.Copy()));
            EditorManager.inst.DisplayNotification("Copied Markers", 1.5f, EditorManager.NotificationType.Success);
        }

        /// <summary>
        /// Copies selected Markers onto the copied Markers list.
        /// </summary>
        public void CopySelectedMarkers()
        {
            copiedMarkers.Clear();
            copiedMarkers.AddRange(timelineMarkers.Where(x => x.Selected && x.Marker).Select(x => x.Marker.Copy()));
            EditorManager.inst.DisplayNotification("Copied Markers", 1.5f, EditorManager.NotificationType.Success);
        }

        /// <summary>
        /// Pastes all copied Markers to the level.
        /// </summary>
        public void PasteMarkers()
        {
            if (copiedMarkers.IsEmpty())
            {
                EditorManager.inst.DisplayNotification("No copied Marker yet!", 1.5f, EditorManager.NotificationType.Error);
                return;
            }

            GameData.Current.data.markers.AddRange(copiedMarkers.Select(x =>
            {
                var copy = x.Copy();

                if (RTEditor.inst.editorInfo.bpmSnapActive && EditorConfig.Instance.BPMSnapsPasted.Value)
                    copy.time = RTEditor.SnapToBPM(copy.time);

                return copy;
            }));

            CreateMarkers();
            OrderMarkers();
            EditorManager.inst.DisplayNotification("Pasted Markers", 1.5f, EditorManager.NotificationType.Success);
        }

        /// <summary>
        /// If a specified time is in the active area of a marker.
        /// </summary>
        /// <param name="marker">Marker to check.</param>
        /// <param name="time">Time to compare.</param>
        /// <returns>Returns true if the <paramref name="time"/> is in the active area of the <paramref name="marker"/>, otherwise returns false.</returns>
        public bool IsInMarkerArea(Marker marker, float time) => time >= marker.time && time < marker.time + marker.duration ||
            (EditorConfig.Instance.ShowCurrentMarkerAnnotation.Value && CurrentMarker && CurrentMarker.Marker == marker);

        /// <summary>
        /// Gets a color from the marker colors list.
        /// </summary>
        /// <param name="colorSlot">Color slot to get.</param>
        /// <returns>Returns a color from the marker colors list.</returns>
        public Color GetColor(int colorSlot) => MarkerEditor.inst.markerColors.TryGetAt(colorSlot, out Color color) ? color : RTColors.errorColor;

        #endregion

        #region Update Values

        /// <summary>
        /// Sets the current selected markers' name and updates it.
        /// </summary>
        /// <param name="name">Name to set to the marker.</param>
        public void SetName(string name)
        {
            ForSelectedMarkers(timelineMarker =>
            {
                timelineMarker.Name = name;
                timelineMarker.RenderName();
                timelineMarker.RenderTooltip();
            });
            UpdateMarkerList();
        }

        /// <summary>
        /// Sets the current selected markers' description and updates it.
        /// </summary>
        /// <param name="desc">Description to set to the marker.</param>
        public void SetDescription(string desc) => ForSelectedMarkers(timelineMarker =>
        {
            timelineMarker.Description = desc;
            timelineMarker.RenderTooltip();
        });

        /// <summary>
        /// Sets the current selected markers' time and updates it.
        /// </summary>
        /// <param name="time">Time to set to the marker.</param>
        public void SetTime(float time)
        {
            CurrentMarker.Time = time;
            if (CurrentMarker.panel && CurrentMarker.panel.Time)
                CurrentMarker.panel.RenderTime();
            OrderMarkers();
        }

        /// <summary>
        /// Sets the current selected markers' duration and updates it.
        /// </summary>
        /// <param name="duration">Duration to set to the marker.</param>
        public void SetDuration(float duration)
        {
            CurrentMarker.Duration = duration;
        }

        /// <summary>
        /// Sets the current selected markers' color slot and updates it.
        /// </summary>
        /// <param name="color">Color slot to set to the marker.</param>
        public void SetColor(int color)
        {
            ForSelectedMarkers(timelineMarker =>
            {
                timelineMarker.ColorSlot = color;
                timelineMarker.RenderTooltip();
                timelineMarker.RenderColor();
            });
            UpdateMarkerList();
        }

        /// <summary>
        /// Runs an action for each selected marker.
        /// </summary>
        /// <param name="action">Function to run per selected marker.</param>
        public void ForSelectedMarkers(Action<TimelineMarker> action) => ForSelectedMarkers(CurrentMarker, action);

        /// <summary>
        /// Runs an action for each selected marker.
        /// </summary>
        /// <param name="timelineMarker">The single current marker..</param>
        /// <param name="action">Function to run per selected marker.</param>
        public void ForSelectedMarkers(TimelineMarker timelineMarker, Action<TimelineMarker> action)
        {
            var selectedMarkers = timelineMarkers.FindAll(x => x.Selected);
            if (selectedMarkers.Count > 1)
                for (int i = 0; i < selectedMarkers.Count; i++)
                    action?.Invoke(selectedMarkers[i]);
            else if (timelineMarker)
                action?.Invoke(timelineMarker);
        }

        #endregion

        #region Annotation

        // directly based on AnnotationEditor code.
        void AnnotationTick()
        {
            if (!ProjectArrhythmia.State.IsEditing || !Dialog || !Dialog.IsCurrent || EventSystem.current.IsPointerOverGameObject())
                return;

            // don't draw if Example is being dragged.
            if (Companion.Entity.Example.Current && Companion.Entity.Example.Current.Dragging)
                return;

            var tool = Settings.tool;
            if (!CurrentMarker || CurrentMarker.Duration == 0f)
            {
                if (Input.GetMouseButtonDown(0) && tool != AnnotationTool.None)
                    EditorManager.inst.DisplayNotification($"Cannot {tool.ToString().ToLower()} if marker duration is set to 0!", 3f, EditorManager.NotificationType.Warning);
                return;
            }

            var pos = RTLevel.Cameras.FG.ScreenToWorldPoint(Input.mousePosition);

            switch (tool)
            {
                case AnnotationTool.Draw: {
                        if (Settings.fixedCamera)
                        {
                            var camPos = RTLevel.Cameras.FG.transform.position;
                            var rot = RTLevel.Cameras.FG.transform.eulerAngles.z;
                            var zoom = RTLevel.Cameras.FG.orthographicSize / 20f;

                            pos = RTMath.Rotate((pos - camPos) / zoom, -rot);
                        }
                        if (Input.GetMouseButtonDown(0))
                        {
                            BeginStroke(pos);
                            break;
                        }
                        if (Input.GetMouseButtonDown(1))
                        {
                            AbortActiveStroke();
                            break;
                        }
                        if (Input.GetMouseButton(0))
                        {
                            ContinueStroke(pos);
                            break;
                        }
                        if (Input.GetMouseButtonUp(0))
                        {
                            FinalizeStroke(pos);
                            break;
                        }
                        break;
                    }
                case AnnotationTool.Erase: {
                        if (Input.GetMouseButton(0))
                            EraseAtPosition(pos);
                        break;
                    }
                case AnnotationTool.Delete: {
                        if (Input.GetMouseButton(0))
                            DeleteAtPosition(pos);
                        break;
                    }
                case AnnotationTool.Bucket: {
                        if (Input.GetMouseButton(0))
                            BucketAtPosition(pos);
                        break;
                    }
                case AnnotationTool.Move: {
                        if (Input.GetMouseButtonDown(0))
                        {
                            BeginMove(pos);
                            break;
                        }
                        if (Input.GetMouseButtonDown(1))
                        {
                            AbortMovement();
                            break;
                        }
                        if (Input.GetMouseButton(0))
                        {
                            ContinueMove(pos);
                            break;
                        }
                        if (Input.GetMouseButtonUp(0))
                        {
                            FinalizeMove(pos);
                            break;
                        }
                        break;
                    }
            }
        }

        void BeginMove(Vector2 pos)
        {
            startMovePos = pos;
            movingAnnotations = true;
            pointsCache.Clear();
            for (int i = 0; i < CurrentMarker.Marker.annotations.Count; i++)
                pointsCache.Add(new List<Vector2>(CurrentMarker.Marker.annotations[i].points));
        }

        void ContinueMove(Vector2 pos)
        {
            if (!movingAnnotations)
                return;

            var rotate = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var moveTo = startMovePos - pos;
            var angle = RTMath.Angle(Vector2.zero, moveTo);
            for (int i = 0; i < CurrentMarker.Marker.annotations.Count; i++)
            {
                var annotation = CurrentMarker.Marker.annotations[i];
                for (int j = 0; j < annotation.points.Count; j++)
                {
                    var origPoint = pointsCache[i][j];
                    if (rotate)
                    {
                        annotation.points[j] = (Vector2)RTMath.Rotate(origPoint - startMovePos, angle) + startMovePos;
                        continue;
                    }

                    annotation.points[j] = origPoint - moveTo;
                }
            }
        }

        void FinalizeMove(Vector2 pos)
        {
            if (!movingAnnotations)
                return;

            ContinueMove(pos);
            movingAnnotations = false;
            pointsCache.Clear();
        }

        void AbortMovement()
        {
            if (!movingAnnotations)
                return;

            for (int i = 0; i < CurrentMarker.Marker.annotations.Count; i++)
            {
                var annotation = CurrentMarker.Marker.annotations[i];
                for (int j = 0; j < annotation.points.Count; j++)
                    annotation.points[j] = pointsCache[i][j];
            }

            movingAnnotations = false;
            pointsCache.Clear();
        }

        void BeginStroke(Vector2 pos)
        {
            currentStroke = new Annotation
            {
                color = Settings.color,
                hexColor = Settings.hexColor,
                opacity = Settings.opacity,
                thickness = Settings.thickness,
                fixedCamera = Settings.fixedCamera,
            };
            currentStroke.points.Add(pos);
            drawingAnnotation = true;

            if (Settings.mirrorDrawingHorizontal)
            {
                mirrorHorizontalStroke = new Annotation
                {
                    color = Settings.color,
                    hexColor = Settings.hexColor,
                    opacity = Settings.opacity,
                    thickness = Settings.thickness,
                    fixedCamera = Settings.fixedCamera,
                };
                mirrorHorizontalStroke.points.Add(new Vector2(-pos.x, pos.y));
            }

            if (Settings.mirrorDrawingVertical)
            {
                mirrorVerticalStroke = new Annotation
                {
                    color = Settings.color,
                    hexColor = Settings.hexColor,
                    opacity = Settings.opacity,
                    thickness = Settings.thickness,
                    fixedCamera = Settings.fixedCamera,
                };
                mirrorVerticalStroke.points.Add(new Vector2(pos.x, -pos.y));
            }
        }

        void ContinueStroke(Vector2 pos)
        {
            ContinueStroke(currentStroke, pos);
            if (mirrorHorizontalStroke)
                ContinueStroke(mirrorHorizontalStroke, new Vector2(-pos.x, pos.y));
            if (mirrorVerticalStroke)
                ContinueStroke(mirrorVerticalStroke, new Vector2(pos.x, -pos.y));
        }

        void ContinueStroke(Annotation annotation, Vector2 pos)
        {
            if (!annotation)
                return;

            if (annotation.points.IsEmpty())
            {
                annotation.points.Add(pos);
                return;
            }
            var point = annotation.points.Last();
            if (RTMath.Distance(point, pos) >= 0.0025f)
                annotation.points.Add(pos);
        }

        void FinalizeStroke(Vector2 pos)
        {
            if (!currentStroke)
                return;

            ContinueStroke(pos);
            var count = CurrentMarker.Marker.annotations.Count;
            if (currentStroke.points.Count > 1)
                CurrentMarker.Marker.annotations.Add(currentStroke);
            if (mirrorHorizontalStroke && mirrorHorizontalStroke.points.Count > 1)
                CurrentMarker.Marker.annotations.Add(mirrorHorizontalStroke);
            if (mirrorVerticalStroke && mirrorVerticalStroke.points.Count > 1)
                CurrentMarker.Marker.annotations.Add(mirrorVerticalStroke);
            var cache = currentStroke;
            var horizontalCache = mirrorHorizontalStroke;
            var verticalCache = mirrorVerticalStroke;
            EditorManager.inst.history.Add(new History.Command("Draw Annotation",
                () =>
                {
                    count = CurrentMarker.Marker.annotations.Count;
                    if (cache.points.Count > 1)
                        CurrentMarker.Marker.annotations.Add(cache);
                    if (horizontalCache && horizontalCache.points.Count > 1)
                        CurrentMarker.Marker.annotations.Add(horizontalCache);
                    if (verticalCache && verticalCache.points.Count > 1)
                        CurrentMarker.Marker.annotations.Add(verticalCache);
                },
                () =>
                {
                    CurrentMarker.Marker.annotations.RemoveAt(count);
                    if (horizontalCache)
                        CurrentMarker.Marker.annotations.RemoveAt(count);
                    if (mirrorVerticalStroke)
                        CurrentMarker.Marker.annotations.RemoveAt(count);
                }));
            currentStroke = null;
            mirrorHorizontalStroke = null;
            mirrorVerticalStroke = null;
            drawingAnnotation = false;
        }

        void EraseAtPosition(Vector2 pos)
        {
            var rSq = eraserRadius * eraserRadius;

            var time = AudioManager.inst.CurrentAudioSource.time;
            for (int i = 0; i < GameData.Current.data.markers.Count; i++)
            {
                var marker = GameData.Current.data.markers[i];
                if (!IsInMarkerArea(marker, time))
                    continue;

                for (int j = marker.annotations.Count - 1; j >= 0; j--)
                {
                    var annotationIndex = j;
                    var annotation = marker.annotations[j];
                    bool shouldRemove = false;
                    int pointIndex = 0;
                    for (int p = 0; p < annotation.points.Count; p++)
                    {
                        var point = annotation.points[p];
                        if (annotation.fixedCamera)
                        {
                            var camPos = RTLevel.Cameras.FG.transform.position;
                            var rot = RTLevel.Cameras.FG.transform.eulerAngles.z;
                            var zoom = RTLevel.Cameras.FG.orthographicSize / 20f;

                            point = RTMath.Move(RTMath.Rotate(point * zoom, rot), camPos);
                        }
                        if (RTMath.Distance(point, pos) <= rSq)
                        {
                            shouldRemove = true;
                            pointIndex = p;
                        }
                    }
                    if (!shouldRemove)
                        continue;

                    var cache = annotation.Copy();
                    var annotationCopy = annotation.Copy();
                    annotation.points.RemoveRange(pointIndex, annotationCopy.points.Count - pointIndex);
                    annotationCopy.points.RemoveRange(0, pointIndex);
                    if (!annotationCopy.points.IsEmpty())
                        marker.annotations.Insert(annotationIndex + 1, annotationCopy);
                    if (annotation.points.IsEmpty())
                        marker.annotations.RemoveAt(annotationIndex);
                    EditorManager.inst.history.Add(new History.Command("Delete Annotation",
                        () =>
                        {
                            if (!annotationCopy.points.IsEmpty())
                                marker.annotations.Insert(annotationIndex + 1, annotationCopy);
                            if (annotation.points.IsEmpty())
                                marker.annotations[annotationIndex] = annotation;
                        },
                        () =>
                        {
                            if (!annotationCopy.points.IsEmpty())
                                marker.annotations.RemoveAt(annotationIndex + 1);
                            if (annotation.points.IsEmpty())
                                marker.annotations[annotationIndex] = cache;
                        }));
                }
            }
        }

        void DeleteAtPosition(Vector2 pos)
        {
            var rSq = eraserRadius * eraserRadius;

            var time = AudioManager.inst.CurrentAudioSource.time;
            for (int i = 0; i < GameData.Current.data.markers.Count; i++)
            {
                var marker = GameData.Current.data.markers[i];
                if (!IsInMarkerArea(marker, time))
                    continue;

                for (int j = marker.annotations.Count - 1; j >= 0; j--)
                {
                    var annotationIndex = j;
                    var annotation = marker.annotations[j];
                    bool shouldRemove = false;
                    for (int p = 0; p < annotation.points.Count; p++)
                    {
                        var point = annotation.points[p];
                        if (annotation.fixedCamera)
                        {
                            var camPos = RTLevel.Cameras.FG.transform.position;
                            var rot = RTLevel.Cameras.FG.transform.eulerAngles.z;
                            var zoom = RTLevel.Cameras.FG.orthographicSize / 20f;

                            point = RTMath.Move(RTMath.Rotate(point * zoom, rot), camPos);
                        }
                        if (RTMath.Distance(point, pos) <= rSq)
                            shouldRemove = true;
                    }
                    if (!shouldRemove)
                        continue;

                    marker.annotations.RemoveAt(j);
                    EditorManager.inst.history.Add(new History.Command("Delete Annotation",
                        () =>
                        {
                            marker.annotations.RemoveAt(annotationIndex);
                        },
                        () =>
                        {
                            marker.annotations.Insert(annotationIndex, annotation);
                        }));
                }
            }
        }

        void BucketAtPosition(Vector2 pos)
        {
            var rSq = bucketRadius * bucketRadius;

            var time = AudioManager.inst.CurrentAudioSource.time;
            for (int i = 0; i < GameData.Current.data.markers.Count; i++)
            {
                var marker = GameData.Current.data.markers[i];
                if (!IsInMarkerArea(marker, time))
                    continue;

                for (int j = 0; j < marker.annotations.Count; j++)
                {
                    var annotation = marker.annotations[j];
                    var shouldBucket = false;
                    for (int p = 0; p < annotation.points.Count; p++)
                    {
                        var point = annotation.points[p];
                        if (annotation.fixedCamera)
                        {
                            var camPos = RTLevel.Cameras.FG.transform.position;
                            var rot = RTLevel.Cameras.FG.transform.eulerAngles.z;
                            var zoom = RTLevel.Cameras.FG.orthographicSize / 20f;

                            point = RTMath.Move(RTMath.Rotate(point * zoom, rot), camPos);
                        }

                        if (RTMath.Distance(point, pos) <= rSq)
                            shouldBucket = true;
                    }
                    if (shouldBucket)
                    {
                        annotation.color = Settings.color;
                        annotation.hexColor = Settings.hexColor;
                        annotation.opacity = Settings.opacity;
                        annotation.fixedCamera = Settings.fixedCamera;
                    }
                }
            }
        }

        void AbortActiveStroke()
        {
            currentStroke = null;
            drawingAnnotation = false;
        }

        /// <summary>
        /// Flips all annotations along a direction.
        /// </summary>
        /// <param name="direction">Direction to flip.</param>
        public void FlipAnnotations(Direction direction)
        {
            switch (direction)
            {
                case Direction.Horizontal: {
                        for (int i = 0; i < CurrentMarker.Marker.annotations.Count; i++)
                        {
                            var annotation = CurrentMarker.Marker.annotations[i];
                            for (int j = 0; j < annotation.points.Count; j++)
                                annotation.points[j] = new Vector2(-annotation.points[j].x, annotation.points[j].y);
                        }
                        break;
                    }
                case Direction.Vertical: {
                        for (int i = 0; i < CurrentMarker.Marker.annotations.Count; i++)
                        {
                            var annotation = CurrentMarker.Marker.annotations[i];
                            for (int j = 0; j < annotation.points.Count; j++)
                                annotation.points[j] = new Vector2(annotation.points[j].x, -annotation.points[j].y);
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Copies all annotations from the current marker.
        /// </summary>
        public void CopyAnnotations()
        {
            if (!CurrentMarker || !CurrentMarker.Marker)
            {
                EditorManager.inst.DisplayNotification("No marker selected.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            copiedAnnotations = new List<Annotation>(CurrentMarker.Marker.annotations);
            EditorManager.inst.DisplayNotification("Copied all annotations from the current marker!", 2f, EditorManager.NotificationType.Success);
        }

        /// <summary>
        /// Pastes all copied annotations onto the current marker.
        /// </summary>
        public void PasteAnnotations()
        {
            if (copiedAnnotations.IsEmpty())
            {
                EditorManager.inst.DisplayNotification("No annotations copied yet.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            CurrentMarker.Marker.annotations.AddRange(copiedAnnotations.Select(x => x.Copy()));
            EditorManager.inst.DisplayNotification("Pasted all annotations onto the current marker!", 2f, EditorManager.NotificationType.Success);
        }

        /// <summary>
        /// Clears all annotations from the current marker.
        /// </summary>
        public void ClearMarkerAnnotations() => RTEditor.inst.ShowWarningPopup("Are you sure you want to remove all annotations from the marker?",
            () =>
            {
                if (!CurrentMarker || !CurrentMarker.Marker)
                    return;
                CurrentMarker.Marker.annotations.Clear();
            });

        #endregion

        #endregion
    }
}
