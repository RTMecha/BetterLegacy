using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetCollision : ModifierActionBase
    {
        #region Constructors

        public SetCollision(bool isGroup)
        {
            this.isGroup = isGroup;
            Name = "setCollision";
            if (isGroup)
                Name += "Other";
            SetupModifier("False");
            if (isGroup)
                Modifier.values.Add("Object Group");
            IsGroup = isGroup;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Physics;

        public override ModifierCompatibility Compatibility => isGroup ? ModifierCompatibility.FullBeatmapCompatible : ModifierCompatibility.BeatmapObjectCompatible;

        readonly bool isGroup;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var colliderEnabled = modifier.GetBool(0, false, modifierLoop.variables);

            if (isGroup)
            {
                if (modifierLoop.reference is not IPrefabable prefabable)
                    return;

                var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, modifierLoop.variables));

                foreach (var other in list)
                {
                    if (other.runtimeObject is RTBeatmapObject otherRuntimeObject && otherRuntimeObject.visualObject && otherRuntimeObject.visualObject.collider)
                        otherRuntimeObject.visualObject.colliderEnabled = colliderEnabled;
                }
                return;
            }

            if (modifierLoop.reference is BeatmapObject beatmapObject && beatmapObject.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.collider)
                runtimeObject.visualObject.colliderEnabled = colliderEnabled;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (isGroup)
            {
                modifierCard.PrefabGroupOnly(modifier, reference);
                modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 1);
            }
            modifierCard.BoolGenerator(modifier, reference, "On", 0, false);
        }

        #endregion
    }
}
