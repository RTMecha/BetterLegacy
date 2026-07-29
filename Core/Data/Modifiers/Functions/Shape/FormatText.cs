using BetterLegacy.Configs;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class FormatText : ModifierActionBase
    {
        #region Constructors

        public FormatText() => SetupModifier();

        #endregion

        #region Values

        public override string Name => "formatText";

        public override ModifierCategoryType Category => ModifierCategoryType.Shape;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (!CoreConfig.Instance.AllowCustomTextFormatting.Value && modifierLoop.reference is BeatmapObject beatmapObject && beatmapObject.ShapeType == ShapeType.Text &&
                beatmapObject.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject is TextObject textObject)
                textObject.SetText(RTString.FormatText(beatmapObject, textObject.text, modifierLoop.variables));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable) { }

        #endregion
    }
}
