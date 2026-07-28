namespace BetterLegacy.Core.Data.Modifiers.Updaters
{
    // TODO: add these to the modifier functions base class
    public abstract class ModifierUpdaterBase : Exists
    {
        public abstract bool RequiresUpdate(Modifier modifier);

        public abstract void UpdateModifier(Modifier modifier, IModifyable modifyable);
    }
}
