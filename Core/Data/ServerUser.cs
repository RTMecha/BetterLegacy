using SimpleJSON;

using BetterLegacy.Core.Data.Network;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Represents a user on the Arcade server.
    /// </summary>
    public class ServerUser : PAObject<ServerUser>, IPacket
    {
        #region Constructors

        public ServerUser() { }
        public ServerUser(string id) => ID = id;

        #endregion

        #region Values

        /// <summary>
        /// Identification of the user.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Steam identification number of the user.
        /// </summary>
        public string SteamID { get; set; }

        /// <summary>
        /// Display name of the user.
        /// </summary>
        public string DisplayName { get; set; }

        #endregion

        #region Functions

        public override void CopyData(ServerUser orig, bool newID = true)
        {
            ID = orig.ID;
            SteamID = orig.SteamID;
            DisplayName = orig.DisplayName;
        }

        public override void ReadJSON(JSONNode jn)
        {
            if (jn["id"] != null)
                ID = jn["id"];
            if (jn["steam_id"] != null)
                SteamID = jn["steam_id"];
            if (jn["display_name"] != null)
                DisplayName = jn["display_name"];
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            if (!string.IsNullOrEmpty(ID))
                jn["id"] = ID;
            if (!string.IsNullOrEmpty(SteamID))
                jn["steam_id"] = SteamID;
            if (!string.IsNullOrEmpty(DisplayName))
                jn["display_name"] = DisplayName;

            return jn;
        }

        public void ReadPacket(NetworkReader reader)
        {
            ID = reader.ReadString();
            SteamID = reader.ReadString();
            DisplayName = reader.ReadString();
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(ID);
            writer.Write(SteamID);
            writer.Write(DisplayName);
        }

        #endregion

        #region Operators

        public static implicit operator string(ServerUser serverUser) => serverUser.ID;
        public static implicit operator ServerUser(string id) => new ServerUser(id);

        #endregion
    }
}
