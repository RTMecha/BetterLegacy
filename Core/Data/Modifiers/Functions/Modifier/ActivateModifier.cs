using System.Collections.Generic;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ActivateModifier : ModifierActionBase
    {
        #region Constructors

        public ActivateModifier()
        {
            SetupModifier("Object Group", "True", "0", "modifierName");
            IsGroup = true;
        }

        #endregion

        #region Values

        public override string Name => "activateModifier";

        public override ModifierCategoryType Category => ModifierCategoryType.Modifier;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, modifierLoop.variables));

            var doMultiple = modifier.GetBool(1, true, modifierLoop.variables);
            var index = modifier.GetInt(2, -1, modifierLoop.variables);

            // 3 is modifier names
            var modifierNames = new List<string>();
            for (int i = 3; i < modifier.values.Count; i++)
                modifierNames.Add(modifier.GetValue(i, modifierLoop.variables));

            for (int i = 0; i < list.Count; i++)
            {
                if (doMultiple)
                {
                    var modifiers = list[i].modifiers.FindAll(x => x.type == Modifier.Type.Action && modifierNames.Contains(x.Name));

                    for (int j = 0; j < modifiers.Count; j++)
                    {
                        var otherModifier = modifiers[i];
                        otherModifier.action?.Run(otherModifier, new ModifierLoop(list[i], modifierLoop.variables));
                    }
                    continue;
                }

                if (index >= 0 && index < list[i].modifiers.Count)
                {
                    var otherModifier = list[i].modifiers[index];
                    otherModifier.action?.Run(otherModifier, new ModifierLoop(list[i], modifierLoop.variables));
                }
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.PrefabGroupOnly(modifier, reference);
            modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 0);
            modifierCard.BoolGenerator(modifier, reference, "Do Multiple", 1, true);
            modifierCard.IntegerGenerator(modifier, reference, "Singlular Index", 2, 0);

            for (int i = 3; i < modifier.values.Count; i++)
            {
                int groupIndex = i;
                var label = modifierCard.LabelGenerator($"- Name {i + 1}");

                modifierCard.DeleteGenerator(modifier, reference, label.transform, () => modifier.values.RemoveAt(groupIndex));

                modifierCard.StringGenerator(modifier, reference, "Modifier Name", groupIndex);
            }

            modifierCard.AddGenerator(modifier, reference, "Add Modifier Ref", () =>
            {
                modifier.values.Add("modifierName");
            });
        }

        #endregion
    }
}
