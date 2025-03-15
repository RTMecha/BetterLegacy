using BetterLegacy.Editor.Data;
using LSFunctions;
using SimpleJSON;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class Marker : Exists
    {
        public Marker()
        {
            id = LSText.randomString(16);
        }

        public Marker(string name, string desc, int color, float time) : this(LSText.randomString(16), name, desc, color, time) { }
        
        public Marker(string id, string name, string desc, int color, float time)
        {
            this.id = id;
            this.name = name;
            this.desc = desc;
            this.color = color;
            this.time = time;
        }

        /// <summary>
        /// ID of the Marker to be used for editor identifying.
        /// </summary>
        public string id;

        public bool active;

        public string name = "";

        public string desc = "Description";

        public int color;

        public float time;

        /// <summary>
        /// Timeline Marker reference.
        /// </summary>
        public TimelineMarker timelineMarker;

        #region Methods

        /// <summary>
        /// Creates a copy of a <see cref="Marker"/>.
        /// </summary>
        /// <param name="orig">Original to copy.</param>
        /// <param name="newID">True if a new ID should be generated, false if it should use the original.</param>
        /// <returns>Returns a copied <see cref="Marker"/>.</returns>
        public static Marker DeepCopy(Marker orig, bool newID = true) => new Marker(newID ? LSText.randomString(16) : orig.id, orig.name, orig.desc, orig.color, orig.time);

        /// <summary>
        /// Parses a Marker from an LS format file.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns a parsed marker.</returns>
        public static Marker Parse(JSONNode jn) => new Marker(jn["name"]?.Value ?? "", jn["desc"]?.Value ?? "", jn["col"].AsInt, jn["t"].AsFloat);

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

        #endregion

        public static implicit operator bool(Marker exists) => exists != null;
    }
}
