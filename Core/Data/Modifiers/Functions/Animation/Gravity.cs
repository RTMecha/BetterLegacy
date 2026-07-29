using System.Collections.Generic;

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

        public override ModifierCategoryType Category => ModifierCategoryType.Animation;

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

                var tag = modifier.GetValue(0, modifierLoop.variables);
                var cache = modifier.GetResultOrDefault(() => new Cache(tag, GameData.Current.FindTransformablesWithTag(modifier, prefabable, tag)));
                if (cache.tag != tag)
                {
                    cache.tag = tag;
                    cache.transformables = GameData.Current.FindTransformablesWithTag(modifier, prefabable, tag);
                }
                cache.pos = RTMath.Lerp(Vector2.zero, new Vector2(gravityX, gravityY), (RTMath.Recursive(Time.time - cache.time, curve)) * (time * CoreHelper.TimeFrame));

                foreach (var transformable in cache.transformables)
                    transformable.PositionOffset = RTMath.Rotate(cache.pos, -transformable.GetFullRotation(false).z);
            }
            else
            {
                if (modifierLoop.reference is not ITransformable transformable)
                    return;

                var cache = modifier.GetResultOrDefault(() => new Cache());
                cache.pos = RTMath.Lerp(Vector2.zero, new Vector2(gravityX, gravityY), (RTMath.Recursive(Time.time - cache.time, curve)) * (time * CoreHelper.TimeFrame));
                transformable.PositionOffset = RTMath.Rotate(cache.pos, -transformable.GetFullRotation(false).z);
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

        #region Sub Classes

        public class Cache
        {
            public Cache() { }

            public Cache(string tag, List<ITransformable> transformables) => this.transformables = transformables;

            public Vector2 pos = Vector2.zero;
            public float time = Time.time;
            public string tag;
            public List<ITransformable> transformables;
        }

        #endregion
    }
}
