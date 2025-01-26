using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Editor.Data.Dialogs
{
    /// <summary>
    /// Represents the base event editor dialog which contains all event keyframe editors.
    /// </summary>
    public class EventEditorDialog : EditorDialog
    {
        public EventEditorDialog() : base(EVENT_EDITOR) { }

        /// <summary>
        /// The currently open event keyframe editor.
        /// </summary>
        public KeyframeDialog CurrentKeyframeDialog { get; set; }

        /// <summary>
        /// A list containing all the event keyframe editors.
        /// </summary>
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

        /// <summary>
        /// Opens an event keyframe editor.
        /// </summary>
        /// <param name="type">The type of event keyframe.</param>
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

        /// <summary>
        /// Checks if <see cref="CurrentKeyframeDialog"/> is of a specific keyframe type.
        /// </summary>
        /// <param name="type">The type of event keyframe.</param>
        /// <returns>Returns true if the current keyframe dialog type matches the specific type, otherwise returns false.</returns>
        public bool IsCurrentKeyframeType(int type) => CurrentKeyframeDialog && CurrentKeyframeDialog.type == type;

        /// <summary>
        /// Closes the keyframe dialogs.
        /// </summary>
        public void CloseKeyframeDialogs()
        {
            for (int i = 0; i < keyframeDialogs.Count; i++)
                keyframeDialogs[i].SetActive(false);
            CurrentKeyframeDialog = null;
        }
    }
}
