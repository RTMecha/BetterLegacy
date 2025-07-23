using UnityEngine;

namespace BetterLegacy.Editor.Data.Dialogs
{
    /// <summary>
    /// Indicates a Dialog contains a tag list.
    /// </summary>
    public interface ITagDialog
    {
        public RectTransform TagsScrollView { get; set; }

        public RectTransform TagsContent { get; set; }
    }
}
