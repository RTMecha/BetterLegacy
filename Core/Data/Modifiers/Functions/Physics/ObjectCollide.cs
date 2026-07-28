using System.Linq;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ObjectCollide : ModifierTriggerBase
    {
        #region Constructors

        public ObjectCollide()
        {
            SetupModifier("Object Group");
            IsGroup = true;
        }

        #endregion

        #region Values

        public override string Name => "objectCollide";

        public override CategoryType Category => CategoryType.Physics;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return false;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.collider)
                return false;

            if (runtimeObject.visualObject is SolidObject solidObject && !solidObject.forceCollisionEnabled)
            {
                solidObject.forceCollisionEnabled = true;
                solidObject.UpdateCollider();
            }

            var list = GameData.Current.FindObjectsWithTag(modifier, beatmapObject, modifier.GetValue(0)).FindAll(x => x.runtimeObject && x.runtimeObject.visualObject && x.runtimeObject.visualObject.collider);
            return !list.IsEmpty() && list.Any(x => x.runtimeObject.visualObject.collider.IsTouching(runtimeObject.visualObject.collider));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.PrefabGroupOnly(modifier, reference);
            modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 0);
        }

        #endregion
    }
}
