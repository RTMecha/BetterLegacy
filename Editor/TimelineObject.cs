using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

using BetterLegacy.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core;

using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BasePrefabObject = DataManager.GameData.PrefabObject;

namespace BetterLegacy.Editor
{
    public delegate TimelineObject TimelineObjectReturn<T>(T t);
    public delegate TimelineObject TimelineObjectReturn<T, T1>(T t, T1 t1);
    public delegate TimelineObject TimelineObjectReturn<T, T1, T2>(T t, T1 t1, T2 t2);

    public class TimelineObject : Exists
    {
        public TimelineObject(object data)
        {
            Data = data;
        }

        public TimelineObject(object data, GameObject gameObject, Image image)
        {
            Data = data;
            GameObject = gameObject;
            Image = image;
        }
        
        public TimelineObject(object data, int type, GameObject gameObject, Image image)
        {
            Data = data;
            Type = type;
            GameObject = gameObject;
            Image = image;
        }

        public T GetData<T>() => (T)Data;

        public object Data { get; set; }
        public GameObject GameObject { get; set; }
        public Image Image { get; set; }
        public HoverUI Hover { get; set; }
        public TextMeshProUGUI Text { get; set; }

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

        public int Type { get; set; }
        int index;
        public int Index
        {
            get
            {
                if (IsBeatmapObject)
                    return DataManager.inst.gameData.beatmapObjects.IndexOf(Data as BeatmapObject);
                if (IsPrefabObject)
                    return DataManager.inst.gameData.prefabObjects.IndexOf(Data as PrefabObject);
                if (IsEventKeyframe)
                    return index;
                return -1;
            }
            set => index = value;
        }

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
        }

        public bool Locked
        {
            get
            {
                if (IsBeatmapObject)
                    return (Data as BeatmapObject).editorData.locked;
                if (IsPrefabObject)
                    return (Data as PrefabObject).editorData.locked;
                return false;
            }
            set
            {
                if (IsBeatmapObject)
                    (Data as BeatmapObject).editorData.locked = value;
                if (IsPrefabObject)
                    (Data as PrefabObject).editorData.locked = value;
            }
        }

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

        public bool selected;

        float zoom = 0.05f;
        public float Zoom { get => zoom; set => zoom = value; }

        float timelinePosition;
        public float TimelinePosition { get => timelinePosition; set => timelinePosition = value; }

        public bool IsBeatmapObject => Data != null && Data is BeatmapObject;
        public bool IsPrefabObject => Data != null && Data is PrefabObject;
        public bool IsEventKeyframe => Data != null && Data is EventKeyframe;

        public List<TimelineObject> InternalSelections { get; set; } = new List<TimelineObject>();
    }
}
