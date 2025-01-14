using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class EventEditorDialog : EditorDialog
    {
        public EventEditorDialog() : base(EVENT_EDITOR) { }

        public KeyframeDialog CurrentKeyframeDialog { get; set; }

        public List<KeyframeDialog> keyframeDialogs = new List<KeyframeDialog>();

        public override void Init()
        {
            if (init)
                return;

            base.Init();

            for (int i = 0; i < EventEditor.inst.dialogRight.childCount; i++)
            {
                var dialog = EventEditor.inst.dialogRight.GetChild(i);
                var keyframeDialog = new KeyframeDialog(i);
                keyframeDialog.GameObject = dialog.gameObject;
                keyframeDialog.Init();
                keyframeDialogs.Add(keyframeDialog);
            }
        }

        public void OpenKeyframeDialog(int type)
        {
            for (int i = 0; i < keyframeDialogs.Count; i++)
            {
                var active = i == type;
                keyframeDialogs[i].SetActive(active);
                if (active)
                    CurrentKeyframeDialog = keyframeDialogs[i];
            }
        }

        public void CloseKeyframeDialogs()
        {
            for (int i = 0; i < keyframeDialogs.Count; i++)
                keyframeDialogs[i].SetActive(false);
            CurrentKeyframeDialog = null;
        }
    }
}
