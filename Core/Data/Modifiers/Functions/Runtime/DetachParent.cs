using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class DetachParent : ModifierActionBase
    {
        #region Constructors

        public DetachParent(bool isGroup)
        {
            this.isGroup = isGroup;
            Name = "detachParent";
            if (isGroup)
                Name += "Other";
            SetupModifier(false, "True");
            if (isGroup)
                Modifier.values.Add("Object Group");
            IsGroup = isGroup;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Runtime;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible.WithPrefabObject(true);

        readonly bool isGroup;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifier.constant)
                return;

            if (isGroup)
            {
                if (modifierLoop.reference is not IPrefabable prefabable)
                    return;

                var parentables = GameData.Current.FindParentables(modifier, prefabable, modifier.GetValue(1, modifierLoop.variables));
                var detach = modifier.GetBool(0, true, modifierLoop.variables);

                foreach (var other in parentables)
                {
                    other.ParentDetatched = detach;

                    if (other is not PrefabObject otherPrefabObject || !otherPrefabObject.runtimeObject)
                        continue;

                    foreach (var beatmapObject in otherPrefabObject.runtimeObject.Spawner.BeatmapObjects)
                        if (beatmapObject.fromPrefabBase)
                            beatmapObject.detatched = otherPrefabObject.detatched;
                }
                return;
            }

            if (modifierLoop.reference is not IParentable parentable)
                return;

            parentable.ParentDetatched = modifier.GetBool(0, true, modifierLoop.variables);

            if (modifierLoop.reference is not PrefabObject prefabObject || !prefabObject.runtimeObject || prefabObject.parentSelf)
                return;

            foreach (var beatmapObject in prefabObject.runtimeObject.Spawner.BeatmapObjects)
                if (beatmapObject.fromPrefabBase)
                    beatmapObject.detatched = prefabObject.detatched;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (isGroup)
            {
                modifierCard.PrefabGroupOnly(modifier, reference);
                modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 1);
            }

            modifierCard.BoolGenerator(modifier, reference, "Detach", 0, false);
        }

        #endregion
    }
}
