using UnityEngine.UI;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class FloatKeyframeModeDialog : FloatKeyframeDialog
    {
        public FloatKeyframeModeDialog(int type) : base(type) { }

        public Dropdown Mode { get; set; }

        public override void Init()
        {
            base.Init();

            Mode = GameObject.transform.GetChild(11).GetComponent<Dropdown>();
        }
    }
}
