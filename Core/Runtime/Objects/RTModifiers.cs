using System.Collections.Generic;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Runtime.Objects
{
    public class RTModifiers : Exists, IRTObject
    {
        public RTModifiers() { }

        public RTModifiers(List<Modifier> modifiers, IModifierReference reference, bool orderMatters, float startTime, float killTime)
        {
            this.modifiers = modifiers;
            this.reference = reference;
            this.orderMatters = orderMatters;

            StartTime = startTime;
            KillTime = killTime;

            modifiers.ForLoop(modifier => ModifiersHelper.AssignModifierActions(modifier, reference));
        }

        public IModifierReference reference;

        public List<Modifier> modifiers;

        public bool orderMatters;

        public float StartTime { get; set; }

        public float KillTime { get; set; }


        public Dictionary<string, string> variables = new Dictionary<string, string>();

        public void Clear() { }

        public void SetActive(bool active)
        {
            if (active)
                return;

            modifiers.ForLoop(modifier =>
            {
                modifier.runCount = 0;
                modifier.active = false;
                modifier.running = false;
                modifier.Inactive?.Invoke(modifier, reference, variables);
            });
        }

        public virtual void Interpolate(float time)
        {
            if (orderMatters)
                ModifiersHelper.RunModifiersLoop(modifiers, reference, variables);
            else
                ModifiersHelper.RunModifiersAll(modifiers, reference, variables);
        }
    }
}
