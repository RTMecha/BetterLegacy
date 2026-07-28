using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetPlayerMask : PlayerActionBase
    {
        #region Constructors

        public SetPlayerMask(Selector selector) : base("setPlayerMask", selector, "8", "0", "0", "0", "0", "255", "255") { }

        #endregion

        #region Functions

        public override void RunOnPlayer(Modifier modifier, ModifierLoop modifierLoop, PAPlayer player)
        {
            if (!player || !player.RuntimePlayer)
                return;

            var comparison = Parser.TryParse(modifier.GetValue(Index(0), modifierLoop.variables), true, UnityEngine.Rendering.CompareFunction.Always);
            var pass = Parser.TryParse(modifier.GetValue(Index(1), modifierLoop.variables), true, UnityEngine.Rendering.StencilOp.Keep);
            var fail = Parser.TryParse(modifier.GetValue(Index(2), modifierLoop.variables), true, UnityEngine.Rendering.StencilOp.Keep);
            var zFail = Parser.TryParse(modifier.GetValue(Index(3), modifierLoop.variables), true, UnityEngine.Rendering.StencilOp.Keep);
            var id = (byte)modifier.GetInt(Index(4), 0, modifierLoop.variables);
            var writeMask = (byte)modifier.GetInt(Index(5), 255, modifierLoop.variables);
            var readMask = (byte)modifier.GetInt(Index(6), 255, modifierLoop.variables);

            for (int i = 0; i < player.RuntimePlayer.playerObjects.Count; i++)
                player.RuntimePlayer.playerObjects[i].SetStencil(comparison, pass, fail, zFail, id, writeMask, readMask);
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            base.RenderModifierCard(modifier, modifierCard, reference, modifyable);
            modifierCard.DropdownGenerator(modifier, reference, "Comparison", Index(0), CoreHelper.ToOptionData<UnityEngine.Rendering.CompareFunction>());
            modifierCard.DropdownGenerator(modifier, reference, "Pass", Index(1), CoreHelper.ToOptionData<UnityEngine.Rendering.StencilOp>());
            modifierCard.DropdownGenerator(modifier, reference, "Fail", Index(2), CoreHelper.ToOptionData<UnityEngine.Rendering.StencilOp>());
            modifierCard.DropdownGenerator(modifier, reference, "ZFail", Index(3), CoreHelper.ToOptionData<UnityEngine.Rendering.StencilOp>());

            modifierCard.IntegerGenerator(modifier, reference, "ID", Index(4), max: 255);
            modifierCard.IntegerGenerator(modifier, reference, "Write Mask", Index(5), max: 255);
            modifierCard.IntegerGenerator(modifier, reference, "Read Mask", Index(6), max: 255);
        }

        #endregion
    }
}
