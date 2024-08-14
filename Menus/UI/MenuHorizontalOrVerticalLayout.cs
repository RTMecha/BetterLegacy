using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Menus.UI
{
    public abstract class MenuHorizontalOrVerticalLayout : MenuLayoutBase
    {
        public bool childControlHeight;
        public bool childControlWidth;
        public bool childForceExpandHeight;
        public bool childForceExpandWidth;
        public bool childScaleHeight;
        public bool childScaleWidth;

        public float spacing;
    }
}
