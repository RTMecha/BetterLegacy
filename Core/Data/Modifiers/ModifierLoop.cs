using System.Collections.Generic;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Data.Modifiers
{
    /// <summary>
    /// Represents a running modifier loop.
    /// </summary>
    public class ModifierLoop
    {
        public ModifierLoop() { }

        public ModifierLoop(IModifierReference reference, Dictionary<string, string> variables)
        {
            this.reference = reference;
            this.variables = variables;
        }

        /// <summary>
        /// The current modifier index.
        /// </summary>
        public int index;

        /// <summary>
        /// The modifier object reference.
        /// </summary>
        public IModifierReference reference;

        /// <summary>
        /// The current modifier variables.
        /// </summary>
        public Dictionary<string, string> variables;

        public string this[string key]
        {
            get => variables[key];
            set => variables[key] = value;
        }

        /// <summary>
        /// Gets a formatted value.
        /// </summary>
        /// <param name="modifier">Modifier reference.</param>
        /// <param name="index">Index of the value to get.</param>
        /// <param name="valueVar">If <see cref="Modifier.GetValue(int, Dictionary{string, string})"/> should get variables.</param>
        /// <returns>Returns the formatted variable.</returns>
        public string GetFormattedValue(Modifier modifier, int index, bool valueVar = true) => ModifiersHelper.FormatStringVariables(modifier.GetValue(index, valueVar ? variables : null), variables);

        public bool TryGetValue(string key, out string value) => variables.TryGetValue(key, out value);

        /// <summary>
        /// If <see cref="variables"/> is null, assigns a new dictionary to it.
        /// </summary>
        public void ValidateDictionary()
        {
            if (variables == null)
                variables = new Dictionary<string, string>();
        }

        public static implicit operator Dictionary<string, string>(ModifierLoop modifierLoop) => modifierLoop.variables;
    }
}
