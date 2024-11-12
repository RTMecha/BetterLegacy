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
        public Dictionary<string, SimpleJSON.JSONNode> elementSettings = new Dictionary<string, SimpleJSON.JSONNode>();

        public static MenuPrefabObject DeepCopy(MenuPrefabObject orig, bool newID = true) => (MenuPrefabObject)MenuImage.DeepCopy(orig, newID);

        public void ApplyElementSetting(MenuImage menuImage)
        {
            if (elementSettings.TryGetValue(menuImage.id, out SimpleJSON.JSONNode jn))
                menuImage.Read(jn, 0, 1, null);
        }
    }
}
