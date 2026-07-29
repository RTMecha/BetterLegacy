using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    // this modifier is a bit weird but it was requested by someone
    public class FollowMousePosition : ModifierActionBase
    {
        #region Constructors

        public FollowMousePosition() => SetupModifier("1", "1");

        #endregion

        #region Values

        public override string Name => "followMousePosition";

        public override ModifierCategoryType Category => ModifierCategoryType.Controls;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.FullBeatmapCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not ITransformable transformable)
                return;

            Vector2 mousePosition = Input.mousePosition;
            mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);

            float p = Time.deltaTime * 60f;
            float po = 1f - Mathf.Pow(1f - Mathf.Clamp(modifier.GetFloat(0, 1f, modifierLoop.variables), 0.001f, 1f), p);
            float ro = 1f - Mathf.Pow(1f - Mathf.Clamp(modifier.GetFloat(1, 1f, modifierLoop.variables), 0.001f, 1f), p);

            if (modifier.Result == null)
                modifier.Result = Vector2.zero;

            var dragPos = (Vector2)modifier.Result;

            var target = new Vector2(mousePosition.x, mousePosition.y);

            transformable.RotationOffset = new Vector3(0f, 0f, (target.x - dragPos.x) * ro);

            dragPos += (target - dragPos) * po;

            modifier.Result = dragPos;

            transformable.PositionOffset = dragPos;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.SingleGenerator(modifier, reference, "Position Focus", 0, 1f);
            modifierCard.SingleGenerator(modifier, reference, "Rotation Delay", 1, 1f);
        }

        #endregion
    }
}
