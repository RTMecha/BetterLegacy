using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class MouseOver : ModifierTriggerBase
    {
        #region Constructors

        public MouseOver() => SetupModifier();

        #endregion

        #region Values

        public override string Name => "mouseOver";

        public override ModifierCategoryType Category => ModifierCategoryType.Controls;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is BeatmapObject beatmapObject && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject && beatmapObject.runtimeObject.visualObject.gameObject)
            {
                if (!beatmapObject.detector)
                {
                    var gameObject = beatmapObject.runtimeObject.visualObject.gameObject;
                    var op = gameObject.GetOrAddComponent<Detector>();
                    op.beatmapObject = beatmapObject;
                    beatmapObject.detector = op;
                }

                return beatmapObject.detector && beatmapObject.detector.hovered;
            }
            return false;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable) { }

        #endregion
    }
}
