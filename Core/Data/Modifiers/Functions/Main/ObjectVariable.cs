using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ObjectVariable : ModifierActionBase
    {
        #region Constructors

        public ObjectVariable(Operation operation, bool isGroup, bool isMath)
        {
            this.operation = operation;
            this.isGroup = isGroup;
            this.isMath = isMath;
            Name = operation.ToString().ToLower() + "ObjectVariable";
            if (isMath)
                Name += "Math";
            if (isGroup)
                Name += "Other";
            SetupModifier(false, "1");
            if (isMath)
                Modifier.values.Add("5");
            if (isGroup)
                Modifier.values.Add("Object Group");
            IsGroup = isGroup;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Main;

        public override ModifierCompatibility Compatibility => isGroup ? ModifierCompatibility.LevelControlCompatible : base.Compatibility;

        readonly Operation operation;

        readonly bool isGroup;

        readonly bool isMath;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            MathOperation operation = MathOperation.Addition;
            Dictionary<string, float> numberVariables = null;
            int value = 0;
            if (isMath)
            {
                if (modifierLoop.reference is not IEvaluatable evaluatable)
                    return;

                numberVariables = evaluatable.GetObjectVariables();
                ModifiersHelper.SetVariables(modifierLoop.variables, numberVariables);
                value = (int)RTMath.Parse(modifier.GetValue(0, modifierLoop.variables), RTLevel.Current?.evaluationContext, numberVariables);
                operation = Parser.TryParse(modifier.GetValue(1, modifierLoop.variables), true, MathOperation.Set);
            }
            else
                value = modifier.GetInt(0, 0, modifierLoop.variables);

            if (isGroup)
            {
                if (modifierLoop.reference is not IPrefabable prefabable)
                    return;

                var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, FormatStringVariables(modifier.GetValue(isMath ? 2 : 1, modifierLoop.variables), modifierLoop.variables));
                if (list.IsEmpty())
                    return;

                foreach (var beatmapObject in list)
                {
                    if (isMath)
                    {
                        beatmapObject.SetOtherObjectVariables(numberVariables);
                        value = (int)RTMath.Parse(modifier.GetValue(0, modifierLoop.variables), RTLevel.Current?.evaluationContext, numberVariables);
                    }
                    Apply(beatmapObject, value, modifier.constant, operation);
                }
                return;
            }
            if (modifierLoop.reference != null)
                Apply(modifierLoop.reference, value, modifier.constant, operation);
        }

        void Apply(IModifierReference reference, int value, bool constant, MathOperation operation) => reference.IntVariable = isMath ? RTMath.ReturnOperation(reference.IntVariable, value, operation) : this.operation switch
        {
            Operation.Add => constant ? Mathf.FloorToInt(reference.IntVariable + (value * Time.deltaTime)) : reference.IntVariable + value,
            Operation.Sub => constant ? Mathf.FloorToInt(reference.IntVariable - (value * Time.deltaTime)) : reference.IntVariable - value,
            _ => value,
        };

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (isGroup)
            {
                modifierCard.PrefabGroupOnly(modifier, reference);
                modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", isMath ? 2 : 1);
            }
            modifierCard.IntegerGenerator(modifier, reference, "Value", 0, 0);
            if (isMath)
                modifierCard.DropdownGenerator(modifier, reference, "Operation", 1, CoreHelper.ToOptionData<MathOperation>());
        }

        #endregion

        #region Sub Classes

        public enum Operation
        {
            Add,
            Sub,
            Set,
        }

        #endregion
    }
}
