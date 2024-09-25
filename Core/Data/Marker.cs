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

        public Marker(string name, string desc, int color, float time) : base(true, name, desc, color, time)
        {
            id = LSText.randomString(16);
        }
        
        public Marker(bool active, string name, string desc, int color, float time) : base(active, name, desc, color, time)
        {
            id = LSText.randomString(16);
        }

        public Marker(string id, string name, string desc, int color, float time) : base(true, name, desc, color, time)
        {
            this.id = id;
        }

        /// <summary>
        /// ID of the Marker to be used for editor identifying.
        /// </summary>
        public string id;

        /// <summary>
        /// Parses a Marker from an LS format file.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns a parsed marker.</returns>
        public static Marker Parse(JSONNode jn) => new Marker(jn["active"].AsBool, jn["name"]?.Value ?? "", jn["desc"]?.Value ?? "", jn["col"].AsInt, jn["t"].AsFloat);

        /// <summary>
        /// Converts the marker to a JSON Object in the LS format.
        /// </summary>
        /// <returns>Returns a JSON Object representing the Marker.</returns>
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

        /// <summary>
        /// Parses a Marker from a VG format file.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns a parsed marker.</returns>
        public static Marker ParseVG(JSONNode jn) => new Marker(jn["ID"]?.Value ?? LSText.randomString(16), jn["n"] ?? "", jn["d"] ?? "", jn["c"].AsInt, jn["t"].AsFloat);

        /// <summary>
        /// Converts the marker to a JSON Object in the VG format.
        /// </summary>
        /// <returns>Returns a JSON Object representing the Marker.</returns>
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

        public override string ToString() => $"{id} - {name}";
    }
}
