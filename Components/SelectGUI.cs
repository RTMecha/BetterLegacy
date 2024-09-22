using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BetterLegacy.Components
{
    /// <summary>
    /// Component for dragging UI elements around.
    /// </summary>
    public class SelectGUI : MonoBehaviour, IEventSystemHandler, IPointerDownHandler, IPointerUpHandler
    {
        public bool dragging;
        Vector3 startMousePos;
        Vector3 startPos;

        public Vector3 ogPos;
        public Transform target;
        public float scale = 1.03f;

        public static bool DragGUI { get; set; }
        public bool OverrideDrag { get; set; }
        public bool CanDrag => DragGUI || OverrideDrag;

        public Action<Vector2> draggingAction;

        void Start()
        {
            ogPos = transform.position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!CanDrag || eventData.button == PointerEventData.InputButton.Right)
                return;

            AudioManager.inst.PlaySound("blip");
            target.localScale = new Vector3(1f, 1f, 1f);
            dragging = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanDrag || eventData.button == PointerEventData.InputButton.Right)
                return;

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                AudioManager.inst.PlaySound("Click");
                target.localScale = new Vector3(scale, scale, 1f);
                dragging = true;
                startMousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
                startPos = target.position;
            }
            else
            {
                AudioManager.inst.PlaySound("Click");
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
