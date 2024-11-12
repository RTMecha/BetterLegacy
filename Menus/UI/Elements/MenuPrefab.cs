using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLegacy.Menus.UI.Interfaces;
using BetterLegacy.Menus.UI.Layouts;
using SimpleJSON;

namespace BetterLegacy.Menus.UI.Elements
{
    public class MenuPrefab
    {
        public string name;
        public string id;
        public Dictionary<string, MenuLayoutBase> layouts = new Dictionary<string, MenuLayoutBase>();
        public List<MenuImage> elements = new List<MenuImage>();

        public static MenuPrefab Parse(JSONNode jn)
        {
            var menuPrefab = new MenuPrefab();
            menuPrefab.id = jn["id"];
            menuPrefab.name = jn["name"];

            if (jn["layouts"] != null)
                CustomMenu.ParseLayouts(menuPrefab.layouts, jn["layouts"]);
            if (jn["elements"] != null)
                menuPrefab.elements.AddRange(CustomMenu.ParseElements(jn["elements"]));

            return menuPrefab;
        }
    }
}
