using SimpleJSON;

using BetterLegacy.Core.Data.Network;

namespace BetterLegacy.Core.Data.Player
{
    /// <summary>
    /// Represents custom player data.
    /// </summary>
    public class PlayerSettings : PAObject<PlayerSettings>, IPacket
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

        public void ReadPacket(NetworkReader reader)
        {
            index = reader.ReadInt32();
            playerModelID = reader.ReadString();
            colorSlot = reader.ReadInt32();
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(index);
            writer.Write(playerModelID);
            writer.Write(colorSlot);
        }

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
