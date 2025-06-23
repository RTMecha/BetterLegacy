using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using TMPro;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Object for storing object data in the timeline.
    /// </summary>
    public class TimelineObject : Exists
    {
        public TimelineObject(object data)
        {
            this.data = data;
            TimelineReference = GetTimelineReferenceType(data);
            isBeatmapObject = TimelineReference == TimelineReferenceType.BeatmapObject;
            isPrefabObject = TimelineReference == TimelineReferenceType.PrefabObject;
            isBackgroundObject = TimelineReference == TimelineReferenceType.BackgroundObject;

            if (isBeatmapObject)
                InternalTimelineObjects = new List<TimelineKeyframe>();
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
            TimelineReferenceType.PrefabObject => GetData<PrefabObject>().id,
            TimelineReferenceType.BackgroundObject => GetData<BackgroundObject>().id,
            _ => string.Empty,
        };

        /// <summary>
        /// Gets the name of the object.
        /// </summary>
        public string Name => TimelineReference switch
        {
            TimelineReferenceType.BeatmapObject => GetData<BeatmapObject>().name,
            TimelineReferenceType.PrefabObject => GetData<PrefabObject>().GetPrefab()?.name,
            TimelineReferenceType.BackgroundObject => GetData<BackgroundObject>().name,
            _ => string.Empty,
        };

        /// <summary>
        /// Gets the index of the object.
        /// </summary>
        public int Index
        {
            get => TimelineReference switch
            {
                TimelineReferenceType.BeatmapObject => GameData.Current.beatmapObjects.IndexOf(GetData<BeatmapObject>()),
                TimelineReferenceType.PrefabObject => GameData.Current.prefabObjects.IndexOf(GetData<PrefabObject>()),
                TimelineReferenceType.BackgroundObject => GameData.Current.backgroundObjects.IndexOf(GetData<BackgroundObject>()),
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
                TimelineReferenceType.BackgroundObject => GetData<BackgroundObject>().StartTime,
                _ => 0f,
            };
            set
            {
                if (isBeatmapObject)
                    GetData<BeatmapObject>().StartTime = value;
                if (isPrefabObject)
                    GetData<PrefabObject>().StartTime = value;
                if (isBackgroundObject)
                    GetData<BackgroundObject>().StartTime = value;
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
                TimelineReferenceType.BackgroundObject => GetData<BackgroundObject>().editorData.Bin,
                _ => 0,
            };
            set
            {
                if (isBeatmapObject)
                    GetData<BeatmapObject>().editorData.Bin = value;
                if (isPrefabObject)
                    GetData<PrefabObject>().editorData.Bin = value;
                if (isBackgroundObject)
                    GetData<BackgroundObject>().editorData.Bin = value;
            }
        }

        /// <summary>
        /// Gets the total life time of the object.
        /// </summary>
        public float Length => TimelineReference switch
        {
            TimelineReferenceType.BeatmapObject => GetData<BeatmapObject>().GetObjectLifeLength(collapse: true),
            TimelineReferenceType.PrefabObject => GetData<PrefabObject>().GetObjectLifeLength(collapse: true),
            TimelineReferenceType.BackgroundObject => GetData<BackgroundObject>().GetObjectLifeLength(collapse: true),
            _ => 0f
        };

        /// <summary>
        /// Gets and sets the layer of the object.
        /// </summary>
        public int Layer
        {
            get => TimelineReference switch
            {
                TimelineReferenceType.BeatmapObject => GetData<BeatmapObject>().editorData.Layer,
                TimelineReferenceType.PrefabObject => GetData<PrefabObject>().editorData.Layer,
                TimelineReferenceType.BackgroundObject => GetData<BackgroundObject>().editorData.Layer,
                _ => 0,
            };
            set
            {
                if (isBeatmapObject)
                    GetData<BeatmapObject>().editorData.Layer = value;
                if (isPrefabObject)
                    GetData<PrefabObject>().editorData.Layer = value;
                if (isBackgroundObject)
                    GetData<BackgroundObject>().editorData.Layer = value;

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
                TimelineReferenceType.BackgroundObject => GetData<BackgroundObject>().editorData.locked,
                _ => false,
            };
            set
            {
                if (isBeatmapObject)
                    GetData<BeatmapObject>().editorData.locked = value;
                if (isPrefabObject)
                    GetData<PrefabObject>().editorData.locked = value;
                if (isBackgroundObject)
                    GetData<BackgroundObject>().editorData.locked = value;
            }
        }

        /// <summary>
        /// Gets and sets the collapsed state of the object.
        /// </summary>
        public bool Collapse
        {
            get => TimelineReference switch
            {
                TimelineReferenceType.BeatmapObject => GetData<BeatmapObject>().editorData.collapse,
                TimelineReferenceType.PrefabObject => GetData<PrefabObject>().editorData.collapse,
                TimelineReferenceType.BackgroundObject => GetData<BackgroundObject>().editorData.collapse,
                _ => false,
            };
            set
            {
                if (isBeatmapObject)
                    GetData<BeatmapObject>().editorData.collapse = value;
                if (isPrefabObject)
                    GetData<PrefabObject>().editorData.collapse = value;
                if (isBackgroundObject)
                    GetData<BackgroundObject>().editorData.collapse = value;
            }
        }

        /// <summary>
        /// Gets and sets the hidden state of the object.
        /// </summary>
        public bool Hidden
        {
            get => TimelineReference switch
            {
                TimelineReferenceType.BeatmapObject => GetData<BeatmapObject>().editorData.hidden,
                TimelineReferenceType.PrefabObject => GetData<PrefabObject>().editorData.hidden,
                TimelineReferenceType.BackgroundObject => GetData<BackgroundObject>().editorData.hidden,
                _ => false,
            };
            set
            {
                if (isBeatmapObject)
                    GetData<BeatmapObject>().editorData.hidden = value;
                if (isPrefabObject)
                    GetData<PrefabObject>().editorData.hidden = value;
                if (isBackgroundObject)
                    GetData<BackgroundObject>().editorData.hidden = value;
            }
        }

        /// <summary>
        /// Gets and sets the selectable in preview state of the object.
        /// </summary>
        public bool SelectableInPreview
        {
            get => TimelineReference switch
            {
                TimelineReferenceType.BeatmapObject => GetData<BeatmapObject>().editorData.selectable,
                TimelineReferenceType.PrefabObject => GetData<PrefabObject>().editorData.selectable,
                TimelineReferenceType.BackgroundObject => GetData<BackgroundObject>().editorData.selectable,
                _ => false,
            };
            set
            {
                if (isBeatmapObject)
                    GetData<BeatmapObject>().editorData.selectable = value;
                if (isPrefabObject)
                    GetData<PrefabObject>().editorData.selectable = value;
                if (isBackgroundObject)
                    GetData<BackgroundObject>().editorData.selectable = value;
            }
        }

        /// <summary>
        /// Gets the objects' editor data.
        /// </summary>
        public ObjectEditorData EditorData => data is IEditable editable ? editable.EditorData : null;

        /// <summary>
        /// If the timeline object is on the currently viewed editor layer.
        /// </summary>
        public bool IsCurrentLayer => TimelineReference switch
        {
            TimelineReferenceType.BeatmapObject => EditorTimeline.inst.layerType == EditorTimeline.LayerType.Objects && Layer == EditorTimeline.inst.Layer,
            TimelineReferenceType.PrefabObject => EditorTimeline.inst.layerType == EditorTimeline.LayerType.Objects && Layer == EditorTimeline.inst.Layer,
            TimelineReferenceType.BackgroundObject => EditorTimeline.inst.layerType == EditorTimeline.LayerType.Objects && Layer == EditorTimeline.inst.Layer,
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
        public List<TimelineKeyframe> InternalTimelineObjects { get; set; }

        /// <summary>
        /// What kind of object <see cref="data"/> is.
        /// </summary>
        public TimelineReferenceType TimelineReference { get; private set; }

        /// <summary>
        /// If the timeline object data is prefabable.
        /// </summary>
        public bool IsPrefabable => data is IPrefabable;

        #endregion

        #endregion

        #region Fields

        #region Validation

        /// <summary>
        /// If the timeline object has been validated.
        /// </summary>
        public bool verified;

        /// <summary>
        /// If the objects' data is of <see cref="BeatmapObject"/> type.
        /// </summary>
        public bool isBeatmapObject;

        /// <summary>
        /// If the objects' data is of <see cref="PrefabObject"/> type.
        /// </summary>
        public bool isPrefabObject;
        
        /// <summary>
        /// If the objects' data is of <see cref="BackgroundObject"/> type.
        /// </summary>
        public bool isBackgroundObject;

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
        /// Casts the object data to <see cref="IPrefabable"/>.
        /// </summary>
        /// <returns>Casted data as <see cref="IPrefabable"/>.</returns>
        public IPrefabable AsPrefabable() => data as IPrefabable;

        /// <summary>
        /// Tries to cast the object data into <see cref="IPrefabable"/> and outputs the result.
        /// </summary>
        /// <param name="result">The output prefabable object.</param>
        /// <returns>Returns true if the object is prefabable, otherwise returns false.</returns>
        public bool TryGetPrefabable(out IPrefabable result)
        {
            if (data is IPrefabable prefabable)
            {
                result = prefabable;
                return true;
            }
            result = null;
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
            if (obj is BackgroundObject)
                return TimelineReferenceType.BackgroundObject;
            return TimelineReferenceType.Null;
        }

        /// <summary>
        /// Initializes the timeline object.
        /// </summary>
        /// <param name="update">If <see cref="Render"/> should run.</param>
        public void Init(bool update = false)
        {
            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            gameObject = ObjEditor.inst.timelineObjectPrefab.Duplicate(EditorTimeline.inst.timelineObjectsParent, "timeline object", Index);
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
        /// Adds to the <see cref="EditorTimeline.timelineObjects"/> list if the timeline object wasn't verified already.
        /// </summary>
        /// <param name="forceAdd">If the timeline object should be added regardless of verification.</param>
        public void AddToList(bool forceAdd = false)
        {
            if (forceAdd || !verified && !EditorTimeline.inst.timelineObjects.Has(x => x.ID == ID))
            {
                verified = true;
                EditorTimeline.inst.timelineObjects.Add(this);
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

            if (isBeatmapObject && TryGetData(out BeatmapObject beatmapObject))
            {
                prefab = beatmapObject.GetPrefab();

                beatmapObject.timelineObject = this;

                name = beatmapObject.name;
                startTime = beatmapObject.StartTime;
                length = beatmapObject.GetObjectLifeLength(collapse: true);

                image.type = EditorTimeline.inst.GetObjectTypePattern(beatmapObject.objectType);
                image.sprite = EditorTimeline.inst.GetObjectTypeSprite(beatmapObject.objectType);

                if (!prefab)
                    beatmapObject.RemovePrefabReference();
            }

            if (isPrefabObject && TryGetData(out PrefabObject prefabObject))
            {
                prefab = prefabObject.GetPrefab();

                prefabObject.timelineObject = this;

                name = prefab.name;
                startTime = prefabObject.StartTime + prefab.offset;
                length = prefabObject.GetObjectLifeLength(collapse: true);

                image.type = Image.Type.Simple;
                image.sprite = null;
            }

            if (isBackgroundObject && TryGetData(out BackgroundObject backgroundObject))
            {
                prefab = backgroundObject.GetPrefab();

                backgroundObject.timelineObject = this;

                name = backgroundObject.name;
                startTime = backgroundObject.StartTime;
                length = backgroundObject.GetObjectLifeLength(collapse: true);

                image.type = Image.Type.Simple;
                image.sprite = null;

                if (!prefab)
                    backgroundObject.RemovePrefabReference();
            }

            RenderText(name);
            RenderIcons(prefab?.GetPrefabType());
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
                case TimelineReferenceType.BeatmapObject: {
                        if (!GameObject || !Image)
                            return;

                        bool isCurrentLayer = IsCurrentLayer;

                        if (setActive && GameObject.activeSelf != isCurrentLayer)
                            GameObject.SetActive(IsCurrentLayer);

                        if (!isCurrentLayer)
                            return;

                        var beatmapObject = GetData<BeatmapObject>();
                        Prefab prefab = null;
                        if (!string.IsNullOrEmpty(beatmapObject.prefabID))
                        {
                            prefab = beatmapObject.GetPrefab();
                            if (!prefab)
                                beatmapObject.RemovePrefabReference();
                        }

                        var color = selected ? GetSelectedColor() : GetColor();
                        if (!selected && prefab && EditorConfig.Instance.PrioritzePrefabTypeColor.Value)
                            color = prefab.GetPrefabType().color;

                        if (Image.color != color)
                            Image.color = color;

                        break;
                    }
                case TimelineReferenceType.PrefabObject: {
                        if (!GameObject || !Image)
                            return;

                        bool isCurrentLayer = IsCurrentLayer;

                        if (setActive && GameObject.activeSelf != isCurrentLayer)
                            GameObject.SetActive(isCurrentLayer);

                        if (!isCurrentLayer)
                            return;

                        var color = selected ? GetSelectedColor() : GetColor();

                        if (Image.color != color)
                            Image.color = color;

                        break;
                    }
                case TimelineReferenceType.BackgroundObject: {
                        if (!GameObject || !Image)
                            return;

                        bool isCurrentLayer = IsCurrentLayer;

                        if (setActive && GameObject.activeSelf != isCurrentLayer)
                            GameObject.SetActive(isCurrentLayer);

                        if (!isCurrentLayer)
                            return;

                        var backgroundObject = GetData<BackgroundObject>();
                        Prefab prefab = null;
                        if (!string.IsNullOrEmpty(backgroundObject.prefabID))
                        {
                            prefab = backgroundObject.GetPrefab();
                            if (!prefab)
                                backgroundObject.RemovePrefabReference();
                        }

                        var color = selected ? GetSelectedColor() : prefab ? prefab.GetPrefabType().color : GetColor();

                        if (Image.color != color)
                            Image.color = color;

                        break;
                    }
            }
        }

        /// <summary>
        /// Sets the text the timeline object displays.
        /// </summary>
        /// <param name="name">Name of the timeline object.</param>
        public void RenderText(string name)
        {
            var textMeshNoob = Text; // ha! take that tmp
            if (!textMeshNoob)
                return;

            textMeshNoob.text = !string.IsNullOrEmpty(name) ? $"<mark=#{GetMarkColor()}>{name}</mark>" : string.Empty;
            textMeshNoob.color = GetTextColor();
        }

        /// <summary>
        /// Updates the timeline objects' position and length.
        /// </summary>
        public void RenderPosLength()
        {
            float timeOffset = 0f;

            if (isPrefabObject)
                timeOffset = GetData<PrefabObject>().GetPrefab().offset;

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

            length = length <= ObjectEditor.TimelineCollapseLength ? ObjectEditor.TimelineCollapseLength * zoom : length * zoom;

            rectTransform.sizeDelta = new Vector2(length, 20f);
            rectTransform.anchoredPosition = new Vector2(time * zoom, (-20 * Mathf.Clamp(Bin, 0, EditorTimeline.inst.BinCount)));
            if (Hover)
                Hover.size = ObjectEditor.TimelineObjectHoverSize;
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

            var locked = Locked;
            gameObject.transform.Find("icons/lock").gameObject.SetActive(locked);
            gameObject.transform.Find("icons/dots").gameObject.SetActive(Collapse);
            var typeIcon = gameObject.transform.Find("icons/type").gameObject;

            var renderTypeIcon = prefabType != null && ObjectEditor.RenderPrefabTypeIcon;
            typeIcon.SetActive(renderTypeIcon);
            if (renderTypeIcon)
                typeIcon.transform.Find("type").GetComponent<Image>().sprite = prefabType.icon;
        }

        public void ShowContextMenu()
        {
            EditorContextMenu.inst.ShowContextMenu(
                new ButtonFunction("Select", () => EditorTimeline.inst.SetCurrentObject(this)),
                new ButtonFunction("Add to Selection", () => EditorTimeline.inst.AddSelectedObject(this)),
                new ButtonFunction("Create New", () => ObjectEditor.inst.CreateNewNormalObject()),
                new ButtonFunction("Update Object", () =>
                {
                    if (isBeatmapObject)
                        RTLevel.Current?.UpdateObject(GetData<BeatmapObject>());
                    if (isPrefabObject)
                        RTLevel.Current?.UpdatePrefab(GetData<PrefabObject>());
                    if (isBackgroundObject)
                        RTLevel.Current?.UpdateBackgroundObject(GetData<BackgroundObject>());
                }),
                new ButtonFunction(true),
                new ButtonFunction("Cut", () =>
                {
                    ObjectEditor.inst.CopyObjects();
                    EditorTimeline.inst.DeleteObjects();
                }),
                new ButtonFunction("Copy", ObjectEditor.inst.CopyObjects),
                new ButtonFunction("Paste", ObjectEditor.inst.PasteObject),
                new ButtonFunction("Duplicate", () =>
                {
                    var offsetTime = EditorTimeline.inst.SelectedObjects.Min(x => x.Time);

                    ObjectEditor.inst.CopyObjects();
                    ObjectEditor.inst.PasteObject(offsetTime);
                }),
                new ButtonFunction("Paste (Keep Prefab)", () => ObjectEditor.inst.PasteObject(0f, false)),
                new ButtonFunction("Duplicate (Keep Prefab)", () =>
                {
                    var offsetTime = EditorTimeline.inst.SelectedObjects.Min(x => x.Time);

                    ObjectEditor.inst.CopyObjects();
                    ObjectEditor.inst.PasteObject(offsetTime, false);
                }),
                new ButtonFunction("Delete", EditorTimeline.inst.DeleteObjects),
                new ButtonFunction(true),
                new ButtonFunction("Hide", () =>
                {
                    foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                    {
                        timelineObject.Hidden = true;
                        switch (timelineObject.TimelineReference)
                        {
                            case TimelineReferenceType.BeatmapObject: {
                                    RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), RTLevel.ObjectContext.HIDE);

                                    break;
                                }
                            case TimelineReferenceType.PrefabObject: {
                                    RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), RTLevel.PrefabContext.HIDE);

                                    break;
                                }
                            case TimelineReferenceType.BackgroundObject: {
                                    RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), RTLevel.BackgroundObjectContext.HIDE);

                                    break;
                                }
                        }
                    }
                }),
                new ButtonFunction("Unhide", () =>
                {
                    foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                    {
                        timelineObject.Hidden = false;
                        switch (timelineObject.TimelineReference)
                        {
                            case TimelineReferenceType.BeatmapObject: {
                                    RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), RTLevel.ObjectContext.HIDE);

                                    break;
                                }
                            case TimelineReferenceType.PrefabObject: {
                                    RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), RTLevel.PrefabContext.HIDE);

                                    break;
                                }
                            case TimelineReferenceType.BackgroundObject: {
                                    RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), RTLevel.BackgroundObjectContext.HIDE);

                                    break;
                                }
                        }
                    }
                }),
                new ButtonFunction("Preview Selectable", () =>
                {
                    foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                    {
                        if (timelineObject.isBackgroundObject)
                            continue;

                        timelineObject.SelectableInPreview = true;
                        switch (timelineObject.TimelineReference)
                        {
                            case TimelineReferenceType.BeatmapObject: {
                                    RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), RTLevel.ObjectContext.SELECTABLE);

                                    break;
                                }
                            case TimelineReferenceType.PrefabObject: {
                                    RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), RTLevel.PrefabContext.SELECTABLE);

                                    break;
                                }
                        }
                    }
                }),
                new ButtonFunction("Preview Unselectable", () =>
                {
                    foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                    {
                        if (timelineObject.isBackgroundObject)
                            continue;

                        timelineObject.SelectableInPreview = false;
                        switch (timelineObject.TimelineReference)
                        {
                            case TimelineReferenceType.BeatmapObject: {
                                    RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), RTLevel.ObjectContext.SELECTABLE);

                                    break;
                                }
                            case TimelineReferenceType.PrefabObject: {
                                    RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), RTLevel.PrefabContext.SELECTABLE);

                                    break;
                                }
                        }
                    }
                }),
                new ButtonFunction(true),
                new ButtonFunction("Move Backwards", () =>
                {
                    switch (TimelineReference)
                    {
                        case TimelineReferenceType.BeatmapObject: {
                                var beatmapObject = GetData<BeatmapObject>();
                                var index = GameData.Current.beatmapObjects.FindIndex(x => x == beatmapObject);
                                if (index <= 0)
                                {
                                    EditorManager.inst.DisplayNotification("Could not move object back since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                                    return;
                                }

                                GameData.Current.beatmapObjects.Move(index, index - 1);
                                EditorTimeline.inst.UpdateTransformIndex();
                                if (ObjectEditor.inst.Dialog.IsCurrent)
                                    ObjectEditor.inst.RenderIndex(beatmapObject);

                                break;
                            }
                        case TimelineReferenceType.PrefabObject: {
                                var prefabObject = GetData<PrefabObject>();
                                var index = GameData.Current.prefabObjects.FindIndex(x => x == prefabObject);
                                if (index <= 0)
                                {
                                    EditorManager.inst.DisplayNotification("Could not move object back since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                                    return;
                                }

                                GameData.Current.prefabObjects.Move(index, index - 1);
                                EditorTimeline.inst.UpdateTransformIndex();
                                break;
                            }
                        case TimelineReferenceType.BackgroundObject: {
                                var backgroundObject = GetData<BackgroundObject>();
                                var index = GameData.Current.backgroundObjects.FindIndex(x => x == backgroundObject);
                                if (index <= 0)
                                {
                                    EditorManager.inst.DisplayNotification("Could not move object back since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                                    return;
                                }

                                GameData.Current.backgroundObjects.Move(index, index - 1);
                                EditorTimeline.inst.UpdateTransformIndex();
                                break;
                            }
                    }
                }),
                new ButtonFunction("Move Forwards", () =>
                {
                    switch (TimelineReference)
                    {
                        case TimelineReferenceType.BeatmapObject: {
                                var beatmapObject = GetData<BeatmapObject>();
                                var index = GameData.Current.beatmapObjects.FindIndex(x => x == beatmapObject);
                                if (index >= GameData.Current.beatmapObjects.Count - 1)
                                {
                                    EditorManager.inst.DisplayNotification("Could not move object forwards since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                                    return;
                                }

                                GameData.Current.beatmapObjects.Move(index, index + 1);
                                EditorTimeline.inst.UpdateTransformIndex();
                                if (ObjectEditor.inst.Dialog.IsCurrent)
                                    ObjectEditor.inst.RenderIndex(beatmapObject);

                                break;
                            }
                        case TimelineReferenceType.PrefabObject: {
                                var prefabObject = GetData<PrefabObject>();
                                var index = GameData.Current.prefabObjects.FindIndex(x => x == prefabObject);
                                if (index >= GameData.Current.prefabObjects.Count - 1)
                                {
                                    EditorManager.inst.DisplayNotification("Could not move object forwards since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                                    return;
                                }

                                GameData.Current.prefabObjects.Move(index, index + 1);
                                EditorTimeline.inst.UpdateTransformIndex();
                                break;
                            }
                        case TimelineReferenceType.BackgroundObject: {
                                var backgroundObject = GetData<BackgroundObject>();
                                var index = GameData.Current.backgroundObjects.FindIndex(x => x == backgroundObject);
                                if (index >= GameData.Current.backgroundObjects.Count - 1)
                                {
                                    EditorManager.inst.DisplayNotification("Could not move object forwards since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                                    return;
                                }

                                GameData.Current.backgroundObjects.Move(index, index + 1);
                                EditorTimeline.inst.UpdateTransformIndex();
                                break;
                            }
                    }
                }),
                new ButtonFunction("Move to Back", () =>
                {
                    switch (TimelineReference)
                    {
                        case TimelineReferenceType.BeatmapObject: {
                                var beatmapObject = GetData<BeatmapObject>();
                                var index = GameData.Current.beatmapObjects.FindIndex(x => x == beatmapObject);
                                if (index <= 0)
                                {
                                    EditorManager.inst.DisplayNotification("Could not move object back since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                                    return;
                                }

                                GameData.Current.beatmapObjects.Move(index, 0);
                                EditorTimeline.inst.UpdateTransformIndex();
                                if (ObjectEditor.inst.Dialog.IsCurrent)
                                    ObjectEditor.inst.RenderIndex(beatmapObject);

                                break;
                            }
                        case TimelineReferenceType.PrefabObject: {
                                var prefabObject = GetData<PrefabObject>();
                                var index = GameData.Current.prefabObjects.FindIndex(x => x == prefabObject);
                                if (index <= 0)
                                {
                                    EditorManager.inst.DisplayNotification("Could not move object back since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                                    return;
                                }

                                GameData.Current.prefabObjects.Move(index, 0);
                                EditorTimeline.inst.UpdateTransformIndex();
                                break;
                            }
                        case TimelineReferenceType.BackgroundObject: {
                                var backgroundObject = GetData<BackgroundObject>();
                                var index = GameData.Current.backgroundObjects.FindIndex(x => x == backgroundObject);
                                if (index <= 0)
                                {
                                    EditorManager.inst.DisplayNotification("Could not move object back since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                                    return;
                                }

                                GameData.Current.backgroundObjects.Move(index, 0);
                                EditorTimeline.inst.UpdateTransformIndex();
                                break;
                            }
                    }
                }),
                new ButtonFunction("Move to Front", () =>
                {
                    switch (TimelineReference)
                    {
                        case TimelineReferenceType.BeatmapObject: {
                                var beatmapObject = GetData<BeatmapObject>();
                                var index = GameData.Current.beatmapObjects.FindIndex(x => x == beatmapObject);
                                if (index >= GameData.Current.beatmapObjects.Count - 1)
                                {
                                    EditorManager.inst.DisplayNotification("Could not move object forwards since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                                    return;
                                }

                                GameData.Current.beatmapObjects.Move(index, GameData.Current.beatmapObjects.Count - 1);
                                EditorTimeline.inst.UpdateTransformIndex();
                                if (ObjectEditor.inst.Dialog.IsCurrent)
                                    ObjectEditor.inst.RenderIndex(beatmapObject);

                                break;
                            }
                        case TimelineReferenceType.PrefabObject: {
                                var prefabObject = GetData<PrefabObject>();
                                var index = GameData.Current.prefabObjects.FindIndex(x => x == prefabObject);
                                if (index >= GameData.Current.prefabObjects.Count - 1)
                                {
                                    EditorManager.inst.DisplayNotification("Could not move object forwards since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                                    return;
                                }

                                GameData.Current.prefabObjects.Move(index, GameData.Current.prefabObjects.Count - 1);
                                EditorTimeline.inst.UpdateTransformIndex();
                                break;
                            }
                        case TimelineReferenceType.BackgroundObject: {
                                var backgroundObject = GetData<BackgroundObject>();
                                var index = GameData.Current.backgroundObjects.FindIndex(x => x == backgroundObject);
                                if (index >= GameData.Current.backgroundObjects.Count - 1)
                                {
                                    EditorManager.inst.DisplayNotification("Could not move object forwards since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                                    return;
                                }

                                GameData.Current.backgroundObjects.Move(index, GameData.Current.backgroundObjects.Count - 1);
                                EditorTimeline.inst.UpdateTransformIndex();
                                break;
                            }
                    }
                })
                );
        }

        public void ShowColorContextMenu(InputField inputField, string currentHexColor)
        {
            EditorContextMenu.inst.ShowContextMenu(
                new ButtonFunction("Edit Color", () =>
                {
                    RTColorPicker.inst.Show(RTColors.HexToColor(currentHexColor),
                        (col, hex) =>
                        {
                            inputField.SetTextWithoutNotify(hex);
                        },
                        (col, hex) =>
                        {
                            CoreHelper.Log($"Set timeline object color: {hex}");
                            // set the input field's text empty so it notices there was a change
                            inputField.SetTextWithoutNotify(string.Empty);
                            inputField.text = hex;
                        }, () =>
                        {
                            inputField.SetTextWithoutNotify(currentHexColor);
                        });
                }),
                new ButtonFunction("Clear", () =>
                {
                    inputField.text = string.Empty;
                }),
                new ButtonFunction(true),
                new ButtonFunction("VG Red", () =>
                {
                    inputField.text = ObjectEditorData.RED;
                }),
                new ButtonFunction("VG Red Green", () =>
                {
                    inputField.text = ObjectEditorData.RED_GREEN;
                }),
                new ButtonFunction("VG Green", () =>
                {
                    inputField.text = ObjectEditorData.GREEN;
                }),
                new ButtonFunction("VG Green Blue", () =>
                {
                    inputField.text = ObjectEditorData.GREEN_BLUE;
                }),
                new ButtonFunction("VG Blue", () =>
                {
                    inputField.text = ObjectEditorData.BLUE;
                }),
                new ButtonFunction("VG Blue Red", () =>
                {
                    inputField.text = ObjectEditorData.RED_BLUE;
                }));
        }

        public Color GetColor()
        {
            var editorData = EditorData;
            return !editorData || string.IsNullOrEmpty(editorData.color) ? isPrefabObject ? GetData<PrefabObject>().GetPrefab().GetPrefabType().color : EditorConfig.Instance.TimelineObjectBaseColor.Value : RTColors.HexToColor(editorData.color);
        }

        public Color GetSelectedColor()
        {
            var editorData = EditorData;
            return !editorData || string.IsNullOrEmpty(editorData.selectedColor) ? ObjEditor.inst.SelectedColor : RTColors.HexToColor(editorData.selectedColor);
        }

        public Color GetTextColor()
        {
            var editorData = EditorData;
            return !editorData || string.IsNullOrEmpty(editorData.textColor) ? EditorConfig.Instance.TimelineObjectTextColor.Value : RTColors.HexToColor(editorData.textColor);
        }

        public string GetMarkColor()
        {
            var editorData = EditorData;
            return !editorData || string.IsNullOrEmpty(editorData.markColor) ? RTColors.ColorToHex(EditorConfig.Instance.TimelineObjectMarkColor.Value) : editorData.markColor;
        }

        #endregion

        /// <summary>
        /// What type <see cref="data"/> is.
        /// </summary>
        public enum TimelineReferenceType
        {
            /// <summary>
            /// If <see cref="data"/> is null.
            /// </summary>
            Null,
            /// <summary>
            /// If <see cref="data"/> is <seealso cref="Core.Data.Beatmap.BeatmapObject"/>.
            /// </summary>
            BeatmapObject,
            /// <summary>
            /// If <see cref="data"/> is <seealso cref="Core.Data.Beatmap.PrefabObject"/>.
            /// </summary>
            PrefabObject,
            /// <summary>
            /// If <see cref="data"/> is <seealso cref="Core.Data.Beatmap.BackgroundObject"/>
            /// </summary>
            BackgroundObject,
        }
    }
}
