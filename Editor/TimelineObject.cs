using BetterLegacy.Components;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Editor.Managers;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BasePrefabObject = DataManager.GameData.PrefabObject;

namespace BetterLegacy.Editor
{
    public delegate TimelineObject TimelineObjectReturn<T>(T t);
    public delegate TimelineObject TimelineObjectReturn<T, T1>(T t, T1 t1);
    public delegate TimelineObject TimelineObjectReturn<T, T1, T2>(T t, T1 t1, T2 t2);

    /// <summary>
    /// The object for storing object data in the timeline.
    /// </summary>
    public class TimelineObject : Exists
    {
        public TimelineObject(object data)
        {
            Data = data;
        }

        /// <summary>
        /// Casts the object data into a type.
        /// </summary>
        /// <typeparam name="T">The type to cast the data to.</typeparam>
        /// <returns>Casted data.</returns>
        public T GetData<T>() => (T)Data;

        /// <summary>
        /// The reference of an object related to the timeline object.
        /// </summary>
        public object Data { get; set; }

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

        /// <summary>
        /// The name text of the timeline object.
        /// </summary>
        public TextMeshProUGUI Text { get; set; }

        public bool verified = false;

        /// <summary>
        /// Gets the ID of the object.
        /// </summary>
        public string ID
        {
            get
            {
                if (IsBeatmapObject)
                    return (Data as BaseBeatmapObject).id;
                if (IsPrefabObject)
                    return (Data as BasePrefabObject).ID;
                if (IsEventKeyframe)
                    return (Data as EventKeyframe).id;
                return "";
            }
        }

        /// <summary>
        /// The type of keyframe. Only used for keyframes.
        /// </summary>
        public int Type { get; set; }
        int index;
        /// <summary>
        /// Gets the index of the object.
        /// </summary>
        public int Index
        {
            get
            {
                if (IsBeatmapObject)
                    return GameData.Current.beatmapObjects.IndexOf(Data as BeatmapObject);
                if (IsPrefabObject)
                    return GameData.Current.prefabObjects.IndexOf(Data as PrefabObject);
                if (IsEventKeyframe)
                    return index;
                return -1;
            }
            set => index = value;
        }

        /// <summary>
        /// Gets and sets the time of the object.
        /// </summary>
        public float Time
        {
            get
            {
                if (IsBeatmapObject)
                    return (Data as BeatmapObject).StartTime;
                if (IsPrefabObject)
                    return (Data as PrefabObject).StartTime;
                if (IsEventKeyframe)
                    return (Data as EventKeyframe).eventTime;
                return -1;
            }
            set
            {
                if (IsBeatmapObject)
                    (Data as BeatmapObject).StartTime = value;
                if (IsPrefabObject)
                    (Data as PrefabObject).StartTime = value;
                if (IsEventKeyframe)
                    (Data as EventKeyframe).eventTime = value;
            }
        }

        /// <summary>
        /// Gets and sets the bin (timeline row) of the object.
        /// </summary>
        public int Bin
        {
            get
            {
                if (IsBeatmapObject)
                    return (Data as BeatmapObject).editorData.Bin;
                if (IsPrefabObject)
                    return (Data as PrefabObject).editorData.Bin;
                if (IsEventKeyframe)
                    return Type;
                return -1;
            }
            set
            {
                if (IsBeatmapObject)
                    (Data as BeatmapObject).editorData.Bin = value;
                if (IsPrefabObject)
                    (Data as PrefabObject).editorData.Bin = value;
                if (IsEventKeyframe)
                    Type = value;
            }
        }

        /// <summary>
        /// Gets and sets the layer of the object. Does not apply to keyframes.
        /// </summary>
        public int Layer
        {
            get
            {
                if (IsBeatmapObject)
                    return (Data as BeatmapObject).editorData.layer;
                if (IsPrefabObject)
                    return (Data as PrefabObject).editorData.layer;
                return -1;
            }
            set
            {
                if (IsBeatmapObject)
                    (Data as BeatmapObject).editorData.layer = value;
                if (IsPrefabObject)
                    (Data as PrefabObject).editorData.layer = value;
            }
        }

        /// <summary>
        /// Gets and sets the locked state of the object.
        /// </summary>
        public bool Locked
        {
            get
            {
                if (IsBeatmapObject)
                    return (Data as BeatmapObject).editorData.locked;
                if (IsPrefabObject)
                    return (Data as PrefabObject).editorData.locked;
                if (IsEventKeyframe)
                    return (Data as EventKeyframe).locked;
                return false;
            }
            set
            {
                if (IsBeatmapObject)
                    (Data as BeatmapObject).editorData.locked = value;
                if (IsPrefabObject)
                    (Data as PrefabObject).editorData.locked = value;
                if (IsEventKeyframe)
                    (Data as EventKeyframe).locked = value;
            }
        }

        /// <summary>
        /// Gets and sets the collapsed state of the object. Does not apply to keyframes.
        /// </summary>
        public bool Collapse
        {
            get
            {
                if (IsBeatmapObject)
                    return (Data as BeatmapObject).editorData.collapse;
                if (IsPrefabObject)
                    return (Data as PrefabObject).editorData.collapse;
                return false;
            }
            set
            {
                if (IsBeatmapObject)
                    (Data as BeatmapObject).editorData.collapse = value;
                if (IsPrefabObject)
                    (Data as PrefabObject).editorData.collapse = value;
            }
        }

        public float timeOffset;
        public int binOffset;

        public bool IsCurrentLayer
        {
            get
            {
                switch (ObjectType)
                {
                    case TimelineObjectType.BeatmapObject: return RTEditor.inst.layerType == RTEditor.LayerType.Objects && Layer == RTEditor.inst.Layer;
                    case TimelineObjectType.PrefabObject: return RTEditor.inst.layerType == RTEditor.LayerType.Objects && Layer == RTEditor.inst.Layer;
                    case TimelineObjectType.EventKeyframe:
                        {
                            int limit = Type / RTEventEditor.EventLimit;
                            bool isCurrentLayer = limit == RTEditor.inst.Layer && RTEditor.inst.layerType == RTEditor.LayerType.Events;
                            return isCurrentLayer && (RTEditor.ShowModdedUI || Type < 10);
                        }
                    default: return false;
                }
            }
        }

        bool selected;
        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                Update(false);
            }
        }

        public void Update(bool setActive = true)
        {
            switch (ObjectType)
            {
                case TimelineObjectType.BeatmapObject:
                    {
                        if (!GameObject || !Image)
                            return;

                        bool isCurrentLayer = IsCurrentLayer;

                        if (setActive && GameObject.activeSelf != isCurrentLayer)
                            GameObject.SetActive(IsCurrentLayer);

                        if (!isCurrentLayer)
                            return;

                        var color = selected ?
                            ObjEditor.inst.SelectedColor :
                            !string.IsNullOrEmpty(GetData<BeatmapObject>().prefabID) ?
                                GetData<BeatmapObject>().Prefab.PrefabType.Color :
                                ObjEditor.inst.NormalColor;

                        if (Image.color != color)
                            Image.color = color;

                        break;
                    }
                case TimelineObjectType.PrefabObject:
                    {
                        if (!GameObject || !Image)
                            return;

                        bool isCurrentLayer = IsCurrentLayer;

                        if (setActive && GameObject.activeSelf != isCurrentLayer)
                            GameObject.SetActive(IsCurrentLayer);

                        if (!isCurrentLayer)
                            return;

                        var color = selected ? ObjEditor.inst.SelectedColor : GetData<PrefabObject>().Prefab.PrefabType.Color;

                        if (Image.color != color)
                            Image.color = color;

                        break;
                    }
                case TimelineObjectType.EventKeyframe:
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
                                break;

                            var color = eventKeyframesRenderBinColor ? theme.GetEventKeyframeColor(Type % RTEventEditor.EventLimit) : ObjEditor.inst.NormalColor;
                            color.a = 1f;
                            color = selected ? !eventKeyframesRenderBinColor ? ObjEditor.inst.SelectedColor : EventEditor.inst.Selected : color;

                            if (Image.color != color)
                                Image.color = color;
                        }

                        break;
                    }
            }
        }

        float zoom = 0.05f;
        /// <summary>
        /// Gets and sets the stored zoom of the object. Specifically used for the object keyframe timeline.
        /// </summary>
        public float Zoom { get => zoom; set => zoom = value; }

        float timelinePosition;
        /// <summary>
        /// Gets and sets the stored timeline scrollbar position of the object. Specifically used for the object keyframe timeline.
        /// </summary>
        public float TimelinePosition { get => timelinePosition; set => timelinePosition = value; }

        /// <summary>
        /// Checks if the objects' data is of 'BeatmapObject' type.
        /// </summary>
        public bool IsBeatmapObject => Data != null && Data is BeatmapObject;

        /// <summary>
        /// Checks if the objects' data is of 'PRefabObject' type.
        /// </summary>
        public bool IsPrefabObject => Data != null && Data is PrefabObject;

        /// <summary>
        /// Checks if the objects' data is of 'EventKeyframe' type.
        /// </summary>
        public bool IsEventKeyframe => Data != null && Data is EventKeyframe;

        /// <summary>
        /// The internal keyframes an object stores. Only used for Beatmap Objects.
        /// </summary>
        public List<TimelineObject> InternalSelections { get; set; } = new List<TimelineObject>();

        public bool isObjectKeyframe;

        public TimelineObjectType ObjectType
        {
            get
            {
                if (IsBeatmapObject)
                    return TimelineObjectType.BeatmapObject;
                if (IsPrefabObject)
                    return TimelineObjectType.PrefabObject;
                if (IsEventKeyframe)
                    return TimelineObjectType.EventKeyframe;
                return TimelineObjectType.Null;
            }
        }

        public enum TimelineObjectType
        {
            Null,
            BeatmapObject,
            PrefabObject,
            EventKeyframe
        }
    }
}
