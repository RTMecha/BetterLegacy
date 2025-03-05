using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using SimpleJSON;

namespace BetterLegacy.Companion.Data
{
    /// <summary>
    /// Represents an <see cref="ExampleModule"/> attribute.<br></br>
    /// This is for storing specific variables across modules.
    /// </summary>
    public class ExampleAttribute : Exists
    {
        public ExampleAttribute(string id, double value, double min, double max)
        {
            this.id = id;
            this.min = min;
            this.max = max;
            Value = value;
        }

        public ExampleAttribute(string id, double value, double min, double max, bool integer)
        {
            this.id = id;
            this.min = min;
            this.max = max;
            this.integer = integer;
            Value = value;
        }

        /// <summary>
        /// Alphanumeric identification.
        /// </summary>
        public string id;

        double value;
        readonly double min;
        readonly double max;
        readonly bool integer;

        /// <summary>
        /// Value of the attribute.
        /// </summary>
        public double Value
        {
            get => RTMath.ClampZero(integer ? RTMath.Round(value) : value, min, max);
            set => this.value = RTMath.ClampZero(integer ? RTMath.Round(value) : value, min, max);
        }

        /// <summary>
        /// Parses an attribute.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns a parsed attribute.</returns>
        public static ExampleAttribute Parse(JSONNode jn) => new ExampleAttribute(jn["id"], jn["val"].AsDouble, jn["min"].AsDouble, jn["max"].AsDouble, jn["int"].AsInt == 1);

        /// <summary>
        /// Converts the attribute to JSON.
        /// </summary>
        /// <returns>Returns a JSON object representing the attribute.</returns>
        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");
            jn["id"] = id;
            jn["val"] = value;

            if (min != 0 || max != 0)
            {
                jn["min"] = min;
                jn["max"] = max;
            }

            if (integer)
                jn["int"] = integer ? 0 : 1;

            return jn;
        }

        public override string ToString() => $"{id} = {Value} ({value})";
    }

}
