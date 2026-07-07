using System.Collections.Generic;

using SimpleJSON;

using BetterLegacy.Core.Data.Network;

namespace BetterLegacy.Menus.UI.Elements
{
    public class MenuPrefabObject : MenuImage
    {
        public string prefabID;
        public MenuPrefab prefab;
        public Dictionary<string, JSONNode> elementSettings = new Dictionary<string, JSONNode>();

        public override void ReadPacket(NetworkReader reader)
        {
            prefabID = reader.ReadString();
            elementSettings = reader.ReadDictionary(reader.ReadString, reader.ReadJSON);
        }

        public override void WritePacket(NetworkWriter writer)
        {
            writer.Write(prefabID);
            writer.Write(elementSettings, writer.Write, writer.Write);
        }

        public static MenuPrefabObject DeepCopy(MenuPrefabObject orig, bool newID = true) => new MenuPrefabObject();

        public void ApplyElementSetting(MenuImage menuImage)
        {
            if (elementSettings.TryGetValue(menuImage.id, out JSONNode jn))
                menuImage.Read(jn, 0, 1, null);
        }
    }
}
