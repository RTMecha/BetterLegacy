using System;
using System.Collections;
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
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Planners;
using BetterLegacy.Editor.Data.Timeline;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// BetterLegacy version of <see cref="MarkerEditor"/>.
    /// </summary>
    public class RTMarkerEditor : MonoBehaviour
    {
        #region Init

        /// <summary>
        /// Initializes <see cref="RTMarkerEditor"/> onto <see cref="MarkerEditor"/>.
        /// </summary>
        public static void Init() => MarkerEditor.inst.gameObject.AddComponent<RTMarkerEditor>();

        /// <summary>
        /// <see cref="RTMarkerEditor"/> global instance reference.
        /// </summary>
        public static RTMarkerEditor inst;

        void Awake()
        {
            inst = this;

            try
            {
                Dialog = new MarkerEditorDialog();
                Dialog.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog
        }

        #endregion

        #region Variables

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

        #endregion

        void Update()
        {
            if (dragging && Input.GetMouseButtonUp((int)EditorConfig.Instance.MarkerDragButton.Value))
                StopDragging();

            for (int i = 0; i < timelineMarkers.Count; i++)
            {
                if (!timelineMarkers[i].dragging)
                    continue;

                timelineMarkers[i].Marker.time = Mathf.Round(Mathf.Clamp(EditorTimeline.inst.GetTimelineTime(RTEditor.inst.editorInfo.bpmSnapActive && EditorConfig.Instance.BPMSnapsMarkers.Value), 0f, AudioManager.inst.CurrentAudioSource.clip.length) * 1000f) / 1000f;
                timelineMarkers[i].RenderPosition();
            }

            if (dragging && CurrentMarker && Dialog.IsCurrent)
                RenderTime(CurrentMarker.Marker);

            if (EditorManager.inst.loading || !markerLooping || GameData.Current.data.markers.Count <= 0 || !markerLoopBegin || !markerLoopEnd)
                return;

            if (AudioManager.inst.CurrentAudioSource.time > markerLoopEnd.Marker.time)
                AudioManager.inst.SetMusicTime(markerLoopBegin.Marker.time);
        }

        #region Editor Rendering

        /// <summary>
        /// Opens the Marker editor.
        /// </summary>
        /// <param name="timelineMarker">The marker to edit.</param>
        public void OpenDialog(TimelineMarker timelineMarker)
        {
            Dialog.Open();

            UpdateMarkerList();
            RenderMarkers();

            var marker = timelineMarker.Marker;

            RenderLabel(timelineMarker);
            RenderColors(marker);
            RenderLayers(marker);
            RenderNameEditor(marker);
            RenderDescriptionEditor(marker);
            RenderTime(marker);

            Dialog.ConvertToPlannerNoteButton.button.onClick.NewListener(() =>
            {
                ProjectPlanner.inst.AddPlanner(new NotePlanner
                {
                    Active = true,
                    Name = marker.name,
                    Color = marker.color,
                    Position = new Vector2(Screen.width / 2, Screen.height / 2),
                    Text = marker.desc,
                });
                ProjectPlanner.inst.SaveNotes();
            });

            CheckDescription(marker);
        }

        /// <summary>
        /// Updates the label of the marker.
        /// </summary>
        /// <param name="timelineMarker">Marker to use.</param>
        public void RenderLabel(TimelineMarker timelineMarker) => MarkerEditor.inst.left.Find("index/text").GetComponent<Text>().text = $"Index: {timelineMarker.Index} ID: {timelineMarker.Marker.id}";

        /// <summary>
        /// Updates the name input field.
        /// </summary>
        /// <param name="marker">Marker to edit.</param>
        public void RenderNameEditor(Marker marker)
        {
            Dialog.NameField.SetTextWithoutNotify(marker.name);
            Dialog.NameField.onValueChanged.NewListener(SetName);
        }

        /// <summary>
        /// Updates the description input field.
        /// </summary>
        /// <param name="marker">Marker to edit.</param>
        public void RenderDescriptionEditor(Marker marker)
        {
            Dialog.DescriptionField.SetTextWithoutNotify(marker.desc);
            Dialog.DescriptionField.onValueChanged.NewListener(SetDescription);
        }

        /// <summary>
        /// Updates the time editor functions.
        /// </summary>
        /// <param name="marker">Marker to edit.</param>
        public void RenderTime(Marker marker)
        {
            Dialog.TimeField.inputField.SetTextWithoutNotify(marker.time.ToString());
            Dialog.TimeField.inputField.onValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    SetTime(num);
            });

            TriggerHelper.AddEventTriggers(Dialog.TimeField.inputField.gameObject, TriggerHelper.ScrollDelta(Dialog.TimeField.inputField));
            TriggerHelper.IncreaseDecreaseButtons(Dialog.TimeField);

            Dialog.TimeField.middleButton.onClick.NewListener(() => Dialog.TimeField.inputField.text = AudioManager.inst.CurrentAudioSource.time.ToString());
            Dialog.SnapBPMButton.button.onClick.NewListener(() => Dialog.TimeField.inputField.text = RTEditor.SnapToBPM(marker.time).ToString());
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

                var contextClickable = gameObject.AddComponent<ContextClickable>();
                contextClickable.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Use", () =>
                        {
                            Debug.Log($"{EditorManager.inst.className}Set Marker {colorIndex}'s color to {colorIndex}");
                            SetColor(colorIndex);
                            UpdateColorSelection();
                        }),
                        new ButtonFunction("Set as Default", () => EditorConfig.Instance.MarkerDefaultColor.Value = colorIndex),
                        new ButtonFunction("Edit Colors", RTSettingEditor.inst.OpenDialog)
                        );
                };

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

                numberFieldStorage.middleButton.onClick.NewListener(() =>
                {
                    numberFieldStorage.inputField.text = EditorTimeline.inst.Layer.ToString();
                });

                TriggerHelper.IncreaseDecreaseButtonsInt(numberFieldStorage);
                TriggerHelper.AddEventTriggers(numberFieldStorage.inputField.gameObject, TriggerHelper.ScrollDeltaInt(numberFieldStorage.inputField, max: int.MaxValue));

                EditorThemeManager.ApplyInputField(numberFieldStorage);

                var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(numberField.transform, "delete");
                var deleteButtonStorage = delete.GetComponent<DeleteButtonStorage>();
                delete.GetComponent<LayoutElement>().ignoreLayout = false;

                deleteButtonStorage.button.onClick.NewListener(() =>
                {
                    marker.layers.RemoveAt(index);
                    RenderLayers(marker);
                });

                EditorThemeManager.ApplyGraphic(deleteButtonStorage.baseImage, ThemeGroup.Delete, true);
                EditorThemeManager.ApplyGraphic(deleteButtonStorage.image, ThemeGroup.Delete_Text);

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

                var deleteText = deleteStorage.label;
                deleteText.text = "Delete Markers";

                var deleteButton = deleteStorage.button;
                deleteButton.onClick.NewListener(ClearMarkers);

                var hover = delete.GetComponent<HoverUI>();
                if (hover)
                    Destroy(hover);

                if (delete.GetComponent<HoverTooltip>())
                {
                    var tt = delete.GetComponent<HoverTooltip>();
                    tt.tooltipLangauges.Clear();
                    tt.tooltipLangauges.Add(TooltipHelper.NewTooltip("Delete all markers.", "Clicking this will delete every marker in the level.", new List<string>()));
                }

                EditorThemeManager.ApplyGraphic(deleteButton.image, ThemeGroup.Delete);
                EditorThemeManager.ApplyGraphic(deleteText, ThemeGroup.Delete_Text);
            }

            int num = 0;
            foreach (var marker in GameData.Current.data.markers)
            {
                if (!RTString.SearchString(MarkerEditor.inst.sortedName, marker.name) && !RTString.SearchString(MarkerEditor.inst.sortedName, marker.desc))
                {
                    num++;
                    if (marker.timelineMarker && marker.timelineMarker.listButton)
                        marker.timelineMarker.listButton.Clear();
                    continue;
                }

                var index = num;

                var markerButton = marker.timelineMarker.listButton;

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
            if (GameData.Current.data.markers.TryFind(x => time > x.time - 0.01f && time < x.time + 0.01f && (x.layers.IsEmpty() || x.layers.Contains(EditorTimeline.inst.Layer)), out Marker baseMarker))
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

            CurrentMarker = timelineMarker;

            if (showDialog)
                OpenDialog(CurrentMarker);

            if (!bringTo)
                return;

            float time = CurrentMarker.Marker.time;
            AudioManager.inst.SetMusicTime(Mathf.Clamp(time, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
            AudioManager.inst.CurrentAudioSource.Pause();
            EditorManager.inst.UpdatePlayButton();

            if (moveTimeline)
                EditorTimeline.inst.SetTimelinePosition(AudioManager.inst.CurrentAudioSource.time / AudioManager.inst.CurrentAudioSource.clip.length);
        }

        /// <summary>
        /// Shows the marker context menu for a timeline marker.
        /// </summary>
        /// <param name="timelineMarker">Timeline marker to use.</param>
        public void ShowMarkerContextMenu(TimelineMarker timelineMarker)
        {
            EditorContextMenu.inst.ShowContextMenu(
                new ButtonFunction("Open", () => SetCurrentMarker(timelineMarker)),
                new ButtonFunction("Open & Bring To", () => SetCurrentMarker(timelineMarker, true)),
                new ButtonFunction(true),
                new ButtonFunction("Copy", () =>
                {
                    if (!timelineMarker.Marker)
                        return;

                    markerCopy = timelineMarker.Marker.Copy();
                    EditorManager.inst.DisplayNotification("Copied Marker", 1.5f, EditorManager.NotificationType.Success);
                }),
                new ButtonFunction("Copy All", CopyAllMarkers),
                new ButtonFunction("Paste", () =>
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
                new ButtonFunction("Paste All", PasteMarkers),
                new ButtonFunction("Delete", () => DeleteMarker(timelineMarker.Index)),
                new ButtonFunction(true),
                new ButtonFunction("Start Marker Looping", () =>
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
                new ButtonFunction("Stop Marker Looping", () => markerLooping = false),
                new ButtonFunction("Set Begin Loop", () =>
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
                new ButtonFunction("Set End Loop", () =>
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
                new ButtonFunction("Clear Marker Loop", () =>
                {
                    markerLoopBegin?.RenderFlags(false, false);
                    markerLoopBegin = null;
                    markerLoopEnd?.RenderFlags(false, false);
                    markerLoopEnd = null;
                    markerLooping = false;
                    EditorManager.inst.DisplayNotification("Stopped and cleared Marker loop.", 3f, EditorManager.NotificationType.Success);
                }),
                new ButtonFunction(true),
                new ButtonFunction("Run Functions", () => RunMarkerFunctions(timelineMarker.Marker))
                );
        }

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
        /// Stops dragging all markers.
        /// </summary>
        public void StopDragging()
        {
            for (int i = 0; i < timelineMarkers.Count; i++)
                timelineMarkers[i].dragging = false;
            dragging = false;

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
            RTEditor.inst.HideWarningPopup();
            Dialog.Close();
            RTCheckpointEditor.inst.SetCurrentCheckpoint(0);
        }, RTEditor.inst.HideWarningPopup);

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

        #endregion

        #region Update Values

        /// <summary>
        /// Sets the current selected markers' name and updates it.
        /// </summary>
        /// <param name="name">Name to set to the marker.</param>
        public void SetName(string name)
        {
            CurrentMarker.Marker.name = name;
            UpdateMarkerList();
            CurrentMarker.RenderName();
            CurrentMarker.RenderTooltip();
        }

        /// <summary>
        /// Sets the current selected markers' description and updates it.
        /// </summary>
        /// <param name="desc">Description to set to the marker.</param>
        public void SetDescription(string desc)
        {
            CurrentMarker.Marker.desc = desc;
            CurrentMarker.RenderTooltip();
        }

        /// <summary>
        /// Sets the current selected markers' time and updates it.
        /// </summary>
        /// <param name="time">Time to set to the marker.</param>
        public void SetTime(float time)
        {
            CurrentMarker.Marker.time = time;
            if (CurrentMarker.listButton && CurrentMarker.listButton.Time)
                CurrentMarker.listButton.RenderTime();
            OrderMarkers();
        }

        /// <summary>
        /// Sets the current selected markers' color slot and updates it.
        /// </summary>
        /// <param name="color">Color slot to set to the marker.</param>
        public void SetColor(int color)
        {
            CurrentMarker.Marker.color = color;
            UpdateMarkerList();

            CurrentMarker.RenderTooltip();
            CurrentMarker.RenderColor();
        }

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
    }
}
