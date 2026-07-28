namespace BetterLegacy.Core.Data.Modifiers.Updaters
{
    public class RigidbodyModifierUpdater : ModifierUpdaterBase
    {
        public override bool RequiresUpdate(Modifier modifier) => modifier.Name == "rigidbody" && modifier.version == 0;

        public override void UpdateModifier(Modifier modifier, IModifyable modifyable)
        {
            modifier.values.RemoveAt(0);
            modifier.version++;
        }
    }
}
