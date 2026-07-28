using System.Collections.Generic;

using UnityEngine.UI;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetEnum : ModifierVariableBase
    {
        #region Constructors

        public GetEnum() => SetupModifier("ENUM_VAR", "0", "False");

        #endregion

        #region Values

        public override string Name => "getEnum";

        public override CategoryType Category => CategoryType.Main;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop)
        {
            var index = (modifier.GetInt(1, 0, modifierLoop.variables) * 2) + 4;
            return modifier.values.Count > index ? FormatStringVariables(modifier.GetValue(index, modifierLoop.variables), modifierLoop.variables) : null;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            var options = new List<string>();
            for (int i = 3; i < modifier.values.Count; i += 2)
                options.Add(modifier.values[i]);

            if (!options.IsEmpty())
                modifierCard.DropdownGenerator(modifier, reference, "Value", 1, options);

            var collapseValue = modifier.GetBool(2, false);
            modifierCard.BoolGenerator("Collapse Enum Editor", collapseValue, _val =>
            {
                modifier.SetValue(2, _val.ToString());
                var value = modifierCard.DialogScrollbarValue;
                modifierCard.RenderModifier(reference);
                CoroutineHelper.PerformAtNextFrame(() => modifierCard.DialogScrollbarValue = value);
            });

            if (collapseValue)
                return;

            int a = 0;
            for (int i = 3; i < modifier.values.Count; i += 2)
            {
                int groupIndex = i;
                var label = modifierCard.LabelGenerator($"- Enum Value {a + 1}");

                modifierCard.DeleteGenerator(modifier, reference, label.transform, () =>
                {
                    for (int j = 0; j < 2; j++)
                        modifier.values.RemoveAt(groupIndex);
                });

                var groupName = modifierCard.StringGenerator(modifier, reference, "Name", i, _val =>
                {
                    var value = modifierCard.DialogScrollbarValue;
                    modifierCard.RenderModifier(reference);
                    CoroutineHelper.PerformAtNextFrame(() => modifierCard.DialogScrollbarValue = value);
                });
                var groupNameField = groupName.transform.Find("Input").GetComponent<InputField>();
                EditorContextMenu.AddContextMenu(groupNameField.gameObject, EditorContextMenu.GetNameFunctions(groupNameField));
                var value = modifierCard.StringGenerator(modifier, reference, "Value", i + 1);
                var valueField = value.transform.Find("Input").GetComponent<InputField>();
                EditorContextMenu.AddContextMenu(valueField.gameObject, EditorContextMenu.GetNameFunctions(valueField));
                a++;
            }

            modifierCard.AddGenerator(modifier, reference, "Add Enum Value", () =>
            {
                modifier.values.Add($"Enum {a}");
                modifier.values.Add(a.ToString());
            });
        }

        #endregion
    }
}
