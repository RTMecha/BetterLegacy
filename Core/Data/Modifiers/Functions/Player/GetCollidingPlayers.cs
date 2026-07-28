using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetCollidingPlayers : ModifierActionBase
    {
        #region Constructors

        public GetCollidingPlayers() => SetupModifier("PLAYER_COLLIDE_VAR");

        #endregion

        #region Values

        public override string Name => "getCollidingPlayers";

        public override CategoryType Category => CategoryType.Player;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        public override Sprite Icon => EditorSprites.DownArrow;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.collider)
                return;

            var collider = runtimeObject.visualObject.collider;

            var players = PlayerManager.Players;
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                modifierLoop.variables[FormatStringVariables(modifier.GetValue(0), modifierLoop.variables) + "_" + i] = (player.RuntimePlayer && player.RuntimePlayer.CurrentCollider && player.RuntimePlayer.CurrentCollider.IsTouching(collider)).ToString();
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
        }

        #endregion
    }
}
