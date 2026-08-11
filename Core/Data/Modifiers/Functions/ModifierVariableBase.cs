using UnityEngine;

using BetterLegacy.Editor;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    /// <summary>
    /// Base class for variable modifier functions. Using this class as the base of a modifier function is not required for it to be a "variable modifier" as some variable modifiers assign to multiple keys.
    /// </summary>
    public abstract class ModifierVariableBase : ModifierActionBase
    {
        #region Values

        public override Sprite Icon => EditorSprites.DownArrow;

        #endregion

        #region Functions

        /// <summary>
        /// Variable function.
        /// </summary>
        /// <param name="modifier">Modifier reference.</param>
        /// <param name="modifierLoop">The current modifier loop.</param>
        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var value = GetValue(modifier, modifierLoop);
            if (value != null)
                modifierLoop.variables[GetKey(modifier, modifierLoop)] = value;
        }

        /// <summary>
        /// Gets the variable key.
        /// </summary>
        /// <param name="modifier">Modifier reference.</param>
        /// <param name="modifierLoop">The current modifier loop.</param>
        /// <returns>Returns the modifier variable key.</returns>
        public virtual string GetKey(Modifier modifier, ModifierLoop modifierLoop) => FormatStringVariables(modifier.GetValue(0), modifierLoop.variables);

        /// <summary>
        /// Gets the variable value.
        /// </summary>
        /// <param name="modifier">Modifier reference.</param>
        /// <param name="modifierLoop">The current modifier loop.</param>
        /// <returns>Returns the modifier variable value.</returns>
        public abstract string GetValue(Modifier modifier, ModifierLoop modifierLoop);

        #endregion
    }
}
