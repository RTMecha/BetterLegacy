using System;

using UnityEngine;
using UnityEngine.EventSystems;

using BetterLegacy.Configs;
using BetterLegacy.Core.Managers;

namespace BetterLegacy.Core.Components
{
    /// <summary>
    /// Component for dragging UI elements around.
    /// </summary>
    public class DraggableUI : MonoBehaviour, IEventSystemHandler, IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>
        /// If the UI is being dragged.
        /// </summary>
        public bool dragging;
        Vector3 startMousePos;
        Vector3 startPos;

        /// <summary>
        /// Original (gangster) position.
        /// </summary>
        public Vector3 ogPos;
        /// <summary>
        /// Target to drag.
        /// </summary>
        public Transform target;
        /// <summary>
        /// Value the scale of the UI is set to when dragging.
        /// </summary>
        public float scale = 1.03f;

        /// <summary>
        /// If the UI can be dragged.
        /// </summary>
        public bool CanDrag => mode switch
        {
            DragMode.NoDrag => false,
            DragMode.OptionalDrag => EditorConfig.Instance.DragUI.Value,
            DragMode.RequiredDrag => true,
            _ => false,
        };

        /// <summary>
        /// Function to run when dragging has begun.
        /// </summary>
        public Action<Vector2> onStartDrag;

        /// <summary>
        /// Function to run when the UI is being dragged.
        /// </summary>
        public Action<Vector2> draggingAction;

        /// <summary>
        /// Function to run when dragging has ended.
        /// </summary>
        public Action<Vector2> onEndDrag;

        /// <summary>
        /// Mode of UI dragging.
        /// </summary>
        public DragMode mode = DragMode.OptionalDrag;

        /// <summary>
        /// Dragging requirement.
        /// </summary>
        public enum DragMode
        {
            /// <summary>
            /// Does not drag.
            /// </summary>
            NoDrag,
            /// <summary>
            /// Drag is optional with the <see cref="EditorConfig.DragUI"/> setting.
            /// </summary>
            OptionalDrag,
            /// <summary>
            /// Dragging is on regardless.
            /// </summary>
            RequiredDrag,
        }

        void Start()
        {
            ogPos = transform.position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!CanDrag || eventData.button == PointerEventData.InputButton.Right)
                return;

            onEndDrag?.Invoke(target.position);
            SoundManager.inst.PlaySound(DefaultSounds.blip);
            target.localScale = new Vector3(1f, 1f, 1f);
            dragging = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanDrag || eventData.button == PointerEventData.InputButton.Right)
                return;

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                onStartDrag?.Invoke(target.position);
                SoundManager.inst.PlaySound(DefaultSounds.Click);
                target.localScale = new Vector3(scale, scale, 1f);
                dragging = true;
                startMousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
                startPos = target.position;
            }
            else
            {
                SoundManager.inst.PlaySound(DefaultSounds.Click);
                target.position = ogPos;
                startMousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
                startPos = target.position;
            }
        }

        void Update()
        {
            if (!dragging)
                return;

            var vector = new Vector3(Input.mousePosition.x, Input.mousePosition.y, target.localPosition.z);
            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (Mathf.Abs(startMousePos.x - vector.x) > Mathf.Abs(startMousePos.y - vector.y))
                    target.position = new Vector3(vector.x, target.position.y);
                if (Mathf.Abs(startMousePos.x - vector.x) < Mathf.Abs(startMousePos.y - vector.y))
                    target.position = new Vector3(target.position.x, vector.y);
            }
            else
            {
                float x = startMousePos.x - vector.x;
                float y = startMousePos.y - vector.y;
                target.position = new Vector3(startPos.x + -x, startPos.y + -y);
            }

            draggingAction?.Invoke(target.position);
        }
    }
}
