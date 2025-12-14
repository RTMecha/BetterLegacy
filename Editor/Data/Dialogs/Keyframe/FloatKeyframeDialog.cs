using BetterLegacy.Core;
using BetterLegacy.Core.Prefabs;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class FloatKeyframeDialog : KeyframeDialog
    {
        public FloatKeyframeDialog(int type) : base(type) { }

        public InputFieldStorage Field { get; set; }

        public override void Init()
        {
            base.Init();

            Field = GameObject.transform.GetChild(9).GetChild(0).gameObject.GetOrAddComponent<InputFieldStorage>();
            Field.Assign();
        }
    }
}
