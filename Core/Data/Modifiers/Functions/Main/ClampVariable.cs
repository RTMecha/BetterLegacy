using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ClampVariable : ModifierActionBase
    {
        #region Constructors

        public ClampVariable(bool isGroup)
        {
            this.isGroup = isGroup;
            Name = "clampVariable";
            if (isGroup)
                Name += "Other";
            SetupModifier(isGroup ? "Object Group" : string.Empty, "0", "1");
            IsGroup = isGroup;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Main;

        public override ModifierCompatibility Compatibility => isGroup ? ModifierCompatibility.LevelControlCompatible : base.Compatibility;

        readonly bool isGroup;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (isGroup)
            {
                if (modifierLoop.reference is not IPrefabable prefabable)
                    return;

                var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables));

                var min = modifier.GetInt(1, 0, modifierLoop.variables);
                var max = modifier.GetInt(2, 0, modifierLoop.variables);

                if (!list.IsEmpty())
                    foreach (var bm in list)
                        bm.IntVariable = RTMath.Clamp(bm.integerVariable, min, max);
            }

            if (modifierLoop.reference is IModifyable modifyable)
                modifyable.IntVariable = RTMath.Clamp(modifyable.IntVariable, modifier.GetInt(1, 0, modifierLoop.variables), modifier.GetInt(2, 0, modifierLoop.variables));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (isGroup)
            {
                modifierCard.PrefabGroupOnly(modifier, reference);
                modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 0);
            }

            modifierCard.IntegerGenerator(modifier, reference, "Minimum", 1, 0);
            modifierCard.IntegerGenerator(modifier, reference, "Maximum", 2, 0);
        }

        #endregion
    }
}
