namespace BetterLegacy.Core.Data.Modifiers.Updaters
{
    public class PlaySoundOnlineUpdater : ModifierUpdaterBase
    {
        public override bool RequiresUpdate(Modifier modifier) => modifier.Name == "playOnlineSound";

        public override void UpdateModifier(Modifier modifier, IModifyable modifyable) => modifier.name = "playSoundOnline";
    }
}
