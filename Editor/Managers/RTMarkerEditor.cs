using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Planners;
using LSFunctions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
            StartCoroutine(SetupUI());
        }

        IEnumerator SetupUI()
        {
            var dialog = EditorManager.inst.GetDialog("Marker Editor").Dialog;

            var activeState = dialog.gameObject.AddComponent<ActiveState>();
            activeState.onStateChanged = active => editorOpen = active;

            MarkerEditor.inst.dialog = dialog;
            MarkerEditor.inst.left = dialog.Find("data/left");
            MarkerEditor.inst.right = dialog.Find("data/right");

            var indexparent = new GameObject("index");
            indexparent.transform.SetParent(MarkerEditor.inst.left);
            indexparent.transform.SetSiblingIndex(0);
            var rtindexpr = indexparent.AddComponent<RectTransform>();
            rtindexpr.pivot = new Vector2(0f, 1f);
            rtindexpr.sizeDelta = new Vector2(371f, 32f);

            var index = new GameObject("text");
            index.transform.parent = indexparent.transform;
            var rtindex = index.AddComponent<RectTransform>();
            index.AddComponent<CanvasRenderer>();
            var ttindex = index.AddComponent<Text>();

            rtindex.anchoredPosition = Vector2.zero;
            rtindex.anchorMax = Vector2.one;
            rtindex.anchorMin = Vector2.zero;
            rtindex.pivot = new Vector2(0f, 1f);
            rtindex.sizeDelta = Vector2.zero;

            ttindex.text = "Index: 0";
            ttindex.font = FontManager.inst.DefaultFont;
            ttindex.color = new Color(0.9f, 0.9f, 0.9f);
            ttindex.alignment = TextAnchor.MiddleLeft;
            ttindex.fontSize = 16;
            ttindex.horizontalOverflow = HorizontalWrapMode.Overflow;

            EditorThemeManager.AddLightText(ttindex);

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

            EditorThemeManager.AddInputField(MarkerEditor.inst.left.Find("name").GetComponent<InputField>());
            EditorThemeManager.AddInputField(MarkerEditor.inst.left.Find("desc").GetComponent<InputField>());

            var time = EditorPrefabHolder.Instance.NumberInputField.Duplicate(MarkerEditor.inst.left, "time new", 4);
            Destroy(MarkerEditor.inst.left.Find("time").gameObject);

            var timeStorage = time.GetComponent<InputFieldStorage>();
            EditorThemeManager.AddInputField(timeStorage.inputField);

            EditorThemeManager.AddSelectable(timeStorage.leftGreaterButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(timeStorage.leftButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(timeStorage.middleButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(timeStorage.rightButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(timeStorage.rightGreaterButton, ThemeGroup.Function_2, false);

            time.name = "time";

            // fixes color slot spacing
            MarkerEditor.inst.left.Find("color").GetComponent<GridLayoutGroup>().spacing = new Vector2(8f, 8f);

            if (!EditorPrefabHolder.Instance.Function2Button)
            {
                CoreHelper.LogError("No Function 2 button for some reason.");
                yield break;
            }

            var makeNote = EditorPrefabHolder.Instance.Function2Button.Duplicate(MarkerEditor.inst.left, "convert to note", 8);
            var makeNoteStorage = makeNote.GetComponent<FunctionButtonStorage>();
            makeNoteStorage.text.text = "Convert to Planner Note";
            makeNoteStorage.button.onClick.ClearAll();

            EditorThemeManager.AddSelectable(makeNoteStorage.button, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(makeNoteStorage.text, ThemeGroup.Function_2_Text);

            EditorHelper.SetComplexity(makeNote, Complexity.Advanced);

            var snapToBPM = EditorPrefabHolder.Instance.Function2Button.Duplicate(MarkerEditor.inst.left, "snap bpm", 5);
            var snapToBPMStorage = snapToBPM.GetComponent<FunctionButtonStorage>();
            snapToBPMStorage.text.text = "Snap BPM";
            snapToBPMStorage.button.onClick.ClearAll();

            EditorThemeManager.AddSelectable(snapToBPMStorage.button, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(snapToBPMStorage.text, ThemeGroup.Function_2_Text);

            EditorHelper.SetComplexity(snapToBPM, Complexity.Normal);

            var prefab = MarkerEditor.inst.markerPrefab;
            var prefabCopy = prefab.Duplicate(transform, prefab.name);
            Destroy(prefabCopy.GetComponent<MarkerHelper>());
            MarkerEditor.inst.markerPrefab = prefabCopy;

            var desc = MarkerEditor.inst.left.Find("desc").GetComponent<InputField>();

            while (!KeybindManager.inst || !KeybindManager.inst.editSprite)
                yield return null;

            var button = EditorPrefabHolder.Instance.DeleteButton.Duplicate(desc.transform, "edit");
            var buttonStorage = button.GetComponent<DeleteButtonStorage>();
            buttonStorage.image.sprite = KeybindManager.inst.editSprite;
            EditorThemeManager.ApplySelectable(buttonStorage.button, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(buttonStorage.image, ThemeGroup.Function_2_Text);
            buttonStorage.button.onClick.ClearAll();
            buttonStorage.button.onClick.AddListener(() => { TextEditor.inst.SetInputField(desc); });
            UIManager.SetRectTransform(buttonStorage.baseImage.rectTransform, new Vector2(171f, 51f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(22f, 22f));
            EditorHelper.SetComplexity(button, Complexity.Advanced);

            yield break;
        }

        #endregion

        #region Variables

        /// <summary>
        /// List of timeline markers.
        /// </summary>
        public List<TimelineMarker> timelineMarkers = new List<TimelineMarker>();

        /// <summary>
        /// The current selected marker.
        /// </summary>
        public TimelineMarker CurrentMarker { get; set; }

        /// <summary>
        /// Quick references to the markers list.
        /// </summary>
        public List<Marker> Markers => GameData.Current.beatmapData.markers;

        /// <summary>
        /// Copied marker.
        /// </summary>
        public Marker markerCopy;

        /// <summary>
        /// If a marker is dragging.
        /// </summary>
        public bool dragging;

        /// <summary>
        /// If the Marker editor is open.
        /// </summary>
        public bool editorOpen;

        List<GameObject> markerColors = new List<GameObject>();

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

                timelineMarkers[i].Marker.time = Mathf.Round(Mathf.Clamp(RTEditor.inst.GetTimelineTime(), 0f, AudioManager.inst.CurrentAudioSource.clip.length) * 1000f) / 1000f;
                timelineMarkers[i].RenderPosition();
            }

            if (dragging && CurrentMarker && editorOpen)
                RenderTime(CurrentMarker.Marker);

            if (EditorManager.inst.loading || !markerLooping || GameData.Current.beatmapData.markers.Count <= 0 || !markerLoopBegin || !markerLoopEnd)
                return;

            int markerStart = markerLoopBegin.Index;
            int markerEnd = markerLoopEnd.Index;

            if (markerStart < 0)
                markerStart = 0;
            if (markerStart > GameData.Current.beatmapData.markers.Count - 1)
                markerStart = GameData.Current.beatmapData.markers.Count - 1;

            if (markerEnd < 0)
                markerEnd = 0;
            if (markerEnd > GameData.Current.beatmapData.markers.Count - 1)
                markerEnd = GameData.Current.beatmapData.markers.Count - 1;

            if (AudioManager.inst.CurrentAudioSource.time > GameData.Current.beatmapData.markers[markerEnd].time)
                AudioManager.inst.SetMusicTime(GameData.Current.beatmapData.markers[markerStart].time);
        }

        #region Editor Rendering

        /// <summary>
        /// Opens the Marker editor.
        /// </summary>
        /// <param name="timelineMarker">The marker to edit.</param>
        public void OpenDialog(TimelineMarker timelineMarker)
        {
            EditorManager.inst.ClearDialogs();
            EditorManager.inst.ShowDialog("Marker Editor");

            UpdateMarkerList();
            RenderMarkers();

            var marker = timelineMarker.Marker;

            RenderLabel(timelineMarker);
            RenderColors(marker);
            RenderNameEditor(marker);
            RenderDescriptionEditor(marker);
            RenderTime(marker);

            var convertToNote = MarkerEditor.inst.left.Find("convert to note").GetComponent<Button>();
            convertToNote.onClick.ClearAll();
            convertToNote.onClick.AddListener(() =>
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
            var name = MarkerEditor.inst.left.Find("name").GetComponent<InputField>();
            name.onValueChanged.ClearAll();
            name.text = marker.name.ToString();
            name.onValueChanged.AddListener(SetName);
        }

        /// <summary>
        /// Updates the description input field.
        /// </summary>
        /// <param name="marker">Marker to edit.</param>
        public void RenderDescriptionEditor(Marker marker)
        {
            var desc = MarkerEditor.inst.left.Find("desc").GetComponent<InputField>();
            desc.onValueChanged.ClearAll();
            desc.text = marker.desc.ToString();
            desc.onValueChanged.AddListener(SetDescription);
        }

        /// <summary>
        /// Updates the time editor functions.
        /// </summary>
        /// <param name="marker">Marker to edit.</param>
        public void RenderTime(Marker marker)
        {
            var time = MarkerEditor.inst.left.Find("time/input").GetComponent<InputField>();
            time.onValueChanged.ClearAll();
            time.text = marker.time.ToString();
            time.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    SetTime(num);
            });

            TriggerHelper.AddEventTriggers(time.gameObject, TriggerHelper.ScrollDelta(time));
            TriggerHelper.IncreaseDecreaseButtons(time, t: MarkerEditor.inst.left.Find("time"));

            var set = MarkerEditor.inst.left.Find("time/|").GetComponent<Button>();
            set.onClick.ClearAll();
            set.onClick.AddListener(() => time.text = AudioManager.inst.CurrentAudioSource.time.ToString());

            var snapBPM = MarkerEditor.inst.left.Find("snap bpm").GetComponent<Button>();
            snapBPM.onClick.ClearAll();
            snapBPM.onClick.AddListener(() => time.text = RTEditor.SnapToBPM(marker.time).ToString());
        }

        /// <summary>
        /// Updates the color slots.
        /// </summary>
        /// <param name="marker">Marker to edit.</param>
        public void RenderColors(Marker marker)
        {
            var colorsParent = MarkerEditor.inst.left.Find("color");
            LSHelpers.DeleteChildren(MarkerEditor.inst.left.Find("color"));
            markerColors.Clear();
            int num = 0;
            foreach (var color in MarkerEditor.inst.markerColors)
            {
                int colorIndex = num;
                var gameObject = EditorManager.inst.colorGUI.Duplicate(colorsParent, "marker color");
                gameObject.transform.localScale = Vector3.one;

                var markerColorSelection = gameObject.transform.Find("Image").gameObject;
                markerColorSelection.SetActive(marker.color == colorIndex);
                markerColors.Add(markerColorSelection);

                var button = gameObject.GetComponent<Button>();
                button.image.color = color;
                button.onClick.ClearAll();
                button.onClick.AddListener(() =>
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

                    RTEditor.inst.ShowContextMenu(
                        new ButtonFunction("Use", () =>
                        {
                            Debug.Log($"{EditorManager.inst.className}Set Marker {colorIndex}'s color to {colorIndex}");
                            SetColor(colorIndex);
                            UpdateColorSelection();
                        }),
                        new ButtonFunction("Set as Default", () => { EditorConfig.Instance.MarkerDefaultColor.Value = colorIndex; }),
                        new ButtonFunction("Edit Colors", SettingEditor.inst.Render)
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
            for (int i = 0; i < markerColors.Count; i++)
                markerColors[i].SetActive(marker.color == i);
        }

        /// <summary>
        /// Checks the description of a marker, running specific functions.
        /// </summary>
        /// <param name="marker">Marker to check.</param>
        public void CheckDescription(Marker marker)
        {
            var matchCollection = Regex.Matches(marker.desc, @"setLayer\((.*?)\)");

            if (matchCollection.Count > 0)
                foreach (var obj in matchCollection)
                {
                    var match = (Match)obj;

                    var matchGroup = match.Groups[1].ToString();
                    if (matchGroup.ToLower() == "events" || matchGroup.ToLower() == "check" || matchGroup.ToLower() == "event/check" || matchGroup.ToLower() == "event")
                        RTEditor.inst.SetLayer(RTEditor.LayerType.Events);
                    else if (matchGroup.ToLower() == "object" || matchGroup.ToLower() == "objects")
                        RTEditor.inst.SetLayer(RTEditor.LayerType.Objects);
                    else if (matchGroup.ToLower() == "toggle" || matchGroup.ToLower() == "swap")
                        RTEditor.inst.SetLayer(RTEditor.inst.layerType == RTEditor.LayerType.Objects ? RTEditor.LayerType.Events : RTEditor.LayerType.Objects);
                    else if (int.TryParse(matchGroup, out int layer))
                        RTEditor.inst.SetLayer(Mathf.Clamp(layer - 1, 0, int.MaxValue));
                }
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

                var deleteText = deleteStorage.text;
                deleteText.text = "Delete Markers";

                var deleteButton = deleteStorage.button;
                deleteButton.onClick.ClearAll();
                deleteButton.onClick.AddListener(() =>
                {
                    RTEditor.inst.ShowWarningPopup("Are you sure you want to delete ALL markers? (This is irreversible!)", () =>
                    {
                        EditorManager.inst.DisplayNotification($"Deleted {GameData.Current.beatmapData.markers.Count} markers!", 2f, EditorManager.NotificationType.Success);
                        GameData.Current.beatmapData.markers.Clear();
                        UpdateMarkerList();
                        CreateMarkers();
                        RTEditor.inst.HideWarningPopup();
                        EditorManager.inst.HideDialog("Marker Editor");
                        CheckpointEditor.inst.SetCurrentCheckpoint(0);
                    }, RTEditor.inst.HideWarningPopup);
                });

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
            foreach (var marker in Markers)
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
            if (!Markers.TryFind(x => time > x.time - 0.01f && time < x.time + 0.01f, out Marker baseMarker))
            {
                marker = new Marker("", "", Mathf.Clamp(EditorConfig.Instance.MarkerDefaultColor.Value, 0, MarkerEditor.inst.markerColors.Count - 1), time);
                Markers.Add(marker);
            }
            else
                marker = baseMarker;

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
            Markers.RemoveAt(index);
            if (index - 1 >= 0)
                SetCurrentMarker(timelineMarkers[index - 1]);
            else
                CheckpointEditor.inst.SetCurrentCheckpoint(0);
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
                RTEditor.inst.SetTimeline(EditorManager.inst.zoomFloat, AudioManager.inst.CurrentAudioSource.time / AudioManager.inst.CurrentAudioSource.clip.length);
        }

        /// <summary>
        /// Shows the marker context menu for a timeline marker.
        /// </summary>
        /// <param name="timelineMarker">Timeline marker to use.</param>
        public void ShowMarkerContextMenu(TimelineMarker timelineMarker)
        {
            RTEditor.inst.ShowContextMenu(
                new ButtonFunction("Open", () => SetCurrentMarker(timelineMarker)),
                new ButtonFunction("Open & Bring To", () => SetCurrentMarker(timelineMarker, true)),
                new ButtonFunction(true),
                new ButtonFunction("Copy", () =>
                {
                    markerCopy = Marker.DeepCopy(timelineMarker.Marker);
                    EditorManager.inst.DisplayNotification("Copied Marker", 1.5f, EditorManager.NotificationType.Success);
                }),
                new ButtonFunction("Paste", () =>
                {
                    if (markerCopy == null)
                    {
                        EditorManager.inst.DisplayNotification("No copied Marker yet!", 1.5f, EditorManager.NotificationType.Error);
                        return;
                    }

                    var marker = Marker.DeepCopy(markerCopy);
                    marker.time = SettingEditor.inst.SnapActive ? RTEditor.SnapToBPM(EditorManager.inst.CurrentAudioPos) : EditorManager.inst.CurrentAudioPos;
                    GameData.Current.beatmapData.markers.Add(marker);
                    CreateMarker(GameData.Current.beatmapData.markers.Count - 1);
                    OrderMarkers();
                    EditorManager.inst.DisplayNotification("Pasted Marker", 1.5f, EditorManager.NotificationType.Success);
                }),
                new ButtonFunction("Delete", () => DeleteMarker(timelineMarker.Index)),
                new ButtonFunction(true),
                new ButtonFunction("Start Marker Looping", () => markerLooping = true),
                new ButtonFunction("Stop Marker Looping", () => markerLooping = false),
                new ButtonFunction("Set Begin Loop", () => markerLoopBegin = timelineMarker),
                new ButtonFunction("Set End Loop", () => markerLoopEnd = timelineMarker)
                );
        }

        /// <summary>
        /// Creates a timeline marker for the marker at a specific index.
        /// </summary>
        /// <param name="index">Index of the marker.</param>
        public void CreateMarker(int index)
        {
            var timelineMarker = new TimelineMarker(Markers[index]);
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

            int num = 0;
            foreach (var marker in GameData.Current.beatmapData.markers)
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
            if (!GameData.IsValid || GameData.Current.beatmapData == null || GameData.Current.beatmapData.markers == null)
                return;

            for (int i = 0; i < GameData.Current.beatmapData.markers.Count; i++)
            {
                var marker = GameData.Current.beatmapData.markers[i];

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

            if (editorOpen && CurrentMarker)
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
            GameData.Current.beatmapData.markers = GameData.Current.beatmapData.markers.OrderBy(x => x.time).ToList();

            RenderMarkers();
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
    }
}
