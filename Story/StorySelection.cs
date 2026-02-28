namespace BetterLegacy.Story
{
    /// <summary>
    /// Represents a way to select a level in the story mode.
    /// </summary>
    public struct StorySelection
    {
        /// <summary>
        /// Chapter index of the level.
        /// </summary>
        public int chapter;

        /// <summary>
        /// Index of the level.
        /// </summary>
        public int level;

        /// <summary>
        /// Cutscene destination.
        /// </summary>
        public CutsceneDestination cutsceneDestination;

        /// <summary>
        /// Cutscene index of the level.
        /// </summary>
        public int cutsceneIndex;

        /// <summary>
        /// If the level is a bonus chapter level.
        /// </summary>
        public bool bonus;

        /// <summary>
        /// If cutscenes should be skipped. If set to <see langword="true"/>, <see cref="cutsceneIndex"/> is ignored.
        /// </summary>
        public bool skipCutscenes;
    }
}
