using LSFunctions;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BaseMarker = DataManager.GameData.BeatmapData.Marker;

namespace BetterLegacy.Core.Data
{
    public class Marker : BaseMarker
    {
        public Marker() : base()
        {
            id = LSText.randomString(16);
        }

        public Marker(string _name, string _desc, int _color, float _time) : base(true, _name, _desc, _color, _time)
        {
            id = LSText.randomString(16);
        }
        
        public Marker(bool _active, string _name, string _desc, int _color, float _time) : base(_active, _name, _desc, _color, _time)
        {
            id = LSText.randomString(16);
        }

        public string id;

        public static Marker Parse(JSONNode jn)
        {
            bool active = jn["active"].AsBool;
            string name = "";
            if (jn["name"] != null)
                name = jn["name"];

            string desc = "";
            if (jn["desc"] != null)
                desc = jn["desc"];

            float time = jn["t"].AsFloat;

            int color = 0;
            if (jn["col"] != null)
                color = jn["col"].AsInt;

            return new Marker(active, name, desc, color, time);
        }

        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");
            jn["active"] = active.ToString();

            if (!string.IsNullOrEmpty(name))
                jn["name"] = name.ToString();

            if (!string.IsNullOrEmpty(desc) && desc != "Description")
                jn["desc"] = desc.ToString();

            if (color != 0)
                jn["col"] = color.ToString();

            jn["t"] = time.ToString();

            return jn;
        }

        public static Marker ParseVG(JSONNode jn)
        {
            return new Marker
            {
                id = jn["ID"] ?? LSText.randomString(16),
                name = jn["n"] ?? "",
                desc = jn["d"] ?? "",
                color = jn["c"] != null ? jn["c"].AsInt : 0,
                time = jn["t"] != null ? jn["t"].AsFloat : 0f,
            };
        }

        public JSONNode ToJSONVG()
        {
            var jn = JSON.Parse("{}");

            jn["ID"] = LSText.randomString(16);

            jn["n"] = name;
            jn["d"] = desc;
            jn["c"] = color;
            jn["t"] = time;

            return jn;
        }
    }
}
