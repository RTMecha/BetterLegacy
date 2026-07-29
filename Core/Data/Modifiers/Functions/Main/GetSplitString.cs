using UnityEngine.UI;

using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetSplitString : ModifierActionBase
    {
        #region Constructors

        public GetSplitString(Type type)
        {
            this.type = type;
            Name = "getSplitString";
            if (type != Type.Array)
                Name += type.ToString();
            SetupModifier(type switch
            {
                Type.Array => new string[] { "split this text", " " },
                Type.At => new string[] { "split this text", " ", "STRING_INDEX_VAR", "0" },
                Type.Count => new string[] { "split this text", " ", "STRING_COUNT_VAR" },
                _ => new string[] { }
            });
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Main;

        readonly Type type;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var str = FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables);
            var ch = modifier.GetValue(1, modifierLoop.variables);

            if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(ch))
                return;

            var split = str.Split(ch[0]);
            if (type != Type.Array)
            {
                var s = FormatStringVariables(modifier.GetValue(2), modifierLoop.variables);
                if (!string.IsNullOrEmpty(s))
                    modifierLoop.variables[s] = type == Type.At ? split.GetAt(modifier.GetInt(3, 0, modifierLoop.variables)) : split.Length.ToString();
                return;
            }
            for (int i = 0; i < split.Length; i++)
            {
                var index = i + 2;
                if (modifier.values.InRange(index))
                {
                    var s = FormatStringVariables(modifier.GetValue(index), modifierLoop.variables);
                    if (!string.IsNullOrEmpty(s))
                        modifierLoop.variables[s] = split[i];
                }
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Text", 0);
            modifierCard.StringGenerator(modifier, reference, "Character", 1);

            if (type != Type.Array)
            {
                modifierCard.StringGenerator(modifier, reference, "Variable Name", 2, renderVariables: false);
                if (type == Type.At)
                    modifierCard.IntegerGenerator(modifier, reference, "Index", 3);
                return;
            }

            int a = 0;
            for (int i = 2; i < modifier.values.Count; i++)
            {
                int groupIndex = i;
                var label = modifierCard.LabelGenerator($"- Variable {a + 1}");

                modifierCard.DeleteGenerator(modifier, reference, label.transform, () => modifier.values.RemoveAt(groupIndex));

                var groupName = modifierCard.StringGenerator(modifier, reference, "Variable Name", i, renderVariables: false);
                var inputField = groupName.transform.Find("Input").GetComponent<InputField>();
                EditorContextMenu.AddContextMenu(inputField.gameObject, EditorContextMenu.GetNameFunctions(inputField));
                a++;
            }

            modifierCard.AddGenerator(modifier, reference, "Add String Value", () =>
            {
                modifier.values.Add($"SPLITSTRING_VAR_{a}");
            });
        }

        #endregion

        #region Sub Classes

        public enum Type
        {
            Array,
            At,
            Count,
        }

        #endregion
    }
}
