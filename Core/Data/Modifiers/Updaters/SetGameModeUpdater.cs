namespace BetterLegacy.Core.Data.Modifiers.Updaters
{
    public class SetGameModeUpdater : ModifierUpdaterBase
    {
        public override bool RequiresUpdate(Modifier modifier) => modifier.Name == "gameMode";

        public override void UpdateModifier(Modifier modifier, IModifyable modifyable) => modifier.name = "setGameMode";
    }
}
