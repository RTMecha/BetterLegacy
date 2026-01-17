using BetterLegacy.Configs;

namespace BetterLegacy.Core.Helpers
{
    /// <summary>
    /// Helper class for Discord.
    /// </summary>
    public static class DiscordHelper
    {
        public static string[] discordSubIcons = new string[]
        {
            "arcade",
            "editor",
            "play",
            "menu",
        };

        public static string[] discordIcons = new string[]
        {
            PA_LOGO_WHITE,
            PA_LOGO_BLACK,
        };

        public const string PA_LOGO_WHITE = "pa_logo_white";
        public const string PA_LOGO_BLACK = "pa_logo_black";
        public static string discordLevel = string.Empty;
        public static string discordDetails = string.Empty;
        public static string discordIcon = string.Empty;
        public static string discordArt = string.Empty;

        /// <summary>
        /// Updates the users' Discord status.
        /// </summary>
        /// <param name="level">Level state.</param>
        /// <param name="details">Details.</param>
        /// <param name="icon">Sub icon.</param>
        /// <param name="art">Main icon.</param>
        public static void UpdateDiscordStatus(string level, string details, string icon, string art = PA_LOGO_WHITE)
        {
            // change status
            DiscordController.inst.OnStateChange(CoreConfig.Instance.DiscordShowLevel.Value ? level : string.Empty);
            DiscordController.inst.OnArtChange(art);
            DiscordController.inst.OnIconChange(icon);
            DiscordController.inst.OnDetailsChange(details);

            // cache
            discordLevel = level;
            discordDetails = details;
            discordIcon = icon;
            discordArt = art;

            DiscordRpc.UpdatePresence(DiscordController.inst.presence);
        }
    }
}
