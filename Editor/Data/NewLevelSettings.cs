using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Represents settings used for creating new levels.
    /// </summary>
    public struct NewLevelSettings
    {
        public NewLevelSettings(string audioPath = null, string levelName = EditorLevelManager.DEFAULT_LEVEL_NAME, string songArtist = EditorLevelManager.DEFAULT_ARTIST, string songTitle = EditorLevelManager.DEFAULT_SONG_TITLE, int difficulty = EditorLevelManager.DEFAULT_DIFFICULTY)
        {
            this.audioPath = audioPath ?? string.Empty;
            this.levelName = levelName;
            this.songArtist = songArtist;
            this.songTitle = songTitle;
            this.difficulty = difficulty;
        }

        #region Values

        /// <summary>
        /// Path to the song to use for the level.
        /// </summary>
        public string audioPath;

        /// <summary>
        /// Name of the level.
        /// </summary>
        public string levelName;

        /// <summary>
        /// Artist of the song used.
        /// </summary>
        public string songArtist;

        /// <summary>
        /// Title of the song used.
        /// </summary>
        public string songTitle;

        /// <summary>
        /// Difficulty the level is planned to be.
        /// </summary>
        public int difficulty;

        #endregion
    }
}
