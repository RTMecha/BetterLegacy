using BetterLegacy.Components;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;
using LSFunctions;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BasePrefabObject = DataManager.GameData.PrefabObject;

namespace BetterLegacy.Editor
{
    /// <summary>
    /// The object for storing object data in the timeline.
    /// </summary>
    public class TimelineObject : Exists
    {
        public TimelineObject(object data)
        {
            this.data = data;
            TimelineReference = GetTimelineReferenceType(data);
            isBeatmapObject = TimelineReference == TimelineReferenceType.BeatmapObject;
            isPrefabObject = TimelineReference == TimelineReferenceType.PrefabObject;
            isEventKeyframe = TimelineReference == TimelineReferenceType.EventKeyframe;

            if (isBeatmapObject)
                InternalTimelineObjects = new List<TimelineObject>();
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

        /// <summary>
        /// The name text of the timeline object.
        /// </summary>
        public TextMeshProUGUI Text { get; set; }

        #endregion

        #region Data

        /// <summary>
        /// Gets the ID of the object.
        /// </summary>
        public string ID => TimelineReference switch
        {
            TimelineReferenceType.BeatmapObject => GetData<BeatmapObject>().id,
            TimelineReferenceType.PrefabObject => GetData<PrefabObject>().ID,
            TimelineReferenceType.EventKeyframe => GetData<EventKeyframe>().id,
            _ => string.Empty,
        };

        /// <summary>
        /// The type of keyframe. Only used for keyframes.
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// Gets the index of the object.
        /// </summary>
        public int Index
        {
            get => TimelineReference switch
            {
                TimelineReferenceType.BeatmapObject => GameData.Current.beatmapObjects.IndexOf(GetData<BeatmapObject>()),
                TimelineReferenceType.PrefabObject => GameData.Current.prefabObjects.IndexOf(GetData<PrefabObject>()),
                TimelineReferenceType.EventKeyframe => index,
                _ => -1,
            };
            set => index = value;
        }

        /// <summary>
        /// Gets and sets the time of the object.
        /// </summary>
        public float Time
        {
            get => TimelineReference switch
            {
                TimelineReferenceType.BeatmapObject => GetData<BeatmapObject>().StartTime,
                TimelineReferenceType.PrefabObject => GetData<PrefabObject>().StartTime,
                TimelineReferenceType.EventKeyframe => GetData<EventKeyframe>().eventTime,
                _ => 0f,
            };
            set
            {
                if (isBeatmapObject)
                    GetData<BeatmapObject>().StartTime = value;
                if (isPrefabObject)
                    GetData<PrefabObject>().StartTime = value;
                if (isEventKeyframe)
                    GetData<EventKeyframe>().eventTime = value;
            }
        }

        /// <summary>
        /// Gets and sets the bin (timeline row) of the object.
        /// </summary>
        public int Bin
        {
            get => TimelineReference switch
            {
                TimelineReferenceType.BeatmapObject => GetData<BeatmapObject>().editorData.Bin,
                TimelineReferenceType.PrefabObject => GetData<PrefabObject>().editorData.Bin,
                TimelineReferenceType.EventKeyframe => Type,
                _ => 0,
            };
            set
            {
                if (isBeatmapObject)
                    GetData<BeatmapObject>().editorData.Bin = value;
                if (isPrefabObject)
                    GetData<PrefabObject>().editorData.Bin = value;
                if (isEventKeyframe)
                    Type = value;
            }
        }

        /// <summary>
        /// Gets the total life time of the object.
        /// </summary>
        public float Length => TimelineReference switch
        {
            TimelineReferenceType.BeatmapObject => GetData<BeatmapObject>().GetObjectLifeLength(_takeCollapseIntoConsideration: true),
            TimelineReferenceType.PrefabObject => GetData<PrefabObject>().GetPrefabLifeLength(true),
            _ => 0f
        };

        /// <summary>
        /// Gets and sets the layer of the object. Does not apply to keyframes.
        /// </summary>
        public int Layer
        {
            get => TimelineReference switch
            {
                TimelineReferenceType.BeatmapObject => GetData<BeatmapObject>().editorData.layer,
                TimelineReferenceType.PrefabObject => GetData<PrefabObject>().editorData.layer,
                _ => 0,
            };
            set
            {
                if (isBeatmapObject)
                    GetData<BeatmapObject>().editorData.layer = value;
                if (isPrefabObject)
                    GetData<PrefabObject>().editorData.layer = value;

                RenderVisibleState();
            }
        }

        /// <summary>
        /// Gets and sets the locked state of the object.
        /// </summary>
        public bool Locked
        {
            get => TimelineReference switch
            {
                TimelineReferenceType.BeatmapObject => GetData<BeatmapObject>().editorData.locked,
                TimelineReferenceType.PrefabObject => GetData<PrefabObject>().editorData.locked,
                TimelineReferenceType.EventKeyframe => GetData<EventKeyframe>().locked,
                _ => false,
            };
            set
            {
                if (isBeatmapObject)
                    GetData<BeatmapObject>().editorData.locked = value;
                if (isPrefabObject)
                    GetData<PrefabObject>().editorData.locked = value;
                if (isEventKeyframe)
                    GetData<EventKeyframe>().locked = value;
            }
        }

        /// <summary>
        /// Gets and sets the collapsed state of the object. Does not apply to keyframes.
        /// </summary>
        public bool Collapse
        {
            get => TimelineReference switch
            {
                TimelineReferenceType.BeatmapObject => GetData<BeatmapObject>().editorData.collapse,
                TimelineReferenceType.PrefabObject => GetData<PrefabObject>().editorData.collapse,
                _ => false,
            };
            set
            {
                if (isBeatmapObject)
                    GetData<BeatmapObject>().editorData.collapse = value;
                if (isPrefabObject)
                    GetData<PrefabObject>().editorData.collapse = value;
            }
        }

        /// <summary>
        /// If the timeline object is on the currently viewed editor layer.
        /// </summary>
        public bool IsCurrentLayer => TimelineReference switch
        {
            TimelineReferenceType.BeatmapObject => RTEditor.inst.layerType == RTEditor.LayerType.Objects && Layer == RTEditor.inst.Layer,
            TimelineReferenceType.PrefabObject => RTEditor.inst.layerType == RTEditor.LayerType.Objects && Layer == RTEditor.inst.Layer,
            TimelineReferenceType.EventKeyframe => (Type / RTEventEditor.EVENT_LIMIT) == RTEditor.inst.Layer && RTEditor.inst.layerType == RTEditor.LayerType.Events && (RTEditor.ShowModdedUI || Type < 10),
            _ => false,
        };

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

        /// <summary>
        /// Gets and sets the stored zoom of the object. Specifically used for the object keyframe timeline.
        /// </summary>
        public float Zoom { get; set; } = 0.05f;

        /// <summary>
        /// Gets and sets the stored timeline scrollbar position of the object. Specifically used for the object keyframe timeline.
        /// </summary>
        public float TimelinePosition { get; set; }

        /// <summary>
        /// The internal keyframes an object stores. Only used for Beatmap Objects.
        /// </summary>
        public List<TimelineObject> InternalTimelineObjects { get; set; }

        /// <summary>
        /// What kind of object <see cref="data"/> is.
        /// </summary>
        public TimelineReferenceType TimelineReference { get; private set; }

        #endregion

        #endregion

        #region Fields

        #region Validation

        /// <summary>
        /// If the timeline object has been validated.
        /// </summary>
        public bool verified;

        /// <summary>
        /// If the objects' data is of 'BeatmapObject' type.
        /// </summary>
        public bool isBeatmapObject;

        /// <summary>
        /// If the objects' data is of 'PRefabObject' type.
        /// </summary>
        public bool isPrefabObject;

        /// <summary>
        /// If the objects' data is of 'EventKeyframe' type.
        /// </summary>
        public bool isEventKeyframe;

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

        #region Internal

        readonly object data;
        int index;
        bool selected;

        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// Casts the object data of the timeline object into a type.
        /// </summary>
        /// <typeparam name="T">The type to cast the data to.</typeparam>
        /// <returns>Casted data.</returns>
        public T GetData<T>() => (T)data;

        /// <summary>
        /// Tries to cast the object data into a type and outputs the result.
        /// </summary>
        /// <typeparam name="T">The type to cast the data to.</typeparam>
        /// <param name="obj">The output object data.</param>
        /// <returns>Returns true if <typeparamref name="T"/> is the same as <see cref="data"/>, otherwise returns false.</returns>
        public bool TryGetData<T>(out T obj)
        {
            if (data is T result)
            {
                obj = result;
                return true;
            }
            obj = default;
            return false;
        }

        /// <summary>
        /// Gets the <see cref="TimelineReferenceType"/> of an object.
        /// </summary>
        /// <param name="obj">Object to get the type of.</param>
        /// <returns>Returns a <see cref="TimelineReferenceType"/> of the <paramref name="obj"/>.</returns>
        public static TimelineReferenceType GetTimelineReferenceType(object obj)
        {
            if (obj is BeatmapObject)
                return TimelineReferenceType.BeatmapObject;
            if (obj is PrefabObject)
                return TimelineReferenceType.PrefabObject;
            if (obj is EventKeyframe)
                return TimelineReferenceType.EventKeyframe;
            return TimelineReferenceType.Null;
        }

        /// <summary>
        /// Initializes the timeline object.
        /// </summary>
        public void Init(bool update = false)
        {
            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            gameObject = ObjEditor.inst.timelineObjectPrefab.Duplicate(RTEditor.inst.timelineObjectsParent, "timeline object", index);
            var storage = gameObject.GetComponent<TimelineObjectStorage>();

            Hover = storage.hoverUI;
            GameObject = gameObject;
            Image = storage.image;
            Text = storage.text;

            storage.eventTrigger.triggers.Clear();
            storage.eventTrigger.triggers.Add(TriggerHelper.CreateBeatmapObjectTrigger(this));
            storage.eventTrigger.triggers.Add(TriggerHelper.CreateBeatmapObjectStartDragTrigger(this));
            storage.eventTrigger.triggers.Add(TriggerHelper.CreateBeatmapObjectEndDragTrigger(this));

            if (update)
            {
                Render();
                return;
            }

            RenderVisibleState();
        }

        /// <summary>
        /// Adds to the <see cref="RTEditor.timelineObjects"/> list if the timeline object wasn't verified already.
        /// </summary>
        /// <param name="forceAdd">If the timeline object should be added regardless of verification.</param>
        public void AddToList(bool forceAdd = false)
        {
            if (forceAdd || !verified && !RTEditor.inst.timelineObjects.Has(x => x.ID == ID))
            {
                verified = true;
                if (isPrefabObject || !RTEditor.inst.timelineObjects.TryFindIndex(x => x.isPrefabObject, out int index))
                    RTEditor.inst.timelineObjects.Add(this);
                else
                    RTEditor.inst.timelineObjects.Insert(index, this);
            }
        }

        /// <summary>
        /// Updates all properties of the timeline object.
        /// </summary>
        public void Render()
        {
            string name = "object name";
            float startTime = 0f;
            float length = 0f;

            var image = Image;

            Prefab prefab = null;

            if (TryGetData(out BeatmapObject beatmapObject))
            {
                prefab = beatmapObject.Prefab;
                var prefabExists = prefab != null;

                beatmapObject.timelineObject = this;

                name = beatmapObject.name;
                startTime = beatmapObject.StartTime;
                length = beatmapObject.GetObjectLifeLength(_takeCollapseIntoConsideration: true);

                image.type = ObjectEditor.inst.GetObjectTypePattern(beatmapObject.objectType);
                image.sprite = ObjectEditor.inst.GetObjectTypeSprite(beatmapObject.objectType);

                if (!prefabExists)
                    beatmapObject.RemovePrefabReference();
            }

            if (TryGetData(out PrefabObject prefabObject))
            {
                prefab = prefabObject.Prefab;
                name = prefab.Name;
                startTime = prefabObject.StartTime + prefab.Offset;
                length = prefabObject.GetPrefabLifeLength(true);
                image.type = Image.Type.Simple;
                image.sprite = null;
            }

            RenderText(name);
            RenderIcons(prefab?.PrefabType);
            RenderPosLength(EditorManager.inst.Zoom, length, startTime);
            RenderVisibleState();
        }

        /// <summary>
        /// Updates the Timeline Objects' active state and color.
        /// </summary>
        /// <param name="setActive">If active state should change.</param>
        public void RenderVisibleState(bool setActive = true)
        {
            switch (TimelineReference)
            {
                case TimelineReferenceType.BeatmapObject:
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
                case TimelineReferenceType.PrefabObject:
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
                case TimelineReferenceType.EventKeyframe:
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

                            var color = eventKeyframesRenderBinColor ? theme.GetEventKeyframeColor(Type % RTEventEditor.EVENT_LIMIT) : ObjEditor.inst.NormalColor;
                            color.a = 1f;
                            color = selected ? !eventKeyframesRenderBinColor ? ObjEditor.inst.SelectedColor : EventEditor.inst.Selected : color;

                            if (Image.color != color)
                                Image.color = color;
                        }

                        break;
                    }
            }
        }

        /// <summary>
        /// Sets the text the timeline object displays.
        /// </summary>
        /// <param name="name">Name of the timeline object.</param>
        /// <param name="nullName">Name to display if <paramref name="name"/> is null or empty.</param>
        public void RenderText(string name)
        {
            var textMeshNoob = Text; // ha! take that tmp
            if (!textMeshNoob)
                return;

            textMeshNoob.text = !string.IsNullOrEmpty(name) ? string.Format("<mark=#000000aa>{0}</mark>", name) : string.Empty;
            textMeshNoob.color = LSColors.white;
        }

        /// <summary>
        /// Updates the timeline objects' position and length.
        /// </summary>
        public void RenderPosLength()
        {
            float timeOffset = 0f;

            if (isPrefabObject)
                timeOffset = GetData<PrefabObject>().Prefab.Offset;

            RenderPosLength(EditorManager.inst.Zoom, Length, Time + timeOffset);
        }

        /// <summary>
        /// Updates the timeline objects' position and length.
        /// </summary>
        /// <param name="zoom">Timeline zoom.</param>
        /// <param name="length">Alive time of the object.</param>
        /// <param name="time">Time the object is positioned at.</param>
        public void RenderPosLength(float zoom, float length, float time)
        {
            var rectTransform = GameObject.transform.AsRT();

            if (isEventKeyframe && isObjectKeyframe)
            {
                rectTransform.sizeDelta = new Vector2(14f, 25f);
                rectTransform.anchoredPosition = new Vector2(time * 14f * zoom + 5f, 0f);

                return;
            }

            length = length <= ObjectEditor.TimelineCollapseLength ? ObjectEditor.TimelineCollapseLength * zoom : length * zoom;

            rectTransform.sizeDelta = new Vector2(length, 20f);
            rectTransform.anchoredPosition = new Vector2(time * zoom, (-20 * Mathf.Clamp(Bin, 0, 14)));
            if (Hover)
                Hover.size = ObjectEditor.TimelineObjectHoverSize;
        }

        public void RenderSprite(List<DataManager.GameData.EventKeyframe> events)
        {
            if (TryGetData(out EventKeyframe eventKeyframe))
                Image.sprite = RTEditor.GetKeyframeIcon(eventKeyframe.curveType, events.Count > Index + 1 ? events[Index + 1].curveType : DataManager.inst.AnimationList[0]);
        }

        /// <summary>
        /// Updates the timeline objects' icons.
        /// </summary>
        /// <param name="prefabType">Prefab Type to retrieve a Prefab Type icon from.</param>
        public void RenderIcons(PrefabType prefabType = null)
        {
            var gameObject = GameObject;
            if (!gameObject)
                return;

            if (isEventKeyframe)
            {
                var lockedObj = gameObject.transform.Find("lock");
                if (lockedObj)
                    lockedObj.gameObject.SetActive(GetData<EventKeyframe>().locked);

                return;
            }

            var locked = Locked;
            gameObject.transform.Find("icons/lock").gameObject.SetActive(locked);
            gameObject.transform.Find("icons/dots").gameObject.SetActive(Collapse);
            var typeIcon = gameObject.transform.Find("icons/type").gameObject;

            var renderTypeIcon = prefabType != null && ObjectEditor.RenderPrefabTypeIcon;
            typeIcon.SetActive(renderTypeIcon);
            if (renderTypeIcon)
                typeIcon.transform.Find("type").GetComponent<Image>().sprite = prefabType.icon;
        }

        #endregion

        /// <summary>
        /// What type <see cref="TimelineObject.data"/> is.
        /// </summary>
        public enum TimelineReferenceType
        {
            /// <summary>
            /// If <see cref="data"/> is null.
            /// </summary>
            Null,
            /// <summary>
            /// If <see cref="data"/> is <seealso cref="Core.Data.BeatmapObject"/>.
            /// </summary>
            BeatmapObject,
            /// <summary>
            /// If <see cref="data"/> is <seealso cref="Core.Data.PrefabObject"/>.
            /// </summary>
            PrefabObject,
            /// <summary>
            /// If <see cref="data"/> is <seealso cref="Core.Data.EventKeyframe"/>.
            /// </summary>
            EventKeyframe
        }
    }
}
