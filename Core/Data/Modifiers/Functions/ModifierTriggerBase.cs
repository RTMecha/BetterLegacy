using UnityEngine;

using BetterLegacy.Editor;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public abstract class ModifierTriggerBase : ModifierFunctionBase
    {
        #region Values

        public override Sprite Icon => EditorSprites.QuestionSprite;

        #endregion

        #region Functions

        /// <summary>
        /// Trigger function.
        /// </summary>
        /// <param name="modifier">Modifier reference.</param>
        /// <param name="modifierLoop">The current modifier loop.</param>
        /// <returns>Returns the result of the trigger check.</returns>
        public abstract bool Run(Modifier modifier, ModifierLoop modifierLoop);

        public Modifier CreateModifier(string name, params string[] values) => new Modifier(Modifier.Type.Trigger, name, true, values)
        {
            function = this,
            trigger = this,
            compatibility = Compatibility,
        };

        public Modifier CreateModifier(string name, int version, params string[] values) => new Modifier(Modifier.Type.Trigger, name, true, values)
        {
            function = this,
            trigger = this,
            compatibility = Compatibility,
            version = version,
        };

        public void SetupModifier(params string[] values) => Modifier = CreateModifier(Name, values);

        public void SetupModifier(int version, params string[] values) => Modifier = CreateModifier(Name, version, values);

        #endregion
    }
}
