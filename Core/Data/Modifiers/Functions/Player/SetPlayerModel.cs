using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetPlayerModel : ModifierActionBase
    {
        #region Constructors

        public SetPlayerModel() => SetupModifier(false, "0", "0");

        #endregion

        #region Values

        public override string Name => "setPlayerModel";

        public override CategoryType Category => CategoryType.Player;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifier.constant)
                return;

            var id = FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables);
            var index = modifier.GetInt(1, 0, modifierLoop.variables);

            if (!PlayersData.Current.playerModels.ContainsKey(id))
                return;

            PlayersData.Current.SetPlayerModel(index, id);
            PlayerManager.AssignPlayerModels();

            if (!PlayerManager.Players.TryGetAt(index, out PAPlayer player) || !player.RuntimePlayer)
                return;

            player.UpdatePlayerModel();

            player.RuntimePlayer.playerNeedsUpdating = true;
            player.RuntimePlayer.UpdateModel();
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.IntegerGenerator(modifier, reference, "Player Index", 1, 0, max: 3);
            var modelID = modifierCard.StringGenerator(modifier, reference, "Model ID", 0);
            EditorContextMenu.AddContextMenu(modelID.transform.Find("Input").gameObject,
                new ButtonElement("Select model", () => PlayerEditor.inst.OpenModelsPopup(model => modifierCard.SetValue(0, model.basePart.id, reference))));
        }

        #endregion
    }
}
