using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Menus.UI.Elements
{
    public class MenuPrefabObject : MenuImage
    {
        public string prefabID;
        public MenuPrefab prefab;

        public static MenuPrefabObject DeepCopy(MenuPrefabObject orig, bool newID = true) => (MenuPrefabObject)MenuImage.DeepCopy(orig, newID);
    }
}
