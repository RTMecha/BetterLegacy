using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetPrefabTime : ModifierActionBase
    {
        #region Constructors

        public SetPrefabTime() => SetupModifier("0", "True");

        #endregion

        #region Values

        public override string Name => "setPrefabTime";

        public override ModifierCategoryType Category => ModifierCategoryType.Prefab;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.PrefabObjectCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not PrefabObject prefabObject || !prefabObject.runtimeObject)
                return;

            prefabObject.runtimeObject.CustomTime = modifier.GetFloat(0, 0f, modifierLoop.variables);
            prefabObject.runtimeObject.UseCustomTime = modifier.GetBool(1, false, modifierLoop.variables);
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.SingleGenerator(modifier, reference, "Time", 0);
            modifierCard.BoolGenerator(modifier, reference, "Use Custom Time", 1);
        }

        #endregion
    }
}
