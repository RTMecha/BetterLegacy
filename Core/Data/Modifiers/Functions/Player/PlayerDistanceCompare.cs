using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class PlayerDistanceCompare : ModifierTriggerBase
    {
        #region Constructors

        public PlayerDistanceCompare(NumberComparison comparison)
        {
            this.comparison = comparison;
            Name = "playerDistance" + comparison.ToString();
            SetupModifier("5");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Player;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.FullBeatmapCompatible;

        readonly NumberComparison comparison;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not ITransformable transformable)
                return false;

            var pos = transformable.GetFullPosition();
            float num = modifier.GetFloat(0, 0f, modifierLoop.variables);
            for (int i = 0; i < PlayerManager.Players.Count; i++)
            {
                var player = PlayerManager.Players[i];
                if (player && player.RuntimePlayer && player.RuntimePlayer.rb && comparison.Compare(Vector2.Distance(player.RuntimePlayer.rb.position, pos), num))
                    return true;
            }
            return false;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.SingleGenerator(modifier, reference, "Compare To", 0, 1f);
        }

        #endregion
    }
}
