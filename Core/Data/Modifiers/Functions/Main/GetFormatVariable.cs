using UnityEngine.UI;

using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetFormatVariable : ModifierVariableBase
    {
        #region Constructors

        public GetFormatVariable() => SetupModifier("STRINGFORMAT_VAR", "text is {0}!");

        #endregion

        #region Values

        public override string Name => "getFormatVariable";

        public override CategoryType Category => CategoryType.Main;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop)
        {
            try
            {
                var args = new object[modifier.values.Count - 2];
                for (int i = 2; i < modifier.values.Count; i++)
                    args[i - 2] = FormatStringVariables(modifier.GetValue(i), modifierLoop.variables);

                return string.Format(modifier.GetValue(1, modifierLoop.variables), args);
            }
            catch
            {
                return null;
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0);
            modifierCard.StringGenerator(modifier, reference, "Format Text", 1);

            int a = 0;
            for (int i = 2; i < modifier.values.Count; i++)
            {
                int groupIndex = i;
                var label = modifierCard.LabelGenerator($"- Text Arg {a + 1}");

                modifierCard.DeleteGenerator(modifier, reference, label.transform, () => modifier.values.RemoveAt(groupIndex));

                var groupName = modifierCard.StringGenerator(modifier, reference, "Variable Name", i, renderVariables: false);
                var inputField = groupName.transform.Find("Input").GetComponent<InputField>();
                EditorContextMenu.AddContextMenu(inputField.gameObject, EditorContextMenu.GetNameFunctions(inputField));
                a++;
            }

            modifierCard.AddGenerator(modifier, reference, "Add Text Value", () =>
            {
                modifier.values.Add($"Text");
            });
        }

        #endregion
    }
}
