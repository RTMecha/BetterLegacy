using BetterLegacy.Arcade.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class DatamoshFunction : ModifierActionBase
    {
        #region Constructors

        public DatamoshFunction(Function function)
        {
            this.function = function;
            Name = "datamosh" + function.ToString();
            SetupModifier();
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Events;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        readonly Function function;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            switch (function)
            {
                case Function.Glitch: {
                        RTEventManager.inst?.datamosh?.Glitch();
                        break;
                    }
                case Function.Reset: {
                        RTEventManager.inst?.datamosh?.Reset();
                        break;
                    }
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable) { }

        #endregion

        #region Sub Classes

        public enum Function
        {
            Glitch,
            Reset,
        }

        #endregion
    }
}
