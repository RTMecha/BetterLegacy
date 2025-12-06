using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Planners;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Components
{
    /// <summary>
    /// Allows a note to be draggable in different ways, including position, size and scale.
    /// </summary>
    class NoteDraggable : MonoBehaviour, IEventSystemHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public enum DragPart
        {
            Base,
            Left,
            Right,
            Up,
            Down
        }

        /// <summary>
        /// Which part of the note is being dragged.
        /// </summary>
        public DragPart part = DragPart.Base;

        /// <summary>
        /// The note reference.
        /// </summary>
        public NotePlanner note;

        bool dragging;
        Vector3 startMousePos;
        Vector3 startPos;
        Vector3 startSca;

        /// <summary>
        /// Gets a true value if the note is in floating mode, otherwise gets a false value if it's in the Project Planner layout.
        /// </summary>
        public static bool Free => ProjectPlanner.inst && (!ProjectPlanner.inst.PlannerActive || ProjectPlanner.inst.CurrentTab != PlannerBase.Type.Note);

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!Free)
                return;

            SoundManager.inst.PlaySound(DefaultSounds.blip);
            if (part == DragPart.Base)
            {
                transform.localScale = new Vector3(note.Scale.x, note.Scale.y, 1f);
                note.Position = transform.localPosition;
                note.Dragging = false;
            }

            ProjectPlanner.inst?.SaveNotes();
            dragging = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (note && note.Hyperlinks && note.Hyperlinks.IsLinkHighlighted)
            {
                note.Hyperlinks.OnPointerClick(eventData);
                return;
            }

            if (Free)
            {
                SoundManager.inst.PlaySound(DefaultSounds.Click);
                dragging = true;

                if (part == DragPart.Base)
                {
                    transform.localScale = new Vector3(note.Scale.x + 0.03f, note.Scale.y + 0.03f, 1f);
                    note.Dragging = true;
                    startMousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
                    startPos = transform.position;
                }
                else
                {
                    startMousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f) * CoreHelper.ScreenScaleInverse;
                    startPos = note.Size;
                    startSca = note.Scale;
                }
            }

            if (!Free && part == DragPart.Base)
            {
                if (eventData.button == PointerEventData.InputButton.Right)
                {
                    var buttonFunctions = new List<ButtonFunction>
                    {
                        new ButtonFunction("Edit", () => ProjectPlanner.inst.OpenNoteEditor(note)),
                        new ButtonFunction("Delete", () =>
                        {
                            ProjectPlanner.inst.notes.RemoveAll(x => x is NotePlanner && x.ID == note.ID);
                            ProjectPlanner.inst.SaveNotes();
                            CoreHelper.Destroy(note.GameObject);
                        }),
                        new ButtonFunction(true),
                        new ButtonFunction("Copy", () =>
                        {
                            ProjectPlanner.inst.copiedPlanners.Clear();
                            ProjectPlanner.inst.copiedPlanners.Add(note);
                            EditorManager.inst.DisplayNotification("Copied note!", 2f, EditorManager.NotificationType.Success);
                        }),
                        new ButtonFunction("Paste", ProjectPlanner.inst.PastePlanners),
                        new ButtonFunction(true),
                    };

                    buttonFunctions.AddRange(EditorContextMenu.GetMoveIndexFunctions(ProjectPlanner.inst.notes, () => ProjectPlanner.inst.notes.IndexOf(note), () =>
                    {
                        for (int i = 0; i < ProjectPlanner.inst.notes.Count; i++)
                            ProjectPlanner.inst.notes[i].Init();
                    }));

                    EditorContextMenu.inst.ShowContextMenu(buttonFunctions);
                    return;
                }

                ProjectPlanner.inst?.OpenNoteEditor(note);
            }
        }

        void Update()
        {
            if (!dragging)
                return;

            var vector = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f) * (part == DragPart.Base ? 1f : CoreHelper.ScreenScaleInverse);

            switch (part)
            {
                case DragPart.Base:
                    {
                        if (Input.GetKey(KeyCode.LeftShift))
                        {
                            transform.position = new Vector3((int)vector.x, (int)vector.y);
                        }
                        else
                        {
                            float x = startMousePos.x - vector.x;
                            float y = startMousePos.y - vector.y;
                            transform.position = new Vector3(startPos.x + -x, startPos.y + -y);
                        }
                        break;
                    }
                case DragPart.Left:
                    {
                        if (Input.GetKey(KeyCode.LeftControl))
                        {
                            var sca = note.Scale;
                            sca.x = Mathf.Clamp(startSca.x + -(startMousePos.x - vector.x) * 0.005f, 0.2f, 2f);
                            note.Scale = sca;
                        }
                        else
                        {
                            var size = note.Size;
                            size.x = Mathf.Clamp(startPos.x + -(startMousePos.x - vector.x) * 2f, 132f, 1700f);
                            note.Size = size;
                        }

                        break;
                    }
                case DragPart.Right:
                    {
                        if (Input.GetKey(KeyCode.LeftControl))
                        {
                            var sca = note.Scale;
                            sca.x = Mathf.Clamp(startSca.x + (startMousePos.x - vector.x) * 0.005f, 0.2f, 2f);
                            note.Scale = sca;
                        }
                        else
                        {
                            var size = note.Size;
                            size.x = Mathf.Clamp(startPos.x + (startMousePos.x - vector.x) * 2f, 132f, 1700f);
                            note.Size = size;
                        }

                        break;
                    }
                case DragPart.Up:
                    {
                        if (Input.GetKey(KeyCode.LeftControl))
                        {
                            var sca = note.Scale;
                            sca.y = Mathf.Clamp(startSca.y + -(startMousePos.y - vector.y) * 0.005f, 0.2f, 2f);
                            note.Scale = sca;
                        }
                        else
                        {
                            var size = note.Size;
                            size.y = Mathf.Clamp(startPos.y + -(startMousePos.y - vector.y) * 2f, 32f, 1000f);
                            note.Size = size;
                        }

                        break;
                    }
                case DragPart.Down:
                    {
                        if (Input.GetKey(KeyCode.LeftControl))
                        {
                            var sca = note.Scale;
                            sca.y = Mathf.Clamp(startSca.y + (startMousePos.y - vector.y) * 0.005f, 0.2f, 2f);
                            note.Scale = sca;
                        }
                        else
                        {
                            var size = note.Size;
                            size.y = Mathf.Clamp(startPos.y + (startMousePos.y - vector.y) * 2f, 32f, 1000f);
                            note.Size = size;
                        }

                        break;
                    }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Free && (part == DragPart.Left || part == DragPart.Right))
                Cursor.SetCursor(ProjectPlanner.inst.horizontalDrag, CursorMode.Auto);

            if (Free && (part == DragPart.Up || part == DragPart.Down))
                Cursor.SetCursor(ProjectPlanner.inst.verticalDrag, CursorMode.Auto);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Free && part != DragPart.Base)
                Cursor.SetCursor(null, CursorMode.Auto);
        }
    }
}
