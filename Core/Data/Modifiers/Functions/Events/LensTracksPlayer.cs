using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class LensTracksPlayer : ModifierActionBase
    {
        #region Constructors

        public LensTracksPlayer() => SetupModifier();

        #endregion

        #region Values

        public override string Name => "lensTracksPlayer";

        public override ModifierCategoryType Category => ModifierCategoryType.Events;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (!RTLevel.Current.eventEngine)
                return;

            if (PlayerManager.inst.players.IsEmpty())
                return;

            var player = PlayerManager.inst.players[0].RuntimePlayer;
            if (!player || !player.rb)
                return;

            var cameraToViewportPoint = RTLevel.Cameras.FG.WorldToViewportPoint(player.rb.position);
            RTLevel.Current.eventEngine.SetOffset(8, 1, cameraToViewportPoint.x - 0.5f);
            RTLevel.Current.eventEngine.SetOffset(8, 2, cameraToViewportPoint.y - 0.5f);
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable) { }

        #endregion
    }
}
