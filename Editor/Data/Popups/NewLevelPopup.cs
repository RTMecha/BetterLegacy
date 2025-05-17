using UnityEngine.UI;

using BetterLegacy.Companion.Data;
using BetterLegacy.Companion.Entity;

namespace BetterLegacy.Editor.Data.Popups
{
    public class NewLevelPopup : EditorPopup
    {
        public NewLevelPopup() : base(NEW_FILE_POPUP) { }

        public InputField SongPath { get; set; }

        public override void Open()
        {
            base.Open();

            // progresses the Create New Level tutorial if it's at the start.
            Example.Current?.tutorials?.AdvanceTutorial(ExampleTutorial.CREATE_LEVEL, 0);
        }
    }
}
