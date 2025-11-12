using System.Collections.Generic;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Data.Modifiers
{
    /// <summary>
    /// Represents a running modifier loop.
    /// </summary>
    public class ModifierLoop : Exists
    {
        public ModifierLoop() { }

        public ModifierLoop(IModifierReference reference, Dictionary<string, string> variables)
        {
            this.reference = reference;
            this.variables = variables;
        }

        /// <summary>
        /// The current state of the modifier loop.
        /// </summary>
        public State state;

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

        /// <summary>
        /// Represets the current state of the modifier loop.
        /// </summary>
        public class State : Exists
        {
            /// <summary>
            /// If the current state has continued.
            /// </summary>
            public bool continued = false;
            /// <summary>
            /// If the current state has returned.
            /// </summary>
            public bool returned = false;
            /// <summary>
            /// Action modifiers at the start with no triggers before it should always run, so result is true by default.
            /// </summary>
            public bool result = true;
            /// <summary>
            /// If the first "or gate" argument is true, then ignore the rest.
            /// </summary>
            public bool triggered = false;
            /// <summary>
            /// The last trigger index used for else if triggers. Else if should only be considered if the index is higher than 0.
            /// </summary>
            public int triggerIndex = 0;
            /// <summary>
            /// The previous type of modifier.
            /// </summary>
            public Modifier.Type previousType = Modifier.Type.Action;
            /// <summary>
            /// The current index of the modifier loop.
            /// </summary>
            public int index = 0;
            /// <summary>
            /// The current index of a forLoop sequence.
            /// </summary>
            public int sequence = 0;
            /// <summary>
            /// The end of a forLoop sequence.
            /// </summary>
            public int end = 0;

            /// <summary>
            /// Resets the modifier loop state.
            /// </summary>
            public void Reset()
            {
                continued = false;
                returned = false;
                result = true;
                triggered = false;
                triggerIndex = 0;
                previousType = Modifier.Type.Action;
                index = 0;
                sequence = 0;
                end = 0;
            }
        }
    }
}
