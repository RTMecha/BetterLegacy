using SimpleJSON;

using BetterLegacy.Core.Data.Network;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Represents a global level variable used for modifiers or math evaluators.
    /// </summary>
    public class BeatmapVariable : PAObject<BeatmapVariable>, IPacket
    {
        #region Constructors

        public BeatmapVariable() { }

        public BeatmapVariable(string name, string value)
        {
            this.name = name;
            this.value = value;
        }

        public BeatmapVariable(Type type, string name, string value)
        {
            this.type = type;
            this.name = name;
            this.value = value;
        }

        #endregion

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

        public void ReadPacket(NetworkReader reader)
        {
            name = reader.ReadString();
            value = reader.ReadString();
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(name);
            writer.Write(value);
        }

        #endregion
    }
}
