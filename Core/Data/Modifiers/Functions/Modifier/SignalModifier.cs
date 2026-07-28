using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SignalModifier : ModifierActionBase
    {
        #region Constructors

        public SignalModifier()
        {
            SetupModifier("0.5", "Object Group");
            IsGroup = true;
        }

        #endregion

        #region Values

        public override string Name => "signalModifier";

        public override CategoryType Category => CategoryType.Modifier;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, modifierLoop.variables));
            var delay = modifier.GetFloat(0, 0f, modifierLoop.variables);

            foreach (var bm in list)
                ModifiersHelper.SignalModifier(bm, delay);
        }

        public override void Inactive(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not IPrefabable prefabable)
                return;

            var modifyables = GameData.Current.FindModifyables(modifier, prefabable, modifier.GetValue(1, modifierLoop.variables));

            foreach (var modifyable in modifyables)
            {
                if (!modifyable.Modifiers.IsEmpty() && modifyable.Modifiers.TryFind(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger, out Modifier m))
                    m.Result = default;
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.PrefabGroupOnly(modifier, reference);
            modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 1);
            modifierCard.SingleGenerator(modifier, reference, "Delay", 0, 0f);
        }

        #endregion
    }
}
