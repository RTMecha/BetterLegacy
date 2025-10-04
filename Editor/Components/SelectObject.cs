using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;

using LSFunctions;

using BetterLegacy.Arcade.Managers;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Components
{
    /// <summary>
    /// Component for selecting and dragging objects.
    /// </summary>
    public class SelectObject : MonoBehaviour
    {
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

        void OnDisable()
        {
            hovered = false; // set hovered off so when the object re-enables it won't think it's still hovered.
        }

        /// <summary>
        /// Assigns a BeatmapObjects' RTObject references.
        /// </summary>
        /// <param name="beatmapObject"></param>
        public void SetObject(BeatmapObject beatmapObject)
        {
            beatmapObject.selector = this;
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

            // don't drag object if Example is being dragged.
            if (Companion.Entity.Example.Current && Companion.Entity.Example.Current.Dragging)
                return;

            startDragTime = Time.time;

            EditorTimeline.inst.SelectObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
        }

        void OnMouseEnter()
        {
            hovered = true;

            if (!CoreHelper.IsEditing)
                return;

            SetTooltip();

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
            dragTime = Time.time;
            if (!CoreHelper.IsEditing || dragTime <= startDragTime + 0.15f || EditorTimeline.inst.SelectedObjectCount >= 2 || EventSystem.current.IsPointerOverGameObject())
                return;

            var vector = new Vector3(Input.mousePosition.x, Input.mousePosition.y, transform.localPosition.z);
            var vector2 = Camera.main.ScreenToWorldPoint(vector);
            var vector3 = new Vector3((float)((int)vector2.x), (float)((int)vector2.y), transform.localPosition.z);

            if (beatmapObject.fromPrefab)
            {
                if (!EditorConfig.Instance.PrefabObjectDraggerEnabled.Value)
                    return;

                var currentSelection = EditorTimeline.inst.CurrentSelection;

                if (!currentSelection.isPrefabObject || currentSelection.ID != beatmapObject.prefabInstanceID)
                    return;

                prefabObjectToDrag = currentSelection.GetData<PrefabObject>();

                selectedKeyframe = prefabObjectToDrag.events[0];

                dragging = true;

                Drag(vector2, vector3);

                return;
            }

            if (!EditorConfig.Instance.ObjectDraggerEnabled.Value)
                return;

            if (!dragging && selectedKeyframe == null)
            {
                dragging = true;
                selectedKeyframe = beatmapObject.GetOrCreateKeyframe(0, CreateKeyframe);
            }

            Drag(vector2, vector3);
        }

        void Drag(Vector3 vector2, Vector3 vector3)
        {
            if (selectedKeyframe == null)
                return;

            if (!setKeyframeValues)
            {
                setKeyframeValues = true;
                dragKeyframeValues = new Vector2(selectedKeyframe.values[0], selectedKeyframe.values[1]);
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
                selectedKeyframe.values[0] = dragKeyframeValues.x - dragOffset.x + (Input.GetKey(KeyCode.LeftShift) ? vector3.x : vector2.x);
            if (firstDirection == Axis.Static || firstDirection == Axis.PosY || firstDirection == Axis.NegY)
                selectedKeyframe.values[1] = dragKeyframeValues.y - dragOffset.y + (Input.GetKey(KeyCode.LeftShift) ? vector3.y : vector2.y);

            if (prefabObjectToDrag)
                RTLevel.Current?.UpdatePrefab(prefabObjectToDrag, PrefabObjectContext.TRANSFORM_OFFSET);
            else
                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
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
                return prefabObject.events[type];

            return beatmapObject.GetOrCreateKeyframe(type, CreateKeyframe);
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

            if (Enabled)
            {
                var currentSelection = EditorTimeline.inst.CurrentSelection;

                if (!beatmapObject.fromPrefab && currentSelection.ID == beatmapObject.id)
                {
                    RTGameManager.inst.objectDragger.position = new Vector3(transform.parent.position.x, transform.parent.position.y, transform.parent.position.z - 10f);
                    RTGameManager.inst.objectDragger.rotation = transform.parent.rotation;
                }

                if (beatmapObject.fromPrefab && currentSelection.ID == beatmapObject.prefabInstanceID)
                {
                    var prefabObject = currentSelection.GetData<PrefabObject>();
                    RTGameManager.inst.objectDragger.position = new Vector3(prefabObject.events[0].values[0], prefabObject.events[0].values[1], -90f);
                    RTGameManager.inst.objectDragger.rotation = Quaternion.Euler(0f, 0f, prefabObject.events[2].values[0]);
                }
            }

            if (!EventSystem.current.IsPointerOverGameObject())
                Highlight(HighlightObjects && hovered);

            if (!EditorConfig.Instance.OutlineSelected.Value || !beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not SolidObject solidObject)
                return;

            var selected = beatmapObject.timelineObject && beatmapObject.timelineObject.Selected;
            var color = selected ? EditorConfig.Instance.OutlineColor.Value : LSColors.transparent;
            if (this.selected == selected && solidObject.editorOutlineData.color == color && solidObject.editorOutlineData.width == EditorConfig.Instance.OutlineWidth.Value)
                return;

            this.selected = selected;

            solidObject.AddEditorOutline();
            solidObject.SetEditorOutline(color, EditorConfig.Instance.OutlineWidth.Value);
        }

        bool selected;

        void FixedUpdate()
        {
            if (!dragging)
                return;

            if (!beatmapObject.fromPrefab)
                ObjectEditor.inst.Dialog.Timeline.RenderDialog(beatmapObject);
            else if (beatmapObject.fromPrefab && prefabObjectToDrag)
                RTPrefabEditor.inst.RenderPrefabObjectTransforms(prefabObjectToDrag);
        }

        public void Highlight(bool highlight)
        {
            if (beatmapObject.fromPrefab)
            {
                if (string.IsNullOrEmpty(beatmapObject.prefabInstanceID))
                    return;

                var prefabObject = beatmapObject.GetPrefabObject();
                if (!prefabObject || prefabObject.fromModifier || !(highlight || ShowObjectsOnlyOnLayer && prefabObject.editorData.Layer != EditorTimeline.inst.Layer))
                    return;

                var beatmapObjects = prefabObject.ExpandedObjects.FindAll(x => x.objectType != BeatmapObject.ObjectType.Empty);
                for (int i = 0; i < beatmapObjects.Count; i++)
                    SetColor(beatmapObjects[i].runtimeObject, prefabObject.editorData.Layer, true);

                return;
            }

            if (!beatmapObject.editorData || !beatmapObject.runtimeObject)
                return;

            SetColor(beatmapObject.runtimeObject, beatmapObject.editorData.Layer, highlight);
        }

        void SetColor(RTBeatmapObject levelObject, int layer, bool highlight)
        {
            if (!levelObject)
                return;

            SetHoverColor(levelObject, highlight);
            SetLayerColor(levelObject, layer);
        }

        void SetHoverColor(RTBeatmapObject runtimeObject, bool highlight)
        {
            if (!highlight || !runtimeObject || !runtimeObject.visualObject)
                return;

            var currentColor = runtimeObject.visualObject.GetPrimaryColor();
            currentColor += Highlight(currentColor);
            runtimeObject.visualObject.SetPrimaryColor(RTColors.FadeColor(currentColor, 1f));

            if (runtimeObject.visualObject.isGradient)
            {
                var secondaryColor = runtimeObject.visualObject.GetSecondaryColor();
                secondaryColor += Highlight(secondaryColor);
                runtimeObject.visualObject.SetSecondaryColor(RTColors.FadeColor(secondaryColor, 1f));
            }
        }

        Color Highlight(Color currentColor) => Input.GetKey(KeyCode.LeftShift) ? new Color(
                currentColor.r > 0.9f ? -HighlightDoubleColor.r : HighlightDoubleColor.r,
                currentColor.g > 0.9f ? -HighlightDoubleColor.g : HighlightDoubleColor.g,
                currentColor.b > 0.9f ? -HighlightDoubleColor.b : HighlightDoubleColor.b,
                0f) : new Color(
                currentColor.r > 0.9f ? -HighlightColor.r : HighlightColor.r,
                currentColor.g > 0.9f ? -HighlightColor.g : HighlightColor.g,
                currentColor.b > 0.9f ? -HighlightColor.b : HighlightColor.b,
                0f);

        void SetLayerColor(RTBeatmapObject runtimeObject, int layer)
        {
            if (!ShowObjectsOnlyOnLayer || layer == EditorManager.inst.layer || !runtimeObject || !runtimeObject.visualObject)
                return;

            var primaryColor = runtimeObject.visualObject.GetPrimaryColor();
            runtimeObject.visualObject.SetPrimaryColor(RTColors.FadeColor(primaryColor, primaryColor.a * LayerOpacity));
            if (runtimeObject && runtimeObject.visualObject.isGradient)
            {
                var secondaryColor = runtimeObject.visualObject.GetSecondaryColor();
                runtimeObject.visualObject.SetSecondaryColor(RTColors.FadeColor(secondaryColor, secondaryColor.a * LayerOpacity));
            }
        }

        void SetTooltip()
        {
            if (!beatmapObject || EventSystem.current.IsPointerOverGameObject())
                return;

            if (tooltipLanguages.Count == 0)
                tooltipLanguages.Add(TooltipHelper.NewTooltip(beatmapObject.name + " [ " + beatmapObject.StartTime + " ]", "", new List<string>()));

            string parent = !string.IsNullOrEmpty(beatmapObject.Parent) ?
                 "<br>P: " + beatmapObject.Parent + " (" + beatmapObject.parentType + ")" :
                 "<br>P: No Parent" + " (" + beatmapObject.parentType + ")";

            string text = beatmapObject.Shape switch
            {
                4 => "<br>S: Text",
                6 => "<br>S: Image",
                _ => "<br>S: " + CoreHelper.GetShape(beatmapObject.Shape, beatmapObject.shapeOption).Replace("eight_circle", "eighth_circle").Replace("eigth_circle_outline", "eighth_circle_outline"),
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
                "<br>ED: {L: " + beatmapObject.editorData.Layer + ", B: " + beatmapObject.editorData.Bin + "}" +
                "<br>POS: {X: " + transform.position.x + ", Y: " + transform.position.y + ", Z: " + transform.position.z + "}" +
                "<br>SCA: {X: " + transform.localScale.x + ", Y: " + transform.localScale.y + "}" +
                "<br>ROT: " + transform.eulerAngles.z +
                "<br>COL: " + RTColors.ColorToHex(col) +
                ptr;
            if (tooltipLanguages[0].hint != result)
                tooltipLanguages[0].hint = result;
        }

        public List<HoverTooltip.Tooltip> tooltipLanguages = new List<HoverTooltip.Tooltip>();
    }
}
