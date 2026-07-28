using System.Text.RegularExpressions;

using UnityEngine.UI;

using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetRegex : ModifierActionBase
    {
        #region Constructors

        public GetRegex() => SetupModifier("text is (.*?)!", "text is awesome!");

        #endregion

        #region Values

        public override string Name => "getRegex";

        public override CategoryType Category => CategoryType.Main;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var regex = new Regex(FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables));
            var match = regex.Match(FormatStringVariables(modifier.GetValue(1, modifierLoop.variables), modifierLoop.variables));

            if (!match.Success)
                return;

            for (int i = 0; i < match.Groups.Count; i++)
            {
                var index = i + 2;
                if (modifier.values.InRange(index))
                    modifierLoop.variables[FormatStringVariables(modifier.GetValue(index), modifierLoop.variables)] = match.Groups[i].ToString();
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Regex", 0);
            modifierCard.StringGenerator(modifier, reference, "Text", 1);

            int a = 0;
            for (int i = 2; i < modifier.values.Count; i++)
            {
                int groupIndex = i;
                var label = modifierCard.LabelGenerator(a == 0 ? "- Whole Match Variable" : $"- Match Variable {a}");

                modifierCard.DeleteGenerator(modifier, reference, label.transform, () => modifier.values.RemoveAt(groupIndex));

                var groupName = modifierCard.StringGenerator(modifier, reference, "Variable Name", i, renderVariables: false);
                var inputField = groupName.transform.Find("Input").GetComponent<InputField>();
                EditorContextMenu.AddContextMenu(inputField.gameObject, EditorContextMenu.GetNameFunctions(inputField));
                a++;
            }

            modifierCard.AddGenerator(modifier, reference, "Add Regex Value", () =>
            {
                modifier.values.Add($"REGEX_VAR_{a}");
            });
        }

        #endregion
    }
}
