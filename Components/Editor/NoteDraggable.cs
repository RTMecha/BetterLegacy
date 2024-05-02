using BetterLegacy.Editor.Managers;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BetterLegacy.Components.Editor
{
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

        public DragPart part = DragPart.Base;

        public ProjectPlannerManager.NoteItem note;

        public Action onClick;

        bool dragging;
        Vector3 startMousePos;
        Vector3 startPos;
        Vector3 startSca;

        public float scale = 0.03f;

        public bool Free => ProjectPlannerManager.inst && (!ProjectPlannerManager.inst.PlannerActive || ProjectPlannerManager.inst.CurrentTab != 5);

        public void OnPointerUp(PointerEventData eventData)
        {
            if (Free)
            {
                AudioManager.inst.PlaySound("blip");
                if (part == DragPart.Base)
                {
                    transform.localScale = new Vector3(note.Scale.x, note.Scale.y, 1f);
                    note.Position = transform.localPosition;
                    note.Dragging = false;
                }
                ProjectPlannerManager.inst?.SaveNotes();
                dragging = false;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Free)
            {
                AudioManager.inst.PlaySound("Click");
                dragging = true;
                if (part == DragPart.Base)
                {
                    transform.localScale = new Vector3(note.Scale.x + scale, note.Scale.y + scale, 1f);
                    note.Dragging = true;
                }
                startMousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);

                if (part == DragPart.Base)
                {
                    startPos = transform.position;
                }
                else
                {
                    startPos = note.Size;
                    startSca = note.Scale;
                }
            }

            if (!Free && part == DragPart.Base)
            {
                onClick?.Invoke();
            }
        }

        void Update()
        {
            if (dragging)
            {
                var vector = new Vector3(Input.mousePosition.x, Input.mousePosition.y, transform.localPosition.z);

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
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Free && (part == DragPart.Left || part == DragPart.Right))
            {
                Cursor.SetCursor(ProjectPlannerManager.inst.horizontalDrag, CursorMode.Auto);
            }

            if (Free && (part == DragPart.Up || part == DragPart.Down))
            {
                Cursor.SetCursor(ProjectPlannerManager.inst.verticalDrag, CursorMode.Auto);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Free && part != DragPart.Base)
                Cursor.SetCursor(null, CursorMode.Auto);
        }
    }
}
