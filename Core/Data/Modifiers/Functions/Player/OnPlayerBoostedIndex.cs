using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;
namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class OnPlayerBoostedIndex : OnPlayerBoosted
    {
        public OnPlayerBoostedIndex() => Modifier.values.Add("0");
        public override string Name => "onPlayerBoostedIndex";
        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var index = modifier.GetInt(0, 0, modifierLoop.variables);
            return PlayerManager.inst.players.TryGetAt(index, out PAPlayer player) && player && player.RuntimePlayer && player.RuntimePlayer.playerBoosted;
        }
        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable) => modifierCard.IntegerGenerator(modifier, reference, "Player Index", 0, 0);
    }
}
