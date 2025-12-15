using BetterLegacy.Core;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class Vector2KeyframeDialog : KeyframeDialog
    {
        public Vector2KeyframeDialog(int type) : base(type) { }

        public Vector2KeyframeDialog(int type, LabelsElement labelsElement) : base(type) => this.labelsElement = labelsElement;

        public Vector2InputFieldStorage Vector2Field { get; set; }

        public LabelsElement labelsElement;

        public float increase = 0.1f;

        public float multiply = 10f;

        public float min = 0f;

        public float max = 0f;

        public bool allowNegative;

        public override void Init()
        {
            if (!GameObject)
                CreateNew(EventEditor.inst.dialogRight);

            base.Init();

            if (newDialog)
            {
                labelsElement?.Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
                Vector2Field = EditorPrefabHolder.Instance.Vector2InputFields.Duplicate(GameObject.transform, "position").GetOrAddComponent<Vector2InputFieldStorage>();
            }
            else
            {
                Vector2Field = GameObject.transform.GetChild(9).gameObject.GetOrAddComponent<Vector2InputFieldStorage>();
                Vector2Field.Assign();
            }
            EditorThemeManager.ApplyInputField(Vector2Field);
        }

        public override void Render()
        {
            base.Render();

            SetFloatInputField(Vector2Field.x, 0, increase, multiply, min, max, allowNegative);
            SetFloatInputField(Vector2Field.y, 1, increase, multiply, min, max, allowNegative);
        }
    }
}
