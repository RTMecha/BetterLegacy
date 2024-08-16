using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Menus.UI
{
    public class MenuEvent : MenuImage
    {
        public void TriggerEvent()
        {
            ParseFunction(funcJSON);
            Spawn();
        }
    }
}
