using System;

using UnityEngine;

using BetterLegacy.Core;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class FloatKeyframeDialog : KeyframeDialog
    {
        public FloatKeyframeDialog(int type, float increase = 0.1f, float multiply = 10f, float min = 0f, float max = 0f, bool allowNegative = true, Action<float> onValueChanged = null, bool isInteger = false) : base(type)
        {
            this.increase = increase;
            this.multiply = multiply;
            this.min = min;
            this.max = max;
            this.allowNegative = allowNegative;
            this.onValueChanged = onValueChanged;
            this.isInteger = isInteger;
        }
        
        public FloatKeyframeDialog(int type, string valueLabel, float increase = 0.1f, float multiply = 10f, float min = 0f, float max = 0f, bool allowNegative = true, Action<float> onValueChanged = null, bool isInteger = false) : base(type)
        {
            this.valueLabel = valueLabel;
            this.increase = increase;
            this.multiply = multiply;
            this.min = min;
            this.max = max;
            this.allowNegative = allowNegative;
            this.onValueChanged = onValueChanged;
            this.isInteger = isInteger;
        }

        public string valueLabel;

        public InputFieldStorage Field { get; set; }

        public float increase = 0.1f;

        public float multiply = 10f;

        public float min = 0f;

        public float max = 0f;

        public bool allowNegative;

        public Action<float> onValueChanged;

        public bool isInteger;

        public override void Init()
        {
            if (!GameObject)
                CreateNew(EventEditor.inst.dialogRight);

            base.Init();

            if (newDialog)
            {
                new LabelsElement(valueLabel).Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
                Field = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "amount").transform.GetChild(0).GetComponent<InputFieldStorage>();
                Field.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);
            }
            else
            {
                Field = GameObject.transform.GetChild(9).GetChild(0).gameObject.GetOrAddComponent<InputFieldStorage>();
                Field.Assign();
            }

            EditorThemeManager.ApplyInputField(Field);
        }

        public override void Render()
        {
            base.Render();
            SetFloatInputField(Field, 0, increase, multiply, min, max, allowNegative, onValueChanged);
        }
    }
}
