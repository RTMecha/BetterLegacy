using SimpleJSON;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Indicates an object can be converted to and from JSON.
    /// </summary>
    public interface IJSON
    {
        /// <summary>
        /// If the object should serialize.
        /// </summary>
        public bool ShouldSerialize { get; }

        /// <summary>
        /// Parses and applies object values from JSON.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        public void ReadJSON(JSONNode jn);

        /// <summary>
        /// Converts the current object to JSON.
        /// </summary>
        /// <returns>Returns a JSONNode that represents the current object..</returns>
        public JSONNode ToJSON();
    }
}
