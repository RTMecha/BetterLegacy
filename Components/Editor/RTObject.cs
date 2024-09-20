using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Editor;
using BetterLegacy.Editor.Managers;
using LSFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BetterLegacy.Components.Editor
{
    /// <summary>
    /// Component for selecting and dragging objects.
    /// </summary>
    public class RTObject : MonoBehaviour
    {
        /// <summary>
        /// If user is only looking at one object, then allow drag.
        /// </summary>
        public bool CanDrag => ObjectEditor.inst.SelectedObjectCount < 2;

        /// <summary>
        /// If dragging is enabled via <see cref="Configs.EditorConfig.ObjectDraggerEnabled"/>.
        /// </summary>
        public static bool Enabled { get; set; }

        /// <summary>
        /// If dragging prioritizes creating keyframes.
        /// </summary>
        public static bool CreateKeyframe { get; set; }

        /// <summary>
        /// If a tooltip should display when the mouse is hovered over the object.
        /// </summary>
        public static bool TipEnabled { get; set; }

        /// <summary>
        /// The BeatmapObject reference.
        /// </summary>
        public BeatmapObject beatmapObject;

        Renderer renderer;

        #region Highlighting

        bool hovered;

        /// <summary>
        /// Color to add when object is hovered if <see cref="HighlightObjects"/> is true.
        /// </summary>
        public static Color HighlightColor { get; set; }

        /// <summary>
        /// Color to add when object is hovered if <see cref="HighlightObjects"/> is true AND when shift is being held.
        /// </summary>
        public static Color HighlightDoubleColor { get; set; }

        /// <summary>
        /// If object should highlight when hovered.
        /// </summary>
        public static bool HighlightObjects { get; set; }

        /// <summary>
        /// Amount of opacity to use when the object is not on the current editor layer.
        /// </summary>
        public static float LayerOpacity { get; set; }

        /// <summary>
        /// If the object should set <see cref="LayerOpacity"/> as the current objects' opacity if the object is not on the current editor layer.
        /// </summary>
        public static bool ShowObjectsOnlyOnLayer { get; set; }

        #endregion

        #region Dragging

        PrefabObject prefabObjectToDrag;
        bool dragging;

        bool setKeyframeValues;
        Vector2 dragKeyframeValues;
        public EventKeyframe selectedKeyframe;
        Vector2 dragOffset;
        Axis firstDirection = Axis.Static;

        /// <summary>
        /// The axis dragging starts from.
        /// </summary>
        public enum Axis
        {
            Static,
            PosX,
            PosY,
            NegX,
            NegY
        }

        #endregion

        void Awake()
        {
            var renderer = GetComponent<Renderer>();
            if (renderer)
                this.renderer = renderer;
        }

        /// <summary>
        /// Assigns a BeatmapObjects' RTObject references.
        /// </summary>
        /// <param name="beatmapObject"></param>
        public void SetObject(BeatmapObject beatmapObject)
        {
            beatmapObject.RTObject = this;
            this.beatmapObject = beatmapObject;
        }

        void OnMouseUp()
        {
            dragging = false;
            selectedKeyframe = null;
            setKeyframeValues = false;
            firstDirection = Axis.Static;
        }

        void OnMouseDown()
        {
            if (!CoreHelper.IsEditing || CoreHelper.IsUsingInputField || EventSystem.current.IsPointerOverGameObject())
                return;

            startDragTime = Time.time;

            // select object if picker is not currently active.
            if (!RTEditor.inst.parentPickerEnabled && !RTEditor.inst.prefabPickerEnabled)
            {
                TimelineObject timelineObject = ObjectEditor.inst.GetTimelineObject(beatmapObject);
                ObjectEditor.inst.RenderTimelineObject(timelineObject);
                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    ObjectEditor.inst.SetCurrentObject(timelineObject);
                    return;
                }

                ObjectEditor.inst.AddSelectedObject(timelineObject);

                return;
            }

            var currentSelection = ObjectEditor.inst.CurrentSelection;
            var selectedObjects = ObjectEditor.inst.SelectedObjects;

            // prefab assign picker
            if (RTEditor.inst.prefabPickerEnabled)
            {
                if (string.IsNullOrEmpty(beatmapObject.prefabInstanceID))
                {
                    EditorManager.inst.DisplayNotification("Object is not assigned to a prefab!", 2f, EditorManager.NotificationType.Error);
                    return;
                }

                if (RTEditor.inst.selectingMultiple)
                {
                    foreach (var otherTimelineObject in selectedObjects.Where(x => x.IsBeatmapObject))
                    {
                        var otherBeatmapObject = otherTimelineObject.GetData<BeatmapObject>();

                        otherBeatmapObject.prefabID = beatmapObject.prefabID;
                        otherBeatmapObject.prefabInstanceID = beatmapObject.prefabInstanceID;

                        ObjectEditor.inst.RenderTimelineObject(otherTimelineObject);
                    }
                }
                else if (currentSelection.IsBeatmapObject)
                {
                    var currentBeatmapObject = currentSelection.GetData<BeatmapObject>();

                    currentBeatmapObject.prefabID = beatmapObject.prefabID;
                    currentBeatmapObject.prefabInstanceID = beatmapObject.prefabInstanceID;

                    ObjectEditor.inst.RenderTimelineObject(currentSelection);
                    CoreHelper.StartCoroutine(ObjectEditor.RefreshObjectGUI(currentBeatmapObject));
                }

                RTEditor.inst.prefabPickerEnabled = false;

                return;
            }

            if (beatmapObject.fromPrefab || !RTEditor.inst.parentPickerEnabled)
                return;

            // parent picker multiple
            if (RTEditor.inst.selectingMultiple)
            {
                bool success = false;
                foreach (var otherTimelineObject in selectedObjects)
                {
                    if (otherTimelineObject.IsPrefabObject)
                    {
                        var prefabObject = otherTimelineObject.GetData<PrefabObject>();
                        prefabObject.parent = beatmapObject.id;
                        Updater.UpdatePrefab(prefabObject);

                        success = true;
                        continue;
                    }
                    success = SetParent(otherTimelineObject, beatmapObject);
                }

                if (!success)
                    EditorManager.inst.DisplayNotification("Cannot set parent to child / self!", 1f, EditorManager.NotificationType.Warning);
                else
                    RTEditor.inst.parentPickerEnabled = false;

                return;
            }

            // assign parent to prefab
            if (currentSelection.IsPrefabObject)
            {
                var prefabObject = currentSelection.GetData<PrefabObject>();
                prefabObject.parent = beatmapObject.id;
                Updater.UpdatePrefab(prefabObject);
                PrefabEditor.inst.OpenPrefabDialog();
                RTEditor.inst.parentPickerEnabled = false;

                return;
            }

            // set single parent
            var tryParent = SetParent(currentSelection, beatmapObject);

            if (!tryParent)
                EditorManager.inst.DisplayNotification("Cannot set parent to child / self!", 1f, EditorManager.NotificationType.Warning);
            else
                RTEditor.inst.parentPickerEnabled = false;
        }

        /// <summary>
        /// Tries to set an objects' parent. If the parent the user is trying to assign an object to a child of the object, then don't set parent.
        /// </summary>
        /// <param name="currentSelection"></param>
        /// <param name="beatmapObjectToParentTo"></param>
        /// <returns></returns>
        public static bool SetParent(TimelineObject currentSelection, BeatmapObject beatmapObjectToParentTo)
        {
            var dictionary = new Dictionary<string, bool>();
            var beatmapObjects = GameData.Current.beatmapObjects;

            foreach (var obj in beatmapObjects)
            {
                bool canParent = true;
                if (!string.IsNullOrEmpty(obj.parent))
                {
                    string parentID = currentSelection.ID;
                    while (!string.IsNullOrEmpty(parentID))
                    {
                        if (parentID == obj.parent)
                        {
                            canParent = false;
                            break;
                        }

                        int index = beatmapObjects.FindIndex(x => x.parent == parentID);
                        parentID = index != -1 ? beatmapObjects[index].id : null;
                    }
                }

                dictionary[obj.id] = canParent;
            }

            dictionary[currentSelection.ID] = false;

            var shouldParent = dictionary.TryGetValue(beatmapObjectToParentTo.id, out bool value) && value;

            if (shouldParent)
            {
                currentSelection.GetData<BeatmapObject>().parent = beatmapObjectToParentTo.id;
                var bm = currentSelection.GetData<BeatmapObject>();
                Updater.UpdateObject(bm);
                ObjectEditor.inst.RenderParent(bm);
            }

            return shouldParent;
        }

        void OnMouseEnter()
        {
            hovered = true;

            if (!TipEnabled || !CoreHelper.IsEditing)
                return;

            DataManager.Language enumTmp = DataManager.inst.GetCurrentLanguageEnum();
            int num = tooltipLanguages.FindIndex(x => x.language == enumTmp);
            if (num != -1)
            {
                var tooltip = tooltipLanguages[num];
                EditorManager.inst.SetTooltip(tooltip.keys, tooltip.desc, tooltip.hint);
                return;
            }
            EditorManager.inst.SetTooltip(null, "No tooltip added yet!", gameObject.name);
        }

        void OnMouseExit()
        {
            hovered = false;
        }

        void OnMouseDrag()
        {
            if (beatmapObject.fromPrefab)
            {
                var currentSelection = ObjectEditor.inst.CurrentSelection;

                if (!currentSelection.IsPrefabObject || currentSelection.ID != beatmapObject.prefabInstanceID)
                    return;

                prefabObjectToDrag = currentSelection.GetData<PrefabObject>();

                selectedKeyframe = (EventKeyframe)prefabObjectToDrag.events[0];

                var vector = new Vector3(Input.mousePosition.x, Input.mousePosition.y, transform.localPosition.z);
                var vector2 = Camera.main.ScreenToWorldPoint(vector);
                var vector3 = new Vector3((float)((int)vector2.x), (float)((int)vector2.y), transform.localPosition.z);

                dragging = true;

                Drag(vector2, vector3);

                return;
            }

            dragTime = Time.time;
            if (CoreHelper.IsEditing && dragTime > startDragTime + 0.1f && CanDrag && Enabled && !EventSystem.current.IsPointerOverGameObject())
            {
                var vector = new Vector3(Input.mousePosition.x, Input.mousePosition.y, transform.localPosition.z);
                var vector2 = Camera.main.ScreenToWorldPoint(vector);
                var vector3 = new Vector3((float)((int)vector2.x), (float)((int)vector2.y), transform.localPosition.z);

                if (!dragging && selectedKeyframe == null)
                {
                    dragging = true;
                    selectedKeyframe = SetCurrentKeyframe(0, beatmapObject);
                }

                Drag(vector2, vector3);
            }
        }

        void Drag(Vector3 vector2, Vector3 vector3)
        {
            if (selectedKeyframe == null)
                return;

            if (!setKeyframeValues)
            {
                setKeyframeValues = true;
                dragKeyframeValues = new Vector2(selectedKeyframe.eventValues[0], selectedKeyframe.eventValues[1]);
                dragOffset = Input.GetKey(KeyCode.LeftShift) ? vector3 : vector2;
            }

            var finalVector = Input.GetKey(KeyCode.LeftShift) ? vector3 : vector2;

            if (Input.GetKey(KeyCode.LeftControl) && firstDirection == Axis.Static)
            {
                if (dragOffset.x > finalVector.x)
                    firstDirection = Axis.PosX;

                if (dragOffset.x < finalVector.x)
                    firstDirection = Axis.NegX;

                if (dragOffset.y > finalVector.y)
                    firstDirection = Axis.PosY;

                if (dragOffset.y < finalVector.y)
                    firstDirection = Axis.NegY;
            }

            if (firstDirection == Axis.Static || firstDirection == Axis.PosX || firstDirection == Axis.NegX)
                selectedKeyframe.eventValues[0] = dragKeyframeValues.x - dragOffset.x + (Input.GetKey(KeyCode.LeftShift) ? vector3.x : vector2.x);
            if (firstDirection == Axis.Static || firstDirection == Axis.PosY || firstDirection == Axis.NegY)
                selectedKeyframe.eventValues[1] = dragKeyframeValues.y - dragOffset.y + (Input.GetKey(KeyCode.LeftShift) ? vector3.y : vector2.y);

            if (prefabObjectToDrag != null)
                Updater.UpdatePrefab(prefabObjectToDrag, "Offset");
            else
                Updater.UpdateObject(beatmapObject, "Keyframes");
        }

        float startDragTime;
        float dragTime;

        /// <summary>
        /// Sets the current keyframe to be dragging.
        /// </summary>
        /// <param name="type">Type to drag.</param>
        /// <param name="beatmapObject">Beatmap Object to affect.</param>
        /// <param name="prefabObject">Prefab Object to affect.</param>
        /// <returns></returns>
        public static EventKeyframe SetCurrentKeyframe(int type, BeatmapObject beatmapObject = null, PrefabObject prefabObject = null)
        {
            if (prefabObject != null)
                return (EventKeyframe)prefabObject.events[type];

            var timeOffset = AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime;
            int nextIndex = beatmapObject.events[type].FindIndex(x => x.eventTime >= timeOffset);
            if (nextIndex < 0)
                nextIndex = beatmapObject.events[type].Count - 1;

            int index;
            EventKeyframe selected;
            if (beatmapObject.events[type].Has(x => x.eventTime > timeOffset - 0.1f && x.eventTime < timeOffset + 0.1f))
            {
                selected = (EventKeyframe)beatmapObject.events[type].Find(x => x.eventTime > timeOffset - 0.1f && x.eventTime < timeOffset + 0.1f);
                index = beatmapObject.events[type].FindIndex(x => x.eventTime > timeOffset - 0.1f && x.eventTime < timeOffset + 0.1f);
                AudioManager.inst.SetMusicTime(selected.eventTime + beatmapObject.StartTime);
            }
            else if (CreateKeyframe)
            {
                selected = EventKeyframe.DeepCopy((EventKeyframe)beatmapObject.events[type][nextIndex]);
                selected.eventTime = timeOffset;
                index = beatmapObject.events[type].Count;
                beatmapObject.events[type].Add(selected);
            }
            else
            {
                index = beatmapObject.events[type].FindLastIndex(x => x.eventTime < timeOffset);
                selected = (EventKeyframe)beatmapObject.events[type][index];
            }

            ObjectEditor.inst.RenderKeyframes(beatmapObject);
            ObjectEditor.inst.SetCurrentKeyframe(beatmapObject, type, index, false, false);

            return selected;
        }

        void Update()
        {
            if (!CoreHelper.IsEditing)
            {
                hovered = false;
                return;
            }

            if (!beatmapObject)
                return;

            var currentSelection = ObjectEditor.inst.CurrentSelection;

            if (!beatmapObject.fromPrefab && currentSelection.ID == beatmapObject.id)
            {
                GameStorageManager.inst.objectDragger.position = new Vector3(transform.parent.position.x, transform.parent.position.y, transform.parent.position.z - 10f);
                GameStorageManager.inst.objectDragger.rotation = transform.parent.rotation;
            }

            if (beatmapObject.fromPrefab && currentSelection.ID == beatmapObject.prefabInstanceID)
            {
                var prefabObject = currentSelection.GetData<PrefabObject>();
                GameStorageManager.inst.objectDragger.position = new Vector3(prefabObject.events[0].eventValues[0], prefabObject.events[0].eventValues[1], -90f);
                GameStorageManager.inst.objectDragger.rotation = Quaternion.Euler(0f, 0f, prefabObject.events[2].eventValues[0]);
            }

            SetTooltip();

            if (beatmapObject.fromPrefab)
            {
                if (string.IsNullOrEmpty(beatmapObject.prefabInstanceID))
                    return;

                var prefabObject = GameData.Current.prefabObjects.Find(x => x.ID == beatmapObject.prefabInstanceID);

                if (prefabObject == null || !(HighlightObjects && hovered || ShowObjectsOnlyOnLayer && prefabObject.editorData.layer != RTEditor.inst.Layer))
                    return;

                var beatmapObjects = GameData.Current.beatmapObjects
                    .FindAll(x =>
                                x.fromPrefab &&
                                x.prefabInstanceID == beatmapObject.prefabInstanceID &&
                                x.objectType != BeatmapObject.ObjectType.Empty);

                for (int i = 0; i < beatmapObjects.Count; i++)
                {
                    var bm = beatmapObjects[i];
                    if (Updater.TryGetObject(bm, out Core.Optimization.Objects.LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.Renderer)
                    {
                        var renderer = levelObject.visualObject.Renderer;
                        if (!renderer || !renderer.material || !renderer.material.HasProperty("_Color"))
                            continue;

                        SetHoverColor(renderer);
                        SetLayerColor(renderer, prefabObject.editorData.Layer);
                    }
                }

                return;
            }

            if (beatmapObject.editorData == null)
                return;

            SetColor(renderer, beatmapObject.editorData.layer);
        }

        void FixedUpdate()
        {
            if (!dragging)
                return;

            if (!beatmapObject.fromPrefab)
                ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);
            else if (beatmapObject.fromPrefab && prefabObjectToDrag != null)
                RTPrefabEditor.inst.RenderPrefabObjectDialog(prefabObjectToDrag);
        }

        void SetColor(Renderer renderer, int layer)
        {
            if (!renderer || !renderer.material || !renderer.material.HasProperty("_Color"))
                return;

            SetHoverColor(renderer);
            SetLayerColor(renderer, layer);
        }

        void SetHoverColor(Renderer renderer)
        {
            if (!HighlightObjects || !hovered)
                return;

            var color = Input.GetKey(KeyCode.LeftShift) ? new Color(
                renderer.material.color.r > 0.9f ? -HighlightDoubleColor.r : HighlightDoubleColor.r,
                renderer.material.color.g > 0.9f ? -HighlightDoubleColor.g : HighlightDoubleColor.g,
                renderer.material.color.b > 0.9f ? -HighlightDoubleColor.b : HighlightDoubleColor.b,
                0f) : new Color(
                renderer.material.color.r > 0.9f ? -HighlightColor.r : HighlightColor.r,
                renderer.material.color.g > 0.9f ? -HighlightColor.g : HighlightColor.g,
                renderer.material.color.b > 0.9f ? -HighlightColor.b : HighlightColor.b,
                0f);

            renderer.material.color += color;
        }

        void SetLayerColor(Renderer renderer, int layer)
        {
            if (ShowObjectsOnlyOnLayer && layer != EditorManager.inst.layer)
                renderer.material.color = LSColors.fadeColor(renderer.material.color, renderer.material.color.a * LayerOpacity);
        }

        void SetTooltip()
        {
            if (!EditorManager.inst.showHelp || !beatmapObject || EventSystem.current.IsPointerOverGameObject())
                return;

            TipEnabled = true;

            if (tooltipLanguages.Count == 0)
                tooltipLanguages.Add(TooltipHelper.NewTooltip(beatmapObject.name + " [ " + beatmapObject.StartTime + " ]", "", new List<string>()));

            string parent = !string.IsNullOrEmpty(beatmapObject.parent) ?
                 "<br>P: " + beatmapObject.parent + " (" + beatmapObject.GetParentType() + ")" :
                 "<br>P: No Parent" + " (" + beatmapObject.GetParentType() + ")";

            string text = beatmapObject.shape switch
            {
                4 => "<br>S: Text",
                6 => "<br>S: Image",
                _ => "<br>S: " + CoreHelper.GetShape(beatmapObject.shape, beatmapObject.shapeOption).Replace("eight_circle", "eighth_circle").Replace("eigth_circle_outline", "eighth_circle_outline"),
            };
            if (!string.IsNullOrEmpty(beatmapObject.text))
                text += "<br>T: " + beatmapObject.text;

            string ptr =
                beatmapObject.fromPrefab && !string.IsNullOrEmpty(beatmapObject.prefabID) && !string.IsNullOrEmpty(beatmapObject.prefabInstanceID) ?
                "<br>PID: " + beatmapObject.prefabID + " | " + beatmapObject.prefabInstanceID : "<br>Not from prefab";

            Color col = LSColors.transparent;
            if (renderer.material.HasProperty("_Color"))
                col = renderer.material.color;

            if (tooltipLanguages[0].desc != "N/ST: " + beatmapObject.name + " [ " + beatmapObject.StartTime + " ]")
                tooltipLanguages[0].desc = "N/ST: " + beatmapObject.name + " [ " + beatmapObject.StartTime + " ]";

            var result = "ID: {" + beatmapObject.id + "}" +
                parent +
                "<br>O: {X: " + beatmapObject.origin.x + ", Y: " + beatmapObject.origin.y + "}" +
                text +
                "<br>D: " + beatmapObject.Depth +
                "<br>ED: {L: " + beatmapObject.editorData.layer + ", B: " + beatmapObject.editorData.Bin + "}" +
                "<br>POS: {X: " + transform.position.x + ", Y: " + transform.position.y + ", Z: " + transform.position.z + "}" +
                "<br>SCA: {X: " + transform.localScale.x + ", Y: " + transform.localScale.y + "}" +
                "<br>ROT: " + transform.eulerAngles.z +
                "<br>COL: " + CoreHelper.ColorToHex(col) +
                ptr;
            if (tooltipLanguages[0].hint != result)
                tooltipLanguages[0].hint = result;
        }

        public List<HoverTooltip.Tooltip> tooltipLanguages = new List<HoverTooltip.Tooltip>();
    }
}
