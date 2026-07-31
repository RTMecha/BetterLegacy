using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetPolygonShape : ModifierActionBase
    {
        #region Constructors

        public SetPolygonShape(bool isGroup)
        {
            this.isGroup = isGroup;
            Name = "setPolygonShape";
            if (isGroup)
                Name += "Other";
            SetupModifier("0.5", "3", "0", "1", "3", "0", "0", "1", "1", "0", "0", "1");
            if (isGroup)
                Modifier.values.Insert(0, "Object Group");
            IsGroup = IsGroup;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Shape;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        readonly bool isGroup;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (isGroup)
            {
                if (modifierLoop.reference is not IPrefabable prefabable)
                    return;

                var radius = RTMath.Clamp(modifier.GetFloat(1, 0.5f, modifierLoop.variables), 0.1f, 10f);
                var sides = RTMath.Clamp(modifier.GetInt(2, 3, modifierLoop.variables), 3, 32);
                var roundness = RTMath.Clamp(modifier.GetFloat(3, 0f, modifierLoop.variables), 0f, 1f);
                var thickness = RTMath.Clamp(modifier.GetFloat(4, 1f, modifierLoop.variables), 0f, 1f);
                var slices = RTMath.Clamp(modifier.GetInt(5, 3, modifierLoop.variables), 0, sides);
                var thicknessOffset = new Vector2(modifier.GetFloat(6, 0f, modifierLoop.variables), modifier.GetFloat(7, 0f, modifierLoop.variables));
                var thicknessScale = new Vector2(modifier.GetFloat(8, 1f, modifierLoop.variables), modifier.GetFloat(9, 1f, modifierLoop.variables));
                var thicknessRotation = modifier.GetFloat(11, 0f, modifierLoop.variables);
                var rotation = modifier.GetFloat(10, 0f, modifierLoop.variables);
                var alternate = modifier.GetFloat(12, 1f, modifierLoop.variables);

                var meshParams = new VGShapes.MeshParams
                {
                    VertexCount = sides,
                    cornerRoundness = roundness,
                    thickness = thickness,
                    SliceCount = slices,
                    thicknessOffset = thicknessOffset,
                    thicknessScale = thicknessScale,
                    thicknessRotation = thicknessRotation,
                    rotation = rotation,
                    alternate = alternate,
                };

                if (modifier.TryGetResult(out VGShapes.MeshParams cache) && meshParams.Equals(cache))
                    return;

                var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, modifierLoop.variables));

                for (int i = 0; i < list.Count; i++)
                {
                    var beatmapObject = list[i];
                    if (beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject is PolygonObject polygonObject)
                        polygonObject.UpdatePolygon(radius, sides, roundness, thickness, slices, thicknessOffset, thicknessScale, rotation, thicknessRotation, alternate);
                }

                modifier.Result = meshParams;
            }
            else
            {
                if (modifierLoop.reference is not BeatmapObject beatmapObject || !beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not PolygonObject polygonObject)
                    return;

                var radius = RTMath.Clamp(modifier.GetFloat(0, 0.5f, modifierLoop.variables), 0.1f, 10f);
                var sides = RTMath.Clamp(modifier.GetInt(1, 3, modifierLoop.variables), 3, 32);
                var roundness = RTMath.Clamp(modifier.GetFloat(2, 0f, modifierLoop.variables), 0f, 1f);
                var thickness = RTMath.Clamp(modifier.GetFloat(3, 1f, modifierLoop.variables), 0f, 1f);
                var slices = RTMath.Clamp(modifier.GetInt(4, 3, modifierLoop.variables), 0, sides);
                var thicknessOffset = new Vector2(modifier.GetFloat(5, 0f, modifierLoop.variables), modifier.GetFloat(6, 0f, modifierLoop.variables));
                var thicknessScale = new Vector2(modifier.GetFloat(7, 1f, modifierLoop.variables), modifier.GetFloat(8, 1f, modifierLoop.variables));
                var thicknessRotation = modifier.GetFloat(10, 0f, modifierLoop.variables);
                var rotation = modifier.GetFloat(9, 0f, modifierLoop.variables);
                var alternate = modifier.GetFloat(11, 1f, modifierLoop.variables);

                var meshParams = new VGShapes.MeshParams
                {
                    radius = radius,
                    VertexCount = sides,
                    cornerRoundness = roundness,
                    thickness = thickness,
                    SliceCount = slices,
                    thicknessOffset = thicknessOffset,
                    thicknessScale = thicknessScale,
                    thicknessRotation = thicknessRotation,
                    rotation = rotation,
                    alternate = alternate,
                };

                if (modifier.TryGetResult(out VGShapes.MeshParams cache) && meshParams.Equals(cache))
                    return;

                polygonObject.UpdatePolygon(radius, sides, roundness, thickness, slices, thicknessOffset, thicknessScale, rotation, thicknessRotation, alternate);
                modifier.Result = meshParams;
            }
        }

        public override bool IsCompatible(IModifyable modifyable) => isGroup || modifyable is IShapeable shapeable && shapeable.ShapeType == ShapeType.Polygon;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            var index = 0;
            if (isGroup)
            {
                modifierCard.PrefabGroupOnly(modifier, reference);
                modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 0);
                index++;
            }

            modifierCard.SingleGenerator(modifier, reference, "Radius", 0 + index);
            modifierCard.IntegerGenerator(modifier, reference, "Sides", 1 + index, min: 3, max: 32);
            modifierCard.SingleGenerator(modifier, reference, "Roundness", 2 + index, max: 1f);
            modifierCard.SingleGenerator(modifier, reference, "Thickness", 3 + index, max: 1f);
            modifierCard.SingleGenerator(modifier, reference, "Thick Offset X", 5 + index);
            modifierCard.SingleGenerator(modifier, reference, "Thick Offset Y", 6 + index);
            modifierCard.SingleGenerator(modifier, reference, "Thick Scale X", 7 + index);
            modifierCard.SingleGenerator(modifier, reference, "Thick Scale Y", 8 + index);
            modifierCard.SingleGenerator(modifier, reference, "Thick Angle", 10 + index);
            modifierCard.IntegerGenerator(modifier, reference, "Slices", 4 + index, max: 32);
            modifierCard.SingleGenerator(modifier, reference, "Angle", 9 + index);
            modifierCard.SingleGenerator(modifier, reference, "Alternate", 11 + index, 1f);
        }

        #endregion
    }
}
