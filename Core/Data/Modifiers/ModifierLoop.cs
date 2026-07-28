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
        /// The original way modifiers run.
        /// </summary>
        /// <param name="modifiers">The list of modifiers to run.</param>
        public ModifierLoopResult RunModifiersAll(List<Modifier> modifiers) => RunModifiersAll(null, null, modifiers);

        /// <summary>
        /// The original way modifiers run.
        /// </summary>
        /// <param name="triggers">The list of triggers to check.</param>
        /// <param name="actions">The list of actions to run.</param>
        /// <param name="modifiers">The list of modifiers to run.</param>
        public ModifierLoopResult RunModifiersAll(List<Modifier> triggers, List<Modifier> actions, List<Modifier> modifiers)
        {
            if (triggers == null || actions == null)
            {
                triggers = new List<Modifier>();
                actions = new List<Modifier>();
                modifiers.ForLoop(modifier =>
                {
                    switch (modifier.type)
                    {
                        case Modifier.Type.Trigger: {
                                triggers.Add(modifier);
                                break;
                            }
                        case Modifier.Type.Action: {
                                actions.Add(modifier);
                                break;
                            }
                    }
                });
            }
            if (!state)
                state = new State();
            else
                state.Reset();
            if (!triggers.IsEmpty())
            {
                // If all triggers are active
                bool result = true;
                triggers.ForLoop(trigger =>
                {
                    if (trigger.compatibility.StoryOnly && !ProjectArrhythmia.State.InStory || trigger.active || trigger.triggerCount > 0 && trigger.runCount >= trigger.triggerCount)
                    {
                        trigger.triggered = false;
                        result = false;
                        return;
                    }

                    var innerResult = trigger.not ? !trigger.RunTrigger(trigger, this) : trigger.RunTrigger(trigger, this);

                    if (trigger.elseIf && !result && innerResult)
                        result = true;

                    if (!trigger.elseIf && !innerResult)
                        result = false;

                    trigger.triggered = innerResult;

                    if (!trigger.running)
                        trigger.runCount = Mathf.FloorToInt(trigger.runCount + (1 * Time.deltaTime));
                    if (!trigger.constant)
                        trigger.active = true;

                    trigger.running = true;
                });
                if (result)
                {
                    bool returned = false;
                    actions.ForLoop(act =>
                    {
                        if (!act.enabled || act.compatibility.StoryOnly && !ProjectArrhythmia.State.InStory || returned || act.active || act.triggerCount > 0 && act.runCount >= act.triggerCount) // Continue if modifier is not constant and was already activated
                            return;

                        if (!act.running)
                            act.runCount = Mathf.FloorToInt(act.runCount + (1 * Time.deltaTime));
                        if (!act.constant)
                            act.active = true;

                        act.running = true;
                        act.RunAction(act, this);
                        if (act.Name == "return")
                            returned = true;
                    });
                    return new ModifierLoopResult(returned, true, Modifier.Type.Action, modifiers.Count);
                }

                // Deactivate both action and trigger modifiers
                modifiers.ForLoop(modifier =>
                {
                    if (!modifier.enabled || modifier.compatibility.StoryOnly && !ProjectArrhythmia.State.InStory || !modifier.active && !modifier.running)
                        return;

                    modifier.active = false;
                    modifier.running = false;
                    modifier.RunInactive(modifier, this);
                });
                return new ModifierLoopResult(false, false, Modifier.Type.Action, modifiers.Count);
            }
            actions.ForLoop(act =>
            {
                if (!act.enabled || act.compatibility.StoryOnly && !ProjectArrhythmia.State.InStory || act.active || act.triggerCount > 0 && act.runCount >= act.triggerCount)
                    return;

                if (!act.running)
                    act.runCount = Mathf.FloorToInt(act.runCount + (1 * Time.deltaTime));
                if (!act.constant)
                    act.active = true;

                act.running = true;
                act.RunAction(act, this);
            });
            return new ModifierLoopResult(false, true, Modifier.Type.Action, modifiers.Count);
        }

        /// <summary>
        /// The advanced way modifiers run.
        /// </summary>
        /// <param name="modifiers">The list of modifiers to run.</param>
        public ModifierLoopResult RunModifiersLoop(List<Modifier> modifiers, int sequence = 0, int end = 0)
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
                if (!modifier.enabled || modifier.compatibility.StoryOnly && !ProjectArrhythmia.State.InStory)
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

                if (isTrigger)
                {
                    if (state.previousType == Modifier.Type.Action) // If previous modifier was an action modifier, result should be considered true as we just started another modifier-block
                    {
                        if (name != "else")
                            state.result = true;
                        state.triggered = false;
                        state.triggerIndex = 0;
                    }

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
                        var innerResult = modifier.not ? !modifier.RunTrigger(modifier, this) : modifier.RunTrigger(modifier, this);
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

                if (name == "return" || name == "continue") // return stops the loop (any), continue moves it to the next loop (forLoop only)
                {
                    // Set modifier inactive state
                    if (!state.result && !(!modifier.active && !modifier.running))
                    {
                        modifier.active = false;
                        modifier.running = false;
                        state.result = false;
                    }

                    if (modifier.active || !state.result || modifier.triggerCount > 0 && modifier.runCount >= modifier.triggerCount) // don't return
                        state.result = false;

                    if (!modifier.running)
                        modifier.runCount = Mathf.FloorToInt(modifier.runCount + (1 * Time.deltaTime));

                    // Only occur once
                    if (!modifier.constant && state.sequence + 1 >= state.end)
                        modifier.active = true;

                    modifier.running = state.result;

                    if (state.result)
                    {
                        state.continued = true;
                        state.returned = name == "return";
                    }

                    state.result = true;

                    state.previousType = modifier.type;
                    state.index++;
                    continue;
                }

                // Set modifier inactive state
                if (!state.result && !(!modifier.active && !modifier.running))
                {
                    modifier.active = false;
                    modifier.running = false;
                    modifier.RunInactive(modifier, this);

                    state.previousType = modifier.type;
                    state.index++;
                    continue;
                }

                // Continue if modifier was already active with constant on
                if (modifier.active || !state.result || modifier.triggerCount > 0 && modifier.runCount >= modifier.triggerCount)
                {
                    if (name == nameof(ModifierFunctions.forLoop) || name == nameof(ModifierFunctions.forLoopPlayer))
                    {
                        var endIndex = modifiers.FindLastIndex(x => x.Name == "return"); // return is treated as a break of the for loop
                        state.previousType = modifier.type;
                        state.index = endIndex <= state.index ? modifiers.Count : endIndex;
                        continue;
                    }

                    state.previousType = modifier.type;
                    state.index++;
                    continue;
                }

                // run count is handled by the resetLoop function.
                if (name != nameof(ModifierFunctions.resetLoop))
                {
                    if (!modifier.running)
                        modifier.runCount = Mathf.FloorToInt(modifier.runCount + (1 * Time.deltaTime));

                    modifier.running = true;
                }

                // Only occur once
                if (!modifier.constant && state.sequence + 1 >= state.end)
                    modifier.active = true;

                if (isAction && state.result) // Only run modifier if result is true
                    modifier.RunAction(modifier, this);

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
