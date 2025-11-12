using System.Collections.Generic;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Modifiers;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Runtime.Objects
{
    public class RTModifiers : Exists, IRTObject
    {
        public RTModifiers() { }

        public RTModifiers(List<Modifier> modifiers, IModifierReference reference, bool orderMatters, float startTime, float killTime, RTLevelBase parentRuntime)
        {
            this.modifiers = modifiers;
            this.reference = reference;
            this.orderMatters = orderMatters;
            UpdateCache();

            ParentRuntime = parentRuntime;
            StartTime = startTime;
            KillTime = killTime;

            modifiers.ForLoop(modifier => ModifiersHelper.AssignModifierFunctions(modifier, reference.ReferenceType));

            loop = new ModifierLoop
            {
                variables = variables,
                reference = reference,
            };
        }

        public IModifierReference reference;

        public List<Modifier> modifiers;

        public bool orderMatters;

        public RTLevelBase ParentRuntime { get; set; }

        public float StartTime { get; set; }
        public float KillTime { get; set; }
        public bool Active { get; set; }

        public bool active;

        public List<Modifier> triggers;

        public List<Modifier> actions;

        public void UpdateCache()
        {
            if (orderMatters)
                return;

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

        public ModifierLoop loop;


        public Dictionary<string, string> variables = new Dictionary<string, string>();

        public void Clear() { }

        public void SetActive(bool active)
        {
            Active = active;
            this.active = active;
            if (active)
                return;

            modifiers.ForLoop(modifier =>
            {
                modifier.runCount = 0;
                modifier.active = false;
                modifier.running = false;
                modifier.RunInactive(modifier, loop);
            });
        }

        public virtual void Interpolate(float time)
        {
            if (orderMatters)
                ModifiersHelper.RunModifiersLoop(modifiers, loop);
            else
                ModifiersHelper.RunModifiersAll(triggers, actions, modifiers, loop);
        }

        public override string ToString() => reference?.ToString() ?? string.Empty;
    }
}
