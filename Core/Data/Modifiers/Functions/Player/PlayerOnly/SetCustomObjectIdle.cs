using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetCustomObjectIdle : ModifierActionBase
    {
        #region Constructors

        public SetCustomObjectIdle() => Modifier = CreateModifier(Name, "0", "True");

        #endregion

        #region Values

        public override string Name => "setCustomObjectIdle";

        public override ModifierCategoryType Category => ModifierCategoryType.Player;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.FullPlayerCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var id = modifier.GetValue(0, modifierLoop.variables);
            var idle = modifier.GetBool(1, true, modifierLoop.variables);
            var customPlayerObject = modifierLoop.reference as RTCustomPlayerObject;
            var player = customPlayerObject ? customPlayerObject.Player.Core : modifierLoop.reference as PAPlayer;

            if (!player || !player.RuntimePlayer)
                return;

            var customObject = string.IsNullOrEmpty(id) && customPlayerObject ? customPlayerObject : player.RuntimePlayer.customObjects.Find(x => x.id == id);

            if (customObject)
                customObject.idle = idle;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "ID", 0);
            modifierCard.BoolGenerator(modifier, reference, "Idle", 1);
        }

        #endregion
    }
}
