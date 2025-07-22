using System.Collections.Generic;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Runtime.Objects
{
    public class RTModifiers<T> : Exists, IRTObject
    {
        public RTModifiers() { }

        public RTModifiers(List<Modifier<T>> modifiers, bool orderMatters, float startTime, float killTime)
        {
            this.modifiers = modifiers;
            this.orderMatters = orderMatters;

            StartTime = startTime;
            KillTime = killTime;
        }

        public List<Modifier<T>> modifiers;

        public bool orderMatters;

        public float StartTime { get; set; }

        public float KillTime { get; set; }

        public int Room { get; set; }

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
                modifier.Inactive?.Invoke(modifier, variables);
            });
        }

        public void Interpolate(float time)
        {
            variables.Clear();
            if (orderMatters)
                ModifiersHelper.RunModifiersLoop(modifiers, variables);
            else
                ModifiersHelper.RunModifiersAll(modifiers, variables);
        }
    }
}
