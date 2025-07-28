using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Represents a checkpoint in the editor.
    /// </summary>
    public class TimelineCheckpoint : Exists
    {
        public TimelineCheckpoint() { }

        public TimelineCheckpoint(Checkpoint checkpoint)
        {
            Checkpoint = checkpoint;
            Checkpoint.timelineCheckpoint = this;
        }

        #region Properties

        #region UI

        /// <summary>
        /// The <see cref="GameObject"/> of the timeline checkpoint.
        /// </summary>
        public GameObject GameObject { get; set; }

        /// <summary>
        /// The <see cref="RectTransform"/> of the timeline checkpoint.
        /// </summary>
        public RectTransform RectTransform { get; set; }

        /// <summary>
        /// The <see cref="Image"/> of the timeline checkpoint.
        /// </summary>
        public Image Image { get; set; }

        #endregion

        #region Data

        /// <summary>
        /// Index of the checkpoint.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// The checkpoint data.
        /// </summary>
        public Checkpoint Checkpoint { get; set; }

        /// <summary>
        /// If the timeline checkpoint is selected.
        /// </summary>
        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                RenderSelected();
            }
        }

        #endregion

        #endregion

        #region Fields

        bool selected;

        /// <summary>
        /// If the timeline checkpoint is being dragged.
        /// </summary>
        public bool dragging;

        /// <summary>
        /// If the timeline checkpoint is rendered on the object layer.
        /// </summary>
        public readonly bool ghost;

        /// <summary>
        /// Drag time offset.
        /// </summary>
        public float timeOffset;

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the timeline checkpoint.
        /// </summary>
        /// <param name="index">Index of the checkpoint.</param>
        public void Init(int index)
        {
            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Delete(gameObject);

            var checkpoint = Checkpoint;

            gameObject = CheckpointEditor.inst.checkpointPrefab.Duplicate(RTCheckpointEditor.inst.parent, "Checkpoint " + index);
            gameObject.transform.localScale = Vector3.one;

            gameObject.transform.AsRT().sizeDelta = new Vector2(8f, 20f);

            TriggerHelper.AddEventTriggers(gameObject,
                TriggerHelper.CreateEntry(EventTriggerType.PointerClick, eventData =>
                {
                    RTCheckpointEditor.inst.SetCurrentCheckpoint(index);
                }),
                TriggerHelper.CreateEntry(EventTriggerType.BeginDrag, eventData =>
                {
                    if (Index == 0)
                    {
                        EditorManager.inst.DisplayNotification("Can't change time of first Checkpoint", 2f, EditorManager.NotificationType.Warning, false);
                        return;
                    }
                    timeOffset = Checkpoint.time;
                    dragging = true;
                    RTCheckpointEditor.inst.dragging = true;
                    RTCheckpointEditor.inst.RenderDialog(Checkpoint);
                }),
                TriggerHelper.CreateEntry(EventTriggerType.EndDrag, eventData =>
                {
                    dragging = false;
                    RTCheckpointEditor.inst.RenderDialog(Checkpoint);
                }));

            Index = index;
            GameObject = gameObject;
            RectTransform = gameObject.transform.AsRT();
            Image = gameObject.GetComponent<Image>();
        }

        /// <summary>
        /// Initializes the timeline checkpoint as a ghost checkpoint.
        /// </summary>
        /// <param name="index">Index of the checkpoint.</param>
        public void InitGhost(int index)
        {
            var gameObject = CheckpointEditor.inst.ghostCheckpointPrefab.Duplicate(EditorManager.inst.timeline.transform, "Checkpoint " + index);
            gameObject.transform.localScale = Vector3.one;

            gameObject.transform.AsRT().sizeDelta = new Vector2(8f, 300f);

            Index = index;
            GameObject = gameObject;
            RectTransform = gameObject.transform.AsRT();
            Image = gameObject.GetComponent<Image>();
        }

        /// <summary>
        /// Renders the whole timeline checkpoint.
        /// </summary>
        public void Render()
        {
            GameObject.SetActive(true);
            RenderPosition();
            RenderSelected();
        }

        /// <summary>
        /// Renders the timeline checkpoint position.
        /// </summary>
        public void RenderPosition() => RenderPosition(Checkpoint.time, EditorManager.inst.Zoom);

        /// <summary>
        /// Renders the timeline checkpoint position.
        /// </summary>
        /// <param name="time">Time position.</param>
        /// <param name="zoom">Timeline zoom.</param>
        /// <param name="offset">Position offset.</param>
        public void RenderPosition(float time, float zoom, float offset = 6f)
        {
            if (RectTransform)
                RectTransform.anchoredPosition = new Vector2(time * zoom - offset, 0f);
        }

        /// <summary>
        /// Renders the selected state of the timeline checkpoint.
        /// </summary>
        public void RenderSelected() => RenderSelected(selected && EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events);

        /// <summary>
        /// Renders the selected state of the timeline checkpoint.
        /// </summary>
        /// <param name="selected">Selected state.</param>
        public void RenderSelected(bool selected)
        {
            if (!ghost && Image)
                Image.color = selected ? CheckpointEditor.inst.selectedColor : CheckpointEditor.inst.deselectedColor;
        }

        #endregion
    }
}
