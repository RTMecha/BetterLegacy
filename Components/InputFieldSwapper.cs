
using BetterLegacy.Core.Helpers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetterLegacy.Components
{
    /// <summary>
    /// Component used for swapping InputField values when scrollwheel is clicked on the InputField. Supports swapping left/right and positive/negative strings.
    /// </summary>
    public class InputFieldSwapper : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public InputField inputField;
        bool hovered;
        public Type type = Type.Num;
        public enum Type
        {
            Num,
            String
        }

        public void Init(InputField inputField, Type type)
        {
            this.inputField = inputField;
            this.type = type;
        }

        public void Init(InputField inputField)
        {
            this.inputField = inputField;
            type = Type.Num;
        }


        public void OnPointerEnter(PointerEventData pointerEventData) => hovered = true;

        public void OnPointerExit(PointerEventData pointerEventData) => hovered = false;

        void Update()
        {
            if (!hovered || !inputField)
                return;

            if (!Input.GetMouseButtonDown(2))
                return;

            if (type == Type.Num)
            {
                if (float.TryParse(inputField.text, out float num))
                {
                    num = -num;
                    inputField.text = num.ToString();
                }
                else if (CoreHelper.InEditor)
                    EditorManager.inst.DisplayNotification("Could not invert number!", 1f, EditorManager.NotificationType.Error);
            }

            if (type == Type.String)
                inputField.text = Flip(inputField.text);
        }

        string Flip(string str)
        {
            string s;
            s = str.Replace("Left", "LSLeft87344874").Replace("Right", "LSRight87344874").Replace("left", "LSleft87344874").Replace("right", "LSright87344874").Replace("LEFT", "LSLEFT87344874").Replace("RIGHT", "LSRIGHT87344874");

            return s.Replace("LSLeft87344874", "Right").Replace("LSRight87344874", "Left").Replace("LSleft87344874", "right").Replace("LSright87344874", "left").Replace("LSLEFT87344874", "RIGHT").Replace("LSRIGHT87344874", "LEFT");
        }
    }
}
