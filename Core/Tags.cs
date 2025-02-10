namespace BetterLegacy.Core
{
    /// <summary>
    /// A list of tags used by PA.
    /// </summary>
    public static class Tags
    {
        /// <summary>
        /// Objects with collisions don't hurt the player.
        /// </summary>
        public const string HELPER = "Helper";
        /// <summary>
        /// Objects that can damage the player.
        /// </summary>
        public const string OBJECTS = "Objects";
        /// <summary>
        /// Background objects.
        /// </summary>
        public const string BACKGROUNDS = "Backgrounds";
        /// <summary>
        /// Represents the hitbox of the player.
        /// </summary>
        public const string PLAYER = "Player";
        /// <summary>
        /// The main camera that renders foreground beatmap objects.
        /// </summary>
        public const string MAIN_CAMERA = "MainCamera";
    }
}
