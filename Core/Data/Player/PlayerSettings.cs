using SimpleJSON;

namespace BetterLegacy.Core.Data.Player
{
    /// <summary>
    /// Represents custom player data.
    /// </summary>
    public class PlayerSettings : PAObject<PlayerSettings>
    {
        // TODO: USE THIS INSTEAD OF THE PLAYER CONFIG AND HAVE THIS SYNC ACROSS CLIENTS SOMEHOW!

        #region Constructors

        public PlayerSettings() { }

        #endregion

        #region Values

        public int index;

        public string playerModelID;

        public int colorSlot = -1;

        #endregion

        #region Functions

        public override void CopyData(PlayerSettings orig, bool newID = true)
        {
            index = orig.index;
            playerModelID = orig.playerModelID;
            colorSlot = orig.colorSlot;
        }

        public override void ReadJSON(JSONNode jn)
        {
            index = jn["index"].AsInt;
            playerModelID = jn["model_id"];
            colorSlot = jn["col"].AsInt;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["index"] = index;
            if (!string.IsNullOrEmpty(playerModelID))
                jn["model_id"] = playerModelID;
            if (colorSlot != -1)
                jn["col"] = colorSlot;

            return jn;
        }

        #endregion
    }
}
