namespace BetterLegacy.Core.Data.Modifiers.Updaters
{
    public class TextUpdater : ModifierUpdaterBase
    {
        public override bool RequiresUpdate(Modifier modifier) => modifier.Name == "removeTextAt" || modifier.Name == "removeTextOtherAt";

        public override void UpdateModifier(Modifier modifier, IModifyable modifyable)
        {
            if (modifier.Name == "removeTextAt")
                modifier.name = "removeAtText";
            if (modifier.Name == "removeTextOtherAt")
                modifier.name = "removeAtTextOther";
        }
    }
}
