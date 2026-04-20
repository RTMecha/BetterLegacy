using System.Linq;
using System.Runtime.CompilerServices;

using BetterLegacy.Configs;
using BetterLegacy.Core.Data;

namespace BetterLegacy.Core.Helpers
{
    /// <summary>
    /// Helper class for Discord.
    /// </summary>
    public static class DiscordHelper
    {
        #region Values

        #region Assets

        /// <summary>
        /// Sub icons that can be used.
        /// </summary>
        public static string[] subIcons = new string[]
        {
            ARCADE,
            EDITOR,
            PLAY,
            MENU,
        };

        /// <summary>
        /// Main icons that can be used.
        /// </summary>
        public static string[] icons = new string[]
        {
            LOGO_LEGACY,
            LOGO_DARK,
            LOGO_PROTOTYPE,
            LOGO_ALPHA,
            LOGO_GLITCH,
            LOGO_AFTERBEAT,
        };

        /// <summary>
        /// The Legacy logo.
        /// </summary>
        public const string LOGO_LEGACY = "logo_legacy";

        /// <summary>
        /// The PA Heart (white) logo on a black background.
        /// </summary>
        public const string LOGO_DARK = "logo_dark";

        /// <summary>
        /// The prototype version logo.
        /// </summary>
        public const string LOGO_PROTOTYPE = "logo_prototype";

        /// <summary>
        /// The alpha version (1.0.0-3.0.0) logo.
        /// </summary>
        public const string LOGO_ALPHA = "logo_alpha";

        /// <summary>
        /// The glitch logo. (logo 2021-2026)
        /// </summary>
        public const string LOGO_GLITCH = "logo_glitch";

        /// <summary>
        /// The AfterBeat logo. (Post rebrand)
        /// </summary>
        public const string LOGO_AFTERBEAT = "logo_afterbeat";

        /// <summary>
        /// Icon that represents the arcade.
        /// </summary>
        public const string ARCADE = "arcade";

        /// <summary>
        /// Icon that represents the editor.
        /// </summary>
        public const string EDITOR = "editor";

        /// <summary>
        /// Icon that represents playing.
        /// </summary>
        public const string PLAY = "play";

        /// <summary>
        /// Icon that represents the interfaces (menus).
        /// </summary>
        public const string MENU = "menu";

        #endregion

        #region Default Keywords

        /// <summary>
        /// In editor details.
        /// </summary>
        public const string IN_EDITOR = "In Editor";

        /// <summary>
        /// In arcade details.
        /// </summary>
        public const string IN_ARCADE = "In Arcade";

        /// <summary>
        /// In story details.
        /// </summary>
        public const string IN_STORY = "In Story";

        /// <summary>
        /// In menu details.
        /// </summary>
        public const string IN_MENU = "In Menu";

        #endregion

        #region Cache

        /// <summary>
        /// Current status state.
        /// </summary>
        public static string state = string.Empty;

        /// <summary>
        /// Current status details.
        /// </summary>
        public static string details = string.Empty;

        /// <summary>
        /// Current status icon.
        /// </summary>
        public static string icon = string.Empty;

        /// <summary>
        /// Current status art.
        /// </summary>
        public static string art = LOGO_LEGACY;

        #endregion

        #endregion

        #region Functions

        /// <summary>
        /// Updates the users' Discord status.
        /// </summary>
        /// <param name="level">Level state.</param>
        /// <param name="details">Details.</param>
        /// <param name="icon">Sub icon.</param>
        /// <param name="art">Main icon.</param>
        public static void UpdateDiscordStatus(string state, string details, string icon, string art = LOGO_LEGACY)
        {
            // change status
            DiscordController.inst.OnStateChange(CoreConfig.Instance.DiscordShowLevel.Value ? state : string.Empty);
            DiscordController.inst.OnArtChange(art);
            DiscordController.inst.OnIconChange(icon);
            DiscordController.inst.OnDetailsChange(details);

            // cache
            DiscordHelper.state = state;
            DiscordHelper.details = details;
            DiscordHelper.icon = icon;
            DiscordHelper.art = art;

            DiscordRpc.UpdatePresence(DiscordController.inst.presence);
        }

        #endregion
    }
}