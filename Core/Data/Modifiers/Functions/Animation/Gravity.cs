using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class Gravity : ModifierActionBase
    {
        #region Constructors

        public Gravity(bool isGroup)
        {
            this.isGroup = isGroup;
            Name = "gravity";
            if (isGroup)
                Name += "Other";
            SetupModifier("0", "0", "-1", "1", "2");
            if (isGroup)
                Modifier.values.Insert(0, "Object Group");
            IsGroup = isGroup;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Animation;

        public override ModifierCompatibility Compatibility => base.Compatibility;

        readonly bool isGroup;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var gravityX = modifier.GetFloat(1, 0f, modifierLoop.variables);
            var gravityY = modifier.GetFloat(2, 0f, modifierLoop.variables);
            var time = modifier.GetFloat(3, 1f, modifierLoop.variables);
            var curve = modifier.GetInt(4, 2, modifierLoop.variables);

            if (isGroup)
            {
                if (modifierLoop.reference is not IPrefabable prefabable)
                    return;

                var transformables = GameData.Current.FindTransformablesWithTag(modifier, prefabable, modifier.GetValue(0, modifierLoop.variables));

                if (modifier.Result == null)
                {
                    modifier.Result = Vector2.zero;
                    modifier.ResultTimer = Time.time;
                }
                else
                    modifier.Result = RTMath.Lerp(Vector2.zero, new Vector2(gravityX, gravityY), (RTMath.Recursive(Time.time - modifier.ResultTimer, curve)) * (time * CoreHelper.TimeFrame));

                var vector = modifier.GetResult<Vector2>();
                foreach (var transformable in transformables)
                    transformable.PositionOffset = RTMath.Rotate(vector, -transformable.GetFullRotation(false).z);
            }
            else
            {
                if (modifierLoop.reference is not ITransformable transformable)
                    return;

                if (modifier.Result == null)
                {
                    modifier.Result = Vector2.zero;
                    modifier.ResultTimer = Time.time;
                }
                else
                    modifier.Result = RTMath.Lerp(Vector2.zero, new Vector2(gravityX, gravityY), (RTMath.Recursive(Time.time - modifier.ResultTimer, curve)) * (time * CoreHelper.TimeFrame));

                var vector = modifier.GetResult<Vector2>();
                transformable.PositionOffset = RTMath.Rotate(vector, -transformable.GetFullRotation(false).z);
            }
        }

        public override void Inactive(Modifier modifier, ModifierLoop modifierLoop) => modifier.Result = default;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (isGroup)
            {
                modifierCard.PrefabGroupOnly(modifier, reference);
                modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 0);
            }

            modifierCard.SingleGenerator(modifier, reference, "X", 1, -1f);
            modifierCard.SingleGenerator(modifier, reference, "Y", 2, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Time Multiply", 3, 1f);
            modifierCard.IntegerGenerator(modifier, reference, "Curve", 4, 2);
        }

        #endregion
    }
}
