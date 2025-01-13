using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Object for storing keyframe data in the timelines.
    /// </summary>
    public class TimelineKeyframe : Exists
    {
        public TimelineKeyframe(EventKeyframe eventKeyframe)
        {
            this.eventKeyframe = eventKeyframe;
        }

        public TimelineKeyframe(EventKeyframe eventKeyframe, BeatmapObject beatmapObject)
        {
            this.eventKeyframe = eventKeyframe;
            this.beatmapObject = beatmapObject;
            isObjectKeyframe = true;
        }

        #region Properties

        #region UI

        /// <summary>
        /// The timeline game object.
        /// </summary>
        public GameObject GameObject { get; set; }

        /// <summary>
        /// The image of the timeline object.
        /// </summary>
        public Image Image { get; set; }

        /// <summary>
        /// The hover scale component of the timeline object.
        /// </summary>
        public HoverUI Hover { get; set; }

        #endregion

        #region Data

        /// <summary>
        /// Gets the ID of the object.
        /// </summary>
        public string ID => eventKeyframe.id;

        /// <summary>
        /// The type of keyframe. Only used for keyframes.
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// Gets the index of the object.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets and sets the time of the object.
        /// </summary>
        public float Time
        {
            get => eventKeyframe.eventTime;
            set => eventKeyframe.eventTime = value;
        }

        /// <summary>
        /// Gets and sets the locked state of the object.
        /// </summary>
        public bool Locked
        {
            get => eventKeyframe.locked;
            set => eventKeyframe.locked = value;
        }

        /// <summary>
        /// If the timeline object is on the currently viewed editor layer.
        /// </summary>
        public bool IsCurrentLayer => (Type / RTEventEditor.EVENT_LIMIT) == EditorTimeline.inst.Layer && EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events && (RTEditor.ShowModdedUI || Type < 10);

        /// <summary>
        /// If the object is currently selected.
        /// </summary>
        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                RenderVisibleState(false);
            }
        }

        #endregion

        #endregion

        #region Fields

        #region Validation

        /// <summary>
        /// If the timeline object has been validated.
        /// </summary>
        public bool verified;

        /// <summary>
        /// If the timeline object is an object keyframe.
        /// </summary>
        public bool isObjectKeyframe;

        #endregion

        #region Dragging

        /// <summary>
        /// Drag time offset.
        /// </summary>
        public float timeOffset;

        /// <summary>
        /// Drag bin offset.
        /// </summary>
        public int binOffset;

        #endregion

        public BeatmapObject beatmapObject;
        public EventKeyframe eventKeyframe;

        bool selected;

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the timeline keyframes.
        /// </summary>
        /// <param name="update">If <see cref="Render"/> should run.</param>
        public void Init(bool update = false)
        {
            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            if (isObjectKeyframe)
            {
                gameObject = ObjEditor.inst.objTimelinePrefab.Duplicate(ObjEditor.inst.TimelineParents[Type], $"{ObjectEditor.IntToType(Type)}_{Index}");

                var button = gameObject.GetComponent<Button>();
                button.onClick.ClearAll();

                TriggerHelper.AddEventTriggers(gameObject,
                    TriggerHelper.CreateTimelineKeyframeTrigger(this),
                    TriggerHelper.CreateTimelineKeyframeStartDragTrigger(this),
                    TriggerHelper.CreateTimelineKeyframeEndDragTrigger(this),
                    TriggerHelper.CreateTimelineKeyframeSelectTrigger(this));
            }
            else
            {
                gameObject = EventEditor.inst.TimelinePrefab.Duplicate(EventEditor.inst.EventHolders.transform.GetChild(Type % RTEventEditor.EVENT_LIMIT), $"keyframe - {Type}");

                TriggerHelper.AddEventTriggers(gameObject,
                    TriggerHelper.CreateTimelineKeyframeTrigger(this),
                    TriggerHelper.CreateTimelineKeyframeStartDragTrigger(this),
                    TriggerHelper.CreateTimelineKeyframeEndDragTrigger(this),
                    TriggerHelper.CreateTimelineKeyframeSelectTrigger(this));
            }

            GameObject = gameObject;
            Image = GameObject.transform.GetChild(0).GetComponent<Image>();

            if (update)
            {
                Render();
                return;
            }

            RenderVisibleState();
        }

        /// <summary>
        /// Updates all properties of the timeline keyframe.
        /// </summary>
        public void Render()
        {
            if (isObjectKeyframe)
            {
                if (beatmapObject.events[Type].TryFindIndex(x => (x as EventKeyframe).id == ID, out int kfIndex))
                    Index = kfIndex;

                RenderSprite(beatmapObject.events[Type]);
                RenderPos();
                RenderIcons();
            }
            else
            {
                var events = RTEventEditor.AllEvents[Type];
                if (events.TryFindIndex(x => (x as EventKeyframe).id == ID, out int index))
                    Index = index;

                if (Type / RTEventEditor.EVENT_LIMIT == EditorTimeline.inst.Layer)
                {
                    RenderSprite(events);
                    RenderPos();
                    RenderIcons();
                }
            }

            RenderVisibleState();
        }

        /// <summary>
        /// Updates the timeline keyframes' active state and color.
        /// </summary>
        /// <param name="setActive">If active state should change.</param>
        public void RenderVisibleState(bool setActive = true)
        {
            var theme = EditorThemeManager.CurrentTheme;
            if (isObjectKeyframe)
            {
                var objectKeyframeColor1 = theme.GetObjectKeyframeColor(0);
                var objectKeyframeColor2 = theme.GetObjectKeyframeColor(1);
                var objectKeyframeColor3 = theme.GetObjectKeyframeColor(2);
                var objectKeyframeColor4 = theme.GetObjectKeyframeColor(3);
                var objectKeyframesRenderBinColor = EditorConfig.Instance.ObjectKeyframesRenderBinColor.Value;

                if (!GameObject || !Image)
                    return;

                if (!GameObject.activeSelf)
                    GameObject.SetActive(true);

                var color = Type switch
                {
                    0 => objectKeyframeColor1,
                    1 => objectKeyframeColor2,
                    2 => objectKeyframeColor3,
                    3 => objectKeyframeColor4,
                    _ => ObjEditor.inst.NormalColor,
                };
                color.a = 1f;
                color = selected ? !objectKeyframesRenderBinColor ? ObjEditor.inst.SelectedColor : EventEditor.inst.Selected : color;

                if (Image.color != color)
                    Image.color = color;
            }
            else
            {
                var eventKeyframesRenderBinColor = EditorConfig.Instance.EventKeyframesRenderBinColor.Value;

                if (!GameObject || !Image)
                    return;

                bool isCurrentLayer = IsCurrentLayer;

                if (setActive && GameObject.activeSelf != isCurrentLayer)
                    GameObject.SetActive(isCurrentLayer);

                if (!isCurrentLayer)
                    return;

                var color = eventKeyframesRenderBinColor ? theme.GetEventKeyframeColor(Type % RTEventEditor.EVENT_LIMIT) : ObjEditor.inst.NormalColor;
                color.a = 1f;
                color = selected ? !eventKeyframesRenderBinColor ? ObjEditor.inst.SelectedColor : EventEditor.inst.Selected : color;

                if (Image.color != color)
                    Image.color = color;
            }
        }

        /// <summary>
        /// Updates the timeline keyframes' position and length.
        /// </summary>
        public void RenderPos() => RenderPos(isObjectKeyframe ? ObjEditor.inst.Zoom : EditorManager.inst.Zoom, Time);

        /// <summary>
        /// Updates the timeline keyframes' position and length.
        /// </summary>
        /// <param name="zoom">Timeline zoom.</param>
        /// <param name="time">Time the keyframe is positioned at.</param>
        public void RenderPos(float zoom, float time)
        {
            var rectTransform = GameObject.transform.AsRT();

            if (isObjectKeyframe)
            {
                rectTransform.sizeDelta = new Vector2(14f, 25f);
                rectTransform.anchoredPosition = new Vector2(time * 14f * zoom + 5f, 0f);
            }
            else
            {
                GameObject.transform.AsRT().anchoredPosition = new Vector2(time * zoom - EditorManager.BaseUnit / 2, 0.0f);
                GameObject.transform.AsRT().pivot = new Vector2(0f, 1f); // Fixes the keyframes being off center.
            }
        }

        /// <summary>
        /// Renders the keyframe sprite based on its type.
        /// </summary>
        /// <param name="events">Keyframe list reference.</param>
        public void RenderSprite(List<DataManager.GameData.EventKeyframe> events) => Image.sprite = EditorTimeline.GetKeyframeIcon(eventKeyframe.curveType, events.Count > Index + 1 ? events[Index + 1].curveType : DataManager.inst.AnimationList[0]);

        /// <summary>
        /// Updates the timeline keyframes' icons.
        /// </summary>
        public void RenderIcons()
        {
            var gameObject = GameObject;
            if (!gameObject)
                return;

            var lockedObj = gameObject.transform.Find("lock");
            if (lockedObj)
                lockedObj.gameObject.SetActive(Locked);

            return;
        }

        #endregion
    }
}
