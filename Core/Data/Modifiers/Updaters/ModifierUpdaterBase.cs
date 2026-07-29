namespace BetterLegacy.Core.Data.Modifiers.Updaters
{
    /// <summary>
    /// Class for handling updating modifiers.
    /// </summary>
    public abstract class ModifierUpdaterBase : Exists
    {
        /// <summary>
        /// Checks if a modifier requires an update.
        /// </summary>
        /// <param name="modifier">Modifier to check.</param>
        /// <returns>Returns <see langword="true"/> if the modifier requires an update, otherwise returns <see langword="false"/>.</returns>
        public abstract bool RequiresUpdate(Modifier modifier);

        /// <summary>
        /// Updates the modifier.
        /// </summary>
        /// <param name="modifier">Modifier to update.</param>
        /// <param name="modifyable">Modifyable object reference. Used for cases where another modifier must be inserted into the modifier list.</param>
        public abstract void UpdateModifier(Modifier modifier, IModifyable modifyable);
    }
}
