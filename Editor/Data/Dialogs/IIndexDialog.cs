using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core.Prefabs;

namespace BetterLegacy.Editor.Data.Dialogs
{
    /// <summary>
    /// Indicates a dialog has an index selector / editor.
    /// </summary>
    public interface IIndexDialog
    {
        public Transform Edit { get; set; }
        public Button JumpToStartButton { get; set; }
        public Button JumpToPrevButton { get; set; }
        public Text KeyframeIndexer { get; set; }
        public Button JumpToNextButton { get; set; }
        public Button JumpToLastButton { get; set; }
        public FunctionButtonStorage CopyButton { get; set; }
        public FunctionButtonStorage PasteButton { get; set; }
        public DeleteButtonStorage DeleteButton { get; set; }
    }
}
