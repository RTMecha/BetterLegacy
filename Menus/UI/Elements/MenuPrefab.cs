using System.Collections.Generic;

using SimpleJSON;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Network;
using BetterLegacy.Menus.UI.Interfaces;
using BetterLegacy.Menus.UI.Layouts;

namespace BetterLegacy.Menus.UI.Elements
{
    public class MenuPrefab : Exists, IPacket
    {
        public string name;
        public string id;
        public Dictionary<string, MenuLayoutBase> layouts = new Dictionary<string, MenuLayoutBase>();
        public List<MenuImage> elements = new List<MenuImage>();

        public void ReadPacket(NetworkReader reader)
        {
            name = reader.ReadString();
            id = reader.ReadString();
            MenuLayoutBase.ReadPacketDictionary(layouts, reader);
            MenuImage.ReadPacketList(elements, reader);
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(name);
            writer.Write(id);
            MenuLayoutBase.WritePacketDictionary(layouts, writer);
            MenuImage.WritePacketList(elements, writer);
        }

        public static MenuPrefab Parse(JSONNode jn)
        {
            var menuPrefab = new MenuPrefab();
            menuPrefab.id = jn["id"];
            menuPrefab.name = jn["name"];

            if (jn["layouts"] != null)
                CustomInterface.ParseLayouts(menuPrefab.layouts, jn["layouts"]);
            if (jn["elements"] != null)
                menuPrefab.elements.AddRange(CustomInterface.ParseElements(jn["elements"]));

            return menuPrefab;
        }
    }
}
