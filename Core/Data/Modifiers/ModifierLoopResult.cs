namespace BetterLegacy.Core.Data.Modifiers
{
    /// <summary>
    /// Result of <see cref="ModifierLoop"/>.
    /// </summary>
    public struct ModifierLoopResult
    {
        public ModifierLoopResult(bool returned, bool result, Modifier.Type previousType, int index)
        {
            this.returned = returned;
            this.result = result;
            this.previousType = previousType;
            this.index = index;
        }

        #region Values

        /// <summary>
        /// If the loop has returned.
        /// </summary>
        public bool returned;

        /// <summary>
        /// The last trigger result.
        /// </summary>
        public bool result;

        /// <summary>
        /// The previous modifier type.
        /// </summary>
        public Modifier.Type previousType;

        /// <summary>
        /// The last index.
        /// </summary>
        public int index;

        #endregion
    }
}
