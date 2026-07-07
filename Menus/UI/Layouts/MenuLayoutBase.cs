using System;
using System.Collections.Generic;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Network;

namespace BetterLegacy.Menus.UI.Layouts
{
    public abstract class MenuLayoutBase : IPacket
    {
        public GameObject gameObject;

        public RectTransform content;

        public string name;

        public TextAnchor childAlignment;

        public RectValues rect = RectValues.Default;

        public bool regenerate = true;

        public bool mask;

        public bool scrollable;

        public RectValues contentRect = RectValues.FullAnchored;

        public JSONNode onScrollUpFuncJSON;
        public JSONNode onScrollDownFuncJSON;

        public Action onScrollUpFunc;
        public Action onScrollDownFunc;

        public static void ReadPacketDictionary(Dictionary<string, MenuLayoutBase> layouts, NetworkReader reader)
        {
            var layoutCount = reader.ReadInt32();
            for (int i = 0; i < layoutCount; i++)
            {
                var key = reader.ReadString();
                var type = reader.ReadString();
                switch (type)
                {
                    case "horizontal": {
                            layouts[key] = Packet.CreateFromPacket<MenuHorizontalLayout>(reader);
                            break;
                        }
                    case "vertical": {
                            layouts[key] = Packet.CreateFromPacket<MenuVerticalLayout>(reader);
                            break;
                        }
                    case "grid": {
                            layouts[key] = Packet.CreateFromPacket<MenuGridLayout>(reader);
                            break;
                        }
                }
            }
        }

        public static void WritePacketDictionary(Dictionary<string, MenuLayoutBase> layouts, NetworkWriter writer)
        {
            writer.Write(layouts.Count);
            foreach (var keyValuePair in layouts)
            {
                writer.Write(keyValuePair.Key);
                var layout = keyValuePair.Value;
                if (layout is MenuHorizontalLayout menuHorizontalLayout)
                {
                    writer.Write("horizontal");
                    menuHorizontalLayout.WritePacket(writer);
                    continue;
                }
                if (layout is MenuVerticalLayout menuVerticalLayout)
                {
                    writer.Write("vertical");
                    menuVerticalLayout.WritePacket(writer);
                    continue;
                }
                if (layout is MenuGridLayout menuGridLayout)
                {
                    writer.Write("grid");
                    menuGridLayout.WritePacket(writer);
                    continue;
                }
            }
        }

        public virtual void ReadPacket(NetworkReader reader)
        {
            name = reader.ReadString();
            childAlignment = (TextAnchor)reader.ReadInt32();

            rect = Packet.CreateFromPacket<RectValues>(reader);
            mask = reader.ReadBoolean();
            regenerate = reader.ReadBoolean();

            scrollable = reader.ReadBoolean();

            contentRect = Packet.CreateFromPacket<RectValues>(reader);

            onScrollUpFuncJSON = reader.ReadJSON();
            onScrollDownFuncJSON = reader.ReadJSON();
        }

        public virtual void WritePacket(NetworkWriter writer)
        {
            writer.Write(name);
            writer.Write((int)childAlignment);

            rect.WritePacket(writer);
            writer.Write(mask);
            writer.Write(regenerate);

            writer.Write(scrollable);

            contentRect.WritePacket(writer);

            writer.Write(onScrollUpFuncJSON);
            writer.Write(onScrollDownFuncJSON);
        }

        public virtual void Read(JSONNode jn)
        {
            name = jn["name"];
            childAlignment = (TextAnchor)jn["align"].AsInt;

            rect = RectValues.TryParse(jn["rect"], RectValues.Default);
            mask = jn["mask"].AsBool;
            regenerate = jn["regenerate"] == null || jn["regenerate"].AsBool;

            scrollable = jn["scrollable"].AsBool;

            onScrollUpFuncJSON = jn["on_scroll_up_func"];
            onScrollDownFuncJSON = jn["on_scroll_down_func"];
        }
    }
}
