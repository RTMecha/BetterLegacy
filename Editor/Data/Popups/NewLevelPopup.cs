using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine.UI;

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
            Example.Current?.tutorials?.AdvanceTutorial(ExampleTutorials.Tutorials.CREATE_LEVEL, 0);
        }
    }
}
