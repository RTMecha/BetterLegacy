using SimpleJSON;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Represents a global level variable used for modifiers or math evaluators.
    /// </summary>
    public class BeatmapVariable : PAObject<BeatmapVariable>
    {
        #region Values

        /// <summary>
        /// Name of the variable.
        /// </summary>
        public string name;

        /// <summary>
        /// Value of the variable.
        /// </summary>
        public string value;

        /// <summary>
        /// The type of the variable.
        /// </summary>
        public Type type;

        /// <summary>
        /// The type of a variable.
        /// </summary>
        public enum Type
        {
            /// <summary>
            /// The variable is for modifiers.
            /// </summary>
            Modifier,
            /// <summary>
            /// The variable is for math evaluators.
            /// </summary>
            Math,
        }

        #endregion

        #region Functions

        public override void CopyData(BeatmapVariable orig, bool newID = true)
        {
            id = newID ? GetStringID() : orig.id;
            name = orig.name;
            value = orig.value;
        }

        public override void ReadJSON(JSONNode jn)
        {
            name = jn["n"];
            value = jn["v"];
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            if (!string.IsNullOrEmpty(name))
                jn["n"] = name;
            if (!string.IsNullOrEmpty(value))
                jn["v"] = value;

            return jn;
        }

        #endregion
    }
}
