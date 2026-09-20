using BetterLegacy.Core.Data.Player;
namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class PlayerBoosted : PlayerTriggerBase
    {
        public PlayerBoosted(Requirement requirement) : base(requirement)
        {
            Name = "playerBoosted";
            if (requirement != Requirement.Nearest)
                Name += requirement.ToString();
            SetupModifier();
            if (requirement == Requirement.Index)
                Modifier.values.Add("0");
        }
        public override string Name { get; }
        public override bool CheckPlayer(Modifier modifier, ModifierLoop modifierLoop, PAPlayer player) => player && player.RuntimePlayer && player.RuntimePlayer.playerBoosted;
    }
}