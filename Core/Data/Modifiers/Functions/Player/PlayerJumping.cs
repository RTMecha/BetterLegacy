using BetterLegacy.Core.Data.Player;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class PlayerJumping : PlayerTriggerBase
    {
        #region Constructors

        public PlayerJumping(Requirement requirement) : base(requirement)
        {
            Name = "playerJumping";
            if (requirement != Requirement.Nearest)
                Name += requirement.ToString();
            SetupModifier();
            if (requirement == Requirement.Index)
                Modifier.values.Add("0");
        }

        #endregion

        #region Values

        public override string Name { get; }

        #endregion

        #region Functions

        public override bool CheckPlayer(Modifier modifier, ModifierLoop modifierLoop, PAPlayer player) => player && player.RuntimePlayer && player.RuntimePlayer.Jumping;

        #endregion
    }
}
