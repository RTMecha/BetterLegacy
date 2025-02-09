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
        public static Version VanillaVersion = new Version(Versions.LEGACY);

        #endregion

        #region Methods

        /// <summary>
        /// Checks if the version matches PA Legacy's version.
        /// </summary>
        /// <param name="version">Version number to compare.</param>
        /// <returns>Returns true if the version number matches the PA Legacy version numbers.</returns>
        public static bool IsMatchingVersion(string version) => version == GAME_VERSION || version == Versions.LEGACY;

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
        public static bool RequireUpdate(string version) => version != GAME_VERSION && version != Versions.LEGACY;

        /// <summary>
        /// Checks if an update is required.
        /// </summary>
        /// <param name="version">Version to compare.</param>
        /// <returns>Returns true if the version doesn't match the PA Legacy versions.</returns>
        public static bool RequireUpdate(Version version) => version != GameVersion && version != VanillaVersion;

        #endregion

        /// <summary>
        /// A list of updates.
        /// </summary>
        public class Versions
        {
            /// <summary>
            /// Legacy branch version.
            /// </summary>
            public const string LEGACY = "20.4.4";
            /// <summary>
            /// Depths' default value was changed.
            /// </summary>
            public const string DEPTH_DEFAULT_CHANGED = "23.1.4";
            /// <summary>
            /// Opacity slider was added.
            /// </summary>
            public const string OPACITY = "24.1.6a";
            /// <summary>
            /// Mode value was added to gradient event keyframes.
            /// </summary>
            public const string GRADIENT_MODE_EVENT = "24.1.7a";
            /// <summary>
            /// Text sprites changed and player bubble.
            /// </summary>
            public const string SPRITES_CHANGED = "24.2.2";
            /// <summary>
            /// Triggers were added.
            /// </summary>
            public const string EVENT_TRIGGER = "24.3.1";
            /// <summary>
            /// The Polygon shape was added.
            /// </summary>
            public const string CUSTOM_SHAPES = "25.2.1";
            /// <summary>
            /// Rotation keyframes now have a fixed setting and shake has more values.
            /// </summary>
            public const string FIXED_ROTATION_SHAKE = "25.2.2";
        }
    }
}
