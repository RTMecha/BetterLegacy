namespace BetterLegacy.Core.Data.Network
{
    /// <summary>
    /// State of an online lobby.
    /// </summary>
    public enum LobbyState
    {
        /// <summary>
        /// Lobby is joinable.
        /// </summary>
        Joinable,
        /// <summary>
        /// Lobby is busy, users will not be able to join.
        /// </summary>
        Busy,
    }
}
