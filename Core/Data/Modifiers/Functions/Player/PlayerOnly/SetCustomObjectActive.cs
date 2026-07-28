using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetCustomObjectActive : ModifierActionBase
    {
        #region Constructors

        public SetCustomObjectActive() => Modifier = CreateModifier(Name, "False", "0", "True");

        #endregion

        #region Values

        public override string Name => "setCustomObjectActive";

        public override CategoryType Category => CategoryType.Player;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.FullPlayerCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var id = modifier.GetValue(1, modifierLoop.variables);
            var player = modifierLoop.reference is RTPlayer.RTCustomPlayerObject customPlayerObject ? customPlayerObject.Player.Core : modifierLoop.reference as PAPlayer;

            if (player && player.RuntimePlayer && player.RuntimePlayer.customObjects.TryFind(x => x.id == id, out RTPlayer.RTCustomPlayerObject customObject))
                customObject.active = modifier.GetBool(0, false, modifierLoop.variables);
        }

        public override void Inactive(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifier.GetBool(2, true, modifierLoop.variables) && modifierLoop.reference is PAPlayer player && player.RuntimePlayer.customObjects.TryFind(x => x.id == modifier.GetValue(1, modifierLoop.variables), out RTPlayer.RTCustomPlayerObject customObject))
                customObject.active = !modifier.GetBool(0, false, modifierLoop.variables);
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "ID", 1);
            modifierCard.BoolGenerator(modifier, reference, "Enabled", 0);
            modifierCard.BoolGenerator(modifier, reference, "Reset", 2);
        }

        #endregion
    }
}
