using LSFunctions;

using SimpleJSON;

using BetterLegacy.Editor.Data;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class Marker : PAObject<Marker>
    {
        public Marker() { }

        public Marker(string name, string desc, int color, float time) : this(LSText.randomString(16), name, desc, color, time) { }
        
        public Marker(string id, string name, string desc, int color, float time)
        {
            this.id = id;
            this.name = name;
            this.desc = desc;
            this.color = color;
            this.time = time;
        }

        public string name = "";

        public string desc = "Description";

        public int color;

        public float time;

        /// <summary>
        /// Timeline Marker reference.
        /// </summary>
        public TimelineMarker timelineMarker;

        #region Methods

        public override void CopyData(Marker orig, bool newID = true)
        {
            id = newID ? LSText.randomString(16) : orig.id;
            name = orig.name;
            desc = orig.desc;
            color = orig.color;
            time = orig.time;
        }

        public override Marker Copy(bool newID = true)
        {
            var marker = new Marker();
            marker.CopyData(this, newID);
            return marker;
        }

        /// <summary>
        /// Parses a Marker from a VG format file.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns a parsed marker.</returns>
        public static Marker ParseVG(JSONNode jn, Version version = default)
        {
            var marker = new Marker();
            marker.ReadJSONVG(jn, version);
            return marker;
        }

        /// <summary>
        /// Parses a Marker from an LS format file.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns a parsed marker.</returns>
        public static Marker Parse(JSONNode jn)
        {
            var marker = new Marker();
            marker.ReadJSON(jn);
            return marker;
        }

        public override void ReadJSONVG(JSONNode jn, Version version = default)
        {
            //id = LSText.randomString(16); // don't need to read the id
            name = jn["n"] ?? string.Empty;
            desc = jn["d"] ?? string.Empty;
            color = jn["c"].AsInt;
            time = jn["t"].AsFloat;
        }

        public override void ReadJSON(JSONNode jn)
        {
            name = jn["name"] ?? string.Empty;
            desc = jn["desc"] ?? string.Empty;
            color = jn["col"].AsInt;
            time = jn["t"].AsFloat;
        }

        public override JSONNode ToJSONVG()
        {
            var jn = JSON.Parse("{}");

            jn["ID"] = LSText.randomString(16);

            jn["n"] = name;
            jn["d"] = desc;
            jn["c"] = color;
            jn["t"] = time;

            return jn;
        }

        public override JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");
            if (!string.IsNullOrEmpty(name))
                jn["name"] = name;

            if (!string.IsNullOrEmpty(desc) && desc != "Description")
                jn["desc"] = desc;

            if (color != 0)
                jn["col"] = color;

            jn["t"] = time;

            return jn;
        }

        #endregion

        #region Operators

        public override bool Equals(object obj) => obj is Marker paObj && id == paObj.id;

        public override int GetHashCode() => base.GetHashCode();

        public override string ToString() => $"{id} - {name}";

        #endregion
    }
}
