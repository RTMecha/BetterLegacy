using SimpleJSON;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Represents a user on the Arcade server.
    /// </summary>
    public class ServerUser : PAObject<ServerUser>
    {
        public ServerUser() { }
        public ServerUser(string id) => ID = id;

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

        public static implicit operator string(ServerUser serverUser) => serverUser.ID;
        public static implicit operator ServerUser(string id) => new ServerUser(id);
    }
}
