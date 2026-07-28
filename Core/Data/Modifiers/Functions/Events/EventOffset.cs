using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class EventOffset : ModifierActionBase
    {
        #region Constructors

        public EventOffset(Type type)
        {
            this.type = type;
            Name = "eventOffset";
            if (type != Type.Normal)
                Name += type.ToString();
            SetupModifier("1", "0", "0", "0");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Events;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        readonly Type type;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (!RTLevel.Current.eventEngine || RTLevel.Current.eventEngine.offsets == null)
                return;

            var eventType = modifier.GetInt(1, 0, modifierLoop.variables);
            var valueIndex = modifier.GetInt(2, 0, modifierLoop.variables);
            if (type == Type.Math)
            {
                if (modifierLoop.reference is IEvaluatable evaluatable)
                {
                    var numberVariables = evaluatable.GetObjectVariables();
                    ModifiersHelper.SetVariables(modifierLoop.variables, numberVariables);
                    RTLevel.Current.eventEngine.SetOffset(eventType, valueIndex, RTMath.Parse(modifier.GetValue(0, modifierLoop.variables), RTLevel.Current?.evaluationContext, numberVariables, evaluatable.GetObjectFunctions()));
                    RTLevel.Current.eventEngine.SetOffsetOperation(eventType, valueIndex, Parser.TryParse(modifier.GetValue(3, modifierLoop.variables), true, MathOperation.Addition));
                }
            }
            else
                RTLevel.Current.eventEngine.SetOffset(eventType, valueIndex, type switch
                {
                    Type.Variable => modifierLoop.reference is IModifyable modifyable ? modifyable.IntVariable : 0f,
                    _ => modifier.GetFloat(0, 1f, modifierLoop.variables),
                });
            RTLevel.Current.eventEngine.SetOffsetOperation(eventType, valueIndex, Parser.TryParse(modifier.GetValue(3, modifierLoop.variables), true, MathOperation.Addition));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.DropdownGenerator(modifier, reference, "Event Type", 1, CoreHelper.StringToOptionData(EventLibrary.displayNames), _val =>
            {
                modifier.SetValue(1, _val.ToString());
                modifier.SetValue(2, "0");
                modifierCard.RenderModifier(reference);
                modifierCard.Update(modifier, reference);
            });
            modifierCard.DropdownGenerator(modifier, reference, "Value Index", 2, CoreHelper.StringToOptionData(EventLibrary.valueNames[RTMath.Clamp(modifier.GetInt(1, 0), 0, EventLibrary.valueNames.Length - 1)]));
            if (type == Type.Math)
                modifierCard.StringGenerator(modifier, reference, "Evaluation", 0);
            else
                modifierCard.SingleGenerator(modifier, reference, "Offset Value", 0, 0f);
            modifierCard.DropdownGenerator(modifier, reference, "Operation", 3, CoreHelper.ToOptionData<MathOperation>());
        }

        #endregion

        #region Sub Classes

        public enum Type
        {
            Normal,
            Variable,
            Math,
        }

        #endregion
    }
}
