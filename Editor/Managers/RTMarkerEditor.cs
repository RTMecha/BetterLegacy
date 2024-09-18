using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BetterLegacy.Components;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using LSFunctions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using BaseMarker = DataManager.GameData.BeatmapData.Marker;

namespace BetterLegacy.Editor.Managers
{
    public class RTMarkerEditor : MonoBehaviour
    {
        public static RTMarkerEditor inst;

        public List<TimelineMarker> timelineMarkers = new List<TimelineMarker>();

        public TimelineMarker CurrentMarker { get; set; }

        public List<BaseMarker> Markers => GameData.Current.beatmapData.markers;

        public static void Init() => MarkerEditor.inst.gameObject.AddComponent<RTMarkerEditor>();

        void Awake()
        {
            inst = this;
            StartCoroutine(SetupUI());
        }

        void Update()
        {
            if (Input.GetMouseButtonUp(2))
                StopDragging();

            for (int i = 0; i < timelineMarkers.Count; i++)
            {
                if (!timelineMarkers[i].dragging)
                    continue;

                timelineMarkers[i].Marker.time = Mathf.Round(Mathf.Clamp(EditorManager.inst.GetTimelineTime(),
                0f, AudioManager.inst.CurrentAudioSource.clip.length) * 1000f) / 1000f;
                RenderMarker(timelineMarkers[i]);
            }

            var config = EditorConfig.Instance;

            if (!config.MarkerLoopActive.Value || DataManager.inst.gameData.beatmapData.markers.Count <= 0)
                return;

            int markerStart = config.MarkerLoopBegin.Value;
            int markerEnd = config.MarkerLoopEnd.Value;

            if (markerStart < 0)
                markerStart = 0;
            if (markerStart > DataManager.inst.gameData.beatmapData.markers.Count - 1)
                markerStart = DataManager.inst.gameData.beatmapData.markers.Count - 1;

            if (markerEnd < 0)
                markerEnd = 0;
            if (markerEnd > DataManager.inst.gameData.beatmapData.markers.Count - 1)
                markerEnd = DataManager.inst.gameData.beatmapData.markers.Count - 1;

            if (AudioManager.inst.CurrentAudioSource.time > DataManager.inst.gameData.beatmapData.markers[markerEnd].time)
                AudioManager.inst.SetMusicTime(DataManager.inst.gameData.beatmapData.markers[markerStart].time);
        }

        public IEnumerator SetupUI()
        {
            var dialog = EditorManager.inst.GetDialog("Marker Editor").Dialog;

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

        public void OpenDialog(TimelineMarker timelineMarker)
        {
            EditorManager.inst.ClearDialogs();
            EditorManager.inst.ShowDialog("Marker Editor");

            UpdateMarkerList();
            RenderMarkers();

            MarkerEditor.inst.left.Find("color").GetComponent<GridLayoutGroup>().spacing = new Vector2(8f, 8f);
            MarkerEditor.inst.left.Find("index/text").GetComponent<Text>().text = $"Index: {timelineMarker.Index} ID: {timelineMarker.Marker.id}";

            var marker = timelineMarker.Marker;

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

            LSHelpers.DeleteChildren(MarkerEditor.inst.left.Find("color"), false);
            int num = 0;
            foreach (var color in MarkerEditor.inst.markerColors)
            {
                int colorIndex = num;
                var gameObject = EditorManager.inst.colorGUI.Duplicate(MarkerEditor.inst.left.Find("color"), "marker color");
                gameObject.transform.localScale = Vector3.one;
                gameObject.transform.Find("Image").gameObject.SetActive(marker.color == colorIndex);
                var button = gameObject.GetComponent<Button>();
                button.image.color = color;
                button.onClick.ClearAll();
                button.onClick.AddListener(() =>
                {
                    Debug.Log($"{EditorManager.inst.className}Set Marker {colorIndex}'s color to {colorIndex}");
                    SetColor(colorIndex);
                    UpdateColorSelection();
                });

                EditorThemeManager.ApplyGraphic(button.image, ThemeGroup.Null, true);
                EditorThemeManager.ApplyGraphic(gameObject.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Background_1);

                num++;
            }

            var name = MarkerEditor.inst.left.Find("name").GetComponent<InputField>();
            name.onValueChanged.ClearAll();
            name.text = marker.name.ToString();
            name.onValueChanged.AddListener(val => { SetName(val); });

            var desc = MarkerEditor.inst.left.Find("desc").GetComponent<InputField>();
            desc.onValueChanged.ClearAll();
            desc.text = marker.desc.ToString();
            desc.onValueChanged.AddListener(val => { SetDescription(val); });

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
            set.onClick.AddListener(() => { time.text = AudioManager.inst.CurrentAudioSource.time.ToString(); });

            var snapBPM = MarkerEditor.inst.left.Find("snap bpm").GetComponent<Button>();
            snapBPM.onClick.ClearAll();
            snapBPM.onClick.AddListener(() => { time.text = RTEditor.SnapToBPM(marker.time).ToString(); });

            var convertToNote = MarkerEditor.inst.left.Find("convert to note").GetComponent<Button>();
            convertToNote.onClick.ClearAll();
            convertToNote.onClick.AddListener(() =>
            {
                var note = new ProjectPlannerManager.NoteItem
                {
                    Active = true,
                    Name = marker.name,
                    Color = marker.color,
                    Position = new Vector2(Screen.width / 2, Screen.height / 2),
                    Text = marker.desc,
                };
                ProjectPlannerManager.inst.planners.Add(note);
                ProjectPlannerManager.inst.GenerateNote(note);

                ProjectPlannerManager.inst.SaveNotes();
            });
        }

        public void CreateNewMarker(float time)
        {
            Marker marker;
            if (!Markers.Has(x => time > x.time - 0.01f && time < x.time + 0.01f))
            {
                Markers.Add(new Marker
                {
                    time = time,
                    name = "",
                    color = Mathf.Clamp(EditorConfig.Instance.MarkerDefaultColor.Value, 0, MarkerEditor.inst.markerColors.Count - 1),
                });

                marker = (Marker)Markers[Markers.Count - 1];
            }
            else
                marker = (Marker)Markers.Find(x => time > x.time - 0.01f && time < x.time + 0.01f);

            OrderMarkers();

            if (timelineMarkers.TryFind(x => x.Marker.id == marker.id, out TimelineMarker timelineMarker))
                SetCurrentMarker(timelineMarker);
        }

        public void DeleteMarker(int index)
        {
            Markers.RemoveAt(index);
            if (index - 1 >= 0)
                SetCurrentMarker(timelineMarkers[index - 1]);
            else
                CheckpointEditor.inst.SetCurrentCheckpoint(0);
            CreateMarkers();
        }

        public void UpdateColorSelection()
        {
            var marker = CurrentMarker.Marker;
            int num = 0;
            foreach (var color in MarkerEditor.inst.markerColors)
            {
                MarkerEditor.inst.left.Find("color").GetChild(num).Find("Image").gameObject.SetActive(marker.color == num);
                num++;
            }
        }

        public void SetCurrentMarker(TimelineMarker timelineMarker, bool bringTo = false, bool moveTimeline = false, bool showDialog = true)
        {
            DataManager.inst.UpdateSettingInt("EditorMarker", timelineMarker.Index);
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
            foreach (var marker in GameData.Current.beatmapData.markers)
            {
                if (!CoreHelper.SearchString(MarkerEditor.inst.sortedName, marker.name) && !CoreHelper.SearchString(MarkerEditor.inst.sortedName, marker.desc))
                {
                    num++;
                    continue;
                }

                var index = num;
                var gameObject = MarkerEditor.inst.markerButtonPrefab.Duplicate(parent, marker.name);

                var name = gameObject.transform.Find("name").GetComponent<Text>();
                var pos = gameObject.transform.Find("pos").GetComponent<Text>();
                var image = gameObject.transform.Find("color").GetComponent<Image>();

                name.text = marker.name;
                pos.text = string.Format("{0:0}:{1:00}.{2:000}", Mathf.Floor(marker.time / 60f), Mathf.Floor(marker.time % 60f), Mathf.Floor(marker.time * 1000f % 1000f));

                var markerColor = MarkerEditor.inst.markerColors[Mathf.Clamp(marker.color, 0, MarkerEditor.inst.markerColors.Count - 1)];
                image.color = markerColor;
                var button = gameObject.GetComponent<Button>();
                button.onClick.AddListener(() => { SetCurrentMarker(timelineMarkers[index], true); });

                TooltipHelper.AddHoverTooltip(gameObject, "<#" + LSColors.ColorToHex(markerColor) + ">" + marker.name + " [ " + marker.time + " ]</color>", marker.desc, new List<string>());

                EditorThemeManager.ApplyGraphic(button.image, ThemeGroup.List_Button_2_Normal, true);
                EditorThemeManager.ApplyGraphic(image, ThemeGroup.Null, true);
                EditorThemeManager.ApplyGraphic(name, ThemeGroup.List_Button_2_Text);
                EditorThemeManager.ApplyGraphic(pos, ThemeGroup.List_Button_2_Text);

                num++;
            }
        }

        public void CreateMarker(int index)
        {
            var marker = GameData.Current.beatmapData.markers[index];
            var gameObject = MarkerEditor.inst.markerPrefab.Duplicate(EditorManager.inst.markerTimeline.transform, $"Marker {index + 1}");
            gameObject.SetActive(true);
            gameObject.transform.localScale = Vector3.one;

            var timelineMarker = new TimelineMarker
            {
                Index = index,
                Marker = (Marker)marker,
                GameObject = gameObject,
                RectTransform = gameObject.transform.AsRT(),
                Handle = gameObject.GetComponent<Image>(),
                Line = gameObject.transform.Find("line").GetComponent<Image>(),
                Text = gameObject.GetComponentInChildren<Text>(),
            };

            TriggerHelper.AddEventTriggers(gameObject, TriggerHelper.CreateEntry(EventTriggerType.PointerClick, eventData =>
            {
                var pointerEventData = (PointerEventData)eventData;

                if (pointerEventData.button == PointerEventData.InputButton.Left)
                {
                    SetCurrentMarker(timelineMarker);
                    AudioManager.inst.SetMusicTimeWithDelay(Mathf.Clamp(timelineMarker.Marker.time, 0f, AudioManager.inst.CurrentAudioSource.clip.length), 0.05f);
                }

                if (pointerEventData.button == PointerEventData.InputButton.Right)
                    DeleteMarker(timelineMarker.Index);
            }), TriggerHelper.CreateEntry(EventTriggerType.BeginDrag, eventData =>
            {
                var pointerEventData = (PointerEventData)eventData;

                if (pointerEventData.button == PointerEventData.InputButton.Middle)
                {
                    CoreHelper.Log($"Started dragging marker {index}");
                    timelineMarker.dragging = true;
                }
            }));

            timelineMarkers.Add(timelineMarker);
        }

        public void CreateMarkers()
        {
            if (timelineMarkers.Count > 0)
            {
                for (int i = 0; i < timelineMarkers.Count; i++)
                    Destroy(timelineMarkers[i].GameObject);

                timelineMarkers.Clear();
            }

            int num = 0;
            foreach (var marker in DataManager.inst.gameData.beatmapData.markers)
            {
                int index = num;
                CreateMarker(index);
                num++;
            }

            RenderMarkers();
        }

        public void RenderMarker(TimelineMarker timelineMarker)
        {
            float time = timelineMarker.Marker.time;
            var markerColor = MarkerEditor.inst.markerColors[Mathf.Clamp(timelineMarker.Marker.color, 0, MarkerEditor.inst.markerColors.Count - 1)];

            var hoverTooltip = timelineMarker.GameObject.GetComponent<HoverTooltip>();
            if (hoverTooltip)
            {
                hoverTooltip.tooltipLangauges.Clear();
                hoverTooltip.tooltipLangauges.Add(TooltipHelper.NewTooltip("<#" + LSColors.ColorToHex(markerColor) + ">" + timelineMarker.Marker.name + " [ " + timelineMarker.Marker.time + " ]</color>", timelineMarker.Marker.desc, new List<string>()));
            }

            timelineMarker.RectTransform.sizeDelta = new Vector2(12f, 12f);
            timelineMarker.RectTransform.anchoredPosition = new Vector2(time * EditorManager.inst.Zoom - 6f, -12f);
            timelineMarker.Handle.color = markerColor;

            timelineMarker.Text.text = timelineMarker.Marker.name;
            EditorThemeManager.ApplyLightText(timelineMarker.Text);
            timelineMarker.Text.transform.AsRT().sizeDelta = new Vector2(EditorConfig.Instance.MarkerTextWidth.Value, 20f);
            timelineMarker.GameObject.SetActive(true);

            timelineMarker.Line.color = EditorConfig.Instance.MarkerLineColor.Value;
            timelineMarker.Line.rectTransform.sizeDelta = new Vector2(EditorConfig.Instance.MarkerLineWidth.Value, 301f);
        }

        public void RenderMarkers()
        {
            if (GameData.IsValid && DataManager.inst.gameData.beatmapData != null && DataManager.inst.gameData.beatmapData.markers != null)
            {
                for (int i = 0; i < DataManager.inst.gameData.beatmapData.markers.Count; i++)
                {
                    var marker = (Marker)DataManager.inst.gameData.beatmapData.markers[i];
                    if (timelineMarkers.TryFind(x => x.Marker != null && x.Marker.id == marker.id, out TimelineMarker timelineMarker))
                    {
                        timelineMarker.Index = i;
                        RenderMarker(timelineMarker);
                    }
                    else
                        CreateMarker(i);
                }

                timelineMarkers = timelineMarkers.OrderBy(x => x.Index).ToList();
            }
        }

        public void SetName(string name)
        {
            CurrentMarker.Marker.name = name;
            UpdateMarkerList();
            RenderMarker(CurrentMarker);
        }

        public void SetDescription(string desc)
        {
            CurrentMarker.Marker.desc = desc;
        }

        public void SetTime(float time)
        {
            CurrentMarker.Marker.time = time;
            UpdateMarkerList();
            RenderMarker(CurrentMarker);
        }

        public void SetColor(int color)
        {
            CurrentMarker.Marker.color = color;
            UpdateMarkerList();
            RenderMarker(CurrentMarker);
        }

        public void StopDragging()
        {
            //CoreHelper.Log($"Stopped dragging.");
            for (int i = 0; i < timelineMarkers.Count; i++)
                timelineMarkers[i].dragging = false;
        }


        public void OrderMarkers()
        {
            GameData.Current.beatmapData.markers = (from x in GameData.Current.beatmapData.markers
                                                             orderby x.time
                                                             select x).ToList();

            RenderMarkers();
        }
    }
}
