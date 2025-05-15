using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class PlayerEditorDialog : EditorDialog
    {
        public PlayerEditorDialog() : base(PLAYER_EDITOR) { }

        public override void Init()
        {
            if (init)
                return;

            base.Init();
        }
    }
}
