using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class BulletCollide : ModifierTriggerBase
    {
        #region Constructors

        public BulletCollide() => SetupModifier();

        #endregion

        #region Values

        public override string Name => "bulletCollide";

        public override ModifierCategoryType Category => ModifierCategoryType.Physics;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return false;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return false;

            if (runtimeObject.visualObject is SolidObject solidObject && !solidObject.forceCollisionEnabled)
            {
                solidObject.forceCollisionEnabled = true;
                solidObject.UpdateCollider();
            }

            if (!beatmapObject.runtimeObject.visualObject.detector)
                runtimeObject.visualObject.gameObject.GetOrAddComponent<Detector>().SetObject(beatmapObject);
            return beatmapObject.runtimeObject.visualObject.detector && beatmapObject.runtimeObject.visualObject.detector.bulletOver;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable) { }

        #endregion
    }
}
