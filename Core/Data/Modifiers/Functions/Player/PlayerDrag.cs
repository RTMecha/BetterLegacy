using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    // TODO: fix this modifier
    // it's supposed to move the players along with the object as it moves
    public class PlayerDrag : ModifierActionBase
    {
        #region Constructors

        public PlayerDrag() => SetupModifier("True", "False", "False");

        #endregion

        #region Values

        public override string Name => "playerDrag";

        public override CategoryType Category => CategoryType.Player;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return;

            var usePosition = modifier.GetBool(0, false, modifierLoop.variables);
            var useScale = modifier.GetBool(1, false, modifierLoop.variables);
            var useRotation = modifier.GetBool(2, false, modifierLoop.variables);

            var prevPos = !usePosition ? Vector3.zero : beatmapObject.GetFullPosition();
            var prevSca = !useScale ? Vector3.zero : beatmapObject.GetFullScale();
            var prevRot = !useRotation ? Vector3.zero : beatmapObject.GetFullRotation(true);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();

                var player = PlayerManager.GetClosestPlayer(pos);
                if (!player || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                var rb = player.RuntimePlayer.rb;

                Vector2 distance = Vector2.zero;
                if (usePosition)
                    distance = pos - prevPos;
                if (useScale)
                {
                    var playerDistance = Vector3.Distance(pos, rb.position);

                    var sca = beatmapObject.GetFullScale();
                    distance += (Vector2)(sca - prevSca) * playerDistance;
                }
                // idk why this rotates the player around the area next to the object instead of around it
                if (useRotation)
                {
                    var rot = beatmapObject.GetFullRotation(true);
                    //var rotationDistance = RTMath.Distance(rot.z, prevRot.z);

                    var amount = (Vector2)(RTMath.Rotate(distance + rb.position + (Vector2)pos, rot.z) - RTMath.Rotate(distance + rb.position + (Vector2)pos, prevRot.z));
                    //var a = (Vector2)RTMath.Rotate(rb.position + (Vector2)pos, rot.z);
                    //var b = (Vector2)RTMath.Rotate(rb.position + (Vector2)pos, prevRot.z);
                    //var amount = new Vector2(RTMath.Distance(a.x, b.x), RTMath.Distance(a.y, b.y));
                    //if (Input.GetKeyDown(KeyCode.U))
                    //    CoreHelper.Log($"Rot: {rot} Prev Rot: {prevRot} A: {a} B: {b} Amount: {amount}");
                    distance = amount;
                }

                rb.position += distance;
            });
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.BoolGenerator(modifier, reference, "Use Position", 0);
            modifierCard.BoolGenerator(modifier, reference, "Use Scale", 1);
            modifierCard.BoolGenerator(modifier, reference, "Use Rotation", 2);
        }

        #endregion
    }
}
