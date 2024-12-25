namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Holds some information for the core of Project Arrhythmia.
    /// </summary>
    public class ProjectArrhythmia
    {
        #region Constants

        /// <summary>
        /// The default application title of Project Arrhythmia.
        /// </summary>
        public const string TITLE = "Project Arrhythmia";

        /// <summary>
        /// The version of PA Legacy. Calculated from version 4.0.19 onwards.
        /// </summary>
        public const string GAME_VERSION = "4.1.16";

        /// <summary>
        /// The version vanilla PA Legacy uses.
        /// </summary>
        public const string VANILLA_VERSION = "20.4.4";

        /// <summary>
        /// The Steam App ID of Project Arrhythmia.
        /// </summary>
        public const uint STEAM_APP_ID = 440310U;

        #endregion

        #region Properties

        /// <summary>
        /// The game version of PA Legacy.
        /// </summary>
        public static Version GameVersion = new Version(GAME_VERSION);

        /// <summary>
        /// The vanilla version number of PA Legacy.
        /// </summary>
        public static Version VanillaVersion = new Version(VANILLA_VERSION);

        #endregion

        #region Methods

        /// <summary>
        /// Checks if the version matches PA Legacy's version.
        /// </summary>
        /// <param name="version">Version number to compare.</param>
        /// <returns>Returns true if the version number matches the PA Legacy version numbers.</returns>
        public static bool IsMatchingVersion(string version) => version == GAME_VERSION || version == VANILLA_VERSION;

        /// <summary>
        /// Checks if the version matches PA Legacy's version.
        /// </summary>
        /// <param name="version">Version number to compare.</param>
        /// <returns>Returns true if the version number matches the PA Legacy version numbers.</returns>
        public static bool IsMatchingVersion(Version version) => version == GameVersion || version == VanillaVersion;

        /// <summary>
        /// Checks if an update is required.
        /// </summary>
        /// <param name="version">Version to compare.</param>
        /// <returns>Returns true if the version doesn't match the PA Legacy versions.</returns>
        public static bool RequireUpdate(string version) => version != GAME_VERSION && version != VANILLA_VERSION;

        /// <summary>
        /// Checks if an update is required.
        /// </summary>
        /// <param name="version">Version to compare.</param>
        /// <returns>Returns true if the version doesn't match the PA Legacy versions.</returns>
        public static bool RequireUpdate(Version version) => version != GameVersion && version != VanillaVersion;

        #endregion
    }
}
