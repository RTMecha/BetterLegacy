using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Timeline
{
    /// <summary>
    /// Represents a marker in the editor.
    /// </summary>
    public class TimelineMarker : Exists, ISelectable
    {
        public TimelineMarker() { }

        public TimelineMarker(Marker marker)
        {
            Marker = marker;
            marker.timelineMarker = this;
            panel = new MarkerPanel(this);
        }

        #region Values

        #region UI

        /// <summary>
        /// The <see cref="GameObject"/> of the timeline marker.
        /// </summary>
        public GameObject GameObject { get; set; }
        /// <summary>
        /// The <see cref="RectTransform"/> of the timeline marker.
        /// </summary>
        public RectTransform RectTransform { get; set; }

        /// <summary>
        /// Text component for displaying the markers' name.
        /// </summary>
        public Text Text { get; set; }
        /// <summary>
        /// Handle of the timeline marker.
        /// </summary>
        public Image Handle { get; set; }
        /// <summary>
        /// Line of the timeline marker.
        /// </summary>
        public Image Line { get; set; }

        /// <summary>
        /// Start flag of the timeline marker.
        /// </summary>
        public Image FlagStart { get; set; }
        /// <summary>
        /// End flag of the timeline marker.
        /// </summary>
        public Image FlagEnd { get; set; }

        /// <summary>
        /// Info tooltip of the timeline marker.
        /// </summary>
        public HoverTooltip HoverTooltip { get; set; }

        #endregion

        #region Data

        /// <summary>
        /// Index of the marker.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// The marker data.
        /// </summary>
        public Marker Marker { get; set; }

        /// <summary>
        /// Name of the marker.
        /// </summary>
        public string Name
        {
            get => Marker.name;
            set
            {
                Marker.name = value;
                RenderName();
                panel?.RenderName();
            }
        }

        /// <summary>
        /// Time of the marker.
        /// </summary>
        public float Time
        {
            get => Marker.time;
            set => Marker.time = value;
        }

        /// <summary>
        /// Description of the marker.
        /// </summary>
        public string Description
        {
            get => Marker.desc;
            set => Marker.desc = value;
        }

        /// <summary>
        /// Color slot of the marker.
        /// </summary>
        public int ColorSlot
        {
            get => Marker.color;
            set
            {
                Marker.color = value;
                RenderColor();
            }
        }

        /// <summary>
        /// The markers' color.
        /// </summary>
        public Color Color => MarkerEditor.inst.markerColors[Mathf.Clamp(Marker.color, 0, MarkerEditor.inst.markerColors.Count - 1)];

        /// <summary>
        /// If the timeline marker is being dragged.
        /// </summary>
        public bool dragging;

        bool selected;
        /// <summary>
        /// If the timeline marker is selected.
        /// </summary>
        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                RenderColor(EditorConfig.Instance.ChangeSelectedMarkerColor.Value && value ? EditorConfig.Instance.MarkerSelectionColor.Value : Color);
            }
        }

        /// <summary>
        /// Marker panel reference.
        /// </summary>
        public MarkerPanel panel;

        public float timeOffset;

        #endregion

        #endregion

        #region Functions

        /// <summary>
        /// Initializes the timeline marker.
        /// </summary>
        /// <param name="index">Index of the marker.</param>
        public void Init(int index)
        {
            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            var marker = Marker;
            gameObject = MarkerEditor.inst.markerPrefab.Duplicate(EditorManager.inst.markerTimeline.transform, $"Marker {index + 1}");
            gameObject.SetActive(EditorConfig.Instance.ShowMarkersOnAllLayers.Value || marker.VisibleOnLayer(EditorTimeline.inst.Layer));
            gameObject.transform.localScale = Vector3.one;
            var markerStorage = gameObject.GetComponent<MarkerStorage>();

            Index = index;
            GameObject = gameObject;
            RectTransform = gameObject.transform.AsRT();
            Handle = markerStorage.handle;
            Line = markerStorage.line;
            Text = markerStorage.label;
            FlagStart = markerStorage.flagStart;
            FlagEnd = markerStorage.flagEnd;
            HoverTooltip = markerStorage.hoverTooltip;

            EditorThemeManager.ApplyLightText(Text);
            EditorThemeManager.ApplyGraphic(FlagStart, ThemeGroup.Add);
            EditorThemeManager.ApplyGraphic(FlagEnd, ThemeGroup.Delete);

            TriggerHelper.AddEventTriggers(gameObject, TriggerHelper.CreateEntry(EventTriggerType.PointerClick, eventData =>
            {
                var pointerEventData = (PointerEventData)eventData;

                switch (pointerEventData.button)
                {
                    case PointerEventData.InputButton.Left: {
                            RTMarkerEditor.inst.SetCurrentMarker(this);
                            AudioManager.inst.SetMusicTimeWithDelay(Mathf.Clamp(Time, 0f, AudioManager.inst.CurrentAudioSource.clip.length), 0.05f);
                            break;
                        }
                    case PointerEventData.InputButton.Right: {
                            if (EditorConfig.Instance.MarkerShowContextMenu.Value)
                            {
                                RTMarkerEditor.inst.ShowMarkerContextMenu(this);
                                break;
                            }
                            RTMarkerEditor.inst.DeleteMarker(Index);
                            break;
                        }
                    case PointerEventData.InputButton.Middle: {
                            if (EditorConfig.Instance.MarkerDragButton.Value == PointerEventData.InputButton.Middle)
                                return;

                            AudioManager.inst.SetMusicTime(Mathf.Clamp(Marker.time, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
                            break;
                        }
                }
            }),
            TriggerHelper.CreateEntry(EventTriggerType.BeginDrag, eventData =>
            {
                var pointerEventData = (PointerEventData)eventData;

                if (pointerEventData.button == EditorConfig.Instance.MarkerDragButton.Value)
                {
                    // if this marker wasn't selected then set it as the current marker.
                    if (!Selected)
                    {
                        RTMarkerEditor.inst.DeselectMarkers();
                        RTMarkerEditor.inst.SetCurrentMarker(this);
                    }

                    CoreHelper.Log($"Started dragging marker {index}");
                    RTMarkerEditor.inst.dragTimeOffset = Time - EditorTimeline.inst.GetTimelineTime(RTEditor.inst.editorInfo.bpmSnapActive && EditorConfig.Instance.BPMSnapsObjects.Value);
                    RTMarkerEditor.inst.SetDragging(true, Time);
                    dragging = true;
                }
            }));
        }

        /// <summary>
        /// Renders the whole timeline marker.
        /// </summary>
        public void Render()
        {
            var markerColor = EditorConfig.Instance.ChangeSelectedMarkerColor.Value && selected ? EditorConfig.Instance.MarkerSelectionColor.Value : Color;

            GameObject.SetActive(EditorConfig.Instance.ShowMarkersOnAllLayers.Value || Marker.VisibleOnLayer(EditorTimeline.inst.Layer));
            RenderPosition();
            RenderTooltip(markerColor);
            RenderName();
            RenderTextWidth();
            RenderColor(markerColor, EditorConfig.Instance.MarkerLineColor.Value);
            RenderLine();
            RenderLineWidth();
        }

        /// <summary>
        /// Renders the whole timeline marker.
        /// </summary>
        /// <param name="name">Name of the marker.</param>
        /// <param name="time">Time of the marker.</param>
        /// <param name="zoom">Timeline zoom.</param>
        /// <param name="handleColor">Color of the timeline markers' handle.</param>
        /// <param name="lineColor">Color of the timeline markers' line.</param>
        public void Render(string name, float time, float zoom, Color handleColor, Color lineColor)
        {
            GameObject.SetActive(true);
            RenderPosition(time, zoom);
            RenderTooltip(handleColor);
            RenderName(name);
            RenderColor(handleColor, lineColor);
            RenderLine();
        }

        /// <summary>
        /// Renders the timeline marker position.
        /// </summary>
        public void RenderPosition() => RenderPosition(Time, EditorManager.inst.Zoom);

        /// <summary>
        /// Renders the timeline marker position.
        /// </summary>
        /// <param name="time">Time position.</param>
        /// <param name="zoom">Timeline zoom.</param>
        /// <param name="offset">Position offset.</param>
        public void RenderPosition(float time, float zoom, float offset = 6f)
        {
            RectTransform.sizeDelta = new Vector2(12f, 12f);
            RectTransform.anchoredPosition = new Vector2(time * zoom - offset, -12f);
        }

        /// <summary>
        /// Updates the tooltip.
        /// </summary>
        public void RenderTooltip() => RenderTooltip(Color);

        /// <summary>
        /// Updates the tooltip.
        /// </summary>
        /// <param name="markerColor">Color to assign to the tooltip label.</param>
        public void RenderTooltip(Color markerColor)
        {
            var hoverTooltip = HoverTooltip;
            if (hoverTooltip)
            {
                hoverTooltip.tooltipLangauges.Clear();
                hoverTooltip.tooltipLangauges.Add(TooltipHelper.NewTooltip($"<#{LSColors.ColorToHex(markerColor)}>{Marker.name} [ {Marker.time} ]</color>", Marker.desc, new List<string>()));
            }
        }

        /// <summary>
        /// Renders the timeline marker name.
        /// </summary>
        public void RenderName() => RenderName(Marker.name);

        /// <summary>
        /// Renders the timeline marker name.
        /// </summary>
        /// <param name="name">Name of the marker.</param>
        public void RenderName(string name)
        {
            if (!Text)
                return;

            Text.gameObject.SetActive(!string.IsNullOrEmpty(name));
            if (!string.IsNullOrEmpty(name))
                Text.text = name;
        }

        /// <summary>
        /// Renders the timeline marker names' width.
        /// </summary>
        public void RenderTextWidth() => RenderTextWidth(EditorConfig.Instance.MarkerTextWidth.Value);

        /// <summary>
        /// Renders the timeline marker names' width.
        /// </summary>
        /// <param name="width">Width of the name text UI.</param>
        public void RenderTextWidth(float width) => Text.transform.AsRT().sizeDelta = new Vector2(width, 20f);

        /// <summary>
        /// Renders the timeline markers' colors.
        /// </summary>
        public void RenderColor() => RenderColor(EditorConfig.Instance.ChangeSelectedMarkerColor.Value && selected ? EditorConfig.Instance.MarkerSelectionColor.Value : Color);

        /// <summary>
        /// Renders the timeline markers' colors.
        /// </summary>
        /// <param name="handleColor">Color of the timeline markers' handle.</param>
        public void RenderColor(Color handleColor) => RenderColor(handleColor, EditorConfig.Instance.MarkerLineColor.Value);

        /// <summary>
        /// Renders the timeline markers' colors.
        /// </summary>
        /// <param name="handleColor">Color of the timeline markers' handle.</param>
        /// <param name="lineColor">Color of the timeline markers' line.</param>
        public void RenderColor(Color handleColor, Color lineColor)
        {
            Handle.color = handleColor;
            Line.color = lineColor;
        }

        /// <summary>
        /// Renders the timeline markers' line.
        /// </summary>
        public void RenderLine() => RenderLine(EditorConfig.Instance.MarkerLineDotted.Value);

        /// <summary>
        /// Renders the timeline markers' line.
        /// </summary>
        /// <param name="line">If the line should be dotted..</param>
        public void RenderLine(bool line)
        {
            Line.sprite = line ? EditorSprites.DottedLineSprite : null;
            Line.type = line ? Image.Type.Tiled : Image.Type.Simple;
        }

        /// <summary>
        /// Renders the timeline markers' lines' width.
        /// </summary>
        public void RenderLineWidth() => RenderLineWidth(EditorConfig.Instance.MarkerLineWidth.Value);

        /// <summary>
        /// Renders the timeline markers' lines' width.
        /// </summary>
        /// <param name="width">Width of the line.</param>
        public void RenderLineWidth(float width) => Line.rectTransform.sizeDelta = new Vector2(width, 301f);

        /// <summary>
        /// Renders the timeline markers' start & end loop flags.
        /// </summary>
        /// <param name="start">If the marker is the start of the marker loop.</param>
        /// <param name="end">If the marker is the end of the marker loop.</param>
        public void RenderFlags(bool start, bool end)
        {
            if (FlagStart)
                FlagStart.gameObject.SetActive(start);
            if (FlagEnd)
                FlagEnd.gameObject.SetActive(end);
        }

        /// <summary>
        /// Converts the marker to a planner note.
        /// </summary>
        public void ToPlannerNote()
        {
            if (!Marker)
                return;

            ProjectPlanner.inst.AddPlanner(new Planners.NotePlanner
            {
                Active = true,
                Name = Name,
                Color = ColorSlot,
                Position = new Vector2(Screen.width / 2, Screen.height / 2),
                Text = Description,
            });
            ProjectPlanner.inst.SaveNotes();
        }

        #endregion
    }
}
