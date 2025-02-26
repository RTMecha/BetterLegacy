using LSFunctions;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Used for setting an objects' new ID.
    /// </summary>
    public struct IDPair
    {
        public IDPair(string oldID) : this(oldID, LSText.randomString(16)) { }

        public IDPair(string oldID, string newID)
        {
            this.oldID = oldID;
            this.newID = newID;
        }

        /// <summary>
        /// The objects' original ID.
        /// </summary>
        public string oldID;

        /// <summary>
        /// The ID to assign to the duplicated object.
        /// </summary>
        public string newID;
    }
}
