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
    /// Component for selecting and dragging objects. Still needs a ton of work though.
    /// </summary>
    public class RTObject : MonoBehaviour
    {
        public bool CanDrag => ObjectEditor.inst.SelectedObjectCount < 2;
        public static bool Enabled { get; set; }
        public static bool CreateKeyframe { get; set; }

        public bool Selected
        {
            get
            {
                var timelineObject = ObjectEditor.inst.GetTimelineObject(beatmapObject);
                return timelineObject.ID == beatmapObject.id && timelineObject.selected;
            }
        }

        public static bool TipEnabled { get; set; }
        public string id;

        public BeatmapObject beatmapObject;

        Renderer renderer;

        #region Highlighting

        public bool hovered;

        public static Color HighlightColor { get; set; }
        public static Color HighlightDoubleColor { get; set; }
        public static bool HighlightObjects { get; set; }
        public static float LayerOpacity { get; set; }
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

        public enum Axis
        {
            Static,
            PosX,
            PosY,
            NegX,
            NegY
        }

        #endregion

        #region Delegates

        public Action onMouseDown;
        public Action onMouseUp;
        public Action onMouseEnter;
        public Action onMouseExit;
        public Action onMouseDrag;

        #endregion

        void Awake()
        {
            var renderer = GetComponent<Renderer>();
            if (renderer)
                this.renderer = renderer;
        }

        public void GenerateDraggers()
        {

        }

        public void SetObject(BeatmapObject beatmapObject)
        {
            id = beatmapObject.id;
            beatmapObject.RTObject = this;
            this.beatmapObject = beatmapObject;
        }

        void OnMouseUp()
        {
            onMouseUp?.Invoke();
            dragging = false;
            selectedKeyframe = null;
            setKeyframeValues = false;
            firstDirection = Axis.Static;
        }

        void OnMouseDown()
        {
            onMouseDown?.Invoke();
            if (EditorManager.inst && EditorManager.inst.isEditing && !string.IsNullOrEmpty(id) && !CoreHelper.IsUsingInputField && !EventSystem.current.IsPointerOverGameObject())
            {
                startDragTime = Time.time;
                {
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

                    if (!RTEditor.inst.parentPickerEnabled)
                        return;

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

                    if (currentSelection.IsPrefabObject)
                    {
                        var prefabObject = currentSelection.GetData<PrefabObject>();
                        prefabObject.parent = beatmapObject.id;
                        Updater.UpdatePrefab(prefabObject);
                        PrefabEditor.inst.OpenPrefabDialog();
                        RTEditor.inst.parentPickerEnabled = false;

                        return;
                    }

                    var tryParent = SetParent(currentSelection, beatmapObject);

                    if (!tryParent)
                        EditorManager.inst.DisplayNotification("Cannot set parent to child / self!", 1f, EditorManager.NotificationType.Warning);
                    else
                        RTEditor.inst.parentPickerEnabled = false;
                }
            }
        }

        public static bool SetParent(TimelineObject currentSelection, BeatmapObject beatmapObjectToParentTo)
        {
            var dictionary = new Dictionary<string, bool>();

            foreach (var obj in DataManager.inst.gameData.beatmapObjects)
            {
                bool flag = true;
                if (!string.IsNullOrEmpty(obj.parent))
                {
                    string parentID = currentSelection.ID;
                    while (!string.IsNullOrEmpty(parentID))
                    {
                        if (parentID == obj.parent)
                        {
                            flag = false;
                            break;
                        }
                        int num2 = DataManager.inst.gameData.beatmapObjects.FindIndex(x => x.parent == parentID);
                        if (num2 != -1)
                        {
                            parentID = DataManager.inst.gameData.beatmapObjects[num2].id;
                        }
                        else
                        {
                            parentID = null;
                        }
                    }
                }
                if (!dictionary.ContainsKey(obj.id))
                    dictionary.Add(obj.id, flag);
            }

            if (dictionary.ContainsKey(currentSelection.ID))
                dictionary[currentSelection.ID] = false;

            if (dictionary.ContainsKey(beatmapObjectToParentTo.id) && dictionary[beatmapObjectToParentTo.id])
            {
                currentSelection.GetData<BeatmapObject>().parent = beatmapObjectToParentTo.id;
                var bm = currentSelection.GetData<BeatmapObject>();
                Updater.UpdateProcessor(bm);
                ObjectEditor.inst.RenderParent(bm);
            }

            return dictionary.ContainsKey(beatmapObjectToParentTo.id) && dictionary[beatmapObjectToParentTo.id];
        }

        void OnMouseEnter()
        {
            hovered = true;
            onMouseEnter?.Invoke();

            if (TipEnabled && EditorManager.inst != null)
            {
                DataManager.Language enumTmp = DataManager.inst.GetCurrentLanguageEnum();
                int num = tooltipLanguages.FindIndex(x => x.language == enumTmp);
                if (num != -1)
                {
                    var tooltip = tooltipLanguages[num];
                    EditorManager.inst.SetTooltip(tooltip.keys, tooltip.desc, tooltip.hint);
                    return;
                }
                EditorManager.inst.SetTooltip(new List<string>(), "No tooltip added yet!", gameObject.name);
            }
        }

        void OnMouseExit()
        {
            hovered = false;
            onMouseExit?.Invoke();
            if (TipEnabled && EditorManager.inst != null)
            {
                EditorManager.inst.SetTooltipDisappear(0.5f);
            }
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

            onMouseDrag?.Invoke();

            dragTime = Time.time;
            if (EditorManager.inst && EditorManager.inst.isEditing && dragTime > startDragTime + 0.1f && CanDrag && Enabled && !EventSystem.current.IsPointerOverGameObject())
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
                Updater.UpdateProcessor(beatmapObject, "Keyframes");
        }

        float startDragTime;
        float dragTime;

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
                AudioManager.inst.CurrentAudioSource.time = selected.eventTime + beatmapObject.StartTime;
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
            if (!EditorManager.inst || !EditorManager.inst.isEditing)
            {
                hovered = false;
                return;
            }

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

            if (EventSystem.current.IsPointerOverGameObject() || beatmapObject == null)
                return;

            SetTooltip();

            if (beatmapObject.fromPrefab)
            {
                if (string.IsNullOrEmpty(beatmapObject.prefabInstanceID))
                    return;

                foreach (var bm in DataManager.inst.gameData.beatmapObjects.Where(x => x.fromPrefab && x.prefabInstanceID == beatmapObject.prefabInstanceID && x.objectType != DataManager.GameData.BeatmapObject.ObjectType.Empty))
                {
                    if (Updater.TryGetObject(bm, out Core.Optimization.Objects.LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.Renderer)
                    {
                        SetColor(levelObject.visualObject.Renderer);
                    }
                }
                return;
            }

            var m = 0f;

            if (beatmapObject != null && ShowObjectsOnlyOnLayer && beatmapObject.editorData.layer != EditorManager.inst.layer)
                m = -renderer.material.color.a + LayerOpacity;

            if (!hovered && renderer != null && renderer.material.HasProperty("_Color"))
                renderer.material.color += new Color(0f, 0f, 0f, m);

            if (HighlightObjects && hovered && renderer != null && renderer.material.HasProperty("_Color"))
            {
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
        }

        void FixedUpdate()
        {
            if (!dragging)
                return;

            if (!beatmapObject.fromPrefab)
                ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);
            else if (beatmapObject.fromPrefab && prefabObjectToDrag != null)
                PrefabEditorManager.inst.RenderPrefabObjectDialog(prefabObjectToDrag);
        }

        void SetColor(Renderer renderer)
        {
            var m = 0f;

            if (HighlightObjects && hovered && renderer != null && renderer.material.HasProperty("_Color"))
            {
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
        }

        void SetTooltip()
        {
            if (!EditorManager.inst.showHelp || beatmapObject == null)
                return;

            TipEnabled = true;

            if (tooltipLanguages.Count == 0)
            {
                tooltipLanguages.Add(TooltipHelper.NewTooltip(beatmapObject.name + " [ " + beatmapObject.StartTime + " ]", "", new List<string>()));
            }

            string parent = "";
            if (!string.IsNullOrEmpty(beatmapObject.parent))
            {
                parent = "<br>P: " + beatmapObject.parent + " (" + beatmapObject.GetParentType() + ")";
            }
            else
            {
                parent = "<br>P: No Parent" + " (" + beatmapObject.GetParentType() + ")";
            }

            string text = "";
            if (beatmapObject.shape != 4 || beatmapObject.shape != 6)
            {
                text = "<br>S: " + CoreHelper.GetShape(beatmapObject.shape, beatmapObject.shapeOption).Replace("eight_circle", "eighth_circle").Replace("eigth_circle_outline", "eighth_circle_outline");

                if (!string.IsNullOrEmpty(beatmapObject.text))
                {
                    text += "<br>T: " + beatmapObject.text;
                }
            }
            if (beatmapObject.shape == 4)
            {
                text = "<br>S: Text" +
                    "<br>T: " + beatmapObject.text;
            }
            if (beatmapObject.shape == 6)
            {
                text = "<br>S: Image" +
                    "<br>T: " + beatmapObject.text;
            }

            string ptr = "";
            if (beatmapObject.fromPrefab && !string.IsNullOrEmpty(beatmapObject.prefabID) && !string.IsNullOrEmpty(beatmapObject.prefabInstanceID))
            {
                ptr = "<br>PID: " + beatmapObject.prefabID + " | " + beatmapObject.prefabInstanceID;
            }
            else
            {
                ptr = "<br>Not from prefab";
            }

            Color col = LSColors.transparent;
            if (renderer.material.HasProperty("_Color"))
            {
                col = renderer.material.color;
            }

            if (tooltipLanguages[0].desc != "N/ST: " + beatmapObject.name + " [ " + beatmapObject.StartTime + " ]")
            {
                tooltipLanguages[0].desc = "N/ST: " + beatmapObject.name + " [ " + beatmapObject.StartTime + " ]";
            }

            if (tooltipLanguages[0].hint != "ID: {" + beatmapObject.id + "}" +
                parent +
                "<br>O: {X: " + beatmapObject.origin.x + ", Y: " + beatmapObject.origin.y + "}" +
                text +
                "<br>D: " + beatmapObject.Depth +
                "<br>ED: {L: " + beatmapObject.editorData.layer + ", B: " + beatmapObject.editorData.Bin + "}" +
                "<br>POS: {X: " + transform.position.x + ", Y: " + transform.position.y + ", Z: " + transform.position.z + "}" +
                "<br>SCA: {X: " + transform.localScale.x + ", Y: " + transform.localScale.y + "}" +
                "<br>ROT: " + transform.eulerAngles.z +
                "<br>COL: " + CoreHelper.ColorToHex(col) +
                ptr)
            {
                tooltipLanguages[0].hint = "ID: {" + beatmapObject.id + "}" +
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
            }
        }

        public List<HoverTooltip.Tooltip> tooltipLanguages = new List<HoverTooltip.Tooltip>();
    }
}
