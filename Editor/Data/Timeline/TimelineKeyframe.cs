using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Timeline
{
    /// <summary>
    /// Represents a keyframe in an editor timeline.
    /// </summary>
    public class TimelineKeyframe : Exists, ISelectable
    {
        #region Constructors

        public TimelineKeyframe(EventKeyframe eventKeyframe)
        {
            this.eventKeyframe = eventKeyframe;
            this.eventKeyframe.timelineKeyframe = this;
        }

        public TimelineKeyframe(EventKeyframe eventKeyframe, IAnimatable animatable, KeyframeTimeline timeline) : this(eventKeyframe)
        {
            this.timeline = timeline;
            this.animatable = animatable;
            isObjectKeyframe = true;
        }

        #endregion

        #region Values

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
            get => eventKeyframe.time;
            set => eventKeyframe.time = value;
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
        public bool IsCurrentLayer =>
            (RTEventEditor.inst.GetEventTypeIndex(Type) / RTEventEditor.EVENT_LIMIT) == EditorTimeline.inst.Layer &&
            EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events &&
            (RTEventEditor.inst.eventBins.TryFindIndex(x => x.index == Type, out int eventBinIndex) ?
                RTEventEditor.inst.eventBins[eventBinIndex].IsActive :
                (RTEditor.ShowModdedUI || Type < 10));

        bool selected;
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

        #region References

        /// <summary>
        /// Keyframe Timeline reference.
        /// </summary>
        public KeyframeTimeline timeline;

        /// <summary>
        /// Animatable object reference.
        /// </summary>
        public IAnimatable animatable;

        /// <summary>
        /// Keyframe reference.
        /// </summary>
        public EventKeyframe eventKeyframe;

        #endregion

        #endregion

        #region Functions

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
                gameObject = ObjEditor.inst.objTimelinePrefab.Duplicate(timeline ? timeline.KeyframeParents[Type] : ObjEditor.inst.TimelineParents[Type], $"{KeyframeTimeline.IntToType(Type)}_{Index}");

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
                gameObject = EventEditor.inst.TimelinePrefab.Duplicate(RTEventEditor.inst.GetTimelineParent(Type), $"keyframe - {Type}");

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
                var events = animatable.GetEventKeyframes(Type);
                if (events.TryFindIndex(x => x.id == ID, out int kfIndex))
                    Index = kfIndex;

                RenderSprite(events);
                RenderPos();
                RenderIcons();
            }
            else
            {
                var events = GameData.Current.events[Type];
                if (events.TryFindIndex(x => x.id == ID, out int index))
                    Index = index;

                if (IsCurrentLayer)
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
            if (!GameObject || !Image)
                return;

            if (isObjectKeyframe)
            {
                if (!GameObject.activeSelf)
                    GameObject.SetActive(true);

                var color = GetColor();

                if (Image.color != color)
                    Image.color = color;
            }
            else
            {
                bool isCurrentLayer = IsCurrentLayer;

                if (setActive && GameObject.activeSelf != isCurrentLayer)
                    GameObject.SetActive(isCurrentLayer);

                if (!isCurrentLayer)
                    return;

                var color = GetColor();

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
                rectTransform.anchoredPosition = new Vector2(time * zoom - EditorManager.BaseUnit / 2, 0.0f);
                rectTransform.pivot = new Vector2(0f, 1f); // Fixes the keyframes being off center.
            }
        }

        /// <summary>
        /// Renders the keyframe sprite based on its type.
        /// </summary>
        /// <param name="events">Keyframe list reference.</param>
        public void RenderSprite(List<EventKeyframe> events) => Image.sprite = EditorTimeline.GetKeyframeIcon(eventKeyframe.curve, events.Count > Index + 1 ? events[Index + 1].curve : Easing.Linear);

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

        /// <summary>
        /// Gets the index coordinates of the timeline keyframe.
        /// </summary>
        /// <returns>Returns a <see cref="KeyframeCoord"/>.</returns>
        public KeyframeCoord GetCoord() => new KeyframeCoord(Type, Index);

        /// <summary>
        /// Sets the index coordinates of the timeline keyframe.
        /// </summary>
        /// <param name="keyframeCoord">Coordinates of the keyframe to set.</param>
        public void SetCoord(KeyframeCoord keyframeCoord)
        {
            Type = keyframeCoord.type;
            Index = keyframeCoord.index;
        }

        /// <summary>
        /// Gets the color the timeline keyframe should render.
        /// </summary>
        /// <returns>Returns the color of the timeline keyframe.</returns>
        public Color GetColor()
        {
            Color color = selected ? GetSelectedColor() : GetNormalColor();
            color.a = 1f;
            return color;
        }

        /// <summary>
        /// Gets the normal color for the timeline keyframe.
        /// </summary>
        /// <returns>Returns the normal color for the timeline keyframe when it is selected.</returns>
        public Color GetNormalColor() => (isObjectKeyframe ? EditorConfig.Instance.ObjectKeyframesRenderBinColor.Value : EditorConfig.Instance.EventKeyframesRenderBinColor.Value) ?
            isObjectKeyframe ? EditorThemeManager.CurrentTheme.GetObjectKeyframeColor(Type) : EditorThemeManager.CurrentTheme.GetEventKeyframeColor(RTEventEditor.inst.GetEventTypeIndex(Type) % RTEventEditor.EVENT_LIMIT) :
            ObjEditor.inst.NormalColor;

        /// <summary>
        /// Gets the selection color for the timeline keyframe.
        /// </summary>
        /// <returns>Returns the selection color for the timeline keyframe when it is selected.</returns>
        public Color GetSelectedColor() => (isObjectKeyframe ? !EditorConfig.Instance.ObjectKeyframesRenderBinColor.Value : !EditorConfig.Instance.EventKeyframesRenderBinColor.Value) ?
            ObjEditor.inst.SelectedColor : EventEditor.inst.Selected;

        public override string ToString() => eventKeyframe ? eventKeyframe.ToString() : base.ToString();

        #endregion
    }
}
