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
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Object for storing marker data.
    /// </summary>
    public class TimelineMarker : Exists
    {
        public TimelineMarker() { }
        public TimelineMarker(Marker marker)
        {
            Marker = marker;
            marker.timelineMarker = this;
            listButton = new MarkerButton(this);
        }

        #region Properties

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
        /// The markers' color.
        /// </summary>
        public Color Color => MarkerEditor.inst.markerColors[Mathf.Clamp(Marker.color, 0, MarkerEditor.inst.markerColors.Count - 1)];

        #endregion

        #endregion

        /// <summary>
        /// If the timeline marker is being dragged.
        /// </summary>
        public bool dragging;

        /// <summary>
        /// Marker button reference.
        /// </summary>
        public MarkerButton listButton;

        #region Methods

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
            gameObject.SetActive(true);
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
                            AudioManager.inst.SetMusicTimeWithDelay(Mathf.Clamp(Marker.time, 0f, AudioManager.inst.CurrentAudioSource.clip.length), 0.05f);
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
                    CoreHelper.Log($"Started dragging marker {index}");
                    dragging = true;
                    RTMarkerEditor.inst.dragging = true;
                }
            }));
        }

        /// <summary>
        /// Renders the whole timeline marker.
        /// </summary>
        public void Render()
        {
            var markerColor = Color;

            GameObject.SetActive(true);
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
        /// Renders the timeline markers position.
        /// </summary>
        public void RenderPosition() => RenderPosition(Marker.time, EditorManager.inst.Zoom);

        /// <summary>
        /// Renders the timeline markers position.
        /// </summary>
        /// <param name="time">Time position.</param>
        /// <param name="zoom">Timeline zoom.</param>
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
        /// Renders the timeline markers' name.
        /// </summary>
        public void RenderName() => RenderName(Marker.name);

        /// <summary>
        /// Renders the timeline markers' name.
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
        /// Renders the timeline markers' names' width.
        /// </summary>
        public void RenderTextWidth() => RenderTextWidth(EditorConfig.Instance.MarkerTextWidth.Value);

        /// <summary>
        /// Renders the timeline markers' names' width.
        /// </summary>
        /// <param name="width">Width of the name text UI.</param>
        public void RenderTextWidth(float width) => Text.transform.AsRT().sizeDelta = new Vector2(width, 20f);

        /// <summary>
        /// Renders the timeline markers' colors.
        /// </summary>
        public void RenderColor() => RenderColor(Color);

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

        #endregion
    }

    /// <summary>
    /// Provides a reference to the markers' button in the marker list.
    /// </summary>
    public class MarkerButton : Exists
    {
        public MarkerButton(TimelineMarker timelineMarker) => this.timelineMarker = timelineMarker;

        readonly TimelineMarker timelineMarker;

        /// <summary>
        /// GameObject of the marker button.
        /// </summary>
        public GameObject GameObject { get; set; }

        /// <summary>
        /// Name text of the marker button.
        /// </summary>
        public Text Name { get; set; }

        /// <summary>
        /// Time text of the marker button.
        /// </summary>
        public Text Time { get; set; }

        /// <summary>
        /// Color image of the marker button.
        /// </summary>
        public Image Color { get; set; }

        /// <summary>
        /// Button component of the marker button.
        /// </summary>
        public Button Button { get; set; }

        /// <summary>
        /// Clears all <see cref="MarkerButton"/> data.
        /// </summary>
        public void Clear()
        {
            GameObject = null;
            Name = null;
            Time = null;
            Color = null;
            Button = null;
        }

        /// <summary>
        /// Renders the marker button color.
        /// </summary>
        public void RenderColor() => RenderColor(timelineMarker.Color);

        /// <summary>
        /// Renders the marker button color.
        /// </summary>
        /// <param name="color">Color to set.</param>
        public void RenderColor(Color color) => Color.color = color;

        /// <summary>
        /// Renders the marker button name.
        /// </summary>
        public void RenderName() => RenderName(timelineMarker.Marker.name);

        /// <summary>
        /// Renders the marker button name.
        /// </summary>
        /// <param name="name">Name to set.</param>
        public void RenderName(string name) => Name.text = name;

        /// <summary>
        /// Renders the marker button time.
        /// </summary>
        public void RenderTime() => RenderTime(timelineMarker.Marker.time);

        /// <summary>
        /// Renders the marker button time.
        /// </summary>
        /// <param name="time">Time to set.</param>
        public void RenderTime(float time) => Time.text = string.Format("{0:0}:{1:00}.{2:000}", Mathf.Floor(time / 60f), Mathf.Floor(time % 60f), Mathf.Floor(time * 1000f % 1000f));
    }
}
