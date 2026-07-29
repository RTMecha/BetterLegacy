using System.Collections.Generic;

using UnityEngine;

namespace BetterLegacy.Core.Data.Modifiers
{
    /// <summary>
    /// Represents a running modifier loop.
    /// </summary>
    public class ModifierLoop : Exists
    {
        #region Constructors

        public ModifierLoop() { }

        public ModifierLoop(IModifierReference reference, Dictionary<string, string> variables)
        {
            this.reference = reference;
            this.variables = variables;
        }

        #endregion

        #region Values

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

        #endregion

        #region Functions
        
        /// <summary>
        /// The advanced way modifiers run.
        /// </summary>
        /// <param name="modifiers">The list of modifiers to run.</param>
        public ModifierLoopResult Run(List<Modifier> modifiers, int sequence = 0, int end = 0)
        {
            if (!state)
                state = new State();
            else
                state.Reset();
            state.sequence = sequence;
            state.end = end;
            while (state.index < modifiers.Count)
            {
                var modifier = modifiers[state.index];
                if (!modifier.function || !modifier.enabled || modifier.compatibility.StoryOnly && !ProjectArrhythmia.State.InStory)
                {
                    state.index++;
                    continue;
                }

                var name = modifier.Name;

                var isAction = modifier.type == Modifier.Type.Action;
                var isTrigger = modifier.type == Modifier.Type.Trigger;

                // Continue to the end of the modifier loop and set all modifiers to not running.
                if (state.continued)
                {
                    modifier.running = false;
                    state.index++;
                    continue;
                }

                if (modifier.function.SpecialFunction)
                {
                    modifier.RunAction(this);
                    state.previousType = modifier.type;
                    state.triggerIndex++;
                    continue;
                }

                if (isTrigger)
                {
                    if (state.previousType == Modifier.Type.Action) // If previous modifier was an action modifier, result should be considered true as we just started another modifier-block
                    {
                        if (name != "else")
                            state.result = true;
                        state.triggered = false;
                        state.triggerIndex = 0;
                    }

                    state.previousResult = state.result;

                    if (modifier.active || modifier.triggerCount > 0 && modifier.runCount >= modifier.triggerCount)
                    {
                        modifier.triggered = false;
                        state.result = false;
                    }
                    else if (name == "else") // else triggers inverse the previous trigger result
                    {
                        var innerResult = state.result;
                        state.result = !innerResult;
                        modifier.triggered = !innerResult;
                    }
                    else
                    {
                        var innerResult = modifier.not ? !modifier.RunTrigger(this) : modifier.RunTrigger(this);
                        var elseIf = state.triggerIndex > 0 && modifier.elseIf;

                        if (elseIf)
                        {
                            if (state.result) // If result is already active, set triggered to true
                                state.triggered = true;
                            else // Otherwise set the result to modifier trigger result
                                state.result = innerResult;
                        }
                        else if (!state.triggered && !innerResult)
                            state.result = false;

                        // Allow trigger to turn result to true again if "elseIf" is on
                        //if (modifier.elseIf && !result && innerResult)
                        //    result = true;

                        //if (!modifier.elseIf && !innerResult)
                        //    result = false;

                        modifier.triggered = innerResult;
                    }

                    state.previousType = modifier.type;
                    state.triggerIndex++;
                }

                // Set modifier inactive state
                if (!state.result && !(!modifier.active && !modifier.running))
                {
                    if (modifier.type != Modifier.Type.Trigger || !state.previousResult) // triggers should only be inactive if the previous triggers are inactive.
                        modifier.Reset(this, false);
                    modifier.function.HandleSkip(modifier, this, modifiers);
                    continue;
                }

                // Continue if modifier was already active with constant on
                if (modifier.active || !state.result || modifier.triggerCount > 0 && modifier.runCount >= modifier.triggerCount)
                {
                    modifier.function.HandleSkip(modifier, this, modifiers);
                    continue;
                }

                // run count is handled by the resetLoop function.
                if (!modifier.function.OverrideRunningState)
                {
                    if (!modifier.running)
                        modifier.runCount = Mathf.FloorToInt(modifier.runCount + (1 * Time.deltaTime));
                    modifier.running = true;
                }

                // Only occur once
                if (!modifier.constant && state.sequence + 1 >= state.end)
                    modifier.active = true;

                if (isAction && state.result) // Only run modifier if result is true
                    modifier.RunAction(this);

                state.previousType = modifier.type;
                state.index++;
            }

            return new ModifierLoopResult(state.returned, state.result, state.previousType, state.index);
        }

        /// <summary>
        /// If <see cref="variables"/> is null, assigns a new dictionary to it.
        /// </summary>
        public void ValidateDictionary()
        {
            if (variables == null)
                variables = new Dictionary<string, string>();
        }

        public static implicit operator Dictionary<string, string>(ModifierLoop modifierLoop) => modifierLoop.variables;

        #endregion

        /// <summary>
        /// Represets the current state of the modifier loop.
        /// </summary>
        public class State : Exists
        {
            #region Values

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
            public bool previousResult = true;
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

            #endregion

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
