using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Player;

namespace BetterLegacy.Companion.Data
{
    /// <summary>
    /// Notice parameters passed to Example's brain.
    /// </summary>
    public class NoticeParameters : Exists
    {
        public NoticeParameters() { }
    }

    /// <summary>
    /// Passes data related to a player.
    /// </summary>
    public class PlayerNoticeParameters : NoticeParameters
    {
        public PlayerNoticeParameters() { }

        public PlayerNoticeParameters(CustomPlayer player)
        {
            this.player = player;
        }

        /// <summary>
        /// Player reference.
        /// </summary>
        public CustomPlayer player;
    }
}
