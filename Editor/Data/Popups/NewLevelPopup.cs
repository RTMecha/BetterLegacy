using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine.UI;

namespace BetterLegacy.Editor.Data.Popups
{
    public class NewLevelPopup : EditorPopup
    {
        public NewLevelPopup() : base(NEW_FILE_POPUP) { }

        public InputField SongPath { get; set; }
    }
}
