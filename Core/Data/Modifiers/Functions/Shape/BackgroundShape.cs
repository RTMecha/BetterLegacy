using UnityEngine;

using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class BackgroundShape : ModifierActionBase
    {
        #region Constructors

        public BackgroundShape() => SetupModifier(false);

        #endregion

        #region Values

        public override string Name => "backgroundShape";

        public override ModifierCategoryType Category => ModifierCategoryType.Shape;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (modifier.HasResult() || beatmapObject.IsSpecialShape || !runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return;

            if (ShapeManager.inst.Shapes3D.TryGetAt(beatmapObject.Shape, out ShapeGroup shapeGroup) && shapeGroup.TryGetShape(beatmapObject.ShapeOption, out Shape shape))
            {
                runtimeObject.visualObject.gameObject.GetComponent<MeshFilter>().mesh = shape.mesh;
                modifier.Result = "frick";
                runtimeObject.visualObject.gameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
            }
        }

        public override bool IsCompatible(IModifyable modifyable) => modifyable is IShapeable shapeable && !shapeable.IsSpecialShape;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable) { }

        #endregion
    }
}
