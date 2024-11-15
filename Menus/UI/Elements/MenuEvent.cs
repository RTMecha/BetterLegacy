using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Menus.UI.Elements
{
    /// <summary>
    /// Element only used for automatically triggering a function when spawned.
    /// </summary>
    public class MenuEvent : MenuImage
    {
        /// <summary>
        /// Automatically runs the <see cref="MenuImage.ParseFunction(SimpleJSON.JSONNode)"/> method.
        /// </summary>
        public void TriggerEvent()
        {
            func?.Invoke();
            if (funcJSON != null)
                ParseFunction(funcJSON);
            Spawn();
        }

        public static MenuEvent DeepCopy(MenuEvent orig, bool newID = true) => (MenuEvent)MenuImage.DeepCopy(orig, newID);
    }
}
