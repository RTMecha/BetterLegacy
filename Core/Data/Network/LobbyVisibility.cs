namespace BetterLegacy.Core.Data.Network
{
    /// <summary>
    /// Visibility of an online lobby.
    /// </summary>
    public enum LobbyVisibility
    {
        /// <summary>
        /// Lobby is publicly viewable.
        /// </summary>
        Public,
        /// <summary>
        /// Lobby is only viewable by friends.
        /// </summary>
        FriendsOnly,
        /// <summary>
        /// Lobby can only be invited to.
        /// </summary>
        Private,
        /// <summary>
        /// Lobby is not shown.
        /// </summary>
        Invisible,
    }
}
