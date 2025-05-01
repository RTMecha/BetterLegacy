using BetterLegacy.Core.Prefabs;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class Vector2KeyframeDialog : KeyframeDialog
    {
        public Vector2KeyframeDialog(int type) : base(type) { }

        public Vector2InputFieldStorage Vector2Field { get; set; }

        public override void Init()
        {
            base.Init();

            Vector2Field = GameObject.transform.GetChild(9).gameObject.AddComponent<Vector2InputFieldStorage>();
            Vector2Field.Assign();
        }
    }
}
