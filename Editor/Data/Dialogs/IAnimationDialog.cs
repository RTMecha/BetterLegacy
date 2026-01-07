using System.Collections.Generic;

using UnityEngine;

namespace BetterLegacy.Editor.Data.Dialogs
{
    /// <summary>
    /// Indicates a Dialog is able to edit animations.
    /// </summary>
    public interface IAnimationDialog
    {
        /// <summary>
        /// Game object of the keyframe editor.
        /// </summary>
        public GameObject GameObject { get; set; }

        /// <summary>
        /// If this dialog is the currently open dialog.
        /// </summary>
        public bool IsCurrent { get; }

        /// <summary>
        /// The currently open keyframe editor.
        /// </summary>
        public KeyframeDialog CurrentKeyframeDialog { get; set; }

        /// <summary>
        /// A list containing all the keyframe editors.
        /// </summary>
        public List<KeyframeDialog> KeyframeDialogs { get; set; }

        /// <summary>
        /// The keyframe timeline of the keyframe editor.
        /// </summary>
        public KeyframeTimeline Timeline { get; set; }
    }
}
