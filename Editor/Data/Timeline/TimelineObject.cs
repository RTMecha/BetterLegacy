using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

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

namespace BetterLegacy.Editor.Data.Timeline
{
    /// <summary>
    /// Represents an object in the editor timeline.
    /// </summary>
    public class TimelineObject : Exists, ISelectable
    {
        public TimelineObject(IEditable data)
        {
            this.data = data;
            TimelineReference = GetTimelineReferenceType(data);
            isBeatmapObject = TimelineReference == TimelineReferenceType.BeatmapObject;
            isPrefabObject = TimelineReference == TimelineReferenceType.PrefabObject;
            isBackgroundObject = TimelineReference == TimelineReferenceType.BackgroundObject;
        }

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

        /// <summary>
        /// The name text of the timeline object.
        /// </summary>
        public TextMeshProUGUI Text { get; set; }

        #endregion

        #region Data

        /// <summary>
        /// Gets the ID of the object.
        /// </summary>
        public string ID => data?.ID;

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
            get => EditorData.Bin;
            set => EditorData.Bin = value;
        }

        /// <summary>
        /// Gets the total life time of the object.
        /// </summary>
        public float Length => TimelineReference switch
        {
            TimelineReferenceType.BeatmapObject => TryGetEditorGroup(out EditorGroup editorGroup) && editorGroup.collapsedType == EditorGroup.CollapsedType.Collapsed ? EditorConfig.Instance.TimelineObjectCollapseLength.Value : GetData<BeatmapObject>().GetObjectLifeLength(collapse: true),
            TimelineReferenceType.PrefabObject => TryGetEditorGroup(out EditorGroup editorGroup) && editorGroup.collapsedType == EditorGroup.CollapsedType.Collapsed ? EditorConfig.Instance.TimelineObjectCollapseLength.Value : GetData<PrefabObject>().GetObjectLifeLength(collapse: true),
            TimelineReferenceType.BackgroundObject => TryGetEditorGroup(out EditorGroup editorGroup) && editorGroup.collapsedType == EditorGroup.CollapsedType.Collapsed ? EditorConfig.Instance.TimelineObjectCollapseLength.Value : GetData<BackgroundObject>().GetObjectLifeLength(collapse: true),
            _ => 0f
        };

        /// <summary>
        /// Gets and sets the layer of the object.
        /// </summary>
        public int Layer
        {
            get => EditorData.Layer;
            set
            {
                EditorData.Layer = value;
                RenderVisibleState();
            }
        }

        /// <summary>
        /// Gets and sets the locked state of the object.
        /// </summary>
        public bool Locked
        {
            get => EditorData.locked;
            set => EditorData.locked = value;
        }

        /// <summary>
        /// Gets and sets the collapsed state of the object.
        /// </summary>
        public bool Collapse
        {
            get => EditorData.collapse;
            set => EditorData.collapse = value;
        }

        /// <summary>
        /// Gets and sets the hidden state of the object.
        /// </summary>
        public bool Hidden
        {
            get => EditorData.hidden;
            set => EditorData.hidden = value;
        }

        /// <summary>
        /// Gets and sets the selectable in preview state of the object.
        /// </summary>
        public bool SelectableInPreview
        {
            get => EditorData.selectable;
            set => EditorData.selectable = value;
        }

        /// <summary>
        /// Gets the objects' editor data.
        /// </summary>
        public ObjectEditorData EditorData => data?.EditorData;

        /// <summary>
        /// If the timeline object is on the currently viewed editor layer.
        /// </summary>
        public bool IsCurrentLayer => EditorTimeline.inst.layerType == EditorTimeline.LayerType.Objects && Layer == EditorTimeline.inst.Layer && (!TryGetEditorGroup(out EditorGroup editorGroup) || editorGroup.collapsedType != EditorGroup.CollapsedType.Hidden);

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
        /// What kind of object <see cref="data"/> is.
        /// </summary>
        public TimelineReferenceType TimelineReference { get; private set; }

        /// <summary>
        /// If the timeline object data is prefabable.
        /// </summary>
        public bool IsPrefabable => data is IPrefabable;

        /// <summary>
        /// Group of the object in the editor.
        /// </summary>
        public string Group
        {
            get => EditorData.editorGroup;
            set => EditorData.editorGroup = value;
        }

        #endregion

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

        readonly IEditable data;
        int index;
        bool selected;
        string prefabID;
        string prefabInstanceID;
        Prefab cachedPrefab;

        #endregion

        #endregion

        #region Functions

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
        public void Init(bool update = true)
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

            storage.eventTrigger.triggers = new List<UnityEngine.EventSystems.EventTrigger.Entry>(4);
            storage.eventTrigger.triggers.Add(TriggerHelper.CreateBeatmapObjectTrigger(this));
            storage.eventTrigger.triggers.Add(TriggerHelper.CreateBeatmapObjectStartDragTrigger(this));
            storage.eventTrigger.triggers.Add(TriggerHelper.CreateBeatmapObjectEndDragTrigger(this));

            if (update)
            {
                Render();
                return;
            }
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
            if (!GameObject)
                return;

            string name = "object name";
            float startTime = 0f;
            float length = Length;

            var image = Image;

            Prefab prefab = GetPrefab();

            if (data != null)
                data.TimelineObject = this;

            if (isBeatmapObject && TryGetData(out BeatmapObject beatmapObject))
            {
                name = beatmapObject.name;
                startTime = beatmapObject.StartTime;

                image.type = EditorTimeline.inst.GetObjectTypePattern(beatmapObject.objectType);
                image.sprite = EditorTimeline.inst.GetObjectTypeSprite(beatmapObject.objectType);

                if (!prefab)
                    beatmapObject.RemovePrefabReference();
            }

            if (isPrefabObject && TryGetData(out PrefabObject prefabObject))
            {
                name = prefab.name;
                startTime = prefabObject.StartTime + prefab.offset;

                image.type = Image.Type.Simple;
                image.sprite = null;
            }

            if (isBackgroundObject && TryGetData(out BackgroundObject backgroundObject))
            {
                name = backgroundObject.name;
                startTime = backgroundObject.StartTime;

                image.type = Image.Type.Simple;
                image.sprite = null;

                if (!prefab)
                    backgroundObject.RemovePrefabReference();
            }

            RenderText(name);
            RenderIcons(prefab);
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
                            GameObject.SetActive(isCurrentLayer);

                        if (!isCurrentLayer)
                            return;

                        var beatmapObject = GetData<BeatmapObject>();
                        Prefab prefab = null;
                        if (!string.IsNullOrEmpty(beatmapObject.prefabID))
                        {
                            prefab = GetPrefab();
                            if (!prefab)
                                beatmapObject.RemovePrefabReference();
                        }

                        var color = selected ? GetSelectedColor() : GetColor();
                        if (!selected && prefab && (EditorConfig.Instance.PrioritzePrefabTypeColor.Value || string.IsNullOrEmpty(EditorData.color)))
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
                            prefab = GetPrefab();
                            if (!prefab)
                                backgroundObject.RemovePrefabReference();
                        }

                        var color = selected ? GetSelectedColor() : GetColor();
                        if (!selected && prefab && (EditorConfig.Instance.PrioritzePrefabTypeColor.Value || string.IsNullOrEmpty(EditorData.color)))
                            color = prefab.GetPrefabType().color;

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
                timeOffset = GetPrefab().offset;

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

            length = length <= EditorConfig.Instance.TimelineObjectCollapseLength.Value ? EditorConfig.Instance.TimelineObjectCollapseLength.Value * zoom : length * zoom;

            if (isPrefabObject && Collapse)
            {
                var prefab = GetPrefab();
                if (prefab)
                    time -= prefab.offset;
            }

            rectTransform.sizeDelta = new Vector2(length, 20f);
            rectTransform.anchoredPosition = new Vector2(time * zoom, (-20 * Mathf.Clamp(Bin, 0, EditorTimeline.inst.BinCount)));
            if (Hover)
                Hover.size = EditorConfig.Instance.TimelineObjectHoverSize.Value;
        }

        /// <summary>
        /// Updates the timeline objects' icons.
        /// </summary>
        /// <param name="prefabType">Prefab Type to retrieve a Prefab Type icon from.</param>
        public void RenderIcons(Prefab prefab = null)
        {
            var gameObject = GameObject;
            if (!gameObject)
                return;

            gameObject.transform.Find("icons/lock").gameObject.SetActive(Locked);
            gameObject.transform.Find("icons/dots").gameObject.SetActive(Collapse);
            var typeIcon = gameObject.transform.Find("icons/type").gameObject;

            var icon = prefab?.GetIcon();
            if (!EditorConfig.Instance.TimelineObjectPrefabIcon.Value || !icon)
                icon = prefab?.GetPrefabType()?.icon;
            var renderTypeIcon = icon && EditorConfig.Instance.TimelineObjectPrefabTypeIcon.Value;
            typeIcon.SetActive(renderTypeIcon);
            if (renderTypeIcon)
                typeIcon.transform.Find("type").GetComponent<Image>().sprite = icon;
        }

        public void ShowContextMenu()
        {
            EditorContextMenu.inst.ShowContextMenu(
                new ButtonElement("Select", () => EditorTimeline.inst.SetCurrentObject(this)),
                new ButtonElement("Add to Selection", () => EditorTimeline.inst.AddSelectedObject(this)),
                new ButtonElement("Create New", () => ObjectEditor.inst.CreateNewNormalObject()),
                new ButtonElement("Update Object", () =>
                {
                    if (isBeatmapObject)
                        RTLevel.Current?.UpdateObject(GetData<BeatmapObject>());
                    if (isPrefabObject)
                        RTLevel.Current?.UpdatePrefab(GetData<PrefabObject>());
                    if (isBackgroundObject)
                        RTLevel.Current?.UpdateBackgroundObject(GetData<BackgroundObject>());
                }),
                new SpacerElement(),
                new ButtonElement("Cut", () =>
                {
                    ObjectEditor.inst.CopyObjects();
                    EditorTimeline.inst.DeleteObjects();
                }),
                new ButtonElement("Copy", ObjectEditor.inst.CopyObjects),
                new ButtonElement("Paste", ObjectEditor.inst.PasteObject),
                new ButtonElement("Duplicate", () =>
                {
                    var offsetTime = EditorTimeline.inst.SelectedObjects.Min(x => x.Time);

                    ObjectEditor.inst.CopyObjects();
                    ObjectEditor.inst.PasteObject(offsetTime);
                }),
                new ButtonElement("Paste (Keep Prefab)", () => ObjectEditor.inst.PasteObject(0f, false)),
                new ButtonElement("Duplicate (Keep Prefab)", () =>
                {
                    var offsetTime = EditorTimeline.inst.SelectedObjects.Min(x => x.Time);

                    ObjectEditor.inst.CopyObjects();
                    ObjectEditor.inst.PasteObject(offsetTime, false);
                }),
                new ButtonElement("Delete", EditorTimeline.inst.DeleteObjects),
                new SpacerElement(),
                new ButtonElement("Hide", () =>
                {
                    foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                    {
                        timelineObject.Hidden = true;
                        switch (timelineObject.TimelineReference)
                        {
                            case TimelineReferenceType.BeatmapObject: {
                                    RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.HIDE);

                                    break;
                                }
                            case TimelineReferenceType.PrefabObject: {
                                    RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.HIDE);

                                    break;
                                }
                            case TimelineReferenceType.BackgroundObject: {
                                    RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.HIDE);

                                    break;
                                }
                        }
                    }
                }),
                new ButtonElement("Unhide", () =>
                {
                    foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                    {
                        timelineObject.Hidden = false;
                        switch (timelineObject.TimelineReference)
                        {
                            case TimelineReferenceType.BeatmapObject: {
                                    RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.HIDE);

                                    break;
                                }
                            case TimelineReferenceType.PrefabObject: {
                                    RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.HIDE);

                                    break;
                                }
                            case TimelineReferenceType.BackgroundObject: {
                                    RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.HIDE);

                                    break;
                                }
                        }
                    }
                }),
                new ButtonElement("Preview Selectable", () =>
                {
                    foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                    {
                        if (timelineObject.isBackgroundObject)
                            continue;

                        timelineObject.SelectableInPreview = true;
                        switch (timelineObject.TimelineReference)
                        {
                            case TimelineReferenceType.BeatmapObject: {
                                    RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.SELECTABLE);

                                    break;
                                }
                            case TimelineReferenceType.PrefabObject: {
                                    RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.SELECTABLE);

                                    break;
                                }
                        }
                    }
                }),
                new ButtonElement("Preview Unselectable", () =>
                {
                    foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                    {
                        if (timelineObject.isBackgroundObject)
                            continue;

                        timelineObject.SelectableInPreview = false;
                        switch (timelineObject.TimelineReference)
                        {
                            case TimelineReferenceType.BeatmapObject: {
                                    RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.SELECTABLE);

                                    break;
                                }
                            case TimelineReferenceType.PrefabObject: {
                                    RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.SELECTABLE);

                                    break;
                                }
                        }
                    }
                }),
                new SpacerElement(),
                new ButtonElement("Move Backwards", () =>
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
                new ButtonElement("Move Forwards", () =>
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
                new ButtonElement("Move to Back", () =>
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
                new ButtonElement("Move to Front", () =>
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

        public void ShowColorContextMenu(InputField inputField, string currentHexColor) => EditorContextMenu.inst.ShowContextMenu(EditorContextMenu.GetEditorColorFunctions(inputField, () => currentHexColor));

        public Color GetColor()
        {
            var editorData = EditorData;
            return !editorData || string.IsNullOrEmpty(editorData.color) ? isPrefabObject ? GetPrefab().GetPrefabType().color : EditorConfig.Instance.TimelineObjectBaseColor.Value : RTColors.HexToColor(editorData.color);
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

        Prefab GetPrefab()
        {
            if (!TryGetPrefabable(out IPrefabable prefabable))
                return null;

            if (prefabID != prefabable.PrefabID || prefabInstanceID != prefabable.PrefabInstanceID)
            {
                prefabID = prefabable.PrefabID;
                prefabInstanceID = prefabable.PrefabInstanceID;
                cachedPrefab = prefabable.GetPrefab();
            }

            return cachedPrefab;
        }

        /// <summary>
        /// Sets the name of the object.
        /// </summary>
        /// <param name="name">Name to set.</param>
        public void SetName(string name)
        {
            switch (TimelineReference)
            {
                case TimelineReferenceType.BeatmapObject: {
                        GetData<BeatmapObject>().name = name;
                        break;
                    }
                case TimelineReferenceType.BackgroundObject: {
                        GetData<BeatmapObject>().name = name;
                        break;
                    }
            }
            RenderText(Name);
        }

        /// <summary>
        /// Tries to get the associated editor group.
        /// </summary>
        /// <param name="editorGroup">Editor group result.</param>
        /// <returns>Returns true if an editor group is found, otherwise returns false.</returns>
        public bool TryGetEditorGroup(out EditorGroup editorGroup)
        {
            if (!RTEditor.inst.editorInfo)
            {
                editorGroup = null;
                return false;
            }
            return RTEditor.inst.editorInfo.TryGetGroup(Group, out editorGroup);
        }

        /// <summary>
        /// Updates the runtime object.
        /// </summary>
        public void UpdateObject()
        {
            switch (TimelineReference)
            {
                case TimelineReferenceType.BeatmapObject: {
                        if (TryGetData(out BeatmapObject beatmapObject))
                            beatmapObject.GetParentRuntime()?.UpdateObject(beatmapObject);
                        break;
                    }
                case TimelineReferenceType.BackgroundObject: {
                        if (TryGetData(out BackgroundObject backgroundObject))
                            backgroundObject.GetParentRuntime()?.UpdateBackgroundObject(backgroundObject);
                        break;
                    }
                case TimelineReferenceType.PrefabObject: {
                        if (TryGetData(out PrefabObject prefabObject))
                            prefabObject.GetParentRuntime()?.UpdatePrefab(prefabObject);
                        break;
                    }
            }
        }

        /// <summary>
        /// Updates a specific value of the runtime object.
        /// </summary>
        /// <param name="context">Context of the value to update.</param>
        public void UpdateObject(string context)
        {
            if (string.IsNullOrEmpty(context))
                UpdateObject();

            switch (TimelineReference)
            {
                case TimelineReferenceType.BeatmapObject: {
                        if (TryGetData(out BeatmapObject beatmapObject))
                            beatmapObject.GetParentRuntime()?.UpdateObject(beatmapObject, context);
                        break;
                    }
                case TimelineReferenceType.BackgroundObject: {
                        if (TryGetData(out BackgroundObject backgroundObject))
                            backgroundObject.GetParentRuntime()?.UpdateBackgroundObject(backgroundObject, context);
                        break;
                    }
                case TimelineReferenceType.PrefabObject: {
                        if (TryGetData(out PrefabObject prefabObject))
                            prefabObject.GetParentRuntime()?.UpdatePrefab(prefabObject, context);
                        break;
                    }
            }
        }

        public override string ToString() => data != null ? data.ToString() : base.ToString();

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
