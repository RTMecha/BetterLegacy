using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class VignetteTracksPlayer : ModifierActionBase
    {
        #region Constructors

        public VignetteTracksPlayer() => SetupModifier();

        #endregion

        #region Values

        public override string Name => "vignetteTracksPlayer";

        public override ModifierCategoryType Category => ModifierCategoryType.Events;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (!RTLevel.Current.eventEngine)
                return;

            var players = PlayerManager.Players;
            if (players.IsEmpty())
                return;

            var player = players[0].RuntimePlayer;

            if (!player || !player.rb)
                return;

            var cameraToViewportPoint = RTLevel.Cameras.FG.WorldToViewportPoint(player.rb.position);
            RTLevel.Current.eventEngine.SetOffset(7, 4, cameraToViewportPoint.x);
            RTLevel.Current.eventEngine.SetOffset(7, 5, cameraToViewportPoint.y);
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable) { }

        #endregion
    }
}
