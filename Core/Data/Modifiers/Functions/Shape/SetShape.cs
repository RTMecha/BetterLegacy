using System.Collections.Generic;
using System.Linq;

using UnityEngine.UI;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetShape : ModifierActionBase
    {
        #region Constructors

        public SetShape() => SetupModifier(false, "0", "0");

        #endregion

        #region Values

        public override string Name => "setShape";

        public override CategoryType Category => CategoryType.Shape;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible.WithBackgroundObject();

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not IShapeable shapeable)
                return;

            shapeable.SetCustomShape(modifier.GetInt(0, 0, modifierLoop.variables), modifier.GetInt(1, 0, modifierLoop.variables));
            if (shapeable is BeatmapObject beatmapObject)
                modifierLoop.reference.GetParentRuntime()?.UpdateObject(beatmapObject, ObjectContext.SHAPE);
            else if (shapeable is BackgroundObject backgroundObject)
                backgroundObject.runtimeObject?.UpdateShape(backgroundObject.Shape, backgroundObject.ShapeOption, backgroundObject.flat);
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            var isBG = modifyable.ReferenceType == ModifierReferenceType.BackgroundObject;

            modifierCard.DropdownGenerator(modifier, reference, "Shape", 0, ShapeManager.inst.Shapes2D.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList(), new List<bool>
            {
                false, // square
                false, // circle
                false, // triangle
                false, // arrow
                !isBG, // text
                false, // hexagon
                !isBG, // image
                false, // pentagon
                false, // misc
                true, // polygon
            },
            _val =>
            {
                var shapeType = (ShapeType)_val;
                if (isBG && (shapeType == ShapeType.Text || shapeType == ShapeType.Image) || shapeType == ShapeType.Polygon)
                    modifier.SetValue(0, "0");
                else
                    modifier.SetValue(0, _val.ToString());

                modifier.SetValue(1, "0");
                modifierCard.RenderModifier(reference);
                modifierCard.Update(modifier, reference);
            });

            var shape = modifier.GetInt(0, 0);
            modifierCard.DropdownGenerator(modifier, reference, "Shape Option", 1, ShapeManager.inst.Shapes2D[shape].shapes.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());
        }

        #endregion
    }
}
